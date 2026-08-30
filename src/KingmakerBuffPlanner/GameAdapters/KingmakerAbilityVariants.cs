using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Domain.Identity;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerAbilitySelection
    {
        internal KingmakerAbilitySelection(
            AbilityData source,
            AbilityData concrete,
            BlueprintAbility sourceBlueprint,
            int variantOrder,
            bool concreteVariant)
        {
            Source = source ?? throw new ArgumentNullException("source");
            Concrete = concrete ?? throw new ArgumentNullException("concrete");
            SourceBlueprint = sourceBlueprint ?? throw new ArgumentNullException("sourceBlueprint");
            if (variantOrder < 0) throw new ArgumentOutOfRangeException("variantOrder");
            VariantOrder = variantOrder;
            IsConcreteVariant = concreteVariant;
        }

        internal AbilityData Source { get; private set; }
        internal AbilityData Concrete { get; private set; }
        internal BlueprintAbility SourceBlueprint { get; private set; }
        internal int VariantOrder { get; private set; }
        internal bool IsConcreteVariant { get; private set; }

        internal string DisplayName
        {
            get
            {
                return AbilityDisplayNameFormatter.Format(
                    SourceBlueprint.Name,
                    Concrete.Name,
                    IsConcreteVariant);
            }
        }

        internal string SourceDisplayName
        {
            get { return SourceBlueprint.Name ?? string.Empty; }
        }
    }

    internal sealed class KingmakerVariantEligibilityTrace
    {
        internal KingmakerVariantEligibilityTrace(
            string sourceGuid, string childGuid, bool eligible, string reason)
        {
            SourceGuid = sourceGuid ?? string.Empty;
            ChildGuid = childGuid ?? string.Empty;
            Eligible = eligible;
            Reason = reason ?? string.Empty;
        }

        internal string SourceGuid { get; private set; }
        internal string ChildGuid { get; private set; }
        internal bool Eligible { get; private set; }
        internal string Reason { get; private set; }
    }

    internal static class KingmakerAbilityVariants
    {
        internal static IEnumerable<KingmakerAbilitySelection> Expand(
            IEnumerable<AbilityData> source,
            Action<KingmakerVariantEligibilityTrace> diagnostic = null)
        {
            foreach (AbilityData data in source ?? new AbilityData[0])
            {
                if (data == null || data.Blueprint == null) continue;
                BlueprintAbility blueprint = data.Blueprint;
                BlueprintAbility[] declared = blueprint.Variants ?? new BlueprintAbility[0];
                if (declared.Length != 0)
                {
                    var sourceDescriptor = Describe(blueprint, false);
                    var concreteByGuid = new Dictionary<string, AbilityData>(
                        StringComparer.Ordinal);
                    var descriptions = new List<SelectableAbilityBlueprint>();
                    foreach (BlueprintAbility child in declared.Where(value => value != null))
                    {
                        AbilityData concrete;
                        string reason;
                        bool eligible = CanSelectConcreteChild(
                            data, child, out concrete, out reason);
                        if (diagnostic != null)
                            diagnostic(new KingmakerVariantEligibilityTrace(
                                blueprint.AssetGuid, child.AssetGuid, eligible, reason));
                        descriptions.Add(Describe(child, eligible));
                        if (eligible) concreteByGuid[child.AssetGuid] = concrete;
                    }
                    IReadOnlyList<SelectableAbilityEntry> entries =
                        SelectableAbilityVariantCatalog.Expand(
                            sourceDescriptor, descriptions);
                    var byGuid = declared.Where(value => value != null)
                        .GroupBy(value => value.AssetGuid, StringComparer.Ordinal)
                        .ToDictionary(group => group.Key, group => group.First(),
                            StringComparer.Ordinal);
                    foreach (SelectableAbilityEntry entry in entries)
                    {
                        BlueprintAbility child;
                        if (!byGuid.TryGetValue(entry.Concrete.BlueprintGuid, out child)) continue;
                        AbilityData concrete;
                        if (!concreteByGuid.TryGetValue(
                            entry.Concrete.BlueprintGuid, out concrete)) continue;
                        yield return new KingmakerAbilitySelection(
                            data, concrete, blueprint,
                            entry.VariantOrder, true);
                    }
                    continue;
                }

                BlueprintAbility parent = blueprint.Parent;
                if (parent != null)
                {
                    BlueprintAbility[] siblings = parent.Variants ?? new BlueprintAbility[0];
                    int order = Array.FindIndex(siblings,
                        value => value != null && value.AssetGuid == blueprint.AssetGuid);
                    yield return new KingmakerAbilitySelection(
                        data, data, parent, Math.Max(0, order), true);
                    if (diagnostic != null)
                        diagnostic(new KingmakerVariantEligibilityTrace(
                            parent.AssetGuid, blueprint.AssetGuid, true,
                            "directly-owned-concrete-source"));
                    continue;
                }

                yield return new KingmakerAbilitySelection(data, data, blueprint, 0, false);
            }
        }

        private static bool CanSelectConcreteChild(
            AbilityData source, BlueprintAbility child,
            out AbilityData concrete, out string reason)
        {
            concrete = null;
            try
            {
                concrete = new AbilityData(source, child);
                if (!concrete.IsVisible())
                {
                    concrete = null;
                    reason = "variant-not-granted";
                    return false;
                }
                reason = "native-selectable-child";
                return true;
            }
            catch (MissingMethodException)
            {
                reason = "variant-contract-unavailable";
                concrete = null;
                return false;
            }
            catch (TypeLoadException)
            {
                reason = "variant-contract-unavailable";
                concrete = null;
                return false;
            }
            catch (Exception exception)
            {
                reason = "variant-native-validation-failed:" +
                    exception.GetType().Name;
                concrete = null;
                return false;
            }
        }

        internal static AbilityData Resolve(AbilityData source, AbilityKey requested)
        {
            if (source == null || requested == null) return null;
            KingmakerAbilitySelection match = Expand(new[] { source })
                .FirstOrDefault(value => Matches(value, requested));
            return match == null ? null : match.Concrete;
        }

        internal static AbilityKey ToAbilityKey(
            KingmakerAbilitySelection selection, SourceKind sourceKind)
        {
            if (selection == null) throw new ArgumentNullException("selection");
            int metamagic = selection.Concrete.MetamagicData == null
                ? 0 : (int)selection.Concrete.MetamagicData.MetamagicMask;
            return new AbilityKey(
                selection.SourceBlueprint.AssetGuid,
                selection.IsConcreteVariant ? selection.Concrete.Blueprint.AssetGuid : string.Empty,
                metamagic,
                sourceKind,
                string.Empty);
        }

        private static bool Matches(
            KingmakerAbilitySelection selection, AbilityKey key)
        {
            if (selection == null || key == null) return false;
            int metamagic = selection.Concrete.MetamagicData == null
                ? 0 : (int)selection.Concrete.MetamagicData.MetamagicMask;
            return string.Equals(selection.SourceBlueprint.AssetGuid,
                    key.BaseAbilityGuid, StringComparison.Ordinal) &&
                string.Equals(selection.IsConcreteVariant
                        ? selection.Concrete.Blueprint.AssetGuid
                        : string.Empty,
                    key.VariantGuid, StringComparison.Ordinal) &&
                metamagic == key.MetamagicMask;
        }

        private static SelectableAbilityBlueprint Describe(
            BlueprintAbility blueprint, bool eligible)
        {
            return new SelectableAbilityBlueprint(
                blueprint.AssetGuid,
                blueprint.Name,
                blueprint.Icon == null ? string.Empty : blueprint.AssetGuid,
                eligible);
        }
    }
}
