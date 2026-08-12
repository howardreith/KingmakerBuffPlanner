using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.UI;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal sealed class RuntimeTestHost
    {
        private readonly RuntimeTestRequest _request;
        private readonly UnityModManager.ModEntry _modEntry;
        private readonly ModLog _log;
        private readonly DateTime _startedAtUtc;
        private bool _completed;
        private int _uiSmokeUpdates;
        private LiveCampaignSaveLoader _liveSaveLoader;
        private int _liveUiPhase;
        private int _liveCycleCount;
        private bool _liveCycleOpening;
        private bool _liveF10MarkerWritten;
        private bool _liveUmmDismissMarkerWritten;

        private RuntimeTestHost(
            RuntimeTestRequest request,
            UnityModManager.ModEntry modEntry,
            ModLog log)
        {
            _request = request;
            _modEntry = modEntry;
            _log = log;
            _startedAtUtc = DateTime.UtcNow;
        }

        internal static RuntimeTestHost TryCreate(
            string[] arguments,
            UnityModManager.ModEntry modEntry,
            ModLog log)
        {
            string rejection;
            RuntimeTestRequest request = RuntimeTestProtocol.TryRead(arguments, out rejection);
            if (request != null) return new RuntimeTestHost(request, modEntry, log);
            if (!string.IsNullOrEmpty(rejection)) log.Info("Runtime request rejected: " + rejection);
            return null;
        }

        internal bool Update()
        {
            if (_completed) return true;
            if (RuntimeTestProtocol.IsLiveUiScenario(_request.Scenario))
            {
                try
                {
                    if (!UpdateLiveUiScenario()) return false;
                }
                catch (Exception exception)
                {
                    _completed = true;
                    _log.Error("Live UI runtime scenario failed.", exception);
                    TryWriteFailure(_startedAtUtc, exception);
                    if (_request.ExitAfterCompletion) Application.Quit();
                    return true;
                }
            }
            if (RuntimeTestProtocol.IsCatalogScenario(_request.Scenario) &&
                ResourcesLibrary.LibraryObject == null)
                return false;
            if (RuntimeTestProtocol.IsUiScenario(_request.Scenario) &&
                !RuntimeTestProtocol.IsLiveUiScenario(_request.Scenario))
            {
                _uiSmokeUpdates++;
                if (StaticCanvas.Instance == null || UnityEngine.EventSystems.EventSystem.current == null)
                {
                    if (_uiSmokeUpdates < 600) return false;
                }
                else if (_uiSmokeUpdates <= 40)
                {
                    if ((_uiSmokeUpdates % 2) == 1) BuffPlannerUiRoot.BeginRuntimeSmoke();
                    else BuffPlannerUiRoot.CloseRuntimeSmoke();
                    return false;
                }
                else if (_uiSmokeUpdates == 41)
                {
                    BuffPlannerUiRoot.ReconstructRuntimeSmoke();
                    return false;
                }
                else if (_uiSmokeUpdates == 42)
                {
                    BuffPlannerUiRoot.DispatchRuntimeInputSmoke();
                    return false;
                }
                else if (_uiSmokeUpdates < 45) return false;
            }
            if (RuntimeTestProtocol.IsNativeUiProbeScenario(_request.Scenario))
            {
                _uiSmokeUpdates++;
                if (!NativeUiContractProbe.IsReady && _uiSmokeUpdates < 600) return false;
            }
            _completed = true;
            DateTime started = _startedAtUtc;
            try
            {
                if (RuntimeTestProtocol.IsUiScenario(_request.Scenario) &&
                    (StaticCanvas.Instance == null || UnityEngine.EventSystems.EventSystem.current == null))
                    throw new InvalidOperationException(
                        "Campaign UI is required for the full-screen input-isolation scenario.");
                Assembly assembly = typeof(Main).Assembly;
                string assemblyPath = assembly.Location;
                string dllHash = Hashing.Sha256(assemblyPath);
                string gameRoot = RuntimePaths.GetGameRoot(_modEntry.Path);
                string managed = Path.Combine(gameRoot, "Kingmaker_Data", "Managed");
                string gameExecutable = Path.Combine(gameRoot, "Kingmaker.exe");
                string umm = Path.Combine(managed, "UnityModManager", "UnityModManager.dll");
                string harmony = Path.Combine(managed, "UnityModManager", "0Harmony12.dll");
                bool dllMatches = string.Equals(
                    dllHash, _request.ExpectedDllSha256, StringComparison.Ordinal);
                NativeCatalogExport catalog = null;
                string catalogPath = null;
                string catalogHash = null;
                HarmonyPatchInventory harmonyInventory = null;
                string harmonyInventoryHash = null;
                UiRootDiagnostics ui = null;
                NativeUiContract nativeUiContract = null;
                string nativeUiContractHash = null;
                if (dllMatches && RuntimeTestProtocol.IsCatalogScenario(_request.Scenario))
                {
                    string modsPath = Path.GetDirectoryName(_modEntry.Path.TrimEnd(
                        Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    catalog = new NativeCatalogExporter(EffectOverrideRegistry.Load(
                        Path.Combine(_modEntry.Path, "NativeEffectOverrides.json")),
                        _request.ProfileId,
                        BlueprintOwnershipIndex.Load(modsPath, _request.ProfileId)).Export();
                    catalogPath = Path.Combine(_request.EvidenceDirectory, "native-buff-catalog.json");
                    string catalogJson = Serialize(catalog);
                    JObject catalogDocument = JObject.Parse(catalogJson);
                    JArray abilityDocuments = catalogDocument["abilities"] as JArray;
                    if ((int)catalogDocument["schemaVersion"] != 4 || abilityDocuments == null ||
                        abilityDocuments.Count != catalog.AbilityCount)
                        throw new InvalidDataException("Serialized catalog contract did not reconcile.");
                    foreach (JObject abilityDocument in abilityDocuments.OfType<JObject>())
                    {
                        var expressionDocument = abilityDocument["expression"] as JObject;
                        if (expressionDocument == null ||
                            string.IsNullOrWhiteSpace((string)expressionDocument["expressionType"]))
                            throw new InvalidDataException("Serialized effect expression lost its discriminator.");
                    }
                    AtomicFile.WriteUtf8(catalogPath, catalogJson);
                    catalogHash = Hashing.Sha256(catalogPath);
                    harmonyInventory = new HarmonyPatchInventoryExporter().Export(_request.ProfileId, harmony);
                    string harmonyInventoryPath = Path.Combine(
                        _request.EvidenceDirectory, "harmony-patch-inventory.json");
                    AtomicFile.WriteUtf8(harmonyInventoryPath, Serialize(harmonyInventory));
                    harmonyInventoryHash = Hashing.Sha256(harmonyInventoryPath);
                }
                if (dllMatches && RuntimeTestProtocol.IsUiScenario(_request.Scenario))
                    ui = BuffPlannerUiRoot.EndRuntimeSmoke();
                if (dllMatches && RuntimeTestProtocol.IsNativeUiProbeScenario(_request.Scenario))
                {
                    nativeUiContract = NativeUiContractProbe.Capture();
                    string nativeUiContractPath = Path.Combine(
                        _request.EvidenceDirectory, "native-ui-contract.json");
                    AtomicFile.WriteUtf8(nativeUiContractPath, Serialize(nativeUiContract));
                    nativeUiContractHash = Hashing.Sha256(nativeUiContractPath);
                }
                var result = new RuntimeTestResult
                {
                    SchemaVersion = 1,
                    RunId = _request.RunId,
                    Scenario = _request.Scenario,
                    ProfileId = _request.ProfileId,
                    Status = dllMatches ? "PASS" : "FAIL",
                    Stage = dllMatches ? "completed" : "identity-validation",
                    LoadedModId = _modEntry.Info.Id,
                    LoadedModVersion = _modEntry.Info.Version,
                    Commit = BuildInfo.Commit,
                    AssemblyMvid = assembly.ManifestModule.ModuleVersionId.ToString("D"),
                    AssemblySha256 = dllHash,
                    PackageSha256 = _request.ExpectedPackageSha256,
                    GameVersion = UnityModManager.gameVersion.ToString(),
                    GameExecutableSha256 = Hashing.Sha256(gameExecutable),
                    UmmVersion = UnityModManager.GetVersion().ToString(),
                    UmmSha256 = Hashing.Sha256(umm),
                    HarmonyVersion = FileVersionInfo.GetVersionInfo(harmony).FileVersion,
                    HarmonySha256 = Hashing.Sha256(harmony),
                    ProcessId = Process.GetCurrentProcess().Id,
                    StartedAtUtc = started.ToString("o"),
                    EndedAtUtc = DateTime.UtcNow.ToString("o"),
                    CatalogSha256 = catalogHash,
                    CatalogAbilityCount = catalog == null ? 0 : catalog.AbilityCount,
                    CatalogCandidateCount = catalog == null ? 0 : catalog.CandidateCount,
                    CatalogDetectedEffectCount = catalog == null ? 0 : catalog.DetectedEffectCount,
                    CatalogDiagnosticAbilityCount = catalog == null ? 0 : catalog.DiagnosticAbilityCount,
                    CatalogOptionalAbilityCount = catalog == null ? 0 : catalog.OptionalAbilityCount,
                    CatalogOptionalCandidateCount = catalog == null ? 0 : catalog.OptionalCandidateCount,
                    CatalogOptionalIncludedCount = catalog == null ? 0 : catalog.OptionalIncludedCount,
                    CatalogOptionalUnsupportedCount = catalog == null ? 0 : catalog.OptionalUnsupportedCount,
                    HarmonyPatchInventorySha256 = harmonyInventoryHash,
                    HarmonyPatchTargetCount = harmonyInventory == null ? 0 : harmonyInventory.TargetCount,
                    HarmonyPatchRecordCount = harmonyInventory == null ? 0 : harmonyInventory.PatchCount,
                    HarmonyMultiOwnerTargetCount = harmonyInventory == null ? 0 : harmonyInventory.MultiOwnerTargetCount,
                    HarmonyBuffPlannerOverlapTargetCount = harmonyInventory == null
                        ? 0 : harmonyInventory.BuffPlannerOverlapTargetCount,
                    UiRootCount = ui == null ? 0 : ui.RootCount,
                    UiRenderedOpenFrames = ui == null ? 0 : ui.RenderedOpenFrames,
                    UiOpenCloseCycles = ui == null ? 0 : ui.OpenCloseCycles,
                    UiScreenWidth = ui == null ? 0 : ui.ScreenWidth,
                    UiScreenHeight = ui == null ? 0 : ui.ScreenHeight,
                    UiHudButtonCount = ui == null ? 0 : ui.HudButtonCount,
                    UiHudListenerCount = ui == null ? 0 : ui.HudListenerCount,
                    UiHudAnchorPath = ui == null ? null : ui.HudAnchorPath,
                    UiHudRaycastCanvasPath = ui == null ? null : ui.HudRaycastCanvasPath,
                    UiHudButtonOrder = ui == null ? null : ui.HudButtonOrder,
                    UiHudRowAboveNativeCluster = ui != null && ui.HudRowAboveNativeCluster,
                    UiHudHitboxesOwnRaycasts = ui != null && ui.HudHitboxesOwnRaycasts,
                    UiHudUnderlyingNativeActivationCount = ui == null ? -1 : ui.HudUnderlyingNativeActivationCount,
                    UiFullScreenRootCount = ui == null ? 0 : ui.FullScreenRootCount,
                    UiFullScreenOpaque = ui != null && ui.FullScreenOpaque,
                    UiFullScreenBlocksRaycasts = ui != null && ui.FullScreenBlocksRaycasts,
                    UiGraphicRaycasterPresent = ui != null && ui.GraphicRaycasterPresent,
                    UiPresentationValid = ui != null && ui.PresentationValid,
                    UiPresentationFailure = ui == null ? null : ui.PresentationFailure,
                    UiPresentationCoverage = ui == null ? 0 : ui.PresentationCoverage,
                    UiPresentationOwnsCenterRaycast = ui != null && ui.PresentationOwnsCenterRaycast,
                    UiPresentationDiagnostic = ui == null ? null : ui.PresentationDiagnostic,
                    UiPresentationValidatedCount = ui == null ? 0 : ui.PresentationValidatedCount,
                    UiPresentationValidatedOrder = ui == null ? 0 : ui.PresentationValidatedOrder,
                    UiInputLeaseAcquiredOrder = ui == null ? 0 : ui.InputLeaseAcquiredOrder,
                    UiLifecycleState = ui == null ? null : ui.LifecycleState,
                    UiPlannerOpen = ui != null && ui.PlannerOpen,
                    UiFullScreenModeActive = ui != null && ui.FullScreenModeActive,
                    UiSelectionDisabled = ui != null && ui.SelectionDisabled,
                    UiEventSystemPresent = ui != null && ui.EventSystemPresent,
                    UiInputLeaseAcquireCount = ui == null ? 0 : ui.InputLeaseAcquireCount,
                    UiInputLeaseReleaseCount = ui == null ? 0 : ui.InputLeaseReleaseCount,
                    UiInputLeaseReleaseCountAfterClose = ui == null ? 0 : ui.InputLeaseReleaseCountAfterClose,
                    UiScreenCreateCount = ui == null ? 0 : ui.ScreenCreateCount,
                    UiScreenDestroyCount = ui == null ? 0 : ui.ScreenDestroyCount,
                    UiHudInstallCount = ui == null ? 0 : ui.HudInstallCount,
                    UiHudDestroyCount = ui == null ? 0 : ui.HudDestroyCount,
                    UiReconstructionCount = ui == null ? 0 : ui.ReconstructionCount,
                    UiNativeCampaignUiAvailable = ui != null && ui.NativeCampaignUiAvailable,
                    UiFullScreenModeActiveAfterClose = ui != null && ui.FullScreenModeActiveAfterClose,
                    UiSelectionDisabledAfterClose = ui != null && ui.SelectionDisabledAfterClose,
                    UiPointerEventCount = ui == null ? 0 : ui.PointerEventCount,
                    UiScrollEventCount = ui == null ? 0 : ui.ScrollEventCount,
                    UiDragEventCount = ui == null ? 0 : ui.DragEventCount,
                    UiLongPointerEventCount = ui == null ? 0 : ui.LongPointerEventCount,
                    UiLongPointerEnterCount = ui == null ? 0 : ui.LongPointerEnterCount,
                    UiLongListenerCount = ui == null ? 0 : ui.LongListenerCount,
                    UiLongGroupResolvedCount = ui == null ? 0 : ui.LongGroupResolvedCount,
                    UiLongPlanRevalidatedCount = ui == null ? 0 : ui.LongPlanRevalidatedCount,
                    UiLongExecutionInvokedCount = ui == null ? 0 : ui.LongExecutionInvokedCount,
                    UiLongRefusalCount = ui == null ? 0 : ui.LongRefusalCount,
                    UiLongResultPresentedCount = ui == null ? 0 : ui.LongResultPresentedCount,
                    UiLongResultMessage = ui == null ? null : ui.LongResultMessage,
                    UiSetupTooltip = ui == null ? null : ui.SetupTooltip,
                    UiLongTooltip = ui == null ? null : ui.LongTooltip,
                    UiInputPlayerCommandCount = ui == null ? -1 : ui.InputPlayerCommandCount,
                    UiInputMovementCommandCount = ui == null ? -1 : ui.InputMovementCommandCount,
                    UiInputAbilityCommandCount = ui == null ? -1 : ui.InputAbilityCommandCount,
                    UiInputSelectionEventCount = ui == null ? -1 : ui.InputSelectionEventCount,
                    UiInputAbilityTargetEventCount = ui == null ? -1 : ui.InputAbilityTargetEventCount,
                    UiInputSelectionUnchanged = ui != null && ui.InputSelectionUnchanged,
                    UiInputCameraUnchanged = ui != null && ui.InputCameraUnchanged,
                    UiInputScrollConsumed = ui != null && ui.InputScrollConsumed,
                    UiInputCancelConsumed = ui != null && ui.InputCancelConsumed,
                    UiGroupSelectorChanged = ui != null && ui.GroupSelectorChanged,
                    UiPausedBeforeOpen = ui != null && ui.PausedBeforeOpen,
                    UiPausedAfterClose = ui != null && ui.PausedAfterClose,
                    UiSelectionDisabledBeforeOpen = ui != null && ui.SelectionDisabledBeforeOpen,
                    UiModeBeforeOpen = ui == null ? null : ui.ModeBeforeOpen,
                    UiModeAfterClose = ui == null ? null : ui.ModeAfterClose,
                    UiF10Armed = ui != null && ui.F10Armed,
                    UiF10KeydownCount = ui == null ? 0 : ui.F10KeydownCount,
                    UiHudObjectEvidence = ui == null ? null : ui.HudObjectEvidence,
                    UiScreenDestroyCountAfterClose = ui == null ? 0 : ui.ScreenDestroyCountAfterClose,
                    WorkingSaveDescriptor = _liveSaveLoader == null ? null : _liveSaveLoader.WorkingDescriptor,
                    BaselineSaveDescriptor = _liveSaveLoader == null ? null : _liveSaveLoader.BaselineDescriptor,
                    WorkingSaveLoadActionCount = _liveSaveLoader == null ? 0 : _liveSaveLoader.LoadActionCount,
                    NativeUiContractSha256 = nativeUiContractHash,
                    NativeUiButtonCount = nativeUiContract == null ? 0 : nativeUiContract.Buttons.Count,
                    NativeUiCandidateAnchorCount = nativeUiContract == null
                        ? 0 : nativeUiContract.CandidateAnchors.Count,
                    Assertions = new List<RuntimeTestAssertion>
                    {
                        RuntimeTestAssertion.Pass("entry-point-loaded", "true", "true"),
                        RuntimeTestAssertion.Pass("standalone-id", "KingmakerBuffPlanner", _modEntry.Info.Id),
                        RuntimeTestAssertion.Pass("version", BuildInfo.Version, _modEntry.Info.Version),
                        RuntimeTestAssertion.Pass("commit", _request.ExpectedCommit, BuildInfo.Commit),
                        dllMatches
                            ? RuntimeTestAssertion.Pass("dll-sha256", _request.ExpectedDllSha256, dllHash)
                            : RuntimeTestAssertion.Fail("dll-sha256", _request.ExpectedDllSha256, dllHash)
                    }
                };
                if (RuntimeTestProtocol.IsNativeUiProbeScenario(_request.Scenario))
                {
                    bool validProbe = nativeUiContract != null &&
                        !string.IsNullOrEmpty(nativeUiContract.EventSystemPath) &&
                        !string.IsNullOrEmpty(nativeUiContract.StaticCanvasPath) &&
                        !string.IsNullOrEmpty(nativeUiContract.ServiceWindowTabsPath) &&
                        nativeUiContract.Buttons.Count > 0 && nativeUiContract.Raycasters.Count > 0;
                    result.Assertions.Add(validProbe
                        ? RuntimeTestAssertion.Pass("native-ui-contract", "complete", nativeUiContractHash)
                        : RuntimeTestAssertion.Fail("native-ui-contract", "complete", "incomplete"));
                    if (!validProbe)
                    {
                        result.Status = "FAIL";
                        result.Stage = "native-ui-contract-validation";
                    }
                }
                int loadedOptionalAssemblies = 0;
                int loadedOptionalUmmEntries = 0;
                bool optionalIdentityFailed = false;
                int plannerAssemblyCount = AppDomain.CurrentDomain.GetAssemblies().Count(a =>
                    string.Equals(a.GetName().Name, "KingmakerBuffPlanner", StringComparison.Ordinal));
                result.Assertions.Add(plannerAssemblyCount == 1
                    ? RuntimeTestAssertion.Pass("buff-planner-assembly-unique", "1", "1")
                    : RuntimeTestAssertion.Fail("buff-planner-assembly-unique", "1", plannerAssemblyCount.ToString()));
                int plannerEntryCount = UnityModManager.modEntries.Count(e =>
                    e != null && e.Info != null &&
                    string.Equals(e.Info.Id, "KingmakerBuffPlanner", StringComparison.Ordinal));
                result.Assertions.Add(plannerEntryCount == 1
                    ? RuntimeTestAssertion.Pass("buff-planner-umm-entry-unique", "1", "1")
                    : RuntimeTestAssertion.Fail("buff-planner-umm-entry-unique", "1", plannerEntryCount.ToString()));
                if (plannerAssemblyCount != 1 || plannerEntryCount != 1) optionalIdentityFailed = true;
                foreach (RuntimeExpectedOptionalMod expected in _request.ExpectedOptionalMods)
                {
                    string expectedAssemblyName = Path.GetFileNameWithoutExtension(expected.AssemblyName);
                    List<Assembly> loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a =>
                        string.Equals(a.GetName().Name, expectedAssemblyName, StringComparison.Ordinal)).ToList();
                    result.Assertions.Add(loadedAssemblies.Count == 1
                        ? RuntimeTestAssertion.Pass("optional-assembly-unique:" + expected.UmmId, "1", "1")
                        : RuntimeTestAssertion.Fail("optional-assembly-unique:" + expected.UmmId, "1",
                            loadedAssemblies.Count.ToString()));
                    if (loadedAssemblies.Count != 1)
                    {
                        optionalIdentityFailed = true;
                        continue;
                    }
                    Assembly loaded = loadedAssemblies[0];
                    loadedOptionalAssemblies++;
                    string loadedHash = Hashing.Sha256(loaded.Location);
                    result.Assertions.Add(RuntimeTestAssertion.Pass(
                        "optional-assembly-loaded:" + expected.UmmId, expectedAssemblyName,
                        loaded.GetName().Name));
                    if (string.Equals(loadedHash, expected.AssemblySha256, StringComparison.Ordinal))
                        result.Assertions.Add(RuntimeTestAssertion.Pass(
                            "optional-assembly-sha256:" + expected.UmmId,
                            expected.AssemblySha256, loadedHash));
                    else
                    {
                        optionalIdentityFailed = true;
                        result.Assertions.Add(RuntimeTestAssertion.Fail(
                            "optional-assembly-sha256:" + expected.UmmId,
                            expected.AssemblySha256, loadedHash));
                    }
                    List<UnityModManager.ModEntry> optionalEntries = UnityModManager.modEntries.Where(e =>
                        e != null && e.Info != null &&
                        string.Equals(e.Info.Id, expected.UmmId, StringComparison.Ordinal)).ToList();
                    result.Assertions.Add(optionalEntries.Count == 1
                        ? RuntimeTestAssertion.Pass("optional-umm-entry-unique:" + expected.UmmId, "1", "1")
                        : RuntimeTestAssertion.Fail("optional-umm-entry-unique:" + expected.UmmId, "1",
                            optionalEntries.Count.ToString()));
                    if (optionalEntries.Count != 1)
                    {
                        optionalIdentityFailed = true;
                        continue;
                    }
                    loadedOptionalUmmEntries++;
                    string loadedVersion = optionalEntries[0].Info.Version;
                    if (string.Equals(loadedVersion, expected.Version, StringComparison.Ordinal))
                        result.Assertions.Add(RuntimeTestAssertion.Pass(
                            "optional-umm-version:" + expected.UmmId, expected.Version, loadedVersion));
                    else
                    {
                        optionalIdentityFailed = true;
                        result.Assertions.Add(RuntimeTestAssertion.Fail(
                            "optional-umm-version:" + expected.UmmId, expected.Version, loadedVersion));
                    }
                }
                result.OptionalLoadedAssemblyCount = loadedOptionalAssemblies;
                result.OptionalLoadedUmmEntryCount = loadedOptionalUmmEntries;
                if (optionalIdentityFailed)
                {
                    result.Status = "FAIL";
                    result.Stage = "optional-identity-validation";
                }
                if (catalog != null)
                {
                    result.Assertions.Add(RuntimeTestAssertion.Pass(
                        "blueprint-library-initialized", "true", "true"));
                    result.Assertions.Add(catalog.AbilityCount > 0
                        ? RuntimeTestAssertion.Pass("catalog-nonempty", ">0", catalog.AbilityCount.ToString())
                        : RuntimeTestAssertion.Fail("catalog-nonempty", ">0", "0"));
                    int exceptions = catalog.Abilities.Count(a => a.Disposition == "scanner-exception");
                    result.Assertions.Add(exceptions == 0
                        ? RuntimeTestAssertion.Pass("scanner-exceptions", "0", "0")
                        : RuntimeTestAssertion.Fail("scanner-exceptions", "0", exceptions.ToString()));
                    if (catalog.AbilityCount == 0 || exceptions != 0)
                    {
                        result.Status = "FAIL";
                        result.Stage = "catalog-validation";
                    }
                    result.Assertions.Add(harmonyInventory != null
                        ? RuntimeTestAssertion.Pass("harmony-patch-inventory", "written", harmonyInventory.TargetCount.ToString())
                        : RuntimeTestAssertion.Fail("harmony-patch-inventory", "written", "missing"));
                    result.Assertions.Add(harmonyInventory != null && harmonyInventory.BuffPlannerOverlapTargetCount == 0
                        ? RuntimeTestAssertion.Pass("harmony-buff-planner-overlap", "0", "0")
                        : RuntimeTestAssertion.Fail("harmony-buff-planner-overlap", "0",
                            harmonyInventory == null ? "missing" : harmonyInventory.BuffPlannerOverlapTargetCount.ToString()));
                    if (harmonyInventory == null || harmonyInventory.BuffPlannerOverlapTargetCount != 0)
                    {
                        result.Status = "FAIL";
                        result.Stage = "harmony-inventory-validation";
                    }
                    if (_request.ProfileId == "call-of-the-wild")
                    {
                        AddPositiveAssertion(result, "optional-harmony-patches", harmonyInventory.PatchCount);
                        AddPositiveAssertion(result, "optional-abilities", catalog.OptionalAbilityCount);
                        AddPositiveAssertion(result, "optional-candidates", catalog.OptionalCandidateCount);
                        AddPositiveAssertion(result, "optional-included", catalog.OptionalIncludedCount);
                        result.Assertions.Add(catalog.OptionalUnsupportedCount == 0
                            ? RuntimeTestAssertion.Pass("optional-unsupported", "0", "0")
                            : RuntimeTestAssertion.Fail("optional-unsupported", "0",
                                catalog.OptionalUnsupportedCount.ToString()));
                        bool expectedMissing = false;
                        foreach (string guid in _request.ExpectedBlueprintGuids)
                        {
                            NativeCatalogEntry entry = catalog.Abilities.FirstOrDefault(a =>
                                a.AbilityGuid == guid && a.Ownership == "call-of-the-wild" &&
                                a.Disposition == "include");
                            if (entry == null) expectedMissing = true;
                            result.Assertions.Add(entry == null
                                ? RuntimeTestAssertion.Fail("optional-blueprint:" + guid,
                                    "owned-and-included", "missing-or-not-included")
                                : RuntimeTestAssertion.Pass("optional-blueprint:" + guid,
                                    "owned-and-included", entry.InternalName));
                        }
                        if (catalog.OptionalAbilityCount == 0 || catalog.OptionalCandidateCount == 0 ||
                            catalog.OptionalIncludedCount == 0 || catalog.OptionalUnsupportedCount != 0 ||
                            expectedMissing)
                        {
                            result.Status = "FAIL";
                            result.Stage = "optional-catalog-validation";
                        }
                    }
                }
                if (ui != null)
                {
                    bool liveUi = RuntimeTestProtocol.IsLiveUiScenario(_request.Scenario);
                    result.Assertions.Add(ui.RootCount == 1
                        ? RuntimeTestAssertion.Pass("ui-singleton-root", "1", "1")
                        : RuntimeTestAssertion.Fail("ui-singleton-root", "1", ui.RootCount.ToString()));
                    result.Assertions.Add(ui.RenderedOpenFrames > 0
                        ? RuntimeTestAssertion.Pass("ui-open-frame-rendered", ">0", ui.RenderedOpenFrames.ToString())
                        : RuntimeTestAssertion.Fail("ui-open-frame-rendered", ">0", "0"));
                    result.Assertions.Add(ui.OpenCloseCycles >= 21
                        ? RuntimeTestAssertion.Pass("ui-repeated-open-close", ">=21", ui.OpenCloseCycles.ToString())
                        : RuntimeTestAssertion.Fail("ui-repeated-open-close", ">=21", ui.OpenCloseCycles.ToString()));
                    result.Assertions.Add(ui.ScreenWidth > 0 && ui.ScreenHeight > 0
                        ? RuntimeTestAssertion.Pass("ui-resolution-observed", ">0x>0",
                            ui.ScreenWidth + "x" + ui.ScreenHeight)
                        : RuntimeTestAssertion.Fail("ui-resolution-observed", ">0x>0",
                            ui.ScreenWidth + "x" + ui.ScreenHeight));
                    AddUiAssertion(result, "ui-hud-buttons", ui.HudButtonCount == 4, "4", ui.HudButtonCount.ToString());
                    AddUiAssertion(result, "ui-hud-listeners", ui.HudListenerCount == 4, "4", ui.HudListenerCount.ToString());
                    AddUiAssertion(result, "ui-native-anchor", !string.IsNullOrWhiteSpace(ui.HudAnchorPath), "nonempty", ui.HudAnchorPath ?? "missing");
                    AddUiAssertion(result, "ui-hud-order", ui.HudButtonOrder == "Setup|Long|Important|Short",
                        "Setup|Long|Important|Short", ui.HudButtonOrder ?? "missing");
                    AddUiAssertion(result, "ui-hud-row-above-native", ui.HudRowAboveNativeCluster,
                        "true", ui.HudRowAboveNativeCluster.ToString());
                    AddUiAssertion(result, "ui-hud-visible-hitboxes-own-raycasts",
                        ui.HudHitboxesOwnRaycasts && !string.IsNullOrWhiteSpace(ui.HudRaycastCanvasPath),
                        "true/nonempty", ui.HudHitboxesOwnRaycasts + "/" + (ui.HudRaycastCanvasPath ?? "missing"));
                    AddUiAssertion(result, "ui-hud-native-controls-unchanged",
                        ui.HudUnderlyingNativeActivationCount == 0, "0",
                        ui.HudUnderlyingNativeActivationCount.ToString());
                    AddUiAssertion(result, "ui-full-screen-root", ui.FullScreenRootCount == 1, "1", ui.FullScreenRootCount.ToString());
                    AddUiAssertion(result, "ui-opaque", ui.FullScreenOpaque, "true", ui.FullScreenOpaque.ToString());
                    AddUiAssertion(result, "ui-blocks-raycasts", ui.FullScreenBlocksRaycasts, "true", ui.FullScreenBlocksRaycasts.ToString());
                    AddUiAssertion(result, "ui-graphic-raycaster", ui.GraphicRaycasterPresent, "true", ui.GraphicRaycasterPresent.ToString());
                    AddUiAssertion(result, "ui-presentation-visible-coverage",
                        ui.PresentationValid && ui.PresentationCoverage >= 0.98f &&
                        ui.PresentationOwnsCenterRaycast && string.IsNullOrEmpty(ui.PresentationFailure),
                        "valid/>=0.98/center-owned/no-failure", ui.PresentationValid + "/" +
                        ui.PresentationCoverage + "/" + ui.PresentationOwnsCenterRaycast + "/" +
                        (ui.PresentationFailure ?? "missing"));
                    AddUiAssertion(result, "ui-presentation-before-input-lease",
                        ui.PresentationValidatedCount > 0 && ui.PresentationValidatedOrder > 0 &&
                        ui.InputLeaseAcquiredOrder > ui.PresentationValidatedOrder,
                        "validated-order < lease-order", ui.PresentationValidatedCount + "/" +
                        ui.PresentationValidatedOrder + "/" + ui.InputLeaseAcquiredOrder);
                    AddUiAssertion(result, "ui-lifecycle-open", ui.LifecycleState == "Open",
                        "Open", ui.LifecycleState ?? "missing");
                    AddUiAssertion(result, "ui-native-full-screen-mode", ui.FullScreenModeActive, "true", ui.FullScreenModeActive.ToString());
                    AddUiAssertion(result, "ui-selection-disabled", ui.SelectionDisabled, "true", ui.SelectionDisabled.ToString());
                    AddUiAssertion(result, "ui-event-system", ui.EventSystemPresent, "true", ui.EventSystemPresent.ToString());
                    AddUiAssertion(result, "ui-input-lease-balanced", ui.InputLeaseAcquireCount > 0 && ui.InputLeaseReleaseCountAfterClose == ui.InputLeaseAcquireCount,
                        "acquire=release-after-close", ui.InputLeaseAcquireCount + "=" + ui.InputLeaseReleaseCountAfterClose);
                    AddUiAssertion(result, "ui-mode-restored", !ui.FullScreenModeActiveAfterClose, "false", ui.FullScreenModeActiveAfterClose.ToString());
                    AddUiAssertion(result, "ui-selection-restored",
                        ui.SelectionDisabledAfterClose == ui.SelectionDisabledBeforeOpen,
                        ui.SelectionDisabledBeforeOpen.ToString(), ui.SelectionDisabledAfterClose.ToString());
                    AddUiAssertion(result, "ui-pause-and-mode-restored",
                        ui.PausedAfterClose == ui.PausedBeforeOpen && ui.ModeAfterClose == ui.ModeBeforeOpen,
                        ui.PausedBeforeOpen + "/" + ui.ModeBeforeOpen,
                        ui.PausedAfterClose + "/" + ui.ModeAfterClose);
                    AddUiAssertion(result, "ui-pointer-events-consumed", ui.PointerEventCount >= 2 &&
                        ui.ScrollEventCount >= 1 && ui.DragEventCount >= 2, ">=2/1/2",
                        ui.PointerEventCount + "/" + ui.ScrollEventCount + "/" + ui.DragEventCount);
                    AddUiAssertion(result, "ui-long-flow-once", ui.LongPointerEnterCount == 1 &&
                        ui.LongPointerEventCount == 1 &&
                        ui.LongListenerCount == 1 && ui.LongGroupResolvedCount == 1 &&
                        ui.LongPlanRevalidatedCount == 1 && ui.LongExecutionInvokedCount == 0 &&
                        ui.LongRefusalCount == 1 && ui.LongResultPresentedCount == 1,
                        "1/1/1/1/1/0/1/1", ui.LongPointerEnterCount + "/" +
                        ui.LongPointerEventCount + "/" + ui.LongListenerCount + "/" +
                        ui.LongGroupResolvedCount + "/" + ui.LongPlanRevalidatedCount + "/" +
                        ui.LongExecutionInvokedCount + "/" + ui.LongRefusalCount + "/" +
                        ui.LongResultPresentedCount);
                    AddUiAssertion(result, "ui-long-empty-feedback",
                        ui.LongResultMessage == "No Long buffs are configured.",
                        "No Long buffs are configured.", ui.LongResultMessage ?? "missing");
                    AddUiAssertion(result, "ui-tooltip-identities",
                        !string.IsNullOrWhiteSpace(ui.SetupTooltip) && ui.SetupTooltip.Contains("F10") &&
                        !string.IsNullOrWhiteSpace(ui.LongTooltip) && ui.LongTooltip.Contains("Long"),
                        "setup/F10 and Long", (ui.SetupTooltip ?? "missing") + " | " + (ui.LongTooltip ?? "missing"));
                    AddUiAssertion(result, "ui-no-world-command", ui.InputPlayerCommandCount == 0 &&
                        ui.InputMovementCommandCount == 0 && ui.InputAbilityCommandCount == 0,
                        "0/0/0", ui.InputPlayerCommandCount + "/" + ui.InputMovementCommandCount + "/" +
                        ui.InputAbilityCommandCount);
                    AddUiAssertion(result, "ui-no-selection-or-ability-target", ui.InputSelectionEventCount == 0 &&
                        ui.InputAbilityTargetEventCount == 0 && ui.InputSelectionUnchanged,
                        "0/0/true", ui.InputSelectionEventCount + "/" + ui.InputAbilityTargetEventCount + "/" +
                        ui.InputSelectionUnchanged);
                    AddUiAssertion(result, "ui-camera-and-scroll-isolated", ui.InputCameraUnchanged &&
                        ui.InputScrollConsumed && ui.InputCancelConsumed,
                        "true/true/true", ui.InputCameraUnchanged + "/" + ui.InputScrollConsumed + "/" +
                        ui.InputCancelConsumed);
                    AddUiAssertion(result, "ui-group-selector", ui.GroupSelectorChanged,
                        "important-selected", ui.GroupSelectorChanged ? "important-selected" : "unchanged");
                    int expectedReconstructions = liveUi ? 0 : 1;
                    result.Assertions.Add(ui.ReconstructionCount == expectedReconstructions
                        ? RuntimeTestAssertion.Pass("ui-root-reconstruction",
                            expectedReconstructions.ToString(), ui.ReconstructionCount.ToString())
                        : RuntimeTestAssertion.Fail("ui-root-reconstruction",
                            expectedReconstructions.ToString(), ui.ReconstructionCount.ToString()));
                    if (liveUi)
                    {
                        AddUiAssertion(result, "ui-f10-armed-and-observed",
                            ui.F10Armed && ui.F10KeydownCount >= 1, "true/>=1",
                            ui.F10Armed + "/" + ui.F10KeydownCount);
                        AddUiAssertion(result, "ui-no-duplicate-full-screen-objects",
                            ui.ScreenCreateCount == ui.ScreenDestroyCount + 1 &&
                            ui.ScreenCreateCount == ui.ScreenDestroyCountAfterClose,
                            "one-open-before-close/zero-after-close", ui.ScreenCreateCount + "/" +
                            ui.ScreenDestroyCount + "/" + ui.ScreenDestroyCountAfterClose);
                        AddUiAssertion(result, "ui-hud-object-evidence",
                            !string.IsNullOrWhiteSpace(ui.HudObjectEvidence) &&
                            ui.HudObjectEvidence.Contains("corners=") &&
                            ui.HudObjectEvidence.Contains("active=True"),
                            "paths/ids/active/corners", ui.HudObjectEvidence ?? "missing");
                        AddUiAssertion(result, "exact-working-save-load",
                            _liveSaveLoader != null && _liveSaveLoader.LoadActionCount == 1 &&
                            !string.IsNullOrWhiteSpace(_liveSaveLoader.WorkingDescriptor) &&
                            !string.IsNullOrWhiteSpace(_liveSaveLoader.BaselineDescriptor),
                            "one/distinct working+baseline", _liveSaveLoader == null ? "missing" :
                            _liveSaveLoader.LoadActionCount + "/" + _liveSaveLoader.WorkingDescriptor +
                            "/" + _liveSaveLoader.BaselineDescriptor);
                    }
                    if (ui.RootCount != 1 || ui.RenderedOpenFrames == 0 || ui.OpenCloseCycles < 21 ||
                        ui.ScreenWidth <= 0 || ui.ScreenHeight <= 0 ||
                        ui.HudButtonCount != 4 || ui.HudListenerCount != 4 ||
                        string.IsNullOrWhiteSpace(ui.HudAnchorPath) ||
                        string.IsNullOrWhiteSpace(ui.HudRaycastCanvasPath) ||
                        ui.HudButtonOrder != "Setup|Long|Important|Short" ||
                        !ui.HudRowAboveNativeCluster || !ui.HudHitboxesOwnRaycasts ||
                        ui.HudUnderlyingNativeActivationCount != 0 || ui.FullScreenRootCount != 1 ||
                        !ui.FullScreenOpaque || !ui.FullScreenBlocksRaycasts ||
                        !ui.GraphicRaycasterPresent || !ui.PresentationValid ||
                        ui.PresentationCoverage < 0.98f || !ui.PresentationOwnsCenterRaycast ||
                        !string.IsNullOrEmpty(ui.PresentationFailure) ||
                        ui.PresentationValidatedCount <= 0 || ui.PresentationValidatedOrder <= 0 ||
                        ui.InputLeaseAcquiredOrder <= ui.PresentationValidatedOrder ||
                        ui.LifecycleState != "Open" || !ui.FullScreenModeActive ||
                        !ui.SelectionDisabled || !ui.EventSystemPresent ||
                        ui.InputLeaseAcquireCount <= 0 ||
                        ui.InputLeaseReleaseCountAfterClose != ui.InputLeaseAcquireCount ||
                        ui.FullScreenModeActiveAfterClose ||
                        ui.SelectionDisabledAfterClose != ui.SelectionDisabledBeforeOpen ||
                        ui.PausedAfterClose != ui.PausedBeforeOpen || ui.ModeAfterClose != ui.ModeBeforeOpen ||
                        ui.PointerEventCount < 2 || ui.ScrollEventCount < 1 || ui.DragEventCount < 2 ||
                        ui.LongPointerEnterCount != 1 || ui.LongPointerEventCount != 1 ||
                        ui.LongListenerCount != 1 ||
                        ui.LongGroupResolvedCount != 1 || ui.LongPlanRevalidatedCount != 1 ||
                        ui.LongExecutionInvokedCount != 0 || ui.LongRefusalCount != 1 ||
                        ui.LongResultPresentedCount != 1 ||
                        ui.LongResultMessage != "No Long buffs are configured." ||
                        string.IsNullOrWhiteSpace(ui.SetupTooltip) || !ui.SetupTooltip.Contains("F10") ||
                        string.IsNullOrWhiteSpace(ui.LongTooltip) || !ui.LongTooltip.Contains("Long") ||
                        ui.InputPlayerCommandCount != 0 || ui.InputMovementCommandCount != 0 ||
                        ui.InputAbilityCommandCount != 0 || ui.InputSelectionEventCount != 0 ||
                        ui.InputAbilityTargetEventCount != 0 || !ui.InputSelectionUnchanged ||
                        !ui.InputCameraUnchanged || !ui.InputScrollConsumed || !ui.InputCancelConsumed ||
                        !ui.GroupSelectorChanged ||
                        ui.ReconstructionCount != expectedReconstructions ||
                        (liveUi && (!ui.F10Armed || ui.F10KeydownCount < 1 ||
                            ui.ScreenCreateCount != ui.ScreenDestroyCount + 1 ||
                            ui.ScreenCreateCount != ui.ScreenDestroyCountAfterClose ||
                            string.IsNullOrWhiteSpace(ui.HudObjectEvidence) ||
                            !ui.HudObjectEvidence.Contains("corners=") ||
                            _liveSaveLoader == null || _liveSaveLoader.LoadActionCount != 1)))
                    {
                        result.Status = "FAIL";
                        result.Stage = "ui-validation";
                    }
                }
                string resultPath = Path.Combine(_request.EvidenceDirectory, "runtime-result.json");
                AtomicFile.WriteUtf8(
                    resultPath,
                    Serialize(result));
                _log.Info("Runtime scenario completed: " + _request.RunId + " " + result.Status + ".");
                if (_request.ExitAfterCompletion) Application.Quit();
            }
            catch (Exception exception)
            {
                _log.Error("Runtime scenario failed.", exception);
                TryWriteFailure(started, exception);
                if (_request.ExitAfterCompletion) Application.Quit();
            }

            return true;
        }

        private bool UpdateLiveUiScenario()
        {
            if (_liveSaveLoader == null)
                _liveSaveLoader = new LiveCampaignSaveLoader(_request, _log);
            if (!_liveSaveLoader.IsComplete)
            {
                _liveSaveLoader.Update();
                return false;
            }
            _uiSmokeUpdates++;
            if (_uiSmokeUpdates > 1800)
                throw new TimeoutException("Live UI scenario timed out;phase=" + _liveUiPhase +
                    ";snapshot=" + BuffPlannerUiRoot.GetSnapshot());
            if (_liveUiPhase == 0)
            {
                if (!_liveUmmDismissMarkerWritten &&
                    BuffPlannerUiRoot.HudFailure.Contains("top=UMM blocking UI/"))
                {
                    AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory,
                        "umm-overlay-ready.json"), "{\"runId\":\"" + _request.RunId +
                        "\",\"topHit\":\"UMM blocking UI\"}" + Environment.NewLine);
                    _liveUmmDismissMarkerWritten = true;
                    _log.Info("[KBP-BOOT] runtime requests physical Escape to dismiss " +
                        "ShowOnStart UMM overlay;marker=umm-overlay-ready.json.");
                }
                if (StaticCanvas.Instance == null ||
                    UnityEngine.EventSystems.EventSystem.current == null ||
                    !BuffPlannerUiRoot.IsHudInstalled) return false;
                if (!_liveF10MarkerWritten)
                {
                    AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "f10-ready.json"),
                        "{\"runId\":\"" + _request.RunId + "\",\"armed\":" +
                        (Main.F10Armed ? "true" : "false") + ",\"snapshot\":" +
                        JsonConvert.ToString(BuffPlannerUiRoot.GetSnapshot()) + "}" + Environment.NewLine);
                    _liveF10MarkerWritten = true;
                    _log.Info("[KBP-BOOT] runtime requests physical F10;marker=f10-ready.json.");
                }
                if (!Main.F10Armed || Main.F10KeydownCount < 1) return false;
                _liveUiPhase = 1;
                return false;
            }
            if (_liveUiPhase == 1)
            {
                if (!BuffPlannerUiRoot.IsScreenOpen) return false;
                BuffPlannerUiRoot.BeginRuntimeSmoke();
                BuffPlannerUiRoot.DispatchRuntimeInputSmoke();
                _liveUiPhase = 2;
                return false;
            }
            if (_liveUiPhase == 2)
            {
                BuffPlannerUiRoot.CloseRuntimeSmoke();
                BuffPlannerUiRoot.DispatchRuntimeHudLong();
                _liveUiPhase = 3;
                return false;
            }
            if (_liveUiPhase == 3)
            {
                if (!_liveCycleOpening)
                {
                    BuffPlannerUiRoot.BeginRuntimeSmoke();
                    _liveCycleOpening = true;
                    return false;
                }
                if (!BuffPlannerUiRoot.IsScreenOpen) return false;
                _liveCycleCount++;
                _liveCycleOpening = false;
                if (_liveCycleCount >= 20)
                {
                    _liveUiPhase = 4;
                    return true;
                }
                BuffPlannerUiRoot.CloseRuntimeSmoke();
                return false;
            }
            return _liveUiPhase >= 4;
        }

        private void TryWriteFailure(DateTime started, Exception exception)
        {
            try
            {
                bool campaignUiUnavailable = RuntimeTestProtocol.IsUiScenario(_request.Scenario) &&
                    exception.Message == "Campaign UI is required for the full-screen input-isolation scenario.";
                Assembly assembly = typeof(Main).Assembly;
                string gameRoot = RuntimePaths.GetGameRoot(_modEntry.Path);
                string managed = Path.Combine(gameRoot, "Kingmaker_Data", "Managed");
                string gameExecutable = Path.Combine(gameRoot, "Kingmaker.exe");
                string umm = Path.Combine(managed, "UnityModManager", "UnityModManager.dll");
                string harmony = Path.Combine(managed, "UnityModManager", "0Harmony12.dll");
                var result = new RuntimeTestResult
                {
                    SchemaVersion = 1,
                    RunId = _request.RunId,
                    Scenario = _request.Scenario,
                    ProfileId = _request.ProfileId,
                    Status = campaignUiUnavailable ? "BLOCKED" : "FAIL",
                    Stage = campaignUiUnavailable ? "campaign-ui-unavailable" : "unhandled-exception",
                    LoadedModId = _modEntry.Info.Id,
                    LoadedModVersion = _modEntry.Info.Version,
                    Commit = BuildInfo.Commit,
                    AssemblyMvid = assembly.ManifestModule.ModuleVersionId.ToString("D"),
                    AssemblySha256 = Hashing.Sha256(assembly.Location),
                    PackageSha256 = _request.ExpectedPackageSha256,
                    GameVersion = UnityModManager.gameVersion.ToString(),
                    GameExecutableSha256 = Hashing.Sha256(gameExecutable),
                    UmmVersion = UnityModManager.GetVersion().ToString(),
                    UmmSha256 = Hashing.Sha256(umm),
                    HarmonyVersion = FileVersionInfo.GetVersionInfo(harmony).FileVersion,
                    HarmonySha256 = Hashing.Sha256(harmony),
                    ProcessId = Process.GetCurrentProcess().Id,
                    StartedAtUtc = started.ToString("o"),
                    EndedAtUtc = DateTime.UtcNow.ToString("o"),
                    ExceptionSummary = exception.GetType().FullName + ": " + exception.Message,
                    Assertions = new List<RuntimeTestAssertion>
                    {
                        RuntimeTestAssertion.Pass("entry-point-loaded", "true", "true"),
                        RuntimeTestAssertion.Pass("standalone-id", "KingmakerBuffPlanner", _modEntry.Info.Id),
                        RuntimeTestAssertion.Pass("version", BuildInfo.Version, _modEntry.Info.Version),
                        RuntimeTestAssertion.Pass("commit", _request.ExpectedCommit, BuildInfo.Commit),
                        RuntimeTestAssertion.Fail("scenario-precondition", "available", exception.Message)
                    }
                };
                AtomicFile.WriteUtf8(
                    Path.Combine(_request.EvidenceDirectory, "runtime-result.json"),
                    Serialize(result));
            }
            catch (Exception writeException)
            {
                _log.Error("Runtime failure result could not be written.", writeException);
            }
        }

        private static string Serialize(RuntimeTestResult result)
        {
            return Serialize((object)result);
        }

        private static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(
                value,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.None,
                    ReferenceLoopHandling = ReferenceLoopHandling.Error,
                    TypeNameHandling = TypeNameHandling.None,
                    Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
                }) + Environment.NewLine;
        }

        private static void AddPositiveAssertion(
            RuntimeTestResult result, string id, int value)
        {
            result.Assertions.Add(value > 0
                ? RuntimeTestAssertion.Pass(id, ">0", value.ToString())
                : RuntimeTestAssertion.Fail(id, ">0", "0"));
        }

        private static void AddUiAssertion(
            RuntimeTestResult result, string id, bool passed, string expected, string observed)
        {
            result.Assertions.Add(passed
                ? RuntimeTestAssertion.Pass(id, expected, observed)
                : RuntimeTestAssertion.Fail(id, expected, observed));
        }
    }

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
        [JsonProperty("uiF10Armed", Order = 112)] public bool UiF10Armed { get; set; }
        [JsonProperty("uiF10KeydownCount", Order = 113)] public int UiF10KeydownCount { get; set; }
        [JsonProperty("uiHudObjectEvidence", Order = 114)] public string UiHudObjectEvidence { get; set; }
        [JsonProperty("uiScreenDestroyCountAfterClose", Order = 115)] public int UiScreenDestroyCountAfterClose { get; set; }
        [JsonProperty("workingSaveDescriptor", Order = 116)] public string WorkingSaveDescriptor { get; set; }
        [JsonProperty("baselineSaveDescriptor", Order = 117)] public string BaselineSaveDescriptor { get; set; }
        [JsonProperty("workingSaveLoadActionCount", Order = 118)] public int WorkingSaveLoadActionCount { get; set; }
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
