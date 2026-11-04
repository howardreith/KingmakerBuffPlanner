using System.Collections.Generic;
using KingmakerBuffPlanner.Domain.Identity;

namespace KingmakerBuffPlanner.Domain.Effects
{
    public static class DiscoveredEffectLookup
    {
        // Catalogue IDs group equivalent effects; their representative can
        // reference a different spell. Validation and projection use the
        // selected ability's discovery graph, including its reference wrapper.
        // Only older inputs lacking that key fall back to the source alias.
        // A present but null/unsupported graph must never borrow a valid one.
        public static EffectExpression ForAbility(
            IReadOnlyDictionary<string, EffectExpression> effects,
            AbilityKey ability, string legacySourceId)
        {
            EffectExpression expression;
            if (effects == null) return null;
            if (ability != null && effects.TryGetValue(ability.Canonical, out expression))
                return expression;
            return legacySourceId != null && effects.TryGetValue(legacySourceId, out expression)
                ? expression : null;
        }
    }
}
