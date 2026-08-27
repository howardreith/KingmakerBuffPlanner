using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class PowerfulChangeBlueprintAnalysis
    {
        internal PowerfulChangeBlueprintAnalysis(BlueprintAbility ability,
            PowerfulChangeEligibility eligibility,
            IEnumerable<string> appliedBuffGuids,
            IEnumerable<string> carrierEvidence,
            IEnumerable<string> componentTypes,
            IEnumerable<string> discoveryDiagnostics)
        {
            AbilityGuid = ability == null ? string.Empty : ability.AssetGuid;
            AbilityName = ability == null ? string.Empty : ability.Name;
            InternalName = ability == null ? string.Empty : ability.name;
            School = ability == null ? "<missing>" : ability.School.ToString();
            Descriptors = ability == null ? "<missing>" :
                ability.SpellDescriptor.ToString();
            Eligibility = eligibility;
            AppliedBuffGuids = Values(appliedBuffGuids);
            CarrierEvidence = Values(carrierEvidence);
            ComponentTypes = Values(componentTypes);
            DiscoveryDiagnostics = Values(discoveryDiagnostics);
        }

        internal string AbilityGuid { get; private set; }
        internal string AbilityName { get; private set; }
        internal string InternalName { get; private set; }
        internal string School { get; private set; }
        internal string Descriptors { get; private set; }
        internal PowerfulChangeEligibility Eligibility { get; private set; }
        internal string[] AppliedBuffGuids { get; private set; }
        internal string[] CarrierEvidence { get; private set; }
        internal string[] ComponentTypes { get; private set; }
        internal string[] DiscoveryDiagnostics { get; private set; }

        private static string[] Values(IEnumerable<string> source)
        {
            return (source ?? new string[0]).Where(value =>
                    !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
        }
    }

    /// <summary>
    /// Uses Buff Planner's proven action-graph adapter to find resulting buffs,
    /// then mirrors the provider's supported positive ability-score carrier
    /// semantics. It never keys qualification by spell name or spell GUID.
    /// </summary>
    internal sealed class KingmakerPowerfulChangeBlueprintAnalyzer
    {
        internal PowerfulChangeBlueprintAnalysis Analyze(
            BlueprintAbility ability,
            bool isGenuineSpell,
            string sourceSpellbookGuid,
            string requiredSpellbookGuid)
        {
            var carrierEvidence = new List<string>();
            var componentTypes = new List<string>();
            var appliedBuffGuids = new List<string>();
            var diagnostics = new List<string>();
            if (ability != null)
            {
                DiscoveryScanResult scan = new ActionGraphScanner().Scan(
                    new KingmakerActionGraphAdapter().Adapt(ability));
                diagnostics.AddRange(scan.Diagnostics.Select(value =>
                    value.Code + ":" + value.NodeIdentity + ":" + value.Detail));
                NativeEffectRecord[] effects = NativeCatalogExporter.GetEffects(
                    scan.Expression);
                foreach (NativeEffectRecord effect in effects.Where(value =>
                    value.Kind == EffectKind.Buff.ToString() ||
                    value.Kind == EffectKind.AreaBuff.ToString()))
                {
                    BlueprintBuff buff = ResourcesLibrary.TryGetBlueprint<
                        BlueprintBuff>(effect.EffectGuid);
                    if (buff == null) continue;
                    appliedBuffGuids.Add(buff.AssetGuid);
                    foreach (BlueprintComponent component in buff.ComponentsArray ??
                        new BlueprintComponent[0])
                    {
                        if (component == null) continue;
                        componentTypes.Add(component.GetType().FullName);
                        var visitor = new CarrierVisitor(carrierEvidence,
                            componentTypes);
                        visitor.Walk(component, "buff[" + buff.AssetGuid +
                            "].components", 0);
                    }
                }
            }
            PowerfulChangeEligibility eligibility =
                PowerfulChangeEligibilityClassifier.Classify(
                    isGenuineSpell && ability != null &&
                        ability.Type == AbilityType.Spell,
                    ability != null && ability.School ==
                        Kingmaker.Blueprints.Classes.Spells.SpellSchool.Transmutation,
                    sourceSpellbookGuid, requiredSpellbookGuid,
                    carrierEvidence, appliedBuffGuids);
            return new PowerfulChangeBlueprintAnalysis(ability, eligibility,
                appliedBuffGuids, carrierEvidence, componentTypes, diagnostics);
        }

        private sealed class CarrierVisitor
        {
            private readonly IList<string> _carriers;
            private readonly IList<string> _componentTypes;
            private readonly HashSet<object> _visited = new HashSet<object>(
                ReferenceComparer.Instance);

            internal CarrierVisitor(IList<string> carriers,
                IList<string> componentTypes)
            {
                _carriers = carriers;
                _componentTypes = componentTypes;
            }

            internal void Walk(object value, string path, int depth)
            {
                if (value == null || depth > 24 || value is string) return;
                Type type = value.GetType();
                if (Scalar(type) || !_visited.Add(value)) return;
                string fullName = type.FullName ?? type.Name;
                string carrier = DescribeCarrier(value, type, path);
                if (!string.IsNullOrWhiteSpace(carrier))
                    _carriers.Add(carrier);
                if (value is BlueprintComponent)
                    _componentTypes.Add(fullName);
                if (value is BlueprintScriptableObject && depth != 0) return;
                var enumerable = value as IEnumerable;
                if (enumerable != null)
                {
                    int index = 0;
                    foreach (object item in enumerable)
                    {
                        if (index >= 512) break;
                        Walk(item, path + "[" + index + "]", depth + 1);
                        index++;
                    }
                    return;
                }
                foreach (FieldInfo field in Fields(type))
                {
                    object member;
                    try { member = field.GetValue(value); }
                    catch (Exception) { continue; }
                    if (member == null || member is BlueprintScriptableObject)
                        continue;
                    if (!Scalar(member.GetType()))
                        Walk(member, path + "." + field.Name, depth + 1);
                }
            }

            private static string DescribeCarrier(object value, Type type,
                string path)
            {
                string fullName = type.FullName ?? type.Name;
                bool polymorph = fullName ==
                    "Kingmaker.UnitLogic.Buffs.Polymorph";
                bool size = fullName ==
                    "Kingmaker.Designers.Mechanics.Buffs.ChangeUnitSize";
                bool statCarrier = Contains(fullName, "AddStatBonus") ||
                    Contains(fullName, "AddContextStatBonus") ||
                    Contains(fullName, "AddGenericStatBonus");
                if (!polymorph && !size && !statCarrier) return string.Empty;

                var details = new List<string>();
                bool abilityStat = size;
                bool positive = size;
                bool valueFieldSeen = false;
                foreach (FieldInfo field in Fields(type))
                {
                    object member;
                    try { member = field.GetValue(value); }
                    catch (Exception) { continue; }
                    if (member == null) continue;
                    string text = Convert.ToString(member,
                        CultureInfo.InvariantCulture);
                    if (IsAbilityScore(text)) abilityStat = true;
                    string name = field.Name.ToLowerInvariant();
                    if (polymorph && (name == "strengthbonus" ||
                        name == "dexteritybonus" ||
                        name == "constitutionbonus"))
                    {
                        abilityStat = true;
                        valueFieldSeen = true;
                        if (IsPositive(text)) positive = true;
                    }
                    if (statCarrier && name == "value")
                    {
                        valueFieldSeen = true;
                        if (IsPositive(text)) positive = true;
                    }
                    if (name.Contains("stat") || name.Contains("bonus") ||
                        name.Contains("value") || name.Contains("descriptor") ||
                        name.Contains("size"))
                        details.Add(field.Name + "=" + text);
                }
                if (!abilityStat || !positive || (!valueFieldSeen && !size))
                    return string.Empty;
                return path + "=" + fullName + "{" + string.Join(",",
                    details.OrderBy(item => item, StringComparer.Ordinal)
                        .ToArray()) + "}";
            }

            private static IEnumerable<FieldInfo> Fields(Type type)
            {
                for (Type cursor = type; cursor != null && cursor != typeof(object);
                    cursor = cursor.BaseType)
                    foreach (FieldInfo field in cursor.GetFields(
                        BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                        .OrderBy(value => value.Name, StringComparer.Ordinal))
                        yield return field;
            }

            private static bool Scalar(Type type)
            {
                return type.IsPrimitive || type.IsEnum || type == typeof(decimal) ||
                    type == typeof(string) || type == typeof(Type);
            }

            private static bool Contains(string value, string token)
            {
                return value.IndexOf(token,
                    StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private static bool IsAbilityScore(string value)
            {
                PowerfulChangeAbilityScore parsed;
                return Enum.TryParse(value, false, out parsed) &&
                    parsed != PowerfulChangeAbilityScore.None;
            }

            private static bool IsPositive(string value)
            {
                double parsed;
                return double.TryParse(value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out parsed) && parsed > 0d;
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceComparer Instance =
                new ReferenceComparer();
            public new bool Equals(object left, object right)
            { return ReferenceEquals(left, right); }
            public int GetHashCode(object value)
            { return RuntimeHelpers.GetHashCode(value); }
        }
    }
}
