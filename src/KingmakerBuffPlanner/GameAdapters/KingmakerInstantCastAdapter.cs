using System;
using System.Linq;
using Kingmaker;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerInstantCastAdapter : IInstantCastRuntimeAdapter, ICastEnhancementRuntimeAdapter
    {
        public bool IsInCombat
        {
            get { return Game.Instance != null && Game.Instance.Player != null && Game.Instance.Player.IsInCombat; }
        }

        public CastRuntimeValidation Validate(CastStep step)
        {
            return new KingmakerAnimatedCastAdapter().Validate(step);
        }

        public CastEnhancementPreparation PrepareEnhancements(CastStep step)
        {
            return new KingmakerCastEnhancementAdapter().Prepare(step);
        }

        public InstantCastResult Fire(CastStep step)
        {
            CastRuntimeValidation finalValidation = Validate(step);
            if (!finalValidation.Valid)
                return new InstantCastResult(false, false, false, false,
                    "final-validation:" + finalValidation.Reason);
            KingmakerAnimatedCastAdapter.ResolvedCast resolved;
            string reason;
            if (!KingmakerAnimatedCastAdapter.TryResolve(step, out resolved, out reason))
                return new InstantCastResult(false, false, false, false, "final-resolution:" + reason);
            int availableBefore = KingmakerAnimatedCastAdapter.SafeAvailableCount(resolved.Ability);
            var rule = Rulebook.Trigger(new RuleCastSpell(resolved.Ability, resolved.Target));
            bool spendInvoked = false;
            if (!rule.IsUMDFailed)
            {
                resolved.Ability.Spend();
                spendInvoked = true;
            }
            int availableAfter = KingmakerAnimatedCastAdapter.SafeAvailableCount(resolved.Ability);
            bool spent = availableBefore >= 0 && availableAfter >= 0 && availableAfter < availableBefore;
            bool observed = rule.Success && EffectsObserved(step);
            return new InstantCastResult(true, rule.Success, observed, spent,
                "rule-success:" + rule.Success + ";umd-failed:" + rule.IsUMDFailed +
                ";spell-failed:" + rule.IsSpellFailed + ";spend-invoked:" + spendInvoked +
                ";available-before:" + availableBefore +
                ";available-after:" + availableAfter + ";expected-effects:" +
                KingmakerAnimatedCastAdapter.ExpectedEffectIds(step.ExpectedEffects) + ";targets:" +
                string.Join(",", step.TargetUnitIds.ToArray()) + ";effects-observed:" + observed);
        }

        public bool EffectsObserved(CastStep step)
        {
            try
            {
                var active = new KingmakerActiveEffectSnapshotBuilder().Build();
                var evaluator = new EffectPresenceEvaluator();
                return step.TargetUnitIds.All(targetId =>
                    evaluator.EvaluateTyped(step.ExpectedEffects, active.GetEffects(targetId), null).Kind ==
                        EffectPresenceKind.Complete);
            }
            catch (Exception) { return false; }
        }
    }
}
