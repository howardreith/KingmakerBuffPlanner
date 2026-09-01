using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.Domain.Planning
{
    public enum CastEnhancementCategory
    {
        MetamagicRod,
        ClassFeature
    }

    internal static class CastEnhancementActivationPolicy
    {
        internal static bool RestoreOriginalState(bool oneShot,
            bool oneShotGroupConsumed)
        {
            return !oneShot || !oneShotGroupConsumed;
        }

        internal static bool RestoreOriginalState(bool oneShot,
            string activationGroupId, ISet<string> consumedGroups)
        {
            return RestoreOriginalState(oneShot, consumedGroups != null &&
                consumedGroups.Contains(activationGroupId ?? string.Empty));
        }
    }

    public sealed class CastEnhancementSnapshot
    {
        public CastEnhancementSnapshot(
            string enhancementId,
            string casterUnitId,
            string sourceBlueprintGuid,
            string displayName,
            string description,
            CastEnhancementCategory category,
            int metamagicMask,
            int maximumSpellLevel,
            int? remainingUses,
            IEnumerable<string> abilityWhiteList,
            string effectDisplayName = null,
            IEnumerable<string> spellbookWhiteList = null,
            string usagePoolId = null,
            bool requiresNativeCommand = false,
            string exclusiveGroupId = null,
            int usageUnitsPerCast = 1,
            bool affectsTargeting = false,
            string nativeActivationGroupId = null,
            string usagePoolDisplayName = null)
        {
            if (string.IsNullOrWhiteSpace(enhancementId)) throw new ArgumentException("Enhancement ID is required.", "enhancementId");
            if (string.IsNullOrWhiteSpace(casterUnitId)) throw new ArgumentException("Caster unit ID is required.", "casterUnitId");
            if (string.IsNullOrWhiteSpace(sourceBlueprintGuid)) throw new ArgumentException("Source blueprint GUID is required.", "sourceBlueprintGuid");
            if (metamagicMask < 0) throw new ArgumentOutOfRangeException("metamagicMask");
            if (maximumSpellLevel < 0) throw new ArgumentOutOfRangeException("maximumSpellLevel");
            if (remainingUses != null && remainingUses.Value < 0) throw new ArgumentOutOfRangeException("remainingUses");
            if (usageUnitsPerCast < 1) throw new ArgumentOutOfRangeException("usageUnitsPerCast");
            EnhancementId = enhancementId;
            CasterUnitId = casterUnitId;
            SourceBlueprintGuid = sourceBlueprintGuid;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Unnamed enhancement" : displayName;
            Description = description ?? string.Empty;
            Category = category;
            MetamagicMask = metamagicMask;
            MaximumSpellLevel = maximumSpellLevel;
            RemainingUses = remainingUses;
            EffectDisplayName = string.IsNullOrWhiteSpace(effectDisplayName)
                ? (category == CastEnhancementCategory.MetamagicRod
                    ? "Metamagic" : "Class feature") : effectDisplayName;
            AbilityWhiteList = new ReadOnlyCollection<string>((abilityWhiteList ?? new string[0])
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
            SpellbookWhiteList = new ReadOnlyCollection<string>((spellbookWhiteList ??
                    new string[0]).Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToList());
            UsagePoolId = string.IsNullOrWhiteSpace(usagePoolId)
                ? enhancementId : usagePoolId;
            RequiresNativeCommand = requiresNativeCommand;
            ExclusiveGroupId = string.IsNullOrWhiteSpace(exclusiveGroupId)
                ? (category == CastEnhancementCategory.MetamagicRod
                    ? "metamagic-rod" : "class-feature")
                : exclusiveGroupId;
            UsageUnitsPerCast = usageUnitsPerCast;
            AffectsTargeting = affectsTargeting;
            NativeActivationGroupId = string.IsNullOrWhiteSpace(
                nativeActivationGroupId) ? ExclusiveGroupId :
                nativeActivationGroupId;
            UsagePoolDisplayName = string.IsNullOrWhiteSpace(
                usagePoolDisplayName) ? "Uses" : usagePoolDisplayName;
        }

        public string EnhancementId { get; private set; }
        public string CasterUnitId { get; private set; }
        public string SourceBlueprintGuid { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public CastEnhancementCategory Category { get; private set; }
        public int MetamagicMask { get; private set; }
        public int MaximumSpellLevel { get; private set; }
        public int? RemainingUses { get; private set; }
        public string EffectDisplayName { get; private set; }
        public IReadOnlyList<string> AbilityWhiteList { get; private set; }
        public IReadOnlyList<string> SpellbookWhiteList { get; private set; }
        public string UsagePoolId { get; private set; }
        public bool RequiresNativeCommand { get; private set; }
        public string ExclusiveGroupId { get; private set; }
        public int UsageUnitsPerCast { get; private set; }
        public bool AffectsTargeting { get; private set; }
        public string NativeActivationGroupId { get; private set; }
        public string UsagePoolDisplayName { get; private set; }

        public bool IsApplicable(ProviderSnapshot provider)
        {
            return string.IsNullOrEmpty(ApplicabilityFailure(provider));
        }

        public bool IsApplicable(Domain.Identity.ProviderKey provider, int spellLevel)
        {
            return string.IsNullOrEmpty(ApplicabilityFailure(provider, spellLevel));
        }

        public string ApplicabilityFailure(ProviderSnapshot provider)
        {
            return provider == null ? "provider-missing" :
                ApplicabilityFailure(provider.Key, provider.SpellLevel);
        }

        public string ApplicabilityFailure(
            Domain.Identity.ProviderKey provider, int spellLevel)
        {
            if (provider == null) return "provider-missing";
            if (!string.Equals(provider.CasterUnitId, CasterUnitId,
                    StringComparison.Ordinal)) return "caster-mismatch";
            if (provider.Ability.SourceKind !=
                Domain.Identity.SourceKind.Spellbook)
                return "source-not-spellbook";
            if (Category == CastEnhancementCategory.ClassFeature)
            {
                if (!SpellbookWhiteList.Contains(provider.SpellbookGuid))
                    return "spellbook-not-qualified";
                string selectedAbilityGuid = string.IsNullOrWhiteSpace(
                    provider.Ability.VariantGuid)
                    ? provider.Ability.BaseAbilityGuid
                    : provider.Ability.VariantGuid;
                return AbilityWhiteList.Contains(selectedAbilityGuid)
                    ? string.Empty : "ability-not-qualified";
            }
            if (Category != CastEnhancementCategory.MetamagicRod)
                return "category-unsupported";
            if ((provider.Ability.MetamagicMask & MetamagicMask) != 0)
                return "metamagic-already-applied";
            if (AbilityWhiteList.Contains(provider.Ability.BaseAbilityGuid) ||
                AbilityWhiteList.Contains(provider.Ability.VariantGuid))
                return string.Empty;
            return spellLevel <= MaximumSpellLevel
                ? string.Empty : "spell-level-exceeds-limit";
        }

        public static bool AreCompatible(IEnumerable<CastEnhancementSnapshot> enhancements)
        {
            List<CastEnhancementSnapshot> values = (enhancements ??
                new CastEnhancementSnapshot[0]).ToList();
            return values.All(value => value != null) &&
                values.Select(value => value.EnhancementId)
                    .Distinct(StringComparer.Ordinal).Count() == values.Count &&
                values.Select(value => value.ExclusiveGroupId)
                    .Distinct(StringComparer.Ordinal).Count() == values.Count;
        }

        public static IReadOnlyDictionary<string, int> UsageRequirements(
            IEnumerable<CastEnhancementSnapshot> enhancements)
        {
            List<CastEnhancementSnapshot> values = (enhancements ??
                new CastEnhancementSnapshot[0]).ToList();
            if (values.Any(value => value == null))
                throw new ArgumentException("Enhancement selection contains null.",
                    "enhancements");
            return new ReadOnlyDictionary<string, int>(values
                .GroupBy(value => value.UsagePoolId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key,
                    group => group.Sum(value => value.UsageUnitsPerCast),
                    StringComparer.Ordinal));
        }
    }
}
