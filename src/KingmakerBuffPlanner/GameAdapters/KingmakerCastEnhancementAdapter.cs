using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Execution;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerCastEnhancementAdapter
    {
        internal CastEnhancementSnapshot[] Discover()
        {
            return KingmakerAnimatedCastAdapter.CollectUnits().Values
                .Where(unit => unit != null && unit.Descriptor != null)
                .SelectMany(Entries)
                .GroupBy(entry => entry.Snapshot.EnhancementId, StringComparer.Ordinal)
                .Select(Combine)
                .OrderBy(value => value.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.EnhancementId, StringComparer.Ordinal).ToArray();
        }

        internal CastEnhancementPreparation Prepare(CastStep step)
        {
            KingmakerAnimatedCastAdapter.ResolvedCast resolved;
            string reason;
            if (!KingmakerAnimatedCastAdapter.TryResolve(step, out resolved, out reason))
                return CastEnhancementPreparation.Fail("cast-resolution:" + reason);
            List<Entry> rods = Entries(resolved.Caster).ToList();
            List<Entry> selected = new List<Entry>();
            foreach (string id in step.EnhancementIds)
            {
                List<Entry> matches = rods.Where(value => value.Snapshot.EnhancementId == id).ToList();
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
            var states = rods.Select(value => new State(value.Ability, value.Ability.IsOn)).ToList();
            var lease = new ActivationLease(states);
            try
            {
                foreach (Entry rod in rods) rod.Ability.IsOn = selected.Contains(rod);
                if (selected.Any(value => !value.Ability.IsOn))
                {
                    lease.Dispose();
                    return CastEnhancementPreparation.Fail("activation-refused");
                }
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
                first.MetamagicMask, first.MaximumSpellLevel, remaining, first.AbilityWhiteList);
        }

        private static IEnumerable<Entry> Entries(UnitEntityData unit)
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
                        .Where(value => value != null).Select(value => value.AssetGuid));
                yield return new Entry(ability, snapshot);
            }
        }

        private sealed class Entry
        {
            internal Entry(ActivatableAbility ability, CastEnhancementSnapshot snapshot)
            {
                Ability = ability;
                Snapshot = snapshot;
            }
            internal ActivatableAbility Ability;
            internal CastEnhancementSnapshot Snapshot;
        }

        private sealed class State
        {
            internal State(ActivatableAbility ability, bool isOn) { Ability = ability; IsOn = isOn; }
            internal ActivatableAbility Ability;
            internal bool IsOn;
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
                foreach (State state in _states.Reverse())
                {
                    try { state.Ability.IsOn = state.IsOn; }
                    catch (Exception) { }
                }
            }
        }
    }
}
