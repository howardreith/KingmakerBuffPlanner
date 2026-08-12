using Newtonsoft.Json;

namespace KingmakerBuffPlanner.UI
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class LiveRowRenderDiagnostics
    {
        [JsonProperty("expectedNames", Order = 1)] public string[] ExpectedNames { get; set; }
        [JsonProperty("rowScreenRectangles", Order = 2)] public string[] RowScreenRectangles { get; set; }
        [JsonProperty("selectedRowName", Order = 3)] public string SelectedRowName { get; set; }
        [JsonProperty("detailsTitleText", Order = 4)] public string DetailsTitleText { get; set; }
        [JsonProperty("sourceViewport", Order = 5)] public string SourceViewport { get; set; }
        [JsonProperty("sourceContent", Order = 6)] public string SourceContent { get; set; }
        [JsonProperty("maskEvidence", Order = 7)] public string MaskEvidence { get; set; }
        [JsonProperty("canaryEvidence", Order = 8)] public string CanaryEvidence { get; set; }
        [JsonProperty("rowEvidence", Order = 9)] public string[] RowEvidence { get; set; }
        [JsonProperty("detailsEvidence", Order = 10)] public string[] DetailsEvidence { get; set; }
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
