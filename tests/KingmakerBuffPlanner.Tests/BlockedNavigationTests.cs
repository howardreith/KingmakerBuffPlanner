using System;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        private static void RunBlockedNavigationTests(string root)
        {
            Run("blocked-navigation-structured-order-and-omissions", () => TestProblemContract(root));
            Run("blocked-navigation-focus-next-previous-have-no-side-effects", () => TestProblemNavigation(root));
            Run("blocked-navigation-draft-unresolved-record-stays-authored", () => TestProblemUnresolved(root));
            Run("blocked-navigation-repair-recomputes-through-authoring", () => TestProblemRepair(root));
            Run("blocked-navigation-repair-last-retains-new-last", () => TestProblemLastRepair(root));
            Run("blocked-navigation-manual-selection-and-escape-leave-mode", () => TestProblemManualExit(root));
            Run("blocked-navigation-global-refusal-never-requests-focus", () => TestProblemGlobalRefusal(root));
            Run("blocked-navigation-planner-and-hud-scopes-share-focus", () => TestProblemRoutes(root));
            Run("blocked-navigation-ready-only-and-success-clear-pending", () => TestProblemReadyOnly(root));
            Run("blocked-navigation-reveal-is-consumed-once", () => TestProblemOneShot(root));
            Run("blocked-navigation-scroll-offset-uses-viewport-bounds", TestProblemRevealGeometry);
            Run("blocked-navigation-physical-judgment-requires-visible-points", TestProblemPhysicalJudgment);
            Run("blocked-navigation-preserves-existing-undo-entry", () => TestProblemExistingUndo(root));
            Run("blocked-navigation-global-refresh-failure-clears-stale-focus", () => TestProblemGlobalRefresh(root));
            Run("blocked-navigation-unavailable-source-keeps-reveal-targets", () => TestProblemUnavailableSource(root));
        }

        private static PlannedCasting ProblemCasting(string id, int order,
            string routine = "long", string source = GraphSourceA,
            CastingAuthoringState state = CastingAuthoringState.Draft,
            string caster = "unit-bard", string target = "unit-t1")
        {
            return new PlannedCasting(id, routine, order, source,
                source == GraphSourceA ? GraphAbilityA : source == GraphSourceB ? GraphAbilityB : GraphAbilityD,
                caster, caster == null ? null : "book-bard", CastingTargetMode.DirectTarget, target,
                null, null, null, null, ExistingEffectPolicy.SkipAlreadyActive, null, state, null);
        }

        private static CastingWorkspaceSession ProblemSession(string root, string name,
            PlannedCasting[] castings, out string dir, ICastingDispatchBoundary dispatch = null)
        {
            dir = Path.Combine(root, "blocked-navigation-" + name);
            Directory.CreateDirectory(dir);
            var document = new CastingPlanDocument("problem-campaign", new[] {
                new RoutineDefinition("long", "Long"),
                new RoutineDefinition("important", "Important"),
                new RoutineDefinition("short", "Short") }, castings);
            new CastingPlanRepository(dir).Save(CastingPlanProfile.FromDocument(document));
            return new CastingWorkspaceSession(dir, document.CampaignId, dispatch);
        }

        private static string ProblemFiles(string dir)
        {
            return string.Join("\n", Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => path + "|" + File.GetLastWriteTimeUtc(path).Ticks + "|" +
                    Convert.ToBase64String(File.ReadAllBytes(path))).ToArray());
        }

        private static void TestProblemContract(string root)
        {
            string dir;
            CastingWorkspaceSession session = ProblemSession(root, "contract", new[] {
                ProblemCasting("zzz-earliest", 0, source: GraphSourceB, state: CastingAuthoringState.Ready),
                ProblemCasting("aaa-unresolved", 1, caster: null),
                ProblemCasting("middle-draft", 2, source: GraphSourceD, target: "unit-t2"),
                ProblemCasting("disabled", 3, state: CastingAuthoringState.Disabled),
                ProblemCasting("active", 4, source: GraphSourceD, state: CastingAuthoringState.Ready),
                ProblemCasting("ready", 5, source: GraphSourceD, state: CastingAuthoringState.Ready, target: "unit-t3")
            }, out dir);
            CastingWorkspaceInputs baseline = GraphInputs(bardLevel2: 0);
            var inputs = new CastingWorkspaceInputs(baseline.Snapshot, baseline.ProviderOptions,
                baseline.EffectsBySource, baseline.Enhancements, liveEffects: LiveEffects(
                    On("unit-t1", "graph-d", 5000, 20, 0)));
            ExplicitCastingPlan plan = session.CompileForRuntime(inputs, "long");
            var gate = new CastingExecutionGate();
            CastingApplyDecision ordinary = gate.Evaluate(plan, CastingApplyMode.Ordinary, "long");
            Expect(!ordinary.Allowed && ordinary.BlockingCastings.Select(value => value.CastingId)
                .SequenceEqual(new[] { "zzz-earliest", "aaa-unresolved", "middle-draft" }),
                "structured blockers lost execution order or included disabled/active omissions");
            foreach (CastingBlocker blocker in ordinary.BlockingCastings)
            {
                ResolvedCasting casting = plan.CastingById(blocker.CastingId);
                string[] expected = casting.ReadinessReasons.Count == 0
                    ? new[] { casting.Readiness == ResolvedCastingReadiness.Draft
                        ? "unresolved-saved-request" : "blocked" } : casting.ReadinessReasons.ToArray();
                Expect(blocker.RoutineId == "long" && blocker.Reasons.SequenceEqual(expected),
                    "structured readiness reasons differ for " + blocker.CastingId + ": expected=" +
                    string.Join("|", expected) + ";actual=" + string.Join("|", blocker.Reasons));
            }
            Expect(ordinary.Omissions.Any(value => value.CastingId == "disabled") &&
                ordinary.Omissions.Any(value => value.CastingId == "active"), "nonblocking omissions disappeared");
            CastingApplyDecision readyOnly = gate.Evaluate(plan, CastingApplyMode.ReadyCastsOnly, "long");
            Expect(readyOnly.Allowed && readyOnly.BlockingCastings.Count == 0 &&
                readyOnly.ExecutableCastingIds.SequenceEqual(new[] { "ready" }) &&
                readyOnly.Omissions.Count == ordinary.Omissions.Count, "Ready Casts Only policy changed");
            WorkspaceApplyResult result = session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            Expect(result.BlockingCastings.SequenceEqual(result.GateDecision.BlockingCastings),
                "workspace result lost structured identities");
            Expect(result.GateDecision.BlockingReasons.Count == 3, "text diagnostic contract changed");
            Expect(CastingRunPresentation.OpensPlanner(new WorkspaceApplyResult(false,
                "unclassified-presentation", ordinary, null)),
                "structured HUD opening depended on refusal text");
            Expect(!CastingRunPresentation.OpensPlanner(new WorkspaceApplyResult(false,
                "party-state-unavailable", null, null)), "an unknown global refusal opened the planner");
        }

        private static void TestProblemNavigation(string root)
        {
            string dir;
            var dispatch = new DisabledCastingDispatchBoundary();
            CastingWorkspaceSession session = ProblemSession(root, "navigation", new[] {
                ProblemCasting("z", 0, source: GraphSourceB),
                ProblemCasting("a", 1), ProblemCasting("m", 2, source: GraphSourceD)
            }, out dir, dispatch);
            CastingWorkspaceInputs inputs = GraphInputs();
            session.BuildGraph(inputs);
            session.PresentForReview(inputs);
            string signature = session.DocumentIntentSignature();
            string files = ProblemFiles(dir);
            CastingReviewStatus review = session.ReviewStatusFor("long");
            Expect(!session.CanUndo, "fixture unexpectedly has Undo history");
            WorkspaceApplyResult refusal = session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            Expect(!refusal.Allowed && session.EditingFocusCastingId == "z" &&
                session.SelectedSourceId == GraphSourceB && session.SelectedRoutineId == "long",
                "ordinary Run did not focus the earliest blocked casting and its buff/routine");
            Expect(!session.ProblemNavigation.CanPrevious && session.ProblemNavigation.CanNext &&
                session.ProblemNavigation.Position == 1 && session.ProblemNavigation.Count == 3,
                "first problem boundary incorrect");
            Expect(!session.NavigateProblem(-1) && session.NavigateProblem(1), "first/next controls incorrect");
            Expect(session.EditingFocusCastingId == "a" && session.SelectedSourceId == GraphSourceA,
                "next did not use canonical graph focus");
            Expect(session.NavigateProblem(1) && session.EditingFocusCastingId == "m" &&
                !session.ProblemNavigation.CanNext && !session.NavigateProblem(1), "last boundary incorrect");
            Expect(session.NavigateProblem(-1) && session.EditingFocusCastingId == "a" &&
                session.ProblemNavigation.Position == 2, "previous did not return to problem 2");
            session.BuildGraph(inputs);
            session.PresentForReview(inputs);
            Expect(session.DocumentIntentSignature() == signature && ProblemFiles(dir) == files &&
                !session.CanUndo && session.ReviewStatusFor("long") == review &&
                dispatch.RecordedSubmissions.Count == 0, "inspection authored, saved, authorized or dispatched");
        }

        private static void TestProblemUnavailableSource(string root)
        {
            string dir;
            CastingWorkspaceSession session = ProblemSession(root, "unavailable",
                new[] { ProblemCasting("missing", 0, state: CastingAuthoringState.Ready) }, out dir);
            CastingWorkspaceInputs baseline = GraphInputs();
            var options = baseline.ProviderOptions.Where(option =>
                !option.Provider.Key.Ability.Equals(GraphAbilityA)).ToList();
            var effects = baseline.EffectsBySource.Where(pair =>
                pair.Key != GraphSourceA && pair.Key != GraphAbilityA.Canonical)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            var snapshot = new PartyProviderSnapshot(baseline.Snapshot.Units,
                options.Select(option => option.Provider), baseline.Snapshot.ResourcePools);
            var inputs = new CastingWorkspaceInputs(snapshot, options, effects, baseline.Enhancements);
            string signature = session.DocumentIntentSignature();
            string files = ProblemFiles(dir);
            WorkspaceApplyResult result = session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            CastingGraphView view = session.BuildGraph(inputs);
            Expect(result.BlockingCastings.Single().CastingId == "missing" &&
                session.EditingFocusCastingId == "missing", "missing source did not produce casting focus");
            CastingGraphCatalogueEntry selected = view.Catalogue.SingleOrDefault(entry => entry.Selected);
            Expect(selected != null && selected.SourceId == GraphSourceA &&
                selected.Label == "Unavailable saved buff" &&
                CastingGraphLayout.Compute(view, null).Chips.Any(chip => chip.CastingId == "missing"),
                "unavailable blocked source has no catalogue/card reveal targets");
            Expect(session.DocumentIntentSignature() == signature && ProblemFiles(dir) == files &&
                !session.CanUndo, "the missing-source reveal fallback authored or saved intent");
        }

        private static void TestProblemUnresolved(string root)
        {
            string dir;
            PlannedCasting imported = new PlannedCasting("imported-draft", "long", 0,
                GraphSourceA, GraphAbilityA, null, null, CastingTargetMode.DirectTarget, "unit-t1",
                null, null, null, null, ExistingEffectPolicy.SkipAlreadyActive, null,
                CastingAuthoringState.Draft, new MigrationProvenance("legacy", 5, "long",
                    "imported", "unit-t1", new[] { "automatic-caster-pending-review" }));
            CastingWorkspaceSession session = ProblemSession(root, "unresolved", new[] { imported }, out dir);
            string signature = session.DocumentIntentSignature();
            WorkspaceApplyResult result = session.Apply(CastingApplyMode.Ordinary, "long", GraphInputs());
            CastingGraphView view = session.BuildGraph(GraphInputs());
            Expect(result.BlockingCastings.Single().CastingId == imported.CastingId &&
                view.Inspector.CasterName == "No caster chosen" && view.Inspector.Reasons.Contains("no caster chosen") &&
                view.Castings.Single().CasterUnitId == null && session.DocumentIntentSignature() == signature,
                "an unresolved saved Draft was hidden, guessed or mutated");
            Expect(session.LastAttemptMessage.Contains("no caster chosen") &&
                session.LastAttemptMessage.Contains("Problem 1 of 1"), "actionable refusal was not presented");
        }

        private static void TestProblemRepair(string root)
        {
            string dir;
            CastingWorkspaceSession session = ProblemSession(root, "repair",
                new[] { ProblemCasting("first", 0), ProblemCasting("second", 1) }, out dir);
            CastingWorkspaceInputs inputs = GraphInputs();
            session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            Expect(session.SetFocusedCastingState(CastingAuthoringState.Ready).Applied, "first repair refused");
            session.BuildGraph(inputs);
            Expect(session.ProblemNavigation.Count == 1 && session.ProblemNavigation.Position == 1 &&
                session.EditingFocusCastingId == "second" &&
                session.ProblemNavigation.PendingRevealCastingId == "second", "repair did not advance and renumber");
            Expect(session.SetFocusedCastingState(CastingAuthoringState.Ready).Applied, "second repair refused");
            session.BuildGraph(inputs);
            Expect(!session.ProblemNavigation.Active && session.ProblemNavigation.PendingRevealCastingId == null &&
                session.LastAttemptMessage == "Long is ready.", "last repair left stale problems");
            session.BuildGraph(inputs);
            Expect(!session.ProblemNavigation.Active, "a refresh resurrected repaired blockers");
        }

        private static void TestProblemLastRepair(string root)
        {
            string dir;
            CastingWorkspaceSession session = ProblemSession(root, "last",
                new[] { ProblemCasting("first", 0), ProblemCasting("last", 1) }, out dir);
            session.Apply(CastingApplyMode.Ordinary, "long", GraphInputs());
            session.NavigateProblem(1);
            session.SetFocusedCastingState(CastingAuthoringState.Ready);
            session.BuildGraph(GraphInputs());
            Expect(session.EditingFocusCastingId == "first" && session.ProblemNavigation.Position == 1 &&
                session.ProblemNavigation.Count == 1, "repairing the last problem did not select the new last");

            // Resource changes can introduce new blockers during the repair.
            // The former last problem must still move to the new last.
            session = ProblemSession(root, "last-with-new-blockers", new[] {
                ProblemCasting("first", 0), ProblemCasting("last", 1),
                ProblemCasting("new-middle", 2, state: CastingAuthoringState.Ready, target: "unit-t2"),
                ProblemCasting("new-last", 3, state: CastingAuthoringState.Ready, target: "unit-t3")
            }, out dir);
            session.Apply(CastingApplyMode.Ordinary, "long", GraphInputs());
            Expect(session.ProblemNavigation.Count == 2 && session.NavigateProblem(1),
                "changing-resource fixture did not start at the last of two problems");
            Expect(session.SetFocusedCastingState(CastingAuthoringState.Disabled).Applied,
                "explicit last-problem repair was refused");
            session.BuildGraph(GraphInputs(bardLevel2: 0));
            Expect(session.EditingFocusCastingId == "new-last" &&
                session.ProblemNavigation.Position == 3 && session.ProblemNavigation.Count == 3,
                "repairing the last problem with newly blocked castings did not select the new last");
        }

        private static void TestProblemManualExit(string root)
        {
            Action<CastingWorkspaceSession>[] selections = {
                session => session.SelectGraphBuff(GraphSourceD),
                session => session.SelectRoutine("short"),
                session => session.SelectGraphCaster("unit-cleric"),
                session => session.FocusGraphCasting("second"),
                session => session.ClearGraphFocus()
            };
            for (int index = 0; index < selections.Length; index++)
            {
                string dir;
                CastingWorkspaceSession session = ProblemSession(root, "manual-" + index,
                    new[] { ProblemCasting("first", 0), ProblemCasting("second", 1) }, out dir);
                session.Apply(CastingApplyMode.Ordinary, "long", GraphInputs());
                string signature = session.DocumentIntentSignature();
                selections[index](session);
                session.BuildGraph(GraphInputs());
                Expect(!session.ProblemNavigation.Active && session.ProblemNavigation.PendingRevealCastingId == null &&
                    session.DocumentIntentSignature() == signature && !session.CanUndo,
                    "manual selection trapped the player or authored intent");
            }
        }

        private static void TestProblemGlobalRefusal(string root)
        {
            string dir;
            CastingWorkspaceSession session = ProblemSession(root, "global",
                new[] { ProblemCasting("ready", 0, state: CastingAuthoringState.Ready) }, out dir);
            string profile = new CastingPlanRepository(dir).GetProfilePath("problem-campaign");
            File.Delete(profile);
            Directory.CreateDirectory(profile);
            session.SetExecutionMode("animated");
            WorkspaceApplyResult result = session.Apply(CastingApplyMode.Ordinary, "long", GraphInputs());
            Expect(result.ReviewReason.StartsWith("persistence-failed:", StringComparison.Ordinal) &&
                result.BlockingCastings.Count == 0 && !session.ProblemNavigation.Active &&
                session.EditingFocusCastingId == null && session.ProblemNavigation.PendingRevealCastingId == null,
                "global persistence refusal fabricated casting focus");
            Expect(CastingRunPresentation.DescribeRefusal("Long", result, session).Contains("not saved"),
                "global persistence explanation disappeared");
        }

        private static void TestProblemRoutes(string root)
        {
            foreach (string routine in new[] { "long", "important", "short" })
            {
                string plannerDir, hudDir;
                PlannedCasting[] castings = { ProblemCasting("blocked", 0, routine: routine) };
                CastingWorkspaceSession planner = ProblemSession(root, "planner-" + routine, castings, out plannerDir);
                CastingWorkspaceSession hud = ProblemSession(root, "hud-" + routine, castings, out hudDir);
                planner.SelectRoutine(routine);
                planner.BuildGraph(GraphInputs());
                planner.Apply(CastingApplyMode.Ordinary, planner.SelectedRoutineId, GraphInputs());
                WorkspaceApplyResult hudResult = hud.Apply(CastingApplyMode.Ordinary, routine, GraphInputs());
                CastingGraphView opened = hud.BuildGraph(GraphInputs());
                Expect(CastingRunPresentation.OpensPlanner(hudResult) &&
                    opened.SelectedRoutineId == routine && opened.FocusedCastingId == planner.EditingFocusCastingId &&
                    opened.SelectedSourceId == planner.SelectedSourceId &&
                    hud.ProblemNavigation.PendingRevealCastingId == planner.ProblemNavigation.PendingRevealCastingId &&
                    hud.LastAttemptMessage == planner.LastAttemptMessage,
                    "HUD and planner attempts produce different focus/reveal intent: " + routine);
            }
        }

        private sealed class ProblemSuccessfulDispatch : ICastingDispatchBoundary
        {
            public string DispositionReason { get { return string.Empty; } }
            public int Calls;
            public CastingDispatchOutcome Submit(ExplicitCastingPlan plan, CastingApplyDecision decision,
                string scopeRoutineId, ExplicitStepConversion projection)
            {
                Calls++;
                return new CastingDispatchOutcome(true, string.Empty, decision.ExecutableCastingIds);
            }
        }

        private static void TestProblemReadyOnly(string root)
        {
            string dir;
            var dispatch = new ProblemSuccessfulDispatch();
            CastingWorkspaceSession session = ProblemSession(root, "ready-only", new[] {
                ProblemCasting("draft", 0), ProblemCasting("ready", 1, state: CastingAuthoringState.Ready)
            }, out dir, dispatch);
            session.Apply(CastingApplyMode.Ordinary, "long", GraphInputs());
            string signature = session.DocumentIntentSignature();
            WorkspaceApplyResult fallback = session.Apply(CastingApplyMode.ReadyCastsOnly, "long", GraphInputs());
            Expect(fallback.Allowed && fallback.Dispatch.Submitted && dispatch.Calls == 1 &&
                fallback.GateDecision.ExecutableCastingIds.SequenceEqual(new[] { "ready" }) &&
                fallback.GateDecision.Omissions.Single().CastingId == "draft" &&
                !session.ProblemNavigation.Active && session.ProblemNavigation.PendingRevealCastingId == null &&
                session.DocumentIntentSignature() == signature, "ready subset or successful-run cleanup changed");
            session.BuildGraph(GraphInputs());
            Expect(!session.ProblemNavigation.Active, "successful submission resurrected stale navigation");
        }

        private static void TestProblemOneShot(string root)
        {
            string dir;
            CastingWorkspaceSession session = ProblemSession(root, "one-shot",
                new[] { ProblemCasting("first", 0), ProblemCasting("second", 1) }, out dir);
            session.Apply(CastingApplyMode.Ordinary, "long", GraphInputs());
            session.ProblemNavigation.CompleteReveal("different");
            Expect(session.ProblemNavigation.PendingRevealCastingId == "first", "wrong target consumed reveal");
            session.ProblemNavigation.CompleteReveal("first");
            session.BuildGraph(GraphInputs());
            session.BuildGraph(GraphInputs());
            Expect(session.ProblemNavigation.PendingRevealCastingId == null, "ordinary refresh reclaimed user scroll");
            session.NavigateProblem(1);
            Expect(session.ProblemNavigation.PendingRevealCastingId == "second", "next did not request one reveal");
            session.ProblemNavigation.CompleteReveal("second");
            session.BuildGraph(GraphInputs());
            Expect(session.ProblemNavigation.PendingRevealCastingId == null, "next reveal was not one-shot");
            session.Apply(CastingApplyMode.Ordinary, "long", GraphInputs());
            Expect(session.ProblemNavigation.PendingRevealCastingId == "first", "new attempt did not reset to first");
        }

        private static void TestProblemGlobalRefresh(string root)
        {
            string dir;
            CastingWorkspaceSession session = ProblemSession(root, "global-refresh",
                new[] { ProblemCasting("draft", 0) }, out dir);
            session.Apply(CastingApplyMode.Ordinary, "long", GraphInputs());
            string signature = session.DocumentIntentSignature();
            string files = ProblemFiles(dir);
            session.RecordGlobalRefusal("Long was not cast: the party state could not be refreshed (fixture failure).");
            session.BuildGraph(GraphInputs());
            Expect(!session.ProblemNavigation.Active && session.EditingFocusCastingId == null &&
                session.ProblemNavigation.PendingRevealCastingId == null &&
                session.LastAttemptMessage.Contains("fixture failure") &&
                session.DocumentIntentSignature() == signature && ProblemFiles(dir) == files && !session.CanUndo,
                "stale problem focus replaced a global refresh failure or authored state");
        }

        private static void TestProblemExistingUndo(string root)
        {
            string dir;
            CastingWorkspaceSession session = ProblemSession(root, "undo",
                new[] { ProblemCasting("first", 0) }, out dir);
            Expect(session.AddCastingForRuntime(ProblemCasting("second", 1)).Applied && session.CanUndo,
                "fixture did not create an authored Undo entry");
            session.Apply(CastingApplyMode.Ordinary, "long", GraphInputs());
            session.NavigateProblem(1);
            session.NavigateProblem(-1);
            session.BuildGraph(GraphInputs());
            Expect(session.CanUndo && session.Undo() &&
                session.Document.Castings.Select(casting => casting.CastingId).SequenceEqual(new[] { "first" }) &&
                !session.CanUndo, "problem navigation changed the existing Undo stack");
        }

        private static RuntimeTesting.ProblemNavigationRecord ValidProblemPhysicalRecord()
        {
            var record = new RuntimeTesting.ProblemNavigationRecord {
                PlannerClosedBeforeHud = true, ColdSessionBeforeHud = true, PlannerOpenedByHud = true,
                GraphOverflow = true, GraphScrollBefore = 1, GraphScrollAfter = 0.2f,
                SourceId = "buff", FirstCastingId = "first", SecondCastingId = "last",
                DocumentBefore = "intent", DocumentAfter = "intent",
                ProfileBeforeSha256 = "stored", ProfileAfterSha256 = "stored",
                ResourcesBefore = "pools", ResourcesAfter = "pools", EffectsBefore = "effects", EffectsAfter = "effects",
                EscapeLeftFocus = true, PlannerClosedAfterEscape = true, InputLeaseReleased = true,
                ScreenWidth = 1920, ScreenHeight = 1080
            };
            record.Acknowledged.AddRange(RuntimeTesting.ProblemNavigationRecord.Actions);
            for (int index = 0; index < 3; index++)
            {
                int position = index == 1 ? 2 : 1;
                var observation = new RuntimeTesting.ProblemScreenObservation {
                    CastingId = index == 1 ? "last" : "first", RoutineId = "long", SourceId = "buff",
                    Position = position, Count = 2, ChipX = 600, ChipY = 400, ChipVisibleFraction = 1,
                    CatalogueX = 100, CatalogueY = 500, RoutineX = 500, RoutineY = 800,
                    InspectorScroll = 1, PreviousEnabled = position > 1, NextEnabled = position < 2,
                    InspectorText = "Problem " + position + " of 2. " + WorkspaceReasonText.Describe("caster-unresolved"),
                    FooterText = "Problem " + position + " of 2", DocumentSignature = "intent"
                };
                observation.MachineReasons.Add("caster-unresolved");
                record.Observations.Add(observation);
            }
            return record;
        }

        private static void TestProblemPhysicalJudgment()
        {
            Expect(ValidProblemPhysicalRecord().Violations().Count == 0, "valid physical record failed");
            Action<RuntimeTesting.ProblemNavigationRecord>[] invalid = {
                record => record.Observations[0].ChipX = null,
                record => record.Observations[1].ChipVisibleFraction = 0.02f,
                record => record.FirstChipVisibleBeforeReveal = true,
                record => record.GraphScrollAfter = record.GraphScrollBefore,
                record => record.Observations[0].InspectorText = "generic planner",
                record => record.DocumentAfter = "changed",
                record => record.ProfileAfterSha256 = "written",
                record => record.DispatchAttempts = 1,
                record => record.RunsStarted = 1,
                record => record.ResourcesAfter = "spent",
                record => record.EffectsAfter = "new effect",
                record => record.AcceptedDigestAfter = "approved",
                record => record.Acknowledged.Remove("problem-moon"),
                record => record.Observations[2].Position = 2,
                record => record.InputLeaseReleased = false
            };
            foreach (Action<RuntimeTesting.ProblemNavigationRecord> corrupt in invalid)
            {
                RuntimeTesting.ProblemNavigationRecord record = ValidProblemPhysicalRecord();
                corrupt(record);
                Expect(record.Violations().Count != 0, "incomplete or side-effecting physical evidence passed");
            }
        }

        private static void TestProblemRevealGeometry()
        {
            Expect(CastingProblemScrollReveal.VerticalOffset(-200, 200, -50, 50, 12) == 0,
                "visible card was scrolled unnecessarily");
            Expect(CastingProblemScrollReveal.VerticalOffset(-200, 200, -500, -444, 12) == 312,
                "below-viewport card did not move by its measured distance plus margin");
            Expect(CastingProblemScrollReveal.VerticalOffset(-200, 200, 300, 356, 12) == -168,
                "above-viewport card did not move minimally");
            Expect(CastingProblemScrollReveal.VerticalOffset(-100, 100, -500, -444, 12) == 412,
                "resize retained a guessed old percentage");
            Expect(CastingProblemScrollReveal.VerticalOffset(-100, 100, -400, 300, 12) == -212,
                "oversize card did not reveal its top");
        }
    }
}
