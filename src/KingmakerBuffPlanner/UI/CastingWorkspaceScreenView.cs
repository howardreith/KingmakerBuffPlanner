using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.UI;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Persistence;
using UnityEngine;
using UnityEngine.UI;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    // The primary casting-first workspace surface: one persistent parchment
    // with a caster lane, casting-card lane, contextual inspector, and a
    // review/Apply footer, rendered from the CastingWorkspaceSession read
    // models and issuing only session commands. The view owns no planner
    // state of its own — no second ledger, no targeting or budget logic.
    //
    // STATUS: the production planner view of the casting-first mode (the
    // player selects the mode in the UMM settings panel; runtime-test
    // workspace scenarios select it for their session). Apply routes the
    // accepted, freshly preflighted plan to the production dispatch
    // boundary and closes the view while the run proceeds.
    internal sealed class CastingWorkspaceScreenView : IDisposable
    {
        internal const string RootName = "KingmakerBuffPlanner.CastingWorkspaceRoot";

        private readonly CastingWorkspaceSession _session;
        private readonly Func<CastingWorkspaceInputs> _inputs;
        private readonly Func<CastingWorkspaceInputs> _freshInputs;
        private readonly Action _close;
        private Button _modeButton;
        private PlannerUiTheme _theme;
        private PlannerNativeThemeSurface _nativeTheme;
        private RectTransform _root;
        private RectTransform _buffGridContent;
        private InputField _buffSearch;
        private Button _pinnedAdd;
        private Button _scopeToggle;
        private Button _pinnedDone;
        private string _buffQuery = string.Empty;
        private PlannerSourceCategory _sourceCategory = PlannerSourceCategory.All;
        private readonly Dictionary<PlannerSourceCategory, Button> _categoryTabs =
            new Dictionary<PlannerSourceCategory, Button>();
        private WorkspaceView _lastView;
        private Text _castingsTitle;
        private RectTransform _cardContent;
        private RectTransform _inspectorContent;
        private Text _headerTitle;
        private Text _headerStatus;
        private Text _scopeLabel;
        private Text _footerBudget;
        private Text _footerResult;
        private Button _applyButton;
        private Button _readyOnlyButton;
        private Button _acceptButton;
        private Button _undoButton;
        private Button _saveButton;
        private Button _reloadButton;
        private Vector2 _cardScrollPosition;
        private RectTransform _routineBar;
        private int _uiLayer;
        private bool _disposed;

        internal CastingWorkspaceScreenView(
            StaticCanvas nativeCanvas,
            CastingWorkspaceSession session,
            Func<CastingWorkspaceInputs> inputsProvider,
            Func<CastingWorkspaceInputs> freshInputsProvider,
            Action close)
        {
            if (nativeCanvas == null)
                throw new ArgumentNullException("nativeCanvas");
            _session = session ?? throw new ArgumentNullException("session");
            _inputs = inputsProvider ?? throw new ArgumentNullException("inputsProvider");
            _freshInputs = freshInputsProvider ??
                throw new ArgumentNullException("freshInputsProvider");
            _close = close ?? throw new ArgumentNullException("close");
            _theme = PlannerUiTheme.Resolve(nativeCanvas);
            Build(nativeCanvas);
        }

        internal GameObject RootObject
        {
            get { return _root == null ? null : _root.gameObject; }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            _root = null;
        }

        // One refresh renders the shared read models and — by being an
        // actual on-screen presentation — feeds the review coordinator.
        internal void RefreshView()
        {
            if (_disposed) return;
            CastingWorkspaceInputs inputs = _inputs();
            WorkspaceView view = _session.BuildView(inputs);
            _session.PresentForReview(inputs);
            _headerTitle.text = "Casting Workspace — " + view.SelectedSourceCaption;
            // The routine's own gate and counts: what Apply of this routine
            // would do, whichever buff is selected below.
            _headerStatus.text = WorkspaceHeaderText.Describe(
                _session.RoutineDisplayName(view.SelectedRoutineId), view.RoutineCastingCount,
                view.RoutineReadyCount, view.SelectedRoutineGate.Allowed,
                view.SelectedRoutineGate.BlockingReasons.Count);
            _scopeLabel.text = view.EditingScopeLabel;
            _lastView = view;
            if (!_importAnnounced && _footerResult != null)
            {
                _importAnnounced = true;
                string import = DescribeImport(_session);
                if (import != null) _footerResult.text = import;
            }
            RebuildBuffGrid(view);
            RebuildCards(view);
            RebuildInspector(view);
            RebuildRoutineBar(view);
            RebuildFooter(view);
            PropagateUiLayer();
        }

        private static RectTransform RectOf(Component component)
        {
            return (RectTransform)component.transform;
        }

        // Runtime evidence for the physical-input scenario (batch 3,
        // section 10): where a named part of this view is on screen, in
        // Unity screen pixels (origin bottom left); null when absent,
        // inactive or off screen. Parts: "search", "buff-grid",
        // "tile:<sourceId>".
        internal Vector2? ScreenPointForRuntime(string part)
        {
            RectTransform rect = null;
            if (part == "search") rect = _buffSearch == null ? null : RectOf(_buffSearch);
            else if (part == "buff-grid")
            {
                ScrollRect scroll = BuffScroll();
                rect = scroll == null ? null : RectOf(scroll);
            }
            else if (part != null && part.StartsWith("tile:", StringComparison.Ordinal) && _buffGridContent != null)
                rect = _buffGridContent.Find("Source." + part.Substring(5)) as RectTransform;
            if (rect == null || !rect.gameObject.activeInHierarchy) return null;
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : canvas.worldCamera;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 center = RectTransformUtility.WorldToScreenPoint(camera, (corners[0] + corners[2]) * 0.5f);
            if (center.x < 1f || center.y < 1f || center.x > Screen.width - 1 || center.y > Screen.height - 1)
                return null;
            return center;
        }

        private ScrollRect BuffScroll()
        {
            return _buffGridContent == null ? null : _buffGridContent.GetComponentInParent<ScrollRect>();
        }

        internal string SearchTextForRuntime
        {
            get { return _buffSearch == null ? null : _buffSearch.text; }
        }

        internal bool SearchFocusedForRuntime
        {
            get { return _buffSearch != null && _buffSearch.isFocused; }
        }

        // "<sourceId>|<label>|<selected>" for every buff tile shown now.
        internal IList<string> VisibleSourcesForRuntime()
        {
            var tiles = new List<string>();
            if (_buffGridContent == null) return tiles;
            foreach (Transform child in _buffGridContent)
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
                ScrollRect scroll = BuffScroll();
                return scroll == null ? (float?)null : scroll.verticalNormalizedPosition;
            }
        }

        internal bool BuffGridOverflowsForRuntime
        {
            get
            {
                ScrollRect scroll = BuffScroll();
                if (scroll == null || scroll.content == null) return false;
                RectTransform viewport = scroll.viewport != null ? scroll.viewport : RectOf(scroll);
                return scroll.content.rect.height > viewport.rect.height + 1f;
            }
        }

        internal string SelectedSourceIdForRuntime
        {
            get { return _lastView == null ? null : _lastView.SelectedSourceId; }
        }

        // The legacy import summary is shown once, on the first refresh
        // after the session imported it.
        private bool _importAnnounced;

        private static string DescribeImport(CastingWorkspaceSession session)
        {
            if (session.LegacyImportBlocked)
                return "Your previous plan could not be imported (" +
                    session.LegacyImportBlockReason + "). It was NOT replaced: saving and " +
                    "Apply are blocked. Repair or restore that file, then press Reload to retry.";
            CastingImportReport report = session.ImportReport;
            if (report == null) return null;
            return "Imported " + report.ResultingCastingCount +
                (report.ResultingCastingCount == 1 ? " casting" : " castings") +
                " from your previous plan: " + report.ReadyCount + " ready, " +
                report.DraftCount + " need review" +
                (report.UnresolvedCasterCount == 0 ? string.Empty
                    : ", " + report.UnresolvedCasterCount + " without a caster") +
                (report.GroupReviewCount == 0 ? string.Empty
                    : ", " + report.GroupReviewCount + " group(s) to confirm") +
                (report.Warnings.Count == 0 ? string.Empty
                    : " · " + report.Warnings.Count + " warning(s)") +
                ". Your previous plan file was kept unchanged.";
        }

        private void Build(StaticCanvas nativeCanvas)
        {
            // Top-level canvas in the game's own service-window pattern
            // (ScreenSpaceCamera bound to the native UI camera), matching
            // FadeCanvas/StaticCanvas themselves. The previous structure —
            // a nested canvas parented under StaticCanvas — demonstrably
            // rendered in NO path (display or camera) across runs
            // casting-ws-root-200200 and casting-ws-layout-011500, while
            // the game's own camera-bound canvases render in both.
            _root = KingmakerUiFactory.CreateRect(RootName, null);
            Canvas nativeRootCanvas = nativeCanvas.GetComponent<Canvas>();
            Canvas canvas = _root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = nativeRootCanvas == null
                ? null : nativeRootCanvas.worldCamera;
            canvas.planeDistance = nativeRootCanvas == null
                ? 100f : nativeRootCanvas.planeDistance;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32000;
            _root.gameObject.AddComponent<GraphicRaycaster>();
            CanvasGroup group = _root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            KingmakerUiFactory.Stretch(_root);
            _nativeTheme = PlannerNativeThemeSurface.Attach(_root, nativeCanvas);
            // Theme surface stays attached for owned paper styling.
            // The native UI camera culls by layer: the game's own canvases
            // sit on the native canvas's layer, while factory-created
            // GameObjects default to layer 0 and are culled from every
            // camera-bound path (the invisibility across runs 200200
            // through 020500).
            _uiLayer = nativeCanvas.gameObject.layer;
            _root.gameObject.layer = _uiLayer;
            // Modal world-input blocker behind the frame.
            RectTransform blocker = KingmakerUiFactory.CreateRect("Blocker", _root);
            KingmakerUiFactory.AddPanel(blocker,
                new Color(0f, 0f, 0f, 0.55f));
            blocker.gameObject.AddComponent<GraphicRaycaster>();
            KingmakerUiFactory.Stretch(blocker);
            RectTransform frame = KingmakerUiFactory.CreateRect("Frame", _root);
            KingmakerUiFactory.AddFramedPanel(frame,
                _theme.ParchmentPanel, _theme.GoldAccent, 2f);
            KingmakerUiFactory.Stretch(frame, 24, 24, 24, 60);
            bool pageArt = ApplyNativePageArt(frame, nativeCanvas);
            BuildHeader(frame);
            BuildRoutineBar(frame);
            BuildLanes(frame);
            BuildFooter(frame);
            if (pageArt) LetPageShowThroughLanes(frame);
            PropagateUiLayer();
        }

        // Native page art (charter §6.1, docs/UI-END-GOAL.md): the planner
        // sits on the game's own spellbook page, as Bubble Buffs does. The
        // donor's sprite is borrowed onto OUR frame only — the native object
        // is never modified — and a missing donor keeps the parchment
        // fallback. Full-page art is not sliced, so it is stretched whole
        // onto the one page-sized surface and never onto nested panels.
        internal static readonly string[] PageArtLocators =
        {
            "ServiceWindow/SpellBook/BookBackground",
            "ServiceWindow/CharacterScreen/BookBackground"
        };

        private string _pageArtEvidence = "page=fallback;not-attempted";

        internal string PageArtEvidence { get { return _pageArtEvidence; } }

        private bool ApplyNativePageArt(RectTransform frame, StaticCanvas nativeCanvas)
        {
            try
            {
                foreach (string locator in PageArtLocators)
                {
                    Transform donor = nativeCanvas.transform.Find(locator);
                    Image image = donor == null ? null : donor.GetComponent<Image>();
                    if (image == null || image.sprite == null) continue;
                    Image target = frame.GetComponent<Image>();
                    target.sprite = image.sprite;
                    target.type = Image.Type.Simple;
                    target.preserveAspect = false;
                    target.color = Color.white;
                    Outline outline = frame.GetComponent<Outline>();
                    if (outline != null) outline.enabled = false;
                    _pageArtEvidence = "page=native;locator=" + locator +
                        ";sprite=" + image.sprite.name;
                    Debug.Log("[KBP-THEME] workspace page art " + _pageArtEvidence);
                    return true;
                }
                _pageArtEvidence = "page=fallback;no-donor-sprite";
            }
            catch (Exception exception)
            {
                _pageArtEvidence = "page=fallback;error=" + exception.GetType().Name;
            }
            Debug.LogWarning("[KBP-THEME] workspace page art " + _pageArtEvidence);
            return false;
        }

        // Over real page art the lanes become light framed boxes (Bubble
        // Buffs' look) instead of opaque parchment slabs.
        private void LetPageShowThroughLanes(RectTransform frame)
        {
            foreach (ScrollRect scroll in frame.GetComponentsInChildren<ScrollRect>(true))
            {
                Image panel = scroll.GetComponent<Image>();
                // No fill at all: even a light parchment fill stacked into a
                // heavy orange cast over the book (live frame qual-211523);
                // the outline alone frames the box.
                if (panel != null)
                    panel.color = new Color(panel.color.r, panel.color.g,
                        panel.color.b, 0f);
            }
            // The book art has transparent margins: header and footer text
            // sit on the dark world there, so they switch to light ink.
            foreach (Text text in new[] { _headerTitle, _headerStatus,
                _scopeLabel, _footerBudget, _footerResult })
                if (text != null) text.color = _theme.ButtonText;
        }

        // Every factory-created GameObject defaults to layer 0; rebuilt rows
        // add more. The native UI camera's culling mask includes the native
        // canvas layer only, so the whole owned tree must carry it to render.
        private void PropagateUiLayer()
        {
            if (_root == null) return;
            foreach (Transform node in _root.GetComponentsInChildren<Transform>(true))
                node.gameObject.layer = _uiLayer;
        }

        private void BuildHeader(RectTransform frame)
        {
            RectTransform header = KingmakerUiFactory.CreateRect("Header", frame);
            KingmakerUiFactory.SetAnchors(header, 0f, 1f, 1f, 1f);
            header.sizeDelta = new Vector2(0f, 52f);
            header.anchoredPosition = Vector2.zero;
            _headerTitle = KingmakerUiFactory.CreateText(
                "Title", header, _theme, "Casting Workspace", 22, TextAnchor.MiddleLeft);
            _headerTitle.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.Stretch(_headerTitle.rectTransform, 16, 12, 6, 4);
            _headerStatus = KingmakerUiFactory.CreateText(
                "Status", header, _theme, string.Empty, 16, TextAnchor.MiddleRight);
            _headerStatus.color = _theme.MutedBrownText;
            KingmakerUiFactory.Stretch(_headerStatus.rectTransform, 360, 16, 6, 4);
            _scopeLabel = KingmakerUiFactory.CreateText(
                "Scope", header, _theme, "Configure next casting", 15, TextAnchor.MiddleRight);
            _scopeLabel.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.SetAnchors(_scopeLabel.rectTransform, 0f, 0f, 1f, 0f);
            _scopeLabel.rectTransform.sizeDelta = new Vector2(0f, 18f);
            _scopeLabel.rectTransform.anchoredPosition = Vector2.zero;
        }

        private void BuildRoutineBar(RectTransform frame)
        {
            _routineBar = KingmakerUiFactory.CreateRect("RoutineBar", frame);
            KingmakerUiFactory.SetAnchors(_routineBar, 0f, 0.90f, 1f, 0.945f);
        }

        private void BuildLanes(RectTransform frame)
        {
            // Bubble Buffs-like composition (docs/UI-END-GOAL.md): an icon
            // grid of buffs across the top; the selected buff's castings —
            // the atomic unit — lower left; the inspector lower right.
            RectTransform buffs = KingmakerUiFactory.CreateRect("Buffs", frame);
            KingmakerUiFactory.SetAnchors(buffs, 0f, 0.565f, 1f, 0.885f);
            // Inset inside the book's printed page edges (titles clipped
            // against the left edge in live frame qual-211922).
            buffs.offsetMin = new Vector2(PageInset, 0f);
            buffs.offsetMax = new Vector2(-PageInset, 0f);
            Text buffTitle;
            _buffGridContent = BuildLanePanel(buffs, "Buffs", out buffTitle);
            _buffSearch = KingmakerUiFactory.CreateInputField(
                "BuffSearch", buffs, _theme, "Search buffs…");
            RectTransform searchRect = RectOf(_buffSearch);
            KingmakerUiFactory.SetAnchors(searchRect, 0.55f, 1f, 1f, 1f);
            searchRect.pivot = new Vector2(0.5f, 1f);
            searchRect.sizeDelta = new Vector2(0f, BuffBarHeight);
            searchRect.anchoredPosition = Vector2.zero;
            ScrollRect buffScroll = _buffGridContent.GetComponentInParent<ScrollRect>();
            if (buffScroll != null)
                KingmakerUiFactory.SetAnchors(RectOf(buffScroll), 0f, 0f, 1f, 1f, 0f, 0f, 0f, BuffBarHeight + 4f);
            foreach (Text text in _buffSearch.GetComponentsInChildren<Text>(true))
            {
                // The factory's 17px text with 5px insets was clipped to
                // nothing in a 22px field (live frame qual-205126).
                text.fontSize = 14;
                text.resizeTextMaxSize = 14;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                KingmakerUiFactory.Stretch(text.rectTransform, 8, 6, 1, 1);
            }
            _buffSearch.onValueChanged.AddListener(value =>
            {
                // Filtering is view-only: it never touches the session,
                // the draft or the document.
                _buffQuery = value ?? string.Empty;
                if (_lastView != null)
                {
                    RebuildBuffGrid(_lastView);
                    PropagateUiLayer();
                }
            });
            // Source-type tabs (Bubble Buffs and the classic catalogue):
            // view-only filters like the search, between the lane title and
            // the search field.
            float tabLeft = 0.12f;
            foreach (PlannerSourceCategory category in new[]
                {
                    PlannerSourceCategory.All, PlannerSourceCategory.Spells,
                    PlannerSourceCategory.Abilities, PlannerSourceCategory.Other
                })
            {
                PlannerSourceCategory captured = category;
                Button tab = KingmakerUiFactory.CreateButton("SourceTab." + captured, buffs,
                    _theme, captured.ToString(), () => Click(() =>
                    {
                        _sourceCategory = captured;
                        foreach (KeyValuePair<PlannerSourceCategory, Button> pair in _categoryTabs)
                            StyleTab(pair.Value, pair.Key == _sourceCategory);
                        if (_lastView != null)
                        {
                            RebuildBuffGrid(_lastView);
                            PropagateUiLayer();
                        }
                    }));
                RectTransform tabRect = RectOf(tab);
                KingmakerUiFactory.SetAnchors(tabRect, tabLeft, 1f, tabLeft + 0.1f, 1f);
                tabRect.pivot = new Vector2(0.5f, 1f);
                tabRect.sizeDelta = new Vector2(0f, BuffBarHeight);
                tabRect.anchoredPosition = Vector2.zero;
                foreach (Text text in tab.GetComponentsInChildren<Text>(true))
                {
                    // Thin 22px tabs with 13px text read as squashed bars
                    // (live frame casting-ws-qual-20260923-q2-03).
                    text.fontSize = 14;
                    text.resizeTextMaxSize = 14;
                    text.verticalOverflow = VerticalWrapMode.Overflow;
                }
                StyleTab(tab, captured == _sourceCategory);
                _categoryTabs[captured] = tab;
                tabLeft += 0.105f;
            }
            UnityEngine.Object.DestroyImmediate(
                _buffGridContent.GetComponent<VerticalLayoutGroup>());
            GridLayoutGroup grid =
                _buffGridContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(352f, 60f);
            grid.spacing = new Vector2(8f, 6f);
            grid.padding = new RectOffset(6, 6, 6, 6);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            RectTransform cards = KingmakerUiFactory.CreateRect("Cards", frame);
            // Castings on the left page, inspector on the right page: the
            // spine runs down the frame's center (live frame qual-215709
            // showed the castings lane text crossing the gutter).
            KingmakerUiFactory.SetAnchors(cards, 0f, 0.085f, 0.5f, 0.55f);
            cards.offsetMin = new Vector2(PageInset, 0f);
            cards.offsetMax = new Vector2(-PageInset * 0.6f, 0f);
            _cardContent = BuildLanePanel(cards, "Castings", out _castingsTitle);
            _scopeToggle = KingmakerUiFactory.CreateButton(
                "CastingsScope", cards, _theme, "Show whole routine", () => Click(() =>
                {
                    _session.ShowWholeRoutine = !_session.ShowWholeRoutine;
                    RefreshView();
                }));
            PinToTitleRow(RectOf(_scopeToggle));
            RectOf(_scopeToggle).anchorMin = new Vector2(0.68f, 1f);
            RectTransform inspector = KingmakerUiFactory.CreateRect("Inspector", frame);
            KingmakerUiFactory.SetAnchors(inspector, 0.5f, 0.085f, 1f, 0.55f);
            inspector.offsetMin = new Vector2(PageInset * 0.6f, 0f);
            inspector.offsetMax = new Vector2(-PageInset, 0f);
            Text inspectorTitle;
            _inspectorContent = BuildLanePanel(inspector, "Inspector", out inspectorTitle);
            // The primary action is pinned to the inspector's title row so
            // it is always visible (it sat below the fold at the end of the
            // scrolling inspector in live frame qual-220104). Add Casting
            // while configuring the next casting; Done while editing one.
            _pinnedAdd = KingmakerUiFactory.CreateButton(
                "AddCasting", inspector, _theme, "Add Casting", () => Click(() =>
                {
                    AuthoringEditResult result = _session.AddCastingFromDraft(_inputs());
                    if (!result.Applied)
                        _footerResult.text = "Add refused: " + WorkspaceRefusalText.Describe(result.Reason);
                    RefreshView();
                }));
            PinToTitleRow(RectOf(_pinnedAdd));
            _pinnedDone = KingmakerUiFactory.CreateButton(
                "DoneEditing", inspector, _theme, "Done — back to next casting",
                () => Click(() =>
                {
                    _session.FocusCasting(null);
                    RefreshView();
                }));
            PinToTitleRow(RectOf(_pinnedDone));
        }

        private const float PageInset = 44f;
        // Height of the buff lane's source tabs and search field.
        private const float BuffBarHeight = 26f;

        private static void PinToTitleRow(RectTransform rect)
        {
            KingmakerUiFactory.SetAnchors(rect, 0.52f, 1f, 1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 26f);
            rect.anchoredPosition = new Vector2(0f, 3f);
        }

        private RectTransform BuildLanePanel(RectTransform lane, string title,
            out Text label)
        {
            label = KingmakerUiFactory.CreateText(
                "LaneTitle", lane, _theme, title, 16, TextAnchor.MiddleLeft);
            label.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.SetAnchors(label.rectTransform, 0f, 1f, 1f, 1f);
            label.rectTransform.sizeDelta = new Vector2(0f, 22f);
            RectTransform content;
            ScrollRect scroll = KingmakerUiFactory.CreateScrollView(
                "Scroll", lane, _theme, out content, 12f);
            // The scroll view must FILL its lane below the title: left at
            // its default 100x100 centered rect it rendered as a small box
            // mid-lane in every live run (rehearsal-6, gseries-081000).
            KingmakerUiFactory.SetAnchors(RectOf(scroll), 0f, 0f, 1f, 1f,
                0f, 0f, 0f, 24f);
            // Content stays top-anchored (factory contract) and grows with
            // its rows so the lane scrolls to the final row instead of being
            // clipped to the viewport height.
            ContentSizeFitter fitter =
                content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return content;
        }

        private void BuildFooter(RectTransform frame)
        {
            RectTransform footer = KingmakerUiFactory.CreateRect("Footer", frame);
            KingmakerUiFactory.SetAnchors(footer, 0f, 0f, 1f, 0.06f);
            footer.offsetMin = Vector2.zero;
            footer.offsetMax = Vector2.zero;
            _footerBudget = KingmakerUiFactory.CreateText(
                "Budget", footer, _theme, string.Empty, 14, TextAnchor.MiddleLeft);
            // Lower-left third only: the budget never runs under the buttons
            // (the first starts at 34 percent of the width); long text wraps
            // and is truncated inside that area.
            _footerBudget.fontSize = 13;
            _footerBudget.horizontalOverflow = HorizontalWrapMode.Wrap;
            _footerBudget.verticalOverflow = VerticalWrapMode.Truncate;
            KingmakerUiFactory.SetAnchors(_footerBudget.rectTransform, 0f, 0f, 0.33f, 0.55f);
            _footerBudget.rectTransform.offsetMin = new Vector2(12f, 2f);
            _footerBudget.rectTransform.offsetMax = new Vector2(-4f, 0f);
            _footerResult = KingmakerUiFactory.CreateText(
                "Result", footer, _theme, string.Empty, 14, TextAnchor.MiddleLeft);
            _footerResult.color = _theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(_footerResult.rectTransform, 0f, 1f, 0.55f, 1f);
            _footerResult.rectTransform.sizeDelta = new Vector2(0f, 16f);
            _footerResult.rectTransform.anchoredPosition = Vector2.zero;
            _saveButton = KingmakerUiFactory.CreateButton(
                "Save", footer, _theme, "Save", () => Click(() =>
                {
                    // A refused save (a protected file) is told, never silent.
                    try
                    {
                        _session.Save();
                        _footerResult.text = "Candidate saved.";
                    }
                    catch (Exception exception)
                    {
                        _footerResult.text = PersistenceMessages.ForSaveFailure(exception);
                    }
                }));
            KingmakerUiFactory.SetAnchors(RectOf(_saveButton), 0.34f, 0.2f, 0.42f, 0.8f);
            _reloadButton = KingmakerUiFactory.CreateButton(
                "Reload", footer, _theme, "Reload", () => Click(() =>
                {
                    CastingPlanLoadStatus status = _session.Reload();
                    _footerResult.text = _session.LegacyImportBlocked
                        ? DescribeImport(_session)
                        : _session.ImportReport != null
                            ? DescribeImport(_session) : "Reloaded: " + status;
                    RefreshView();
                }));
            KingmakerUiFactory.SetAnchors(RectOf(_reloadButton), 0.43f, 0.2f, 0.51f, 0.8f);
            _undoButton = KingmakerUiFactory.CreateButton(
                "Undo", footer, _theme, "Undo", () => Click(() =>
                {
                    _session.Undo();
                    RefreshView();
                }));
            KingmakerUiFactory.SetAnchors(RectOf(_undoButton), 0.62f, 0.2f, 0.70f, 0.8f);
            _acceptButton = KingmakerUiFactory.CreateButton(
                "Accept", footer, _theme, "Accept Plan", () => Click(() =>
                {
                    bool accepted = _session.AcceptPresentedPlan(_inputs());
                    string reviewWarning = PersistenceMessages.ForReviewWarning(_session.ReviewStoreWarning);
                    _footerResult.text = !accepted
                        ? "Acceptance refused: plan changed or not presented."
                        : reviewWarning ?? "Plan accepted.";
                }));
            KingmakerUiFactory.SetAnchors(RectOf(_acceptButton), 0.71f, 0.15f, 0.83f, 0.85f);
            _applyButton = KingmakerUiFactory.CreateButton(
                "Apply", footer, _theme, "Review & Apply", () => Click(() =>
                    RunApply(CastingApplyMode.Ordinary)));
            KingmakerUiFactory.SetAnchors(RectOf(_applyButton), 0.84f, 0.15f, 0.97f, 0.85f);
            _readyOnlyButton = KingmakerUiFactory.CreateButton(
                "ReadyOnly", footer, _theme, "Ready Casts Only", () => Click(() =>
                    RunApply(CastingApplyMode.ReadyCastsOnly)));
            RectOf(_readyOnlyButton).anchorMin = new Vector2(0.62f, 0f);
            RectOf(_readyOnlyButton).anchorMax = new Vector2(0.97f, 0.12f);
            _readyOnlyButton.gameObject.SetActive(false);
            // Per-plan execution mode (saved with the plan): native
            // animated casting, or Instant.
            _modeButton = KingmakerUiFactory.CreateButton(
                "ExecutionMode", footer, _theme, ModeCaption(), () => Click(() =>
                {
                    _session.SetExecutionMode(
                        _session.ExecutionMode == "instant" ? "animated" : "instant");
                    SetModeCaption();
                    _footerResult.text = "Casting mode: " + _session.ExecutionMode +
                        " (Save to keep it).";
                }));
            KingmakerUiFactory.SetAnchors(RectOf(_modeButton), 0.52f, 0.2f, 0.61f, 0.8f);
            _footerResult.text = DescribeReadiness();
        }

        private string ModeCaption()
        {
            return _session.ExecutionMode == "instant" ? "Mode: Instant" : "Mode: Animated";
        }

        private void SetModeCaption()
        {
            Text caption = _modeButton == null ? null
                : _modeButton.GetComponentInChildren<Text>();
            if (caption != null) caption.text = ModeCaption();
        }

        // The last run result when there is one; otherwise whether native
        // casting is available in this session.
        private string DescribeReadiness()
        {
            if (!string.IsNullOrEmpty(_session.LastAttemptMessage))
                return "Last attempt: " + _session.LastAttemptMessage;
            CastingRunReport last = _session.LastRunReport;
            if (last != null)
                return "Last run: " + CastingRunPresentation.Describe(last,
                    _session.RoutineDisplayName(last.ScopeRoutineId), _session.CastingLabel);
            string disposition = _session.DispatchDisposition ?? string.Empty;
            if (disposition == "native-casting-enabled")
                return "Review the plan, press Accept Plan, then Review & Apply (or use the " +
                    "routine buttons).";
            if (disposition == "native-casting-busy")
                return "A routine is running; press its button again to stop it.";
            return "Native casting is not available in this session (" + disposition + ").";
        }

        private void RunApply(CastingApplyMode mode)
        {
            string name = _session.RoutineDisplayName(_session.SelectedRoutineId);
            CastingWorkspaceInputs inputs;
            try { inputs = _freshInputs(); }
            catch (Exception exception)
            {
                // A stale plan is never executed: without fresh discovery
                // nothing is submitted.
                _footerResult.text = name + " was not cast: the party state could not be " +
                    "refreshed (" + exception.Message + ").";
                return;
            }
            WorkspaceApplyResult result = _session.Apply(
                mode, _session.SelectedRoutineId, inputs);
            if (!result.Allowed && result.GateDecision != null &&
                !result.GateDecision.Allowed && mode == CastingApplyMode.Ordinary)
            {
                int notReady = _lastView == null ? 0
                    : Math.Max(0, _lastView.RoutineCastingCount - _lastView.RoutineReadyCount);
                _footerResult.text = "Apply blocked: " + (notReady == 1 ? "1 casting is" : notReady + " castings are") +
                    " not ready (see the cards). Fix them, or use Ready Casts Only to run the ready ones.";
                _readyOnlyButton.gameObject.SetActive(true);
                return;
            }
            if (result.Allowed && result.Dispatch != null && result.Dispatch.Submitted)
            {
                // The run proceeds in the world: the workspace closes so the
                // party can act, and the result is reported when it ends.
                _close();
                return;
            }
            _footerResult.text = CastingRunPresentation.DescribeRefusal(name, result);
            if (result.Dispatch != null && !result.Dispatch.Submitted &&
                result.Projection != null && result.Projection.Converted)
                _footerResult.text += " Would run " + result.Projection.Plan.Steps.Count +
                    (result.Projection.Plan.Steps.Count == 1 ? " cast" : " casts") +
                    " in order.";
        }

        private void Click(Action action)
        {
            PlannerNativeTheme.PlayClick(null);
            action();
        }

        private void RebuildBuffGrid(WorkspaceView view)
        {
            KingmakerUiFactory.DestroyChildren(_buffGridContent);
            if (view.Draft == null) return;
            IReadOnlyDictionary<string, int> counts =
                WorkspaceBuffSummary.CastingsBySource(
                    _session.Document.Castings, view.SelectedRoutineId);
            int shown = 0;
            foreach (WorkspaceSourceOption source in view.Draft.Sources)
            {
                // The selected buff stays visible even when filtered out.
                if (!source.Selected &&
                    (!WorkspaceSourceLabels.Matches(source, _buffQuery) ||
                     !WorkspaceSourceLabels.MatchesCategory(source, _sourceCategory))) continue;
                shown++;
                WorkspaceSourceOption captured = source;
                int count;
                counts.TryGetValue(captured.SourceId, out count);
                RectTransform rect = KingmakerUiFactory.CreateRect(
                    "Source." + captured.SourceId, _buffGridContent);
                Image background = KingmakerUiFactory.AddPanel(rect,
                    captured.Selected ? _theme.ParchmentRaised : _theme.ParchmentPanel);
                Button button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                button.onClick.AddListener(() => Click(() =>
                {
                    _session.SelectBuff(captured.SourceId);
                    _session.Draft.SourceId = captured.SourceId;
                    RefreshView();
                }));
                if (captured.Selected)
                {
                    RectTransform rule = KingmakerUiFactory.CreateRect("Selected", rect);
                    KingmakerUiFactory.SetAnchors(rule, 0f, 0f, 0f, 1f);
                    rule.sizeDelta = new Vector2(5f, 0f);
                    rule.pivot = new Vector2(0f, 0.5f);
                    KingmakerUiFactory.AddPanel(rule, _theme.GoldAccent).raycastTarget = false;
                }
                RectTransform iconRect = KingmakerUiFactory.CreateRect("Icon", rect);
                KingmakerUiFactory.SetAnchors(iconRect, 0f, 0f, 0f, 1f, 8f, 0f, 5f, 5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.sizeDelta = new Vector2(50f, -10f);
                iconRect.anchoredPosition = new Vector2(8f, 0f);
                Sprite icon = ResolveAbilityIcon(captured.IconAbility);
                Image iconImage = iconRect.gameObject.AddComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                iconImage.color = icon == null ? _theme.MutedBrownText : Color.white;
                Text name = KingmakerUiFactory.CreateText("Name", rect, _theme,
                    captured.Label, 15, TextAnchor.UpperLeft);
                name.fontStyle = captured.Selected ? FontStyle.Bold : FontStyle.Normal;
                KingmakerUiFactory.Stretch(name.rectTransform, 66, 8, 20, 4);
                string routineName = _session.RoutineDisplayName(view.SelectedRoutineId);
                Text castings = KingmakerUiFactory.CreateText("Count", rect, _theme,
                    count == 0 ? "no castings in " + routineName
                        : count + (count == 1 ? " casting" : " castings") +
                          " in " + routineName,
                    12, TextAnchor.LowerLeft);
                castings.color = count == 0 ? _theme.MutedBrownText : _theme.GreenSuccess;
                KingmakerUiFactory.Stretch(castings.rectTransform, 66, 8, 3, 36);
            }
            if (shown == 0)
            {
                string where = _sourceCategory == PlannerSourceCategory.All
                    ? string.Empty : " under " + _sourceCategory;
                Text none = KingmakerUiFactory.CreateText("NoMatch", _buffGridContent,
                    _theme, _buffQuery.Length == 0
                        ? "No buffs" + where + "."
                        : "No buff matches \"" + _buffQuery + "\"" + where + ".", 14,
                    TextAnchor.MiddleLeft);
                none.color = _theme.MutedBrownText;
            }
        }

        private static Sprite ResolveAbilityIcon(AbilityKey ability)
        {
            if (ability == null) return null;
            try
            {
                if (!string.IsNullOrWhiteSpace(ability.VariantGuid))
                {
                    BlueprintAbility concrete =
                        ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(
                            ability.VariantGuid);
                    if (concrete != null && concrete.Icon != null) return concrete.Icon;
                }
                BlueprintAbility parent =
                    ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(
                        ability.BaseAbilityGuid);
                return parent == null ? null : parent.Icon;
            }
            catch (Exception)
            {
                // A non-ability source (item, fact) simply shows no icon.
                return null;
            }
        }

        // A portrait tile: the primary hit area for choosing a caster or a
        // recipient (charter §6.2). Selected tiles carry a gold frame;
        // coverage tints follow Bubble Buffs' legend.
        private Button CreatePortraitTile(string name, Transform parent,
            string unitId, string label, bool selected, Color tint,
            UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = KingmakerUiFactory.CreateRect(name, parent);
            Image frame = KingmakerUiFactory.AddPanel(rect,
                selected ? _theme.GoldAccent : _theme.ParchmentPanel);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            if (action != null) button.onClick.AddListener(action);
            RectTransform picture = KingmakerUiFactory.CreateRect("Portrait", rect);
            KingmakerUiFactory.SetAnchors(picture, 0f, 0.2f, 1f, 1f, 4f, 4f, 2f, 4f);
            Sprite portrait = BuffPlannerScreenView.ResolvePortrait(unitId);
            Image image = picture.gameObject.AddComponent<Image>();
            image.sprite = portrait;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = portrait == null ? new Color(0f, 0f, 0f, 0.08f) : tint;
            Text caption = KingmakerUiFactory.CreateText("Label", rect, _theme,
                label, 13, TextAnchor.MiddleCenter);
            caption.color = selected ? _theme.ButtonText : _theme.DarkBrownText;
            caption.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            caption.resizeTextForBestFit = true;
            caption.resizeTextMinSize = 10;
            caption.resizeTextMaxSize = 13;
            KingmakerUiFactory.SetAnchors(caption.rectTransform, 0f, 0f, 1f, 0.2f, 2f, 2f, 1f, 0f);
            return button;
        }

        private static string SourceLabel(WorkspaceView view, string sourceId)
        {
            WorkspaceSourceOption match = view == null || view.Draft == null ? null :
                view.Draft.Sources.FirstOrDefault(source => string.Equals(
                    source.SourceId, sourceId, StringComparison.Ordinal));
            return match == null ? "unnamed buff" : match.Label;
        }

        private static string UnitName(WorkspaceView view, string unitId)
        {
            WorkspaceTargetOption match = view == null || view.Draft == null ? null :
                view.Draft.Targets.FirstOrDefault(target => string.Equals(
                    target.UnitId, unitId, StringComparison.Ordinal));
            return match == null ? unitId : match.DisplayName;
        }

        private RectTransform CreateTileRow(string name)
        {
            RectTransform row = KingmakerUiFactory.CreateRect(name, _inspectorContent);
            GridLayoutGroup grid = row.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(88f, 112f);
            grid.spacing = new Vector2(6f, 6f);
            grid.childAlignment = TextAnchor.UpperLeft;
            return row;
        }

        // Enhancements are compact chips (Extend, rods, metamagic) laid out
        // in rows; a selected chip is gold, an unselected one keeps the
        // native button look. Selected enhancements on a new casting are
        // requirements, never silently dropped.
        private RectTransform CreateChipRow(string name)
        {
            RectTransform row = KingmakerUiFactory.CreateRect(name, _inspectorContent);
            GridLayoutGroup grid = row.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(236f, 30f);
            grid.spacing = new Vector2(6f, 6f);
            grid.childAlignment = TextAnchor.UpperLeft;
            return row;
        }

        private void StyleChip(Button chip, bool selected)
        {
            Image image = chip.targetGraphic as Image;
            if (image != null)
                image.color = selected ? _theme.GoldAccent : Color.white;
            Transform labelNode = chip.transform.Find("Label");
            Text label = labelNode == null ? null : labelNode.GetComponent<Text>();
            if (label != null && selected)
                label.text = "● " + label.text;
        }

        private Color CoverageTint(WorkspaceRecipientCoverage coverage)
        {
            switch (coverage)
            {
                case WorkspaceRecipientCoverage.CoveredReady:
                    return new Color(0.55f, 1f, 0.55f, 1f);
                case WorkspaceRecipientCoverage.CoveredNotReady:
                    return new Color(1f, 0.85f, 0.40f, 1f);
                default:
                    return Color.white;
            }
        }

        private void RebuildCards(WorkspaceView view)
        {
            ScrollRect scroll = _cardContent.GetComponentInParent<ScrollRect>();
            if (scroll != null) _cardScrollPosition = scroll.normalizedPosition;
            KingmakerUiFactory.DestroyChildren(_cardContent);
            _castingsTitle.text = _session.ShowWholeRoutine
                ? "Every casting in " + view.SelectedRoutineId + " — each card is one cast"
                : "Castings of " + view.SelectedSourceCaption + " — each card is one cast";
            KingmakerUiFactory.SetButtonLabel(_scopeToggle, _session.ShowWholeRoutine
                ? "Only this buff" : "Show whole routine");
            if (view.Cards.Count == 0)
            {
                Text hint = KingmakerUiFactory.CreateText(
                    "EmptyHint", _cardContent, _theme,
                    "No castings of this buff yet.\n" +
                    "Choose who casts it and who receives it in the Inspector, " +
                    "then press Add Casting. Each casting is one cast of one " +
                    "buff and can be edited on its own.", 15, TextAnchor.UpperLeft);
                hint.color = _theme.MutedBrownText;
                hint.horizontalOverflow = HorizontalWrapMode.Wrap;
                KingmakerUiFactory.AddLayout(hint.rectTransform, 90f);
            }
            foreach (WorkspaceCastingCard card in view.Cards)
            {
                RectTransform entry = KingmakerUiFactory.CreateRect(
                    "Card." + card.CastingId, _cardContent);
                KingmakerUiFactory.AddFramedPanel(entry,
                    card.EditingFocus
                        ? _theme.ParchmentRaised : _theme.ParchmentPanel,
                    _theme.GoldAccent);
                KingmakerUiFactory.AddLayout(entry, 96f);
                AddCardPortrait(entry, "CasterPortrait", card.CasterUnitId, 8f);
                if (card.DirectTargetUnitId != null)
                {
                    Text arrow = KingmakerUiFactory.CreateText("Arrow", entry, _theme,
                        "→", 22, TextAnchor.MiddleCenter);
                    KingmakerUiFactory.SetAnchors(arrow.rectTransform, 0f, 0f, 0f, 1f);
                    arrow.rectTransform.pivot = new Vector2(0f, 0.5f);
                    arrow.rectTransform.sizeDelta = new Vector2(24f, 0f);
                    arrow.rectTransform.anchoredPosition = new Vector2(76f, 0f);
                    AddCardPortrait(entry, "TargetPortrait", card.DirectTargetUnitId, 102f);
                }
                else
                {
                    // Group casting: one cast, several predicted
                    // beneficiaries (small portraits); intended recipients
                    // outside predicted coverage stay visible in red —
                    // never silently covered by another casting.
                    Text arrow = KingmakerUiFactory.CreateText("Arrow", entry, _theme,
                        "⇉", 20, TextAnchor.MiddleCenter);
                    KingmakerUiFactory.SetAnchors(arrow.rectTransform, 0f, 0f, 0f, 1f);
                    arrow.rectTransform.pivot = new Vector2(0f, 0.5f);
                    arrow.rectTransform.sizeDelta = new Vector2(24f, 0f);
                    arrow.rectTransform.anchoredPosition = new Vector2(76f, 0f);
                    var shown = new List<KeyValuePair<string, bool>>();
                    foreach (string unit in card.PredictedBeneficiaryUnitIds ?? new string[0])
                        shown.Add(new KeyValuePair<string, bool>(unit, false));
                    foreach (string unit in card.CoverageGapUnitIds ?? new string[0])
                        shown.Add(new KeyValuePair<string, bool>(unit, true));
                    for (int index = 0; index < shown.Count && index < 6; index++)
                    {
                        RectTransform small = KingmakerUiFactory.CreateRect(
                            "Beneficiary." + shown[index].Key, entry);
                        KingmakerUiFactory.SetAnchors(small, 0f, 0.5f, 0f, 0.5f);
                        small.pivot = new Vector2(0f, 0.5f);
                        small.sizeDelta = new Vector2(30f, 38f);
                        small.anchoredPosition = new Vector2(
                            102f + (index % 3) * 32f, index < 3 ? 20f : -20f);
                        Sprite portrait = BuffPlannerScreenView.ResolvePortrait(shown[index].Key);
                        Image image = small.gameObject.AddComponent<Image>();
                        image.sprite = portrait;
                        image.preserveAspect = true;
                        image.raycastTarget = false;
                        image.color = portrait == null ? new Color(0f, 0f, 0f, 0.08f)
                            : shown[index].Value ? new Color(1f, 0.45f, 0.45f, 1f) : Color.white;
                    }
                }
                string coverage = card.CoverageSummary.Length == 0
                    ? string.Empty : " · " + card.CoverageSummary;
                string buffName = _session.ShowWholeRoutine
                    ? SourceLabel(view, card.SourceId) + ": " : string.Empty;
                Text title = KingmakerUiFactory.CreateText(
                    "Title", entry, _theme,
                    buffName + card.Headline + "   (" + card.Subtitle + ")", 16,
                    TextAnchor.MiddleLeft);
                title.fontStyle = FontStyle.Bold;
                KingmakerUiFactory.SetAnchors(title.rectTransform, 0f, 0.55f, 0.62f, 1f,
                    176f, 4f, 0f, 4f);
                Text status = KingmakerUiFactory.CreateText(
                    "Status", entry, _theme,
                    card.StatusLabel + coverage, 14, TextAnchor.UpperRight);
                KingmakerUiFactory.SetAnchors(status.rectTransform, 0.55f, 0.55f, 0.98f, 0.92f);
                Text detail = KingmakerUiFactory.CreateText(
                    "Detail", entry, _theme,
                    BuildCardDetail(card), 13, TextAnchor.UpperLeft);
                KingmakerUiFactory.SetAnchors(detail.rectTransform, 0f, 0.05f, 0.85f, 0.55f,
                    176f, 8f, 2f, 2f);
                Button edit = KingmakerUiFactory.CreateButton(
                    "Edit." + card.CastingId, entry, _theme, "Edit", () => Click(() =>
                    {
                        _session.FocusCasting(card.CastingId);
                        RefreshView();
                    }));
                KingmakerUiFactory.SetAnchors(RectOf(edit), 0.86f, 0.08f, 0.98f, 0.4f);
            }
            if (scroll != null) scroll.normalizedPosition = _cardScrollPosition;
        }

        private void AddCardPortrait(RectTransform entry, string name,
            string unitId, float x)
        {
            RectTransform picture = KingmakerUiFactory.CreateRect(name, entry);
            KingmakerUiFactory.SetAnchors(picture, 0f, 0f, 0f, 1f);
            picture.pivot = new Vector2(0f, 0.5f);
            picture.sizeDelta = new Vector2(66f, -12f);
            picture.anchoredPosition = new Vector2(x, 0f);
            Sprite portrait = BuffPlannerScreenView.ResolvePortrait(unitId);
            Image image = picture.gameObject.AddComponent<Image>();
            image.sprite = portrait;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = portrait == null ? new Color(0f, 0f, 0f, 0.08f) : Color.white;
        }

        private static string BuildCardDetail(WorkspaceCastingCard card)
        {
            var parts = new List<string>();
            if (card.DirectTargetUnitId != null)
                parts.Add("Target: " + (card.DirectTargetDisplayName ??
                    card.DirectTargetUnitId));
            if (card.OriginLabel.Length != 0) parts.Add(card.OriginLabel);
            if (card.EnhancementLabels.Count != 0)
                parts.Add("Enhancements: " + string.Join(", ", card.EnhancementLabels));
            if (card.CostLabels.Count != 0)
                parts.Add("Cost: " + string.Join(", ", card.CostLabels));
            if (card.CoverageGapDisplayNames.Count != 0)
                parts.Add("Outside coverage: " + string.Join(", ",
                    card.CoverageGapDisplayNames));
            // Player-facing reasons; the import reason is not repeated when the
            // review items themselves are listed.
            string[] reasons = card.ReadinessReasons
                .Where(code => card.ReviewItems.Count == 0 ||
                    !code.StartsWith("import-review-unresolved", StringComparison.Ordinal))
                .Select(WorkspaceReasonText.Describe).Distinct(StringComparer.Ordinal).ToArray();
            if (reasons.Length != 0)
                parts.Add("Why not ready: " + string.Join("; ", reasons));
            if (card.ReviewItems.Count != 0)
                parts.Add("Imported, needs your review: " + string.Join("; ", card.ReviewItems
                    .Select(WorkspaceReasonText.DescribeReviewItem).Distinct(StringComparer.Ordinal).ToArray()));
            if (card.ExecutionLimitation != null)
                parts.Add("Cannot run in this version: " +
                    CastingRunPresentation.DescribeLimitation(card.ExecutionLimitation));
            if (card.ExistingEffectNotes.Count != 0)
                parts.Add("Existing effect: " + string.Join("; ", card.ExistingEffectNotes
                    .Select(CastingRunPresentation.DescribeExistingEffectNote).ToArray()));
            if (card.LastRunOutcome != null) parts.Add("Last run: " + card.LastRunOutcome);
            return string.Join("  ·  ", parts);
        }

        private void RebuildInspector(WorkspaceView view)
        {
            bool editing = view.EditingScope == WorkspaceEditingScope.EditingSingleCasting;
            if (_pinnedAdd != null) _pinnedAdd.gameObject.SetActive(!editing);
            if (_pinnedDone != null) _pinnedDone.gameObject.SetActive(editing);
            KingmakerUiFactory.DestroyChildren(_inspectorContent);
            Text scope = KingmakerUiFactory.CreateText(
                "Scope", _inspectorContent, _theme, view.EditingScopeLabel, 16,
                TextAnchor.MiddleLeft);
            scope.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.AddLayout(scope.rectTransform, 30f);
            RebuildImportNotices();
            if (view.EditingScope == WorkspaceEditingScope.EditingSingleCasting)
            {
                RebuildFocusedCastingEditor(view);
                return;
            }
            RebuildDraftEditor(view);
        }

        // Review L1: plan-wide legacy constraints are shown with an explicit,
        // undoable acknowledgement; until then no apply mode runs.
        private void RebuildImportNotices()
        {
            IReadOnlyList<string> pending = _session.PendingImportNotices;
            if (pending.Count == 0) return;
            AddInspectorCaption("Imported plan-wide constraints (" + pending.Count + ")");
            Text notices = KingmakerUiFactory.CreateText(
                "ImportNotices", _inspectorContent, _theme,
                string.Join("\n", pending.ToArray()) +
                "\nApply is refused until these are acknowledged.",
                12, TextAnchor.UpperLeft);
            notices.color = _theme.MutedBrownText;
            KingmakerUiFactory.AddLayout(notices.rectTransform, 18f * (pending.Count + 1));
            Button acknowledge = KingmakerUiFactory.CreateButton(
                "AcknowledgeImportNotices", _inspectorContent, _theme,
                "Acknowledge imported constraints", () => Click(() =>
                {
                    AuthoringEditResult result = _session.AcknowledgeImportNotices();
                    SurfaceRefusal(result, "acknowledge");
                    if (result.Applied)
                        _footerResult.text = "Acknowledged (Undo reverts): " + result.Scope;
                    RefreshView();
                }));
            KingmakerUiFactory.AddLayout(RectOf(acknowledge), 30f);
        }

        private void RebuildFocusedCastingEditor(WorkspaceView view)
        {
            Domain.Authoring.PlannedCasting focused =
                _session.Document.Castings.FirstOrDefault(casting =>
                    casting != null && string.Equals(casting.CastingId,
                        _session.EditingFocusCastingId, StringComparison.Ordinal));
            if (focused == null)
            {
                Text missing = KingmakerUiFactory.CreateText(
                    "Missing", _inspectorContent, _theme,
                    "Focused casting is absent from the document.", 13,
                    TextAnchor.UpperLeft);
                missing.color = _theme.MutedBrownText;
                KingmakerUiFactory.AddLayout(missing.rectTransform, 34f);
                return;
            }
            if (focused.Provenance != null &&
                focused.Provenance.UnresolvedReviewItems.Count != 0)
            {
                AddInspectorCaption("Imported: needs review");
                Text items = KingmakerUiFactory.CreateText(
                    "ImportReviewItems", _inspectorContent, _theme,
                    string.Join("\n", focused.Provenance.UnresolvedReviewItems.ToArray()) +
                    "\nThis casting cannot run until the review is resolved.",
                    12, TextAnchor.UpperLeft);
                items.color = _theme.MutedBrownText;
                KingmakerUiFactory.AddLayout(items.rectTransform,
                    18f * (focused.Provenance.UnresolvedReviewItems.Count + 1));
                Button resolve = KingmakerUiFactory.CreateButton(
                    "ResolveImportReview", _inspectorContent, _theme,
                    "Resolve review (keep current choices)", () => Click(() =>
                    {
                        AuthoringEditResult result = _session.ResolveFocusedImportReview();
                        SurfaceRefusal(result, "resolve review");
                        if (result.Applied)
                            _footerResult.text = "Resolved (Undo reverts): " + result.Scope;
                        RefreshView();
                    }));
                KingmakerUiFactory.AddLayout(RectOf(resolve), 30f);
            }
            bool directRecord = focused.TargetMode ==
                Domain.Authoring.CastingTargetMode.DirectTarget;
            AddInspectorCaption(directRecord
                ? "Retarget (direct)" : "Group targeting");
            if (directRecord)
            {
                RectTransform retargetRow = CreateTileRow("RetargetTiles");
                foreach (WorkspaceTargetOption target in view.Draft.Targets)
                {
                    WorkspaceTargetOption captured = target;
                    bool selected = string.Equals(focused.DirectTargetUnitId,
                        captured.UnitId, StringComparison.Ordinal);
                    CreatePortraitTile("Target." + captured.UnitId, retargetRow,
                        captured.UnitId, captured.DisplayName, selected, Color.white,
                        () => Click(() =>
                        {
                            AuthoringEditResult result = _session
                                .SetFocusedTargeting(
                                    Domain.Authoring.CastingTargetMode.DirectTarget,
                                    captured.UnitId, null, null);
                            SurfaceRefusal(result, "retarget");
                            RefreshView();
                        }));
                }
            }
            else
            {
                // Group records edit origin and coverage through the
                // group-aware command — never direct-target cloning
                // (review G2).
                Button casterOrigin = KingmakerUiFactory.CreateButton(
                    "FocusedOrigin.Caster", _inspectorContent, _theme,
                    focused.Origin != null && focused.Origin.IsCasterCentered
                        ? "[x] Origin: caster" : "[  ] Origin: caster",
                    () => Click(() =>
                    {
                        AuthoringEditResult result = _session
                            .SetFocusedTargeting(
                                Domain.Authoring.CastingTargetMode.CasterCenteredOrigin,
                                null, null, focused.RequiredCoverageUnitIds);
                        SurfaceRefusal(result, "origin");
                        RefreshView();
                    }));
                KingmakerUiFactory.AddLayout(RectOf(casterOrigin), 30f);
                RectTransform focusedOriginRow = CreateTileRow("FocusedOriginTiles");
                foreach (WorkspaceOriginOption origin in view.FocusedOrigins)
                {
                    WorkspaceOriginOption captured = origin;
                    CreatePortraitTile("FocusedOrigin." + captured.AnchorUnitId,
                        focusedOriginRow, captured.AnchorUnitId,
                        "Origin: " + UnitName(view, captured.AnchorUnitId),
                        captured.Selected, Color.white,
                        () => Click(() =>
                        {
                            AuthoringEditResult result = _session
                                .SetFocusedTargeting(
                                    Domain.Authoring.CastingTargetMode.AnchoredOrigin,
                                    null, captured.AnchorUnitId,
                                    focused.RequiredCoverageUnitIds);
                            SurfaceRefusal(result, "origin");
                            RefreshView();
                        }));
                }
                AddInspectorCaption("Required coverage");
                RectTransform focusedCoverageRow = CreateTileRow("FocusedCoverageTiles");
                foreach (WorkspaceTargetOption target in view.Draft.Targets)
                {
                    WorkspaceTargetOption captured = target;
                    bool covered = focused.RequiredCoverageUnitIds.Contains(
                        captured.UnitId);
                    CreatePortraitTile("FocusedCoverage." + captured.UnitId,
                        focusedCoverageRow, captured.UnitId, captured.DisplayName,
                        covered, Color.white,
                        () => Click(() =>
                        {
                            var coverage = focused.RequiredCoverageUnitIds
                                .Where(unitId => !string.Equals(unitId,
                                    captured.UnitId, StringComparison.Ordinal))
                                .ToList();
                            if (!covered) coverage.Add(captured.UnitId);
                            AuthoringEditResult result = _session
                                .SetFocusedTargeting(focused.TargetMode, null,
                                    focused.Origin == null ||
                                        focused.Origin.IsCasterCentered
                                        ? null
                                        : focused.Origin.AnchorUnitId,
                                    coverage);
                            SurfaceRefusal(result, "coverage");
                            RefreshView();
                        }));
                }
            }
            AddInspectorCaption("Enhancements (this casting)");
            RectTransform focusedChips = CreateChipRow("FocusedEnhancementChips");
            foreach (WorkspaceEnhancementOption enhancement in
                view.FocusedEnhancements)
            {
                WorkspaceEnhancementOption captured = enhancement;
                bool selected = focused.Enhancements.Any(selection =>
                    selection != null && string.Equals(
                        selection.EnhancementId,
                        captured.EnhancementId,
                        StringComparison.Ordinal));
                Button toggle = KingmakerUiFactory.CreateButton(
                    "FocusedEnhancement." + captured.EnhancementId,
                    focusedChips, _theme, captured.Label,
                    () => Click(() =>
                    {
                        var selections = focused.Enhancements
                            .Where(selection => selection != null &&
                                !string.Equals(selection.EnhancementId,
                                    captured.EnhancementId,
                                    StringComparison.Ordinal))
                            .ToList();
                        if (!selected)
                            selections.Add(
                                new Domain.Authoring.AuthoredEnhancementSelection(
                                    captured.EnhancementId, true, null));
                        ApplyFocusedEdit(
                            focused.WithEnhancementSelections(selections));
                    }));
                StyleChip(toggle, selected);
            }
            if (view.FocusedEnhancements.Count == 0)
            {
                Text none = KingmakerUiFactory.CreateText(
                    "NoFocusedEnhancement", _inspectorContent, _theme,
                    "None available for this casting's caster and ability.",
                    13, TextAnchor.MiddleLeft);
                none.color = _theme.MutedBrownText;
                KingmakerUiFactory.AddLayout(none.rectTransform, 26f);
            }
            AddInspectorCaption("Casting state");
            Button disable = KingmakerUiFactory.CreateButton(
                "Disable", _inspectorContent, _theme, "Disable", () => Click(() =>
                {
                    SurfaceRefusal(_session.SetFocusedCastingState(
                        Domain.Authoring.CastingAuthoringState.Disabled), "disable");
                    RefreshView();
                }));
            KingmakerUiFactory.AddLayout(RectOf(disable), 30f);
            Button enable = KingmakerUiFactory.CreateButton(
                "Enable", _inspectorContent, _theme, "Mark Ready", () => Click(() =>
                {
                    SurfaceRefusal(_session.SetFocusedCastingState(
                        Domain.Authoring.CastingAuthoringState.Ready), "mark ready");
                    RefreshView();
                }));
            KingmakerUiFactory.AddLayout(RectOf(enable), 30f);
            Button remove = KingmakerUiFactory.CreateButton(
                "Remove", _inspectorContent, _theme, "Remove", () => Click(() =>
                {
                    _session.RemoveFocusedCasting();
                    RefreshView();
                }));
            KingmakerUiFactory.AddLayout(RectOf(remove), 30f);
        }

        // A denied operation must explain itself, not silently redraw the
        // unchanged screen (review G2).
        private void SurfaceRefusal(AuthoringEditResult result, string action)
        {
            if (result != null && !result.Applied)
                _footerResult.text = action + " refused: " + WorkspaceRefusalText.Describe(result.Reason);
            else if (result != null && result.Applied &&
                !string.IsNullOrEmpty(result.Reason))
                _footerResult.text = action + ": " + result.Reason;
        }

        private void ApplyFocusedEdit(Domain.Authoring.PlannedCasting replacement)
        {
            AuthoringEditResult result = _session.UpdateFocusedCasting(replacement);
            if (!result.Applied)
                _footerResult.text = "Edit refused: " + WorkspaceRefusalText.Describe(result.Reason);
            RefreshView();
        }

        // The next-casting draft editor: every control writes ONLY the
        // session draft and canonical session commands — never a second
        // ledger (review R1).
        private void RebuildDraftEditor(WorkspaceView view)
        {
            WorkspaceDraftView draft = view.Draft;
            if (draft == null)
            {
                Text missing = KingmakerUiFactory.CreateText(
                    "Missing", _inspectorContent, _theme,
                    "Discovery produced no catalogue for the draft editor.",
                    13, TextAnchor.UpperLeft);
                missing.color = _theme.MutedBrownText;
                KingmakerUiFactory.AddLayout(missing.rectTransform, 48f);
                return;
            }
            Text buffLine = KingmakerUiFactory.CreateText("DraftBuff", _inspectorContent,
                _theme, "Buff: " + view.SelectedSourceCaption +
                "  (choose another in the grid above)", 14, TextAnchor.MiddleLeft);
            KingmakerUiFactory.AddLayout(buffLine.rectTransform, 24f);
            AddInspectorCaption("Cast by");
            if (draft.CapableCasters.Count == 0)
            {
                Text none = KingmakerUiFactory.CreateText(
                    "NoCaster", _inspectorContent, _theme,
                    "No eligible caster for this buff.", 13, TextAnchor.MiddleLeft);
                none.color = _theme.MutedBrownText;
                KingmakerUiFactory.AddLayout(none.rectTransform, 26f);
            }
            RectTransform casterRow = CreateTileRow("CasterTiles");
            foreach (WorkspaceCasterRow caster in draft.CapableCasters)
            {
                WorkspaceCasterRow captured = caster;
                bool selected = string.Equals(draft.CasterUnitId,
                    captured.UnitId, StringComparison.Ordinal);
                CreatePortraitTile("DraftCaster." + captured.UnitId, casterRow,
                    captured.UnitId,
                    string.IsNullOrEmpty(captured.DisplayName)
                        ? captured.UnitId : captured.DisplayName,
                    selected, captured.ReadinessReasons.Count == 0
                        ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f),
                    () => Click(() =>
                    {
                        _session.ChooseDraftCaster(captured.UnitId);
                        RefreshView();
                    }));
            }
            AddInspectorCaption("Targeting");
            bool direct = draft.TargetMode ==
                Domain.Authoring.CastingTargetMode.DirectTarget;
            Button mode = KingmakerUiFactory.CreateButton(
                "Mode", _inspectorContent, _theme,
                direct ? "Mode: single target" : "Mode: group from origin",
                () => Click(() =>
                {
                    // One coherent shape operation: the session command
                    // clears/rebuilds the incompatible fields (review F3).
                    AuthoringEditResult result = direct
                        ? _session.SetDraftTargeting(
                            Domain.Authoring.CastingTargetMode.CasterCenteredOrigin,
                            null, null, null)
                        : _session.SetDraftTargeting(
                            Domain.Authoring.CastingTargetMode.DirectTarget,
                            draft.DirectTargetUnitId, null, null);
                    SurfaceRefusal(result, "mode");
                    RefreshView();
                }));
            KingmakerUiFactory.AddLayout(RectOf(mode), 30f);
            if (direct)
            {
                AddInspectorCaption("Cast on");
                RectTransform targetRow = CreateTileRow("TargetTiles");
                foreach (WorkspaceTargetOption target in draft.Targets)
                {
                    WorkspaceTargetOption captured = target;
                    bool selected = string.Equals(draft.DirectTargetUnitId,
                        captured.UnitId, StringComparison.Ordinal);
                    bool illegal = captured.Legal == false;
                    Button tile = CreatePortraitTile("DraftTarget." + captured.UnitId, targetRow,
                        captured.UnitId, captured.DisplayName, selected,
                        illegal ? new Color(1f, 0.45f, 0.45f, 1f)
                            : CoverageTint(WorkspaceBuffSummary.CoverageFor(
                                view.SelectedBuffCards, view.SelectedSourceId,
                                view.SelectedRoutineId, captured.UnitId)),
                        () => Click(() =>
                        {
                            _session.SetDraftTargeting(
                                Domain.Authoring.CastingTargetMode.DirectTarget,
                                captured.UnitId, null, null);
                            RefreshView();
                        }));
                    // An illegal recipient cannot become a casting target.
                    if (illegal) tile.interactable = false;
                }
                Text legend = KingmakerUiFactory.CreateText("CoverageLegend",
                    _inspectorContent, _theme,
                    "Green: already has a Ready casting of this buff · " +
                    "Amber: has one that is not Ready · Red: this caster cannot target them",
                    12, TextAnchor.MiddleLeft);
                legend.color = _theme.MutedBrownText;
                KingmakerUiFactory.AddLayout(legend.rectTransform, 20f);
            }
            else
            {
                Button casterOrigin = KingmakerUiFactory.CreateButton(
                    "Origin.Caster", _inspectorContent, _theme,
                    string.IsNullOrEmpty(draft.OriginAnchorUnitId)
                        ? "[x] Origin: caster" : "[  ] Origin: caster",
                    () => Click(() =>
                    {
                        AuthoringEditResult result = _session.SetDraftTargeting(
                            Domain.Authoring.CastingTargetMode.CasterCenteredOrigin,
                            null, null, _session.Draft.RequiredCoverageUnitIds);
                        SurfaceRefusal(result, "origin");
                        RefreshView();
                    }));
                KingmakerUiFactory.AddLayout(RectOf(casterOrigin), 30f);
                RectTransform originRow = CreateTileRow("OriginTiles");
                foreach (WorkspaceOriginOption origin in draft.Origins)
                {
                    WorkspaceOriginOption captured = origin;
                    CreatePortraitTile("Origin." + captured.AnchorUnitId, originRow,
                        captured.AnchorUnitId,
                        "Origin: " + UnitName(view, captured.AnchorUnitId),
                        captured.Selected, Color.white,
                        () => Click(() =>
                        {
                            AuthoringEditResult result = _session.SetDraftTargeting(
                                Domain.Authoring.CastingTargetMode.AnchoredOrigin,
                                null, captured.AnchorUnitId,
                                _session.Draft.RequiredCoverageUnitIds);
                            SurfaceRefusal(result, "origin");
                            RefreshView();
                        }));
                }
                AddInspectorCaption("Required coverage (intended recipients)");
                RectTransform coverageRow = CreateTileRow("CoverageTiles");
                foreach (WorkspaceTargetOption target in draft.Targets)
                {
                    WorkspaceTargetOption captured = target;
                    bool covered = _session.Draft.RequiredCoverageUnitIds
                        .Contains(captured.UnitId);
                    CreatePortraitTile("DraftCoverage." + captured.UnitId, coverageRow,
                        captured.UnitId, captured.DisplayName, covered, Color.white,
                        () => Click(() =>
                        {
                            var coverage = _session.Draft
                                .RequiredCoverageUnitIds
                                .Where(unitId => !string.Equals(unitId,
                                    captured.UnitId, StringComparison.Ordinal))
                                .ToList();
                            if (!covered) coverage.Add(captured.UnitId);
                            // H2b: a coverage edit preserves the CURRENT
                            // group mode and origin — a caster-centered
                            // spell never needs an anchor.
                            AuthoringEditResult result = _session
                                .SetDraftTargeting(draft.TargetMode, null,
                                    string.IsNullOrEmpty(draft.OriginAnchorUnitId)
                                        ? null : draft.OriginAnchorUnitId,
                                    coverage);
                            SurfaceRefusal(result, "coverage");
                            RefreshView();
                        }));
                }
                AddInspectorCaption("Switch back to single target");
                if (!string.IsNullOrEmpty(draft.RememberedDirectTargetUnitId))
                {
                    Button restore = KingmakerUiFactory.CreateButton(
                        "Mode.Restore", _inspectorContent, _theme,
                        "Restore recipient: " + draft.RememberedDirectTargetUnitId,
                        () => Click(() =>
                        {
                            AuthoringEditResult result = _session.SetDraftTargeting(
                                Domain.Authoring.CastingTargetMode.DirectTarget,
                                null, null, null);
                            SurfaceRefusal(result, "mode");
                            RefreshView();
                        }));
                    KingmakerUiFactory.AddLayout(RectOf(restore), 30f);
                }
                foreach (WorkspaceTargetOption target in draft.Targets)
                {
                    WorkspaceTargetOption captured = target;
                    Button pick = KingmakerUiFactory.CreateButton(
                        "Mode.PickReturn." + captured.UnitId, _inspectorContent,
                        _theme,
                        "Single target on " + captured.DisplayName,
                        () => Click(() =>
                        {
                            AuthoringEditResult result = _session.SetDraftTargeting(
                                Domain.Authoring.CastingTargetMode.DirectTarget,
                                captured.UnitId, null, null);
                            SurfaceRefusal(result, "mode");
                            RefreshView();
                        }));
                    KingmakerUiFactory.AddLayout(RectOf(pick), 30f);
                }
            }
            AddInspectorCaption("Enhancements");
            if (draft.Enhancements.Count == 0)
            {
                Text none = KingmakerUiFactory.CreateText(
                    "NoEnhancement", _inspectorContent, _theme,
                    "None available for this caster and ability.", 13,
                    TextAnchor.MiddleLeft);
                none.color = _theme.MutedBrownText;
                KingmakerUiFactory.AddLayout(none.rectTransform, 26f);
            }
            RectTransform draftChips = CreateChipRow("EnhancementChips");
            foreach (WorkspaceEnhancementOption enhancement in
                draft.Enhancements)
            {
                WorkspaceEnhancementOption captured = enhancement;
                Button toggle = KingmakerUiFactory.CreateButton(
                    "Enhancement." + captured.EnhancementId, draftChips,
                    _theme, captured.Label,
                    () => Click(() =>
                    {
                        if (captured.Selected)
                            _session.Draft.Enhancements.RemoveAll(selection =>
                                selection != null && string.Equals(
                                    selection.EnhancementId,
                                    captured.EnhancementId,
                                    StringComparison.Ordinal));
                        else
                            _session.Draft.Enhancements.Add(
                                new Domain.Authoring.AuthoredEnhancementSelection(
                                    captured.EnhancementId, true, null));
                        RefreshView();
                    }));
                StyleChip(toggle, captured.Selected);
            }
            AddInspectorCaption("State");
            Button state = KingmakerUiFactory.CreateButton(
                "State", _inspectorContent, _theme,
                "State: " + draft.State, () => Click(() =>
                {
                    _session.Draft.State = draft.State ==
                        Domain.Authoring.CastingAuthoringState.Ready
                        ? Domain.Authoring.CastingAuthoringState.Draft
                        : Domain.Authoring.CastingAuthoringState.Ready;
                    RefreshView();
                }));
            KingmakerUiFactory.AddLayout(RectOf(state), 30f);
        }

        private void AddInspectorCaption(string caption)
        {
            Text label = KingmakerUiFactory.CreateText(
                "Caption." + caption, _inspectorContent, _theme, caption, 14,
                TextAnchor.MiddleLeft);
            label.fontStyle = FontStyle.Bold;
            label.color = _theme.MutedBrownText;
            KingmakerUiFactory.AddLayout(label.rectTransform, 24f);
        }

        private void RebuildRoutineBar(WorkspaceView view)
        {
            if (_routineBar == null) return;
            KingmakerUiFactory.DestroyChildren(_routineBar);
            foreach (string routineId in view.RoutineIds)
            {
                string captured = routineId;
                bool selected = string.Equals(view.SelectedRoutineId, captured,
                    StringComparison.Ordinal);
                int count = _session.Document.Castings.Count(value => value != null &&
                    string.Equals(value.RoutineId, captured, StringComparison.Ordinal));
                Button tab = KingmakerUiFactory.CreateButton(
                    "Routine." + captured, _routineBar, _theme,
                    _session.RoutineDisplayName(captured) + " (" + count + ")",
                    () => Click(() =>
                    {
                        _session.SelectRoutine(captured);
                        RefreshView();
                    }));
                StyleTab(tab, selected);
                RectOf(tab).pivot = new Vector2(0f, 0.5f);
                RectOf(tab).anchorMin = new Vector2(0f, 0.1f);
                RectOf(tab).anchorMax = new Vector2(0f, 0.9f);
                RectOf(tab).sizeDelta = new Vector2(170f, 0f);
                RectOf(tab).anchoredPosition = new Vector2(
                    16f + view.RoutineIds.TakeWhile(id =>
                        !string.Equals(id, captured, StringComparison.Ordinal))
                        .Count() * 178f, 0f);
            }
        }

        // A selected tab (routine or source type) is gold with a bold label
        // (the chip convention); the others keep the native button look.
        private void StyleTab(Button tab, bool selected)
        {
            Image image = tab.targetGraphic as Image;
            if (image != null) image.color = selected ? _theme.GoldAccent : Color.white;
            Transform labelNode = tab.transform.Find("Label");
            Text label = labelNode == null ? null : labelNode.GetComponent<Text>();
            if (label != null) label.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
        }

        private void RebuildFooter(WorkspaceView view)
        {
            IReadOnlyList<string> lines = WorkspaceBudgetRow.FooterLines(view.BudgetRows);
            _footerBudget.text = lines.Count == 0
                ? "No resource demand yet."
                : string.Join("   ", lines.ToArray());
        }
    }
}
