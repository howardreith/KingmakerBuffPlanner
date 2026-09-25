using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
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
                long? gameTime = null;
                string clockFailure = null;
                try { gameTime = Game.Instance.TimeController.GameTime.Ticks; }
                catch (Exception exception) { clockFailure = exception.GetType().Name; }
                return ProbeObservation.Read(phase, clock.Next(), DateTime.UtcNow, target.UniqueId,
                    available, instances, reserved, gameTime, clockFailure);
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
                    buff.EndTime.Ticks, Modifiers(unit, buff)))
                .ToList();
        }

        // The stat modifiers one buff instance gives its unit, each
        // "<stat>/<descriptor>/<value>" (the modifier's source is the
        // instance itself); null when they cannot be read.
        private static List<string> Modifiers(UnitEntityData unit, Buff buff)
        {
            try
            {
                var result = new List<string>();
                foreach (ModifiableValue stat in unit.Descriptor.Stats.GetList())
                {
                    if (stat == null) continue;
                    foreach (ModifiableValue.Modifier modifier in stat.Modifiers)
                        if (modifier != null && ReferenceEquals(modifier.Source, buff))
                            result.Add(stat.Type + "/" + modifier.ModDescriptor + "/" +
                                modifier.ModValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                return result;
            }
            catch (Exception) { return null; }
        }

        // The enhanced recipe's caster read: the amount of the enhancement's
        // own resource (a "class-feature-resource|<caster>|<resource>" pool)
        // and every activatable ability of the caster, on or off.
        public CasterEnhancementObservation ObserveCaster(string casterUnitId, string usagePoolId)
        {
            try
            {
                if (Game.Instance == null || Game.Instance.Player == null)
                    return CasterEnhancementObservation.Failed("player-state-unavailable");
                UnitEntityData caster;
                if (!KingmakerAnimatedCastAdapter.CollectUnits().TryGetValue(casterUnitId ?? string.Empty,
                        out caster) || caster.Descriptor == null)
                    return CasterEnhancementObservation.Failed("caster-not-in-party");
                string[] parts = (usagePoolId ?? string.Empty).Split('|');
                if (parts.Length != 3 || parts[1] != casterUnitId ||
                    (parts[0] != "class-feature-resource" && parts[0] != "metamagic-rod"))
                    return CasterEnhancementObservation.Failed("pool-not-a-caster-resource:" + usagePoolId);
                int amount;
                if (parts[0] == "metamagic-rod")
                {
                    // A rod's charges: its toggle's own resource count (every
                    // copy of that rod item the caster carries).
                    int? charges = null;
                    foreach (ActivatableAbility rod in caster.Descriptor.ActivatableAbilities.Enumerable)
                    {
                        if (rod == null || rod.Blueprint == null || rod.Blueprint.Buff == null ||
                            rod.Blueprint.Buff.GetComponent<Kingmaker.Designers.Mechanics.Facts.MetamagicRodMechanics>() == null)
                            continue;
                        string item = rod.SourceItem != null && rod.SourceItem.Blueprint != null
                            ? rod.SourceItem.Blueprint.AssetGuid : rod.Blueprint.AssetGuid;
                        if (item != parts[2]) continue;
                        if (rod.ResourceCount == null)
                            return CasterEnhancementObservation.Failed("rod-charges-unread:" + parts[2]);
                        charges = (charges ?? 0) + rod.ResourceCount.Value;
                    }
                    if (charges == null) return CasterEnhancementObservation.Failed("rod-missing:" + parts[2]);
                    amount = charges.Value;
                }
                else
                {
                    BlueprintAbilityResource resource =
                        ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(parts[2]);
                    if (resource == null)
                        return CasterEnhancementObservation.Failed("resource-blueprint-missing:" + parts[2]);
                    amount = caster.Descriptor.Resources.GetResourceAmount(resource);
                }
                var activatables = new Dictionary<string, bool>(StringComparer.Ordinal);
                foreach (ActivatableAbility ability in caster.Descriptor.ActivatableAbilities.Enumerable)
                {
                    if (ability == null || ability.Blueprint == null) continue;
                    string key = ability.Blueprint.AssetGuid;
                    for (int index = 2; activatables.ContainsKey(key); index++)
                        key = ability.Blueprint.AssetGuid + "#" +
                            index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    activatables[key] = ability.IsOn;
                }
                return CasterEnhancementObservation.Read(amount, activatables);
            }
            catch (Exception exception)
            {
                return CasterEnhancementObservation.Failed("native-read-exception:" +
                    exception.GetType().Name + ":" + exception.Message);
            }
        }
    }
}
