using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.UI;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // The casting-first workspace as the addendum v1.1 casting graph: a
    // narrow buff catalogue; for the selected buff, the casters with their
    // exact sources and what the whole plan leaves of each, one ink line and
    // one chip per planned casting, and the targets; the selected casting's
    // inspector; the two labelled budgets and the actions in the footer.
    //
    // The view renders CastingWorkspaceSession.BuildGraph read models and
    // sends session commands. It keeps no planner state of its own: no
    // counter, no targeting or budget rule, no persisted geometry. Lines and
    // chips are recomputed only on a refresh (a command, a selection, a
    // resize), never per frame.
    internal sealed class CastingWorkspaceScreenView : IDisposable
    {
        internal const string RootName = "KingmakerBuffPlanner.CastingWorkspaceRoot";
        // Mode identity (addendum 7): this view is the casting-first planner.
        internal const string ModeLabel = "Planner: Casting-first";

        private static readonly Color Ink = new Color(0.20f, 0.14f, 0.10f, 0.85f);
        private static readonly Color InkFaint = new Color(0.20f, 0.14f, 0.10f, 0.35f);
        // The inks the contrast tests measure on the paper (WP7).
        private static readonly Color Burgundy = KingmakerUiFactory.ToColor(PlannerParchmentPalette.Burgundy);
        private static readonly Color BlockedInk = KingmakerUiFactory.ToColor(PlannerParchmentPalette.BlockedInk, 0.95f);
        private static readonly Color LegalInk = KingmakerUiFactory.ToColor(PlannerParchmentPalette.LegalInk, 0.95f);
        private const float LineThickness = 2f;
        private const float SelectedLineThickness = 4f;
        private const float CorridorThickness = 24f;

        private readonly CastingWorkspaceSession _session;
        private readonly Func<CastingWorkspaceInputs> _inputs;
        private readonly Func<CastingWorkspaceInputs> _freshInputs;
        private readonly Action _close;
        private readonly Action _switchToClassic;
        private PlannerUiTheme _theme;
        private PlannerNativeThemeSurface _nativeTheme;
        private RectTransform _root;
        private RectTransform _frame;
        // WP7: the frame's native scroll paper (exact fallback: the flat
        // tint and outline above).
        private ParchmentSurface _frameParchment;
        private int _uiLayer;
        private bool _disposed;
        private bool _importAnnounced;
        private float _reloadArmedUntil;
        private CastingGraphView _lastView;
        private GraphLayoutResult _lastLayout;
        private GraphLayoutMetrics _lastMetrics;

        // Header.
        private Text _title;
        private Text _headerStatus;
        private RectTransform _routineBar;
        // Catalogue.
        private InputField _buffSearch;
        private RectTransform _catalogueContent;
        private string _buffQuery = string.Empty;
        private PlannerSourceCategory _sourceCategory = PlannerSourceCategory.All;
        private readonly Dictionary<PlannerSourceCategory, Button> _categoryTabs =
            new Dictionary<PlannerSourceCategory, Button>();
        // Graph.
        private Image _bannerIcon;
        private Text _bannerTitle;
        private Text _bannerDetail;
        private Text _guidance;
        private ScrollRect _graphScroll;
        private ParchmentRule _headerRule;
        private RectTransform _graphViewport;
        private RectTransform _graphContent;
        // Inspector.
        private RectTransform _inspectorContent;
        private Text _inspectorTitle;
        private ScrollRect _inspectorScroll;
        private bool _showedProblems;
        private Vector2 _laidOutViewportSize;
        internal string ProblemRevealCastingIdForRuntime { get; private set; }
        internal bool ProblemChipVisibleBeforeRevealForRuntime { get; private set; }
        internal float ProblemGraphScrollBeforeRevealForRuntime { get; private set; }
        // Footer.
        private Text _footerSelectedRun;
        private Text _footerOnePass;
        private Text _footerResult;
        private Text _footerSave;
        private Image _footerLedger;
        private Button _footerRecovery;
        private string _shownSaveState;
        private Button _modeButton;
        private Button _readyOnlyButton;
        private Button _undoButton;
        private Button _runButton;
        // Right-click spell description panel (everyday-use v1.2): a
        // read-only, scrollable presentation of the exact concrete
        // variant's native localized description. It never authors,
        // targets, casts or persists anything. WP7 draws it as a spell
        // scroll (title / meta / rule / body on the native paper) whose
        // input policy is SpellScrollModalState.
        private readonly SpellScrollModalState _inspectState = new SpellScrollModalState();
        private RectTransform _inspectRoot;
        private Text _inspectTitle;
        private Text _inspectMeta;
        private Text _inspectBody;
        private ScrollRect _inspectScroll;
        private RectTransform _inspectPanel;
        private ParchmentSurface _inspectParchment;
        private ParchmentRule _inspectRule;

        internal CastingWorkspaceScreenView(
            StaticCanvas nativeCanvas,
            CastingWorkspaceSession session,
            Func<CastingWorkspaceInputs> inputsProvider,
            Func<CastingWorkspaceInputs> freshInputsProvider,
            Action close,
            Action switchToClassic = null)
        {
            if (nativeCanvas == null)
                throw new ArgumentNullException("nativeCanvas");
            _session = session ?? throw new ArgumentNullException("session");
            _inputs = inputsProvider ?? throw new ArgumentNullException("inputsProvider");
            _freshInputs = freshInputsProvider ??
                throw new ArgumentNullException("freshInputsProvider");
            _close = close ?? throw new ArgumentNullException("close");
            _switchToClassic = switchToClassic;
            _theme = PlannerUiTheme.Resolve(nativeCanvas);
            Build(nativeCanvas);
        }

        internal GameObject RootObject
        {
            get { return _root == null ? null : _root.gameObject; }
        }

        // Everyday-use v1.2's one continuous scroll (the two-page book art
        // stays retired), now drawn on the native paper when its donor
        // validates: which donors were borrowed and what every parchment
        // surface and rule drew (WP7).
        internal string PageArtEvidence
        {
            get { return "page=continuous-scroll;book-art-retired;" + WorkspacePaperEvidenceForRuntime; }
        }

        internal string WorkspacePaperEvidenceForRuntime
        {
            get { return _nativeTheme == null ? "parchment=unavailable" : _nativeTheme.ParchmentEvidence; }
        }

        // The last rendered read model and geometry (runtime evidence only).
        internal CastingGraphView LastGraphForRuntime { get { return _lastView; } }
        internal GraphLayoutResult LastLayoutForRuntime { get { return _lastLayout; } }
        internal GraphLayoutMetrics LastMetricsForRuntime { get { return _lastMetrics; } }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            _root = null;
        }

        // Escape leaves the focused casting (its inspector) first; returns
        // false when there was nothing to leave, so the caller closes.
        internal bool HandleEscape()
        {
            if (_disposed) return false;
            // The description panel is the innermost surface: Escape closes
            // it first, leaving the planner exactly as it was.
            if (_inspectState.Handle(SpellScrollInput.Escape) == SpellScrollOutcome.Closed)
            {
                SyncSpellInspect();
                return true;
            }
            if (_session.EditingFocusCastingId == null) return false;
            _session.ClearGraphFocus();
            RefreshView();
            return true;
        }

        internal void ShowNotice(string text)
        {
            if (_footerResult != null && !string.IsNullOrEmpty(text)) _footerResult.text = text;
        }

        // One refresh renders the shared read models and, being an actual
        // presentation on screen, feeds the review coordinator.
        // The header rule sits in the gap above the routine bar when there is
        // one (ParchmentHeaderRule); re-placed on every refresh because the
        // frame's height is only known once the canvas has laid it out.
        private void PlaceHeaderRule()
        {
            if (_headerRule == null || _frame == null) return;
            float height = _frame.rect.height;
            if (height <= 0f) height = Screen.height - 84f;
            float? offset = ParchmentHeaderRule.OffsetBelowTop(height);
            _headerRule.Rect.gameObject.SetActive(offset.HasValue);
            if (offset.HasValue) _headerRule.Rect.anchoredPosition = new Vector2(0f, -offset.Value);
            _headerRule.Placement = offset.HasValue
                ? "below-header:" + offset.Value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                : "hidden:no-room-above-routines";
        }

        internal void RefreshView()
        {
            if (_disposed) return;
            PlaceHeaderRule();
            float cataloguePosition = CatalogueScroll() == null ? 1f
                : CatalogueScroll().verticalNormalizedPosition;
            float inspectorPosition = _inspectorScroll == null ? 1f
                : _inspectorScroll.verticalNormalizedPosition;
            CastingWorkspaceInputs inputs = _inputs();
            CastingGraphView view = _session.BuildGraph(inputs);
            _session.PresentForReview(inputs);
            _lastView = view;
            if (!_importAnnounced && _footerResult != null)
            {
                _importAnnounced = true;
                string import = PersistenceMessages.ForCastingLoad(_session.LoadStatus,
                    _session.PrimaryPlanFileExists, _session.LoadSourcePath, _session.LoadWarning) ??
                    DescribeImport(_session);
                if (import != null) _footerResult.text = import;
            }
            RebuildHeader(view);
            RebuildCatalogue(view);
            RebuildBanner(view);
            RebuildGraph(view);
            RebuildInspector(view);
            RebuildFooter(view);
            // Borrowed native artwork, fonts and the click sound reach the
            // rebuilt controls; every control is pointer-highlighted only.
            if (_nativeTheme != null) _nativeTheme.ApplyTo(_root);
            KingmakerUiFactory.OwnPointerHighlights(_root);
            PropagateUiLayer();
            Canvas.ForceUpdateCanvases();
            if (CatalogueScroll() != null) CatalogueScroll().verticalNormalizedPosition = cataloguePosition;
            if (_inspectorScroll != null) _inspectorScroll.verticalNormalizedPosition = inspectorPosition;
            RevealPendingProblem();
        }

        // Geometry-only resize handling: no additional compiler or readiness
        // path. A size change may re-arm one reveal; ordinary ticks do not.
        internal void TickGeometry()
        {
            if (_disposed || _lastView == null || _graphViewport == null) return;
            if ((_graphViewport.rect.size - _laidOutViewportSize).sqrMagnitude > 1f)
            {
                RebuildGraph(_lastView);
                if (_session.ProblemNavigation.Active) _session.ProblemNavigation.RequestReveal();
            }
            RevealPendingProblem();
        }

        private void RevealPendingProblem()
        {
            string id = _session.ProblemNavigation.PendingRevealCastingId;
            if (id == null || _lastView == null ||
                !string.Equals(id, _lastView.FocusedCastingId, StringComparison.Ordinal)) return;
            Canvas.ForceUpdateCanvases();
            RectTransform chip = FindGraphPart("Casting." + id);
            RectTransform tile = _catalogueContent == null ? null
                : _catalogueContent.Find("Source." + _lastView.SelectedSourceId) as RectTransform;
            // Wait for every required rectangle before moving any scroll.
            // A partial layout must not repeatedly reclaim graph scrolling.
            if (!CanRevealInScroll(_graphScroll, chip) || !CanRevealInScroll(CatalogueScroll(), tile) ||
                _inspectorScroll == null || _inspectorScroll.viewport == null ||
                _inspectorScroll.viewport.rect.height <= 0) return;
            if (!string.Equals(ProblemRevealCastingIdForRuntime, id, StringComparison.Ordinal))
            {
                ProblemRevealCastingIdForRuntime = id;
                ProblemChipVisibleBeforeRevealForRuntime = ScreenCentre(chip) != null;
                ProblemGraphScrollBeforeRevealForRuntime = _graphScroll.verticalNormalizedPosition;
            }
            RevealInScroll(_graphScroll, chip);
            RevealInScroll(CatalogueScroll(), tile);
            _inspectorScroll.StopMovement();
            _inspectorScroll.verticalNormalizedPosition = 1f;
            _session.ProblemNavigation.CompleteReveal(id);
        }

        private static bool CanRevealInScroll(ScrollRect scroll, RectTransform target)
        {
            return scroll != null && target != null && scroll.content != null && scroll.viewport != null &&
                scroll.viewport.rect.height > 0;
        }

        private static void RevealInScroll(ScrollRect scroll, RectTransform target)
        {
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, target);
            float offset = CastingProblemScrollReveal.VerticalOffset(scroll.viewport.rect.yMin,
                scroll.viewport.rect.yMax, bounds.min.y, bounds.max.y, 12f);
            float overflow = scroll.content.rect.height - scroll.viewport.rect.height;
            scroll.StopMovement();
            if (overflow > 0)
                scroll.verticalNormalizedPosition = Mathf.Clamp01(
                    scroll.verticalNormalizedPosition - offset / overflow);
        }

        // ------------------------------------------------------------------
        // Runtime evidence seams (read-only)
        // ------------------------------------------------------------------

        private static RectTransform RectOf(Component component)
        {
            return (RectTransform)component.transform;
        }

        // Where a named part of the view is on screen, in Unity screen pixels
        // (origin bottom left); null when absent, inactive or off screen.
        // Parts: "search", "buff-grid" (the catalogue), "tile:<sourceId>",
        // "caster:<unitId>", "source:<unitId>:<index>", "target:<unitId>",
        // "chip:<castingId>", "line:<castingId>" (the middle of the inbound
        // segment's hit corridor).
        internal Vector2? ScreenPointForRuntime(string part)
        {
            RectTransform rect = null;
            if (part == "graph") rect = _graphScroll == null ? null : RectOf(_graphScroll);
            else if (part == "search") rect = _buffSearch == null ? null : RectOf(_buffSearch);
            else if (part == "buff-grid")
            {
                ScrollRect scroll = CatalogueScroll();
                rect = scroll == null ? null : RectOf(scroll);
            }
            else if (part != null && part.StartsWith("tile:", StringComparison.Ordinal) && _catalogueContent != null)
                rect = _catalogueContent.Find("Source." + part.Substring(5)) as RectTransform;
            else if (part != null && part.StartsWith("routine:", StringComparison.Ordinal) && _routineBar != null)
                rect = _routineBar.Find("Routine." + part.Substring(8)) as RectTransform;
            else if (part == "problem-next" && _inspectorContent != null)
                rect = _inspectorContent.Find("ProblemNext") as RectTransform;
            else if (part == "problem-previous" && _inspectorContent != null)
                rect = _inspectorContent.Find("ProblemPrevious") as RectTransform;
            else if (part == "undo")
                rect = _undoButton == null ? null : _undoButton.transform as RectTransform;
            else if (part != null && _graphContent != null)
            {
                if (part.StartsWith("caster:", StringComparison.Ordinal))
                    rect = FindGraphPart("Caster." + part.Substring(7));
                else if (part.StartsWith("source:", StringComparison.Ordinal))
                    rect = FindGraphPart("Provider." + part.Substring(7).Replace(':', '.'));
                else if (part.StartsWith("target:", StringComparison.Ordinal))
                    rect = FindGraphPart("Target." + part.Substring(7));
                else if (part.StartsWith("chip:", StringComparison.Ordinal))
                    rect = FindGraphPart("Casting." + part.Substring(5));
                else if (part.StartsWith("line:", StringComparison.Ordinal))
                    rect = FindGraphPart("Line." + part.Substring(5) + ".in");
            }
            return ScreenCentre(rect);
        }

        private RectTransform FindGraphPart(string name)
        {
            foreach (RectTransform rect in _graphContent.GetComponentsInChildren<RectTransform>(false))
                if (rect != null && string.Equals(rect.name, name, StringComparison.Ordinal)) return rect;
            return null;
        }

        private static Vector2? ScreenCentre(RectTransform rect)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy) return null;
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : canvas.worldCamera;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 centre = RectTransformUtility.WorldToScreenPoint(camera, (corners[0] + corners[2]) * 0.5f);
            if (centre.x < 1f || centre.y < 1f || centre.x > Screen.width - 1 || centre.y > Screen.height - 1)
                return null;
            // A screen point behind a viewport mask is not visible evidence.
            foreach (Mask mask in rect.GetComponentsInParent<Mask>())
                if (mask.enabled && !RectTransformUtility.RectangleContainsScreenPoint(
                        RectOf(mask), centre, camera)) return null;
            foreach (RectMask2D mask in rect.GetComponentsInParent<RectMask2D>())
                if (mask.enabled && !RectTransformUtility.RectangleContainsScreenPoint(
                        RectOf(mask), centre, camera)) return null;
            return centre;
        }

        internal float ProblemChipVisibleFractionForRuntime(string castingId)
        {
            RectTransform chip = FindGraphPart("Casting." + castingId);
            if (chip == null || _graphViewport == null) return 0f;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_graphViewport, chip);
            Rect viewport = _graphViewport.rect;
            float width = Mathf.Max(0f, Mathf.Min(viewport.xMax, bounds.max.x) - Mathf.Max(viewport.xMin, bounds.min.x));
            float height = Mathf.Max(0f, Mathf.Min(viewport.yMax, bounds.max.y) - Mathf.Max(viewport.yMin, bounds.min.y));
            return bounds.size.x <= 0 || bounds.size.y <= 0 ? 0f : width * height / (bounds.size.x * bounds.size.y);
        }

        internal bool? ProblemControlEnabledForRuntime(bool next)
        {
            Transform control = _inspectorContent == null ? null
                : _inspectorContent.Find(next ? "ProblemNext" : "ProblemPrevious");
            Button button = control == null ? null : control.GetComponent<Button>();
            return button == null ? (bool?)null : button.interactable;
        }

        internal string VisibleInspectorTextForRuntime
        {
            get
            {
                return _inspectorContent == null ? string.Empty : string.Join("\n",
                    _inspectorContent.GetComponentsInChildren<Text>()
                        .Where(text => ScreenCentre(text.rectTransform) != null)
                        .Select(text => text.text).ToArray());
            }
        }

        internal float InspectorScrollPositionForRuntime
        {
            get { return _inspectorScroll == null ? -1f : _inspectorScroll.verticalNormalizedPosition; }
        }

        internal string FooterResultForRuntime
        {
            get { return _footerResult == null ? string.Empty : _footerResult.text; }
        }

        private ScrollRect CatalogueScroll()
        {
            return _catalogueContent == null ? null : _catalogueContent.GetComponentInParent<ScrollRect>();
        }

        internal string SearchTextForRuntime
        {
            get { return _buffSearch == null ? null : _buffSearch.text; }
        }

        internal bool SearchFocusedForRuntime
        {
            get { return _buffSearch != null && _buffSearch.isFocused; }
        }

        // "<sourceId>|<label>|<selected>" for every catalogue entry shown now.
        internal IList<string> VisibleSourcesForRuntime()
        {
            var tiles = new List<string>();
            if (_catalogueContent == null) return tiles;
            foreach (Transform child in _catalogueContent)
            {
                if (child == null || !child.name.StartsWith("Source.", StringComparison.Ordinal)) continue;
                Transform label = child.Find("Name");
                Text text = label == null ? null : label.GetComponent<Text>();
                tiles.Add(child.name.Substring("Source.".Length) + "|" +
                    (text == null ? string.Empty : text.text) + "|" + (child.Find("Selected") != null));
            }
            return tiles;
        }

        internal float? BuffGridScrollForRuntime
        {
            get
            {
                ScrollRect scroll = CatalogueScroll();
                return scroll == null ? (float?)null : scroll.verticalNormalizedPosition;
            }
        }

        internal bool BuffGridOverflowsForRuntime
        {
            get
            {
                ScrollRect scroll = CatalogueScroll();
                if (scroll == null || scroll.content == null) return false;
                RectTransform viewport = scroll.viewport != null ? scroll.viewport : RectOf(scroll);
                return scroll.content.rect.height > viewport.rect.height + 1f;
            }
        }

        // The continuous scroll's own position (the graph area), for the
        // physical wheel judgement; null when the scroll is absent.
        internal float? GraphScrollPositionForRuntime
        {
            get
            {
                return _graphScroll == null || _graphScroll.viewport == null
                    ? (float?)null : _graphScroll.verticalNormalizedPosition;
            }
        }

        // True when the graph content is taller than the viewport (the
        // wheel has something to scroll).
        internal bool GraphScrollOverflowForRuntime
        {
            get
            {
                return _graphScroll != null && _graphScroll.content != null &&
                    _graphScroll.viewport != null &&
                    _graphScroll.content.rect.height > _graphScroll.viewport.rect.height + 1f;
            }
        }

        // The right-click inspect target for the physical scenario: the
        // focused casting's chip when it is on screen, else the first
        // casting chip on screen.
        internal string InspectTargetPartForRuntime
        {
            get
            {
                var candidates = new System.Collections.Generic.List<string>();
                if (_lastView != null && !string.IsNullOrEmpty(_lastView.FocusedCastingId))
                    candidates.Add("chip:" + _lastView.FocusedCastingId);
                if (_lastView != null)
                    foreach (var casting in _lastView.Castings)
                        candidates.Add("chip:" + casting.CastingId);
                foreach (string candidate in candidates)
                    if (ScreenPointForRuntime(candidate) != null) return candidate;
                return null;
            }
        }

        // How many castings the last built graph view holds (diagnostic for
        // the physical scenario: distinguishes an empty plan from a chip
        // that is merely off screen).
        internal int GraphCastingCountForRuntime
        {
            get { return _lastView == null ? 0 : _lastView.Castings.Count; }
        }

        internal string SelectedSourceIdForRuntime
        {
            get { return _lastView == null ? null : _lastView.SelectedSourceId; }
        }

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        private void Build(StaticCanvas nativeCanvas)
        {
            // Top-level canvas in the game's own service-window pattern
            // (ScreenSpaceCamera bound to the native UI camera): a canvas
            // nested under StaticCanvas rendered in no path in earlier runs.
            _root = KingmakerUiFactory.CreateRect(RootName, null);
            Canvas nativeRootCanvas = nativeCanvas.GetComponent<Canvas>();
            Canvas canvas = _root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = nativeRootCanvas == null ? null : nativeRootCanvas.worldCamera;
            canvas.planeDistance = nativeRootCanvas == null ? 100f : nativeRootCanvas.planeDistance;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32000;
            _root.gameObject.AddComponent<GraphicRaycaster>();
            CanvasGroup group = _root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            KingmakerUiFactory.Stretch(_root);
            _nativeTheme = PlannerNativeThemeSurface.Attach(_root, nativeCanvas);
            // The native UI camera culls by layer: every owned object carries
            // the native canvas's layer (factory objects default to 0).
            _uiLayer = nativeCanvas.gameObject.layer;
            _root.gameObject.layer = _uiLayer;
            RectTransform blocker = KingmakerUiFactory.CreateRect("Blocker", _root);
            KingmakerUiFactory.AddPanel(blocker, new Color(0f, 0f, 0f, 0.55f));
            KingmakerUiFactory.Stretch(blocker);
            _frame = KingmakerUiFactory.CreateRect("Frame", _root);
            KingmakerUiFactory.AddFramedPanel(_frame, _theme.ParchmentPanel, _theme.GoldAccent, 2f);
            KingmakerUiFactory.Stretch(_frame, 24, 24, 24, 60);
            // Everyday-use v1.2: one continuous scroll. The borrowed
            // two-page book sprite (with its central binding under the
            // graph connections) stays retired. WP7: the continuous
            // writing surface is the native scroll paper, one sheet with no
            // central fold, drawn under every lane; the flat tint and
            // outline above remain its exact fallback.
            _frameParchment = ParchmentSurface.Create(ParchmentSurfaces.WorkspaceFrame, _frame,
                ParchmentSurfaces.WorkspaceFrameOutsets);
            BuildHeader(_frame);
            BuildCatalogue(_frame);
            BuildGraphArea(_frame);
            BuildInspector(_frame);
            BuildFooter(_frame);
            ApplyScrollPaper();
            PropagateUiLayer();
        }

        // Historical donors of the retired two-page book art, retained for
        // the evidence string only (everyday-use v1.2 removed their use:
        // the binding sat under the casting connections).
        internal static readonly string[] PageArtLocators =
        {
            "ServiceWindow/SpellBook/BookBackground",
            "ServiceWindow/CharacterScreen/BookBackground"
        };

        // The frame's paper and its scroll rules. The lanes' flat wells and
        // the footer ledger become light washes on the paper so the sheet
        // shows through (a translucent wash drops its Outline, which would
        // otherwise fill it - ParchmentSurfaces.WashKeepsOutline); the rules under the
        // header and above the footer close the scroll's body (hidden
        // without the paper, so a failed theme is exactly the flat look).
        // Nothing here reads or mutates a donor: the theme surface hands the
        // validated ScrollPaper/ScrollRule donors to these owned layers.
        private void ApplyScrollPaper()
        {
            foreach (ScrollRect lane in new[] { CatalogueScroll(), _graphScroll, _inspectorScroll })
                if (lane != null)
                    _frameParchment.AddWash(lane.GetComponent<Image>(), PlannerParchmentPalette.WellWashAlpha);
            _frameParchment.AddWash(_footerLedger, PlannerParchmentPalette.LedgerWashAlpha);
            // Between the header row (its controls end 41 units down) and
            // the routine bar; and in the gap between the lanes (8.5% of the
            // frame up) and the footer (7.8%).
            ParchmentRule header = ParchmentRule.Create("HeaderRule", _frame, _frameParchment, false);
            KingmakerUiFactory.SetAnchors(header.Rect, 0.028f, 1f, 0.972f, 1f);
            _headerRule = header;
            PlaceHeaderRule();
            ParchmentRule footer = ParchmentRule.Create("FooterRule", _frame, _frameParchment, false);
            KingmakerUiFactory.SetAnchors(footer.Rect, 0.028f, 0.0815f, 0.972f, 0.0815f);
            // The rules are part of the sheet: drawn right above the paper
            // and beneath every control, so a crowded small screen covers
            // them instead of striking through its routine tabs.
            header.Rect.SetSiblingIndex(1);
            footer.Rect.SetSiblingIndex(2);
            _nativeTheme.RegisterParchment(_frameParchment);
            _nativeTheme.RegisterRule(header);
            _nativeTheme.RegisterRule(footer);
        }

        // ------------------------------------------------------------------
        // Right-click spell description (read-only)
        // ------------------------------------------------------------------

        // Fires only for the right mouse button, leaving left-click
        // authoring semantics untouched on the same control.
        internal sealed class RightClickProxy : MonoBehaviour, IPointerClickHandler
        {
            internal Action OnRightClick;
            public void OnPointerClick(PointerEventData eventData)
            {
                if (eventData.button == PointerEventData.InputButton.Right && OnRightClick != null)
                    OnRightClick();
            }
        }

        internal bool SpellInspectOpen
        {
            get { return _inspectState.IsOpen && _inspectRoot != null && _inspectRoot.gameObject.activeSelf; }
        }

        internal void ShowSpellInspect(string title, string description, string durationText,
            bool exactVariant)
        {
            if (_disposed || _frame == null) return;
            if (_inspectRoot == null) BuildSpellInspect();
            _inspectState.Open(SpellScrollContent.Compose(title, description, durationText, exactVariant));
            SpellScrollContent content = _inspectState.Content;
            _inspectTitle.text = content.Title;
            _inspectMeta.text = content.Meta;
            // The exact native localized description, read-only (a Text,
            // never an input), unaltered.
            _inspectBody.text = content.Body;
            SyncSpellInspect();
            if (_inspectScroll != null)
            {
                _inspectScroll.StopMovement();
                _inspectScroll.verticalNormalizedPosition = 1f;
            }
            PropagateUiLayer();
        }

        internal void CloseSpellInspect()
        {
            _inspectState.Close();
            SyncSpellInspect();
        }

        // The scroll's GameObject shows exactly the policy's open state.
        private void SyncSpellInspect()
        {
            if (_inspectRoot != null && _inspectRoot.gameObject.activeSelf != _inspectState.IsOpen)
                _inspectRoot.gameObject.SetActive(_inspectState.IsOpen);
        }

        private void SpellInspectInput(SpellScrollInput input)
        {
            if (_inspectState.Handle(input) == SpellScrollOutcome.Closed) SyncSpellInspect();
        }

        private void BuildSpellInspect()
        {
            _inspectRoot = KingmakerUiFactory.CreateRect("SpellInspect", _root);
            KingmakerUiFactory.Stretch(_inspectRoot);
            // Outside the scroll there is only the dimmed backdrop: it takes
            // every press, click and wheel there (a click closes the
            // description, SpellScrollModalState), so nothing falls through
            // to the graph or the game. The scroll is the backdrop's sibling,
            // so its own clicks never bubble to it.
            RectTransform backdrop = KingmakerUiFactory.CreateRect("Backdrop", _inspectRoot);
            KingmakerUiFactory.AddPanel(backdrop, new Color(0f, 0f, 0f, 0.35f));
            KingmakerUiFactory.Stretch(backdrop);
            SpellScrollBackdrop sink = backdrop.gameObject.AddComponent<SpellScrollBackdrop>();
            sink.OutsideClick = () => SpellInspectInput(SpellScrollInput.ClickOutsideScroll);
            sink.Wheel = () => SpellInspectInput(SpellScrollInput.WheelElsewhere);
            // A fixed-size panel centred on the dimmed overlay. SetAnchors'
            // offsets are edge insets, not a size: the earlier point anchor
            // with insets 760/520 gave the panel a negative width and no
            // height, so the description never rendered (D11, visible in
            // beta-f0cf4f16r11-phys-sel-01 and beta-3c1c5d4ar13-phys-sel-01
            // physical-cf-inspect.png); the Close button had the same fault.
            RectTransform panel = KingmakerUiFactory.CreateRect("Panel", _inspectRoot);
            KingmakerUiFactory.AddFramedPanel(panel, _theme.ParchmentPanel, _theme.GoldAccent, 2f);
            CentreFixed(panel, new Vector2(0.5f, 0.5f),
                new Vector2(SpellScrollLayout.PanelWidth, SpellScrollLayout.PanelHeight), Vector2.zero);
            _inspectPanel = panel;
            // WP7: the spell scroll - the native paper behind a centred
            // title, the meta line, a scroll rule and the scrolling body.
            _inspectParchment = ParchmentSurface.Create(ParchmentSurfaces.SpellScroll, panel,
                ParchmentSurfaces.SpellScrollOutsets);
            SpellScrollLayout layout = SpellScrollLayout.Compute(SpellScrollLayout.PanelWidth,
                SpellScrollLayout.PanelHeight);
            _inspectTitle = KingmakerUiFactory.CreateText("Title", panel, _theme, string.Empty,
                SpellScrollLayout.TitleFontSize, TextAnchor.MiddleCenter);
            _inspectTitle.fontStyle = FontStyle.Bold;
            _inspectTitle.color = KingmakerUiFactory.ToColor(PlannerParchmentPalette.HeadingInk);
            _inspectTitle.resizeTextForBestFit = true;
            _inspectTitle.resizeTextMinSize = SpellScrollLayout.TitleMinimumFontSize;
            _inspectTitle.resizeTextMaxSize = SpellScrollLayout.TitleFontSize;
            Place(_inspectTitle.rectTransform, layout.Title.X, layout.Title.Y, layout.Title.Width,
                layout.Title.Height);
            _inspectMeta = KingmakerUiFactory.CreateText("Meta", panel, _theme, string.Empty,
                SpellScrollLayout.MetaFontSize, TextAnchor.MiddleCenter);
            _inspectMeta.fontStyle = FontStyle.Italic;
            _inspectMeta.color = KingmakerUiFactory.ToColor(PlannerParchmentPalette.MetaInk);
            Place(_inspectMeta.rectTransform, layout.Meta.X, layout.Meta.Y, layout.Meta.Width, layout.Meta.Height);
            _inspectRule = ParchmentRule.Create("Rule", panel, _inspectParchment, true);
            RectTransform rule = _inspectRule.Rect;
            rule.anchorMin = new Vector2(0f, 1f);
            rule.anchorMax = new Vector2(0f, 1f);
            rule.pivot = new Vector2(0.5f, 0.5f);
            rule.sizeDelta = new Vector2(layout.Rule.Width, layout.Rule.Height);
            rule.anchoredPosition = new Vector2(layout.Rule.X + layout.Rule.Width / 2f,
                -(layout.Rule.Y + layout.Rule.Height / 2f));
            Button close = KingmakerUiFactory.CreateButton("Close", panel, _theme, "Close",
                CloseSpellInspect);
            Place(RectOf(close), layout.Close.X, layout.Close.Y, layout.Close.Width, layout.Close.Height);
            // The description scrolls inside the planner's standard scroll
            // view: the content's height follows the wrapped text through the
            // view's layout group, so long text can be wheeled through; the
            // bar appears only when the text overflows. The wheel over it
            // reaches this ScrollRect first and stops there: the planner's
            // scrolls beneath never see it.
            RectTransform content;
            _inspectScroll = KingmakerUiFactory.CreateScrollView("Description", panel, _theme, out content,
                SpellScrollLayout.BodyScrollbarWidth);
            _inspectScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            Place(RectOf(_inspectScroll), layout.Body.X, layout.Body.Y, layout.Body.Width, layout.Body.Height);
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _inspectBody = KingmakerUiFactory.CreateText("Body", content, _theme, string.Empty,
                SpellScrollLayout.BodyFontSize, TextAnchor.UpperLeft);
            _inspectBody.color = KingmakerUiFactory.ToColor(PlannerParchmentPalette.PrimaryInk);
            _inspectBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _inspectBody.verticalOverflow = VerticalWrapMode.Overflow;
            // On the paper the body sits straight on the sheet; the flat
            // well and its outline are the exact fallback.
            _inspectParchment.AddWash(RectOf(_inspectScroll).GetComponent<Image>(), 0f);
            if (_nativeTheme != null)
            {
                _nativeTheme.RegisterParchment(_inspectParchment);
                _nativeTheme.RegisterRule(_inspectRule);
                // Built after the last rebuild pass: borrow the native fonts,
                // button artwork and click sound now, not at the next refresh.
                _nativeTheme.ApplyTo(_inspectRoot);
            }
            _inspectRoot.gameObject.SetActive(false);
        }

        // What the open spell scroll drew and its input policy (runtime
        // evidence only).
        internal string SpellInspectPaperEvidenceForRuntime
        {
            get
            {
                if (_inspectParchment == null) return "spell-scroll=not-built";
                return _inspectParchment.Evidence + "|" + _inspectRule.Evidence + "|input=" +
                    SpellScrollModalState.OutsideClickPolicy;
            }
        }

        // Fixed size at an anchor point: the pivot is the anchor, so the
        // offset is measured from that point inward.
        private static void CentreFixed(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        // The open description's presentation, for the physical judgement:
        // the panel's on-screen size in pixels (zero when any corner is off
        // screen), its title and body text, how far the wrapped text
        // overflows the viewport, and the scroll position (1 = top).
        internal Vector2? SpellInspectPanelScreenSizeForRuntime
        {
            get
            {
                if (!SpellInspectOpen || _inspectPanel == null) return null;
                Canvas canvas = _inspectPanel.GetComponentInParent<Canvas>();
                Camera camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null : canvas.worldCamera;
                var corners = new Vector3[4];
                _inspectPanel.GetWorldCorners(corners);
                Vector2 low = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 high = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                if (low.x < 0f || low.y < 0f || high.x > Screen.width || high.y > Screen.height) return Vector2.zero;
                return high - low;
            }
        }

        internal Vector2? SpellInspectPanelCentreForRuntime
        {
            get { return SpellInspectOpen ? ScreenCentre(_inspectPanel) : null; }
        }

        internal string SpellInspectTitleForRuntime
        {
            get { return SpellInspectOpen && _inspectTitle != null ? _inspectTitle.text : null; }
        }

        internal string SpellInspectBodyForRuntime
        {
            get { return SpellInspectOpen && _inspectBody != null ? _inspectBody.text : null; }
        }

        internal float SpellInspectOverflowForRuntime
        {
            get
            {
                if (!SpellInspectOpen || _inspectScroll == null || _inspectScroll.content == null ||
                    _inspectScroll.viewport == null) return 0f;
                return _inspectScroll.content.rect.height - _inspectScroll.viewport.rect.height;
            }
        }

        internal float? SpellInspectScrollPositionForRuntime
        {
            get
            {
                return SpellInspectOpen && _inspectScroll != null
                    ? _inspectScroll.verticalNormalizedPosition : (float?)null;
            }
        }

        // Wires right-click on one control to show the exact spell's
        // native description.
        private void AddSpellInspect(Component control, System.Func<CastingGraphView> view,
            string spellTitle, string description, string durationText, bool exactVariant)
        {
            if (control == null) return;
            RightClickProxy proxy = control.GetComponent<RightClickProxy>();
            if (proxy == null) proxy = control.gameObject.AddComponent<RightClickProxy>();
            RightClickProxy captured = proxy;
            captured.OnRightClick = () =>
            {
                _lastView = view();
                ShowSpellInspect(spellTitle, description, durationText, exactVariant);
            };
        }

        private void PropagateUiLayer()
        {
            if (_root == null) return;
            foreach (Transform node in _root.GetComponentsInChildren<Transform>(true))
                node.gameObject.layer = _uiLayer;
        }

        private void BuildHeader(RectTransform frame)
        {
            // Row 1, on the continuous parchment: mode, title, status.
            RectTransform header = KingmakerUiFactory.CreateRect("Header", frame);
            KingmakerUiFactory.SetAnchors(header, 0f, 1f, 1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(0f, 48f);
            header.anchoredPosition = new Vector2(0f, 2f);
            Text mode = KingmakerUiFactory.CreateText("PlannerMode", header, _theme, ModeLabel, 16,
                TextAnchor.MiddleLeft);
            mode.fontStyle = FontStyle.Bold;
            // Legible on the parchment (the light ink was chosen for the
            // retired dark book margin and read as a faint smear, r11/r13).
            mode.color = _theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(mode.rectTransform, 0f, 0f, 0f, 1f);
            mode.rectTransform.pivot = new Vector2(0f, 0.5f);
            mode.rectTransform.sizeDelta = new Vector2(230f, 0f);
            mode.rectTransform.anchoredPosition = new Vector2(16f, 0f);
            // Everyday-use v1.2: the graph is the only normal authoring
            // route; the prominent in-planner Classic switch is retired.
            // Classic recovery is the retained importer, the byte-exact
            // archive of the prior plan and reinstalling the prior package.
            _title = KingmakerUiFactory.CreateText("Title", header, _theme, "Buff Planner — casting plan", 22,
                TextAnchor.MiddleLeft);
            _title.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.SetAnchors(_title.rectTransform, 0f, 0f, 0.62f, 1f, 480f, 0f, 0f, 0f);
            _headerStatus = KingmakerUiFactory.CreateText("Status", header, _theme, string.Empty, 15,
                TextAnchor.MiddleRight);
            KingmakerUiFactory.SetAnchors(_headerStatus.rectTransform, 0.55f, 0f, 1f, 1f, 0f, 16f, 0f, 0f);
            Button close = KingmakerUiFactory.CreateButton("Close", header, _theme, "Close", () => _close());
            RectTransform closeRect = RectOf(close);
            KingmakerUiFactory.SetAnchors(closeRect, 1f, 0f, 1f, 1f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(90f, -10f);
            closeRect.anchoredPosition = new Vector2(-8f, 0f);
            _headerStatus.rectTransform.offsetMax = new Vector2(-110f, 0f);
            // Row 2, on the book's top edge: the routines.
            _routineBar = KingmakerUiFactory.CreateRect("RoutineBar", frame);
            KingmakerUiFactory.SetAnchors(_routineBar, 0f, 0.900f, 1f, ParchmentHeaderRule.RoutineBarTopAnchor,
                52f, 52f, 0f, 0f);
        }

        private void BuildCatalogue(RectTransform frame)
        {
            RectTransform lane = KingmakerUiFactory.CreateRect("Catalogue", frame);
            KingmakerUiFactory.SetAnchors(lane, 0.028f, 0.085f, 0.163f, 0.895f);
            Text title = KingmakerUiFactory.CreateText("LaneTitle", lane, _theme, "Buffs", 16, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            title.color = Burgundy;
            KingmakerUiFactory.SetAnchors(title.rectTransform, 0f, 1f, 1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(0f, 22f);
            // Discoverability (addendum 6): the right-click description
            // gesture is stated, not left undocumented.
            Text hint = KingmakerUiFactory.CreateText("InspectHint", lane, _theme,
                "Right-click a spell for its description", 11, TextAnchor.LowerLeft);
            hint.color = _theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(hint.rectTransform, 0f, 0f, 1f, 0f);
            hint.rectTransform.pivot = new Vector2(0.5f, 0f);
            hint.rectTransform.sizeDelta = new Vector2(0f, 18f);
            _buffSearch = KingmakerUiFactory.CreateInputField("BuffSearch", lane, _theme, "Search buffs…");
            RectTransform search = RectOf(_buffSearch);
            KingmakerUiFactory.SetAnchors(search, 0f, 1f, 1f, 1f);
            search.pivot = new Vector2(0.5f, 1f);
            search.sizeDelta = new Vector2(0f, 28f);
            search.anchoredPosition = new Vector2(0f, -24f);
            foreach (Text text in _buffSearch.GetComponentsInChildren<Text>(true))
            {
                text.fontSize = 14;
                text.resizeTextMaxSize = 14;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                KingmakerUiFactory.Stretch(text.rectTransform, 8, 6, 1, 1);
            }
            _buffSearch.onValueChanged.AddListener(value =>
            {
                // View-only filter: never the session, draft or document.
                _buffQuery = value ?? string.Empty;
                if (_lastView != null)
                {
                    RebuildCatalogue(_lastView);
                    KingmakerUiFactory.OwnPointerHighlights(_catalogueContent);
                    PropagateUiLayer();
                }
            });
            PlannerSourceCategory[] categories =
            {
                PlannerSourceCategory.All, PlannerSourceCategory.Spells,
                PlannerSourceCategory.Abilities, PlannerSourceCategory.Other
            };
            for (int index = 0; index < categories.Length; index++)
            {
                PlannerSourceCategory captured = categories[index];
                Button tab = KingmakerUiFactory.CreateButton("SourceTab." + captured, lane, _theme,
                    captured.ToString(), () =>
                    {
                        _sourceCategory = captured;
                        if (_lastView != null)
                        {
                            RebuildCatalogue(_lastView);
                            KingmakerUiFactory.OwnPointerHighlights(_catalogueContent);
                            PropagateUiLayer();
                        }
                    });
                RectTransform rect = RectOf(tab);
                KingmakerUiFactory.SetAnchors(rect, index * 0.25f, 1f, (index + 1) * 0.25f, 1f, 1f, 1f, 0f, 0f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(-2f, 26f);
                rect.anchoredPosition = new Vector2(0f, -56f);
                foreach (Text text in tab.GetComponentsInChildren<Text>(true))
                {
                    text.fontSize = 13;
                    text.resizeTextMaxSize = 13;
                    text.resizeTextMinSize = 10;
                }
                _categoryTabs[captured] = tab;
            }
            ScrollRect scroll = KingmakerUiFactory.CreateScrollView("Scroll", lane, _theme,
                out _catalogueContent, 10f);
            KingmakerUiFactory.SetAnchors(RectOf(scroll), 0f, 0f, 1f, 1f, 0f, 0f, 0f, 88f);
            ContentSizeFitter fitter = _catalogueContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void BuildGraphArea(RectTransform frame)
        {
            RectTransform area = KingmakerUiFactory.CreateRect("Graph", frame);
            KingmakerUiFactory.SetAnchors(area, 0.172f, 0.085f, 0.742f, 0.895f);
            // Banner: the selected buff, prominently, and the next step.
            RectTransform banner = KingmakerUiFactory.CreateRect("Banner", area);
            KingmakerUiFactory.SetAnchors(banner, 0f, 1f, 1f, 1f);
            banner.pivot = new Vector2(0.5f, 1f);
            banner.sizeDelta = new Vector2(0f, 74f);
            RectTransform icon = KingmakerUiFactory.CreateRect("Icon", banner);
            KingmakerUiFactory.SetAnchors(icon, 0f, 1f, 0f, 1f);
            icon.pivot = new Vector2(0f, 1f);
            icon.sizeDelta = new Vector2(52f, 52f);
            icon.anchoredPosition = new Vector2(2f, -2f);
            _bannerIcon = icon.gameObject.AddComponent<Image>();
            _bannerIcon.preserveAspect = true;
            _bannerIcon.raycastTarget = false;
            _bannerTitle = KingmakerUiFactory.CreateText("BuffName", banner, _theme, string.Empty, 24,
                TextAnchor.UpperLeft);
            _bannerTitle.fontStyle = FontStyle.Bold;
            _bannerTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            _bannerTitle.resizeTextForBestFit = true;
            _bannerTitle.resizeTextMinSize = 16;
            _bannerTitle.resizeTextMaxSize = 24;
            KingmakerUiFactory.SetAnchors(_bannerTitle.rectTransform, 0f, 1f, 1f, 1f);
            _bannerTitle.rectTransform.pivot = new Vector2(0f, 1f);
            _bannerTitle.rectTransform.offsetMin = new Vector2(64f, -32f);
            _bannerTitle.rectTransform.offsetMax = new Vector2(-4f, 0f);
            _bannerDetail = KingmakerUiFactory.CreateText("BuffDetail", banner, _theme, string.Empty, 14,
                TextAnchor.UpperLeft);
            _bannerDetail.color = _theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(_bannerDetail.rectTransform, 0f, 1f, 1f, 1f);
            _bannerDetail.rectTransform.offsetMin = new Vector2(64f, -52f);
            _bannerDetail.rectTransform.offsetMax = new Vector2(-4f, -32f);
            _guidance = KingmakerUiFactory.CreateText("Guidance", banner, _theme, string.Empty, 15,
                TextAnchor.UpperLeft);
            _guidance.color = Burgundy;
            _guidance.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.SetAnchors(_guidance.rectTransform, 0f, 1f, 1f, 1f);
            _guidance.rectTransform.offsetMin = new Vector2(64f, -74f);
            _guidance.rectTransform.offsetMax = new Vector2(-4f, -52f);
            // Lane captions.
            RectTransform captions = KingmakerUiFactory.CreateRect("LaneCaptions", area);
            KingmakerUiFactory.SetAnchors(captions, 0f, 1f, 1f, 1f);
            captions.pivot = new Vector2(0.5f, 1f);
            captions.sizeDelta = new Vector2(0f, 22f);
            captions.anchoredPosition = new Vector2(0f, -78f);
            AddLaneCaption(captions, "CasterCaption", "Who casts it · from which source", 0f, 0.36f);
            AddLaneCaption(captions, "CastingCaption", "Castings · one line is one cast", 0.36f, 0.76f);
            AddLaneCaption(captions, "TargetCaption", "Who receives it", 0.76f, 1f);
            _graphScroll = KingmakerUiFactory.CreateScrollView("Scroll", area, _theme, out _graphContent, 10f);
            KingmakerUiFactory.SetAnchors(RectOf(_graphScroll), 0f, 0f, 1f, 1f, 0f, 0f, 0f, 102f);
            _graphViewport = _graphScroll.viewport;
            // Absolute placement: the graph lays itself out (no layout group),
            // so anchors and lines use the same computed geometry.
            UnityEngine.Object.DestroyImmediate(_graphContent.GetComponent<VerticalLayoutGroup>());
            _graphContent.anchorMin = new Vector2(0f, 1f);
            _graphContent.anchorMax = new Vector2(1f, 1f);
            _graphContent.pivot = new Vector2(0.5f, 1f);
        }

        private void AddLaneCaption(RectTransform parent, string name, string caption, float from, float to)
        {
            Text text = KingmakerUiFactory.CreateText(name, parent, _theme, caption, 14, TextAnchor.MiddleLeft);
            text.fontStyle = FontStyle.Bold;
            text.color = Burgundy;
            KingmakerUiFactory.SetAnchors(text.rectTransform, from, 0f, to, 1f, 6f, 4f, 0f, 0f);
        }

        private void BuildInspector(RectTransform frame)
        {
            RectTransform lane = KingmakerUiFactory.CreateRect("Inspector", frame);
            KingmakerUiFactory.SetAnchors(lane, 0.752f, 0.085f, 0.972f, 0.895f);
            _inspectorTitle = KingmakerUiFactory.CreateText("LaneTitle", lane, _theme, "Casting", 16,
                TextAnchor.MiddleLeft);
            _inspectorTitle.fontStyle = FontStyle.Bold;
            _inspectorTitle.color = Burgundy;
            KingmakerUiFactory.SetAnchors(_inspectorTitle.rectTransform, 0f, 1f, 1f, 1f);
            _inspectorTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            _inspectorTitle.rectTransform.sizeDelta = new Vector2(0f, 24f);
            ScrollRect scroll = KingmakerUiFactory.CreateScrollView("Scroll", lane, _theme,
                out _inspectorContent, 10f);
            _inspectorScroll = scroll;
            KingmakerUiFactory.SetAnchors(RectOf(scroll), 0f, 0f, 1f, 1f, 0f, 0f, 0f, 28f);
            ContentSizeFitter fitter = _inspectorContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            VerticalLayoutGroup layout = _inspectorContent.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 3f;
                layout.padding = new RectOffset(8, 8, 6, 10);
            }
        }

        private void BuildFooter(RectTransform frame)
        {
            RectTransform footer = KingmakerUiFactory.CreateRect("Footer", frame);
            KingmakerUiFactory.SetAnchors(footer, 0f, 0f, 1f, 0.078f, 52f, 44f, 6f, 0f);
            _footerSelectedRun = FooterLine(footer, "SelectedRunBudget", 0.67f, 1f);
            _footerOnePass = FooterLine(footer, "OnePassBudget", 0.34f, 0.67f);
            _footerResult = FooterLine(footer, "Result", 0f, 0.34f);
            _footerResult.color = Burgundy;
            // Everyday-use v1.2 §2/§4/§5: autosave replaced Save (a passive
            // status shows it, refreshed with the view), the Run click
            // authorizes the current revision (no Accept Plan / Review &
            // Apply ceremony), and Reload left everyday use: it appears -
            // with Retry save - only as the exceptional recovery action
            // when saving failed or is refused.
            string[] names = { "ExecutionMode", "Undo", "Run" };
            string[] captions = { ModeCaption(), "Undo",
                WorkspaceFooterText.RunCaption(_session.RoutineDisplayName(_session.SelectedRoutineId)) };
            Action[] actions =
            {
                ToggleModeCommand, UndoCommand,
                () => RunApply(CastingApplyMode.Ordinary)
            };
            float[] widths = { 150f, 90f, 150f };
            // The save column sits left of the three text lines (it used to
            // overlap the first one: beta-3c1c5d4ar13-reload-01 frames).
            const float SaveColumn = 104f;
            _footerSave = FooterLine(footer, "SaveStatus", 0.55f, 1f);
            _footerSave.alignment = TextAnchor.MiddleCenter;
            _footerSave.rectTransform.anchorMax = new Vector2(0f, 1f);
            _footerSave.rectTransform.offsetMin = new Vector2(0f, 0f);
            _footerSave.rectTransform.offsetMax = new Vector2(SaveColumn - 8f, 0f);
            _footerRecovery = KingmakerUiFactory.CreateButton("SaveRecovery", footer, _theme,
                WorkspaceFooterText.RetrySaveAction, () => Command(RecoveryCommand));
            KingmakerUiFactory.SetAnchors(RectOf(_footerRecovery), 0f, 0.06f, 0f, 0.52f);
            RectOf(_footerRecovery).offsetMax = new Vector2(SaveColumn - 8f, RectOf(_footerRecovery).offsetMax.y);
            _footerRecovery.gameObject.SetActive(false);
            float right = 0f;
            for (int index = names.Length - 1; index >= 0; index--)
            {
                Action action = actions[index];
                Button button = KingmakerUiFactory.CreateButton(names[index], footer, _theme, captions[index],
                    () => Command(action));
                RectTransform rect = RectOf(button);
                KingmakerUiFactory.SetAnchors(rect, 1f, 0.12f, 1f, 0.88f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.sizeDelta = new Vector2(widths[index], 0f);
                rect.anchoredPosition = new Vector2(-right, 0f);
                right += widths[index] + 8f;
                if (names[index] == "ExecutionMode") _modeButton = button;
                if (names[index] == "Undo") _undoButton = button;
                if (names[index] == "Run") _runButton = button;
            }
            _readyOnlyButton = KingmakerUiFactory.CreateButton("ReadyOnly", footer, _theme, "Ready Casts Only",
                () => Command(() => RunApply(CastingApplyMode.ReadyCastsOnly)));
            RectTransform ready = RectOf(_readyOnlyButton);
            KingmakerUiFactory.SetAnchors(ready, 1f, 0.12f, 1f, 0.88f);
            ready.pivot = new Vector2(1f, 0.5f);
            ready.sizeDelta = new Vector2(170f, 0f);
            ready.anchoredPosition = new Vector2(-right, 0f);
            _readyOnlyButton.gameObject.SetActive(false);
            // The text lines start after the save column and end where the
            // buttons begin.
            float textRight = right + 180f;
            foreach (Text line in new[] { _footerSelectedRun, _footerOnePass, _footerResult })
            {
                line.rectTransform.offsetMin = new Vector2(SaveColumn, 0f);
                line.rectTransform.offsetMax = new Vector2(-textRight, 0f);
            }
            // The budget lines sit on the book's dark lower edge (seen in the
            // 1920x1200 frame): dark ink gets its own parchment ground.
            RectTransform ledger = KingmakerUiFactory.CreateRect("FooterLedger", footer);
            KingmakerUiFactory.SetAnchors(ledger, 0f, 0f, 1f, 1f, -8f, textRight - 8f, -2f, -2f);
            Image ground = KingmakerUiFactory.AddFramedPanel(ledger, _theme.ParchmentRaised, _theme.GoldAccent);
            ground.raycastTarget = false;
            ledger.SetAsFirstSibling();
            _footerLedger = ground;
            _footerResult.text = DescribeReadiness();
            ShowSaveState();
        }

        // The passive save status and its exceptional recovery action,
        // refreshed with every view rebuild (v1.2 §4: never "Saved" before
        // the current revision is durable). When the state turns into a
        // problem, the explanation replaces the result line once.
        private void ShowSaveState()
        {
            if (_footerSave == null) return;
            string state = _session.FooterSaveState;
            string problem = WorkspaceFooterText.SaveProblem(state);
            _footerSave.text = WorkspaceFooterText.SaveStatus(state);
            _footerSave.color = problem == null ? _theme.MutedBrownText : BlockedInk;
            string action = WorkspaceFooterText.RecoveryAction(state);
            if (_footerRecovery != null)
            {
                _footerRecovery.gameObject.SetActive(action != null);
                if (action != null) KingmakerUiFactory.SetButtonLabel(_footerRecovery, action);
            }
            if (problem != null && !string.Equals(state, _shownSaveState, StringComparison.Ordinal) &&
                _footerResult != null)
                _footerResult.text = problem;
            _shownSaveState = state;
        }

        private void RecoveryCommand()
        {
            string action = WorkspaceFooterText.RecoveryAction(_session.FooterSaveState);
            if (action == WorkspaceFooterText.ReloadAction)
            {
                ReloadCommand();
                return;
            }
            if (action == WorkspaceFooterText.RetrySaveAction)
                _footerResult.text = _session.RetryFailedSave()
                    ? "Saved: your latest edit is on disk."
                    : WorkspaceFooterText.SaveProblem(_session.FooterSaveState);
        }

        private Text FooterLine(RectTransform footer, string name, float from, float to)
        {
            Text text = KingmakerUiFactory.CreateText(name, footer, _theme, string.Empty, 13, TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            // Long budget lists shrink to fit before anything is cut.
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = 13;
            KingmakerUiFactory.SetAnchors(text.rectTransform, 0f, from, 1f, to, 0f, 0f, 0f, 0f);
            return text;
        }

        // ------------------------------------------------------------------
        // Rebuilds (on refresh only)
        // ------------------------------------------------------------------

        private void RebuildHeader(CastingGraphView view)
        {
            _headerStatus.text = WorkspaceHeaderText.Describe(view.SelectedRoutineName,
                view.RoutineCastingCount, view.RoutineReadyCount,
                view.SelectedRoutineGate != null && view.SelectedRoutineGate.Allowed,
                view.SelectedRoutineGate == null ? 0 : view.SelectedRoutineGate.BlockingReasons.Count) +
                (!_session.SavesRefused ? string.Empty
                    : _session.LegacyImportBlocked ? " · not saved: the classic plan could not be imported"
                    : " · not saved: the plan file cannot be read");
            KingmakerUiFactory.DestroyChildren(_routineBar);
            Text label = KingmakerUiFactory.CreateText("RoutineLabel", _routineBar, _theme, "Routine:", 15,
                TextAnchor.MiddleLeft);
            label.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.SetAnchors(label.rectTransform, 0f, 0f, 0f, 1f);
            label.rectTransform.pivot = new Vector2(0f, 0.5f);
            label.rectTransform.sizeDelta = new Vector2(80f, 0f);
            float x = 84f;
            foreach (CastingGraphRoutineTab routine in view.Routines)
            {
                string captured = routine.RoutineId;
                Button tab = KingmakerUiFactory.CreateButton("Routine." + captured, _routineBar, _theme,
                    routine.Name + " (" + routine.CastingCount + ")", () => Command(() =>
                        _session.SelectRoutine(captured)));
                KingmakerUiFactory.ApplyPalette(tab, routine.Selected);
                RectTransform rect = RectOf(tab);
                KingmakerUiFactory.SetAnchors(rect, 0f, 0.08f, 0f, 0.92f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(150f, 0f);
                rect.anchoredPosition = new Vector2(x, 0f);
                x += 158f;
            }
        }

        private void RebuildCatalogue(CastingGraphView view)
        {
            foreach (KeyValuePair<PlannerSourceCategory, Button> pair in _categoryTabs)
                KingmakerUiFactory.ApplyPalette(pair.Value, pair.Key == _sourceCategory);
            KingmakerUiFactory.DestroyChildren(_catalogueContent);
            int shown = 0;
            foreach (CastingGraphCatalogueEntry entry in view.Catalogue)
            {
                // The selected buff stays listed even when filtered out.
                if (!entry.Selected &&
                    (!WorkspaceSourceLabels.Matches(entry.Source, _buffQuery) ||
                     !WorkspaceSourceLabels.MatchesCategory(entry.Source, _sourceCategory))) continue;
                shown++;
                string captured = entry.SourceId;
                RectTransform rect = KingmakerUiFactory.CreateRect("Source." + captured, _catalogueContent);
                KingmakerUiFactory.AddLayout(rect, 52f);
                Image background = KingmakerUiFactory.AddPanel(rect,
                    entry.Selected ? new Color(1f, 0.95f, 0.84f, 0.95f) : new Color(1f, 1f, 1f, 0f));
                Button button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                ApplyRowHover(button, entry.Selected);
                button.onClick.AddListener(() => Command(() => _session.SelectGraphBuff(captured, _inputs())));
                // Right-click reads the spell; it never selects, authors or
                // mutates anything (everyday-use v1.2 E05).
                AddSpellInspect(button, () => _session.BuildGraph(_freshInputs()),
                    entry.Label, entry.Source.Description, entry.Source.DurationText,
                    entry.Selected && view.SelectedSourceExact);
                if (entry.Selected)
                {
                    RectTransform rule = KingmakerUiFactory.CreateRect("Selected", rect);
                    KingmakerUiFactory.SetAnchors(rule, 0f, 0f, 0f, 1f);
                    rule.pivot = new Vector2(0f, 0.5f);
                    rule.sizeDelta = new Vector2(4f, 0f);
                    KingmakerUiFactory.AddPanel(rule, Burgundy).raycastTarget = false;
                }
                AddIcon(rect, entry.Source.IconAbility, 36f, 8f);
                Text name = KingmakerUiFactory.CreateText("Name", rect, _theme, entry.Label, 14, TextAnchor.UpperLeft);
                name.fontStyle = entry.Selected ? FontStyle.Bold : FontStyle.Normal;
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 11;
                name.resizeTextMaxSize = 14;
                KingmakerUiFactory.Stretch(name.rectTransform, 50, 4, 17, 3);
                Text count = KingmakerUiFactory.CreateText("Count", rect, _theme,
                    entry.RoutineCount + " in " + view.SelectedRoutineName +
                        (entry.PlanCount == entry.RoutineCount ? string.Empty : " · " + entry.PlanCount + " in plan"),
                    12, TextAnchor.LowerLeft);
                count.color = entry.RoutineCount == 0 ? _theme.MutedBrownText : _theme.GreenSuccess;
                KingmakerUiFactory.Stretch(count.rectTransform, 50, 4, 3, 34);
            }
            if (shown == 0)
            {
                Text none = KingmakerUiFactory.CreateText("NoMatch", _catalogueContent, _theme,
                    _buffQuery.Length == 0 ? "No buffs here." : "No buff matches \"" + _buffQuery + "\".",
                    13, TextAnchor.MiddleLeft);
                none.color = _theme.MutedBrownText;
                KingmakerUiFactory.AddLayout(none.rectTransform, 30f);
            }
        }

        private void RebuildBanner(CastingGraphView view)
        {
            Sprite icon = ResolveAbilityIcon(view.SelectedSourceIcon);
            _bannerIcon.sprite = icon;
            _bannerIcon.color = icon == null ? new Color(0f, 0f, 0f, 0f) : Color.white;
            _bannerTitle.text = view.SelectedSourceCaption;
            string kind = view.SelectedSourceIsGroup == true ? "Group buff: one casting reaches everyone in its area"
                : view.SelectedSourceIsGroup == false ? "Single target: one casting per recipient"
                : "Targeting not understood";
            _bannerDetail.text = kind + " · " + view.Castings.Count + " in " + view.SelectedRoutineName +
                (view.OtherRoutineCastings == 0 ? string.Empty
                    : " · " + view.OtherRoutineCastings + " in other routines (shown there)");
            _guidance.text = view.Guidance;
            // Right-click the selected-spell header: the exact variant's
            // description when a concrete row is selected, else base text.
            AddSpellInspect(_bannerIcon != null ? (Component)_bannerIcon : (Component)_bannerTitle,
                () => _session.BuildGraph(_freshInputs()), view.SelectedSourceCaption,
                view.SelectedSourceDescription, view.SelectedSourceDurationText,
                view.SelectedSourceExact);
        }

        private GraphLayoutMetrics MetricsFor(float width)
        {
            var metrics = new GraphLayoutMetrics();
            float usable = Mathf.Max(600f, width);
            metrics.CasterLaneWidth = Mathf.Round(usable * 0.36f);
            metrics.TargetLaneWidth = Mathf.Round(usable * 0.24f);
            metrics.ChipLaneWidth = usable - metrics.CasterLaneWidth - metrics.TargetLaneWidth;
            metrics.ChipWidth = Mathf.Min(190f, metrics.ChipLaneWidth - 40f);
            return metrics;
        }

        // The graph viewport's size. A just-created root canvas may not have
        // taken the screen's size yet; the frame's anchors then give it
        // (the canvas has no scaler, so canvas units are screen pixels).
        private Vector2 GraphViewportSize()
        {
            Canvas.ForceUpdateCanvases();
            Rect rect = _graphViewport.rect;
            if (rect.width > 300f && rect.height > 200f) return rect.size;
            float frameWidth = Mathf.Max(800f, Screen.width - 48f);
            float frameHeight = Mathf.Max(600f, Screen.height - 84f);
            return new Vector2(frameWidth * (0.742f - 0.172f) - 16f,
                frameHeight * (0.895f - 0.085f) - 102f - 6f);
        }

        private void RebuildGraph(CastingGraphView view)
        {
            float scroll = _graphScroll == null ? 1f : _graphScroll.verticalNormalizedPosition;
            KingmakerUiFactory.DestroyChildren(_graphContent);
            Vector2 viewport = GraphViewportSize();
            _laidOutViewportSize = _graphViewport == null ? viewport : _graphViewport.rect.size;
            GraphLayoutMetrics m = MetricsFor(viewport.x);
            GraphLayoutResult layout = CastingGraphLayout.Compute(view, m);
            _lastLayout = layout;
            _lastMetrics = m;
            float height = Mathf.Max(layout.Height, viewport.y);
            _graphContent.sizeDelta = new Vector2(0f, height);
            RectTransform lines = Layer("Lines");
            RectTransform chips = Layer("Chips");
            RectTransform casters = Layer("Casters");
            RectTransform targets = Layer("Targets");
            if (view.Casters.Count == 0)
                Hint(casters, "NoCaster", view.SelectedSourceId.Length == 0 ? string.Empty
                    : "Nobody in the party can cast this buff now.", 6f, 10f, m.CasterLaneWidth - 12f);
            foreach (CastingGraphCasterNode caster in view.Casters)
                BuildCasterNode(casters, caster, layout, m);
            foreach (CastingGraphTargetNode target in view.Targets)
                BuildTargetNode(targets, target, layout, m);
            if (view.Castings.Count == 0 && view.SelectedSourceId.Length != 0)
                Hint(chips, "NoCasting", "No castings of this buff in " + view.SelectedRoutineName +
                    " yet. Choose a caster on the left, then click who receives it: each click adds one casting.",
                    m.ChipLaneLeft + 16f, 40f, m.ChipLaneWidth - 32f);
            foreach (GraphConnection connection in layout.Connections)
            {
                CastingGraphCasting casting = view.CastingById(connection.CastingId);
                if (casting == null) continue;
                Color colour = casting.Selected ? Burgundy
                    : casting.Readiness == ResolvedCastingReadiness.Blocked ? BlockedInk
                    : casting.Readiness == ResolvedCastingReadiness.Ready ||
                      casting.Readiness == ResolvedCastingReadiness.AlreadySatisfied ? Ink : InkFaint;
                float thickness = casting.Selected ? SelectedLineThickness : LineThickness;
                DrawSegment(lines, "Ink." + casting.CastingId + ".in", connection.Inbound, colour, thickness);
                DrawSegment(lines, "Ink." + casting.CastingId + ".out", connection.Outbound, colour, thickness);
                foreach (KeyValuePair<string, GraphSegment> branch in connection.Branches)
                    DrawDashed(lines, "Branch." + casting.CastingId + "." + branch.Key, branch.Value,
                        casting.Selected ? Burgundy : InkFaint, 1.5f);
                // Wide invisible corridors: the line is easy to click without
                // pixel precision; both select this casting only.
                Corridor(lines, "Line." + casting.CastingId + ".in", connection.Inbound, casting.CastingId);
                Corridor(lines, "Line." + casting.CastingId + ".out", connection.Outbound, casting.CastingId);
            }
            foreach (GraphChipPlacement placement in layout.Chips)
            {
                CastingGraphCasting casting = view.CastingById(placement.CastingId);
                if (casting != null) BuildChip(chips, casting, placement, m);
            }
            if (_graphScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                _graphScroll.verticalNormalizedPosition = Mathf.Clamp01(scroll);
            }
        }

        private RectTransform Layer(string name)
        {
            RectTransform layer = KingmakerUiFactory.CreateRect(name, _graphContent);
            KingmakerUiFactory.Stretch(layer);
            return layer;
        }

        // Positions a child at graph coordinates (top-left origin, y down).
        private static RectTransform Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
            return rect;
        }

        private void Hint(RectTransform parent, string name, string text, float x, float y, float width)
        {
            if (string.IsNullOrEmpty(text)) return;
            Text hint = KingmakerUiFactory.CreateText(name, parent, _theme, text, 14, TextAnchor.UpperLeft);
            hint.color = _theme.MutedBrownText;
            hint.fontStyle = FontStyle.Italic;
            Place(hint.rectTransform, x, y, width, 80f);
        }

        private void BuildCasterNode(RectTransform layer, CastingGraphCasterNode caster, GraphLayoutResult layout,
            GraphLayoutMetrics m)
        {
            float top;
            if (!layout.CasterTops.TryGetValue(caster.UnitId, out top)) return;
            string captured = caster.UnitId;
            RectTransform header = Place(KingmakerUiFactory.CreateRect("Caster." + (caster.IsUnresolved
                ? "unresolved" : caster.UnitId), layer), 0f, top, m.CasterLaneWidth - 6f, m.CasterHeaderHeight - 4f);
            Image background = KingmakerUiFactory.AddPanel(header, caster.Selected
                ? new Color(1f, 0.95f, 0.84f, 0.95f) : new Color(1f, 1f, 1f, 0f));
            if (!caster.IsUnresolved)
            {
                Button button = header.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                ApplyRowHover(button, caster.Selected);
                button.onClick.AddListener(() => Command(() =>
                    SurfaceClick(_session.ClickGraphCaster(captured, _inputs()))));
            }
            if (caster.Selected)
            {
                RectTransform rule = KingmakerUiFactory.CreateRect("Selected", header);
                KingmakerUiFactory.SetAnchors(rule, 0f, 0f, 0f, 1f);
                rule.pivot = new Vector2(0f, 0.5f);
                rule.sizeDelta = new Vector2(4f, 0f);
                KingmakerUiFactory.AddPanel(rule, Burgundy).raycastTarget = false;
            }
            AddPortrait(header, caster.IsUnresolved ? null : caster.UnitId, 44f, 8f);
            Text name = KingmakerUiFactory.CreateText("Name", header, _theme, caster.DisplayName, 16, TextAnchor.UpperLeft);
            name.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.Stretch(name.rectTransform, 60, 4, 22, 4);
            if (caster.Note.Length != 0)
            {
                Text note = KingmakerUiFactory.CreateText("Note", header, _theme, caster.Note, 12, TextAnchor.LowerLeft);
                note.color = caster.IsUnresolved ? BlockedInk : _theme.MutedBrownText;
                KingmakerUiFactory.Stretch(note.rectTransform, 60, 4, 4, 30);
            }
            for (int index = 0; index < caster.Sources.Count; index++)
            {
                CastingGraphSourceRow row = caster.Sources[index];
                float rowTop;
                if (!layout.SourceRowTops.TryGetValue(CastingGraphLayout.SourceRowKey(caster.UnitId, row.ProviderKey),
                        out rowTop)) continue;
                RectTransform rect = Place(KingmakerUiFactory.CreateRect("Provider." + caster.UnitId + "." + index,
                    layer), 22f, rowTop, m.CasterLaneWidth - 24f, m.SourceRowHeight - 4f);
                Image rowBackground = KingmakerUiFactory.AddPanel(rect, row.Selected
                    ? new Color(0.99f, 0.90f, 0.78f, 1f) : new Color(1f, 0.97f, 0.90f, 0.55f));
                Outline outline = rect.gameObject.AddComponent<Outline>();
                outline.effectColor = row.Selected ? Burgundy : new Color(0.45f, 0.32f, 0.20f, 0.45f);
                outline.effectDistance = row.Selected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
                Button button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = rowBackground;
                ApplyRowHover(button, row.Selected);
                string key = row.ProviderKey;
                button.onClick.AddListener(() => Command(() =>
                    SurfaceClick(_session.ClickGraphSource(key, _inputs()))));
                Text label = KingmakerUiFactory.CreateText("Label", rect, _theme, row.Label +
                        (row.IsGroup ? "  (group)" : string.Empty), 14, TextAnchor.UpperLeft);
                label.fontStyle = FontStyle.Bold;
                KingmakerUiFactory.Stretch(label.rectTransform, 8, 6, 20, 3);
                string capacity = row.CapacityText + " · " + row.PoolLabel +
                    (row.SharedPoolWith.Count == 0 ? string.Empty : " (shared with " +
                        string.Join(", ", row.SharedPoolWith.ToArray()) + ")");
                Text count = KingmakerUiFactory.CreateText("Capacity", rect, _theme,
                    row.Usable ? capacity : row.BlockedReason + " " + capacity, 12, TextAnchor.LowerLeft);
                count.color = row.Usable ? new Color(0.22f, 0.36f, 0.22f, 1f) : BlockedInk;
                count.resizeTextForBestFit = true;
                count.resizeTextMinSize = 10;
                count.resizeTextMaxSize = 12;
                KingmakerUiFactory.Stretch(count.rectTransform, 8, 6, 3, 21);
            }
        }

        private void BuildTargetNode(RectTransform layer, CastingGraphTargetNode target, GraphLayoutResult layout,
            GraphLayoutMetrics m)
        {
            float top;
            if (!layout.TargetTops.TryGetValue(target.UnitId, out top)) return;
            string captured = target.UnitId;
            RectTransform rect = Place(KingmakerUiFactory.CreateRect("Target." + target.UnitId, layer),
                m.TargetLaneLeft + 6f, top, m.TargetLaneWidth - 8f, m.TargetNodeHeight);
            bool legal = target.Legality == CastingGraphTargetLegality.Legal;
            bool illegal = target.Legality == CastingGraphTargetLegality.Illegal;
            Image background = KingmakerUiFactory.AddPanel(rect, legal
                ? new Color(0.93f, 0.97f, 0.88f, 0.80f) : new Color(1f, 0.97f, 0.90f, 0.45f));
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = legal ? LegalInk : new Color(0.45f, 0.32f, 0.20f, 0.35f);
            outline.effectDistance = legal ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ApplyRowHover(button, false);
            button.onClick.AddListener(() => Command(() =>
                SurfaceClick(_session.ClickGraphRecipient(captured, _inputs()))));
            Image portrait = AddPortrait(rect, target.UnitId, 62f, 6f);
            if (portrait != null && illegal) portrait.color = new Color(0.55f, 0.50f, 0.48f, 0.75f);
            Text name = KingmakerUiFactory.CreateText("Name", rect, _theme, target.DisplayName, 15, TextAnchor.UpperLeft);
            name.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.Stretch(name.rectTransform, 74, 4, 50, 6);
            Text hint = KingmakerUiFactory.CreateText("Hint", rect, _theme,
                illegal ? target.IllegalReason : target.Hint, 12, TextAnchor.UpperLeft);
            hint.color = illegal ? BlockedInk : legal ? LegalInk : _theme.MutedBrownText;
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            KingmakerUiFactory.Stretch(hint.rectTransform, 74, 4, 4, 28);
        }

        private void BuildChip(RectTransform layer, CastingGraphCasting casting, GraphChipPlacement placement,
            GraphLayoutMetrics m)
        {
            string captured = casting.CastingId;
            RectTransform rect = Place(KingmakerUiFactory.CreateRect("Casting." + casting.CastingId, layer),
                placement.Left.X, placement.Top, m.ChipWidth, m.ChipHeight);
            // Opaque on the paper too: the chip's state reads from its own
            // ground, outline and status ink (contrast-tested, WP7).
            Image background = KingmakerUiFactory.AddPanel(rect, KingmakerUiFactory.ToColor(casting.Selected
                ? PlannerParchmentPalette.ChipGroundSelected : PlannerParchmentPalette.ChipGround));
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = casting.Selected ? Burgundy
                : casting.Readiness == ResolvedCastingReadiness.Blocked ? BlockedInk : Ink;
            outline.effectDistance = casting.Selected ? new Vector2(3f, -3f) : new Vector2(1f, -1f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ApplyRowHover(button, casting.Selected);
            button.onClick.AddListener(() => Command(() => _session.FocusGraphCasting(captured)));
            AddSpellInspect(button, () => _session.BuildGraph(_freshInputs()),
                ChipInspectTitle(casting, _lastView),
                casting.SpellDescription, casting.SpellDurationText, true);
            string status = (_session.ProblemNavigation.Active && casting.Selected
                ? "Problem " + _session.ProblemNavigation.Position + " of " + _session.ProblemNavigation.Count +
                    " · Not ready" : casting.StatusLabel) +
                (casting.ShortInOnePass ? " · short in one pass" : string.Empty) +
                (casting.RedundantInOnePass ? " · covered earlier" : string.Empty) +
                (casting.NeedsReview ? " · review" : string.Empty);
            Text first = KingmakerUiFactory.CreateText("Status", rect, _theme, casting.OrderLabel + "  " + status,
                14, TextAnchor.UpperLeft);
            first.fontStyle = FontStyle.Bold;
            first.color = casting.Readiness == ResolvedCastingReadiness.Blocked || casting.ShortInOnePass
                ? BlockedInk : _theme.DarkBrownText;
            first.resizeTextForBestFit = true;
            first.resizeTextMinSize = 11;
            first.resizeTextMaxSize = 14;
            KingmakerUiFactory.Stretch(first.rectTransform, 7, 5, 22, 3);
            string detail = casting.IsGroup
                ? "Group · expected to reach " + casting.Beneficiaries.Count
                : casting.EnhancementBadges.Count == 0 ? "no enhancement"
                : string.Join(", ", casting.EnhancementBadges.ToArray());
            if (casting.IsGroup && casting.EnhancementBadges.Count != 0)
                detail += " · " + string.Join(", ", casting.EnhancementBadges.ToArray());
            Text second = KingmakerUiFactory.CreateText("Detail", rect, _theme, detail, 12, TextAnchor.LowerLeft);
            second.color = _theme.MutedBrownText;
            second.resizeTextForBestFit = true;
            second.resizeTextMinSize = 10;
            second.resizeTextMaxSize = 12;
            KingmakerUiFactory.Stretch(second.rectTransform, 7, 5, 4, 24);
        }

        // The chip's spell title for the inspect: the provider key's tail
        // (book/level context lives in the inspector), or the order label.
        // "#2 Resistance": the casting's order and the spell as the game names
        // it; when the source is gone from the party snapshot, the buff shown
        // in the banner (the graph only shows that buff's castings) - never
        // a provider-key fragment (D12).
        internal static string ChipInspectTitle(CastingGraphCasting casting, CastingGraphView view)
        {
            string name = !string.IsNullOrWhiteSpace(casting.SpellTitle) ? casting.SpellTitle
                : view != null && !string.IsNullOrWhiteSpace(view.SelectedSourceCaption)
                    ? view.SelectedSourceCaption : "Spell";
            return (casting.OrderLabel + " " + name).Trim();
        }

        // A straight ink segment: one rotated Image (graph y points down,
        // Unity's up, so the angle is negated).
        private void DrawSegment(RectTransform layer, string name, GraphSegment segment, Color colour,
            float thickness)
        {
            if (segment.Length < 0.5f) return;
            RectTransform rect = KingmakerUiFactory.CreateRect(name, layer);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(segment.Length, thickness);
            rect.anchoredPosition = new Vector2(segment.Center.X, -segment.Center.Y);
            rect.localRotation = Quaternion.Euler(0f, 0f, -segment.AngleDegrees);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
        }

        // Derived beneficiary branch: dashes along the segment (never a
        // casting, never clickable).
        private void DrawDashed(RectTransform layer, string name, GraphSegment segment, Color colour, float thickness)
        {
            const float dash = 7f;
            const float gap = 5f;
            if (segment.Length < 1f) return;
            float dx = (segment.To.X - segment.From.X) / segment.Length;
            float dy = (segment.To.Y - segment.From.Y) / segment.Length;
            int index = 0;
            for (float start = 0f; start < segment.Length; start += dash + gap)
            {
                float end = Mathf.Min(segment.Length, start + dash);
                var from = new GraphPoint(segment.From.X + dx * start, segment.From.Y + dy * start);
                var to = new GraphPoint(segment.From.X + dx * end, segment.From.Y + dy * end);
                DrawSegment(layer, name + "." + index++, new GraphSegment(from, to), colour, thickness);
            }
        }

        // The invisible, wide hit corridor along a connection segment.
        private void Corridor(RectTransform layer, string name, GraphSegment segment, string castingId)
        {
            if (segment.Length < 2f) return;
            RectTransform rect = KingmakerUiFactory.CreateRect(name, layer);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(segment.Length, CorridorThickness);
            rect.anchoredPosition = new Vector2(segment.Center.X, -segment.Center.Y);
            rect.localRotation = Quaternion.Euler(0f, 0f, -segment.AngleDegrees);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            string captured = castingId;
            button.onClick.AddListener(() => Command(() => _session.FocusGraphCasting(captured)));
        }

        // Rows, chips and portraits: a restrained hover on their own panel,
        // exactly one control at a time (pointer-only highlight).
        private static void ApplyRowHover(Button button, bool selected)
        {
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = selected ? new Color(0.97f, 0.90f, 0.82f, 1f) : new Color(0.90f, 0.84f, 0.74f, 1f);
            colors.pressedColor = new Color(0.80f, 0.72f, 0.60f, 1f);
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.8f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            KingmakerUiFactory.OwnPointerHighlight(button);
        }

        private Image AddPortrait(RectTransform parent, string unitId, float size, float x)
        {
            RectTransform rect = KingmakerUiFactory.CreateRect("Portrait", parent);
            KingmakerUiFactory.SetAnchors(rect, 0f, 1f, 0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(size * 0.78f, size);
            rect.anchoredPosition = new Vector2(x, -4f);
            Sprite portrait = unitId == null ? null : BuffPlannerScreenView.ResolvePortrait(unitId);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = portrait;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = portrait == null ? new Color(0.35f, 0.25f, 0.18f, 0.35f) : Color.white;
            return image;
        }

        private void AddIcon(RectTransform parent, AbilityKey ability, float size, float x)
        {
            RectTransform rect = KingmakerUiFactory.CreateRect("Icon", parent);
            KingmakerUiFactory.SetAnchors(rect, 0f, 0.5f, 0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(x, 0f);
            Sprite icon = ResolveAbilityIcon(ability);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = icon == null ? new Color(0f, 0f, 0f, 0f) : Color.white;
        }

        private static Sprite ResolveAbilityIcon(AbilityKey ability)
        {
            if (ability == null) return null;
            try
            {
                if (!string.IsNullOrWhiteSpace(ability.VariantGuid))
                {
                    BlueprintAbility concrete = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(ability.VariantGuid);
                    if (concrete != null && concrete.Icon != null) return concrete.Icon;
                }
                BlueprintAbility parent = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(ability.BaseAbilityGuid);
                return parent == null ? null : parent.Icon;
            }
            catch (Exception)
            {
                // A non-ability source (item, fact) shows no icon.
                return null;
            }
        }

        // ------------------------------------------------------------------
        // Inspector
        // ------------------------------------------------------------------

        private void RebuildInspector(CastingGraphView view)
        {
            KingmakerUiFactory.DestroyChildren(_inspectorContent);
            RebuildImportNotices();
            if (view.Inspector == null)
            {
                _inspectorTitle.text = "Next casting";
                RebuildNextCasting(view);
                return;
            }
            RebuildCastingInspector(view, view.Inspector);
        }

        private void RebuildNextCasting(CastingGraphView view)
        {
            CastingGraphCasterNode caster = view.CasterById(view.SelectedCasterUnitId);
            CastingGraphSourceRow row = caster == null ? null
                : caster.Sources.FirstOrDefault(value => value.Selected);
            Line("Steps", "1. Buff: " + (view.SelectedSourceId.Length == 0 ? "choose one on the left"
                : view.SelectedSourceCaption), 14, _theme.DarkBrownText, true);
            Line("StepCaster", "2. Caster: " + (caster == null ? "click a caster in the left lane"
                : caster.DisplayName + (row != null ? " · " + row.Label
                    : caster.Sources.Count > 1 ? " · now choose the exact source under the caster" : string.Empty)),
                14, _theme.DarkBrownText, false);
            Line("StepTarget", "3. Target: " + (row == null ? "available once the source is chosen"
                : view.SelectedSourceIsGroup == true ? "click the unit the group is centred on"
                : "click who receives it; each click adds one casting"), 14, _theme.DarkBrownText, false);
            Line("StepEdit", "4. Click a casting's line or card to set its enhancements.", 14,
                _theme.DarkBrownText, false);
            if (row != null)
            {
                Section("Chosen source");
                Line("SourceDetail", row.Detail, 13, _theme.DarkBrownText, false);
                Line("SourceCapacity", row.CapacityText + " after the whole plan · " + row.PoolLabel, 13,
                    row.Usable ? LegalInk : BlockedInk, false);
                if (!row.Usable) Line("SourceBlocked", row.BlockedReason, 13, BlockedInk, false);
            }
            // v1.2 §7 / E16: the Share control sits between the exact
            // source and the target click - the owner's authoring order.
            if (row != null && view.NextCastingModifiers != null &&
                view.NextCastingModifiers.Count != 0)
            {
                Section("Share with an ally (next casting)");
                foreach (CastingGraphModifierOption modifier in view.NextCastingModifiers)
                {
                    string id = modifier.ModifierId;
                    Button toggle = ActionButton("DraftModifier." + id,
                        (modifier.Selected ? "[x] " : "[  ] ") + modifier.Title,
                        () => Surface(_session.ToggleDraftTargetingModifier(id, _inputs()), "share"));
                    if (!modifier.Selected && !modifier.Available)
                        KingmakerUiFactory.SetInteractable(toggle, false);
                    if (modifier.Selected) KingmakerUiFactory.ApplyPalette(toggle, true);
                    if (modifier.Detail.Length != 0)
                        Line("DraftModifierDetail", modifier.Detail, 12, _theme.MutedBrownText, false);
                    if (modifier.CostText.Length != 0)
                        Line("DraftModifierCost", "Cost: " + modifier.CostText, 12,
                            _theme.DarkBrownText, false);
                    if (!modifier.Selected && modifier.UnavailableReason.Length != 0)
                        Line("DraftModifierUnavailable",
                            WorkspaceReasonText.DescribeNested(modifier.UnavailableReason),
                            12, BlockedInk, false);
                }
            }
            // The next casting's existing-effect choice (each casting keeps its
            // own afterwards; its inspector changes only that one).
            bool recastDraft = _session.Draft.ExistingEffectPolicy == ExistingEffectPolicy.Overwrite;
            ActionButton("DraftRecastPolicy", recastDraft ? "[x] New castings: cast again even if active"
                : "[  ] New castings: cast again even if active (now: skip)", () =>
            {
                _session.Draft.ExistingEffectPolicy = recastDraft
                    ? ExistingEffectPolicy.SkipAlreadyActive : ExistingEffectPolicy.Overwrite;
            });
        }

        private void RebuildCastingInspector(CastingGraphView view, CastingGraphInspector inspector)
        {
            _inspectorTitle.text = inspector.Title;
            CastingProblemNavigation problems = _session.ProblemNavigation;
            bool problem = problems.Active && string.Equals(problems.Current.CastingId,
                inspector.CastingId, StringComparison.Ordinal);
            if (problem)
            {
                Line("ProblemPosition", inspector.RoutineName + " · Problem " + problems.Position +
                    " of " + problems.Count, 17, Burgundy, true);
                Line("ProblemBuff", view.SelectedSourceCaption, 16, _theme.DarkBrownText, true);
            }
            else ActionButton("DoneEditing", "Done (back to the next casting)", () => _session.ClearGraphFocus());
            Line("Headline", inspector.Headline, 17, _theme.DarkBrownText, true);
            CastingGraphCasting chip = inspector.Chip;
            string status = chip == null ? "Not shown in this routine" : chip.StatusLabel;
            Line("CastingStatus", "Status: " + status, 14,
                problem || chip != null && chip.Readiness == ResolvedCastingReadiness.Blocked ? BlockedInk : LegalInk, true);
            foreach (string reason in problem ? problems.Current.Reasons.Select(WorkspaceReasonText.Describe)
                : inspector.Reasons)
                Line("Reason", (problem ? "Not ready: " : "· ") + reason, 13, BlockedInk, false);
            if (problem)
            {
                Button previous = ActionButton("ProblemPrevious", "Previous Problem",
                    () => _session.NavigateProblem(-1));
                Button next = ActionButton("ProblemNext", "Next Problem",
                    () => _session.NavigateProblem(1));
                KingmakerUiFactory.SetInteractable(previous, problems.CanPrevious);
                KingmakerUiFactory.SetInteractable(next, problems.CanNext);
                ActionButton("DoneEditing", "Done (back to the next casting)", () => _session.ClearGraphFocus());
            }
            if (chip != null && chip.ShortInOnePass)
                Line("ShortInOnePass", "· short of a resource when every routine runs in one pass", 13, BlockedInk, false);
            if (chip != null && chip.RedundantInOnePass)
                Line("Redundant", "· an earlier identical casting already gives this in one pass; with \"skip if " +
                    "active\" it would be skipped and spend nothing", 13, _theme.MutedBrownText, false);
            if (inspector.ExecutionLimitation.Length != 0)
                Line("Limitation", "Cannot run in this version: " + inspector.ExecutionLimitation, 13, BlockedInk, false);
            if (inspector.ReviewItems.Count != 0)
            {
                Section("Imported: needs your review");
                foreach (string item in inspector.ReviewItems) Line("ReviewItem", "· " + item, 13, BlockedInk, false);
                ActionButton("ResolveImportReview", "Resolve review (keep current choices)", () =>
                    Surface(_session.ResolveFocusedImportReview(), "resolve review"));
            }
            // Target or group coverage.
            PlannedCasting focused = inspector.Casting;
            bool group = focused.TargetMode != CastingTargetMode.DirectTarget;
            Section(group ? "Group" : "Target");
            Line("TargetLabel", inspector.TargetLabel, 14, _theme.DarkBrownText, false);
            // An imported casting may be in the wrong shape for its buff: a
            // single-target casting of a group buff can become one group
            // casting centred on its caster (its target stays required), and
            // a group casting of a single-target buff can become a casting on
            // one chosen member. Offered only when the buff needs it.
            bool? groupAbility = _session.FocusedCastingIsGroupAbility(_inputs());
            if (!group)
            {
                if (groupAbility == true)
                    ActionButton("FocusedMode.Group", "Make it a group casting (centred on the caster)",
                        () => Surface(_session.SetFocusedTargeting(CastingTargetMode.CasterCenteredOrigin, null, null,
                            null), "mode"));
            }
            else if (groupAbility == false)
            {
                Line("SingleHint", "This buff has one target per casting: choose who receives it.", 13,
                    BlockedInk, false);
                foreach (CastingGraphTargetNode target in view.Targets)
                {
                    string unit = target.UnitId;
                    ActionButton("FocusedSingle." + unit, "Single target: " + target.DisplayName,
                        () => Surface(_session.SetFocusedTargeting(CastingTargetMode.DirectTarget, unit, null, null),
                            "mode"));
                }
            }
            if (group && inspector.CoverageText.Length != 0)
                Line("Coverage", inspector.CoverageText, 13, _theme.DarkBrownText, false);
            // WP3: retargeting, re-centring, removal and the caster are graph
            // gestures; the inspector only says how.
            Line("GestureHint", group
                ? "Click another party member to re-centre it there; click its centre again to remove it. " +
                    "Click another caster or source on the left to change who casts it."
                : "Click another portrait to move it; click its recipient again to remove it. " +
                    "Click another caster or source on the left to change who casts it.",
                12, _theme.MutedBrownText, false);
            // v1.2 §7 / E16: this casting's own Share control - on/off
            // through the normal authoring boundary (announces, autosaves,
            // undoable), with the same honest availability the compiler
            // judges. Disabling keeps a now-illegal ally target visible
            // and repairable.
            if (inspector.Modifiers != null && inspector.Modifiers.Count != 0)
            {
                Section("Share with an ally");
                foreach (CastingGraphModifierOption modifier in inspector.Modifiers)
                {
                    string id = modifier.ModifierId;
                    Button toggle = ActionButton("FocusedModifier." + id,
                        (modifier.Selected ? "[x] " : "[  ] ") + modifier.Title,
                        () => Surface(_session.ToggleFocusedTargetingModifier(id, _inputs()), "share"));
                    if (!modifier.Selected && !modifier.Available)
                        KingmakerUiFactory.SetInteractable(toggle, false);
                    if (modifier.Selected) KingmakerUiFactory.ApplyPalette(toggle, true);
                    if (modifier.Detail.Length != 0)
                        Line("FocusedModifierDetail", modifier.Detail, 12, _theme.MutedBrownText, false);
                    if (modifier.CostText.Length != 0)
                        Line("FocusedModifierCost", "Cost: " + modifier.CostText, 12,
                            _theme.DarkBrownText, false);
                    // A saved choice that no longer applies stays visible
                    // as a repair warning; untick it to repair the casting.
                    if (modifier.UnavailableReason.Length != 0)
                        Line("FocusedModifierUnavailable", (modifier.Selected
                                ? "Chosen, but no longer available: " : string.Empty) +
                            WorkspaceReasonText.DescribeNested(modifier.UnavailableReason) +
                            (modifier.Selected ? " - untick it to repair this casting" : string.Empty),
                            12, BlockedInk, false);
                }
            }
            // Enhancements: this casting only.
            Section("Enhancements (this casting only)");
            if (inspector.Enhancements.Count == 0)
                Line("NoEnhancement", "None available for this caster and source.", 13, _theme.MutedBrownText, false);
            foreach (CastingGraphEnhancementOption option in inspector.Enhancements)
            {
                string id = option.EnhancementId;
                Button toggle = ActionButton("Enhancement." + id,
                    (option.Selected ? "[x] " : "[  ] ") + option.Name + (option.Selected && !option.Required
                        ? " (optional, imported)" : string.Empty),
                    () => Surface(_session.ToggleFocusedEnhancement(id, _inputs()), "enhancement"));
                if (!option.Selected && option.UnavailableReason.Length != 0)
                    KingmakerUiFactory.SetInteractable(toggle, false);
                if (option.Selected) KingmakerUiFactory.ApplyPalette(toggle, true);
                Line("EnhancementSource", option.Mechanism + " · " + option.SourceName, 12, _theme.MutedBrownText, false);
                Line("EnhancementEffect", "Effect: " + option.ExpectedEffect, 12, _theme.DarkBrownText, false);
                Line("EnhancementCost", "Cost: " + option.CostText + " · " + option.BudgetText, 12,
                    option.PoolRemaining == 0 && !option.Selected ? BlockedInk : _theme.DarkBrownText, false);
                if (option.UnavailableReason.Length != 0 && !option.Selected)
                    Line("EnhancementUnavailable", option.UnavailableReason, 12, BlockedInk, false);
            }
            Section("Cost of this casting");
            if (inspector.CostLines.Count == 0)
                Line("NoCost", "Nothing is reserved for it now.", 13, _theme.MutedBrownText, false);
            foreach (string cost in inspector.CostLines) Line("Cost", "· " + cost, 13, _theme.DarkBrownText, false);
            // Routine, order, existing effect, state.
            Section("Routine and order");
            Line("RoutineLine", "Casting " + (inspector.Casting.Order + 1) + " of " + inspector.RoutineCount +
                " in " + inspector.RoutineName, 13, _theme.DarkBrownText, false);
            foreach (CastingGraphRoutineTab routine in view.Routines)
            {
                string routineId = routine.RoutineId;
                if (string.Equals(routineId, inspector.Casting.RoutineId, StringComparison.Ordinal)) continue;
                ActionButton("FocusedRoutine." + routineId, "Move to " + routine.Name,
                    () => Surface(_session.MoveFocusedCastingToRoutine(routineId), "move"));
            }
            Button earlier = ActionButton("FocusedOrder.Earlier", "Cast earlier",
                () => Surface(_session.MoveFocusedCastingWithinRoutine(-1), "move"));
            KingmakerUiFactory.SetInteractable(earlier, inspector.CanMoveEarlier);
            Button later = ActionButton("FocusedOrder.Later", "Cast later",
                () => Surface(_session.MoveFocusedCastingWithinRoutine(1), "move"));
            KingmakerUiFactory.SetInteractable(later, inspector.CanMoveLater);
            Section("If the buff is already there");
            ActionButton("FocusedRecastPolicy", inspector.RecastsExisting ? "[x] Cast it again anyway"
                : "[  ] Cast it again anyway (now: skip it)",
                () => Surface(_session.SetFocusedRecastPolicy(inspector.RecastsExisting
                    ? ExistingEffectPolicy.SkipAlreadyActive : ExistingEffectPolicy.Overwrite), "recast"));
            foreach (string note in inspector.ExistingEffectNotes)
                Line("ExistingEffect", "· " + note, 12, _theme.MutedBrownText, false);
            if (inspector.LastRun.Length != 0)
                Line("LastRun", "Last run: " + inspector.LastRun, 12, _theme.MutedBrownText, false);
            Section("This casting");
            // WP3: a genuine Draft (an imported choice still to finish) can be
            // marked Ready. A Disabled record from an earlier version is shown
            // honestly - it is never cast and never blocks - and is removed,
            // not re-enabled; no new Disabled record or duplicate is authored.
            if (inspector.Casting.State == CastingAuthoringState.Draft)
                ActionButton("MarkReady", "Mark Ready", () => Surface(_session.SetFocusedCastingState(
                    CastingAuthoringState.Ready), "mark ready"));
            if (inspector.Casting.State == CastingAuthoringState.Disabled)
                Line("LegacyDisabled", "Disabled in an earlier version: it is not cast and blocks nothing. " +
                    "Remove it if you no longer want it.", 13, _theme.MutedBrownText, false);
            ActionButton("Remove", "Remove this casting", () =>
            {
                AuthoringEditResult result = _session.RemoveFocusedCasting();
                if (!result.Applied) Surface(result, "remove");
                else _footerResult.text = "Casting removed (Undo restores it).";
            });
        }

        private void RebuildImportNotices()
        {
            IReadOnlyList<string> pending = _session.PendingImportNotices;
            if (pending.Count == 0) return;
            Section("Imported plan-wide constraints (" + pending.Count + ")");
            foreach (string notice in pending) Line("ImportNotice", "· " + notice, 12, _theme.MutedBrownText, false);
            Line("ImportNoticeRule", "Apply is refused until these are acknowledged.", 12, BlockedInk, false);
            ActionButton("AcknowledgeImportNotices", "Acknowledge imported constraints", () =>
            {
                AuthoringEditResult result = _session.AcknowledgeImportNotices();
                Surface(result, "acknowledge");
                if (result.Applied) _footerResult.text = "Acknowledged (Undo reverts): " + result.Scope;
            });
        }

        private void Section(string caption)
        {
            Text label = KingmakerUiFactory.CreateText("Section." + caption, _inspectorContent, _theme, caption, 14,
                TextAnchor.LowerLeft);
            label.fontStyle = FontStyle.Bold;
            label.color = Burgundy;
            KingmakerUiFactory.AddLayout(label.rectTransform, 26f);
        }

        private Text Line(string name, string text, int size, Color colour, bool bold)
        {
            Text line = KingmakerUiFactory.CreateText(name, _inspectorContent, _theme, text, size,
                TextAnchor.UpperLeft);
            line.color = colour;
            line.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            line.horizontalOverflow = HorizontalWrapMode.Wrap;
            line.verticalOverflow = VerticalWrapMode.Overflow;
            // Wrapped text takes its own preferred height in the column.
            return line;
        }

        private Button ActionButton(string name, string caption, Action action)
        {
            Button button = KingmakerUiFactory.CreateButton(name, _inspectorContent, _theme, caption,
                () => Command(action));
            foreach (Text text in button.GetComponentsInChildren<Text>(true))
            {
                text.fontSize = 14;
                text.resizeTextMaxSize = 14;
                text.resizeTextMinSize = 11;
                text.alignment = TextAnchor.MiddleLeft;
            }
            KingmakerUiFactory.AddLayout(RectOf(button), 30f);
            return button;
        }

        // ------------------------------------------------------------------
        // Commands (all go through the session; the view refreshes after)
        // ------------------------------------------------------------------

        private void Command(Action action)
        {
            if (_disposed) return;
            try
            {
                action();
            }
            catch (Exception exception)
            {
                // A failed command is told, never swallowed into a stale view.
                _footerResult.text = "That did not work: " + exception.Message;
                Debug.LogError("[KBP-WORKSPACE] command failed: " + exception);
            }
            RefreshView();
        }

        // WP3: one footer sentence per graph gesture; a refusal keeps the
        // casting unchanged and says why.
        private void SurfaceClick(CastingGraphClickResult result)
        {
            if (result == null) return;
            switch (result.Outcome)
            {
                case CastingGraphClickOutcome.Added:
                    _footerResult.text = "Added one casting and selected it. Click the same portrait again " +
                        "to remove it, or another to move it (Undo reverses either).";
                    return;
                case CastingGraphClickOutcome.Focused:
                    _footerResult.text = "Selected the casting this recipient already has. Click the portrait " +
                        "again to remove it.";
                    return;
                case CastingGraphClickOutcome.Removed:
                    _footerResult.text = "Casting removed (Undo restores it).";
                    return;
                case CastingGraphClickOutcome.Retargeted:
                    _footerResult.text = "Moved the casting to a new recipient (Undo moves it back).";
                    return;
                case CastingGraphClickOutcome.Recentred:
                    _footerResult.text = "Re-centred the group casting (Undo moves it back).";
                    return;
                case CastingGraphClickOutcome.ProviderChanged:
                    _footerResult.text = "Changed who casts it" + (string.IsNullOrEmpty(result.Edit.Reason)
                        ? string.Empty : ": " + result.Edit.Reason) + " (Undo restores the previous caster).";
                    return;
                case CastingGraphClickOutcome.DraftConfigured:
                    return;
                default:
                    Surface(result.Edit, "that click");
                    return;
            }
        }

        // A denied operation explains itself; an applied one with a note
        // shows the note.
        private void Surface(AuthoringEditResult result, string action)
        {
            if (result == null) return;
            if (!result.Applied)
                _footerResult.text = char.ToUpperInvariant(action[0]) + action.Substring(1) + " refused: " +
                    WorkspaceRefusalText.Describe(result.Reason);
            else if (!string.IsNullOrEmpty(result.Reason))
                _footerResult.text = char.ToUpperInvariant(action[0]) + action.Substring(1) + ": " + result.Reason;
        }

        // The exceptional recovery action for a refused save (offered only
        // then - v1.2 §4): re-read the stored plan after the player moved
        // the unreadable file aside or repaired the previous plan.
        private void ReloadCommand()
        {
            // A reload that can replace edits not yet on disk asks for a second press.
            if (_session.IsDirty && Time.unscaledTime > _reloadArmedUntil)
            {
                _reloadArmedUntil = Time.unscaledTime + 5f;
                _footerResult.text = "Reload replaces this plan with the stored one, and edits that are not " +
                    "saved can be lost. Press Reload again to continue.";
                return;
            }
            _reloadArmedUntil = 0f;
            CastingPlanLoadStatus status = _session.Reload();
            // Castings kept across the reload are deliberate edits made
            // while saving was blocked: now that it is not, they save like
            // any other edit.
            bool keptSaved = _session.LastReloadNote != "kept-unsaved" || _session.RetryFailedSave();
            _footerResult.text = _session.LegacyImportBlocked
                ? DescribeImport(_session)
                : PersistenceMessages.ForCastingLoad(status, _session.PrimaryPlanFileExists,
                    _session.LoadSourcePath, _session.LoadWarning) ??
                  (_session.LastReloadNote == "kept-unsaved"
                      ? (keptSaved
                          ? "No casting plan was stored yet; the castings you added were kept and are now saved."
                          : WorkspaceFooterText.SaveProblem(_session.FooterSaveState))
                      : _session.LastReloadNote == "imported-into-unsaved"
                          ? "Your classic plan was imported alongside your castings and saved."
                          : _session.ImportReport != null ? DescribeImport(_session)
                          : status == CastingPlanLoadStatus.Absent
                              ? "No casting plan is stored yet; your first edit saves one."
                              : "Reloaded the stored casting plan.");
        }

        private void ToggleModeCommand()
        {
            _session.SetExecutionMode(_session.ExecutionMode == "instant" ? "animated" : "instant");
            KingmakerUiFactory.SetButtonLabel(_modeButton, ModeCaption());
            _footerResult.text = WorkspaceFooterText.ModeChanged(_session.ExecutionMode);
        }

        private void UndoCommand()
        {
            if (!_session.Undo()) _footerResult.text = "Nothing to undo.";
        }

        private string ModeCaption()
        {
            return _session.ExecutionMode == "instant" ? "Mode: Instant" : "Mode: Animated";
        }

        private string DescribeReadiness()
        {
            // 0.4.2 (B): an automatic spellbook-removal edit stays named
            // (with its Undo) until it is undone or superseded.
            string removal = _session.SpellbookReconciliationNotice;
            if (!string.IsNullOrEmpty(removal))
                return string.IsNullOrEmpty(_session.LastAttemptMessage) ||
                    _session.LastAttemptMessage.StartsWith(removal, StringComparison.Ordinal)
                    ? removal : removal + " Last attempt: " + _session.LastAttemptMessage;
            if (!string.IsNullOrEmpty(_session.LastAttemptMessage))
                return "Last attempt: " + _session.LastAttemptMessage;
            CastingRunReport last = _session.LastRunReport;
            if (last != null)
                return "Last run: " + CastingRunPresentation.Describe(last,
                    _session.RoutineDisplayName(last.ScopeRoutineId), _session.CastingLabel);
            return WorkspaceFooterText.Readiness(_session.DispatchDisposition,
                _session.RoutineDisplayName(_session.SelectedRoutineId));
        }

        private void RunApply(CastingApplyMode mode)
        {
            string name = _session.RoutineDisplayName(_session.SelectedRoutineId);
            CastingWorkspaceInputs inputs;
            try { inputs = _freshInputs(); }
            catch (Exception exception)
            {
                // Without fresh discovery nothing is submitted.
                string refreshRefusal = name + " was not cast: the party state could not be refreshed (" +
                    exception.Message + ").";
                _session.RecordGlobalRefusal(refreshRefusal);
                _footerResult.text = refreshRefusal;
                return;
            }
            WorkspaceApplyResult result = _session.Apply(mode, _session.SelectedRoutineId, inputs);
            if (!result.Allowed && result.BlockingCastings.Count != 0 &&
                mode == CastingApplyMode.Ordinary)
                _readyOnlyButton.gameObject.SetActive(true);
            if (result.Allowed && result.Dispatch != null && result.Dispatch.Submitted)
            {
                // The run proceeds in the world; the result is reported at its end.
                _close();
                return;
            }
            string refusal = CastingRunPresentation.DescribeRefusal(name, result, _session);
            if (result.SpellbookReconciliation != null && result.SpellbookReconciliation.Applied)
                refusal = result.SpellbookReconciliation.Notice + " " + refusal;
            _session.RecordAttempt(refusal);
            _footerResult.text = refusal;
            Debug.Log("[KBP-CF-RUN] refused;reason=" + result.ReviewReason);
            if (result.Dispatch != null && !result.Dispatch.Submitted && result.Projection != null &&
                result.Projection.Converted)
                _footerResult.text += " Would run " + result.Projection.Plan.Steps.Count +
                    (result.Projection.Plan.Steps.Count == 1 ? " cast" : " casts") + " in order.";
        }

        private void RebuildFooter(CastingGraphView view)
        {
            bool problems = _session.ProblemNavigation.Active;
            if (problems || _showedProblems)
                _footerResult.text = _session.LastAttemptMessage ?? DescribeReadiness();
            _showedProblems = problems;
            if (problems) _readyOnlyButton.gameObject.SetActive(true);
            else if (view.SelectedRoutineGate != null && view.SelectedRoutineGate.BlockingCastings.Count == 0)
                _readyOnlyButton.gameObject.SetActive(false);
            _footerSelectedRun.text = view.SelectedRunLabel + ": " + (view.SelectedRunBudget.Count == 0
                ? "nothing spent yet" : string.Join("   ", view.SelectedRunBudget.ToArray()));
            // The conflict first: a long pool list may be cut short, the
            // shortfall never is.
            string shortfall = WorkspaceFooterText.WholePlan(view.OnePassShortCount);
            _footerOnePass.text = (shortfall.Length == 0 ? string.Empty : shortfall + "   ") +
                view.OnePassLabel + ": " + (view.OnePassBudget.Count == 0
                    ? "nothing spent yet" : string.Join("   ", view.OnePassBudget.ToArray()));
            _footerOnePass.color = view.OnePassShortCount > 0 ? BlockedInk : _theme.DarkBrownText;
            if (_undoButton != null) KingmakerUiFactory.SetInteractable(_undoButton, _session.CanUndo);
            // Run runs the SELECTED routine, so it says which one.
            if (_runButton != null)
                KingmakerUiFactory.SetButtonLabel(_runButton, WorkspaceFooterText.RunCaption(
                    _session.RoutineDisplayName(_session.SelectedRoutineId)));
            ShowSaveState();
        }

        private static string DescribeImport(CastingWorkspaceSession session)
        {
            if (session.LegacyImportBlocked)
                return "Your previous plan could not be imported (" + session.LegacyImportBlockReason +
                    "). It was NOT replaced: saving and runs are blocked. Repair or restore that file, then press " +
                    "Reload to retry.";
            CastingImportReport report = session.ImportReport;
            if (report == null) return null;
            return "Imported " + report.ResultingCastingCount +
                (report.ResultingCastingCount == 1 ? " casting" : " castings") +
                " from your previous plan: " + report.ReadyCount + " ready, " + report.DraftCount + " need review" +
                (report.UnresolvedCasterCount == 0 ? string.Empty : ", " + report.UnresolvedCasterCount +
                    " without a caster") +
                (report.GroupReviewCount == 0 ? string.Empty : ", " + report.GroupReviewCount + " group(s) to confirm") +
                (report.Warnings.Count == 0 ? string.Empty : " · " + report.Warnings.Count + " warning(s)") +
                ". Your previous plan file was kept unchanged.";
        }
    }
}
