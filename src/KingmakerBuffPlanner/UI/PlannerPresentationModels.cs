using System;
using System.Collections.Generic;
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
                .Count(unitId => model.IsTargetLegal(source, unitId));
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
            string description, bool selected, bool available)
        {
            EnhancementId = enhancementId ?? string.Empty;
            Title = title ?? string.Empty;
            Summary = summary ?? string.Empty;
            Description = description ?? string.Empty;
            Selected = selected;
            Available = available;
        }

        public string EnhancementId { get; private set; }
        public string Title { get; private set; }
        public string Summary { get; private set; }
        public string Description { get; private set; }
        public bool Selected { get; private set; }
        public bool Available { get; private set; }
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
            string selectedEnhancementId, IEnumerable<EnhancementChoiceViewModel> choices,
            CasterPolicyViewModel casterPolicy)
        {
            CasterText = casterText;
            CasterDetail = casterDetail;
            EnhancementLabel = enhancementLabel;
            EnhancementDescription = enhancementDescription;
            CandidateCount = candidateCount;
            SelectedEnhancementId = selectedEnhancementId ?? string.Empty;
            Choices = choices.ToList().AsReadOnly();
            CasterPolicy = casterPolicy ?? CasterPolicyViewModel.Empty();
        }

        public string CasterText { get; private set; }
        public string CasterDetail { get; private set; }
        public string EnhancementLabel { get; private set; }
        public string EnhancementDescription { get; private set; }
        public int CandidateCount { get; private set; }
        public string SelectedEnhancementId { get; private set; }
        public IReadOnlyList<EnhancementChoiceViewModel> Choices { get; private set; }
        public CasterPolicyViewModel CasterPolicy { get; private set; }

        public static SelectedCastingViewModel Create(SetupSourceRow source,
            PlannerSetupModel model, string routineId, RoutinePlanResult preview)
        {
            if (source == null || model == null)
                return new SelectedCastingViewModel("Caster: None", string.Empty,
                    "Enhancement: None available", "Select a buff to choose an enhancement.",
                    0, string.Empty, new[] { NoneChoice(true) },
                    CasterPolicyViewModel.Empty());

            IReadOnlyList<string> selectedIds = model.GetSelectedEnhancementIds(routineId);
            string selectedId = selectedIds.FirstOrDefault() ?? string.Empty;
            CastEnhancementSnapshot selected = model.GetEnhancement(selectedId);
            IReadOnlyList<CastEnhancementSnapshot> applicable = model.GetApplicableEnhancements();
            var choices = new List<EnhancementChoiceViewModel> { NoneChoice(selectedId.Length == 0) };
            choices.AddRange(applicable.Select(value => Choice(value,
                value.EnhancementId == selectedId, true, model)));
            if (selectedId.Length != 0 && !applicable.Any(value => value.EnhancementId == selectedId))
                choices.Add(selected == null
                    ? new EnhancementChoiceViewModel(selectedId, "Unavailable enhancement",
                        "Unavailable", "Persisted enhancement source: " + selectedId, true, false)
                    : Choice(selected, true, false, model));

            CasterPolicyViewModel casterPolicy = CasterPolicyViewModel.Create(
                source, model, routineId, preview);

            return new SelectedCastingViewModel(casterPolicy.Summary,
                casterPolicy.Description,
                model.GetEnhancementSummary(routineId), model.GetEnhancementDescription(routineId),
                applicable.Count, selectedId, choices, casterPolicy);
        }

        private static EnhancementChoiceViewModel NoneChoice(bool selected)
        {
            return new EnhancementChoiceViewModel(string.Empty, "None", "Unenhanced cast",
                "Cast without a temporary casting enhancement.", selected, true);
        }

        private static EnhancementChoiceViewModel Choice(CastEnhancementSnapshot value,
            bool selected, bool available, PlannerSetupModel model)
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
            if (!available) description = "Unavailable: " + description;
            return new EnhancementChoiceViewModel(value.EnhancementId, value.DisplayName,
                summary, description, selected, available);
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
            bool legal = model.IsTargetLegal(source, unit.UnitId);
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
