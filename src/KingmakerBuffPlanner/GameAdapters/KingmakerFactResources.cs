using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.UnitLogic;
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
        // The durable selection rule for an item source; empty otherwise.
        internal string Selector = string.Empty;
        internal ResourcePoolSnapshot Pool;
        internal int Cost;
    }

    internal static class KingmakerFactResources
    {
        // ItemEntity has no persistent ID in 2.1.7b. This token identifies one
        // live object for pools, reservations and the run's exact binding; it
        // is never persisted. What a saved plan keeps is the explicit rule in
        // KingmakerFactResource.Selector, which is not a physical identity.
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
                if (item != null && (item.Blueprint == null || string.IsNullOrWhiteSpace(item.Blueprint.AssetGuid)))
                    return Refuse("item-blueprint-unread", out refusal);
                string instance = item == null ? string.Empty :
                    ItemIdentities.GetValue(item, value => new ItemIdentity()).Id;
                string selector = item == null ? string.Empty :
                    ItemSourceSelector.ForEquippedType(item.Blueprint.AssetGuid);
                if (charges)
                {
                    var blueprint = item.Blueprint as BlueprintItemEquipment;
                    if (blueprint == null || !blueprint.GainAbility || item.Count != 1)
                        return Refuse("item-charge-contract-unsupported", out refusal);
                    if (item.Charges < 0 || available.Value != item.Charges)
                        return Refuse("item-charge-count-mismatch", out refusal);
                    result = new KingmakerFactResource
                    {
                        Kind = SourceKind.Item, InstanceId = instance, Selector = selector, Cost = 1,
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
                        InstanceId = instance, Selector = selector, Cost = cost,
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
                    InstanceId = instance, Selector = selector, Cost = 0,
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
            ProviderKey provider, ResourceReservation reservation, string requiredItemSelector = null)
        {
            KingmakerFactResource resource;
            string refusal;
            if (!TryRead(unitId, source, concrete, out resource, out refusal)) return refusal;
            // The run binds to the one live object planned; the saved rule
            // must additionally still describe that object.
            if (resource.Kind != provider.Ability.SourceKind || resource.InstanceId != provider.SourceInstanceId ||
                (resource.Kind == SourceKind.Item &&
                    !RuleDescribes(provider.Ability.SpecialSourceId, resource, concrete)))
                return "fact-source-identity-mismatch";
            // The provider may carry the plain rule; the authored slot rule
            // still has to hold for this same live item.
            if (resource.Kind == SourceKind.Item && !string.IsNullOrEmpty(requiredItemSelector) &&
                !RuleDescribes(requiredItemSelector, resource, concrete))
                return "fact-source-item-left-saved-slot:" + requiredItemSelector;
            if (reservation != null && (resource.Pool.PoolKey != reservation.PoolKey ||
                (resource.Pool.Kind == ResourcePoolKind.Unlimited) != reservation.Unlimited ||
                resource.Cost != reservation.Units))
                return "fact-source-reservation-mismatch;pool=" + resource.Pool.PoolKey +
                    ";planned-unlimited=" + reservation.Unlimited +
                    ";bound-kind=" + resource.Pool.Kind + ";planned-cost=" + reservation.Units +
                    ";bound-cost=" + resource.Cost;
            return null;
        }

        private static bool RuleDescribes(string selector, KingmakerFactResource resource, AbilityData concrete)
        {
            ItemSourceSelection selection = ItemSourceSelector.Parse(selector);
            if (!selection.IsDurable ||
                resource.Selector != ItemSourceSelector.ForEquippedType(selection.ItemBlueprintGuid))
                return false;
            return selection.Kind != ItemSelectionKind.EquippedSlot ||
                EquippedSlotId(concrete.Caster, concrete.SourceItem) == selection.SlotId;
        }

        // The named position an equipped item occupies on its wielder, or
        // null when it is not in one of these slots. A position, not an item
        // identity: only used for an explicit choice between identical copies.
        internal static string EquippedSlotId(UnitDescriptor owner, ItemEntity item)
        {
            try
            {
                ItemSlot held = item == null ? null : item.HoldingSlot;
                UnitBody body = owner == null ? null : owner.Body;
                if (held == null || body == null) return null;
                var named = new KeyValuePair<string, ItemSlot>[]
                {
                    new KeyValuePair<string, ItemSlot>("Armor", body.Armor),
                    new KeyValuePair<string, ItemSlot>("Belt", body.Belt),
                    new KeyValuePair<string, ItemSlot>("Head", body.Head),
                    new KeyValuePair<string, ItemSlot>("Feet", body.Feet),
                    new KeyValuePair<string, ItemSlot>("Gloves", body.Gloves),
                    new KeyValuePair<string, ItemSlot>("Neck", body.Neck),
                    new KeyValuePair<string, ItemSlot>("Ring1", body.Ring1),
                    new KeyValuePair<string, ItemSlot>("Ring2", body.Ring2),
                    new KeyValuePair<string, ItemSlot>("Wrist", body.Wrist),
                    new KeyValuePair<string, ItemSlot>("Shoulders", body.Shoulders)
                };
                foreach (KeyValuePair<string, ItemSlot> slot in named)
                    if (ReferenceEquals(slot.Value, held)) return slot.Key;
                int quick = 0;
                foreach (ItemSlot slot in body.QuickSlots)
                {
                    if (ReferenceEquals(slot, held)) return "Quick" + quick;
                    quick++;
                }
                for (int set = 0; set < body.HandsEquipmentSets.Count; set++)
                {
                    if (ReferenceEquals(body.HandsEquipmentSets[set].PrimaryHand, held)) return "Set" + set + "Primary";
                    if (ReferenceEquals(body.HandsEquipmentSets[set].SecondaryHand, held)) return "Set" + set + "Secondary";
                }
                return null;
            }
            catch (Exception) { return null; }
        }

        private static bool Refuse(string reason, out string refusal)
        {
            refusal = "fact-resource-unsupported:" + reason;
            return false;
        }
    }
}
