using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Mechanics.Actions;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Domain.Effects;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerActionGraphAdapter
    {
        private static readonly Dictionary<Type, MemberInfo[]> ActionListMembers =
            new Dictionary<Type, MemberInfo[]>();
        private readonly HashSet<string> _activeAbilities = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<BlueprintAbility> _activeAbilityObjects =
            new HashSet<BlueprintAbility>(ReferenceEqualityComparer<BlueprintAbility>.Instance);

        internal DiscoveryNode Adapt(BlueprintAbility ability)
        {
            if (ability == null) throw new ArgumentNullException("ability");
            return AdaptAbility(ability);
        }

        private DiscoveryNode AdaptAbility(BlueprintAbility ability)
        {
            string id = ability.AssetGuid;
            if (_activeAbilities.Contains(id) || _activeAbilityObjects.Contains(ability))
                return new DiscoveryNode(DiscoveryNodeKind.Unknown, id, sourceContract: "ability-cycle");
            _activeAbilities.Add(id);
            _activeAbilityObjects.Add(ability);
            try
            {
                var children = new List<DiscoveryNode>();
                AbilityEffectStickyTouch sticky = ability.GetComponent<AbilityEffectStickyTouch>();
                if (sticky != null && sticky.TouchDeliveryAbility != null)
                    children.Add(Reference(sticky.TouchDeliveryAbility, "AbilityEffectStickyTouch"));
                AbilityEffectRunAction run = ability.GetComponent<AbilityEffectRunAction>();
                if (run != null) children.Add(AdaptList(run.Actions));
                BlueprintAbility[] variants = ability.Variants;
                if (variants != null)
                {
                    foreach (BlueprintAbility variant in variants
                        .Where(v => v != null)
                        .OrderBy(v => v.AssetGuid, StringComparer.Ordinal))
                        children.Add(Reference(variant, "AbilityVariants"));
                }
                return new DiscoveryNode(DiscoveryNodeKind.AbilityReference, id, children,
                    referencedAbilityId: id, sourceContract: "BlueprintAbility");
            }
            finally
            {
                _activeAbilities.Remove(id);
                _activeAbilityObjects.Remove(ability);
            }
        }

        private DiscoveryNode Reference(BlueprintAbility ability, string contract)
        {
            return new DiscoveryNode(DiscoveryNodeKind.AbilityReference, ability.AssetGuid,
                new[] { AdaptAbility(ability) }, referencedAbilityId: ability.AssetGuid,
                sourceContract: contract);
        }

        private DiscoveryNode AdaptList(ActionList list)
        {
            GameAction[] actions = list == null || list.Actions == null ? new GameAction[0] : list.Actions;
            return new DiscoveryNode(DiscoveryNodeKind.Sequence, "ActionList",
                actions.Select(AdaptAction).ToArray(), sourceContract: "ActionList");
        }

        private DiscoveryNode AdaptAction(GameAction action)
        {
            if (action == null) return new DiscoveryNode(DiscoveryNodeKind.Empty, "null");
            var apply = action as ContextActionApplyBuff;
            if (apply != null && apply.Buff != null)
                return Effect(EffectKind.Buff, apply.Buff.AssetGuid,
                    apply.ToCaster ? EffectTarget.Caster : EffectTarget.CurrentTarget,
                    "ContextActionApplyBuff");
            var conditional = action as Conditional;
            if (conditional != null)
                return new DiscoveryNode(DiscoveryNodeKind.Conditional, action.GetType().FullName,
                    whenTrue: AdaptList(conditional.IfTrue), whenFalse: AdaptList(conditional.IfFalse),
                    conditionContract: DescribeConditions(conditional), sourceContract: "Conditional");
            var cast = action as ContextActionCastSpell;
            if (cast != null && cast.Spell != null) return Reference(cast.Spell, "ContextActionCastSpell");
            var pet = action as ContextActionsOnPet;
            if (pet != null) return Target(EffectTarget.Pet, AdaptList(pet.Actions), "ContextActionsOnPet");
            var party = action as ContextActionPartyMembers;
            if (party != null) return Target(EffectTarget.Party, AdaptList(party.Action), "ContextActionPartyMembers");
            var enchant = action as ContextActionEnchantWornItem;
            if (enchant != null && enchant.Enchantment != null)
                return Effect(EffectKind.WornItemEnchantment, enchant.Enchantment.AssetGuid,
                    enchant.ToCaster ? EffectTarget.Caster : EffectTarget.CurrentTarget,
                    "ContextActionEnchantWornItem");
            var area = action as ContextActionSpawnAreaEffect;
            if (area != null && area.AreaEffect != null)
            {
                AbilityAreaEffectBuff areaBuff = area.AreaEffect.GetComponent<AbilityAreaEffectBuff>();
                if (areaBuff != null && areaBuff.Buff != null)
                    return Effect(EffectKind.AreaBuff, areaBuff.Buff.AssetGuid,
                        EffectTarget.AreaRecipients, "ContextActionSpawnAreaEffect+AbilityAreaEffectBuff");
            }
            DiscoveryNode reflected = AdaptProvenActionLists(action);
            return reflected ?? new DiscoveryNode(DiscoveryNodeKind.Unknown,
                DescribeType(action.GetType()), sourceContract: "unsupported-action");
        }

        private DiscoveryNode AdaptProvenActionLists(GameAction action)
        {
            var lists = new List<DiscoveryNode>();
            foreach (MemberInfo member in GetActionListMembers(action.GetType()))
            {
                try
                {
                    var field = member as FieldInfo;
                    ActionList value = field != null
                        ? (ActionList)field.GetValue(action)
                        : (ActionList)((PropertyInfo)member).GetValue(action, null);
                    lists.Add(AdaptList(value));
                }
                catch (Exception exception)
                {
                    lists.Add(new DiscoveryNode(DiscoveryNodeKind.Unknown,
                        DescribeType(action.GetType()) + "." + member.Name,
                        sourceContract: "ActionList-read-failed:" + exception.GetType().FullName));
                }
            }
            return lists.Count == 0 ? null : new DiscoveryNode(
                DiscoveryNodeKind.Sequence, DescribeType(action.GetType()), lists,
                sourceContract: "reflected-exact-ActionList-wrapper");
        }

        private static MemberInfo[] GetActionListMembers(Type type)
        {
            MemberInfo[] members;
            if (ActionListMembers.TryGetValue(type, out members)) return members;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            members = type.GetFields(flags)
                .Where(f => f.FieldType == typeof(ActionList)).Cast<MemberInfo>()
                .Concat(type.GetProperties(flags)
                    .Where(p => p.PropertyType == typeof(ActionList) && p.CanRead &&
                        p.GetIndexParameters().Length == 0).Cast<MemberInfo>())
                .OrderBy(m => m.Name, StringComparer.Ordinal)
                .ThenBy(m => m.MemberType)
                .ToArray();
            ActionListMembers.Add(type, members);
            return members;
        }

        private static DiscoveryNode Effect(EffectKind kind, string id, EffectTarget target, string contract)
        {
            return new DiscoveryNode(DiscoveryNodeKind.Effect, contract, effectKind: kind,
                effectId: id, target: target, sourceContract: contract);
        }

        private static DiscoveryNode Target(EffectTarget target, DiscoveryNode child, string contract)
        {
            return new DiscoveryNode(DiscoveryNodeKind.TargetTransform, contract,
                new[] { child }, target: target, sourceContract: contract);
        }

        private static string DescribeConditions(Conditional conditional)
        {
            if (conditional.ConditionsChecker == null || conditional.ConditionsChecker.Conditions == null)
                return string.Empty;
            return conditional.ConditionsChecker.Operation + ":" + string.Join(",",
                conditional.ConditionsChecker.Conditions.Select(c => c == null ? "null" : c.GetType().FullName).ToArray());
        }

        private static string DescribeType(Type type)
        {
            return type.FullName + ", " + type.Assembly.GetName().Name +
                ", Version=" + type.Assembly.GetName().Version;
        }
    }
}
