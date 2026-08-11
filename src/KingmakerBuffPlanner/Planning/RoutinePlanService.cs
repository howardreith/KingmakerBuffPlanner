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
        internal RoutinePlanResult(CastPlan plan, IEnumerable<string> unsupportedSourceIds)
        {
            Plan = plan;
            UnsupportedSourceIds = new ReadOnlyCollection<string>((unsupportedSourceIds ?? new string[0])
                .OrderBy(v => v, StringComparer.Ordinal).ToList());
        }

        public CastPlan Plan { get; private set; }
        public IReadOnlyList<string> UnsupportedSourceIds { get; private set; }
    }

    public sealed class RoutinePlanService
    {
        public RoutinePlanResult Plan(
            BuffPlannerProfile profile,
            string routineId,
            PartyProviderSnapshot snapshot,
            ActiveEffectSnapshot activeEffects,
            IDictionary<string, EffectExpression> effectsBySource,
            IEnumerable<ProviderPlanningOption> providerOptions)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            if (activeEffects == null) throw new ArgumentNullException("activeEffects");
            if (effectsBySource == null) throw new ArgumentNullException("effectsBySource");
            RoutineProfile routine = profile.Routines.FirstOrDefault(r => r.RoutineId == routineId);
            if (routine == null) throw new ArgumentException("Unknown routine.", "routineId");
            var requests = new List<BuffCastRequest>();
            var unsupported = new List<string>();
            foreach (SourceAssignmentProfile assignment in routine.Assignments
                .OrderBy(a => a.SourceId, StringComparer.Ordinal))
            {
                EffectExpression expression;
                if (!effectsBySource.TryGetValue(assignment.SourceId, out expression) ||
                    !EffectExpressionAnalysis.ContainsLeaf(expression))
                {
                    unsupported.Add(assignment.SourceId);
                    continue;
                }
                CastGroupingKind grouping = EffectExpressionTargetAnalysis.Contains(
                    expression, EffectTarget.Party)
                    ? CastGroupingKind.MassConfiguredTargets
                    : CastGroupingKind.PerTarget;
                requests.Add(new BuffCastRequest(
                    new BuffSourceDefinition(assignment.SourceId, assignment.Ability.ToKey(), expression, grouping),
                    assignment.WantedTargetUnitIds, assignment.ExistingEffectPolicy,
                    assignment.IgnoredPresenceMarkers));
            }
            ProviderSelectionPolicy policy = BuildPolicy(profile.ProviderPreferences);
            CastPlan plan = new CastPlanner().PlanRoutine(snapshot, requests,
                providerOptions, policy, activeEffects);
            return new RoutinePlanResult(plan, unsupported);
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
