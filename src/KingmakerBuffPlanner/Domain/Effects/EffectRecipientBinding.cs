using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.Domain.Effects
{
    // Optional, verified action-wrapper metadata. It does not change saved
    // catalogue identity. Concrete owners DO enter the execution projection.
    public sealed class NearbyRecipientRule
    {
        public NearbyRecipientRule(float radius, int maximum, bool self, bool friends, bool enemies)
        { Radius = radius; Maximum = maximum; Self = self; Friends = friends; Enemies = enemies; }
        [JsonProperty("radius")] public float Radius { get; private set; }
        [JsonProperty("maximum")] public int Maximum { get; private set; }
        [JsonProperty("self")] public bool Self { get; private set; }
        [JsonProperty("friends")] public bool Friends { get; private set; }
        [JsonProperty("enemies")] public bool Enemies { get; private set; }
    }

    public static class EffectRecipientBinding
    {
        public const string UnresolvedSecondaryRecipient = "unresolved-secondary-recipient";
        public static bool ContainsNearby(EffectExpression expression)
        {
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) return sequence.NearbyRecipients != null || sequence.Children.Any(ContainsNearby);
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null) return ContainsNearby(conditional.WhenTrue) || ContainsNearby(conditional.WhenFalse);
            var reference = expression as ReferencedAbilityExpression;
            if (reference != null) return ContainsNearby(reference.Child);
            var targeted = expression as TargetedEffectExpression;
            return targeted != null && ContainsNearby(targeted.Child);
        }

        public static EffectExpression Bind(EffectExpression expression,
            Func<NearbyRecipientRule, IReadOnlyList<string>> select, string currentOwner = null)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) return currentOwner == null || leaf.Target != EffectTarget.CurrentTarget ? expression :
                new EffectLeafExpression(leaf.Kind, leaf.EffectId, leaf.Target, leaf.SourceContract, leaf.ActionPath, currentOwner);
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                if (sequence.NearbyRecipients != null)
                {
                    IReadOnlyList<string> owners = select(sequence.NearbyRecipients);
                    // Unknown ownership must never be accepted as primary-target coverage.
                    if (owners == null) owners = new[] { UnresolvedSecondaryRecipient };
                    return new SequenceEffectExpression(owners.SelectMany(owner => sequence.Children.Select(child => Bind(child, select, owner))));
                }
                var children = sequence.Children.Select(child => Bind(child, select, currentOwner)).ToList();
                return children.SequenceEqual(sequence.Children) ? expression : new SequenceEffectExpression(children);
            }
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                EffectExpression yes = Bind(conditional.WhenTrue, select, currentOwner), no = Bind(conditional.WhenFalse, select, currentOwner);
                return ReferenceEquals(yes, conditional.WhenTrue) && ReferenceEquals(no, conditional.WhenFalse) ? expression :
                    new ConditionalEffectExpression(conditional.ConditionContract, yes, no, conditional.CasterFactGate);
            }
            var reference = expression as ReferencedAbilityExpression;
            if (reference != null)
            {
                EffectExpression child = Bind(reference.Child, select, currentOwner);
                return ReferenceEquals(child, reference.Child) ? expression : new ReferencedAbilityExpression(reference.AbilityId, child);
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null)
            {
                EffectExpression child = Bind(targeted.Child, select, targeted.Target == EffectTarget.CurrentTarget ? currentOwner : null);
                return ReferenceEquals(child, targeted.Child) ? expression : new TargetedEffectExpression(targeted.Target, child);
            }
            return expression;
        }
    }
}
