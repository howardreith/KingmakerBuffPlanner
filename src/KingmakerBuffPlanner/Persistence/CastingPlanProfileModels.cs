using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.Persistence
{
    // Schema 6: the casting-first canonical saved plan. One persisted casting
    // is one authored invocation with explicit caster, source identity,
    // target/origin mode, coverage intent, and enhancement selections. The
    // file is candidate storage beside the legacy schema-5 profile until
    // cutover; the two never share a writer.
    public sealed class CastingPlanProfile
    {
        internal const int CurrentSchemaVersion = 6;

        [JsonProperty("schemaVersion", Required = Required.Always, Order = 1)]
        public int SchemaVersion { get; set; }
        [JsonProperty("campaignId", Required = Required.Always, Order = 2)]
        public string CampaignId { get; set; }
        [JsonProperty("routines", Required = Required.Always, Order = 3)]
        public List<RoutineDefinitionProfile> Routines { get; set; }
        [JsonProperty("castings", Required = Required.Always, Order = 4)]
        public List<PlannedCastingProfile> Castings { get; set; }
        [JsonProperty("ui", Required = Required.Always, Order = 5)]
        public UiProfile Ui { get; set; }
        [JsonProperty("execution", Required = Required.Always, Order = 6)]
        public ExecutionProfile Execution { get; set; }

        public static CastingPlanProfile CreateDefault(string campaignId)
        {
            return new CastingPlanProfile
            {
                SchemaVersion = CurrentSchemaVersion,
                CampaignId = campaignId,
                Routines = new List<RoutineDefinitionProfile>
                {
                    RoutineDefinitionProfile.FromDomain(new RoutineDefinition("long", "Long")),
                    RoutineDefinitionProfile.FromDomain(new RoutineDefinition("important", "Important")),
                    RoutineDefinitionProfile.FromDomain(new RoutineDefinition("short", "Short"))
                },
                Castings = new List<PlannedCastingProfile>(),
                Ui = UiProfile.Default(),
                Execution = ExecutionProfile.Default()
            };
        }

        public CastingPlanDocument ToDocument()
        {
            return new CastingPlanDocument(
                CampaignId,
                Routines.Select(value => value.ToDomain()),
                Castings.Select(value => value.ToDomain()));
        }

        public static CastingPlanProfile FromDocument(
            CastingPlanDocument document, UiProfile ui = null, ExecutionProfile execution = null)
        {
            if (document == null) throw new ArgumentNullException("document");
            return new CastingPlanProfile
            {
                SchemaVersion = CurrentSchemaVersion,
                CampaignId = document.CampaignId,
                Routines = document.Routines
                    .Select(value => RoutineDefinitionProfile.FromDomain(value)).ToList(),
                Castings = document.Castings
                    .Select(value => PlannedCastingProfile.FromDomain(value)).ToList(),
                Ui = ui ?? UiProfile.Default(),
                Execution = execution ?? ExecutionProfile.Default()
            };
        }
    }

    public sealed class RoutineDefinitionProfile
    {
        [JsonProperty("routineId", Required = Required.Always, Order = 1)]
        public string RoutineId { get; set; }
        [JsonProperty("name", Required = Required.Always, Order = 2)]
        public string Name { get; set; }

        internal static RoutineDefinitionProfile FromDomain(RoutineDefinition routine)
        {
            return new RoutineDefinitionProfile
            {
                RoutineId = routine.RoutineId,
                Name = routine.Name
            };
        }

        internal RoutineDefinition ToDomain()
        {
            return new RoutineDefinition(RoutineId, Name);
        }
    }

    public sealed class PlannedCastingProfile
    {
        [JsonProperty("castingId", Required = Required.Always, Order = 1)]
        public string CastingId { get; set; }
        [JsonProperty("routineId", Required = Required.Always, Order = 2)]
        public string RoutineId { get; set; }
        [JsonProperty("order", Required = Required.Always, Order = 3)]
        public int Order { get; set; }
        [JsonProperty("sourceId", Required = Required.Always, Order = 4)]
        public string SourceId { get; set; }
        [JsonProperty("ability", Required = Required.Always, Order = 5)]
        public AbilityKeyProfile Ability { get; set; }
        [JsonProperty("casterUnitId", Required = Required.AllowNull, Order = 6)]
        public string CasterUnitId { get; set; }
        [JsonProperty("spellbookGuid", Required = Required.AllowNull, Order = 7)]
        public string SpellbookGuid { get; set; }
        [JsonProperty("targetMode", Required = Required.Always, Order = 8)]
        public CastingTargetMode TargetMode { get; set; }
        [JsonProperty("directTargetUnitId", Required = Required.AllowNull, Order = 9)]
        public string DirectTargetUnitId { get; set; }
        [JsonProperty("origin", Required = Required.AllowNull, Order = 10)]
        public CastingOriginProfile Origin { get; set; }
        [JsonProperty("requiredCoverageUnitIds", Required = Required.Always, Order = 11)]
        public List<string> RequiredCoverageUnitIds { get; set; }
        [JsonProperty("targetingModifiers", Required = Required.Always, Order = 12)]
        public List<TargetingModifierSelectionProfile> TargetingModifiers { get; set; }
        [JsonProperty("enhancements", Required = Required.Always, Order = 13)]
        public List<AuthoredEnhancementSelectionProfile> Enhancements { get; set; }
        [JsonProperty("existingEffectPolicy", Required = Required.Always, Order = 14)]
        public ExistingEffectPolicy ExistingEffectPolicy { get; set; }
        [JsonProperty("ignoredPresenceMarkers", Required = Required.Always, Order = 15)]
        public List<string> IgnoredPresenceMarkers { get; set; }
        [JsonProperty("state", Required = Required.Always, Order = 16)]
        public CastingAuthoringState State { get; set; }
        [JsonProperty("provenance", Required = Required.AllowNull, Order = 17)]
        public MigrationProvenanceProfile Provenance { get; set; }

        internal static PlannedCastingProfile FromDomain(PlannedCasting casting)
        {
            return new PlannedCastingProfile
            {
                CastingId = casting.CastingId,
                RoutineId = casting.RoutineId,
                Order = casting.Order,
                SourceId = casting.SourceId,
                Ability = AbilityKeyProfile.FromKey(casting.Ability),
                CasterUnitId = casting.CasterUnitId,
                SpellbookGuid = casting.SpellbookGuid,
                TargetMode = casting.TargetMode,
                DirectTargetUnitId = casting.DirectTargetUnitId,
                Origin = CastingOriginProfile.FromDomain(casting.Origin),
                RequiredCoverageUnitIds = casting.RequiredCoverageUnitIds.ToList(),
                TargetingModifiers = casting.TargetingModifiers
                    .Select(TargetingModifierSelectionProfile.FromDomain).ToList(),
                Enhancements = casting.Enhancements
                    .Select(AuthoredEnhancementSelectionProfile.FromDomain).ToList(),
                ExistingEffectPolicy = casting.ExistingEffectPolicy,
                IgnoredPresenceMarkers = casting.IgnoredPresenceMarkers.ToList(),
                State = casting.State,
                Provenance = MigrationProvenanceProfile.FromDomain(casting.Provenance)
            };
        }

        internal PlannedCasting ToDomain()
        {
            return new PlannedCasting(
                CastingId, RoutineId, Order, SourceId, Ability.ToKey(),
                CasterUnitId, SpellbookGuid, TargetMode, DirectTargetUnitId,
                Origin == null ? null : Origin.ToDomain(),
                RequiredCoverageUnitIds,
                (TargetingModifiers ?? new List<TargetingModifierSelectionProfile>())
                    .Select(value => value.ToDomain()),
                (Enhancements ?? new List<AuthoredEnhancementSelectionProfile>())
                    .Select(value => value.ToDomain()),
                ExistingEffectPolicy, IgnoredPresenceMarkers, State,
                Provenance == null ? null : Provenance.ToDomain());
        }
    }

    public sealed class CastingOriginProfile
    {
        [JsonProperty("casterCentered", Required = Required.Always, Order = 1)]
        public bool CasterCentered { get; set; }
        [JsonProperty("anchorUnitId", Required = Required.AllowNull, Order = 2)]
        public string AnchorUnitId { get; set; }

        internal static CastingOriginProfile FromDomain(CastingOrigin origin)
        {
            return origin == null ? null : new CastingOriginProfile
            {
                CasterCentered = origin.IsCasterCentered,
                AnchorUnitId = origin.AnchorUnitId
            };
        }

        internal CastingOrigin ToDomain()
        {
            return CasterCentered
                ? CastingOrigin.CasterCentered()
                : CastingOrigin.Anchored(AnchorUnitId);
        }
    }

    public sealed class TargetingModifierSelectionProfile
    {
        [JsonProperty("modifierId", Required = Required.Always, Order = 1)]
        public string ModifierId { get; set; }
        [JsonProperty("enabled", Required = Required.Always, Order = 2)]
        public bool Enabled { get; set; }
        [JsonProperty("exactSourceRef", Required = Required.AllowNull, Order = 3)]
        public string ExactSourceRef { get; set; }

        internal static TargetingModifierSelectionProfile FromDomain(
            TargetingModifierSelection selection)
        {
            return new TargetingModifierSelectionProfile
            {
                ModifierId = selection.ModifierId,
                Enabled = selection.Enabled,
                ExactSourceRef = selection.ExactSourceRef
            };
        }

        internal TargetingModifierSelection ToDomain()
        {
            return new TargetingModifierSelection(ModifierId, Enabled, ExactSourceRef);
        }
    }

    public sealed class AuthoredEnhancementSelectionProfile
    {
        [JsonProperty("enhancementId", Required = Newtonsoft.Json.Required.Always, Order = 1)]
        public string EnhancementId { get; set; }
        [JsonProperty("required", Required = Newtonsoft.Json.Required.Always, Order = 2)]
        public bool Required { get; set; }
        [JsonProperty("exactSourceRef", Required = Newtonsoft.Json.Required.AllowNull, Order = 3)]
        public string ExactSourceRef { get; set; }

        internal static AuthoredEnhancementSelectionProfile FromDomain(
            AuthoredEnhancementSelection selection)
        {
            return new AuthoredEnhancementSelectionProfile
            {
                EnhancementId = selection.EnhancementId,
                Required = selection.Required,
                ExactSourceRef = selection.ExactSourceRef
            };
        }

        internal AuthoredEnhancementSelection ToDomain()
        {
            return new AuthoredEnhancementSelection(EnhancementId, Required, ExactSourceRef);
        }
    }

    public sealed class MigrationProvenanceProfile
    {
        [JsonProperty("legacyAssignmentId", Required = Required.Always, Order = 1)]
        public string LegacyAssignmentId { get; set; }
        [JsonProperty("legacySchemaVersion", Required = Required.Always, Order = 2)]
        public int LegacySchemaVersion { get; set; }
        [JsonProperty("legacyRoutineId", Required = Required.AllowNull, Order = 3)]
        public string LegacyRoutineId { get; set; }
        [JsonProperty("note", Required = Required.AllowNull, Order = 4)]
        public string Note { get; set; }

        internal static MigrationProvenanceProfile FromDomain(MigrationProvenance provenance)
        {
            return provenance == null ? null : new MigrationProvenanceProfile
            {
                LegacyAssignmentId = provenance.LegacyAssignmentId,
                LegacySchemaVersion = provenance.LegacySchemaVersion,
                LegacyRoutineId = provenance.LegacyRoutineId,
                Note = provenance.Note
            };
        }

        internal MigrationProvenance ToDomain()
        {
            return new MigrationProvenance(
                LegacyAssignmentId, LegacySchemaVersion, LegacyRoutineId, Note);
        }
    }
}
