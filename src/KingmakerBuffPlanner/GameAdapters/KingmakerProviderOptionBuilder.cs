using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.Utility;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerProviderOptionBuilder
    {
        private readonly List<string> _diagnostics = new List<string>();

        internal IReadOnlyList<string> Diagnostics
        { get { return _diagnostics.AsReadOnly(); } }

        internal ProviderPlanningOption[] Build(
            PartyProviderSnapshot snapshot,
            IDictionary<string, EffectExpression> effectsBySource)
        {
            _diagnostics.Clear();
            Dictionary<string, UnitEntityData> liveUnits = KingmakerAnimatedCastAdapter.CollectUnits();
            var options = new List<ProviderPlanningOption>();
            foreach (ProviderSnapshot provider in snapshot.Providers)
            {
                EffectExpression expression;
                if (!effectsBySource.TryGetValue(provider.Key.Ability.Canonical, out expression)) continue;
                UnitEntityData caster;
                liveUnits.TryGetValue(provider.Key.CasterUnitId, out caster);
                AbilityData ability = caster == null ? null :
                    KingmakerAnimatedCastAdapter.ResolveAbility(caster, provider.Key);
                BlueprintAbility blueprint = ability == null ? null : ability.Blueprint;
                CastExecutionCapability executionCapability =
                    KingmakerStickyTouchCastAdapter.Classify(blueprint);
                AbilityData targetingAbility = ability;
                StickyTouchCastResolution stickyResolution = null;
                string stickyResolutionReason = string.Empty;
                if (executionCapability.Strategy ==
                    CastExecutionStrategy.StickyTouchDeliveryRuleCast)
                {
                    if (KingmakerStickyTouchCastAdapter.TryCreateDelivery(
                            ability, out stickyResolution,
                            out stickyResolutionReason))
                        targetingAbility = stickyResolution.ExecutionAbility;
                    else
                        executionCapability = new CastExecutionCapability(
                            CastExecutionStrategy.AnimatedFallback,
                            "sticky-delivery-context-unsupported:" +
                            stickyResolutionReason);
                }
                BlueprintAbility declaredSource = ResourcesLibrary
                    .TryGetBlueprint<BlueprintAbility>(
                        provider.Key.Ability.BaseAbilityGuid);
                var areaResolver = new KingmakerAreaCoverageResolver();
                AlliedAreaCoverage areaCoverage = blueprint == null
                    ? null : areaResolver.Resolve(blueprint, declaredSource);
                bool infusedPersonal = EffectExpressionTargetAnalysis
                    .ContainsOnly(expression, EffectTarget.Caster) &&
                    ability != null && ability.IsAlchemistSpell &&
                    ability.AlchemistInfusion && ability.TargetAnchor ==
                        AbilityTargetAnchor.Unit;
                // Review R1-F8: the decisions are game-free; this builder only
                // supplies the native target verdict and live distances.
                AbilityData verdictAbility = targetingAbility;
                IReadOnlyDictionary<string, bool> facts = ReadCasterFacts(expression, caster);
                ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(
                    provider, expression, snapshot.Units, caster != null, infusedPersonal,
                    areaCoverage,
                    unitId =>
                    {
                        UnitEntityData target;
                        return liveUnits.TryGetValue(unitId, out target) &&
                            KingmakerAnimatedCastAdapter.CanTarget(verdictAbility, new TargetWrapper(target));
                    },
                    (fromId, toId) =>
                    {
                        UnitEntityData from, to;
                        return liveUnits.TryGetValue(fromId, out from) && liveUnits.TryGetValue(toId, out to)
                            ? from.DistanceTo(to) : (float?)null;
                    },
                    facts,
                    (unitId, factId) =>
                    {
                        UnitEntityData unit;
                        return liveUnits.TryGetValue(unitId, out unit) ? ReadUnitFact(unit, factId) : null;
                    },
                    // Review R4-F3: a unit fact that is not a buff (a creature
                    // type feature) cannot be added or removed by a casting.
                    factId =>
                    {
                        BlueprintUnitFact fact = ResourcesLibrary.TryGetBlueprint<BlueprintUnitFact>(factId);
                        return fact != null && !(fact is Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff);
                    });
                bool party = decision.Party;
                bool areaRecipients = decision.AreaRecipients;
                bool unsafeArea = decision.UnsafeArea;
                var recipientIdsByAnchor = decision.RecipientIdsByAnchor;
                string[] anchors = decision.Anchors;
                string[] reachableIds = decision.ReachableIds;
                var effectsByTarget = new Dictionary<string, EffectExpression>(StringComparer.Ordinal);
                EffectExpression applicable = EffectConditionBinding.Bind(expression, facts);
                if (EffectRecipientBinding.ContainsNearby(applicable))
                    foreach (string targetId in reachableIds)
                    {
                        UnitEntityData target;
                        liveUnits.TryGetValue(targetId, out target);
                        effectsByTarget[targetId] = EffectRecipientBinding.Bind(applicable,
                            rule => new NearbyRecipientSelection(SelectNearbyRecipients(rule, caster, target),
                                NearbyRecipientCandidates(rule, caster, target, snapshot.Units
                                    .Select(unit => { UnitEntityData live; return liveUnits.TryGetValue(unit.UnitId, out live) ? live : null; }))));
                    }
                options.Add(ProviderTargetingPlanner.BuildOption(provider, decision,
                    executionCapability.Strategy, executionCapability.Reason, facts, effectsByTarget));
                string targetClass = decision.TargetClass;
                string[] paths = NativeCatalogExporter.GetEffects(expression)
                    .Select(value => value.ActionPath)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                        StringComparer.Ordinal).ToArray();
                if (blueprint != null && blueprint.StickyTouch != null)
                {
                    AbilityEffectStickyTouch sticky = blueprint.StickyTouch;
                    BlueprintAbility delivery = sticky == null
                        ? null : sticky.TouchDeliveryAbility;
                    _diagnostics.Add("provider=" + provider.Key.Canonical +
                        ";execution-strategy=" + executionCapability.Strategy +
                        ";strategy-reason=" + executionCapability.Reason +
                        ";carrier-guid=" + blueprint.AssetGuid +
                        ";delivery-guid=" + (delivery == null
                            ? "none" : delivery.AssetGuid) +
                        ";source-ability-data=" +
                        KingmakerStickyTouchCastAdapter.Identity(ability) +
                        ";execution-ability-data=" +
                        KingmakerStickyTouchCastAdapter.Identity(targetingAbility) +
                        ";delivery-target-anchor=" + (stickyResolution == null
                            ? "unresolved" : stickyResolution.ExecutionAbility
                                .TargetAnchor.ToString()) +
                        ";delivery-target-self=" + (delivery != null &&
                            delivery.CanTargetSelf) +
                        ";delivery-target-friends=" + (delivery != null &&
                            delivery.CanTargetFriends) +
                        ";delivery-target-enemies=" + (delivery != null &&
                            delivery.CanTargetEnemies) +
                        ";delivery-target-point=" + (delivery != null &&
                            delivery.CanTargetPoint) +
                        (string.IsNullOrEmpty(stickyResolutionReason)
                            ? string.Empty : ";delivery-resolution=" +
                                stickyResolutionReason));
                }
                if (party || areaRecipients || unsafeArea || infusedPersonal)
                    _diagnostics.Add("base=" +
                        provider.Key.Ability.BaseAbilityGuid + ";variant=" +
                        provider.Key.Ability.VariantGuid + ";source=" +
                        provider.Key.Ability.Canonical + ";paths=[" +
                        string.Join(",", paths) + "];target=" + targetClass +
                        ";grouping=" + (party || areaRecipients ? "Mass" :
                            "PerTarget") + ";radius=" + (areaCoverage == null
                            ? "none" : areaCoverage.Radius.ToString("R")) +
                        ";anchors=[" + string.Join(",", anchors) +
                        "];coverage=[" + string.Join("|", recipientIdsByAnchor
                            .OrderBy(value => value.Key, StringComparer.Ordinal)
                            .Select(value => value.Key + "->" + string.Join(",",
                                value.Value.ToArray())).ToArray()) + "]" +
                        (areaRecipients && areaCoverage == null ? ";rejected=" +
                            areaResolver.LastFailureReason : string.Empty));
            }
            return options.OrderBy(o => o.Provider.Key.Canonical, StringComparer.Ordinal).ToArray();
        }

        private static IReadOnlyDictionary<string, bool> ReadCasterFacts(EffectExpression expression, UnitEntityData caster)
        {
            var facts = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (string id in EffectConditionBinding.FactIds(expression))
            {
                try
                {
                    var fact = ResourcesLibrary.TryGetBlueprint<BlueprintUnitFact>(id);
                    if (fact != null && caster != null && caster.Descriptor != null)
                        facts[id] = caster.Descriptor.HasFact(fact);
                }
                catch (Exception) { /* Unreadable is unknown, never false. */ }
            }
            return facts;
        }

        // Whether a live unit has a fact (a recipient-fact gate); null when
        // it cannot be read, never false by default.
        private static bool? ReadUnitFact(UnitEntityData unit, string factId)
        {
            try
            {
                var fact = ResourcesLibrary.TryGetBlueprint<BlueprintUnitFact>(factId);
                if (fact == null || unit == null || unit.Descriptor == null) return null;
                return unit.Descriptor.HasFact(fact);
            }
            catch (Exception) { return null; }
        }

        private static IReadOnlyList<string> SelectNearbyRecipients(NearbyRecipientRule rule,
            UnitEntityData caster, UnitEntityData target)
        {
            if (caster == null || target == null) return null;
            try
            {
                var units = Kingmaker.Designers.GameHelper.GetTargetsAround(target.Position, rule.Radius, false, false)
                    .Where(unit => unit != null && unit != target &&
                        (rule.Self || unit != caster) &&
                        (caster.IsAlly(unit) ? rule.Friends : rule.Enemies))
                    .Select(unit => new { Unit = unit, Distance = target.DistanceTo(unit) })
                    .OrderBy(value => value.Distance).ToList();
                // The native SortedDictionary also refuses equal-distance keys.
                if (units.GroupBy(value => value.Distance).Any(group => group.Count() > 1)) return null;
                return units.Take(rule.Maximum == 0 ? units.Count : rule.Maximum)
                    .Select(value => value.Unit.UniqueId).ToArray();
            }
            catch (Exception) { return null; }
        }

        // H2 (0.6.11 review): every OBSERVABLE unit (a snapshot party member
        // or pet, so the compiler's readable-owner preflight never sees a new
        // unreadable owner)
        // the native wrapper's own unit filter could legitimately select when
        // its action runs, whatever its distance now - movement between
        // admission and application can change the nearest. Distance and the
        // count bound are checked against the attempt's own observations.
        private static IReadOnlyList<string> NearbyRecipientCandidates(NearbyRecipientRule rule,
            UnitEntityData caster, UnitEntityData target, IEnumerable<UnitEntityData> observable)
        {
            if (caster == null || target == null) return new string[0];
            try
            {
                return observable.Where(unit => unit != null && unit != target &&
                        (rule.Self || unit != caster) &&
                        (caster.IsAlly(unit) ? rule.Friends : rule.Enemies))
                    .Select(unit => unit.UniqueId).Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            }
            catch (Exception) { return new string[0]; }
        }

    }
}
