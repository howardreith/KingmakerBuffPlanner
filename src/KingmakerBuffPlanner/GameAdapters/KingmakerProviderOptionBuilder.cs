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
        internal ProviderPlanningOption[] Build(
            PartyProviderSnapshot snapshot,
            IDictionary<string, EffectExpression> effectsBySource)
        {
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
                AlliedAreaCoverage areaCoverage = blueprint == null
                    ? null : new KingmakerAreaCoverageResolver().Resolve(blueprint);
                bool party = EffectExpressionTargetAnalysis.Contains(
                    expression, EffectTarget.Party);
                bool areaRecipients = EffectExpressionTargetAnalysis.Contains(
                    expression, EffectTarget.AlliedAreaRecipients);
                IEnumerable<UnitSnapshot> reachable;
                var recipientIdsByAnchor =
                    new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal);
                string[] anchors;
                if (EffectExpressionTargetAnalysis.ContainsOnly(expression, EffectTarget.Caster))
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
                    anchors = LegalAnchorIds(ability, units, liveUnits);
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
                    anchors = LegalAnchorIds(ability, units, liveUnits);
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
                else
                {
                    reachable = units.Where(u =>
                    {
                        UnitEntityData target;
                        return liveUnits.TryGetValue(u.UnitId, out target) &&
                            KingmakerAnimatedCastAdapter.CanTarget(ability, new TargetWrapper(target));
                    });
                    anchors = reachable.Select(u => u.UnitId).ToArray();
                }
                string[] reachableIds = reachable.Select(u => u.UnitId)
                    .Distinct(StringComparer.Ordinal).ToArray();
                bool mass = party || recipientIdsByAnchor.Count != 0;
                options.Add(new ProviderPlanningOption(provider, reachableIds, anchors,
                    provider.EffectiveCasterLevel, provider.ExpectedDurationRounds,
                    blueprint != null && blueprint.StickyTouch != null,
                    recipientIdsByAnchor));
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
