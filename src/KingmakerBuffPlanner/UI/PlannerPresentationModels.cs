using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    public enum PlannerPresentationStatus
    {
        Neutral,
        Success,
        Warning,
        Failure,
        Disabled
    }

    public enum TargetPortraitState
    {
        Neutral,
        DirectSelectedAndCovered,
        DirectSelectedButUnavailable,
        IndirectlyCovered,
        InvalidTarget
    }

    public sealed class RoutineMembershipChipViewModel
    {
        internal RoutineMembershipChipViewModel(
            string routineId,
            string abbreviation,
            string label,
            bool active)
        {
            RoutineId = routineId ?? string.Empty;
            Abbreviation = abbreviation ?? string.Empty;
            Label = label ?? string.Empty;
            IsActive = active;
            Tooltip = (active ? "Configured in active " : "Also configured in ") +
                Label + ".";
        }

        public string RoutineId { get; private set; }
        public string Abbreviation { get; private set; }
        public string Label { get; private set; }
        public bool IsActive { get; private set; }
        public string Tooltip { get; private set; }
    }

    public sealed class BuffCardViewModel
    {
        internal BuffCardViewModel(SetupSourceRow source, PlannerSetupModel model,
            string activeRoutineId, bool selected)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (model == null) throw new ArgumentNullException("model");
            SourceId = source.SourceId;
            Name = string.IsNullOrWhiteSpace(source.DisplayName) ? "Unnamed buff" : source.DisplayName;
            Selected = selected;
            RoutineMemberships = BuildRoutineMemberships(model.Profile, source.SourceId,
                activeRoutineId);
            RoutineBadge = string.Join(" ", RoutineMemberships.Select(value =>
                value.Abbreviation).ToArray());
            RoutineProfile activeRoutine = model.Profile.Routines.First(routine =>
                routine.RoutineId == activeRoutineId);
            SourceAssignmentProfile activeAssignment = activeRoutine.Assignments
                .FirstOrDefault(assignment => assignment.SourceId == source.SourceId);
            int requested = activeAssignment == null ? 0 : activeAssignment
                .WantedTargetUnitIds.Distinct(StringComparer.Ordinal).Count();
            Configured = requested != 0;
            bool available = model.IsSourceAvailable(source);
            Availability = BuildAvailability(source, model);
            Configuration = requested == 0 ? "No targets selected" : requested == 1
                ? "1 target selected" : requested + " targets selected";
            Status = requested == 0 ? PlannerPresentationStatus.Neutral :
                BuildStatus(source, model, activeRoutineId, requested, available);
            SourceType = SourceSummary(source);
        }

        public string SourceId { get; private set; }
        public string Name { get; private set; }
        public string RoutineBadge { get; private set; }
        public IReadOnlyList<RoutineMembershipChipViewModel> RoutineMemberships
        {
            get; private set;
        }
        public string Availability { get; private set; }
        public string Configuration { get; private set; }
        public string SourceType { get; private set; }
        public bool Selected { get; private set; }
        public bool Configured { get; private set; }
        public PlannerPresentationStatus Status { get; private set; }

        private static PlannerPresentationStatus BuildStatus(SetupSourceRow source,
            PlannerSetupModel model, string routineId, int requested, bool available)
        {
            if (!available) return PlannerPresentationStatus.Failure;
            int legal = model.Profile.Routines.First(routine => routine.RoutineId == routineId)
                .Assignments.Where(assignment => assignment.SourceId == source.SourceId)
                .SelectMany(assignment => assignment.WantedTargetUnitIds)
                .Count(unitId => model.IsTargetLegal(source, routineId, unitId));
            if (legal == requested) return PlannerPresentationStatus.Success;
            return legal == 0 ? PlannerPresentationStatus.Failure : PlannerPresentationStatus.Warning;
        }

        private static IReadOnlyList<RoutineMembershipChipViewModel> BuildRoutineMemberships(
            BuffPlannerProfile profile, string sourceId, string activeRoutineId)
        {
            var values = new List<RoutineMembershipChipViewModel>();
            AddMembership(values, profile, sourceId, activeRoutineId,
                "long", "L", "Long");
            AddMembership(values, profile, sourceId, activeRoutineId,
                "important", "I", "Important");
            AddMembership(values, profile, sourceId, activeRoutineId,
                "short", "S", "Short");
            return values;
        }

        private static void AddMembership(
            ICollection<RoutineMembershipChipViewModel> values,
            BuffPlannerProfile profile,
            string sourceId,
            string activeRoutineId,
            string routineId,
            string abbreviation,
            string label)
        {
            if (Assigned(profile, routineId, sourceId))
                values.Add(new RoutineMembershipChipViewModel(routineId, abbreviation,
                    label, routineId == activeRoutineId));
        }

        private static bool Assigned(BuffPlannerProfile profile, string routineId, string sourceId)
        {
            RoutineProfile routine = profile.Routines.First(item => item.RoutineId == routineId);
            return routine.Assignments.Any(item => item.SourceId == sourceId &&
                item.WantedTargetUnitIds.Count != 0);
        }

        internal static string BuildAvailability(SetupSourceRow source, PlannerSetupModel model)
        {
            var usable = source.Providers.Where(provider =>
                string.IsNullOrEmpty(model.GetProviderRejectionReason(provider))).ToList();
            if (usable.Count == 0)
            {
                string reason = PlayerReason(model.GetSourceUnavailableReason(source));
                return string.IsNullOrWhiteSpace(reason) ? "Unavailable now" : reason;
            }
            if (usable.Any(provider => model.GetRemainingCasts(provider) == null))
                return usable.Count > 1 ? "At will · multiple sources" : "At will";
            int remaining = AvailableCastCount(usable, model);
            bool prepared = usable.Any(provider =>
                model.GetResourcePool(provider).Kind == ResourcePoolKind.PreparedSlots);
            if (remaining == 1) return prepared ? "1 prepared" : "1 available";
            return remaining + (prepared ? " prepared" : " available");
        }

        private static int RemainingForPool(IEnumerable<ProviderSnapshot> providers,
            PlannerSetupModel model)
        {
            List<ProviderSnapshot> values = providers.ToList();
            ResourcePoolSnapshot pool = model.GetResourcePool(values[0]);
            if (pool.Kind != ResourcePoolKind.PreparedSlots)
                return values.Max(provider => model.GetRemainingCasts(provider) ?? 0);
            var eligible = new HashSet<string>(values.SelectMany(provider => provider.EligibleTokenIds),
                StringComparer.Ordinal);
            int available = pool.Tokens.Count(token => token.Available && token.IsPrimary &&
                eligible.Contains(token.TokenId));
            int cost = Math.Max(1, values.Min(provider => provider.UnitsPerCast));
            return available / cost;
        }

        internal static int AvailableCastCount(IEnumerable<ProviderSnapshot> providers,
            PlannerSetupModel model)
        {
            return (providers ?? new ProviderSnapshot[0])
                .GroupBy(provider => provider.ResourcePoolKey, StringComparer.Ordinal)
                .Sum(group => RemainingForPool(group, model));
        }

        internal static string SourceSummary(SetupSourceRow source)
        {
            string type = source.Abilities.Select(ability => ability.SourceKind).Distinct().Count() > 1
                ? "Multiple sources" : PlayerSourceType(source.Ability.SourceKind);
            int providers = source.Providers.Select(provider => provider.Key.CasterUnitId)
                .Distinct(StringComparer.Ordinal).Count();
            return providers > 1 && type != "Multiple sources"
                ? type + " · " + providers + " sources" : type;
        }

        internal static string PlayerSourceType(SourceKind kind)
        {
            switch (kind)
            {
                case SourceKind.Spellbook: return "Spell";
                case SourceKind.AbilityResource:
                case SourceKind.Fact: return "Ability";
                case SourceKind.Item: return "Other";
                default: return "Other";
            }
        }

        internal static string PlayerReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) return string.Empty;
            if (reason == "resource pool exhausted") return "No prepared slot remains.";
            if (reason == "missing material component") return "Required item missing.";
            if (reason == "caster unavailable" || reason == "no available provider" ||
                reason == "no provider option was normalized")
                return "No eligible caster is currently available.";
            if (reason == "no legal party or pet target") return "No legal target.";
            if (reason == "banned by profile") return "No eligible caster is currently available.";
            return "Unavailable now";
        }
    }

    public sealed class EnhancementChoiceViewModel
    {
        internal EnhancementChoiceViewModel(string enhancementId, string title, string summary,
            string description, bool selected, bool available,
            bool checkboxStyle = false, bool affectsTargeting = false,
            string budgetNote = null, bool canSelect = true, bool canDeselect = true,
            string policyCaption = null, bool canTogglePolicy = false)
        {
            EnhancementId = enhancementId ?? string.Empty;
            Title = title ?? string.Empty;
            Summary = summary ?? string.Empty;
            Description = description ?? string.Empty;
            Selected = selected;
            Available = available;
            CheckboxStyle = checkboxStyle;
            AffectsTargeting = affectsTargeting;
            BudgetNote = budgetNote ?? string.Empty;
            // Adding requires current availability; removing a configured
            // selection never does — an exhausted or vanished enhancement
            // must stay individually removable.
            CanSelect = canSelect && available;
            CanDeselect = canDeselect && selected;
            PolicyCaption = policyCaption ?? string.Empty;
            CanTogglePolicy = canTogglePolicy;
        }

        public string EnhancementId { get; private set; }
        public string Title { get; private set; }
        public string Summary { get; private set; }
        public string Description { get; private set; }
        public bool Selected { get; private set; }
        public bool Available { get; private set; }
        public bool CheckboxStyle { get; private set; }
        public bool AffectsTargeting { get; private set; }
        public string BudgetNote { get; private set; }
        public bool CanSelect { get; private set; }
        public bool CanDeselect { get; private set; }
        public string PolicyCaption { get; private set; }
        public bool CanTogglePolicy { get; private set; }
    }

    // One routine-level budget line per shared enhancement usage pool, built
    // ONLY from the production plan's ResourcePoolAllocations. The UI never
    // keeps a second charge counter: native charges now, configured demand,
    // allocated casts, unmet demand, and projected balance all come from the
    // same authoritative result the executor uses.
    public sealed class EnhancementBudgetLineViewModel
    {
        internal EnhancementBudgetLineViewModel(
            string poolKey, string poolLabel, string ownerName, string effectName,
            int nativeChargesNow, int requestedUses, int allocatedUses,
            int unmetUses, int projectedRemaining, string routineLabel,
            IReadOnlyList<string> affectedLabels)
        {
            PoolKey = poolKey ?? string.Empty;
            PoolLabel = poolLabel ?? string.Empty;
            OwnerName = ownerName ?? string.Empty;
            EffectName = effectName ?? string.Empty;
            NativeChargesNow = nativeChargesNow;
            RequestedUses = requestedUses;
            AllocatedUses = allocatedUses;
            UnmetUses = unmetUses;
            ProjectedRemaining = projectedRemaining;
            RoutineLabel = routineLabel ?? string.Empty;
            AffectedLabels = affectedLabels ?? new string[0];
            Text = BuildText();
        }

        public string PoolKey { get; private set; }
        public string PoolLabel { get; private set; }
        public string OwnerName { get; private set; }
        public string EffectName { get; private set; }
        public int NativeChargesNow { get; private set; }
        public int RequestedUses { get; private set; }
        public int AllocatedUses { get; private set; }
        public int UnmetUses { get; private set; }
        public int ProjectedRemaining { get; private set; }
        public string RoutineLabel { get; private set; }
        public IReadOnlyList<string> AffectedLabels { get; private set; }
        public string Text { get; private set; }

        private string BuildText()
        {
            var builder = new System.Text.StringBuilder();
            builder.Append(PoolLabel);
            if (!string.IsNullOrEmpty(OwnerName))
                builder.Append(" — ").Append(OwnerName);
            builder.Append(": native now ").Append(NativeChargesNow);
            builder.Append(" | ").Append(RoutineLabel).Append(": ")
                .Append(RequestedUses).Append(" requested | ")
                .Append(AllocatedUses).Append(" allocated | ")
                .Append(UnmetUses).Append(" unmet");
            builder.Append(" | projected after ").Append(RoutineLabel)
                .Append(": ").Append(ProjectedRemaining);
            if (AffectedLabels.Count != 0)
                builder.Append(" | affected: ")
                    .Append(string.Join("; ", AffectedLabels.ToArray()));
            return builder.ToString();
        }
    }

    // Derives chooser/card budget presentation from one routine plan result.
    // Editing a plan does not consume charges and the routine is a template:
    // every number here is plan accounting against live native balances.
    public static class EnhancementBudgetModel
    {
        internal const string PoolKeyPrefix = "enhancement:";

        public static IReadOnlyList<EnhancementBudgetLineViewModel> Lines(
            RoutinePlanResult preview, PlannerSetupModel model, string routineId)
        {
            if (preview == null || model == null) return new EnhancementBudgetLineViewModel[0];
            string routineLabel = RoutineLabel(routineId);
            var lines = new List<EnhancementBudgetLineViewModel>();
            foreach (ResourcePoolAllocation allocation in preview.Plan.ResourceAllocations)
            {
                if (!IsEnhancementPool(allocation.PoolKey)) continue;
                CastEnhancementSnapshot representative = Representative(
                    model, PoolUsageId(allocation.PoolKey));
                if (representative == null) continue;
                string ownerName = model.Snapshot.Units
                    .FirstOrDefault(unit => unit.UnitId == representative.CasterUnitId)
                    ?.DisplayName ?? representative.CasterUnitId;
                lines.Add(new EnhancementBudgetLineViewModel(
                    allocation.PoolKey,
                    representative.DisplayName,
                    ownerName,
                    PlannerSetupModel.EffectName(representative),
                    allocation.AvailableNow,
                    allocation.RequestedUsage,
                    allocation.AllocatedUsage,
                    allocation.UnmetDemand,
                    allocation.ForecastRemaining,
                    routineLabel,
                    BuildAffectedLabels(preview, model, routineId, allocation)));
            }
            return lines;
        }

        // Chooser sticky-summary budget: compact and length-bounded so any
        // number of pools and any name lengths stay renderable in the fixed
        // summary area. Full per-pool detail lives on each row's tooltip and
        // in Assignments & Resources; it is never silently truncated away.
        public static string SummaryText(IReadOnlyList<EnhancementBudgetLineViewModel> lines,
            string routineId)
        {
            List<EnhancementBudgetLineViewModel> demanded = (lines ?? new EnhancementBudgetLineViewModel[0])
                .Where(line => line.RequestedUses != 0 || line.AllocatedUses != 0).ToList();
            if (demanded.Count == 0) return string.Empty;
            string routineLabel = RoutineLabel(routineId);
            var builder = new System.Text.StringBuilder();
            int worstUnmet = demanded.Max(line => line.UnmetUses);
            if (worstUnmet == 0)
            {
                builder.Append("Shared enhancement budgets (").Append(routineLabel)
                    .Append("): all requested charges funded across ")
                    .Append(demanded.Count)
                    .Append(demanded.Count == 1 ? " pool. Editing never consumes charges." :
                        " pools. Editing never consumes charges.")
                    .Append(" Row tooltips and Assignments & Resources carry each pool's detail.");
            }
            else
            {
                EnhancementBudgetLineViewModel worst = demanded
                    .Where(line => line.UnmetUses == worstUnmet)
                    .OrderByDescending(line => line.RequestedUses)
                    .First();
                builder.Append("Shared enhancement budgets (").Append(routineLabel)
                    .Append("): ").Append(demanded.Count)
                    .Append(demanded.Count == 1 ? " pool with demand; " : " pools with demand; ")
                    .Append(worst.PoolLabel);
                if (!string.IsNullOrEmpty(worst.OwnerName))
                    builder.Append(" — ").Append(worst.OwnerName);
                builder.Append(" is short ").Append(worst.UnmetUses)
                    .Append(worst.UnmetUses == 1 ? " charge" : " charges")
                    .Append(" of ").Append(worst.RequestedUses)
                    .Append(" requested. Editing never consumes charges.")
                    .Append(" Row tooltips and Assignments & Resources carry each pool's detail.");
            }
            return BoundText(builder.ToString(), MaximumSummaryLength);
        }

        // Row-level note for one enhancement choice, scoped to one spell (and
        // one child assignment when the chooser was opened from it). All
        // counts come from the same production plan; the note distinguishes
        // requested targets, funded targets, already-active targets, and the
        // casts/charges actually allocated to this scope.
        public static string ChoiceNote(CastEnhancementSnapshot enhancement,
            bool selected, IReadOnlyList<EnhancementBudgetLineViewModel> lines,
            RoutinePlanResult preview, PlannerSetupModel model, string routineId,
            string sourceId, string assignmentId)
        {
            if (enhancement == null) return string.Empty;
            var parts = new List<string>();
            EnhancementBudgetLineViewModel line = LineFor(lines, enhancement.UsagePoolId);
            if (line != null && (line.RequestedUses != 0 || line.AllocatedUses != 0))
                parts.Add(line.RoutineLabel + " pool: " + line.RequestedUses +
                    " requested, " + line.AllocatedUses + " allocated" +
                    (line.UnmetUses == 0 ? string.Empty : ", " + line.UnmetUses + " unmet"));
            if (selected && preview != null && model != null &&
                !string.IsNullOrEmpty(sourceId))
            {
                SpellCoverage coverage = SpellCoverage.For(preview, model, routineId,
                    sourceId, assignmentId, enhancement);
                parts.Add(coverage.Note);
            }            else if (enhancement.UsageUnitsPerCast > 0)
            {
                parts.Add("selecting reserves " + enhancement.UsageUnitsPerCast +
                    (enhancement.UsageUnitsPerCast == 1 ? " charge" : " charges") +
                    " per enhanced cast");
            }
            return BoundText(string.Join(" | ", parts.ToArray()), MaximumNoteLength);
        }

        // Local accounting for one spell (optionally one child assignment)
        // against the authoritative plan. Never a parallel allocation rule:
        // every number is read back out of the plan's own steps/outcomes.
        internal sealed class SpellCoverage
        {
            internal int RequestedTargets;
            internal int FundedTargets;
            internal int SkippedTargets;
            internal int AllocatedCasts;
            internal int AllocatedCharges;
            internal bool Communal;
            internal string ScopeLabel = "this spell";

            internal string Note
            {
                get
                {
                    var builder = new System.Text.StringBuilder();
                    if (Communal)
                    {
                        builder.Append(ScopeLabel).Append(": ")
                            .Append(AllocatedCasts)
                            .Append(AllocatedCasts == 1 ? " communal cast covers its targets"
                                : " communal casts cover their targets")
                            .Append(" (").Append(AllocatedCharges)
                            .Append(AllocatedCharges == 1 ? " charge" : " charges")
                            .Append(" allocated)");
                    }
                    else
                    {
                        builder.Append(ScopeLabel).Append(": ")
                            .Append(FundedTargets).Append("/")
                            .Append(RequestedTargets).Append(" targets funded")
                            .Append(" (").Append(AllocatedCharges)
                            .Append(AllocatedCharges == 1 ? " charge" : " charges")
                            .Append(" allocated)");
                    }
                    if (SkippedTargets != 0)
                        builder.Append("; ").Append(SkippedTargets).Append(" already active");
                    return builder.ToString();
                }
            }

            internal static SpellCoverage For(RoutinePlanResult preview,
                PlannerSetupModel model, string routineId, string sourceId,
                string assignmentId, CastEnhancementSnapshot enhancement)
            {
                var coverage = new SpellCoverage();
                coverage.ScopeLabel = string.IsNullOrEmpty(assignmentId)
                    ? "this spell" : "this assignment";
                // Saved assignments may key on the ability's canonical id
                // while the catalog row exposes the aggregate id; both refer
                // to this spell, so coverage matches either identity.
                var sourceKeys = new HashSet<string>(StringComparer.Ordinal);
                if (!string.IsNullOrEmpty(sourceId)) sourceKeys.Add(sourceId);
                SetupSourceRow row = string.IsNullOrEmpty(sourceId) ? null :
                    model.Sources.FirstOrDefault(item =>
                        string.Equals(item.SourceId, sourceId, StringComparison.Ordinal));
                if (row != null)
                    foreach (AbilityKey key in row.Abilities)
                        sourceKeys.Add(key.Canonical);
                RoutineProfile routine = model.Profile.Routines.FirstOrDefault(item =>
                    item.RoutineId == routineId);
                if (routine == null) return coverage;
                var requestingTargets = new HashSet<string>(StringComparer.Ordinal);
                foreach (SourceAssignmentProfile assignment in routine.Assignments)
                {
                    if (!sourceKeys.Contains(assignment.SourceId))
                        continue;
                    foreach (CastingAssignmentProfile casting in assignment.CastingAssignments)
                    {
                        if (!string.IsNullOrEmpty(assignmentId) &&
                            casting.AssignmentId != assignmentId) continue;
                        if (!casting.Enhancements.Any(selection =>
                                selection.EnhancementId == enhancement.EnhancementId)) continue;
                        foreach (string target in casting.TargetUnitIds)
                            requestingTargets.Add(target);
                    }
                }
                coverage.RequestedTargets = requestingTargets.Count;
                var fundedTargets = new HashSet<string>(StringComparer.Ordinal);
                foreach (CastStep step in preview.Plan.Steps)
                {
                    if (!sourceKeys.Contains(step.SourceId))
                        continue;
                    if (!string.IsNullOrEmpty(assignmentId) &&
                        step.AssignmentId != assignmentId) continue;
                    if (!step.EnhancementIds.Contains(enhancement.EnhancementId)) continue;
                    coverage.AllocatedCasts++;
                    int charges;
                    if (step.EnhancementUsageByPool.TryGetValue(
                            enhancement.UsagePoolId, out charges))
                        coverage.AllocatedCharges += charges;
                    if (step.MassCast) coverage.Communal = true;
                    foreach (string recipient in step.ExpectedRecipientUnitIds)
                        if (requestingTargets.Contains(recipient))
                            fundedTargets.Add(recipient);
                }
                coverage.FundedTargets = fundedTargets.Count;
                foreach (TargetPlanOutcome outcome in preview.Plan.Outcomes)
                {
                    if (!sourceKeys.Contains(outcome.SourceId))
                        continue;
                    if (!string.IsNullOrEmpty(assignmentId) &&
                        outcome.AssignmentId != assignmentId) continue;
                    if (!requestingTargets.Contains(outcome.UnitId)) continue;
                    if (outcome.Kind == TargetOutcomeKind.SkippedAlreadyActive)
                        coverage.SkippedTargets++;
                }
                return coverage;
            }
        }

        public static EnhancementBudgetLineViewModel LineFor(
            IReadOnlyList<EnhancementBudgetLineViewModel> lines, string usagePoolId)
        {
            if (lines == null || string.IsNullOrEmpty(usagePoolId)) return null;
            string key = PoolKeyPrefix + usagePoolId;
            return lines.FirstOrDefault(line => string.Equals(line.PoolKey, key,
                StringComparison.Ordinal));
        }

        // Presentation-length bounds (C3): the sticky summary and the row
        // note must stay renderable inside their fixed areas regardless of
        // pool count or name lengths. Full unbounded detail always remains
        // available on the row tooltip and in Assignments & Resources.
        internal const int MaximumSummaryLength = 300;
        internal const int MaximumNoteLength = 160;

        internal static string BoundText(string value, int maximum)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maximum) return value;
            return value.Substring(0, Math.Max(0, maximum - 1)) + "…";
        }

        private static List<string> BuildAffectedLabels(RoutinePlanResult preview,
            PlannerSetupModel model, string routineId, ResourcePoolAllocation allocation)
        {
            var labels = new List<string>();
            RoutineProfile routine = model.Profile.Routines.FirstOrDefault(item =>
                item.RoutineId == routineId);
            if (routine == null) return labels;
            var enhancementsByPool = new HashSet<string>(model.Enhancements
                .Where(value => value.UsagePoolId == PoolUsageId(allocation.PoolKey))
                .Select(value => value.EnhancementId), StringComparer.Ordinal);
            var assignmentIds = new HashSet<string>(allocation.Traces, StringComparer.Ordinal);
            foreach (SourceAssignmentProfile assignment in routine.Assignments)
            {
                foreach (CastingAssignmentProfile casting in assignment.CastingAssignments)
                {
                    if (!assignmentIds.Contains(casting.AssignmentId)) continue;
                    if (!casting.Enhancements.Any(selection =>
                            enhancementsByPool.Contains(selection.EnhancementId))) continue;
                    SetupSourceRow source = model.Sources.FirstOrDefault(item =>
                        item.SourceId == assignment.SourceId);
                    string sourceName = source == null ? assignment.SourceId : source.DisplayName;
                    string targets = string.Join(", ", casting.TargetUnitIds.Select(id =>
                    {
                        UnitSnapshot unit = model.Snapshot.Units.FirstOrDefault(value =>
                            value.UnitId == id);
                        return unit == null || string.IsNullOrWhiteSpace(unit.DisplayName)
                            ? id : unit.DisplayName;
                    }).ToArray());
                    labels.Add(sourceName + (string.IsNullOrEmpty(targets)
                        ? string.Empty : " (" + targets + ")"));
                }
            }
            return labels;
        }

        internal static bool IsEnhancementPool(string poolKey)
        {
            return poolKey != null && poolKey.StartsWith(PoolKeyPrefix,
                StringComparison.Ordinal);
        }

        internal static string PoolUsageId(string poolKey)
        {
            return string.IsNullOrEmpty(poolKey) ? string.Empty
                : poolKey.Substring(PoolKeyPrefix.Length);
        }

        internal static string RoutineLabel(string routineId)
        {
            if (string.IsNullOrEmpty(routineId)) return "Routine";
            return char.ToUpperInvariant(routineId[0]) + routineId.Substring(1);
        }

        private static CastEnhancementSnapshot Representative(PlannerSetupModel model,
            string usagePoolId)
        {
            return model.Enhancements.FirstOrDefault(value =>
                string.Equals(value.UsagePoolId, usagePoolId, StringComparison.Ordinal));
        }
    }

    public sealed class ProviderPolicyRowViewModel
    {
        internal ProviderPolicyRowViewModel(
            ProviderSnapshot provider,
            PlannerSetupModel model,
            int order,
            int count)
        {
            ProviderKey = provider.Key.Canonical;
            CasterUnitId = provider.Key.CasterUnitId;
            CasterName = model.GetCasterDisplayName(provider);
            ProviderPreferenceProfile preference =
                model.GetProviderPreference(ProviderKey);
            Enabled = preference == null || !preference.Banned;
            MaximumCasts = preference == null ? null : preference.MaximumCasts;
            Priority = preference == null ? null : preference.Priority;
            Order = order + 1;
            CanMoveEarlier = order > 0;
            CanMoveLater = order + 1 < count;
            SpellLevel = provider.SpellLevel;
            int? remaining = model.GetRemainingCasts(provider);
            Remaining = remaining == null ? "At will" :
                remaining.Value + (remaining.Value == 1
                    ? " cast remaining" : " casts remaining");
            string unavailable =
                model.GetProviderTemporaryUnavailableReason(provider);
            UnavailableReason = unavailable == "resource pool exhausted" &&
                model.GetResourcePool(provider).Kind !=
                    ResourcePoolKind.PreparedSlots
                ? "No casts remain right now."
                : BuffCardViewModel.PlayerReason(unavailable);
            Source = SourceDescription(provider);
            MaximumSelectable = Math.Max(6,
                Math.Min(20, remaining ?? 6));
            if (MaximumCasts != null)
                MaximumSelectable = Math.Max(MaximumSelectable,
                    Math.Min(20, MaximumCasts.Value));
        }

        public string ProviderKey { get; private set; }
        public string CasterUnitId { get; private set; }
        public string CasterName { get; private set; }
        public string Source { get; private set; }
        public int SpellLevel { get; private set; }
        public string Remaining { get; private set; }
        public string UnavailableReason { get; private set; }
        public bool Enabled { get; private set; }
        public int? MaximumCasts { get; private set; }
        public int? Priority { get; private set; }
        public int Order { get; private set; }
        public bool CanMoveEarlier { get; private set; }
        public bool CanMoveLater { get; private set; }
        public int MaximumSelectable { get; private set; }

        public int? NextMaximumCasts()
        {
            if (MaximumCasts == null) return 1;
            return MaximumCasts.Value >= MaximumSelectable
                ? (int?)null : MaximumCasts.Value + 1;
        }

        private static string SourceDescription(ProviderSnapshot provider)
        {
            if (provider.Key.Ability.SourceKind == SourceKind.Spellbook)
            {
                string book = provider.Key.SpellbookGuid;
                if (book.Length > 8) book = book.Substring(0, 8);
                return "Spellbook " + book +
                    (provider.SpellLevel > 0 ? " | spell level " +
                        provider.SpellLevel : " | cantrip") +
                    (string.IsNullOrWhiteSpace(provider.Key.SourceInstanceId)
                        ? string.Empty : " | " + provider.Key.SourceInstanceId);
            }
            string kind = provider.Key.Ability.SourceKind == SourceKind.AbilityResource
                ? "Resource ability" :
                provider.Key.Ability.SourceKind == SourceKind.Fact
                    ? "Granted ability" :
                provider.Key.Ability.SourceKind == SourceKind.Item
                    ? "Item source" : "Ability source";
            return kind + (string.IsNullOrWhiteSpace(provider.Key.SourceInstanceId)
                ? string.Empty : " | " + provider.Key.SourceInstanceId);
        }
    }

    public sealed class CasterPolicyViewModel
    {
        private CasterPolicyViewModel(
            string summary,
            string description,
            bool warning,
            IEnumerable<ProviderPolicyRowViewModel> providers)
        {
            Summary = summary ?? string.Empty;
            Description = description ?? string.Empty;
            Warning = warning;
            Providers = providers.ToList().AsReadOnly();
        }

        public string Summary { get; private set; }
        public string Description { get; private set; }
        public bool Warning { get; private set; }
        public IReadOnlyList<ProviderPolicyRowViewModel> Providers { get; private set; }

        internal static CasterPolicyViewModel Empty()
        {
            return new CasterPolicyViewModel(
                "Casters: None",
                "Select a buff to choose its casters.",
                false,
                new ProviderPolicyRowViewModel[0]);
        }

        public static CasterPolicyViewModel Create(
            SetupSourceRow source,
            PlannerSetupModel model,
            string routineId,
            RoutinePlanResult preview)
        {
            if (source == null || model == null) return Empty();
            List<ProviderSnapshot> ordered = model.GetOrderedProviders(source).ToList();
            var rows = ordered.Select((provider, index) =>
                new ProviderPolicyRowViewModel(
                    provider, model, index, ordered.Count)).ToList();
            RoutineProfile routine = model.Profile.Routines.First(item =>
                item.RoutineId == routineId);
            SourceAssignmentProfile assignment = routine.Assignments.FirstOrDefault(
                item => item.SourceId == source.SourceId);
            int requested = assignment == null
                ? 0 : assignment.WantedTargetUnitIds.Count;
            List<CastStep> steps = preview == null ? new List<CastStep>() :
                preview.Plan.Steps.Where(step =>
                    step.SourceId == source.SourceId).ToList();
            int unfulfilled = preview == null ? 0 : preview.Plan.Outcomes.Count(outcome =>
                outcome.SourceId == source.SourceId &&
                outcome.Kind == TargetOutcomeKind.Unfulfilled);

            string summary;
            bool warning = requested != 0 && unfulfilled != 0;
            if (requested != 0)
            {
                var counts = steps.GroupBy(step => step.Provider.Canonical,
                        StringComparer.Ordinal)
                    .Select(group => new
                    {
                        Key = group.Key,
                        Count = group.Count()
                    }).ToList();
                string allocations = string.Join(", ", counts.Select(value =>
                {
                    ProviderPolicyRowViewModel row = rows.FirstOrDefault(item =>
                        item.ProviderKey == value.Key);
                    return (row == null ? value.Key : row.CasterName) +
                        " " + value.Count;
                }).ToArray());
                summary = counts.Count == 0
                    ? "Planned casters: None"
                    : "Planned casters: " + allocations;
                if (unfulfilled != 0)
                    summary += " | " + unfulfilled + " unfulfilled";
            }
            else
            {
                bool automatic = rows.All(row => row.Enabled &&
                    row.Priority == null && row.MaximumCasts == null);
                if (automatic) summary = "Casters: Automatic";
                else
                {
                    string configured = string.Join(", ", rows.Where(row => row.Enabled)
                        .Select(row => row.CasterName +
                            (row.MaximumCasts == null ? string.Empty :
                                " (max " + row.MaximumCasts.Value + ")"))
                        .ToArray());
                    summary = configured.Length == 0
                        ? "Casters: None enabled" : "Casters: " + configured;
                    warning = configured.Length == 0;
                }
            }
            string description = string.Join("\n", rows.Select(row =>
                row.Order + ". " + row.CasterName + " | " + row.Source +
                " | " + row.Remaining +
                (row.Enabled ? string.Empty : " | Do not use") +
                (row.MaximumCasts == null ? string.Empty :
                    " | maximum per run " + row.MaximumCasts.Value) +
                (string.IsNullOrWhiteSpace(row.UnavailableReason)
                    ? string.Empty : " | " + row.UnavailableReason)).ToArray());
            if (warning)
                description = "Provider policy cannot cover every selected target.\n" +
                    description;
            return new CasterPolicyViewModel(
                summary, description, warning, rows);
        }
    }

    public sealed class SelectedCastingViewModel
    {
        private SelectedCastingViewModel(string casterText, string casterDetail,
            string enhancementLabel, string enhancementDescription, int candidateCount,
            IEnumerable<string> selectedEnhancementIds,
            IEnumerable<EnhancementChoiceViewModel> choices,
            CasterPolicyViewModel casterPolicy,
            IReadOnlyList<EnhancementBudgetLineViewModel> budgetLines,
            string enhancementBudgetText,
            bool enhancementWarning,
            string assignmentId)
        {
            CasterText = casterText;
            CasterDetail = casterDetail;
            EnhancementLabel = enhancementLabel;
            EnhancementDescription = enhancementDescription;
            CandidateCount = candidateCount;
            SelectedEnhancementIds = new ReadOnlyCollection<string>(
                (selectedEnhancementIds ?? new string[0]).Where(value =>
                    !string.IsNullOrWhiteSpace(value)).Distinct(
                    StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToList());
            SelectedEnhancementId = SelectedEnhancementIds.FirstOrDefault() ??
                string.Empty;
            Choices = choices.ToList().AsReadOnly();
            CasterPolicy = casterPolicy ?? CasterPolicyViewModel.Empty();
            BudgetLines = budgetLines ?? new EnhancementBudgetLineViewModel[0];
            EnhancementBudgetText = enhancementBudgetText ?? string.Empty;
            EnhancementWarning = enhancementWarning;
            AssignmentId = assignmentId ?? string.Empty;
        }

        public string CasterText { get; private set; }
        public string CasterDetail { get; private set; }
        public string EnhancementLabel { get; private set; }
        public string EnhancementDescription { get; private set; }
        public int CandidateCount { get; private set; }
        public string SelectedEnhancementId { get; private set; }
        public IReadOnlyList<string> SelectedEnhancementIds
        { get; private set; }
        public IReadOnlyList<EnhancementChoiceViewModel> Choices { get; private set; }
        public CasterPolicyViewModel CasterPolicy { get; private set; }
        public IReadOnlyList<EnhancementBudgetLineViewModel> BudgetLines
        { get; private set; }
        public string EnhancementBudgetText { get; private set; }
        public bool EnhancementWarning { get; private set; }
        public string AssignmentId { get; private set; }

        public static SelectedCastingViewModel Create(SetupSourceRow source,
            PlannerSetupModel model, string routineId, RoutinePlanResult preview)
        {
            return Create(source, model, routineId, preview, null);
        }

        public static SelectedCastingViewModel Create(SetupSourceRow source,
            PlannerSetupModel model, string routineId, RoutinePlanResult preview,
            string assignmentId)
        {
            if (source == null || model == null)
                return new SelectedCastingViewModel("Caster: None", string.Empty,
                    "Enhancement: None available", "Select a buff to choose an enhancement.",
                    0, new string[0], new[] { NoneChoice(true) },
                    CasterPolicyViewModel.Empty(),
                    new EnhancementBudgetLineViewModel[0], string.Empty, false, null);

            // One authoritative selection scope: the Automatic child for the
            // ordinary chooser, the exact child assignment for the
            // assignment-aware chooser. Choices, selected state, notes, and
            // the unavailable loop all read this list — never the Automatic
            // state when a specific assignment was asked for.
            IReadOnlyList<string> selectedIds = string.IsNullOrEmpty(assignmentId)
                ? model.GetSelectedEnhancementIds(routineId)
                : model.GetAssignmentEnhancementIds(routineId, source.SourceId,
                    assignmentId);
            Dictionary<string, bool> requiredBySelection =
                SelectionPolicies(model, routineId, source.SourceId, assignmentId);
            IReadOnlyList<CastEnhancementSnapshot> applicable =
                model.GetApplicableEnhancements();
            IReadOnlyList<EnhancementBudgetLineViewModel> budgetLines =
                EnhancementBudgetModel.Lines(preview, model, routineId);
            var choices = new List<EnhancementChoiceViewModel> {
                NoneChoice(selectedIds.Count == 0) };
            choices.AddRange(applicable.Select(value =>
            {
                bool selected = selectedIds.Contains(value.EnhancementId);
                return Choice(value, selected,
                    // Assignment scope: a choice is offered as available only
                    // when THIS child's own providers qualify for it, not the
                    // source-wide union.
                    string.IsNullOrEmpty(assignmentId) ||
                        IsApplicableToAssignment(value, model, source, routineId,
                            assignmentId),
                    model, routineId, preview, budgetLines, source.SourceId,
                    assignmentId, requiredBySelection);
            }));
            foreach (string unavailableId in selectedIds.Where(id =>
                !applicable.Any(value => value.EnhancementId == id)))
            {
                CastEnhancementSnapshot selected = model.GetEnhancement(
                    unavailableId);
                choices.Add(selected == null
                    ? new EnhancementChoiceViewModel(unavailableId,
                        "Unavailable enhancement", "Unavailable",
                        "Persisted enhancement source: " + unavailableId +
                        ". Click to remove this persisted selection.",
                        true, false, false, false, string.Empty,
                        false, true)
                    : Choice(selected, true, false, model, routineId,
                        preview, budgetLines, source.SourceId,
                        assignmentId, requiredBySelection));
            }

            CasterPolicyViewModel casterPolicy = CasterPolicyViewModel.Create(
                source, model, routineId, preview);

            string label = model.GetEnhancementSummary(routineId);
            string description = model.GetEnhancementDescription(routineId);
            bool warning = false;
            if (selectedIds.Count != 0 && budgetLines.Count != 0)
            {
                foreach (string id in selectedIds)
                {
                    CastEnhancementSnapshot selected = model.GetEnhancement(id);
                    EnhancementBudgetLineViewModel line = selected == null ? null :
                        EnhancementBudgetModel.LineFor(budgetLines, selected.UsagePoolId);
                    if (line == null || line.UnmetUses <= 0) continue;
                    warning = true;
                    label += " | " + line.UnmetUses +
                        (line.UnmetUses == 1 ? " charge" : " charges") +
                        " unmet in " + line.RoutineLabel;                }
                string budgetText = EnhancementBudgetModel.SummaryText(budgetLines, routineId);
                if (!string.IsNullOrEmpty(budgetText))
                    description += "\n\n" + budgetText;
            }
            return new SelectedCastingViewModel(casterPolicy.Summary,
                casterPolicy.Description,
                label, description,
                applicable.Count, selectedIds, choices, casterPolicy,
                budgetLines,
                EnhancementBudgetModel.SummaryText(budgetLines, routineId),
                warning, assignmentId);
        }

        private static Dictionary<string, bool> SelectionPolicies(
            PlannerSetupModel model, string routineId, string sourceId, string assignmentId)
        {
            var policies = new Dictionary<string, bool>(StringComparer.Ordinal);
            RoutineProfile routine = model.Profile.Routines.FirstOrDefault(item =>
                item.RoutineId == routineId);
            if (routine == null) return policies;
            foreach (SourceAssignmentProfile assignment in routine.Assignments)
            {
                if (!string.Equals(assignment.SourceId, sourceId, StringComparison.Ordinal))
                    continue;
                foreach (CastingAssignmentProfile casting in assignment.CastingAssignments)
                {
                    if (!string.IsNullOrEmpty(assignmentId) &&
                        casting.AssignmentId != assignmentId) continue;
                    foreach (EnhancementSelectionProfile selection in casting.Enhancements)
                        policies[selection.EnhancementId] =
                            selection.Required == null || selection.Required.Value;
                }
            }
            return policies;
        }

        // Availability for one child assignment: the enhancement must be
        // applicable to at least one provider that THIS assignment's own
        // pins/targeting resolve to — the same resolver the planner uses.
        private static bool IsApplicableToAssignment(CastEnhancementSnapshot enhancement,
            PlannerSetupModel model, SetupSourceRow source, string routineId,
            string assignmentId)
        {
            foreach (AssignmentProviderOption candidate in model.GetAssignmentProviderOptions(
                    source, routineId))
                if (string.Equals(candidate.AssignmentId, assignmentId,
                        StringComparison.Ordinal) &&
                    enhancement.IsApplicable(candidate.Option.Provider))
                    return true;
            return false;
        }

        private static EnhancementChoiceViewModel NoneChoice(bool selected)
        {
            return new EnhancementChoiceViewModel(string.Empty, "None", "Unenhanced cast",
                "Cast without a temporary casting enhancement.", selected, true);
        }

        private static EnhancementChoiceViewModel Choice(CastEnhancementSnapshot value,
            bool selected, bool available, PlannerSetupModel model, string routineId,
            RoutinePlanResult preview,
            IReadOnlyList<EnhancementBudgetLineViewModel> budgetLines, string sourceId,
            string assignmentId, Dictionary<string, bool> requiredBySelection)
        {
            string uses = value.RemainingUses == null ? "Uses not limited" :
                value.RemainingUses.Value + (value.RemainingUses.Value == 1 ? " use" : " uses");
            string summary = PlannerSetupModel.EffectName(value) + " | " + uses;
            string owner = model.Snapshot.Units.FirstOrDefault(unit =>
                unit.UnitId == value.CasterUnitId)?.DisplayName ?? value.CasterUnitId;
            string description = "Owner: " + owner + "\nApplies " +
                PlannerSetupModel.EffectName(value) + " to this cast." +
                (value.Category == CastEnhancementCategory.MetamagicRod
                    ? "\nSpell-level limit: " + value.MaximumSpellLevel
                    : "\nRequires the matching live caster feature and spell qualification.") +
                (string.IsNullOrWhiteSpace(value.Description)
                    ? string.Empty : "\n" + value.Description);
            EnhancementBudgetLineViewModel line = EnhancementBudgetModel.LineFor(
                budgetLines, value.UsagePoolId);
            if (line != null && (line.RequestedUses != 0 || line.AllocatedUses != 0))
                description += "\n" + line.Text;
            if (!available) description = "Unavailable: " + description;
            string budgetNote = EnhancementBudgetModel.ChoiceNote(value, selected,
                budgetLines, preview, model, routineId, sourceId, assignmentId);
            // Policy caption honesty: a targeting modifier can never be
            // optional (the planner never drops it), so it states that;
            // everything else states its actual required/optional policy and
            // may toggle when an assignment scope is editing it.
            bool required;
            if (!requiredBySelection.TryGetValue(value.EnhancementId, out required))
                required = true;
            string policyCaption = value.AffectsTargeting
                ? "REQUIRED (targeting)"
                : (required ? "REQUIRED" : "OPTIONAL");
            bool canTogglePolicy = !string.IsNullOrEmpty(assignmentId) && selected &&
                !value.AffectsTargeting;
            return new EnhancementChoiceViewModel(value.EnhancementId, value.DisplayName,
                summary, description, selected, available,
                value.AffectsTargeting, value.AffectsTargeting, budgetNote,
                true, true, policyCaption, canTogglePolicy);
        }

    }

    public static class ChooserScrollLayoutContract
    {
        // Must mirror the shared factory scroll view: VerticalLayoutGroup
        // padding 4/4/4/4 and spacing 4 on content, and the fixed per-row
        // LayoutElement heights the two modal choosers install.
        public const float RowSpacing = 4f;
        public const float ContentPadding = 4f;
        public const float EnhancementRowHeight = 68f;
        public const float ScrollbarWidth = 18f;
        public const float MinimumHandleRatio = 0.1f;

        public static float ContentHeight(int rowCount, float rowHeight)
        {
            if (rowCount <= 0 || rowHeight <= 0f) return 0f;
            return (ContentPadding * 2f) + (rowCount * rowHeight) +
                ((rowCount - 1) * RowSpacing);
        }

        public static float MaxScrollOffset(float viewportHeight, float contentHeight)
        {
            if (viewportHeight <= 0f || contentHeight <= 0f) return 0f;
            float offset = contentHeight - viewportHeight;
            return offset > 0f ? offset : 0f;
        }

        public static float ClampScrollOffset(float offset, float viewportHeight,
            float contentHeight)
        {
            if (offset < 0f) return 0f;
            float maximum = MaxScrollOffset(viewportHeight, contentHeight);
            return offset > maximum ? maximum : offset;
        }

        public static float ScrollbarHandleRatio(float viewportHeight, float contentHeight)
        {
            if (contentHeight <= 0f || viewportHeight <= 0f) return 1f;
            float ratio = viewportHeight / contentHeight;
            if (ratio < MinimumHandleRatio) return MinimumHandleRatio;
            return ratio > 1f ? 1f : ratio;
        }

        // Returns the scroll offset (distance the content top is pulled up
        // inside the viewport) that reveals the given zero-based row, changing
        // the offset only when the row sits outside the visible window.
        public static float OffsetRevealingRow(int rowIndex, float currentOffset,
            float viewportHeight, float contentHeight, float rowHeight)
        {
            if (rowIndex < 0) return ClampScrollOffset(currentOffset,
                viewportHeight, contentHeight);
            float rowTop = ContentPadding + (rowIndex * (rowHeight + RowSpacing));
            float rowBottom = rowTop + rowHeight;
            float offset = currentOffset;
            if (rowTop < offset) offset = rowTop;
            else if (rowBottom > offset + viewportHeight)
                offset = rowBottom - viewportHeight;
            return ClampScrollOffset(offset, viewportHeight, contentHeight);
        }
    }

    public static class CastingPanelLayoutContract
    {
        public const int ButtonFontSize = 17;
        public const int LabelVerticalPadding = 1;
        public const float MinimumEnhancementButtonHeight = 32f;
        public const float MinimumCasterPolicyRowHeight = 92f;
        public const float MinimumCasterPolicyRowWidth = 720f;
        public const string SettingsCloseLabel = "CLOSE";

        public static bool CanRenderLabel(float buttonHeight)
        {
            return buttonHeight - (LabelVerticalPadding * 2) >= ButtonFontSize;
        }

        public static bool CanRenderCasterPolicyRow(float width, float height)
        {
            return width >= MinimumCasterPolicyRowWidth &&
                height >= MinimumCasterPolicyRowHeight;
        }
    }
    public sealed class TargetPortraitViewModel
    {
        internal TargetPortraitViewModel(UnitSnapshot unit, TargetPortraitState state,
            bool explicitlyRequested, bool castAnchor, bool expectedRecipient, bool fulfilled,
            string failureReason)
        {
            UnitId = unit.UnitId;
            Name = string.IsNullOrWhiteSpace(unit.DisplayName) ? "Party member" : unit.DisplayName;
            IsPet = unit.IsPet;
            Wanted = explicitlyRequested;
            Legal = state != TargetPortraitState.InvalidTarget;
            Indirect = state == TargetPortraitState.IndirectlyCovered;
            IsExplicitlyRequested = explicitlyRequested;
            IsCastAnchor = castAnchor;
            IsExpectedRecipient = expectedRecipient;
            IsFulfilled = fulfilled;
            FailureReason = failureReason ?? string.Empty;
            State = state;
            DisplayLabel = state == TargetPortraitState.DirectSelectedAndCovered ? "SELECTED" :
                state == TargetPortraitState.DirectSelectedButUnavailable ? "SELECTED !" :
                state == TargetPortraitState.IndirectlyCovered ? "COVERED" : string.Empty;
            Tooltip = BuildTooltip(state, FailureReason, castAnchor);
            Status = State == TargetPortraitState.InvalidTarget ? PlannerPresentationStatus.Failure :
                State == TargetPortraitState.DirectSelectedAndCovered ||
                State == TargetPortraitState.IndirectlyCovered
                    ? PlannerPresentationStatus.Success :
                State == TargetPortraitState.DirectSelectedButUnavailable
                    ? PlannerPresentationStatus.Warning : PlannerPresentationStatus.Neutral;
        }

        public string UnitId { get; private set; }
        public string Name { get; private set; }
        public bool IsPet { get; private set; }
        public bool Wanted { get; private set; }
        public bool Legal { get; private set; }
        public bool Indirect { get; private set; }
        public bool IsExplicitlyRequested { get; private set; }
        public bool IsCastAnchor { get; private set; }
        public bool IsExpectedRecipient { get; private set; }
        public bool IsFulfilled { get; private set; }
        public string FailureReason { get; private set; }
        public string DisplayLabel { get; private set; }
        public string Tooltip { get; private set; }
        public TargetPortraitState State { get; private set; }
        public PlannerPresentationStatus Status { get; private set; }

        internal static TargetPortraitViewModel Create(SetupSourceRow source,
            PlannerSetupModel model, string routineId, UnitSnapshot unit,
            RoutinePlanResult preview)
        {
            bool wanted = model.IsTargetWanted(routineId, source.SourceId, unit.UnitId);
            bool legal = model.IsTargetLegal(source, routineId, unit.UnitId);
            List<TargetPlanOutcome> outcomes = preview == null ? new List<TargetPlanOutcome>() :
                preview.Plan.Outcomes.Where(item => item.SourceId == source.SourceId &&
                    item.UnitId == unit.UnitId).ToList();
            List<CastStep> steps = preview == null ? new List<CastStep>() : preview.Plan.Steps
                .Where(item => item.SourceId == source.SourceId).ToList();
            bool expected = steps.Any(step => step.ExpectedRecipientUnitIds.Contains(unit.UnitId));
            bool anchor = steps.Any(step => step.AnchorUnitId == unit.UnitId);
            bool fulfilled = outcomes.Any(item => item.Kind == TargetOutcomeKind.Fulfilled ||
                item.Kind == TargetOutcomeKind.SkippedAlreadyActive);
            TargetPlanOutcome failure = outcomes.FirstOrDefault(item =>
                item.Kind == TargetOutcomeKind.Unfulfilled);
            TargetPortraitState state = !legal ? TargetPortraitState.InvalidTarget :
                wanted && fulfilled ? TargetPortraitState.DirectSelectedAndCovered :
                wanted ? TargetPortraitState.DirectSelectedButUnavailable :
                expected ? TargetPortraitState.IndirectlyCovered : TargetPortraitState.Neutral;
            string reason = failure == null ? string.Empty : PlayerFailureReason(failure.Reason);
            if (state == TargetPortraitState.InvalidTarget && string.IsNullOrEmpty(reason))
                reason = InvalidReason(unit);
            if (state == TargetPortraitState.DirectSelectedButUnavailable && string.IsNullOrEmpty(reason))
                reason = BuffCardViewModel.PlayerReason(model.GetSourceUnavailableReason(source));
            return new TargetPortraitViewModel(unit, state, wanted, anchor, expected, fulfilled, reason);
        }

        private static string BuildTooltip(TargetPortraitState state, string reason, bool anchor)
        {
            if (state == TargetPortraitState.DirectSelectedAndCovered)
                return anchor ? "Selected target and cast anchor. Covered by the planned cast." :
                    "Selected target. Covered by the planned cast.";
            if (state == TargetPortraitState.IndirectlyCovered)
                return anchor ? "Cast anchor. Also affected by the planned cast." :
                    "Also affected by the planned cast.";
            if (state == TargetPortraitState.DirectSelectedButUnavailable)
                return string.IsNullOrWhiteSpace(reason) ?
                    "Selected, but not covered by the current plan." : reason;
            if (state == TargetPortraitState.InvalidTarget)
                return string.IsNullOrWhiteSpace(reason) ? "This is not a legal target." : reason;
            return "Valid target. Click to select.";
        }

        private static string PlayerFailureReason(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            if (value.EndsWith(":target-not-in-party", StringComparison.Ordinal))
                return "This target is outside the supported cast plan.";
            if (value.EndsWith(":target-currently-invalid", StringComparison.Ordinal))
                return "This target cannot currently receive the effect.";
            if (value.EndsWith(":no-valid-provider-or-resource", StringComparison.Ordinal) ||
                value.EndsWith(":no-valid-mass-provider-or-resource", StringComparison.Ordinal))
                return "No eligible caster or cast resource is currently available.";
            return "This target is not covered by the current plan.";
        }

        private static string InvalidReason(UnitSnapshot unit)
        {
            if (!unit.TargetValidation.Alive) return "This target is not alive.";
            if (!unit.TargetValidation.Friendly) return "This effect requires a friendly target.";
            if (!unit.TargetValidation.Targetable) return "This target cannot currently be targeted.";
            return "This target is not legal for the selected effect.";
        }
    }

    public sealed class RoutineSummaryViewModel
    {
        internal RoutineSummaryViewModel(string id, string name, int fulfilled, int requested)
        {
            Id = id;
            Name = name;
            Fulfilled = fulfilled;
            Requested = requested;
            int issues = Math.Max(0, requested - fulfilled);
            Label = name + "  " + fulfilled + " ready" +
                (issues == 0 ? string.Empty : "  " + issues + (issues == 1 ? " issue" : " issues"));
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public int Fulfilled { get; private set; }
        public int Requested { get; private set; }
        public string Label { get; private set; }
    }

    public sealed class SelectedBuffPlanSummaryViewModel
    {
        internal SelectedBuffPlanSummaryViewModel(SetupSourceRow source, PlannerSetupModel model,
            string routineId, RoutinePlanResult preview)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (model == null) throw new ArgumentNullException("model");
            if (preview == null) throw new ArgumentNullException("preview");
            SourceId = source.SourceId;
            PlannedCasts = preview.Plan.Steps.Count(item => item.SourceId == source.SourceId);
            var explicitIds = new HashSet<string>(model.Profile.Routines.First(item =>
                item.RoutineId == routineId).Assignments
                .Where(item => item.SourceId == source.SourceId)
                .SelectMany(item => item.WantedTargetUnitIds), StringComparer.Ordinal);
            SelectedTargets = explicitIds.Count;
            AdditionalRecipients = preview.Plan.Steps.Where(item => item.SourceId == source.SourceId)
                .SelectMany(item => item.ExpectedRecipientUnitIds).Distinct(StringComparer.Ordinal)
                .Count(id => !explicitIds.Contains(id));
            Availability = BuildAvailability(source, model);
            Text = "Available: " + Availability + "\nPlanned: " + PlannedCasts +
                (PlannedCasts == 1 ? " cast" : " casts");
            if (SelectedTargets != 0 || AdditionalRecipients != 0)
                Text += "   " + SelectedTargets + (SelectedTargets == 1 ? " selected target" :
                    " selected targets") + (AdditionalRecipients == 0 ? string.Empty : "   " +
                    AdditionalRecipients + (AdditionalRecipients == 1 ?
                        " additional ally covered" : " additional allies covered"));
            Text += BuildEnhancementShortageText(preview, model, routineId);
        }

        // Resolved allocation shortages from the authoritative plan, not
        // source availability: a scarce shared pool that cannot fund every
        // configured enhanced cast is visible on the ordinary card.
        private static string BuildEnhancementShortageText(RoutinePlanResult preview,
            PlannerSetupModel model, string routineId)
        {
            if (preview == null || model == null) return string.Empty;
            var shortages = new List<string>();
            foreach (ResourcePoolAllocation allocation in preview.Plan.ResourceAllocations)
            {
                if (allocation.UnmetDemand <= 0 ||
                    !EnhancementBudgetModel.IsEnhancementPool(allocation.PoolKey)) continue;
                CastEnhancementSnapshot representative = model.Enhancements
                    .FirstOrDefault(value => string.Equals(value.UsagePoolId,
                        EnhancementBudgetModel.PoolUsageId(allocation.PoolKey),
                        StringComparison.Ordinal));
                if (representative == null) continue;
                shortages.Add(representative.DisplayName + ": " + allocation.UnmetDemand +
                    " of " + allocation.RequestedUsage + " requested charges unfunded in " +
                    EnhancementBudgetModel.RoutineLabel(routineId));
            }
            return shortages.Count == 0 ? string.Empty
                : "\nUnmet enhancement demand — " + string.Join("; ", shortages.ToArray());
        }

        public string SourceId { get; private set; }
        public string Availability { get; private set; }
        public int PlannedCasts { get; private set; }
        public int SelectedTargets { get; private set; }
        public int AdditionalRecipients { get; private set; }
        public string Text { get; private set; }

        private static string BuildAvailability(SetupSourceRow source, PlannerSetupModel model)
        {
            var usable = source.Providers.Where(provider =>
                string.IsNullOrEmpty(model.GetProviderRejectionReason(provider))).ToList();
            if (usable.Count == 0) return "0";
            if (usable.Any(provider => model.GetRemainingCasts(provider) == null)) return "At will";
            int casts = BuffCardViewModel.AvailableCastCount(usable, model);
            int casters = usable.Select(provider => provider.Key.CasterUnitId)
                .Distinct(StringComparer.Ordinal).Count();
            bool allPrepared = usable.All(provider =>
                model.GetResourcePool(provider).Kind == ResourcePoolKind.PreparedSlots);
            if (casters > 1) return casts + " casts across " + casters + " casters";
            return casts + (allPrepared ? " prepared" : casts == 1 ? " cast" : " casts");
        }
    }

    public enum PlannerPointerGesture
    {
        Left,
        Right,
        Other
    }

    public sealed class PlannerDescriptionRequest
    {
        private PlannerDescriptionRequest(string sourceId, AbilityKey ability)
        {
            SourceId = sourceId;
            Ability = ability;
        }

        public string SourceId { get; private set; }
        public AbilityKey Ability { get; private set; }

        public static bool TryCreate(
            PlannerPointerGesture gesture,
            string sourceId,
            IEnumerable<SetupSourceRow> sources,
            out PlannerDescriptionRequest request)
        {
            request = null;
            if (gesture != PlannerPointerGesture.Right || string.IsNullOrWhiteSpace(sourceId))
                return false;
            SetupSourceRow source = (sources ?? new SetupSourceRow[0])
                .FirstOrDefault(item => item != null &&
                    string.Equals(item.SourceId, sourceId, StringComparison.Ordinal));
            if (source == null) return false;
            request = new PlannerDescriptionRequest(source.SourceId, source.Ability);
            return true;
        }
    }

    public sealed class PlannerSettingsViewModel
    {
        internal PlannerSettingsViewModel(BuffPlannerProfile profile)
        {
            CastingMode = profile.Execution.Mode == "instant" ? "Instant" : "Animated";
            CombatUse = profile.Execution.OutOfCombatOnly ? "Blocked" : "Allowed";
            Fallback = profile.Execution.AllowAnimatedFallback ? "Allowed" : "Disabled";
            ExistingBuffs = profile.Execution.RecastExisting ? "Recast" : "Skip active";
            Hotkey = string.IsNullOrWhiteSpace(profile.Ui.Hotkey)
                ? PlannerHotkeyText.Default : profile.Ui.Hotkey;
        }

        public string CastingMode { get; private set; }
        public string CombatUse { get; private set; }
        public string Fallback { get; private set; }
        public string ExistingBuffs { get; private set; }
        public string Hotkey { get; private set; }
    }
}
