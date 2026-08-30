using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KingmakerBuffPlanner.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Persistence
{
    public sealed class ProfileLoadResult
    {
        internal ProfileLoadResult(BuffPlannerProfile profile, bool recovered, bool migrated, string sourcePath, string warning)
        {
            Profile = profile;
            RecoveredFromBackup = recovered;
            Migrated = migrated;
            SourcePath = sourcePath ?? string.Empty;
            Warning = warning ?? string.Empty;
        }

        public BuffPlannerProfile Profile { get; private set; }
        public bool RecoveredFromBackup { get; private set; }
        public bool Migrated { get; private set; }
        public string SourcePath { get; private set; }
        public string Warning { get; private set; }
    }

    public sealed class ProfileRepository
    {
        private const int BackupCount = 3;
        private readonly string _settingsDirectory;

        public ProfileRepository(string modPath)
        {
            if (string.IsNullOrWhiteSpace(modPath) || !Path.IsPathRooted(modPath))
                throw new ArgumentException("Absolute mod path is required.", "modPath");
            _settingsDirectory = Path.Combine(Path.GetFullPath(modPath), "UserSettings");
        }

        public ProfileLoadResult Load(string campaignId)
        {
            RequireCampaign(campaignId);
            string primary = GetProfilePath(campaignId);
            var attempts = new List<string> { primary };
            for (int i = 1; i <= BackupCount; i++) attempts.Add(BackupPath(primary, i));
            string warning = string.Empty;
            for (int i = 0; i < attempts.Count; i++)
            {
                string path = attempts[i];
                if (!File.Exists(path)) continue;
                try
                {
                    bool migrated;
                    BuffPlannerProfile profile = Deserialize(File.ReadAllText(path), campaignId, out migrated);
                    return new ProfileLoadResult(profile, i != 0, migrated, path, warning);
                }
                catch (Exception exception)
                {
                    warning = AppendWarning(warning, Path.GetFileName(path) + ": " + exception.Message);
                }
            }
            return new ProfileLoadResult(BuffPlannerProfile.CreateDefault(campaignId), false, false,
                string.Empty, warning);
        }

        public void Save(BuffPlannerProfile profile)
        {
            Validate(profile, profile == null ? null : profile.CampaignId);
            Directory.CreateDirectory(_settingsDirectory);
            string path = GetProfilePath(profile.CampaignId);
            string json = Serialize(profile);
            if (File.Exists(path))
            {
                string previous = File.ReadAllText(path);
                try
                {
                    bool migrated;
                    Deserialize(previous, profile.CampaignId, out migrated);
                    RotateBackups(path, previous);
                }
                catch (Exception)
                {
                    // A malformed primary is never promoted over a known-good backup.
                }
            }
            AtomicFile.WriteUtf8(path, json);
        }

        internal string GetProfilePath(string campaignId)
        {
            RequireCampaign(campaignId);
            return Path.Combine(_settingsDirectory,
                "kingmaker-buff-planner-" + CampaignHash(campaignId) + ".json");
        }

        private static BuffPlannerProfile Deserialize(string json, string campaignId, out bool migrated)
        {
            RejectDuplicateProperties(json);
            JObject document = JObject.Parse(json);
            migrated = ProfileMigrator.Migrate(document);
            var settings = Settings();
            settings.MissingMemberHandling = MissingMemberHandling.Error;
            BuffPlannerProfile profile = document.ToObject<BuffPlannerProfile>(JsonSerializer.Create(settings));
            Validate(profile, campaignId);
            return profile;
        }

        private static string Serialize(BuffPlannerProfile profile)
        {
            return JsonConvert.SerializeObject(profile, Formatting.Indented, Settings()) + Environment.NewLine;
        }

        private static JsonSerializerSettings Settings()
        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                TypeNameHandling = TypeNameHandling.None,
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Error,
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            };
        }

        private static void Validate(BuffPlannerProfile profile, string campaignId)
        {
            if (profile == null) throw new InvalidDataException("profile-null");
            if (profile.SchemaVersion != BuffPlannerProfile.CurrentSchemaVersion)
                throw new InvalidDataException("schema-version");
            RequireCampaign(profile.CampaignId);
            if (campaignId != null && !string.Equals(profile.CampaignId, campaignId, StringComparison.Ordinal))
                throw new InvalidDataException("campaign-id-mismatch");
            if (profile.Routines == null || profile.ProviderPreferences == null ||
                profile.HiddenSourceIds == null || profile.Ui == null || profile.Execution == null)
                throw new InvalidDataException("required-profile-section-null");
            RequireUnique(profile.Routines.Select(r => r == null ? null : r.RoutineId), "routine-id");
            RequireUnique(profile.ProviderPreferences.Select(p => p == null ? null : p.ProviderKey), "provider-key");
            if (profile.ProviderPreferences.Any(preference =>
                    (preference.Priority != null && preference.Priority.Value < 0) ||
                    (preference.MaximumCasts != null &&
                        preference.MaximumCasts.Value < 1)))
                throw new InvalidDataException("invalid-provider-preference");
            foreach (RoutineProfile routine in profile.Routines)
            {
                if (string.IsNullOrWhiteSpace(routine.Name) || routine.Assignments == null)
                    throw new InvalidDataException("invalid-routine");
                RequireUnique(routine.Assignments.Select(a => a == null ? null : a.SourceId), "source-id");
                foreach (SourceAssignmentProfile assignment in routine.Assignments)
                {
                    if (assignment.Ability == null || assignment.WantedTargetUnitIds == null ||
                        assignment.IgnoredPresenceMarkers == null || assignment.SelectedEnhancementIds == null)
                        throw new InvalidDataException("invalid-assignment");
                    RequireUnique(assignment.SelectedEnhancementIds, "enhancement-id");
                    assignment.Ability.ToKey();
                }
            }
            if (profile.Ui.Scale < 0.5f || profile.Ui.Scale > 3.0f)
                throw new InvalidDataException("ui-scale");
            if (profile.Execution.Mode != "animated" && profile.Execution.Mode != "instant")
                throw new InvalidDataException("execution-mode");
            PlannerHotkeyText.Validate(profile.Ui.Hotkey);
        }

        private static void RequireUnique(IEnumerable<string> values, string label)
        {
            var list = values.ToList();
            if (list.Any(string.IsNullOrWhiteSpace) ||
                list.Distinct(StringComparer.Ordinal).Count() != list.Count)
                throw new InvalidDataException("duplicate-or-empty-" + label);
        }

        private static void RotateBackups(string primary, string previous)
        {
            for (int i = BackupCount; i >= 2; i--)
            {
                string source = BackupPath(primary, i - 1);
                if (File.Exists(source)) AtomicFile.WriteUtf8(BackupPath(primary, i), File.ReadAllText(source));
            }
            AtomicFile.WriteUtf8(BackupPath(primary, 1), previous);
        }

        private static string BackupPath(string primary, int index) { return primary + ".bak" + index; }

        private static string CampaignHash(string campaignId)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(new UTF8Encoding(false).GetBytes(campaignId));
                var result = new StringBuilder(24);
                for (int i = 0; i < 12; i++) result.Append(hash[i].ToString("x2"));
                return result.ToString();
            }
        }

        private static void RequireCampaign(string campaignId)
        {
            if (string.IsNullOrWhiteSpace(campaignId) || campaignId.Length > 512)
                throw new ArgumentException("Exact campaign ID is required.", "campaignId");
        }

        private static string AppendWarning(string current, string value)
        {
            return string.IsNullOrEmpty(current) ? value : current + " | " + value;
        }

        private static void RejectDuplicateProperties(string json)
        {
            var properties = new Stack<HashSet<string>>();
            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                        properties.Push(new HashSet<string>(StringComparer.Ordinal));
                    else if (reader.TokenType == JsonToken.PropertyName)
                    {
                        if (properties.Count == 0 || !properties.Peek().Add((string)reader.Value))
                            throw new InvalidDataException("duplicate-property");
                    }
                    else if (reader.TokenType == JsonToken.EndObject)
                    {
                        if (properties.Count == 0) throw new InvalidDataException("malformed-object");
                        properties.Pop();
                    }
                }
            }
            if (properties.Count != 0) throw new InvalidDataException("malformed-object");
        }
    }

    internal static class ProfileMigrator
    {
        internal static bool Migrate(JObject document)
        {
            JToken schema = document["schemaVersion"];
            if (schema == null || schema.Type != JTokenType.Integer) throw new InvalidDataException("schema-version-missing");
            int version = (int)schema;
            bool migrated = false;
            if (version == 1)
            {
                if (document["ui"] == null) document["ui"] = JObject.FromObject(UiProfile.Default());
                if (document["execution"] == null)
                    document["execution"] = JObject.FromObject(ExecutionProfile.Default());
                document["schemaVersion"] = 2;
                version = 2;
                migrated = true;
            }
            if (version == 2)
            {
                JObject ui = document["ui"] as JObject;
                if (ui == null) throw new InvalidDataException("ui-missing");
                string hotkey = (string)ui["hotkey"];
                if (string.IsNullOrWhiteSpace(hotkey) ||
                    string.Equals(hotkey, "F10", StringComparison.OrdinalIgnoreCase))
                    ui["hotkey"] = PlannerHotkeyText.Default;
                JObject execution = document["execution"] as JObject;
                if (execution == null) throw new InvalidDataException("execution-missing");
                if (execution["recastExisting"] == null) execution["recastExisting"] = false;
                document["hiddenSourceIds"] = new JArray();
                document["schemaVersion"] = 3;
                version = 3;
                migrated = true;
            }
            if (version == 3)
            {
                JArray routines = document["routines"] as JArray;
                if (routines == null) throw new InvalidDataException("routines-missing");
                foreach (JObject routine in routines.OfType<JObject>())
                {
                    JArray assignments = routine["assignments"] as JArray;
                    if (assignments == null) throw new InvalidDataException("assignments-missing");
                    foreach (JObject assignment in assignments.OfType<JObject>())
                        if (assignment["selectedEnhancementIds"] == null)
                            assignment["selectedEnhancementIds"] = new JArray();
                }
                document["schemaVersion"] = 4;
                version = 4;
                migrated = true;
            }
            if (version != BuffPlannerProfile.CurrentSchemaVersion)
                throw new InvalidDataException("unsupported-schema-version:" + version);
            JArray hidden = document["hiddenSourceIds"] as JArray;
            if (hidden != null && hidden.Count != 0)
            {
                hidden.RemoveAll();
                migrated = true;
            }
            return migrated;
        }
    }

    internal static class PlannerHotkeyText
    {
        internal const string Default = "Ctrl+Shift+B";

        internal static void Validate(string value)
        {
            if (value != "Ctrl+Shift+B" && value != "Ctrl+Shift+P")
                throw new InvalidDataException("planner-hotkey");
        }
    }
}
