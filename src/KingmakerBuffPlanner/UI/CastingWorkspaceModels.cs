using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    public enum WorkspaceEditingScope
    {
        ConfigureNextCasting,
        EditingSingleCasting
    }

    // Read model for one caster row in the caster lane: capability is
    // separated from current readiness, and exhausted or unavailable casters
    // stay listed with their reasons.
    public sealed class WorkspaceCasterRow
    {
        internal WorkspaceCasterRow(
            string unitId, string displayName, bool capable,
            IReadOnlyList<string> readinessReasons, bool selectedFocus)
        {
            UnitId = unitId;
            DisplayName = displayName ?? string.Empty;
            Capable = capable;
            ReadinessReasons = new ReadOnlyCollection<string>(
                (readinessReasons ?? new string[0]).ToList());
            SelectedFocus = selectedFocus;
        }

        public string UnitId { get; private set; }
        public string DisplayName { get; private set; }
        public bool Capable { get; private set; }
        public IReadOnlyList<string> ReadinessReasons { get; private set; }
        public bool SelectedFocus { get; private set; }
    }

    // Read model for one casting card. Everything shown derives from the
    // shared resolved plan — the view never keeps its own ledger.
    public sealed class WorkspaceCastingCard
    {
        internal WorkspaceCastingCard(
            string castingId,
            string routineId,
            int order,
            string casterUnitId,
            string sourceId,
            string modeLabel,
            string directTargetUnitId,
            string originLabel,
            IReadOnlyList<string> requiredCoverageUnitIds,
            IReadOnlyList<string> predictedBeneficiaryUnitIds,
            IReadOnlyList<string> coverageGapUnitIds,
            IReadOnlyList<string> enhancementLabels,
            IReadOnlyList<string> costLabels,
            ResolvedCastingReadiness readiness,
            IReadOnlyList<string> readinessReasons,
            bool editingFocus)
        {
            CastingId = castingId;
            RoutineId = routineId;
            Order = order;
            CasterUnitId = casterUnitId;
            SourceId = sourceId;
            ModeLabel = modeLabel;
            DirectTargetUnitId = directTargetUnitId;
            OriginLabel = originLabel;
            RequiredCoverageUnitIds = requiredCoverageUnitIds;
            PredictedBeneficiaryUnitIds = predictedBeneficiaryUnitIds;
            CoverageGapUnitIds = coverageGapUnitIds;
            EnhancementLabels = enhancementLabels;
            CostLabels = costLabels;
            Readiness = readiness;
            ReadinessReasons = readinessReasons;
            EditingFocus = editingFocus;
        }

        public string CastingId { get; private set; }
        public string RoutineId { get; private set; }
        public int Order { get; private set; }
        public string CasterUnitId { get; private set; }
        public string CasterDisplayName { get; private set; }
        public string DirectTargetDisplayName { get; private set; }

        internal void ApplyDisplayNames(string caster, string target,
            Func<string, string> nameOf = null)
        {
            CasterDisplayName = caster ?? CasterUnitId ?? string.Empty;
            DirectTargetDisplayName = target;
            Func<string, string> resolve = nameOf ?? (id => id);
            // Players read names, never unit ids: the anchored origin and
            // the missed-coverage list are resolved like caster and target.
            const string originPrefix = "Origin: ";
            if (OriginLabel != null && OriginLabel.StartsWith(originPrefix,
                    StringComparison.Ordinal))
            {
                string anchor = OriginLabel.Substring(originPrefix.Length);
                if (!string.Equals(anchor, "caster", StringComparison.Ordinal))
                    OriginLabel = originPrefix + (resolve(anchor) ?? anchor);
            }
            CoverageGapDisplayNames = (CoverageGapUnitIds ?? new string[0])
                .Select(id => resolve(id) ?? id).ToList();
        }

        public IReadOnlyList<string> CoverageGapDisplayNames { get; private set; } =
            new string[0];

        // Durable import-review items carried by this record's provenance
        // (review K3); empty for authored or cleanly imported records.
        public IReadOnlyList<string> ReviewItems { get; private set; } = new string[0];

        internal void ApplyReviewItems(IEnumerable<string> unresolved,
            IEnumerable<string> resolved = null)
        {
            ReviewItems = (unresolved ?? new string[0]).ToList();
            ResolvedReviewItems = (resolved ?? new string[0]).ToList();
        }

        // Imported review items the player explicitly resolved (kept as
        // history, review L1).
        public IReadOnlyList<string> ResolvedReviewItems { get; private set; } = new string[0];
        public string SourceId { get; private set; }
        public string ModeLabel { get; private set; }
        public string DirectTargetUnitId { get; private set; }
        public string OriginLabel { get; private set; }
        public IReadOnlyList<string> RequiredCoverageUnitIds { get; private set; }
        public IReadOnlyList<string> PredictedBeneficiaryUnitIds { get; private set; }
        public IReadOnlyList<string> CoverageGapUnitIds { get; private set; }
        public IReadOnlyList<string> EnhancementLabels { get; private set; }
        public IReadOnlyList<string> CostLabels { get; private set; }
        public ResolvedCastingReadiness Readiness { get; private set; }
        public IReadOnlyList<string> ReadinessReasons { get; private set; }
        public bool EditingFocus { get; private set; }

        // Card headline: who casts it on whom — the casting as a player
        // reads it, not its internal id.
        public string Headline
        {
            get
            {
                string caster = string.IsNullOrEmpty(CasterDisplayName)
                    ? "Unresolved caster" : CasterDisplayName;
                string target = DirectTargetUnitId != null
                    ? (DirectTargetDisplayName ?? DirectTargetUnitId)
                    : (string.IsNullOrEmpty(OriginLabel) ? "group" : OriginLabel);
                return caster + " → " + target;
            }
        }

        public string Subtitle
        {
            get { return "Casting " + (Order + 1) + " in " + RoutineId; }
        }

        // Group castings only: predicted beneficiaries over intended
        // coverage. A direct-target casting has no coverage ratio.
        public string CoverageSummary
        {
            get
            {
                if (DirectTargetUnitId != null) return string.Empty;
                return "coverage " + (PredictedBeneficiaryUnitIds == null ? 0
                        : PredictedBeneficiaryUnitIds.Count) + "/" +
                    (RequiredCoverageUnitIds == null ? 0 : RequiredCoverageUnitIds.Count);
            }
        }
    }

    // One budget line in the footer drill-down, copied from the shared
    // resolved plan's authoritative lines with the responsible casting IDs.
    public sealed class WorkspaceBudgetRow
    {
        internal WorkspaceBudgetRow(CastingBudgetLine line)
        {
            PoolKey = line.PoolKey;
            Category = line.Category;
            AvailableNow = line.AvailableNow;
            RequestedUsage = line.RequestedUsage;
            AllocatedUsage = line.AllocatedUsage;
            UnmetDemand = line.UnmetDemand;
            ForecastRemaining = line.ForecastRemaining;
            ResponsibleCastingIds = line.Traces;
        }

        public string PoolKey { get; private set; }
        public CastingCostCategory Category { get; private set; }
        public int? AvailableNow { get; private set; }
        public int RequestedUsage { get; private set; }
        public int AllocatedUsage { get; private set; }
        public int UnmetDemand { get; private set; }
        public int? ForecastRemaining { get; private set; }
        public IReadOnlyList<string> ResponsibleCastingIds { get; private set; }
    }

    // One selectable buff source in the draft editor's catalogue.
    public sealed class WorkspaceSourceOption
    {
        internal WorkspaceSourceOption(
            string sourceId, string displayName, bool selected,
            string detail = null, AbilityKey iconAbility = null)
        {
            SourceId = sourceId ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? SourceId : displayName;
            Selected = selected;
            Detail = detail ?? string.Empty;
            IconAbility = iconAbility;
        }

        // The discovered ability whose native icon represents this buff in
        // the grid; null when discovery supplied none (view shows a glyph).
        public AbilityKey IconAbility { get; private set; }

        public string SourceId { get; private set; }
        public string DisplayName { get; private set; }
        public bool Selected { get; private set; }
        // Distinguishes sources that share a display name (for example two
        // "Aid Another" sources); empty when the name is already unique.
        public string Detail { get; private set; }

        public string Label
        {
            get { return Detail.Length == 0 ? DisplayName : DisplayName + " — " + Detail; }
        }
    }

    // Coverage of one party member by the selected buff's castings in the
    // selected routine, using Bubble Buffs' legend: gray = no casting,
    // green = covered by a Ready casting, amber = only covered by castings
    // that are not Ready (draft, blocked, disabled).
    public enum WorkspaceRecipientCoverage
    {
        None,
        CoveredReady,
        CoveredNotReady
    }

    public static class WorkspaceBuffSummary
    {
        // Number of castings per buff source in one routine — the counts
        // shown on the buff grid cards.
        public static IReadOnlyDictionary<string, int> CastingsBySource(
            IEnumerable<PlannedCasting> castings, string routineId)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (PlannedCasting casting in castings ?? new PlannedCasting[0])
            {
                if (casting == null || !string.Equals(casting.RoutineId,
                        routineId, StringComparison.Ordinal)) continue;
                int count;
                result.TryGetValue(casting.SourceId, out count);
                result[casting.SourceId] = count + 1;
            }
            return result;
        }

        // Which party members castings of ONE buff source cover in the
        // routine: a direct recipient or a predicted group beneficiary.
        // Ready wins over not-ready. The source filter is explicit (review
        // K7): coverage never depends on which cards a list happens to show.
        public static WorkspaceRecipientCoverage CoverageFor(
            IEnumerable<WorkspaceCastingCard> cards, string sourceId,
            string routineId, string unitId)
        {
            WorkspaceRecipientCoverage best = WorkspaceRecipientCoverage.None;
            foreach (WorkspaceCastingCard card in cards ?? new WorkspaceCastingCard[0])
            {
                if (card == null || !string.Equals(card.RoutineId, routineId,
                        StringComparison.Ordinal) ||
                    !string.Equals(card.SourceId, sourceId,
                        StringComparison.Ordinal)) continue;
                bool covers = string.Equals(card.DirectTargetUnitId, unitId,
                        StringComparison.Ordinal) ||
                    (card.PredictedBeneficiaryUnitIds != null &&
                     card.PredictedBeneficiaryUnitIds.Contains(unitId));
                if (!covers) continue;
                if (card.Readiness == ResolvedCastingReadiness.Ready)
                    return WorkspaceRecipientCoverage.CoveredReady;
                best = WorkspaceRecipientCoverage.CoveredNotReady;
            }
            return best;
        }
    }

    // What discovery knows about one buff source, for labeling.
    public sealed class WorkspaceSourceDescriptor
    {
        public WorkspaceSourceDescriptor(string sourceId, string displayName,
            IEnumerable<string> variantNames, IEnumerable<string> kindNames,
            IEnumerable<string> casterNames)
        {
            SourceId = sourceId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            VariantNames = Clean(variantNames);
            KindNames = Clean(kindNames);
            CasterNames = Clean(casterNames);
        }

        public string SourceId { get; private set; }
        public string DisplayName { get; private set; }
        public IReadOnlyList<string> VariantNames { get; private set; }
        public IReadOnlyList<string> KindNames { get; private set; }
        public IReadOnlyList<string> CasterNames { get; private set; }

        private static IReadOnlyList<string> Clean(IEnumerable<string> values)
        {
            return (values ?? new string[0])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList();
        }
    }

    // Deterministic disambiguation for same-named buff sources: prefer the
    // discovered variant names, then source kind and who can cast it, and
    // only as a last resort an ordinal. Unique names get no detail.
    public static class WorkspaceSourceLabels
    {
        // Buff grid search: every whitespace-separated term must appear in
        // the full label (name plus disambiguating detail), ignoring case.
        public static bool Matches(WorkspaceSourceOption source, string query)
        {
            if (source == null) return false;
            if (string.IsNullOrWhiteSpace(query)) return true;
            string label = source.Label;
            foreach (string term in query.Split(new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries))
                if (label.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            return true;
        }

        public static IReadOnlyDictionary<string, string> Details(
            IEnumerable<WorkspaceSourceDescriptor> sources)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            List<WorkspaceSourceDescriptor> all = (sources ??
                new WorkspaceSourceDescriptor[0]).Where(s => s != null).ToList();
            foreach (IGrouping<string, WorkspaceSourceDescriptor> group in all
                .GroupBy(s => s.DisplayName, StringComparer.Ordinal))
            {
                List<WorkspaceSourceDescriptor> members = group
                    .OrderBy(s => s.SourceId, StringComparer.Ordinal).ToList();
                if (members.Count == 1)
                {
                    result[members[0].SourceId] = string.Empty;
                    continue;
                }
                // Discovered variant names usually repeat the base name
                // ("Use Heal Skill — Treat Affliction"); keep only the part
                // that distinguishes the variant.
                Func<WorkspaceSourceDescriptor, string> byVariant = s =>
                    string.Join(", ", s.VariantNames
                        .Select(v => StripBase(v, s.DisplayName))
                        .Where(v => v.Length != 0)
                        .Distinct(StringComparer.Ordinal));
                Func<WorkspaceSourceDescriptor, string> byKindAndCaster = s =>
                    string.Join("/", s.KindNames) +
                    (s.CasterNames.Count == 0 ? string.Empty
                        : (s.KindNames.Count == 0 ? string.Empty : " · ") +
                          string.Join(", ", s.CasterNames));
                Func<WorkspaceSourceDescriptor, string> chosen = null;
                foreach (Func<WorkspaceSourceDescriptor, string> candidate in
                    new[] { byVariant, byKindAndCaster })
                {
                    List<string> details = members.Select(candidate).ToList();
                    if (details.All(d => d.Length != 0) &&
                        details.Distinct(StringComparer.Ordinal).Count() == details.Count)
                    {
                        chosen = candidate;
                        break;
                    }
                }
                for (int index = 0; index < members.Count; index++)
                {
                    string detail = chosen != null ? chosen(members[index])
                        : byKindAndCaster(members[index]);
                    if (chosen == null)
                        detail = (detail.Length == 0 ? string.Empty : detail + " · ") +
                            "source " + (index + 1);
                    result[members[index].SourceId] = detail;
                }
            }
            return result;
        }

        private static string StripBase(string variant, string baseName)
        {
            if (string.Equals(variant, baseName, StringComparison.Ordinal))
                return string.Empty;
            foreach (string separator in new[] { " — ", " - ", ": " })
            {
                string prefix = baseName + separator;
                if (variant.StartsWith(prefix, StringComparison.Ordinal))
                    return variant.Substring(prefix.Length).Trim();
            }
            return variant;
        }
    }

    // One selectable recipient unit for a direct-target casting.
    public sealed class WorkspaceTargetOption
    {
        internal WorkspaceTargetOption(
            string unitId, string displayName, bool selected,
            bool? legal = null)
        {
            UnitId = unitId ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? UnitId : displayName;
            Selected = selected;
            Legal = legal;
        }

        public string UnitId { get; private set; }
        public string DisplayName { get; private set; }
        public bool Selected { get; private set; }
        // Whether the draft's resolved caster/ability can target this unit
        // (Bubble Buffs' red state). Null when no provider option is
        // resolved yet (no caster chosen): unknown is never shown as legal
        // or illegal.
        public bool? Legal { get; private set; }
    }

    // One selectable group origin (anchor) for a group casting.
    public sealed class WorkspaceOriginOption
    {
        internal WorkspaceOriginOption(
            string anchorUnitId, bool selected)
        {
            AnchorUnitId = anchorUnitId ?? string.Empty;
            Selected = selected;
        }

        public string AnchorUnitId { get; private set; }
        public bool Selected { get; private set; }
    }

    // One toggleable per-casting enhancement for the draft.
    public sealed class WorkspaceEnhancementOption
    {
        internal WorkspaceEnhancementOption(
            string enhancementId, string label, bool selected)
        {
            EnhancementId = enhancementId ?? string.Empty;
            Label = string.IsNullOrWhiteSpace(label) ? EnhancementId : label;
            Selected = selected;
        }

        public string EnhancementId { get; private set; }
        public string Label { get; private set; }
        public bool Selected { get; private set; }
    }

    // The draft editor read model: everything the next-casting controls
    // need, derived from the SAME discovery inputs the plan compiles from.
    public sealed class WorkspaceDraftView
    {
        internal WorkspaceDraftView(
            string sourceId,
            string casterUnitId,
            CastingTargetMode targetMode,
            string directTargetUnitId,
            string originAnchorUnitId,
            string rememberedDirectTargetUnitId,
            Domain.Authoring.CastingAuthoringState state,
            IReadOnlyList<WorkspaceSourceOption> sources,
            IReadOnlyList<WorkspaceCasterRow> capableCasters,
            IReadOnlyList<WorkspaceTargetOption> targets,
            IReadOnlyList<WorkspaceOriginOption> origins,
            IReadOnlyList<WorkspaceEnhancementOption> enhancements)
        {
            SourceId = sourceId ?? string.Empty;
            CasterUnitId = casterUnitId ?? string.Empty;
            TargetMode = targetMode;
            DirectTargetUnitId = directTargetUnitId ?? string.Empty;
            OriginAnchorUnitId = originAnchorUnitId ?? string.Empty;
            RememberedDirectTargetUnitId = rememberedDirectTargetUnitId ?? string.Empty;
            State = state;
            Sources = sources;
            CapableCasters = capableCasters;
            Targets = targets;
            Origins = origins;
            Enhancements = enhancements;
        }

        public string SourceId { get; private set; }
        public string CasterUnitId { get; private set; }
        public CastingTargetMode TargetMode { get; private set; }
        public string DirectTargetUnitId { get; private set; }
        public string OriginAnchorUnitId { get; private set; }
        // The previously chosen direct recipient, preserved across a switch
        // to group mode so switching back can restore it explicitly instead
        // of silently choosing one (review G2).
        public string RememberedDirectTargetUnitId { get; private set; }
        public Domain.Authoring.CastingAuthoringState State { get; private set; }
        public IReadOnlyList<WorkspaceSourceOption> Sources { get; private set; }
        public IReadOnlyList<WorkspaceCasterRow> CapableCasters { get; private set; }
        public IReadOnlyList<WorkspaceTargetOption> Targets { get; private set; }
        public IReadOnlyList<WorkspaceOriginOption> Origins { get; private set; }
        public IReadOnlyList<WorkspaceEnhancementOption> Enhancements { get; private set; }
    }

    // The full read model the Unity view renders after each refresh. Built
    // from the document plus the shared resolved plan; browsing never
    // mutates anything.
    public sealed class WorkspaceView
    {
        internal WorkspaceView(
            string selectedSourceId,
            string selectedRoutineId,
            IReadOnlyList<WorkspaceCasterRow> casters,
            IReadOnlyList<WorkspaceCastingCard> cards,
            IReadOnlyList<WorkspaceBudgetRow> budgetRows,
            IReadOnlyList<string> routineIds,
            CastingApplyDecision selectedRoutineGate,
            CastingApplyDecision onePassGate,
            CastingReviewStatus reviewStatus,
            WorkspaceEditingScope editingScope,
            string editingScopeLabel,
            IReadOnlyList<string> diagnostics,
            WorkspaceDraftView draft)
        {
            SelectedSourceId = selectedSourceId ?? string.Empty;
            SelectedRoutineId = selectedRoutineId ?? string.Empty;
            Casters = casters;
            Cards = cards;
            BudgetRows = budgetRows;
            RoutineIds = routineIds;
            SelectedRoutineGate = selectedRoutineGate;
            OnePassGate = onePassGate;
            ReviewStatus = reviewStatus;
            EditingScope = editingScope;
            EditingScopeLabel = editingScopeLabel ?? string.Empty;
            Diagnostics = diagnostics;
            Draft = draft;
        }

        public string SelectedSourceId { get; private set; }
        public string SelectedRoutineId { get; private set; }
        public IReadOnlyList<WorkspaceCasterRow> Casters { get; private set; }
        public IReadOnlyList<WorkspaceCastingCard> Cards { get; private set; }
        public IReadOnlyList<WorkspaceBudgetRow> BudgetRows { get; private set; }
        public IReadOnlyList<string> RoutineIds { get; private set; }
        public CastingApplyDecision SelectedRoutineGate { get; private set; }
        public CastingApplyDecision OnePassGate { get; private set; }
        public CastingReviewStatus ReviewStatus { get; private set; }
        public WorkspaceEditingScope EditingScope { get; private set; }
        public string EditingScopeLabel { get; private set; }
        public IReadOnlyList<string> Diagnostics { get; private set; }
        public WorkspaceDraftView Draft { get; private set; }

        // Every casting of the SELECTED buff (all routines), independent of
        // the castings lane's this-buff / whole-routine display scope; the
        // recipient coverage colours are computed from these only.
        public IReadOnlyList<WorkspaceCastingCard> SelectedBuffCards
        {
            get { return _selectedBuffCards; }
        }
        internal readonly List<WorkspaceCastingCard> _selectedBuffCards =
            new List<WorkspaceCastingCard>();
        // Enhancement options for the FOCUSED casting, derived from that
        // casting's own caster and ability — never the next-casting draft
        // (review G1).
        public IReadOnlyList<WorkspaceEnhancementOption> FocusedEnhancements
        {
            get { return _focusedEnhancements; }
        }

        // Origin options for the FOCUSED record's own provider (review H2d).
        public IReadOnlyList<WorkspaceOriginOption> FocusedOrigins
        {
            get { return _focusedOrigins; }
        }
        internal readonly List<WorkspaceEnhancementOption> _focusedEnhancements =
            new List<WorkspaceEnhancementOption>();
        internal readonly List<WorkspaceOriginOption> _focusedOrigins =
            new List<WorkspaceOriginOption>();

        // Header caption for the selected buff: the discovered display name
        // of the selected source; never the raw source key when a name was
        // discovered (the raw "variant|<guid>|<guid>" key reached the header
        // in every live run through casting-ws-claude-rehearsal-*).
        public string SelectedSourceCaption
        {
            get
            {
                if (SelectedSourceId.Length == 0) return "no buff selected";
                WorkspaceSourceOption match = Draft == null ? null :
                    Draft.Sources.FirstOrDefault(source => source != null &&
                        string.Equals(source.SourceId, SelectedSourceId,
                            StringComparison.Ordinal));
                if (match != null && !string.Equals(match.DisplayName,
                        match.SourceId, StringComparison.Ordinal))
                    return match.Label;
                return "unnamed buff source";
            }
        }

        public WorkspaceCastingCard CardById(string castingId)
        {
            return Cards.FirstOrDefault(card =>
                string.Equals(card.CastingId, castingId, StringComparison.Ordinal));
        }
    }
}
