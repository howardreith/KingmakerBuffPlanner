using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.RuntimeTesting;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        // Native UI finish (KBP_NATIVE_UI_FINISH_2026-10-10).
        private static void RunNativeUiFinishTests(string root)
        {
            Run("native-ui-scenario-admission", TestNativeUiScenarioAdmission);
            Run("native-ui-record-facts-judgement", TestNativeUiFactsJudgement);
            Run("native-ui-record-qualify-judgement", TestNativeUiQualifyJudgement);
        }

        // The no-input native UI scenario is a workspace scenario on the
        // automation family only, admitted with the live-save contract plus
        // its required stage (and, for the layout boundary, a screen size);
        // a stage anywhere else, an unknown stage, a casting allowance or
        // the advanced copy are refused.
        private static void TestNativeUiScenarioAdmission()
        {
            const string scenario = "live-workspace-native-ui";
            if (!RuntimeTestProtocol.IsNativeUiScenario(scenario) || !RuntimeTestProtocol.IsWorkspaceScenario(scenario) ||
                !RuntimeTestProtocol.IsNoInputWorkspaceScenario(scenario) ||
                RuntimeTestProtocol.IsPhysicalWorkspaceScenario(scenario) ||
                RuntimeTestProtocol.IsAdvancedFamilyScenario(scenario) ||
                RuntimeTestProtocol.IsCastingQualificationScenario(scenario))
                throw new InvalidOperationException("The native UI scenario is classified wrongly.");
            JObject launcher = JObject.Parse(File.ReadAllText(Path.Combine(FindRepositoryRoot(), "tests",
                "KingmakerBuffPlanner.Tests", "Fixtures", "kbp042-removal-01-runtime-request.json")));
            int serial = 0;
            Func<string, Action<JObject>, string> admit = (requestScenario, mutation) =>
            {
                serial++;
                string directory = Path.Combine(_protocolEvidenceRoot, "nui-admit-" + serial);
                Directory.CreateDirectory(directory);
                JObject request = (JObject)launcher.DeepClone();
                request["runId"] = "nui-admit-" + serial;
                request["scenario"] = requestScenario;
                request["evidenceDirectory"] = directory;
                request["expectedCommit"] = BuildInfo.Commit;
                request["expectedModVersion"] = BuildInfo.Version;
                ((JObject)request["parameters"]).Remove("physicalExpectation");
                if (requestScenario == scenario) request["parameters"]["nativeUiStage"] = "facts";
                if (mutation != null) mutation(request);
                string path = Path.Combine(directory, "runtime-request.json");
                File.WriteAllText(path, request.ToString());
                string rejection;
                RuntimeTestRequest read = ReadProtocol(
                    new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
                return read == null ? rejection : string.Empty;
            };
            foreach (Action<JObject> accepted in new Action<JObject>[]
            {
                null,
                r => r["parameters"]["nativeUiStage"] = "qualify",
                r => r["parameters"]["expectedScreen"] = "1280x720"
            })
            {
                string rejection = admit(scenario, accepted);
                if (rejection.Length != 0)
                    throw new InvalidOperationException("A native UI request is rejected: " + rejection);
            }
            var refused = new List<Tuple<string, string, Action<JObject>>>
            {
                Tuple.Create<string, string, Action<JObject>>(scenario, "invalid-request:native-ui-stage-required",
                    r => ((JObject)r["parameters"]).Remove("nativeUiStage")),
                Tuple.Create<string, string, Action<JObject>>(scenario, "invalid-request:native-ui-stage",
                    r => r["parameters"]["nativeUiStage"] = "cast"),
                Tuple.Create<string, string, Action<JObject>>(scenario, "invalid-request:expected-screen",
                    r => r["parameters"]["expectedScreen"] = "320x200"),
                Tuple.Create<string, string, Action<JObject>>(scenario, "invalid-request:live-save-parameters",
                    r => r["parameters"]["cfAllowance"] = "{}"),
                Tuple.Create<string, string, Action<JObject>>(scenario, "invalid-request:live-save-family-scenario", r =>
                    {
                        r["parameters"]["workingSaveName"] = "KBP_ADVANCED_WORKING";
                        r["parameters"]["baselineSaveName"] = "KBP_ADVANCED_BASELINE";
                    }),
                Tuple.Create<string, string, Action<JObject>>("live-workspace-removal",
                    "invalid-request:native-ui-stage-only-with-native-ui",
                    r => r["parameters"]["nativeUiStage"] = "facts")
            };
            foreach (Tuple<string, string, Action<JObject>> shape in refused)
            {
                string rejection = admit(shape.Item1, shape.Item3);
                if (rejection != shape.Item2)
                    throw new InvalidOperationException("Native UI admission did not refuse with " + shape.Item2 +
                        ": " + (rejection.Length == 0 ? "accepted" : rejection));
            }
        }

        private static NativeUiRecord CapturedNativeUiRecord(string stage)
        {
            var record = new NativeUiRecord
            {
                RunId = "nui", Stage = stage, NativeHudButtons = 12, NativePopupOpened = true, PlannerOpened = true,
                NativeHudCell = new[] { 48f, 48f }
            };
            record.FactsFiles.AddRange(new[] { "hud", "text", "spellbook", "native-popup" });
            record.Spellbooks.Add(new NativeUiSpellbookView { Unit = "Linzi", Shown = true });
            return record;
        }

        // The facts stage judges only that the native contracts and frames
        // were captured, and that nothing ran.
        private static void TestNativeUiFactsJudgement()
        {
            NativeUiRecord good = CapturedNativeUiRecord(NativeUiRecord.FactsStage);
            if (good.Violations().Count != 0)
                throw new InvalidOperationException("A complete facts record is judged: " +
                    string.Join("|", good.Violations().ToArray()));
            var defects = new Dictionary<string, Action<NativeUiRecord>>
            {
                { "facts-missing:native-popup", r => r.FactsFiles.Remove("native-popup") },
                { "native-popup-not-opened", r => r.NativePopupOpened = false },
                { "capture-missing:nui-hud.png:IOException", r => r.MissingCaptures.Add("nui-hud.png:IOException") },
                { "runs-started:1", r => r.RunsStarted = 1 },
                { "spellbook-not-shown", r => r.Spellbooks[0].Shown = false },
                { "stage:cast", r => r.Stage = "cast" }
            };
            foreach (KeyValuePair<string, Action<NativeUiRecord>> defect in defects)
            {
                NativeUiRecord record = CapturedNativeUiRecord(NativeUiRecord.FactsStage);
                defect.Value(record);
                if (!record.Violations().Contains(defect.Key))
                    throw new InvalidOperationException("The facts record missed " + defect.Key + ": " +
                        string.Join("|", record.Violations().ToArray()));
            }
        }

        private static NativeUiRecord QualifiedNativeUiRecord()
        {
            NativeUiRecord record = CapturedNativeUiRecord(NativeUiRecord.QualifyStage);
            record.PlannerHudButtons = 4;
            record.PlannerHudCell = new[] { 48f, 48f };
            record.PlannerColumnOffsets = new[] { 0f, 0.5f, -0.5f, 0f };
            record.PlannerRowAboveNativeRows = true;
            record.PlannerFrameEnclosesRow = true;
            record.OldRailHiddenUnderRow = true;
            record.HudTopHitOwnedCells = 4;
            record.PlannerHudTooltipShown = true;
            record.PlannerHudTooltipTitle = "Buff Planner";
            NativeUiSpellbookView view = record.Spellbooks[0];
            view.PlannerButtonPresent = true;
            view.PlannerButtonInteractable = true;
            view.PlannerButtonCount = 1;
            view.MetamagicPresent = true;
            view.MetamagicActive = true;
            view.MetamagicRect = new[] { 1440f, 100f, 150f, 40f };
            view.PlannerRect = new[] { 1600f, 100f, 150f, 40f };
            record.SpellbookTopRightEntryAbsent = true;
            record.SpellbookHandoffOpenedPlanner = true;
            record.PlannerPopupPresenter = "native-description-window";
            record.PlannerPopupClosedByEscape = true;
            record.PlannerStillOpenAfterPopupEscape = true;
            record.PlannerDocumentBeforePopup = "doc";
            record.PlannerDocumentAfterPopup = "doc";
            record.PlannerTitle = "Buff Planner";
            record.PlannerVisibleLabels.AddRange(new[] { "Buff Planner", "Caster & source", "Castings", "Recipients" });
            foreach (string route in new[] { "inventory", "character", "spellbook" })
                record.Switches.Add(new NativeUiWindowSwitch
                {
                    Route = route, PlannerOpenBefore = true, NativeShownAfter = true,
                    FullScreenModeWhileNative = true, NativeReleasedAfterClose = true, PlannerReopened = true,
                    PlannerRootsAfterReopen = 1, SelectedSourceBefore = "src", SelectedSourceAfterReopen = "src"
                });
            record.SearchFocusSuppressesNativeBindings = true;
            record.EscapeClosedFocusFirst = true;
            record.EscapeClosedPlannerLast = true;
            record.LeaseReleasedAtEnd = true;
            record.RoutinePressRefused = true;
            record.GameLogPlannerLines.Add("Buff Planner: Long was not cast.");
            return record;
        }

        // The qualify stage judges each approved integration; each defect
        // the owner reported (or a regression of a working behaviour) is a
        // named violation.
        private static void TestNativeUiQualifyJudgement()
        {
            NativeUiRecord good = QualifiedNativeUiRecord();
            if (good.Violations().Count != 0)
                throw new InvalidOperationException("A qualified record is judged: " +
                    string.Join("|", good.Violations().ToArray()));
            var defects = new Dictionary<string, Action<NativeUiRecord>>
            {
                { "hud-cell-size:42.0,42.0!=48.0,48.0", r => r.PlannerHudCell = new[] { 42f, 42f } },
                { "hud-columns:0.0,0.0,3.0,0.0", r => r.PlannerColumnOffsets = new[] { 0f, 0f, 3f, 0f } },
                { "hud-frame-not-enclosing-row", r => r.PlannerFrameEnclosesRow = false },
                { "hud-old-rail-under-row", r => r.OldRailHiddenUnderRow = false },
                { "hud-tooltip:none", r => { r.PlannerHudTooltipShown = false; r.PlannerHudTooltipTitle = null; } },
                { "spellbook-planner-button-disabled:Linzi", r => r.Spellbooks[0].PlannerButtonInteractable = false },
                { "spellbook-pair-height-or-baseline:Linzi", r => r.Spellbooks[0].PlannerRect = new[] { 1600f, 100f, 150f, 30f } },
                { "spellbook-top-right-entry-present", r => r.SpellbookTopRightEntryAbsent = false },
                { "popup-presenter:planner-scroll", r => r.PlannerPopupPresenter = "planner-scroll" },
                { "popup-changed-plan", r => r.PlannerDocumentAfterPopup = "doc2" },
                { "retired-label:Planner: Casting-first", r => r.PlannerVisibleLabels.Add("Planner: Casting-first") },
                { "planner-title:Buff Planner — casting plan", r => r.PlannerTitle = "Buff Planner — casting plan" },
                { "switch:inventory:planner-stayed", r => r.Switches[0].PlannerOpenAfter = true },
                { "switch:character:native-lost-full-screen-mode", r => r.Switches[1].FullScreenModeWhileNative = false },
                { "switch:spellbook:pause-changed", r => r.Switches[2].PausedAfterClose = true },
                { "switch:inventory:selection-not-kept", r => r.Switches[0].SelectedSourceAfterReopen = "other" },
                { "search-focus-native-bindings", r => r.SearchFocusSuppressesNativeBindings = false },
                { "escape-order", r => r.EscapeClosedFocusFirst = false },
                { "game-log-planner-lines:2", r => r.GameLogPlannerLines.Add("Buff Planner: again") }
            };
            foreach (KeyValuePair<string, Action<NativeUiRecord>> defect in defects)
            {
                NativeUiRecord record = QualifiedNativeUiRecord();
                defect.Value(record);
                if (!record.Violations().Contains(defect.Key))
                    throw new InvalidOperationException("The qualify record missed " + defect.Key + ": " +
                        string.Join("|", record.Violations().ToArray()));
            }
        }
    }
}
