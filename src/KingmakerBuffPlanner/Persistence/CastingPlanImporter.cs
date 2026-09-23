using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
            // Review K2: identity is (routine, legacy assignment id,
            // recipient key). A record already in the candidate is reused
            // only when its persisted provenance matches that identity
            // exactly; any other ID collision refuses the import.
            var allIds = new HashSet<string>(
                baseDocument.Castings.Select(value => value.CastingId),
                StringComparer.Ordinal);
            var existingByIdentity = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (PlannedCasting casting in baseDocument.Castings)
            {
                if (casting.Provenance == null) continue;
                string key = casting.Provenance.LegacyRecipientKey;
                if (string.IsNullOrEmpty(key))
                    // Records imported before the recipient key existed:
                    // their identity is recoverable from the record itself.
                    key = casting.DirectTargetUnitId ?? "group";
                existingByIdentity[ImportIdentity(casting.Provenance.LegacyRoutineId,
                    casting.Provenance.LegacyAssignmentId, key)] = casting.CastingId;
            }
            var produced = new List<PlannedCasting>();
            var mappings = new List<CastingImportMapping>();
            int pooledEnhancements = 0;
            int groupReviews = 0;
            int unresolvedCasters = 0;
            foreach (LegacyChild entry in ordered)
            {
                CastGroupingKind grouping;
                bool groupingKnown = groupingMap.TryGetValue(
                    entry.Assignment.SourceId, out grouping);
                var recipientKeys = new List<string>();
                if (entry.Child.TargetUnitIds.Count == 0)
                {
                    // Kept as a durable Draft with a review item — never a
                    // silent drop or a phantom recipient.
                    warnings.Add("legacy-child-without-target:" + entry.Child.AssignmentId);
                    recipientKeys.Add(NoRecipientKey);
                }
                else if (!groupingKnown)
                {
                    // Unknown grouping is not single-target evidence: one
                    // Draft keeps every legacy recipient pending review
                    // instead of guessing the cast count.
                    warnings.Add("legacy-grouping-unknown:" + entry.Assignment.SourceId);
                    recipientKeys.Add(GroupingUnknownKey);
                }
                else if (grouping == CastGroupingKind.MassConfiguredTargets)
                    recipientKeys.Add(GroupKey);
                else
                    recipientKeys.AddRange(entry.Child.TargetUnitIds);
                var castingIds = new List<string>();
                bool reusedExisting = false;
                foreach (string recipientKey in recipientKeys)
                {
                    string identity = ImportIdentity(entry.RoutineId,
                        entry.Child.AssignmentId, recipientKey);
                    string existingId;
                    if (existingByIdentity.TryGetValue(identity, out existingId))
                    {
                        reusedExisting = true;
                        castingIds.Add(existingId);
                        continue;
                    }
                    string castingId = MigrationCastingId(entry.RoutineId,
                        entry.Child.AssignmentId, recipientKey);
                    if (!allIds.Add(castingId))
                        throw new InvalidDataException(
                            "import-id-collision:" + castingId +
                            " (an unrelated record already uses this id)");
                    produced.Add(BuildCasting(entry, castingId, recipientKey,
                        ref pooledEnhancements, ref groupReviews,
                        ref unresolvedCasters));
                    existingByIdentity[identity] = castingId;
                    castingIds.Add(castingId);
                }
                mappings.Add(new CastingImportMapping(
                    entry.Child.AssignmentId, entry.RoutineId, castingIds,
                    reusedExisting ? "reused" : recipientKeys[0] == NoRecipientKey
                        ? "unresolved-no-recipient"
                        : recipientKeys[0] == GroupingUnknownKey
                            ? "unresolved-grouping" : "imported"));
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
            // Legacy provider bans/caps/priorities are plan-wide constraints
            // no single casting carries: preserved as durable import notices
            // (review K3) until explicitly acknowledged.
            var notices = new List<string>(baseDocument.ImportNotices);
            foreach (ProviderPreferenceProfile preference in legacy.ProviderPreferences)
            {
                if (preference == null) continue;
                string notice = "legacy-provider-preference:" + preference.ProviderKey +
                    ";banned=" + preference.Banned +
                    ";priority=" + (preference.Priority.HasValue
                        ? preference.Priority.Value.ToString() : "none") +
                    ";maximumCasts=" + (preference.MaximumCasts.HasValue
                        ? preference.MaximumCasts.Value.ToString() : "none");
                if (!notices.Contains(notice)) notices.Add(notice);
            }
            var document = new CastingPlanDocument(
                baseDocument.CampaignId, baseDocument.Routines, normalized, notices,
                baseDocument.AcknowledgedImportNotices);
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

        private const string GroupKey = "group";
        private const string GroupingUnknownKey = "grouping-unknown";
        private const string NoRecipientKey = "no-recipient";

        private static string ImportIdentity(string routineId,
            string legacyAssignmentId, string recipientKey)
        {
            return (routineId ?? string.Empty) + "\u0001" + legacyAssignmentId +
                "\u0001" + recipientKey;
        }

        private static PlannedCasting BuildCasting(
            LegacyChild entry,
            string castingId,
            string recipientKey,
            ref int pooledEnhancements,
            ref int groupReviews,
            ref int unresolvedCasters)
        {
            CastingAssignmentProfile child = entry.Child;
            var reviewItems = new List<string>();
            string casterUnitId = child.CasterUnitId;
            if (child.IsAutomatic)
            {
                // An automatic legacy choice becomes a review draft; today's
                // best caster is never silently pinned as player intent.
                reviewItems.Add(AutomaticNote);
                unresolvedCasters++;
            }
            else if (casterUnitId == null)
            {
                reviewItems.Add(ProviderPinNote);
                unresolvedCasters++;
            }
            // Review K3: the exact legacy provider pin is carried as a
            // durable constraint, not dropped in favour of caster+spellbook.
            if (!string.IsNullOrWhiteSpace(child.ProviderKey))
                reviewItems.Add("provider-pin:" + child.ProviderKey);
            var enhancements = new List<AuthoredEnhancementSelection>();
            foreach (EnhancementSelectionProfile selection in child.Enhancements)
            {
                enhancements.Add(new AuthoredEnhancementSelection(
                    selection.EnhancementId, selection.IsRequired, null));
                reviewItems.Add("enhancement:" + selection.EnhancementId + ":" +
                    (selection.IsRequired ? "required" : "optional") +
                    ":exact-source-pending");
                pooledEnhancements++;
            }
            string directTarget = null;
            var coverage = new List<string>();
            CastingTargetMode mode;
            CastingOrigin origin = null;
            if (recipientKey == GroupKey || recipientKey == GroupingUnknownKey ||
                recipientKey == NoRecipientKey)
            {
                mode = CastingTargetMode.CasterCenteredOrigin;
                origin = CastingOrigin.CasterCentered();
                coverage.AddRange(child.TargetUnitIds);
                if (recipientKey == GroupKey)
                {
                    reviewItems.Add(GroupReviewNote);
                    groupReviews++;
                }
                else if (recipientKey == GroupingUnknownKey)
                    reviewItems.Add("grouping-unknown:single-or-group-pending-review;targets=" +
                        string.Join(",", child.TargetUnitIds));
                else
                    reviewItems.Add("no-recipient:pending-review");
            }
            else
            {
                mode = CastingTargetMode.DirectTarget;
                directTarget = recipientKey;
            }
            // Only a clean, known per-target, explicitly pinned child is
            // Ready; everything with an unresolved item stays Draft and is
            // counted as unresolved work by ordinary Apply.
            bool ready = reviewItems.Count == 0 && casterUnitId != null;
            var provenance = new MigrationProvenance(
                child.AssignmentId, 5, entry.RoutineId,
                string.Join("|", reviewItems), recipientKey, reviewItems);
            return new PlannedCasting(
                castingId,
                entry.RoutineId, 0, entry.Assignment.SourceId,
                entry.Assignment.Ability.ToKey(), casterUnitId, child.SpellbookGuid,
                mode, directTarget, origin, coverage, null, enhancements,
                entry.Assignment.ExistingEffectPolicy,
                entry.Assignment.IgnoredPresenceMarkers,
                ready ? CastingAuthoringState.Ready : CastingAuthoringState.Draft,
                provenance);
        }

        internal static string MigrationCastingId(string routineId,
            string legacyAssignmentId, string recipientKey)
        {
            if (string.IsNullOrWhiteSpace(legacyAssignmentId))
                throw new ArgumentException(
                    "Legacy assignment ID is required.", "legacyAssignmentId");
            // Deterministic identity from COMPLETE provenance (review K2):
            // the same legacy child id may legitimately exist in two
            // routines, so the routine is part of the id.
            return "m5:" + routineId + ":" + legacyAssignmentId + ":" + recipientKey;
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
