using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerInstantCastAdapter : IInstantCastRuntimeAdapter, ICastEnhancementRuntimeAdapter
    {
        private readonly Dictionary<CastStep, StickyTouchTransaction> _transactions =
            new Dictionary<CastStep, StickyTouchTransaction>();

        public bool IsInCombat
        {
            get { return Game.Instance != null && Game.Instance.Player != null && Game.Instance.Player.IsInCombat; }
        }

        public CastRuntimeValidation Validate(CastStep step)
        {
            CastRuntimeValidation common = new KingmakerAnimatedCastAdapter()
                .ValidateSource(step, false);
            if (!common.Valid) return common;
            KingmakerAnimatedCastAdapter.ResolvedCast resolved;
            string reason;
            if (!KingmakerAnimatedCastAdapter.TryResolve(
                    step, out resolved, out reason))
                return CastRuntimeValidation.Fail(reason);
            if (step.ExecutionStrategy ==
                CastExecutionStrategy.StickyTouchDeliveryRuleCast)
            {
                StickyTouchCastResolution sticky;
                if (!KingmakerStickyTouchCastAdapter.TryCreateDelivery(
                        resolved.Ability, out sticky, out reason))
                    return CastRuntimeValidation.Fail(
                        "sticky-delivery-resolution:" + reason);
                if (!KingmakerAnimatedCastAdapter.CanTarget(
                        sticky.ExecutionAbility, resolved.Target))
                    return CastRuntimeValidation.Fail(
                        "sticky-delivery-target-invalid");
                if (KingmakerStickyTouchCastAdapter.HasHeldTouch(
                        resolved.Caster, null))
                    return CastRuntimeValidation.Fail(
                        "sticky-touch-held-charge-already-active");
                if (KingmakerStickyTouchCastAdapter.FindDeliveryCommand(
                        resolved.Caster, sticky.DeliveryBlueprint,
                        null) != null)
                    return CastRuntimeValidation.Fail(
                        "sticky-delivery-command-already-active");
                return CastRuntimeValidation.Pass();
            }
            if (resolved.Ability.Blueprint != null &&
                resolved.Ability.Blueprint.StickyTouch != null)
                return CastRuntimeValidation.Fail(
                    "sticky-touch-provider-not-classified-for-direct-delivery");
            return KingmakerAnimatedCastAdapter.CanTarget(
                resolved.Ability, resolved.Target)
                ? CastRuntimeValidation.Pass()
                : CastRuntimeValidation.Fail("target-invalid");
        }

        public CastEnhancementPreparation PrepareEnhancements(CastStep step)
        {
            return new KingmakerCastEnhancementAdapter().Prepare(step);
        }

        public InstantCastResult Fire(CastStep step)
        {
            CastRuntimeValidation finalValidation = Validate(step);
            if (!finalValidation.Valid)
                return new InstantCastResult(false, false, false, false, false,
                    "final-validation:" + finalValidation.Reason);
            KingmakerAnimatedCastAdapter.ResolvedCast resolved;
            string reason;
            if (!KingmakerAnimatedCastAdapter.TryResolve(step, out resolved, out reason))
                return new InstantCastResult(false, false, false, false, false,
                    "final-resolution:" + reason);
            AbilityData sourceAbility = resolved.Ability;
            AbilityData executionAbility = sourceAbility;
            StickyTouchCastResolution stickyResolution = null;
            StickyTouchTransaction transaction = null;
            if (step.ExecutionStrategy ==
                CastExecutionStrategy.StickyTouchDeliveryRuleCast)
            {
                if (!KingmakerStickyTouchCastAdapter.TryCreateDelivery(
                        sourceAbility, out stickyResolution, out reason))
                    return new InstantCastResult(false, false, false, false,
                        false, "sticky-delivery-resolution:" + reason);
                executionAbility = stickyResolution.ExecutionAbility;
                transaction = new StickyTouchTransaction(
                    resolved.Caster, resolved.Target, stickyResolution);
            }
            else if (sourceAbility.Blueprint != null &&
                sourceAbility.Blueprint.StickyTouch != null)
                return new InstantCastResult(false, false, false, false,
                    false, "sticky-touch-provider-was-not-classified-for-direct-delivery");

            int availableBefore = KingmakerAnimatedCastAdapter.SafeAvailableCount(
                sourceAbility);
            RuleCastSpell rule;
            try
            {
                rule = Rulebook.Trigger(new RuleCastSpell(
                    executionAbility, resolved.Target));
            }
            catch (Exception exception)
            {
                return new InstantCastResult(false, false, false, false,
                    false, "rule-cast-exception:" +
                    exception.GetType().FullName + ":" + exception.Message);
            }
            if (transaction != null) _transactions[step] = transaction;
            bool spendInvoked = false;
            Exception spendFailure = null;
            if (RuleCastSpendPolicy.ShouldInvokeSpend(true, rule.IsUMDFailed))
            {
                spendInvoked = true;
                try { sourceAbility.Spend(); }
                catch (Exception exception) { spendFailure = exception; }
            }
            int availableAfter = KingmakerAnimatedCastAdapter.SafeAvailableCount(
                sourceAbility);
            bool spent = availableBefore >= 0 && availableAfter >= 0 && availableAfter < availableBefore;
            bool observed = rule.Success && EffectsObserved(step);
            string carrierGuid = sourceAbility.Blueprint == null
                ? string.Empty : sourceAbility.Blueprint.AssetGuid;
            string deliveryGuid = stickyResolution == null
                ? string.Empty : stickyResolution.DeliveryBlueprint.AssetGuid;
            return new InstantCastResult(true,
                rule.Success && spendFailure == null, observed, spent,
                spendInvoked,
                "rule-success:" + rule.Success + ";umd-failed:" + rule.IsUMDFailed +
                ";spell-failed:" + rule.IsSpellFailed + ";spend-invoked:" + spendInvoked +
                ";spend-failure:" + (spendFailure == null
                    ? "none" : spendFailure.GetType().FullName + ":" +
                        spendFailure.Message) +
                ";spend-owner:source-ability-data" +
                ";available-before:" + availableBefore +
                ";available-after:" + availableAfter +
                ";strategy:" + step.ExecutionStrategy +
                ";strategy-reason:" + step.ExecutionStrategyReason +
                ";carrier-guid:" + carrierGuid +
                ";delivery-guid:" + deliveryGuid +
                ";source-ability-data:" +
                KingmakerStickyTouchCastAdapter.Identity(sourceAbility) +
                ";execution-ability-data:" +
                KingmakerStickyTouchCastAdapter.Identity(executionAbility) +
                ";rule-cast-submitted:true;expected-effects:" +
                KingmakerAnimatedCastAdapter.ExpectedEffectIds(step.ExpectedEffects) + ";targets:" +
                string.Join(",", step.ExpectedRecipientUnitIds.ToArray()) +
                ";carrier-command-created:false;delivery-command-created:false" +
                ";effects-observed:" + observed);
        }

        public bool EffectsObserved(CastStep step)
        {
            try
            {
                var active = new KingmakerActiveEffectSnapshotBuilder().Build();
                var evaluator = new EffectPresenceEvaluator();
                return step.ExpectedRecipientUnitIds.All(targetId =>
                    evaluator.EvaluateTyped(step.ExpectedEffects, active.GetEffects(targetId), null).Kind ==
                        EffectPresenceKind.Complete);
            }
            catch (Exception) { return false; }
        }

        public InstantCastCompletion InspectCompletion(CastStep step)
        {
            StickyTouchTransaction transaction;
            if (!_transactions.TryGetValue(step, out transaction))
                return InstantCastCompletion.Settled(
                    "ordinary-rule-cast-settled");
            bool heldTouch = KingmakerStickyTouchCastAdapter.HasHeldTouch(
                transaction.Caster,
                transaction.Resolution.DeliveryBlueprint);
            UnitUseAbility deliveryCommand =
                KingmakerStickyTouchCastAdapter.FindDeliveryCommand(
                    transaction.Caster,
                    transaction.Resolution.DeliveryBlueprint,
                    transaction.Target);
            bool residual = heldTouch || deliveryCommand != null;
            string detail = "held-touch:" + heldTouch +
                ";delivery-command-present:" + (deliveryCommand != null) +
                ";carrier-guid:" +
                transaction.Resolution.CarrierBlueprint.AssetGuid +
                ";delivery-guid:" +
                transaction.Resolution.DeliveryBlueprint.AssetGuid;
            if (residual) return InstantCastCompletion.Pending(detail);
            _transactions.Remove(step);
            return InstantCastCompletion.Settled(detail);
        }

        public InstantCastCompletion Cleanup(CastStep step)
        {
            StickyTouchTransaction transaction;
            if (!_transactions.TryGetValue(step, out transaction))
                return InstantCastCompletion.Settled(
                    "ordinary-rule-cast-no-cleanup-required");
            UnitUseAbility deliveryCommand =
                KingmakerStickyTouchCastAdapter.FindDeliveryCommand(
                    transaction.Caster,
                    transaction.Resolution.DeliveryBlueprint,
                    transaction.Target);
            bool interrupted = false;
            if (deliveryCommand != null && !deliveryCommand.IsFinished)
            {
                try
                {
                    deliveryCommand.Interrupt(true);
                    interrupted = true;
                }
                catch (Exception) { }
            }
            bool heldRemoved = KingmakerStickyTouchCastAdapter.RemoveHeldTouch(
                transaction.Caster,
                transaction.Resolution.DeliveryBlueprint);
            bool residual = KingmakerStickyTouchCastAdapter.HasHeldTouch(
                    transaction.Caster,
                    transaction.Resolution.DeliveryBlueprint) ||
                KingmakerStickyTouchCastAdapter.FindDeliveryCommand(
                    transaction.Caster,
                    transaction.Resolution.DeliveryBlueprint,
                    transaction.Target) != null;
            if (!residual) _transactions.Remove(step);
            string detail = "sticky-delivery-cleanup;command-interrupted:" +
                interrupted + ";held-touch-removed:" + heldRemoved +
                ";residual-delivery-state:" + residual;
            return residual ? InstantCastCompletion.Pending(detail) :
                InstantCastCompletion.Settled(detail);
        }

        private sealed class StickyTouchTransaction
        {
            internal StickyTouchTransaction(UnitEntityData caster,
                TargetWrapper target, StickyTouchCastResolution resolution)
            {
                Caster = caster;
                Target = target;
                Resolution = resolution;
            }

            internal UnitEntityData Caster;
            internal TargetWrapper Target;
            internal StickyTouchCastResolution Resolution;
        }
    }

    internal sealed class StickyTouchCastResolution
    {
        internal StickyTouchCastResolution(AbilityData sourceAbility,
            AbilityData executionAbility, BlueprintAbility carrierBlueprint,
            BlueprintAbility deliveryBlueprint)
        {
            SourceAbility = sourceAbility;
            ExecutionAbility = executionAbility;
            CarrierBlueprint = carrierBlueprint;
            DeliveryBlueprint = deliveryBlueprint;
        }

        internal AbilityData SourceAbility;
        internal AbilityData ExecutionAbility;
        internal BlueprintAbility CarrierBlueprint;
        internal BlueprintAbility DeliveryBlueprint;
    }

    internal static class KingmakerStickyTouchCastAdapter
    {
        internal static CastExecutionCapability Classify(BlueprintAbility carrier)
        {
            AbilityEffectStickyTouch sticky = carrier == null
                ? null : carrier.StickyTouch;
            BlueprintAbility delivery = sticky == null
                ? null : sticky.TouchDeliveryAbility;
            return StickyTouchExecutionClassifier.Classify(
                sticky != null,
                delivery != null,
                delivery != null &&
                    delivery.GetComponent<AbilityDeliverTouch>() != null,
                delivery != null && !delivery.CanTargetPoint &&
                    (delivery.CanTargetFriends ||
                        delivery.CanTargetEnemies),
                delivery != null && delivery.CanTargetSelf,
                delivery != null && delivery.CanTargetFriends,
                delivery != null && delivery.CanTargetEnemies,
                delivery != null && delivery.CanTargetPoint);
        }

        internal static bool TryCreateDelivery(AbilityData source,
            out StickyTouchCastResolution resolution, out string reason)
        {
            resolution = null;
            reason = string.Empty;
            if (source == null || source.Blueprint == null)
                return Fail("source-ability-data-missing", out reason);
            CastExecutionCapability capability = Classify(source.Blueprint);
            if (capability.Strategy !=
                    CastExecutionStrategy.StickyTouchDeliveryRuleCast)
                return Fail(capability.Reason, out reason);
            AbilityEffectStickyTouch sticky = source.Blueprint.StickyTouch;
            BlueprintAbility delivery = sticky.TouchDeliveryAbility;
            AbilityData execution;
            try
            {
                execution = new AbilityData(source, delivery)
                {
                    ConvertedFrom = source,
                    MetamagicData = source.MetamagicData,
                    OverrideDC = source.OverrideDC,
                    OverrideSpellLevel = source.OverrideSpellLevel,
                    ParamSpellbook = source.ParamSpellbook,
                    ParamSpellLevel = source.ParamSpellLevel,
                    ParamSpellSlot = source.ParamSpellSlot,
                    PotionForOther = source.PotionForOther,
                    SpellSource = source.SpellSource
                };
            }
            catch (Exception exception)
            {
                return Fail("delivery-ability-data-construction-exception:" +
                    exception.GetType().Name, out reason);
            }
            if (execution.Blueprint != delivery || execution.Caster !=
                    source.Caster || execution.ConvertedFrom != source ||
                execution.Fact != source.Fact || execution.Spellbook !=
                    source.Spellbook || execution.MetamagicData !=
                    source.MetamagicData)
                return Fail("delivery-ability-data-context-mismatch", out reason);
            if (execution.TargetAnchor != AbilityTargetAnchor.Unit)
                return Fail("delivery-ability-data-target-anchor-not-unit",
                    out reason);
            resolution = new StickyTouchCastResolution(source, execution,
                source.Blueprint, delivery);
            return true;
        }

        internal static bool HasHeldTouch(UnitEntityData caster,
            BlueprintAbility delivery)
        {
            if (caster == null || caster.Descriptor == null) return false;
            UnitPartTouch touch = caster.Descriptor.Get<UnitPartTouch>();
            return touch != null && touch.Ability != null &&
                (delivery == null || touch.Ability.Blueprint == delivery);
        }

        internal static UnitUseAbility FindDeliveryCommand(
            UnitEntityData caster, BlueprintAbility delivery,
            TargetWrapper target, bool includeFinished = false)
        {
            if (caster == null || caster.Commands == null || delivery == null)
                return null;
            foreach (UnitCommand command in caster.Commands)
            {
                UnitUseAbility match = MatchDeliveryCommand(
                    command, delivery, target);
                if (match != null && (includeFinished || !match.IsFinished))
                    return match;
            }
            UnitUseAbility previous = MatchDeliveryCommand(
                caster.Commands.PreviousCommand, delivery, target);
            return previous != null && (includeFinished ||
                !previous.IsFinished) ? previous : null;
        }

        internal static bool RemoveHeldTouch(UnitEntityData caster,
            BlueprintAbility delivery)
        {
            if (!HasHeldTouch(caster, delivery)) return true;
            try { caster.Descriptor.Remove<UnitPartTouch>(); }
            catch (Exception) { return false; }
            return !HasHeldTouch(caster, delivery);
        }

        internal static string Identity(AbilityData ability)
        {
            return ability == null ? "none" :
                RuntimeHelpers.GetHashCode(ability).ToString("x8") + "@" +
                (ability.Blueprint == null
                    ? "no-blueprint" : ability.Blueprint.AssetGuid);
        }

        private static UnitUseAbility MatchDeliveryCommand(
            UnitCommand command, BlueprintAbility delivery,
            TargetWrapper target)
        {
            UnitUseAbility use = command as UnitUseAbility;
            if (use == null || use.Spell == null ||
                use.Spell.Blueprint != delivery) return null;
            if (target == null || target.Unit == null) return use;
            return use.Target != null && use.Target.Unit == target.Unit
                ? use : null;
        }

        private static bool Fail(string value, out string reason)
        {
            reason = value;
            return false;
        }
    }
}
