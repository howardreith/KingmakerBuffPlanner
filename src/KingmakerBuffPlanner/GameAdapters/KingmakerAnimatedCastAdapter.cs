using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerAnimatedCastAdapter : ICastRuntimeAdapter, ICastEnhancementRuntimeAdapter
    {
        public bool IsInCombat
        {
            get { return Game.Instance != null && Game.Instance.Player != null && Game.Instance.Player.IsInCombat; }
        }

        public CastRuntimeValidation Validate(CastStep step)
        {
            CastRuntimeValidation common = ValidateSource(step, false);
            if (!common.Valid) return common;
            ResolvedCast resolved;
            string reason;
            if (!TryResolve(step, out resolved, out reason))
                return CastRuntimeValidation.Fail(reason);
            AbilityEffectStickyTouch sticky = resolved.Ability.Blueprint == null
                ? null : resolved.Ability.Blueprint.StickyTouch;
            if (sticky == null)
                return CanTarget(resolved.Ability, resolved.Target)
                    ? CastRuntimeValidation.Pass()
                    : CastRuntimeValidation.Fail("target-invalid");

            if (KingmakerStickyTouchCastAdapter.HasHeldTouch(
                    resolved.Caster, null))
                return CastRuntimeValidation.Fail(
                    "sticky-touch-held-charge-already-active");
            BlueprintAbility delivery = sticky.TouchDeliveryAbility;
            if (delivery != null &&
                KingmakerStickyTouchCastAdapter.FindDeliveryCommand(
                    resolved.Caster, delivery, null) != null)
                return CastRuntimeValidation.Fail(
                    "sticky-delivery-command-already-active");
            CastExecutionCapability capability =
                KingmakerStickyTouchCastAdapter.Classify(
                    resolved.Ability.Blueprint);
            if (capability.Strategy ==
                CastExecutionStrategy.StickyTouchDeliveryRuleCast)
            {
                StickyTouchCastResolution resolution;
                if (!KingmakerStickyTouchCastAdapter.TryCreateDelivery(
                        resolved.Ability, out resolution, out reason))
                    return CastRuntimeValidation.Fail(
                        "sticky-delivery-resolution:" + reason);
                if (!CanTarget(resolution.ExecutionAbility, resolved.Target))
                    return CastRuntimeValidation.Fail(
                        "sticky-delivery-target-invalid");
            }
            if (!CanTarget(resolved.Ability, resolved.Target))
                return CastRuntimeValidation.Fail("carrier-target-invalid");
            return CastRuntimeValidation.Pass();
        }

        internal CastRuntimeValidation ValidateSource(CastStep step,
            bool validateTarget)
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
            if (validateTarget && !CanTarget(resolved.Ability,
                    resolved.Target))
                return CastRuntimeValidation.Fail("target-invalid");
            return CastRuntimeValidation.Pass();
        }

        public CastEnhancementPreparation PrepareEnhancements(CastStep step)
        {
            return new KingmakerCastEnhancementAdapter().Prepare(step);
        }

        public IAnimatedCastOperation StartAnimated(CastStep step)
        {
            ResolvedCast resolved;
            string reason;
            if (!TryResolve(step, out resolved, out reason))
                throw new InvalidOperationException(reason);
            AbilityEffectStickyTouch sticky = resolved.Ability.Blueprint == null
                ? null : resolved.Ability.Blueprint.StickyTouch;
            BlueprintAbility delivery = sticky == null
                ? null : sticky.TouchDeliveryAbility;
            if (sticky != null &&
                KingmakerStickyTouchCastAdapter.HasHeldTouch(
                    resolved.Caster, null))
                throw new InvalidOperationException(
                    "sticky-touch-held-charge-already-active");
            UnitCommand command = UnitUseAbility.CreateCastCommand(resolved.Ability, resolved.Target);
            if (command == null) throw new InvalidOperationException("Kingmaker returned no cast command.");
            int availableBefore = SafeAvailableCount(resolved.Ability);
            UnitCommand previousCommand = resolved.Caster.Commands.PreviousCommand;
            resolved.Caster.Commands.AddToQueue(command);
            return new KingmakerAnimatedOperation(command, step,
                resolved.Caster, resolved.Target, resolved.Ability,
                delivery, previousCommand, availableBefore);
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
            return ResolveAbility(caster, step.Provider, step.Reservation.TokenIds);
        }

        internal static AbilityData ResolveAbility(UnitEntityData caster, ProviderKey provider)
        {
            return ResolveAbility(caster, provider, null);
        }

        private static AbilityData ResolveAbility(
            UnitEntityData caster,
            ProviderKey provider,
            IReadOnlyList<string> reservedTokenIds)
        {
            if (provider.Ability.SourceKind == SourceKind.Spellbook)
            {
                List<Spellbook> ownedBooks = caster.Descriptor.Spellbooks.Where(b => b != null &&
                    b.Blueprint != null).ToList();
                Spellbook book = ownedBooks.FirstOrDefault(b => b != null &&
                    b.Blueprint != null && b.Blueprint.AssetGuid == provider.SpellbookGuid);
                if (book == null || !new KingmakerSpellbookRoleAdapter().IsIncluded(book, ownedBooks))
                    return null;
                if (reservedTokenIds != null && reservedTokenIds.Count != 0)
                {
                    foreach (SpellSlot slot in book.GetAllMemorizedSpells().Where(s => s != null &&
                        s.Spell != null && s.Available && s.IsMainSlot &&
                        reservedTokenIds.Contains(SlotId(s))))
                    {
                        AbilityData match = KingmakerAbilityVariants.Resolve(
                            slot.Spell, provider.Ability);
                        if (match != null) return match;
                    }
                    return null;
                }
                foreach (SpellSlot slot in book.GetAllMemorizedSpells().Where(s => s != null &&
                    s.Spell != null && s.IsMainSlot))
                {
                    AbilityData match = KingmakerAbilityVariants.Resolve(
                        slot.Spell, provider.Ability);
                    if (match != null) return match;
                }
                AbilityData known = ResolveSource(
                    book.GetAllKnownSpells(), provider);
                if (known != null) return known;
                for (int level = 0; level <= book.MaxSpellLevel; level++)
                {
                    AbilityData custom = ResolveSource(
                        book.GetCustomSpells(level), provider);
                    if (custom != null) return custom;
                }
                return null;
            }
            if (provider.Ability.SourceKind == SourceKind.AbilityResource ||
                provider.Ability.SourceKind == SourceKind.Fact)
            {
                foreach (Ability fact in caster.Descriptor.Abilities.Enumerable
                    .Where(value => value != null && value.Data != null))
                {
                    AbilityData match = KingmakerAbilityVariants.Resolve(
                        fact.Data, provider.Ability);
                    if (match != null) return match;
                }
                return null;
            }
            return null;
        }

        private static bool SourceInstanceMatches(AbilityData data, string sourceInstance)
        {
            int heighten = data.MetamagicData == null ? 0 : data.MetamagicData.HeightenLevel;
            return sourceInstance == "level-" + data.SpellLevel + "|heighten-" + heighten;
        }

        private static AbilityData ResolveSource(
            IEnumerable<AbilityData> sources, ProviderKey provider)
        {
            foreach (AbilityData source in sources ?? new AbilityData[0])
            {
                AbilityData match = KingmakerAbilityVariants.Resolve(
                    source, provider.Ability);
                if (match != null &&
                    SourceInstanceMatches(match, provider.SourceInstanceId))
                    return match;
            }
            return null;
        }

        internal static Dictionary<string, UnitEntityData> CollectUnits()
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

        internal static bool CanTarget(AbilityData ability, TargetWrapper target)
        {
            try { return ability != null && target != null && ability.CanTarget(target); }
            catch (Exception) { return false; }
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
            private readonly UnitCommand _carrierCommand;
            private readonly CastStep _step;
            private readonly UnitEntityData _caster;
            private readonly TargetWrapper _target;
            private readonly AbilityData _sourceAbility;
            private readonly BlueprintAbility _deliveryBlueprint;
            private readonly UnitCommand _previousCommandAtStart;
            private readonly int _availableBefore;
            private readonly AnimatedStickyTouchLifecycle _stickyLifecycle;
            private int _postCompletionFrames;
            private int _pollFrames;
            private bool? _observed;
            private bool _timedOut;
            private bool _disposed;
            private UnitUseAbility _deliveryCommand;
            private AnimatedStickyTouchLifecycleDecision _stickyDecision;

            internal KingmakerAnimatedOperation(UnitCommand command, CastStep step,
                UnitEntityData caster, TargetWrapper target,
                AbilityData sourceAbility,
                BlueprintAbility deliveryBlueprint,
                UnitCommand previousCommandAtStart, int availableBefore)
            {
                _carrierCommand = command;
                _step = step;
                _caster = caster;
                _target = target;
                _sourceAbility = sourceAbility;
                _deliveryBlueprint = deliveryBlueprint;
                _previousCommandAtStart = previousCommandAtStart;
                _availableBefore = availableBefore;
                if (_deliveryBlueprint != null)
                    _stickyLifecycle = new AnimatedStickyTouchLifecycle(3600);
            }

            public bool IsCompleted
            {
                get
                {
                    if (_stickyLifecycle != null)
                    {
                        CaptureDeliveryCommand();
                        _stickyDecision = _stickyLifecycle.Observe(
                            StickySnapshot());
                        _timedOut = _stickyDecision.TimedOut;
                        return _stickyDecision.Complete;
                    }
                    if (++_pollFrames >= 3600 &&
                        !_carrierCommand.IsFinished)
                    {
                        _timedOut = true;
                        return true;
                    }
                    if (!_carrierCommand.IsFinished) return false;
                    if (_carrierCommand.Result !=
                        UnitCommand.ResultType.Success) return true;
                    if (EffectsObserved) return true;
                    return ++_postCompletionFrames >= 12;
                }
            }

            public bool IsStarted { get { return _carrierCommand.IsStarted; } }
            public bool TimedOut { get { return _timedOut; } }
            public bool Succeeded
            {
                get
                {
                    return _stickyLifecycle == null
                        ? _carrierCommand.Result ==
                            UnitCommand.ResultType.Success
                        : _stickyDecision != null &&
                            _stickyDecision.Succeeded;
                }
            }
            public bool ResourceSpent
            {
                get
                {
                    int after = SafeAvailableCount(_sourceAbility);
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
                        bool observed = _step.ExpectedRecipientUnitIds.All(
                            targetId =>
                            evaluator.EvaluateTyped(_step.ExpectedEffects, active.GetEffects(targetId), null).Kind ==
                                EffectPresenceKind.Complete);
                        if (observed) _observed = true;
                        return observed;
                    }
                    catch (Exception) { return false; }
                }
            }
            public bool HasResidualDeliveryState
            {
                get
                {
                    if (_deliveryBlueprint == null) return false;
                    bool held = KingmakerStickyTouchCastAdapter.HasHeldTouch(
                        _caster, _deliveryBlueprint);
                    UnitUseAbility active =
                        KingmakerStickyTouchCastAdapter.FindDeliveryCommand(
                            _caster, _deliveryBlueprint, null);
                    return held || active != null ||
                        (_deliveryCommand != null &&
                            !_deliveryCommand.IsFinished);
                }
            }
            public string Detail
            {
                get
                {
                    return "original-command-start:" +
                        _carrierCommand.IsStarted +
                        ";original-command-end:" +
                        _carrierCommand.IsFinished +
                        ";original-command-result:" +
                        _carrierCommand.Result +
                        ";delivery-command-identified:" +
                        (_deliveryCommand != null) +
                        ";delivery-command-start:" +
                        (_deliveryCommand != null &&
                            _deliveryCommand.IsStarted) +
                        ";delivery-command-end:" +
                        (_deliveryCommand != null &&
                            _deliveryCommand.IsFinished) +
                        ";delivery-command-result:" +
                        (_deliveryCommand == null ? "none" :
                            _deliveryCommand.Result.ToString()) +
                        ";held-touch:" +
                        (_deliveryBlueprint != null &&
                            KingmakerStickyTouchCastAdapter.HasHeldTouch(
                                _caster, _deliveryBlueprint)) +
                        ";lifecycle:" + (_stickyDecision == null
                            ? "ordinary-or-pending" :
                                _stickyDecision.Detail) +
                        ";timed-out:" + _timedOut +
                        ";available-before:" + _availableBefore +
                        ";available-after:" +
                        SafeAvailableCount(_sourceAbility) +
                        ";carrier-guid:" + (_sourceAbility.Blueprint == null
                            ? "none" : _sourceAbility.Blueprint.AssetGuid) +
                        ";delivery-guid:" + (_deliveryBlueprint == null
                            ? "none" : _deliveryBlueprint.AssetGuid) +
                        ";expected-effects:" + ExpectedEffectIds(_step.ExpectedEffects) +
                        ";targets:" + string.Join(",",
                            _step.ExpectedRecipientUnitIds.ToArray()) +
                        ";effects-observed:" + EffectsObserved;
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                CaptureDeliveryCommand();
                InterruptIfRunning(_deliveryCommand);
                InterruptIfRunning(_carrierCommand);
                if (_deliveryBlueprint != null)
                    KingmakerStickyTouchCastAdapter.RemoveHeldTouch(
                        _caster, _deliveryBlueprint);
            }

            private AnimatedStickyTouchLifecycleSnapshot StickySnapshot()
            {
                bool selfTarget = _target != null &&
                    _target.Unit == _caster;
                return new AnimatedStickyTouchLifecycleSnapshot(
                    _carrierCommand.IsFinished,
                    _carrierCommand.IsFinished &&
                        _carrierCommand.Result ==
                            UnitCommand.ResultType.Success,
                    !selfTarget,
                    _deliveryCommand != null,
                    _deliveryCommand != null &&
                        _deliveryCommand.IsFinished,
                    _deliveryCommand != null &&
                        _deliveryCommand.IsFinished &&
                        _deliveryCommand.Result ==
                            UnitCommand.ResultType.Success,
                    KingmakerStickyTouchCastAdapter.HasHeldTouch(
                        _caster, _deliveryBlueprint),
                    EffectsObserved);
            }

            private void CaptureDeliveryCommand()
            {
                if (_deliveryCommand != null ||
                    _deliveryBlueprint == null) return;
                UnitUseAbility candidate =
                    KingmakerStickyTouchCastAdapter.FindDeliveryCommand(
                        _caster, _deliveryBlueprint, _target, true);
                if (candidate != null && !object.ReferenceEquals(
                        candidate, _previousCommandAtStart))
                    _deliveryCommand = candidate;
            }

            private static void InterruptIfRunning(UnitCommand command)
            {
                if (command == null || command.IsFinished) return;
                try { command.Interrupt(true); }
                catch (Exception) { }
            }
        }
    }

}
