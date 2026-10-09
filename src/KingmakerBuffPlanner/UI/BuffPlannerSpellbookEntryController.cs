using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.ServiceWindow;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // Owned Buff Planner button on the native spellbook tab, plus the guarded
    // handoff into the planner. The button is created and destroyed only by
    // this controller, never joins the native layout, and never touches
    // native listeners. Discovery is a bounded exact-path/tolerant-scan
    // lookup on a fixed cadence — no per-frame global hierarchy search.
    //
    // WP2B repair (the button did not open the planner):
    // - The button was pinned 20/18 units inside the spellbook's top-right
    //   corner, which is exactly where the service window's 143x137 top-bar
    //   Close button sits; it now lives on the service window root as its
    //   last child (above the top bar) and is placed by
    //   SpellbookEntryPlacement against the live native controls.
    // - The close affordance was searched by name inside the spellbook,
    //   which holds no button named "Close"; the handoff therefore always
    //   refused. It is now the exact ServiceWindow/Top/Close button.
    // - Every handoff closes the native window first and waits until both
    //   the FullScreenUi mode and the window itself have released.
    internal sealed class BuffPlannerSpellbookEntryController
    {
        internal const string ButtonName = "BuffPlannerSpellbookButton";
        private const string RetiredButtonName = ButtonName + ".Retired";
        private const int TickCadence = 15;
        // The service window's tabs and top bar lay out after activation; the
        // placement is checked once more after this many frames.
        private const int PlacementSettleFrames = 30;
        private const float ButtonDesignWidth = 170f;
        private const float ButtonDesignHeight = 36f;

        private readonly Action<string> _log;
        private readonly Func<bool> _openPlanner;
        private readonly Func<bool> _plannerOpen;
        private readonly Func<bool> _plannerPresentationReady;
        private readonly Action<SpellbookHandoffRecovery, string> _recover;
        private readonly SpellbookHandoffStateMachine _handoff =
            new SpellbookHandoffStateMachine();
        private PlannerUiTheme _theme;
        private int _themeCanvasInstanceId;
        private int _tickSkip;
        private int _spellbookInstanceId;
        private string _spellbookLocator;
        private Transform _spellbook;
        private Transform _serviceWindow;
        private UIWindow _serviceTabs;
        // The window a running handoff closed; kept apart from _serviceTabs,
        // which is forgotten as soon as the spellbook tab disappears.
        private UIWindow _handoffWindow;
        private Button _nativeClose;
        private Button _ownedButton;
        private bool _handoffActive;
        private int _placementSettle;
        private string _unavailableReason;

        internal BuffPlannerSpellbookEntryController(
            Action<string> log, Func<bool> openPlanner, Func<bool> plannerOpen,
            PlannerUiTheme theme,
            Func<bool> plannerPresentationReady = null,
            Action<SpellbookHandoffRecovery, string> recover = null)
        {
            _log = log ?? (value => { });
            _openPlanner = openPlanner ?? throw new ArgumentNullException("openPlanner");
            _plannerOpen = plannerOpen ?? throw new ArgumentNullException("plannerOpen");
            // The screen lifecycle opens presentation-first with deferred
            // readiness, so "is open" alone is not success.
            _plannerPresentationReady = plannerPresentationReady ?? plannerOpen;
            _recover = recover ?? delegate { };
            _theme = theme ?? PlannerUiTheme.Resolve(null);
        }

        internal SpellbookHandoffState HandoffState { get { return _handoff.State; } }
        internal string HandoffFailure { get { return _handoff.Failure; } }
        internal bool IsAttached
        {
            get { return _ownedButton != null && _ownedButton.gameObject != null; }
        }

        // Runtime evidence only (guarded physical scenario).
        internal Button OwnedButtonForRuntime { get { return IsAttached ? _ownedButton : null; } }
        internal Transform ServiceWindowForRuntime { get { return _serviceWindow; } }
        internal Transform SpellbookWindowForRuntime { get { return _spellbook; } }
        internal string PlacementForRuntime { get; private set; }
        internal string LastRefusalForRuntime { get; private set; }
        internal int NativeCloseInvocationsForRuntime { get; private set; }
        internal int OpenerInvocationsForRuntime { get; private set; }
        internal int ButtonsAttachedForRuntime { get; private set; }
        internal bool NativeOwnerActiveForRuntime { get { return NativeOwnerActive(); } }

        // Owned buttons currently present under the native windows (strays
        // from an earlier controller included; retired ones excluded).
        internal int OwnedButtonCountForRuntime
        {
            get { return CountOwned(_serviceWindow) + CountOwned(_spellbook); }
        }

        internal void Tick()
        {
            if (_handoffActive) TickHandoff();
            // The button lives on the service window root, which stays shown
            // for the other tabs: leave with the spellbook tab immediately.
            if (_ownedButton != null && (_spellbook == null ||
                    !_spellbook.gameObject.activeInHierarchy))
                ForgetWindow();
            if (_ownedButton != null && _placementSettle > 0 && --_placementSettle == 0)
            {
                try { PlaceOwnedButton("settled"); }
                catch (Exception exception)
                {
                    _log("[KBP-SPELLBOOK] placement failed: " + exception.Message);
                }
            }
            if (++_tickSkip < TickCadence) return;
            _tickSkip = 0;
            try
            {
                ObserveSpellbook();
            }
            catch (Exception exception)
            {
                _log("[KBP-SPELLBOOK] observe failed: " + exception.Message);
            }
        }

        private bool NativeOwnerActive()
        {
            bool fullScreenActive = Game.Instance != null &&
                Game.Instance.IsModeActive(GameModeType.FullScreenUi);
            UIWindow window = _handoffActive ? _handoffWindow : _serviceTabs;
            bool windowShown = window != null && window.IsShow;
            return fullScreenActive || windowShown;
        }

        private void TickHandoff()
        {
            if (_handoff.ObserveRelease(NativeOwnerActive()))
            {
                // Native ownership released: invoke the opener exactly once,
                // then wait out the deferred presentation lifecycle.
                bool accepted = false;
                OpenerInvocationsForRuntime++;
                try { accepted = _openPlanner(); }
                catch (Exception exception)
                {
                    _log("[KBP-SPELLBOOK] opener threw: " + exception.Message);
                }
                _handoff.ObserveOpenResult(accepted);
                if (_handoff.State == SpellbookHandoffState.Failed)
                    FailHandoff();
                return;
            }
            if (_handoff.ObservePresentation(_plannerPresentationReady()))
            {
                _handoffActive = false;
                _handoffWindow = null;
                RestoreButton();
                _log("[KBP-SPELLBOOK] handoff completed;planner=presentation-ready.");
                return;
            }
            if (_handoff.State == SpellbookHandoffState.Failed)
                FailHandoff();
        }

        private void FailHandoff()
        {
            _handoffActive = false;
            _handoffWindow = null;
            string failure = _handoff.Failure;
            SpellbookHandoffRecovery recovery = SpellbookHandoffStateMachine.RecoveryFor(failure);
            RestoreButton();
            try { _recover(recovery, failure); }
            catch (Exception exception)
            {
                _log("[KBP-SPELLBOOK] native recovery failed: " + exception.Message);
            }
            _log("[KBP-SPELLBOOK] handoff failed;reason=" + failure +
                ";recovery=" + recovery + ".");
        }

        private void ObserveSpellbook()
        {
            StaticCanvas canvas = StaticCanvas.Instance;
            if (canvas == null) return;
            string reason;
            SpellbookWindowLocator.Result found = SpellbookWindowLocator.Find(
                canvas.transform, new UnityNodeAccess(), out reason);
            Transform window = found == null ? null : found.Window as Transform;
            if (window == null || !window.gameObject.activeInHierarchy)
            {
                // Either the window has not been created yet in this scene, or
                // it was destroyed with the scene taking our button with it;
                // forget the dead reference and wait for the next window
                // instance instead of recreating anything here.
                ForgetWindow();
                if (!string.IsNullOrEmpty(reason) && reason.StartsWith(
                        "spellbook-window-ambiguous", StringComparison.Ordinal))
                    _log("[KBP-SPELLBOOK] discovery refused;reason=" + reason + ".");
                return;
            }
            int instanceId = window.GetInstanceID();
            if (instanceId == _spellbookInstanceId && _ownedButton != null) return;
            ForgetWindow();
            Transform serviceWindow = found.ServiceWindow as Transform;
            Transform closeNode = found.NativeClose as Transform;
            Button nativeClose = closeNode == null ? null : closeNode.GetComponent<Button>();
            string unavailable = serviceWindow == null ? "service-window-missing"
                : closeNode == null ? found.CloseReason
                : nativeClose == null ? "native-close-not-a-button" : null;
            if (unavailable != null)
            {
                // Offer no control whose handoff cannot run; log once per cause.
                if (!string.Equals(unavailable, _unavailableReason, StringComparison.Ordinal))
                    _log("[KBP-SPELLBOOK] entry unavailable;reason=" + unavailable + ".");
                _unavailableReason = unavailable;
                return;
            }
            _unavailableReason = null;
            _spellbook = window;
            _spellbookInstanceId = instanceId;
            _spellbookLocator = found.Locator;
            _serviceWindow = serviceWindow;
            _serviceTabs = serviceWindow.GetComponent<UIWindow>();
            _nativeClose = nativeClose;
            AttachOwnedButton(canvas);
        }

        private void AttachOwnedButton(StaticCanvas canvas)
        {
            if (_serviceWindow == null || _ownedButton != null) return;
            ResolveThemeForCanvas(canvas);
            RetireStrays(_serviceWindow);
            RetireStrays(_spellbook);
            _ownedButton = KingmakerUiFactory.CreateButton(ButtonName, _serviceWindow, _theme,
                "BUFF PLANNER", BeginHandoff);
            ButtonsAttachedForRuntime++;
            // Last child of the service window: drawn and raycast above the
            // native top bar and the spellbook page.
            _ownedButton.transform.SetAsLastSibling();
            // Out-of-layout, like the HUD row: a planner-owned control must
            // never participate in native layout or the native layout will
            // re-anchor it.
            LayoutElement layout = _ownedButton.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            RectTransform buttonRect = (RectTransform)_ownedButton.transform;
            // Explicit design size: percentage anchors of the native window
            // produced a sliver-sized, caption-clipped control in RC1.
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.sizeDelta = new Vector2(ButtonDesignWidth, ButtonDesignHeight);
            Text label = _ownedButton.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontSize = 14;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 11;
                label.resizeTextMaxSize = 14;
            }
            _ownedButton.interactable = true;
            // Complete native button states (normal/hover/pressed/disabled),
            // not just a borrowed normal sprite: borrow through the same
            // validated donor contract the planner surface uses. Fail-soft —
            // the readable factory styling stays when donors are absent.
            try
            {
                PlannerNativeTheme nativeTheme = PlannerNativeTheme.Resolve(canvas);
                NativeThemeResource buttonDonor = nativeTheme.Resources.Get(
                    NativeThemeCapability.Buttons);
                if (buttonDonor != null)
                    PlannerNativeTheme.ApplyButton((Button)buttonDonor.Components[0],
                        _ownedButton);
                NativeThemeResource bodyDonor = nativeTheme.Resources.Get(
                    NativeThemeCapability.Body);
                if (bodyDonor != null)
                    foreach (Text buttonText in _ownedButton.GetComponentsInChildren<Text>(true))
                        PlannerNativeTheme.ApplyText((Text)bodyDonor.Components[0], buttonText);
                _log("[KBP-SPELLBOOK] native button states applied;summary=" +
                    nativeTheme.Resources.Summary + ".");
            }
            catch (Exception exception)
            {
                _log("[KBP-SPELLBOOK] native button states unavailable;reason=" +
                    exception.Message + ".");
            }
            Canvas.ForceUpdateCanvases();
            KingmakerUiFactory.FitButtonToCaption(buttonRect, ButtonDesignWidth,
                ButtonDesignHeight);
            PlaceOwnedButton("attached");
            _placementSettle = PlacementSettleFrames;
            _log("[KBP-SPELLBOOK] owned button attached;window=" + _spellbookInstanceId +
                ";locator=" + _spellbookLocator +
                ";parent=service-window-root;close=" + SpellbookWindowLocator.NativeClosePath +
                ";theme=" + (_theme == null ? "missing" : _theme.ResolutionSummary) +
                ";screen=" + Screen.width + "x" + Screen.height + ".");
        }

        // Ordered candidates against every active native control of the
        // service window, in the service window's own local space.
        private void PlaceOwnedButton(string stage)
        {
            if (_ownedButton == null || _nativeClose == null) return;
            var root = _serviceWindow as RectTransform;
            var close = _nativeClose.transform as RectTransform;
            var buttonRect = (RectTransform)_ownedButton.transform;
            if (root == null || close == null) return;
            Rect bounds = root.rect;
            var window = new PlacementRect(bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax);
            var controls = new List<PlacementRect>();
            foreach (Selectable selectable in _serviceWindow.GetComponentsInChildren<Selectable>(false))
            {
                if (selectable == null || selectable == _nativeClose || !selectable.IsActive())
                    continue;
                if (selectable.transform.IsChildOf(_ownedButton.transform)) continue;
                var target = selectable.transform as RectTransform;
                if (target != null) controls.Add(LocalRect(root, target));
            }
            SpellbookEntryPlacementResult placement = SpellbookEntryPlacement.Choose(window,
                LocalRect(root, close), controls, buttonRect.rect.width, buttonRect.rect.height);
            buttonRect.anchoredPosition = new Vector2(placement.Rect.XMax - window.XMax,
                placement.Rect.YMax - window.YMax);
            string summary = placement.Candidate + ";rect=" + placement.Rect +
                ";conflictFree=" + placement.ConflictFree +
                (placement.ConflictFree ? string.Empty : ";conflict=" + placement.Conflict);
            if (!string.Equals(summary, PlacementForRuntime, StringComparison.Ordinal))
                _log("[KBP-SPELLBOOK] placement " + stage + ";candidate=" + summary +
                    ";controls=" + controls.Count + ".");
            PlacementForRuntime = summary;
        }

        private static PlacementRect LocalRect(RectTransform root, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            float xMin = float.MaxValue, yMin = float.MaxValue;
            float xMax = float.MinValue, yMax = float.MinValue;
            foreach (Vector3 corner in corners)
            {
                Vector3 local = root.InverseTransformPoint(corner);
                xMin = Math.Min(xMin, local.x);
                yMin = Math.Min(yMin, local.y);
                xMax = Math.Max(xMax, local.x);
                yMax = Math.Max(yMax, local.y);
            }
            return new PlacementRect(xMin, yMin, xMax, yMax);
        }

        // The boot-time theme resolves with no campaign canvas and therefore
        // no native artwork; the button re-resolves against the live
        // StaticCanvas so it borrows the native button sprite family.
        private void ResolveThemeForCanvas(StaticCanvas canvas)
        {
            int canvasId = canvas == null || canvas.transform == null
                ? 0 : canvas.transform.GetInstanceID();
            if (_themeCanvasInstanceId == canvasId && _theme != null) return;
            _theme = PlannerUiTheme.Resolve(canvas);
            _themeCanvasInstanceId = canvasId;
        }

        private void ForgetWindow()
        {
            DetachOwnedButton();
            _spellbook = null;
            _spellbookInstanceId = 0;
            _serviceWindow = null;
            _serviceTabs = null;
            _nativeClose = null;
            _placementSettle = 0;
        }

        private void DetachOwnedButton()
        {
            if (_ownedButton == null) return;
            // Only the owned control is destroyed; nothing native is touched.
            if (_ownedButton.gameObject != null) Retire(_ownedButton.gameObject);
            _ownedButton = null;
        }

        // A button left by an earlier controller instance (mod toggle,
        // reload) never accumulates beside the new one.
        private static void RetireStrays(Transform parent)
        {
            if (parent == null) return;
            var strays = new List<GameObject>();
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child != null && child.name == ButtonName) strays.Add(child.gameObject);
            }
            foreach (GameObject stray in strays) Retire(stray);
        }

        // Destroy is deferred to the end of the frame; the retired object is
        // hidden and renamed at once so it is never seen or counted again.
        private static void Retire(GameObject owned)
        {
            owned.SetActive(false);
            owned.name = RetiredButtonName;
            UnityEngine.Object.Destroy(owned);
        }

        private static int CountOwned(Transform parent)
        {
            if (parent == null) return 0;
            int count = 0;
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child != null && child.name == ButtonName) count++;
            }
            return count;
        }

        private void BeginHandoff()
        {
            bool nativeCloseUsable = _nativeClose != null &&
                _nativeClose.gameObject.activeInHierarchy && _nativeClose.IsInteractable();
            string refusal = SpellbookHandoffStateMachine.Admit(_handoffActive,
                Game.Instance != null, _plannerOpen(), nativeCloseUsable);
            if (refusal != null)
            {
                LastRefusalForRuntime = refusal;
                _log("[KBP-SPELLBOOK] handoff refused;reason=" + refusal + ".");
                return;
            }
            _log("[KBP-SPELLBOOK] handoff begin;fullScreenActive=" +
                Game.Instance.IsModeActive(GameModeType.FullScreenUi) +
                ";windowShown=" + (_serviceTabs != null && _serviceTabs.IsShow) +
                ";window=" + _spellbookInstanceId + ".");
            // Close through the native window's own close affordance; the
            // planner never seizes the mode itself.
            _handoff.Begin();
            _handoffActive = true;
            _handoffWindow = _serviceTabs;
            if (_ownedButton != null) _ownedButton.interactable = false;
            NativeCloseInvocationsForRuntime++;
            _nativeClose.onClick.Invoke();
        }

        private void RestoreButton()
        {
            if (_ownedButton != null && _ownedButton.gameObject != null)
                _ownedButton.interactable = true;
        }

        internal void Release()
        {
            _handoff.Reset();
            _handoffActive = false;
            _handoffWindow = null;
            ForgetWindow();
        }

        // Structural node access for the bounded locator: transforms only,
        // no component enumeration and no donor validation.
        private sealed class UnityNodeAccess : INativeThemeSource
        {
            public bool IsAlive(object value)
            {
                return value is UnityEngine.Object && (UnityEngine.Object)value != null;
            }
            public bool SameNode(object first, object second)
            {
                return (UnityEngine.Object)first == (UnityEngine.Object)second;
            }
            public string Name(object node) { return ((Transform)node).name; }
            public object Parent(object node) { return ((Transform)node).parent; }
            public int ChildCount(object node) { return ((Transform)node).childCount; }
            public object Child(object node, int index)
            {
                return ((Transform)node).GetChild(index);
            }
            public object[] Components(object node, NativeThemeComponent component)
            {
                throw new NotSupportedException("Locator does not enumerate components.");
            }
            public NativeThemeResource SoundResource()
            {
                throw new NotSupportedException("Locator does not resolve donors.");
            }
            public void Validate(NativeThemeCapability capability, object[] components)
            {
                throw new NotSupportedException("Locator does not validate donors.");
            }
        }
    }
}
