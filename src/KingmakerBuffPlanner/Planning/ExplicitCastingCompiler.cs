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
            IReadOnlyList<string> preCoveredUnitIds = null)
        {
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
            IReadOnlyList<string> costShape = null)
        {
            return new ResolvedCasting(
                CastingId, RoutineId, Order, SourceId, Ability, CasterUnitId,
                Provider, TargetMode, DirectTargetUnitId, Origin,
                RequiredCoverageUnitIds, PredictedBeneficiaryUnitIds, CoverageGaps,
                TargetingModifiers, Enhancements, AppliedEnhancementIds,
                OmittedEnhancementIds, ExistingEffectPolicy, IgnoredPresenceMarkers,
                readiness, readinessReasons, CapableCasterUnitIds, Provenance, cost,
                ExistingEffectNotes, costShape ?? CostShape, PreCoveredUnitIds);
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
            IEnumerable<string> pendingImportNotices = null)
        {
            Castings = new ReadOnlyCollection<ResolvedCasting>(castings.ToList());
            PendingImportNotices = new ReadOnlyCollection<string>(
                (pendingImportNotices ?? new string[0]).ToList());
            Diagnostics = new ReadOnlyCollection<string>(diagnostics.ToList());
            BudgetLines = new ReadOnlyCollection<CastingBudgetLine>(
                (budgetLines ?? new CastingBudgetLine[0]).ToList());
        }

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
            ActiveEffectSnapshot liveEffects = null)
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
            foreach (PlannedCasting casting in document.Castings)
            {
                List<CastEnhancementSnapshot> matched;
                List<CastEnhancementSnapshot> intended;
                ProviderSnapshot provider;
                List<ModifierUsageDemand> modifierDemands;
                castings.Add(CompileOne(casting, snapshot, options, effectsBySource,
                    enhancementList, modifierList, diagnostics, liveEffects, out matched,
                    out intended, out provider, out modifierDemands));
                matchedEnhancements[casting.CastingId] = matched;
                intendedEnhancements[casting.CastingId] = intended;
                providers[casting.CastingId] = provider;
                modifierDemandsByCasting[casting.CastingId] = modifierDemands;
            }
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
            var finalized = new List<ResolvedCasting>();
            foreach (ResolvedCasting casting in castings)
            {
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
                if (casting.Readiness != ResolvedCastingReadiness.Ready ||
                    (budgetRoutineScope != null &&
                     casting.RoutineId != budgetRoutineScope))
                {
                    finalized.Add(casting.WithBudgetResult(casting.Readiness,
                        casting.ReadinessReasons, casting.Cost, shape));
                    continue;
                }
                if (projectEffects &&
                    casting.ExistingEffectPolicy == ExistingEffectPolicy.SkipAlreadyActive &&
                    IsStructurallySatisfied(casting, matched, grantedByUnit))
                {
                    finalized.Add(casting.WithBudgetResult(
                        ResolvedCastingReadiness.AlreadySatisfied,
                        new[] { "already-satisfied-structural" },
                        new CastingCostLine[0], shape));
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
                    if (projectEffects)
                        ProjectEffects(casting, matched, grantedByUnit);
                    continue;
                }
                ledger.RecordUnfunded(casting.CastingId, demands);
                var reasons = casting.ReadinessReasons
                    .Concat(new[] { reason })
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal).ToList();
                finalized.Add(casting.WithBudgetResult(
                    ResolvedCastingReadiness.Blocked, reasons, new CastingCostLine[0], shape));
            }
            AddDuplicateRequestWarnings(finalized, diagnostics);
            return new ExplicitCastingPlan(finalized, diagnostics, ledger.BuildReport(),
                document.PendingImportNotices);
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
            out List<CastEnhancementSnapshot> matchedEnhancements,
            out List<CastEnhancementSnapshot> intendedEnhancements,
            out ProviderSnapshot providerSnapshot,
            out List<ModifierUsageDemand> modifierDemands)
        {
            var reasons = new List<string>();
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
            if (option != null)
                option = ApplyTargetingModifiers(
                    casting, targetingModifiers, option, reasons, modifierDemands);
            // Resources are judged AFTER targeting, enhancements and the live
            // existing-effect assessment: a casting whose effect is already
            // sufficiently active needs no resources, so an exhausted pool
            // must not block it (repeat use right after a routine ran).
            var resourceReasons = new List<string>();
            if (option != null)
                HasSpendableResources(option, snapshot, resourceReasons);
            if (option != null)
            {
                predicted = ResolveTargeting(casting, option, snapshot, gaps, reasons);
                if (predicted == null)
                {
                    predicted = new string[0];
                    option = null;
                }
            }
            else if (casting.TargetMode != CastingTargetMode.DirectTarget &&
                casting.RequiredCoverageUnitIds.Count != 0)
            {
                // Without a resolved option there is no predicted coverage;
                // every intended recipient stays visible as uncovered intent.
                gaps.AddRange(casting.RequiredCoverageUnitIds.Select(
                    unitId => new CoverageGap(unitId, "coverage-unresolved")));
            }
            var applied = new List<string>();
            var omitted = new List<string>();
            var matched = new List<CastEnhancementSnapshot>();
            var intended = new List<CastEnhancementSnapshot>();
            var requiredExhausted = new List<CastEnhancementSnapshot>();
            if (option != null)
                ResolveEnhancements(casting, option, enhancements, applied, omitted,
                    matched, intended, requiredExhausted, reasons, resourceReasons);
            else
                foreach (AuthoredEnhancementSelection selection in casting.Enhancements)
                    if (selection.Required)
                        reasons.Add("enhancements-unvalidated:source-unresolved");
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
                AssessExistingEffects(casting, option.Provider,
                    matched.Concat(requiredExhausted).ToList(), predicted,
                    effectsBySource, liveEffects, existingNotes, satisfiedUnits, preCovered);
            if (alreadyActive)
                reasons.Add("already-active:" + string.Join(",", satisfiedUnits.ToArray()));
            else
                reasons.AddRange(resourceReasons);
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
                capableCasters, casting.Provenance, null, existingNotes, null, preCovered);
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
            List<ModifierUsageDemand> modifierDemands)
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
            }
            // Every applied modifier's verified cost enters the same atomic
            // cost vector; an unresolved modifier never reaches here, so an
            // unknown cost is never treated as free.
            foreach (ICastingTargetingModifier modifier in applied)
                modifierDemands.AddRange(modifier.UsageDemands(casting, option));
            return option;
        }

        // Structural satisfaction is only claimed for identical ability
        // identity with no strength-affecting enhancements requested or
        // granted: anything else is an unknown equivalence that must keep
        // casting rather than inventing satisfaction or saving resources.
        private static bool IsStructurallySatisfied(
            ResolvedCasting casting,
            IReadOnlyList<CastEnhancementSnapshot> matchedEnhancements,
            Dictionary<string, HashSet<string>> grantedByUnit)
        {
            if (matchedEnhancements != null && matchedEnhancements.Count != 0)
                return false;
            if (casting.PredictedBeneficiaryUnitIds.Count == 0)
                return false;
            foreach (string unitId in casting.PredictedBeneficiaryUnitIds)
            {
                HashSet<string> granted;
                if (!grantedByUnit.TryGetValue(unitId, out granted) ||
                    !granted.Contains(casting.Ability.Canonical))
                    return false;
            }
            return true;
        }

        private static void ProjectEffects(
            ResolvedCasting casting,
            IReadOnlyList<CastEnhancementSnapshot> matchedEnhancements,
            Dictionary<string, HashSet<string>> grantedByUnit)
        {
            if (matchedEnhancements != null && matchedEnhancements.Count != 0)
                return;
            foreach (string unitId in casting.PredictedBeneficiaryUnitIds)
            {
                HashSet<string> granted;
                if (!grantedByUnit.TryGetValue(unitId, out granted))
                {
                    granted = new HashSet<string>(StringComparer.Ordinal);
                    grantedByUnit[unitId] = granted;
                }
                granted.Add(casting.Ability.Canonical);
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
                reasons.Add("caster-not-capable:" + casting.CasterUnitId);
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
                reasons.Add("exact-source-ambiguous:" + ordered.Count);
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
            List<string> reasons)
        {
            if (casting.TargetMode == CastingTargetMode.DirectTarget)
                return ResolveDirectTarget(casting, option, snapshot, reasons);
            return ResolveGroupOrigin(casting, option, gaps, reasons);
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
            List<string> reasons)
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
            // castings, and uncovered intent stays visible as gaps.
            IReadOnlyList<string> predicted = option.CoveredTargetIdsForAnchor(anchorId);
            if (predicted == null || predicted.Count == 0)
            {
                // Final review A2: nobody is predicted to be reached, so there
                // is nothing to cast for and nothing a run could confirm.
                reasons.Add("predicted-coverage-empty:" + anchorId);
                return null;
            }
            var covered = new HashSet<string>(predicted, StringComparer.Ordinal);
            gaps.AddRange(casting.RequiredCoverageUnitIds
                .Where(unitId => !covered.Contains(unitId))
                .Select(unitId => new CoverageGap(unitId, "outside-predicted-coverage")));
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
            ProviderSnapshot provider,
            IReadOnlyList<CastEnhancementSnapshot> matched,
            IReadOnlyList<string> predicted,
            IReadOnlyDictionary<string, EffectExpression> effectsBySource,
            ActiveEffectSnapshot liveEffects,
            List<string> notes,
            List<string> satisfiedUnitIds,
            List<string> preCoveredUnitIds)
        {
            EffectExpression expression;
            if (!effectsBySource.TryGetValue(casting.SourceId, out expression) &&
                !effectsBySource.TryGetValue(casting.Ability.Canonical, out expression))
                return false;
            IReadOnlyList<string> recipients =
                casting.TargetMode == CastingTargetMode.DirectTarget
                    ? new ReadOnlyCollection<string>(new[] { casting.DirectTargetUnitId })
                    : casting.RequiredCoverageUnitIds.Count != 0
                        ? casting.RequiredCoverageUnitIds
                        : predicted;
            if (recipients == null || recipients.Count == 0 ||
                recipients.Any(string.IsNullOrEmpty))
                return false;
            ExistingEffectRequirement requirement = RequirementFor(provider, matched);
            var ignored = new HashSet<string>(
                casting.IgnoredPresenceMarkers ?? new string[0], StringComparer.Ordinal);
            bool skip = casting.ExistingEffectPolicy == ExistingEffectPolicy.SkipAlreadyActive;
            bool allSufficient = true;
            foreach (string unitId in recipients)
            {
                ExistingEffectRecipientAssessment assessment = ExistingEffectSufficiency.Assess(
                    unitId, expression, InstancesFor(liveEffects, unitId), ignored, requirement);
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
                        notes[index] = CoveredNotePrefix(InstancesFor(liveEffects, unit), expression,
                            requirement) + rest;
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
                    ExistingEffectRecipientAssessment extra = ExistingEffectSufficiency.Assess(
                        unitId, expression, InstancesFor(liveEffects, unitId), ignored, requirement);
                    if (extra.Verdict != ExistingEffectVerdict.Sufficient) continue;
                    preCoveredUnitIds.Add(unitId);
                    notes.Add(CoveredNotePrefix(InstancesFor(liveEffects, unitId), expression, requirement) +
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
            double planned = requirement.PlannedExpectedRounds *
                ((requirement.PlannedMetamagicMask & ExistingEffectSufficiency.ExtendMetamagicFlag) != 0 ? 2.0 : 1.0);
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
                ExistingEffectSufficiency.IsPerLevelDuration(provider.DurationText),
                unprovable);
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
