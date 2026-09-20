using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    // The game-world inputs one workspace refresh needs. In production these
    // come from the discovery/party/compatibility adapters; deterministic
    // fixtures supply them in integration tests. They are external game
    // boundaries, not substitutes for planner business services.
    public sealed class CastingWorkspaceInputs
    {
        public CastingWorkspaceInputs(
            PartyProviderSnapshot snapshot,
            IEnumerable<ProviderPlanningOption> providerOptions,
            IReadOnlyDictionary<string, EffectExpression> effectsBySource,
            IEnumerable<CastEnhancementSnapshot> enhancements,
            IEnumerable<ICastingTargetingModifier> targetingModifiers = null)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException("snapshot");
            ProviderOptions = (providerOptions ?? new ProviderPlanningOption[0])
                .Where(value => value != null).ToList();
            EffectsBySource = effectsBySource ??
                throw new ArgumentNullException("effectsBySource");
            Enhancements = (enhancements ?? new CastEnhancementSnapshot[0])
                .Where(value => value != null).ToList();
            TargetingModifiers = (targetingModifiers ?? new ICastingTargetingModifier[0])
                .Where(value => value != null).ToList();
        }

        public PartyProviderSnapshot Snapshot { get; private set; }
        public IReadOnlyList<ProviderPlanningOption> ProviderOptions { get; private set; }
        public IReadOnlyDictionary<string, EffectExpression> EffectsBySource { get; private set; }
        public IReadOnlyList<CastEnhancementSnapshot> Enhancements { get; private set; }
        public IReadOnlyList<ICastingTargetingModifier> TargetingModifiers
        { get; private set; }
    }

    // The boundary between the reviewed casting plan and native submission.
    // The production implementation refuses: no qualified executor exists
    // for the casting-first resolved plan yet, and legacy expansion or
    // substitution paths must never receive new-model intent. The refusal
    // records the attempted submission identity so integration tests can
    // prove policy without any gameplay claim.
    public interface ICastingDispatchBoundary
    {
        string DispositionReason { get; }

        CastingDispatchOutcome Submit(
            ExplicitCastingPlan plan,
            CastingApplyDecision decision,
            string scopeRoutineId);
    }

    public sealed class CastingDispatchOutcome
    {
        internal CastingDispatchOutcome(bool submitted, string reason,
            IReadOnlyList<string> castingIds)
        {
            Submitted = submitted;
            Reason = reason ?? string.Empty;
            CastingIds = castingIds;
        }

        public bool Submitted { get; private set; }
        public string Reason { get; private set; }
        public IReadOnlyList<string> CastingIds { get; private set; }
    }

    public sealed class DisabledCastingDispatchBoundary : ICastingDispatchBoundary
    {
        internal readonly List<string> RecordedSubmissions =
            new List<string>();

        public string DispositionReason
        {
            get
            {
                return "native-submission-disabled:" +
                    "no-qualified-casting-first-executor";
            }
        }

        public CastingDispatchOutcome Submit(
            ExplicitCastingPlan plan,
            CastingApplyDecision decision,
            string scopeRoutineId)
        {
            // Policy proof only: the exact reviewed identity is recorded and
            // refused. This is never a cast, never a gameplay result, and
            // never routes through a legacy expansion/substitution path.
            RecordedSubmissions.Add(
                (scopeRoutineId ?? "one-pass") + "|" +
                string.Join(",", decision.ExecutableCastingIds));
            return new CastingDispatchOutcome(
                false, DispositionReason, decision.ExecutableCastingIds);
        }
    }

    public sealed class WorkspaceApplyResult
    {
        internal WorkspaceApplyResult(
            bool allowed,
            string reviewReason,
            CastingApplyDecision gateDecision,
            CastingDispatchOutcome dispatch)
        {
            Allowed = allowed;
            ReviewReason = reviewReason ?? string.Empty;
            GateDecision = gateDecision;
            Dispatch = dispatch;
        }

        public bool Allowed { get; private set; }
        // Empty when the review coordinator permitted the submission.
        public string ReviewReason { get; private set; }
        public CastingApplyDecision GateDecision { get; private set; }
        public CastingDispatchOutcome Dispatch { get; private set; }
    }

    // The primary casting-first workspace session: one connected controller
    // over the real authoring service (the only mutation authority), the
    // shared compiler/forecast/gate services, candidate persistence, and the
    // review coordinator. Browsing and previewing never mutate the document
    // and never approve anything; every mutation is an explicit command with
    // a disclosed scope and Undo; Apply routes through presented-plan review
    // and preflight before a dispatch boundary that is disabled until a
    // qualified executor exists.
    public sealed class CastingWorkspaceSession
    {
        private readonly CastingPlanRepository _repository;
        private readonly CastingExecutionGate _gate = new CastingExecutionGate();
        private readonly CastingForecastService _forecast =
            new CastingForecastService();
        private readonly CastingReviewCoordinator _review =
            new CastingReviewCoordinator();
        private readonly ICastingDispatchBoundary _dispatch;
        private readonly ExplicitCastingCompiler _compiler =
            new ExplicitCastingCompiler();
        private CastingAuthoringService _authoring;
        private bool _submissionInFlight;

        public CastingWorkspaceSession(
            string modPath, string campaignId,
            ICastingDispatchBoundary dispatchBoundary = null)
        {
            if (string.IsNullOrWhiteSpace(modPath))
                throw new ArgumentException("Absolute mod path is required.", "modPath");
            if (string.IsNullOrWhiteSpace(campaignId))
                throw new ArgumentException("Exact campaign ID is required.",
                    "campaignId");
            _repository = new CastingPlanRepository(modPath);
            _dispatch = dispatchBoundary ?? new DisabledCastingDispatchBoundary();
            CampaignId = campaignId;
            CastingPlanLoadResult loaded = _repository.Load(campaignId);
            LoadStatus = loaded.Status;
            LoadWarning = loaded.Warning;
            switch (loaded.Status)
            {
                case CastingPlanLoadStatus.Loaded:
                case CastingPlanLoadStatus.RecoveredFromBackup:
                    _authoring = new CastingAuthoringService(
                        loaded.Profile.ToDocument());
                    break;
                case CastingPlanLoadStatus.Absent:
                    _authoring = new CastingAuthoringService(NewDocument());
                    break;
                default:
                    // Corrupt or unsupported candidate data is never
                    // overwritten by this session: editing starts from an
                    // empty in-memory document and saving stays refused
                    // until the operator resolves the stored bytes.
                    PersistenceBlocked = true;
                    _authoring = new CastingAuthoringService(NewDocument());
                    break;
            }
            SelectedRoutineId = "long";
        }

        public string CampaignId { get; private set; }
        public CastingPlanLoadStatus LoadStatus { get; private set; }
        public string LoadWarning { get; private set; }
        public bool PersistenceBlocked { get; private set; }

        // Browsing selection (focus only — never a mutation).
        public string SelectedSourceId { get; private set; }
        public string SelectedRoutineId { get; private set; }
        public string SelectedCasterUnitId { get; private set; }
        public string EditingFocusCastingId { get; private set; }

        // Draft defaults for the NEXT casting only; changes never
        // retroactively edit existing castings.
        public CastingDraft Draft { get; } = new CastingDraft();

        public CastingPlanDocument Document
        {
            get { return _authoring.Document; }
        }

        public bool CanUndo
        {
            get { return _authoring.CanUndo; }
        }

        public string DispatchDisposition
        {
            get { return _dispatch.DispositionReason; }
        }

        // ------------------------------------------------------------------
        // Browsing (pure)
        // ------------------------------------------------------------------

        public void SelectBuff(string sourceId)
        {
            SelectedSourceId = string.IsNullOrWhiteSpace(sourceId)
                ? string.Empty : sourceId;
        }

        public void SelectRoutine(string routineId)
        {
            if (_authoring.Document.Routines.All(
                    value => value.RoutineId != routineId))
                throw new ArgumentException("Unknown routine.", "routineId");
            SelectedRoutineId = routineId;
        }

        // Selecting another caster changes editing focus only — existing
        // assignments and connections are untouched.
        public void SelectCaster(string casterUnitId)
        {
            SelectedCasterUnitId = string.IsNullOrWhiteSpace(casterUnitId)
                ? null : casterUnitId;
        }

        public void FocusCasting(string castingId)
        {
            if (castingId != null &&
                _authoring.Document.Castings.All(
                    value => value.CastingId != castingId))
                throw new ArgumentException("Unknown casting.", "castingId");
            EditingFocusCastingId = castingId;
        }

        // ------------------------------------------------------------------
        // Read models
        // ------------------------------------------------------------------

        // Compiles the shared resolved plan (calculated state only — this
        // never presents or approves anything).
        public ExplicitCastingPlan CompilePlan(CastingWorkspaceInputs inputs)
        {
            return Compile(inputs, null, false);
        }

        public WorkspaceView BuildView(CastingWorkspaceInputs inputs)
        {
            ExplicitCastingPlan plan = Compile(inputs, null, false);
            string selectedSource = string.IsNullOrEmpty(SelectedSourceId)
                ? FirstSourceId() : SelectedSourceId;
            var casters = BuildCasterRows(plan, inputs.Snapshot, selectedSource);
            var cards = BuildCards(plan, selectedSource);
            var budget = plan.BudgetLines
                .Select(line => new WorkspaceBudgetRow(line)).ToList();
            CastingApplyDecision selectedGate = _gate.Evaluate(
                plan, CastingApplyMode.Ordinary, SelectedRoutineId);
            CastingApplyDecision onePassGate = _gate.Evaluate(
                plan, CastingApplyMode.Ordinary);
            WorkspaceEditingScope scope = EditingFocusCastingId == null
                ? WorkspaceEditingScope.ConfigureNextCasting
                : WorkspaceEditingScope.EditingSingleCasting;
            string scopeLabel = EditingFocusCastingId == null
                ? "Configure next casting"
                : "Editing casting " + EditingFocusCastingId;
            return new WorkspaceView(
                selectedSource, SelectedRoutineId, casters, cards, budget,
                _authoring.Document.Routines.Select(value => value.RoutineId)
                    .ToList(),
                selectedGate, onePassGate, _review.Status, scope, scopeLabel,
                plan.Diagnostics);
        }

        // ------------------------------------------------------------------
        // Review and Apply (the actual presentation boundary)
        // ------------------------------------------------------------------

        // The view calls this when it actually renders a plan for review —
        // computing a preview alone is not presentation.
        public CastingPlanSignature PresentForReview(CastingWorkspaceInputs inputs)
        {
            ExplicitCastingPlan plan = Compile(inputs, null, false);
            CastingPlanSignature signature = CastingPlanSignature.For(plan);
            _review.Present(signature);
            return signature;
        }

        // The player accepts the contents as currently compiled; the
        // coordinator refuses when those contents were never presented or
        // have materially changed since presentation.
        public bool AcceptPresentedPlan(CastingWorkspaceInputs inputs)
        {
            ExplicitCastingPlan plan = Compile(inputs, null, false);
            return _review.Accept(CastingPlanSignature.For(plan)).Allowed;
        }

        public WorkspaceApplyResult Apply(
            CastingApplyMode mode,
            string scopeRoutineId,
            CastingWorkspaceInputs inputs)
        {
            if (_submissionInFlight)
                return RefusedInFlight(null);
            // The decision is computed from the plan as it exists right now,
            // not from any earlier preview.
            ExplicitCastingPlan plan = Compile(inputs, null, false);
            CastingApplyDecision decision = _gate.Evaluate(plan, mode, scopeRoutineId);
            if (mode == CastingApplyMode.Ordinary && !decision.Allowed)
                return RefusedInFlight(decision);
            CastingPlanSignature signature = CastingPlanSignature.For(plan);
            CastingReviewDecision review = _review.TrySubmit(signature);
            if (!review.Allowed)
                return new WorkspaceApplyResult(
                    false, review.Reason, decision, null);
            try
            {
                _submissionInFlight = true;
                CastingDispatchOutcome dispatch = _dispatch.Submit(
                    plan, decision, scopeRoutineId);
                return new WorkspaceApplyResult(
                    dispatch.Submitted, dispatch.Submitted
                        ? string.Empty : dispatch.Reason,
                    decision, dispatch);
            }
            finally
            {
                _submissionInFlight = false;
            }
        }

        // ------------------------------------------------------------------
        // Authoring commands (explicit mutations with disclosed scope)
        // ------------------------------------------------------------------

        public AuthoringEditResult AddCastingFromDraft()
        {
            PlannedCasting casting = Draft.Materialize(
                NextCastingId(), SelectedRoutineId);
            return _authoring.AddCasting(casting);
        }

        public AuthoringEditResult UpdateFocusedCasting(PlannedCasting replacement)
        {
            if (EditingFocusCastingId == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            if (!string.Equals(replacement.CastingId, EditingFocusCastingId,
                    StringComparison.Ordinal))
                return AuthoringEditResult.Refuse(
                    "replacement-does-not-match-focus:" + replacement.CastingId);
            return _authoring.UpdateCasting(replacement);
        }

        public AuthoringEditResult RemoveFocusedCasting()
        {
            if (EditingFocusCastingId == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            return _authoring.RemoveCasting(EditingFocusCastingId);
        }

        public AuthoringEditResult MoveFocusedCasting(
            string targetRoutineId, int targetPosition)
        {
            if (EditingFocusCastingId == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            return _authoring.MoveCasting(
                EditingFocusCastingId, targetRoutineId, targetPosition);
        }

        public AuthoringEditResult SetFocusedCastingState(CastingAuthoringState state)
        {
            if (EditingFocusCastingId == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            return _authoring.SetCastingState(EditingFocusCastingId, state);
        }

        public bool Undo()
        {
            return _authoring.Undo();
        }

        // ------------------------------------------------------------------
        // Persistence (candidate storage; one canonical writer)
        // ------------------------------------------------------------------

        public void Save()
        {
            if (PersistenceBlocked)
                throw new InvalidOperationException(
                    "Candidate persistence is blocked: " + LoadStatus +
                    " " + LoadWarning);
            _repository.Save(CastingPlanProfile.FromDocument(
                _authoring.Document, null, null));
        }

        public CastingPlanLoadStatus Reload()
        {
            CastingPlanLoadResult loaded = _repository.Load(CampaignId);
            LoadStatus = loaded.Status;
            LoadWarning = loaded.Warning;
            switch (loaded.Status)
            {
                case CastingPlanLoadStatus.Loaded:
                case CastingPlanLoadStatus.RecoveredFromBackup:
                    _authoring = new CastingAuthoringService(
                        loaded.Profile.ToDocument());
                    PersistenceBlocked = false;
                    break;
                default:
                    PersistenceBlocked = true;
                    break;
            }
            return loaded.Status;
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private ExplicitCastingPlan Compile(
            CastingWorkspaceInputs inputs, string routineScope, bool projectEffects)
        {
            if (inputs == null) throw new ArgumentNullException("inputs");
            return _compiler.Compile(
                _authoring.Document, inputs.Snapshot, inputs.ProviderOptions,
                inputs.EffectsBySource, inputs.Enhancements,
                routineScope, inputs.TargetingModifiers, projectEffects);
        }

        private WorkspaceApplyResult RefusedInFlight(CastingApplyDecision decision)
        {
            return new WorkspaceApplyResult(
                false, "submission-already-in-flight", decision, null);
        }

        private string FirstSourceId()
        {
            PlannedCasting first = _authoring.Document.Castings.FirstOrDefault();
            return first == null ? string.Empty : first.SourceId;
        }

        private List<WorkspaceCasterRow> BuildCasterRows(
            ExplicitCastingPlan plan, PartyProviderSnapshot snapshot,
            string selectedSource)
        {
            // Capability per caster for the selected buff, separated from
            // this plan's current readiness reasons for that caster.
            var rows = new List<WorkspaceCasterRow>();
            foreach (UnitSnapshot unit in snapshot.Units)
            {
                bool capable = plan.Castings
                    .Where(value => string.Equals(value.SourceId, selectedSource,
                        StringComparison.Ordinal))
                    .SelectMany(value => value.CapableCasterUnitIds)
                    .Contains(unit.UnitId);
                List<string> reasons = plan.Castings
                    .Where(value => value.CasterUnitId == unit.UnitId)
                    .SelectMany(value => value.ReadinessReasons)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal).ToList();
                rows.Add(new WorkspaceCasterRow(
                    unit.UnitId, unit.DisplayName, capable, reasons,
                    unit.UnitId == SelectedCasterUnitId));
            }
            return rows;
        }

        private List<WorkspaceCastingCard> BuildCards(
            ExplicitCastingPlan plan, string selectedSource)
        {
            var cards = new List<WorkspaceCastingCard>();
            foreach (ResolvedCasting casting in plan.Castings
                .Where(value => string.Equals(value.SourceId, selectedSource,
                    StringComparison.Ordinal)))
            {
                string modeLabel;
                string originLabel = string.Empty;
                switch (casting.TargetMode)
                {
                    case CastingTargetMode.CasterCenteredOrigin:
                        modeLabel = "Group · caster-centered";
                        originLabel = "Origin: caster";
                        break;
                    case CastingTargetMode.AnchoredOrigin:
                        modeLabel = "Group · anchored";
                        originLabel = "Origin: " + casting.Origin.AnchorUnitId;
                        break;
                    default:
                        modeLabel = "Direct target";
                        break;
                }
                cards.Add(new WorkspaceCastingCard(
                    casting.CastingId, casting.RoutineId, casting.Order,
                    casting.CasterUnitId, casting.SourceId, modeLabel,
                    casting.DirectTargetUnitId, originLabel,
                    casting.RequiredCoverageUnitIds,
                    casting.PredictedBeneficiaryUnitIds,
                    casting.CoverageGaps.Select(gap => gap.UnitId).ToList(),
                    casting.Enhancements
                        .Select(value => value.EnhancementId +
                            (value.Required ? " (required)" : " (optional)"))
                        .ToList(),
                    casting.Cost.Select(line =>
                        line.PoolKey + ": " + line.Units).ToList(),
                    casting.Readiness, casting.ReadinessReasons,
                    casting.CastingId == EditingFocusCastingId));
            }
            return cards;
        }

        private CastingPlanDocument NewDocument()
        {
            return new CastingPlanDocument(CampaignId,
                new[]
                {
                    new RoutineDefinition("long", "Long"),
                    new RoutineDefinition("important", "Important"),
                    new RoutineDefinition("short", "Short")
                },
                new PlannedCasting[0]);
        }

        private string NextCastingId()
        {
            int index = _authoring.Document.Castings.Count + 1;
            string candidate;
            do
            {
                candidate = "cast-" + index;
                index++;
            }
            while (_authoring.Document.Castings.Any(
                value => value.CastingId == candidate));
            return candidate;
        }
    }

    // Player-facing defaults for the NEXT casting. Carrying defaults
    // forward never edits existing castings; Materialize builds one new
    // record from the current values.
    public sealed class CastingDraft
    {
        public string SourceId { get; set; }
        public AbilityKey Ability { get; set; }
        public string CasterUnitId { get; set; }
        public string SpellbookGuid { get; set; }
        public CastingTargetMode TargetMode { get; set; }
        public string DirectTargetUnitId { get; set; }
        public CastingOrigin Origin { get; set; }
        public List<string> RequiredCoverageUnitIds { get; } = new List<string>();
        public List<TargetingModifierSelection> TargetingModifiers { get; } =
            new List<TargetingModifierSelection>();
        public List<AuthoredEnhancementSelection> Enhancements { get; } =
            new List<AuthoredEnhancementSelection>();
        public CastingAuthoringState State { get; set; }

        internal PlannedCasting Materialize(string castingId, string routineId)
        {
            if (string.IsNullOrWhiteSpace(castingId))
                throw new ArgumentException("Casting ID is required.", "castingId");
            return new PlannedCasting(
                castingId, routineId, 0,
                string.IsNullOrWhiteSpace(SourceId) ? "unsourced" : SourceId,
                Ability ?? throw new InvalidOperationException(
                    "The draft needs an ability before it can become a casting."),
                CasterUnitId, SpellbookGuid, TargetMode, DirectTargetUnitId,
                Origin, RequiredCoverageUnitIds, TargetingModifiers,
                Enhancements, ExistingEffectPolicy.SkipAlreadyActive, null,
                // A brand-new casting is a draft or ready intent; parking a
                // record before it exists is not a meaningful action.
                State == CastingAuthoringState.Disabled
                    ? CastingAuthoringState.Draft
                    : State,
                null);
        }
    }
}
