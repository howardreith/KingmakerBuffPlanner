using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using KingmakerBuffPlanner.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Reads the installed Unity UI's own per-control visual state and drives
    // its real pointer handlers. Members verified in Kingmaker 2.1.7b's
    // UnityEngine.UI.dll (Selectable): m_CurrentSelectionState (Normal,
    // Highlighted, Pressed, Disabled), isPointerInside, hasSelection. A
    // synthetic pointer goes through ExecuteEvents to the same handlers the
    // StandaloneInputModule calls, so what is read is what Unity would draw.
    // Runtime diagnostics only.
    internal static class PlannerHoverProbe
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo StateField = typeof(Selectable).GetField("m_CurrentSelectionState", Instance);
        private static readonly PropertyInfo PointerInsideProperty = typeof(Selectable).GetProperty("isPointerInside", Instance);
        private static readonly PropertyInfo HasSelectionProperty = typeof(Selectable).GetProperty("hasSelection", Instance);

        internal static bool Available
        {
            get { return StateField != null && PointerInsideProperty != null && HasSelectionProperty != null; }
        }

        internal static string StateOf(Selectable control)
        {
            if (control == null || StateField == null) return "missing";
            object value = StateField.GetValue(control);
            return value == null ? "missing" : value.ToString();
        }

        private static bool Flag(PropertyInfo property, Selectable control)
        {
            if (property == null || control == null) return false;
            object value = property.GetValue(control, null);
            return value is bool && (bool)value;
        }

        // A control draws its highlight only through a visible transition and
        // only while interactable: the installed Unity UI keeps a hovered
        // disabled control's stored state Highlighted but draws Disabled.
        internal static bool DrawsHighlight(Selectable control)
        {
            return control != null && control.transition != Selectable.Transition.None &&
                control.IsInteractable();
        }

        internal static IList<Selectable> Controls(GameObject root)
        {
            if (root == null) return new List<Selectable>();
            return root.GetComponentsInChildren<Selectable>(false)
                .Where(control => control != null && control.isActiveAndEnabled).ToList();
        }

        internal static Selectable Find(GameObject root, string name)
        {
            return Controls(root).FirstOrDefault(control =>
                string.Equals(control.name, name, StringComparison.Ordinal));
        }

        internal static bool IsUnder(GameObject candidate, IEnumerable<GameObject> roots)
        {
            if (candidate == null) return false;
            foreach (GameObject root in roots)
                if (root != null && candidate.transform.IsChildOf(root.transform)) return true;
            return false;
        }

        // Unity's own reading under the roots (inactive/destroyed-pending
        // controls excluded): which controls draw a highlight, which report
        // the pointer inside, and who holds the EventSystem selection.
        internal static HoverSample Sample(string label, string surface, string behaviour, bool physical,
            string aimed, string clicked, string expectedOwner, string topControl, bool? cursorInsideOwner,
            string detail, params GameObject[] roots)
        {
            var highlighted = new List<string>();
            var inside = new List<string>();
            foreach (GameObject root in roots)
                foreach (Selectable control in Controls(root))
                {
                    if (StateOf(control) == "Highlighted" && DrawsHighlight(control)) highlighted.Add(control.name);
                    if (Flag(PointerInsideProperty, control)) inside.Add(control.name);
                }
            GameObject selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            bool plannerSelection = selected != null && IsUnder(selected, roots) &&
                selected.GetComponent<InputField>() == null;
            return new HoverSample(label, surface, behaviour, physical, aimed, clicked, expectedOwner,
                topControl, highlighted, inside, selected == null ? null : selected.name, plannerSelection,
                cursorInsideOwner, detail);
        }

        // ------------------------------------------------------------------
        // Synthetic pointer (the handlers the input module would call)
        // ------------------------------------------------------------------

        private static PointerEventData Pointer(Selectable control)
        {
            var data = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left
            };
            Vector2? centre = ScreenCentre(control);
            if (centre.HasValue) data.position = centre.Value;
            return data;
        }

        internal static void Enter(Selectable control)
        {
            if (control == null) return;
            PointerEventData data = Pointer(control);
            data.pointerEnter = control.gameObject;
            ExecuteEvents.Execute(control.gameObject, data, ExecuteEvents.pointerEnterHandler);
        }

        internal static void Exit(Selectable control)
        {
            if (control == null) return;
            PointerEventData data = Pointer(control);
            ExecuteEvents.Execute(control.gameObject, data, ExecuteEvents.pointerExitHandler);
        }

        // A full click as the input module sends it: enter, down, up and
        // click on the same object, then exit. Returns whether the control
        // took the EventSystem selection on the press.
        internal static bool Click(Selectable control)
        {
            if (control == null) return false;
            Enter(control);
            PointerEventData data = Pointer(control);
            data.pointerEnter = control.gameObject;
            data.pointerPress = control.gameObject;
            data.rawPointerPress = control.gameObject;
            data.pressPosition = data.position;
            data.eligibleForClick = true;
            ExecuteEvents.Execute(control.gameObject, data, ExecuteEvents.pointerDownHandler);
            bool selected = EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == control.gameObject;
            ExecuteEvents.Execute(control.gameObject, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(control.gameObject, data, ExecuteEvents.pointerClickHandler);
            data.pointerPress = null;
            data.rawPointerPress = null;
            data.eligibleForClick = false;
            ExecuteEvents.Execute(control.gameObject, data, ExecuteEvents.pointerExitHandler);
            return selected;
        }

        // Holds a control pressed (no click is ever sent), for the pressed
        // state capture; Release lets it go without clicking.
        internal static void Press(Selectable control)
        {
            if (control == null) return;
            Enter(control);
            PointerEventData data = Pointer(control);
            data.pointerEnter = control.gameObject;
            data.pointerPress = control.gameObject;
            data.rawPointerPress = control.gameObject;
            ExecuteEvents.Execute(control.gameObject, data, ExecuteEvents.pointerDownHandler);
        }

        internal static void Release(Selectable control)
        {
            if (control == null) return;
            PointerEventData data = Pointer(control);
            data.pointerEnter = control.gameObject;
            data.pointerPress = control.gameObject;
            ExecuteEvents.Execute(control.gameObject, data, ExecuteEvents.pointerUpHandler);
            data.pointerPress = null;
            ExecuteEvents.Execute(control.gameObject, data, ExecuteEvents.pointerExitHandler);
        }

        internal static void ClearSelection()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        // ------------------------------------------------------------------
        // Screen geometry and raycasts
        // ------------------------------------------------------------------

        private static Camera CameraFor(Component component)
        {
            Canvas canvas = component == null ? null : component.GetComponentInParent<Canvas>();
            return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        internal static Rect? ScreenRect(Component component)
        {
            RectTransform rect = component == null ? null : component.transform as RectTransform;
            if (rect == null || !rect.gameObject.activeInHierarchy) return null;
            Camera camera = CameraFor(rect);
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 corner in corners)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        internal static Vector2? ScreenCentre(Component component)
        {
            RectTransform rect = component == null ? null : component.transform as RectTransform;
            if (rect == null || !rect.gameObject.activeInHierarchy) return null;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 centre = RectTransformUtility.WorldToScreenPoint(CameraFor(rect), (corners[0] + corners[2]) * 0.5f);
            if (centre.x < 1f || centre.y < 1f || centre.x > UnityEngine.Screen.width - 1 ||
                centre.y > UnityEngine.Screen.height - 1) return null;
            return centre;
        }

        // The centre when the pointer can actually reach it: on screen and
        // inside every clipping ancestor (a scroll view's viewport).
        internal static Vector2? VisibleCentre(Component component)
        {
            Vector2? centre = ScreenCentre(component);
            if (!centre.HasValue) return null;
            foreach (RectMask2D clip in component.GetComponentsInParent<RectMask2D>())
                if (clip.isActiveAndEnabled && !Contains(clip, centre.Value)) return null;
            foreach (Mask clip in component.GetComponentsInParent<Mask>())
                if (clip.isActiveAndEnabled && !Contains(clip, centre.Value)) return null;
            return centre;
        }

        // Where every drawn highlight other than the expected owner sits
        // relative to the cursor (Unity pixels, +y up): "name@dx,dy".
        internal static string Ghosts(string expectedOwner, Vector2 cursor, params GameObject[] roots)
        {
            var ghosts = new List<string>();
            foreach (GameObject root in roots)
                foreach (Selectable control in Controls(root))
                {
                    if (StateOf(control) != "Highlighted" || !DrawsHighlight(control) ||
                        string.Equals(control.name, expectedOwner, StringComparison.Ordinal)) continue;
                    Vector2? centre = ScreenCentre(control);
                    ghosts.Add(control.name + "@" + (centre.HasValue
                        ? (centre.Value.x - cursor.x).ToString("F0", CultureInfo.InvariantCulture) + "," +
                          (centre.Value.y - cursor.y).ToString("F0", CultureInfo.InvariantCulture)
                        : "offscreen"));
                }
            return ghosts.Count == 0 ? "none" : string.Join("|", ghosts.ToArray());
        }

        internal static bool Contains(Component component, Vector2 screenPoint)
        {
            RectTransform rect = component == null ? null : component.transform as RectTransform;
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint,
                CameraFor(rect));
        }

        // The control the input module would give the pointer at a screen
        // point: the nearest Selectable at or above the topmost raycast hit.
        internal static Selectable TopControlAt(Vector2 point, out bool anyHit)
        {
            anyHit = false;
            if (EventSystem.current == null) return null;
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            RaycastResult first = hits.FirstOrDefault(hit => hit.gameObject != null);
            if (first.gameObject == null) return null;
            anyHit = true;
            return first.gameObject.GetComponentInParent<Selectable>();
        }

        // A point over the planner (it blocks the world there) where no
        // control would take the pointer: where a sweep parks the cursor.
        internal static Vector2? NeutralPoint(GameObject root)
        {
            Rect? bounds = ScreenRect(root == null ? null : root.transform);
            if (!bounds.HasValue) return null;
            Rect area = bounds.Value;
            float[][] fractions =
            {
                new[] { 0.5f, 0.985f }, new[] { 0.35f, 0.985f }, new[] { 0.012f, 0.5f },
                new[] { 0.988f, 0.5f }, new[] { 0.5f, 0.012f }, new[] { 0.012f, 0.012f },
                new[] { 0.988f, 0.988f }, new[] { 0.25f, 0.5f }, new[] { 0.5f, 0.5f }
            };
            foreach (float[] fraction in fractions)
            {
                var point = new Vector2(area.xMin + area.width * fraction[0], area.yMin + area.height * fraction[1]);
                point.x = Mathf.Clamp(point.x, 2f, UnityEngine.Screen.width - 2f);
                point.y = Mathf.Clamp(point.y, 2f, UnityEngine.Screen.height - 2f);
                bool anyHit;
                Selectable top = TopControlAt(point, out anyHit);
                if (anyHit && top == null) return point;
            }
            return null;
        }

        // ------------------------------------------------------------------
        // Button-state capture
        // ------------------------------------------------------------------

        internal static ButtonStateObservation Observe(string state, Selectable control)
        {
            if (control == null)
                return new ButtonStateObservation(state, null, null, false, false, false, null, null);
            ColorBlock colors = control.colors;
            UiRgb selectedTint = PlannerButtonPalette.SelectedTint;
            bool selectedPalette = Near(colors.normalColor.r, selectedTint.R) &&
                Near(colors.normalColor.g, selectedTint.G) && Near(colors.normalColor.b, selectedTint.B);
            bool interactable = control.IsInteractable();
            string stored = StateOf(control);
            // The colour the transition draws for what Unity would show now.
            Color expected = !interactable ? colors.disabledColor
                : stored == "Pressed" ? colors.pressedColor
                : stored == "Highlighted" ? colors.highlightedColor
                : colors.normalColor;
            expected *= colors.colorMultiplier;
            Color tint = control.targetGraphic == null ? Color.clear
                : control.targetGraphic.canvasRenderer.GetColor();
            bool tintMatches = control.transition == Selectable.Transition.ColorTint &&
                control.targetGraphic != null && Near(tint.r, expected.r) && Near(tint.g, expected.g) &&
                Near(tint.b, expected.b) && Near(tint.a, expected.a);
            Rect? rect = ScreenRect(control);
            return new ButtonStateObservation(state, control.name, stored, interactable, selectedPalette,
                tintMatches, "#" + ColorUtility.ToHtmlStringRGBA(tint),
                rect.HasValue ? string.Format(CultureInfo.InvariantCulture, "{0:F0},{1:F0},{2:F0},{3:F0}",
                    rect.Value.x, rect.Value.y, rect.Value.width, rect.Value.height) : "offscreen");
        }

        private static bool Near(float a, float b)
        {
            return Mathf.Abs(a - b) < 0.02f;
        }

        internal static string Point(Vector2 point)
        {
            return point.x.ToString("F1", CultureInfo.InvariantCulture) + "," +
                point.y.ToString("F1", CultureInfo.InvariantCulture);
        }
    }
}
