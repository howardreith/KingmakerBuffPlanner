using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerAnimatedCastAdapter : ICastRuntimeAdapter
    {
        public bool IsInCombat
        {
            get { return Game.Instance != null && Game.Instance.Player != null && Game.Instance.Player.IsInCombat; }
        }

        public CastRuntimeValidation Validate(CastStep step)
        {
            if (step == null) throw new ArgumentNullException("step");
            ResolvedCast resolved;
            string reason;
            if (!TryResolve(step, out resolved, out reason)) return CastRuntimeValidation.Fail(reason);
            if (resolved.Caster.Descriptor == null || resolved.Caster.Descriptor.State == null ||
                resolved.Caster.Descriptor.State.IsDead || !resolved.Caster.Descriptor.State.IsConscious)
                return CastRuntimeValidation.Fail("caster-incapacitated");
            if (!resolved.Ability.IsAvailableForCast) return CastRuntimeValidation.Fail("ability-unavailable");
            if (!MaterialComponentAvailability.IsSatisfied(
                resolved.Ability.RequireMaterialComponent,
                () => resolved.Ability.HasEnoughMaterialComponent))
                return CastRuntimeValidation.Fail("material-component-unavailable");
            if (!resolved.Ability.CanTarget(resolved.Target)) return CastRuntimeValidation.Fail("target-invalid");
            return CastRuntimeValidation.Pass();
        }

        public IAnimatedCastOperation StartAnimated(CastStep step)
        {
            ResolvedCast resolved;
            string reason;
            if (!TryResolve(step, out resolved, out reason))
                throw new InvalidOperationException(reason);
            UnitCommand command = UnitUseAbility.CreateCastCommand(resolved.Ability, resolved.Target);
            if (command == null) throw new InvalidOperationException("Kingmaker returned no cast command.");
            int availableBefore = SafeAvailableCount(resolved.Ability);
            resolved.Caster.Commands.AddToQueue(command);
            return new KingmakerAnimatedOperation(command, step, resolved.Ability, availableBefore);
        }

        internal static bool TryResolve(CastStep step, out ResolvedCast resolved, out string reason)
        {
            resolved = null;
            reason = string.Empty;
            if (Game.Instance == null || Game.Instance.Player == null)
                return Fail("player-state-unavailable", out reason);
            Dictionary<string, UnitEntityData> units = CollectUnits();
            UnitEntityData caster;
            if (!units.TryGetValue(step.Provider.CasterUnitId, out caster))
                return Fail("caster-not-in-party", out reason);
            string targetId = step.MassCast
                ? step.AnchorUnitId
                : step.TargetUnitIds.FirstOrDefault();
            UnitEntityData target;
            if (string.IsNullOrEmpty(targetId) || !units.TryGetValue(targetId, out target))
                return Fail("target-not-in-party", out reason);
            AbilityData ability = ResolveAbility(caster, step);
            if (ability == null) return Fail("provider-ability-not-found", out reason);
            resolved = new ResolvedCast(caster, ability, new TargetWrapper(target));
            return true;
        }

        private static AbilityData ResolveAbility(UnitEntityData caster, CastStep step)
        {
            ProviderKey provider = step.Provider;
            if (provider.Ability.SourceKind == SourceKind.Spellbook)
            {
                Spellbook book = caster.Descriptor.Spellbooks.FirstOrDefault(b => b != null &&
                    b.Blueprint != null && b.Blueprint.AssetGuid == provider.SpellbookGuid);
                if (book == null) return null;
                if (step.Reservation.TokenIds.Count != 0)
                {
                    foreach (SpellSlot slot in book.GetAllMemorizedSpells().Where(s => s != null &&
                        s.Spell != null && s.IsMainSlot && step.Reservation.TokenIds.Contains(SlotId(s))))
                    {
                        AbilityData match = Expand(new[] { slot.Spell }).FirstOrDefault(d => Matches(d, provider.Ability));
                        if (match != null) return match;
                    }
                    return null;
                }
                foreach (AbilityData data in Expand(book.GetAllKnownSpells()))
                    if (Matches(data, provider.Ability) && SourceInstanceMatches(data, provider.SourceInstanceId))
                        return data;
                for (int level = 0; level <= book.MaxSpellLevel; level++)
                    foreach (AbilityData data in Expand(book.GetCustomSpells(level)))
                        if (Matches(data, provider.Ability) && SourceInstanceMatches(data, provider.SourceInstanceId))
                            return data;
                return null;
            }
            if (provider.Ability.SourceKind == SourceKind.AbilityResource ||
                provider.Ability.SourceKind == SourceKind.Fact)
            {
                return caster.Descriptor.Abilities.Enumerable.Where(a => a != null && a.Data != null)
                    .Select(a => a.Data).FirstOrDefault(d => Matches(d, provider.Ability));
            }
            return null;
        }

        private static bool Matches(AbilityData data, AbilityKey key)
        {
            if (data == null || data.Blueprint == null) return false;
            string baseGuid = data.Blueprint.Parent == null
                ? data.Blueprint.AssetGuid
                : data.Blueprint.Parent.AssetGuid;
            string variantGuid = data.Blueprint.Parent == null ? string.Empty : data.Blueprint.AssetGuid;
            int metamagic = data.MetamagicData == null ? 0 : (int)data.MetamagicData.MetamagicMask;
            return baseGuid == key.BaseAbilityGuid && variantGuid == key.VariantGuid &&
                metamagic == key.MetamagicMask;
        }

        private static bool SourceInstanceMatches(AbilityData data, string sourceInstance)
        {
            int heighten = data.MetamagicData == null ? 0 : data.MetamagicData.HeightenLevel;
            return sourceInstance == "level-" + data.SpellLevel + "|heighten-" + heighten;
        }

        private static IEnumerable<AbilityData> Expand(IEnumerable<AbilityData> source)
        {
            foreach (AbilityData data in source ?? new AbilityData[0])
            {
                if (data == null) continue;
                yield return data;
                foreach (AbilityData variant in data.Variants ?? new AbilityData[0])
                    if (variant != null) yield return variant;
            }
        }

        private static Dictionary<string, UnitEntityData> CollectUnits()
        {
            var units = new Dictionary<string, UnitEntityData>(StringComparer.Ordinal);
            foreach (UnitEntityData unit in Game.Instance.Player.Party ?? new List<UnitEntityData>())
            {
                if (unit == null || string.IsNullOrWhiteSpace(unit.UniqueId)) continue;
                units[unit.UniqueId] = unit;
                UnitEntityData pet = unit.Descriptor == null ? null : unit.Descriptor.Pet;
                if (pet != null && !string.IsNullOrWhiteSpace(pet.UniqueId)) units[pet.UniqueId] = pet;
            }
            return units;
        }

        private static string SlotId(SpellSlot slot)
        {
            return "level-" + slot.SpellLevel + "|type-" + (int)slot.Type + "|index-" + slot.Index;
        }

        private static bool Fail(string value, out string reason)
        {
            reason = value;
            return false;
        }

        internal static int SafeAvailableCount(AbilityData ability)
        {
            try { return ability == null ? -1 : ability.GetAvailableForCastCount(); }
            catch (Exception) { return -1; }
        }

        internal static string ExpectedEffectIds(EffectExpression expression)
        {
            var ids = new SortedSet<string>(StringComparer.Ordinal);
            CollectEffectIds(expression, ids);
            return string.Join(",", ids.ToArray());
        }

        private static void CollectEffectIds(EffectExpression expression, ISet<string> ids)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) { ids.Add(leaf.EffectId); return; }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                foreach (EffectExpression child in sequence.Children) CollectEffectIds(child, ids);
                return;
            }
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                CollectEffectIds(conditional.WhenTrue, ids);
                CollectEffectIds(conditional.WhenFalse, ids);
                return;
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) { CollectEffectIds(targeted.Child, ids); return; }
            var referenced = expression as ReferencedAbilityExpression;
            if (referenced != null) CollectEffectIds(referenced.Child, ids);
        }

        internal sealed class ResolvedCast
        {
            internal ResolvedCast(UnitEntityData caster, AbilityData ability, TargetWrapper target)
            {
                Caster = caster;
                Ability = ability;
                Target = target;
            }
            internal UnitEntityData Caster;
            internal AbilityData Ability;
            internal TargetWrapper Target;
        }

        private sealed class KingmakerAnimatedOperation : IAnimatedCastOperation
        {
            private readonly UnitCommand _command;
            private readonly CastStep _step;
            private readonly AbilityData _ability;
            private readonly int _availableBefore;
            private int _postCompletionFrames;
            private int _pollFrames;
            private bool? _observed;
            private bool _timedOut;

            internal KingmakerAnimatedOperation(UnitCommand command, CastStep step,
                AbilityData ability, int availableBefore)
            {
                _command = command;
                _step = step;
                _ability = ability;
                _availableBefore = availableBefore;
            }

            public bool IsCompleted
            {
                get
                {
                    if (++_pollFrames >= 3600 && !_command.IsFinished)
                    {
                        _timedOut = true;
                        return true;
                    }
                    if (!_command.IsFinished) return false;
                    if (_command.Result != UnitCommand.ResultType.Success) return true;
                    if (EffectsObserved) return true;
                    return ++_postCompletionFrames >= 12;
                }
            }

            public bool IsStarted { get { return _command.IsStarted; } }
            public bool TimedOut { get { return _timedOut; } }
            public bool Succeeded { get { return _command.Result == UnitCommand.ResultType.Success; } }
            public bool ResourceSpent
            {
                get
                {
                    int after = SafeAvailableCount(_ability);
                    return _availableBefore >= 0 && after >= 0 && after < _availableBefore;
                }
            }
            public bool EffectsObserved
            {
                get
                {
                    if (_observed == true) return true;
                    try
                    {
                        var active = new KingmakerActiveEffectSnapshotBuilder().Build();
                        var evaluator = new EffectPresenceEvaluator();
                        bool observed = _step.TargetUnitIds.All(targetId =>
                            evaluator.EvaluateTyped(_step.ExpectedEffects, active.GetEffects(targetId), null).Kind ==
                                EffectPresenceKind.Complete);
                        if (observed) _observed = true;
                        return observed;
                    }
                    catch (Exception) { return false; }
                }
            }
            public string Detail
            {
                get
                {
                    return "command-started:" + _command.IsStarted + ";command-result:" +
                        _command.Result + ";timed-out:" + _timedOut + ";available-before:" +
                        _availableBefore + ";available-after:" + SafeAvailableCount(_ability) +
                        ";expected-effects:" + ExpectedEffectIds(_step.ExpectedEffects) +
                        ";targets:" + string.Join(",", _step.TargetUnitIds.ToArray()) +
                        ";effects-observed:" + EffectsObserved;
                }
            }
        }
    }

}
