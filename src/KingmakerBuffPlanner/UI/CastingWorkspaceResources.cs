using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    public enum WorkspaceResourceScope
    {
        SelectedRoutine,
        AllRoutinesNoRest
    }

    // Structured accounting for the native Resources window. The Unity
    // view only lays these rows out; compilation, policy, atomic allocation,
    // omissions and shared-pool ownership stay in planning/session services.
    public sealed class WorkspaceResourceRow
    {
        internal WorkspaceResourceRow(CastingBudgetLine line, string label,
            int wouldSpend, int executableCastCount)
        {
            PoolKey = line.PoolKey;
            Category = line.Category;
            Label = label ?? PlannerText.Get("resources.fallback");
            AvailableNow = line.AvailableNow;
            Needed = line.RequestedUsage;
            WouldSpend = wouldSpend;
            ShortBy = line.UnmetDemand;
            Unlimited = line.Unlimited;
            ExecutableCastCount = executableCastCount;
            ResponsibleCastingIds = new ReadOnlyCollection<string>(line.Traces.ToList());
        }

        public string PoolKey { get; private set; }
        public CastingCostCategory Category { get; private set; }
        public string Label { get; private set; }
        public int? AvailableNow { get; private set; }
        public int Needed { get; private set; }
        public int WouldSpend { get; private set; }
        public int ShortBy { get; private set; }
        public bool Unlimited { get; private set; }
        public int ExecutableCastCount { get; private set; }
        public IReadOnlyList<string> ResponsibleCastingIds { get; private set; }

        public string AvailableText { get { return Unlimited ? PlannerText.Get("resources.unlimited") : Number(AvailableNow); } }
        public string NeededText
        {
            get { return Unlimited ? CastCount(ResponsibleCastingIds.Count) : Needed.ToString(); }
        }
        public string WouldSpendText
        {
            get { return Unlimited ? CastCount(ExecutableCastCount) : WouldSpend.ToString(); }
        }
        public string ShortText { get { return ShortBy == 0 ? "—" : ShortBy.ToString(); } }

        private static string Number(int? value)
        {
            return value == null ? PlannerText.Get("resources.unknown") : value.Value.ToString();
        }

        private static string CastCount(int count)
        {
            return PlannerText.Get(count == 1 ? "resources.casts.one" : "resources.casts.many", count);
        }
    }

    public sealed class WorkspaceResourceGroup
    {
        internal WorkspaceResourceGroup(string key, string label,
            IEnumerable<WorkspaceResourceRow> rows)
        {
            Key = key ?? string.Empty;
            Label = label ?? PlannerText.Get("resources.shared");
            Rows = new ReadOnlyCollection<WorkspaceResourceRow>(
                (rows ?? new WorkspaceResourceRow[0]).ToList());
        }

        public string Key { get; private set; }
        public string Label { get; private set; }
        public IReadOnlyList<WorkspaceResourceRow> Rows { get; private set; }
    }

    public sealed class WorkspaceResourceReport
    {
        internal WorkspaceResourceReport(WorkspaceResourceScope scope,
            string scopeLabel, string policyLabel, string summary,
            int savedCastingCount, int executableCastingCount, int omissionCount,
            CastingApplyDecision decision, IEnumerable<WorkspaceResourceGroup> groups)
        {
            Scope = scope;
            ScopeLabel = scopeLabel ?? string.Empty;
            PolicyLabel = policyLabel ?? string.Empty;
            Summary = summary ?? string.Empty;
            SavedCastingCount = savedCastingCount;
            ExecutableCastingCount = executableCastingCount;
            OmissionCount = omissionCount;
            Decision = decision;
            Groups = new ReadOnlyCollection<WorkspaceResourceGroup>(
                (groups ?? new WorkspaceResourceGroup[0]).ToList());
        }

        public WorkspaceResourceScope Scope { get; private set; }
        public string ScopeLabel { get; private set; }
        public string PolicyLabel { get; private set; }
        public string Summary { get; private set; }
        public int SavedCastingCount { get; private set; }
        public int ExecutableCastingCount { get; private set; }
        public int OmissionCount { get; private set; }
        public CastingApplyDecision Decision { get; private set; }
        public IReadOnlyList<WorkspaceResourceGroup> Groups { get; private set; }
    }

    public sealed partial class CastingWorkspaceSession
    {
        public WorkspaceResourceReport BuildResourceReport(
            CastingWorkspaceInputs inputs, WorkspaceResourceScope scope)
        {
            if (inputs == null) throw new ArgumentNullException("inputs");
            string routine = scope == WorkspaceResourceScope.SelectedRoutine
                ? SelectedRoutineId : null;
            ExplicitCastingPlan plan = routine == null
                ? ForecastOnePass(inputs).Plan
                : Compile(inputs, routine, false);
            RefreshPoolLabels(inputs);
            CastingApplyDecision decision = _gate.Evaluate(plan, ApplyMode, routine);
            var executable = new HashSet<string>(decision.Allowed
                ? decision.ExecutableCastingIds : new string[0], StringComparer.Ordinal);
            var names = inputs.Snapshot.Units.ToDictionary(value => value.UnitId,
                value => string.IsNullOrWhiteSpace(value.DisplayName)
                    ? value.UnitId : value.DisplayName, StringComparer.Ordinal);

            var grouped = new Dictionary<string, List<WorkspaceResourceRow>>(
                StringComparer.Ordinal);
            foreach (CastingBudgetLine line in plan.BudgetLines)
            {
                List<string> owners = ResourceOwners(line, plan, inputs);
                string groupKey = owners.Count == 1 ? owners[0] : string.Empty;
                string groupLabel;
                if (groupKey.Length == 0 || !names.TryGetValue(groupKey, out groupLabel))
                    groupLabel = "Shared resources";
                string label = BudgetLabel(line);
                if (groupKey.Length != 0 && label.StartsWith(groupLabel + ": ",
                        StringComparison.InvariantCultureIgnoreCase))
                    label = label.Substring(groupLabel.Length + 2);
                label = WithPreparedRestrictions(label, line, plan, inputs);
                int spend = 0;
                int executableCasts = 0;
                foreach (ResolvedCasting casting in plan.Castings.Where(value =>
                    executable.Contains(value.CastingId) && line.Traces.Contains(value.CastingId)))
                {
                    executableCasts++;
                    spend += casting.Cost.Where(cost => cost.Category == line.Category &&
                        string.Equals(cost.PoolKey, line.PoolKey, StringComparison.Ordinal))
                        .Sum(cost => cost.Units);
                }
                List<WorkspaceResourceRow> rows;
                if (!grouped.TryGetValue(groupKey, out rows))
                {
                    rows = new List<WorkspaceResourceRow>();
                    grouped.Add(groupKey, rows);
                }
                rows.Add(new WorkspaceResourceRow(line, label, spend, executableCasts));
            }

            List<WorkspaceResourceGroup> groups = grouped.Select(pair =>
            {
                string label;
                if (pair.Key.Length == 0 || !names.TryGetValue(pair.Key, out label))
                    label = "Shared resources";
                return new WorkspaceResourceGroup(pair.Key, label,
                    pair.Value.OrderBy(value => value.Label,
                        StringComparer.InvariantCultureIgnoreCase)
                        .ThenBy(value => value.PoolKey, StringComparer.Ordinal));
            }).OrderBy(value => value.Key.Length == 0 ? 1 : 0)
                .ThenBy(value => value.Label, StringComparer.InvariantCultureIgnoreCase).ToList();

            int saved = plan.Castings.Count;
            int run = decision.Allowed ? decision.ExecutableCastingIds.Count : 0;
            int omitted = decision.Omissions.Count;
            string policy = ApplyMode == CastingApplyMode.ReadyCastsOnly
                ? PlannerText.Get("resources.policyAvailable") : PlannerText.Get("resources.policyStrict");
            string summary;
            if (!decision.Allowed)
            {
                int blocked = decision.BlockingCastings.Count;
                summary = blocked == 0 ? PlannerText.Get("resources.forecast.global", policy)
                    : PlannerText.Get(blocked == 1 ? "resources.forecast.blocked.one" : "resources.forecast.blocked.many", policy, blocked);
            }
            else if (run == 0)
                summary = PlannerText.Get(omitted == 1 ? "resources.forecast.empty.one" : "resources.forecast.empty.many", policy, omitted);
            else
            {
                string ready = PlannerText.Get(run == 1 ? "resources.forecast.run.one" : "resources.forecast.run.many", policy, run);
                summary = omitted == 0 ? PlannerText.Get("resources.forecast.runComplete", ready)
                    : PlannerText.Get(omitted == 1 ? "resources.forecast.runOmit.one" : "resources.forecast.runOmit.many", ready, omitted);
            }

            return new WorkspaceResourceReport(scope,
                routine == null ? PlannerText.Get("resources.all")
                    : PlannerText.Get("resources.scope", RoutineDisplayName(routine)),
                policy, summary, saved, run, omitted, decision, groups);
        }

        private static string WithPreparedRestrictions(string label,
            CastingBudgetLine line, ExplicitCastingPlan plan,
            CastingWorkspaceInputs inputs)
        {
            if (line.Category != CastingCostCategory.NativePool) return label;
            ResourcePoolSnapshot pool = inputs.Snapshot.ResourcePools.FirstOrDefault(value =>
                string.Equals(value.PoolKey, line.PoolKey, StringComparison.Ordinal));
            if (pool == null || pool.Kind != ResourcePoolKind.PreparedSlots) return label;
            var namesByProvider = inputs.Snapshot.Providers.ToDictionary(
                value => value.Key.Canonical,
                value => string.IsNullOrWhiteSpace(value.DisplayName)
                    ? value.SourceDisplayName : value.DisplayName,
                StringComparer.Ordinal);
            List<string> exact = plan.Castings.Where(value =>
                    line.Traces.Contains(value.CastingId) && value.Provider != null)
                .Select(value => namesByProvider.TryGetValue(value.Provider.Canonical,
                        out string name) ? name : string.Empty)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(value => value, StringComparer.InvariantCultureIgnoreCase).ToList();
            if (exact.Count == 0) return PlannerText.Get("resources.exact.label", label);
            string shown = string.Join(", ", exact.Take(3).ToArray());
            if (exact.Count > 3) shown += " +" + (exact.Count - 3);
            return PlannerText.Get("resources.exact.named", label, shown);
        }

        private static List<string> ResourceOwners(CastingBudgetLine line,
            ExplicitCastingPlan plan, CastingWorkspaceInputs inputs)
        {
            IEnumerable<string> owners;
            switch (line.Category)
            {
                case CastingCostCategory.NativePool:
                    owners = inputs.Snapshot.Providers.Where(value =>
                            string.Equals(value.ResourcePoolKey, line.PoolKey,
                                StringComparison.Ordinal))
                        .Select(value => value.Key.CasterUnitId);
                    break;
                case CastingCostCategory.EnhancementPool:
                    owners = inputs.Enhancements.Where(value =>
                            string.Equals(value.UsagePoolId, line.PoolKey,
                                StringComparison.Ordinal))
                        .Select(value => value.CasterUnitId);
                    break;
                default:
                    owners = inputs.Snapshot.Providers.Where(value =>
                            value.MaterialComponent != null &&
                            string.Equals(value.MaterialComponent.ItemGuid, line.PoolKey,
                                StringComparison.Ordinal))
                        .Select(value => value.Key.CasterUnitId);
                    break;
            }
            List<string> distinct = owners.Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.Ordinal).ToList();
            if (distinct.Count != 0) return distinct;
            return plan.Castings.Where(value => line.Traces.Contains(value.CastingId) &&
                    !string.IsNullOrEmpty(value.CasterUnitId))
                .Select(value => value.CasterUnitId).Distinct(StringComparer.Ordinal).ToList();
        }
    }
}
