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
    public enum CastingPlanLoadStatus
    {
        Absent,
        Loaded,
        RecoveredFromBackup,
        UnsupportedSchema,
        Corrupt
    }

    public sealed class CastingPlanLoadResult
    {
        internal CastingPlanLoadResult(
            CastingPlanLoadStatus status, CastingPlanProfile profile,
            string sourcePath, string warning)
        {
            Status = status;
            Profile = profile;
            SourcePath = sourcePath ?? string.Empty;
            Warning = warning ?? string.Empty;
        }

        public CastingPlanLoadStatus Status { get; private set; }
        public CastingPlanProfile Profile { get; private set; }
        public string SourcePath { get; private set; }
        public string Warning { get; private set; }
    }

    // Candidate-storage repository for the schema-6 casting plan. Failure
    // states are reported, never papered over: a corrupt or newer-schema
    // primary yields its status and leaves the bytes untouched, and Save
    // refuses to overwrite a primary it cannot read or that carries a newer
    // schema. No default profile is ever fabricated over unresolved data.
    public sealed class CastingPlanRepository
    {
        private const int BackupCount = 3;
        private readonly string _settingsDirectory;

        public CastingPlanRepository(string modPath)
        {
            if (string.IsNullOrWhiteSpace(modPath) || !Path.IsPathRooted(modPath))
                throw new ArgumentException("Absolute mod path is required.", "modPath");
            _settingsDirectory = Path.Combine(Path.GetFullPath(modPath), "UserSettings");
        }

        public CastingPlanLoadResult Load(string campaignId)
        {
            RequireCampaign(campaignId);
            string primary = GetProfilePath(campaignId);
            var attempts = new List<string> { primary };
            for (int i = 1; i <= BackupCount; i++) attempts.Add(BackupPath(primary, i));
            string warnings = string.Empty;
            bool sawAny = false;
            foreach (string path in attempts)
            {
                if (!File.Exists(path)) continue;
                sawAny = true;
                string json;
                try
                {
                    json = File.ReadAllText(path);
                }
                catch (Exception exception)
                {
                    warnings = AppendWarning(warnings,
                        Path.GetFileName(path) + ":unreadable:" + exception.Message);
                    continue;
                }
                int schema;
                if (!TryReadSchemaVersion(json, out schema))
                {
                    warnings = AppendWarning(warnings,
                        Path.GetFileName(path) + ":schema-version-missing");
                    continue;
                }
                // A newer-schema file is reported as such and never silently
                // resolved through an older sibling or backup.
                if (schema > CastingPlanProfile.CurrentSchemaVersion)
                    return new CastingPlanLoadResult(
                        CastingPlanLoadStatus.UnsupportedSchema, null, path,
                        "schema-version-newer:" + schema);
                try
                {
                    CastingPlanProfile profile = Deserialize(json, campaignId);
                    if (schema < CastingPlanProfile.CurrentSchemaVersion)
                        return new CastingPlanLoadResult(
                            CastingPlanLoadStatus.UnsupportedSchema, null, path,
                            "schema-version-older:" + schema + ":migration-pending");
                    return new CastingPlanLoadResult(
                        string.Equals(path, primary, StringComparison.Ordinal)
                            ? CastingPlanLoadStatus.Loaded
                            : CastingPlanLoadStatus.RecoveredFromBackup,
                        profile, path, warnings);
                }
                catch (Exception exception)
                {
                    warnings = AppendWarning(warnings,
                        Path.GetFileName(path) + ":invalid:" + exception.Message);
                }
            }
            // Files existed but none parsed: that is corruption with the
            // accumulated evidence, not an absent profile.
            return new CastingPlanLoadResult(
                sawAny ? CastingPlanLoadStatus.Corrupt : CastingPlanLoadStatus.Absent,
                null, sawAny ? primary : string.Empty, warnings);
        }

        public void Save(CastingPlanProfile profile)
        {
            Validate(profile, profile == null ? null : profile.CampaignId);
            Directory.CreateDirectory(_settingsDirectory);
            string path = GetProfilePath(profile.CampaignId);
            if (File.Exists(path))
            {
                string previous = File.ReadAllText(path);
                int schema;
                if (!TryReadSchemaVersion(previous, out schema) ||
                    schema > CastingPlanProfile.CurrentSchemaVersion)
                    throw new InvalidDataException(
                        "refusing-to-overwrite-unreadable-or-newer-primary");
                // A candidate write must round-trip before it can replace a
                // readable primary; a malformed previous primary is never
                // rotated into the backup chain.
                try
                {
                    Deserialize(previous, profile.CampaignId);
                    RotateBackups(path, previous);
                }
                catch (Exception)
                {
                }
            }
            AtomicFile.WriteUtf8(path, Serialize(profile));
        }

        internal string GetProfilePath(string campaignId)
        {
            RequireCampaign(campaignId);
            return Path.Combine(_settingsDirectory,
                "kingmaker-buff-planner-casting-" + CampaignHash(campaignId) + ".json");
        }

        private static bool TryReadSchemaVersion(string json, out int schemaVersion)
        {
            schemaVersion = 0;
            try
            {
                JObject document = JObject.Parse(json);
                JToken token = document["schemaVersion"];
                if (token == null || token.Type != JTokenType.Integer) return false;
                schemaVersion = (int)token;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static CastingPlanProfile Deserialize(string json, string campaignId)
        {
            RejectDuplicateProperties(json);
            var settings = Settings();
            settings.MissingMemberHandling = MissingMemberHandling.Error;
            CastingPlanProfile profile = JsonConvert.DeserializeObject<CastingPlanProfile>(
                json, settings);
            Validate(profile, campaignId);
            // Re-derive the domain document: full constructor invariants are
            // part of read validation, not just write validation.
            profile.ToDocument();
            return profile;
        }

        private static string Serialize(CastingPlanProfile profile)
        {
            return JsonConvert.SerializeObject(profile, Formatting.Indented, Settings()) +
                Environment.NewLine;
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

        private static void Validate(CastingPlanProfile profile, string campaignId)
        {
            if (profile == null) throw new InvalidDataException("profile-null");
            if (profile.SchemaVersion != CastingPlanProfile.CurrentSchemaVersion)
                throw new InvalidDataException("schema-version");
            RequireCampaign(profile.CampaignId);
            if (campaignId != null && !string.Equals(profile.CampaignId, campaignId,
                    StringComparison.Ordinal))
                throw new InvalidDataException("campaign-id-mismatch");
            if (profile.Routines == null || profile.Castings == null ||
                profile.Ui == null || profile.Execution == null)
                throw new InvalidDataException("required-profile-section-null");
            if (profile.Routines.Count == 0)
                throw new InvalidDataException("routines-empty");
            RequireUnique(profile.Routines.Select(value =>
                value == null ? null : value.RoutineId), "routine-id");
            var routineIds = new HashSet<string>(
                profile.Routines.Select(value => value.RoutineId), StringComparer.Ordinal);
            RequireUnique(profile.Castings.Select(value =>
                value == null ? null : value.CastingId), "casting-id");
            foreach (PlannedCastingProfile casting in profile.Castings)
            {
                if (casting == null || casting.Ability == null ||
                    casting.RequiredCoverageUnitIds == null ||
                    casting.TargetingModifiers == null ||
                    casting.Enhancements == null ||
                    casting.IgnoredPresenceMarkers == null)
                    throw new InvalidDataException("invalid-casting");
                if (!routineIds.Contains(casting.RoutineId))
                    throw new InvalidDataException("casting-routine-unknown:" + casting.CastingId);
                casting.Ability.ToKey();
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
                if (File.Exists(source))
                    AtomicFile.WriteUtf8(BackupPath(primary, i), File.ReadAllText(source));
            }
            AtomicFile.WriteUtf8(BackupPath(primary, 1), previous);
        }

        private static string BackupPath(string primary, int index)
        {
            return primary + ".bak" + index;
        }

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
                        if (properties.Count == 0 ||
                            !properties.Peek().Add((string)reader.Value))
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
}
