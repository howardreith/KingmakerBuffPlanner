using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Domain.Authoring
{
    // How one invocation addresses its recipients. The mode must match the
    // ability's verified targeting contract; a single-target spell is always
    // DirectTarget and a group ability is always an origin mode.
    public enum CastingTargetMode
    {
        DirectTarget,
        CasterCenteredOrigin,
        AnchoredOrigin
    }

    // Draft records authored intent that is not executable yet; only Ready
    // records may execute. Disabled is the player's explicit parking of a
    // record: it stays visible with identity and intent but never blocks a
    // run — unlike a saved Draft, which is an unresolved request that
    // ordinary Apply must not silently skip. Blocking conditions are
    // compiler output, never a persisted state.
    public enum CastingAuthoringState
    {
        Draft,
        Ready,
        Disabled
    }

    public sealed class CastingOrigin
    {
        private CastingOrigin(bool casterCentered, string anchorUnitId)
        {
            IsCasterCentered = casterCentered;
            AnchorUnitId = anchorUnitId ?? string.Empty;
        }

        public bool IsCasterCentered { get; private set; }
        public string AnchorUnitId { get; private set; }

        public static CastingOrigin CasterCentered()
        {
            return new CastingOrigin(true, string.Empty);
        }

        public static CastingOrigin Anchored(string anchorUnitId)
        {
            if (string.IsNullOrWhiteSpace(anchorUnitId))
                throw new ArgumentException("Anchor unit ID is required.", "anchorUnitId");
            return new CastingOrigin(false, anchorUnitId);
        }
    }

    // A targeting modifier (for example Share Transmutation) changes recipient
    // eligibility for the casting; it never changes how many invocations the
    // casting represents.
    public sealed class TargetingModifierSelection
    {
        public TargetingModifierSelection(string modifierId, bool enabled, string exactSourceRef)
        {
            if (string.IsNullOrWhiteSpace(modifierId))
                throw new ArgumentException("Modifier ID is required.", "modifierId");
            ModifierId = modifierId;
            Enabled = enabled;
            ExactSourceRef = string.IsNullOrWhiteSpace(exactSourceRef) ? null : exactSourceRef;
        }

        public string ModifierId { get; private set; }
        public bool Enabled { get; private set; }
        public string ExactSourceRef { get; private set; }
    }

    // One selected enhancement on one casting. Selected enhancements on new
    // castings are requirements; Required=false is the explicit legacy
    // optional-intent import, never a silent downgrade.
    public sealed class AuthoredEnhancementSelection
    {
        public AuthoredEnhancementSelection(
            string enhancementId, bool required, string exactSourceRef)
        {
            if (string.IsNullOrWhiteSpace(enhancementId))
                throw new ArgumentException("Enhancement ID is required.", "enhancementId");
            EnhancementId = enhancementId;
            Required = required;
            ExactSourceRef = string.IsNullOrWhiteSpace(exactSourceRef) ? null : exactSourceRef;
        }

        public string EnhancementId { get; private set; }
        public bool Required { get; private set; }
        public string ExactSourceRef { get; private set; }
    }

    // Import provenance for castings produced by migration. Repeated
    // migration must reuse, never duplicate, the same identity.
    public sealed class MigrationProvenance
    {
        public MigrationProvenance(
            string legacyAssignmentId, int legacySchemaVersion,
            string legacyRoutineId, string note)
        {
            if (string.IsNullOrWhiteSpace(legacyAssignmentId))
                throw new ArgumentException("Legacy assignment ID is required.", "legacyAssignmentId");
            if (legacySchemaVersion < 1)
                throw new ArgumentOutOfRangeException("legacySchemaVersion");
            LegacyAssignmentId = legacyAssignmentId;
            LegacySchemaVersion = legacySchemaVersion;
            LegacyRoutineId = legacyRoutineId ?? string.Empty;
            Note = note ?? string.Empty;
        }

        public string LegacyAssignmentId { get; private set; }
        public int LegacySchemaVersion { get; private set; }
        public string LegacyRoutineId { get; private set; }
        public string Note { get; private set; }
    }

    public sealed class RoutineDefinition
    {
        public RoutineDefinition(string routineId, string name)
        {
            if (string.IsNullOrWhiteSpace(routineId))
                throw new ArgumentException("Routine ID is required.", "routineId");
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Routine name is required.", "name");
            RoutineId = routineId;
            Name = name;
        }

        public string RoutineId { get; private set; }
        public string Name { get; private set; }
    }

    // The canonical per-casting record. One Ready record means exactly one
    // invocation of its selected ability; recipients and derived coverage are
    // represented inside the record, never as sibling records.
    public sealed class PlannedCasting
    {
        public PlannedCasting(
            string castingId,
            string routineId,
            int order,
            string sourceId,
            AbilityKey ability,
            string casterUnitId,
            string spellbookGuid,
            CastingTargetMode targetMode,
            string directTargetUnitId,
            CastingOrigin origin,
            IEnumerable<string> requiredCoverageUnitIds,
            IEnumerable<TargetingModifierSelection> targetingModifiers,
            IEnumerable<AuthoredEnhancementSelection> enhancements,
            ExistingEffectPolicy existingEffectPolicy,
            IEnumerable<string> ignoredPresenceMarkers,
            CastingAuthoringState state,
            MigrationProvenance provenance)
        {
            if (string.IsNullOrWhiteSpace(castingId))
                throw new ArgumentException("Casting ID is required.", "castingId");
            if (string.IsNullOrWhiteSpace(routineId))
                throw new ArgumentException("Routine ID is required.", "routineId");
            if (order < 0) throw new ArgumentOutOfRangeException("order");
            if (string.IsNullOrWhiteSpace(sourceId))
                throw new ArgumentException("Source ID is required.", "sourceId");
            Ability = ability ?? throw new ArgumentNullException("ability");
            if (!Enum.IsDefined(typeof(CastingTargetMode), targetMode))
                throw new ArgumentOutOfRangeException("targetMode");
            if (!Enum.IsDefined(typeof(CastingAuthoringState), state))
                throw new ArgumentOutOfRangeException("state");
            // A Ready casting without a caster would silently reintroduce the
            // automatic-assignment behavior this model replaces; unresolved
            // casters stay Draft until a player resolves them.
            if (state == CastingAuthoringState.Ready && string.IsNullOrWhiteSpace(casterUnitId))
                throw new ArgumentException("A Ready casting requires an explicit caster.", "casterUnitId");
            CastingId = castingId;
            RoutineId = routineId;
            Order = order;
            SourceId = sourceId;
            CasterUnitId = string.IsNullOrWhiteSpace(casterUnitId) ? null : casterUnitId;
            SpellbookGuid = string.IsNullOrWhiteSpace(spellbookGuid) ? null : spellbookGuid;
            TargetMode = targetMode;
            DirectTargetUnitId = string.IsNullOrWhiteSpace(directTargetUnitId) ? null : directTargetUnitId;
            Origin = origin;
            RequiredCoverageUnitIds = Distinct(requiredCoverageUnitIds, "requiredCoverageUnitIds");
            TargetingModifiers = DistinctBy(
                targetingModifiers, value => value.ModifierId, "targetingModifiers");
            Enhancements = DistinctBy(
                enhancements, value => value.EnhancementId, "enhancements");
            ExistingEffectPolicy = existingEffectPolicy;
            IgnoredPresenceMarkers = Distinct(ignoredPresenceMarkers, "ignoredPresenceMarkers");
            State = state;
            Provenance = provenance;
            ValidateTargetModeShape();
        }

        // View-level edit clones: single-field replacements that preserve
        // identity, order, and every other authored intent exactly. The
        // session's UpdateFocusedCasting still validates the replacement.
        public PlannedCasting WithDirectTarget(string unitId)
        {
            return new PlannedCasting(CastingId, RoutineId, Order, SourceId,
                Ability, CasterUnitId, SpellbookGuid, TargetMode, unitId,
                Origin, RequiredCoverageUnitIds, TargetingModifiers,
                Enhancements, ExistingEffectPolicy, IgnoredPresenceMarkers,
                State, Provenance);
        }

        // Targeting-shape edit clone: mode, direct recipient, origin, and
        // required coverage change together so the replacement always
        // satisfies ValidateTargetModeShape (review G2).
        public PlannedCasting WithTargeting(
            CastingTargetMode mode,
            string directTargetUnitId,
            CastingOrigin origin,
            IEnumerable<string> requiredCoverageUnitIds)
        {
            return new PlannedCasting(CastingId, RoutineId, Order, SourceId,
                Ability, CasterUnitId, SpellbookGuid, mode, directTargetUnitId,
                origin, requiredCoverageUnitIds, TargetingModifiers,
                Enhancements, ExistingEffectPolicy, IgnoredPresenceMarkers,
                State, Provenance);
        }

        public PlannedCasting WithEnhancementSelections(
            IEnumerable<AuthoredEnhancementSelection> selections)
        {
            return new PlannedCasting(CastingId, RoutineId, Order, SourceId,
                Ability, CasterUnitId, SpellbookGuid, TargetMode,
                DirectTargetUnitId, Origin, RequiredCoverageUnitIds,
                TargetingModifiers, selections, ExistingEffectPolicy,
                IgnoredPresenceMarkers, State, Provenance);
        }

        public string CastingId { get; private set; }
        public string RoutineId { get; private set; }
        public int Order { get; private set; }
        public string SourceId { get; private set; }
        public AbilityKey Ability { get; private set; }
        public string CasterUnitId { get; private set; }
        public string SpellbookGuid { get; private set; }
        public CastingTargetMode TargetMode { get; private set; }
        public string DirectTargetUnitId { get; private set; }
        public CastingOrigin Origin { get; private set; }
        public IReadOnlyList<string> RequiredCoverageUnitIds { get; private set; }
        public IReadOnlyList<TargetingModifierSelection> TargetingModifiers { get; private set; }
        public IReadOnlyList<AuthoredEnhancementSelection> Enhancements { get; private set; }
        public ExistingEffectPolicy ExistingEffectPolicy { get; private set; }
        public IReadOnlyList<string> IgnoredPresenceMarkers { get; private set; }
        public CastingAuthoringState State { get; private set; }
        public MigrationProvenance Provenance { get; private set; }

        private void ValidateTargetModeShape()
        {
            if (TargetMode == CastingTargetMode.DirectTarget)
            {
                if (DirectTargetUnitId == null)
                    throw new ArgumentException(
                        "A direct-target casting requires its direct target.", "directTargetUnitId");
                if (Origin != null)
                    throw new ArgumentException(
                        "A direct-target casting cannot carry a group origin.", "origin");
                if (RequiredCoverageUnitIds.Count != 0)
                    throw new ArgumentException(
                        "A direct-target casting cannot carry required coverage.", "requiredCoverageUnitIds");
                return;
            }
            if (DirectTargetUnitId != null)
                throw new ArgumentException(
                    "A group casting has an origin, not a direct target.", "directTargetUnitId");
            if (Origin == null)
                throw new ArgumentException("A group casting requires an origin.", "origin");
            if (TargetMode == CastingTargetMode.CasterCenteredOrigin && !Origin.IsCasterCentered)
                throw new ArgumentException(
                    "A caster-centered casting must use the caster origin.", "origin");
            if (TargetMode == CastingTargetMode.AnchoredOrigin && Origin.IsCasterCentered)
                throw new ArgumentException(
                    "An anchored casting must select an anchor unit.", "origin");
        }

        internal static PlannedCasting WithOrder(PlannedCasting casting, int order)
        {
            if (casting.Order == order) return casting;
            return new PlannedCasting(
                casting.CastingId, casting.RoutineId, order, casting.SourceId,
                casting.Ability, casting.CasterUnitId, casting.SpellbookGuid,
                casting.TargetMode, casting.DirectTargetUnitId, casting.Origin,
                casting.RequiredCoverageUnitIds, casting.TargetingModifiers,
                casting.Enhancements, casting.ExistingEffectPolicy,
                casting.IgnoredPresenceMarkers, casting.State, casting.Provenance);
        }

        internal static IReadOnlyList<T> DistinctBy<T>(
            IEnumerable<T> values, Func<T, string> key, string label)
        {
            var list = (values ?? new T[0]).Where(value => value != null).ToList();
            if (list.Select(key).Any(string.IsNullOrWhiteSpace) ||
                list.Select(key).Distinct(StringComparer.Ordinal).Count() != list.Count)
                throw new ArgumentException("duplicate-or-empty-" + label);
            return new ReadOnlyCollection<T>(list);
        }

        private static IReadOnlyList<string> Distinct(IEnumerable<string> values, string label)
        {
            return DistinctBy((values ?? new string[0]).Where(v => v != null),
                value => value, label);
        }
    }

    // The canonical saved plan. Castings are stored in persisted order —
    // routine declaration order, then explicit order inside the routine —
    // and that order, never catalogue sorting, drives execution.
    public sealed class CastingPlanDocument
    {
        public CastingPlanDocument(
            string campaignId,
            IEnumerable<RoutineDefinition> routines,
            IEnumerable<PlannedCasting> castings)
        {
            if (string.IsNullOrWhiteSpace(campaignId))
                throw new ArgumentException("Campaign ID is required.", "campaignId");
            CampaignId = campaignId;
            var routineList = (routines ?? throw new ArgumentNullException("routines")).ToList();
            if (routineList.Count == 0)
                throw new ArgumentException("At least one routine is required.", "routines");
            RequireUnique(routineList.Select(value => value.RoutineId), "routine-id");
            Routines = new ReadOnlyCollection<RoutineDefinition>(routineList);
            var castingList = (castings ?? new PlannedCasting[0])
                .Where(value => value != null).ToList();
            RequireUnique(castingList.Select(value => value.CastingId), "casting-id");
            var routineIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < routineList.Count; i++) routineIndex[routineList[i].RoutineId] = i;
            foreach (PlannedCasting casting in castingList)
                if (!routineIndex.ContainsKey(casting.RoutineId))
                    throw new ArgumentException(
                        "Casting references an unknown routine: " + casting.CastingId, "castings");
            // Orders must be the contiguous normalized positions of the
            // persisted list, and the list must be grouped in routine
            // declaration order; otherwise execution order would depend on
            // re-sorting rather than persistence.
            var positionInRoutine = new Dictionary<string, int>(StringComparer.Ordinal);
            int lastRoutineIndex = -1;
            foreach (PlannedCasting casting in castingList)
            {
                int routineIdx = routineIndex[casting.RoutineId];
                if (routineIdx < lastRoutineIndex)
                    throw new ArgumentException(
                        "Castings must be grouped in routine declaration order: " + casting.CastingId,
                        "castings");
                lastRoutineIndex = routineIdx;
                int prior;
                int position = positionInRoutine.TryGetValue(casting.RoutineId, out prior)
                    ? prior + 1 : 0;
                positionInRoutine[casting.RoutineId] = position;
                if (casting.Order != position)
                    throw new ArgumentException(
                        "Casting order does not match persisted position: " + casting.CastingId,
                        "castings");
            }
            Castings = new ReadOnlyCollection<PlannedCasting>(castingList);
        }

        public string CampaignId { get; private set; }
        public IReadOnlyList<RoutineDefinition> Routines { get; private set; }
        public IReadOnlyList<PlannedCasting> Castings { get; private set; }

        private static void RequireUnique(IEnumerable<string> values, string label)
        {
            var list = values.ToList();
            if (list.Any(string.IsNullOrWhiteSpace) ||
                list.Distinct(StringComparer.Ordinal).Count() != list.Count)
                throw new ArgumentException("duplicate-or-empty-" + label);
        }
    }
}
