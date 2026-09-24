using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using KingmakerBuffPlanner.Domain.Planning;

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
        private readonly RectTransform _iconFrame;
        private readonly Text _fallback;
        private readonly Text _name;
        private readonly Text _availability;
        private readonly Text _configuration;
        private readonly RoutineMembershipChipView[] _routineChips;
        private readonly PlannerRightClickHandler _inspect;
        private UnityAction _select;

        internal BuffCardView(RectTransform parent, PlannerUiTheme theme)
        {
            _theme = theme;
            Rect = KingmakerUiFactory.CreateRect("BuffCard", parent);
            _background = KingmakerUiFactory.AddFramedPanel(Rect, theme.ParchmentRaised,
                theme.MutedBrownText);
            Button = Rect.gameObject.AddComponent<Button>();
            Button.targetGraphic = _background;
            _inspect = Rect.gameObject.AddComponent<PlannerRightClickHandler>();
            RectTransform stripe = KingmakerUiFactory.CreateRect("StatusStripe", Rect);
            KingmakerUiFactory.SetAnchors(stripe, 0, 0, 0.018f, 1, 1, 0, 3, 3);
            _status = KingmakerUiFactory.AddPanel(stripe, theme.MutedBrownText);
            _status.raycastTarget = false;
            _iconFrame = KingmakerUiFactory.CreateRect("IconFrame", Rect);
            KingmakerUiFactory.SetAnchors(_iconFrame, 0.035f, 0.12f, 0.23f, 0.88f);
            Image frameImage = KingmakerUiFactory.AddFramedPanel(_iconFrame,
                new Color(0.16f, 0.10f, 0.07f, 1f), theme.GoldAccent);
            frameImage.raycastTarget = false;
            RectTransform iconRect = KingmakerUiFactory.CreateRect("AbilityIcon", _iconFrame);
            KingmakerUiFactory.Stretch(iconRect, 4, 4, 4, 4);
            _icon = iconRect.gameObject.AddComponent<Image>();
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
            _fallback = KingmakerUiFactory.CreateText("MissingIcon", _iconFrame, theme, "?", 28,
                TextAnchor.MiddleCenter);
            _fallback.color = theme.MutedBrownText;
            KingmakerUiFactory.Stretch(_fallback.rectTransform);
            _name = KingmakerUiFactory.CreateText("Name", Rect, theme, string.Empty, 17,
                TextAnchor.MiddleLeft);
            _name.fontStyle = FontStyle.Bold;
            _name.horizontalOverflow = HorizontalWrapMode.Wrap;
            _name.verticalOverflow = VerticalWrapMode.Overflow;
            KingmakerUiFactory.SetAnchors(_name.rectTransform, 0.25f, 0.55f, 0.79f, 0.92f);
            _routineChips = new[]
            {
                new RoutineMembershipChipView("L", Rect, theme, Button),
                new RoutineMembershipChipView("I", Rect, theme, Button),
                new RoutineMembershipChipView("S", Rect, theme, Button)
            };
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

        internal float MeasureNameHeight(string value, float cellWidth)
        {
            float width = CompleteNameLayout.NameWidth(cellWidth);
            ConfigureTopRect(_name.rectTransform, CompleteNameLayout.TextLeft,
                CompleteNameLayout.TextTop, width, CompleteNameLayout.MinimumNameHeight);
            string previous = _name.text;
            _name.text = value ?? string.Empty;
            float preferred = Mathf.Ceil(_name.preferredHeight);
            _name.text = previous;
            return Mathf.Max(CompleteNameLayout.MinimumNameHeight, preferred);
        }

        internal void ApplyLayout(float cellWidth, float cardHeight, float nameHeight)
        {
            float width = CompleteNameLayout.NameWidth(cellWidth);
            float safeNameHeight = Mathf.Max(
                CompleteNameLayout.MinimumNameHeight, nameHeight);
            ConfigureTopRect(_iconFrame, 12f, 12f, 64f, 64f);
            ConfigureTopRect(_name.rectTransform, CompleteNameLayout.TextLeft,
                CompleteNameLayout.TextTop, width, safeNameHeight);
            float availabilityTop = CompleteNameLayout.TextTop + safeNameHeight +
                CompleteNameLayout.NameToAvailabilityGap;
            ConfigureTopRect(_availability.rectTransform, CompleteNameLayout.TextLeft,
                availabilityTop, width, CompleteNameLayout.AvailabilityHeight);
            float configurationTop = availabilityTop +
                CompleteNameLayout.AvailabilityHeight;
            ConfigureTopRect(_configuration.rectTransform, CompleteNameLayout.TextLeft,
                configurationTop, Mathf.Max(30f, width - CompleteNameLayout.BadgeWidth),
                CompleteNameLayout.ConfigurationHeight);
            float chipsLeft = cellWidth - CompleteNameLayout.TextRight -
                CompleteNameLayout.RoutineChipWidth;
            for (int index = 0; index < _routineChips.Length; index++)
                ConfigureTopRect(_routineChips[index].Rect,
                    chipsLeft + index * (CompleteNameLayout.RoutineChipSize +
                        CompleteNameLayout.RoutineChipSpacing),
                    configurationTop + 1f, CompleteNameLayout.RoutineChipSize,
                    CompleteNameLayout.RoutineChipSize);
        }

        private static void ConfigureTopRect(
            RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        internal void Bind(BuffCardViewModel model, Sprite icon, UnityAction selected,
            Action<string> inspect, Func<PlannerPresentationStatus, Color> statusColor,
            Action<string> showTooltip)
        {
            SourceId = model.SourceId;
            Rect.name = "Source." + model.SourceId;
            _name.text = model.Name;
            foreach (RoutineMembershipChipView chip in _routineChips)
                chip.Bind(model.RoutineMemberships.FirstOrDefault(value =>
                    value.Abbreviation == chip.Abbreviation), showTooltip);
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
            _inspect.SourceId = model.SourceId;
            _inspect.Inspect = inspect;
            Rect.gameObject.SetActive(true);
        }

        internal void Hide()
        {
            SourceId = string.Empty;
            _inspect.SourceId = string.Empty;
            _inspect.Inspect = null;
            Rect.gameObject.SetActive(false);
        }
    }

    internal sealed class RoutineMembershipChipView
    {
        private readonly Image _background;
        private readonly Text _label;
        private readonly Outline _outline;
        private readonly PlannerHoverTooltip _tooltip;

        internal RoutineMembershipChipView(string abbreviation, RectTransform parent,
            PlannerUiTheme theme, Button owner)
        {
            Abbreviation = abbreviation;
            Rect = KingmakerUiFactory.CreateRect("RoutineChip." + abbreviation, parent);
            _background = KingmakerUiFactory.AddPanel(Rect, theme.ParchmentPanel);
            _background.raycastTarget = true;
            _outline = Rect.gameObject.AddComponent<Outline>();
            _outline.effectColor = theme.MutedBrownText;
            _outline.effectDistance = new Vector2(1f, -1f);
            _label = KingmakerUiFactory.CreateText("Label", Rect, theme, abbreviation, 11,
                TextAnchor.MiddleCenter);
            _label.fontStyle = FontStyle.Bold;
            _label.raycastTarget = false;
            KingmakerUiFactory.Stretch(_label.rectTransform);
            _tooltip = Rect.gameObject.AddComponent<PlannerHoverTooltip>();
            PlannerCardChildClickForwarder forwarder =
                Rect.gameObject.AddComponent<PlannerCardChildClickForwarder>();
            forwarder.Owner = owner;
        }

        internal string Abbreviation { get; private set; }
        internal RectTransform Rect { get; private set; }

        internal void Bind(RoutineMembershipChipViewModel model,
            Action<string> showTooltip)
        {
            bool configured = model != null;
            Rect.gameObject.SetActive(configured);
            if (!configured) return;
            _label.text = model.Abbreviation;
            _background.color = model.IsActive
                ? new Color(0.38f, 0.13f, 0.10f, 1f)
                : new Color(0.74f, 0.65f, 0.45f, 0.82f);
            _label.color = model.IsActive ? Color.white : Color.black;
            _outline.effectColor = model.IsActive
                ? new Color(0.86f, 0.70f, 0.34f, 1f)
                : new Color(0.31f, 0.22f, 0.12f, 1f);
            _tooltip.Text = model.Tooltip;
            _tooltip.Show = showTooltip;
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
        private readonly Action<string> _inspect;
        private readonly Func<PlannerPresentationStatus, Color> _statusColor;
        private readonly Action<string> _showTooltip;
        private IReadOnlyList<BuffCardViewModel> _models = new BuffCardViewModel[0];
        private BuffGridMetrics _metrics;
        private BuffGridLayout _layout;
        private float[] _nameHeights = new float[0];
        private int _firstRow = -1;

        internal BuffGridView(RectTransform parent, PlannerUiTheme theme,
            Func<string, Sprite> icon, Action<string> select,
            Action<string> inspect,
            Func<PlannerPresentationStatus, Color> statusColor,
            Action<string> showTooltip)
        {
            _icon = icon;
            _select = select;
            _inspect = inspect;
            _statusColor = statusColor;
            _showTooltip = showTooltip;
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
            _nameHeights = new float[_models.Count];
            var requiredHeights = new float[_models.Count];
            BuffCardView measurer = _pool[0];
            for (int index = 0; index < _models.Count; index++)
            {
                float nameHeight = measurer.MeasureNameHeight(
                    _models[index].Name, _metrics.CellWidth);
                _nameHeights[index] = nameHeight;
                requiredHeights[index] = CompleteNameLayout.RequiredCardHeight(
                    _metrics.CellHeight, nameHeight);
            }
            _layout = BuffGridLayout.Calculate(requiredHeights,
                _metrics.CellHeight, _metrics.VerticalSpacing);
            float contentHeight = Mathf.Max(height, 12f + _layout.ContentHeight);
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
            _content.anchoredPosition = new Vector2(
                0, _layout.ScrollOffsetForItem(index));
            _firstRow = -1;
            BindVisible(true);
            visible = _pool.Cards.FirstOrDefault(card => card.SourceId == sourceId);
            if (visible == null) return false;
            visible.Button.onClick.Invoke();
            return true;
        }

        private void BindVisible(bool force)
        {
            if (_metrics == null || _layout == null) return;
            int row = _layout.FirstVisibleRow(_content.anchoredPosition.y);
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
                float rowHeight = _layout.RowHeight(absoluteRow);
                card.Rect.sizeDelta = new Vector2(_metrics.CellWidth, rowHeight);
                card.Rect.anchoredPosition = new Vector2(_metrics.SideInset + column *
                    (_metrics.CellWidth + spacing), -6f - _layout.RowOffset(absoluteRow));
                card.ApplyLayout(_metrics.CellWidth, rowHeight, _nameHeights[modelIndex]);
                BuffCardViewModel model = _models[modelIndex];
                card.Bind(model, _icon(model.SourceId), () => _select(model.SourceId),
                    _inspect, _statusColor, _showTooltip);
            }
        }
    }

    internal sealed class PlannerTargetStripView
    {
        private readonly RectTransform _root;
        private readonly PlannerUiTheme _theme;
        private readonly Dictionary<string, Button> _buttons =
            new Dictionary<string, Button>(StringComparer.Ordinal);
        private readonly Action<string> _showTooltip;

        internal PlannerTargetStripView(RectTransform parent, PlannerUiTheme theme,
            Action<string> showTooltip)
        {
            _theme = theme;
            _showTooltip = showTooltip;
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
                if (target.State == TargetPortraitState.DirectSelectedAndCovered)
                {
                    surface = new Color(0.12f, 0.31f, 0.15f, 1f);
                    frame = new Color(0.38f, 0.88f, 0.43f, 1f);
                    thickness = 4f;
                }
                else if (target.State == TargetPortraitState.IndirectlyCovered)
                {
                    surface = new Color(0.15f, 0.25f, 0.14f, 1f);
                    frame = new Color(0.48f, 0.72f, 0.42f, 1f);
                    thickness = 2f;
                }
                else if (target.State == TargetPortraitState.DirectSelectedButUnavailable)
                {
                    surface = new Color(0.36f, 0.27f, 0.08f, 1f);
                    thickness = 3f;
                }
                Image background = KingmakerUiFactory.AddFramedPanel(rect, surface, frame, thickness);
                Button button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                button.interactable = target.State != TargetPortraitState.InvalidTarget;
                button.onClick.AddListener(() => toggle(target.UnitId));
                PlannerHoverTooltip hover = button.gameObject.AddComponent<PlannerHoverTooltip>();
                hover.Text = target.Tooltip;
                hover.Show = _showTooltip;
                _buttons[target.UnitId] = button;
                RectTransform imageRect = KingmakerUiFactory.CreateRect("Portrait", rect);
                KingmakerUiFactory.SetAnchors(imageRect, 0.08f, 0.25f, 0.92f, 0.94f);
                Image image = imageRect.gameObject.AddComponent<Image>();
                image.sprite = portrait(target.UnitId);
                image.preserveAspect = true;
                image.color = target.State == TargetPortraitState.DirectSelectedAndCovered
                    ? new Color(0.74f, 1f, 0.76f, 1f)
                    : target.State == TargetPortraitState.IndirectlyCovered
                        ? new Color(0.86f, 1f, 0.84f, 1f)
                        : target.State == TargetPortraitState.DirectSelectedButUnavailable
                            ? new Color(1f, 0.86f, 0.55f, 0.88f)
                            : target.Legal ? Color.white : new Color(0.45f, 0.32f, 0.28f, 0.8f);
                image.raycastTarget = false;
                RectTransform overlayRect = KingmakerUiFactory.CreateRect("StateOverlay", imageRect);
                KingmakerUiFactory.Stretch(overlayRect);
                Image overlay = KingmakerUiFactory.AddPanel(overlayRect,
                    target.State == TargetPortraitState.DirectSelectedAndCovered
                        ? new Color(0.12f, 0.65f, 0.20f, 0.32f)
                        : target.State == TargetPortraitState.IndirectlyCovered
                            ? new Color(0.22f, 0.58f, 0.24f, 0.16f)
                            : target.State == TargetPortraitState.DirectSelectedButUnavailable
                                ? new Color(0.82f, 0.55f, 0.08f, 0.25f)
                                : target.State == TargetPortraitState.InvalidTarget
                                    ? new Color(0.55f, 0.08f, 0.06f, 0.18f) : Color.clear);
                overlay.raycastTarget = false;
                Text name = KingmakerUiFactory.CreateText("Name", rect, _theme,
                    target.Name, 12, TextAnchor.MiddleCenter);
                KingmakerUiFactory.SetAnchors(name.rectTransform, 0, 0.01f, 1, 0.25f);
                Text mark = KingmakerUiFactory.CreateText("State", rect, _theme,
                    target.DisplayLabel, 11, TextAnchor.UpperCenter);
                mark.fontStyle = FontStyle.Bold;
                mark.color = target.State == TargetPortraitState.DirectSelectedAndCovered
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
        private readonly Button _casterPolicy;
        private readonly Text _casterPolicyLabel;
        private readonly PlannerHoverTooltip _casterPolicyTooltip;
        private readonly Text _targetsLabel;
        private readonly Text _plan;
        private readonly PlannerTargetStripView _targets;
        private readonly Button _selectAll;
        private readonly Button _clear;
        private readonly Button _assignments;
        private readonly Button _enhancement;
        private readonly Text _enhancementLabel;
        private readonly PlannerHoverTooltip _enhancementTooltip;

        internal PlannerSelectedBuffView(RectTransform parent, PlannerUiTheme theme,
            Action selectAll, Action clear, Action openCasters,
            Action openEnhancements, Action<string> showTooltip,
            Action openAssignments = null)
        {
            _theme = theme;
            Root = KingmakerUiFactory.CreateRect("SelectedBuff", parent);
            KingmakerUiFactory.SetAnchors(Root, 0.02f, 0.075f, 0.98f, 0.305f);
            KingmakerUiFactory.AddFramedPanel(Root, theme.ParchmentPanel, theme.GoldAccent);
            RectTransform frame = KingmakerUiFactory.CreateRect("SelectedIconFrame", Root);
            KingmakerUiFactory.SetAnchors(frame, 0.015f, 0.43f, 0.09f, 0.93f);
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
            _name.horizontalOverflow = HorizontalWrapMode.Wrap;
            _name.verticalOverflow = VerticalWrapMode.Overflow;
            KingmakerUiFactory.SetAnchors(_name.rectTransform, 0.105f, 0.70f, 0.42f, 0.96f);
            _meta = KingmakerUiFactory.CreateText("SelectedMeta", Root, theme,
                string.Empty, 14, TextAnchor.MiddleLeft);
            _meta.color = theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(_meta.rectTransform, 0.105f, 0.59f, 0.42f, 0.70f);
            _description = KingmakerUiFactory.CreateText("SelectedDescription", Root, theme,
                string.Empty, 14, TextAnchor.UpperLeft);
            _description.verticalOverflow = VerticalWrapMode.Truncate;
            KingmakerUiFactory.SetAnchors(_description.rectTransform, 0.105f, 0.39f, 0.42f, 0.59f);
            _casterPolicy = KingmakerUiFactory.CreateButton(
                "ChooseCasters", Root, theme, "Casters: Automatic",
                () => openCasters());
            KingmakerUiFactory.SetAnchors((RectTransform)_casterPolicy.transform,
                0.105f, 0.20f, 0.42f, 0.39f);
            _casterPolicyLabel = KingmakerUiFactory.SetButtonLabel(
                _casterPolicy, "Casters: Automatic");
            _casterPolicyLabel.alignment = TextAnchor.MiddleLeft;
            _casterPolicyLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _casterPolicyLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _casterPolicyTooltip =
                _casterPolicy.gameObject.AddComponent<PlannerHoverTooltip>();
            _casterPolicyTooltip.Show = showTooltip;
            _enhancement = KingmakerUiFactory.CreateButton("ChooseEnhancement", Root, theme,
                "Enhancement: None available", () => openEnhancements());
            KingmakerUiFactory.SetAnchors((RectTransform)_enhancement.transform,
                0.105f, 0.025f, 0.42f, 0.19f);
            _enhancementLabel = KingmakerUiFactory.SetButtonLabel(_enhancement,
                "Enhancement: None available");
            _enhancementLabel.alignment = TextAnchor.MiddleLeft;
            _enhancementLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _enhancementTooltip = _enhancement.gameObject.AddComponent<PlannerHoverTooltip>();
            _enhancementTooltip.Show = showTooltip;
            _targetsLabel = KingmakerUiFactory.CreateText("TargetsLabel", Root, theme,
                string.Empty, 17, TextAnchor.MiddleLeft);
            _targetsLabel.color = theme.BurgundyPrimary;
            _targetsLabel.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.SetAnchors(_targetsLabel.rectTransform, 0.435f, 0.78f, 0.98f, 0.95f);
            _targets = new PlannerTargetStripView(Root, theme, showTooltip);
            KingmakerUiFactory.SetAnchors(_targets.Root, 0.435f, 0.34f, 0.84f, 0.77f);
            _selectAll = KingmakerUiFactory.CreateButton("SelectAllValid", Root, theme,
                "Select All Valid", () => selectAll());
            KingmakerUiFactory.SetAnchors((RectTransform)_selectAll.transform,
                0.85f, 0.52f, 0.975f, 0.73f);
            _clear = KingmakerUiFactory.CreateButton("ClearTargets", Root, theme,
                "Clear Targets", () => clear());
            KingmakerUiFactory.SetAnchors((RectTransform)_clear.transform,
                0.85f, 0.27f, 0.975f, 0.48f);
            // Direct selected-spell entry into the assignment/resource
            // editor: the same surface the header button opens, seeded with
            // this source's assignments.
            _assignments = KingmakerUiFactory.CreateButton("EditAssignments", Root, theme,
                "Edit Assignments", () =>
                {
                    if (openAssignments != null) openAssignments();
                });
            KingmakerUiFactory.SetAnchors((RectTransform)_assignments.transform,
                0.85f, 0.02f, 0.975f, 0.23f);
            _plan = KingmakerUiFactory.CreateText("PlanSummary", Root, theme,
                string.Empty, 15, TextAnchor.MiddleLeft);
            _plan.color = theme.BurgundyPrimary;
            _plan.horizontalOverflow = HorizontalWrapMode.Wrap;
            _plan.verticalOverflow = VerticalWrapMode.Overflow;
            // Column-aligned with the target strip so the Edit Assignments
            // action below never overlaps the plan text rectangle.
            KingmakerUiFactory.SetAnchors(_plan.rectTransform, 0.435f, 0.03f, 0.84f, 0.31f);
        }

        internal RectTransform Root { get; private set; }
        internal string BoundName { get { return _name.text; } }
        internal int TargetCount { get { return _targets.Root.childCount; } }
        internal bool InvokeTarget(string unitId) { return _targets.InvokeTarget(unitId); }

        internal void Bind(SetupSourceRow source, Sprite icon, string routineId,
            IReadOnlyList<TargetPortraitViewModel> targets, Func<string, Sprite> portrait,
            Action<string> toggle, Func<PlannerPresentationStatus, Color> statusColor,
            string planSummary, SelectedCastingViewModel casting, bool interactable)
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
            _description.text = available ? Compact(source.Description, 190) :
                "Choose a card, then click portraits to edit the active routine.";
            KingmakerUiFactory.SetButtonLabel(
                _casterPolicy, casting.CasterPolicy.Summary);
            _casterPolicyTooltip.Text = casting.CasterPolicy.Description;
            _casterPolicy.image.color = casting.CasterPolicy.Warning
                ? _theme.AmberWarning : _theme.ParchmentRaised;
            _targetsLabel.text = "Targets for " + char.ToUpperInvariant(routineId[0]) +
                routineId.Substring(1);
            _targets.Bind(targets, portrait, toggle, statusColor);
            _plan.text = planSummary ?? string.Empty;
            KingmakerUiFactory.SetButtonLabel(_enhancement, casting.EnhancementLabel);
            _enhancementTooltip.Text = casting.EnhancementDescription ?? string.Empty;
            _enhancement.interactable = available && interactable;
            // Unmet shared-pool demand is a resolved allocation shortage, not
            // merely configured intent: the ordinary card must carry it, not
            // only the hidden resource view.
            _enhancement.image.color = casting.EnhancementWarning
                ? _theme.AmberWarning : _theme.ParchmentRaised;
            _casterPolicy.interactable = available && interactable;
            _assignments.interactable = available && interactable;
            _selectAll.interactable = available && interactable;
            _clear.interactable = available && interactable && targets.Any(target => target.Wanted);
        }

        internal string EnhancementRenderEvidence(int candidateCount, string selectedEnhancementId)
        {
            RectTransform buttonRect = (RectTransform)_enhancement.transform;
            Color color = _enhancementLabel.color;
            return "EnhancementButton: active=" + _enhancement.gameObject.activeInHierarchy +
                ";interactable=" + _enhancement.interactable +
                ";labelText=" + _enhancementLabel.text +
                ";labelActive=" + _enhancementLabel.gameObject.activeInHierarchy +
                ";labelAlpha=" + _enhancementLabel.canvasRenderer.GetAlpha().ToString("0.###") +
                ";labelColor=" + color +
                ";labelRect=" + _enhancementLabel.rectTransform.rect +
                ";buttonRect=" + buttonRect.rect +
                ";siblingIndex=" + _enhancementLabel.transform.GetSiblingIndex() +
                ";candidateCount=" + candidateCount +
                ";selectedEnhancementId=" + (selectedEnhancementId ?? string.Empty);
        }

        private static string Compact(string value, int limit)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ?
                "No description is available." : value.Replace("\r", " ").Replace("\n", " ").Trim();
            return normalized.Length <= limit ? normalized : normalized.Substring(0, limit - 3) + "...";
        }
    }

    internal sealed class PlannerEnhancementChooserView
    {
        private readonly PlannerUiTheme _theme;
        private readonly RectTransform _frame;
        private readonly RectTransform _content;
        private readonly ScrollRect _scroll;
        private readonly RectTransform _viewport;
        private readonly Scrollbar _scrollbar;
        private readonly Action<string> _select;
        private readonly Action<string> _showTooltip;
        private readonly Action<RectTransform> _rowsBound;
        private readonly List<GameObject> _rows = new List<GameObject>();
        private readonly Text _subtitle;
        private readonly Text _budget;
        private readonly Button _closeButton;
        // Assignment-scoped editing context: when set, selections route to
        // that child assignment and each selected row gains a required/
        // optional policy toggle.
        private string _assignmentSourceId;
        private string _assignmentId;
        private Action<string, string, string> _assignmentSelect;
        private Action<string, string, string, bool> _assignmentPolicy;
        private IReadOnlyList<EnhancementSelectionSummary> _assignmentSelections;

        internal PlannerEnhancementChooserView(RectTransform parent, PlannerUiTheme theme,
            Action<string> select, Action<string> showTooltip,
            Action<RectTransform> rowsBound = null)
        {
            _theme = theme;
            _select = select;
            _showTooltip = showTooltip;
            _rowsBound = rowsBound;
            Root = KingmakerUiFactory.CreateRect("EnhancementChooser", parent);
            KingmakerUiFactory.Stretch(Root);
            Image blocker = Root.gameObject.AddComponent<Image>();
            blocker.color = new Color(0.035f, 0.025f, 0.02f, 0.72f);
            blocker.raycastTarget = true;
            Button outside = Root.gameObject.AddComponent<Button>();
            outside.onClick.AddListener(Hide);

            _frame = KingmakerUiFactory.CreateRect("EnhancementChooserFrame", Root);
            KingmakerUiFactory.SetAnchors(_frame, 0.19f, 0.14f, 0.81f, 0.84f);
            KingmakerUiFactory.AddFramedPanel(_frame, theme.ParchmentRaised,
                theme.BurgundyPrimary, 2f).raycastTarget = true;
            Text title = KingmakerUiFactory.CreateText("EnhancementChooserTitle", _frame, theme,
                "CASTING ENHANCEMENT", 24, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            title.color = theme.BurgundyPrimary;
            KingmakerUiFactory.SetAnchors(title.rectTransform, 0.05f, 0.90f, 0.72f, 0.98f);
            _subtitle = KingmakerUiFactory.CreateText("EnhancementChooserSubtitle", _frame, theme,
                string.Empty, 14, TextAnchor.MiddleLeft);
            _subtitle.color = theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(_subtitle.rectTransform, 0.05f, 0.835f, 0.82f, 0.90f);
            // Sticky routine-level budget summary above the scroll area: the
            // same authoritative plan numbers the rows and the selected-spell
            // card show, so the scarce-pool state stays visible while
            // scrolling options.
            _budget = KingmakerUiFactory.CreateText("EnhancementChooserBudget", _frame, theme,
                string.Empty, 13, TextAnchor.UpperLeft);
            _budget.color = theme.BurgundyPrimary;
            _budget.horizontalOverflow = HorizontalWrapMode.Wrap;
            _budget.verticalOverflow = VerticalWrapMode.Truncate;
            KingmakerUiFactory.SetAnchors(_budget.rectTransform, 0.05f, 0.735f, 0.95f, 0.83f);
            Button close = KingmakerUiFactory.CreateButton("CloseEnhancementChooser", _frame,
                theme, "CLOSE", Hide);
            _closeButton = close;
            KingmakerUiFactory.SetAnchors((RectTransform)close.transform,
                0.83f, 0.90f, 0.95f, 0.98f);
            _scroll = KingmakerUiFactory.CreateScrollView("EnhancementChoices",
                _frame, theme, out _content,
                ChooserScrollLayoutContract.ScrollbarWidth);
            _viewport = _scroll.viewport;
            _scrollbar = _scroll.verticalScrollbar;
            KingmakerUiFactory.SetAnchors((RectTransform)_scroll.transform,
                0.05f, 0.03f, 0.95f, 0.725f);
            PlannerDescriptionEscape escape = Root.gameObject.AddComponent<PlannerDescriptionEscape>();
            escape.Close = Hide;
            Root.gameObject.SetActive(false);
        }

        internal RectTransform Root { get; private set; }
        internal RectTransform PaperSurface { get { return _frame; } }
        internal bool IsOpen { get { return Root.gameObject.activeSelf; } }
        internal ScrollRect Scroll { get { return _scroll; } }
        internal RectTransform Content { get { return _content; } }
        internal RectTransform Viewport { get { return _viewport; } }

        internal void ShowForAssignment(SelectedCastingViewModel model,
            string sourceId, string assignmentId,
            IReadOnlyList<EnhancementSelectionSummary> selections,
            Action<string, string, string> select,
            Action<string, string, string, bool> policy)
        {
            _assignmentSourceId = sourceId;
            _assignmentId = assignmentId;
            _assignmentSelections = selections;
            _assignmentSelect = select;
            _assignmentPolicy = policy;
            Show(model);
        }

        internal void ClearAssignmentScope()
        {
            _assignmentSourceId = null;
            _assignmentId = null;
            _assignmentSelections = null;
            _assignmentSelect = null;
            _assignmentPolicy = null;
        }

        internal void Show(SelectedCastingViewModel model)
        {
            // A refresh while the chooser is already open (an option was just
            // toggled) must keep the scroll offset; a fresh open reveals the
            // first selected row instead of starting at an arbitrary place.
            bool refresh = Root.gameObject.activeSelf;
            float previousOffset = refresh ? _content.anchoredPosition.y : 0f;
            int selectedRow = -1;
            ClearRows();
            string scope = _assignmentId == null ? string.Empty
                : " | assignment " + _assignmentId;
            _subtitle.text = model.CasterText + scope + " | " + model.CandidateCount +
                (model.CandidateCount == 1 ? " applicable option" : " applicable options");
            _budget.text = model.EnhancementBudgetText ?? string.Empty;
            _budget.gameObject.SetActive(
                !string.IsNullOrWhiteSpace(model.EnhancementBudgetText));
            int rowIndex = 0;
            foreach (EnhancementChoiceViewModel choice in model.Choices)
            {
                bool assignmentSelected = false;
                bool assignmentRequired = true;
                if (_assignmentId != null && _assignmentSelections != null)
                {
                    EnhancementSelectionSummary match = _assignmentSelections
                        .FirstOrDefault(candidate => candidate.EnhancementId == choice.EnhancementId);
                    assignmentSelected = match != null;
                    assignmentRequired = match == null || match.Required;
                }
                bool selected = _assignmentId == null ? choice.Selected : assignmentSelected;
                string selection = choice.CheckboxStyle
                    ? (selected ? "[x] " : "[ ] ")
                    : (selected ? "SELECTED | " : string.Empty);
                if (selected && selectedRow < 0) selectedRow = rowIndex;
                string policy = _assignmentId != null && selected
                    ? " | " + choice.PolicyCaption
                    : string.Empty;
                // The per-choice budget note is plan-derived (never a second
                // counter): current pool allocation and this selection's
                // charge demand, visible directly on the row.
                string budgetNote = string.IsNullOrWhiteSpace(choice.BudgetNote)
                    ? string.Empty : " | " + choice.BudgetNote;
                string text = selection + choice.Title +
                    "\n" + choice.Summary + budgetNote + policy;
                Button button;
                if (_assignmentId == null)
                {
                    button = KingmakerUiFactory.CreateButton("EnhancementChoice", _content,
                        _theme, text, () =>
                        {
                            // Adding needs availability; removing a configured
                            // selection never does.
                            if (!choice.CanSelect && !choice.CanDeselect) return;
                            _select(choice.EnhancementId);
                        });
                    KingmakerUiFactory.AddLayout((RectTransform)button.transform,
                        ChooserScrollLayoutContract.EnhancementRowHeight);
                }
                else
                {
                    // Assignment mode rows are containers so the choice and
                    // its policy toggle share one layout row.
                    RectTransform container = KingmakerUiFactory.CreateRect(
                        "EnhancementChoiceRow", _content);
                    KingmakerUiFactory.AddLayout(container,
                        ChooserScrollLayoutContract.EnhancementRowHeight);
                    button = KingmakerUiFactory.CreateButton("EnhancementChoice", container,
                        _theme, text, () =>
                        {
                            if (!choice.CanSelect && !choice.CanDeselect) return;
                            if (_assignmentSelect != null)
                                _assignmentSelect(_assignmentSourceId, _assignmentId,
                                    choice.EnhancementId);
                        });
                    KingmakerUiFactory.SetAnchors((RectTransform)button.transform,
                        0f, 0f, 0.84f, 1f);
                    if (selected && _assignmentPolicy != null)
                    {
                        bool requiredSnapshot = assignmentRequired;
                        string captionSnapshot = choice.PolicyCaption;
                        // The caption states the actual policy and targeting
                        // semantics; a targeting modifier cannot present
                        // itself as optional because the planner never drops
                        // it. Only non-targeting selections toggle.
                        Button policyToggle = KingmakerUiFactory.CreateButton(
                            "Policy." + choice.EnhancementId, container, _theme,
                            captionSnapshot, () =>
                            {
                                if (!choice.CanTogglePolicy) return;
                                _assignmentPolicy(_assignmentSourceId, _assignmentId,
                                    choice.EnhancementId, !requiredSnapshot);
                            });
                        KingmakerUiFactory.SetAnchors((RectTransform)policyToggle.transform,
                            0.845f, 0.05f, 0.995f, 0.95f);
                        policyToggle.interactable = choice.CanTogglePolicy;
                        Text policyLabel = policyToggle.GetComponentInChildren<Text>(true);
                        if (policyLabel != null) policyLabel.fontSize = 10;
                        KingmakerUiFactory.FitButtonToCaption(
                            (RectTransform)policyToggle.transform, 110f, 30f);
                    }
                    _rows.Add(container.gameObject);
                }
                Text label = KingmakerUiFactory.SetButtonLabel(button, text);
                label.alignment = TextAnchor.MiddleLeft;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 12;
                label.resizeTextMaxSize = 16;
                if (selected)
                    button.image.color = _theme.GreenSuccess;
                // Selected rows stay clickable so an exhausted or vanished
                // selection can be removed individually; unavailable
                // unselected rows remain inert.
                button.interactable = choice.CanSelect || choice.CanDeselect;
                PlannerHoverTooltip tooltip = button.gameObject.AddComponent<PlannerHoverTooltip>();
                tooltip.Text = choice.Description;
                tooltip.Show = _showTooltip;
                if (_assignmentId == null) _rows.Add(button.gameObject);
                rowIndex++;
            }
            Root.SetAsLastSibling();
            Root.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            // The content rect height has exactly one owner: this view, sized
            // from the fixed per-row contract. The shared VerticalLayoutGroup
            // only stacks rows; it never resizes the content itself, and no
            // ContentSizeFitter competes for the height.
            float viewportHeight = Mathf.Max(0f, _viewport.rect.height);
            float contentHeight = ChooserScrollLayoutContract.ContentHeight(
                _rows.Count, ChooserScrollLayoutContract.EnhancementRowHeight);
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            float offset = refresh
                ? ChooserScrollLayoutContract.ClampScrollOffset(
                    previousOffset, viewportHeight, contentHeight)
                : ChooserScrollLayoutContract.OffsetRevealingRow(
                    selectedRow, 0f, viewportHeight, contentHeight,
                    ChooserScrollLayoutContract.EnhancementRowHeight);
            _content.anchoredPosition = new Vector2(0f, offset);
            if (_scrollbar != null)
                _scrollbar.size = ChooserScrollLayoutContract.ScrollbarHandleRatio(
                    viewportHeight, contentHeight);
            KingmakerUiFactory.ForceLayoutAndSnap(Root);
            KingmakerUiFactory.FitButtonToCaption(
                (RectTransform)_closeButton.transform, 96f, 34f);
            if (_rowsBound != null) _rowsBound(Root);
        }

        internal void Hide()
        {
            Root.gameObject.SetActive(false);
            ClearAssignmentScope();
            if (_showTooltip != null) _showTooltip(string.Empty);
        }

        private void ClearRows()
        {
            foreach (GameObject row in _rows)
            {
                if (row == null) continue;
                row.SetActive(false);
                UnityEngine.Object.Destroy(row);
            }
            _rows.Clear();
        }
    }

    internal sealed class PlannerCasterPolicyChooserView
    {
        private readonly PlannerUiTheme _theme;
        private readonly RectTransform _frame;
        private readonly RectTransform _content;
        private readonly ScrollRect _scroll;
        private readonly RectTransform _viewport;
        private readonly Scrollbar _scrollbar;
        private readonly Action<string, bool> _setEnabled;
        private readonly Action<string> _moveEarlier;
        private readonly Action<string> _moveLater;
        private readonly Action<string, int?> _setMaximum;
        private readonly Action _reset;
        private readonly Action<string> _showTooltip;
        private readonly Action<RectTransform> _rowsBound;
        private readonly List<GameObject> _rows = new List<GameObject>();
        private readonly Text _subtitle;
        private readonly Button _resetButton;

        internal PlannerCasterPolicyChooserView(
            RectTransform parent,
            PlannerUiTheme theme,
            Action<string, bool> setEnabled,
            Action<string> moveEarlier,
            Action<string> moveLater,
            Action<string, int?> setMaximum,
            Action reset,
            Action<string> showTooltip,
            Action<RectTransform> rowsBound = null)
        {
            _theme = theme;
            _setEnabled = setEnabled;
            _moveEarlier = moveEarlier;
            _moveLater = moveLater;
            _setMaximum = setMaximum;
            _reset = reset;
            _showTooltip = showTooltip;
            _rowsBound = rowsBound;
            Root = KingmakerUiFactory.CreateRect("CasterPolicyChooser", parent);
            KingmakerUiFactory.Stretch(Root);
            Image blocker = Root.gameObject.AddComponent<Image>();
            blocker.color = new Color(0.035f, 0.025f, 0.02f, 0.72f);
            blocker.raycastTarget = true;
            Button outside = Root.gameObject.AddComponent<Button>();
            outside.onClick.AddListener(Hide);

            _frame = KingmakerUiFactory.CreateRect(
                "CasterPolicyChooserFrame", Root);
            KingmakerUiFactory.SetAnchors(_frame, 0.10f, 0.08f, 0.90f, 0.92f);
            KingmakerUiFactory.AddFramedPanel(_frame, theme.ParchmentRaised,
                theme.BurgundyPrimary, 2f).raycastTarget = true;
            Text title = KingmakerUiFactory.CreateText(
                "CasterPolicyChooserTitle", _frame, theme,
                "CASTER POLICY", 24, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            title.color = theme.BurgundyPrimary;
            KingmakerUiFactory.SetAnchors(
                title.rectTransform, 0.035f, 0.89f, 0.48f, 0.97f);
            _subtitle = KingmakerUiFactory.CreateText(
                "CasterPolicyChooserSubtitle", _frame, theme,
                "Choose order, enabled casters, and maximum casts per run.",
                14, TextAnchor.MiddleLeft);
            _subtitle.color = theme.MutedBrownText;
            _subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            KingmakerUiFactory.SetAnchors(
                _subtitle.rectTransform, 0.035f, 0.80f, 0.72f, 0.89f);
            _resetButton = KingmakerUiFactory.CreateButton(
                "ResetCasterPolicy", _frame, theme,
                "RESET AUTOMATIC", () => _reset());
            KingmakerUiFactory.SetAnchors(
                (RectTransform)_resetButton.transform,
                0.69f, 0.89f, 0.84f, 0.97f);
            Button close = KingmakerUiFactory.CreateButton(
                "CloseCasterPolicyChooser", _frame, theme, "CLOSE", Hide);
            KingmakerUiFactory.SetAnchors(
                (RectTransform)close.transform,
                0.85f, 0.89f, 0.965f, 0.97f);
            _scroll = KingmakerUiFactory.CreateScrollView(
                "CasterPolicyRows", _frame, theme, out _content,
                ChooserScrollLayoutContract.ScrollbarWidth);
            _viewport = _scroll.viewport;
            _scrollbar = _scroll.verticalScrollbar;
            KingmakerUiFactory.SetAnchors(
                (RectTransform)_scroll.transform,
                0.035f, 0.055f, 0.965f, 0.79f);
            PlannerDescriptionEscape escape =
                Root.gameObject.AddComponent<PlannerDescriptionEscape>();
            escape.Close = Hide;
            Root.gameObject.SetActive(false);
        }

        internal RectTransform Root { get; private set; }
        internal RectTransform PaperSurface { get { return _frame; } }
        internal bool IsOpen { get { return Root.gameObject.activeSelf; } }
        internal ScrollRect Scroll { get { return _scroll; } }
        internal RectTransform Content { get { return _content; } }
        internal RectTransform Viewport { get { return _viewport; } }

        internal void Show(
            CasterPolicyViewModel model,
            Func<string, Sprite> portrait,
            bool interactable)
        {
            // Every policy action (enable/order/maximum) rebuilds these rows
            // through RefreshCasterPolicyChooser; that refresh must keep the
            // offset so the row just acted on stays in view.
            bool refresh = Root.gameObject.activeSelf;
            float previousOffset = refresh ? _content.anchoredPosition.y : 0f;
            ClearRows();
            _subtitle.text = model.Summary +
                "\nMaximum per run applies only to this buff in one routine execution.";
            _resetButton.interactable = interactable;
            foreach (ProviderPolicyRowViewModel provider in model.Providers)
                BuildRow(provider, portrait == null
                    ? null : portrait(provider.CasterUnitId), interactable);
            Root.SetAsLastSibling();
            Root.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            // Same single-owner rule as the enhancement chooser: this view
            // sizes the content rect from the fixed row contract; the shared
            // layout group only stacks rows.
            float viewportHeight = Mathf.Max(0f, _viewport.rect.height);
            float contentHeight = ChooserScrollLayoutContract.ContentHeight(
                _rows.Count, CastingPanelLayoutContract.MinimumCasterPolicyRowHeight);
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            float offset = ChooserScrollLayoutContract.ClampScrollOffset(
                previousOffset, viewportHeight, contentHeight);
            _content.anchoredPosition = new Vector2(0f, offset);
            if (_scrollbar != null)
                _scrollbar.size = ChooserScrollLayoutContract.ScrollbarHandleRatio(
                    viewportHeight, contentHeight);
            KingmakerUiFactory.ForceLayoutAndSnap(Root);
            KingmakerUiFactory.FitButtonToCaption(
                (RectTransform)_resetButton.transform, 150f, 34f);
            if (_rowsBound != null) _rowsBound(Root);
        }

        internal void Hide()
        {
            Root.gameObject.SetActive(false);
            if (_showTooltip != null) _showTooltip(string.Empty);
        }

        private void BuildRow(
            ProviderPolicyRowViewModel model,
            Sprite portrait,
            bool interactable)
        {
            RectTransform row = KingmakerUiFactory.CreateRect(
                "Provider." + model.Order, _content);
            KingmakerUiFactory.AddLayout(
                row, CastingPanelLayoutContract.MinimumCasterPolicyRowHeight);
            KingmakerUiFactory.AddFramedPanel(row,
                model.Enabled ? _theme.ParchmentPanel : _theme.DisabledGray,
                _theme.MutedBrownText);

            RectTransform portraitFrame =
                KingmakerUiFactory.CreateRect("Portrait", row);
            KingmakerUiFactory.SetAnchors(
                portraitFrame, 0.012f, 0.14f, 0.078f, 0.86f);
            KingmakerUiFactory.AddFramedPanel(
                portraitFrame, Color.black, _theme.GoldAccent);
            RectTransform portraitImage =
                KingmakerUiFactory.CreateRect("PortraitImage", portraitFrame);
            KingmakerUiFactory.Stretch(portraitImage, 3, 3, 3, 3);
            Image image = portraitImage.gameObject.AddComponent<Image>();
            image.sprite = portrait;
            image.preserveAspect = true;
            image.raycastTarget = false;
            Text identity = KingmakerUiFactory.CreateText(
                "Identity", row, _theme,
                model.Order + ". " + model.CasterName +
                "\n" + model.Source,
                15, TextAnchor.MiddleLeft);
            identity.fontStyle = FontStyle.Bold;
            identity.horizontalOverflow = HorizontalWrapMode.Wrap;
            KingmakerUiFactory.SetAnchors(
                identity.rectTransform, 0.09f, 0.47f, 0.47f, 0.91f);
            Text availability = KingmakerUiFactory.CreateText(
                "Availability", row, _theme,
                model.Remaining +
                (string.IsNullOrWhiteSpace(model.UnavailableReason)
                    ? string.Empty : " | " + model.UnavailableReason),
                13, TextAnchor.MiddleLeft);
            availability.horizontalOverflow = HorizontalWrapMode.Wrap;
            availability.color = string.IsNullOrWhiteSpace(model.UnavailableReason)
                ? _theme.MutedBrownText : _theme.AmberWarning;
            KingmakerUiFactory.SetAnchors(
                availability.rectTransform, 0.09f, 0.08f, 0.47f, 0.48f);

            Button enabled = KingmakerUiFactory.CreateButton(
                "Enabled", row, _theme,
                model.Enabled ? "USE" : "DO NOT USE",
                () => _setEnabled(model.ProviderKey, !model.Enabled));
            KingmakerUiFactory.SetAnchors(
                (RectTransform)enabled.transform,
                0.49f, 0.22f, 0.62f, 0.78f);
            enabled.interactable = interactable;
            Button earlier = KingmakerUiFactory.CreateButton(
                "Earlier", row, _theme, "EARLIER",
                () => _moveEarlier(model.ProviderKey));
            KingmakerUiFactory.SetAnchors(
                (RectTransform)earlier.transform,
                0.635f, 0.22f, 0.735f, 0.78f);
            earlier.interactable = interactable && model.CanMoveEarlier;
            Button later = KingmakerUiFactory.CreateButton(
                "Later", row, _theme, "LATER",
                () => _moveLater(model.ProviderKey));
            KingmakerUiFactory.SetAnchors(
                (RectTransform)later.transform,
                0.745f, 0.22f, 0.835f, 0.78f);
            later.interactable = interactable && model.CanMoveLater;
            Button maximum = KingmakerUiFactory.CreateButton(
                "MaximumPerRun", row, _theme,
                "MAX/RUN\n" + (model.MaximumCasts == null
                    ? "Unlimited" : model.MaximumCasts.Value.ToString()),
                () => _setMaximum(
                    model.ProviderKey, model.NextMaximumCasts()));
            KingmakerUiFactory.SetAnchors(
                (RectTransform)maximum.transform,
                0.85f, 0.16f, 0.985f, 0.84f);
            Text maximumLabel =
                maximum.GetComponentInChildren<Text>(true);
            if (maximumLabel != null)
            {
                maximumLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                maximumLabel.resizeTextForBestFit = true;
                maximumLabel.resizeTextMinSize = 11;
                maximumLabel.resizeTextMaxSize = 15;
            }
            maximum.interactable = interactable;
            PlannerHoverTooltip tooltip =
                maximum.gameObject.AddComponent<PlannerHoverTooltip>();
            tooltip.Text =
                "Maximum casts from this exact provider in one routine execution.";
            tooltip.Show = _showTooltip;
            _rows.Add(row.gameObject);
        }

        private void ClearRows()
        {
            foreach (GameObject row in _rows)
            {
                if (row == null) continue;
                row.SetActive(false);
                UnityEngine.Object.Destroy(row);
            }
            _rows.Clear();
        }
    }
    // Per-assignment target picker: the ordinary, discoverable way to add
    // and remove explicit targets on one child assignment, including targets
    // that other rows hold (they move, never duplicate).
    internal sealed class PlannerAssignmentTargetChooserView
    {
        private readonly PlannerUiTheme _theme;
        private readonly RectTransform _frame;
        private readonly RectTransform _content;
        private readonly ScrollRect _scroll;
        private readonly RectTransform _viewport;
        private readonly Scrollbar _scrollbar;
        private readonly Text _subtitle;
        private readonly Button _closeButton;
        private readonly Action _refresh;
        private readonly Action<string> _showTooltip;
        private readonly Action<RectTransform> _rowsBound;
        private readonly List<GameObject> _rows = new List<GameObject>();

        internal PlannerAssignmentTargetChooserView(RectTransform parent,
            PlannerUiTheme theme, Action refresh, Action<string> showTooltip,
            Action<RectTransform> rowsBound = null)
        {
            _theme = theme;
            _refresh = refresh;
            _showTooltip = showTooltip;
            _rowsBound = rowsBound;
            Root = KingmakerUiFactory.CreateRect("AssignmentTargetChooser", parent);
            KingmakerUiFactory.Stretch(Root);
            Image blocker = Root.gameObject.AddComponent<Image>();
            blocker.color = new Color(0.035f, 0.025f, 0.02f, 0.72f);
            blocker.raycastTarget = true;
            Button outside = Root.gameObject.AddComponent<Button>();
            outside.onClick.AddListener(Hide);
            _frame = KingmakerUiFactory.CreateRect("TargetChooserFrame", Root);
            KingmakerUiFactory.SetAnchors(_frame, 0.25f, 0.14f, 0.75f, 0.86f);
            KingmakerUiFactory.AddFramedPanel(_frame, theme.ParchmentRaised,
                theme.BurgundyPrimary, 2f).raycastTarget = true;
            Text title = KingmakerUiFactory.CreateText("Title", _frame, theme,
                "TARGETS", 22, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            title.color = theme.BurgundyPrimary;
            KingmakerUiFactory.SetAnchors(title.rectTransform, 0.04f, 0.90f, 0.60f, 0.98f);
            _subtitle = KingmakerUiFactory.CreateText("Subtitle", _frame, theme,
                string.Empty, 13, TextAnchor.MiddleLeft);
            _subtitle.color = theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(_subtitle.rectTransform, 0.04f, 0.83f, 0.96f, 0.90f);
            _closeButton = KingmakerUiFactory.CreateButton("Close", _frame, theme,
                "CLOSE", Hide);
            KingmakerUiFactory.SetAnchors((RectTransform)_closeButton.transform,
                0.84f, 0.90f, 0.97f, 0.98f);
            _scroll = KingmakerUiFactory.CreateScrollView("TargetRows", _frame, theme,
                out _content, ChooserScrollLayoutContract.ScrollbarWidth);
            _viewport = _scroll.viewport;
            _scrollbar = _scroll.verticalScrollbar;
            KingmakerUiFactory.SetAnchors((RectTransform)_scroll.transform,
                0.03f, 0.03f, 0.97f, 0.82f);
            PlannerDescriptionEscape escape = Root.gameObject
                .AddComponent<PlannerDescriptionEscape>();
            escape.Close = Hide;
            Root.gameObject.SetActive(false);
        }

        internal RectTransform Root { get; private set; }
        internal RectTransform PaperSurface { get { return _frame; } }
        internal bool IsOpen { get { return Root.gameObject.activeSelf; } }

        internal void Show(string subtitle,
            IEnumerable<AssignmentTargetRowViewModel> rows)
        {
            bool refresh = Root.gameObject.activeSelf;
            float previousOffset = refresh ? _content.anchoredPosition.y : 0f;
            foreach (GameObject row in _rows)
            {
                if (row == null) continue;
                row.SetActive(false);
                UnityEngine.Object.Destroy(row);
            }
            _rows.Clear();
            _subtitle.text = subtitle ?? string.Empty;
            foreach (AssignmentTargetRowViewModel rowModel in rows)
            {
                AssignmentTargetRowViewModel captured = rowModel;
                Button button = KingmakerUiFactory.CreateButton(
                    "Target." + rowModel.UnitId, _content, _theme,
                    (rowModel.Assigned ? "[x] " : "[ ] ") + rowModel.DisplayName +
                        (string.IsNullOrWhiteSpace(rowModel.Reason)
                            ? string.Empty : " — " + rowModel.Reason),
                    () =>
                    {
                        captured.Toggle();
                        _refresh();
                    });
                KingmakerUiFactory.AddLayout((RectTransform)button.transform,
                    ChooserScrollLayoutContract.EnhancementRowHeight);
                button.interactable = rowModel.Assigned || rowModel.CanAssign;
                if (rowModel.Assigned)
                    button.image.color = _theme.GreenSuccess;
                _rows.Add(button.gameObject);
            }
            Root.SetAsLastSibling();
            Root.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            float viewportHeight = Mathf.Max(0f, _viewport.rect.height);
            float contentHeight = ChooserScrollLayoutContract.ContentHeight(
                _rows.Count, ChooserScrollLayoutContract.EnhancementRowHeight);
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            _content.anchoredPosition = new Vector2(0f, ChooserScrollLayoutContract
                .ClampScrollOffset(previousOffset, viewportHeight, contentHeight));
            if (_scrollbar != null)
                _scrollbar.size = ChooserScrollLayoutContract.ScrollbarHandleRatio(
                    viewportHeight, contentHeight);
            KingmakerUiFactory.ForceLayoutAndSnap(Root);
            KingmakerUiFactory.FitButtonToCaption(
                (RectTransform)_closeButton.transform, 96f, 34f);
            if (_rowsBound != null) _rowsBound(Root);
        }

        internal void Hide()
        {
            Root.gameObject.SetActive(false);
            if (_showTooltip != null) _showTooltip(string.Empty);
        }
    }

    // Casting Order view: numbered child assignments with explicit
    // Earlier/Later controls, resolved provider/pin status, per-pool resource
    // accounting, competing demand, the read-only combined forecast, and the
    // player-facing assignment editor (add/remove rows, pin casters, move or
    // split targets, per-assignment enhancements). All content derives from
    // the planner's authoritative results; the view keeps no competing
    // calculations of its own.
    internal sealed class PlannerCastingOrderView
    {
        private const float RowHeight = 84f;

        private readonly PlannerUiTheme _theme;
        private readonly RectTransform _frame;
        private readonly RectTransform _content;
        private readonly ScrollRect _scroll;
        private readonly RectTransform _viewport;
        private readonly Scrollbar _scrollbar;
        private readonly Text _resources;
        private readonly Text _forecast;
        private readonly Button _closeButton;
        private readonly Button _addButton;
        private Button _reverseButton;
        private readonly Action<string> _showTooltip;
        private readonly Action<RectTransform> _rowsBound;
        private readonly Func<string, IReadOnlyList<CastingAssignmentRowViewModel>> _rows;
        private readonly Func<string, IReadOnlyList<ResourceUsageLineViewModel>> _resourceLines;
        private readonly Func<IReadOnlyList<string>, SequentialForecastPlanner.Result> _sequenceForecast;
        private readonly Action<string> _moveEarlier;
        private readonly Action<string> _moveLater;
        private readonly Action _refresh;
        private readonly Action _addAssignment;
        private readonly Action<string> _removeAssignment;
        private readonly Action<string> _cycleCaster;
        private readonly Action<string, string> _removeTarget;
        private readonly Action<string, string> _splitTarget;
        private readonly Action<string, string, string> _moveTarget;
        private readonly Action<string, string> _openAssignmentEnhancements;
        private readonly Action<string, string> _openAssignmentTargets;
        private IReadOnlyList<CastingAssignmentRowViewModel> _currentRows =
            new CastingAssignmentRowViewModel[0];
        private IReadOnlyList<CastingOrderLayout.RowPlan> _currentPlan =
            new CastingOrderLayout.RowPlan[0];
        private readonly List<GameObject> _rowObjects = new List<GameObject>();
        private readonly List<Button> _routineToggles = new List<Button>();
        private readonly List<float> _rowHeights = new List<float>();
        private readonly List<string> _forecastSequence = new List<string>();
        private bool _forecastReversed;
        private string _routineId;

        internal PlannerCastingOrderView(
            RectTransform parent,
            PlannerUiTheme theme,
            Func<string, IReadOnlyList<CastingAssignmentRowViewModel>> rows,
            Func<string, IReadOnlyList<ResourceUsageLineViewModel>> resourceLines,
            Func<IReadOnlyList<string>, SequentialForecastPlanner.Result> sequenceForecast,
            Action<string> moveEarlier,
            Action<string> moveLater,
            Action refresh,
            Action<string> showTooltip,
            Action<RectTransform> rowsBound = null,
            Action addAssignment = null,
            Action<string> removeAssignment = null,
            Action<string> cycleCaster = null,
            Action<string, string> removeTarget = null,
            Action<string, string> splitTarget = null,
            Action<string, string, string> moveTarget = null,
            Action<string, string> openAssignmentEnhancements = null,
            Action<string, string> openAssignmentTargets = null)
        {
            _theme = theme;
            _rows = rows;
            _resourceLines = resourceLines;
            _sequenceForecast = sequenceForecast;
            _moveEarlier = moveEarlier;
            _moveLater = moveLater;
            _refresh = refresh;
            _addAssignment = addAssignment;
            _removeAssignment = removeAssignment;
            _cycleCaster = cycleCaster;
            _removeTarget = removeTarget;
            _splitTarget = splitTarget;
            _moveTarget = moveTarget;
            _openAssignmentEnhancements = openAssignmentEnhancements;
            _openAssignmentTargets = openAssignmentTargets;
            _showTooltip = showTooltip;
            _rowsBound = rowsBound;
            Root = KingmakerUiFactory.CreateRect("CastingOrderView", parent);
            KingmakerUiFactory.Stretch(Root);
            Image blocker = Root.gameObject.AddComponent<Image>();
            blocker.color = new Color(0.035f, 0.025f, 0.02f, 0.72f);
            blocker.raycastTarget = true;
            Button outside = Root.gameObject.AddComponent<Button>();
            outside.onClick.AddListener(Hide);

            _frame = KingmakerUiFactory.CreateRect("CastingOrderFrame", Root);
            KingmakerUiFactory.SetAnchors(_frame, 0.08f, 0.05f, 0.92f, 0.95f);
            KingmakerUiFactory.AddFramedPanel(_frame, theme.ParchmentRaised,
                theme.BurgundyPrimary, 2f).raycastTarget = true;
            Text title = KingmakerUiFactory.CreateText("CastingOrderTitle", _frame, theme,
                "ASSIGNMENTS & RESOURCES", 24, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            title.color = theme.BurgundyPrimary;
            KingmakerUiFactory.SetAnchors(title.rectTransform, 0.03f, 0.955f, 0.42f, 0.99f);
            _addButton = KingmakerUiFactory.CreateButton("AddAssignment", _frame,
                theme, "ADD ASSIGNMENT", () =>
                {
                    if (_addAssignment != null)
                    {
                        _addAssignment();
                        // Refresh so the new row is immediately visible.
                        _refresh();
                    }
                });
            KingmakerUiFactory.SetAnchors((RectTransform)_addButton.transform,
                0.43f, 0.955f, 0.585f, 0.99f);
            _addButton.interactable = false;
            _closeButton = KingmakerUiFactory.CreateButton("CloseCastingOrder", _frame,
                theme, "CLOSE", Hide);
            KingmakerUiFactory.SetAnchors((RectTransform)_closeButton.transform,
                0.86f, 0.955f, 0.985f, 0.99f);
            BuildRoutineToggles(_frame);
            _scroll = KingmakerUiFactory.CreateScrollView("CastingOrderRows",
                _frame, theme, out _content,
                ChooserScrollLayoutContract.ScrollbarWidth);
            _viewport = _scroll.viewport;
            _scrollbar = _scroll.verticalScrollbar;
            KingmakerUiFactory.SetAnchors((RectTransform)_scroll.transform,
                0.03f, 0.335f, 0.97f, 0.895f);
            _resources = KingmakerUiFactory.CreateText("CastingOrderResources", _frame, theme,
                string.Empty, 14, TextAnchor.UpperLeft);
            _resources.color = theme.DarkBrownText;
            _resources.horizontalOverflow = HorizontalWrapMode.Wrap;
            _resources.verticalOverflow = VerticalWrapMode.Overflow;
            KingmakerUiFactory.SetAnchors(_resources.rectTransform, 0.03f, 0.185f, 0.97f, 0.325f);
            _forecast = KingmakerUiFactory.CreateText("CastingOrderForecast", _frame, theme,
                string.Empty, 13, TextAnchor.UpperLeft);
            _forecast.color = theme.MutedBrownText;
            _forecast.horizontalOverflow = HorizontalWrapMode.Wrap;
            _forecast.verticalOverflow = VerticalWrapMode.Overflow;
            KingmakerUiFactory.SetAnchors(_forecast.rectTransform, 0.03f, 0.01f, 0.97f, 0.175f);
            PlannerDescriptionEscape escape = Root.gameObject.AddComponent<PlannerDescriptionEscape>();
            escape.Close = Hide;
            Root.gameObject.SetActive(false);
        }

        private void BuildRoutineToggles(RectTransform frame)
        {
            RectTransform bar = KingmakerUiFactory.CreateRect("ForecastToggles", frame);
            KingmakerUiFactory.SetAnchors(bar, 0.03f, 0.905f, 0.97f, 0.955f);
            Text label = KingmakerUiFactory.CreateText("ForecastLabel", bar, _theme,
                "FORECAST (one run per selected routine):", 13, TextAnchor.MiddleLeft);
            label.color = _theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(label.rectTransform, 0f, 0.1f, 0.42f, 0.9f);
            string[] routines = { "long", "important", "short" };
            for (int index = 0; index < routines.Length; index++)
            {
                string routineId = routines[index];
                Button toggle = KingmakerUiFactory.CreateButton(
                    "ForecastToggle." + routineId, bar, _theme, routineId.ToUpperInvariant(),
                    () =>
                    {
                        // Selection order is the player's explicit sequence.
                        if (_forecastSequence.Contains(routineId))
                            _forecastSequence.Remove(routineId);
                        else _forecastSequence.Add(routineId);
                        BindForecast();
                    });
                KingmakerUiFactory.SetAnchors((RectTransform)toggle.transform,
                    0.44f + index * 0.14f, 0.05f, 0.57f + index * 0.14f, 0.95f);
                _routineToggles.Add(toggle);
            }
            Button reverse = KingmakerUiFactory.CreateButton(
                "ForecastReverse", bar, _theme, "REVERSE",
                () =>
                {
                    _forecastSequence.Reverse();
                    _forecastReversed = !_forecastReversed;
                    BindForecast();
                });
            KingmakerUiFactory.SetAnchors((RectTransform)reverse.transform,
                0.87f, 0.05f, 0.995f, 0.95f);
            _reverseButton = reverse;
        }

        internal RectTransform Root { get; private set; }
        internal RectTransform PaperSurface { get { return _frame; } }
        internal bool IsOpen { get { return Root.gameObject.activeSelf; } }

        internal void Show(string routineId)
        {
            _routineId = routineId;
            bool refresh = Root.gameObject.activeSelf;
            float previousOffset = refresh ? _content.anchoredPosition.y : 0f;
            BindRows(refresh, previousOffset);
            BindResources();
            BindForecast();
            Root.SetAsLastSibling();
            Root.gameObject.SetActive(true);
            KingmakerUiFactory.ForceLayoutAndSnap(Root);
            KingmakerUiFactory.FitButtonToCaption(
                (RectTransform)_closeButton.transform, 96f, 34f);
            if (_rowsBound != null) _rowsBound(Root);
        }

        internal void Hide()
        {
            Root.gameObject.SetActive(false);
            if (_showTooltip != null) _showTooltip(string.Empty);
        }

        private void BindRows(bool refresh, float previousOffset)
        {
            foreach (GameObject row in _rowObjects)
            {
                if (row == null) continue;
                row.SetActive(false);
                UnityEngine.Object.Destroy(row);
            }
            _rowObjects.Clear();
            _rowHeights.Clear();
            IReadOnlyList<CastingAssignmentRowViewModel> rows = _rows(_routineId);
            _currentRows = rows;
            IReadOnlyList<CastingOrderLayout.RowPlan> plan =
                CastingOrderLayout.PlanRows(rows);
            _currentPlan = plan;
            foreach (CastingOrderLayout.RowPlan rowPlan in plan)
            {
                CastingAssignmentRowViewModel model = rows.First(candidate =>
                    candidate.AssignmentId == rowPlan.AssignmentId);
                float height = rowPlan.IsTargetRow
                    ? CastingOrderLayout.TargetRowHeight : CastingOrderLayout.HeaderRowHeight;
                _rowHeights.Add(height);
                BuildRow(model, rowPlan);
            }
            if (_addButton != null)
                _addButton.interactable = _addAssignment != null;
            float viewportHeight = Mathf.Max(0f, _viewport.rect.height);
            float contentHeight = CastingOrderLayout.TotalHeight(plan);
            if (contentHeight > 0f)
                contentHeight += (ChooserScrollLayoutContract.ContentPadding * 2f) +
                    (ChooserScrollLayoutContract.RowSpacing * (plan.Count - 1));
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            _content.anchoredPosition = new Vector2(0f, ChooserScrollLayoutContract
                .ClampScrollOffset(previousOffset, viewportHeight, contentHeight));
            if (_scrollbar != null)
                _scrollbar.size = ChooserScrollLayoutContract.ScrollbarHandleRatio(
                    viewportHeight, contentHeight);
        }

        private void BuildRow(CastingAssignmentRowViewModel model,
            CastingOrderLayout.RowPlan rowPlan)
        {
            RectTransform row = KingmakerUiFactory.CreateRect(
                (rowPlan.IsTargetRow ? "Target." + rowPlan.UnitId
                    : "Assignment." + model.AssignmentId), _content);
            KingmakerUiFactory.AddLayout(row, rowPlan.IsTargetRow
                ? CastingOrderLayout.TargetRowHeight : CastingOrderLayout.HeaderRowHeight);
            KingmakerUiFactory.AddFramedPanel(row,
                rowPlan.IsTargetRow ? _theme.ParchmentPanel :
                    model.PinUnresolved ? _theme.AmberWarning :
                    model.UnfulfilledTargets > 0 ? _theme.DisabledGray : _theme.ParchmentPanel,
                _theme.MutedBrownText);
            if (rowPlan.IsTargetRow)
            {
                BuildTargetRow(row, model, rowPlan);
            }
            else
            {
                string routing = model.ResolvedProviderTexts.Count == 0
                    ? string.Empty
                    : " | via " + string.Join(", ", model.ResolvedProviderTexts.ToArray()) +
                        " -> " + (model.RecipientNames.Count == 0 ? "no recipients"
                            : string.Join(", ", model.RecipientNames.ToArray()));
                Text identity = KingmakerUiFactory.CreateText("Identity", row, _theme,
                    model.Number + ". " + model.SourceDisplayName + " — " + model.CasterText +
                    " | targets " + model.TargetNames.Count + routing,
                    14, TextAnchor.MiddleLeft);
                identity.horizontalOverflow = HorizontalWrapMode.Wrap;
                identity.verticalOverflow = VerticalWrapMode.Overflow;
                KingmakerUiFactory.SetAnchors(identity.rectTransform, 0.01f, 0.74f, 0.99f, 0.96f);
                Text status = KingmakerUiFactory.CreateText("Status", row, _theme,
                    model.Status, 13, TextAnchor.UpperRight);
                status.color = model.PinUnresolved || model.UnfulfilledTargets > 0
                    ? _theme.AmberWarning : _theme.GreenSuccess;
                status.horizontalOverflow = HorizontalWrapMode.Wrap;
                status.verticalOverflow = VerticalWrapMode.Overflow;
                KingmakerUiFactory.SetAnchors(status.rectTransform, 0.71f, 0.74f, 0.99f, 0.96f);
                if (model.Editable)
                {
                    Button targets = KingmakerUiFactory.CreateButton("Targets", row, _theme,
                        "+ TARGETS", () =>
                        {
                            if (_openAssignmentTargets != null)
                                _openAssignmentTargets(model.SourceId, model.AssignmentId);
                        });
                    KingmakerUiFactory.SetAnchors((RectTransform)targets.transform,
                        0.01f, 0.06f, 0.14f, 0.46f);
                    Text targetsLabel = targets.GetComponentInChildren<Text>(true);
                    if (targetsLabel != null) targetsLabel.fontSize = 11;
                    Button caster = KingmakerUiFactory.CreateButton("Caster", row, _theme,
                        "CASTER", () =>
                        {
                            if (_cycleCaster != null) _cycleCaster(model.AssignmentId);
                            _refresh();
                        });
                    KingmakerUiFactory.SetAnchors((RectTransform)caster.transform,
                        0.15f, 0.06f, 0.28f, 0.46f);
                    Text casterLabel = caster.GetComponentInChildren<Text>(true);
                    if (casterLabel != null) casterLabel.fontSize = 11;
                    Button enhance = KingmakerUiFactory.CreateButton("Enhance", row, _theme,
                        "ENHANCE", () =>
                        {
                            if (_openAssignmentEnhancements != null)
                                _openAssignmentEnhancements(model.SourceId, model.AssignmentId);
                        });
                    KingmakerUiFactory.SetAnchors((RectTransform)enhance.transform,
                        0.29f, 0.06f, 0.42f, 0.46f);
                    Text enhanceLabel = enhance.GetComponentInChildren<Text>(true);
                    if (enhanceLabel != null) enhanceLabel.fontSize = 11;
                    Button removeRow = KingmakerUiFactory.CreateButton("RemoveRow", row, _theme,
                        "REMOVE", () =>
                        {
                            if (_removeAssignment != null) _removeAssignment(model.AssignmentId);
                            _refresh();
                        });
                    KingmakerUiFactory.SetAnchors((RectTransform)removeRow.transform,
                        0.43f, 0.06f, 0.56f, 0.46f);
                    Text removeLabel = removeRow.GetComponentInChildren<Text>(true);
                    if (removeLabel != null) removeLabel.fontSize = 11;
                }
                Button earlier = KingmakerUiFactory.CreateButton("Earlier", row, _theme,
                    "EARLIER", () =>
                    {
                        _moveEarlier(model.AssignmentId);
                        _refresh();
                    });
                KingmakerUiFactory.SetAnchors((RectTransform)earlier.transform,
                    0.71f, 0.04f, 0.85f, 0.46f);
                earlier.interactable = model.CanMoveEarlier;
                Text earlierLabel = earlier.GetComponentInChildren<Text>(true);
                if (earlierLabel != null) earlierLabel.fontSize = 11;
                Button later = KingmakerUiFactory.CreateButton("Later", row, _theme,
                    "LATER", () =>
                    {
                        _moveLater(model.AssignmentId);
                        _refresh();
                    });
                KingmakerUiFactory.SetAnchors((RectTransform)later.transform,
                    0.86f, 0.04f, 0.99f, 0.46f);
                later.interactable = model.CanMoveLater;
                Text laterLabel = later.GetComponentInChildren<Text>(true);
                if (laterLabel != null) laterLabel.fontSize = 11;
                Text enhancements = KingmakerUiFactory.CreateText("Enhancements", row, _theme,
                    "Enhancements: " + (model.EnhancementTexts.Count == 0
                        ? "none" : string.Join(", ", model.EnhancementTexts.ToArray())),
                    12, TextAnchor.MiddleLeft);
                enhancements.color = _theme.MutedBrownText;
                enhancements.horizontalOverflow = HorizontalWrapMode.Wrap;
                enhancements.verticalOverflow = VerticalWrapMode.Overflow;
                KingmakerUiFactory.SetAnchors(enhancements.rectTransform, 0.01f, 0.50f, 0.99f, 0.72f);
                // Identity/status live in 0.74..0.96; the enhancement band
                // 0.50..0.72 never intersects them.
            }
            _rowObjects.Add(row.gameObject);
        }

        private void BuildTargetRow(RectTransform row,
            CastingAssignmentRowViewModel model, CastingOrderLayout.RowPlan rowPlan)
        {
            string unitId = rowPlan.UnitId;
            int unitIndex = ((IList<string>)model.TargetUnitIds).IndexOf(unitId);
            string displayName = unitIndex >= 0 && unitIndex < model.TargetNames.Count
                ? model.TargetNames[unitIndex] : unitId;
            Text label = KingmakerUiFactory.CreateText("TargetName", row, _theme,
                "• " + displayName, 13, TextAnchor.MiddleLeft);
            KingmakerUiFactory.SetAnchors(label.rectTransform, 0.01f, 0.1f, 0.40f, 0.9f);
            if (!model.Editable) return;
            List<CastingAssignmentRowViewModel> siblings = _currentRows
                .Where(candidate => candidate.SourceId == model.SourceId)
                .OrderBy(candidate => candidate.Order)
                .ToList();
            int siblingIndex = siblings.FindIndex(candidate =>
                candidate.AssignmentId == model.AssignmentId);
            CastingAssignmentRowViewModel destination = siblings.Count < 2 ? null
                : siblings[(siblingIndex + 1) % siblings.Count];
            Button remove = KingmakerUiFactory.CreateButton("RemoveTarget", row, _theme,
                "X", () =>
                {
                    if (_removeTarget != null) _removeTarget(model.AssignmentId, unitId);
                    _refresh();
                });
            KingmakerUiFactory.SetAnchors((RectTransform)remove.transform,
                0.41f, 0.08f, 0.53f, 0.92f);
            Text removeLabel = remove.GetComponentInChildren<Text>(true);
            if (removeLabel != null) removeLabel.fontSize = 11;
            Button move = KingmakerUiFactory.CreateButton("MoveTarget", row, _theme,
                destination == null ? "MOVE" : "MOVE -> " + destination.Number,
                () =>
                {
                    if (_moveTarget != null && destination != null)
                        _moveTarget(model.AssignmentId, destination.AssignmentId, unitId);
                    _refresh();
                });
            KingmakerUiFactory.SetAnchors((RectTransform)move.transform,
                0.54f, 0.08f, 0.76f, 0.92f);
            move.interactable = destination != null;
            Text moveLabel = move.GetComponentInChildren<Text>(true);
            if (moveLabel != null) moveLabel.fontSize = 11;
            Button split = KingmakerUiFactory.CreateButton("SplitTarget", row, _theme,
                "SPLIT", () =>
                {
                    if (_splitTarget != null) _splitTarget(model.AssignmentId, unitId);
                    _refresh();
                });
            KingmakerUiFactory.SetAnchors((RectTransform)split.transform,
                0.77f, 0.08f, 0.89f, 0.92f);
            Text splitLabel = split.GetComponentInChildren<Text>(true);
            if (splitLabel != null) splitLabel.fontSize = 11;
        }

        private void BindResources()
        {
            IReadOnlyList<ResourceUsageLineViewModel> lines = _resourceLines(_routineId);
            _resources.text = lines.Count == 0
                ? "No limited resource usage in this routine."
                : "RESOURCES — " + string.Join("\n", lines
                    .Select(line => line.Summary).ToArray());
        }

        private void BindForecast()
        {
            string[] orderNames = { "long", "important", "short" };
            for (int index = 0; index < _routineToggles.Count; index++)
            {
                Button toggle = _routineToggles[index];
                if (toggle == null) continue;
                int position = _forecastSequence.IndexOf(orderNames[index]);
                toggle.image.color = position >= 0 ? _theme.GreenSuccess : _theme.ParchmentRaised;
                Text toggleLabel = toggle.GetComponentInChildren<Text>(true);
                if (toggleLabel != null)
                    toggleLabel.text = orderNames[index].ToUpperInvariant() +
                        (position >= 0 ? " " + (position + 1) : string.Empty);
            }
            if (_reverseButton != null)
                _reverseButton.image.color = _forecastReversed
                    ? _theme.GreenSuccess : _theme.ParchmentRaised;
            if (_forecastSequence.Count == 0)
            {
                _forecast.text = "Select routines above in order for a combined " +
                    "forecast. " + SequentialForecastPlanner.Result.AssumptionText;
                return;
            }
            SequentialForecastPlanner.Result forecast = _sequenceForecast(_forecastSequence);
            var builder = new System.Text.StringBuilder("FORECAST — ");
            foreach (SequentialForecastPlanner.Occurrence occurrence in forecast.Occurrences)
            {
                int unmet = occurrence.Plan.Outcomes.Count(o =>
                    o.Kind == TargetOutcomeKind.Unfulfilled);
                builder.Append(occurrence.RoutineId).Append(": ")
                    .Append(occurrence.Plan.Steps.Count).Append(" cast(s), ")
                    .Append(occurrence.Plan.Outcomes.Count(o =>
                        o.Kind == TargetOutcomeKind.Fulfilled)).Append(" covered, ")
                    .Append(unmet).Append(" unmet; ");
            }
            foreach (KeyValuePair<string, int> balance in forecast.ForecastRemainingByNativePool
                .Where(pair => pair.Value >= 0)
                .OrderBy(pair => pair.Key, StringComparer.Ordinal))
                builder.Append("\n").Append(balance.Key).Append(" forecast remaining ")
                    .Append(balance.Value);
            builder.Append("\n").Append(
                SequentialForecastPlanner.Result.AssumptionText);
            _forecast.text = builder.ToString();
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
                CastingPanelLayoutContract.SettingsCloseLabel, () => close());
            KingmakerUiFactory.SetAnchors((RectTransform)done.transform, 0.72f, 0.015f, 0.94f, 0.12f);
            Root.gameObject.SetActive(false);
        }

        internal RectTransform Root { get; private set; }
        internal bool IsOpen { get { return Root.gameObject.activeSelf; } }
        internal void Show(bool value) { Root.gameObject.SetActive(value); }

        internal bool InvokeCastingModeForRuntime()
        {
            if (_mode == null || !_mode.interactable) return false;
            _mode.onClick.Invoke();
            return true;
        }

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

    internal sealed class PlannerRightClickHandler : MonoBehaviour,
        UnityEngine.EventSystems.IPointerClickHandler
    {
        internal string SourceId;
        internal Action<string> Inspect;

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (eventData == null ||
                eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Right)
                return;
            eventData.Use();
            if (Inspect != null && !string.IsNullOrWhiteSpace(SourceId))
                Inspect(SourceId);
        }
    }

    internal sealed class PlannerCardChildClickForwarder : MonoBehaviour,
        UnityEngine.EventSystems.IPointerClickHandler
    {
        internal Button Owner;

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (eventData == null || Owner == null || !Owner.interactable ||
                eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Left)
                return;
            Owner.onClick.Invoke();
            eventData.Use();
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
