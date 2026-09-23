using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Planning
{
    // The outcome of converting an approved explicit plan into executor
    // steps. Either every approved casting converts or nothing does: a
    // partial step list would run some castings of a reviewed plan while
    // silently dropping others.
    // Review K4: what a projection may contain.
    public enum ExplicitProjectionScope
    {
        // Every contract the executor step can carry end to end; anything
        // it cannot carry refuses the whole conversion.
        Standard,
        // The first live-cast probe: exactly one direct-target casting with
        // no enhancements, targeting modifiers, group or material cost.
        SingleCastProbe
    }

    public sealed class ExplicitStepConversion
    {
        private ExplicitStepConversion(CastPlan plan,
            IReadOnlyList<string> castingIds, string refusal,
            ExplicitProjectionScope scope, string projectionId)
        {
            Plan = plan;
            CastingIds = castingIds ?? new ReadOnlyCollection<string>(new List<string>());
            Refusal = refusal ?? string.Empty;
            Scope = scope;
            ProjectionId = projectionId ?? string.Empty;
        }

        public ExplicitProjectionScope Scope { get; private set; }
        // Content identity of the exact approved steps (casting ids,
        // providers, targets, enhancements, reservations, strategy). A
        // native adapter must consume THIS projection and report this id,
        // never re-plan.
        public string ProjectionId { get; private set; }
        public CastPlan Plan { get; private set; }
        // Casting id for each step, index-aligned with Plan.Steps.
        public IReadOnlyList<string> CastingIds { get; private set; }
        public string Refusal { get; private set; }
        public bool Converted { get { return Plan != null; } }

        internal static ExplicitStepConversion Success(CastPlan plan,
            IList<string> castingIds, ExplicitProjectionScope scope, string projectionId)
        {
            return new ExplicitStepConversion(plan,
                new ReadOnlyCollection<string>(castingIds.ToList()), null, scope,
                projectionId);
        }

        internal static ExplicitStepConversion Refuse(string reason,
            ExplicitProjectionScope scope = ExplicitProjectionScope.Standard)
        {
            return new ExplicitStepConversion(null, null, reason, scope, null);
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
            IReadOnlyDictionary<string, EffectExpression> effectsBySource,
            ExplicitProjectionScope scope = ExplicitProjectionScope.Standard)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            if (decision == null) throw new ArgumentNullException("decision");
            if (!decision.Allowed)
                return ExplicitStepConversion.Refuse("decision-not-allowed", scope);
            if (scope == ExplicitProjectionScope.SingleCastProbe &&
                decision.ExecutableCastingIds.Count != 1)
                return ExplicitStepConversion.Refuse("probe-requires-exactly-one-casting:" +
                    decision.ExecutableCastingIds.Count, scope);
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
                // Review K4: contracts the executor step cannot carry refuse
                // the WHOLE conversion instead of vanishing from it.
                string unsupported = UnsupportedContract(casting, scope);
                if (unsupported != null)
                    return ExplicitStepConversion.Refuse(unsupported, scope);
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
                return ExplicitStepConversion.Refuse("no-executable-castings", scope);
            return ExplicitStepConversion.Success(
                new CastPlan(steps, new TargetPlanOutcome[0],
                    new[] { "explicit-casting-projection;castings=" + steps.Count +
                        ";scope=" + scope }),
                ids, scope, ProjectionIdentity(steps, scope));
        }

        private static string UnsupportedContract(ResolvedCasting casting,
            ExplicitProjectionScope scope)
        {
            string id = casting.CastingId;
            List<string> modifiers = (casting.TargetingModifiers ??
                    new TargetingModifierSelection[0])
                .Where(value => value != null && value.Enabled)
                .Select(value => value.ModifierId).ToList();
            if (modifiers.Count != 0)
                return "unsupported-contract:targeting-modifier:" + id + ":" +
                    string.Join(",", modifiers);
            if ((casting.Enhancements ?? new AuthoredEnhancementSelection[0])
                    .Any(value => value != null && value.ExactSourceRef != null))
                return "unsupported-contract:exact-enhancement-source:" + id;
            if (casting.TargetMode != CastingTargetMode.DirectTarget &&
                casting.CoverageIncomplete)
                return "unsupported-contract:required-coverage-incomplete:" + id;
            if (scope == ExplicitProjectionScope.SingleCastProbe)
            {
                if (casting.TargetMode != CastingTargetMode.DirectTarget)
                    return "probe-unsupported:group:" + id;
                if ((casting.Enhancements != null && casting.Enhancements.Count != 0) ||
                    (casting.AppliedEnhancementIds != null &&
                     casting.AppliedEnhancementIds.Count != 0))
                    return "probe-unsupported:enhancement:" + id;
                if (casting.TargetingModifiers != null && casting.TargetingModifiers.Count != 0)
                    return "probe-unsupported:targeting-modifier:" + id;
                if (casting.Cost.Any(line => line.Category != CastingCostCategory.NativePool))
                    return "probe-unsupported:non-native-cost:" + id;
            }
            return null;
        }

        private static string ProjectionIdentity(IEnumerable<CastStep> steps,
            ExplicitProjectionScope scope)
        {
            var text = new StringBuilder("scope=" + scope);
            foreach (CastStep step in steps)
            {
                text.Append('\n').Append(step.AssignmentId)
                    .Append('|').Append(step.Provider.Canonical)
                    .Append('|').Append(step.AnchorUnitId ?? string.Empty)
                    .Append('|').Append(string.Join(",", step.TargetUnitIds))
                    .Append('|').Append(string.Join(",", step.ExpectedRecipientUnitIds))
                    .Append('|').Append(string.Join(",", step.EnhancementIds))
                    .Append('|').Append(step.Reservation.PoolKey).Append(':')
                    .Append(step.Reservation.Units)
                    .Append('|').Append(step.MaterialReservation == null ? string.Empty
                        : step.MaterialReservation.ItemGuid + ":" + step.MaterialReservation.Count)
                    .Append('|').Append(step.MassCast)
                    .Append('|').Append(step.ExecutionStrategy);
            }
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(
                    Encoding.UTF8.GetBytes(text.ToString()))).Replace("-", string.Empty)
                    .ToLowerInvariant();
        }
    }
}
