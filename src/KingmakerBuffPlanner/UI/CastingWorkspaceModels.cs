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
                    case ResolvedCastingReadiness.AlreadySatisfied: return "Already active";
                    case ResolvedCastingReadiness.Ready:
                        return ExecutionLimitation == null ? "Ready" : "Ready - cannot run yet";
                    default: return Readiness.ToString();
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
                    ? "Unresolved caster" : CasterDisplayName;
                string target = DirectTargetUnitId != null
                    ? (DirectTargetDisplayName ?? DirectTargetUnitId)
                    : (string.IsNullOrEmpty(OriginLabel) ? "group" : OriginLabel);
                return caster + " → " + target;
            }
        }

        public string Subtitle
        {
            get { return "Casting " + (Order + 1) + " in " + (RoutineName ?? RoutineId); }
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
        internal WorkspaceBudgetRow(CastingBudgetLine line, string label = null)
        {
            PoolKey = line.PoolKey;
            Label = string.IsNullOrWhiteSpace(label) ? "resource" : label;
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
                    : Label + " (" + ResponsibleCastingIds.Count + " cast" +
                        (ResponsibleCastingIds.Count == 1 ? "" : "s") + ", nothing spent)";
            if (UnmetDemand == 0 && RequestedUsage == 0) return null;
            return Label + ": " + AllocatedUsage + " of " + RequestedUsage + " covered" +
                (UnmetDemand == 0 ? string.Empty : ", short by " + UnmetDemand);
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
                lines.Add(group.Key + " (" + casts + " cast" + (casts == 1 ? string.Empty : "s") +
                    ", nothing spent)");
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
            IEnumerable<SourceKind> sourceKinds = null)
        {
            SourceId = sourceId ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? SourceId : displayName;
            Selected = selected;
            Detail = detail ?? string.Empty;
            IconAbility = iconAbility;
            SourceKinds = (sourceKinds ?? new SourceKind[0]).Distinct()
                .OrderBy(kind => kind).ToList();
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
                    return "choose who receives it to switch back to a single target.";
                case "draft-ability-unresolved":
                    if (detail.StartsWith("no-caster-selected", StringComparison.Ordinal))
                        return "choose who casts it first.";
                    if (detail.StartsWith("no-provider-for-source-and-caster", StringComparison.Ordinal))
                        return "that character cannot cast this buff.";
                    if (detail.StartsWith("exact-source-ambiguous", StringComparison.Ordinal))
                        return "that character can cast it in more than one way; pick one under Cast from.";
                    return "the party could not be read; close and reopen the planner.";
                case "no-editing-focus":
                case "focused-casting-missing":
                    return "select a casting card (Edit) first.";
                case "targeting-mode-unsupported":
                case "target-mode-unsupported":
                    return "this buff cannot be cast that way.";
                case "targeting-requires-anchor":
                    return "choose the group's origin first.";
                case "ready-requires-caster":
                    return "choose who casts it before marking it Ready.";
                case "ready-requires-import-review":
                    return "review what the import changed before marking it Ready.";
                case "state-unchanged":
                    return "it already is.";
                case "casting-unknown":
                case "casting-missing":
                case "casting-null":
                    return "that casting no longer exists.";
                case "routine-unknown":
                    return "that routine no longer exists.";
                case "no-import-review":
                case "no-import-notices":
                    return "there is nothing to review.";
                case "provider-unavailable":
                    return "that character can no longer cast this buff that way.";
                case "provider-unchanged":
                case "recast-policy-unchanged":
                case "routine-unchanged":
                    return "it already is.";
                case "already-first":
                    return "it is already cast first in its routine.";
                case "already-last":
                    return "it is already cast last in its routine.";
                case "provider-not-pinnable":
                    return "that character knows this spell at more than one level in the same spellbook, and this version cannot pick one of them; cast it another way.";
                default:
                    return head.Length == 0 ? "not possible right now." : head.Replace('-', ' ') + ".";
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
                    return "choose who receives this casting first.";
                case "A group casting requires an origin.":
                case "An anchored casting must select an anchor unit.":
                    return "choose the unit the group spell is centred on first.";
                case "A caster-centered casting must use the caster origin.":
                    return "a caster-centred group spell is centred on its caster.";
                case "A group casting has an origin, not a direct target.":
                    return "a group casting has no single recipient; switch to a single target to pick one.";
                case "A direct-target casting cannot carry a group origin.":
                case "A direct-target casting cannot carry required coverage.":
                    return "a single-target casting has no group area; switch to group targeting for that.";
                default:
                    if (message.IndexOf("caster", StringComparison.OrdinalIgnoreCase) >= 0)
                        return "choose who casts it first.";
                    return "this casting is not complete yet.";
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
            return "Running every routine in one pass leaves " + shortCount +
                (shortCount == 1 ? " casting" : " castings") + " of this routine short of a resource.";
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
                string slot = poolKind == ResourcePoolKind.Unlimited ? "free"
                    : poolKind == ResourcePoolKind.PreparedSlots ? "prepared slot"
                    : poolKind == ResourcePoolKind.SpontaneousLevel ? "spell slot" : "uses";
                label = (string.IsNullOrEmpty(casterName) ? string.Empty : casterName + ": ") +
                    bookName + " level " + spellLevel + " (" + slot + ")";
            }
            else
            {
                string resource = string.IsNullOrWhiteSpace(resourceLabel) ? "resource" : resourceLabel;
                label = !string.IsNullOrEmpty(casterName) &&
                    !resource.StartsWith(casterName + ":", StringComparison.Ordinal)
                        ? casterName + ": " + resource : resource;
            }
            if (casterLevel > 0) label += ", caster level " + casterLevel;
            if (metamagicMask != 0) label += ", with metamagic";
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
                    choice.Label += " (option " + index++ + ")";
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
                case "caster-unresolved": return "no caster chosen";
                case "caster-not-in-party": return "the caster is not in the party";
                case "caster-not-capable": return "the caster cannot cast this";
                case "source-unresolvable": return "the buff's source was not found";
                case "exact-source-ambiguous": return "the caster can cast it in more than one way; pick the exact spell or item";
                case "provider-option-unavailable": return "the caster cannot cast it right now";
                case "spellbook-constraint-unsatisfied": return "not in the chosen spellbook";
                case "resource-pool-unknown": return "its resource was not found";
                case "resource-pool-exhausted": return "no casts left";
                case "prepared-slots-exhausted": return "no prepared slot left";
                case "target-not-in-party": return "the recipient is not in the party";
                case "target-not-friendly": return "the recipient is not an ally";
                case "target-not-conscious": return "the recipient is unconscious";
                case "target-not-alive": return "the recipient is dead";
                case "target-not-targetable": return "the recipient cannot be targeted";
                case "target-unreachable": return "the caster cannot target this recipient";
                case "target-mode-mismatch": return "the buff cannot be cast this way";
                case "origin-anchor-illegal": return "the group spell cannot be centred there";
                case "origin-caster-illegal": return "the group spell cannot be centred on its caster; centre it on a party member";
                case "predicted-coverage-empty": return "no party member would be reached";
                case "ability-targeting-unsupported": return "this buff's targeting is not supported";
                case "targeting-modifier-unavailable": return "a required targeting modifier is not available";
                case "enhancements-unvalidated": return "an enhancement could not be checked";
                case "enhancement-incompatible": return "an enhancement does not fit this casting";
                case "enhancement-changes-targeting":
                    return "an enhancement that changes whom the spell reaches (such as Share Transmutation) is not executed yet";
                case "import-review-unresolved": return "imported: needs your review";
                case "already-active": return "already active";
                case "present-effect-not-sufficient": return "the active effect is weaker or about to expire";
                default: return head.Length == 0 ? "not ready" : head.Replace('-', ' ');
            }
        }

        public static string DescribeReviewItem(string item)
        {
            return DescribeReviewItem(item, null);
        }

        public static string DescribeReviewItem(string item, Func<string, string> unitName)
        {
            string value = item ?? string.Empty;
            if (value == "automatic-caster-pending-review")
                return "the old plan let the planner pick any caster; choose one";
            if (value == "provider-pin-without-caster-pending-review")
                return "the old plan named a source but no caster; choose the caster";
            if (value == "group-origin-and-count-pending-review")
                return "the old plan cast this on a group; check where it is centred and who it reaches";
            if (value == "no-recipient:pending-review")
                return "the old plan named no recipient; choose one";
            if (value.StartsWith("grouping-unknown:", StringComparison.Ordinal))
            {
                // Focused re-review: the recipients the old plan named.
                int targetsAt = value.IndexOf("targets=", StringComparison.Ordinal);
                string[] ids = targetsAt < 0 ? new string[0]
                    : value.Substring(targetsAt + 8).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                // Names only: without a way to name them, no ids are shown.
                return "the old plan did not say single target or group" +
                    (ids.Length == 0 || unitName == null ? string.Empty
                        : " (it named " + string.Join(", ", ids.Select(unitName).ToArray()) + ")") + "; choose";
            }
            if (value.StartsWith("provider-pin:", StringComparison.Ordinal))
                return "the old plan used one exact source; check it";
            if (value.StartsWith("enhancement:", StringComparison.Ordinal))
                return value.Contains(":required:")
                    ? "the old plan required an enhancement; check which one"
                    : "the old plan allowed an optional enhancement; check which one";
            int colon = value.IndexOf(':');
            return (colon < 0 ? value : value.Substring(0, colon)).Replace('-', ' ');
        }
    }

    // The workspace header for the selected routine as a whole (live frame
    // casting-ws-qual-20260923-q2-03 read "0 of 0 castings ready · ready to
    // apply": the selected buff's count beside the routine's gate).
    public static class WorkspaceHeaderText
    {
        public static string Describe(string routineName, int castings, int ready,
            bool applyAllowed, int blockingReasons)
        {
            if (castings == 0) return routineName + " · no castings yet";
            return routineName + " · " + ready + " of " + castings + " casting" +
                (castings == 1 ? string.Empty : "s") + " ready · " +
                (applyAllowed ? "ready to apply" : "Apply blocked");
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
                case ResourcePoolKind.Unlimited: what = "free"; break;
                case ResourcePoolKind.PreparedSlots:
                    what = spellLevel == null ? "prepared slot" : "prepared level " + spellLevel + " slot"; break;
                case ResourcePoolKind.SpontaneousLevel:
                    what = spellLevel == null ? "spell slot" : "level " + spellLevel + " spell slot"; break;
                case ResourcePoolKind.AbilityResource:
                    what = string.IsNullOrWhiteSpace(sourceName) ? "ability use" : sourceName + " uses"; break;
                default: what = "item charge"; break;
            }
            return string.IsNullOrWhiteSpace(owner) ? what : owner + ": " + what;
        }
    }

    // Deterministic disambiguation for same-named buff sources: prefer the
    // discovered variant names, then source kind and who can cast it, and
    // only as a last resort an ordinal. Unique names get no detail.
    public static class WorkspaceSourceLabels
    {
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
