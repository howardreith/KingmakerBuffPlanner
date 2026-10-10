using System;
using KingmakerBuffPlanner.Domain.Identity;

namespace KingmakerBuffPlanner.Domain.Effects
{
    public static class CatalogSourceIdentity
    {
        private const string VariantPrefix = "variant|";
        private const string AbilityPrefix = "ability|";

        // Spells keep the effect aggregate (one spell in several books is
        // one entry). 0.4.2: an ability owned through a class feature or
        // fact (a ki power, a bloodline or domain power) is its own entry,
        // keyed by its own blueprint. It is never pooled with a spell, or
        // another ability, merely because the effect matches: Call of the
        // Wild's Shadow Clone applies Mirror Image's buff, and under the
        // shared effect identity it served no entry at all.
        public static string For(AbilityKey ability, EffectExpression expression)
        {
            if (ability == null) throw new ArgumentNullException("ability");
            if (!string.IsNullOrWhiteSpace(ability.VariantGuid))
                return VariantPrefix + ability.BaseAbilityGuid + "|" + ability.VariantGuid;
            if (ability.SourceKind != SourceKind.Spellbook)
                return AbilityPrefix + ability.BaseAbilityGuid;
            return EffectAggregateIdentity.For(expression, ability.Canonical);
        }

        public static bool IsVariant(string sourceId)
        {
            return !string.IsNullOrWhiteSpace(sourceId) &&
                sourceId.StartsWith(VariantPrefix, StringComparison.Ordinal);
        }

        public static bool IsAbility(string sourceId)
        {
            return !string.IsNullOrWhiteSpace(sourceId) &&
                sourceId.StartsWith(AbilityPrefix, StringComparison.Ordinal);
        }

        public static bool MatchesVariant(string sourceId, AbilityKey ability)
        {
            if (ability == null || !IsVariant(sourceId)) return false;
            string[] parts = sourceId.Split('|');
            return parts.Length == 3 && parts[1].Length != 0 && parts[2].Length != 0 &&
                string.Equals(parts[1], ability.BaseAbilityGuid, StringComparison.Ordinal) &&
                string.Equals(parts[2], ability.VariantGuid, StringComparison.Ordinal);
        }

        public static bool MatchesAbility(string sourceId, AbilityKey ability)
        {
            return ability != null && IsAbility(sourceId) &&
                string.IsNullOrWhiteSpace(ability.VariantGuid) &&
                ability.SourceKind != SourceKind.Spellbook &&
                string.Equals(sourceId, AbilityPrefix + ability.BaseAbilityGuid,
                    StringComparison.Ordinal);
        }

        // The identity a saved casting's own ability has under the current
        // rule when it can be derived without discovery (a class/fact
        // ability or a concrete variant); null for a spell, whose effect
        // aggregate needs the discovered expression.
        public static string DerivableFor(AbilityKey ability)
        {
            if (ability == null) return null;
            if (!string.IsNullOrWhiteSpace(ability.VariantGuid))
                return VariantPrefix + ability.BaseAbilityGuid + "|" + ability.VariantGuid;
            return ability.SourceKind == SourceKind.Spellbook
                ? null : AbilityPrefix + ability.BaseAbilityGuid;
        }
    }
}
