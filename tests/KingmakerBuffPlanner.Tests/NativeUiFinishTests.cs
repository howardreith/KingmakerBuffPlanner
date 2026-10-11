using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.RuntimeTesting;
using KingmakerBuffPlanner.UI;
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
            Run("native-window-takes-over-from-the-planner", TestNativeWindowTakeover);
            Run("native-ui-planner-copy", TestNativeUiPlannerCopy);
            Run("native-ui-game-log-lines", TestNativeUiGameLogLines);
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

        // UI-02: a native full-screen window shown while the planner is open
        // takes over (never the planner's own notification, never a hide);
        // the lease then hands the game's single full-screen flag to it
        // before restoring selection, so FullScreenUi mode is not ended
        // under the open native window; an ordinary close still restores.
        private static void TestNativeWindowTakeover()
        {
            if (!PlannerWindowTransition.YieldsTo(true, false, true) ||
                PlannerWindowTransition.YieldsTo(true, true, true) ||
                PlannerWindowTransition.YieldsTo(true, false, false) ||
                PlannerWindowTransition.YieldsTo(false, false, true))
                throw new InvalidOperationException("The window takeover rule is wrong.");
            var boundary = new FakeInputBoundary();
            BuffPlannerInputLease lease = BuffPlannerInputLease.Acquire(boundary);
            lease.ReleaseToNativeWindow();
            lease.ReleaseToNativeWindow();
            if (boundary.YieldCount != 1 || !boundary.YieldedBeforeRestore || boundary.RestoreCount != 1 ||
                !lease.IsReleased)
                throw new InvalidOperationException("The lease did not hand the full-screen state over once.");
            boundary = new FakeInputBoundary();
            lease = BuffPlannerInputLease.Acquire(boundary);
            lease.Dispose();
            if (boundary.YieldCount != 0 || boundary.RestoreCount != 1)
                throw new InvalidOperationException("An ordinary close handed the full-screen state away.");
            string source = ProductionSource("UI", "KingmakerPlannerInputBoundary.cs");
            string yield = WithoutComments(MemberBody(source, "public void YieldFullScreenOwnership()"));
            string restore = WithoutComments(MemberBody(source, "public void RestoreState(object state)"));
            string raise = WithoutComments(MemberBody(source, "private static void Raise(bool state)"));
            if (!yield.Contains("_requested = false;") || !restore.Contains("if (_requested) Raise(false);") ||
                !raise.Contains("RaisingOwnNotification = true;") || !raise.Contains("RaisingOwnNotification = false;"))
                throw new InvalidOperationException("The Kingmaker boundary does not keep the native window's state.");
            string uiRoot = WithoutComments(ProductionSource("UI", "BuffPlannerUiRoot.cs"));
            string handler = WithoutComments(MemberBody(ProductionSource("UI", "BuffPlannerUiRoot.cs"),
                "public void HandleFullScreenUiChanged("));
            if (!uiRoot.Contains("IFullScreenUIHandler") ||
                !handler.Contains("KingmakerPlannerInputBoundary.RaisingOwnNotification") ||
                !handler.Contains("YieldToNativeWindow("))
                throw new InvalidOperationException("The planner does not answer native full-screen windows.");
        }

        // UI-04/UI-06/UI-07: the planner's short copy. A card states the
        // effect and what the run does about it as two facts; a portrait
        // states what a click does now; the sidebar one next step; a
        // source row what is left and what it spends; Run's line what a
        // run does or exactly why it cannot.
        private static void TestNativeUiPlannerCopy()
        {
            var cases = new ActualExpected
            {
                { CastingGraphText.CardStatus(ResolvedCastingReadiness.AlreadySatisfied, false, null), "Active \u00b7 Will skip" },
                { CastingGraphText.CardStatus(ResolvedCastingReadiness.Ready, true, null), "Ready \u00b7 Cannot run yet" },
                { CastingGraphText.CardStatus(ResolvedCastingReadiness.Ready, false,
                    new[] { "existing-active-recast:unit-1" }), "Active \u00b7 Will recast" },
                { CastingGraphText.CardStatus(ResolvedCastingReadiness.Ready, false,
                    new[] { "existing-insufficient:unit-1" }), "Active \u00b7 Will recast" },
                { CastingGraphText.CardStatus(ResolvedCastingReadiness.Ready, false, new string[0]), "Ready" },
                { CastingGraphText.CardStatus(ResolvedCastingReadiness.Blocked, false, null), "Not ready" },
                { CastingGraphText.RecipientHint(true, true, false, null, false, "#2", null), "Click again to remove #2" },
                { CastingGraphText.RecipientHint(true, true, false, null, false, null, "#1"), "Has casting #1" },
                { CastingGraphText.RecipientHint(true, false, false, "Out of this caster's reach.", false, null, null),
                    "Out of this caster's reach." },
                { CastingGraphText.RecipientHint(true, true, false, null, false, null, null), "Click to move it here" },
                { CastingGraphText.RecipientHint(true, true, false, null, true, null, null), "Click to centre it here" },
                { CastingGraphText.RecipientHint(false, true, false, null, false, null, "#3"),
                    "Has casting #3 \u00b7 click to select it" },
                { CastingGraphText.RecipientHint(false, false, true, null, false, null, null), "Choose a caster first" },
                { CastingGraphText.RecipientHint(false, true, false, null, false, null, null), "Click to add a casting" },
                { CastingGraphText.RecipientHint(false, true, false, null, true, null, null), "Click to centre a casting here" },
                { CastingGraphText.NextStep(false, false, false, false), "Choose a buff" },
                { CastingGraphText.NextStep(true, true, false, false), "Choose a caster and source" },
                { CastingGraphText.NextStep(true, true, true, false), "Choose a recipient" },
                { CastingGraphText.NextStep(true, true, true, true), "Choose where to centre it" },
                { CastingGraphText.SourceLine(true, null, "2 left after plan", "level 1 spell slot", null),
                    "2 left after plan \u00b7 level 1 spell slot" },
                { CastingGraphText.SourceLine(false, "No casts left after the rest of the plan.", "0 left after plan",
                    "level 1 spell slot", new[] { "Bless" }),
                    "None left after the plan \u00b7 0 left after plan \u00b7 level 1 spell slot (shared with Bless)" },
                { CastingGraphText.CatalogueCount(2, 3, "Long"), "2 in Long \u00b7 3 in all" },
                { CastingGraphText.CatalogueCount(0, 1, "Long"), "1 in other routines" },
                { CastingGraphText.CatalogueCount(0, 0, "Long"), string.Empty },
                { WorkspaceFooterText.RunStatus("native-casting-enabled", "Long", 3, false, 1, 2, null, 0),
                    "Long cannot run: 2 castings are not ready (marked in red). Ready Casts Only runs the other 1." },
                { WorkspaceFooterText.RunStatus("native-casting-enabled", "Long", 2, true, 2, 0, null, 1),
                    "Run Long casts 2 castings. 1 casting is short if every routine runs without rest (see Resources)." },
                { WorkspaceFooterText.RunStatus("native-casting-enabled", "Long", 1, true, 0, 0, null, 0),
                    "Nothing to cast in Long: every casting is already active." },
                { WorkspaceFooterText.RunStatus("native-casting-enabled", "Short", 0, false, 0, 0, null, 0),
                    "Short has no castings yet." },
                { WorkspaceFooterText.RunStatus("native-casting-busy", "Long", 1, false, 0, 0, null, 0),
                    "A routine is running; press its HUD button again to stop it." }
            };
            foreach (KeyValuePair<string, string> pair in cases)
                if (pair.Key != pair.Value)
                    throw new InvalidOperationException("Planner copy: expected '" + pair.Value + "', got '" + pair.Key + "'.");
        }

        // UI-08: a run adds exactly one concise summary line to the game's
        // own log (the game logs each cast itself); a refused press with no
        // planner window open adds its refusal's first sentence.
        private static void TestNativeUiGameLogLines()
        {
            Func<string, CastingOutcomeState, bool, bool, CastingOutcomeEntry> entry =
                (id, state, planned, spent) => new CastingOutcomeEntry(id, state, planned,
                    planned && state != CastingOutcomeState.NotProcessed, spent, "detail");
            var done = new CastingRunReport("run-1", "long", CastingApplyMode.Ordinary, "p", "completed", false, false,
                new[]
                {
                    entry("cast-1", CastingOutcomeState.EffectConfirmed, true, true),
                    entry("cast-2", CastingOutcomeState.EffectConfirmed, true, true),
                    entry("cast-3", CastingOutcomeState.Skipped, false, false)
                }, new string[0]);
            var halted = new CastingRunReport("run-2", "long", CastingApplyMode.Ordinary, "p", "halted:x", false, true,
                new[]
                {
                    entry("cast-1", CastingOutcomeState.Failed, true, true),
                    entry("cast-2", CastingOutcomeState.NotProcessed, true, false)
                }, new string[0]);
            var stopped = new CastingRunReport("run-3", "long", CastingApplyMode.Ordinary, "p", "cancelled:x", true, false,
                new[] { entry("cast-1", CastingOutcomeState.EffectConfirmed, true, true) }, new string[0]);
            var lines = new ActualExpected
            {
                { CastingRunPresentation.GameLogLine(done, "Long"), "Buff Planner: Long done - 2 of 2 cast, 1 already active." },
                { CastingRunPresentation.GameLogLine(halted, "Long"),
                    "Buff Planner: Long finished with a failure - 0 of 2 cast, 1 failed, 1 not cast." },
                { CastingRunPresentation.GameLogLine(stopped, "Long"), "Buff Planner: Long stopped - 1 of 1 cast." },
                { CastingRunPresentation.GameLogRefusal("Cannot run Long: the party is in combat. Try later."),
                    "Buff Planner: Cannot run Long: the party is in combat." },
                { CastingRunPresentation.GameLogRefusal(null), "Buff Planner: nothing was cast." }
            };
            foreach (KeyValuePair<string, string> pair in lines)
                if (pair.Key != pair.Value)
                    throw new InvalidOperationException("Game log line: expected '" + pair.Value + "', got '" + pair.Key + "'.");
            string uiRoot = ProductionSource("UI", "BuffPlannerUiRoot.cs");
            string refusal = WithoutComments(MemberBody(uiRoot, "private void ReportClosedPlannerRefusal(string refusal)"));
            if (!refusal.Contains("if (_castingWorkspace != null) return;") ||
                !WithoutComments(uiRoot).Contains("WriteGameLog(CastingRunPresentation.GameLogLine(report, name),"))
                throw new InvalidOperationException("Run results do not reach the game log as one line.");
        }

        // (actual, expected) pairs; actual values may repeat.
        private sealed class ActualExpected : List<KeyValuePair<string, string>>
        {
            public void Add(string actual, string expected)
            {
                Add(new KeyValuePair<string, string>(actual, expected));
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
