using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.UI.Constructor;
using Kingmaker.UI.IngameMenu;
using KingmakerBuffPlanner.Infrastructure;
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
        private readonly ModLog _log;
        private readonly Action _openSetup;
        private readonly Action<string> _quickExecute;
        private readonly List<Sprite> _ownedSprites = new List<Sprite>();
        private readonly List<Texture2D> _ownedTextures = new List<Texture2D>();
        private RectTransform _root;
        private RectTransform _nativeCluster;
        private GraphicRaycaster _nativeRaycaster;
        private Text _feedback;
        private Text _tooltip;
        private Button[] _buttons = new Button[0];
        private Func<string>[] _tooltips = new Func<string>[0];
        private int _listenerCount;
        private float _feedbackUntil;
        private IngameMenuController _anchorController;
        private DeferredUiReadinessGate _readiness = new DeferredUiReadinessGate(2);
        private bool _installed;
        private int _validationFailures;
        private int _installAttempts;
        private string _lastFailure = "not-attempted";

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

        internal bool IsInstalled { get { return _installed && _root != null; } }
        internal int ButtonCount { get { return _buttons.Count(button => button != null); } }
        internal int ListenerCount { get { return _listenerCount; } }
        internal string AnchorPath { get; private set; }
        internal string RaycastCanvasPath { get; private set; }
        internal bool RowAboveNativeCluster { get; private set; }
        internal bool VisibleHitboxesOwnRaycasts { get; private set; }
        internal string ButtonOrder { get { return "Setup|Long|Important|Short"; } }
        internal int RuntimeUnderlyingNativeActivationCount { get; private set; }
        internal string LastFailure { get { return _lastFailure; } }
        internal int InstallAttempts { get { return _installAttempts; } }
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
                        ",corners=" + string.Join("|", corners.Select(value => value.ToString()).ToArray()));
                }
                return "root=" + RootInstanceId + ";host=" + AnchorPath +
                    ";raycaster=" + RaycastCanvasPath + ";" + string.Join(";", entries.ToArray());
            }
        }

        internal bool TryInstall()
        {
            if (_root != null)
            {
                if (_anchorController != null && _root.parent != null) return _installed;
                LogFailure("candidate-host-destroyed");
                DisposeOwnedRoot();
            }
            IngameMenuController controller = UnityEngine.Object.FindObjectOfType<IngameMenuController>();
            if (controller == null) return RejectHost("ingame-menu-controller-not-found");
            if (!controller.gameObject.activeInHierarchy)
                return RejectHost("ingame-menu-controller-inactive:" + GetPath(controller.transform));
            ButtonPF formation = ResolveFormationButton(controller);
            if (formation == null) return RejectHost("formation-button-field-null");
            if (formation.transform.parent == null) return RejectHost("formation-button-parent-null");
            RectTransform reference = formation.transform as RectTransform;
            RectTransform parent = formation.transform.parent as RectTransform;
            if (reference == null) return RejectHost("formation-button-not-rect-transform");
            if (parent == null) return RejectHost("formation-parent-not-rect-transform");
            GraphicRaycaster raycaster = parent.GetComponentInParent<GraphicRaycaster>();
            if (raycaster == null) return RejectHost("native-graphic-raycaster-not-found:" + GetPath(parent));
            if (!raycaster.isActiveAndEnabled)
                return RejectHost("native-graphic-raycaster-inactive:" + GetPath(raycaster.transform));
            if (EventSystem.current == null) return RejectHost("event-system-not-ready");
            if (parent.GetComponentsInParent<CanvasGroup>(true).Any(group =>
                group != null && (!group.interactable || !group.blocksRaycasts)))
                return RejectHost("native-canvas-group-blocked:" + GetPath(parent));

            Transform duplicate = parent.Find(RootName);
            if (duplicate != null) UnityEngine.Object.Destroy(duplicate.gameObject);
            _anchorController = controller;
            _nativeCluster = parent;
            _nativeRaycaster = raycaster;
            AnchorPath = GetPath(parent);
            RaycastCanvasPath = GetPath(raycaster.transform);
            _root = KingmakerUiFactory.CreateRect(RootName, parent);
            _root.anchorMin = new Vector2(0, 1);
            _root.anchorMax = new Vector2(0, 1);
            _root.pivot = new Vector2(0, 0);
            float width = Mathf.Max(42f, reference.rect.width);
            float height = Mathf.Max(42f, reference.rect.height);
            _root.anchoredPosition = new Vector2(0, 8f);
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
                CreatePlannerButton("Setup", "setup", width, height, _openSetup,
                    () => "Open Buff Planner setup. F10 is the fallback shortcut."),
                CreatePlannerButton("Long", "long", width, height,
                    () => _quickExecute("long"), () => RoutineTooltip("long")),
                CreatePlannerButton("Important", "important", width, height,
                    () => _quickExecute("important"), () => RoutineTooltip("important")),
                CreatePlannerButton("Short", "short", width, height,
                    () => _quickExecute("short"), () => RoutineTooltip("short"))
            };
            foreach (Button button in _buttons) button.interactable = false;
            _tooltips = new Func<string>[]
            {
                () => "Open Buff Planner setup. F10 is the fallback shortcut.",
                () => RoutineTooltip("long"),
                () => RoutineTooltip("important"),
                () => RoutineTooltip("short")
            };
            _root.SetAsLastSibling();
            _installed = false;
            _validationFailures = 0;
            _readiness.Reset();
            _installAttempts++;
            _lastFailure = "candidate-awaiting-deferred-readiness";
            _log.Info("[KBP-BOOT] HUD install attempted;attempt=" + _installAttempts +
                ";candidate=" + RootInstanceId + ";host=" + AnchorPath +
                ";raycastCanvas=" + RaycastCanvasPath + ";deferredFrames=2.");
            return false;
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
            if (_root != null && !_installed)
            {
                if (!_readiness.ObserveFrame()) return;
                Canvas.ForceUpdateCanvases();
                string rowFailure;
                string hitFailure;
                RowAboveNativeCluster = ValidateRowAboveCluster(out rowFailure);
                VisibleHitboxesOwnRaycasts = ValidateHitOwnership(out hitFailure);
                if (!RowAboveNativeCluster || !VisibleHitboxesOwnRaycasts)
                {
                    _validationFailures++;
                    LogFailure(!RowAboveNativeCluster ? rowFailure : hitFailure);
                    if (_validationFailures >= 120)
                    {
                        _log.Info("[KBP-BOOT] HUD candidate expired;candidate=" +
                            RootInstanceId + ";validationFrames=" + _validationFailures + ".");
                        DisposeOwnedRoot();
                    }
                    return;
                }
                _installed = true;
                _lastFailure = string.Empty;
                _diagnostics.RecordHudInstalled();
                RefreshAvailability();
                _log.Info("[KBP-BOOT] HUD install succeeded;attempt=" + _installAttempts +
                    ";candidate=" + RootInstanceId + ";host=" + AnchorPath +
                    ";buttons=" + ButtonCount + ";listeners=" + ListenerCount +
                    ";active=" + _root.gameObject.activeInHierarchy + ".");
            }
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
            _installed = false;
            _readiness.Reset();
            _validationFailures = 0;
            _feedback = null;
            _tooltip = null;
            _buttons = new Button[0];
            _tooltips = new Func<string>[0];
            _listenerCount = 0;
            _anchorController = null;
            _nativeCluster = null;
            _nativeRaycaster = null;
            AnchorPath = string.Empty;
            RaycastCanvasPath = string.Empty;
            RowAboveNativeCluster = false;
            VisibleHitboxesOwnRaycasts = false;
        }

        internal bool DispatchRuntimeClick(string routineId)
        {
            int index = routineId == "long" ? 1 : routineId == "important" ? 2 :
                routineId == "short" ? 3 : -1;
            if (index < 0 || index >= _buttons.Length || _buttons[index] == null ||
                EventSystem.current == null) return false;
            Button plannerButton = _buttons[index];
            Vector3[] corners = new Vector3[4];
            ((RectTransform)plannerButton.transform).GetWorldCorners(corners);
            Vector2 center = new Vector2((corners[0].x + corners[2].x) * 0.5f,
                (corners[0].y + corners[2].y) * 0.5f);
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

        private Button CreatePlannerButton(
            string displayName,
            string iconKind,
            float width,
            float height,
            Action action,
            Func<string> tooltip)
        {
            PlannerUiTheme theme = PlannerUiTheme.Resolve(_anchorController);
            Button button = KingmakerUiFactory.CreateButton("KBP." + displayName, _root,
                theme, string.Empty, null);
            RectTransform rect = button.transform as RectTransform;
            rect.sizeDelta = new Vector2(width, height);
            LayoutElement element = button.gameObject.GetComponent<LayoutElement>() ??
                button.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.minWidth = width;
            element.minHeight = height;

            foreach (Text text in button.GetComponentsInChildren<Text>(true))
                UnityEngine.Object.Destroy(text.gameObject);
            button.onClick.AddListener(() => action());
            _listenerCount++;
            PlannerPointerSink sink = button.gameObject.AddComponent<PlannerPointerSink>();
            sink.Diagnostics = _diagnostics;
            sink.RoutineId = iconKind == "setup" ? string.Empty : iconKind;
            HudTooltipTarget hover = button.gameObject.AddComponent<HudTooltipTarget>();
            hover.Text = tooltip;
            hover.Show = ShowTooltip;
            hover.Diagnostics = _diagnostics;
            hover.RoutineId = iconKind;

            RectTransform iconRect = KingmakerUiFactory.CreateRect("KBP.Icon", button.transform);
            KingmakerUiFactory.Stretch(iconRect, 7, 7, 7, 7);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = CreateIcon(iconKind);
            icon.preserveAspect = true;
            icon.raycastTarget = true;
            return button;
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
                Vector3[] corners = new Vector3[4];
                ((RectTransform)button.transform).GetWorldCorners(corners);
                var eventData = new PointerEventData(EventSystem.current)
                {
                    position = new Vector2((corners[0].x + corners[2].x) * 0.5f,
                        (corners[0].y + corners[2].y) * 0.5f)
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

        private bool RejectHost(string reason)
        {
            LogFailure(reason);
            return false;
        }

        private void LogFailure(string reason)
        {
            reason = string.IsNullOrEmpty(reason) ? "unknown" : reason;
            if (string.Equals(_lastFailure, reason, StringComparison.Ordinal) &&
                _validationFailures != 30 && _validationFailures != 120) return;
            _lastFailure = reason;
            _log.Info("[KBP-BOOT] HUD install failed;reason=" + reason +
                ";retryable=true;attempt=" + _installAttempts +
                ";candidate=" + RootInstanceId + ".");
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
        internal BuffPlannerUiLifecycleDiagnostics Diagnostics;
        internal string RoutineId;
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Diagnostics != null) Diagnostics.RecordPointerEnter(RoutineId);
            if (Show != null) Show(Text == null ? string.Empty : Text());
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            if (Show != null) Show(string.Empty);
        }
    }
}
