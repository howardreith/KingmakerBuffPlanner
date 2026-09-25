using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.UI;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal sealed class RuntimeTestHost
    {
        // Wall-clock budgets for the workspace frame capture, matching the
        // menu diagnostic: unfocused players spin far above 60 fps, and the
        // engine screenshot is flushed asynchronously.
        private const int WorkspaceBlackFrameRecaptureMilliseconds = 1000;
        private const int WorkspaceBlackFrameRecaptureMaxAttempts = 30;
        private const int WorkspaceEngineCaptureWaitMilliseconds = 10000;
        private const int WorkspaceOpenSettleMilliseconds = 750;

        private readonly RuntimeTestRequest _request;
        private readonly UnityModManager.ModEntry _modEntry;
        private readonly ModLog _log;
        private readonly DateTime _startedAtUtc;
        private bool _completed;
        private int _uiSmokeUpdates;
        private System.Diagnostics.Stopwatch _livePhaseElapsed;
        private System.Diagnostics.Stopwatch _liveCameraSettleElapsed;
        private bool _uiReconstructionRequested;
        private int _uiPostReconstructionUpdates;
        private LiveCampaignSaveLoader _liveSaveLoader;
        private bool _workspaceSelectionApplied;
        private bool _workspaceProgrammaticOpen;
        private MenuRenderDiagnostic _menuDiagnostic;
        private int _liveUiPhase;
        private int _liveCycleCount;
        private bool _liveCycleOpening;
        private bool _liveHotkeyMarkerWritten;
        private bool _liveUmmDismissMarkerWritten;
        private int _liveTooltipStableFrames;
        private DateTime _liveTooltipStableStartedAtUtc;
        private bool _liveTooltipStable;
        private string _liveTooltipEvidence = string.Empty;
        private string _liveInitialCatalogEvidence = string.Empty;
        private bool _liveBlessSelectedAndConfigured;
        private int _liveCameraSettleFrames;
        private int _liveHoverEnterBaseline;
        private int _liveRenderWaitFrames;
        private string _liveRenderScreenshotPath;
        private string _liveRenderScreenshotSha256 = string.Empty;
        private string _liveSelectedDetailsScreenshotSha256 = string.Empty;
        private string _liveGridOverviewScreenshotSha256 = string.Empty;
        private string _liveTargetColorsScreenshotSha256 = string.Empty;
        private string _liveSettingsScreenshotSha256 = string.Empty;
        private string _liveHudScreenshotSha256 = string.Empty;
        private string _liveCatalogControlEvidence = string.Empty;
        private int _liveDirectSelectedTargetCount;
        private int _liveIndirectCoveredTargetCount;
        private int _liveHudScreenshotWaitFrames;
        private LiveRowRenderDiagnostics _liveRenderDiagnostics;
        private NativeUiContract _nativeUiContract;
        private MenuFrameCapture _workspaceLastCapture;
        private MenuFrameCapture _workspaceFrameCapture;
        private int _workspaceBlackAttempts;
        private long _workspaceEngineWaitStartedMillis = -1;
        private string _workspacePresentationEvidence;
        private string _workspaceLumaEvidence;
        private string _workspaceClosedLuma;
        private string _workspaceClosedScreenshotSha256;
        private int _workspaceClosedWaitUpdates;
        private bool _workspaceWasOpenAtCapture;
        private long _workspaceOpenSeenMillis = -1;
        private bool _workspaceControlRequested;
        private string _workspaceControlLuma;
        private string _workspaceControlSha256;
        private float[] _workspaceControlSamples;
        private float[] _workspaceOpenSamples;
        private float _workspaceChangedFraction = -1f;
        private string _workspaceOpenLumaSummary = "missing";
        private bool _workspaceOpenNonBlack;
        private int _workspaceInteractionStep;
        private string _workspaceInteractionEvidence = "not-run";
        private string _workspaceReopenEvidence = "not-run";
        private string _workspaceReloadEvidence = "not-run";
        private string _reloadLoadedEvidence;
        private long _reloadStartedMillis;
        private int _reloadSettleFrame = -1;
        private int _reloadSubscriptionsBefore;
        private int _reloadHudRootsBefore;
        private int _reloadRunsBefore;
        private int _reloadUnloadsBefore;
        private int _reloadLoadsBefore;
        private int _reloadActivatedBefore;
        private int _reloadCompleteBefore;
        private UI.CastingWorkspaceSession _reloadSessionBefore;
        private string _workspaceSavedIntentIds;
        private string _workspaceIntentBeforeEdit;
        private long _manualHoldStartedMillis = -1;
        private long _manualHoldDeadlineMillis;
        private string _manualOutcome;
        private string _manualReadyEvidence;
        // Structured automatic interaction evidence (review J1) and the
        // manual terminal coordinator (review J2): both carry their own
        // acceptance predicates in WorkspaceScenarioContracts.cs.
        private readonly WorkspaceInteractionRecord _workspaceInteraction =
            new WorkspaceInteractionRecord();
        private ManualTerminalCoordinator _manualTerminal;
        // Single-cast probe (dormant unless the owner's allowance is present).
        private readonly SingleCastProbeRunRecord _probeRecord = new SingleCastProbeRunRecord();
        private SingleCastProbeSelection _probeSelection;
        private SingleCastProbeRunOwner _probeOwner;
        private readonly ProbeSequenceClock _probeClock = new ProbeSequenceClock();
        private readonly List<string> _interactionCasters = new List<string>();
        private readonly List<string> _interactionTargets = new List<string>();
        private readonly List<string> _interactionCastIds = new List<string>();
        private readonly List<string> _interactionCastTargets = new List<string>();
        private string _interactionRetarget;
        // Pointer-highlight diagnostic (phases 140 and 145).
        private readonly HoverOwnershipRecord _hover = new HoverOwnershipRecord();
        private bool _hoverStarted;
        private bool _hoverReopenChecked;
        private int _hoverStep;
        private int _hoverAfter;
        private int _hoverFrames;
        private string _hoverPending;
        private Vector2 _hoverPoint;
        private string _hoverAimed;
        private string _hoverPendingSurface;
        private string _hoverPendingBehaviour;
        private bool _hoverPendingSample;
        private System.Diagnostics.Stopwatch _hoverClock = new System.Diagnostics.Stopwatch();
        private MenuFrameCapture _hoverCapture;
        private string _hoverCaptureName;
        private Vector2 _hoverGraphNeutral;
        private string _hoverHeld;
        private string[] _hoverButtons = new string[0];
        private readonly List<string> _hoverSweep = new List<string>();
        private int _hoverSweepIndex;
        private string _hoverClassicRoutine;
        private string _hoverClassicTarget;
        private bool _hoverClassicRowSelected;

        // Casters of the shown buff with a source the graph can pin.
        private static List<string> GraphCapableCasters(UI.CastingGraphView graph)
        {
            return graph.Casters.Where(caster => !caster.IsUnresolved &&
                    caster.Sources.Any(row => row.Pinnable))
                .Select(caster => caster.UnitId).ToList();
        }

        // Legal recipients of the shown buff for the chosen caster, never the
        // caster itself (Aid Another cannot target its caster).
        private static List<string> GraphLegalOthers(UI.CastingGraphView graph, string caster)
        {
            return graph.Targets.Where(target =>
                    target.Legality == UI.CastingGraphTargetLegality.Legal &&
                    !string.Equals(target.UnitId, caster, StringComparison.Ordinal))
                .Select(target => target.UnitId).ToList();
        }

        // The scripted sequence adds three single-target castings from two
        // casters to three different recipients, so it needs a single-target
        // buff two casters can give to at least two others each. Choosing a
        // buff or caster is focus only (never a document mutation); the
        // caller restores the shown buff and selects the chosen one through
        // its real catalogue control.
        private static string ChooseInteractionSource(UI.CastingWorkspaceSession session,
            CastingWorkspaceInputs inputs, out int probed)
        {
            probed = 0;
            List<string> order = session.BuildGraph(inputs).Catalogue
                .OrderByDescending(entry => entry.Selected).Select(entry => entry.SourceId).ToList();
            foreach (string sourceId in order.Take(80))
            {
                probed++;
                session.SelectGraphBuff(sourceId, inputs);
                UI.CastingGraphView graph = session.BuildGraph(inputs);
                if (graph.SelectedSourceIsGroup != false || graph.Targets.Count < 3) continue;
                List<string> casters = GraphCapableCasters(graph);
                if (casters.Count < 2) continue;
                bool enough = true;
                foreach (string caster in casters.Take(2))
                {
                    session.SelectGraphCaster(caster, inputs);
                    if (GraphLegalOthers(session.BuildGraph(inputs), caster).Count < 2) enough = false;
                }
                if (enough) return sourceId;
            }
            return null;
        }

        // The index of the caster's source row to click: usable and pinnable
        // first, then pinnable (a row short of casts still adds a casting,
        // which the plan then reports short).
        private static int UsableSourceRow(UI.CastingWorkspaceSession session, CastingWorkspaceInputs inputs,
            string caster)
        {
            UI.CastingGraphCasterNode node = session.BuildGraph(inputs).CasterById(caster);
            if (node == null) return -1;
            for (int index = 0; index < node.Sources.Count; index++)
                if (node.Sources[index].Usable && node.Sources[index].Pinnable) return index;
            for (int index = 0; index < node.Sources.Count; index++)
                if (node.Sources[index].Pinnable) return index;
            return -1;
        }

        // A legal recipient for the chosen caster and source that no casting
        // of the buff in this routine reaches yet (the graph shows, never
        // steals, an assigned target) and that is not the caster.
        private string GraphInteractionTarget(UI.CastingWorkspaceSession session, CastingWorkspaceInputs inputs,
            string caster)
        {
            UI.CastingGraphView graph = session.BuildGraph(inputs);
            List<string> legal = GraphLegalOthers(graph, caster);
            string free = legal.FirstOrDefault(unit =>
            {
                UI.CastingGraphTargetNode node = graph.TargetById(unit);
                return node != null && node.CastingIds.Count == 0 && !_interactionCastTargets.Contains(unit);
            });
            return free ?? legal.FirstOrDefault() ?? _interactionTargets.FirstOrDefault(unit =>
                !string.Equals(unit, caster, StringComparison.Ordinal)) ?? _interactionTargets[0];
        }

        // Another legal recipient for the focused casting: neither its caster
        // nor its current recipient, preferably one no other casting reaches.
        private string GraphRetarget(UI.CastingWorkspaceSession session, CastingWorkspaceInputs inputs,
            string castingId)
        {
            UI.CastingGraphView graph = session.BuildGraph(inputs);
            Domain.Authoring.PlannedCasting casting = session.Document.Castings.FirstOrDefault(value =>
                value != null && string.Equals(value.CastingId, castingId, StringComparison.Ordinal));
            string caster = casting == null ? null : casting.CasterUnitId;
            string current = casting == null ? null : casting.DirectTargetUnitId;
            List<string> candidates = (graph.Inspector != null &&
                    string.Equals(graph.Inspector.CastingId, castingId, StringComparison.Ordinal)
                    ? graph.Inspector.Retargets.Select(target => target.UnitId)
                    : _interactionTargets)
                .Where(unit => !string.Equals(unit, caster, StringComparison.Ordinal) &&
                    !string.Equals(unit, current, StringComparison.Ordinal)).ToList();
            return candidates.FirstOrDefault(unit => !_interactionCastTargets.Contains(unit)) ??
                candidates.FirstOrDefault() ?? current ?? _interactionTargets[0];
        }

        // The focused inspector's retarget control for a unit. Its list opens
        // through its own toggle only when closed (the toggle would close an
        // open list).
        private static string InvokeRetarget(string unitId)
        {
            string outcome = Invoke("Retarget." + unitId);
            // Missing, or only a same-frame destroyed-pending copy: closed.
            if (!outcome.StartsWith("control-missing", StringComparison.Ordinal) &&
                !outcome.StartsWith("control-inactive", StringComparison.Ordinal)) return outcome;
            string toggle = Invoke("ToggleRetargets");
            if (toggle != WorkspaceControlOutcome.Invoked) return "toggle-" + toggle;
            return Invoke("Retarget." + unitId);
        }

        // A focus change made directly on the session (not through a
        // control) is rendered before the next control is looked up.
        private static void RefreshWorkspaceForRuntime()
        {
            UI.CastingWorkspaceScreenView view = BuffPlannerUiRoot.CastingWorkspaceViewForRuntime;
            if (view != null) view.RefreshView();
        }

        // The standard workspace scenario continues into the pointer-highlight
        // diagnostic before its close/reopen; the reload scenario goes
        // straight to the close.
        private void FinishWorkspaceInteraction()
        {
            if (string.Equals(_request.Scenario, "live-workspace-qual", StringComparison.Ordinal))
            {
                _hoverStep = 0;
                _liveUiPhase = 140;
                return;
            }
            TransitionToBisectionClose();
        }
        private string _interactionSourceId;
        private MenuFrameCapture _workspaceCameraOpenCapture;
        private MenuFrameCapture _workspaceCameraControlCapture;
        private string _workspaceCameraOpenLuma = "missing";
        private string _workspaceCameraControlLuma = "missing";
        private readonly System.Diagnostics.Stopwatch _workspaceCaptureElapsed =
            new System.Diagnostics.Stopwatch();
        private string _workspaceEngineScreenshotSha256;

        private RuntimeTestHost(
            RuntimeTestRequest request,
            UnityModManager.ModEntry modEntry,
            ModLog log)
        {
            _request = request;
            _modEntry = modEntry;
            _log = log;
            _startedAtUtc = DateTime.UtcNow;
            // Review N2: a probe request's owner exists from host creation, so
            // a disable/unload/failure at ANY point is terminal for it.
            if (RuntimeTestProtocol.IsProbeScenario(request.Scenario))
                _probeOwner = new SingleCastProbeRunOwner(_probeRecord, CloseProbeWorkspace,
                    PublishProbeRecord);
        }

        internal static RuntimeTestHost TryCreate(
            string[] arguments,
            UnityModManager.ModEntry modEntry,
            ModLog log)
        {
            string rejection;
            RuntimeTestRequest request = RuntimeTestProtocol.TryRead(arguments, out rejection);
            if (request != null)
            {
                // Taken before any planner session exists: neither player
                // route (casting-first boundary, classic routine execution)
                // of an automation session submits native casts. The probe
                // and the qualification keep their own allowance-bound
                // boundaries.
                UI.NativeCastingSessionPolicy.LockForRuntimeTest(request.Scenario);
                RuntimePerformanceDiagnostics.Configure(request, log);
                return new RuntimeTestHost(request, modEntry, log);
            }
            if (!string.IsNullOrEmpty(rejection)) log.Info("Runtime request rejected: " + rejection);
            return null;
        }

        internal bool Update()
        {
            if (_completed) return true;
            // Review N2: after Shutdown (disable/unload/host failure) no later
            // update or re-enable can resume this request: it completes once,
            // as a failure, without selecting, arming or submitting.
            if (_shutdownReason != null)
            {
                _completed = true;
                try
                {
                    TryWriteFailure(_startedAtUtc,
                        new InvalidOperationException("runtime-request-shut-down:" + _shutdownReason));
                }
                catch (Exception exception)
                {
                    _log.Error("[KBP-PROBE] shutdown result could not be written.", exception);
                }
                return true;
            }
            // Workspace scenario: route the planner to the casting-first
            // workspace instead of the legacy screen. This must precede the
            // live-UI dispatch below (which returns until the phase machine
            // completes) and must not sit inside the non-live UI-smoke gate
            // — both mistakes left the selection unapplied and the legacy
            // screen opened instead (runs casting-ws-visual-183000 and
            // casting-ws-root-190500).
            if (RuntimeTestProtocol.IsWorkspaceScenario(_request.Scenario) &&
                !_workspaceSelectionApplied)
            {
                UI.CastingWorkspaceDevSelection.Enabled = true;
                _workspaceSelectionApplied = true;
                _log.Info("[KBP-WORKSPACE] dev selection enabled for workspace scenario.");
            }
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
                    // Review M3: the probe owner's terminal cleanup runs BEFORE
                    // the failure result is published; the primary failure is
                    // kept and cleanup failures are appended by the owner.
                    try
                    {
                        Shutdown("host-exception:" + exception.GetType().Name + ":" + exception.Message);
                    }
                    catch (Exception cleanup)
                    {
                        _log.Error("[KBP-PROBE] terminal cleanup after host failure failed.", cleanup);
                    }
                    TryWriteFailure(_startedAtUtc, exception);
                    if (_request.ExitAfterCompletion) Application.Quit();
                    return true;
                }
            }
            if (RuntimeTestProtocol.IsMenuDiagnosticScenario(_request.Scenario))
            {
                try
                {
                    if (!UpdateMenuDiagnosticScenario()) return false;
                }
                catch (Exception exception)
                {
                    _completed = true;
                    _log.Error("Menu diagnostic runtime scenario failed.", exception);
                    TryWriteFailure(_startedAtUtc, exception);
                    if (_request.ExitAfterCompletion) Application.Quit();
                    return true;
                }
            }
            // Focused re-review: every other scenario obeys the same stop rule
            // while it waits (the catalog's resource library, the performance
            // window, the UI smoke, the native probe).
            string waitingStop = LiveRunStopReason();
            if (waitingStop != null)
            {
                _completed = true;
                var stopped = new TimeoutException("Runtime scenario stopped;" + waitingStop + ";scenario=" +
                    _request.Scenario);
                _log.Error("Runtime scenario stopped by its overall deadline or the launcher's abort.", stopped);
                TryWriteFailure(_startedAtUtc, stopped);
                if (_request.ExitAfterCompletion) Application.Quit();
                return true;
            }
            if (RuntimeTestProtocol.IsCatalogScenario(_request.Scenario) &&
                ResourcesLibrary.LibraryObject == null)
                return false;
            if (RuntimeTestProtocol.IsPerformanceScenario(_request.Scenario) &&
                !RuntimePerformanceDiagnostics.IsDurationComplete)
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
                else if (!_uiReconstructionRequested)
                {
                    _uiReconstructionRequested = true;
                    BuffPlannerUiRoot.ReconstructRuntimeSmoke();
                    return false;
                }
                else if (BuffPlannerUiRoot.IsRuntimeReconstructionPending)
                {
                    return false;
                }
                else
                {
                    _uiPostReconstructionUpdates++;
                    if (_uiPostReconstructionUpdates == 1)
                    {
                        BuffPlannerUiRoot.DispatchRuntimeInputSmoke();
                        return false;
                    }
                    if (_uiPostReconstructionUpdates < 4) return false;
                }
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
                RuntimePerformanceProfile performanceProfile = null;
                string performanceProfileHash = null;
                UiRootDiagnostics ui = null;
                NativeUiContract nativeUiContract = _nativeUiContract;
                string nativeUiContractPath = Path.Combine(
                    _request.EvidenceDirectory, "native-ui-contract.json");
                string nativeUiContractHash = nativeUiContract != null && File.Exists(nativeUiContractPath)
                    ? Hashing.Sha256(nativeUiContractPath) : null;
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
                    AtomicFile.WriteUtf8(nativeUiContractPath, Serialize(nativeUiContract));
                    nativeUiContractHash = Hashing.Sha256(nativeUiContractPath);
                }
                if (RuntimeTestProtocol.IsPerformanceScenario(_request.Scenario))
                {
                    performanceProfile = RuntimePerformanceDiagnostics.CompleteProfile();
                    string performancePath = Path.Combine(
                        _request.EvidenceDirectory, "performance-profile.json");
                    AtomicFile.WriteUtf8(performancePath, Serialize(performanceProfile));
                    performanceProfileHash = Hashing.Sha256(performancePath);
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
                    UiHudRowLeftAlignedWithNativeCluster = ui != null &&
                        ui.HudRowLeftAlignedWithNativeCluster,
                    UiHudGlyphsCentered = ui != null && ui.HudGlyphsCentered,
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
                    UiHotkeyArmed = ui != null && ui.HotkeyArmed,
                    UiHotkeyKeydownCount = ui == null ? 0 : ui.HotkeyKeydownCount,
                    UiHudObjectEvidence = ui == null ? null : ui.HudObjectEvidence,
                    UiScreenDestroyCountAfterClose = ui == null ? 0 : ui.ScreenDestroyCountAfterClose,
                    WorkingSaveDescriptor = _liveSaveLoader == null ? null : _liveSaveLoader.WorkingDescriptor,
                    BaselineSaveDescriptor = _liveSaveLoader == null ? null : _liveSaveLoader.BaselineDescriptor,
                    WorkingSaveLoadActionCount = _liveSaveLoader == null ? 0 : _liveSaveLoader.LoadActionCount,
                    SaveChainHandlerInvocations = _liveSaveLoader == null ? 0 : _liveSaveLoader.HandlerInvocationCount,
                    SaveChainCatalogInvocations = _liveSaveLoader == null ? 0 : _liveSaveLoader.CatalogInvocationCount,
                    SaveChainCatalogDescriptorCount = _liveSaveLoader == null ? 0 : _liveSaveLoader.CatalogDescriptorCount,
                    SaveChainWorkingMatchCount = _liveSaveLoader == null ? 0 : _liveSaveLoader.WorkingMatchCount,
                    SaveChainBaselineMatchCount = _liveSaveLoader == null ? 0 : _liveSaveLoader.BaselineMatchCount,
                    SaveChainSlotReceiverCorrelated = _liveSaveLoader != null && _liveSaveLoader.SlotReceiverCorrelated,
                    SaveChainWindowReceiverCorrelated = _liveSaveLoader != null && _liveSaveLoader.WindowReceiverCorrelated,
                    SaveChainWindowArgumentCorrelated = _liveSaveLoader != null && _liveSaveLoader.WindowArgumentCorrelated,
                    SaveChainLoadEntryCorrelated = _liveSaveLoader != null && _liveSaveLoader.LoadEntryCorrelated,
                    SaveChainCompletionCallback = _liveSaveLoader != null && _liveSaveLoader.CompletionCallbackObserved,
                    SaveChainNoUnexpectedWrite = _liveSaveLoader == null ||
                        !_liveSaveLoader.UnexpectedSaveWriteObserved,
                    SaveChainSequences = _liveSaveLoader == null ? null : _liveSaveLoader.ChainSequences,
                    SaveChainFingerprint = _liveSaveLoader == null ? null : _liveSaveLoader.FingerprintEvidence,
                    UiInitialCatalogEvidence = _liveInitialCatalogEvidence,
                    UiCatalogEvidence = ui == null ? null : ui.CatalogEvidence,
                    UiCatalogVisibleViewModels = ui == null ? 0 : ui.CatalogVisibleViewModels,
                    UiCatalogInstantiatedRows = ui == null ? 0 : ui.CatalogInstantiatedRows,
                    UiCatalogActiveRows = ui == null ? 0 : ui.CatalogActiveRows,
                    UiCatalogVisibleRows = ui == null ? 0 : ui.CatalogVisibleRows,
                    UiCatalogSelectedDetailsBound = ui != null && ui.CatalogSelectedDetailsBound,
                    UiCatalogBlessEvidence = ui == null ? null : ui.CatalogBlessEvidence,
                    UiCatalogProviderCount = ui == null ? 0 : ui.CatalogProviderCount,
                    UiCatalogAggregateAbilityCount = ui == null ? 0 : ui.CatalogAggregateAbilityCount,
                    UiCatalogConsolidatedCardCount = ui == null ? 0 : ui.CatalogConsolidatedCardCount,
                    UiDirectSelectedTargetCount = _liveDirectSelectedTargetCount,
                    UiIndirectCoveredTargetCount = _liveIndirectCoveredTargetCount,
                    UiCatalogControlEvidence = _liveCatalogControlEvidence,
                    UiBlessSelectedAndConfigured = _liveBlessSelectedAndConfigured,
                    UiTooltipStable = _liveTooltipStable,
                    UiTooltipEvidence = _liveTooltipEvidence,
                    UiTooltipListenerCount = ui == null ? 0 : ui.TooltipListenerCount,
                    UiTooltipRaycastGraphicCount = ui == null ? -1 : ui.TooltipRaycastGraphicCount,
                    UiTooltipBlocksRaycasts = ui != null && ui.TooltipBlocksRaycasts,
                    UiTooltipNativeTriggerCount = ui == null ? 0 : ui.TooltipNativeTriggerCount,
                    UiTooltipUsesNativeParchmentPresentation = ui != null &&
                        ui.TooltipUsesNativeParchmentPresentation,
                    UiSetupOpenSoundCount = ui == null ? 0 : ui.SetupOpenSoundCount,
                    UiPhysicalInputPlayerCommandCount = ui == null ? -1 : ui.PhysicalInputPlayerCommandCount,
                    UiPhysicalInputMovementCommandCount = ui == null ? -1 : ui.PhysicalInputMovementCommandCount,
                    UiPhysicalInputAbilityCommandCount = ui == null ? -1 : ui.PhysicalInputAbilityCommandCount,
                    UiPhysicalInputSelectionEventCount = ui == null ? -1 : ui.PhysicalInputSelectionEventCount,
                    UiPhysicalInputAbilityTargetEventCount = ui == null ? -1 : ui.PhysicalInputAbilityTargetEventCount,
                    UiPhysicalInputSelectionUnchanged = ui != null && ui.PhysicalInputSelectionUnchanged,
                    UiPhysicalInputCameraUnchanged = ui != null && ui.PhysicalInputCameraUnchanged,
                    UiImportantResultMessage = ui == null ? null : ui.ImportantResultMessage,
                    UiShortResultMessage = ui == null ? null : ui.ShortResultMessage,
                    UiConfiguredLongResultMessage = ui == null ? null : ui.ConfiguredLongResultMessage,
                    UiConfiguredLongDisposition = ui == null ? null : ui.ConfiguredLongDisposition,
                    UiConfiguredLongPlanned = ui == null ? 0 : ui.ConfiguredLongPlanned,
                    UiConfiguredLongSubmitted = ui == null ? 0 : ui.ConfiguredLongSubmitted,
                    UiConfiguredLongConfirmed = ui == null ? 0 : ui.ConfiguredLongConfirmed,
                    UiRenderExpectedNames = _liveRenderDiagnostics == null ? null :
                        _liveRenderDiagnostics.ExpectedNames,
                    UiRenderRowScreenRectangles = _liveRenderDiagnostics == null ? null :
                        _liveRenderDiagnostics.RowScreenRectangles,
                    UiRenderSelectedRowName = _liveRenderDiagnostics == null ? null :
                        _liveRenderDiagnostics.SelectedRowName,
                    UiRenderDetailsTitleText = _liveRenderDiagnostics == null ? null :
                        _liveRenderDiagnostics.DetailsTitleText,
                    UiRenderBoundRowCount = _liveRenderDiagnostics == null ? 0 :
                        _liveRenderDiagnostics.BoundRowCount,
                    UiRenderMaskEvidence = _liveRenderDiagnostics == null ? null :
                        _liveRenderDiagnostics.MaskEvidence,
                    UiRenderCanaryEvidence = _liveRenderDiagnostics == null ? null :
                        _liveRenderDiagnostics.CanaryEvidence,
                    UiRenderRowEvidence = _liveRenderDiagnostics == null ? null :
                        _liveRenderDiagnostics.RowEvidence,
                    UiRenderDetailsEvidence = _liveRenderDiagnostics == null ? null :
                        _liveRenderDiagnostics.DetailsEvidence,
                    UiRenderScreenshotSha256 = _liveRenderScreenshotSha256,
                    UiSelectedDetailsScreenshotSha256 = _liveSelectedDetailsScreenshotSha256,
                    UiGridOverviewScreenshotSha256 = _liveGridOverviewScreenshotSha256,
                    UiTargetColorsScreenshotSha256 = _liveTargetColorsScreenshotSha256,
                    UiSettingsScreenshotSha256 = _liveSettingsScreenshotSha256,
                    UiHudScreenshotSha256 = _liveHudScreenshotSha256,
                    UiRenderAbilityIconCount = _liveRenderDiagnostics == null ? 0 :
                        _liveRenderDiagnostics.AbilityIconCount,
                    UiRenderMissingIconCount = _liveRenderDiagnostics == null ? 0 :
                        _liveRenderDiagnostics.MissingIconCount,
                    UiCastingModeControlCount = _liveRenderDiagnostics == null ? 0 :
                        _liveRenderDiagnostics.CastingModeControlCount,
                    UiRetiredPrimaryLabelCount = _liveRenderDiagnostics == null ? 0 :
                        _liveRenderDiagnostics.RetiredPrimaryLabelCount,
                    UiThemeResolution = _liveRenderDiagnostics == null ? null :
                        _liveRenderDiagnostics.ThemeResolution,
                    UiTextRenderingEvidence = _liveRenderDiagnostics == null ? null :
                        _liveRenderDiagnostics.TextRenderingEvidence,
                    UiNestedPlannerCanvasScalerCount = _liveRenderDiagnostics == null ? -1 :
                        _liveRenderDiagnostics.NestedCanvasScalerCount,
                    UiFractionalRectCount = _liveRenderDiagnostics == null ? -1 :
                        _liveRenderDiagnostics.FractionalRectCount,
                    MenuFrameScreenshotSha256 = _menuDiagnostic == null
                        ? null : _menuDiagnostic.MenuFrameScreenshotSha256,
                    MenuFrameEngineScreenshotSha256 = _menuDiagnostic == null
                        ? null : _menuDiagnostic.MenuFrameEngineScreenshotSha256,
                    MenuFrameWidth = _menuDiagnostic == null ? 0 : _menuDiagnostic.MenuFrameWidth,
                    MenuFrameHeight = _menuDiagnostic == null ? 0 : _menuDiagnostic.MenuFrameHeight,
                    MenuFrameLumaSummary = _menuDiagnostic == null || _menuDiagnostic.MenuFrameLuma == null
                        ? null : _menuDiagnostic.MenuFrameLuma.Describe(),
                    MenuFrameNonBlack = _menuDiagnostic != null && _menuDiagnostic.MenuFrameLuma != null &&
                        _menuDiagnostic.MenuFrameLuma.IsNonBlack,
                    MenuFrameProgressSummary = _menuDiagnostic == null
                        ? null : _menuDiagnostic.FrameProgressSummary,
                    MenuButtonInventory = _menuDiagnostic == null ? null : _menuDiagnostic.MenuButtonInventory,
                    MenuClickTarget = _menuDiagnostic == null ? null : _menuDiagnostic.MenuClickTarget,
                    MenuClickAcknowledged = _menuDiagnostic != null && _menuDiagnostic.MenuClickAcknowledged,
                    MenuWindowOpened = _menuDiagnostic != null && _menuDiagnostic.MenuWindowOpened,
                    MenuWindowDescriptor = _menuDiagnostic == null ? null : _menuDiagnostic.MenuWindowDescriptor,
                    MenuWindowScreenshotSha256 = _menuDiagnostic == null
                        ? null : _menuDiagnostic.MenuWindowScreenshotSha256,
                    MenuEscape1ScreenshotSha256 = _menuDiagnostic == null
                        ? null : _menuDiagnostic.MenuEscape1ScreenshotSha256,
                    MenuEscape1ChangedFraction = _menuDiagnostic == null ? 0 : _menuDiagnostic.MenuEscape1ChangedFraction,
                    MenuEscape1Acknowledged = _menuDiagnostic != null && _menuDiagnostic.MenuEscape1Acknowledged,
                    MenuEscape2ScreenshotSha256 = _menuDiagnostic == null
                        ? null : _menuDiagnostic.MenuEscape2ScreenshotSha256,
                    MenuEscape2ChangedFraction = _menuDiagnostic == null ? 0 : _menuDiagnostic.MenuEscape2ChangedFraction,
                    MenuEscape2Acknowledged = _menuDiagnostic != null && _menuDiagnostic.MenuEscape2Acknowledged,
                    NativeUiContractSha256 = nativeUiContractHash,
                    NativeUiButtonCount = nativeUiContract == null ? 0 : nativeUiContract.Buttons.Count,
                    NativeUiCandidateAnchorCount = nativeUiContract == null
                        ? 0 : nativeUiContract.CandidateAnchors.Count,
                    PerformanceProfileSha256 = performanceProfileHash,
                    PerformanceMinimumFramesPerSecond = performanceProfile == null ? 0 :
                        performanceProfile.MinimumFramesPerSecond,
                    PerformanceAverageFramesPerSecond = performanceProfile == null ? 0 :
                        performanceProfile.AverageFramesPerSecond,
                    PerformanceHudObjectFindInvocationCount = performanceProfile == null ? 0 :
                        performanceProfile.HudObjectFindInvocationCount,
                    PerformanceHudObjectFindTotalMilliseconds = performanceProfile == null ? 0 :
                        performanceProfile.HudObjectFindTotalMilliseconds,
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
                if (RuntimeTestProtocol.IsPerformanceScenario(_request.Scenario))
                {
                    bool profileValid = performanceProfile != null &&
                        performanceProfile.QualifiedSampleCount > 0 &&
                        performanceProfile.TotalFrameCount > 0 &&
                        !string.IsNullOrWhiteSpace(performanceProfileHash);
                    result.Assertions.Add(profileValid
                        ? RuntimeTestAssertion.Pass("performance-profile", "complete", performanceProfileHash)
                        : RuntimeTestAssertion.Fail("performance-profile", "complete", "missing-or-empty"));
                    bool minimumMet = profileValid && performanceProfile.MeetsRequestedMinimum;
                    result.Assertions.Add(minimumMet
                        ? RuntimeTestAssertion.Pass("performance-minimum-fps",
                            performanceProfile.RequestedMinimumFramesPerSecond.ToString("F2"),
                            performanceProfile.MinimumFramesPerSecond.ToString("F2"))
                        : RuntimeTestAssertion.Fail("performance-minimum-fps",
                            performanceProfile == null ? "missing" :
                                performanceProfile.RequestedMinimumFramesPerSecond.ToString("F2"),
                            performanceProfile == null ? "missing" :
                                performanceProfile.MinimumFramesPerSecond.ToString("F2")));
                    if (!profileValid || !minimumMet)
                    {
                        result.Status = "FAIL";
                        result.Stage = "performance-validation";
                    }
                }
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
                if (RuntimeTestProtocol.IsMenuDiagnosticScenario(_request.Scenario))
                {
                    bool frameCaptured = _menuDiagnostic != null &&
                        !string.IsNullOrWhiteSpace(_menuDiagnostic.MenuFrameScreenshotSha256);
                    result.Assertions.Add(frameCaptured
                        ? RuntimeTestAssertion.Pass("menu-frame-captured", "end-of-frame png + sha256",
                            _menuDiagnostic.MenuFrameScreenshotSha256)
                        : RuntimeTestAssertion.Fail("menu-frame-captured", "end-of-frame png + sha256", "missing"));
                    bool engineCaptured = _menuDiagnostic != null &&
                        !string.IsNullOrWhiteSpace(_menuDiagnostic.MenuFrameEngineScreenshotSha256);
                    result.Assertions.Add(engineCaptured
                        ? RuntimeTestAssertion.Pass("menu-frame-engine-capture", "engine png + sha256",
                            _menuDiagnostic.MenuFrameEngineScreenshotSha256)
                        : RuntimeTestAssertion.Fail("menu-frame-engine-capture", "engine png + sha256", "missing"));
                    string lumaEvidence = _menuDiagnostic == null || _menuDiagnostic.MenuFrameLuma == null
                        ? "missing" : _menuDiagnostic.MenuFrameLuma.Describe();
                    bool nonBlack = _menuDiagnostic != null && _menuDiagnostic.MenuFrameLuma != null &&
                        _menuDiagnostic.MenuFrameLuma.IsNonBlack;
                    result.Assertions.Add(nonBlack
                        ? RuntimeTestAssertion.Pass("menu-frame-nonblack", "blackFraction<0.98", lumaEvidence)
                        : RuntimeTestAssertion.Fail("menu-frame-nonblack", "blackFraction<0.98", lumaEvidence));
                    bool frameProgressed = _menuDiagnostic != null &&
                        !string.IsNullOrWhiteSpace(_menuDiagnostic.FrameProgressSummary);
                    result.Assertions.Add(frameProgressed
                        ? RuntimeTestAssertion.Pass("menu-frame-progress-observed", "frameCount advanced",
                            _menuDiagnostic.FrameProgressSummary)
                        : RuntimeTestAssertion.Fail("menu-frame-progress-observed", "frameCount advanced", "missing"));
                    bool windowOpened = false;
                    bool windowCaptured = false;
                    if (RuntimeTestProtocol.IsMenuInputDiagnosticScenario(_request.Scenario))
                    {
                        windowOpened = _menuDiagnostic != null && _menuDiagnostic.MenuWindowOpened;
                        result.Assertions.Add(windowOpened
                            ? RuntimeTestAssertion.Pass("menu-loadgame-window-opened",
                                "active SaveLoadWindow after physical click",
                                _menuDiagnostic.MenuWindowDescriptor + ";clickAcknowledged=" +
                                _menuDiagnostic.MenuClickAcknowledged)
                            : RuntimeTestAssertion.Fail("menu-loadgame-window-opened",
                                "active SaveLoadWindow after physical click", "missing"));
                        windowCaptured = _menuDiagnostic != null &&
                            !string.IsNullOrWhiteSpace(_menuDiagnostic.MenuWindowScreenshotSha256);
                        result.Assertions.Add(windowCaptured
                            ? RuntimeTestAssertion.Pass("menu-window-screenshot-captured", "png + sha256",
                                _menuDiagnostic.MenuWindowScreenshotSha256)
                            : RuntimeTestAssertion.Fail("menu-window-screenshot-captured", "png + sha256", "missing"));
                    }
                    if (!frameCaptured || !engineCaptured || !nonBlack || !frameProgressed ||
                        (RuntimeTestProtocol.IsMenuInputDiagnosticScenario(_request.Scenario) &&
                            (!windowOpened || !windowCaptured)))
                    {
                        result.Status = "FAIL";
                        result.Stage = "menu-diagnostic-validation";
                    }
                }
                // A requested window size (-DisplayMode) is judged against the
                // screen the game actually has now.
                object expectedScreenRaw;
                if (!RuntimeTestProtocol.IsPhysicalWorkspaceScenario(_request.Scenario) &&
                    _request.Parameters.TryGetValue("expectedScreen", out expectedScreenRaw))
                {
                    string actualScreen = Screen.width + "x" + Screen.height;
                    bool screenMatches = actualScreen == expectedScreenRaw as string;
                    result.Assertions.Add(screenMatches
                        ? RuntimeTestAssertion.Pass("screen-size", expectedScreenRaw as string,
                            actualScreen + ";fullScreen=" + Screen.fullScreen)
                        : RuntimeTestAssertion.Fail("screen-size", expectedScreenRaw as string,
                            actualScreen + ";fullScreen=" + Screen.fullScreen));
                    if (!screenMatches)
                    {
                        result.Status = "FAIL";
                        result.Stage = "screen-size";
                    }
                }
                if (RuntimeTestProtocol.IsManualWorkspaceScenario(
                        _request.Scenario))
                {
                    // Manual lifecycle contract (review I3): prove the
                    // ready/hold/terminal sequence and zero automatic
                    // editing/input — never manufacture automatic
                    // interaction evidence, and never inherit it. The
                    // terminal coordinator keeps the operator request,
                    // final capture, camera restoration and cleanup as
                    // separate assertions (review J2); none of them is a
                    // usability verdict.
                    ManualTerminalCoordinator terminal = _manualTerminal ??
                        new ManualTerminalCoordinator();
                    terminal.AppendAssertions(result, _manualReadyEvidence,
                        _workspaceInteractionEvidence);
                }
                else if (RuntimeTestProtocol.IsPhysicalWorkspaceScenario(_request.Scenario))
                {
                    // Physical-input acceptance (Unity-free rules in
                    // PhysicalWorkspaceRecord): every action delivered by the
                    // OS and acknowledged, and the view's own state after it.
                    IList<string> violations = _physicalRecord.Violations();
                    result.Assertions.Add(violations.Count == 0
                        ? RuntimeTestAssertion.Pass("physical-workspace",
                            "search typing, wheel, press target, focus loss, Escape (OS input)",
                            "screen=" + _physicalRecord.ScreenWidth + "x" + _physicalRecord.ScreenHeight +
                                ";text=" + _physicalRecord.SearchTextAfterFocus + ";selected=" +
                                _physicalRecord.SelectedBeforeClick + ">" + _physicalRecord.SelectedAfterClick +
                                ";wheel=" + _physicalRecord.WheelEvidence)
                        : RuntimeTestAssertion.Fail("physical-workspace",
                            "search typing, wheel, press target, focus loss, Escape (OS input)",
                            string.Join("|", violations.ToArray())));
                    if (violations.Count != 0)
                    {
                        result.Status = "FAIL";
                        result.Stage = "physical-validation";
                    }
                }
                else if (RuntimeTestProtocol.IsImportScenario(_request.Scenario))
                {
                    // First-open import in game (mission section 11): the
                    // production migration imported the seeded classic plan,
                    // left it byte-unchanged, archived it byte-exact and made
                    // nothing Ready; the planner closed cleanly.
                    string import = _importVerification ?? "missing";
                    bool imported = import.StartsWith("passed=True", StringComparison.Ordinal);
                    result.Assertions.Add(imported
                        ? RuntimeTestAssertion.Pass("workspace-first-open-import",
                            "migrated;classic unchanged;archived;one casting per target;none Ready", import)
                        : RuntimeTestAssertion.Fail("workspace-first-open-import",
                            "migrated;classic unchanged;archived;one casting per target;none Ready", import));
                    result.Assertions.Add(_importWorkspaceClosed
                        ? RuntimeTestAssertion.Pass("import-workspace-closed",
                            "closed;lease released", "closed=True;lease=released")
                        : RuntimeTestAssertion.Fail("import-workspace-closed",
                            "closed;lease released", "not-closed-or-lease-held"));
                    if (!imported || !_importWorkspaceClosed)
                    {
                        result.Status = "FAIL";
                        result.Stage = "import-validation";
                    }
                }
                else if (RuntimeTestProtocol.IsInspectionScenario(_request.Scenario))
                {
                    // Inspection acceptance: evidence written, nothing
                    // submitted (session lock, zero runs), production close
                    // with the input lease released. Frames are recorded
                    // as files but are not a pass condition here.
                    result.Assertions.Add(_inspectionWritten
                        ? RuntimeTestAssertion.Pass("inspection-written",
                            "advanced-inspection.json", _inspectionSummary)
                        : RuntimeTestAssertion.Fail("inspection-written",
                            "advanced-inspection.json", _inspectionFailure ?? "missing"));
                    bool noSubmission = UI.NativeCastingSessionPolicy.Locked &&
                        _inspectionStartedRuns == 0;
                    result.Assertions.Add(noSubmission
                        ? RuntimeTestAssertion.Pass("inspection-no-native-submission",
                            "session locked;0 runs", "locked=True;runs=0;disposition=" +
                                _inspectionDisposition)
                        : RuntimeTestAssertion.Fail("inspection-no-native-submission",
                            "session locked;0 runs", "locked=" +
                                UI.NativeCastingSessionPolicy.Locked + ";runs=" +
                                _inspectionStartedRuns));
                    result.Assertions.Add(_inspectionWorkspaceClosed
                        ? RuntimeTestAssertion.Pass("inspection-workspace-closed",
                            "closed;lease released", "closed=True;lease=released")
                        : RuntimeTestAssertion.Fail("inspection-workspace-closed",
                            "closed;lease released", "not-closed-or-lease-held"));
                    if (!_inspectionWritten || !noSubmission || !_inspectionWorkspaceClosed)
                    {
                        result.Status = "FAIL";
                        result.Stage = "inspection-validation";
                    }
                }
                else if (RuntimeTestProtocol.IsClassicCastScenario(_request.Scenario))
                {
                    // Classic acceptance (Unity-free rules in ClassicCastRecord):
                    // a selection run records the authored plan and never
                    // executes; a cast run claims only the judged steps.
                    IList<string> violations = _classicRecord.Violations();
                    bool planned = _classicRecord.PlanSteps >= 1 && !string.IsNullOrEmpty(_classicRecord.PlanDigest);
                    result.Assertions.Add(planned
                        ? RuntimeTestAssertion.Pass("classic-plan", "authored through the classic controls;>=1 step",
                            "steps=" + _classicRecord.PlanSteps + ";digest=" + _classicRecord.PlanDigest +
                                ";target=" + _classicTarget)
                        : RuntimeTestAssertion.Fail("classic-plan", "authored through the classic controls;>=1 step",
                            "steps=" + _classicRecord.PlanSteps + ";" + string.Join("|", _classicRecord.Failures.ToArray())));
                    result.Assertions.Add(_classicRecord.CastingScenario
                        ? (violations.Count == 0
                            ? RuntimeTestAssertion.Pass("classic-cast",
                                "grant used once;every step confirmed;effects applied;at-will counts unchanged;no finite change",
                                "mode=" + _classicRecord.ExecutionMode + ";confirmed=" + _classicRecord.Confirmed + "/" +
                                    _classicRecord.PlanSteps + ";" + _classicRecord.Grant)
                            : RuntimeTestAssertion.Fail("classic-cast",
                                "grant used once;every step confirmed;effects applied;at-will counts unchanged;no finite change",
                                string.Join("|", violations.ToArray())))
                        : (violations.Count == 0
                            ? RuntimeTestAssertion.Pass("classic-select-no-dispatch", "no grant;no run",
                                "grantArmed=" + _classicRecord.GrantArmedAtEnd + ";grantAttempts=" +
                                    _classicRecord.GrantAttempts + ";castingFirstRuns=" +
                                    _classicRecord.CastingFirstRuns + ";classicRun=" + _classicRecord.ClassicRunSeen)
                            : RuntimeTestAssertion.Fail("classic-select-no-dispatch", "no grant;no run",
                                string.Join("|", violations.ToArray()))));
                    bool classicLocked = UI.NativeCastingSessionPolicy.Locked && _classicWorkspaceClosed;
                    result.Assertions.Add(classicLocked
                        ? RuntimeTestAssertion.Pass("classic-session-locked",
                            "session locked except the single-use grant;workspace closed", "locked=True;closed=True")
                        : RuntimeTestAssertion.Fail("classic-session-locked",
                            "session locked except the single-use grant;workspace closed", "locked=" +
                                UI.NativeCastingSessionPolicy.Locked + ";closed=" + _classicWorkspaceClosed));
                    if (!planned || violations.Count != 0 || !classicLocked)
                    {
                        result.Status = "FAIL";
                        result.Stage = "classic-validation";
                    }
                }
                else if (RuntimeTestProtocol.IsQualificationScenario(_request.Scenario))
                {
                    // Qualification acceptance (Unity-free rules in
                    // CastingQualificationRecord): a selection-only run is a
                    // forecast, never a gameplay claim; a casting run claims
                    // exactly the judged steps.
                    IList<string> violations = _qualificationRecord.Violations();
                    CastingQualificationSelection selection = _qualificationRecord.Selection;
                    int forecastSteps = selection == null ? 3
                        : CastingQualificationRecipe.ForecastSteps(selection.Recipe);
                    bool selected = selection != null && selection.Selected &&
                        _qualificationRecord.Forecast != null &&
                        _qualificationRecord.Forecast.Count == forecastSteps;
                    result.Assertions.Add(selected
                        ? RuntimeTestAssertion.Pass("qualification-selection",
                            "recipe selected;" + forecastSteps + " forecast steps",
                            string.Join(",", selection.Castings.Select(casting => casting.CastingId + "=" +
                                casting.CasterUnitId + ">" + (casting.DirectTargetUnitId ??
                                    (casting.Origin == null ? string.Empty
                                        : casting.Origin.IsCasterCentered ? "caster-centred"
                                        : "anchor:" + casting.Origin.AnchorUnitId))).ToArray()))
                        : RuntimeTestAssertion.Fail("qualification-selection",
                            "recipe selected;" + forecastSteps + " forecast steps",
                            selection == null ? "missing" : selection.Refusal));
                    result.Assertions.Add(_qualificationRecord.CastingScenario
                        ? (violations.Count == 0
                            ? RuntimeTestAssertion.Pass("qualification-run",
                                string.Join("/", _qualificationRecord.JudgedSteps.ToArray()) + " as forecast", "planned=" +
                                    _qualificationRecord.PlannedSubmissions + ";max=" +
                                    _qualificationRecord.MaximumSubmissions)
                            : RuntimeTestAssertion.Fail("qualification-run",
                                string.Join("/", _qualificationRecord.JudgedSteps.ToArray()) + " as forecast",
                                string.Join("|", violations.ToArray())))
                        : (violations.Count == 0 && _qualificationRecord.Submissions.Count == 0 &&
                            (_qualificationHost == null || _qualificationHost.StartedRuns == 0)
                            ? RuntimeTestAssertion.Pass("qualification-selection-only-no-dispatch",
                                "no boundary;no run", "submissions=0;qualificationRuns=0")
                            : RuntimeTestAssertion.Fail("qualification-selection-only-no-dispatch",
                                "no boundary;no run", string.Join("|", violations.ToArray()) +
                                    ";qualificationRuns=" + (_qualificationHost == null ? 0
                                        : _qualificationHost.StartedRuns))));
                    // Review P3-4: evidence that is never vacuous - the player
                    // routes stayed locked, the planner's host ran exactly the
                    // runs the qualification boundary submitted (none before
                    // it, none from any other route), and the workspace closed
                    // with its input lease released.
                    int boundaryRuns = _qualificationRecord.Submissions.Count(value =>
                        value.StartsWith("submitted:", StringComparison.Ordinal));
                    bool playerRoutesQuiet = UI.NativeCastingSessionPolicy.Locked &&
                        _qualificationRunsBefore == 0 &&
                        BuffPlannerUiRoot.CastingRunsStartedForRuntime == boundaryRuns;
                    result.Assertions.Add(playerRoutesQuiet && _qualificationWorkspaceClosed
                        ? RuntimeTestAssertion.Pass("qualification-player-routes-locked",
                            "session locked;host ran only the approved submissions;workspace closed",
                            "locked=True;runs=" + boundaryRuns + ";submitted=" + boundaryRuns + ";closed=True")
                        : RuntimeTestAssertion.Fail("qualification-player-routes-locked",
                            "session locked;host ran only the approved submissions;workspace closed", "locked=" +
                                UI.NativeCastingSessionPolicy.Locked + ";before=" + _qualificationRunsBefore +
                                ";runs=" + BuffPlannerUiRoot.CastingRunsStartedForRuntime + ";submitted=" +
                                boundaryRuns + ";closed=" + _qualificationWorkspaceClosed));
                    if (!selected || violations.Count != 0 || !playerRoutesQuiet ||
                        !_qualificationWorkspaceClosed)
                    {
                        result.Status = "FAIL";
                        result.Stage = "qualification-validation";
                    }
                }
                else if (RuntimeTestProtocol.IsProbeScenario(_request.Scenario))
                {
                    // Probe acceptance (Unity-free rules in
                    // SingleCastProbeRunRecord). A selection-only run is
                    // never a gameplay claim; a casting run claims only what
                    // the executor and the pool snapshot observed.
                    IList<string> violations = _probeRecord.Violations();
                    result.Assertions.Add(_probeRecord.Selected
                        ? RuntimeTestAssertion.Pass("probe-selection", "one plain casting",
                            _probeRecord.SelectionEvidence)
                        : RuntimeTestAssertion.Fail("probe-selection", "one plain casting",
                            _probeRecord.SelectionEvidence ?? "missing"));
                    result.Assertions.Add(_probeRecord.CastingScenario
                        ? (violations.Count == 0
                            ? RuntimeTestAssertion.Pass("probe-native-cast",
                                "allowance valid;1 confirmed;exact resource delta",
                                "projection=" + _probeRecord.ProjectionId + ";effect=" +
                                (_probeRecord.Observation == null ? "none" : _probeRecord.Observation.EffectOutcome) +
                                ";resource=" +
                                (_probeRecord.Observation == null ? "none" : _probeRecord.Observation.ResourceOutcome))
                            : RuntimeTestAssertion.Fail("probe-native-cast",
                                "allowance valid;1 confirmed;exact resource delta",
                                string.Join("|", violations.ToArray())))
                        : (violations.Count == 0
                            ? RuntimeTestAssertion.Pass("probe-selection-only-no-dispatch",
                                "no boundary constructed", "boundary=False;submitted=False")
                            : RuntimeTestAssertion.Fail("probe-selection-only-no-dispatch",
                                "no boundary constructed", string.Join("|", violations.ToArray()))));
                    if (violations.Count != 0)
                    {
                        result.Status = "FAIL";
                        result.Stage = "probe-validation";
                    }
                }
                else if (RuntimeTestProtocol.IsWorkspaceScenario(_request.Scenario))
                {
                    // The workspace scenario's own acceptance gate. The prior
                    // runs passed on identity checks alone (black frame) and
                    // then on the LEGACY screen's IsScreenOpen (the dev
                    // selection never applied); qualification must fail
                    // closed until the workspace ROOT is proven open with
                    // the legacy screen closed AND both capture paths
                    // produced non-black evidence.
                    bool workspaceOpen = _liveInitialCatalogEvidence.Contains("workspaceRoot=active") &&
                        _liveInitialCatalogEvidence.Contains("legacyScreen=closed");
                    result.Assertions.Add(workspaceOpen
                        ? RuntimeTestAssertion.Pass("workspace-screen-open",
                            "workspaceRoot=active;legacyScreen=closed", _liveInitialCatalogEvidence)
                        : RuntimeTestAssertion.Fail("workspace-screen-open",
                            "workspaceRoot=active;legacyScreen=closed",
                            string.IsNullOrWhiteSpace(_liveInitialCatalogEvidence)
                                ? "missing" : _liveInitialCatalogEvidence));
                    bool frameCaptured = !string.IsNullOrWhiteSpace(_liveRenderScreenshotSha256);
                    result.Assertions.Add(frameCaptured
                        ? RuntimeTestAssertion.Pass("workspace-frame-captured",
                            "end-of-frame png + sha256", _liveRenderScreenshotSha256)
                        : RuntimeTestAssertion.Fail("workspace-frame-captured",
                            "end-of-frame png + sha256", "missing"));
                    bool engineCaptured = !string.IsNullOrEmpty(_workspaceEngineScreenshotSha256);
                    result.Assertions.Add(engineCaptured
                        ? RuntimeTestAssertion.Pass("workspace-frame-engine-capture",
                            "engine png + sha256", _workspaceEngineScreenshotSha256)
                        : RuntimeTestAssertion.Fail("workspace-frame-engine-capture",
                            "engine png + sha256", "missing"));
                    string lumaEvidence = _workspaceOpenLumaSummary;
                    bool nonBlack = _workspaceOpenNonBlack;
                    result.Assertions.Add(nonBlack
                        ? RuntimeTestAssertion.Pass("workspace-frame-nonblack",
                            "blackFraction<0.98 (open frame " +
                                (_liveRenderScreenshotSha256 ?? string.Empty) + ")",
                            lumaEvidence)
                        : RuntimeTestAssertion.Fail("workspace-frame-nonblack",
                            "blackFraction<0.98 (open frame " +
                                (_liveRenderScreenshotSha256 ?? string.Empty) + ")",
                            lumaEvidence));
                    // A non-null view field plus a non-black frame proved
                    // insufficient (casting-ws-root-200200: object present,
                    // frame showed only the game HUD). The hierarchy itself
                    // must be active, sized, alpha-visible, and carrying
                    // renderable text.
                    string presentation = _workspacePresentationEvidence ?? "missing";
                    bool presented = presentation.Contains("workspace=present") &&
                        presentation.Contains("activeInHierarchy=True") &&
                        presentation.Contains("canvasEnabled=True") &&
                        !presentation.Contains("alpha=0.00") &&
                        presentation.Contains("renderableTexts=") &&
                        !presentation.Contains("renderableTexts=0");
                    result.Assertions.Add(presented
                        ? RuntimeTestAssertion.Pass("workspace-hierarchy-presents",
                            "active;alpha>0;renderableTexts>0", presentation)
                        : RuntimeTestAssertion.Fail("workspace-hierarchy-presents",
                            "active;alpha>0;renderableTexts>0", presentation));
                    // Matched comparison: a control frame (workspace
                    // closed, same session/build/resolution) must exist
                    // and the open frame must differ from it visibly —
                    // scene animation alone is far below this fraction,
                    // while a full-screen overlay changes most samples.
                    bool controlCaptured = !string.IsNullOrEmpty(_workspaceControlSha256) &&
                        !string.IsNullOrEmpty(_workspaceControlLuma);
                    result.Assertions.Add(controlCaptured
                        ? RuntimeTestAssertion.Pass("workspace-control-frame-captured",
                            "end-of-frame png + sha256 + luma",
                            "controlLuma=" + _workspaceControlLuma)
                        : RuntimeTestAssertion.Fail("workspace-control-frame-captured",
                            "end-of-frame png + sha256 + luma", "missing"));
                    string changeEvidence = "changedFraction=" + _workspaceChangedFraction.ToString(
                        "F5", System.Globalization.CultureInfo.InvariantCulture) +
                        ";openLuma=" + (_workspaceLumaEvidence ?? "missing") +
                        ";controlLuma=" + (_workspaceControlLuma ?? "missing");
                    bool visibleChange = controlCaptured &&
                        _workspaceChangedFraction >= 0.05f;
                    result.Assertions.Add(visibleChange
                        ? RuntimeTestAssertion.Pass("workspace-vs-control-visible-change",
                            "changedFraction>=0.05", changeEvidence)
                        : RuntimeTestAssertion.Fail("workspace-vs-control-visible-change",
                            "changedFraction>=0.05", changeEvidence));
                    // Behavior-based interaction assertions (review R1/C4):
                    // browse-without-mutation, three single-target castings
                    // from two casters, one focused edit, Undo, deliberate
                    // re-edit, save, and reopen with exact identity/order.
                    // Field-based predicate over the structured record the
                    // producer filled (review J1): the State control is valid
                    // as a real click or a legitimately already-Ready draft
                    // (review I5), and the exact-record checks carry the
                    // correctness burden. Never a substring of the log text.
                    string interactionEvidence = _workspaceInteraction.Describe();
                    IList<string> interactionViolations =
                        _workspaceInteraction.Violations();
                    bool interactions = interactionViolations.Count == 0;
                    if (!interactions)
                        interactionEvidence += ";violations=" +
                            string.Join(",", interactionViolations.ToArray());
                    result.Assertions.Add(interactions
                        ? RuntimeTestAssertion.Pass("workspace-interaction-sequence",
                            "browse;3 casts/2 casters;edit;undo;save",
                            interactionEvidence)
                        : RuntimeTestAssertion.Fail("workspace-interaction-sequence",
                            "browse;3 casts/2 casters;edit;undo;save",
                            interactionEvidence));
                    string reopenEvidence = _workspaceReopenEvidence ?? "missing";
                    bool reopenPreserved = reopenEvidence.Contains("preserved=True");
                    result.Assertions.Add(reopenPreserved
                        ? RuntimeTestAssertion.Pass("workspace-reopen-preserves-intent",
                            "exact IDs and order after close/reopen", reopenEvidence)
                        : RuntimeTestAssertion.Fail("workspace-reopen-preserves-intent",
                            "exact IDs and order after close/reopen", reopenEvidence));
                    bool reloadPassed = true;
                    if (RuntimeTestProtocol.IsReloadScenario(_request.Scenario))
                    {
                        string reloadEvidence = _workspaceReloadEvidence ?? "missing";
                        reloadPassed = reloadEvidence.StartsWith("passed=True", StringComparison.Ordinal);
                        result.Assertions.Add(reloadPassed
                            ? RuntimeTestAssertion.Pass("workspace-reload-preserves-plan-and-ownership",
                                "saved plan, same campaign, one subscription, one HUD root, no run, clean",
                                reloadEvidence)
                            : RuntimeTestAssertion.Fail("workspace-reload-preserves-plan-and-ownership",
                                "saved plan, same campaign, one subscription, one HUD root, no run, clean",
                                reloadEvidence));
                    }
                    // The pointer highlight (addendum v1.1): judged on Unity's
                    // own control states by HoverOwnershipRecord; the rc6
                    // ghost must be reproduced for the fix to mean anything.
                    bool hoverOwned = true;
                    if (string.Equals(_request.Scenario, "live-workspace-qual", StringComparison.Ordinal))
                    {
                        PlannerUiReproduction.Rc6ButtonBehaviour = false;
                        WriteHoverEvidence();
                        IList<string> hoverViolations = _hover.Violations();
                        hoverOwned = hoverViolations.Count == 0;
                        string hoverEvidence = _hover.Describe() + (hoverOwned ? string.Empty
                            : ";violations=" + string.Join(",", hoverViolations.ToArray()));
                        const string hoverExpected =
                            "one highlight, under the pointer, gone on exit; rc6 ghost reproduced; five button states";
                        result.Assertions.Add(hoverOwned
                            ? RuntimeTestAssertion.Pass("workspace-hover-ownership", hoverExpected, hoverEvidence)
                            : RuntimeTestAssertion.Fail("workspace-hover-ownership", hoverExpected, hoverEvidence));
                    }
                    if (!workspaceOpen || !frameCaptured || !engineCaptured ||
                        !nonBlack || !presented || !controlCaptured || !visibleChange)
                    {
                        result.Status = "FAIL";
                        result.Stage = "workspace-visual-validation";
                    }
                    else if (!interactions || !reopenPreserved)
                    {
                        result.Status = "FAIL";
                        result.Stage = "workspace-interaction-validation";
                    }
                    else if (!reloadPassed)
                    {
                        result.Status = "FAIL";
                        result.Stage = "workspace-reload-validation";
                    }
                    else if (!hoverOwned)
                    {
                        result.Status = "FAIL";
                        result.Stage = "workspace-hover-validation";
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
                    string hashTarget = LoadedAssemblyIdentity.ResolveCanonicalFile(loaded.Location);
                    if (!string.Equals(hashTarget, loaded.Location, StringComparison.OrdinalIgnoreCase))
                        _log.Info("[KBP-BOOT] mono image sidecar detected;hashing canonical assembly;loaded=" +
                            loaded.Location + ";canonical=" + hashTarget + ".");
                    string loadedHash = Hashing.Sha256(hashTarget);
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
                    AddUiAssertion(result, "ui-hud-row-left-aligned-native",
                        ui.HudRowLeftAlignedWithNativeCluster, "true",
                        ui.HudRowLeftAlignedWithNativeCluster.ToString());
                    AddUiAssertion(result, "ui-hud-glyphs-centered", ui.HudGlyphsCentered,
                        "true", ui.HudGlyphsCentered.ToString());
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
                    bool longFlowValid = liveUi
                        ? ui.LongPointerEnterCount >= 2 && ui.LongPointerEventCount == 2 &&
                            ui.LongListenerCount == 2 && ui.LongGroupResolvedCount == 2 &&
                            ui.LongPlanRevalidatedCount == 2 && ui.LongResultPresentedCount == 2
                        : ui.LongPointerEnterCount == 1 && ui.LongPointerEventCount == 1 &&
                            ui.LongListenerCount == 1 && ui.LongGroupResolvedCount == 1 &&
                            ui.LongPlanRevalidatedCount == 1 && ui.LongExecutionInvokedCount == 0 &&
                            ui.LongRefusalCount == 1 && ui.LongResultPresentedCount == 1;
                    AddUiAssertion(result, "ui-long-flow-once", longFlowValid,
                        liveUi ? ">=2/2/2/2/2/*/*/2" : "1/1/1/1/1/0/1/1", ui.LongPointerEnterCount + "/" +
                        ui.LongPointerEventCount + "/" + ui.LongListenerCount + "/" +
                        ui.LongGroupResolvedCount + "/" + ui.LongPlanRevalidatedCount + "/" +
                        ui.LongExecutionInvokedCount + "/" + ui.LongRefusalCount + "/" +
                        ui.LongResultPresentedCount);
                    AddUiAssertion(result, "ui-long-empty-feedback",
                        ui.LongResultMessage == "No Long buffs are configured.",
                        "No Long buffs are configured.", ui.LongResultMessage ?? "missing");
                    AddUiAssertion(result, "ui-tooltip-identities",
                        !string.IsNullOrWhiteSpace(ui.SetupTooltip) && ui.SetupTooltip.Contains("Ctrl+Shift+B") &&
                        !string.IsNullOrWhiteSpace(ui.LongTooltip) && ui.LongTooltip.Contains("Long"),
                        "setup/Ctrl+Shift+B and Long", (ui.SetupTooltip ?? "missing") + " | " + (ui.LongTooltip ?? "missing"));
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
                        AddUiAssertion(result, "ui-live-catalog-visible",
                            ui.CatalogVisibleViewModels > 0 && ui.CatalogInstantiatedRows == 32 &&
                            ui.CatalogActiveRows > 0 && ui.CatalogActiveRows <= 32 &&
                            ui.CatalogVisibleRows > 0 && ui.CatalogSelectedDetailsBound &&
                            !string.IsNullOrWhiteSpace(_liveInitialCatalogEvidence),
                            "post-cast VMs>0; pool=32; active/visible/details", ui.CatalogEvidence ?? "missing");
                        bool liveRenderValid = _liveRenderDiagnostics != null &&
                            _liveRenderDiagnostics.ExpectedNames != null &&
                            _liveRenderDiagnostics.ExpectedNames.Length == 5 &&
                            _liveRenderDiagnostics.RowScreenRectangles != null &&
                            _liveRenderDiagnostics.RowScreenRectangles.Length == 5 &&
                            _liveRenderDiagnostics.BoundRowCount >= 5 &&
                            _liveRenderDiagnostics.CanaryEvidence == "absent" &&
                            _liveRenderDiagnostics.SelectedRowName ==
                                _liveRenderDiagnostics.DetailsTitleText &&
                            !string.IsNullOrWhiteSpace(_liveRenderScreenshotSha256);
                        AddUiAssertion(result, "ui-live-production-render-evidence", liveRenderValid,
                            "five rows/rectangles; selected=details; canary absent; screenshot hash",
                            _liveRenderDiagnostics == null ? "missing" :
                                string.Join("|", _liveRenderDiagnostics.ExpectedNames) + ";selected=" +
                                _liveRenderDiagnostics.SelectedRowName + ";details=" +
                                _liveRenderDiagnostics.DetailsTitleText + ";canary=" +
                                _liveRenderDiagnostics.CanaryEvidence + ";sha256=" +
                                _liveRenderScreenshotSha256);
                        bool blessMaterialContract = _liveInitialCatalogEvidence.Contains(
                            "material=blueprint=90e59f4a4ada87243b7b3535a06d0638") &&
                            _liveInitialCatalogEvidence.Contains("require=False") &&
                            _liveInitialCatalogEvidence.Contains("item=none") &&
                            _liveInitialCatalogEvidence.Contains("hasEnough=False") &&
                            _liveInitialCatalogEvidence.Contains("consumableRequired=False");
                        AddUiAssertion(result, "ui-bless-material-contract", blessMaterialContract,
                            "require=false/item=none/hasEnough=false/consumable=false",
                            _liveInitialCatalogEvidence);
                        bool catalogControls = _liveCatalogControlEvidence.Contains(
                                "All=" + ui.CatalogVisibleViewModels) &&
                            _liveCatalogControlEvidence.Contains("Spells=") &&
                            _liveCatalogControlEvidence.Contains("Abilities=") &&
                            _liveCatalogControlEvidence.Contains("Other=") &&
                            _liveCatalogControlEvidence.Contains("longSelected=1") &&
                            _liveCatalogControlEvidence.Contains("importantSelected=0") &&
                            _liveCatalogControlEvidence.Contains("longRestored=1") &&
                            _liveCatalogControlEvidence.Contains("selectedOnlyOff=True");
                        AddUiAssertion(result, "ui-catalog-controls-and-routine-local-selection",
                            catalogControls, "all categories; Long=1/Important=0/Long=1",
                            _liveCatalogControlEvidence);
                        AddUiAssertion(result, "ui-physical-tooltip-stable",
                            _liveTooltipStable && !string.IsNullOrWhiteSpace(_liveTooltipEvidence) &&
                            ui.TooltipListenerCount == 4 && ui.TooltipRaycastGraphicCount == 0 &&
                            !ui.TooltipBlocksRaycasts && ui.TooltipNativeTriggerCount == 4 &&
                            ui.TooltipUsesNativeParchmentPresentation,
                            ">=5 seconds/inside/4 native triggers/0 raycast/blocks=false",
                            _liveTooltipEvidence + ";finalListeners=" + ui.TooltipListenerCount);
                        AddUiAssertion(result, "ui-physical-pointer-isolation",
                            ui.PhysicalInputPlayerCommandCount == 0 &&
                            ui.PhysicalInputMovementCommandCount == 0 &&
                            ui.PhysicalInputAbilityCommandCount == 0 &&
                            ui.PhysicalInputSelectionEventCount == 0 &&
                            ui.PhysicalInputAbilityTargetEventCount == 0 &&
                            ui.PhysicalInputSelectionUnchanged && ui.PhysicalInputCameraUnchanged &&
                            ui.HudUnderlyingNativeActivationCount == 0,
                            "0/0/0/0/0/unchanged/unchanged/native=0",
                            ui.PhysicalInputPlayerCommandCount + "/" +
                            ui.PhysicalInputMovementCommandCount + "/" +
                            ui.PhysicalInputAbilityCommandCount + "/" +
                            ui.PhysicalInputSelectionEventCount + "/" +
                            ui.PhysicalInputAbilityTargetEventCount + "/" +
                            ui.PhysicalInputSelectionUnchanged + "/" +
                            ui.PhysicalInputCameraUnchanged + "/" +
                            ui.HudUnderlyingNativeActivationCount);
                        // The configured Long press reaches the classic
                        // routine execution, which the session lock refuses:
                        // nothing is submitted in an automation session.
                        bool configuredOutcome = !string.IsNullOrWhiteSpace(
                            ui.ConfiguredLongResultMessage) &&
                            ui.ConfiguredLongDisposition == "Refused" &&
                            ui.ConfiguredLongSubmitted == 0 && ui.ConfiguredLongConfirmed == 0 &&
                            ui.ConfiguredLongResultMessage.Contains("automated test session");
                        AddUiAssertion(result, "ui-quick-visible-results",
                            ui.LongResultMessage == "No Long buffs are configured." &&
                            ui.ImportantResultMessage == "No Important buffs are configured." &&
                            ui.ShortResultMessage == "No Short buffs are configured." &&
                            _liveBlessSelectedAndConfigured && configuredOutcome,
                            "three explicit empty outcomes + configured Long refused by the session lock",
                            ui.LongResultMessage + " | " + ui.ImportantResultMessage + " | " +
                            ui.ShortResultMessage + " | " + ui.ConfiguredLongDisposition + ": " +
                            ui.ConfiguredLongResultMessage);
                        AddUiAssertion(result, "ui-hotkey-armed-and-observed",
                            ui.HotkeyArmed && ui.HotkeyKeydownCount >= 1, "true/>=1",
                            ui.HotkeyArmed + "/" + ui.HotkeyKeydownCount);
                        AddUiAssertion(result, "ui-no-duplicate-full-screen-objects",
                            ui.ScreenCreateCount == ui.ScreenDestroyCount + 1 &&
                            ui.ScreenCreateCount == ui.ScreenDestroyCountAfterClose,
                            "one-open-before-close/zero-after-close", ui.ScreenCreateCount + "/" +
                            ui.ScreenDestroyCount + "/" + ui.ScreenDestroyCountAfterClose);
                        AddUiAssertion(result, "ui-hud-object-evidence",
                            !string.IsNullOrWhiteSpace(ui.HudObjectEvidence) &&
                            ui.HudObjectEvidence.Contains("corners=") &&
                            ui.HudObjectEvidence.Contains("active=True") &&
                            ui.HudObjectEvidence.Contains("style=targetSprite=True") &&
                            ui.HudObjectEvidence.Contains("normal=") &&
                            ui.HudObjectEvidence.Contains("highlighted=") &&
                            ui.HudObjectEvidence.Contains("pressed=") &&
                            ui.HudObjectEvidence.Contains("disabled=") &&
                            ui.HudObjectEvidence.Split(new[] { "nativeSkin=True" },
                                StringSplitOptions.None).Length == 5 &&
                            ui.HudObjectEvidence.Split(new[] { "spriteInk=1.000,1.000,1.000,1.000" },
                                StringSplitOptions.None).Length == 5,
                            "paths/ids/active/corners + captured native skin + four neutral alpha glyphs",
                            ui.HudObjectEvidence ?? "missing");
                        AddUiAssertion(result, "ui-text-native-pixel-path",
                            _liveRenderDiagnostics != null &&
                            _liveRenderDiagnostics.NestedCanvasScalerCount == 0 &&
                            _liveRenderDiagnostics.FractionalRectCount == 0 &&
                            _liveRenderDiagnostics.TextRenderingEvidence.Contains("font=Arial") &&
                            _liveRenderDiagnostics.TextRenderingEvidence.Contains("bestFit=False") &&
                            _liveRenderDiagnostics.TextRenderingEvidence.Contains("shader=UI/Default"),
                            "no-scaler/no-fractional/Arial/fixed/UI-Default",
                            _liveRenderDiagnostics == null ? "missing" :
                                _liveRenderDiagnostics.TextRenderingEvidence + ";fractional=" +
                                _liveRenderDiagnostics.FractionalRectCount);
                        AddUiAssertion(result, "ui-card-aggregation-evidence",
                            ui.CatalogAggregateAbilityCount >= ui.CatalogVisibleViewModels &&
                            ui.CatalogProviderCount >= ui.CatalogAggregateAbilityCount &&
                            ui.CatalogConsolidatedCardCount >= 1 &&
                            ui.CatalogBlessEvidence.Contains("providers=2,abilities=2"),
                            "providers>=abilities>=cards", ui.CatalogProviderCount + "/" +
                            ui.CatalogAggregateAbilityCount + "/" + ui.CatalogVisibleViewModels +
                            ";consolidated=" + ui.CatalogConsolidatedCardCount);
                        AddUiAssertion(result, "ui-direct-target-state-visible",
                            _liveDirectSelectedTargetCount >= 1, ">=1",
                            _liveDirectSelectedTargetCount.ToString());
                        AddUiAssertion(result, "exact-working-save-load",
                            _liveSaveLoader != null && _liveSaveLoader.LoadActionCount == 1 &&
                            !string.IsNullOrWhiteSpace(_liveSaveLoader.WorkingDescriptor) &&
                            !string.IsNullOrWhiteSpace(_liveSaveLoader.BaselineDescriptor),
                            "one/distinct working+baseline", _liveSaveLoader == null ? "missing" :
                            _liveSaveLoader.LoadActionCount + "/" + _liveSaveLoader.WorkingDescriptor +
                            "/" + _liveSaveLoader.BaselineDescriptor);
                        AddUiAssertion(result, "save-chain-handler-observed",
                            _liveSaveLoader != null && _liveSaveLoader.HandlerInvocationCount == 1,
                            "1", _liveSaveLoader == null ? "missing" :
                            _liveSaveLoader.HandlerInvocationCount.ToString());
                        AddUiAssertion(result, "save-chain-catalog-and-descriptors",
                            _liveSaveLoader != null && _liveSaveLoader.CatalogInvocationCount == 1 &&
                            _liveSaveLoader.WorkingMatchCount == 1 &&
                            _liveSaveLoader.BaselineMatchCount == 1,
                            "1/1/1", _liveSaveLoader == null ? "missing" :
                            _liveSaveLoader.CatalogInvocationCount + "/" +
                            _liveSaveLoader.WorkingMatchCount + "/" +
                            _liveSaveLoader.BaselineMatchCount + ";catalogDescriptors=" +
                            _liveSaveLoader.CatalogDescriptorCount);
                        AddUiAssertion(result, "save-chain-receiver-correlation",
                            _liveSaveLoader != null && _liveSaveLoader.SlotReceiverCorrelated &&
                            _liveSaveLoader.WindowReceiverCorrelated &&
                            _liveSaveLoader.WindowArgumentCorrelated,
                            "slot/window/windowArgument", _liveSaveLoader == null ? "missing" :
                            _liveSaveLoader.SlotReceiverCorrelated + "/" +
                            _liveSaveLoader.WindowReceiverCorrelated + "/" +
                            _liveSaveLoader.WindowArgumentCorrelated);
                        AddUiAssertion(result, "save-chain-load-entry-and-completion",
                            _liveSaveLoader != null && _liveSaveLoader.LoadEntryCorrelated &&
                            _liveSaveLoader.CompletionCallbackObserved &&
                            !_liveSaveLoader.UnexpectedSaveWriteObserved &&
                            !_liveSaveLoader.WrongThreadObserved &&
                            !_liveSaveLoader.OrderingViolationObserved,
                            "correlated/callback/no-write", _liveSaveLoader == null ? "missing" :
                            _liveSaveLoader.LoadEntryCorrelated + "/" +
                            _liveSaveLoader.CompletionCallbackObserved + "/" +
                            !_liveSaveLoader.UnexpectedSaveWriteObserved + ";sequences=" +
                            _liveSaveLoader.ChainSequences);
                        AddUiAssertion(result, "save-chain-fingerprint",
                            _liveSaveLoader != null &&
                            !string.IsNullOrWhiteSpace(_liveSaveLoader.FingerprintEvidence),
                            "stable gameId+party", _liveSaveLoader == null ||
                            string.IsNullOrWhiteSpace(_liveSaveLoader.FingerprintEvidence)
                                ? "missing" : _liveSaveLoader.FingerprintEvidence);
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
                        !longFlowValid ||
                        ui.LongResultMessage != "No Long buffs are configured." ||
                        string.IsNullOrWhiteSpace(ui.SetupTooltip) || !ui.SetupTooltip.Contains("Ctrl+Shift+B") ||
                        string.IsNullOrWhiteSpace(ui.LongTooltip) || !ui.LongTooltip.Contains("Long") ||
                        ui.InputPlayerCommandCount != 0 || ui.InputMovementCommandCount != 0 ||
                        ui.InputAbilityCommandCount != 0 || ui.InputSelectionEventCount != 0 ||
                        ui.InputAbilityTargetEventCount != 0 || !ui.InputSelectionUnchanged ||
                        !ui.InputCameraUnchanged || !ui.InputScrollConsumed || !ui.InputCancelConsumed ||
                        !ui.GroupSelectorChanged ||
                        ui.ReconstructionCount != expectedReconstructions ||
                        (liveUi && (!ui.HotkeyArmed || ui.HotkeyKeydownCount < 1 ||
                            ui.CatalogVisibleViewModels <= 0 ||
                            ui.CatalogInstantiatedRows != 32 || ui.CatalogActiveRows > 32 ||
                            ui.CatalogActiveRows <= 0 || ui.CatalogVisibleRows <= 0 ||
                            !ui.CatalogSelectedDetailsBound ||
                            string.IsNullOrWhiteSpace(ui.CatalogBlessEvidence) ||
                            string.IsNullOrWhiteSpace(_liveInitialCatalogEvidence) ||
                            !_liveInitialCatalogEvidence.Contains("rowVisible=True") ||
                            !_liveInitialCatalogEvidence.Contains("require=False") ||
                            !_liveInitialCatalogEvidence.Contains("item=none") ||
                            !_liveInitialCatalogEvidence.Contains("hasEnough=False") ||
                            !_liveInitialCatalogEvidence.Contains("consumableRequired=False") ||
                            _liveRenderDiagnostics == null ||
                            _liveRenderDiagnostics.ExpectedNames == null ||
                            _liveRenderDiagnostics.ExpectedNames.Length != 5 ||
                            _liveRenderDiagnostics.RowScreenRectangles == null ||
                            _liveRenderDiagnostics.RowScreenRectangles.Length != 5 ||
                            _liveRenderDiagnostics.BoundRowCount < 5 ||
                            _liveRenderDiagnostics.CanaryEvidence != "absent" ||
                            _liveRenderDiagnostics.SelectedRowName !=
                                _liveRenderDiagnostics.DetailsTitleText ||
                            string.IsNullOrWhiteSpace(_liveRenderScreenshotSha256) ||
                            !_liveTooltipStable || ui.TooltipListenerCount != 4 ||
                            ui.TooltipRaycastGraphicCount != 0 || ui.TooltipBlocksRaycasts ||
                            ui.TooltipNativeTriggerCount != 4 ||
                            !ui.TooltipUsesNativeParchmentPresentation ||
                            ui.SetupOpenSoundCount < 1 ||
                            ui.PhysicalInputPlayerCommandCount != 0 ||
                            ui.PhysicalInputMovementCommandCount != 0 ||
                            ui.PhysicalInputAbilityCommandCount != 0 ||
                            ui.PhysicalInputSelectionEventCount != 0 ||
                            ui.PhysicalInputAbilityTargetEventCount != 0 ||
                            !ui.PhysicalInputSelectionUnchanged || !ui.PhysicalInputCameraUnchanged ||
                            ui.ImportantResultMessage != "No Important buffs are configured." ||
                            ui.ShortResultMessage != "No Short buffs are configured." ||
                            !_liveBlessSelectedAndConfigured ||
                            string.IsNullOrWhiteSpace(ui.ConfiguredLongResultMessage) ||
                            ui.ConfiguredLongDisposition != "Refused" ||
                            ui.ConfiguredLongSubmitted != 0 || ui.ConfiguredLongConfirmed != 0 ||
                            ui.ScreenCreateCount != ui.ScreenDestroyCount + 1 ||
                            ui.ScreenCreateCount != ui.ScreenDestroyCountAfterClose ||
                            string.IsNullOrWhiteSpace(ui.HudObjectEvidence) ||
                            !ui.HudObjectEvidence.Contains("corners=") ||
                            _liveSaveLoader == null || _liveSaveLoader.LoadActionCount != 1 ||
                            _liveSaveLoader.HandlerInvocationCount != 1 ||
                            _liveSaveLoader.CatalogInvocationCount != 1 ||
                            _liveSaveLoader.WorkingMatchCount != 1 ||
                            _liveSaveLoader.BaselineMatchCount != 1 ||
                            !_liveSaveLoader.SlotReceiverCorrelated ||
                            !_liveSaveLoader.WindowReceiverCorrelated ||
                            !_liveSaveLoader.WindowArgumentCorrelated ||
                            !_liveSaveLoader.LoadEntryCorrelated ||
                            !_liveSaveLoader.CompletionCallbackObserved ||
                            _liveSaveLoader.UnexpectedSaveWriteObserved ||
                            _liveSaveLoader.WrongThreadObserved ||
                            _liveSaveLoader.OrderingViolationObserved)))
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

        private bool UpdateMenuDiagnosticScenario()
        {
            string stop = LiveRunStopReason();
            if (stop != null)
                throw new TimeoutException("Menu diagnostics stopped;" + stop);
            if (_menuDiagnostic == null)
                _menuDiagnostic = new MenuRenderDiagnostic(
                    _request, _log,
                    RuntimeTestProtocol.IsMenuInputDiagnosticScenario(_request.Scenario),
                    WritePhysicalInputRequest);
            if (!_menuDiagnostic.IsComplete)
            {
                _menuDiagnostic.Update();
                return false;
            }
            return true;
        }

        private DateTime? _processStartUtc;

        // Final review C3 and its re-review: the run's own overall deadline
        // and the launcher's abort marker (see RuntimeTestProtocol.RunStopReason),
        // checked on every update from the first one on, the campaign load and
        // the menu diagnostics included; the deadline is logged once.
        private string LiveRunStopReason()
        {
            if (_processStartUtc == null)
            {
                try
                {
                    _processStartUtc = System.Diagnostics.Process.GetCurrentProcess().StartTime
                        .ToUniversalTime();
                }
                catch (Exception) { _processStartUtc = _startedAtUtc; }
                int margin = RuntimeTestProtocol.OverallDeadlineMarginSeconds(_request.TimeoutSeconds);
                _log.Info("[KBP-RT] overall deadline " + _processStartUtc.Value
                    .AddSeconds(_request.TimeoutSeconds - margin).ToString("o") + ";timeout=" +
                    _request.TimeoutSeconds + ";margin=" + margin + ".");
            }
            bool abort;
            try
            {
                abort = File.Exists(Path.Combine(_request.EvidenceDirectory,
                    RuntimeTestProtocol.AbortMarkerFileName));
            }
            catch (Exception) { abort = true; }
            return RuntimeTestProtocol.RunStopReason(DateTime.UtcNow, _processStartUtc.Value,
                _request.TimeoutSeconds, abort);
        }

        private bool UpdateLiveUiScenario()
        {
            string stop = LiveRunStopReason();
            if (stop != null)
                throw new TimeoutException("Live run stopped;" + stop + ";phase=" + _liveUiPhase);
            if (_liveSaveLoader == null)
                _liveSaveLoader = new LiveCampaignSaveLoader(_request, _log);
            if (!_liveSaveLoader.IsComplete)
            {
                _liveSaveLoader.Update();
                return false;
            }
            _uiSmokeUpdates++;
            // Wall-clock budget: an unfocused Unity player can dispatch far
            // more updates per second than 60, so update counts must never
            // govern this timeout (the campaign load itself is budgeted by
            // the save loader's per-stage stopwatch).
            if (_livePhaseElapsed == null) _livePhaseElapsed = System.Diagnostics.Stopwatch.StartNew();
            double liveBudgetSeconds = 300;
            if (RuntimeTestProtocol.IsManualWorkspaceScenario(
                    _request.Scenario))
                liveBudgetSeconds = 600 +
                    RuntimeTestProtocol.ReadManualHoldSeconds(
                        _request.Parameters);
            if (RuntimeTestProtocol.IsProbeScenario(_request.Scenario))
                liveBudgetSeconds = 600 + RuntimeTestProtocol.ProbeRunDeadlineSeconds;
            if (RuntimeTestProtocol.IsQualificationScenario(_request.Scenario))
                liveBudgetSeconds = 600 + RuntimeTestProtocol.QualificationRunDeadlineSeconds;
            if (RuntimeTestProtocol.IsClassicCastScenario(_request.Scenario))
                liveBudgetSeconds = 600 + RuntimeTestProtocol.ClassicRunDeadlineSeconds;
            if (RuntimeTestProtocol.IsPhysicalWorkspaceScenario(_request.Scenario))
                liveBudgetSeconds = 600 + (int)PhysicalDeadlineSeconds;
            if (RuntimeTestProtocol.IsReloadScenario(_request.Scenario))
                liveBudgetSeconds = 300 + LiveCampaignSaveLoader.ReloadBudgetSeconds;
            if (_livePhaseElapsed.Elapsed.TotalSeconds > liveBudgetSeconds)
                throw new TimeoutException("Live UI scenario timed out;phase=" + _liveUiPhase +
                    ";elapsedSeconds=" + _livePhaseElapsed.Elapsed.TotalSeconds.ToString("F1",
                        System.Globalization.CultureInfo.InvariantCulture) +
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
                    // Close UMM through its verified lifecycle API:
                    // UI.Instance.ToggleWindow(false) performs hide handling,
                    // removes the input blocker, restores cursor state, and
                    // invokes the game callback. Never destroy the manager
                    // or manually remove its blocker.
                    try
                    {
                        var uiType = typeof(UnityModManager.ModEntry).Assembly
                            .GetType("UnityModManagerNet.UnityModManager+UI");
                        if (uiType != null)
                        {
                            var instanceProp = uiType.GetProperty("Instance",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Static);
                            var openedProp = uiType.GetProperty("Opened",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            var toggleMethod = uiType.GetMethod("ToggleWindow",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance,
                                null, new[] { typeof(bool) }, null);
                            if (instanceProp != null && openedProp != null && toggleMethod != null)
                            {
                                object ui = instanceProp.GetValue(null, null);
                                if (ui != null)
                                {
                                    bool wasOpened = (bool)openedProp.GetValue(ui, null);
                                    _log.Info("[KBP-BOOT] UMM close requested;wasOpened=" + wasOpened);
                                    if (wasOpened)
                                    {
                                        toggleMethod.Invoke(ui, new object[] { false });
                                        bool nowOpened = (bool)openedProp.GetValue(ui, null);
                                        _log.Info("[KBP-BOOT] UMM close result;nowOpened=" + nowOpened);
                                        if (!nowOpened)
                                        {
                                            // TERMINAL state consumed by every pending
                                            // automatic input path (review F7): the
                                            // launcher suppresses its physical Escape
                                            // deliveries once UMM is closed here.
                                            AtomicFile.WriteUtf8(
                                                Path.Combine(_request.EvidenceDirectory,
                                                    "programmatic-umm-closed.json"),
                                                "{\"runId\":\"" + _request.RunId +
                                                "\",\"stage\":\"programmatic-umm-closed\"}" +
                                                Environment.NewLine);
                                        }
                                        else
                                            _log.Info("[KBP-BOOT] UMM ToggleWindow(false) returned but Opened is still true.");
                                    }
                                }
                                else
                                {
                                    _log.Info("[KBP-BOOT] UMM UI.Instance is null; initialization may be pending.");
                                }
                            }
                            else
                            {
                                _log.Info("[KBP-BOOT] UMM UI contract incomplete; cannot close programmatically.");
                            }
                        }
                    }
                    catch (Exception dismissException)
                    {
                        _log.Error("[KBP-BOOT] UMM programmatic close failed: " +
                            dismissException.Message, dismissException);
                    }
                }
                if (StaticCanvas.Instance == null ||
                    UnityEngine.EventSystems.EventSystem.current == null) return false;
                if (_nativeUiContract == null)
                {
                    _nativeUiContract = NativeUiContractProbe.Capture();
                    AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory,
                        "native-ui-contract.json"), Serialize(_nativeUiContract));
                    _log.Info("[KBP-UI-THEME] captured exact native campaign visual contract;" +
                        "visuals=" + _nativeUiContract.Visuals.Count + ";fonts=" +
                        _nativeUiContract.Fonts.Count + ";portraits=" +
                        _nativeUiContract.Portraits.Count + ".");
                }
                if (!BuffPlannerUiRoot.IsHudInstalled) return false;
                // The harness may have just closed a native Escape veil after dismissing
                // UMM. Capture the gameplay state that the planner is actually opening
                // from, not the transient pre-dismiss menu state.
                BuffPlannerUiRoot.CaptureRuntimeBaseline(true);
                if (!_liveHotkeyMarkerWritten &&
                    RuntimeTestProtocol.IsWorkspaceScenario(_request.Scenario) &&
                    !_workspaceControlRequested)
                {
                    // The matched control frame is captured BEFORE any
                    // opening route (physical, programmatic, or manual) so
                    // none can bypass it (reviews R6, I1).
                    _workspaceControlRequested = true;
                    _log.Info("[KBP-WORKSPACE] capturing control frame before opening;" +
                        MenuRenderDiagnostic.EnvironmentSample() + ".");
                    BeginWorkspaceCapture("workspace-control-frame.png");
                    BeginWorkspaceCameraCapture("workspace-camera-control.png", true);
                    _liveUiPhase = 24;
                    return false;
                }
                if (!_liveHotkeyMarkerWritten &&
                    !RuntimeTestProtocol.IsNoInputWorkspaceScenario(
                        _request.Scenario))
                {
                    AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "hotkey-ready.json"),
                        "{\"runId\":\"" + _request.RunId + "\",\"armed\":" +
                        (Main.HotkeyArmed ? "true" : "false") + ",\"binding\":\"Ctrl+Shift+B\",\"snapshot\":" +
                        JsonConvert.ToString(BuffPlannerUiRoot.GetSnapshot()) + "}" + Environment.NewLine);
                    _liveHotkeyMarkerWritten = true;
                    _log.Info("[KBP-BOOT] runtime requests physical planner hotkey;binding=Ctrl+Shift+B;marker=hotkey-ready.json.");
                }
                if (!Main.HotkeyArmed || Main.HotkeyKeydownCount < 1)
                {
                    // Foreground activation may fail in the automated
                    // context. After a bounded wait (control frame already
                    // captured at the hotkey-request point), open through
                    // the production path without physical input.
                    if (_uiSmokeUpdates > 300 && !_workspaceProgrammaticOpen &&
                        _workspaceControlRequested)
                    {
                        _liveUiPhase = 22;
                        return false;
                    }
                    return false;
                }
                _liveUiPhase = 1;
                return false;
            }
            if (_liveUiPhase == 24)
            {
                // Control frame consumed for EVERY workspace scenario. For
                // the manual scenario no hotkey request is ever written and
                // no physical input is ever awaited: the candidate opens
                // through the production path immediately (review I1).
                if (!ConsumeWorkspaceCapture("workspace-control-frame.png")) return false;
                _workspaceControlLuma = _workspaceFrameCapture.Summary == null
                    ? "missing" : _workspaceFrameCapture.Summary.Describe();
                _workspaceControlSha256 =
                    Hashing.Sha256(_workspaceFrameCapture.FullPath);
                _workspaceControlSamples = _workspaceFrameCapture.Samples;
                if (RuntimeTestProtocol.IsNoInputWorkspaceScenario(
                        _request.Scenario))
                {
                    _liveHotkeyMarkerWritten = true;
                    if (RuntimeTestProtocol.IsImportScenario(_request.Scenario)) SeedClassicPlanForImport();
                    _log.Info("[KBP-MANUAL] control frame consumed; opening the " +
                        "candidate programmatically with no input request.");
                    _liveUiPhase = 22;
                    return false;
                }
                // Automated qualification: the hotkey request marker is
                // written and phase 0 resumes its armed/fallback flow.
                AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory,
                    "hotkey-ready.json"),
                    "{\"runId\":\"" + _request.RunId + "\",\"armed\":" +
                    (Main.HotkeyArmed ? "true" : "false") + ",\"binding\":\"Ctrl+Shift+B\",\"snapshot\":" +
                    JsonConvert.ToString(BuffPlannerUiRoot.GetSnapshot()) + "}" + Environment.NewLine);
                _liveHotkeyMarkerWritten = true;
                _log.Info("[KBP-BOOT] runtime requests physical planner hotkey;binding=Ctrl+Shift+B;marker=hotkey-ready.json;controlCapturedFirst=True.");
                _liveUiPhase = 0;
                return false;
            }
            if (_liveUiPhase == 22)
            {
                // Control frame was already captured before the hotkey
                // request (phase 24); open through the production path and
                // mark the fallback so the launcher suppresses its pending
                // physical chord (terminal coordination, review R5).
                _workspaceProgrammaticOpen = true;
                AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory,
                    "programmatic-open.json"),
                    "{\"runId\":\"" + _request.RunId +
                    "\",\"stage\":\"programmatic-open\"}" + Environment.NewLine);
                _log.Info("[KBP-WORKSPACE] hotkey unavailable; opening planner programmatically;controlLuma=" +
                    _workspaceControlLuma + ".");
                UI.BuffPlannerUiRoot.HandlePlannerHotkey();
                _liveUiPhase = 1;
                return false;
            }
            if (_liveUiPhase == 1 && RuntimeTestProtocol.IsWorkspaceScenario(_request.Scenario))
            {
                // Workspace scenario: the casting-first workspace view must
                // be the screen that opened (workspace root present, legacy
                // catalog screen closed — the legacy authoring path is never
                // open at the same time), then capture the same dual-path
                // frame evidence the menu diagnostic uses. A single async
                // engine capture already proved insufficient here
                // (casting-ws-final-171029 wrote a black png while every
                // functional assertion passed), so the primary path is the
                // end-of-frame ReadPixels capture with luma statistics and
                // bounded black-frame retries. IsScreenOpen alone must never
                // gate this: it reflects only the legacy screen, which is
                // what opened in casting-ws-visual-183000 when the dev
                // selection failed to apply.
                if (!BuffPlannerUiRoot.IsCastingWorkspaceOpen) return false;
                if (BuffPlannerUiRoot.IsScreenOpen) return false;
                if (_workspaceOpenSeenMillis < 0)
                {
                    _workspaceOpenSeenMillis = _workspaceCaptureElapsed.IsRunning
                        ? _workspaceCaptureElapsed.ElapsedMilliseconds
                        : 0;
                    if (!_workspaceCaptureElapsed.IsRunning) _workspaceCaptureElapsed.Start();
                    return false;
                }
                // Wall-clock settle so the nested canvas has rendered and
                // batched at least one frame before capture; a two-update
                // settle captured a HUD-only frame in casting-ws-root-200200.
                if (_workspaceCaptureElapsed.ElapsedMilliseconds - _workspaceOpenSeenMillis <
                    WorkspaceOpenSettleMilliseconds) return false;
                _workspaceBlackAttempts = 0;
                _workspaceEngineWaitStartedMillis = -1;
                _workspaceCameraOpenCapture = null;
                BeginWorkspaceCapture("workspace-frame.png");
                BeginWorkspaceCameraCapture("workspace-camera-frame.png", false);
                CaptureScreenshot(Path.Combine(
                    _request.EvidenceDirectory, "workspace-frame-engine.png"));
                _log.Info("[KBP-WORKSPACE] screen open; workspace frame capture requested;environment=" +
                    MenuRenderDiagnostic.EnvironmentSample() + ";attempt=1.");
                _liveUiPhase = 17;
                return false;
            }
            if (_liveUiPhase == 17)
            {
                if (!_workspaceCaptureElapsed.IsRunning) _workspaceCaptureElapsed.Start();
                if (!ConsumeWorkspaceCapture("workspace-frame.png")) return false;
                if (_workspaceFrameCapture.Summary != null &&
                    !_workspaceFrameCapture.Summary.IsNonBlack &&
                    _workspaceBlackAttempts < WorkspaceBlackFrameRecaptureMaxAttempts)
                {
                    // Black presentation is a documented intermittent state
                    // (launchdiag-1 captured a presented menu; menuinput-4/5
                    // captured black from both paths). Retry on wall clock.
                    _workspaceBlackAttempts++;
                    _log.Info("[KBP-WORKSPACE] workspace frame black;recapture scheduled;attempt=" +
                        (_workspaceBlackAttempts + 1) + ";luma=" +
                        _workspaceFrameCapture.Summary.Describe() + ".");
                    System.Threading.Thread.Sleep(WorkspaceBlackFrameRecaptureMilliseconds);
                    BeginWorkspaceCapture("workspace-frame.png");
                    return false;
                }
                _liveRenderScreenshotSha256 = Hashing.Sha256(_workspaceFrameCapture.FullPath);
                _liveUiPhase = 18;
                return false;
            }
            if (_liveUiPhase == 18)
            {
                // The engine capture is written asynchronously by Unity; wait
                // briefly, then record whatever the engine produced.
                if (!_workspaceCaptureElapsed.IsRunning) _workspaceCaptureElapsed.Start();
                if (_workspaceEngineWaitStartedMillis < 0)
                    _workspaceEngineWaitStartedMillis = _workspaceCaptureElapsed.ElapsedMilliseconds;
                string engineHash;
                if (TryHashScreenshot(Path.Combine(
                        _request.EvidenceDirectory, "workspace-frame-engine.png"), out engineHash) ||
                    _workspaceCaptureElapsed.ElapsedMilliseconds - _workspaceEngineWaitStartedMillis >=
                        WorkspaceEngineCaptureWaitMilliseconds)
                {
                    _workspaceEngineScreenshotSha256 = engineHash ?? string.Empty;
                    string lumaEvidence = _workspaceFrameCapture.Summary == null
                        ? "missing" : _workspaceFrameCapture.Summary.Describe();
                    _workspaceLumaEvidence = lumaEvidence;
                    _workspacePresentationEvidence =
                        BuffPlannerUiRoot.CastingWorkspacePresentationEvidence();
                    _workspaceOpenSamples = _workspaceFrameCapture.Samples;
                    // Immutable open-frame record: later phases (bisection
                    // close) reuse _workspaceFrameCapture, so the nonblack
                    // assertion must read the OPEN frame's stored summary,
                    // bound to the open hash (review C3).
                    _workspaceOpenLumaSummary = _workspaceFrameCapture.Summary == null
                        ? "missing" : _workspaceFrameCapture.Summary.Describe();
                    _workspaceOpenNonBlack = _workspaceFrameCapture.Summary != null &&
                        _workspaceFrameCapture.Summary.IsNonBlack;
                    // Root state at CAPTURE time; the bisection below closes
                    // the workspace, so phase 21 must not re-sample it.
                    _workspaceWasOpenAtCapture = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    if (RuntimeTestProtocol.IsManualWorkspaceScenario(
                            _request.Scenario))
                    {
                        // Supervised manual phase (review H1): scripted
                        // authoring is SUSPENDED here; the hold phase waits
                        // on terminal markers or its deadline.
                        _manualHoldStartedMillis = -1;
                        _liveUiPhase = 30;
                        return false;
                    }
                    if (RuntimeTestProtocol.IsInspectionScenario(_request.Scenario))
                    {
                        // Inspection: read-only evidence, then close.
                        _liveUiPhase = 45;
                        return false;
                    }
                    if (RuntimeTestProtocol.IsImportScenario(_request.Scenario))
                    {
                        // First-open import: judge the migration, then close.
                        _liveUiPhase = 90;
                        return false;
                    }
                    if (RuntimeTestProtocol.IsClassicCastScenario(_request.Scenario))
                    {
                        // Classic: back to the Classic planner and its routes.
                        _liveUiPhase = 110;
                        return false;
                    }
                    if (RuntimeTestProtocol.IsPhysicalWorkspaceScenario(_request.Scenario))
                    {
                        // Physical input only: search, wheel, a press
                        // target, focus loss and Escape through the OS.
                        _liveUiPhase = 120;
                        return false;
                    }
                    if (RuntimeTestProtocol.IsQualificationScenario(_request.Scenario))
                    {
                        // Qualification: the production-path driver.
                        _liveUiPhase = 55;
                        return false;
                    }
                    if (RuntimeTestProtocol.IsProbeScenario(_request.Scenario))
                    {
                        // Probe: no scripted authoring; selection (and, only
                        // with the owner's allowance, one native cast).
                        _liveUiPhase = 40;
                        return false;
                    }
                    // The guarded interaction sequence runs BEFORE the
                    // bisection close, against the live session the view
                    // owns (review R1/C4: direct session calls, labeled as
                    // such — never physical-input claims).
                    _workspaceInteractionStep = 0;
                    _liveUiPhase = 25;
                }
                return false;
            }
            if (_liveUiPhase == 45)
            {
                return UpdateInspection();
            }
            if (_liveUiPhase == 90)
            {
                return UpdateImport();
            }
            if (_liveUiPhase == 55)
            {
                return UpdateQualification();
            }
            if (_liveUiPhase == 110)
            {
                return UpdateClassicCast();
            }
            if (_liveUiPhase == 120)
            {
                return UpdatePhysicalWorkspace();
            }
            if (_liveUiPhase == 40)
            {
                return UpdateProbeSelection();
            }
            if (_liveUiPhase == 41)
            {
                return UpdateProbeRun();
            }
            if (_liveUiPhase == 42)
            {
                return CompleteProbe();
            }
            if (_liveUiPhase == 43)
            {
                return UpdateProbeAwaitWorld();
            }
            if (_liveUiPhase == 30)
            {
                // manual-ready preconditions: workspace root open, legacy
                // closed, and — by construction of this scenario — no
                // synthetic input ever requested (no hotkey-ready.json, no
                // physical-input requests, no UMM escape markers).
                if (!BuffPlannerUiRoot.IsCastingWorkspaceOpen) return false;
                if (BuffPlannerUiRoot.IsScreenOpen) return false;
                if (_manualHoldStartedMillis < 0)
                {
                    int holdSeconds = RuntimeTestProtocol.ReadManualHoldSeconds(
                        _request.Parameters);
                    _manualHoldStartedMillis =
                        _workspaceCaptureElapsed.IsRunning
                            ? _workspaceCaptureElapsed.ElapsedMilliseconds
                            : 0;
                    if (!_workspaceCaptureElapsed.IsRunning)
                        _workspaceCaptureElapsed.Start();
                    _manualHoldDeadlineMillis =
                        _manualHoldStartedMillis + holdSeconds * 1000L;
                    string ready = "{\"schemaVersion\":1,\"runId\":" +
                        JsonConvert.ToString(_request.RunId) +
                        ",\"stage\":\"manual-ready\"" +
                        ",\"syntheticInputRequested\":false" +
                        ",\"workspaceOpen\":true" +
                        ",\"legacyScreenClosed\":true" +
                        ",\"holdSeconds\":" + holdSeconds +
                        ",\"deadlineUtc\":\"" +
                        DateTime.UtcNow.AddSeconds(holdSeconds).ToString("o") +
                        "\"}";
                    AtomicFile.WriteUtf8(Path.Combine(
                        _request.EvidenceDirectory, "manual-ready.json"),
                        ready + Environment.NewLine);
                    _manualReadyEvidence = "manual-ready;workspaceOpen=true;" +
                        "legacyScreenClosed=true;syntheticInputRequested=false;" +
                        "holdSeconds=" + holdSeconds;
                    _log.Info("[KBP-MANUAL] manual-ready acknowledged;" +
                        "syntheticInputRequested=false;holdSeconds=" +
                        holdSeconds + ";operator may now interact.");
                    // One-shot transition into the hold (review I2): the
                    // hold phase is the only consumer of terminal markers
                    // and the deadline.
                    _liveUiPhase = 31;
                }
                return false;
            }
            if (_liveUiPhase == 31)
            {
                // Non-blocking hold: rendering/event processing continue
                // (this Update returns each frame); the phase advances only
                // on a terminal marker or the deadline. Deadline is NEVER
                // acceptance (review H1); stop wins over done (review I2).
                long now = _workspaceCaptureElapsed.ElapsedMilliseconds;
                bool deadlineElapsed = now >= _manualHoldDeadlineMillis;
                ManualTerminalRequest request = ManualTerminalPolicy.Classify(
                    deadlineElapsed,
                    !deadlineElapsed && File.Exists(Path.Combine(
                        _request.EvidenceDirectory, "manual-stop.json")),
                    !deadlineElapsed && File.Exists(Path.Combine(
                        _request.EvidenceDirectory, "manual-done.json")));
                if (request == ManualTerminalRequest.None) return false;
                // Final review C5: a rehearsal is labelled as one.
                bool rehearsal = RuntimeTestProtocol.IsManualRehearsal(_request.Parameters);
                if (!rehearsal && request == ManualTerminalRequest.Completed)
                {
                    try
                    {
                        rehearsal = ManualTerminalPolicy.IsRehearsalMarker(File.ReadAllText(
                            Path.Combine(_request.EvidenceDirectory, "manual-done.json")));
                    }
                    catch (Exception) { rehearsal = false; }
                }
                _manualTerminal = new ManualTerminalCoordinator();
                _manualTerminal.Begin(request, now, rehearsal);
                _manualOutcome = _manualTerminal.OutcomeText;
                _log.Info("[KBP-MANUAL] terminal request observed;outcome=" +
                    _manualOutcome + ";final capture requested (bounded " +
                    ManualTerminalCoordinator.DefaultCaptureBudgetMillis + "ms).");
                try
                {
                    BeginWorkspaceCameraCapture(
                        ManualTerminalCoordinator.FinalCaptureFileName, false);
                }
                catch (Exception exception)
                {
                    _manualTerminal.RecordCaptureStartFailure(
                        exception.GetType().Name + ":" + exception.Message);
                    _log.Error("[KBP-MANUAL] final capture could not start.", exception);
                }
                _liveUiPhase = 32;
                return false;
            }
            if (_liveUiPhase == 32)
            {
                // Bounded final capture (review J2): proceed once the
                // callback resolved (success OR failure) or the budget is
                // exhausted — evidence capture can never hold the session
                // open, and its failure/restoration outcome is consumed, not
                // inferred from the file name.
                if (!_manualTerminal.Poll(_workspaceCaptureElapsed.ElapsedMilliseconds,
                        ObserveCapture(_workspaceCameraOpenCapture,
                            ManualTerminalCoordinator.FinalCaptureFileName)))
                    return false;
                // Cleanup runs regardless of the capture outcome; its own
                // failure is recorded beside (never instead of) the primary.
                string closeFailure = null;
                try { BuffPlannerUiRoot.CloseCastingWorkspaceForRuntime(); }
                catch (Exception exception)
                {
                    closeFailure = exception.GetType().Name + ":" + exception.Message;
                    _log.Error("[KBP-MANUAL] production close failed.", exception);
                }
                _manualTerminal.RecordClose(BuffPlannerUiRoot.IsCastingWorkspaceOpen,
                    BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime,
                    closeFailure);
                _log.Info("[KBP-MANUAL] manual phase terminal;outcome=" +
                    _manualOutcome + ";finalCapture=" + _manualTerminal.CaptureEvidence +
                    ";cameraRestoration=" + _manualTerminal.RestorationEvidence +
                    ";close=" + _manualTerminal.CloseEvidence + ".");
                _liveInitialCatalogEvidence = "manual-scenario;outcome=" +
                    _manualOutcome;
                _workspaceInteractionEvidence = "manual;outcome=" + _manualOutcome;
                _workspaceReopenEvidence = "manual;no-reopen-claim";
                _completed = true;
                return true;
            }
            if (_liveUiPhase == 25)
            {
                return UpdateWorkspaceInteraction();
            }
            if (_liveUiPhase == 140 || _liveUiPhase == 145)
            {
                return UpdateHoverOwnership();
            }
            if (_liveUiPhase == 19)
            {
                if (_workspaceClosedWaitUpdates < 4)
                {
                    _workspaceClosedWaitUpdates++;
                    return false;
                }
                BeginWorkspaceCapture("workspace-closed-frame.png");
                _liveUiPhase = 20;
                return false;
            }
            if (_liveUiPhase == 20)
            {
                if (!ConsumeWorkspaceCapture("workspace-closed-frame.png")) return false;
                _workspaceClosedLuma = _workspaceFrameCapture.Summary == null
                    ? "missing" : _workspaceFrameCapture.Summary.Describe();
                _workspaceClosedScreenshotSha256 =
                    Hashing.Sha256(_workspaceFrameCapture.FullPath);
                // Reopen through the production toggle route and verify the
                // saved candidate survived the close/reopen cycle.
                UI.BuffPlannerUiRoot.HandlePlannerHotkey();
                _log.Info("[KBP-WORKSPACE] reopening workspace through the production toggle for save/reopen verification.");
                _liveUiPhase = 26;
                return false;
            }
            if (_liveUiPhase == 26)
            {
                if (!BuffPlannerUiRoot.IsCastingWorkspaceOpen) return false;
                if (!_workspaceCaptureElapsed.IsRunning) _workspaceCaptureElapsed.Start();
                if (_workspaceOpenSeenMillis >= 0 &&
                    _workspaceCaptureElapsed.ElapsedMilliseconds - _workspaceOpenSeenMillis <
                    0) return false;
                _workspaceReopenEvidence = VerifyWorkspaceReopen();
                BeginWorkspaceCameraCapture("ws-interact-reopened.png", false);
                _liveUiPhase = 27;
                return false;
            }
            if (_liveUiPhase == 27)
            {
                if (_workspaceCameraOpenCapture == null ||
                    !string.Equals(_workspaceCameraOpenCapture.FileName,
                        "ws-interact-reopened.png", StringComparison.OrdinalIgnoreCase))
                    return false;
                _log.Info("[KBP-WORKSPACE] interaction sequence complete;" +
                    _workspaceInteractionEvidence + ";reopen=" +
                    _workspaceReopenEvidence + ".");
                // The reload adds a native load only to a run whose authoring,
                // save and reopen already passed (review of 1332ed8..542cd66).
                bool reloadable = _workspaceInteraction.Violations().Count == 0 &&
                    (_workspaceReopenEvidence ?? string.Empty).Contains("preserved=True");
                if (RuntimeTestProtocol.IsReloadScenario(_request.Scenario) && !reloadable)
                    _workspaceReloadEvidence = "passed=False;skipped:interaction-or-reopen-failed";
                if (_hoverStarted && !_hoverReopenChecked)
                {
                    // The pointer-highlight diagnostic's close/reopen reading.
                    _hoverReopenChecked = true;
                    _hoverStep = 30;
                    _liveUiPhase = 145;
                    return false;
                }
                _liveUiPhase = RuntimeTestProtocol.IsReloadScenario(_request.Scenario) && reloadable ? 80 : 21;
                return false;
            }
            if (_liveUiPhase == 80)
            {
                // Mission section 8 (save/reload): close the planner through
                // its owned route, then load the exact WORKING save again in
                // game through the guarded loader (no save write).
                if (BuffPlannerUiRoot.IsCastingWorkspaceOpen)
                {
                    BuffPlannerUiRoot.CloseCastingWorkspaceForRuntime();
                    return false;
                }
                if (BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime) return false;
                _reloadSubscriptionsBefore = BuffPlannerUiRoot.ActiveEventSubscriptionsForRuntime;
                _reloadHudRootsBefore = BuffPlannerUiRoot.HudRootCountForRuntime;
                _reloadRunsBefore = BuffPlannerUiRoot.CastingRunsStartedForRuntime;
                _reloadUnloadsBefore = BuffPlannerUiRoot.LifecycleSignalsForRuntime("OnAreaBeginUnloading");
                _reloadLoadsBefore = BuffPlannerUiRoot.LifecycleSignalsForRuntime("OnAreaLoadingComplete") +
                    BuffPlannerUiRoot.LifecycleSignalsForRuntime("OnAreaActivated");
                _reloadActivatedBefore = BuffPlannerUiRoot.LifecycleSignalsForRuntime("OnAreaActivated");
                _reloadCompleteBefore = BuffPlannerUiRoot.LifecycleSignalsForRuntime("OnAreaLoadingComplete");
                _reloadSessionBefore = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                _reloadStartedMillis = _workspaceCaptureElapsed.ElapsedMilliseconds;
                _reloadSettleFrame = -1;
                _log.Info("[KBP-RELOAD] loading the exact working save again in game;subscriptions=" +
                    _reloadSubscriptionsBefore + ";hudRoots=" + _reloadHudRootsBefore + ".");
                _liveSaveLoader.BeginGuardedReload();
                _liveUiPhase = 81;
                return false;
            }
            if (_liveUiPhase == 81)
            {
                // Settled: the guarded load completed with a stable campaign
                // identity, the area unloaded and loaded again, the world
                // runs and the HUD is back; then 60 rendered frames.
                bool areaCycled =
                    BuffPlannerUiRoot.LifecycleSignalsForRuntime("OnAreaBeginUnloading") > _reloadUnloadsBefore &&
                    BuffPlannerUiRoot.LifecycleSignalsForRuntime("OnAreaLoadingComplete") +
                        BuffPlannerUiRoot.LifecycleSignalsForRuntime("OnAreaActivated") > _reloadLoadsBefore;
                string loaded = _liveSaveLoader.UpdateReload(areaCycled);
                if (loaded == null || !areaCycled || !BuffPlannerUiRoot.WorldRunsForCasting ||
                    !BuffPlannerUiRoot.IsHudInstalledForRuntime)
                {
                    if (_workspaceCaptureElapsed.ElapsedMilliseconds - _reloadStartedMillis >
                        LiveCampaignSaveLoader.ReloadBudgetSeconds * 1000L)
                    {
                        string settle = "loaded=" + (loaded ?? "pending") + ";areaCycled=" + areaCycled +
                            ";world=" + BuffPlannerUiRoot.WorldStateForRuntime + ";hud=" +
                            BuffPlannerUiRoot.IsHudInstalledForRuntime;
                        _liveSaveLoader.FailReload("host-timeout:" + settle);
                        throw new TimeoutException("The in-game reload did not settle;" + settle + ".");
                    }
                    return false;
                }
                if (_reloadSettleFrame < 0)
                {
                    _reloadSettleFrame = Time.frameCount;
                    return false;
                }
                if (Time.frameCount - _reloadSettleFrame < 60) return false;
                _reloadLoadedEvidence = loaded;
                UI.BuffPlannerUiRoot.HandlePlannerHotkey();
                _log.Info("[KBP-RELOAD] reload settled;" + loaded + ";reopening through the production toggle.");
                _liveUiPhase = 82;
                return false;
            }
            if (_liveUiPhase == 82)
            {
                if (!BuffPlannerUiRoot.IsCastingWorkspaceOpen)
                {
                    if (_workspaceCaptureElapsed.ElapsedMilliseconds - _reloadStartedMillis >
                        LiveCampaignSaveLoader.ReloadBudgetSeconds * 1000L)
                        throw new TimeoutException("The planner did not reopen after the in-game reload.");
                    return false;
                }
                _workspaceReloadEvidence = VerifyWorkspaceReload();
                _log.Info("[KBP-RELOAD] reopened after the reload;" + _workspaceReloadEvidence + ".");
                BeginWorkspaceCameraCapture("ws-reload-reopened.png", false);
                _liveUiPhase = 83;
                return false;
            }
            if (_liveUiPhase == 83)
            {
                if (_workspaceCameraOpenCapture == null ||
                    !string.Equals(_workspaceCameraOpenCapture.FileName,
                        "ws-reload-reopened.png", StringComparison.OrdinalIgnoreCase))
                    return false;
                _liveUiPhase = 21;
                return false;
            }
            if (_liveUiPhase == 21)
            {
                _workspaceChangedFraction = _workspaceControlSamples == null ||
                    _workspaceOpenSamples == null ||
                    _workspaceControlSamples.Length != _workspaceOpenSamples.Length
                        ? -1f
                        : MenuFrameStats.ComputeChangedFraction(
                            _workspaceControlSamples, _workspaceOpenSamples);
                _workspaceCameraOpenLuma = DescribeCameraCapture(
                    _workspaceCameraOpenCapture, "workspace-camera-frame.png");
                _workspaceCameraControlLuma = DescribeCameraCapture(
                    _workspaceCameraControlCapture, "workspace-camera-control.png");
                WriteWorkspaceRenderMarker(_workspaceLumaEvidence);
                _liveInitialCatalogEvidence = "workspace-scenario:" +
                    _request.Scenario +
                    ";workspaceRoot=" + (_workspaceWasOpenAtCapture ? "active" : "missing") +
                    ";legacyScreen=" + (BuffPlannerUiRoot.IsScreenOpen ? "open" : "closed") +
                    ";luma=" + _workspaceLumaEvidence + ";blackRecaptures=" + _workspaceBlackAttempts +
                    ";controlLuma=" + (_workspaceControlLuma ?? "missing") +
                    ";changedFraction=" + _workspaceChangedFraction.ToString(
                        "F5", System.Globalization.CultureInfo.InvariantCulture) +
                    ";closedLuma=" + (_workspaceClosedLuma ?? "missing") +
                    ";" + _workspacePresentationEvidence;
                _completed = true;
                _log.Info("[KBP-WORKSPACE] workspace capture sequence complete;openLuma=" +
                    _workspaceLumaEvidence + ";controlLuma=" + (_workspaceControlLuma ?? "missing") +
                    ";closedLuma=" + (_workspaceClosedLuma ?? "missing") + ";changedFraction=" +
                    _workspaceChangedFraction.ToString("F5",
                        System.Globalization.CultureInfo.InvariantCulture) +
                    ";presentation=" + _workspacePresentationEvidence + ".");
                return true;
            }
            if (_liveUiPhase == 1)
            {
                if (!BuffPlannerUiRoot.IsScreenOpen) return false;
                CatalogLayoutDiagnostics catalog = BuffPlannerUiRoot.CatalogDiagnosticsForRuntime();
                if (catalog == null || catalog.Filters == null ||
                    catalog.Filters.VisibleViewModels < 1 || catalog.InstantiatedRows < 1 ||
                    catalog.ActiveRows < 1 || catalog.VisibleRows < 1 ||
                    !catalog.SelectedDetailsBound || !catalog.BlessEvidence.Contains("rowVisible=True"))
                    throw new InvalidOperationException("Live catalog is not visibly bound: " +
                        (catalog == null ? "missing" : catalog.ToString()));
                _liveInitialCatalogEvidence = catalog.ToString();
                if (!BuffPlannerUiRoot.SelectFirstRowForRuntime())
                    throw new InvalidOperationException("First live row could not be selected.");
                _liveUiPhase = 13;
                return false;
            }
            if (_liveUiPhase == 13)
            {
                _liveRenderWaitFrames++;
                if (_liveRenderWaitFrames < 2) return false;
                _liveRenderDiagnostics = BuffPlannerUiRoot.LiveRowRenderDiagnosticsForRuntime();
                if (_liveRenderDiagnostics == null)
                    throw new InvalidOperationException("Live row render diagnostics are absent.");
                if (_liveRenderDiagnostics.ExpectedNames == null ||
                    _liveRenderDiagnostics.ExpectedNames.Length != 5 ||
                    _liveRenderDiagnostics.RowScreenRectangles == null ||
                    _liveRenderDiagnostics.RowScreenRectangles.Length != 5 ||
                    _liveRenderDiagnostics.BoundRowCount < 5 ||
                    string.IsNullOrWhiteSpace(_liveRenderDiagnostics.SelectedRowName) ||
                    _liveRenderDiagnostics.SelectedRowName != _liveRenderDiagnostics.DetailsTitleText ||
                    _liveRenderDiagnostics.CanaryEvidence != "absent" ||
                    _liveRenderDiagnostics.AbilityIconCount +
                        _liveRenderDiagnostics.MissingIconCount !=
                        _liveRenderDiagnostics.BoundRowCount ||
                    _liveRenderDiagnostics.CastingModeControlCount != 1 ||
                    _liveRenderDiagnostics.RetiredPrimaryLabelCount != 0 ||
                    string.IsNullOrWhiteSpace(_liveRenderDiagnostics.ThemeResolution) ||
                    string.IsNullOrWhiteSpace(_liveRenderDiagnostics.TextRenderingEvidence) ||
                    _liveRenderDiagnostics.NestedCanvasScalerCount != 0 ||
                    _liveRenderDiagnostics.FractionalRectCount != 0)
                    throw new InvalidOperationException("Live production render evidence is incomplete: " +
                        Serialize(_liveRenderDiagnostics));
                AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory,
                    "live-row-render-diagnostics.json"), Serialize(_liveRenderDiagnostics));
                _liveRenderScreenshotPath = Path.Combine(_request.EvidenceDirectory,
                    "planner-render.png");
                CaptureScreenshot(_liveRenderScreenshotPath);
                _liveUiPhase = 14;
                return false;
            }
            if (_liveUiPhase == 14)
            {
                if (!TryHashScreenshot(_liveRenderScreenshotPath,
                    out _liveRenderScreenshotSha256)) return false;
                _log.Info("[KBP-RENDER] screenshot=" + _liveRenderScreenshotPath +
                    ";sha256=" + _liveRenderScreenshotSha256 + ".");
                if (!BuffPlannerUiRoot.PrepareVisualEvidenceForRuntime("selected-details"))
                    throw new InvalidOperationException("Selected-details evidence view is unavailable.");
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory,
                    "planner-selected-details.png"));
                _liveUiPhase = 15;
                return false;
            }
            if (_liveUiPhase == 15)
            {
                if (!TryHashScreenshot(Path.Combine(_request.EvidenceDirectory,
                    "planner-selected-details.png"), out _liveSelectedDetailsScreenshotSha256))
                    return false;
                if (!BuffPlannerUiRoot.PrepareVisualEvidenceForRuntime("grid-overview"))
                    throw new InvalidOperationException("Grid-overview evidence view is unavailable.");
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory,
                    "planner-grid-overview.png"));
                _liveUiPhase = 16;
                return false;
            }
            if (_liveUiPhase == 16)
            {
                if (!TryHashScreenshot(Path.Combine(_request.EvidenceDirectory,
                    "planner-grid-overview.png"), out _liveGridOverviewScreenshotSha256))
                    return false;
                BuffPlannerUiRoot.BeginRuntimeSmoke();
                BuffPlannerUiRoot.DispatchRuntimeInputSmoke();
                BuffPlannerUiRoot.CloseRuntimeSmoke();
                WritePhysicalInputRequest("settle-center", "hover",
                    BuffPlannerUiRoot.ScreenCenterForRuntime());
                _liveUiPhase = 12;
                return false;
            }
            if (_liveUiPhase == 12)
            {
                if (!PhysicalInputAcknowledged("settle-center")) return false;
                _liveCameraSettleFrames++;
                if (_liveCameraSettleElapsed == null)
                    _liveCameraSettleElapsed = System.Diagnostics.Stopwatch.StartNew();
                if (_liveCameraSettleElapsed.Elapsed.TotalSeconds < 2) return false;
                BuffPlannerUiRoot.BeginPhysicalInputProbe();
                QuickFlowDiagnostics baselineFlow =
                    BuffPlannerUiRoot.QuickFlowForRuntime("long");
                _liveHoverEnterBaseline = baselineFlow == null ? 0 :
                    baselineFlow.PointerEnters;
                WritePhysicalInputRequest("hover-long", "hover",
                    BuffPlannerUiRoot.HudButtonCenterForRuntime("long"));
                _liveUiPhase = 2;
                return false;
            }
            if (_liveUiPhase == 2)
            {
                if (!PhysicalInputAcknowledged("hover-long")) return false;
                HudTooltipRuntimeDiagnostics tooltip =
                    BuffPlannerUiRoot.TooltipDiagnosticsForRuntime();
                QuickFlowDiagnostics flow = BuffPlannerUiRoot.QuickFlowForRuntime("long");
                if (tooltip == null || !tooltip.Active)
                {
                    _liveTooltipStableFrames = 0;
                    _liveTooltipStableStartedAtUtc = default(DateTime);
                    if ((_uiSmokeUpdates % 120) == 0)
                        _log.Info("[KBP-INPUT] physical hover waiting;" +
                            BuffPlannerUiRoot.PhysicalHoverSnapshotForRuntime("long") + ".");
                    return false;
                }
                if (!tooltip.InsideScreen || tooltip.ListenerCount != 4 ||
                    tooltip.RaycastGraphicCount != 0 || tooltip.BlocksRaycasts ||
                    flow == null || flow.PointerEnters != _liveHoverEnterBaseline + 1)
                    throw new InvalidOperationException("Tooltip ownership is invalid: active=" +
                        tooltip.Active + ";inside=" + tooltip.InsideScreen + ";listeners=" +
                        tooltip.ListenerCount + ";raycastGraphics=" + tooltip.RaycastGraphicCount +
                        ";blocks=" + tooltip.BlocksRaycasts + ";enters=" +
                        (flow == null ? -1 : flow.PointerEnters) + ";baseline=" +
                        _liveHoverEnterBaseline + ";bounds=" + tooltip.Bounds);
                _liveTooltipStableFrames++;
                if (_liveTooltipStableStartedAtUtc == default(DateTime))
                    _liveTooltipStableStartedAtUtc = DateTime.UtcNow;
                TimeSpan stableElapsed = DateTime.UtcNow - _liveTooltipStableStartedAtUtc;
                if (stableElapsed < TimeSpan.FromSeconds(5)) return false;
                _liveTooltipStable = true;
                _liveTooltipEvidence = "elapsedMs=" +
                    ((long)stableElapsed.TotalMilliseconds) + ";frames=" +
                    _liveTooltipStableFrames + ";bounds=" +
                    tooltip.Bounds + ";listeners=" + tooltip.ListenerCount +
                    ";raycastGraphics=" + tooltip.RaycastGraphicCount + ";blocks=" +
                    tooltip.BlocksRaycasts + ";pointerEnterDelta=" +
                    (flow.PointerEnters - _liveHoverEnterBaseline);
                WritePhysicalInputRequest("click-long-empty", "click",
                    BuffPlannerUiRoot.HudButtonCenterForRuntime("long"));
                _liveUiPhase = 3;
                return false;
            }
            if (_liveUiPhase == 3)
            {
                if (!PhysicalInputAcknowledged("click-long-empty")) return false;
                QuickExecutionResult result = BuffPlannerUiRoot.QuickResultForRuntime("long");
                if (result == null || result.Message != "No Long buffs are configured.") return false;
                WritePhysicalInputRequest("click-important-empty", "click",
                    BuffPlannerUiRoot.HudButtonCenterForRuntime("important"));
                _liveUiPhase = 4;
                return false;
            }
            if (_liveUiPhase == 4)
            {
                if (!PhysicalInputAcknowledged("click-important-empty")) return false;
                QuickExecutionResult result = BuffPlannerUiRoot.QuickResultForRuntime("important");
                if (result == null || result.Message != "No Important buffs are configured.") return false;
                WritePhysicalInputRequest("click-short-empty", "click",
                    BuffPlannerUiRoot.HudButtonCenterForRuntime("short"));
                _liveUiPhase = 5;
                return false;
            }
            if (_liveUiPhase == 5)
            {
                if (!PhysicalInputAcknowledged("click-short-empty")) return false;
                QuickExecutionResult result = BuffPlannerUiRoot.QuickResultForRuntime("short");
                if (result == null || result.Message != "No Short buffs are configured.") return false;
                WritePhysicalInputRequest("click-setup", "click",
                    BuffPlannerUiRoot.HudButtonCenterForRuntime("setup"));
                _liveUiPhase = 6;
                return false;
            }
            if (_liveUiPhase == 6)
            {
                if (!PhysicalInputAcknowledged("click-setup") ||
                    !BuffPlannerUiRoot.IsScreenOpen) return false;
                WritePhysicalInputRequest("click-modal", "click",
                    BuffPlannerUiRoot.ModalBackgroundPointForRuntime());
                _liveUiPhase = 7;
                return false;
            }
            if (_liveUiPhase == 7)
            {
                if (!PhysicalInputAcknowledged("click-modal")) return false;
                UiInputIsolationProbeResult physical = BuffPlannerUiRoot.EndPhysicalInputProbe();
                if (physical == null || physical.PlayerCommandCount != 0 ||
                    physical.MovementCommandCount != 0 || physical.AbilityCommandCount != 0 ||
                    physical.SelectionEventCount != 0 || physical.AbilityTargetEventCount != 0 ||
                    !physical.SelectionUnchanged || !physical.CameraUnchanged)
                    throw new InvalidOperationException("Physical planner clicks reached world input;" +
                        "player=" + physical.PlayerCommandCount + ";movement=" +
                        physical.MovementCommandCount + ";ability=" + physical.AbilityCommandCount +
                        ";selectionEvents=" + physical.SelectionEventCount +
                        ";abilityTargetEvents=" + physical.AbilityTargetEventCount +
                        ";selectionUnchanged=" + physical.SelectionUnchanged +
                        ";cameraUnchanged=" + physical.CameraUnchanged + ".");
                _liveBlessSelectedAndConfigured =
                    BuffPlannerUiRoot.SelectAndConfigureBlessForRuntime(
                        (string)_request.Parameters["executionMode"]);
                if (!_liveBlessSelectedAndConfigured)
                    throw new InvalidOperationException("Visible spellbook Bless row could not be selected/configured.");
                CatalogLayoutDiagnostics targetState = BuffPlannerUiRoot.CatalogDiagnosticsForRuntime();
                if (targetState == null) throw new InvalidOperationException(
                    "Target-state diagnostics are unavailable after Bless configuration.");
                _liveDirectSelectedTargetCount = targetState.DirectSelectedTargetCount;
                _liveIndirectCoveredTargetCount = targetState.IndirectCoveredTargetCount;
                _liveCatalogControlEvidence = BuffPlannerUiRoot.DispatchCatalogControlsForRuntime();
                if (string.IsNullOrWhiteSpace(_liveCatalogControlEvidence))
                    throw new InvalidOperationException("Catalog controls could not be physically dispatched.");
                if (!BuffPlannerUiRoot.PrepareVisualEvidenceForRuntime("target-colors"))
                    throw new InvalidOperationException("Target-color evidence view is unavailable.");
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory,
                    "planner-target-colors.png"));
                _liveUiPhase = 70;
                return false;
            }
            if (_liveUiPhase == 70)
            {
                if (!TryHashScreenshot(Path.Combine(_request.EvidenceDirectory,
                    "planner-target-colors.png"), out _liveTargetColorsScreenshotSha256))
                    return false;
                if (!BuffPlannerUiRoot.PrepareVisualEvidenceForRuntime("settings"))
                    throw new InvalidOperationException("Settings evidence view is unavailable.");
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory,
                    "planner-settings.png"));
                _liveUiPhase = 71;
                return false;
            }
            if (_liveUiPhase == 71)
            {
                if (!TryHashScreenshot(Path.Combine(_request.EvidenceDirectory,
                    "planner-settings.png"), out _liveSettingsScreenshotSha256))
                    return false;
                BuffPlannerUiRoot.CloseRuntimeSmoke();
                if (BuffPlannerUiRoot.IsScreenOpen)
                    throw new InvalidOperationException("Planner did not close cleanly after configuration.");
                _liveUiPhase = 72;
                return false;
            }
            if (_liveUiPhase == 72)
            {
                _liveHudScreenshotWaitFrames++;
                if (_liveHudScreenshotWaitFrames < 2) return false;
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, "hud-integration.png"));
                _liveUiPhase = 73;
                return false;
            }
            if (_liveUiPhase == 73)
            {
                if (!TryHashScreenshot(Path.Combine(_request.EvidenceDirectory,
                    "hud-integration.png"), out _liveHudScreenshotSha256)) return false;
                WritePhysicalInputRequest("click-long-configured", "click",
                    BuffPlannerUiRoot.HudButtonCenterForRuntime("long"));
                _liveUiPhase = 9;
                return false;
            }
            if (_liveUiPhase == 9)
            {
                if (!PhysicalInputAcknowledged("click-long-configured") ||
                    BuffPlannerUiRoot.IsExecutingForRuntime) return false;
                QuickFlowDiagnostics flow = BuffPlannerUiRoot.QuickFlowForRuntime("long");
                QuickExecutionResult result = BuffPlannerUiRoot.QuickResultForRuntime("long");
                if (flow == null || flow.ResultsPresented < 2 || result == null) return false;
                if (result.Disposition == QuickExecutionDisposition.Completed &&
                    result.Confirmed < 1)
                    throw new InvalidOperationException("Configured Long reported completion without confirmation.");
                _liveUiPhase = 10;
                return false;
            }
            if (_liveUiPhase == 10)
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
                    _liveUiPhase = 11;
                    return true;
                }
                BuffPlannerUiRoot.CloseRuntimeSmoke();
                return false;
            }
            return _liveUiPhase == 11;
        }

        private void WritePhysicalInputRequest(string id, string kind, Vector2 position)
        {
            WritePhysicalInputRequest(id, kind, position, null);
        }

        // extraJson: further members (",\"text\":\"...\"", ",\"delta\":-360"), or null.
        private void WritePhysicalInputRequest(string id, string kind, Vector2 position, string extraJson)
        {
            string path = Path.Combine(_request.EvidenceDirectory,
                "physical-input-" + id + ".json");
            // Kingmaker changes JsonConvert.DefaultSettings to preserve object
            // references; anonymous marker serialization would collapse to {$id:1}.
            string json = "{\"schemaVersion\":1,\"runId\":" +
                JsonConvert.ToString(_request.RunId) + ",\"actionId\":" +
                JsonConvert.ToString(id) + ",\"action\":" + JsonConvert.ToString(kind) +
                ",\"x\":" + position.x.ToString("R", CultureInfo.InvariantCulture) +
                ",\"y\":" + position.y.ToString("R", CultureInfo.InvariantCulture) +
                ",\"unityScreenWidth\":" + Screen.width +
                ",\"unityScreenHeight\":" + Screen.height + (extraJson ?? string.Empty) + "}";
            AtomicFile.WriteUtf8(path, json + Environment.NewLine);
            _log.Info("[KBP-INPUT] physical action requested;id=" + id + ";action=" +
                kind + ";x=" + position.x.ToString("F1") + ";y=" +
                position.y.ToString("F1") + ".");
        }

        private static void CaptureScreenshot(string path)
        {
            Type screenCapture = Type.GetType(
                "UnityEngine.ScreenCapture, UnityEngine.ScreenCaptureModule", false);
            MethodInfo capture = screenCapture == null ? null : screenCapture.GetMethod(
                "CaptureScreenshot", BindingFlags.Static | BindingFlags.Public, null,
                new[] { typeof(string) }, null);
            if (capture == null)
                throw new MissingMethodException("Unity screenshot capture API is unavailable.");
            capture.Invoke(null, new object[] { path });
        }

        private void BeginWorkspaceCapture(string fileName)
        {
            MenuDiagnosticCaptureHost.CaptureMenuFrame(
                Path.Combine(_request.EvidenceDirectory, fileName),
                delegate(MenuFrameCapture capture, Exception failure)
                {
                    capture.Failure = failure;
                    _workspaceLastCapture = capture;
                });
        }

        private void BeginWorkspaceCameraCapture(string fileName, bool control)
        {
            // Display-independent diagnostic lane: camera-render capture
            // that bypasses the presented backbuffer. Recorded honestly
            // alongside the primary paths; not itself an acceptance gate
            // until proven.
            MenuDiagnosticCaptureHost.CaptureMenuFrameThroughCameras(
                Path.Combine(_request.EvidenceDirectory, fileName),
                delegate(MenuFrameCapture capture, Exception failure)
                {
                    capture.Failure = failure;
                    if (control) _workspaceCameraControlCapture = capture;
                    else _workspaceCameraOpenCapture = capture;
                }, _log);
        }

        private bool ConsumeWorkspaceCapture(string fileName)
        {
            if (_workspaceLastCapture == null ||
                !string.Equals(_workspaceLastCapture.FileName, fileName, StringComparison.OrdinalIgnoreCase) ||
                _workspaceLastCapture.Handled) return false;
            _workspaceLastCapture.Handled = true;
            _workspaceFrameCapture = _workspaceLastCapture;
            if (_workspaceFrameCapture.Failure != null)
                throw new InvalidOperationException("Workspace frame capture failed for " + fileName +
                    ": " + _workspaceFrameCapture.Failure.GetType().Name + ": " +
                    _workspaceFrameCapture.Failure.Message);
            return true;
        }

        // Unity-free snapshot of a camera-path capture for the manual
        // terminal contract; null until the named capture has completed.
        private static ManualCaptureObservation ObserveCapture(
            MenuFrameCapture capture, string fileName)
        {
            if (capture == null || !string.Equals(capture.FileName, fileName,
                    StringComparison.OrdinalIgnoreCase))
                return null;
            bool exists = !string.IsNullOrEmpty(capture.FullPath) &&
                File.Exists(capture.FullPath);
            return new ManualCaptureObservation
            {
                FileName = capture.FileName,
                FailureType = capture.Failure == null
                    ? null : capture.Failure.GetType().Name,
                FailureMessage = capture.Failure == null
                    ? null : capture.Failure.Message,
                RestorationClean = capture.RestorationClean,
                RestorationVerdict = capture.RestorationVerdict,
                FileExists = exists,
                Sha256 = exists && capture.Failure == null
                    ? Hashing.Sha256(capture.FullPath) : null,
                LumaSummary = capture.Summary == null
                    ? null : capture.Summary.Describe()
            };
        }

        private static string DescribeCameraCapture(
            MenuFrameCapture capture, string fileName)        {
            if (capture == null || !string.Equals(capture.FileName, fileName,
                    StringComparison.OrdinalIgnoreCase))
                return "missing";
            if (capture.Failure != null)
                return "failed:" + capture.Failure.GetType().Name;
            string luma = capture.Summary == null
                ? "missing" : capture.Summary.Describe();
            return luma + ";sha256=" + (File.Exists(capture.FullPath)
                ? Hashing.Sha256(capture.FullPath) : "missing");
        }

        // Guarded interaction sequence (reviews R1/C4, G4): EVERY claimed
        // control-covered action goes through its real ACTIVE, INTERACTABLE
        // button — state, enhancement, edit, retarget, undo, done, and save
        // included. After each Add the scenario asserts one-record growth,
        // a distinct new identity, exact authored fields, and unchanged
        // siblings; a deliberately refused Add is exercised and must leave
        // the document untouched. Direct session calls appear only where
        // labeled as such (selection scope for driving), never as
        // substitutes for the controls under test.
        // ------------------------------------------------------------------
        // Single-cast probe phases (40-42). Selection is discovery-driven
        // and recorded in full before anything else; the boundary exists
        // only in the casting scenario and only with a valid allowance. The
        // SingleCastProbeRunOwner owns the boundary, the fresh observations
        // and the ONE terminal cleanup path (reviews M2/M3); this host only
        // routes frames, stop/deadline, host failures and disable/unload to it.
        // ------------------------------------------------------------------
        // Inspection scenario: read-only evidence of the loaded copy, then a
        // production close. No authoring, no save, no submission (the
        // runtime-test session lock keeps the casting boundary refusing).
        private bool UpdateInspection()
        {
            CastingWorkspaceInputs inputs = null;
            try { inputs = BuffPlannerUiRoot.CastingWorkspaceInputsForRuntime(); }
            catch (Exception exception)
            {
                _inspectionFailure = "inputs-unavailable:" + exception.GetType().Name + ":" +
                    exception.Message;
            }
            if (inputs != null)
            {
                try
                {
                    JObject inspection = AdvancedCopyInspection.Collect(inputs);
                    inspection["runId"] = _request.RunId;
                    // Mission batch 3, section 7: the party's real
                    // capabilities as the planner's own discovery sees them
                    // (spellbooks, pools and tokens, variants, targeting
                    // shapes, pets, enhancements), read-only, so the
                    // qualification cases are chosen from them.
                    inspection["capabilities"] = new JArray(CastingCapabilityInventory
                        .Describe(inputs).Cast<object>().ToArray());
                    object working;
                    inspection["workingSaveName"] = _request.Parameters.TryGetValue(
                        "workingSaveName", out working) ? working as string : null;
                    _inspectionStartedRuns = BuffPlannerUiRoot.CastingRunsStartedForRuntime;
                    _inspectionDisposition = BuffPlannerUiRoot.CastingDispatchDispositionForRuntime;
                    inspection["nativeCasting"] = new JObject
                    {
                        { "sessionLocked", UI.NativeCastingSessionPolicy.Locked },
                        { "lockReason", UI.NativeCastingSessionPolicy.LockReason },
                        { "dispatchDisposition", _inspectionDisposition },
                        { "startedRuns", _inspectionStartedRuns }
                    };
                    AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory,
                        "advanced-inspection.json"), inspection.ToString(Formatting.Indented) +
                        Environment.NewLine);
                    _inspectionWritten = true;
                    _inspectionSummary = "units=" + inputs.Snapshot.Units.Count +
                        ";providers=" + inputs.Snapshot.Providers.Count +
                        ";pools=" + inputs.Snapshot.ResourcePools.Count +
                        ";sources=" + inputs.EffectsBySource.Count +
                        ";enhancements=" + inputs.Enhancements.Count;
                    _log.Info("[KBP-INSPECT] written;" + _inspectionSummary + ".");
                }
                catch (Exception exception)
                {
                    _inspectionFailure = "collect-failed:" + exception.GetType().Name + ":" +
                        exception.Message;
                    _log.Error("[KBP-INSPECT] collection failed.", exception);
                }
            }
            ProbeWorkspaceCloseResult closed = CloseProbeWorkspace();
            _inspectionWorkspaceClosed = closed.Closed && closed.InputLeaseReleased &&
                string.IsNullOrEmpty(closed.Failure);
            _liveInitialCatalogEvidence = "inspection-scenario;workspaceRoot=active;legacyScreen=closed";
            _workspaceInteractionEvidence = "inspection;no-authoring";
            _workspaceReopenEvidence = "inspection;no-reopen-claim";
            _completed = true;
            return true;
        }

        // Mission section 11 (first-open import in game): before the first
        // open, a classic plan for the loaded campaign is written through the
        // classic repository, built from live discovery: the zero-cost
        // recipe's buff as one Automatic assignment on two party members.
        private void SeedClassicPlanForImport()
        {
            try
            {
                string campaignId = Kingmaker.Game.Instance == null || Kingmaker.Game.Instance.Player == null
                    ? null : Kingmaker.Game.Instance.Player.GameId;
                CastingWorkspaceInputs inputs = BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime();
                if (string.IsNullOrEmpty(campaignId) || inputs == null)
                {
                    _importFailure = "seed:no-campaign-or-discovery";
                    return;
                }
                CastingQualificationSelection selection =
                    CastingQualificationRecipe.SelectZeroCostMixed(inputs, campaignId);
                List<string> targets = selection.Castings
                    .Select(value => value.DirectTargetUnitId)
                    .Where(value => !string.IsNullOrEmpty(value))
                    .Distinct(StringComparer.Ordinal).Take(2).ToList();
                if (!selection.Selected || targets.Count != 2)
                {
                    _importFailure = "seed:no-zero-cost-buff:" + selection.Refusal;
                    return;
                }
                var repository = new KingmakerBuffPlanner.Persistence.ProfileRepository(_modEntry.Path);
                string path = repository.GetProfilePath(campaignId);
                if (File.Exists(path))
                {
                    _importFailure = "seed:classic-plan-already-present";
                    return;
                }
                KingmakerBuffPlanner.Persistence.BuffPlannerProfile profile =
                    KingmakerBuffPlanner.Persistence.BuffPlannerProfile.CreateDefault(campaignId);
                profile.Routines[0].Assignments.Add(new KingmakerBuffPlanner.Persistence.SourceAssignmentProfile
                {
                    SourceId = selection.SourceId,
                    Ability = KingmakerBuffPlanner.Persistence.AbilityKeyProfile.FromKey(selection.Ability),
                    ExistingEffectPolicy = ExistingEffectPolicy.SkipAlreadyActive,
                    IgnoredPresenceMarkers = new List<string>(),
                    CastingAssignments = new List<KingmakerBuffPlanner.Persistence.CastingAssignmentProfile>
                    {
                        new KingmakerBuffPlanner.Persistence.CastingAssignmentProfile
                        {
                            AssignmentId = "auto-" + selection.SourceId,
                            Order = 0,
                            CasterUnitId = null,
                            SpellbookGuid = null,
                            ProviderKey = null,
                            TargetUnitIds = targets,
                            Enhancements = new List<KingmakerBuffPlanner.Persistence.EnhancementSelectionProfile>()
                        }
                    }
                });
                repository.Save(profile);
                // Final review B1: the seed is a genuine schema-4 document, as
                // the released 0.0.19 wrote it; the first open must migrate it
                // in memory and import it without rewriting it.
                AtomicFile.WriteUtf8(path, LegacyProfileFixture.ToSchema4Json(File.ReadAllText(path)));
                _importClassicPath = path;
                _importClassicSha256 = Hashing.Sha256(path);
                _importExpectedCastings = targets.Count;
                _importSeedEvidence = "campaign=" + campaignId + ";schema=4;source=" + selection.SourceId +
                    ";targets=" + string.Join(",", targets.ToArray()) + ";classicSha256=" + _importClassicSha256;
                _log.Info("[KBP-IMPORT] classic plan seeded before the first open;" + _importSeedEvidence + ".");
            }
            catch (Exception exception)
            {
                _importFailure = "seed-failed:" + exception.GetType().Name + ":" + exception.Message;
                _log.Error("[KBP-IMPORT] seeding failed.", exception);
            }
        }

        // The first open ran the production migration: judge it, capture the
        // opened planner, close it through its owned route and finish.
        private bool UpdateImport()
        {
            if (_importVerification == null)
            {
                _importVerification = VerifyWorkspaceImport();
                _log.Info("[KBP-IMPORT] first open;" + _importVerification + ".");
                BeginWorkspaceCameraCapture("ws-import-opened.png", false);
                return false;
            }
            if (_workspaceCameraOpenCapture == null ||
                !string.Equals(_workspaceCameraOpenCapture.FileName, "ws-import-opened.png",
                    StringComparison.OrdinalIgnoreCase))
                return false;
            ProbeWorkspaceCloseResult closed = CloseProbeWorkspace();
            _importWorkspaceClosed = closed.Closed && closed.InputLeaseReleased &&
                string.IsNullOrEmpty(closed.Failure);
            _liveInitialCatalogEvidence = "import-scenario;workspaceRoot=active;legacyScreen=closed";
            _workspaceInteractionEvidence = "import;no-authoring";
            _workspaceReopenEvidence = "import;no-reopen-claim";
            _completed = true;
            return true;
        }

        // Imported (not blocked, not a fresh empty plan), one casting per
        // classic target, nothing Ready on its own, the classic file
        // byte-unchanged and archived byte-exact beside it.
        private string VerifyWorkspaceImport()
        {
            UI.CastingWorkspaceSession session = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
            if (session == null) return "passed=False;session-missing;seed=" + (_importSeedEvidence ?? "missing");
            string settings = Path.Combine(_modEntry.Path, "UserSettings");
            bool classicUnchanged = _importClassicPath != null && File.Exists(_importClassicPath) &&
                string.Equals(Hashing.Sha256(_importClassicPath), _importClassicSha256, StringComparison.Ordinal);
            string[] archives = Directory.Exists(settings)
                ? Directory.GetFiles(settings, "kbp-casting-*.orig") : new string[0];
            bool archived = _importClassicSha256 != null && archives.Any(path => string.Equals(
                Hashing.Sha256(path), _importClassicSha256, StringComparison.Ordinal));
            KingmakerBuffPlanner.Persistence.CastingImportReport report = session.ImportReport;
            bool migrated = session.MigrationStatus == KingmakerBuffPlanner.Persistence.CastingMigrationStatus.Migrated &&
                !session.LegacyImportBlocked;
            int castings = session.Document == null ? -1 : session.Document.Castings.Count;
            bool imported = report != null && _importExpectedCastings > 0 &&
                report.ResultingCastingCount == _importExpectedCastings && castings == _importExpectedCastings;
            bool reviewed = report != null && report.ReadyCount == 0 && session.Document != null &&
                session.Document.Castings.All(value => value != null && value.State != CastingAuthoringState.Ready);
            bool passed = _importFailure == null && migrated && classicUnchanged && archived && imported && reviewed;
            return "passed=" + passed + ";migrated=" + migrated + ";status=" + session.MigrationStatus +
                ";classicUnchanged=" + classicUnchanged + ";archived=" + archived + ";castings=" + castings +
                ";resulting=" + (report == null ? -1 : report.ResultingCastingCount) +
                ";ready=" + (report == null ? -1 : report.ReadyCount) +
                ";drafts=" + (report == null ? -1 : report.DraftCount) +
                ";unresolvedCasters=" + (report == null ? -1 : report.UnresolvedCasterCount) +
                ";seed=" + (_importSeedEvidence ?? "missing") +
                (_importFailure == null ? string.Empty : ";failure=" + _importFailure);
        }

        private string _importSeedEvidence;
        private string _importClassicPath;
        private string _importClassicSha256;
        private int _importExpectedCastings;
        private string _importVerification;
        private bool _importWorkspaceClosed;
        private string _importFailure;

        private bool _inspectionWritten;
        private bool _inspectionWorkspaceClosed;
        private int _inspectionStartedRuns = -1;
        private string _inspectionDisposition = string.Empty;
        private string _inspectionSummary = string.Empty;
        private string _inspectionFailure;

        // Physical-input workspace scenario (mission batch 3, section 10).
        // Every action is a request the launcher delivers through the game
        // window with the operating system's input, acknowledged in a file;
        // this host only locates targets and reads the view's own state
        // after each action. Nothing is authored, saved or cast.
        private const double PhysicalDeadlineSeconds = 180;
        private readonly PhysicalWorkspaceRecord _physicalRecord = new PhysicalWorkspaceRecord();
        private int _physicalStep;
        private string _physicalPending;
        private string _physicalModeBefore;
        private bool _physicalPublished;
        private System.Diagnostics.Stopwatch _physicalClock;
        private System.Diagnostics.Stopwatch _physicalSettle;

        private bool UpdatePhysicalWorkspace()
        {
            CastingWorkspaceScreenView view = BuffPlannerUiRoot.CastingWorkspaceViewForRuntime;
            if (_physicalClock == null)
            {
                _physicalClock = System.Diagnostics.Stopwatch.StartNew();
                object screenRaw;
                _physicalRecord.ExpectedScreen = _request.Parameters.TryGetValue("expectedScreen", out screenRaw)
                    ? screenRaw as string : null;
                _physicalRecord.ScreenWidth = Screen.width;
                _physicalRecord.ScreenHeight = Screen.height;
                _physicalRecord.FullScreen = Screen.fullScreen;
                // The workspace must have opened through the physical hotkey,
                // not the host's labeled programmatic fallback (review C4).
                _physicalRecord.OpenedPhysically = !_workspaceProgrammaticOpen;
                _physicalModeBefore = GameModeName();
                BuffPlannerUiRoot.BeginPhysicalInputProbe();
                _log.Info("[KBP-PHYSICAL] started;screen=" + Screen.width + "x" + Screen.height +
                    ";fullScreen=" + Screen.fullScreen + ";mode=" + _physicalModeBefore +
                    ";openedPhysically=" + _physicalRecord.OpenedPhysically + ".");
            }
            if (_physicalClock.Elapsed.TotalSeconds > PhysicalDeadlineSeconds)
                return FinishPhysical("deadline:step" + _physicalStep);
            if (_physicalPending != null)
            {
                if (!PhysicalInputAcknowledged(_physicalPending)) return false;
                string ack = File.ReadAllText(Path.Combine(_request.EvidenceDirectory,
                    "physical-input-" + _physicalPending + ".ack.json"));
                JObject ackRoot = JObject.Parse(ack);
                if (ackRoot.Value<bool?>("deliveryFailed") == true)
                    return FinishPhysical("delivery-failed:" + _physicalPending + ":" + ackRoot.Value<string>("error"));
                if (_physicalPending == "ws-focus-cycle") _physicalRecord.FocusCycle = ackRoot.Value<string>("detail");
                _physicalRecord.Acknowledged.Add(_physicalPending);
                _physicalPending = null;
                _physicalSettle = System.Diagnostics.Stopwatch.StartNew();
            }
            double settled = _physicalSettle == null ? 0 : _physicalSettle.Elapsed.TotalSeconds;
            if (view == null && _physicalStep >= 1 && _physicalStep <= 7)
                return FinishPhysical("workspace-closed-early:step" + _physicalStep);
            if (_physicalStep == 0)
            {
                Vector2? search = view == null ? null : view.ScreenPointForRuntime("search");
                if (search == null) return FinishPhysical("search-not-on-screen");
                return RequestPhysical("ws-click-search", "click", search.Value, null, 1);
            }
            if (_physicalStep == 1)
            {
                // Focus first; nothing is typed into a field that is not
                // focused (letters would reach the game as hotkeys).
                if (!view.SearchFocusedForRuntime && settled < 3) return false;
                _physicalRecord.SearchFocused = view.SearchFocusedForRuntime;
                if (!_physicalRecord.SearchFocused) return FinishPhysical("search-not-focused-before-typing");
                // The query names a tile that is NOT selected now, so the
                // click below must change the selection (review B5).
                _physicalRecord.SelectedBeforeClick = view.SelectedSourceIdForRuntime;
                foreach (string tile in view.VisibleSourcesForRuntime())
                {
                    string[] parts = tile.Split('|');
                    string query;
                    string suffix;
                    if (parts.Length != 3 || parts[2] == "True" || parts[0] == _physicalRecord.SelectedBeforeClick ||
                        !PhysicalWorkspaceRecord.DeriveQuery(parts[1], out query, out suffix)) continue;
                    _physicalRecord.TargetSource = parts[0];
                    _physicalRecord.Query = query;
                    _physicalRecord.QuerySuffix = suffix;
                    break;
                }
                if (_physicalRecord.TargetSource == null) return FinishPhysical("no-unselected-target-tile");
                Vector2? grid = view.ScreenPointForRuntime("buff-grid");
                if (grid == null) return FinishPhysical("grid-not-on-screen");
                _physicalRecord.GridOverflows = view.BuffGridOverflowsForRuntime;
                _physicalRecord.ScrollBefore = view.BuffGridScrollForRuntime;
                return RequestPhysical("ws-wheel-grid", "wheel", grid.Value, ",\"delta\":-360", 2);
            }
            if (_physicalStep == 2)
            {
                if (settled < 1) return false;
                _physicalRecord.ScrollAfter = view.BuffGridScrollForRuntime;
                if (!view.SearchFocusedForRuntime) return FinishPhysical("search-focus-lost-before-typing");
                return RequestPhysical("ws-type-query", "type", Vector2.zero,
                    ",\"text\":" + JsonConvert.ToString(_physicalRecord.Query), 3);
            }
            if (_physicalStep == 3)
            {
                if (view.SearchTextForRuntime != _physicalRecord.Query && settled < 3) return false;
                if (settled < 0.5) return false;
                _physicalRecord.SearchText = view.SearchTextForRuntime;
                _physicalRecord.ModeAfterTyping = GameModeName() == _physicalModeBefore ? "planner" : GameModeName();
                _physicalRecord.VisibleAfterQuery.AddRange(view.VisibleSourcesForRuntime());
                Vector2? tilePoint = view.ScreenPointForRuntime("tile:" + _physicalRecord.TargetSource);
                if (tilePoint == null) return FinishPhysical("target-tile-not-on-screen:" + _physicalRecord.TargetSource);
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, "workspace-physical-query.png"));
                return RequestPhysical("ws-click-tile", "click", tilePoint.Value, null, 4);
            }
            if (_physicalStep == 4)
            {
                if (view.SelectedSourceIdForRuntime != _physicalRecord.TargetSource && settled < 3) return false;
                _physicalRecord.SelectedAfterClick = view.SelectedSourceIdForRuntime;
                return RequestPhysical("ws-focus-cycle", "focus-cycle", Vector2.zero, null, 5);
            }
            if (_physicalStep == 5)
            {
                // The game window was minimized and restored by the launcher
                // (the game may not update while minimized); it must regain
                // focus with the workspace still open.
                if (!Application.isFocused && settled < 10) return false;
                _physicalRecord.FocusRegained = Application.isFocused;
                _physicalRecord.WorkspaceOpenAfterFocus = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                view = BuffPlannerUiRoot.CastingWorkspaceViewForRuntime;
                Vector2? search = view == null ? null : view.ScreenPointForRuntime("search");
                if (search == null) return FinishPhysical("search-not-on-screen-after-focus");
                return RequestPhysical("ws-click-search-again", "click", search.Value, null, 6);
            }
            if (_physicalStep == 6)
            {
                if (!view.SearchFocusedForRuntime && settled < 3) return false;
                if (!view.SearchFocusedForRuntime) return FinishPhysical("search-not-focused-after-focus-loss");
                return RequestPhysical("ws-type-more", "type", Vector2.zero,
                    ",\"text\":" + JsonConvert.ToString(_physicalRecord.QuerySuffix), 7);
            }
            if (_physicalStep == 7)
            {
                string expected = _physicalRecord.Query + _physicalRecord.QuerySuffix;
                if (view.SearchTextForRuntime != expected && settled < 3) return false;
                _physicalRecord.SearchTextAfterFocus = view.SearchTextForRuntime;
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, "workspace-physical-after-focus.png"));
                return RequestPhysical("ws-escape", "key-escape", Vector2.zero, null, 8);
            }
            if (_physicalStep == 8)
            {
                if (BuffPlannerUiRoot.IsCastingWorkspaceOpen && settled < 5) return false;
                if (settled < 1) return false;
                _physicalRecord.ClosedByEscape = !BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                _physicalRecord.LeaseReleased = !BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime;
                _physicalRecord.ModeAfterClose = GameModeName();
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, "workspace-physical-closed.png"));
                return FinishPhysical(null);
            }
            return false;
        }

        private bool RequestPhysical(string id, string action, Vector2 position, string extraJson, int nextStep)
        {
            WritePhysicalInputRequest(id, action, position, extraJson);
            _physicalPending = id;
            _physicalStep = nextStep;
            return false;
        }

        private static string GameModeName()
        {
            return Kingmaker.Game.Instance == null ? "none" : Kingmaker.Game.Instance.CurrentMode.ToString();
        }

        private bool FinishPhysical(string failure)
        {
            if (failure != null) _physicalRecord.Failures.Add(failure);
            UiInputIsolationProbeResult isolation = BuffPlannerUiRoot.EndPhysicalInputProbe();
            if (isolation != null)
            {
                _physicalRecord.PlayerCommands = isolation.PlayerCommandCount;
                _physicalRecord.MovementCommands = isolation.MovementCommandCount;
                _physicalRecord.AbilityCommands = isolation.AbilityCommandCount;
                _physicalRecord.SelectionEvents = isolation.SelectionEventCount;
                _physicalRecord.AbilityTargetEvents = isolation.AbilityTargetEventCount;
                _physicalRecord.SelectionUnchanged = isolation.SelectionUnchanged;
                _physicalRecord.CameraUnchanged = isolation.CameraUnchanged;
            }
            else _physicalRecord.Failures.Add("isolation-probe-missing");
            PublishPhysicalRecord();
            _completed = true;
            return true;
        }

        private void PublishPhysicalRecord()
        {
            _physicalPublished = true;
            PhysicalWorkspaceRecord record = _physicalRecord;
            AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "physical-workspace.json"), new JObject
            {
                { "schemaVersion", 1 },
                { "runId", _request.RunId },
                { "expectedScreen", record.ExpectedScreen },
                { "screen", record.ScreenWidth + "x" + record.ScreenHeight },
                { "fullScreen", record.FullScreen },
                { "openedPhysically", record.OpenedPhysically },
                { "query", record.Query },
                { "querySuffix", record.QuerySuffix },
                { "targetSource", record.TargetSource },
                { "selectedBeforeClick", record.SelectedBeforeClick },
                { "acknowledged", new JArray(record.Acknowledged.Cast<object>().ToArray()) },
                { "searchFocused", record.SearchFocused },
                { "searchText", record.SearchText },
                { "visibleAfterQuery", new JArray(record.VisibleAfterQuery.Cast<object>().ToArray()) },
                { "modeBefore", _physicalModeBefore },
                { "modeAfterTyping", record.ModeAfterTyping },
                { "gridOverflows", record.GridOverflows },
                { "scrollBefore", record.ScrollBefore.HasValue ? (JToken)record.ScrollBefore.Value : JValue.CreateNull() },
                { "scrollAfter", record.ScrollAfter.HasValue ? (JToken)record.ScrollAfter.Value : JValue.CreateNull() },
                { "wheelEvidence", record.WheelEvidence },
                { "selectedAfterClick", record.SelectedAfterClick },
                { "focusCycle", record.FocusCycle },
                { "focusRegained", record.FocusRegained },
                { "workspaceOpenAfterFocus", record.WorkspaceOpenAfterFocus },
                { "searchTextAfterFocus", record.SearchTextAfterFocus },
                { "closedByEscape", record.ClosedByEscape },
                { "leaseReleased", record.LeaseReleased },
                { "modeAfterClose", record.ModeAfterClose },
                { "isolation", "commands=" + record.PlayerCommands + "/" + record.MovementCommands + "/" +
                    record.AbilityCommands + ";events=" + record.SelectionEvents + "/" + record.AbilityTargetEvents +
                    ";selectionUnchanged=" + record.SelectionUnchanged + ";cameraUnchanged=" + record.CameraUnchanged },
                { "failures", new JArray(record.Failures.Cast<object>().ToArray()) },
                { "violations", new JArray(record.Violations().Cast<object>().ToArray()) }
            }.ToString(Formatting.Indented) + Environment.NewLine);
            _log.Info("[KBP-PHYSICAL] published;violations=" + string.Join("|", record.Violations().ToArray()) + ".");
        }

        // Classic cast scenarios (mission batch 3, section 6). The casting-
        // first workspace is closed and the planner switched back to Classic
        // (an automated session's forced default); the Classic screen's own
        // controls author the Resistance cantrip for one target; the classic
        // plan is recorded (select) or, with the run-bound allowance,
        // executed once through the HUD's routine entry under a single-use
        // grant for exactly that plan (cast), observed around the run and
        // judged by ClassicCastRecord. Every other classic route stays
        // locked.
        internal const string ClassicCantripGuid = "7bc8e27cba24f0e43ae64ed201ad5785";

        private bool UpdateClassicCast()
        {
            if (_classicStage == 0)
            {
                ProbeWorkspaceCloseResult closed = CloseProbeWorkspace();
                _classicWorkspaceClosed = closed.Closed && closed.InputLeaseReleased &&
                    string.IsNullOrEmpty(closed.Failure);
                _classicRecord.CastingScenario = RuntimeTestProtocol.IsCastingClassicScenario(_request.Scenario);
                _classicRecord.ExecutionMode = _request.Parameters["executionMode"] as string;
                _classicStage = 1;
                if (!_classicWorkspaceClosed)
                    return FailClassic("workspace-not-closed:" + (closed.Failure ?? "lease-held"));
                UI.CastingWorkspaceDevSelection.Enabled = false;
                BuffPlannerUiRoot.HandlePlannerHotkey();
                _classicClock = System.Diagnostics.Stopwatch.StartNew();
                _log.Info("[KBP-CLASSIC] classic planner selected and opened;scenario=" + _request.Scenario + ".");
                return false;
            }
            if (_classicStage == 1)
            {
                if (!BuffPlannerUiRoot.IsScreenOpen)
                {
                    if (_classicClock.Elapsed.TotalSeconds > 60) return FailClassic("classic-screen-not-open");
                    return false;
                }
                if (BuffPlannerUiRoot.IsCastingWorkspaceOpen) return FailClassic("casting-first-workspace-open");
                _classicTarget = BuffPlannerUiRoot.FirstClassicTargetForRuntime();
                if (!BuffPlannerUiRoot.ConfigureClassicCastForRuntime(ClassicCantripGuid, _classicTarget,
                        _classicRecord.ExecutionMode))
                    return FailClassic("classic-authoring-refused:target=" + (_classicTarget ?? "none"));
                _classicPlan = BuffPlannerUiRoot.ClassicPlanForRuntime("long");
                _classicRecord.PlanSteps = _classicPlan == null ? 0 : _classicPlan.Steps.Count;
                _classicRecord.PlanDigest = _classicPlan == null || _classicPlan.Steps.Count == 0
                    ? null : ClassicPlanDigest.Of(_classicPlan);
                WriteClassicPlan();
                BuffPlannerUiRoot.CloseRuntimeSmoke();
                _classicClock.Reset();
                _classicClock.Start();
                _classicStage = 2;
                if (_classicRecord.PlanSteps < 1) return FailClassic("classic-plan-empty");
                if (!_classicRecord.CastingScenario)
                {
                    // Measured, not assumed: no grant, no casting-first run,
                    // no Classic run or result.
                    _classicRecord.GrantArmedAtEnd = UI.NativeCastingSessionPolicy.ClassicGrant != null;
                    _classicRecord.CastingFirstRuns = BuffPlannerUiRoot.CastingRunsStartedForRuntime;
                    _classicRecord.ClassicRunSeen = BuffPlannerUiRoot.IsExecutingForRuntime ||
                        BuffPlannerUiRoot.QuickResultForRuntime("long") != null;
                    PublishClassicRecord();
                    _completed = true;
                    return true;
                }
                return false;
            }
            if (_classicStage == 2)
            {
                if (BuffPlannerUiRoot.IsScreenOpen || !BuffPlannerUiRoot.WorldRunsForCasting)
                {
                    if (_classicClock.Elapsed.TotalSeconds > 120) return FailClassic("world-not-running-after-close");
                    return false;
                }
                ClassicCastAllowance allowance = ReadClassicAllowance();
                if (allowance == null) return FailClassic("allowance:" + _classicRecord.AllowanceStatus);
                _classicBefore = new Dictionary<int, ProbeObservation>();
                for (int index = 0; index < _classicPlan.Steps.Count; index++)
                {
                    CastStep step = _classicPlan.Steps[index];
                    ProbeObservation before;
                    try { before = new KingmakerProbeObserver().Observe(step, "classic-before:" + index, _probeClock); }
                    catch (Exception exception) { return FailClassic("before-read:" + index + ":" + exception.GetType().Name); }
                    if (before == null || !before.Succeeded ||
                        !string.Equals(before.TargetUnitId, step.TargetUnitIds.FirstOrDefault(), StringComparison.Ordinal) ||
                        before.AvailableForCast == null)
                        return FailClassic("before-read:" + index + ":" + (before == null ? "null"
                            : before.Failure ?? "incomplete"));
                    _classicBefore[index] = before;
                }
                _classicPoolsBefore = ClassicFinitePools();
                _classicGrant = new ClassicCastGrant(_request.RunId, "long", _classicRecord.PlanDigest,
                    _classicRecord.ExecutionMode, allowance.MaximumNativeSubmissions);
                if (!UI.NativeCastingSessionPolicy.ArmClassicGrant(_classicGrant))
                    return FailClassic("grant-not-armed");
                // The result this press produces is a NEW one (a result from
                // earlier in the session never stands in for it).
                _classicQuickBefore = BuffPlannerUiRoot.QuickResultForRuntime("long");
                _log.Info("[KBP-CLASSIC] grant armed;pressing the Long routine;" + _classicGrant.Describe() + ".");
                if (!BuffPlannerUiRoot.PressRoutineForRuntime("long"))
                    return FailClassic("press-not-started");
                _classicClock.Reset();
                _classicClock.Start();
                _classicStage = 3;
                return false;
            }
            if (_classicStage == 3)
            {
                QuickExecutionResult quick = BuffPlannerUiRoot.QuickResultForRuntime("long");
                if (BuffPlannerUiRoot.IsExecutingForRuntime || quick == null ||
                    ReferenceEquals(quick, _classicQuickBefore))
                {
                    if (_classicClock.Elapsed.TotalSeconds > RuntimeTestProtocol.ClassicRunDeadlineSeconds)
                        return FailClassic("classic-run-deadline");
                    return false;
                }
                JudgeClassicRun(quick);
                EndClassicScenario("classic-scenario-completed");
                PublishClassicRecord();
                _completed = true;
                return true;
            }
            return false;
        }

        // The scenario's end: a Classic run still going is ended through its
        // owned terminal and the grant is disarmed, used or not.
        private void EndClassicScenario(string reason)
        {
            BuffPlannerUiRoot.EndClassicRunForRuntime(reason);
            UI.NativeCastingSessionPolicy.DisarmClassicGrant();
            _classicRecord.GrantArmedAtEnd = UI.NativeCastingSessionPolicy.ClassicGrant != null &&
                !UI.NativeCastingSessionPolicy.ClassicGrant.Disarmed;
        }

        private bool FailClassic(string failure)
        {
            _classicRecord.Failures.Add(failure);
            _log.Info("[KBP-CLASSIC] failed;" + failure + ".");
            EndClassicScenario("classic-scenario-failed");
            if (_classicGrant != null)
            {
                _classicRecord.Grant = _classicGrant.Describe();
                _classicRecord.GrantConsumed = _classicGrant.Consumed;
                _classicRecord.GrantAttempts = _classicGrant.Attempts;
            }
            PublishClassicRecord();
            _completed = true;
            return true;
        }

        // The optional mods the request names, loaded exactly as expected,
        // checked BEFORE anything is cast (the final result checks them
        // again for every scenario). Null when they are.
        private string OptionalModMismatch()
        {
            foreach (RuntimeExpectedOptionalMod expected in _request.ExpectedOptionalMods)
            {
                string name = Path.GetFileNameWithoutExtension(expected.AssemblyName);
                List<Assembly> loaded = AppDomain.CurrentDomain.GetAssemblies().Where(a =>
                    string.Equals(a.GetName().Name, name, StringComparison.Ordinal)).ToList();
                if (loaded.Count != 1)
                    return "optional-mod-mismatch:" + expected.UmmId + ":assemblies=" + loaded.Count;
                string hash;
                try { hash = Hashing.Sha256(LoadedAssemblyIdentity.ResolveCanonicalFile(loaded[0].Location)); }
                catch (Exception exception)
                {
                    return "optional-mod-mismatch:" + expected.UmmId + ":" + exception.GetType().Name;
                }
                if (!string.Equals(hash, expected.AssemblySha256, StringComparison.Ordinal))
                    return "optional-mod-mismatch:" + expected.UmmId + ":sha256";
            }
            return null;
        }

        // One string parameter of the request, or null.
        private string RequestText(string name)
        {
            object raw;
            return _request.Parameters.TryGetValue(name, out raw) ? raw as string : null;
        }

        // The run-bound classic allowance: this run, this build, this
        // campaign, profile and WORKING save, this casting mode and routine,
        // and exactly the plan the classic controls just authored.
        private ClassicCastAllowance ReadClassicAllowance()
        {
            object raw;
            string json = _request.Parameters.TryGetValue("classicAllowance", out raw) ? raw as string : null;
            string refusal = "absent";
            ClassicCastAllowance allowance = json == null ? null
                : ClassicCastAllowance.Parse(json, _request.RunId, out refusal);
            _classicRecord.AllowanceStatus = allowance == null ? refusal : "parsed";
            if (allowance == null) return null;
            SingleCastProbeRuntimeIdentity measured = MeasureProbeRuntimeIdentity();
            string campaign = Kingmaker.Game.Instance == null || Kingmaker.Game.Instance.Player == null
                ? null : Kingmaker.Game.Instance.Player.GameId;
            string mismatch =
                measured.SourceCommit != allowance.SourceCommit ? "identity-mismatch:commit"
                : measured.PackageSha256 != allowance.PackageSha256 ? "identity-mismatch:package"
                : measured.DllSha256 != allowance.DllSha256 ? "identity-mismatch:dll"
                : measured.AssemblyMvid != allowance.AssemblyMvid ? "identity-mismatch:mvid"
                : !string.Equals(campaign, allowance.FixtureGameId, StringComparison.Ordinal) ? "fixture-mismatch"
                : AllowanceFixtureBinding.RequestMismatch(allowance.CompatibilityProfileId,
                    allowance.WorkingSaveSha256, _request.ProfileId, RequestText("workingSha256")) ??
                OptionalModMismatch() ??
                (!string.Equals(_classicRecord.ExecutionMode, allowance.ExecutionMode, StringComparison.Ordinal)
                    ? "execution-mode-mismatch"
                : allowance.RoutineId != "long" ? "routine-mismatch"
                : !string.Equals(_classicRecord.PlanDigest, allowance.ApprovedPlanDigest, StringComparison.Ordinal)
                    ? "plan-differs-from-approval"
                : _classicRecord.PlanSteps > allowance.MaximumNativeSubmissions ? "plan-over-budget"
                : null);
            _classicRecord.AllowanceStatus = mismatch ?? "valid";
            return mismatch == null ? allowance : null;
        }

        // Remaining units of every finite pool of the party (spell levels,
        // prepared slots, ability resources), for the no-finite-loss check.
        private static Dictionary<string, int> ClassicFinitePools()
        {
            var pools = new Dictionary<string, int>(StringComparer.Ordinal);
            CastingWorkspaceInputs inputs = BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime();
            foreach (Domain.Providers.ResourcePoolSnapshot pool in inputs.Snapshot.ResourcePools)
                if (pool.Kind != Domain.Providers.ResourcePoolKind.Unlimited && !pools.ContainsKey(pool.PoolKey))
                    pools.Add(pool.PoolKey, pool.Kind == Domain.Providers.ResourcePoolKind.PreparedSlots
                        ? pool.Tokens.Count(token => token.Available) : pool.Remaining);
            return pools;
        }

        private void JudgeClassicRun(QuickExecutionResult quick)
        {
            _classicRecord.QuickDisposition = quick.Disposition.ToString();
            _classicRecord.Grant = _classicGrant.Describe();
            _classicRecord.GrantConsumed = _classicGrant.Consumed;
            _classicRecord.GrantAttempts = _classicGrant.Attempts;
            // The classic route never uses the casting-first host: none of
            // its runs may have started in this session.
            _classicRecord.CastingFirstRuns = BuffPlannerUiRoot.CastingRunsStartedForRuntime;
            ExecutionReport report = BuffPlannerUiRoot.ClassicReportForRuntime;
            if (report == null)
            {
                _classicRecord.Failures.Add("report-missing");
                return;
            }
            _classicRecord.Planned = report.Planned;
            _classicRecord.Queued = report.Queued;
            _classicRecord.CastStarted = report.CastStarted;
            _classicRecord.Submitted = report.Submitted;
            _classicRecord.Confirmed = report.Confirmed;
            _classicRecord.Failed = report.Failed;
            var stopping = new[]
            {
                CastExecutionStatus.FailedValidation, CastExecutionStatus.FailedSubmission,
                CastExecutionStatus.FailedExecution, CastExecutionStatus.TimedOutUnconfirmed,
                CastExecutionStatus.ResidualStateUnsettled
            };
            for (int index = 0; index < _classicPlan.Steps.Count; index++)
            {
                CastStep step = _classicPlan.Steps[index];
                List<CastExecutionRecord> records = report.Records.Where(record => record.StepIndex == index).ToList();
                CastExecutionRecord stop = records.FirstOrDefault(record => stopping.Contains(record.Status));
                string final = stop != null ? stop.Status.ToString()
                    : records.Any(record => record.Status == CastExecutionStatus.EffectConfirmed) ? "EffectConfirmed"
                    : records.Count == 0 ? "NoRecord" : records.Last().Status.ToString();
                ProbeObservation before;
                _classicBefore.TryGetValue(index, out before);
                ProbeObservation after = null;
                try { after = new KingmakerProbeObserver().Observe(step, "classic-after:" + index, _probeClock); }
                catch (Exception exception)
                {
                    _classicRecord.Failures.Add("after-read:" + index + ":" + exception.GetType().Name);
                }
                _classicRecord.Steps.Add(new ClassicCastStepResult(index, step.Provider.Canonical,
                    step.Provider.Ability.SourceKind == Domain.Identity.SourceKind.Spellbook &&
                        step.Provider.SourceInstanceId == "level-0|heighten-0",
                    step.TargetUnitIds.FirstOrDefault())
                {
                    FinalStatus = final,
                    Detail = string.Join(" || ", records.Select(record => record.Status + ":" + record.Detail).ToArray()),
                    Transition = CastingQualificationDriver.Transition(before, after),
                    AvailableBefore = before == null ? null : before.AvailableForCast,
                    AvailableAfter = after == null ? null : after.AvailableForCast
                });
            }
            Dictionary<string, int> poolsAfter = ClassicFinitePools();
            foreach (KeyValuePair<string, int> pool in _classicPoolsBefore.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                int remaining;
                _classicRecord.FinitePools.Add(pool.Key + ":" + pool.Value + ">" +
                    (poolsAfter.TryGetValue(pool.Key, out remaining) ? remaining.ToString() : "missing"));
            }
        }

        private void WriteClassicPlan()
        {
            var steps = new JArray();
            if (_classicPlan != null)
                for (int index = 0; index < _classicPlan.Steps.Count; index++)
                {
                    CastStep step = _classicPlan.Steps[index];
                    steps.Add(new JObject
                    {
                        { "index", index },
                        { "provider", step.Provider.Canonical },
                        { "caster", step.Provider.CasterUnitId },
                        { "spellbook", step.Provider.SpellbookGuid },
                        { "sourceKind", step.Provider.Ability.SourceKind.ToString() },
                        { "sourceInstance", step.Provider.SourceInstanceId },
                        { "source", step.SourceId },
                        { "targets", new JArray(step.TargetUnitIds.Cast<object>().ToArray()) },
                        { "pool", step.Reservation == null ? null : step.Reservation.PoolKey },
                        { "unlimited", step.Reservation != null && step.Reservation.Unlimited },
                        { "strategy", step.ExecutionStrategy.ToString() }
                    });
                }
            AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "classic-plan.json"), new JObject
            {
                { "schemaVersion", 1 },
                { "runId", _request.RunId },
                { "scenario", _request.Scenario },
                { "executionMode", _classicRecord.ExecutionMode },
                { "target", _classicTarget },
                { "planDigest", _classicRecord.PlanDigest },
                { "canonical", _classicPlan == null ? null : ClassicPlanDigest.Canonical(_classicPlan) },
                { "steps", steps }
            }.ToString(Formatting.Indented) + Environment.NewLine);
        }

        private void PublishClassicRecord()
        {
            _classicPublished = true;
            ClassicCastRecord record = _classicRecord;
            AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "classic-outcome.json"), new JObject
            {
                { "schemaVersion", 2 },
                { "runId", _request.RunId },
                { "scenario", _request.Scenario },
                { "castingScenario", record.CastingScenario },
                { "allowanceStatus", record.AllowanceStatus },
                { "executionMode", record.ExecutionMode },
                { "planDigest", record.PlanDigest },
                { "planSteps", record.PlanSteps },
                { "grant", record.Grant },
                { "grantConsumed", record.GrantConsumed },
                { "grantAttempts", record.GrantAttempts },
                { "quickDisposition", record.QuickDisposition },
                { "report", record.ReportLine },
                { "castingFirstRuns", record.CastingFirstRuns },
                { "grantArmedAtEnd", record.GrantArmedAtEnd },
                { "classicRunSeen", record.ClassicRunSeen },
                { "steps", new JArray(record.Steps.Select(step => (object)new JObject
                    {
                        { "index", step.StepIndex },
                        { "provider", step.ProviderCanonical },
                        { "levelZeroSpellbook", step.LevelZeroSpellbook },
                        { "target", step.TargetUnitId },
                        { "finalStatus", step.FinalStatus },
                        { "transition", step.Transition },
                        { "availableBefore", step.AvailableBefore.HasValue ? (JToken)step.AvailableBefore.Value : JValue.CreateNull() },
                        { "availableAfter", step.AvailableAfter.HasValue ? (JToken)step.AvailableAfter.Value : JValue.CreateNull() },
                        { "detail", step.Detail }
                    }).ToArray()) },
                { "finitePools", new JArray(record.FinitePools.Cast<object>().ToArray()) },
                { "failures", new JArray(record.Failures.Cast<object>().ToArray()) },
                { "violations", new JArray(record.Violations().Cast<object>().ToArray()) }
            }.ToString(Formatting.Indented) + Environment.NewLine);
            _log.Info("[KBP-CLASSIC] published;violations=" + string.Join("|", record.Violations().ToArray()) + ".");
        }

        // Qualification scenario: the Unity-free driver runs the whole
        // sequence over the production path; this host only builds it with
        // the real adapters (fresh discovery, production executors, fresh
        // native reads), pumps it once per update and publishes the record.
        private bool UpdateQualification()
        {
            if (_qualificationDriver == null)
            {
                ProbeWorkspaceCloseResult closed = CloseProbeWorkspace();
                // Review P3-7: the production workspace must close and
                // release its input lease before the driver authors anything.
                _qualificationWorkspaceClosed = closed.Closed && closed.InputLeaseReleased &&
                    string.IsNullOrEmpty(closed.Failure);
                _qualificationRecord.CastingScenario =
                    RuntimeTestProtocol.IsCastingQualificationScenario(_request.Scenario);
                if (!_qualificationWorkspaceClosed)
                {
                    _qualificationRecord.Failures.Add("workspace-not-closed:" +
                        (closed.Failure ?? "lease-held"));
                    _qualificationRecord.TerminalReason = "failed:workspace-not-closed";
                    PublishQualificationRecord();
                    _completed = true;
                    return true;
                }
                CastingQualificationAllowance allowance = null;
                if (_qualificationRecord.CastingScenario)
                {
                    object raw;
                    string json = _request.Parameters.TryGetValue("qualificationAllowance", out raw)
                        ? raw as string : null;
                    string refusal = "absent";
                    allowance = json == null ? null
                        : CastingQualificationAllowance.Parse(json, _request.RunId, out refusal);
                    _qualificationRecord.AllowanceStatus = allowance == null ? refusal : "parsed";
                    // The launcher's casting mode and the allowance's must
                    // be the same one.
                    string requestedMode = _request.Parameters["executionMode"] as string;
                    if (allowance != null && !string.Equals(allowance.ExecutionMode, requestedMode,
                            StringComparison.Ordinal))
                    {
                        _qualificationRecord.AllowanceStatus = "execution-mode-mismatch:" +
                            allowance.ExecutionMode + "/" + (requestedMode ?? "none");
                        allowance = null;
                    }
                    if (allowance != null)
                    {
                        SingleCastProbeRuntimeIdentity measured = MeasureProbeRuntimeIdentity();
                        string mismatch =
                            measured.SourceCommit != allowance.SourceCommit ? "commit"
                            : measured.PackageSha256 != allowance.PackageSha256 ? "package"
                            : measured.DllSha256 != allowance.DllSha256 ? "dll"
                            : measured.AssemblyMvid != allowance.AssemblyMvid ? "mvid" : null;
                        if (mismatch != null)
                        {
                            _qualificationRecord.AllowanceStatus = "identity-mismatch:" + mismatch;
                            allowance = null;
                        }
                    }
                    // Review C5: the approved profile and WORKING save are the
                    // ones this run was launched with.
                    string bindingMismatch = allowance == null ? null
                        : AllowanceFixtureBinding.RequestMismatch(allowance.CompatibilityProfileId,
                            allowance.WorkingSaveSha256, _request.ProfileId, RequestText("workingSha256")) ??
                            OptionalModMismatch();
                    if (bindingMismatch != null)
                    {
                        _qualificationRecord.AllowanceStatus = bindingMismatch;
                        allowance = null;
                    }
                }
                string campaignId = Kingmaker.Game.Instance == null || Kingmaker.Game.Instance.Player == null
                    ? null : Kingmaker.Game.Instance.Player.GameId;
                // Read-only: how the game judges each caster's level-0
                // spells (the command's own availability guard) before
                // anything is authored or cast.
                IList<string> cantrips = GameAdapters.KingmakerCantripDiagnostics.Describe();
                AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "cantrip-diagnostics.json"),
                    new JObject
                    {
                        { "schemaVersion", 1 },
                        { "runId", _request.RunId },
                        { "lines", new JArray(cantrips.Cast<object>().ToArray()) }
                    }.ToString(Formatting.Indented) + Environment.NewLine);
                _log.Info("[KBP-QUAL] cantrip diagnostics;lines=" + cantrips.Count + ".");
                // Read-only: the loaded area, the autosave setting and every
                // area transition present (mission batch 3, section 9).
                IList<string> areaLines = GameAdapters.KingmakerAreaDiagnostics.Describe();
                AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "area-diagnostics.json"),
                    new JObject
                    {
                        { "schemaVersion", 1 },
                        { "runId", _request.RunId },
                        { "lines", new JArray(areaLines.Cast<object>().ToArray()) }
                    }.ToString(Formatting.Indented) + Environment.NewLine);
                _log.Info("[KBP-QUAL] area diagnostics;lines=" + areaLines.Count + ".");
                // Read-only: every capability the planner's own discovery
                // sees (providers, pools, targeting shapes, enhancements).
                IList<string> capabilities = CastingCapabilityInventory.Describe(
                    BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime());
                AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "capability-inventory.json"),
                    new JObject
                    {
                        { "schemaVersion", 1 },
                        { "runId", _request.RunId },
                        { "lines", new JArray(capabilities.Cast<object>().ToArray()) }
                    }.ToString(Formatting.Indented) + Environment.NewLine);
                _log.Info("[KBP-QUAL] capability inventory;lines=" + capabilities.Count + ".");
                string modPath = _modEntry.Path;
                object recipeRaw;
                // The planner's own execution host runs the approved runs:
                // its root pumps it once per frame while the world runs and
                // its deadline counts only running time; the run's own
                // deadline stays wall-clock time and bounds a world that
                // never runs (review P3-2). Each mod update ticks the root
                // (one pump) and THEN this host, so the stop step's press
                // always lands before the next pump: a casting that ended
                // in that pump is followed by the stop, never by the next
                // casting.
                var clock = System.Diagnostics.Stopwatch.StartNew();
                _qualificationHost = BuffPlannerUiRoot.CastingHostForRuntime;
                _qualificationRunsBefore = _qualificationHost.StartedRuns;
                _qualificationDriver = new CastingQualificationDriver(_qualificationRecord, allowance,
                    campaignId, BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime,
                    boundary => new CastingWorkspaceSession(modPath, campaignId, boundary),
                    _qualificationHost,
                    (step, label) => new KingmakerProbeObserver().Observe(step, label, _probeClock),
                    () => clock.ElapsedMilliseconds,
                    RuntimeTestProtocol.QualificationRunDeadlineSeconds * 1000L,
                    _request.Parameters.TryGetValue("qualificationRecipe", out recipeRaw)
                        ? recipeRaw as string : null,
                    () => BuffPlannerUiRoot.WorldRunsForCasting, true,
                    BuffPlannerUiRoot.PressRoutineForRuntime, Main.SetEnabledForRuntime,
                    () => "subscriptions=" + BuffPlannerUiRoot.ActiveEventSubscriptionsForRuntime +
                        ";hudRoots=" + BuffPlannerUiRoot.HudRootCountForRuntime +
                        ";hudInstalled=" + BuffPlannerUiRoot.IsHudInstalledForRuntime +
                        ";plannerRoots=" + UnityEngine.Object.FindObjectsOfType<BuffPlannerUiRoot>().Length +
                        ";mode=" + (Kingmaker.Game.Instance == null ? "none"
                            : Kingmaker.Game.Instance.CurrentMode.ToString()),
                    () => BuffPlannerUiRoot.OwnedTicksForRuntime,
                    (step, recipient, label) => new KingmakerProbeObserver().ObserveRecipient(
                        step, recipient, label, _probeClock),
                    (caster, pool) => new KingmakerProbeObserver().ObserveCaster(caster, pool));
                _log.Info("[KBP-QUAL] driver built;casting=" + _qualificationRecord.CastingScenario +
                    ";allowance=" + _qualificationRecord.AllowanceStatus + ";workspaceClosed=" +
                    closed.Closed + ";campaign=" + campaignId + ".");
                return false;
            }
            _qualificationDriver.Update();
            if (!_qualificationDriver.Completed) return false;
            PublishQualificationRecord();
            _liveInitialCatalogEvidence = "qualification-scenario;workspaceRoot=active;legacyScreen=closed";
            _workspaceInteractionEvidence = "qualification;recipe-authoring";
            _workspaceReopenEvidence = "qualification;session-reopened-from-disk";
            _completed = true;
            return true;
        }

        private readonly CastingQualificationRecord _qualificationRecord = new CastingQualificationRecord();
        private CastingQualificationDriver _qualificationDriver;
        private CastingExecutionHost _qualificationHost;
        private int _qualificationRunsBefore = -1;

        private readonly ClassicCastRecord _classicRecord = new ClassicCastRecord();
        private int _classicStage;
        private bool _classicWorkspaceClosed;
        private bool _classicPublished;
        private string _classicTarget;
        private CastPlan _classicPlan;
        private ClassicCastGrant _classicGrant;
        private QuickExecutionResult _classicQuickBefore;
        private Dictionary<int, ProbeObservation> _classicBefore;
        private Dictionary<string, int> _classicPoolsBefore;
        private System.Diagnostics.Stopwatch _classicClock;
        private bool _qualificationWorkspaceClosed;
        private bool _qualificationPublished;

        private void PublishQualificationRecord()
        {
            _qualificationPublished = true;
            CastingQualificationRecord record = _qualificationRecord;
            var steps = new JArray();
            foreach (CastingQualificationStepResult step in record.Steps)
            {
                steps.Add(new JObject
                {
                    { "name", step.Name },
                    { "applyAllowed", step.ApplyAllowed },
                    { "applyReason", step.ApplyReason },
                    { "projectionId", step.ProjectionId },
                    { "terminal", step.Report == null ? null : step.Report.TerminalReason },
                    { "entries", step.Report == null ? new JArray() : new JArray(step.Report.Entries
                        .Select(entry => (object)(entry.CastingId + "=" + entry.State + ";submitted=" +
                            entry.Submitted + ";spend=" + entry.SpendReported + ";free=" + entry.FreeCast +
                            ";detail=" + entry.Detail)).ToArray()) },
                    { "transitions", new JArray(step.Transitions.Cast<object>().ToArray()) },
                    { "availability", new JArray(step.Availability.Cast<object>().ToArray()) },
                    // Typed token readings (ids are opaque and contain "|").
                    { "tokens", new JArray(step.TokenReadings.Select(reading => (object)new JObject
                        {
                            { "castingId", reading.CastingId },
                            { "tokenId", reading.TokenId },
                            { "before", reading.Before.HasValue ? (JToken)reading.Before.Value : JValue.CreateNull() },
                            { "after", reading.After.HasValue ? (JToken)reading.After.Value : JValue.CreateNull() }
                        }).ToArray()) },
                    { "unreadTokenCastings", new JArray(step.UnreadTokenCastings.Cast<object>().ToArray()) },
                    { "cleanupFailures", step.Report == null ? new JArray()
                        : new JArray(step.Report.CleanupFailures.Cast<object>().ToArray()) },
                    // The enhanced recipe's caster reads and per-casting
                    // stat modifiers (before > after).
                    { "caster", step.CasterBefore == null && step.CasterAfter == null ? null
                        : (step.CasterBefore == null ? "none" : step.CasterBefore.Describe()) + " > " +
                            (step.CasterAfter == null ? "none" : step.CasterAfter.Describe()) },
                    { "modifiers", new JArray(step.ModifiersAfter.Keys.OrderBy(key => key, StringComparer.Ordinal)
                        .Select(key =>
                        {
                            IReadOnlyList<string> before;
                            step.ModifiersBefore.TryGetValue(key, out before);
                            IReadOnlyList<string> after = step.ModifiersAfter[key];
                            return (object)(key + ":" + (before == null ? "unread" : "[" +
                                string.Join(";", before.ToArray()) + "]") + ">" + (after == null ? "unread"
                                    : "[" + string.Join(";", after.ToArray()) + "]"));
                        }).ToArray()) },
                    { "remainingSeconds", new JArray(step.RemainingSecondsAfter.Keys.OrderBy(key => key,
                        StringComparer.Ordinal).Select(key => (object)(key + ":" + (step.RemainingSecondsAfter[key] == null
                            ? "unread" : step.RemainingSecondsAfter[key].Value.ToString("0.0",
                                System.Globalization.CultureInfo.InvariantCulture)))).ToArray()) },
                    { "observations", new JArray(step.Observations.Cast<object>().ToArray()) }
                });
            }
            CastingQualificationSelection selection = record.Selection;
            AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "qual-outcome.json"),
                new JObject
                {
                    { "schemaVersion", 1 },
                    { "runId", _request.RunId },
                    { "scenario", _request.Scenario },
                    { "castingScenario", record.CastingScenario },
                    { "allowanceStatus", record.AllowanceStatus },
                    { "terminalReason", record.TerminalReason },
                    { "exhaustedAccepted", record.ExhaustedAccepted.HasValue
                        ? (JToken)record.ExhaustedAccepted.Value : JValue.CreateNull() },
                    { "selection", selection == null ? null : new JObject
                        {
                            { "selected", selection.Selected },
                            { "recipe", selection.Recipe },
                            { "roster", new JArray(record.Roster.Select(unit => (object)new JObject
                                {
                                    { "unitId", unit.UnitId }, { "name", unit.DisplayName },
                                    { "isPet", unit.IsPet }, { "masterUnitId", unit.MasterUnitId },
                                    { "targetable", unit.TargetValidation.Alive && unit.TargetValidation.Conscious &&
                                        unit.TargetValidation.Friendly && unit.TargetValidation.Targetable }
                                }).ToArray()) },
                            { "coverage", new JArray(selection.Coverage.Cast<object>().ToArray()) },
                            { "refusal", selection.Refusal },
                            { "sourceId", selection.SourceId },
                            { "castings", new JArray(selection.Castings.Select(casting => (object)(
                                casting.CastingId + "=" + casting.CasterUnitId + ">" +
                                casting.DirectTargetUnitId + (casting.Enhancements.Count == 0 ? string.Empty
                                    : "+" + string.Join("+", casting.Enhancements.Where(value => value != null)
                                        .Select(value => value.EnhancementId).ToArray())))).ToArray()) },
                            { "enhancement", selection.Enhancement == null ? null : new JObject
                                {
                                    { "enhancementId", selection.Enhancement.EnhancementId },
                                    { "name", selection.Enhancement.DisplayName },
                                    { "casterUnitId", selection.Enhancement.CasterUnitId },
                                    { "usagePoolId", selection.Enhancement.UsagePoolId },
                                    { "unitsPerCast", selection.Enhancement.UnitsPerCast },
                                    { "modifier", selection.Enhancement.ModifierPrefix },
                                    { "increase", selection.Enhancement.Increase },
                                    { "kind", selection.Enhancement.Kind },
                                    { "durationFactor", selection.Enhancement.DurationFactor }
                                } },
                            { "rejections", new JArray(selection.Rejections.Cast<object>().ToArray()) }
                        } },
                    { "enhancementOptions", new JArray(record.EnhancementOptions.Cast<object>().ToArray()) },
                    { "forecast", record.Forecast == null ? new JArray() : new JArray(record.Forecast
                        .Select(step => (object)new JObject
                        {
                            { "name", step.Name },
                            { "projectionId", step.ProjectionId },
                            { "refusal", step.Refusal },
                            { "castingIds", new JArray(step.CastingIds.Cast<object>().ToArray()) },
                            { "canonicalContract", step.CanonicalContract }
                        }).ToArray()) },
                    { "steps", steps },
                    { "submissions", new JArray(record.Submissions.Cast<object>().ToArray()) },
                    { "plannedSubmissions", record.PlannedSubmissions },
                    { "maximumSubmissions", record.MaximumSubmissions },
                    { "executionMode", record.ExecutionMode },
                    { "stopPress", record.StopPress },
                    { "stopPressHandled", record.StopPressHandled },
                    { "stopPressedInFlight", record.StopPressedInFlight.HasValue
                        ? (JToken)record.StopPressedInFlight.Value : JValue.CreateNull() },
                    { "disable", record.Disable },
                    { "disabledAt", record.DisabledAt },
                    { "disableHeldUpdates", record.DisableHeldUpdates },
                    { "acceptingWhileDisabled", record.AcceptingWhileDisabled },
                    { "runningWhileDisabled", record.RunningWhileDisabled },
                    { "ownerTicksDuringHold", record.OwnerTicksDuringHold.HasValue
                        ? (JToken)record.OwnerTicksDuringHold.Value : JValue.CreateNull() },
                    { "runsStartedDuringHold", record.RunsStartedDuringHold },
                    { "acceptingAfterEnable", record.AcceptingAfterEnable },
                    { "lifecycleBefore", record.LifecycleBefore },
                    { "lifecycleAfter", record.LifecycleAfter },
                    { "runsStarted", record.RunsStarted },
                    { "runsReported", record.RunsReported },
                    { "callbackFailure", record.CallbackFailure },
                    { "hostRunsBefore", _qualificationRunsBefore },
                    { "hostRunsStarted", _qualificationHost == null ? -1 : _qualificationHost.StartedRuns },
                    { "failures", new JArray(record.Failures.Cast<object>().ToArray()) },
                    { "violations", new JArray(record.Violations().Cast<object>().ToArray()) }
                }.ToString(Formatting.Indented) + Environment.NewLine);
            _log.Info("[KBP-QUAL] published;terminal=" + record.TerminalReason + ";violations=" +
                string.Join("|", record.Violations().ToArray()) + ".");
        }

        private bool UpdateProbeSelection()
        {
            if (_probeOwner == null || !_probeOwner.BeginSelection())
            {
                _liveUiPhase = 42;
                return false;
            }
            if (!BuffPlannerUiRoot.IsCastingWorkspaceOpen) return false;
            CastingWorkspaceInputs inputs = BuffPlannerUiRoot.CastingWorkspaceInputsForRuntime();
            if (inputs == null) return false;
            _probeRecord.CastingScenario = RuntimeTestProtocol.IsCastingProbeScenario(_request.Scenario);
            _probeSelection = SingleCastProbeSelector.Select(inputs, "probe",
                KingmakerProbeObserver.EffectPresent);
            _probeRecord.Selected = _probeSelection.Selected;
            ExplicitStepConversion projection = _probeSelection.Projection;
            CastStep step = _probeSelection.Selected ? projection.Plan.Steps[0] : null;
            _probeRecord.ProjectionId = _probeSelection.Selected ? projection.ProjectionId : null;
            _probeRecord.SelectionEvidence = _probeSelection.Selected
                ? "caster=" + _probeSelection.CasterUnitId + ";target=" + _probeSelection.TargetUnitId +
                    ";source=" + _probeSelection.SourceId + ";provider=" + _probeSelection.Provider.Canonical +
                    ";projection=" + projection.ProjectionId + ";considered=" + _probeSelection.CandidatesConsidered
                : "refused:" + _probeSelection.Refusal + ";considered=" + _probeSelection.CandidatesConsidered;
            // A read-only fresh observation of the selected casting (no
            // submission): evidence for the proposal only.
            ProbeObservation preview = step == null ? null
                : new KingmakerProbeObserver().Observe(step, "selection-preview", _probeClock);
            AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "probe-selection.json"),
                new JObject
                {
                    { "schemaVersion", 2 },
                    { "runId", _request.RunId },
                    { "scenario", _request.Scenario },
                    { "selected", _probeSelection.Selected },
                    { "refusal", _probeSelection.Refusal },
                    { "casterUnitId", _probeSelection.CasterUnitId },
                    { "targetUnitId", _probeSelection.TargetUnitId },
                    { "sourceId", _probeSelection.SourceId },
                    { "provider", _probeSelection.Provider == null ? null : _probeSelection.Provider.Canonical },
                    { "projectionId", _probeRecord.ProjectionId },
                    { "canonicalContract", _probeSelection.Selected ? projection.CanonicalContract : null },
                    { "reservedPoolKey", step == null ? null : step.Reservation.PoolKey },
                    { "reservedUnits", step == null ? 0 : step.Reservation.Units },
                    { "reservationUnlimited", step != null && step.Reservation.Unlimited },
                    { "selectionPreviewObservation", preview == null ? null : preview.Describe() },
                    { "candidatesConsidered", _probeSelection.CandidatesConsidered },
                    { "rejections", new JArray(_probeSelection.Rejections.Cast<object>().ToArray()) }
                }.ToString(Formatting.Indented) + Environment.NewLine);
            _log.Info("[KBP-PROBE] selection;" + _probeRecord.SelectionEvidence + ".");
            if (!_probeRecord.CastingScenario || !_probeSelection.Selected)
            {
                _probeOwner.Terminate("completed");
                _liveUiPhase = 42;
                return false;
            }
            object raw;
            string allowanceJson = _request.Parameters != null &&
                _request.Parameters.TryGetValue("probeAllowance", out raw) ? raw as string : null;
            string refusal = "absent";
            SingleCastProbeAllowance allowance = allowanceJson == null ? null
                : SingleCastProbeAllowance.Parse(allowanceJson, _request.RunId, out refusal);
            // The approved profile and WORKING save, and the optional mods
            // loaded exactly as requested, before anything is cast.
            string probeBinding = allowance == null ? null
                : AllowanceFixtureBinding.RequestMismatch(allowance.CompatibilityProfileId,
                    allowance.WorkingSaveSha256, _request.ProfileId, RequestText("workingSha256")) ??
                    OptionalModMismatch();
            if (probeBinding != null)
            {
                refusal = probeBinding;
                allowance = null;
            }
            _probeRecord.AllowanceStatus = allowance == null ? refusal : "valid";
            if (allowance == null)
            {
                _log.Info("[KBP-PROBE] no valid allowance (" + refusal + "); no dispatch boundary constructed.");
                _probeOwner.Terminate("completed");
                _liveUiPhase = 42;
                return false;
            }
            // The cast must run in the world, not under the planner: close the
            // workspace (its input lease holds the game in FullScreenUi) and
            // submit only once the world runs (probe
            // casting-probe-cast-20260923-p1-01 cast inside the open planner;
            // the rule was queued and the effect stayed absent).
            _probeAllowance = allowance;
            _probeStep = step;
            ProbeWorkspaceCloseResult closedForCast = CloseProbeWorkspace();
            _probeRecord.SubmitWorldState = "workspaceClosed=" + closedForCast.Closed +
                ";leaseReleased=" + closedForCast.InputLeaseReleased +
                (string.IsNullOrEmpty(closedForCast.Failure) ? string.Empty
                    : ";closeFailure=" + closedForCast.Failure);
            _probeWorldWaitFrames = 0;
            _probeWorldWaitStartedMillis = _workspaceCaptureElapsed.ElapsedMilliseconds;
            _liveUiPhase = 43;
            return false;
        }

        // The probe's world predicate: Default mode, not paused, and the
        // planner closed with its input lease released.
        private static bool ProbeWorldRuns(out string state)
        {
            state = BuffPlannerUiRoot.WorldStateForRuntime + ";workspaceOpen=" +
                BuffPlannerUiRoot.IsCastingWorkspaceOpen + ";leaseHeld=" +
                BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime;
            return BuffPlannerUiRoot.WorldRunsForCasting &&
                !BuffPlannerUiRoot.IsCastingWorkspaceOpen &&
                !BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime;
        }

        // Waits for the world to run with the planner closed, then constructs
        // the boundary and submits the one approved cast. The wait is bounded
        // in elapsed time, never in updates (an unfocused game can run far
        // above 60 updates a second).
        private bool UpdateProbeAwaitWorld()
        {
            string state;
            bool running = ProbeWorldRuns(out state);
            if (!running)
            {
                _probeWorldWaitFrames++;
                if (_workspaceCaptureElapsed.ElapsedMilliseconds - _probeWorldWaitStartedMillis <
                    RuntimeTestProtocol.ProbeWorldWaitSeconds * 1000L) return false;
                _probeRecord.SubmitWorldState += ";atRefusal:" + state;
                _probeRecord.SubmitReason = "world-not-running:" + state;
                _log.Info("[KBP-PROBE] the world did not run; no dispatch boundary constructed;" + state + ".");
                _probeOwner.Terminate("completed");
                _liveUiPhase = 42;
                return false;
            }
            _probeRecord.WorldRunningAtSubmit = true;
            _probeRecord.SubmitWorldState += ";atSubmit:" + state + ";waitFrames=" + _probeWorldWaitFrames;
            ExplicitStepConversion projection = _probeSelection.Projection;
            var boundary = new SingleCastProbeBoundary(_probeAllowance,
                () => new InstantCastExecutor(new KingmakerInstantCastAdapter(_log.Info), true),
                MeasureProbeRuntimeIdentity);
            CastingDispatchOutcome outcome = _probeOwner.Submit(boundary,
                new SingleCastProbeObservationSession(new KingmakerProbeObserver(), _probeClock, _probeStep),
                _probeSelection.Plan, _probeSelection.Decision, projection,
                _workspaceCaptureElapsed.ElapsedMilliseconds);
            _log.Info("[KBP-PROBE] owner submit;submitted=" + outcome.Submitted +
                ";reason=" + outcome.Reason + ";world=" + state + ".");
            if (!outcome.Submitted)
            {
                _probeOwner.Terminate("completed");
                _liveUiPhase = 42;
                return false;
            }
            _liveUiPhase = 41;
            return false;
        }

        private SingleCastProbeAllowance _probeAllowance;
        private CastStep _probeStep;
        private int _probeWorldWaitFrames;
        private long _probeWorldWaitStartedMillis;

        // The native step and its confirmation frames advance only while the
        // world runs (review P2-1); the deadline is wall-clock time.
        private bool UpdateProbeRun()
        {
            bool stop = File.Exists(Path.Combine(_request.EvidenceDirectory, "probe-stop.json"));
            string state;
            bool running = ProbeWorldRuns(out state);
            if (_probeOwner.Pump(_workspaceCaptureElapsed.ElapsedMilliseconds,
                    RuntimeTestProtocol.ProbeRunDeadlineSeconds * 1000L, stop, running, state))
                _liveUiPhase = 42;
            return false;
        }

        private bool CompleteProbe()
        {
            if (_probeOwner != null) _probeOwner.Terminate("completed");
            _liveInitialCatalogEvidence = "probe-scenario;workspaceRoot=active;legacyScreen=closed";
            _workspaceInteractionEvidence = "probe;no-authoring";
            _workspaceReopenEvidence = "probe;no-reopen-claim";
            _completed = true;
            return true;
        }

        // Review M3: every non-normal end (host exception, mod disable,
        // unload) reaches the SAME idempotent owner terminal.
        internal void Shutdown(string reason)
        {
            if (_shutdownReason == null)
                _shutdownReason = string.IsNullOrEmpty(reason) ? "unspecified" : reason;
            if (_probeOwner != null) _probeOwner.Terminate(reason);
            // The qualification run ends through the same host terminal (the
            // in-flight executor restores temporary native state).
            if (_qualificationDriver != null) _qualificationDriver.Terminate(reason);
            if (_qualificationHost != null) _qualificationHost.Shutdown(reason);
            if (RuntimeTestProtocol.IsPhysicalWorkspaceScenario(_request.Scenario) && _physicalClock != null &&
                !_physicalPublished)
            {
                _physicalRecord.Failures.Add("shutdown:" + reason);
                try { PublishPhysicalRecord(); }
                catch (Exception exception)
                {
                    _log.Error("[KBP-PHYSICAL] record not published at shutdown.", exception);
                }
            }
            if (RuntimeTestProtocol.IsClassicCastScenario(_request.Scenario) && _classicStage != 0 &&
                !_classicPublished)
            {
                _classicRecord.Failures.Add("shutdown:" + reason);
                try { EndClassicScenario("shutdown:" + reason); }
                catch (Exception exception) { _log.Error("[KBP-CLASSIC] run not ended at shutdown.", exception); }
                try { PublishClassicRecord(); }
                catch (Exception exception)
                {
                    _log.Error("[KBP-CLASSIC] record not published at shutdown.", exception);
                }
            }
            // Review P3-6: a run ended by shutdown still leaves its record.
            if (_qualificationDriver != null && !_qualificationPublished)
            {
                try { PublishQualificationRecord(); }
                catch (Exception exception)
                {
                    _log.Error("[KBP-QUAL] record not published at shutdown.", exception);
                }
            }
        }

        private string _shutdownReason;

        // Review N1: measured at submit time from the LOADED code, never from
        // the request alone: compiled-in commit, the launcher-verified
        // package, the loaded DLL file hash and the loaded module MVID.
        private SingleCastProbeRuntimeIdentity MeasureProbeRuntimeIdentity()
        {
            Assembly assembly = typeof(Main).Assembly;
            return new SingleCastProbeRuntimeIdentity(BuildInfo.Commit,
                _request.ExpectedPackageSha256, Hashing.Sha256(assembly.Location),
                assembly.ManifestModule.ModuleVersionId.ToString("D"));
        }

        private ProbeWorkspaceCloseResult CloseProbeWorkspace()
        {
            string failure = null;
            try { BuffPlannerUiRoot.CloseCastingWorkspaceForRuntime(); }
            catch (Exception exception)
            {
                failure = exception.GetType().Name + ":" + exception.Message;
                _log.Error("[KBP-PROBE] production close failed.", exception);
            }
            return new ProbeWorkspaceCloseResult(!BuffPlannerUiRoot.IsCastingWorkspaceOpen,
                !BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime, failure);
        }

        private void PublishProbeRecord(SingleCastProbeRunRecord record)
        {
            ExplicitCastingRunOutcome outcome = record.Outcome;
            SingleCastProbeObservationSession observation = record.Observation;
            AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "probe-outcome.json"),
                new JObject
                {
                    { "schemaVersion", 2 },
                    { "runId", _request.RunId },
                    { "castingScenario", record.CastingScenario },
                    { "terminalReason", record.TerminalReason },
                    { "allowanceStatus", record.AllowanceStatus },
                    { "boundaryConstructed", record.BoundaryConstructed },
                    { "submitted", record.Submitted },
                    { "submitReason", record.SubmitReason },
                    { "boundaryDisposed", record.BoundaryDisposed },
                    { "measuredIdentity", record.MeasuredIdentity },
                    { "worldRunningAtSubmit", record.WorldRunningAtSubmit },
                    { "submitWorldState", record.SubmitWorldState },
                    { "firstStepWorldState", record.FirstStepWorldState },
                    { "heldPumps", record.HeldPumps },
                    { "invocation", new JObject
                        {
                            { "outcomeProjectionId", outcome == null ? null : outcome.ProjectionId },
                            { "allConfirmed", outcome != null && outcome.AllConfirmed },
                            { "cancelled", outcome != null && outcome.Cancelled },
                            { "haltReason", outcome == null ? null : outcome.HaltReason },
                            { "entries", outcome == null ? new JArray() : new JArray(outcome.Entries.Select(entry =>
                                (object)new JObject
                                {
                                    { "castingId", entry.CastingId },
                                    { "processed", entry.Processed },
                                    { "nativeSubmissionReported", entry.NativeSubmissionReported },
                                    { "confirmed", entry.Confirmed },
                                    { "finalStatus", entry.FinalStatus },
                                    { "detail", entry.Detail }
                                }).ToArray()) }
                        } },
                    { "observation", observation == null ? null : new JObject
                        {
                            { "before", observation.Before == null ? null : observation.Before.Describe() },
                            { "submissionSequence", observation.SubmissionSequence },
                            { "after", observation.After == null ? null : observation.After.Describe() },
                            { "effectOutcome", observation.EffectOutcome },
                            { "resourceOutcome", observation.ResourceOutcome }
                        } },
                    { "cleanup", new JObject
                        {
                            { "recorded", record.CleanupRecorded },
                            { "workspaceClosed", record.WorkspaceClosed },
                            { "inputLeaseReleased", record.InputLeaseReleased },
                            { "failures", new JArray(record.CleanupFailures.Cast<object>().ToArray()) }
                        } },
                    { "violations", new JArray(record.Violations().Cast<object>().ToArray()) }
                }.ToString(Formatting.Indented) + Environment.NewLine);
            _log.Info("[KBP-PROBE] terminal;reason=" + record.TerminalReason +
                ";violations=" + record.Violations().Count + ".");
        }

        private bool UpdateWorkspaceInteraction()
        {
            UI.CastingWorkspaceSession session =
                BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
            if (session == null)
            {
                _workspaceInteraction.AddNote("session-missing");
                SyncInteractionEvidence();
                TransitionToBisectionClose();
                return false;
            }
            try
            {
                CastingWorkspaceInputs currentInputs =
                    BuffPlannerUiRoot.CastingWorkspaceInputsForRuntime();
                if (_workspaceInteractionStep == 0)
                {
                    // The casting graph (addendum v1.1): a single-target buff
                    // two casters can give; the buff is then selected through
                    // its real catalogue tile.
                    string shownSource = session.SelectedSourceId;
                    int probed;
                    string chosenSource = ChooseInteractionSource(session, currentInputs, out probed);
                    if (!string.IsNullOrEmpty(shownSource)) session.SelectGraphBuff(shownSource, currentInputs);
                    RefreshWorkspaceForRuntime();
                    _interactionSourceId = chosenSource ?? shownSource;
                    _workspaceInteraction.AddNote("interactionSource=" + (chosenSource ?? "none-suitable") +
                        ";probed=" + probed);
                    // Full canonical signature before/after browsing.
                    string beforeBrowse = session.DocumentIntentSignature();
                    string buffControl = Invoke("Source." + _interactionSourceId);
                    UI.CastingGraphView graph = session.BuildGraph(currentInputs);
                    _interactionCasters.Clear();
                    _interactionTargets.Clear();
                    _interactionCasters.AddRange(GraphCapableCasters(graph));
                    foreach (UI.CastingGraphTargetNode target in graph.Targets)
                        _interactionTargets.Add(target.UnitId);
                    _workspaceInteraction.AddNote("buffSelected=" + string.Equals(session.SelectedSourceId,
                        _interactionSourceId, StringComparison.Ordinal));
                    // The source-type tabs are view-only: Spells, then back
                    // to All, inside the same no-mutation check.
                    string spellsTab = Invoke("SourceTab.Spells");
                    string allTab = Invoke("SourceTab.All");
                    bool browseClean = string.Equals(
                        session.DocumentIntentSignature(), beforeBrowse,
                        StringComparison.Ordinal);
                    _workspaceInteraction.SourceTabsControl =
                        spellsTab != WorkspaceControlOutcome.Invoked ? spellsTab : allTab;
                    _workspaceInteraction.SourceTabsClean = browseClean;
                    _workspaceInteraction.RecordBrowse(browseClean, buffControl,
                        _interactionCasters.Count, _interactionTargets.Count);
                    SyncInteractionEvidence();
                    session.SelectRoutine(graph.Routines.Count == 0
                        ? "long" : graph.Routines[0].RoutineId);
                    RefreshWorkspaceForRuntime();
                    BeginWorkspaceCameraCapture("ws-interact-browse.png", false);
                    _workspaceInteractionStep = 1;
                    return false;
                }
                if (_interactionCasters.Count < 2 || _interactionTargets.Count < 3)
                {
                    _workspaceInteraction.AddNote("insufficient-party");
                    SyncInteractionEvidence();
                    TransitionToBisectionClose();
                    return false;
                }
                if (_workspaceInteractionStep >= 1 && _workspaceInteractionStep <= 3)
                {
                    int index = _workspaceInteractionStep - 1;
                    string caster = _interactionCasters[index == 1 ? 1 : 0];
                    int beforeCount = session.Document.Castings.Count;
                    string siblingsBefore =
                        WorkspaceCastStepEvaluator.SiblingSignature(
                            session.Document.Castings, null);
                    // The graph gesture: the caster, its exact source row, then
                    // the target whose click adds the casting.
                    string casterClick = Invoke("Caster." + caster);
                    int row = UsableSourceRow(session, currentInputs, caster);
                    string sourceClick = row < 0 ? "control-missing:Provider." + caster + ".usable"
                        : Invoke("Provider." + caster + "." + row);
                    // A legal recipient for THIS source that no casting of the
                    // buff in this routine has yet (the graph shows, never
                    // steals, an assigned target), and never the caster itself
                    // (Aid Another cannot target its caster).
                    string target = GraphInteractionTarget(session, currentInputs, caster);
                    _interactionCastTargets.Add(target);
                    // Expected record from the POST-selection draft (review
                    // H4): the exact chosen source's ability and spellbook.
                    var expected = new WorkspaceCastExpectation
                    {
                        CasterUnitId = caster,
                        DirectTargetUnitId = target,
                        SourceId = _interactionSourceId,
                        Ability = session.Draft.Ability,
                        SpellbookGuid = string.IsNullOrEmpty(session.Draft.SpellbookGuid)
                            ? null : session.Draft.SpellbookGuid,
                        RoutineId = session.SelectedRoutineId
                    };
                    string addClick = Invoke("Target." + target);
                    var castings = session.Document.Castings;
                    Domain.Authoring.PlannedCasting created;
                    WorkspaceCastStepEvidence step =
                        _workspaceInteraction.RecordCastStep(
                            WorkspaceCastStepEvaluator.Evaluate(index + 1,
                                casterClick, sourceClick, WorkspaceControlOutcome.AlreadyReady,
                                addClick, castings.ToList(), beforeCount,
                                _interactionCastIds, expected, siblingsBefore,
                                out created));
                    if (created != null)
                        _interactionCastIds.Add(created.CastingId);
                    SyncInteractionEvidence();
                    if (!step.Exact)
                    {
                        // A refused or wrong Add fails the sequence here —
                        // no recycled last-record id can satisfy it.
                        _workspaceInteraction.AddNote("cast" + (index + 1) +
                            "RefusalDetail=count" + castings.Count +
                            ";created=" + (created == null ? "none"
                                : created.CastingId + "/" + created.CasterUnitId +
                                "/" + created.DirectTargetUnitId));
                        SyncInteractionEvidence();
                        TransitionToBisectionClose();
                        return false;
                    }
                    _workspaceInteractionStep++;
                    return false;
                }
                if (_workspaceInteractionStep == 4)
                {
                    // Negative case: with NO caster chosen, a target click must
                    // be refused and leave the document untouched (review G4;
                    // the graph's analogue of an Add with nothing chosen).
                    session.SelectCaster(null);
                    session.Draft.CasterUnitId = null;
                    session.Draft.Ability = null;
                    session.Draft.SpellbookGuid = null;
                    RefreshWorkspaceForRuntime();
                    string signatureBefore = session.DocumentIntentSignature();
                    int countBefore = session.Document.Castings.Count;
                    string refusedAdd = Invoke("Target." + _interactionTargets[0]);
                    bool refusedClean =
                        session.Document.Castings.Count == countBefore &&
                        string.Equals(session.DocumentIntentSignature(),
                            signatureBefore, StringComparison.Ordinal);
                    _workspaceInteraction.RefusedAddControl = refusedAdd;
                    _workspaceInteraction.RefusedAddClean = refusedClean;
                    SyncInteractionEvidence();
                    _workspaceInteractionStep = 5;
                    return false;
                }
                if (_workspaceInteractionStep == 5)
                {
                    // A casting's LINE (its wide hit corridor) and its CHIP both
                    // open that casting's inspector (G07).
                    string lineId = _interactionCastIds.Count > 0 ? _interactionCastIds[0] : string.Empty;
                    string lineClick = Invoke("Line." + lineId + ".in");
                    bool lineFocused = string.Equals(session.EditingFocusCastingId, lineId,
                        StringComparison.Ordinal);
                    _workspaceInteraction.AddNote("lineControl=" + lineClick + ";lineFocused=" + lineFocused);
                    string editId = _interactionCastIds.Count > 1
                        ? _interactionCastIds[1] : string.Empty;
                    string editClick = Invoke("Casting." + editId);
                    Domain.Authoring.PlannedCasting focused =
                        session.Document.Castings.FirstOrDefault(casting =>
                            casting != null && string.Equals(casting.CastingId,
                                editId, StringComparison.Ordinal));
                    bool editFocused = focused != null && lineFocused &&
                        string.Equals(session.EditingFocusCastingId, editId,
                            StringComparison.Ordinal);
                    _workspaceInteraction.EditControl = editClick == WorkspaceControlOutcome.Invoked &&
                        lineClick != WorkspaceControlOutcome.Invoked ? "line:" + lineClick : editClick;
                    _workspaceInteraction.EditFocused = editFocused;
                    SyncInteractionEvidence();
                    BeginWorkspaceCameraCapture("ws-interact-authored.png", false);
                    _workspaceInteractionStep = 6;
                    return false;
                }
                if (_workspaceInteractionStep == 6)
                {
                    // Retarget through the FOCUSED inspector's real control.
                    string editId = _interactionCastIds.Count > 1
                        ? _interactionCastIds[1] : string.Empty;
                    // Another legal recipient for cast 2's source: neither the
                    // caster nor its current recipient; the re-edit reuses it.
                    _interactionRetarget = GraphRetarget(session, currentInputs, editId);
                    string newTarget = _interactionRetarget;
                    // Baseline BEFORE the edit: Undo must restore exactly
                    // this, so it is captured before the retarget control
                    // fires, never after.
                    _workspaceIntentBeforeEdit =
                        session.DocumentIntentSignature();
                    string retargetClick = InvokeRetarget(newTarget);
                    Domain.Authoring.PlannedCasting focused =
                        session.Document.Castings.FirstOrDefault(casting =>
                            casting != null && string.Equals(casting.CastingId,
                                editId, StringComparison.Ordinal));
                    bool retargeted = focused != null &&
                        string.Equals(focused.DirectTargetUnitId, newTarget,
                            StringComparison.Ordinal);
                    _workspaceInteraction.RetargetControl = retargetClick;
                    _workspaceInteraction.RetargetApplied = retargeted;
                    SyncInteractionEvidence();
                    _workspaceInteractionStep = 7;
                    return false;
                }
                if (_workspaceInteractionStep == 7)
                {
                    string undoClick = Invoke("Undo");
                    bool intentRestored = string.Equals(
                        session.DocumentIntentSignature(),
                        _workspaceIntentBeforeEdit, StringComparison.Ordinal);
                    _workspaceInteraction.UndoControl = undoClick;
                    _workspaceInteraction.UndoIntentRestored = intentRestored;
                    SyncInteractionEvidence();
                    BeginWorkspaceCameraCapture("ws-interact-undo.png", false);
                    _workspaceInteractionStep = 8;
                    return false;
                }
                if (_workspaceInteractionStep == 8)
                {
                    // Undo restored the pre-edit intent (the edit is gone);
                    // deliberately re-apply it, then Done and Save through
                    // their controls.
                    string editId = _interactionCastIds.Count > 1
                        ? _interactionCastIds[1] : string.Empty;
                    string newTarget = _interactionRetarget ?? GraphRetarget(session, currentInputs, editId);
                    Invoke("Casting." + editId);
                    InvokeRetarget(newTarget);
                    string doneClick = Invoke("DoneEditing");
                    bool doneCleared = session.EditingFocusCastingId == null;
                    string saveClick = Invoke("Save");
                    _workspaceSavedIntentIds = session.DocumentIntentSignature();
                    _workspaceInteraction.DoneControl = doneClick;
                    _workspaceInteraction.DoneClearedFocus = doneCleared;
                    _workspaceInteraction.SaveControl = saveClick;
                    // Saved is observed, not asserted: the session must
                    // report its authored intent clean after the control.
                    _workspaceInteraction.Saved =
                        saveClick == WorkspaceControlOutcome.Invoked &&
                        !session.IsDirty;
                    SyncInteractionEvidence();
                    _log.Info("[KBP-WORKSPACE] interaction authored and saved;" +
                        _workspaceInteractionEvidence + ".");
                    FinishWorkspaceInteraction();
                    return false;
                }
            }
            catch (Exception exception)
            {
                _workspaceInteraction.AddNote("error=" + exception.GetType().Name +
                    ":" + exception.Message);
                SyncInteractionEvidence();
                _log.Error("[KBP-WORKSPACE] interaction step failed.", exception);
                TransitionToBisectionClose();
                return false;
            }
            TransitionToBisectionClose();
            return false;
        }

        private void SyncInteractionEvidence()
        {
            _workspaceInteractionEvidence = _workspaceInteraction.Describe();
        }

        // ------------------------------------------------------------------
        // Pointer-highlight diagnostic (live-workspace-qual only; addendum
        // v1.1: exactly one highlight, on the control under the pointer).
        // Unity's own control states are read (PlannerHoverProbe) while the
        // real cursor is parked on a neutral point or swept over controls;
        // the owner-reported rc6 ghost is reproduced on the Classic screen
        // with only PlannerUiReproduction switched, then the shipped
        // behaviour is measured on the same binary. Hover moves only: the
        // sweep never clicks.
        // ------------------------------------------------------------------

        private GameObject HoverWorkspaceRoot()
        {
            UI.CastingWorkspaceScreenView view = BuffPlannerUiRoot.CastingWorkspaceViewForRuntime;
            return view == null ? null : view.RootObject;
        }

        private void RequestHover(string id, Vector2 point, string aimed, string surface, string behaviour,
            bool sample, int after)
        {
            WritePhysicalInputRequest(id, "hover", point);
            _hoverPending = id;
            _hoverPoint = point;
            _hoverAimed = aimed;
            _hoverPendingSurface = surface;
            _hoverPendingBehaviour = behaviour;
            _hoverPendingSample = sample;
            _hoverAfter = after;
            _hoverFrames = 0;
            _hoverClock = System.Diagnostics.Stopwatch.StartNew();
        }

        // The pending physical move: acknowledged, then three frames for the
        // input module to process the cursor, then Unity's reading there.
        private bool UpdateHoverPending()
        {
            if (!PhysicalInputAcknowledged(_hoverPending))
            {
                if (_hoverClock.Elapsed.TotalSeconds > 45)
                {
                    _hover.AddFailure("physical-timeout:" + _hoverPending);
                    _hoverPending = null;
                    _hoverStep = _hoverAfter;
                }
                return false;
            }
            JObject ack = JObject.Parse(File.ReadAllText(Path.Combine(_request.EvidenceDirectory,
                "physical-input-" + _hoverPending + ".ack.json")));
            if (ack.Value<bool?>("deliveryFailed") == true)
            {
                _hover.AddFailure("physical-delivery-failed:" + _hoverPending + ":" + ack.Value<string>("error"));
                _hoverPending = null;
                _hoverStep = _hoverAfter;
                return false;
            }
            if (_hoverFrames < 3)
            {
                _hoverFrames++;
                return false;
            }
            if (_hoverPendingSample)
            {
                GameObject root = _hoverPendingSurface == "classic"
                    ? BuffPlannerUiRoot.ClassicRootForRuntime : HoverWorkspaceRoot();
                Vector2 mouse = Input.mousePosition;
                bool anyHit;
                UnityEngine.UI.Selectable top = PlannerHoverProbe.TopControlAt(mouse, out anyHit);
                bool topUnder = top != null && PlannerHoverProbe.IsUnder(top.gameObject, new[] { root });
                string expected = topUnder && PlannerHoverProbe.DrawsHighlight(top) ? top.name : null;
                List<UnityEngine.UI.Selectable> owners = PlannerHoverProbe.Controls(root)
                    .Where(control => PlannerHoverProbe.StateOf(control) == "Highlighted" &&
                        PlannerHoverProbe.DrawsHighlight(control)).ToList();
                bool? inside = owners.Count == 1 ? PlannerHoverProbe.Contains(owners[0], mouse) : (bool?)null;
                _hover.Add(PlannerHoverProbe.Sample(_hoverPending, _hoverPendingSurface, _hoverPendingBehaviour,
                    true, _hoverAimed, null, expected, topUnder ? top.name : null, inside,
                    "requested=" + PlannerHoverProbe.Point(_hoverPoint) + ";unityCursor=" +
                    PlannerHoverProbe.Point(mouse) + ";client=" + ack.Value<string>("windowsClientCursor") +
                    ";anyHit=" + anyHit, root));
            }
            _hoverPending = null;
            _hoverStep = _hoverAfter;
            return false;
        }

        private void BeginHoverCapture(string fileName)
        {
            _hoverCapture = null;
            MenuDiagnosticCaptureHost.CaptureMenuFrameThroughCameras(
                Path.Combine(_request.EvidenceDirectory, fileName),
                delegate(MenuFrameCapture capture, Exception failure)
                {
                    capture.Failure = failure;
                    _hoverCapture = capture;
                }, _log);
            _hoverCaptureName = fileName;
            _hoverClock = System.Diagnostics.Stopwatch.StartNew();
        }

        // True once the named capture completed (a failure is recorded, not
        // thrown: the frame is evidence for the owner, not the verdict).
        private bool HoverCaptureDone()
        {
            if (_hoverCapture == null || !string.Equals(_hoverCapture.FileName, _hoverCaptureName,
                    StringComparison.OrdinalIgnoreCase))
            {
                if (_hoverClock.Elapsed.TotalSeconds <= 30) return false;
                _hover.AddFailure("capture-timeout:" + _hoverCaptureName);
                return true;
            }
            if (_hoverCapture.Failure != null)
                _hover.AddFailure("capture-failed:" + _hoverCaptureName + ":" + _hoverCapture.Failure.Message);
            return true;
        }

        private void HoverSynthetic(string label, string surface, string behaviour, string aimed, string clicked,
            GameObject root)
        {
            _hover.Add(PlannerHoverProbe.Sample(label, surface, behaviour, false, aimed, clicked, aimed, null,
                null, string.Empty, root));
        }

        private bool UpdateHoverOwnership()
        {
            try
            {
                if (_hoverPending != null) return UpdateHoverPending();
                GameObject root = HoverWorkspaceRoot();
                GameObject classic = BuffPlannerUiRoot.ClassicRootForRuntime;
                if (_hoverStep == 0)
                {
                    _hoverStarted = true;
                    _hover.ProbeAvailable = PlannerHoverProbe.Available;
                    _hover.Screen = UnityEngine.Screen.width + "x" + UnityEngine.Screen.height;
                    Vector2? neutral = PlannerHoverProbe.NeutralPoint(root);
                    if (!_hover.ProbeAvailable || !neutral.HasValue)
                    {
                        _hover.AddFailure(!_hover.ProbeAvailable ? "probe-members-missing" : "no-neutral-point:graph");
                        return FinishHover();
                    }
                    _hoverGraphNeutral = neutral.Value;
                    RequestHover("hover-graph-neutral", neutral.Value, null, "graph", HoverBehaviour.Fixed, true, 1);
                    return false;
                }
                if (_hoverStep == 1)
                {
                    // A click on a view-only control, then hovers: the click
                    // must not keep the control lit (the owner's ghost).
                    UnityEngine.UI.Selectable tab = PlannerHoverProbe.Find(root, "SourceTab.All");
                    bool took = PlannerHoverProbe.Click(tab);
                    _hover.AddNote("graphClick=" + (tab == null ? "missing" : tab.name) + ";tookSelection=" + took);
                    string[] names =
                    {
                        "Target." + _interactionTargets.FirstOrDefault(),
                        "Caster." + _interactionCasters.FirstOrDefault(),
                        "Casting." + _interactionCastIds.FirstOrDefault()
                    };
                    for (int index = 0; index < names.Length; index++)
                    {
                        UnityEngine.UI.Selectable control = PlannerHoverProbe.Find(root, names[index]);
                        if (control == null)
                        {
                            _hover.AddFailure("graph-control-missing:" + names[index]);
                            continue;
                        }
                        PlannerHoverProbe.Enter(control);
                        HoverSynthetic(index == 0 ? "graph-click-then-hover" : "graph-hover-" + index, "graph",
                            HoverBehaviour.Fixed, control.name, index == 0 && tab != null ? tab.name : null, root);
                        PlannerHoverProbe.Exit(control);
                        HoverSynthetic("graph-exit-" + index, "graph", HoverBehaviour.Fixed, null, null, root);
                    }
                    // One hovered node held for the frame the owner will see.
                    _hoverHeld = names[0];
                    PlannerHoverProbe.Enter(PlannerHoverProbe.Find(root, _hoverHeld));
                    BeginHoverCapture("hover-graph-fixed.png");
                    _hoverStep = 2;
                    return false;
                }
                if (_hoverStep == 2)
                {
                    if (!HoverCaptureDone()) return false;
                    HoverSynthetic("graph-hover-captured", "graph", HoverBehaviour.Fixed, _hoverHeld, null, root);
                    PlannerHoverProbe.Exit(PlannerHoverProbe.Find(root, _hoverHeld));
                    // Five button states in one frame. The first casting's
                    // inspector (focus only) gives a naturally disabled
                    // control: it cannot move earlier than first.
                    Invoke("Casting." + _interactionCastIds.FirstOrDefault());
                    UnityEngine.UI.Selectable disabled = PlannerHoverProbe.Find(root, "FocusedOrder.Earlier");
                    if (disabled == null || disabled.interactable)
                        disabled = PlannerHoverProbe.Controls(root).FirstOrDefault(control =>
                            control is UnityEngine.UI.Button && !control.interactable);
                    UI.CastingWorkspaceSession session = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                    _hoverButtons = new[]
                    {
                        "Reload", "Save", "ExecutionMode",
                        "Routine." + (session == null ? "long" : session.SelectedRoutineId),
                        disabled == null ? "none" : disabled.name
                    };
                    PlannerHoverProbe.Enter(PlannerHoverProbe.Find(root, _hoverButtons[1]));
                    PlannerHoverProbe.Press(PlannerHoverProbe.Find(root, _hoverButtons[2]));
                    BeginHoverCapture("button-states.png");
                    _hoverStep = 3;
                    return false;
                }
                if (_hoverStep == 3)
                {
                    if (!HoverCaptureDone()) return false;
                    for (int index = 0; index < HoverOwnershipRecord.RequiredButtonStates.Length; index++)
                        _hover.AddButton(PlannerHoverProbe.Observe(HoverOwnershipRecord.RequiredButtonStates[index],
                            PlannerHoverProbe.Find(root, _hoverButtons[index])));
                    // Let go without a click (a click would toggle the mode).
                    PlannerHoverProbe.Release(PlannerHoverProbe.Find(root, _hoverButtons[2]));
                    PlannerHoverProbe.Exit(PlannerHoverProbe.Find(root, _hoverButtons[1]));
                    Invoke("DoneEditing");
                    _hoverSweep.Clear();
                    string caster = _interactionCasters.FirstOrDefault();
                    UI.CastingWorkspaceSession session = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                    _hoverSweep.AddRange(new[]
                    {
                        "Caster." + caster, "Provider." + caster + ".0",
                        "Target." + _interactionTargets.FirstOrDefault(),
                        "Casting." + _interactionCastIds.FirstOrDefault(),
                        "Target." + _interactionTargets.LastOrDefault(),
                        "Caster." + _interactionCasters.LastOrDefault(),
                        "Source." + _interactionSourceId, "Save",
                        "Routine." + (session == null ? "long" : session.SelectedRoutineId)
                    });
                    _hoverSweepIndex = 0;
                    _hoverStep = 4;
                    return false;
                }
                if (_hoverStep == 4)
                {
                    // The live sweep: the real cursor over each control.
                    while (_hoverSweepIndex < _hoverSweep.Count)
                    {
                        string name = _hoverSweep[_hoverSweepIndex++];
                        Vector2? point = PlannerHoverProbe.ScreenCentre(PlannerHoverProbe.Find(root, name));
                        if (!point.HasValue)
                        {
                            _hover.AddNote("sweep-not-shown:" + name);
                            continue;
                        }
                        RequestHover("hover-sweep-" + _hoverSweepIndex, point.Value, name, "graph",
                            HoverBehaviour.Fixed, true, 4);
                        return false;
                    }
                    RequestHover("hover-graph-neutral-end", _hoverGraphNeutral, null, "graph",
                        HoverBehaviour.Fixed, true, 5);
                    return false;
                }
                if (_hoverStep == 5)
                {
                    _hover.GraphSelectablesBeforeClose = PlannerHoverProbe.Controls(root).Count;
                    BuffPlannerUiRoot.CloseCastingWorkspaceForRuntime();
                    _hoverFrames = 0;
                    _hoverStep = 6;
                    return false;
                }
                if (_hoverStep == 6 || _hoverStep == 10)
                {
                    // Workspace (or the rc6 Classic screen) closed: open the
                    // Classic screen with the behaviour under test.
                    if (BuffPlannerUiRoot.IsCastingWorkspaceOpen || BuffPlannerUiRoot.IsScreenOpen ||
                        _hoverFrames++ < 3) return false;
                    PlannerUiReproduction.Rc6ButtonBehaviour = _hoverStep == 6;
                    if (!BuffPlannerUiRoot.OpenClassicForRuntime())
                    {
                        _hover.AddFailure("classic-open-refused:" + (_hoverStep == 6 ? "rc6" : "fixed"));
                        PlannerUiReproduction.Rc6ButtonBehaviour = false;
                        return FinishHover();
                    }
                    _hoverFrames = 0;
                    _hoverClock = System.Diagnostics.Stopwatch.StartNew();
                    _hoverStep = _hoverStep == 6 ? 7 : 11;
                    return false;
                }
                if (_hoverStep == 7 || _hoverStep == 11)
                {
                    bool rc6 = _hoverStep == 7;
                    if (!BuffPlannerUiRoot.IsScreenOpen || classic == null || _hoverFrames++ < 5)
                    {
                        if (_hoverClock.Elapsed.TotalSeconds <= 30) return false;
                        _hover.AddFailure("classic-not-open:" + (rc6 ? "rc6" : "fixed"));
                        PlannerUiReproduction.Rc6ButtonBehaviour = false;
                        return FinishHover();
                    }
                    _hoverClassicRoutine = PlannerHoverProbe.Controls(classic).Where(control =>
                            control.name.StartsWith("Routine.", StringComparison.Ordinal) && control.interactable)
                        .Select(control => control.name).FirstOrDefault();
                    _hoverClassicTarget = PlannerHoverProbe.Controls(classic).Where(control =>
                            control.name.StartsWith("Target.", StringComparison.Ordinal))
                        .Select(control => control.name).FirstOrDefault();
                    if (_hoverClassicTarget == null && !_hoverClassicRowSelected)
                    {
                        // Portraits show for a selected buff: select the
                        // first catalogue row (view state only), then retry.
                        _hoverClassicRowSelected = true;
                        _hover.AddNote("classicFirstRowSelected=" + BuffPlannerUiRoot.SelectFirstRowForRuntime());
                        _hoverFrames = 0;
                        return false;
                    }
                    Vector2? neutral = PlannerHoverProbe.NeutralPoint(classic);
                    if (_hoverClassicRoutine == null || _hoverClassicTarget == null || !neutral.HasValue)
                    {
                        _hover.AddFailure("classic-controls-missing:routine=" + (_hoverClassicRoutine ?? "none") +
                            ";target=" + (_hoverClassicTarget ?? "none") + ";neutral=" + neutral.HasValue);
                        PlannerUiReproduction.Rc6ButtonBehaviour = false;
                        BuffPlannerUiRoot.CloseRuntimeSmoke();
                        return FinishHover();
                    }
                    RequestHover(rc6 ? "hover-classic-rc6-neutral" : "hover-classic-neutral", neutral.Value, null,
                        "classic", rc6 ? HoverBehaviour.Rc6 : HoverBehaviour.Fixed, true, rc6 ? 8 : 12);
                    return false;
                }
                if (_hoverStep == 8 || _hoverStep == 12)
                {
                    // Click the routine tab, then hover a portrait: the rc6
                    // behaviour keeps the tab lit (two highlights); the
                    // shipped one shows only the hovered portrait.
                    bool rc6 = _hoverStep == 8;
                    string behaviour = rc6 ? HoverBehaviour.Rc6 : HoverBehaviour.Fixed;
                    bool took = PlannerHoverProbe.Click(PlannerHoverProbe.Find(classic, _hoverClassicRoutine));
                    // The routine click rebinds the portraits: the hovered
                    // one is chosen from what is shown after it.
                    UnityEngine.UI.Selectable target = PlannerHoverProbe.Controls(classic).FirstOrDefault(control =>
                        control.name.StartsWith("Target.", StringComparison.Ordinal) && control.interactable);
                    _hoverClassicTarget = target == null ? "Target.none" : target.name;
                    _hover.AddNote("classic" + (rc6 ? "Rc6" : "Fixed") + "Click=" + _hoverClassicRoutine +
                        ";tookSelection=" + took + ";hover=" + _hoverClassicTarget);
                    PlannerHoverProbe.Enter(target);
                    HoverSynthetic(rc6 ? "classic-rc6-click-then-hover" : "classic-click-then-hover", "classic",
                        behaviour, _hoverClassicTarget, _hoverClassicRoutine, classic);
                    BeginHoverCapture(rc6 ? "hover-classic-rc6.png" : "hover-classic-fixed.png");
                    _hoverStep = rc6 ? 9 : 13;
                    return false;
                }
                if (_hoverStep == 9 || _hoverStep == 13)
                {
                    if (!HoverCaptureDone()) return false;
                    bool rc6 = _hoverStep == 9;
                    PlannerHoverProbe.Exit(PlannerHoverProbe.Find(classic, _hoverClassicTarget));
                    HoverSynthetic(rc6 ? "classic-rc6-exit" : "classic-exit", "classic",
                        rc6 ? HoverBehaviour.Rc6 : HoverBehaviour.Fixed, null, null, classic);
                    BuffPlannerUiRoot.CloseRuntimeSmoke();
                    PlannerHoverProbe.ClearSelection();
                    PlannerUiReproduction.Rc6ButtonBehaviour = false;
                    _hoverFrames = 0;
                    _hoverStep = rc6 ? 10 : 14;
                    return false;
                }
                if (_hoverStep == 14)
                {
                    if (BuffPlannerUiRoot.IsScreenOpen || _hoverFrames++ < 3) return false;
                    // Park the cursor where the reopened workspace has no
                    // control, then continue into the close/reopen check.
                    RequestHover("hover-park", _hoverGraphNeutral, null, "graph", HoverBehaviour.Fixed, false, 15);
                    return false;
                }
                if (_hoverStep == 15)
                {
                    TransitionToBisectionClose();
                    return false;
                }
                // Phase 145: after the production close and reopen.
                if (_hoverStep == 30)
                {
                    _hover.PlannerRootsAfterReopen = BuffPlannerUiRoot.PlannerRootCountForRuntime();
                    _hover.GraphSelectablesAfterReopen = PlannerHoverProbe.Controls(root).Count;
                    Vector2? neutral = PlannerHoverProbe.NeutralPoint(root);
                    if (!neutral.HasValue)
                    {
                        _hover.AddFailure("no-neutral-point:reopen");
                        _hoverStep = 31;
                        return false;
                    }
                    RequestHover("hover-reopen-neutral", neutral.Value, null, "reopen", HoverBehaviour.Fixed, true, 31);
                    return false;
                }
                if (_hoverStep == 31)
                {
                    string name = "Target." + _interactionTargets.FirstOrDefault();
                    UnityEngine.UI.Selectable target = PlannerHoverProbe.Find(root, name);
                    if (target == null) _hover.AddFailure("reopen-control-missing:" + name);
                    PlannerHoverProbe.Enter(target);
                    HoverSynthetic("reopen-hover", "reopen", HoverBehaviour.Fixed, target == null ? null : name, null,
                        root);
                    PlannerHoverProbe.Exit(target);
                    HoverSynthetic("reopen-exit", "reopen", HoverBehaviour.Fixed, null, null, root);
                    _hover.Completed = true;
                    WriteHoverEvidence();
                    _liveUiPhase = 21;
                    return false;
                }
                _hover.AddFailure("hover-step-unknown:" + _hoverStep);
                return FinishHover();
            }
            catch (Exception exception)
            {
                PlannerUiReproduction.Rc6ButtonBehaviour = false;
                _hover.AddFailure("error=" + exception.GetType().Name + ":" + exception.Message);
                _log.Error("[KBP-HOVER] hover diagnostic step " + _hoverStep + " failed.", exception);
                return FinishHover();
            }
        }

        // An early end: the rc6 switch is off, no Classic screen stays open,
        // the evidence is written, and the standard close/reopen continues
        // (the incomplete record fails its own assertion).
        private bool FinishHover()
        {
            PlannerUiReproduction.Rc6ButtonBehaviour = false;
            _hoverPending = null;
            _hoverReopenChecked = true;
            try
            {
                if (BuffPlannerUiRoot.IsScreenOpen) BuffPlannerUiRoot.CloseRuntimeSmoke();
            }
            catch (Exception exception)
            {
                _hover.AddFailure("classic-close-failed:" + exception.Message);
            }
            WriteHoverEvidence();
            if (_liveUiPhase == 145) _liveUiPhase = 21;
            else TransitionToBisectionClose();
            return false;
        }

        private void WriteHoverEvidence()
        {
            try
            {
                IList<string> violations = _hover.Violations();
                var root = new JObject
                {
                    { "schemaVersion", 1 },
                    { "runId", _request.RunId },
                    { "scenario", _request.Scenario },
                    { "screen", _hover.Screen },
                    { "probeAvailable", _hover.ProbeAvailable },
                    { "ghostReproduced", _hover.GhostReproduced },
                    { "completed", _hover.Completed },
                    { "plannerRootsAfterReopen", _hover.PlannerRootsAfterReopen },
                    { "graphSelectablesBeforeClose", _hover.GraphSelectablesBeforeClose },
                    { "graphSelectablesAfterReopen", _hover.GraphSelectablesAfterReopen },
                    { "samples", new JArray(_hover.Samples.Select(sample => (object)new JObject
                        {
                            { "label", sample.Label },
                            { "surface", sample.Surface },
                            { "behaviour", sample.Behaviour },
                            { "physical", sample.Physical },
                            { "aimed", sample.Aimed },
                            { "clicked", sample.Clicked },
                            { "expectedOwner", sample.ExpectedOwner },
                            { "topControl", sample.TopControl },
                            { "highlighted", new JArray(sample.Highlighted.Cast<object>().ToArray()) },
                            { "pointerInside", new JArray(sample.PointerInside.Cast<object>().ToArray()) },
                            { "selection", sample.Selection },
                            { "selectionIsPlannerControl", sample.SelectionIsPlannerControl },
                            { "cursorInsideOwner", sample.CursorInsideOwner },
                            { "detail", sample.Detail },
                            { "violations", new JArray(sample.Judge().Cast<object>().ToArray()) }
                        }).ToArray()) },
                    { "buttonStates", new JArray(_hover.Buttons.Select(button => (object)new JObject
                        {
                            { "state", button.State },
                            { "control", button.Control },
                            { "unityState", button.UnityState },
                            { "selectedPalette", button.SelectedPalette },
                            { "tint", button.Tint },
                            { "screenRect", button.ScreenRect },
                            { "shown", button.Shown }
                        }).ToArray()) },
                    { "notes", new JArray(_hover.Notes.Cast<object>().ToArray()) },
                    { "failures", new JArray(_hover.Failures.Cast<object>().ToArray()) },
                    { "violations", new JArray(violations.Cast<object>().ToArray()) }
                };
                AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "hover-ownership.json"),
                    root.ToString(Formatting.Indented) + Environment.NewLine);
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-HOVER] hover evidence could not be written.", exception);
            }
        }

        private static string Invoke(string buttonName)
        {
            return BuffPlannerUiRoot.CastingWorkspaceInvokeControlForRuntime(
                buttonName);
        }

        private void TransitionToBisectionClose()
        {
            BuffPlannerUiRoot.CloseCastingWorkspaceForRuntime();
            _workspaceClosedWaitUpdates = 0;
            _log.Info("[KBP-WORKSPACE] workspace closed for bisection capture;interactions=" +
                _workspaceInteractionEvidence + ".");
            _liveUiPhase = 19;
        }

        // Mission section 8: after an in-game reload the planner shows the
        // saved plan for the same campaign, with one live event subscription,
        // one HUD root, no casting run and a clean session.
        private string VerifyWorkspaceReload()
        {
            UI.CastingWorkspaceSession session =
                BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
            object rawGame;
            string expectedGame = _request.Parameters.TryGetValue("expectedGameId", out rawGame)
                ? rawGame as string : null;
            bool preserved = session != null && session.Document != null &&
                !string.IsNullOrEmpty(_workspaceSavedIntentIds) &&
                string.Equals(session.DocumentIntentSignature(), _workspaceSavedIntentIds,
                    StringComparison.Ordinal);
            // Review of 1332ed8..542cd66, P2-1: what a fresh session would
            // read from disk must equal what was saved (the same-campaign
            // session object is retained, so comparing it with itself proves
            // nothing about the file).
            string diskStatus = "no-campaign";
            string disk = string.IsNullOrEmpty(expectedGame) ? null
                : UI.CastingWorkspaceSession.SavedIntentSignature(_modEntry.Path, expectedGame, out diskStatus);
            bool diskPreserved = disk != null && string.Equals(disk, _workspaceSavedIntentIds, StringComparison.Ordinal);
            bool campaign = session != null && !string.IsNullOrEmpty(expectedGame) &&
                string.Equals(session.CampaignId, expectedGame, StringComparison.Ordinal);
            int subscriptions = BuffPlannerUiRoot.ActiveEventSubscriptionsForRuntime;
            int hudRoots = BuffPlannerUiRoot.HudRootCountForRuntime;
            // One delivery of each single-shot area event for one load: a
            // duplicate EventBus registration would deliver them twice.
            int unloads = BuffPlannerUiRoot.LifecycleSignalsForRuntime("OnAreaBeginUnloading") - _reloadUnloadsBefore;
            int completes = BuffPlannerUiRoot.LifecycleSignalsForRuntime("OnAreaLoadingComplete") - _reloadCompleteBefore;
            int activations = BuffPlannerUiRoot.LifecycleSignalsForRuntime("OnAreaActivated") - _reloadActivatedBefore;
            bool idle = !BuffPlannerUiRoot.IsCastingRunActive &&
                BuffPlannerUiRoot.CastingRunsStartedForRuntime == _reloadRunsBefore;
            bool clean = session != null && !session.IsDirty;
            bool passed = preserved && diskPreserved && campaign && _reloadSubscriptionsBefore == 1 &&
                subscriptions == 1 && _reloadHudRootsBefore == 1 && hudRoots == 1 &&
                unloads == 1 && completes == 1 && idle && clean;
            return "passed=" + passed + ";preserved=" + preserved + ";diskPreserved=" + diskPreserved +
                ";diskStatus=" + diskStatus + ";campaign=" + campaign +
                ";subscriptions=" + _reloadSubscriptionsBefore + "->" + subscriptions +
                ";hudRoots=" + _reloadHudRootsBefore + "->" + hudRoots +
                ";areaEvents=unload+" + unloads + ",complete+" + completes + ",activated+" + activations +
                ";sessionReused=" + ReferenceEquals(session, _reloadSessionBefore) + ";idle=" + idle +
                ";dirty=" + (session == null ? "no-session" : session.IsDirty.ToString()) +
                ";loadStatus=" + (session == null ? "none" : session.LoadStatus.ToString()) +
                ";loaded=" + (_reloadLoadedEvidence ?? "missing");
        }

        private string VerifyWorkspaceReopen()
        {
            UI.CastingWorkspaceSession session =
                BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
            if (session == null || session.Document == null)
                return "session-missing";
            // Full-intent comparison (review F2): caster, ability, target,
            // routine, enhancements, state, and coverage must all survive,
            // and the preserved session must report clean (not dirty).
            string signature = session.DocumentIntentSignature();
            bool preserved = !string.IsNullOrEmpty(_workspaceSavedIntentIds) &&
                string.Equals(signature, _workspaceSavedIntentIds,
                    StringComparison.Ordinal);
            return "preserved=" + preserved + ";dirty=" + session.IsDirty +
                ";loadStatus=" + session.LoadStatus + ";castings=" +
                session.Document.Castings.Count;
        }

        private void WriteWorkspaceRenderMarker(string lumaEvidence)
        {
            string path = Path.Combine(_request.EvidenceDirectory, "workspace-render.json");
            string json = "{\"schemaVersion\":1,\"runId\":" + JsonConvert.ToString(_request.RunId) +
                ",\"scenario\":" + JsonConvert.ToString(_request.Scenario) +
                ",\"stage\":\"workspace-frame-captured\"" +
                ",\"environment\":" + JsonConvert.ToString(
                    MenuRenderDiagnostic.EnvironmentSample()) +
                ",\"presentation\":" + JsonConvert.ToString(
                    _workspacePresentationEvidence ?? string.Empty) +
                ",\"controlLuma\":" + JsonConvert.ToString(_workspaceControlLuma ?? string.Empty) +
                ",\"controlSha256\":" + JsonConvert.ToString(
                    _workspaceControlSha256 ?? string.Empty) +
                ",\"changedFraction\":" + _workspaceChangedFraction.ToString(
                    "F5", System.Globalization.CultureInfo.InvariantCulture) +
                ",\"closedLuma\":" + JsonConvert.ToString(_workspaceClosedLuma ?? string.Empty) +
                ",\"cameraOpen\":" + JsonConvert.ToString(_workspaceCameraOpenLuma) +
                ",\"cameraControl\":" + JsonConvert.ToString(_workspaceCameraControlLuma) +
                ",\"closedSha256\":" + JsonConvert.ToString(
                    _workspaceClosedScreenshotSha256 ?? string.Empty) +
                ",\"readPixelsSha256\":" + JsonConvert.ToString(
                    _liveRenderScreenshotSha256 ?? string.Empty) +
                ",\"engineSha256\":" + JsonConvert.ToString(
                    _workspaceEngineScreenshotSha256 ?? string.Empty) +
                ",\"luma\":" + JsonConvert.ToString(lumaEvidence ?? string.Empty) +
                ",\"blackRecaptures\":" + _workspaceBlackAttempts + "}";
            AtomicFile.WriteUtf8(path, json + Environment.NewLine);
        }

        private static bool TryHashScreenshot(string path, out string sha256)
        {
            sha256 = string.Empty;
            if (string.IsNullOrEmpty(path) || !File.Exists(path) ||
                new FileInfo(path).Length < 1000) return false;
            sha256 = Hashing.Sha256(path);
            return true;
        }

        private bool PhysicalInputAcknowledged(string id)
        {
            return File.Exists(Path.Combine(_request.EvidenceDirectory,
                "physical-input-" + id + ".ack.json"));
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
}
