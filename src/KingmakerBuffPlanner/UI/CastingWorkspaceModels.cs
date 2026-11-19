using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Domain.Planning;
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

        // Why this version cannot execute the casting as authored (null when
        // it can): shown before Apply; Apply refuses the run with it.
        public string ExecutionLimitation { get; private set; }

        // The live existing-effect verdict per intended recipient.
        public IReadOnlyList<string> ExistingEffectNotes { get; private set; } = new string[0];

        internal void ApplyExecutionDetail(string limitation, IEnumerable<string> existingNotes)
        {
            ExecutionLimitation = string.IsNullOrEmpty(limitation) ? null : limitation;
            ExistingEffectNotes = (existingNotes ?? new string[0]).ToList();
        }

        // This casting's outcome in the last reported run of its routine
        // (null when it took no part in that run): history, not a promise.
        public string LastRunOutcome { get; private set; }

        internal void ApplyLastRun(string outcome)
        {
            LastRunOutcome = string.IsNullOrEmpty(outcome) ? null : outcome;
        }

        // The card status as a player reads it.
        public string StatusLabel
        {
            get
            {
                switch (Readiness)
                {
                    case ResolvedCastingReadiness.AlreadySatisfied: return PlannerText.Get("workspace.card.active");
                    case ResolvedCastingReadiness.Ready:
                        return ExecutionLimitation == null ? PlannerText.Get("workspace.card.ready") : PlannerText.Get("workspace.card.wait");
                    default: return (Readiness == ResolvedCastingReadiness.Blocked ? PlannerText.Get("workspace.card.blocked")
                    : Readiness == ResolvedCastingReadiness.Draft ? PlannerText.Get("workspace.card.draft")
                    : PlannerText.Get("workspace.card.disabled"));
                }
            }
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

        // Card headline: who casts it on whom — the casting as a player
        // reads it, not its internal id.
        public string Headline
        {
            get
            {
                string caster = string.IsNullOrEmpty(CasterDisplayName)
                    ? PlannerText.Get("workspace.card.unresolvedCaster") : CasterDisplayName;
                string target = DirectTargetUnitId != null
                    ? (DirectTargetDisplayName ?? DirectTargetUnitId)
                    : (string.IsNullOrEmpty(OriginLabel) ? PlannerText.Get("workspace.card.group") : OriginLabel);
                return caster + " → " + target;
            }
        }

        public string Subtitle
        {
            get { return PlannerText.Get("workspace.card.subtitle", Order + 1, RoutineName ?? RoutineId); }
        }

        // The routine display name (Long, Important, Short); null until the
        // session sets it.
        public string RoutineName { get; private set; }

        internal void ApplyRoutineName(string name)
        {
            RoutineName = string.IsNullOrWhiteSpace(name) ? null : name;
        }

        // Group castings only: predicted beneficiaries over intended
        // coverage. A direct-target casting has no coverage ratio.
        public string CoverageSummary
        {
            get
            {
                if (DirectTargetUnitId != null) return string.Empty;
                return PlannerText.Get("workspace.card.coverage", PredictedBeneficiaryUnitIds == null ? 0 : PredictedBeneficiaryUnitIds.Count, RequiredCoverageUnitIds == null ? 0 : RequiredCoverageUnitIds.Count);
            }
        }
    }

    // One budget line in the footer drill-down, copied from the shared
    // resolved plan's authoritative lines with the responsible casting IDs.
    public sealed class WorkspaceBudgetRow
    {
        internal WorkspaceBudgetRow(CastingBudgetLine line, string label = null)
        {
            PoolKey = line.PoolKey;
            Label = string.IsNullOrWhiteSpace(label) ? PlannerText.Get("workspace.pool.resource") : label;
            Category = line.Category;
            AvailableNow = line.AvailableNow;
            RequestedUsage = line.RequestedUsage;
            AllocatedUsage = line.AllocatedUsage;
            UnmetDemand = line.UnmetDemand;
            ForecastRemaining = line.ForecastRemaining;
            ResponsibleCastingIds = line.Traces;
            Unlimited = line.Unlimited;
        }

        // Verified Unlimited native pool (e.g. cantrips): shown as
        // "unlimited", never as an unknown or zero balance.
        public bool Unlimited { get; private set; }

        // Player-facing pool name (whose and what kind), never the key.
        public string Label { get; private set; }

        // Footer text for this row, or null when it has nothing to show.
        public string Describe()
        {
            if (Unlimited)
                return ResponsibleCastingIds.Count == 0 ? null
                    : (ResponsibleCastingIds.Count == 1 ? PlannerText.Get("workspace.budget.free.one", Label, ResponsibleCastingIds.Count)
                    : PlannerText.Get("workspace.budget.free.many", Label, ResponsibleCastingIds.Count));
            if (UnmetDemand == 0 && RequestedUsage == 0) return null;
            return (UnmetDemand != 0 ? PlannerText.Get("workspace.budget.short", Label, AllocatedUsage, RequestedUsage, UnmetDemand)
                    : PlannerText.Get("workspace.budget.covered", Label, AllocatedUsage, RequestedUsage));
        }

        internal void ApplyLabel(string label)
        {
            if (!string.IsNullOrWhiteSpace(label)) Label = label;
        }

        // Rows of different pools that would read the same (two spellbooks
        // of one caster at the same level) are numbered in pool order; free
        // rows are merged in the footer instead.
        internal static void DisambiguateLabels(IList<WorkspaceBudgetRow> rows)
        {
            foreach (IGrouping<string, WorkspaceBudgetRow> group in (rows ?? new WorkspaceBudgetRow[0])
                .Where(value => value != null && !value.Unlimited)
                .GroupBy(value => value.Label, StringComparer.Ordinal)
                .Where(value => value.Count() > 1))
            {
                int index = 0;
                foreach (WorkspaceBudgetRow row in group.OrderBy(value => value.PoolKey, StringComparer.Ordinal))
                    if (++index > 1) row.ApplyLabel(row.Label + " (" + index + ")");
            }
        }

        // The footer's lines (review of f7726c9..1332ed8, P3-D): shortages
        // first, so a truncated footer never hides one; then the other
        // finite rows; then free pools merged by label.
        public static IReadOnlyList<string> FooterLines(IEnumerable<WorkspaceBudgetRow> rows)
        {
            List<WorkspaceBudgetRow> list = (rows ?? new WorkspaceBudgetRow[0])
                .Where(value => value != null).ToList();
            var lines = new List<string>();
            foreach (WorkspaceBudgetRow row in list.Where(value => !value.Unlimited && value.UnmetDemand > 0)
                .Concat(list.Where(value => !value.Unlimited && value.UnmetDemand == 0)))
            {
                string text = row.Describe();
                if (text != null) lines.Add(text);
            }
            foreach (IGrouping<string, WorkspaceBudgetRow> group in list
                .Where(value => value.Unlimited && value.ResponsibleCastingIds.Count > 0)
                .GroupBy(value => value.Label, StringComparer.Ordinal))
            {
                int casts = group.Sum(value => value.ResponsibleCastingIds.Count);
                lines.Add((casts == 1 ? PlannerText.Get("workspace.budget.groupFree.one", group.Key, casts)
                    : PlannerText.Get("workspace.budget.groupFree.many", group.Key, casts)));
            }
            return lines;
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
            string detail = null, AbilityKey iconAbility = null,
            IEnumerable<SourceKind> sourceKinds = null,
            string description = "", string durationText = "")
        {
            SourceId = sourceId ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? SourceId : displayName;
            Selected = selected;
            Detail = detail ?? string.Empty;
            IconAbility = iconAbility;
            SourceKinds = (sourceKinds ?? new SourceKind[0]).Distinct()
                .OrderBy(kind => kind).ToList();
            Description = description ?? string.Empty;
            DurationText = durationText ?? string.Empty;
        }

        // How the party can provide this buff (spellbook, ability resource,
        // feature, item), from its discovered providers; drives the grid's
        // Spells / Abilities / Other tabs.
        public IReadOnlyList<SourceKind> SourceKinds { get; private set; }

        // The discovered ability whose native icon represents this buff in
        // the grid; null when discovery supplied none (view shows a glyph).
        public AbilityKey IconAbility { get; private set; }

        public string SourceId { get; private set; }
        public string DisplayName { get; private set; }
        public bool Selected { get; private set; }
        // Base native spell description/duration (the lowest-variant
        // provider's), labelled as base by the inspect when no exact
        // caster/source row is selected.
        public string Description { get; private set; }
        public string DurationText { get; private set; }
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

    // Player-facing text for an authoring refusal: what to do next, never an
    // internal code or casting id (live frame casting-ws-qual-20260923-q2-03
    // showed "draft-invalid:A direct-target casting requires its direct
    // target."). Unmapped codes are shown in words without their ids.
    public static class WorkspaceRefusalText
    {
        public static string Describe(string reason)
        {
            string code = reason ?? string.Empty;
            int colon = code.IndexOf(':');
            string head = colon < 0 ? code : code.Substring(0, colon);
            string detail = colon < 0 ? string.Empty : code.Substring(colon + 1);
            switch (head)
            {
                case "draft-invalid":
                case "targeting-invalid":
                    return ShapeAdvice(detail);
                case "targeting-requires-direct-target":
                    return PlannerText.Get("reason.refusal.single");
                case "draft-ability-unresolved":
                    if (detail.StartsWith("no-caster-selected", StringComparison.Ordinal))
                        return PlannerText.Get("reason.refusal.casterFirst");
                    if (detail.StartsWith("no-provider-for-source-and-caster", StringComparison.Ordinal))
                        return PlannerText.Get("reason.refusal.unusable");
                    if (detail.StartsWith("exact-source-ambiguous", StringComparison.Ordinal))
                        return PlannerText.Get("reason.refusal.exact");
                    return PlannerText.Get("reason.refusal.noParty");
                case "no-editing-focus":
                case "focused-casting-missing":
                    return PlannerText.Get("reason.refusal.selectCard");
                case "targeting-mode-unsupported":
                case "target-mode-unsupported":
                    return PlannerText.Get("reason.refusal.mode");
                case "targeting-requires-anchor":
                    return PlannerText.Get("reason.refusal.origin");
                case "ready-requires-caster":
                    return PlannerText.Get("reason.refusal.readyCaster");
                case "ready-requires-import-review":
                    return PlannerText.Get("reason.refusal.readyReview");
                case "state-unchanged":
                    return PlannerText.Get("reason.refusal.noChange");
                case "casting-unknown":
                case "casting-missing":
                case "casting-null":
                    return PlannerText.Get("reason.refusal.missingCast");
                case "routine-unknown":
                    return PlannerText.Get("reason.refusal.missingRoutine");
                case "no-import-review":
                case "no-import-notices":
                    return PlannerText.Get("reason.refusal.nothingReview");
                case "provider-unavailable":
                    return PlannerText.Get("reason.refusal.unavailableProvider");
                case "provider-unchanged":
                case "recast-policy-unchanged":
                case "routine-unchanged":
                    return PlannerText.Get("reason.refusal.noChange");
                case "already-first":
                    return PlannerText.Get("reason.refusal.first");
                case "already-last":
                    return PlannerText.Get("reason.refusal.last");
                case "provider-not-pinnable":
                    return PlannerText.Get("reason.refusal.levelAmbiguous");
                case "ability-targeting-unsupported": return PlannerText.Get("reason.ability-targeting-unsupported");
                case "caster-not-capable": return PlannerText.Get("reason.caster-not-capable");
                case "enhancement-changes-targeting": return PlannerText.Get("reason.enhancement-changes-targeting");
                case "enhancement-incompatible": return PlannerText.Get("reason.enhancement-incompatible");
                case "origin-anchor-illegal": return PlannerText.Get("reason.origin-anchor-illegal");
                case "origin-caster-illegal": return PlannerText.Get("reason.origin-caster-illegal");
                case "target-not-in-party": return PlannerText.Get("reason.target-not-in-party");
                case "target-unreachable": return PlannerText.Get("reason.target-unreachable");
                case "exact-source-ambiguous": return PlannerText.Get("reason.exact-source-ambiguous");
                case "source-unresolved": return PlannerText.Get("reason.source-unresolvable");
                case "targeting-modifier-unavailable": return WorkspaceReasonText.DescribeNested(code);
                case "enhancement-unavailable": return PlannerText.Get("authoring.refused.enhancement-unavailable");
                case "provider-invalid": return PlannerText.Get("authoring.refused.provider-invalid");
                case "target-already-has-casting": return PlannerText.Get("authoring.refused.target-already-has-casting");
                case "target-unchanged": return PlannerText.Get("authoring.refused.target-unchanged");
                case "recipient-already-has-casting": return PlannerText.Get("authoring.refused.recipient-already-has-casting");
                case "provider-cannot-reach-target": return PlannerText.Get("authoring.refused.provider-cannot-reach-target");
                case "replacement-does-not-match-focus": return PlannerText.Get("authoring.refused.replacement-does-not-match-focus");
                case "casting-id-collision": return PlannerText.Get("authoring.refused.casting-id-collision");
                case "routine-change-requires-move": return PlannerText.Get("authoring.refused.routine-change-requires-move");
                case "no-buff-selected": return PlannerText.Get("authoring.refused.no-buff-selected");
                case "enhancement-not-applicable":
                    switch (detail.Substring(detail.LastIndexOf(':') + 1))
                    {
                        case "caster-mismatch": return PlannerText.Get("graph.enhancement.owner");
                        case "source-not-spellbook": return PlannerText.Get("graph.enhancement.spellOnly");
                        case "spellbook-not-qualified": return PlannerText.Get("graph.enhancement.wrongBook");
                        case "ability-not-qualified": return PlannerText.Get("graph.enhancement.wrongSpell");
                        case "metamagic-already-applied": return PlannerText.Get("graph.enhancement.duplicate");
                        case "spell-level-exceeds-limit": return PlannerText.Get("graph.enhancement.tooHigh");
                        case "category-unsupported": return PlannerText.Get("graph.enhancement.unsupported");
                        default: return PlannerText.Get("authoring.refused.inapplicable");
                    }
                case "enhancement-not-meaningful":
                    switch (detail.Substring(detail.LastIndexOf(':') + 1))
                    {
                        case KingmakerBuffPlanner.Domain.Planning.CastEnhancementRelevance.PiercingReason: return PlannerText.Get("enhancement.relevance.piercing");
                        case KingmakerBuffPlanner.Domain.Planning.CastEnhancementRelevance.PersistentReason: return PlannerText.Get("enhancement.relevance.persistent");
                        case KingmakerBuffPlanner.Domain.Planning.CastEnhancementRelevance.SelectiveReason: return PlannerText.Get("enhancement.relevance.selective");
                        case KingmakerBuffPlanner.Domain.Planning.CastEnhancementRelevance.QuickenReason: return PlannerText.Get("enhancement.relevance.quicken");
                        case KingmakerBuffPlanner.Domain.Planning.CastEnhancementRelevance.ThrenodicReason: return PlannerText.Get("enhancement.relevance.threnodic");
                        case KingmakerBuffPlanner.Domain.Planning.CastEnhancementRelevance.ExtendUnknownReason: return PlannerText.Get("enhancement.relevance.extendUnknown");
                        case KingmakerBuffPlanner.Domain.Planning.CastEnhancementRelevance.ExtendUnsupportedReason: return PlannerText.Get("enhancement.relevance.extendUnsupported");
                        default: return PlannerText.Get("authoring.refused.irrelevant");
                    }
                case "position-out-of-range": return PlannerText.Get("refusal.position-out-of-range");
                case "no-castings": return PlannerText.Get("refusal.no-castings");
                case "duplicate-casting-ids": return PlannerText.Get("refusal.duplicate-casting-ids");
                case "disable-retired": return PlannerText.Get("refusal.disable-retired");
                default:
                    return head.Length == 0 ? PlannerText.Get("reason.refusal.unknown") : PlannerText.Get("refusal.detail", head.Replace('-', ' '));
            }
        }

        // The casting-shape messages are this mod's own (PlannedCasting
        // validation); each gets the advice that fixes it (review of
        // f7726c9..1332ed8, P3-E). The .NET parameter-name line is ignored.
        private static string ShapeAdvice(string detail)
        {
            string message = (detail ?? string.Empty).Split(new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            switch (message.Trim())
            {
                case "A direct-target casting requires its direct target.":
                    return PlannerText.Get("reason.refusal.recipient");
                case "A group casting requires an origin.":
                case "An anchored casting must select an anchor unit.":
                    return PlannerText.Get("reason.refusal.anchor");
                case "A caster-centered casting must use the caster origin.":
                    return PlannerText.Get("reason.refusal.casterOrigin");
                case "A group casting has an origin, not a direct target.":
                    return PlannerText.Get("reason.refusal.groupTarget");
                case "A direct-target casting cannot carry a group origin.":
                case "A direct-target casting cannot carry required coverage.":
                    return PlannerText.Get("reason.refusal.directTarget");
                default:
                    if (message.IndexOf("caster", StringComparison.OrdinalIgnoreCase) >= 0)
                        return PlannerText.Get("reason.refusal.casterFirst");
                    return PlannerText.Get("reason.refusal.incomplete");
            }
        }
    }

    // Final review B7: the whole-plan forecast is shown, not only computed:
    // when every routine run one after another would leave castings blocked
    // (for example two routines relying on the same last charge), the
    // footer says so; nothing is added while the whole plan fits.
    public static class WorkspaceFooterText
    {
        // shortCount: the selected routine's castings that are ready on their
        // own but short of a resource when every routine runs in one pass
        // (re-review: drafts, disabled castings and import notices are not
        // shortages).
        public static string WholePlan(int shortCount)
        {
            if (shortCount <= 0) return string.Empty;
            return PlannerText.Get(shortCount == 1 ? "footer.short.all.one" : "footer.short.all.many", shortCount);
        }

        // Everyday-use v1.2 §2/§5: the footer's standing line. Edits save
        // themselves and the Run click is the authorization - there is no
        // Save, Accept Plan or Review & Apply step to point the player at.
        public static string Readiness(string disposition, string routineName)
        {
            string value = disposition ?? string.Empty;
            if (value == "native-casting-enabled")
                return PlannerText.Get("footer.ready.autosave", routineName);
            if (value == "native-casting-busy")
                return PlannerText.Get("footer.busy.after");
            if (value.StartsWith("native-submission-disabled:runtime-test-session:", StringComparison.Ordinal))
                return PlannerText.Get("footer.disabled.automation");
            return PlannerText.Get("footer.native.unavailable", value);
        }

        // Native UI finish (UI-08): the footer's one line beside Run - what a
        // Run does now, or exactly why it cannot (the count of castings that
        // are not ready and where they are marked).
        public static string RunStatus(string disposition, string routineName, int castings,
            bool allowed, int executable, int notReady, string otherBlock, int shortInOnePass,
            bool castAvailable = false, int omissions = 0)
        {
            string value = disposition ?? string.Empty;
            if (value == "native-casting-busy")
                return PlannerText.Get("footer.busy.stop");
            if (value.StartsWith("native-submission-disabled:runtime-test-session:", StringComparison.Ordinal))
                return "Casting is disabled during this automated session.";
            if (value != "native-casting-enabled")
                return PlannerText.Get("footer.session.unavailable", value);
            if (castings == 0) return PlannerText.Get("footer.noCastings", routineName);
            if (notReady > 0)
                return PlannerText.Get(notReady == 1 ? "footer.status.notReady.one" : "footer.status.notReady.many", routineName, notReady) +
                    (executable > 0 ? PlannerText.Get("footer.status.runOther", executable) : string.Empty);
            if (!allowed)
                return PlannerText.Get("footer.status.blocked", routineName,
                    string.IsNullOrEmpty(otherBlock) ? PlannerText.Get("footer.status.seeMarked") : otherBlock);
            string shortage = shortInOnePass <= 0 ? string.Empty : PlannerText.Get(
                shortInOnePass == 1 ? "footer.status.short.one" : "footer.status.short.many", shortInOnePass);
            if (executable == 0)
                return (castAvailable && omissions != 0
                    ? PlannerText.Get(omissions == 1 ? "footer.status.omittedEmpty.one" : "footer.status.omittedEmpty.many", routineName, omissions)
                    : PlannerText.Get("footer.status.active", routineName)) + shortage;
            string omitted = !castAvailable || omissions == 0 ? string.Empty : PlannerText.Get(
                omissions == 1 ? "footer.status.omits.one" : "footer.status.omits.many", omissions);
            string runKey = castAvailable
                ? (executable == 1 ? "footer.status.available.one" : "footer.status.available.many")
                : (executable == 1 ? "footer.status.casts.one" : "footer.status.casts.many");
            return PlannerText.Get(runKey, routineName, executable) + omitted + shortage;
        }

        // The Run button names the routine it runs: the selected one.
        public static string RunCaption(string routineName)
        {
            return PlannerText.Get("footer.run.title", routineName);
        }

        public static string RunBlocked(string routineName, int notReady)
        {
            return PlannerText.Get(notReady == 1 ? "footer.status.fix.one" : "footer.status.fix.many", routineName, notReady);
        }

        // The mode change is an edit like any other: it saves itself, and
        // the save status beside it says whether that worked.
        public static string ModeChanged(string mode)
        {
            return PlannerText.Get("footer.status.mode", PlannerText.Get(mode == "instant" ? "pad.mode.instant" : "pad.mode.animated"));
        }

        // A plan setting toggled: like every edit it saves itself.
        public static string SettingChanged(string setting, bool on)
        {
            return PlannerText.Get(on ? "footer.setting.on" : "footer.setting.off", setting);
        }

        // v1.2 §4: the passive save status. PlannerText.Get("footer.saved") only while the current
        // revision is durable; anything else is named, never hidden.
        public const string RetrySaveAction = "Retry save";
        public const string ReloadAction = "Reload";

        public static string RecoveryCaption(string action)
        {
            return action == ReloadAction ? PlannerText.Get("footer.recovery.caption.reload")
                : action == RetrySaveAction ? PlannerText.Get("footer.recovery.caption.retry") : string.Empty;
        }

        // The workspace's save indicator after the 0.6.11 refinement: empty
        // while saved or saving (autosave is silent), PlannerText.Get("footer.unsaved") otherwise.
        public static string SaveIndicator(string saveState)
        {
            string value = saveState ?? string.Empty;
            return value == "saved" || value == "saving" ? string.Empty : "Not saved";
        }

        public static string SaveStatus(string saveState)
        {
            string value = saveState ?? string.Empty;
            if (value == "saved") return "Saved";
            if (value == "saving") return PlannerText.Get("footer.saving");
            return "Not saved";
        }

        // Null while saved or saving; otherwise what happened and what the
        // player can do (the footer's recovery action).
        public static string SaveProblem(string saveState)
        {
            string value = saveState ?? string.Empty;
            if (value == "saved" || value == "saving") return null;
            if (value == "migration-pending")
                return PlannerText.Get("footer.upgrade.pending");
            if (value.StartsWith("save-failed:", StringComparison.Ordinal))
                return PlannerText.Get("footer.save.failed", value.Substring("save-failed:".Length));
            if (value == "save-refused:legacy-import")
                return PlannerText.Get("footer.save.import");
            if (value.StartsWith("save-refused", StringComparison.Ordinal))
                return PlannerText.Get("footer.save.protected");
            return PlannerText.Get("footer.save.other", value);
        }

        // The exceptional recovery action for the save state, or null.
        public static string RecoveryAction(string saveState)
        {
            string value = saveState ?? string.Empty;
            if (value == "migration-pending") return RetrySaveAction;
            if (value.StartsWith("save-failed:", StringComparison.Ordinal)) return RetrySaveAction;
            if (value.StartsWith("save-refused", StringComparison.Ordinal)) return ReloadAction;
            return null;
        }
    }

    // Final review B2/B3: one exact way to cast the buff - who casts it,
    // and from which spellbook level, item or ability - as the provider
    // picker offers it for the next casting or for the focused one.
    public sealed class WorkspaceProviderChoice
    {
        internal WorkspaceProviderChoice(string providerKey, string casterUnitId,
            string casterName, string label, bool selected)
        {
            ProviderKey = providerKey ?? string.Empty;
            CasterUnitId = casterUnitId ?? string.Empty;
            CasterName = casterName ?? string.Empty;
            Label = label ?? string.Empty;
            Selected = selected;
        }

        public string ProviderKey { get; private set; }
        public string CasterUnitId { get; private set; }
        public string CasterName { get; private set; }
        public string Label { get; internal set; }
        public bool Selected { get; private set; }
    }

    public static class WorkspaceProviderLabels
    {
        // What the player reads on a provider choice: whose it is and which
        // exact source - a spellbook by name and spell level with the kind of
        // slot it spends (re-review), or an item's or ability's resource -
        // then its caster level and any metamagic.
        public static string Describe(string casterName, string bookName, int spellLevel,
            ResourcePoolKind? poolKind, string resourceLabel, int casterLevel, int metamagicMask)
        {
            string label;
            if (!string.IsNullOrWhiteSpace(bookName))
            {
                string slot = poolKind == ResourcePoolKind.Unlimited ? PlannerText.Get("workspace.pool.free")
                    : poolKind == ResourcePoolKind.PreparedSlots ? PlannerText.Get("workspace.pool.prepared")
                    : poolKind == ResourcePoolKind.SpontaneousLevel ? PlannerText.Get("workspace.pool.spontaneous") : PlannerText.Get("workspace.pool.uses");
                label = (!string.IsNullOrEmpty(casterName) ? PlannerText.Get("workspace.provider.bookOwned", casterName, bookName, spellLevel, slot)
                    : PlannerText.Get("workspace.provider.book", bookName, spellLevel, slot));
            }
            else
            {
                string resource = string.IsNullOrWhiteSpace(resourceLabel) ? PlannerText.Get("workspace.pool.resource") : resourceLabel;
                label = !string.IsNullOrEmpty(casterName) &&
                    !resource.StartsWith(casterName + ":", StringComparison.Ordinal)
                        ? casterName + ": " + resource : resource;
            }
            if (casterLevel > 0) label += PlannerText.Get("workspace.provider.cl", casterLevel);
            if (metamagicMask != 0) label += PlannerText.Get("workspace.provider.metamagic");
            return label;
        }

        // Choices that would read the same get an ordinal, so no two
        // choices on screen look alike.
        public static void Disambiguate(IList<WorkspaceProviderChoice> choices)
        {
            foreach (IGrouping<string, WorkspaceProviderChoice> same in (choices ??
                    new WorkspaceProviderChoice[0])
                .GroupBy(value => value.Label, StringComparer.Ordinal)
                .Where(group => group.Count() > 1).ToList())
            {
                int index = 1;
                foreach (WorkspaceProviderChoice choice in same)
                    choice.Label += PlannerText.Get("workspace.provider.option", index++);
            }
        }
    }

    // Player-facing text for a casting's readiness reasons and its import
    // review items (live frame casting-ws-import-20260923-i1-01 showed
    // "caster-unresolved, import-review-unresolved:automatic-caster-pending-
    // review"). Unit ids and other details after the code are dropped.
    public static class WorkspaceReasonText
    {
        public static string Describe(string code)
        {
            string value = code ?? string.Empty;
            int colon = value.IndexOf(':');
            string head = colon < 0 ? value : value.Substring(0, colon);
            switch (head)
            {
                case "unresolved-saved-request": return PlannerText.Get("reason.unresolved-saved-request");
                case "caster-unresolved": return PlannerText.Get("reason.caster-unresolved");
                case "caster-not-in-party": return PlannerText.Get("reason.caster-not-in-party");
                case "caster-not-capable": return PlannerText.Get("reason.caster-not-capable");
                case ItemSourceResolution.Missing:
                    return PlannerText.Get("reason.item.Missing");
                case ItemSourceResolution.Ambiguous:
                    return PlannerText.Get("reason.item.Ambiguous");
                case ItemSourceResolution.SlotEmpty:
                    return PlannerText.Get("reason.item.SlotEmpty");
                case ItemSourceResolution.LegacySession:
                case ItemSourceResolution.LegacyFact:
                    return PlannerText.Get("reason.item.LegacyFact");
                case ItemSourceResolution.Invalid:
                    return PlannerText.Get("reason.item.Invalid");
                case "source-unresolvable": return PlannerText.Get("reason.source-unresolvable");
                case "exact-source-ambiguous": return PlannerText.Get("reason.exact-source-ambiguous");
                case "provider-option-unavailable": return PlannerText.Get("reason.provider-option-unavailable");
                case "spellbook-constraint-unsatisfied": return PlannerText.Get("reason.spellbook-constraint-unsatisfied");
                case "resource-pool-unknown": return PlannerText.Get("reason.resource-pool-unknown");
                case "resource-pool-exhausted": return PlannerText.Get("reason.resource-pool-exhausted");
                case "prepared-slots-exhausted": return PlannerText.Get("reason.prepared-slots-exhausted");
                case "no-eligible-prepared-token": return PlannerText.Get("reason.no-eligible-prepared-token");
                case "insufficient-shared-resource": return PlannerText.Get("reason.insufficient-shared-resource");
                case "enhancement-exhausted": return PlannerText.Get("reason.enhancement-exhausted");
                case "enhancement-pool-exhausted": return PlannerText.Get("reason.enhancement-pool-exhausted");
                case "enhancement-pool-unknown": return PlannerText.Get("reason.enhancement-pool-unknown");
                case "enhancement-balance-unknown": return PlannerText.Get("reason.enhancement-balance-unknown");
                case "material-unavailable": return PlannerText.Get("reason.material-unavailable");
                case "target-not-in-party": return PlannerText.Get("reason.target-not-in-party");
                case "effect-owner-unresolved": return PlannerText.Get("reason.effect-owner-unresolved");
                case "effect-owner-not-readable": return PlannerText.Get("reason.effect-owner-not-readable");
                case "target-not-friendly": return PlannerText.Get("reason.target-not-friendly");
                case "target-not-conscious": return PlannerText.Get("reason.target-not-conscious");
                case "target-not-alive": return PlannerText.Get("reason.target-not-alive");
                case "target-not-targetable": return PlannerText.Get("reason.target-not-targetable");
                case "target-unreachable": return PlannerText.Get("reason.target-unreachable");
                case "target-mode-mismatch": return PlannerText.Get("reason.target-mode-mismatch");
                case "origin-anchor-illegal": return PlannerText.Get("reason.origin-anchor-illegal");
                case "origin-caster-illegal": return PlannerText.Get("reason.origin-caster-illegal");
                case "predicted-coverage-empty": return PlannerText.Get("reason.predicted-coverage-empty");
                case "ability-targeting-unsupported": return PlannerText.Get("reason.ability-targeting-unsupported");
                case "targeting-modifier-unavailable": return PlannerText.Get("reason.targeting-modifier-unavailable");
                case "share-source-unresolved": return PlannerText.Get("reason.share-source-unresolved");
                case "share-feature-unavailable": return PlannerText.Get("reason.share-feature-unavailable");
                case "share-source-not-supported": return PlannerText.Get("reason.share-source-not-supported");
                case "share-no-legal-recipient": return PlannerText.Get("reason.share-no-legal-recipient");
                case "share-not-needed": return PlannerText.Get("reason.share-not-needed");
                case "share-unverified-caster": return PlannerText.Get("reason.share-unverified-caster");
                case "enhancements-unvalidated": return PlannerText.Get("reason.enhancements-unvalidated");
                case "enhancement-incompatible": return PlannerText.Get("reason.enhancement-incompatible");
                case "enhancement-changes-targeting":
                    // v1.2 §7: Share is a targeting choice made before the
                    // target ("Share with an ally"), never an enhancement.
                    return PlannerText.Get("reason.enhancement-changes-targeting");
                case "import-review-unresolved": return PlannerText.Get("reason.import-review-unresolved");
                case "already-active": return PlannerText.Get("reason.already-active");
                case "present-effect-not-sufficient": return PlannerText.Get("reason.present-effect-not-sufficient");
                case CastingExecutionPolicy.InstantRouteUnavailable:
                    return PlannerText.Get("reason.instantUnavailable", InstantRouteCause(value));
                default: return head.Length == 0 ? PlannerText.Get("reason.notReady") : PlannerText.Get("reason.detail", head.Replace('-', ' '));
            }
        }

        // instant-route-unavailable:<strategy>:<strategy reason>
        private static string InstantRouteCause(string code)
        {
            string[] parts = code.Split(new[] { ':' }, 3);
            string reason = parts.Length == 3 ? parts[2] : string.Empty;
            if (reason.StartsWith("enhancement-native-command-required", StringComparison.Ordinal))
                return PlannerText.Get("reason.instant.enhancement");
            if (reason.StartsWith("sticky-delivery", StringComparison.Ordinal) ||
                reason.StartsWith("provider-direct-sticky-touch", StringComparison.Ordinal))
                return PlannerText.Get("reason.instant.touch");
            if (reason.StartsWith("multiple-direct-cast-providers", StringComparison.Ordinal))
                return PlannerText.Get("reason.instant.combined");
            return PlannerText.Get("reason.instant.animated");
        }

        // A nested refusal ("targeting-modifier-unavailable:<id>:<reason>")
        // described by its inner reason, so a Share toggle row shows the
        // specific honest cause, not the wrapper.
        public static string DescribeNested(string code)
        {
            string value = code ?? string.Empty;
            string[] parts = value.Split(':');
            if (parts.Length >= 3 && parts[0] == "targeting-modifier-unavailable")
                return Describe(string.Join(":", parts, 2, parts.Length - 2));
            return Describe(value);
        }

        public static string DescribeReviewItem(string item)
        {
            return DescribeReviewItem(item, null);
        }

        public static string DescribeReviewItem(string item, Func<string, string> unitName)
        {
            string value = item ?? string.Empty;
            if (value == "automatic-caster-pending-review")
                return PlannerText.Get("reason.import.caster");
            if (value == "provider-pin-without-caster-pending-review")
                return PlannerText.Get("reason.import.source");
            if (value == "group-origin-and-count-pending-review")
                return PlannerText.Get("reason.import.group");
            if (value == "no-recipient:pending-review")
                return PlannerText.Get("reason.import.recipient");
            if (value.StartsWith("grouping-unknown:", StringComparison.Ordinal))
            {
                // Focused re-review: the recipients the old plan named.
                int targetsAt = value.IndexOf("targets=", StringComparison.Ordinal);
                string[] ids = targetsAt < 0 ? new string[0]
                    : value.Substring(targetsAt + 8).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                // Names only: without a way to name them, no ids are shown.
                return (ids.Length > 0 && unitName != null ? PlannerText.Get("reason.import.shapeNamed", string.Join(", ", ids.Select(unitName).ToArray())) : PlannerText.Get("reason.import.shape"));
            }
            if (value.StartsWith("provider-pin:", StringComparison.Ordinal))
                return PlannerText.Get("reason.import.exact");
            if (value.StartsWith("enhancement:", StringComparison.Ordinal))
                return value.Contains(":required:")
                    ? PlannerText.Get("reason.import.required")
                    : PlannerText.Get("reason.import.optional");
            int colon = value.IndexOf(':');
            return (colon < 0 ? value : value.Substring(0, colon)).Replace('-', ' ');
        }
    }

    // The workspace header for the selected routine as a whole (live frame
    // casting-ws-qual-20260923-q2-03 read "0 of 0 castings ready · ready to
    // apply": the selected buff's count beside the routine's gate).
    public static class WorkspaceHeaderText
    {
        // Native UI finish (UI-07/UI-08): the routine's count only; why a
        // run cannot proceed is stated beside the Run action
        // (WorkspaceFooterText.RunStatus), never as a bare "Apply blocked".
        public static string Describe(string routineName, int castings, int ready)
        {
            if (castings == 0) return PlannerText.Get("workspace.header.empty", routineName);
            return (castings == 1 ? PlannerText.Get("workspace.header.ready.one", routineName, ready, castings)
                    : PlannerText.Get("workspace.header.ready.many", routineName, ready, castings));
        }
    }

    // Player-facing resource pool names.
    public static class WorkspacePoolLabels
    {
        public static string Describe(ResourcePoolKind kind, int? spellLevel, string owner,
            string sourceName = null)
        {
            string what;
            switch (kind)
            {
                case ResourcePoolKind.Unlimited: what = PlannerText.Get("workspace.pool.free"); break;
                case ResourcePoolKind.PreparedSlots:
                    what = spellLevel == null ? PlannerText.Get("workspace.pool.prepared") : PlannerText.Get("workspace.preparedLevel", spellLevel); break;
                case ResourcePoolKind.SpontaneousLevel:
                    what = spellLevel == null ? PlannerText.Get("workspace.pool.spontaneous") : PlannerText.Get("workspace.slotLevel", spellLevel); break;
                case ResourcePoolKind.AbilityResource:
                    what = string.IsNullOrWhiteSpace(sourceName) ? PlannerText.Get("workspace.pool.ability") : PlannerText.Get("workspace.abilityUses", sourceName); break;
                default: what = PlannerText.Get("workspace.pool.charge"); break;
            }
            return string.IsNullOrWhiteSpace(owner) ? what : owner + ": " + what;
        }
    }

    // Deterministic disambiguation for same-named buff sources: prefer the
    // discovered variant names, then source kind and who can cast it, and
    // only as a last resort an ordinal. Unique names get no detail.
    public static class WorkspaceSourceLabels
    {
        internal static string KindName(SourceKind kind)
        {
            switch (kind)
            {
                case SourceKind.Spellbook: return PlannerText.Get("workspace.source.kind.spell");
                case SourceKind.AbilityResource: return PlannerText.Get("workspace.source.kind.ability");
                case SourceKind.Item: return PlannerText.Get("workspace.source.kind.item");
                default: return PlannerText.Get("workspace.source.kind.feature");
            }
        }

        // The grid's order: alphabetical by display name, then the
        // disambiguating detail, then the source id (deterministic).
        internal static List<WorkspaceSourceOption> GridOrder(IEnumerable<WorkspaceSourceOption> sources)
        {
            return (sources ?? new WorkspaceSourceOption[0])
                .Where(value => value != null)
                .OrderBy(value => value.DisplayName, StringComparer.InvariantCultureIgnoreCase)
                .ThenBy(value => value.Detail, StringComparer.InvariantCultureIgnoreCase)
                .ThenBy(value => value.SourceId, StringComparer.Ordinal)
                .ToList();
        }

        // The grid's source-type tabs, with the classic catalogue's
        // meaning: Spells = a spellbook provider; Abilities = an ability
        // resource or a feature; Other = anything else (items). A buff
        // with several kinds of provider appears under each of them.
        internal static bool MatchesCategory(WorkspaceSourceOption source,
            PlannerSourceCategory category)
        {
            if (source == null) return false;
            switch (category)
            {
                case PlannerSourceCategory.Spells:
                    return source.SourceKinds.Contains(SourceKind.Spellbook);
                case PlannerSourceCategory.Abilities:
                    return source.SourceKinds.Contains(SourceKind.AbilityResource) ||
                        source.SourceKinds.Contains(SourceKind.Fact);
                case PlannerSourceCategory.Other:
                    return source.SourceKinds.Any(kind => kind != SourceKind.Spellbook &&
                        kind != SourceKind.AbilityResource && kind != SourceKind.Fact);
                default:
                    return true;
            }
        }

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
                        detail = (detail.Length != 0 ? PlannerText.Get("workspace.source.numberDetail", detail, index + 1)
                    : PlannerText.Get("workspace.source.number", index + 1));
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
        // The selected routine as a whole (the header's gate is the
        // routine's own), independent of the selected buff's cards.
        public int RoutineCastingCount { get; internal set; }
        public int RoutineReadyCount { get; internal set; }
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

        // Final review B2/B3: the exact providers for the focused casting's
        // buff (every capable caster, and each source of a caster that has
        // several) and for the next casting's chosen caster.
        public IReadOnlyList<WorkspaceProviderChoice> FocusedProviders
        {
            get { return _focusedProviders; }
        }
        public IReadOnlyList<WorkspaceProviderChoice> DraftProviders
        {
            get { return _draftProviders; }
        }
        internal readonly List<WorkspaceProviderChoice> _focusedProviders =
            new List<WorkspaceProviderChoice>();
        internal readonly List<WorkspaceProviderChoice> _draftProviders =
            new List<WorkspaceProviderChoice>();

        // Re-review: the selected routine's castings that are ready on their
        // own but short of a resource when every routine runs in one pass.
        public int OnePassShortCount { get; internal set; }

        // Header caption for the selected buff: the discovered display name
        // of the selected source; never the raw source key when a name was
        // discovered (the raw "variant|<guid>|<guid>" key reached the header
        // in every live run through casting-ws-claude-rehearsal-*).
        public string SelectedSourceCaption
        {
            get
            {
                if (SelectedSourceId.Length == 0) return PlannerText.Get("workspace.source.none");
                WorkspaceSourceOption match = Draft == null ? null :
                    Draft.Sources.FirstOrDefault(source => source != null &&
                        string.Equals(source.SourceId, SelectedSourceId,
                            StringComparison.Ordinal));
                if (match != null && !string.Equals(match.DisplayName,
                        match.SourceId, StringComparison.Ordinal))
                    return match.Label;
                return PlannerText.Get("workspace.source.unnamed");
            }
        }

        public WorkspaceCastingCard CardById(string castingId)
        {
            return Cards.FirstOrDefault(card =>
                string.Equals(card.CastingId, castingId, StringComparison.Ordinal));
        }
    }
}
