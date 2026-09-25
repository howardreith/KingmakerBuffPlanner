using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    // The casting-graph workspace (addendum v1.1): buff -> caster/source ->
    // target creates one casting, drawn as one connection; the casting's
    // inspector edits only that casting. Selection and focus never mutate
    // the document; every mutation goes through the authoring service with
    // its disclosed scope and Undo. Capacity comes from the compiled plans'
    // own ledgers - the graph keeps no counter of its own.
    public sealed partial class CastingWorkspaceSession
    {
        // ------------------------------------------------------------------
        // Focus (never a mutation)
        // ------------------------------------------------------------------

        // Chooses the buff the graph shows. The chosen caster stays chosen
        // when it can cast the new buff too; its source is re-chosen only
        // when it has exactly one way to cast it.
        public void SelectGraphBuff(string sourceId, CastingWorkspaceInputs inputs = null)
        {
            if (inputs != null) _lastInputs = inputs;
            string previousCaster = Draft.CasterUnitId ?? SelectedCasterUnitId;
            bool changed = !string.Equals(sourceId ?? string.Empty, SelectedSourceId ?? string.Empty,
                StringComparison.Ordinal);
            SelectBuff(sourceId);
            Draft.SourceId = SelectedSourceId;
            EditingFocusCastingId = null;
            if (!changed) return;
            Draft.Ability = null;
            Draft.SpellbookGuid = null;
            _resolvedDraftKey = string.Empty;
            if (previousCaster != null && _lastInputs != null &&
                CapableCasterUnitIdsForSource(_lastInputs, SelectedSourceId).Contains(previousCaster))
                SelectGraphCaster(previousCaster);
            else
            {
                SelectCaster(null);
                Draft.CasterUnitId = null;
            }
        }

        // Chooses who casts the next casting. A caster with exactly one way to
        // cast the selected buff has that source chosen with it; with several,
        // the exact source must be chosen before a casting can be added.
        public void SelectGraphCaster(string casterUnitId, CastingWorkspaceInputs inputs = null)
        {
            if (inputs != null) _lastInputs = inputs;
            EditingFocusCastingId = null;
            ChooseDraftCaster(casterUnitId);
            Draft.Ability = null;
            Draft.SpellbookGuid = null;
            _resolvedDraftKey = string.Empty;
            if (_lastInputs == null || string.IsNullOrEmpty(casterUnitId)) return;
            List<ProviderPlanningOption> sources = SourceOptionsFor(_lastInputs,
                SelectedSourceId, casterUnitId);
            if (sources.Count == 1 && TwinCount(_lastInputs, sources[0].Provider.Key) == 1)
            {
                Draft.Ability = sources[0].Provider.Key.Ability;
                Draft.SpellbookGuid = sources[0].Provider.Key.SpellbookGuid;
                _resolvedDraftKey = DraftResolutionKey();
            }
        }

        // Chooses the exact source (spellbook level, item or ability) of the
        // next casting, and its caster with it.
        public AuthoringEditResult SelectGraphSource(string providerKey,
            CastingWorkspaceInputs inputs = null)
        {
            AuthoringEditResult result = ChooseDraftProvider(providerKey, inputs);
            if (result.Applied) EditingFocusCastingId = null;
            return result;
        }

        // Focuses one casting and shows it where it lives (its buff and its
        // routine). Only the focus changes.
        public void FocusGraphCasting(string castingId)
        {
            FocusCasting(castingId);
            PlannedCasting casting = FocusedCasting();
            if (casting == null) return;
            if (!string.Equals(SelectedSourceId, casting.SourceId, StringComparison.Ordinal))
            {
                SelectBuff(casting.SourceId);
                Draft.SourceId = casting.SourceId;
            }
            if (!string.Equals(SelectedRoutineId, casting.RoutineId, StringComparison.Ordinal))
                SelectRoutine(casting.RoutineId);
        }

        public void ClearGraphFocus()
        {
            EditingFocusCastingId = null;
        }

        // ------------------------------------------------------------------
        // Authoring (explicit, undoable, disclosed scope)
        // ------------------------------------------------------------------

        // The graph's primary gesture: with a buff and an exact source
        // chosen, clicking a legal target adds ONE casting from that source
        // to that target (a group ability: one casting centred there) and
        // focuses it; sibling castings are untouched. A target that already
        // has a casting of this buff in this routine is never silently
        // stolen or doubled: its casting is shown instead.
        public CastingGraphEditResult AddGraphCasting(string unitId,
            CastingWorkspaceInputs inputs = null)
        {
            if (inputs != null) _lastInputs = inputs;
            if (_lastInputs == null)
                return GraphRefusal("draft-ability-unresolved:no-discovery-inputs");
            string source = SelectedSourceId;
            if (string.IsNullOrEmpty(source)) return GraphRefusal("no-buff-selected");
            string caster = Draft.CasterUnitId ?? SelectedCasterUnitId;
            if (string.IsNullOrEmpty(caster))
                return GraphRefusal("draft-ability-unresolved:no-caster-selected");
            List<ProviderPlanningOption> sources = SourceOptionsFor(_lastInputs, source, caster);
            if (sources.Count == 0)
                return GraphRefusal("draft-ability-unresolved:no-provider-for-source-and-caster");
            ProviderPlanningOption option = Draft.Ability == null ? null : sources.FirstOrDefault(value =>
                string.Equals(value.Provider.Key.Ability.Canonical, Draft.Ability.Canonical,
                    StringComparison.Ordinal) &&
                string.Equals(NullIfEmpty(value.Provider.Key.SpellbookGuid),
                    NullIfEmpty(Draft.SpellbookGuid), StringComparison.Ordinal));
            if (option == null)
                return GraphRefusal(sources.Count > 1
                    ? "draft-ability-unresolved:exact-source-ambiguous:" + sources.Count
                    : "draft-ability-unresolved:no-provider-for-source-and-caster");
            if (TwinCount(_lastInputs, option.Provider.Key) > 1)
                return GraphRefusal("provider-not-pinnable:" + option.Provider.Key.Canonical);
            UnitSnapshot unit = _lastInputs.Snapshot.Units.FirstOrDefault(value =>
                string.Equals(value.UnitId, unitId, StringComparison.Ordinal));
            if (unit == null) return GraphRefusal("target-not-in-party:" + unitId);
            bool? group = IsGroupSource(_lastInputs, source);
            if (group == null) return GraphRefusal("ability-targeting-unsupported:" + source);
            PlannedCasting existing = ExistingCastingFor(source, SelectedRoutineId, unitId, group.Value);
            if (existing != null)
            {
                EditingFocusCastingId = existing.CastingId;
                return new CastingGraphEditResult(AuthoringEditResult.Refuse(
                    "target-already-has-casting:" + existing.CastingId), existing.CastingId, true);
            }
            PlannedCasting casting;
            string castingId = NextCastingId();
            if (!group.Value)
            {
                if (!option.ReachableTargetIds.Contains(unitId))
                    return GraphRefusal("target-unreachable:" + unitId);
                string invalid = InvalidTargetReason(unit);
                if (invalid != null) return GraphRefusal(invalid + ":" + unitId);
                casting = new PlannedCasting(castingId, SelectedRoutineId, 0, source,
                    option.Provider.Key.Ability, caster, NullIfEmpty(option.Provider.Key.SpellbookGuid),
                    CastingTargetMode.DirectTarget, unitId, null, null, null, null,
                    Draft.ExistingEffectPolicy, null, CastingAuthoringState.Ready, null);
            }
            else
            {
                CastingTargetMode mode;
                CastingOrigin origin;
                if (string.Equals(unitId, caster, StringComparison.Ordinal) &&
                    option.LegalAnchorIds.Contains(caster))
                {
                    mode = CastingTargetMode.CasterCenteredOrigin;
                    origin = CastingOrigin.CasterCentered();
                }
                else if (option.LegalAnchorIds.Contains(unitId))
                {
                    mode = CastingTargetMode.AnchoredOrigin;
                    origin = CastingOrigin.Anchored(unitId);
                }
                else return GraphRefusal("origin-anchor-illegal:" + unitId);
                casting = new PlannedCasting(castingId, SelectedRoutineId, 0, source,
                    option.Provider.Key.Ability, caster, NullIfEmpty(option.Provider.Key.SpellbookGuid),
                    mode, null, origin, null, null, null, Draft.ExistingEffectPolicy, null,
                    CastingAuthoringState.Ready, null);
            }
            AuthoringEditResult result = _authoring.AddCasting(casting);
            if (result.Applied) EditingFocusCastingId = casting.CastingId;
            return new CastingGraphEditResult(result, result.Applied ? casting.CastingId : null, false);
        }

        // An explicit parallel casting: the focused casting copied as a new
        // record (same caster, source, target, enhancements and policy) at
        // the end of its routine, then focused. The original is unchanged.
        public CastingGraphEditResult DuplicateFocusedCasting()
        {
            PlannedCasting focused = FocusedCasting();
            if (focused == null) return GraphRefusal("no-editing-focus");
            // A copy is fresh intent: no import provenance travels with it,
            // and a record that could not be Ready itself starts as a Draft.
            bool unresolved = focused.CasterUnitId == null || (focused.Provenance != null &&
                focused.Provenance.UnresolvedReviewItems.Count != 0);
            CastingAuthoringState state = unresolved ? CastingAuthoringState.Draft : focused.State;
            var copy = new PlannedCasting(NextCastingId(), focused.RoutineId, 0,
                focused.SourceId, focused.Ability, focused.CasterUnitId, focused.SpellbookGuid,
                focused.TargetMode, focused.DirectTargetUnitId, focused.Origin,
                focused.RequiredCoverageUnitIds, focused.TargetingModifiers, focused.Enhancements,
                focused.ExistingEffectPolicy, focused.IgnoredPresenceMarkers, state, null);
            AuthoringEditResult result = _authoring.AddCasting(copy);
            if (result.Applied) EditingFocusCastingId = copy.CastingId;
            return new CastingGraphEditResult(result, result.Applied ? copy.CastingId : null, false);
        }

        // Adds or removes one enhancement on the FOCUSED casting only. Only a
        // verified enhancement of that casting's own caster that applies to
        // its exact source can be added; an incompatible one (a second rod) is
        // refused with its reason, never swapped for the one already chosen.
        // A pool that cannot fund it is not refused here: the compiler blocks
        // the casting atomically and the budget shows the deficit.
        public AuthoringEditResult ToggleFocusedEnhancement(string enhancementId,
            CastingWorkspaceInputs inputs = null)
        {
            if (inputs != null) _lastInputs = inputs;
            PlannedCasting focused = FocusedCasting();
            if (focused == null) return AuthoringEditResult.Refuse("no-editing-focus");
            if (string.IsNullOrWhiteSpace(enhancementId))
                return AuthoringEditResult.Refuse("enhancement-unavailable:");
            if (focused.Enhancements.Any(value => string.Equals(value.EnhancementId, enhancementId,
                    StringComparison.Ordinal)))
                return UpdateFocusedCasting(focused.WithEnhancementSelections(
                    focused.Enhancements.Where(value => !string.Equals(value.EnhancementId,
                        enhancementId, StringComparison.Ordinal))));
            if (_lastInputs == null)
                return AuthoringEditResult.Refuse("draft-ability-unresolved:no-discovery-inputs");
            CastEnhancementSnapshot enhancement = _lastInputs.Enhancements.FirstOrDefault(value =>
                string.Equals(value.EnhancementId, enhancementId, StringComparison.Ordinal) &&
                string.Equals(value.CasterUnitId, focused.CasterUnitId, StringComparison.Ordinal));
            if (enhancement == null)
                return AuthoringEditResult.Refuse("enhancement-unavailable:" + enhancementId);
            if (enhancement.AffectsTargeting)
                return AuthoringEditResult.Refuse("enhancement-changes-targeting:" + enhancementId);
            if (!OffersEnhancement(enhancement, focused.Ability, focused.SpellbookGuid, _lastInputs))
                return AuthoringEditResult.Refuse("enhancement-not-applicable:" + enhancementId);
            ProviderPlanningOption option = FindRecordOption(_lastInputs, focused);
            string failure = option == null ? string.Empty
                : enhancement.ApplicabilityFailure(option.Provider);
            if (failure.Length != 0)
                return AuthoringEditResult.Refuse("enhancement-not-applicable:" + enhancementId +
                    ":" + failure);
            List<CastEnhancementSnapshot> combined = focused.Enhancements
                .Select(selection => _lastInputs.Enhancements.FirstOrDefault(value =>
                    string.Equals(value.EnhancementId, selection.EnhancementId,
                        StringComparison.Ordinal) &&
                    string.Equals(value.CasterUnitId, focused.CasterUnitId, StringComparison.Ordinal)))
                .Where(value => value != null).Concat(new[] { enhancement }).ToList();
            if (!CastEnhancementSnapshot.AreCompatible(combined))
                return AuthoringEditResult.Refuse("enhancement-incompatible:" + enhancementId);
            return UpdateFocusedCasting(focused.WithEnhancementSelections(focused.Enhancements
                .Concat(new[] { new AuthoredEnhancementSelection(enhancementId, true, null) })));
        }

        // Moves the focused casting to another recipient: a direct casting to
        // another target, a group casting to another origin (its required
        // coverage is kept).
        public AuthoringEditResult RetargetFocusedCasting(string unitId)
        {
            PlannedCasting focused = FocusedCasting();
            if (focused == null) return AuthoringEditResult.Refuse("no-editing-focus");
            if (focused.TargetMode == CastingTargetMode.DirectTarget)
            {
                if (string.Equals(focused.DirectTargetUnitId, unitId, StringComparison.Ordinal))
                    return AuthoringEditResult.Refuse("target-unchanged");
                return SetFocusedTargeting(CastingTargetMode.DirectTarget, unitId, null, null);
            }
            bool casterCentred = string.Equals(unitId, focused.CasterUnitId, StringComparison.Ordinal);
            return SetFocusedTargeting(
                casterCentred ? CastingTargetMode.CasterCenteredOrigin : CastingTargetMode.AnchoredOrigin,
                null, casterCentred ? null : unitId, focused.RequiredCoverageUnitIds);
        }

        // Group castings: marks one unit as intended (required) coverage, or
        // stops requiring it. Coverage is intent; predicted beneficiaries are
        // derived and never become castings.
        public AuthoringEditResult SetFocusedCoverage(string unitId, bool required)
        {
            PlannedCasting focused = FocusedCasting();
            if (focused == null) return AuthoringEditResult.Refuse("no-editing-focus");
            if (focused.TargetMode == CastingTargetMode.DirectTarget)
                return AuthoringEditResult.Refuse("targeting-mode-unsupported");
            List<string> coverage = focused.RequiredCoverageUnitIds
                .Where(value => !string.Equals(value, unitId, StringComparison.Ordinal)).ToList();
            if (required) coverage.Add(unitId);
            if (coverage.Count == focused.RequiredCoverageUnitIds.Count &&
                coverage.All(focused.RequiredCoverageUnitIds.Contains))
                return AuthoringEditResult.Refuse("coverage-unchanged");
            return SetFocusedTargeting(focused.TargetMode, null,
                focused.Origin == null || focused.Origin.IsCasterCentered ? null : focused.Origin.AnchorUnitId,
                coverage);
        }

        // ------------------------------------------------------------------
        // Read model
        // ------------------------------------------------------------------

        public CastingGraphView BuildGraph(CastingWorkspaceInputs inputs)
        {
            if (inputs == null) throw new ArgumentNullException("inputs");
            // The selected run (live effects, this routine's reservations)
            // and the explicitly labelled one-pass sequence over the whole
            // plan: the two budget views the addendum requires. The caster
            // lane's counts come from the one-pass plan's own ledger.
            ExplicitCastingPlan plan = Compile(inputs, SelectedRoutineId, false);
            CastingForecast onePass = _forecast.ForecastOnePass(
                _authoring.Document, inputs.Snapshot, inputs.ProviderOptions,
                inputs.EffectsBySource, inputs.Enhancements, inputs.TargetingModifiers);
            RefreshPoolLabels(inputs);
            if (EditingFocusCastingId != null && FocusedCasting() == null)
                EditingFocusCastingId = null;
            if (string.IsNullOrEmpty(SelectedSourceId))
            {
                string first = FirstSourceId(inputs);
                if (!string.IsNullOrEmpty(first)) SelectedSourceId = first;
            }
            string source = SelectedSourceId ?? string.Empty;
            if (string.IsNullOrEmpty(Draft.SourceId)) Draft.SourceId = source;
            var names = inputs.Snapshot.Units.ToDictionary(unit => unit.UnitId,
                unit => string.IsNullOrWhiteSpace(unit.DisplayName) ? unit.UnitId : unit.DisplayName,
                StringComparer.Ordinal);
            Func<string, string> nameOf = unitId => unitId == null ? null
                : names.TryGetValue(unitId, out string name) ? name : unitId;
            var view = new CastingGraphView
            {
                SelectedSourceId = source,
                SelectedRoutineId = SelectedRoutineId,
                SelectedRoutineName = RoutineDisplayName(SelectedRoutineId),
                FocusedCastingId = EditingFocusCastingId
            };
            List<WorkspaceSourceOption> sources = BuildSourceOptions(inputs, source);
            WorkspaceSourceOption selected = sources.FirstOrDefault(value => value.Selected);
            view.SelectedSourceCaption = selected == null ? (source.Length == 0 ? "no buff selected"
                : "unnamed buff source") : selected.Label;
            view.SelectedSourceIcon = selected == null ? null : selected.IconAbility;
            view.SelectedSourceIsGroup = source.Length == 0 ? null : IsGroupSource(inputs, source);
            view.Routines = _authoring.Document.Routines.Select(routine => new CastingGraphRoutineTab(
                routine.RoutineId, RoutineDisplayName(routine.RoutineId),
                _authoring.Document.Castings.Count(value => value != null &&
                    string.Equals(value.RoutineId, routine.RoutineId, StringComparison.Ordinal)),
                string.Equals(routine.RoutineId, SelectedRoutineId, StringComparison.Ordinal))).ToList();
            view.Catalogue = sources.Select(value => new CastingGraphCatalogueEntry(value,
                _authoring.Document.Castings.Count(casting => casting != null &&
                    string.Equals(casting.SourceId, value.SourceId, StringComparison.Ordinal) &&
                    string.Equals(casting.RoutineId, SelectedRoutineId, StringComparison.Ordinal)),
                _authoring.Document.Castings.Count(casting => casting != null &&
                    string.Equals(casting.SourceId, value.SourceId, StringComparison.Ordinal))))
                .ToList();
            // Castings of the selected buff in the selected routine: the graph.
            List<ResolvedCasting> shown = plan.Castings.Where(value =>
                string.Equals(value.SourceId, source, StringComparison.Ordinal) &&
                string.Equals(value.RoutineId, SelectedRoutineId, StringComparison.Ordinal)).ToList();
            view.OtherRoutineCastings = _authoring.Document.Castings.Count(value => value != null &&
                string.Equals(value.SourceId, source, StringComparison.Ordinal) &&
                !string.Equals(value.RoutineId, SelectedRoutineId, StringComparison.Ordinal));
            List<CastingGraphCasting> chips = BuildGraphChips(plan, onePass.Plan, shown, inputs);
            view.Castings = chips;
            view.Casters = BuildGraphCasters(inputs, onePass.Plan, source, chips);
            CastingGraphCasterNode chosenCaster = view.Casters.FirstOrDefault(value => value.Selected);
            view.SelectedCasterUnitId = chosenCaster == null ? null : chosenCaster.UnitId;
            CastingGraphSourceRow chosenRow = chosenCaster == null ? null
                : chosenCaster.Sources.FirstOrDefault(value => value.Selected);
            view.SelectedProviderKey = chosenRow == null ? null : chosenRow.ProviderKey;
            ProviderPlanningOption chosenOption = chosenRow == null ? null
                : FindProviderOption(inputs, source, chosenRow.ProviderKey);
            view.Targets = BuildGraphTargets(inputs, chosenOption, view.SelectedSourceIsGroup == true,
                chips, nameOf);
            view.Inspector = BuildGraphInspector(inputs, plan, onePass.Plan, chips, nameOf);
            view.Guidance = CastingGraphText.Guidance(source.Length != 0,
                chosenCaster == null ? null : chosenCaster.DisplayName,
                chosenCaster == null ? 0 : chosenCaster.Sources.Count,
                chosenRow == null ? null : chosenRow.Label,
                view.SelectedSourceIsGroup == true, chips.Count);
            // Budgets: the selected run and the whole plan in one pass, each
            // labelled; a preview never approves anything.
            view.SelectedRunLabel = RoutineDisplayName(SelectedRoutineId) + " run on its own";
            view.SelectedRunBudget = FooterLinesOf(plan);
            view.OnePassLabel = "Whole plan in one pass (" + string.Join(" > ",
                _authoring.Document.Routines.Select(value => RoutineDisplayName(value.RoutineId)).ToArray()) +
                ", no rest)";
            view.OnePassBudget = FooterLinesOf(onePass.Plan);
            view.SelectedRoutineGate = _gate.Evaluate(plan, CastingApplyMode.Ordinary, SelectedRoutineId);
            view.ReviewStatus = _review.StatusFor(SelectedRoutineId);
            List<ResolvedCasting> routineCastings = plan.Castings.Where(value => string.Equals(
                value.RoutineId, SelectedRoutineId, StringComparison.Ordinal)).ToList();
            view.RoutineCastingCount = routineCastings.Count;
            view.RoutineReadyCount = routineCastings.Count(value =>
                value.Readiness == ResolvedCastingReadiness.Ready);
            view.OnePassShortCount = onePass.Plan.Castings.Count(value =>
                string.Equals(value.RoutineId, SelectedRoutineId, StringComparison.Ordinal) &&
                value.Readiness == ResolvedCastingReadiness.Blocked &&
                plan.Castings.Any(alone => string.Equals(alone.CastingId, value.CastingId,
                    StringComparison.Ordinal) && alone.IsExecutable));
            return view;
        }

        private IReadOnlyList<string> FooterLinesOf(ExplicitCastingPlan plan)
        {
            var rows = plan.BudgetLines.Select(line => new WorkspaceBudgetRow(line, BudgetLabel(line)))
                .ToList();
            WorkspaceBudgetRow.DisambiguateLabels(rows);
            return WorkspaceBudgetRow.FooterLines(rows);
        }

        private List<CastingGraphCasting> BuildGraphChips(ExplicitCastingPlan plan,
            ExplicitCastingPlan onePass, List<ResolvedCasting> shown, CastingWorkspaceInputs inputs)
        {
            var chips = new List<CastingGraphCasting>();
            foreach (ResolvedCasting casting in shown)
            {
                PlannedCasting record = _authoring.Document.Castings.FirstOrDefault(value =>
                    string.Equals(value.CastingId, casting.CastingId, StringComparison.Ordinal));
                string target = casting.TargetMode == CastingTargetMode.DirectTarget
                    ? casting.DirectTargetUnitId
                    : casting.TargetMode == CastingTargetMode.CasterCenteredOrigin
                        ? casting.CasterUnitId : casting.Origin == null ? null : casting.Origin.AnchorUnitId;
                bool review = casting.Provenance != null && casting.Provenance.UnresolvedReviewItems.Count != 0;
                string reason = casting.ReadinessReasons
                    .Where(code => !review || !code.StartsWith("import-review-unresolved",
                        StringComparison.Ordinal))
                    .Select(WorkspaceReasonText.Describe).FirstOrDefault() ??
                    (review ? "imported: needs your review" : string.Empty);
                ResolvedCasting whole = onePass.CastingById(casting.CastingId);
                chips.Add(new CastingGraphCasting(casting.CastingId, casting.RoutineId,
                    casting.Order, casting.CasterUnitId,
                    casting.Provider == null ? null : casting.Provider.Canonical,
                    casting.TargetMode, target,
                    casting.TargetMode == CastingTargetMode.DirectTarget ? new string[0]
                        : (IEnumerable<string>)casting.PredictedBeneficiaryUnitIds,
                    casting.RequiredCoverageUnitIds,
                    casting.CoverageGaps.Select(gap => gap.UnitId),
                    casting.Readiness, GraphStatusLabel(casting), reason,
                    casting.Enhancements.Select(selection => EnhancementBadge(inputs,
                        selection.EnhancementId, casting.CasterUnitId)),
                    string.Join(", ", casting.Cost.Select(CostLabel).ToArray()),
                    string.Equals(casting.CastingId, EditingFocusCastingId, StringComparison.Ordinal),
                    record == null ? CastingAuthoringState.Draft : record.State, review,
                    casting.IsExecutable && whole != null &&
                        whole.Readiness == ResolvedCastingReadiness.Blocked,
                    casting.IsExecutable && whole != null &&
                        whole.Readiness == ResolvedCastingReadiness.AlreadySatisfied));
            }
            // Parallel castings (same caster, source and target) stay distinct.
            foreach (IGrouping<string, CastingGraphCasting> same in chips.GroupBy(value =>
                (value.CasterUnitId ?? string.Empty) + "|" + (value.SourceProviderKey ?? string.Empty) +
                "|" + (value.TargetUnitId ?? string.Empty), StringComparer.Ordinal))
            {
                int index = 0;
                int count = same.Count();
                foreach (CastingGraphCasting chip in same.OrderBy(value => value.Order))
                {
                    chip.ParallelIndex = index++;
                    chip.ParallelCount = count;
                }
            }
            return chips;
        }

        private static string GraphStatusLabel(ResolvedCasting casting)
        {
            switch (casting.Readiness)
            {
                case ResolvedCastingReadiness.AlreadySatisfied: return "Already active";
                case ResolvedCastingReadiness.Ready:
                    return ExplicitCastingStepConverter.StandardExecutionLimitation(casting) == null
                        ? "Ready" : "Ready - cannot run yet";
                case ResolvedCastingReadiness.Disabled: return "Disabled";
                case ResolvedCastingReadiness.Draft: return "Draft";
                default: return "Blocked";
            }
        }

        private string EnhancementBadge(CastingWorkspaceInputs inputs, string enhancementId,
            string casterUnitId)
        {
            CastEnhancementSnapshot enhancement = inputs == null ? null : inputs.Enhancements
                .FirstOrDefault(value => string.Equals(value.EnhancementId, enhancementId,
                        StringComparison.Ordinal) &&
                    string.Equals(value.CasterUnitId, casterUnitId, StringComparison.Ordinal));
            if (enhancement == null) return EnhancementName(enhancementId);
            return EnhancementTitle(enhancement);
        }

        // The enhancement's own effect name ("Extend Spell", "Powerful
        // Change") when it has one, else the item/feature name.
        private static string EnhancementTitle(CastEnhancementSnapshot enhancement)
        {
            string effect = enhancement.EffectDisplayName;
            bool generic = string.IsNullOrWhiteSpace(effect) ||
                string.Equals(effect, "Metamagic", StringComparison.Ordinal) ||
                string.Equals(effect, "Class feature", StringComparison.Ordinal);
            return generic ? enhancement.DisplayName : effect;
        }

        private List<CastingGraphCasterNode> BuildGraphCasters(CastingWorkspaceInputs inputs,
            ExplicitCastingPlan onePass, string source, List<CastingGraphCasting> chips)
        {
            var nodes = new List<CastingGraphCasterNode>();
            HashSet<string> capable = CapableCasterUnitIdsForSource(inputs, source);
            var partyIds = new HashSet<string>(inputs.Snapshot.Units.Select(unit => unit.UnitId),
                StringComparer.Ordinal);
            // Every caster a shown casting starts from stays on the page too,
            // even one that can no longer cast the buff, so no line dangles.
            var casting = new HashSet<string>(chips.Where(value => value.CasterUnitId != null &&
                partyIds.Contains(value.CasterUnitId)).Select(value => value.CasterUnitId),
                StringComparer.Ordinal);
            if (chips.Any(value => value.CasterUnitId == null || !partyIds.Contains(value.CasterUnitId)))
                nodes.Add(new CastingGraphCasterNode(string.Empty, "Needs a caster", false, null,
                    "Imported castings whose caster is not chosen or not in the party.", true));
            string chosenCaster = Draft.CasterUnitId ?? SelectedCasterUnitId;
            // Every row with the caster's name, so a pool shared across the
            // party (item charges) is disclosed under each caster.
            var allRows = new List<KeyValuePair<string, CastingGraphSourceRow>>();
            foreach (UnitSnapshot unit in inputs.Snapshot.Units)
            {
                if (!capable.Contains(unit.UnitId) && !casting.Contains(unit.UnitId)) continue;
                List<ProviderPlanningOption> options = SourceOptionsFor(inputs, source, unit.UnitId);
                var rows = new List<CastingGraphSourceRow>();
                foreach (ProviderPlanningOption option in options)
                {
                    ProviderSnapshot provider = option.Provider;
                    ResourcePoolSnapshot pool = inputs.Snapshot.ResourcePools.FirstOrDefault(value =>
                        string.Equals(value.PoolKey, provider.ResourcePoolKey, StringComparison.Ordinal));
                    CastingCapacityEstimate estimate = onePass.Capacity == null ? null
                        : onePass.Capacity.AdditionalCastings(provider);
                    bool pinnable = TwinCount(inputs, provider.Key) == 1;
                    bool selectedRow = string.Equals(chosenCaster, unit.UnitId, StringComparison.Ordinal) &&
                        Draft.Ability != null &&
                        string.Equals(Draft.Ability.Canonical, provider.Key.Ability.Canonical,
                            StringComparison.Ordinal) &&
                        string.Equals(NullIfEmpty(Draft.SpellbookGuid), NullIfEmpty(provider.Key.SpellbookGuid),
                            StringComparison.Ordinal);
                    rows.Add(new CastingGraphSourceRow(provider.Key.Canonical, unit.UnitId,
                        SourceRowLabel(provider, pool), WorkspaceProviderLabels.Describe(null,
                            provider.SourceBookName, provider.SpellLevel, pool == null ? (ResourcePoolKind?)null
                                : pool.Kind, PoolLabel(provider.ResourcePoolKey), provider.EffectiveCasterLevel,
                            provider.Key.Ability.MetamagicMask),
                        provider.ResourcePoolKey, SourcePoolLabel(provider, pool),
                        pool == null ? (ResourcePoolKind?)null : pool.Kind, estimate,
                        CastingGraphText.Capacity(estimate, pool == null ? (ResourcePoolKind?)null : pool.Kind,
                            provider.UnitsPerCast),
                        pinnable, CastingGraphText.BlockedReason(estimate, pinnable), selectedRow,
                        IsGroupSource(inputs, source) == true));
                }
                string displayName = string.IsNullOrWhiteSpace(unit.DisplayName) ? unit.UnitId : unit.DisplayName;
                allRows.AddRange(rows.Select(row => new KeyValuePair<string, CastingGraphSourceRow>(displayName, row)));
                string note = !capable.Contains(unit.UnitId) ? "Cannot cast this buff now."
                    : rows.Count > 1 && !rows.Any(value => value.Selected) ? "Choose the exact source."
                    : rows.Count != 0 && rows.All(value => !value.Usable) ? "Nothing left to cast it with."
                    : string.Empty;
                nodes.Add(new CastingGraphCasterNode(unit.UnitId, unit.DisplayName,
                    string.Equals(chosenCaster, unit.UnitId, StringComparison.Ordinal), rows, note, false));
            }
            // A pool is shared by every row that draws on it: the same
            // caster's other sources by label, another caster's with the
            // caster's name. Each row's count is what is left for it alone.
            // At-will sources (no pool key) share nothing.
            foreach (KeyValuePair<string, CastingGraphSourceRow> entry in allRows)
            {
                CastingGraphSourceRow row = entry.Value;
                if (string.IsNullOrEmpty(row.PoolKey)) continue;
                row._sharedPoolWith.AddRange(allRows.Where(other => !ReferenceEquals(other.Value, row) &&
                        string.Equals(other.Value.PoolKey, row.PoolKey, StringComparison.Ordinal))
                    .Select(other => string.Equals(other.Value.CasterUnitId, row.CasterUnitId, StringComparison.Ordinal)
                        ? other.Value.Label : other.Key + ": " + other.Value.Label));
            }
            return nodes;
        }

        private List<CastingGraphTargetNode> BuildGraphTargets(CastingWorkspaceInputs inputs,
            ProviderPlanningOption option, bool group, List<CastingGraphCasting> chips,
            Func<string, string> nameOf)
        {
            var targets = new List<CastingGraphTargetNode>();
            foreach (UnitSnapshot unit in inputs.Snapshot.Units)
            {
                List<CastingGraphCasting> touching = chips.Where(value =>
                    string.Equals(value.TargetUnitId, unit.UnitId, StringComparison.Ordinal) ||
                    (value.IsGroup && value.Beneficiaries.Contains(unit.UnitId))).ToList();
                WorkspaceRecipientCoverage coverage = touching.Any(value =>
                        value.Readiness == ResolvedCastingReadiness.Ready)
                    ? WorkspaceRecipientCoverage.CoveredReady
                    : touching.Count != 0 ? WorkspaceRecipientCoverage.CoveredNotReady
                    : WorkspaceRecipientCoverage.None;
                CastingGraphTargetLegality legality = CastingGraphTargetLegality.Unknown;
                string reason = string.Empty;
                if (option != null)
                {
                    string invalid = InvalidTargetReason(unit);
                    bool reachable = group ? option.LegalAnchorIds.Contains(unit.UnitId)
                        : option.ReachableTargetIds.Contains(unit.UnitId);
                    legality = reachable && (group || invalid == null)
                        ? CastingGraphTargetLegality.Legal : CastingGraphTargetLegality.Illegal;
                    reason = !reachable
                        ? (group ? "The group spell cannot be centred here." : "Out of this caster's reach.")
                        : !group && invalid != null ? WorkspaceReasonText.Describe(invalid) : string.Empty;
                }
                string hint = touching.Count != 0
                    ? "Shows " + string.Join(", ", touching.Select(value => "casting " + value.OrderLabel)
                        .ToArray())
                    : legality == CastingGraphTargetLegality.Legal
                        ? (group ? "Click: one group casting centred here" : "Click: add a casting to " +
                            nameOf(unit.UnitId))
                        : legality == CastingGraphTargetLegality.Unknown ? "Choose a caster first"
                        : reason;
                targets.Add(new CastingGraphTargetNode(unit.UnitId, unit.DisplayName, legality, reason,
                    touching.Select(value => value.CastingId), coverage, hint));
            }
            return targets;
        }

        private CastingGraphInspector BuildGraphInspector(CastingWorkspaceInputs inputs,
            ExplicitCastingPlan plan, ExplicitCastingPlan onePass, List<CastingGraphCasting> chips,
            Func<string, string> nameOf)
        {
            PlannedCasting focused = FocusedCasting();
            if (focused == null) return null;
            ResolvedCasting resolved = plan.CastingById(focused.CastingId);
            CastingGraphCasting chip = chips.FirstOrDefault(value => string.Equals(value.CastingId,
                focused.CastingId, StringComparison.Ordinal));
            ProviderPlanningOption option = focused.CasterUnitId == null ? null
                : FindRecordOption(inputs, focused);
            int routineCount = _authoring.Document.Castings.Count(value => string.Equals(
                value.RoutineId, focused.RoutineId, StringComparison.Ordinal));
            string casterName = focused.CasterUnitId == null ? "No caster chosen" : nameOf(focused.CasterUnitId);
            string targetLabel;
            switch (focused.TargetMode)
            {
                case CastingTargetMode.CasterCenteredOrigin:
                    targetLabel = "Group, centred on " + casterName;
                    break;
                case CastingTargetMode.AnchoredOrigin:
                    targetLabel = "Group, centred on " + nameOf(focused.Origin.AnchorUnitId);
                    break;
                default:
                    targetLabel = nameOf(focused.DirectTargetUnitId);
                    break;
            }
            ResourcePoolSnapshot pool = option == null ? null : inputs.Snapshot.ResourcePools
                .FirstOrDefault(value => string.Equals(value.PoolKey, option.Provider.ResourcePoolKey,
                    StringComparison.Ordinal));
            string sourceLabel = option == null ? "Source not resolved"
                : SourceRowLabel(option.Provider, pool) + " · " + SourcePoolLabel(option.Provider, pool);
            bool review = focused.Provenance != null && focused.Provenance.UnresolvedReviewItems.Count != 0;
            IEnumerable<string> reasons = resolved == null ? new string[0] : resolved.ReadinessReasons
                .Where(code => !review || !code.StartsWith("import-review-unresolved", StringComparison.Ordinal))
                .Select(WorkspaceReasonText.Describe).Distinct(StringComparer.Ordinal);
            IEnumerable<string> reviewItems = !review ? new string[0] : focused.Provenance
                .UnresolvedReviewItems.Select(item => WorkspaceReasonText.DescribeReviewItem(item, nameOf));
            var costLines = new List<string>();
            CastingCapacity capacity = onePass.Capacity;
            if (resolved != null && resolved.Cost.Count != 0)
                foreach (CastingCostLine line in resolved.Cost)
                    costLines.Add(CostLabel(line) + LeftAfterPlan(capacity, line.Category, line.PoolKey,
                        line.Unlimited));
            else if (resolved != null)
                foreach (string shape in resolved.CostShape)
                {
                    int first = shape.IndexOf(':');
                    int last = shape.LastIndexOf(':');
                    if (first < 0 || last <= first) continue;
                    CastingCostCategory category;
                    if (!Enum.TryParse(shape.Substring(0, first), out category)) continue;
                    string key = shape.Substring(first + 1, last - first - 1);
                    int units;
                    int.TryParse(shape.Substring(last + 1), out units);
                    // An at-will source's pool has no count to show.
                    bool unlimited = category == CastingCostCategory.NativePool && capacity != null &&
                        capacity.NativeKind(key) == ResourcePoolKind.Unlimited;
                    costLines.Add("Would spend: " + CostLabel(new CastingCostLine(category, key,
                        Math.Max(units, 1), null, null)) + LeftAfterPlan(capacity, category, key, unlimited));
                }
            var enhancements = new List<CastingGraphEnhancementOption>();
            if (inputs.Enhancements != null && focused.CasterUnitId != null)
            {
                List<CastEnhancementSnapshot> chosenSnapshots = focused.Enhancements
                    .Select(selection => inputs.Enhancements.FirstOrDefault(value =>
                        string.Equals(value.EnhancementId, selection.EnhancementId, StringComparison.Ordinal) &&
                        string.Equals(value.CasterUnitId, focused.CasterUnitId, StringComparison.Ordinal)))
                    .Where(value => value != null).ToList();
                foreach (CastEnhancementSnapshot enhancement in inputs.Enhancements.Where(value =>
                    value != null && string.Equals(value.CasterUnitId, focused.CasterUnitId,
                        StringComparison.Ordinal)))
                {
                    AuthoredEnhancementSelection selection = focused.Enhancements.FirstOrDefault(value =>
                        string.Equals(value.EnhancementId, enhancement.EnhancementId, StringComparison.Ordinal));
                    bool chosen = selection != null;
                    if (!chosen && !OffersEnhancement(enhancement, focused.Ability, focused.SpellbookGuid, inputs))
                        continue;
                    int? remaining = capacity == null ? null : capacity.EnhancementRemaining(enhancement.UsagePoolId);
                    int? available = capacity == null ? null : capacity.EnhancementAvailableNow(enhancement.UsagePoolId);
                    string unavailable = string.Empty;
                    if (!chosen)
                    {
                        if (enhancement.AffectsTargeting)
                            unavailable = "Changes whom the spell reaches; not executed in this version.";
                        else if (option != null)
                            unavailable = CastingGraphText.Applicability(enhancement.ApplicabilityFailure(option.Provider));
                        if (unavailable.Length == 0 && !CastEnhancementSnapshot.AreCompatible(
                                chosenSnapshots.Concat(new[] { enhancement })))
                            unavailable = "Cannot be combined with " + string.Join(", ", chosenSnapshots
                                .Select(EnhancementTitle).ToArray()) + " on one casting.";
                        if (unavailable.Length == 0 && enhancement.RemainingUses == 0)
                            unavailable = "No uses left.";
                        if (unavailable.Length == 0 && remaining != null &&
                            remaining.Value < enhancement.UsageUnitsPerCast)
                            unavailable = "Not enough uses left after the rest of the plan (" + remaining.Value +
                                " left, needs " + enhancement.UsageUnitsPerCast + ").";
                    }
                    string poolName = enhancement.UsagePoolDisplayName;
                    enhancements.Add(new CastingGraphEnhancementOption(enhancement.EnhancementId,
                        EnhancementTitle(enhancement), CastingGraphText.Mechanism(enhancement),
                        enhancement.DisplayName, chosen, selection == null || selection.Required, unavailable,
                        enhancement.UsageUnitsPerCast + (enhancement.UsageUnitsPerCast == 1 ? " use" : " uses") +
                            " of " + poolName, poolName, remaining, available,
                        remaining == null ? poolName + ": uses left unknown"
                            : poolName + ": " + remaining.Value + (available == null ? string.Empty
                                : " of " + available.Value) + " left after the whole plan",
                        CastingGraphText.ExpectedEffect(enhancement)));
                }
            }
            IEnumerable<WorkspaceProviderChoice> providers = ProviderChoices(inputs, focused.SourceId, null,
                focused.CasterUnitId, focused.Ability, focused.SpellbookGuid);
            var retargets = new List<CastingGraphTargetNode>();
            if (option != null)
            {
                bool group = focused.TargetMode != CastingTargetMode.DirectTarget;
                foreach (UnitSnapshot unit in inputs.Snapshot.Units)
                {
                    bool legal = group ? option.LegalAnchorIds.Contains(unit.UnitId)
                        : option.ReachableTargetIds.Contains(unit.UnitId) && InvalidTargetReason(unit) == null;
                    if (!legal) continue;
                    retargets.Add(new CastingGraphTargetNode(unit.UnitId, unit.DisplayName,
                        CastingGraphTargetLegality.Legal, string.Empty, new string[0],
                        WorkspaceRecipientCoverage.None, string.Empty));
                }
            }
            string coverageText = string.Empty;
            if (focused.TargetMode != CastingTargetMode.DirectTarget && resolved != null)
                coverageText = "Reaches " + resolved.PredictedBeneficiaryUnitIds.Count + ": " +
                    string.Join(", ", resolved.PredictedBeneficiaryUnitIds.Select(nameOf).ToArray()) +
                    (resolved.RequiredCoverageUnitIds.Count == 0 ? " · no recipients marked as required"
                        : " · required: " + string.Join(", ", resolved.RequiredCoverageUnitIds.Select(nameOf).ToArray())) +
                    (resolved.CoverageGaps.Count == 0 ? string.Empty
                        : " · MISSED: " + string.Join(", ", resolved.CoverageGaps.Select(gap => nameOf(gap.UnitId)).ToArray()) +
                          " (outside the predicted area; no second casting is added for them)");
            CastingOutcomeEntry lastRun = LastRunReport == null ? null : LastRunReport.Entries
                .FirstOrDefault(entry => string.Equals(entry.CastingId, focused.CastingId, StringComparison.Ordinal));
            return new CastingGraphInspector(focused, chip,
                "Casting " + (focused.Order + 1) + " in " + RoutineDisplayName(focused.RoutineId),
                casterName + " → " + targetLabel, casterName, sourceLabel, targetLabel,
                RoutineDisplayName(focused.RoutineId), routineCount, reasons, reviewItems, costLines,
                enhancements, providers, retargets, coverageText,
                resolved == null ? null : CastingRunPresentation.DescribeLimitation(
                    ExplicitCastingStepConverter.StandardExecutionLimitation(resolved)),
                resolved == null ? new string[0] : resolved.ExistingEffectNotes.Select(note =>
                    CastingRunPresentation.DescribeExistingEffectNote(note, nameOf)),
                CastingRunPresentation.DescribeEntry(lastRun));
        }

        private static string LeftAfterPlan(CastingCapacity capacity, CastingCostCategory category,
            string poolKey, bool unlimited)
        {
            if (capacity == null || unlimited) return string.Empty;
            int? remaining;
            int? available;
            switch (category)
            {
                case CastingCostCategory.NativePool:
                    remaining = capacity.NativeRemaining(poolKey);
                    available = capacity.NativeAvailableNow(poolKey);
                    break;
                case CastingCostCategory.EnhancementPool:
                    remaining = capacity.EnhancementRemaining(poolKey);
                    available = capacity.EnhancementAvailableNow(poolKey);
                    break;
                default:
                    return string.Empty;
            }
            if (remaining == null) return " · left after the whole plan: unknown";
            return " · " + remaining.Value + (available == null ? string.Empty : " of " + available.Value) +
                " left after the whole plan";
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        // Every discovered provider option of one caster that serves the
        // source (its exact sources), in a stable readable order.
        private static List<ProviderPlanningOption> SourceOptionsFor(CastingWorkspaceInputs inputs,
            string sourceId, string casterUnitId)
        {
            EffectExpression expression;
            if (inputs == null || inputs.ProviderOptions == null || inputs.EffectsBySource == null ||
                string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(casterUnitId) ||
                !inputs.EffectsBySource.TryGetValue(sourceId, out expression))
                return new List<ProviderPlanningOption>();
            return inputs.ProviderOptions.Where(option => option != null && option.Provider != null &&
                    string.Equals(option.Provider.Key.CasterUnitId, casterUnitId, StringComparison.Ordinal) &&
                    OptionServesExpression(inputs, option, expression))
                .OrderBy(option => option.Provider.SourceBookName, StringComparer.InvariantCultureIgnoreCase)
                .ThenBy(option => option.Provider.SpellLevel)
                .ThenBy(option => option.Provider.Key.Canonical, StringComparer.Ordinal)
                .ToList();
        }

        // True for a group ability, false for a single-target one, null when
        // its targeting is not understood (the compiler's own rule).
        private static bool? IsGroupSource(CastingWorkspaceInputs inputs, string sourceId)
        {
            EffectExpression expression;
            if (inputs == null || inputs.EffectsBySource == null || string.IsNullOrEmpty(sourceId) ||
                !inputs.EffectsBySource.TryGetValue(sourceId, out expression))
                return null;
            CastGroupingKind grouping;
            if (!EffectExpressionTargetAnalysis.TryGetGrouping(expression, out grouping)) return null;
            return grouping == CastGroupingKind.MassConfiguredTargets;
        }

        private PlannedCasting ExistingCastingFor(string sourceId, string routineId, string unitId,
            bool group)
        {
            List<PlannedCasting> same = _authoring.Document.Castings.Where(casting => casting != null &&
                string.Equals(casting.SourceId, sourceId, StringComparison.Ordinal) &&
                string.Equals(casting.RoutineId, routineId, StringComparison.Ordinal)).ToList();
            if (!group)
                return same.FirstOrDefault(casting => casting.TargetMode == CastingTargetMode.DirectTarget &&
                    string.Equals(casting.DirectTargetUnitId, unitId, StringComparison.Ordinal));
            List<PlannedCasting> groups = same.Where(casting =>
                casting.TargetMode != CastingTargetMode.DirectTarget).ToList();
            PlannedCasting centred = groups.FirstOrDefault(casting => string.Equals(
                casting.TargetMode == CastingTargetMode.CasterCenteredOrigin ? casting.CasterUnitId
                    : casting.Origin == null ? null : casting.Origin.AnchorUnitId,
                unitId, StringComparison.Ordinal));
            if (centred != null || groups.Count == 0 || _lastInputs == null) return centred;
            // A unit a group casting of this buff already reaches is shown
            // that casting; a second group cast is only added explicitly
            // (Duplicate), never by clicking a covered member.
            ExplicitCastingPlan plan = Compile(_lastInputs, routineId, false);
            return groups.FirstOrDefault(casting =>
            {
                ResolvedCasting resolved = plan.CastingById(casting.CastingId);
                return resolved != null && resolved.PredictedBeneficiaryUnitIds.Contains(unitId);
            });
        }

        private static string InvalidTargetReason(UnitSnapshot unit)
        {
            if (!unit.TargetValidation.Alive) return "target-not-alive";
            if (!unit.TargetValidation.Conscious) return "target-not-conscious";
            if (!unit.TargetValidation.Friendly) return "target-not-friendly";
            if (!unit.TargetValidation.Targetable) return "target-not-targetable";
            return null;
        }

        // The source row's own name: a spellbook by name and level, an
        // ability or item by its resource.
        private static string SourceRowLabel(ProviderSnapshot provider, ResourcePoolSnapshot pool)
        {
            string metamagic = provider.Key.Ability.MetamagicMask != 0 ? " (metamagic)" : string.Empty;
            if (!string.IsNullOrWhiteSpace(provider.SourceBookName))
                return provider.SourceBookName + " level " + provider.SpellLevel + metamagic;
            string resource = pool == null ? "Resource" : WorkspacePoolLabels.Describe(pool.Kind, null, null,
                provider.SourceDisplayName);
            return char.ToUpperInvariant(resource[0]) + resource.Substring(1) + metamagic;
        }

        // The pool that controls a source row's count, without its owner
        // (the row sits under its caster already).
        private static string SourcePoolLabel(ProviderSnapshot provider, ResourcePoolSnapshot pool)
        {
            if (pool == null) return "unknown resource";
            return WorkspacePoolLabels.Describe(pool.Kind,
                pool.Kind == ResourcePoolKind.SpontaneousLevel || pool.Kind == ResourcePoolKind.PreparedSlots
                    ? provider.SpellLevel : (int?)null, null, provider.SourceDisplayName);
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static CastingGraphEditResult GraphRefusal(string reason)
        {
            return new CastingGraphEditResult(AuthoringEditResult.Refuse(reason), null, false);
        }
    }
}
