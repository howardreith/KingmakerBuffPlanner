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
        private bool _ambiguous;
        private string _selectorFilter;

        internal const string ConditionedAreaSelector = "area-selector-has-own-condition";
        internal const string UnreadAreaSelector = "area-selector-condition-unread";

        // Missing-beneficial-buffs review R2-F5: a point-capable ability is
        // planned on a unit anchor with one disk of allied recipients; its
        // own AbilityTargetsAround condition (m_Condition) would filter that
        // disk in ways the planner does not model, so a non-empty or unread
        // condition yields no coverage (the casting cannot be planned).
        // Recovery (R12-F4): the same holds for every allied area, whatever
        // CanTargetPoint says (Good Hope's selector included): a valid anchor
        // proves nothing about which recipients the selector's own filter
        // admits. (In the 2.1.7b dump no allied-area buff the planner covers
        // has a selector condition; the 32 conditioned selectors belong to
        // offensive, enemy or non-area abilities.)
        private static readonly FieldInfo SelectorCondition = typeof(AbilityTargetsAround).GetField(
            "m_Condition", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static string PointSelectorFilter(AbilityTargetsAround around)
        {
            try
            {
                var checker = SelectorCondition == null ? null : SelectorCondition.GetValue(around) as ConditionsChecker;
                if (checker == null) return UnreadAreaSelector;
                return checker.Conditions == null || checker.Conditions.Length == 0 ? null : ConditionedAreaSelector;
            }
            catch (Exception) { return UnreadAreaSelector; }
        }

        internal string LastFailureReason { get; private set; }

        internal AlliedAreaCoverage Resolve(BlueprintAbility root,
            BlueprintAbility declaredSource = null)
        {
            LastFailureReason = string.Empty;
            _ambiguous = false;
            _selectorFilter = null;
            if (root == null)
            {
                LastFailureReason = "ability-missing";
                return null;
            }
            if (declaredSource != null && !IsDeclaredSource(root,
                    declaredSource))
            {
                LastFailureReason = "selected-variant-source-mismatch";
                return null;
            }
            var candidates = new List<AlliedAreaCoverage>();
            var active = new HashSet<BlueprintAbility>(
                ReferenceEqualityComparer<BlueprintAbility>.Instance);
            CollectAbility(root, active, candidates);
            if (declaredSource != null && !ReferenceEquals(root,
                    declaredSource))
                CollectAbility(declaredSource, active, candidates);
            // Review R10-F1: the selector type is part of the contract; the
            // same radius with different selectors is contradictory.
            AlliedAreaCoverage[] distinct = candidates
                .GroupBy(value => value.Radius.ToString("R") + "|" + value.RequiresAlly, StringComparer.Ordinal)
                .Select(group => group.First()).ToArray();
            if (_selectorFilter != null)
            {
                LastFailureReason = _selectorFilter;
                return null;
            }
            if (_ambiguous)
            {
                LastFailureReason = "conflicting-or-unreadable-area-contract";
                return null;
            }
            if (distinct.Length != 1)
            {
                LastFailureReason = distinct.Length == 0
                    ? "allied-area-radius-missing"
                    : "contradictory-allied-area-radii";
                return null;
            }
            return distinct[0];
        }

        private static bool IsDeclaredSource(BlueprintAbility selected,
            BlueprintAbility source)
        {
            if (selected == null || source == null) return false;
            if (ReferenceEquals(selected, source) || selected.AssetGuid ==
                    source.AssetGuid) return true;
            if (selected.Parent != null && selected.Parent.AssetGuid ==
                    source.AssetGuid) return true;
            return (source.Variants ?? new BlueprintAbility[0]).Any(value =>
                value != null && value.AssetGuid == selected.AssetGuid);
        }

        private void CollectAbility(
            BlueprintAbility ability,
            HashSet<BlueprintAbility> active,
            List<AlliedAreaCoverage> candidates)
        {
            if (ability == null || !active.Add(ability)) return;
            try
            {
                AbilityTargetsAround around = ability.GetComponent<AbilityTargetsAround>();
                if (around != null)
                {
                    if (_selectorFilter == null)
                        _selectorFilter = PointSelectorFilter(around);
                    if (AreaRecipientSemantics.IsAllied(
                        AreaSelection(around.TargetType),
                        ability.CanTargetFriends, ability.CanTargetEnemies,
                        ability.CanTargetPoint))
                        candidates.Add(new AlliedAreaCoverage(
                            around.AoERadius.Meters, ability.AssetGuid,
                            around.TargetType == Kingmaker.UnitLogic.Abilities.Components.TargetType.Ally));
                    else _ambiguous = true;
                }

                AbilityEffectStickyTouch sticky = ability.GetComponent<AbilityEffectStickyTouch>();
                if (sticky != null) CollectAbility(sticky.TouchDeliveryAbility, active, candidates);
                AbilityEffectRunAction run = ability.GetComponent<AbilityEffectRunAction>();
                if (run != null) CollectList(run.Actions, active, candidates);
            }
            finally { active.Remove(ability); }
        }

        private void CollectList(
            ActionList list,
            HashSet<BlueprintAbility> active,
            List<AlliedAreaCoverage> candidates)
        {
            GameAction[] actions = list == null || list.Actions == null
                ? new GameAction[0] : list.Actions;
            foreach (GameAction action in actions)
                CollectAction(action, active, candidates);
        }

        private void CollectAction(
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
                    _ambiguous = true;
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
        internal AlliedAreaCoverage(float radius, string sourceAbilityGuid, bool requiresAlly = false)
        {
            Radius = radius;
            SourceAbilityGuid = sourceAbilityGuid ?? string.Empty;
            RequiresAlly = requiresAlly;
        }

        internal float Radius { get; private set; }
        // Review R10-F1: the native selector is TargetType.Ally (only units
        // allied to the caster are selected); Any selects every unit.
        internal bool RequiresAlly { get; private set; }
        internal string SourceAbilityGuid { get; private set; }
    }
}
