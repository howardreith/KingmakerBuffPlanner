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
        private sealed class NativeUiFieldsOnlyContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            protected override List<System.Reflection.MemberInfo> GetSerializableMembers(Type objectType)
            {
                return base.GetSerializableMembers(objectType).Where(member =>
                    member.MemberType == System.Reflection.MemberTypes.Field).ToList();
            }
        }

        private static void TestNativeUiStateJson()
        {
            Func<Newtonsoft.Json.JsonSerializerSettings> previous = Newtonsoft.Json.JsonConvert.DefaultSettings;
            try
            {
                Newtonsoft.Json.JsonConvert.DefaultSettings = () => new Newtonsoft.Json.JsonSerializerSettings
                {
                    ContractResolver = new NativeUiFieldsOnlyContractResolver(),
                    PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects
                };
                JObject omitted = JObject.FromObject(new { name = "setup", interactable = false });
                Expect(omitted.Property("name") == null && omitted.Property("interactable") == null,
                    "The matched field-only contract must reproduce the missing anonymous state.");
                JObject button = JObject.Parse(NativeUiStateJson.HudButton("setup", false,
                    new[] { 1f, 0.5f, 0.25f, 0f }).ToString());
                JObject casting = JObject.Parse(NativeUiStateJson.Casting("cast-1", "caster", "target",
                    "Active - Will skip", "AlreadyActive", "provider").ToString());
                Expect((string)button["name"] == "setup" && button["interactable"].Type == JTokenType.Boolean &&
                    !(bool)button["interactable"] && (float)button["highlight"][1] == 0.5f,
                    "The actual unavailable flag, control identity and rendered tint must survive game defaults.");
                Expect((string)casting["CastingId"] == "cast-1" && (string)casting["CasterUnitId"] == "caster" &&
                    (string)casting["TargetUnitId"] == "target" && (string)casting["SourceProviderKey"] == "provider" &&
                    (string)casting["Readiness"] == "AlreadyActive" && (string)casting["StatusLabel"] == "Active - Will skip",
                    "Casting state and exact source/receiver identities must survive game defaults.");
            }
            finally { Newtonsoft.Json.JsonConvert.DefaultSettings = previous; }
        }

        private static void RunNativeUiFinishTests(string root)
        {
            Run("native-pause-restoration-world-boundary", TestPauseRestoreWorldBoundary);
            Run("native-pause-restoration-queued-mode", TestPauseRestoreQueuedMode);
            Run("native-pause-restoration-original-state", TestPauseRestoreOriginalState);
            Run("native-pause-restoration-bounded-timeout", TestPauseRestoreTimeout);
            Run("physical-search-record-capture-and-history", TestPhysicalSearchObservation);
            Run("native-ui-snapshots-retain-state-under-game-json-defaults", TestNativeUiStateJson);
            Run("native-ui-capture-holds-frame-and-output", TestNativeUiCaptureBoundary);
            Run("native-ui-capture-bounded-timeout", TestNativeUiCaptureTimeout);
            Run("native-ui-capture-refuses-stale-output", TestNativeUiCaptureFreshOutput);
            Run("native-ui-scenario-admission", TestNativeUiScenarioAdmission);
            Run("native-ui-record-facts-judgement", TestNativeUiFactsJudgement);
            Run("native-ui-record-qualify-judgement", TestNativeUiQualifyJudgement);
            Run("native-window-takes-over-from-the-planner", TestNativeWindowTakeover);
            Run("native-ui-planner-copy", TestNativeUiPlannerCopy);
            Run("native-ui-game-log-lines", TestNativeUiGameLogLines);
            Run("native-hud-stepped-enclosure-geometry", TestHudStepGeometry);
            Run("native-hud-cell-art-composition", TestHudCellArt);
            Run("native-source-tabs-share-one-caption-size", TestSourceTabLayout);
            Run("native-text-material-matches-selected-font-atlas", TestNativeTextMaterialSelection);
        }

        private static void TestNativeTextMaterialSelection()
        {
            const string role = "NexusSerif-Regular SDF - Cue Answer";
            Expect(NativeTextMaterialSelection.Matches(role, role, 42, 42),
                "A named role material on the selected font atlas must be accepted.");
            Expect(!NativeTextMaterialSelection.Matches(role, role, 42, 17),
                "A same-name fallback material on another font atlas must be refused.");
            Expect(!NativeTextMaterialSelection.Matches(role,
                    "NexusSerif-Regular SDF - ButtonLabel", 42, 42),
                "A material from another text role must be refused.");
            Expect(!NativeTextMaterialSelection.Matches(role, role, 0, 0),
                "An unresolved atlas must retain the font's default material.");
        }

        private static void TestPhysicalSearchObservation()
        {
            Func<PhysicalSearchObservation> good = () =>
            {
                var value = new PhysicalSearchObservation {
                    Query = "ligh", TypedQuery = "ligh", TypedWithBindingLetter = "lighb",
                    ModeBefore = "FullScreenUI", ModeAfterQuery = "FullScreenUI", ModeAfterBindingLetter = "FullScreenUI",
                    FocusedBeforeTyping = true, PlannerStayedOpen = true, DocumentUnchanged = true,
                    CleanupComplete = true, PreservedSelectedSource = "selected-resistance",
                    ExpectedSourcesAfterBindingLetter = 1, SourcesAfterBindingLetter = 1 };
                value.ExpectedSources.Add("light-spell");
                value.ObservedSources.Add("light-spell"); value.ObservedSources.Add("selected-resistance");
                value.Captures.Add(new JObject { { "file", "physical-cf-search-filtered.png" }, { "requestFrame", 10 }, { "completedFrame", 11 } });
                value.Captures.Add(new JObject { { "file", "physical-cf-search-binding-text.png" }, { "requestFrame", 20 }, { "completedFrame", 21 } });
                return value;
            };
            Expect(good().Violations().Count == 0, "Complete physical search state must pass.");
            foreach (Action<PhysicalSearchObservation> breakState in new Action<PhysicalSearchObservation>[] {
                v => v.FocusedBeforeTyping = false, v => v.TypedQuery = "lig", v => v.TypedWithBindingLetter = "ligh",
                v => v.ModeAfterBindingLetter = "Spellbook", v => v.ObservedSources.Clear(),
                v => v.ObservedSources.Add("foreign"), v => v.PreservedSelectedSource = null,
                v => v.SourcesAfterBindingLetter = 0, v => v.ExpectedSourcesAfterBindingLetter = -1,
                v => v.DocumentUnchanged = false, v => v.CleanupComplete = false,
                v => v.Captures[0]["completedFrame"] = 10, v => v.CaptureFailure = "timeout" })
            {
                PhysicalSearchObservation value = good(); breakState(value);
                Expect(value.Violations().Count != 0, "Incomplete typing, isolation, cleanup or capture must fail.");
            }
            var historical = new PhysicalWorkspaceRecord { MoonExpectation = "select", CastingFirst = true };
            Expect(historical.ExpectedCastingActions.SequenceEqual(PhysicalWorkspaceRecord.CastingActions),
                "Historical schema 1 keeps its original action interpretation.");
            historical.SchemaVersion = 2;
            Expect(PhysicalSearchObservation.Actions.All(action => historical.ExpectedCastingActions.Contains(action)) &&
                historical.Violations().Contains("search:observation-missing"),
                "New schema cannot omit the physical search observation.");
            JObject raw = good().ToJson();
            Expect(raw["focusedBeforeTyping"].Type == JTokenType.Boolean &&
                (string)raw["dispatch"] == "physical-os-input" && (string)raw["cleanupDispatch"] == "view-callback",
                "Physical delivery and callback cleanup must remain distinct in raw evidence.");
            Expect((string)raw["preservedSelectedSource"] == "selected-resistance" &&
                (int)raw["expectedSourcesAfterBindingLetter"] == 1,
                "Search evidence must distinguish the selected-row exception from query matches.");
        }

        private static void TestPauseRestoreWorldBoundary()
        {
            var restore = new NativeUiPauseRestoration(false, 10000);
            int requests = 0;
            Action<bool> request = value => requests++;
            if (restore.Observe(false, true, 1, request) != NativeUiPauseRestoreStatus.Pending ||
                restore.Observe(false, false, 2, request) != NativeUiPauseRestoreStatus.Pending || requests != 0)
                throw new InvalidOperationException("Pause restoration ran beneath an owned full-screen window.");
            if (restore.Observe(true, true, 3, request) != NativeUiPauseRestoreStatus.Pending || requests != 1)
                throw new InvalidOperationException("World release did not permit the one owned restoration request.");
        }

        private static void TestPauseRestoreQueuedMode()
        {
            var restore = new NativeUiPauseRestoration(false, 10000);
            int requests = 0;
            Action<bool> request = value =>
            {
                if (value) throw new InvalidOperationException("Unpause restoration requested pause.");
                requests++;
            };
            if (restore.Observe(true, true, 1, request) != NativeUiPauseRestoreStatus.Pending ||
                restore.Observe(true, true, 2, request) != NativeUiPauseRestoreStatus.Pending || requests != 1)
                throw new InvalidOperationException("A queued request was treated as observed restoration or repeated.");
            if (restore.Observe(true, false, 3, request) != NativeUiPauseRestoreStatus.Restored ||
                restore.Observe(true, false, 10000, request) != NativeUiPauseRestoreStatus.Restored || requests != 1)
                throw new InvalidOperationException("The later native state was not retained as restoration.");
        }

        private static void TestPauseRestoreOriginalState()
        {
            foreach (bool original in new[] { false, true })
            {
                var restore = new NativeUiPauseRestoration(original, 10000);
                if (restore.Observe(true, original, 1, value =>
                    { throw new InvalidOperationException("An unchanged native pause state was rewritten."); })
                    != NativeUiPauseRestoreStatus.Restored)
                    throw new InvalidOperationException("The original native pause state was not preserved.");
                restore = new NativeUiPauseRestoration(original, 10000);
                if (restore.Observe(true, !original, 1, value =>
                    { if (value != original) throw new InvalidOperationException("Wrong original pause requested."); })
                    != NativeUiPauseRestoreStatus.Pending ||
                    restore.Observe(true, original, 2, value => {}) != NativeUiPauseRestoreStatus.Restored)
                    throw new InvalidOperationException("Restoration did not preserve either original pause value.");
            }
        }

        private static void TestPauseRestoreTimeout()
        {
            foreach (bool worldReleased in new[] { false, true })
            {
                var restore = new NativeUiPauseRestoration(false, 10000);
                if (restore.Observe(worldReleased, true, 9999, value => {}) != NativeUiPauseRestoreStatus.Pending ||
                    restore.Observe(worldReleased, true, 10000, value => {}) != NativeUiPauseRestoreStatus.TimedOut ||
                    restore.Observe(true, false, 10001, value => {}) != NativeUiPauseRestoreStatus.TimedOut)
                    throw new InvalidOperationException("A blocked or stalled pause restoration was not bounded.");
            }
        }

        private static readonly byte[] NativeUiTestPng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aMxkAAAAASUVORK5CYII=");

        private static string NativeUiCaptureTestPath()
        {
            return Path.Combine(_protocolEvidenceRoot, "nui-capture-" + Guid.NewGuid().ToString("N") + ".png");
        }

        private static void TestNativeUiCaptureBoundary()
        {
            string path = NativeUiCaptureTestPath();
            var capture = new NativeUiCaptureBoundary(path, 42, 10000);
            if (capture.Observe(43, 1) != NativeUiCaptureStatus.Pending)
                throw new InvalidOperationException("A later frame without output released the UI state.");
            File.WriteAllBytes(path, NativeUiTestPng.Take(NativeUiTestPng.Length - 12).ToArray());
            if (capture.Observe(43, 2) != NativeUiCaptureStatus.Pending)
                throw new InvalidOperationException("A partial PNG released the UI state.");
            File.WriteAllBytes(path, NativeUiTestPng);
            using (var writer = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                if (capture.Observe(43, 3) != NativeUiCaptureStatus.Pending)
                    throw new InvalidOperationException("Output still open for writing released the UI state.");
            if (capture.Observe(42, 4) != NativeUiCaptureStatus.Pending)
                throw new InvalidOperationException("Ready output in the request frame released the UI state.");
            if (capture.Observe(43, 5) != NativeUiCaptureStatus.Completed ||
                capture.Observe(44, 10000) != NativeUiCaptureStatus.Completed)
                throw new InvalidOperationException("Completed output on a later frame was not retained.");
        }

        private static void TestNativeUiCaptureTimeout()
        {
            foreach (string condition in new[] { "missing", "partial", "stalled-frame", "locked" })
            {
                string path = NativeUiCaptureTestPath();
                var capture = new NativeUiCaptureBoundary(path, 42, 10000);
                if (condition != "missing") File.WriteAllBytes(path, condition == "partial"
                    ? NativeUiTestPng.Take(NativeUiTestPng.Length - 12).ToArray() : NativeUiTestPng);
                int frame = condition == "stalled-frame" ? 42 : 43;
                using (FileStream writer = condition == "locked"
                    ? new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite) : null)
                {
                    if (capture.Observe(frame, 9999) != NativeUiCaptureStatus.Pending ||
                        capture.Observe(frame, 10000) != NativeUiCaptureStatus.TimedOut)
                        throw new InvalidOperationException("Capture timeout was not bounded for " + condition + ".");
                }
                File.WriteAllBytes(path, NativeUiTestPng);
                if (capture.Observe(44, 10001) != NativeUiCaptureStatus.TimedOut)
                    throw new InvalidOperationException("Late output relabelled a timeout as completion.");
            }
            string latePath = NativeUiCaptureTestPath();
            var late = new NativeUiCaptureBoundary(latePath, 42, 10000);
            File.WriteAllBytes(latePath, NativeUiTestPng);
            if (late.Observe(43, 10000) != NativeUiCaptureStatus.TimedOut)
                throw new InvalidOperationException("Output first observed at the deadline bypassed the timeout.");
        }

        private static void TestNativeUiCaptureFreshOutput()
        {
            string path = NativeUiCaptureTestPath();
            File.WriteAllBytes(path, NativeUiTestPng);
            bool refused = false;
            try { new NativeUiCaptureBoundary(path, 42, 10000); }
            catch (IOException) { refused = true; }
            if (!refused || !File.ReadAllBytes(path).SequenceEqual(NativeUiTestPng))
                throw new InvalidOperationException("An existing capture was reused or overwritten.");
            path = NativeUiCaptureTestPath();
            var capture = new NativeUiCaptureBoundary(path, 42, 10000);
            byte[] invalid = NativeUiTestPng.ToArray();
            invalid[0] = 0;
            File.WriteAllBytes(path, invalid);
            if (capture.Observe(43, 1) != NativeUiCaptureStatus.Pending)
                throw new InvalidOperationException("A non-PNG file was accepted as capture output.");
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
                { CastingGraphText.RecipientHint(CastingGraphRecipientAction.Remove, false, "#2", null),
                    "Has casting #2 \u00b7 click to remove it" },
                { CastingGraphText.RecipientHint(CastingGraphRecipientAction.InspectGroupCoverage, true, "#1", null),
                    "Covered by #1 \u00b7 click to inspect it" },
                { CastingGraphText.RecipientHint(CastingGraphRecipientAction.Refuse, false, null,
                    "Out of this caster's reach."), "Out of this caster's reach." },
                { CastingGraphText.RecipientHint(CastingGraphRecipientAction.Retarget, false, null, null),
                    "Click to move it here" },
                { CastingGraphText.RecipientHint(CastingGraphRecipientAction.Recentre, true, null, null),
                    "Click to centre it here" },
                { CastingGraphText.RecipientHint(CastingGraphRecipientAction.Remove, false, "#3", null),
                    "Has casting #3 \u00b7 click to remove it" },
                { CastingGraphText.RecipientHint(CastingGraphRecipientAction.Refuse, false, null,
                    "Choose a caster and exact source first."), "Choose a caster and exact source first." },
                { CastingGraphText.RecipientHint(CastingGraphRecipientAction.Add, false, null, null),
                    "Click to add a casting" },
                { CastingGraphText.RecipientHint(CastingGraphRecipientAction.Add, true, null, null),
                    "Click to centre a casting here" },
                { CastingGraphText.RecipientHint(CastingGraphRecipientAction.Disambiguate, false, "#1, #2", null),
                    "Multiple matching castings (#1, #2) \u00b7 inspect one to remove it" },
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
                    "Long cannot run: 2 castings are not ready (marked in red). Choose Cast available to run the other 1." },
                { WorkspaceFooterText.RunStatus("native-casting-enabled", "Long", 2, true, 2, 0, null, 1),
                    "Run Long casts 2 castings. 1 casting is short if every routine runs without rest (see Resources)." },
                { WorkspaceFooterText.RunStatus("native-casting-enabled", "Long", 1, true, 0, 0, null, 0),
                    "Nothing to cast in Long: every casting is already active." },
                { WorkspaceFooterText.RunStatus("native-casting-enabled", "Short", 0, false, 0, 0, null, 0),
                    "Short has no castings yet." },
                { WorkspaceFooterText.RunStatus("native-casting-busy", "Long", 1, false, 0, 0, null, 0),
                    "A routine is running; press its HUD button again to stop it." },
                { WorkspaceFooterText.RunStatus("native-submission-disabled:runtime-test-session:live-workspace-native-ui",
                    "Long", 2, false, 2, 0, null, 0), "Casting is disabled during this automated session." },
                { WorkspaceFooterText.Readiness("native-submission-disabled:runtime-test-session:live-workspace-native-ui",
                    "Long"), "Casting is disabled during this automated session." }
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

        // UI-09 (R12): from the live native metrics (kbpnui-facts-03: columns
        // at 2/51/100/149/198/247, 48-unit cells, rows at 2.2 and 51.2, the
        // frame's top at 116.8, TBM_backButton 668 x 252 at 0.5 units per
        // pixel, slice borders 196/253, frame 363.89 wide) the four cells
        // stand one native gap above the first four columns, the native top
        // border rises one row over them, the step's rail stands after the
        // fourth column, and the native rail it meets is redrawn from the
        // exact texture columns the native tiled frame shows there.
        // The catalogue's source tabs (UI-03, UI-06): one caption size for all
        // four, every caption inside margins clear of the button frame, each
        // tab as wide as its caption needs. Caption widths are the four
        // captions at 14 px; the rows are the catalogue lane at 1920x1080 and
        // at 1280x720, where four equal quarters overran (kbpnui-final-qual-07,
        // kbpnui-final-qual-720-05).
        private static void TestSourceTabLayout()
        {
            float[] captions = { 19.6f, 39.2f, 55.1f, 38.4f };
            const float margin = SourceTabLayout.CaptionMargin;
            Action<SourceTabLayout, float> fitsRow = (layout, row) =>
            {
                if (!layout.Fits) throw new InvalidOperationException("The tabs do not fit a row of " + row + ".");
                float total = layout.Widths.Sum() + (captions.Length - 1) * SourceTabLayout.TabGap;
                if (Math.Abs(total - row) > 0.05f)
                    throw new InvalidOperationException("The tabs do not fill the row: " + total + " of " + row + ".");
                for (int index = 0; index < captions.Length; index++)
                {
                    if (captions[index] * layout.CaptionSize / 14f + 2f * margin > layout.Widths[index] + 0.01f)
                        throw new InvalidOperationException("Caption " + index + " overruns its tab at " + layout.CaptionSize + ".");
                    if (index > 0 && Math.Abs(layout.Lefts[index] - (layout.Lefts[index - 1] + layout.Widths[index - 1] +
                            SourceTabLayout.TabGap)) > 0.01f)
                        throw new InvalidOperationException("Tab " + index + " is not beside its neighbour.");
                }
                if (layout.Lefts[0] != 0f) throw new InvalidOperationException("The first tab does not start the row.");
            };
            SourceTabLayout wide = SourceTabLayout.Compute(253f, captions, 14f, 9f);
            if (wide.CaptionSize != 14f) throw new InvalidOperationException("A wide row shrank the captions: " + wide.CaptionSize);
            fitsRow(wide, 253f);
            SourceTabLayout narrow = SourceTabLayout.Compute(166f, captions, 14f, 9f);
            if (narrow.CaptionSize >= 14f || narrow.CaptionSize < 9f || narrow.CaptionSize * 2f != Math.Floor(narrow.CaptionSize * 2f))
                throw new InvalidOperationException("The narrow row's caption size is " + narrow.CaptionSize + ".");
            fitsRow(narrow, 166f);
            // The equal quarters this replaces: "Abilities" had no room at the
            // same size.
            float quarter = (166f - 3f * SourceTabLayout.TabGap) / 4f - 2f * margin;
            if (captions[2] * narrow.CaptionSize / 14f <= quarter)
                throw new InvalidOperationException("The regression case no longer overran equal quarters.");
            SourceTabLayout cramped = SourceTabLayout.Compute(60f, captions, 14f, 9f);
            if (cramped.CaptionSize != 9f || cramped.Fits)
                throw new InvalidOperationException("A row too narrow for the minimum size is reported as fitting.");
            foreach (Action invalid in new Action[]
                {
                    () => SourceTabLayout.Compute(0f, captions, 14f, 9f),
                    () => SourceTabLayout.Compute(166f, new float[0], 14f, 9f),
                    () => SourceTabLayout.Compute(166f, captions, 8f, 9f)
                })
            {
                bool refused = false;
                try { invalid(); }
                catch (ArgumentException) { refused = true; }
                if (!refused) throw new InvalidOperationException("An invalid tab row was laid out.");
            }
        }

        private static void TestHudStepGeometry()
        {
            float[] lefts = { 2f, 51f, 100f, 149f, 198f, 247f };
            HudStepLayout step = HudStepLayout.Compute(lefts, 48f, 48f, 51.2f, 49f, 116.8f, 0.5f);
            Func<float, float, bool> near = (a, b) => Math.Abs(a - b) < 0.06f;
            for (int index = 0; index < 4; index++)
                if (!near(step.Cells[index].X, lefts[index]) || !near(step.Cells[index].Y, 100.2f) ||
                    !near(step.Cells[index].Width, 48f) || !near(step.Cells[index].Height, 48f))
                    throw new InvalidOperationException("Cell " + index + " is at " + step.Cells[index]);
            if (step.StripRowsPx != 133 || !near(step.RaisedTop, 165.7f) ||
                !near(step.Strip.X, 0f) || !near(step.Strip.Y, 99.2f) || !near(step.Strip.Right, 155f) ||
                !near(step.Corner.X, 155f) || !near(step.Corner.Right, 215f) ||
                !near(step.Junction.X, 198f) || !near(step.Junction.Y, 99.8f) || !near(step.Junction.Top, 113.8f))
                throw new InvalidOperationException("The enclosure is wrong: strip " + step.Strip + ", corner " +
                    step.Corner + ", junction " + step.Junction + ", top " + step.RaisedTop);
            if (step.Violation(lefts, 48f, 51.2f) != null)
                throw new InvalidOperationException("The enclosure violates R12: " + step.Violation(lefts, 48f, 51.2f));
            string pieces = string.Join(" ", step.FramePieces(668, 252, 363.89f, 196, 253).Select(piece =>
                piece.Name + ":" + piece.SourceX + "," + piece.SourceY + "," + piece.SourceWidth + "x" +
                piece.SourceHeight + "@" + piece.Target).ToArray());
            const string expected = "Strip:0,119,310x133@0.0,99.2,155.0x66.5 Corner:548,119,120x133@155.0,99.2,60.0x66.5 " +
                "Junction:396,218,19x28@198.0,99.8,9.5x14.0 Junction:196,218,32x28@207.5,99.8,16.0x14.0";
            if (pieces != expected) throw new InvalidOperationException("Frame pieces: " + pieces);
            // A row off its native columns, or a frame over the native
            // row, is refused.
            float[] shifted = lefts.Select(value => value + 6f).ToArray();
            if (step.Violation(shifted, 48f, 51.2f) != "cell-column:0" ||
                step.Violation(lefts, 48f, 60f) == null)
                throw new InvalidOperationException("An off-grid enclosure was accepted.");
            bool refused = false;
            try { HudStepLayout.Compute(lefts.Take(4).ToArray(), 48f, 48f, 51.2f, 49f, 116.8f, 0.5f); }
            catch (ArgumentException) { refused = true; }
            if (!refused) throw new InvalidOperationException("Four native columns were enough to step after.");
        }

        // UI-09: a cell drawn from native cell art keeps the native frame and
        // alpha pixel for pixel, lifts the native icon out to plain stone,
        // engraves the owned glyph centred on its ink in the native icon's
        // own gold, and refuses art with no engraved icon to match.
        private static void TestHudCellArt()
        {
            const int side = 60;
            var rgba = new byte[side * side * 4];
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    bool frame = x < 6 || y < 6 || x >= side - 6 || y >= side - 6;
                    bool icon = (x - 30) * (x - 30) + (y - 30) * (y - 30) <= 64;
                    byte[] color = frame ? new byte[] { 100, 95, 60 } : icon ? new byte[] { 170, 160, 100 }
                        : new byte[] { 25, 25, 25 };
                    int i = (y * side + x) * 4;
                    rgba[i] = color[0]; rgba[i + 1] = color[1]; rgba[i + 2] = color[2];
                    rgba[i + 3] = (byte)(x == 0 && y == 0 ? 0 : 255);
                }
            var mask = new float[64 * 64];
            for (int y = 10; y <= 13; y++)
                for (int x = 5; x <= 44; x++) mask[y * 64 + x] = 1f;
            HudCellArtResult art = HudCellArt.Compose(new HudPixels(side, side, rgba), mask, 64);
            Func<int, int, int[]> at = (x, y) => Enumerable.Range(0, 4)
                .Select(channel => (int)art.Pixels.Rgba[(y * side + x) * 4 + channel]).ToArray();
            if (!at(2, 30).SequenceEqual(new[] { 100, 95, 60, 255 }) || at(0, 0)[3] != 0 ||
                !at(57, 57).SequenceEqual(new[] { 100, 95, 60, 255 }))
                throw new InvalidOperationException("The native frame or alpha changed.");
            if (at(30, 37).Take(3).Any(value => Math.Abs(value - 25) > 2))
                throw new InvalidOperationException("The native icon was not lifted: " + string.Join(",", at(30, 37)));
            if (!at(30, 30).Take(3).SequenceEqual(new[] { 170, 160, 100 }))
                throw new InvalidOperationException("The glyph is not the native gold: " + string.Join(",", at(30, 30)));
            if (Math.Abs(art.InkOffsetX) > 0.5f || Math.Abs(art.InkOffsetY) > 0.5f ||
                !art.Evidence.Contains("ink=38x4"))
                throw new InvalidOperationException("The glyph is not centred on its ink: " + art.Evidence);
            var blank = (byte[])rgba.Clone();
            for (int i = 0; i < side * side; i++)
                if (blank[i * 4] == 170) { blank[i * 4] = 25; blank[i * 4 + 1] = 25; blank[i * 4 + 2] = 25; }
            bool refused = false;
            try { HudCellArt.Compose(new HudPixels(side, side, blank), mask, 64); }
            catch (InvalidOperationException) { refused = true; }
            if (!refused) throw new InvalidOperationException("Art with no native icon was accepted.");
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
                NativeHudCell = new[] { 48f, 48f }, ScreenWidth = 1920, ScreenHeight = 1200
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
            record.PlannerHudIdleHighlightAlpha = 0f;
            record.PlannerHudHoverHighlight = new[] { 1f, 1f, 1f, 1f };
            record.PlannerHudPressedHighlight = new[] { 0.784f, 0.784f, 0.784f, 1f };
            record.PlannerHudTooltipShown = true;
            record.PlannerHudTooltipTitle = "Buff Planner";
            NativeUiSpellbookView view = record.Spellbooks[0];
            view.MetamagicActive = true;
            view.MetamagicInteractable = true;
            view.MetamagicAction = new NativeUiMetamagicAction
            {
                ButtonInvoked = true, ModeWhileOpen = true, ReturnInvoked = true,
                ControllerPresentAfterReturn = true, PlannerButtonOfferedAfterReturn = true
            };
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
            // Titles in the form the runs read them, ornamented initial included.
            record.NativePopupTitle = ResistanceTitle;
            record.PlannerPopupTitle = ResistanceTitle;
            record.PopupSameSpell = NativeUiRecord.SameDescriptionTitle(record.NativePopupTitle, record.PlannerPopupTitle);
            record.PlannerPopupClosedByEscape = true;
            record.PlannerStillOpenAfterPopupEscape = true;
            record.PlannerPopupCloseButtonFound = true;
            record.PlannerPopupClosedByCloseButton = true;
            record.PlannerDocumentBeforePopup = "doc";
            record.PlannerDocumentAfterPopup = "doc";
            record.CastingPopup = new NativeUiCastingPopup
            {
                CastingId = "casting-1", ExpectedCasterUnitId = "unit-1", ActualCasterUnitId = "unit-1",
                ExpectedAbilityGuid = "resistance", ActualAbilityGuid = "resistance",
                ExpectedTitle = "Resistance", ActualTitle = ResistanceTitle,
                Presenter = "native-description-window", ClosedByEscape = true,
                DocumentBefore = "doc", DocumentAfter = "doc"
            };
            string[] categories = { "All", "Spells", "Abilities", "Other" };
            float[] widths = { 19.6f, 39.2f, 55.1f, 38.4f };
            for (int index = 0; index < categories.Length; index++)
                record.SourceTabs.Add(new NativeUiCaptionFit
                {
                    Caption = categories[index], Size = 14f, TextWidth = widths[index], AvailableWidth = widths[index] + 8f
                });
            foreach (string category in categories)
            {
                string[] rows = category == "All" ? new[] { "src", "spell", "ability" }
                    : category == "Spells" ? new[] { "src", "spell" }
                    : category == "Abilities" ? new[] { "src", "ability" } : new[] { "src" };
                record.CatalogueFilters.Add(new NativeUiCatalogueFilter
                {
                    Category = category, ExpectedSourceIds = rows, VisibleSourceIds = (string[])rows.Clone(),
                    DocumentBefore = "doc", DocumentAfter = "doc"
                });
            }
            record.PlannerTitle = "Buff Planner";
            record.PlannerVisibleLabels.AddRange(new[] { "Buff Planner", "Caster & source", "Castings", "Recipients" });
            record.ResourcesOpened = true;
            record.ResourcesTopmost = true;
            record.ResourcesCloseFocused = true;
            record.ResourcesScrollClips = true;
            record.ResourcesScrollOverflow = 240f;
            record.ResourcesPanelSize = new[] { 1536f, 1008f };
            record.ResourcesSelectedSummary = "Long · 3 ready · 1 unavailable";
            record.ResourcesAllSummary = "All routines - no rest · 8 ready · 2 unavailable";
            record.ResourcesSelectedAgainSummary = "Long · 3 ready · 1 unavailable";
            record.ResourcesReopenedSummary = "Long · 3 ready · 1 unavailable";
            record.ResourcesSelectedScope = "SelectedRoutine";
            record.ResourcesAllScope = "AllRoutinesNoRest";
            record.ResourcesSelectedAgainScope = "SelectedRoutine";
            record.ResourcesReopenedScope = "SelectedRoutine";
            record.ResourcesSelectedCells.Add("Pool.SpellSlots.bard-l2/Resource=Bard level 2 spell slots");
            record.ResourcesAllCells.Add("Pool.SpellSlots.bard-l2/Needed=8");
            record.ResourcesSelectedAgainCells.Add("Pool.SpellSlots.bard-l2/Spend=3");
            record.ResourcesReopenedCells.Add("Pool.SpellSlots.bard-l2/Short=1");
            record.ResourcesSelectedCellsUseNativeLayer = true;
            record.ResourcesAllCellsUseNativeLayer = true;
            record.ResourcesSelectedAgainCellsUseNativeLayer = true;
            record.ResourcesReopenedCellsUseNativeLayer = true;
            record.ResourcesClosedBetweenOpens = true;
            record.ResourcesReopened = true;
            record.ResourcesReopenedCloseFocused = true;
            record.ResourcesSaveStatus = "Saved";
            record.ResourcesClosedByEscape = true;
            record.ResourcesDocumentBefore = "doc";
            record.ResourcesDocumentAfter = "doc";
            record.AvailabilityPolicyCaption = "Require full plan";
            record.RunCaption = "Run Long";
            foreach (string route in new[] { "inventory", "character", "spellbook", "key:OpenInventory" })
                record.Switches.Add(new NativeUiWindowSwitch
                {
                    Route = route, PlannerOpenBefore = true, NativeShownAfter = true,
                    FullScreenModeWhileNative = true, NativeReleasedAfterClose = true, PlannerReopened = true,
                    PlannerRootsAfterReopen = 1, SelectedSourceBefore = "src", SelectedSourceAfterReopen = "src",
                    KeyBinding = route.StartsWith("key:", StringComparison.Ordinal) ? new NativeUiKeyBindingObservation
                    {
                        DispatchKind = "binding-callback-forced", BindingName = route.Substring(4), Key = "I",
                        CurrentMode = "FullScreenUi", BindingMode = "FullScreenUi", WorksWhenPaused = true
                    } : null
                });
            foreach (string route in new[] { "inventory", "key:OpenJournal" })
                record.Switches.Add(new NativeUiWindowSwitch
                {
                    Route = route, PauseRequested = true, PausedBefore = true, PausedWhileNative = true,
                    PausedAfterClose = true, PlannerOpenBefore = true, NativeShownAfter = true,
                    FullScreenModeWhileNative = true, NativeReleasedAfterClose = true, PlannerReopened = true,
                    PlannerRootsAfterReopen = 1, SelectedSourceBefore = "src", SelectedSourceAfterReopen = "src",
                    KeyBinding = route.StartsWith("key:", StringComparison.Ordinal) ? new NativeUiKeyBindingObservation
                    {
                        DispatchKind = "binding-callback-forced", BindingName = route.Substring(4), Key = "J",
                        CurrentMode = "FullScreenUi", BindingMode = "FullScreenUi", WorksWhenPaused = true, Paused = true
                    } : null
                });
            record.OwnedPauseRestored = true;
            record.Spellbooks.Add(new NativeUiSpellbookView
            {
                Unit = "Hedwirg", Shown = true, PlannerButtonPresent = true, PlannerButtonInteractable = true,
                PlannerButtonCount = 1, UnavailablePlannerAction = new NativeUiUnavailablePlannerAction
                {
                    ButtonInvoked = true, PlannerOpened = true, UnitIdBefore = "unit-3", UnitIdAfterReturn = "unit-3",
                    NativeBookRestored = true, MetamagicStillUnavailable = true, PlannerClosedOnReturn = true
                }
            });
            record.SearchFocusSuppressesNativeBindings = true;
            record.SaveFailureShown = true;
            record.SaveRecoveryWorked = true;
            record.SaveFailureUndoRestored = true;
            record.PlanningSteps.AddRange(new[] { "focus:ok", "arm-retarget:ok", "retarget:ok",
                "undo-retarget:ok", "cast-later:ok", "undo-order:ok", "done:ok", "choose-source:ok",
                "add-inspects-only:ok", "choose-next-source:ok", "successive-add:ok", "focus-added:ok",
                "arm-change-caster:ok", "change-caster:ok", "undo-caster:ok", "remove-explicit:ok",
                "undo-remove:ok", "remove-normal:ok", "remove-second-normal:ok" });
            record.PlanningRestored = true;
            record.EscapeClosedFocusFirst = true;
            record.EscapeClosedPlannerLast = true;
            record.LeaseReleasedAtEnd = true;
            record.RoutinePressRefused = true;
            record.GameLogPlannerLines.Add("Buff Planner: Long was not cast.");
            return record;
        }

        private const string ResistanceTitle =
            "<font=\"Saber_Dist32\"><color=#983F1D><size=140%>R</size></color></font>esistance";
        private const string EarPiercingScreamTitle =
            "<font=\"Saber_Dist32\"><color=#983F1D><size=140%>E</size></color></font>ar-Piercing Scream";

        // The qualify stage judges each approved integration; each defect
        // the owner reported (or a regression of a working behaviour) is a
        // named violation.
        private static void TestNativeUiQualifyJudgement()
        {
            NativeUiRecord good = QualifiedNativeUiRecord();
            if (good.Violations().Count != 0)
                throw new InvalidOperationException("A qualified record is judged: " +
                    string.Join("|", good.Violations().ToArray()));
            // Schema 1 retains its original verdict and cannot prove the new observations.
            NativeUiRecord legacy = QualifiedNativeUiRecord();
            legacy.SchemaVersion = 1;
            legacy.SourceTabs.Clear();
            legacy.CastingPopup = null;
            legacy.CatalogueFilters.Clear();
            if (legacy.Violations().Count != 0)
                throw new InvalidOperationException("A legacy verdict was relabelled by schema-2 requirements.");
            NativeUiRecord schema4 = QualifiedNativeUiRecord();
            schema4.SchemaVersion = 4;
            schema4.ResourcesOpened = false;
            schema4.ResourcesPanelSize = null;
            schema4.AvailabilityPolicyCaption = null;
            schema4.RunCaption = null;
            schema4.PlanningSteps.Remove("arm-retarget:ok");
            if (schema4.Violations().Count != 0)
                throw new InvalidOperationException("A schema-4 verdict was relabelled by human-feedback requirements.");
            NativeUiRecord schema5 = QualifiedNativeUiRecord();
            schema5.SchemaVersion = 5;
            schema5.PlanningSteps.Remove("add-inspects-only:ok");
            schema5.PlanningSteps.Add("add-without-focus:ok");
            schema5.ResourcesSelectedCells.Clear();
            schema5.ResourcesReopened = false;
            if (schema5.Violations().Count != 0)
                throw new InvalidOperationException("A schema-5 verdict was relabelled by correction requirements.");
            legacy.SchemaVersion = 2;
            foreach (string missing in new[] { "source-tabs:missing-captions", "casting-popup:missing",
                    "catalogue-filters:missing-categories" })
                if (!legacy.Violations().Contains(missing))
                    throw new InvalidOperationException("Missing observation was accepted: " + missing);
            NativeUiRecord schema2 = QualifiedNativeUiRecord();
            schema2.SchemaVersion = 2;
            schema2.Spellbooks[0].MetamagicAction = null;
            schema2.Switches[3].KeyBinding = null;
            if (schema2.Violations().Count != 0)
                throw new InvalidOperationException("A schema-2 verdict was relabelled by schema-3 requirements.");
            schema2.SchemaVersion = 3;
            foreach (string missing in new[] { "metamagic:Linzi:missing-action",
                    "key-route:key:OpenInventory:missing-observation" })
                if (!schema2.Violations().Contains(missing))
                    throw new InvalidOperationException("Schema 3 accepted a missing native action: " + missing);
            NativeUiRecord schema3 = QualifiedNativeUiRecord();
            schema3.SchemaVersion = 3;
            schema3.Switches.RemoveAll(item => item.PauseRequested);
            schema3.OwnedPauseRestored = false;
            schema3.Spellbooks[1].UnavailablePlannerAction = null;
            if (schema3.Violations().Count != 0)
                throw new InvalidOperationException("A schema-3 verdict was relabelled by schema-4 requirements.");
            schema3.SchemaVersion = 4;
            foreach (string missing in new[] { "pause:no-key-route", "pause:no-hud-route", "pause:not-restored",
                    "unavailable-metamagic:Hedwirg:missing-action" })
                if (!schema3.Violations().Contains(missing))
                    throw new InvalidOperationException("Schema 4 accepted a missing observation: " + missing);
            var defects = new Dictionary<string, Action<NativeUiRecord>>
            {
                { "hud-cell-size:42.0,42.0!=48.0,48.0", r => r.PlannerHudCell = new[] { 42f, 42f } },
                { "hud-columns:0.0,0.0,3.0,0.0", r => r.PlannerColumnOffsets = new[] { 0f, 0f, 3f, 0f } },
                { "hud-frame-not-enclosing-row", r => r.PlannerFrameEnclosesRow = false },
                { "hud-old-rail-under-row", r => r.OldRailHiddenUnderRow = false },
                { "hud-idle-highlight:1.0", r => r.PlannerHudIdleHighlightAlpha = 1f },
                { "hud-hover-highlight:1.0,1.0,1.0,0.0", r => r.PlannerHudHoverHighlight = new[] { 1f, 1f, 1f, 0f } },
                // The press the probe once sent without entering the cell
                // rendered the idle tint (kbpnui-final-qual-05).
                { "hud-pressed-highlight:1.0,1.0,1.0,0.0", r => r.PlannerHudPressedHighlight = new[] { 1f, 1f, 1f, 0f } },
                { "hud-tooltip:none", r => { r.PlannerHudTooltipShown = false; r.PlannerHudTooltipTitle = null; } },
                { "spellbook-planner-button-disabled:Linzi", r => r.Spellbooks[0].PlannerButtonInteractable = false },
                { "spellbook-pair-height-or-baseline:Linzi", r => r.Spellbooks[0].PlannerRect = new[] { 1600f, 100f, 150f, 30f } },
                { "spellbook-top-right-entry-present", r => r.SpellbookTopRightEntryAbsent = false },
                { "popup-presenter:planner-scroll", r => r.PlannerPopupPresenter = "planner-scroll" },
                { "schema:7", r => r.SchemaVersion = 7 },
                { "pause:inventory:not-paused-before", r => r.Switches[4].PausedBefore = false },
                { "pause:not-restored", r => r.OwnedPauseRestored = false },
                { "unavailable-metamagic:no-view", r => r.Spellbooks.RemoveAt(1) },
                { "unavailable-metamagic:Hedwirg:did-not-open-planner", r => r.Spellbooks[1].UnavailablePlannerAction.PlannerOpened = false },
                { "unavailable-metamagic:Hedwirg:did-not-return", r => r.Spellbooks[1].UnavailablePlannerAction.MetamagicStillUnavailable = false },
                { "unavailable-metamagic:Hedwirg:character-changed", r => r.Spellbooks[1].UnavailablePlannerAction.UnitIdAfterReturn = "unit-1" },
                { "key-route:key:OpenInventory:dispatch-kind", r => r.Switches[3].KeyBinding.DispatchKind = "physical-key" },
                { "key-route:key:OpenInventory:binding-name", r => r.Switches[3].KeyBinding.BindingName = "OpenSpells" },
                { "key-route:key:OpenInventory:unbound", r => r.Switches[3].KeyBinding.Key = "None" },
                { "key-route:key:OpenInventory:mode", r => r.Switches[3].KeyBinding.BindingMode = "Default" },
                { "key-route:key:OpenInventory:keyboard-disabled", r => r.Switches[3].KeyBinding.KeyboardDisabled = true },
                { "key-route:key:OpenInventory:input-field-selected", r => r.Switches[3].KeyBinding.InputFieldSelected = true },
                { "key-route:key:OpenInventory:controller-gamepad", r => r.Switches[3].KeyBinding.ControllerGamepad = true },
                { "key-route:key:OpenInventory:pause-observation", r => r.Switches[3].KeyBinding.Paused = true },
                { "key-route:key:OpenInventory:does-not-work-paused", r =>
                    { r.Switches[3].PausedBefore = true; r.Switches[3].KeyBinding.Paused = true;
                      r.Switches[3].KeyBinding.WorksWhenPaused = false; } },
                { "metamagic:Linzi:did-not-open", r => r.Spellbooks[0].MetamagicAction.ModeWhileOpen = false },
                { "metamagic:Tartuccio:did-not-open", r =>
                    { r.Spellbooks[0].Unit = "Tartuccio"; r.Spellbooks[0].MetamagicAction.ButtonInvoked = false; } },
                { "metamagic:Linzi:opened-planner", r => r.Spellbooks[0].MetamagicAction.PlannerOpenWhileMetamagic = true },
                { "metamagic:Linzi:did-not-return", r => r.Spellbooks[0].MetamagicAction.ModeAfterReturn = true },
                { "metamagic:Tartuccio:did-not-return", r =>
                    { r.Spellbooks[0].Unit = "Tartuccio"; r.Spellbooks[0].MetamagicAction.ReturnInvoked = false; } },
                { "metamagic:Hedwirg:did-not-return", r =>
                    { r.Spellbooks[0].Unit = "Hedwirg"; r.Spellbooks[0].MetamagicAction.ControllerPresentAfterReturn = false; } },
                { "metamagic:Linzi:planner-entry-unavailable", r => r.Spellbooks[0].MetamagicAction.PlannerButtonOfferedAfterReturn = false },
                { "source-tabs:missing-captions", r => r.SourceTabs.RemoveAt(0) },
                { "source-tab:Abilities:too-small", r => r.SourceTabs[2].Size = 9f },
                { "source-tab:Abilities:clipped", r => r.SourceTabs[2].AvailableWidth = 20f },
                { "source-tab:Spells:too-small", r => r.SourceTabs[1].Size = float.NaN },
                { "source-tab:Other:clipped", r => r.SourceTabs[3].TextWidth = float.PositiveInfinity },
                { "source-tabs:unequal-sizes", r => r.SourceTabs[3].Size = 13f },
                { "casting-popup:presenter", r => r.CastingPopup.Presenter = "planner-scroll" },
                { "casting-popup:caster", r => r.CastingPopup.ActualCasterUnitId = "unit-2" },
                { "casting-popup:ability", r => r.CastingPopup.ActualAbilityGuid = "other-ability" },
                { "casting-popup:title", r => r.CastingPopup.ActualTitle = EarPiercingScreamTitle },
                { "casting-popup:escape", r => r.CastingPopup.ClosedByEscape = false },
                { "casting-popup:changed-plan", r => r.CastingPopup.DocumentAfter = "doc2" },
                { "catalogue-filters:missing-categories", r => r.CatalogueFilters.RemoveAt(0) },
                { "catalogue-filter:All:rows", r => r.CatalogueFilters[0].VisibleSourceIds = new[] { "src" } },
                { "catalogue-filter:Spells:rows", r => r.CatalogueFilters[1].VisibleSourceIds = new[] { "spell" } },
                { "catalogue-filter:Abilities:changed-plan", r => r.CatalogueFilters[2].DocumentAfter = "doc2" },
                { "popup-changed-plan", r => r.PlannerDocumentAfterPopup = "doc2" },
                { "popup-close-button:did-not-close", r => r.PlannerPopupClosedByCloseButton = false },
                { "popup-not-same-spell", r => r.PopupSameSpell =
                    NativeUiRecord.SameDescriptionTitle(EarPiercingScreamTitle, r.PlannerPopupTitle) },
                { "retired-label:Planner: Casting-first", r => r.PlannerVisibleLabels.Add("Planner: Casting-first") },
                { "planner-title:Buff Planner â€” casting plan", r => r.PlannerTitle = "Buff Planner â€” casting plan" },
                { "switch:inventory:planner-stayed", r => r.Switches[0].PlannerOpenAfter = true },
                { "switch:character:native-lost-full-screen-mode", r => r.Switches[1].FullScreenModeWhileNative = false },
                { "switch:spellbook:pause-changed", r => r.Switches[2].PausedAfterClose = true },
                { "switch:inventory:selection-not-kept", r => r.Switches[0].SelectedSourceAfterReopen = "other" },
                { "switch:key:OpenInventory:planner-stayed", r => r.Switches[3].PlannerOpenAfter = true },
                { "switch-no-key-route", r => r.Switches.RemoveAll(item => item.Route.StartsWith("key:", StringComparison.Ordinal)) },
                { "switch-no-hud-route", r => r.Switches.RemoveAll(item => !item.Route.StartsWith("key:", StringComparison.Ordinal)) },
                { "search-focus-native-bindings", r => r.SearchFocusSuppressesNativeBindings = false },
                { "save-failure-not-shown:none", r => r.SaveFailureShown = false },
                { "save-recovery-failed", r => r.SaveRecoveryWorked = false },
                { "save-failure-undo", r => r.SaveFailureUndoRestored = false },
                { "planning:retarget:target=unit-1", r => r.PlanningSteps[2] = "retarget:target=unit-1" },
                { "resources:not-opened", r => r.ResourcesOpened = false },
                { "resources:changed-plan", r => r.ResourcesDocumentAfter = "changed" },
                { "resources:selected-visible-pool-cells-missing", r => r.ResourcesSelectedCells.Clear() },
                { "resources:accounting-cells-wrong-layer", r => r.ResourcesReopenedCellsUseNativeLayer = false },
                { "resources:scope-transition", r => r.ResourcesSelectedAgainScope = "AllRoutinesNoRest" },
                { "resources:reopen", r => r.ResourcesReopened = false },
                { "availability-policy-caption:none", r => r.AvailabilityPolicyCaption = null },
                { "run-caption:RUN LONG", r => r.RunCaption = "RUN LONG" },
                { "planning-contract-missing:arm-retarget:ok", r => r.PlanningSteps.Remove("arm-retarget:ok") },
                { "planning-contract-missing:successive-add:ok", r => r.PlanningSteps.Remove("successive-add:ok") },
                { "planning-not-restored", r => r.PlanningRestored = false },
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
