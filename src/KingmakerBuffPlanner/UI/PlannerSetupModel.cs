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
            Providers = new ReadOnlyCollection<ProviderSnapshot>(providers
                .OrderBy(p => p.Key.Canonical, StringComparer.Ordinal).ToList());
        }

        public AbilityKey Ability { get; private set; }
        public string DisplayName { get; private set; }
        public int SpellLevel { get; private set; }
        public IReadOnlyList<ProviderSnapshot> Providers { get; private set; }
        public string SourceId { get { return Ability.Canonical; } }
    }

    public sealed class PlannerSetupModel
    {
        private readonly Action<BuffPlannerProfile> _save;
        private readonly ActiveEffectSnapshot _activeEffects;
        private readonly IReadOnlyDictionary<string, EffectExpression> _effects;

        public PlannerSetupModel(
            BuffPlannerProfile profile,
            PartyProviderSnapshot snapshot,
            ActiveEffectSnapshot activeEffects,
            IDictionary<string, EffectExpression> effectsByAbilityKey,
            Action<BuffPlannerProfile> save)
        {
            Profile = profile ?? throw new ArgumentNullException("profile");
            Snapshot = snapshot ?? throw new ArgumentNullException("snapshot");
            _activeEffects = activeEffects ?? throw new ArgumentNullException("activeEffects");
            _effects = new ReadOnlyDictionary<string, EffectExpression>(
                new Dictionary<string, EffectExpression>(effectsByAbilityKey ??
                    new Dictionary<string, EffectExpression>(), StringComparer.Ordinal));
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

        public void ToggleRoutine(string routineId)
        {
            SetupSourceRow source = RequireSelected();
            RoutineProfile routine = FindRoutine(routineId);
            SourceAssignmentProfile existing = routine.Assignments.FirstOrDefault(a => a.SourceId == source.SourceId);
            if (existing == null)
            {
                routine.Assignments.Add(new SourceAssignmentProfile
                {
                    SourceId = source.SourceId,
                    Ability = AbilityKeyProfile.FromKey(source.Ability),
                    WantedTargetUnitIds = new List<string>(),
                    ExistingEffectPolicy = ExistingEffectPolicy.SkipAlreadyActive,
                    IgnoredPresenceMarkers = new List<string>()
                });
            }
            else routine.Assignments.Remove(existing);
            _save(Profile);
        }

        public void ToggleTarget(string routineId, string unitId)
        {
            if (!Snapshot.Units.Any(u => u.UnitId == unitId)) throw new ArgumentException("Unknown unit.", "unitId");
            SourceAssignmentProfile assignment = RequireAssignment(routineId);
            if (assignment.WantedTargetUnitIds.Contains(unitId)) assignment.WantedTargetUnitIds.Remove(unitId);
            else assignment.WantedTargetUnitIds.Add(unitId);
            _save(Profile);
        }

        public bool IsTargetWanted(string routineId, string unitId)
        {
            SourceAssignmentProfile assignment = FindRoutine(routineId).Assignments
                .FirstOrDefault(a => a.SourceId == SelectedSourceId);
            return assignment != null && assignment.WantedTargetUnitIds.Contains(unitId);
        }

        public EffectPresenceKind GetPresence(string unitId)
        {
            EffectExpression expression;
            if (!_effects.TryGetValue(SelectedSourceId, out expression)) return EffectPresenceKind.Absent;
            return new EffectPresenceEvaluator().EvaluateTyped(expression,
                _activeEffects.GetEffects(unitId), new HashSet<string>(StringComparer.Ordinal)).Kind;
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

        public void ToggleHidden()
        {
            SetupSourceRow source = RequireSelected();
            if (Profile.HiddenSourceIds.Contains(source.SourceId)) Profile.HiddenSourceIds.Remove(source.SourceId);
            else Profile.HiddenSourceIds.Add(source.SourceId);
            _save(Profile);
        }

        public void ToggleExistingEffectPolicy(string routineId)
        {
            SourceAssignmentProfile assignment = RequireAssignment(routineId);
            assignment.ExistingEffectPolicy = assignment.ExistingEffectPolicy == ExistingEffectPolicy.SkipAlreadyActive
                ? ExistingEffectPolicy.Overwrite
                : ExistingEffectPolicy.SkipAlreadyActive;
            _save(Profile);
        }

        public ExistingEffectPolicy GetExistingEffectPolicy(string routineId)
        {
            SourceAssignmentProfile assignment = FindRoutine(routineId).Assignments
                .FirstOrDefault(a => a.SourceId == SelectedSourceId);
            return assignment == null
                ? ExistingEffectPolicy.SkipAlreadyActive
                : assignment.ExistingEffectPolicy;
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

        private SourceAssignmentProfile RequireAssignment(string routineId)
        {
            RoutineProfile routine = FindRoutine(routineId);
            SourceAssignmentProfile assignment = routine.Assignments.FirstOrDefault(a => a.SourceId == SelectedSourceId);
            if (assignment == null)
            {
                ToggleRoutine(routineId);
                assignment = routine.Assignments.First(a => a.SourceId == SelectedSourceId);
            }
            return assignment;
        }

        private void RequireProvider(string providerKey)
        {
            if (!Snapshot.Providers.Any(p => p.Key.Canonical == providerKey))
                throw new ArgumentException("Unknown provider.", "providerKey");
        }
    }
}
