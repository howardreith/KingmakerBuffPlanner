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

        // Legacy prediction-only binding: the predicted owners are the only
        // candidates (no alternative native selection is authorized).
        public static EffectExpression Bind(EffectExpression expression,
            Func<NearbyRecipientRule, IReadOnlyList<string>> select, string currentOwner = null)
        {
            return Bind(expression, rule =>
            {
                IReadOnlyList<string> owners = select(rule);
                return owners == null ? null : new NearbyRecipientSelection(owners, owners);
            }, currentOwner, null);
        }

        // H2 (0.6.11 review): the planner PREDICTS nearby owners for preflight
        // and display, but the native wrapper chooses when its action runs.
        // Predicted owners are bound as before; each bound leaf also carries
        // the authorized candidate set (every observable unit the rule could
        // legitimately select), so confirmation can follow the actual
        // application instead of a stale prediction.
        public static EffectExpression Bind(EffectExpression expression,
            Func<NearbyRecipientRule, NearbyRecipientSelection> select)
        {
            return Bind(expression, select, null, null);
        }

        private static EffectExpression Bind(EffectExpression expression,
            Func<NearbyRecipientRule, NearbyRecipientSelection> select, string currentOwner,
            NearbySelectionBinding secondary)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) return currentOwner == null || leaf.Target != EffectTarget.CurrentTarget ? expression :
                leaf.WithOwner(currentOwner, secondary);
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                if (sequence.NearbyRecipients != null)
                {
                    NearbyRecipientSelection selection = select(sequence.NearbyRecipients);
                    // Unknown ownership must never be accepted as primary-target coverage.
                    if (selection == null || selection.Predicted == null)
                        return new SequenceEffectExpression(sequence.Children.Select(child =>
                            Bind(child, select, UnresolvedSecondaryRecipient, null)));
                    var binding = new NearbySelectionBinding(sequence.NearbyRecipients.Radius,
                        sequence.NearbyRecipients.Maximum, selection.Predicted.Concat(selection.Candidates));
                    return new SequenceEffectExpression(selection.Predicted.SelectMany(owner =>
                        sequence.Children.Select(child => Bind(child, select, owner, binding))));
                }
                var children = sequence.Children.Select(child => Bind(child, select, currentOwner, secondary)).ToList();
                return children.SequenceEqual(sequence.Children) ? expression : new SequenceEffectExpression(children);
            }
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                EffectExpression yes = Bind(conditional.WhenTrue, select, currentOwner, secondary),
                    no = Bind(conditional.WhenFalse, select, currentOwner, secondary);
                return ReferenceEquals(yes, conditional.WhenTrue) && ReferenceEquals(no, conditional.WhenFalse) ? expression :
                    new ConditionalEffectExpression(conditional.ConditionContract, yes, no, conditional.CasterFactGate);
            }
            var reference = expression as ReferencedAbilityExpression;
            if (reference != null)
            {
                EffectExpression child = Bind(reference.Child, select, currentOwner, secondary);
                return ReferenceEquals(child, reference.Child) ? expression : new ReferencedAbilityExpression(reference.AbilityId, child);
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null)
            {
                bool current = targeted.Target == EffectTarget.CurrentTarget;
                EffectExpression child = Bind(targeted.Child, select, current ? currentOwner : null, current ? secondary : null);
                return ReferenceEquals(child, targeted.Child) ? expression : new TargetedEffectExpression(targeted.Target, child);
            }
            return expression;
        }
    }

    // A planning-time prediction plus the bounded set of observable units the
    // native rule may legitimately select instead. Predicted null: unresolved.
    public sealed class NearbyRecipientSelection
    {
        public NearbyRecipientSelection(IEnumerable<string> predicted, IEnumerable<string> candidates)
        {
            Predicted = predicted == null ? null : Clean(predicted);
            Candidates = Clean(candidates ?? new string[0]);
        }
        public IReadOnlyList<string> Predicted { get; private set; }
        public IReadOnlyList<string> Candidates { get; private set; }
        internal static IReadOnlyList<string> Clean(IEnumerable<string> values)
        {
            return values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToList().AsReadOnly();
        }
    }

    // Authorization for a natively selected secondary owner: the rule's
    // radius and count bound, and every unit that may be observed for it.
    // Part of the execution projection, not of catalogue identity.
    public sealed class NearbySelectionBinding
    {
        public NearbySelectionBinding(float radius, int maximum, IEnumerable<string> candidateUnitIds)
        {
            Radius = radius;
            Maximum = maximum;
            CandidateUnitIds = NearbyRecipientSelection.Clean(candidateUnitIds ?? new string[0]);
        }
        [JsonProperty("radius")] public float Radius { get; private set; }
        [JsonProperty("maximum")] public int Maximum { get; private set; }
        [JsonProperty("candidateUnitIds")] public IReadOnlyList<string> CandidateUnitIds { get; private set; }
    }
}
