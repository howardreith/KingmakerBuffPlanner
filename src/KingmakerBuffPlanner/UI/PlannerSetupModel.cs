using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    public sealed class SetupSourceRow
    {
        private readonly AbilityKey _ability;

        internal SetupSourceRow(string sourceId, IEnumerable<AbilityKey> abilities,
            AbilityKey representativeAbility, string displayName, string sourceDisplayName,
            int variantOrder, int spellLevel, IEnumerable<ProviderSnapshot> providers)
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
            DisplayName = displayName ?? string.Empty;
            SourceDisplayName = string.IsNullOrWhiteSpace(sourceDisplayName)
                ? DisplayName : sourceDisplayName;
            SearchText = AbilityDisplayNameFormatter.SearchText(
                DisplayName, SourceDisplayName);
            VariantOrder = variantOrder;
            IsConcreteVariant = CatalogSourceIdentity.IsVariant(SourceId);
            SortGroupName = IsConcreteVariant ? SourceDisplayName : DisplayName;
            SortGroupId = IsConcreteVariant ? _ability.BaseAbilityGuid : SourceId;
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
        public string SourceDisplayName { get; private set; }
        public string SearchText { get; private set; }
        public string SortGroupName { get; private set; }
        public string SortGroupId { get; private set; }
        public int VariantOrder { get; private set; }
        public bool IsConcreteVariant { get; private set; }
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

    public sealed class VariantReselectionNotice
    {
        internal VariantReselectionNotice(
            string routineId, string sourceId, string displayName, int candidateCount)
        {
            RoutineId = routineId ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            CandidateCount = candidateCount;
        }

        public string RoutineId { get; private set; }
        public string SourceId { get; private set; }
        public string DisplayName { get; private set; }
        public int CandidateCount { get; private set; }
    }

    public sealed class PlannerSetupModel
    {
        private readonly Action<BuffPlannerProfile> _save;
        private readonly ActiveEffectSnapshot _activeEffects;
        private readonly IReadOnlyDictionary<string, EffectExpression> _effects;
        private readonly IReadOnlyList<ProviderPlanningOption> _providerOptions;
        private readonly IReadOnlyList<CastEnhancementSnapshot> _enhancements;
        private readonly EffectiveProviderOptionResolver _targeting;

        public PlannerSetupModel(
            BuffPlannerProfile profile,
            PartyProviderSnapshot snapshot,
            ActiveEffectSnapshot activeEffects,
            IDictionary<string, EffectExpression> effectsByAbilityKey,
            IEnumerable<ProviderPlanningOption> providerOptions,
            Action<BuffPlannerProfile> save,
            IEnumerable<CastEnhancementSnapshot> enhancements = null,
            EffectiveProviderOptionResolver targeting = null)
        {
            Profile = profile ?? throw new ArgumentNullException("profile");
            foreach (SourceAssignmentProfile assignment in Profile.Routines
                .SelectMany(routine => routine.Assignments))
            {
                if (assignment.CastingAssignments == null)
                    assignment.CastingAssignments = new List<CastingAssignmentProfile>();
                foreach (CastingAssignmentProfile casting in assignment.CastingAssignments)
                    if (casting.Enhancements == null)
                        casting.Enhancements = new List<EnhancementSelectionProfile>();
            }
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
            _targeting = targeting ?? new EffectiveProviderOptionResolver();
            Sources = new ReadOnlyCollection<SetupSourceRow>(snapshot.Providers
                .GroupBy(provider => AggregateId(provider.Key.Ability, effects), StringComparer.Ordinal)
                .Select(group => CreateSourceRow(group.Key, group))
                .OrderBy(s => s.SortGroupName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.SortGroupId, StringComparer.Ordinal)
                .ThenBy(s => s.VariantOrder)
                .ThenBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase)
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
            VariantReselectionNotices = BuildVariantReselectionNotices();
            if (AssignmentMigrationApplied) _save(Profile);
            SelectedSourceId = Sources.Count == 0 ? string.Empty : Sources[0].SourceId;
        }

        public BuffPlannerProfile Profile { get; private set; }
        public PartyProviderSnapshot Snapshot { get; private set; }
        public IReadOnlyList<SetupSourceRow> Sources { get; private set; }
        public bool AssignmentMigrationApplied { get; private set; }
        public IReadOnlyList<VariantReselectionNotice> VariantReselectionNotices { get; private set; }
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
            if (!IsTargetLegal(source, routineId, unitId))
                throw new InvalidOperationException("The selected buff cannot target this character.");
            RoutineProfile routine = FindRoutine(routineId);
            SourceAssignmentProfile assignment = routine.Assignments
                .FirstOrDefault(item => item.SourceId == source.SourceId);
            CastingAssignmentProfile casting = AutomaticChild(routine, assignment, source);
            if (casting == null) return;
            if (assignment == null)
            {
                // AutomaticChild created the parent; track it locally.
                assignment = routine.Assignments.First(item =>
                    item.SourceId == source.SourceId);
            }
            if (casting.TargetUnitIds.Contains(unitId))
            {
                casting.TargetUnitIds.Remove(unitId);
            }
            else
            {
                // A target already on another child of this source must move,
                // not duplicate: the simple portrait view is authoritative.
                foreach (CastingAssignmentProfile sibling in assignment.CastingAssignments
                    .Where(child => child != casting && child.TargetUnitIds.Contains(unitId))
                    .ToList())
                    sibling.TargetUnitIds.Remove(unitId);
                PruneEmptyAssignment(routine, assignment);
                // Re-resolve: pruning may have rebuilt the automatic child.
                assignment = routine.Assignments
                    .FirstOrDefault(item => item.SourceId == source.SourceId);
                if (assignment == null)
                {
                    assignment = CreateAssignment(source);
                    routine.Assignments.Add(assignment);
                }
                casting = AutomaticChild(routine, assignment, source);
                if (casting == null) return;
                casting.TargetUnitIds.Add(unitId);
            }
            PruneEmptyAssignment(routine, assignment);
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

        public bool IsTargetLegal(SetupSourceRow source, string routineId,
            string unitId)
        {
            if (source == null || !Snapshot.Units.Any(unit =>
                    unit.UnitId == unitId)) return false;
            return GetEffectiveProviderOptions(source, routineId).Any(option =>
                option.ReachableTargetIds.Contains(unitId));
        }

        public bool IsIndirectBeneficiary(SetupSourceRow source, string routineId, string unitId)
        {
            if (source == null || IsTargetWanted(routineId, source.SourceId, unitId)) return false;
            EffectExpression expression;
            if (!_effects.TryGetValue(source.SourceId, out expression) ||
                (!EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.Party) &&
                 !EffectExpressionTargetAnalysis.Contains(
                     expression, EffectTarget.AlliedAreaRecipients))) return false;
            SourceAssignmentProfile assignment = FindRoutine(routineId).Assignments
                .FirstOrDefault(item => item.SourceId == source.SourceId);
            if (assignment == null || assignment.WantedTargetUnitIds.Count == 0) return false;
            return GetEffectiveProviderOptions(source, routineId)
                .Any(option => assignment.WantedTargetUnitIds.Any(anchor =>
                    option.LegalAnchorIds.Contains(anchor) &&
                    option.CoveredTargetIdsForAnchor(anchor).Contains(unitId)));
        }

        public void SetAllValidTargets(string routineId, bool selected)
        {
            SetupSourceRow source = RequireSelected();
            RoutineProfile routine = FindRoutine(routineId);
            SourceAssignmentProfile assignment = routine.Assignments
                .FirstOrDefault(item => item.SourceId == source.SourceId);
            List<string> next = selected ? Snapshot.Units
                .Where(unit => IsTargetLegal(source, routineId, unit.UnitId))
                .Select(unit => unit.UnitId).Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal).ToList() : new List<string>();
            if (next.Count == 0)
            {
                if (assignment == null) return;
                // Clearing targets clears targets: enhancements and pins on
                // the automatic child stay configured as intent.
                foreach (CastingAssignmentProfile casting in assignment.CastingAssignments)
                    if (casting.IsAutomatic)
                        casting.TargetUnitIds = new List<string>();
                PruneEmptyAssignment(routine, assignment);
                _save(Profile);
                return;
            }
            CastingAssignmentProfile automatic = AutomaticChild(routine, assignment, source);
            if (automatic == null) return;
            if (automatic.TargetUnitIds.SequenceEqual(next, StringComparer.Ordinal)) return;
            automatic.TargetUnitIds = next;
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
            CastingAssignmentProfile automatic = assignment == null
                ? null : assignment.AutomaticAssignment;
            return new ReadOnlyCollection<string>((automatic == null
                    ? (IEnumerable<string>)new string[0]
                    : automatic.Enhancements.Select(selection => selection.EnhancementId))
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
        }

        public string GetEnhancementSummary(string routineId)
        {
            IReadOnlyList<string> selected = GetSelectedEnhancementIds(routineId);
            IReadOnlyList<CastEnhancementSnapshot> applicable = GetApplicableEnhancements();
            if (selected.Count == 0) return applicable.Count == 0
                ? "Enhancement: None available"
                : "Enhancement: None  " + applicable.Count + " available";
            List<CastEnhancementSnapshot> values = selected.Select(GetEnhancement)
                .Where(value => value != null).OrderBy(value => value.DisplayName,
                    StringComparer.OrdinalIgnoreCase).ThenBy(value =>
                    value.EnhancementId, StringComparer.Ordinal).ToList();
            if (values.Count != selected.Count || values.Any(value =>
                    !applicable.Any(option => option.EnhancementId ==
                        value.EnhancementId)))
                return "Enhancement unavailable: " + string.Join(" + ",
                    selected.Select(id => GetEnhancement(id) == null ?
                        "Unknown source" : GetEnhancement(id).DisplayName).ToArray());
            if (values.Count == 1)
                return "Enhancement: " + values[0].DisplayName +
                    UsesSuffix(values[0]) + AggregateUsageSuffix(values);
            return "Enhancements: " + string.Join(" + ", values.Select(value =>
                value.DisplayName).ToArray()) + AggregateUsageSuffix(values);
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
            List<CastEnhancementSnapshot> values = selected.Select(GetEnhancement)
                .Where(value => value != null).OrderBy(value => value.DisplayName,
                    StringComparer.OrdinalIgnoreCase).ThenBy(value =>
                    value.EnhancementId, StringComparer.Ordinal).ToList();
            if (values.Count != selected.Count)
                return "Unavailable persisted enhancement(s): " + string.Join(", ",
                    selected.Where(id => GetEnhancement(id) == null).ToArray());
            string details = string.Join("\n", values.Select(value =>
                value.DisplayName + " — " + EffectName(value) +
                (value.Category == CastEnhancementCategory.MetamagicRod
                    ? " | Maximum spell level " + value.MaximumSpellLevel
                    : " | Qualifying caster spell only") +
                (string.IsNullOrWhiteSpace(value.Description) ? string.Empty :
                    "\n" + value.Description)).ToArray());
            return details + AggregateUsageDescription(values);
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
            if (assignment == null && string.IsNullOrWhiteSpace(enhancementId)) return;
            CastingAssignmentProfile casting = AutomaticChild(routine, assignment, source);
            if (casting == null) return;
            var selected = new List<string>(casting.Enhancements
                .Select(selection => selection.EnhancementId));
            if (string.IsNullOrWhiteSpace(enhancementId)) selected.Clear();
            else if (selected.Contains(enhancementId))
                selected.Remove(enhancementId);
            else
            {
                CastEnhancementSnapshot next = GetEnhancement(enhancementId);
                selected.RemoveAll(id =>
                {
                    CastEnhancementSnapshot current = GetEnhancement(id);
                    return current != null && current.ExclusiveGroupId ==
                        next.ExclusiveGroupId;
                });
                selected.Add(enhancementId);
                if (!CastEnhancementSnapshot.AreCompatible(selected.Select(
                        GetEnhancement)))
                    throw new InvalidOperationException(
                        "The selected enhancement combination is incompatible.");
            }
            // Targets are never pruned here: an enhancement change can make a
            // target illegal, but that intent stays configured, visible, and
            // repairable instead of being silently dropped.
            casting.Enhancements = selected.Distinct(StringComparer.Ordinal)
                .Select(id => new EnhancementSelectionProfile
                {
                    EnhancementId = id,
                    Required = casting.Enhancements
                        .Where(existing => existing.EnhancementId == id)
                        .Select(existing => existing.Required)
                        .DefaultIfEmpty(true).First()
                }).ToList();
            PruneEmptyAssignment(routine, assignment);
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
            if (enhancement.Category == CastEnhancementCategory.ClassFeature)
                return value;
            return value.EndsWith(" Spell", StringComparison.OrdinalIgnoreCase)
                ? value : value + " Spell";
        }

        internal static string UsesSuffix(CastEnhancementSnapshot enhancement)
        {
            if (enhancement == null || enhancement.RemainingUses == null) return string.Empty;
            return "  " + enhancement.RemainingUses.Value +
                (enhancement.RemainingUses.Value == 1 ? " use" : " uses");
        }

        public IReadOnlyList<ProviderPlanningOption> GetEffectiveProviderOptions(
            SetupSourceRow source, string routineId)
        {
            if (source == null) return new ReadOnlyCollection<ProviderPlanningOption>(
                new List<ProviderPlanningOption>());
            EffectExpression expression;
            if (!_effects.TryGetValue(source.SourceId, out expression))
                return new ReadOnlyCollection<ProviderPlanningOption>(
                    new List<ProviderPlanningOption>());
            SourceAssignmentProfile assignment = FindRoutine(routineId)
                .Assignments.FirstOrDefault(value => value.SourceId ==
                    source.SourceId);
            CastGroupingKind grouping;
            if (!EffectExpressionTargetAnalysis.TryGetGrouping(expression,
                    out grouping))
                return new ReadOnlyCollection<ProviderPlanningOption>(
                    new List<ProviderPlanningOption>());
            var request = new BuffCastRequest(new BuffSourceDefinition(
                    source.SourceId, source.Abilities, expression, grouping),
                assignment == null ? (IEnumerable<string>)new string[0] :
                    assignment.WantedTargetUnitIds,
                assignment == null ? ExistingEffectPolicy.SkipAlreadyActive :
                    assignment.ExistingEffectPolicy,
                assignment == null ? (IEnumerable<string>)new string[0] :
                    assignment.IgnoredPresenceMarkers,
                assignment == null ? (IEnumerable<string>)new string[0] :
                    assignment.SelectedEnhancementIds);
            return _targeting.Resolve(Snapshot, request, _providerOptions,
                _enhancements);
        }

        private static string AggregateUsageSuffix(
            IEnumerable<CastEnhancementSnapshot> enhancements)
        {
            List<CastEnhancementSnapshot> values = enhancements.ToList();
            string[] summaries = CastEnhancementSnapshot.UsageRequirements(values)
                .Select(pair =>
                {
                    CastEnhancementSnapshot representative = values.First(value =>
                        value.UsagePoolId == pair.Key);
                    return representative.UsagePoolDisplayName + ": " +
                        pair.Value + " per cast / " +
                        (representative.RemainingUses == null ? "unlimited" :
                            representative.RemainingUses.Value.ToString() +
                            " remaining");
                }).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            return summaries.Length == 0 ? string.Empty : "  " +
                string.Join("; ", summaries);
        }

        private static string AggregateUsageDescription(
            IEnumerable<CastEnhancementSnapshot> enhancements)
        {
            string suffix = AggregateUsageSuffix(enhancements);
            return string.IsNullOrWhiteSpace(suffix) ? string.Empty :
                "\n" + suffix.Trim();
        }
        public ProviderPreferenceProfile GetProviderPreference(string providerKey)
        {
            return Profile.ProviderPreferences.FirstOrDefault(p => p.ProviderKey == providerKey);
        }

        public IReadOnlyList<ProviderSnapshot> GetOrderedProviders(SetupSourceRow source)
        {
            if (source == null)
                return new ReadOnlyCollection<ProviderSnapshot>(
                    new List<ProviderSnapshot>());
            return new ReadOnlyCollection<ProviderSnapshot>(source.Providers
                .OrderBy(provider =>
                {
                    ProviderPreferenceProfile preference =
                        GetProviderPreference(provider.Key.Canonical);
                    return preference == null || preference.Priority == null
                        ? int.MaxValue : preference.Priority.Value;
                })
                .ThenBy(provider => provider.Key.Canonical, StringComparer.Ordinal)
                .ToList());
        }

        public void SetProviderEnabled(string providerKey, bool enabled)
        {
            RequireSelectedProvider(providerKey);
            ProviderPreferenceProfile preference = GetOrCreateProviderPreference(providerKey);
            preference.Banned = !enabled;
            RemoveAutomaticPreference(preference);
            _save(Profile);
        }

        public void SetProviderMaximumCasts(string providerKey, int? maximumCasts)
        {
            RequireSelectedProvider(providerKey);
            if (maximumCasts != null && maximumCasts.Value < 1)
                throw new ArgumentOutOfRangeException("maximumCasts");
            ProviderPreferenceProfile preference = GetOrCreateProviderPreference(providerKey);
            preference.MaximumCasts = maximumCasts;
            RemoveAutomaticPreference(preference);
            _save(Profile);
        }

        public void MoveProviderEarlier(string providerKey)
        {
            MoveProvider(providerKey, -1);
        }

        public void MoveProviderLater(string providerKey)
        {
            MoveProvider(providerKey, 1);
        }

        public void ResetSelectedSourceProvidersToAutomatic()
        {
            SetupSourceRow source = RequireSelected();
            var keys = new HashSet<string>(source.Providers.Select(
                provider => provider.Key.Canonical), StringComparer.Ordinal);
            int removed = Profile.ProviderPreferences.RemoveAll(
                preference => keys.Contains(preference.ProviderKey));
            if (removed != 0) _save(Profile);
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

        public IReadOnlyList<CastingAssignmentProfile> GetRoutineCastingOrder(string routineId)
        {
            RoutineProfile routine = FindRoutine(routineId);
            return new ReadOnlyCollection<CastingAssignmentProfile>(routine.Assignments
                .SelectMany(assignment => assignment.CastingAssignments)
                .OrderBy(child => child.Order)
                .ThenBy(child => child.AssignmentId, StringComparer.Ordinal).ToList());
        }

        public IReadOnlyList<CastingAssignmentProfile> GetCastingAssignments(
            string routineId, string sourceId)
        {
            SourceAssignmentProfile assignment = FindRoutine(routineId).Assignments
                .FirstOrDefault(value => value.SourceId == sourceId);
            return new ReadOnlyCollection<CastingAssignmentProfile>(assignment == null
                ? new List<CastingAssignmentProfile>()
                : assignment.CastingAssignments
                    .OrderBy(child => child.Order)
                    .ThenBy(child => child.AssignmentId, StringComparer.Ordinal).ToList());
        }

        public CastingAssignmentProfile FindCastingAssignment(string routineId,
            string sourceId, string assignmentId)
        {
            SourceAssignmentProfile assignment = FindRoutine(routineId).Assignments
                .FirstOrDefault(value => value.SourceId == sourceId);
            CastingAssignmentProfile casting = assignment == null ? null :
                assignment.CastingAssignments.FirstOrDefault(child =>
                    child.AssignmentId == assignmentId);
            if (casting == null)
                throw new ArgumentException("Unknown casting assignment.", "assignmentId");
            return casting;
        }

        public CastingAssignmentProfile AddCastingAssignment(string routineId, string sourceId)
        {
            SetupSourceRow source = Sources.FirstOrDefault(value => value.SourceId == sourceId);
            if (source == null) throw new ArgumentException("Unknown source.", "sourceId");
            RoutineProfile routine = FindRoutine(routineId);
            SourceAssignmentProfile assignment = routine.Assignments
                .FirstOrDefault(value => value.SourceId == sourceId);
            if (assignment == null)
            {
                assignment = CreateAssignment(source);
                routine.Assignments.Add(assignment);
            }
            var casting = new CastingAssignmentProfile
            {
                AssignmentId = NextManualAssignmentId(routine),
                Order = NextOrder(routine),
                CasterUnitId = null,
                SpellbookGuid = null,
                ProviderKey = null,
                TargetUnitIds = new List<string>(),
                Enhancements = new List<EnhancementSelectionProfile>()
            };
            assignment.CastingAssignments.Add(casting);
            _save(Profile);
            return casting;
        }

        // Splits one target out of an assignment into its own Automatic
        // child in one mutation; pins and enhancements of the original stay
        // on the original so nothing is silently duplicated or reinterpreted.
        public CastingAssignmentProfile SplitCastingAssignment(string routineId,
            string sourceId, string assignmentId, string unitId)
        {
            RoutineProfile routine = FindRoutine(routineId);
            SetupSourceRow source = Sources.FirstOrDefault(value => value.SourceId == sourceId);
            if (source == null) throw new ArgumentException("Unknown source.", "sourceId");
            CastingAssignmentProfile casting = FindCastingAssignment(
                routineId, sourceId, assignmentId);
            if (!casting.TargetUnitIds.Contains(unitId))
                throw new ArgumentException("Target is not configured on this assignment.", "unitId");
            SourceAssignmentProfile parent = routine.Assignments
                .First(value => value.SourceId == sourceId);
            var split = new CastingAssignmentProfile
            {
                AssignmentId = NextManualAssignmentId(routine),
                Order = casting.Order,
                // Splitting divides the target list, not the configuration:
                // pins and enhancement selections carry to the new row.
                CasterUnitId = casting.CasterUnitId,
                SpellbookGuid = casting.SpellbookGuid,
                ProviderKey = casting.ProviderKey,
                TargetUnitIds = new List<string> { unitId },
                Enhancements = casting.Enhancements
                    .Select(selection => new EnhancementSelectionProfile
                    {
                        EnhancementId = selection.EnhancementId,
                        Required = selection.Required
                    }).ToList()
            };
            casting.TargetUnitIds.Remove(unitId);
            // The split child allocates immediately after its origin so the
            // routine-wide order stays explicit; renumbering keeps it total.
            parent.CastingAssignments.Insert(
                parent.CastingAssignments.IndexOf(casting) + 1, split);
            RenumberAssignmentOrders(routine);
            _save(Profile);
            return split;
        }

        // An explicit move is atomic: one save, no duplicate target left
        // behind on the source assignment, and no silent removal when the
        // move is refused.
        public void MoveTargetToAssignment(string routineId, string sourceId,
            string fromAssignmentId, string toAssignmentId, string unitId)
        {
            FindRoutine(routineId);
            CastingAssignmentProfile from = FindCastingAssignment(
                routineId, sourceId, fromAssignmentId);
            CastingAssignmentProfile to = FindCastingAssignment(
                routineId, sourceId, toAssignmentId);
            if (!from.TargetUnitIds.Contains(unitId))
                throw new ArgumentException("Target is not configured on the source assignment.", "unitId");
            if (to.TargetUnitIds.Contains(unitId))
                throw new ArgumentException(
                    "Target is already explicitly assigned there; refusing to duplicate it.", "unitId");
            from.TargetUnitIds.Remove(unitId);
            to.TargetUnitIds.Add(unitId);
            _save(Profile);
        }

        // Invalid or vanished targets must always be removable; removal never
        // touches other targets, pins, or enhancements.
        public void RemoveTargetFromAssignment(string routineId, string sourceId,
            string assignmentId, string unitId)
        {
            RoutineProfile routine = FindRoutine(routineId);
            CastingAssignmentProfile casting = FindCastingAssignment(
                routineId, sourceId, assignmentId);
            if (!casting.TargetUnitIds.Remove(unitId)) return;
            SourceAssignmentProfile parent = routine.Assignments
                .First(value => value.SourceId == sourceId);
            if (casting.IsAutomatic && casting.TargetUnitIds.Count == 0 &&
                casting.Enhancements.Count == 0)
                parent.CastingAssignments.Remove(casting);
            if (parent.CastingAssignments.Count == 0)
                routine.Assignments.Remove(parent);
            _save(Profile);
        }

        // A pin is a hard eligibility constraint stored verbatim; setting it
        // never rewrites targets or enhancements even when the pinned caster
        // is currently missing, unconscious, or otherwise unavailable.
        public void SetCastingAssignmentCaster(string routineId, string sourceId,
            string assignmentId, string casterUnitId, string spellbookGuid)
        {
            FindRoutine(routineId);
            CastingAssignmentProfile casting = FindCastingAssignment(
                routineId, sourceId, assignmentId);
            casting.CasterUnitId = string.IsNullOrWhiteSpace(casterUnitId)
                ? null : casterUnitId;
            casting.SpellbookGuid = string.IsNullOrWhiteSpace(spellbookGuid)
                ? null : spellbookGuid;
            casting.ProviderKey = null;
            _save(Profile);
        }

        public void SetCastingAssignmentProvider(string routineId, string sourceId,
            string assignmentId, string providerKey)
        {
            FindRoutine(routineId);
            CastingAssignmentProfile casting = FindCastingAssignment(
                routineId, sourceId, assignmentId);
            casting.ProviderKey = string.IsNullOrWhiteSpace(providerKey)
                ? null : providerKey;
            casting.CasterUnitId = null;
            casting.SpellbookGuid = null;
            _save(Profile);
        }

        public void SetCastingAssignmentEnhancementPolicy(string routineId,
            string sourceId, string assignmentId, string enhancementId, bool required)
        {
            FindRoutine(routineId);
            CastingAssignmentProfile casting = FindCastingAssignment(
                routineId, sourceId, assignmentId);
            EnhancementSelectionProfile selection = casting.Enhancements.FirstOrDefault(
                value => value.EnhancementId == enhancementId);
            if (selection == null)
                throw new ArgumentException(
                    "Enhancement is not configured on this assignment.", "enhancementId");
            selection.Required = required;
            _save(Profile);
        }

        // Assignment-scoped enhancement selection: the same exclusivity and
        // applicability rules as the simple workflow, applied to one child
        // assignment instead of the source's Automatic child.
        public void SetAssignmentEnhancement(string routineId, string sourceId,
            string assignmentId, string enhancementId)
        {
            SetupSourceRow source = Sources.FirstOrDefault(value => value.SourceId == sourceId);
            if (source == null) throw new ArgumentException("Unknown source.", "sourceId");
            FindRoutine(routineId);
            CastingAssignmentProfile casting = FindCastingAssignment(
                routineId, sourceId, assignmentId);
            // Removal never requires the enhancement to be available again;
            // only adding a new selection checks current applicability.
            bool isRemoval = casting.Enhancements.Any(selection =>
                selection.EnhancementId == enhancementId);
            if (!isRemoval && !string.IsNullOrWhiteSpace(enhancementId) &&
                !GetApplicableEnhancements()
                    .Any(value => value.EnhancementId == enhancementId))
                throw new InvalidOperationException(
                    "The enhancement is not currently applicable and available.");
            var selected = new List<string>(casting.Enhancements
                .Select(selection => selection.EnhancementId));
            if (string.IsNullOrWhiteSpace(enhancementId)) selected.Clear();
            else if (selected.Contains(enhancementId)) selected.Remove(enhancementId);
            else
            {
                CastEnhancementSnapshot next = GetEnhancement(enhancementId);
                selected.RemoveAll(id =>
                {
                    CastEnhancementSnapshot current = GetEnhancement(id);
                    return current != null && current.ExclusiveGroupId == next.ExclusiveGroupId;
                });
                selected.Add(enhancementId);
                if (!CastEnhancementSnapshot.AreCompatible(selected.Select(GetEnhancement)))
                    throw new InvalidOperationException(
                        "The selected enhancement combination is incompatible.");
            }
            casting.Enhancements = selected.Distinct(StringComparer.Ordinal)
                .Select(id => new EnhancementSelectionProfile
                {
                    EnhancementId = id,
                    Required = casting.Enhancements
                        .Where(existing => existing.EnhancementId == id)
                        .Select(existing => existing.Required)
                        .DefaultIfEmpty(true).First()
                }).ToList();
            _save(Profile);
        }

        // Editor-side target addition/removal for one child assignment.
        // Legal targets toggle in; removal always works, including for
        // targets that have become invalid or vanished from the party.
        public void ToggleAssignmentTarget(string routineId, string sourceId,
            string assignmentId, string unitId)
        {
            SetupSourceRow source = Sources.FirstOrDefault(value => value.SourceId == sourceId);
            if (source == null) throw new ArgumentException("Unknown source.", "sourceId");
            RoutineProfile routine = FindRoutine(routineId);
            CastingAssignmentProfile casting = FindCastingAssignment(
                routineId, sourceId, assignmentId);
            if (casting.TargetUnitIds.Contains(unitId))
            {
                casting.TargetUnitIds.Remove(unitId);
            }
            else
            {
                if (!Snapshot.Units.Any(unit => unit.UnitId == unitId))
                    throw new ArgumentException("Unknown unit.", "unitId");
                if (!IsTargetLegal(source, routineId, unitId))
                    throw new InvalidOperationException(
                        "The selected buff cannot target this character.");
                // Never duplicate across siblings of the same source.
                SourceAssignmentProfile parent = routine.Assignments
                    .First(value => value.SourceId == sourceId);
                foreach (CastingAssignmentProfile sibling in parent.CastingAssignments
                    .Where(child => child != casting && child.TargetUnitIds.Contains(unitId))
                    .ToList())
                    sibling.TargetUnitIds.Remove(unitId);
                casting.TargetUnitIds.Add(unitId);
            }
            PruneEmptyAssignment(routine, routine.Assignments
                .First(value => value.SourceId == sourceId));
            _save(Profile);
        }

        public IReadOnlyList<string> GetAssignmentEnhancementIds(string routineId,
            string sourceId, string assignmentId)
        {
            CastingAssignmentProfile casting = FindCastingAssignment(
                routineId, sourceId, assignmentId);
            return new ReadOnlyCollection<string>(casting.Enhancements
                .Select(selection => selection.EnhancementId)
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
        }

        // Cycles Automatic -> each party candidate -> Automatic. Pinning never
        // rewrites targets or enhancements; an unavailable pin stays visible
        // and unresolved.
        public void CycleCastingAssignmentCaster(string routineId, string sourceId,
            string assignmentId)
        {
            RoutineProfile routine = FindRoutine(routineId);
            SetupSourceRow source = Sources.FirstOrDefault(value => value.SourceId == sourceId);
            if (source == null) throw new ArgumentException("Unknown source.", "sourceId");
            CastingAssignmentProfile casting = FindCastingAssignment(
                routineId, sourceId, assignmentId);
            List<string> candidates = Snapshot.Units
                .Where(unit => source.Providers.Any(provider =>
                    provider.Key.CasterUnitId == unit.UnitId))
                .Select(unit => unit.UnitId)
                .OrderBy(unitId => unitId, StringComparer.Ordinal).ToList();
            string current = casting.CasterUnitId;
            string next = null;
            if (current == null) next = candidates.Count == 0 ? null : candidates[0];
            else
            {
                int index = candidates.IndexOf(current);
                next = index < 0 || index + 1 >= candidates.Count ? null : candidates[index + 1];
            }
            casting.CasterUnitId = next;
            casting.SpellbookGuid = null;
            casting.ProviderKey = null;
            _save(Profile);
        }

        public void RemoveCastingAssignment(string routineId, string sourceId,
            string assignmentId)
        {
            RoutineProfile routine = FindRoutine(routineId);
            SourceAssignmentProfile parent = routine.Assignments
                .FirstOrDefault(value => value.SourceId == sourceId);
            CastingAssignmentProfile casting = FindCastingAssignment(
                routineId, sourceId, assignmentId);
            parent.CastingAssignments.Remove(casting);
            if (parent.CastingAssignments.Count == 0)
                routine.Assignments.Remove(parent);
            _save(Profile);
        }

        // Reordering swaps explicit order values; catalog sorting, filtering,
        // and reopening cannot influence these numbers.
        public void MoveCastingAssignmentEarlier(string routineId, string assignmentId)
        {
            MoveCastingAssignment(routineId, assignmentId, -1);
        }

        public void MoveCastingAssignmentLater(string routineId, string assignmentId)
        {
            MoveCastingAssignment(routineId, assignmentId, 1);
        }

        private void MoveCastingAssignment(string routineId, string assignmentId, int delta)
        {
            RoutineProfile routine = FindRoutine(routineId);
            List<CastingAssignmentProfile> ordered = GetRoutineCastingOrder(routineId).ToList();
            int index = ordered.FindIndex(child => child.AssignmentId == assignmentId);
            if (index < 0) throw new ArgumentException("Unknown casting assignment.", "assignmentId");
            int target = index + delta;
            if (target < 0 || target >= ordered.Count) return;
            CastingAssignmentProfile moved = ordered[index];
            ordered.RemoveAt(index);
            ordered.Insert(target, moved);
            int order = 0;
            foreach (CastingAssignmentProfile child in ordered) child.Order = order++;
            _save(Profile);
        }

        private static string NextManualAssignmentId(RoutineProfile routine)
        {
            var used = new HashSet<string>(routine.Assignments.SelectMany(
                assignment => assignment.CastingAssignments.Select(child => child.AssignmentId)),
                StringComparer.Ordinal);
            int index = 1;
            while (used.Contains("cast-" + index)) index++;
            return "cast-" + index;
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
            return GetProviderTemporaryUnavailableReason(provider);
        }

        public string GetProviderTemporaryUnavailableReason(ProviderSnapshot provider)
        {
            if (provider == null) throw new ArgumentNullException("provider");
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

        private void MoveProvider(string providerKey, int delta)
        {
            SetupSourceRow source = RequireSelectedProvider(providerKey);
            List<ProviderSnapshot> ordered = GetOrderedProviders(source).ToList();
            int current = ordered.FindIndex(provider =>
                provider.Key.Canonical == providerKey);
            int next = Math.Max(0, Math.Min(ordered.Count - 1, current + delta));
            if (current < 0 || next == current) return;
            ProviderSnapshot moved = ordered[current];
            ordered.RemoveAt(current);
            ordered.Insert(next, moved);
            for (int index = 0; index < ordered.Count; index++)
                GetOrCreateProviderPreference(
                    ordered[index].Key.Canonical).Priority = index;
            _save(Profile);
        }

        private ProviderPreferenceProfile GetOrCreateProviderPreference(string providerKey)
        {
            ProviderPreferenceProfile preference = GetProviderPreference(providerKey);
            if (preference != null) return preference;
            preference = new ProviderPreferenceProfile
            {
                ProviderKey = providerKey,
                Banned = false,
                Priority = null,
                MaximumCasts = null
            };
            Profile.ProviderPreferences.Add(preference);
            return preference;
        }

        private void RemoveAutomaticPreference(ProviderPreferenceProfile preference)
        {
            if (preference != null && !preference.Banned &&
                preference.Priority == null && preference.MaximumCasts == null)
                Profile.ProviderPreferences.Remove(preference);
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
                ExistingEffectPolicy = Profile.Execution.RecastExisting
                    ? ExistingEffectPolicy.Overwrite : ExistingEffectPolicy.SkipAlreadyActive,
                IgnoredPresenceMarkers = new List<string>(),
                CastingAssignments = new List<CastingAssignmentProfile>()
            };
        }

        // The simple workflow's single Automatic child. This is the only
        // writable copy of the simple interface's state; the source-level
        // summaries read from the same children.
        private CastingAssignmentProfile AutomaticChild(RoutineProfile routine,
            SourceAssignmentProfile assignment, SetupSourceRow source)
        {
            if (assignment == null)
            {
                assignment = CreateAssignment(source);
                routine.Assignments.Add(assignment);
            }
            CastingAssignmentProfile automatic = assignment.AutomaticAssignment;
            if (automatic == null)
            {
                automatic = CastingAssignmentProfile.CreateAutomatic(
                    NextAssignmentId(routine, source.SourceId), NextOrder(routine));
                assignment.CastingAssignments.Add(automatic);
            }
            return automatic;
        }

        private static string NextAssignmentId(RoutineProfile routine, string sourceId)
        {
            // The original automatic child keeps the plain id; if it was
            // pinned or another child already claimed it, later automatic
            // children get a stable numeric suffix so ids stay unique.
            var used = new HashSet<string>(routine.Assignments.SelectMany(
                assignment => assignment.CastingAssignments.Select(child => child.AssignmentId)),
                StringComparer.Ordinal);
            string candidate = "auto-" + sourceId;
            int suffix = 2;
            while (used.Contains(candidate)) candidate = "auto-" + sourceId + "-" + suffix++;
            return candidate;
        }

        private static int NextOrder(RoutineProfile routine)
        {
            int max = -1;
            foreach (int order in routine.Assignments.SelectMany(
                assignment => assignment.CastingAssignments.Select(child => child.Order)))
                if (order > max) max = order;
            return max + 1;
        }

        // An assignment with no configured intent is removed; a parent source
        // with no children goes with it. Nothing with targets, pins, or
        // enhancements is ever removed automatically.
        private static void PruneEmptyAssignment(RoutineProfile routine,
            SourceAssignmentProfile assignment)
        {
            if (assignment == null) return;
            foreach (CastingAssignmentProfile child in assignment.CastingAssignments
                .Where(value => value.IsAutomatic && value.TargetUnitIds.Count == 0 &&
                    value.Enhancements.Count == 0).ToList())
                assignment.CastingAssignments.Remove(child);
            if (assignment.CastingAssignments.Count == 0)
                routine.Assignments.Remove(assignment);
        }

        private static string AggregateId(AbilityKey ability,
            IDictionary<string, EffectExpression> effects)
        {
            EffectExpression expression;
            effects.TryGetValue(ability.Canonical, out expression);
            return CatalogSourceIdentity.For(ability, expression);
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
                representative.SourceDisplayName,
                values.Min(provider => provider.VariantOrder),
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
                bool routineChanged = false;
                var rebound = new List<SourceAssignmentProfile>();
                var aggregateAssignments = new Dictionary<string, SourceAssignmentProfile>(StringComparer.Ordinal);
                foreach (SourceAssignmentProfile assignment in routine.Assignments)
                {
                    SetupSourceRow source;
                    if (!sourceByLegacyId.TryGetValue(assignment.SourceId, out source))
                        source = Sources.FirstOrDefault(item => item.SourceId == assignment.SourceId);
                    if (source == null)
                        source = ResolveUnambiguousVariant(assignment);
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
                        {
                            changed = true;
                            routineChanged = true;
                        }
                        assignment.SourceId = source.SourceId;
                        assignment.Ability = AbilityKeyProfile.FromKey(source.Ability);
                        aggregateAssignments.Add(source.SourceId, assignment);
                        rebound.Add(assignment);
                        continue;
                    }
                    // Duplicate sources consolidate by merging their child
                    // casting assignments; child identities and relative
                    // orders survive so no configured cast is regenerated.
                    existing.CastingAssignments.AddRange(assignment.CastingAssignments);
                    existing.IgnoredPresenceMarkers = existing.IgnoredPresenceMarkers
                        .Concat(assignment.IgnoredPresenceMarkers).Distinct(StringComparer.Ordinal)
                        .OrderBy(item => item, StringComparer.Ordinal).ToList();
                    if (assignment.ExistingEffectPolicy == ExistingEffectPolicy.Overwrite)
                        existing.ExistingEffectPolicy = ExistingEffectPolicy.Overwrite;
                    changed = true;
                    routineChanged = true;
                }
                if (routineChanged)
                {
                    RenumberAssignmentOrders(routine);
                    routine.Assignments = rebound;
                }
            }
            return changed;
        }

        // Re-establishes a unique routine-wide total order after merges while
        // preserving the previous relative order wherever it was unambiguous.
        private static void RenumberAssignmentOrders(RoutineProfile routine)
        {
            int order = 0;
            foreach (CastingAssignmentProfile casting in routine.Assignments
                .SelectMany(assignment => assignment.CastingAssignments)
                .OrderBy(child => child.Order)
                .ThenBy(child => child.AssignmentId, StringComparer.Ordinal))
                casting.Order = order++;
        }

        private SetupSourceRow ResolveUnambiguousVariant(SourceAssignmentProfile assignment)
        {
            AbilityKey persisted = assignment.Ability.ToKey();
            if (!string.IsNullOrWhiteSpace(persisted.VariantGuid)) return null;
            List<SetupSourceRow> candidates = VariantCandidates(persisted);
            return candidates.Count == 1 ? candidates[0] : null;
        }

        private IReadOnlyList<VariantReselectionNotice> BuildVariantReselectionNotices()
        {
            var notices = new List<VariantReselectionNotice>();
            var supported = new HashSet<string>(Sources.Select(source => source.SourceId),
                StringComparer.Ordinal);
            foreach (RoutineProfile routine in Profile.Routines)
            {
                foreach (SourceAssignmentProfile assignment in routine.Assignments.Where(value =>
                    !supported.Contains(value.SourceId)))
                {
                    AbilityKey persisted = assignment.Ability.ToKey();
                    List<SetupSourceRow> candidates = VariantCandidates(persisted);
                    if (string.IsNullOrWhiteSpace(persisted.VariantGuid) &&
                        candidates.Count < 2) continue;
                    string name = candidates.Select(value => value.SourceDisplayName)
                        .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ??
                        persisted.BaseAbilityGuid;
                    notices.Add(new VariantReselectionNotice(
                        routine.RoutineId, assignment.SourceId, name, candidates.Count));
                }
            }
            return new ReadOnlyCollection<VariantReselectionNotice>(notices
                .OrderBy(value => value.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.RoutineId, StringComparer.Ordinal)
                .ThenBy(value => value.SourceId, StringComparer.Ordinal).ToList());
        }

        private List<SetupSourceRow> VariantCandidates(AbilityKey persisted)
        {
            return Sources.Where(source => source.IsConcreteVariant &&
                source.Abilities.Any(ability =>
                    ability.BaseAbilityGuid == persisted.BaseAbilityGuid &&
                    ability.SourceKind == persisted.SourceKind &&
                    ability.MetamagicMask == persisted.MetamagicMask &&
                    ability.SpecialSourceId == persisted.SpecialSourceId))
                .OrderBy(source => source.VariantOrder)
                .ThenBy(source => source.SourceId, StringComparer.Ordinal).ToList();
        }

        private SetupSourceRow RequireSelectedProvider(string providerKey)
        {
            SetupSourceRow source = RequireSelected();
            if (!source.Providers.Any(p => p.Key.Canonical == providerKey))
                throw new ArgumentException("Unknown provider.", "providerKey");
            return source;
        }
    }
}
