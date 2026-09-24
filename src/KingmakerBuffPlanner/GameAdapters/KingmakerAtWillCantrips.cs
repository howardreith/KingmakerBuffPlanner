using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Execution;

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
        // The caster's at-will cantrip ability for the requested ability, by
        // the Unity-free rule AtWillCantripChoice; null with no refusal when
        // none exists, null with a refusal when the choice is ambiguous.
        internal static AbilityData Resolve(UnitEntityData caster, AbilityKey requested,
            int preferredCasterLevel, out string refusal)
        {
            refusal = null;
            if (caster == null || caster.Descriptor == null || requested == null) return null;
            var candidates = new List<KeyValuePair<AtWillCantripCandidate, AbilityData>>();
            int index = 0;
            foreach (Ability fact in caster.Descriptor.Abilities.Enumerable)
            {
                index++;
                if (fact == null || fact.Data == null || fact.Blueprint == null) continue;
                bool cantrip;
                try { cantrip = fact.Blueprint.IsCantrip; }
                catch (Exception) { cantrip = false; }
                AbilityData match = cantrip ? KingmakerAbilityVariants.Resolve(fact.Data, requested) : null;
                AbilityData judged = match ?? fact.Data;
                candidates.Add(new KeyValuePair<AtWillCantripCandidate, AbilityData>(
                    new AtWillCantripCandidate(fact.Blueprint.AssetGuid + "#" + index, cantrip, match != null,
                        SafeHasSpellbook(judged), SafeAvailable(judged), SafeCount(judged), CasterLevel(judged)),
                    match));
            }
            AtWillCantripCandidate chosen = AtWillCantripChoice.Choose(
                candidates.Select(pair => pair.Key), preferredCasterLevel, out refusal);
            return chosen == null ? null : candidates.First(pair => ReferenceEquals(pair.Key, chosen)).Value;
        }

        private static bool SafeHasSpellbook(AbilityData data)
        {
            try { return data.Spellbook != null; }
            catch (Exception) { return true; }
        }

        private static bool SafeAvailable(AbilityData data)
        {
            try { return data.IsAvailable; }
            catch (Exception) { return false; }
        }

        private static int SafeCount(AbilityData data)
        {
            try { return data.GetAvailableForCastCount(); }
            catch (Exception) { return 0; }
        }

        private static int CasterLevel(AbilityData data)
        {
            try { return data.CalculateParams().CasterLevel; }
            catch (Exception) { return -1; }
        }
    }
}
