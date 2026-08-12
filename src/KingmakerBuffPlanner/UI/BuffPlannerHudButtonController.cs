using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.UI.Constructor;
using Kingmaker.UI.IngameMenu;
using Kingmaker.UI.Tooltip;
using KingmakerBuffPlanner.Persistence;
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
        private readonly Action _openSetup;
        private readonly Action<string> _quickExecute;
        private readonly List<Sprite> _ownedSprites = new List<Sprite>();
        private readonly List<Texture2D> _ownedTextures = new List<Texture2D>();
        private RectTransform _root;
        private Text _feedback;
        private Text _tooltip;
        private Button[] _buttons = new Button[0];
        private Func<string>[] _tooltips = new Func<string>[0];
        private int _listenerCount;
        private float _feedbackUntil;
        private IngameMenuController _anchorController;

        internal BuffPlannerHudButtonController(
            PlannerUiSession session,
            BuffPlannerUiLifecycleDiagnostics diagnostics,
            Action openSetup,
            Action<string> quickExecute)
        {
            _session = session ?? throw new ArgumentNullException("session");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
            _openSetup = openSetup ?? throw new ArgumentNullException("openSetup");
            _quickExecute = quickExecute ?? throw new ArgumentNullException("quickExecute");
        }

        internal bool IsInstalled { get { return _root != null; } }
        internal int ButtonCount { get { return _buttons.Count(button => button != null); } }
        internal int ListenerCount { get { return _listenerCount; } }
        internal string AnchorPath { get; private set; }

        internal bool TryInstall()
        {
            if (_root != null)
            {
                if (_anchorController != null && _root.parent != null) return true;
                DisposeOwnedRoot();
            }
            IngameMenuController controller = UnityEngine.Object.FindObjectOfType<IngameMenuController>();
            if (controller == null || !controller.gameObject.activeInHierarchy) return false;
            ButtonPF formation = ResolveFormationButton(controller);
            if (formation == null || formation.transform.parent == null) return false;
            RectTransform reference = formation.transform as RectTransform;
            RectTransform parent = formation.transform.parent as RectTransform;
            if (reference == null || parent == null) return false;

            Transform duplicate = parent.Find(RootName);
            if (duplicate != null) UnityEngine.Object.Destroy(duplicate.gameObject);
            _anchorController = controller;
            AnchorPath = GetPath(parent);
            _root = KingmakerUiFactory.CreateRect(RootName, parent);
            _root.anchorMin = reference.anchorMin;
            _root.anchorMax = reference.anchorMax;
            _root.pivot = reference.pivot;
            float width = Mathf.Max(42f, reference.rect.width);
            float height = Mathf.Max(42f, reference.rect.height);
            _root.anchoredPosition = reference.anchoredPosition + new Vector2(0, height + 8f);
            _root.sizeDelta = new Vector2(width * 4f + 18f, height);
            HorizontalLayoutGroup layout = _root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            PlannerUiTheme theme = PlannerUiTheme.Resolve(controller);
            _tooltip = CreateHudMessage("Tooltip", theme, height + 6f);
            _feedback = CreateHudMessage("Feedback", theme, height + 34f);
            _feedback.color = new Color(1f, 0.84f, 0.42f, 1f);
            _buttons = new[]
            {
                CreateNativeButton(formation, "Setup", "setup", width, height, _openSetup,
                    () => "Open Buff Planner setup. F10 is the fallback shortcut."),
                CreateNativeButton(formation, "Long", "long", width, height,
                    () => _quickExecute("long"), () => RoutineTooltip("long")),
                CreateNativeButton(formation, "Important", "important", width, height,
                    () => _quickExecute("important"), () => RoutineTooltip("important")),
                CreateNativeButton(formation, "Short", "short", width, height,
                    () => _quickExecute("short"), () => RoutineTooltip("short"))
            };
            _tooltips = new Func<string>[]
            {
                () => "Open Buff Planner setup. F10 is the fallback shortcut.",
                () => RoutineTooltip("long"),
                () => RoutineTooltip("important"),
                () => RoutineTooltip("short")
            };
            _root.SetAsLastSibling();
            _diagnostics.RecordHudInstalled();
            RefreshAvailability();
            return true;
        }

        internal void RefreshAvailability()
        {
            if (_buttons.Length != 4) return;
            bool ready = _session.Model != null && !_session.IsExecuting;
            _buttons[0].interactable = !_session.IsExecuting;
            for (int index = 1; index < 4; index++) _buttons[index].interactable = ready;
        }

        internal void Present(QuickExecutionResult result)
        {
            if (_feedback == null) return;
            _feedback.text = result == null ? "Buff routine finished." : result.Message;
            _feedback.transform.parent.gameObject.SetActive(true);
            _feedbackUntil = Time.unscaledTime + 8f;
            RefreshAvailability();
        }

        internal void Tick()
        {
            if (_feedback != null && _feedback.transform.parent.gameObject.activeSelf &&
                Time.unscaledTime >= _feedbackUntil)
                _feedback.transform.parent.gameObject.SetActive(false);
            RefreshAvailability();
        }

        public void Dispose()
        {
            DisposeOwnedRoot();
            foreach (Sprite sprite in _ownedSprites) if (sprite != null) UnityEngine.Object.Destroy(sprite);
            foreach (Texture2D texture in _ownedTextures) if (texture != null) UnityEngine.Object.Destroy(texture);
            _ownedSprites.Clear();
            _ownedTextures.Clear();
        }

        private void DisposeOwnedRoot()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
                _diagnostics.RecordHudDestroyed();
            }
            _root = null;
            _feedback = null;
            _tooltip = null;
            _buttons = new Button[0];
            _tooltips = new Func<string>[0];
            _listenerCount = 0;
            _anchorController = null;
            AnchorPath = string.Empty;
        }

        internal bool DispatchRuntimeClick(string routineId)
        {
            int index = routineId == "long" ? 1 : routineId == "important" ? 2 :
                routineId == "short" ? 3 : -1;
            if (index < 0 || index >= _buttons.Length || _buttons[index] == null ||
                EventSystem.current == null) return false;
            var down = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(_buttons[index].gameObject, down, ExecuteEvents.pointerDownHandler);
            var up = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(_buttons[index].gameObject, up, ExecuteEvents.pointerUpHandler);
            var click = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(_buttons[index].gameObject, click, ExecuteEvents.pointerClickHandler);
            return true;
        }

        internal string TooltipForRuntime(string routineId)
        {
            int index = routineId == "setup" ? 0 : routineId == "long" ? 1 :
                routineId == "important" ? 2 : routineId == "short" ? 3 : -1;
            return index < 0 || index >= _tooltips.Length || _tooltips[index] == null
                ? string.Empty : _tooltips[index]();
        }

        private Text CreateHudMessage(string name, PlannerUiTheme theme, float y)
        {
            RectTransform rect = KingmakerUiFactory.CreateRect(name, _root);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(620, 26);
            Image background = KingmakerUiFactory.AddPanel(rect,
                new Color(0.04f, 0.03f, 0.02f, 0.96f));
            background.raycastTarget = false;
            Text text = KingmakerUiFactory.CreateText("Label", rect, theme, string.Empty, 16,
                TextAnchor.MiddleLeft);
            KingmakerUiFactory.Stretch(text.rectTransform, 8, 8, 2, 2);
            rect.gameObject.SetActive(false);
            return text;
        }

        private Button CreateNativeButton(
            ButtonPF template,
            string displayName,
            string iconKind,
            float width,
            float height,
            Action action,
            Func<string> tooltip)
        {
            GameObject clone = UnityEngine.Object.Instantiate(template.gameObject);
            clone.name = "KBP." + displayName;
            clone.transform.SetParent(_root, false);
            clone.transform.localScale = Vector3.one;
            clone.SetActive(true);
            RectTransform rect = clone.transform as RectTransform;
            rect.sizeDelta = new Vector2(width, height);
            LayoutElement element = clone.GetComponent<LayoutElement>() ?? clone.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.minWidth = width;
            element.minHeight = height;

            foreach (TooltipTrigger trigger in clone.GetComponentsInChildren<TooltipTrigger>(true))
            {
                trigger.enabled = false;
                UnityEngine.Object.Destroy(trigger);
            }
            foreach (Text text in clone.GetComponentsInChildren<Text>(true)) text.gameObject.SetActive(false);
            ButtonPF nativeButton = clone.GetComponent<ButtonPF>();
            if (nativeButton != null)
            {
                nativeButton.OnRightClick = new Button.ButtonClickedEvent();
                nativeButton.OnEnter = new UnityEvent();
                nativeButton.OnExit = new UnityEvent();
                nativeButton.DisableWarningMessage = string.Empty;
            }
            Button button = nativeButton ?? clone.GetComponent<Button>();
            if (button == null) button = clone.AddComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => action());
            _listenerCount++;
            PlannerPointerSink sink = clone.GetComponent<PlannerPointerSink>() ??
                clone.AddComponent<PlannerPointerSink>();
            sink.Diagnostics = _diagnostics;
            sink.RoutineId = iconKind == "setup" ? string.Empty : iconKind;
            HudTooltipTarget hover = clone.GetComponent<HudTooltipTarget>() ??
                clone.AddComponent<HudTooltipTarget>();
            hover.Text = tooltip;
            hover.Show = ShowTooltip;

            RectTransform iconRect = KingmakerUiFactory.CreateRect("KBP.Icon", clone.transform);
            KingmakerUiFactory.Stretch(iconRect, 7, 7, 7, 7);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = CreateIcon(iconKind);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return button;
        }

        private void ShowTooltip(string value)
        {
            if (_tooltip == null) return;
            _tooltip.text = value ?? string.Empty;
            _tooltip.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(value));
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
            Color ink = new Color(0.96f, 0.82f, 0.42f, 1f);
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
            texture.Apply(false, true);
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

    internal sealed class HudTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal Func<string> Text;
        internal Action<string> Show;
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Show != null) Show(Text == null ? string.Empty : Text());
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            if (Show != null) Show(string.Empty);
        }
    }
}
