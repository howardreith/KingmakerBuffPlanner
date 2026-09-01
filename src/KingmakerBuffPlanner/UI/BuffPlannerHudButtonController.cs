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

        internal BuffPlannerHudButtonController(
            PlannerUiSession session,
            BuffPlannerUiLifecycleDiagnostics diagnostics,
            ModLog log,
            Action openSetup,
            Action<string> quickExecute)
        {
            _session = session ?? throw new ArgumentNullException("session");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
            _log = log ?? throw new ArgumentNullException("log");
            _openSetup = openSetup ?? throw new ArgumentNullException("openSetup");
            _quickExecute = quickExecute ?? throw new ArgumentNullException("quickExecute");
        }

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
                    entries.Add(index + ":name=" + button.name +
                        ",id=" + button.gameObject.GetInstanceID() +
                        ",active=" + button.gameObject.activeInHierarchy +
                        ",interactable=" + button.interactable +
                        ",tile=" + ColorEvidence((button.targetGraphic as Image) == null
                            ? Color.clear : ((Image)button.targetGraphic).color) +
                        ",nativeSkin=" + ((button.targetGraphic as Image) != null &&
                            ((Image)button.targetGraphic).sprite != null) +
                        ",transition=" + button.transition +
                        ",iconTint=" + ColorEvidence(button.transform.Find("KBP.Icon") == null
                            ? Color.clear : button.transform.Find("KBP.Icon").GetComponent<Image>().color) +
                        ",spriteInk=" + SpriteInkEvidence(button.transform.Find("KBP.Icon") == null
                            ? null : button.transform.Find("KBP.Icon").GetComponent<Image>()) +
                        ",glyph=" + GlyphEvidence(button) +
                        ",screenCenter=" + ScreenCenter((RectTransform)button.transform) +
                        ",corners=" + string.Join("|", corners.Select(value => value.ToString()).ToArray()));
                }
                return "root=" + RootInstanceId + ";host=" + AnchorPath +
                    ";raycaster=" + RaycastCanvasPath + ";style=" +
                    (_nativeStyle == null ? "missing" : _nativeStyle.Evidence) + ";" +
                    string.Join(";", entries.ToArray());
            }
        }

        private static string ColorEvidence(Color color)
        {
            return color.r.ToString("0.000") + "," + color.g.ToString("0.000") + "," +
                color.b.ToString("0.000") + "," + color.a.ToString("0.000");
        }

        private static string SpriteInkEvidence(Image image)
        {
            if (image == null || image.sprite == null || image.sprite.texture == null)
                return "missing";
            Rect rect = image.sprite.rect;
            Texture2D texture = image.sprite.texture;
            for (int y = (int)rect.yMin; y < (int)rect.yMax; y++)
                for (int x = (int)rect.xMin; x < (int)rect.xMax; x++)
                {
                    Color pixel = texture.GetPixel(x, y);
                    if (pixel.a > 0.5f) return ColorEvidence(pixel);
                }
            return "transparent";
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
            AnchorPath = GetPath(parent);
            RaycastCanvasPath = GetPath(raycaster.transform);
            _root = KingmakerUiFactory.CreateRect(RootName, parent);
            _candidateOwned = true;
            _candidateHudIdentity = hudHost.GetInstanceID();
            _root.anchorMin = new Vector2(0, 1);
            _root.anchorMax = new Vector2(0, 1);
            _root.pivot = new Vector2(0, 0);
            float width = Mathf.Max(42f, reference.rect.width);
            float height = Mathf.Max(42f, reference.rect.height);
            _root.anchoredPosition = new Vector2(NativeGridLeftInset(parent), 8f);
            _root.sizeDelta = new Vector2(width * 4f + 18f, height);
            LayoutElement rootLayout = _root.gameObject.AddComponent<LayoutElement>();
            rootLayout.ignoreLayout = true;
            HorizontalLayoutGroup layout = _root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            _buttons = new[]
            {
                CreatePlannerButton("Setup", "setup", width, height, _openSetup,
                    () => "Open Buff Planner setup. Shortcut: " + PlannerHotkey.Binding + "."),
                CreatePlannerButton("Long", "long", width, height,
                    () => _quickExecute("long"), () => RoutineTooltip("long")),
                CreatePlannerButton("Important", "important", width, height,
                    () => _quickExecute("important"), () => RoutineTooltip("important")),
                CreatePlannerButton("Short", "short", width, height,
                    () => _quickExecute("short"), () => RoutineTooltip("short"))
            };
            _nativeTooltips = _buttons.Select(button => button == null ? null :
                button.GetComponent<TooltipTrigger>()).Where(trigger => trigger != null).ToArray();
            foreach (Button button in _buttons) button.interactable = false;
            _tooltips = new Func<string>[]
            {
                () => "Open Buff Planner setup. Shortcut: " + PlannerHotkey.Binding + ".",
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
            _log.Info("[KBP-BOOT] HUD install attempted;attempt=" + _installAttempts +
                ";candidateCreate=" + _candidateCreateCount + ";candidate=" + RootInstanceId +
                ";hud=" + _candidateHudIdentity + ";anchor=" + AnchorInstanceId +
                ";host=" + AnchorPath +
                ";raycastCanvas=" + RaycastCanvasPath + ";deferredFrames=2.");
            return _lastAttemptResult;
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
                    RowAboveNativeCluster = ValidateRowAboveCluster(out aboveFailure);
                    RowLeftAlignedWithNativeCluster = ValidateRowLeftAlignment(out alignmentFailure);
                    GlyphsCentered = ValidateGlyphCentering(out glyphFailure);
                    VisibleHitboxesOwnRaycasts = ValidateHitOwnership(out hitFailure);
                    if (!RowAboveNativeCluster || !RowLeftAlignedWithNativeCluster ||
                        !GlyphsCentered || !VisibleHitboxesOwnRaycasts)
                    {
                        HudCandidateTickResult validationResult =
                            _candidateValidation.RecordValidation(false);
                        _lastValidationFailure = !RowAboveNativeCluster ? aboveFailure :
                            !RowLeftAlignedWithNativeCluster ? alignmentFailure :
                            !GlyphsCentered ? glyphFailure : hitFailure;
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

        private Button CreatePlannerButton(
            string displayName,
            string iconKind,
            float width,
            float height,
            Action action,
            Func<string> tooltip)
        {
            RectTransform rect = KingmakerUiFactory.CreateRect("KBP." + displayName, _root);
            Image tile = rect.gameObject.AddComponent<Image>();
            ButtonPF button = rect.gameObject.AddComponent<ButtonPF>();
            button.targetGraphic = tile;
            _nativeStyle.Apply(button, tile);
            rect.sizeDelta = new Vector2(width, height);
            LayoutElement element = button.gameObject.GetComponent<LayoutElement>() ??
                button.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.minWidth = width;
            element.minHeight = height;

            button.onClick.AddListener(() => action());
            _listenerCount++;
            PlannerPointerSink sink = button.gameObject.AddComponent<PlannerPointerSink>();
            sink.Diagnostics = _diagnostics;
            sink.RoutineId = iconKind == "setup" ? string.Empty : iconKind;
            sink.HoverChanged = active => _nativeTooltipActive = active;
            TooltipTrigger nativeTooltip = button.gameObject.AddComponent<TooltipTrigger>();
            nativeTooltip.SetNameAndDescription(TooltipTitle(iconKind),
                tooltip == null ? string.Empty : tooltip());

            RectTransform iconRect = KingmakerUiFactory.CreateRect("KBP.Icon", button.transform);
            float safeSize = Mathf.Max(20f, Mathf.Min(width, height) - 16f);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(safeSize, safeSize);
            iconRect.localScale = Vector3.one;
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = CreateIcon(iconKind);
            iconRect.anchoredPosition = HudGlyphLayout.OpticalOffset(icon.sprite, safeSize);
            icon.preserveAspect = true;
            icon.material = _nativeStyle.IconMaterial;
            icon.color = _nativeStyle.IconColor;
            icon.raycastTarget = false;
            return button;
        }

        private static float NativeGridLeftInset(RectTransform nativeCluster)
        {
            if (nativeCluster == null) return 0f;
            bool found = false;
            float left = 0f;
            foreach (Button button in nativeCluster.GetComponentsInChildren<Button>(true))
            {
                if (button == null || button.transform.parent != nativeCluster ||
                    button.transform.name == RootName) continue;
                RectTransform rect = button.transform as RectTransform;
                if (rect == null) continue;
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    float local = nativeCluster.InverseTransformPoint(corner).x;
                    if (!found || local < left) { left = local; found = true; }
                }
            }
            return found ? left : 0f;
        }

        private bool ValidateRowAboveCluster(out string failure)
        {
            failure = string.Empty;
            if (_root == null || _nativeCluster == null)
            {
                failure = "row-or-native-cluster-null";
                return false;
            }
            Vector3[] rootCorners = new Vector3[4];
            Vector3[] clusterCorners = new Vector3[4];
            _root.GetWorldCorners(rootCorners);
            _nativeCluster.GetWorldCorners(clusterCorners);
            float rootBottom = rootCorners.Min(value => value.y);
            float clusterTop = clusterCorners.Max(value => value.y);
            bool valid = rootBottom >= clusterTop + 0.5f;
            if (!valid) failure = "row-not-above-native-cluster:rootBottom=" + rootBottom +
                ";clusterTop=" + clusterTop;
            return valid;
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
                if (button == null || !button.gameObject.activeInHierarchy ||
                    button.targetGraphic == null || !button.targetGraphic.raycastTarget)
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

        private bool ValidateRowLeftAlignment(out string failure)
        {
            failure = string.Empty;
            if (_root == null || _nativeCluster == null) return false;
            var rootCorners = new Vector3[4]; _root.GetWorldCorners(rootCorners);
            float rootLeft = rootCorners.Min(value => value.x);
            float expectedLocal = NativeGridLeftInset(_nativeCluster);
            float expectedWorld = _nativeCluster.TransformPoint(new Vector3(expectedLocal, 0, 0)).x;
            bool aligned = Mathf.Abs(rootLeft - expectedWorld) <= 1.1f;
            if (!aligned) failure = "row-left-misaligned:root=" + rootLeft + ";native=" + expectedWorld;
            return aligned;
        }

        private bool ValidateGlyphCentering(out string failure)
        {
            failure = string.Empty;
            foreach (Button button in _buttons)
            {
                RectTransform icon = button == null ? null :
                    button.transform.Find("KBP.Icon") as RectTransform;
                RectTransform hitbox = button == null ? null : button.transform as RectTransform;
                Image image = icon == null ? null : icon.GetComponent<Image>();
                Vector2 expectedOffset = image == null ? Vector2.zero :
                    HudGlyphLayout.OpticalOffset(image.sprite, icon == null ? 0f : icon.rect.width);
                if (icon == null || hitbox == null || image == null || !image.preserveAspect ||
                    icon.anchorMin != new Vector2(0.5f, 0.5f) ||
                    icon.anchorMax != new Vector2(0.5f, 0.5f) || icon.pivot != new Vector2(0.5f, 0.5f) ||
                    icon.localScale != Vector3.one ||
                    Vector2.Distance(icon.anchoredPosition, expectedOffset) > 0.1f)
                {
                    failure = "glyph-not-centered:" + (button == null ? "missing" : button.name);
                    return false;
                }
            }
            return true;
        }

        private string GlyphEvidence(Button button)
        {
            RectTransform hitbox = button == null ? null : button.transform as RectTransform;
            RectTransform icon = button == null ? null : button.transform.Find("KBP.Icon") as RectTransform;
            Image image = icon == null ? null : icon.GetComponent<Image>();
            return icon == null || hitbox == null ? "missing" : "anchor=" + icon.anchorMin +
                ",pivot=" + icon.pivot + ",position=" + icon.anchoredPosition +
                ",size=" + icon.sizeDelta + ",scale=" + icon.localScale +
                ",preserve=" + (image != null && image.preserveAspect) + ",rectCenterDelta=" +
                Vector2.Distance(ScreenCenter(icon), ScreenCenter(hitbox)).ToString("F2") +
                ",alphaCenterCorrection=" + HudGlyphLayout.OpticalOffset(
                    image == null ? null : image.sprite, icon.rect.width);
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
            string name = char.ToUpperInvariant(routineId[0]) + routineId.Substring(1);
            if (_session.Model == null) return "Load a campaign to run " + name + ".";
            if (_session.IsExecuting) return "A buff routine is already executing.";
            RoutineProfile routine = _session.Model.Profile.Routines
                .FirstOrDefault(item => item.RoutineId == routineId);
            if (routine == null || routine.Assignments.Count == 0)
                return "No " + name + " buffs are configured.";
            int targets = routine.Assignments.Sum(item => item.WantedTargetUnitIds.Count);
            return "Execute " + name + ": " + routine.Assignments.Count +
                " buffs, " + targets + " requested targets, " +
                _session.Model.Profile.Execution.Mode + " mode.";
        }

        private Sprite CreateIcon(string kind)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.name = "KingmakerBuffPlanner." + kind + ".Icon";
            texture.filterMode = FilterMode.Bilinear;
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
            texture.SetPixels(pixels);
            // The project-owned glyph is an alpha mask; the captured native icon material
            // and tint provide every visible state color at runtime.
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 64f);
            sprite.name = texture.name;
            _ownedTextures.Add(texture);
            _ownedSprites.Add(sprite);
            return sprite;
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
            Color iconColor)
        {
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
        internal bool IsComplete { get { return TargetSprite != null; } }

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
                    ";iconColor=" + ColorEvidence(IconColor);
            }
        }

        internal static NativeHudButtonStyle Capture(ButtonPF nativeButton)
        {
            Image target = nativeButton == null ? null : nativeButton.targetGraphic as Image;
            if (target == null) return null;
            Image icon = nativeButton.GetComponentsInChildren<Image>(true)
                .Where(image => image != null && image != target && image.sprite != null)
                .FirstOrDefault();
            return new NativeHudButtonStyle(target.sprite, target.type, target.material,
                target.color, nativeButton.transition, nativeButton.colors,
                nativeButton.spriteState, nativeButton.navigation,
                nativeButton.OnClickSoundActive, nativeButton.OnHoverSoundActive,
                icon == null ? null : icon.material,
                icon == null ? Color.white : icon.color);
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

    internal static class HudGlyphLayout
    {
        internal static Vector2 OpticalOffset(Sprite sprite, float safeSize)
        {
            if (sprite == null || sprite.texture == null || safeSize <= 0f) return Vector2.zero;
            Rect region = sprite.rect;
            Texture2D texture = sprite.texture;
            int minX = (int)region.xMax, minY = (int)region.yMax;
            int maxX = (int)region.xMin - 1, maxY = (int)region.yMin - 1;
            for (int y = (int)region.yMin; y < (int)region.yMax; y++)
                for (int x = (int)region.xMin; x < (int)region.xMax; x++)
                    if (texture.GetPixel(x, y).a > 0.05f)
                    {
                        minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                    }
            if (maxX < minX || maxY < minY) return Vector2.zero;
            float alphaCenterX = (minX + maxX + 1f) * 0.5f;
            float alphaCenterY = (minY + maxY + 1f) * 0.5f;
            float regionCenterX = region.xMin + region.width * 0.5f;
            float regionCenterY = region.yMin + region.height * 0.5f;
            return new Vector2(-(alphaCenterX - regionCenterX) / region.width * safeSize,
                -(alphaCenterY - regionCenterY) / region.height * safeSize);
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
