using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;

namespace KingmakerBuffPlanner.Planning
{
    public sealed class RoutinePlanResult
    {
        internal RoutinePlanResult(
            CastPlan plan,
            IEnumerable<string> unsupportedSourceIds,
            IEnumerable<string> animatedFallbackSourceIds)
        {
            Plan = plan;
            UnsupportedSourceIds = new ReadOnlyCollection<string>((unsupportedSourceIds ?? new string[0])
                .OrderBy(v => v, StringComparer.Ordinal).ToList());
            AnimatedFallbackSourceIds = new ReadOnlyCollection<string>(
                (animatedFallbackSourceIds ?? new string[0]).Distinct(StringComparer.Ordinal)
                    .OrderBy(v => v, StringComparer.Ordinal).ToList());
        }

        public CastPlan Plan { get; private set; }
        public IReadOnlyList<string> UnsupportedSourceIds { get; private set; }
        public IReadOnlyList<string> AnimatedFallbackSourceIds { get; private set; }
    }

    public sealed class RoutinePlanService
    {
        public RoutinePlanResult Plan(
            BuffPlannerProfile profile,
            string routineId,
            PartyProviderSnapshot snapshot,
            ActiveEffectSnapshot activeEffects,
            IDictionary<string, EffectExpression> effectsBySource,
            IEnumerable<ProviderPlanningOption> providerOptions,
            IEnumerable<CastEnhancementSnapshot> enhancements = null,
            EffectiveProviderOptionResolver targeting = null)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            if (activeEffects == null) throw new ArgumentNullException("activeEffects");
            if (effectsBySource == null) throw new ArgumentNullException("effectsBySource");
            var optionList = (providerOptions ?? throw new ArgumentNullException("providerOptions")).ToList();
            RoutineProfile routine = profile.Routines.FirstOrDefault(r => r.RoutineId == routineId);
            if (routine == null) throw new ArgumentException("Unknown routine.", "routineId");
            var requests = new List<BuffCastRequest>();
            var unsupported = new List<string>();
            var unresolvable = new List<TargetPlanOutcome>();
            var animatedFallback = new HashSet<string>(StringComparer.Ordinal);
            var abilitiesBySource = new Dictionary<string, IReadOnlyList<Domain.Identity.AbilityKey>>(StringComparer.Ordinal);
            foreach (SourceAssignmentProfile assignment in routine.Assignments
                .OrderBy(a => a.SourceId, StringComparer.Ordinal))
            {
                EffectExpression expression;
                IReadOnlyList<Domain.Identity.AbilityKey> abilities = ResolveAbilities(
                    assignment, snapshot, effectsBySource, out expression);
                if (abilities.Count == 0 || expression == null ||
                    !EffectExpressionAnalysis.ContainsLeaf(expression))
                {
                    unsupported.Add(assignment.SourceId);
                    RecordUnresolvableChildren(assignment, unresolvable,
                        "source-unresolvable");
                    continue;
                }
                CastGroupingKind grouping;
                if (!EffectExpressionTargetAnalysis.TryGetGrouping(
                        expression, out grouping))
                {
                    unsupported.Add(assignment.SourceId);
                    RecordUnresolvableChildren(assignment, unresolvable,
                        "source-graph-unsupported");
                    continue;
                }
                abilitiesBySource[assignment.SourceId] = abilities;
                var definition = new BuffSourceDefinition(assignment.SourceId, abilities, expression, grouping);
                // Every child casting assignment becomes its own request with
                // its own identity, pins, ordered targets, and enhancement
                // selections; requests for one source never overwrite or
                // conflate each other.
                foreach (CastingAssignmentProfile casting in assignment.CastingAssignments)
                {
                    requests.Add(new BuffCastRequest(definition,
                        casting.TargetUnitIds, assignment.ExistingEffectPolicy,
                        assignment.IgnoredPresenceMarkers,
                        casting.Enhancements.Select(selection =>
                            new EnhancementRequest(selection.EnhancementId,
                                selection.IsRequired)),
                        casting.AssignmentId, casting.CasterUnitId,
                        casting.ProviderKey, casting.SpellbookGuid, casting.Order));
                }
            }
            ProviderSelectionPolicy policy = BuildPolicy(profile.ProviderPreferences);
            CastPlan plan = new CastPlanner(targeting).PlanRoutine(snapshot, requests,
                optionList, policy, activeEffects, enhancements);
            var fallbackProviderAbilities = new HashSet<string>(optionList
                .Where(o => o.RequiresAnimatedExecution)
                .Select(o => o.Provider.Key.Ability.Canonical), StringComparer.Ordinal);
            foreach (SourceAssignmentProfile assignment in routine.Assignments)
            {
                IReadOnlyList<Domain.Identity.AbilityKey> abilities;
                if (abilitiesBySource.TryGetValue(assignment.SourceId, out abilities) &&
                    abilities.Any(ability =>
                        fallbackProviderAbilities.Contains(ability.Canonical)))
                    animatedFallback.Add(assignment.SourceId);
            }
            var completePlan = new CastPlan(plan.Steps, plan.Outcomes,
                plan.Diagnostics.Concat(unresolvable.Select(request =>
                    "unresolvable-request:" + request.AssignmentId + ":" +
                    request.UnitId + ":" + request.Reason)).ToList(),
                plan.ResourceAllocations, unresolvable);
            return new RoutinePlanResult(completePlan, unsupported, animatedFallback);
        }

        // Configured targets beneath an unresolvable source stay visible as
        // requested-but-unresolvable outcomes with their assignment identity.
        private static void RecordUnresolvableChildren(
            SourceAssignmentProfile assignment, List<TargetPlanOutcome> unresolvable,
            string reason)
        {
            foreach (CastingAssignmentProfile casting in assignment.CastingAssignments)
                foreach (string unitId in casting.TargetUnitIds)
                    unresolvable.Add(new TargetPlanOutcome(assignment.SourceId,
                        casting.AssignmentId, unitId,
                        TargetOutcomeKind.Unfulfilled,
                        casting.AssignmentId + ":" + reason, new string[0]));
        }

        private static IReadOnlyList<Domain.Identity.AbilityKey> ResolveAbilities(
            SourceAssignmentProfile assignment,
            PartyProviderSnapshot snapshot,
            IDictionary<string, EffectExpression> effectsBySource,
            out EffectExpression expression)
        {
            expression = null;
            if (CatalogSourceIdentity.IsVariant(assignment.SourceId))
            {
                var matches = snapshot.Providers.Select(provider => provider.Key.Ability)
                    .Where(ability => CatalogSourceIdentity.MatchesVariant(
                        assignment.SourceId, ability))
                    .GroupBy(ability => ability.Canonical, StringComparer.Ordinal)
                    .Select(group => group.First())
                    .OrderBy(ability => ability.Canonical, StringComparer.Ordinal).ToList();
                if (matches.Count != 0)
                    effectsBySource.TryGetValue(matches[0].Canonical, out expression);
                return new ReadOnlyCollection<Domain.Identity.AbilityKey>(matches);
            }
            if (EffectAggregateIdentity.IsAggregate(assignment.SourceId))
            {
                var matches = snapshot.Providers.Select(provider => provider.Key.Ability)
                    .GroupBy(ability => ability.Canonical, StringComparer.Ordinal)
                    .Select(group => group.First()).Where(ability =>
                    {
                        EffectExpression candidate;
                        return effectsBySource.TryGetValue(ability.Canonical, out candidate) &&
                            EffectAggregateIdentity.For(candidate, ability.Canonical) == assignment.SourceId;
                    }).OrderBy(ability => ability.Canonical, StringComparer.Ordinal).ToList();
                if (matches.Count != 0)
                    effectsBySource.TryGetValue(matches[0].Canonical, out expression);
                return new ReadOnlyCollection<Domain.Identity.AbilityKey>(matches);
            }
            Domain.Identity.AbilityKey exact = assignment.Ability.ToKey();
            if (!effectsBySource.TryGetValue(assignment.SourceId, out expression))
                effectsBySource.TryGetValue(exact.Canonical, out expression);
            return expression == null
                ? new ReadOnlyCollection<Domain.Identity.AbilityKey>(new List<Domain.Identity.AbilityKey>())
                : new ReadOnlyCollection<Domain.Identity.AbilityKey>(new List<Domain.Identity.AbilityKey> { exact });
        }

        private static ProviderSelectionPolicy BuildPolicy(
            IEnumerable<ProviderPreferenceProfile> preferences)
        {
            var values = (preferences ?? new ProviderPreferenceProfile[0]).ToList();
            return new ProviderSelectionPolicy(
                values.Where(p => p.Banned).Select(p => p.ProviderKey),
                values.Where(p => p.Priority != null).ToDictionary(
                    p => p.ProviderKey, p => p.Priority.Value, StringComparer.Ordinal),
                values.Where(p => p.MaximumCasts != null).ToDictionary(
                    p => p.ProviderKey, p => p.MaximumCasts.Value, StringComparer.Ordinal));
        }
    }

    public static class EffectExpressionTargetAnalysis
    {
        public static bool Contains(EffectExpression expression, EffectTarget target)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) return leaf.Target == target;
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) return sequence.Children.Any(e => Contains(e, target));
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
                return Contains(conditional.WhenTrue, target) || Contains(conditional.WhenFalse, target);
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) return targeted.Target == target || Contains(targeted.Child, target);
            var referenced = expression as ReferencedAbilityExpression;
            return referenced != null && Contains(referenced.Child, target);
        }

        public static bool ContainsOnly(EffectExpression expression, EffectTarget target)
        {
            var targets = new HashSet<EffectTarget>();
            Collect(expression, targets);
            return targets.Count != 0 && targets.All(t => t == target);
        }

        public static bool TryGetGrouping(EffectExpression expression,
            out CastGroupingKind grouping)
        {
            grouping = CastGroupingKind.PerTarget;
            if (Contains(expression, EffectTarget.EnemyAreaRecipients) ||
                Contains(expression, EffectTarget.AmbiguousAreaRecipients))
                return false;
            if (Contains(expression, EffectTarget.Party) ||
                Contains(expression, EffectTarget.AlliedAreaRecipients))
                grouping = CastGroupingKind.MassConfiguredTargets;
            return true;
        }

        private static void Collect(EffectExpression expression, ISet<EffectTarget> targets)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) { targets.Add(leaf.Target); return; }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) { foreach (EffectExpression child in sequence.Children) Collect(child, targets); return; }
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null) { Collect(conditional.WhenTrue, targets); Collect(conditional.WhenFalse, targets); return; }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) { Collect(targeted.Child, targets); return; }
            var referenced = expression as ReferencedAbilityExpression;
            if (referenced != null) Collect(referenced.Child, targets);
        }
    }
}
