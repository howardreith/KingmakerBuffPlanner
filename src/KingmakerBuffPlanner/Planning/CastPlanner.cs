using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.Planning
{
    public sealed class CastPlanner
    {
        private readonly EffectPresenceEvaluator _presence = new EffectPresenceEvaluator();

        public CastPlan Plan(
            PartyProviderSnapshot snapshot,
            BuffCastRequest request,
            IEnumerable<ProviderPlanningOption> providerOptions,
            ProviderSelectionPolicy selectionPolicy,
            ActiveEffectSnapshot activeEffects)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            if (request == null) throw new ArgumentNullException("request");
            if (selectionPolicy == null) throw new ArgumentNullException("selectionPolicy");
            if (activeEffects == null) throw new ArgumentNullException("activeEffects");
            var options = (providerOptions ?? throw new ArgumentNullException("providerOptions")).ToList();
            ValidateOptions(snapshot, options);
            options = options.Where(o => o.Provider.Key.Ability.Equals(request.Source.Ability)).ToList();
            var units = snapshot.Units.ToDictionary(u => u.UnitId, StringComparer.Ordinal);
            var outcomes = new List<TargetPlanOutcome>();
            var pending = new List<string>();
            var ignored = new HashSet<string>(request.IgnoredEffectIds, StringComparer.Ordinal);
            foreach (string targetId in request.TargetUnitIds)
            {
                UnitSnapshot unit;
                if (!units.TryGetValue(targetId, out unit))
                {
                    outcomes.Add(Unfulfilled(targetId, "target-not-in-party"));
                    continue;
                }
                if (!unit.TargetValidation.Alive || !unit.TargetValidation.Friendly ||
                    !unit.TargetValidation.Targetable)
                {
                    outcomes.Add(Unfulfilled(targetId, "target-currently-invalid"));
                    continue;
                }
                if (request.ExistingEffectPolicy == ExistingEffectPolicy.SkipAlreadyActive)
                {
                    EffectPresenceResult presence = _presence.EvaluateTyped(request.Source.Effects,
                        activeEffects.GetEffects(targetId), ignored);
                    if (presence.Kind == EffectPresenceKind.Complete)
                    {
                        outcomes.Add(new TargetPlanOutcome(targetId,
                            TargetOutcomeKind.SkippedAlreadyActive, "already-active", presence.PresentMarkers));
                        continue;
                    }
                }
                pending.Add(targetId);
            }

            var steps = new List<CastStep>();
            var diagnostics = new List<string>();
            var ledger = new ResourceLedger(snapshot.ResourcePools);
            var castsByProvider = new Dictionary<string, int>(StringComparer.Ordinal);
            var materials = snapshot.Providers.Where(p => p.MaterialComponent != null)
                .GroupBy(p => p.MaterialComponent.ItemGuid, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Max(p => p.MaterialComponent.AvailableCount), StringComparer.Ordinal);
            var poolKinds = snapshot.ResourcePools.ToDictionary(p => p.PoolKey, p => p.Kind, StringComparer.Ordinal);
            if (request.Source.Grouping == CastGroupingKind.MassConfiguredTargets)
                PlanMass(request, pending, options, selectionPolicy, ledger, castsByProvider,
                    poolKinds, materials, steps, outcomes, diagnostics);
            else
                PlanPerTarget(request, pending, options, selectionPolicy, ledger, castsByProvider,
                    poolKinds, materials, steps, outcomes, diagnostics);
            return new CastPlan(steps, outcomes, diagnostics);
        }

        private static void PlanPerTarget(
            BuffCastRequest request,
            IEnumerable<string> pending,
            List<ProviderPlanningOption> options,
            ProviderSelectionPolicy policy,
            ResourceLedger ledger,
            Dictionary<string, int> castsByProvider,
            Dictionary<string, ResourcePoolKind> poolKinds,
            Dictionary<string, int> materials,
            List<CastStep> steps,
            List<TargetPlanOutcome> outcomes,
            List<string> diagnostics)
        {
            var remaining = pending.ToList();
            foreach (string targetId in remaining.ToArray())
            {
                Selection selection = SelectAndReserve(options, policy, ledger, castsByProvider,
                    poolKinds, materials, new[] { targetId }, remaining);
                if (selection == null)
                {
                    outcomes.Add(Unfulfilled(targetId, "no-valid-provider-or-resource"));
                    diagnostics.Add("unfulfilled:" + targetId + ":no-valid-provider-or-resource");
                    remaining.Remove(targetId);
                    continue;
                }
                steps.Add(new CastStep(selection.Option.Provider.Key, selection.Anchor,
                    new[] { targetId }, selection.Reservation, selection.MaterialReservation,
                    request.Source.Effects, false));
                outcomes.Add(new TargetPlanOutcome(targetId, TargetOutcomeKind.Fulfilled,
                    "planned", new string[0]));
                remaining.Remove(targetId);
            }
        }

        private static void PlanMass(
            BuffCastRequest request,
            IEnumerable<string> pending,
            List<ProviderPlanningOption> options,
            ProviderSelectionPolicy policy,
            ResourceLedger ledger,
            Dictionary<string, int> castsByProvider,
            Dictionary<string, ResourcePoolKind> poolKinds,
            Dictionary<string, int> materials,
            List<CastStep> steps,
            List<TargetPlanOutcome> outcomes,
            List<string> diagnostics)
        {
            var remaining = pending.ToList();
            while (remaining.Count != 0)
            {
                Selection selection = SelectAndReserve(options, policy, ledger, castsByProvider,
                    poolKinds, materials, remaining, remaining);
                if (selection == null)
                {
                    foreach (string targetId in remaining)
                        outcomes.Add(Unfulfilled(targetId, "no-valid-mass-provider-or-resource"));
                    diagnostics.Add("unfulfilled-mass-targets:" + string.Join(",", remaining.ToArray()));
                    break;
                }
                string[] covered = remaining.Where(id => selection.Option.ReachableTargetIds.Contains(id))
                    .OrderBy(id => id, StringComparer.Ordinal).ToArray();
                steps.Add(new CastStep(selection.Option.Provider.Key, selection.Anchor,
                    covered, selection.Reservation, selection.MaterialReservation,
                    request.Source.Effects, true));
                foreach (string targetId in covered)
                {
                    outcomes.Add(new TargetPlanOutcome(targetId, TargetOutcomeKind.Fulfilled,
                        "planned-mass", new string[0]));
                    remaining.Remove(targetId);
                }
            }
        }

        private static Selection SelectAndReserve(
            IEnumerable<ProviderPlanningOption> sourceOptions,
            ProviderSelectionPolicy policy,
            ResourceLedger ledger,
            Dictionary<string, int> castsByProvider,
            Dictionary<string, ResourcePoolKind> poolKinds,
            Dictionary<string, int> materials,
            IEnumerable<string> requiredTargets,
            IEnumerable<string> allRemainingTargets)
        {
            string[] required = requiredTargets.ToArray();
            string[] allRemaining = allRemainingTargets.ToArray();
            IEnumerable<ProviderPlanningOption> candidates = sourceOptions.Where(option =>
                !policy.BannedProviderKeys.Contains(option.Provider.Key.Canonical) &&
                HasMaterial(option.Provider, materials) &&
                option.LegalAnchorIds.Count != 0 &&
                required.Any(id => option.ReachableTargetIds.Contains(id)) &&
                IsUnderCap(option.Provider.Key.Canonical, policy, castsByProvider));
            foreach (ProviderPlanningOption option in candidates
                .OrderBy(o => Priority(o, policy))
                .ThenByDescending(o => allRemaining.All(id => o.ReachableTargetIds.Contains(id)))
                .ThenByDescending(o => allRemaining.Count(id => o.ReachableTargetIds.Contains(id)))
                .ThenByDescending(o => o.EffectiveCasterLevel)
                .ThenByDescending(o => o.ExpectedDurationRounds)
                .ThenBy(o => ResourceRank(poolKinds[o.Provider.ResourcePoolKey]))
                .ThenBy(o => o.Provider.Key.Canonical, StringComparer.Ordinal))
            {
                ResourceReservation reservation;
                string reason;
                if (!ledger.TryReserve(option.Provider, out reservation, out reason)) continue;
                MaterialReservation materialReservation = ReserveMaterial(option.Provider, materials);
                string key = option.Provider.Key.Canonical;
                castsByProvider[key] = castsByProvider.ContainsKey(key) ? castsByProvider[key] + 1 : 1;
                string anchor = option.LegalAnchorIds.Contains(option.Provider.Key.CasterUnitId)
                    ? option.Provider.Key.CasterUnitId
                    : option.LegalAnchorIds[0];
                return new Selection(option, anchor, reservation, materialReservation);
            }
            return null;
        }

        private static int Priority(ProviderPlanningOption option, ProviderSelectionPolicy policy)
        {
            int value;
            return policy.ExplicitPriorities.TryGetValue(option.Provider.Key.Canonical, out value)
                ? value
                : int.MaxValue;
        }

        private static bool HasMaterial(ProviderSnapshot provider, Dictionary<string, int> materials)
        {
            MaterialRequirementSnapshot requirement = provider.MaterialComponent;
            if (requirement == null) return true;
            int remaining;
            return materials.TryGetValue(requirement.ItemGuid, out remaining) &&
                remaining >= requirement.RequiredCount;
        }

        private static MaterialReservation ReserveMaterial(
            ProviderSnapshot provider,
            Dictionary<string, int> materials)
        {
            MaterialRequirementSnapshot requirement = provider.MaterialComponent;
            if (requirement == null) return null;
            materials[requirement.ItemGuid] -= requirement.RequiredCount;
            return new MaterialReservation(requirement.ItemGuid, requirement.RequiredCount);
        }

        private static bool IsUnderCap(
            string providerKey,
            ProviderSelectionPolicy policy,
            Dictionary<string, int> castsByProvider)
        {
            int maximum;
            if (!policy.MaximumCasts.TryGetValue(providerKey, out maximum)) return true;
            int used;
            castsByProvider.TryGetValue(providerKey, out used);
            return used < maximum;
        }

        private static int ResourceRank(ResourcePoolKind kind)
        {
            if (kind == ResourcePoolKind.Unlimited || kind == ResourcePoolKind.PreparedSlots) return 0;
            if (kind == ResourcePoolKind.AbilityResource || kind == ResourcePoolKind.ItemCharges) return 1;
            return 2;
        }

        private static void ValidateOptions(
            PartyProviderSnapshot snapshot,
            IEnumerable<ProviderPlanningOption> options)
        {
            var snapshotKeys = new HashSet<string>(snapshot.Providers.Select(p => p.Key.Canonical), StringComparer.Ordinal);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (ProviderPlanningOption option in options)
            {
                if (!snapshotKeys.Contains(option.Provider.Key.Canonical))
                    throw new ArgumentException("Planning option provider is absent from the snapshot.", "options");
                if (!keys.Add(option.Provider.Key.Canonical))
                    throw new ArgumentException("Planning options contain a duplicate provider key.", "options");
            }
        }

        private static TargetPlanOutcome Unfulfilled(string targetId, string reason)
        {
            return new TargetPlanOutcome(targetId, TargetOutcomeKind.Unfulfilled, reason, new string[0]);
        }

        private sealed class Selection
        {
            internal Selection(
                ProviderPlanningOption option,
                string anchor,
                ResourceReservation reservation,
                MaterialReservation materialReservation)
            {
                Option = option;
                Anchor = anchor;
                Reservation = reservation;
                MaterialReservation = materialReservation;
            }

            internal ProviderPlanningOption Option;
            internal string Anchor;
            internal ResourceReservation Reservation;
            internal MaterialReservation MaterialReservation;
        }
    }
}
