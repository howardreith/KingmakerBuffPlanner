using System;
using System.Collections.Generic;
using Kingmaker.UI;
using KingmakerBuffPlanner.Domain.Authoring;
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
    // STATUS: source-integrated and compiled against the installed Unity
    // 2018.4 legacy UI contracts; not yet visually or audibly qualified in
    // the running game (native donor qualification remains open), and its
    // composition-root wiring behind CastingWorkspaceDevSelection is the
    // remaining integration step recorded in the migration status.
    internal sealed class CastingWorkspaceScreenView : IDisposable
    {
        internal const string RootName = "KingmakerBuffPlanner.CastingWorkspaceRoot";

        private readonly CastingWorkspaceSession _session;
        private readonly Func<CastingWorkspaceInputs> _inputs;
        private readonly Action _close;
        private PlannerUiTheme _theme;
        private PlannerNativeThemeSurface _nativeTheme;
        private RectTransform _root;
        private RectTransform _casterContent;
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
        private Vector2 _cardScrollPosition;
        private int _uiLayer;
        private bool _disposed;

        internal CastingWorkspaceScreenView(
            StaticCanvas nativeCanvas,
            CastingWorkspaceSession session,
            Func<CastingWorkspaceInputs> inputsProvider,
            Action close)
        {
            if (nativeCanvas == null)
                throw new ArgumentNullException("nativeCanvas");
            _session = session ?? throw new ArgumentNullException("session");
            _inputs = inputsProvider ?? throw new ArgumentNullException("inputsProvider");
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
            _headerTitle.text = "Casting Workspace — " +
                (string.IsNullOrEmpty(view.SelectedSourceId)
                    ? "no buff selected" : view.SelectedSourceId);
            int ready = 0;
            foreach (WorkspaceCastingCard card in view.Cards)
                if (card.Readiness == ResolvedCastingReadiness.Ready) ready++;
            _headerStatus.text = "Routine " + view.SelectedRoutineId +
                " · " + ready + "/" + view.Cards.Count + " ready · one-pass " +
                (view.OnePassGate.Allowed ? "clear" :
                    view.OnePassGate.BlockingReasons.Count + " blocked");
            _scopeLabel.text = view.EditingScopeLabel;
            RebuildCasters(view);
            RebuildCards(view);
            RebuildInspector(view);
            RebuildFooter(view);
            PropagateUiLayer();
        }

        private static RectTransform RectOf(Component component)
        {
            return (RectTransform)component.transform;
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
            BuildHeader(frame);
            BuildLanes(frame);
            BuildFooter(frame);
            PropagateUiLayer();
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

        private void BuildLanes(RectTransform frame)
        {
            // Caster lane (left).
            RectTransform casters = KingmakerUiFactory.CreateRect("Casters", frame);
            KingmakerUiFactory.SetAnchors(casters, 0f, 0.06f, 0.28f, 0.88f);
            casters.offsetMin = new Vector2(10f, 0f);
            casters.offsetMax = new Vector2(-4f, 0f);
            _casterContent = BuildLanePanel(casters, "Casters");
            // Casting-card lane (center).
            RectTransform cards = KingmakerUiFactory.CreateRect("Cards", frame);
            KingmakerUiFactory.SetAnchors(cards, 0.28f, 0.06f, 0.72f, 0.88f);
            cards.offsetMin = new Vector2(4f, 0f);
            cards.offsetMax = new Vector2(-4f, 0f);
            _cardContent = BuildLanePanel(cards, "Castings");
            // Inspector (right).
            RectTransform inspector = KingmakerUiFactory.CreateRect("Inspector", frame);
            KingmakerUiFactory.SetAnchors(inspector, 0.72f, 0.06f, 1f, 0.88f);
            inspector.offsetMin = new Vector2(4f, 0f);
            inspector.offsetMax = new Vector2(-10f, 0f);
            _inspectorContent = BuildLanePanel(inspector, "Inspector");
        }

        private RectTransform BuildLanePanel(RectTransform lane, string title)
        {
            Text label = KingmakerUiFactory.CreateText(
                "LaneTitle", lane, _theme, title, 16, TextAnchor.MiddleLeft);
            label.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.SetAnchors(label.rectTransform, 0f, 1f, 1f, 1f);
            label.rectTransform.sizeDelta = new Vector2(0f, 22f);
            RectTransform content;
            KingmakerUiFactory.CreateScrollView(
                "Scroll", lane, _theme, out content, 12f);
            KingmakerUiFactory.SetAnchors(content, 0f, 0f, 1f, 1f);
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
            _footerBudget.horizontalOverflow = HorizontalWrapMode.Overflow;
            KingmakerUiFactory.Stretch(_footerBudget.rectTransform, 12, 480, 4, 4);
            _footerResult = KingmakerUiFactory.CreateText(
                "Result", footer, _theme, string.Empty, 14, TextAnchor.MiddleLeft);
            _footerResult.color = _theme.MutedBrownText;
            KingmakerUiFactory.SetAnchors(_footerResult.rectTransform, 0f, 1f, 0.55f, 1f);
            _footerResult.rectTransform.sizeDelta = new Vector2(0f, 16f);
            _footerResult.rectTransform.anchoredPosition = Vector2.zero;
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
                    _footerResult.text = accepted
                        ? "Plan accepted."
                        : "Acceptance refused: plan changed or not presented.";
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
            _footerResult.text = _session.DispatchDisposition;
        }

        private void RunApply(CastingApplyMode mode)
        {
            WorkspaceApplyResult result = _session.Apply(
                mode, _session.SelectedRoutineId, _inputs());
            if (!result.Allowed && result.GateDecision != null &&
                !result.GateDecision.Allowed && mode == CastingApplyMode.Ordinary)
            {
                _footerResult.text = "Apply blocked (" +
                    result.GateDecision.BlockingReasons.Count +
                    "): use Ready Casts Only deliberately.";
                _readyOnlyButton.gameObject.SetActive(true);
                return;
            }
            _footerResult.text = result.Allowed
                ? "Submitted." : "Refused: " + result.ReviewReason;
            // The dispatch boundary refuses native submission explicitly;
            // that refusal is the honest result text, never a cast claim.
            if (result.Dispatch != null && !result.Dispatch.Submitted)
                _footerResult.text = result.Dispatch.Reason;
        }

        private void Click(Action action)
        {
            PlannerNativeTheme.PlayClick(null);
            action();
        }

        private void RebuildCasters(WorkspaceView view)
        {
            KingmakerUiFactory.DestroyChildren(_casterContent);
            foreach (WorkspaceCasterRow row in view.Casters)
            {
                RectTransform entry = KingmakerUiFactory.CreateRect(
                    "Caster", _casterContent);
                KingmakerUiFactory.AddFramedPanel(entry,
                    row.SelectedFocus
                        ? _theme.ParchmentRaised : _theme.ParchmentPanel,
                    _theme.GoldAccent);
                KingmakerUiFactory.AddLayout(entry, 44f);
                Text name = KingmakerUiFactory.CreateText(
                    "Name", entry, _theme,
                    (string.IsNullOrEmpty(row.DisplayName)
                        ? row.UnitId : row.DisplayName) +
                    (row.Capable ? string.Empty : " — not capable"), 16,
                    TextAnchor.MiddleLeft);
                name.fontStyle = row.SelectedFocus ? FontStyle.Bold : FontStyle.Normal;
                KingmakerUiFactory.Stretch(name.rectTransform, 8, 8, 4, 4);
                Button select = KingmakerUiFactory.CreateButton(
                    "Focus", entry, _theme, "Focus", () => Click(() =>
                    {
                        _session.SelectCaster(row.UnitId);
                        RefreshView();
                    }));
                KingmakerUiFactory.SetAnchors(RectOf(select), 0.72f, 0.15f, 0.98f, 0.85f);
                if (row.ReadinessReasons.Count != 0)
                {
                    Text reasons = KingmakerUiFactory.CreateText(
                        "Reasons", entry, _theme,
                        string.Join(", ", row.ReadinessReasons), 12,
                        TextAnchor.MiddleLeft);
                    reasons.color = _theme.MutedBrownText;
                    KingmakerUiFactory.SetAnchors(reasons.rectTransform, 0f, 0f, 0.7f, 0.34f);
                    reasons.rectTransform.offsetMin = new Vector2(8f, 2f);
                }
            }
        }

        private void RebuildCards(WorkspaceView view)
        {
            ScrollRect scroll = _cardContent.GetComponentInParent<ScrollRect>();
            if (scroll != null) _cardScrollPosition = scroll.normalizedPosition;
            KingmakerUiFactory.DestroyChildren(_cardContent);
            foreach (WorkspaceCastingCard card in view.Cards)
            {
                RectTransform entry = KingmakerUiFactory.CreateRect(
                    "Card." + card.CastingId, _cardContent);
                KingmakerUiFactory.AddFramedPanel(entry,
                    card.EditingFocus
                        ? _theme.ParchmentRaised : _theme.ParchmentPanel,
                    _theme.GoldAccent);
                KingmakerUiFactory.AddLayout(entry, 96f);
                string coverage = card.DirectTargetUnitId == null
                    ? string.Empty
                    : " · coverage " + card.PredictedBeneficiaryUnitIds.Count +
                        "/" + card.RequiredCoverageUnitIds.Count;
                Text title = KingmakerUiFactory.CreateText(
                    "Title", entry, _theme,
                    card.CastingId + " · " + card.RoutineId + " #" + card.Order +
                    " · " + (card.CasterUnitId ?? "unresolved caster"), 16,
                    TextAnchor.MiddleLeft);
                title.fontStyle = FontStyle.Bold;
                KingmakerUiFactory.Stretch(title.rectTransform, 8, 90, 4, 2);
                Text status = KingmakerUiFactory.CreateText(
                    "Status", entry, _theme,
                    card.Readiness.ToString() + coverage, 14, TextAnchor.UpperRight);
                KingmakerUiFactory.SetAnchors(status.rectTransform, 0.55f, 0.55f, 0.98f, 0.92f);
                Text detail = KingmakerUiFactory.CreateText(
                    "Detail", entry, _theme,
                    BuildCardDetail(card), 13, TextAnchor.UpperLeft);
                KingmakerUiFactory.SetAnchors(detail.rectTransform, 0f, 0.22f, 1f, 0.55f);
                detail.rectTransform.offsetMin = new Vector2(8f, 2f);
                detail.rectTransform.offsetMax = new Vector2(-8f, -2f);
                Button edit = KingmakerUiFactory.CreateButton(
                    "Edit", entry, _theme, "Edit", () => Click(() =>
                    {
                        _session.FocusCasting(card.CastingId);
                        RefreshView();
                    }));
                KingmakerUiFactory.SetAnchors(RectOf(edit), 0.86f, 0.08f, 0.98f, 0.4f);
            }
            if (scroll != null) scroll.normalizedPosition = _cardScrollPosition;
        }

        private static string BuildCardDetail(WorkspaceCastingCard card)
        {
            var parts = new List<string>();
            if (card.DirectTargetUnitId != null)
                parts.Add("Target: " + card.DirectTargetUnitId);
            if (card.OriginLabel.Length != 0) parts.Add(card.OriginLabel);
            if (card.EnhancementLabels.Count != 0)
                parts.Add("Enhancements: " + string.Join(", ", card.EnhancementLabels));
            if (card.CostLabels.Count != 0)
                parts.Add("Cost: " + string.Join(", ", card.CostLabels));
            if (card.CoverageGapUnitIds.Count != 0)
                parts.Add("Outside coverage: " + string.Join(", ",
                    card.CoverageGapUnitIds));
            if (card.ReadinessReasons.Count != 0)
                parts.Add("Reasons: " + string.Join(", ", card.ReadinessReasons));
            return string.Join("  ·  ", parts);
        }

        private void RebuildInspector(WorkspaceView view)
        {
            KingmakerUiFactory.DestroyChildren(_inspectorContent);
            Text scope = KingmakerUiFactory.CreateText(
                "Scope", _inspectorContent, _theme, view.EditingScopeLabel, 16,
                TextAnchor.MiddleLeft);
            scope.fontStyle = FontStyle.Bold;
            KingmakerUiFactory.AddLayout(scope.rectTransform, 30f);
            if (view.EditingScope != WorkspaceEditingScope.EditingSingleCasting)
            {
                Text hint = KingmakerUiFactory.CreateText(
                    "Hint", _inspectorContent, _theme,
                    "Draft below configures the NEXT casting; it never edits " +
                    "existing cards.", 13, TextAnchor.UpperLeft);
                hint.color = _theme.MutedBrownText;
                KingmakerUiFactory.AddLayout(hint.rectTransform, 48f);
                return;
            }
            string focused = _session.EditingFocusCastingId;
            Button disable = KingmakerUiFactory.CreateButton(
                "Disable", _inspectorContent, _theme, "Disable", () => Click(() =>
                {
                    _session.SetFocusedCastingState(
                        Domain.Authoring.CastingAuthoringState.Disabled);
                    RefreshView();
                }));
            KingmakerUiFactory.AddLayout(RectOf(disable), 34f);
            Button enable = KingmakerUiFactory.CreateButton(
                "Enable", _inspectorContent, _theme, "Mark Ready", () => Click(() =>
                {
                    _session.SetFocusedCastingState(
                        Domain.Authoring.CastingAuthoringState.Ready);
                    RefreshView();
                }));
            KingmakerUiFactory.AddLayout(RectOf(enable), 34f);
            Button remove = KingmakerUiFactory.CreateButton(
                "Remove", _inspectorContent, _theme, "Remove", () => Click(() =>
                {
                    _session.RemoveFocusedCasting();
                    RefreshView();
                }));
            KingmakerUiFactory.AddLayout(RectOf(remove), 34f);
        }

        private void RebuildFooter(WorkspaceView view)
        {
            var lines = new List<string>();
            foreach (WorkspaceBudgetRow row in view.BudgetRows)
            {
                if (row.UnmetDemand == 0 && row.RequestedUsage == 0) continue;
                lines.Add(row.PoolKey + " " + row.AllocatedUsage + "/" +
                    row.RequestedUsage +
                    (row.UnmetDemand == 0 ? string.Empty
                        : " (unmet " + row.UnmetDemand + " — " +
                            string.Join(",", row.ResponsibleCastingIds) + ")"));
            }
            _footerBudget.text = lines.Count == 0
                ? "No resource demand yet."
                : string.Join("   ", lines);
        }
    }
}
