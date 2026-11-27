using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Execution;

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
    //
    // PR #29 review: two reads, because "could not read" is not "read and
    // found nothing". Cast confirmation takes ReadComplete: an unreadable
    // source throws, so KingmakerEffectInstanceReader.ReadBaseline fails and
    // nothing is cast, and a later read that fails confirms nothing. A
    // baseline that silently omitted an existing enchantment would let that
    // unchanged enchantment, once readable, pass as freshly applied. The
    // planning view (KingmakerActiveEffectSnapshotBuilder) takes Read, which
    // omits an unreadable extra source: there an omitted enchantment can only
    // look absent, which makes the planner cast, never skip.
    internal static class KingmakerEnchantedItems
    {
        internal static IReadOnlyList<ItemEntity> Read(UnitBody body)
        {
            return Read(body, false);
        }

        internal static IReadOnlyList<ItemEntity> ReadComplete(UnitBody body)
        {
            return Read(body, true);
        }

        private static IReadOnlyList<ItemEntity> Read(UnitBody body, bool complete)
        {
            if (body == null)
            {
                if (complete) throw new InvalidOperationException("unit-body-unreadable");
                return new ItemEntity[0];
            }
            var slots = body.CurrentEquipmentSlots
                .Select(slot => slot == null ? null : slot.MaybeItem).ToList();
            ItemEntity emptyHand = null;
            var natural = new List<ItemEntity>();
            try { emptyHand = body.EmptyHandWeapon; }
            catch (Exception) when (!complete) { emptyHand = null; }
            try
            {
                foreach (HandSlot hand in new[] { body.PrimaryHand, body.SecondaryHand })
                    natural.Add(hand == null ? null : hand.MaybeItem);
            }
            catch (Exception) when (!complete) { /* the ordinary hand set is already among the slots */ }
            try
            {
                List<WeaponSlot> additional = body.AdditionalLimbs;
                if (additional != null) natural.AddRange(additional.Select(slot => slot == null ? null : slot.MaybeItem));
            }
            catch (Exception) when (!complete) { /* limbs unread */ }
            return Distinct(slots, emptyHand, natural);
        }

        // Cast confirmation's read (KingmakerEffectInstanceReader): the
        // unit's instances of the expected worn-item enchantments, from every
        // item the complete read observes. An unreadable item source throws.
        internal static List<ObservedEffectInstance> Instances(UnitBody body, HashSet<string> ids)
        {
            var instances = new List<ObservedEffectInstance>();
            foreach (ItemEntity item in ReadComplete(body))
            {
                if (item.Enchantments == null) continue;
                foreach (var enchantment in item.Enchantments)
                {
                    if (enchantment == null || enchantment.Blueprint == null ||
                        !ids.Contains(enchantment.Blueprint.AssetGuid)) continue;
                    instances.Add(new ObservedEffectInstance(EffectKind.WornItemEnchantment,
                        enchantment.Blueprint.AssetGuid, Identity(enchantment),
                        enchantment.EndTime.Ticks, false));
                }
            }
            return instances;
        }

        // A process-local identity of a native instance (reference identity).
        internal static string Identity(object instance)
        {
            return RuntimeHelpers.GetHashCode(instance).ToString(CultureInfo.InvariantCulture);
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
