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
            IEnumerable<CastEnhancementSnapshot> enhancements = null)
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
                    continue;
                }
                CastGroupingKind grouping = EffectExpressionTargetAnalysis.Contains(
                    expression, EffectTarget.Party) ||
                    EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.AreaRecipients)
                    ? CastGroupingKind.MassConfiguredTargets
                    : CastGroupingKind.PerTarget;
                requests.Add(new BuffCastRequest(
                    new BuffSourceDefinition(assignment.SourceId, abilities, expression, grouping),
                    assignment.WantedTargetUnitIds, assignment.ExistingEffectPolicy,
                    assignment.IgnoredPresenceMarkers, assignment.SelectedEnhancementIds));
                abilitiesBySource[assignment.SourceId] = abilities;
            }
            ProviderSelectionPolicy policy = BuildPolicy(profile.ProviderPreferences);
            CastPlan plan = new CastPlanner().PlanRoutine(snapshot, requests,
                optionList, policy, activeEffects, enhancements);
            var fallbackProviderAbilities = new HashSet<string>(optionList
                .Where(o => o.RequiresAnimatedExecution)
                .Select(o => o.Provider.Key.Ability.Canonical), StringComparer.Ordinal);
            return new RoutinePlanResult(plan, unsupported, routine.Assignments
                .Where(a => abilitiesBySource.ContainsKey(a.SourceId) &&
                    abilitiesBySource[a.SourceId].Any(ability =>
                        fallbackProviderAbilities.Contains(ability.Canonical)))
                .Select(a => a.SourceId));
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
