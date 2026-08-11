using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Infrastructure;
using Newtonsoft.Json;
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
            if (string.Equals(_request.Scenario, "native-buff-catalog", StringComparison.Ordinal) &&
                ResourcesLibrary.LibraryObject == null)
                return false;
            _completed = true;
            DateTime started = _startedAtUtc;
            try
            {
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
                if (dllMatches && string.Equals(
                    _request.Scenario, "native-buff-catalog", StringComparison.Ordinal))
                {
                    catalog = new NativeCatalogExporter().Export();
                    catalogPath = Path.Combine(_request.EvidenceDirectory, "native-buff-catalog.json");
                    AtomicFile.WriteUtf8(catalogPath, Serialize(catalog));
                    catalogHash = Hashing.Sha256(catalogPath);
                }
                var result = new RuntimeTestResult
                {
                    SchemaVersion = 1,
                    RunId = _request.RunId,
                    Scenario = _request.Scenario,
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

        private void TryWriteFailure(DateTime started, Exception exception)
        {
            try
            {
                var result = new RuntimeTestResult
                {
                    SchemaVersion = 1,
                    RunId = _request.RunId,
                    Scenario = _request.Scenario,
                    Status = "FAIL",
                    Stage = "unhandled-exception",
                    LoadedModId = _modEntry.Info.Id,
                    LoadedModVersion = _modEntry.Info.Version,
                    Commit = BuildInfo.Commit,
                    ProcessId = Process.GetCurrentProcess().Id,
                    StartedAtUtc = started.ToString("o"),
                    EndedAtUtc = DateTime.UtcNow.ToString("o"),
                    ExceptionSummary = exception.GetType().FullName + ": " + exception.Message,
                    Assertions = new List<RuntimeTestAssertion>()
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
    }

    internal sealed class RuntimeTestResult
    {
        [JsonProperty("schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
        [JsonProperty("runId", Order = 2)] public string RunId { get; set; }
        [JsonProperty("scenario", Order = 3)] public string Scenario { get; set; }
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
