using System;
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
        private TMP_Text _page;
        private TMP_Text _status;
        private readonly TMP_Text[] _rows = new TMP_Text[4];
        private readonly Image[] _focus = new Image[4];
        private int _pageNumber = 1;

        internal ControllerRoutineView(TMP_Text donor)
        {
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
                Label(panel, donor, "Title", "Buff Planner — saved routines", 28, 22, 18, 836, 42);
                _settings = Label(panel, donor, "Settings", "", 22, 22, 68, 836, 38);
                for (int i = 0; i < 4; i++)
                {
                    RectTransform row = Rect(panel, "Focus" + i, new Vector2(0, 1), new Vector2(0, 1));
                    Place(row, 22, 115 + i * 46, 836, 42);
                    _focus[i] = row.gameObject.AddComponent<Image>();
                    _rows[i] = Label(row, donor, "Caption", "", 24, 12, 3, 812, 36);
                }
                _status = Label(panel, donor, "Status", "", 21, 22, 305, 836, 32);
                _details = Label(panel, donor, "Details", "", 22, 22, 345, 836, 166);
                _details.overflowMode = TextOverflowModes.Page;
                _page = Label(panel, donor, "Page", "", 19, 22, 514, 836, 28);
                Label(panel, donor, "Setup", "Create/edit plans with mouse and keyboard. Run and Stop here.",
                    20, 22, 552, 836, 34);
                Hint(panel, donor, RewiredConsts.Action.DPadUp, "Select", 22, 602);
                Hint(panel, donor, RewiredConsts.Action.Confirm, "Run / Stop", 220, 602);
                Hint(panel, donor, RewiredConsts.Action.Decline, "Back", 470, 602);
                Hint(panel, donor, RewiredConsts.Action.DPadRight, "Details", 660, 602);
            }
            catch { Dispose(); throw; }
        }

        internal bool Usable { get { return _root != null && _root.activeInHierarchy; } }

        internal void Refresh(ControllerRoutineModel model, bool paused, bool closing)
        {
            _settings.text = model.Settings;
            _status.text = closing ? "Release controls to return to the game."
                : model.Running ? "Running: " + model.ActiveRoutine + (paused ? " (waiting while the game is paused)" : "")
                : "Focused: " + model.Row(model.Focus);
            for (int i = 0; i < 4; i++)
            {
                _focus[i].color = i == model.Focus ? new Color(.44f, .28f, .10f, 1) : new Color(.16f, .14f, .12f, 1);
                _rows[i].text = (i == model.Focus ? "> " : "  ") + model.Row(i);
            }
            if (_details.text != model.Message)
            {
                _details.text = model.Message;
                _pageNumber = 1;
            }
            UpdatePage();
        }

        internal void TurnPage(int direction)
        {
            _details.ForceMeshUpdate();
            _pageNumber = Mathf.Clamp(_pageNumber + direction, 1, Math.Max(1, _details.textInfo.pageCount));
            UpdatePage();
        }

        private void UpdatePage()
        {
            _details.ForceMeshUpdate();
            _details.pageToDisplay = _pageNumber;
            _page.text = "Details " + _pageNumber + " / " + Math.Max(1, _details.textInfo.pageCount) +
                " — D-pad Left / Right changes page; Up / Down selects an action.";
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

        private static void Hint(Transform parent, TMP_Text donor, int action, string text, float x, float y)
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
            Label(parent, donor, "HintLabel", text, 20, x + 40, y, text.StartsWith("Buff Planner", StringComparison.Ordinal) ? 350 : 182, 36);
        }

        private static TMP_Text Label(Transform parent, TMP_Text donor, string name, string text,
            float size, float x, float y, float width, float height)
        {
            RectTransform rect = Rect(parent, name, new Vector2(0, 1), new Vector2(0, 1));
            rect.gameObject.SetActive(false);
            Place(rect, x, y, width, height);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = donor.font;
            label.fontSharedMaterial = donor.fontSharedMaterial;
            rect.gameObject.SetActive(true);
            label.fontSize = size;
            label.color = new Color(1, .94f, .80f, 1);
            label.richText = false;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Truncate;
            label.raycastTarget = false;
            label.text = text;
            return label;
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
