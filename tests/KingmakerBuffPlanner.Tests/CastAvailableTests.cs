using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        private static void RunCastAvailableTests(string root)
        {
            Run("cast-available-migration-once-and-intent-preserved", () => TestAvailableMigration(root));
            Run("cast-available-migration-failed-save-backup-and-pending", () => TestAvailableRecovery(root));
            Run("cast-available-classic-import-keeps-current-choice", () => TestAvailableImport(root));
            Run("cast-available-zero-ready-strict-and-long-log", () => TestAvailableAdmission(root));
            Run("cast-available-controller-default-logs-once", () => TestAvailableController(root));
            Run("cast-available-depleted-selected-preparation-names-real-cause", () => TestAvailablePreparedCause(root));
            foreach (string executionMode in new[] { "instant", "animated" })
            {
                string mode = executionMode;
                Run("cast-available-partial-order-and-log-" + mode, () => TestAvailablePartial(root, mode));
                Run("cast-available-evolving-repeat-" + mode, () => TestAvailableRepeat(root, mode));
                Run("cast-available-partial-unconfirmed-halts-" + mode, () => TestAvailableFailure(root, mode));
                Run("cast-available-late-shortage-still-halts-" + mode, () => TestAvailableLateShortage(root, mode));
                Run("cast-available-active-report-control-" + mode,
                    () => TestAvailableActiveReport(root, mode, false));
                Run("cast-available-active-report-after-noop-" + mode,
                    () => TestAvailableActiveReport(root, mode, true));
            }
        }

        private static void TestAvailableMigration(string root)
        {
            foreach (string policy in new[] { ExecutionProfile.RequireFullPlan, ExecutionProfile.CastAvailable, null, "unknown" })
            {
                string dir = Path.Combine(root, "available-migrate-" + (policy ?? "null"));
                var repository = new CastingPlanRepository(dir);
                var profile = CastingPlanProfile.FromDocument(CastingDocument(
                    ProblemCasting("kept-a", 0, state: CastingAuthoringState.Ready),
                    ProblemCasting("kept-b", 1, state: CastingAuthoringState.Disabled)));
                profile.FormatRevision = 4;
                profile.Execution.AvailabilityPolicy = policy;
                profile.Execution.Mode = "animated";
                profile.Execution.RecastExisting = true;
                profile.Ui.Scale = 1.2f;
                profile.Ui.Hotkey = "Ctrl+Shift+B";
                repository.Save(profile);
                string path = repository.GetProfilePath(profile.CampaignId);
                string original = File.ReadAllText(path);
                var session = new CastingWorkspaceSession(dir, profile.CampaignId);
                Expect(session.ApplyMode == CastingApplyMode.ReadyCastsOnly && session.IsDirty &&
                    session.FooterSaveState != "saved", "legacy policy was strict or falsely durable");
                session.BuildGraph(GraphInputs());
                session.Reload();
                Expect(File.ReadAllText(path) == original && session.IsDirty, "read/reload wrote a migration");
                session.Save();
                var after = JObject.Parse(File.ReadAllText(path));
                var before = JObject.Parse(original);
                Expect((int)after["formatRevision"] == 5 &&
                    (string)after["execution"]["availabilityPolicy"] == ExecutionProfile.CastAvailable,
                    "save did not stamp the migration");
                after.Remove("formatRevision"); before.Remove("formatRevision");
                ((JObject)after["execution"]).Remove("availabilityPolicy");
                ((JObject)before["execution"]).Remove("availabilityPolicy");
                Expect(JToken.DeepEquals(before, after), "migration changed unrelated intent/settings");
                session.SetAvailabilityPolicy(ExecutionProfile.RequireFullPlan);
                var reopened = new CastingWorkspaceSession(dir, profile.CampaignId);
                Expect(reopened.ApplyMode == CastingApplyMode.Ordinary && !reopened.IsDirty,
                    "post-migration strict was reset");
                reopened.Reload();
                var clear = reopened.BeginClearPlan();
                Expect(reopened.ClearPlan(clear).Applied && reopened.Undo(), "clear/undo fixture failed");
                reopened.FocusCasting("kept-a");
                Expect(reopened.RemoveFocusedCasting().Applied && reopened.Undo(), "edit/undo fixture failed");
                reopened.Save();
                Expect(new CastingWorkspaceSession(dir, profile.CampaignId).ApplyMode == CastingApplyMode.Ordinary,
                    "document Undo revived another policy");
                reopened.SetAvailabilityPolicy(ExecutionProfile.CastAvailable);
                Expect(new CastingWorkspaceSession(dir, profile.CampaignId).ApplyMode == CastingApplyMode.ReadyCastsOnly,
                    "turning strict off was not saved");
            }
        }

        private static void TestAvailableRecovery(string root)
        {
            var run = new RepeatSession(root, "instant", "migration-recovery");
            string dir = Path.GetDirectoryName(Path.GetDirectoryName(run.Session.PlanPathForRuntime));
            string path = run.Session.PlanPathForRuntime;
            var legacy = JObject.Parse(File.ReadAllText(path));
            legacy["formatRevision"] = 4;
            legacy["execution"]["availabilityPolicy"] = ExecutionProfile.RequireFullPlan;
            File.WriteAllText(path, legacy.ToString());
            byte[] original = File.ReadAllBytes(path);
            CastingWorkspaceSession session;
            var boundary = new AcceptingDispatchBoundary();
            using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                session = new CastingWorkspaceSession(dir, "repeat-campaign", boundary);
                var result = CastingRunPolicy.Apply(session, null, "long", run.World.Inputs());
                Expect(!result.Allowed && result.ReviewReason.StartsWith("persistence-failed:") &&
                    session.FooterSaveState.StartsWith("save-failed:") && session.IsDirty,
                    "failed migration save admitted a run or claimed Saved");
                Expect(File.ReadAllBytes(path).SequenceEqual(original), "failed save replaced old bytes");
                var pending = session.CaptureRecovery();
                var recovered = new CastingWorkspaceSession(dir, "repeat-campaign", boundary, recovery: pending);
                Expect(recovered.ApplyMode == CastingApplyMode.ReadyCastsOnly && recovered.IsDirty &&
                    !CastingRunPolicy.Apply(recovered, null, "long", run.World.Inputs()).Allowed,
                    "pending recovery lost policy/durability barrier");
            }
            Expect(session.RetryFailedSave() && !session.IsDirty, "migration retry did not save");
            // An unreadable primary cannot be replaced just because a legacy backup loaded.
            File.WriteAllBytes(path + ".bak.1", original);
            File.WriteAllText(path, "{bad-json");
            var backup = new CastingWorkspaceSession(dir, "repeat-campaign", boundary);
            Expect(backup.LoadStatus == CastingPlanLoadStatus.RecoveredFromBackup && backup.IsDirty &&
                backup.ApplyMode == CastingApplyMode.ReadyCastsOnly, "backup policy was not migrated");
            Expect(!CastingRunPolicy.Apply(backup, null, "long", run.World.Inputs()).Allowed &&
                File.ReadAllText(path) == "{bad-json", "migration overwrote protected primary");
            File.WriteAllBytes(path, original);
            var future = JObject.Parse(File.ReadAllText(path)); future["formatRevision"] = 6;
            File.WriteAllText(path, future.ToString());
            var unsupported = new CastingWorkspaceSession(dir, "repeat-campaign", boundary);
            Expect(unsupported.PersistenceBlocked && unsupported.LoadStatus == CastingPlanLoadStatus.UnsupportedSchema &&
                !CastingRunPolicy.Apply(unsupported, null, "long", run.World.Inputs()).Allowed,
                "a future profile escaped rejection");
        }

        private static void TestAvailableImport(string root)
        {
            foreach (bool chosen in new[] { false, true })
            {
                string dir = Path.Combine(root, "available-classic-" + chosen);
                var legacy = BuffPlannerProfile.CreateDefault("available-classic");
                legacy.Execution.AvailabilityPolicy = ExecutionProfile.RequireFullPlan;
                legacy.Execution.Mode = "animated";
                var session = new CastingWorkspaceSession(dir, legacy.CampaignId);
                if (chosen) session.SetAvailabilityPolicy(ExecutionProfile.RequireFullPlan);
                // Simulate Classic becoming available before explicit Reload/import.
                if (File.Exists(session.PlanPathForRuntime)) File.Delete(session.PlanPathForRuntime);
                new ProfileRepository(dir).Save(legacy);
                session.Reload();
                Expect(session.MigrationStatus == CastingMigrationStatus.Migrated &&
                    session.AvailabilityPolicy == (chosen ? ExecutionProfile.RequireFullPlan : ExecutionProfile.CastAvailable),
                    "Classic import inherited strict or lost a current explicit choice");
                Expect(new CastingPlanRepository(dir).Load(legacy.CampaignId).Profile.FormatRevision == 5,
                    "Classic import did not stamp the migration");
                Expect(session.ExecutionMode == "animated", "a policy-only choice overwrote the inherited mode");
            }
        }

        private static void TestAvailableAdmission(string root)
        {
            string dir;
            var castings = new[] {
                ProblemCasting("ready", 0, state: CastingAuthoringState.Ready),
                ProblemCasting("unavailable", 1, state: CastingAuthoringState.Ready),
                ProblemCasting("disabled", 2, state: CastingAuthoringState.Disabled) };
            var session = ProblemSession(root, "available-admission", castings, out dir);
            var logs = new List<string>();
            session.RunLog = (lines, warning) => logs.AddRange(lines);
            session.SetAvailabilityPolicy(ExecutionProfile.RequireFullPlan);
            var strict = CastingRunPolicy.Apply(session, null, "long", GraphInputs(bardLevel2: 1));
            Expect(!strict.Allowed && !CastingRunPresentation.OpensPlanner(strict) &&
                logs.Count == 4 && logs[0].Contains("1 unavailable") &&
                logs[0].Contains("1 withheld by Require full plan") &&
                logs[1].Contains("ready, withheld") && logs[2].Contains("earlier planned castings") &&
                logs[3].Contains("explicitly disabled"), "strict admission lost blockers/withheld/disabled");
            foreach (string line in logs) Console.WriteLine("[CAST-AVAILABLE-STRICT] " + line);
            logs.Clear();
            session.SetAvailabilityPolicy(ExecutionProfile.CastAvailable);
            var zero = CastingRunPolicy.Apply(session, null, "long", GraphInputs(bardLevel2: 0));
            Expect(!zero.Allowed && logs.Count == 4 && logs[0].Contains("2 unavailable") &&
                logs[1].Contains(session.RunCastingLabel("ready")) &&
                !logs[1].Contains("spellbook"), "zero-ready logging was missing or misleading");
            foreach (string line in logs) Console.WriteLine("[CAST-AVAILABLE-ZERO] " + line);
            int count = logs.Count;
            session.BuildGraph(GraphInputs()); session.BuildResourceReport(GraphInputs(), WorkspaceResourceScope.SelectedRoutine);
            ControllerRoutineDetails.LatestOutcome(session);
            Expect(logs.Count == count, "preview/handoff replayed a report");
            logs.Clear();
            session.SelectRoutine("short");
            CastingRunPolicy.Apply(session, null, "short", GraphInputs());
            Expect(logs.Count == 1 && logs[0].Contains("nothing configured to cast"), "empty no-op was dishonest");
            // All entries survive a long report, including unresolved saved identity.
            var many = Enumerable.Range(0, 150).Select(i => new PlannedCasting("lost-" + i, "long", i,
                "missing-" + i, new AbilityKey("missing-ability-" + i, null, 0, SourceKind.Spellbook, null),
                "unit-bard", "book-bard", CastingTargetMode.DirectTarget, "unit-t1", null, null,
                null, null, ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null)).ToArray();
            var longSession = ProblemSession(root, "available-long", many, out dir);
            logs.Clear(); longSession.RunLog = (lines, warning) => logs.AddRange(lines);
            CastingRunPolicy.Apply(longSession, null, "long", GraphInputs());
            Expect(logs.Count == 151 && logs.Skip(1).Distinct().Count() == 150 &&
                logs.Last().Contains("Casting 150"), "long report dropped or conflated omissions");
        }

        private static void TestAvailablePartial(string root, string mode)
        {
            var run = new RepeatSession(root, mode, "available-partial", true);
            foreach (string token in run.World.State.Tokens.Keys.ToArray()) run.World.State.Tokens[token] = false;
            run.World.State.Tokens[run.World.State.Primaries.First()] = true;
            var logs = new List<string>(); run.Session.RunLog = (lines, warning) => logs.AddRange(lines);
            string intent = run.Session.DocumentIntentSignature();
            var result = run.Apply(useSavedPolicy: true);
            Expect(result.Allowed && result.GateDecision.Mode == CastingApplyMode.ReadyCastsOnly &&
                run.Host.LastReport.Confirmed == 2 && run.Host.LastReport.Omitted == 2 &&
                run.World.State.Fired.SequenceEqual(new[] { run.Session.Document.Castings[0].CastingId,
                    run.Session.Document.Castings[3].CastingId }) &&
                run.Session.DocumentIntentSignature() == intent, "default subset/order/intent changed");
            Expect(logs.Count == 3 && logs[0].Contains("2 unavailable") &&
                logs[1].Contains("unit-t2") && logs[2].Contains("unit-t3"), "partial log hid omissions");
            if (mode == "instant")
                foreach (string line in logs) Console.WriteLine("[CAST-AVAILABLE-PARTIAL] " + line);
            int count = logs.Count;
            run.Session.RecordRunReport(run.Host.LastReport);
            Expect(logs.Count == count, "completion replay duplicated a log");
            run.Session.RunLog = (lines, warning) => { throw new InvalidOperationException("native log unavailable"); };
            var next = run.Apply(useSavedPolicy: true);
            Expect(!next.Allowed && run.Host.StartedRuns == 1, "log failure changed admission");
        }

        private static void TestAvailableRepeat(string root, string mode)
        {
            var run = new RepeatSession(root, mode, "available-repeat", true);
            var logs = new List<string>(); run.Session.RunLog = (lines, warning) => logs.AddRange(lines);
            Expect(run.Apply(useSavedPolicy: true).Allowed, "default first run failed");
            CastingRunReport first = run.Host.LastReport;
            logs.Clear();
            Expect(!run.Apply(useSavedPolicy: true).Allowed && run.Host.StartedRuns == 1 &&
                logs.Count == 5 && logs[0].Contains("4 already active") &&
                logs.All(line => !line.Contains("resource use")), "repeat replayed spending/completion");
            run.World.State.Active.Remove("unit-t4"); run.World.State.SpontaneousRemaining = 1;
            logs.Clear();
            Expect(run.Apply(useSavedPolicy: true).Allowed && run.Host.LastReport.Confirmed == 1 &&
                run.Host.LastReport.Skipped == 3 && run.World.State.Fired.Count == 5 &&
                logs.Count == 4, "covered exhausted sources blocked an affordable later cast");
            run.Session.RecordRunReport(first);
            Expect(logs.Count == 4 && run.Session.LastRunReport == run.Host.LastReport,
                "an older completion callback replayed or replaced the latest report");
        }

        private static void TestAvailableFailure(string root, string mode)
        {
            var run = new RepeatSession(root, mode, "available-failure", true);
            run.World.State.Active["unit-t1"] = new KeyValuePair<string, long>("old-instance", 100);
            foreach (string token in run.World.State.Tokens.Keys.ToArray()) run.World.State.Tokens[token] = false;
            run.World.State.Tokens[run.World.State.Primaries.First()] = true;
            run.World.Observation = "retain";
            var logs = new List<string>(); run.Session.RunLog = (lines, warning) => logs.AddRange(lines);
            Expect(run.Apply(recast: true, useSavedPolicy: true).Allowed, "partial recast did not start");
            var report = run.Host.LastReport;
            Expect(report.Halted && report.Failed == 1 && report.NotProcessed == 1 && report.Omitted == 2 &&
                report.ResourcesSpent == 1 && run.World.State.Fired.Count == 1 && logs.Count == 5 &&
                report.Entries[0].Detail.Contains("expected-application-unconfirmed") &&
                report.Entries[0].Detail.Contains("effect-evidence"), "partial run weakened unconfirmed stop/evidence");
        }

        private static void TestAvailableController(string root)
        {
            var fixture = new ControllerFixture(root, "available-default", count: 2);
            PartyProviderSnapshot snapshot;
            fixture.Inputs = WorkspaceInputs(out snapshot, remainingPerCaster: 1);
            var logs = new List<string>(); fixture.Session.RunLog = (lines, warning) => logs.AddRange(lines);
            fixture.Menu.Confirm();
            Expect(fixture.LastApply.Allowed && fixture.LastApply.GateDecision.Mode == CastingApplyMode.ReadyCastsOnly &&
                logs.Count == 0, "controller forced strict or logged an admission for a started run");
            fixture.Pump();
            Expect(logs.Count == 2 && fixture.Host.LastReport.Confirmed == 1 && logs[0].Contains("1 unavailable"),
                "controller completion did not use shared reporting");
            fixture.Session.RecordRunReport(fixture.Host.LastReport);
            ControllerRoutineDetails.LatestOutcome(fixture.Session);
            Expect(logs.Count == 2, "controller handoff replayed completion");
        }

        private static void TestAvailableLateShortage(string root, string mode)
        {
            var run = new RepeatSession(root, mode, "available-late-shortage", true);
            run.World.State.Tokens[run.World.State.Primaries.Last()] = false;
            run.World.ValidationFailureId = run.Session.Document.Castings[1].CastingId;
            var logs = new List<string>(); run.Session.RunLog = (lines, warning) => logs.AddRange(lines);
            Expect(run.Apply(useSavedPolicy: true).Allowed, "late-shortage fixture was refused at preflight");
            var report = run.Host.LastReport;
            Expect(report.Halted && report.Confirmed == 1 && report.Failed == 1 && report.Omitted == 1 &&
                report.NotProcessed == 1 && report.ResourcesSpent == 1 && run.World.State.Fired.Count == 1 &&
                logs.Count == 4 && logs[0].Contains("1 unavailable") && logs[0].Contains("1 failed") &&
                report.Entries[1].Detail.Contains("simulated-late-resource-shortage"),
                "a late resource shortage was reclassified as a harmless omission or continued");
        }

        private static void TestAvailableActiveReport(string root, string mode, bool interveningAttempt)
        {
            var run = new RepeatSession(root, mode, "active-report-" + interveningAttempt, true);
            foreach (string token in run.World.State.Tokens.Keys.ToArray()) run.World.State.Tokens[token] = false;
            run.World.State.Tokens[run.World.State.Primaries.First()] = true;
            var logs = new List<string>(); run.Session.RunLog = (lines, warning) => logs.AddRange(lines);
            var started = run.Session.Apply(run.Session.ApplyMode, "long", run.World.Inputs());
            Expect(started.Allowed && run.Host.IsRunning && logs.Count == 0, "partial Long did not start quietly");
            string[] expected = started.GateDecision.Omissions.Select(omission =>
                "Not cast: " + run.Session.RunCastingLabel(omission.CastingId) +
                " — not enough prepared uses; earlier planned castings have first claim on this pool.").ToArray();
            int frames = 0;
            while (run.World.State.Fired.Count == 0 && run.Host.IsRunning && frames++ < 100) run.Host.Pump();
            Expect(run.Host.IsRunning && run.World.State.Fired.Count == 1 &&
                run.World.State.Tokens.Values.All(available => !available), "first resource spend was not observed");
            // Reopening/refreshing uses the actual workspace read model. The
            // subsequent direct Apply is the mouse planner's Run boundary.
            run.Session.BuildGraph(run.World.Inputs());
            if (interveningAttempt)
            {
                run.Session.SelectRoutine("short");
                run.Session.BuildGraph(run.World.Inputs());
                var empty = run.Session.Apply(run.Session.ApplyMode, "short", run.World.Inputs());
                Expect(!empty.Allowed && empty.ReviewReason == "nothing-to-cast:0" &&
                    logs.Count == 1 && logs[0].Contains("Short") && logs[0].Contains("nothing configured to cast") &&
                    run.Host.StartedRuns == 1 && run.Host.ActiveScopeRoutineId == "long",
                    "empty Short did not log exactly one independent no-op");
                run.Session.BuildGraph(run.World.Inputs());
            }
            frames = 0;
            while (run.Host.IsRunning && frames++ < 100) run.Host.Pump();
            Expect(!run.Host.IsRunning && run.Host.LastReport.Confirmed == 2 && run.Host.LastReport.Omitted == 2 &&
                run.Host.LastReport.ResourcesSpent == 2 && run.World.State.Fired.Count == 2,
                "intervening attempt changed Long's cast subset or spending");
            string[] actual = logs.Where(line => line.StartsWith("Not cast:", StringComparison.Ordinal)).ToArray();
            Expect(actual.SequenceEqual(expected), "active run's admission identity/reasons changed: " +
                string.Join(" | ", actual));
            Expect(logs.Count == (interveningAttempt ? 4 : 3) && logs.Count(line => line.Contains("2 cast, 2 unavailable")) == 1,
                "Long's terminal report was missing or duplicated");
            run.Session.RecordRunReport(run.Host.LastReport);
            Expect(logs.Count == (interveningAttempt ? 4 : 3), "completion replay duplicated Long's report");
        }

        private static void TestAvailablePreparedCause(string root)
        {
            var run = new RepeatSession(root, "instant", "available-prepared-cause");
            string spent = run.World.State.Primaries.First();
            run.World.State.Tokens[spent] = false;
            CastingWorkspaceInputs original = run.World.Inputs();
            ProviderSnapshot wizard = original.Snapshot.Providers.Single(p => p.Key.CasterUnitId == "unit-wizard");
            var selected = new ProviderSnapshot(wizard.Key, wizard.DisplayName, wizard.SpellLevel,
                wizard.ResourcePoolKey, 1, new[] { spent }, null, 1, 100, "", "");
            var providers = original.Snapshot.Providers.Select(p => p.Key.Equals(wizard.Key) ? selected : p).ToArray();
            var options = providers.Select(p => new ProviderPlanningOption(p,
                original.Snapshot.Units.Select(u => u.UnitId), new[] { p.Key.CasterUnitId }, 10, 100,
                CastExecutionStrategy.DirectRuleCast, "simulated-direct", null));
            var inputs = new CastingWorkspaceInputs(new PartyProviderSnapshot(original.Snapshot.Units,
                providers, original.Snapshot.ResourcePools), options, original.EffectsBySource, original.Enhancements);
            var logs = new List<string>(); run.Session.RunLog = (lines, warning) => logs.AddRange(lines);
            Expect(CastingRunPolicy.Apply(run.Session, null, "long", inputs).Allowed, "later sentinel was blocked");
            while (run.Host.IsRunning) run.Host.Pump();
            Expect(logs.Count == 2 && logs[1].Contains("no prepared uses remain") &&
                !logs[1].Contains("earlier planned"), "other preparations disguised a depleted exact source");
        }
    }
}
