using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Items;
using Kingmaker.Items.Slots;

namespace KingmakerBuffPlanner.GameAdapters
{
    // Greater Magic Fang repair: the items whose enchantments the planner
    // observes on a unit (existing effects and cast confirmation alike).
    // Native 2.1.7b MagicFang.RunAction (pinned in the regression) enchants
    // Body.EmptyHandWeapon, the Body.PrimaryHand and Body.SecondaryHand
    // weapons when they are natural, and every natural weapon of
    // Body.AdditionalLimbs. While polymorphed, those hand getters return the
    // polymorph hand set and the limbs the polymorph limbs.
    // Body.CurrentEquipmentSlots holds only the ordinary hand set and the
    // worn-equipment slots, and an empty hand's MaybeItem is null, so the
    // native hands, the empty-hand weapon and the limb weapons are read too.
    // An item reached more than once is observed once.
    internal static class KingmakerEnchantedItems
    {
        internal static IReadOnlyList<ItemEntity> Read(UnitBody body)
        {
            if (body == null) return new ItemEntity[0];
            var slots = body.CurrentEquipmentSlots
                .Select(slot => slot == null ? null : slot.MaybeItem).ToList();
            ItemEntity emptyHand = null;
            var natural = new List<ItemEntity>();
            // An unreadable extra source is not observed; what it would show
            // then never confirms or satisfies anything.
            try { emptyHand = body.EmptyHandWeapon; }
            catch (Exception) { emptyHand = null; }
            try
            {
                foreach (HandSlot hand in new[] { body.PrimaryHand, body.SecondaryHand })
                    natural.Add(hand == null ? null : hand.MaybeItem);
            }
            catch (Exception) { /* the ordinary hand set is already among the slots */ }
            try
            {
                List<WeaponSlot> additional = body.AdditionalLimbs;
                if (additional != null) natural.AddRange(additional.Select(slot => slot == null ? null : slot.MaybeItem));
            }
            catch (Exception) { /* limbs unread */ }
            return Distinct(slots, emptyHand, natural);
        }

        // The slot items, then the empty-hand weapon, then the native hand and
        // limb items, without nulls and with each instance once (reference
        // identity).
        internal static IReadOnlyList<T> Distinct<T>(IEnumerable<T> slotItems, T emptyHand,
            IEnumerable<T> naturalItems) where T : class
        {
            var seen = new HashSet<T>(Discovery.ReferenceEqualityComparer<T>.Instance);
            var items = new List<T>();
            foreach (T item in (slotItems ?? new T[0]).Concat(new[] { emptyHand }).Concat(naturalItems ?? new T[0]))
                if (item != null && seen.Add(item)) items.Add(item);
            return items;
        }
    }
}
