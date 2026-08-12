using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    public sealed class SetupSourceRow
    {
        internal SetupSourceRow(AbilityKey ability, string displayName, int spellLevel, IEnumerable<ProviderSnapshot> providers)
        {
            Ability = ability;
            DisplayName = displayName;
            SpellLevel = spellLevel;
            var ordered = providers.OrderBy(p => p.Key.Canonical, StringComparer.Ordinal).ToList();
            Providers = new ReadOnlyCollection<ProviderSnapshot>(ordered);
            Description = ordered.Select(p => p.Description).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
            DurationText = ordered.Select(p => p.DurationText).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
            ExpectedDurationRounds = ordered.Count == 0 ? 0 : ordered.Max(p => p.ExpectedDurationRounds);
        }

        public AbilityKey Ability { get; private set; }
        public string DisplayName { get; private set; }
        public int SpellLevel { get; private set; }
        public IReadOnlyList<ProviderSnapshot> Providers { get; private set; }
        public string Description { get; private set; }
        public string DurationText { get; private set; }
        public int ExpectedDurationRounds { get; private set; }
        public string SourceId { get { return Ability.Canonical; } }
    }

    public sealed class PlannerSetupModel
    {
        private readonly Action<BuffPlannerProfile> _save;
        private readonly ActiveEffectSnapshot _activeEffects;
        private readonly IReadOnlyDictionary<string, EffectExpression> _effects;
        private readonly IReadOnlyList<ProviderPlanningOption> _providerOptions;

        public PlannerSetupModel(
            BuffPlannerProfile profile,
            PartyProviderSnapshot snapshot,
            ActiveEffectSnapshot activeEffects,
            IDictionary<string, EffectExpression> effectsByAbilityKey,
            IEnumerable<ProviderPlanningOption> providerOptions,
            Action<BuffPlannerProfile> save)
        {
            Profile = profile ?? throw new ArgumentNullException("profile");
            Snapshot = snapshot ?? throw new ArgumentNullException("snapshot");
            _activeEffects = activeEffects ?? throw new ArgumentNullException("activeEffects");
            _effects = new ReadOnlyDictionary<string, EffectExpression>(
                new Dictionary<string, EffectExpression>(effectsByAbilityKey ??
                    new Dictionary<string, EffectExpression>(), StringComparer.Ordinal));
            _providerOptions = new ReadOnlyCollection<ProviderPlanningOption>(
                (providerOptions ?? new ProviderPlanningOption[0])
                    .OrderBy(item => item.Provider.Key.Canonical, StringComparer.Ordinal).ToList());
            _save = save ?? throw new ArgumentNullException("save");
            Sources = new ReadOnlyCollection<SetupSourceRow>(snapshot.Providers
                .GroupBy(p => p.Key.Ability.Canonical, StringComparer.Ordinal)
                .Select(g => new SetupSourceRow(g.First().Key.Ability,
                    g.OrderBy(p => p.DisplayName, StringComparer.Ordinal).First().DisplayName,
                    g.Min(p => p.SpellLevel), g))
                .OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.SpellLevel)
                .ThenBy(s => s.SourceId, StringComparer.Ordinal).ToList());
            SelectedSourceId = Sources.Count == 0 ? string.Empty : Sources[0].SourceId;
        }

        public BuffPlannerProfile Profile { get; private set; }
        public PartyProviderSnapshot Snapshot { get; private set; }
        public IReadOnlyList<SetupSourceRow> Sources { get; private set; }
        public string SelectedSourceId { get; private set; }
        public SetupSourceRow SelectedSource { get { return Sources.FirstOrDefault(s => s.SourceId == SelectedSourceId); } }
        public IReadOnlyList<string> UnsupportedSavedSourceIds
        {
            get
            {
                var supported = new HashSet<string>(Sources.Select(s => s.SourceId), StringComparer.Ordinal);
                return new ReadOnlyCollection<string>(Profile.Routines.SelectMany(r => r.Assignments)
                    .Select(a => a.SourceId).Where(id => !supported.Contains(id))
                    .Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToList());
            }
        }

        public void SelectSource(string sourceId)
        {
            if (!Sources.Any(s => s.SourceId == sourceId)) throw new ArgumentException("Unknown source.", "sourceId");
            SelectedSourceId = sourceId;
        }

        public bool IsAssigned(string routineId)
        {
            RoutineProfile routine = FindRoutine(routineId);
            return routine.Assignments.Any(a => a.SourceId == SelectedSourceId);
        }

        public void ToggleTarget(string routineId, string unitId)
        {
            SetupSourceRow source = RequireSelected();
            if (!Snapshot.Units.Any(u => u.UnitId == unitId))
                throw new ArgumentException("Unknown unit.", "unitId");
            if (!IsTargetLegal(source, unitId))
                throw new InvalidOperationException("The selected buff cannot target this character.");
            RoutineProfile routine = FindRoutine(routineId);
            SourceAssignmentProfile assignment = routine.Assignments
                .FirstOrDefault(item => item.SourceId == source.SourceId);
            if (assignment == null)
            {
                assignment = CreateAssignment(source);
                routine.Assignments.Add(assignment);
            }
            if (assignment.WantedTargetUnitIds.Contains(unitId))
                assignment.WantedTargetUnitIds.Remove(unitId);
            else assignment.WantedTargetUnitIds.Add(unitId);
            if (assignment.WantedTargetUnitIds.Count == 0) routine.Assignments.Remove(assignment);
            _save(Profile);
        }

        public bool IsTargetWanted(string routineId, string unitId)
        {
            return IsTargetWanted(routineId, SelectedSourceId, unitId);
        }

        public bool IsTargetWanted(string routineId, string sourceId, string unitId)
        {
            SourceAssignmentProfile assignment = FindRoutine(routineId).Assignments
                .FirstOrDefault(a => a.SourceId == sourceId);
            return assignment != null && assignment.WantedTargetUnitIds.Contains(unitId);
        }

        public EffectPresenceKind GetPresence(string unitId)
        {
            return GetPresence(SelectedSourceId, unitId);
        }

        public EffectPresenceKind GetPresence(string sourceId, string unitId)
        {
            EffectExpression expression;
            if (!_effects.TryGetValue(sourceId, out expression)) return EffectPresenceKind.Absent;
            return new EffectPresenceEvaluator().EvaluateTyped(expression,
                _activeEffects.GetEffects(unitId), new HashSet<string>(StringComparer.Ordinal)).Kind;
        }

        public bool IsTargetLegal(SetupSourceRow source, string unitId)
        {
            if (source == null || !Snapshot.Units.Any(unit => unit.UnitId == unitId)) return false;
            return _providerOptions.Any(option => source.Providers.Any(provider =>
                option.Provider.Key.Equals(provider.Key)) && option.ReachableTargetIds.Contains(unitId));
        }

        public bool IsIndirectBeneficiary(SetupSourceRow source, string routineId, string unitId)
        {
            if (source == null || IsTargetWanted(routineId, source.SourceId, unitId)) return false;
            EffectExpression expression;
            if (!_effects.TryGetValue(source.SourceId, out expression) ||
                !EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.Party)) return false;
            SourceAssignmentProfile assignment = FindRoutine(routineId).Assignments
                .FirstOrDefault(item => item.SourceId == source.SourceId);
            return assignment != null && assignment.WantedTargetUnitIds.Count != 0 &&
                IsTargetLegal(source, unitId);
        }

        public void SetAllValidTargets(string routineId, bool selected)
        {
            SetupSourceRow source = RequireSelected();
            RoutineProfile routine = FindRoutine(routineId);
            SourceAssignmentProfile assignment = routine.Assignments
                .FirstOrDefault(item => item.SourceId == source.SourceId);
            List<string> next = selected ? Snapshot.Units
                .Where(unit => IsTargetLegal(source, unit.UnitId))
                .Select(unit => unit.UnitId).Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal).ToList() : new List<string>();
            if (next.Count == 0)
            {
                if (assignment == null) return;
                routine.Assignments.Remove(assignment);
                _save(Profile);
                return;
            }
            if (assignment == null)
            {
                assignment = CreateAssignment(source);
                routine.Assignments.Add(assignment);
            }
            if (assignment.WantedTargetUnitIds.SequenceEqual(next, StringComparer.Ordinal)) return;
            assignment.WantedTargetUnitIds = next;
            _save(Profile);
        }

        public ProviderPreferenceProfile GetProviderPreference(string providerKey)
        {
            return Profile.ProviderPreferences.FirstOrDefault(p => p.ProviderKey == providerKey);
        }

        public void CycleProviderPreference(string providerKey)
        {
            RequireProvider(providerKey);
            ProviderPreferenceProfile preference = GetProviderPreference(providerKey);
            if (preference == null)
            {
                Profile.ProviderPreferences.Add(new ProviderPreferenceProfile
                {
                    ProviderKey = providerKey,
                    Priority = 0,
                    Banned = false,
                    MaximumCasts = null
                });
            }
            else if (!preference.Banned)
            {
                preference.Banned = true;
                preference.Priority = null;
            }
            else Profile.ProviderPreferences.Remove(preference);
            _save(Profile);
        }

        public void AdjustProviderCap(string providerKey, int delta)
        {
            RequireProvider(providerKey);
            ProviderPreferenceProfile preference = GetProviderPreference(providerKey);
            if (preference == null)
            {
                preference = new ProviderPreferenceProfile
                {
                    ProviderKey = providerKey,
                    Banned = false,
                    Priority = null,
                    MaximumCasts = null
                };
                Profile.ProviderPreferences.Add(preference);
            }
            int next = (preference.MaximumCasts ?? 0) + delta;
            preference.MaximumCasts = next <= 0 ? (int?)null : next;
            if (!preference.Banned && preference.Priority == null && preference.MaximumCasts == null)
                Profile.ProviderPreferences.Remove(preference);
            _save(Profile);
        }

        public void SetScale(float scale)
        {
            if (scale < 0.5f || scale > 3.0f) throw new ArgumentOutOfRangeException("scale");
            Profile.Ui.Scale = scale;
            _save(Profile);
        }

        public void ToggleExecutionMode()
        {
            Profile.Execution.Mode = Profile.Execution.Mode == "instant" ? "animated" : "instant";
            _save(Profile);
        }

        public void ToggleOutOfCombatOnly()
        {
            Profile.Execution.OutOfCombatOnly = !Profile.Execution.OutOfCombatOnly;
            _save(Profile);
        }

        public void ToggleAnimatedFallback()
        {
            Profile.Execution.AllowAnimatedFallback = !Profile.Execution.AllowAnimatedFallback;
            _save(Profile);
        }

        public void ToggleRecastExisting()
        {
            Profile.Execution.RecastExisting = !Profile.Execution.RecastExisting;
            ExistingEffectPolicy policy = Profile.Execution.RecastExisting
                ? ExistingEffectPolicy.Overwrite : ExistingEffectPolicy.SkipAlreadyActive;
            foreach (SourceAssignmentProfile assignment in Profile.Routines
                .SelectMany(routine => routine.Assignments))
                assignment.ExistingEffectPolicy = policy;
            _save(Profile);
        }

        public void TogglePlannerHotkey()
        {
            Profile.Ui.Hotkey = Profile.Ui.Hotkey == "Ctrl+Shift+P"
                ? PlannerHotkeyText.Default : "Ctrl+Shift+P";
            _save(Profile);
        }

        public void ClearRoutine(string routineId)
        {
            RoutineProfile routine = FindRoutine(routineId);
            if (routine.Assignments.Count == 0) return;
            routine.Assignments.Clear();
            _save(Profile);
        }

        public string GetCasterDisplayName(ProviderSnapshot provider)
        {
            UnitSnapshot unit = Snapshot.Units.FirstOrDefault(u => u.UnitId == provider.Key.CasterUnitId);
            return unit == null ? provider.Key.CasterUnitId : unit.DisplayName;
        }

        public ResourcePoolSnapshot GetResourcePool(ProviderSnapshot provider)
        {
            return Snapshot.ResourcePools.First(p => p.PoolKey == provider.ResourcePoolKey);
        }

        public int? GetRemainingCasts(ProviderSnapshot provider)
        {
            ResourcePoolSnapshot pool = GetResourcePool(provider);
            if (pool.Kind == ResourcePoolKind.Unlimited || provider.UnitsPerCast == 0) return null;
            if (pool.Kind == ResourcePoolKind.PreparedSlots)
            {
                int available = pool.Tokens.Count(t => t.Available && t.IsPrimary &&
                    provider.EligibleTokenIds.Contains(t.TokenId));
                return available / Math.Max(1, provider.UnitsPerCast);
            }
            return pool.Remaining / Math.Max(1, provider.UnitsPerCast);
        }

        public string GetProviderRejectionReason(ProviderSnapshot provider)
        {
            ProviderPreferenceProfile preference = GetProviderPreference(provider.Key.Canonical);
            if (preference != null && preference.Banned) return "banned by profile";
            if (provider.MaterialComponent != null &&
                provider.MaterialComponent.AvailableCount < provider.MaterialComponent.RequiredCount)
                return "missing material component";
            int? remaining = GetRemainingCasts(provider);
            if (remaining != null && remaining.Value == 0) return "resource pool exhausted";
            UnitSnapshot caster = Snapshot.Units.FirstOrDefault(u => u.UnitId == provider.Key.CasterUnitId);
            if (caster == null || !caster.TargetValidation.Alive || !caster.TargetValidation.Conscious)
                return "caster unavailable";
            return string.Empty;
        }

        public bool IsSourceAvailable(SetupSourceRow source)
        {
            if (source == null) return false;
            return source.Providers.Any(provider =>
                string.IsNullOrEmpty(GetProviderRejectionReason(provider)) &&
                _providerOptions.Any(option => option.Provider.Key.Equals(provider.Key) &&
                    option.ReachableTargetIds.Count != 0));
        }

        public string GetSourceUnavailableReason(SetupSourceRow source)
        {
            if (source == null) return "source is absent";
            if (IsSourceAvailable(source)) return string.Empty;
            string rejection = source.Providers.Select(GetProviderRejectionReason)
                .FirstOrDefault(value => !string.IsNullOrEmpty(value));
            if (!string.IsNullOrEmpty(rejection)) return rejection;
            if (!_providerOptions.Any(option => source.Providers.Any(provider =>
                option.Provider.Key.Equals(provider.Key)))) return "no provider option was normalized";
            if (!_providerOptions.Any(option => source.Providers.Any(provider =>
                option.Provider.Key.Equals(provider.Key)) && option.ReachableTargetIds.Count != 0))
                return "no legal party or pet target";
            return "no available provider";
        }

        private SetupSourceRow RequireSelected()
        {
            SetupSourceRow source = SelectedSource;
            if (source == null) throw new InvalidOperationException("No source is selected.");
            return source;
        }

        private RoutineProfile FindRoutine(string routineId)
        {
            RoutineProfile routine = Profile.Routines.FirstOrDefault(r => r.RoutineId == routineId);
            if (routine == null) throw new ArgumentException("Unknown routine.", "routineId");
            return routine;
        }

        private SourceAssignmentProfile CreateAssignment(SetupSourceRow source)
        {
            return new SourceAssignmentProfile
            {
                SourceId = source.SourceId,
                Ability = AbilityKeyProfile.FromKey(source.Ability),
                WantedTargetUnitIds = new List<string>(),
                ExistingEffectPolicy = Profile.Execution.RecastExisting
                    ? ExistingEffectPolicy.Overwrite : ExistingEffectPolicy.SkipAlreadyActive,
                IgnoredPresenceMarkers = new List<string>()
            };
        }

        private void RequireProvider(string providerKey)
        {
            if (!Snapshot.Providers.Any(p => p.Key.Canonical == providerKey))
                throw new ArgumentException("Unknown provider.", "providerKey");
        }
    }
}
