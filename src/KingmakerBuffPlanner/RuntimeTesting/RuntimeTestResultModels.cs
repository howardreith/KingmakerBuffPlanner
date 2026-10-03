using System.Collections.Generic;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Serialized runtime result contract. Unity-free so the source-only
    // suite can evaluate the same assertion producers the live host uses.
    internal sealed class RuntimeTestResult
    {
        [JsonProperty("schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
        [JsonProperty("runId", Order = 2)] public string RunId { get; set; }
        [JsonProperty("scenario", Order = 3)] public string Scenario { get; set; }
        [JsonProperty("profileId", Order = 100)] public string ProfileId { get; set; }
        [JsonProperty("status", Order = 4)] public string Status { get; set; }
        [JsonProperty("stage", Order = 5)] public string Stage { get; set; }
        [JsonProperty("loadedModId", Order = 6)] public string LoadedModId { get; set; }
        [JsonProperty("loadedModVersion", Order = 7)] public string LoadedModVersion { get; set; }
        [JsonProperty("commit", Order = 8)] public string Commit { get; set; }
        [JsonProperty("assemblyMvid", Order = 9)] public string AssemblyMvid { get; set; }
        [JsonProperty("assemblySha256", Order = 10)] public string AssemblySha256 { get; set; }
        [JsonProperty("packageSha256", Order = 11)] public string PackageSha256 { get; set; }
        [JsonProperty("gameVersion", Order = 12)] public string GameVersion { get; set; }
        [JsonProperty("gameExecutableSha256", Order = 13)] public string GameExecutableSha256 { get; set; }
        [JsonProperty("ummVersion", Order = 14)] public string UmmVersion { get; set; }
        [JsonProperty("ummSha256", Order = 15)] public string UmmSha256 { get; set; }
        [JsonProperty("harmonyVersion", Order = 16)] public string HarmonyVersion { get; set; }
        [JsonProperty("harmonySha256", Order = 17)] public string HarmonySha256 { get; set; }
        [JsonProperty("processId", Order = 18)] public int ProcessId { get; set; }
        [JsonProperty("startedAtUtc", Order = 19)] public string StartedAtUtc { get; set; }
        [JsonProperty("endedAtUtc", Order = 20)] public string EndedAtUtc { get; set; }
        [JsonProperty("exceptionSummary", Order = 21)] public string ExceptionSummary { get; set; }
        [JsonProperty("assertions", Order = 22)] public List<RuntimeTestAssertion> Assertions { get; set; }
        [JsonProperty("catalogSha256", Order = 23)] public string CatalogSha256 { get; set; }
        [JsonProperty("catalogAbilityCount", Order = 24)] public int CatalogAbilityCount { get; set; }
        [JsonProperty("catalogCandidateCount", Order = 25)] public int CatalogCandidateCount { get; set; }
        [JsonProperty("catalogDetectedEffectCount", Order = 26)] public int CatalogDetectedEffectCount { get; set; }
        [JsonProperty("catalogDiagnosticAbilityCount", Order = 27)] public int CatalogDiagnosticAbilityCount { get; set; }
        [JsonProperty("uiRootCount", Order = 28)] public int UiRootCount { get; set; }
        [JsonProperty("uiRenderedOpenFrames", Order = 29)] public int UiRenderedOpenFrames { get; set; }
        [JsonProperty("uiScreenWidth", Order = 30)] public int UiScreenWidth { get; set; }
        [JsonProperty("uiScreenHeight", Order = 31)] public int UiScreenHeight { get; set; }
        [JsonProperty("uiOpenCloseCycles", Order = 32)] public int UiOpenCloseCycles { get; set; }
        [JsonProperty("uiHudButtonCount", Order = 33)] public int UiHudButtonCount { get; set; }
        [JsonProperty("uiHudListenerCount", Order = 34)] public int UiHudListenerCount { get; set; }
        [JsonProperty("uiHudAnchorPath", Order = 35)] public string UiHudAnchorPath { get; set; }
        [JsonProperty("uiFullScreenRootCount", Order = 36)] public int UiFullScreenRootCount { get; set; }
        [JsonProperty("uiFullScreenOpaque", Order = 37)] public bool UiFullScreenOpaque { get; set; }
        [JsonProperty("uiReconstructionCount", Order = 38)] public int UiReconstructionCount { get; set; }
        [JsonProperty("catalogOptionalAbilityCount", Order = 39)] public int CatalogOptionalAbilityCount { get; set; }
        [JsonProperty("catalogOptionalCandidateCount", Order = 40)] public int CatalogOptionalCandidateCount { get; set; }
        [JsonProperty("catalogOptionalIncludedCount", Order = 41)] public int CatalogOptionalIncludedCount { get; set; }
        [JsonProperty("catalogOptionalUnsupportedCount", Order = 42)] public int CatalogOptionalUnsupportedCount { get; set; }
        [JsonProperty("optionalLoadedAssemblyCount", Order = 43)] public int OptionalLoadedAssemblyCount { get; set; }
        [JsonProperty("harmonyPatchInventorySha256", Order = 44)] public string HarmonyPatchInventorySha256 { get; set; }
        [JsonProperty("harmonyPatchTargetCount", Order = 45)] public int HarmonyPatchTargetCount { get; set; }
        [JsonProperty("harmonyPatchRecordCount", Order = 46)] public int HarmonyPatchRecordCount { get; set; }
        [JsonProperty("harmonyMultiOwnerTargetCount", Order = 47)] public int HarmonyMultiOwnerTargetCount { get; set; }
        [JsonProperty("harmonyBuffPlannerOverlapTargetCount", Order = 48)] public int HarmonyBuffPlannerOverlapTargetCount { get; set; }
        [JsonProperty("optionalLoadedUmmEntryCount", Order = 49)] public int OptionalLoadedUmmEntryCount { get; set; }
        [JsonProperty("nativeUiContractSha256", Order = 50)] public string NativeUiContractSha256 { get; set; }
        [JsonProperty("nativeUiButtonCount", Order = 51)] public int NativeUiButtonCount { get; set; }
        [JsonProperty("nativeUiCandidateAnchorCount", Order = 52)] public int NativeUiCandidateAnchorCount { get; set; }
        [JsonProperty("performanceProfileSha256", Order = 52)] public string PerformanceProfileSha256 { get; set; }
        [JsonProperty("performanceMinimumFramesPerSecond", Order = 52)] public double PerformanceMinimumFramesPerSecond { get; set; }
        [JsonProperty("performanceAverageFramesPerSecond", Order = 52)] public double PerformanceAverageFramesPerSecond { get; set; }
        [JsonProperty("performanceHudObjectFindInvocationCount", Order = 52)] public long PerformanceHudObjectFindInvocationCount { get; set; }
        [JsonProperty("performanceHudObjectFindTotalMilliseconds", Order = 52)] public double PerformanceHudObjectFindTotalMilliseconds { get; set; }
        [JsonProperty("uiFullScreenBlocksRaycasts", Order = 53)] public bool UiFullScreenBlocksRaycasts { get; set; }
        [JsonProperty("uiGraphicRaycasterPresent", Order = 54)] public bool UiGraphicRaycasterPresent { get; set; }
        [JsonProperty("uiPlannerOpen", Order = 55)] public bool UiPlannerOpen { get; set; }
        [JsonProperty("uiFullScreenModeActive", Order = 56)] public bool UiFullScreenModeActive { get; set; }
        [JsonProperty("uiSelectionDisabled", Order = 57)] public bool UiSelectionDisabled { get; set; }
        [JsonProperty("uiEventSystemPresent", Order = 58)] public bool UiEventSystemPresent { get; set; }
        [JsonProperty("uiInputLeaseAcquireCount", Order = 59)] public int UiInputLeaseAcquireCount { get; set; }
        [JsonProperty("uiInputLeaseReleaseCount", Order = 60)] public int UiInputLeaseReleaseCount { get; set; }
        [JsonProperty("uiInputLeaseReleaseCountAfterClose", Order = 61)] public int UiInputLeaseReleaseCountAfterClose { get; set; }
        [JsonProperty("uiScreenCreateCount", Order = 62)] public int UiScreenCreateCount { get; set; }
        [JsonProperty("uiScreenDestroyCount", Order = 63)] public int UiScreenDestroyCount { get; set; }
        [JsonProperty("uiHudInstallCount", Order = 64)] public int UiHudInstallCount { get; set; }
        [JsonProperty("uiHudDestroyCount", Order = 65)] public int UiHudDestroyCount { get; set; }
        [JsonProperty("uiNativeCampaignUiAvailable", Order = 66)] public bool UiNativeCampaignUiAvailable { get; set; }
        [JsonProperty("uiFullScreenModeActiveAfterClose", Order = 67)] public bool UiFullScreenModeActiveAfterClose { get; set; }
        [JsonProperty("uiSelectionDisabledAfterClose", Order = 68)] public bool UiSelectionDisabledAfterClose { get; set; }
        [JsonProperty("uiPointerEventCount", Order = 69)] public int UiPointerEventCount { get; set; }
        [JsonProperty("uiScrollEventCount", Order = 70)] public int UiScrollEventCount { get; set; }
        [JsonProperty("uiDragEventCount", Order = 71)] public int UiDragEventCount { get; set; }
        [JsonProperty("uiLongPointerEventCount", Order = 72)] public int UiLongPointerEventCount { get; set; }
        [JsonProperty("uiLongListenerCount", Order = 73)] public int UiLongListenerCount { get; set; }
        [JsonProperty("uiLongGroupResolvedCount", Order = 74)] public int UiLongGroupResolvedCount { get; set; }
        [JsonProperty("uiLongPlanRevalidatedCount", Order = 75)] public int UiLongPlanRevalidatedCount { get; set; }
        [JsonProperty("uiLongExecutionInvokedCount", Order = 76)] public int UiLongExecutionInvokedCount { get; set; }
        [JsonProperty("uiLongRefusalCount", Order = 77)] public int UiLongRefusalCount { get; set; }
        [JsonProperty("uiLongResultPresentedCount", Order = 78)] public int UiLongResultPresentedCount { get; set; }
        [JsonProperty("uiLongResultMessage", Order = 79)] public string UiLongResultMessage { get; set; }
        [JsonProperty("uiSetupTooltip", Order = 80)] public string UiSetupTooltip { get; set; }
        [JsonProperty("uiLongTooltip", Order = 81)] public string UiLongTooltip { get; set; }
        [JsonProperty("uiInputPlayerCommandCount", Order = 82)] public int UiInputPlayerCommandCount { get; set; }
        [JsonProperty("uiInputMovementCommandCount", Order = 83)] public int UiInputMovementCommandCount { get; set; }
        [JsonProperty("uiInputAbilityCommandCount", Order = 84)] public int UiInputAbilityCommandCount { get; set; }
        [JsonProperty("uiInputSelectionEventCount", Order = 85)] public int UiInputSelectionEventCount { get; set; }
        [JsonProperty("uiInputAbilityTargetEventCount", Order = 86)] public int UiInputAbilityTargetEventCount { get; set; }
        [JsonProperty("uiInputSelectionUnchanged", Order = 87)] public bool UiInputSelectionUnchanged { get; set; }
        [JsonProperty("uiInputCameraUnchanged", Order = 88)] public bool UiInputCameraUnchanged { get; set; }
        [JsonProperty("uiInputScrollConsumed", Order = 89)] public bool UiInputScrollConsumed { get; set; }
        [JsonProperty("uiInputCancelConsumed", Order = 90)] public bool UiInputCancelConsumed { get; set; }
        [JsonProperty("uiGroupSelectorChanged", Order = 91)] public bool UiGroupSelectorChanged { get; set; }
        [JsonProperty("uiPausedBeforeOpen", Order = 92)] public bool UiPausedBeforeOpen { get; set; }
        [JsonProperty("uiPausedAfterClose", Order = 93)] public bool UiPausedAfterClose { get; set; }
        [JsonProperty("uiSelectionDisabledBeforeOpen", Order = 94)] public bool UiSelectionDisabledBeforeOpen { get; set; }
        [JsonProperty("uiModeBeforeOpen", Order = 95)] public string UiModeBeforeOpen { get; set; }
        [JsonProperty("uiModeAfterClose", Order = 96)] public string UiModeAfterClose { get; set; }
        [JsonProperty("uiHudRaycastCanvasPath", Order = 97)] public string UiHudRaycastCanvasPath { get; set; }
        [JsonProperty("uiHudButtonOrder", Order = 98)] public string UiHudButtonOrder { get; set; }
        [JsonProperty("uiHudRowAboveNativeCluster", Order = 99)] public bool UiHudRowAboveNativeCluster { get; set; }
        [JsonProperty("uiHudRowLeftAlignedWithNativeCluster", Order = 99)]
        public bool UiHudRowLeftAlignedWithNativeCluster { get; set; }
        [JsonProperty("uiHudGlyphsCentered", Order = 99)] public bool UiHudGlyphsCentered { get; set; }
        [JsonProperty("uiHudHitboxesOwnRaycasts", Order = 100)] public bool UiHudHitboxesOwnRaycasts { get; set; }
        [JsonProperty("uiHudUnderlyingNativeActivationCount", Order = 101)] public int UiHudUnderlyingNativeActivationCount { get; set; }
        [JsonProperty("uiPresentationValid", Order = 102)] public bool UiPresentationValid { get; set; }
        [JsonProperty("uiPresentationFailure", Order = 103)] public string UiPresentationFailure { get; set; }
        [JsonProperty("uiPresentationCoverage", Order = 104)] public float UiPresentationCoverage { get; set; }
        [JsonProperty("uiPresentationOwnsCenterRaycast", Order = 105)] public bool UiPresentationOwnsCenterRaycast { get; set; }
        [JsonProperty("uiPresentationDiagnostic", Order = 106)] public string UiPresentationDiagnostic { get; set; }
        [JsonProperty("uiPresentationValidatedCount", Order = 107)] public int UiPresentationValidatedCount { get; set; }
        [JsonProperty("uiPresentationValidatedOrder", Order = 108)] public int UiPresentationValidatedOrder { get; set; }
        [JsonProperty("uiInputLeaseAcquiredOrder", Order = 109)] public int UiInputLeaseAcquiredOrder { get; set; }
        [JsonProperty("uiLifecycleState", Order = 110)] public string UiLifecycleState { get; set; }
        [JsonProperty("uiLongPointerEnterCount", Order = 111)] public int UiLongPointerEnterCount { get; set; }
        [JsonProperty("uiHotkeyArmed", Order = 112)] public bool UiHotkeyArmed { get; set; }
        [JsonProperty("uiHotkeyKeydownCount", Order = 113)] public int UiHotkeyKeydownCount { get; set; }
        [JsonProperty("uiHudObjectEvidence", Order = 114)] public string UiHudObjectEvidence { get; set; }
        [JsonProperty("uiScreenDestroyCountAfterClose", Order = 115)] public int UiScreenDestroyCountAfterClose { get; set; }
        [JsonProperty("workingSaveDescriptor", Order = 116)] public string WorkingSaveDescriptor { get; set; }
        [JsonProperty("baselineSaveDescriptor", Order = 117)] public string BaselineSaveDescriptor { get; set; }
        [JsonProperty("workingSaveLoadActionCount", Order = 118)] public int WorkingSaveLoadActionCount { get; set; }
        [JsonProperty("uiInitialCatalogEvidence", Order = 119)] public string UiInitialCatalogEvidence { get; set; }
        [JsonProperty("uiCatalogEvidence", Order = 120)] public string UiCatalogEvidence { get; set; }
        [JsonProperty("uiCatalogVisibleViewModels", Order = 121)] public int UiCatalogVisibleViewModels { get; set; }
        [JsonProperty("uiCatalogInstantiatedRows", Order = 122)] public int UiCatalogInstantiatedRows { get; set; }
        [JsonProperty("uiCatalogActiveRows", Order = 123)] public int UiCatalogActiveRows { get; set; }
        [JsonProperty("uiCatalogVisibleRows", Order = 124)] public int UiCatalogVisibleRows { get; set; }
        [JsonProperty("uiCatalogSelectedDetailsBound", Order = 125)] public bool UiCatalogSelectedDetailsBound { get; set; }
        [JsonProperty("uiCatalogBlessEvidence", Order = 126)] public string UiCatalogBlessEvidence { get; set; }
        [JsonProperty("uiCatalogProviderCount", Order = 126)] public int UiCatalogProviderCount { get; set; }
        [JsonProperty("uiCatalogAggregateAbilityCount", Order = 126)] public int UiCatalogAggregateAbilityCount { get; set; }
        [JsonProperty("uiCatalogConsolidatedCardCount", Order = 126)] public int UiCatalogConsolidatedCardCount { get; set; }
        [JsonProperty("uiDirectSelectedTargetCount", Order = 126)] public int UiDirectSelectedTargetCount { get; set; }
        [JsonProperty("uiIndirectCoveredTargetCount", Order = 126)] public int UiIndirectCoveredTargetCount { get; set; }
        [JsonProperty("uiCatalogControlEvidence", Order = 126)] public string UiCatalogControlEvidence { get; set; }
        [JsonProperty("uiBlessSelectedAndConfigured", Order = 127)] public bool UiBlessSelectedAndConfigured { get; set; }
        [JsonProperty("uiTooltipStable", Order = 128)] public bool UiTooltipStable { get; set; }
        [JsonProperty("uiTooltipEvidence", Order = 129)] public string UiTooltipEvidence { get; set; }
        [JsonProperty("uiTooltipListenerCount", Order = 130)] public int UiTooltipListenerCount { get; set; }
        [JsonProperty("uiTooltipRaycastGraphicCount", Order = 131)] public int UiTooltipRaycastGraphicCount { get; set; }
        [JsonProperty("uiTooltipBlocksRaycasts", Order = 132)] public bool UiTooltipBlocksRaycasts { get; set; }
        [JsonProperty("uiTooltipNativeTriggerCount", Order = 132)] public int UiTooltipNativeTriggerCount { get; set; }
        [JsonProperty("uiTooltipUsesNativeParchmentPresentation", Order = 132)]
        public bool UiTooltipUsesNativeParchmentPresentation { get; set; }
        [JsonProperty("uiSetupOpenSoundCount", Order = 132)] public int UiSetupOpenSoundCount { get; set; }
        [JsonProperty("uiPhysicalInputPlayerCommandCount", Order = 133)] public int UiPhysicalInputPlayerCommandCount { get; set; }
        [JsonProperty("uiPhysicalInputMovementCommandCount", Order = 134)] public int UiPhysicalInputMovementCommandCount { get; set; }
        [JsonProperty("uiPhysicalInputAbilityCommandCount", Order = 135)] public int UiPhysicalInputAbilityCommandCount { get; set; }
        [JsonProperty("uiPhysicalInputSelectionEventCount", Order = 136)] public int UiPhysicalInputSelectionEventCount { get; set; }
        [JsonProperty("uiPhysicalInputAbilityTargetEventCount", Order = 137)] public int UiPhysicalInputAbilityTargetEventCount { get; set; }
        [JsonProperty("uiPhysicalInputSelectionUnchanged", Order = 138)] public bool UiPhysicalInputSelectionUnchanged { get; set; }
        [JsonProperty("uiPhysicalInputCameraUnchanged", Order = 139)] public bool UiPhysicalInputCameraUnchanged { get; set; }
        [JsonProperty("uiImportantResultMessage", Order = 140)] public string UiImportantResultMessage { get; set; }
        [JsonProperty("uiShortResultMessage", Order = 141)] public string UiShortResultMessage { get; set; }
        [JsonProperty("uiConfiguredLongResultMessage", Order = 142)] public string UiConfiguredLongResultMessage { get; set; }
        [JsonProperty("uiConfiguredLongDisposition", Order = 143)] public string UiConfiguredLongDisposition { get; set; }
        [JsonProperty("uiConfiguredLongPlanned", Order = 144)] public int UiConfiguredLongPlanned { get; set; }
        [JsonProperty("uiConfiguredLongSubmitted", Order = 145)] public int UiConfiguredLongSubmitted { get; set; }
        [JsonProperty("uiConfiguredLongConfirmed", Order = 146)] public int UiConfiguredLongConfirmed { get; set; }
        [JsonProperty("uiRenderExpectedNames", Order = 147)] public string[] UiRenderExpectedNames { get; set; }
        [JsonProperty("uiRenderRowScreenRectangles", Order = 148)] public string[] UiRenderRowScreenRectangles { get; set; }
        [JsonProperty("uiRenderSelectedRowName", Order = 149)] public string UiRenderSelectedRowName { get; set; }
        [JsonProperty("uiRenderDetailsTitleText", Order = 150)] public string UiRenderDetailsTitleText { get; set; }
        [JsonProperty("uiRenderBoundRowCount", Order = 151)] public int UiRenderBoundRowCount { get; set; }
        [JsonProperty("uiRenderMaskEvidence", Order = 152)] public string UiRenderMaskEvidence { get; set; }
        [JsonProperty("uiRenderCanaryEvidence", Order = 153)] public string UiRenderCanaryEvidence { get; set; }
        [JsonProperty("uiRenderRowEvidence", Order = 154)] public string[] UiRenderRowEvidence { get; set; }
        [JsonProperty("uiRenderDetailsEvidence", Order = 155)] public string[] UiRenderDetailsEvidence { get; set; }
        [JsonProperty("uiRenderScreenshotSha256", Order = 156)] public string UiRenderScreenshotSha256 { get; set; }
        [JsonProperty("uiSelectedDetailsScreenshotSha256", Order = 157)] public string UiSelectedDetailsScreenshotSha256 { get; set; }
        [JsonProperty("uiGridOverviewScreenshotSha256", Order = 158)] public string UiGridOverviewScreenshotSha256 { get; set; }
        [JsonProperty("uiTargetColorsScreenshotSha256", Order = 159)] public string UiTargetColorsScreenshotSha256 { get; set; }
        [JsonProperty("uiSettingsScreenshotSha256", Order = 160)] public string UiSettingsScreenshotSha256 { get; set; }
        [JsonProperty("uiHudScreenshotSha256", Order = 161)] public string UiHudScreenshotSha256 { get; set; }
        [JsonProperty("uiRenderAbilityIconCount", Order = 161)] public int UiRenderAbilityIconCount { get; set; }
        [JsonProperty("uiRenderMissingIconCount", Order = 162)] public int UiRenderMissingIconCount { get; set; }
        [JsonProperty("uiCastingModeControlCount", Order = 163)] public int UiCastingModeControlCount { get; set; }
        [JsonProperty("uiRetiredPrimaryLabelCount", Order = 164)] public int UiRetiredPrimaryLabelCount { get; set; }
        [JsonProperty("uiThemeResolution", Order = 165)] public string UiThemeResolution { get; set; }
        [JsonProperty("uiTextRenderingEvidence", Order = 166)] public string UiTextRenderingEvidence { get; set; }
        [JsonProperty("uiNestedPlannerCanvasScalerCount", Order = 167)]
        public int UiNestedPlannerCanvasScalerCount { get; set; }
        [JsonProperty("uiFractionalRectCount", Order = 168)] public int UiFractionalRectCount { get; set; }
        [JsonProperty("menuFrameScreenshotSha256", Order = 169)] public string MenuFrameScreenshotSha256 { get; set; }
        [JsonProperty("menuFrameEngineScreenshotSha256", Order = 170)] public string MenuFrameEngineScreenshotSha256 { get; set; }
        [JsonProperty("menuFrameWidth", Order = 171)] public int MenuFrameWidth { get; set; }
        [JsonProperty("menuFrameHeight", Order = 172)] public int MenuFrameHeight { get; set; }
        [JsonProperty("menuFrameLumaSummary", Order = 173)] public string MenuFrameLumaSummary { get; set; }
        [JsonProperty("menuFrameNonBlack", Order = 174)] public bool MenuFrameNonBlack { get; set; }
        [JsonProperty("menuFrameProgressSummary", Order = 175)] public string MenuFrameProgressSummary { get; set; }
        [JsonProperty("menuButtonInventory", Order = 176)] public string MenuButtonInventory { get; set; }
        [JsonProperty("menuClickTarget", Order = 177)] public string MenuClickTarget { get; set; }
        [JsonProperty("menuClickAcknowledged", Order = 178)] public bool MenuClickAcknowledged { get; set; }
        [JsonProperty("menuWindowOpened", Order = 179)] public bool MenuWindowOpened { get; set; }
        [JsonProperty("menuWindowDescriptor", Order = 180)] public string MenuWindowDescriptor { get; set; }
        [JsonProperty("menuWindowScreenshotSha256", Order = 181)] public string MenuWindowScreenshotSha256 { get; set; }
        [JsonProperty("menuEscape1ScreenshotSha256", Order = 182)] public string MenuEscape1ScreenshotSha256 { get; set; }
        [JsonProperty("menuEscape1ChangedFraction", Order = 183)] public double MenuEscape1ChangedFraction { get; set; }
        [JsonProperty("menuEscape1Acknowledged", Order = 184)] public bool MenuEscape1Acknowledged { get; set; }
        [JsonProperty("menuEscape2ScreenshotSha256", Order = 185)] public string MenuEscape2ScreenshotSha256 { get; set; }
        [JsonProperty("menuEscape2ChangedFraction", Order = 186)] public double MenuEscape2ChangedFraction { get; set; }
        [JsonProperty("menuEscape2Acknowledged", Order = 187)] public bool MenuEscape2Acknowledged { get; set; }
        [JsonProperty("saveChainHandlerInvocations", Order = 188)] public int SaveChainHandlerInvocations { get; set; }
        [JsonProperty("saveChainCatalogInvocations", Order = 189)] public int SaveChainCatalogInvocations { get; set; }
        [JsonProperty("saveChainCatalogDescriptorCount", Order = 190)] public int SaveChainCatalogDescriptorCount { get; set; }
        [JsonProperty("saveChainWorkingMatchCount", Order = 191)] public int SaveChainWorkingMatchCount { get; set; }
        [JsonProperty("saveChainBaselineMatchCount", Order = 192)] public int SaveChainBaselineMatchCount { get; set; }
        [JsonProperty("saveChainSlotReceiverCorrelated", Order = 193)] public bool SaveChainSlotReceiverCorrelated { get; set; }
        [JsonProperty("saveChainWindowReceiverCorrelated", Order = 194)] public bool SaveChainWindowReceiverCorrelated { get; set; }
        [JsonProperty("saveChainWindowArgumentCorrelated", Order = 195)] public bool SaveChainWindowArgumentCorrelated { get; set; }
        [JsonProperty("saveChainLoadEntryCorrelated", Order = 196)] public bool SaveChainLoadEntryCorrelated { get; set; }
        [JsonProperty("saveChainCompletionCallback", Order = 197)] public bool SaveChainCompletionCallback { get; set; }
        [JsonProperty("saveChainNoUnexpectedWrite", Order = 198)] public bool SaveChainNoUnexpectedWrite { get; set; }
        [JsonProperty("saveChainSequences", Order = 199)] public string SaveChainSequences { get; set; }
        [JsonProperty("saveChainFingerprint", Order = 200)] public string SaveChainFingerprint { get; set; }
    }

    internal sealed class RuntimeTestAssertion
    {
        [JsonProperty("id", Order = 1)] public string Id { get; set; }
        [JsonProperty("expected", Order = 2)] public string Expected { get; set; }
        [JsonProperty("observed", Order = 3)] public string Observed { get; set; }
        [JsonProperty("status", Order = 4)] public string Status { get; set; }

        internal static RuntimeTestAssertion Pass(string id, string expected, string observed)
        {
            return new RuntimeTestAssertion
            {
                Id = id,
                Expected = expected,
                Observed = observed,
                Status = "PASS"
            };
        }

        internal static RuntimeTestAssertion Fail(string id, string expected, string observed)
        {
            return new RuntimeTestAssertion
            {
                Id = id,
                Expected = expected,
                Observed = observed,
                Status = "FAIL"
            };
        }
    }
}
