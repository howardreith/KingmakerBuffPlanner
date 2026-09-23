using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal static class RuntimeTestProtocol
    {
        internal const string ActivationFlag = "-kbpRuntimeTestRequest";
        internal const string EvidenceRoot = @"C:\Dev\KingmakerBuffPlannerLab\runtime-evidence";

        internal static RuntimeTestRequest TryRead(string[] arguments, out string rejection)
        {
            return TryReadWithinRoot(arguments, EvidenceRoot, out rejection);
        }

        internal static RuntimeTestRequest TryReadWithinRoot(
            string[] arguments,
            string evidenceRoot,
            out string rejection)
        {
            rejection = string.Empty;
            if (arguments == null) return null;

            int flagIndex = -1;
            for (int i = 0; i < arguments.Length; i++)
            {
                if (!string.Equals(arguments[i], ActivationFlag, StringComparison.Ordinal)) continue;
                if (flagIndex >= 0)
                {
                    rejection = "duplicate-activation-flag";
                    return null;
                }

                flagIndex = i;
            }

            if (flagIndex < 0) return null;
            if (flagIndex + 1 >= arguments.Length || string.IsNullOrWhiteSpace(arguments[flagIndex + 1]))
            {
                rejection = "missing-request-path";
                return null;
            }

            try
            {
                string requestPath = RequireDescendant(arguments[flagIndex + 1], evidenceRoot);
                if (!File.Exists(requestPath)) throw new InvalidDataException("request-file-missing");
                string json = File.ReadAllText(requestPath);
                RejectDuplicateProperties(json);
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error,
                    NullValueHandling = NullValueHandling.Include
                };
                RuntimeTestRequest request = JsonConvert.DeserializeObject<RuntimeTestRequest>(json, settings);
                Validate(request, requestPath, evidenceRoot);
                return request;
            }
            catch (Exception exception)
            {
                rejection = "invalid-request:" + exception.Message;
                return null;
            }
        }

        private static void Validate(
            RuntimeTestRequest request,
            string requestPath,
            string evidenceRoot)
        {
            if (request == null) throw new InvalidDataException("request-null");
            if (request.SchemaVersion != 1) throw new InvalidDataException("schema-version");
            if (!request.Enabled) throw new InvalidDataException("not-enabled");
            if (!IsSafeIdentifier(request.RunId)) throw new InvalidDataException("run-id");
            if (!IsSafeIdentifier(request.ProfileId) ||
                (request.ProfileId != "native-only" && request.ProfileId != "call-of-the-wild" &&
                 request.ProfileId != "human-reproduction" && request.ProfileId != "full-user"))
                throw new InvalidDataException("profile-id");
            if (!IsKnownScenario(request.Scenario))
                throw new InvalidDataException("scenario");
            if (!string.Equals(request.ExpectedModVersion, BuildInfo.Version, StringComparison.Ordinal))
                throw new InvalidDataException("version-mismatch");
            if (!string.Equals(request.ExpectedCommit, BuildInfo.Commit, StringComparison.Ordinal))
                throw new InvalidDataException("commit-mismatch");
            if (!IsSha256(request.ExpectedPackageSha256) || !IsSha256(request.ExpectedDllSha256))
                throw new InvalidDataException("expected-hash");
            if (request.TimeoutSeconds < 5 || request.TimeoutSeconds > 1800)
                throw new InvalidDataException("timeout");
            ValidateParameters(request);
            if (request.ExpectedOptionalMods == null || request.ExpectedBlueprintGuids == null)
                throw new InvalidDataException("compatibility-expectations");
            foreach (RuntimeExpectedOptionalMod mod in request.ExpectedOptionalMods)
                if (mod == null || !IsSafeIdentifier(mod.UmmId) || !IsSafeIdentifier(mod.AssemblyName) ||
                    !IsSha256(mod.AssemblySha256) || string.IsNullOrWhiteSpace(mod.Version))
                    throw new InvalidDataException("optional-mod-expectation");
            foreach (string guid in request.ExpectedBlueprintGuids)
                if (!IsBlueprintGuid(guid)) throw new InvalidDataException("expected-blueprint-guid");
            if (new HashSet<string>(request.ExpectedBlueprintGuids, StringComparer.Ordinal).Count !=
                request.ExpectedBlueprintGuids.Count)
                throw new InvalidDataException("duplicate-expected-blueprint-guid");
            if ((request.ProfileId == "native-only" && request.ExpectedOptionalMods.Count != 0) ||
                (request.ProfileId == "call-of-the-wild" && request.ExpectedOptionalMods.Count != 1) ||
                (request.ProfileId == "human-reproduction" && request.ExpectedOptionalMods.Count != 3) ||
                (request.ProfileId == "full-user" && request.ExpectedOptionalMods.Count != 15))
                throw new InvalidDataException("profile-mod-expectation");
            if ((request.ProfileId == "native-only" && request.ExpectedBlueprintGuids.Count != 0) ||
                (request.ProfileId == "call-of-the-wild" && request.ExpectedBlueprintGuids.Count < 3))
                throw new InvalidDataException("profile-blueprint-expectation");

            string evidence = RequireDescendant(request.EvidenceDirectory, evidenceRoot);
            if (!Directory.Exists(evidence)) throw new InvalidDataException("evidence-directory-missing");
            string requestDirectory = Path.GetDirectoryName(requestPath).TrimEnd('\\');
            if (!string.Equals(evidence, requestDirectory, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("request-evidence-mismatch");
            string resultPath = Path.Combine(evidence, "runtime-result.json");
            if (File.Exists(resultPath)) throw new InvalidDataException("run-id-reused");
        }

        private static string RequireDescendant(string path, string root)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
                throw new InvalidDataException("absolute-path-required");
            string fullRoot = Path.GetFullPath(root).TrimEnd('\\');
            string fullPath = Path.GetFullPath(path).TrimEnd('\\');
            if (!fullPath.StartsWith(fullRoot + "\\", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("path-outside-root");
            return fullPath;
        }

        private static bool IsSafeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 100) return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') &&
                    !(c >= '0' && c <= '9') && c != '.' && c != '_' && c != '-') return false;
            }

            return true;
        }

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 64) return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')) return false;
            }
            return true;
        }

        private static bool IsBlueprintGuid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32) return false;
            foreach (char c in value)
                if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')) return false;
            return true;
        }

        internal static bool IsCatalogScenario(string scenario)
        {
            return string.Equals(scenario, "native-buff-catalog", StringComparison.Ordinal) ||
                string.Equals(scenario, "final-no-save-core", StringComparison.Ordinal);
        }

        internal static bool IsUiScenario(string scenario)
        {
            return string.Equals(scenario, "ui-root-smoke", StringComparison.Ordinal) ||
                string.Equals(scenario, "live-ui-bootstrap", StringComparison.Ordinal);
        }

        internal static bool IsLiveUiScenario(string scenario)
        {
            return string.Equals(scenario, "live-ui-bootstrap", StringComparison.Ordinal) ||
                IsWorkspaceScenario(scenario);
        }

        internal static bool IsWorkspaceScenario(string scenario)
        {
            return string.Equals(scenario, "live-workspace-qual",
                StringComparison.Ordinal) ||
                IsReloadScenario(scenario) ||
                IsImportScenario(scenario) ||
                IsManualWorkspaceScenario(scenario) ||
                IsProbeScenario(scenario) ||
                IsInspectionScenario(scenario) ||
                IsQualificationScenario(scenario);
        }

        // The workspace scenario plus an in-game reload (mission section 8,
        // save/reload): after the saved close and reopen, the planner is
        // closed, the exact WORKING save is loaded again through the game's
        // own Game.LoadGame under the guarded read-only loader, and the
        // reopened planner must show the saved plan for the same campaign
        // with one event subscription, one HUD root and no casting run.
        internal static bool IsReloadScenario(string scenario)
        {
            return string.Equals(scenario, "live-workspace-reload", StringComparison.Ordinal);
        }

        // First-open import in game (mission section 11): before the first
        // open the host writes a classic plan for the loaded campaign from
        // live discovery; the first open must import it through the
        // production migration (never modifying it, archiving it byte-exact,
        // making nothing Ready on its own). No synthetic input; automation
        // family only.
        internal static bool IsImportScenario(string scenario)
        {
            return string.Equals(scenario, "live-workspace-import", StringComparison.Ordinal);
        }

        // Guarded casting qualification through the PRODUCTION path.
        // "live-cast-qual-select" selects the recipe and forecasts every
        // step projection without constructing any boundary;
        // "live-cast-qual" additionally requires the run-bound schema-3
        // allowance (qualificationAllowance parameter) naming exactly those
        // projections, and is the only other path that can submit casts.
        internal static bool IsQualificationScenario(string scenario)
        {
            return string.Equals(scenario, "live-cast-qual-select", StringComparison.Ordinal) ||
                IsCastingQualificationScenario(scenario);
        }

        internal static bool IsCastingQualificationScenario(string scenario)
        {
            return string.Equals(scenario, "live-cast-qual", StringComparison.Ordinal);
        }

        internal const int QualificationRunDeadlineSeconds = 240;

        // Read-only inspection of a loaded campaign copy (the advanced-copy
        // family first; the automation fixture is its smoke test): opens the
        // workspace through the production path with zero synthetic input,
        // records campaign, roster, pools, providers, enhancements and live
        // effects, closes the workspace, and never authors or submits.
        internal static bool IsInspectionScenario(string scenario)
        {
            return string.Equals(scenario, "live-advanced-inspect", StringComparison.Ordinal);
        }

        // The advanced copy is inspected before anything casts on it: the
        // non-casting inspection, workspace and qualification-selection
        // scenarios may load it; a casting qualification may load it only
        // with its run-bound allowance (and the launcher first requires a
        // passing inspection of the same bound pair).
        internal static bool IsAdvancedFamilyScenario(string scenario)
        {
            return IsInspectionScenario(scenario) ||
                string.Equals(scenario, "live-workspace-qual", StringComparison.Ordinal) ||
                string.Equals(scenario, "live-cast-qual-select", StringComparison.Ordinal) ||
                IsManualWorkspaceScenario(scenario);
        }

        // Single-cast probe scenarios. BOTH request zero synthetic input and
        // open the workspace through the production path like the manual
        // scenario. "live-cast-probe-select" only discovers and records the
        // exact probe selection and projection identity; it never constructs
        // a dispatch boundary. "live-cast-probe" additionally requires the
        // owner's run-bound one-shot allowance (probeAllowance parameter)
        // and is the ONLY path that can submit a native cast.
        internal static bool IsProbeScenario(string scenario)
        {
            return string.Equals(scenario, "live-cast-probe-select", StringComparison.Ordinal) ||
                IsCastingProbeScenario(scenario);
        }

        internal static bool IsCastingProbeScenario(string scenario)
        {
            return string.Equals(scenario, "live-cast-probe", StringComparison.Ordinal);
        }

        // Workspace scenarios that must never request synthetic input.
        internal static bool IsNoInputWorkspaceScenario(string scenario)
        {
            return IsManualWorkspaceScenario(scenario) || IsProbeScenario(scenario) ||
                IsInspectionScenario(scenario) || IsQualificationScenario(scenario) ||
                IsImportScenario(scenario);
        }

        internal const int ProbeRunDeadlineSeconds = 60;
        // How long the probe waits for the world to run after closing the
        // planner, in elapsed time.
        internal const int ProbeWorldWaitSeconds = 30;

        // The supervised manual-inspection scenario (review H1): the full
        // guarded pipeline through the opened workspace, then a BOUNDED
        // human-hold phase with all synthetic input suppressed. It requires
        // an explicit manualHoldSeconds parameter; the host acknowledges
        // manual-ready only with the workspace open and no input requested,
        // and terminal outcomes are done-marker, stop-marker, or deadline
        // (which is never acceptance).
        internal static bool IsManualWorkspaceScenario(string scenario)
        {
            return string.Equals(scenario, "live-workspace-manual",
                StringComparison.Ordinal);
        }

        internal static int ReadManualHoldSeconds(
            System.Collections.Generic.IDictionary<string, object> parameters)
        {
            if (parameters == null ||
                !parameters.ContainsKey("manualHoldSeconds"))
                throw new InvalidDataException("manual-hold-seconds-missing");
            object raw = parameters["manualHoldSeconds"];
            // Range-check at full width BEFORE narrowing (review C3): an
            // out-of-range long must never wrap into an accepted int.
            long seconds;
            if (raw is int) seconds = (int)raw;
            else if (raw is long) seconds = (long)raw;
            else throw new InvalidDataException("manual-hold-seconds-invalid");
            if (seconds < MinimumManualHoldSeconds ||
                seconds > MaximumManualHoldSeconds)
                throw new InvalidDataException("manual-hold-seconds-range");
            return (int)seconds;
        }

        // The launcher's [ValidateRange(30, 1200)] on -ManualHoldSeconds is
        // the same contract; the reader enforces it independently.
        internal const int MinimumManualHoldSeconds = 30;
        internal const int MaximumManualHoldSeconds = 1200;

        internal static bool IsNativeUiProbeScenario(string scenario)
        {
            return string.Equals(scenario, "ui-native-contract-probe", StringComparison.Ordinal);
        }

        internal static bool IsPerformanceScenario(string scenario)
        {
            return string.Equals(scenario, "performance-probe", StringComparison.Ordinal);
        }

        internal static bool IsLaunchRenderDiagnosticScenario(string scenario)
        {
            return string.Equals(scenario, "launch-render-diagnostic", StringComparison.Ordinal);
        }

        internal static bool IsMenuInputDiagnosticScenario(string scenario)
        {
            return string.Equals(scenario, "menu-input-diagnostic", StringComparison.Ordinal);
        }

        internal static bool IsMenuDiagnosticScenario(string scenario)
        {
            return IsLaunchRenderDiagnosticScenario(scenario) ||
                IsMenuInputDiagnosticScenario(scenario);
        }

        private static bool IsKnownScenario(string scenario)
        {
            return string.Equals(scenario, "mod-load-smoke", StringComparison.Ordinal) ||
                IsCatalogScenario(scenario) || IsUiScenario(scenario) ||
                IsNativeUiProbeScenario(scenario) || IsPerformanceScenario(scenario) ||
                IsMenuDiagnosticScenario(scenario) ||
                IsWorkspaceScenario(scenario);
        }

        private static void ValidateParameters(RuntimeTestRequest request)
        {
            if (request.Parameters == null) throw new InvalidDataException("parameters");
            if (IsManualWorkspaceScenario(request.Scenario))
            {
                // The manual scenario carries the EXACT live-save contract
                // plus exactly one permitted extension, the bounded hold
                // (review I4): no early return may bypass the guarded save
                // validation.
                string[] manualExact =
                {
                    "workingSaveName", "workingFileName", "workingSha256",
                    "baselineSaveName", "baselineFileName", "baselineSha256",
                    "expectedGameName", "expectedGameId", "executionMode",
                    "manualHoldSeconds"
                };
                if (request.Parameters.Count != manualExact.Length ||
                    manualExact.Any(name =>
                        !request.Parameters.ContainsKey(name)))
                    throw new InvalidDataException("manual-save-parameters");
                ValidateLiveSaveParameters(request, manualExact.Length);
                ReadManualHoldSeconds(request.Parameters);
                return;
            }
            if (request.Parameters.ContainsKey("manualHoldSeconds"))
                throw new InvalidDataException(
                    "manual-hold-seconds-only-with-manual-scenario");
            // The probe allowance exists only on the casting probe scenario
            // (never on selection-only or any other scenario), as a string
            // the host parses strictly against this run id.
            bool hasAllowance = request.Parameters.ContainsKey("probeAllowance");
            if (hasAllowance && !IsCastingProbeScenario(request.Scenario))
                throw new InvalidDataException("probe-allowance-only-with-casting-probe");
            bool hasQualification = request.Parameters.ContainsKey("qualificationAllowance");
            if (hasQualification && !IsCastingQualificationScenario(request.Scenario))
                throw new InvalidDataException("qualification-allowance-only-with-casting-qualification");
            bool hasRecipe = request.Parameters.ContainsKey("qualificationRecipe");
            if (hasRecipe && !IsQualificationScenario(request.Scenario))
                throw new InvalidDataException("qualification-recipe-only-with-qualification");
            if (IsQualificationScenario(request.Scenario))
            {
                if (hasQualification && !(request.Parameters["qualificationAllowance"] is string))
                    throw new InvalidDataException("qualification-allowance-type");
                if (hasRecipe && !Execution.CastingQualificationRecipe.IsKnown(
                        request.Parameters["qualificationRecipe"] as string))
                    throw new InvalidDataException("qualification-recipe-unknown");
                ValidateLiveSaveParameters(request, 9 + (hasQualification ? 1 : 0) + (hasRecipe ? 1 : 0),
                    hasQualification);
                if (!string.Equals(request.Parameters["executionMode"] as string, "instant",
                        StringComparison.Ordinal))
                    throw new InvalidDataException("qualification-execution-mode-instant-only");
                return;
            }
            if (IsProbeScenario(request.Scenario))
            {
                if (hasAllowance && !(request.Parameters["probeAllowance"] is string))
                    throw new InvalidDataException("probe-allowance-type");
                ValidateLiveSaveParameters(request, 9 + (hasAllowance ? 1 : 0));
                if (!string.Equals(request.Parameters["executionMode"] as string, "instant",
                        StringComparison.Ordinal))
                    throw new InvalidDataException("probe-execution-mode-instant-only");
                return;
            }
            if (IsPerformanceScenario(request.Scenario))
            {
                string[] performanceNames =
                {
                    "durationSeconds", "disableHudDiscovery", "minimumFramesPerSecond"
                };
                if (request.Parameters.Count != performanceNames.Length ||
                    performanceNames.Any(name => !request.Parameters.ContainsKey(name)))
                    throw new InvalidDataException("performance-parameters");
                int duration;
                double minimum;
                try
                {
                    duration = Convert.ToInt32(request.Parameters["durationSeconds"]);
                    minimum = Convert.ToDouble(request.Parameters["minimumFramesPerSecond"]);
                }
                catch (Exception exception)
                {
                    throw new InvalidDataException("performance-parameter-type", exception);
                }
                if (duration < 5 || duration > 60)
                    throw new InvalidDataException("performance-duration");
                if (double.IsNaN(minimum) || double.IsInfinity(minimum) || minimum < 0 || minimum > 240)
                    throw new InvalidDataException("performance-minimum-fps");
                if (!(request.Parameters["disableHudDiscovery"] is bool))
                    throw new InvalidDataException("performance-disable-hud-discovery");
                return;
            }
            if (!IsLiveUiScenario(request.Scenario))
            {
                if (request.Parameters.Count != 0) throw new InvalidDataException("parameters");
                return;
            }
            string[] automaticExact =
            {
                "workingSaveName", "workingFileName", "workingSha256",
                "baselineSaveName", "baselineFileName", "baselineSha256",
                "expectedGameName", "expectedGameId", "executionMode"
            };
            if (request.Parameters.Count != automaticExact.Length)
                throw new InvalidDataException("live-save-parameters");
            ValidateLiveSaveParameters(request, automaticExact.Length);
        }

        // The guarded live-save contract shared by every scenario that
        // stages the WORKING campaign (reviews I4): exact keys, required
        // names, real SHA-256 values, distinct files, and a valid mode.
        // allowanceBound: the request carries a run-bound casting allowance,
        // the only way a casting scenario may load the advanced copy.
        private static void ValidateLiveSaveParameters(
            RuntimeTestRequest request, int expectedTotal, bool allowanceBound = false)
        {
            string[] exact =
            {
                "workingSaveName", "workingFileName", "workingSha256",
                "baselineSaveName", "baselineFileName", "baselineSha256",
                "expectedGameName", "expectedGameId", "executionMode"
            };
            if (request.Parameters.Count != expectedTotal ||
                exact.Any(name => !request.Parameters.ContainsKey(name)))
                throw new InvalidDataException("live-save-parameters");
            foreach (string name in exact)
            {
                object raw = request.Parameters[name];
                string value = raw as string;
                if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException("live-save-parameter:" + name);
                if (name.EndsWith("Sha256", StringComparison.Ordinal) && !IsSha256(value))
                    throw new InvalidDataException("live-save-hash:" + name);
            }
            string workingName = (string)request.Parameters["workingSaveName"];
            string baselineName = (string)request.Parameters["baselineSaveName"];
            bool automation = workingName == "KBP_AUTOMATION_WORKING" &&
                baselineName == "KBP_AUTOMATION_BASELINE";
            bool advanced = workingName == "KBP_ADVANCED_WORKING" &&
                baselineName == "KBP_ADVANCED_BASELINE";
            // Exactly one sealed family, never a mixed pair.
            if (!automation && !advanced)
                throw new InvalidDataException("live-save-names");
            if (advanced && !IsAdvancedFamilyScenario(request.Scenario) &&
                !(allowanceBound && IsCastingQualificationScenario(request.Scenario)))
                throw new InvalidDataException("live-save-family-scenario");
            if (string.Equals((string)request.Parameters["workingFileName"],
                (string)request.Parameters["baselineFileName"], StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("live-save-files-not-distinct");
            string executionMode = (string)request.Parameters["executionMode"];
            if (executionMode != "animated" && executionMode != "instant")
                throw new InvalidDataException("live-execution-mode");
        }

        private static void RejectDuplicateProperties(string json)
        {
            var objectProperties = new Stack<HashSet<string>>();
            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                        objectProperties.Push(new HashSet<string>(StringComparer.Ordinal));
                    else if (reader.TokenType == JsonToken.PropertyName)
                    {
                        if (objectProperties.Count == 0 ||
                            !objectProperties.Peek().Add((string)reader.Value))
                            throw new InvalidDataException("duplicate-property");
                    }
                    else if (reader.TokenType == JsonToken.EndObject)
                    {
                        if (objectProperties.Count == 0) throw new InvalidDataException("malformed-object");
                        objectProperties.Pop();
                    }
                }
            }

            if (objectProperties.Count != 0) throw new InvalidDataException("malformed-object");
        }
    }

    internal sealed class RuntimeTestRequest
    {
        [JsonProperty("schemaVersion", Required = Required.Always, Order = 1)]
        public int SchemaVersion { get; set; }

        [JsonProperty("enabled", Required = Required.Always, Order = 2)]
        public bool Enabled { get; set; }

        [JsonProperty("runId", Required = Required.Always, Order = 3)]
        public string RunId { get; set; }

        [JsonProperty("scenario", Required = Required.Always, Order = 4)]
        public string Scenario { get; set; }

        [JsonProperty("profileId", Required = Required.Always, Order = 5)]
        public string ProfileId { get; set; }

        [JsonProperty("expectedModVersion", Required = Required.Always, Order = 6)]
        public string ExpectedModVersion { get; set; }

        [JsonProperty("expectedCommit", Required = Required.Always, Order = 7)]
        public string ExpectedCommit { get; set; }

        [JsonProperty("evidenceDirectory", Required = Required.Always, Order = 8)]
        public string EvidenceDirectory { get; set; }

        [JsonProperty("expectedPackageSha256", Required = Required.Always, Order = 9)]
        public string ExpectedPackageSha256 { get; set; }

        [JsonProperty("expectedDllSha256", Required = Required.Always, Order = 10)]
        public string ExpectedDllSha256 { get; set; }

        [JsonProperty("timeoutSeconds", Required = Required.Always, Order = 11)]
        public int TimeoutSeconds { get; set; }

        [JsonProperty("exitAfterCompletion", Required = Required.Always, Order = 12)]
        public bool ExitAfterCompletion { get; set; }

        [JsonProperty("expectedOptionalMods", Required = Required.Always, Order = 13)]
        public List<RuntimeExpectedOptionalMod> ExpectedOptionalMods { get; set; }

        [JsonProperty("expectedBlueprintGuids", Required = Required.Always, Order = 14)]
        public List<string> ExpectedBlueprintGuids { get; set; }

        [JsonProperty("parameters", Required = Required.Always, Order = 15)]
        public Dictionary<string, object> Parameters { get; set; }
    }

    internal sealed class RuntimeExpectedOptionalMod
    {
        [JsonProperty("ummId", Required = Required.Always, Order = 1)] public string UmmId { get; set; }
        [JsonProperty("version", Required = Required.Always, Order = 2)] public string Version { get; set; }
        [JsonProperty("assemblyName", Required = Required.Always, Order = 3)] public string AssemblyName { get; set; }
        [JsonProperty("assemblySha256", Required = Required.Always, Order = 4)] public string AssemblySha256 { get; set; }
    }
}
