using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.UI
{
    // An effective provider option together with the child assignment whose
    // request produced it. A single provider can legitimately carry different
    // effective targeting under different assignments (self-cast vs Share),
    // so identity travels with the option all the way to presentation.
    public sealed class AssignmentProviderOption
    {
        internal AssignmentProviderOption(string assignmentId,
            ProviderPlanningOption option)
        {
            AssignmentId = assignmentId ?? string.Empty;
            Option = option ?? throw new ArgumentNullException("option");
        }

        public string AssignmentId { get; private set; }
        public ProviderPlanningOption Option { get; private set; }
    }

    // Assignment-aware eligibility used by the target picker and legality
    // summaries. Reuses the planner's own resolver so the UI never implements
    // a contradictory eligibility rule: pins (caster/spellbook/provider) and
    // enhancement selections are enforced by the same code path that plans.
    public static class AssignmentEligibility
    {
        // Resolves one request per child; per-assignment results keep their
        // identity — a provider appearing under several assignments appears
        // once per assignment with that assignment's effective targeting.
        public static IReadOnlyList<AssignmentProviderOption> ResolveChildren(
            BuffSourceDefinition definition,
            ExistingEffectPolicy policy,
            IReadOnlyCollection<string> ignoredMarkers,
            IEnumerable<CastingAssignmentChild> children,
            EffectiveProviderOptionResolver targeting,
            PartyProviderSnapshot snapshot,
            IEnumerable<ProviderPlanningOption> baseOptions,
            IEnumerable<CastEnhancementSnapshot> enhancementCatalog)
        {
            if (definition == null) throw new ArgumentNullException("definition");
            if (children == null) throw new ArgumentNullException("children");
            if (targeting == null) throw new ArgumentNullException("targeting");
            var results = new List<AssignmentProviderOption>();
            foreach (CastingAssignmentChild child in children)
            {
                var request = new BuffCastRequest(definition,
                    child.TargetUnitIds, policy, ignoredMarkers,
                    child.Enhancements,
                    child.AssignmentId, child.CasterUnitId, child.ProviderKey,
                    child.SpellbookGuid, child.Order);
                foreach (ProviderPlanningOption option in targeting.Resolve(
                        snapshot, request, baseOptions, enhancementCatalog))
                    results.Add(new AssignmentProviderOption(child.AssignmentId, option));
            }
            return new ReadOnlyCollection<AssignmentProviderOption>(results);
        }

        // Assignment-specific legality: the selected child's pins and
        // enhancements decide; another child's reach never leaks in.
        public static bool IsTargetLegalForAssignment(
            IEnumerable<AssignmentProviderOption> resolved, string assignmentId,
            string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId)) return false;
            foreach (AssignmentProviderOption candidate in resolved ?? 
                new AssignmentProviderOption[0])
                if (string.Equals(candidate.AssignmentId, assignmentId,
                        StringComparison.Ordinal) &&
                    candidate.Option.ReachableTargetIds.Contains(unitId))
                    return true;
            return false;
        }

        // Source-level presentation aggregate: which units ANY child can
        // reach. This is a display fact, never an executable union.
        public static IReadOnlyCollection<string> ReachableUnits(
            IEnumerable<AssignmentProviderOption> resolved)
        {
            var units = new HashSet<string>(StringComparer.Ordinal);
            foreach (AssignmentProviderOption candidate in resolved ??
                new AssignmentProviderOption[0])
                foreach (string unitId in candidate.Option.ReachableTargetIds)
                    units.Add(unitId);
            return units;
        }
    }

    // Value shape for one child's request, decoupled from persistence types
    // so eligibility services can be tested without profiles.
    public sealed class CastingAssignmentChild
    {
        public CastingAssignmentChild(string assignmentId, int order,
            string casterUnitId, string spellbookGuid, string providerKey,
            IReadOnlyCollection<string> targetUnitIds,
            IReadOnlyCollection<EnhancementRequest> enhancements)
        {
            AssignmentId = assignmentId ?? string.Empty;
            Order = order;
            CasterUnitId = casterUnitId;
            SpellbookGuid = spellbookGuid;
            ProviderKey = providerKey;
            TargetUnitIds = targetUnitIds ?? new string[0];
            Enhancements = enhancements ?? new EnhancementRequest[0];
        }

        public string AssignmentId { get; private set; }
        public int Order { get; private set; }
        public string CasterUnitId { get; private set; }
        public string SpellbookGuid { get; private set; }
        public string ProviderKey { get; private set; }
        public IReadOnlyCollection<string> TargetUnitIds { get; private set; }
        public IReadOnlyCollection<EnhancementRequest> Enhancements { get; private set; }
    }
}
