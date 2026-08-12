using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class BuffPlannerScreenView : IDisposable
    {
        internal const string RootName = "KingmakerBuffPlanner.FullScreenRoot";
        private readonly PlannerUiSession _session;
        private readonly BuffPlannerUiLifecycleDiagnostics _diagnostics;
        private readonly Action _close;
        private readonly Action<string> _execute;
        private readonly PlannerUiTheme _theme;
        private RectTransform _root;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private GraphicRaycaster _raycaster;
        private Image _blocker;
        private RectTransform _sourceContent;
        private RectTransform _sourceViewport;
        private RectTransform _detailContent;
        private RectTransform _detailViewport;
        private Text _status;
        private Text _result;
        private Text _catalogSummary;
        private InputField _search;
        private Button[] _routineTabs;
        private Text _configuredFilterLabel;
        private Text _hiddenFilterLabel;
        private Text[] _advancedFilterLabels;
        private RectTransform _advancedFilters;
        private RectTransform _catalogScrollRoot;
        private readonly List<Button> _sourceRows = new List<Button>();
        private readonly Dictionary<string, TargetPortraitVisual> _targetVisuals =
            new Dictionary<string, TargetPortraitVisual>(StringComparer.Ordinal);
        private bool _advancedFiltersExpanded;
        private bool _advancedCastingExpanded;
        private string _routineId = "long";
        private readonly CatalogFilterState _filters = new CatalogFilterState();
        private bool _resettingFilters;
        private bool _disposed;

        internal BuffPlannerScreenView(
            StaticCanvas nativeCanvas,
            PlannerUiSession session,
            BuffPlannerUiLifecycleDiagnostics diagnostics,
            Action close,
            Action<string> execute)
        {
            if (nativeCanvas == null) throw new ArgumentNullException("nativeCanvas");
            _session = session ?? throw new ArgumentNullException("session");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
            _close = close ?? throw new ArgumentNullException("close");
            _execute = execute ?? throw new ArgumentNullException("execute");
            _theme = PlannerUiTheme.Resolve(nativeCanvas);
            try
            {
                Build(nativeCanvas);
                RefreshAll();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal bool IsAlive { get { return !_disposed && _root != null; } }
        internal GameObject RootObject { get { return _root == null ? null : _root.gameObject; } }
        internal bool IsOpaque { get; private set; }
        internal bool BlocksRaycasts { get; private set; }
        internal bool HasGraphicRaycaster { get; private set; }
        internal int RootCount
        {
            get
            {
                return Resources.FindObjectsOfTypeAll<PlannerScreenMarker>()
                    .Count(marker => marker != null && marker.gameObject.name == RootName);
            }
        }
        internal string ActiveRoutineId { get { return _routineId; } }
        internal PlannerPresentationValidation LastValidation { get; private set; }
        internal CatalogLayoutDiagnostics LastCatalogDiagnostics { get; private set; }

        internal PlannerPresentationValidation ValidatePresentation()
        {
            if (_root == null)
                return LastValidation = PlannerPresentationValidation.Failed("root-null");
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
            Canvas.ForceUpdateCanvases();
            Vector3[] corners = new Vector3[4];
            _root.GetWorldCorners(corners);
            float minX = corners.Min(corner => corner.x);
            float maxX = corners.Max(corner => corner.x);
            float minY = corners.Min(corner => corner.y);
            float maxY = corners.Max(corner => corner.y);
            float coveredWidth = Mathf.Max(0, Mathf.Min(Screen.width, maxX) - Mathf.Max(0, minX));
            float coveredHeight = Mathf.Max(0, Mathf.Min(Screen.height, maxY) - Mathf.Max(0, minY));
            float coverage = Screen.width <= 0 || Screen.height <= 0 ? 0 :
                (coveredWidth * coveredHeight) / (Screen.width * (float)Screen.height);
            EventSystem eventSystem = EventSystem.current;
            bool ownsCenterRaycast = false;
            string topRaycast = string.Empty;
            if (eventSystem != null)
            {
                var eventData = new PointerEventData(eventSystem)
                {
                    position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
                };
                var hits = new List<RaycastResult>();
                eventSystem.RaycastAll(eventData, hits);
                if (hits.Count != 0 && hits[0].gameObject != null)
                {
                    topRaycast = GetPath(hits[0].gameObject.transform);
                    ownsCenterRaycast = hits[0].gameObject.transform == _root ||
                        hits[0].gameObject.transform.IsChildOf(_root);
                }
            }
            bool controls = _root.Find("ServiceFrame/Header/Close") != null &&
                _root.Find("ServiceFrame/RoutineTabs") != null &&
                _root.Find("ServiceFrame/Footer/Execute") != null;
            string failure = !_root.gameObject.activeSelf ? "root-inactive-self" :
                !_root.gameObject.activeInHierarchy ? "root-inactive-hierarchy" :
                _canvas == null || !_canvas.isActiveAndEnabled ? "canvas-inactive" :
                _canvas.rootCanvas != _canvas ? "canvas-not-root" :
                _canvasGroup == null || _canvasGroup.alpha < 0.999f ? "canvas-transparent" :
                !_canvasGroup.interactable || !_canvasGroup.blocksRaycasts ? "canvas-group-not-interactive" :
                _blocker == null || !_blocker.raycastTarget || _blocker.color.a < 0.999f ? "blocker-invalid" :
                _raycaster == null || !_raycaster.isActiveAndEnabled ? "raycaster-inactive" :
                eventSystem == null ? "event-system-absent" :
                _root.rect.width <= 1 || _root.rect.height <= 1 ? "root-zero-size" :
                coverage < 0.98f ? "screen-coverage:" + coverage.ToString("F4") :
                !controls ? "required-controls-absent" :
                !ownsCenterRaycast ? "center-raycast-not-owned:" + topRaycast : string.Empty;
            LastValidation = new PlannerPresentationValidation(
                string.IsNullOrEmpty(failure), failure, _root.GetInstanceID(),
                _root.gameObject.activeSelf, _root.gameObject.activeInHierarchy,
                GetPath(_root), _canvas == null ? string.Empty : _canvas.name,
                _canvas == null ? string.Empty : _canvas.renderMode.ToString(),
                _canvas == null ? 0 : _canvas.sortingOrder,
                _canvas != null && _canvas.overrideSorting,
                _root.anchorMin, _root.anchorMax, _root.pivot, _root.sizeDelta,
                _root.rect.size, corners, Screen.width, Screen.height, coverage,
                _canvasGroup == null ? 0 : _canvasGroup.alpha,
                _canvasGroup != null && _canvasGroup.interactable,
                _canvasGroup != null && _canvasGroup.blocksRaycasts,
                _blocker != null && _blocker.raycastTarget,
                _blocker == null ? 0 : _blocker.color.a,
                _raycaster == null ? string.Empty : GetPath(_raycaster.transform),
                eventSystem == null ? string.Empty : GetPath(eventSystem.transform),
                ownsCenterRaycast, topRaycast);
            return LastValidation;
        }

        internal bool DispatchRoutineTabForRuntime(string routineId)
        {
            int index = routineId == "long" ? 0 : routineId == "important" ? 1 :
                routineId == "short" ? 2 : -1;
            if (index < 0 || _routineTabs == null || index >= _routineTabs.Length ||
                _routineTabs[index] == null || EventSystem.current == null) return false;
            var click = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left
            };
            ExecuteEvents.Execute(_routineTabs[index].gameObject, click,
                ExecuteEvents.pointerClickHandler);
            return true;
        }

        internal void RefreshAll()
        {
            if (!IsAlive) return;
            _status.text = _session.Status;
            PlannerSetupModel model = _session.Model;
            if (model == null)
            {
                _result.text = "Load a campaign to configure and execute buff routines. " +
                    "The planner never writes to a Kingmaker save.";
            }
            else if (!string.IsNullOrWhiteSpace(_session.ProfileStatus))
                _result.text = _session.ProfileStatus;
            RefreshTabs();
            RefreshCatalog();
        }

        internal bool DispatchBlessRowForRuntime()
        {
            if (_session.Model == null || EventSystem.current == null) return false;
            const string bless = "90e59f4a4ada87243b7b3535a06d0638";
            SetupSourceRow source = _session.Model.Sources
                .Where(item => item.Ability.BaseAbilityGuid == bless ||
                    item.Ability.VariantGuid == bless)
                .OrderBy(item => item.Ability.SourceKind == SourceKind.Spellbook ? 0 : 1)
                .FirstOrDefault();
            Button row = source == null ? null : _sourceRows.FirstOrDefault(item =>
                item != null && item.name == "Source." + source.SourceId);
            if (row == null || !row.gameObject.activeInHierarchy) return false;
            var click = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null,
                    ((RectTransform)row.transform).TransformPoint(row.transform.localPosition))
            };
            ExecuteEvents.Execute(row.gameObject, click, ExecuteEvents.pointerClickHandler);
            return _session.Model.SelectedSourceId == source.SourceId &&
                GetCatalogDiagnostics().SelectedDetailsBound;
        }

        internal bool SelectFirstRowForRuntime()
        {
            Button row = _sourceRows.FirstOrDefault(item => item != null &&
                item.gameObject.activeInHierarchy);
            if (row == null || _session.Model == null) return false;
            SetupSourceRow source = _session.Model.Sources.FirstOrDefault(item =>
                row.name == "Source." + item.SourceId);
            if (source == null) return false;
            _session.Model.SelectSource(source.SourceId);
            RefreshCatalog();
            return _session.Model.SelectedSourceId == source.SourceId;
        }

        internal LiveRowRenderDiagnostics GetLiveRowRenderDiagnostics()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_sourceContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_sourceViewport);
            Canvas.ForceUpdateCanvases();
            PlannerSetupModel model = _session.Model;
            var rows = _sourceRows.Where(item => item != null).Take(5).ToList();
            Text detailsTitle = _detailContent == null ? null :
                _detailContent.GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(item => item.name == "Heading");
            return new LiveRowRenderDiagnostics
            {
                ExpectedNames = rows.Select(item => RowDisplayName(model, item)).ToArray(),
                RowScreenRectangles = rows.Select(item =>
                    ScreenRectangle((RectTransform)item.transform)).ToArray(),
                SelectedRowName = model == null || model.SelectedSource == null ? string.Empty :
                    model.SelectedSource.DisplayName,
                DetailsTitleText = detailsTitle == null ? string.Empty : detailsTitle.text,
                BoundRowCount = _sourceRows.Count,
                SourceViewport = RectEvidence(_sourceViewport),
                SourceContent = RectEvidence(_sourceContent),
                MaskEvidence = BuildMaskEvidence(),
                CanaryEvidence = _sourceContent == null ||
                    _sourceContent.Find("KBP.RenderCanary") == null ? "absent" : "present",
                RowEvidence = rows.Select(item => BuildGraphicEvidence(item.gameObject)).ToArray(),
                DetailsEvidence = _detailContent == null ? new string[0] :
                    _detailContent.GetComponentsInChildren<Graphic>(true).Take(8)
                        .Select(item => BuildGraphicEvidence(item.gameObject)).ToArray()
            };
        }

        internal void RefreshCatalogForRuntime()
        {
            RefreshCatalog();
        }

        internal void ShowResult(QuickExecutionResult result)
        {
            if (_result != null)
                _result.text = result == null ? _session.Status : result.Message;
            if (_status != null) _status.text = _session.Status;
            RefreshTabs();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_root != null)
            {
                PlannerPointerOwnership.Unregister(_root);
                UnityEngine.Object.Destroy(_root.gameObject);
                _diagnostics.RecordScreenDestroyed();
            }
            _root = null;
        }

        private void Build(StaticCanvas nativeCanvas)
        {
            foreach (PlannerScreenMarker existing in Resources.FindObjectsOfTypeAll<PlannerScreenMarker>())
                if (existing != null && existing.gameObject.name == RootName)
                    UnityEngine.Object.Destroy(existing.gameObject);
            _root = KingmakerUiFactory.CreateRect(RootName, null);
            KingmakerUiFactory.Stretch(_root);
            _root.gameObject.AddComponent<PlannerScreenMarker>();
            _canvas = _root.gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 32000;
            CanvasScaler scaler = _root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            _raycaster = _root.gameObject.AddComponent<GraphicRaycaster>();
            _raycaster.ignoreReversedGraphics = true;
            _canvasGroup = _root.gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _blocker = KingmakerUiFactory.AddPanel(_root,
                _theme.ParchmentBackgroundSprite == null ? _theme.ParchmentBackground : Color.white,
                _theme.ParchmentBackgroundSprite);
            _blocker.raycastTarget = true;
            PlannerPointerSink sink = _root.gameObject.AddComponent<PlannerPointerSink>();
            sink.Diagnostics = _diagnostics;
            IsOpaque = _blocker.color.a >= 0.999f;
            BlocksRaycasts = _blocker.raycastTarget && _canvasGroup.blocksRaycasts && _canvasGroup.interactable;
            HasGraphicRaycaster = _raycaster.isActiveAndEnabled;

            RectTransform frame = KingmakerUiFactory.CreateRect("ServiceFrame", _root);
            KingmakerUiFactory.SetAnchors(frame, 0.025f, 0.025f, 0.975f, 0.975f);
            KingmakerUiFactory.AddFramedPanel(frame, _theme.ParchmentPanel,
                _theme.BurgundyPrimary, 2f);

            BuildHeader(frame);
            BuildRoutineTabs(frame);
            BuildLeftPanel(frame);
            BuildDetailPanel(frame);
            BuildFooter(frame);
            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
            PlannerPointerOwnership.Register(_root);
            _diagnostics.RecordScreenCreated();
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

        private void BuildHeader(RectTransform frame)
        {
            RectTransform header = KingmakerUiFactory.CreateRect("Header", frame);
            KingmakerUiFactory.SetAnchors(header, 0, 0.92f, 1, 1, 14, 14, 5, 5);
            KingmakerUiFactory.AddFramedPanel(header, _theme.ParchmentRaised,
                _theme.GoldAccent);
            Text title = KingmakerUiFactory.CreateText("Title", header, _theme,
                "BUFF PLANNER", 30, TextAnchor.MiddleCenter);
            KingmakerUiFactory.Stretch(title.rectTransform, 80, 80, 4, 4);
            _status = KingmakerUiFactory.CreateText("Status", header, _theme,
                string.Empty, 15, TextAnchor.LowerLeft);
            _status.color = _theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(_status.rectTransform, 0, 0, 0.72f, 0.45f, 12, 0, 2, 0);
            Button close = KingmakerUiFactory.CreateButton("Close", header, _theme, "X", () => _close());
            KingmakerUiFactory.SetAnchors((RectTransform)close.transform, 0.94f, 0.12f, 0.99f, 0.88f);
        }

        private void BuildRoutineTabs(RectTransform frame)
        {
            RectTransform tabs = KingmakerUiFactory.CreateRect("RoutineTabs", frame);
            KingmakerUiFactory.SetAnchors(tabs, 0.18f, 0.855f, 0.82f, 0.915f);
            HorizontalLayoutGroup layout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            _routineTabs = new[]
            {
                CreateRoutineTab(tabs, "long", "LONG"),
                CreateRoutineTab(tabs, "important", "IMPORTANT"),
                CreateRoutineTab(tabs, "short", "SHORT")
            };
        }

        private Button CreateRoutineTab(RectTransform parent, string id, string label)
        {
            return KingmakerUiFactory.CreateButton("Tab." + id, parent, _theme, label, () =>
            {
                _routineId = id;
                RefreshAll();
            });
        }

        private void BuildLeftPanel(RectTransform frame)
        {
            RectTransform panel = KingmakerUiFactory.CreateRect("CatalogPanel", frame);
            KingmakerUiFactory.SetAnchors(panel, 0.015f, 0.16f, 0.38f, 0.845f, 0, 8, 0, 0);
            KingmakerUiFactory.AddFramedPanel(panel, _theme.ParchmentPanel,
                _theme.GoldAccent);
            _search = KingmakerUiFactory.CreateInputField("Search", panel, _theme, "Search buffs...");
            KingmakerUiFactory.SetAnchors((RectTransform)_search.transform, 0.02f, 0.92f, 0.98f, 0.985f);
            _search.onValueChanged.AddListener(value =>
            {
                if (!_resettingFilters) RefreshCatalog();
            });

            RectTransform filters = KingmakerUiFactory.CreateRect("Filters", panel);
            KingmakerUiFactory.SetAnchors(filters, 0.02f, 0.84f, 0.98f, 0.91f);
            HorizontalLayoutGroup filterLayout = filters.gameObject.AddComponent<HorizontalLayoutGroup>();
            filterLayout.spacing = 5;
            filterLayout.childControlWidth = true;
            filterLayout.childForceExpandWidth = true;
            filterLayout.childControlHeight = true;
            filterLayout.childForceExpandHeight = true;
            Button configuredFilter = KingmakerUiFactory.CreateButton("Configured", filters, _theme,
                "Configured only", () =>
            {
                _filters.ConfiguredOnly = !_filters.ConfiguredOnly;
                RefreshCatalog();
            });
            Button hiddenFilter = KingmakerUiFactory.CreateButton("Hidden", filters, _theme,
                "Show hidden", () =>
            {
                _filters.ShowHidden = !_filters.ShowHidden;
                RefreshCatalog();
            });
            KingmakerUiFactory.CreateButton("ResetFilters", filters, _theme,
                "Reset", ResetFilters);
            KingmakerUiFactory.CreateButton("AdvancedFilters", filters, _theme,
                "Advanced Filters", () =>
            {
                _advancedFiltersExpanded = !_advancedFiltersExpanded;
                UpdateAdvancedFiltersVisibility();
                RefreshCatalog();
            });
            _configuredFilterLabel = configuredFilter.GetComponentInChildren<Text>();
            _hiddenFilterLabel = hiddenFilter.GetComponentInChildren<Text>();

            _advancedFilters = KingmakerUiFactory.CreateRect("AdvancedFilterDrawer", panel);
            KingmakerUiFactory.SetAnchors(_advancedFilters, 0.02f, 0.71f, 0.98f, 0.78f);
            HorizontalLayoutGroup advancedLayout =
                _advancedFilters.gameObject.AddComponent<HorizontalLayoutGroup>();
            advancedLayout.spacing = 5;
            advancedLayout.childControlWidth = true;
            advancedLayout.childForceExpandWidth = true;
            advancedLayout.childControlHeight = true;
            advancedLayout.childForceExpandHeight = true;
            Button sourceFilter = KingmakerUiFactory.CreateButton("SourceCategory",
                _advancedFilters, _theme, "All sources", () =>
            {
                _filters.SourceCategoryFilter = (_filters.SourceCategoryFilter + 1) % 3;
                RefreshCatalog();
            });
            Button durationFilter = KingmakerUiFactory.CreateButton("DurationCategory",
                _advancedFilters, _theme, "Any duration", () =>
            {
                _filters.DurationFilter = (_filters.DurationFilter + 1) % 3;
                RefreshCatalog();
            });
            Button availabilityFilter = KingmakerUiFactory.CreateButton("AvailabilityCategory",
                _advancedFilters, _theme, "All availability", () =>
            {
                _filters.AvailabilityFilter = (_filters.AvailabilityFilter + 1) % 3;
                RefreshCatalog();
            });
            _advancedFilterLabels = new[]
            {
                sourceFilter.GetComponentInChildren<Text>(),
                durationFilter.GetComponentInChildren<Text>(),
                availabilityFilter.GetComponentInChildren<Text>()
            };

            RectTransform filterStatus = KingmakerUiFactory.CreateRect("FilterStatus", panel);
            KingmakerUiFactory.SetAnchors(filterStatus, 0.02f, 0.78f, 0.98f, 0.835f);
            _catalogSummary = KingmakerUiFactory.CreateText("Summary", filterStatus, _theme,
                string.Empty, 14, TextAnchor.MiddleLeft);
            KingmakerUiFactory.Stretch(_catalogSummary.rectTransform);

            ScrollRect scroll = KingmakerUiFactory.CreateScrollView(
                "BuffCatalog", panel, _theme, out _sourceContent);
            _catalogScrollRoot = (RectTransform)scroll.transform;
            _sourceViewport = scroll.viewport;
            UpdateAdvancedFiltersVisibility();
        }

        private void BuildDetailPanel(RectTransform frame)
        {
            RectTransform panel = KingmakerUiFactory.CreateRect("DetailsPanel", frame);
            KingmakerUiFactory.SetAnchors(panel, 0.38f, 0.16f, 0.985f, 0.845f, 8, 0, 0, 0);
            KingmakerUiFactory.AddFramedPanel(panel, _theme.ParchmentPanel,
                _theme.GoldAccent);
            ScrollRect scroll = KingmakerUiFactory.CreateScrollView(
                "Details", panel, _theme, out _detailContent);
            _detailViewport = scroll.viewport;
            KingmakerUiFactory.SetAnchors((RectTransform)scroll.transform, 0.015f, 0.02f, 0.985f, 0.985f);
        }

        private void BuildFooter(RectTransform frame)
        {
            RectTransform footer = KingmakerUiFactory.CreateRect("Footer", frame);
            KingmakerUiFactory.SetAnchors(footer, 0.015f, 0.015f, 0.985f, 0.15f);
            KingmakerUiFactory.AddFramedPanel(footer, _theme.ParchmentRaised,
                _theme.GoldAccent);
            _result = KingmakerUiFactory.CreateText("Result", footer, _theme,
                string.Empty, 16, TextAnchor.MiddleLeft);
            KingmakerUiFactory.SetAnchors(_result.rectTransform, 0.015f, 0.10f, 0.67f, 0.90f);
            Button refresh = KingmakerUiFactory.CreateButton("Refresh", footer, _theme,
                "REFRESH", () => { _session.Refresh(); RefreshAll(); });
            KingmakerUiFactory.SetAnchors((RectTransform)refresh.transform, 0.69f, 0.20f, 0.78f, 0.80f);
            Button close = KingmakerUiFactory.CreateButton("Close", footer, _theme,
                "CLOSE", () => _close());
            KingmakerUiFactory.SetAnchors((RectTransform)close.transform, 0.79f, 0.20f, 0.87f, 0.80f);
            Button execute = KingmakerUiFactory.CreateButton("Execute", footer, _theme,
                "APPLY " + _routineId.ToUpperInvariant(), () => _execute(_routineId));
            KingmakerUiFactory.SetAnchors((RectTransform)execute.transform, 0.88f, 0.14f, 0.985f, 0.86f);
        }

        private void RefreshTabs()
        {
            if (_routineTabs == null) return;
            string[] ids = { "long", "important", "short" };
            for (int index = 0; index < _routineTabs.Length; index++)
            {
                Button tab = _routineTabs[index];
                Image image = tab == null ? null : tab.targetGraphic as Image;
                if (image != null) image.color = ids[index] == _routineId
                    ? _theme.BurgundyPrimary : _theme.ParchmentRaised;
                if (tab != null)
                {
                    tab.interactable = !_session.IsExecuting;
                    Text label = tab.GetComponentInChildren<Text>(true);
                    if (label != null) label.text = BuildRoutineSummary(ids[index]).Label;
                }
            }
            Button execute = _root == null ? null : _root.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.name == "Execute");
            if (execute != null)
            {
                Text label = execute.GetComponentInChildren<Text>(true);
                if (label != null) label.text = "APPLY " + _routineId.ToUpperInvariant();
                execute.interactable = _session.Model != null && !_session.IsExecuting;
            }
        }

        private RoutineSummaryViewModel BuildRoutineSummary(string routineId)
        {
            string name = char.ToUpperInvariant(routineId[0]) + routineId.Substring(1);
            PlannerSetupModel model = _session.Model;
            if (model == null) return new RoutineSummaryViewModel(routineId, name, 0, 0);
            int requested = model.Profile.Routines.First(item => item.RoutineId == routineId)
                .Assignments.Sum(item => item.WantedTargetUnitIds.Count);
            try
            {
                RoutinePlanResult preview = _session.PreviewRoutine(routineId);
                int covered = preview.Plan.Outcomes.Count(item =>
                    item.Kind == TargetOutcomeKind.Fulfilled ||
                    item.Kind == TargetOutcomeKind.SkippedAlreadyActive);
                return new RoutineSummaryViewModel(routineId, name, covered, requested);
            }
            catch
            {
                return new RoutineSummaryViewModel(routineId, name, 0, requested);
            }
        }

        private void RefreshCatalog()
        {
            RefreshSourceList();
            try { RefreshDetails(); }
            catch (Exception exception)
            {
                _session.RecordBindingFailure("selected-details", exception);
                KingmakerUiFactory.DestroyChildren(_detailContent);
                AddHeading(_detailContent, "BUFF DETAILS ERROR");
                AddBodyText(_detailContent, "Selected-buff binding failed: " +
                    exception.Message, 72);
                FinalizeScrollContent(_detailContent, _detailViewport);
            }
            UpdateCatalogDiagnostics();
        }

        private void RefreshSourceList()
        {
            if (_sourceContent == null) return;
            UpdateFilterLabels();
            KingmakerUiFactory.DestroyChildren(_sourceContent);
            _sourceRows.Clear();
            PlannerSetupModel model = _session.Model;
            if (model == null)
            {
                AddBodyText(_sourceContent, "No campaign catalog is available.", 54);
                AddCatalogAction(_sourceContent, "REFRESH", () =>
                {
                    _session.Refresh();
                    RefreshAll();
                });
                FinalizeScrollContent(_sourceContent, _sourceViewport);
                return;
            }
            CatalogFilterDiagnostics filters;
            List<SetupSourceRow> sources = ApplyFilters(model, out filters);
            sources = sources.OrderBy(source => source.DisplayName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(source => source.SpellLevel).ToList();
            if (sources.Count != 0 && !sources.Any(source =>
                source.SourceId == model.SelectedSourceId))
                model.SelectSource(sources[0].SourceId);
            foreach (SetupSourceRow source in sources)
            {
                try
                {
                    var cardModel = new BuffCardViewModel(source, model,
                        source.SourceId == model.SelectedSourceId);
                    Button row = CreateBuffCard(_sourceContent, source, cardModel, () =>
                    {
                        _advancedCastingExpanded = false;
                        model.SelectSource(source.SourceId);
                        RefreshCatalog();
                    });
                    _sourceRows.Add(row);
                }
                catch (Exception exception)
                {
                    _session.RecordBindingFailure("source-row:" + source.SourceId, exception);
                    AddBodyText(_sourceContent, "Could not bind " + source.DisplayName +
                        ": " + exception.Message, 54);
                }
            }
            if (_sourceRows.Count == 0)
            {
                int availableCount = model.Sources.Count(model.IsSourceAvailable);
                if (model.Sources.Count == 0)
                    AddBodyText(_sourceContent, "No beneficial buff entries were normalized from " +
                        (_session.CatalogDiscovery == null ? 0 :
                            _session.CatalogDiscovery.RawCandidateCount) + " raw sources.", 72);
                else if (availableCount == 0)
                {
                    string reasons = string.Join(", ", model.Sources
                        .Select(model.GetSourceUnavailableReason).Distinct().Take(4).ToArray());
                    AddBodyText(_sourceContent, "No available beneficial buffs. " + reasons, 72);
                }
                else AddBodyText(_sourceContent, "0 of " + availableCount +
                    " buffs shown because of filters. Active filters: " +
                    filters.ActiveFilters + ".", 72);
                AddCatalogAction(_sourceContent, availableCount == 0 ? "REFRESH" : "RESET FILTERS",
                    availableCount == 0 ? (Action)(() =>
                    {
                        _session.Refresh();
                        RefreshAll();
                    }) : ResetFilters);
            }
            if (_catalogSummary != null)
            {
                _catalogSummary.text = _sourceRows.Count == sources.Count
                    ? sources.Count + (sources.Count == 1 ? " buff shown" : " buffs shown") +
                        "     " + StatusLegend()
                    : "Some matching buffs could not be shown. Refresh or check the log.";
            }
            FinalizeScrollContent(_sourceContent, _sourceViewport);
            LastCatalogDiagnostics = new CatalogLayoutDiagnostics { Filters = filters };
        }

        private Button CreateBuffCard(RectTransform parent, SetupSourceRow source,
            BuffCardViewModel card, Action select)
        {
            RectTransform rect = KingmakerUiFactory.CreateRect("Source." + source.SourceId, parent);
            Image background = KingmakerUiFactory.AddFramedPanel(rect,
                _theme.ParchmentRaised,
                card.Selected ? _theme.GoldAccent : _theme.MutedBrownText,
                card.Selected ? 2f : 1f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => select());
            EventTrigger trigger = rect.gameObject.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerEnter, ignored => PreviewTargets(source));
            AddTrigger(trigger, EventTriggerType.PointerExit, ignored => PreviewTargets(null));
            KingmakerUiFactory.AddLayout(rect, 76);

            RectTransform status = KingmakerUiFactory.CreateRect("Status", rect);
            KingmakerUiFactory.SetAnchors(status, 0, 0, 0.018f, 1, 2, 0, 3, 3);
            Image statusImage = KingmakerUiFactory.AddPanel(status, StatusColor(card.Status));
            statusImage.raycastTarget = false;

            RectTransform iconFrame = KingmakerUiFactory.CreateRect("IconFrame", rect);
            KingmakerUiFactory.SetAnchors(iconFrame, 0.025f, 0.10f, 0.145f, 0.90f);
            Image iconBackground = KingmakerUiFactory.AddFramedPanel(iconFrame,
                new Color(0.20f, 0.14f, 0.10f, 1f), _theme.GoldAccent);
            iconBackground.raycastTarget = false;
            Sprite icon = ResolveAbilityIcon(source);
            if (icon != null)
            {
                RectTransform iconRect = KingmakerUiFactory.CreateRect("AbilityIcon", iconFrame);
                KingmakerUiFactory.Stretch(iconRect, 4, 4, 4, 4);
                Image iconImage = iconRect.gameObject.AddComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
            else
            {
                Text fallback = KingmakerUiFactory.CreateText("MissingIcon", iconFrame, _theme,
                    "?", 26, TextAnchor.MiddleCenter);
                fallback.color = _theme.MutedBrownText;
                KingmakerUiFactory.Stretch(fallback.rectTransform);
            }

            Text name = KingmakerUiFactory.CreateText("Name", rect, _theme,
                card.Name, 17, TextAnchor.MiddleLeft);
            name.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.SetAnchors(name.rectTransform, 0.16f, 0.47f, 0.78f, 0.94f);
            Text badge = KingmakerUiFactory.CreateText("RoutineBadge", rect, _theme,
                card.RoutineBadge, 14, TextAnchor.MiddleCenter);
            badge.color = _theme.BurgundyPrimary;
            KingmakerUiFactory.SetAnchors(badge.rectTransform, 0.80f, 0.53f, 0.97f, 0.92f);
            Text availability = KingmakerUiFactory.CreateText("Availability", rect, _theme,
                card.Availability, 14, TextAnchor.MiddleLeft);
            availability.color = _theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(availability.rectTransform, 0.16f, 0.08f, 0.55f, 0.48f);
            Text configured = KingmakerUiFactory.CreateText("Configuration", rect, _theme,
                card.Configuration, 13, TextAnchor.MiddleRight);
            configured.color = StatusColor(card.Status);
            KingmakerUiFactory.SetAnchors(configured.rectTransform, 0.54f, 0.08f, 0.97f, 0.48f);

            return button;
        }

        private Sprite ResolveAbilityIcon(SetupSourceRow source)
        {
            string guid = string.IsNullOrEmpty(source.Ability.VariantGuid)
                ? source.Ability.BaseAbilityGuid : source.Ability.VariantGuid;
            BlueprintAbility ability = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(guid);
            return ability == null ? null : ability.Icon;
        }

        private Color StatusColor(PlannerPresentationStatus status)
        {
            switch (status)
            {
                case PlannerPresentationStatus.Success: return _theme.GreenSuccess;
                case PlannerPresentationStatus.Warning: return _theme.AmberWarning;
                case PlannerPresentationStatus.Failure: return _theme.RedFailure;
                case PlannerPresentationStatus.Disabled: return _theme.DisabledGray;
                default: return _theme.MutedBrownText;
            }
        }

        private string StatusLegend()
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(_theme.MutedBrownText) +
                ">Neutral</color>  <color=#" + ColorUtility.ToHtmlStringRGB(_theme.GreenSuccess) +
                ">Ready</color>  <color=#" + ColorUtility.ToHtmlStringRGB(_theme.AmberWarning) +
                ">Partial</color>  <color=#" + ColorUtility.ToHtmlStringRGB(_theme.RedFailure) +
                ">Blocked</color>";
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type,
            UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private void PreviewTargets(SetupSourceRow source)
        {
            PlannerSetupModel model = _session.Model;
            if (model == null || _targetVisuals.Count == 0) return;
            SetupSourceRow preview = source ?? model.SelectedSource;
            if (preview == null) return;
            foreach (UnitSnapshot unit in model.Snapshot.Units)
            {
                TargetPortraitVisual visual;
                if (!_targetVisuals.TryGetValue(unit.UnitId, out visual)) continue;
                ApplyTargetVisual(visual,
                    TargetPortraitViewModel.Create(preview, model, _routineId, unit));
            }
        }

        private void RefreshDetails()
        {
            if (_detailContent == null) return;
            KingmakerUiFactory.DestroyChildren(_detailContent);
            PlannerSetupModel model = _session.Model;
            SetupSourceRow source = model == null ? null : model.SelectedSource;
            if (source == null)
            {
                AddHeading(_detailContent, "BUFF DETAILS");
                AddBodyText(_detailContent,
                    "Choose a discovered buff after loading a campaign. Search, filters, routine tabs, " +
                    "provider controls, targets, plan feedback, and settings remain external to saves.", 96);
                FinalizeScrollContent(_detailContent, _detailViewport);
                return;
            }

            CreateSelectedBuffHeader(_detailContent, source);
            AddBodyText(_detailContent, BuffCardViewModel.PlayerSourceType(source.Ability.SourceKind) +
                (source.Ability.SourceKind == SourceKind.Spellbook
                    ? " level " + source.SpellLevel : string.Empty) + "  |  " +
                source.DurationText, 34);
            AddBodyText(_detailContent, string.IsNullOrWhiteSpace(source.Description)
                ? "No localized description is available." : source.Description, 82);

            RectTransform assignmentRow = AddHorizontalRow(_detailContent, 44);
            Button assign = KingmakerUiFactory.CreateButton("Assign", assignmentRow, _theme,
                model.IsAssigned(_routineId) ? "REMOVE FROM " + _routineId.ToUpperInvariant()
                    : "ADD TO " + _routineId.ToUpperInvariant(),
                () => { model.ToggleRoutine(_routineId); RefreshCatalog(); });
            Button hidden = KingmakerUiFactory.CreateButton("Hide", assignmentRow, _theme,
                model.Profile.HiddenSourceIds.Contains(source.SourceId) ? "UNHIDE" : "HIDE",
                () => { model.ToggleHidden(); RefreshCatalog(); });
            assign.interactable = !_session.IsExecuting;
            hidden.interactable = !_session.IsExecuting;

            AddHeading(_detailContent, "TARGETS - PARTY AND PETS");
            RectTransform targetActions = AddHorizontalRow(_detailContent, 38);
            Button selectAll = KingmakerUiFactory.CreateButton("SelectAllValid", targetActions,
                _theme, "SELECT ALL VALID", () =>
                {
                    model.SetAllValidTargets(_routineId, true);
                    RefreshDetails();
                });
            Button clearTargets = KingmakerUiFactory.CreateButton("ClearTargets", targetActions,
                _theme, "CLEAR TARGETS", () =>
                {
                    model.SetAllValidTargets(_routineId, false);
                    RefreshDetails();
                });
            selectAll.interactable = !_session.IsExecuting;
            clearTargets.interactable = !_session.IsExecuting;
            int targetRows = Math.Max(1, (model.Snapshot.Units.Count + 5) / 6);
            RectTransform targets = KingmakerUiFactory.CreateRect("TargetGrid", _detailContent);
            KingmakerUiFactory.AddLayout(targets, targetRows * 116);
            GridLayoutGroup targetLayout = targets.gameObject.AddComponent<GridLayoutGroup>();
            targetLayout.cellSize = new Vector2(92, 108);
            targetLayout.spacing = new Vector2(7, 7);
            targetLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            targetLayout.constraintCount = 6;
            targetLayout.childAlignment = TextAnchor.UpperLeft;
            _targetVisuals.Clear();
            foreach (UnitSnapshot unit in model.Snapshot.Units)
                CreateTargetCard(targets, source, model, unit);

            AddHeading(_detailContent, "CASTING SOURCE");
            var castingSource = new CastingSourceSummaryViewModel(source, model);
            AddBodyText(_detailContent, castingSource.Summary, 36);
            if (source.Providers.Count != 0)
            {
                Button advancedCasting = KingmakerUiFactory.CreateButton(
                    "AdvancedCastingSource", _detailContent, _theme,
                    (_advancedCastingExpanded ? "Hide " : string.Empty) +
                        "Advanced Casting Source", () =>
                    {
                        _advancedCastingExpanded = !_advancedCastingExpanded;
                        RefreshDetails();
                    });
                KingmakerUiFactory.AddLayout((RectTransform)advancedCasting.transform, 38);
                if (_advancedCastingExpanded)
                    foreach (ProviderSnapshot provider in source.Providers)
                        CreateProviderRow(_detailContent, model, provider);
            }

            AddHeading(_detailContent, "PLAN SUMMARY");
            try
            {
                RoutinePlanResult preview = _session.PreviewRoutine(_routineId);
                int fulfilled = preview.Plan.Outcomes.Count(item => item.Kind == TargetOutcomeKind.Fulfilled);
                int skipped = preview.Plan.Outcomes.Count(item => item.Kind == TargetOutcomeKind.SkippedAlreadyActive);
                int unfulfilled = preview.Plan.Outcomes.Count(item => item.Kind == TargetOutcomeKind.Unfulfilled);
                int covered = fulfilled + skipped;
                int requested = covered + unfulfilled;
                string planText = preview.Plan.Steps.Count +
                    (preview.Plan.Steps.Count == 1 ? " cast planned" : " casts planned") +
                    "\n" + covered + " of " + requested + " targets covered" +
                    (skipped == 0 ? string.Empty : "\n" + skipped +
                        (skipped == 1 ? " existing buff will be skipped" :
                            " existing buffs will be skipped")) +
                    (unfulfilled == 0 ? string.Empty : "\n" + unfulfilled +
                        (unfulfilled == 1 ? " target needs attention" :
                            " targets need attention"));
                AddBodyText(_detailContent, planText, 72);
            }
            catch (Exception exception)
            {
                AddBodyText(_detailContent, "Plan unavailable: " + exception.Message, 54);
            }

            AddHeading(_detailContent, "SETTINGS");
            RectTransform settings = AddHorizontalRow(_detailContent, 44);
            var settingsModel = new PlannerSettingsViewModel(model.Profile);
            KingmakerUiFactory.CreateButton("ExecutionMode", settings, _theme,
                "Casting mode: " + settingsModel.CastingMode,
                () => { model.ToggleExecutionMode(); RefreshDetails(); });
            KingmakerUiFactory.CreateButton("Combat", settings, _theme,
                "Combat use: " + settingsModel.CombatUse,
                () => { model.ToggleOutOfCombatOnly(); RefreshDetails(); });
            Button existing = KingmakerUiFactory.CreateButton("ExistingBuffs", settings, _theme,
                "Existing buffs: " +
                    (model.GetExistingEffectPolicy(_routineId) == ExistingEffectPolicy.SkipAlreadyActive
                        ? "Skip active" : "Recast"),
                () => { model.ToggleExistingEffectPolicy(_routineId); RefreshDetails(); });
            existing.interactable = model.IsAssigned(_routineId) && !_session.IsExecuting;
            KingmakerUiFactory.CreateButton("Fallback", settings, _theme,
                "Fallback: " + settingsModel.Fallback,
                () => { model.ToggleAnimatedFallback(); RefreshDetails(); });
            FinalizeScrollContent(_detailContent, _detailViewport);
        }

        private void CreateSelectedBuffHeader(RectTransform parent, SetupSourceRow source)
        {
            RectTransform row = AddHorizontalRow(parent, 76);
            RectTransform iconFrame = KingmakerUiFactory.CreateRect("SelectedIconFrame", row);
            LayoutElement iconLayout = iconFrame.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 76;
            iconLayout.minWidth = 76;
            Image frame = KingmakerUiFactory.AddFramedPanel(iconFrame,
                new Color(0.20f, 0.14f, 0.10f, 1f), _theme.GoldAccent);
            frame.raycastTarget = false;
            Sprite icon = ResolveAbilityIcon(source);
            if (icon != null)
            {
                RectTransform iconRect = KingmakerUiFactory.CreateRect("AbilityIcon", iconFrame);
                KingmakerUiFactory.Stretch(iconRect, 7, 7, 7, 7);
                Image image = iconRect.gameObject.AddComponent<Image>();
                image.sprite = icon;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            else
            {
                Text fallback = KingmakerUiFactory.CreateText("MissingIcon", iconFrame,
                    _theme, "?", 30, TextAnchor.MiddleCenter);
                fallback.color = _theme.MutedBrownText;
                KingmakerUiFactory.Stretch(fallback.rectTransform);
            }
            Text title = KingmakerUiFactory.CreateText("Heading", row, _theme,
                source.DisplayName, 24, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            title.color = _theme.BurgundyPrimary;
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        private void CreateTargetCard(RectTransform parent, SetupSourceRow source,
            PlannerSetupModel model, UnitSnapshot unit)
        {
            Button card = KingmakerUiFactory.CreateButton("Target." + unit.UnitId, parent, _theme,
                unit.DisplayName + (unit.IsPet ? "\nPET" : string.Empty), () =>
                {
                    model.ToggleTarget(_routineId, unit.UnitId);
                    RefreshDetails();
                });
            RectTransform rect = (RectTransform)card.transform;
            LayoutElement layout = KingmakerUiFactory.AddLayout(rect, 108);
            layout.preferredWidth = 92;
            layout.minWidth = 92;
            Text label = card.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.alignment = TextAnchor.LowerCenter;
                KingmakerUiFactory.SetAnchors(label.rectTransform, 0, 0, 1, 0.30f, 3, 3, 2, 0);
            }
            Sprite portrait = ResolvePortrait(unit.UnitId);
            if (portrait != null)
            {
                RectTransform portraitRect = KingmakerUiFactory.CreateRect("Portrait", rect);
                KingmakerUiFactory.SetAnchors(portraitRect, 0.12f, 0.31f, 0.88f, 0.96f);
                Image image = portraitRect.gameObject.AddComponent<Image>();
                image.sprite = portrait;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            RectTransform overlayRect = KingmakerUiFactory.CreateRect("StateOverlay", rect);
            KingmakerUiFactory.SetAnchors(overlayRect, 0.10f, 0.30f, 0.90f, 0.97f);
            Image overlay = KingmakerUiFactory.AddPanel(overlayRect, Color.clear,
                _theme.NativeSelectedOrnament);
            overlay.raycastTarget = false;
            Text mark = KingmakerUiFactory.CreateText("StateMark", overlayRect, _theme,
                string.Empty, 25, TextAnchor.MiddleCenter);
            mark.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.Stretch(mark.rectTransform);
            var visual = new TargetPortraitVisual(card, overlay, mark);
            _targetVisuals[unit.UnitId] = visual;
            TargetPortraitViewModel target = TargetPortraitViewModel.Create(
                source, model, _routineId, unit);
            ApplyTargetVisual(visual, target);
            card.interactable = target.Legal && model.IsAssigned(_routineId) &&
                !_session.IsExecuting;
        }

        private void ApplyTargetVisual(TargetPortraitVisual visual, TargetPortraitViewModel target)
        {
            Color color = StatusColor(target.Status);
            visual.Overlay.color = target.Status == PlannerPresentationStatus.Neutral
                ? Color.clear : new Color(color.r, color.g, color.b,
                    target.Indirect ? 0.28f : 0.58f);
            visual.Mark.text = target.Indirect ? "•" : target.Wanted
                ? (target.Status == PlannerPresentationStatus.Success ? "✓" : "!") :
                target.Legal ? string.Empty : "×";
            visual.Mark.color = target.Status == PlannerPresentationStatus.Neutral
                ? _theme.MutedBrownText : color;
            Image background = visual.Button.targetGraphic as Image;
            if (background != null) background.color = target.Status == PlannerPresentationStatus.Neutral
                ? _theme.ParchmentRaised : new Color(color.r, color.g, color.b, 0.48f);
        }

        private void CreateProviderRow(RectTransform parent, PlannerSetupModel model, ProviderSnapshot provider)
        {
            RectTransform row = AddHorizontalRow(parent, 48);
            string rejection = model.GetProviderRejectionReason(provider);
            BlueprintSpellbook spellbook = string.IsNullOrEmpty(provider.Key.SpellbookGuid)
                ? null : ResourcesLibrary.TryGetBlueprint<BlueprintSpellbook>(provider.Key.SpellbookGuid);
            string spellbookName = spellbook == null || string.IsNullOrWhiteSpace(spellbook.Name)
                ? (provider.Key.Ability.SourceKind == SourceKind.Spellbook ? "Spellbook" : "Ability")
                : spellbook.Name;
            Text label = KingmakerUiFactory.CreateText("Provider", row, _theme,
                model.GetCasterDisplayName(provider) + " — " + spellbookName + " — " +
                BuffCardViewModel.BuildAvailabilityForProvider(provider, model) +
                (string.IsNullOrEmpty(rejection) ? string.Empty : " — " +
                    BuffCardViewModel.PlayerReason(rejection)),
                15, TextAnchor.MiddleLeft);
            LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1;
            ProviderPreferenceProfile preference = model.GetProviderPreference(provider.Key.Canonical);
            string state = preference == null ? "Automatic priority" :
                preference.Banned ? "Disabled" : "Preferred";
            Button stateButton = KingmakerUiFactory.CreateButton("ProviderState", row, _theme,
                state, () => { model.CycleProviderPreference(provider.Key.Canonical); RefreshDetails(); });
            LayoutElement stateLayout = stateButton.gameObject.AddComponent<LayoutElement>();
            stateLayout.preferredWidth = 95;
            Button minus = KingmakerUiFactory.CreateButton("CapMinus", row, _theme, "-", () =>
            {
                model.AdjustProviderCap(provider.Key.Canonical, -1);
                RefreshDetails();
            });
            minus.gameObject.AddComponent<LayoutElement>().preferredWidth = 38;
            Button plus = KingmakerUiFactory.CreateButton("CapPlus", row, _theme, "+", () =>
            {
                model.AdjustProviderCap(provider.Key.Canonical, 1);
                RefreshDetails();
            });
            plus.gameObject.AddComponent<LayoutElement>().preferredWidth = 38;
            Text cap = KingmakerUiFactory.CreateText("Cap", row, _theme,
                preference == null || preference.MaximumCasts == null
                    ? "No limit" : "Maximum " + preference.MaximumCasts.Value + " casts",
                14, TextAnchor.MiddleCenter);
            cap.gameObject.AddComponent<LayoutElement>().preferredWidth = 130;
        }

        private RectTransform AddHorizontalRow(RectTransform parent, float height)
        {
            RectTransform row = KingmakerUiFactory.CreateRect("Row", parent);
            KingmakerUiFactory.AddLayout(row, height);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            return row;
        }

        private void AddHeading(RectTransform parent, string value)
        {
            Text text = KingmakerUiFactory.CreateText("Heading", parent, _theme,
                value, 20, TextAnchor.MiddleLeft);
            text.fontStyle = FontStyle.Bold;
            text.color = _theme.GoldAccent;
            KingmakerUiFactory.AddLayout(text.rectTransform, 34);
        }

        private void AddBodyText(RectTransform parent, string value, float height)
        {
            Text text = KingmakerUiFactory.CreateText("Body", parent, _theme,
                value, 16, TextAnchor.UpperLeft);
            KingmakerUiFactory.AddLayout(text.rectTransform, height);
        }

        internal CatalogLayoutDiagnostics GetCatalogDiagnostics()
        {
            UpdateCatalogDiagnostics();
            return LastCatalogDiagnostics;
        }

        private List<SetupSourceRow> ApplyFilters(
            PlannerSetupModel model,
            out CatalogFilterDiagnostics diagnostics)
        {
            _filters.Search = _search == null ? string.Empty : _search.text;
            return _filters.Apply(model, _routineId, out diagnostics);
        }

        private void ResetFilters()
        {
            _filters.Reset();
            if (_search != null)
            {
                _resettingFilters = true;
                _search.text = string.Empty;
                _resettingFilters = false;
            }
            RefreshCatalog();
        }

        private void UpdateFilterLabels()
        {
            if (_configuredFilterLabel != null)
                _configuredFilterLabel.text = (_filters.ConfiguredOnly ? "✓ " : string.Empty) +
                    "Configured only";
            if (_hiddenFilterLabel != null)
                _hiddenFilterLabel.text = (_filters.ShowHidden ? "✓ " : string.Empty) +
                    "Show hidden";
            if (_advancedFilterLabels == null || _advancedFilterLabels.Length != 3) return;
            _advancedFilterLabels[0].text = _filters.SourceCategoryFilter == 1 ? "Spells" :
                _filters.SourceCategoryFilter == 2 ? "Abilities" : "All sources";
            _advancedFilterLabels[1].text = _filters.DurationFilter == 1 ? "Long duration" :
                _filters.DurationFilter == 2 ? "Short duration" : "Any duration";
            _advancedFilterLabels[2].text = _filters.AvailabilityFilter == 1 ? "Available now" :
                _filters.AvailabilityFilter == 2 ? "Unavailable" : "All availability";
        }

        private void UpdateAdvancedFiltersVisibility()
        {
            if (_advancedFilters != null)
                _advancedFilters.gameObject.SetActive(_advancedFiltersExpanded);
            if (_catalogScrollRoot != null)
                KingmakerUiFactory.SetAnchors(_catalogScrollRoot, 0.02f, 0.02f, 0.98f,
                    _advancedFiltersExpanded ? 0.70f : 0.77f);
        }

        private void AddCatalogAction(RectTransform parent, string label, Action action)
        {
            Button button = KingmakerUiFactory.CreateButton("CatalogAction", parent, _theme,
                label, () => action());
            KingmakerUiFactory.AddLayout((RectTransform)button.transform, 42);
        }

        private string BuildMaskEvidence()
        {
            Mask mask = _sourceViewport == null ? null : _sourceViewport.GetComponent<Mask>();
            RectMask2D rectMask = _sourceViewport == null ? null :
                _sourceViewport.GetComponent<RectMask2D>();
            Image image = _sourceViewport == null ? null : _sourceViewport.GetComponent<Image>();
            return "mask=" + (mask != null) + ",enabled=" + (mask != null && mask.enabled) +
                ",showGraphic=" + (mask != null && mask.showMaskGraphic) +
                ",rectMask=" + (rectMask != null) + ",viewportImage=" +
                GraphicEvidence(image) + ",viewport=" + RectEvidence(_sourceViewport);
        }

        private static string RowDisplayName(PlannerSetupModel model, Button row)
        {
            if (model == null || row == null) return string.Empty;
            SetupSourceRow source = model.Sources.FirstOrDefault(item =>
                row.name == "Source." + item.SourceId);
            return source == null ? string.Empty : source.DisplayName;
        }

        private static string BuildGraphicEvidence(GameObject root)
        {
            if (root == null) return "missing";
            RectTransform rect = root.transform as RectTransform;
            Canvas canvas = root.GetComponentInParent<Canvas>();
            CanvasGroup[] groups = root.GetComponentsInParent<CanvasGroup>(true);
            float inheritedGroupAlpha = 1f;
            foreach (CanvasGroup group in groups)
            {
                inheritedGroupAlpha *= group.alpha;
                if (group.ignoreParentGroups) break;
            }
            string components = string.Join(",", root.GetComponents<Component>()
                .Where(item => item != null).Select(item => item.GetType().FullName).ToArray());
            string graphics = string.Join("|", root.GetComponentsInChildren<Graphic>(true)
                .Select(GraphicEvidence).ToArray());
            return "path=" + GetPath(root.transform) + ";id=" + root.GetInstanceID() +
                ";components=" + components + ";activeSelf=" + root.activeSelf +
                ";activeHierarchy=" + root.activeInHierarchy + ";parent=" +
                (root.transform.parent == null ? string.Empty : GetPath(root.transform.parent)) +
                ";sibling=" + root.transform.GetSiblingIndex() + ";layer=" + root.layer +
                ";canvas=" + (canvas == null ? "missing" : GetPath(canvas.transform) +
                    ",mode=" + canvas.renderMode + ",sort=" + canvas.sortingOrder +
                    ",override=" + canvas.overrideSorting) +
                ";groupAlpha=" + inheritedGroupAlpha.ToString("R") +
                ";rect=" + RectEvidence(rect) + ";layout=min:" +
                LayoutUtility.GetMinHeight(rect).ToString("R") + ",preferred:" +
                LayoutUtility.GetPreferredHeight(rect).ToString("R") + ",flex:" +
                LayoutUtility.GetFlexibleHeight(rect).ToString("R") +
                ";graphics=" + graphics;
        }

        private static string GraphicEvidence(Graphic graphic)
        {
            if (graphic == null) return "missing";
            CanvasRenderer renderer = graphic.canvasRenderer;
            Material material = graphic.materialForRendering;
            Text text = graphic as Text;
            string font = text == null || text.font == null ? string.Empty : text.font.name;
            string value = text == null ? string.Empty : text.text;
            string inheritedAlpha = "unknown";
            try
            {
                MethodInfo method = typeof(CanvasRenderer).GetMethod("GetInheritedAlpha",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null) inheritedAlpha = Convert.ToSingle(method.Invoke(renderer,
                    null), CultureInfo.InvariantCulture).ToString("R");
            }
            catch { }
            return graphic.GetType().Name + "(path=" + GetPath(graphic.transform) +
                ",enabled=" + graphic.enabled + ",raycast=" + graphic.raycastTarget +
                ",color=" + graphic.color + ",rendererCull=" + renderer.cull +
                ",depth=" + renderer.absoluteDepth + ",materials=" + renderer.materialCount +
                ",alpha=" + renderer.GetAlpha().ToString("R") +
                ",inheritedAlpha=" + inheritedAlpha + ",material=" +
                (material == null ? "null" : material.name) + ",shader=" +
                (material == null || material.shader == null ? "null" : material.shader.name) +
                ",font=" + font + ",fontSize=" + (text == null ? 0 : text.fontSize) +
                ",text=" + value.Replace("\r", " ").Replace("\n", " ") + ")";
        }

        private static string RectEvidence(RectTransform rect)
        {
            if (rect == null) return "missing";
            return "anchors=" + rect.anchorMin + ".." + rect.anchorMax + ",pivot=" +
                rect.pivot + ",anchored=" + rect.anchoredPosition + ",sizeDelta=" +
                rect.sizeDelta + ",rect=" + rect.rect + ",screen=" + ScreenRectangle(rect);
        }

        private static string ScreenRectangle(RectTransform rect)
        {
            if (rect == null) return "missing";
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float minX = corners.Min(item => item.x);
            float maxX = corners.Max(item => item.x);
            float minY = corners.Min(item => item.y);
            float maxY = corners.Max(item => item.y);
            return minX.ToString("F1") + "," + minY.ToString("F1") + "-" +
                maxX.ToString("F1") + "," + maxY.ToString("F1");
        }

        private static void FinalizeScrollContent(RectTransform content, RectTransform viewport)
        {
            if (content == null || viewport == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float preferred = Mathf.Max(1f, LayoutUtility.GetPreferredHeight(content));
            float viewportHeight = Mathf.Max(1f, viewport.rect.height);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(preferred, viewportHeight));
            content.anchoredPosition = new Vector2(0, Mathf.Max(0, content.anchoredPosition.y));
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Canvas.ForceUpdateCanvases();
        }

        private void UpdateCatalogDiagnostics()
        {
            if (_sourceContent == null || _sourceViewport == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_sourceContent);
            CatalogLayoutDiagnostics value = LastCatalogDiagnostics ?? new CatalogLayoutDiagnostics();
            value.InstantiatedRows = _sourceRows.Count;
            value.ActiveRows = _sourceRows.Count(row => row != null && row.gameObject.activeInHierarchy);
            value.VisibleRows = _sourceRows.Count(row => row != null &&
                row.gameObject.activeInHierarchy && RectanglesOverlap(
                    (RectTransform)row.transform, _sourceViewport));
            value.ContentWidth = _sourceContent.rect.width;
            value.ContentHeight = _sourceContent.rect.height;
            value.ViewportWidth = _sourceViewport.rect.width;
            value.ViewportHeight = _sourceViewport.rect.height;
            value.DetailChildren = _detailContent == null ? 0 : _detailContent.childCount;
            PlannerSetupModel model = _session.Model;
            value.SelectedSourceId = model == null ? string.Empty : model.SelectedSourceId;
            value.SelectedDetailsBound = model != null && model.SelectedSource != null &&
                _detailContent != null && _detailContent.childCount >= 3;
            value.BindingFailure = _session.LastBindingFailure ?? string.Empty;
            value.BlessEvidence = BuildBlessEvidence(model);
            LastCatalogDiagnostics = value;
        }

        private string BuildBlessEvidence(PlannerSetupModel model)
        {
            const string bless = "90e59f4a4ada87243b7b3535a06d0638";
            SetupSourceRow source = model == null ? null : model.Sources
                .Where(item => item.Ability.BaseAbilityGuid == bless ||
                    item.Ability.VariantGuid == bless)
                .OrderBy(item => item.Ability.SourceKind == SourceKind.Spellbook ? 0 : 1)
                .FirstOrDefault();
            if (source == null) return "absent";
            Button row = _sourceRows.FirstOrDefault(item => item != null &&
                item.name == "Source." + source.SourceId);
            bool assigned = model.Profile.Routines.SelectMany(item => item.Assignments)
                .Any(item => item.SourceId == source.SourceId);
            string bounds = row == null ? "none" : ((RectTransform)row.transform).rect.size.ToString();
            return "source=" + source.SourceId + ",available=" + model.IsSourceAvailable(source) +
                ",reason=" + model.GetSourceUnavailableReason(source) + ",assigned=" + assigned +
                ",providers=" + source.Providers.Count + ",row=" + (row != null) +
                ",rowActive=" + (row != null && row.gameObject.activeInHierarchy) +
                ",rowVisible=" + (row != null && RectanglesOverlap(
                    (RectTransform)row.transform, _sourceViewport)) + ",bounds=" + bounds +
                ",material=" + (_session.CatalogDiscovery == null ? "missing" :
                    _session.CatalogDiscovery.BlessMaterialEvidence);
        }

        private static bool RectanglesOverlap(RectTransform first, RectTransform second)
        {
            if (first == null || second == null) return false;
            var a = new Vector3[4];
            var b = new Vector3[4];
            first.GetWorldCorners(a);
            second.GetWorldCorners(b);
            float aMinX = a.Min(value => value.x);
            float aMaxX = a.Max(value => value.x);
            float aMinY = a.Min(value => value.y);
            float aMaxY = a.Max(value => value.y);
            float bMinX = b.Min(value => value.x);
            float bMaxX = b.Max(value => value.x);
            float bMinY = b.Min(value => value.y);
            float bMaxY = b.Max(value => value.y);
            return aMaxX > bMinX && aMinX < bMaxX && aMaxY > bMinY && aMinY < bMaxY;
        }

        private static Sprite ResolvePortrait(string unitId)
        {
            if (Game.Instance == null || Game.Instance.Player == null) return null;
            foreach (UnitEntityData member in Game.Instance.Player.Party)
            {
                if (member != null && member.UniqueId == unitId)
                    return member.Portrait == null ? null : member.Portrait.SmallPortrait;
                UnitEntityData pet = member == null || member.Descriptor == null
                    ? null : member.Descriptor.Pet;
                if (pet != null && pet.UniqueId == unitId)
                    return pet.Portrait == null ? null : pet.Portrait.SmallPortrait;
            }
            return null;
        }

        private sealed class TargetPortraitVisual
        {
            internal TargetPortraitVisual(Button button, Image overlay, Text mark)
            {
                Button = button;
                Overlay = overlay;
                Mark = mark;
            }

            internal Button Button { get; private set; }
            internal Image Overlay { get; private set; }
            internal Text Mark { get; private set; }
        }
    }

    internal sealed class PlannerScreenMarker : MonoBehaviour { }

    internal sealed class PlannerPresentationValidation
    {
        internal PlannerPresentationValidation(bool valid, string failure, int rootInstanceId,
            bool activeSelf, bool activeInHierarchy, string hierarchy, string canvasIdentity,
            string renderMode, int sortingOrder, bool overrideSorting, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 rectSize,
            Vector3[] worldCorners, int screenWidth, int screenHeight, float coverage,
            float alpha, bool interactable, bool blocksRaycasts, bool backgroundRaycastTarget,
            float backgroundOpacity, string raycasterIdentity, string eventSystemIdentity,
            bool ownsCenterRaycast, string topRaycast)
        {
            Valid = valid; Failure = failure ?? string.Empty; RootInstanceId = rootInstanceId;
            ActiveSelf = activeSelf; ActiveInHierarchy = activeInHierarchy; Hierarchy = hierarchy ?? string.Empty;
            CanvasIdentity = canvasIdentity ?? string.Empty; RenderMode = renderMode ?? string.Empty;
            SortingOrder = sortingOrder; OverrideSorting = overrideSorting; AnchorMin = anchorMin;
            AnchorMax = anchorMax; Pivot = pivot; SizeDelta = sizeDelta; RectSize = rectSize;
            WorldCorners = worldCorners ?? new Vector3[0]; ScreenWidth = screenWidth;
            ScreenHeight = screenHeight; Coverage = coverage; Alpha = alpha;
            Interactable = interactable; BlocksRaycasts = blocksRaycasts;
            BackgroundRaycastTarget = backgroundRaycastTarget; BackgroundOpacity = backgroundOpacity;
            RaycasterIdentity = raycasterIdentity ?? string.Empty;
            EventSystemIdentity = eventSystemIdentity ?? string.Empty;
            OwnsCenterRaycast = ownsCenterRaycast; TopRaycast = topRaycast ?? string.Empty;
        }

        internal static PlannerPresentationValidation Failed(string failure)
        {
            return new PlannerPresentationValidation(false, failure, 0, false, false,
                string.Empty, string.Empty, string.Empty, 0, false, Vector2.zero, Vector2.zero,
                Vector2.zero, Vector2.zero, Vector2.zero, new Vector3[0], Screen.width,
                Screen.height, 0, 0, false, false, false, 0, string.Empty, string.Empty,
                false, string.Empty);
        }

        internal bool Valid { get; private set; }
        internal string Failure { get; private set; }
        internal int RootInstanceId { get; private set; }
        internal bool ActiveSelf { get; private set; }
        internal bool ActiveInHierarchy { get; private set; }
        internal string Hierarchy { get; private set; }
        internal string CanvasIdentity { get; private set; }
        internal string RenderMode { get; private set; }
        internal int SortingOrder { get; private set; }
        internal bool OverrideSorting { get; private set; }
        internal Vector2 AnchorMin { get; private set; }
        internal Vector2 AnchorMax { get; private set; }
        internal Vector2 Pivot { get; private set; }
        internal Vector2 SizeDelta { get; private set; }
        internal Vector2 RectSize { get; private set; }
        internal Vector3[] WorldCorners { get; private set; }
        internal int ScreenWidth { get; private set; }
        internal int ScreenHeight { get; private set; }
        internal float Coverage { get; private set; }
        internal float Alpha { get; private set; }
        internal bool Interactable { get; private set; }
        internal bool BlocksRaycasts { get; private set; }
        internal bool BackgroundRaycastTarget { get; private set; }
        internal float BackgroundOpacity { get; private set; }
        internal string RaycasterIdentity { get; private set; }
        internal string EventSystemIdentity { get; private set; }
        internal bool OwnsCenterRaycast { get; private set; }
        internal string TopRaycast { get; private set; }

        public override string ToString()
        {
            string corners = string.Join(";", WorldCorners.Select(value => value.ToString()).ToArray());
            return "valid=" + Valid + ";failure=" + Failure + ";root=" + RootInstanceId +
                ";activeSelf=" + ActiveSelf + ";activeHierarchy=" + ActiveInHierarchy +
                ";hierarchy=" + Hierarchy + ";canvas=" + CanvasIdentity + ";mode=" + RenderMode +
                ";sort=" + SortingOrder + ";override=" + OverrideSorting + ";anchors=" +
                AnchorMin + ".." + AnchorMax + ";pivot=" + Pivot + ";sizeDelta=" + SizeDelta +
                ";rect=" + RectSize + ";corners=" + corners + ";screen=" + ScreenWidth + "x" +
                ScreenHeight + ";coverage=" + Coverage.ToString("F4") + ";alpha=" + Alpha +
                ";interactable=" + Interactable + ";blocks=" + BlocksRaycasts +
                ";backgroundRaycast=" + BackgroundRaycastTarget + ";backgroundAlpha=" +
                BackgroundOpacity + ";raycaster=" + RaycasterIdentity + ";eventSystem=" +
                EventSystemIdentity + ";ownsCenter=" + OwnsCenterRaycast + ";top=" + TopRaycast;
        }
    }
}
