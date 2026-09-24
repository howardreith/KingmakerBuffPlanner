using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic.Buffs;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Execution;

namespace KingmakerBuffPlanner.GameAdapters
{
    // Final review A3: the live reads behind AppliedEffectJudgement. A read
    // takes each expected recipient's instances of the step's expected
    // effects (buffs, and the worn-item enchantments some buffs apply)
    // directly from the game, with a process-local identity, the end time
    // and suppression. A failed read never confirms.
    internal static class KingmakerEffectInstanceReader
    {
        // Before submission: every expected recipient must be read, or
        // nothing is cast.
        internal static EffectBaseline ReadBaseline(CastStep step, out string failure)
        {
            failure = null;
            try
            {
                if (step == null || step.ExpectedEffects == null)
                {
                    failure = "expected-effects-missing";
                    return null;
                }
                if (step.ExpectedRecipientUnitIds.Count == 0)
                {
                    failure = "no-expected-recipients";
                    return null;
                }
                Dictionary<string, UnitEntityData> units = KingmakerAnimatedCastAdapter.CollectUnits();
                HashSet<string> ids = ExpectedIds(step.ExpectedEffects);
                var read = new Dictionary<string, IEnumerable<ObservedEffectInstance>>(
                    StringComparer.Ordinal);
                foreach (string unitId in step.ExpectedRecipientUnitIds)
                {
                    UnitEntityData unit;
                    if (!units.TryGetValue(unitId, out unit) || unit.Descriptor == null)
                    {
                        failure = "recipient-not-in-party:" + unitId;
                        return null;
                    }
                    read[unitId] = Instances(unit, ids);
                }
                return new EffectBaseline(read);
            }
            catch (Exception exception)
            {
                failure = "read-exception:" + exception.GetType().Name + ":" + exception.Message;
                return null;
            }
        }

        // After submission: every expected recipient reached by this attempt.
        internal static bool AppliedByThisAttempt(CastStep step, EffectBaseline baseline)
        {
            try
            {
                if (step == null || baseline == null || step.ExpectedEffects == null) return false;
                Dictionary<string, UnitEntityData> units = KingmakerAnimatedCastAdapter.CollectUnits();
                HashSet<string> ids = ExpectedIds(step.ExpectedEffects);
                return AppliedEffectJudgement.AllReached(step.ExpectedRecipientUnitIds,
                    step.ExpectedEffects, baseline, unitId =>
                    {
                        UnitEntityData unit;
                        return units.TryGetValue(unitId, out unit) && unit.Descriptor != null
                            ? Instances(unit, ids) : null;
                    });
            }
            catch (Exception) { return false; }
        }

        private static HashSet<string> ExpectedIds(EffectExpression expected)
        {
            return new HashSet<string>(KingmakerAnimatedCastAdapter.ExpectedEffectIds(expected)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        }

        private static List<ObservedEffectInstance> Instances(UnitEntityData unit, HashSet<string> ids)
        {
            var instances = new List<ObservedEffectInstance>();
            foreach (Buff buff in unit.Descriptor.Buffs.Enumerable)
            {
                if (buff == null || buff.Blueprint == null ||
                    !ids.Contains(buff.Blueprint.AssetGuid)) continue;
                instances.Add(new ObservedEffectInstance(
                    string.IsNullOrWhiteSpace(buff.SourceAreaEffectId)
                        ? EffectKind.Buff : EffectKind.AreaBuff,
                    buff.Blueprint.AssetGuid, Key(buff), buff.EndTime.Ticks, Suppressed(buff)));
            }
            foreach (ItemSlot slot in unit.Descriptor.Body.CurrentEquipmentSlots)
            {
                ItemEntity item = slot == null ? null : slot.MaybeItem;
                if (item == null || item.Enchantments == null) continue;
                foreach (var enchantment in item.Enchantments)
                {
                    if (enchantment == null || enchantment.Blueprint == null ||
                        !ids.Contains(enchantment.Blueprint.AssetGuid)) continue;
                    instances.Add(new ObservedEffectInstance(EffectKind.WornItemEnchantment,
                        enchantment.Blueprint.AssetGuid, Key(enchantment),
                        enchantment.EndTime.Ticks, false));
                }
            }
            return instances;
        }

        // An unreadable suppression flag counts as suppressed: such an
        // instance never confirms.
        private static bool Suppressed(Buff buff)
        {
            try { return buff.IsSuppressed; }
            catch (Exception) { return true; }
        }

        private static string Key(object instance)
        {
            return RuntimeHelpers.GetHashCode(instance).ToString(CultureInfo.InvariantCulture);
        }
    }
}
