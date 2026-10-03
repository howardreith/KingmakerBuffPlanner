using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.Domain.Planning
{
    public interface ICastTargetingModifier
    {
        ProviderPlanningOption Apply(
            EffectiveProviderOptionContext context,
            ProviderPlanningOption option);
    }

    public sealed class EffectiveProviderOptionContext
    {
        internal EffectiveProviderOptionContext(
            PartyProviderSnapshot snapshot,
            BuffCastRequest request,
            IEnumerable<CastEnhancementSnapshot> selectedEnhancements)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException("snapshot");
            Request = request ?? throw new ArgumentNullException("request");
            SelectedEnhancements = new ReadOnlyCollection<CastEnhancementSnapshot>(
                (selectedEnhancements ?? new CastEnhancementSnapshot[0])
                    .OrderBy(value => value.EnhancementId,
                        StringComparer.Ordinal).ToList());
        }

        public PartyProviderSnapshot Snapshot { get; private set; }
        public BuffCastRequest Request { get; private set; }
        public IReadOnlyList<CastEnhancementSnapshot> SelectedEnhancements
        { get; private set; }
    }

    /// <summary>
    /// Produces the single assignment-aware provider-option contract consumed
    /// by portrait legality, preview, planning, and execution preflight.
    /// Base options already include passive native AbilityData targeting; an
    /// explicit targeting modifier may only narrow or expand one proven option.
    /// Unknown, incompatible, or inapplicable persisted selections fail closed.
    /// </summary>
    public sealed class EffectiveProviderOptionResolver
    {
        private readonly IReadOnlyList<ICastTargetingModifier> _modifiers;

        public EffectiveProviderOptionResolver(
            IEnumerable<ICastTargetingModifier> modifiers = null)
        {
            _modifiers = new ReadOnlyCollection<ICastTargetingModifier>(
                (modifiers ?? new ICastTargetingModifier[0])
                    .Where(value => value != null).ToList());
        }

        public IReadOnlyList<ProviderPlanningOption> Resolve(
            PartyProviderSnapshot snapshot,
            BuffCastRequest request,
            IEnumerable<ProviderPlanningOption> baseOptions,
            IEnumerable<CastEnhancementSnapshot> enhancementCatalog = null)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            if (request == null) throw new ArgumentNullException("request");
            var catalog = (enhancementCatalog ??
                new CastEnhancementSnapshot[0]).Where(value => value != null)
                .GroupBy(value => value.EnhancementId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(),
                    StringComparer.Ordinal);
            var selected = new List<CastEnhancementSnapshot>();
            foreach (string id in request.EnhancementIds)
            {
                CastEnhancementSnapshot enhancement;
                if (!catalog.TryGetValue(id, out enhancement))
                    return Empty();
                selected.Add(enhancement);
            }
            if (!CastEnhancementSnapshot.AreCompatible(selected))
                return Empty();

            var abilities = new HashSet<string>(request.Source.Abilities
                .Select(value => value.Canonical), StringComparer.Ordinal);
            var context = new EffectiveProviderOptionContext(snapshot,
                request, selected);
            var result = new List<ProviderPlanningOption>();
            foreach (ProviderPlanningOption candidate in (baseOptions ??
                throw new ArgumentNullException("baseOptions"))
                .Where(value => value != null && abilities.Contains(
                    value.Provider.Key.Ability.Canonical))
                .OrderBy(value => value.Provider.Key.Canonical,
                    StringComparer.Ordinal))
            {
                // Pins are hard eligibility constraints, enforced by the same
                // resolver the planner uses: a pinned caster, spellbook, or
                // exact provider removes every other candidate before any
                // ranking or targeting work.
                if (request.CasterUnitId != null && !string.Equals(
                        candidate.Provider.Key.CasterUnitId, request.CasterUnitId,
                        StringComparison.Ordinal)) continue;
                if (request.SpellbookGuid != null && !string.Equals(
                        candidate.Provider.Key.SpellbookGuid, request.SpellbookGuid,
                        StringComparison.Ordinal)) continue;
                if (request.ProviderKeyConstraint != null && !string.Equals(
                        candidate.Provider.Key.Canonical, request.ProviderKeyConstraint,
                        StringComparison.Ordinal)) continue;
                if (selected.Any(value => !value.IsApplicable(
                        candidate.Provider))) continue;
                ProviderPlanningOption effective = candidate;
                foreach (ICastTargetingModifier modifier in _modifiers)
                {
                    effective = modifier.Apply(context, effective);
                    if (effective == null) break;
                }
                if (effective != null)
                    effective = CastEnhancementExecutionPolicy.Apply(
                        context, effective);
                if (effective != null) result.Add(effective);
            }
            return new ReadOnlyCollection<ProviderPlanningOption>(result);
        }

        private static IReadOnlyList<ProviderPlanningOption> Empty()
        {
            return new ReadOnlyCollection<ProviderPlanningOption>(
                new List<ProviderPlanningOption>());
        }
    }

    /// <summary>
    /// Resolves execution requirements for the complete selected enhancement
    /// set after targeting modifiers have produced the effective option.
    /// A provider-direct capability is assignment scoped; any legacy native
    /// requirement still wins and preserves the safe animated path.
    /// </summary>
    internal static class CastEnhancementExecutionPolicy
    {
        internal static ProviderPlanningOption Apply(
            EffectiveProviderOptionContext context,
            ProviderPlanningOption option)
        {
            if (context == null) throw new ArgumentNullException("context");
            return Apply(context.SelectedEnhancements, option);
        }

        // The same rule for an explicit casting's applied enhancements (the
        // casting-first compiler has no classic request context).
        internal static ProviderPlanningOption Apply(
            IEnumerable<CastEnhancementSnapshot> selectedEnhancements,
            ProviderPlanningOption option)
        {
            if (option == null) return null;
            CastEnhancementSnapshot[] selected = (selectedEnhancements ??
                    new CastEnhancementSnapshot[0]).Where(value => value != null)
                .ToArray();
            CastEnhancementSnapshot[] native = selected.Where(value =>
                value.RequiresNativeCommand).ToArray();
            if (native.Length != 0)
            {
                if (option.ExecutionStrategy ==
                    CastExecutionStrategy.NativeCommandRequired)
                    return option;
                return WithStrategy(option,
                    CastExecutionStrategy.NativeCommandRequired,
                    "enhancement-native-command-required:" + string.Join(
                        ",", native.Select(value => value.EnhancementId)
                            .OrderBy(value => value,
                                StringComparer.Ordinal).ToArray()));
            }

            string[] providers = selected.Select(value =>
                    value.DirectCastProviderId)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
            if (providers.Length == 0) return option;
            if (option.ExecutionStrategy ==
                    CastExecutionStrategy.AnimatedFallback ||
                option.ExecutionStrategy ==
                    CastExecutionStrategy.NativeCommandRequired)
                return option;
            if (providers.Length != 1 || option.ExecutionStrategy ==
                    CastExecutionStrategy.StickyTouchDeliveryRuleCast)
                return WithStrategy(option,
                    CastExecutionStrategy.NativeCommandRequired,
                    providers.Length != 1
                        ? "multiple-direct-cast-providers-native-command-required"
                        : "provider-direct-sticky-touch-native-command-required");
            return WithStrategy(option,
                CastExecutionStrategy.ProviderDirectRuleCast,
                "provider-direct-cast:" + providers[0] +
                ";enhancements:" + string.Join(",", selected.Select(value =>
                    value.EnhancementId).OrderBy(value => value,
                        StringComparer.Ordinal).ToArray()) +
                ";base:" + option.ExecutionStrategyReason);
        }

        private static ProviderPlanningOption WithStrategy(
            ProviderPlanningOption option, CastExecutionStrategy strategy,
            string reason)
        {
            return new ProviderPlanningOption(option.Provider,
                option.ReachableTargetIds, option.LegalAnchorIds,
                option.EffectiveCasterLevel, option.ExpectedDurationRounds,
                strategy, reason, option.RecipientIdsByAnchor.ToDictionary(
                    pair => pair.Key,
                    pair => (IEnumerable<string>)pair.Value,
                    StringComparer.Ordinal));
        }
    }
}
