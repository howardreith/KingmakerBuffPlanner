using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
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
            var options = new List<ProviderPlanningOption>();
            foreach (ProviderSnapshot provider in snapshot.Providers)
            {
                EffectExpression expression;
                if (!effectsBySource.TryGetValue(provider.Key.Ability.Canonical, out expression)) continue;
                string guid = string.IsNullOrEmpty(provider.Key.Ability.VariantGuid)
                    ? provider.Key.Ability.BaseAbilityGuid : provider.Key.Ability.VariantGuid;
                BlueprintAbility blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(guid);
                IEnumerable<UnitSnapshot> reachable;
                if (EffectExpressionTargetAnalysis.ContainsOnly(expression, EffectTarget.Caster))
                    reachable = units.Where(u => u.UnitId == provider.Key.CasterUnitId);
                else if (EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.Pet))
                    reachable = units.Where(u => u.IsPet && u.MasterUnitId == provider.Key.CasterUnitId);
                else if (EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.Party))
                    reachable = units;
                else
                    reachable = units.Where(u => blueprint != null &&
                        (u.UnitId == provider.Key.CasterUnitId
                            ? blueprint.CanTargetSelf : blueprint.CanTargetFriends));
                string[] reachableIds = reachable.Select(u => u.UnitId).Distinct(StringComparer.Ordinal).ToArray();
                bool mass = EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.Party);
                string[] anchors = mass
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
