using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
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

        internal void ApplyDisplayNames(string caster, string target)
        {
            CasterDisplayName = caster ?? CasterUnitId ?? string.Empty;
            DirectTargetDisplayName = target;
        }
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
            string sourceId, string displayName, bool selected)
        {
            SourceId = sourceId ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? SourceId : displayName;
            Selected = selected;
        }

        public string SourceId { get; private set; }
        public string DisplayName { get; private set; }
        public bool Selected { get; private set; }
    }

    // One selectable recipient unit for a direct-target casting.
    public sealed class WorkspaceTargetOption
    {
        internal WorkspaceTargetOption(
            string unitId, string displayName, bool selected)
        {
            UnitId = unitId ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? UnitId : displayName;
            Selected = selected;
        }

        public string UnitId { get; private set; }
        public string DisplayName { get; private set; }
        public bool Selected { get; private set; }
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
                    return match.DisplayName;
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
