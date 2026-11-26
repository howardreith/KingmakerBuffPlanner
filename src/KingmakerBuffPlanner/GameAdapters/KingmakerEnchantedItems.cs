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
    // Body.EmptyHandWeapon, the primary and secondary hand weapons when they
    // are natural, and every natural weapon of Body.AdditionalLimbs (the
    // polymorph limbs while polymorphed). Body.CurrentEquipmentSlots holds
    // only the hand and worn-equipment slots, and an empty hand's MaybeItem
    // is null, so the empty-hand weapon and the limb weapons are read too.
    // An item reached more than once is observed once.
    internal static class KingmakerEnchantedItems
    {
        internal static IReadOnlyList<ItemEntity> Read(UnitBody body)
        {
            if (body == null) return new ItemEntity[0];
            IEnumerable<ItemEntity> slots = body.CurrentEquipmentSlots
                .Select(slot => slot == null ? null : slot.MaybeItem).ToList();
            ItemEntity emptyHand = null;
            List<ItemEntity> limbs = null;
            // An unreadable extra source is not observed; what it would show
            // then never confirms or satisfies anything.
            try { emptyHand = body.EmptyHandWeapon; }
            catch (Exception) { emptyHand = null; }
            try
            {
                List<WeaponSlot> additional = body.AdditionalLimbs;
                limbs = additional == null ? null
                    : additional.Select(slot => slot == null ? null : slot.MaybeItem).ToList();
            }
            catch (Exception) { limbs = null; }
            return Distinct(slots, emptyHand, limbs);
        }

        // The slot items, then the empty-hand weapon, then the limb items,
        // without nulls and with each instance once (reference identity).
        internal static IReadOnlyList<T> Distinct<T>(IEnumerable<T> slotItems, T emptyHand,
            IEnumerable<T> limbItems) where T : class
        {
            var seen = new HashSet<T>(Discovery.ReferenceEqualityComparer<T>.Instance);
            var items = new List<T>();
            foreach (T item in (slotItems ?? new T[0]).Concat(new[] { emptyHand }).Concat(limbItems ?? new T[0]))
                if (item != null && seen.Add(item)) items.Add(item);
            return items;
        }
    }
}
