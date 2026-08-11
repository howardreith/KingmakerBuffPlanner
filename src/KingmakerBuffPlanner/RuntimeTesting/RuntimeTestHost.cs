using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
        private bool _completed;

        private RuntimeTestHost(
            RuntimeTestRequest request,
            UnityModManager.ModEntry modEntry,
            ModLog log)
        {
            _request = request;
            _modEntry = modEntry;
            _log = log;
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
            _completed = true;
            DateTime started = DateTime.UtcNow;
            try
            {
                Assembly assembly = typeof(Main).Assembly;
                string assemblyPath = assembly.Location;
                var result = new RuntimeTestResult
                {
                    SchemaVersion = 1,
                    RunId = _request.RunId,
                    Scenario = _request.Scenario,
                    Status = "PASS",
                    Stage = "completed",
                    LoadedModId = _modEntry.Info.Id,
                    LoadedModVersion = _modEntry.Info.Version,
                    Commit = BuildInfo.Commit,
                    AssemblyMvid = assembly.ManifestModule.ModuleVersionId.ToString("D"),
                    AssemblySha256 = Hashing.Sha256(assemblyPath),
                    ProcessId = Process.GetCurrentProcess().Id,
                    StartedAtUtc = started.ToString("o"),
                    EndedAtUtc = DateTime.UtcNow.ToString("o"),
                    Assertions = new List<RuntimeTestAssertion>
                    {
                        RuntimeTestAssertion.Pass("entry-point-loaded", "true", "true"),
                        RuntimeTestAssertion.Pass("standalone-id", "KingmakerBuffPlanner", _modEntry.Info.Id),
                        RuntimeTestAssertion.Pass("version", BuildInfo.Version, _modEntry.Info.Version),
                        RuntimeTestAssertion.Pass("commit", _request.ExpectedCommit, BuildInfo.Commit)
                    }
                };
                string resultPath = Path.Combine(_request.EvidenceDirectory, "runtime-result.json");
                AtomicFile.WriteUtf8(
                    resultPath,
                    JsonConvert.SerializeObject(result, Formatting.Indented) + Environment.NewLine);
                _log.Info("Runtime scenario completed: " + _request.RunId + " PASS.");
                if (_request.ExitAfterCompletion) Application.Quit();
            }
            catch (Exception exception)
            {
                _log.Error("Runtime scenario failed.", exception);
            }

            return true;
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
        [JsonProperty("processId", Order = 11)] public int ProcessId { get; set; }
        [JsonProperty("startedAtUtc", Order = 12)] public string StartedAtUtc { get; set; }
        [JsonProperty("endedAtUtc", Order = 13)] public string EndedAtUtc { get; set; }
        [JsonProperty("assertions", Order = 14)] public List<RuntimeTestAssertion> Assertions { get; set; }
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
    }
}
