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
            if (leaf != null)
            {
                if (leaf.Target != EffectTarget.CurrentTarget) return expression;
                // One natively selected obligation, whatever the prediction.
                if (secondary != null) return leaf.WithOwner(null, secondary);
                return currentOwner == null ? expression : leaf.WithOwner(currentOwner, null);
            }
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
                    // Review of PR #21: the obligation no longer depends on how
                    // many owners were predicted; an empty prediction keeps the
                    // requirement and its authorized candidates.
                    var binding = new NearbySelectionBinding(sequence.NearbyRecipients,
                        selection.Predicted, selection.Candidates);
                    return new SequenceEffectExpression(sequence.Children.Select(child =>
                        Bind(child, select, null, binding)));
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

    // A natively selected secondary obligation: the wrapper's rule, the
    // planning-time PREDICTION (preflight/display only, never trusted by
    // confirmation) and the authorized observable candidates. Part of the
    // execution projection and its authorization, not of catalogue identity.
    public sealed class NearbySelectionBinding
    {
        public NearbySelectionBinding(NearbyRecipientRule rule, IEnumerable<string> predicted,
            IEnumerable<string> candidates)
        {
            if (rule == null) throw new ArgumentNullException("rule");
            Radius = rule.Radius; Maximum = rule.Maximum;
            Self = rule.Self; Friends = rule.Friends; Enemies = rule.Enemies;
            PredictedUnitIds = NearbyRecipientSelection.Clean(predicted ?? new string[0]);
            CandidateUnitIds = NearbyRecipientSelection.Clean((candidates ?? new string[0]).Concat(PredictedUnitIds));
        }
        [JsonProperty("radius")] public float Radius { get; private set; }
        [JsonProperty("maximum")] public int Maximum { get; private set; }
        [JsonProperty("self")] public bool Self { get; private set; }
        [JsonProperty("friends")] public bool Friends { get; private set; }
        [JsonProperty("enemies")] public bool Enemies { get; private set; }
        [JsonProperty("predictedUnitIds")] public IReadOnlyList<string> PredictedUnitIds { get; private set; }
        // Every unit observed for this obligation (prediction included).
        [JsonProperty("candidateUnitIds")] public IReadOnlyList<string> CandidateUnitIds { get; private set; }

        // Groups application-boundary geometry taken with the same filter.
        [JsonIgnore]
        public string GeometryKey
        {
            get
            {
                return Radius.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "|" +
                    (Self ? "s" : "-") + (Friends ? "f" : "-") + (Enemies ? "e" : "-");
            }
        }
    }

    // One unit the native wrapper's own filter admitted around a selection
    // centre at one post-submission read, with the native distance.
    public sealed class NearbyEligibleUnit
    {
        public NearbyEligibleUnit(string unitId, float distance) { UnitId = unitId; Distance = distance; }
        public string UnitId { get; private set; }
        public float Distance { get; private set; }
    }
}
