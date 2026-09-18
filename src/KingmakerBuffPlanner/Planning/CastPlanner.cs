using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.Planning
{
    public sealed class CastPlanner
    {
        private readonly EffectPresenceEvaluator _presence = new EffectPresenceEvaluator();
        private readonly EffectiveProviderOptionResolver _targeting;

        public CastPlanner(EffectiveProviderOptionResolver targeting = null)
        {
            _targeting = targeting ?? new EffectiveProviderOptionResolver();
        }

        public CastPlan Plan(
            PartyProviderSnapshot snapshot,
            BuffCastRequest request,
            IEnumerable<ProviderPlanningOption> providerOptions,
            ProviderSelectionPolicy selectionPolicy,
            ActiveEffectSnapshot activeEffects,
            IEnumerable<CastEnhancementSnapshot> enhancements = null)
        {
            if (request == null) throw new ArgumentNullException("request");
            return PlanRoutine(snapshot, new[] { request }, providerOptions,
                selectionPolicy, activeEffects, enhancements);
        }

        public CastPlan PlanRoutine(
            PartyProviderSnapshot snapshot,
            IEnumerable<BuffCastRequest> requests,
            IEnumerable<ProviderPlanningOption> providerOptions,
            ProviderSelectionPolicy selectionPolicy,
            ActiveEffectSnapshot activeEffects,
            IEnumerable<CastEnhancementSnapshot> enhancements = null)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            if (selectionPolicy == null) throw new ArgumentNullException("selectionPolicy");
            if (activeEffects == null) throw new ArgumentNullException("activeEffects");
            var requestList = (requests ?? throw new ArgumentNullException("requests")).ToList();
            if (requestList.Any(r => r == null)) throw new ArgumentException("Routine request is null.", "requests");
            var options = (providerOptions ?? throw new ArgumentNullException("providerOptions")).ToList();
            ValidateOptions(snapshot, options);
            var units = snapshot.Units.ToDictionary(u => u.UnitId, StringComparer.Ordinal);
            var outcomes = new List<TargetPlanOutcome>();
            var steps = new List<CastStep>();
            var diagnostics = new List<string>();
            var ledger = new ResourceLedger(snapshot.ResourcePools);
            var enhancementList = (enhancements ?? new CastEnhancementSnapshot[0]).ToList();
            if (enhancementList.Any(value => value == null) ||
                enhancementList.Select(value => value.EnhancementId).Distinct(StringComparer.Ordinal).Count() != enhancementList.Count)
                throw new ArgumentException("Enhancement catalog contains null or duplicate IDs.", "enhancements");
            var enhancementById = enhancementList.ToDictionary(value => value.EnhancementId, StringComparer.Ordinal);
            var enhancementRemaining = new Dictionary<string, int?>(StringComparer.Ordinal);
            foreach (IGrouping<string, CastEnhancementSnapshot> pool in enhancementList
                .GroupBy(value => value.UsagePoolId, StringComparer.Ordinal))
            {
                int?[] amounts = pool.Select(value => value.RemainingUses)
                    .Distinct().ToArray();
                if (amounts.Length != 1)
                    throw new ArgumentException("Enhancements sharing a usage pool disagree on remaining uses.",
                        "enhancements");
                enhancementRemaining.Add(pool.Key, amounts[0]);
            }
            // Budget identity: if an enhancement usage pool claims the same key
            // as a native resource pool, it is the same native budget and must
            // never be spent twice. Diagnose the collision instead of merging
            // two independent balances silently.
            foreach (ResourcePoolSnapshot pool in snapshot.ResourcePools)
                if (enhancementRemaining.ContainsKey(pool.PoolKey))
                    throw new ArgumentException(
                        "Enhancement usage pool collides with a native resource pool key: " + pool.PoolKey,
                        "enhancements");
            var castsByProvider = new Dictionary<string, int>(StringComparer.Ordinal);
            var materials = snapshot.Providers.Where(p => p.MaterialComponent != null)
                .GroupBy(p => p.MaterialComponent.ItemGuid, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Max(p => p.MaterialComponent.AvailableCount), StringComparer.Ordinal);
            var poolKinds = snapshot.ResourcePools.ToDictionary(p => p.PoolKey, p => p.Kind, StringComparer.Ordinal);
            var accounting = new AllocationAccounting(snapshot.ResourcePools, enhancementList);
            // Allocation order is the routine-wide explicit assignment order
            // with stable final tie-breakers; catalog sorting and dictionary
            // iteration order cannot influence it.
            foreach (BuffCastRequest request in requestList
                .OrderBy(r => r.Order)
                .ThenBy(r => r.Source.SourceId, StringComparer.Ordinal)
                .ThenBy(r => r.AssignmentId, StringComparer.Ordinal))
            {
                var pending = new List<string>();
                var ignored = new HashSet<string>(request.IgnoredEffectIds, StringComparer.Ordinal);
                foreach (string targetId in request.TargetUnitIds)
                {
                    UnitSnapshot unit;
                    if (!units.TryGetValue(targetId, out unit))
                    {
                        outcomes.Add(Unfulfilled(request, targetId, "target-not-in-party"));
                        continue;
                    }
                    if (!unit.TargetValidation.Alive || !unit.TargetValidation.Friendly ||
                        !unit.TargetValidation.Targetable)
                    {
                        outcomes.Add(Unfulfilled(request, targetId, "target-currently-invalid"));
                        continue;
                    }
                    if (request.ExistingEffectPolicy == ExistingEffectPolicy.SkipAlreadyActive)
                    {
                        EffectPresenceResult presence = _presence.EvaluateTyped(request.Source.Effects,
                            activeEffects.GetEffects(targetId), ignored);
                        if (presence.Kind == EffectPresenceKind.Complete)
                        {
                            // Already-active skips never reserve or demand.
                            outcomes.Add(new TargetPlanOutcome(request.Source.SourceId,
                                request.AssignmentId, targetId,
                                TargetOutcomeKind.SkippedAlreadyActive,
                                request.Source.SourceId + ":already-active", presence.PresentMarkers));
                            continue;
                        }
                    }
                    pending.Add(targetId);
                }
                List<ProviderPlanningOption> sourceOptions = _targeting.Resolve(
                    snapshot, request, options, enhancementList).ToList();
                var context = new PlanContext(request, sourceOptions, selectionPolicy,
                    ledger, castsByProvider, poolKinds, materials, enhancementById,
                    enhancementRemaining, accounting, steps, outcomes, diagnostics);
                if (request.Source.Grouping == CastGroupingKind.MassConfiguredTargets)
                    PlanMass(context, pending);
                else
                    PlanPerTarget(context, pending);
            }
            return new CastPlan(steps, outcomes, diagnostics, accounting.Build());
        }

        private sealed class PlanContext
        {
            internal readonly BuffCastRequest Request;
            internal readonly List<ProviderPlanningOption> Options;
            internal readonly ProviderSelectionPolicy Policy;
            internal readonly ResourceLedger Ledger;
            internal readonly Dictionary<string, int> CastsByProvider;
            internal readonly Dictionary<string, ResourcePoolKind> PoolKinds;
            internal readonly Dictionary<string, int> Materials;
            internal readonly IDictionary<string, CastEnhancementSnapshot> Enhancements;
            internal readonly IDictionary<string, int?> EnhancementRemaining;
            internal readonly AllocationAccounting Accounting;
            internal readonly List<CastStep> Steps;
            internal readonly List<TargetPlanOutcome> Outcomes;
            internal readonly List<string> Diagnostics;

            internal PlanContext(BuffCastRequest request,
                List<ProviderPlanningOption> options, ProviderSelectionPolicy policy,
                ResourceLedger ledger, Dictionary<string, int> castsByProvider,
                Dictionary<string, ResourcePoolKind> poolKinds,
                Dictionary<string, int> materials,
                IDictionary<string, CastEnhancementSnapshot> enhancements,
                IDictionary<string, int?> enhancementRemaining,
                AllocationAccounting accounting, List<CastStep> steps,
                List<TargetPlanOutcome> outcomes, List<string> diagnostics)
            {
                Request = request;
                Options = options;
                Policy = policy;
                Ledger = ledger;
                CastsByProvider = castsByProvider;
                PoolKinds = poolKinds;
                Materials = materials;
                Enhancements = enhancements;
                EnhancementRemaining = enhancementRemaining;
                Accounting = accounting;
                Steps = steps;
                Outcomes = outcomes;
                Diagnostics = diagnostics;
            }
        }

        private static void PlanPerTarget(PlanContext context, IEnumerable<string> pending)
        {
            BuffCastRequest request = context.Request;
            var remaining = pending.ToList();
            foreach (string targetId in remaining.ToArray())
            {
                Selection selection = SelectAndReserve(context, new[] { targetId }, remaining);
                if (selection == null)
                {
                    string failure = request.EnhancementIds.Count == 0
                        ? "no-valid-provider-or-resource" : "requested-enhancement-unavailable";
                    context.Outcomes.Add(Unfulfilled(request, targetId, failure));
                    context.Diagnostics.Add("unfulfilled:" + request.AssignmentId + ":" +
                        targetId + ":" + failure);
                    context.Diagnostics.Add(ProviderSelectionFailure(request, context));
                    RecordFailedDemand(context);
                    remaining.Remove(targetId);
                    continue;
                }
                string[] recipients = ExpectedRecipients(request.Source.Effects, selection.Option,
                    selection.Anchor, new[] { targetId });
                context.Steps.Add(new CastStep(request.Source.SourceId, request.AssignmentId,
                    selection.Option.Provider.Key, selection.Anchor, new[] { targetId },
                    recipients, selection.Reservation, selection.MaterialReservation,
                    request.Source.Effects, false,
                    selection.Option.ExecutionStrategy, selection.Option.ExecutionStrategyReason,
                    selection.AppliedEnhancementIds,
                    EnhancementUsage(selection.AppliedEnhancementIds, context.Enhancements),
                    selection.OmittedEnhancementIds));
                context.Outcomes.Add(new TargetPlanOutcome(request.Source.SourceId,
                    request.AssignmentId, targetId, TargetOutcomeKind.Fulfilled,
                    request.AssignmentId + ":planned" +
                    (selection.OmittedEnhancementIds.Count == 0 ? string.Empty :
                        ":omitted-optional:" + string.Join(",", selection.OmittedEnhancementIds.ToArray())),
                    new string[0]));
                remaining.Remove(targetId);
            }
        }

        private static void PlanMass(PlanContext context, IEnumerable<string> pending)
        {
            BuffCastRequest request = context.Request;
            var remaining = pending.ToList();
            while (remaining.Count != 0)
            {
                Selection selection = SelectAndReserve(context, remaining, remaining);
                if (selection == null)
                {
                    string failure = request.EnhancementIds.Count == 0
                        ? "no-valid-mass-provider-or-resource"
                        : "requested-enhancement-unavailable";
                    foreach (string targetId in remaining)
                        context.Outcomes.Add(Unfulfilled(request, targetId, failure));
                    context.Diagnostics.Add("unfulfilled-mass-targets:" + request.AssignmentId +
                        ":" + failure + ":" + string.Join(",", remaining.ToArray()));
                    context.Diagnostics.Add(ProviderSelectionFailure(request, context));
                    RecordFailedDemand(context);
                    break;
                }
                string[] covered = remaining.Where(id => selection.Option
                    .CoveredTargetIdsForAnchor(selection.Anchor).Contains(id))
                    .OrderBy(id => id, StringComparer.Ordinal).ToArray();
                string[] recipients = ExpectedRecipients(request.Source.Effects, selection.Option,
                    selection.Anchor, covered);
                context.Steps.Add(new CastStep(request.Source.SourceId, request.AssignmentId,
                    selection.Option.Provider.Key, selection.Anchor, covered, recipients,
                    selection.Reservation, selection.MaterialReservation,
                    request.Source.Effects, true,
                    selection.Option.ExecutionStrategy, selection.Option.ExecutionStrategyReason,
                    selection.AppliedEnhancementIds,
                    EnhancementUsage(selection.AppliedEnhancementIds, context.Enhancements),
                    selection.OmittedEnhancementIds));
                foreach (string targetId in covered)
                {
                    context.Outcomes.Add(new TargetPlanOutcome(request.Source.SourceId,
                        request.AssignmentId, targetId, TargetOutcomeKind.Fulfilled,
                        request.AssignmentId + ":planned-mass" +
                        (selection.OmittedEnhancementIds.Count == 0 ? string.Empty :
                            ":omitted-optional:" + string.Join(",", selection.OmittedEnhancementIds.ToArray())),
                        new string[0]));
                    remaining.Remove(targetId);
                }
            }
        }

        private static Selection SelectAndReserve(PlanContext context,
            IEnumerable<string> requiredTargets, IEnumerable<string> allRemainingTargets)
        {
            BuffCastRequest request = context.Request;
            string[] required = requiredTargets.ToArray();
            string[] allRemaining = allRemainingTargets.ToArray();
            IEnumerable<ProviderPlanningOption> eligible = context.Options;
            string pinFailure = null;
            // Pins are hard eligibility constraints applied before any
            // ranking: an unavailable pinned caster/provider stays visible
            // and unresolved rather than falling back to another candidate.
            if (request.CasterUnitId != null)
            {
                IEnumerable<ProviderPlanningOption> pinned = eligible.Where(option =>
                    string.Equals(option.Provider.Key.CasterUnitId, request.CasterUnitId,
                        StringComparison.Ordinal));
                if (!pinned.Any()) pinFailure = "pinned-caster-unavailable:" + request.CasterUnitId;
                else eligible = pinned;
            }
            if (request.SpellbookGuid != null)
            {
                IEnumerable<ProviderPlanningOption> pinned = eligible.Where(option =>
                    string.Equals(option.Provider.Key.SpellbookGuid, request.SpellbookGuid,
                        StringComparison.Ordinal));
                if (!pinned.Any()) pinFailure = pinFailure ?? "pinned-spellbook-unavailable:" + request.SpellbookGuid;
                else eligible = pinned;
            }
            if (request.ProviderKeyConstraint != null)
            {
                IEnumerable<ProviderPlanningOption> pinned = eligible.Where(option =>
                    string.Equals(option.Provider.Key.Canonical, request.ProviderKeyConstraint,
                        StringComparison.Ordinal));
                if (!pinned.Any()) pinFailure = pinFailure ?? "pinned-provider-unavailable:" + request.ProviderKeyConstraint;
                else eligible = pinned;
            }
            if (pinFailure != null)
            {
                context.Diagnostics.Add("pin-unresolved:" + request.AssignmentId +
                    ":" + pinFailure);
                return null;
            }
            // Every intended cast counts its configured enhancement demand once
            // up front — including casts that later proceed without an
            // optional enhancement — so requested/unmet reflect configured
            // intent rather than only the successful subset.
            context.Accounting.RecordEnhancementDemand(
                request.EnhancementSelections
                    .Where(selection => context.Enhancements.ContainsKey(selection.EnhancementId))
                    .Select(selection => selection.EnhancementId),
                context.Enhancements, request.AssignmentId);
            IEnumerable<ProviderPlanningOption> candidates = eligible.Where(option =>
                !context.Policy.BannedProviderKeys.Contains(option.Provider.Key.Canonical) &&
                HasMaterial(option.Provider, context.Materials) &&
                option.LegalAnchorIds.Count != 0 &&
                required.Any(id => option.ReachableTargetIds.Contains(id)) &&
                IsUnderCap(option.Provider.Key.Canonical, context.Policy, context.CastsByProvider));
            foreach (ProviderPlanningOption option in candidates
                .OrderBy(o => Priority(o, context.Policy))
                .ThenByDescending(o => allRemaining.All(id => o.ReachableTargetIds.Contains(id)))
                .ThenByDescending(o => allRemaining.Count(id => o.ReachableTargetIds.Contains(id)))
                .ThenByDescending(o => o.EffectiveCasterLevel)
                .ThenByDescending(o => o.ExpectedDurationRounds)
                .ThenBy(o => ResourceRank(context.PoolKinds[o.Provider.ResourcePoolKey]))
                .ThenBy(o => o.Provider.Key.Canonical, StringComparer.Ordinal))
            {
                EnhancementSetResolution enhancements = ResolveEnhancementSet(context, option);
                if (!enhancements.Available)
                    continue;
                string anchor = option.LegalAnchorIds
                    .Where(value => option.CoveredTargetIdsForAnchor(value)
                        .Any(id => required.Contains(id)))
                    .OrderByDescending(value => required.Contains(value))
                    .ThenByDescending(value => option.CoveredTargetIdsForAnchor(value)
                        .Count(id => allRemaining.Contains(id)))
                    .ThenBy(value => value, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (string.IsNullOrWhiteSpace(anchor)) continue;
                ResourceReservation reservation;
                string reason;
                // A candidate's full requirements — resource pool, material,
                // and enhancement charges — are committed together below; a
                // candidate rejected before that point leaks no reservation.
                if (!context.Ledger.TryReserve(option.Provider, out reservation, out reason)) continue;
                MaterialReservation materialReservation = ReserveMaterial(option.Provider, context.Materials);
                context.Accounting.RecordNativeAllocation(option.Provider.ResourcePoolKey,
                    reservation.Units, request.AssignmentId);
                ReserveEnhancements(enhancements.AppliedIds, context.Enhancements,
                    context.EnhancementRemaining);
                context.Accounting.RecordEnhancementAllocation(enhancements.AppliedIds,
                    context.Enhancements, request.AssignmentId);                string key = option.Provider.Key.Canonical;
                context.CastsByProvider[key] = context.CastsByProvider.ContainsKey(key)
                    ? context.CastsByProvider[key] + 1 : 1;
                return new Selection(option, anchor, reservation, materialReservation,
                    enhancements.AppliedIds, enhancements.OmittedIds);
            }
            return null;
        }

        // Required-by-default enhancement policy with a documented
        // deterministic fallback: optional enhancements are dropped from the
        // END of the request order one at a time, and the whole remaining
        // set is revalidated against this provider before any omission is
        // accepted. Targeting modifiers (for example Share) are never
        // omissible because dropping one can make the recipient illegal.
        private static EnhancementSetResolution ResolveEnhancementSet(
            PlanContext context, ProviderPlanningOption option)
        {
            List<EnhancementRequest> selections = context.Request.EnhancementSelections
                .Where(selection => context.Enhancements.ContainsKey(selection.EnhancementId)
                    ? selection.Required ||
                        !context.Enhancements[selection.EnhancementId].AffectsTargeting
                    : selection.Required)
                .ToList();
            List<string> missing = context.Request.EnhancementSelections
                .Where(selection => !context.Enhancements.ContainsKey(selection.EnhancementId))
                .Select(selection => selection.EnhancementId).ToList();
            List<EnhancementRequest> droppable = selections.Where(selection =>
                !selection.Required && !IsTargetingModifier(context, selection.EnhancementId))
                .ToList();
            List<EnhancementRequest> mandatory = selections.Except(droppable).ToList();
            for (int drop = 0; drop <= droppable.Count; drop++)
            {
                List<EnhancementRequest> candidate = mandatory
                    .Concat(droppable.Take(droppable.Count - drop)).ToList();
                if (missing.Any(id => candidate.Any(selection => selection.EnhancementId == id)))
                    continue;
                if (EnhancementsAvailable(option.Provider,
                        candidate.Select(selection => selection.EnhancementId).ToList(),
                        context.Enhancements, context.EnhancementRemaining))
                    return new EnhancementSetResolution(true,
                        candidate.Select(selection => selection.EnhancementId).ToList(),
                        droppable.Skip(droppable.Count - drop)
                            .Select(selection => selection.EnhancementId).ToList(),
                        null);
            }
            string failure = missing.Count != 0
                ? "enhancement-unknown:" + string.Join(",", missing.ToArray())
                : "requested-enhancement-unavailable";
            return new EnhancementSetResolution(false, new string[0], new string[0], failure);
        }

        private static bool IsTargetingModifier(PlanContext context, string enhancementId)
        {
            CastEnhancementSnapshot enhancement;
            return context.Enhancements.TryGetValue(enhancementId, out enhancement) &&
                enhancement.AffectsTargeting;
        }

        private static void RecordFailedDemand(PlanContext context)
        {
            // Failed casts still count the native pool units of the best
            // remaining candidate when no provider could serve it; enhancement
            // demand was already recorded for the attempt itself.
            ProviderPlanningOption best = context.Options
                .Where(option => !context.Policy.BannedProviderKeys
                    .Contains(option.Provider.Key.Canonical))
                .OrderBy(o => Priority(o, context.Policy))
                .ThenBy(o => o.Provider.Key.Canonical, StringComparer.Ordinal)
                .FirstOrDefault();
            if (best != null)
                context.Accounting.RecordNativeDemand(best.Provider.ResourcePoolKey,
                    best.Provider.UnitsPerCast, context.Request.AssignmentId);
        }

        private static bool EnhancementsAvailable(
            ProviderSnapshot provider,
            IReadOnlyList<string> requestedIds,
            IDictionary<string, CastEnhancementSnapshot> enhancements,
            IDictionary<string, int?> remaining)
        {
            if (requestedIds == null || requestedIds.Count == 0) return true;
            var selected = new List<CastEnhancementSnapshot>();
            foreach (string id in requestedIds)
            {
                CastEnhancementSnapshot enhancement;
                if (!enhancements.TryGetValue(id, out enhancement) ||
                    !enhancement.IsApplicable(provider)) return false;
                selected.Add(enhancement);
            }
            if (!CastEnhancementSnapshot.AreCompatible(selected)) return false;
            foreach (KeyValuePair<string, int> requirement in
                CastEnhancementSnapshot.UsageRequirements(selected))
            {
                int? uses;
                if (!remaining.TryGetValue(requirement.Key, out uses) ||
                    (uses != null && uses.Value < requirement.Value))
                    return false;
            }
            return true;
        }

        private static void ReserveEnhancements(
            IEnumerable<string> requestedIds,
            IDictionary<string, CastEnhancementSnapshot> enhancements,
            IDictionary<string, int?> remaining)
        {
            List<CastEnhancementSnapshot> selected = (requestedIds ??
                new string[0]).Select(id => enhancements[id]).ToList();
            foreach (KeyValuePair<string, int> requirement in
                CastEnhancementSnapshot.UsageRequirements(selected))
            {
                int? uses = remaining[requirement.Key];
                if (uses != null)
                {
                    if (uses.Value < requirement.Value)
                        throw new InvalidOperationException(
                            "Enhancement usage ledger would become negative.");
                    remaining[requirement.Key] = uses.Value -
                        requirement.Value;
                }
            }
        }

        private static int Priority(ProviderPlanningOption option, ProviderSelectionPolicy policy)
        {
            int value;
            return policy.ExplicitPriorities.TryGetValue(option.Provider.Key.Canonical, out value)
                ? value
                : int.MaxValue;
        }

        private static IDictionary<string, int> EnhancementUsage(
            IEnumerable<string> requestedIds,
            IDictionary<string, CastEnhancementSnapshot> enhancements)
        {
            return CastEnhancementSnapshot.UsageRequirements((requestedIds ??
                new string[0]).Where(id => enhancements.ContainsKey(id))
                .Select(id => enhancements[id]))
                .ToDictionary(value => value.Key, value => value.Value,
                    StringComparer.Ordinal);
        }

        private static string[] ExpectedRecipients(EffectExpression effects,
            ProviderPlanningOption option, string anchorUnitId,
            IEnumerable<string> directTargets)
        {
            bool expands = EffectExpressionTargetAnalysis.Contains(effects, EffectTarget.Party) ||
                EffectExpressionTargetAnalysis.Contains(
                    effects, EffectTarget.AlliedAreaRecipients);
            return (expands ? option.CoveredTargetIdsForAnchor(anchorUnitId) : directTargets)
                .Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray();
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

        private static string ProviderSelectionFailure(
            BuffCastRequest request, PlanContext context)
        {
            List<ProviderPlanningOption> providers = context.Options.ToList();
            int banned = providers.Count(option =>
                context.Policy.BannedProviderKeys.Contains(option.Provider.Key.Canonical));
            int capped = providers.Count(option =>
                !context.Policy.BannedProviderKeys.Contains(option.Provider.Key.Canonical) &&
                !IsUnderCap(option.Provider.Key.Canonical, context.Policy, context.CastsByProvider));
            int policyEligible = providers.Count - banned - capped;
            string reason = request.CasterUnitId != null || request.ProviderKeyConstraint != null ||
                request.SpellbookGuid != null
                ? "pinned-constraint-refusal"
                : providers.Count == 0
                    ? "no-current-provider"
                    : policyEligible == 0
                        ? "provider-policy-refusal"
                        : "temporary-resource-or-target-unavailable";
            return "provider-selection-refused:assignment=" + request.AssignmentId +
                ";source=" + request.Source.SourceId +
                ";reason=" + reason + ";providers=" + providers.Count +
                ";banned=" + banned + ";at-cap=" + capped +
                ";policy-eligible=" + policyEligible;
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

        private static TargetPlanOutcome Unfulfilled(BuffCastRequest request,
            string targetId, string failure)
        {
            return new TargetPlanOutcome(request.Source.SourceId, request.AssignmentId,
                targetId, TargetOutcomeKind.Unfulfilled,
                request.AssignmentId + ":" + failure, new string[0]);
        }

        // Per-pool demand/allocation accounting. One instance per plan; the
        // plan's ResourceAllocations are built from it, so UI summaries and
        // execution share one authoritative result.
        private sealed class AllocationAccounting
        {
            private readonly Dictionary<string, ResourcePoolKind> _kinds =
                new Dictionary<string, ResourcePoolKind>(StringComparer.Ordinal);
            private readonly Dictionary<string, int> _available =
                new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly Dictionary<string, int> _requested =
                new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly Dictionary<string, int> _allocated =
                new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly Dictionary<string, HashSet<string>> _traces =
                new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            internal AllocationAccounting(IEnumerable<ResourcePoolSnapshot> pools,
                IEnumerable<CastEnhancementSnapshot> enhancements)
            {
                foreach (ResourcePoolSnapshot pool in pools)
                    if (pool.Kind != ResourcePoolKind.Unlimited)
                    {
                        _kinds[pool.PoolKey] = pool.Kind;
                        _available[pool.PoolKey] = pool.Remaining;
                    }
                foreach (IGrouping<string, CastEnhancementSnapshot> group in enhancements
                    .GroupBy(value => value.UsagePoolId, StringComparer.Ordinal))
                {
                    // Unlimited usage pools are not budgets; only limited
                    // charge reservoirs get an accounting line.
                    if (group.First().RemainingUses == null) continue;
                    string key = UsagePoolKey(group.Key);
                    _kinds[key] = ResourcePoolKind.ItemCharges;
                    _available[key] = group.First().RemainingUses.Value;
                }
            }

            private static string UsagePoolKey(string usagePoolId)
            {
                return "enhancement:" + usagePoolId;
            }

            internal void RecordEnhancementDemand(IEnumerable<string> enhancementIds,
                IDictionary<string, CastEnhancementSnapshot> enhancements, string assignmentId)
            {
                foreach (KeyValuePair<string, int> requirement in UsageRequirements(
                        enhancementIds, enhancements))
                {
                    Add(UsagePoolKey(requirement.Key), requirement.Value, 0, assignmentId);
                }
            }

            internal void RecordEnhancementAllocation(IEnumerable<string> enhancementIds,
                IDictionary<string, CastEnhancementSnapshot> enhancements, string assignmentId)
            {
                // Demand was already recorded for the attempt; this adds only
                // the charges actually committed.
                foreach (KeyValuePair<string, int> requirement in UsageRequirements(
                        enhancementIds, enhancements))
                {
                    Add(UsagePoolKey(requirement.Key), 0, requirement.Value, assignmentId);
                }
            }

            internal void RecordNativeDemand(string poolKey, int units, string assignmentId)
            {
                Add(poolKey, units, 0, assignmentId);
            }

            internal void RecordNativeAllocation(string poolKey, int units, string assignmentId)
            {
                Add(poolKey, units, units, assignmentId);
            }

            private static Dictionary<string, int> UsageRequirements(
                IEnumerable<string> enhancementIds,
                IDictionary<string, CastEnhancementSnapshot> enhancements)
            {
                return CastEnhancementSnapshot.UsageRequirements((enhancementIds ??
                    new string[0]).Where(id => enhancements.ContainsKey(id))
                    .Select(id => enhancements[id]))
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            }

            private void Add(string poolKey, int requested, int allocated, string assignmentId)
            {
                if (requested == 0 && allocated == 0) return;
                _requested[poolKey] = _requested.ContainsKey(poolKey)
                    ? _requested[poolKey] + requested : requested;
                _allocated[poolKey] = _allocated.ContainsKey(poolKey)
                    ? _allocated[poolKey] + allocated : allocated;
                HashSet<string> traces;
                if (!_traces.TryGetValue(poolKey, out traces))
                    _traces[poolKey] = traces = new HashSet<string>(StringComparer.Ordinal);
                traces.Add(assignmentId);
            }

            internal IReadOnlyList<ResourcePoolAllocation> Build()
            {
                var result = new List<ResourcePoolAllocation>();
                foreach (string poolKey in _requested.Keys
                    .Union(_allocated.Keys, StringComparer.Ordinal)
                    .OrderBy(key => key, StringComparer.Ordinal))
                {
                    int available;
                    _available.TryGetValue(poolKey, out available);
                    int requested, allocated;
                    _requested.TryGetValue(poolKey, out requested);
                    _allocated.TryGetValue(poolKey, out allocated);
                    ResourcePoolKind kind;
                    _kinds.TryGetValue(poolKey, out kind);
                    HashSet<string> traces;
                    _traces.TryGetValue(poolKey, out traces);
                    result.Add(new ResourcePoolAllocation(poolKey, kind, available,
                        requested, allocated, traces ?? new HashSet<string>()));
                }
                return result;
            }
        }

        private sealed class EnhancementSetResolution
        {
            internal EnhancementSetResolution(bool available, IReadOnlyList<string> appliedIds,
                IReadOnlyList<string> omittedIds, string failure)
            {
                Available = available;
                AppliedIds = appliedIds ?? new string[0];
                OmittedIds = omittedIds ?? new string[0];
                Failure = failure ?? string.Empty;
            }

            internal bool Available;
            internal IReadOnlyList<string> AppliedIds;
            internal IReadOnlyList<string> OmittedIds;
            internal string Failure;
        }

        private sealed class Selection
        {
            internal Selection(
                ProviderPlanningOption option,
                string anchor,
                ResourceReservation reservation,
                MaterialReservation materialReservation,
                IReadOnlyList<string> appliedEnhancementIds,
                IReadOnlyList<string> omittedEnhancementIds)
            {
                Option = option;
                Anchor = anchor;
                Reservation = reservation;
                MaterialReservation = materialReservation;
                AppliedEnhancementIds = appliedEnhancementIds;
                OmittedEnhancementIds = omittedEnhancementIds;
            }

            internal ProviderPlanningOption Option;
            internal string Anchor;
            internal ResourceReservation Reservation;
            internal MaterialReservation MaterialReservation;
            internal IReadOnlyList<string> AppliedEnhancementIds;
            internal IReadOnlyList<string> OmittedEnhancementIds;
        }
    }
}
