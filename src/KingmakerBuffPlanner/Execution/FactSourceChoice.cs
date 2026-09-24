using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerBuffPlanner.Execution
{
    // One of a caster's owned abilities that matches an authored Fact or
    // AbilityResource provider (same base ability, variant and metamagic).
    public sealed class FactSourceCandidate
    {
        public FactSourceCandidate(string identity, bool spellbookBound, bool resourceBound, string poolKey)
        {
            Identity = identity ?? string.Empty;
            SpellbookBound = spellbookBound;
            ResourceBound = resourceBound;
            PoolKey = poolKey ?? string.Empty;
        }

        public string Identity { get; private set; }
        // Bound to a spellbook: discovery never offers those as facts.
        public bool SpellbookBound { get; private set; }
        // Spends an ability resource (AbilityResource); otherwise free (Fact).
        public bool ResourceBound { get; private set; }
        // The pool discovery prices it from.
        public string PoolKey { get; private set; }

        public string Describe()
        {
            return Identity + "/" + (SpellbookBound ? "spellbook" : ResourceBound ? "resource" : "free") +
                "/" + PoolKey;
        }
    }

    // Which owned ability a Fact or AbilityResource casting is cast through
    // (batch 3 review A3): one of the provider's own kind (free or
    // resource-bound, never the other), unbound to a spellbook, and, when
    // the step reserved a pool, exactly from that pool. Candidates sharing
    // that pool spend the same thing and are equivalent; when none is left
    // the casting is refused, never moved to a source of another cost.
    public static class FactSourceChoice
    {
        public const string UnavailablePrefix = "fact-source-unavailable:";

        public static FactSourceCandidate Choose(IEnumerable<FactSourceCandidate> candidates,
            bool resourceBound, string reservedPoolKey, out int equivalents, out string refusal)
        {
            refusal = null;
            equivalents = 0;
            List<FactSourceCandidate> seen = (candidates ?? new FactSourceCandidate[0])
                .Where(value => value != null).ToList();
            List<FactSourceCandidate> eligible = seen.Where(value => !value.SpellbookBound &&
                value.ResourceBound == resourceBound &&
                (reservedPoolKey == null || string.Equals(value.PoolKey, reservedPoolKey, StringComparison.Ordinal)))
                .ToList();
            if (eligible.Count == 0)
            {
                refusal = UnavailablePrefix + (resourceBound ? "resource" : "free") + ":" +
                    (reservedPoolKey ?? "unreserved") + ";seen=" +
                    string.Join(",", seen.Select(value => value.Describe()).ToArray());
                return null;
            }
            equivalents = eligible.Count(value => string.Equals(value.PoolKey, eligible[0].PoolKey,
                StringComparison.Ordinal));
            return eligible[0];
        }
    }
}
