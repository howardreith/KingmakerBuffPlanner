using System;
using System.Linq;
using Kingmaker.Blueprints.Console;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // A small owned overlay. No cloned listeners, EventSystem submit handler,
    // fullscreen notification, pause state or mouse editor is involved.
    internal sealed class ControllerRoutineView : IDisposable
    {
        private GameObject _root;
        private TMP_Text _settings;
        private TMP_Text _details;
        private TMP_Text _status;
        private TMP_Text _confirm;
        private TMP_Text _title;
        private TMP_Text _back;
        private RectTransform _detailsPanel;
        private ScrollRect _scroll;
        private readonly TMP_Text[] _rows = new TMP_Text[6];
        private readonly Image[] _focus = new Image[6];
        private readonly Action<string> _log;
        private int _settleFrame;
        private bool _measured;
        private int _detailsSettleFrame;
        private bool _detailsMeasured;

        internal ControllerRoutineView(TMP_Text donor, Action<string> log = null)
        {
            _log = log ?? (_ => { });
            if (donor == null || donor.font == null)
                throw new InvalidOperationException("The native controller menu font is unavailable.");
            try
            {
                _root = new GameObject("KingmakerBuffPlanner.ControllerRoutines", typeof(RectTransform));
                Canvas canvas = _root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 250;
                CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                _root.AddComponent<GraphicRaycaster>();
                RectTransform backdrop = Rect(_root.transform, "Backdrop", Vector2.zero, Vector2.one);
                backdrop.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);
                RectTransform panel = Rect(_root.transform, "Menu", new Vector2(.5f, .5f), new Vector2(.5f, .5f));
                panel.sizeDelta = new Vector2(880, 660);
                panel.gameObject.AddComponent<Image>().color = new Color(.09f, .075f, .065f, 1);
                _title = Label(panel, donor, "Title", "Buff Planner", 28, 22, 18, 836, 42, _log);
                _settings = Label(panel, donor, "Settings", "", 22, 22, 68, 836, 38);
                for (int i = 0; i < 6; i++)
                {
                    RectTransform row = Rect(panel, "Focus" + i, new Vector2(0, 1), new Vector2(0, 1));
                    int position = i == 4 ? 0 : i < 4 ? i + 1 : i;
                    Place(row, 22, 110 + position * 44, 836, 40);
                    _focus[i] = row.gameObject.AddComponent<Image>();
                    _rows[i] = Label(row, donor, "Caption", "", 24, 12, 2, 812, 36, i == 0 ? _log : null);
                }
                _status = Label(panel, donor, "Status", "", 21, 22, 386, 836, 150, _log);
                Hint(panel, donor, RewiredConsts.Action.DPadUp, "Select", 22, 560, 130);
                _confirm = Hint(panel, donor, RewiredConsts.Action.Confirm, "", 220, 560, 330);
                _back = Hint(panel, donor, RewiredConsts.Action.Decline, "Back to game", 590, 560, 220);
                Label(panel, donor, "AuthoringNote", "Configure uses mouse and keyboard in this session.", 20, 22, 612, 836, 32);
                BuildResults(panel, donor);
                _settleFrame = Time.frameCount + 2;
            }
            catch { Dispose(); throw; }
        }

        internal bool Usable { get { return _root != null && _root.activeInHierarchy; } }

        internal void Refresh(ControllerRoutineModel model, bool paused, bool closing)
        {
            _settings.text = model.Settings;
            _status.text = closing ? "Release controls to continue."
                : model.Running ? "Running: " + model.ActiveRoutine + (paused ? " (waiting while the game is paused)" : "")
                : model.Summary;
            for (int i = 0; i < 6; i++)
            {
                _focus[i].color = i == model.Focus ? new Color(.44f, .28f, .10f, 1) : new Color(.16f, .14f, .12f, 1);
                _rows[i].text = (i == model.Focus ? "> " : "  ") + model.Row(i);
                _rows[i].color = model.Available(i) ? new Color(1, .94f, .80f, 1) : new Color(.62f, .59f, .54f, 1);
            }
            _confirm.text = model.ConfirmHint;
            _back.text = "Back to game";
            if (_details.text != model.Message)
            {
                _details.text = model.Message;
                Vector2 size = _details.rectTransform.sizeDelta;
                size.y = Math.Max(390, _details.GetPreferredValues(model.Message, 784, 0).y + 16);
                _details.rectTransform.sizeDelta = size;
                _scroll.verticalNormalizedPosition = 1;
            }
            if (model.ResultsOpen && !_detailsPanel.gameObject.activeSelf)
            { _detailsSettleFrame = Time.frameCount + 2; _detailsMeasured = false; }
            _detailsPanel.gameObject.SetActive(model.ResultsOpen);
        }

        internal void TurnPage(int direction)
        {
            _scroll.verticalNormalizedPosition = Mathf.Clamp01(_scroll.verticalNormalizedPosition - direction * .15f);
        }

        private void BuildResults(RectTransform panel, TMP_Text donor)
        {
            _detailsPanel = Rect(panel, "Results", Vector2.zero, Vector2.one);
            _detailsPanel.gameObject.AddComponent<Image>().color = new Color(.09f, .075f, .065f, 1);
            Label(_detailsPanel, donor, "ResultsTitle", "Results and explanations", 28, 22, 18, 836, 42);
            RectTransform viewport = Rect(_detailsPanel, "Viewport", new Vector2(0, 1), new Vector2(0, 1));
            Place(viewport, 22, 84, 816, 440);
            viewport.gameObject.AddComponent<Image>().color = new Color(.13f, .11f, .09f, 1);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.viewport = viewport;
            _details = Label(viewport, donor, "Details", "", 22, 12, 0, 784, 440, _log);
            _scroll.content = _details.rectTransform;
            RectTransform track = Rect(_detailsPanel, "ScrollTrack", new Vector2(0, 1), new Vector2(0, 1));
            Place(track, 846, 84, 12, 440);
            track.gameObject.AddComponent<Image>().color = new Color(.25f, .21f, .16f, 1);
            Scrollbar bar = track.gameObject.AddComponent<Scrollbar>();
            RectTransform handle = Rect(track, "Handle", Vector2.zero, Vector2.one);
            Image thumb = handle.gameObject.AddComponent<Image>();
            thumb.color = new Color(.75f, .57f, .30f, 1);
            bar.handleRect = handle;
            bar.targetGraphic = thumb;
            bar.direction = Scrollbar.Direction.BottomToTop;
            _scroll.verticalScrollbar = bar;
            Hint(_detailsPanel, donor, RewiredConsts.Action.DPadDown, "Scroll", 22, 570, 220);
            Hint(_detailsPanel, donor, RewiredConsts.Action.Decline, "Back to routines", 440, 570, 340);
            _detailsPanel.gameObject.SetActive(false);
        }

        internal void ObserveSettledGeometry()
        {
            if (!_detailsMeasured && _detailsPanel.gameObject.activeInHierarchy && Time.frameCount >= _detailsSettleFrame)
            {
                _detailsMeasured = true;
                _details.ForceMeshUpdate();
                Measure(_details, "settled-results", 784, _log);
            }
            if (_measured || Time.frameCount < _settleFrame) return;
            _measured = true;
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text label in new[] { _title, _rows[0], _status, _confirm })
            {
                label.ForceMeshUpdate();
                Measure(label, "settled", label == _rows[0] ? 812 : label == _confirm ? 330 : 836, _log);
            }
            _log("[KBP-CONTROLLER-LAYOUT] screen=" + Screen.width + "x" + Screen.height + ";settled=true;notPixelAcceptance=true.");
        }

        internal static GameObject EntryHint(Transform parent, TMP_Text donor)
        {
            RectTransform hint = Rect(parent, "KingmakerBuffPlanner.ControllerEntry", new Vector2(.5f, 1), new Vector2(.5f, 1));
            try
            {
                hint.pivot = new Vector2(.5f, 1);
                hint.anchoredPosition = new Vector2(0, -24);
                hint.sizeDelta = new Vector2(450, 48);
                hint.gameObject.AddComponent<Image>().color = new Color(.08f, .07f, .06f, .95f);
                Hint(hint, donor, RewiredConsts.Action.Func01, "Buff Planner — saved routines", 8, 6);
                return hint.gameObject;
            }
            catch
            {
                UnityEngine.Object.Destroy(hint.gameObject);
                throw;
            }
        }

        internal static GameObject BookHint(Transform parent, TMP_Text donor, int action)
        {
            RectTransform hint = Rect(parent, "KingmakerBuffPlanner.BookEntry", new Vector2(.5f, 0), new Vector2(.5f, 0));
            try
            {
                hint.pivot = new Vector2(.5f, 0);
                hint.anchoredPosition = new Vector2(0, 64);
                hint.sizeDelta = new Vector2(360, 44);
                hint.gameObject.AddComponent<Image>().color = new Color(.08f, .07f, .06f, .95f);
                Hint(hint, donor, action, "Buff Planner", 8, 4, 300);
                return hint.gameObject;
            }
            catch { UnityEngine.Object.Destroy(hint.gameObject); throw; }
        }

        private static TMP_Text Hint(Transform parent, TMP_Text donor, int action, string text, float x, float y, float width = 350)
        {
            RectTransform icon = Rect(parent, "HintIcon", new Vector2(0, 1), new Vector2(0, 1));
            Place(icon, x, y, 32, 32);
            Image image = icon.gameObject.AddComponent<Image>();
            if (Kingmaker.Assets.Console.GamepadInput.GamePad.Instance.SwapButtonsForJapanese)
            {
                if (action == RewiredConsts.Action.Confirm) action = RewiredConsts.Action.Decline;
                else if (action == RewiredConsts.Action.Decline) action = RewiredConsts.Action.Confirm;
            }
            image.sprite = GamePadIcons.Instance.GetIcon(action);
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (image.sprite == null) throw new InvalidOperationException("Native controller hint icon is unavailable for action " + action + ".");
            return Label(parent, donor, "HintLabel", text, 20, x + 40, y, width, 36);
        }

        private static TMP_Text Label(Transform parent, TMP_Text donor, string name, string text,
            float size, float x, float y, float width, float height, Action<string> log = null)
        {
            RectTransform rect = Rect(parent, name, new Vector2(0, 1), new Vector2(0, 1));
            rect.gameObject.SetActive(false);
            Place(rect, x, y, width, height);
            if (log != null) log("[KBP-CONTROLLER-LAYOUT] label=" + name + ";phase=before-component;rect=" + rect.rect.size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            Measure(label, "after-component", width, log);
            label.font = donor.font;
            // Font's base material, not a donor label's potentially masked
            // stencil/clipping material. Never mutate either shared material.
            label.fontSharedMaterial = donor.font.material;
            rect.gameObject.SetActive(true);
            Measure(label, "after-activation", width, log);
            label.autoSizeTextContainer = false;
            label.enableAutoSizing = false;
            label.margin = Vector4.zero;
            // Installed TMP Awake -> LoadDefaultSettings writes sizeDelta.
            // Establish final geometry ONCE after Awake, not every frame.
            Place(rect, x, y, width, height);
            label.fontSize = size;
            label.color = new Color(1, .94f, .80f, 1);
            label.richText = false;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Truncate;
            label.raycastTarget = false;
            label.text = text;
            Measure(label, "final-layout", width, log);
            return label;
        }

        private static void Measure(TMP_Text label, string phase, float intendedWidth, Action<string> log)
        {
            if (log == null) return;
            try
            {
            RectTransform rect = label.rectTransform;
            log("[KBP-CONTROLLER-LAYOUT] label=" + label.name + ";phase=" + phase +
                ";intendedWidth=" + intendedWidth + ";actual=" + rect.rect.size +
                ";anchors=" + rect.anchorMin + "/" + rect.anchorMax + ";pivot=" + rect.pivot +
                ";scale=" + rect.localScale + ";margin=" + label.margin +
                ";autoContainer=" + label.autoSizeTextContainer + ";autoFont=" + label.enableAutoSizing +
                ";preferred=" + label.preferredWidth + "/" + label.preferredHeight +
                ";bounds=" + label.textBounds.size + ";overflow=" + label.overflowMode +
                ";pages=" + (label.textInfo == null ? 0 : label.textInfo.pageCount) +
                ";layoutControllers=" + rect.GetComponents<Component>().OfType<ILayoutController>().Count() +
                ";mask=" + (rect.GetComponentInParent<Mask>() != null) +
                ";rectMask=" + (rect.GetComponentInParent<RectMask2D>() != null) +
                ";material=" + (label.fontSharedMaterial == null ? "none" : label.fontSharedMaterial.name) +
                ";materialClip=" + (label.fontSharedMaterial != null && label.fontSharedMaterial.HasProperty("_ClipRect")
                    ? label.fontSharedMaterial.GetVector("_ClipRect").ToString() : "none"));
            }
            catch (Exception exception)
            { log("[KBP-CONTROLLER-LAYOUT] phase=" + phase + ";diagnosticUnavailable=" + exception.GetType().Name); }
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        public void Dispose()
        {
            if (_root == null) return;
            _root.SetActive(false);
            UnityEngine.Object.Destroy(_root);
            _root = null;
        }
    }
}
