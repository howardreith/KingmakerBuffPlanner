using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using KingmakerBuffPlanner.Domain.Effects;

namespace KingmakerBuffPlanner.Discovery
{
    public enum DiscoveryNodeKind
    {
        Empty,
        Sequence,
        Conditional,
        TargetTransform,
        Effect,
        OffensiveAction,
        RestorativeAction,
        AbilityReference,
        Unknown
    }

    public sealed class DiscoveryNode
    {
        public DiscoveryNode(
            DiscoveryNodeKind kind,
            string identity,
            IEnumerable<DiscoveryNode> children = null,
            DiscoveryNode whenTrue = null,
            DiscoveryNode whenFalse = null,
            string conditionContract = null,
            EffectKind effectKind = EffectKind.Buff,
            string effectId = null,
            EffectTarget target = EffectTarget.CurrentTarget,
            string sourceContract = null,
            string referencedAbilityId = null,
            CasterFactGate casterFactGate = null,
            NearbyRecipientRule nearbyRecipients = null,
            float? maximumDurationSeconds = null)
        {
            Kind = kind;
            Identity = identity ?? string.Empty;
            Children = new ReadOnlyCollection<DiscoveryNode>(
                new List<DiscoveryNode>(children ?? new DiscoveryNode[0]));
            WhenTrue = whenTrue;
            WhenFalse = whenFalse;
            ConditionContract = conditionContract ?? string.Empty;
            EffectKind = effectKind;
            EffectId = effectId ?? string.Empty;
            Target = target;
            SourceContract = sourceContract ?? string.Empty;
            ReferencedAbilityId = referencedAbilityId ?? string.Empty;
            CasterFactGate = casterFactGate;
            NearbyRecipients = nearbyRecipients;
            MaximumDurationSeconds = maximumDurationSeconds;
        }

        public DiscoveryNodeKind Kind { get; private set; }
        public string Identity { get; private set; }
        public IReadOnlyList<DiscoveryNode> Children { get; private set; }
        public DiscoveryNode WhenTrue { get; private set; }
        public DiscoveryNode WhenFalse { get; private set; }
        public string ConditionContract { get; private set; }
        public EffectKind EffectKind { get; private set; }
        public string EffectId { get; private set; }
        public EffectTarget Target { get; private set; }
        public string SourceContract { get; private set; }
        public string ReferencedAbilityId { get; private set; }
        public CasterFactGate CasterFactGate { get; private set; }
        public NearbyRecipientRule NearbyRecipients { get; private set; }
        // Effect nodes: the longest native lifetime of the application when
        // it is structurally fixed (see EffectApplicationLifetime).
        public float? MaximumDurationSeconds { get; private set; }
    }
}
