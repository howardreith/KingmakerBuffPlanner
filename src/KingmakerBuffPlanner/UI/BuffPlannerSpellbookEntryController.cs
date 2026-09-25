using System;
using System.Linq;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // Owned Buff Planner button inside the native spellbook window, plus the
    // guarded handoff into the planner. The button is created and destroyed
    // only by this controller, is marked with an owned component, never
    // joins the native layout, and never touches native listeners. Discovery
    // is a bounded exact-path/tolerant-scan lookup on a fixed cadence — no
    // per-frame global hierarchy search. Rendered placement and the
    // same-context return trip require the live campaign lane and remain
    // unqualified.
    internal sealed class BuffPlannerSpellbookEntryController
    {
        internal const string ButtonName = "BuffPlannerSpellbookButton";
        private const int TickCadence = 15;
        private const float ButtonDesignWidth = 170f;
        private const float ButtonDesignHeight = 36f;

        private readonly Action<string> _log;
        private readonly Func<bool> _openPlanner;
        private readonly Func<bool> _plannerOpen;
        private readonly Func<bool> _plannerPresentationReady;
        private readonly Action _recoverNativeUi;
        private readonly SpellbookHandoffStateMachine _handoff =
            new SpellbookHandoffStateMachine();
        private PlannerUiTheme _theme;
        private int _themeCanvasInstanceId;
        private int _tickSkip;
        private int _spellbookInstanceId;
        private string _spellbookLocator;
        private Transform _spellbook;
        private Button _ownedButton;
        private bool _handoffActive;

        internal BuffPlannerSpellbookEntryController(
            Action<string> log, Func<bool> openPlanner, Func<bool> plannerOpen,
            PlannerUiTheme theme,
            Func<bool> plannerPresentationReady = null,
            Action recoverNativeUi = null)
        {
            _log = log ?? (value => { });
            _openPlanner = openPlanner ?? throw new ArgumentNullException("openPlanner");
            _plannerOpen = plannerOpen ?? throw new ArgumentNullException("plannerOpen");
            // The screen lifecycle opens presentation-first with deferred
            // readiness, so "is open" alone is not success.
            _plannerPresentationReady = plannerPresentationReady ?? plannerOpen;
            // Recovery when the handoff fails after the native closure: ask
            // for the native escape veil so the player lands in a usable UI.
            _recoverNativeUi = recoverNativeUi ?? delegate { };
            _theme = theme ?? PlannerUiTheme.Resolve(null);
        }

        internal SpellbookHandoffState HandoffState { get { return _handoff.State; } }
        internal bool IsAttached
        {
            get { return _ownedButton != null && _ownedButton.gameObject != null; }
        }

        internal void Tick()
        {
            if (_handoffActive) TickHandoff();
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

        private void TickHandoff()
        {
            bool fullScreenActive = Game.Instance != null &&
                Game.Instance.IsModeActive(GameModeType.FullScreenUi);
            if (_handoff.ObserveRelease(fullScreenActive))
            {
                // Native ownership released: invoke the opener exactly once,
                // then wait out the deferred presentation lifecycle.
                bool accepted = false;
                try { accepted = _openPlanner(); }
                catch (Exception exception)
                {
                    _log("[KBP-SPELLBOOK] opener threw: " + exception.Message);
                }
                _handoff.ObserveOpenResult(accepted);
                if (_handoff.State == SpellbookHandoffState.Failed)
                    FailHandoffAfterClosure();
                return;
            }
            if (_handoff.ObservePresentation(_plannerPresentationReady()))
            {
                _handoffActive = false;
                RestoreButton();
                _log("[KBP-SPELLBOOK] handoff completed;planner=presentation-ready.");
                return;
            }
            if (_handoff.State == SpellbookHandoffState.Failed)
                FailHandoffAfterClosure();
        }

        private void FailHandoffAfterClosure()
        {
            _handoffActive = false;
            RestoreButton();
            // The spellbook already closed natively; recover to a usable
            // native interface instead of leaving the player in limbo.
            try { _recoverNativeUi(); }
            catch (Exception exception)
            {
                _log("[KBP-SPELLBOOK] native recovery failed: " + exception.Message);
            }
            _log("[KBP-SPELLBOOK] handoff failed;reason=" + _handoff.Failure +
                ";native-ui-recovery-requested.");
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
                if (_ownedButton != null && _ownedButton.gameObject != null)
                    UnityEngine.Object.Destroy(_ownedButton.gameObject);
                _ownedButton = null;
                _spellbook = null;
                _spellbookInstanceId = 0;
                if (!string.IsNullOrEmpty(reason) && reason.StartsWith(
                        "spellbook-window-ambiguous", StringComparison.Ordinal))
                    _log("[KBP-SPELLBOOK] discovery refused;reason=" + reason + ".");
                return;
            }
            int instanceId = window.GetInstanceID();
            if (instanceId != _spellbookInstanceId || _ownedButton == null)
            {
                DetachOwnedButton();
                _spellbook = window;
                _spellbookInstanceId = instanceId;
                _spellbookLocator = found.Locator;
                AttachOwnedButton(canvas);
            }
        }

        private void AttachOwnedButton(StaticCanvas canvas)
        {
            if (_spellbook == null || _ownedButton != null) return;
            ResolveThemeForCanvas(canvas);
            _ownedButton = KingmakerUiFactory.CreateButton(ButtonName, _spellbook, _theme,
                "BUFF PLANNER", BeginHandoff);
            // Out-of-layout, like the HUD row: a planner-owned control must
            // never participate in native spellbook layout or the native
            // layout will re-anchor it.
            LayoutElement layout = _ownedButton.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            RectTransform buttonRect = (RectTransform)_ownedButton.transform;
            // Corner anchoring with an explicit design size: percentage
            // anchors of the native window produced a sliver-sized,
            // caption-clipped control in RC1 regardless of the window's own
            // rect, so the button's size no longer depends on the parent's
            // geometry at all.
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-20f, -18f);
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
            var corners = new Vector3[4];
            buttonRect.GetWorldCorners(corners);
            _log("[KBP-SPELLBOOK] owned button attached;window=" + _spellbookInstanceId +
                ";locator=" + _spellbookLocator +
                ";theme=" + (_theme == null ? "missing" : _theme.ResolutionSummary) +
                ";screenRect=" + corners[0].x.ToString("F0") + "," + corners[0].y.ToString("F0") +
                "-" + corners[2].x.ToString("F0") + "," + corners[2].y.ToString("F0") +
                ";screen=" + Screen.width + "x" + Screen.height + ".");
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

        private void DetachOwnedButton()
        {
            if (_ownedButton == null) return;
            // Only the owned control is destroyed; nothing native is touched.
            if (_ownedButton.gameObject != null)
                UnityEngine.Object.Destroy(_ownedButton.gameObject);
            _ownedButton = null;
        }

        private void BeginHandoff()
        {
            if (_handoffActive) return;
            if (Game.Instance == null)
            {
                _log("[KBP-SPELLBOOK] handoff refused;reason=no-game.");
                return;
            }
            if (_plannerOpen())
            {
                _log("[KBP-SPELLBOOK] handoff refused;reason=planner-already-open.");
                return;
            }
            bool fullScreenActive = Game.Instance.IsModeActive(GameModeType.FullScreenUi);
            _log("[KBP-SPELLBOOK] handoff begin;fullScreenActive=" + fullScreenActive +
                ";window=" + _spellbookInstanceId + ".");
            if (fullScreenActive)
            {
                // Request native closure through the spellbook's own close
                // affordance; the planner never seizes the mode itself.
                Button nativeClose = _spellbook == null ? null : _spellbook
                    .GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(button => button != null &&
                        button.name == "Close" && button.gameObject != null);
                if (nativeClose == null)
                {
                    _log("[KBP-SPELLBOOK] handoff refused;reason=native-close-affordance-missing.");
                    return;
                }
                nativeClose.onClick.Invoke();
                _handoff.Begin();
                _handoffActive = true;
                if (_ownedButton != null) _ownedButton.interactable = false;
                return;
            }
            if (_openPlanner())
                _log("[KBP-SPELLBOOK] planner opened directly;no-fullscreen-owner.");
            else
                _log("[KBP-SPELLBOOK] direct open refused.");
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
            DetachOwnedButton();
            _spellbook = null;
            _spellbookInstanceId = 0;
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
