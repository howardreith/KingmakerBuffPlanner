using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Persistence;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class CatalogFilterState
    {
        internal string Search = string.Empty;
        internal bool ConfiguredOnly;
        internal bool ShowHidden;
        internal int DurationFilter;
        internal int SourceCategoryFilter;
        internal int AvailabilityFilter;

        internal void Reset()
        {
            Search = string.Empty;
            ConfiguredOnly = false;
            ShowHidden = false;
            DurationFilter = 0;
            SourceCategoryFilter = 0;
            AvailabilityFilter = 0;
        }

        internal List<SetupSourceRow> Apply(
            PlannerSetupModel model,
            string routineId,
            out CatalogFilterDiagnostics diagnostics)
        {
            if (model == null) throw new ArgumentNullException("model");
            diagnostics = new CatalogFilterDiagnostics();
            var active = new List<string>();
            List<SetupSourceRow> values = model.Sources.ToList();
            diagnostics.TotalEntries = values.Count;
            RoutineProfile routine = model.Profile.Routines.First(item => item.RoutineId == routineId);
            diagnostics.AssignedToActiveGroup = routine.Assignments.Count;

            if (!string.IsNullOrWhiteSpace(Search))
            {
                values = values.Where(source => source.DisplayName.IndexOf(
                    Search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                active.Add("search='" + Search + "'");
            }
            diagnostics.AfterSearch = values.Count;

            if (ConfiguredOnly)
            {
                values = values.Where(source => model.Profile.Routines.Any(group =>
                    group.Assignments.Any(item => item.SourceId == source.SourceId))).ToList();
                active.Add("configured only");
            }
            diagnostics.AfterConfigured = values.Count;

            if (DurationFilter == 1)
            {
                values = values.Where(source => source.ExpectedDurationRounds >= 10).ToList();
                active.Add("long duration");
            }
            else if (DurationFilter == 2)
            {
                values = values.Where(source => source.ExpectedDurationRounds > 0 &&
                    source.ExpectedDurationRounds < 10).ToList();
                active.Add("short duration");
            }
            diagnostics.AfterDuration = values.Count;

            if (SourceCategoryFilter == 1)
            {
                values = values.Where(source =>
                    source.Ability.SourceKind == SourceKind.Spellbook).ToList();
                active.Add("spells");
            }
            else if (SourceCategoryFilter == 2)
            {
                values = values.Where(source =>
                    source.Ability.SourceKind == SourceKind.AbilityResource ||
                    source.Ability.SourceKind == SourceKind.Fact).ToList();
                active.Add("abilities");
            }
            diagnostics.AfterSource = values.Count;

            if (!ShowHidden)
            {
                values = values.Where(source =>
                    !model.Profile.HiddenSourceIds.Contains(source.SourceId)).ToList();
                active.Add("hidden excluded");
            }
            else active.Add("hidden included");
            diagnostics.AfterHidden = values.Count;

            if (AvailabilityFilter == 1)
            {
                values = values.Where(model.IsSourceAvailable).ToList();
                active.Add("available only");
            }
            else if (AvailabilityFilter == 2)
            {
                values = values.Where(source => !model.IsSourceAvailable(source)).ToList();
                active.Add("unavailable only");
            }
            else active.Add("all availability");
            diagnostics.AfterAvailability = values.Count;
            diagnostics.VisibleViewModels = values.Count;
            diagnostics.ActiveFilters = active.Count == 0 ? "none" :
                string.Join(", ", active.ToArray());
            return values;
        }
    }

    internal sealed class CatalogFilterDiagnostics
    {
        internal int TotalEntries;
        internal int AssignedToActiveGroup;
        internal int AfterSearch;
        internal int AfterConfigured;
        internal int AfterDuration;
        internal int AfterSource;
        internal int AfterHidden;
        internal int AfterAvailability;
        internal int VisibleViewModels;
        internal string ActiveFilters = string.Empty;

        public override string ToString()
        {
            return "total=" + TotalEntries + ";groupAssigned=" + AssignedToActiveGroup +
                ";search=" + AfterSearch + ";configured=" + AfterConfigured +
                ";duration=" + AfterDuration + ";source=" + AfterSource +
                ";hidden=" + AfterHidden + ";availability=" + AfterAvailability +
                ";viewModels=" + VisibleViewModels + ";activeFilters=" + ActiveFilters;
        }
    }

}
