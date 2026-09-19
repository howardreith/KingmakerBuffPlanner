using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Persistence
{
    public sealed class CastingImportMapping
    {
        internal CastingImportMapping(
            string legacyAssignmentId,
            string legacyRoutineId,
            IReadOnlyList<string> producedCastingIds,
            string disposition)
        {
            LegacyAssignmentId = legacyAssignmentId;
            LegacyRoutineId = legacyRoutineId;
            ProducedCastingIds = producedCastingIds;
            Disposition = disposition ?? string.Empty;
        }

        public string LegacyAssignmentId { get; private set; }
        public string LegacyRoutineId { get; private set; }
        public IReadOnlyList<string> ProducedCastingIds { get; private set; }
        public string Disposition { get; private set; }
    }

    public sealed class CastingImportReport
    {
        internal CastingImportReport(
            int legacyRoutineCount,
            int legacyChildCount,
            int resultingCastingCount,
            int readyCount,
            int draftCount,
            int unresolvedCasterCount,
            int groupReviewCount,
            int pooledEnhancementCount,
            int policyNoticeCount,
            IEnumerable<CastingImportMapping> mappings,
            IEnumerable<string> warnings)
        {
            LegacyRoutineCount = legacyRoutineCount;
            LegacyChildCount = legacyChildCount;
            ResultingCastingCount = resultingCastingCount;
            ReadyCount = readyCount;
            DraftCount = draftCount;
            UnresolvedCasterCount = unresolvedCasterCount;
            GroupReviewCount = groupReviewCount;
            PooledEnhancementCount = pooledEnhancementCount;
            PolicyNoticeCount = policyNoticeCount;
            Mappings = new ReadOnlyCollection<CastingImportMapping>(mappings.ToList());
            Warnings = new ReadOnlyCollection<string>(warnings
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal)
                .ToList());
        }

        public int LegacyRoutineCount { get; private set; }
        public int LegacyChildCount { get; private set; }
        public int ResultingCastingCount { get; private set; }
        public int ReadyCount { get; private set; }
        public int DraftCount { get; private set; }
        public int UnresolvedCasterCount { get; private set; }
        public int GroupReviewCount { get; private set; }
        public int PooledEnhancementCount { get; private set; }
        public int PolicyNoticeCount { get; private set; }
        public IReadOnlyList<CastingImportMapping> Mappings { get; private set; }
        public IReadOnlyList<string> Warnings { get; private set; }
    }

    public sealed class CastingImportResult
    {
        internal CastingImportResult(
            CastingPlanDocument document, CastingImportReport report)
        {
            Document = document;
            Report = report;
        }

        public CastingPlanDocument Document { get; private set; }
        public CastingImportReport Report { get; private set; }
    }

    // Converts a schema-5 legacy profile into the canonical casting document
    // following the charter conversion rules. Conversion is deterministic
    // and idempotent: casting identities derive from legacy assignment
    // provenance, and importing the same legacy data onto a document that
    // already holds those identities reuses them instead of duplicating.
    //
    //   Pinned single-target children split into one casting per recipient,
    //   preserving target order, enhancements, policy, and relative routine
    //   order. Automatic children become review drafts with their targets
    //   preserved and an unresolved caster. Group children keep their
    //   requested coverage but stay drafts pending origin/count review.
    //   Pooled rod selections stay pooled (no invented exact identity) and
    //   are counted for review.
    public sealed class CastingPlanImporter
    {
        private const string PooledRodNote = "pooled-rod-exact-source-pending";
        private const string GroupReviewNote = "group-origin-and-count-pending-review";
        private const string AutomaticNote = "automatic-caster-pending-review";
        private const string ProviderPinNote = "provider-pin-without-caster-pending-review";

        public CastingImportResult Import(
            BuffPlannerProfile legacy,
            CastingPlanDocument existing = null,
            IDictionary<string, CastGroupingKind> groupingsBySourceId = null)
        {
            if (legacy == null) throw new ArgumentNullException("legacy");
            var groupingMap = groupingsBySourceId ??
                new Dictionary<string, CastGroupingKind>(StringComparer.Ordinal);
            CastingPlanDocument baseDocument = existing ?? DefaultDocument(legacy);
            var warnings = new List<string>();
            // Legacy children carry an explicit per-routine order; that
            // order — never source or catalogue iteration — drives the new
            // persisted order.
            var ordered = new List<LegacyChild>();
            foreach (RoutineProfile routine in legacy.Routines)
            {
                if (baseDocument.Routines.All(value =>
                        value.RoutineId != routine.RoutineId))
                {
                    warnings.Add("legacy-routine-unknown:" + routine.RoutineId);
                    continue;
                }
                var children = new List<LegacyChild>();
                foreach (SourceAssignmentProfile assignment in routine.Assignments)
                    foreach (CastingAssignmentProfile child in assignment.CastingAssignments)
                        children.Add(new LegacyChild(routine.RoutineId, assignment, child));
                children.Sort((left, right) =>
                {
                    int byOrder = left.Child.Order.CompareTo(right.Child.Order);
                    return byOrder != 0 ? byOrder
                        : string.Compare(left.Child.AssignmentId,
                            right.Child.AssignmentId, StringComparison.Ordinal);
                });
                if (children.Select(value => value.Child.Order).Distinct().Count() !=
                    children.Count)
                    warnings.Add("legacy-order-not-unique:" + routine.RoutineId);
                ordered.AddRange(children);
            }
            var existingIds = new HashSet<string>(
                baseDocument.Castings.Select(value => value.CastingId),
                StringComparer.Ordinal);
            var produced = new List<PlannedCasting>();
            var mappings = new List<CastingImportMapping>();
            int pooledEnhancements = 0;
            int groupReviews = 0;
            int unresolvedCasters = 0;
            foreach (LegacyChild entry in ordered)
            {
                bool isGroup = IsGroup(entry.Assignment, groupingMap);
                if (!isGroup && entry.Child.TargetUnitIds.Count == 0)
                {
                    // A legacy child without recipients cannot become a
                    // casting (the explicit model requires a recipient or an
                    // origin); it stays a visible unresolved import item
                    // instead of a phantom record or a silent drop.
                    warnings.Add("legacy-child-without-target:" + entry.Child.AssignmentId);
                    mappings.Add(new CastingImportMapping(
                        entry.Child.AssignmentId, entry.RoutineId,
                        new string[0], "unresolved-no-recipient"));
                    continue;
                }
                int recipients = isGroup ? 1 : entry.Child.TargetUnitIds.Count;
                var castingIds = new List<string>();
                bool reusedExisting = false;
                for (int targetIndex = 0; targetIndex < recipients; targetIndex++)
                {
                    string castingId = MigrationCastingId(entry.Child.AssignmentId,
                        isGroup ? -1 : targetIndex);
                    if (existingIds.Contains(castingId))
                    {
                        // Idempotent re-import: the record already exists with
                        // this provenance identity and is never duplicated.
                        reusedExisting = true;
                        castingIds.Add(castingId);
                        continue;
                    }
                    produced.Add(BuildCasting(entry, isGroup, targetIndex,
                        ref pooledEnhancements, ref groupReviews,
                        ref unresolvedCasters));
                    castingIds.Add(castingId);
                }
                mappings.Add(new CastingImportMapping(
                    entry.Child.AssignmentId, entry.RoutineId, castingIds,
                    reusedExisting ? "reused" : "imported"));
            }
            // Newly produced castings may belong to any routine while the
            // existing document already holds others; a stable sort by
            // routine rank keeps the persisted list routine-major without
            // disturbing relative order inside a routine, and orders are
            // re-derived from the final persisted positions.
            var routineRank = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < baseDocument.Routines.Count; i++)
                routineRank[baseDocument.Routines[i].RoutineId] = i;
            var positionInRoutine = new Dictionary<string, int>(StringComparer.Ordinal);
            List<PlannedCasting> normalized = baseDocument.Castings.Concat(produced)
                .OrderBy(value => routineRank[value.RoutineId])
                .Select(value =>
                {
                    int prior;
                    int position = positionInRoutine.TryGetValue(
                        value.RoutineId, out prior) ? prior + 1 : 0;
                    positionInRoutine[value.RoutineId] = position;
                    return PlannedCasting.WithOrder(value, position);
                })
                .ToList();
            var document = new CastingPlanDocument(
                baseDocument.CampaignId, baseDocument.Routines, normalized);
            int ready = document.Castings.Count(
                value => value.State == CastingAuthoringState.Ready);
            var report = new CastingImportReport(
                legacy.Routines.Count, ordered.Count, document.Castings.Count,
                ready, document.Castings.Count - ready, unresolvedCasters,
                groupReviews, pooledEnhancements, legacy.ProviderPreferences.Count,
                mappings, warnings);
            return new CastingImportResult(document, report);
        }

        private static CastingPlanDocument DefaultDocument(BuffPlannerProfile legacy)
        {
            return new CastingPlanDocument(legacy.CampaignId,
                new[]
                {
                    new RoutineDefinition("long", "Long"),
                    new RoutineDefinition("important", "Important"),
                    new RoutineDefinition("short", "Short")
                },
                new PlannedCasting[0]);
        }

        private static bool IsGroup(
            SourceAssignmentProfile assignment,
            IDictionary<string, CastGroupingKind> groupingsBySourceId)
        {
            CastGroupingKind grouping;
            return groupingsBySourceId.TryGetValue(assignment.SourceId, out grouping) &&
                grouping == CastGroupingKind.MassConfiguredTargets;
        }

        private static PlannedCasting BuildCasting(
            LegacyChild entry,
            bool group,
            int targetIndex,
            ref int pooledEnhancements,
            ref int groupReviews,
            ref int unresolvedCasters)
        {
            CastingAssignmentProfile child = entry.Child;
            string note = string.Empty;
            string casterUnitId = child.CasterUnitId;
            if (child.IsAutomatic)
            {
                // An automatic legacy choice becomes a review draft; today's
                // best caster is never silently pinned as player intent.
                note = AutomaticNote;
                unresolvedCasters++;
            }
            else if (casterUnitId == null)
            {
                // A provider/spellbook pin without a caster unit cannot
                // resolve to an explicit caster; it stays a review draft
                // rather than being dropped or guessed.
                note = ProviderPinNote;
                unresolvedCasters++;
            }
            var enhancements = new List<AuthoredEnhancementSelection>();
            foreach (EnhancementSelectionProfile selection in child.Enhancements)
            {
                enhancements.Add(new AuthoredEnhancementSelection(
                    selection.EnhancementId, selection.IsRequired, null));
                pooledEnhancements++;
            }
            string directTarget = null;
            var coverage = new List<string>();
            CastingTargetMode mode;
            CastingOrigin origin = null;
            if (group)
            {
                mode = CastingTargetMode.CasterCenteredOrigin;
                origin = CastingOrigin.CasterCentered();
                coverage.AddRange(child.TargetUnitIds);
                // The legacy profile never authored a group origin or cast
                // count; both stay pending explicit review.
                note = GroupReviewNote;
                groupReviews++;
            }
            else
            {
                mode = CastingTargetMode.DirectTarget;
                directTarget = child.TargetUnitIds[targetIndex];
            }
            bool ready = !group && casterUnitId != null;
            var notes = new List<string>();
            if (!string.IsNullOrEmpty(note)) notes.Add(note);
            if (enhancements.Count != 0) notes.Add(PooledRodNote);
            var provenance = new MigrationProvenance(
                child.AssignmentId, 5, entry.RoutineId,
                string.Join("|", notes));
            return new PlannedCasting(
                MigrationCastingId(child.AssignmentId, group ? -1 : targetIndex),
                entry.RoutineId, 0, entry.Assignment.SourceId,
                entry.Assignment.Ability.ToKey(), casterUnitId, child.SpellbookGuid,
                mode, directTarget, origin, coverage, null, enhancements,
                entry.Assignment.ExistingEffectPolicy,
                entry.Assignment.IgnoredPresenceMarkers,
                ready ? CastingAuthoringState.Ready : CastingAuthoringState.Draft,
                provenance);
        }

        internal static string MigrationCastingId(string legacyAssignmentId, int targetIndex)
        {
            if (string.IsNullOrWhiteSpace(legacyAssignmentId))
                throw new ArgumentException(
                    "Legacy assignment ID is required.", "legacyAssignmentId");
            // Deterministic identity from provenance: repeated import of the
            // same legacy child (and recipient index) yields the same ID, so
            // idempotent re-import maps onto the same records instead of
            // duplicating them.
            return "m5:" + legacyAssignmentId + ":" +
                (targetIndex < 0 ? "group" : targetIndex.ToString());
        }

        private sealed class LegacyChild
        {
            public LegacyChild(string routineId,
                SourceAssignmentProfile assignment, CastingAssignmentProfile child)
            {
                RoutineId = routineId;
                Assignment = assignment;
                Child = child;
            }

            public string RoutineId { get; private set; }
            public SourceAssignmentProfile Assignment { get; private set; }
            public CastingAssignmentProfile Child { get; private set; }
        }
    }
}
