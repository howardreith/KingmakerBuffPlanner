using System;
using System.Linq;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Domain.Effects;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerBuffSourceDiscovery
    {
        private readonly EffectOverrideRegistry _overrides;
        private readonly NativeCandidateClassifier _classifier = new NativeCandidateClassifier();

        internal KingmakerBuffSourceDiscovery(EffectOverrideRegistry overrides)
        {
            _overrides = overrides ?? EffectOverrideRegistry.Empty();
        }

        internal bool TryDiscover(
            BlueprintAbility ability, out EffectExpression expression, out string reason)
        {
            if (ability == null) throw new ArgumentNullException("ability");
            DiscoveryScanResult scan = new ActionGraphScanner().Scan(
                new KingmakerActionGraphAdapter().Adapt(ability));
            EffectOverrideApplication applied = _overrides.Apply(ability.AssetGuid, scan.Expression);
            expression = applied.Expression;
            NativeEffectRecord[] effects = NativeCatalogExporter.GetEffects(expression);
            NativeCandidateAuditDecision decision = _classifier.Classify(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = ability.CanTargetSelf,
                CanTargetFriends = ability.CanTargetFriends,
                CanTargetEnemies = ability.CanTargetEnemies,
                CanTargetPoint = ability.CanTargetPoint,
                HasVariants = (ability.Variants ?? new BlueprintAbility[0]).Length != 0,
                IsStickyTouch = ability.StickyTouch != null,
                EffectOnAlly = ability.EffectOnAlly.ToString(),
                EffectOnEnemy = ability.EffectOnEnemy.ToString(),
                Range = ability.Range.ToString(),
                Effects = effects.Select(e => new NativeCandidateEffectFacts
                {
                    Kind = e.Kind,
                    Target = e.Target,
                    Harmful = e.Harmful,
                    SourceContract = e.SourceContract,
                    ActionPath = e.ActionPath
                }).ToArray(),
                DiagnosticContracts = scan.Diagnostics.Select(d =>
                    d.NodeIdentity + "|" + d.Detail).ToArray()
            });
            if (applied.Entry != null)
            {
                if (applied.Entry.Disposition == "exclude" ||
                    applied.Entry.Disposition == "unsupported-with-reason")
                {
                    reason = applied.Entry.Reason;
                    return false;
                }
                reason = applied.Entry.Reason;
                return EffectExpressionAnalysis.ContainsLeaf(expression);
            }
            reason = decision.Reason;
            return decision.Disposition == "include";
        }
    }
}
