using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic.Buffs;
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
            var result = new Dictionary<string, IEnumerable<ActiveEffectMarker>>(StringComparer.Ordinal);
            foreach (UnitEntityData unit in units.Values.OrderBy(u => u.UniqueId, StringComparer.Ordinal))
            {
                var markers = new HashSet<ActiveEffectMarker>();
                if (unit.Descriptor != null)
                {
                    foreach (Buff buff in unit.Descriptor.Buffs.Enumerable.Where(b => b != null && b.Blueprint != null))
                    {
                        EffectKind kind = string.IsNullOrWhiteSpace(buff.SourceAreaEffectId)
                            ? EffectKind.Buff
                            : EffectKind.AreaBuff;
                        markers.Add(new ActiveEffectMarker(kind, buff.Blueprint.AssetGuid));
                    }
                    foreach (ItemSlot slot in unit.Descriptor.Body.CurrentEquipmentSlots)
                    {
                        ItemEntity item = slot == null ? null : slot.MaybeItem;
                        if (item == null || item.Enchantments == null) continue;
                        foreach (var enchantment in item.Enchantments.Where(e => e != null && e.Blueprint != null))
                            markers.Add(new ActiveEffectMarker(
                                EffectKind.WornItemEnchantment, enchantment.Blueprint.AssetGuid));
                    }
                }
                result[unit.UniqueId] = markers;
            }
            return ActiveEffectSnapshot.FromTypedEffects(result);
        }
    }
}
