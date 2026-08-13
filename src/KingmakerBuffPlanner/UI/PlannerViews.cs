using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class PlannerRoutineTabsView
    {
        private readonly PlannerUiTheme _theme;
        private readonly Button[] _buttons;
        private readonly string[] _ids = { "long", "important", "short" };

        internal PlannerRoutineTabsView(RectTransform parent, PlannerUiTheme theme,
            Action<string> selected)
        {
            _theme = theme;
            RectTransform root = KingmakerUiFactory.CreateRect("RoutineTabs", parent);
            KingmakerUiFactory.SetAnchors(root, 0.12f, 0.865f, 0.88f, 0.925f);
            HorizontalLayoutGroup layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            _buttons = _ids.Select(id => KingmakerUiFactory.CreateButton("Routine." + id,
                root, theme, id.ToUpperInvariant(), () => selected(id))).ToArray();
        }

        internal Button Button(string id)
        {
            int index = Array.IndexOf(_ids, id);
            return index < 0 ? null : _buttons[index];
        }

        internal void Bind(string activeId, Func<string, RoutineSummaryViewModel> summary,
            bool interactable)
        {
            for (int index = 0; index < _buttons.Length; index++)
            {
                Button button = _buttons[index];
                button.interactable = interactable;
                Image image = button.targetGraphic as Image;
                if (image != null) image.color = _ids[index] == activeId
                    ? _theme.BurgundyPrimary : _theme.ParchmentRaised;
                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null) label.text = summary(_ids[index]).Label;
            }
        }
    }

    internal sealed class PlannerCategoryTabsView
    {
        private readonly PlannerUiTheme _theme;
        private readonly Dictionary<PlannerSourceCategory, Button> _buttons =
            new Dictionary<PlannerSourceCategory, Button>();
        private readonly Text _selectedOnlyLabel;
        private readonly Button _selectedOnly;

        internal PlannerCategoryTabsView(RectTransform parent, PlannerUiTheme theme,
            Action<PlannerSourceCategory> selectCategory, Action toggleSelectedOnly,
            Action<string> showTooltip)
        {
            _theme = theme;
            RectTransform root = KingmakerUiFactory.CreateRect("CatalogControls", parent);
            KingmakerUiFactory.Stretch(root);
            HorizontalLayoutGroup layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 7f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            foreach (PlannerSourceCategory category in Enum.GetValues(typeof(PlannerSourceCategory)))
            {
                Button button = KingmakerUiFactory.CreateButton("Category." + category, root,
                    theme, category.ToString(), () => selectCategory(category));
                button.gameObject.AddComponent<LayoutElement>().preferredWidth = 118f;
                _buttons.Add(category, button);
            }
            _selectedOnly = KingmakerUiFactory.CreateButton("SelectedOnly", root, theme,
                "Selected only", () => toggleSelectedOnly());
            _selectedOnly.gameObject.AddComponent<LayoutElement>().preferredWidth = 180f;
            _selectedOnlyLabel = _selectedOnly.GetComponentInChildren<Text>(true);
            PlannerHoverTooltip hover = _selectedOnly.gameObject.AddComponent<PlannerHoverTooltip>();
            hover.Text = "Show buffs with one or more selected targets in the active routine.";
            hover.Show = showTooltip;
        }

        internal void Bind(PlannerSourceCategory selected, bool selectedOnly, bool interactable)
        {
            foreach (KeyValuePair<PlannerSourceCategory, Button> pair in _buttons)
            {
                pair.Value.interactable = interactable;
                Image image = pair.Value.targetGraphic as Image;
                if (image != null) image.color = pair.Key == selected
                    ? _theme.BurgundyPrimary : _theme.ParchmentRaised;
            }
            _selectedOnly.interactable = interactable;
            _selectedOnlyLabel.text = (selectedOnly ? "✓ " : string.Empty) + "Selected only";
            Image selectedImage = _selectedOnly.targetGraphic as Image;
            if (selectedImage != null) selectedImage.color = selectedOnly
                ? _theme.BurgundyPrimary : _theme.ParchmentRaised;
        }

        internal Button Button(PlannerSourceCategory category) { return _buttons[category]; }
        internal Button SelectedOnlyButton { get { return _selectedOnly; } }
    }

    internal sealed class BuffCardView
    {
        private readonly PlannerUiTheme _theme;
        private readonly Image _background;
        private readonly Image _status;
        private readonly Image _icon;
        private readonly Text _fallback;
        private readonly Text _name;
        private readonly Text _availability;
        private readonly Text _configuration;
        private readonly Text _badge;
        private UnityAction _select;

        internal BuffCardView(RectTransform parent, PlannerUiTheme theme)
        {
            _theme = theme;
            Rect = KingmakerUiFactory.CreateRect("BuffCard", parent);
            _background = KingmakerUiFactory.AddFramedPanel(Rect, theme.ParchmentRaised,
                theme.MutedBrownText);
            Button = Rect.gameObject.AddComponent<Button>();
            Button.targetGraphic = _background;
            RectTransform stripe = KingmakerUiFactory.CreateRect("StatusStripe", Rect);
            KingmakerUiFactory.SetAnchors(stripe, 0, 0, 0.018f, 1, 1, 0, 3, 3);
            _status = KingmakerUiFactory.AddPanel(stripe, theme.MutedBrownText);
            _status.raycastTarget = false;
            RectTransform frame = KingmakerUiFactory.CreateRect("IconFrame", Rect);
            KingmakerUiFactory.SetAnchors(frame, 0.035f, 0.12f, 0.23f, 0.88f);
            Image frameImage = KingmakerUiFactory.AddFramedPanel(frame,
                new Color(0.16f, 0.10f, 0.07f, 1f), theme.GoldAccent);
            frameImage.raycastTarget = false;
            RectTransform iconRect = KingmakerUiFactory.CreateRect("AbilityIcon", frame);
            KingmakerUiFactory.Stretch(iconRect, 4, 4, 4, 4);
            _icon = iconRect.gameObject.AddComponent<Image>();
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
            _fallback = KingmakerUiFactory.CreateText("MissingIcon", frame, theme, "?", 28,
                TextAnchor.MiddleCenter);
            _fallback.color = theme.MutedBrownText;
            KingmakerUiFactory.Stretch(_fallback.rectTransform);
            _name = KingmakerUiFactory.CreateText("Name", Rect, theme, string.Empty, 17,
                TextAnchor.MiddleLeft);
            _name.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.SetAnchors(_name.rectTransform, 0.25f, 0.55f, 0.79f, 0.92f);
            _badge = KingmakerUiFactory.CreateText("RoutineBadge", Rect, theme, string.Empty, 14,
                TextAnchor.MiddleRight);
            _badge.color = theme.BurgundyPrimary;
            KingmakerUiFactory.SetAnchors(_badge.rectTransform, 0.80f, 0.57f, 0.965f, 0.92f);
            _availability = KingmakerUiFactory.CreateText("Availability", Rect, theme,
                string.Empty, 14, TextAnchor.MiddleLeft);
            _availability.color = theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(_availability.rectTransform, 0.25f, 0.28f, 0.96f, 0.55f);
            _configuration = KingmakerUiFactory.CreateText("Configuration", Rect, theme,
                string.Empty, 13, TextAnchor.MiddleLeft);
            KingmakerUiFactory.SetAnchors(_configuration.rectTransform, 0.25f, 0.06f, 0.96f, 0.30f);
        }

        internal RectTransform Rect { get; private set; }
        internal Button Button { get; private set; }
        internal string SourceId { get; private set; }

        internal void Bind(BuffCardViewModel model, Sprite icon, UnityAction selected,
            Func<PlannerPresentationStatus, Color> statusColor)
        {
            SourceId = model.SourceId;
            Rect.name = "Source." + model.SourceId;
            _name.text = model.Name;
            _badge.text = model.RoutineBadge;
            _availability.text = model.Availability;
            _configuration.text = model.Configuration;
            Color color = statusColor(model.Status);
            _status.color = color;
            _configuration.color = color;
            _background.color = model.Selected ? new Color(0.94f, 0.78f, 0.52f, 0.96f)
                : _theme.ParchmentRaised;
            Outline outline = Rect.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = model.Selected ? _theme.GoldAccent : _theme.MutedBrownText;
                outline.effectDistance = model.Selected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            }
            _icon.sprite = icon;
            _icon.gameObject.SetActive(icon != null);
            _fallback.gameObject.SetActive(icon == null);
            if (_select != null) Button.onClick.RemoveListener(_select);
            _select = selected;
            Button.onClick.AddListener(_select);
            Rect.gameObject.SetActive(true);
        }

        internal void Hide()
        {
            SourceId = string.Empty;
            Rect.gameObject.SetActive(false);
        }
    }

    internal sealed class BuffCardPool
    {
        private readonly List<BuffCardView> _cards;

        internal BuffCardPool(RectTransform parent, PlannerUiTheme theme, int capacity)
        {
            _cards = Enumerable.Range(0, capacity)
                .Select(ignored => new BuffCardView(parent, theme)).ToList();
        }

        internal int Capacity { get { return _cards.Count; } }
        internal IReadOnlyList<BuffCardView> Cards { get { return _cards; } }
        internal BuffCardView this[int index] { get { return _cards[index]; } }
        internal void HideAll() { foreach (BuffCardView card in _cards) card.Hide(); }
    }

    internal sealed class BuffCardGridScrollSink : MonoBehaviour,
        UnityEngine.EventSystems.IScrollHandler
    {
        internal ScrollRect Scroll;
        internal BuffPlannerUiLifecycleDiagnostics Diagnostics;

        public void OnScroll(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (Scroll != null)
            {
                Vector2 position = Scroll.content.anchoredPosition;
                position.y = Mathf.Max(0, position.y + eventData.scrollDelta.y * -32f);
                Scroll.content.anchoredPosition = position;
            }
            if (Diagnostics != null) Diagnostics.RecordScroll();
            eventData.Use();
        }
    }

    internal sealed class BuffGridView
    {
        private readonly ScrollRect _scroll;
        private readonly RectTransform _content;
        private readonly RectTransform _viewport;
        private readonly BuffCardPool _pool;
        private readonly Func<string, Sprite> _icon;
        private readonly Action<string> _select;
        private readonly Func<PlannerPresentationStatus, Color> _statusColor;
        private IReadOnlyList<BuffCardViewModel> _models = new BuffCardViewModel[0];
        private BuffGridMetrics _metrics;
        private int _firstRow = -1;

        internal BuffGridView(RectTransform parent, PlannerUiTheme theme,
            Func<string, Sprite> icon, Action<string> select,
            Func<PlannerPresentationStatus, Color> statusColor)
        {
            _icon = icon;
            _select = select;
            _statusColor = statusColor;
            _scroll = KingmakerUiFactory.CreateScrollView("BuffGrid", parent, theme, out _content);
            KingmakerUiFactory.SetAnchors((RectTransform)_scroll.transform, 0.02f, 0.315f,
                0.98f, 0.795f);
            _viewport = _scroll.viewport;
            VerticalLayoutGroup oldLayout = _content.GetComponent<VerticalLayoutGroup>();
            if (oldLayout != null)
            {
                oldLayout.enabled = false;
                UnityEngine.Object.Destroy(oldLayout);
            }
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1);
            _pool = new BuffCardPool(_content, theme, BuffGridMetrics.PoolCapacity);
            _scroll.onValueChanged.AddListener(ignored => BindVisible(false));
        }

        internal ScrollRect Scroll { get { return _scroll; } }
        internal RectTransform Content { get { return _content; } }
        internal RectTransform Viewport { get { return _viewport; } }
        internal IReadOnlyList<BuffCardView> Cards { get { return _pool.Cards; } }
        internal BuffGridMetrics Metrics { get { return _metrics; } }

        internal void Bind(IReadOnlyList<BuffCardViewModel> models, bool preserveScroll)
        {
            Vector2 previous = _content.anchoredPosition;
            _models = models ?? new BuffCardViewModel[0];
            Canvas.ForceUpdateCanvases();
            float width = Mathf.Max(920f, _viewport.rect.width);
            float height = Mathf.Max(360f, _viewport.rect.height);
            _metrics = BuffGridMetrics.Calculate(width, height);
            int rows = BuffGridMetrics.RowCount(_models.Count);
            float contentHeight = Mathf.Max(height, 12f + rows * (_metrics.CellHeight + 10f));
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            _content.anchoredPosition = preserveScroll
                ? new Vector2(0, Mathf.Clamp(previous.y, 0, Mathf.Max(0, contentHeight - height)))
                : Vector2.zero;
            _firstRow = -1;
            BindVisible(true);
        }

        internal bool SelectForRuntime(string sourceId)
        {
            BuffCardView visible = _pool.Cards.FirstOrDefault(card => card.SourceId == sourceId);
            if (visible != null) { visible.Button.onClick.Invoke(); return true; }
            int index = -1;
            for (int i = 0; i < _models.Count; i++)
                if (_models[i].SourceId == sourceId) { index = i; break; }
            if (index < 0) return false;
            int row = index / BuffGridMetrics.ColumnCount;
            _content.anchoredPosition = new Vector2(0, row * (_metrics.CellHeight + 10f));
            _firstRow = -1;
            BindVisible(true);
            visible = _pool.Cards.FirstOrDefault(card => card.SourceId == sourceId);
            if (visible == null) return false;
            visible.Button.onClick.Invoke();
            return true;
        }

        private void BindVisible(bool force)
        {
            if (_metrics == null) return;
            int row = Mathf.Max(0, Mathf.FloorToInt(_content.anchoredPosition.y /
                (_metrics.CellHeight + 10f)) - 1);
            if (!force && row == _firstRow) return;
            _firstRow = row;
            _pool.HideAll();
            float spacing = _metrics.HorizontalSpacing;
            for (int poolIndex = 0; poolIndex < _pool.Capacity; poolIndex++)
            {
                int modelIndex = BuffGridMetrics.ModelIndex(row, poolIndex);
                if (modelIndex >= _models.Count) break;
                int absoluteRow = modelIndex / BuffGridMetrics.ColumnCount;
                int column = modelIndex % BuffGridMetrics.ColumnCount;
                BuffCardView card = _pool[poolIndex];
                card.Rect.anchorMin = new Vector2(0, 1);
                card.Rect.anchorMax = new Vector2(0, 1);
                card.Rect.pivot = new Vector2(0, 1);
                card.Rect.sizeDelta = new Vector2(_metrics.CellWidth, _metrics.CellHeight);
                card.Rect.anchoredPosition = new Vector2(_metrics.SideInset + column *
                    (_metrics.CellWidth + spacing), -6f - absoluteRow *
                    (_metrics.CellHeight + spacing));
                BuffCardViewModel model = _models[modelIndex];
                card.Bind(model, _icon(model.SourceId), () => _select(model.SourceId), _statusColor);
            }
        }
    }

    internal sealed class PlannerTargetStripView
    {
        private readonly RectTransform _root;
        private readonly PlannerUiTheme _theme;
        private readonly Dictionary<string, Button> _buttons =
            new Dictionary<string, Button>(StringComparer.Ordinal);

        internal PlannerTargetStripView(RectTransform parent, PlannerUiTheme theme)
        {
            _theme = theme;
            _root = KingmakerUiFactory.CreateRect("TargetPortraits", parent);
            HorizontalLayoutGroup layout = _root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        internal RectTransform Root { get { return _root; } }

        internal void Bind(IReadOnlyList<TargetPortraitViewModel> targets,
            Func<string, Sprite> portrait, Action<string> toggle,
            Func<PlannerPresentationStatus, Color> statusColor)
        {
            KingmakerUiFactory.DestroyChildren(_root);
            _buttons.Clear();
            foreach (TargetPortraitViewModel target in targets)
            {
                RectTransform rect = KingmakerUiFactory.CreateRect("Target." + target.UnitId, _root);
                LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = 78f;
                element.minWidth = 78f;
                Color frame = statusColor(target.Status);
                Color surface = new Color(0.16f, 0.10f, 0.07f, 1f);
                float thickness = 1f;
                if (target.State == TargetPortraitState.DirectSelected)
                {
                    surface = new Color(0.12f, 0.31f, 0.15f, 1f);
                    frame = new Color(0.38f, 0.88f, 0.43f, 1f);
                    thickness = 4f;
                }
                else if (target.State == TargetPortraitState.IndirectCovered)
                {
                    surface = new Color(0.15f, 0.25f, 0.14f, 1f);
                    frame = new Color(0.48f, 0.72f, 0.42f, 1f);
                    thickness = 2f;
                }
                else if (target.State == TargetPortraitState.SelectedButUnfulfillable)
                {
                    surface = new Color(0.36f, 0.27f, 0.08f, 1f);
                    thickness = 3f;
                }
                Image background = KingmakerUiFactory.AddFramedPanel(rect, surface, frame, thickness);
                Button button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                button.interactable = target.Legal;
                button.onClick.AddListener(() => toggle(target.UnitId));
                _buttons[target.UnitId] = button;
                RectTransform imageRect = KingmakerUiFactory.CreateRect("Portrait", rect);
                KingmakerUiFactory.SetAnchors(imageRect, 0.08f, 0.25f, 0.92f, 0.94f);
                Image image = imageRect.gameObject.AddComponent<Image>();
                image.sprite = portrait(target.UnitId);
                image.preserveAspect = true;
                image.color = target.State == TargetPortraitState.DirectSelected
                    ? new Color(0.74f, 1f, 0.76f, 1f)
                    : target.State == TargetPortraitState.IndirectCovered
                        ? new Color(0.86f, 1f, 0.84f, 1f)
                        : target.State == TargetPortraitState.SelectedButUnfulfillable
                            ? new Color(1f, 0.86f, 0.55f, 0.88f)
                            : target.Legal ? Color.white : new Color(0.45f, 0.32f, 0.28f, 0.8f);
                image.raycastTarget = false;
                RectTransform overlayRect = KingmakerUiFactory.CreateRect("StateOverlay", imageRect);
                KingmakerUiFactory.Stretch(overlayRect);
                Image overlay = KingmakerUiFactory.AddPanel(overlayRect,
                    target.State == TargetPortraitState.DirectSelected
                        ? new Color(0.12f, 0.65f, 0.20f, 0.32f)
                        : target.State == TargetPortraitState.IndirectCovered
                            ? new Color(0.22f, 0.58f, 0.24f, 0.16f)
                            : target.State == TargetPortraitState.SelectedButUnfulfillable
                                ? new Color(0.82f, 0.55f, 0.08f, 0.25f)
                                : target.State == TargetPortraitState.Invalid
                                    ? new Color(0.55f, 0.08f, 0.06f, 0.18f) : Color.clear);
                overlay.raycastTarget = false;
                Text name = KingmakerUiFactory.CreateText("Name", rect, _theme,
                    target.Name, 12, TextAnchor.MiddleCenter);
                KingmakerUiFactory.SetAnchors(name.rectTransform, 0, 0.01f, 1, 0.25f);
                Text mark = KingmakerUiFactory.CreateText("State", rect, _theme,
                    target.State == TargetPortraitState.DirectSelected ? "SELECTED" :
                    target.State == TargetPortraitState.IndirectCovered ? "COVERED" :
                    target.State == TargetPortraitState.SelectedButUnfulfillable ? "SELECTED !" :
                    string.Empty, 11, TextAnchor.UpperCenter);
                mark.fontStyle = FontStyle.Bold;
                mark.color = target.State == TargetPortraitState.DirectSelected
                    ? new Color(0.82f, 1f, 0.78f, 1f) : frame;
                KingmakerUiFactory.SetAnchors(mark.rectTransform, 0.03f, 0.67f, 0.97f, 0.97f);
            }
        }

        internal bool InvokeTarget(string unitId)
        {
            Button button;
            if (!_buttons.TryGetValue(unitId, out button) || button == null || !button.interactable)
                return false;
            button.onClick.Invoke();
            return true;
        }
    }

    internal sealed class PlannerSelectedBuffView
    {
        private readonly PlannerUiTheme _theme;
        private readonly Image _icon;
        private readonly Text _fallback;
        private readonly Text _name;
        private readonly Text _meta;
        private readonly Text _description;
        private readonly Text _targetsLabel;
        private readonly Text _plan;
        private readonly PlannerTargetStripView _targets;
        private readonly Button _selectAll;
        private readonly Button _clear;

        internal PlannerSelectedBuffView(RectTransform parent, PlannerUiTheme theme,
            Action selectAll, Action clear)
        {
            _theme = theme;
            Root = KingmakerUiFactory.CreateRect("SelectedBuff", parent);
            KingmakerUiFactory.SetAnchors(Root, 0.02f, 0.075f, 0.98f, 0.305f);
            KingmakerUiFactory.AddFramedPanel(Root, theme.ParchmentPanel, theme.GoldAccent);
            RectTransform frame = KingmakerUiFactory.CreateRect("SelectedIconFrame", Root);
            KingmakerUiFactory.SetAnchors(frame, 0.015f, 0.39f, 0.09f, 0.93f);
            Image frameImage = KingmakerUiFactory.AddFramedPanel(frame,
                new Color(0.16f, 0.10f, 0.07f, 1f), theme.GoldAccent);
            frameImage.raycastTarget = false;
            RectTransform iconRect = KingmakerUiFactory.CreateRect("AbilityIcon", frame);
            KingmakerUiFactory.Stretch(iconRect, 5, 5, 5, 5);
            _icon = iconRect.gameObject.AddComponent<Image>();
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
            _fallback = KingmakerUiFactory.CreateText("MissingIcon", frame, theme, "?", 32,
                TextAnchor.MiddleCenter);
            KingmakerUiFactory.Stretch(_fallback.rectTransform);
            _name = KingmakerUiFactory.CreateText("SelectedName", Root, theme,
                "Select a buff", 23, TextAnchor.MiddleLeft);
            _name.fontStyle = FontStyle.Bold;
            _name.color = theme.BurgundyPrimary;
            KingmakerUiFactory.SetAnchors(_name.rectTransform, 0.105f, 0.74f, 0.42f, 0.94f);
            _meta = KingmakerUiFactory.CreateText("SelectedMeta", Root, theme,
                string.Empty, 14, TextAnchor.MiddleLeft);
            _meta.color = theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(_meta.rectTransform, 0.105f, 0.61f, 0.42f, 0.76f);
            _description = KingmakerUiFactory.CreateText("SelectedDescription", Root, theme,
                string.Empty, 14, TextAnchor.UpperLeft);
            KingmakerUiFactory.SetAnchors(_description.rectTransform, 0.105f, 0.14f, 0.42f, 0.60f);
            _targetsLabel = KingmakerUiFactory.CreateText("TargetsLabel", Root, theme,
                string.Empty, 17, TextAnchor.MiddleLeft);
            _targetsLabel.color = theme.BurgundyPrimary;
            _targetsLabel.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.SetAnchors(_targetsLabel.rectTransform, 0.435f, 0.78f, 0.98f, 0.95f);
            _targets = new PlannerTargetStripView(Root, theme);
            KingmakerUiFactory.SetAnchors(_targets.Root, 0.435f, 0.25f, 0.84f, 0.77f);
            _selectAll = KingmakerUiFactory.CreateButton("SelectAllValid", Root, theme,
                "Select All Valid", () => selectAll());
            KingmakerUiFactory.SetAnchors((RectTransform)_selectAll.transform,
                0.85f, 0.52f, 0.975f, 0.73f);
            _clear = KingmakerUiFactory.CreateButton("ClearTargets", Root, theme,
                "Clear Targets", () => clear());
            KingmakerUiFactory.SetAnchors((RectTransform)_clear.transform,
                0.85f, 0.27f, 0.975f, 0.48f);
            _plan = KingmakerUiFactory.CreateText("PlanSummary", Root, theme,
                string.Empty, 15, TextAnchor.MiddleLeft);
            _plan.color = theme.BurgundyPrimary;
            KingmakerUiFactory.SetAnchors(_plan.rectTransform, 0.435f, 0.04f, 0.975f, 0.22f);
        }

        internal RectTransform Root { get; private set; }
        internal string BoundName { get { return _name.text; } }
        internal int TargetCount { get { return _targets.Root.childCount; } }
        internal bool InvokeTarget(string unitId) { return _targets.InvokeTarget(unitId); }

        internal void Bind(SetupSourceRow source, Sprite icon, string routineId,
            IReadOnlyList<TargetPortraitViewModel> targets, Func<string, Sprite> portrait,
            Action<string> toggle, Func<PlannerPresentationStatus, Color> statusColor,
            string planSummary, bool interactable)
        {
            bool available = source != null;
            _icon.sprite = icon;
            _icon.gameObject.SetActive(icon != null);
            _fallback.gameObject.SetActive(available && icon == null);
            _name.text = available ? source.DisplayName : "Select a buff";
            _meta.text = available ? BuffCardViewModel.SourceSummary(source) +
                (source.SpellLevel > 0 ? " | Level " + source.SpellLevel : string.Empty) +
                (string.IsNullOrWhiteSpace(source.DurationText) ? string.Empty :
                    " | " + source.DurationText) : string.Empty;
            _description.text = available ? Compact(source.Description, 260) :
                "Choose a card, then click portraits to edit the active routine.";
            _targetsLabel.text = "Targets for " + char.ToUpperInvariant(routineId[0]) +
                routineId.Substring(1);
            _targets.Bind(targets, portrait, toggle, statusColor);
            _plan.text = planSummary ?? string.Empty;
            _selectAll.interactable = available && interactable;
            _clear.interactable = available && interactable && targets.Any(target => target.Wanted);
        }

        private static string Compact(string value, int limit)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ?
                "No description is available." : value.Replace("\r", " ").Replace("\n", " ").Trim();
            return normalized.Length <= limit ? normalized : normalized.Substring(0, limit - 3) + "...";
        }
    }

    internal sealed class PlannerSettingsView
    {
        private readonly Button _mode;
        private readonly Button _combat;
        private readonly Button _existing;
        private readonly Button _fallback;
        private readonly Button _hotkey;

        internal PlannerSettingsView(RectTransform parent, PlannerUiTheme theme,
            Action toggleMode, Action toggleCombat, Action toggleExisting,
            Action toggleFallback, Action toggleHotkey, Action close)
        {
            Root = KingmakerUiFactory.CreateRect("SettingsPanel", parent);
            KingmakerUiFactory.SetAnchors(Root, 0.31f, 0.29f, 0.69f, 0.75f);
            KingmakerUiFactory.AddFramedPanel(Root, theme.ParchmentRaised,
                theme.BurgundyPrimary, 2f);
            Text title = KingmakerUiFactory.CreateText("SettingsTitle", Root, theme,
                "PLANNER SETTINGS", 24, TextAnchor.MiddleCenter);
            title.color = theme.BurgundyPrimary;
            title.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.SetAnchors(title.rectTransform, 0.06f, 0.84f, 0.94f, 0.97f);
            _mode = SettingButton("CastingMode", 0.68f, 0.81f, toggleMode, theme);
            _combat = SettingButton("CombatUse", 0.53f, 0.66f, toggleCombat, theme);
            _existing = SettingButton("ExistingBuffs", 0.38f, 0.51f, toggleExisting, theme);
            _fallback = SettingButton("Fallback", 0.23f, 0.36f, toggleFallback, theme);
            _hotkey = SettingButton("PlannerHotkey", 0.08f, 0.21f, toggleHotkey, theme);
            Button done = KingmakerUiFactory.CreateButton("SettingsDone", Root, theme,
                "Done", () => close());
            KingmakerUiFactory.SetAnchors((RectTransform)done.transform, 0.72f, 0.015f, 0.94f, 0.075f);
            Root.gameObject.SetActive(false);
        }

        internal RectTransform Root { get; private set; }
        internal bool IsOpen { get { return Root.gameObject.activeSelf; } }
        internal void Show(bool value) { Root.gameObject.SetActive(value); }

        internal void Bind(PlannerSettingsViewModel model, bool interactable)
        {
            Set(_mode, "Casting mode: " + model.CastingMode, interactable);
            Set(_combat, "Combat use: " + model.CombatUse, interactable);
            Set(_existing, "Existing buffs: " + model.ExistingBuffs, interactable);
            Set(_fallback, "Fallback: " + model.Fallback, interactable);
            Set(_hotkey, "Planner hotkey: " + model.Hotkey, interactable);
        }

        private Button SettingButton(string name, float minY, float maxY,
            Action action, PlannerUiTheme theme)
        {
            Button button = KingmakerUiFactory.CreateButton(name, Root, theme,
                string.Empty, () => action());
            KingmakerUiFactory.SetAnchors((RectTransform)button.transform,
                0.08f, minY, 0.92f, maxY);
            return button;
        }

        private static void Set(Button button, string text, bool interactable)
        {
            button.interactable = interactable;
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.text = text;
        }
    }

    internal sealed class PlannerHoverTooltip : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        internal string Text;
        internal Action<string> Show;
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (Show != null) Show(Text ?? string.Empty);
        }
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (Show != null) Show(string.Empty);
        }
    }
}
