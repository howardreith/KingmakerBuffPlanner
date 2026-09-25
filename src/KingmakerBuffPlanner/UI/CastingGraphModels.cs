using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    // Read models of the casting-graph workspace (addendum v1.1): for the
    // selected buff, casters with their exact sources, one chip and one
    // connection per planned casting, and the targets. Everything here is
    // derived from the canonical document and the compiled plans each
    // refresh; nothing is persisted and the view keeps no ledger of its own.

    public sealed class CastingGraphRoutineTab
    {
        internal CastingGraphRoutineTab(string routineId, string name, int castingCount, bool selected)
        {
            RoutineId = routineId ?? string.Empty;
            Name = string.IsNullOrWhiteSpace(name) ? RoutineId : name;
            CastingCount = castingCount;
            Selected = selected;
        }

        public string RoutineId { get; private set; }
        public string Name { get; private set; }
        public int CastingCount { get; private set; }
        public bool Selected { get; private set; }
    }

    public sealed class CastingGraphCatalogueEntry
    {
        internal CastingGraphCatalogueEntry(WorkspaceSourceOption source, int routineCount,
            int planCount)
        {
            Source = source ?? throw new ArgumentNullException("source");
            RoutineCount = routineCount;
            PlanCount = planCount;
        }

        public WorkspaceSourceOption Source { get; private set; }
        public string SourceId { get { return Source.SourceId; } }
        public string Label { get { return Source.Label; } }
        public bool Selected { get { return Source.Selected; } }
        // Castings of this buff in the selected routine, and in the whole plan.
        public int RoutineCount { get; private set; }
        public int PlanCount { get; private set; }
    }

    // One exact way a caster can cast the selected buff (a spellbook level,
    // an item or an ability resource) and what the WHOLE plan leaves of it.
    public sealed class CastingGraphSourceRow
    {
        internal CastingGraphSourceRow(
            string providerKey, string casterUnitId, string label, string detail,
            string poolKey, string poolLabel, ResourcePoolKind? poolKind,
            CastingCapacityEstimate capacity, string capacityText, bool pinnable,
            string blockedReason, bool selected, bool group)
        {
            ProviderKey = providerKey ?? string.Empty;
            CasterUnitId = casterUnitId ?? string.Empty;
            Label = label ?? string.Empty;
            Detail = detail ?? string.Empty;
            PoolKey = poolKey ?? string.Empty;
            PoolLabel = poolLabel ?? string.Empty;
            PoolKind = poolKind;
            Capacity = capacity;
            CapacityText = capacityText ?? string.Empty;
            Pinnable = pinnable;
            BlockedReason = blockedReason ?? string.Empty;
            Selected = selected;
            IsGroup = group;
        }

        public string ProviderKey { get; private set; }
        public string CasterUnitId { get; private set; }
        // "Bard level 2", "Mutagen", "Wand of Shield".
        public string Label { get; private set; }
        // Full provider description (spellbook, slot kind, caster level).
        public string Detail { get; private set; }
        public string PoolKey { get; private set; }
        // The pool that controls the count ("level 2 spell slot").
        public string PoolLabel { get; private set; }
        public ResourcePoolKind? PoolKind { get; private set; }
        public CastingCapacityEstimate Capacity { get; private set; }
        // "3 of 4 casts remaining", "1 exact slot ready", "Unlimited".
        public string CapacityText { get; private set; }
        // False when the casting could not record which of two identical
        // entries it means (the same spell at two levels of one book).
        public bool Pinnable { get; private set; }
        // Why a new casting from this source cannot be added now (empty when
        // it can): no casts left, material unavailable, not pinnable.
        public string BlockedReason { get; private set; }
        public bool Selected { get; private set; }
        // The ability reaches a group from one origin (one casting, several
        // beneficiaries); otherwise it has one direct target.
        public bool IsGroup { get; private set; }
        // Labels of this caster's other rows drawing on the same pool: their
        // counts are alternatives, never a sum.
        public IReadOnlyList<string> SharedPoolWith { get { return _sharedPoolWith; } }
        internal readonly List<string> _sharedPoolWith = new List<string>();
        public bool Usable { get { return BlockedReason.Length == 0; } }
    }

    public sealed class CastingGraphCasterNode
    {
        internal CastingGraphCasterNode(string unitId, string displayName, bool selected,
            IEnumerable<CastingGraphSourceRow> sources, string note, bool unresolved)
        {
            UnitId = unitId ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? UnitId : displayName;
            Selected = selected;
            Sources = new ReadOnlyCollection<CastingGraphSourceRow>(
                (sources ?? new CastingGraphSourceRow[0]).ToList());
            Note = note ?? string.Empty;
            IsUnresolved = unresolved;
        }

        public string UnitId { get; private set; }
        public string DisplayName { get; private set; }
        public bool Selected { get; private set; }
        public IReadOnlyList<CastingGraphSourceRow> Sources { get; private set; }
        // Readiness/availability summary for the caster as a whole.
        public string Note { get; private set; }
        // The anchor for imported castings that have no caster yet.
        public bool IsUnresolved { get; private set; }
    }

    public enum CastingGraphTargetLegality
    {
        // No source is selected, so legality is not known yet.
        Unknown,
        Legal,
        Illegal
    }

    public sealed class CastingGraphTargetNode
    {
        internal CastingGraphTargetNode(string unitId, string displayName,
            CastingGraphTargetLegality legality, string illegalReason,
            IEnumerable<string> castingIds, WorkspaceRecipientCoverage coverage, string hint)
        {
            UnitId = unitId ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? UnitId : displayName;
            Legality = legality;
            IllegalReason = illegalReason ?? string.Empty;
            CastingIds = new ReadOnlyCollection<string>((castingIds ?? new string[0]).ToList());
            Coverage = coverage;
            Hint = hint ?? string.Empty;
        }

        public string UnitId { get; private set; }
        public string DisplayName { get; private set; }
        public CastingGraphTargetLegality Legality { get; private set; }
        public string IllegalReason { get; private set; }
        // Castings of the selected buff in the selected routine that reach
        // this unit directly or take it as their group origin.
        public IReadOnlyList<string> CastingIds { get; private set; }
        public WorkspaceRecipientCoverage Coverage { get; private set; }
        // What a click does now ("Adds a casting", "Shows casting #2", ...).
        public string Hint { get; private set; }
    }

    public sealed class CastingGraphCasting
    {
        internal CastingGraphCasting(
            string castingId, string routineId, int order, string casterUnitId,
            string sourceProviderKey, CastingTargetMode targetMode, string targetUnitId,
            IEnumerable<string> beneficiaries, IEnumerable<string> requiredCoverage,
            IEnumerable<string> coverageGaps, ResolvedCastingReadiness readiness,
            string statusLabel, string reasonText, IEnumerable<string> enhancementBadges,
            string costText, bool selected, CastingAuthoringState state, bool needsReview,
            bool shortInOnePass, bool redundantInOnePass = false)
        {
            RedundantInOnePass = redundantInOnePass;
            CastingId = castingId ?? string.Empty;
            RoutineId = routineId ?? string.Empty;
            Order = order;
            CasterUnitId = casterUnitId;
            SourceProviderKey = sourceProviderKey;
            TargetMode = targetMode;
            TargetUnitId = targetUnitId;
            Beneficiaries = new ReadOnlyCollection<string>((beneficiaries ?? new string[0]).ToList());
            RequiredCoverage = new ReadOnlyCollection<string>((requiredCoverage ?? new string[0]).ToList());
            CoverageGaps = new ReadOnlyCollection<string>((coverageGaps ?? new string[0]).ToList());
            Readiness = readiness;
            StatusLabel = statusLabel ?? string.Empty;
            ReasonText = reasonText ?? string.Empty;
            EnhancementBadges = new ReadOnlyCollection<string>(
                (enhancementBadges ?? new string[0]).ToList());
            CostText = costText ?? string.Empty;
            Selected = selected;
            State = state;
            NeedsReview = needsReview;
            ShortInOnePass = shortInOnePass;
        }

        public string CastingId { get; private set; }
        public string RoutineId { get; private set; }
        // Position in its routine (0-based); the chip shows Order + 1.
        public int Order { get; private set; }
        public string OrderLabel { get { return "#" + (Order + 1); } }
        // Null for an imported casting with no caster yet.
        public string CasterUnitId { get; private set; }
        // The exact source row the connection starts from; null when the
        // casting's source did not resolve (the line starts at the caster).
        public string SourceProviderKey { get; private set; }
        public CastingTargetMode TargetMode { get; private set; }
        // The direct target, or the group's origin unit (the caster itself
        // for a caster-centred casting).
        public string TargetUnitId { get; private set; }
        // Group only: predicted beneficiaries (derived branches, not castings).
        public IReadOnlyList<string> Beneficiaries { get; private set; }
        public IReadOnlyList<string> RequiredCoverage { get; private set; }
        public IReadOnlyList<string> CoverageGaps { get; private set; }
        public ResolvedCastingReadiness Readiness { get; private set; }
        public string StatusLabel { get; private set; }
        public string ReasonText { get; private set; }
        public IReadOnlyList<string> EnhancementBadges { get; private set; }
        public string CostText { get; private set; }
        public bool Selected { get; private set; }
        public CastingAuthoringState State { get; private set; }
        public bool NeedsReview { get; private set; }
        // Ready in its routine alone, but short of a resource when every
        // routine runs in one pass.
        public bool ShortInOnePass { get; private set; }
        // Ready on its own, but an earlier identical casting in the one-pass
        // sequence already gives its recipients the effect, so with "skip if
        // active" it would be skipped and spend nothing.
        public bool RedundantInOnePass { get; private set; }
        public bool IsGroup { get { return TargetMode != CastingTargetMode.DirectTarget; } }
        // Castings sharing caster, source and target with this one (parallel
        // castings stay separate chips; the index keeps their lines apart).
        public int ParallelIndex { get; internal set; }
        public int ParallelCount { get; internal set; }
    }

    public sealed class CastingGraphEnhancementOption
    {
        internal CastingGraphEnhancementOption(
            string enhancementId, string name, string mechanism, string sourceName,
            bool selected, bool required, string unavailableReason, string costText,
            string poolLabel, int? poolRemaining, int? poolAvailable, string budgetText,
            string expectedEffect)
        {
            EnhancementId = enhancementId ?? string.Empty;
            Name = string.IsNullOrWhiteSpace(name) ? EnhancementId : name;
            Mechanism = mechanism ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            Selected = selected;
            Required = required;
            UnavailableReason = unavailableReason ?? string.Empty;
            CostText = costText ?? string.Empty;
            PoolLabel = poolLabel ?? string.Empty;
            PoolRemaining = poolRemaining;
            PoolAvailable = poolAvailable;
            BudgetText = budgetText ?? string.Empty;
            ExpectedEffect = expectedEffect ?? string.Empty;
        }

        public string EnhancementId { get; private set; }
        public string Name { get; private set; }
        // "Metamagic rod" or "Class feature".
        public string Mechanism { get; private set; }
        // The item or feature that provides it.
        public string SourceName { get; private set; }
        public bool Selected { get; private set; }
        // Required on new castings; false only for imported optional intent.
        public bool Required { get; private set; }
        // Why it cannot be added to this casting (empty when it can).
        public string UnavailableReason { get; private set; }
        public string CostText { get; private set; }
        public string PoolLabel { get; private set; }
        // Uses left in its pool after the whole plan (null: unknown).
        public int? PoolRemaining { get; private set; }
        public int? PoolAvailable { get; private set; }
        public string BudgetText { get; private set; }
        public string ExpectedEffect { get; private set; }
        public bool CanAdd { get { return !Selected && UnavailableReason.Length == 0; } }
    }

    public sealed class CastingGraphInspector
    {
        internal CastingGraphInspector(PlannedCasting casting, CastingGraphCasting chip,
            string title, string headline, string casterName, string sourceLabel,
            string targetLabel, string routineName, int routineCount,
            IEnumerable<string> reasons, IEnumerable<string> reviewItems,
            IEnumerable<string> costLines, IEnumerable<CastingGraphEnhancementOption> enhancements,
            IEnumerable<WorkspaceProviderChoice> providers,
            IEnumerable<CastingGraphTargetNode> retargets, string coverageText,
            string limitation, IEnumerable<string> existingEffectNotes, string lastRun)
        {
            Casting = casting ?? throw new ArgumentNullException("casting");
            Chip = chip;
            Title = title ?? string.Empty;
            Headline = headline ?? string.Empty;
            CasterName = casterName ?? string.Empty;
            SourceLabel = sourceLabel ?? string.Empty;
            TargetLabel = targetLabel ?? string.Empty;
            RoutineName = routineName ?? string.Empty;
            RoutineCount = routineCount;
            Reasons = new ReadOnlyCollection<string>((reasons ?? new string[0]).ToList());
            ReviewItems = new ReadOnlyCollection<string>((reviewItems ?? new string[0]).ToList());
            CostLines = new ReadOnlyCollection<string>((costLines ?? new string[0]).ToList());
            Enhancements = new ReadOnlyCollection<CastingGraphEnhancementOption>(
                (enhancements ?? new CastingGraphEnhancementOption[0]).ToList());
            Providers = new ReadOnlyCollection<WorkspaceProviderChoice>(
                (providers ?? new WorkspaceProviderChoice[0]).ToList());
            Retargets = new ReadOnlyCollection<CastingGraphTargetNode>(
                (retargets ?? new CastingGraphTargetNode[0]).ToList());
            CoverageText = coverageText ?? string.Empty;
            ExecutionLimitation = limitation ?? string.Empty;
            ExistingEffectNotes = new ReadOnlyCollection<string>(
                (existingEffectNotes ?? new string[0]).ToList());
            LastRun = lastRun ?? string.Empty;
        }

        public PlannedCasting Casting { get; private set; }
        public string CastingId { get { return Casting.CastingId; } }
        public CastingGraphCasting Chip { get; private set; }
        public string Title { get; private set; }
        public string Headline { get; private set; }
        public string CasterName { get; private set; }
        public string SourceLabel { get; private set; }
        public string TargetLabel { get; private set; }
        public string RoutineName { get; private set; }
        public int RoutineCount { get; private set; }
        public bool CanMoveEarlier { get { return Casting.Order > 0; } }
        public bool CanMoveLater { get { return Casting.Order < RoutineCount - 1; } }
        public IReadOnlyList<string> Reasons { get; private set; }
        public IReadOnlyList<string> ReviewItems { get; private set; }
        public IReadOnlyList<string> CostLines { get; private set; }
        public IReadOnlyList<CastingGraphEnhancementOption> Enhancements { get; private set; }
        // Every exact provider of this buff (to change caster or source).
        public IReadOnlyList<WorkspaceProviderChoice> Providers { get; private set; }
        // Units this casting may be moved to (legal for its own source).
        public IReadOnlyList<CastingGraphTargetNode> Retargets { get; private set; }
        public string CoverageText { get; private set; }
        public string ExecutionLimitation { get; private set; }
        public IReadOnlyList<string> ExistingEffectNotes { get; private set; }
        public string LastRun { get; private set; }
        public bool RecastsExisting
        {
            get { return Casting.ExistingEffectPolicy == ExistingEffectPolicy.Overwrite; }
        }
    }

    // The whole read model the Unity graph view renders after a refresh.
    public sealed class CastingGraphView
    {
        internal CastingGraphView()
        {
        }

        public string SelectedSourceId { get; internal set; }
        public string SelectedSourceCaption { get; internal set; }
        public AbilityKey SelectedSourceIcon { get; internal set; }
        // True for a group buff, false for single target, null unknown.
        public bool? SelectedSourceIsGroup { get; internal set; }
        public string SelectedRoutineId { get; internal set; }
        public string SelectedRoutineName { get; internal set; }
        public IReadOnlyList<CastingGraphRoutineTab> Routines { get; internal set; }
        public IReadOnlyList<CastingGraphCatalogueEntry> Catalogue { get; internal set; }
        public IReadOnlyList<CastingGraphCasterNode> Casters { get; internal set; }
        public string SelectedCasterUnitId { get; internal set; }
        public string SelectedProviderKey { get; internal set; }
        public IReadOnlyList<CastingGraphTargetNode> Targets { get; internal set; }
        public IReadOnlyList<CastingGraphCasting> Castings { get; internal set; }
        // Castings of this buff in OTHER routines (not drawn; counted).
        public int OtherRoutineCastings { get; internal set; }
        public string FocusedCastingId { get; internal set; }
        public CastingGraphInspector Inspector { get; internal set; }
        // The next step, in words.
        public string Guidance { get; internal set; }
        public string SelectedRunLabel { get; internal set; }
        public IReadOnlyList<string> SelectedRunBudget { get; internal set; }
        public string OnePassLabel { get; internal set; }
        public IReadOnlyList<string> OnePassBudget { get; internal set; }
        public int OnePassShortCount { get; internal set; }
        public CastingApplyDecision SelectedRoutineGate { get; internal set; }
        public CastingReviewStatus ReviewStatus { get; internal set; }
        public int RoutineCastingCount { get; internal set; }
        public int RoutineReadyCount { get; internal set; }

        public CastingGraphCasting CastingById(string castingId)
        {
            return (Castings ?? new CastingGraphCasting[0]).FirstOrDefault(value =>
                string.Equals(value.CastingId, castingId, StringComparison.Ordinal));
        }

        public CastingGraphTargetNode TargetById(string unitId)
        {
            return (Targets ?? new CastingGraphTargetNode[0]).FirstOrDefault(value =>
                string.Equals(value.UnitId, unitId, StringComparison.Ordinal));
        }

        public CastingGraphCasterNode CasterById(string unitId)
        {
            return (Casters ?? new CastingGraphCasterNode[0]).FirstOrDefault(value =>
                string.Equals(value.UnitId, unitId ?? string.Empty, StringComparison.Ordinal));
        }
    }

    // The result of a graph authoring gesture: the edit itself, the casting
    // it concerns, and whether an existing casting was shown instead of a
    // new one being created (an assigned target is never silently stolen).
    public sealed class CastingGraphEditResult
    {
        internal CastingGraphEditResult(AuthoringEditResult edit, string castingId,
            bool showedExisting)
        {
            Edit = edit;
            CastingId = castingId;
            ShowedExisting = showedExisting;
        }

        public AuthoringEditResult Edit { get; private set; }
        public string CastingId { get; private set; }
        public bool ShowedExisting { get; private set; }
        public bool Applied { get { return Edit != null && Edit.Applied; } }
    }

    // Player-facing words for the graph (pure; tested).
    public static class CastingGraphText
    {
        // What a source can still fund after the whole plan, as the caster
        // lane shows it.
        public static string Capacity(CastingCapacityEstimate estimate, ResourcePoolKind? kind,
            int unitsPerCast)
        {
            if (estimate == null) return "Remaining unknown";
            if (estimate.Kind == CastingCapacityKind.Unlimited) return "Unlimited";
            if (estimate.Kind == CastingCapacityKind.Unknown) return "Remaining unknown";
            int count = estimate.AdditionalCastings;
            string more = estimate.IsLowerBound ? "+" : string.Empty;
            if (count == 0)
            {
                if (estimate.LimitingCategory == CastingCostCategory.Material)
                    return "Blocked: material unavailable";
                if (kind == ResourcePoolKind.PreparedSlots) return "No prepared slot left";
                int? total = Casts(estimate.NativeAvailableNow, unitsPerCast);
                return "0 / " + (total == null ? "?" : total.Value.ToString()) + " remaining";
            }
            string limited = estimate.LimitingCategory == CastingCostCategory.Material
                ? " (material limits it)" : string.Empty;
            if (kind == ResourcePoolKind.PreparedSlots)
                return count + more + (count == 1 ? " exact slot ready" : " exact slots ready") + limited;
            int? all = Casts(estimate.NativeAvailableNow, unitsPerCast);
            bool showAll = all != null && !estimate.IsLowerBound && all.Value >= count;
            int noun = showAll ? all.Value : count;
            return count + more + (showAll ? " of " + all.Value : string.Empty) +
                (noun == 1 ? " cast" : " casts") + " remaining" + limited;
        }

        private static int? Casts(int? units, int unitsPerCast)
        {
            if (units == null) return null;
            return unitsPerCast <= 1 ? units : units / unitsPerCast;
        }

        // Why a new casting cannot come from this source now; empty when it
        // can. The plan's own capacity decides, never a view count.
        public static string BlockedReason(CastingCapacityEstimate estimate, bool pinnable)
        {
            if (!pinnable)
                return "This spell is known at two levels of one spellbook; this version cannot pick one.";
            if (estimate == null || estimate.Kind == CastingCapacityKind.Unknown)
                return string.Empty;
            if (estimate.CanFundAnother) return string.Empty;
            if (estimate.LimitingCategory == CastingCostCategory.Material)
                return "Material component unavailable.";
            return "No casts left after the rest of the plan.";
        }

        // What an enhancement is expected to change, from its verified kind
        // only (unknown effects are said to be unknown).
        public static string ExpectedEffect(CastEnhancementSnapshot enhancement)
        {
            if (enhancement == null) return string.Empty;
            if (enhancement.AffectsTargeting)
                return "Changes whom the spell reaches (not executed in this version).";
            if (enhancement.Category == CastEnhancementCategory.MetamagicRod)
            {
                var parts = new List<string>();
                int mask = enhancement.MetamagicMask;
                if ((mask & ExistingEffectSufficiency.ExtendMetamagicFlag) != 0)
                    parts.Add("duration x2");
                if ((mask & 1) != 0) parts.Add("variable numbers +50%");
                if ((mask & 2) != 0) parts.Add("variable numbers maximized");
                if ((mask & 16) != 0) parts.Add("spell level raised");
                return parts.Count == 0
                    ? enhancement.EffectDisplayName + " metamagic (effect not calculated)."
                    : char.ToUpperInvariant(parts[0][0]) + string.Join(", ", parts.ToArray()).Substring(1) + ".";
            }
            if (!string.IsNullOrWhiteSpace(enhancement.Description))
            {
                string description = enhancement.Description.Trim();
                return description.Length <= 160 ? description : description.Substring(0, 157) + "...";
            }
            return enhancement.EffectDisplayName + " (effect not calculated).";
        }

        public static string Mechanism(CastEnhancementSnapshot enhancement)
        {
            if (enhancement == null) return string.Empty;
            return enhancement.Category == CastEnhancementCategory.MetamagicRod
                ? "Metamagic rod" : "Class feature";
        }

        // An enhancement's own applicability refusal, in words.
        public static string Applicability(string failure)
        {
            switch (failure ?? string.Empty)
            {
                case "": return string.Empty;
                case "caster-mismatch": return "Belongs to another character.";
                case "source-not-spellbook": return "Only spells from a spellbook can use it.";
                case "spellbook-not-qualified": return "Not for this spellbook.";
                case "ability-not-qualified": return "Not for this spell.";
                case "metamagic-already-applied": return "This spell already has that metamagic.";
                case "spell-level-exceeds-limit": return "The spell's level is above this rod's limit.";
                case "category-unsupported": return "Not supported in this version.";
                default: return failure.Replace('-', ' ') + ".";
            }
        }

        public static string Guidance(bool buffSelected, string casterName, int casterSources,
            string sourceLabel, bool group, int castingsShown)
        {
            if (!buffSelected) return "Choose a buff on the left.";
            if (string.IsNullOrEmpty(casterName))
                return castingsShown == 0
                    ? "Choose who casts it: click a caster, then who receives it."
                    : "Click a casting's line or card to edit it, or choose a caster to add another.";
            if (string.IsNullOrEmpty(sourceLabel))
                return casterName + " can cast it " + casterSources +
                    " ways: choose the exact source under " + casterName + ".";
            return group
                ? casterName + " · " + sourceLabel + ": click the unit the group spell is centred on. One casting reaches everyone in range."
                : casterName + " · " + sourceLabel + ": click who receives it. Each click adds one casting.";
        }
    }

    // Pure geometry of the graph (graph-local pixels: origin at the content's
    // top-left, x to the right, y DOWN). The Unity view only places objects
    // at these positions; layout decisions are tested here.
    public struct GraphPoint
    {
        public GraphPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X;
        public float Y;
    }

    public struct GraphSegment
    {
        public GraphSegment(GraphPoint from, GraphPoint to)
        {
            From = from;
            To = to;
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            Length = (float)Math.Sqrt(dx * dx + dy * dy);
            // Angle in the y-down graph frame; the view negates it for Unity's
            // y-up frame.
            AngleDegrees = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
            Center = new GraphPoint((from.X + to.X) / 2f, (from.Y + to.Y) / 2f);
        }

        public GraphPoint From;
        public GraphPoint To;
        public float Length;
        public float AngleDegrees;
        public GraphPoint Center;

        // Distance from a point to this segment (hit-corridor contract).
        public float DistanceTo(GraphPoint point)
        {
            float dx = To.X - From.X;
            float dy = To.Y - From.Y;
            float lengthSquared = dx * dx + dy * dy;
            float t = lengthSquared <= 0f ? 0f
                : ((point.X - From.X) * dx + (point.Y - From.Y) * dy) / lengthSquared;
            t = Math.Max(0f, Math.Min(1f, t));
            float px = From.X + t * dx - point.X;
            float py = From.Y + t * dy - point.Y;
            return (float)Math.Sqrt(px * px + py * py);
        }
    }

    public sealed class GraphLayoutMetrics
    {
        public float CasterLaneWidth = 330f;
        public float ChipLaneWidth = 250f;
        public float TargetLaneWidth = 230f;
        public float CasterHeaderHeight = 58f;
        public float SourceRowHeight = 46f;
        public float CasterGap = 12f;
        public float TargetNodeHeight = 92f;
        public float TargetGap = 8f;
        public float ChipWidth = 176f;
        public float ChipHeight = 48f;
        public float ChipGap = 8f;
        public float TopPadding = 8f;
        public float BottomPadding = 12f;
        // Parallel castings' anchors are spread by this much per index.
        public float ParallelSpread = 6f;

        public float ChipLaneLeft { get { return CasterLaneWidth; } }
        public float TargetLaneLeft { get { return CasterLaneWidth + ChipLaneWidth; } }
        public float Width { get { return CasterLaneWidth + ChipLaneWidth + TargetLaneWidth; } }
    }

    public sealed class GraphChipPlacement
    {
        internal GraphChipPlacement(string castingId, float top, GraphPoint left, GraphPoint right)
        {
            CastingId = castingId;
            Top = top;
            Left = left;
            Right = right;
        }

        public string CastingId { get; private set; }
        public float Top { get; private set; }
        public GraphPoint Left { get; private set; }
        public GraphPoint Right { get; private set; }
    }

    public sealed class GraphConnection
    {
        internal GraphConnection(string castingId, GraphSegment inbound, GraphSegment outbound,
            IEnumerable<KeyValuePair<string, GraphSegment>> branches)
        {
            CastingId = castingId;
            Inbound = inbound;
            Outbound = outbound;
            Branches = new ReadOnlyCollection<KeyValuePair<string, GraphSegment>>(
                (branches ?? new KeyValuePair<string, GraphSegment>[0]).ToList());
        }

        public string CastingId { get; private set; }
        // Caster/source anchor -> chip.
        public GraphSegment Inbound { get; private set; }
        // Chip -> direct target or group origin.
        public GraphSegment Outbound { get; private set; }
        // Group only: chip -> each other predicted beneficiary (dashed,
        // derived; never a casting, never a cost).
        public IReadOnlyList<KeyValuePair<string, GraphSegment>> Branches { get; private set; }
    }

    public sealed class GraphLayoutResult
    {
        internal GraphLayoutResult()
        {
        }

        public float Height { get; internal set; }
        public IReadOnlyDictionary<string, float> CasterTops { get; internal set; }
        // Keyed by caster unit id + "|" + provider key.
        public IReadOnlyDictionary<string, float> SourceRowTops { get; internal set; }
        public IReadOnlyDictionary<string, float> TargetTops { get; internal set; }
        public IReadOnlyList<GraphChipPlacement> Chips { get; internal set; }
        public IReadOnlyList<GraphConnection> Connections { get; internal set; }

        public GraphChipPlacement ChipFor(string castingId)
        {
            return Chips.FirstOrDefault(value => string.Equals(value.CastingId, castingId,
                StringComparison.Ordinal));
        }

        public GraphConnection ConnectionFor(string castingId)
        {
            return Connections.FirstOrDefault(value => string.Equals(value.CastingId, castingId,
                StringComparison.Ordinal));
        }
    }

    public static class CastingGraphLayout
    {
        public static string SourceRowKey(string casterUnitId, string providerKey)
        {
            return (casterUnitId ?? string.Empty) + "|" + (providerKey ?? string.Empty);
        }

        // Places caster nodes (with their source rows) and target nodes in
        // fixed lanes, then one chip per casting in the middle lane as close
        // as possible to halfway between its anchors without overlapping any
        // other chip (a deterministic sweep in ideal-position order).
        public static GraphLayoutResult Compute(CastingGraphView view, GraphLayoutMetrics metrics)
        {
            if (view == null) throw new ArgumentNullException("view");
            GraphLayoutMetrics m = metrics ?? new GraphLayoutMetrics();
            var casterTops = new Dictionary<string, float>(StringComparer.Ordinal);
            var sourceTops = new Dictionary<string, float>(StringComparer.Ordinal);
            var targetTops = new Dictionary<string, float>(StringComparer.Ordinal);
            float y = m.TopPadding;
            foreach (CastingGraphCasterNode caster in view.Casters ?? new CastingGraphCasterNode[0])
            {
                casterTops[caster.UnitId] = y;
                y += m.CasterHeaderHeight;
                foreach (CastingGraphSourceRow row in caster.Sources)
                {
                    sourceTops[SourceRowKey(caster.UnitId, row.ProviderKey)] = y;
                    y += m.SourceRowHeight;
                }
                y += m.CasterGap;
            }
            float casterBottom = y;
            y = m.TopPadding;
            foreach (CastingGraphTargetNode target in view.Targets ?? new CastingGraphTargetNode[0])
            {
                targetTops[target.UnitId] = y;
                y += m.TargetNodeHeight + m.TargetGap;
            }
            float targetBottom = y;
            var anchors = new List<KeyValuePair<CastingGraphCasting, GraphPoint[]>>();
            foreach (CastingGraphCasting casting in view.Castings ?? new CastingGraphCasting[0])
            {
                GraphPoint from = SourceAnchor(casting, casterTops, sourceTops, m);
                GraphPoint to = TargetAnchor(casting.TargetUnitId, targetTops, m);
                float spread = (casting.ParallelIndex - (casting.ParallelCount - 1) / 2f) * m.ParallelSpread;
                from = new GraphPoint(from.X, from.Y + spread);
                to = new GraphPoint(to.X, to.Y + spread);
                anchors.Add(new KeyValuePair<CastingGraphCasting, GraphPoint[]>(casting,
                    new[] { from, to }));
            }
            // Ideal chip centre: halfway between the anchors; ties keep the
            // routine order so the layout is deterministic.
            var ordered = anchors
                .Select(pair => new
                {
                    Casting = pair.Key,
                    From = pair.Value[0],
                    To = pair.Value[1],
                    Ideal = (pair.Value[0].Y + pair.Value[1].Y) / 2f - m.ChipHeight / 2f
                })
                .OrderBy(value => value.Ideal)
                .ThenBy(value => value.Casting.Order)
                .ThenBy(value => value.Casting.CastingId, StringComparer.Ordinal)
                .ToList();
            var chips = new List<GraphChipPlacement>();
            var connections = new List<GraphConnection>();
            float next = m.TopPadding;
            float chipLeftX = m.ChipLaneLeft + (m.ChipLaneWidth - m.ChipWidth) / 2f;
            float chipRightX = chipLeftX + m.ChipWidth;
            foreach (var item in ordered)
            {
                float top = Math.Max(item.Ideal, next);
                next = top + m.ChipHeight + m.ChipGap;
                var left = new GraphPoint(chipLeftX, top + m.ChipHeight / 2f);
                var right = new GraphPoint(chipRightX, top + m.ChipHeight / 2f);
                chips.Add(new GraphChipPlacement(item.Casting.CastingId, top, left, right));
                var branches = new List<KeyValuePair<string, GraphSegment>>();
                if (item.Casting.IsGroup)
                    foreach (string unitId in item.Casting.Beneficiaries)
                    {
                        if (string.Equals(unitId, item.Casting.TargetUnitId, StringComparison.Ordinal) ||
                            !targetTops.ContainsKey(unitId)) continue;
                        branches.Add(new KeyValuePair<string, GraphSegment>(unitId,
                            new GraphSegment(right, TargetAnchor(unitId, targetTops, m))));
                    }
                connections.Add(new GraphConnection(item.Casting.CastingId,
                    new GraphSegment(item.From, left), new GraphSegment(right, item.To), branches));
            }
            // Chips keep the routine order on screen reading: the list is
            // returned in routine order, geometry in its own order.
            chips = chips.OrderBy(chip => (view.CastingById(chip.CastingId) ?? Dummy).Order).ToList();
            float chipBottom = next;
            return new GraphLayoutResult
            {
                Height = Math.Max(Math.Max(casterBottom, targetBottom), chipBottom) + m.BottomPadding,
                CasterTops = casterTops,
                SourceRowTops = sourceTops,
                TargetTops = targetTops,
                Chips = chips,
                Connections = connections
            };
        }

        private static readonly CastingGraphCasting Dummy = new CastingGraphCasting(
            string.Empty, string.Empty, int.MaxValue, null, null, CastingTargetMode.DirectTarget,
            null, null, null, null, ResolvedCastingReadiness.Draft, null, null, null, null, false,
            CastingAuthoringState.Draft, false, false);

        private static GraphPoint SourceAnchor(CastingGraphCasting casting,
            Dictionary<string, float> casterTops, Dictionary<string, float> sourceTops,
            GraphLayoutMetrics m)
        {
            string caster = casting.CasterUnitId ?? string.Empty;
            float top;
            if (casting.SourceProviderKey != null &&
                sourceTops.TryGetValue(SourceRowKey(caster, casting.SourceProviderKey), out top))
                return new GraphPoint(m.CasterLaneWidth, top + m.SourceRowHeight / 2f);
            if (casterTops.TryGetValue(caster, out top))
                return new GraphPoint(m.CasterLaneWidth, top + m.CasterHeaderHeight / 2f);
            return new GraphPoint(m.CasterLaneWidth, m.TopPadding);
        }

        private static GraphPoint TargetAnchor(string unitId, Dictionary<string, float> targetTops,
            GraphLayoutMetrics m)
        {
            float top;
            if (unitId != null && targetTops.TryGetValue(unitId, out top))
                return new GraphPoint(m.TargetLaneLeft, top + m.TargetNodeHeight * 0.4f);
            return new GraphPoint(m.TargetLaneLeft, m.TopPadding);
        }
    }
}
