using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.Constructor;
using Kingmaker.UI.ServiceWindow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // Owned Buff Planner button on the native spellbook tab, plus the guarded
    // handoff into the planner. The button is created and destroyed only by
    // this controller, never joins the native layout, and never touches
    // native listeners; the only native state it changes is the Metamagic
    // footer's geometry, put back exactly. Discovery is a bounded exact-path/tolerant-scan
    // lookup on a fixed cadence — no per-frame global hierarchy search.
    //
    // WP2B repair (the button did not open the planner):
    // - The button was pinned 20/18 units inside the spellbook's top-right
    //   corner, which is exactly where the service window's 143x137 top-bar
    //   Close button sits (superseded by the footer pair below).
    // - The close affordance was searched by name inside the spellbook,
    //   which holds no button named "Close"; the handoff therefore always
    //   refused. It is now the exact ServiceWindow/Top/Close button.
    // - Every handoff closes the native window first and waits until both
    //   the FullScreenUi mode and the window itself have released.
    //
    // Native UI finish (UI-10): the entry is BUFF PLANNER beside METAMAGIC
    // in the book's footer (SpellbookFooterLayout), built from the native
    // Metamagic button's own art, states, sounds and caption style; the
    // top-right entry is gone. The native Metamagic place is narrowed and
    // its plate widened over both while the planner button is attached, and
    // restored exactly when it detaches.
    internal sealed class BuffPlannerSpellbookEntryController
    {
        internal const string ButtonName = "BuffPlannerSpellbookButton";
        private const string RetiredButtonName = ButtonName + ".Retired";
        private const int TickCadence = 15;
        // The service window's tabs and top bar lay out after activation; the
        // placement is checked once more after this many frames.
        private const int PlacementSettleFrames = 30;
        internal const string FooterPlaceName = "BuffPlannerButtonPlace";
        internal const string NativeFooterPath = "Container_Book/MetamagicButtonPlace";

        private readonly Action<string> _log;
        private readonly Func<bool> _openPlanner;
        private readonly Func<bool> _plannerOpen;
        private readonly Func<bool> _plannerPresentationReady;
        private readonly Action<SpellbookHandoffRecovery, string> _recover;
        private readonly SpellbookHandoffStateMachine _handoff =
            new SpellbookHandoffStateMachine();
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
        private RectTransform _ownedPlace;
        private Image _ownedPlate;
        private NativeFooter _nativeFooter;
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
        // Handoffs whose native owner (mode and window) was observed released.
        internal int NativeReleasesForRuntime { get; private set; }
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
            // Every frame (a few comparisons): Metamagic shows or hides with
            // the character, and the native window may lay its footer out
            // again; the pair follows at once.
            if (_ownedButton != null)
            {
                try { PlaceOwnedButton(_placementSettle > 0 && --_placementSettle == 0 ? "settled" : null); }
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
                NativeReleasesForRuntime++;
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
            AttachOwnedButton();
        }

        private void AttachOwnedButton()
        {
            if (_serviceWindow == null || _spellbook == null || _ownedButton != null) return;
            RetireStrays(_serviceWindow);
            RetireStrays(_spellbook);
            NativeFooter footer = NativeFooter.Find(_spellbook);
            if (footer == null)
            {
                const string missing = "native-footer-missing:" + NativeFooterPath;
                if (!string.Equals(missing, _unavailableReason, StringComparison.Ordinal))
                    _log("[KBP-SPELLBOOK] entry unavailable;reason=" + missing + ".");
                _unavailableReason = missing;
                return;
            }
            _nativeFooter = footer;
            // The planner's place: the native Metamagic place's construction
            // on the spellbook root (shown for every character, whether or
            // not the book's footer is), drawn above the book.
            _ownedPlace = KingmakerUiFactory.CreateRect(FooterPlaceName, _spellbook);
            _ownedPlace.anchorMin = new Vector2(0.5f, 0.5f);
            _ownedPlace.anchorMax = new Vector2(0.5f, 0.5f);
            _ownedPlace.pivot = new Vector2(0.5f, 0.5f);
            _ownedPlace.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            RectTransform plateRect = KingmakerUiFactory.CreateRect("ButtonPlace", _ownedPlace);
            plateRect.anchorMin = footer.Plate.rectTransform.anchorMin;
            plateRect.anchorMax = footer.Plate.rectTransform.anchorMax;
            plateRect.offsetMin = footer.PlateOffsetMin;
            plateRect.offsetMax = footer.PlateOffsetMax;
            _ownedPlate = plateRect.gameObject.AddComponent<Image>();
            CopyImage(footer.Plate, _ownedPlate);
            RectTransform buttonRect = KingmakerUiFactory.CreateRect(ButtonName, _ownedPlace);
            KingmakerUiFactory.Stretch(buttonRect, 0, 0, 0, 0);
            Image face = buttonRect.gameObject.AddComponent<Image>();
            CopyImage(footer.Apply.image, face);
            ButtonPF button = buttonRect.gameObject.AddComponent<ButtonPF>();
            button.targetGraphic = face;
            button.transition = footer.Apply.transition;
            button.spriteState = footer.Apply.spriteState;
            button.colors = footer.Apply.colors;
            button.navigation = footer.Apply.navigation;
            var nativeButton = footer.Apply as ButtonPF;
            if (nativeButton != null)
            {
                button.OnClickSoundActive = nativeButton.OnClickSoundActive;
                button.OnHoverSoundActive = nativeButton.OnHoverSoundActive;
            }
            button.onClick.AddListener(BeginHandoff);
            CreateCaption(buttonRect, footer.Label, "Buff Planner");
            _ownedButton = button;
            _ownedButton.interactable = true;
            ButtonsAttachedForRuntime++;
            // Above the book, below anything the spellbook opens over it.
            Transform book = footer.Place.parent;
            if (book != null && book.parent == _spellbook) _ownedPlace.SetSiblingIndex(book.GetSiblingIndex() + 1);
            else _ownedPlace.SetAsLastSibling();
            PlaceOwnedButton("attached");
            _placementSettle = PlacementSettleFrames;
            _log("[KBP-SPELLBOOK] owned button attached;window=" + _spellbookInstanceId +
                ";locator=" + _spellbookLocator + ";parent=spellbook-footer;close=" +
                SpellbookWindowLocator.NativeClosePath + ";native=" + footer.Evidence +
                ";screen=" + Screen.width + "x" + Screen.height + ".");
        }

        // The pair in the footer: the native Metamagic place narrowed (or
        // restored when Metamagic is hidden), the planner place beside it in
        // the book container's own space, the shared native plate widened
        // over both or the planner's own plate shown alone.
        private void PlaceOwnedButton(string stage)
        {
            if (_ownedButton == null || _ownedPlace == null || _nativeFooter == null) return;
            if (!_nativeFooter.Alive)
            {
                _nativeFooter = null;
                return;
            }
            bool metamagicShown = _nativeFooter.Apply.gameObject.activeInHierarchy;
            SpellbookFooterLayout layout = SpellbookFooterLayout.Compute(_nativeFooter.OriginalCentre.x,
                _nativeFooter.OriginalSize.x, metamagicShown);
            if (metamagicShown) _nativeFooter.Pair(layout);
            else _nativeFooter.Restore();
            _ownedPlate.enabled = !metamagicShown;
            Transform book = _nativeFooter.Place.parent;
            Vector3 centre = book.TransformPoint(new Vector3(layout.PlannerCentre, _nativeFooter.OriginalCentre.y,
                _nativeFooter.OriginalCentre.z));
            Vector3 scale = book.lossyScale;
            Vector3 parentScale = _spellbook.lossyScale;
            _ownedPlace.localScale = new Vector3(SafeRatio(scale.x, parentScale.x), SafeRatio(scale.y, parentScale.y), 1f);
            _ownedPlace.sizeDelta = new Vector2(layout.PlannerWidth, _nativeFooter.OriginalSize.y);
            _ownedPlace.position = centre;
            string summary = "footer-pair;" + layout.Describe();
            if (stage != null && !string.Equals(summary, PlacementForRuntime, StringComparison.Ordinal))
                _log("[KBP-SPELLBOOK] placement " + stage + ";candidate=" + summary + ".");
            else if (!string.Equals(summary, PlacementForRuntime, StringComparison.Ordinal))
                _log("[KBP-SPELLBOOK] placement changed;candidate=" + summary + ".");
            PlacementForRuntime = summary;
        }

        private static float SafeRatio(float value, float parent)
        {
            return Mathf.Abs(parent) < 0.0001f ? 1f : value / parent;
        }

        private static void CopyImage(Image native, Image owned)
        {
            owned.sprite = native.sprite;
            owned.type = native.type;
            owned.material = native.material;
            owned.color = native.color;
            owned.fillCenter = native.fillCenter;
            owned.preserveAspect = native.preserveAspect;
            owned.raycastTarget = native.raycastTarget;
        }

        // The native Metamagic caption's face, material, size range, case and
        // ink; created inactive so TextMeshPro never falls back to its own
        // default font while the native one is assigned.
        private static void CreateCaption(RectTransform button, TMP_Text native, string caption)
        {
            var go = new GameObject("TextMeshPro Text");
            go.SetActive(false);
            go.layer = button.gameObject.layer;
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(button, false);
            KingmakerUiFactory.Stretch(rect, 0, 0, 0, 0);
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.font = native.font;
            label.fontSharedMaterial = native.fontSharedMaterial;
            label.enableAutoSizing = native.enableAutoSizing;
            label.fontSizeMin = native.fontSizeMin;
            label.fontSizeMax = native.fontSizeMax;
            label.fontSize = native.enableAutoSizing ? native.fontSizeMax : native.fontSize;
            label.fontStyle = native.fontStyle;
            label.fontWeight = native.fontWeight;
            label.color = native.color;
            label.alignment = native.alignment;
            label.characterSpacing = native.characterSpacing;
            label.wordSpacing = native.wordSpacing;
            label.enableWordWrapping = native.enableWordWrapping;
            label.overflowMode = native.overflowMode;
            label.margin = native.margin;
            label.richText = native.richText;
            label.raycastTarget = false;
            label.text = caption;
            go.SetActive(true);
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
            // The native footer back exactly as it was; then only the owned
            // place is destroyed.
            if (_nativeFooter != null && _nativeFooter.Alive) _nativeFooter.Restore();
            _nativeFooter = null;
            if (_ownedPlace != null) Retire(_ownedPlace.gameObject);
            else if (_ownedButton != null && _ownedButton.gameObject != null) Retire(_ownedButton.gameObject);
            _ownedPlace = null;
            _ownedPlate = null;
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
                if (child != null && (child.name == ButtonName || child.name == FooterPlaceName))
                    strays.Add(child.gameObject);
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
                if (child == null) continue;
                if (child.name == ButtonName) count++;
                else if (child.name == FooterPlaceName && child.Find(ButtonName) != null) count++;
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

        // The native Metamagic footer (exact 2.1.7b contract, kbpnui-facts-03):
        // Container_Book/MetamagicButtonPlace (a horizontal layout group)
        // holding the plate ButtonPlace (main_menu_back, out of layout,
        // stretched past the place) and the ButtonPF Apply with its TMP
        // caption. What pairing changes is recorded first and put back
        // exactly by Restore.
        private sealed class NativeFooter
        {
            internal RectTransform Place;
            internal Button Apply;
            internal Image Plate;
            internal TMP_Text Label;
            internal Vector3 OriginalCentre;
            internal Vector2 OriginalSize;
            private Vector2 _placePosition;
            private Vector2 _applySize;
            private Vector2 _applyPosition;
            private Vector2 _labelSize;
            private Vector2 _labelPosition;
            internal Vector2 PlateOffsetMin;
            internal Vector2 PlateOffsetMax;
            private bool _paired;

            internal static NativeFooter Find(Transform spellbook)
            {
                var place = spellbook == null ? null : spellbook.Find(NativeFooterPath) as RectTransform;
                Transform apply = place == null ? null : place.Find("Apply");
                Transform plate = place == null ? null : place.Find("ButtonPlace");
                Button button = apply == null ? null : apply.GetComponent<Button>();
                Image plateImage = plate == null ? null : plate.GetComponent<Image>();
                TMP_Text label = apply == null ? null : apply.GetComponentInChildren<TMP_Text>(true);
                if (place == null || place.parent == null || button == null || button.image == null ||
                    plateImage == null || plateImage.sprite == null || label == null || label.font == null)
                    return null;
                return new NativeFooter
                {
                    Place = place,
                    Apply = button,
                    Plate = plateImage,
                    Label = label,
                    OriginalCentre = place.localPosition,
                    OriginalSize = place.sizeDelta,
                    _placePosition = place.anchoredPosition,
                    _applySize = ((RectTransform)apply).sizeDelta,
                    _applyPosition = ((RectTransform)apply).anchoredPosition,
                    _labelSize = label.rectTransform.sizeDelta,
                    _labelPosition = label.rectTransform.anchoredPosition,
                    PlateOffsetMin = plateImage.rectTransform.offsetMin,
                    PlateOffsetMax = plateImage.rectTransform.offsetMax
                };
            }

            internal bool Alive
            {
                get { return Place != null && Apply != null && Plate != null && Label != null; }
            }

            internal string Evidence
            {
                get
                {
                    return "place=" + OriginalSize.x.ToString("0.00") + "x" + OriginalSize.y.ToString("0.00") +
                        "@" + OriginalCentre.x.ToString("0.00") + "," + OriginalCentre.y.ToString("0.00") +
                        ";plate=" + Plate.sprite.name + ";face=" + (Apply.image.sprite == null ? "none" : Apply.image.sprite.name) +
                        ";caption=" + Label.font.name + "|" + Label.fontSharedMaterial.name;
                }
            }

            internal void Pair(SpellbookFooterLayout layout)
            {
                float dx = layout.MetamagicCentre - OriginalCentre.x;
                Vector2 position = _placePosition + new Vector2(dx, 0f);
                var size = new Vector2(layout.MetamagicWidth, OriginalSize.y);
                var apply = (RectTransform)Apply.transform;
                var applySize = new Vector2(layout.MetamagicWidth, _applySize.y);
                var applyPosition = new Vector2(_applyPosition.x - (_applySize.x - layout.MetamagicWidth) / 2f,
                    _applyPosition.y);
                var labelSize = new Vector2(layout.MetamagicWidth, _labelSize.y);
                var labelPosition = new Vector2(_labelPosition.x - (_labelSize.x - layout.MetamagicWidth) / 2f,
                    _labelPosition.y);
                var plateMax = PlateOffsetMax + new Vector2(layout.PlateExtension, 0f);
                if (_paired && Place.anchoredPosition == position && Place.sizeDelta == size &&
                    apply.sizeDelta == applySize && Plate.rectTransform.offsetMax == plateMax) return;
                Place.anchoredPosition = position;
                Place.sizeDelta = size;
                apply.sizeDelta = applySize;
                apply.anchoredPosition = applyPosition;
                Label.rectTransform.sizeDelta = labelSize;
                Label.rectTransform.anchoredPosition = labelPosition;
                Plate.rectTransform.offsetMax = plateMax;
                _paired = true;
                LayoutRebuilder.MarkLayoutForRebuild(Place);
            }

            internal void Restore()
            {
                if (!_paired) return;
                Place.anchoredPosition = _placePosition;
                Place.sizeDelta = OriginalSize;
                ((RectTransform)Apply.transform).sizeDelta = _applySize;
                ((RectTransform)Apply.transform).anchoredPosition = _applyPosition;
                Label.rectTransform.sizeDelta = _labelSize;
                Label.rectTransform.anchoredPosition = _labelPosition;
                Plate.rectTransform.offsetMin = PlateOffsetMin;
                Plate.rectTransform.offsetMax = PlateOffsetMax;
                _paired = false;
                LayoutRebuilder.MarkLayoutForRebuild(Place);
            }
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
