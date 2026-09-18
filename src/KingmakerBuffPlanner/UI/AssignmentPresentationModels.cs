using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Persistence;

namespace KingmakerBuffPlanner.UI
{
    // One authoritative partial-execution decision. Default Apply must not
    // quietly run the ready subset of an incomplete routine; an explicit
    // ready-only path is the only way to proceed, and its reporting states
    // requested coverage separately from successful casts.
    public static class PartialExecutionGate
    {
        public sealed class Decision
        {
            internal Decision(bool blocked, int requestedTargets, int fulfilled,
                int skippedActive, int unfulfilled, int plannedCasts,
                IReadOnlyList<string> unmetReasons)
            {
                Blocked = blocked;
                RequestedTargets = requestedTargets;
                Fulfilled = fulfilled;
                SkippedActive = skippedActive;
                Unfulfilled = unfulfilled;
                PlannedCasts = plannedCasts;
                UnmetReasons = unmetReasons;
            }

            public bool Blocked { get; private set; }
            public int RequestedTargets { get; private set; }
            public int Fulfilled { get; private set; }
            public int SkippedActive { get; private set; }
            public int Unfulfilled { get; private set; }
            public int PlannedCasts { get; private set; }
            public IReadOnlyList<string> UnmetReasons { get; private set; }

            public string Summary
            {
                get
                {
                    return "Requested " + RequestedTargets + " targets: " +
                        Fulfilled + " covered by " + PlannedCasts + " planned cast(s), " +
                        SkippedActive + " already active, " + Unfulfilled + " unmet." +
                        (UnmetReasons.Count == 0 ? string.Empty :
                            " Unmet: " + string.Join("; ", UnmetReasons.ToArray()));
                }
            }
        }

        public static Decision Evaluate(CastPlan plan)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            int fulfilled = plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.Fulfilled);
            int skipped = plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.SkippedAlreadyActive);
            var unmet = plan.Outcomes
                .Where(o => o.Kind == TargetOutcomeKind.Unfulfilled)
                .Select(o => o.UnitId + " (" + o.Reason + ")")
                .OrderBy(value => value, StringComparer.Ordinal).ToList();
            return new Decision(unmet.Count != 0, plan.Outcomes.Count, fulfilled,
                skipped, unmet.Count, plan.Steps.Count, unmet);
        }
    }

    // One party unit row inside the per-assignment target picker. Toggle is
    // captured per row; assignment is the source of truth for [x] state.
    public sealed class AssignmentTargetRowViewModel
    {
        internal AssignmentTargetRowViewModel(string unitId, string displayName,
            bool assigned, bool canAssign, string reason, Action toggle)
        {
            UnitId = unitId;
            DisplayName = displayName;
            Assigned = assigned;
            CanAssign = canAssign;
            Reason = reason ?? string.Empty;
            _toggle = toggle;
        }

        private readonly Action _toggle;
        public string UnitId { get; private set; }
        public string DisplayName { get; private set; }
        public bool Assigned { get; private set; }
        public bool CanAssign { get; private set; }
        public string Reason { get; private set; }

        public void Toggle() { _toggle(); }
    }

    // Flat, non-overlapping row plan for the casting-order editor: one
    // header row per assignment followed by one row per explicit target, so
    // any number of targets gets its own actionable controls instead of
    // stacking into clamped coordinates.
    public static class CastingOrderLayout
    {
        public const float HeaderRowHeight = 76f;
        public const float TargetRowHeight = 34f;

        public sealed class RowPlan
        {
            internal RowPlan(int index, string assignmentId, string unitId,
                float top, float height)
            {
                Index = index;
                AssignmentId = assignmentId;
                UnitId = unitId;
                Top = top;
                Height = height;
            }

            public int Index { get; private set; }
            public string AssignmentId { get; private set; }
            public string UnitId { get; private set; }
            public bool IsTargetRow { get { return UnitId != null; } }
            public float Top { get; private set; }
            internal float Height { get; private set; }
            public float Bottom { get { return Top + Height; } }
        }

        public static IReadOnlyList<RowPlan> PlanRows(
            IEnumerable<CastingAssignmentRowViewModel> rows)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            var plan = new List<RowPlan>();
            float top = 0f;
            int index = 0;
            foreach (CastingAssignmentRowViewModel row in rows)
            {
                plan.Add(new RowPlan(index++, row.AssignmentId, null, top, HeaderRowHeight));
                top += HeaderRowHeight;
                foreach (string unitId in row.TargetUnitIds)
                {
                    plan.Add(new RowPlan(index++, row.AssignmentId, unitId,
                        top, TargetRowHeight));
                    top += TargetRowHeight;
                }
            }
            return new ReadOnlyCollection<RowPlan>(plan);
        }

        public static float TotalHeight(IReadOnlyList<RowPlan> plan)
        {
            return plan.Count == 0 ? 0f : plan[plan.Count - 1].Bottom;
        }

        public static bool RowsAreDistinct(IReadOnlyList<RowPlan> plan)
        {
            for (int index = 1; index < plan.Count; index++)
                if (plan[index].Top < plan[index - 1].Bottom) return false;
            return true;
        }
    }

    // Assignment-scoped enhancement selection state for chooser binding.
    public sealed class EnhancementSelectionSummary
    {
        internal EnhancementSelectionSummary(string enhancementId, bool required)
        {
            EnhancementId = enhancementId;
            Required = required;
        }

        public string EnhancementId { get; private set; }
        public bool Required { get; private set; }
    }

    // Detects materially different plans between the preview the player
    // confirmed and the freshly computed one immediately before execution.
    // Material dimensions: resolved caster/provider (the item), enhancement
    // and omission sets, reserved cost, allocation order, and target
    // coverage. Cosmetic differences (diagnostic wording, marker lists) are
    // not material. The partial-execution gate alone does not cover this;
    // both must pass before anything executes.
    public static class PlanMaterialChangeDetector
    {
        public static string DescribeMaterialChange(CastPlan confirmed, CastPlan current)
        {
            if (confirmed == null || current == null) return null;
            List<string> confirmedSteps = Steps(confirmed);
            List<string> currentSteps = Steps(current);
            if (!confirmedSteps.SequenceEqual(currentSteps))
            {
                int index = 0;
                while (index < confirmedSteps.Count && index < currentSteps.Count &&
                    confirmedSteps[index] == currentSteps[index]) index++;
                string expected = index < confirmedSteps.Count ? confirmedSteps[index] : "<end>";
                string actual = index < currentSteps.Count ? currentSteps[index] : "<end>";
                return "cast " + (index + 1) + " changed: [" + expected + "] -> [" + actual + "]";
            }
            List<string> confirmedCoverage = Coverage(confirmed);
            List<string> currentCoverage = Coverage(current);
            if (!confirmedCoverage.SequenceEqual(currentCoverage))
            {
                var lost = confirmedCoverage.Except(currentCoverage, StringComparer.Ordinal).ToList();
                var gained = currentCoverage.Except(confirmedCoverage, StringComparer.Ordinal).ToList();
                return "coverage changed: lost [" + string.Join(",", lost.ToArray()) +
                    "] gained [" + string.Join(",", gained.ToArray()) + "]";
            }
            return null;
        }

        private static List<string> Steps(CastPlan plan)
        {
            // Ordered per-cast signature: assignment, resolved provider (the
            // caster/item), anchor, ordered direct targets, recipients, the
            // complete cost vector (pool, units, token identities, material,
            // enhancement usage quantities), enhancement and omission sets,
            // and the execution strategy.
            return plan.Steps.Select(step =>
                step.AssignmentId + "|" + step.Provider.Canonical + "|" +
                (step.AnchorUnitId ?? string.Empty) + "|" +
                string.Join(">", step.TargetUnitIds.ToArray()) + "|" +
                string.Join(">", step.ExpectedRecipientUnitIds.ToArray()) + "|" +
                (step.Reservation == null ? "-" :
                    step.Reservation.PoolKey + ":" + step.Reservation.Units + ":" +
                    string.Join("+", step.Reservation.TokenIds.ToArray())) + "|" +
                (step.MaterialReservation == null ? "-" :
                    step.MaterialReservation.ItemGuid + ":" + step.MaterialReservation.Count) + "|" +
                string.Join("+", step.EnhancementIds.ToArray()) + "|-" +
                string.Join("+", step.OmittedEnhancementIds.ToArray()) + "|" +
                string.Join(";", step.EnhancementUsageByPool
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => pair.Key + ":" + pair.Value).ToArray()) + "|" +
                step.ExecutionStrategy).ToList();
        }

        private static List<string> Coverage(CastPlan plan)
        {
            return plan.Outcomes
                .Select(outcome => outcome.AssignmentId + "|" + outcome.UnitId + "|" +
                    (int)outcome.Kind)
                .OrderBy(value => value, StringComparer.Ordinal).ToList();
        }
    }

    public sealed class CastingAssignmentRowViewModel
    {
        internal CastingAssignmentRowViewModel(int number, string sourceDisplayName,
            string sourceId, CastingAssignmentProfile assignment, string casterText,
            bool pinUnresolved, IReadOnlyList<string> targetNames,
            IReadOnlyList<string> enhancementTexts,
            int plannedCasts, int fulfilledTargets, int unfulfilledTargets,
            bool canMoveEarlier, bool canMoveLater, bool automatic)
        {
            Number = number;
            SourceDisplayName = sourceDisplayName;
            SourceId = sourceId;
            AssignmentId = assignment.AssignmentId;
            Order = assignment.Order;
            CasterText = casterText;
            PinUnresolved = pinUnresolved;
            Automatic = automatic;
            TargetNames = targetNames;
            TargetUnitIds = new ReadOnlyCollection<string>(
                assignment.TargetUnitIds.ToList());
            EnhancementTexts = enhancementTexts;
            PlannedCasts = plannedCasts;
            FulfilledTargets = fulfilledTargets;
            UnfulfilledTargets = unfulfilledTargets;
            CanMoveEarlier = canMoveEarlier;
            CanMoveLater = canMoveLater;
        }

        public int Number { get; private set; }
        public string SourceDisplayName { get; private set; }
        public string AssignmentId { get; private set; }
        public string SourceId { get; private set; }
        public int Order { get; private set; }
        public string CasterText { get; private set; }
        public bool PinUnresolved { get; private set; }
        public bool Automatic { get; private set; }
        public IReadOnlyList<string> TargetNames { get; private set; }
        public IReadOnlyList<string> TargetUnitIds { get; private set; }
        public IReadOnlyList<string> EnhancementTexts { get; private set; }
        public int PlannedCasts { get; private set; }
        public int FulfilledTargets { get; private set; }
        public int UnfulfilledTargets { get; private set; }
        public bool CanMoveEarlier { get; private set; }
        public bool CanMoveLater { get; private set; }
        public bool Editable { get; internal set; }
        public IReadOnlyList<string> ResolvedProviderTexts { get; internal set; }
        public IReadOnlyList<string> RecipientNames { get; internal set; }

        public string Status =>
            PinUnresolved ? "Pinned caster unavailable" :
            UnfulfilledTargets > 0 ? UnfulfilledTargets + " unmet target(s)" :
            PlannedCasts == 0 ? FulfilledTargets + " already active" :
            "Planned " + PlannedCasts + " cast(s) covering " + FulfilledTargets + " target(s)";

        public static IReadOnlyList<CastingAssignmentRowViewModel> CreateRoutineRows(
            BuffPlannerProfile profile, string routineId,
            Func<string, string> sourceDisplayName,
            Func<string, string> unitDisplayName,
            Func<string, string> enhancementDisplayName,
            CastPlan plan,
            string selectedSourceId = null,
            Func<string, string> providerDisplayName = null)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            RoutineProfile routine = profile.Routines.FirstOrDefault(r => r.RoutineId == routineId);
            if (routine == null) throw new ArgumentException("Unknown routine.", "routineId");
            var rows = new List<CastingAssignmentRowViewModel>();
            var ordered = routine.Assignments
                .SelectMany(assignment => assignment.CastingAssignments.Select(
                    child => new { Parent = assignment, Child = child }))
                .OrderBy(pair => pair.Child.Order)
                .ThenBy(pair => pair.Child.AssignmentId, StringComparer.Ordinal).ToList();
            var recipientsByAssignment = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var providersByAssignment = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            if (plan != null)
            {
                foreach (CastStep step in plan.Steps)
                {
                    List<string> providers;
                    if (!providersByAssignment.TryGetValue(step.AssignmentId, out providers))
                        providersByAssignment[step.AssignmentId] = providers = new List<string>();
                    providers.Add(providerDisplayName == null
                        ? step.Provider.CasterUnitId : providerDisplayName(step.Provider.CasterUnitId));
                    List<string> recipients;
                    if (!recipientsByAssignment.TryGetValue(step.AssignmentId, out recipients))
                        recipientsByAssignment[step.AssignmentId] = recipients = new List<string>();
                    foreach (string recipient in step.ExpectedRecipientUnitIds)
                        recipients.Add(unitDisplayName == null
                            ? recipient : unitDisplayName(recipient));
                }
            }
            for (int index = 0; index < ordered.Count; index++)
            {
                var pair = ordered[index];
                CastingAssignmentProfile child = pair.Child;
                string casterText = child.IsAutomatic
                    ? "Automatic"
                    : "Pinned: " + (unitDisplayName == null
                        ? child.CasterUnitId : unitDisplayName(child.CasterUnitId)) +
                        (string.IsNullOrWhiteSpace(child.SpellbookGuid)
                            ? string.Empty : " | " + child.SpellbookGuid) +
                        (string.IsNullOrWhiteSpace(child.ProviderKey)
                            ? string.Empty : " | " + child.ProviderKey);
                bool pinUnresolved = !child.IsAutomatic && plan != null &&
                    plan.Diagnostics.Any(line => line.StartsWith("pin-unresolved:" +
                        child.AssignmentId + ":", StringComparison.Ordinal));
                List<string> targetNames = child.TargetUnitIds
                    .Select(id => unitDisplayName == null ? id : unitDisplayName(id))
                    .ToList();
                List<string> enhancementTexts = child.Enhancements.Select(selection =>
                    (enhancementDisplayName == null
                        ? selection.EnhancementId : enhancementDisplayName(selection.EnhancementId)) +
                    (selection.IsRequired ? string.Empty : " (cast without when unavailable)"))
                    .ToList();
                int plannedCasts = plan == null ? 0 : plan.Steps.Count(step =>
                    step.AssignmentId == child.AssignmentId);
                int fulfilled = plan == null ? 0 : plan.Outcomes.Count(o =>
                    o.AssignmentId == child.AssignmentId &&
                    o.Kind == TargetOutcomeKind.Fulfilled);
                int unfulfilled = plan == null ? 0 : plan.Outcomes.Count(o =>
                    o.AssignmentId == child.AssignmentId &&
                    o.Kind == TargetOutcomeKind.Unfulfilled);
                List<string> providers;
                providersByAssignment.TryGetValue(child.AssignmentId, out providers);
                List<string> recipients;
                recipientsByAssignment.TryGetValue(child.AssignmentId, out recipients);
                rows.Add(new CastingAssignmentRowViewModel(index + 1,
                    sourceDisplayName == null ? pair.Parent.SourceId
                        : sourceDisplayName(pair.Parent.SourceId),
                    pair.Parent.SourceId,
                    child, casterText, pinUnresolved,
                    new ReadOnlyCollection<string>(targetNames),
                    new ReadOnlyCollection<string>(enhancementTexts),
                    plannedCasts, fulfilled, unfulfilled,
                    index > 0, index < ordered.Count - 1, child.IsAutomatic)
                {
                    Editable = selectedSourceId != null &&
                        string.Equals(pair.Parent.SourceId, selectedSourceId,
                            StringComparison.Ordinal),
                    ResolvedProviderTexts = new ReadOnlyCollection<string>(
                        (providers ?? new List<string>()).Distinct().ToList()),
                    RecipientNames = new ReadOnlyCollection<string>(
                        (recipients ?? new List<string>()).Distinct().ToList())
                });
            }
            return new ReadOnlyCollection<CastingAssignmentRowViewModel>(rows);
        }
    }

    public sealed class ResourceUsageLineViewModel
    {
        internal ResourceUsageLineViewModel(ResourcePoolAllocation allocation,
            string displayName, string routineId,
            IReadOnlyList<string> competingRoutineIds)
        {
            PoolKey = allocation.PoolKey;
            DisplayName = displayName;
            AvailableNow = allocation.AvailableNow;
            RequestedUsage = allocation.RequestedUsage;
            AllocatedUsage = allocation.AllocatedUsage;
            UnmetDemand = allocation.UnmetDemand;
            ForecastRemaining = allocation.ForecastRemaining;
            Traces = allocation.Traces;
            CurrentRoutineId = routineId;
            // Competing demand from other routines is configured intent, never
            // a committed reservation; it is labeled as such in the UI.
            CompetingRoutineIds = competingRoutineIds;
        }

        public string PoolKey { get; private set; }
        public string DisplayName { get; private set; }
        public int AvailableNow { get; private set; }
        public int RequestedUsage { get; private set; }
        public int AllocatedUsage { get; private set; }
        public int UnmetDemand { get; private set; }
        public int ForecastRemaining { get; private set; }
        public IReadOnlyList<string> Traces { get; private set; }
        public string CurrentRoutineId { get; private set; }
        public IReadOnlyList<string> CompetingRoutineIds { get; private set; }

        public string Summary
        {
            get
            {
                string competing = CompetingRoutineIds.Count == 0 ? string.Empty :
                    " | also requested by " + string.Join(", ", CompetingRoutineIds.ToArray()) +
                        " (their own runs)";
                return DisplayName + ": requested " + RequestedUsage +
                    " / available " + AvailableNow + " / allocated " + AllocatedUsage +
                    " / unmet " + UnmetDemand + " / forecast remaining " + ForecastRemaining +
                    competing;
            }
        }
    }

}
