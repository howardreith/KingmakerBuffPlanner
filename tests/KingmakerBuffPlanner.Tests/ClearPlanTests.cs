using System;
using System.IO;
using System.Linq;
using System.Reflection;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        private static void RunClearPlanTests(string root)
        {
            Run("clear-plan-authoritative-scope-and-isolation", () => TestClearPlanScope(root));
            Run("clear-plan-open-cancel-empty-and-repeat-are-read-only", () => TestClearPlanCancel(root));
            Run("clear-plan-one-announcement-save-and-exact-undo", () => TestClearPlanUndo(root));
            Run("clear-plan-stale-edit-reload-settings-and-owner", () => TestClearPlanStale(root));
            Run("clear-plan-busy-at-confirmation-refuses", () => TestClearPlanBusy(root));
            Run("clear-plan-save-failure-and-retry", () => TestClearPlanSaveFailure(root));
            Run("clear-plan-recovered-primary-remains-protected", () => TestClearPlanRecovery(root));
            Run("clear-plan-continued-graph-editing-keeps-issued-identities", () => TestClearPlanContinue(root));
            Run("clear-plan-modal-neutral-edges-and-scoped-focus", TestClearPlanInput);
        }

        private static CastingWorkspaceSession ClearFixture(string root, string name, out string dir)
        {
            dir = Path.Combine(root, "clear-plan-" + name);
            Directory.CreateDirectory(dir);
            var document = new CastingPlanDocument("clear-campaign", new[]
            {
                new RoutineDefinition("long", "Long"), new RoutineDefinition("short", "Short"),
                new RoutineDefinition("important", "Important"), new RoutineDefinition("custom", "Custom")
            }, new[]
            {
                DirectCasting("cast-7", "short", "unit-cleric", "unit-t1", "missing-source", LifecycleAbility(),
                    new[] { new AuthoredEnhancementSelection("exact-enhancement", true, null) }),
                DirectCasting("cast-8", "important", null, "unit-t2", "unresolved", LifecycleAbility(), null, null, CastingAuthoringState.Draft),
                DirectCasting("cast-9", "custom", "unit-cleric", "unit-t1", "exact-item",
                    new AbilityKey("item-buff", null, 0, SourceKind.Item, "exact-item-token"), null, null, CastingAuthoringState.Disabled)
            }, new[] { "kept import metadata" }, new[] { "kept import metadata" });
            new CastingPlanRepository(dir).Save(CastingPlanProfile.FromDocument(document));
            return new CastingWorkspaceSession(dir, "clear-campaign");
        }

        private static void TestClearPlanScope(string root)
        {
            string dir;
            var session = ClearFixture(root, "scope", out dir);
            var other = new CastingWorkspaceSession(dir, "other-campaign");
            Expect(other.AddCastingForRuntime(LifecycleCasting("cast-other", "unit-t1")).Applied, "other campaign setup");
            string otherPath = new CastingPlanRepository(dir).GetProfilePath("other-campaign");
            string otherBytes = File.ReadAllText(otherPath);
            string legacy = Path.Combine(dir, "legacy-profile.json");
            File.WriteAllText(legacy, "preserved legacy data");
            session.SetExecutionMode("animated");
            session.SetAvailabilityPolicy(ExecutionProfile.CastAvailable);
            session.SelectRoutine("long");
            session.SelectBuff("a-source-with-no-visible-cards");
            Expect(session.BuildGraph(GraphInputs()).Castings.Count == 0, "the empty selected routine should hide all fixture castings");
            CastingPlanDocument before = session.Document;
            ClearPlanConfirmation confirmation = session.BeginClearPlan();
            Expect(confirmation.CastingCount == before.Castings.Count && confirmation.CastingCount == 3,
                "confirmation must count the authoritative document including unresolved and other routines");
            Expect(session.ClearPlan(confirmation).Applied && session.Document.Castings.Count == 0, "entire document was not cleared");
            Expect(session.Document.CampaignId == before.CampaignId && session.Document.Routines.SequenceEqual(before.Routines) &&
                session.Document.ImportNotices.SequenceEqual(before.ImportNotices) &&
                session.Document.AcknowledgedImportNotices.SequenceEqual(before.AcknowledgedImportNotices), "document metadata changed");
            Expect(session.ExecutionMode == "animated" && session.AvailabilityPolicy == ExecutionProfile.CastAvailable, "settings changed");
            Expect(File.ReadAllText(otherPath) == otherBytes && File.ReadAllText(legacy) == "preserved legacy data", "scope leaked to another profile");
            var fresh = new CastingWorkspaceSession(dir, session.CampaignId, null, null,
                () => { throw new InvalidOperationException("An empty saved document must not import legacy data."); });
            Expect(fresh.Document.Castings.Count == 0 && fresh.LoadStatus == CastingPlanLoadStatus.Loaded && !fresh.CanUndo,
                "empty plan did not reopen durably, or Undo was implied across restart");
            Expect(session.DispatchRefusalsForRuntime.Count == 0, "authoring dispatched execution");
        }

        private static void TestClearPlanCancel(string root)
        {
            string dir;
            var session = ClearFixture(root, "cancel", out dir);
            string path = new CastingPlanRepository(dir).GetProfilePath(session.CampaignId);
            string bytes = File.ReadAllText(path);
            DateTime write = File.GetLastWriteTimeUtc(path);
            var before = session.Document;
            var token = session.BeginClearPlan();
            Expect(ReferenceEquals(token, session.BeginClearPlan()), "more than one pending confirmation");
            session.CancelClearPlan();
            Expect(!session.ClearPlan(token).Applied && ReferenceEquals(before, session.Document) && !session.CanUndo &&
                File.ReadAllText(path) == bytes && File.GetLastWriteTimeUtc(path) == write, "opening/cancel/stale delivery mutated or saved");
            Expect(session.ClearPlan(session.BeginClearPlan()).Applied, "clear failed");
            bytes = File.ReadAllText(path); write = File.GetLastWriteTimeUtc(path);
            Expect(!session.CanClearPlan && session.BeginClearPlan() == null && !session.ClearPlan(token).Applied &&
                File.ReadAllText(path) == bytes && File.GetLastWriteTimeUtc(path) == write, "empty/repeat saved or edited");
            Expect(session.Undo() && ReferenceEquals(before, session.Document) && !session.CanUndo, "duplicate command added Undo history");
        }

        private static void TestClearPlanUndo(string root)
        {
            string dir;
            var session = ClearFixture(root, "undo", out dir);
            Expect(session.AddCastingForRuntime(DirectCasting("cast-10", "long", "unit-cleric", "unit-t3", "other", LifecycleAbility())).Applied, "earlier edit");
            var before = session.Document;
            string signature = session.DocumentIntentSignature();
            string path = new CastingPlanRepository(dir).GetProfilePath(session.CampaignId);
            string bytes = File.ReadAllText(path);
            var authoring = (CastingAuthoringService)typeof(CastingWorkspaceSession).GetField("_authoring",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            int announcements = 0;
            authoring.DocumentChanged += (document, revision) => announcements++;
            var confirmation = session.BeginClearPlan();
            Expect(confirmation.CastingCount == 4, "count omitted one of the four routines");
            Expect(session.ClearPlan(confirmation).Applied && announcements == 1, "clear was not one edit/announcement");
            Expect(File.ReadAllText(path + ".bak1") == bytes, "more than one autosave rotated the populated backup away");
            Expect(session.Undo() && announcements == 2 && ReferenceEquals(before, session.Document), "Undo did not restore exact immutable authored document");
            Expect(new CastingWorkspaceSession(dir, session.CampaignId).DocumentIntentSignature() == signature, "Undo restoration did not persist");
            Expect(session.Undo() && session.Document.Castings.All(c => c.CastingId != "cast-10"), "earlier Undo history was lost");
        }

        private static void TestClearPlanStale(string root)
        {
            string dir;
            var session = ClearFixture(root, "stale", out dir);
            var token = session.BeginClearPlan();
            Expect(session.AddCastingForRuntime(LifecycleCasting("cast-20", "unit-t2")).Applied, "edit setup");
            Expect(!session.ClearPlan(token).Applied && session.Document.Castings.Count == 4, "expanded plan accepted old confirmation");
            session.Undo();
            Expect(!session.ClearPlan(token).Applied, "edit then Undo revived stale confirmation");
            token = session.BeginClearPlan(); session.Reload();
            Expect(!session.ClearPlan(token).Applied, "replacement with restarted revision accepted old token");
            token = session.BeginClearPlan(); session.SetExecutionMode("animated");
            Expect(!session.ClearPlan(token).Applied, "settings edit did not invalidate confirmation");
            token = session.BeginClearPlan();
            var other = new CastingWorkspaceSession(dir, session.CampaignId);
            Expect(!other.ClearPlan(token).Applied, "different session accepted token");
            session.CancelClearPlan(); // the owning screen's Dispose/context teardown path
            Expect(!session.ClearPlan(token).Applied && session.Document.Castings.Count == 3, "ended owner retained approval");
        }

        private static void TestClearPlanBusy(string root)
        {
            string dir;
            var session = ClearFixture(root, "busy", out dir);
            var token = session.BeginClearPlan();
            var inputs = GraphInputs();
            var busy = new CastingWorkspaceInputs(inputs.Snapshot, inputs.ProviderOptions, inputs.EffectsBySource,
                inputs.Enhancements, runActive: true);
            Expect(!session.ClearPlan(token, busy).Applied && session.Document.Castings.Count == 3 &&
                !session.CanClearPlan && session.BeginClearPlan(busy) == null && !session.CanUndo, "busy clear edited or created history");
            Expect(session.BeginClearPlan(inputs) != null, "editor did not recover after run ended");
        }

        private static void TestClearPlanSaveFailure(string root)
        {
            string dir;
            var session = ClearFixture(root, "save-failure", out dir);
            string path = new CastingPlanRepository(dir).GetProfilePath(session.CampaignId);
            string bytes = File.ReadAllText(path);
            using (var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Expect(session.ClearPlan(session.BeginClearPlan()).Applied && session.Document.Castings.Count == 0, "locked file prevented authored edit");
                Expect(session.IsDirty && session.AutosaveStatus.StartsWith("save-failed", StringComparison.Ordinal) &&
                    File.ReadAllText(path) == bytes, "failed autosave was misreported or damaged durable plan");
                Expect(session.CaptureRecovery().Document.Castings.Count == 0, "dirty clear lost its recovery owner");
            }
            Expect(session.RetryFailedSave() && !session.IsDirty && new CastingWorkspaceSession(dir, session.CampaignId).Document.Castings.Count == 0,
                "retry did not persist the empty document");
        }

        private static void TestClearPlanRecovery(string root)
        {
            string dir;
            var session = ClearFixture(root, "recovery", out dir);
            session.AddCastingForRuntime(LifecycleCasting("cast-20", "unit-t1"));
            string path = new CastingPlanRepository(dir).GetProfilePath(session.CampaignId);
            File.WriteAllText(path, "{ torn");
            var recovered = new CastingWorkspaceSession(dir, session.CampaignId);
            Expect(recovered.SavesRefused && recovered.Document.Castings.Count > 0, "fixture did not recover protected backup");
            Expect(recovered.ClearPlan(recovered.BeginClearPlan()).Applied && recovered.IsDirty && recovered.SavesRefused &&
                recovered.AutosaveStatus != "saved" && File.ReadAllText(path) == "{ torn", "clear bypassed recovery protection");
        }

        private static void TestClearPlanContinue(string root)
        {
            CastingWorkspaceInputs inputs;
            var session = GraphSession(root, "clear-continue", out inputs);
            string prior = GraphAdd(session, inputs, GraphSourceA, "unit-cleric", null, "unit-t1");
            session.FocusGraphCasting(prior);
            Expect(session.BeginChangeRecipient().Applied, "armed gesture fixture");
            var confirmation = session.BeginClearPlan(inputs);
            Expect(confirmation.CastingCount == 1 && confirmation.Message.Contains("1 planned casting from") &&
                !confirmation.Message.Contains("1 planned castings"), "singular confirmation was incorrect");
            Expect(session.ClearPlan(confirmation).Applied && session.EditingFocusCastingId == null &&
                session.GraphEditIntent == CastingGraphEditIntent.None, "deleted row or armed gesture survived");
            var empty = session.BuildGraph(inputs);
            Expect(empty.Castings.Count == 0, "empty graph retained cards");
            string next = GraphAdd(session, inputs, GraphSourceA, "unit-cleric", null, "unit-t1");
            Expect(next != prior && session.Document.Castings.Count == 1 && session.BuildGraph(inputs).Castings.Count == 1,
                "continued editing reused an issued identity or retained stale cards");
        }

        private static void TestClearPlanInput()
        {
            var gate = new WorkspaceModalInputGate();
            gate.Open(10);
            for (int frame = 10; frame < 30; frame++)
            { gate.Observe(false, frame); Expect(!gate.Take(frame), "held opening confirm accepted"); }
            gate.Observe(true, 30);
            Expect(!gate.Take(30) && gate.Take(31) && !gate.Take(31) && !gate.Take(32), "neutral/fresh edge or repeat guard failed");
            Expect(!gate.AllowsCommand(ControllerWorkspaceCommand.NextRoutine) && !gate.AllowsCommand(ControllerWorkspaceCommand.PreviousRoutine) &&
                !gate.AllowsCommand(ControllerWorkspaceCommand.Inspect), "modal allowed underlying command");
            var focus = new ControllerWorkspaceFocus();
            int cancels = 0, clears = 0;
            focus.Replace(new[]
            {
                new ControllerFocusItem("clear/cancel", "ClearPlan", "Cancel", true, () => cancels++, bounds: new ControllerFocusBounds(0, 0, 100, 40)),
                new ControllerFocusItem("clear/confirm", "ClearPlan", "Clear plan", true, () => clears++, bounds: new ControllerFocusBounds(120, 0, 240, 40))
            });
            focus.Restore("clear/cancel");
            focus.Confirm(); Expect(cancels == 1 && clears == 0, "initial focus was not Cancel");
            focus.Move(ControllerWorkspaceCommand.Right); focus.Confirm();
            Expect(clears == 1 && focus.Items.Count == 2, "confirm/focus escaped modal scope");
            focus.Move(ControllerWorkspaceCommand.Down);
            Expect(focus.Key.StartsWith("clear/", StringComparison.Ordinal), "focus escaped modal");
            gate.Close(32);
            for (int frame = 32; frame < 40; frame++)
            { gate.Observe(false, frame); Expect(gate.BlocksBackground && !gate.AllowsCommand(ControllerWorkspaceCommand.Confirm), "closing edge leaked"); }
            gate.Observe(true, 40); gate.Observe(true, 41);
            Expect(!gate.BlocksBackground, "background never regained input");
            gate.Open(42);
            Expect(!gate.Take(42) && !gate.Take(43), "reopening retained prior approval edge");
        }
    }
}
