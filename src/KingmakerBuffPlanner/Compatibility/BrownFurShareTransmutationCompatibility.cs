using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.FactLogic;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.GameAdapters;

namespace KingmakerBuffPlanner.Compatibility
{
    internal sealed class ShareTransmutationRuntimeEntry
    {
        internal ShareTransmutationRuntimeEntry(ActivatableAbility ability,
            CastEnhancementSnapshot snapshot)
        {
            Ability = ability;
            Snapshot = snapshot;
        }

        internal ActivatableAbility Ability { get; private set; }
        internal CastEnhancementSnapshot Snapshot { get; private set; }
    }

    /// <summary>
    /// Optional exact-profile integration. It proves the feature, toggle,
    /// marker, reservoir, spell shape, and native TargetAnchor patch by
    /// temporarily arming only the exact Share toggle. No optional type is
    /// referenced and no resource or cast transaction is invoked.
    /// </summary>
    internal sealed class BrownFurShareTransmutationCompatibility
    {
        private readonly List<string> _diagnostics = new List<string>();

        internal IReadOnlyList<string> ContractDiagnostics
        { get { return _diagnostics.AsReadOnly(); } }

        internal ShareTransmutationRuntimeEntry[] Discover(
            IEnumerable<UnitEntityData> units,
            PartyProviderSnapshot snapshot)
        {
            _diagnostics.Clear();
            var result = new List<ShareTransmutationRuntimeEntry>();
            string nativeReason;
            if (!TryValidateNativeRuntime(out nativeReason))
            {
                _diagnostics.Add("provider=rejected;reason=" + nativeReason);
                return result.ToArray();
            }
            BlueprintFeature feature = ResourcesLibrary.TryGetBlueprint<
                BlueprintFeature>(BrownFurShareTransmutationProfile.FeatureGuid);
            string featureReason;
            if (!ValidFeatureBlueprint(feature, out featureReason))
            {
                _diagnostics.Add("provider=rejected;reason=" + featureReason);
                return result.ToArray();
            }
            foreach (UnitEntityData unit in (units ?? new UnitEntityData[0])
                .Where(value => value != null && value.Descriptor != null &&
                    !string.IsNullOrWhiteSpace(value.UniqueId))
                .OrderBy(value => value.UniqueId, StringComparer.Ordinal))
            {
                if (!unit.Descriptor.HasFact(feature)) continue;
                ActivatableAbility toggle;
                string reason;
                if (!TryResolveToggle(unit, out toggle, out reason))
                {
                    _diagnostics.Add("caster=" + unit.UniqueId +
                        ";rejected=" + reason);
                    continue;
                }
                var abilities = new List<string>();
                var spellbooks = new List<string>();
                foreach (ProviderSnapshot provider in snapshot == null
                    ? new ProviderSnapshot[0] : snapshot.Providers.Where(value =>
                        value.Key.CasterUnitId == unit.UniqueId))
                {
                    AbilityData data = KingmakerAnimatedCastAdapter.ResolveAbility(
                        unit, provider.Key);
                    if (!IsSupportedSpell(data, provider.Key, toggle,
                            out reason))
                    {
                        _diagnostics.Add("caster=" + unit.UniqueId +
                            ";provider=" + provider.Key.Canonical +
                            ";rejected=" + reason);
                        continue;
                    }
                    abilities.Add(SelectedAbilityGuid(provider.Key));
                    spellbooks.Add(provider.Key.SpellbookGuid);
                }
                if (abilities.Count == 0) continue;
                result.Add(new ShareTransmutationRuntimeEntry(toggle,
                    Snapshot(unit.UniqueId, toggle.Blueprint,
                        Math.Max(0, toggle.ResourceCount ?? 0), abilities,
                        spellbooks)));
            }
            return result.ToArray();
        }

        internal ShareTransmutationRuntimeEntry[] ForCast(
            UnitEntityData unit, ProviderKey provider, AbilityData ability)
        {
            if (unit == null || unit.Descriptor == null || provider == null)
                return new ShareTransmutationRuntimeEntry[0];
            string nativeReason;
            if (!TryValidateNativeRuntime(out nativeReason))
                return new ShareTransmutationRuntimeEntry[0];
            BlueprintFeature feature = ResourcesLibrary.TryGetBlueprint<
                BlueprintFeature>(BrownFurShareTransmutationProfile.FeatureGuid);
            string featureReason;
            if (!ValidFeatureBlueprint(feature, out featureReason) ||
                !unit.Descriptor.HasFact(feature))
                return new ShareTransmutationRuntimeEntry[0];
            ActivatableAbility toggle;
            string reason;
            if (!TryResolveToggle(unit, out toggle, out reason) ||
                !IsSupportedSpell(ability, provider, toggle, out reason))
                return new ShareTransmutationRuntimeEntry[0];
            return new[] { new ShareTransmutationRuntimeEntry(toggle,
                Snapshot(unit.UniqueId, toggle.Blueprint,
                    Math.Max(0, toggle.ResourceCount ?? 0),
                    new[] { SelectedAbilityGuid(provider) },
                    new[] { provider.SpellbookGuid })) };
        }

        internal static bool TryDescribePersisted(string id,
            out CastEnhancementSnapshot snapshot)
        {
            snapshot = null;
            string[] parts = (id ?? string.Empty).Split('|');
            if (parts.Length != 3 || parts[0] != "share-transmutation" ||
                string.IsNullOrWhiteSpace(parts[1]) || parts[2] !=
                    BrownFurShareTransmutationProfile.ActivatableGuid)
                return false;
            BlueprintActivatableAbility blueprint = ResourcesLibrary
                .TryGetBlueprint<BlueprintActivatableAbility>(parts[2]);
            string reason;
            if (!ValidToggleBlueprint(blueprint, out reason)) return false;
            snapshot = Snapshot(parts[1], blueprint, 0, new string[0],
                new string[0]);
            return true;
        }

        internal static bool TryResolveToggle(UnitEntityData unit,
            out ActivatableAbility ability, out string reason)
        {
            ability = null;
            reason = string.Empty;
            if (unit == null || unit.Descriptor == null ||
                unit.Descriptor.ActivatableAbilities == null)
            {
                reason = "caster-activatables-unavailable";
                return false;
            }
            ActivatableAbility[] matches = unit.Descriptor.ActivatableAbilities
                .Enumerable.Where(value => value != null &&
                    value.Blueprint != null && value.Blueprint.AssetGuid ==
                        BrownFurShareTransmutationProfile.ActivatableGuid)
                .ToArray();
            if (matches.Length != 1)
            {
                reason = "owned-share-toggle-count-" + matches.Length;
                return false;
            }
            if (!ValidToggleBlueprint(matches[0].Blueprint, out reason))
                return false;
            ability = matches[0];
            return true;
        }

        internal static bool TryValidateNativeRuntime(out string reason)
        {
            reason = string.Empty;
            try
            {
                Assembly[] matches = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(value => value.GetName().Name ==
                        "KingmakerGunslinger").ToArray();
                if (matches.Length != 1)
                    return Fail("optional-provider-assembly-count-" +
                        matches.Length, out reason);
                var contracts = new Dictionary<string, string[]> {
                    { "KingmakerGunslinger.BrownFur.BrownFurCastIntentRuntime",
                        new[] { "Arm" } },
                    { "KingmakerGunslinger.BrownFur.BrownFurShareTargetingRuntime",
                        new[] { "TryOverrideAnchor", "TryOverrideTarget",
                            "TryOverrideApproachDistance" } },
                    { "KingmakerGunslinger.BrownFur.BrownFurExactDebitPolicy",
                        new[] { "TryDebitExact" } },
                    { "KingmakerGunslinger.BrownFur.BrownFurShareTargetAnchorPatch",
                        new[] { "Postfix" } },
                    { "KingmakerGunslinger.BrownFur.BrownFurShareCanTargetPatch",
                        new[] { "Postfix" } },
                    { "KingmakerGunslinger.BrownFur.BrownFurShareApproachDistancePatch",
                        new[] { "Postfix" } }
                };
                const BindingFlags flags = BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.Instance |
                    BindingFlags.Static | BindingFlags.DeclaredOnly;
                foreach (KeyValuePair<string, string[]> contract in contracts)
                {
                    Type type = matches[0].GetType(contract.Key, false);
                    if (type == null)
                        return Fail("native-type-missing:" + contract.Key,
                            out reason);
                    foreach (string method in contract.Value)
                        if (!type.GetMethods(flags).Any(value => value.Name ==
                                method))
                            return Fail("native-method-missing:" + contract.Key +
                                "::" + method, out reason);
                }
                return true;
            }
            catch (Exception exception)
            {
                return Fail("native-contract-probe-exception:" +
                    exception.GetType().Name, out reason);
            }
        }

        internal static bool IsSupportedSpell(AbilityData ability,
            ProviderKey provider, ActivatableAbility toggle,
            out string reason)
        {
            reason = string.Empty;
            if (ability == null || ability.Blueprint == null)
                return Fail("ability-unresolved", out reason);
            if (provider == null || provider.Ability.SourceKind !=
                    SourceKind.Spellbook || ability.Spellbook == null ||
                ability.SourceItem != null)
                return Fail("source-not-genuine-spellbook-spell", out reason);
            if (ability.Blueprint.Range != AbilityRange.Personal)
                return Fail("range-not-personal", out reason);
            if (ability.Blueprint.School != SpellSchool.Transmutation)
                return Fail("school-not-transmutation", out reason);
            bool original = toggle.IsOn;
            try
            {
                toggle.IsOn = true;
                if (!toggle.IsOn)
                    return Fail("native-share-activation-refused", out reason);
                if (ability.TargetAnchor != AbilityTargetAnchor.Unit)
                    return Fail("native-share-target-anchor-not-augmented",
                        out reason);
                return true;
            }
            catch (Exception exception)
            {
                return Fail("native-share-probe-exception:" +
                    exception.GetType().Name, out reason);
            }
            finally
            {
                try { toggle.IsOn = original; }
                catch (Exception) { }
            }
        }

        private static bool ValidToggleBlueprint(
            BlueprintActivatableAbility blueprint, out string reason)
        {
            reason = string.Empty;
            if (blueprint == null || blueprint.AssetGuid !=
                BrownFurShareTransmutationProfile.ActivatableGuid)
                return Fail("share-toggle-blueprint-missing", out reason);
            if (blueprint.Buff == null || blueprint.Buff.AssetGuid !=
                BrownFurShareTransmutationProfile.MarkerBuffGuid)
                return Fail("share-toggle-marker-contract-mismatch", out reason);
            if ((blueprint.Buff.ComponentsArray ??
                    new BlueprintComponent[0]).Length != 0)
                return Fail("share-marker-component-contract-mismatch",
                    out reason);
            ActivatableAbilityResourceLogic[] resources =
                (blueprint.ComponentsArray ?? new BlueprintComponent[0])
                    .OfType<ActivatableAbilityResourceLogic>().ToArray();
            if ((blueprint.ComponentsArray ??
                    new BlueprintComponent[0]).Length != 1 ||
                resources.Length != 1 || resources[0].RequiredResource == null ||
                resources[0].RequiredResource.AssetGuid !=
                    BrownFurShareTransmutationProfile.ReservoirGuid ||
                resources[0].SpendType != ActivatableAbilityResourceLogic
                    .ResourceSpendType.Never)
                return Fail("share-toggle-reservoir-contract-mismatch",
                    out reason);
            if (blueprint.Group != ActivatableAbilityGroup.None ||
                blueprint.WeightInGroup != 1 || blueprint.IsOnByDefault ||
                blueprint.OnlyInCombat || blueprint.ActivationType !=
                    AbilityActivationType.Immediately)
                return Fail("share-toggle-activation-contract-mismatch",
                    out reason);
            return true;
        }

        private static bool ValidFeatureBlueprint(BlueprintFeature feature,
            out string reason)
        {
            reason = string.Empty;
            if (feature == null || feature.AssetGuid !=
                    BrownFurShareTransmutationProfile.FeatureGuid)
                return Fail("optional-share-feature-absent", out reason);
            BlueprintComponent[] components = feature.ComponentsArray ??
                new BlueprintComponent[0];
            AddFacts[] grants = components.OfType<AddFacts>().ToArray();
            if (components.Length != 1 || grants.Length != 1 ||
                grants[0].Facts == null || grants[0].Facts.Length != 1 ||
                grants[0].Facts[0] == null || grants[0].Facts[0].AssetGuid !=
                    BrownFurShareTransmutationProfile.ActivatableGuid)
                return Fail("share-feature-grant-contract-mismatch", out reason);
            return true;
        }

        private static CastEnhancementSnapshot Snapshot(string casterUnitId,
            BlueprintActivatableAbility blueprint, int remaining,
            IEnumerable<string> abilities, IEnumerable<string> spellbooks)
        {
            return new CastEnhancementSnapshot(
                BrownFurShareTransmutationProfile.EnhancementId(casterUnitId),
                casterUnitId,
                BrownFurShareTransmutationProfile.ActivatableGuid,
                blueprint == null || string.IsNullOrWhiteSpace(blueprint.Name)
                    ? "Share Transmutation" : blueprint.Name,
                blueprint == null ? string.Empty : blueprint.Description,
                CastEnhancementCategory.ClassFeature, 0, 0, remaining,
                abilities, "Share Transmutation", spellbooks,
                BrownFurShareTransmutationProfile.UsagePoolId(casterUnitId),
                true, "brown-fur-share-transmutation", 1, true,
                "brown-fur-share-transmutation", "Arcane Reservoir");
        }

        private static string SelectedAbilityGuid(ProviderKey provider)
        {
            return provider == null || provider.Ability == null ? string.Empty :
                (string.IsNullOrWhiteSpace(provider.Ability.VariantGuid)
                    ? provider.Ability.BaseAbilityGuid
                    : provider.Ability.VariantGuid);
        }

        private static bool Fail(string value, out string reason)
        {
            reason = value;
            return false;
        }
    }
}
