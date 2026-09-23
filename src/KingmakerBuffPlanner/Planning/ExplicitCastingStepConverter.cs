using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Planning
{
    // The outcome of converting an approved explicit plan into executor
    // steps. Either every approved casting converts or nothing does: a
    // partial step list would run some castings of a reviewed plan while
    // silently dropping others.
    public sealed class ExplicitStepConversion
    {
        private ExplicitStepConversion(CastPlan plan,
            IReadOnlyList<string> castingIds, string refusal)
        {
            Plan = plan;
            CastingIds = castingIds ?? new ReadOnlyCollection<string>(new List<string>());
            Refusal = refusal ?? string.Empty;
        }

        public CastPlan Plan { get; private set; }
        // Casting id for each step, index-aligned with Plan.Steps.
        public IReadOnlyList<string> CastingIds { get; private set; }
        public string Refusal { get; private set; }
        public bool Converted { get { return Plan != null; } }

        internal static ExplicitStepConversion Success(CastPlan plan,
            IList<string> castingIds)
        {
            return new ExplicitStepConversion(plan,
                new ReadOnlyCollection<string>(castingIds.ToList()), null);
        }

        internal static ExplicitStepConversion Refuse(string reason)
        {
            return new ExplicitStepConversion(null, null, reason);
        }
    }

    // Charter §5.3 / takeover §10.B: the existing animated and instant
    // executors consume the SAME resolved explicit castings the workspace
    // authored — one approved Ready casting becomes exactly one executor
    // step with its exact provider, target shape, applied enhancements and
    // reserved costs. Nothing here expands coverage, substitutes a caster,
    // or re-plans: the step list is a projection of the approved decision,
    // in the decision's order. Native dispatch of these steps stays behind
    // the disabled dispatch boundary until separately authorized.
    public static class ExplicitCastingStepConverter
    {
        public static ExplicitStepConversion Convert(
            ExplicitCastingPlan plan,
            CastingApplyDecision decision,
            IEnumerable<ProviderPlanningOption> providerOptions,
            IReadOnlyDictionary<string, EffectExpression> effectsBySource)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            if (decision == null) throw new ArgumentNullException("decision");
            if (!decision.Allowed)
                return ExplicitStepConversion.Refuse("decision-not-allowed");
            var options = (providerOptions ?? new ProviderPlanningOption[0])
                .Where(option => option != null && option.Provider != null)
                .GroupBy(option => option.Provider.Key.Canonical, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(),
                    StringComparer.Ordinal);
            var byId = plan.Castings.Where(casting => casting != null)
                .ToDictionary(casting => casting.CastingId, StringComparer.Ordinal);
            var steps = new List<CastStep>();
            var ids = new List<string>();
            foreach (string castingId in decision.ExecutableCastingIds)
            {
                ResolvedCasting casting;
                if (!byId.TryGetValue(castingId, out casting))
                    return ExplicitStepConversion.Refuse("casting-missing:" + castingId);
                if (!casting.IsExecutable)
                    return ExplicitStepConversion.Refuse("casting-not-ready:" + castingId);
                if (casting.Provider == null)
                    return ExplicitStepConversion.Refuse("provider-unresolved:" + castingId);
                ProviderPlanningOption option;
                if (!options.TryGetValue(casting.Provider.Canonical, out option))
                    return ExplicitStepConversion.Refuse("provider-option-missing:" + castingId);
                List<CastingCostLine> native = casting.Cost
                    .Where(line => line.Category == CastingCostCategory.NativePool).ToList();
                if (native.Count != 1)
                    return ExplicitStepConversion.Refuse("native-cost-count:" + castingId +
                        ":" + native.Count);
                List<CastingCostLine> materials = casting.Cost
                    .Where(line => line.Category == CastingCostCategory.Material).ToList();
                if (materials.Count > 1)
                    return ExplicitStepConversion.Refuse("material-cost-count:" + castingId);
                EffectExpression expected = null;
                if (effectsBySource != null)
                    effectsBySource.TryGetValue(casting.SourceId, out expected);
                if (expected == null)
                    return ExplicitStepConversion.Refuse("expected-effects-missing:" + castingId);

                string anchor;
                IEnumerable<string> targets;
                IEnumerable<string> recipients;
                bool mass;
                switch (casting.TargetMode)
                {
                    case CastingTargetMode.DirectTarget:
                        if (string.IsNullOrEmpty(casting.DirectTargetUnitId))
                            return ExplicitStepConversion.Refuse("direct-target-missing:" + castingId);
                        anchor = null;
                        targets = new[] { casting.DirectTargetUnitId };
                        recipients = targets;
                        mass = false;
                        break;
                    case CastingTargetMode.CasterCenteredOrigin:
                        anchor = casting.CasterUnitId;
                        targets = new[] { casting.CasterUnitId };
                        recipients = casting.PredictedBeneficiaryUnitIds;
                        mass = true;
                        break;
                    case CastingTargetMode.AnchoredOrigin:
                        if (casting.Origin == null ||
                            string.IsNullOrEmpty(casting.Origin.AnchorUnitId))
                            return ExplicitStepConversion.Refuse("anchor-missing:" + castingId);
                        anchor = casting.Origin.AnchorUnitId;
                        targets = new[] { anchor };
                        recipients = casting.PredictedBeneficiaryUnitIds;
                        mass = true;
                        break;
                    default:
                        return ExplicitStepConversion.Refuse("target-mode-unsupported:" + castingId);
                }

                var enhancementUsage = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (CastingCostLine line in casting.Cost.Where(line =>
                    line.Category == CastingCostCategory.EnhancementPool))
                {
                    int units;
                    enhancementUsage.TryGetValue(line.PoolKey, out units);
                    enhancementUsage[line.PoolKey] = units + line.Units;
                }
                CastingCostLine material = materials.FirstOrDefault();
                steps.Add(new CastStep(
                    casting.SourceId,
                    casting.CastingId,
                    casting.Provider,
                    anchor,
                    targets,
                    recipients,
                    new ResourceReservation(native[0].PoolKey, native[0].Units,
                        native[0].TokenIds),
                    material == null ? null
                        : new MaterialReservation(material.ItemGuid, material.Units),
                    expected,
                    mass,
                    option.ExecutionStrategy,
                    option.ExecutionStrategyReason,
                    casting.AppliedEnhancementIds,
                    enhancementUsage,
                    casting.OmittedEnhancementIds));
                ids.Add(casting.CastingId);
            }
            if (steps.Count == 0)
                return ExplicitStepConversion.Refuse("no-executable-castings");
            return ExplicitStepConversion.Success(
                new CastPlan(steps, new TargetPlanOutcome[0],
                    new[] { "explicit-casting-projection;castings=" + steps.Count }),
                ids);
        }
    }
}
