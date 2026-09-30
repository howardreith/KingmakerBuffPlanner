using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using KingmakerBuffPlanner.Compatibility;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;
using UnityEngine;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerCastEnhancementAdapter
    {
        private readonly BrownFurPowerfulChangeCompatibility _brownFur =
            new BrownFurPowerfulChangeCompatibility();
        private readonly BrownFurShareTransmutationCompatibility _share =
            new BrownFurShareTransmutationCompatibility();

        internal IReadOnlyList<string> ContractDiagnostics
        {
            get
            {
                return _brownFur.ContractDiagnostics.Concat(
                    _share.ContractDiagnostics).ToArray();
            }
        }

        internal CastEnhancementSnapshot[] Discover(
            PartyProviderSnapshot snapshot,
            IEnumerable<string> persistedEnhancementIds = null)
        {
            UnitEntityData[] units = KingmakerAnimatedCastAdapter.CollectUnits()
                .Values
                .Where(unit => unit != null && unit.Descriptor != null)
                .ToArray();
            var runtimeEntries = units.SelectMany(RodEntries).Concat(
                _brownFur.Discover(units, snapshot).Select(value =>
                    new Entry(value.Ability, value.Snapshot, true))).Concat(
                _share.Discover(units, snapshot).Select(value =>
                    new Entry(value.Ability, value.Snapshot, true)));
            var values = runtimeEntries
                .GroupBy(entry => entry.Snapshot.EnhancementId, StringComparer.Ordinal)
                .Select(Combine).ToList();
            var known = new HashSet<string>(values.Select(value => value.EnhancementId),
                StringComparer.Ordinal);
            foreach (string id in persistedEnhancementIds ?? new string[0])
            {
                CastEnhancementSnapshot unavailable;
                if (!known.Contains(id) && TryDescribePersisted(id, out unavailable))
                {
                    values.Add(unavailable);
                    known.Add(id);
                }
            }
            return values.OrderBy(value => value.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.EnhancementId, StringComparer.Ordinal).ToArray();
        }

        internal string Describe(ProviderSnapshot provider,
            IEnumerable<CastEnhancementSnapshot> enhancements)
        {
            return _brownFur.Describe(provider, enhancements);
        }

        internal CastEnhancementPreparation Prepare(CastStep step)
        {
            KingmakerAnimatedCastAdapter.ResolvedCast resolved;
            string reason;
            // A casting-first step with no enhancement whose cast cannot be
            // resolved is left to the cast's own validation, which refuses it
            // with the same reason as before (nothing will be cast).
            if (!KingmakerAnimatedCastAdapter.TryResolve(step, out resolved, out reason))
                return step.EnhancementIds.Count == 0 ? CastEnhancementPreparation.Pass(null)
                    : CastEnhancementPreparation.Fail("cast-resolution:" + reason);
            List<Entry> entries = RodEntries(resolved.Caster).Concat(
                _brownFur.ForCast(resolved.Caster, step.Provider,
                    resolved.Ability).Select(value => new Entry(value.Ability,
                        value.Snapshot, true))).Concat(
                _share.ForCast(resolved.Caster, step.Provider,
                    resolved.Ability).Select(value => new Entry(value.Ability,
                        value.Snapshot, true))).ToList();
            List<Entry> selected = new List<Entry>();
            foreach (string id in step.EnhancementIds)
            {
                List<Entry> matches = entries.Where(value =>
                    value.Snapshot.EnhancementId == id).ToList();
                if (matches.Count == 0) return CastEnhancementPreparation.Fail("source-not-owned:" + id);
                if (!matches[0].Snapshot.IsApplicable(step.Provider, resolved.Ability.SpellLevel))
                    return CastEnhancementPreparation.Fail("source-inapplicable:" + id);
                // Casting-first prefers the copy already running (then on),
                // so a rod the player left on is the one used and no other
                // copy has to be started or stopped for it.
                Entry entry = step.ExactEnhancements
                    ? matches.Where(value => value.Ability.IsAvailable)
                        .OrderByDescending(value => value.Ability.IsRunning)
                        .ThenByDescending(value => value.Ability.IsOn).FirstOrDefault()
                    : matches.FirstOrDefault(value => value.Ability.IsAvailable);
                if (entry == null) return CastEnhancementPreparation.Fail("source-exhausted:" + id);
                selected.Add(entry);
            }
            if (!CastEnhancementSnapshot.AreCompatible(selected.Select(value => value.Snapshot)))
                return CastEnhancementPreparation.Fail("enhancement-conflict");
            foreach (KeyValuePair<string, int> requirement in
                CastEnhancementSnapshot.UsageRequirements(selected.Select(
                    value => value.Snapshot)))
            {
                int?[] remaining = selected.Where(value =>
                        value.Snapshot.UsagePoolId == requirement.Key)
                    .Select(value => value.Snapshot.RemainingUses).Distinct()
                    .ToArray();
                if (remaining.Length != 1)
                    return CastEnhancementPreparation.Fail(
                        "usage-pool-contract-mismatch:" + requirement.Key);
                if (remaining[0] != null && remaining[0].Value <
                    requirement.Value)
                    return CastEnhancementPreparation.Fail(
                        "shared-pool-exhausted:" + requirement.Key +
                        ":required-" + requirement.Value + ":remaining-" +
                        remaining[0].Value);
            }
            var states = new List<State>();
            foreach (Entry entry in entries)
            {
                State state = states.FirstOrDefault(value =>
                    ReferenceEquals(value.Ability, entry.Ability));
                if (state == null)
                {
                    state = new State(entry.Ability, entry.Ability.IsOn,
                        entry.Snapshot.NativeActivationGroupId);
                    states.Add(state);
                }
                state.OneShot = state.OneShot || entry.OneShot;
                state.Rod = state.Rod || entry.Snapshot.Category == CastEnhancementCategory.MetamagicRod;
                state.Selected = state.Selected || selected.Contains(entry);
            }
            var lease = new ActivationLease(states, step.ExactEnhancements);
            try
            {
                if (step.ExactEnhancements)
                {
                    // Casting-first: everything the casting did not choose is
                    // switched off first. A toggle whose blueprint defers
                    // deactivation keeps running, its buff applied, until the
                    // next round after it is switched off
                    // (ActivatableAbility.OnTurnOff), so an unchosen rod still
                    // running is stopped now, its buff removed at once; the
                    // chosen ones go on only after that.
                    foreach (State state in states.Where(value => !value.Selected))
                        state.Ability.IsOn = false;
                    foreach (State state in states.Where(value => !value.Selected && value.Rod &&
                            value.Ability.IsRunning))
                        state.Ability.Stop(true);
                    foreach (State state in states.Where(value => value.Selected))
                        state.Ability.IsOn = true;
                }
                else
                    foreach (State state in states)
                        state.Ability.IsOn = state.Selected;
                if (states.Any(value => value.Selected && !value.Ability.IsOn))
                {
                    lease.Dispose();
                    return CastEnhancementPreparation.Fail("activation-refused");
                }
                // Casting-first: nothing the casting did not choose stays on or
                // running for its cast; anything that does refuses the cast.
                // Switching a stopped rod back on after the cast restarts it
                // (the qualification's caster reads check that this costs no
                // charge). The Brown Fur toggles stop at once only with the
                // installed provider's own immediate-off patch; without it an
                // unchosen one still running refuses the cast, visibly.
                if (step.ExactEnhancements)
                {
                    State still = states.FirstOrDefault(value => !value.Selected &&
                        (value.Ability.IsOn || value.Ability.IsRunning));
                    if (still != null)
                    {
                        lease.Dispose();
                        return CastEnhancementPreparation.Fail("deactivation-refused:" +
                            (still.Ability.Blueprint == null ? "unknown" : still.Ability.Blueprint.AssetGuid));
                    }
                }
                foreach (State state in states.Where(value => value.Selected &&
                    value.OneShot)) state.ArmedByLease = true;
                return CastEnhancementPreparation.Pass(lease);
            }
            catch (Exception exception)
            {
                lease.Dispose();
                return CastEnhancementPreparation.Fail("activation-exception:" +
                    exception.GetType().FullName + ":" + exception.Message);
            }
        }

        private static CastEnhancementSnapshot Combine(IGrouping<string, Entry> group)
        {
            List<CastEnhancementSnapshot> values = group.Select(entry => entry.Snapshot).ToList();
            CastEnhancementSnapshot first = values[0];
            int? remaining = values.Any(value => value.RemainingUses == null)
                ? (int?)null : values.Sum(value => value.RemainingUses.Value);
            return new CastEnhancementSnapshot(first.EnhancementId, first.CasterUnitId,
                first.SourceBlueprintGuid, first.DisplayName, first.Description, first.Category,
                first.MetamagicMask, first.MaximumSpellLevel, remaining, first.AbilityWhiteList,
                first.EffectDisplayName, first.SpellbookWhiteList,
                first.UsagePoolId, first.RequiresNativeCommand,
                first.ExclusiveGroupId, first.UsageUnitsPerCast,
                first.AffectsTargeting, first.NativeActivationGroupId,
                first.UsagePoolDisplayName, first.DirectCastProviderId);
        }

        private static bool TryDescribePersisted(string id, out CastEnhancementSnapshot snapshot)
        {
            snapshot = null;
            string[] parts = (id ?? string.Empty).Split('|');
            if (parts.Length != 3 || parts[0] != "metamagic-rod" ||
                string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]))
            {
                if (BrownFurPowerfulChangeCompatibility.TryDescribePersisted(
                        id, out snapshot)) return true;
                return BrownFurShareTransmutationCompatibility
                    .TryDescribePersisted(id, out snapshot);
            }
            BlueprintItem item = ResourcesLibrary.TryGetBlueprint<BlueprintItem>(parts[2]);
            BlueprintItemEquipment equipment = item as BlueprintItemEquipment;
            if (equipment == null || equipment.ActivatableAbility == null ||
                equipment.ActivatableAbility.Buff == null) return false;
            MetamagicRodMechanics mechanics = equipment.ActivatableAbility.Buff
                .GetComponent<MetamagicRodMechanics>();
            if (mechanics == null) return false;
            snapshot = new CastEnhancementSnapshot(id, parts[1], parts[2], item.Name,
                item.Description, CastEnhancementCategory.MetamagicRod,
                (int)mechanics.Metamagic, mechanics.MaxSpellLevel, 0,
                (mechanics.AbilitiesWhiteList ?? new Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility[0])
                    .Where(value => value != null).Select(value => value.AssetGuid),
                RodEffectName((int)mechanics.Metamagic, item.Name));
            return true;
        }

        // Effect naming contract: the game enum's own names first, then the
        // installed provider's display-name contract, then the item-derived
        // descriptor. Raw masks are logged as diagnostics only.
        private static string RodEffectName(int metamagicMask, string itemDisplayName)
        {
            string effect = CastEnhancementNaming.EffectDisplayName(
                metamagicMask, itemDisplayName, NamedMetamagic);
            if (CastEnhancementNaming.IsReadableName(((Metamagic)metamagicMask).ToString()))
                return effect;
            Debug.Log("[KBP-METAMAGIC] unnamed mask;value=" + metamagicMask +
                ";item='" + itemDisplayName + "';provider=" +
                CallOfTheWildMetamagicNames.ContractSummary +
                ";resolved='" + effect + "'.");
            return effect;
        }

        private static string NamedMetamagic(int metamagicMask)
        {
            string native = ((Metamagic)metamagicMask).ToString();
            if (CastEnhancementNaming.IsReadableName(native)) return native;
            return CallOfTheWildMetamagicNames.Describe(metamagicMask);
        }

        private static IEnumerable<Entry> RodEntries(UnitEntityData unit)
        {
            foreach (ActivatableAbility ability in unit.Descriptor.ActivatableAbilities.Enumerable
                .Where(value => value != null && value.Blueprint != null && value.Blueprint.Buff != null))
            {
                MetamagicRodMechanics mechanics =
                    ability.Blueprint.Buff.GetComponent<MetamagicRodMechanics>();
                if (mechanics == null || mechanics.RodAbility != ability.Blueprint) continue;
                string sourceGuid = ability.SourceItem != null && ability.SourceItem.Blueprint != null
                    ? ability.SourceItem.Blueprint.AssetGuid : ability.Blueprint.AssetGuid;
                string id = "metamagic-rod|" + unit.UniqueId + "|" + sourceGuid;
                string name = ability.SourceItem == null ? ability.Blueprint.Name : ability.SourceItem.Name;
                int? remaining = ability.ResourceCount;
                var snapshot = new CastEnhancementSnapshot(id, unit.UniqueId, sourceGuid,
                    name, ability.SourceItem == null ? ability.Blueprint.Description :
                        ability.SourceItem.Description, CastEnhancementCategory.MetamagicRod,
                    (int)mechanics.Metamagic, mechanics.MaxSpellLevel, remaining,
                    (mechanics.AbilitiesWhiteList ?? new Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility[0])
                        .Where(value => value != null).Select(value => value.AssetGuid),
                    RodEffectName((int)mechanics.Metamagic, name));
                yield return new Entry(ability, snapshot, false);
            }
        }

        private sealed class Entry
        {
            internal Entry(ActivatableAbility ability,
                CastEnhancementSnapshot snapshot, bool oneShot)
            {
                Ability = ability;
                Snapshot = snapshot;
                OneShot = oneShot;
            }
            internal ActivatableAbility Ability;
            internal CastEnhancementSnapshot Snapshot;
            internal bool OneShot;
        }

        private sealed class State
        {
            internal State(ActivatableAbility ability, bool isOn,
                string activationGroupId)
            {
                Ability = ability;
                IsOn = isOn;
                ActivationGroupId = activationGroupId ?? string.Empty;
            }
            internal ActivatableAbility Ability;
            internal bool IsOn;
            internal bool Selected;
            internal bool OneShot;
            internal bool Rod;
            internal bool ArmedByLease;
            internal string ActivationGroupId;
        }

        private sealed class ActivationLease : IDisposable,
            Execution.IEnhancementCleanupOutcome
        {
            private readonly IReadOnlyList<State> _states;
            private readonly bool _exact;
            private bool _disposed;
            internal ActivationLease(IReadOnlyList<State> states, bool exact)
            {
                _states = states;
                _exact = exact;
            }
            // R579-2: cleanup is OBSERVABLE. Empty when every owned native
            // state was restored to its policy-expected value; otherwise the
            // record of what could not be established. The executors surface
            // this in the run report and halt progression to a later cast,
            // because a non-throwing setter is not proof of restoration.
            public string CleanupFailure { get; private set; }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                var failures = new List<string>();
                var consumedGroups = new HashSet<string>(StringComparer.Ordinal);
                try
                {
                    foreach (string group in _states
                        .Where(state => state.OneShot && state.Selected &&
                            state.ArmedByLease && !state.Ability.IsOn)
                        .Select(state => state.ActivationGroupId))
                        consumedGroups.Add(group);
                }
                catch (Exception exception)
                {
                    // Consumption could not be read: every later verification
                    // is uncertain, so the failure is reported rather than
                    // guessed. One-shot members are NOT resurrected on this
                    // path (resurrecting a consumed feature is the harmful
                    // direction); the executors halt subsequent casts.
                    failures.Add("consumption-unreadable:" +
                        exception.GetType().Name);
                }
                foreach (State state in _states.Reverse())
                {
                    // Policy-aware expected value: a successfully consumed
                    // one-shot member stays consumed; everything else returns
                    // to its pre-cast state.
                    bool expected = CastEnhancementActivationPolicy
                        .RestoreOriginalState(state.OneShot,
                            state.ActivationGroupId, consumedGroups);
                    string identity = state.Ability.Blueprint == null
                        ? "unknown" : state.Ability.Blueprint.AssetGuid;
                    try
                    {
                        if (expected)
                            state.Ability.IsOn = state.IsOn;
                    }
                    catch (Exception exception)
                    {
                        failures.Add("restore-exception:" + identity + ":" +
                            exception.GetType().Name);
                        continue;
                    }
                    // Verify the postcondition instead of trusting a
                    // non-throwing setter; keep cleaning the other states.
                    try
                    {
                        if (state.Ability.IsOn != expected)
                            failures.Add("restore-mismatch:" + identity +
                                ";expected=" + expected + ";actual=" +
                                state.Ability.IsOn);
                    }
                    catch (Exception exception)
                    {
                        failures.Add("postcondition-unreadable:" + identity +
                            ":" + exception.GetType().Name);
                    }
                }
                // Casting-first: a rod switched on for the cast and back off
                // would keep running until the next round, extending whatever
                // is cast next; it is stopped now - with the same observable
                // failure reporting.
                if (_exact)
                    foreach (State state in _states.Where(value => value.Rod))
                    {
                        try
                        {
                            if (!state.Ability.IsOn && state.Ability.IsRunning)
                                state.Ability.Stop(true);
                        }
                        catch (Exception exception)
                        {
                            failures.Add("rod-stop-exception:" +
                                exception.GetType().Name);
                        }
                    }
                CleanupFailure = string.Join("|", failures.ToArray());
            }
        }
    }
}
