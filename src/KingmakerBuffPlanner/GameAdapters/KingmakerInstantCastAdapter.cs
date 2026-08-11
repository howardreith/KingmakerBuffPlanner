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
    internal sealed class KingmakerInstantCastAdapter : IInstantCastRuntimeAdapter
    {
        public bool IsInCombat
        {
            get { return Game.Instance != null && Game.Instance.Player != null && Game.Instance.Player.IsInCombat; }
        }

        public CastRuntimeValidation Validate(CastStep step)
        {
            return new KingmakerAnimatedCastAdapter().Validate(step);
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
            var rule = Rulebook.Trigger(new RuleCastSpell(resolved.Ability, resolved.Target));
            bool spent = false;
            if (!rule.IsUMDFailed)
            {
                resolved.Ability.Spend();
                spent = true;
            }
            bool observed = false;
            if (rule.Success)
            {
                try
                {
                    var active = new KingmakerActiveEffectSnapshotBuilder().Build();
                    var evaluator = new EffectPresenceEvaluator();
                    observed = step.TargetUnitIds.All(targetId =>
                        evaluator.EvaluateTyped(step.ExpectedEffects, active.GetEffects(targetId), null).Kind ==
                            EffectPresenceKind.Complete);
                }
                catch (Exception) { observed = false; }
            }
            return new InstantCastResult(true, rule.Success, observed, spent,
                "rule-success:" + rule.Success + ";umd-failed:" + rule.IsUMDFailed +
                ";spell-failed:" + rule.IsSpellFailed + ";effects-observed:" + observed);
        }
    }
}
