using System;
using System.Runtime.CompilerServices;
using Kingmaker.Items;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    // Native quantities and costs, never inferred from the number of casts.
    // Used by discovery AND binding. Unsupported contracts leave saved intent
    // unresolved; they must not acquire an unlimited provider.
    internal sealed class KingmakerFactResource
    {
        internal SourceKind Kind;
        internal string InstanceId;
        internal ResourcePoolSnapshot Pool;
        internal int Cost;
    }

    internal static class KingmakerFactResources
    {
        // ItemEntity has no persistent ID in 2.1.7b. Never approximate one
        // from a blueprint, inventory position or equipment slot. This token
        // survives rescans of the SAME object; after reload the authored
        // selection stays saved but unresolved until explicitly reselected.
        private static readonly ConditionalWeakTable<ItemEntity, ItemIdentity> ItemIdentities =
            new ConditionalWeakTable<ItemEntity, ItemIdentity>();

        private sealed class ItemIdentity
        {
            internal readonly string Id = "session-item-" + Guid.NewGuid().ToString("N");
        }

        internal static bool TryRead(string unitId, AbilityData source, AbilityData concrete,
            out KingmakerFactResource result, out string refusal)
        {
            result = null;
            refusal = null;
            try
            {
                if (source == null || concrete == null || source.Blueprint == null || concrete.Blueprint == null)
                    return Refuse("source-blueprint-unread", out refusal);
                if (source.Spellbook != null || concrete.Spellbook != null)
                    return Refuse("spellbook-bound", out refusal);
                int? available = KingmakerCount(concrete);
                if (!available.HasValue || available.Value < -1)
                    return Refuse("native-availability-unread-or-unknown", out refusal);
                ItemEntity item = concrete.SourceItem;
                if (!ReferenceEquals(item, source.SourceItem))
                    return Refuse("variant-item-owner-mismatch", out refusal);
                AbilityResourceLogic logic = concrete.Blueprint.GetComponent<AbilityResourceLogic>();
                AbilityResourceLogic sourceLogic = source.Blueprint.GetComponent<AbilityResourceLogic>();
                // Converted counts may delegate to the parent, but Spend uses
                // the concrete blueprint. Do not price a parent's resource
                // unless the submitted ability actually spends that resource.
                if ((sourceLogic != null && sourceLogic.IsSpendResource) &&
                    (logic == null || !logic.IsSpendResource ||
                     !ReferenceEquals(logic.RequiredResource, sourceLogic.RequiredResource) ||
                     source.ResourceCost != concrete.ResourceCost))
                    return Refuse("variant-resource-contract-mismatch", out refusal);
                bool resource = logic != null && logic.IsSpendResource && logic.RequiredResource != null;
                if (logic != null && logic.IsSpendResource && logic.RequiredResource == null)
                    return Refuse("ability-resource-owner-unread", out refusal);
                bool charges = item != null && item.IsSpendCharges;
                if (resource && charges)
                    return Refuse("combined-item-and-ability-resource", out refusal);
                string instance = item == null ? string.Empty :
                    ItemIdentities.GetValue(item, value => new ItemIdentity()).Id;
                if (charges)
                {
                    var blueprint = item.Blueprint as BlueprintItemEquipment;
                    if (blueprint == null || !blueprint.GainAbility || item.Count != 1)
                        return Refuse("item-charge-contract-unsupported", out refusal);
                    if (item.Charges < 0 || available.Value != item.Charges)
                        return Refuse("item-charge-count-mismatch", out refusal);
                    result = new KingmakerFactResource
                    {
                        Kind = SourceKind.Item, InstanceId = instance, Cost = 1,
                        Pool = new ResourcePoolSnapshot("item|" + instance, ResourcePoolKind.ItemCharges,
                            Math.Max(item.Charges, blueprint.Charges), item.Charges, null)
                    };
                    return true;
                }
                if (resource)
                {
                    int cost = concrete.ResourceCost;
                    int remaining = concrete.Caster.Resources.GetResourceAmount(logic.RequiredResource);
                    if (cost <= 0 || remaining < 0 || available.Value != remaining / cost)
                        return Refuse("ability-resource-cost-or-count-mismatch", out refusal);
                    result = new KingmakerFactResource
                    {
                        Kind = item == null ? SourceKind.AbilityResource : SourceKind.Item,
                        InstanceId = instance, Cost = cost,
                        Pool = new ResourcePoolSnapshot(unitId + "|resource|" + logic.RequiredResource.AssetGuid,
                            ResourcePoolKind.AbilityResource,
                            Math.Max(remaining, logic.RequiredResource.GetMaxAmount(concrete.Caster)), remaining, null)
                    };
                    return true;
                }
                if (available.Value != -1)
                    return Refuse("finite-native-contract-unsupported:" + available.Value, out refusal);
                result = new KingmakerFactResource
                {
                    Kind = item == null ? SourceKind.Fact : SourceKind.Item,
                    InstanceId = instance, Cost = 0,
                    Pool = new ResourcePoolSnapshot(item == null
                        ? unitId + "|free|" + (source.Blueprint.Parent ?? source.Blueprint).AssetGuid : "item|" + instance,
                        ResourcePoolKind.Unlimited, 0, 0, null)
                };
                return true;
            }
            catch (Exception exception)
            {
                return Refuse("native-contract-unread:" + exception.GetType().Name, out refusal);
            }
        }

        internal static int? KingmakerCount(AbilityData ability)
        {
            try { return ability == null ? (int?)null : ability.GetAvailableForCastCount(); }
            catch (Exception) { return null; }
        }

        internal static string Describe(AbilityData ability)
        {
            try
            {
                ItemEntity item = ability.SourceItem;
                return "ability-resource:" + (ability.Resource == null ? "none" : ability.Resource.AssetGuid) +
                    ";ability-resource-cost:" + ability.ResourceCost +
                    ";source-spellbook:" + (ability.Spellbook == null ? "none" : ability.Spellbook.Blueprint.AssetGuid) +
                    ";converted-from:" + (ability.ConvertedFrom == null ? "none" : ability.ConvertedFrom.Blueprint.AssetGuid) +
                    ";source-item:" + (item == null ? "none" : item.Blueprint.AssetGuid +
                        ";item-token:" + ItemIdentities.GetValue(item, value => new ItemIdentity()).Id +
                        ";item-charges:" + item.Charges + ";item-count:" + item.Count +
                        ";item-spends-charges:" + item.IsSpendCharges);
            }
            catch (Exception exception) { return "source-contract-unread:" + exception.GetType().Name; }
        }

        internal static string ValidateBinding(string unitId, AbilityData source, AbilityData concrete,
            ProviderKey provider, ResourceReservation reservation)
        {
            KingmakerFactResource resource;
            string refusal;
            if (!TryRead(unitId, source, concrete, out resource, out refusal)) return refusal;
            if (resource.Kind != provider.Ability.SourceKind || resource.InstanceId != provider.SourceInstanceId ||
                (resource.Kind == SourceKind.Item && resource.InstanceId != provider.Ability.SpecialSourceId))
                return "fact-source-identity-mismatch";
            if (reservation != null && (resource.Pool.PoolKey != reservation.PoolKey ||
                (resource.Pool.Kind == ResourcePoolKind.Unlimited) != reservation.Unlimited ||
                resource.Cost != reservation.Units))
                return "fact-source-reservation-mismatch;pool=" + resource.Pool.PoolKey +
                    ";planned-unlimited=" + reservation.Unlimited +
                    ";bound-kind=" + resource.Pool.Kind + ";planned-cost=" + reservation.Units +
                    ";bound-cost=" + resource.Cost;
            return null;
        }

        private static bool Refuse(string reason, out string refusal)
        {
            refusal = "fact-resource-unsupported:" + reason;
            return false;
        }
    }
}
