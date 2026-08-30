using System;
using System.Collections.Generic;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.Persistence
{
    public sealed class BuffPlannerProfile
    {
        internal const int CurrentSchemaVersion = 4;

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
        [JsonProperty("wantedTargetUnitIds", Required = Required.Always, Order = 3)]
        public List<string> WantedTargetUnitIds { get; set; }
        [JsonProperty("existingEffectPolicy", Required = Required.Always, Order = 4)]
        public ExistingEffectPolicy ExistingEffectPolicy { get; set; }
        [JsonProperty("ignoredPresenceMarkers", Required = Required.Always, Order = 5)]
        public List<string> IgnoredPresenceMarkers { get; set; }
        [JsonProperty("selectedEnhancementIds", Required = Required.Always, Order = 6)]
        public List<string> SelectedEnhancementIds { get; set; }
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
