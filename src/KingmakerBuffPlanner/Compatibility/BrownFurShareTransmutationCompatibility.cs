using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;
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
            string directReason;
            bool direct = BrownFurDirectCastCompatibility
                .TryValidateContract(out directReason);
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
                !direct, "brown-fur-share-transmutation", 1, true,
                "brown-fur-share-transmutation", "Arcane Reservoir",
                direct ? BrownFurDirectCastCompatibility.ProviderId : null);
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

    internal sealed class BrownFurDirectCastStatusSnapshot
    {
        internal BrownFurDirectCastStatusSnapshot(bool accepted,
            bool committed, bool complete, bool residualState, string state,
            string failure, string detail, string transactionIdentity,
            int reservoirCost)
        {
            Accepted = accepted;
            Committed = committed;
            Complete = complete;
            ResidualState = residualState;
            State = state ?? string.Empty;
            Failure = failure ?? string.Empty;
            Detail = detail ?? string.Empty;
            TransactionIdentity = transactionIdentity ?? string.Empty;
            ReservoirCost = reservoirCost;
        }

        internal bool Accepted { get; private set; }
        internal bool Committed { get; private set; }
        internal bool Complete { get; private set; }
        internal bool ResidualState { get; private set; }
        internal string State { get; private set; }
        internal string Failure { get; private set; }
        internal string Detail { get; private set; }
        internal string TransactionIdentity { get; private set; }
        internal int ReservoirCost { get; private set; }

        internal string Describe()
        {
            return "accepted:" + Accepted + ";committed:" + Committed +
                ";complete:" + Complete + ";residual:" + ResidualState +
                ";state:" + State + ";failure:" +
                (string.IsNullOrWhiteSpace(Failure) ? "none" : Failure) +
                ";detail:" + Detail + ";transaction:" +
                TransactionIdentity + ";reservoir-cost:" + ReservoirCost;
        }
    }

    internal sealed class BrownFurDirectCastLease : IDisposable
    {
        private readonly BrownFurDirectCastContract _contract;
        private readonly object _handle;
        private bool _disposed;

        internal BrownFurDirectCastLease(
            BrownFurDirectCastContract contract, object handle)
        {
            _contract = contract ?? throw new ArgumentNullException("contract");
            _handle = handle ?? throw new ArgumentNullException("handle");
        }

        internal BrownFurDirectCastStatusSnapshot Inspect()
        {
            return _contract.InvokeStatus(_contract.Inspect, _handle,
                new object[0]);
        }

        internal BrownFurDirectCastStatusSnapshot CompleteRule(
            RuleCastSpell rule)
        {
            return _contract.InvokeStatus(_contract.CompleteRule, _handle,
                new object[] { rule });
        }

        internal BrownFurDirectCastStatusSnapshot Cleanup()
        {
            return _contract.InvokeStatus(_contract.Cleanup, _handle,
                new object[0]);
        }

        public void Dispose()
        {
            if (_disposed) return;
            ((IDisposable)_handle).Dispose();
            _disposed = true;
        }
    }

    internal sealed class BrownFurDirectCastContract
    {
        internal BrownFurDirectCastContract(MethodInfo validate,
            MethodInfo begin, MethodInfo inspect, MethodInfo completeRule,
            MethodInfo cleanup, IDictionary<string, PropertyInfo> properties)
        {
            Validate = validate;
            Begin = begin;
            Inspect = inspect;
            CompleteRule = completeRule;
            Cleanup = cleanup;
            Properties = properties;
        }

        internal MethodInfo Validate { get; private set; }
        internal MethodInfo Begin { get; private set; }
        internal MethodInfo Inspect { get; private set; }
        internal MethodInfo CompleteRule { get; private set; }
        internal MethodInfo Cleanup { get; private set; }
        internal IDictionary<string, PropertyInfo> Properties
        { get; private set; }

        internal BrownFurDirectCastStatusSnapshot InvokeStatus(
            MethodInfo method, object instance, object[] arguments)
        {
            object status = method.Invoke(instance, arguments);
            if (status == null) throw new InvalidOperationException(
                "Provider direct-cast status was null.");
            return new BrownFurDirectCastStatusSnapshot(
                (bool)Properties["Accepted"].GetValue(status, null),
                (bool)Properties["Committed"].GetValue(status, null),
                (bool)Properties["Complete"].GetValue(status, null),
                (bool)Properties["ResidualState"].GetValue(status, null),
                (string)Properties["State"].GetValue(status, null),
                (string)Properties["Failure"].GetValue(status, null),
                (string)Properties["Detail"].GetValue(status, null),
                (string)Properties["TransactionIdentity"].GetValue(
                    status, null),
                (int)Properties["ReservoirCost"].GetValue(status, null));
        }
    }

    /// <summary>
    /// Bounded reflection bridge to the provider-owned transaction. The
    /// version, public types, exact game parameters, return types, and status
    /// surface are all validated before Instant routing is advertised.
    /// </summary>
    internal static class BrownFurDirectCastCompatibility
    {
        internal const string ProviderId = "brown-fur-direct-cast-v1";
        private const int ContractVersion = 1;
        private const string ProviderAssemblyName = "KingmakerGunslinger";
        private const string ApiTypeName =
            "KingmakerGunslinger.BrownFur.BrownFurDirectCastApi";
        private const string HandleTypeName =
            "KingmakerGunslinger.BrownFur.BrownFurDirectCastHandle";
        private const string StatusTypeName =
            "KingmakerGunslinger.BrownFur.BrownFurDirectCastStatus";

        internal static bool TryValidateContract(out string reason)
        {
            BrownFurDirectCastContract contract;
            return TryResolve(out contract, out reason);
        }

        internal static bool TryValidate(AbilityData ability,
            TargetWrapper target, out BrownFurDirectCastStatusSnapshot status,
            out string reason)
        {
            status = null;
            BrownFurDirectCastContract contract;
            if (!TryResolve(out contract, out reason)) return false;
            try
            {
                status = contract.InvokeStatus(contract.Validate, null,
                    new object[] { ability, target });
                return true;
            }
            catch (Exception exception)
            {
                reason = InvocationFailure("validate", exception);
                return false;
            }
        }

        internal static bool TryBegin(AbilityData ability,
            TargetWrapper target, out BrownFurDirectCastLease lease,
            out BrownFurDirectCastStatusSnapshot status, out string reason)
        {
            lease = null;
            status = null;
            BrownFurDirectCastContract contract;
            if (!TryResolve(out contract, out reason)) return false;
            object handle = null;
            try
            {
                handle = contract.Begin.Invoke(null,
                    new object[] { ability, target });
                if (handle == null)
                {
                    reason = "provider-direct-begin-returned-null";
                    return false;
                }
                lease = new BrownFurDirectCastLease(contract, handle);
                status = lease.Inspect();
                return true;
            }
            catch (Exception exception)
            {
                if (handle is IDisposable)
                {
                    try { ((IDisposable)handle).Dispose(); }
                    catch (Exception) { }
                }
                lease = null;
                reason = InvocationFailure("begin", exception);
                return false;
            }
        }

        private static bool TryResolve(out BrownFurDirectCastContract contract,
            out string reason)
        {
            contract = null;
            reason = string.Empty;
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(value => value.GetName().Name ==
                        ProviderAssemblyName).ToArray();
                if (assemblies.Length != 1)
                    return Fail("provider-direct-assembly-count-" +
                        assemblies.Length, out reason);
                Assembly assembly = assemblies[0];
                Type api = assembly.GetType(ApiTypeName, false);
                Type handle = assembly.GetType(HandleTypeName, false);
                Type status = assembly.GetType(StatusTypeName, false);
                if (api == null || !api.IsPublic || !api.IsAbstract ||
                    !api.IsSealed)
                    return Fail("provider-direct-api-type-mismatch",
                        out reason);
                if (handle == null || !handle.IsPublic || !handle.IsSealed ||
                    !typeof(IDisposable).IsAssignableFrom(handle))
                    return Fail("provider-direct-handle-type-mismatch",
                        out reason);
                if (status == null || !status.IsPublic || !status.IsSealed)
                    return Fail("provider-direct-status-type-mismatch",
                        out reason);
                FieldInfo version = api.GetField("ContractVersion",
                    BindingFlags.Public | BindingFlags.Static |
                    BindingFlags.DeclaredOnly);
                if (version == null || !version.IsLiteral ||
                    version.FieldType != typeof(int) ||
                    (int)version.GetRawConstantValue() != ContractVersion)
                    return Fail("provider-direct-contract-version-mismatch",
                        out reason);

                MethodInfo validate;
                MethodInfo begin;
                MethodInfo inspect;
                MethodInfo completeRule;
                MethodInfo cleanup;
                if (!TryMethod(api, "Validate", true, status,
                        new[] { typeof(AbilityData), typeof(TargetWrapper) },
                        out validate, out reason) ||
                    !TryMethod(api, "Begin", true, handle,
                        new[] { typeof(AbilityData), typeof(TargetWrapper) },
                        out begin, out reason) ||
                    !TryMethod(handle, "Inspect", false, status, Type.EmptyTypes,
                        out inspect, out reason) ||
                    !TryMethod(handle, "CompleteRule", false, status,
                        new[] { typeof(RuleCastSpell) }, out completeRule,
                        out reason) ||
                    !TryMethod(handle, "Cleanup", false, status,
                        Type.EmptyTypes, out cleanup, out reason)) return false;

                var properties = new Dictionary<string, PropertyInfo>(
                    StringComparer.Ordinal);
                foreach (KeyValuePair<string, Type> expected in
                    ExpectedStatusProperties())
                {
                    PropertyInfo property = status.GetProperty(expected.Key,
                        BindingFlags.Public | BindingFlags.Instance |
                        BindingFlags.DeclaredOnly);
                    if (property == null || property.PropertyType !=
                            expected.Value || property.GetIndexParameters()
                            .Length != 0 || property.GetGetMethod(false) == null ||
                        property.GetSetMethod(false) != null)
                        return Fail("provider-direct-status-property-mismatch:" +
                            expected.Key, out reason);
                    properties.Add(expected.Key, property);
                }
                contract = new BrownFurDirectCastContract(validate, begin,
                    inspect, completeRule, cleanup, properties);
                return true;
            }
            catch (Exception exception)
            {
                return Fail("provider-direct-contract-probe-exception:" +
                    exception.GetType().Name, out reason);
            }
        }

        private static bool TryMethod(Type type, string name, bool isStatic,
            Type returnType, Type[] parameters, out MethodInfo method,
            out string reason)
        {
            method = null;
            reason = string.Empty;
            MethodInfo[] matches = type.GetMethods(BindingFlags.Public |
                    (isStatic ? BindingFlags.Static : BindingFlags.Instance) |
                    BindingFlags.DeclaredOnly)
                .Where(value => value.Name == name &&
                    value.ReturnType == returnType &&
                    value.GetParameters().Select(parameter =>
                        parameter.ParameterType).SequenceEqual(parameters))
                .ToArray();
            if (matches.Length != 1)
                return Fail("provider-direct-method-mismatch:" +
                    type.FullName + "::" + name, out reason);
            method = matches[0];
            return true;
        }

        private static IEnumerable<KeyValuePair<string, Type>>
            ExpectedStatusProperties()
        {
            yield return new KeyValuePair<string, Type>("Accepted",
                typeof(bool));
            yield return new KeyValuePair<string, Type>("Committed",
                typeof(bool));
            yield return new KeyValuePair<string, Type>("Complete",
                typeof(bool));
            yield return new KeyValuePair<string, Type>("ResidualState",
                typeof(bool));
            yield return new KeyValuePair<string, Type>("State",
                typeof(string));
            yield return new KeyValuePair<string, Type>("Failure",
                typeof(string));
            yield return new KeyValuePair<string, Type>("Detail",
                typeof(string));
            yield return new KeyValuePair<string, Type>("TransactionIdentity",
                typeof(string));
            yield return new KeyValuePair<string, Type>("ReservoirCost",
                typeof(int));
        }

        private static string InvocationFailure(string operation,
            Exception exception)
        {
            TargetInvocationException invocation = exception as
                TargetInvocationException;
            Exception actual = invocation != null &&
                invocation.InnerException != null ? invocation.InnerException :
                    exception;
            return "provider-direct-" + operation + "-exception:" +
                (actual == null ? "unknown" : actual.GetType().FullName);
        }

        private static bool Fail(string value, out string reason)
        {
            reason = value;
            return false;
        }
    }
}
