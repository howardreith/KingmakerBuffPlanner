using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.Utility;
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
                AbilityTargetsAround targetsAround = blueprint == null
                    ? null : blueprint.GetComponent<AbilityTargetsAround>();
                bool party = EffectExpressionTargetAnalysis.Contains(
                    expression, EffectTarget.Party);
                bool areaRecipients = EffectExpressionTargetAnalysis.Contains(
                    expression, EffectTarget.AlliedAreaRecipients);
                IEnumerable<UnitSnapshot> reachable;
                if (EffectExpressionTargetAnalysis.ContainsOnly(expression, EffectTarget.Caster))
                    reachable = units.Where(u => u.UnitId == provider.Key.CasterUnitId);
                else if (EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.Pet))
                    reachable = units.Where(u => u.IsPet && u.MasterUnitId == provider.Key.CasterUnitId);
                else if (party)
                    reachable = units;
                else if (areaRecipients && targetsAround != null && caster != null)
                {
                    float radius = targetsAround.AoERadius.Meters;
                    reachable = targetsAround.TargetType != TargetType.Ally
                        ? new UnitSnapshot[0] : units.Where(u =>
                    {
                        UnitEntityData target;
                        return liveUnits.TryGetValue(u.UnitId, out target) &&
                            caster.DistanceTo(target) <= radius + 0.01f;
                    });
                }
                else
                    reachable = units.Where(u =>
                    {
                        UnitEntityData target;
                        return liveUnits.TryGetValue(u.UnitId, out target) &&
                            KingmakerAnimatedCastAdapter.CanTarget(ability, new TargetWrapper(target));
                    });
                string[] reachableIds = reachable.Select(u => u.UnitId)
                    .Distinct(StringComparer.Ordinal).ToArray();
                bool mass = party || areaRecipients;
                bool casterCentered = party || targetsAround != null;
                string[] anchors = mass && casterCentered
                    ? reachableIds.Where(id => id == provider.Key.CasterUnitId).ToArray()
                    : reachableIds;
                options.Add(new ProviderPlanningOption(provider, reachableIds, anchors,
                    provider.EffectiveCasterLevel, provider.ExpectedDurationRounds,
                    blueprint != null && blueprint.StickyTouch != null));
            }
            return options.OrderBy(o => o.Provider.Key.Canonical, StringComparer.Ordinal).ToArray();
        }
    }
}
