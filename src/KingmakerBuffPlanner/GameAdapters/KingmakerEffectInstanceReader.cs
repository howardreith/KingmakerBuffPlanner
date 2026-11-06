using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
                foreach (string unitId in step.EffectObservationUnitIds)
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

        // After submission: a direct cast must reach its recipient; a group
        // cast (WP3, 0.4.0) must reach at least one predicted recipient -
        // whoever the native area actually affected - and a member outside
        // the area is not a failure.
        internal static bool AppliedByThisAttempt(CastStep step, EffectBaseline baseline)
        {
            try
            {
                if (step == null || baseline == null || step.ExpectedEffects == null) return false;
                Dictionary<string, UnitEntityData> units;
                Dictionary<string, IEnumerable<ObservedEffectInstance>> observed = ReadAfter(step, out units);
                Func<string, IEnumerable<ObservedEffectInstance>> readAfter = unitId =>
                    observed.ContainsKey(unitId) ? observed[unitId] : null;
                // The positioned observation first, so a first sighting keeps
                // the native geometry of the read that made it.
                AppliedEffectJudgement.RecordProgress(baseline, step.EffectObservationUnitIds, readAfter,
                    Geometry(step, units));
                bool confirmed = Judge(step, baseline, readAfter);
                baseline.RecordConfirmationPoll(confirmed, new EffectBaseline(observed), step.EffectObservationUnitIds);
                return confirmed;
            }
            catch (Exception) { return false; }
        }

        // H1/H2 (0.6.11 review): a post-submission read taken while an
        // animated command is still running. It confirms nothing by itself;
        // it records what this attempt was seen to apply, and where, before a
        // short-lived application can expire.
        internal static void RecordProgress(CastStep step, EffectBaseline baseline)
        {
            try
            {
                if (step == null || baseline == null || step.ExpectedEffects == null) return;
                Dictionary<string, UnitEntityData> units;
                Dictionary<string, IEnumerable<ObservedEffectInstance>> observed = ReadAfter(step, out units);
                AppliedEffectJudgement.RecordProgress(baseline, step.EffectObservationUnitIds,
                    unitId => observed.ContainsKey(unitId) ? observed[unitId] : null, Geometry(step, units));
            }
            catch (Exception) { /* An unreadable progress read records nothing. */ }
        }

        private static Dictionary<string, IEnumerable<ObservedEffectInstance>> ReadAfter(CastStep step,
            out Dictionary<string, UnitEntityData> units)
        {
            units = KingmakerAnimatedCastAdapter.CollectUnits();
            HashSet<string> ids = ExpectedIds(step.ExpectedEffects);
            var observed = new Dictionary<string, IEnumerable<ObservedEffectInstance>>(StringComparer.Ordinal);
            foreach (string unitId in step.EffectObservationUnitIds)
            {
                UnitEntityData unit;
                observed[unitId] = units.TryGetValue(unitId, out unit) && unit.Descriptor != null
                    ? Instances(unit, ids) : null;
            }
            return observed;
        }

        // Application-boundary geometry for natively selected obligations: at
        // this read, every unit the nearby wrapper's own filter (the same
        // GameHelper.GetTargetsAround query, self/friend/enemy test and
        // UnitEntityData.DistanceTo it sorts by) admits within radius plus the
        // judgement tolerance of each selection centre - party or not - with
        // its native distance. Null when anything is unreadable.
        private static IDictionary<string, IReadOnlyList<NearbyEligibleUnit>> Geometry(CastStep step,
            Dictionary<string, UnitEntityData> units)
        {
            var bindings = EffectApplicationLifetime.Leaves(step.ExpectedEffects)
                .Select(leaf => leaf.SecondarySelection).Where(binding => binding != null)
                .GroupBy(binding => binding.GeometryKey, StringComparer.Ordinal).Select(group => group.First()).ToList();
            if (bindings.Count == 0) return null;
            try
            {
                UnitEntityData caster;
                if (!units.TryGetValue(step.Provider.CasterUnitId, out caster)) return null;
                var geometry = new Dictionary<string, IReadOnlyList<NearbyEligibleUnit>>(StringComparer.Ordinal);
                foreach (string center in step.ExpectedRecipientUnitIds)
                {
                    UnitEntityData origin;
                    if (!units.TryGetValue(center, out origin)) return null;
                    foreach (NearbySelectionBinding binding in bindings)
                    {
                        float reach = binding.Radius + AppliedEffectJudgement.SecondaryDistanceTolerance;
                        geometry[EffectBaseline.GeometryKey(center, binding)] = Kingmaker.Designers.GameHelper
                            .GetTargetsAround(origin.Position, reach, false, false)
                            .Where(unit => unit != null && unit != origin && (binding.Self || unit != caster) &&
                                (caster.IsAlly(unit) ? binding.Friends : binding.Enemies) &&
                                !string.IsNullOrWhiteSpace(unit.UniqueId))
                            .Select(unit => new NearbyEligibleUnit(unit.UniqueId, origin.DistanceTo(unit)))
                            .ToList();
                    }
                }
                return geometry;
            }
            catch (Exception) { return null; }
        }

        private static bool Judge(CastStep step, EffectBaseline baseline,
            Func<string, IEnumerable<ObservedEffectInstance>> readAfter)
        {
            if (step.MassCast)
                return AppliedEffectJudgement.AnyReached(step.ExpectedRecipientUnitIds,
                    step.ExpectedEffects, baseline, readAfter, step.Provider.CasterUnitId);
            return AppliedEffectJudgement.AllReached(step.ExpectedRecipientUnitIds,
                step.ExpectedEffects, baseline, readAfter, step.PreCoveredRecipientUnitIds, step.Provider.CasterUnitId);
        }

        internal static string Describe(CastStep step, EffectBaseline before)
        {
            if (step == null) return "step-missing";
            string failure;
            EffectBaseline after = ReadBaseline(step, out failure);
            return "before:{" + (before == null ? "unreadable" :
                before.Describe(step.EffectObservationUnitIds)) + "};after:{" +
                (after == null ? "unreadable:" + failure :
                    after.Describe(step.EffectObservationUnitIds)) + "};confirmation:" +
                (before == null ? "baseline-unreadable" : before.DescribeConfirmationPolls());
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
                bool suppressionReadable;
                bool suppressed = Suppressed(buff, out suppressionReadable);
                instances.Add(new ObservedEffectInstance(
                    string.IsNullOrWhiteSpace(buff.SourceAreaEffectId)
                        ? EffectKind.Buff : EffectKind.AreaBuff,
                    buff.Blueprint.AssetGuid, Key(buff), buff.EndTime.Ticks, suppressed,
                    "suppression-readable=" + suppressionReadable + ":" + ContextDetail(buff)));
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
        private static bool Suppressed(Buff buff, out bool readable)
        {
            readable = false;
            try
            {
                bool suppressed = buff.IsSuppressed;
                readable = true;
                return suppressed;
            }
            catch (Exception) { return true; }
        }

        private static string Key(object instance)
        {
            return RuntimeHelpers.GetHashCode(instance).ToString(CultureInfo.InvariantCulture);
        }

        private static string ContextDetail(Buff buff)
        {
            try
            {
                var parameters = buff.Context == null ? null : buff.Context.Params;
                return "stacking=" + buff.Blueprint.Stacking + ":remaining-ticks=" +
                    buff.TimeLeft.Ticks.ToString(CultureInfo.InvariantCulture) + ":cl=" +
                    (parameters == null ? "unreadable" : parameters.CasterLevel.ToString(CultureInfo.InvariantCulture)) +
                    ":metamagic=" + (parameters == null ? "unreadable" :
                        ((int)parameters.Metamagic).ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception) { return "native-context-unreadable"; }
        }
    }
}
