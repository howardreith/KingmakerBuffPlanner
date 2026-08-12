using System;
using System.Collections;
using System.Linq;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.Selection;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.RuntimeTesting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class BuffPlannerUiRoot : MonoBehaviour, IPlannerRoutineRunner
    {
        private const string ObjectName = "KingmakerBuffPlanner.UiRoot";
        private static BuffPlannerUiRoot _instance;
        private PlannerUiSession _session;
        private ModLog _log;
        private string _modPath;
        private bool _enabled = true;
        private BuffPlannerUiLifecycleDiagnostics _diagnostics;
        private BuffPlannerHudButtonController _hud;
        private BuffPlannerScreenController _screen;
        private BuffPlannerQuickExecuteController _quick;
        private int _runtimeOpenCycles;
        private int _runtimeReconstructionCount;
        private int _runtimeObservedFrames;
        private UiInputIsolationProbeResult _runtimeInputProbe;
        private QuickExecutionResult _lastQuickResult;
        private bool _runtimeBaselineCaptured;
        private bool _runtimePausedBefore;
        private bool _runtimeSelectionDisabledBefore;
        private GameModeType _runtimeModeBefore;

        internal static void Ensure(string modPath, ModLog log)
        {
            if (_instance != null) return;
            var gameObject = new GameObject(ObjectName);
            DontDestroyOnLoad(gameObject);
            _instance = gameObject.AddComponent<BuffPlannerUiRoot>();
            _instance.Initialize(modPath, log);
        }

        internal static void SetEnabled(bool enabled)
        {
            if (_instance == null) return;
            _instance._enabled = enabled;
            if (!enabled) _instance.ReleasePlayerUi();
        }

        internal static void DestroyOwned()
        {
            if (_instance == null) return;
            _instance.ReleaseAll();
            Destroy(_instance.gameObject);
            _instance = null;
        }

        internal static void BeginRuntimeSmoke()
        {
            if (_instance == null) throw new InvalidOperationException("UI root is absent.");
            if (!_instance._runtimeBaselineCaptured)
            {
                _instance._runtimeBaselineCaptured = true;
                _instance._runtimePausedBefore = Game.Instance != null && Game.Instance.IsPaused;
                _instance._runtimeModeBefore = Game.Instance == null
                    ? default(GameModeType) : Game.Instance.CurrentMode;
                _instance._runtimeSelectionDisabledBefore = SelectionManager.Instance != null &&
                    SelectionManager.Instance.IsDisabled;
            }
            _instance._runtimeOpenCycles++;
            if (StaticCanvas.Instance != null) _instance._screen.Open();
        }

        internal static void ReconstructRuntimeSmoke()
        {
            if (_instance == null) throw new InvalidOperationException("UI root is absent.");
            int cycles = _instance._runtimeOpenCycles;
            int reconstructions = _instance._runtimeReconstructionCount;
            bool baselineCaptured = _instance._runtimeBaselineCaptured;
            bool pausedBefore = _instance._runtimePausedBefore;
            bool selectionDisabledBefore = _instance._runtimeSelectionDisabledBefore;
            GameModeType modeBefore = _instance._runtimeModeBefore;
            string modPath = _instance._modPath;
            ModLog log = _instance._log;
            DestroyOwned();
            Ensure(modPath, log);
            _instance._runtimeOpenCycles = cycles;
            _instance._runtimeReconstructionCount = reconstructions + 1;
            _instance._runtimeBaselineCaptured = baselineCaptured;
            _instance._runtimePausedBefore = pausedBefore;
            _instance._runtimeSelectionDisabledBefore = selectionDisabledBefore;
            _instance._runtimeModeBefore = modeBefore;
            BeginRuntimeSmoke();
            if (!_instance._hud.DispatchRuntimeClick("long"))
                throw new InvalidOperationException("Runtime Long HUD click could not be dispatched.");
        }

        internal static void CloseRuntimeSmoke()
        {
            if (_instance == null) throw new InvalidOperationException("UI root is absent.");
            _instance._screen.Close();
        }

        internal static void DispatchRuntimeInputSmoke()
        {
            if (_instance == null || !_instance._screen.IsOpen)
                throw new InvalidOperationException("Planner screen is not open.");
            using (var probe = new UiInputIsolationProbe())
                _instance._runtimeInputProbe = probe.Dispatch(_instance._screen.View);
        }

        internal static UiRootDiagnostics EndRuntimeSmoke()
        {
            if (_instance == null) throw new InvalidOperationException("UI root is absent.");
            BuffPlannerScreenView view = _instance._screen.View;
            UiInputIsolationProbeResult input = _instance._runtimeInputProbe;
            QuickFlowDiagnostics longFlow = _instance._diagnostics.GetFlow("long");
            UiRootDiagnostics result = new UiRootDiagnostics
            {
                RootCount = FindObjectsOfType<BuffPlannerUiRoot>().Length,
                RenderedOpenFrames = _instance._runtimeObservedFrames,
                OpenCloseCycles = _instance._runtimeOpenCycles,
                ScreenWidth = Screen.width,
                ScreenHeight = Screen.height,
                HudButtonCount = _instance._hud.ButtonCount,
                HudListenerCount = _instance._hud.ListenerCount,
                HudAnchorPath = _instance._hud.AnchorPath,
                FullScreenRootCount = view == null ? 0 : view.RootCount,
                FullScreenOpaque = view != null && view.IsOpaque,
                FullScreenBlocksRaycasts = view != null && view.BlocksRaycasts,
                GraphicRaycasterPresent = view != null && view.HasGraphicRaycaster,
                PlannerOpen = _instance._screen.IsOpen,
                FullScreenModeActive = Game.Instance != null &&
                    Game.Instance.IsModeActive(GameModeType.FullScreenUi),
                SelectionDisabled = SelectionManager.Instance != null &&
                    SelectionManager.Instance.IsDisabled,
                EventSystemPresent = EventSystem.current != null,
                InputLeaseAcquireCount = _instance._diagnostics.InputLeaseAcquireCount,
                InputLeaseReleaseCount = _instance._diagnostics.InputLeaseReleaseCount,
                ScreenCreateCount = _instance._diagnostics.ScreenCreateCount,
                ScreenDestroyCount = _instance._diagnostics.ScreenDestroyCount,
                HudInstallCount = _instance._diagnostics.HudInstallCount,
                HudDestroyCount = _instance._diagnostics.HudDestroyCount,
                ReconstructionCount = _instance._runtimeReconstructionCount,
                NativeCampaignUiAvailable = StaticCanvas.Instance != null,
                PointerEventCount = _instance._diagnostics.PointerEventCount,
                ScrollEventCount = _instance._diagnostics.ScrollEventCount,
                DragEventCount = _instance._diagnostics.DragEventCount,
                LongPointerEventCount = longFlow.PointerEvents,
                LongListenerCount = longFlow.Listeners,
                LongGroupResolvedCount = longFlow.GroupsResolved,
                LongPlanRevalidatedCount = longFlow.PlansRevalidated,
                LongExecutionInvokedCount = longFlow.ExecutionsInvoked,
                LongRefusalCount = longFlow.Refusals,
                LongResultPresentedCount = longFlow.ResultsPresented,
                LongResultMessage = _instance._lastQuickResult == null
                    ? string.Empty : _instance._lastQuickResult.Message,
                SetupTooltip = _instance._hud.TooltipForRuntime("setup"),
                LongTooltip = _instance._hud.TooltipForRuntime("long"),
                InputPlayerCommandCount = input == null ? -1 : input.PlayerCommandCount,
                InputMovementCommandCount = input == null ? -1 : input.MovementCommandCount,
                InputAbilityCommandCount = input == null ? -1 : input.AbilityCommandCount,
                InputSelectionEventCount = input == null ? -1 : input.SelectionEventCount,
                InputAbilityTargetEventCount = input == null ? -1 : input.AbilityTargetEventCount,
                InputSelectionUnchanged = input != null && input.SelectionUnchanged,
                InputCameraUnchanged = input != null && input.CameraUnchanged,
                InputScrollConsumed = input != null && input.ScrollConsumed,
                InputCancelConsumed = input != null && input.CancelConsumed,
                GroupSelectorChanged = input != null && input.GroupSelectorChanged,
                PausedBeforeOpen = _instance._runtimePausedBefore,
                SelectionDisabledBeforeOpen = _instance._runtimeSelectionDisabledBefore,
                ModeBeforeOpen = _instance._runtimeModeBefore.ToString()
            };
            _instance._screen.Close();
            result.InputLeaseReleaseCountAfterClose = _instance._diagnostics.InputLeaseReleaseCount;
            result.FullScreenModeActiveAfterClose = Game.Instance != null &&
                Game.Instance.IsModeActive(GameModeType.FullScreenUi);
            result.SelectionDisabledAfterClose = SelectionManager.Instance != null &&
                SelectionManager.Instance.IsDisabled;
            result.PausedAfterClose = Game.Instance != null && Game.Instance.IsPaused;
            result.ModeAfterClose = Game.Instance == null
                ? default(GameModeType).ToString() : Game.Instance.CurrentMode.ToString();
            return result;
        }

        public bool TryStart(string routineId, Action<QuickExecutionResult> completed)
        {
            if (!_enabled || _session == null || _session.IsExecuting) return false;
            StartCoroutine(_session.ExecuteRoutine(routineId, completed));
            return true;
        }

        private void Initialize(string modPath, ModLog log)
        {
            _modPath = modPath;
            _log = log;
            _session = new PlannerUiSession(modPath, log);
            _diagnostics = new BuffPlannerUiLifecycleDiagnostics();
            _quick = new BuffPlannerQuickExecuteController(this, _diagnostics, PresentQuickResult);
            _screen = new BuffPlannerScreenController(_session, _diagnostics, log,
                routineId => _quick.Execute(routineId));
            _hud = new BuffPlannerHudButtonController(_session, _diagnostics,
                () => _screen.Open(), routineId => _quick.Execute(routineId));
        }

        private void Update()
        {
            if (!_enabled) return;
            try
            {
                if (Input.GetKeyDown(KeyCode.F10))
                {
                    if (_screen.IsOpen) _screen.Close();
                    else _screen.Open();
                }
                else if (_screen.IsOpen && Input.GetKeyDown(KeyCode.Escape)) _screen.Close();
                _screen.Tick();
                _hud.TryInstall();
                _hud.Tick();
                if (_screen.IsOpen) _runtimeObservedFrames++;
            }
            catch (Exception exception)
            {
                _log.Error("Buff Planner UI update failed.", exception);
                _screen.Close();
            }
        }

        private void PresentQuickResult(QuickExecutionResult result)
        {
            _lastQuickResult = result;
            _hud.Present(result);
            _screen.Present(result);
            _log.Info("Routine UI result: " + result.RoutineId + " " +
                result.Disposition + " " + result.Message);
        }

        private void OnDisable()
        {
            ReleasePlayerUi();
        }

        private void OnDestroy()
        {
            ReleaseAll();
            if (_instance == this) _instance = null;
        }

        private void ReleasePlayerUi()
        {
            if (_screen != null) _screen.Close();
            if (_hud != null) _hud.Dispose();
        }

        private void ReleaseAll()
        {
            StopAllCoroutines();
            if (_screen != null) _screen.Dispose();
            if (_hud != null) _hud.Dispose();
            _screen = null;
            _hud = null;
            _quick = null;
        }
    }

    internal sealed class UiRootDiagnostics
    {
        internal int RootCount;
        internal int RenderedOpenFrames;
        internal int OpenCloseCycles;
        internal int ScreenWidth;
        internal int ScreenHeight;
        internal int HudButtonCount;
        internal int HudListenerCount;
        internal string HudAnchorPath;
        internal int FullScreenRootCount;
        internal bool FullScreenOpaque;
        internal bool FullScreenBlocksRaycasts;
        internal bool GraphicRaycasterPresent;
        internal bool PlannerOpen;
        internal bool FullScreenModeActive;
        internal bool SelectionDisabled;
        internal bool EventSystemPresent;
        internal int InputLeaseAcquireCount;
        internal int InputLeaseReleaseCount;
        internal int InputLeaseReleaseCountAfterClose;
        internal int ScreenCreateCount;
        internal int ScreenDestroyCount;
        internal int HudInstallCount;
        internal int HudDestroyCount;
        internal int ReconstructionCount;
        internal bool NativeCampaignUiAvailable;
        internal bool FullScreenModeActiveAfterClose;
        internal bool SelectionDisabledAfterClose;
        internal int PointerEventCount;
        internal int ScrollEventCount;
        internal int DragEventCount;
        internal int LongPointerEventCount;
        internal int LongListenerCount;
        internal int LongGroupResolvedCount;
        internal int LongPlanRevalidatedCount;
        internal int LongExecutionInvokedCount;
        internal int LongRefusalCount;
        internal int LongResultPresentedCount;
        internal string LongResultMessage;
        internal string SetupTooltip;
        internal string LongTooltip;
        internal int InputPlayerCommandCount;
        internal int InputMovementCommandCount;
        internal int InputAbilityCommandCount;
        internal int InputSelectionEventCount;
        internal int InputAbilityTargetEventCount;
        internal bool InputSelectionUnchanged;
        internal bool InputCameraUnchanged;
        internal bool InputScrollConsumed;
        internal bool InputCancelConsumed;
        internal bool GroupSelectorChanged;
        internal bool PausedBeforeOpen;
        internal bool PausedAfterClose;
        internal bool SelectionDisabledBeforeOpen;
        internal string ModeBeforeOpen;
        internal string ModeAfterClose;
    }
}
