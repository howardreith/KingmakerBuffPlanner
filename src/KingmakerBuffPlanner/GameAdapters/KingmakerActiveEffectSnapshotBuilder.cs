using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Mechanics;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerActiveEffectSnapshotBuilder
    {
        internal ActiveEffectSnapshot Build()
        {
            if (Game.Instance == null || Game.Instance.Player == null)
                throw new InvalidOperationException("Kingmaker player state is unavailable.");
            var units = new Dictionary<string, UnitEntityData>(StringComparer.Ordinal);
            foreach (UnitEntityData unit in Game.Instance.Player.Party ?? new List<UnitEntityData>())
            {
                if (unit == null || string.IsNullOrWhiteSpace(unit.UniqueId)) continue;
                units[unit.UniqueId] = unit;
                UnitEntityData pet = unit.Descriptor == null ? null : unit.Descriptor.Pet;
                if (pet != null && !string.IsNullOrWhiteSpace(pet.UniqueId)) units[pet.UniqueId] = pet;
            }
            var result = new Dictionary<string, IEnumerable<ActiveEffectInstance>>(StringComparer.Ordinal);
            foreach (UnitEntityData unit in units.Values.OrderBy(u => u.UniqueId, StringComparer.Ordinal))
            {
                var instances = new List<ActiveEffectInstance>();
                if (unit.Descriptor != null)
                {
                    foreach (Buff buff in unit.Descriptor.Buffs.Enumerable.Where(b => b != null && b.Blueprint != null))
                    {
                        EffectKind kind = string.IsNullOrWhiteSpace(buff.SourceAreaEffectId)
                            ? EffectKind.Buff
                            : EffectKind.AreaBuff;
                        instances.Add(new ActiveEffectInstance(kind, buff.Blueprint.AssetGuid,
                            RemainingRounds(buff), CasterLevel(buff), Metamagic(buff),
                            Suppressed(buff)));
                    }
                    foreach (ItemSlot slot in unit.Descriptor.Body.CurrentEquipmentSlots)
                    {
                        ItemEntity item = slot == null ? null : slot.MaybeItem;
                        if (item == null || item.Enchantments == null) continue;
                        // Worn enchantments last while worn: no expiry, and
                        // no caster level or metamagic to compare.
                        foreach (var enchantment in item.Enchantments.Where(e => e != null && e.Blueprint != null))
                            instances.Add(new ActiveEffectInstance(
                                EffectKind.WornItemEnchantment, enchantment.Blueprint.AssetGuid,
                                null, null, null));
                    }
                }
                result[unit.UniqueId] = instances;
            }
            // Markers (all instances) are exactly what the legacy planner
            // always saw; the instance detail feeds only the casting-first
            // existing-effect policy.
            return ActiveEffectSnapshot.FromInstances(result);
        }

        // Rounds of 6 seconds; null for a permanent buff (no expiry).
        private static double? RemainingRounds(Buff buff)
        {
            try
            {
                if (buff.IsPermanent) return null;
                double seconds = buff.TimeLeft.TotalSeconds;
                return seconds <= 0 ? 0d : seconds / 6d;
            }
            catch (Exception)
            {
                // Unreadable duration: treat as nearly expired rather than
                // as permanent, so it never satisfies a duration check.
                return 0d;
            }
        }

        private static int? CasterLevel(Buff buff)
        {
            try
            {
                MechanicsContext context = buff.Context;
                AbilityParams parameters = context == null ? null : context.Params;
                return parameters == null ? (int?)null : Math.Max(0, parameters.CasterLevel);
            }
            catch (Exception) { return null; }
        }

        private static int? Metamagic(Buff buff)
        {
            try
            {
                MechanicsContext context = buff.Context;
                AbilityParams parameters = context == null ? null : context.Params;
                return parameters == null ? (int?)null : Math.Max(0, (int)parameters.Metamagic);
            }
            catch (Exception) { return null; }
        }

        private static bool Suppressed(Buff buff)
        {
            try { return buff.IsSuppressed; }
            catch (Exception) { return false; }
        }
    }
}
