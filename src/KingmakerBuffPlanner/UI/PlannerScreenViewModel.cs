using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class PlannerScreenViewModel
    {
        private readonly PlannerUiSession _session;
        private readonly CatalogFilterState _filters;

        internal PlannerScreenViewModel(PlannerUiSession session, CatalogFilterState filters)
        {
            _session = session ?? throw new ArgumentNullException("session");
            _filters = filters ?? throw new ArgumentNullException("filters");
            ActiveRoutineId = "long";
        }

        internal string ActiveRoutineId { get; private set; }
        internal PlannerSourceCategory Category { get { return _filters.SourceCategory; } }
        internal bool SelectedOnly { get { return _filters.SelectedOnly; } }

        internal void SetRoutine(string routineId)
        {
            if (routineId != "long" && routineId != "important" && routineId != "short")
                throw new ArgumentException("Unknown routine.", "routineId");
            ActiveRoutineId = routineId;
        }

        internal void SetSearch(string value) { _filters.Search = value ?? string.Empty; }
        internal void SetCategory(PlannerSourceCategory category) { _filters.SourceCategory = category; }
        internal void ToggleSelectedOnly() { _filters.SelectedOnly = !_filters.SelectedOnly; }

        internal IReadOnlyList<BuffCardViewModel> Cards(out CatalogFilterDiagnostics diagnostics)
        {
            PlannerSetupModel model = _session.Model;
            if (model == null)
            {
                diagnostics = new CatalogFilterDiagnostics();
                return new ReadOnlyCollection<BuffCardViewModel>(new List<BuffCardViewModel>());
            }
            List<SetupSourceRow> sources = _filters.Apply(model, ActiveRoutineId, out diagnostics);
            return new ReadOnlyCollection<BuffCardViewModel>(sources.Select(source =>
                new BuffCardViewModel(source, model, ActiveRoutineId,
                    source.SourceId == model.SelectedSourceId)).ToList());
        }

        internal IReadOnlyList<TargetPortraitViewModel> Targets()
        {
            PlannerSetupModel model = _session.Model;
            SetupSourceRow source = model == null ? null : model.SelectedSource;
            return new ReadOnlyCollection<TargetPortraitViewModel>(source == null
                ? new List<TargetPortraitViewModel>()
                : model.Snapshot.Units.Select(unit => TargetPortraitViewModel.Create(
                    source, model, ActiveRoutineId, unit)).ToList());
        }

        internal RoutineSummaryViewModel RoutineSummary(string routineId)
        {
            string name = char.ToUpperInvariant(routineId[0]) + routineId.Substring(1);
            PlannerSetupModel model = _session.Model;
            if (model == null) return new RoutineSummaryViewModel(routineId, name, 0, 0);
            int requested = model.Profile.Routines.First(item => item.RoutineId == routineId)
                .Assignments.Sum(item => item.WantedTargetUnitIds.Count);
            try
            {
                RoutinePlanResult preview = _session.PreviewRoutine(routineId);
                int covered = preview.Plan.Outcomes.Count(item =>
                    item.Kind == TargetOutcomeKind.Fulfilled ||
                    item.Kind == TargetOutcomeKind.SkippedAlreadyActive);
                return new RoutineSummaryViewModel(routineId, name, covered, requested);
            }
            catch
            {
                return new RoutineSummaryViewModel(routineId, name, 0, requested);
            }
        }

        internal string PlanSummary()
        {
            PlannerSetupModel model = _session.Model;
            if (model == null) return "Load a campaign to build a plan.";
            try
            {
                RoutinePlanResult preview = _session.PreviewRoutine(ActiveRoutineId);
                int covered = preview.Plan.Outcomes.Count(item =>
                    item.Kind == TargetOutcomeKind.Fulfilled ||
                    item.Kind == TargetOutcomeKind.SkippedAlreadyActive);
                int requested = model.Profile.Routines.First(item =>
                    item.RoutineId == ActiveRoutineId).Assignments
                    .Sum(item => item.WantedTargetUnitIds.Count);
                int blocked = preview.Plan.Outcomes.Count(item =>
                    item.Kind == TargetOutcomeKind.Unfulfilled);
                string summary = preview.Plan.Steps.Count +
                    (preview.Plan.Steps.Count == 1 ? " cast" : " casts") + " | " +
                    covered + " of " + requested + " targets covered";
                return blocked == 0 ? summary : summary + " | " + blocked + " blocked";
            }
            catch (Exception exception)
            {
                return "Plan unavailable: " + exception.Message;
            }
        }
    }

}
