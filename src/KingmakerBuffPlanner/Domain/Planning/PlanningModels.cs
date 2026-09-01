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

    public sealed class BuffCastRequest
    {
        public BuffCastRequest(
            BuffSourceDefinition source,
            IEnumerable<string> targetUnitIds,
            ExistingEffectPolicy existingEffectPolicy,
            IEnumerable<string> ignoredEffectIds,
            IEnumerable<string> enhancementIds = null)
        {
            Source = source ?? throw new ArgumentNullException("source");
            TargetUnitIds = new ReadOnlyCollection<string>((targetUnitIds ?? throw new ArgumentNullException("targetUnitIds"))
                .Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.Ordinal)
                .OrderBy(v => v, StringComparer.Ordinal).ToList());
            ExistingEffectPolicy = existingEffectPolicy;
            IgnoredEffectIds = new ReadOnlyCollection<string>((ignoredEffectIds ?? new string[0])
                .Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.Ordinal)
                .OrderBy(v => v, StringComparer.Ordinal).ToList());
            EnhancementIds = new ReadOnlyCollection<string>((enhancementIds ?? new string[0])
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .OrderBy(v => v, StringComparer.Ordinal).ToList());
        }

        public BuffSourceDefinition Source { get; private set; }
        public IReadOnlyList<string> TargetUnitIds { get; private set; }
        public ExistingEffectPolicy ExistingEffectPolicy { get; private set; }
        public IReadOnlyList<string> IgnoredEffectIds { get; private set; }
        public IReadOnlyList<string> EnhancementIds { get; private set; }
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
            RequiresAnimatedExecution = requiresAnimatedExecution;
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
        public bool RequiresAnimatedExecution { get; private set; }

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

    public sealed class ActiveEffectSnapshot
    {
        private readonly IReadOnlyDictionary<string, ISet<ActiveEffectMarker>> _effectsByUnit;

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

        public ISet<ActiveEffectMarker> GetEffects(string unitId)
        {
            ISet<ActiveEffectMarker> effects;
            return _effectsByUnit.TryGetValue(unitId, out effects)
                ? effects
                : new HashSet<ActiveEffectMarker>();
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
        internal TargetPlanOutcome(string sourceId, string unitId, TargetOutcomeKind kind,
            string reason, IEnumerable<string> markers)
        {
            SourceId = sourceId ?? string.Empty;
            UnitId = unitId;
            Kind = kind;
            Reason = reason ?? string.Empty;
            Markers = new ReadOnlyCollection<string>((markers ?? new string[0]).OrderBy(v => v, StringComparer.Ordinal).ToList());
        }

        public string SourceId { get; private set; }
        public string UnitId { get; private set; }
        public TargetOutcomeKind Kind { get; private set; }
        public string Reason { get; private set; }
        public IReadOnlyList<string> Markers { get; private set; }
    }

    public sealed class CastStep
    {
        internal CastStep(
            string sourceId,
            ProviderKey provider,
            string anchorUnitId,
            IEnumerable<string> targetUnitIds,
            IEnumerable<string> expectedRecipientUnitIds,
            ResourceReservation reservation,
            MaterialReservation materialReservation,
            EffectExpression expectedEffects,
            bool massCast,
            IEnumerable<string> enhancementIds = null,
            IDictionary<string, int> enhancementUsageByPool = null)
        {
            SourceId = sourceId ?? string.Empty;
            Provider = provider;
            AnchorUnitId = anchorUnitId;
            TargetUnitIds = new ReadOnlyCollection<string>(targetUnitIds.OrderBy(v => v, StringComparer.Ordinal).ToList());
            ExpectedRecipientUnitIds = new ReadOnlyCollection<string>(
                (expectedRecipientUnitIds ?? targetUnitIds).Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToList());
            Reservation = reservation;
            MaterialReservation = materialReservation;
            ExpectedEffects = expectedEffects;
            MassCast = massCast;
            EnhancementIds = new ReadOnlyCollection<string>((enhancementIds ?? new string[0])
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
            EnhancementUsageByPool = new ReadOnlyDictionary<string, int>(
                new Dictionary<string, int>(enhancementUsageByPool ??
                    new Dictionary<string, int>(), StringComparer.Ordinal));
        }

        public string SourceId { get; private set; }
        public ProviderKey Provider { get; private set; }
        public string AnchorUnitId { get; private set; }
        public IReadOnlyList<string> TargetUnitIds { get; private set; }
        public IReadOnlyList<string> ExpectedRecipientUnitIds { get; private set; }
        public ResourceReservation Reservation { get; private set; }
        public MaterialReservation MaterialReservation { get; private set; }
        public EffectExpression ExpectedEffects { get; private set; }
        public bool MassCast { get; private set; }
        public IReadOnlyList<string> EnhancementIds { get; private set; }
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

    public sealed class CastPlan
    {
        internal CastPlan(IEnumerable<CastStep> steps, IEnumerable<TargetPlanOutcome> outcomes, IEnumerable<string> diagnostics)
        {
            Steps = new ReadOnlyCollection<CastStep>(steps.ToList());
            Outcomes = new ReadOnlyCollection<TargetPlanOutcome>(outcomes.OrderBy(o => o.UnitId, StringComparer.Ordinal).ToList());
            Diagnostics = new ReadOnlyCollection<string>(diagnostics.ToList());
        }

        public IReadOnlyList<CastStep> Steps { get; private set; }
        public IReadOnlyList<TargetPlanOutcome> Outcomes { get; private set; }
        public IReadOnlyList<string> Diagnostics { get; private set; }
    }
}
