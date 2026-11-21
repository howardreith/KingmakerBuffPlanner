using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    // Which units one provider may reach, which units are legal anchors, and
    // whom each anchor's cast is predicted to reach.
    internal sealed class ProviderTargetingDecision
    {
        internal string[] ReachableIds;
        internal string[] Anchors;
        internal Dictionary<string, IEnumerable<string>> RecipientIdsByAnchor;
        internal bool Party;
        internal bool AreaRecipients;
        internal bool UnsafeArea;
        internal bool InfusedPersonal;
        internal string TargetClass;
    }

    // The game-free targeting decisions of KingmakerProviderOptionBuilder
    // (review R1-F8). The builder supplies two live reads: the native
    // AbilityData.CanTarget verdict for a unit (false when either side is
    // unreadable) and the distance between two live units (null when
    // unreadable). Offline tests inject both, so the anchor, fallback and
    // recipient decisions they assert are these production decisions.
    // A point-capable allied burst (missing-beneficial-buffs §3.C) is an
    // AlliedAreaRecipients effect: its anchors are exactly the units native
    // CanTarget admits - with NO caster fallback - so a game that refuses a
    // unit centre leaves no anchor and the planner refuses the casting.
    internal static class ProviderTargetingPlanner
    {
        internal static ProviderTargetingDecision Decide(
            ProviderSnapshot provider,
            EffectExpression expression,
            IEnumerable<UnitSnapshot> snapshotUnits,
            bool casterReadable,
            bool infusedPersonal,
            AlliedAreaCoverage areaCoverage,
            Func<string, bool> canTarget,
            Func<string, string, float?> distance)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            Func<string, bool> legal = canTarget ?? (id => false);
            Func<string, string, float?> measure = distance ?? ((a, b) => null);
            UnitSnapshot[] units = (snapshotUnits ?? new UnitSnapshot[0]).Where(u => u.TargetValidation.Alive &&
                u.TargetValidation.Friendly && u.TargetValidation.Targetable).ToArray();
            string casterId = provider.Key.CasterUnitId;
            var decision = new ProviderTargetingDecision
            {
                RecipientIdsByAnchor = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal),
                Party = EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.Party),
                AreaRecipients = EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.AlliedAreaRecipients),
                UnsafeArea = EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.EnemyAreaRecipients) ||
                    EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.AmbiguousAreaRecipients),
                InfusedPersonal = infusedPersonal
            };
            IEnumerable<UnitSnapshot> reachable;
            string[] anchors;
            bool personal = EffectExpressionTargetAnalysis.ContainsOnly(expression, EffectTarget.Caster);
            if (personal && !infusedPersonal)
            {
                reachable = units.Where(u => u.UnitId == casterId);
                anchors = reachable.Select(u => u.UnitId).ToArray();
            }
            // Re-review: a party effect that also reaches pets is planned as
            // a party effect (every unit, pets included, from a legal
            // origin); only a pet-only effect is limited to the pet.
            else if (decision.Party)
            {
                reachable = units;
                anchors = LegalAnchorIds(units, legal);
                if (anchors.Length == 0 && units.Any(unit => unit.UnitId == casterId))
                    anchors = new[] { casterId };
                foreach (string anchor in anchors)
                    decision.RecipientIdsByAnchor.Add(anchor, units.Select(unit => unit.UnitId).ToArray());
            }
            else if (EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.Pet))
            {
                reachable = units.Where(u => u.IsPet && u.MasterUnitId == casterId);
                anchors = reachable.Select(u => u.UnitId).ToArray();
            }
            else if (decision.AreaRecipients && areaCoverage != null && casterReadable)
            {
                float radius = areaCoverage.Radius;
                anchors = LegalAnchorIds(units, legal);
                foreach (string anchorId in anchors)
                {
                    if (!measure(anchorId, anchorId).HasValue) continue;
                    string id = anchorId;
                    decision.RecipientIdsByAnchor.Add(anchorId, units.Where(unit =>
                    {
                        float? metres = measure(id, unit.UnitId);
                        return metres.HasValue && metres.Value <= radius + 0.01f;
                    }).Select(unit => unit.UnitId).ToArray());
                }
                reachable = units.Where(unit => decision.RecipientIdsByAnchor.Values
                    .Any(recipients => recipients.Contains(unit.UnitId)));
            }
            else if (decision.AreaRecipients || decision.UnsafeArea)
            {
                reachable = new UnitSnapshot[0];
                anchors = new string[0];
            }
            else
            {
                reachable = units.Where(u => legal(u.UnitId));
                anchors = reachable.Select(u => u.UnitId).ToArray();
            }
            decision.ReachableIds = reachable.Select(u => u.UnitId).Distinct(StringComparer.Ordinal).ToArray();
            decision.Anchors = anchors;
            decision.TargetClass = decision.Party ? "Party" : decision.AreaRecipients
                ? "AlliedAreaRecipients" : decision.UnsafeArea
                    ? "UnsafeArea" : infusedPersonal
                        ? "NativeAlchemistInfusion" : personal ? "Caster" : "Direct";
            return decision;
        }

        // The planning option for one provider and its targeting decision.
        internal static ProviderPlanningOption BuildOption(ProviderSnapshot provider,
            ProviderTargetingDecision decision, CastExecutionStrategy strategy, string strategyReason,
            IReadOnlyDictionary<string, bool> casterFacts = null,
            IReadOnlyDictionary<string, EffectExpression> effectsByTarget = null)
        {
            return new ProviderPlanningOption(provider, decision.ReachableIds, decision.Anchors,
                provider.EffectiveCasterLevel, provider.ExpectedDurationRounds, strategy, strategyReason,
                decision.RecipientIdsByAnchor, casterFacts, effectsByTarget);
        }

        private static string[] LegalAnchorIds(IEnumerable<UnitSnapshot> units, Func<string, bool> canTarget)
        {
            return (units ?? new UnitSnapshot[0]).Where(unit => canTarget(unit.UnitId))
                .Select(unit => unit.UnitId).Distinct(StringComparer.Ordinal)
                .OrderBy(unitId => unitId, StringComparer.Ordinal).ToArray();
        }
    }
}
