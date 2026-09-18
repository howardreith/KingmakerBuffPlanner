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
        public IReadOnlyList<string> EnhancementTexts { get; private set; }
        public int PlannedCasts { get; private set; }
        public int FulfilledTargets { get; private set; }
        public int UnfulfilledTargets { get; private set; }
        public bool CanMoveEarlier { get; private set; }
        public bool CanMoveLater { get; private set; }

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
            CastPlan plan)
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
                rows.Add(new CastingAssignmentRowViewModel(index + 1,
                    sourceDisplayName == null ? pair.Parent.SourceId
                        : sourceDisplayName(pair.Parent.SourceId),
                    pair.Parent.SourceId,
                    child, casterText, pinUnresolved,
                    new ReadOnlyCollection<string>(targetNames),
                    new ReadOnlyCollection<string>(enhancementTexts),
                    plannedCasts, fulfilled, unfulfilled,
                    index > 0, index < ordered.Count - 1, child.IsAutomatic));
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

    // Read-only combined forecast across selected routine occurrences, one run
    // per selected routine. Balances carry forward in the selected order;
    // unsupported projections stay labeled conservative demand estimates.
    public sealed class RoutineSequenceForecast
    {
        public sealed class RoutineStep
        {
            internal RoutineStep(string routineId, int plannedCasts, int fulfilled,
                int unfulfilled, int skippedActive,
                IReadOnlyList<string> poolNotes)
            {
                RoutineId = routineId;
                PlannedCasts = plannedCasts;
                Fulfilled = fulfilled;
                Unfulfilled = unfulfilled;
                SkippedActive = skippedActive;
                PoolNotes = poolNotes;
            }

            public string RoutineId { get; private set; }
            public int PlannedCasts { get; private set; }
            public int Fulfilled { get; private set; }
            public int Unfulfilled { get; private set; }
            public int SkippedActive { get; private set; }
            public IReadOnlyList<string> PoolNotes { get; private set; }
        }

        internal RoutineSequenceForecast(IReadOnlyList<RoutineStep> steps,
            IReadOnlyDictionary<string, int> forecastRemainingByPool)
        {
            Steps = steps;
            ForecastRemainingByPool = forecastRemainingByPool;
        }

        public IReadOnlyList<RoutineStep> Steps { get; private set; }
        public IReadOnlyDictionary<string, int> ForecastRemainingByPool { get; private set; }

        public const string AssumptionText =
            "One run per selected routine, successful casts, no intervening combat " +
            "or expiration; carried effects skip for free where already active. " +
            "Conditional coverage is a conservative demand estimate, not an exact forecast.";

        // Each input is one routine occurrence's plan against the SAME native
        // snapshot; allocations are subtracted in the given order.
        public static RoutineSequenceForecast Compute(
            IEnumerable<KeyValuePair<string, CastPlan>> routinePlansInOrder)
        {
            if (routinePlansInOrder == null)
                throw new ArgumentNullException("routinePlansInOrder");
            var balances = new Dictionary<string, int>(StringComparer.Ordinal);
            var available = new Dictionary<string, int>(StringComparer.Ordinal);
            var steps = new List<RoutineStep>();
            foreach (KeyValuePair<string, CastPlan> pair in routinePlansInOrder)
            {
                CastPlan plan = pair.Value;
                if (plan == null) throw new ArgumentException("Routine plan is null.", "routinePlansInOrder");
                var notes = new List<string>();
                foreach (ResourcePoolAllocation allocation in plan.ResourceAllocations)
                {
                    int start;
                    if (!available.TryGetValue(allocation.PoolKey, out start))
                    {
                        start = allocation.AvailableNow;
                        available[allocation.PoolKey] = start;
                        balances[allocation.PoolKey] = start;
                    }
                    int remaining = balances[allocation.PoolKey] - allocation.AllocatedUsage;
                    if (remaining < 0) remaining = 0;
                    balances[allocation.PoolKey] = remaining;
                    notes.Add(allocation.PoolKey + ": allocated " + allocation.AllocatedUsage +
                        ", forecast remaining " + remaining + " of " + start +
                        (allocation.UnmetDemand == 0 ? string.Empty :
                            " (unmet demand " + allocation.UnmetDemand + " in this run)"));
                }
                steps.Add(new RoutineStep(pair.Key, plan.Steps.Count,
                    plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.Fulfilled),
                    plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.Unfulfilled),
                    plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.SkippedAlreadyActive),
                    new ReadOnlyCollection<string>(notes)));
            }
            return new RoutineSequenceForecast(
                new ReadOnlyCollection<RoutineStep>(steps),
                new ReadOnlyDictionary<string, int>(balances));
        }
    }
}
