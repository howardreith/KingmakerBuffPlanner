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
        private readonly AbilityKey _ability;

        internal SetupSourceRow(string sourceId, IEnumerable<AbilityKey> abilities,
            AbilityKey representativeAbility, string displayName, int spellLevel,
            IEnumerable<ProviderSnapshot> providers)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Source ID is required.", "sourceId");
            SourceId = sourceId;
            var abilityList = (abilities ?? throw new ArgumentNullException("abilities"))
                .Where(item => item != null).GroupBy(item => item.Canonical, StringComparer.Ordinal)
                .Select(group => group.First()).OrderBy(item => item.Canonical, StringComparer.Ordinal).ToList();
            if (abilityList.Count == 0) throw new ArgumentException("At least one ability is required.", "abilities");
            Abilities = new ReadOnlyCollection<AbilityKey>(abilityList);
            _ability = representativeAbility ?? throw new ArgumentNullException("representativeAbility");
            if (!Abilities.Any(item => item.Equals(_ability)))
                throw new ArgumentException("Representative ability must belong to the aggregate.", "representativeAbility");
            DisplayName = displayName;
            SpellLevel = spellLevel;
            var ordered = providers.OrderBy(p => p.Key.Canonical, StringComparer.Ordinal).ToList();
            Providers = new ReadOnlyCollection<ProviderSnapshot>(ordered);
            Description = ordered.Select(p => p.Description).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
            DurationText = ordered.Select(p => p.DurationText).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
            ExpectedDurationRounds = ordered.Count == 0 ? 0 : ordered.Max(p => p.ExpectedDurationRounds);
        }

        public AbilityKey Ability { get { return _ability; } }
        public IReadOnlyList<AbilityKey> Abilities { get; private set; }
        public string DisplayName { get; private set; }
        public int SpellLevel { get; private set; }
        public IReadOnlyList<ProviderSnapshot> Providers { get; private set; }
        public string Description { get; private set; }
        public string DurationText { get; private set; }
        public int ExpectedDurationRounds { get; private set; }
        public string SourceId { get; private set; }

        internal bool HasSourceKind(SourceKind kind)
        {
            return Abilities.Any(ability => ability.SourceKind == kind);
        }
    }

    public sealed class PlannerSetupModel
    {
        private readonly Action<BuffPlannerProfile> _save;
        private readonly ActiveEffectSnapshot _activeEffects;
        private readonly IReadOnlyDictionary<string, EffectExpression> _effects;
        private readonly IReadOnlyList<ProviderPlanningOption> _providerOptions;
        private readonly IReadOnlyList<CastEnhancementSnapshot> _enhancements;

        public PlannerSetupModel(
            BuffPlannerProfile profile,
            PartyProviderSnapshot snapshot,
            ActiveEffectSnapshot activeEffects,
            IDictionary<string, EffectExpression> effectsByAbilityKey,
            IEnumerable<ProviderPlanningOption> providerOptions,
            Action<BuffPlannerProfile> save,
            IEnumerable<CastEnhancementSnapshot> enhancements = null)
        {
            Profile = profile ?? throw new ArgumentNullException("profile");
            foreach (SourceAssignmentProfile assignment in Profile.Routines
                .SelectMany(routine => routine.Assignments))
                if (assignment.SelectedEnhancementIds == null)
                    assignment.SelectedEnhancementIds = new List<string>();
            Snapshot = snapshot ?? throw new ArgumentNullException("snapshot");
            _activeEffects = activeEffects ?? throw new ArgumentNullException("activeEffects");
            var effects = new Dictionary<string, EffectExpression>(effectsByAbilityKey ??
                new Dictionary<string, EffectExpression>(), StringComparer.Ordinal);
            _providerOptions = new ReadOnlyCollection<ProviderPlanningOption>(
                (providerOptions ?? new ProviderPlanningOption[0])
                    .OrderBy(item => item.Provider.Key.Canonical, StringComparer.Ordinal).ToList());
            _save = save ?? throw new ArgumentNullException("save");
            _enhancements = new ReadOnlyCollection<CastEnhancementSnapshot>(
                (enhancements ?? new CastEnhancementSnapshot[0])
                    .Where(value => value != null).OrderBy(value => value.DisplayName,
                        StringComparer.OrdinalIgnoreCase).ThenBy(value => value.EnhancementId,
                        StringComparer.Ordinal).ToList());
            Sources = new ReadOnlyCollection<SetupSourceRow>(snapshot.Providers
                .GroupBy(provider => AggregateId(provider.Key.Ability, effects), StringComparer.Ordinal)
                .Select(group => CreateSourceRow(group.Key, group))
                .OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.SpellLevel)
                .ThenBy(s => s.SourceId, StringComparer.Ordinal).ToList());
            foreach (SetupSourceRow source in Sources)
            {
                EffectExpression expression;
                if (effects.TryGetValue(source.Ability.Canonical, out expression))
                    effects[source.SourceId] = expression;
            }
            _effects = new ReadOnlyDictionary<string, EffectExpression>(effects);
            AssignmentMigrationApplied = RebindLegacyAssignments();
            if (AssignmentMigrationApplied) _save(Profile);
            SelectedSourceId = Sources.Count == 0 ? string.Empty : Sources[0].SourceId;
        }

        public BuffPlannerProfile Profile { get; private set; }
        public PartyProviderSnapshot Snapshot { get; private set; }
        public IReadOnlyList<SetupSourceRow> Sources { get; private set; }
        public bool AssignmentMigrationApplied { get; private set; }
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
            if (assignment.WantedTargetUnitIds.Count == 0 && assignment.SelectedEnhancementIds.Count == 0) routine.Assignments.Remove(assignment);
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
                (!EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.Party) &&
                 !EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.AreaRecipients))) return false;
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
                assignment.WantedTargetUnitIds = next;
                if (assignment.SelectedEnhancementIds.Count == 0) routine.Assignments.Remove(assignment);
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

        public CastEnhancementSnapshot GetEnhancement(string enhancementId)
        {
            return _enhancements.FirstOrDefault(value => value.EnhancementId == enhancementId);
        }

        public IReadOnlyList<CastEnhancementSnapshot> GetApplicableEnhancements()
        {
            SetupSourceRow source = SelectedSource;
            if (source == null) return new ReadOnlyCollection<CastEnhancementSnapshot>(
                new List<CastEnhancementSnapshot>());
            return new ReadOnlyCollection<CastEnhancementSnapshot>(_enhancements.Where(value =>
                    value.RemainingUses != 0 && source.Providers.Any(value.IsApplicable))
                .OrderBy(value => value.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.EnhancementId, StringComparer.Ordinal).ToList());
        }

        public IReadOnlyList<string> GetSelectedEnhancementIds(string routineId)
        {
            SourceAssignmentProfile assignment = FindRoutine(routineId).Assignments
                .FirstOrDefault(value => value.SourceId == SelectedSourceId);
            return new ReadOnlyCollection<string>((assignment == null ||
                    assignment.SelectedEnhancementIds == null ? (IEnumerable<string>)new string[0] :
                    assignment.SelectedEnhancementIds)
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
        }

        public string GetEnhancementSummary(string routineId)
        {
            IReadOnlyList<string> selected = GetSelectedEnhancementIds(routineId);
            IReadOnlyList<CastEnhancementSnapshot> applicable = GetApplicableEnhancements();
            if (selected.Count == 0) return applicable.Count == 0
                ? "Enhancement: None available"
                : "Enhancement: None  " + applicable.Count + " available";
            CastEnhancementSnapshot enhancement = GetEnhancement(selected[0]);
            if (enhancement == null || !applicable.Any(value =>
                value.EnhancementId == selected[0]))
                return "Enhancement unavailable: " + (enhancement == null
                    ? "Unknown source" : enhancement.DisplayName);
            return "Enhancement: " + enhancement.DisplayName + UsesSuffix(enhancement);
        }

        public string GetEnhancementDescription(string routineId)
        {
            IReadOnlyList<string> selected = GetSelectedEnhancementIds(routineId);
            if (selected.Count == 0)
            {
                IReadOnlyList<CastEnhancementSnapshot> options = GetApplicableEnhancements();
                return options.Count == 0 ? "No applicable cast enhancements are available." :
                    "Choose None or an applicable caster-owned enhancement. " +
                    options.Count + " option(s) available.";
            }
            CastEnhancementSnapshot value = GetEnhancement(selected[0]);
            if (value == null) return "Unavailable persisted enhancement: " + selected[0];
            return value.DisplayName + UsesSuffix(value) + "\n" +
                EffectName(value) + " | Maximum spell level " + value.MaximumSpellLevel +
                (string.IsNullOrWhiteSpace(value.Description) ? string.Empty :
                    "\n" + value.Description);
        }

        public void SetEnhancement(string routineId, string enhancementId)
        {
            SetupSourceRow source = RequireSelected();
            RoutineProfile routine = FindRoutine(routineId);
            SourceAssignmentProfile assignment = routine.Assignments
                .FirstOrDefault(value => value.SourceId == source.SourceId);
            if (!string.IsNullOrWhiteSpace(enhancementId) && !GetApplicableEnhancements()
                .Any(value => value.EnhancementId == enhancementId))
                throw new InvalidOperationException("The enhancement is not currently applicable and available.");
            if (assignment == null)
            {
                if (string.IsNullOrWhiteSpace(enhancementId)) return;
                assignment = CreateAssignment(source);
                routine.Assignments.Add(assignment);
            }
            assignment.SelectedEnhancementIds = string.IsNullOrWhiteSpace(enhancementId)
                ? new List<string>() : new List<string> { enhancementId };
            if (assignment.SelectedEnhancementIds.Count == 0 &&
                assignment.WantedTargetUnitIds.Count == 0) routine.Assignments.Remove(assignment);
            _save(Profile);
        }

        public void CycleEnhancement(string routineId)
        {
            IReadOnlyList<CastEnhancementSnapshot> options = GetApplicableEnhancements();
            string current = GetSelectedEnhancementIds(routineId).FirstOrDefault();
            int index = options.ToList().FindIndex(value => value.EnhancementId == current);
            string next = index < 0 ? (options.Count == 0 ? null : options[0].EnhancementId) :
                (index + 1 < options.Count ? options[index + 1].EnhancementId : null);
            SetEnhancement(routineId, next);
        }

        internal static string EffectName(CastEnhancementSnapshot enhancement)
        {
            string value = enhancement == null ? string.Empty : enhancement.EffectDisplayName;
            if (string.IsNullOrWhiteSpace(value) || value == "Metamagic") return "Metamagic";
            return value.EndsWith(" Spell", StringComparison.OrdinalIgnoreCase)
                ? value : value + " Spell";
        }

        internal static string UsesSuffix(CastEnhancementSnapshot enhancement)
        {
            if (enhancement == null || enhancement.RemainingUses == null) return string.Empty;
            return "  " + enhancement.RemainingUses.Value +
                (enhancement.RemainingUses.Value == 1 ? " use" : " uses");
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
                IgnoredPresenceMarkers = new List<string>(),
                SelectedEnhancementIds = new List<string>()
            };
        }

        private static string AggregateId(AbilityKey ability,
            IDictionary<string, EffectExpression> effects)
        {
            EffectExpression expression;
            effects.TryGetValue(ability.Canonical, out expression);
            return EffectAggregateIdentity.For(expression, ability.Canonical);
        }

        private static SetupSourceRow CreateSourceRow(string sourceId,
            IEnumerable<ProviderSnapshot> providers)
        {
            List<ProviderSnapshot> values = providers.OrderBy(provider => provider.DisplayName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(provider => provider.SpellLevel)
                .ThenBy(provider => provider.Key.Canonical, StringComparer.Ordinal).ToList();
            ProviderSnapshot representative = values[0];
            return new SetupSourceRow(sourceId, values.Select(provider => provider.Key.Ability),
                representative.Key.Ability, representative.DisplayName,
                values.Min(provider => provider.SpellLevel), values);
        }

        private bool RebindLegacyAssignments()
        {
            var sourceByLegacyId = Sources.SelectMany(source => source.Abilities.Select(ability =>
                    new { LegacyId = ability.Canonical, Source = source }))
                .ToDictionary(item => item.LegacyId, item => item.Source, StringComparer.Ordinal);
            bool changed = false;
            foreach (RoutineProfile routine in Profile.Routines)
            {
                var rebound = new List<SourceAssignmentProfile>();
                var aggregateAssignments = new Dictionary<string, SourceAssignmentProfile>(StringComparer.Ordinal);
                foreach (SourceAssignmentProfile assignment in routine.Assignments)
                {
                    SetupSourceRow source;
                    if (!sourceByLegacyId.TryGetValue(assignment.SourceId, out source))
                        source = Sources.FirstOrDefault(item => item.SourceId == assignment.SourceId);
                    if (source == null)
                    {
                        rebound.Add(assignment);
                        continue;
                    }
                    SourceAssignmentProfile existing;
                    if (!aggregateAssignments.TryGetValue(source.SourceId, out existing))
                    {
                        if (assignment.SourceId != source.SourceId ||
                            assignment.Ability.ToKey().Canonical != source.Ability.Canonical)
                            changed = true;
                        assignment.SourceId = source.SourceId;
                        assignment.Ability = AbilityKeyProfile.FromKey(source.Ability);
                        aggregateAssignments.Add(source.SourceId, assignment);
                        rebound.Add(assignment);
                        continue;
                    }
                    existing.WantedTargetUnitIds = existing.WantedTargetUnitIds
                        .Concat(assignment.WantedTargetUnitIds).Distinct(StringComparer.Ordinal)
                        .OrderBy(item => item, StringComparer.Ordinal).ToList();
                    existing.IgnoredPresenceMarkers = existing.IgnoredPresenceMarkers
                        .Concat(assignment.IgnoredPresenceMarkers).Distinct(StringComparer.Ordinal)
                        .OrderBy(item => item, StringComparer.Ordinal).ToList();
                    existing.SelectedEnhancementIds = (existing.SelectedEnhancementIds ?? new List<string>())
                        .Concat(assignment.SelectedEnhancementIds ?? new List<string>())
                        .Distinct(StringComparer.Ordinal).OrderBy(item => item, StringComparer.Ordinal).ToList();
                    if (assignment.ExistingEffectPolicy == ExistingEffectPolicy.Overwrite)
                        existing.ExistingEffectPolicy = ExistingEffectPolicy.Overwrite;
                    changed = true;
                }
                if (changed) routine.Assignments = rebound;
            }
            return changed;
        }

        private void RequireProvider(string providerKey)
        {
            if (!Snapshot.Providers.Any(p => p.Key.Canonical == providerKey))
                throw new ArgumentException("Unknown provider.", "providerKey");
        }
    }
}
