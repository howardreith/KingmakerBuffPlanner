using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
            IReadOnlyList<string> diagnostics)
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

        public WorkspaceCastingCard CardById(string castingId)
        {
            return Cards.FirstOrDefault(card =>
                string.Equals(card.CastingId, castingId, StringComparison.Ordinal));
        }
    }
}
