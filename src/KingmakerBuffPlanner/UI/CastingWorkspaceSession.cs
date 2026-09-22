using System;
using System.Collections.Generic;
using System.Globalization;
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
        private CastingWorkspaceInputs _lastInputs;
        private string _savedIntentSignature = string.Empty;
        private string _resolvedDraftKey = string.Empty;
        private string _rememberedDirectRecipient = string.Empty;
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
            // The dirty baseline covers every load state — an absent or
            // blocked candidate starts exactly as clean as a loaded one.
            _savedIntentSignature = DocumentIntentSignature();
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
            string resolved = string.IsNullOrWhiteSpace(sourceId)
                ? string.Empty : sourceId;
            if (!string.Equals(resolved, SelectedSourceId,
                    StringComparison.Ordinal))
                // A stale ability under a different displayed source must
                // never survive selection (review F1).
                Draft.Ability = null;
            SelectedSourceId = resolved;
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
            string resolved = string.IsNullOrWhiteSpace(casterUnitId)
                ? null : casterUnitId;
            if (!string.Equals(resolved, SelectedCasterUnitId,
                    StringComparison.Ordinal))
                Draft.Ability = null;
            SelectedCasterUnitId = resolved;
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

        // Compiles the whole-document plan (calculated state only — this
        // never presents or approves anything).
        public ExplicitCastingPlan CompilePlan(CastingWorkspaceInputs inputs)
        {
            return Compile(inputs, null, false);
        }

        public WorkspaceView BuildView(CastingWorkspaceInputs inputs)
        {
            // The selected-run plan reserves resources for the SELECTED
            // routine only; the explicitly labeled one-pass forecast is a
            // separate whole-document construction with effect projection
            // (review R3).
            ExplicitCastingPlan plan = Compile(inputs, SelectedRoutineId, false);
            string selectedSource = string.IsNullOrEmpty(SelectedSourceId)
                ? FirstSourceId(inputs) : SelectedSourceId;
            var casters = BuildCasterRows(plan, inputs, selectedSource);
            var cards = BuildCards(plan, selectedSource);
            var namesByUnit = inputs.Snapshot.Units.ToDictionary(
                unit => unit.UnitId,
                unit => string.IsNullOrEmpty(unit.DisplayName)
                    ? unit.UnitId : unit.DisplayName,
                StringComparer.Ordinal);
            foreach (WorkspaceCastingCard card in cards)
                card.ApplyDisplayNames(
                    namesByUnit.TryGetValue(card.CasterUnitId ?? string.Empty,
                        out string casterName) ? casterName : null,
                    card.DirectTargetUnitId == null ? null
                        : namesByUnit.TryGetValue(card.DirectTargetUnitId,
                            out string targetName) ? targetName : null);
            var budget = plan.BudgetLines
                .Select(line => new WorkspaceBudgetRow(line)).ToList();
            CastingApplyDecision selectedGate = _gate.Evaluate(
                plan, CastingApplyMode.Ordinary, SelectedRoutineId);
            CastingForecast onePass = _forecast.ForecastOnePass(
                _authoring.Document, inputs.Snapshot, inputs.ProviderOptions,
                inputs.EffectsBySource, inputs.Enhancements,
                inputs.TargetingModifiers);
            CastingApplyDecision onePassGate = _gate.Evaluate(
                onePass.Plan, CastingApplyMode.Ordinary);
            WorkspaceEditingScope scope = EditingFocusCastingId == null
                ? WorkspaceEditingScope.ConfigureNextCasting
                : WorkspaceEditingScope.EditingSingleCasting;
            string scopeLabel = EditingFocusCastingId == null
                ? "Configure next casting"
                : "Editing casting " + EditingFocusCastingId;
            var view = new WorkspaceView(
                selectedSource, SelectedRoutineId, casters, cards, budget,
                _authoring.Document.Routines.Select(value => value.RoutineId)
                    .ToList(),
                selectedGate, onePassGate, _review.Status, scope, scopeLabel,
                plan.Diagnostics,
                BuildDraftView(inputs, selectedSource, casters));
            BuildFocusedEnhancements(view, inputs);
            return view;
        }

        // Enhancement options for the FOCUSED record, derived from that
        // record's own caster and ability — not the draft's (review G1).
        private void BuildFocusedEnhancements(
            WorkspaceView view, CastingWorkspaceInputs inputs)
        {
            if (EditingFocusCastingId == null ||
                inputs == null || inputs.Enhancements == null) return;
            PlannedCasting focused = _authoring.Document.Castings
                .FirstOrDefault(value => value != null && string.Equals(
                    value.CastingId, EditingFocusCastingId,
                    StringComparison.Ordinal));
            if (focused == null || focused.Ability == null ||
                string.IsNullOrEmpty(focused.CasterUnitId)) return;
            string baseGuid = focused.Ability.BaseAbilityGuid;
            string variantGuid = focused.Ability.VariantGuid;
            foreach (CastEnhancementSnapshot enhancement in inputs.Enhancements)
            {
                if (enhancement == null ||
                    !string.Equals(enhancement.CasterUnitId,
                        focused.CasterUnitId, StringComparison.Ordinal))
                    continue;
                bool qualified = enhancement.AbilityWhiteList.Count == 0 ||
                    enhancement.AbilityWhiteList.Contains(baseGuid) ||
                    enhancement.AbilityWhiteList.Contains(variantGuid);
                if (!qualified) continue;
                view._focusedEnhancements.Add(new WorkspaceEnhancementOption(
                    enhancement.EnhancementId, enhancement.DisplayName,
                    focused.Enhancements.Any(selection => selection != null &&
                        string.Equals(selection.EnhancementId,
                            enhancement.EnhancementId,
                            StringComparison.Ordinal))));
            }
        }

        private WorkspaceDraftView BuildDraftView(
            CastingWorkspaceInputs inputs, string selectedSource,
            List<WorkspaceCasterRow> casters)
        {
            var sources = new List<WorkspaceSourceOption>();
            if (inputs.EffectsBySource != null && inputs.ProviderOptions != null)
            {
                var sourceIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (ProviderPlanningOption option in inputs.ProviderOptions)
                {
                    if (option == null || option.Provider == null) continue;
                    AbilityKey ability = option.Provider.Key.Ability;
                    EffectExpression abilityExpression = null;
                    if (ability != null && inputs.EffectsBySource.TryGetValue(
                            ability.Canonical, out abilityExpression))
                    {
                        foreach (KeyValuePair<string, EffectExpression> pair in
                            inputs.EffectsBySource)
                            if (!string.Equals(pair.Key, ability.Canonical,
                                    StringComparison.Ordinal) &&
                                ReferenceEquals(pair.Value, abilityExpression))
                                sourceIds.Add(pair.Key);
                    }
                }
                foreach (string sourceId in sourceIds.OrderBy(
                         value => value, StringComparer.Ordinal))
                {
                    // The label comes from THIS source's own providers only
                    // (review F5): the first nonempty SourceDisplayName of
                    // the whole list would give every buff the same name.
                    EffectExpression labelExpression;
                    string display = inputs.EffectsBySource.TryGetValue(
                            sourceId, out labelExpression)
                        ? inputs.ProviderOptions
                            .Where(value => value != null &&
                                value.Provider != null &&
                                OptionServesExpression(inputs, value,
                                    labelExpression))
                            .Select(value => value.Provider.SourceDisplayName)
                            .FirstOrDefault(name =>
                                !string.IsNullOrWhiteSpace(name))
                        : null;
                    sources.Add(new WorkspaceSourceOption(
                        sourceId, display,
                        string.Equals(sourceId, selectedSource,
                            StringComparison.Ordinal)));
                }
            }
            string draftSource = string.IsNullOrEmpty(Draft.SourceId)
                ? selectedSource : Draft.SourceId;
            string draftCaster = Draft.CasterUnitId ?? SelectedCasterUnitId;
            int draftCandidates;
            ProviderPlanningOption draftOption = FindDraftOption(
                inputs, draftSource, draftCaster, out draftCandidates);
            var targets = inputs.Snapshot.Units
                .Select(unit => new WorkspaceTargetOption(
                    unit.UnitId, unit.DisplayName,
                    string.Equals(unit.UnitId, Draft.DirectTargetUnitId,
                        StringComparison.Ordinal)))
                .ToList();
            var origins = new List<WorkspaceOriginOption>();
            if (draftOption != null)
                foreach (string anchor in draftOption.LegalAnchorIds)
                    origins.Add(new WorkspaceOriginOption(anchor,
                        Draft.Origin != null && !Draft.Origin.IsCasterCentered &&
                        string.Equals(Draft.Origin.AnchorUnitId, anchor,
                            StringComparison.Ordinal)));
            var enhancements = new List<WorkspaceEnhancementOption>();
            AbilityKey enhancementAbility = Draft.Ability ??
                DraftAbilityFor(inputs);
            if (inputs.Enhancements != null && enhancementAbility != null &&
                !string.IsNullOrEmpty(draftCaster))
            {
                string baseGuid = enhancementAbility.BaseAbilityGuid;
                string variantGuid = enhancementAbility.VariantGuid;
                foreach (CastEnhancementSnapshot enhancement in inputs.Enhancements)
                {
                    if (enhancement == null ||
                        !string.Equals(enhancement.CasterUnitId, draftCaster,
                            StringComparison.Ordinal)) continue;
                    bool qualified = enhancement.AbilityWhiteList.Count == 0 ||
                        enhancement.AbilityWhiteList.Contains(baseGuid) ||
                        enhancement.AbilityWhiteList.Contains(variantGuid);
                    if (!qualified) continue;
                    enhancements.Add(new WorkspaceEnhancementOption(
                        enhancement.EnhancementId, enhancement.DisplayName,
                        Draft.Enhancements.Any(selection => selection != null &&
                            string.Equals(selection.EnhancementId,
                                enhancement.EnhancementId,
                                StringComparison.Ordinal))));
                }
            }
            return new WorkspaceDraftView(
                draftSource,
                draftCaster ?? string.Empty,
                Draft.TargetMode,
                Draft.DirectTargetUnitId ?? string.Empty,
                Draft.Origin == null || Draft.Origin.IsCasterCentered
                    ? string.Empty : Draft.Origin.AnchorUnitId,
                _rememberedDirectRecipient,
                Draft.State,
                sources,
                casters.Where(row => row.Capable).ToList(),
                targets, origins, enhancements);
        }

        // Resolves the exact discovered provider option serving the draft's
        // current source and caster (the ability the Add control authors).
        public AbilityKey DraftAbilityFor(CastingWorkspaceInputs inputs)
        {
            if (inputs == null) return null;
            string draftSource = string.IsNullOrEmpty(Draft.SourceId)
                ? SelectedSourceId : Draft.SourceId;
            int candidates;
            ProviderPlanningOption option = FindDraftOption(
                inputs, draftSource,
                Draft.CasterUnitId ?? SelectedCasterUnitId, out candidates);
            return candidates == 1 && option != null
                ? option.Provider.Key.Ability : null;
        }

        private ProviderPlanningOption FindDraftOption(
            CastingWorkspaceInputs inputs, string draftSource, string draftCaster,
            out int candidateCount)
        {
            candidateCount = 0;
            if (inputs.ProviderOptions == null ||
                inputs.EffectsBySource == null ||
                string.IsNullOrEmpty(draftCaster)) return null;
            EffectExpression sourceExpression;
            if (!inputs.EffectsBySource.TryGetValue(
                    draftSource, out sourceExpression)) return null;
            ProviderPlanningOption match = null;
            foreach (ProviderPlanningOption option in inputs.ProviderOptions)
            {
                if (option == null || option.Provider == null ||
                    !string.Equals(option.Provider.Key.CasterUnitId,
                        draftCaster, StringComparison.Ordinal) ||
                    !OptionServesExpression(inputs, option, sourceExpression))
                    continue;
                candidateCount++;
                if (match == null ||
                    string.Compare(option.Provider.Key.Canonical,
                        match.Provider.Key.Canonical,
                        StringComparison.Ordinal) < 0)
                    match = option;
            }
            return match;
        }

        private static bool OptionServesExpression(
            CastingWorkspaceInputs inputs,
            ProviderPlanningOption option,
            EffectExpression sourceExpression)
        {
            EffectExpression abilityExpression;
            return option.Provider.Key.Ability != null &&
                inputs.EffectsBySource.TryGetValue(
                    option.Provider.Key.Ability.Canonical, out abilityExpression) &&
                ReferenceEquals(abilityExpression, sourceExpression);
        }

        // ------------------------------------------------------------------
        // Review and Apply (the actual presentation boundary)
        // ------------------------------------------------------------------

        // The view calls this when it actually renders a plan for review —
        // computing a preview alone is not presentation. The presented
        // contract is the SELECTED-RUN scope Apply will submit.
        public CastingPlanSignature PresentForReview(CastingWorkspaceInputs inputs)
        {
            ExplicitCastingPlan plan = Compile(inputs, SelectedRoutineId, false);
            CastingPlanSignature signature = CastingPlanSignature.For(plan);
            _review.Present(signature);
            return signature;
        }

        // The player accepts the contents as currently compiled; the
        // coordinator refuses when those contents were never presented or
        // have materially changed since presentation.
        public bool AcceptPresentedPlan(CastingWorkspaceInputs inputs)
        {
            ExplicitCastingPlan plan = Compile(inputs, SelectedRoutineId, false);
            return _review.Accept(CastingPlanSignature.For(plan)).Allowed;
        }

        public WorkspaceApplyResult Apply(
            CastingApplyMode mode,
            string scopeRoutineId,
            CastingWorkspaceInputs inputs)
        {
            if (_submissionInFlight)
                return RefusedInFlight(null);
            // Normalize the optional scope ONCE (review C1): the compiler,
            // gate, and dispatch must all see the same selected-run scope —
            // a null scope must never mean whole-plan to one consumer and
            // selected-routine to another.
            string scope = string.IsNullOrEmpty(scopeRoutineId)
                ? SelectedRoutineId : scopeRoutineId;
            // The decision is computed from the selected-run plan as it
            // exists right now — the same scope that was presented — not
            // from any earlier preview or a whole-document compile whose
            // cross-routine reservations the scope filter cannot undo.
            ExplicitCastingPlan plan = Compile(inputs, scope, false);
            CastingApplyDecision decision = _gate.Evaluate(plan, mode, scope);
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
                    plan, decision, scope);
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

        public AuthoringEditResult AddCastingFromDraft(
            CastingWorkspaceInputs inputs = null)
        {
            // The view supplies fresh discovery inputs at click time; a
            // headless caller may pass them explicitly. Resolution never
            // depends on a prior BuildView having happened.
            if (inputs != null) _lastInputs = inputs;
            // The visible controls set source and caster; the exact ability
            // is resolved HERE through the same discovery the plan compiles
            // from (review F1) — callers never compensate privately, and an
            // unresolvable or ambiguous draft is an explained refusal, never
            // a throw out of a UI callback or a substitute casting.
            string draftKey = DraftResolutionKey();
            if (Draft.Ability == null || !string.Equals(draftKey,
                    _resolvedDraftKey, StringComparison.Ordinal))
            {
                // Re-resolve whenever the (source, caster) pair changed, not
                // only when the ability is null: a stale ability or
                // spellbook under a different selection must never survive
                // to an authored record (review F1). Deliberately unresolved
                // intent keeps an explicitly supplied ability (the plan
                // marks it blocked); only a UI draft with no ability at all
                // refuses with an explanation.
                string refusal = ResolveDraftAbility();
                if (refusal != null)
                {
                    if (Draft.Ability == null)
                        return AuthoringEditResult.Refuse(refusal);
                    Draft.SpellbookGuid = null;
                }
            }
            try
            {
                PlannedCasting casting = Draft.Materialize(
                    NextCastingId(), SelectedRoutineId);
                return _authoring.AddCasting(casting);
            }
            catch (ArgumentException exception)
            {
                return AuthoringEditResult.Refuse("draft-invalid:" +
                    exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return AuthoringEditResult.Refuse("draft-invalid:" +
                    exception.Message);
            }
        }

        // Returns null when the draft ability was resolved; otherwise an
        // explained refusal reason. Resolution writes the ability and its
        // spellbook so Materialize and the compiler see the same option.
        private string ResolveDraftAbility()
        {
            if (_lastInputs == null)
                return "draft-ability-unresolved:no-discovery-inputs";
            string source = string.IsNullOrEmpty(Draft.SourceId)
                ? SelectedSourceId : Draft.SourceId;
            string caster = Draft.CasterUnitId ?? SelectedCasterUnitId;
            if (string.IsNullOrEmpty(caster))
                return "draft-ability-unresolved:no-caster-selected";
            int candidates;
            ProviderPlanningOption option = FindDraftOption(
                _lastInputs, source, caster, out candidates);
            if (candidates == 0)
                return "draft-ability-unresolved:no-provider-for-source-and-caster";
            if (candidates > 1)
                return "draft-ability-unresolved:exact-source-ambiguous:" +
                    candidates;
            Draft.Ability = option.Provider.Key.Ability;
            Draft.SpellbookGuid = option.Provider.Key.SpellbookGuid;
            _resolvedDraftKey = DraftResolutionKey();
            return null;
        }

        private string DraftResolutionKey()
        {
            string source = string.IsNullOrEmpty(Draft.SourceId)
                ? SelectedSourceId : Draft.SourceId;
            string caster = Draft.CasterUnitId ?? SelectedCasterUnitId;
            return (source ?? string.Empty) + "" + (caster ?? string.Empty);
        }

        // Group-aware focused-casting targeting edit: mode, recipient,
        // origin, and coverage change together through the canonical
        // UpdateFocusedCasting command — never direct-target cloning on a
        // group record (review G2).
        public AuthoringEditResult SetFocusedTargeting(
            CastingTargetMode mode,
            string directTargetUnitId,
            string originAnchorUnitId,
            IEnumerable<string> requiredCoverageUnitIds)
        {
            if (EditingFocusCastingId == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            PlannedCasting focused = _authoring.Document.Castings
                .FirstOrDefault(value => value != null && string.Equals(
                    value.CastingId, EditingFocusCastingId,
                    StringComparison.Ordinal));
            if (focused == null)
                return AuthoringEditResult.Refuse("focused-casting-missing");
            if (!Enum.IsDefined(typeof(CastingTargetMode), mode))
                return AuthoringEditResult.Refuse("targeting-mode-unsupported");
            string direct = mode == CastingTargetMode.DirectTarget
                ? directTargetUnitId : null;
            CastingOrigin origin = mode == CastingTargetMode.DirectTarget
                ? null
                : mode == CastingTargetMode.AnchoredOrigin
                    ? CastingOrigin.Anchored(originAnchorUnitId)
                    : CastingOrigin.CasterCentered();
            try
            {
                return UpdateFocusedCasting(focused.WithTargeting(
                    mode, direct, origin, requiredCoverageUnitIds));
            }
            catch (ArgumentException exception)
            {
                return AuthoringEditResult.Refuse(
                    "targeting-invalid:" + exception.Message);
            }
        }

        // One coherent targeting-shape operation (review F3): mode, origin,
        // direct target, and coverage change together so the draft always
        // satisfies ValidateTargetModeShape.
        public AuthoringEditResult SetDraftTargeting(
            CastingTargetMode mode,
            string directTargetUnitId,
            string originAnchorUnitId,
            IEnumerable<string> requiredCoverageUnitIds)
        {
            // Validate first; a refused command must leave the draft
            // exactly as it was (review F3).
            if (!Enum.IsDefined(typeof(CastingTargetMode), mode))
                return AuthoringEditResult.Refuse("targeting-mode-unsupported");
            if (mode == CastingTargetMode.DirectTarget &&
                string.IsNullOrWhiteSpace(directTargetUnitId))
                return AuthoringEditResult.Refuse("targeting-requires-direct-target");
            if (mode == CastingTargetMode.AnchoredOrigin &&
                string.IsNullOrWhiteSpace(originAnchorUnitId))
                return AuthoringEditResult.Refuse("targeting-requires-anchor");
            Draft.TargetMode = mode;
            Draft.DirectTargetUnitId = null;
            Draft.RequiredCoverageUnitIds.Clear();
            if (mode == CastingTargetMode.DirectTarget)
            {
                string recipient = directTargetUnitId;
                bool restored = false;
                if (string.IsNullOrWhiteSpace(recipient) &&
                    !string.IsNullOrEmpty(_rememberedDirectRecipient))
                {
                    // Switching back restores the PREVIOUSLY CHOSEN
                    // recipient explicitly; nothing is silently picked
                    // (review G2).
                    recipient = _rememberedDirectRecipient;
                    restored = true;
                }
                if (string.IsNullOrWhiteSpace(recipient))
                    return AuthoringEditResult.Refuse(
                        "targeting-requires-direct-target:" +
                        "pick a recipient to switch back");
                _rememberedDirectRecipient = recipient;
                Draft.DirectTargetUnitId = recipient;
                Draft.Origin = null;
                return AuthoringEditResult.Accept(
                    restored ? "draft-targeting:restored-recipient"
                        : "draft-targeting",
                    new string[0]);
            }
            if (!string.IsNullOrEmpty(Draft.DirectTargetUnitId))
                _rememberedDirectRecipient = Draft.DirectTargetUnitId;
            foreach (string unitId in requiredCoverageUnitIds ?? new string[0])
                if (!string.IsNullOrWhiteSpace(unitId) &&
                    !Draft.RequiredCoverageUnitIds.Contains(unitId))
                    Draft.RequiredCoverageUnitIds.Add(unitId);
            if (mode == CastingTargetMode.AnchoredOrigin)
                Draft.Origin = CastingOrigin.Anchored(originAnchorUnitId);
            else
                Draft.Origin = CastingOrigin.CasterCentered();
            return AuthoringEditResult.Accept("draft-targeting", new string[0]);
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
            AuthoringEditResult result = _authoring.RemoveCasting(
                EditingFocusCastingId);
            if (result.Applied)
                // A removed record must not leave a stale focus id behind
                // (review F4).
                EditingFocusCastingId = null;
            return result;
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
            _savedIntentSignature = DocumentIntentSignature();
        }

        // Canonical authored-document comparison (review G5): the EXISTING
        // serialization model — every persisted authored field including
        // targeting-modifier parameters, exact sources, policies, and the
        // ordered routine definitions — not a selectively maintained
        // delimiter string. Derived observations are excluded by the
        // profile model itself.
        public string DocumentIntentSignature()
        {
            CastingPlanProfile profile = CastingPlanProfile.FromDocument(
                _authoring.Document, null, null);
            // Normalize the volatile schema stamp so equality reflects
            // CONTENT, and stamp the campaign binding explicitly.
            profile.SchemaVersion = 0;
            return CampaignId + "" +
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    profile, Newtonsoft.Json.Formatting.None,
                    new Newtonsoft.Json.JsonSerializerSettings
                    {
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                        Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
                    });
        }

        // True when authored intent differs from the last explicitly saved
        // (or loaded) document — the dirty-state contract for close/reopen
        // preservation (review F6).
        public bool IsDirty
        {
            get { return !string.Equals(DocumentIntentSignature(),
                _savedIntentSignature ?? string.Empty,
                StringComparison.Ordinal); }
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
                    // Focus cannot survive a document swap (review F4).
                    EditingFocusCastingId = null;
                    _savedIntentSignature = DocumentIntentSignature();
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
            _lastInputs = inputs;
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

        private string FirstSourceId(CastingWorkspaceInputs inputs)
        {
            PlannedCasting first = _authoring.Document.Castings.FirstOrDefault();
            if (first != null) return first.SourceId;
            // With nothing authored, the initial selection is the first
            // discovered catalogue source, not a blank buff list (review C1).
            if (inputs == null || inputs.ProviderOptions == null ||
                inputs.EffectsBySource == null || inputs.ProviderOptions.Count == 0)
                return string.Empty;
            ProviderPlanningOption firstOption = inputs.ProviderOptions
                .Where(value => value != null && value.Provider != null)
                .OrderBy(value => value.Provider.Key.Canonical, StringComparer.Ordinal)
                .First();
            AbilityKey ability = firstOption.Provider.Key.Ability;
            EffectExpression abilityExpression;
            if (ability == null ||
                !inputs.EffectsBySource.TryGetValue(
                    ability.Canonical, out abilityExpression))
                return string.Empty;
            foreach (KeyValuePair<string, EffectExpression> pair in
                inputs.EffectsBySource.OrderBy(
                    value => value.Key, StringComparer.Ordinal))
                if (!string.Equals(pair.Key, ability.Canonical,
                        StringComparison.Ordinal) &&
                    ReferenceEquals(pair.Value, abilityExpression))
                    return pair.Key;
            return string.Empty;
        }

        // Eligible casters for a source derive from the DISCOVERED provider
        // options, so a fresh buff shows its casters before anything is
        // authored (review C1). The source/ability linkage uses the same
        // alias-instance contract the legacy catalogue model establishes
        // (PlannerSetupModel aliases effects[sourceId] = effects[ability]):
        // a provider serves a source when its ability's expression IS the
        // source's expression instance. Plan-derived readiness stays
        // separate and untouched.
        private static HashSet<string> CapableCasterUnitIdsForSource(
            CastingWorkspaceInputs inputs, string selectedSource)
        {
            var capable = new HashSet<string>(StringComparer.Ordinal);
            EffectExpression sourceExpression;
            if (inputs == null || inputs.ProviderOptions == null ||
                inputs.EffectsBySource == null ||
                string.IsNullOrEmpty(selectedSource) ||
                !inputs.EffectsBySource.TryGetValue(
                    selectedSource, out sourceExpression))
                return capable;
            foreach (ProviderPlanningOption option in inputs.ProviderOptions)
            {
                if (option == null || option.Provider == null) continue;
                AbilityKey ability = option.Provider.Key.Ability;
                EffectExpression abilityExpression;
                if (ability == null ||
                    !inputs.EffectsBySource.TryGetValue(
                        ability.Canonical, out abilityExpression) ||
                    !ReferenceEquals(abilityExpression, sourceExpression)) continue;
                capable.Add(option.Provider.Key.CasterUnitId);
            }
            return capable;
        }

        private List<WorkspaceCasterRow> BuildCasterRows(
            ExplicitCastingPlan plan, CastingWorkspaceInputs inputs,
            string selectedSource)
        {
            // Capability per caster for the selected buff, separated from
            // this plan's current readiness reasons for that caster.
            HashSet<string> capableIds =
                CapableCasterUnitIdsForSource(inputs, selectedSource);
            var rows = new List<WorkspaceCasterRow>();
            foreach (UnitSnapshot unit in inputs.Snapshot.Units)
            {
                bool capable = capableIds.Contains(unit.UnitId);
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
