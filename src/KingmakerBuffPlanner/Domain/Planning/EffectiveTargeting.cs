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
                if (selected.Any(value => !value.IsApplicable(
                        candidate.Provider))) continue;
                ProviderPlanningOption effective = candidate;
                foreach (ICastTargetingModifier modifier in _modifiers)
                {
                    effective = modifier.Apply(context, effective);
                    if (effective == null) break;
                }
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
}
