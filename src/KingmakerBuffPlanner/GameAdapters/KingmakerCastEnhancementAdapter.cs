using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using KingmakerBuffPlanner.Compatibility;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerCastEnhancementAdapter
    {
        private readonly BrownFurPowerfulChangeCompatibility _brownFur =
            new BrownFurPowerfulChangeCompatibility();

        internal IReadOnlyList<string> ContractDiagnostics
        { get { return _brownFur.ContractDiagnostics; } }

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
            if (!KingmakerAnimatedCastAdapter.TryResolve(step, out resolved, out reason))
                return CastEnhancementPreparation.Fail("cast-resolution:" + reason);
            List<Entry> entries = RodEntries(resolved.Caster).Concat(
                _brownFur.ForCast(resolved.Caster, step.Provider,
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
                Entry entry = matches.FirstOrDefault(value =>
                    value.Ability.IsAvailable && value.Ability.ResourceCount > 0);
                if (entry == null) return CastEnhancementPreparation.Fail("source-exhausted:" + id);
                selected.Add(entry);
            }
            if (!CastEnhancementSnapshot.AreCompatible(selected.Select(value => value.Snapshot)))
                return CastEnhancementPreparation.Fail("enhancement-conflict");
            var states = new List<State>();
            foreach (Entry entry in entries)
            {
                State state = states.FirstOrDefault(value =>
                    ReferenceEquals(value.Ability, entry.Ability));
                if (state == null)
                {
                    state = new State(entry.Ability, entry.Ability.IsOn);
                    states.Add(state);
                }
                state.OneShot = state.OneShot || entry.OneShot;
                state.Selected = state.Selected || selected.Contains(entry);
            }
            var lease = new ActivationLease(states);
            try
            {
                foreach (State state in states)
                    state.Ability.IsOn = state.Selected;
                if (states.Any(value => value.Selected && !value.Ability.IsOn))
                {
                    lease.Dispose();
                    return CastEnhancementPreparation.Fail("activation-refused");
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
                first.UsagePoolId, first.RequiresNativeCommand);
        }

        private static bool TryDescribePersisted(string id, out CastEnhancementSnapshot snapshot)
        {
            snapshot = null;
            string[] parts = (id ?? string.Empty).Split('|');
            if (parts.Length != 3 || parts[0] != "metamagic-rod" ||
                string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]))
                return BrownFurPowerfulChangeCompatibility.TryDescribePersisted(
                    id, out snapshot);
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
                Humanize(mechanics.Metamagic.ToString()));
            return true;
        }

        private static string Humanize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Metamagic";
            var result = new System.Text.StringBuilder(value.Length + 4);
            for (int index = 0; index < value.Length; index++)
            {
                if (index != 0 && char.IsUpper(value[index]) && !char.IsUpper(value[index - 1]))
                    result.Append(' ');
                result.Append(value[index]);
            }
            return result.ToString();
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
                    Humanize(mechanics.Metamagic.ToString()));
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
            internal State(ActivatableAbility ability, bool isOn) { Ability = ability; IsOn = isOn; }
            internal ActivatableAbility Ability;
            internal bool IsOn;
            internal bool Selected;
            internal bool OneShot;
            internal bool ArmedByLease;
        }

        private sealed class ActivationLease : IDisposable
        {
            private readonly IReadOnlyList<State> _states;
            private bool _disposed;
            internal ActivationLease(IReadOnlyList<State> states) { _states = states; }
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                bool oneShotConsumed = _states.Any(state => state.OneShot &&
                    state.Selected && state.ArmedByLease &&
                    !state.Ability.IsOn);
                foreach (State state in _states.Reverse())
                {
                    try
                    {
                        // A successful provider transaction consumes the
                        // selected member of its mutually-exclusive one-shot
                        // group. Do not resurrect any prior member afterward.
                        if (CastEnhancementActivationPolicy.RestoreOriginalState(
                                state.OneShot, oneShotConsumed))
                            state.Ability.IsOn = state.IsOn;
                    }
                    catch (Exception) { }
                }
            }
        }
    }
}
