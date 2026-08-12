using System;

namespace KingmakerBuffPlanner.UI
{
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

    internal sealed class CatalogLayoutDiagnostics
    {
        internal CatalogFilterDiagnostics Filters;
        internal int InstantiatedRows;
        internal int ActiveRows;
        internal int VisibleRows;
        internal int DetailChildren;
        internal bool SelectedDetailsBound;
        internal string SelectedSourceId = string.Empty;
        internal float ContentWidth;
        internal float ContentHeight;
        internal float ViewportWidth;
        internal float ViewportHeight;
        internal string BindingFailure = string.Empty;
        internal string BlessEvidence = string.Empty;

        public override string ToString()
        {
            return (Filters == null ? "filters=missing" : Filters.ToString()) +
                ";rows=" + InstantiatedRows + ";activeRows=" + ActiveRows +
                ";visibleRows=" + VisibleRows + ";content=" +
                ContentWidth.ToString("F1") + "x" + ContentHeight.ToString("F1") +
                ";viewport=" + ViewportWidth.ToString("F1") + "x" +
                ViewportHeight.ToString("F1") + ";selected=" + SelectedSourceId +
                ";detailsBound=" + SelectedDetailsBound + ";detailChildren=" +
                DetailChildren + ";bindingFailure=" + BindingFailure +
                ";Bless=" + BlessEvidence;
        }
    }
}
