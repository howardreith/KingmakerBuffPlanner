using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;

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
        DirectSelected,
        IndirectCovered,
        ValidUnselected,
        Invalid,
        SelectedButUnfulfillable
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
            RoutineBadge = BuildRoutineBadge(model.Profile, source.SourceId);
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
            SourceType = source.Abilities.Select(ability => ability.SourceKind).Distinct().Count() > 1
                ? "Multiple sources" : PlayerSourceType(source.Ability.SourceKind);
        }

        public string SourceId { get; private set; }
        public string Name { get; private set; }
        public string RoutineBadge { get; private set; }
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

        private static string BuildRoutineBadge(BuffPlannerProfile profile, string sourceId)
        {
            var labels = new List<string>();
            if (Assigned(profile, "long", sourceId)) labels.Add("L");
            if (Assigned(profile, "important", sourceId)) labels.Add("I");
            if (Assigned(profile, "short", sourceId)) labels.Add("S");
            return string.Join(" ", labels.ToArray());
        }

        private static bool Assigned(BuffPlannerProfile profile, string routineId, string sourceId)
        {
            RoutineProfile routine = profile.Routines.First(item => item.RoutineId == routineId);
            return routine.Assignments.Any(item => item.SourceId == sourceId &&
                item.WantedTargetUnitIds.Count != 0);
        }

        private static string BuildAvailability(SetupSourceRow source, PlannerSetupModel model)
        {
            var usable = source.Providers.Where(provider =>
                string.IsNullOrEmpty(model.GetProviderRejectionReason(provider))).ToList();
            if (usable.Count == 0)
            {
                string reason = PlayerReason(model.GetSourceUnavailableReason(source));
                return string.IsNullOrWhiteSpace(reason) ? "Unavailable now" : reason;
            }
            if (usable.Any(provider => model.GetRemainingCasts(provider) == null)) return "At will";
            int remaining = usable.Sum(provider => model.GetRemainingCasts(provider) ?? 0);
            bool prepared = usable.Any(provider =>
                model.GetResourcePool(provider).Kind == ResourcePoolKind.PreparedSlots);
            if (remaining == 1) return prepared ? "1 prepared" : "1 available";
            return remaining + (prepared ? " prepared" : " available");
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

    public sealed class TargetPortraitViewModel
    {
        internal TargetPortraitViewModel(UnitSnapshot unit, bool wanted, bool legal, bool fulfilled,
            bool indirect)
        {
            UnitId = unit.UnitId;
            Name = string.IsNullOrWhiteSpace(unit.DisplayName) ? "Party member" : unit.DisplayName;
            IsPet = unit.IsPet;
            Wanted = wanted;
            Legal = legal;
            Indirect = indirect;
            State = !legal ? TargetPortraitState.Invalid : wanted
                ? (fulfilled ? TargetPortraitState.DirectSelected :
                    TargetPortraitState.SelectedButUnfulfillable)
                : indirect ? TargetPortraitState.IndirectCovered : TargetPortraitState.ValidUnselected;
            Status = State == TargetPortraitState.Invalid ? PlannerPresentationStatus.Failure :
                State == TargetPortraitState.DirectSelected || State == TargetPortraitState.IndirectCovered
                    ? PlannerPresentationStatus.Success :
                State == TargetPortraitState.SelectedButUnfulfillable
                    ? PlannerPresentationStatus.Warning : PlannerPresentationStatus.Neutral;
        }

        public string UnitId { get; private set; }
        public string Name { get; private set; }
        public bool IsPet { get; private set; }
        public bool Wanted { get; private set; }
        public bool Legal { get; private set; }
        public bool Indirect { get; private set; }
        public TargetPortraitState State { get; private set; }
        public PlannerPresentationStatus Status { get; private set; }

        internal static TargetPortraitViewModel Create(SetupSourceRow source,
            PlannerSetupModel model, string routineId, UnitSnapshot unit)
        {
            bool wanted = model.IsTargetWanted(routineId, source.SourceId, unit.UnitId);
            bool legal = model.IsTargetLegal(source, unit.UnitId);
            bool active = model.GetPresence(source.SourceId, unit.UnitId) ==
                Planning.EffectPresenceKind.Complete;
            bool fulfilled = active || (legal && model.IsSourceAvailable(source));
            return new TargetPortraitViewModel(unit, wanted, legal, fulfilled,
                model.IsIndirectBeneficiary(source, routineId, unit.UnitId));
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
            Label = name + "     " + fulfilled + "/" + requested +
                (requested == 0 || fulfilled == requested ? " ready" : string.Empty);
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public int Fulfilled { get; private set; }
        public int Requested { get; private set; }
        public string Label { get; private set; }
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
