using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public sealed class BuffCardViewModel
    {
        internal BuffCardViewModel(SetupSourceRow source, PlannerSetupModel model, bool selected)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (model == null) throw new ArgumentNullException("model");
            SourceId = source.SourceId;
            Name = string.IsNullOrWhiteSpace(source.DisplayName) ? "Unnamed buff" : source.DisplayName;
            Selected = selected;
            RoutineBadge = BuildRoutineBadge(model.Profile, source.SourceId);
            int requested = model.Profile.Routines.SelectMany(routine => routine.Assignments)
                .Where(assignment => assignment.SourceId == source.SourceId)
                .SelectMany(assignment => assignment.WantedTargetUnitIds)
                .Distinct(StringComparer.Ordinal).Count();
            Configured = RoutineBadge.Length != 0;
            bool available = model.IsSourceAvailable(source);
            Availability = BuildAvailability(source, model);
            Configuration = requested == 0
                ? (Configured ? "Choose targets" : "Not configured")
                : requested == 1 ? "1 target configured" : requested + " targets configured";
            Status = !Configured ? PlannerPresentationStatus.Neutral :
                requested == 0 ? PlannerPresentationStatus.Warning :
                available ? PlannerPresentationStatus.Success : PlannerPresentationStatus.Failure;
            SourceType = PlayerSourceType(source.Ability.SourceKind);
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
            return routine.Assignments.Any(item => item.SourceId == sourceId);
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

        internal static string BuildAvailabilityForProvider(
            ProviderSnapshot provider, PlannerSetupModel model)
        {
            string rejection = model.GetProviderRejectionReason(provider);
            if (!string.IsNullOrEmpty(rejection)) return PlayerReason(rejection);
            int? remaining = model.GetRemainingCasts(provider);
            if (remaining == null) return "At will";
            bool prepared = model.GetResourcePool(provider).Kind == ResourcePoolKind.PreparedSlots;
            if (remaining.Value == 1) return prepared ? "1 prepared" : "1 available";
            return remaining.Value + (prepared ? " prepared" : " available");
        }

        internal static string PlayerSourceType(SourceKind kind)
        {
            switch (kind)
            {
                case SourceKind.Spellbook: return "Spell";
                case SourceKind.AbilityResource: return "Ability";
                case SourceKind.Item: return "Item";
                default: return "Ability";
            }
        }

        internal static string PlayerReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) return string.Empty;
            if (reason == "resource pool exhausted") return "No casts remain";
            if (reason == "missing material component") return "Required item missing";
            if (reason == "caster unavailable") return "Caster unavailable";
            if (reason == "no legal party or pet target") return "No legal target";
            if (reason == "banned by profile") return "Casting source disabled";
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
            Status = !legal ? PlannerPresentationStatus.Failure : !wanted
                ? PlannerPresentationStatus.Neutral : fulfilled
                    ? PlannerPresentationStatus.Success : PlannerPresentationStatus.Warning;
        }

        public string UnitId { get; private set; }
        public string Name { get; private set; }
        public bool IsPet { get; private set; }
        public bool Wanted { get; private set; }
        public bool Legal { get; private set; }
        public bool Indirect { get; private set; }
        public PlannerPresentationStatus Status { get; private set; }
    }

    public sealed class CastingSourceSummaryViewModel
    {
        internal CastingSourceSummaryViewModel(SetupSourceRow source, PlannerSetupModel model)
        {
            var parts = source.Providers.Take(2).Select(provider =>
                model.GetCasterDisplayName(provider) + " — " +
                BuffCardViewModel.BuildAvailabilityForProvider(provider, model)).ToArray();
            Summary = source.Providers.Count == 0 ? "Automatic — no casting source" :
                source.Providers.Count == 1 ? "Automatic  " + parts[0] :
                "Automatic — best available caster";
            ProviderCount = source.Providers.Count;
        }

        public string Summary { get; private set; }
        public int ProviderCount { get; private set; }
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
        }

        public string CastingMode { get; private set; }
        public string CombatUse { get; private set; }
        public string Fallback { get; private set; }
    }

}
