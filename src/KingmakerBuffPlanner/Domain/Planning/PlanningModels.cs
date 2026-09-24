using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.Domain.Planning
{
    public enum CastGroupingKind
    {
        PerTarget,
        MassConfiguredTargets
    }

    public enum ExistingEffectPolicy
    {
        SkipAlreadyActive,
        Overwrite
    }

    public enum CastExecutionStrategy
    {
        DirectRuleCast,
        StickyTouchDeliveryRuleCast,
        AnimatedFallback,
        NativeCommandRequired,
        ProviderDirectRuleCast
    }

    public sealed class CastExecutionCapability
    {
        public CastExecutionCapability(CastExecutionStrategy strategy, string reason)
        {
            Strategy = strategy;
            Reason = reason ?? string.Empty;
        }

        public CastExecutionStrategy Strategy { get; private set; }
        public string Reason { get; private set; }
    }

    public static class StickyTouchExecutionClassifier
    {
        public static CastExecutionCapability Classify(
            bool isStickyTouch,
            bool hasDeliveryBlueprint,
            bool hasTouchDeliveryComponent,
            bool deliveryTargetsUnit,
            bool deliveryCanTargetSelf,
            bool deliveryCanTargetFriends,
            bool deliveryCanTargetEnemies,
            bool deliveryCanTargetPoint)
        {
            if (!isStickyTouch)
                return new CastExecutionCapability(
                    CastExecutionStrategy.DirectRuleCast,
                    "ordinary-direct-rule-cast");
            if (!hasDeliveryBlueprint)
                return AnimatedFallback("sticky-delivery-blueprint-missing");
            if (!hasTouchDeliveryComponent)
                return AnimatedFallback("sticky-delivery-touch-component-missing");
            if (!deliveryTargetsUnit)
                return AnimatedFallback("sticky-delivery-target-anchor-unsupported");
            if (deliveryCanTargetPoint)
                return AnimatedFallback("sticky-delivery-point-targeting-ambiguous");
            if (deliveryCanTargetEnemies)
                return AnimatedFallback("sticky-delivery-hostile-targeting-ambiguous");
            if (!deliveryCanTargetSelf && !deliveryCanTargetFriends)
                return AnimatedFallback("sticky-delivery-has-no-beneficial-unit-target");
            return new CastExecutionCapability(
                CastExecutionStrategy.StickyTouchDeliveryRuleCast,
                "supported-beneficial-sticky-touch-delivery");
        }

        private static CastExecutionCapability AnimatedFallback(string reason)
        {
            return new CastExecutionCapability(
                CastExecutionStrategy.AnimatedFallback, reason);
        }
    }

    public enum TargetOutcomeKind
    {
        Fulfilled,
        SkippedAlreadyActive,
        Unfulfilled
    }

    public sealed class BuffSourceDefinition
    {
        public BuffSourceDefinition(
            string sourceId,
            AbilityKey ability,
            EffectExpression effects,
            CastGroupingKind grouping)
            : this(sourceId, new[] { ability }, effects, grouping)
        {
        }

        public BuffSourceDefinition(
            string sourceId,
            IEnumerable<AbilityKey> abilities,
            EffectExpression effects,
            CastGroupingKind grouping)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Source ID is required.", "sourceId");
            SourceId = sourceId;
            var values = (abilities ?? throw new ArgumentNullException("abilities"))
                .Where(item => item != null).GroupBy(item => item.Canonical, StringComparer.Ordinal)
                .Select(group => group.First()).OrderBy(item => item.Canonical, StringComparer.Ordinal).ToList();
            if (values.Count == 0) throw new ArgumentException("At least one ability is required.", "abilities");
            Abilities = new ReadOnlyCollection<AbilityKey>(values);
            Effects = effects ?? throw new ArgumentNullException("effects");
            Grouping = grouping;
        }

        public string SourceId { get; private set; }
        public AbilityKey Ability { get { return Abilities[0]; } }
        public IReadOnlyList<AbilityKey> Abilities { get; private set; }
        public EffectExpression Effects { get; private set; }
        public CastGroupingKind Grouping { get; private set; }
    }

    public sealed class EnhancementRequest
    {
        public EnhancementRequest(string enhancementId, bool required)
        {
            if (string.IsNullOrWhiteSpace(enhancementId))
                throw new ArgumentException("Enhancement ID is required.", "enhancementId");
            EnhancementId = enhancementId;
            Required = required;
        }

        public string EnhancementId { get; private set; }
        public bool Required { get; private set; }
    }

    public sealed class BuffCastRequest
    {
        public BuffCastRequest(
            BuffSourceDefinition source,
            IEnumerable<string> targetUnitIds,
            ExistingEffectPolicy existingEffectPolicy,
            IEnumerable<string> ignoredEffectIds,
            IEnumerable<string> enhancementIds = null)
            : this(source, targetUnitIds, existingEffectPolicy, ignoredEffectIds,
                (enhancementIds ?? new string[0]).Select(id => new EnhancementRequest(id, true)),
                null, null, null, null, 0)
        {
        }

        public BuffCastRequest(
            BuffSourceDefinition source,
            IEnumerable<string> targetUnitIds,
            ExistingEffectPolicy existingEffectPolicy,
            IEnumerable<string> ignoredEffectIds,
            IEnumerable<EnhancementRequest> enhancements,
            string assignmentId,
            string casterUnitId,
            string providerKeyConstraint,
            string spellbookGuid,
            int order)
        {
            Source = source ?? throw new ArgumentNullException("source");
            // Target order is explicit configured order: it controls which
            // recipients are allocated first and must survive round trips.
            TargetUnitIds = new ReadOnlyCollection<string>((targetUnitIds ?? throw new ArgumentNullException("targetUnitIds"))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.Ordinal).ToList());
            ExistingEffectPolicy = existingEffectPolicy;
            IgnoredEffectIds = new ReadOnlyCollection<string>((ignoredEffectIds ?? new string[0])
                .Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.Ordinal)
                .OrderBy(v => v, StringComparer.Ordinal).ToList());
            EnhancementSelections = new ReadOnlyCollection<EnhancementRequest>(
                (enhancements ?? new EnhancementRequest[0])
                    .Where(value => value != null)
                    .GroupBy(value => value.EnhancementId, StringComparer.Ordinal)
                    .Select(group => group.OrderBy(value => value.Required ? 0 : 1).First())
                    .ToList());
            AssignmentId = string.IsNullOrWhiteSpace(assignmentId) ? source.SourceId : assignmentId;
            // A pin is a hard eligibility constraint, never a preference.
            CasterUnitId = string.IsNullOrWhiteSpace(casterUnitId) ? null : casterUnitId;
            ProviderKeyConstraint = string.IsNullOrWhiteSpace(providerKeyConstraint)
                ? null : providerKeyConstraint;
            SpellbookGuid = string.IsNullOrWhiteSpace(spellbookGuid) ? null : spellbookGuid;
            Order = order;
        }

        public BuffSourceDefinition Source { get; private set; }
        public IReadOnlyList<string> TargetUnitIds { get; private set; }
        public ExistingEffectPolicy ExistingEffectPolicy { get; private set; }
        public IReadOnlyList<string> IgnoredEffectIds { get; private set; }
        public IReadOnlyList<EnhancementRequest> EnhancementSelections { get; private set; }
        public string AssignmentId { get; private set; }
        public string CasterUnitId { get; private set; }
        public string ProviderKeyConstraint { get; private set; }
        public string SpellbookGuid { get; private set; }
        public int Order { get; private set; }
        public IReadOnlyList<string> EnhancementIds
        {
            get
            {
                return EnhancementSelections
                    .Select(value => value.EnhancementId)
                    .OrderBy(v => v, StringComparer.Ordinal).ToList();
            }
        }
    }

    public sealed class ProviderPlanningOption
    {
        public ProviderPlanningOption(
            ProviderSnapshot provider,
            IEnumerable<string> reachableTargetIds,
            IEnumerable<string> legalAnchorIds,
            int effectiveCasterLevel,
            int expectedDurationRounds,
            bool requiresAnimatedExecution = false,
            IDictionary<string, IEnumerable<string>> recipientIdsByAnchor = null)
            : this(provider, reachableTargetIds, legalAnchorIds,
                effectiveCasterLevel, expectedDurationRounds,
                requiresAnimatedExecution
                    ? CastExecutionStrategy.AnimatedFallback
                    : CastExecutionStrategy.DirectRuleCast,
                requiresAnimatedExecution
                    ? "legacy-animated-fallback"
                    : "ordinary-direct-rule-cast",
                recipientIdsByAnchor)
        {
        }

        public ProviderPlanningOption(
            ProviderSnapshot provider,
            IEnumerable<string> reachableTargetIds,
            IEnumerable<string> legalAnchorIds,
            int effectiveCasterLevel,
            int expectedDurationRounds,
            CastExecutionStrategy executionStrategy,
            string executionStrategyReason,
            IDictionary<string, IEnumerable<string>> recipientIdsByAnchor = null)
        {
            Provider = provider ?? throw new ArgumentNullException("provider");
            if (effectiveCasterLevel < 0) throw new ArgumentOutOfRangeException("effectiveCasterLevel");
            if (expectedDurationRounds < 0) throw new ArgumentOutOfRangeException("expectedDurationRounds");
            ReachableTargetIds = Sorted(reachableTargetIds);
            LegalAnchorIds = Sorted(legalAnchorIds);
            var reachable = new HashSet<string>(ReachableTargetIds, StringComparer.Ordinal);
            var coverage = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, IEnumerable<string>> pair in recipientIdsByAnchor ??
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(pair.Key) ||
                    !LegalAnchorIds.Contains(pair.Key))
                    throw new ArgumentException(
                        "Anchor coverage references an unknown legal anchor.",
                        "recipientIdsByAnchor");
                IReadOnlyList<string> recipients = Sorted(pair.Value);
                if (recipients.Any(id => !reachable.Contains(id)))
                    throw new ArgumentException(
                        "Anchor coverage references an unreachable target.",
                        "recipientIdsByAnchor");
                coverage.Add(pair.Key, recipients);
            }
            RecipientIdsByAnchor = new ReadOnlyDictionary<string, IReadOnlyList<string>>(
                coverage);
            EffectiveCasterLevel = effectiveCasterLevel;
            ExpectedDurationRounds = expectedDurationRounds;
            ExecutionStrategy = executionStrategy;
            ExecutionStrategyReason = executionStrategyReason ?? string.Empty;
        }

        public ProviderSnapshot Provider { get; private set; }
        public IReadOnlyList<string> ReachableTargetIds { get; private set; }
        public IReadOnlyList<string> LegalAnchorIds { get; private set; }
        public IReadOnlyDictionary<string, IReadOnlyList<string>> RecipientIdsByAnchor
        {
            get; private set;
        }
        public int EffectiveCasterLevel { get; private set; }
        public int ExpectedDurationRounds { get; private set; }
        public CastExecutionStrategy ExecutionStrategy { get; private set; }
        public string ExecutionStrategyReason { get; private set; }
        public bool RequiresAnimatedExecution
        {
            get
            {
                return ExecutionStrategy == CastExecutionStrategy.AnimatedFallback ||
                    ExecutionStrategy == CastExecutionStrategy.NativeCommandRequired;
            }
        }

        public IReadOnlyList<string> CoveredTargetIdsForAnchor(string anchorUnitId)
        {
            IReadOnlyList<string> covered;
            return !string.IsNullOrWhiteSpace(anchorUnitId) &&
                RecipientIdsByAnchor.TryGetValue(anchorUnitId, out covered)
                ? covered : ReachableTargetIds;
        }

        private static IReadOnlyList<string> Sorted(IEnumerable<string> values)
        {
            return new ReadOnlyCollection<string>((values ?? new string[0])
                .Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.Ordinal)
                .OrderBy(v => v, StringComparer.Ordinal).ToList());
        }
    }

    public sealed class ProviderSelectionPolicy
    {
        public ProviderSelectionPolicy(
            IEnumerable<string> bannedProviderKeys,
            IDictionary<string, int> explicitPriorities,
            IDictionary<string, int> maximumCasts)
        {
            BannedProviderKeys = new HashSet<string>(bannedProviderKeys ?? new string[0], StringComparer.Ordinal);
            ExplicitPriorities = new ReadOnlyDictionary<string, int>(
                new Dictionary<string, int>(explicitPriorities ?? new Dictionary<string, int>(), StringComparer.Ordinal));
            MaximumCasts = new ReadOnlyDictionary<string, int>(
                new Dictionary<string, int>(maximumCasts ?? new Dictionary<string, int>(), StringComparer.Ordinal));
            if (ExplicitPriorities.Values.Any(v => v < 0)) throw new ArgumentOutOfRangeException("explicitPriorities");
            if (MaximumCasts.Values.Any(v => v < 0)) throw new ArgumentOutOfRangeException("maximumCasts");
        }

        public ISet<string> BannedProviderKeys { get; private set; }
        public IReadOnlyDictionary<string, int> ExplicitPriorities { get; private set; }
        public IReadOnlyDictionary<string, int> MaximumCasts { get; private set; }
    }

    // One live effect instance on a unit, with the details the casting-first
    // existing-effect policy needs to tell an equal-or-stronger effect from
    // a weaker, suppressed or nearly expired one. Unknown caster level or
    // metamagic stays null (never zero); a null remaining duration means the
    // instance has no expiry (permanent, or worn while equipped).
    public sealed class ActiveEffectInstance
    {
        public ActiveEffectInstance(EffectKind kind, string effectId,
            double? remainingRounds, int? casterLevel, int? metamagicMask,
            bool suppressed = false, bool suppressionReadable = true)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                throw new ArgumentException("Effect ID is required.", "effectId");
            if (remainingRounds != null &&
                (double.IsNaN(remainingRounds.Value) || remainingRounds.Value < 0))
                throw new ArgumentOutOfRangeException("remainingRounds");
            if (casterLevel != null && casterLevel.Value < 0)
                throw new ArgumentOutOfRangeException("casterLevel");
            if (metamagicMask != null && metamagicMask.Value < 0)
                throw new ArgumentOutOfRangeException("metamagicMask");
            Kind = kind;
            EffectId = effectId;
            RemainingRounds = remainingRounds;
            CasterLevel = casterLevel;
            MetamagicMask = metamagicMask;
            Suppressed = suppressed;
            SuppressionReadable = suppressionReadable;
        }

        public EffectKind Kind { get; private set; }
        public string EffectId { get; private set; }
        public double? RemainingRounds { get; private set; }
        public int? CasterLevel { get; private set; }
        public int? MetamagicMask { get; private set; }
        public bool Suppressed { get; private set; }
        // False when the game's suppression flag could not be read: the
        // instance still shows as present, but it never proves an existing
        // effect sufficient (review of rc4).
        public bool SuppressionReadable { get; private set; }

        public ActiveEffectMarker Marker
        {
            get { return new ActiveEffectMarker(Kind, EffectId); }
        }
    }

    public sealed class ActiveEffectSnapshot
    {
        private readonly IReadOnlyDictionary<string, ISet<ActiveEffectMarker>> _effectsByUnit;
        private IReadOnlyDictionary<string, IReadOnlyList<ActiveEffectInstance>> _instancesByUnit;

        public ActiveEffectSnapshot(IDictionary<string, IEnumerable<string>> effectsByUnit)
        {
            var copy = new Dictionary<string, ISet<ActiveEffectMarker>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, IEnumerable<string>> pair in
                effectsByUnit ?? new Dictionary<string, IEnumerable<string>>())
                copy[pair.Key] = new HashSet<ActiveEffectMarker>((pair.Value ?? new string[0])
                    .Select(id => new ActiveEffectMarker(EffectKind.Buff, id)));
            _effectsByUnit = new ReadOnlyDictionary<string, ISet<ActiveEffectMarker>>(copy);
        }

        private ActiveEffectSnapshot(IDictionary<string, IEnumerable<ActiveEffectMarker>> effectsByUnit)
        {
            var copy = new Dictionary<string, ISet<ActiveEffectMarker>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, IEnumerable<ActiveEffectMarker>> pair in effectsByUnit)
                copy[pair.Key] = new HashSet<ActiveEffectMarker>(pair.Value ?? new ActiveEffectMarker[0]);
            _effectsByUnit = new ReadOnlyDictionary<string, ISet<ActiveEffectMarker>>(copy);
        }

        public static ActiveEffectSnapshot FromTypedEffects(
            IDictionary<string, IEnumerable<ActiveEffectMarker>> effectsByUnit)
        {
            return new ActiveEffectSnapshot(effectsByUnit ??
                new Dictionary<string, IEnumerable<ActiveEffectMarker>>());
        }

        // A snapshot that also carries per-instance detail. Markers are
        // derived from every instance (suppressed ones included), so the
        // legacy planner sees exactly the markers it always saw; only the
        // casting-first existing-effect policy reads the instance detail.
        public static ActiveEffectSnapshot FromInstances(
            IDictionary<string, IEnumerable<ActiveEffectInstance>> instancesByUnit)
        {
            var markers = new Dictionary<string, IEnumerable<ActiveEffectMarker>>(
                StringComparer.Ordinal);
            var instances = new Dictionary<string, IReadOnlyList<ActiveEffectInstance>>(
                StringComparer.Ordinal);
            foreach (KeyValuePair<string, IEnumerable<ActiveEffectInstance>> pair in
                instancesByUnit ?? new Dictionary<string, IEnumerable<ActiveEffectInstance>>())
            {
                List<ActiveEffectInstance> list = (pair.Value ?? new ActiveEffectInstance[0])
                    .Where(value => value != null).ToList();
                instances[pair.Key] = new ReadOnlyCollection<ActiveEffectInstance>(list);
                markers[pair.Key] = list.Select(value => value.Marker).ToList();
            }
            var snapshot = new ActiveEffectSnapshot(markers);
            snapshot._instancesByUnit =
                new ReadOnlyDictionary<string, IReadOnlyList<ActiveEffectInstance>>(instances);
            return snapshot;
        }

        // True when this snapshot carries instance detail (FromInstances).
        public bool HasInstanceDetail
        {
            get { return _instancesByUnit != null; }
        }

        public IReadOnlyList<ActiveEffectInstance> GetInstances(string unitId)
        {
            IReadOnlyList<ActiveEffectInstance> instances;
            return _instancesByUnit != null && unitId != null &&
                _instancesByUnit.TryGetValue(unitId, out instances)
                    ? instances
                    : new ReadOnlyCollection<ActiveEffectInstance>(
                        new List<ActiveEffectInstance>());
        }

        public ISet<ActiveEffectMarker> GetEffects(string unitId)
        {
            ISet<ActiveEffectMarker> effects;
            return _effectsByUnit.TryGetValue(unitId, out effects)
                ? effects
                : new HashSet<ActiveEffectMarker>();
        }

        // Typed per-unit copy for callers that carry state forward (for
        // example a sequence forecast projecting granted effects).
        public Dictionary<string, HashSet<ActiveEffectMarker>> ToUnitMarkers()
        {
            var copy = new Dictionary<string, HashSet<ActiveEffectMarker>>(
                StringComparer.Ordinal);
            foreach (KeyValuePair<string, ISet<ActiveEffectMarker>> pair in _effectsByUnit)
                copy[pair.Key] = new HashSet<ActiveEffectMarker>(pair.Value);
            return copy;
        }
    }

    public sealed class ActiveEffectMarker : IEquatable<ActiveEffectMarker>
    {
        public ActiveEffectMarker(EffectKind kind, string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId)) throw new ArgumentException("Effect ID is required.", "effectId");
            Kind = kind;
            EffectId = effectId;
        }

        public EffectKind Kind { get; private set; }
        public string EffectId { get; private set; }
        public bool Equals(ActiveEffectMarker other)
        {
            return other != null && Kind == other.Kind &&
                string.Equals(EffectId, other.EffectId, StringComparison.Ordinal);
        }
        public override bool Equals(object obj) { return Equals(obj as ActiveEffectMarker); }
        public override int GetHashCode() { return ((int)Kind * 397) ^ EffectId.GetHashCode(); }
    }

    public sealed class TargetPlanOutcome
    {
        internal TargetPlanOutcome(string sourceId, string assignmentId, string unitId,
            TargetOutcomeKind kind, string reason, IEnumerable<string> markers)
        {
            SourceId = sourceId ?? string.Empty;
            AssignmentId = assignmentId ?? sourceId ?? string.Empty;
            UnitId = unitId;
            Kind = kind;
            Reason = reason ?? string.Empty;
            Markers = new ReadOnlyCollection<string>((markers ?? new string[0]).OrderBy(v => v, StringComparer.Ordinal).ToList());
        }

        public string SourceId { get; private set; }
        public string AssignmentId { get; private set; }
        public string UnitId { get; private set; }
        public TargetOutcomeKind Kind { get; private set; }
        public string Reason { get; private set; }
        public IReadOnlyList<string> Markers { get; private set; }
    }

    public sealed class CastStep
    {
        internal CastStep(
            string sourceId,
            string assignmentId,
            ProviderKey provider,
            string anchorUnitId,
            IEnumerable<string> targetUnitIds,
            IEnumerable<string> expectedRecipientUnitIds,
            ResourceReservation reservation,
            MaterialReservation materialReservation,
            EffectExpression expectedEffects,
            bool massCast,
            CastExecutionStrategy executionStrategy,
            string executionStrategyReason,
            IEnumerable<string> enhancementIds = null,
            IDictionary<string, int> enhancementUsageByPool = null,
            IEnumerable<string> omittedEnhancementIds = null,
            IEnumerable<string> preCoveredRecipientUnitIds = null)
        {
            SourceId = sourceId ?? string.Empty;
            AssignmentId = assignmentId ?? sourceId ?? string.Empty;
            Provider = provider;
            AnchorUnitId = anchorUnitId;
            TargetUnitIds = new ReadOnlyCollection<string>(targetUnitIds.OrderBy(v => v, StringComparer.Ordinal).ToList());
            ExpectedRecipientUnitIds = new ReadOnlyCollection<string>(
                (expectedRecipientUnitIds ?? targetUnitIds).Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToList());
            PreCoveredRecipientUnitIds = new ReadOnlyCollection<string>(
                (preCoveredRecipientUnitIds ?? new string[0])
                    .Where(v => !string.IsNullOrWhiteSpace(v) && ExpectedRecipientUnitIds.Contains(v))
                    .Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToList());
            Reservation = reservation;
            MaterialReservation = materialReservation;
            ExpectedEffects = expectedEffects;
            MassCast = massCast;
            ExecutionStrategy = executionStrategy;
            ExecutionStrategyReason = executionStrategyReason ?? string.Empty;
            EnhancementIds = new ReadOnlyCollection<string>((enhancementIds ?? new string[0])
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
            // Only explicitly permitted omissions appear here; required
            // enhancements never appear because their absence blocks the cast.
            OmittedEnhancementIds = new ReadOnlyCollection<string>((omittedEnhancementIds ?? new string[0])
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
            EnhancementUsageByPool = new ReadOnlyDictionary<string, int>(
                new Dictionary<string, int>(enhancementUsageByPool ??
                    new Dictionary<string, int>(), StringComparer.Ordinal));
        }

        public string SourceId { get; private set; }
        public string AssignmentId { get; private set; }
        public ProviderKey Provider { get; private set; }
        public string AnchorUnitId { get; private set; }
        public IReadOnlyList<string> TargetUnitIds { get; private set; }
        public IReadOnlyList<string> ExpectedRecipientUnitIds { get; private set; }
        // Expected recipients the plan proved already adequately covered
        // (mixed coverage under skip-if-active): confirmation accepts their
        // kept coverage; every other recipient needs a new or refreshed
        // instance.
        public IReadOnlyList<string> PreCoveredRecipientUnitIds { get; private set; }
        public ResourceReservation Reservation { get; private set; }
        public MaterialReservation MaterialReservation { get; private set; }
        public EffectExpression ExpectedEffects { get; private set; }
        public bool MassCast { get; private set; }
        public CastExecutionStrategy ExecutionStrategy { get; private set; }
        public string ExecutionStrategyReason { get; private set; }
        public IReadOnlyList<string> EnhancementIds { get; private set; }
        public IReadOnlyList<string> OmittedEnhancementIds { get; private set; }
        public IReadOnlyDictionary<string, int> EnhancementUsageByPool
        { get; private set; }
    }

    public sealed class MaterialReservation
    {
        internal MaterialReservation(string itemGuid, int count)
        {
            ItemGuid = itemGuid;
            Count = count;
        }

        public string ItemGuid { get; private set; }
        public int Count { get; private set; }
    }

    // One authoritative per-pool accounting line. The same structure covers
    // native resource pools and enhancement usage pools; UI summaries and
    // execution preparation read this instead of keeping their own counters.
    public sealed class ResourcePoolAllocation
    {
        internal ResourcePoolAllocation(
            string poolKey,
            ResourcePoolKind kind,
            int availableNow,
            int requestedUsage,
            int allocatedUsage,
            IEnumerable<string> traces)
        {
            PoolKey = poolKey;
            Kind = kind;
            AvailableNow = availableNow;
            RequestedUsage = requestedUsage;
            AllocatedUsage = allocatedUsage;
            UnmetDemand = Math.Max(0, requestedUsage - allocatedUsage);
            ForecastRemaining = Math.Max(0, availableNow - allocatedUsage);
            Traces = new ReadOnlyCollection<string>((traces ?? new string[0])
                .Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToList());
        }

        public string PoolKey { get; private set; }
        public ResourcePoolKind Kind { get; private set; }
        public int AvailableNow { get; private set; }
        public int RequestedUsage { get; private set; }
        public int AllocatedUsage { get; private set; }
        public int UnmetDemand { get; private set; }
        public int ForecastRemaining { get; private set; }
        public IReadOnlyList<string> Traces { get; private set; }
    }

    public sealed class CastPlan
    {
        internal CastPlan(IEnumerable<CastStep> steps, IEnumerable<TargetPlanOutcome> outcomes,
            IEnumerable<string> diagnostics,
            IEnumerable<ResourcePoolAllocation> resourceAllocations = null,
            IEnumerable<TargetPlanOutcome> unresolvableRequests = null)
        {
            Steps = new ReadOnlyCollection<CastStep>(steps.ToList());
            Outcomes = new ReadOnlyCollection<TargetPlanOutcome>(outcomes
                .OrderBy(o => o.AssignmentId, StringComparer.Ordinal)
                .ThenBy(o => o.UnitId, StringComparer.Ordinal).ToList());
            Diagnostics = new ReadOnlyCollection<string>(diagnostics.ToList());
            ResourceAllocations = new ReadOnlyCollection<ResourcePoolAllocation>(
                (resourceAllocations ?? new ResourcePoolAllocation[0]).ToList());
            // Configured child/target requests whose saved source cannot be
            // resolved at all (missing mod, unknown variant, ambiguous graph)
            // stay in requested-coverage accounting instead of vanishing.
            UnresolvableRequests = new ReadOnlyCollection<TargetPlanOutcome>(
                (unresolvableRequests ?? new TargetPlanOutcome[0])
                .OrderBy(o => o.AssignmentId, StringComparer.Ordinal)
                .ThenBy(o => o.UnitId, StringComparer.Ordinal).ToList());
        }

        public IReadOnlyList<CastStep> Steps { get; private set; }
        public IReadOnlyList<TargetPlanOutcome> Outcomes { get; private set; }
        public IReadOnlyList<string> Diagnostics { get; private set; }
        public IReadOnlyList<ResourcePoolAllocation> ResourceAllocations { get; private set; }
        public IReadOnlyList<TargetPlanOutcome> UnresolvableRequests { get; private set; }

        public ResourcePoolAllocation AllocationFor(string poolKey)
        {
            return ResourceAllocations.FirstOrDefault(allocation =>
                string.Equals(allocation.PoolKey, poolKey, StringComparison.Ordinal));
        }
    }
}
