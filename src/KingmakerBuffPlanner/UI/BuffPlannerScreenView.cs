using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI;
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
        private readonly List<Button> _sourceRows = new List<Button>();
        private string _routineId = "long";
        private bool _configuredOnly;
        private bool _requestedOnly;
        private bool _unconfiguredOnly;
        private bool _showHidden;
        private bool _sortByLevel;
        private int _durationFilter;
        private int _sourceKindFilter = -1;
        private bool _showUnavailable;
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
            _theme = PlannerUiTheme.Resolve(nativeCanvas.ServiceWindow == null
                ? (Component)nativeCanvas : nativeCanvas.ServiceWindow.WindowTabs);
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
            _blocker = KingmakerUiFactory.AddPanel(_root, _theme.Background, _theme.PanelSprite);
            _blocker.color = _theme.Background;
            _blocker.raycastTarget = true;
            PlannerPointerSink sink = _root.gameObject.AddComponent<PlannerPointerSink>();
            sink.Diagnostics = _diagnostics;
            IsOpaque = _blocker.color.a >= 0.999f;
            BlocksRaycasts = _blocker.raycastTarget && _canvasGroup.blocksRaycasts && _canvasGroup.interactable;
            HasGraphicRaycaster = _raycaster.isActiveAndEnabled;

            RectTransform frame = KingmakerUiFactory.CreateRect("ServiceFrame", _root);
            KingmakerUiFactory.SetAnchors(frame, 0.025f, 0.025f, 0.975f, 0.975f);
            KingmakerUiFactory.AddPanel(frame, _theme.Panel, _theme.PanelSprite);

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
            KingmakerUiFactory.AddPanel(header, new Color(0.10f, 0.07f, 0.04f, 1f));
            Text title = KingmakerUiFactory.CreateText("Title", header, _theme,
                "BUFF PLANNER", 30, TextAnchor.MiddleCenter);
            KingmakerUiFactory.Stretch(title.rectTransform, 80, 80, 4, 4);
            _status = KingmakerUiFactory.CreateText("Status", header, _theme,
                string.Empty, 15, TextAnchor.LowerLeft);
            _status.color = _theme.MutedText;
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
            KingmakerUiFactory.AddPanel(panel, new Color(0.11f, 0.08f, 0.05f, 1f));
            _search = KingmakerUiFactory.CreateInputField("Search", panel, _theme, "Search buffs...");
            KingmakerUiFactory.SetAnchors((RectTransform)_search.transform, 0.02f, 0.92f, 0.98f, 0.985f);
            _search.onValueChanged.AddListener(value =>
            {
                if (!_resettingFilters) RefreshCatalog();
            });

            RectTransform filters = KingmakerUiFactory.CreateRect("Filters", panel);
            KingmakerUiFactory.SetAnchors(filters, 0.02f, 0.82f, 0.98f, 0.91f);
            HorizontalLayoutGroup filterLayout = filters.gameObject.AddComponent<HorizontalLayoutGroup>();
            filterLayout.spacing = 5;
            filterLayout.childControlWidth = true;
            filterLayout.childForceExpandWidth = true;
            filterLayout.childControlHeight = true;
            filterLayout.childForceExpandHeight = true;
            KingmakerUiFactory.CreateButton("Configured", filters, _theme, "CONFIG", CycleConfigurationFilter);
            KingmakerUiFactory.CreateButton("Duration", filters, _theme, "DURATION", () =>
            {
                _durationFilter = (_durationFilter + 1) % 4;
                RefreshCatalog();
            });
            KingmakerUiFactory.CreateButton("Kind", filters, _theme, "SOURCE", () =>
            {
                _sourceKindFilter++;
                if (_sourceKindFilter > 3) _sourceKindFilter = -1;
                RefreshCatalog();
            });
            KingmakerUiFactory.CreateButton("Sort", filters, _theme, "SORT", () =>
            {
                _sortByLevel = !_sortByLevel;
                RefreshCatalog();
            });
            KingmakerUiFactory.CreateButton("Hidden", filters, _theme, "HIDDEN", () =>
            {
                _showHidden = !_showHidden;
                RefreshCatalog();
            });
            KingmakerUiFactory.CreateButton("Availability", filters, _theme, "AVAIL", () =>
            {
                _showUnavailable = !_showUnavailable;
                RefreshCatalog();
            });

            RectTransform filterStatus = KingmakerUiFactory.CreateRect("FilterStatus", panel);
            KingmakerUiFactory.SetAnchors(filterStatus, 0.02f, 0.75f, 0.98f, 0.81f);
            _catalogSummary = KingmakerUiFactory.CreateText("Summary", filterStatus, _theme,
                string.Empty, 14, TextAnchor.MiddleLeft);
            KingmakerUiFactory.SetAnchors(_catalogSummary.rectTransform, 0, 0, 0.76f, 1);
            Button reset = KingmakerUiFactory.CreateButton("ResetFilters", filterStatus, _theme,
                "RESET FILTERS", ResetFilters);
            KingmakerUiFactory.SetAnchors((RectTransform)reset.transform, 0.77f, 0, 1, 1);

            ScrollRect scroll = KingmakerUiFactory.CreateScrollView(
                "BuffCatalog", panel, _theme, out _sourceContent);
            _sourceViewport = scroll.viewport;
            KingmakerUiFactory.SetAnchors((RectTransform)scroll.transform, 0.02f, 0.02f, 0.98f, 0.74f);
        }

        private void BuildDetailPanel(RectTransform frame)
        {
            RectTransform panel = KingmakerUiFactory.CreateRect("DetailsPanel", frame);
            KingmakerUiFactory.SetAnchors(panel, 0.38f, 0.16f, 0.985f, 0.845f, 8, 0, 0, 0);
            KingmakerUiFactory.AddPanel(panel, new Color(0.12f, 0.085f, 0.052f, 1f));
            ScrollRect scroll = KingmakerUiFactory.CreateScrollView(
                "Details", panel, _theme, out _detailContent);
            _detailViewport = scroll.viewport;
            KingmakerUiFactory.SetAnchors((RectTransform)scroll.transform, 0.015f, 0.02f, 0.985f, 0.985f);
        }

        private void BuildFooter(RectTransform frame)
        {
            RectTransform footer = KingmakerUiFactory.CreateRect("Footer", frame);
            KingmakerUiFactory.SetAnchors(footer, 0.015f, 0.015f, 0.985f, 0.15f);
            KingmakerUiFactory.AddPanel(footer, new Color(0.09f, 0.065f, 0.04f, 1f));
            _result = KingmakerUiFactory.CreateText("Result", footer, _theme,
                string.Empty, 16, TextAnchor.MiddleLeft);
            KingmakerUiFactory.SetAnchors(_result.rectTransform, 0.015f, 0.10f, 0.67f, 0.90f);
            Button refresh = KingmakerUiFactory.CreateButton("Refresh", footer, _theme,
                "REFRESH", () => { _session.Refresh(); RefreshAll(); });
            KingmakerUiFactory.SetAnchors((RectTransform)refresh.transform, 0.69f, 0.20f, 0.78f, 0.80f);
            Button settings = KingmakerUiFactory.CreateButton("Mode", footer, _theme,
                "MODE", () =>
                {
                    if (_session.Model != null) _session.Model.ToggleExecutionMode();
                    RefreshAll();
                });
            KingmakerUiFactory.SetAnchors((RectTransform)settings.transform, 0.79f, 0.20f, 0.87f, 0.80f);
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
                    ? _theme.AccentSelected : _theme.PanelLight;
                if (tab != null) tab.interactable = !_session.IsExecuting;
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
            sources = _sortByLevel
                ? sources.OrderBy(source => source.SpellLevel)
                    .ThenBy(source => source.DisplayName, StringComparer.OrdinalIgnoreCase).ToList()
                : sources.OrderBy(source => source.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(source => source.SpellLevel).ToList();
            if (sources.Count != 0 && !sources.Any(source =>
                source.SourceId == model.SelectedSourceId))
                model.SelectSource(sources[0].SourceId);
            foreach (SetupSourceRow source in sources)
            {
                try
                {
                    bool selected = source.SourceId == model.SelectedSourceId;
                    bool assigned = model.Profile.Routines.Any(routine =>
                        routine.Assignments.Any(assignment => assignment.SourceId == source.SourceId));
                    bool available = model.IsSourceAvailable(source);
                    string label = (assigned ? "[+] " : "[ ] ") + source.DisplayName +
                        "   L" + source.SpellLevel + "   " + source.Providers.Count + " providers" +
                        (available ? string.Empty : "   UNAVAILABLE: " +
                            model.GetSourceUnavailableReason(source));
                    Button row = KingmakerUiFactory.CreateButton("Source." + source.SourceId,
                        _sourceContent, _theme, label, () =>
                        {
                            model.SelectSource(source.SourceId);
                            RefreshCatalog();
                        });
                    KingmakerUiFactory.AddLayout((RectTransform)row.transform, 42);
                    Text rowText = row.GetComponentInChildren<Text>();
                    if (rowText != null) rowText.alignment = TextAnchor.MiddleLeft;
                    Image rowImage = row.targetGraphic as Image;
                    if (selected && rowImage != null) rowImage.color = _theme.AccentSelected;
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
                _catalogSummary.text = filters.VisibleViewModels + " of " + filters.TotalEntries +
                    " shown | " + filters.ActiveFilters;
            FinalizeScrollContent(_sourceContent, _sourceViewport);
            LastCatalogDiagnostics = new CatalogLayoutDiagnostics { Filters = filters };
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

            AddHeading(_detailContent, source.DisplayName);
            AddBodyText(_detailContent, "Spell level " + source.SpellLevel + "  |  " +
                source.Ability.SourceKind + "  |  " + source.DurationText, 34);
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
            Button policy = KingmakerUiFactory.CreateButton("Policy", assignmentRow, _theme,
                "ACTIVE: " + model.GetExistingEffectPolicy(_routineId),
                () => { model.ToggleExistingEffectPolicy(_routineId); RefreshDetails(); });
            assign.interactable = !_session.IsExecuting;
            hidden.interactable = !_session.IsExecuting;
            policy.interactable = model.IsAssigned(_routineId) && !_session.IsExecuting;

            AddHeading(_detailContent, "TARGETS - PARTY AND PETS");
            int targetRows = Math.Max(1, (model.Snapshot.Units.Count + 5) / 6);
            RectTransform targets = KingmakerUiFactory.CreateRect("TargetGrid", _detailContent);
            KingmakerUiFactory.AddLayout(targets, targetRows * 116);
            GridLayoutGroup targetLayout = targets.gameObject.AddComponent<GridLayoutGroup>();
            targetLayout.cellSize = new Vector2(92, 108);
            targetLayout.spacing = new Vector2(7, 7);
            targetLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            targetLayout.constraintCount = 6;
            targetLayout.childAlignment = TextAnchor.UpperLeft;
            foreach (UnitSnapshot unit in model.Snapshot.Units)
                CreateTargetCard(targets, model, unit);

            AddHeading(_detailContent, "PROVIDERS AND RESOURCES");
            foreach (ProviderSnapshot provider in source.Providers)
                CreateProviderRow(_detailContent, model, provider);

            AddHeading(_detailContent, "PLAN SUMMARY");
            try
            {
                RoutinePlanResult preview = _session.PreviewRoutine(_routineId);
                int fulfilled = preview.Plan.Outcomes.Count(item => item.Kind == TargetOutcomeKind.Fulfilled);
                int skipped = preview.Plan.Outcomes.Count(item => item.Kind == TargetOutcomeKind.SkippedAlreadyActive);
                int unfulfilled = preview.Plan.Outcomes.Count(item => item.Kind == TargetOutcomeKind.Unfulfilled);
                AddBodyText(_detailContent, "Planned casts: " + preview.Plan.Steps.Count +
                    "  |  fulfilled: " + fulfilled + "  |  active skipped: " + skipped +
                    "  |  unfulfilled: " + unfulfilled + "  |  unsupported saved sources: " +
                    preview.UnsupportedSourceIds.Count, 54);
            }
            catch (Exception exception)
            {
                AddBodyText(_detailContent, "Plan unavailable: " + exception.Message, 54);
            }

            AddHeading(_detailContent, "SETTINGS");
            RectTransform settings = AddHorizontalRow(_detailContent, 44);
            KingmakerUiFactory.CreateButton("ExecutionMode", settings, _theme,
                "MODE: " + model.Profile.Execution.Mode.ToUpperInvariant(),
                () => { model.ToggleExecutionMode(); RefreshDetails(); });
            KingmakerUiFactory.CreateButton("Combat", settings, _theme,
                model.Profile.Execution.OutOfCombatOnly ? "COMBAT: BLOCKED" : "COMBAT: ALLOWED",
                () => { model.ToggleOutOfCombatOnly(); RefreshDetails(); });
            KingmakerUiFactory.CreateButton("Fallback", settings, _theme,
                model.Profile.Execution.AllowAnimatedFallback ? "FALLBACK: ALLOWED" : "FALLBACK: BLOCKED",
                () => { model.ToggleAnimatedFallback(); RefreshDetails(); });
            FinalizeScrollContent(_detailContent, _detailViewport);
        }

        private void CreateTargetCard(RectTransform parent, PlannerSetupModel model, UnitSnapshot unit)
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
            bool wanted = model.IsTargetWanted(_routineId, unit.UnitId);
            Image background = card.targetGraphic as Image;
            if (background != null) background.color = wanted ? _theme.AccentSelected : _theme.PanelLight;
            card.interactable = model.IsAssigned(_routineId) && !_session.IsExecuting;
        }

        private void CreateProviderRow(RectTransform parent, PlannerSetupModel model, ProviderSnapshot provider)
        {
            RectTransform row = AddHorizontalRow(parent, 48);
            ResourcePoolSnapshot pool = model.GetResourcePool(provider);
            int? remaining = model.GetRemainingCasts(provider);
            string rejection = model.GetProviderRejectionReason(provider);
            Text label = KingmakerUiFactory.CreateText("Provider", row, _theme,
                model.GetCasterDisplayName(provider) + " - " + provider.DisplayName +
                "  CL " + provider.EffectiveCasterLevel + "  " + pool.Kind +
                "  remaining " + (remaining == null ? "unlimited" : remaining.Value.ToString()) +
                (string.IsNullOrEmpty(rejection) ? string.Empty : "  |  " + rejection),
                15, TextAnchor.MiddleLeft);
            LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1;
            ProviderPreferenceProfile preference = model.GetProviderPreference(provider.Key.Canonical);
            string state = preference == null ? "AUTO" : preference.Banned ? "BANNED" : "PRIORITY";
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
                "CAP " + (preference == null || preference.MaximumCasts == null
                    ? "ANY" : preference.MaximumCasts.Value.ToString()), 14, TextAnchor.MiddleCenter);
            cap.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;
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
            text.color = _theme.Accent;
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

        private void CycleConfigurationFilter()
        {
            if (!_configuredOnly && !_requestedOnly && !_unconfiguredOnly) _configuredOnly = true;
            else if (_configuredOnly)
            {
                _configuredOnly = false;
                _requestedOnly = true;
            }
            else if (_requestedOnly)
            {
                _requestedOnly = false;
                _unconfiguredOnly = true;
            }
            else _unconfiguredOnly = false;
            RefreshCatalog();
        }

        private List<SetupSourceRow> ApplyFilters(
            PlannerSetupModel model,
            out CatalogFilterDiagnostics diagnostics)
        {
            diagnostics = new CatalogFilterDiagnostics();
            var active = new List<string>();
            List<SetupSourceRow> values = model.Sources.ToList();
            diagnostics.TotalEntries = values.Count;
            RoutineProfile routine = model.Profile.Routines.First(item => item.RoutineId == _routineId);
            diagnostics.AssignedToActiveGroup = routine.Assignments.Count;

            string search = _search == null ? string.Empty : _search.text;
            if (!string.IsNullOrWhiteSpace(search))
            {
                values = values.Where(source => source.DisplayName.IndexOf(
                    search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                active.Add("search='" + search + "'");
            }
            diagnostics.AfterSearch = values.Count;

            if (_configuredOnly)
            {
                values = values.Where(source => model.Profile.Routines.Any(group =>
                    group.Assignments.Any(item => item.SourceId == source.SourceId))).ToList();
                active.Add("configured only");
            }
            else if (_requestedOnly)
            {
                values = values.Where(source => model.Profile.Routines.Any(group =>
                    group.Assignments.Any(item => item.SourceId == source.SourceId &&
                        item.WantedTargetUnitIds.Count != 0))).ToList();
                active.Add("requested only");
            }
            else if (_unconfiguredOnly)
            {
                values = values.Where(source => !model.Profile.Routines.Any(group =>
                    group.Assignments.Any(item => item.SourceId == source.SourceId))).ToList();
                active.Add("unconfigured only");
            }
            diagnostics.AfterConfigured = values.Count;

            if (_durationFilter == 1)
            {
                values = values.Where(source => source.ExpectedDurationRounds > 0 &&
                    source.ExpectedDurationRounds < 10).ToList();
                active.Add("short duration");
            }
            else if (_durationFilter == 2)
            {
                values = values.Where(source => source.ExpectedDurationRounds >= 10).ToList();
                active.Add("long duration");
            }
            else if (_durationFilter == 3)
            {
                values = values.Where(source => source.ExpectedDurationRounds == 0).ToList();
                active.Add("unknown duration");
            }
            diagnostics.AfterDuration = values.Count;

            if (_sourceKindFilter >= 0)
            {
                values = values.Where(source =>
                    (int)source.Ability.SourceKind == _sourceKindFilter).ToList();
                active.Add("source kind " + _sourceKindFilter);
            }
            diagnostics.AfterSource = values.Count;

            if (!_showHidden)
            {
                values = values.Where(source =>
                    !model.Profile.HiddenSourceIds.Contains(source.SourceId)).ToList();
                active.Add("hidden excluded");
            }
            else active.Add("hidden included");
            diagnostics.AfterHidden = values.Count;

            if (!_showUnavailable)
            {
                values = values.Where(model.IsSourceAvailable).ToList();
                active.Add("available only");
            }
            else active.Add("unavailable included");
            diagnostics.AfterAvailability = values.Count;
            diagnostics.VisibleViewModels = values.Count;
            diagnostics.ActiveFilters = active.Count == 0 ? "none" : string.Join(", ", active.ToArray());
            return values;
        }

        private void ResetFilters()
        {
            _configuredOnly = false;
            _requestedOnly = false;
            _unconfiguredOnly = false;
            _showHidden = false;
            _showUnavailable = false;
            _sortByLevel = false;
            _durationFilter = 0;
            _sourceKindFilter = -1;
            if (_search != null)
            {
                _resettingFilters = true;
                _search.text = string.Empty;
                _resettingFilters = false;
            }
            RefreshCatalog();
        }

        private void AddCatalogAction(RectTransform parent, string label, Action action)
        {
            Button button = KingmakerUiFactory.CreateButton("CatalogAction", parent, _theme,
                label, () => action());
            KingmakerUiFactory.AddLayout((RectTransform)button.transform, 42);
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
            SetupSourceRow source = model == null ? null : model.Sources.FirstOrDefault(item =>
                item.Ability.BaseAbilityGuid == bless || item.Ability.VariantGuid == bless);
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
                    (RectTransform)row.transform, _sourceViewport)) + ",bounds=" + bounds;
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
