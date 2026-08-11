using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.Planning
{
    public sealed class ResourceReservation
    {
        internal ResourceReservation(string poolKey, int units, IEnumerable<string> tokenIds)
        {
            PoolKey = poolKey;
            Units = units;
            TokenIds = new ReadOnlyCollection<string>(tokenIds.OrderBy(v => v, StringComparer.Ordinal).ToList());
        }

        public string PoolKey { get; private set; }
        public int Units { get; private set; }
        public IReadOnlyList<string> TokenIds { get; private set; }
    }

    public sealed class ResourceLedger
    {
        private readonly Dictionary<string, PoolState> _pools;

        public ResourceLedger(IEnumerable<ResourcePoolSnapshot> pools)
        {
            _pools = (pools ?? throw new ArgumentNullException("pools"))
                .ToDictionary(p => p.PoolKey, p => new PoolState(p), StringComparer.Ordinal);
        }

        public bool TryReserve(
            ProviderSnapshot provider,
            out ResourceReservation reservation,
            out string reason)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            PoolState pool;
            if (!_pools.TryGetValue(provider.ResourcePoolKey, out pool))
                throw new ArgumentException("Provider pool is absent from the ledger.", "provider");
            if (pool.Kind == ResourcePoolKind.Unlimited)
            {
                reservation = new ResourceReservation(pool.Key, 0, new string[0]);
                reason = string.Empty;
                return true;
            }
            if (pool.Kind != ResourcePoolKind.PreparedSlots)
            {
                if (pool.Remaining < provider.UnitsPerCast)
                    return Fail("insufficient-shared-resource", out reservation, out reason);
                pool.Remaining -= provider.UnitsPerCast;
                reservation = new ResourceReservation(pool.Key, provider.UnitsPerCast, new string[0]);
                reason = string.Empty;
                return true;
            }

            foreach (string tokenId in provider.EligibleTokenIds)
            {
                TokenState token;
                if (!pool.Tokens.TryGetValue(tokenId, out token) || !token.Available) continue;
                var required = new HashSet<string>(StringComparer.Ordinal) { tokenId };
                foreach (string linked in token.LinkedTokenIds) required.Add(linked);
                bool available = true;
                foreach (string requiredId in required)
                {
                    TokenState requiredToken;
                    if (!pool.Tokens.TryGetValue(requiredId, out requiredToken) || !requiredToken.Available)
                    {
                        available = false;
                        break;
                    }
                }
                if (!available) continue;
                foreach (string requiredId in required) pool.Tokens[requiredId].Available = false;
                reservation = new ResourceReservation(pool.Key, required.Count, required);
                reason = string.Empty;
                return true;
            }
            return Fail("no-eligible-prepared-token", out reservation, out reason);
        }

        public int GetRemaining(string poolKey)
        {
            PoolState pool;
            if (!_pools.TryGetValue(poolKey, out pool)) throw new ArgumentException("Unknown pool.", "poolKey");
            return pool.Kind == ResourcePoolKind.PreparedSlots
                ? pool.Tokens.Values.Count(t => t.Available)
                : pool.Remaining;
        }

        private static bool Fail(
            string failure,
            out ResourceReservation reservation,
            out string reason)
        {
            reservation = null;
            reason = failure;
            return false;
        }

        private sealed class PoolState
        {
            internal PoolState(ResourcePoolSnapshot snapshot)
            {
                Key = snapshot.PoolKey;
                Kind = snapshot.Kind;
                Remaining = snapshot.Remaining;
                Tokens = snapshot.Tokens.ToDictionary(t => t.TokenId, t => new TokenState(t), StringComparer.Ordinal);
            }

            internal string Key;
            internal ResourcePoolKind Kind;
            internal int Remaining;
            internal Dictionary<string, TokenState> Tokens;
        }

        private sealed class TokenState
        {
            internal TokenState(ResourceTokenSnapshot snapshot)
            {
                Available = snapshot.Available;
                LinkedTokenIds = snapshot.LinkedTokenIds;
            }

            internal bool Available;
            internal IReadOnlyList<string> LinkedTokenIds;
        }
    }
}
