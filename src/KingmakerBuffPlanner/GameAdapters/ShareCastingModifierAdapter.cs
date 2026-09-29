using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    // The pure planning-side Share Transmutation (everyday-use v1.2 §7,
    // review addendum §3). ONE registration resolves the exact casting's
    // caster through immutable VERIFIED capability facts — the
    // targeting-affecting enhancement snapshots the installed-provider
    // integration produced (feature ownership, the caster's own reservoir
    // identity and per-use cost) — so a casting by any capable caster is
    // costed against ITS OWN reservoir regardless of party order, and an
    // incapable caster is refused honestly. It expands a PERSONAL
    // transmutation to the verified legal allies, declares the reservoir
    // demand in the compiler's atomic cost vector, and never arms a live
    // toggle, reads live game state or spends anything. Deterministic:
    // same facts → same result.
    public sealed class ShareCastingModifier : ICastingTargetingModifier
    {
        public const string Id = "share-transmutation";

        // One verified capability: the feature-owning caster's reservoir
        // pool and per-use cost, exactly as the installed provider
        // integration reported them.
        public sealed class ShareCapability
        {
            public ShareCapability(string casterUnitId, string reservoirPoolId,
                int unitsPerUse, int? remainingUses)
            {
                if (string.IsNullOrWhiteSpace(casterUnitId))
                    throw new ArgumentException("Caster unit ID is required.", "casterUnitId");
                CasterUnitId = casterUnitId;
                ReservoirPoolId = reservoirPoolId;
                UnitsPerUse = Math.Max(1, unitsPerUse);
                RemainingUses = remainingUses;
            }

            public string CasterUnitId { get; private set; }
            public string ReservoirPoolId { get; private set; }
            public int UnitsPerUse { get; private set; }
            public int? RemainingUses { get; private set; }
        }

        private readonly Dictionary<string, ShareCapability> _capabilities;
        private readonly IReadOnlyList<string> _legalAllies;

        public ShareCastingModifier(IEnumerable<ShareCapability> capabilities,
            IEnumerable<string> legalAllies)
        {
            _capabilities = (capabilities ?? new ShareCapability[0])
                .Where(value => value != null)
                .ToDictionary(value => value.CasterUnitId, StringComparer.Ordinal);
            _legalAllies = (legalAllies ?? new string[0])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList();
        }

        public string ModifierId
        {
            get { return Id; }
        }

        public CastingModifierResult Apply(PlannedCasting casting, ProviderPlanningOption option)
        {
            if (option == null || option.Provider == null)
                return CastingModifierResult.Unavailable("share-source-unresolved");
            string caster = option.Provider.Key.CasterUnitId;
            ShareCapability capability;
            if (!_capabilities.TryGetValue(caster, out capability))
                return CastingModifierResult.Unavailable("share-feature-unavailable:" + caster);
            if (_legalAllies.Count == 0)
                return CastingModifierResult.Unavailable("share-no-legal-recipient");
            if (capability.RemainingUses != null && capability.RemainingUses.Value <= 0)
                return CastingModifierResult.Unavailable("share-reservoir-exhausted:" + caster);
            // Personal spells reach only their caster; Share extends exactly
            // those to the verified legal allies. A spell that can already
            // target others does not need (and cannot use) Share.
            List<string> reachable = (option.ReachableTargetIds ?? new string[0]).ToList();
            bool personal = reachable.Count == 1 &&
                string.Equals(reachable[0], caster, StringComparison.Ordinal);
            if (!personal)
                return CastingModifierResult.Unavailable(
                    "share-not-needed:the spell can already target others");
            var expanded = new HashSet<string>(reachable, StringComparer.Ordinal);
            foreach (string ally in _legalAllies) expanded.Add(ally);
            return CastingModifierResult.Applied(new ProviderPlanningOption(
                option.Provider, expanded.OrderBy(value => value, StringComparer.Ordinal),
                option.LegalAnchorIds, option.EffectiveCasterLevel,
                option.ExpectedDurationRounds, option.ExecutionStrategy,
                option.ExecutionStrategyReason,
                option.RecipientIdsByAnchor == null
                    ? null
                    : option.RecipientIdsByAnchor.ToDictionary(
                        pair => pair.Key,
                        pair => (IEnumerable<string>)pair.Value,
                        StringComparer.Ordinal)));
        }

        public IReadOnlyList<ModifierUsageDemand> UsageDemands(
            PlannedCasting casting, ProviderPlanningOption option)
        {
            ShareCapability capability = null;
            string caster = casting == null ? null : casting.CasterUnitId;
            if (caster != null) _capabilities.TryGetValue(caster, out capability);
            if (capability == null && option != null && option.Provider != null)
                _capabilities.TryGetValue(option.Provider.Key.CasterUnitId, out capability);
            if (capability == null)
            {
                // An unverified caster must never be costed as free: an
                // unsatisfiable demand blocks the casting atomically.
                return new[] { new ModifierUsageDemand("share-unverified-caster", int.MaxValue) };
            }
            return new[]
            {
                new ModifierUsageDemand(capability.ReservoirPoolId, capability.UnitsPerUse)
            };
        }
    }
}
