using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.UI;
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
            RebuildRoutineBar(view);
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
            BuildRoutineBar(frame);
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

        private void BuildRoutineBar(RectTransform frame)
        {
            _routineBar = KingmakerUiFactory.CreateRect("RoutineBar", frame);
            KingmakerUiFactory.SetAnchors(_routineBar, 0f, 0.90f, 1f, 0.945f);
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
            _saveButton = KingmakerUiFactory.CreateButton(
                "Save", footer, _theme, "Save", () => Click(() =>
                {
                    _session.Save();
                    _footerResult.text = "Candidate saved.";
                }));
            KingmakerUiFactory.SetAnchors(RectOf(_saveButton), 0.34f, 0.2f, 0.42f, 0.8f);
            _reloadButton = KingmakerUiFactory.CreateButton(
                "Reload", footer, _theme, "Reload", () => Click(() =>
                {
                    CastingPlanLoadStatus status = _session.Reload();
                    _footerResult.text = "Reloaded: " + status;
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
            if (view.EditingScope == WorkspaceEditingScope.EditingSingleCasting)
            {
                RebuildFocusedCastingEditor(view);
                return;
            }
            RebuildDraftEditor(view);
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
            AddInspectorCaption("Retarget");
            foreach (WorkspaceTargetOption target in view.Draft.Targets)
            {
                WorkspaceTargetOption captured = target;
                bool selected = string.Equals(focused.DirectTargetUnitId,
                    captured.UnitId, StringComparison.Ordinal);
                Button pick = KingmakerUiFactory.CreateButton(
                    "Target." + captured.UnitId, _inspectorContent, _theme,
                    (selected ? "[x] " : "[  ] ") + captured.DisplayName,
                    () => Click(() => ApplyFocusedEdit(
                        focused.WithDirectTarget(captured.UnitId))));
                KingmakerUiFactory.AddLayout(RectOf(pick), 30f);
            }
            AddInspectorCaption("Casting state");
            Button disable = KingmakerUiFactory.CreateButton(
                "Disable", _inspectorContent, _theme, "Disable", () => Click(() =>
                {
                    _session.SetFocusedCastingState(
                        Domain.Authoring.CastingAuthoringState.Disabled);
                    RefreshView();
                }));
            KingmakerUiFactory.AddLayout(RectOf(disable), 30f);
            Button enable = KingmakerUiFactory.CreateButton(
                "Enable", _inspectorContent, _theme, "Mark Ready", () => Click(() =>
                {
                    _session.SetFocusedCastingState(
                        Domain.Authoring.CastingAuthoringState.Ready);
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

        private void ApplyFocusedEdit(Domain.Authoring.PlannedCasting replacement)
        {
            AuthoringEditResult result = _session.UpdateFocusedCasting(replacement);
            if (!result.Applied)
                _footerResult.text = "Edit refused: " + result.Reason;
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
            AddInspectorCaption("Buff");
            foreach (WorkspaceSourceOption source in draft.Sources)
            {
                WorkspaceSourceOption captured = source;
                Button pick = KingmakerUiFactory.CreateButton(
                    "Source." + captured.SourceId, _inspectorContent, _theme,
                    (captured.Selected ? "[x] " : "[  ] ") + captured.DisplayName,
                    () => Click(() =>
                    {
                        _session.SelectBuff(captured.SourceId);
                        _session.Draft.SourceId = captured.SourceId;
                        RefreshView();
                    }));
                KingmakerUiFactory.AddLayout(RectOf(pick), 30f);
            }
            AddInspectorCaption("Caster");
            if (draft.CapableCasters.Count == 0)
            {
                Text none = KingmakerUiFactory.CreateText(
                    "NoCaster", _inspectorContent, _theme,
                    "No eligible caster for this buff.", 13, TextAnchor.MiddleLeft);
                none.color = _theme.MutedBrownText;
                KingmakerUiFactory.AddLayout(none.rectTransform, 26f);
            }
            foreach (WorkspaceCasterRow caster in draft.CapableCasters)
            {
                WorkspaceCasterRow captured = caster;
                bool selected = string.Equals(draft.CasterUnitId,
                    captured.UnitId, StringComparison.Ordinal);
                Button pick = KingmakerUiFactory.CreateButton(
                    "DraftCaster." + captured.UnitId, _inspectorContent, _theme,
                    (selected ? "[x] " : "[  ] ") +
                        (string.IsNullOrEmpty(captured.DisplayName)
                            ? captured.UnitId : captured.DisplayName),
                    () => Click(() =>
                    {
                        _session.SelectCaster(captured.UnitId);
                        _session.Draft.CasterUnitId = captured.UnitId;
                        RefreshView();
                    }));
                KingmakerUiFactory.AddLayout(RectOf(pick), 30f);
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
                    if (direct)
                        _session.SetDraftTargeting(
                            Domain.Authoring.CastingTargetMode.CasterCenteredOrigin,
                            null, null, null);
                    else
                        _session.SetDraftTargeting(
                            Domain.Authoring.CastingTargetMode.DirectTarget,
                            draft.DirectTargetUnitId, null, null);
                    RefreshView();
                }));
            KingmakerUiFactory.AddLayout(RectOf(mode), 30f);
            if (direct)
            {
                foreach (WorkspaceTargetOption target in draft.Targets)
                {
                    WorkspaceTargetOption captured = target;
                    bool selected = string.Equals(draft.DirectTargetUnitId,
                        captured.UnitId, StringComparison.Ordinal);
                    Button pick = KingmakerUiFactory.CreateButton(
                        "DraftTarget." + captured.UnitId, _inspectorContent,
                        _theme,
                        (selected ? "[x] " : "[  ] ") + captured.DisplayName,
                        () => Click(() =>
                        {
                            _session.SetDraftTargeting(
                                Domain.Authoring.CastingTargetMode.DirectTarget,
                                captured.UnitId, null, null);
                            RefreshView();
                        }));
                    KingmakerUiFactory.AddLayout(RectOf(pick), 30f);
                }
            }
            else
            {
                Button casterOrigin = KingmakerUiFactory.CreateButton(
                    "Origin.Caster", _inspectorContent, _theme,
                    string.IsNullOrEmpty(draft.OriginAnchorUnitId)
                        ? "[x] Origin: caster" : "[  ] Origin: caster",
                    () => Click(() =>
                    {
                        _session.SetDraftTargeting(
                            Domain.Authoring.CastingTargetMode.CasterCenteredOrigin,
                            null, null, null);
                        RefreshView();
                    }));
                KingmakerUiFactory.AddLayout(RectOf(casterOrigin), 30f);
                foreach (WorkspaceOriginOption origin in draft.Origins)
                {
                    WorkspaceOriginOption captured = origin;
                    Button pick = KingmakerUiFactory.CreateButton(
                        "Origin." + captured.AnchorUnitId, _inspectorContent,
                        _theme,
                        (captured.Selected ? "[x] " : "[  ] ") +
                            "Origin: " + captured.AnchorUnitId,
                        () => Click(() =>
                        {
                            _session.SetDraftTargeting(
                                Domain.Authoring.CastingTargetMode.AnchoredOrigin,
                                null, captured.AnchorUnitId, null);
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
            foreach (WorkspaceEnhancementOption enhancement in
                draft.Enhancements)
            {
                WorkspaceEnhancementOption captured = enhancement;
                Button toggle = KingmakerUiFactory.CreateButton(
                    "Enhancement." + captured.EnhancementId, _inspectorContent,
                    _theme,
                    (captured.Selected ? "[x] " : "[  ] ") + captured.Label,
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
                KingmakerUiFactory.AddLayout(RectOf(toggle), 30f);
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
            Button add = KingmakerUiFactory.CreateButton(
                "AddCasting", _inspectorContent, _theme, "Add Casting",
                () => Click(() =>
                {
                    AuthoringEditResult result = _session.AddCastingFromDraft(_inputs());
                    if (!result.Applied)
                        _footerResult.text = "Add refused: " + result.Reason;
                    RefreshView();
                }));
            KingmakerUiFactory.AddLayout(RectOf(add), 36f);
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
                Button tab = KingmakerUiFactory.CreateButton(
                    "Routine." + captured, _routineBar, _theme,
                    (selected ? "[x] " : string.Empty) + captured,
                    () => Click(() =>
                    {
                        _session.SelectRoutine(captured);
                        RefreshView();
                    }));
                RectOf(tab).anchorMin = new Vector2(0f, 0.1f);
                RectOf(tab).anchorMax = new Vector2(0f, 0.9f);
                RectOf(tab).sizeDelta = new Vector2(170f, 0f);
                RectOf(tab).anchoredPosition = new Vector2(
                    16f + view.RoutineIds.TakeWhile(id =>
                        !string.Equals(id, captured, StringComparison.Ordinal))
                        .Count() * 178f, 0f);
            }
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
