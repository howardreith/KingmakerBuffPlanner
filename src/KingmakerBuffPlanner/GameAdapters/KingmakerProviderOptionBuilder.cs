using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
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
            var units = snapshot.Units.Where(u => u.TargetValidation.Alive &&
                u.TargetValidation.Friendly && u.TargetValidation.Targetable).ToArray();
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
                bool party = EffectExpressionTargetAnalysis.Contains(
                    expression, EffectTarget.Party);
                bool areaRecipients = EffectExpressionTargetAnalysis.Contains(
                    expression, EffectTarget.AlliedAreaRecipients);
                bool unsafeArea = EffectExpressionTargetAnalysis.Contains(
                        expression, EffectTarget.EnemyAreaRecipients) ||
                    EffectExpressionTargetAnalysis.Contains(expression,
                        EffectTarget.AmbiguousAreaRecipients);
                bool infusedPersonal = EffectExpressionTargetAnalysis
                    .ContainsOnly(expression, EffectTarget.Caster) &&
                    ability != null && ability.IsAlchemistSpell &&
                    ability.AlchemistInfusion && ability.TargetAnchor ==
                        AbilityTargetAnchor.Unit;
                IEnumerable<UnitSnapshot> reachable;
                var recipientIdsByAnchor =
                    new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal);
                string[] anchors;
                if (EffectExpressionTargetAnalysis.ContainsOnly(expression,
                        EffectTarget.Caster) && !infusedPersonal)
                {
                    reachable = units.Where(u => u.UnitId == provider.Key.CasterUnitId);
                    anchors = reachable.Select(u => u.UnitId).ToArray();
                }
                else if (EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.Pet))
                {
                    reachable = units.Where(u => u.IsPet && u.MasterUnitId == provider.Key.CasterUnitId);
                    anchors = reachable.Select(u => u.UnitId).ToArray();
                }
                else if (party)
                {
                    reachable = units;
                    anchors = LegalAnchorIds(targetingAbility, units, liveUnits);
                    if (anchors.Length == 0 && units.Any(unit =>
                        unit.UnitId == provider.Key.CasterUnitId))
                        anchors = new[] { provider.Key.CasterUnitId };
                    foreach (string anchor in anchors)
                        recipientIdsByAnchor.Add(anchor,
                            units.Select(unit => unit.UnitId).ToArray());
                }
                else if (areaRecipients && areaCoverage != null && caster != null)
                {
                    float radius = areaCoverage.Radius;
                    anchors = LegalAnchorIds(targetingAbility, units, liveUnits);
                    foreach (string anchorId in anchors)
                    {
                        UnitEntityData anchor;
                        if (!liveUnits.TryGetValue(anchorId, out anchor)) continue;
                        recipientIdsByAnchor.Add(anchorId, units.Where(unit =>
                        {
                            UnitEntityData target;
                            return liveUnits.TryGetValue(unit.UnitId, out target) &&
                                anchor.DistanceTo(target) <= radius + 0.01f;
                        }).Select(unit => unit.UnitId).ToArray());
                    }
                    reachable = units.Where(unit => recipientIdsByAnchor.Values
                        .Any(recipients => recipients.Contains(unit.UnitId)));
                }
                else if (areaRecipients || unsafeArea)
                {
                    reachable = new UnitSnapshot[0];
                    anchors = new string[0];
                }
                else
                {
                    reachable = units.Where(u =>
                    {
                        UnitEntityData target;
                        return liveUnits.TryGetValue(u.UnitId, out target) &&
                            KingmakerAnimatedCastAdapter.CanTarget(targetingAbility,
                                new TargetWrapper(target));
                    });
                    anchors = reachable.Select(u => u.UnitId).ToArray();
                }
                string[] reachableIds = reachable.Select(u => u.UnitId)
                    .Distinct(StringComparer.Ordinal).ToArray();
                options.Add(new ProviderPlanningOption(provider, reachableIds, anchors,
                    provider.EffectiveCasterLevel, provider.ExpectedDurationRounds,
                    executionCapability.Strategy,
                    executionCapability.Reason,
                    recipientIdsByAnchor));
                string targetClass = party ? "Party" : areaRecipients
                    ? "AlliedAreaRecipients" : unsafeArea
                        ? "UnsafeArea" : infusedPersonal
                            ? "NativeAlchemistInfusion" :
                            EffectExpressionTargetAnalysis.ContainsOnly(
                                expression, EffectTarget.Caster)
                                ? "Caster" : "Direct";
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

        private static string[] LegalAnchorIds(
            AbilityData ability,
            IEnumerable<UnitSnapshot> units,
            IDictionary<string, UnitEntityData> liveUnits)
        {
            return (units ?? new UnitSnapshot[0]).Where(unit =>
            {
                UnitEntityData target;
                return liveUnits != null && liveUnits.TryGetValue(unit.UnitId, out target) &&
                    KingmakerAnimatedCastAdapter.CanTarget(ability, new TargetWrapper(target));
            }).Select(unit => unit.UnitId).Distinct(StringComparer.Ordinal)
                .OrderBy(unitId => unitId, StringComparer.Ordinal).ToArray();
        }

    }
}
