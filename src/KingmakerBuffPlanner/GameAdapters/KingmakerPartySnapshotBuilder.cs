using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerPartySnapshotBuilder
    {
        internal PartyProviderSnapshot Build()
        {
            if (Game.Instance == null || Game.Instance.Player == null)
                throw new InvalidOperationException("Kingmaker player state is unavailable.");
            var units = CollectUnits(Game.Instance.Player.Party);
            var unitSnapshots = new List<UnitSnapshot>();
            var providers = new List<ProviderSnapshot>();
            var pools = new List<ResourcePoolSnapshot>();
            foreach (UnitEntityData unit in units)
            {
                unitSnapshots.Add(ToUnitSnapshot(unit, units));
                ScanSpellbooks(unit, providers, pools);
                ScanResourceAndFreeAbilities(unit, providers, pools);
            }
            return new PartyProviderSnapshot(unitSnapshots, providers, pools);
        }

        private static List<UnitEntityData> CollectUnits(IEnumerable<UnitEntityData> party)
        {
            var result = new Dictionary<string, UnitEntityData>(StringComparer.Ordinal);
            foreach (UnitEntityData unit in party ?? new UnitEntityData[0])
            {
                if (unit == null || string.IsNullOrWhiteSpace(unit.UniqueId)) continue;
                result[unit.UniqueId] = unit;
                UnitEntityData pet = unit.Descriptor == null ? null : unit.Descriptor.Pet;
                if (pet != null && !string.IsNullOrWhiteSpace(pet.UniqueId)) result[pet.UniqueId] = pet;
            }
            return result.Values.OrderBy(u => u.UniqueId, StringComparer.Ordinal).ToList();
        }

        private static UnitSnapshot ToUnitSnapshot(UnitEntityData unit, IEnumerable<UnitEntityData> units)
        {
            UnitEntityData master = units.FirstOrDefault(u => u.Descriptor != null &&
                u.Descriptor.Pet != null && u.Descriptor.Pet.UniqueId == unit.UniqueId);
            bool isPet = unit.Descriptor != null && unit.Descriptor.IsPet;
            bool alive = unit.Descriptor != null && unit.Descriptor.State != null && !unit.Descriptor.State.IsDead;
            bool conscious = unit.Descriptor != null && unit.Descriptor.State != null && unit.Descriptor.State.IsConscious;
            bool targetable = unit.Descriptor != null && unit.Descriptor.State != null &&
                unit.Descriptor.State.IsUntargetable.Count == 0;
            return new UnitSnapshot(unit.UniqueId, unit.CharacterName, isPet,
                master == null ? string.Empty : master.UniqueId,
                new TargetValidationSnapshot(alive, conscious, unit.IsPlayerFaction, targetable));
        }

        private static void ScanSpellbooks(
            UnitEntityData unit,
            List<ProviderSnapshot> providers,
            List<ResourcePoolSnapshot> pools)
        {
            foreach (Spellbook spellbook in unit.Descriptor.Spellbooks
                .Where(b => b != null && b.Blueprint != null)
                .OrderBy(b => b.Blueprint.AssetGuid, StringComparer.Ordinal))
            {
                if (spellbook.Blueprint.Spontaneous)
                    ScanSpontaneousSpellbook(unit, spellbook, providers, pools);
                else
                    ScanPreparedSpellbook(unit, spellbook, providers, pools);
            }
        }

        private static void ScanSpontaneousSpellbook(
            UnitEntityData unit,
            Spellbook spellbook,
            List<ProviderSnapshot> providers,
            List<ResourcePoolSnapshot> pools)
        {
            for (int level = 0; level <= spellbook.MaxSpellLevel; level++)
            {
                string poolKey = PoolKey(unit.UniqueId, spellbook.Blueprint.AssetGuid,
                    level == 0 ? "unlimited" : "spontaneous-" + level);
                if (level == 0)
                    pools.Add(new ResourcePoolSnapshot(poolKey, ResourcePoolKind.Unlimited, 0, 0, null));
                else
                {
                    int remaining = Math.Max(0, spellbook.GetSpontaneousSlots(level));
                    int capacity = Math.Max(remaining, spellbook.GetSpellsPerDay(level));
                    pools.Add(new ResourcePoolSnapshot(poolKey, ResourcePoolKind.SpontaneousLevel,
                        capacity, remaining, null));
                }
                foreach (AbilityData data in ExpandVariants(spellbook.GetKnownSpells(level)))
                    AddSpellProvider(unit, spellbook, data, poolKey, level == 0 ? 0 : 1,
                        new string[0], providers);
                foreach (AbilityData data in ExpandVariants(spellbook.GetCustomSpells(level)))
                    AddSpellProvider(unit, spellbook, data, poolKey, level == 0 ? 0 : 1,
                        new string[0], providers);
            }
        }

        private static void ScanPreparedSpellbook(
            UnitEntityData unit,
            Spellbook spellbook,
            List<ProviderSnapshot> providers,
            List<ResourcePoolSnapshot> pools)
        {
            var allSlots = spellbook.GetAllMemorizedSpells().Where(s => s != null && s.Spell != null)
                .OrderBy(s => s.SpellLevel).ThenBy(s => s.Type).ThenBy(s => s.Index).ToList();
            var cantripSlots = allSlots.Where(s => s.SpellLevel == 0).ToList();
            if (cantripSlots.Count != 0)
            {
                string unlimitedKey = PoolKey(unit.UniqueId, spellbook.Blueprint.AssetGuid, "unlimited");
                pools.Add(new ResourcePoolSnapshot(unlimitedKey, ResourcePoolKind.Unlimited, 0, 0, null));
                foreach (IGrouping<string, SpellSlot> group in cantripSlots.GroupBy(
                    s => ToAbilityKey(s.Spell, SourceKind.Spellbook).Canonical, StringComparer.Ordinal))
                {
                    foreach (AbilityData data in ExpandVariants(new[] { group.First().Spell }))
                        AddSpellProvider(unit, spellbook, data, unlimitedKey, 0, new string[0], providers);
                }
            }
            var slots = allSlots.Where(s => s.SpellLevel > 0).ToList();
            if (slots.Count == 0) return;
            var ids = slots.ToDictionary(s => s, SlotId, ReferenceEqualityComparer<SpellSlot>.Instance);
            var tokens = new List<ResourceTokenSnapshot>();
            foreach (SpellSlot slot in slots)
            {
                string[] linked = (slot.LinkedSlots ?? new SpellSlot[0]).Where(ids.ContainsKey)
                    .Select(s => ids[s]).OrderBy(v => v, StringComparer.Ordinal).ToArray();
                tokens.Add(new ResourceTokenSnapshot(ids[slot], ToAbilityKey(slot.Spell, SourceKind.Spellbook),
                    slot.SpellLevel, ToSlotKind(slot), slot.Available, slot.IsMainSlot, linked));
            }
            if (tokens.Count == 0) return;
            string poolKey = PoolKey(unit.UniqueId, spellbook.Blueprint.AssetGuid, "prepared");
            pools.Add(new ResourcePoolSnapshot(poolKey, ResourcePoolKind.PreparedSlots,
                tokens.Count, tokens.Count(t => t.Available), tokens));
            foreach (IGrouping<string, SpellSlot> group in slots.GroupBy(
                s => ToAbilityKey(s.Spell, SourceKind.Spellbook).Canonical, StringComparer.Ordinal))
            {
                foreach (AbilityData data in ExpandVariants(new[] { group.First().Spell }))
                    AddSpellProvider(unit, spellbook, data, poolKey, 1,
                        group.Where(s => s.IsMainSlot).Select(s => ids[s]), providers);
            }
        }

        private static void ScanResourceAndFreeAbilities(
            UnitEntityData unit,
            List<ProviderSnapshot> providers,
            List<ResourcePoolSnapshot> pools)
        {
            var poolKeys = new HashSet<string>(pools.Select(p => p.PoolKey), StringComparer.Ordinal);
            foreach (Ability fact in unit.Descriptor.Abilities.Enumerable
                .Where(a => a != null && a.Data != null && a.Data.Blueprint != null)
                .OrderBy(a => a.Blueprint.AssetGuid, StringComparer.Ordinal))
            {
                AbilityData data = fact.Data;
                if (data.Spellbook != null || !HasDetectedEffect(data.Blueprint)) continue;
                ResourcePoolSnapshot pool;
                int cost;
                SourceKind sourceKind;
                if (data.Resource != null)
                {
                    string key = unit.UniqueId + "|resource|" + data.Resource.AssetGuid;
                    int remaining = Math.Max(0, unit.Descriptor.Resources.GetResourceAmount(data.Resource));
                    int capacity = Math.Max(remaining, data.Resource.GetMaxAmount(unit.Descriptor));
                    pool = new ResourcePoolSnapshot(key, ResourcePoolKind.AbilityResource,
                        capacity, remaining, null);
                    cost = data.ResourceCost;
                    sourceKind = SourceKind.AbilityResource;
                }
                else
                {
                    string key = unit.UniqueId + "|free|" + data.Blueprint.AssetGuid;
                    pool = new ResourcePoolSnapshot(key, ResourcePoolKind.Unlimited, 0, 0, null);
                    cost = 0;
                    sourceKind = SourceKind.Fact;
                }
                if (poolKeys.Add(pool.PoolKey)) pools.Add(pool);
                var ability = ToAbilityKey(data, sourceKind);
                var keyForProvider = new ProviderKey(unit.UniqueId, string.Empty, ability, string.Empty);
                providers.Add(new ProviderSnapshot(keyForProvider, data.Name, 0,
                    pool.PoolKey, cost, null));
            }
        }

        private static void AddSpellProvider(
            UnitEntityData unit,
            Spellbook spellbook,
            AbilityData data,
            string poolKey,
            int cost,
            IEnumerable<string> tokens,
            List<ProviderSnapshot> providers)
        {
            if (data == null || data.Blueprint == null || !HasDetectedEffect(data.Blueprint)) return;
            var ability = ToAbilityKey(data, SourceKind.Spellbook);
            int heighten = data.MetamagicData == null ? 0 : data.MetamagicData.HeightenLevel;
            string sourceInstance = "level-" + data.SpellLevel + "|heighten-" + heighten;
            var key = new ProviderKey(unit.UniqueId, spellbook.Blueprint.AssetGuid, ability, sourceInstance);
            if (providers.Any(p => p.Key.Equals(key))) return;
            providers.Add(new ProviderSnapshot(key, data.Name, data.SpellLevel,
                poolKey, cost, tokens));
        }

        private static IEnumerable<AbilityData> ExpandVariants(IEnumerable<AbilityData> source)
        {
            foreach (AbilityData data in source ?? new AbilityData[0])
            {
                if (data == null) continue;
                yield return data;
                foreach (AbilityData variant in data.Variants ?? new AbilityData[0])
                    if (variant != null) yield return variant;
            }
        }

        private static bool HasDetectedEffect(BlueprintAbility ability)
        {
            return EffectExpressionAnalysis.ContainsLeaf(
                new ActionGraphScanner().Scan(new KingmakerActionGraphAdapter().Adapt(ability)).Expression);
        }

        private static AbilityKey ToAbilityKey(AbilityData data, SourceKind sourceKind)
        {
            BlueprintAbility blueprint = data.Blueprint;
            string baseGuid = blueprint.Parent == null ? blueprint.AssetGuid : blueprint.Parent.AssetGuid;
            string variantGuid = blueprint.Parent == null ? string.Empty : blueprint.AssetGuid;
            int metamagic = data.MetamagicData == null ? 0 : (int)data.MetamagicData.MetamagicMask;
            return new AbilityKey(baseGuid, variantGuid, metamagic, sourceKind, string.Empty);
        }

        private static string PoolKey(string unitId, string spellbookGuid, string suffix)
        {
            return unitId + "|spellbook|" + spellbookGuid + "|" + suffix;
        }

        private static string SlotId(SpellSlot slot)
        {
            return "level-" + slot.SpellLevel + "|type-" + (int)slot.Type + "|index-" + slot.Index;
        }

        private static PreparedSlotKind ToSlotKind(SpellSlot slot)
        {
            if (slot.IsOpposition) return PreparedSlotKind.Opposition;
            SpellSlotType type = slot.Type;
            if (type == SpellSlotType.Domain) return PreparedSlotKind.Domain;
            if (type == SpellSlotType.DomainAndCommon) return PreparedSlotKind.DomainAndCommon;
            if (type == SpellSlotType.Opposite) return PreparedSlotKind.Opposition;
            if (type == SpellSlotType.Favorite) return PreparedSlotKind.Favorite;
            return PreparedSlotKind.Common;
        }
    }
}
