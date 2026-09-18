using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.Persistence
{
    public sealed class BuffPlannerProfile
    {
        internal const int CurrentSchemaVersion = 5;

        [JsonProperty("schemaVersion", Required = Required.Always, Order = 1)]
        public int SchemaVersion { get; set; }
        [JsonProperty("campaignId", Required = Required.Always, Order = 2)]
        public string CampaignId { get; set; }
        [JsonProperty("routines", Required = Required.Always, Order = 3)]
        public List<RoutineProfile> Routines { get; set; }
        [JsonProperty("providerPreferences", Required = Required.Always, Order = 4)]
        public List<ProviderPreferenceProfile> ProviderPreferences { get; set; }
        [JsonProperty("hiddenSourceIds", Required = Required.Always, Order = 5)]
        public List<string> HiddenSourceIds { get; set; }
        [JsonProperty("ui", Required = Required.Always, Order = 6)]
        public UiProfile Ui { get; set; }
        [JsonProperty("execution", Required = Required.Always, Order = 7)]
        public ExecutionProfile Execution { get; set; }

        public static BuffPlannerProfile CreateDefault(string campaignId)
        {
            return new BuffPlannerProfile
            {
                SchemaVersion = CurrentSchemaVersion,
                CampaignId = campaignId,
                Routines = new List<RoutineProfile>
                {
                    RoutineProfile.Empty("long", "Long"),
                    RoutineProfile.Empty("important", "Important"),
                    RoutineProfile.Empty("short", "Short")
                },
                ProviderPreferences = new List<ProviderPreferenceProfile>(),
                HiddenSourceIds = new List<string>(),
                Ui = UiProfile.Default(),
                Execution = ExecutionProfile.Default()
            };
        }
    }

    public sealed class RoutineProfile
    {
        [JsonProperty("routineId", Required = Required.Always, Order = 1)] public string RoutineId { get; set; }
        [JsonProperty("name", Required = Required.Always, Order = 2)] public string Name { get; set; }
        [JsonProperty("assignments", Required = Required.Always, Order = 3)]
        public List<SourceAssignmentProfile> Assignments { get; set; }

        internal static RoutineProfile Empty(string id, string name)
        {
            return new RoutineProfile { RoutineId = id, Name = name, Assignments = new List<SourceAssignmentProfile>() };
        }
    }

    public sealed class SourceAssignmentProfile
    {
        [JsonProperty("sourceId", Required = Required.Always, Order = 1)] public string SourceId { get; set; }
        [JsonProperty("ability", Required = Required.Always, Order = 2)] public AbilityKeyProfile Ability { get; set; }
        [JsonProperty("existingEffectPolicy", Required = Required.Always, Order = 3)]
        public ExistingEffectPolicy ExistingEffectPolicy { get; set; }
        [JsonProperty("ignoredPresenceMarkers", Required = Required.Always, Order = 4)]
        public List<string> IgnoredPresenceMarkers { get; set; }
        [JsonProperty("castingAssignments", Required = Required.Always, Order = 5)]
        public List<CastingAssignmentProfile> CastingAssignments { get; set; }

        internal static SourceAssignmentProfile Create(string sourceId, AbilityKey ability)
        {
            return new SourceAssignmentProfile
            {
                SourceId = sourceId,
                Ability = AbilityKeyProfile.FromKey(ability),
                ExistingEffectPolicy = ExistingEffectPolicy.SkipAlreadyActive,
                IgnoredPresenceMarkers = new List<string>(),
                CastingAssignments = new List<CastingAssignmentProfile>()
            };
        }

        // The simple single-caster workflow edits exactly one Automatic
        // child; this is the derived entry point for it, not a second
        // writable copy of child state.
        internal CastingAssignmentProfile AutomaticAssignment
        {
            get
            {
                return CastingAssignments.FirstOrDefault(child =>
                    child.IsAutomatic && child.TargetUnitIds.Count == 0 &&
                    child.Enhancements.Count == 0) ??
                    CastingAssignments.FirstOrDefault(child => child.IsAutomatic);
            }
        }

        // Source-level membership summaries are derived from the children and
        // materialized for readers; they are never a second writable copy and
        // never persisted.
        [JsonIgnore]
        public IReadOnlyList<string> WantedTargetUnitIds
        {
            get
            {
                return new ReadOnlyCollection<string>(CastingAssignments
                    .SelectMany(child => child.TargetUnitIds)
                    .Distinct(StringComparer.Ordinal).ToList());
            }
        }

        [JsonIgnore]
        public IReadOnlyList<string> SelectedEnhancementIds
        {
            get
            {
                return new ReadOnlyCollection<string>(CastingAssignments
                    .SelectMany(child => child.Enhancements)
                    .Select(selection => selection.EnhancementId)
                    .Distinct(StringComparer.Ordinal).ToList());
            }
        }
    }

    public sealed class CastingAssignmentProfile
    {
        [JsonProperty("assignmentId", Required = Required.Always, Order = 1)]
        public string AssignmentId { get; set; }
        [JsonProperty("order", Required = Required.Always, Order = 2)]
        public int Order { get; set; }
        [JsonProperty("casterUnitId", Required = Required.AllowNull, Order = 3)]
        public string CasterUnitId { get; set; }
        [JsonProperty("spellbookGuid", Required = Required.AllowNull, Order = 4)]
        public string SpellbookGuid { get; set; }
        [JsonProperty("providerKey", Required = Required.AllowNull, Order = 5)]
        public string ProviderKey { get; set; }
        [JsonProperty("targetUnitIds", Required = Required.Always, Order = 6)]
        public List<string> TargetUnitIds { get; set; }
        [JsonProperty("enhancements", Required = Required.Always, Order = 7)]
        public List<EnhancementSelectionProfile> Enhancements { get; set; }

        // Automatic means no pin: the planner chooses the caster, spellbook,
        // and provider using the existing ranking rules.
        [JsonIgnore]
        public bool IsAutomatic
        {
            get
            {
                return string.IsNullOrWhiteSpace(CasterUnitId) &&
                    string.IsNullOrWhiteSpace(ProviderKey) &&
                    string.IsNullOrWhiteSpace(SpellbookGuid);
            }
        }

        internal static CastingAssignmentProfile CreateAutomatic(string assignmentId, int order)
        {
            return new CastingAssignmentProfile
            {
                AssignmentId = assignmentId,
                Order = order,
                CasterUnitId = null,
                SpellbookGuid = null,
                ProviderKey = null,
                TargetUnitIds = new List<string>(),
                Enhancements = new List<EnhancementSelectionProfile>()
            };
        }
    }

    public sealed class EnhancementSelectionProfile
    {
        [JsonProperty("enhancementId", Required = Newtonsoft.Json.Required.Always, Order = 1)]
        public string EnhancementId { get; set; }
        // Required is the default policy. Required.AllowNull keeps "cast
        // without this enhancement when unavailable" an explicit opt-in that
        // never silently substitutes a different item or caster.
        [JsonProperty("required", Required = Newtonsoft.Json.Required.AllowNull, Order = 2)]
        public bool? Required { get; set; }

        public bool IsRequired
        {
            get { return Required != false; }
        }
    }

    public sealed class AbilityKeyProfile
    {
        [JsonProperty("baseAbilityGuid", Required = Required.Always, Order = 1)] public string BaseAbilityGuid { get; set; }
        [JsonProperty("variantGuid", Required = Required.Always, Order = 2)] public string VariantGuid { get; set; }
        [JsonProperty("metamagicMask", Required = Required.Always, Order = 3)] public int MetamagicMask { get; set; }
        [JsonProperty("sourceKind", Required = Required.Always, Order = 4)] public SourceKind SourceKind { get; set; }
        [JsonProperty("specialSourceId", Required = Required.Always, Order = 5)] public string SpecialSourceId { get; set; }

        public static AbilityKeyProfile FromKey(AbilityKey key)
        {
            if (key == null) throw new ArgumentNullException("key");
            return new AbilityKeyProfile
            {
                BaseAbilityGuid = key.BaseAbilityGuid,
                VariantGuid = key.VariantGuid,
                MetamagicMask = key.MetamagicMask,
                SourceKind = key.SourceKind,
                SpecialSourceId = key.SpecialSourceId
            };
        }

        public AbilityKey ToKey()
        {
            return new AbilityKey(BaseAbilityGuid, VariantGuid, MetamagicMask, SourceKind, SpecialSourceId);
        }
    }

    public sealed class ProviderPreferenceProfile
    {
        [JsonProperty("providerKey", Required = Required.Always, Order = 1)] public string ProviderKey { get; set; }
        [JsonProperty("banned", Required = Required.Always, Order = 2)] public bool Banned { get; set; }
        [JsonProperty("priority", Required = Required.AllowNull, Order = 3)] public int? Priority { get; set; }
        [JsonProperty("maximumCasts", Required = Required.AllowNull, Order = 4)] public int? MaximumCasts { get; set; }
    }

    public sealed class UiProfile
    {
        [JsonProperty("scale", Required = Required.Always, Order = 1)] public float Scale { get; set; }
        [JsonProperty("hotkey", Required = Required.Always, Order = 2)] public string Hotkey { get; set; }

        internal static UiProfile Default()
        {
            return new UiProfile { Scale = 1.0f, Hotkey = "Ctrl+Shift+B" };
        }
    }

    public sealed class ExecutionProfile
    {
        [JsonProperty("mode", Required = Required.Always, Order = 1)] public string Mode { get; set; }
        [JsonProperty("allowAnimatedFallback", Required = Required.Always, Order = 2)] public bool AllowAnimatedFallback { get; set; }
        [JsonProperty("outOfCombatOnly", Required = Required.Always, Order = 3)] public bool OutOfCombatOnly { get; set; }
        [JsonProperty("recastExisting", Required = Required.Always, Order = 4)] public bool RecastExisting { get; set; }

        internal static ExecutionProfile Default()
        {
            return new ExecutionProfile
            {
                Mode = "animated",
                AllowAnimatedFallback = true,
                OutOfCombatOnly = true,
                RecastExisting = false
            };
        }
    }
}
