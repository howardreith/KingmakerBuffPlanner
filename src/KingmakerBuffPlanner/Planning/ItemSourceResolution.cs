using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.Planning
{
    // Pure resolution of a saved item selection against the current providers.
    // Compiler, inspector and diagnostics share it so they cannot disagree.
    public static class ItemSourceResolution
    {
        public const string ReasonPrefix = "item-source-";
        public const string Missing = ReasonPrefix + "missing";
        public const string Ambiguous = ReasonPrefix + "ambiguous";
        public const string SlotEmpty = ReasonPrefix + "slot-empty";
        public const string LegacySession = ReasonPrefix + "legacy-session";
        public const string LegacyFact = ReasonPrefix + "legacy-fact";
        public const string Invalid = ReasonPrefix + "invalid";

        public static bool IsRefusal(string reason)
        {
            return reason != null && reason.StartsWith(ReasonPrefix, StringComparison.Ordinal);
        }

        // A slot-qualified selection is published under the plain rule once
        // the caster no longer has several copies. The rule still names that
        // slot, so its occupant satisfies it; another slot's copy never does.
        public static AbilityKey EffectiveAbility(AbilityKey saved, string casterUnitId,
            IEnumerable<ProviderSnapshot> providers)
        {
            if (saved == null || saved.SourceKind != SourceKind.Item ||
                ItemSourceSelector.Parse(saved.SpecialSourceId).Kind != ItemSelectionKind.EquippedSlot)
                return saved;
            List<ProviderSnapshot> own = OwnedBy(providers, casterUnitId);
            if (own.Any(provider => provider.Key.Ability.Equals(saved))) return saved;
            List<ProviderSnapshot> occupant = own.Where(provider => SameAbility(provider, saved) &&
                string.Equals(provider.EquippedSlotSelector, saved.SpecialSourceId,
                    StringComparison.Ordinal)).ToList();
            return occupant.Count == 1 ? occupant[0].Key.Ability : saved;
        }

        // Why a saved selection has no provider on its caster, or null when
        // this is not an item-selection problem.
        public static string Refusal(AbilityKey saved, string casterUnitId,
            IEnumerable<ProviderSnapshot> providers)
        {
            if (saved == null) return null;
            List<ProviderSnapshot> own = OwnedBy(providers, casterUnitId);
            if (saved.SourceKind == SourceKind.Fact)
                return own.Any(provider => provider.Key.Ability.SourceKind == SourceKind.Item &&
                        SameAbility(provider, saved))
                    ? LegacyFact + ":" + casterUnitId : null;
            if (saved.SourceKind != SourceKind.Item) return null;
            ItemSourceSelection selection = ItemSourceSelector.Parse(saved.SpecialSourceId);
            switch (selection.Kind)
            {
                case ItemSelectionKind.LegacySession:
                    return LegacySession + ":" + casterUnitId;
                case ItemSelectionKind.None:
                case ItemSelectionKind.Invalid:
                    return Invalid + ":" + casterUnitId;
            }
            List<ProviderSnapshot> sameType = own.Where(provider => SameAbility(provider, saved) &&
                string.Equals(ItemSourceSelector.Parse(provider.Key.Ability.SpecialSourceId).ItemBlueprintGuid,
                    selection.ItemBlueprintGuid, StringComparison.Ordinal)).ToList();
            if (selection.Kind == ItemSelectionKind.EquippedType)
                // Any live same-type source here is published under the slot
                // rule, which discovery only does when copies are identical.
                return sameType.Count != 0 ? Ambiguous + ":" + sameType.Count : Missing + ":" + casterUnitId;
            return sameType.Count == 0 ? Missing + ":" + casterUnitId : SlotEmpty + ":" + selection.SlotId;
        }

        public static bool IsItemRelated(ResolvedCasting casting)
        {
            return casting != null && (casting.Ability.SourceKind == SourceKind.Item ||
                casting.ReadinessReasons.Any(IsRefusal));
        }

        // One concise record of a resolution; contains no per-frame state, so
        // an unchanged result produces an identical string.
        public static string Describe(ResolvedCasting casting, IEnumerable<ProviderSnapshot> providers)
        {
            string refusal = casting.ReadinessReasons.FirstOrDefault(IsRefusal);
            IEnumerable<string> candidates = OwnedBy(providers, casting.CasterUnitId)
                .Where(provider => provider.Key.Ability.SourceKind == SourceKind.Item &&
                    SameAbility(provider, casting.Ability))
                .Select(provider => provider.Key.Ability.SpecialSourceId + "~" + provider.Key.SourceInstanceId);
            return "casting=" + casting.CastingId + ";caster=" + (casting.CasterUnitId ?? "none") +
                ";saved=" + casting.Ability.SourceKind + ":" + casting.Ability.SpecialSourceId +
                ";outcome=" + (refusal ?? (casting.Provider == null
                    ? "unresolved" : "resolved:" + casting.Provider.SourceInstanceId)) +
                ";candidates=[" + string.Join(",", candidates.OrderBy(value => value,
                    StringComparer.Ordinal).ToArray()) + "]";
        }

        private static List<ProviderSnapshot> OwnedBy(IEnumerable<ProviderSnapshot> providers, string casterUnitId)
        {
            return (providers ?? new ProviderSnapshot[0]).Where(provider => provider != null &&
                string.Equals(provider.Key.CasterUnitId, casterUnitId, StringComparison.Ordinal)).ToList();
        }

        private static bool SameAbility(ProviderSnapshot provider, AbilityKey saved)
        {
            AbilityKey live = provider.Key.Ability;
            return string.Equals(live.BaseAbilityGuid, saved.BaseAbilityGuid, StringComparison.Ordinal) &&
                string.Equals(live.VariantGuid, saved.VariantGuid, StringComparison.Ordinal) &&
                live.MetamagicMask == saved.MetamagicMask;
        }
    }
}
