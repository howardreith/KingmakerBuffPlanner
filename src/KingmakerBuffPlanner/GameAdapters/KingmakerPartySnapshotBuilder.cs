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
        private readonly KingmakerBuffSourceDiscovery _sourceDiscovery;
        private readonly Dictionary<string, EffectExpression> _effectsBySource =
            new Dictionary<string, EffectExpression>(StringComparer.Ordinal);
        private readonly List<PartySourceDiscoveryTrace> _sourceTraces =
            new List<PartySourceDiscoveryTrace>();
        private readonly List<PartyVariantEligibilityTrace> _variantTraces =
            new List<PartyVariantEligibilityTrace>();
        private int _rawCandidateCount;
        private int _beneficialCandidateCount;
        private int _spellbookCount;
        private string _blessMaterialEvidence = string.Empty;

        internal KingmakerPartySnapshotBuilder(EffectOverrideRegistry overrides = null)
        {
            _sourceDiscovery = new KingmakerBuffSourceDiscovery(overrides);
        }

        internal PartyCatalogDiscoveryDiagnostics Diagnostics { get; private set; }
        internal IDictionary<string, EffectExpression> EffectsBySource
        {
            get { return new Dictionary<string, EffectExpression>(_effectsBySource, StringComparer.Ordinal); }
        }

        internal PartyProviderSnapshot Build()
        {
            if (Game.Instance == null || Game.Instance.Player == null)
                throw new InvalidOperationException("Kingmaker player state is unavailable.");
            _effectsBySource.Clear();
            _sourceTraces.Clear();
            _variantTraces.Clear();
            _rawCandidateCount = 0;
            _beneficialCandidateCount = 0;
            _spellbookCount = 0;
            _blessMaterialEvidence = string.Empty;
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
            var snapshot = new PartyProviderSnapshot(unitSnapshots, providers, pools);
            Diagnostics = new PartyCatalogDiscoveryDiagnostics(
                units.Count, _spellbookCount, _rawCandidateCount, _beneficialCandidateCount,
                _effectsBySource.Count, providers.Count, _sourceTraces,
                _variantTraces, _blessMaterialEvidence);
            return snapshot;
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

        private void ScanSpellbooks(
            UnitEntityData unit,
            List<ProviderSnapshot> providers,
            List<ResourcePoolSnapshot> pools)
        {
            foreach (Spellbook spellbook in unit.Descriptor.Spellbooks
                .Where(b => b != null && b.Blueprint != null)
                .OrderBy(b => b.Blueprint.AssetGuid, StringComparer.Ordinal))
            {
                _spellbookCount++;
                if (spellbook.Blueprint.Spontaneous)
                    ScanSpontaneousSpellbook(unit, spellbook, providers, pools);
                else
                    ScanPreparedSpellbook(unit, spellbook, providers, pools);
            }
        }

        private void ScanSpontaneousSpellbook(
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
                foreach (KingmakerAbilitySelection selection in ExpandOwned(
                    spellbook.GetKnownSpells(level), unit, spellbook.Blueprint.AssetGuid))
                    AddSpellProvider(unit, spellbook, selection, poolKey, level == 0 ? 0 : 1,
                        new string[0], providers);
                foreach (KingmakerAbilitySelection selection in ExpandOwned(
                    spellbook.GetCustomSpells(level), unit, spellbook.Blueprint.AssetGuid))
                    AddSpellProvider(unit, spellbook, selection, poolKey, level == 0 ? 0 : 1,
                        new string[0], providers);
            }
        }

        private void ScanPreparedSpellbook(
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
                    foreach (KingmakerAbilitySelection selection in ExpandOwned(
                        new[] { group.First().Spell }, unit, spellbook.Blueprint.AssetGuid))
                        AddSpellProvider(unit, spellbook, selection, unlimitedKey, 0, new string[0], providers);
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
                foreach (KingmakerAbilitySelection selection in ExpandOwned(
                    new[] { group.First().Spell }, unit, spellbook.Blueprint.AssetGuid))
                    AddSpellProvider(unit, spellbook, selection, poolKey, 1,
                        group.Where(s => s.IsMainSlot).Select(s => ids[s]), providers);
            }
        }

        private void ScanResourceAndFreeAbilities(
            UnitEntityData unit,
            List<ProviderSnapshot> providers,
            List<ResourcePoolSnapshot> pools)
        {
            var poolKeys = new HashSet<string>(pools.Select(p => p.PoolKey), StringComparer.Ordinal);
            foreach (Ability fact in unit.Descriptor.Abilities.Enumerable
                .Where(a => a != null && a.Data != null && a.Data.Blueprint != null)
                .OrderBy(a => a.Blueprint.AssetGuid, StringComparer.Ordinal))
            {
                AbilityData source = fact.Data;
                if (source.Spellbook != null) continue;
                foreach (KingmakerAbilitySelection selection in
                    ExpandOwned(new[] { source }, unit, string.Empty))
                    AddFactProvider(unit, selection, providers, pools, poolKeys);
            }
        }

        private IEnumerable<KingmakerAbilitySelection> ExpandOwned(
            IEnumerable<AbilityData> source, UnitEntityData unit, string spellbookGuid)
        {
            return KingmakerAbilityVariants.Expand(source, trace =>
                _variantTraces.Add(new PartyVariantEligibilityTrace(
                    unit == null ? string.Empty : unit.UniqueId,
                    spellbookGuid,
                    trace.SourceGuid,
                    trace.ChildGuid,
                    trace.Eligible,
                    trace.Reason)));
        }

        private void AddFactProvider(
            UnitEntityData unit,
            KingmakerAbilitySelection selection,
            List<ProviderSnapshot> providers,
            List<ResourcePoolSnapshot> pools,
            HashSet<string> poolKeys)
        {
            AbilityData data = selection.Concrete;
            AbilityData resourceContext = data.Resource != null ? data : selection.Source;
            SourceKind sourceKind = resourceContext.Resource != null
                ? SourceKind.AbilityResource : SourceKind.Fact;
            AbilityKey ability = KingmakerAbilityVariants.ToAbilityKey(selection, sourceKind);
            _rawCandidateCount++;
            EffectExpression expression;
            string reason;
            bool beneficial = _sourceDiscovery.TryDiscover(
                data.Blueprint, out expression, out reason);
            _sourceTraces.Add(new PartySourceDiscoveryTrace(
                ability.Canonical, data.Blueprint.AssetGuid, selection.DisplayName,
                unit.UniqueId, string.Empty, false, beneficial, reason));
            if (!beneficial) return;
            _beneficialCandidateCount++;
            _effectsBySource[ability.Canonical] = expression;

            ResourcePoolSnapshot pool;
            int cost;
            if (resourceContext.Resource != null)
            {
                string key = unit.UniqueId + "|resource|" + resourceContext.Resource.AssetGuid;
                int remaining = Math.Max(0,
                    unit.Descriptor.Resources.GetResourceAmount(resourceContext.Resource));
                int capacity = Math.Max(remaining,
                    resourceContext.Resource.GetMaxAmount(unit.Descriptor));
                pool = new ResourcePoolSnapshot(key, ResourcePoolKind.AbilityResource,
                    capacity, remaining, null);
                cost = resourceContext.ResourceCost;
            }
            else
            {
                string key = unit.UniqueId + "|free|" +
                    selection.SourceBlueprint.AssetGuid;
                pool = new ResourcePoolSnapshot(key, ResourcePoolKind.Unlimited, 0, 0, null);
                cost = 0;
            }
            if (poolKeys.Add(pool.PoolKey)) pools.Add(pool);
            var keyForProvider = new ProviderKey(
                unit.UniqueId, string.Empty, ability, string.Empty);
            if (providers.Any(provider => provider.Key.Equals(keyForProvider))) return;
            string duration = DurationText(selection);
            providers.Add(new ProviderSnapshot(keyForProvider, selection.DisplayName, 0,
                pool.PoolKey, cost, null, ToMaterialRequirement(selection),
                CasterLevel(data), ExpectedDurationRounds(data, duration),
                Description(selection), duration, selection.SourceDisplayName,
                selection.VariantOrder));
        }

        private void AddSpellProvider(
            UnitEntityData unit,
            Spellbook spellbook,
            KingmakerAbilitySelection selection,
            string poolKey,
            int cost,
            IEnumerable<string> tokens,
            List<ProviderSnapshot> providers)
        {
            AbilityData data = selection.Concrete;
            if (data == null || data.Blueprint == null) return;
            if (data.Blueprint.AssetGuid == "90e59f4a4ada87243b7b3535a06d0638")
                _blessMaterialEvidence = DescribeMaterialComponent(data);
            AbilityKey ability = KingmakerAbilityVariants.ToAbilityKey(
                selection, SourceKind.Spellbook);
            _rawCandidateCount++;
            EffectExpression expression;
            string reason;
            bool beneficial = _sourceDiscovery.TryDiscover(
                data.Blueprint, out expression, out reason);
            _sourceTraces.Add(new PartySourceDiscoveryTrace(
                ability.Canonical, data.Blueprint.AssetGuid, selection.DisplayName,
                unit.UniqueId, spellbook.Blueprint.AssetGuid,
                !spellbook.Blueprint.Spontaneous, beneficial, reason));
            if (!beneficial) return;
            _beneficialCandidateCount++;
            _effectsBySource[ability.Canonical] = expression;
            AbilityData source = selection.Source;
            int heighten = source.MetamagicData == null
                ? 0 : source.MetamagicData.HeightenLevel;
            string sourceInstance = "level-" + source.SpellLevel +
                "|heighten-" + heighten;
            var key = new ProviderKey(
                unit.UniqueId, spellbook.Blueprint.AssetGuid, ability, sourceInstance);
            if (providers.Any(p => p.Key.Equals(key))) return;
            string duration = DurationText(selection);
            providers.Add(new ProviderSnapshot(key, selection.DisplayName,
                source.SpellLevel, poolKey, cost, tokens,
                ToMaterialRequirement(selection), CasterLevel(data),
                ExpectedDurationRounds(data, duration), Description(selection),
                duration, selection.SourceDisplayName, selection.VariantOrder));
        }

        private static int CasterLevel(AbilityData data)
        {
            try { return Math.Max(0, data.CalculateParams().CasterLevel); }
            catch (Exception) { return 0; }
        }

        private static int ExpectedDurationRounds(AbilityData data, string duration)
        {
            int casterLevel = Math.Max(1, CasterLevel(data));
            string normalized = duration.ToLowerInvariant();
            if (normalized.Contains("day")) return 14400 * casterLevel;
            if (normalized.Contains("hour")) return 600 * casterLevel;
            if (normalized.Contains("10 min") || normalized.Contains("ten min")) return 100 * casterLevel;
            if (normalized.Contains("minute") || normalized.Contains(" min")) return 10 * casterLevel;
            if (normalized.Contains("round")) return casterLevel;
            return 0;
        }

        private static AbilityKey ToAbilityKey(AbilityData data, SourceKind sourceKind)
        {
            BlueprintAbility blueprint = data.Blueprint;
            string baseGuid = blueprint.Parent == null ? blueprint.AssetGuid : blueprint.Parent.AssetGuid;
            string variantGuid = blueprint.Parent == null ? string.Empty : blueprint.AssetGuid;
            int metamagic = data.MetamagicData == null ? 0 : (int)data.MetamagicData.MetamagicMask;
            return new AbilityKey(baseGuid, variantGuid, metamagic, sourceKind, string.Empty);
        }

        private static MaterialRequirementSnapshot ToMaterialRequirement(
            KingmakerAbilitySelection selection)
        {
            AbilityData data = selection.Concrete;
            BlueprintAbility.MaterialComponentData material = data.Blueprint.MaterialComponent;
            if (!data.RequireMaterialComponent || material.Item == null || material.Count < 1)
            {
                data = selection.Source;
                material = data.Blueprint.MaterialComponent;
            }
            if (!data.RequireMaterialComponent || material.Item == null || material.Count < 1)
                return null;
            return new MaterialRequirementSnapshot(material.Item.AssetGuid, material.Count,
                Game.Instance.Player.Inventory.Count(material.Item));
        }

        private static string Description(KingmakerAbilitySelection selection)
        {
            string value = selection.Concrete.Blueprint.Description;
            return string.IsNullOrWhiteSpace(value)
                ? selection.SourceBlueprint.Description ?? string.Empty
                : value;
        }

        private static string DurationText(KingmakerAbilitySelection selection)
        {
            string value = selection.Concrete.Blueprint.LocalizedDuration == null
                ? string.Empty
                : selection.Concrete.Blueprint.LocalizedDuration.ToString();
            if (!string.IsNullOrWhiteSpace(value)) return value;
            return selection.SourceBlueprint.LocalizedDuration == null
                ? string.Empty
                : selection.SourceBlueprint.LocalizedDuration.ToString();
        }

        private static string DescribeMaterialComponent(AbilityData data)
        {
            BlueprintAbility.MaterialComponentData material = data.Blueprint.MaterialComponent;
            string item = material == null || material.Item == null
                ? "none" : material.Item.AssetGuid;
            int count = material == null ? 0 : material.Count;
            bool hasEnough;
            try { hasEnough = data.HasEnoughMaterialComponent; }
            catch (Exception exception)
            {
                return "blueprint=" + data.Blueprint.AssetGuid + ",require=" +
                    data.RequireMaterialComponent + ",item=" + item + ",count=" + count +
                    ",hasEnough=threw:" + exception.GetType().Name;
            }
            return "blueprint=" + data.Blueprint.AssetGuid + ",require=" +
                data.RequireMaterialComponent + ",item=" + item + ",count=" + count +
                ",hasEnough=" + hasEnough + ",consumableRequired=" +
                (data.RequireMaterialComponent && material != null &&
                    material.Item != null && material.Count > 0);
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

    internal sealed class PartyCatalogDiscoveryDiagnostics
    {
        internal PartyCatalogDiscoveryDiagnostics(
            int partyUnitCount,
            int spellbookCount,
            int rawCandidateCount,
            int beneficialCandidateCount,
            int normalizedEntryCount,
            int providerCount,
            IEnumerable<PartySourceDiscoveryTrace> sources,
            IEnumerable<PartyVariantEligibilityTrace> variants,
            string blessMaterialEvidence)
        {
            PartyUnitCount = partyUnitCount;
            SpellbookCount = spellbookCount;
            RawCandidateCount = rawCandidateCount;
            BeneficialCandidateCount = beneficialCandidateCount;
            NormalizedEntryCount = normalizedEntryCount;
            ProviderCount = providerCount;
            Sources = (sources ?? new PartySourceDiscoveryTrace[0]).ToArray();
            Variants = (variants ?? new PartyVariantEligibilityTrace[0])
                .GroupBy(value => value.Canonical, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(value => value.Canonical, StringComparer.Ordinal).ToArray();
            BlessMaterialEvidence = blessMaterialEvidence ?? string.Empty;
        }

        internal int PartyUnitCount { get; private set; }
        internal int SpellbookCount { get; private set; }
        internal int RawCandidateCount { get; private set; }
        internal int BeneficialCandidateCount { get; private set; }
        internal int NormalizedEntryCount { get; private set; }
        internal int ProviderCount { get; private set; }
        internal IReadOnlyList<PartySourceDiscoveryTrace> Sources { get; private set; }
        internal IReadOnlyList<PartyVariantEligibilityTrace> Variants { get; private set; }
        internal string BlessMaterialEvidence { get; private set; }

        public override string ToString()
        {
            return "party=" + PartyUnitCount + ";spellbooks=" + SpellbookCount +
                ";raw=" + RawCandidateCount + ";beneficial=" + BeneficialCandidateCount +
                ";normalized=" + NormalizedEntryCount + ";providers=" + ProviderCount;
        }
    }

    internal sealed class PartyVariantEligibilityTrace
    {
        internal PartyVariantEligibilityTrace(
            string casterUnitId,
            string spellbookGuid,
            string sourceGuid,
            string childGuid,
            bool eligible,
            string reason)
        {
            CasterUnitId = casterUnitId ?? string.Empty;
            SpellbookGuid = spellbookGuid ?? string.Empty;
            SourceGuid = sourceGuid ?? string.Empty;
            ChildGuid = childGuid ?? string.Empty;
            Eligible = eligible;
            Reason = reason ?? string.Empty;
        }

        internal string CasterUnitId { get; private set; }
        internal string SpellbookGuid { get; private set; }
        internal string SourceGuid { get; private set; }
        internal string ChildGuid { get; private set; }
        internal bool Eligible { get; private set; }
        internal string Reason { get; private set; }
        internal string Canonical
        {
            get
            {
                return CasterUnitId + "|" + SpellbookGuid + "|" +
                    SourceGuid + "|" + ChildGuid + "|" + Eligible + "|" + Reason;
            }
        }
    }

    internal sealed class PartySourceDiscoveryTrace
    {
        internal PartySourceDiscoveryTrace(
            string sourceId,
            string blueprintGuid,
            string displayName,
            string casterUnitId,
            string spellbookGuid,
            bool prepared,
            bool beneficial,
            string reason)
        {
            SourceId = sourceId ?? string.Empty;
            BlueprintGuid = blueprintGuid ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            CasterUnitId = casterUnitId ?? string.Empty;
            SpellbookGuid = spellbookGuid ?? string.Empty;
            Prepared = prepared;
            Beneficial = beneficial;
            Reason = reason ?? string.Empty;
        }

        internal string SourceId { get; private set; }
        internal string BlueprintGuid { get; private set; }
        internal string DisplayName { get; private set; }
        internal string CasterUnitId { get; private set; }
        internal string SpellbookGuid { get; private set; }
        internal bool Prepared { get; private set; }
        internal bool Beneficial { get; private set; }
        internal string Reason { get; private set; }
    }
}
