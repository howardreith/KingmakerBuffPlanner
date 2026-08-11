using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Planning
{
    public enum EffectPresenceKind
    {
        Absent,
        Partial,
        Complete
    }

    public sealed class EffectPresenceResult
    {
        internal EffectPresenceResult(EffectPresenceKind kind, IEnumerable<string> markers)
        {
            Kind = kind;
            PresentMarkers = new ReadOnlyCollection<string>(markers.Distinct(StringComparer.Ordinal)
                .OrderBy(v => v, StringComparer.Ordinal).ToList());
        }

        public EffectPresenceKind Kind { get; private set; }
        public IReadOnlyList<string> PresentMarkers { get; private set; }
    }

    public sealed class EffectPresenceEvaluator
    {
        public EffectPresenceResult Evaluate(
            EffectExpression expression,
            ISet<string> activeEffectIds,
            ISet<string> ignoredEffectIds)
        {
            var typed = new HashSet<ActiveEffectMarker>((activeEffectIds ??
                new HashSet<string>(StringComparer.Ordinal)).Select(id =>
                    new ActiveEffectMarker(EffectKind.Buff, id)));
            return EvaluateTyped(expression, typed, ignoredEffectIds);
        }

        public EffectPresenceResult EvaluateTyped(
            EffectExpression expression,
            ISet<ActiveEffectMarker> activeEffects,
            ISet<string> ignoredEffectIds)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            NodePresence node = Visit(expression,
                activeEffects ?? new HashSet<ActiveEffectMarker>(),
                ignoredEffectIds ?? new HashSet<string>(StringComparer.Ordinal));
            EffectPresenceKind kind = node.Relevant == 0
                ? EffectPresenceKind.Absent
                : node.Complete ? EffectPresenceKind.Complete
                : node.Present ? EffectPresenceKind.Partial
                : EffectPresenceKind.Absent;
            return new EffectPresenceResult(kind, node.Markers);
        }

        private static NodePresence Visit(
            EffectExpression expression,
            ISet<ActiveEffectMarker> active,
            ISet<string> ignored)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null)
            {
                if (ignored.Contains(leaf.EffectId)) return NodePresence.Irrelevant();
                bool present = active.Contains(new ActiveEffectMarker(leaf.Kind, leaf.EffectId));
                return new NodePresence(1, present, present,
                    present ? new[] { leaf.EffectId } : new string[0]);
            }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) return AllOf(sequence.Children.Select(e => Visit(e, active, ignored)));
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null) return AnyOf(new[]
            {
                Visit(conditional.WhenTrue, active, ignored),
                Visit(conditional.WhenFalse, active, ignored)
            });
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) return Visit(targeted.Child, active, ignored);
            var referenced = expression as ReferencedAbilityExpression;
            if (referenced != null) return Visit(referenced.Child, active, ignored);
            return NodePresence.Irrelevant();
        }

        private static NodePresence AllOf(IEnumerable<NodePresence> values)
        {
            var relevant = values.Where(v => v.Relevant > 0).ToList();
            if (relevant.Count == 0) return NodePresence.Irrelevant();
            return new NodePresence(relevant.Sum(v => v.Relevant),
                relevant.Any(v => v.Present), relevant.All(v => v.Complete),
                relevant.SelectMany(v => v.Markers));
        }

        private static NodePresence AnyOf(IEnumerable<NodePresence> values)
        {
            var relevant = values.Where(v => v.Relevant > 0).ToList();
            if (relevant.Count == 0) return NodePresence.Irrelevant();
            return new NodePresence(relevant.Sum(v => v.Relevant),
                relevant.Any(v => v.Present), relevant.Any(v => v.Complete),
                relevant.SelectMany(v => v.Markers));
        }

        private sealed class NodePresence
        {
            internal NodePresence(int relevant, bool present, bool complete, IEnumerable<string> markers)
            {
                Relevant = relevant;
                Present = present;
                Complete = complete;
                Markers = markers.ToArray();
            }

            internal int Relevant;
            internal bool Present;
            internal bool Complete;
            internal string[] Markers;
            internal static NodePresence Irrelevant()
            {
                return new NodePresence(0, false, false, new string[0]);
            }
        }
    }
}
