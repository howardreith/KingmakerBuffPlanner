using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Identity;

namespace KingmakerBuffPlanner.Domain.Providers
{
    public enum ResourcePoolKind
    {
        PreparedSlots,
        SpontaneousLevel,
        Unlimited,
        AbilityResource,
        ItemCharges
    }

    public enum PreparedSlotKind
    {
        Common,
        Favorite,
        Opposition,
        Domain,
        DomainAndCommon
    }

    public sealed class ResourceTokenSnapshot
    {
        public ResourceTokenSnapshot(
            string tokenId,
            AbilityKey slottedAbility,
            int spellLevel,
            PreparedSlotKind slotKind,
            bool available,
            bool isPrimary,
            IEnumerable<string> linkedTokenIds)
        {
            if (string.IsNullOrWhiteSpace(tokenId)) throw new ArgumentException("Token ID is required.", "tokenId");
            if (spellLevel < 0) throw new ArgumentOutOfRangeException("spellLevel");
            TokenId = tokenId;
            SlottedAbility = slottedAbility ?? throw new ArgumentNullException("slottedAbility");
            SpellLevel = spellLevel;
            SlotKind = slotKind;
            Available = available;
            IsPrimary = isPrimary;
            LinkedTokenIds = new ReadOnlyCollection<string>((linkedTokenIds ?? new string[0])
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToList());
        }

        public string TokenId { get; private set; }
        public AbilityKey SlottedAbility { get; private set; }
        public int SpellLevel { get; private set; }
        public PreparedSlotKind SlotKind { get; private set; }
        public bool Available { get; private set; }
        public bool IsPrimary { get; private set; }
        public IReadOnlyList<string> LinkedTokenIds { get; private set; }
    }

    public sealed class ResourcePoolSnapshot
    {
        public ResourcePoolSnapshot(
            string poolKey,
            ResourcePoolKind kind,
            int capacity,
            int remaining,
            IEnumerable<ResourceTokenSnapshot> tokens)
        {
            if (string.IsNullOrWhiteSpace(poolKey)) throw new ArgumentException("Pool key is required.", "poolKey");
            if (capacity < 0 || remaining < 0 || remaining > capacity)
                throw new ArgumentOutOfRangeException("remaining");
            var tokenList = (tokens ?? new ResourceTokenSnapshot[0]).OrderBy(t => t.TokenId, StringComparer.Ordinal).ToList();
            if (tokenList.Select(t => t.TokenId).Distinct(StringComparer.Ordinal).Count() != tokenList.Count)
                throw new ArgumentException("Resource token IDs must be unique.", "tokens");
            if (kind == ResourcePoolKind.Unlimited && (capacity != 0 || remaining != 0 || tokenList.Count != 0))
                throw new ArgumentException("Unlimited pools do not use numeric credits or tokens.", "kind");
            if (kind == ResourcePoolKind.PreparedSlots && capacity != tokenList.Count)
                throw new ArgumentException("Prepared pool capacity must equal its discrete token count.", "capacity");
            if (kind == ResourcePoolKind.PreparedSlots && remaining != tokenList.Count(t => t.Available))
                throw new ArgumentException("Prepared pool remaining count must reconcile with available tokens.", "remaining");
            if (kind != ResourcePoolKind.PreparedSlots && tokenList.Count != 0)
                throw new ArgumentException("Only prepared pools contain discrete tokens.", "tokens");
            var tokenIds = new HashSet<string>(tokenList.Select(t => t.TokenId), StringComparer.Ordinal);
            if (tokenList.Any(t => t.LinkedTokenIds.Any(id => !tokenIds.Contains(id))))
                throw new ArgumentException("Prepared token links must remain inside their pool.", "tokens");
            PoolKey = poolKey;
            Kind = kind;
            Capacity = capacity;
            Remaining = remaining;
            Tokens = new ReadOnlyCollection<ResourceTokenSnapshot>(tokenList);
        }

        public string PoolKey { get; private set; }
        public ResourcePoolKind Kind { get; private set; }
        public int Capacity { get; private set; }
        public int Remaining { get; private set; }
        public IReadOnlyList<ResourceTokenSnapshot> Tokens { get; private set; }
    }

    public sealed class TargetValidationSnapshot
    {
        public TargetValidationSnapshot(bool alive, bool conscious, bool friendly, bool targetable)
        {
            Alive = alive;
            Conscious = conscious;
            Friendly = friendly;
            Targetable = targetable;
        }

        public bool Alive { get; private set; }
        public bool Conscious { get; private set; }
        public bool Friendly { get; private set; }
        public bool Targetable { get; private set; }
    }

    public sealed class UnitSnapshot
    {
        public UnitSnapshot(
            string unitId,
            string displayName,
            bool isPet,
            string masterUnitId,
            TargetValidationSnapshot targetValidation)
        {
            if (string.IsNullOrWhiteSpace(unitId)) throw new ArgumentException("Unit ID is required.", "unitId");
            UnitId = unitId;
            DisplayName = displayName ?? string.Empty;
            IsPet = isPet;
            MasterUnitId = masterUnitId ?? string.Empty;
            TargetValidation = targetValidation ?? throw new ArgumentNullException("targetValidation");
        }

        public string UnitId { get; private set; }
        public string DisplayName { get; private set; }
        public bool IsPet { get; private set; }
        public string MasterUnitId { get; private set; }
        public TargetValidationSnapshot TargetValidation { get; private set; }
    }

    public sealed class ProviderSnapshot
    {
        public ProviderSnapshot(
            ProviderKey key,
            string displayName,
            int spellLevel,
            string resourcePoolKey,
            int unitsPerCast,
            IEnumerable<string> eligibleTokenIds,
            MaterialRequirementSnapshot materialComponent = null)
        {
            Key = key ?? throw new ArgumentNullException("key");
            if (spellLevel < 0) throw new ArgumentOutOfRangeException("spellLevel");
            if (string.IsNullOrWhiteSpace(resourcePoolKey))
                throw new ArgumentException("Resource pool key is required.", "resourcePoolKey");
            if (unitsPerCast < 0) throw new ArgumentOutOfRangeException("unitsPerCast");
            DisplayName = displayName ?? string.Empty;
            SpellLevel = spellLevel;
            ResourcePoolKey = resourcePoolKey;
            UnitsPerCast = unitsPerCast;
            EligibleTokenIds = new ReadOnlyCollection<string>((eligibleTokenIds ?? new string[0])
                .Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.Ordinal)
                .OrderBy(v => v, StringComparer.Ordinal).ToList());
            MaterialComponent = materialComponent;
        }

        public ProviderKey Key { get; private set; }
        public string DisplayName { get; private set; }
        public int SpellLevel { get; private set; }
        public string ResourcePoolKey { get; private set; }
        public int UnitsPerCast { get; private set; }
        public IReadOnlyList<string> EligibleTokenIds { get; private set; }
        public MaterialRequirementSnapshot MaterialComponent { get; private set; }
    }

    public sealed class MaterialRequirementSnapshot
    {
        public MaterialRequirementSnapshot(string itemGuid, int requiredCount, int availableCount)
        {
            if (string.IsNullOrWhiteSpace(itemGuid)) throw new ArgumentException("Material item GUID is required.", "itemGuid");
            if (requiredCount < 1) throw new ArgumentOutOfRangeException("requiredCount");
            if (availableCount < 0) throw new ArgumentOutOfRangeException("availableCount");
            ItemGuid = itemGuid;
            RequiredCount = requiredCount;
            AvailableCount = availableCount;
        }

        public string ItemGuid { get; private set; }
        public int RequiredCount { get; private set; }
        public int AvailableCount { get; private set; }
        public bool Available { get { return AvailableCount >= RequiredCount; } }
    }

    public sealed class PartyProviderSnapshot
    {
        public PartyProviderSnapshot(
            IEnumerable<UnitSnapshot> units,
            IEnumerable<ProviderSnapshot> providers,
            IEnumerable<ResourcePoolSnapshot> resourcePools)
        {
            var unitList = (units ?? throw new ArgumentNullException("units"))
                .OrderBy(u => u.UnitId, StringComparer.Ordinal).ToList();
            var providerList = (providers ?? throw new ArgumentNullException("providers"))
                .OrderBy(p => p.Key.Canonical, StringComparer.Ordinal).ToList();
            var poolList = (resourcePools ?? throw new ArgumentNullException("resourcePools"))
                .OrderBy(p => p.PoolKey, StringComparer.Ordinal).ToList();
            RequireUnique(unitList.Select(u => u.UnitId), "unit IDs");
            RequireUnique(providerList.Select(p => p.Key.Canonical), "provider keys");
            RequireUnique(poolList.Select(p => p.PoolKey), "resource pool keys");
            var unitIds = new HashSet<string>(unitList.Select(u => u.UnitId), StringComparer.Ordinal);
            var poolsByKey = poolList.ToDictionary(p => p.PoolKey, StringComparer.Ordinal);
            foreach (ProviderSnapshot provider in providerList)
            {
                if (!unitIds.Contains(provider.Key.CasterUnitId))
                    throw new ArgumentException("Provider caster is absent from the party snapshot.", "providers");
                ResourcePoolSnapshot pool;
                if (!poolsByKey.TryGetValue(provider.ResourcePoolKey, out pool))
                    throw new ArgumentException("Provider resource pool is absent.", "providers");
                var tokenIds = new HashSet<string>(pool.Tokens.Select(t => t.TokenId), StringComparer.Ordinal);
                if (provider.EligibleTokenIds.Any(id => !tokenIds.Contains(id)))
                    throw new ArgumentException("Provider references an absent resource token.", "providers");
                if (pool.Tokens.Any(t => provider.EligibleTokenIds.Contains(t.TokenId) && !t.IsPrimary))
                    throw new ArgumentException("Provider cannot spend a linked slot as a primary token.", "providers");
                if (pool.Kind == ResourcePoolKind.PreparedSlots && provider.EligibleTokenIds.Count == 0)
                    throw new ArgumentException("Prepared providers require at least one eligible token.", "providers");
                if (pool.Kind != ResourcePoolKind.PreparedSlots && provider.EligibleTokenIds.Count != 0)
                    throw new ArgumentException("Shared-pool providers cannot claim discrete tokens.", "providers");
                if (pool.Kind == ResourcePoolKind.Unlimited && provider.UnitsPerCast != 0)
                    throw new ArgumentException("Unlimited providers use zero numeric units per cast.", "providers");
            }
            Units = new ReadOnlyCollection<UnitSnapshot>(unitList);
            Providers = new ReadOnlyCollection<ProviderSnapshot>(providerList);
            ResourcePools = new ReadOnlyCollection<ResourcePoolSnapshot>(poolList);
        }

        public IReadOnlyList<UnitSnapshot> Units { get; private set; }
        public IReadOnlyList<ProviderSnapshot> Providers { get; private set; }
        public IReadOnlyList<ResourcePoolSnapshot> ResourcePools { get; private set; }

        private static void RequireUnique(IEnumerable<string> values, string label)
        {
            var list = values.ToList();
            if (list.Distinct(StringComparer.Ordinal).Count() != list.Count)
                throw new ArgumentException("Snapshot contains duplicate " + label + ".");
        }
    }
}
