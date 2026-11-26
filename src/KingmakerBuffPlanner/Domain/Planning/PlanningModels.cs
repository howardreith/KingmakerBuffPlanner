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

    // WP4 (0.4.0) execution policy. Instant is strict: a casting runs
    // instantly only through a qualified rule-cast route and never falls
    // back to a normal animated cast; a casting that cannot is Not Ready in
    // Instant mode and runs only in the explicit Animated mode. Routines
    // never run during combat. Neither rule is a player setting any more;
    // the legacy persisted preference fields are normalized to this policy.
    public static class CastingExecutionPolicy
    {
        public const bool OutOfCombatOnly = true;
        public const bool AllowAnimatedFallback = false;
        public const string InstantRouteUnavailable = "instant-route-unavailable";
        public const string CombatActive = "combat-active";

        public static bool IsInstantCapable(CastExecutionStrategy strategy)
        {
            return strategy == CastExecutionStrategy.DirectRuleCast ||
                strategy == CastExecutionStrategy.StickyTouchDeliveryRuleCast ||
                strategy == CastExecutionStrategy.ProviderDirectRuleCast;
        }
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
        // The two instant sticky-touch reasons: a delivery that may also be
        // aimed at enemies yet is a willing-target buff (WP6), and one that
        // can only reach the caster or friends.
        public const string WillingTargetReason = "supported-willing-target-sticky-touch-delivery";
        public const string BeneficialReason = "supported-beneficial-sticky-touch-delivery";

        public static CastExecutionCapability Classify(
            bool isStickyTouch,
            bool hasDeliveryBlueprint,
            bool hasTouchDeliveryComponent,
            bool deliveryTargetsUnit,
            bool deliveryCanTargetSelf,
            bool deliveryCanTargetFriends,
            bool deliveryCanTargetEnemies,
            bool deliveryCanTargetPoint,
            bool deliveryHelpfulToAllies = false,
            bool deliveryHarmfulToEnemies = true)
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
            // WP6 (0.4.0): a delivery that may also be aimed at an enemy is
            // still a willing-target buff when it is helpful to allies and
            // not harmful to enemies (Magic Circle against Alignment: its
            // ally branch applies the carrier). The planner aims it only at
            // its own party, where the native touch attack auto-hits (the
            // instant adapter checks that condition before every cast).
            if (deliveryCanTargetEnemies &&
                (!deliveryHelpfulToAllies || deliveryHarmfulToEnemies))
                return AnimatedFallback("sticky-delivery-hostile-targeting-ambiguous");
            if (!deliveryCanTargetSelf && !deliveryCanTargetFriends)
                return AnimatedFallback("sticky-delivery-has-no-beneficial-unit-target");
            return new CastExecutionCapability(
                CastExecutionStrategy.StickyTouchDeliveryRuleCast,
                deliveryCanTargetEnemies ? WillingTargetReason : BeneficialReason);
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
            IDictionary<string, IEnumerable<string>> recipientIdsByAnchor = null,
            IReadOnlyDictionary<string, bool> casterFacts = null,
            IReadOnlyDictionary<string, EffectExpression> effectsByTarget = null,
            IDictionary<string, IEnumerable<string>> affectedIdsByAnchor = null,
            IDictionary<string, IReadOnlyDictionary<string, bool>> fixedRecipientFacts = null,
            IDictionary<string, IReadOnlyDictionary<string, bool>> recipientFactReads = null,
            bool requiresAllyRelation = false,
            IDictionary<string, IEnumerable<string>> uncertainIdsByAnchor = null)
        {
            Provider = provider ?? throw new ArgumentNullException("provider");
            RequiresAllyRelation = requiresAllyRelation;
            // Missing-beneficial-buffs recovery (R9-F5): the units an anchor's
            // burst may reach although that was not established (the live
            // distance from the centre could not be read). They are among the
            // affected units, so anything they might lose or have replaced is
            // accounted for, but nothing becomes certain on them.
            var uncertain = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, IEnumerable<string>> pair in uncertainIdsByAnchor ??
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal))
                if (!string.IsNullOrWhiteSpace(pair.Key)) uncertain[pair.Key] = Sorted(pair.Value);
            UncertainIdsByAnchor = new ReadOnlyDictionary<string, IReadOnlyList<string>>(uncertain);
            FixedRecipientFacts = CopyFacts(fixedRecipientFacts);
            RecipientFactReads = CopyFacts(recipientFactReads);
            // Missing-beneficial-buffs review R3-F1: every unit an anchor's
            // native actions reach (an ally inside the burst who only LOSES
            // an effect is not a beneficiary but is affected). Conflict
            // analysis reads these; coverage claims read RecipientIdsByAnchor.
            var affected = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, IEnumerable<string>> pair in affectedIdsByAnchor ??
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal))
                if (!string.IsNullOrWhiteSpace(pair.Key)) affected[pair.Key] = Sorted(pair.Value);
            AffectedIdsByAnchor = new ReadOnlyDictionary<string, IReadOnlyList<string>>(affected);
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
            CasterFacts = new ReadOnlyDictionary<string, bool>((casterFacts ?? new Dictionary<string, bool>())
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
            EffectsByTarget = new ReadOnlyDictionary<string, EffectExpression>((effectsByTarget ?? new Dictionary<string, EffectExpression>())
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
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
        public IReadOnlyDictionary<string, bool> CasterFacts { get; private set; }
        public IReadOnlyDictionary<string, EffectExpression> EffectsByTarget { get; private set; }
        public EffectExpression BindEffects(EffectExpression expression, string target)
        {
            EffectExpression bound;
            return target != null && EffectsByTarget.TryGetValue(target, out bound) ? bound :
                EffectRecipientBinding.Bind(EffectConditionBinding.Bind(expression, CasterFacts), rule => null);
        }
        // Greater Magic Fang repair (review): only the caster-level tier
        // selection bound from this provider's caster facts. The Classic
        // planner keeps its steps' expression otherwise unbound; this makes a
        // step expect the one tier its provider applies.
        public EffectExpression BindCasterLevel(EffectExpression expression)
        {
            int level;
            return EffectConditionBinding.Bind(expression, CasterFacts
                .Where(fact => CasterLevelPredicate.TryParse(fact.Key, out level))
                .ToDictionary(fact => fact.Key, fact => fact.Value, StringComparer.Ordinal));
        }
        public bool RequiresAnimatedExecution
        {
            get
            {
                return ExecutionStrategy == CastExecutionStrategy.AnimatedFallback ||
                    ExecutionStrategy == CastExecutionStrategy.NativeCommandRequired;
            }
        }

        public IReadOnlyDictionary<string, IReadOnlyList<string>> AffectedIdsByAnchor { get; private set; }

        // Missing-beneficial-buffs recovery (R9-F5): the affected units whose
        // reach from an anchor is possible but not established.
        public IReadOnlyDictionary<string, IReadOnlyList<string>> UncertainIdsByAnchor { get; private set; }

        // Missing-beneficial-buffs recovery (R9-F5, R10-F1): whether the
        // native actions cast from the anchor certainly reach the unit - its
        // distance was measured inside the burst and, under an Ally-only
        // selector, its relation to the caster was read as allied. A unit only
        // possibly reached may lose or have replaced what it has (invalidation
        // and removals count it); what the cast applies there is never certain
        // (no restored removal, no granted coverage, no replacement evidence).
        public bool ReachesDefinitely(string anchorUnitId, string unitId)
        {
            if (string.IsNullOrEmpty(unitId)) return false;
            if (RequiresAllyRelation && RecipientFactRead(unitId, RecipientFactGate.AllyOfCaster) != true) return false;
            IReadOnlyList<string> uncertain;
            return string.IsNullOrWhiteSpace(anchorUnitId) ||
                !UncertainIdsByAnchor.TryGetValue(anchorUnitId, out uncertain) || !uncertain.Contains(unitId);
        }

        // The uncertain-reach map in the constructor's shape (for copies).
        public IDictionary<string, IEnumerable<string>> UncertainCopy()
        {
            return UncertainIdsByAnchor.ToDictionary(pair => pair.Key, pair => (IEnumerable<string>)pair.Value,
                StringComparer.Ordinal);
        }

        // Review R4-F3: fixed facts (creature types, never a buff a casting
        // can add or remove) read for units inside the option's bursts.
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, bool>> FixedRecipientFacts { get; private set; }

        // Review R7-F6: every recipient-fact gate key read live for units
        // inside the option's bursts (fixed and mutable; an unread key is
        // absent). The starting state of a sequential forecast.
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, bool>> RecipientFactReads { get; private set; }

        // Review R10-F1: the burst's native selector admits only units allied
        // to the caster (AbilityTargetsAround TargetType.Ally); a recomputed
        // beneficiary needs a read, allied relation.
        public bool RequiresAllyRelation { get; private set; }

        public bool? RecipientFactRead(string unit, string key)
        {
            IReadOnlyDictionary<string, bool> facts;
            bool value;
            return unit != null && key != null && RecipientFactReads.TryGetValue(unit, out facts) &&
                facts.TryGetValue(key, out value) ? value : (bool?)null;
        }

        private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, bool>> CopyFacts(
            IDictionary<string, IReadOnlyDictionary<string, bool>> facts)
        {
            return new ReadOnlyDictionary<string, IReadOnlyDictionary<string, bool>>(
                (facts ?? new Dictionary<string, IReadOnlyDictionary<string, bool>>())
                    .Where(pair => pair.Key != null && pair.Value != null)
                    .ToDictionary(pair => pair.Key, pair => (IReadOnlyDictionary<string, bool>)
                        new ReadOnlyDictionary<string, bool>(pair.Value.ToDictionary(fact => fact.Key,
                            fact => fact.Value, StringComparer.Ordinal)), StringComparer.Ordinal));
        }

        // The expression as it acts on one recipient: the provider binding
        // plus that recipient's fixed facts. Mutable facts (an opposing buff
        // an earlier casting may add) stay unresolved, so conflict analysis
        // keeps every branch they could select.
        public EffectExpression BindForRecipient(EffectExpression expression, string recipient)
        {
            EffectExpression bound = BindEffects(expression, recipient);
            IReadOnlyDictionary<string, bool> facts;
            if (recipient == null || !FixedRecipientFacts.TryGetValue(recipient, out facts) || facts.Count == 0)
                return bound;
            bool unresolved;
            return EffectRecipientFactBinding.Bind(bound, fact =>
            {
                bool value;
                return facts.TryGetValue(fact, out value) ? value : (bool?)null;
            }, out unresolved);
        }

        // Review R5-F5: the same option with narrowed per-anchor beneficiaries
        // (a sequential forecast's later routine); everything else unchanged.
        // Review R7-F6: a widened anchor may name a unit no anchor covered at
        // the start; it becomes reachable.
        // Review R10 (R8-F3): optionally with the recipient fact reads the new
        // map was computed from (the state earlier occurrences leave), so a
        // later consumer recomputing coverage starts from that same state.
        public ProviderPlanningOption WithRecipientIdsByAnchor(
            IDictionary<string, IEnumerable<string>> recipientIdsByAnchor,
            IDictionary<string, IReadOnlyDictionary<string, bool>> recipientFactReads = null)
        {
            IEnumerable<string> reachable = ReachableTargetIds.Concat((recipientIdsByAnchor ??
                    new Dictionary<string, IEnumerable<string>>()).Values.SelectMany(value => value ?? new string[0]));
            return new ProviderPlanningOption(Provider, reachable, LegalAnchorIds, EffectiveCasterLevel,
                ExpectedDurationRounds, ExecutionStrategy, ExecutionStrategyReason, recipientIdsByAnchor,
                CasterFacts, EffectsByTarget,
                AffectedIdsByAnchor.ToDictionary(pair => pair.Key, pair => (IEnumerable<string>)pair.Value,
                    StringComparer.Ordinal),
                FixedRecipientFacts.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
                recipientFactReads ??
                    RecipientFactReads.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
                RequiresAllyRelation, UncertainCopy());
        }

        // The units an anchor's native actions reach (beneficiaries and units
        // that only lose an effect); the covered units when not recorded.
        public IReadOnlyList<string> AffectedTargetIdsForAnchor(string anchorUnitId)
        {
            IReadOnlyList<string> affected;
            IReadOnlyList<string> covered = CoveredTargetIdsForAnchor(anchorUnitId);
            return !string.IsNullOrWhiteSpace(anchorUnitId) &&
                AffectedIdsByAnchor.TryGetValue(anchorUnitId, out affected)
                ? new ReadOnlyCollection<string>(affected.Concat(covered).Distinct(StringComparer.Ordinal)
                    .OrderBy(v => v, StringComparer.Ordinal).ToList())
                : covered;
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

    // Missing-beneficial-buffs review R5-F5 / R7-F6: the recipient state of a
    // sequential forecast. It starts from the live reads each option made of
    // its recipient-fact gates (RecipientFactReads) and applies every funded
    // casting's changes as that casting binds for each unit it reaches under
    // the state so far: an effect it certainly leaves is known present, one
    // it certainly removes known absent, anything only possible becomes
    // unknown unless the state already agrees. A later group casting's
    // beneficiaries are then every unit its burst reaches whose payload
    // definitely lands under that state - narrowed (Mass Reduce on an ally an
    // earlier Mass Enlarge reached) or widened (Good Hope on allies whose
    // Despair an earlier Good Hope removed); an unknown or unread condition
    // withholds the claim. Every earlier funded casting of the same routine
    // is visible (review R8-F3), and in a one-pass sequence those of earlier
    // routines too (castings are grouped by routine in sequence order); a
    // casting that would undo an earlier requested effect keeps its specific
    // removal-conflict refusal (the compiler checks it first).
    public sealed class SequentialRecipientState
    {
        private readonly Dictionary<string, Dictionary<string, bool?>> _committed =
            new Dictionary<string, Dictionary<string, bool?>>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, bool?>> _pending =
            new Dictionary<string, Dictionary<string, bool?>>(StringComparer.Ordinal);
        private string _routine;
        private readonly bool _acrossRoutines;

        // Review R8-F3: the routine's own earlier funded castings always
        // count (they run first); earlier routines only in a one-pass
        // sequence (acrossRoutines) - an independent routine preview starts
        // from its own snapshot.
        public SequentialRecipientState(bool acrossRoutines = true)
        {
            _acrossRoutines = acrossRoutines;
        }

        // Whether anything earlier changed anything.
        public bool HasChanges { get { return _committed.Count != 0 || _pending.Count != 0; } }

        // Called for every casting in sequence order.
        public void EnterRoutine(string routineId)
        {
            routineId = routineId ?? string.Empty;
            if (_routine != null && string.Equals(_routine, routineId, StringComparison.Ordinal)) return;
            if (_acrossRoutines)
                foreach (KeyValuePair<string, Dictionary<string, bool?>> unit in _pending)
                    foreach (KeyValuePair<string, bool?> effect in unit.Value)
                        Set(_committed, unit.Key, effect.Key, effect.Value);
            else
                _committed.Clear();
            _pending.Clear();
            _routine = routineId;
        }

        // Every unit an anchor's burst reaches whose payload definitely lands
        // under the state earlier routines leave.
        public List<string> Beneficiaries(ProviderPlanningOption option, EffectExpression expression, string anchor)
        {
            if (option == null || expression == null) return new List<string>();
            return option.AffectedTargetIdsForAnchor(anchor)
                .Where(unit => Receives(option, expression, unit, anchor)).ToList();
        }

        public bool Receives(ProviderPlanningOption option, EffectExpression expression, string unit)
        {
            return Receives(option, expression, unit, null);
        }

        public bool Receives(ProviderPlanningOption option, EffectExpression expression, string unit, string anchor)
        {
            // Review R10-F1: an Ally-only native selector needs a read,
            // allied relation. Recovery (R9-F5): and a unit whose distance
            // from the centre was not measured is not certainly reached.
            if (!option.ReachesDefinitely(anchor, unit)) return false;
            bool unresolved;
            EffectExpression bound = Bind(option, expression, unit, true, out unresolved);
            // Review R9-F3: a unit read as hostile to the caster is affected
            // by the enemy branch, never a beneficiary.
            return !unresolved && EffectExpressionAnalysis.ContainsDefiniteLeaf(bound) &&
                !EffectExpressionAnalysis.HasCasterGate(bound) &&
                !EffectRecipientFactBinding.HostileOnPath(option.BindEffects(expression, unit),
                    Reader(option, unit, true));
        }

        // One funded casting's changes on the units it reaches (its own
        // routine's earlier changes included).
        public void Record(ProviderPlanningOption option, EffectExpression expression,
            IEnumerable<string> reached, string casterUnitId, string anchor = null)
        {
            if (option == null || expression == null) return;
            foreach (string unit in (reached ?? new string[0]).Distinct(StringComparer.Ordinal))
            {
                bool unresolved;
                EffectChanges changes = EffectChangeAnalysis.Analyze(
                    Bind(option, expression, unit, true, out unresolved), casterUnitId, unit);
                // Review R11 (R10-F1): a unit an Ally-only selector may skip
                // (relation to the caster not read as allied) is only
                // possibly reached: nothing becomes definite on it (a
                // possible change makes the state unknown unless it agrees).
                // Recovery (R9-F5): likewise a unit whose distance from the
                // centre was not measured.
                bool possiblyReached = !option.ReachesDefinitely(anchor, unit);
                var guaranteed = new HashSet<KeyValuePair<string, string>>(possiblyReached
                    ? new KeyValuePair<string, string>[0] : changes.Guaranteed);
                var certainlyRemoved = new HashSet<KeyValuePair<string, string>>(possiblyReached
                    ? new KeyValuePair<string, string>[0] : changes.CertainlyRemoved);
                foreach (KeyValuePair<string, string> key in changes.Applied.Where(k => !guaranteed.Contains(k)))
                    Set(_pending, key.Key, key.Value, Current(option, key.Key, key.Value) == true ? true : (bool?)null);
                foreach (KeyValuePair<string, string> key in changes.Removed.Where(k => !certainlyRemoved.Contains(k)))
                    Set(_pending, key.Key, key.Value, Current(option, key.Key, key.Value) == false ? false : (bool?)null);
                foreach (KeyValuePair<string, string> key in guaranteed) Set(_pending, key.Key, key.Value, true);
                foreach (KeyValuePair<string, string> key in certainlyRemoved) Set(_pending, key.Key, key.Value, false);
            }
        }

        private EffectExpression Bind(ProviderPlanningOption option, EffectExpression expression, string unit,
            bool includePending, out bool unresolved)
        {
            EffectExpression bound = option.BindForRecipient(expression, unit);
            return EffectRecipientFactBinding.Bind(bound, Reader(option, unit, includePending), out unresolved);
        }

        // A recipient-fact read under the state: relations from the option's
        // live read; effects from a recorded change, else the option's read.
        private Func<string, bool?> Reader(ProviderPlanningOption option, string unit, bool includePending)
        {
            return key =>
            {
                if (RecipientFactGate.IsRelation(key)) return option.RecipientFactRead(unit, key);
                bool? value;
                string effect = RecipientFactGate.EffectIdOf(key);
                if (includePending && TryGet(_pending, unit, effect, out value)) return value;
                if (TryGet(_committed, unit, effect, out value)) return value;
                return option.RecipientFactRead(unit, key);
            };
        }

        // An effect's value on a unit so far: a recorded change, else the
        // option's starting read of it as a fact or buff predicate.
        private bool? Current(ProviderPlanningOption option, string unit, string effect)
        {
            bool? value;
            if (TryGet(_pending, unit, effect, out value) || TryGet(_committed, unit, effect, out value)) return value;
            return option.RecipientFactRead(unit, effect) ??
                option.RecipientFactRead(unit, RecipientFactGate.BuffPredicate(effect));
        }

        private static bool TryGet(Dictionary<string, Dictionary<string, bool?>> state, string unit, string effect,
            out bool? value)
        {
            Dictionary<string, bool?> effects;
            value = null;
            return unit != null && effect != null && state.TryGetValue(unit, out effects) &&
                effects.TryGetValue(effect, out value);
        }

        private static void Set(Dictionary<string, Dictionary<string, bool?>> state, string unit, string effect,
            bool? value)
        {
            if (unit == null || effect == null) return;
            Dictionary<string, bool?> effects;
            if (!state.TryGetValue(unit, out effects))
                state[unit] = effects = new Dictionary<string, bool?>(StringComparer.Ordinal);
            effects[effect] = value;
        }

        // The option with every anchor's beneficiaries recomputed under the
        // state (the option itself when nothing earlier changed anything).
        public ProviderPlanningOption Reevaluate(ProviderPlanningOption option, EffectExpression expression)
        {
            if (!HasChanges || option == null || expression == null || option.RecipientIdsByAnchor.Count == 0)
                return option;
            var map = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, IReadOnlyList<string>> pair in option.RecipientIdsByAnchor)
                map[pair.Key] = Beneficiaries(option, expression, pair.Key);
            // Review R10 (R8-F3): the returned option also carries the state
            // as its recipient fact reads, so a consumer that starts its own
            // state from the option (a later Classic routine's assignments)
            // keeps what earlier occurrences changed.
            return option.WithRecipientIdsByAnchor(map, StateReads(option, expression));
        }

        // The option's recipient fact reads with every recorded change applied
        // (a known value replaces the read; an unknown one removes it).
        private IDictionary<string, IReadOnlyDictionary<string, bool>> StateReads(ProviderPlanningOption option,
            EffectExpression expression)
        {
            var keys = EffectRecipientFactBinding.FactIds(expression)
                .Where(key => !RecipientFactGate.IsRelation(key)).ToList();
            var units = new HashSet<string>(option.RecipientFactReads.Keys, StringComparer.Ordinal);
            units.UnionWith(_committed.Keys);
            units.UnionWith(_pending.Keys);
            var result = new Dictionary<string, IReadOnlyDictionary<string, bool>>(StringComparer.Ordinal);
            foreach (string unit in units)
            {
                IReadOnlyDictionary<string, bool> original;
                var reads = option.RecipientFactReads.TryGetValue(unit, out original)
                    ? original.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
                    : new Dictionary<string, bool>(StringComparer.Ordinal);
                foreach (string key in keys)
                {
                    bool? value;
                    string effect = RecipientFactGate.EffectIdOf(key);
                    if (!TryGet(_pending, unit, effect, out value) && !TryGet(_committed, unit, effect, out value))
                        continue;
                    if (value.HasValue) reads[key] = value.Value;
                    else reads.Remove(key);
                }
                if (reads.Count != 0) result[unit] = reads;
            }
            return result;
        }

        // Review R10 (R9-F1): what earlier funded castings left certain about
        // an effect on a unit (null: unknown or not recorded).
        public bool? Known(string unit, string effect)
        {
            bool? value;
            if (TryGet(_pending, unit, effect, out value) || TryGet(_committed, unit, effect, out value)) return value;
            return null;
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
            bool suppressed = false, bool suppressionReadable = true, string casterUnitId = null)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                throw new ArgumentException("Effect ID is required.", "effectId");
            CasterUnitId = string.IsNullOrWhiteSpace(casterUnitId) ? null : casterUnitId;
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
        // Review R8-F1: the unit whose cast applied the instance (null when
        // unread); a caster-relative effect is only proven for that caster.
        public string CasterUnitId { get; private set; }

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
            IEnumerable<string> preCoveredRecipientUnitIds = null,
            bool exactEnhancements = false,
            string requiredItemSelector = null)
        {
            RequiredItemSelector = requiredItemSelector ?? string.Empty;
            ExactEnhancements = exactEnhancements;
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
            EffectObservationUnitIds = new ReadOnlyCollection<string>(ExpectedRecipientUnitIds.SelectMany(id =>
                new EffectRecipientScope(expectedEffects, provider == null ? null : provider.CasterUnitId, id).UnitIds)
                .Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToList());
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
        // The casting's authored item-slot rule, or "". Execution validates it
        // in addition to Provider, whose key may be the plain rule.
        public string RequiredItemSelector { get; private set; }
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
        // Observation owners include mixed-payload caster effects, without
        // turning the caster into another primary area beneficiary/cast target.
        public IReadOnlyList<string> EffectObservationUnitIds { get; private set; }
        public bool MassCast { get; private set; }
        public CastExecutionStrategy ExecutionStrategy { get; private set; }
        public string ExecutionStrategyReason { get; private set; }
        public IReadOnlyList<string> EnhancementIds { get; private set; }
        // Casting-first steps: of the enhancements the planner models
        // (metamagic rods, Powerful Change, Share Transmutation), the cast
        // applies exactly EnhancementIds. One the player left switched on in
        // the game is switched off for the cast and back on after it, or the
        // cast is refused when the game would keep it applied (advanced
        // qualification 2026-09-24: an Extend rod left on was spent on a
        // casting that did not choose it). Toggles of other mods that change
        // spells are not managed. Classic steps leave the game's switches as
        // they are when they choose no enhancement.
        public bool ExactEnhancements { get; private set; }
        public IReadOnlyList<string> OmittedEnhancementIds { get; private set; }
        public IReadOnlyDictionary<string, int> EnhancementUsageByPool
        { get; private set; }

        // The same step with run context on its reservation (see
        // ResourceReservation.EarlierConfirmedTokenIds); every other value,
        // including the authorized tokens, is unchanged.
        internal CastStep WithReservation(ResourceReservation reservation)
        {
            var copy = (CastStep)MemberwiseClone();
            copy.Reservation = reservation;
            return copy;
        }
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
