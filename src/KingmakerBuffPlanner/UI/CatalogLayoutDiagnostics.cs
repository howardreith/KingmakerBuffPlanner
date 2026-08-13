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
        [JsonProperty("boundRowCount", Order = 5)] public int BoundRowCount { get; set; }
        [JsonProperty("sourceViewport", Order = 6)] public string SourceViewport { get; set; }
        [JsonProperty("sourceContent", Order = 7)] public string SourceContent { get; set; }
        [JsonProperty("maskEvidence", Order = 8)] public string MaskEvidence { get; set; }
        [JsonProperty("canaryEvidence", Order = 9)] public string CanaryEvidence { get; set; }
        [JsonProperty("rowEvidence", Order = 10)] public string[] RowEvidence { get; set; }
        [JsonProperty("detailsEvidence", Order = 11)] public string[] DetailsEvidence { get; set; }
        [JsonProperty("abilityIconCount", Order = 12)] public int AbilityIconCount { get; set; }
        [JsonProperty("missingIconCount", Order = 13)] public int MissingIconCount { get; set; }
        [JsonProperty("castingModeControlCount", Order = 14)] public int CastingModeControlCount { get; set; }
        [JsonProperty("retiredPrimaryLabelCount", Order = 15)] public int RetiredPrimaryLabelCount { get; set; }
        [JsonProperty("themeResolution", Order = 16)] public string ThemeResolution { get; set; }
        [JsonProperty("textRenderingEvidence", Order = 17)] public string TextRenderingEvidence { get; set; }
        [JsonProperty("nestedCanvasScalerCount", Order = 18)] public int NestedCanvasScalerCount { get; set; }
        [JsonProperty("fractionalRectCount", Order = 19)] public int FractionalRectCount { get; set; }
        [JsonProperty("pixelSnapEvidence", Order = 20)] public string PixelSnapEvidence { get; set; }
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
        internal int ProviderCount;
        internal int AggregateAbilityCount;
        internal int ConsolidatedCardCount;
        internal int DirectSelectedTargetCount;
        internal int IndirectCoveredTargetCount;

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
                ";providers=" + ProviderCount + ";aggregateAbilities=" +
                AggregateAbilityCount + ";consolidatedCards=" + ConsolidatedCardCount +
                ";directTargets=" + DirectSelectedTargetCount + ";indirectTargets=" +
                IndirectCoveredTargetCount + ";Bless=" + BlessEvidence;
        }
    }
}
