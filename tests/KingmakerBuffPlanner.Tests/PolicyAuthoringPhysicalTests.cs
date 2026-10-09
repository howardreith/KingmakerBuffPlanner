using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.RuntimeTesting;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        // 0.4.0 native evidence for WP4 (combat refusal) and WP3 (direct
        // graph manipulation): the physical scenario's two new expectations,
        // their requests and their Unity-free judgements.
        private static void RunPolicyAuthoringPhysicalTests(string root)
        {
            Run("physical-combat-and-authoring-requests", () => TestPolicyAuthoringRequests(root));
            Run("physical-combat-judgement", TestPhysicalCombatJudgement);
            Run("physical-authoring-judgement", TestPhysicalAuthoringJudgement);
        }

        private static void TestPolicyAuthoringRequests(string root)
        {
            foreach (string expectation in new[] { "combat", "authoring" })
            {
                string path = WriteRequest(root, "physical-" + expectation, o =>
                {
                    o["scenario"] = "live-workspace-physical";
                    o["parameters"] = new Dictionary<string, object>
                    {
                        { "workingSaveName", "KBP_AUTOMATION_WORKING" },
                        { "workingFileName", "Manual_305_KBP_AUTOMATION_WORKING.zks" },
                        { "workingSha256", new string('a', 64) },
                        { "baselineSaveName", "KBP_AUTOMATION_BASELINE" },
                        { "baselineFileName", "Manual_304_KBP_AUTOMATION_BASELINE.zks" },
                        { "baselineSha256", new string('b', 64) },
                        { "expectedGameName", "Yadmila" },
                        { "expectedGameId", "3d556254-8ba9-4e9f-8d11-755eecd0b661" },
                        { "executionMode", "animated" },
                        { "physicalExpectation", expectation }
                    };
                });
                string rejection;
                RuntimeTestRequest request = ReadProtocol(
                    new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
                if (request == null || rejection.Length != 0)
                    throw new InvalidOperationException("A valid " + expectation + " request was rejected: " + rejection);
                // Neither expectation casts: a casting allowance is refused.
                string withAllowance = WriteRequest(root, "physical-" + expectation + "-allowance", o =>
                {
                    o["scenario"] = "live-workspace-physical";
                    o["parameters"] = new Dictionary<string, object>
                    {
                        { "workingSaveName", "KBP_AUTOMATION_WORKING" },
                        { "workingFileName", "Manual_305_KBP_AUTOMATION_WORKING.zks" },
                        { "workingSha256", new string('a', 64) },
                        { "baselineSaveName", "KBP_AUTOMATION_BASELINE" },
                        { "baselineFileName", "Manual_304_KBP_AUTOMATION_BASELINE.zks" },
                        { "baselineSha256", new string('b', 64) },
                        { "expectedGameName", "Yadmila" },
                        { "expectedGameId", "3d556254-8ba9-4e9f-8d11-755eecd0b661" },
                        { "executionMode", "instant" },
                        { "physicalExpectation", expectation },
                        { "cfAllowance", "{}" }
                    };
                });
                if (ReadProtocol(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, withAllowance },
                        out rejection) != null ||
                    rejection.IndexOf("cf-allowance-only-with-cast-expectation", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("A " + expectation + " request accepted a casting allowance.");
            }
        }

        // A clean selection press (refused by the lock, nothing landed) with
        // the closing Escape's clean final state; each expectation adds its
        // own evidence.
        private static PhysicalWorkspaceRecord SelectionPressRecord(string expectation)
        {
            var record = new PhysicalWorkspaceRecord
            {
                ExpectedScreen = "1920x1080", ScreenWidth = 1920, ScreenHeight = 1080,
                OpenedPhysically = true, CastingFirst = true, MoonExpectation = expectation,
                MoonRunStarted = false, MoonWorkspaceStayedClosed = true,
                MoonRefusal = "native-submission-disabled:runtime-test-session;Refused:Casting is locked.",
                ColdSessionBeforeMoon = true, EditorNeverOpenedBeforeMoon = true,
                LongEffectBefore = false, LongEffectAfter = false,
                ImportantEffectBefore = false, ImportantEffectAfter = false,
                ModeBeforeOpen = "Default", ModeAfterClose = "Default", EscMenuOpenAfterClose = false,
                LeaseReleased = true, ClosedByEscape = true, SelectionUnchanged = true
            };
            record.SeedLongCastings.Add("seed-long-1");
            record.SeedImportantCastings.Add("seed-important-1");
            return record;
        }

        private static void TestPhysicalCombatJudgement()
        {
            Func<PhysicalWorkspaceRecord> good = () =>
            {
                PhysicalWorkspaceRecord record = SelectionPressRecord("combat");
                record.Acknowledged.AddRange(record.ExpectedCastingActions);
                record.GraphOverflow = true;
                record.GraphScrollBefore = 1f;
                record.GraphScrollAfter = 0.4f;
                record.InspectOpened = true;
                record.InspectClosedByEscape = true;
                record.DocumentSignatureBeforeBrowse = "castings=16;revision=3";
                record.DocumentSignatureAfterInspect = "castings=16;revision=3";
                WithVisibleDescription(record);
                record.CombatUnitId = "unit-1";
                record.CombatInCombatBefore = false;
                record.CombatHeldUpdates = 180;
                record.CombatAtPress = true;
                record.CombatRunStarted = false;
                record.CombatEditorOpened = false;
                record.CombatDispatchRefusals = 0;
                record.CombatRefusal = "Refused:" + CastingRunPresentation.CombatRefusalText;
                record.CombatAvailability = "99>99";
                record.CombatEffectAfter = false;
                record.CombatClearedBeforeMoon = true;
                return record;
            };
            if (!good().ExpectedCastingActions.SequenceEqual(new[] { "cf-moon-combat" }
                    .Concat(PhysicalWorkspaceRecord.CastingActions)))
                throw new InvalidOperationException("The combat run does not require its in-combat press.");
            if (good().Violations().Count != 0)
                throw new InvalidOperationException("A clean combat run was refused: " +
                    string.Join("|", good().Violations().ToArray()));
            var shapes = new Dictionary<string, Action<PhysicalWorkspaceRecord>>
            {
                { "unacknowledged:cf-moon-combat", r => r.Acknowledged.Remove("cf-moon-combat") },
                { "combat:party-already-in-combat:true", r => r.CombatInCombatBefore = true },
                { "combat:not-in-combat-at-press", r => r.CombatAtPress = false },
                { "combat:run-started", r => r.CombatRunStarted = true },
                { "combat:editor-opened", r => r.CombatEditorOpened = true },
                { "combat:reached-dispatch:1", r => r.CombatDispatchRefusals = 1 },
                { "combat:not-refused-for-combat:Refused:Casting is locked.",
                    r => r.CombatRefusal = "Refused:Casting is locked." },
                { "combat:resource-changed:99>98", r => r.CombatAvailability = "99>98" },
                { "combat:resource-changed:unread", r => r.CombatAvailability = null },
                { "combat:effect-landed", r => r.CombatEffectAfter = true },
                { "combat:not-cleared-before-moon", r => r.CombatClearedBeforeMoon = false },
                // The ordinary press after combat must still reach the lock.
                { "moon:not-refused-by-lock:combat-active", r => r.MoonRefusal = "combat-active" },
                { "moon:run-without-grant", r => r.MoonRunStarted = true }
            };
            foreach (KeyValuePair<string, Action<PhysicalWorkspaceRecord>> shape in shapes)
            {
                PhysicalWorkspaceRecord bad = good();
                shape.Value(bad);
                if (!bad.Violations().Contains(shape.Key))
                    throw new InvalidOperationException("Combat judgement missed " + shape.Key + ": " +
                        string.Join("|", bad.Violations().ToArray()));
            }
        }

        private static void TestPhysicalAuthoringJudgement()
        {
            Func<PhysicalWorkspaceRecord> good = () =>
            {
                PhysicalWorkspaceRecord record = SelectionPressRecord("authoring");
                record.Acknowledged.AddRange(PhysicalWorkspaceRecord.AuthoringActions);
                record.AuthoringRoutine = "long";
                record.AuthoringSource = "source-resistance";
                record.AuthoringCaster = "unit-linzi";
                record.AuthoringTargets = "unit-hedwirg,unit-linzi";
                record.AuthoringBuffSelected = true;
                record.AuthoringCountBefore = 16;
                record.AuthoringCountAfter = 17;
                record.AuthoringAdded = true;
                record.AuthoringRemoved = true;
                record.AuthoringReadded = true;
                record.AuthoringRetargeted = true;
                record.AuthoringUndoRestored = true;
                record.AuthoringFocusedBeforeEscape = true;
                record.AuthoringEscapeClearedFocus = true;
                record.AuthoringOpenAfterFirstEscape = true;
                record.AuthoringDurable = true;
                return record;
            };
            // Browsing and the description are not part of this run: none of
            // their evidence is required, and the document is meant to change.
            if (good().Violations().Count != 0)
                throw new InvalidOperationException("A clean authoring run was refused: " +
                    string.Join("|", good().Violations().ToArray()));
            PhysicalWorkspaceRecord withProvider = good();
            withProvider.AuthoringProviderChanged = true;
            withProvider.AuthoringProviderKeptTarget = true;
            withProvider.AuthoringProviderUndone = true;
            if (withProvider.Violations().Count != 0)
                throw new InvalidOperationException("A clean provider change was refused.");
            var shapes = new Dictionary<string, Action<PhysicalWorkspaceRecord>>
            {
                { "unacknowledged:auth-retarget", r => r.Acknowledged.Remove("auth-retarget") },
                { "unacknowledged:auth-escape-focus", r => r.Acknowledged.Remove("auth-escape-focus") },
                { "authoring:not-chosen", r => r.AuthoringTargets = "unit-hedwirg" },
                { "authoring:buff-not-selected", r => r.AuthoringBuffSelected = false },
                { "authoring:add", r => r.AuthoringAdded = false },
                { "authoring:same-portrait-remove", r => r.AuthoringRemoved = null },
                { "authoring:readd", r => r.AuthoringReadded = false },
                { "authoring:retarget", r => r.AuthoringRetargeted = false },
                { "authoring:provider-change:changed=True;kept=False;undone=True",
                    r => { r.AuthoringProviderChanged = true; r.AuthoringProviderKeptTarget = false;
                        r.AuthoringProviderUndone = true; } },
                { "authoring:provider-change:changed=False;kept=;undone=", r => r.AuthoringProviderChanged = false },
                { "authoring:undo", r => r.AuthoringUndoRestored = false },
                { "authoring:card-focus", r => r.AuthoringFocusedBeforeEscape = false },
                { "authoring:escape-did-not-leave-focus-first", r => r.AuthoringOpenAfterFirstEscape = false },
                { "authoring:not-saved", r => r.AuthoringDurable = false },
                { "authoring:count:16>18", r => r.AuthoringCountAfter = 18 },
                { "moon:not-refused-by-lock:none", r => r.MoonRefusal = null },
                { "escape-did-not-close", r => r.ClosedByEscape = false },
                { "world-input-leaked:commands=0/1/0;events=0/0;selectionUnchanged=True",
                    r => r.MovementCommands = 1 }
            };
            foreach (KeyValuePair<string, Action<PhysicalWorkspaceRecord>> shape in shapes)
            {
                PhysicalWorkspaceRecord bad = good();
                shape.Value(bad);
                if (!bad.Violations().Contains(shape.Key))
                    throw new InvalidOperationException("Authoring judgement missed " + shape.Key + ": " +
                        string.Join("|", bad.Violations().ToArray()));
            }
        }
    }
}
