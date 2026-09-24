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
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
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
        // Live EventBus subscriptions held by planner roots (reload evidence:
        // exactly one across area and save loads).
        private static int _activeEventSubscriptions;
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
        private readonly Dictionary<string, int> _lifecycleSignalsByName =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private int _lastLoggedHudIdentity = int.MinValue;
        private bool _lastLoggedHudActive;
        private int _hudInstallExceptionCount;
        private int _hudTickExceptionCount;
        // Casting-first production execution: one host owns at most one run;
        // Tick pumps it; disable/unload/teardown shut it down.
        private CastingExecutionHost _castingHost;
        private PlannerModeStore _plannerModeStore;
        private PlannerMode _plannerMode = PlannerMode.Classic;
        private string _plannerModeWarning = string.Empty;
        private Action<QuickExecutionResult> _pendingCastingCompletion;
        // The last casting-first outcome per routine (refusal or run
        // result), shown on demand in that routine HUD tooltip.
        private readonly Dictionary<string, string> _lastCastingPress =
            new Dictionary<string, string>(StringComparer.Ordinal);
        // World-running time (milliseconds) for the casting host deadline: it
        // advances only while casting can execute.
        private readonly CastingWorldClock _castingWorldClock = new CastingWorldClock();
        private bool _castingRunHeld;

        // Whether a cast can execute in the world now: the Default game mode
        // and not paused. A rule submitted while the world is held (paused,
        // a dialog, or a full-screen window such as the planner itself) is
        // queued but does not execute, so its confirmation window would
        // expire with the effect still absent (probe
        // casting-probe-cast-20260923-p1-01 cast inside the open planner).
        internal static bool WorldRunsForCasting
        {
            get
            {
                return Game.Instance != null && !Game.Instance.IsPaused &&
                    Game.Instance.CurrentMode == GameModeType.Default;
            }
        }

        internal static string WorldStateForRuntime
        {
            get
            {
                return Game.Instance == null ? "game=absent"
                    : "mode=" + Game.Instance.CurrentMode + ";paused=" + Game.Instance.IsPaused;
            }
        }

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
            if (enabled)
            {
                if (_instance._castingHost != null) _instance._castingHost.Resume();
                _instance.RequestHudInstall("mod-enabled", true);
            }
            else
            {
                // A running routine ends through its owned terminal (the
                // executor restores any temporary native state) before the
                // player UI is released: the casting-first host's run and a
                // Classic run alike.
                if (_instance._castingHost != null)
                    _instance._castingHost.Shutdown("mod-disabled");
                _instance.EndClassicRun("mod-disabled");
                _instance.SuspendHudInstall("mod-disabled");
                _instance.ReleasePlayerUi();
            }
        }

        // ------------------------------------------------------------------
        // Planner mode (the deliberate, player-facing activation path)
        // ------------------------------------------------------------------

        internal static bool IsCastingFirstSelected
        {
            get { return _instance != null && _instance._plannerMode == PlannerMode.CastingFirst; }
        }

        // Qualification seams: fresh discovery exactly as Apply uses it, and
        // the production executor for given execution settings.
        internal static CastingWorkspaceInputs CastingWorkspaceFreshInputsForRuntime()
        {
            return _instance == null ? null : _instance.BuildFreshCastingWorkspaceInputs();
        }

        internal static ICastExecutor CreateCastingExecutorForRuntime(ExecutionProfile settings)
        {
            if (_instance == null) throw new InvalidOperationException("UI root is absent.");
            return _instance.CreateCastingExecutor(settings ?? ExecutionProfile.Default());
        }

        // The planner's own execution host, which this root pumps once per
        // frame while the world runs (runtime qualification submits its
        // approved runs to it through the qualification boundary).
        internal static CastingExecutionHost CastingHostForRuntime
        {
            get
            {
                if (_instance == null || _instance._castingHost == null)
                    throw new InvalidOperationException("UI root casting host is absent.");
                return _instance._castingHost;
            }
        }

        // A routine press exactly as the HUD button delivers it (the single
        // routine entry); while a run is active this is the player's stop.
        internal static bool PressRoutineForRuntime(string routineId)
        {
            return _instance != null && _instance.ExecuteRoutineRequest(routineId);
        }

        // Runtime evidence: how many production casting runs started in
        // this session, and the current dispatch disposition.
        internal static int CastingRunsStartedForRuntime
        {
            get { return _instance == null || _instance._castingHost == null
                ? 0 : _instance._castingHost.StartedRuns; }
        }

        // Reload evidence (mission section 8): live EventBus subscriptions,
        // lifecycle signals by name, whether the HUD is installed, and how
        // many HUD roots exist in the loaded scenes.
        internal static int ActiveEventSubscriptionsForRuntime
        {
            get { return _activeEventSubscriptions; }
        }

        internal static int LifecycleSignalsForRuntime(string name)
        {
            int seen;
            return _instance != null && _instance._lifecycleSignalsByName.TryGetValue(name, out seen) ? seen : 0;
        }

        internal static bool IsHudInstalledForRuntime
        {
            get { return _instance != null && _instance._hud != null && _instance._hud.IsInstalled; }
        }

        // Every HUD root in the loaded scenes, inactive ones included (review
        // of 1332ed8..542cd66, P2-1): FindObjectsOfType sees active objects
        // only; FindObjectsOfTypeAll also returns assets, so scene objects
        // are kept.
        internal static int HudRootCountForRuntime
        {
            get
            {
                return Resources.FindObjectsOfTypeAll<RectTransform>().Count(rect =>
                    rect != null && rect.gameObject.scene.IsValid() &&
                    string.Equals(rect.name, BuffPlannerHudButtonController.RootName, StringComparison.Ordinal));
            }
        }

        internal static string CastingDispatchDispositionForRuntime
        {
            get { return _instance == null || _instance._castingWorkspaceSession == null
                ? "no-session" : _instance._castingWorkspaceSession.DispatchDisposition; }
        }

        internal static bool IsCastingRunActive
        {
            get { return _instance != null && _instance._castingHost != null &&
                _instance._castingHost.IsRunning; }
        }

        // Returns null on success, otherwise why the mode was not changed.
        internal static string TrySetPlannerMode(PlannerMode mode)
        {
            if (_instance == null) return "The planner is not loaded yet.";
            return _instance.SetPlannerMode(mode);
        }

        private string SetPlannerMode(PlannerMode mode)
        {
            if (mode == _plannerMode) return null;
            if (NativeCastingSessionPolicy.Locked)
                return "The planner mode cannot be changed during an automated test session.";
            if ((_castingHost != null && _castingHost.IsRunning) ||
                (_session != null && _session.IsExecuting) || _quickStartPending)
                return "A buff routine is running; wait for it to finish or stop it first.";
            // Neither planner may stay open across the switch.
            CloseCastingWorkspace();
            if (_screen != null) _screen.Close();
            try { _plannerModeStore.Save(mode); }
            catch (Exception exception)
            {
                _log.Error("[KBP-MODE] planner mode could not be saved.", exception);
                return "The planner mode could not be saved: " + exception.Message;
            }
            _plannerMode = mode;
            _log.Info("[KBP-MODE] planner mode set;mode=" + mode + ";store=" +
                _plannerModeStore.FilePath + ".");
            return null;
        }

        // Casting-first routes are active for the chosen mode, or for a
        // runtime-test session that selected the workspace.
        private bool CastingFirstActive
        {
            get { return CastingWorkspaceDevSelection.Enabled ||
                _plannerMode == PlannerMode.CastingFirst; }
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
            _ownedTicks++;
            if (_instance != null) _instance.Tick(deltaTime);
        }

        // How often the mod's update has ticked the planner root (the
        // qualification's held disable must see no tick).
        private static long _ownedTicks;
        internal static long OwnedTicksForRuntime { get { return _ownedTicks; } }

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

        // The open casting-first view, for the physical-input scenario's
        // read-only evidence (screen points, search, tiles, scroll); null
        // when closed.
        internal static CastingWorkspaceScreenView CastingWorkspaceViewForRuntime
        {
            get { return _instance == null ? null : _instance._castingWorkspace; }
        }

        // Read-only runtime postcondition for the manual terminal step
        // (review J2): the production close must release the input lease.
        internal static bool IsCastingWorkspaceInputLeaseHeldForRuntime
        {
            get { return _instance != null && _instance._workspaceInputLease != null; }
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
            // A same-frame rebuild leaves the destroyed-pending OLD buttons
            // (SetActive(false) + deferred Destroy) attached until end of
            // frame; among same-named matches the LIVE one is the real
            // control. Callback coverage is still not reachability: if no
            // match is active (or interactable) the control is reported as
            // such (review G4).
            UnityEngine.UI.Button live = null;
            UnityEngine.UI.Button anyMatch = null;
            foreach (UnityEngine.UI.Button button in buttons)
            {
                if (button == null || !string.Equals(button.name, buttonName,
                        StringComparison.Ordinal)) continue;
                if (anyMatch == null) anyMatch = button;
                if (button.gameObject.activeInHierarchy)
                {
                    live = button;
                    break;
                }
            }
            if (live == null)
                return anyMatch == null
                    ? "control-missing:" + buttonName
                    : "control-inactive:" + buttonName;
            if (!live.interactable)
                return "control-not-interactable:" + buttonName;
            live.onClick.Invoke();
            return "invoked";
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
            string pageArt = view.PageArtEvidence;
            GameObject root = view.RootObject;
            if (root == null) return "workspace=root-null";
            RectTransform rect = (RectTransform)root.transform;
            var sb = new System.Text.StringBuilder();
            sb.Append("workspace=present;").Append(pageArt)
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
            // Scale evidence (mission section 10, resolutions and scales):
            // the workspace is its own top-level canvas; the native UI
            // canvas and its scaler say how the game scales its own UI.
            // The scaler is read on the native ROOT canvas (a scaler lives on
            // the root, not necessarily on StaticCanvas itself); invariant
            // number formats.
            System.Globalization.CultureInfo invariant = System.Globalization.CultureInfo.InvariantCulture;
            Canvas nativeCanvas = StaticCanvas.Instance == null
                ? null : StaticCanvas.Instance.GetComponent<Canvas>();
            Canvas nativeRoot = nativeCanvas == null ? null : nativeCanvas.rootCanvas;
            UnityEngine.UI.CanvasScaler nativeScaler = nativeRoot == null
                ? null : nativeRoot.GetComponent<UnityEngine.UI.CanvasScaler>();
            sb.Append(";screen=").Append(Screen.width).Append("x").Append(Screen.height)
                .Append(";ownScale=").Append(canvas == null ? "null" : canvas.scaleFactor.ToString("F3", invariant))
                .Append(";nativeRoot=").Append(nativeRoot == null ? "null" : nativeRoot.name)
                .Append(";nativeScale=").Append(nativeRoot == null ? "null" : nativeRoot.scaleFactor.ToString("F3", invariant))
                .Append(";nativeScaler=").Append(nativeScaler == null ? "none"
                    : nativeScaler.uiScaleMode + "/" + nativeScaler.referenceResolution.x.ToString("F0", invariant) + "x" +
                        nativeScaler.referenceResolution.y.ToString("F0", invariant) + "/" + nativeScaler.screenMatchMode +
                        "/match" + nativeScaler.matchWidthOrHeight.ToString("F2", invariant) +
                        "/factor" + nativeScaler.scaleFactor.ToString("F2", invariant));
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
                ";plannerHotkey=" + PlannerHotkey.Binding + ";armed-in-Main.OnUpdate" +
                ";plannerMode=" + root._plannerMode +
                (root._plannerModeWarning.Length == 0 ? string.Empty
                    : ";plannerModeWarning=" + root._plannerModeWarning) +
                ";castingFirstActive=" + root.CastingFirstActive +
                ";nativeCastingLocked=" + NativeCastingSessionPolicy.Locked +
                ";castingRun=" + (root._castingHost == null ? "host-missing"
                    : root._castingHost.IsRunning ? root._castingHost.ActiveRunId
                    : root._castingHost.Accepting ? "idle"
                    : "shutdown:" + root._castingHost.ShutdownReason);
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

        // Classic authoring through the classic screen's own controls: the
        // grid row of the ability and the details panel's target toggle for
        // the Long routine, then the casting mode. True when the routine now
        // wants that target in that mode.
        internal static bool ConfigureClassicCastForRuntime(string abilityGuid, string targetUnitId,
            string executionMode)
        {
            if (_instance == null || _instance._screen.View == null ||
                !_instance._screen.View.DispatchSourceRowForRuntime(abilityGuid)) return false;
            PlannerSetupModel model = _instance._session.Model;
            if (string.IsNullOrEmpty(targetUnitId)) return false;
            if (!model.IsTargetWanted("long", targetUnitId) &&
                !_instance._screen.View.DispatchTargetForRuntime(targetUnitId)) return false;
            // The casting mode through the Classic settings panel's own control.
            if (model.Profile.Execution.Mode != executionMode &&
                !_instance._screen.View.ToggleExecutionModeForRuntime()) return false;
            _instance._screen.View.RefreshCatalogForRuntime();
            return model.IsAssigned("long") && model.IsTargetWanted("long", targetUnitId) &&
                model.Profile.Execution.Mode == executionMode;
        }

        // The first party member the classic model can target.
        internal static string FirstClassicTargetForRuntime()
        {
            if (_instance == null || _instance._session == null || _instance._session.Model == null)
                return null;
            return _instance._session.Model.Snapshot.Units.Where(unit =>
                    unit.TargetValidation.Alive && unit.TargetValidation.Conscious &&
                    unit.TargetValidation.Friendly && unit.TargetValidation.Targetable)
                .Select(unit => unit.UnitId).FirstOrDefault();
        }

        // The classic routine's plan exactly as its execution will compute it.
        internal static CastPlan ClassicPlanForRuntime(string routineId)
        {
            if (_instance == null || _instance._session == null) return null;
            RoutinePlanResult preview = _instance._session.PreviewRoutine(routineId);
            return preview == null ? null : preview.Plan;
        }

        internal static ExecutionReport ClassicReportForRuntime
        {
            get { return _instance == null || _instance._session == null ? null : _instance._session.LastExecutionReport; }
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
            if (CastingFirstActive)
                return StartCastingFirstRoutine(routineId, completed, CastingApplyMode.Ordinary);
            if (!_enabled || _session == null || _session.IsExecuting || _quickStartPending ||
                (_castingHost != null && _castingHost.IsRunning))
                return false;
            _quickStartPending = true;
            StartClassicRun(routineId, completed, false);
            return true;
        }

        public bool TryStartReadyOnly(string routineId, Action<QuickExecutionResult> completed)
        {
            if (CastingFirstActive)
                return StartCastingFirstRoutine(routineId, completed,
                    CastingApplyMode.ReadyCastsOnly);
            if (!_enabled || _session == null || _session.IsExecuting || _quickStartPending ||
                (_castingHost != null && _castingHost.IsRunning))
                return false;
            _quickStartPending = true;
            StartClassicRun(routineId, completed, true);
            return true;
        }

        // The casting-first quick-run shared by the HUD buttons, the planner
        // hotkey flow and any other routine route: the same session Apply as
        // the workspace (current preflight on FRESH discovery, the routine
        // accepted contents, the gate, the exact projection) and the same
        // production dispatch boundary. Pressing a routine while a run is
        // active is the deliberate stop of that run.
        private bool StartCastingFirstRoutine(string routineId,
            Action<QuickExecutionResult> completed, CastingApplyMode mode)
        {
            if (!_enabled || _session == null) return false;
            if (_session.IsExecuting || _quickStartPending) return false;
            string name = char.ToUpperInvariant(routineId[0]) + routineId.Substring(1);
            if (_castingHost.IsRunning)
            {
                string running = _castingHost.ActiveScopeRoutineId ?? "the";
                _castingHost.RequestStop(CastingExecutionHost.PlayerStopReason);
                _log.Info("[KBP-CF-RUN] stop requested by routine press;routine=" + routineId +
                    ";running=" + running + ";effective=after-current-cast.");
                CompleteQuick(completed, new QuickExecutionResult(routineId, name,
                    QuickExecutionDisposition.Refused,
                    "Stopping the running " + running + " routine after the cast in progress.",
                    0, 0, 0));
                return true;
            }
            // One fresh discovery pass serves both the campaign identity and
            // the preflight inputs; a failed refresh refuses (never a stale
            // plan).
            CastingWorkspaceInputs inputs;
            try { inputs = BuildFreshCastingWorkspaceInputs(); }
            catch (Exception exception)
            {
                _log.Error("[KBP-CF-RUN] fresh preflight inputs unavailable;routine=" +
                    routineId + ".", exception);
                CompleteQuick(completed, new QuickExecutionResult(routineId, name,
                    QuickExecutionDisposition.Refused,
                    name + " was not cast: the party state could not be refreshed (" +
                    exception.Message + ").", 0, 0, 0));
                return true;
            }
            CastingWorkspaceSession session;
            string unavailable = EnsureCastingSession(out session, false);
            if (session == null)
            {
                CompleteQuick(completed, new QuickExecutionResult(routineId, name,
                    QuickExecutionDisposition.Refused,
                    "Cannot run " + name + ": " + unavailable, 0, 0, 0));
                return true;
            }
            name = session.RoutineDisplayName(routineId);
            WorkspaceApplyResult result;
            try { result = session.Apply(mode, routineId, inputs); }
            catch (Exception exception)
            {
                _log.Error("[KBP-CF-RUN] apply failed before submission;routine=" +
                    routineId + ".", exception);
                CompleteQuick(completed, new QuickExecutionResult(routineId, name,
                    QuickExecutionDisposition.Failed,
                    name + " failed before anything was cast: " + exception.Message,
                    0, 0, 0));
                return true;
            }
            if (!result.Allowed)
            {
                string refusal = CastingRunPresentation.DescribeRefusal(name, result);
                _log.Info("[KBP-CF-RUN] refused;routine=" + routineId + ";mode=" + mode +
                    ";reason=" + result.ReviewReason + ".");
                session.RecordAttempt(refusal);
                _lastCastingPress[PressKey(session, routineId)] = refusal;
                CompleteQuick(completed, new QuickExecutionResult(routineId, name,
                    QuickExecutionDisposition.Refused, refusal,
                    result.GateDecision == null ? 0 : result.GateDecision.ExecutableCastingIds.Count,
                    0, 0));
                // No floating result (the accepted HUD boundary): a refusal
                // the player resolves in the planner opens it on that
                // routine, with the reason in the footer.
                if (CastingRunPresentation.OpensPlanner(result.ReviewReason) &&
                    _castingWorkspace == null)
                {
                    try
                    {
                        session.SelectRoutine(routineId);
                        OpenSetup();
                    }
                    catch (Exception exception)
                    {
                        _log.Error("[KBP-CF-RUN] opening the planner after a refusal failed.", exception);
                    }
                }
                return true;
            }
            _pendingCastingCompletion = completed;
            LogRunStarted(routineId, mode, result);
            return true;
        }

        private static void CompleteQuick(Action<QuickExecutionResult> completed,
            QuickExecutionResult result)
        {
            if (completed != null) completed(result);
        }

        // The running Classic routine, owned here (batch 3 review): the
        // coroutine, its outer iterator and the completion to present.
        private IEnumerator _classicRunIterator;
        private Coroutine _classicRunCoroutine;
        private string _classicRunRoutineId;
        private Action<QuickExecutionResult> _classicRunCompleted;

        private void StartClassicRun(string routineId, Action<QuickExecutionResult> completed,
            bool readyOnlyExplicit)
        {
            IEnumerator run = ExecuteQuickRoutine(routineId, completed, readyOnlyExplicit);
            _classicRunIterator = run;
            _classicRunRoutineId = routineId;
            _classicRunCompleted = completed;
            _classicRunCoroutine = StartCoroutine(run);
        }

        // The Classic run's owned terminal (mod disabled, area change,
        // teardown, a runtime scenario's end): the coroutine stops and its
        // iterators are disposed, which runs the executor's cleanup for the
        // cast in progress; a run that had not reported yet reports its
        // interruption.
        private void EndClassicRun(string reason)
        {
            IEnumerator run = _classicRunIterator;
            if (run == null) return;
            _classicRunIterator = null;
            if (_classicRunCoroutine != null) StopCoroutine(_classicRunCoroutine);
            _classicRunCoroutine = null;
            try
            {
                IDisposable disposable = run as IDisposable;
                if (disposable != null) disposable.Dispose();
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-QUICK] classic run cleanup failed;reason=" + reason + ".", exception);
            }
            if (_session != null && _session.IsExecuting)
            {
                QuickExecutionResult result = _session.EndInterruptedExecution(_classicRunRoutineId, reason);
                if (_classicRunCompleted != null) _classicRunCompleted(result);
            }
            _classicRunCompleted = null;
        }

        internal static void EndClassicRunForRuntime(string reason)
        {
            if (_instance != null) _instance.EndClassicRun(reason);
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
            IEnumerator routine = null;
            try
            {
                // The session's own checks run at the press; only its casting
                // phase waits for the world (final review A1).
                routine = _session.ExecuteRoutine(routineId,
                    observedCompletion, readyOnlyExplicit);
                bool closeRequested = false;
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
                    // Final review A1 (re-review): an accepted run (the
                    // session's checks passed and it is executing) closes the
                    // open Classic screen, which pauses the game, so the party
                    // casts at once; its result is shown the next time the
                    // planner opens.
                    if (!closeRequested && _session.IsExecuting && _screen != null && _screen.IsOpen)
                    {
                        closeRequested = true;
                        _closeScreenForClassicRun = true;
                    }
                    yield return current;
                }
            }
            finally
            {
                _quickStartPending = false;
                // A run stopped early still runs the session's own finally
                // (the executor's cleanup): the inner iterator is disposed
                // here, never abandoned.
                IDisposable inner = routine as IDisposable;
                if (inner != null)
                {
                    try { inner.Dispose(); }
                    catch (Exception exception)
                    {
                        _log.Error("[KBP-QUICK] classic routine cleanup failed.", exception);
                    }
                }
                if (completedCalled && ReferenceEquals(_classicRunCompleted, completed))
                {
                    _classicRunIterator = null;
                    _classicRunCoroutine = null;
                    _classicRunCompleted = null;
                }
            }
        }

        private void Initialize(string modPath, ModLog log)
        {
            _modPath = modPath;
            _log = log;
            _session = new PlannerUiSession(modPath, log);
            _session.ClassicSavesSuppressed = () => CastingFirstActive;
            _session.ClassicWorldRuns = () => WorldRunsForCasting && _castingWorkspace == null &&
                (_screen == null || !_screen.IsOpen);
            _plannerModeStore = new PlannerModeStore(modPath);
            string modeWarning;
            _plannerMode = _plannerModeStore.Load(out modeWarning);
            _plannerModeWarning = modeWarning ?? string.Empty;
            if (NativeCastingSessionPolicy.Locked)
            {
                // Automation runs are deterministic: a persisted player mode
                // never changes which planner a scenario drives (workspace
                // scenarios select casting-first explicitly).
                _plannerMode = PlannerMode.Classic;
                _plannerModeWarning = "runtime-test-session:persisted-mode-ignored";
            }
            _castingHost = new CastingExecutionHost(CreateCastingExecutor,
                () => _castingWorldClock.Milliseconds);
            _castingHost.RunCompleted = OnCastingRunCompleted;
            _log.Info("[KBP-MODE] planner mode=" + _plannerMode +
                (_plannerModeWarning.Length == 0 ? string.Empty : ";warning=" + _plannerModeWarning) +
                ";nativeCastingLocked=" + NativeCastingSessionPolicy.Locked + ".");
            _diagnostics = new BuffPlannerUiLifecycleDiagnostics();
            _quick = new BuffPlannerQuickExecuteController(this, _diagnostics, PresentQuickResult);
            _screen = new BuffPlannerScreenController(_session, _diagnostics, log,
                routineId => ExecuteRoutineRequest(routineId), PlayNativeSetupOpenSound,
                routineId => ExecuteRoutineRequest(routineId, true));
            _hud = new BuffPlannerHudButtonController(_session, _diagnostics, log,
                () => { OpenSetup(); }, routineId => ExecuteRoutineRequest(routineId),
                CastingFirstRoutineTooltip);
            _spellbookEntry = new BuffPlannerSpellbookEntryController(
                value => _log.Info(value),
                () => OpenSetup(),
                // Final review B6: in casting-first mode the planner that
                // opens is the workspace, not the Classic screen.
                () => (_screen != null && _screen.IsOpen) || _castingWorkspace != null,
                PlannerUiTheme.Resolve(null),
                () => (_screen != null && _screen.LifecycleState ==
                    PlannerScreenLifecycleState.Open) || _castingWorkspace != null,
                RequestNativeEscapeVeil);
            try
            {
                _eventSubscription = EventBus.Subscribe((object)this);
                if (_eventSubscription != null) _activeEventSubscriptions++;
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

        // Single routine-execution entry for every route (HUD buttons, the
        // legacy screen, hotkey and spellbook flows). The quick controller
        // records the flow diagnostics and calls TryStart, which sends the
        // request to the casting-first pipeline whenever that planner is
        // active - no route can reach the legacy executor then.
        private bool ExecuteRoutineRequest(string routineId, bool readyOnly = false)
        {
            if (_quick == null) return false;
            return _quick.Execute(routineId, readyOnly);
        }

        private bool OpenSetup()
        {
            if (CastingFirstActive)
                return OpenCastingWorkspace();
            return _screen != null && _screen.Open();
        }

        // The casting-first planner (selected in the mod settings, or by a
        // workspace runtime scenario) renders instead of the classic screen.
        // It consumes the same discovery data through the classic session's
        // model (one snapshot, one option set, one effect map) and owns its
        // records through its own session; the classic authoring path is
        // never open at the same time. Its Apply reaches the native host
        // through the production dispatch boundary (refusing in an
        // automated test session; see NativeCastingSessionPolicy).
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
                // Campaign identity resolves BEFORE any ownership is
                // acquired: an unresolved identity refuses without ever
                // holding the input lease, and every later failure path
                // funnels through the catch, which releases the local lease
                // exactly once (review H3).
                CastingWorkspaceSession workspaceSession;
                string unresolved = EnsureCastingSession(out workspaceSession);
                string campaignId = workspaceSession == null ? null : workspaceSession.CampaignId;
                if (workspaceSession == null)
                {
                    // Unresolved/transitional campaign identity must not
                    // bind arbitrary work to an unknown-campaign fallback
                    // (review G3) — and must not leak an acquired lease.
                    LogUiUnavailable(
                        "casting-workspace: campaign identity unresolved (" + unresolved + ")");
                    return false;
                }
                // Acquire the established game-mode/selection input lease
                // exactly once per open, AFTER identity resolves and BEFORE
                // construction; a failed acquire self-restores, and the
                // catch below releases this local reference on any
                // subsequent failure (reviews R2, H3).
                lease = BuffPlannerInputLease.Acquire(new KingmakerPlannerInputBoundary());
                _castingWorkspace = new CastingWorkspaceScreenView(
                    StaticCanvas.Instance, workspaceSession,
                    BuildCastingWorkspaceInputs, BuildFreshCastingWorkspaceInputs,
                    CloseCastingWorkspace);
                _workspaceInputLease = lease;
                lease = null;
                _castingWorkspace.RefreshView();
                _log.Info("[KBP-WORKSPACE] casting-first workspace opened;" +
                    "campaign=" + campaignId +
                    ";dispatch=" + workspaceSession.DispatchDisposition +
                    ";load=" + workspaceSession.LoadStatus +
                    ";legacyImport=" + (workspaceSession.MigrationStatus.HasValue
                        ? workspaceSession.MigrationStatus.Value.ToString() : "not-attempted") +
                    (workspaceSession.ImportReport == null ? string.Empty
                        : ";imported=" + workspaceSession.ImportReport.ResultingCastingCount));
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

        private bool _closeScreenForClassicRun;
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
            return CurrentCastingInputs();
        }

        // Apply and quick-run preflight: discovery is re-read NOW and a
        // failed refresh refuses the run instead of falling back to earlier
        // state (the bounded provider above is for rendering only).
        private CastingWorkspaceInputs BuildFreshCastingWorkspaceInputs()
        {
            _lastWorkspaceInputsRefreshUtc = DateTime.UtcNow;
            _session.Refresh();
            if (_session.Model == null)
                throw new InvalidOperationException(_session.Status ?? "discovery failed");
            return CurrentCastingInputs();
        }

        private CastingWorkspaceInputs CurrentCastingInputs()
        {
            return new CastingWorkspaceInputs(
                _session.Model.Snapshot,
                _session.ProviderOptions,
                _session.Model.EffectsBySource,
                _session.Model.Enhancements,
                null,
                _session.ActiveEffects);
        }

        // Resolves (or reuses) the casting-first session for the loaded
        // campaign. Returns null on success, otherwise why none exists.
        private string EnsureCastingSession(out CastingWorkspaceSession session,
            bool refresh = true)
        {
            session = null;
            if (refresh)
            {
                _lastWorkspaceInputsRefreshUtc = DateTime.UtcNow;
                _session.Refresh();
            }
            string campaignId = _session.Model == null || _session.Model.Profile == null
                ? null : _session.Model.Profile.CampaignId;
            if (string.IsNullOrEmpty(campaignId))
                return string.IsNullOrEmpty(_session.Status) ? "no campaign is loaded" : _session.Status;
            session = CastingWorkspaceSessionBinding.Resolve(
                _castingWorkspaceSession, campaignId,
                delegate(string id) { return CreateCastingSession(id); },
                delegate(string message) { _log.Info(message); });
            if (session == null) return "campaign identity unresolved";
            _castingWorkspaceSession = session;
            return null;
        }

        private CastingWorkspaceSession CreateCastingSession(string campaignId)
        {
            return new CastingWorkspaceSession(_modPath, campaignId, CreateDispatchBoundary(),
                _session.Model == null ? null : _session.Model.SourceGroupings());
        }

        // Ordinary play submits through the production boundary; a
        // runtime-test session is locked to an explicit refusal.
        private ICastingDispatchBoundary CreateDispatchBoundary()
        {
            if (NativeCastingSessionPolicy.Locked)
                return new DisabledCastingDispatchBoundary(NativeCastingSessionPolicy.LockReason);
            return new NativeCastingDispatchBoundary(_castingHost,
                () => _castingWorkspaceSession == null
                    ? ExecutionProfile.Default()
                    : _castingWorkspaceSession.ExecutionSettings,
                message => _log.Info("[KBP-CF-RUN] " + message));
        }

        // The same executors and adapters the classic planner uses: animated
        // native casting by default; Instant mode through the hybrid executor
        // (animated only where a step requires a native command or the
        // player allowed the animated fallback).
        private ICastExecutor CreateCastingExecutor(ExecutionProfile settings)
        {
            if (!string.Equals(settings.Mode, "instant", StringComparison.Ordinal))
                return new AnimatedCastExecutor(new KingmakerAnimatedCastAdapter(),
                    settings.OutOfCombatOnly);
            IEnumerable<CastEnhancementSnapshot> enhancements = _session.Model == null
                ? new CastEnhancementSnapshot[0] : _session.Model.Enhancements;
            var nativeCommand = new HashSet<string>(enhancements
                .Where(value => value != null && value.RequiresNativeCommand)
                .Select(value => value.EnhancementId), StringComparer.Ordinal);
            return new HybridCastExecutor(
                new KingmakerInstantCastAdapter(_log.Info), new KingmakerAnimatedCastAdapter(),
                settings.AllowAnimatedFallback, settings.OutOfCombatOnly,
                step => step.EnhancementIds.Any(nativeCommand.Contains),
                (index, step, animated, route) => _log.Info("[KBP-CF-ROUTE] step=" + index +
                    ";casting=" + step.AssignmentId + ";provider=" + step.Provider.Canonical +
                    ";animated=" + animated + ";" + route + "."));
        }

        private void OnCastingRunCompleted(CastingRunReport report)
        {
            // The next inputs re-read discovery (spent slots, new effects).
            _lastWorkspaceInputsRefreshUtc = DateTime.MinValue;
            CastingWorkspaceSession session = _castingWorkspaceSession;
            string routineId = string.IsNullOrEmpty(report.ScopeRoutineId)
                ? "long" : report.ScopeRoutineId;
            string name = session == null ? routineId : session.RoutineDisplayName(routineId);
            if (session != null) session.RecordRunReport(report);
            LogRunReport(report, session);
            _lastCastingPress[PressKey(session, routineId)] = CastingRunPresentation.Describe(report, name,
                session == null ? (Func<string, string>)null : session.CastingLabel);
            Func<string, string> label = null;
            if (session != null) label = session.CastingLabel;
            QuickExecutionResult quick = CastingRunPresentation.ToQuickResult(report, name, label);
            Action<QuickExecutionResult> pending = _pendingCastingCompletion;
            _pendingCastingCompletion = null;
            try
            {
                if (pending != null) pending(quick);
                else PresentQuickResult(quick);
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-CF-RUN] result presentation failed.", exception);
            }
            // Deliberately no floating or native-log result (the accepted
            // HUD boundary): the full result is logged, shown in the planner
            // footer, and kept on the session for the next planner open.
        }

        // HUD routine tooltips in casting-first mode describe the casting
        // plan (not the classic profile): castings in the routine, whether
        // it is accepted, the run state, and the stop gesture.
        private string CastingFirstRoutineTooltip(string routineId)
        {
            if (!CastingFirstActive) return null;
            string name = char.ToUpperInvariant(routineId[0]) + routineId.Substring(1);
            if (_castingHost != null && _castingHost.IsRunning)
                return (_castingRunHeld
                        ? "Waiting for the game to run (it is paused, or a window is open). "
                        : string.Empty) +
                    (string.Equals(_castingHost.ActiveScopeRoutineId, routineId,
                        StringComparison.Ordinal)
                        ? name + " is running. Press again to stop it after the cast in progress."
                        : "Another routine is running. Press to stop it after the cast in progress.");
            CastingWorkspaceSession session = _castingWorkspaceSession;
            // Final review B7: a session, or a press result, kept from another
            // campaign never describes the one now loaded.
            string loadedCampaignId = Game.Instance == null || Game.Instance.Player == null
                ? null : Game.Instance.Player.GameId;
            if (session != null && !string.Equals(session.CampaignId, loadedCampaignId,
                    StringComparison.Ordinal))
                session = null;
            if (session == null)
                return "Cast " + name + " (casting-first planner). Open the planner to " +
                    "review and accept the routine first.";
            name = session.RoutineDisplayName(routineId);
            int castings = session.Document.Castings.Count(value => value != null &&
                string.Equals(value.RoutineId, routineId, StringComparison.Ordinal));
            // The tooltip never recomputes the plan: an acceptance on file
            // is described as such, not as a promise that the press runs.
            string acceptance;
            switch (session.AcceptanceStandingFor(routineId))
            {
                case CastingAcceptanceStanding.Current:
                    acceptance = ". Accepted.";
                    break;
                case CastingAcceptanceStanding.OnFile:
                    acceptance = ". An accepted plan is on file; it runs only if nothing " +
                        "changed since.";
                    break;
                case CastingAcceptanceStanding.Changed:
                    acceptance = ". It differs from the accepted plan right now - open the " +
                        "planner to see why.";
                    break;
                default:
                    acceptance = ". Not yet accepted - open the planner to review it.";
                    break;
            }
            string last;
            _lastCastingPress.TryGetValue(PressKey(session, routineId), out last);
            return "Cast " + name + ": " + castings + (castings == 1 ? " casting" : " castings") +
                ", " + session.ExecutionMode + " mode" + acceptance +
                (string.IsNullOrEmpty(last) ? string.Empty
                    : " Last: " + (last.Length <= 180 ? last : last.Substring(0, 177) + "..."));
        }

        // A press result belongs to the campaign whose session produced it.
        private static string PressKey(CastingWorkspaceSession session, string routineId)
        {
            return (session == null ? string.Empty : session.CampaignId) + "|" + routineId;
        }

        private void LogRunStarted(string routineId, CastingApplyMode mode,
            WorkspaceApplyResult result)
        {
            _log.Info("[KBP-CF-RUN] quick-run submitted;routine=" + routineId + ";mode=" + mode +
                ";dispatch=" + result.Dispatch.Reason +
                ";castings=" + string.Join(",", result.Projection.CastingIds.ToArray()) +
                ";projection=" + result.Projection.ProjectionId +
                ";omissions=" + result.GateDecision.Omissions.Count + ".");
        }

        private void LogRunReport(CastingRunReport report, CastingWorkspaceSession session)
        {
            _log.Info("[KBP-CF-RUN] terminal;run=" + report.RunId + ";routine=" +
                report.ScopeRoutineId + ";mode=" + report.Mode + ";projection=" +
                report.ProjectionId + ";terminal=" + report.TerminalReason +
                ";planned=" + report.Planned + ";submitted=" + report.Submitted +
                ";confirmed=" + report.Confirmed + ";failed=" + report.Failed +
                ";cancelled=" + report.CancelledCastings + ";notProcessed=" + report.NotProcessed +
                ";skipped=" + report.Skipped + ";omitted=" + report.Omitted +
                ";resourcesSpent=" + report.ResourcesSpent +
                ";cleanupFailures=" + string.Join("|", report.CleanupFailures.ToArray()) + ".");
            foreach (CastingOutcomeEntry entry in report.Entries)
                _log.Info("[KBP-CF-RUN] casting;run=" + report.RunId + ";casting=" +
                    entry.CastingId + ";label=" +
                    (session == null ? entry.CastingId : session.CastingLabel(entry.CastingId)) +
                    ";state=" + entry.State + ";planned=" + entry.Planned +
                    ";submitted=" + entry.Submitted + ";spendReported=" + entry.SpendReported +
                    ";free=" + entry.FreeCast + ";detail=" + entry.Detail + ".");
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
            // One step of the active casting run per frame (executors yield
            // per frame), only while the world runs and no planner window
            // holds the game; a held run waits, and its deadline counts only
            // running time. The host ends the run on its deadline.
            if (_castingHost != null)
            {
                bool worldRuns = WorldRunsForCasting && _castingWorkspace == null && !_screen.IsOpen;
                _castingRunHeld = _castingHost.IsRunning && !worldRuns;
                _castingWorldClock.Advance(worldRuns, deltaTime);
                if (worldRuns)
                {
                    try { _castingHost.Pump(); }
                    catch (Exception exception)
                    {
                        _log.Error("[KBP-CF-RUN] run pump failed.", exception);
                    }
                }
            }
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
                // The close an accepted Classic run asked for, one frame after
                // the press (never inside the button's own callback).
                if (_closeScreenForClassicRun)
                {
                    _closeScreenForClassicRun = false;
                    if (_screen.LifecycleState != PlannerScreenLifecycleState.Closed)
                    {
                        _screen.Close();
                        _log.Info("[KBP-QUICK] Classic screen closed for the accepted run.");
                    }
                }
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
            // An area change (including loading another save) ends a running
            // routine through its owned terminal; new runs remain possible.
            if (_castingHost != null) _castingHost.Cancel("area-unloading");
            EndClassicRun("area-unloading");
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
            int seen;
            _lifecycleSignalsByName[name] = _lifecycleSignalsByName.TryGetValue(name, out seen) ? seen + 1 : 1;
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
            if (_screen != null) _screen.Present(result);
            _log.Info("Routine UI result: " + result.RoutineId + " " +
                result.Disposition + " " + result.Message);
        }

        private void OnDisable()
        {
            if (_castingHost != null) _castingHost.Shutdown("ui-root-disabled");
            SuspendHudInstall("ui-root-disabled");
            ReleasePlayerUi();
        }

        private void OnEnable()
        {
            if (_castingHost != null && _enabled && !_disposed) _castingHost.Resume();
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
            // End any casting run first, through its owned terminal, while
            // the result can still be logged and presented.
            if (_castingHost != null) _castingHost.Shutdown("root-teardown");
            EndClassicRun("root-teardown");
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
                _activeEventSubscriptions--;
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
