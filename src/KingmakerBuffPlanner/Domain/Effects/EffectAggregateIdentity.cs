using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace KingmakerBuffPlanner.Domain.Effects
{
    public static class EffectAggregateIdentity
    {
        private const string Prefix = "effect|";

        public static string For(EffectExpression expression, string unresolvedSourceId)
        {
            if (expression == null || !EffectExpressionAnalysis.ContainsLeaf(expression))
                return unresolvedSourceId ?? string.Empty;
            string semantic = Describe(expression);
            using (var hash = SHA256.Create())
                return Prefix + ToHex(hash.ComputeHash(Encoding.UTF8.GetBytes(semantic)));
        }

        public static bool IsAggregate(string sourceId)
        {
            return !string.IsNullOrEmpty(sourceId) &&
                sourceId.StartsWith(Prefix, StringComparison.Ordinal);
        }

        internal static string Describe(EffectExpression expression)
        {
            var empty = expression as EmptyEffectExpression;
            if (empty != null) return "empty";
            var leaf = expression as EffectLeafExpression;
            if (leaf != null)
                return "leaf(" + ((int)leaf.Kind).ToString(CultureInfo.InvariantCulture) + "," +
                    Escape(leaf.EffectId) + "," +
                    ((int)leaf.Target).ToString(CultureInfo.InvariantCulture) + ")";
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                var value = new StringBuilder("sequence[");
                for (int index = 0; index < sequence.Children.Count; index++)
                {
                    if (index != 0) value.Append(';');
                    value.Append(Describe(sequence.Children[index]));
                }
                return value.Append(']').ToString();
            }
            var conditional = expression as ConditionalEffectExpression;
            // Greater Magic Fang repair (review): which tier a caster-level
            // selection applies depends on the caster, and catalogue identity
            // excludes the caster. The selection is described by its tiers in
            // effect-id order - also how earlier versions described Magic
            // Fang's tiers (one sequence of every element, sorted by GUID) - so
            // saved spell identities keep matching.
            var tiers = new List<EffectLeafExpression>();
            if (conditional != null && CasterLevelTiers(conditional, tiers))
                return "sequence[" + string.Join(";", tiers.OrderBy(tier => tier.EffectId, StringComparer.Ordinal)
                    .Select(tier => Describe(tier)).ToArray()) + "]";
            if (conditional != null)
                return "conditional(" + Escape(conditional.ConditionContract) + "," +
                    Describe(conditional.WhenTrue) + "," + Describe(conditional.WhenFalse) + ")";
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null)
                return "targeted(" + ((int)targeted.Target).ToString(CultureInfo.InvariantCulture) +
                    "," + Describe(targeted.Child) + ")";
            var referenced = expression as ReferencedAbilityExpression;
            if (referenced != null) return "ability-reference(" + Describe(referenced.Child) + ")";
            throw new InvalidOperationException("Unknown effect expression type: " +
                expression.GetType().FullName);
        }

        // A chain of conditionals gated only by caster-level predicates whose
        // branches are leaves or further such conditionals: its leaves.
        private static bool CasterLevelTiers(EffectExpression expression, List<EffectLeafExpression> tiers)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) { tiers.Add(leaf); return true; }
            var conditional = expression as ConditionalEffectExpression;
            int level;
            return conditional != null && conditional.RecipientFactGate == null &&
                conditional.CasterFactGate != null && conditional.CasterFactGate.Facts.Count != 0 &&
                conditional.CasterFactGate.Facts.All(fact => CasterLevelPredicate.TryParse(fact.Key, out level)) &&
                CasterLevelTiers(conditional.WhenTrue, tiers) && CasterLevelTiers(conditional.WhenFalse, tiers);
        }

        private static string Escape(string value)
        {
            string text = value ?? string.Empty;
            return text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text;
        }

        private static string ToHex(byte[] bytes)
        {
            const string digits = "0123456789abcdef";
            var chars = new char[bytes.Length * 2];
            for (int index = 0; index < bytes.Length; index++)
            {
                chars[index * 2] = digits[bytes[index] >> 4];
                chars[(index * 2) + 1] = digits[bytes[index] & 15];
            }
            return new string(chars);
        }
    }
}
