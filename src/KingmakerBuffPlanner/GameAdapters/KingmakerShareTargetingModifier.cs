using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.Utility;
using KingmakerBuffPlanner.Compatibility;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerShareTargetingModifier :
        ICastTargetingModifier
    {
        private readonly List<string> _diagnostics = new List<string>();

        internal IReadOnlyList<string> Diagnostics
        { get { return _diagnostics.AsReadOnly(); } }

        internal IReadOnlyList<string> DrainDiagnostics()
        {
            string[] values = _diagnostics.ToArray();
            _diagnostics.Clear();
            return values;
        }

        public ProviderPlanningOption Apply(
            EffectiveProviderOptionContext context,
            ProviderPlanningOption option)
        {
            CastEnhancementSnapshot[] selected = context.SelectedEnhancements
                .Where(value => value.AffectsTargeting).ToArray();
            if (selected.Length == 0) return option;
            if (selected.Length != 1 || selected[0].SourceBlueprintGuid !=
                    BrownFurShareTransmutationProfile.ActivatableGuid)
                return Reject(option, "unsupported-targeting-enhancement-set");
            CastEnhancementSnapshot share = selected[0];
            if (!share.IsApplicable(option.Provider))
                return Reject(option, "share-provider-inapplicable");
            string reason;
            if (!BrownFurShareTransmutationCompatibility
                    .TryValidateNativeRuntime(out reason))
                return Reject(option, reason);
            Dictionary<string, UnitEntityData> live =
                KingmakerAnimatedCastAdapter.CollectUnits();
            UnitEntityData caster;
            if (!live.TryGetValue(option.Provider.Key.CasterUnitId, out caster))
                return Reject(option, "share-caster-unresolved");
            AbilityData ability = KingmakerAnimatedCastAdapter.ResolveAbility(
                caster, option.Provider.Key);
            ActivatableAbility toggle;
            if (!BrownFurShareTransmutationCompatibility.TryResolveToggle(
                    caster, out toggle, out reason) ||
                !BrownFurShareTransmutationCompatibility.IsSupportedSpell(
                    ability, option.Provider.Key, toggle, out reason))
                return Reject(option, reason);

            bool original = toggle.IsOn;
            try
            {
                toggle.IsOn = true;
                if (!toggle.IsOn || ability.TargetAnchor !=
                    AbilityTargetAnchor.Unit)
                    return Reject(option, "share-native-probe-refused");
                string[] legal = context.Snapshot.Units.Where(unit =>
                    unit.TargetValidation.Alive &&
                    unit.TargetValidation.Friendly &&
                    unit.TargetValidation.Targetable).Where(unit =>
                    {
                        UnitEntityData target;
                        return live.TryGetValue(unit.UnitId, out target) &&
                            KingmakerAnimatedCastAdapter.CanTarget(ability,
                                new TargetWrapper(target));
                    }).Select(unit => unit.UnitId).Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();
                Record("provider=" + option.Provider.Key.Canonical +
                    ";share=accepted;legalTargets=" + string.Join(",", legal));
                return new ProviderPlanningOption(option.Provider, legal, legal,
                    option.EffectiveCasterLevel,
                    option.ExpectedDurationRounds, true);
            }
            catch (Exception exception)
            {
                return Reject(option, "share-target-probe-exception:" +
                    exception.GetType().Name);
            }
            finally
            {
                try { toggle.IsOn = original; }
                catch (Exception) { }
            }
        }

        private ProviderPlanningOption Reject(ProviderPlanningOption option,
            string reason)
        {
            Record("provider=" + option.Provider.Key.Canonical +
                ";share=rejected;reason=" + reason);
            return null;
        }

        private void Record(string value)
        {
            if (_diagnostics.Count < 64 && !_diagnostics.Contains(value))
                _diagnostics.Add(value);
        }
    }
}
