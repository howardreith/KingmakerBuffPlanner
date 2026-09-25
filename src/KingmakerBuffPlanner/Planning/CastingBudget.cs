using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.Planning
{
    public enum CastingCostCategory
    {
        NativePool,
        EnhancementPool,
        Material
    }

    // One cost component of one casting's complete cost vector.
    public sealed class CastingCostLine
    {
        internal CastingCostLine(
            CastingCostCategory category,
            string poolKey,
            int units,
            IEnumerable<string> tokenIds,
            string itemGuid,
            bool unlimited = false)
        {
            Category = category;
            PoolKey = poolKey ?? string.Empty;
            Units = units;
            Unlimited = unlimited;
            TokenIds = new ReadOnlyCollection<string>((tokenIds ?? new string[0])
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
            ItemGuid = itemGuid ?? string.Empty;
        }

        public CastingCostCategory Category { get; private set; }
        public string PoolKey { get; private set; }
        public int Units { get; private set; }
        public IReadOnlyList<string> TokenIds { get; private set; }
        public string ItemGuid { get; private set; }
        // Review M1: a native line reserved from a verified Unlimited pool
        // (zero units, no tokens). Only such a line proves a free cast.
        public bool Unlimited { get; private set; }
    }

    // One authoritative per-pool budget line for a compiled plan. AvailableNow
    // is null when the pool's balance is unknown — unknown is never zero.
    public sealed class CastingBudgetLine
    {
        internal CastingBudgetLine(
            string poolKey,
            CastingCostCategory category,
            int? availableNow,
            int requestedUsage,
            int allocatedUsage,
            IEnumerable<string> traces,
            bool unlimited = false)
        {
            Unlimited = unlimited;
            PoolKey = poolKey;
            Category = category;
            AvailableNow = availableNow;
            RequestedUsage = requestedUsage;
            AllocatedUsage = allocatedUsage;
            UnmetDemand = Math.Max(0, requestedUsage - allocatedUsage);
            ForecastRemaining = availableNow == null
                ? null : new int?(Math.Max(0, availableNow.Value - allocatedUsage));
            Traces = new ReadOnlyCollection<string>((traces ?? new string[0])
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal)
                .ToList());
        }

        public string PoolKey { get; private set; }
        public CastingCostCategory Category { get; private set; }
        public int? AvailableNow { get; private set; }
        public int RequestedUsage { get; private set; }
        public int AllocatedUsage { get; private set; }
        public int UnmetDemand { get; private set; }
        public int? ForecastRemaining { get; private set; }
        public IReadOnlyList<string> Traces { get; private set; }
        // A verified Unlimited native pool: no balance to forecast (null),
        // which here means "not limited", never "unknown".
        public bool Unlimited { get; private set; }
    }

    internal sealed class CastingDemand
    {
        public CastingDemand(
            CastingCostCategory category,
            string poolKey,
            int units,
            string itemGuid)
        {
            Category = category;
            PoolKey = poolKey;
            Units = units;
            ItemGuid = itemGuid ?? string.Empty;
        }

        public CastingCostCategory Category { get; private set; }
        public string PoolKey { get; private set; }
        public int Units { get; private set; }
        public string ItemGuid { get; private set; }
    }

    // Shared-budget ledger for one compilation. Each casting's complete cost
    // vector — native pool (including linked prepared tokens), enhancement
    // usage pools, and material components — reserves atomically: a deficit
    // in any component blocks the casting and reserves nothing anywhere.
    // Pools are shared across castings in persisted order, so two features
    // spending one real reservoir see one combined balance.
    internal sealed class CastingBudgetLedger
    {
        private readonly ResourceLedger _native;
        private readonly Dictionary<string, ResourcePoolKind> _nativeKinds;
        private readonly Dictionary<string, int> _nativeInitial;
        private readonly Dictionary<string, int?> _enhancementInitial =
            new Dictionary<string, int?>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _enhancementRemaining =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _materialInitial =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _materialRemaining =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _requestedUnits =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _allocatedUnits =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> _traces =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, CastingCostCategory> _categoryBy =
            new Dictionary<string, CastingCostCategory>(StringComparer.Ordinal);

        public CastingBudgetLedger(
            PartyProviderSnapshot snapshot,
            IEnumerable<CastEnhancementSnapshot> enhancements)
        {
            _native = new ResourceLedger(snapshot.ResourcePools);
            _nativeKinds = snapshot.ResourcePools.ToDictionary(
                pool => pool.PoolKey, pool => pool.Kind, StringComparer.Ordinal);
            _nativeInitial = snapshot.ResourcePools.ToDictionary(
                pool => pool.PoolKey,
                pool => pool.Kind == ResourcePoolKind.PreparedSlots
                    ? pool.Tokens.Count(token => token.Available) : pool.Remaining,
                StringComparer.Ordinal);
            foreach (IGrouping<string, CastEnhancementSnapshot> group in
                (enhancements ?? new CastEnhancementSnapshot[0])
                    .GroupBy(value => value.UsagePoolId, StringComparer.Ordinal))
            {
                // Snapshots of one shared reservoir each report the pool's
                // balance; the minimum of the reported balances is the
                // conservative shared number, never a per-item sum.
                int? known = group.Where(value => value.RemainingUses != null)
                    .Select(value => value.RemainingUses.Value).Cast<int?>()
                    .DefaultIfEmpty(null).Min();
                _enhancementInitial[group.Key] = known;
                if (known != null) _enhancementRemaining[group.Key] = known.Value;
            }
            foreach (ProviderSnapshot provider in snapshot.Providers)
            {
                MaterialRequirementSnapshot material = provider.MaterialComponent;
                if (material == null) continue;
                int existing;
                _materialInitial[material.ItemGuid] = material.AvailableCount;
                _materialRemaining.TryGetValue(material.ItemGuid, out existing);
                if (material.AvailableCount > existing)
                    _materialRemaining[material.ItemGuid] = material.AvailableCount;
            }
        }

        // The complete cost vector of one casting: its native pool charge,
        // the summed demand of its selected enhancements and applied
        // targeting modifiers per usage pool, and its material component.
        // Modifier demands merge into the same pool grouping so features
        // sharing a reservoir validate as combined demand, atomically.
        public IReadOnlyList<CastingDemand> DemandsFor(
            ProviderSnapshot provider,
            IEnumerable<CastEnhancementSnapshot> selectedEnhancements,
            IEnumerable<ModifierUsageDemand> modifierDemands = null)
        {
            var demands = new List<CastingDemand>();
            ResourcePoolKind kind;
            // Review M1: a KNOWN Unlimited pool is still one native demand
            // (zero units), reserved through the ledger so the verified free
            // cost reaches the resolved casting. Any other or unknown pool
            // keeps a paid demand of at least one unit.
            if (provider != null)
                demands.Add(new CastingDemand(
                    CastingCostCategory.NativePool, provider.ResourcePoolKey,
                    _nativeKinds.TryGetValue(provider.ResourcePoolKey, out kind) &&
                        kind == ResourcePoolKind.Unlimited
                        ? 0 : Math.Max(1, provider.UnitsPerCast), string.Empty));
            var poolUnits = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (IGrouping<string, CastEnhancementSnapshot> group in
                (selectedEnhancements ?? new CastEnhancementSnapshot[0])
                    .Where(value => value != null)
                    .GroupBy(value => value.UsagePoolId, StringComparer.Ordinal))
                poolUnits[group.Key] =
                    group.Sum(value => value.UsageUnitsPerCast);
            foreach (ModifierUsageDemand demand in
                (modifierDemands ?? new ModifierUsageDemand[0])
                    .Where(value => value != null))
                poolUnits[demand.UsagePoolId] =
                    (poolUnits.TryGetValue(demand.UsagePoolId, out var prior)
                        ? prior : 0) + demand.Units;
            foreach (KeyValuePair<string, int> pool in poolUnits)
                demands.Add(new CastingDemand(
                    CastingCostCategory.EnhancementPool, pool.Key,
                    pool.Value, string.Empty));
            MaterialRequirementSnapshot materialComponent =
                provider == null ? null : provider.MaterialComponent;
            if (materialComponent != null)
                demands.Add(new CastingDemand(
                    CastingCostCategory.Material, materialComponent.ItemGuid,
                    materialComponent.RequiredCount, materialComponent.ItemGuid));
            return demands;
        }

        // Validates the complete cost vector first; only a fully fundable
        // casting mutates any balance. A failed candidate leaves every pool
        // exactly as it was.
        public bool TryReserveAtomically(
            string castingId,
            ProviderSnapshot provider,
            IReadOnlyList<CastingDemand> demands,
            out IReadOnlyList<CastingCostLine> cost,
            out string reason)
        {
            cost = null;
            foreach (CastingDemand demand in demands)
            {
                if (demand.Category == CastingCostCategory.EnhancementPool)
                {
                    if (!_enhancementInitial.ContainsKey(demand.PoolKey)) continue;
                    int remaining;
                    if (_enhancementRemaining.TryGetValue(demand.PoolKey, out remaining) &&
                        remaining < demand.Units)
                    {
                        reason = "enhancement-pool-exhausted:" + demand.PoolKey +
                            ":" + remaining + "<" + demand.Units;
                        return false;
                    }
                    continue;
                }
                if (demand.Category == CastingCostCategory.Material)
                {
                    int remaining;
                    if (_materialRemaining.TryGetValue(demand.PoolKey, out remaining) &&
                        remaining < demand.Units)
                    {
                        reason = "material-unavailable:" + demand.PoolKey +
                            ":" + remaining + "<" + demand.Units;
                        return false;
                    }
                    continue;
                }
                // Native pre-check by pool kind: shared pools must still
                // hold the units after earlier castings' reservations;
                // prepared pools must still expose an available token.
                ResourcePoolKind kind;
                if (_nativeKinds.TryGetValue(demand.PoolKey, out kind) &&
                    kind != ResourcePoolKind.Unlimited)
                {
                    int remaining = _native.GetRemaining(demand.PoolKey);
                    if (kind == ResourcePoolKind.PreparedSlots
                        ? remaining < 1
                        : remaining < demand.Units)
                    {
                        reason = kind == ResourcePoolKind.PreparedSlots
                            ? "prepared-slots-exhausted:" + demand.PoolKey
                            : "resource-pool-exhausted:" + demand.PoolKey +
                                ":" + remaining + "<" + demand.Units;
                        return false;
                    }
                }
            }
            // Every component is fundable: commit, native reservation first
            // so a late native surprise cannot follow an already-committed
            // enhancement or material charge.
            var lines = new List<CastingCostLine>();
            foreach (CastingDemand demand in demands)
            {
                if (demand.Category != CastingCostCategory.NativePool) continue;
                if (provider == null)
                {
                    reason = "native-reservation-failed";
                    return false;
                }
                ResourceReservation reservation;
                string nativeReason;
                if (!_native.TryReserve(provider, out reservation, out nativeReason))
                {
                    reason = nativeReason;
                    return false;
                }
                lines.Add(new CastingCostLine(
                    demand.Category, demand.PoolKey, reservation.Units,
                    reservation.TokenIds, null, reservation.Unlimited));
                // Final review A4: what a funded casting uses is what it
                // reserved (a linked prepared pair is two slots), never less
                // than its demand (an unverified zero cost still requests one
                // unit), so a later unfunded casting shows as a real shortage.
                Record(demand.PoolKey, demand.Category, castingId,
                    Math.Max(demand.Units, reservation.Units), reservation.Units);
            }
            foreach (CastingDemand demand in demands)
            {
                if (demand.Category == CastingCostCategory.EnhancementPool)
                {
                    if (_enhancementRemaining.ContainsKey(demand.PoolKey))
                        _enhancementRemaining[demand.PoolKey] -= demand.Units;
                    lines.Add(new CastingCostLine(
                        demand.Category, demand.PoolKey, demand.Units, null, null));
                    Record(demand.PoolKey, demand.Category, castingId, demand.Units,
                        demand.Units);
                    continue;
                }
                if (demand.Category == CastingCostCategory.Material)
                {
                    if (_materialRemaining.ContainsKey(demand.PoolKey))
                        _materialRemaining[demand.PoolKey] -= demand.Units;
                    lines.Add(new CastingCostLine(
                        demand.Category, demand.PoolKey, demand.Units, null,
                        demand.ItemGuid));
                    Record(demand.PoolKey, demand.Category, castingId, demand.Units,
                        demand.Units);
                    continue;
                }
            }
            cost = lines;
            reason = null;
            return true;
        }

        // Records demand traces for a casting that could not be funded, so
        // the budget line exposes the deficit and its responsible castings.
        public void RecordUnfunded(
            string castingId, IReadOnlyList<CastingDemand> demands)
        {
            foreach (CastingDemand demand in demands)
                Record(demand.PoolKey, demand.Category, castingId, demand.Units, 0);
        }

        public IReadOnlyList<CastingBudgetLine> BuildReport()
        {
            var lines = new List<CastingBudgetLine>();
            foreach (KeyValuePair<string, CastingCostCategory> pair in _categoryBy)
            {
                int requested;
                _requestedUnits.TryGetValue(pair.Key, out requested);
                int allocated;
                _allocatedUnits.TryGetValue(pair.Key, out allocated);
                int? available = null;
                ResourcePoolKind nativeKind;
                bool unlimited = pair.Value == CastingCostCategory.NativePool &&
                    _nativeKinds.TryGetValue(pair.Key, out nativeKind) &&
                    nativeKind == ResourcePoolKind.Unlimited;
                if (pair.Value == CastingCostCategory.NativePool &&
                    _nativeInitial.ContainsKey(pair.Key) && !unlimited)
                    available = _nativeInitial[pair.Key];
                else if (pair.Value == CastingCostCategory.EnhancementPool &&
                    _enhancementInitial.ContainsKey(pair.Key))
                    available = _enhancementInitial[pair.Key];
                else if (pair.Value == CastingCostCategory.Material &&
                    _materialInitial.ContainsKey(pair.Key))
                    available = _materialInitial[pair.Key];
                List<string> traces;
                _traces.TryGetValue(pair.Key, out traces);
                IEnumerable<string> traceList = traces;
                lines.Add(new CastingBudgetLine(
                    pair.Key, pair.Value, available, requested, allocated,
                    traceList ?? new string[0], unlimited));
            }
            return lines.OrderBy(line => line.PoolKey, StringComparer.Ordinal)
                .ToList();
        }

        private void Record(
            string poolKey,
            CastingCostCategory category,
            string castingId,
            int requested,
            int allocated)
        {
            List<string> poolTraces;
            if (!_traces.TryGetValue(poolKey, out poolTraces))
            {
                poolTraces = new List<string>();
                _traces[poolKey] = poolTraces;
            }
            if (!poolTraces.Contains(castingId)) poolTraces.Add(castingId);
            _categoryBy[poolKey] = category;
            int prior;
            _requestedUnits[poolKey] =
                (_requestedUnits.TryGetValue(poolKey, out prior) ? prior : 0) + requested;
            _allocatedUnits[poolKey] =
                (_allocatedUnits.TryGetValue(poolKey, out prior) ? prior : 0) + allocated;
        }
    }
}
