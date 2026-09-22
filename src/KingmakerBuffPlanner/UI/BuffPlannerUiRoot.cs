using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UI.Selection;
using Kingmaker.PubSubSystem;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.RuntimeTesting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class BuffPlannerUiRoot : MonoBehaviour, IPlannerRoutineRunner,
        ISceneHandler, IAreaLoadingStagesHandler, IAreaActivationHandler
    {
        private const string ObjectName = "KingmakerBuffPlanner.UiRoot";
        private static BuffPlannerUiRoot _instance;
        private PlannerUiSession _session;
        private ModLog _log;
        private string _modPath;
        private bool _enabled = true;
        private bool _quickStartPending;
        private BuffPlannerUiLifecycleDiagnostics _diagnostics;
        private BuffPlannerHudButtonController _hud;
        private BuffPlannerScreenController _screen;
        private BuffPlannerInputLease _workspaceInputLease;
        private CastingWorkspaceSession _castingWorkspaceSession;
        private CastingWorkspaceScreenView _castingWorkspace;
        private BuffPlannerSpellbookEntryController _spellbookEntry;
        private BuffPlannerQuickExecuteController _quick;
        private int _runtimeOpenCycles;
        private int _runtimeReconstructionCount;
        private int _runtimeObservedFrames;
        private UiInputIsolationProbeResult _runtimeInputProbe;
        private UiInputIsolationProbe _runtimePhysicalProbe;
        private UiInputIsolationProbeResult _runtimePhysicalInput;
        private readonly Dictionary<string, QuickExecutionResult> _runtimeQuickResults =
            new Dictionary<string, QuickExecutionResult>(StringComparer.Ordinal);
        private QuickExecutionResult _runtimeFirstLongResult;
        private QuickExecutionResult _lastQuickResult;
        private bool _runtimeBaselineCaptured;
        private bool _runtimePausedBefore;
        private bool _runtimeSelectionDisabledBefore;
        private GameModeType _runtimeModeBefore;
        private bool _runtimeReconstructionPending;
        private IDisposable _eventSubscription;
        private bool _disposed;
        private readonly HudInstallInvalidationGate _hudInstallGate =
            new HudInstallInvalidationGate();
        private int _tickCount;
        private int _lifecycleSignalCount;
        private int _lastLoggedHudIdentity = int.MinValue;
        private bool _lastLoggedHudActive;
        private int _hudInstallExceptionCount;
        private int _hudTickExceptionCount;

        public int Priority { get { return 400; } }

        internal static void Ensure(string modPath, ModLog log)
        {
            if (_instance != null) return;
            var gameObject = new GameObject(ObjectName);
            DontDestroyOnLoad(gameObject);
            _instance = gameObject.AddComponent<BuffPlannerUiRoot>();
            _instance.Initialize(modPath, log);
            log.Info("[KBP-BOOT] controller constructed;instance=" +
                gameObject.GetInstanceID() + ";retained=static;dontDestroyOnLoad=true.");
        }

        internal static void SetEnabled(bool enabled)
        {
            if (_instance == null) return;
            _instance._enabled = enabled;
            if (enabled) _instance.RequestHudInstall("mod-enabled", true);
            else
            {
                _instance.SuspendHudInstall("mod-disabled");
                _instance.ReleasePlayerUi();
            }
        }

        internal static void DestroyOwned()
        {
            if (_instance == null) return;
            _instance.ReleaseAll();
            Destroy(_instance.gameObject);
            _instance = null;
        }

        internal static void HandlePlannerHotkey()
        {
            if (_instance == null)
            {
                return;
            }
            _instance.RequestHudInstall("planner-hotkey", false);
            // The hotkey TOGGLES the casting workspace: closing must not
            // fall through to OpenSetup, which would destroy and silently
            // reconstruct the candidate session (review R2).
            if (_instance._castingWorkspace != null)
            {
                _instance.CloseCastingWorkspace();
                _instance._log.Info("[KBP-BOOT] casting workspace close requested;source=PlannerHotkey.");
                return;
            }
            if (_instance._screen.LifecycleState != PlannerScreenLifecycleState.Closed)
            {
                _instance._screen.Close();
                _instance._log.Info("[KBP-BOOT] full-screen close requested;source=PlannerHotkey.");
                return;
            }
            if (!_instance.OpenSetup())
                _instance.LogUiUnavailable(_instance._screen.LastFailure);
        }

        internal static void TickOwned(float deltaTime)
        {
            if (_instance != null) _instance.Tick(deltaTime);
        }

        internal static bool IsHudInstalled
        {
            get { return _instance != null && _instance._hud != null && _instance._hud.IsInstalled; }
        }

        internal static string HudFailure
        {
            get { return _instance == null || _instance._hud == null
                ? string.Empty : _instance._hud.LastFailure; }
        }

        internal static bool IsScreenOpen
        {
            get { return _instance != null && _instance._screen != null && _instance._screen.IsOpen; }
        }

        // The casting-first workspace view is open. Distinct from
        // IsScreenOpen, which reflects only the legacy catalog screen;
        // qualification must never accept the legacy screen as workspace
        // evidence.
        internal static bool IsCastingWorkspaceOpen
        {
            get { return _instance != null && _instance._castingWorkspace != null; }
        }

        // Runtime-only close seam for diagnostic bisection: closing the
        // workspace without routing through HandlePlannerHotkey (whose
        // toggle would immediately reopen it through OpenSetup).
        internal static void CloseCastingWorkspaceForRuntime()
        {
            if (_instance != null) _instance.CloseCastingWorkspace();
        }

        // Runtime seams for the guarded interaction scenario: the LIVE
        // session owned by the open workspace view and fresh production
        // inputs. The scenario issues canonical session commands (labeled
        // direct session calls, never physical-input claims) and captures
        // the rendered view states between steps.
        internal static CastingWorkspaceSession CastingWorkspaceSessionForRuntime()
        {
            return _instance == null ? null : _instance._castingWorkspaceSession;
        }

        internal static CastingWorkspaceInputs CastingWorkspaceInputsForRuntime()
        {
            return _instance == null ? null : _instance.BuildCastingWorkspaceInputs();
        }

        // Invokes the PRODUCTION onClick wiring of a named workspace button
        // (review F2 control path). Returns null when the button does not
        // exist; the caller records that honestly instead of silently
        // falling back.
        internal static string CastingWorkspaceInvokeControlForRuntime(
            string buttonName)
        {
            CastingWorkspaceScreenView view =
                _instance == null ? null : _instance._castingWorkspace;
            GameObject root = view == null ? null : view.RootObject;
            if (root == null) return "workspace-missing";
            UnityEngine.UI.Button[] buttons =
                root.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            bool seen = false;
            foreach (UnityEngine.UI.Button button in buttons)
                if (button != null && string.Equals(button.name, buttonName,
                        StringComparison.Ordinal))
                {
                    seen = true;
                    // Callback coverage is not reachability: a control the
                    // player cannot see or press is reported as such
                    // (review G4).
                    if (!button.gameObject.activeInHierarchy)
                        return "control-inactive:" + buttonName;
                    if (!button.interactable)
                        return "control-not-interactable:" + buttonName;
                    button.onClick.Invoke();
                    return "invoked";
                }
            return seen ? "control-unusable:" + buttonName
                : "control-missing:" + buttonName;
        }

        // Runtime presentation evidence for the workspace root: whether the
        // GameObject hierarchy is actually active, sized, and carrying
        // renderable text. A non-null view field alone proved insufficient
        // (run casting-ws-root-200200: object present, screen showed only
        // the game HUD).
        internal static string CastingWorkspacePresentationEvidence()
        {
            CastingWorkspaceScreenView view =
                _instance == null ? null : _instance._castingWorkspace;
            if (view == null) return "workspace=missing";
            GameObject root = view.RootObject;
            if (root == null) return "workspace=root-null";
            RectTransform rect = (RectTransform)root.transform;
            var sb = new System.Text.StringBuilder();
            sb.Append("workspace=present")
                .Append(";activeSelf=").Append(root.activeSelf)
                .Append(";activeInHierarchy=").Append(root.activeInHierarchy)
                .Append(";childCount=").Append(root.transform.childCount)
                .Append(";rect=").Append(rect.rect.width.ToString("F0"))
                .Append("x").Append(rect.rect.height.ToString("F0"))
                .Append(";parent=").Append(root.transform.parent == null
                    ? "null" : root.transform.parent.name);
            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas != null)
                sb.Append(";canvasEnabled=").Append(canvas.enabled)
                    .Append(";sortingOrder=").Append(canvas.sortingOrder)
                    .Append(";overrideSorting=").Append(canvas.overrideSorting);
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            if (group != null)
                sb.Append(";alpha=").Append(group.alpha.ToString("F2"))
                    .Append(";blocksRaycasts=").Append(group.blocksRaycasts)
                    .Append(";interactable=").Append(group.interactable);
            UnityEngine.UI.Text[] texts =
                root.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            int renderableTexts = 0;
            foreach (UnityEngine.UI.Text text in texts)
                if (text != null && text.gameObject.activeInHierarchy &&
                    text.font != null && text.enabled) renderableTexts++;
            sb.Append(";texts=").Append(texts.Length)
                .Append(";renderableTexts=").Append(renderableTexts)
                .Append(";rootLayer=").Append(root.layer);
            Canvas rootCanvas = canvas == null ? null : canvas.rootCanvas;
            sb.Append(";rootCanvas=").Append(rootCanvas == null
                    ? "null" : rootCanvas.name)
                .Append(";rootMode=").Append(rootCanvas == null
                    ? "null" : rootCanvas.renderMode.ToString())
                .Append(";rootOrder=").Append(rootCanvas == null
                    ? "null" : rootCanvas.sortingOrder.ToString());
            // Per-node dump (bounded): which graphics exist, their rect
            // sizes, and their effective colors — the discriminator for
            // "blocker renders but frame/texts invisible".
            var nodes = new System.Text.StringBuilder();
            AppendNodeDump(root.transform, 0, 3, nodes);
            sb.Append(";nodes=").Append(nodes);
            return sb.ToString();
        }

        private static void AppendNodeDump(
            Transform node, int depth, int maxDepth, System.Text.StringBuilder sink)
        {
            if (node == null || depth > maxDepth || sink.Length > 1600) return;
            var rect = node as RectTransform;
            sink.Append('/').Append(node.name);
            if (node.gameObject.activeSelf) sink.Append("@on"); else sink.Append("@OFF");
            if (rect != null)
                sink.Append(rect.rect.width.ToString("F0")).Append("x")
                    .Append(rect.rect.height.ToString("F0"));
            UnityEngine.UI.Image image =
                node.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
                sink.Append(":Img").Append(image.enabled ? "+" : "-")
                    .Append("a").Append(image.color.a.ToString("F2"));
            UnityEngine.UI.Text text = node.GetComponent<UnityEngine.UI.Text>();
            if (text != null)
                sink.Append(":Txt").Append(text.enabled ? "+" : "-")
                    .Append("a").Append(text.color.a.ToString("F2"))
                    .Append("l").Append(text.text.Length);
            for (int i = 0; i < node.childCount; i++)
                AppendNodeDump(node.GetChild(i), depth + 1, maxDepth, sink);
        }

        internal static bool IsRuntimeReconstructionPending
        {
            get { return _instance != null && _instance._runtimeReconstructionPending; }
        }

        internal static string GetSnapshot()
        {
            if (_instance == null) return "controller=absent;plannerHotkey=polling-owned-by-Main";
            BuffPlannerUiRoot root = _instance;
            string mode = Game.Instance == null ? "game-null" : Game.Instance.CurrentMode.ToString();
            return "controller=" + root.gameObject.GetInstanceID() +
                ";enabled=" + root._enabled +
                ";disposed=" + root._disposed +
                ";ticks=" + root._tickCount +
                ";eventBusSubscribed=" + (root._eventSubscription != null) +
                ";lifecycleSignals=" + root._lifecycleSignalCount +
                ";mode=" + mode +
                ";staticCanvas=" + (StaticCanvas.Instance != null) +
                ";eventSystem=" + (EventSystem.current == null ? "null" : EventSystem.current.name) +
                ";hudInstalled=" + (root._hud != null && root._hud.IsInstalled) +
                ";hudCandidate=" + (root._hud == null ? 0 : root._hud.RootInstanceId) +
                ";hudAttempts=" + (root._hud == null ? 0 : root._hud.InstallAttempts) +
                ";hudCandidateCreates=" + (root._hud == null ? 0 : root._hud.CandidateCreateCount) +
                ";hudState=" + root._hudInstallGate.State +
                ";hudInstallRequested=" + root._hudInstallGate.IsRequested +
                ";hudRetryScheduled=" + root._hudInstallGate.IsRetryScheduled +
                ";hudRetryFrames=" + root._hudInstallGate.RetryFramesRemaining +
                ";hudInstallInvalidations=" + root._hudInstallGate.RequestCount +
                ";hudInstallDispatches=" + root._hudInstallGate.AttemptCount +
                ";hudRetryRearms=" + root._hudInstallGate.RetryArmCount +
                ";hudRetryDispatches=" + root._hudInstallGate.RetryDispatchCount +
                ";hudHostIdentity=" + root._hudInstallGate.HostIdentity +
                ";hudHostActive=" + root._hudInstallGate.HostActive +
                ";hudAttemptResult=" + root._hudInstallGate.LastAttemptResult +
                ";hudCandidateResult=" + root._hudInstallGate.LastCandidateResult +
                ";hudTransition=" + root._hudInstallGate.LastTransition +
                ";hudAnchorIdentity=" + (root._hud == null ? 0 : root._hud.AnchorInstanceId) +
                ";hudNativeClusterIdentity=" + (root._hud == null ? 0 : root._hud.NativeClusterInstanceId) +
                ";hudRootActive=" + (root._hud != null && root._hud.RootActive) +
                ";hudAnchorActive=" + (root._hud != null && root._hud.AnchorActive) +
                ";hudNativeClusterActive=" + (root._hud != null && root._hud.NativeClusterActive) +
                ";hudHostingFailure=" + (root._hud == null ? "controller-null" : root._hud.HostingChainFailure) +
                ";hudValidationFailures=" + (root._hud == null ? 0 : root._hud.CandidateValidationFailures) +
                ";hudLastValidationFailure=" + (root._hud == null ? "controller-null" : root._hud.LastValidationFailure) +
                ";hudInstallExceptions=" + root._hudInstallExceptionCount +
                ";hudTickExceptions=" + root._hudTickExceptionCount +
                ";hudFailure=" + (root._hud == null ? "controller-null" : root._hud.LastFailure) +
                ";screenState=" + (root._screen == null ? "controller-null" : root._screen.LifecycleState.ToString()) +
                ";screenFailure=" + (root._screen == null ? "controller-null" : root._screen.LastFailure) +
                ";plannerHotkey=" + PlannerHotkey.Binding + ";armed-in-Main.OnUpdate";
        }

        internal static void BeginRuntimeSmoke()
        {
            if (_instance == null) throw new InvalidOperationException("UI root is absent.");
            CaptureRuntimeBaseline();
            _instance._runtimeOpenCycles++;
            if (StaticCanvas.Instance != null) _instance.OpenSetup();
        }

        internal static void CaptureRuntimeBaseline(bool refresh = false)
        {
            if (_instance == null) throw new InvalidOperationException("UI root is absent.");
            if (refresh || !_instance._runtimeBaselineCaptured)
            {
                _instance._runtimeBaselineCaptured = true;
                _instance._runtimePausedBefore = Game.Instance != null && Game.Instance.IsPaused;
                _instance._runtimeModeBefore = Game.Instance == null
                    ? default(GameModeType) : Game.Instance.CurrentMode;
                _instance._runtimeSelectionDisabledBefore = SelectionManager.Instance != null &&
                    SelectionManager.Instance.IsDisabled;
            }
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
            _instance._runtimeReconstructionPending = true;
            _instance.RequestHudInstall("runtime-root-reconstruction", false);
        }

        internal static void CloseRuntimeSmoke()
        {
            if (_instance == null) throw new InvalidOperationException("UI root is absent.");
            _instance._screen.Close();
        }

        internal static void DispatchRuntimeHudLong()
        {
            if (_instance == null || _instance._hud == null ||
                !_instance._hud.DispatchRuntimeClick("long"))
                throw new InvalidOperationException("Runtime Long HUD click could not be dispatched.");
        }

        internal static Vector2 HudButtonCenterForRuntime(string routineId)
        {
            if (_instance == null || _instance._hud == null)
                throw new InvalidOperationException("Runtime HUD is unavailable.");
            return _instance._hud.ButtonCenterForRuntime(routineId);
        }

        internal static Vector2 ScreenCenterForRuntime()
        {
            return new Vector2(Screen.width / 2f, Screen.height / 2f);
        }

        internal static Vector2 ModalBackgroundPointForRuntime()
        {
            return new Vector2(Screen.width / 2f, Screen.height - 4f);
        }

        internal static void BeginPhysicalInputProbe()
        {
            if (_instance == null) throw new InvalidOperationException("UI root is absent.");
            if (_instance._runtimePhysicalProbe != null) return;
            _instance._runtimePhysicalProbe = new UiInputIsolationProbe();
            _instance._hud.BeginRuntimePhysicalObservation();
        }

        internal static UiInputIsolationProbeResult PhysicalInputSnapshotForRuntime()
        {
            if (_instance == null) return null;
            if (_instance._runtimePhysicalProbe == null)
                return _instance._runtimePhysicalInput;
            _instance._runtimePhysicalInput = _instance._runtimePhysicalProbe.Snapshot();
            return _instance._runtimePhysicalInput;
        }

        internal static UiInputIsolationProbeResult EndPhysicalInputProbe()
        {
            if (_instance == null || _instance._runtimePhysicalProbe == null)
                return null;
            _instance._runtimePhysicalInput = _instance._runtimePhysicalProbe.Snapshot();
            _instance._runtimePhysicalProbe.Dispose();
            _instance._runtimePhysicalProbe = null;
            _instance._hud.EndRuntimePhysicalObservation();
            return _instance._runtimePhysicalInput;
        }

        internal static bool IsExecutingForRuntime
        {
            get { return _instance != null && _instance._session != null &&
                _instance._session.IsExecuting; }
        }

        internal static HudTooltipRuntimeDiagnostics TooltipDiagnosticsForRuntime()
        {
            return _instance == null || _instance._hud == null ? null :
                _instance._hud.GetTooltipDiagnostics();
        }

        internal static string PhysicalHoverSnapshotForRuntime(string routineId)
        {
            return _instance == null || _instance._hud == null ? "hud=missing" :
                _instance._hud.PhysicalHoverSnapshotForRuntime(routineId);
        }

        internal static QuickFlowDiagnostics QuickFlowForRuntime(string routineId)
        {
            return _instance == null ? null : _instance._diagnostics.GetFlow(routineId);
        }

        internal static QuickExecutionResult QuickResultForRuntime(string routineId)
        {
            QuickExecutionResult result;
            return _instance != null && _instance._runtimeQuickResults.TryGetValue(
                routineId, out result) ? result : null;
        }

        internal static CatalogLayoutDiagnostics CatalogDiagnosticsForRuntime()
        {
            return _instance == null || _instance._screen == null ||
                _instance._screen.View == null ? null :
                _instance._screen.View.GetCatalogDiagnostics();
        }

        internal static bool SelectFirstRowForRuntime()
        {
            return _instance != null && _instance._screen != null &&
                _instance._screen.View != null &&
                _instance._screen.View.SelectFirstRowForRuntime();
        }

        internal static LiveRowRenderDiagnostics LiveRowRenderDiagnosticsForRuntime()
        {
            return _instance == null || _instance._screen == null ||
                _instance._screen.View == null ? null :
                _instance._screen.View.GetLiveRowRenderDiagnostics();
        }

        internal static bool PrepareVisualEvidenceForRuntime(string view)
        {
            return _instance != null && _instance._screen != null &&
                _instance._screen.View != null &&
                _instance._screen.View.PrepareVisualEvidenceForRuntime(view);
        }

        internal static bool SelectAndConfigureBlessForRuntime(string executionMode)
        {
            if (_instance == null || _instance._screen.View == null ||
                !_instance._screen.View.DispatchBlessRowForRuntime()) return false;
            PlannerSetupModel model = _instance._session.Model;
            string target = model.Snapshot.Units.Where(unit =>
                    unit.TargetValidation.Alive && unit.TargetValidation.Conscious &&
                    unit.TargetValidation.Friendly && unit.TargetValidation.Targetable)
                .Select(unit => unit.UnitId).FirstOrDefault();
            if (string.IsNullOrEmpty(target)) return false;
            if (!model.IsTargetWanted("long", target) &&
                !_instance._screen.View.DispatchTargetForRuntime(target)) return false;
            if (!model.Profile.Execution.RecastExisting) model.ToggleRecastExisting();
            if (model.Profile.Execution.Mode != executionMode) model.ToggleExecutionMode();
            _instance._screen.View.RefreshCatalogForRuntime();
            return model.IsAssigned("long") && model.IsTargetWanted("long", target);
        }

        internal static void DispatchRuntimeInputSmoke()
        {
            if (_instance == null || !_instance._screen.IsOpen)
                throw new InvalidOperationException("Planner screen is not open.");
            using (var probe = new UiInputIsolationProbe())
                _instance._runtimeInputProbe = probe.Dispatch(_instance._screen.View);
        }

        internal static string DispatchCatalogControlsForRuntime()
        {
            return _instance == null || _instance._screen == null ||
                _instance._screen.View == null ? string.Empty :
                _instance._screen.View.DispatchCatalogControlsForRuntime();
        }

        internal static UiRootDiagnostics EndRuntimeSmoke()
        {
            if (_instance == null) throw new InvalidOperationException("UI root is absent.");
            BuffPlannerScreenView view = _instance._screen.View;
            UiInputIsolationProbeResult input = _instance._runtimeInputProbe;
            QuickFlowDiagnostics longFlow = _instance._diagnostics.GetFlow("long");
            PlannerPresentationValidation presentation = view == null ? null : view.LastValidation;
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
                HudRaycastCanvasPath = _instance._hud.RaycastCanvasPath,
                HudButtonOrder = _instance._hud.ButtonOrder,
                HudRowAboveNativeCluster = _instance._hud.RowAboveNativeCluster,
                HudRowLeftAlignedWithNativeCluster = _instance._hud.RowLeftAlignedWithNativeCluster,
                HudGlyphsCentered = _instance._hud.GlyphsCentered,
                HudHitboxesOwnRaycasts = _instance._hud.VisibleHitboxesOwnRaycasts,
                HudUnderlyingNativeActivationCount = _instance._hud.RuntimeUnderlyingNativeActivationCount,
                HotkeyArmed = Main.HotkeyArmed,
                HotkeyKeydownCount = Main.HotkeyKeydownCount,
                HudObjectEvidence = _instance._hud.ObjectEvidence,
                FullScreenRootCount = view == null ? 0 : view.RootCount,
                FullScreenOpaque = view != null && view.IsOpaque,
                FullScreenBlocksRaycasts = view != null && view.BlocksRaycasts,
                GraphicRaycasterPresent = view != null && view.HasGraphicRaycaster,
                PresentationValid = presentation != null && presentation.Valid,
                PresentationFailure = presentation == null ? "missing" : presentation.Failure,
                PresentationCoverage = presentation == null ? 0 : presentation.Coverage,
                PresentationOwnsCenterRaycast = presentation != null && presentation.OwnsCenterRaycast,
                PresentationDiagnostic = presentation == null ? "missing" : presentation.ToString(),
                PresentationValidatedCount = _instance._diagnostics.PresentationValidatedCount,
                PresentationValidatedOrder = _instance._diagnostics.PresentationValidatedOrder,
                InputLeaseAcquiredOrder = _instance._diagnostics.InputLeaseAcquiredOrder,
                LifecycleState = _instance._screen.LifecycleState.ToString(),
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
                LongPointerEnterCount = longFlow.PointerEnters,
                LongListenerCount = longFlow.Listeners,
                LongGroupResolvedCount = longFlow.GroupsResolved,
                LongPlanRevalidatedCount = longFlow.PlansRevalidated,
                LongExecutionInvokedCount = longFlow.ExecutionsInvoked,
                LongRefusalCount = longFlow.Refusals,
                LongResultPresentedCount = longFlow.ResultsPresented,
                LongResultMessage = _instance._runtimeFirstLongResult == null
                    ? string.Empty : _instance._runtimeFirstLongResult.Message,
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
            CatalogLayoutDiagnostics catalog = view == null ? null : view.GetCatalogDiagnostics();
            HudTooltipRuntimeDiagnostics tooltip = _instance._hud.GetTooltipDiagnostics();
            UiInputIsolationProbeResult physical = PhysicalInputSnapshotForRuntime();
            result.CatalogEvidence = catalog == null ? "missing" : catalog.ToString();
            result.CatalogVisibleViewModels = catalog == null || catalog.Filters == null ? 0 :
                catalog.Filters.VisibleViewModels;
            result.CatalogInstantiatedRows = catalog == null ? 0 : catalog.InstantiatedRows;
            result.CatalogActiveRows = catalog == null ? 0 : catalog.ActiveRows;
            result.CatalogVisibleRows = catalog == null ? 0 : catalog.VisibleRows;
            result.CatalogSelectedDetailsBound = catalog != null && catalog.SelectedDetailsBound;
            result.CatalogBlessEvidence = catalog == null ? "missing" : catalog.BlessEvidence;
            result.CatalogProviderCount = catalog == null ? 0 : catalog.ProviderCount;
            result.CatalogAggregateAbilityCount = catalog == null ? 0 : catalog.AggregateAbilityCount;
            result.CatalogConsolidatedCardCount = catalog == null ? 0 : catalog.ConsolidatedCardCount;
            result.DirectSelectedTargetCount = catalog == null ? 0 : catalog.DirectSelectedTargetCount;
            result.IndirectCoveredTargetCount = catalog == null ? 0 : catalog.IndirectCoveredTargetCount;
            result.TooltipActive = tooltip != null && tooltip.Active;
            result.TooltipInsideScreen = tooltip != null && tooltip.InsideScreen;
            result.TooltipBounds = tooltip == null ? "missing" : tooltip.Bounds;
            result.TooltipListenerCount = tooltip == null ? 0 : tooltip.ListenerCount;
            result.TooltipRaycastGraphicCount = tooltip == null ? -1 : tooltip.RaycastGraphicCount;
            result.TooltipBlocksRaycasts = tooltip != null && tooltip.BlocksRaycasts;
            result.TooltipNativeTriggerCount = tooltip == null ? 0 : tooltip.NativeTriggerCount;
            result.TooltipUsesNativeParchmentPresentation = tooltip != null &&
                tooltip.UsesNativeParchmentPresentation;
            result.SetupOpenSoundCount = _instance._diagnostics.SetupOpenSoundCount;
            result.PhysicalInputPlayerCommandCount = physical == null ? -1 : physical.PlayerCommandCount;
            result.PhysicalInputMovementCommandCount = physical == null ? -1 : physical.MovementCommandCount;
            result.PhysicalInputAbilityCommandCount = physical == null ? -1 : physical.AbilityCommandCount;
            result.PhysicalInputSelectionEventCount = physical == null ? -1 : physical.SelectionEventCount;
            result.PhysicalInputAbilityTargetEventCount = physical == null ? -1 : physical.AbilityTargetEventCount;
            result.PhysicalInputSelectionUnchanged = physical != null && physical.SelectionUnchanged;
            result.PhysicalInputCameraUnchanged = physical != null && physical.CameraUnchanged;
            QuickExecutionResult importantResult = QuickResultForRuntime("important");
            QuickExecutionResult shortResult = QuickResultForRuntime("short");
            QuickExecutionResult longResult = QuickResultForRuntime("long");
            result.ImportantResultMessage = importantResult == null ? string.Empty :
                importantResult.Message;
            result.ShortResultMessage = shortResult == null ? string.Empty : shortResult.Message;
            result.ConfiguredLongResultMessage = longResult == null ? string.Empty : longResult.Message;
            result.ConfiguredLongDisposition = longResult == null ? string.Empty :
                longResult.Disposition.ToString();
            result.ConfiguredLongPlanned = longResult == null ? 0 : longResult.Planned;
            result.ConfiguredLongSubmitted = longResult == null ? 0 : longResult.Submitted;
            result.ConfiguredLongConfirmed = longResult == null ? 0 : longResult.Confirmed;
            _instance._screen.Close();
            result.InputLeaseReleaseCountAfterClose = _instance._diagnostics.InputLeaseReleaseCount;
            result.ScreenDestroyCountAfterClose = _instance._diagnostics.ScreenDestroyCount;
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
            if (!_enabled || _session == null || _session.IsExecuting || _quickStartPending)
                return false;
            _quickStartPending = true;
            StartCoroutine(ExecuteQuickRoutine(routineId, completed, false));
            return true;
        }

        public bool TryStartReadyOnly(string routineId, Action<QuickExecutionResult> completed)
        {
            if (!_enabled || _session == null || _session.IsExecuting || _quickStartPending)
                return false;
            _quickStartPending = true;
            StartCoroutine(ExecuteQuickRoutine(routineId, completed, true));
            return true;
        }

        private IEnumerator ExecuteQuickRoutine(
            string routineId,
            Action<QuickExecutionResult> completed,
            bool readyOnlyExplicit)
        {
            bool completedCalled = false;
            Action<QuickExecutionResult> observedCompletion = result =>
            {
                completedCalled = true;
                if (completed != null) completed(result);
            };
            try
            {
                IEnumerator routine = _session.ExecuteRoutine(routineId,
                    observedCompletion, readyOnlyExplicit);
                while (true)
                {
                    bool moved = false;
                    object current = null;
                    Exception failure = null;
                    try
                    {
                        moved = routine.MoveNext();
                        if (moved) current = routine.Current;
                    }
                    catch (Exception exception)
                    {
                        failure = exception;
                    }
                    if (failure != null)
                    {
                        if (!completedCalled)
                            observedCompletion(_session.AbortUnexpectedExecution(
                                routineId, failure));
                        yield break;
                    }
                    if (!moved) yield break;
                    yield return current;
                }
            }
            finally
            {
                _quickStartPending = false;
            }
        }

        private void Initialize(string modPath, ModLog log)
        {
            _modPath = modPath;
            _log = log;
            _session = new PlannerUiSession(modPath, log);
            _diagnostics = new BuffPlannerUiLifecycleDiagnostics();
            _quick = new BuffPlannerQuickExecuteController(this, _diagnostics, PresentQuickResult);
            _screen = new BuffPlannerScreenController(_session, _diagnostics, log,
                routineId => ExecuteLegacyRoutine(routineId), PlayNativeSetupOpenSound,
                routineId => ExecuteLegacyRoutine(routineId, true));
            _hud = new BuffPlannerHudButtonController(_session, _diagnostics, log,
                () => { OpenSetup(); }, routineId => ExecuteLegacyRoutine(routineId));
            _spellbookEntry = new BuffPlannerSpellbookEntryController(
                value => _log.Info(value),
                () => OpenSetup(),
                () => _screen != null && _screen.IsOpen,
                PlannerUiTheme.Resolve(null),
                () => _screen != null && _screen.LifecycleState ==
                    PlannerScreenLifecycleState.Open,
                RequestNativeEscapeVeil);
            try
            {
                _eventSubscription = EventBus.Subscribe((object)this);
                _log.Info("[KBP-BOOT] EventBus subscribed;scene=true;areaStages=true;" +
                    "areaActivation=true;controller=" + gameObject.GetInstanceID() + ".");
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-BOOT] EventBus subscription failed;" +
                    "HUD host-transition observation remains active.",
                    exception);
            }
        }

        // Single guarded legacy-execution entry: while the casting-first
        // workspace is selected, every legacy quick-run route (screen,
        // HUD, hotkey, spellbook) refuses here instead of bypassing the
        // workspace's explicitly disabled dispatch boundary.
        private void ExecuteLegacyRoutine(string routineId, bool readyOnly = false)
        {
            if (!CastingWorkspaceDevSelection.LegacyExecutionPermitted)
            {
                _log.Info("[KBP-WORKSPACE] refused legacy quick execution;routine=" +
                    routineId + ";reason=" +
                    CastingWorkspaceDevSelection.LegacyExecutionRefusal);
                return;
            }
            _quick.Execute(routineId, readyOnly);
        }

        private bool OpenSetup()
        {
            if (CastingWorkspaceDevSelection.Enabled)
                return OpenCastingWorkspace();
            return _screen != null && _screen.Open();
        }

        // Session-scoped development selection: the casting-first workspace
        // renders instead of the legacy screen for this whole session. It
        // consumes the same discovery data through the legacy session's
        // model (one snapshot, one option set, one effect map) and owns its
        // candidate records through its own session; the legacy authoring
        // path is never open at the same time. Native submission stays
        // explicitly disabled at its dispatch boundary.
        private bool OpenCastingWorkspace()
        {
            if (_castingWorkspace != null) return false;
            BuffPlannerInputLease lease = null;
            try
            {
                if (StaticCanvas.Instance == null)
                {
                    LogUiUnavailable("casting-workspace: campaign UI unavailable");
                    return false;
                }
                // Acquire the established game-mode/selection input lease
                // exactly once per open, before construction; a failed
                // acquire self-restores and fails the open (review R2).
                lease = BuffPlannerInputLease.Acquire(new KingmakerPlannerInputBoundary());
                _session.Refresh();
                string campaignId = _session.Model == null ||
                    _session.Model.Profile == null
                        ? null : _session.Model.Profile.CampaignId;
                var workspaceSession = CastingWorkspaceSessionBinding.Resolve(
                    _castingWorkspaceSession, campaignId,
                    delegate(string id)
                    {
                        return new CastingWorkspaceSession(_modPath, id);
                    },
                    delegate(string message) { _log.Info(message); });
                if (workspaceSession == null)
                {
                    // Unresolved/transitional campaign identity must not
                    // bind arbitrary work to an unknown-campaign fallback
                    // (review G3).
                    LogUiUnavailable(
                        "casting-workspace: campaign identity unresolved");
                    return false;
                }
                _castingWorkspaceSession = workspaceSession;
                _castingWorkspace = new CastingWorkspaceScreenView(
                    StaticCanvas.Instance, workspaceSession,
                    BuildCastingWorkspaceInputs, CloseCastingWorkspace);
                _workspaceInputLease = lease;
                lease = null;
                _castingWorkspace.RefreshView();
                _log.Info("[KBP-WORKSPACE] casting-first workspace opened;" +
                    "campaign=" + campaignId +
                    ";dispatch=" + workspaceSession.DispatchDisposition);
                return true;
            }
            catch (Exception exception)
            {
                if (lease != null) lease.Dispose();
                CloseCastingWorkspace();
                _log.Error("[KBP-WORKSPACE] open failed.", exception);
                LogUiUnavailable("casting-workspace:" + exception.Message);
                return false;
            }
        }

        private void CloseCastingWorkspace()
        {
            if (_workspaceInputLease != null)
            {
                _workspaceInputLease.Dispose();
                _workspaceInputLease = null;
            }
            if (_castingWorkspace == null) return;
            // Deliberate unsaved-state policy: closing discards in-memory
            // authoring edits; the next open reloads the last explicitly
            // saved document. The session is never silently reconstructed
            // while open.
            _castingWorkspace.Dispose();
            _castingWorkspace = null;
            // Unsaved intent is PRESERVED across an ordinary close/reopen
            // (review F6): the session (document + undo history + dirty
            // state) survives; only the view is disposed. Full teardown
            // (ReleaseAll) is the defined discard point and logs it.
        }

        private DateTime _lastWorkspaceInputsRefreshUtc = DateTime.MinValue;
        private static readonly TimeSpan WorkspaceInputsRefreshMinimum =
            TimeSpan.FromSeconds(2);

        private CastingWorkspaceInputs BuildCastingWorkspaceInputs()
        {
            if (_session.Model == null)
                throw new InvalidOperationException(
                    "Discovery has not produced a party model yet.");
            // Bounded freshness at the production input boundary (review
            // C2): Apply/present/rebuild preflights re-run discovery at most
            // once per two seconds — never per frame — and a failed refresh
            // falls back to the existing model rather than failing the call.
            if (DateTime.UtcNow - _lastWorkspaceInputsRefreshUtc >=
                WorkspaceInputsRefreshMinimum)
            {
                _lastWorkspaceInputsRefreshUtc = DateTime.UtcNow;
                try { _session.Refresh(); }
                catch (Exception exception)
                {
                    _log.Error("[KBP-WORKSPACE] bounded input refresh failed;" +
                        " using prior discovery state.", exception);
                }
            }
            return new CastingWorkspaceInputs(
                _session.Model.Snapshot,
                _session.ProviderOptions,
                _session.Model.EffectsBySource,
                _session.Model.Enhancements);
        }

        private void RequestNativeEscapeVeil()
        {
            // Recovery after a failed handoff whose native spellbook already
            // closed: land the player in a usable interface. No verified
            // offline contract exists for re-opening the native spellbook,
            // so recovery opens the planner itself through our own owned
            // machinery (mode is free at this point) and logs the exact
            // missing native contract rather than guessing an API.
            _log.Info("[KBP-SPELLBOOK] recovery: opening planner directly; " +
                "return-to-spellbook awaits a verified native reopen contract.");
            try
            {
                OpenSetup();
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-SPELLBOOK] planner recovery open failed.", exception);
            }
        }

        private bool PlayNativeSetupOpenSound()
        {
            if (Game.Instance == null || Game.Instance.UI == null ||
                Game.Instance.UI.Common == null || Game.Instance.UI.Common.UISound == null)
                return false;
            Game.Instance.UI.Common.UISound.Play(UISoundType.CharacterScreenOpen);
            return true;
        }

        private void Tick(float deltaTime)
        {
            if (!_enabled) return;
            long rootStartedAt = RuntimePerformanceDiagnostics.BeginOperation();
            _tickCount++;
            try
            {
                if (_spellbookEntry != null) _spellbookEntry.Tick();
                if (_castingWorkspace != null && Input.GetKeyDown(KeyCode.Escape))
                {
                    // Escape closes the owned workspace before the legacy
                    // screen is consulted; the two are never open together.
                    CloseCastingWorkspace();
                    _log.Info("[KBP-BOOT] casting workspace close requested;source=Escape.");
                }
                if (_screen.LifecycleState != PlannerScreenLifecycleState.Closed &&
                    Input.GetKeyDown(KeyCode.Escape)) _screen.Close();
                long screenStartedAt = RuntimePerformanceDiagnostics.BeginOperation();
                try { _screen.Tick(); }
                finally
                {
                    RuntimePerformanceDiagnostics.RecordOperation(
                        RuntimePerformanceOperation.ScreenTick, screenStartedAt);
                }
                StaticCanvas canvas = StaticCanvas.Instance;
                UISectionHUDController hudHost = canvas == null ? null : canvas.HUDController;
                int hudHostIdentity = hudHost == null ? 0 : hudHost.GetInstanceID();
                bool hudHostActive = hudHost != null && hudHost.gameObject.activeInHierarchy;
                LogHudHostTransition(hudHostIdentity, hudHostActive);
                HudInstallDispatchDecision installDecision = _hudInstallGate.ObserveHost(
                    hudHostIdentity, hudHostActive);
                if (installDecision == HudInstallDispatchDecision.Dispatch)
                    _log.Info("[KBP-BOOT] HUD installation dispatch requested;reason=" +
                        _hudInstallGate.LastTransition + ";dispatch=" +
                        _hudInstallGate.AttemptCount + ";retryDispatch=" +
                        _hudInstallGate.RetryDispatchCount + ";hud=" +
                        hudHostIdentity + ";active=" + hudHostActive + ".");
                if (installDecision == HudInstallDispatchDecision.Dispatch &&
                    !RuntimePerformanceDiagnostics.SuppressHudDiscovery)
                {
                    DispatchHudInstall(hudHost);
                }
                long hudTickStartedAt = RuntimePerformanceDiagnostics.BeginOperation();
                try
                {
                    HudCandidateTickResult candidateResult = _hud.Tick(hudHost);
                    ObserveHudCandidateResult(candidateResult);
                }
                catch (Exception exception)
                {
                    _hudTickExceptionCount++;
                    _log.Error("[KBP-BOOT] HUD candidate tick failed;owned UI will be " +
                        "disposed and retry re-armed.", exception);
                    try
                    {
                        _hud.RecoverFromFault("candidate-tick-exception");
                    }
                    catch (Exception cleanupException)
                    {
                        _log.Error("[KBP-BOOT] HUD owned-UI fault cleanup also failed;" +
                            "retry remains re-armed.", cleanupException);
                    }
                    ObserveHudCandidateResult(HudCandidateTickResult.Stale);
                }
                finally
                {
                    RuntimePerformanceDiagnostics.RecordOperation(
                        RuntimePerformanceOperation.HudTick, hudTickStartedAt);
                }
                CompleteRuntimeReconstructionWhenReady();
                if (_screen.IsOpen) _runtimeObservedFrames++;
            }
            catch (Exception exception)
            {
                _log.Error("Buff Planner UI update failed.", exception);
                _screen.Close();
            }
            finally
            {
                RuntimePerformanceDiagnostics.RecordOperation(
                    RuntimePerformanceOperation.UiRootTick, rootStartedAt);
            }
        }

        private void DispatchHudInstall(UISectionHUDController hudHost)
        {
            long installStartedAt = RuntimePerformanceDiagnostics.BeginOperation();
            HudInstallAttemptResult result;
            try
            {
                result = _hud.TryInstall(hudHost);
            }
            catch (Exception exception)
            {
                _hudInstallExceptionCount++;
                _log.Error("[KBP-BOOT] scoped HUD installation attempt failed;owned UI " +
                    "will be disposed and bounded retry re-armed.", exception);
                try
                {
                    _hud.RecoverFromFault("scoped-install-exception");
                }
                catch (Exception cleanupException)
                {
                    _log.Error("[KBP-BOOT] HUD owned-UI install cleanup also failed;" +
                        "retry remains re-armed.", cleanupException);
                }
                result = HudInstallAttemptResult.RetryableNotReady;
            }
            finally
            {
                RuntimePerformanceDiagnostics.RecordOperation(
                    RuntimePerformanceOperation.HudInstall, installStartedAt);
            }
            _hudInstallGate.RecordAttemptResult(result);
            _log.Info("[KBP-BOOT] scoped HUD attempt result;result=" + result +
                ";dispatch=" + _hudInstallGate.AttemptCount +
                ";retryDispatch=" + _hudInstallGate.RetryDispatchCount +
                ";hud=" + (hudHost == null ? 0 : hudHost.GetInstanceID()) +
                ";anchor=" + _hud.AnchorInstanceId + ";candidate=" + _hud.RootInstanceId +
                ";state=" + _hudInstallGate.State + ";failure=" + _hud.LastFailure + ".");
            if (result == HudInstallAttemptResult.RetryableNotReady ||
                result == HudInstallAttemptResult.StaleCandidateDisposed)
                LogHudRetryRearmed("attempt-" + result);
        }

        private void ObserveHudCandidateResult(HudCandidateTickResult result)
        {
            if (result == HudCandidateTickResult.None) return;
            HudCandidateTickResult previous = _hudInstallGate.LastCandidateResult;
            _hudInstallGate.RecordCandidateResult(result);
            if (result != HudCandidateTickResult.Pending || previous != result)
                _log.Info("[KBP-BOOT] HUD candidate transition;result=" + result +
                    ";candidate=" + _hud.RootInstanceId + ";anchor=" +
                    _hud.AnchorInstanceId + ";state=" + _hudInstallGate.State +
                    ";validationFrames=" + _hud.CandidateValidationFailures +
                    ";lastValidationFailure=" + _hud.LastValidationFailure + ".");
            if (result == HudCandidateTickResult.Expired ||
                result == HudCandidateTickResult.Stale)
                LogHudRetryRearmed("candidate-" + result);
        }

        private void LogHudHostTransition(int hudHostIdentity, bool hudHostActive)
        {
            if (hudHostIdentity == _lastLoggedHudIdentity &&
                hudHostActive == _lastLoggedHudActive) return;
            int previousIdentity = _lastLoggedHudIdentity == int.MinValue
                ? 0 : _lastLoggedHudIdentity;
            _lastLoggedHudIdentity = hudHostIdentity;
            _lastLoggedHudActive = hudHostActive;
            _log.Info("[KBP-BOOT] " + (hudHostActive
                    ? "active HUD detected" : "active HUD unavailable") +
                ";previous=" + previousIdentity +
                ";current=" + hudHostIdentity + ";active=" + hudHostActive +
                ";suspended=" + _hudInstallGate.IsSuspended + ".");
        }

        private void LogHudRetryRearmed(string reason)
        {
            _log.Info("[KBP-BOOT] HUD retry re-armed;reason=" + reason +
                ";retryArm=" + _hudInstallGate.RetryArmCount +
                ";retryAfterFrames=" + _hudInstallGate.RetryFramesRemaining +
                ";hud=" + _hudInstallGate.HostIdentity +
                ";active=" + _hudInstallGate.HostActive +
                ";state=" + _hudInstallGate.State + ".");
        }

        private void CompleteRuntimeReconstructionWhenReady()
        {
            if (!_runtimeReconstructionPending || !_hud.IsInstalled) return;
            if (!_hud.DispatchRuntimeClick("long")) return;
            _runtimeReconstructionPending = false;
            BeginRuntimeSmoke();
        }

        public void OnAreaBeginUnloading()
        {
            SignalLifecycle("OnAreaBeginUnloading", true);
            ReleasePlayerUi();
        }

        public void OnAreaDidLoad()
        {
            SignalLifecycle("OnAreaDidLoad", false);
        }

        public void OnAreaScenesLoaded()
        {
            SignalLifecycle("OnAreaScenesLoaded", false);
        }

        public void OnAreaLoadingComplete()
        {
            SignalLifecycle("OnAreaLoadingComplete", false);
        }

        public void OnAreaActivated()
        {
            SignalLifecycle("OnAreaActivated", false);
        }

        private void SignalLifecycle(string name, bool unloading)
        {
            _lifecycleSignalCount++;
            if (unloading || !_enabled) SuspendHudInstall(name);
            else RequestHudInstall(name, true);
            _log.Info("[KBP-BOOT] lifecycle callback;name=" + name +
                ";count=" + _lifecycleSignalCount + ";installRequested=" +
                _hudInstallGate.IsRequested + ";suspended=" +
                _hudInstallGate.IsSuspended + ";state=" +
                _hudInstallGate.State + ";mode=" +
                (Game.Instance == null ? "game-null" : Game.Instance.CurrentMode.ToString()) + ".");
        }

        private void RequestHudInstall(string reason, bool resume)
        {
            bool requested = resume
                ? _hudInstallGate.ResumeAndRequest(reason)
                : _hudInstallGate.Request(reason);
            if (!requested) return;
            _log.Info("[KBP-BOOT] HUD installation dispatch requested;reason=" + reason +
                ";request=" + _hudInstallGate.RequestCount +
                ";hud=" + _hudInstallGate.HostIdentity +
                ";active=" + _hudInstallGate.HostActive +
                ";state=" + _hudInstallGate.State + ".");
        }

        private void SuspendHudInstall(string reason)
        {
            if (!_hudInstallGate.Suspend(reason)) return;
            _runtimeReconstructionPending = false;
            _log.Info("[KBP-BOOT] HUD installation suspended;reason=" + reason +
                ";hud=" + _hudInstallGate.HostIdentity +
                ";active=" + _hudInstallGate.HostActive +
                ";state=" + _hudInstallGate.State + ".");
        }

        private void LogUiUnavailable(string reason)
        {
            string exact = string.IsNullOrEmpty(reason) ? "unknown-readiness-failure" : reason;
            _log.Info("Buff Planner UI is unavailable: " + exact);
            _log.Info("[KBP-BOOT] full-screen install failed;reason=" + exact +
                ";retryable=true;plannerHotkeyArmed=true.");
        }

        private void PresentQuickResult(QuickExecutionResult result)
        {
            _lastQuickResult = result;
            if (result.RoutineId == "long" && _runtimeFirstLongResult == null)
                _runtimeFirstLongResult = result;
            _runtimeQuickResults[result.RoutineId] = result;
            _screen.Present(result);
            _log.Info("Routine UI result: " + result.RoutineId + " " +
                result.Disposition + " " + result.Message);
        }

        private void OnDisable()
        {
            SuspendHudInstall("ui-root-disabled");
            ReleasePlayerUi();
        }

        private void OnDestroy()
        {
            ReleaseAll();
            if (_instance == this) _instance = null;
        }

        private void ReleasePlayerUi()
        {
            CloseCastingWorkspace();
            if (_screen != null) _screen.Close();
            if (_hud != null) _hud.Dispose();
        }

        private void ReleaseAll()
        {
            if (_disposed) return;
            _disposed = true;
            StopAllCoroutines();
            CloseCastingWorkspace();
            if (_castingWorkspaceSession != null)
            {
                // Defined shutdown policy: discarding the session drops any
                // unsaved edits; this is the only silent-loss point and it
                // is logged (review F6).
                _log.Info("[KBP-WORKSPACE] session discarded at teardown;dirty=" +
                    _castingWorkspaceSession.IsDirty + ".");
                _castingWorkspaceSession = null;
            }
            if (_runtimePhysicalProbe != null) _runtimePhysicalProbe.Dispose();
            _runtimePhysicalProbe = null;
            if (_spellbookEntry != null) _spellbookEntry.Release();
            _spellbookEntry = null;
            if (_eventSubscription != null)
            {
                _eventSubscription.Dispose();
                _eventSubscription = null;
                _log.Info("[KBP-BOOT] EventBus unsubscribed;controller=" +
                    gameObject.GetInstanceID() + ".");
            }
            else EventBus.Unsubscribe((object)this);
            if (_screen != null) _screen.Dispose();
            if (_hud != null) _hud.Dispose();
            _screen = null;
            _hud = null;
            _quick = null;
            _log.Info("[KBP-BOOT] controller disposed;instance=" +
                gameObject.GetInstanceID() + ".");
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
        internal string HudRaycastCanvasPath;
        internal string HudButtonOrder;
        internal bool HudRowAboveNativeCluster;
        internal bool HudRowLeftAlignedWithNativeCluster;
        internal bool HudGlyphsCentered;
        internal bool HudHitboxesOwnRaycasts;
        internal int HudUnderlyingNativeActivationCount;
        internal bool HotkeyArmed;
        internal int HotkeyKeydownCount;
        internal string HudObjectEvidence;
        internal int FullScreenRootCount;
        internal bool FullScreenOpaque;
        internal bool FullScreenBlocksRaycasts;
        internal bool GraphicRaycasterPresent;
        internal bool PresentationValid;
        internal string PresentationFailure;
        internal float PresentationCoverage;
        internal bool PresentationOwnsCenterRaycast;
        internal string PresentationDiagnostic;
        internal int PresentationValidatedCount;
        internal int PresentationValidatedOrder;
        internal int InputLeaseAcquiredOrder;
        internal string LifecycleState;
        internal bool PlannerOpen;
        internal bool FullScreenModeActive;
        internal bool SelectionDisabled;
        internal bool EventSystemPresent;
        internal int InputLeaseAcquireCount;
        internal int InputLeaseReleaseCount;
        internal int InputLeaseReleaseCountAfterClose;
        internal int ScreenCreateCount;
        internal int ScreenDestroyCount;
        internal int ScreenDestroyCountAfterClose;
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
        internal int LongPointerEnterCount;
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
        internal string CatalogEvidence;
        internal int CatalogVisibleViewModels;
        internal int CatalogInstantiatedRows;
        internal int CatalogActiveRows;
        internal int CatalogVisibleRows;
        internal bool CatalogSelectedDetailsBound;
        internal string CatalogBlessEvidence;
        internal int CatalogProviderCount;
        internal int CatalogAggregateAbilityCount;
        internal int CatalogConsolidatedCardCount;
        internal int DirectSelectedTargetCount;
        internal int IndirectCoveredTargetCount;
        internal bool TooltipActive;
        internal bool TooltipInsideScreen;
        internal string TooltipBounds;
        internal int TooltipListenerCount;
        internal int TooltipRaycastGraphicCount;
        internal bool TooltipBlocksRaycasts;
        internal int TooltipNativeTriggerCount;
        internal bool TooltipUsesNativeParchmentPresentation;
        internal int SetupOpenSoundCount;
        internal int PhysicalInputPlayerCommandCount;
        internal int PhysicalInputMovementCommandCount;
        internal int PhysicalInputAbilityCommandCount;
        internal int PhysicalInputSelectionEventCount;
        internal int PhysicalInputAbilityTargetEventCount;
        internal bool PhysicalInputSelectionUnchanged;
        internal bool PhysicalInputCameraUnchanged;
        internal string ImportantResultMessage;
        internal string ShortResultMessage;
        internal string ConfiguredLongResultMessage;
        internal string ConfiguredLongDisposition;
        internal int ConfiguredLongPlanned;
        internal int ConfiguredLongSubmitted;
        internal int ConfiguredLongConfirmed;
    }
}
