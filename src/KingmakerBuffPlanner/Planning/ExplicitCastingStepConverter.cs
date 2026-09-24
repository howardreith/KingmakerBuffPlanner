using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        // The first live-cast probe (review L6): exactly one direct-target
        // casting of a plain native spellbook spell (no metamagic) by one
        // caster on a DIFFERENT unit, with a plain direct rule-cast
        // strategy, a single current-target buff effect, and no
        // enhancements, targeting modifiers, group, material or
        // non-native cost.
        SingleCastProbe
    }

    public sealed class ExplicitStepConversion
    {
        private ExplicitStepConversion(CastPlan plan,
            IReadOnlyList<string> castingIds, string refusal,
            ExplicitProjectionScope scope, string projectionId,
            string canonicalContract)
        {
            Plan = plan;
            CastingIds = castingIds ?? new ReadOnlyCollection<string>(new List<string>());
            Refusal = refusal ?? string.Empty;
            Scope = scope;
            ProjectionId = projectionId ?? string.Empty;
            CanonicalContract = canonicalContract ?? string.Empty;
        }

        public ExplicitProjectionScope Scope { get; private set; }
        // Review L3: SHA-256 of CanonicalContract — a versioned, explicitly
        // structured representation of EVERY executable/observed field of
        // every step in order (casting and source ids, full provider and
        // ability identity, anchor, targets, expected recipients, native
        // reservation with exact tokens, material, expected effects,
        // strategy and reason, applied/omitted enhancements, enhancement
        // pool usage). A native adapter must consume THIS projection and
        // report this id, never re-plan.
        public string ProjectionId { get; private set; }
        // The exact canonical JSON the id hashes, for inspection/evidence.
        public string CanonicalContract { get; private set; }
        public const int IdentityVersion = 3;
        public CastPlan Plan { get; private set; }
        // Casting id for each step, index-aligned with Plan.Steps.
        public IReadOnlyList<string> CastingIds { get; private set; }
        public string Refusal { get; private set; }
        public bool Converted { get { return Plan != null; } }

        internal static ExplicitStepConversion Success(CastPlan plan,
            IList<string> castingIds, ExplicitProjectionScope scope, string projectionId,
            string canonicalContract)
        {
            return new ExplicitStepConversion(plan,
                new ReadOnlyCollection<string>(castingIds.ToList()), null, scope,
                projectionId, canonicalContract);
        }

        internal static ExplicitStepConversion Refuse(string reason,
            ExplicitProjectionScope scope = ExplicitProjectionScope.Standard)
        {
            return new ExplicitStepConversion(null, null, reason, scope, null, null);
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
            // Review L6: an undefined scope value is refused, never treated
            // as Standard.
            if (!Enum.IsDefined(typeof(ExplicitProjectionScope), scope))
                return ExplicitStepConversion.Refuse("projection-scope-invalid:" + (int)scope,
                    ExplicitProjectionScope.Standard);
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
                // Review M1: a zero native charge is executable only when the
                // ledger verified an Unlimited pool; a flagged line must be a
                // true zero. Anything else is an unknown cost and refused.
                if (native[0].Unlimited
                        ? native[0].Units != 0 || native[0].TokenIds.Count != 0
                        : native[0].Units < 1)
                    return ExplicitStepConversion.Refuse("native-cost-unverified:" + castingId +
                        ":units=" + native[0].Units + ";unlimited=" + native[0].Unlimited);
                List<CastingCostLine> materials = casting.Cost
                    .Where(line => line.Category == CastingCostCategory.Material).ToList();
                if (materials.Count > 1)
                    return ExplicitStepConversion.Refuse("material-cost-count:" + castingId);
                EffectExpression expected = null;
                if (effectsBySource != null)
                    effectsBySource.TryGetValue(casting.SourceId, out expected);
                if (expected == null)
                    return ExplicitStepConversion.Refuse("expected-effects-missing:" + castingId);
                if (scope == ExplicitProjectionScope.SingleCastProbe)
                {
                    string probe = ProbeStepRefusal(casting, option, expected);
                    if (probe != null)
                        return ExplicitStepConversion.Refuse(probe, scope);
                }

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
                // Final review A2: a step with no expected recipient could
                // never be confirmed; it is never executed.
                if (recipients == null || !recipients.Any(value => !string.IsNullOrWhiteSpace(value)))
                    return ExplicitStepConversion.Refuse("no-predicted-recipients:" + castingId);

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
                        native[0].TokenIds, native[0].Unlimited),
                    material == null ? null
                        : new MaterialReservation(material.ItemGuid, material.Units),
                    expected,
                    mass,
                    casting.ExecutionStrategy ?? option.ExecutionStrategy,
                    casting.ExecutionStrategy == null ? option.ExecutionStrategyReason
                        : casting.ExecutionStrategyReason,
                    casting.AppliedEnhancementIds,
                    enhancementUsage,
                    casting.OmittedEnhancementIds,
                    mass ? casting.PreCoveredUnitIds : null));
                ids.Add(casting.CastingId);
            }
            if (steps.Count == 0)
                return ExplicitStepConversion.Refuse("no-executable-castings", scope);
            string canonical;
            try { canonical = CanonicalContract(steps, scope); }
            catch (NotSupportedException exception)
            {
                // Never certify a partial identity.
                return ExplicitStepConversion.Refuse(
                    "projection-identity-unrepresentable:" + exception.Message, scope);
            }
            return ExplicitStepConversion.Success(
                new CastPlan(steps, new TargetPlanOutcome[0],
                    new[] { "explicit-casting-projection;castings=" + steps.Count +
                        ";scope=" + scope }),
                ids, scope, Sha256Hex(canonical), canonical);
        }

        // Review L6: the probe subset is enforced HERE, independently of any
        // discovery-side selector.
        private static string ProbeStepRefusal(ResolvedCasting casting,
            ProviderPlanningOption option, EffectExpression expected)
        {
            string id = casting.CastingId;
            AbilityKey ability = casting.Provider.Ability;
            if (ability.SourceKind != SourceKind.Spellbook ||
                !string.IsNullOrEmpty(ability.SpecialSourceId))
                return "probe-unsupported:source-kind:" + ability.SourceKind + ":" + id;
            if (ability.MetamagicMask != 0 || casting.Ability.MetamagicMask != 0)
                return "probe-unsupported:metamagic:" + ability.MetamagicMask + ":" + id;
            if (!string.Equals(casting.Provider.CasterUnitId, casting.CasterUnitId,
                    StringComparison.Ordinal))
                return "probe-unsupported:provider-caster-mismatch:" + id;
            if (string.IsNullOrEmpty(casting.DirectTargetUnitId) ||
                string.Equals(casting.CasterUnitId, casting.DirectTargetUnitId,
                    StringComparison.Ordinal))
                return "probe-unsupported:self-target:" + id;
            if (option.ReachableTargetIds == null ||
                !option.ReachableTargetIds.Contains(casting.DirectTargetUnitId))
                return "probe-unsupported:target-not-verified-reachable:" + id;
            if (option.ExecutionStrategy != CastExecutionStrategy.DirectRuleCast)
                return "probe-unsupported:strategy:" + option.ExecutionStrategy + ":" + id;
            if (!IsPlainCurrentTargetBuff(expected, ability))
                return "probe-unsupported:effect-shape:" + id;
            return null;
        }

        // A plain buff on the chosen target: one or more buff leaves aimed at
        // the current target, optionally in sequences, optionally under the
        // discovery wrapper that references THE CAST ABILITY ITSELF.
        // Conditionals, references to any OTHER ability, area/party/caster
        // targets and worn-item enchantments are unmodeled for the probe.
        // The cast ability itself is its base spell, or for a variant
        // provider the variant it casts (advanced fixture, 2026-09-24: every
        // variant spell's effect is wrapped in a reference to the variant, so
        // matching only the base spell refused all of them).
        internal static bool IsPlainCurrentTargetBuff(EffectExpression expression, AbilityKey ability)
        {
            return ability != null &&
                IsPlainCurrentTargetBuff(expression, ability.BaseAbilityGuid, ability.VariantGuid);
        }

        internal static bool IsPlainCurrentTargetBuff(EffectExpression expression,
            string castAbilityGuid, string castVariantGuid = null)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null)
                return leaf.Kind == EffectKind.Buff && leaf.Target == EffectTarget.CurrentTarget;
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
                return sequence.Children.Count != 0 &&
                    sequence.Children.All(child =>
                        IsPlainCurrentTargetBuff(child, castAbilityGuid, castVariantGuid));
            var reference = expression as ReferencedAbilityExpression;
            if (reference != null)
                return ((!string.IsNullOrEmpty(castAbilityGuid) &&
                        string.Equals(reference.AbilityId, castAbilityGuid, StringComparison.Ordinal)) ||
                    (!string.IsNullOrEmpty(castVariantGuid) &&
                        string.Equals(reference.AbilityId, castVariantGuid, StringComparison.Ordinal))) &&
                    IsPlainCurrentTargetBuff(reference.Child, castAbilityGuid, castVariantGuid);
            return false;
        }

        // The Standard-scope contract check alone, for disclosure BEFORE
        // execution (the workspace card): null when the executor step can
        // carry the casting as authored; otherwise the same refusal Apply
        // would report for the whole conversion.
        internal static string StandardExecutionLimitation(ResolvedCasting casting)
        {
            return casting == null ? null
                : UnsupportedContract(casting, ExplicitProjectionScope.Standard);
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

        // Review L3: explicit structure, fixed key order, deterministic
        // normalization of set-like collections, and the step sequence in
        // order. Any value it cannot represent throws NotSupportedException
        // so the conversion is refused instead of partially identified.
        internal static string CanonicalContract(IList<CastStep> steps,
            ExplicitProjectionScope scope)
        {
            var stepArray = new JArray();
            for (int index = 0; index < steps.Count; index++)
            {
                CastStep step = steps[index];
                if (step.Provider == null || step.Reservation == null)
                    throw new NotSupportedException("step-incomplete:" + index);
                var stepObject = new JObject
                {
                    { "index", index },
                    { "castingId", step.AssignmentId },
                    { "sourceId", step.SourceId },
                    { "provider", Provider(step.Provider) },
                    { "anchorUnitId", step.AnchorUnitId },
                    { "targetUnitIds", Ordered(step.TargetUnitIds) },
                    { "expectedRecipientUnitIds", Sorted(step.ExpectedRecipientUnitIds) },
                    { "reservation", new JObject
                        {
                            { "poolKey", step.Reservation.PoolKey },
                            { "units", step.Reservation.Units },
                            { "unlimited", step.Reservation.Unlimited },
                            { "tokenIds", Sorted(step.Reservation.TokenIds) }
                        } },
                    { "material", step.MaterialReservation == null ? JValue.CreateNull()
                        : (JToken)new JObject
                        {
                            { "itemGuid", step.MaterialReservation.ItemGuid },
                            { "count", step.MaterialReservation.Count }
                        } },
                    { "expectedEffects", Effect(step.ExpectedEffects) },
                    { "massCast", step.MassCast },
                    { "executionStrategy", step.ExecutionStrategy.ToString() },
                    { "executionStrategyReason", step.ExecutionStrategyReason },
                    { "enhancementIds", Ordered(step.EnhancementIds) },
                    { "omittedEnhancementIds", Sorted(step.OmittedEnhancementIds) },
                    { "enhancementUsageByPool", UsageByPool(step.EnhancementUsageByPool) }
                };
                // Mixed coverage: only a step that names pre-covered
                // recipients carries the key, so every other identity is
                // unchanged.
                if (step.PreCoveredRecipientUnitIds.Count != 0)
                    stepObject.Add("preCoveredRecipientUnitIds", Sorted(step.PreCoveredRecipientUnitIds));
                stepArray.Add(stepObject);
            }
            var root = new JObject
            {
                { "format", "kbp-explicit-projection" },
                { "identityVersion", ExplicitStepConversion.IdentityVersion },
                { "scope", scope.ToString() },
                { "steps", stepArray }
            };
            return root.ToString(Formatting.None);
        }

        private static JObject Provider(ProviderKey provider)
        {
            return new JObject
            {
                { "casterUnitId", provider.CasterUnitId },
                { "spellbookGuid", provider.SpellbookGuid },
                { "sourceInstanceId", provider.SourceInstanceId },
                { "ability", new JObject
                    {
                        { "sourceKind", provider.Ability.SourceKind.ToString() },
                        { "baseAbilityGuid", provider.Ability.BaseAbilityGuid },
                        { "variantGuid", provider.Ability.VariantGuid },
                        { "metamagicMask", provider.Ability.MetamagicMask },
                        { "specialSourceId", provider.Ability.SpecialSourceId }
                    } }
            };
        }

        private static JArray Ordered(IEnumerable<string> values)
        {
            return new JArray((values ?? new string[0]).Select(value => (object)value).ToArray());
        }

        private static JArray Sorted(IEnumerable<string> values)
        {
            return new JArray((values ?? new string[0]).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(value => (object)value).ToArray());
        }

        private static JArray UsageByPool(IReadOnlyDictionary<string, int> usage)
        {
            var array = new JArray();
            foreach (KeyValuePair<string, int> pair in (usage ??
                    new Dictionary<string, int>()).OrderBy(pair => pair.Key, StringComparer.Ordinal))
                array.Add(new JObject { { "poolKey", pair.Key }, { "units", pair.Value } });
            return array;
        }

        private static JToken Effect(EffectExpression expression)
        {
            if (expression == null) throw new NotSupportedException("effect-null");
            if (expression is EmptyEffectExpression)
                return new JObject { { "type", "empty" } };
            var leaf = expression as EffectLeafExpression;
            if (leaf != null)
                return new JObject
                {
                    { "type", "leaf" },
                    { "kind", leaf.Kind.ToString() },
                    { "effectId", leaf.EffectId },
                    { "target", leaf.Target.ToString() },
                    { "sourceContract", leaf.SourceContract },
                    { "actionPath", leaf.ActionPath }
                };
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
                return new JObject
                {
                    { "type", "sequence" },
                    { "children", new JArray(sequence.Children.Select(Effect).ToArray()) }
                };
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
                return new JObject
                {
                    { "type", "conditional" },
                    { "conditionContract", conditional.ConditionContract },
                    { "whenTrue", Effect(conditional.WhenTrue) },
                    { "whenFalse", Effect(conditional.WhenFalse) }
                };
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null)
                return new JObject
                {
                    { "type", "targeted" },
                    { "target", targeted.Target.ToString() },
                    { "child", Effect(targeted.Child) }
                };
            var referenced = expression as ReferencedAbilityExpression;
            if (referenced != null)
                return new JObject
                {
                    { "type", "ability-reference" },
                    { "abilityId", referenced.AbilityId },
                    { "child", Effect(referenced.Child) }
                };
            throw new NotSupportedException("effect-type:" + expression.GetType().Name);
        }

        // The identity of an exact step list (used by the probe boundary to
        // recompute the id from the steps it is actually handed).
        internal static string Identity(IList<CastStep> steps, ExplicitProjectionScope scope)
        {
            return Sha256Hex(CanonicalContract(steps, scope));
        }

        private static string Sha256Hex(string text)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(
                    Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty)
                    .ToLowerInvariant();
        }
    }
}
