using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.Execution
{
    // One instance of an expected effect on a recipient, as read for
    // confirmation: its kind and effect id, a process-local instance
    // identity, its end time (ticks) and whether the game suppresses it.
    public sealed class ObservedEffectInstance
    {
        public ObservedEffectInstance(EffectKind kind, string effectId, string instanceKey,
            long endTimeTicks, bool suppressed)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                throw new ArgumentException("Effect ID is required.", "effectId");
            Kind = kind;
            EffectId = effectId;
            InstanceKey = instanceKey ?? string.Empty;
            EndTimeTicks = endTimeTicks;
            Suppressed = suppressed;
        }

        public EffectKind Kind { get; private set; }
        public string EffectId { get; private set; }
        public string InstanceKey { get; private set; }
        public long EndTimeTicks { get; private set; }
        public bool Suppressed { get; private set; }

        internal string Identity
        {
            get { return Kind + ":" + EffectId + "#" + InstanceKey; }
        }
    }

    // Each expected recipient's instances of the expected effects, read
    // before the cast was submitted.
    public sealed class EffectBaseline
    {
        private readonly Dictionary<string, IReadOnlyList<ObservedEffectInstance>> _byUnit =
            new Dictionary<string, IReadOnlyList<ObservedEffectInstance>>(StringComparer.Ordinal);

        public EffectBaseline(IDictionary<string, IEnumerable<ObservedEffectInstance>> byUnit)
        {
            foreach (KeyValuePair<string, IEnumerable<ObservedEffectInstance>> pair in byUnit ??
                new Dictionary<string, IEnumerable<ObservedEffectInstance>>())
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null) continue;
                _byUnit[pair.Key] = new ReadOnlyCollection<ObservedEffectInstance>(
                    pair.Value.Where(value => value != null).ToList());
            }
        }

        public bool TryGet(string unitId, out IReadOnlyList<ObservedEffectInstance> instances)
        {
            instances = null;
            return unitId != null && _byUnit.TryGetValue(unitId, out instances);
        }
    }

    // Final review A3: an expected effect confirms a cast only when this
    // attempt put it there. Presence alone is no proof: a recast over an
    // insufficient, suppressed or deliberately recast instance would find
    // the old one and confirm a cast that delivered nothing. A recipient is
    // reached only when the expected effects are complete over instances
    // that are not suppressed and at least one of those is new (an identity
    // not read before submission) or refreshed (the same identity with a
    // later end) - the probe's own rule. A recipient without a read before
    // submission, or unreadable after it, is not reached, and an empty
    // recipient set confirms nothing (final review A2).
    public static class AppliedEffectJudgement
    {
        public static bool Reached(EffectExpression expected,
            IEnumerable<ObservedEffectInstance> before,
            IEnumerable<ObservedEffectInstance> after)
        {
            if (expected == null || before == null || after == null) return false;
            List<ObservedEffectInstance> live = after
                .Where(value => value != null && !value.Suppressed).ToList();
            var markers = new HashSet<ActiveEffectMarker>(live.Select(value =>
                new ActiveEffectMarker(value.Kind, value.EffectId)));
            if (new EffectPresenceEvaluator().EvaluateTyped(expected, markers, null).Kind !=
                    EffectPresenceKind.Complete)
                return false;
            var ends = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (ObservedEffectInstance prior in before.Where(value => value != null))
            {
                long end;
                if (!ends.TryGetValue(prior.Identity, out end) || prior.EndTimeTicks > end)
                    ends[prior.Identity] = prior.EndTimeTicks;
            }
            // Re-review: the new or refreshed instance must be of an expected
            // kind as well as an expected effect (an area instance of the same
            // buff does not show that the buff itself landed).
            var leaves = new HashSet<string>(StringComparer.Ordinal);
            CollectLeafMarkers(expected, leaves);
            return live.Any(value =>
            {
                if (!leaves.Contains(value.Kind + ":" + value.EffectId)) return false;
                long end;
                return !ends.TryGetValue(value.Identity, out end) || value.EndTimeTicks > end;
            });
        }

        private static void CollectLeafMarkers(EffectExpression expression, ISet<string> leaves)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) { leaves.Add(leaf.Kind + ":" + leaf.EffectId); return; }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                foreach (EffectExpression child in sequence.Children) CollectLeafMarkers(child, leaves);
                return;
            }
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                CollectLeafMarkers(conditional.WhenTrue, leaves);
                CollectLeafMarkers(conditional.WhenFalse, leaves);
                return;
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) { CollectLeafMarkers(targeted.Child, leaves); return; }
            var referenced = expression as ReferencedAbilityExpression;
            if (referenced != null) CollectLeafMarkers(referenced.Child, leaves);
        }

        public static bool AllReached(IReadOnlyList<string> recipients, EffectExpression expected,
            EffectBaseline baseline, Func<string, IEnumerable<ObservedEffectInstance>> readAfter)
        {
            if (recipients == null || recipients.Count == 0 || baseline == null || readAfter == null)
                return false;
            foreach (string unitId in recipients)
            {
                IReadOnlyList<ObservedEffectInstance> before;
                if (!baseline.TryGet(unitId, out before)) return false;
                if (!Reached(expected, before, readAfter(unitId))) return false;
            }
            return true;
        }
    }
}
