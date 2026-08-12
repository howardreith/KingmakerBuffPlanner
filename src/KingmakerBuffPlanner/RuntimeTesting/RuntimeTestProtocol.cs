using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal static class RuntimeTestProtocol
    {
        internal const string ActivationFlag = "-kbpRuntimeTestRequest";
        internal const string EvidenceRoot = @"C:\Dev\KingmakerBuffPlannerLab\runtime-evidence";

        internal static RuntimeTestRequest TryRead(string[] arguments, out string rejection)
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
                string requestPath = RequireDescendant(arguments[flagIndex + 1], EvidenceRoot);
                if (!File.Exists(requestPath)) throw new InvalidDataException("request-file-missing");
                string json = File.ReadAllText(requestPath);
                RejectDuplicateProperties(json);
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error,
                    NullValueHandling = NullValueHandling.Include
                };
                RuntimeTestRequest request = JsonConvert.DeserializeObject<RuntimeTestRequest>(json, settings);
                Validate(request, requestPath);
                return request;
            }
            catch (Exception exception)
            {
                rejection = "invalid-request:" + exception.Message;
                return null;
            }
        }

        private static void Validate(RuntimeTestRequest request, string requestPath)
        {
            if (request == null) throw new InvalidDataException("request-null");
            if (request.SchemaVersion != 1) throw new InvalidDataException("schema-version");
            if (!request.Enabled) throw new InvalidDataException("not-enabled");
            if (!IsSafeIdentifier(request.RunId)) throw new InvalidDataException("run-id");
            if (!IsSafeIdentifier(request.ProfileId) ||
                (request.ProfileId != "native-only" && request.ProfileId != "call-of-the-wild"))
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
            if (request.Parameters == null || request.Parameters.Count != 0)
                throw new InvalidDataException("parameters");
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
                (request.ProfileId == "call-of-the-wild" && request.ExpectedOptionalMods.Count != 1))
                throw new InvalidDataException("profile-mod-expectation");
            if ((request.ProfileId == "native-only" && request.ExpectedBlueprintGuids.Count != 0) ||
                (request.ProfileId == "call-of-the-wild" && request.ExpectedBlueprintGuids.Count < 3))
                throw new InvalidDataException("profile-blueprint-expectation");

            string evidence = RequireDescendant(request.EvidenceDirectory, EvidenceRoot);
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
                string.Equals(scenario, "final-no-save-core", StringComparison.Ordinal);
        }

        internal static bool IsNativeUiProbeScenario(string scenario)
        {
            return string.Equals(scenario, "ui-native-contract-probe", StringComparison.Ordinal);
        }

        private static bool IsKnownScenario(string scenario)
        {
            return string.Equals(scenario, "mod-load-smoke", StringComparison.Ordinal) ||
                IsCatalogScenario(scenario) || IsUiScenario(scenario) ||
                IsNativeUiProbeScenario(scenario);
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
