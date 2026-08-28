using System;
using KingmakerBuffPlanner.Domain.Identity;

namespace KingmakerBuffPlanner.Domain.Effects
{
    public static class CatalogSourceIdentity
    {
        private const string VariantPrefix = "variant|";

        public static string For(AbilityKey ability, EffectExpression expression)
        {
            if (ability == null) throw new ArgumentNullException("ability");
            return string.IsNullOrWhiteSpace(ability.VariantGuid)
                ? EffectAggregateIdentity.For(expression, ability.Canonical)
                : VariantPrefix + ability.BaseAbilityGuid + "|" + ability.VariantGuid;
        }

        public static bool IsVariant(string sourceId)
        {
            return !string.IsNullOrWhiteSpace(sourceId) &&
                sourceId.StartsWith(VariantPrefix, StringComparison.Ordinal);
        }

        public static bool MatchesVariant(string sourceId, AbilityKey ability)
        {
            if (ability == null || !IsVariant(sourceId)) return false;
            string[] parts = sourceId.Split('|');
            return parts.Length == 3 && parts[1].Length != 0 && parts[2].Length != 0 &&
                string.Equals(parts[1], ability.BaseAbilityGuid, StringComparison.Ordinal) &&
                string.Equals(parts[2], ability.VariantGuid, StringComparison.Ordinal);
        }
    }
}
