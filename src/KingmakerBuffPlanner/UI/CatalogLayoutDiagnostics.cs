namespace KingmakerBuffPlanner.UI
{
    internal sealed class LiveRowRenderDiagnostics
    {
        public string[] ExpectedNames { get; set; }
        public string[] RowScreenRectangles { get; set; }
        public string SelectedRowName { get; set; }
        public string DetailsTitleText { get; set; }
        public string SourceViewport { get; set; }
        public string SourceContent { get; set; }
        public string MaskEvidence { get; set; }
        public string CanaryEvidence { get; set; }
        public string[] RowEvidence { get; set; }
        public string[] DetailsEvidence { get; set; }
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
