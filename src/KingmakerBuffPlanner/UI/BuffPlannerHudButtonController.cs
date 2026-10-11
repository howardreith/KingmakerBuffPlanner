using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.UI.Constructor;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UI.IngameMenu;
using Kingmaker.UI.Tooltip;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.RuntimeTesting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class BuffPlannerHudButtonController : IDisposable
    {
        internal const string RootName = "KingmakerBuffPlanner.HudButtons";
        private readonly PlannerUiSession _session;
        private readonly BuffPlannerUiLifecycleDiagnostics _diagnostics;
        private readonly ModLog _log;
        private readonly Action _openSetup;
        private readonly Action<string> _quickExecute;
        // Whether casting-first is the active planner (Setup tooltip only:
        // the Setup button always opens the planner, the moon/Long button
        // always runs Long).
        private readonly Func<bool> _castingFirst;
        private readonly List<Sprite> _ownedSprites = new List<Sprite>();
        private readonly List<Texture2D> _ownedTextures = new List<Texture2D>();
        private RectTransform _root;
        private RectTransform _nativeCluster;
        private GraphicRaycaster _nativeRaycaster;
        private NativeHudButtonStyle _nativeStyle;
        private TooltipTrigger[] _nativeTooltips = new TooltipTrigger[0];
        private Button[] _buttons = new Button[0];
        private Func<string>[] _tooltips = new Func<string>[0];
        private int _listenerCount;
        private IngameMenuController _anchorController;
        private DeferredUiReadinessGate _readiness = new DeferredUiReadinessGate(2);
        private readonly HudCandidateValidationGate _candidateValidation =
            new HudCandidateValidationGate(120);
        private bool _candidateOwned;
        private bool _installed;
        private int _installAttempts;
        private int _candidateCreateCount;
        private int _candidateHudIdentity;
        private readonly List<Tuple<Button, UnityAction>> _runtimeNativeListeners =
            new List<Tuple<Button, UnityAction>>();
        private string _lastFailure = "not-attempted";
        private string _lastValidationFailure = string.Empty;
        private HudInstallAttemptResult _lastAttemptResult = HudInstallAttemptResult.None;
        private HudCandidateTickResult _lastCandidateTickResult = HudCandidateTickResult.None;
        private bool _nativeTooltipActive;
        // Native UI finish (UI-09): the stepped enclosure the cells sit in
        // and the native art they are made from.
        private static readonly string[] CellKinds = { "setup", "long", "important", "short" };
        private static readonly Dictionary<string, HudCellArtResult> CellArtCache =
            new Dictionary<string, HudCellArtResult>(StringComparer.Ordinal);
        private static readonly Dictionary<int, HudPixels> NativeCellPixels = new Dictionary<int, HudPixels>();
        private HudStepLayout _step;
        private Image _nativeFrame;
        private Vector2 _stepOrigin;
        private readonly string[] _cellArtEvidence = new string[4];
        private readonly Vector2[] _inkOffsets = new Vector2[4];

        internal BuffPlannerHudButtonController(
            PlannerUiSession session,
            BuffPlannerUiLifecycleDiagnostics diagnostics,
            ModLog log,
            Action openSetup,
            Action<string> quickExecute,
            Func<string, string> routineTooltipOverride = null,
            Func<bool> castingFirstForMoon = null)
        {
            _session = session ?? throw new ArgumentNullException("session");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
            _log = log ?? throw new ArgumentNullException("log");
            _openSetup = openSetup ?? throw new ArgumentNullException("openSetup");
            _quickExecute = quickExecute ?? throw new ArgumentNullException("quickExecute");
            _routineTooltipOverride = routineTooltipOverride;
            _castingFirst = castingFirstForMoon;
        }

        // Casting-first mode describes its own plan; null keeps the classic
        // tooltip.
        private readonly Func<string, string> _routineTooltipOverride;

        internal bool IsInstalled
        {
            get
            {
                if (!_installed) return false;
                string failure;
                return IsHostingChainViable(CurrentHudHost(), out failure);
            }
        }
        internal bool HasCandidate { get { return _candidateOwned && _root != null; } }
        internal int ButtonCount { get { return _buttons.Count(button => button != null); } }
        internal int ListenerCount { get { return _listenerCount; } }
        internal string AnchorPath { get; private set; }
        internal string RaycastCanvasPath { get; private set; }
        internal bool RowAboveNativeCluster { get; private set; }
        internal bool RowLeftAlignedWithNativeCluster { get; private set; }
        internal bool GlyphsCentered { get; private set; }
        internal bool VisibleHitboxesOwnRaycasts { get; private set; }
        internal bool FrameEnclosesRow { get; private set; }
        internal string ButtonOrder { get { return "Setup|Long|Important|Short"; } }
        internal int RuntimeUnderlyingNativeActivationCount { get; private set; }
        internal string LastFailure { get { return _lastFailure; } }
        internal string LastValidationFailure { get { return _lastValidationFailure; } }
        internal string HostingChainFailure
        {
            get
            {
                string failure;
                return IsHostingChainViable(CurrentHudHost(), out failure) ? string.Empty : failure;
            }
        }
        internal HudInstallAttemptResult LastAttemptResult { get { return _lastAttemptResult; } }
        internal HudCandidateTickResult LastCandidateTickResult { get { return _lastCandidateTickResult; } }
        internal int InstallAttempts { get { return _installAttempts; } }
        internal int CandidateCreateCount { get { return _candidateCreateCount; } }
        internal int CandidateValidationFailures { get { return _candidateValidation.FailureFrames; } }
        internal int CandidateHudIdentity { get { return _candidateHudIdentity; } }
        internal int AnchorInstanceId
        {
            get { return _anchorController == null ? 0 : _anchorController.gameObject.GetInstanceID(); }
        }
        internal int NativeClusterInstanceId
        {
            get { return _nativeCluster == null ? 0 : _nativeCluster.gameObject.GetInstanceID(); }
        }
        internal bool RootActive
        {
            get { return _root != null && _root.gameObject.activeInHierarchy; }
        }
        internal bool AnchorActive
        {
            get
            {
                return _anchorController != null &&
                    _anchorController.gameObject.activeInHierarchy;
            }
        }
        internal bool NativeClusterActive
        {
            get { return _nativeCluster != null && _nativeCluster.gameObject.activeInHierarchy; }
        }
        internal int RootInstanceId { get { return _root == null ? 0 : _root.gameObject.GetInstanceID(); } }
        internal string ObjectEvidence
        {
            get
            {
                if (_root == null) return "root=absent";
                var entries = new List<string>();
                for (int index = 0; index < _buttons.Length; index++)
                {
                    Button button = _buttons[index];
                    if (button == null) { entries.Add(index + ":null"); continue; }
                    var corners = new Vector3[4];
                    ((RectTransform)button.transform).GetWorldCorners(corners);
                    Image face = button.GetComponent<Image>();
                    Image highlight = button.targetGraphic as Image;
                    entries.Add(index + ":name=" + button.name +
                        ",id=" + button.gameObject.GetInstanceID() +
                        ",active=" + button.gameObject.activeInHierarchy +
                        ",interactable=" + button.interactable +
                        ",tile=" + ColorEvidence(face == null ? Color.clear : face.color) +
                        ",nativeSkin=" + (face != null && face.sprite != null && highlight != null &&
                            highlight != face && highlight.sprite != null) +
                        ",transition=" + button.transition +
                        ",highlight=" + (highlight == null ? "missing" : highlight.name) +
                        ",cellArt=" + (index < _cellArtEvidence.Length && _cellArtEvidence[index] != null
                            ? _cellArtEvidence[index] : "missing") +
                        ",screenCenter=" + ScreenCenter((RectTransform)button.transform) +
                        ",corners=" + string.Join("|", corners.Select(value => value.ToString()).ToArray()));
                }
                return "root=" + RootInstanceId + ";host=" + AnchorPath +
                    ";raycaster=" + RaycastCanvasPath + ";" + FrameEvidenceForRuntime + ";style=" +
                    (_nativeStyle == null ? "missing" : _nativeStyle.Evidence) + ";" +
                    string.Join(";", entries.ToArray());
            }
        }

        private static string ColorEvidence(Color color)
        {
            return color.r.ToString("0.000") + "," + color.g.ToString("0.000") + "," +
                color.b.ToString("0.000") + "," + color.a.ToString("0.000");
        }

        internal HudInstallAttemptResult TryInstall(UISectionHUDController hudHost)
        {
            _installAttempts++;
            if (_candidateOwned || _root != null || _anchorController != null || _nativeCluster != null)
            {
                string staleReason;
                if (IsHostingChainViable(hudHost, out staleReason))
                {
                    _lastAttemptResult = _installed
                        ? HudInstallAttemptResult.AlreadyInstalled
                        : HudInstallAttemptResult.CandidatePending;
                    return _lastAttemptResult;
                }
                int staleCandidate = RootInstanceId;
                int staleAnchor = AnchorInstanceId;
                _lastFailure = "candidate-stale:" + staleReason;
                _log.Info("[KBP-BOOT] HUD candidate stale;reason=" + staleReason +
                    ";candidate=" + staleCandidate + ";candidateHud=" + _candidateHudIdentity +
                    ";currentHud=" + (hudHost == null ? 0 : hudHost.GetInstanceID()) +
                    ";anchor=" + staleAnchor + ";installed=" + _installed + ".");
                DisposeOwnedRoot();
                _lastAttemptResult = HudInstallAttemptResult.StaleCandidateDisposed;
                return _lastAttemptResult;
            }
            if (hudHost == null) return RejectHost("campaign-hud-controller-not-found",
                HudInstallAttemptResult.NoActiveHud);
            if (!hudHost.gameObject.activeInHierarchy)
                return RejectHost("campaign-hud-controller-inactive:" + GetPath(hudHost.transform),
                    HudInstallAttemptResult.NoActiveHud);
            long findStartedAt = RuntimePerformanceDiagnostics.BeginOperation();
            IngameMenuController controller = null;
            try
            {
                controller = hudHost.GetComponentInChildren<IngameMenuController>(true);
            }
            finally
            {
                RuntimePerformanceDiagnostics.RecordHudObjectFind(
                    findStartedAt, controller != null);
            }
            if (controller == null) return RejectHost("ingame-menu-controller-not-found",
                HudInstallAttemptResult.RetryableNotReady);
            if (!controller.gameObject.activeInHierarchy)
                return RejectHost("ingame-menu-controller-inactive:" + GetPath(controller.transform),
                    HudInstallAttemptResult.RetryableNotReady);
            ButtonPF formation = ResolveFormationButton(controller);
            if (formation == null) return RejectHost("formation-button-field-null",
                HudInstallAttemptResult.RetryableNotReady);
            if (formation.transform.parent == null) return RejectHost("formation-button-parent-null",
                HudInstallAttemptResult.RetryableNotReady);
            RectTransform reference = formation.transform as RectTransform;
            RectTransform parent = formation.transform.parent as RectTransform;
            if (reference == null) return RejectHost("formation-button-not-rect-transform",
                HudInstallAttemptResult.RetryableNotReady);
            if (parent == null) return RejectHost("formation-parent-not-rect-transform",
                HudInstallAttemptResult.RetryableNotReady);
            GraphicRaycaster raycaster = parent.GetComponentInParent<GraphicRaycaster>();
            if (raycaster == null) return RejectHost("native-graphic-raycaster-not-found:" + GetPath(parent),
                HudInstallAttemptResult.RetryableNotReady);
            if (!raycaster.isActiveAndEnabled)
                return RejectHost("native-graphic-raycaster-inactive:" + GetPath(raycaster.transform),
                    HudInstallAttemptResult.RetryableNotReady);
            if (EventSystem.current == null) return RejectHost("event-system-not-ready",
                HudInstallAttemptResult.RetryableNotReady);
            if (parent.GetComponentsInParent<CanvasGroup>(true).Any(group =>
                group != null && (!group.interactable || !group.blocksRaycasts)))
                return RejectHost("native-canvas-group-blocked:" + GetPath(parent),
                    HudInstallAttemptResult.RetryableNotReady);

            Transform duplicate = parent.Find(RootName);
            if (duplicate != null) UnityEngine.Object.Destroy(duplicate.gameObject);
            _anchorController = controller;
            _nativeCluster = parent;
            _nativeRaycaster = raycaster;
            _nativeStyle = NativeHudButtonStyle.Capture(formation);
            if (_nativeStyle == null || !_nativeStyle.IsComplete)
                return RejectHost("native-button-style-contract-incomplete",
                    HudInstallAttemptResult.RetryableNotReady);
            // Native UI finish (UI-09): the owner's stepped enclosure (R12),
            // measured from the live native grid and frame.
            Image nativeFrame = NativeFrame(parent);
            if (nativeFrame == null)
                return RejectHost("native-frame-missing:" + GetPath(parent),
                    HudInstallAttemptResult.RetryableNotReady);
            string geometryFailure;
            HudStepLayout step = MeasureStep(parent, nativeFrame, reference, out geometryFailure);
            if (step == null)
                return RejectHost("hud-step-geometry:" + geometryFailure,
                    HudInstallAttemptResult.RetryableNotReady);
            AnchorPath = GetPath(parent);
            RaycastCanvasPath = GetPath(raycaster.transform);
            _step = step;
            _nativeFrame = nativeFrame;
            _root = KingmakerUiFactory.CreateRect(RootName, parent);
            _candidateOwned = true;
            _candidateHudIdentity = hudHost.GetInstanceID();
            // The enclosure's own rect, from the native top row's top edge
            // to the raised frame, on the panel's bottom-left corner.
            _stepOrigin = new Vector2(0f, step.Strip.Y);
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.zero;
            _root.pivot = Vector2.zero;
            _root.anchoredPosition = _stepOrigin;
            _root.sizeDelta = new Vector2(Mathf.Max(step.Corner.Right, step.Junction.Right),
                step.RaisedTop - step.Strip.Y);
            LayoutElement rootLayout = _root.gameObject.AddComponent<LayoutElement>();
            rootLayout.ignoreLayout = true;
            float width = reference.rect.width;
            float height = reference.rect.height;
            try
            {
                Rect frameRegion = nativeFrame.sprite.rect;
                foreach (HudFramePiece piece in step.FramePieces((int)frameRegion.width, (int)frameRegion.height,
                        nativeFrame.rectTransform.rect.width, (int)nativeFrame.sprite.border.x,
                        (int)nativeFrame.sprite.border.z))
                    CreateFramePiece(piece, nativeFrame);
                _buttons = CreateCells(width, height);
            }
            catch (Exception exception)
            {
                // Never a half-native HUD: without the native art the cells
                // are not built at all, and the install retries.
                DisposeOwnedRoot();
                return RejectHost("native-cell-art:" + exception.GetType().Name + ":" + exception.Message,
                    HudInstallAttemptResult.RetryableNotReady);
            }
            for (int index = 0; index < _buttons.Length; index++)
                PlaceInStep((RectTransform)_buttons[index].transform, step.Cells[index]);
            _nativeTooltips = _buttons.Select(button => button == null ? null :
                button.GetComponent<TooltipTrigger>()).Where(trigger => trigger != null).ToArray();
            foreach (Button button in _buttons) button.interactable = false;
            _tooltips = new Func<string>[]
            {
                SetupTooltip,
                () => RoutineTooltip("long"),
                () => RoutineTooltip("important"),
                () => RoutineTooltip("short")
            };
            _root.SetAsLastSibling();
            _installed = false;
            _candidateValidation.Reset();
            _lastValidationFailure = string.Empty;
            _readiness.Reset();
            _candidateCreateCount++;
            _lastFailure = "candidate-awaiting-deferred-readiness";
            _lastAttemptResult = HudInstallAttemptResult.CandidateCreated;
            _lastCandidateTickResult = HudCandidateTickResult.None;
            _log.Info("[KBP-HUD-STYLE] " + _nativeStyle.Evidence + ".");
            _log.Info("[KBP-HUD-FRAME] " + FrameEvidenceForRuntime + ";cells=" +
                string.Join("|", _cellArtEvidence.Select(value => value ?? "missing").ToArray()) + ".");
            _log.Info("[KBP-BOOT] HUD install attempted;attempt=" + _installAttempts +
                ";candidateCreate=" + _candidateCreateCount + ";candidate=" + RootInstanceId +
                ";hud=" + _candidateHudIdentity + ";anchor=" + AnchorInstanceId +
                ";host=" + AnchorPath +
                ";raycastCanvas=" + RaycastCanvasPath + ";deferredFrames=2.");
            return _lastAttemptResult;
        }

        // The four cells in the HUD's order (Setup, Long, Important, Short).
        private Button[] CreateCells(float width, float height)
        {
            return new[]
            {
                CreatePlannerButton("Setup", "setup", width, height, _openSetup,
                    () => SetupTooltip()),
                CreatePlannerButton("Long", "long", width, height,
                    () => _quickExecute("long"), () => RoutineTooltip("long")),
                CreatePlannerButton("Important", "important", width, height,
                    () => _quickExecute("important"), () => RoutineTooltip("important")),
                CreatePlannerButton("Short", "short", width, height,
                    () => _quickExecute("short"), () => RoutineTooltip("short"))
            };
        }

        internal void RefreshAvailability()
        {
            if (_buttons.Length != 4) return;
            _buttons[0].interactable = !_session.IsExecuting;
            for (int index = 1; index < 4; index++)
                _buttons[index].interactable = !_session.IsExecuting;
            RefreshNativeTooltipText();
        }

        internal HudCandidateTickResult Tick(UISectionHUDController hudHost)
        {
            _lastCandidateTickResult = HudCandidateTickResult.None;
            if (_candidateOwned || _root != null || _anchorController != null || _nativeCluster != null)
            {
                string staleReason;
                if (!IsHostingChainViable(hudHost, out staleReason))
                {
                    int staleCandidate = RootInstanceId;
                    int staleAnchor = AnchorInstanceId;
                    bool wasInstalled = _installed;
                    _lastFailure = "candidate-stale:" + staleReason;
                    _log.Info("[KBP-BOOT] HUD candidate stale;reason=" + staleReason +
                        ";candidate=" + staleCandidate + ";candidateHud=" + _candidateHudIdentity +
                        ";currentHud=" + (hudHost == null ? 0 : hudHost.GetInstanceID()) +
                        ";anchor=" + staleAnchor + ";installed=" + wasInstalled + ".");
                    DisposeOwnedRoot();
                    _lastCandidateTickResult = HudCandidateTickResult.Stale;
                    return _lastCandidateTickResult;
                }
                if (!_installed)
                {
                    if (!_readiness.ObserveFrame())
                    {
                        _lastCandidateTickResult = HudCandidateTickResult.Pending;
                        return _lastCandidateTickResult;
                    }
                    Canvas.ForceUpdateCanvases();
                    string aboveFailure;
                    string alignmentFailure;
                    string glyphFailure;
                    string hitFailure;
                    string frameFailure;
                    RowAboveNativeCluster = ValidateRowAboveCluster(out aboveFailure);
                    RowLeftAlignedWithNativeCluster = ValidateRowLeftAlignment(out alignmentFailure);
                    GlyphsCentered = ValidateGlyphCentering(out glyphFailure);
                    VisibleHitboxesOwnRaycasts = ValidateHitOwnership(out hitFailure);
                    FrameEnclosesRow = ValidateEnclosure(out frameFailure);
                    if (!RowAboveNativeCluster || !RowLeftAlignedWithNativeCluster ||
                        !GlyphsCentered || !VisibleHitboxesOwnRaycasts || !FrameEnclosesRow)
                    {
                        HudCandidateTickResult validationResult =
                            _candidateValidation.RecordValidation(false);
                        _lastValidationFailure = !RowAboveNativeCluster ? aboveFailure :
                            !RowLeftAlignedWithNativeCluster ? alignmentFailure :
                            !GlyphsCentered ? glyphFailure :
                            !VisibleHitboxesOwnRaycasts ? hitFailure : frameFailure;
                        LogFailure(_lastValidationFailure);
                        if (validationResult == HudCandidateTickResult.Expired)
                        {
                            int expiredCandidate = RootInstanceId;
                            string expiredFailure = _lastValidationFailure;
                            _log.Info("[KBP-BOOT] HUD candidate expired;candidate=" +
                                expiredCandidate + ";hud=" + _candidateHudIdentity +
                                ";anchor=" + AnchorInstanceId + ";validationFrames=" +
                                _candidateValidation.FailureFrames + ";lastValidationFailure=" +
                                expiredFailure + ".");
                            DisposeOwnedRoot();
                            _lastFailure = "candidate-expired:" + expiredFailure;
                            _lastCandidateTickResult = HudCandidateTickResult.Expired;
                            return _lastCandidateTickResult;
                        }
                        _lastCandidateTickResult = validationResult;
                        return _lastCandidateTickResult;
                    }
                    _candidateValidation.RecordValidation(true);
                    _installed = true;
                    _lastFailure = string.Empty;
                    _lastValidationFailure = string.Empty;
                    foreach (Button button in _buttons)
                        PlannerPointerOwnership.Register((RectTransform)button.transform);
                    _diagnostics.RecordHudInstalled();
                    RefreshAvailability();
                    _lastCandidateTickResult = HudCandidateTickResult.Installed;
                    _log.Info("[KBP-BOOT] HUD candidate installed;attempt=" + _installAttempts +
                        ";candidateCreate=" + _candidateCreateCount + ";candidate=" + RootInstanceId +
                        ";hud=" + _candidateHudIdentity + ";anchor=" + AnchorInstanceId +
                        ";host=" + AnchorPath + ";buttons=" + ButtonCount +
                        ";listeners=" + ListenerCount + ";active=" +
                        _root.gameObject.activeInHierarchy + ".");
                }
            }
            RefreshAvailability();
            return _lastCandidateTickResult;
        }

        public void Dispose()
        {
            DisposeOwnedRoot();
        }

        internal HudCandidateTickResult RecoverFromFault(string reason)
        {
            int candidate = RootInstanceId;
            bool wasInstalled = _installed;
            DisposeOwnedRoot();
            _lastFailure = string.IsNullOrWhiteSpace(reason) ? "hud-controller-fault" : reason;
            _lastCandidateTickResult = HudCandidateTickResult.Stale;
            _log.Info("[KBP-BOOT] HUD owned UI disposed after fault;reason=" + _lastFailure +
                ";candidate=" + candidate + ";installed=" + wasInstalled +
                ";retryable=true.");
            return _lastCandidateTickResult;
        }

        private void DisposeOwnedRoot()
        {
            EndRuntimePhysicalObservation();
            if (_root != null)
            {
                foreach (Button button in _buttons)
                    if (button != null) PlannerPointerOwnership.Unregister(
                        (RectTransform)button.transform);
                UnityEngine.Object.Destroy(_root.gameObject);
                _diagnostics.RecordHudDestroyed();
            }
            foreach (Sprite sprite in _ownedSprites)
                if (sprite != null) UnityEngine.Object.Destroy(sprite);
            foreach (Texture2D texture in _ownedTextures)
                if (texture != null) UnityEngine.Object.Destroy(texture);
            _ownedSprites.Clear();
            _ownedTextures.Clear();
            _root = null;
            _candidateOwned = false;
            _installed = false;
            _readiness.Reset();
            _candidateValidation.Reset();
            _nativeTooltips = new TooltipTrigger[0];
            _nativeTooltipActive = false;
            _buttons = new Button[0];
            _tooltips = new Func<string>[0];
            _listenerCount = 0;
            _anchorController = null;
            _nativeCluster = null;
            _nativeRaycaster = null;
            _nativeStyle = null;
            _step = null;
            _nativeFrame = null;
            FrameEnclosesRow = false;
            for (int index = 0; index < _cellArtEvidence.Length; index++)
            {
                _cellArtEvidence[index] = null;
                _inkOffsets[index] = Vector2.zero;
            }
            _candidateHudIdentity = 0;
            AnchorPath = string.Empty;
            RaycastCanvasPath = string.Empty;
            RowAboveNativeCluster = false;
            RowLeftAlignedWithNativeCluster = false;
            GlyphsCentered = false;
            VisibleHitboxesOwnRaycasts = false;
        }

        internal bool DispatchRuntimeClick(string routineId)
        {
            int index = routineId == "long" ? 1 : routineId == "important" ? 2 :
                routineId == "short" ? 3 : -1;
            if (index < 0 || index >= _buttons.Length || _buttons[index] == null ||
                EventSystem.current == null) return false;
            Button plannerButton = _buttons[index];
            Vector2 center = ScreenCenter((RectTransform)plannerButton.transform);
            var raycast = new PointerEventData(EventSystem.current) { position = center };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(raycast, hits);
            if (hits.Count == 0 || hits[0].gameObject == null) return false;
            Transform top = hits[0].gameObject.transform;
            if (top != plannerButton.transform && !top.IsChildOf(plannerButton.transform)) return false;
            var nativeListeners = new List<Tuple<Button, UnityAction>>();
            foreach (Button native in _nativeCluster.GetComponentsInChildren<Button>(true))
            {
                if (native == null || native.transform == plannerButton.transform ||
                    native.transform.IsChildOf(_root)) continue;
                UnityAction listener = () => RuntimeUnderlyingNativeActivationCount++;
                native.onClick.AddListener(listener);
                nativeListeners.Add(Tuple.Create(native, listener));
            }
            try
            {
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,
                    Pointer(center), ExecuteEvents.pointerEnterHandler);
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,
                    Pointer(center), ExecuteEvents.pointerDownHandler);
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,
                    Pointer(center), ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,
                    Pointer(center), ExecuteEvents.pointerClickHandler);
            }
            finally
            {
                foreach (Tuple<Button, UnityAction> item in nativeListeners)
                    if (item.Item1 != null) item.Item1.onClick.RemoveListener(item.Item2);
            }
            return true;
        }

        internal void BeginRuntimePhysicalObservation()
        {
            EndRuntimePhysicalObservation();
            if (_nativeCluster == null) return;
            foreach (Button native in _nativeCluster.GetComponentsInChildren<Button>(true))
            {
                if (native == null || native.transform.IsChildOf(_root)) continue;
                UnityAction listener = () => RuntimeUnderlyingNativeActivationCount++;
                native.onClick.AddListener(listener);
                _runtimeNativeListeners.Add(Tuple.Create(native, listener));
            }
        }

        internal void EndRuntimePhysicalObservation()
        {
            foreach (Tuple<Button, UnityAction> item in _runtimeNativeListeners)
                if (item.Item1 != null) item.Item1.onClick.RemoveListener(item.Item2);
            _runtimeNativeListeners.Clear();
        }

        private static PointerEventData Pointer(Vector2 position)
        {
            return new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = position
            };
        }

        internal string TooltipForRuntime(string routineId)
        {
            int index = routineId == "setup" ? 0 : routineId == "long" ? 1 :
                routineId == "important" ? 2 : routineId == "short" ? 3 : -1;
            return index < 0 || index >= _tooltips.Length || _tooltips[index] == null
                ? string.Empty : TooltipTitle(routineId) + "\n" + _tooltips[index]();
        }

        // Runtime evidence only: an owned cell ("setup", "long", "important",
        // "short") and the native panel the cells extend.
        internal Button ButtonForRuntime(string routineId)
        {
            int index = routineId == "setup" ? 0 : routineId == "long" ? 1 :
                routineId == "important" ? 2 : routineId == "short" ? 3 : -1;
            return index < 0 || index >= _buttons.Length ? null : _buttons[index];
        }

        internal RectTransform NativeClusterForRuntime { get { return _nativeCluster; } }
        internal RectTransform RootForRuntime { get { return _root; } }

        // What frames the owned row and what its cells are drawn with.
        internal string FrameEvidenceForRuntime
        {
            get
            {
                if (_step == null || _root == null) return "frame=missing;encloses=False;railUnderRow=native";
                var pieces = new List<string>();
                foreach (Transform child in _root)
                    if (child.name.StartsWith("KBP.Frame.", StringComparison.Ordinal))
                        pieces.Add(child.name.Substring("KBP.Frame.".Length) + "@" +
                            PanelRect((RectTransform)child) + ":" + child.gameObject.activeInHierarchy);
                return "frame=stepped-native;encloses=" + FrameEnclosesRow + ";railUnderRow=" +
                    (FrameEnclosesRow ? "hidden" : "native") + ";raisedTop=" +
                    _step.RaisedTop.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) +
                    ";pieces=" + string.Join("|", pieces.ToArray());
            }
        }

        internal string SkinEvidenceForRuntime
        {
            get
            {
                return _nativeStyle == null ? "skin=missing" : "skin=native-cell-art;face=" +
                    (_nativeStyle.BaseSprite == null ? "missing" : _nativeStyle.BaseSprite.name) +
                    ";highlight=" + (_nativeStyle.TargetSprite == null ? "missing" : _nativeStyle.TargetSprite.name) +
                    ";frame=" + (_nativeFrame == null || _nativeFrame.sprite == null ? "missing" : _nativeFrame.sprite.name) +
                    ";" + _nativeStyle.Evidence;
            }
        }

        internal Vector2 ButtonCenterForRuntime(string routineId)
        {
            int index = routineId == "setup" ? 0 : routineId == "long" ? 1 :
                routineId == "important" ? 2 : routineId == "short" ? 3 : -1;
            if (index < 0 || index >= _buttons.Length || _buttons[index] == null)
                throw new InvalidOperationException("HUD button is unavailable: " + routineId);
            return ScreenCenter((RectTransform)_buttons[index].transform);
        }

        internal HudTooltipRuntimeDiagnostics GetTooltipDiagnostics()
        {
            int count = _nativeTooltips.Count(trigger => trigger != null);
            return new HudTooltipRuntimeDiagnostics
            {
                Active = _nativeTooltipActive,
                InsideScreen = count == 4,
                Bounds = count == 0 ? "absent" : "native-tooltip-service",
                ListenerCount = count,
                RaycastGraphicCount = 0,
                BlocksRaycasts = false,
                NativeTriggerCount = count,
                UsesNativeParchmentPresentation = count == 4
            };
        }

        internal string PhysicalHoverSnapshotForRuntime(string routineId)
        {
            Vector2 expected = ButtonCenterForRuntime(routineId);
            Vector2 actual = Input.mousePosition;
            int index = routineId == "setup" ? 0 : routineId == "long" ? 1 :
                routineId == "important" ? 2 : routineId == "short" ? 3 : -1;
            RectTransform region = index < 0 || index >= _buttons.Length ||
                _buttons[index] == null ? null : (RectTransform)_buttons[index].transform;
            bool contains = region != null && RectTransformUtility.RectangleContainsScreenPoint(
                region, actual, _nativeRaycaster == null ? null : _nativeRaycaster.eventCamera);
            string top = "none";
            if (EventSystem.current != null)
            {
                var data = new PointerEventData(EventSystem.current) { position = actual };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(data, hits);
                if (hits.Count != 0 && hits[0].gameObject != null)
                    top = GetPath(hits[0].gameObject.transform);
            }
            return "expected=" + expected + ";actual=" + actual + ";contains=" + contains +
                ";plannerContains=" + PlannerPointerOwnership.Contains(actual) +
                ";top=" + top + ";tooltip=" +
                _nativeTooltipActive;
        }

        private static string TooltipTitle(string routineId)
        {
            if (routineId == "setup") return "Buff Planner";
            if (routineId == "long") return "Long Buffs";
            if (routineId == "important") return "Important Buffs";
            return "Short Buffs";
        }

        private void RefreshNativeTooltipText()
        {
            for (int index = 0; index < _nativeTooltips.Length; index++)
            {
                TooltipTrigger trigger = _nativeTooltips[index];
                if (trigger == null || index >= _tooltips.Length || _tooltips[index] == null)
                    continue;
                string routineId = index == 0 ? "setup" : index == 1 ? "long" :
                    index == 2 ? "important" : "short";
                trigger.SetNameAndDescription(TooltipTitle(routineId), _tooltips[index]());
            }
        }

        // v1.2 §5: the Setup (gear) button is the HUD's clearly labelled
        // editor entry - a click opens the planner. The owner's "moon" is
        // the Long button (its glyph is the crescent), whose click runs
        // Long with no editor detour.
        private string SetupTooltip()
        {
            return _castingFirst != null && _castingFirst()
                ? "Open the Buff Planner. Shortcut: " + PlannerHotkey.Binding +
                    ". The moon button runs your Long routine with one click."
                : "Open Buff Planner setup. Shortcut: " + PlannerHotkey.Binding + ".";
        }

        private Button CreatePlannerButton(
            string displayName,
            string iconKind,
            float width,
            float height,
            Action action,
            Func<string> tooltip)
        {
            // The native cell's own construction (Formation, kbpnui-facts-03):
            // the idle art on the button itself, which takes the hits, and
            // the hover art in a Highlight child the native colour tint
            // fades in (hover), dims (pressed) and hides (idle, disabled).
            // Both are the native cell art with this cell's glyph engraved.
            int index = Array.IndexOf(CellKinds, iconKind);
            RectTransform rect = KingmakerUiFactory.CreateRect("KBP." + displayName, _root);
            rect.sizeDelta = new Vector2(width, height);
            Image face = rect.gameObject.AddComponent<Image>();
            face.sprite = CreateIcon(iconKind, _nativeStyle.BaseSprite, "Normal", index);
            face.type = Image.Type.Simple;
            face.material = _nativeStyle.BaseMaterial;
            face.color = _nativeStyle.BaseColor;
            face.raycastTarget = true;
            RectTransform highlightRect = KingmakerUiFactory.CreateRect("Highlight", rect);
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;
            Image highlight = highlightRect.gameObject.AddComponent<Image>();
            ButtonPF button = rect.gameObject.AddComponent<ButtonPF>();
            button.targetGraphic = highlight;
            _nativeStyle.Apply(button, highlight);
            highlight.sprite = CreateIcon(iconKind, _nativeStyle.TargetSprite, "Hover", index);
            highlight.raycastTarget = false;

            button.onClick.AddListener(() => action());
            _listenerCount++;
            PlannerPointerSink sink = button.gameObject.AddComponent<PlannerPointerSink>();
            sink.Diagnostics = _diagnostics;
            sink.RoutineId = iconKind == "setup" ? string.Empty : iconKind;
            sink.HoverChanged = active => _nativeTooltipActive = active;
            TooltipTrigger nativeTooltip = button.gameObject.AddComponent<TooltipTrigger>();
            nativeTooltip.SetNameAndDescription(TooltipTitle(iconKind),
                tooltip == null ? string.Empty : tooltip());
            return button;
        }

        // The native frame image of the panel (exact 2.1.7b contract: the
        // panel's "Background", a tiled 9-slice of TBM_backButton).
        private static Image NativeFrame(RectTransform panel)
        {
            Transform background = panel.Find("Background");
            Image image = background == null ? null : background.GetComponent<Image>();
            return image == null || image.sprite == null || image.sprite.texture == null ||
                !image.gameObject.activeInHierarchy ? null : image;
        }

        // The native grid's square cells (the panel's own ButtonPF children
        // the grid lays out), in panel units from its bottom-left corner.
        private List<HudRect> NativeGridCells(RectTransform panel)
        {
            var cells = new List<HudRect>();
            foreach (Transform child in panel)
            {
                if (child == null || child.name == RootName || !child.gameObject.activeInHierarchy ||
                    child.GetComponent<ButtonPF>() == null) continue;
                LayoutElement element = child.GetComponent<LayoutElement>();
                if (element != null && element.ignoreLayout) continue;
                HudRect rect = PanelRect(child as RectTransform, panel);
                if (Mathf.Abs(rect.Width - rect.Height) < 1f) cells.Add(rect);
            }
            return cells;
        }

        private HudStepLayout MeasureStep(RectTransform panel, Image nativeFrame, RectTransform reference,
            out string failure)
        {
            failure = string.Empty;
            List<HudRect> cells = NativeGridCells(panel);
            float[] lefts = cells.Select(cell => (float)Math.Round(cell.X, 1)).Distinct().OrderBy(value => value)
                .ToArray();
            float[] bottoms = cells.Select(cell => (float)Math.Round(cell.Y, 1)).Distinct().OrderBy(value => value)
                .ToArray();
            if (lefts.Length < 5 || bottoms.Length != 2)
            {
                failure = "native-grid:columns=" + lefts.Length + ";rows=" + bottoms.Length;
                return null;
            }
            HudRect frame = PanelRect(nativeFrame.rectTransform, panel);
            float unitsPerPixel = nativeFrame.pixelsPerUnit <= 0f ? 0f : 1f / nativeFrame.pixelsPerUnit;
            HudStepLayout layout;
            try
            {
                layout = HudStepLayout.Compute(lefts, reference.rect.width, reference.rect.height, bottoms[1],
                    bottoms[1] - bottoms[0], frame.Top, unitsPerPixel);
            }
            catch (Exception exception)
            {
                failure = "native-metrics:" + exception.Message;
                return null;
            }
            failure = layout.Violation(lefts, reference.rect.width, bottoms[1]);
            if (failure != null) return null;
            failure = string.Empty;
            return layout;
        }

        private void CreateFramePiece(HudFramePiece piece, Image nativeFrame)
        {
            RectTransform rect = KingmakerUiFactory.CreateRect("KBP.Frame." + piece.Name, _root);
            PlaceInStep(rect, piece.Target);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = NativeHudArt.CreatePiece(nativeFrame.sprite, piece, _ownedSprites);
            image.type = Image.Type.Simple;
            image.material = nativeFrame.material;
            image.color = nativeFrame.color;
            image.raycastTarget = false;
        }

        private void PlaceInStep(RectTransform rect, HudRect target)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(target.X - _stepOrigin.x, target.Y - _stepOrigin.y);
            rect.sizeDelta = new Vector2(target.Width, target.Height);
        }

        // A rect in panel units from the panel's bottom-left corner.
        private HudRect PanelRect(RectTransform rect, RectTransform panel = null)
        {
            panel = panel ?? _nativeCluster;
            if (rect == null || panel == null) return new HudRect(0f, 0f, 0f, 0f);
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 min = panel.InverseTransformPoint(corners[0]);
            Vector3 max = panel.InverseTransformPoint(corners[2]);
            Rect space = panel.rect;
            return new HudRect(min.x - space.xMin, min.y - space.yMin, max.x - min.x, max.y - min.y);
        }

        // The owned row stands above the native top row, one native gap
        // clear of it.
        private bool ValidateRowAboveCluster(out string failure)
        {
            failure = string.Empty;
            if (_root == null || _nativeCluster == null || _buttons.Length != 4)
            {
                failure = "row-or-native-cluster-null";
                return false;
            }
            List<HudRect> native = NativeGridCells(_nativeCluster);
            if (native.Count == 0)
            {
                failure = "native-grid-empty";
                return false;
            }
            float nativeTop = native.Max(cell => cell.Top);
            float rowBottom = _buttons.Min(button => PanelRect((RectTransform)button.transform).Y);
            bool valid = rowBottom >= nativeTop + 0.5f;
            if (!valid) failure = "row-not-above-native-row:rowBottom=" + rowBottom + ";nativeTop=" + nativeTop;
            return valid;
        }

        // The stepped enclosure stands: the measured geometry holds and
        // every frame piece is shown.
        private bool ValidateEnclosure(out string failure)
        {
            failure = string.Empty;
            if (_step == null || _root == null || _nativeCluster == null)
            {
                failure = "enclosure-missing";
                return false;
            }
            List<HudRect> native = NativeGridCells(_nativeCluster);
            float[] lefts = native.Select(cell => (float)Math.Round(cell.X, 1)).Distinct().OrderBy(value => value)
                .ToArray();
            string violation = lefts.Length < 5 || native.Count == 0 ? "native-grid-changed"
                : _step.Violation(lefts, _step.Cells[0].Width, native.Max(cell => cell.Y));
            int pieces = 0;
            foreach (Transform child in _root)
                if (child.name.StartsWith("KBP.Frame.", StringComparison.Ordinal) &&
                    child.gameObject.activeInHierarchy && child.GetComponent<Image>() != null &&
                    child.GetComponent<Image>().sprite != null) pieces++;
            if (violation == null && pieces < 3) violation = "frame-pieces:" + pieces;
            if (violation != null) failure = "enclosure:" + violation;
            return violation == null;
        }

        private bool ValidateHitOwnership(out string failure)
        {
            failure = string.Empty;
            if (_buttons.Length != 4 || EventSystem.current == null || _nativeRaycaster == null)
            {
                failure = "hit-prerequisite-missing:buttons=" + _buttons.Length +
                    ";eventSystem=" + (EventSystem.current == null ? "null" : EventSystem.current.name) +
                    ";raycaster=" + (_nativeRaycaster == null ? "null" : GetPath(_nativeRaycaster.transform));
                return false;
            }
            for (int index = 0; index < _buttons.Length; index++)
            {
                Button button = _buttons[index];
                Image face = button == null ? null : button.GetComponent<Image>();
                if (button == null || !button.gameObject.activeInHierarchy ||
                    face == null || !face.raycastTarget)
                {
                    failure = "button-not-raycast-ready:index=" + index;
                    return false;
                }
                var eventData = new PointerEventData(EventSystem.current)
                {
                    position = ScreenCenter((RectTransform)button.transform)
                };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, hits);
                if (hits.Count == 0 || hits[0].gameObject == null)
                {
                    failure = "button-raycast-empty:index=" + index + ";center=" + eventData.position;
                    return false;
                }
                Transform hit = hits[0].gameObject.transform;
                if (hit != button.transform && !hit.IsChildOf(button.transform))
                {
                    failure = "button-raycast-not-owned:index=" + index + ";top=" + GetPath(hit);
                    return false;
                }
            }
            return true;
        }

        // Each owned cell stands on its native column.
        private bool ValidateRowLeftAlignment(out string failure)
        {
            failure = string.Empty;
            if (_root == null || _nativeCluster == null || _buttons.Length != 4) return false;
            float[] lefts = NativeGridCells(_nativeCluster).Select(cell => (float)Math.Round(cell.X, 1))
                .Distinct().OrderBy(value => value).ToArray();
            if (lefts.Length < 4)
            {
                failure = "native-columns:" + lefts.Length;
                return false;
            }
            for (int index = 0; index < 4; index++)
            {
                float left = PanelRect((RectTransform)_buttons[index].transform).X;
                if (Mathf.Abs(left - lefts[index]) > 1.1f)
                {
                    failure = "cell-off-column:index=" + index + ";cell=" + left + ";native=" + lefts[index];
                    return false;
                }
            }
            return true;
        }

        // Each cell's glyph was engraved centred on its ink (HudCellArt)
        // into both the idle and the hover art.
        private bool ValidateGlyphCentering(out string failure)
        {
            failure = string.Empty;
            for (int index = 0; index < _buttons.Length; index++)
            {
                Button button = _buttons[index];
                Image face = button == null ? null : button.GetComponent<Image>();
                Image highlight = button == null ? null : button.targetGraphic as Image;
                if (face == null || face.sprite == null || highlight == null || highlight == face ||
                    highlight.sprite == null || _cellArtEvidence[index] == null ||
                    _inkOffsets[index].magnitude > 1f)
                {
                    failure = "glyph-not-centered:" + (button == null ? "missing" : button.name) +
                        ";inkOffset=" + _inkOffsets[index];
                    return false;
                }
            }
            return true;
        }

        private Vector2 ScreenCenter(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
            Camera eventCamera = _nativeRaycaster == null ? null : _nativeRaycaster.eventCamera;
            return RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter);
        }

        private HudInstallAttemptResult RejectHost(
            string reason,
            HudInstallAttemptResult result)
        {
            LogFailure(reason);
            _lastAttemptResult = result;
            return result;
        }

        private bool IsHostingChainViable(
            UISectionHUDController hudHost,
            out string failure)
        {
            return HudHostingChainValidator.IsViable(
                CaptureHostingChain(hudHost), out failure);
        }

        private HudHostingChainSnapshot CaptureHostingChain(UISectionHUDController hudHost)
        {
            bool rootExists = _candidateOwned && _root != null;
            bool anchorExists = _anchorController != null;
            bool clusterExists = _nativeCluster != null;
            bool hudExists = hudHost != null;
            return new HudHostingChainSnapshot(
                rootExists,
                rootExists && _root.parent != null,
                rootExists && _root.gameObject.activeInHierarchy,
                anchorExists,
                anchorExists && _anchorController.gameObject.activeInHierarchy,
                clusterExists,
                clusterExists && _nativeCluster.gameObject.activeInHierarchy,
                hudExists,
                hudExists && hudHost.gameObject.activeInHierarchy,
                rootExists && clusterExists && _root.parent == _nativeCluster,
                anchorExists && hudExists &&
                    _anchorController.transform.IsChildOf(hudHost.transform),
                clusterExists && hudExists && _nativeCluster.IsChildOf(hudHost.transform),
                rootExists && hudExists && _root.IsChildOf(hudHost.transform),
                _nativeRaycaster != null && _nativeRaycaster.isActiveAndEnabled);
        }

        private static UISectionHUDController CurrentHudHost()
        {
            StaticCanvas canvas = StaticCanvas.Instance;
            return canvas == null ? null : canvas.HUDController;
        }

        private void LogFailure(string reason)
        {
            reason = string.IsNullOrEmpty(reason) ? "unknown" : reason;
            int validationFrame = _candidateValidation.FailureFrames;
            if (validationFrame > 0)
            {
                bool milestone = validationFrame == 1 || validationFrame == 30 ||
                    validationFrame == 60 || validationFrame == 90 ||
                    validationFrame == _candidateValidation.MaximumFailureFrames;
                if (!milestone)
                {
                    _lastFailure = reason;
                    return;
                }
            }
            else if (string.Equals(_lastFailure, reason, StringComparison.Ordinal)) return;
            _lastFailure = reason;
            _log.Info("[KBP-BOOT] HUD install failed;reason=" + reason +
                ";retryable=true;attempt=" + _installAttempts +
                ";candidate=" + RootInstanceId + ".");
        }

        private string RoutineTooltip(string routineId)
        {
            if (_routineTooltipOverride != null)
            {
                string casting = null;
                try { casting = _routineTooltipOverride(routineId); }
                catch (Exception exception)
                {
                    _log.Error("[KBP-HUD] casting-first tooltip failed.", exception);
                }
                if (casting != null) return casting;
            }
            string name = char.ToUpperInvariant(routineId[0]) + routineId.Substring(1);
            if (_session.Model == null) return "Load a campaign to run " + name + ".";
            if (_session.IsExecuting)
                return _session.ClassicRunHeld
                    ? "A buff routine is waiting to cast: it continues once the game runs and no planner window is open."
                    : "A buff routine is already executing.";
            RoutineProfile routine = _session.Model.Profile.Routines
                .FirstOrDefault(item => item.RoutineId == routineId);
            if (routine == null || routine.Assignments.Count == 0)
                return "No " + name + " buffs are configured.";
            int targets = routine.Assignments.Sum(item => item.WantedTargetUnitIds.Count);
            return "Execute " + name + ": " + routine.Assignments.Count +
                " buffs, " + targets + " requested targets, " +
                _session.Model.Profile.Execution.Mode + " mode.";
        }

        // The owned glyph as a neutral alpha mask (64 x 64, white ink),
        // engraved into the native cell art (HudCellArt): the idle art for
        // "Normal", the hover art for "Hover". Composed once per native
        // art and glyph.
        private Sprite CreateIcon(string kind, Sprite nativeCell, string state, int index)
        {
            const int size = 64;
            string cellName = "KingmakerBuffPlanner." + kind + ".Cell";
            Color clear = new Color(0, 0, 0, 0);
            Color ink = Color.white;
            Color[] pixels = Enumerable.Repeat(clear, size * size).ToArray();
            Action<int, int> set = (x, y) =>
            {
                if (x >= 0 && x < size && y >= 0 && y < size) pixels[y * size + x] = ink;
            };
            if (kind == "setup")
            {
                DrawRing(set, 32, 32, 19, 4);
                for (int arm = 0; arm < 8; arm++)
                {
                    double angle = arm * Math.PI / 4.0;
                    DrawDisc(set, 32 + (int)(Math.Cos(angle) * 25),
                        32 + (int)(Math.Sin(angle) * 25), 4);
                }
                DrawDisc(set, 32, 32, 6);
            }
            else if (kind == "long")
            {
                DrawDisc(set, 31, 32, 22);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        if ((x - 41) * (x - 41) + (y - 39) * (y - 39) <= 20 * 20)
                            pixels[y * size + x] = clear;
            }
            else if (kind == "important")
            {
                for (int y = 7; y <= 57; y++)
                {
                    int half = y <= 32 ? (y - 7) / 2 : (57 - y) / 2;
                    for (int x = 32 - half; x <= 32 + half; x++) set(x, y);
                }
                DrawDisc(set, 32, 32, 5);
            }
            else
            {
                DrawDisc(set, 32, 32, 13);
                for (int ray = 0; ray < 8; ray++)
                {
                    double angle = ray * Math.PI / 4.0;
                    for (int distance = 19; distance <= 27; distance++)
                        DrawDisc(set, 32 + (int)(Math.Cos(angle) * distance),
                            32 + (int)(Math.Sin(angle) * distance), 2);
                }
            }
            if (nativeCell == null || nativeCell.texture == null)
                throw new InvalidOperationException("native-cell-art-missing:" + state);
            string key = nativeCell.GetInstanceID() + ":" + kind;
            HudCellArtResult art;
            if (!CellArtCache.TryGetValue(key, out art))
            {
                HudPixels native;
                if (!NativeCellPixels.TryGetValue(nativeCell.GetInstanceID(), out native))
                {
                    native = NativeHudArt.Read(nativeCell);
                    NativeCellPixels[nativeCell.GetInstanceID()] = native;
                }
                art = HudCellArt.Compose(native, pixels.Select(pixel => pixel.a).ToArray(), size);
                CellArtCache[key] = art;
            }
            if (state == "Normal" && index >= 0 && index < _cellArtEvidence.Length)
            {
                _cellArtEvidence[index] = "native-composed:" + nativeCell.name + ";" + art.Evidence;
                _inkOffsets[index] = new Vector2(art.InkOffsetX, art.InkOffsetY);
            }
            return NativeHudArt.CreateSprite(art.Pixels, cellName + "." + state, nativeCell.pixelsPerUnit,
                _ownedTextures, _ownedSprites);
        }

        private static void DrawDisc(Action<int, int> set, int centerX, int centerY, int radius)
        {
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                    if (x * x + y * y <= radius * radius) set(centerX + x, centerY + y);
        }

        private static void DrawRing(Action<int, int> set, int centerX, int centerY,
            int radius, int thickness)
        {
            int outer = radius * radius;
            int inner = (radius - thickness) * (radius - thickness);
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                {
                    int distance = x * x + y * y;
                    if (distance <= outer && distance >= inner) set(centerX + x, centerY + y);
                }
        }

        private static ButtonPF ResolveFormationButton(IngameMenuController controller)
        {
            FieldInfo field = typeof(IngameMenuController).GetField("m_FormationButton",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field == null ? null : field.GetValue(controller) as ButtonPF;
        }

        private static string GetPath(Transform transform)
        {
            var names = new List<string>();
            while (transform != null)
            {
                names.Add(transform.name);
                transform = transform.parent;
            }
            names.Reverse();
            return string.Join("/", names.ToArray());
        }
    }

    internal sealed class NativeHudButtonStyle
    {
        private NativeHudButtonStyle(
            Sprite targetSprite,
            Image.Type targetType,
            Material targetMaterial,
            Color targetColor,
            Selectable.Transition transition,
            ColorBlock colors,
            SpriteState sprites,
            Navigation navigation,
            bool clickSoundActive,
            bool hoverSoundActive,
            Material iconMaterial,
            Color iconColor,
            Sprite baseSprite)
        {
            BaseSprite = baseSprite;
            TargetSprite = targetSprite;
            TargetType = targetType;
            TargetMaterial = targetMaterial;
            TargetColor = targetColor;
            Transition = transition;
            Colors = colors;
            Sprites = sprites;
            Navigation = navigation;
            ClickSoundActive = clickSoundActive;
            HoverSoundActive = hoverSoundActive;
            IconMaterial = iconMaterial;
            IconColor = iconColor;
        }

        internal Sprite TargetSprite { get; private set; }
        internal Image.Type TargetType { get; private set; }
        internal Material TargetMaterial { get; private set; }
        internal Color TargetColor { get; private set; }
        internal Selectable.Transition Transition { get; private set; }
        internal ColorBlock Colors { get; private set; }
        internal SpriteState Sprites { get; private set; }
        internal Navigation Navigation { get; private set; }
        internal bool ClickSoundActive { get; private set; }
        internal bool HoverSoundActive { get; private set; }
        internal Material IconMaterial { get; private set; }
        internal Color IconColor { get; private set; }
        // The native cell's idle art (the button's own image); IconMaterial
        // and IconColor are that image's.
        internal Sprite BaseSprite { get; private set; }
        internal Material BaseMaterial { get { return IconMaterial; } }
        internal Color BaseColor { get { return IconColor; } }
        internal bool IsComplete { get { return TargetSprite != null && BaseSprite != null; } }

        internal string Evidence
        {
            get
            {
                return "targetSprite=" + (TargetSprite != null) +
                    ";targetType=" + TargetType +
                    ";targetMaterial=" + (TargetMaterial == null ? "default" : TargetMaterial.name) +
                    ";targetColor=" + ColorEvidence(TargetColor) +
                    ";transition=" + Transition +
                    ";normal=" + ColorEvidence(Colors.normalColor) +
                    ";highlighted=" + ColorEvidence(Colors.highlightedColor) +
                    ";pressed=" + ColorEvidence(Colors.pressedColor) +
                    ";disabled=" + ColorEvidence(Colors.disabledColor) +
                    ";multiplier=" + Colors.colorMultiplier.ToString("0.000") +
                    ";fade=" + Colors.fadeDuration.ToString("0.000") +
                    ";spriteHighlighted=" + (Sprites.highlightedSprite != null) +
                    ";spritePressed=" + (Sprites.pressedSprite != null) +
                    ";spriteDisabled=" + (Sprites.disabledSprite != null) +
                    ";navigation=" + Navigation.mode +
                    ";clickSound=" + ClickSoundActive +
                    ";hoverSound=" + HoverSoundActive +
                    ";iconMaterial=" + (IconMaterial == null ? "default" : IconMaterial.name) +
                    ";iconColor=" + ColorEvidence(IconColor) +
                    ";baseSprite=" + (BaseSprite == null ? "missing" : BaseSprite.name);
            }
        }

        internal static NativeHudButtonStyle Capture(ButtonPF nativeButton)
        {
            Image target = nativeButton == null ? null : nativeButton.targetGraphic as Image;
            if (target == null) return null;
            Image icon = nativeButton.GetComponent<Image>();
            if (icon == target || icon == null || icon.sprite == null)
                icon = nativeButton.GetComponentsInChildren<Image>(true)
                    .Where(image => image != null && image != target && image.sprite != null)
                    .FirstOrDefault();
            return new NativeHudButtonStyle(target.sprite, target.type, target.material,
                target.color, nativeButton.transition, nativeButton.colors,
                nativeButton.spriteState, nativeButton.navigation,
                nativeButton.OnClickSoundActive, nativeButton.OnHoverSoundActive,
                icon == null ? null : icon.material,
                icon == null ? Color.white : icon.color,
                icon == null ? null : icon.sprite);
        }

        internal void Apply(ButtonPF ownedButton, Image ownedTarget)
        {
            if (ownedButton == null) throw new ArgumentNullException("ownedButton");
            if (ownedTarget == null) throw new ArgumentNullException("ownedTarget");
            ownedTarget.sprite = TargetSprite;
            ownedTarget.type = TargetType;
            ownedTarget.material = TargetMaterial;
            ownedTarget.color = TargetColor;
            ownedButton.transition = Transition;
            ownedButton.colors = Colors;
            ownedButton.spriteState = Sprites;
            ownedButton.navigation = Navigation;
            ownedButton.OnClickSoundActive = ClickSoundActive;
            ownedButton.OnHoverSoundActive = HoverSoundActive;
        }

        private static string ColorEvidence(Color color)
        {
            return color.r.ToString("0.000") + "," + color.g.ToString("0.000") + "," +
                color.b.ToString("0.000") + "," + color.a.ToString("0.000");
        }
    }

    internal sealed class HudTooltipRuntimeDiagnostics
    {
        internal bool Active;
        internal bool InsideScreen;
        internal string Bounds;
        internal int ListenerCount;
        internal int RaycastGraphicCount;
        internal bool BlocksRaycasts;
        internal int NativeTriggerCount;
        internal bool UsesNativeParchmentPresentation;
    }
}
