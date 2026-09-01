using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.FactLogic;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.GameAdapters;

namespace KingmakerBuffPlanner.Compatibility
{
    internal sealed class PowerfulChangeRuntimeEntry
    {
        internal PowerfulChangeRuntimeEntry(ActivatableAbility ability,
            CastEnhancementSnapshot snapshot)
        {
            Ability = ability;
            Snapshot = snapshot;
        }

        internal ActivatableAbility Ability { get; private set; }
        internal CastEnhancementSnapshot Snapshot { get; private set; }
    }

    internal sealed class BrownFurPowerfulChangeCompatibility
    {
        private readonly Dictionary<string, PowerfulChangeProviderTrace> _traces =
            new Dictionary<string, PowerfulChangeProviderTrace>(
                StringComparer.Ordinal);
        private readonly List<string> _contractDiagnostics = new List<string>();
        private readonly KingmakerPowerfulChangeBlueprintAnalyzer _analyzer =
            new KingmakerPowerfulChangeBlueprintAnalyzer();

        internal IReadOnlyList<string> ContractDiagnostics
        { get { return _contractDiagnostics.AsReadOnly(); } }

        internal PowerfulChangeRuntimeEntry[] Discover(
            IEnumerable<UnitEntityData> units,
            PartyProviderSnapshot snapshot)
        {
            _traces.Clear();
            _contractDiagnostics.Clear();
            Dictionary<string, UnitEntityData> live = (units ??
                    new UnitEntityData[0]).Where(value => value != null &&
                    value.Descriptor != null &&
                    !string.IsNullOrWhiteSpace(value.UniqueId))
                .GroupBy(value => value.UniqueId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(),
                    StringComparer.Ordinal);
            var analyses = new Dictionary<string,
                PowerfulChangeBlueprintAnalysis>(StringComparer.Ordinal);
            var ownership = new Dictionary<string, bool>(StringComparer.Ordinal);
            BlueprintFeature feature = ResourcesLibrary.TryGetBlueprint<
                BlueprintFeature>(BrownFurPowerfulChangeProfile.FeatureGuid);

            foreach (ProviderSnapshot provider in snapshot == null
                ? new ProviderSnapshot[0] : snapshot.Providers)
            {
                UnitEntityData unit;
                live.TryGetValue(provider.Key.CasterUnitId, out unit);
                bool owns = unit != null && feature != null &&
                    unit.Descriptor.HasFact(feature);
                ownership[provider.Key.CasterUnitId] = owns;
                AbilityData data = unit == null ? null :
                    KingmakerAnimatedCastAdapter.ResolveAbility(unit,
                        provider.Key);
                PowerfulChangeBlueprintAnalysis analysis = null;
                if (owns && data != null)
                {
                    analysis = _analyzer.Analyze(data.Blueprint,
                        data.Spellbook != null && data.SourceItem == null,
                        provider.Key.SpellbookGuid,
                        BrownFurPowerfulChangeProfile.CastingSpellbookGuid);
                    analyses[provider.Key.Canonical] = analysis;
                }
                _traces[provider.Key.Canonical] =
                    PowerfulChangeProviderTrace.Create(unit, provider, data,
                        feature != null, owns, analysis);
            }

            var result = new List<PowerfulChangeRuntimeEntry>();
            foreach (KeyValuePair<string, UnitEntityData> pair in live
                .OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                bool owns;
                ownership.TryGetValue(pair.Key, out owns);
                if (!owns && (feature == null ||
                    !pair.Value.Descriptor.HasFact(feature))) continue;
                result.AddRange(BuildEntries(pair.Value,
                    snapshot == null ? new ProviderSnapshot[0] :
                        snapshot.Providers.Where(value =>
                            value.Key.CasterUnitId == pair.Key), analyses));
            }
            return result.ToArray();
        }

        internal PowerfulChangeRuntimeEntry[] ForCast(UnitEntityData unit,
            ProviderKey provider, AbilityData ability)
        {
            if (unit == null || unit.Descriptor == null || provider == null)
                return new PowerfulChangeRuntimeEntry[0];
            BlueprintFeature feature = ResourcesLibrary.TryGetBlueprint<
                BlueprintFeature>(BrownFurPowerfulChangeProfile.FeatureGuid);
            if (feature == null || !unit.Descriptor.HasFact(feature))
                return new PowerfulChangeRuntimeEntry[0];
            PowerfulChangeBlueprintAnalysis analysis = _analyzer.Analyze(
                ability == null ? null : ability.Blueprint,
                ability != null && ability.Spellbook != null &&
                    ability.SourceItem == null,
                provider.SpellbookGuid,
                BrownFurPowerfulChangeProfile.CastingSpellbookGuid);
            string key = provider.Canonical;
            var analyses = new Dictionary<string,
                PowerfulChangeBlueprintAnalysis>(StringComparer.Ordinal) {
                    { key, analysis }
                };
            var snapshot = new ProviderSnapshot(provider,
                ability == null ? string.Empty : ability.Name,
                ability == null ? 0 : Math.Max(0, ability.SpellLevel),
                "runtime-powerful-change", 0, null);
            return BuildEntries(unit, new[] { snapshot }, analyses).ToArray();
        }

        internal string Describe(ProviderSnapshot provider,
            IEnumerable<CastEnhancementSnapshot> enhancements)
        {
            if (provider == null) return string.Empty;
            PowerfulChangeProviderTrace trace;
            if (!_traces.TryGetValue(provider.Key.Canonical, out trace))
                return "[KBP][Enhancement] Provider=" +
                    provider.Key.Canonical +
                    ";RejectedReason=provider-trace-unavailable.";
            string[] available = (enhancements ??
                    new CastEnhancementSnapshot[0]).Where(value =>
                    value.Category == CastEnhancementCategory.ClassFeature &&
                    value.RemainingUses != 0 && value.IsApplicable(provider))
                .Select(value => value.DisplayName).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
            return trace.Describe(available);
        }

        internal static bool TryDescribePersisted(string id,
            out CastEnhancementSnapshot snapshot)
        {
            snapshot = null;
            string[] parts = (id ?? string.Empty).Split('|');
            if (parts.Length != 3 || parts[0] != "class-feature" ||
                string.IsNullOrWhiteSpace(parts[1])) return false;
            BrownFurPowerfulChangeToggleContract contract =
                BrownFurPowerfulChangeProfile.Find(parts[2]);
            if (contract == null) return false;
            BlueprintActivatableAbility blueprint = ResourcesLibrary
                .TryGetBlueprint<BlueprintActivatableAbility>(
                    contract.ActivatableGuid);
            string reason;
            if (!ValidToggleBlueprint(blueprint, contract, out reason))
                return false;
            snapshot = Snapshot(parts[1], contract, blueprint, 0,
                new string[0]);
            return true;
        }

        private IEnumerable<PowerfulChangeRuntimeEntry> BuildEntries(
            UnitEntityData unit,
            IEnumerable<ProviderSnapshot> providers,
            IDictionary<string, PowerfulChangeBlueprintAnalysis> analyses)
        {
            ProviderSnapshot[] providerList = (providers ??
                new ProviderSnapshot[0]).ToArray();
            foreach (BrownFurPowerfulChangeToggleContract contract in
                BrownFurPowerfulChangeProfile.Toggles)
            {
                ActivatableAbility ability;
                string reason;
                if (!TryResolveToggle(unit, contract, out ability, out reason))
                {
                    _contractDiagnostics.Add("caster=" + unit.UniqueId +
                        ";score=" + contract.Score + ";toggle=" +
                        contract.ActivatableGuid + ";rejected=" + reason);
                    continue;
                }
                string[] eligibleAbilities = providerList.Where(provider =>
                    {
                        PowerfulChangeBlueprintAnalysis analysis;
                        return analyses.TryGetValue(provider.Key.Canonical,
                                out analysis) && analysis.Eligibility != null &&
                            analysis.Eligibility.Supports(contract.Score);
                    }).Select(provider => SelectedAbilityGuid(provider.Key))
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                        StringComparer.Ordinal).ToArray();
                int remaining = Math.Max(0, ability.ResourceCount ?? 0);
                yield return new PowerfulChangeRuntimeEntry(ability,
                    Snapshot(unit.UniqueId, contract, ability.Blueprint,
                        remaining, eligibleAbilities));
            }
        }

        private static CastEnhancementSnapshot Snapshot(string casterUnitId,
            BrownFurPowerfulChangeToggleContract contract,
            BlueprintActivatableAbility blueprint,
            int remaining,
            IEnumerable<string> eligibleAbilityGuids)
        {
            string name = blueprint == null || string.IsNullOrWhiteSpace(
                blueprint.Name) ? "Powerful Change: " + contract.Score :
                blueprint.Name;
            string description = blueprint == null ? string.Empty :
                blueprint.Description;
            return new CastEnhancementSnapshot(
                BrownFurPowerfulChangeProfile.EnhancementId(casterUnitId,
                    contract.ActivatableGuid), casterUnitId,
                contract.ActivatableGuid, name, description,
                CastEnhancementCategory.ClassFeature, 0, 0, remaining,
                eligibleAbilityGuids,
                "Powerful Change: " + contract.Score,
                new[] { BrownFurPowerfulChangeProfile.CastingSpellbookGuid },
                BrownFurPowerfulChangeProfile.UsagePoolId(casterUnitId), true,
                "brown-fur-powerful-change", 1, false,
                "brown-fur-powerful-change", "Arcane Reservoir");
        }

        private static bool TryResolveToggle(UnitEntityData unit,
            BrownFurPowerfulChangeToggleContract contract,
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
                        contract.ActivatableGuid).ToArray();
            if (matches.Length != 1)
            {
                reason = "owned-toggle-count-" + matches.Length;
                return false;
            }
            if (!ValidToggleBlueprint(matches[0].Blueprint, contract,
                    out reason)) return false;
            ability = matches[0];
            return true;
        }

        private static bool ValidToggleBlueprint(
            BlueprintActivatableAbility blueprint,
            BrownFurPowerfulChangeToggleContract contract,
            out string reason)
        {
            reason = string.Empty;
            if (blueprint == null || blueprint.AssetGuid !=
                contract.ActivatableGuid)
            {
                reason = "toggle-blueprint-missing";
                return false;
            }
            if (blueprint.Buff == null || blueprint.Buff.AssetGuid !=
                contract.MarkerBuffGuid)
            {
                reason = "toggle-marker-contract-mismatch";
                return false;
            }
            ActivatableAbilityResourceLogic[] resources =
                (blueprint.ComponentsArray ?? new BlueprintComponent[0])
                .OfType<ActivatableAbilityResourceLogic>().ToArray();
            if (resources.Length != 1 || resources[0].RequiredResource == null ||
                resources[0].RequiredResource.AssetGuid !=
                    BrownFurPowerfulChangeProfile.ReservoirGuid ||
                resources[0].SpendType != ActivatableAbilityResourceLogic
                    .ResourceSpendType.Never)
            {
                reason = "toggle-reservoir-contract-mismatch";
                return false;
            }
            return true;
        }

        private static string SelectedAbilityGuid(ProviderKey provider)
        {
            if (provider == null || provider.Ability == null)
                return string.Empty;
            return string.IsNullOrWhiteSpace(provider.Ability.VariantGuid)
                ? provider.Ability.BaseAbilityGuid
                : provider.Ability.VariantGuid;
        }
    }

    internal sealed class PowerfulChangeProviderTrace
    {
        private PowerfulChangeProviderTrace() { }

        internal string CasterName;
        internal string CasterId;
        internal string AbilityName;
        internal string AbilityGuid;
        internal string SpellbookGuid;
        internal string School;
        internal string Descriptors;
        internal bool ProviderDetected;
        internal bool FeatureOwned;
        internal string[] Buffs = new string[0];
        internal string[] Components = new string[0];
        internal string[] Carriers = new string[0];
        internal string[] Scores = new string[0];
        internal string RejectedReason;

        internal static PowerfulChangeProviderTrace Create(UnitEntityData unit,
            ProviderSnapshot provider, AbilityData data, bool providerDetected,
            bool featureOwned, PowerfulChangeBlueprintAnalysis analysis)
        {
            string selected = provider == null ? string.Empty :
                (string.IsNullOrWhiteSpace(provider.Key.Ability.VariantGuid)
                    ? provider.Key.Ability.BaseAbilityGuid
                    : provider.Key.Ability.VariantGuid);
            var trace = new PowerfulChangeProviderTrace {
                CasterName = unit == null ? string.Empty : unit.CharacterName,
                CasterId = provider == null ? string.Empty :
                    provider.Key.CasterUnitId,
                AbilityName = data == null ? (provider == null ? string.Empty :
                    provider.DisplayName) : data.Name,
                AbilityGuid = data == null || data.Blueprint == null ? selected :
                    data.Blueprint.AssetGuid,
                SpellbookGuid = provider == null ? string.Empty :
                    provider.Key.SpellbookGuid,
                School = data == null || data.Blueprint == null ? "<unresolved>" :
                    data.Blueprint.School.ToString(),
                Descriptors = data == null || data.Blueprint == null ?
                    "<unresolved>" : data.Blueprint.SpellDescriptor.ToString(),
                ProviderDetected = providerDetected,
                FeatureOwned = featureOwned
            };
            if (!providerDetected)
                trace.RejectedReason = "optional-provider-blueprints-absent";
            else if (!featureOwned)
                trace.RejectedReason = "powerful-change-feature-not-owned";
            else if (analysis == null || analysis.Eligibility == null)
                trace.RejectedReason = "ability-analysis-unavailable";
            else
            {
                trace.Buffs = analysis.AppliedBuffGuids;
                trace.Components = analysis.ComponentTypes;
                trace.Carriers = analysis.Eligibility.CarrierFamilies.ToArray();
                trace.Scores = analysis.Eligibility.AbilityScores.Select(value =>
                    value.ToString()).ToArray();
                trace.RejectedReason = analysis.Eligibility.Eligible ?
                    string.Empty : analysis.Eligibility.Reason;
            }
            return trace;
        }

        internal string Describe(IEnumerable<string> availableEnhancements)
        {
            string[] available = (availableEnhancements ?? new string[0])
                .ToArray();
            bool qualifies = Scores.Length != 0 &&
                string.IsNullOrEmpty(RejectedReason);
            return "[KBP][Enhancement] Caster=" + CasterName +
                ";CasterId=" + CasterId + ";Ability=" + AbilityName + " (" +
                AbilityGuid + ");ResultingBuffs=[" + string.Join(",", Buffs) +
                "];Spellbook=" + SpellbookGuid + ";School=" + School +
                ";Descriptors=" + Descriptors +
                ";PowerfulChangeProviderDetected=" + ProviderDetected +
                ";PowerfulChangeDetected=" + FeatureOwned +
                ";PowerfulChangeFeature=" + (FeatureOwned ?
                    BrownFurPowerfulChangeProfile.FeatureGuid : "none") +
                ";Components=[" + string.Join(",", Components) +
                "];CarrierFamilies=[" + string.Join(",", Carriers) +
                "];MatchedScores=[" + string.Join(",", Scores) +
                "];QualifiesForPowerfulChange=" + qualifies +
                ";RejectedReason=" + (string.IsNullOrEmpty(RejectedReason) ?
                    "none" : RejectedReason) + ";AvailableEnhancements=[" +
                string.Join(",", available) + "].";
        }
    }
}
