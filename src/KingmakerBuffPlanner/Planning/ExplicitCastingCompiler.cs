using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.Planning
{
    public enum ResolvedCastingReadiness
    {
        Ready,
        Blocked,
        Draft,
        Disabled,
        AlreadySatisfied
    }

    // One intended recipient of a group casting that the resolved targeting
    // does not predict covering. The casting stays one invocation; the gap
    // stays visible instead of silently growing a second cast.
    public sealed class CoverageGap
    {
        internal CoverageGap(string unitId, string reason)
        {
            UnitId = unitId;
            Reason = reason ?? string.Empty;
        }

        public string UnitId { get; private set; }
        public string Reason { get; private set; }
    }

    // The shared resolved representation for one casting: authoring, preview,
    // preflight, and execution all consume this shape. Exactly one resolved
    // casting exists per planned casting — never an expanded sibling.
    public sealed class ResolvedCasting
    {
        internal ResolvedCasting(
            string castingId,
            string routineId,
            int order,
            string sourceId,
            AbilityKey ability,
            string casterUnitId,
            ProviderKey provider,
            CastingTargetMode targetMode,
            string directTargetUnitId,
            CastingOrigin origin,
            IReadOnlyList<string> requiredCoverageUnitIds,
            IReadOnlyList<string> predictedBeneficiaryUnitIds,
            IReadOnlyList<CoverageGap> coverageGaps,
            IReadOnlyList<TargetingModifierSelection> targetingModifiers,
            IReadOnlyList<AuthoredEnhancementSelection> enhancements,
            IReadOnlyList<string> appliedEnhancementIds,
            IReadOnlyList<string> omittedEnhancementIds,
            ExistingEffectPolicy existingEffectPolicy,
            IReadOnlyList<string> ignoredPresenceMarkers,
            ResolvedCastingReadiness readiness,
            IReadOnlyList<string> readinessReasons,
            IReadOnlyList<string> capableCasterUnitIds,
            MigrationProvenance provenance,
            IReadOnlyList<CastingCostLine> cost = null,
            IReadOnlyList<string> existingEffectNotes = null,
            IReadOnlyList<string> costShape = null,
            IReadOnlyList<string> preCoveredUnitIds = null,
            CastExecutionStrategy? executionStrategy = null,
            string executionStrategyReason = null,
            string requiredItemSelector = null)
        {
            RequiredItemSelector = requiredItemSelector ?? string.Empty;
            ExecutionStrategy = executionStrategy;
            ExecutionStrategyReason = executionStrategyReason;
            CastingId = castingId;
            RoutineId = routineId;
            Order = order;
            SourceId = sourceId;
            Ability = ability;
            CasterUnitId = casterUnitId;
            Provider = provider;
            TargetMode = targetMode;
            DirectTargetUnitId = directTargetUnitId;
            Origin = origin;
            RequiredCoverageUnitIds = requiredCoverageUnitIds;
            PredictedBeneficiaryUnitIds = predictedBeneficiaryUnitIds;
            CoverageGaps = coverageGaps;
            TargetingModifiers = targetingModifiers;
            Enhancements = enhancements;
            AppliedEnhancementIds = appliedEnhancementIds;
            OmittedEnhancementIds = omittedEnhancementIds;
            ExistingEffectPolicy = existingEffectPolicy;
            IgnoredPresenceMarkers = ignoredPresenceMarkers;
            Readiness = readiness;
            ReadinessReasons = readinessReasons;
            CapableCasterUnitIds = capableCasterUnitIds;
            Provenance = provenance;
            Cost = new ReadOnlyCollection<CastingCostLine>(
                (cost ?? new CastingCostLine[0]).ToList());
            ExistingEffectNotes = new ReadOnlyCollection<string>(
                (existingEffectNotes ?? new string[0]).ToList());
            CostShape = new ReadOnlyCollection<string>(
                (costShape ?? new string[0]).ToList());
            PreCoveredUnitIds = new ReadOnlyCollection<string>(
                (preCoveredUnitIds ?? new string[0]).ToList());
        }

        // Disclosure of the live existing-effect assessment for this
        // casting's recipients: already active (why it is skipped), present
        // but insufficient (why it still casts), or present and recast by
        // an always-recast policy. Informational; never a blocking reason.
        public IReadOnlyList<string> ExistingEffectNotes { get; private set; }

        // Mixed coverage (owner, 2026-09-24): the predicted recipients of a
        // group casting that still casts whose existing effect the plan
        // proved sufficient from readable detail (skip-if-active only). The
        // cast goes ahead for the others; a recipient here is confirmed by
        // keeping its coverage, since a game may leave a longer-lasting
        // instance unchanged.
        public IReadOnlyList<string> PreCoveredUnitIds { get; private set; }

        // The execution strategy of the resolved option with its targeting
        // modifiers and applied enhancements (null when unresolved). The
        // classic planner's rule: an enhancement that needs a native command
        // casts through one; a provider-direct enhancement (Brown-Fur
        // Powerful Change) casts through the provider's own transaction,
        // since a plain rule cast never enrols it.
        public CastExecutionStrategy? ExecutionStrategy { get; private set; }
        public string ExecutionStrategyReason { get; private set; }

        // The authored slot rule, kept beside the live Provider: that key may
        // be the plain rule once the caster has a single copy, and execution
        // must still refuse the item if it leaves the saved slot.
        public string RequiredItemSelector { get; private set; }

        // The casting's would-be cost vector as "category:pool:units",
        // independent of whether it reserves now (an already-satisfied
        // casting keeps the shape of what it would spend). The review
        // signature uses this so a skip that flips with live effects is a
        // harmless refresh, while any change of what a casting would spend
        // stays material.
        public IReadOnlyList<string> CostShape { get; private set; }

        // The complete cost vector this casting reserved atomically: native
        // pool charge, enhancement usage pools, and material components.
        public IReadOnlyList<CastingCostLine> Cost { get; private set; }

        internal ResolvedCasting WithBudgetResult(
            ResolvedCastingReadiness readiness,
            IReadOnlyList<string> readinessReasons,
            IReadOnlyList<CastingCostLine> cost,
            IReadOnlyList<string> costShape = null,
            IReadOnlyList<string> existingEffectNotes = null)
        {
            return new ResolvedCasting(
                CastingId, RoutineId, Order, SourceId, Ability, CasterUnitId,
                Provider, TargetMode, DirectTargetUnitId, Origin,
                RequiredCoverageUnitIds, PredictedBeneficiaryUnitIds, CoverageGaps,
                TargetingModifiers, Enhancements, AppliedEnhancementIds,
                OmittedEnhancementIds, ExistingEffectPolicy, IgnoredPresenceMarkers,
                readiness, readinessReasons, CapableCasterUnitIds, Provenance, cost,
                existingEffectNotes ?? ExistingEffectNotes, costShape ?? CostShape, PreCoveredUnitIds,
                ExecutionStrategy, ExecutionStrategyReason, RequiredItemSelector);
        }

        // Review R5-F5: the same casting with narrowed predicted beneficiaries
        // (a sequential forecast found an earlier casting changes what the
        // payload does to the others).
        internal ResolvedCasting WithPredictedBeneficiaries(IReadOnlyList<string> beneficiaries)
        {
            return new ResolvedCasting(
                CastingId, RoutineId, Order, SourceId, Ability, CasterUnitId,
                Provider, TargetMode, DirectTargetUnitId, Origin,
                RequiredCoverageUnitIds, new ReadOnlyCollection<string>(beneficiaries.ToList()), CoverageGaps,
                TargetingModifiers, Enhancements, AppliedEnhancementIds,
                OmittedEnhancementIds, ExistingEffectPolicy, IgnoredPresenceMarkers,
                Readiness, ReadinessReasons, CapableCasterUnitIds, Provenance, Cost,
                ExistingEffectNotes, CostShape, PreCoveredUnitIds,
                ExecutionStrategy, ExecutionStrategyReason, RequiredItemSelector);
        }

        public string CastingId { get; private set; }
        public string RoutineId { get; private set; }
        public int Order { get; private set; }
        public string SourceId { get; private set; }
        public AbilityKey Ability { get; private set; }
        public string CasterUnitId { get; private set; }
        public ProviderKey Provider { get; private set; }
        public CastingTargetMode TargetMode { get; private set; }
        public string DirectTargetUnitId { get; private set; }
        public CastingOrigin Origin { get; private set; }
        public IReadOnlyList<string> RequiredCoverageUnitIds { get; private set; }
        public IReadOnlyList<string> PredictedBeneficiaryUnitIds { get; private set; }
        public IReadOnlyList<CoverageGap> CoverageGaps { get; private set; }
        public bool CoverageIncomplete { get { return CoverageGaps.Count != 0; } }
        public IReadOnlyList<TargetingModifierSelection> TargetingModifiers { get; private set; }
        public IReadOnlyList<AuthoredEnhancementSelection> Enhancements { get; private set; }
        public IReadOnlyList<string> AppliedEnhancementIds { get; private set; }
        public IReadOnlyList<string> OmittedEnhancementIds { get; private set; }
        public ExistingEffectPolicy ExistingEffectPolicy { get; private set; }
        public IReadOnlyList<string> IgnoredPresenceMarkers { get; private set; }
        public ResolvedCastingReadiness Readiness { get; private set; }
        public IReadOnlyList<string> ReadinessReasons { get; private set; }
        public bool IsExecutable { get { return Readiness == ResolvedCastingReadiness.Ready; } }
        // Capability is separate from current readiness: a capable caster can
        // be listed even when its pools are exhausted right now.
        public IReadOnlyList<string> CapableCasterUnitIds { get; private set; }
        public MigrationProvenance Provenance { get; private set; }
    }

    public sealed class ExplicitCastingPlan
    {
        internal ExplicitCastingPlan(
            IEnumerable<ResolvedCasting> castings,
            IEnumerable<string> diagnostics,
            IEnumerable<CastingBudgetLine> budgetLines = null,
            IEnumerable<string> pendingImportNotices = null,
            CastingCapacity capacity = null)
        {
            Castings = new ReadOnlyCollection<ResolvedCasting>(castings.ToList());
            PendingImportNotices = new ReadOnlyCollection<string>(
                (pendingImportNotices ?? new string[0]).ToList());
            Diagnostics = new ReadOnlyCollection<string>(diagnostics.ToList());
            BudgetLines = new ReadOnlyCollection<CastingBudgetLine>(
                (budgetLines ?? new CastingBudgetLine[0]).ToList());
            Capacity = capacity;
        }

        // What this plan can still fund after all of its reservations, by
        // its own ledger rules (null only for a plan not built by the
        // compiler). The workspace's caster/source counts come from here.
        public CastingCapacity Capacity { get; private set; }

        // Exactly one resolved casting per planned casting, in persisted
        // order. Compilation never adds or removes castings.
        public IReadOnlyList<ResolvedCasting> Castings { get; private set; }
        public IReadOnlyList<string> Diagnostics { get; private set; }

        // Plan-wide legacy constraints not yet acknowledged (review L1): the
        // execution gate refuses every apply mode while any remain.
        public IReadOnlyList<string> PendingImportNotices { get; private set; }

        // One authoritative line per pool touched by this plan: available
        // now, requested and allocated demand, deficits, and the responsible
        // casting IDs. Unknown balances stay null, never zero.
        public IReadOnlyList<CastingBudgetLine> BudgetLines { get; private set; }

        public int ReadyInvocationCount
        {
            get
            {
                return Castings.Count(value => value.IsExecutable);
            }
        }

        public ResolvedCasting CastingById(string castingId)
        {
            return Castings.FirstOrDefault(value =>
                string.Equals(value.CastingId, castingId, StringComparison.Ordinal));
        }

        public CastingBudgetLine BudgetLineFor(string poolKey)
        {
            return BudgetLines.FirstOrDefault(line =>
                string.Equals(line.PoolKey, poolKey, StringComparison.Ordinal));
        }
    }

    // Compiles the canonical casting document against a party snapshot into
    // the shared resolved plan. Deterministic and side-effect free: opening,
    // browsing, previewing, or recompiling never mutates the document and
    // never touches live game state (reservations live in the compile-local
    // budget ledger).
    //
    // Scope: exact caster/source/variant resolution, target-mode
    // verification against the ability's structural contract, honest group
    // coverage without expansion, per-casting enhancement availability,
    // capability separated from current readiness, and atomic shared-budget
    // reservation across native pools (including linked prepared tokens),
    // enhancement usage pools, and materials. Exact physical item identity
    // (durable rod instance binding) is deliberately NOT claimed yet.
    public sealed class ExplicitCastingCompiler
    {
        public ExplicitCastingPlan Compile(
            CastingPlanDocument document,
            PartyProviderSnapshot snapshot,
            IEnumerable<ProviderPlanningOption> providerOptions,
            IReadOnlyDictionary<string, EffectExpression> effectsBySource,
            IEnumerable<CastEnhancementSnapshot> enhancements = null,
            string budgetRoutineScope = null,
            IEnumerable<ICastingTargetingModifier> targetingModifiers = null,
            bool projectEffects = false,
            ActiveEffectSnapshot liveEffects = null,
            bool strictInstant = false)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            if (effectsBySource == null) throw new ArgumentNullException("effectsBySource");
            var options = (providerOptions ?? new ProviderPlanningOption[0]).ToList();
            var enhancementList = (enhancements ?? new CastEnhancementSnapshot[0]).ToList();
            var modifierList = (targetingModifiers ?? new ICastingTargetingModifier[0])
                .Where(value => value != null).ToList();
            var castings = new List<ResolvedCasting>();
            var diagnostics = new List<string>();
            var matchedEnhancements = new Dictionary<string, List<CastEnhancementSnapshot>>(
                StringComparer.Ordinal);
            var intendedEnhancements = new Dictionary<string, List<CastEnhancementSnapshot>>(
                StringComparer.Ordinal);
            var providers = new Dictionary<string, ProviderSnapshot>(
                StringComparer.Ordinal);
            var modifierDemandsByCasting =
                new Dictionary<string, List<ModifierUsageDemand>>(
                    StringComparer.Ordinal);
            var resolvedOptions = new Dictionary<string, ProviderPlanningOption>(
                StringComparer.Ordinal);
            var plannedById = new Dictionary<string, PlannedCasting>(StringComparer.Ordinal);
            foreach (PlannedCasting casting in document.Castings)
            {
                plannedById[casting.CastingId] = casting;
                List<CastEnhancementSnapshot> matched;
                List<CastEnhancementSnapshot> intended;
                ProviderSnapshot provider;
                List<ModifierUsageDemand> modifierDemands;
                ProviderPlanningOption resolvedOption;
                castings.Add(CompileOne(casting, snapshot, options, effectsBySource,
                    enhancementList, modifierList, diagnostics, liveEffects, strictInstant,
                    out matched, out intended, out provider, out modifierDemands,
                    out resolvedOption, true));
                matchedEnhancements[casting.CastingId] = matched;
                intendedEnhancements[casting.CastingId] = intended;
                providers[casting.CastingId] = provider;
                modifierDemandsByCasting[casting.CastingId] = modifierDemands;
                resolvedOptions[casting.CastingId] = resolvedOption;
            }
            // Removal conflicts (missing-beneficial-buffs §3.B; review
            // R1-F1/F2/F3) are judged inside the budget pass, in execution
            // order, from each casting's BOUND expression: only a casting
            // that is finally admitted (funded and Ready, or already
            // satisfied and so staying active) constrains later castings of
            // its routine.
            var removalState = new RemovalConflictState();
            // Shared budget pass: every Ready casting reserves its complete
            // cost vector atomically in persisted order; a deficit blocks the
            // casting and reserves nothing anywhere. A routine scope limits
            // reservation to one routine's castings for a selected-run
            // preview; out-of-scope castings keep their compile-time
            // readiness and are explicitly not budget-checked in that view.
            // Effect projection (one-pass sequence views) carries structural
            // effect presence forward: a SkipAlreadyActive casting whose
            // recipients a proven-equal earlier executing casting already
            // covered becomes AlreadySatisfied - one fewer invocation and
            // one fewer reservation. Equivalence is deliberately minimal:
            // identical ability identity and no strength-affecting
            // enhancements on either side; anything else stays an honest
            // unknown that still casts.
            var ledger = new CastingBudgetLedger(snapshot, enhancementList);
            var grantedByUnit = new Dictionary<string, HashSet<string>>(
                StringComparer.Ordinal);
            // unit -> structural grant key -> the effect ids it predicts there.
            var grantEffects = new Dictionary<string, Dictionary<string, HashSet<string>>>(
                StringComparer.Ordinal);
            var finalized = new List<ResolvedCasting>();
            // Review R5-F5: what earlier funded castings may have changed on
            // each unit. Read only in a one-pass sequence view, and only for
            // earlier routines; an independent routine preview starts from its
            // own snapshot.
            // Review R7-F6: the full recipient state (SequentialRecipientState).
            // Review R8-F3: the routine's own earlier funded castings count in
            // every view; earlier routines only in a one-pass sequence.
            var projection = new SequentialRecipientState(projectEffects);
            // Review R9-F1: a definite structural grant of an earlier funded
            // casting of the SAME routine counts in every view for a permanent
            // effect (see the structural check below); grants of earlier
            // routines only in a one-pass sequence.
            string grantRoutine = null;
            // Review R10 (R8-F1): the live evidence still standing after the
            // funded castings so far. A funded caster-relative application by
            // one caster may replace another caster's instance of that effect
            // on the units it reaches (coexistence unverified), so those
            // instances no longer prove anything; "<unit>\u001f<effect>" ->
            // the applying caster. Same scope as the structural grants.
            ActiveEffectSnapshot standingLive = liveEffects;
            var casterRelativeApplied = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ResolvedCasting compiled in castings)
            {
                ResolvedCasting casting = compiled;
                projection.EnterRoutine(casting.RoutineId);
                if (!projectEffects && !string.Equals(grantRoutine, casting.RoutineId, StringComparison.Ordinal))
                {
                    grantedByUnit.Clear();
                    grantEffects.Clear();
                    standingLive = liveEffects;
                    casterRelativeApplied.Clear();
                }
                grantRoutine = casting.RoutineId;
                List<CastEnhancementSnapshot> matched;
                matchedEnhancements.TryGetValue(casting.CastingId, out matched);
                ProviderSnapshot provider;
                providers.TryGetValue(casting.CastingId, out provider);
                List<ModifierUsageDemand> modifierDemands;
                modifierDemandsByCasting.TryGetValue(
                    casting.CastingId, out modifierDemands);
                // The would-be cost vector, known whenever the exact
                // provider resolved (also for skipped or blocked castings).
                // A skipped casting keeps the shape of its INTENDED
                // enhancements (an exhausted rod after the run included), so
                // a skip is never a material change by itself.
                List<CastEnhancementSnapshot> intended;
                intendedEnhancements.TryGetValue(casting.CastingId, out intended);
                IReadOnlyList<string> shape = provider == null
                    ? new string[0]
                    : CostShapeOf(ledger.DemandsFor(provider,
                        casting.Readiness == ResolvedCastingReadiness.AlreadySatisfied
                            ? intended : matched, modifierDemands));
                ProviderPlanningOption resolved;
                resolvedOptions.TryGetValue(casting.CastingId, out resolved);
                // Review R9 (R8-F3): a group casting skipped as already
                // satisfied for its INITIAL beneficiaries is re-assessed when
                // earlier funded castings change who its burst benefits (Good
                // Hope after an earlier Good Hope cleared a newly eligible
                // ally's Despair): recipients, existing-effect verdict, notes
                // and readiness are recomputed before the budget decides.
                PlannedCasting planned;
                // Review R10 (R8-F1): a skip resting on a live caster-relative
                // instance that another caster's funded application may since
                // have replaced is re-assessed against the standing evidence.
                // Review R11 (R8-F1): a Ready row too (its per-recipient
                // pre-covered credit and notes rest on the same evidence).
                if ((casting.Readiness == ResolvedCastingReadiness.AlreadySatisfied ||
                        casting.Readiness == ResolvedCastingReadiness.Ready) && liveEffects != null &&
                    casterRelativeApplied.Count != 0 && plannedById.TryGetValue(casting.CastingId, out planned) &&
                    ReplacedByOtherCaster(casting, effectsBySource, casterRelativeApplied))
                {
                    casting = CompileOne(planned, snapshot, options, effectsBySource, enhancementList,
                        modifierList, new List<string>(), standingLive, strictInstant, out matched,
                        out intended, out provider, out modifierDemands, out resolved, true);
                    shape = provider == null
                        ? new string[0]
                        : CostShapeOf(ledger.DemandsFor(provider,
                            casting.Readiness == ResolvedCastingReadiness.AlreadySatisfied
                                ? intended : matched, modifierDemands));
                }
                if (projection.HasChanges && resolved != null &&
                    casting.TargetMode != CastingTargetMode.DirectTarget &&
                    casting.Readiness == ResolvedCastingReadiness.AlreadySatisfied &&
                    plannedById.TryGetValue(casting.CastingId, out planned))
                {
                    EffectExpression payload = DiscoveredEffectLookup.ForAbility(
                        effectsBySource, casting.Ability, casting.SourceId);
                    string anchor = casting.TargetMode == CastingTargetMode.CasterCenteredOrigin
                        ? casting.CasterUnitId : casting.Origin == null ? null : casting.Origin.AnchorUnitId;
                    List<string> now = projection.Beneficiaries(resolved, payload, anchor);
                    if (!now.OrderBy(v => v, StringComparer.Ordinal).SequenceEqual(
                            casting.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal)))
                    {
                        casting = CompileOne(planned, snapshot, options, effectsBySource, enhancementList,
                            modifierList, new List<string>(), standingLive, strictInstant, out matched,
                            out intended, out provider, out modifierDemands, out resolved, true, now);
                        shape = provider == null
                            ? new string[0]
                            : CostShapeOf(ledger.DemandsFor(provider,
                                casting.Readiness == ResolvedCastingReadiness.AlreadySatisfied
                                    ? intended : matched, modifierDemands));
                    }
                }
                // Review R5-F5: a group casting's predicted beneficiary keeps
                // its claim only while its payload still lands under what
                // earlier funded castings may have changed (Mass Reduce on an
                // ally an earlier Mass Enlarge reached removes Enlarge
                // instead). Nobody left: nothing to cast for.
                // Review R7-F6: likewise a casting refused at compile time ONLY
                // because its burst reached nobody eligible is re-evaluated: an
                // earlier routine may have made its allies eligible (Good Hope
                // after an earlier Good Hope removed their Despair).
                bool deferredEmpty = casting.Readiness == ResolvedCastingReadiness.Blocked &&
                    casting.ReadinessReasons.Count != 0 && casting.ReadinessReasons.All(code =>
                        code.StartsWith(PredictedCoverageEmpty, StringComparison.Ordinal));
                if (projection.HasChanges && resolved != null &&
                    casting.TargetMode != CastingTargetMode.DirectTarget &&
                    (casting.Readiness == ResolvedCastingReadiness.Ready || deferredEmpty))
                {
                    EffectExpression payload = DiscoveredEffectLookup.ForAbility(
                        effectsBySource, casting.Ability, casting.SourceId);
                    string anchor = casting.TargetMode == CastingTargetMode.CasterCenteredOrigin
                        ? casting.CasterUnitId : casting.Origin == null ? null : casting.Origin.AnchorUnitId;
                    List<string> now = projection.Beneficiaries(resolved, payload, anchor);
                    if (now.Count == 0 && casting.Readiness == ResolvedCastingReadiness.Ready)
                    {
                        // Review R8-F3: a cast that would undo an earlier
                        // requested effect keeps its specific refusal.
                        string conflict = removalState.RemovesProvided(casting.RoutineId,
                            ChangesFor(casting, resolved, effectsBySource, true));
                        if (conflict != null)
                        {
                            finalized.Add(BlockForRemoval(casting, RemovesPlannedEffect + ":" + conflict, shape,
                                diagnostics, removalState.OwnersRemovedByEarlier(casting.RoutineId,
                                    ChangesFor(casting, resolved, effectsBySource, true))));
                            continue;
                        }
                    }
                    if (now.Count == 0)
                    {
                        // Review R6 (R5-F5): the stored prediction is the
                        // projected one - nobody.
                        casting = casting.WithPredictedBeneficiaries(now);
                        finalized.Add(casting.WithBudgetResult(ResolvedCastingReadiness.Blocked,
                            casting.ReadinessReasons.Concat(new[] { PredictedCoverageEmpty + anchor })
                                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal)
                                .ToList(),
                            new CastingCostLine[0], shape, BlockedNotes(casting, null)));
                        continue;
                    }
                    if (deferredEmpty)
                        casting = casting.WithBudgetResult(ResolvedCastingReadiness.Ready, new string[0],
                            new CastingCostLine[0], shape);
                    if (!now.OrderBy(v => v, StringComparer.Ordinal).SequenceEqual(
                            casting.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal)))
                        casting = casting.WithPredictedBeneficiaries(now);
                }
                // Review R5-F4: a skip changes nothing; it protects only the
                // effects on the recipients its satisfaction rests on (its
                // beneficiaries), not every unit inside its burst. A cast that
                // executes is analyzed over every unit it reaches.
                EffectChanges changes = casting.Readiness == ResolvedCastingReadiness.Ready ||
                    casting.Readiness == ResolvedCastingReadiness.AlreadySatisfied
                    ? ChangesFor(casting, resolved, effectsBySource,
                        casting.Readiness == ResolvedCastingReadiness.Ready) : null;
                string removalConflict;
                if (casting.Readiness == ResolvedCastingReadiness.AlreadySatisfied)
                {
                    // R1-F1: a skip is only truthful while nothing admitted
                    // earlier in the routine removes the effect it relies on.
                    removalConflict = removalState.RemovedByEarlier(casting.RoutineId, changes);
                    if (removalConflict != null)
                    {
                        finalized.Add(BlockForRemoval(casting, EffectRemovedByEarlierCasting +
                            ":" + removalConflict, shape, diagnostics,
                            removalState.OwnersRemovedByEarlier(casting.RoutineId, changes)));
                        continue;
                    }
                    removalState.Admit(casting.RoutineId, casting.CastingId, changes, false);
                }
                bool outOfScope = budgetRoutineScope != null &&
                    casting.RoutineId != budgetRoutineScope;
                // A proven structural repeat (one-pass view) casts nothing:
                // it is decided before any removal check and, like a live
                // already-active skip, protects the effect it keeps for its
                // routine (review R2-F2).
                // Review R9-F1: outside a one-pass sequence only a PERMANENT
                // effect's same-routine repeat is structurally satisfied (it
                // can change nothing: a resistance selection already chosen
                // earlier in the routine); a timed effect's repeat still
                // refreshes its duration and stays charged there (the one-pass
                // view reports it redundant).
                if (casting.Readiness == ResolvedCastingReadiness.Ready && !outOfScope &&
                    (projectEffects || HasOnlyPermanentApplications(casting, effectsBySource)) &&
                    casting.ExistingEffectPolicy == ExistingEffectPolicy.SkipAlreadyActive &&
                    IsStructurallySatisfied(casting, matched, grantedByUnit,
                        HasCasterRelativePayload(casting, effectsBySource)))
                {
                    // R5-F4: like a live skip, only its beneficiaries' effects.
                    removalState.Admit(casting.RoutineId, casting.CastingId,
                        ChangesFor(casting, resolved, effectsBySource, false), false);
                    finalized.Add(casting.WithBudgetResult(
                        ResolvedCastingReadiness.AlreadySatisfied,
                        new[] { "already-satisfied-structural" },
                        new CastingCostLine[0], shape));
                    continue;
                }
                if (casting.Readiness == ResolvedCastingReadiness.Ready)
                {
                    removalConflict = removalState.RemovesProvided(casting.RoutineId, changes);
                    if (removalConflict != null)
                    {
                        finalized.Add(BlockForRemoval(casting, RemovesPlannedEffect + ":" +
                            removalConflict, shape, diagnostics,
                            removalState.OwnersRemovedByEarlier(casting.RoutineId, changes)));
                        continue;
                    }
                    // An out-of-scope casting is not budget-checked in this
                    // view; it keeps its compile-time readiness, so it
                    // constrains the rest of its own routine provisionally.
                    if (outOfScope)
                    {
                        removalState.Admit(casting.RoutineId, casting.CastingId, changes);
                        RecordSequential(projection, casting, resolved, effectsBySource);
                        standingLive = RecordCasterRelative(casting, resolved, effectsBySource, standingLive,
                            casterRelativeApplied);
                    }
                }
                if (casting.Readiness != ResolvedCastingReadiness.Ready || outOfScope)
                {
                    // Review R6 (R5-F6): a row that arrives already refused
                    // (an initially exhausted pool, a disabled or draft row)
                    // does not run either; its notes say so.
                    bool runs = casting.Readiness == ResolvedCastingReadiness.Ready ||
                        casting.Readiness == ResolvedCastingReadiness.AlreadySatisfied;
                    finalized.Add(casting.WithBudgetResult(casting.Readiness,
                        casting.ReadinessReasons, casting.Cost, shape,
                        runs ? null : BlockedNotes(casting, null)));
                    continue;
                }
                IReadOnlyList<CastingDemand> demands = ledger.DemandsFor(
                    provider, matched, modifierDemands);
                IReadOnlyList<CastingCostLine> cost;
                string reason;
                if (ledger.TryReserveAtomically(
                        casting.CastingId, provider, demands, out cost, out reason))
                {
                    finalized.Add(casting.WithBudgetResult(
                        casting.Readiness, casting.ReadinessReasons, cost, shape));
                    // R1-F2: only a funded casting enters the routine's
                    // removal state.
                    removalState.Admit(casting.RoutineId, casting.CastingId, changes);
                    RecordSequential(projection, casting, resolved, effectsBySource);
                    standingLive = RecordCasterRelative(casting, resolved, effectsBySource, standingLive,
                        casterRelativeApplied);
                    {
                        // R1-F4: the one-pass sequence forgets a structural
                        // grant whose effect this casting removes (review
                        // R9-F1: within one routine in every view).
                        InvalidateRemovedGrants(changes, grantedByUnit, grantEffects);
                        bool casterRelative = HasCasterRelativePayload(casting, effectsBySource);
                        if (casterRelative)
                            InvalidateOtherCasterGrants(changes, StructuralGrantKey(casting, true),
                                grantedByUnit, grantEffects);
                        ProjectEffects(casting, matched, grantedByUnit, changes, grantEffects, casterRelative);
                    }
                    continue;
                }
                ledger.RecordUnfunded(casting.CastingId, demands);
                var reasons = casting.ReadinessReasons
                    .Concat(new[] { reason })
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal).ToList();
                // Review R5-F6: a casting the shared ledger refuses does not
                // run either; its notes say so (effects an earlier admitted
                // casting removes are named as removed).
                finalized.Add(casting.WithBudgetResult(
                    ResolvedCastingReadiness.Blocked, reasons, new CastingCostLine[0], shape,
                    BlockedNotes(casting, removalState.OwnersRemovedByEarlier(casting.RoutineId, changes))));
            }
            AddDuplicateRequestWarnings(finalized, diagnostics);
            return new ExplicitCastingPlan(finalized, diagnostics, ledger.BuildReport(),
                document.PendingImportNotices, new CastingCapacity(ledger));
        }

        private static ResolvedCasting CompileOne(
            PlannedCasting casting,
            PartyProviderSnapshot snapshot,
            List<ProviderPlanningOption> options,
            IReadOnlyDictionary<string, EffectExpression> effectsBySource,
            List<CastEnhancementSnapshot> enhancements,
            List<ICastingTargetingModifier> targetingModifiers,
            List<string> diagnostics,
            ActiveEffectSnapshot liveEffects,
            bool strictInstant,
            out List<CastEnhancementSnapshot> matchedEnhancements,
            out List<CastEnhancementSnapshot> intendedEnhancements,
            out ProviderSnapshot providerSnapshot,
            out List<ModifierUsageDemand> modifierDemands,
            out ProviderPlanningOption resolvedOption,
            bool deferEmptyGroupCoverage = false,
            IReadOnlyList<string> predictedOverride = null)
        {
            var reasons = new List<string>();
            string requiredItemSelector = ItemSourceResolution.RequiredSelector(casting.Ability);
            AbilityKey effectiveAbility = ItemSourceResolution.EffectiveAbility(
                casting.Ability, casting.CasterUnitId, snapshot.Providers);
            if (!ReferenceEquals(effectiveAbility, casting.Ability))
                casting = casting.WithProvider(casting.CasterUnitId, effectiveAbility,
                    casting.SpellbookGuid, casting.Enhancements);
            var capableCasters = snapshot.Providers
                .Where(provider => string.Equals(
                    provider.Key.Ability.Canonical, casting.Ability.Canonical,
                    StringComparison.Ordinal))
                .Select(provider => provider.Key.CasterUnitId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToList();
            // Review L1: unresolved imported review items (provider pin,
            // enhancement requiredness, grouping/origin review, missing
            // caster or recipient) keep the casting out of executable
            // readiness whatever its authored state says.
            if (casting.Provenance != null)
                foreach (string item in casting.Provenance.UnresolvedReviewItems)
                    reasons.Add("import-review-unresolved:" + item);
            ProviderPlanningOption option = ResolveOption(
                casting, snapshot, options, capableCasters, reasons);
            VerifyAbilityTargetMode(casting, effectsBySource, reasons);
            IReadOnlyList<string> predicted = new string[0];
            var gaps = new List<CoverageGap>();
            modifierDemands = new List<ModifierUsageDemand>();
            var appliedModifiers = new List<ICastingTargetingModifier>();
            if (option != null)
                option = ApplyTargetingModifiers(
                    casting, targetingModifiers, option, reasons, modifierDemands,
                    appliedModifiers);
            // Resources are judged AFTER targeting, enhancements and the live
            // existing-effect assessment: a casting whose effect is already
            // sufficiently active needs no resources, so an exhausted pool
            // must not block it (repeat use right after a routine ran).
            var resourceReasons = new List<string>();
            if (option != null)
                HasSpendableResources(option, snapshot, resourceReasons);
            if (option != null)
            {
                predicted = ResolveTargeting(casting, option, snapshot, gaps, reasons, deferEmptyGroupCoverage);
                if (predicted == null)
                {
                    predicted = new string[0];
                    option = null;
                }
                // Review R9 (R8-F3): the budget pass re-assesses a group
                // casting for the beneficiaries earlier funded castings of
                // the sequence leave it.
                else if (predictedOverride != null && casting.TargetMode != CastingTargetMode.DirectTarget)
                    predicted = predictedOverride.ToList();
            }
            var applied = new List<string>();
            var omitted = new List<string>();
            var matched = new List<CastEnhancementSnapshot>();
            var intended = new List<CastEnhancementSnapshot>();
            var requiredExhausted = new List<CastEnhancementSnapshot>();
            CastExecutionStrategy? strategy = null;
            string strategyReason = null;
            if (option != null)
            {
                ResolveEnhancements(casting, option, enhancements, applied, omitted,
                    matched, intended, requiredExhausted, reasons, resourceReasons);
                // R579-1: the COMPLETE executable enhancement contract
                // resolves BEFORE the strategy freezes. An applied targeting
                // modifier (Share) contributes its VERIFIED enhancement
                // snapshot — resolved against the same enhancement list the
                // host supplied, applicability checked — so the existing
                // execution policy selects the correct route (native command
                // precedence, verified direct provider, fallbacks). The
                // modifier's cost is NOT counted again: UsageDemands already
                // carries it, and the ledger aggregates `matched` only.
                var modifierExecuted = new List<CastEnhancementSnapshot>();
                foreach (ICastingTargetingModifier modifier in appliedModifiers)
                    foreach (string executionId in modifier.ExecutionEnhancementIds(
                             casting, option))
                    {
                        CastEnhancementSnapshot execution = enhancements.FirstOrDefault(
                            value => string.Equals(value.EnhancementId, executionId,
                                StringComparison.Ordinal) &&
                            string.Equals(value.CasterUnitId, casting.CasterUnitId,
                                StringComparison.Ordinal));
                        if (execution == null)
                        {
                            reasons.Add("targeting-modifier-execution-unverified:" +
                                executionId);
                            option = null;
                            continue;
                        }
                        string executionApplicability =
                            execution.ApplicabilityFailure(option.Provider);
                        if (executionApplicability.Length != 0)
                        {
                            reasons.Add("targeting-modifier-execution-inapplicable:" +
                                executionId + ":" + executionApplicability);
                            option = null;
                            continue;
                        }
                        if (!modifierExecuted.Contains(execution))
                            modifierExecuted.Add(execution);
                    }
                // The applied enhancements decide how the cast must execute,
                // exactly as for the classic planner - now together with the
                // modifier-backed ones. The effective strategy (after
                // targeting modifiers too) is always recorded.
                ProviderPlanningOption effective = option == null ? null
                    : CastEnhancementExecutionPolicy.Apply(
                        modifierExecuted.Count == 0
                            ? matched
                            : matched.Concat(modifierExecuted), option);
                if (effective != null)
                {
                    strategy = effective.ExecutionStrategy;
                    strategyReason = effective.ExecutionStrategyReason;
                }
                // The modifier's execution identities join the resolved
                // casting's APPLIED ids: the converter's step arms exactly
                // them through the enhancement lease, and the projection
                // hash covers them. They do not join `matched` (cost and
                // satisfaction semantics keep using the authored set).
                foreach (CastEnhancementSnapshot execution in modifierExecuted)
                    if (!applied.Contains(execution.EnhancementId))
                        applied.Add(execution.EnhancementId);
            }
            else
                foreach (AuthoredEnhancementSelection selection in casting.Enhancements)
                    if (selection.Required)
                        reasons.Add("enhancements-unvalidated:source-unresolved");
            // The execution reader can observe party members and their pets.
            // Preserve native secondary selection, including uncertainty, but
            // make known unsupported owners a per-casting admission blocker.
            // This precedes active-effect skipping and the budget pass: neither
            // can turn an unobservable required effect into executable intent.
            if (option != null)
            {
                EffectExpression expected = option.BindEffects(DiscoveredEffectLookup.ForAbility(
                    effectsBySource, casting.Ability, casting.SourceId), casting.DirectTargetUnitId);
                var readableOwners = new HashSet<string>(snapshot.Units.Select(unit => unit.UnitId), StringComparer.Ordinal);
                foreach (string owner in predicted.SelectMany(primary =>
                    new EffectRecipientScope(expected, casting.CasterUnitId, primary).UnitIds).Distinct(StringComparer.Ordinal))
                    if (owner == EffectRecipientBinding.UnresolvedSecondaryRecipient)
                        reasons.Add("effect-owner-unresolved");
                    else if (!readableOwners.Contains(owner))
                        reasons.Add("effect-owner-not-readable:" + owner);
            }
            // Only a structurally valid, authored-Ready casting may be
            // satisfied by live effects; a structural block (caster, target,
            // enhancement, modifier, import review) stays a block that the
            // player must resolve, whatever is active right now. Resource
            // exhaustion alone is overridden by a sufficient active effect.
            var existingNotes = new List<string>();
            var satisfiedUnits = new List<string>();
            var preCovered = new List<string>();
            bool alreadyActive = liveEffects != null && option != null &&
                reasons.Count == 0 && casting.State == CastingAuthoringState.Ready &&
                AssessExistingEffects(casting, option,
                    matched.Concat(requiredExhausted).ToList(), predicted,
                    effectsBySource, liveEffects, existingNotes, satisfiedUnits, preCovered);
            if (alreadyActive)
                reasons.Add("already-active:" + string.Join(",", satisfiedUnits.ToArray()));
            else
                reasons.AddRange(resourceReasons);
            // WP4 strict Instant: a casting whose effective route is not a
            // qualified instant rule cast is Not Ready in Instant mode (it
            // never silently animates); an already-active skip casts nothing
            // and stays a skip. The reason names this casting, so the gate
            // and Not Ready navigation identify it structurally.
            if (strictInstant && !alreadyActive && strategy.HasValue &&
                !CastingExecutionPolicy.IsInstantCapable(strategy.Value))
                reasons.Add(CastingExecutionPolicy.InstantRouteUnavailable + ":" +
                    strategy.Value + ":" + (strategyReason ?? string.Empty));
            ResolvedCastingReadiness readiness =
                casting.State == CastingAuthoringState.Draft
                    ? ResolvedCastingReadiness.Draft
                    : casting.State == CastingAuthoringState.Disabled
                        ? ResolvedCastingReadiness.Disabled
                        : alreadyActive
                            ? ResolvedCastingReadiness.AlreadySatisfied
                            : reasons.Count == 0
                                ? ResolvedCastingReadiness.Ready
                                : ResolvedCastingReadiness.Blocked;
            // Targeting modifiers change recipient eligibility only: an
            // enabled modifier transforms the proven option or blocks with a
            // repairable reason. With no host registry the selection stays
            // an unvalidated diagnostic instead of being guessed.
            foreach (string modifier in casting.TargetingModifiers
                .Where(value => !value.Enabled)
                .Select(value => value.ModifierId))
                diagnostics.Add("targeting-modifier-disabled:" + modifier +
                    ":" + casting.CastingId);
            matchedEnhancements = matched;
            intendedEnhancements = intended;
            providerSnapshot = option == null ? null : option.Provider;
            resolvedOption = option;
            return new ResolvedCasting(
                casting.CastingId, casting.RoutineId, casting.Order, casting.SourceId,
                casting.Ability, casting.CasterUnitId,
                option == null ? null : option.Provider.Key,
                casting.TargetMode, casting.DirectTargetUnitId, casting.Origin,
                casting.RequiredCoverageUnitIds, predicted, gaps,
                casting.TargetingModifiers, casting.Enhancements, applied, omitted,
                casting.ExistingEffectPolicy, casting.IgnoredPresenceMarkers,
                readiness,
                reasons.Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal).ToList(),
                capableCasters, casting.Provenance, null, existingNotes, null, preCovered,
                strategy, strategyReason, requiredItemSelector);
        }

        // Applies the casting's enabled targeting modifiers in authored
        // order. A modifier transforms the proven option (eligibility
        // change); an unavailable modifier blocks the casting with a
        // repairable reason; an unknown or unvalidated enabled selection
        // blocks rather than becoming permission to execute the unmodified
        // spell. A casting that requests no modifier is unaffected by an
        // absent registry.
        private static ProviderPlanningOption ApplyTargetingModifiers(
            PlannedCasting casting,
            List<ICastingTargetingModifier> modifiers,
            ProviderPlanningOption option,
            List<string> reasons,
            List<ModifierUsageDemand> modifierDemands,
            List<ICastingTargetingModifier> appliedModifiers)
        {
            var applied = new List<ICastingTargetingModifier>();
            foreach (TargetingModifierSelection selection in casting.TargetingModifiers)
            {
                if (!selection.Enabled) continue;
                ICastingTargetingModifier modifier = modifiers.FirstOrDefault(
                    value => string.Equals(value.ModifierId, selection.ModifierId,
                        StringComparison.Ordinal));
                if (modifier == null)
                {
                    // An enabled modifier the host cannot validate is not
                    // permission to execute the unmodified spell: the casting
                    // blocks as repairable intent either way. With no
                    // registry at all the block is explicitly "unresolved"
                    // (the selection remains editable and repairs when a
                    // registry appears); against a registry an unknown id is
                    // a dead selection that can never repair.
                    reasons.Add(modifiers.Count == 0
                        ? "targeting-modifier-unresolved:" + selection.ModifierId
                        : "targeting-modifier-unknown:" + selection.ModifierId);
                    return null;
                }
                CastingModifierResult result = modifier.Apply(casting, option);
                if (!result.IsApplied)
                {
                    reasons.Add("targeting-modifier-unavailable:" +
                        selection.ModifierId + ":" + result.UnavailableReason);
                    return null;
                }
                option = result.Option;
                applied.Add(modifier);
                appliedModifiers.Add(modifier);
            }
            // Every applied modifier's verified cost enters the same atomic
            // cost vector; an unresolved modifier never reaches here, so an
            // unknown cost is never treated as free.
            foreach (ICastingTargetingModifier modifier in applied)
                modifierDemands.AddRange(modifier.UsageDemands(casting, option));
            return option;
        }

        public const string RemovesPlannedEffect = "removes-planned-effect";

        // A skip-if-active casting whose effect an earlier admitted casting
        // of the same routine removes (review R1-F1).
        public const string EffectRemovedByEarlierCasting = "effect-removed-by-earlier-casting";

        // Missing-beneficial-buffs mission §3.B: mutually exclusive
        // selections are never planned as stackable. A casting whose own
        // native actions may remove an effect that an earlier ADMITTED
        // casting of the same routine applies (or keeps active, as an
        // already-satisfied skip) on the same unit would undo that casting's
        // claimed coverage - the wizard's Abjuration resistances each apply
        // their own buff and remove the other four; Enlarge and Reduce
        // Person, Mass remove each other instead of applying. The later
        // casting is blocked with the conflict named; it casts nothing and
        // reserves nothing. Conversely a skip that relies on an effect an
        // earlier admitted casting removes is no longer satisfied and is
        // blocked with that conflict named (it is never silently counted as
        // covered). Switching away from an effect active before the run is
        // ordinary native behavior and is not blocked. Each routine is its
        // own run, so the state is per routine. The analysis reads the
        // casting's bound expression (EffectChangeAnalysis): only realizable
        // removals count, and only a certain later re-application on the
        // same path cancels one. The retired Classic planner shares it
        // (review R1-F5).
        internal sealed class RemovalConflictState
        {
            // routine -> unit -> effect -> earlier admitted casting providing it
            private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _provided =
                new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.Ordinal);
            // routine -> unit -> effect -> earlier admitted casting removing it
            private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _removed =
                new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.Ordinal);

            // "<earlier casting>:<effect>:<unit>" for the first removal by
            // this casting of an effect an earlier admitted one provides.
            internal string RemovesProvided(string routineId, EffectChanges changes)
            {
                return First(Map(_provided, routineId), changes == null ? null : changes.Removed);
            }

            // "<earlier casting>:<effect>:<unit>" for the first effect this
            // casting relies on that an earlier admitted one removes.
            internal string RemovedByEarlier(string routineId, EffectChanges changes)
            {
                return First(Map(_removed, routineId), changes == null ? null : changes.Applied);
            }

            // The units on which an earlier admitted casting removes an
            // effect this casting relies on (review R3-F4).
            internal HashSet<string> OwnersRemovedByEarlier(string routineId, EffectChanges changes)
            {
                Dictionary<string, Dictionary<string, string>> removed = Map(_removed, routineId);
                var owners = new HashSet<string>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, string> key in changes == null
                    ? new KeyValuePair<string, string>[0] : changes.Applied)
                {
                    Dictionary<string, string> effects;
                    if (key.Key != null && removed.TryGetValue(key.Key, out effects) && effects.ContainsKey(key.Value))
                        owners.Add(key.Key);
                }
                return owners;
            }

            // casts: false for an already-satisfied skip, which keeps its
            // effect active but runs none of its actions (no removal).
            internal void Admit(string routineId, string castingId, EffectChanges changes, bool casts = true)
            {
                if (changes == null) return;
                Dictionary<string, Dictionary<string, string>> provided = Map(_provided, routineId);
                Dictionary<string, Dictionary<string, string>> removed = Map(_removed, routineId);
                foreach (KeyValuePair<string, string> removal in casts ? changes.Removed
                    : new KeyValuePair<string, string>[0])
                {
                    Dictionary<string, string> effects;
                    if (provided.TryGetValue(removal.Key, out effects)) effects.Remove(removal.Value);
                    Set(removed, removal, castingId);
                }
                // Review R1-F3: only an application certain on every path
                // restores an effect an earlier casting may have removed; a
                // possible one (an alternative branch) does not.
                foreach (KeyValuePair<string, string> application in changes.Guaranteed)
                {
                    Dictionary<string, string> effects;
                    if (removed.TryGetValue(application.Key, out effects)) effects.Remove(application.Value);
                }
                foreach (KeyValuePair<string, string> application in changes.Applied)
                    Set(provided, application, castingId);
            }

            private static string First(Dictionary<string, Dictionary<string, string>> state,
                IEnumerable<KeyValuePair<string, string>> keys)
            {
                foreach (KeyValuePair<string, string> key in keys ?? new KeyValuePair<string, string>[0])
                {
                    Dictionary<string, string> effects;
                    string casting;
                    if (key.Key != null && state.TryGetValue(key.Key, out effects) &&
                        effects.TryGetValue(key.Value, out casting))
                        return casting + ":" + key.Value + ":" + key.Key;
                }
                return null;
            }

            private static void Set(Dictionary<string, Dictionary<string, string>> state,
                KeyValuePair<string, string> key, string castingId)
            {
                if (key.Key == null) return;
                Dictionary<string, string> effects;
                if (!state.TryGetValue(key.Key, out effects))
                    state[key.Key] = effects = new Dictionary<string, string>(StringComparer.Ordinal);
                effects[key.Value] = castingId;
            }

            private static Dictionary<string, Dictionary<string, string>> Map(
                Dictionary<string, Dictionary<string, Dictionary<string, string>>> all, string routineId)
            {
                Dictionary<string, Dictionary<string, string>> state;
                if (!all.TryGetValue(routineId ?? string.Empty, out state))
                    all[routineId ?? string.Empty] = state =
                        new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
                return state;
            }
        }

        // The effects a casting may apply and realizably remove on its
        // predicted recipients, from the same provider binding execution
        // uses (caster facts; nearby-recipient selection per target).
        public const string PredictedCoverageEmpty = "predicted-coverage-empty:";

        // Review R7-F6: a funded casting's changes enter the sequential state
        // for every unit it reaches (beneficiaries and units inside its burst).
        private static void RecordSequential(SequentialRecipientState state, ResolvedCasting casting,
            ProviderPlanningOption option, IReadOnlyDictionary<string, EffectExpression> effectsBySource)
        {
            if (option == null) return;
            EffectExpression expression = DiscoveredEffectLookup.ForAbility(
                effectsBySource, casting.Ability, casting.SourceId);
            IEnumerable<string> reached = casting.TargetMode == CastingTargetMode.DirectTarget
                ? new[] { casting.DirectTargetUnitId } : casting.PredictedBeneficiaryUnitIds;
            if (casting.TargetMode != CastingTargetMode.DirectTarget)
            {
                string anchor = casting.TargetMode == CastingTargetMode.CasterCenteredOrigin
                    ? casting.CasterUnitId : casting.Origin == null ? null : casting.Origin.AnchorUnitId;
                if (anchor != null) reached = reached.Concat(option.AffectedTargetIdsForAnchor(anchor));
            }
            state.Record(option, expression, reached, casting.CasterUnitId);
        }

        private static EffectChanges ChangesFor(ResolvedCasting casting, ProviderPlanningOption option,
            IReadOnlyDictionary<string, EffectExpression> effectsBySource, bool includeAffected = true)
        {
            if (option == null) return null;
            EffectExpression expression = DiscoveredEffectLookup.ForAbility(
                effectsBySource, casting.Ability, casting.SourceId);
            if (expression == null) return null;
            // Review R3-F1: a group cast's native actions reach every unit
            // inside its burst, including an ally that only LOSES an effect
            // (Mass Reduce on an enlarged ally) and so is not a predicted
            // beneficiary; its removals are analyzed too. Review R5-F4: a
            // skip runs nothing, so only its beneficiaries count.
            IEnumerable<string> reached = casting.PredictedBeneficiaryUnitIds;
            if (includeAffected && casting.TargetMode != CastingTargetMode.DirectTarget)
            {
                string anchor = casting.TargetMode == CastingTargetMode.CasterCenteredOrigin
                    ? casting.CasterUnitId : casting.Origin == null ? null : casting.Origin.AnchorUnitId;
                if (anchor != null && option.AffectedIdsByAnchor.ContainsKey(anchor))
                    reached = reached.Concat(option.AffectedTargetIdsForAnchor(anchor))
                        .Distinct(StringComparer.Ordinal).ToList();
            }
            return EffectChanges.Merge(reached.Select(recipient =>
                EffectChangeAnalysis.Analyze(option.BindForRecipient(expression, recipient),
                    casting.CasterUnitId, recipient)));
        }

        private static ResolvedCasting BlockForRemoval(ResolvedCasting casting, string reason,
            IReadOnlyList<string> shape, List<string> diagnostics, ICollection<string> removedOwners = null)
        {
            diagnostics.Add(reason + ":by:" + casting.CastingId);
            return casting.WithBudgetResult(ResolvedCastingReadiness.Blocked,
                casting.ReadinessReasons
                    .Where(value => !value.StartsWith("already-active:", StringComparison.Ordinal))
                    .Concat(new[] { reason }).Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal).ToList(),
                new CastingCostLine[0], shape, BlockedNotes(casting, removedOwners));
        }

        // The existing-effect notes of a casting that does not run (blocked
        // for a removal conflict, by the shared ledger, or with no
        // beneficiary left).
        // Review R1-F1/R3-F4: an invalidated skip no longer says "already
        // active (skipped)". Only a recipient whose effect an earlier
        // casting removes is described as losing it; another recipient is
        // described as still active (the blocked casting does not run).
        // Review R4-F4/R5-F6: the same holds for every note of a mixed group
        // casting - none may still say the cast goes ahead, may shorten an
        // effect, or will (re)cast.
        private static IReadOnlyList<string> BlockedNotes(ResolvedCasting casting, ICollection<string> removedOwners)
        {
            return casting.ExistingEffectNotes.Select(note =>
            {
                string present = new[] { "already-active:", "already-covered:", "already-covered-longer:",
                    "existing-active-recast:" }.FirstOrDefault(prefix =>
                        note.StartsWith(prefix, StringComparison.Ordinal));
                if (present != null)
                {
                    string rest = note.Substring(present.Length);
                    int split = rest.IndexOf(':');
                    string unit = split < 0 ? rest : rest.Substring(0, split);
                    return (removedOwners != null && removedOwners.Contains(unit)
                        ? ExistingRemovedEarlierNote : ExistingKeptBlockedNote) + rest;
                }
                string partial = new[] { "existing-insufficient:", "existing-incomplete:" }.FirstOrDefault(prefix =>
                    note.StartsWith(prefix, StringComparison.Ordinal));
                return partial == null ? note : ExistingNotCastBlockedNote + note.Substring(partial.Length);
            }).ToList();
        }

        // Existing-effect note of a skip an earlier casting invalidates:
        // "existing-removed-earlier:<unit>[:detail]".
        public const string ExistingRemovedEarlierNote = "existing-removed-earlier:";
        // Note of a recipient whose effect stays active while its blocked
        // group casting does not run: "existing-kept-blocked:<unit>[:detail]".
        public const string ExistingKeptBlockedNote = "existing-kept-blocked:";
        // Note of a recipient whose effect is absent or insufficient while its
        // blocked casting does not run: "existing-not-cast-blocked:<unit>[:detail]".
        public const string ExistingNotCastBlockedNote = "existing-not-cast-blocked:";

        // R1-F4: a removal later in the one-pass sequence invalidates every
        // structural grant predicting the removed effect on that unit.
        private static void InvalidateRemovedGrants(EffectChanges changes,
            Dictionary<string, HashSet<string>> grantedByUnit,
            Dictionary<string, Dictionary<string, HashSet<string>>> grantEffects)
        {
            if (changes == null) return;
            foreach (KeyValuePair<string, string> removal in changes.Removed)
            {
                Dictionary<string, HashSet<string>> grants;
                HashSet<string> granted;
                if (removal.Key == null || !grantEffects.TryGetValue(removal.Key, out grants) ||
                    !grantedByUnit.TryGetValue(removal.Key, out granted))
                    continue;
                foreach (string key in grants.Where(pair => pair.Value.Contains(removal.Value))
                    .Select(pair => pair.Key).ToList())
                {
                    granted.Remove(key);
                    grants.Remove(key);
                }
            }
        }

        // Review R10 (R8-F1): a funded casting's caster-relative applications
        // on every unit it reaches; other casters' live instances of those
        // effects there stop counting as evidence.
        private static ActiveEffectSnapshot RecordCasterRelative(ResolvedCasting casting,
            ProviderPlanningOption option, IReadOnlyDictionary<string, EffectExpression> effectsBySource,
            ActiveEffectSnapshot standing, Dictionary<string, string> applied)
        {
            EffectExpression payload = DiscoveredEffectLookup.ForAbility(
                effectsBySource, casting.Ability, casting.SourceId);
            IList<KeyValuePair<string, bool>> roles = payload == null ? new List<KeyValuePair<string, bool>>()
                : EffectApplicationLifetime.CasterRelativeRoles(payload);
            if (roles.Count == 0) return standing;
            // Review R11 (R8-F1): only where each leaf can land - the caster
            // for a caster-role leaf; otherwise the direct target, or every
            // unit the group cast's selected disk may reach.
            IEnumerable<string> recipients = casting.TargetMode == CastingTargetMode.DirectTarget
                ? new[] { casting.DirectTargetUnitId } : casting.PredictedBeneficiaryUnitIds;
            if (casting.TargetMode != CastingTargetMode.DirectTarget && option != null)
            {
                string anchor = casting.TargetMode == CastingTargetMode.CasterCenteredOrigin
                    ? casting.CasterUnitId : casting.Origin == null ? null : casting.Origin.AnchorUnitId;
                if (anchor != null) recipients = recipients.Concat(option.AffectedTargetIdsForAnchor(anchor));
            }
            List<string> reached = recipients.Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.Ordinal).ToList();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, bool> role in roles)
                foreach (string unit in role.Value ? new[] { casting.CasterUnitId } : (IEnumerable<string>)reached)
                {
                    if (string.IsNullOrEmpty(unit)) continue;
                    applied[unit + "\u001f" + role.Key] = casting.CasterUnitId;
                    keys.Add(unit + "\u001f" + role.Key);
                }
            if (standing == null || !standing.HasInstanceDetail) return standing;
            var filtered = new Dictionary<string, IEnumerable<ActiveEffectInstance>>(StringComparer.Ordinal);
            foreach (string unit in standing.ToUnitMarkers().Keys)
                filtered[unit] = standing.GetInstances(unit).Where(instance =>
                    !keys.Contains(unit + "\u001f" + instance.EffectId) ||
                    string.Equals(instance.CasterUnitId, casting.CasterUnitId, StringComparison.Ordinal)).ToList();
            return ActiveEffectSnapshot.FromInstances(filtered);
        }

        // Whether a skip's caster-relative effect was applied, on one of the
        // units it rests on, by another caster's funded casting.
        private static bool ReplacedByOtherCaster(ResolvedCasting casting,
            IReadOnlyDictionary<string, EffectExpression> effectsBySource, Dictionary<string, string> applied)
        {
            EffectExpression payload = DiscoveredEffectLookup.ForAbility(
                effectsBySource, casting.Ability, casting.SourceId);
            if (payload == null) return false;
            IEnumerable<string> recipients = casting.TargetMode == CastingTargetMode.DirectTarget
                ? new[] { casting.DirectTargetUnitId } : casting.PredictedBeneficiaryUnitIds;
            foreach (KeyValuePair<string, bool> role in EffectApplicationLifetime.CasterRelativeRoles(payload))
                foreach (string unit in role.Value ? new[] { casting.CasterUnitId } : recipients)
                {
                    string applier;
                    if (!string.IsNullOrEmpty(unit) && applied.TryGetValue(unit + "\u001f" + role.Key, out applier) &&
                        !string.Equals(applier, casting.CasterUnitId, StringComparison.Ordinal))
                        return true;
                }
            return false;
        }

        // Review R9 (R8-F1): whether a caster-relative buff (Firebrand) from
        // caster A stays effective on a unit after caster B applies the same
        // buff there is not established offline (replacement or coexistence
        // of same-blueprint instances from different casters is unverified),
        // so a funded application by another caster that may land on a unit
        // invalidates every other caster's grant of that effect there.
        private static void InvalidateOtherCasterGrants(EffectChanges changes, string ownKey,
            Dictionary<string, HashSet<string>> grantedByUnit,
            Dictionary<string, Dictionary<string, HashSet<string>>> grantEffects)
        {
            if (changes == null) return;
            foreach (KeyValuePair<string, string> application in changes.Applied)
            {
                Dictionary<string, HashSet<string>> grants;
                HashSet<string> granted;
                if (application.Key == null || !grantEffects.TryGetValue(application.Key, out grants) ||
                    !grantedByUnit.TryGetValue(application.Key, out granted))
                    continue;
                foreach (string key in grants.Where(pair => !string.Equals(pair.Key, ownKey, StringComparison.Ordinal) &&
                        pair.Key.Contains("|caster:") && pair.Value.Contains(application.Value))
                    .Select(pair => pair.Key).ToList())
                {
                    granted.Remove(key);
                    grants.Remove(key);
                }
            }
        }

        // Structural satisfaction is only claimed for identical ability
        // identity with no strength-affecting enhancements requested or
        // granted: anything else is an unknown equivalence that must keep
        // casting rather than inventing satisfaction or saving resources.
        private static bool IsStructurallySatisfied(
            ResolvedCasting casting,
            IReadOnlyList<CastEnhancementSnapshot> matchedEnhancements,
            Dictionary<string, HashSet<string>> grantedByUnit,
            bool casterRelative)
        {
            if (matchedEnhancements != null && matchedEnhancements.Count != 0)
                return false;
            if (casting.PredictedBeneficiaryUnitIds.Count == 0)
                return false;
            foreach (string unitId in casting.PredictedBeneficiaryUnitIds)
            {
                HashSet<string> granted;
                if (!grantedByUnit.TryGetValue(unitId, out granted) ||
                    !granted.Contains(StructuralGrantKey(casting, casterRelative)))
                    return false;
            }
            return true;
        }

        // An item rule is shared by every copy of the type, but one copy's
        // casting never makes another copy's casting (and charges) redundant.
        // Review R8-F1: neither does another caster's casting of a payload
        // whose benefit depends on its caster (Firebrand).
        private static string StructuralGrantKey(ResolvedCasting casting, bool casterRelative)
        {
            string key = casting.Ability.SourceKind == SourceKind.Item && casting.Provider != null
                ? casting.Provider.Canonical : casting.Ability.Canonical;
            return casterRelative ? key + "|caster:" + casting.CasterUnitId : key;
        }

        // Review R9-F1: every application of the payload is a permanent
        // native application (ContextActionApplyBuff.Permanent): a repeat has
        // no duration to refresh.
        private static bool HasOnlyPermanentApplications(ResolvedCasting casting,
            IReadOnlyDictionary<string, EffectExpression> effectsBySource)
        {
            EffectExpression payload = DiscoveredEffectLookup.ForAbility(
                effectsBySource, casting.Ability, casting.SourceId);
            List<EffectLeafExpression> leaves = payload == null ? new List<EffectLeafExpression>()
                : EffectApplicationLifetime.Leaves(payload).ToList();
            return leaves.Count != 0 && leaves.All(leaf => leaf.PermanentApplication);
        }

        private static bool HasCasterRelativePayload(ResolvedCasting casting,
            IReadOnlyDictionary<string, EffectExpression> effectsBySource)
        {
            EffectExpression payload = DiscoveredEffectLookup.ForAbility(
                effectsBySource, casting.Ability, casting.SourceId);
            return payload != null && EffectApplicationLifetime.Leaves(payload).Any(leaf => leaf.CasterRelative);
        }

        private static void ProjectEffects(
            ResolvedCasting casting,
            IReadOnlyList<CastEnhancementSnapshot> matchedEnhancements,
            Dictionary<string, HashSet<string>> grantedByUnit,
            EffectChanges changes,
            Dictionary<string, Dictionary<string, HashSet<string>>> grantEffects,
            bool casterRelative)
        {
            if (matchedEnhancements != null && matchedEnhancements.Count != 0)
                return;
            string key = StructuralGrantKey(casting, casterRelative);
            foreach (string unitId in casting.PredictedBeneficiaryUnitIds)
            {
                // Review R2-F1: a funded cast grants a free structural repeat
                // to a unit only when every effect it may apply there is
                // certain on every path (Good Hope's Despair branch removes
                // Despair and applies nothing; that is no grant).
                if (changes == null) continue;
                string unit = unitId;
                var possible = changes.Applied.Where(pair => pair.Key == unit).ToList();
                if (possible.Count == 0 || possible.Any(pair => !changes.Guaranteed.Contains(pair))) continue;
                HashSet<string> granted;
                if (!grantedByUnit.TryGetValue(unitId, out granted))
                {
                    granted = new HashSet<string>(StringComparer.Ordinal);
                    grantedByUnit[unitId] = granted;
                }
                granted.Add(key);
                // The effects this grant stands for on the unit (any owner
                // the casting reaches from it is the unit's own scope).
                Dictionary<string, HashSet<string>> grants;
                if (!grantEffects.TryGetValue(unitId, out grants))
                    grantEffects[unitId] = grants = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
                HashSet<string> effects;
                if (!grants.TryGetValue(key, out effects))
                    grants[key] = effects = new HashSet<string>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, string> applied in possible) effects.Add(applied.Value);
            }
        }

        private static ProviderPlanningOption ResolveOption(
            PlannedCasting casting,
            PartyProviderSnapshot snapshot,
            List<ProviderPlanningOption> options,
            IReadOnlyList<string> capableCasters,
            List<string> reasons)
        {
            if (casting.CasterUnitId == null)
            {
                reasons.Add("caster-unresolved");
                return null;
            }
            if (snapshot.Units.All(unit => unit.UnitId != casting.CasterUnitId))
            {
                reasons.Add("caster-not-in-party:" + casting.CasterUnitId);
                return null;
            }
            if (!capableCasters.Contains(casting.CasterUnitId))
            {
                // Never "cannot cast" when the real problem is a saved item
                // selection that no longer identifies a live source.
                reasons.Add(ItemSourceResolution.Refusal(casting.Ability, casting.CasterUnitId,
                    snapshot.Providers) ?? "caster-not-capable:" + casting.CasterUnitId);
                return null;
            }
            IEnumerable<ProviderPlanningOption> candidates = options.Where(option =>
                string.Equals(option.Provider.Key.Ability.Canonical,
                    casting.Ability.Canonical, StringComparison.Ordinal) &&
                string.Equals(option.Provider.Key.CasterUnitId,
                    casting.CasterUnitId, StringComparison.Ordinal));
            if (casting.SpellbookGuid != null)
            {
                var constrained = candidates.Where(option =>
                    string.Equals(option.Provider.Key.SpellbookGuid,
                        casting.SpellbookGuid, StringComparison.Ordinal)).ToList();
                if (constrained.Count == 0 && candidates.Any())
                {
                    reasons.Add("spellbook-constraint-unsatisfied:" + casting.SpellbookGuid);
                    return null;
                }
                candidates = constrained;
            }
            var ordered = candidates.OrderBy(
                option => option.Provider.Key.Canonical, StringComparer.Ordinal).ToList();
            if (ordered.Count == 0)
            {
                reasons.Add("provider-option-unavailable");
                return null;
            }
            if (ordered.Select(option => option.Provider.Key.Canonical).Distinct(
                    StringComparer.Ordinal).Count() > 1)
            {
                // The exact physical source was not pinned; picking one
                // silently would reintroduce first-available substitution.
                reasons.Add((casting.Ability.SourceKind == SourceKind.Item
                    ? ItemSourceResolution.Ambiguous : "exact-source-ambiguous") + ":" + ordered.Count);
                return null;
            }
            return ordered[0];
        }

        private static void VerifyAbilityTargetMode(
            PlannedCasting casting,
            IReadOnlyDictionary<string, EffectExpression> effectsBySource,
            List<string> reasons)
        {
            EffectExpression expression;
            if (!effectsBySource.TryGetValue(casting.SourceId, out expression) &&
                !effectsBySource.TryGetValue(casting.Ability.Canonical, out expression))
            {
                reasons.Add("source-unresolvable:" + casting.SourceId);
                return;
            }
            CastGroupingKind grouping;
            if (!EffectExpressionTargetAnalysis.TryGetGrouping(expression, out grouping))
            {
                reasons.Add("ability-targeting-unsupported:" + casting.SourceId);
                return;
            }
            bool authoredGroup = casting.TargetMode != CastingTargetMode.DirectTarget;
            bool abilityGroup = grouping == CastGroupingKind.MassConfiguredTargets;
            if (authoredGroup != abilityGroup)
                reasons.Add("target-mode-mismatch:" + casting.TargetMode +
                    ":ability-" + grouping);
        }

        private static bool HasSpendableResources(
            ProviderPlanningOption option, PartyProviderSnapshot snapshot, List<string> reasons)
        {
            ResourcePoolSnapshot pool = snapshot.ResourcePools.FirstOrDefault(
                value => string.Equals(value.PoolKey, option.Provider.ResourcePoolKey,
                    StringComparison.Ordinal));
            if (pool == null)
            {
                reasons.Add("resource-pool-unknown:" + option.Provider.ResourcePoolKey);
                return false;
            }
            if (pool.Kind == ResourcePoolKind.PreparedSlots)
            {
                var available = new HashSet<string>(
                    pool.Tokens.Where(token => token.Available)
                        .Select(token => token.TokenId), StringComparer.Ordinal);
                if (!option.Provider.EligibleTokenIds.Any(available.Contains))
                {
                    reasons.Add("prepared-slots-exhausted:" + pool.PoolKey);
                    return false;
                }
                return true;
            }
            if (pool.Remaining < option.Provider.UnitsPerCast)
            {
                reasons.Add("resource-pool-exhausted:" + pool.PoolKey +
                    ":" + pool.Remaining + "<" + option.Provider.UnitsPerCast);
                return false;
            }
            return true;
        }

        // Returns predicted beneficiaries, or null when the authored target
        // is illegal for this option.
        private static IReadOnlyList<string> ResolveTargeting(
            PlannedCasting casting,
            ProviderPlanningOption option,
            PartyProviderSnapshot snapshot,
            List<CoverageGap> gaps,
            List<string> reasons,
            bool deferEmptyGroupCoverage = false)
        {
            if (casting.TargetMode == CastingTargetMode.DirectTarget)
                return ResolveDirectTarget(casting, option, snapshot, reasons);
            return ResolveGroupOrigin(casting, option, gaps, reasons, deferEmptyGroupCoverage);
        }

        private static IReadOnlyList<string> ResolveDirectTarget(
            PlannedCasting casting,
            ProviderPlanningOption option,
            PartyProviderSnapshot snapshot,
            List<string> reasons)
        {
            string targetId = casting.DirectTargetUnitId;
            UnitSnapshot unit = snapshot.Units.FirstOrDefault(
                value => value.UnitId == targetId);
            if (unit == null)
            {
                reasons.Add("target-not-in-party:" + targetId);
                return null;
            }
            if (!option.ReachableTargetIds.Contains(targetId))
            {
                reasons.Add("target-unreachable:" + targetId);
                return null;
            }
            if (!unit.TargetValidation.Alive)
                reasons.Add("target-not-alive:" + targetId);
            if (!unit.TargetValidation.Conscious)
                reasons.Add("target-not-conscious:" + targetId);
            if (!unit.TargetValidation.Friendly)
                reasons.Add("target-not-friendly:" + targetId);
            if (!unit.TargetValidation.Targetable)
                reasons.Add("target-not-targetable:" + targetId);
            return reasons.Count != 0
                ? null
                : new ReadOnlyCollection<string>(new[] { targetId });
        }

        private static IReadOnlyList<string> ResolveGroupOrigin(
            PlannedCasting casting,
            ProviderPlanningOption option,
            List<CoverageGap> gaps,
            List<string> reasons,
            bool deferEmptyGroupCoverage = false)
        {
            string anchorId = casting.TargetMode == CastingTargetMode.CasterCenteredOrigin
                ? casting.CasterUnitId
                : casting.Origin.AnchorUnitId;
            if (casting.TargetMode == CastingTargetMode.AnchoredOrigin &&
                !option.LegalAnchorIds.Contains(anchorId))
            {
                reasons.Add("origin-anchor-illegal:" + anchorId);
                return null;
            }
            // Final review A2: a caster-centred casting is aimed at its
            // caster, so the caster must be a legal origin exactly as an
            // anchor must be; otherwise its prediction would fall back to
            // every reachable unit (an over-prediction) or to nobody, and the
            // cast would fail only at run time.
            if (casting.TargetMode == CastingTargetMode.CasterCenteredOrigin &&
                !option.LegalAnchorIds.Contains(anchorId))
            {
                reasons.Add("origin-caster-illegal:" + anchorId);
                return null;
            }
            // One invocation; beneficiaries are derived edges, never extra
            // castings. WP3 (0.4.0): the cast affects whoever the native
            // ability reaches from its centre; the prediction is read-only
            // information and no member is a required-coverage constraint.
            IReadOnlyList<string> predicted = option.CoveredTargetIdsForAnchor(anchorId);
            if (predicted == null || predicted.Count == 0)
            {
                // Final review A2: nobody is predicted to be reached, so there
                // is nothing to cast for and nothing a run could confirm.
                reasons.Add(PredictedCoverageEmpty + anchorId);
                // Review R7-F6: a one-pass sequence keeps the resolved option,
                // so its budget pass can re-evaluate the burst after earlier
                // routines (the casting stays refused otherwise).
                return deferEmptyGroupCoverage ? new ReadOnlyCollection<string>(new string[0]) : null;
            }
            return predicted;
        }

        private static void ResolveEnhancements(
            PlannedCasting casting,
            ProviderPlanningOption option,
            List<CastEnhancementSnapshot> enhancements,
            List<string> applied,
            List<string> omitted,
            List<CastEnhancementSnapshot> matched,
            List<CastEnhancementSnapshot> intended,
            List<CastEnhancementSnapshot> requiredExhausted,
            List<string> reasons,
            List<string> resourceReasons)
        {
            foreach (AuthoredEnhancementSelection selection in casting.Enhancements)
            {
                CastEnhancementSnapshot snapshot = enhancements.FirstOrDefault(value =>
                    string.Equals(value.EnhancementId, selection.EnhancementId,
                        StringComparison.Ordinal) &&
                    string.Equals(value.CasterUnitId, casting.CasterUnitId,
                        StringComparison.Ordinal));
                string failure = null;
                if (snapshot == null)
                    failure = "enhancement-unavailable:" + selection.EnhancementId;
                else if (snapshot.AffectsTargeting)
                    // An enhancement that changes whom the spell reaches
                    // (Share Transmutation) is not executed in this version:
                    // the casting's targeting never applies it, so it would
                    // arm the enhancement on the caster's own target
                    // (review of the enhanced recipe). It stays selected and
                    // visible, and blocks.
                    failure = "enhancement-changes-targeting:" + selection.EnhancementId;
                else
                {
                    string applicability = snapshot.ApplicabilityFailure(option.Provider);
                    if (applicability.Length != 0)
                        failure = "enhancement-not-applicable:" +
                            selection.EnhancementId + ":" + applicability;
                    else
                    {
                        intended.Add(snapshot);
                        if (snapshot.RemainingUses == 0)
                            failure = "enhancement-exhausted:" + selection.EnhancementId;
                    }
                }
                if (failure == null)
                {
                    matched.Add(snapshot);
                    applied.Add(selection.EnhancementId);
                    continue;
                }
                // Selected enhancements on new castings are requirements: no
                // silent removal, substitution, or weakening. Only explicit
                // legacy optional intent may be omitted, disclosed as such.
                // An exhausted REQUIRED enhancement is a resource shortage like
                // a spent slot: it blocks a casting that must cast, but a
                // sufficient active effect (typically from the run that
                // spent it) waives it.
                if (selection.Required && snapshot != null &&
                    failure.StartsWith("enhancement-exhausted:", StringComparison.Ordinal))
                {
                    resourceReasons.Add(failure);
                    requiredExhausted.Add(snapshot);
                }
                else if (selection.Required) reasons.Add(failure);
                else omitted.Add(selection.EnhancementId + ":" + failure);
            }
            // Review P3-3: an exhausted required enhancement is still part
            // of the selection; an incompatible set stays a structural block
            // that no active effect waives.
            List<CastEnhancementSnapshot> selected = matched.Concat(requiredExhausted).ToList();
            if (selected.Count != 0 && !CastEnhancementSnapshot.AreCompatible(selected))
                reasons.Add("enhancement-incompatible");
        }

        // Live existing-effect assessment for one casting. Every INTENDED
        // recipient is judged against exact effect identities: the direct
        // target, or the group's required coverage (the predicted
        // beneficiaries when no coverage is required). Returns true only
        // when the policy skips and every intended recipient already has a
        // sufficient instance of the complete effect; notes disclose each
        // recipient's verdict either way.
        private static bool AssessExistingEffects(
            PlannedCasting casting,
            ProviderPlanningOption option,
            IReadOnlyList<CastEnhancementSnapshot> matched,
            IReadOnlyList<string> predicted,
            IReadOnlyDictionary<string, EffectExpression> effectsBySource,
            ActiveEffectSnapshot liveEffects,
            List<string> notes,
            List<string> satisfiedUnitIds,
            List<string> preCoveredUnitIds)
        {
            EffectExpression expression = EffectConditionBinding.Bind(DiscoveredEffectLookup.ForAbility(
                effectsBySource, casting.Ability, casting.SourceId), option.CasterFacts);
            if (expression == null) return false;
            IReadOnlyList<string> recipients =
                casting.TargetMode == CastingTargetMode.DirectTarget
                    ? new ReadOnlyCollection<string>(new[] { casting.DirectTargetUnitId })
                    : predicted;
            if (recipients == null || recipients.Count == 0 ||
                recipients.Any(string.IsNullOrEmpty))
                return false;
            ExistingEffectRequirement requirement = RequirementFor(option.Provider, matched);
            var ignored = new HashSet<string>(
                casting.IgnoredPresenceMarkers ?? new string[0], StringComparer.Ordinal);
            bool skip = casting.ExistingEffectPolicy == ExistingEffectPolicy.SkipAlreadyActive;
            bool allSufficient = true;
            foreach (string unitId in recipients)
            {
                // Review R7-F4: the recipient's own branch (its fixed facts,
                // the caster relation included): an enemy-only Prayer penalty
                // never satisfies a friendly recipient's Prayer.
                ExistingEffectRecipientAssessment assessment = ExistingEffectSufficiency.AssessScoped(
                    casting.CasterUnitId, unitId, option.BindForRecipient(expression, unitId), owner => InstancesFor(liveEffects, owner), ignored, requirement);
                string detail = assessment.Reasons.Count == 0 ? string.Empty
                    : ":" + string.Join("|", assessment.Reasons.ToArray());
                switch (assessment.Verdict)
                {
                    case ExistingEffectVerdict.Sufficient:
                        satisfiedUnitIds.Add(unitId);
                        notes.Add((skip ? "already-active:" : "existing-active-recast:") +
                            unitId + detail);
                        break;
                    case ExistingEffectVerdict.Insufficient:
                        allSufficient = false;
                        notes.Add("existing-insufficient:" + unitId + detail);
                        break;
                    default:
                        allSufficient = false;
                        if (detail.Length != 0)
                            notes.Add("existing-incomplete:" + unitId + detail);
                        break;
                }
            }
            // Mixed coverage (owner, 2026-09-24): a group casting that still
            // casts under skip-if-active also reaches recipients whose
            // existing effect is already sufficient. They are named for the
            // step (every predicted recipient is assessed, required coverage
            // or not), and their note says the cast goes ahead for the
            // others - never "skipped" - and, when their effect outlasts what
            // this casting gives, that the game may replace it with the
            // shorter one (seen live: a communal form replaced a longer
            // direct instance).
            if (skip && !allSufficient && casting.TargetMode != CastingTargetMode.DirectTarget &&
                predicted != null)
            {
                for (int index = 0; index < notes.Count; index++)
                    if (notes[index].StartsWith("already-active:", StringComparison.Ordinal))
                    {
                        string rest = notes[index].Substring("already-active:".Length);
                        int split = rest.IndexOf(':');
                        string unit = split < 0 ? rest : rest.Substring(0, split);
                        notes[index] = CoveredNotePrefix(InstancesFor(liveEffects, unit),
                            option.BindForRecipient(expression, unit), requirement) + rest;
                    }
                foreach (string unitId in predicted)
                {
                    if (string.IsNullOrEmpty(unitId) || preCoveredUnitIds.Contains(unitId)) continue;
                    if (satisfiedUnitIds.Contains(unitId))
                    {
                        preCoveredUnitIds.Add(unitId);
                        continue;
                    }
                    if (recipients.Contains(unitId)) continue;
                    ExistingEffectRecipientAssessment extra = ExistingEffectSufficiency.AssessScoped(
                        casting.CasterUnitId, unitId, option.BindForRecipient(expression, unitId), owner => InstancesFor(liveEffects, owner), ignored, requirement);
                    if (extra.Verdict != ExistingEffectVerdict.Sufficient) continue;
                    preCoveredUnitIds.Add(unitId);
                    notes.Add(CoveredNotePrefix(InstancesFor(liveEffects, unitId),
                        option.BindForRecipient(expression, unitId), requirement) +
                        unitId + (extra.Reasons.Count == 0 ? string.Empty
                            : ":" + string.Join("|", extra.Reasons.ToArray())));
                }
            }
            return skip && allSufficient;
        }

        // "already-covered-longer:" when a live, unsuppressed instance of the
        // effect outlasts what the planned casting gives (compared only when
        // that duration is a trustworthy estimate; a permanent instance
        // outlasts any), else "already-covered:".
        private static string CoveredNotePrefix(IEnumerable<ActiveEffectInstance> instances,
            EffectExpression expression, ExistingEffectRequirement requirement)
        {
            if (!requirement.DurationComparable || requirement.PlannedExpectedRounds <= 0)
                return "already-covered:";
            double planned = requirement.FinalExpectedRounds;
            var effects = new HashSet<string>(LeafEffectIds(expression), StringComparer.Ordinal);
            bool outlasts = (instances ?? new ActiveEffectInstance[0]).Any(value => value != null &&
                !value.Suppressed && effects.Contains(value.EffectId) &&
                (value.RemainingRounds == null || value.RemainingRounds.Value > planned));
            return outlasts ? "already-covered-longer:" : "already-covered:";
        }

        private static IEnumerable<string> LeafEffectIds(EffectExpression expression)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) { yield return leaf.EffectId; yield break; }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                foreach (EffectExpression child in sequence.Children)
                    foreach (string id in LeafEffectIds(child)) yield return id;
                yield break;
            }
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                foreach (string id in LeafEffectIds(conditional.WhenTrue)) yield return id;
                foreach (string id in LeafEffectIds(conditional.WhenFalse)) yield return id;
                yield break;
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null)
            {
                foreach (string id in LeafEffectIds(targeted.Child)) yield return id;
                yield break;
            }
            var referenced = expression as ReferencedAbilityExpression;
            if (referenced != null)
                foreach (string id in LeafEffectIds(referenced.Child)) yield return id;
        }

        private static IEnumerable<ActiveEffectInstance> InstancesFor(
            ActiveEffectSnapshot liveEffects, string unitId)
        {
            if (liveEffects.HasInstanceDetail) return liveEffects.GetInstances(unitId);
            // Marker-only snapshot: presence without detail (legacy parity);
            // unknown caster level and metamagic stay unknown.
            return liveEffects.GetEffects(unitId).Select(marker =>
                new ActiveEffectInstance(marker.Kind, marker.EffectId, null, null, null));
        }

        private static ExistingEffectRequirement RequirementFor(
            ProviderSnapshot provider, IReadOnlyList<CastEnhancementSnapshot> matched)
        {
            int mask = provider.Key.Ability.MetamagicMask;
            string unprovable = null;
            foreach (CastEnhancementSnapshot enhancement in
                matched ?? new CastEnhancementSnapshot[0])
            {
                if (enhancement == null) continue;
                if (enhancement.Category == CastEnhancementCategory.MetamagicRod)
                    mask |= enhancement.MetamagicMask;
                else if (!enhancement.AffectsTargeting && unprovable == null)
                    unprovable = "class-feature:" + enhancement.EnhancementId;
            }
            return new ExistingEffectRequirement(provider.EffectiveCasterLevel, mask,
                provider.ExpectedDurationRounds,
                provider.Duration.Kind == DurationKind.PerCasterLevel,
                unprovable, provider.Duration.Extendable, provider.Key.CasterUnitId);
        }

        private static IReadOnlyList<string> CostShapeOf(IEnumerable<CastingDemand> demands)
        {
            return new ReadOnlyCollection<string>(demands
                .Select(demand => demand.Category + ":" + demand.PoolKey + ":" + demand.Units)
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
        }

        private static void AddDuplicateRequestWarnings(
            List<ResolvedCasting> castings, List<string> diagnostics)
        {
            for (int i = 0; i < castings.Count; i++)
            {
                if (!castings[i].IsExecutable) continue;
                for (int j = i + 1; j < castings.Count; j++)
                {
                    if (!castings[j].IsExecutable) continue;
                    if (!string.Equals(castings[i].Ability.Canonical,
                            castings[j].Ability.Canonical, StringComparison.Ordinal))
                        continue;
                    string overlap = castings[i].PredictedBeneficiaryUnitIds
                        .FirstOrDefault(unitId =>
                            castings[j].PredictedBeneficiaryUnitIds.Contains(unitId));
                    if (overlap != null)
                        diagnostics.Add("duplicate-effect-request:" +
                            castings[i].CastingId + ":" + castings[j].CastingId +
                            ":" + overlap);
                }
            }
        }
    }
}
