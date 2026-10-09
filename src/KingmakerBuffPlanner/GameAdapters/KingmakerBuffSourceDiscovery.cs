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
            // Every ability reaching live discovery belongs to a party member,
            // so it is reachable; the facts are the catalogue export's own.
            NativeCandidateAuditDecision decision = _classifier.Classify(NativeCatalogExporter.AuditFacts(
                ability, true, NativeCatalogExporter.GetEffects(expression), scan.Diagnostics));
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
