using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using KingmakerBuffPlanner.Discovery;

namespace KingmakerBuffPlanner.GameAdapters
{
    /// <summary>
    /// Finds one unambiguous, structurally allied AbilityTargetsAround contract
    /// reachable from an ability. This is geometry metadata only; the action
    /// graph remains the authority for persistent-effect branch semantics.
    /// </summary>
    internal sealed class KingmakerAreaCoverageResolver
    {
        private static readonly Dictionary<Type, MemberInfo[]> ActionListMembers =
            new Dictionary<Type, MemberInfo[]>();

        internal AlliedAreaCoverage Resolve(BlueprintAbility root)
        {
            if (root == null) return null;
            var candidates = new List<AlliedAreaCoverage>();
            CollectAbility(root, new HashSet<BlueprintAbility>(
                ReferenceEqualityComparer<BlueprintAbility>.Instance), candidates);
            AlliedAreaCoverage[] distinct = candidates
                .GroupBy(value => value.Radius.ToString("R"), StringComparer.Ordinal)
                .Select(group => group.First()).ToArray();
            return distinct.Length == 1 ? distinct[0] : null;
        }

        private static void CollectAbility(
            BlueprintAbility ability,
            HashSet<BlueprintAbility> active,
            List<AlliedAreaCoverage> candidates)
        {
            if (ability == null || !active.Add(ability)) return;
            try
            {
                AbilityTargetsAround around = ability.GetComponent<AbilityTargetsAround>();
                if (around != null && AreaRecipientSemantics.IsAllied(
                    AreaSelection(around.TargetType), ability.CanTargetFriends,
                    ability.CanTargetEnemies, ability.CanTargetPoint))
                {
                    candidates.Add(new AlliedAreaCoverage(around.AoERadius.Meters,
                        ability.AssetGuid));
                }

                AbilityEffectStickyTouch sticky = ability.GetComponent<AbilityEffectStickyTouch>();
                if (sticky != null) CollectAbility(sticky.TouchDeliveryAbility, active, candidates);
                AbilityEffectRunAction run = ability.GetComponent<AbilityEffectRunAction>();
                if (run != null) CollectList(run.Actions, active, candidates);
            }
            finally { active.Remove(ability); }
        }

        private static void CollectList(
            ActionList list,
            HashSet<BlueprintAbility> active,
            List<AlliedAreaCoverage> candidates)
        {
            GameAction[] actions = list == null || list.Actions == null
                ? new GameAction[0] : list.Actions;
            foreach (GameAction action in actions)
                CollectAction(action, active, candidates);
        }

        private static void CollectAction(
            GameAction action,
            HashSet<BlueprintAbility> active,
            List<AlliedAreaCoverage> candidates)
        {
            if (action == null) return;
            var cast = action as ContextActionCastSpell;
            if (cast != null) { CollectAbility(cast.Spell, active, candidates); return; }
            var conditional = action as Conditional;
            if (conditional != null)
            {
                CollectList(conditional.IfTrue, active, candidates);
                CollectList(conditional.IfFalse, active, candidates);
                return;
            }
            var party = action as ContextActionPartyMembers;
            if (party != null) { CollectList(party.Action, active, candidates); return; }
            var pet = action as ContextActionsOnPet;
            if (pet != null) { CollectList(pet.Actions, active, candidates); return; }
            foreach (MemberInfo member in GetActionListMembers(action.GetType()))
            {
                try
                {
                    var field = member as FieldInfo;
                    ActionList nested = field != null
                        ? (ActionList)field.GetValue(action)
                        : (ActionList)((PropertyInfo)member).GetValue(action, null);
                    CollectList(nested, active, candidates);
                }
                catch (Exception)
                {
                    // An optional wrapper that cannot be read is ambiguous; it
                    // contributes no coverage rather than widening recipients.
                }
            }
        }

        private static MemberInfo[] GetActionListMembers(Type type)
        {
            MemberInfo[] members;
            if (ActionListMembers.TryGetValue(type, out members)) return members;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic;
            members = type.GetFields(flags).Where(field => field.FieldType == typeof(ActionList))
                .Cast<MemberInfo>().Concat(type.GetProperties(flags).Where(property =>
                    property.PropertyType == typeof(ActionList) && property.CanRead &&
                    property.GetIndexParameters().Length == 0).Cast<MemberInfo>())
                .OrderBy(member => member.Name, StringComparer.Ordinal).ToArray();
            ActionListMembers.Add(type, members);
            return members;
        }

        private static AreaSelectionTarget AreaSelection(TargetType target)
        {
            if (target == TargetType.Ally) return AreaSelectionTarget.Ally;
            if (target == TargetType.Enemy) return AreaSelectionTarget.Enemy;
            return target == TargetType.Any ? AreaSelectionTarget.Any : AreaSelectionTarget.Unknown;
        }
    }

    internal sealed class AlliedAreaCoverage
    {
        internal AlliedAreaCoverage(float radius, string sourceAbilityGuid)
        {
            Radius = radius;
            SourceAbilityGuid = sourceAbilityGuid ?? string.Empty;
        }

        internal float Radius { get; private set; }
        internal string SourceAbilityGuid { get; private set; }
    }
}
