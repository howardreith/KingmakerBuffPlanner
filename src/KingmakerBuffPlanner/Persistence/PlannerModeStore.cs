using System;
using System.IO;
using KingmakerBuffPlanner.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Persistence
{
    public enum PlannerMode
    {
        // The established planner screen and quick-run (the default).
        Classic,
        // The casting-first planner: one saved casting is one native cast.
        CastingFirst
    }

    // The deliberate, player-chosen planner mode (UMM settings panel). It is
    // stored beside the profiles in UserSettings, which installation and
    // rollback preserve. Everyday-use v1.2: the casting-first graph is the
    // default and the only normal authoring route, so an absent store means
    // CastingFirst. An explicit "classic" value is honored during the
    // bounded Classic retirement (recovery/migration), never as the
    // ordinary new-install experience.
    public sealed class PlannerModeStore
    {
        internal const int SchemaVersion = 1;
        internal const string FileName = "planner-mode.json";
        private readonly string _settingsDirectory;

        public PlannerModeStore(string modPath)
        {
            if (string.IsNullOrWhiteSpace(modPath) || !Path.IsPathRooted(modPath))
                throw new ArgumentException("Absolute mod path is required.", "modPath");
            _settingsDirectory = Path.Combine(Path.GetFullPath(modPath), "UserSettings");
        }

        public string FilePath
        {
            get { return Path.Combine(_settingsDirectory, FileName); }
        }

        public PlannerMode Load(out string warning)
        {
            warning = string.Empty;
            if (!File.Exists(FilePath)) return PlannerMode.CastingFirst;
            try
            {
                JObject root = JObject.Parse(File.ReadAllText(FilePath));
                if (root.Value<int?>("schemaVersion") != SchemaVersion)
                {
                    warning = "planner-mode-ignored:schema-version";
                    return PlannerMode.CastingFirst;
                }
                string mode = root.Value<string>("mode");
                if (string.Equals(mode, "classic", StringComparison.Ordinal))
                {
                    warning = "planner-mode-explicit-classic";
                    return PlannerMode.Classic;
                }
                if (!string.Equals(mode, "casting-first", StringComparison.Ordinal))
                    warning = "planner-mode-ignored:unknown-mode";
                return PlannerMode.CastingFirst;
            }
            catch (Exception exception)
            {
                warning = "planner-mode-ignored:unreadable:" + exception.GetType().Name;
                return PlannerMode.CastingFirst;
            }
        }

        public void Save(PlannerMode mode)
        {
            // A mode file this store refuses to read (another schema, an
            // unknown mode, unreadable) is never replaced: the toggle
            // reports why and the file keeps its bytes. An explicit classic
            // value is a successful read and may be deliberately changed.
            if (File.Exists(FilePath))
            {
                string refused;
                Load(out refused);
                if (refused.StartsWith("planner-mode-ignored", StringComparison.Ordinal))
                    throw new InvalidOperationException(FileName + " was left unchanged (" + refused +
                        "): it is unreadable or from another planner version.");
            }
            var root = new JObject
            {
                { "schemaVersion", SchemaVersion },
                { "mode", mode == PlannerMode.CastingFirst ? "casting-first" : "classic" }
            };
            Directory.CreateDirectory(_settingsDirectory);
            AtomicFile.WriteUtf8(FilePath, root.ToString(Formatting.Indented));
        }
    }
}
