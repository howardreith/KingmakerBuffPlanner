using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Persistence;

namespace KingmakerBuffPlanner.UI
{
    internal enum PlannerSourceCategory
    {
        All,
        Spells,
        Abilities,
        Other
    }

    internal sealed class CatalogFilterState
    {
        internal string Search = string.Empty;
        internal bool SelectedOnly;
        internal PlannerSourceCategory SourceCategory;

        internal void Reset()
        {
            Search = string.Empty;
            SelectedOnly = false;
            SourceCategory = PlannerSourceCategory.All;
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
            diagnostics.AssignedToActiveGroup = routine.Assignments.Count(item =>
                item.WantedTargetUnitIds.Count != 0);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                values = values.Where(source => source.SearchText.IndexOf(
                    Search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                active.Add("search='" + Search + "'");
            }
            diagnostics.AfterSearch = values.Count;

            if (SelectedOnly)
            {
                values = values.Where(source => routine.Assignments.Any(item =>
                    item.SourceId == source.SourceId &&
                    item.WantedTargetUnitIds.Count != 0)).ToList();
                active.Add("selected only in " + routineId);
            }
            diagnostics.AfterConfigured = values.Count;
            diagnostics.AfterDuration = values.Count;

            if (SourceCategory == PlannerSourceCategory.Spells)
            {
                values = values.Where(source =>
                    source.HasSourceKind(SourceKind.Spellbook)).ToList();
                active.Add("spells");
            }
            else if (SourceCategory == PlannerSourceCategory.Abilities)
            {
                values = values.Where(source =>
                    source.HasSourceKind(SourceKind.AbilityResource) ||
                    source.HasSourceKind(SourceKind.Fact)).ToList();
                active.Add("abilities");
            }
            else if (SourceCategory == PlannerSourceCategory.Other)
            {
                values = values.Where(source =>
                    source.Abilities.Any(ability => ability.SourceKind != SourceKind.Spellbook &&
                    ability.SourceKind != SourceKind.AbilityResource &&
                    ability.SourceKind != SourceKind.Fact)).ToList();
                active.Add("other");
            }
            diagnostics.AfterSource = values.Count;
            diagnostics.AfterHidden = values.Count;
            diagnostics.AfterAvailability = values.Count;
            values = values.OrderBy(source => source.SortGroupName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(source => source.SortGroupId, StringComparer.Ordinal)
                .ThenBy(source => source.VariantOrder)
                .ThenBy(source => source.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(source => source.SpellLevel)
                .ThenBy(source => source.SourceId, StringComparer.Ordinal).ToList();
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
                ";search=" + AfterSearch + ";selected=" + AfterConfigured +
                ";source=" + AfterSource + ";viewModels=" + VisibleViewModels +
                ";activeFilters=" + ActiveFilters;
        }
    }
}
