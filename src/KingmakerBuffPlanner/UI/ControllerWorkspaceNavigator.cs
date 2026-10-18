using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class ControllerControlIdentity : MonoBehaviour
    {
        internal string Key, Region;
        internal static void Set(Component control, string key, string region)
        {
            var identity = control.gameObject.AddComponent<ControllerControlIdentity>();
            identity.Key = key; identity.Region = region;
        }
    }

    // Thin Unity presentation bridge. All edits are the existing controls'
    // semantic callbacks; there is no pointer-coordinate or Unity Submit path.
    internal sealed class ControllerWorkspaceNavigator : IDisposable
    {
        private readonly CastingWorkspaceScreenView _workspace;
        private readonly PlannerNativeKit _kit;
        private readonly Transform _root;
        private readonly ControllerWorkspaceFocus _focus = new ControllerWorkspaceFocus();
        private readonly Dictionary<string, RectTransform> _rects = new Dictionary<string, RectTransform>();
        private RectTransform _outline;
        private TMP_Text _hint;
        private RectTransform _modal;
        private ScrollRect _reader;
        private ControllerSearchKeyboard _keyboard;
        private TMP_InputField _search;
        private TMP_Text _keyboardText;
        private TMP_Text _keyboardHints;
        private readonly List<Image> _keys = new List<Image>();
        private string _returnKey, _scope, _resourcesOrigin;
        private bool _disposed;
        private static readonly string[] Regions = { "Header", "Catalogue", "Casters", "Castings", "Recipients", "Inspector", "Footer", "Resources" };

        internal ControllerWorkspaceNavigator(CastingWorkspaceScreenView workspace, PlannerNativeKit kit)
        {
            _workspace = workspace; _kit = kit; _root = workspace.RootObject.transform;
            _hint = kit.Text(_root, "ControllerHints", "", PlannerTextRole.Body, 18, TextAlignmentOptions.MidlineLeft);
            KingmakerUiFactory.SetAnchors(_hint.rectTransform, 0, 0, 1, 0, 28, 28, 0, 0);
            _hint.rectTransform.sizeDelta = new Vector2(_hint.rectTransform.sizeDelta.x, 48);
            _hint.rectTransform.pivot = new Vector2(.5f, 0);
            _hint.rectTransform.anchoredPosition = new Vector2(0, 52);
            RectTransform glyphs = KingmakerUiFactory.CreateRect("MappedControllerHints", _root);
            KingmakerUiFactory.SetAnchors(glyphs, 0, 0, 1, 0);
            glyphs.pivot = new Vector2(.5f, 0); glyphs.sizeDelta = new Vector2(0, 48);
            ControllerRoutineView.Hint(glyphs, _hint, RewiredConsts.Action.Confirm, "Select", 20, 4, 150);
            ControllerRoutineView.Hint(glyphs, _hint, RewiredConsts.Action.Decline, "Back", 210, 4, 150);
            ControllerRoutineView.Hint(glyphs, _hint, RewiredConsts.Action.Func01, "Inspect", 400, 4, 150);
            ControllerRoutineView.Hint(glyphs, _hint, RewiredConsts.Action.LeftUp, "Routine", 590, 4, 150);
            ControllerRoutineView.Hint(glyphs, _hint, RewiredConsts.Action.RightUp, "Routine", 780, 4, 150);
            _outline = KingmakerUiFactory.CreateRect("ControllerFocus", _root);
            Image fill = _outline.gameObject.AddComponent<Image>();
            fill.color = new Color(.1f, .8f, 1f, .08f); fill.raycastTarget = false;
            Outline edge = _outline.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(.02f, .55f, .9f, 1); edge.effectDistance = new Vector2(3, -3);
            Rebuild();
        }

        internal string FocusKey { get { return _focus.Key; } }
        internal bool ModalOpen { get { return _modal != null; } }
        internal void BeforeRebuild()
        {
            if (_outline != null) { _outline.SetParent(_root, false); _outline.gameObject.SetActive(false); }
        }

        internal void Rebuild()
        {
            if (_disposed || _keyboard != null || _reader != null) return;
            Transform scope = _workspace.ControllerFocusScope;
            string scopeId = _workspace.ControllerResourcesOpen ? "Resources" : "Workspace";
            if (_scope != scopeId)
            {
                if (scopeId == "Resources") _resourcesOrigin = _focus.Key;
                _scope = scopeId;
            }
            var targets = new List<ControllerFocusItem>();
            _rects.Clear();
            foreach (Button button in scope.GetComponentsInChildren<Button>(false))
            {
                if (!button.gameObject.activeInHierarchy || button.name == "Blocker") continue;
                ControllerControlIdentity identity = button.GetComponent<ControllerControlIdentity>();
                string region = scopeId == "Resources" ? "Resources" : identity != null ? identity.Region : Region(button.transform);
                if (region == null) continue;
                string key = identity == null ? Path(button.transform, _root) : identity.Key;
                if (region == "Inspector") key = "inspection/" + _workspace.ControllerEditingKey + "/" + key;
                Button captured = button;
                string details = Details(button);
                _rects[key] = button.transform as RectTransform;
                var proxy = button.GetComponent<CastingWorkspaceScreenView.RightClickProxy>();
                targets.Add(new ControllerFocusItem(key, region, details, button.isActiveAndEnabled && button.IsInteractable(),
                    () =>
                    {
                        if (captured != null && captured.isActiveAndEnabled && captured.gameObject.activeInHierarchy && captured.IsInteractable()) captured.onClick.Invoke();
                        else ShowText("Unavailable", details);
                    }, null,
                    proxy != null && proxy.OnRightClick != null ? proxy.OnRightClick : (Action)(() => ShowText("Control details", details))));
            }
            foreach (TMP_InputField field in scope.GetComponentsInChildren<TMP_InputField>(false))
            {
                if (!field.gameObject.activeInHierarchy) continue;
                TMP_InputField captured = field;
                string key = Path(field.transform, _root);
                _rects[key] = field.transform as RectTransform;
                targets.Add(new ControllerFocusItem(key, "Catalogue", "Search buffs", field.IsInteractable(), () => OpenKeyboard(captured)));
            }
            _focus.Replace(targets.OrderBy(t => Array.IndexOf(Regions, t.Region)));
            if (scopeId == "Workspace" && _resourcesOrigin != null)
            { _focus.Restore(_resourcesOrigin); _resourcesOrigin = null; }
            DrawFocus(true);
        }

        private static string Region(Transform control)
        {
            for (Transform node = control; node != null; node = node.parent)
            {
                if (node.name.StartsWith("Caster.", StringComparison.Ordinal) || node.name.StartsWith("Provider.", StringComparison.Ordinal)) return "Casters";
                if (node.name.StartsWith("Casting.", StringComparison.Ordinal)) return "Castings";
                if (node.name.StartsWith("Target.", StringComparison.Ordinal)) return "Recipients";
                if (node.name == "Catalogue" || node.name == "Inspector" || node.name == "Footer") return node.name;
                if (node.name == "Header" || node.name == "RoutineBar") return "Header";
            }
            return null;
        }

        private static string Path(Transform node, Transform root)
        {
            var names = new List<string>();
            for (; node != null && node != root; node = node.parent) names.Add(Uri.EscapeDataString(node.name));
            names.Reverse(); return string.Join("/", names.ToArray());
        }

        private static string Details(Button button)
        {
            var lines = button.GetComponentsInChildren<TMP_Text>(false).Select(t => t.text).Where(t => !string.IsNullOrEmpty(t)).ToList();
            // Options' explanatory siblings remain available on disabled controls.
            Transform parent = button.transform.parent;
            if (parent != null && parent.name == "Content")
                for (int i = button.transform.GetSiblingIndex() + 1; i < parent.childCount; i++)
                {
                    Transform next = parent.GetChild(i);
                    if (!next.gameObject.activeSelf) continue;
                    if (next.GetComponent<Button>() != null || next.name.StartsWith("Section", StringComparison.Ordinal)) break;
                    TMP_Text text = next.GetComponent<TMP_Text>();
                    if (text != null && !string.IsNullOrEmpty(text.text)) lines.Add(text.text);
                }
            return lines.Count == 0 ? button.name : string.Join("\n", lines.ToArray());
        }

        internal void Execute(ControllerWorkspaceCommand command)
        {
            if (_disposed) return;
            if (_keyboard != null)
            {
                if (command == ControllerWorkspaceCommand.Up) _keyboard.Move(0, -1);
                if (command == ControllerWorkspaceCommand.Down) _keyboard.Move(0, 1);
                if (command == ControllerWorkspaceCommand.Left) _keyboard.Move(-1, 0);
                if (command == ControllerWorkspaceCommand.Right) _keyboard.Move(1, 0);
                if (command == ControllerWorkspaceCommand.Confirm) _keyboard.Confirm();
                if (_keyboard.Finished) FinishKeyboard(); else DrawKeyboard();
                return;
            }
            if (_reader != null)
            {
                if (command == ControllerWorkspaceCommand.Up || command == ControllerWorkspaceCommand.ScrollUp) Scroll(_reader, -1);
                if (command == ControllerWorkspaceCommand.Down || command == ControllerWorkspaceCommand.ScrollDown) Scroll(_reader, 1);
                if (command == ControllerWorkspaceCommand.Confirm) CloseOwnedModal();
                return;
            }
            if (command == ControllerWorkspaceCommand.PreviousRoutine || command == ControllerWorkspaceCommand.NextRoutine)
            { if (!_workspace.ControllerResourcesOpen) _workspace.ControllerCycleRoutine(command == ControllerWorkspaceCommand.PreviousRoutine ? -1 : 1); return; }
            if (command == ControllerWorkspaceCommand.Up || command == ControllerWorkspaceCommand.Down)
                _focus.Move(command == ControllerWorkspaceCommand.Up ? -1 : 1, false);
            if (command == ControllerWorkspaceCommand.Left || command == ControllerWorkspaceCommand.Right)
                _focus.Move(command == ControllerWorkspaceCommand.Left ? -1 : 1, true);
            if (command == ControllerWorkspaceCommand.Confirm)
            {
                string unavailable = _focus.Confirm();
                if (unavailable != null) ShowText("Unavailable", unavailable);
                Rebuild(); return;
            }
            if (command == ControllerWorkspaceCommand.Inspect)
            {
                ControllerFocusItem focused = _focus.Current;
                if (focused != null && focused.Inspect != null) focused.Inspect();
                return;
            }
            if (command == ControllerWorkspaceCommand.ScrollUp || command == ControllerWorkspaceCommand.ScrollDown)
            {
                ScrollRect scroll = _workspace.ControllerResourcesOpen ? _workspace.ControllerResourcesScroll : FocusedScroll();
                Scroll(scroll, command == ControllerWorkspaceCommand.ScrollUp ? -1 : 1); return;
            }
            DrawFocus(true);
        }

        private ScrollRect FocusedScroll()
        {
            RectTransform rect;
            return _focus.Key != null && _rects.TryGetValue(_focus.Key, out rect) && rect != null ? rect.GetComponentInParent<ScrollRect>() : null;
        }

        private static void Scroll(ScrollRect scroll, int direction)
        {
            if (scroll == null || scroll.viewport == null || scroll.content == null) return;
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = ControllerResultScroll.Next(scroll.verticalNormalizedPosition,
                scroll.content.rect.height, scroll.viewport.rect.height, direction);
        }

        private void DrawFocus(bool reveal)
        {
            RectTransform rect;
            if (_focus.Key == null || !_rects.TryGetValue(_focus.Key, out rect) || rect == null)
            { _outline.gameObject.SetActive(false); return; }
            _outline.gameObject.SetActive(true); _outline.SetParent(rect, false);
            KingmakerUiFactory.Stretch(_outline, -3, -3, -3, -3); _outline.SetAsLastSibling();
            ControllerFocusItem current = _focus.Current;
            string caption = current == null ? "" : current.Caption.Split('\n')[0];
            _hint.text = (current == null ? "" : current.Region + " > ") + caption +
                "\nD-pad Up/Down: focus; Left/Right: region. Right stick: scroll. Back leaves one layer.";
            if (reveal) Reveal(rect);
        }

        private static void Reveal(RectTransform target)
        {
            ScrollRect scroll = target.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.content == null || scroll.viewport == null) return;
            Canvas.ForceUpdateCanvases();
            Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, target);
            Rect v = scroll.viewport.rect;
            float dy = b.max.y > v.yMax ? b.max.y - v.yMax : b.min.y < v.yMin ? b.min.y - v.yMin : 0;
            Vector2 p = scroll.content.anchoredPosition; p.y -= dy; scroll.content.anchoredPosition = p;
        }

        private RectTransform CreateModal(string title)
        {
            _returnKey = _focus.Key;
            _outline.SetParent(_root, false); _outline.gameObject.SetActive(false);
            RectTransform backdrop = KingmakerUiFactory.CreateRect("ControllerModal", _root);
            KingmakerUiFactory.Stretch(backdrop);
            KingmakerUiFactory.AddPanel(backdrop, new Color(0, 0, 0, .75f));
            RectTransform panel = KingmakerUiFactory.CreateRect("Paper", backdrop);
            KingmakerUiFactory.SetAnchors(panel, .12f, .12f, .88f, .88f);
            KingmakerUiFactory.AddPanel(panel, new Color(.94f, .89f, .77f, 1));
            _kit.Frame(panel, "common_frame", Color.white);
            TMP_Text heading = _kit.Text(panel, "Title", title, PlannerTextRole.Title, 26);
            KingmakerUiFactory.SetAnchors(heading.rectTransform, 0, 1, 1, 1, 24, 24, 0, 0);
            heading.rectTransform.pivot = new Vector2(.5f, 1); heading.rectTransform.sizeDelta = new Vector2(heading.rectTransform.sizeDelta.x, 48);
            _modal = backdrop; backdrop.SetAsLastSibling();
            return panel;
        }

        internal void ShowText(string title, string text)
        {
            if (_disposed || _modal != null) return;
            RectTransform panel = CreateModal(title);
            RectTransform viewport = KingmakerUiFactory.CreateRect("Viewport", panel);
            KingmakerUiFactory.Stretch(viewport, 24, 24, 68, 62);
            viewport.gameObject.AddComponent<RectMask2D>();
            _reader = viewport.gameObject.AddComponent<ScrollRect>();
            _reader.horizontal = false; _reader.movementType = ScrollRect.MovementType.Clamped;
            _reader.viewport = viewport;
            TMP_Text body = _kit.Text(viewport, "Body", text ?? "No details available.", PlannerTextRole.Body, 20);
            RectTransform content = body.rectTransform;
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            content.sizeDelta = new Vector2(0, Math.Max(viewport.rect.height, body.GetPreferredValues(body.text, viewport.rect.width, 0).y + 16));
            _reader.content = content; _reader.verticalNormalizedPosition = 1;
            TMP_Text footer = _kit.Text(panel, "Hints", "Up/Down or right stick: scroll    Confirm / Back: close details", PlannerTextRole.Body, 18);
            KingmakerUiFactory.SetAnchors(footer.rectTransform, 0, 0, 1, 0, 24, 24, 0, 0);
            footer.rectTransform.pivot = new Vector2(.5f, 0); footer.rectTransform.sizeDelta = new Vector2(footer.rectTransform.sizeDelta.x, 48);
        }

        private void OpenKeyboard(TMP_InputField field)
        {
            if (_modal != null) return;
            // Optional pointer typing must not remain an underlying focused
            // field while controller Back belongs to this keyboard modal.
            if (field.isFocused) field.DeactivateInputField();
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            _search = field; _keyboard = new ControllerSearchKeyboard(field.text);
            RectTransform panel = CreateModal("Search buffs — controller keyboard");
            _keyboardText = _kit.Text(panel, "Draft", _keyboard.Draft, PlannerTextRole.Strong, 24);
            KingmakerUiFactory.SetAnchors(_keyboardText.rectTransform, 0, .78f, 1, .88f, 24, 24, 0, 0);
            _keyboardHints = _kit.Text(panel, "Hints", "", PlannerTextRole.Body, 18);
            KingmakerUiFactory.SetAnchors(_keyboardHints.rectTransform, 0, 0, 1, .10f, 24, 24, 0, 0);
            for (int index = 0; index < ControllerSearchKeyboard.Keys.Length; index++)
            {
                RectTransform cell = KingmakerUiFactory.CreateRect("Key." + ControllerSearchKeyboard.Keys[index], panel);
                int col = index % ControllerSearchKeyboard.Columns, row = index / ControllerSearchKeyboard.Columns;
                KingmakerUiFactory.SetAnchors(cell, .04f + col * .115f, .65f - row * .105f, .145f + col * .115f, .74f - row * .105f);
                Image fill = KingmakerUiFactory.AddPanel(cell, Color.white); _keys.Add(fill);
                TMP_Text caption = _kit.Text(cell, "Caption", ControllerSearchKeyboard.Keys[index], PlannerTextRole.Body, 20, TextAlignmentOptions.Center);
                KingmakerUiFactory.Stretch(caption.rectTransform, 2, 2, 2, 2);
            }
            DrawKeyboard();
        }

        private void DrawKeyboard()
        {
            _keyboardText.text = _keyboard.Draft + "_";
            for (int i = 0; i < _keys.Count; i++) _keys[i].color = i == _keyboard.Focus ? new Color(.45f, .75f, 1, 1) : new Color(.84f, .77f, .61f, 1);
            _hint.text = "D-pad: character   Confirm: " + ControllerSearchKeyboard.Keys[_keyboard.Focus] + "   Back: cancel search (keep previous text)";
            _keyboardHints.text = _hint.text;
        }

        private void FinishKeyboard()
        {
            bool accept = _keyboard.Accepted; string draft = _keyboard.Draft; TMP_InputField field = _search;
            CloseOwnedModal();
            if (accept && field != null) field.text = draft;
            Rebuild();
        }

        internal bool CloseOwnedModal()
        {
            if (_modal == null) return false;
            _modal.gameObject.SetActive(false); UnityEngine.Object.Destroy(_modal.gameObject);
            _modal = null; _reader = null; _keyboard = null; _search = null; _keys.Clear();
            Rebuild(); _focus.Restore(_returnKey); _returnKey = null; DrawFocus(true);
            return true;
        }

        public void Dispose()
        { _disposed = true; if (_modal != null) UnityEngine.Object.Destroy(_modal.gameObject); if (_outline != null) UnityEngine.Object.Destroy(_outline.gameObject); }
    }
}
