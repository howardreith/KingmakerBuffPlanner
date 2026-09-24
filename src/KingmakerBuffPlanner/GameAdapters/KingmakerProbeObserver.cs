using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Execution;

namespace KingmakerBuffPlanner.GameAdapters
{
    // Review M2: the probe's authoritative native producer. Each call reads
    // the live game state directly - the caster's AbilityData availability
    // and the target's buffs matching the expected effect ids - never the
    // UI's cached discovery, and stamps the sequence at the moment of the
    // read. Any failure is returned as a failed observation.
    internal sealed class KingmakerProbeObserver : IProbeObserver
    {
        public ProbeObservation Observe(CastStep step, string phase, ProbeSequenceClock clock)
        {
            return ObserveUnit(step, step == null ? null : step.TargetUnitIds.FirstOrDefault(), phase, clock);
        }

        // One expected recipient of a group casting: the same source reads,
        // and that recipient's instances of the expected effects.
        public ProbeObservation ObserveRecipient(CastStep step, string recipientUnitId, string phase,
            ProbeSequenceClock clock)
        {
            return ObserveUnit(step, recipientUnitId, phase, clock);
        }

        private ProbeObservation ObserveUnit(CastStep step, string targetId, string phase,
            ProbeSequenceClock clock)
        {
            try
            {
                if (Game.Instance == null || Game.Instance.Player == null)
                    return ProbeObservation.Failed(phase, clock.Next(), DateTime.UtcNow, "player-state-unavailable");
                // Review N3: resolve units and the source for OBSERVATION,
                // independent of spendability - after a successful cast the
                // reserved prepared slot is consumed but is still the source.
                Dictionary<string, UnitEntityData> units = KingmakerAnimatedCastAdapter.CollectUnits();
                UnitEntityData caster;
                UnitEntityData target;
                if (!units.TryGetValue(step.Provider.CasterUnitId, out caster) || caster.Descriptor == null)
                    return ProbeObservation.Failed(phase, clock.Next(), DateTime.UtcNow, "caster-not-in-party");
                if (string.IsNullOrEmpty(targetId) || !units.TryGetValue(targetId, out target) ||
                    target.Descriptor == null)
                    return ProbeObservation.Failed(phase, clock.Next(), DateTime.UtcNow, "target-not-in-party");
                IDictionary<string, bool> reserved = null;
                if (step.Reservation != null && step.Reservation.TokenIds.Count != 0)
                {
                    string failure;
                    reserved = ProbeSourceSlots.ReservedExactly(
                        KingmakerAnimatedCastAdapter.ObserveSpellbookSlots(caster, step.Provider),
                        step.Reservation.TokenIds, out failure);
                    if (reserved == null)
                        return ProbeObservation.Failed(phase, clock.Next(), DateTime.UtcNow, failure);
                }
                AbilityData ability = KingmakerAnimatedCastAdapter.ResolveAbility(caster, step);
                int? available = ability == null ? (int?)null : ability.GetAvailableForCastCount();
                if (available == null && reserved == null)
                    return ProbeObservation.Failed(phase, clock.Next(), DateTime.UtcNow, "source-ability-not-found");
                List<ProbeEffectInstance> instances = Instances(target, ExpectedIds(step.ExpectedEffects));
                return ProbeObservation.Read(phase, clock.Next(), DateTime.UtcNow, target.UniqueId,
                    available, instances, reserved);
            }
            catch (Exception exception)
            {
                return ProbeObservation.Failed(phase, clock.Next(), DateTime.UtcNow,
                    "native-read-exception:" + exception.GetType().Name + ":" + exception.Message);
            }
        }

        // Fresh presence read for target preference (null = unknown).
        internal static bool? EffectPresent(string unitId, EffectExpression expected)
        {
            try
            {
                if (Game.Instance == null || Game.Instance.Player == null) return null;
                UnitEntityData unit = (Game.Instance.Player.Party ?? new List<UnitEntityData>())
                    .FirstOrDefault(value => value != null && value.UniqueId == unitId);
                if (unit == null || unit.Descriptor == null) return null;
                return Instances(unit, ExpectedIds(expected)).Count != 0;
            }
            catch (Exception) { return null; }
        }

        private static HashSet<string> ExpectedIds(EffectExpression expected)
        {
            return new HashSet<string>(KingmakerAnimatedCastAdapter.ExpectedEffectIds(expected)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        }

        private static List<ProbeEffectInstance> Instances(UnitEntityData unit, HashSet<string> ids)
        {
            return unit.Descriptor.Buffs.Enumerable
                .Where(buff => buff != null && buff.Blueprint != null && ids.Contains(buff.Blueprint.AssetGuid))
                .Select(buff => new ProbeEffectInstance(buff.Blueprint.AssetGuid,
                    RuntimeHelpers.GetHashCode(buff).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    buff.EndTime.Ticks))
                .ToList();
        }
    }
}
