using System;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal static class KingmakerDurationAdapter
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly FieldInfo BaseType = typeof(ContextRankConfig).GetField("m_BaseValueType", PrivateInstance);
        private static readonly MethodInfo Progression = RankMethod("ApplyProgression");
        private static readonly MethodInfo MinMax = RankMethod("ApplyMinMax");

        private static MethodInfo RankMethod(string name)
        {
            return typeof(ContextRankConfig).GetMethod(name, PrivateInstance, null, new[] { typeof(int) }, null);
        }

        internal static DurationEstimate Estimate(BlueprintAbility ability, int casterLevel)
        {
            if (ReferenceEquals(ability, null)) return default(DurationEstimate);
            try
            {
                var applications = ability.GetComponents<AbilityApplyEffect>().ToArray();
                if (applications.Length != 1 || applications[0].GetType() != typeof(AbilityEffectRunAction) ||
                    ability.GetComponents<AbilityExecuteActionOnCast>().Any(x => x.Actions != null &&
                        x.Actions.Actions != null && x.Actions.Actions.Length != 0)) return default(DurationEstimate);
                int budget = 128;
                return ReadList(((AbilityEffectRunAction)applications[0]).Actions,
                    ability.GetComponents<ContextRankConfig>().ToArray(), casterLevel, 0, ref budget);
            }
            catch (Exception) { return default(DurationEstimate); }
        }

        private static DurationEstimate ReadList(ActionList list, ContextRankConfig[] ranks, int level, int depth, ref int budget)
        {
            if (depth > 16 || list == null || list.Actions == null || list.Actions.Length == 0)
                return default(DurationEstimate);
            var result = default(DurationEstimate);
            for (int i = 0; i < list.Actions.Length; i++)
            {
                if (--budget < 0) return default(DurationEstimate);
                var next = ReadAction(list.Actions[i], ranks, level, depth, ref budget);
                if (next.Kind == DurationKind.Unknown || (i > 0 && !result.Equals(next))) return default(DurationEstimate);
                result = next;
            }
            return result;
        }

        private static DurationEstimate ReadAction(GameAction action, ContextRankConfig[] ranks, int level, int depth, ref int budget)
        {
            if (ReferenceEquals(action, null)) return default(DurationEstimate);
            var type = action.GetType();
            if (type == typeof(ContextActionApplyBuff))
            {
                var apply = (ContextActionApplyBuff)action;
                if (ReferenceEquals(apply.Buff, null) || apply.AsChild) return default(DurationEstimate);
                if (apply.Permanent) return DurationEstimate.Permanent;
                if (apply.UseDurationSeconds)
                {
                    double rounds = apply.DurationSeconds / 6.0;
                    return rounds > 0 && rounds <= int.MaxValue && rounds == Math.Truncate(rounds)
                        ? new DurationEstimate(DurationKind.Fixed, (int)rounds, false) : default(DurationEstimate);
                }
                return ReadValue(apply.DurationValue, ranks, level);
            }
            if (type == typeof(ContextActionEnchantWornItem))
            {
                var enchant = (ContextActionEnchantWornItem)action;
                if (ReferenceEquals(enchant.Enchantment, null)) return default(DurationEstimate);
                return enchant.Permanent ? DurationEstimate.Permanent : ReadValue(enchant.DurationValue, ranks, level);
            }
            if (type == typeof(Conditional))
            {
                var branches = (Conditional)action;
                var yes = ReadList(branches.IfTrue, ranks, level, depth + 1, ref budget);
                if (yes.Kind == DurationKind.Unknown) return yes;
                var no = ReadList(branches.IfFalse, ranks, level, depth + 1, ref budget);
                return yes.Equals(no) ? yes : default(DurationEstimate);
            }
            if (type == typeof(ContextActionsOnPet))
                return ReadList(((ContextActionsOnPet)action).Actions, ranks, level, depth + 1, ref budget);
            if (type == typeof(ContextActionPartyMembers))
                return ReadList(((ContextActionPartyMembers)action).Action, ranks, level, depth + 1, ref budget);
            return default(DurationEstimate);
        }

        private static DurationEstimate ReadValue(ContextDurationValue value, ContextRankConfig[] ranks, int level)
        {
            // Native Calculate rolls dice through the rule system. Only inspect deterministic operands.
            if (value == null || value.BonusValue == null || value.DiceCountValue == null ||
                (value.DiceType != DiceType.Zero && value.DiceType != DiceType.One)) return default(DurationEstimate);
            bool scaled = false;
            int bonus, dice = 0;
            if (!ReadInteger(value.BonusValue, ranks, level, ref scaled, out bonus)) return default(DurationEstimate);
            if (value.DiceType == DiceType.One && (!ReadInteger(value.DiceCountValue, ranks, level, ref scaled, out dice) || dice < 0))
                return default(DurationEstimate);
            long rounds = ((long)bonus + dice) * value.Rate.ToRounds().Value;
            if (rounds < 1 || rounds > int.MaxValue) return default(DurationEstimate);
            return new DurationEstimate(scaled ? DurationKind.PerCasterLevel : DurationKind.Fixed, (int)rounds, value.IsExtendable);
        }

        private static bool ReadInteger(ContextValue value, ContextRankConfig[] ranks, int level, ref bool scaled, out int result)
        {
            result = value.Value;
            if (value.ValueType == ContextValueType.Simple) return true;
            if (value.ValueType != ContextValueType.Rank || level <= 0 || !Enum.IsDefined(typeof(AbilityRankType), value.ValueRank))
                return false;
            var matching = ranks.Where(config => config.Type == value.ValueRank).ToArray();
            // AbilityExecutionContext starts every rank at the effective caster level.
            result = level;
            if (matching.Length != 0)
            {
                if (matching.Length != 1 || matching[0].GetType() != typeof(ContextRankConfig) ||
                    BaseType == null || Progression == null || MinMax == null ||
                    (ContextRankBaseValueType)BaseType.GetValue(matching[0]) != ContextRankBaseValueType.CasterLevel)
                    return false;
                object progressed = Progression.Invoke(matching[0], new object[] { level });
                result = (int)MinMax.Invoke(matching[0], new[] { progressed });
            }
            scaled = true;
            return true;
        }
    }
}
