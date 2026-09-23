using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using KingmakerBuffPlanner.Domain.Identity;

namespace KingmakerBuffPlanner.GameAdapters
{
    // Cantrips are cast at will through the ability the class grants for
    // each one (BindAbilitiesToClass): its data has no spellbook, the game
    // judges it available and its count is unlimited (-1). A spellbook's
    // own level-0 entry needs a level-0 slot, and these spellbooks have none
    // (spells per day 0 at level 0): the game's cast command refuses it
    // (live runs casting-qual-cast-20260923-a1-anim-01 and
    // casting-qual-select-20260923-d1-01).
    internal static class KingmakerAtWillCantrips
    {
        // The caster's at-will cantrip ability for the requested ability,
        // or null. Several matching abilities (one per class) resolve to
        // the one at the preferred caster level, else the first.
        internal static AbilityData Resolve(UnitEntityData caster, AbilityKey requested,
            int preferredCasterLevel)
        {
            if (caster == null || caster.Descriptor == null || requested == null) return null;
            var candidates = new List<AbilityData>();
            foreach (Ability fact in caster.Descriptor.Abilities.Enumerable)
            {
                if (fact == null || fact.Data == null || fact.Blueprint == null) continue;
                bool cantrip;
                try { cantrip = fact.Blueprint.IsCantrip; }
                catch (Exception) { cantrip = false; }
                if (!cantrip) continue;
                AbilityData match = KingmakerAbilityVariants.Resolve(fact.Data, requested);
                if (match != null && IsAtWill(match)) candidates.Add(match);
            }
            if (candidates.Count <= 1) return candidates.FirstOrDefault();
            return candidates.FirstOrDefault(value => CasterLevel(value) == preferredCasterLevel) ??
                candidates[0];
        }

        // The game's own judgement: no spellbook resource, available, and
        // an unlimited count.
        internal static bool IsAtWill(AbilityData data)
        {
            try
            {
                return data != null && data.Spellbook == null && data.IsAvailable &&
                    data.GetAvailableForCastCount() < 0;
            }
            catch (Exception) { return false; }
        }

        private static int CasterLevel(AbilityData data)
        {
            try { return data.CalculateParams().CasterLevel; }
            catch (Exception) { return -1; }
        }
    }
}
