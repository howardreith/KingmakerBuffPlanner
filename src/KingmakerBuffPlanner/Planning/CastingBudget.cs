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
                // Every required component is tracked, including one the
                // party has none of: an untracked zero never blocked a casting
                // (graph review). Providers reporting one item agree on the
                // largest count, as the classic planner reads it.
                int existing;
                if (!_materialRemaining.TryGetValue(material.ItemGuid, out existing) ||
                    material.AvailableCount > existing)
                {
                    _materialRemaining[material.ItemGuid] = material.AvailableCount;
                    _materialInitial[material.ItemGuid] = material.AvailableCount;
                }
            }
        }

        // A deep copy of every balance and every demand record: reservations
        // on the copy never reach the original (capacity probes).
        private CastingBudgetLedger(CastingBudgetLedger source)
        {
            _native = source._native.Clone();
            _nativeKinds = source._nativeKinds;
            _nativeInitial = source._nativeInitial;
            foreach (KeyValuePair<string, int?> pair in source._enhancementInitial)
                _enhancementInitial[pair.Key] = pair.Value;
            foreach (KeyValuePair<string, int> pair in source._enhancementRemaining)
                _enhancementRemaining[pair.Key] = pair.Value;
            foreach (KeyValuePair<string, int> pair in source._materialInitial)
                _materialInitial[pair.Key] = pair.Value;
            foreach (KeyValuePair<string, int> pair in source._materialRemaining)
                _materialRemaining[pair.Key] = pair.Value;
            foreach (KeyValuePair<string, int> pair in source._requestedUnits)
                _requestedUnits[pair.Key] = pair.Value;
            foreach (KeyValuePair<string, int> pair in source._allocatedUnits)
                _allocatedUnits[pair.Key] = pair.Value;
            foreach (KeyValuePair<string, List<string>> pair in source._traces)
                _traces[pair.Key] = new List<string>(pair.Value);
            foreach (KeyValuePair<string, CastingCostCategory> pair in source._categoryBy)
                _categoryBy[pair.Key] = pair.Value;
        }

        internal CastingBudgetLedger Clone()
        {
            return new CastingBudgetLedger(this);
        }

        internal ResourcePoolKind? NativeKind(string poolKey)
        {
            ResourcePoolKind kind;
            return poolKey != null && _nativeKinds.TryGetValue(poolKey, out kind)
                ? kind : (ResourcePoolKind?)null;
        }

        // Units (or available prepared slots) left after every reservation
        // made so far; null for a pool this ledger does not know.
        internal int? NativeRemaining(string poolKey)
        {
            return _native.Knows(poolKey) ? _native.GetRemaining(poolKey) : (int?)null;
        }

        internal int? NativeAvailableNow(string poolKey)
        {
            int available;
            return poolKey != null && _nativeInitial.TryGetValue(poolKey, out available)
                ? available : (int?)null;
        }

        // Null means the pool's balance is unknown (never zero).
        internal int? EnhancementRemaining(string usagePoolId)
        {
            int remaining;
            return usagePoolId != null && _enhancementRemaining.TryGetValue(usagePoolId, out remaining)
                ? remaining : (int?)null;
        }

        internal int? EnhancementAvailableNow(string usagePoolId)
        {
            int? available;
            return usagePoolId != null && _enhancementInitial.TryGetValue(usagePoolId, out available)
                ? available : null;
        }

        internal int? MaterialRemaining(string itemGuid)
        {
            int remaining;
            return itemGuid != null && _materialRemaining.TryGetValue(itemGuid, out remaining)
                ? remaining : (int?)null;
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

    public enum CastingCapacityKind
    {
        // A counted number of further castings the plan can still fund.
        Finite,
        // A verified Unlimited source with no finite enhancement or material
        // demand: not limited, never "unknown".
        Unlimited,
        // The balance or the cost is not known: never shown as zero or free.
        Unknown
    }

    // How many MORE castings of one exact source (with an optional
    // enhancement set) the plan could still fund, and what stops it.
    public sealed class CastingCapacityEstimate
    {
        internal CastingCapacityEstimate(
            CastingCapacityKind kind,
            int additionalCastings,
            bool lowerBound,
            CastingCostCategory? limitingCategory,
            string limitingPoolKey,
            string stopReason,
            int? nativeRemaining,
            int? nativeAvailableNow,
            string unknownReason)
        {
            Kind = kind;
            AdditionalCastings = kind == CastingCapacityKind.Finite ? additionalCastings : 0;
            IsLowerBound = lowerBound;
            LimitingCategory = limitingCategory;
            LimitingPoolKey = limitingPoolKey ?? string.Empty;
            StopReason = stopReason ?? string.Empty;
            NativeRemaining = nativeRemaining;
            NativeAvailableNow = nativeAvailableNow;
            UnknownReason = unknownReason ?? string.Empty;
        }

        public CastingCapacityKind Kind { get; private set; }
        // Further castings the plan can fund now (Finite only; 0 otherwise).
        public int AdditionalCastings { get; private set; }
        // The count reached CastingCapacity.CountCap: "at least" this many.
        public bool IsLowerBound { get; private set; }
        // Which component ran out first (Finite with a stop only).
        public CastingCostCategory? LimitingCategory { get; private set; }
        public string LimitingPoolKey { get; private set; }
        // The ledger's own refusal that ended the count.
        public string StopReason { get; private set; }
        // The source's native pool after every reservation of the plan, and
        // before any: units, or available prepared slots of the whole pool.
        public int? NativeRemaining { get; private set; }
        public int? NativeAvailableNow { get; private set; }
        public string UnknownReason { get; private set; }

        public bool CanFundAnother
        {
            get
            {
                return Kind == CastingCapacityKind.Unlimited ||
                    (Kind == CastingCapacityKind.Finite && AdditionalCastings > 0);
            }
        }
    }

    // What a compiled plan can still fund once every reservation it made is
    // in place. Each question reserves further hypothetical castings with the
    // plan's OWN ledger rules (atomic cost vectors, shared pools, linked
    // prepared slots, enhancement pools, materials) on an isolated copy of
    // the plan's final ledger, so no view keeps a second ledger and asking
    // never changes the plan. It answers "how many more of this exact
    // source", which is why two sources sharing one pool each report the
    // shared balance: their counts are alternatives, never a sum.
    public sealed class CastingCapacity
    {
        // Counting stops here; a count that reaches it is a lower bound.
        public const int CountCap = 99;
        private const string ProbeCastingId = "capacity-probe";
        private readonly CastingBudgetLedger _final;

        internal CastingCapacity(CastingBudgetLedger final)
        {
            _final = final ?? throw new ArgumentNullException("final");
        }

        public CastingCapacityEstimate AdditionalCastings(
            ProviderSnapshot provider,
            IEnumerable<CastEnhancementSnapshot> enhancements = null)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            List<CastEnhancementSnapshot> selected = (enhancements ??
                new CastEnhancementSnapshot[0]).Where(value => value != null).ToList();
            string poolKey = provider.ResourcePoolKey;
            ResourcePoolKind? kind = _final.NativeKind(poolKey);
            int? remaining = _final.NativeRemaining(poolKey);
            int? available = _final.NativeAvailableNow(poolKey);
            if (kind == null)
                return Unknown("resource-pool-unknown:" + poolKey, remaining, available);
            // Review M1: a zero cost on a finite pool is an unverified cost,
            // not a free cast; counting it would never end and would lie.
            if (kind != ResourcePoolKind.Unlimited && kind != ResourcePoolKind.PreparedSlots &&
                provider.UnitsPerCast == 0)
                return Unknown("cost-unverified:" + poolKey, remaining, available);
            foreach (CastEnhancementSnapshot enhancement in selected)
                if (_final.EnhancementAvailableNow(enhancement.UsagePoolId) == null)
                    return Unknown("enhancement-balance-unknown:" + enhancement.EnhancementId,
                        remaining, available);
            if (kind == ResourcePoolKind.Unlimited && provider.MaterialComponent == null &&
                selected.Count == 0)
                return new CastingCapacityEstimate(CastingCapacityKind.Unlimited, 0, false,
                    null, null, null, remaining, available, null);
            CastingBudgetLedger probe = _final.Clone();
            int count = 0;
            string stop = null;
            while (count < CountCap)
            {
                IReadOnlyList<CastingDemand> demands = probe.DemandsFor(provider, selected);
                IReadOnlyList<CastingCostLine> cost;
                string reason;
                if (!probe.TryReserveAtomically(ProbeCastingId, provider, demands,
                        out cost, out reason))
                {
                    stop = reason ?? "reservation-refused";
                    break;
                }
                count++;
            }
            CastingCostCategory? category = null;
            string limitingPool = null;
            if (stop != null)
                Classify(stop, provider, selected, probe, out category, out limitingPool);
            return new CastingCapacityEstimate(CastingCapacityKind.Finite, count,
                stop == null, category, limitingPool, stop, remaining, available, null);
        }

        // The kind of a native pool the plan knows (an Unlimited pool's
        // "remaining" is not a count); null for an unknown pool.
        public ResourcePoolKind? NativeKind(string poolKey)
        {
            return _final.NativeKind(poolKey);
        }

        // The source's native pool after the plan (units or available
        // prepared slots); null for an unknown pool.
        public int? NativeRemaining(string poolKey)
        {
            return _final.NativeRemaining(poolKey);
        }

        public int? NativeAvailableNow(string poolKey)
        {
            return _final.NativeAvailableNow(poolKey);
        }

        // An enhancement usage pool after the plan; null when its balance is
        // unknown (never zero).
        public int? EnhancementRemaining(string usagePoolId)
        {
            return _final.EnhancementRemaining(usagePoolId);
        }

        public int? EnhancementAvailableNow(string usagePoolId)
        {
            return _final.EnhancementAvailableNow(usagePoolId);
        }

        private static CastingCapacityEstimate Unknown(string reason, int? remaining,
            int? available)
        {
            return new CastingCapacityEstimate(CastingCapacityKind.Unknown, 0, false,
                null, null, null, remaining, available, reason);
        }

        private static void Classify(string stop, ProviderSnapshot provider,
            List<CastEnhancementSnapshot> selected, CastingBudgetLedger probe,
            out CastingCostCategory? category, out string pool)
        {
            if (stop.StartsWith("enhancement-pool-exhausted:", StringComparison.Ordinal))
            {
                category = CastingCostCategory.EnhancementPool;
                // The pool whose remaining balance cannot cover this casting's
                // combined demand on it.
                pool = selected
                    .GroupBy(value => value.UsagePoolId, StringComparer.Ordinal)
                    .Where(group => probe.EnhancementRemaining(group.Key) != null &&
                        probe.EnhancementRemaining(group.Key).Value <
                            group.Sum(value => value.UsageUnitsPerCast))
                    .Select(group => group.Key).FirstOrDefault();
                return;
            }
            if (stop.StartsWith("material-unavailable:", StringComparison.Ordinal))
            {
                category = CastingCostCategory.Material;
                pool = provider.MaterialComponent == null ? null
                    : provider.MaterialComponent.ItemGuid;
                return;
            }
            category = CastingCostCategory.NativePool;
            pool = provider.ResourcePoolKey;
        }
    }
}
