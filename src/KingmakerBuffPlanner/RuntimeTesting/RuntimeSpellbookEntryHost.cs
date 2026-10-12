using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker.UI.ServiceWindow;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // WP2B guarded physical scenario: open the native spellbook with the
    // game's own key binding, click the owned Buff Planner button with the
    // OS pointer, close the planner with Escape; three times, then once more
    // with one simulated opener refusal to prove recovery. No casting, no
    // plan edit, no allowance.
    internal sealed partial class RuntimeTestHost
    {
        private const int SpellbookStartStep = 300;
        private const int SpellbookOpenedStep = 301;
        private const int SpellbookClickedStep = 302;
        private const int SpellbookEscapedStep = 303;
        private const double SpellbookWaitSeconds = 8;

        private readonly SpellbookEntryRecord _spellbookRecord = new SpellbookEntryRecord();
        private SpellbookCycleObservation _spellbookCycle;
        private int _spellbookCycleNumber;
        private int _spellbookCloseBefore;
        private int _spellbookOpenerBefore;
        private int _spellbookReleasesBefore;
        private int _spellbookOpensBefore;
        private int _spellbookRunsBefore;
        private string _spellbookProfilePath;
        private bool _spellbookPauseOwned;
        private bool _spellbookPauseCleanupFailed;
        private NativeUiPauseRestoration _spellbookPauseRestore;
        private readonly System.Diagnostics.Stopwatch _spellbookPauseClock = new System.Diagnostics.Stopwatch();
        private string _spellbookPendingFailure;
        private NativeUiCaptureBoundary _spellbookCapture;
        private readonly System.Diagnostics.Stopwatch _spellbookCaptureClock = new System.Diagnostics.Stopwatch();
        private string _spellbookLastCaptureFile;
        private string _spellbookCaptureState;


        private bool PhysicalSpellbookRequested
        {
            get
            {
                object expectation;
                return _request.Parameters.TryGetValue("physicalExpectation", out expectation) &&
                    string.Equals(expectation as string, "spellbook", StringComparison.Ordinal);
            }
        }

        private bool UpdateSpellbookEntry(double settled)
        {
            if (_physicalStep == SpellbookStartStep + 4)
            {
                if (!_spellbookPauseOwned)
                {
                    if (!NativeUiPauseWorldReleased())
                    {
                        if (_spellbookPauseClock.ElapsedMilliseconds < 10000) return false;
                        return FinishSpellbookEntry("pause:world-not-released");
                    }
                    _spellbookRecord.Pause.OriginalPaused = Kingmaker.Game.Instance.IsPaused;
                    _spellbookRecord.Pause.PauseAppliedAtWorld = true;
                    _spellbookPauseOwned = true;
                    Kingmaker.Game.Instance.IsPaused = true;
                }
                if (!Kingmaker.Game.Instance.IsPaused)
                {
                    if (_spellbookPauseClock.ElapsedMilliseconds < 10000) return false;
                    return FinishSpellbookEntry("pause:did-not-activate");
                }
                _spellbookRecord.Pause.PausedBeforeKey = true;
                return RequestSpellbookOpen(2);
            }
            if (_physicalStep == SpellbookStartStep + 5 || _physicalStep == SpellbookStartStep + 6)
            {
                NativeUiPauseRestoreStatus status = _spellbookPauseRestore.Observe(NativeUiPauseWorldReleased(),
                    Kingmaker.Game.Instance.IsPaused, _spellbookPauseClock.ElapsedMilliseconds,
                    value => Kingmaker.Game.Instance.IsPaused = value);
                if (status == NativeUiPauseRestoreStatus.Pending) return false;
                _spellbookRecord.Pause.OriginalPauseRestored = status == NativeUiPauseRestoreStatus.Restored &&
                    Kingmaker.Game.Instance.IsPaused == _spellbookRecord.Pause.OriginalPaused;
                _spellbookPauseOwned = status != NativeUiPauseRestoreStatus.Restored;
                if (status == NativeUiPauseRestoreStatus.TimedOut)
                {
                    _spellbookPauseCleanupFailed = true;
                    _spellbookRecord.Failures.Add("pause:restore-timeout");
                }
                if (_physicalStep == SpellbookStartStep + 6 || status == NativeUiPauseRestoreStatus.TimedOut)
                    return FinishSpellbookEntry(_spellbookPendingFailure);
                return RequestSpellbookOpen(3);
            }
            if (_spellbookCapture != null)
            {
                try
                {
                    if (SpellbookCaptureState() != _spellbookCaptureState)
                        throw new InvalidOperationException("Spellbook state changed before capture completed.");
                    NativeUiCaptureStatus status = _spellbookCapture.Observe(Time.frameCount,
                        _spellbookCaptureClock.ElapsedMilliseconds);
                    if (status == NativeUiCaptureStatus.Pending) return false;
                    if (status == NativeUiCaptureStatus.TimedOut) throw new IOException("Spellbook capture exceeded ten seconds.");
                    _spellbookRecord.Pause.Captures.Add(new JObject {
                        { "file", Path.GetFileName(_spellbookCapture.Path) },
                        { "requestFrame", _spellbookCapture.RequestFrame }, { "completedFrame", Time.frameCount } });
                    _spellbookCapture = null;
                    return false;
                }
                catch (Exception exception)
                {
                    _spellbookRecord.Pause.CaptureFailure = exception.Message;
                    return FinishSpellbookEntry("capture:" + exception.Message);
                }
            }
            BuffPlannerSpellbookEntryController entry = BuffPlannerUiRoot.SpellbookEntryForRuntime;
            if (entry == null) return FinishSpellbookEntry("spellbook-entry-controller-missing");
            if (_physicalStep == SpellbookStartStep)
            {
                if (_physicalClock.Elapsed.TotalSeconds < 4) return false;
                if (BuffPlannerUiRoot.NativeEscMenuOpenForRuntime)
                {
                    if (++_physicalMenuCloseAttempts > 3) return FinishSpellbookEntry("menu-veil");
                    return RequestPhysical("sb-menu-close-" + _physicalMenuCloseAttempts,
                        "key-escape", Vector2.zero, null, SpellbookStartStep);
                }
                _spellbookRecord.SchemaVersion = 2;
                _spellbookRecord.Pause = new SpellbookPauseObservation();
                _spellbookRecord.RunId = _request.RunId;
                _spellbookRecord.SourceCommit = _request.ExpectedCommit;
                _spellbookRecord.PackageSha256 = _request.ExpectedPackageSha256;
                _spellbookRecord.DllSha256 = _request.ExpectedDllSha256;
                _spellbookRecord.AssemblyMvid = typeof(RuntimeTestHost).Assembly.ManifestModule
                    .ModuleVersionId.ToString("D");
                _spellbookRecord.ScreenWidth = Screen.width;
                _spellbookRecord.ScreenHeight = Screen.height;
                _spellbookRecord.CastingFirst = BuffPlannerUiRoot.CastingFirstActiveForRuntime;
                _spellbookRecord.PlannerClosedAtStart = !BuffPlannerUiRoot.IsCastingWorkspaceOpen &&
                    BuffPlannerUiRoot.PlannerRootCountForRuntime() == 0;
                Kingmaker.UI.KeyboardAccess.Binding binding = Kingmaker.Game.Instance.Keyboard
                    .GetBindingByName("OpenSpells");
                if (binding == null) return FinishSpellbookEntry("open-spells-binding-missing");
                _spellbookRecord.OpenSpellsBinding = binding.Key + (binding.IsCtrlDown ? "+ctrl" : "") +
                    (binding.IsShiftDown ? "+shift" : "") + (binding.IsAltDown ? "+alt" : "");
                if (binding.IsCtrlDown || binding.IsShiftDown || binding.IsAltDown ||
                    binding.Key < KeyCode.A || binding.Key > KeyCode.Z)
                    return FinishSpellbookEntry("open-spells-binding-not-a-plain-letter:" +
                        _spellbookRecord.OpenSpellsBinding);
                // KeyCode.A..Z map onto the Windows virtual keys 0x41..0x5A.
                _spellbookRecord.OpenSpellsVirtualKey = 0x41 + (binding.Key - KeyCode.A);
                CastingWorkspaceInputs inputs = BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime();
                string campaign = Kingmaker.Game.Instance.Player.GameId;
                _spellbookRecord.DocumentBefore = new CastingWorkspaceSession(_modEntry.Path, campaign,
                    new DisabledCastingDispatchBoundary()).DocumentIntentSignature();
                _spellbookProfilePath = new CastingPlanRepository(_modEntry.Path).GetProfilePath(campaign);
                _spellbookRecord.ProfileBeforeSha256 = SpellbookFileHash(_spellbookProfilePath);
                _spellbookRecord.ResourcesBefore = ProblemResources(inputs);
                _spellbookRecord.EffectsBefore = ProblemEffects(inputs);
                _spellbookRunsBefore = BuffPlannerUiRoot.CastingRunsStartedForRuntime;
                BuffPlannerUiRoot.BeginPhysicalInputProbe();
                return RequestSpellbookOpen(1);
            }
            if (_physicalStep == SpellbookOpenedStep)
            {
                // The controller attaches on its cadence and re-places once
                // the native layout settles; wait for both.
                bool shown = NativeSpellbookShown();
                if ((!shown || !entry.IsAttached || settled < 1.5) && settled < SpellbookWaitSeconds)
                    return false;
                ObserveSpellbookOffered(entry, shown);
                if (_spellbookCycleNumber == 2) _spellbookRecord.Pause.PausedWithSpellbook = Kingmaker.Game.Instance.IsPaused;
                if (RequestSpellbookCapture("spellbook-" + SpellbookEntryRecord.CycleSuffix(_spellbookCycleNumber) + "-open.png")) return false;
                if (!_spellbookCycle.ButtonAttached || !_spellbookCycle.ButtonX.HasValue)
                    return FinishSpellbookEntry("spellbook-button-not-offered:" + _spellbookCycleNumber);
                if (_spellbookCycle.FaultInjected)
                {
                    _spellbookRecord.FaultArmed = SpellbookHandoffFaults.ArmOpenRefusalForRuntime();
                    if (!_spellbookRecord.FaultArmed) return FinishSpellbookEntry("fault-not-armable");
                }
                _spellbookCloseBefore = entry.NativeCloseInvocationsForRuntime;
                _spellbookOpenerBefore = entry.OpenerInvocationsForRuntime;
                _spellbookReleasesBefore = entry.NativeReleasesForRuntime;
                _spellbookOpensBefore = BuffPlannerUiRoot.CastingWorkspaceOpensForRuntime;
                return RequestPhysical("sb-click-" + SpellbookEntryRecord.CycleSuffix(_spellbookCycleNumber),
                    "click", new Vector2(_spellbookCycle.ButtonX.Value, _spellbookCycle.ButtonY.Value),
                    null, SpellbookClickedStep);
            }
            if (_physicalStep == SpellbookClickedStep)
            {
                SpellbookHandoffState state = entry.HandoffState;
                bool settledHandoff = state == SpellbookHandoffState.Completed ||
                    state == SpellbookHandoffState.Failed;
                // A recovered failure reopens the native spellbook, which the
                // controller then offers its button on again.
                bool recovered = !_spellbookCycle.FaultInjected || (NativeSpellbookShown() &&
                    entry.IsAttached && entry.OwnedButtonForRuntime.interactable);
                if ((!settledHandoff || !recovered || settled < 1.0) && settled < SpellbookWaitSeconds)
                    return false;
                ObserveSpellbookClicked(entry);
                if (_spellbookCycleNumber == 2) _spellbookRecord.Pause.PausedWithPlanner = Kingmaker.Game.Instance.IsPaused;
                if (RequestSpellbookCapture("spellbook-" + SpellbookEntryRecord.CycleSuffix(_spellbookCycleNumber) + "-after-click.png")) return false;
                if (_spellbookCycleNumber == 2)
                    return RequestPhysical("sb-switch-2", "key", Vector2.zero,
                        ",\"vk\":" + _spellbookRecord.OpenSpellsVirtualKey, SpellbookStartStep + 7);
                return RequestPhysical("sb-escape-" + SpellbookEntryRecord.CycleSuffix(_spellbookCycleNumber),
                    "key-escape", Vector2.zero, null, SpellbookEscapedStep);
            }
            if (_physicalStep == SpellbookStartStep + 7)
            {
                if ((!NativeSpellbookShown() || BuffPlannerUiRoot.IsCastingWorkspaceOpen || settled < 1.5) &&
                    settled < SpellbookWaitSeconds) return false;
                _spellbookRecord.Pause.PlannerClosedAfterSwitch = !BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                _spellbookRecord.Pause.SpellbookShownAfterSwitch = NativeSpellbookShown();
                _spellbookRecord.Pause.LeaseReleasedAfterSwitch = !BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime;
                _spellbookRecord.Pause.PausedAfterSwitch = Kingmaker.Game.Instance.IsPaused;
                if (RequestSpellbookCapture("spellbook-2-paused-switch.png")) return false;
                return RequestPhysical("sb-escape-2", "key-escape", Vector2.zero, null, SpellbookEscapedStep);
            }
            if (_physicalStep == SpellbookEscapedStep)
            {
                if (settled < 1.5) return false;
                _spellbookCycle.PlannerClosedAfterEscape = !BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                _spellbookCycle.InputLeaseReleasedAfterEscape =
                    !BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime;
                _spellbookCycle.SpellbookShownAfterEscape = NativeSpellbookShown();
                _spellbookCycle.NativeOwnerActiveAfterEscape = entry.NativeOwnerActiveForRuntime;
                _spellbookCycle.NativeMenuOpenAfterEscape = BuffPlannerUiRoot.NativeEscMenuOpenForRuntime;
                _spellbookCycle.OwnedButtonsAfterEscape = OwnedSpellbookButtons();
                _spellbookCycle.PlannerRootsAfterEscape = BuffPlannerUiRoot.PlannerRootCountForRuntime();
                if (_spellbookCycleNumber == 2)
                {
                    _spellbookRecord.Pause.PausedAfterEscape = Kingmaker.Game.Instance.IsPaused;
                    BeginSpellbookPauseRestore(SpellbookStartStep + 5);
                    return false;
                }
                if (_spellbookCycleNumber <= SpellbookEntryRecord.OrdinaryCycles)
                    return RequestSpellbookOpen(_spellbookCycleNumber + 1);
                if (RequestSpellbookCapture("spellbook-closed.png")) return false;
                return FinishSpellbookEntry(null);
            }
            return false;
        }

        private bool RequestSpellbookOpen(int cycle)
        {
            if (cycle == 2 && !_spellbookRecord.Pause.PauseAppliedAtWorld)
            {
                _spellbookPauseClock.Restart();
                _physicalStep = SpellbookStartStep + 4;
                return false;
            }
            _spellbookCycleNumber = cycle;
            _spellbookCycle = new SpellbookCycleObservation {
                Cycle = cycle, FaultInjected = cycle > SpellbookEntryRecord.OrdinaryCycles };
            _spellbookRecord.Cycles.Add(_spellbookCycle);
            return RequestPhysical("sb-open-" + SpellbookEntryRecord.CycleSuffix(cycle), "key",
                Vector2.zero, ",\"vk\":" + _spellbookRecord.OpenSpellsVirtualKey, SpellbookOpenedStep);
        }

        private void ObserveSpellbookOffered(BuffPlannerSpellbookEntryController entry, bool shown)
        {
            _spellbookCycle.SpellbookShown = shown;
            _spellbookCycle.PlannerOpenBeforeClick = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
            Button button = entry.OwnedButtonForRuntime;
            _spellbookCycle.ButtonAttached = button != null && button.gameObject.activeInHierarchy;
            _spellbookCycle.ButtonInteractable = button != null && button.IsInteractable();
            _spellbookCycle.OwnedButtonCount = OwnedSpellbookButtons();
            _spellbookCycle.ListenerCount = RuntimeListenerCount(button);
            _spellbookCycle.Placement = entry.PlacementForRuntime;
            if (button == null) return;
            var rect = (RectTransform)button.transform;
            Canvas canvas = button.GetComponentInParent<Canvas>();
            Camera camera = canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : canvas.rootCanvas.worldCamera;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            Vector2 center = (min + max) * 0.5f;
            _spellbookCycle.ButtonX = center.x;
            _spellbookCycle.ButtonY = center.y;
            _spellbookCycle.ButtonWidth = max.x - min.x;
            _spellbookCycle.ButtonHeight = max.y - min.y;
            // What the pointer actually reaches at that point: the first
            // EventSystem raycast hit must belong to the owned button.
            if (EventSystem.current != null)
            {
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) {
                    position = center }, hits);
                if (hits.Count > 0 && hits[0].gameObject != null)
                {
                    _spellbookCycle.TopmostHitIsOwned = hits[0].gameObject.transform.IsChildOf(button.transform);
                    _spellbookCycle.TopmostHitPath = HierarchyPath(hits[0].gameObject.transform);
                }
            }
        }

        private void ObserveSpellbookClicked(BuffPlannerSpellbookEntryController entry)
        {
            _spellbookCycle.NativeCloseInvocations = entry.NativeCloseInvocationsForRuntime - _spellbookCloseBefore;
            _spellbookCycle.OpenerInvocations = entry.OpenerInvocationsForRuntime - _spellbookOpenerBefore;
            _spellbookCycle.NativeReleases = entry.NativeReleasesForRuntime - _spellbookReleasesBefore;
            _spellbookCycle.WorkspaceOpens = BuffPlannerUiRoot.CastingWorkspaceOpensForRuntime - _spellbookOpensBefore;
            _spellbookCycle.HandoffState = entry.HandoffState.ToString();
            _spellbookCycle.HandoffFailure = entry.HandoffFailure;
            _spellbookCycle.PlannerOpenAfterClick = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
            _spellbookCycle.InputLeaseHeldAfterClick = BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime;
            _spellbookCycle.SpellbookShownAfterClick = NativeSpellbookShown();
            _spellbookCycle.ServiceWindowShownAfterClick = NativeServiceWindowShown();
            _spellbookCycle.PlannerOwnsFullScreenAfterClick = Kingmaker.Game.Instance != null &&
                Kingmaker.Game.Instance.IsModeActive(Kingmaker.GameModes.GameModeType.FullScreenUi) &&
                BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime;
            _spellbookCycle.PlannerRootsAfterClick = BuffPlannerUiRoot.PlannerRootCountForRuntime();
            Button button = entry.OwnedButtonForRuntime;
            _spellbookCycle.ButtonRestoredAfterRecovery = button != null &&
                button.gameObject.activeInHierarchy && button.IsInteractable();
            _spellbookCycle.OwnedButtonsAfterRecovery = OwnedSpellbookButtons();
        }

        private static bool NativeServiceWindowShown()
        {
            Kingmaker.Game game = Kingmaker.Game.Instance;
            ServiceWindowController controller = game == null || game.UI == null ? null : game.UI.ServiceWindow;
            return controller != null && controller.WindowTabs != null && controller.WindowTabs.IsShow;
        }

        private static bool NativeSpellbookShown()
        {
            Kingmaker.Game game = Kingmaker.Game.Instance;
            ServiceWindowController controller = game == null || game.UI == null ? null : game.UI.ServiceWindow;
            if (controller == null || controller.WindowTabs == null || !controller.WindowTabs.IsShow) return false;
            FullScreenTabsWindow.SubPair[] pairs = controller.WindowTabs.SubWindowsList;
            int index = (int)ServiceWindowTabs.ServiceWindowState.SpellBook;
            return pairs != null && pairs.Length > index && pairs[index] != null &&
                pairs[index].SubWindow != null && pairs[index].SubWindow.IsShow;
        }

        // Owned buttons anywhere under the native service window (a stray
        // from an earlier controller would be counted too).
        private static int OwnedSpellbookButtons()
        {
            Kingmaker.Game game = Kingmaker.Game.Instance;
            ServiceWindowController controller = game == null || game.UI == null ? null : game.UI.ServiceWindow;
            if (controller == null) return 0;
            int count = 0;
            foreach (Transform node in controller.GetComponentsInChildren<Transform>(true))
                if (node != null && node.name == BuffPlannerSpellbookEntryController.ButtonName) count++;
            return count;
        }

        // Bounded reflection over UnityEventBase.m_Calls.m_RuntimeCalls; -1
        // when the contract is not as expected (judged as a violation).
        private static int RuntimeListenerCount(Button button)
        {
            if (button == null) return 0;
            try
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo callsField = typeof(UnityEngine.Events.UnityEventBase).GetField("m_Calls", flags);
                object calls = callsField == null ? null : callsField.GetValue(button.onClick);
                FieldInfo runtimeField = calls == null ? null : calls.GetType().GetField("m_RuntimeCalls", flags);
                var runtime = runtimeField == null ? null : runtimeField.GetValue(calls) as System.Collections.ICollection;
                return runtime == null ? -1 : runtime.Count + button.onClick.GetPersistentEventCount();
            }
            catch (Exception) { return -1; }
        }

        private static string HierarchyPath(Transform node)
        {
            var parts = new List<string>();
            for (Transform current = node; current != null && parts.Count < 12; current = current.parent)
                parts.Insert(0, current.name);
            return string.Join("/", parts.ToArray());
        }

        private static string SpellbookFileHash(string path)
        {
            return File.Exists(path) ? ProblemFileHash(path) : "absent";
        }

        private bool RequestSpellbookCapture(string file)
        {
            if (_spellbookLastCaptureFile == file) return false;
            _spellbookLastCaptureFile = file;
            _spellbookCaptureState = SpellbookCaptureState();
            _spellbookCapture = new NativeUiCaptureBoundary(Path.Combine(_request.EvidenceDirectory, file), Time.frameCount, 10000);
            _spellbookCaptureClock.Restart();
            CaptureScreenshot(_spellbookCapture.Path);
            return true;
        }

        private static string SpellbookCaptureState()
        {
            return GameModeName() + "|" + BuffPlannerUiRoot.IsCastingWorkspaceOpen + "|" +
                NativeSpellbookShown() + "|" + Kingmaker.Game.Instance.IsPaused;
        }

        private void BeginSpellbookPauseRestore(int step)
        {
            _spellbookPauseRestore = new NativeUiPauseRestoration(_spellbookRecord.Pause.OriginalPaused, 10000);
            _spellbookPauseClock.Restart();
            _physicalStep = step;
        }

        private void RestoreSpellbookPauseAtShutdown()
        {
            if (!_spellbookPauseOwned) return;
            try { if (BuffPlannerUiRoot.IsCastingWorkspaceOpen) CloseProbeWorkspace(); }
            finally
            {
                try { if (NativeServiceWindowShown()) CloseNativeServiceWindow(); }
                finally
                {
                    Kingmaker.Game.Instance.IsPaused = _spellbookRecord.Pause.OriginalPaused;
                    _spellbookRecord.Pause.OriginalPauseRestored = Kingmaker.Game.Instance.IsPaused == _spellbookRecord.Pause.OriginalPaused;
                    _spellbookPauseOwned = false;
                }
            }
        }

        private bool FinishSpellbookEntry(string failure)
        {
            if (_spellbookPauseOwned && !_spellbookPauseCleanupFailed)
            {
                _spellbookPendingFailure = failure;
                if (BuffPlannerUiRoot.IsCastingWorkspaceOpen) CloseProbeWorkspace();
                if (NativeServiceWindowShown()) CloseNativeServiceWindow();
                BeginSpellbookPauseRestore(SpellbookStartStep + 6);
                return false;
            }
            if (failure != null) _spellbookRecord.Failures.Add(failure);
            try
            {
                string campaign = Kingmaker.Game.Instance.Player.GameId;
                _spellbookRecord.DocumentAfter = new CastingWorkspaceSession(_modEntry.Path, campaign,
                    new DisabledCastingDispatchBoundary()).DocumentIntentSignature();
                _spellbookRecord.ProfileAfterSha256 = SpellbookFileHash(_spellbookProfilePath ??
                    new CastingPlanRepository(_modEntry.Path).GetProfilePath(campaign));
                CastingWorkspaceInputs after = BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime();
                _spellbookRecord.ResourcesAfter = ProblemResources(after);
                _spellbookRecord.EffectsAfter = ProblemEffects(after);
            }
            catch (Exception exception)
            {
                _spellbookRecord.Failures.Add("after-state-unreadable:" + exception.Message);
            }
            _spellbookRecord.RunsStarted = BuffPlannerUiRoot.CastingRunsStartedForRuntime - _spellbookRunsBefore;
            UiInputIsolationProbeResult isolation = BuffPlannerUiRoot.EndPhysicalInputProbe();
            if (isolation != null)
            {
                _spellbookRecord.MovementCommands = isolation.MovementCommandCount;
                _spellbookRecord.AbilityCommands = isolation.AbilityCommandCount;
                _spellbookRecord.AbilityTargetEvents = isolation.AbilityTargetEventCount;
                _spellbookRecord.SelectionUnchanged = isolation.SelectionUnchanged;
            }
            else _spellbookRecord.Failures.Add("isolation-probe-missing");
            _spellbookRecord.Acknowledged.Clear();
            _spellbookRecord.Acknowledged.AddRange(_physicalRecord.Acknowledged);
            PublishSpellbookRecord();
            _completed = true;
            return true;
        }

        private void PublishSpellbookRecord()
        {
            _physicalPublished = true;
            var serializer = JsonSerializer.Create(new JsonSerializerSettings {
                ContractResolver = new CamelCasePropertyNamesContractResolver() });
            JObject record = JObject.FromObject(_spellbookRecord, serializer);
            record["schemaVersion"] = _spellbookRecord.SchemaVersion;
            record["violations"] = new JArray(_spellbookRecord.Violations());
            AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "physical-spellbook-entry.json"),
                record.ToString(Formatting.Indented));
            _log.Info("[KBP-SPELLBOOK-RUN] " + string.Join("|", _spellbookRecord.Violations().ToArray()));
        }
    }
}
