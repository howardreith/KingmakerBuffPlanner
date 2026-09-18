using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    // Real sequential forecast planning. Each selected routine occurrence is
    // planned by the production planner against the snapshot that remains
    // after the previous occurrence: native pool balances, prepared tokens,
    // material components, enhancement charges, and the active effects
    // projected by earlier successful casts all carry forward. A routine
    // whose demand exceeds what is left is reported with honest unmet
    // outcomes — independently full-budget previews are never summed, and
    // shortages are never clamped away.
    public static class SequentialForecastPlanner
    {
        public sealed class Occurrence
        {
            internal Occurrence(string routineId, CastPlan plan)
            {
                RoutineId = routineId;
                Plan = plan;
            }

            public string RoutineId { get; private set; }
            public CastPlan Plan { get; private set; }
        }

        public sealed class Result
        {
            internal Result(IReadOnlyList<Occurrence> occurrences,
                IReadOnlyDictionary<string, int> forecastRemainingByNativePool,
                IReadOnlyDictionary<string, int?> forecastRemainingByEnhancementPool)
            {
                Occurrences = occurrences;
                ForecastRemainingByNativePool = forecastRemainingByNativePool;
                ForecastRemainingByEnhancementPool = forecastRemainingByEnhancementPool;
            }

            public IReadOnlyList<Occurrence> Occurrences { get; private set; }
            public IReadOnlyDictionary<string, int> ForecastRemainingByNativePool
            { get; private set; }
            public IReadOnlyDictionary<string, int?> ForecastRemainingByEnhancementPool
            { get; private set; }

            public const string AssumptionText =
                "One run per selected routine in the selected order, successful casts, " +
                "no intervening combat or expiration. Effects granted by earlier runs " +
                "project forward so later already-active targets skip for free; " +
                "conditional coverage beyond those effects is a conservative demand " +
                "estimate, not an exact forecast.";
        }

        public static Result Compute(
            BuffPlannerProfile profile,
            IReadOnlyList<string> routineIdsInOrder,
            PartyProviderSnapshot snapshot,
            ActiveEffectSnapshot activeEffects,
            IDictionary<string, EffectExpression> effectsBySource,
            IEnumerable<ProviderPlanningOption> providerOptions,
            IEnumerable<CastEnhancementSnapshot> enhancements,
            EffectiveProviderOptionResolver targeting = null)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            if (routineIdsInOrder == null) throw new ArgumentNullException("routineIdsInOrder");
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            if (activeEffects == null) throw new ArgumentNullException("activeEffects");
            if (effectsBySource == null) throw new ArgumentNullException("effectsBySource");
            var occurrences = new List<Occurrence>();
            // Carried state.
            var poolRemaining = snapshot.ResourcePools
                .ToDictionary(pool => pool.PoolKey, pool => pool.Remaining,
                    StringComparer.Ordinal);
            var poolTokens = snapshot.ResourcePools
                .Where(pool => pool.Kind == ResourcePoolKind.PreparedSlots)
                .ToDictionary(pool => pool.PoolKey,
                    pool => pool.Tokens.ToDictionary(
                        token => token.TokenId, token => token.Available,
                        StringComparer.Ordinal),
                    StringComparer.Ordinal);
            var materialRemaining = snapshot.Providers
                .Where(provider => provider.MaterialComponent != null)
                .GroupBy(provider => provider.MaterialComponent.ItemGuid,
                    StringComparer.Ordinal)
                .ToDictionary(group => group.Key,
                    group => group.Max(provider => provider.MaterialComponent.AvailableCount),
                    StringComparer.Ordinal);
            var enhancementCharges = (enhancements ?? new CastEnhancementSnapshot[0])
                .GroupBy(value => value.UsagePoolId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key,
                    group => group.First().RemainingUses,
                    StringComparer.Ordinal);
            var carriedEffects = activeEffects.ToUnitMarkers();

            foreach (string routineId in routineIdsInOrder
                .Distinct(StringComparer.Ordinal))
            {
                PartyProviderSnapshot occurrenceSnapshot = BuildSnapshot(
                    snapshot, poolRemaining, poolTokens, materialRemaining,
                    enhancements, enhancementCharges);
                var occurrenceEffects = new Dictionary<string, IEnumerable<ActiveEffectMarker>>(
                    StringComparer.Ordinal);
                foreach (KeyValuePair<string, HashSet<ActiveEffectMarker>> pair in carriedEffects)
                    occurrenceEffects[pair.Key] = pair.Value;
                ActiveEffectSnapshot projectedActive =
                    ActiveEffectSnapshot.FromTypedEffects(occurrenceEffects);
                var service = new RoutinePlanService();
                RoutinePlanResult result = service.Plan(profile, routineId,
                    occurrenceSnapshot, projectedActive, effectsBySource,
                    providerOptions, BuildEnhancements(enhancements,
                        enhancementCharges), targeting);
                occurrences.Add(new Occurrence(routineId, result.Plan));
                // Carry forward only what this occurrence actually committed.
                foreach (CastStep step in result.Plan.Steps)
                {
                    if (step.Reservation != null && step.Reservation.Units > 0)
                    {
                        if (poolRemaining.ContainsKey(step.Reservation.PoolKey))
                        {
                            poolRemaining[step.Reservation.PoolKey] =
                                Math.Max(0, poolRemaining[step.Reservation.PoolKey] -
                                    step.Reservation.Units);
                        }
                        // Prepared slots allocate by token availability in the
                        // ledger, not by the aggregate: consume the exact
                        // reserved tokens (linked companions included) so a
                        // later occurrence cannot reuse a spent slot.
                        Dictionary<string, bool> tokens;
                        if (poolTokens.TryGetValue(step.Reservation.PoolKey, out tokens))
                        {
                            foreach (string tokenId in step.Reservation.TokenIds)
                            {
                                if (tokens.ContainsKey(tokenId))
                                    tokens[tokenId] = false;
                            }
                        }
                    }
                    foreach (KeyValuePair<string, int> usage in step.EnhancementUsageByPool)
                    {
                        int? remaining;
                        if (enhancementCharges.TryGetValue(usage.Key, out remaining) &&
                            remaining != null)
                            enhancementCharges[usage.Key] =
                                Math.Max(0, remaining.Value - usage.Value);
                    }
                    if (step.MaterialReservation != null)
                    {
                        int material;
                        if (materialRemaining.TryGetValue(
                                step.MaterialReservation.ItemGuid, out material))
                            materialRemaining[step.MaterialReservation.ItemGuid] =
                                Math.Max(0, material - step.MaterialReservation.Count);
                    }
                    // Project granted effects forward so later routines skip
                    // already-covered targets for free — but only effects the
                    // forecast assumptions justify. Conditional alternatives
                    // are NOT a union: mutually exclusive branches stay
                    // unproven, and kinds/targeting structure are preserved.
                    foreach (ProjectedEffect projection in ProjectEffects(step))
                    {
                        HashSet<ActiveEffectMarker> markers;
                        if (!carriedEffects.TryGetValue(projection.RecipientUnitId,
                                out markers))
                            carriedEffects[projection.RecipientUnitId] = markers =
                                new HashSet<ActiveEffectMarker>();
                        markers.Add(new ActiveEffectMarker(projection.Kind,
                            projection.EffectId));
                    }
                }
            }

            PartyProviderSnapshot finalSnapshot = BuildSnapshot(snapshot,
                poolRemaining, poolTokens, materialRemaining, enhancements,
                enhancementCharges);
            return new Result(
                new ReadOnlyCollection<Occurrence>(occurrences),
                new ReadOnlyDictionary<string, int>(finalSnapshot.ResourcePools
                    .ToDictionary(pool => pool.PoolKey, pool => pool.Remaining,
                        StringComparer.Ordinal)),
                new ReadOnlyDictionary<string, int?>((enhancements ??
                    new CastEnhancementSnapshot[0])
                    .GroupBy(value => value.UsagePoolId, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key,
                        group => enhancementCharges.TryGetValue(group.Key,
                            out var value) ? value : group.First().RemainingUses,
                        StringComparer.Ordinal)));
        }

        private static PartyProviderSnapshot BuildSnapshot(
            PartyProviderSnapshot original,
            Dictionary<string, int> poolRemaining,
            Dictionary<string, Dictionary<string, bool>> poolTokens,
            Dictionary<string, int> materialRemaining,
            IEnumerable<CastEnhancementSnapshot> enhancements,
            Dictionary<string, int?> enhancementCharges)
        {
            var pools = new List<ResourcePoolSnapshot>();
            foreach (ResourcePoolSnapshot pool in original.ResourcePools)
            {
                if (pool.Kind == ResourcePoolKind.PreparedSlots)
                {
                    Dictionary<string, bool> tokens;
                    if (!poolTokens.TryGetValue(pool.PoolKey, out tokens)) tokens = null;
                    var carriedTokens = pool.Tokens.Select(token =>
                        new ResourceTokenSnapshot(token.TokenId, token.SlottedAbility,
                            token.SpellLevel, token.SlotKind,
                            tokens == null ? token.Available : SafeToken(tokens, token.TokenId),
                            token.IsPrimary, token.LinkedTokenIds)).ToList();
                    int available = carriedTokens.Count(t => t.Available);
                    poolRemaining[pool.PoolKey] = available;
                    pools.Add(new ResourcePoolSnapshot(pool.PoolKey, pool.Kind,
                        carriedTokens.Count, available, carriedTokens));
                }
                else
                {
                    int remaining;
                    if (!poolRemaining.TryGetValue(pool.PoolKey, out remaining))
                        remaining = pool.Remaining;
                    pools.Add(new ResourcePoolSnapshot(pool.PoolKey, pool.Kind,
                        pool.Capacity, Math.Min(remaining, pool.Capacity), null));
                }
            }
            var providers = new List<ProviderSnapshot>();
            foreach (ProviderSnapshot provider in original.Providers)
            {
                MaterialRequirementSnapshot material = provider.MaterialComponent;
                if (material != null)
                {
                    int remaining;
                    if (!materialRemaining.TryGetValue(material.ItemGuid, out remaining))
                        remaining = material.AvailableCount;
                    material = new MaterialRequirementSnapshot(material.ItemGuid,
                        material.RequiredCount, remaining);
                }
                providers.Add(new ProviderSnapshot(provider.Key, provider.DisplayName,
                    provider.SpellLevel, provider.ResourcePoolKey, provider.UnitsPerCast,
                    provider.EligibleTokenIds, material, provider.EffectiveCasterLevel,
                    provider.ExpectedDurationRounds, provider.Description,
                    provider.DurationText, provider.SourceDisplayName,
                    provider.VariantOrder));
            }
            return new PartyProviderSnapshot(original.Units, providers, pools);
        }

        private static bool SafeToken(Dictionary<string, bool> tokens, string tokenId)
        {
            bool available;
            return tokens.TryGetValue(tokenId, out available) && available;
        }

        private static IReadOnlyList<CastEnhancementSnapshot> BuildEnhancements(
            IEnumerable<CastEnhancementSnapshot> enhancements,
            Dictionary<string, int?> enhancementCharges)
        {
            return (enhancements ?? new CastEnhancementSnapshot[0])
                .Select(value =>
                {
                    int? remaining;
                    if (enhancementCharges.TryGetValue(value.UsagePoolId, out remaining) &&
                        remaining != value.RemainingUses && remaining != null)
                        return new CastEnhancementSnapshot(value.EnhancementId,
                            value.CasterUnitId, value.SourceBlueprintGuid,
                            value.DisplayName, value.Description, value.Category,
                            value.MetamagicMask, value.MaximumSpellLevel, remaining,
                            value.AbilityWhiteList, value.EffectDisplayName,
                            value.SpellbookWhiteList, value.UsagePoolId,
                            value.RequiresNativeCommand, value.ExclusiveGroupId,
                            value.UsageUnitsPerCast, value.AffectsTargeting,
                            value.NativeActivationGroupId, value.UsagePoolDisplayName,
                            value.DirectCastProviderId);
                    return value;
                }).ToList();
        }

        private sealed class ProjectedEffect
        {
            internal string EffectId;
            internal EffectKind Kind;
            internal string RecipientUnitId;
        }

        // Effects justified under "successful cast" assumptions only:
        // unconditional leaves with their real kind, scoped to the recipient
        // their targeting structure names. Conditional alternatives and any
        // unrecognized node stay unproven — later demand for them remains a
        // conservative estimate instead of a free already-active skip.
        private static IEnumerable<ProjectedEffect> ProjectEffects(CastStep step)
        {
            var projections = new List<ProjectedEffect>();
            CollectJustified(step.ExpectedEffects, step, projections);
            return projections;
        }

        private static void CollectJustified(EffectExpression expression,
            CastStep step, List<ProjectedEffect> projections)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null)
            {
                if (leaf.Target == EffectTarget.Caster)
                {
                    // A caster-directed effect lands on the actual planned
                    // caster, never on the cast anchor (an ally anchor with a
                    // self-buff component must not grant that self-buff to
                    // the ally in the forecast).
                    projections.Add(new ProjectedEffect
                    {
                        EffectId = leaf.EffectId, Kind = leaf.Kind,
                        RecipientUnitId = step.Provider.CasterUnitId
                    });
                }
                else if (leaf.Target == EffectTarget.CurrentTarget && step.TargetUnitIds.Count == 1)
                {
                    projections.Add(new ProjectedEffect
                    {
                        EffectId = leaf.EffectId, Kind = leaf.Kind,
                        RecipientUnitId = step.TargetUnitIds[0]
                    });
                }
                else if (leaf.Target == EffectTarget.Party ||
                    leaf.Target == EffectTarget.AlliedAreaRecipients)
                {
                    foreach (string candidate in step.ExpectedRecipientUnitIds)
                        projections.Add(new ProjectedEffect
                        {
                            EffectId = leaf.EffectId, Kind = leaf.Kind,
                            RecipientUnitId = candidate
                        });
                }
                return;
            }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                foreach (EffectExpression child in sequence.Children)
                    CollectJustified(child, step, projections);
                return;
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null)
            {
                // A target wrapper re-scopes its child; without a proven
                // recipient mapping for the wrapper's target we project
                // nothing rather than invent coverage.
                if (targeted.Target == EffectTarget.CurrentTarget &&
                    step.TargetUnitIds.Count == 1)
                    CollectJustified(targeted.Child, step, projections);
                return;
            }
            var referenced = expression as ReferencedAbilityExpression;
            if (referenced != null) { CollectJustified(referenced.Child, step, projections); return; }
            // ConditionalEffectExpression and unknown nodes: no projection.
        }
    }
}
