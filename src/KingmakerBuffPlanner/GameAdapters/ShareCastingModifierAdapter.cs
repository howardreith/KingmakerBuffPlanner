using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    // The pure planning-side Share Transmutation (everyday-use v1.2 §7).
    // It expands a PERSONAL transmutation's reachable set to the verified
    // legal allies from the party snapshot and declares the verified native
    // reservoir demand; it never arms a live toggle, never reads live game
    // state and never spends anything. The same construction always yields
    // the same result (recompilation cannot leak state). Execution-time
    // native setup/restore is the executor's business, not this class's.
    public sealed class ShareCastingModifier : ICastingTargetingModifier
    {
        public const string Id = "share-transmutation";
        private const int ReservoirUnitsPerUse = 1;

        private readonly string _reservoirPoolId;
        private readonly IReadOnlyList<string> _legalAllies;

        public ShareCastingModifier(string reservoirPoolId, IEnumerable<string> legalAllies)
        {
            if (string.IsNullOrWhiteSpace(reservoirPoolId))
                throw new ArgumentException("Reservoir pool ID is required.", "reservoirPoolId");
            _reservoirPoolId = reservoirPoolId;
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
            if (_legalAllies.Count == 0)
                return CastingModifierResult.Unavailable("share-feature-unavailable");
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
            // The verified Brown-Fur Share toggle spends the same arcane
            // reservoir as Powerful Change; declaring it here puts both in
            // ONE atomic cost vector (combined demand, reserved once).
            return new[] { new ModifierUsageDemand(_reservoirPoolId, ReservoirUnitsPerUse) };
        }
    }
}
