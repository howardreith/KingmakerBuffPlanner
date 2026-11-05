using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Execution
{
    public static partial class CastingQualificationRecipe
    {
        public const string HeroismSuccessor = "heroism-successor";
        // Diagnostic recipe identities only. Discovery and confirmation remain structural.
        internal const string HeroismGuid = "5ab0d42fb68c9e34abae4921822b9d63";
        internal const string MirrorImageGuid = "3e4ab69ada402d145a5e0ad3ad4b8564";

        private static CastingQualificationSelection SelectHeroismSuccessor(
            CastingWorkspaceInputs inputs, string campaignId)
        {
            if (inputs == null) throw new ArgumentNullException("inputs");
            var rejections = new List<string>();
            int considered = 0;
            var options = inputs.ProviderOptions.Where(value => value != null && value.Provider != null &&
                value.Provider.Key.Ability.SourceKind == SourceKind.Spellbook &&
                value.Provider.Key.Ability.MetamagicMask == 0 &&
                value.ExecutionStrategy == CastExecutionStrategy.DirectRuleCast)
                .OrderBy(value => value.Provider.Key.Canonical, StringComparer.Ordinal).ToList();
            foreach (ProviderPlanningOption heroism in options.Where(value =>
                value.Provider.Key.Ability.BaseAbilityGuid == HeroismGuid))
            foreach (ProviderPlanningOption successor in options.Where(value =>
                value.Provider.Key.Ability.BaseAbilityGuid == MirrorImageGuid ||
                string.Equals(value.Provider.DisplayName, "Heightened Awareness", StringComparison.Ordinal)))
            {
                considered++;
                string key = heroism.Provider.Key.Canonical + ">" + successor.Provider.Key.Canonical;
                if (CastsAvailable(inputs, heroism.Provider, 2) < 2 ||
                    CastsAvailable(inputs, successor.Provider, 3) < 3)
                { RejectHeroism(rejections, key + "|requires-two-heroism-and-three-successor-casts"); continue; }
                string source = SingleCastProbeSelector.SourceIdFor(inputs.EffectsBySource, heroism.Provider.Key.Ability);
                EffectExpression raw = DiscoveredEffectLookup.ForAbility(inputs.EffectsBySource,
                    heroism.Provider.Key.Ability, source);
                string successorSource = SingleCastProbeSelector.SourceIdFor(inputs.EffectsBySource, successor.Provider.Key.Ability);
                EffectExpression next = DiscoveredEffectLookup.ForAbility(inputs.EffectsBySource,
                    successor.Provider.Key.Ability, successorSource);
                if (raw == null || next == null || EffectConditionBinding.FactIds(raw).Any(id =>
                    heroism.CasterFacts == null || !heroism.CasterFacts.ContainsKey(id)))
                { RejectHeroism(rejections, key + "|missing-discovery-or-caster-facts"); continue; }
                var legal = new HashSet<string>(inputs.Snapshot.Units.Where(unit =>
                    unit.TargetValidation.Alive && unit.TargetValidation.Conscious &&
                    unit.TargetValidation.Friendly && unit.TargetValidation.Targetable).Select(unit => unit.UnitId), StringComparer.Ordinal);
                foreach (string target in heroism.ReachableTargetIds.Where(id => legal.Contains(id) &&
                    id != heroism.Provider.Key.CasterUnitId).OrderBy(id => id, StringComparer.Ordinal))
                foreach (string nextTarget in successor.ReachableTargetIds.Where(legal.Contains)
                    .OrderBy(id => id == successor.Provider.Key.CasterUnitId ? 0 : 1).ThenBy(id => id, StringComparer.Ordinal))
                {
                    var castings = new[]
                    {
                        HeroismCasting(0, source, heroism, target),
                        HeroismCasting(1, successorSource, successor, nextTarget)
                    };
                    var selection = new CastingQualificationSelection(null, source, heroism.Provider.Key.Ability,
                        castings, considered, rejections, HeroismSuccessor,
                        new[] { "real-heroism", "distinct-successor", "different-caster-recipient", "repeat", "skip-active" });
                    IReadOnlyList<CastingQualificationStepForecast> forecast = CastingQualificationForecast.ForecastHeroism(selection, inputs, campaignId);
                    if (forecast.Any(step => step.Refusal != null) ||
                        !forecast.Select(step => step.CastingIds.Count).SequenceEqual(new[] { 2, 2, 1 }))
                    { RejectHeroism(rejections, key + "|forecast:" + string.Join("|", forecast.Select(step => step.Refusal ?? string.Join(",", step.CastingIds)).ToArray())); continue; }
                    selection.EffectContext = new JObject
                    {
                        { "heroismAbilityGuid", heroism.Provider.Key.Ability.BaseAbilityGuid },
                        { "successorAbilityGuid", successor.Provider.Key.Ability.BaseAbilityGuid },
                        { "successorName", successor.Provider.DisplayName },
                        { "casterUnitId", heroism.Provider.Key.CasterUnitId },
                        { "primaryRecipientUnitId", target },
                        { "casterFacts", JObject.FromObject(heroism.CasterFacts ?? new Dictionary<string, bool>()) },
                        { "discoveredExpression", JObject.FromObject(raw) },
                        { "boundExpression", JObject.FromObject(heroism.BindEffects(raw, target)) },
                        { "observationUnitIds", new JArray(forecast[0].Projection.Plan.Steps[0].EffectObservationUnitIds) }
                    };
                    return selection;
                }
            }
            return new CastingQualificationSelection("no-affordable-real-heroism-successor", null, null,
                null, considered, rejections, HeroismSuccessor);
        }

        private static void RejectHeroism(List<string> reasons, string value)
        { if (reasons.Count < MaximumRecordedRejections) reasons.Add(value); }

        private static PlannedCasting HeroismCasting(int order, string source, ProviderPlanningOption option, string target)
        {
            return new PlannedCasting(CastingIds[order], RoutineId, order, source, option.Provider.Key.Ability,
                option.Provider.Key.CasterUnitId, option.Provider.Key.SpellbookGuid, CastingTargetMode.DirectTarget,
                target, null, null, null, null, ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
        }
    }

    public static partial class CastingQualificationForecast
    {
        internal static IReadOnlyList<CastingQualificationStepForecast> ForecastHeroism(
            CastingQualificationSelection selection, CastingWorkspaceInputs inputs, string campaignId)
        {
            CastingPlanDocument document = BuildDocument(campaignId, selection.Castings);
            var first = Project("heroism-first", document, inputs, inputs.LiveEffects);
            var spent = first.Projection == null ? new ResourceReservation[0]
                : first.Projection.Plan.Steps.Select(step => step.Reservation).ToArray();
            var afterFirst = WithSpent(inputs, spent);
            afterFirst = WithLiveEffects(afterFirst, GrantHeroism(afterFirst, selection.Castings));
            foreach (PlannedCasting casting in selection.Castings)
                document = WithPolicy(document, casting.CastingId, ExistingEffectPolicy.Overwrite);
            var recast = Project("heroism-recast", document, afterFirst, afterFirst.LiveEffects);
            if (recast.Projection != null) spent = spent.Concat(recast.Projection.Plan.Steps.Select(step => step.Reservation)).ToArray();
            var afterRecast = WithSpent(inputs, spent);
            afterRecast = WithLiveEffects(afterRecast, GrantHeroism(afterRecast, selection.Castings));
            document = WithPolicy(document, selection.Castings[0].CastingId, ExistingEffectPolicy.SkipAlreadyActive);
            var skip = Project("heroism-skip", document, afterRecast, afterRecast.LiveEffects);
            return new ReadOnlyCollection<CastingQualificationStepForecast>(new[] { first, recast, skip });
        }

        private static ActiveEffectSnapshot GrantHeroism(CastingWorkspaceInputs inputs, IEnumerable<PlannedCasting> castings)
        {
            var instances = inputs.Snapshot.Units.ToDictionary(unit => unit.UnitId, unit =>
                inputs.LiveEffects == null ? new List<ActiveEffectInstance>() :
                inputs.LiveEffects.HasInstanceDetail ? inputs.LiveEffects.GetInstances(unit.UnitId).ToList() :
                inputs.LiveEffects.GetEffects(unit.UnitId).Select(marker =>
                    new ActiveEffectInstance(marker.Kind, marker.EffectId, null, null, null)).ToList(), StringComparer.Ordinal);
            foreach (PlannedCasting casting in castings)
            {
                ProviderPlanningOption option = inputs.ProviderOptions.First(value =>
                    value.Provider.Key.CasterUnitId == casting.CasterUnitId && value.Provider.Key.Ability.Canonical == casting.Ability.Canonical);
                EffectExpression expected = option.BindEffects(DiscoveredEffectLookup.ForAbility(inputs.EffectsBySource,
                    casting.Ability, casting.SourceId), casting.DirectTargetUnitId);
                var scope = new EffectRecipientScope(expected, casting.CasterUnitId, casting.DirectTargetUnitId);
                new EffectPresenceEvaluator().EvaluateScoped(expected, (leaf, role) =>
                {
                    List<ActiveEffectInstance> onUnit;
                    if (instances.TryGetValue(scope.Owner(leaf, role), out onUnit))
                        onUnit.Add(new ActiveEffectInstance(leaf.Kind, leaf.EffectId, null,
                            option.Provider.EffectiveCasterLevel, casting.Ability.MetamagicMask));
                    return true;
                }, null);
            }
            return ActiveEffectSnapshot.FromInstances(instances.ToDictionary(pair => pair.Key,
                pair => (IEnumerable<ActiveEffectInstance>)pair.Value, StringComparer.Ordinal));
        }
    }

    public sealed partial class CastingQualificationRecord
    {
        private static readonly string[] HeroismStepNames = { "heroism-first", "heroism-recast", "heroism-skip" };
        private string HeroismStepFailure(CastingQualificationStepResult step, IReadOnlyList<PlannedCasting> castings)
        {
            bool skip = step.Name == "heroism-skip";
            if (!HeroismStepNames.Contains(step.Name)) return "unknown-heroism-step";
            if (step.Report == null || step.Report.TerminalReason != "completed" || step.Report.Cancelled ||
                step.Report.CleanupFailures.Count != 0) return "report:" + (step.Report == null ? "none" : step.Report.TerminalReason);
            if (step.Report.Entries.Count != 2 || step.Report.Submitted != (skip ? 1 : 2) ||
                step.StateOf(castings[0].CastingId) != (skip ? CastingOutcomeState.Skipped : CastingOutcomeState.EffectConfirmed) ||
                step.StateOf(castings[1].CastingId) != CastingOutcomeState.EffectConfirmed) return "states:" + States(step);
            foreach (PlannedCasting casting in castings)
            {
                string transition = step.TransitionOf(casting.CastingId);
                if (skip && casting == castings[0] ? transition != "unchanged" :
                    transition != "new-instance" && transition != "refreshed") return "effects:" + casting.CastingId + "=" + transition;
            }
            return ResourceFailure(step, castings);
        }
    }

    public sealed partial class CastingQualificationDriver
    {
        private void HeroismEdit(bool skip)
        {
            foreach (PlannedCasting casting in _session.Document.Castings.ToArray())
            {
                _session.FocusCasting(casting.CastingId);
                var updated = _session.UpdateFocusedCasting(new PlannedCasting(casting.CastingId, casting.RoutineId,
                    casting.Order, casting.SourceId, casting.Ability, casting.CasterUnitId, casting.SpellbookGuid,
                    casting.TargetMode, casting.DirectTargetUnitId, casting.Origin, casting.RequiredCoverageUnitIds,
                    casting.TargetingModifiers, casting.Enhancements,
                    skip && casting.CastingId == Record.Selection.Castings[0].CastingId ? ExistingEffectPolicy.SkipAlreadyActive : ExistingEffectPolicy.Overwrite,
                    casting.IgnoredPresenceMarkers, casting.State, casting.Provenance));
                if (!updated.Applied) { Fail("heroism-policy-refused:" + updated.Reason); return; }
            }
            _session.Save();
            CastingWorkspaceInputs inputs = _freshInputs();
            _session.PresentForReview(inputs);
            if (!_session.AcceptPresentedPlan(inputs)) { Fail("heroism-accept-refused"); return; }
            _phase = skip ? "heroism-skip" : "heroism-recast";
        }
    }
}
