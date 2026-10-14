using System;
using System.Collections;
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
using TMPro;
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

        private static readonly Color Ink = new Color(0.20f, 0.14f, 0.10f, 0.85f);
        private static readonly Color InkFaint = new Color(0.20f, 0.14f, 0.10f, 0.35f);
        // The inks the contrast tests measure on the paper (WP7).
        private static readonly Color Burgundy = KingmakerUiFactory.ToColor(PlannerParchmentPalette.Burgundy);
        private static readonly Color BlockedInk = KingmakerUiFactory.ToColor(PlannerParchmentPalette.BlockedInk, 0.95f);
        private static readonly Color LegalInk = KingmakerUiFactory.ToColor(PlannerParchmentPalette.LegalInk, 0.95f);
        // Native UI finish (UI-04): fine ink lines; the selected one heavier.
        private const float LineThickness = 1.5f;
        private const float SelectedLineThickness = 3f;
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
        // 0.4.2: the full-screen blocker showing the native table.
        private ParchmentBackdrop _backdrop;
        private int _uiLayer;
        private bool _disposed;
        private bool _importAnnounced;
        private float _reloadArmedUntil;
        private CastingGraphView _lastView;
        private string _nextRefreshCause;
        private double? _nextCommandAndPersistenceMilliseconds;
        private long? _nextInteractionStartedAt;
        private int _refreshSequence;
        private FirstCorrectFrameProbe _firstCorrectFrameProbe;
        private GraphLayoutResult _lastLayout;
        private GraphLayoutMetrics _lastMetrics;

        // Native UI finish (UI-03/UI-11): the game's own typography and paper
        // kit for every text and control the view draws.
        private PlannerNativeKit _kit;
        // Header.
        private TMP_Text _title;
        private TMP_Text _headerStatus;
        private RectTransform _routineBar;
        // Catalogue.
        private TMP_InputField _buffSearch;
        private RectTransform _catalogueContent;
        private string _buffQuery = string.Empty;
        private PlannerSourceCategory _sourceCategory = PlannerSourceCategory.All;
        private readonly Dictionary<PlannerSourceCategory, Button> _categoryTabs =
            new Dictionary<PlannerSourceCategory, Button>();
        private const float SourceTabCaptionMax = 14f;
        private const float SourceTabCaptionMin = 9f;
        private readonly List<TMP_Text> _sourceTabCaptions = new List<TMP_Text>();
        private float _sourceTabsWidth = -1f;
        private string _sourceTabFit = "not-laid-out";
        // Graph.
        private Image _bannerIcon;
        private TMP_Text _bannerTitle;
        private TMP_Text _bannerDetail;
        private TMP_Text _guidance;
        private ScrollRect _graphScroll;
        private ParchmentRule _headerRule;
        private RectTransform _graphViewport;
        private RectTransform _graphContent;
        // Inspector.
        private RectTransform _inspectorContent;
        private TMP_Text _inspectorTitle;
        private int _sectionCount;
        private ScrollRect _inspectorScroll;
        private bool _showedProblems;
        private Vector2 _laidOutViewportSize;
        internal string ProblemRevealCastingIdForRuntime { get; private set; }
        internal bool ProblemChipVisibleBeforeRevealForRuntime { get; private set; }
        internal float ProblemGraphScrollBeforeRevealForRuntime { get; private set; }
        // Footer.
        private TMP_Text _footerResult;
        private TMP_Text _footerSave;
        private Image _footerLedger;
        private float _footerButtonsRight;
        private Button _footerRecovery;
        private string _shownSaveState;
        // True while the footer line shows a command's feedback (until the
        // next command); otherwise it shows Run's status.
        private bool _feedbackShown;
        private Button _resourcesButton;
        private RectTransform _resources;
        private RectTransform _resourcesDialog;
        private RectTransform _resourcesContent;
        private ScrollRect _resourcesScroll;
        private TMP_Text _resourcesSummary;
        private TMP_Text _resourcesSaveStatus;
        private Button _resourcesCloseButton;
        private Button _resourcesSelectedScopeButton;
        private Button _resourcesAllScopeButton;
        private WorkspaceResourceScope _resourceScope = WorkspaceResourceScope.SelectedRoutine;
        private Button _modeButton;
        private Button _policyButton;
        private Button _undoButton;
        private Button _runButton;
        // Native UI finish (UI-01): a right click shows the game's own
        // description window (NativeDescriptionPresenter), read-only: it
        // never authors, targets, casts or persists anything.
        private NativeDescriptionPresenter _description;
        private bool _descriptionShownLastFrame;
        private readonly Action<string> _log;

        // Canvas layout completion is not the player's first rendered frame.
        // Sample EndOfFrame after the synchronously rebuilt view so the live
        // evidence can retain the separate stage timings and also measure
        // input-to-first-correct-rendered-frame latency.
        private sealed class FirstCorrectFrameProbe : MonoBehaviour
        {
            internal void Arm(long startedAt, int sequence, string cause,
                Action<string> log)
            {
                StopAllCoroutines();
                StartCoroutine(Observe(startedAt, sequence, cause, log));
            }

            private static IEnumerator Observe(long startedAt, int sequence,
                string cause, Action<string> log)
            {
                yield return new WaitForEndOfFrame();
                double milliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - startedAt) *
                    1000.0 / System.Diagnostics.Stopwatch.Frequency;
                log("[KBP-WORKSPACE-PERF] sequence=" + sequence + ";cause=" + cause +
                    ";inputToFirstCorrectRenderedFrameMs=" + milliseconds.ToString("F3") +
                    ";renderedFrame=" + Time.frameCount + ".");
            }
        }

        internal CastingWorkspaceScreenView(
            StaticCanvas nativeCanvas,
            CastingWorkspaceSession session,
            Func<CastingWorkspaceInputs> inputsProvider,
            Func<CastingWorkspaceInputs> freshInputsProvider,
            Action close,
            Action switchToClassic = null,
            Action<string> log = null)
        {
            _log = log ?? (value => { });
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

        // The footer's result line as the player reads it (runtime evidence).
        internal string FooterResultTextForRuntime
        {
            get { return _footerResult == null ? null : _footerResult.text; }
        }

        internal string WorkspacePaperEvidenceForRuntime
        {
            get { return _nativeTheme == null ? "parchment=unavailable" : _nativeTheme.ParchmentEvidence; }
        }

        // The last rendered read model and geometry (runtime evidence only).
        internal CastingGraphView LastGraphForRuntime { get { return _lastView; } }
        internal GraphLayoutResult LastLayoutForRuntime { get { return _lastLayout; } }
        internal GraphLayoutMetrics LastMetricsForRuntime { get { return _lastMetrics; } }

        // Human-feedback refinement runtime evidence. These reads expose the
        // rendered modal's observable state without rebuilding it or changing
        // the plan; screenshots remain the authority for appearance.
        internal bool ResourcesOpenForRuntime
        {
            get { return _resources != null && _resources.gameObject.activeInHierarchy; }
        }

        internal bool ResourcesTopmostForRuntime
        {
            get
            {
                return ResourcesOpenForRuntime && _resources.parent != null &&
                    _resources.GetSiblingIndex() == _resources.parent.childCount - 1;
            }
        }

        internal bool ResourcesCloseFocusedForRuntime
        {
            get
            {
                return ResourcesOpenForRuntime && EventSystem.current != null &&
                    _resourcesCloseButton != null &&
                    EventSystem.current.currentSelectedGameObject == _resourcesCloseButton.gameObject;
            }
        }

        internal bool ResourcesScrollClipsForRuntime
        {
            get
            {
                if (!ResourcesOpenForRuntime || _resourcesScroll == null ||
                    _resourcesScroll.viewport == null || _resourcesScroll.content == null) return false;
                RectTransform viewport = _resourcesScroll.viewport;
                RectMask2D rectMask = viewport.GetComponent<RectMask2D>();
                Mask mask = viewport.GetComponent<Mask>();
                return (rectMask != null && rectMask.enabled) || (mask != null && mask.enabled);
            }
        }

        internal float ResourcesScrollOverflowForRuntime
        {
            get
            {
                if (_resourcesScroll == null || _resourcesScroll.viewport == null ||
                    _resourcesScroll.content == null) return -1f;
                return Mathf.Max(0f, _resourcesScroll.content.rect.height -
                    _resourcesScroll.viewport.rect.height);
            }
        }

        internal Vector2? ResourcesPanelScreenSizeForRuntime
        {
            get
            {
                if (!ResourcesOpenForRuntime || _resourcesDialog == null) return null;
                Canvas canvas = _resourcesDialog.GetComponentInParent<Canvas>();
                Camera camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null : canvas.worldCamera;
                var corners = new Vector3[4];
                _resourcesDialog.GetWorldCorners(corners);
                Vector2 lower = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 upper = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                if (lower.x < 0f || lower.y < 0f || upper.x > Screen.width || upper.y > Screen.height)
                    return null;
                return new Vector2(upper.x - lower.x, upper.y - lower.y);
            }
        }

        internal string ResourcesSummaryForRuntime
        {
            get { return _resourcesSummary == null ? null : _resourcesSummary.text; }
        }

        internal string ResourcesSaveStatusForRuntime
        {
            get { return _resourcesSaveStatus == null ? null : _resourcesSaveStatus.text; }
        }

        internal string ResourcesScopeForRuntime
        {
            get { return _resourceScope.ToString(); }
        }

        // Actual accounting cells which intersect the masked viewport after
        // layout. Row identity is retained so runtime evidence can prove a
        // useful pool row, not merely the static column headings or summary.
        internal string[] ResourcesVisibleAccountingCellsForRuntime
        {
            get
            {
                if (!ResourcesOpenForRuntime || _resourcesContent == null ||
                    _resourcesScroll == null || _resourcesScroll.viewport == null)
                    return new string[0];
                Canvas.ForceUpdateCanvases();
                Rect viewport = _resourcesScroll.viewport.rect;
                return _resourcesContent.GetComponentsInChildren<TMP_Text>(false)
                    .Where(text => IsAccountingCell(text) && text.isActiveAndEnabled &&
                        text.canvasRenderer != null && !text.canvasRenderer.cull &&
                        BoundsOverlap(RectTransformUtility.CalculateRelativeRectTransformBounds(
                            _resourcesScroll.viewport, text.rectTransform), viewport) &&
                        !string.IsNullOrWhiteSpace(text.text))
                    .Select(text => text.transform.parent.name + "/" + text.name + "=" + text.text)
                    .ToArray();
            }
        }

        internal bool ResourcesAccountingUsesNativeLayerForRuntime
        {
            get
            {
                if (!ResourcesOpenForRuntime || _resourcesContent == null) return false;
                TMP_Text[] cells = _resourcesContent.GetComponentsInChildren<TMP_Text>(false)
                    .Where(IsAccountingCell).ToArray();
                return cells.Length != 0 && cells.All(text => text.gameObject.layer == _uiLayer &&
                    text.transform.parent != null && text.transform.parent.gameObject.layer == _uiLayer);
            }
        }

        private static bool IsAccountingCell(TMP_Text text)
        {
            if (text == null || text.transform.parent == null) return false;
            string row = text.transform.parent.name ?? string.Empty;
            return row == "Columns" || row.StartsWith("Pool.", StringComparison.Ordinal);
        }

        private static bool BoundsOverlap(Bounds bounds, Rect rect)
        {
            return bounds.max.x > rect.xMin && bounds.min.x < rect.xMax &&
                bounds.max.y > rect.yMin && bounds.min.y < rect.yMax;
        }

        internal string AvailabilityPolicyCaptionForRuntime
        {
            get { return ButtonCaption(_policyButton); }
        }

        internal string RunCaptionForRuntime
        {
            get { return ButtonCaption(_runButton); }
        }

        private static string ButtonCaption(Button button)
        {
            TMP_Text caption = button == null ? null : button.GetComponentInChildren<TMP_Text>(true);
            return caption == null ? null : caption.text;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_description != null) _description.Dispose();
            _description = null;
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            _root = null;
        }

        // Escape leaves the focused casting (its inspector) first; returns
        // false when there was nothing to leave, so the caller closes.
        internal bool HandleEscape()
        {
            if (_disposed) return false;
            // The native description is the innermost window: Escape closes
            // it first, leaving the planner exactly as it was. Shown at the
            // end of the last frame counts too: the game's own Escape
            // manager may have closed it earlier in this frame, and one
            // press never closes both it and the planner.
            bool described = NativeDescriptionPresenter.CloseIfOpen() || _descriptionShownLastFrame;
            _descriptionShownLastFrame = false;
            if (described) return true;
            // Then the Resources details, if shown.
            if (_resources != null && _resources.gameObject.activeSelf)
            {
                CloseResources();
                return true;
            }
            if (_session.EditingFocusCastingId == null) return false;
            _session.ClearGraphFocus();
            RefreshView();
            return true;
        }

        // Called once at the end of every frame's planner tick.
        internal void NoteFrameEnd()
        {
            _descriptionShownLastFrame = NativeDescriptionPresenter.IsOpen;
            FitSourceTabs();
        }

        // The source tabs follow the lane's width (SourceTabLayout); laid out
        // again only when that width changes.
        private void FitSourceTabs()
        {
            if (_sourceTabCaptions.Count == 0 || _sourceTabCaptions.Count != _categoryTabs.Count) return;
            RectTransform lane = _sourceTabCaptions[0].rectTransform.parent == null ? null
                : _sourceTabCaptions[0].rectTransform.parent.parent as RectTransform;
            float width = lane == null ? 0f : lane.rect.width;
            if (width <= 0f || Mathf.Abs(width - _sourceTabsWidth) < 0.5f) return;
            _sourceTabsWidth = width;
            var measured = new float[_sourceTabCaptions.Count];
            for (int index = 0; index < measured.Length; index++)
            {
                TMP_Text caption = _sourceTabCaptions[index];
                caption.fontSize = SourceTabCaptionMax;
                measured[index] = Mathf.Max(1f, caption.GetPreferredValues(caption.text,
                    float.PositiveInfinity, float.PositiveInfinity).x);
            }
            SourceTabLayout layout = SourceTabLayout.Compute(width, measured, SourceTabCaptionMax, SourceTabCaptionMin);
            var parts = new List<string>();
            for (int index = 0; index < measured.Length; index++)
            {
                TMP_Text caption = _sourceTabCaptions[index];
                caption.fontSize = layout.CaptionSize;
                RectTransform tab = caption.rectTransform.parent as RectTransform;
                if (tab != null)
                {
                    tab.anchoredPosition = new Vector2(layout.Lefts[index], tab.anchoredPosition.y);
                    tab.sizeDelta = new Vector2(layout.Widths[index], tab.sizeDelta.y);
                }
                parts.Add(caption.text + ":" + Invariant(measured[index] * layout.CaptionSize / SourceTabCaptionMax) +
                    "/" + Invariant(layout.Widths[index] - 2f * SourceTabLayout.CaptionMargin));
            }
            _sourceTabFit = "size=" + Invariant(layout.CaptionSize) + ";fits=" + layout.Fits + ";row=" +
                Invariant(width) + ";" + string.Join(";", parts.ToArray());
        }

        private static string Invariant(float value)
        {
            return value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        // Runtime evidence: the source tabs' shared caption size and, per
        // tab, the caption's width against the room inside its margins.
        internal string SourceTabFitForRuntime
        {
            get { return _sourceTabFit; }
        }

        // Read-only observations of the laid-out controls, not a forced state.
        internal TMP_Text[] SourceTabCaptionsForRuntime
        {
            get { return _sourceTabCaptions.ToArray(); }
        }

        internal string[] CatalogueSourceIdsForRuntime
        {
            get
            {
                if (_catalogueContent == null) return new string[0];
                return _catalogueContent.Cast<Transform>()
                    .Where(child => child.gameObject.activeInHierarchy &&
                        child.name.StartsWith("Source.", StringComparison.Ordinal))
                    .Select(child => child.name.Substring("Source.".Length)).ToArray();
            }
        }

        internal string SpellInspectCasterForRuntime
        {
            get { return _description == null ? null : _description.LastCasterUnitId; }
        }

        internal string SpellInspectAbilityForRuntime
        {
            get { return _description == null ? null : _description.LastAbilityGuid; }
        }

        internal void ShowNotice(string text)
        {
            Feedback(text, true);
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
            long totalStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            long interactionStarted = _nextInteractionStartedAt ?? totalStarted;
            _nextInteractionStartedAt = null;
            int sequence = ++_refreshSequence;
            string cause = _lastView == null ? "cold-open"
                : (_nextRefreshCause ?? "warm-refresh");
            _nextRefreshCause = null;
            double commandAndPersistenceMilliseconds =
                _nextCommandAndPersistenceMilliseconds ?? 0d;
            _nextCommandAndPersistenceMilliseconds = null;
            PlaceHeaderRule();
            float cataloguePosition = CatalogueScroll() == null ? 1f
                : CatalogueScroll().verticalNormalizedPosition;
            float inspectorPosition = _inspectorScroll == null ? 1f
                : _inspectorScroll.verticalNormalizedPosition;
            long stageStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            CastingWorkspaceInputs inputs = _inputs();
            double inputMilliseconds = ElapsedMilliseconds(stageStarted);
            stageStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            CastingGraphView view = _session.BuildGraph(inputs);
            double calculationMilliseconds = ElapsedMilliseconds(stageStarted);
            stageStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            _session.PresentForReview(inputs);
            double reviewMilliseconds = ElapsedMilliseconds(stageStarted);
            _lastView = view;
            if (!_importAnnounced && _footerResult != null)
            {
                _importAnnounced = true;
                string import = PersistenceMessages.ForCastingLoad(_session.LoadStatus,
                    _session.PrimaryPlanFileExists, _session.LoadSourcePath, _session.LoadWarning) ??
                    DescribeImport(_session);
                if (import != null) Feedback(import);
            }
            stageStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            RebuildHeader(view);
            RebuildCatalogue(view);
            RebuildBanner(view);
            RebuildGraph(view);
            RebuildInspector(view);
            RebuildFooter(view);
            double regionMilliseconds = ElapsedMilliseconds(stageStarted);
            // Borrowed native artwork, fonts and the click sound reach the
            // rebuilt controls; every control is pointer-highlighted only.
            stageStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            if (_nativeTheme != null) _nativeTheme.ApplyTo(_root);
            KingmakerUiFactory.OwnPointerHighlights(_root);
            PropagateUiLayer();
            double themeMilliseconds = ElapsedMilliseconds(stageStarted);
            stageStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            Canvas.ForceUpdateCanvases();
            if (CatalogueScroll() != null) CatalogueScroll().verticalNormalizedPosition = cataloguePosition;
            if (_inspectorScroll != null) _inspectorScroll.verticalNormalizedPosition = inspectorPosition;
            RevealPendingProblem();
            double layoutMilliseconds = ElapsedMilliseconds(stageStarted);
            _log("[KBP-WORKSPACE-PERF] sequence=" + sequence + ";cause=" + cause +
                ";commandAndPersistenceMs=" +
                    commandAndPersistenceMilliseconds.ToString("F3") +
                ";inputMs=" + inputMilliseconds.ToString("F3") +
                ";calculationMs=" + calculationMilliseconds.ToString("F3") +
                ";reviewMs=" + reviewMilliseconds.ToString("F3") +
                ";regionsMs=" + regionMilliseconds.ToString("F3") +
                ";themeMs=" + themeMilliseconds.ToString("F3") +
                ";layoutMs=" + layoutMilliseconds.ToString("F3") +
                ";presentedRefreshMs=" + ElapsedMilliseconds(totalStarted).ToString("F3"));
            if (_firstCorrectFrameProbe != null)
                _firstCorrectFrameProbe.Arm(interactionStarted, sequence, cause, _log);
        }

        private static double ElapsedMilliseconds(long startedAt)
        {
            return (System.Diagnostics.Stopwatch.GetTimestamp() - startedAt) * 1000.0 /
                System.Diagnostics.Stopwatch.Frequency;
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
                    _inspectorContent.GetComponentsInChildren<TMP_Text>()
                        .Where(text => ScreenCentre(text.rectTransform) != null)
                        .Select(text => text.text).ToArray());
            }
        }

        internal float InspectorScrollPositionForRuntime
        {
            get { return _inspectorScroll == null ? -1f : _inspectorScroll.verticalNormalizedPosition; }
        }

        internal string FooterSaveTextForRuntime
        {
            get { return _footerSave == null ? null : _footerSave.text; }
        }

        internal bool SaveRecoveryShownForRuntime
        {
            get { return _footerRecovery != null && _footerRecovery.gameObject.activeInHierarchy; }
        }

        internal string FooterResultForRuntime
        {
            get { return _footerResult == null ? string.Empty : _footerResult.text; }
        }

        // Native UI finish evidence: the window title and every label the
        // player can see now (legacy and TextMeshPro text alike).
        internal string TitleTextForRuntime
        {
            // As read: the ornamental initial's markup is not part of it.
            get { return _title == null ? null : _title.GetParsedText(); }
        }

        internal IList<string> VisibleLabelsForRuntime()
        {
            var labels = new List<string>();
            if (_root == null) return labels;
            foreach (Text text in _root.GetComponentsInChildren<Text>(false))
                if (text != null && text.enabled && !string.IsNullOrEmpty(text.text)) labels.Add(text.text);
            foreach (TMPro.TMP_Text text in _root.GetComponentsInChildren<TMPro.TMP_Text>(false))
                if (text != null && text.enabled && !string.IsNullOrEmpty(text.text)) labels.Add(text.text);
            return labels;
        }

        // The right-click read of a part ("tile:<sourceId>", "chip:<id>",
        // "banner"), exactly as a right mouse click on it delivers it.
        internal bool RightClickForRuntime(string part)
        {
            RectTransform rect = null;
            if (part == "banner") rect = _bannerIcon == null ? null : RectOf(_bannerIcon);
            else if (part != null && part.StartsWith("tile:", StringComparison.Ordinal) && _catalogueContent != null)
                rect = _catalogueContent.Find("Source." + part.Substring(5)) as RectTransform;
            else if (part != null && part.StartsWith("chip:", StringComparison.Ordinal) && _graphContent != null)
                rect = FindGraphPart("Casting." + part.Substring(5));
            RightClickProxy proxy = rect == null ? null : rect.GetComponent<RightClickProxy>();
            if (proxy == null || proxy.OnRightClick == null) return false;
            proxy.OnRightClick();
            return true;
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

        // Runtime evidence only: the search focused (or left) as a click
        // on it focuses it.
        internal void FocusSearchForRuntime(bool focus)
        {
            if (_buffSearch == null || EventSystem.current == null) return;
            if (focus)
            {
                EventSystem.current.SetSelectedGameObject(_buffSearch.gameObject);
                _buffSearch.ActivateInputField();
                return;
            }
            _buffSearch.DeactivateInputField();
            if (EventSystem.current.currentSelectedGameObject == _buffSearch.gameObject)
                EventSystem.current.SetSelectedGameObject(null);
        }

        // Owned probe cleanup uses the real view binding and releases text focus.
        internal void RestoreSearchForRuntime(string text)
        {
            if (_buffSearch != null) _buffSearch.text = text ?? string.Empty;
            FocusSearchForRuntime(false);
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
                TMP_Text text = label == null ? null : label.GetComponent<TMP_Text>();
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
            _firstCorrectFrameProbe = _root.gameObject.AddComponent<FirstCorrectFrameProbe>();
            _description = new NativeDescriptionPresenter(canvas, _root, _log);
            CanvasGroup group = _root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            KingmakerUiFactory.Stretch(_root);
            _nativeTheme = PlannerNativeThemeSurface.Attach(_root, nativeCanvas);
            _kit = PlannerNativeKit.Resolve(nativeCanvas);
            // The native UI camera culls by layer: every owned object carries
            // the native canvas's layer (factory objects default to 0).
            _uiLayer = nativeCanvas.gameObject.layer;
            _root.gameObject.layer = _uiLayer;
            RectTransform blocker = KingmakerUiFactory.CreateRect("Blocker", _root);
            KingmakerUiFactory.AddPanel(blocker, new Color(0f, 0f, 0f, 0.55f));
            KingmakerUiFactory.Stretch(blocker);
            // 0.4.2: the blocker shows the native table the game's own
            // windows lie on (its dimming tint is the exact fallback).
            _backdrop = new ParchmentBackdrop(blocker.GetComponent<Image>());
            _nativeTheme.RegisterBackdrop(_backdrop);
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
                ParchmentSurfaces.WorkspaceFrameOutsets, ParchmentSurfaces.WorkspaceFrameSheetOutsets,
                ParchmentSurfaces.FrameSheetUnitsPerTexel);
            BuildHeader(_frame);
            BuildCatalogue(_frame);
            BuildGraphArea(_frame);
            BuildInspector(_frame);
            BuildFooter(_frame);
            ApplyScrollPaper();
            foreach (ScrollRect lane in new[] { CatalogueScroll(), _graphScroll, _inspectorScroll })
                NativeScrollbar(lane);
            PropagateUiLayer();
        }

        // The game's own scrollbar art (the combat log's slider track and
        // handle, UI-11); the factory colours stay as the fallback.
        private void NativeScrollbar(ScrollRect scroll)
        {
            Scrollbar bar = scroll == null ? null : scroll.verticalScrollbar;
            Sprite track = _kit.Sprite("SliderArea");
            Sprite handle = _kit.Sprite("ScrollBar_Big_Handler");
            if (bar == null || track == null || handle == null) return;
            Image trackImage = bar.GetComponent<Image>();
            if (trackImage != null)
            {
                trackImage.sprite = track;
                trackImage.type = Image.Type.Sliced;
                trackImage.color = Color.white;
            }
            Image handleImage = bar.handleRect == null ? null : bar.handleRect.GetComponent<Image>();
            if (handleImage != null)
            {
                handleImage.sprite = handle;
                handleImage.type = Image.Type.Sliced;
                handleImage.color = Color.white;
            }
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
                    _frameParchment.AddWash(lane.GetComponent<Image>(), PlannerParchmentPalette.WellWashAlpha,
                        PlannerParchmentPalette.SheetWellWashAlpha);
            _frameParchment.AddWash(_footerLedger, PlannerParchmentPalette.LedgerWashAlpha,
                PlannerParchmentPalette.SheetLedgerWashAlpha);
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
            get { return NativeDescriptionPresenter.IsOpen; }
        }

        internal void CloseSpellInspect()
        {
            NativeDescriptionPresenter.CloseIfOpen();
        }

        // Runtime evidence only: the native description of an ability, as a
        // right click on a part showing it opens it.
        internal bool OpenDescriptionForRuntime(AbilityKey ability, bool exactVariant, string casterUnitId)
        {
            CloseResources();
            return _description != null && _description.Open(ability, exactVariant, casterUnitId);
        }

        // Runtime layout probe only: a long text in the same native window.
        internal bool OpenDescriptionTextForRuntime(string title, string text)
        {
            CloseResources();
            return _description != null && _description.OpenText(title, text);
        }

        // What the open spell scroll drew and its input policy (runtime
        // evidence only).
        // What the planner's backdrop drew (runtime evidence only).
        internal string BackdropEvidenceForRuntime
        {
            get { return _backdrop == null ? "backdrop=not-built" : _backdrop.Evidence; }
        }

        internal string SpellInspectPaperEvidenceForRuntime
        {
            get
            {
                return _description == null ? "description=not-built" : "description=native-description-window;" +
                    _description.LiftEvidence + ";subject=" + (_description.LastSubject ?? "none") +
                    (_description.LastFailure == null ? string.Empty : ";failure=" + _description.LastFailure);
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
                RectTransform panel = NativeDescriptionPresenter.Panel;
                if (!SpellInspectOpen || panel == null) return null;
                Canvas canvas = panel.GetComponentInParent<Canvas>();
                Camera camera = canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null : canvas.rootCanvas.worldCamera;
                var corners = new Vector3[4];
                panel.GetWorldCorners(corners);
                Vector2 low = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 high = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                if (low.x < 0f || low.y < 0f || high.x > Screen.width || high.y > Screen.height) return Vector2.zero;
                return high - low;
            }
        }

        internal Vector2? SpellInspectPanelCentreForRuntime
        {
            get { return SpellInspectOpen ? ScreenCentre(NativeDescriptionPresenter.Panel) : null; }
        }

        internal string SpellInspectTitleForRuntime
        {
            get { return SpellInspectOpen ? NativeDescriptionPresenter.Title : null; }
        }

        internal string SpellInspectBodyForRuntime
        {
            get { return SpellInspectOpen ? NativeDescriptionPresenter.Body : null; }
        }

        internal float SpellInspectOverflowForRuntime
        {
            get
            {
                ScrollRect scroll = NativeDescriptionPresenter.Scroll;
                if (!SpellInspectOpen || scroll == null || scroll.content == null || scroll.viewport == null) return 0f;
                return scroll.content.rect.height - scroll.viewport.rect.height;
            }
        }

        internal float? SpellInspectScrollPositionForRuntime
        {
            get
            {
                ScrollRect scroll = NativeDescriptionPresenter.Scroll;
                return SpellInspectOpen && scroll != null ? scroll.verticalNormalizedPosition : (float?)null;
            }
        }

        // Wires right-click on one control to show the ability's native
        // description: the caster's own spell or ability when the control
        // stands for a casting or a chosen caster/source, else the base
        // spell. The plan is never changed.
        private void AddSpellInspect(Component control, System.Func<CastingGraphView> view,
            AbilityKey ability, bool exactVariant, string casterUnitId, string name)
        {
            if (control == null) return;
            RightClickProxy proxy = control.GetComponent<RightClickProxy>();
            if (proxy == null) proxy = control.gameObject.AddComponent<RightClickProxy>();
            RightClickProxy captured = proxy;
            captured.OnRightClick = () =>
            {
                CloseResources();
                _lastView = view();
                if (_description == null || !_description.Open(ability, exactVariant, casterUnitId))
                    ShowNotice("No description is available for " + (string.IsNullOrEmpty(name) ? "this buff" : name) + ".");
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
            // Row 1, on the paper: the window's title (UI-07: "Buff Planner",
            // with the native ornamental initial, and no mode label), the
            // selected routine's count, Close.
            RectTransform header = KingmakerUiFactory.CreateRect("Header", frame);
            KingmakerUiFactory.SetAnchors(header, 0f, 1f, 1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(0f, 48f);
            header.anchoredPosition = new Vector2(0f, 2f);
            _title = _kit.Text(header, "Title", "Buff Planner", PlannerTextRole.Title, 28f,
                TextAlignmentOptions.MidlineLeft);
            _title.enableWordWrapping = false;
            KingmakerUiFactory.SetAnchors(_title.rectTransform, 0f, 0f, 0.5f, 1f, 22f, 0f, 0f, 0f);
            _headerStatus = _kit.Text(header, "Status", string.Empty, PlannerTextRole.Secondary, 16f,
                TextAlignmentOptions.MidlineRight);
            _headerStatus.enableWordWrapping = false;
            KingmakerUiFactory.SetAnchors(_headerStatus.rectTransform, 0.4f, 0f, 1f, 1f, 0f, 126f, 0f, 0f);
            Button close = _kit.Button(header, "Close", "Close", () => _close(), PlannerTextRole.Button, 17f);
            RectTransform closeRect = RectOf(close);
            KingmakerUiFactory.SetAnchors(closeRect, 1f, 0f, 1f, 1f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(106f, -8f);
            closeRect.anchoredPosition = new Vector2(-8f, 0f);
            // Row 2, on the book's top edge: the routines.
            _routineBar = KingmakerUiFactory.CreateRect("RoutineBar", frame);
            KingmakerUiFactory.SetAnchors(_routineBar, 0f, 0.900f, 1f, ParchmentHeaderRule.RoutineBarTopAnchor,
                52f, 52f, 0f, 0f);
        }

        private void BuildCatalogue(RectTransform frame)
        {
            RectTransform lane = KingmakerUiFactory.CreateRect("Catalogue", frame);
            KingmakerUiFactory.SetAnchors(lane, 0.028f, 0.085f, 0.163f, 0.895f);
            TextMeshProUGUI title = _kit.Text(lane, "LaneTitle", "Buffs", PlannerTextRole.Heading, 19f,
                TextAlignmentOptions.MidlineLeft);
            KingmakerUiFactory.SetAnchors(title.rectTransform, 0f, 1f, 1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(0f, 22f);
            // The one gesture no control shows (addendum 6).
            TextMeshProUGUI hint = _kit.Text(lane, "InspectHint", "Right-click a buff for its description",
                PlannerTextRole.Small, 13f, TextAlignmentOptions.BottomLeft);
            KingmakerUiFactory.SetAnchors(hint.rectTransform, 0f, 0f, 1f, 0f);
            hint.rectTransform.pivot = new Vector2(0.5f, 0f);
            hint.rectTransform.sizeDelta = new Vector2(0f, 18f);
            _buffSearch = _kit.InputField(lane, "BuffSearch", "Search buffs…");
            RectTransform search = RectOf(_buffSearch);
            KingmakerUiFactory.SetAnchors(search, 0f, 1f, 1f, 1f);
            search.pivot = new Vector2(0.5f, 1f);
            search.sizeDelta = new Vector2(0f, 30f);
            search.anchoredPosition = new Vector2(0f, -24f);
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
            _sourceTabCaptions.Clear();
            for (int index = 0; index < categories.Length; index++)
            {
                PlannerSourceCategory captured = categories[index];
                Button tab = _kit.Button(lane, "SourceTab." + captured, captured.ToString(), () =>
                    {
                        _sourceCategory = captured;
                        if (_lastView != null)
                        {
                            RebuildCatalogue(_lastView);
                            KingmakerUiFactory.OwnPointerHighlights(_catalogueContent);
                            PropagateUiLayer();
                        }
                    }, PlannerTextRole.Button, 14f);
                // Placed by FitSourceTabs once the lane has its width: one
                // row, one caption size, each tab as wide as its caption needs.
                RectTransform rect = RectOf(tab);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(60f, 28f);
                rect.anchoredPosition = new Vector2(index * 62f, -58f);
                TMP_Text caption = tab.GetComponentInChildren<TMP_Text>(true);
                if (caption != null)
                {
                    caption.enableAutoSizing = false;
                    caption.fontSize = SourceTabCaptionMax;
                    KingmakerUiFactory.Stretch(caption.rectTransform, SourceTabLayout.CaptionMargin,
                        SourceTabLayout.CaptionMargin, 2f, 2f);
                    _sourceTabCaptions.Add(caption);
                }
                _categoryTabs[captured] = tab;
            }
            _sourceTabsWidth = -1f;
            ScrollRect scroll = KingmakerUiFactory.CreateScrollView("Scroll", lane, _theme,
                out _catalogueContent, 16f);
            KingmakerUiFactory.SetAnchors(RectOf(scroll), 0f, 0f, 1f, 1f, 0f, 0f, 20f, 92f);
            ContentSizeFitter fitter = _catalogueContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            VerticalLayoutGroup rows = _catalogueContent.GetComponent<VerticalLayoutGroup>();
            if (rows != null) rows.spacing = 4f;
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
            _kit.Frame(icon, PlannerNativeKit.ThinFrame, Color.white);
            _bannerTitle = _kit.Text(banner, "BuffName", string.Empty, PlannerTextRole.Strong, 24f,
                TextAlignmentOptions.TopLeft);
            _bannerTitle.enableWordWrapping = false;
            _bannerTitle.overflowMode = TextOverflowModes.Ellipsis;
            _bannerTitle.enableAutoSizing = true;
            _bannerTitle.fontSizeMin = 17f;
            _bannerTitle.fontSizeMax = 24f;
            KingmakerUiFactory.SetAnchors(_bannerTitle.rectTransform, 0f, 1f, 1f, 1f);
            _bannerTitle.rectTransform.pivot = new Vector2(0f, 1f);
            _bannerTitle.rectTransform.offsetMin = new Vector2(66f, -32f);
            _bannerTitle.rectTransform.offsetMax = new Vector2(-4f, 0f);
            _bannerDetail = _kit.Text(banner, "BuffDetail", string.Empty, PlannerTextRole.Secondary, 15f,
                TextAlignmentOptions.TopLeft);
            _bannerDetail.enableWordWrapping = false;
            _bannerDetail.overflowMode = TextOverflowModes.Ellipsis;
            KingmakerUiFactory.SetAnchors(_bannerDetail.rectTransform, 0f, 1f, 1f, 1f);
            _bannerDetail.rectTransform.offsetMin = new Vector2(66f, -52f);
            _bannerDetail.rectTransform.offsetMax = new Vector2(-4f, -32f);
            _guidance = _kit.Text(banner, "Guidance", string.Empty, PlannerTextRole.Strong, 16f,
                TextAlignmentOptions.TopLeft);
            _guidance.color = PlannerNativeKit.HeadingInk;
            _guidance.enableWordWrapping = false;
            _guidance.overflowMode = TextOverflowModes.Ellipsis;
            KingmakerUiFactory.SetAnchors(_guidance.rectTransform, 0f, 1f, 1f, 1f);
            _guidance.rectTransform.offsetMin = new Vector2(66f, -74f);
            _guidance.rectTransform.offsetMax = new Vector2(-4f, -52f);
            // Lane captions (UI-07: short headings).
            RectTransform captions = KingmakerUiFactory.CreateRect("LaneCaptions", area);
            KingmakerUiFactory.SetAnchors(captions, 0f, 1f, 1f, 1f);
            captions.pivot = new Vector2(0.5f, 1f);
            captions.sizeDelta = new Vector2(0f, 24f);
            captions.anchoredPosition = new Vector2(0f, -78f);
            AddLaneCaption(captions, "CasterCaption", "Caster & source", 0f, 0.36f);
            AddLaneCaption(captions, "CastingCaption", "Castings", 0.36f, 0.76f);
            AddLaneCaption(captions, "TargetCaption", "Recipients", 0.76f, 1f);
            _graphScroll = KingmakerUiFactory.CreateScrollView("Scroll", area, _theme, out _graphContent, 16f);
            KingmakerUiFactory.SetAnchors(RectOf(_graphScroll), 0f, 0f, 1f, 1f, 0f, 0f, 0f, 104f);
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
            TextMeshProUGUI text = _kit.Text(parent, name, caption, PlannerTextRole.Heading, 18f,
                TextAlignmentOptions.MidlineLeft);
            text.enableWordWrapping = false;
            KingmakerUiFactory.SetAnchors(text.rectTransform, from, 0f, to, 1f, 6f, 4f, 0f, 0f);
        }

        private void BuildInspector(RectTransform frame)
        {
            RectTransform lane = KingmakerUiFactory.CreateRect("Inspector", frame);
            KingmakerUiFactory.SetAnchors(lane, 0.752f, 0.085f, 0.972f, 0.895f);
            _inspectorTitle = _kit.Text(lane, "LaneTitle", "New casting", PlannerTextRole.Heading, 19f,
                TextAlignmentOptions.MidlineLeft);
            KingmakerUiFactory.SetAnchors(_inspectorTitle.rectTransform, 0f, 1f, 1f, 1f);
            _inspectorTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            _inspectorTitle.rectTransform.sizeDelta = new Vector2(0f, 24f);
            ScrollRect scroll = KingmakerUiFactory.CreateScrollView("Scroll", lane, _theme,
                out _inspectorContent, 16f);
            _inspectorScroll = scroll;
            KingmakerUiFactory.SetAnchors(RectOf(scroll), 0f, 0f, 1f, 1f, 0f, 0f, 0f, 28f);
            ContentSizeFitter fitter = _inspectorContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            VerticalLayoutGroup layout = _inspectorContent.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 4f;
                layout.padding = new RectOffset(8, 8, 6, 10);
            }
        }

        // Native UI finish (UI-08): a compact execution footer - the save
        // state, one line beside Run (what a run does now or exactly why it
        // cannot, or the last command's feedback), and Resources, Mode, Undo
        // and Run. The budgets moved into the Resources details.
        private void BuildFooter(RectTransform frame)
        {
            RectTransform footer = KingmakerUiFactory.CreateRect("Footer", frame);
            KingmakerUiFactory.SetAnchors(footer, 0f, 0f, 1f, 0.078f, 52f, 44f, 6f, 0f);
            // Everyday-use v1.2 §2/§4/§5: autosave replaced Save (a passive
            // status shows it, refreshed with the view), the Run click
            // authorizes the current revision, and Reload appears - with
            // Retry save - only as the exceptional recovery action.
            const float SaveColumn = 112f;
            _footerSave = _kit.Text(footer, "SaveStatus", string.Empty, PlannerTextRole.Small, 14f,
                TextAlignmentOptions.MidlineLeft);
            KingmakerUiFactory.SetAnchors(_footerSave.rectTransform, 0f, 0.5f, 0f, 1f, 0f, 0f, 0f, 0f);
            _footerSave.rectTransform.offsetMax = new Vector2(SaveColumn - 8f, 0f);
            _footerRecovery = _kit.Button(footer, "SaveRecovery", WorkspaceFooterText.RetrySaveAction,
                () => Command(RecoveryCommand), PlannerTextRole.Button, 14f);
            KingmakerUiFactory.SetAnchors(RectOf(_footerRecovery), 0f, 0.04f, 0f, 0.5f);
            RectOf(_footerRecovery).offsetMax = new Vector2(SaveColumn - 8f, RectOf(_footerRecovery).offsetMax.y);
            _footerRecovery.gameObject.SetActive(false);
            string[] names = { "Resources", "AvailabilityPolicy", "ExecutionMode", "Undo", "Run" };
            string[] captions = { "Resources", PolicyCaption(), ModeCaption(), "Undo",
                WorkspaceFooterText.RunCaption(_session.RoutineDisplayName(_session.SelectedRoutineId)) };
            Action[] actions =
            {
                ToggleResources, ToggleAvailabilityPolicyCommand, ToggleModeCommand, UndoCommand,
                RunApply
            };
            float[] widths = { 128f, 168f, 154f, 88f, 176f };
            float right = 0f;
            for (int index = names.Length - 1; index >= 0; index--)
            {
                Action action = actions[index];
                Action invoke = names[index] == "Resources"
                    ? action : (Action)(() => Command(action));
                Button button = _kit.Button(footer, names[index], captions[index], invoke,
                    PlannerTextRole.Button,
                    names[index] == "Run" ? 19f : 16f);
                RectTransform rect = RectOf(button);
                KingmakerUiFactory.SetAnchors(rect, 1f, 0.12f, 1f, 0.88f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.sizeDelta = new Vector2(widths[index], 0f);
                rect.anchoredPosition = new Vector2(-right, 0f);
                right += widths[index] + 8f;
                if (names[index] == "Resources") _resourcesButton = button;
                if (names[index] == "AvailabilityPolicy") _policyButton = button;
                if (names[index] == "ExecutionMode") _modeButton = button;
                if (names[index] == "Undo") _undoButton = button;
                if (names[index] == "Run") _runButton = button;
            }
            // The one line starts after the save column and ends where the
            // buttons (and Ready Casts Only, when offered) begin.
            _footerButtonsRight = right;
            float textRight = right;
            _footerResult = _kit.Text(footer, "Result", string.Empty, PlannerTextRole.Body, 16f,
                TextAlignmentOptions.MidlineLeft);
            _footerResult.enableAutoSizing = true;
            _footerResult.fontSizeMin = 12f;
            _footerResult.fontSizeMax = 16f;
            _footerResult.overflowMode = TextOverflowModes.Ellipsis;
            KingmakerUiFactory.SetAnchors(_footerResult.rectTransform, 0f, 0f, 1f, 1f, SaveColumn, textRight, 0f, 0f);
            // The footer line's paper ground (washed onto the sheet).
            RectTransform ledger = KingmakerUiFactory.CreateRect("FooterLedger", footer);
            KingmakerUiFactory.SetAnchors(ledger, 0f, 0f, 1f, 1f, -8f, textRight - 8f, -2f, -2f);
            Image ground = KingmakerUiFactory.AddFramedPanel(ledger, _theme.ParchmentRaised, _theme.GoldAccent);
            ground.raycastTarget = false;
            ledger.SetAsFirstSibling();
            _footerLedger = ground;
            BuildResources(frame);
            ShowSaveState();
        }

        // The Resources details (UI-08): the selected routine considered on
        // its own beside the whole plan run without rest, as the existing
        // accounting states them, with that forecast's order and assumption.
        private void BuildResources(RectTransform frame)
        {
            _resources = KingmakerUiFactory.CreateRect("ResourceDetails", frame);
            KingmakerUiFactory.Stretch(_resources);
            RectTransform modalBlocker = KingmakerUiFactory.CreateRect("Blocker", _resources);
            KingmakerUiFactory.Stretch(modalBlocker);
            Image blocker = KingmakerUiFactory.AddPanel(modalBlocker,
                new Color(0.08f, 0.05f, 0.03f, 0.58f));
            blocker.raycastTarget = true;
            Button dismiss = modalBlocker.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = blocker;
            dismiss.onClick.AddListener(CloseResources);

            RectTransform dialog = KingmakerUiFactory.CreateRect("Dialog", _resources);
            _resourcesDialog = dialog;
            KingmakerUiFactory.SetAnchors(dialog, 0.10f, 0.08f, 0.90f, 0.92f);
            Image paper = _kit.Paint(dialog, PlannerNativeKit.PaperSlip, Color.white, true);
            if (paper == null)
                paper = KingmakerUiFactory.AddFramedPanel(dialog, _theme.ParchmentRaised,
                    _theme.GoldAccent);
            paper.color = Color.white;
            paper.raycastTarget = true;
            _kit.Frame(dialog, PlannerNativeKit.ThinFrame, Color.white);
            TextMeshProUGUI heading = _kit.Text(dialog, "Heading", "Resources", PlannerTextRole.Heading, 22f,
                TextAlignmentOptions.TopLeft);
            KingmakerUiFactory.SetAnchors(heading.rectTransform, 0f, 1f, 1f, 1f, 24f, 160f, 0f, 0f);
            heading.rectTransform.pivot = new Vector2(0.5f, 1f);
            heading.rectTransform.sizeDelta = new Vector2(-184f, 34f);
            heading.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            _resourcesCloseButton = _kit.Button(dialog, "Close", "Close", CloseResources,
                PlannerTextRole.Button, 16f);
            KingmakerUiFactory.SetAnchors(RectOf(_resourcesCloseButton), 1f, 1f, 1f, 1f);
            RectOf(_resourcesCloseButton).pivot = new Vector2(1f, 1f);
            RectOf(_resourcesCloseButton).sizeDelta = new Vector2(118f, 34f);
            RectOf(_resourcesCloseButton).anchoredPosition = new Vector2(-22f, -16f);

            _resourcesSelectedScopeButton = _kit.Button(dialog, "SelectedRoutineScope",
                "Selected routine", () => SetResourceScope(WorkspaceResourceScope.SelectedRoutine),
                PlannerTextRole.Button, 15f);
            KingmakerUiFactory.SetAnchors(RectOf(_resourcesSelectedScopeButton), 0f, 1f, 0f, 1f);
            RectOf(_resourcesSelectedScopeButton).pivot = new Vector2(0f, 1f);
            RectOf(_resourcesSelectedScopeButton).sizeDelta = new Vector2(190f, 34f);
            RectOf(_resourcesSelectedScopeButton).anchoredPosition = new Vector2(24f, -58f);
            _resourcesAllScopeButton = _kit.Button(dialog, "AllRoutinesScope",
                "All routines - no rest", () => SetResourceScope(WorkspaceResourceScope.AllRoutinesNoRest),
                PlannerTextRole.Button, 15f);
            KingmakerUiFactory.SetAnchors(RectOf(_resourcesAllScopeButton), 0f, 1f, 0f, 1f);
            RectOf(_resourcesAllScopeButton).pivot = new Vector2(0f, 1f);
            RectOf(_resourcesAllScopeButton).sizeDelta = new Vector2(220f, 34f);
            RectOf(_resourcesAllScopeButton).anchoredPosition = new Vector2(222f, -58f);

            _resourcesSummary = _kit.Text(dialog, "Summary", string.Empty, PlannerTextRole.Body, 15f,
                TextAlignmentOptions.TopLeft);
            _resourcesSummary.enableWordWrapping = true;
            _resourcesSummary.overflowMode = TextOverflowModes.Ellipsis;
            KingmakerUiFactory.SetAnchors(_resourcesSummary.rectTransform, 0f, 1f, 1f, 1f,
                24f, 24f, 0f, 0f);
            _resourcesSummary.rectTransform.pivot = new Vector2(0.5f, 1f);
            _resourcesSummary.rectTransform.sizeDelta = new Vector2(-48f, 48f);
            _resourcesSummary.rectTransform.anchoredPosition = new Vector2(0f, -98f);

            _resourcesScroll = KingmakerUiFactory.CreateScrollView("Accounting", dialog,
                _theme, out _resourcesContent, 16f);
            KingmakerUiFactory.Stretch(RectOf(_resourcesScroll), 22f, 22f, 54f, 152f);
            ContentSizeFitter fitter = _resourcesContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            NativeScrollbar(_resourcesScroll);
            _resourcesSaveStatus = _kit.Text(dialog, "SaveStatus", string.Empty,
                PlannerTextRole.Small, 13f, TextAlignmentOptions.MidlineLeft);
            _resourcesSaveStatus.enableWordWrapping = false;
            _resourcesSaveStatus.overflowMode = TextOverflowModes.Ellipsis;
            KingmakerUiFactory.SetAnchors(_resourcesSaveStatus.rectTransform,
                0f, 0f, 1f, 0f, 24f, 24f, 0f, 0f);
            _resourcesSaveStatus.rectTransform.pivot = new Vector2(0.5f, 0f);
            _resourcesSaveStatus.rectTransform.sizeDelta = new Vector2(-48f, 28f);
            _resourcesSaveStatus.rectTransform.anchoredPosition = new Vector2(0f, 18f);
            _resources.gameObject.SetActive(false);
        }

        private void ToggleResources()
        {
            if (_resources == null) return;
            if (_resources.gameObject.activeSelf)
            {
                CloseResources();
                return;
            }
            NativeDescriptionPresenter.CloseIfOpen();
            try { RebuildResources(); }
            catch (Exception exception)
            {
                Feedback("Resources could not be prepared: " + exception.Message, true);
                _log("[KBP-WORKSPACE] Resources refused: " + exception);
                return;
            }
            _resources.gameObject.SetActive(true);
            _resources.SetAsLastSibling();
            if (_resourcesButton != null) _kit.SetSelected(_resourcesButton, true);
            if (EventSystem.current != null && _resourcesCloseButton != null)
                EventSystem.current.SetSelectedGameObject(_resourcesCloseButton.gameObject);
        }

        private void CloseResources()
        {
            if (_resources == null || !_resources.gameObject.activeSelf) return;
            _resources.gameObject.SetActive(false);
            if (_resourcesButton != null) _kit.SetSelected(_resourcesButton, false);
            if (EventSystem.current != null && _resourcesButton != null)
                EventSystem.current.SetSelectedGameObject(_resourcesButton.gameObject);
        }

        private void SetResourceScope(WorkspaceResourceScope scope)
        {
            if (_resourceScope == scope) return;
            WorkspaceResourceScope prior = _resourceScope;
            _resourceScope = scope;
            try { RebuildResources(); }
            catch (Exception exception)
            {
                _resourceScope = prior;
                Feedback("Resources could not change scope: " + exception.Message, true);
                _log("[KBP-WORKSPACE] Resources scope refused: " + exception);
            }
        }

        private void RebuildResources()
        {
            if (_resourcesContent == null) return;
            WorkspaceResourceReport report = _session.BuildResourceReport(_inputs(), _resourceScope);
            _resourcesSummary.text = report.ScopeLabel + " · " + report.Summary;
            string saveState = _session.FooterSaveState;
            string saveProblem = WorkspaceFooterText.SaveProblem(saveState);
            string recovery = WorkspaceFooterText.RecoveryAction(saveState);
            _resourcesSaveStatus.text = saveProblem == null
                ? WorkspaceFooterText.SaveStatus(saveState)
                : saveProblem + (recovery == null ? string.Empty
                    : " Close Resources to " + recovery + ".");
            _resourcesSaveStatus.color = saveProblem == null
                ? PlannerNativeKit.SecondaryInk : BlockedInk;
            _kit.SetSelected(_resourcesSelectedScopeButton,
                _resourceScope == WorkspaceResourceScope.SelectedRoutine);
            _kit.SetSelected(_resourcesAllScopeButton,
                _resourceScope == WorkspaceResourceScope.AllRoutinesNoRest);
            float position = _resourcesScroll == null ? 1f
                : _resourcesScroll.verticalNormalizedPosition;
            KingmakerUiFactory.DestroyChildren(_resourcesContent);
            ResourceTableRow("Columns", "Resource", "Available now", "Saved need", "Would spend", "Short",
                PlannerTextRole.Strong, false);
            if (report.Groups.Count == 0)
            {
                TMP_Text empty = _kit.Text(_resourcesContent, "Empty",
                    "This scope has no resource-bearing castings.", PlannerTextRole.Body, 15f,
                    TextAlignmentOptions.TopLeft);
                KingmakerUiFactory.AddLayout(empty.rectTransform, 34f);
            }
            foreach (WorkspaceResourceGroup group in report.Groups)
            {
                TMP_Text heading = _kit.Text(_resourcesContent, "Group." + group.Key,
                    group.Label, PlannerTextRole.Heading, 17f, TextAlignmentOptions.MidlineLeft);
                KingmakerUiFactory.AddLayout(heading.rectTransform, 32f);
                foreach (WorkspaceResourceRow row in group.Rows)
                    ResourceTableRow("Pool." + row.Category + "." + row.PoolKey,
                        row.Label, row.AvailableText, row.NeededText,
                        row.WouldSpendText, row.ShortText, PlannerTextRole.Body,
                        row.ShortBy != 0);
            }
            Canvas.ForceUpdateCanvases();
            if (_resourcesScroll != null)
                _resourcesScroll.verticalNormalizedPosition = position;
        }

        private void ResourceTableRow(string name, string resource, string available,
            string needed, string spend, string shortBy, PlannerTextRole role, bool problem)
        {
            RectTransform row = KingmakerUiFactory.CreateRect(name, _resourcesContent);
            // CreateRect starts on Unity's default layer. These rows are
            // rebuilt after the one construction-time layer propagation, so
            // put each row on the native UI camera's layer before its cells
            // inherit the layer and render.
            row.gameObject.layer = _uiLayer;
            KingmakerUiFactory.AddLayout(row, 36f);
            ResourceCell(row, "Resource", resource, 0f, 0.46f, role, problem);
            ResourceCell(row, "AvailableNow", available, 0.46f, 0.60f, role, false);
            ResourceCell(row, "Needed", needed, 0.60f, 0.74f, role, false);
            ResourceCell(row, "Spend", spend, 0.74f, 0.88f, role, false);
            ResourceCell(row, "Short", shortBy, 0.88f, 1f, role, problem);
        }

        private void ResourceCell(RectTransform row, string name, string value,
            float from, float to, PlannerTextRole role, bool problem)
        {
            TextMeshProUGUI text = _kit.Text(row, name, value, role, 14f,
                from == 0f ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight);
            text.gameObject.layer = _uiLayer;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = 14f;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            if (problem) text.color = BlockedInk;
            KingmakerUiFactory.SetAnchors(text.rectTransform, from, 0f, to, 1f,
                from == 0f ? 4f : 5f, to == 1f ? 4f : 5f, 0f, 0f);
        }

        // The passive save status and its exceptional recovery action,
        // refreshed with every view rebuild (v1.2 §4: never "Saved" before
        // the current revision is durable). When the state turns into a
        // problem, the explanation replaces the footer line once.
        private void ShowSaveState()
        {
            if (_footerSave == null) return;
            string state = _session.FooterSaveState;
            string problem = WorkspaceFooterText.SaveProblem(state);
            _footerSave.text = WorkspaceFooterText.SaveStatus(state);
            _footerSave.color = problem == null ? PlannerNativeKit.SecondaryInk : BlockedInk;
            string action = WorkspaceFooterText.RecoveryAction(state);
            if (_footerRecovery != null)
            {
                _footerRecovery.gameObject.SetActive(action != null);
                if (action != null) PlannerNativeKit.SetCaption(_footerRecovery, action);
            }
            if (problem != null && !string.Equals(state, _shownSaveState, StringComparison.Ordinal))
                Feedback(problem, true);
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
            {
                bool saved = _session.RetryFailedSave();
                Feedback(saved ? "Saved: your latest edit is on disk."
                    : WorkspaceFooterText.SaveProblem(_session.FooterSaveState), !saved);
            }
        }

        // The footer's one line: a command's feedback (kept until the next
        // command; a problem in the blocked ink, an ordinary edit not).
        private void Feedback(string text, bool problem = false)
        {
            if (_footerResult == null || string.IsNullOrEmpty(text)) return;
            _footerResult.text = text;
            _footerResult.color = problem ? BlockedInk : PlannerNativeKit.BodyInk;
            _feedbackShown = true;
        }

        // ------------------------------------------------------------------
        // Rebuilds (on refresh only)
        // ------------------------------------------------------------------

        private void RebuildHeader(CastingGraphView view)
        {
            _headerStatus.text = WorkspaceHeaderText.Describe(view.SelectedRoutineName,
                view.RoutineCastingCount, view.RoutineReadyCount) +
                (!_session.SavesRefused ? string.Empty
                    : _session.LegacyImportBlocked ? " · not saved: the classic plan could not be imported"
                    : " · not saved: the plan file cannot be read");
            KingmakerUiFactory.DestroyChildren(_routineBar);
            TextMeshProUGUI label = _kit.Text(_routineBar, "RoutineLabel", "Routine", PlannerTextRole.Heading, 18f,
                TextAlignmentOptions.MidlineLeft);
            KingmakerUiFactory.SetAnchors(label.rectTransform, 0f, 0f, 0f, 1f);
            label.rectTransform.pivot = new Vector2(0f, 0.5f);
            label.rectTransform.sizeDelta = new Vector2(92f, 0f);
            float x = 96f;
            foreach (CastingGraphRoutineTab routine in view.Routines)
            {
                string captured = routine.RoutineId;
                Button tab = _kit.Button(_routineBar, "Routine." + captured,
                    routine.Name + " (" + routine.CastingCount + ")", () => Command(() =>
                        _session.SelectRoutine(captured), "routine-switch"), PlannerTextRole.Button, 16f);
                _kit.SetSelected(tab, routine.Selected);
                RectTransform rect = RectOf(tab);
                KingmakerUiFactory.SetAnchors(rect, 0f, 0.08f, 0f, 0.92f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(160f, 0f);
                rect.anchoredPosition = new Vector2(x, 0f);
                x += 168f;
            }
        }

        // Native UI finish (UI-06): each entry is a paper slip (the
        // character sheet's) with its framed icon, the name and type that
        // tell entries apart, and a planned-casting count only when there is
        // one; the selected entry wears the thin frame in burgundy.
        private void RebuildCatalogue(CastingGraphView view)
        {
            foreach (KeyValuePair<PlannerSourceCategory, Button> pair in _categoryTabs)
                _kit.SetSelected(pair.Value, pair.Key == _sourceCategory);
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
                string count = CastingGraphText.CatalogueCount(entry.RoutineCount, entry.PlanCount,
                    view.SelectedRoutineName);
                RectTransform rect = KingmakerUiFactory.CreateRect("Source." + captured, _catalogueContent);
                KingmakerUiFactory.AddLayout(rect, count.Length == 0 ? 54f : 64f);
                Image background = _kit.Paint(rect, PlannerNativeKit.PaperSlip,
                    entry.Selected ? Color.white : new Color(1f, 1f, 1f, 0.82f), true) ??
                    KingmakerUiFactory.AddPanel(rect, entry.Selected
                        ? new Color(1f, 0.95f, 0.84f, 0.95f) : new Color(1f, 1f, 1f, 0f));
                Button button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                ApplyRowHover(button, entry.Selected);
                button.onClick.AddListener(() => Command(
                    () => _session.SelectGraphBuff(captured, _inputs()), "buff-switch"));
                // Right-click reads the spell; it never selects, authors or
                // mutates anything (everyday-use v1.2 E05).
                AddSpellInspect(button, () => _session.BuildGraph(_inputs()),
                    entry.Selected && view.SelectedSourceExact ? view.SelectedSourceAbility : entry.Source.IconAbility,
                    entry.Selected && view.SelectedSourceExact,
                    entry.Selected && view.SelectedSourceExact ? view.SelectedCasterUnitId : null, entry.Label);
                if (entry.Selected) _kit.Frame(rect, PlannerNativeKit.ThinFrame, PlannerNativeKit.Burgundy, "Selected");
                AddIcon(rect, entry.Source.IconAbility, 38f, 8f);
                TextMeshProUGUI name = _kit.Text(rect, "Name", entry.Label,
                    entry.Selected ? PlannerTextRole.Strong : PlannerTextRole.Body, 16f, TextAlignmentOptions.TopLeft);
                if (entry.Selected) name.color = PlannerNativeKit.Burgundy;
                name.enableAutoSizing = true;
                name.fontSizeMin = 11f;
                name.fontSizeMax = 16f;
                name.overflowMode = TextOverflowModes.Ellipsis;
                KingmakerUiFactory.Stretch(name.rectTransform, 54f, 8f, count.Length == 0 ? 5f : 20f, 6f);
                if (count.Length != 0)
                {
                    TextMeshProUGUI planned = _kit.Text(rect, "Count", count, PlannerTextRole.Small, 13f,
                        TextAlignmentOptions.BottomLeft);
                    planned.color = entry.RoutineCount == 0 ? PlannerNativeKit.SecondaryInk : LegalInk;
                    planned.enableWordWrapping = false;
                    KingmakerUiFactory.Stretch(planned.rectTransform, 54f, 8f, 4f, 40f);
                }
            }
            if (shown == 0)
            {
                TextMeshProUGUI none = _kit.Text(_catalogueContent, "NoMatch",
                    _buffQuery.Length == 0 ? "No buffs here." : "No buff matches \"" + _buffQuery + "\".",
                    PlannerTextRole.Secondary, 15f, TextAlignmentOptions.MidlineLeft);
                KingmakerUiFactory.AddLayout(none.rectTransform, 30f);
            }
        }

        private void RebuildBanner(CastingGraphView view)
        {
            Sprite icon = ResolveAbilityIcon(view.SelectedSourceIcon);
            _bannerIcon.sprite = icon;
            _bannerIcon.color = icon == null ? new Color(0f, 0f, 0f, 0f) : Color.white;
            _bannerTitle.text = view.SelectedSourceCaption;
            string kind = view.SelectedSourceIsGroup == true ? "Group buff"
                : view.SelectedSourceIsGroup == false ? "Single target" : "Targeting not understood";
            _bannerDetail.text = kind + " · " + view.Castings.Count + " in " + view.SelectedRoutineName +
                (view.OtherRoutineCastings == 0 ? string.Empty
                    : " · " + view.OtherRoutineCastings + " in other routines");
            // The next step while adding; nothing while editing one casting
            // (the sidebar is about that casting then).
            CastingGraphCasterNode caster = view.CasterById(view.SelectedCasterUnitId);
            bool sourceChosen = caster != null && caster.Sources.Any(value => value.Selected);
            _guidance.text = view.Inspector != null ? string.Empty
                : CastingGraphText.NextStep(view.SelectedSourceId.Length != 0, caster != null, sourceChosen,
                    view.SelectedSourceIsGroup == true);
            // Right-click the selected-spell header: the exact variant's
            // description when a concrete row is selected, else base text.
            AddSpellInspect(_bannerIcon != null ? (Component)_bannerIcon : (Component)_bannerTitle,
                () => _session.BuildGraph(_inputs()), view.SelectedSourceAbility,
                view.SelectedSourceExact, view.SelectedSourceExact ? view.SelectedCasterUnitId : null,
                view.SelectedSourceCaption);
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
                Hint(chips, "NoCasting", "No castings of this buff in " + view.SelectedRoutineName + " yet.",
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
            TextMeshProUGUI hint = _kit.Text(parent, name, text, PlannerTextRole.Secondary, 15f,
                TextAlignmentOptions.TopLeft);
            hint.fontStyle = FontStyles.Italic;
            Place(hint.rectTransform, x, y, width, 80f);
        }

        // Native UI finish (UI-11): a caster is a framed portrait and name on
        // the paper; each exact source is a paper slip whose second line says
        // what the plan leaves of it and what it spends (UI-06/UI-07). The
        // chosen caster and source wear the thin frame in burgundy.
        private void BuildCasterNode(RectTransform layer, CastingGraphCasterNode caster, GraphLayoutResult layout,
            GraphLayoutMetrics m)
        {
            float top;
            if (!layout.CasterTops.TryGetValue(caster.UnitId, out top)) return;
            string captured = caster.UnitId;
            RectTransform header = Place(KingmakerUiFactory.CreateRect("Caster." + (caster.IsUnresolved
                ? "unresolved" : caster.UnitId), layer), 0f, top, m.CasterLaneWidth - 6f, m.CasterHeaderHeight - 4f);
            Image background = KingmakerUiFactory.AddPanel(header, caster.Selected
                ? new Color(1f, 0.95f, 0.84f, 0.55f) : new Color(1f, 1f, 1f, 0f));
            if (!caster.IsUnresolved)
            {
                Button button = header.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                ApplyRowHover(button, caster.Selected);
                button.onClick.AddListener(() => Command(() =>
                    SurfaceClick(_session.ClickGraphCaster(captured, _inputs())), "provider-switch"));
            }
            if (caster.Selected)
            {
                RectTransform rule = KingmakerUiFactory.CreateRect("Selected", header);
                KingmakerUiFactory.SetAnchors(rule, 0f, 0f, 0f, 1f);
                rule.pivot = new Vector2(0f, 0.5f);
                rule.sizeDelta = new Vector2(3f, 0f);
                KingmakerUiFactory.AddPanel(rule, PlannerNativeKit.Burgundy).raycastTarget = false;
            }
            AddPortrait(header, caster.IsUnresolved ? null : caster.UnitId, 44f, 8f);
            TextMeshProUGUI name = _kit.Text(header, "Name", caster.DisplayName, PlannerTextRole.Strong, 18f,
                TextAlignmentOptions.TopLeft);
            if (caster.Selected) name.color = PlannerNativeKit.Burgundy;
            name.enableWordWrapping = false;
            name.overflowMode = TextOverflowModes.Ellipsis;
            KingmakerUiFactory.Stretch(name.rectTransform, 60, 4, 22, 4);
            if (caster.Note.Length != 0)
            {
                TextMeshProUGUI note = _kit.Text(header, "Note", caster.Note, PlannerTextRole.Small, 13f,
                    TextAlignmentOptions.BottomLeft);
                if (caster.IsUnresolved) note.color = BlockedInk;
                note.enableWordWrapping = false;
                note.overflowMode = TextOverflowModes.Ellipsis;
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
                Image rowBackground = _kit.Paint(rect, PlannerNativeKit.PaperSlip,
                    row.Selected ? Color.white : new Color(1f, 1f, 1f, 0.78f), true) ??
                    KingmakerUiFactory.AddPanel(rect, row.Selected
                        ? new Color(0.99f, 0.90f, 0.78f, 1f) : new Color(1f, 0.97f, 0.90f, 0.55f));
                if (row.Selected) _kit.Frame(rect, PlannerNativeKit.ThinFrame, PlannerNativeKit.Burgundy, "Selected");
                Button button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = rowBackground;
                ApplyRowHover(button, row.Selected);
                string key = row.ProviderKey;
                button.onClick.AddListener(() => Command(() =>
                    SurfaceClick(_session.ClickGraphSource(key, _inputs())), "provider-switch"));
                TextMeshProUGUI label = _kit.Text(rect, "Label", row.Label + (row.IsGroup ? " · group" : string.Empty),
                    PlannerTextRole.Strong, 15f, TextAlignmentOptions.TopLeft);
                if (row.Selected) label.color = PlannerNativeKit.Burgundy;
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Ellipsis;
                KingmakerUiFactory.Stretch(label.rectTransform, 10, 8, 19, 2);
                TextMeshProUGUI count = _kit.Text(rect, "Capacity", CastingGraphText.SourceLine(row.Usable,
                        row.BlockedReason, row.CapacityText, row.PoolLabel, row.SharedPoolWith),
                    PlannerTextRole.Small, 13f, TextAlignmentOptions.BottomLeft);
                count.color = row.Usable ? LegalInk : BlockedInk;
                count.enableAutoSizing = true;
                count.fontSizeMin = 10f;
                count.fontSizeMax = 13f;
                count.enableWordWrapping = false;
                count.overflowMode = TextOverflowModes.Ellipsis;
                KingmakerUiFactory.Stretch(count.rectTransform, 10, 8, 2, 23);
            }
        }

        // A recipient: framed portrait and name on a paper slip whose wash
        // says whether a click can give it this casting (the hint says what
        // the click does, UI-05).
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
            Color wash = legal ? new Color(0.86f, 0.93f, 0.80f, 1f)
                : illegal ? new Color(0.86f, 0.84f, 0.81f, 0.85f) : new Color(1f, 1f, 1f, 0.82f);
            Image background = _kit.Paint(rect, PlannerNativeKit.PaperSlip, wash, true) ??
                KingmakerUiFactory.AddPanel(rect, legal
                    ? new Color(0.93f, 0.97f, 0.88f, 0.80f) : new Color(1f, 0.97f, 0.90f, 0.45f));
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ApplyRowHover(button, false);
            button.onClick.AddListener(() => Command(() =>
                SurfaceClick(_session.ClickGraphRecipient(captured, _inputs())), "recipient-toggle"));
            Image portrait = AddPortrait(rect, target.UnitId, 62f, 8f);
            if (portrait != null && illegal) portrait.color = new Color(0.55f, 0.50f, 0.48f, 0.75f);
            TextMeshProUGUI name = _kit.Text(rect, "Name", target.DisplayName, PlannerTextRole.Strong, 16f,
                TextAlignmentOptions.TopLeft);
            name.enableWordWrapping = false;
            name.overflowMode = TextOverflowModes.Ellipsis;
            KingmakerUiFactory.Stretch(name.rectTransform, 64, 6, 48, 8);
            TextMeshProUGUI hint = _kit.Text(rect, "Hint", illegal ? target.IllegalReason : target.Hint,
                PlannerTextRole.Small, 13f, TextAlignmentOptions.TopLeft);
            hint.color = illegal ? BlockedInk : legal ? LegalInk : PlannerNativeKit.SecondaryInk;
            hint.enableAutoSizing = true;
            hint.fontSizeMin = 10f;
            hint.fontSizeMax = 13f;
            KingmakerUiFactory.Stretch(hint.rectTransform, 64, 6, 4, 30);
        }

        // Native UI finish (UI-04): a casting is a light paper slip (the
        // dialogue's notched sheet) on its ink line: its order and one
        // concise state ("Active · Will skip"), then only meaningful detail
        // (enabled enhancements, a group's reach). Selected: the thin frame
        // and order in burgundy; not ready: the state in the blocked ink.
        private void BuildChip(RectTransform layer, CastingGraphCasting casting, GraphChipPlacement placement,
            GraphLayoutMetrics m)
        {
            string captured = casting.CastingId;
            RectTransform rect = Place(KingmakerUiFactory.CreateRect("Casting." + casting.CastingId, layer),
                placement.Left.X, placement.Top, m.ChipWidth, m.ChipHeight);
            Image background = _kit.Paint(rect, PlannerNativeKit.PaperSlip, Color.white, true) ??
                KingmakerUiFactory.AddPanel(rect, KingmakerUiFactory.ToColor(casting.Selected
                    ? PlannerParchmentPalette.ChipGroundSelected : PlannerParchmentPalette.ChipGround));
            bool blocked = casting.Readiness == ResolvedCastingReadiness.Blocked || casting.ShortInOnePass;
            if (casting.Selected) _kit.Frame(rect, PlannerNativeKit.ThinFrame, PlannerNativeKit.Burgundy, "Selected");
            else if (blocked) _kit.Frame(rect, PlannerNativeKit.ThinFrame, BlockedInk, "Blocked");
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ApplyRowHover(button, casting.Selected);
            button.onClick.AddListener(() => Command(() => _session.FocusGraphCasting(captured)));
            AddSpellInspect(button, () => _session.BuildGraph(_inputs()),
                casting.SpellAbility, true, casting.CasterUnitId, ChipInspectTitle(casting, _lastView));
            string status = (_session.ProblemNavigation.Active && casting.Selected
                ? "Problem " + _session.ProblemNavigation.Position + " of " + _session.ProblemNavigation.Count +
                    " · Not ready" : casting.StatusLabel) +
                (casting.ShortInOnePass ? " · Short without rest" : string.Empty) +
                (casting.RedundantInOnePass ? " · Covered earlier" : string.Empty) +
                (casting.NeedsReview ? " · Review" : string.Empty);
            TextMeshProUGUI first = _kit.Text(rect, "Status", casting.OrderLabel + "  " + status,
                PlannerTextRole.Strong, 15f, TextAlignmentOptions.TopLeft);
            first.color = blocked ? BlockedInk : casting.Selected ? PlannerNativeKit.Burgundy : PlannerNativeKit.BodyInk;
            first.enableAutoSizing = true;
            first.fontSizeMin = 11f;
            first.fontSizeMax = 15f;
            first.enableWordWrapping = false;
            first.overflowMode = TextOverflowModes.Ellipsis;
            var details = new List<string>();
            if (casting.IsGroup) details.Add("Group · reaches " + casting.Beneficiaries.Count);
            details.AddRange(casting.EnhancementBadges.Where(value => !string.IsNullOrEmpty(value)));
            if (details.Count == 0)
            {
                KingmakerUiFactory.Stretch(first.rectTransform, 9, 7, 6, 6);
                first.alignment = TextAlignmentOptions.MidlineLeft;
                return;
            }
            KingmakerUiFactory.Stretch(first.rectTransform, 9, 7, 22, 4);
            TextMeshProUGUI second = _kit.Text(rect, "Detail", string.Join(" · ", details.ToArray()),
                PlannerTextRole.Small, 13f, TextAlignmentOptions.BottomLeft);
            second.enableAutoSizing = true;
            second.fontSizeMin = 10f;
            second.fontSizeMax = 13f;
            second.enableWordWrapping = false;
            second.overflowMode = TextOverflowModes.Ellipsis;
            KingmakerUiFactory.Stretch(second.rectTransform, 9, 7, 4, 25);
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
            _kit.Frame(rect, PlannerNativeKit.PortraitFrame, Color.white);
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
            if (icon != null) _kit.Frame(rect, PlannerNativeKit.ThinFrame, Color.white);
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
            _sectionCount = 0;
            RebuildImportNotices();
            if (view.Inspector == null)
            {
                _inspectorTitle.text = "New casting";
                RebuildNextCasting(view);
                return;
            }
            RebuildCastingInspector(view, view.Inspector);
        }

        // Native UI finish (UI-05), adding a casting: only the current step's
        // instruction, the chosen source in brief, the Share choice where it
        // applies, and the default for new castings - its own scope, named so.
        private void RebuildNextCasting(CastingGraphView view)
        {
            CastingGraphCasterNode caster = view.CasterById(view.SelectedCasterUnitId);
            CastingGraphSourceRow row = caster == null ? null
                : caster.Sources.FirstOrDefault(value => value.Selected);
            Line("NextStep", CastingGraphText.NextStep(view.SelectedSourceId.Length != 0, caster != null,
                row != null, view.SelectedSourceIsGroup == true), PlannerTextRole.Strong, 17f,
                PlannerNativeKit.HeadingInk);
            if (row != null)
            {
                Line("SourceDetail", caster.DisplayName + " · " + row.Label, PlannerTextRole.Body, 15f, null);
                Line("SourceCapacity", CastingGraphText.SourceLine(row.Usable, row.BlockedReason, row.CapacityText,
                    row.PoolLabel, row.SharedPoolWith), PlannerTextRole.Small, 13f, row.Usable ? LegalInk : BlockedInk);
            }
            // v1.2 §7 / E16: the Share control sits between the exact
            // source and the target click - the owner's authoring order.
            if (row != null && view.NextCastingModifiers != null &&
                view.NextCastingModifiers.Count != 0)
            {
                Section("Share with an ally");
                foreach (CastingGraphModifierOption modifier in view.NextCastingModifiers)
                {
                    string id = modifier.ModifierId;
                    Checkbox("DraftModifier." + id, modifier.Title, modifier.Selected,
                        () => Surface(_session.ToggleDraftTargetingModifier(id, _inputs()), "share"),
                        modifier.Selected || modifier.Available);
                    ModifierDetail(modifier, "DraftModifier");
                }
            }
            // The default for the NEXT castings (each casting keeps its own
            // afterwards; its options change only that one).
            Section("New castings");
            bool recastDraft = _session.Draft.ExistingEffectPolicy == ExistingEffectPolicy.Overwrite;
            Checkbox("DraftRecastPolicy", "Recast if active", recastDraft, () =>
            {
                _session.Draft.ExistingEffectPolicy = recastDraft
                    ? ExistingEffectPolicy.SkipAlreadyActive : ExistingEffectPolicy.Overwrite;
            });
        }

        // Native UI finish (UI-05), editing one casting, in the owner's
        // order: Done; caster -> recipient and the source; the one outcome
        // line; Recast if active; Enhancements; the resource summary; then,
        // set apart, Remove casting. Order stays available compactly; the
        // routine reassignment section is gone (routines are chosen above).
        private void RebuildCastingInspector(CastingGraphView view, CastingGraphInspector inspector)
        {
            _inspectorTitle.text = "Casting options";
            CastingProblemNavigation problems = _session.ProblemNavigation;
            bool problem = problems.Active && string.Equals(problems.Current.CastingId,
                inspector.CastingId, StringComparison.Ordinal);
            CastingGraphCasting chip = inspector.Chip;
            if (problem)
            {
                Line("ProblemPosition", inspector.RoutineName + " · Problem " + problems.Position +
                    " of " + problems.Count, PlannerTextRole.Strong, 17f, PlannerNativeKit.Burgundy);
                Line("ProblemBuff", view.SelectedSourceCaption, PlannerTextRole.Strong, 16f, null);
            }
            else ActionButton("DoneEditing", "Done", () => _session.ClearGraphFocus(), 108f);
            // 2. Who casts it on whom, and from which source.
            Line("Headline", RelationLine(view, inspector), PlannerTextRole.Strong, 18f, null);
            string source = SourceCaption(view, chip);
            if (source.Length != 0) Line("Source", source, PlannerTextRole.Secondary, 15f, null);
            RectTransform change = Row("ExplicitChange", 32f);
            RowButton(change, "ChangeRecipient",
                _session.GraphEditIntent == CastingGraphEditIntent.ChangeRecipient
                    ? "Cancel recipient change" : "Change recipient",
                ToggleRecipientEditCommand, 0f, 0.5f);
            RowButton(change, "ChangeProvider",
                _session.GraphEditIntent == CastingGraphEditIntent.ChangeProvider
                    ? "Cancel caster change" : "Change caster/source",
                ToggleProviderEditCommand, 0.5f, 1f);
            if (_session.GraphEditIntent == CastingGraphEditIntent.ChangeRecipient)
                Line("ChangeRecipientHint", "Choose the new recipient once; the control then turns off.",
                    PlannerTextRole.Small, 13f, Burgundy);
            else if (_session.GraphEditIntent == CastingGraphEditIntent.ChangeProvider)
                Line("ChangeProviderHint", "Choose the new caster and exact source once; the control then turns off.",
                    PlannerTextRole.Small, 13f, Burgundy);
            // 3. The one outcome line.
            string status = chip == null ? "Not shown in this routine" : chip.StatusLabel;
            Line("CastingStatus", status, PlannerTextRole.Strong, 16f,
                problem || chip != null && chip.Readiness == ResolvedCastingReadiness.Blocked ? BlockedInk : LegalInk);
            foreach (string reason in problem ? problems.Current.Reasons.Select(WorkspaceReasonText.Describe)
                : inspector.Reasons)
                Line("Reason", (problem ? "Not ready: " : string.Empty) + reason, PlannerTextRole.Small, 14f, BlockedInk);
            if (problem)
            {
                Button previous = ActionButton("ProblemPrevious", "Previous Problem",
                    () => _session.NavigateProblem(-1));
                Button next = ActionButton("ProblemNext", "Next Problem",
                    () => _session.NavigateProblem(1));
                PlannerNativeKit.SetEnabled(previous, problems.CanPrevious);
                PlannerNativeKit.SetEnabled(next, problems.CanNext);
                ActionButton("DoneEditing", "Done", () => _session.ClearGraphFocus(), 108f);
            }
            if (chip != null && chip.ShortInOnePass)
                Line("ShortInOnePass", "Short of a resource if every routine runs without rest.",
                    PlannerTextRole.Small, 14f, BlockedInk);
            if (chip != null && chip.RedundantInOnePass)
                Line("Redundant", "An earlier identical casting already gives this without rest.",
                    PlannerTextRole.Small, 14f, null);
            if (inspector.ExecutionLimitation.Length != 0)
                Line("Limitation", "Cannot run in this version: " + inspector.ExecutionLimitation,
                    PlannerTextRole.Small, 14f, BlockedInk);
            if (inspector.ReviewItems.Count != 0)
            {
                Section("Imported: needs your review");
                foreach (string item in inspector.ReviewItems)
                    Line("ReviewItem", "· " + item, PlannerTextRole.Small, 14f, BlockedInk);
                ActionButton("ResolveImportReview", "Resolve review (keep current choices)", () =>
                    Surface(_session.ResolveFocusedImportReview(), "resolve review"));
            }
            // 4. This casting's own existing-effect choice.
            Checkbox("FocusedRecastPolicy", "Recast if active", inspector.RecastsExisting,
                () => Surface(_session.SetFocusedRecastPolicy(inspector.RecastsExisting
                    ? ExistingEffectPolicy.SkipAlreadyActive : ExistingEffectPolicy.Overwrite), "recast"));
            // An imported casting may be in the wrong shape for its buff: a
            // single-target casting of a group buff can become one group
            // casting centred on its caster, and a group casting of a
            // single-target buff a casting on one chosen member. Offered
            // only when the buff needs it.
            PlannedCasting focused = inspector.Casting;
            bool group = focused.TargetMode != CastingTargetMode.DirectTarget;
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
                Line("SingleHint", "This buff has one target per casting: choose who receives it.",
                    PlannerTextRole.Small, 14f, BlockedInk);
                foreach (CastingGraphTargetNode target in view.Targets)
                {
                    string unit = target.UnitId;
                    ActionButton("FocusedSingle." + unit, "Single target: " + target.DisplayName,
                        () => Surface(_session.SetFocusedTargeting(CastingTargetMode.DirectTarget, unit, null, null),
                            "mode"));
                }
            }
            if (group && inspector.CoverageText.Length != 0)
                Line("Coverage", inspector.CoverageText, PlannerTextRole.Small, 14f, null);
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
                    Checkbox("FocusedModifier." + id, modifier.Title, modifier.Selected,
                        () => Surface(_session.ToggleFocusedTargetingModifier(id, _inputs()), "share"),
                        modifier.Selected || modifier.Available);
                    ModifierDetail(modifier, "FocusedModifier");
                }
            }
            // 5. Enhancements: this casting only.
            Section("Enhancements");
            if (inspector.Enhancements.Count == 0)
                Line("NoEnhancement", "No enhancements available", PlannerTextRole.Secondary, 15f, null);
            foreach (CastingGraphEnhancementOption option in inspector.Enhancements)
            {
                string id = option.EnhancementId;
                bool usable = option.Selected || option.UnavailableReason.Length == 0;
                Checkbox("Enhancement." + id, option.Name + (option.Selected && !option.Required
                        ? " (optional, imported)" : string.Empty), option.Selected,
                    () => Surface(_session.ToggleFocusedEnhancement(id, _inputs()), "enhancement"), usable);
                var detail = new List<string>();
                if (!string.IsNullOrEmpty(option.ExpectedEffect)) detail.Add(option.ExpectedEffect);
                if (!string.IsNullOrEmpty(option.SourceName)) detail.Add(option.SourceName);
                if (!string.IsNullOrEmpty(option.BudgetText)) detail.Add(option.BudgetText);
                if (detail.Count != 0)
                    Line("EnhancementDetail", string.Join(" · ", detail.ToArray()), PlannerTextRole.Small, 13f,
                        option.PoolRemaining == 0 && !option.Selected ? BlockedInk : (Color?)null, 30f);
                if (option.UnavailableReason.Length != 0 && !option.Selected)
                    Line("EnhancementUnavailable", option.UnavailableReason, PlannerTextRole.Small, 13f, BlockedInk, 30f);
            }
            // 6. What this casting spends: nominal when the run skips it.
            Section("Resources");
            bool skipped = chip != null && chip.Readiness == ResolvedCastingReadiness.AlreadySatisfied &&
                !inspector.RecastsExisting;
            if (inspector.CostLines.Count == 0)
                Line("NoCost", "Free", PlannerTextRole.Body, 15f, null);
            foreach (string cost in inspector.CostLines)
                Line("Cost", (skipped ? "If cast: " : string.Empty) + cost, PlannerTextRole.Body, 15f, null);
            if (skipped)
                Line("SkippedSpends", "Active now: the run skips it and spends nothing.", PlannerTextRole.Small, 13f, null);
            ActionButton("OpenResources", "Resource details", ToggleResources, 170f);
            // Order within the routine (compact).
            Line("RoutineLine", "Casting " + (inspector.Casting.Order + 1) + " of " + inspector.RoutineCount +
                " in " + inspector.RoutineName, PlannerTextRole.Small, 14f, null);
            RectTransform order = Row("Order", 30f);
            Button earlier = RowButton(order, "FocusedOrder.Earlier", "Cast earlier",
                () => Surface(_session.MoveFocusedCastingWithinRoutine(-1), "move"), 0f, 0.5f);
            PlannerNativeKit.SetEnabled(earlier, inspector.CanMoveEarlier);
            Button later = RowButton(order, "FocusedOrder.Later", "Cast later",
                () => Surface(_session.MoveFocusedCastingWithinRoutine(1), "move"), 0.5f, 1f);
            PlannerNativeKit.SetEnabled(later, inspector.CanMoveLater);
            if (inspector.LastRun.Length != 0)
                Line("LastRun", "Last run: " + inspector.LastRun, PlannerTextRole.Small, 13f, null);
            // WP3: a genuine Draft (an imported choice still to finish) can be
            // marked Ready. A Disabled record from an earlier version is shown
            // honestly - it is never cast and never blocks - and is removed,
            // not re-enabled; no new Disabled record or duplicate is authored.
            if (inspector.Casting.State == CastingAuthoringState.Draft)
                ActionButton("MarkReady", "Mark Ready", () => Surface(_session.SetFocusedCastingState(
                    CastingAuthoringState.Ready), "mark ready"));
            if (inspector.Casting.State == CastingAuthoringState.Disabled)
                Line("LegacyDisabled", "Disabled in an earlier version: it is not cast and blocks nothing. " +
                    "Remove it if you no longer want it.", PlannerTextRole.Small, 14f, null);
            // 7. Set apart: removal.
            Divider();
            ActionButton("Remove", "Remove casting", () =>
            {
                AuthoringEditResult result = _session.RemoveFocusedCasting();
                if (!result.Applied) Surface(result, "remove");
                else Feedback("Casting removed (Undo restores it).");
            });
        }

        // "Linzi » Hedwirg" (the native face has no arrow glyph), or the
        // group's centre for a group casting.
        private static string RelationLine(CastingGraphView view, CastingGraphInspector inspector)
        {
            CastingGraphCasting chip = inspector.Chip;
            CastingGraphCasterNode caster = chip == null ? null : view.CasterById(chip.CasterUnitId);
            string casterName = caster == null ? "Unknown caster" : caster.DisplayName;
            PlannedCasting casting = inspector.Casting;
            if (casting.TargetMode == CastingTargetMode.DirectTarget)
            {
                CastingGraphTargetNode target = view.TargetById(casting.DirectTargetUnitId);
                return casterName + " » " + (target == null ? inspector.TargetLabel : target.DisplayName);
            }
            return casterName + " » group: " + inspector.TargetLabel;
        }

        // The casting's source as its caster's row names it ("Wizard ·
        // Level 1"), or nothing when that row is not shown.
        private static string SourceCaption(CastingGraphView view, CastingGraphCasting chip)
        {
            CastingGraphCasterNode caster = chip == null ? null : view.CasterById(chip.CasterUnitId);
            CastingGraphSourceRow row = caster == null ? null : caster.Sources.FirstOrDefault(value =>
                string.Equals(value.ProviderKey, chip.SourceProviderKey, StringComparison.Ordinal));
            return row == null ? string.Empty : row.Label;
        }

        private void ModifierDetail(CastingGraphModifierOption modifier, string prefix)
        {
            var detail = new List<string>();
            if (modifier.Detail.Length != 0) detail.Add(modifier.Detail);
            if (modifier.CostText.Length != 0) detail.Add("Cost: " + modifier.CostText);
            if (detail.Count != 0)
                Line(prefix + "Detail", string.Join(" · ", detail.ToArray()), PlannerTextRole.Small, 13f, null, 30f);
            // A saved choice that no longer applies stays visible as a
            // repair warning; untick it to repair the casting.
            if (modifier.UnavailableReason.Length != 0 && (modifier.Selected || prefix == "DraftModifier"))
                Line(prefix + "Unavailable", (modifier.Selected ? "Chosen, but no longer available: " : string.Empty) +
                    WorkspaceReasonText.DescribeNested(modifier.UnavailableReason) +
                    (modifier.Selected ? " - untick it to repair this casting" : string.Empty),
                    PlannerTextRole.Small, 13f, BlockedInk, 30f);
        }

        private void RebuildImportNotices()
        {
            IReadOnlyList<string> pending = _session.PendingImportNotices;
            if (pending.Count == 0) return;
            Section("Imported plan-wide constraints (" + pending.Count + ")");
            foreach (string notice in pending) Line("ImportNotice", "· " + notice, PlannerTextRole.Small, 13f, null);
            Line("ImportNoticeRule", "Runs are refused until these are acknowledged.", PlannerTextRole.Small, 13f,
                BlockedInk);
            ActionButton("AcknowledgeImportNotices", "Acknowledge imported constraints", () =>
            {
                AuthoringEditResult result = _session.AcknowledgeImportNotices();
                Surface(result, "acknowledge");
                if (result.Applied) Feedback("Acknowledged (Undo reverts): " + result.Scope);
            });
        }

        // A short native section heading, with a light rule between sections.
        private void Section(string caption)
        {
            if (_sectionCount++ > 0) Divider();
            TextMeshProUGUI label = _kit.Text(_inspectorContent, "Section." + caption, caption,
                PlannerTextRole.Heading, 17f, TextAlignmentOptions.BottomLeft);
            label.enableWordWrapping = false;
            KingmakerUiFactory.AddLayout(label.rectTransform, 24f);
        }

        private void Divider()
        {
            RectTransform slot = KingmakerUiFactory.CreateRect("Rule", _inspectorContent);
            KingmakerUiFactory.AddLayout(slot, 10f);
            RectTransform line = KingmakerUiFactory.CreateRect("Line", slot);
            KingmakerUiFactory.SetAnchors(line, 0f, 0.5f, 1f, 0.5f, 6f, 6f, 0f, 0f);
            line.sizeDelta = new Vector2(line.sizeDelta.x, 3f);
            if (_kit.Paint(line, "blockscroll_bottom", new Color(1f, 1f, 1f, 0.8f)) == null)
            {
                Image plain = line.gameObject.AddComponent<Image>();
                plain.color = new Color(PlannerNativeKit.BodyInk.r, PlannerNativeKit.BodyInk.g,
                    PlannerNativeKit.BodyInk.b, 0.25f);
                plain.raycastTarget = false;
            }
        }

        private TextMeshProUGUI Line(string name, string text, PlannerTextRole role, float size, Color? colour,
            float indent = 0f)
        {
            TextMeshProUGUI line = _kit.Text(_inspectorContent, name, text, role, size, TextAlignmentOptions.TopLeft);
            if (colour.HasValue) line.color = colour.Value;
            line.enableWordWrapping = true;
            if (indent > 0f) line.margin = new Vector4(indent, 0f, 0f, 0f);
            // Wrapped text takes its own preferred height in the column.
            return line;
        }

        private Button ActionButton(string name, string caption, Action action, float width = 0f)
        {
            if (width <= 0f)
            {
                Button wide = _kit.Button(_inspectorContent, name, caption, () => Command(action),
                    PlannerTextRole.Button, 15f);
                KingmakerUiFactory.AddLayout(RectOf(wide), 32f);
                return wide;
            }
            // A compact control keeps its own width at the column's left.
            RectTransform slot = Row(name + ".Slot", 32f);
            Button button = _kit.Button(slot, name, caption, () => Command(action), PlannerTextRole.Button, 15f);
            RectTransform rect = RectOf(button);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(width, 0f);
            rect.anchoredPosition = Vector2.zero;
            return button;
        }

        private Button Checkbox(string name, string caption, bool on, Action toggle, bool enabled = true)
        {
            Button box = _kit.Checkbox(_inspectorContent, name, caption, on, () => Command(toggle), enabled, 15f);
            KingmakerUiFactory.AddLayout(RectOf(box), 28f);
            return box;
        }

        private RectTransform Row(string name, float height)
        {
            RectTransform row = KingmakerUiFactory.CreateRect(name, _inspectorContent);
            KingmakerUiFactory.AddLayout(row, height);
            return row;
        }

        private Button RowButton(RectTransform row, string name, string caption, Action action, float from, float to)
        {
            Button button = _kit.Button(row, name, caption, () => Command(action), PlannerTextRole.Button, 15f);
            KingmakerUiFactory.SetAnchors(RectOf(button), from, 0f, to, 1f, from > 0f ? 3f : 0f,
                to < 1f ? 3f : 0f, 0f, 0f);
            return button;
        }

        // ------------------------------------------------------------------
        // Commands (all go through the session; the view refreshes after)
        // ------------------------------------------------------------------

        private void Command(Action action, string refreshCause = "command")
        {
            if (_disposed) return;
            long commandStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            _nextInteractionStartedAt = commandStarted;
            // A command's feedback lasts until the next command; without one
            // the footer shows Run's status again.
            _feedbackShown = false;
            try
            {
                action();
            }
            catch (Exception exception)
            {
                // A failed command is told, never swallowed into a stale view.
                Feedback("That did not work: " + exception.Message, true);
                Debug.LogError("[KBP-WORKSPACE] command failed: " + exception);
            }
            _nextCommandAndPersistenceMilliseconds = ElapsedMilliseconds(commandStarted);
            _nextRefreshCause = refreshCause;
            RefreshView();
        }

        // WP3: one short footer sentence per graph gesture; a refusal keeps
        // the casting unchanged and says why.
        private void SurfaceClick(CastingGraphClickResult result)
        {
            if (result == null) return;
            switch (result.Outcome)
            {
                case CastingGraphClickOutcome.Added:
                    Feedback("Casting added (Undo removes it).");
                    return;
                case CastingGraphClickOutcome.Focused:
                    Feedback("Selected the casting this recipient already has.");
                    return;
                case CastingGraphClickOutcome.DisambiguationRequired:
                    Feedback("More than one saved casting matches. Select a casting card, then use Remove casting.",
                        true);
                    return;
                case CastingGraphClickOutcome.Removed:
                    Feedback("Casting removed (Undo restores it).");
                    return;
                case CastingGraphClickOutcome.Retargeted:
                    Feedback("Casting moved to a new recipient (Undo moves it back).");
                    return;
                case CastingGraphClickOutcome.Recentred:
                    Feedback("Group casting re-centred (Undo moves it back).");
                    return;
                case CastingGraphClickOutcome.ProviderChanged:
                    Feedback("Caster changed" + (string.IsNullOrEmpty(result.Edit.Reason)
                        ? string.Empty : ": " + result.Edit.Reason) + " (Undo restores the previous caster).");
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
                Feedback(char.ToUpperInvariant(action[0]) + action.Substring(1) + " refused: " +
                    WorkspaceRefusalText.Describe(result.Reason), true);
            else if (!string.IsNullOrEmpty(result.Reason))
                Feedback(char.ToUpperInvariant(action[0]) + action.Substring(1) + ": " + result.Reason);
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
                Feedback("Reload replaces this plan with the stored one, and edits that are not " +
                    "saved can be lost. Press Reload again to continue.", true);
                return;
            }
            _reloadArmedUntil = 0f;
            CastingPlanLoadStatus status = _session.Reload();
            // Castings kept across the reload are deliberate edits made
            // while saving was blocked: now that it is not, they save like
            // any other edit.
            bool keptSaved = _session.LastReloadNote != "kept-unsaved" || _session.RetryFailedSave();
            Feedback(_session.LegacyImportBlocked
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
                              : "Reloaded the stored casting plan."),
                _session.LegacyImportBlocked || !keptSaved);
        }

        private void ToggleModeCommand()
        {
            _session.SetExecutionMode(_session.ExecutionMode == "instant" ? "animated" : "instant");
            PlannerNativeKit.SetCaption(_modeButton, ModeCaption());
            Feedback(WorkspaceFooterText.ModeChanged(_session.ExecutionMode));
        }

        private void ToggleAvailabilityPolicyCommand()
        {
            string next = _session.AvailabilityPolicy == ExecutionProfile.RequireFullPlan
                ? ExecutionProfile.CastAvailable : ExecutionProfile.RequireFullPlan;
            _session.SetAvailabilityPolicy(next);
            PlannerNativeKit.SetCaption(_policyButton, PolicyCaption());
            Feedback(next == ExecutionProfile.CastAvailable
                ? "Cast available: ready castings run in authored order; every omission is reported."
                : "Require full plan: any unavailable requested casting refuses the run.");
        }

        private void ToggleRecipientEditCommand()
        {
            if (_session.GraphEditIntent == CastingGraphEditIntent.ChangeRecipient)
            {
                _session.CancelGraphEditIntent();
                Feedback("Recipient change cancelled.");
                return;
            }
            Surface(_session.BeginChangeRecipient(), "change recipient");
        }

        private void ToggleProviderEditCommand()
        {
            if (_session.GraphEditIntent == CastingGraphEditIntent.ChangeProvider)
            {
                _session.CancelGraphEditIntent();
                Feedback("Caster/source change cancelled.");
                return;
            }
            Surface(_session.BeginChangeProvider(), "change caster/source");
        }

        private void UndoCommand()
        {
            if (!_session.Undo()) Feedback("Nothing to undo.");
        }

        private string ModeCaption()
        {
            return _session.ExecutionMode == "instant" ? "Mode: Instant" : "Mode: Animated";
        }

        private string PolicyCaption()
        {
            return _session.AvailabilityPolicy == ExecutionProfile.CastAvailable
                ? "Cast available" : "Require full plan";
        }

        // The footer's standing line when no command has just spoken: an
        // automatic spellbook-removal edit (with its Undo) or the last
        // attempt while it stands, else Run's status.
        private string FooterStatus(CastingGraphView view, out bool problem)
        {
            problem = false;
            // 0.4.2 (B): an automatic spellbook-removal edit stays named
            // (with its Undo) until it is undone or superseded.
            string removal = _session.SpellbookReconciliationNotice;
            if (!string.IsNullOrEmpty(removal))
                return string.IsNullOrEmpty(_session.LastAttemptMessage) ||
                    _session.LastAttemptMessage.StartsWith(removal, StringComparison.Ordinal)
                    ? removal : removal + " Last attempt: " + _session.LastAttemptMessage;
            CastingApplyDecision gate = view.SelectedRoutineGate;
            int notReady = gate == null ? 0 : gate.BlockingCastings.Count;
            int executable = gate == null ? 0 : gate.ExecutableCastingIds.Count;
            string otherBlock = gate == null || gate.Allowed || gate.BlockingReasons.Count == 0 ? null
                : WorkspaceReasonText.Describe(gate.BlockingReasons[0]);
            bool allowed = gate != null && gate.Allowed;
            problem = !allowed && view.RoutineCastingCount != 0;
            return WorkspaceFooterText.RunStatus(_session.DispatchDisposition, view.SelectedRoutineName,
                view.RoutineCastingCount, allowed, executable, notReady, otherBlock,
                view.OnePassShortCount,
                _session.ApplyMode == CastingApplyMode.ReadyCastsOnly,
                gate == null ? 0 : gate.Omissions.Count);
        }

        private void RunApply()
        {
            CastingApplyMode mode = _session.ApplyMode;
            string name = _session.RoutineDisplayName(_session.SelectedRoutineId);
            CastingWorkspaceInputs inputs;
            try { inputs = _freshInputs(); }
            catch (Exception exception)
            {
                // Without fresh discovery nothing is submitted.
                string refreshRefusal = name + " was not cast: the party state could not be refreshed (" +
                    exception.Message + ").";
                _session.RecordGlobalRefusal(refreshRefusal);
                Feedback(refreshRefusal, true);
                return;
            }
            WorkspaceApplyResult result = _session.Apply(mode, _session.SelectedRoutineId, inputs);
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
            Debug.Log("[KBP-CF-RUN] refused;reason=" + result.ReviewReason);
            if (result.Dispatch != null && !result.Dispatch.Submitted && result.Projection != null &&
                result.Projection.Converted)
                refusal += " Would run " + result.Projection.Plan.Steps.Count +
                    (result.Projection.Plan.Steps.Count == 1 ? " cast" : " casts") + " in order.";
            Feedback(refusal, true);
        }

        private void RebuildFooter(CastingGraphView view)
        {
            bool problems = _session.ProblemNavigation.Active;
            if (problems || _showedProblems)
            {
                if (!string.IsNullOrEmpty(_session.LastAttemptMessage) && !_feedbackShown)
                    Feedback(_session.LastAttemptMessage, true);
            }
            _showedProblems = problems;
            float textRight = _footerButtonsRight;
            if (_footerResult != null)
                _footerResult.rectTransform.offsetMax = new Vector2(-textRight,
                    _footerResult.rectTransform.offsetMax.y);
            if (_footerLedger != null)
                _footerLedger.rectTransform.offsetMax = new Vector2(-(textRight - 8f),
                    _footerLedger.rectTransform.offsetMax.y);
            if (!_feedbackShown && _footerResult != null)
            {
                bool problem;
                _footerResult.text = FooterStatus(view, out problem);
                _footerResult.color = problem ? BlockedInk : PlannerNativeKit.BodyInk;
            }
            if (_resources != null && _resources.gameObject.activeSelf)
                RebuildResources();
            if (_undoButton != null) PlannerNativeKit.SetEnabled(_undoButton, _session.CanUndo);
            // Run runs the SELECTED routine, so it says which one.
            if (_runButton != null)
                PlannerNativeKit.SetCaption(_runButton, WorkspaceFooterText.RunCaption(
                    _session.RoutineDisplayName(_session.SelectedRoutineId)));
            if (_policyButton != null)
                PlannerNativeKit.SetCaption(_policyButton, PolicyCaption());
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
