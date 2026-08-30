using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Domain.Identity;
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
        private readonly CatalogFilterState _filters = new CatalogFilterState();
        private PlannerScreenViewModel _viewModel;
        private RectTransform _root;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private GraphicRaycaster _raycaster;
        private Image _blocker;
        private Text _status;
        private Text _result;
        private Text _catalogSummary;
        private Text _tooltip;
        private InputField _search;
        private PlannerRoutineTabsView _routineTabs;
        private PlannerCategoryTabsView _categoryTabs;
        private BuffGridView _grid;
        private PlannerSelectedBuffView _selected;
        private PlannerEnhancementChooserView _enhancementChooser;
        private PlannerCasterPolicyChooserView _casterPolicyChooser;
        private PlannerSettingsView _settings;
        private PlannerDescriptionModal _description;
        private Button _executeButton;
        private bool _disposed;
        private string _lastEnhancementRenderEvidence = string.Empty;

        internal BuffPlannerScreenView(StaticCanvas nativeCanvas, PlannerUiSession session,
            BuffPlannerUiLifecycleDiagnostics diagnostics, Action close, Action<string> execute)
        {
            if (nativeCanvas == null) throw new ArgumentNullException("nativeCanvas");
            _session = session ?? throw new ArgumentNullException("session");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
            _close = close ?? throw new ArgumentNullException("close");
            _execute = execute ?? throw new ArgumentNullException("execute");
            _theme = PlannerUiTheme.Resolve(nativeCanvas);
            _viewModel = new PlannerScreenViewModel(session, _filters);
            try
            {
                Build();
                RefreshAll(false);
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
        internal string ActiveRoutineId { get { return _viewModel.ActiveRoutineId; } }
        internal PlannerPresentationValidation LastValidation { get; private set; }
        internal CatalogLayoutDiagnostics LastCatalogDiagnostics { get; private set; }

        internal PlannerPresentationValidation ValidatePresentation()
        {
            if (_root == null) return LastValidation = PlannerPresentationValidation.Failed("root-null");
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            _root.GetWorldCorners(corners);
            float minX = corners.Min(corner => corner.x);
            float maxX = corners.Max(corner => corner.x);
            float minY = corners.Min(corner => corner.y);
            float maxY = corners.Max(corner => corner.y);
            float coveredWidth = Mathf.Max(0, Mathf.Min(Screen.width, maxX) - Mathf.Max(0, minX));
            float coveredHeight = Mathf.Max(0, Mathf.Min(Screen.height, maxY) - Mathf.Max(0, minY));
            float coverage = Screen.width <= 0 || Screen.height <= 0 ? 0 :
                coveredWidth * coveredHeight / (Screen.width * (float)Screen.height);
            EventSystem eventSystem = EventSystem.current;
            bool ownsCenter = false;
            string top = string.Empty;
            if (eventSystem != null)
            {
                var data = new PointerEventData(eventSystem)
                {
                    position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
                };
                var hits = new List<RaycastResult>();
                eventSystem.RaycastAll(data, hits);
                if (hits.Count != 0 && hits[0].gameObject != null)
                {
                    top = GetPath(hits[0].gameObject.transform);
                    ownsCenter = hits[0].gameObject.transform == _root ||
                        hits[0].gameObject.transform.IsChildOf(_root);
                }
            }
            bool controls = _root.Find("ServiceFrame/Header/Close") != null &&
                _root.Find("ServiceFrame/RoutineTabs") != null &&
                _root.Find("ServiceFrame/Footer/Execute") != null &&
                _root.Find("ServiceFrame/BuffGrid") != null &&
                _root.Find("ServiceFrame/SelectedBuff") != null;
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
                !ownsCenter ? "center-raycast-not-owned:" + top : string.Empty;
            LastValidation = new PlannerPresentationValidation(string.IsNullOrEmpty(failure),
                failure, _root.GetInstanceID(), _root.gameObject.activeSelf,
                _root.gameObject.activeInHierarchy, GetPath(_root), _canvas.name,
                _canvas.renderMode.ToString(), _canvas.sortingOrder, _canvas.overrideSorting,
                _root.anchorMin, _root.anchorMax, _root.pivot, _root.sizeDelta, _root.rect.size,
                corners, Screen.width, Screen.height, coverage, _canvasGroup.alpha,
                _canvasGroup.interactable, _canvasGroup.blocksRaycasts, _blocker.raycastTarget,
                _blocker.color.a, GetPath(_raycaster.transform),
                eventSystem == null ? string.Empty : GetPath(eventSystem.transform), ownsCenter, top);
            return LastValidation;
        }

        internal bool DispatchRoutineTabForRuntime(string routineId)
        {
            Button button = _routineTabs == null ? null : _routineTabs.Button(routineId);
            if (button == null) return false;
            button.onClick.Invoke();
            return ActiveRoutineId == routineId;
        }

        internal void RefreshAll() { RefreshAll(true); }

        private void RefreshAll(bool preserveScroll)
        {
            if (!IsAlive) return;
            PlannerSetupModel model = _session.Model;
            bool ready = model != null && !_session.IsExecuting;
            if (model == null)
            {
                _status.text = "Load a campaign to begin.";
                _result.text = "Load a campaign to configure and execute buff routines.";
            }
            else
            {
                _status.text = model.Snapshot.Units.Count + " targets | " +
                    model.Sources.Count + " buffs found";
                if (string.IsNullOrWhiteSpace(_result.text))
                    _result.text = _session.ProfileStatus.StartsWith("No prior profile",
                        StringComparison.Ordinal) ? "New planner setup created for this campaign." :
                        "Planner setup is ready.";
            }
            _routineTabs.Bind(ActiveRoutineId, _viewModel.RoutineSummary, ready);
            _categoryTabs.Bind(_viewModel.Category, _viewModel.SelectedOnly, ready);
            RefreshCatalog(preserveScroll);
            if (model != null) _settings.Bind(new PlannerSettingsViewModel(model.Profile), ready);
            _executeButton.interactable = ready;
            Text executeLabel = _executeButton.GetComponentInChildren<Text>(true);
            if (executeLabel != null) executeLabel.text = "APPLY " + ActiveRoutineId.ToUpperInvariant();
            KingmakerUiFactory.ForceLayoutAndSnap(_root);
        }

        private void RefreshCatalog(bool preserveScroll)
        {
            PlannerSetupModel model = _session.Model;
            CatalogFilterDiagnostics filters;
            IReadOnlyList<BuffCardViewModel> cards = _viewModel.Cards(out filters);
            if (model != null && cards.Count != 0 && !cards.Any(card => card.SourceId == model.SelectedSourceId))
                model.SelectSource(cards[0].SourceId);
            _grid.Bind(cards, preserveScroll);
            _catalogSummary.text = cards.Count + (cards.Count == 1 ? " buff" : " buffs") +
                " • alphabetical • " + filters.ActiveFilters;
            RefreshSelected();
            KingmakerUiFactory.ForceLayoutAndSnap(_root);
            RecordEnhancementRenderEvidence();
            UpdateCatalogDiagnostics(filters);
        }

        private void RefreshSelected()
        {
            PlannerSetupModel model = _session.Model;
            SetupSourceRow source = model == null ? null : model.SelectedSource;
            IReadOnlyList<TargetPortraitViewModel> targets = _viewModel.Targets();
            string planSummary = _viewModel.PlanSummary();
            SelectedCastingViewModel casting = SelectedCastingViewModel.Create(source, model,
                ActiveRoutineId, _session.LastPreview);
            _selected.Bind(source, ResolveAbilityIcon(source), ActiveRoutineId,
                targets, ResolvePortrait, unitId =>
                {
                    model.ToggleTarget(ActiveRoutineId, unitId);
                    RefreshAll(true);
                }, StatusColor, planSummary, casting, model != null && !_session.IsExecuting);
        }

        private void OpenEnhancementChooser()
        {
            PlannerSetupModel model = _session.Model;
            SetupSourceRow source = model == null ? null : model.SelectedSource;
            if (source == null) return;
            _casterPolicyChooser.Hide();
            RoutinePlanResult preview = null;
            try { preview = _session.PreviewRoutine(ActiveRoutineId); }
            catch { preview = null; }
            _enhancementChooser.Show(SelectedCastingViewModel.Create(source, model,
                ActiveRoutineId, preview));
        }

        private void OpenCasterPolicyChooser()
        {
            PlannerSetupModel model = _session.Model;
            SetupSourceRow source = model == null ? null : model.SelectedSource;
            if (source == null) return;
            _enhancementChooser.Hide();
            RoutinePlanResult preview = null;
            try { preview = _session.PreviewRoutine(ActiveRoutineId); }
            catch { preview = null; }
            _casterPolicyChooser.Show(CasterPolicyViewModel.Create(
                source, model, ActiveRoutineId, preview),
                ResolvePortrait, !_session.IsExecuting);
        }

        private void RefreshCasterPolicyChooser()
        {
            RefreshAll(true);
            OpenCasterPolicyChooser();
        }

        private void RecordEnhancementRenderEvidence()
        {
            if (_selected == null || _session.Model == null) return;
            SelectedCastingViewModel casting = SelectedCastingViewModel.Create(
                _session.Model.SelectedSource, _session.Model, ActiveRoutineId, _session.LastPreview);
            string evidence = _selected.EnhancementRenderEvidence(casting.CandidateCount,
                casting.SelectedEnhancementId);
            if (evidence == _lastEnhancementRenderEvidence) return;
            _lastEnhancementRenderEvidence = evidence;
            _session.RecordEnhancementUiEvidence(evidence);
        }
        internal bool DispatchBlessRowForRuntime()
        {
            if (_session.Model == null) return false;
            const string bless = "90e59f4a4ada87243b7b3535a06d0638";
            SetupSourceRow source = _session.Model.Sources.Where(item =>
                    item.Abilities.Any(ability => ability.BaseAbilityGuid == bless ||
                        ability.VariantGuid == bless))
                .OrderBy(item => item.Ability.SourceKind == SourceKind.Spellbook ? 0 : 1)
                .FirstOrDefault();
            return source != null && _grid.SelectForRuntime(source.SourceId) &&
                _session.Model.SelectedSourceId == source.SourceId;
        }

        internal bool SelectFirstRowForRuntime()
        {
            PlannerSetupModel model = _session.Model;
            SetupSourceRow first = model == null ? null : model.Sources
                .OrderBy(source => source.DisplayName, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            return first != null && _grid.SelectForRuntime(first.SourceId);
        }

        internal bool DispatchTargetForRuntime(string unitId)
        {
            return _selected != null && _selected.InvokeTarget(unitId);
        }

        internal LiveRowRenderDiagnostics GetLiveRowRenderDiagnostics()
        {
            KingmakerUiFactory.ForceLayoutAndSnap(_root);
            PlannerSetupModel model = _session.Model;
            List<BuffCardView> cards = _grid.Cards.Where(card =>
                card.Rect.gameObject.activeInHierarchy).Take(5).ToList();
            Text[] allText = _root.GetComponentsInChildren<Text>(true);
            RectTransform[] pixelSnapRects = allText.Select(text => text.rectTransform)
                .Concat(cards.Select(card => card.Rect)).Distinct().ToArray();
            return new LiveRowRenderDiagnostics
            {
                ExpectedNames = cards.Select(card => card.Rect.GetComponentsInChildren<Text>(true)
                    .First(text => text.name == "Name").text).ToArray(),
                RowScreenRectangles = cards.Select(card => ScreenRectangle(card.Rect)).ToArray(),
                SelectedRowName = model == null || model.SelectedSource == null ? string.Empty :
                    model.SelectedSource.DisplayName,
                DetailsTitleText = _selected.BoundName,
                BoundRowCount = cards.Count,
                SourceViewport = RectEvidence(_grid.Viewport),
                SourceContent = RectEvidence(_grid.Content),
                MaskEvidence = BuildMaskEvidence(),
                CanaryEvidence = "absent",
                RowEvidence = cards.Select(card => BuildGraphicEvidence(card.Rect.gameObject)).ToArray(),
                DetailsEvidence = _selected.Root.GetComponentsInChildren<Graphic>(true).Take(8)
                    .Select(graphic => BuildGraphicEvidence(graphic.gameObject)).ToArray(),
                AbilityIconCount = cards.Count(card => card.Rect.Find("IconFrame/AbilityIcon") != null &&
                    card.Rect.Find("IconFrame/AbilityIcon").gameObject.activeSelf),
                MissingIconCount = cards.Count(card => card.Rect.Find("IconFrame/MissingIcon") != null &&
                    card.Rect.Find("IconFrame/MissingIcon").gameObject.activeSelf),
                CastingModeControlCount = allText.Count(text =>
                    text.text.StartsWith("Casting mode: ", StringComparison.Ordinal)),
                RetiredPrimaryLabelCount = allText.Count(text => IsRetired(text.text)),
                ThemeResolution = _theme.ResolutionSummary,
                TextRenderingEvidence = BuildTextRenderingEvidence(),
                NestedCanvasScalerCount = _root.GetComponentsInChildren<CanvasScaler>(true).Length,
                FractionalRectCount = pixelSnapRects.Count(HasFractionalGeometry),
                PixelSnapEvidence = "scope=text+active-cards;rects=" + pixelSnapRects.Length +
                    ";fractional=" + pixelSnapRects.Count(HasFractionalGeometry)
            };
        }

        internal void RefreshCatalogForRuntime() { RefreshCatalog(true); }

        internal string DispatchCatalogControlsForRuntime()
        {
            var evidence = new List<string>();
            foreach (PlannerSourceCategory category in Enum.GetValues(typeof(PlannerSourceCategory)))
            {
                Button button = _categoryTabs.Button(category);
                if (button == null) return string.Empty;
                button.onClick.Invoke();
                evidence.Add(category + "=" + LastCatalogDiagnostics.Filters.VisibleViewModels);
            }
            _categoryTabs.Button(PlannerSourceCategory.All).onClick.Invoke();
            _categoryTabs.SelectedOnlyButton.onClick.Invoke();
            evidence.Add("longSelected=" + LastCatalogDiagnostics.Filters.VisibleViewModels);
            if (!DispatchRoutineTabForRuntime("important")) return string.Empty;
            evidence.Add("importantSelected=" + LastCatalogDiagnostics.Filters.VisibleViewModels);
            if (!DispatchRoutineTabForRuntime("long")) return string.Empty;
            evidence.Add("longRestored=" + LastCatalogDiagnostics.Filters.VisibleViewModels);
            _categoryTabs.SelectedOnlyButton.onClick.Invoke();
            evidence.Add("selectedOnlyOff=" + (!_viewModel.SelectedOnly));
            return string.Join(";", evidence.ToArray());
        }

        internal bool PrepareVisualEvidenceForRuntime(string view)
        {
            bool settings = view == "settings";
            _settings.Show(settings);
            Canvas.ForceUpdateCanvases();
            return true;
        }

        internal void ShowResult(QuickExecutionResult result)
        {
            if (_result != null) _result.text = result == null ? _session.Status : result.Message;
            RefreshAll(true);
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

        private void Build()
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
            _raycaster = _root.gameObject.AddComponent<GraphicRaycaster>();
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
            BlocksRaycasts = true;
            HasGraphicRaycaster = true;

            RectTransform frame = KingmakerUiFactory.CreateRect("ServiceFrame", _root);
            KingmakerUiFactory.SetAnchors(frame, 0.025f, 0.025f, 0.975f, 0.975f);
            KingmakerUiFactory.AddFramedPanel(frame, _theme.ServiceSurface,
                _theme.BurgundyPrimary, 2f);
            BuildHeader(frame);
            _routineTabs = new PlannerRoutineTabsView(frame, _theme, routineId =>
            {
                _viewModel.SetRoutine(routineId);
                RefreshAll(false);
            });
            _search = KingmakerUiFactory.CreateInputField("Search", frame, _theme, "Search buffs...");
            KingmakerUiFactory.SetAnchors((RectTransform)_search.transform,
                0.02f, 0.805f, 0.31f, 0.85f);
            _search.onValueChanged.AddListener(value =>
            {
                _viewModel.SetSearch(value);
                RefreshCatalog(false);
            });
            RectTransform categories = KingmakerUiFactory.CreateRect("CategoryTabs", frame);
            KingmakerUiFactory.SetAnchors(categories, 0.325f, 0.805f, 0.98f, 0.85f);
            _categoryTabs = new PlannerCategoryTabsView(categories, _theme, category =>
            {
                _viewModel.SetCategory(category);
                RefreshAll(false);
            }, () =>
            {
                _viewModel.ToggleSelectedOnly();
                RefreshAll(false);
            }, ShowTooltip);
            _catalogSummary = KingmakerUiFactory.CreateText("CatalogSummary", frame, _theme,
                string.Empty, 13, TextAnchor.MiddleLeft);
            _catalogSummary.color = _theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(_catalogSummary.rectTransform, 0.02f, 0.775f, 0.98f, 0.805f);
            _grid = new BuffGridView(frame, _theme, ResolveAbilityIcon, sourceId =>
            {
                _enhancementChooser.Hide();
                _casterPolicyChooser.Hide();
                _session.Model.SelectSource(sourceId);
                RefreshCatalog(true);
            }, OpenDescription, StatusColor);
            BuffCardGridScrollSink gridScroll = _grid.Scroll.gameObject
                .AddComponent<BuffCardGridScrollSink>();
            gridScroll.Scroll = _grid.Scroll;
            gridScroll.Diagnostics = _diagnostics;
            _selected = new PlannerSelectedBuffView(frame, _theme, () =>
            {
                _session.Model.SetAllValidTargets(ActiveRoutineId, true);
                RefreshAll(true);
            }, () =>
            {
                _session.Model.SetAllValidTargets(ActiveRoutineId, false);
                RefreshAll(true);
            }, OpenCasterPolicyChooser, OpenEnhancementChooser, ShowTooltip);
            _enhancementChooser = new PlannerEnhancementChooserView(_root, _theme,
                enhancementId =>
                {
                    _session.Model.SetEnhancement(ActiveRoutineId, enhancementId);
                    _enhancementChooser.Hide();
                    RefreshAll(true);
                }, ShowTooltip);
            _casterPolicyChooser = new PlannerCasterPolicyChooserView(
                _root, _theme,
                (providerKey, enabled) =>
                {
                    _session.Model.SetProviderEnabled(providerKey, enabled);
                    RefreshCasterPolicyChooser();
                },
                providerKey =>
                {
                    _session.Model.MoveProviderEarlier(providerKey);
                    RefreshCasterPolicyChooser();
                },
                providerKey =>
                {
                    _session.Model.MoveProviderLater(providerKey);
                    RefreshCasterPolicyChooser();
                },
                (providerKey, maximum) =>
                {
                    _session.Model.SetProviderMaximumCasts(
                        providerKey, maximum);
                    RefreshCasterPolicyChooser();
                },
                () =>
                {
                    _session.Model.ResetSelectedSourceProvidersToAutomatic();
                    RefreshCasterPolicyChooser();
                },
                ShowTooltip);
            BuildFooter(frame);
            _settings = new PlannerSettingsView(frame, _theme, () =>
            {
                _session.Model.ToggleExecutionMode(); RefreshAll(true);
            }, () =>
            {
                _session.Model.ToggleOutOfCombatOnly(); RefreshAll(true);
            }, () =>
            {
                _session.Model.ToggleRecastExisting(); RefreshAll(true);
            }, () =>
            {
                _session.Model.ToggleAnimatedFallback(); RefreshAll(true);
            }, () =>
            {
                _session.Model.TogglePlannerHotkey();
                PlannerHotkey.SetBinding(_session.Model.Profile.Ui.Hotkey);
                RefreshAll(true);
            }, () => _settings.Show(false));
            _description = new PlannerDescriptionModal(_root, _theme);
            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
            PlannerPointerOwnership.Register(_root);
            _diagnostics.RecordScreenCreated();
        }

        private void BuildHeader(RectTransform frame)
        {
            RectTransform header = KingmakerUiFactory.CreateRect("Header", frame);
            KingmakerUiFactory.SetAnchors(header, 0, 0.93f, 1, 1, 14, 14, 5, 5);
            KingmakerUiFactory.AddFramedPanel(header, _theme.ParchmentRaised, _theme.GoldAccent);
            Text title = KingmakerUiFactory.CreateText("Title", header, _theme,
                "BUFF PLANNER", 28, TextAnchor.MiddleCenter);
            KingmakerUiFactory.Stretch(title.rectTransform, 100, 200, 4, 4);
            _status = KingmakerUiFactory.CreateText("Status", header, _theme,
                string.Empty, 14, TextAnchor.LowerLeft);
            _status.color = _theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(_status.rectTransform, 0.01f, 0.02f, 0.40f, 0.44f);
            Button settings = KingmakerUiFactory.CreateButton("Settings", header, _theme,
                "Settings", () => _settings.Show(!_settings.IsOpen));
            KingmakerUiFactory.SetAnchors((RectTransform)settings.transform,
                0.82f, 0.14f, 0.93f, 0.86f);
            Button close = KingmakerUiFactory.CreateButton("Close", header, _theme,
                "X", () => _close());
            KingmakerUiFactory.SetAnchors((RectTransform)close.transform,
                0.94f, 0.14f, 0.99f, 0.86f);
        }

        private void BuildFooter(RectTransform frame)
        {
            RectTransform footer = KingmakerUiFactory.CreateRect("Footer", frame);
            KingmakerUiFactory.SetAnchors(footer, 0.02f, 0.012f, 0.98f, 0.068f);
            KingmakerUiFactory.AddFramedPanel(footer, _theme.ParchmentRaised, _theme.GoldAccent);
            _result = KingmakerUiFactory.CreateText("Result", footer, _theme,
                string.Empty, 14, TextAnchor.MiddleLeft);
            KingmakerUiFactory.SetAnchors(_result.rectTransform, 0.015f, 0.10f, 0.68f, 0.90f);
            _tooltip = KingmakerUiFactory.CreateText("Tooltip", footer, _theme,
                string.Empty, 13, TextAnchor.MiddleLeft);
            _tooltip.color = _theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(_tooltip.rectTransform, 0.015f, 0.10f, 0.68f, 0.90f);
            _tooltip.gameObject.SetActive(false);
            Button close = KingmakerUiFactory.CreateButton("Close", footer, _theme,
                "CLOSE", () => _close());
            KingmakerUiFactory.SetAnchors((RectTransform)close.transform,
                0.72f, 0.12f, 0.82f, 0.88f);
            _executeButton = KingmakerUiFactory.CreateButton("Execute", footer, _theme,
                "APPLY LONG", () => _execute(ActiveRoutineId));
            KingmakerUiFactory.SetAnchors((RectTransform)_executeButton.transform,
                0.83f, 0.08f, 0.985f, 0.92f);
        }

        private void ShowTooltip(string value)
        {
            bool show = !string.IsNullOrWhiteSpace(value);
            _tooltip.text = value ?? string.Empty;
            _tooltip.gameObject.SetActive(show);
            _result.gameObject.SetActive(!show);
        }

        private void OpenDescription(string sourceId)
        {
            PlannerSetupModel model = _session.Model;
            PlannerDescriptionRequest request;
            if (model == null || !PlannerDescriptionRequest.TryCreate(
                    PlannerPointerGesture.Right, sourceId, model.Sources, out request))
                return;
            SetupSourceRow source = model.Sources.First(item =>
                string.Equals(item.SourceId, request.SourceId, StringComparison.Ordinal));
            BlueprintAbility ability = ResolveAbilityBlueprint(request.Ability);
            if (ability == null)
            {
                ShowResult(new QuickExecutionResult(ActiveRoutineId, "Description",
                    QuickExecutionDisposition.Refused,
                    "The selected ability blueprint is no longer available.", 0, 0, 0));
                return;
            }
            _description.Show(source, ability, ResolveAbilityIcon(source));
        }

        private Sprite ResolveAbilityIcon(string sourceId)
        {
            PlannerSetupModel model = _session.Model;
            return ResolveAbilityIcon(model == null ? null : model.Sources
                .FirstOrDefault(source => source.SourceId == sourceId));
        }

        private static Sprite ResolveAbilityIcon(SetupSourceRow source)
        {
            if (source == null) return null;
            if (!string.IsNullOrWhiteSpace(source.Ability.VariantGuid))
            {
                BlueprintAbility concrete =
                    ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(
                        source.Ability.VariantGuid);
                if (concrete != null && concrete.Icon != null) return concrete.Icon;
            }
            BlueprintAbility parent = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(
                source.Ability.BaseAbilityGuid);
            return parent == null ? null : parent.Icon;
        }

        private static BlueprintAbility ResolveAbilityBlueprint(AbilityKey ability)
        {
            if (ability == null) return null;
            if (!string.IsNullOrWhiteSpace(ability.VariantGuid))
            {
                BlueprintAbility concrete =
                    ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(
                        ability.VariantGuid);
                if (concrete != null) return concrete;
            }
            return ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(
                ability.BaseAbilityGuid);
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

        private void UpdateCatalogDiagnostics(CatalogFilterDiagnostics filters)
        {
            Canvas.ForceUpdateCanvases();
            List<BuffCardView> active = _grid.Cards.Where(card =>
                card.Rect.gameObject.activeInHierarchy).ToList();
            PlannerSetupModel model = _session.Model;
            LastCatalogDiagnostics = new CatalogLayoutDiagnostics
            {
                Filters = filters,
                InstantiatedRows = _grid.Cards.Count,
                ActiveRows = active.Count,
                VisibleRows = active.Count(card => RectanglesOverlap(card.Rect, _grid.Viewport)),
                ContentWidth = _grid.Content.rect.width,
                ContentHeight = _grid.Content.rect.height,
                ViewportWidth = _grid.Viewport.rect.width,
                ViewportHeight = _grid.Viewport.rect.height,
                DetailChildren = _selected.TargetCount + 6,
                SelectedSourceId = model == null ? string.Empty : model.SelectedSourceId,
                SelectedDetailsBound = model != null && model.SelectedSource != null,
                BindingFailure = _session.LastBindingFailure ?? string.Empty,
                BlessEvidence = BuildBlessEvidence(model),
                ProviderCount = model == null ? 0 : model.Snapshot.Providers.Count,
                AggregateAbilityCount = model == null ? 0 : model.Sources.Sum(source => source.Abilities.Count),
                ConsolidatedCardCount = model == null ? 0 : model.Sources.Count(source =>
                    source.Abilities.Count > 1),
                DirectSelectedTargetCount = model == null ? 0 : _viewModel.Targets().Count(target =>
                    target.State == TargetPortraitState.DirectSelectedAndCovered),
                IndirectCoveredTargetCount = model == null ? 0 : _viewModel.Targets().Count(target =>
                    target.State == TargetPortraitState.IndirectlyCovered)
            };
        }

        internal CatalogLayoutDiagnostics GetCatalogDiagnostics()
        {
            CatalogFilterDiagnostics filters;
            _viewModel.Cards(out filters);
            UpdateCatalogDiagnostics(filters);
            return LastCatalogDiagnostics;
        }

        private string BuildBlessEvidence(PlannerSetupModel model)
        {
            const string bless = "90e59f4a4ada87243b7b3535a06d0638";
            SetupSourceRow source = model == null ? null : model.Sources.FirstOrDefault(item =>
                item.Abilities.Any(ability => ability.BaseAbilityGuid == bless ||
                    ability.VariantGuid == bless));
            if (source == null) return "absent";
            BuffCardView card = _grid.Cards.FirstOrDefault(item => item.SourceId == source.SourceId);
            return "source=" + source.SourceId + ",available=" + model.IsSourceAvailable(source) +
                ",assigned=" + model.Profile.Routines.Any(routine => routine.Assignments.Any(
                    assignment => assignment.SourceId == source.SourceId &&
                    assignment.WantedTargetUnitIds.Count != 0)) + ",providers=" +
                source.Providers.Count + ",abilities=" + source.Abilities.Count +
                ",migrated=" + model.AssignmentMigrationApplied +
                ",card=" + (card != null) + ",rowVisible=" +
                (card != null && RectanglesOverlap(card.Rect, _grid.Viewport)) + ",material=" +
                (_session.CatalogDiscovery == null ? "missing" :
                    _session.CatalogDiscovery.BlessMaterialEvidence);
        }

        private string BuildMaskEvidence()
        {
            Mask mask = _grid.Viewport.GetComponent<Mask>();
            Image image = _grid.Viewport.GetComponent<Image>();
            return "mask=" + (mask != null) + ",enabled=" + (mask != null && mask.enabled) +
                ",showGraphic=" + (mask != null && mask.showMaskGraphic) +
                ",viewportImage=" + GraphicEvidence(image) + ",viewport=" +
                RectEvidence(_grid.Viewport);
        }

        private string BuildTextRenderingEvidence()
        {
            Text sample = _root.GetComponentsInChildren<Text>(true).FirstOrDefault(text =>
                text.gameObject.activeInHierarchy);
            Material material = sample == null ? null : sample.material;
            return "canvas=" + (_canvas == null ? "missing" : _canvas.name) +
                ",renderMode=" + (_canvas == null ? "missing" : _canvas.renderMode.ToString()) +
                ",scaleFactor=" + (_canvas == null ? 0f : _canvas.scaleFactor).ToString("F3") +
                ",rootScale=" + (_root == null ? Vector3.zero : _root.localScale) +
                ",nestedCanvases=" + (_root == null ? 0 :
                    _root.GetComponentsInChildren<Canvas>(true).Length - 1) +
                ",canvasScalers=" + (_root == null ? 0 :
                    _root.GetComponentsInChildren<CanvasScaler>(true).Length) +
                ",textType=" + (sample == null ? "missing" : sample.GetType().FullName) +
                ",font=" + (sample == null || sample.font == null ? "missing" : sample.font.name) +
                ",fontSize=" + (sample == null ? 0 : sample.fontSize) +
                ",bestFit=" + (sample != null && sample.resizeTextForBestFit) +
                ",shader=" + (material == null || material.shader == null ? "missing" :
                    material.shader.name) + ",rect=" +
                (sample == null ? "missing" : RectEvidence(sample.rectTransform));
        }

        private static bool HasFractionalGeometry(RectTransform rect)
        {
            if (rect == null) return false;
            const float tolerance = 0.01f;
            return Mathf.Abs(rect.anchoredPosition.x - Mathf.Round(rect.anchoredPosition.x)) > tolerance ||
                Mathf.Abs(rect.anchoredPosition.y - Mathf.Round(rect.anchoredPosition.y)) > tolerance ||
                Mathf.Abs(rect.sizeDelta.x - Mathf.Round(rect.sizeDelta.x)) > tolerance ||
                Mathf.Abs(rect.sizeDelta.y - Mathf.Round(rect.sizeDelta.y)) > tolerance;
        }

        private static bool IsRetired(string value)
        {
            string[] retired = { "Configured" + " only", "Show" + " hidden", "Advanced" + " Filters",
                "Casting" + " Source", "Advanced Casting" + " Source", "Add to" + " Long",
                "Add to" + " Important", "Add to" + " Short", "Available" + "-only", "CAP" + " ANY" };
            return retired.Any(label => string.Equals(value, label, StringComparison.OrdinalIgnoreCase));
        }

        private static bool RectanglesOverlap(RectTransform first, RectTransform second)
        {
            var a = new Vector3[4]; var b = new Vector3[4];
            first.GetWorldCorners(a); second.GetWorldCorners(b);
            return a.Max(v => v.x) > b.Min(v => v.x) && a.Min(v => v.x) < b.Max(v => v.x) &&
                a.Max(v => v.y) > b.Min(v => v.y) && a.Min(v => v.y) < b.Max(v => v.y);
        }

        private static string GetPath(Transform transform)
        {
            var names = new List<string>();
            while (transform != null) { names.Add(transform.name); transform = transform.parent; }
            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        private static string RectEvidence(RectTransform rect)
        {
            return rect == null ? "missing" : "anchors=" + rect.anchorMin + ".." + rect.anchorMax +
                ",pivot=" + rect.pivot + ",anchored=" + rect.anchoredPosition +
                ",sizeDelta=" + rect.sizeDelta + ",rect=" + rect.rect +
                ",screen=" + ScreenRectangle(rect);
        }

        private static string ScreenRectangle(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return corners.Min(v => v.x).ToString("F1") + "," +
                corners.Min(v => v.y).ToString("F1") + "-" +
                corners.Max(v => v.x).ToString("F1") + "," +
                corners.Max(v => v.y).ToString("F1");
        }

        private static string BuildGraphicEvidence(GameObject root)
        {
            if (root == null) return "missing";
            CanvasRenderer renderer = root.GetComponent<CanvasRenderer>();
            Graphic rootGraphic = root.GetComponent<Graphic>();
            Material material = rootGraphic == null ? null : rootGraphic.materialForRendering;
            float inheritedAlpha = root.GetComponentsInParent<CanvasGroup>(true)
                .Aggregate(1f, (alpha, group) => alpha * group.alpha);
            return "path=" + GetPath(root.transform) + ";id=" + root.GetInstanceID() +
                ";active=" + root.activeInHierarchy + ";rendererCull=" +
                (renderer != null && renderer.cull) + ";inheritedAlpha=" +
                inheritedAlpha.ToString("0.###") + ";shader=" +
                (material == null || material.shader == null ? string.Empty :
                    material.shader.name) + ";rect=" +
                RectEvidence(root.transform as RectTransform) + ";graphics=" +
                string.Join("|", root.GetComponentsInChildren<Graphic>(true)
                    .Select(GraphicEvidence).ToArray());
        }

        private static string GraphicEvidence(Graphic graphic)
        {
            if (graphic == null) return "missing";
            Text text = graphic as Text;
            return graphic.GetType().Name + "(path=" + GetPath(graphic.transform) +
                ",enabled=" + graphic.enabled + ",raycast=" + graphic.raycastTarget +
                ",color=" + graphic.color + ",cull=" + graphic.canvasRenderer.cull +
                ",font=" + (text == null || text.font == null ? string.Empty : text.font.name) +
                ",fontSize=" + (text == null ? 0 : text.fontSize) +
                ",bestFit=" + (text != null && text.resizeTextForBestFit) +
                ",scale=" + graphic.rectTransform.localScale +
                ",text=" + (text == null ? string.Empty : text.text.Replace("\n", " ")) + ")";
        }
    }

    internal sealed class PlannerDescriptionModal
    {
        private readonly Image _icon;
        private readonly Text _fallback;
        private readonly Text _name;
        private readonly Text _meta;
        private readonly Text _body;
        private readonly ScrollRect _scroll;

        internal PlannerDescriptionModal(RectTransform parent, PlannerUiTheme theme)
        {
            Root = KingmakerUiFactory.CreateRect("DescriptionModal", parent);
            KingmakerUiFactory.Stretch(Root);
            Image backdrop = KingmakerUiFactory.AddPanel(Root,
                new Color(0.05f, 0.035f, 0.025f, 0.82f));
            backdrop.raycastTarget = true;
            RectTransform frame = KingmakerUiFactory.CreateRect("DescriptionFrame", Root);
            KingmakerUiFactory.SetAnchors(frame, 0.19f, 0.08f, 0.81f, 0.92f);
            KingmakerUiFactory.AddFramedPanel(frame, theme.ParchmentRaised,
                theme.GoldAccent, 3f);

            RectTransform iconFrame = KingmakerUiFactory.CreateRect("AbilityIconFrame", frame);
            KingmakerUiFactory.SetAnchors(iconFrame, 0.035f, 0.79f, 0.16f, 0.965f);
            KingmakerUiFactory.AddFramedPanel(iconFrame,
                new Color(0.16f, 0.10f, 0.07f, 1f), theme.GoldAccent, 2f).raycastTarget = false;
            RectTransform iconRect = KingmakerUiFactory.CreateRect("AbilityIcon", iconFrame);
            KingmakerUiFactory.Stretch(iconRect, 5, 5, 5, 5);
            _icon = iconRect.gameObject.AddComponent<Image>();
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
            _fallback = KingmakerUiFactory.CreateText("MissingIcon", iconFrame, theme,
                "?", 40, TextAnchor.MiddleCenter);
            KingmakerUiFactory.Stretch(_fallback.rectTransform);

            _name = KingmakerUiFactory.CreateText("AbilityName", frame, theme,
                string.Empty, 25, TextAnchor.MiddleLeft);
            _name.fontStyle = FontStyle.Bold;
            _name.color = theme.BurgundyPrimary;
            _name.horizontalOverflow = HorizontalWrapMode.Wrap;
            _name.verticalOverflow = VerticalWrapMode.Overflow;
            KingmakerUiFactory.SetAnchors(_name.rectTransform, 0.18f, 0.82f, 0.86f, 0.965f);
            _meta = KingmakerUiFactory.CreateText("AbilityMeta", frame, theme,
                string.Empty, 15, TextAnchor.MiddleLeft);
            _meta.color = theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(_meta.rectTransform, 0.18f, 0.75f, 0.86f, 0.82f);

            Button close = KingmakerUiFactory.CreateButton("CloseDescription", frame, theme,
                "X", Hide);
            KingmakerUiFactory.SetAnchors((RectTransform)close.transform,
                0.89f, 0.90f, 0.965f, 0.97f);

            RectTransform content;
            _scroll = KingmakerUiFactory.CreateScrollView("DescriptionScroll", frame,
                theme, out content);
            KingmakerUiFactory.SetAnchors((RectTransform)_scroll.transform,
                0.035f, 0.055f, 0.965f, 0.73f);
            ContentSizeFitter contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _body = KingmakerUiFactory.CreateText("DescriptionBody", content, theme,
                string.Empty, 16, TextAnchor.UpperLeft);
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Overflow;
            ContentSizeFitter bodyFitter = _body.gameObject.AddComponent<ContentSizeFitter>();
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            PlannerDescriptionEscape escape = Root.gameObject
                .AddComponent<PlannerDescriptionEscape>();
            escape.Close = Hide;
            Root.gameObject.SetActive(false);
        }

        internal RectTransform Root { get; private set; }
        internal bool IsOpen { get { return Root.gameObject.activeSelf; } }

        internal void Show(SetupSourceRow source, BlueprintAbility ability, Sprite icon)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (ability == null) throw new ArgumentNullException("ability");
            _icon.sprite = icon;
            _icon.gameObject.SetActive(icon != null);
            _fallback.gameObject.SetActive(icon == null);
            _name.text = source.DisplayName;
            string school = ability.School.ToString() == "None"
                ? string.Empty : ability.School.ToString();
            _meta.text = BuffCardViewModel.SourceSummary(source) +
                (source.SpellLevel > 0 ? " | Level " + source.SpellLevel : string.Empty) +
                (string.IsNullOrWhiteSpace(school) ? string.Empty : " | " + school);
            _body.text = BuildBody(ability);
            _scroll.verticalNormalizedPosition = 1f;
            Root.SetAsLastSibling();
            Root.gameObject.SetActive(true);
            KingmakerUiFactory.ForceLayoutAndSnap(Root);
        }

        internal void Hide()
        {
            Root.gameObject.SetActive(false);
        }

        private static string BuildBody(BlueprintAbility ability)
        {
            var sections = new List<string>();
            Add(sections, "Target", Safe(ability.GetTarget));
            Add(sections, "Duration", ability.LocalizedDuration);
            Add(sections, "Casting time", Safe(() => UIUtilityTexts.GetAbilityActionText(ability)));
            Add(sections, "Saving throw", ability.LocalizedSavingThrow);
            Add(sections, "Spell resistance", Safe(() =>
                UIUtilityTexts.GetSpellResistanceText(ability)));
            Add(sections, "Descriptors", Safe(() =>
                UIUtilityTexts.GetSpellDescriptorsText(ability)));
            if (!string.IsNullOrWhiteSpace(ability.Description))
                sections.Add(ability.Description);
            return string.Join("\n\n", sections.ToArray());
        }

        private static void Add(ICollection<string> sections, string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                sections.Add("<b>" + label + "</b>\n" + value.Trim());
        }

        private static string Safe(Func<string> value)
        {
            try { return value == null ? string.Empty : value() ?? string.Empty; }
            catch (Exception) { return string.Empty; }
        }
    }

    internal sealed class PlannerDescriptionEscape : MonoBehaviour
    {
        internal Action Close;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && Close != null) Close();
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
            ActiveSelf = activeSelf; ActiveInHierarchy = activeInHierarchy;
            Hierarchy = hierarchy ?? string.Empty; CanvasIdentity = canvasIdentity ?? string.Empty;
            RenderMode = renderMode ?? string.Empty; SortingOrder = sortingOrder;
            OverrideSorting = overrideSorting; AnchorMin = anchorMin; AnchorMax = anchorMax;
            Pivot = pivot; SizeDelta = sizeDelta; RectSize = rectSize;
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
            return "valid=" + Valid + ";failure=" + Failure + ";root=" + RootInstanceId +
                ";activeSelf=" + ActiveSelf + ";activeHierarchy=" + ActiveInHierarchy +
                ";hierarchy=" + Hierarchy + ";canvas=" + CanvasIdentity + ";mode=" + RenderMode +
                ";sort=" + SortingOrder + ";override=" + OverrideSorting + ";anchors=" +
                AnchorMin + ".." + AnchorMax + ";pivot=" + Pivot + ";sizeDelta=" + SizeDelta +
                ";rect=" + RectSize + ";screen=" + ScreenWidth + "x" + ScreenHeight +
                ";coverage=" + Coverage.ToString("F4") + ";alpha=" + Alpha +
                ";interactable=" + Interactable + ";blocks=" + BlocksRaycasts +
                ";backgroundRaycast=" + BackgroundRaycastTarget + ";backgroundAlpha=" +
                BackgroundOpacity + ";raycaster=" + RaycasterIdentity + ";eventSystem=" +
                EventSystemIdentity + ";ownsCenter=" + OwnsCenterRaycast + ";top=" + TopRaycast;
        }
    }
}
