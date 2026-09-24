using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Diagnostics;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class PlannerUiSession
    {
        private readonly ProfileRepository _profiles;
        private readonly ModLog _log;
        private readonly EffectOverrideRegistry _overrides;
        private PartyProviderSnapshot _snapshot;
        private ActiveEffectSnapshot _activeEffects;
        private Dictionary<string, EffectExpression> _effects;
        private ProviderPlanningOption[] _providerOptions;
        private CastEnhancementSnapshot[] _enhancements;
        private KingmakerCastEnhancementAdapter _enhancementAdapter;
        private KingmakerShareTargetingModifier _shareTargeting;
        private EffectiveProviderOptionResolver _targeting;

        internal PlannerUiSession(string modPath, ModLog log)
        {
            _profiles = new ProfileRepository(modPath);
            _overrides = EffectOverrideRegistry.Load(
                System.IO.Path.Combine(modPath, "NativeEffectOverrides.json"));
            _log = log;
            Status = "Open a campaign to configure routines.";
        }

        internal PlannerSetupModel Model { get; private set; }
        internal string Status { get; private set; }

        // Final review B5: while casting-first mode is active its refreshes
        // must never rewrite the Classic plan file (the Classic model saves
        // when it rebinds assignments to the party's current abilities). The
        // check runs at save time, so the Classic screen saves again as soon
        // as Classic is the mode.
        internal Func<bool> ClassicSavesSuppressed { get; set; }

        // Final review A1 (re-review): whether the world runs for a Classic
        // run's casting phase; set by the planner root to the casting-first
        // host's own rule (the world runs, and no planner window is open).
        internal Func<bool> ClassicWorldRuns { get; set; }

        private void SaveClassicProfile(BuffPlannerProfile profile)
        {
            Func<bool> suppressed = ClassicSavesSuppressed;
            if (suppressed != null && suppressed())
            {
                _log.Info("[KBP-PROFILE] Classic save skipped: the casting-first planner is active.");
                return;
            }
            _profiles.Save(profile);
            string refusal = _profiles.LastSaveRefusal;
            if (refusal == null)
            {
                // A save that went through ends any earlier notice, and the
                // file is now the bytes of this plan (focused re-review).
                PersistenceNotice = null;
                ClassicSavesRefused = false;
                ClassicPrimarySha256 = ProfileRepository.TryHash(_profiles.GetProfilePath(profile.CampaignId));
                return;
            }
            ClassicSavesRefused = true;
            PersistenceNotice = PersistenceMessages.ForClassicSaveRefusal(refusal);
            _log.Info("[KBP-PROFILE] Classic save refused: " + refusal + ".");
        }
        internal bool IsExecuting { get; private set; }
        internal RoutinePlanResult LastPreview { get; private set; }
        // Routine identity of LastPreview; the material-change gate only
        // compares a confirmation baseline for the same routine.
        internal string LastPreviewRoutineId { get; private set; }
        private readonly PlannerReviewCoordinator _review = new PlannerReviewCoordinator();
        internal ExecutionReport LastExecutionReport { get; private set; }
        internal string ProfileStatus { get; private set; }
        // Final review B4: the saved Classic setup could not be read, a backup
        // was loaded instead, or a change was not saved; null when all is
        // well. The Classic screen always shows it.
        internal string PersistenceNotice { get; private set; }
        // Focused re-review: whether Classic saves are refused now (the main
        // file cannot be read, or the last save was refused); other notices
        // do not stop saving.
        internal bool ClassicSavesRefused { get; private set; }
        // The SHA-256 of the main Classic file this plan was read from or
        // last saved to; null when it came from a backup or is a new default.
        internal string ClassicPrimarySha256 { get; private set; }
        // A Classic run that has been accepted but waits for the world to run
        // (a paused game, or a planner window open).
        internal bool ClassicRunHeld
        {
            get
            {
                Func<bool> worldRuns = ClassicWorldRuns;
                return IsExecuting && worldRuns != null && !worldRuns();
            }
        }
        internal PartyCatalogDiscoveryDiagnostics CatalogDiscovery { get; private set; }
        internal IReadOnlyList<ProviderPlanningOption> ProviderOptions
        {
            get { return _providerOptions ?? new ProviderPlanningOption[0]; }
        }
        internal string LastBindingFailure { get; private set; }
        // Live party effects with instance detail from the last refresh
        // (null before a campaign snapshot exists).
        internal ActiveEffectSnapshot ActiveEffects
        {
            get { return _activeEffects; }
        }

        internal void Refresh()
        {
            try
            {
                if (Game.Instance == null || Game.Instance.Player == null ||
                    string.IsNullOrWhiteSpace(Game.Instance.Player.GameId))
                {
                    Model = null;
                    ClassicPrimarySha256 = null;
                    _snapshot = null;
                    _activeEffects = null;
                    _effects = null;
                    _providerOptions = null;
                    _enhancements = null;
                    _enhancementAdapter = null;
                    CatalogDiscovery = null;
                    Status = "No campaign is loaded. Profiles are external and are not created at the main menu.";
                    return;
                }
                string campaignId = Game.Instance.Player.GameId;
                var snapshotBuilder = new KingmakerPartySnapshotBuilder(_overrides);
                PartyProviderSnapshot snapshot = snapshotBuilder.Build();
                var active = new KingmakerActiveEffectSnapshotBuilder().Build();
                var effects = new Dictionary<string, EffectExpression>(
                    snapshotBuilder.EffectsBySource, StringComparer.Ordinal);
                ProfileLoadResult loaded = _profiles.Load(campaignId);
                PlannerHotkey.SetBinding(loaded.Profile.Ui.Hotkey);
                string primaryName = System.IO.Path.GetFileName(_profiles.GetProfilePath(campaignId));
                PersistenceNotice = PersistenceMessages.ForClassicLoad(loaded.SourcePath,
                    loaded.RecoveredFromBackup, loaded.Warning, primaryName);
                ClassicSavesRefused = PersistenceMessages.ClassicPrimaryUnreadable(loaded.Warning, primaryName);
                ClassicPrimarySha256 = loaded.PrimarySha256;
                ProfileStatus = string.IsNullOrEmpty(loaded.SourcePath)
                    ? (string.IsNullOrEmpty(loaded.Warning)
                        ? "No prior profile was found; using a new schema "
                        : "The saved profile could not be read (it is kept unchanged); using a new schema ") +
                        BuffPlannerProfile.CurrentSchemaVersion + " profile."
                    : "Loaded profile " + loaded.SourcePath + "; schema=" +
                        loaded.Profile.SchemaVersion + "; migrated=" + loaded.Migrated +
                        "; recoveredFromBackup=" + loaded.RecoveredFromBackup + ".";
                _log.Info("Profile load: " + ProfileStatus);
                if (!string.IsNullOrEmpty(loaded.Warning))
                    _log.Info("Profile recovery warning: " + loaded.Warning);
                var optionBuilder = new KingmakerProviderOptionBuilder();
                _providerOptions = optionBuilder.Build(snapshot, effects);
                foreach (string diagnostic in optionBuilder.Diagnostics)
                    _log.Info("[KBP-TARGETING-CONTRACT] " + diagnostic + ".");
                string[] persistedEnhancementIds = loaded.Profile.Routines
                    .SelectMany(routine => routine.Assignments)
                    .SelectMany(assignment => assignment.SelectedEnhancementIds ?? new List<string>())
                    .Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToArray();
                _enhancementAdapter = new KingmakerCastEnhancementAdapter();
                _enhancements = _enhancementAdapter.Discover(snapshot,
                    persistedEnhancementIds);
                _shareTargeting = new KingmakerShareTargetingModifier();
                _targeting = new EffectiveProviderOptionResolver(
                    new ICastTargetingModifier[] { _shareTargeting });
                Model = new PlannerSetupModel(loaded.Profile, snapshot, active, effects,
                    _providerOptions, SaveClassicProfile, _enhancements,
                    _targeting);
                if (Model.VariantReselectionNotices.Count != 0)
                {
                    string names = string.Join(", ", Model.VariantReselectionNotices
                        .Select(value => value.DisplayName)
                        .Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
                    ProfileStatus += " Saved selections requiring a concrete variant " +
                        "reselection: " + names + ".";
                    foreach (VariantReselectionNotice notice in
                        Model.VariantReselectionNotices)
                        _log.Info("[KBP-VARIANT-MIGRATION] routine=" + notice.RoutineId +
                            ";source=" + notice.SourceId + ";parent=" +
                            notice.DisplayName + ";candidateCount=" +
                            notice.CandidateCount + ";action=reselection-required.");
                }
                _snapshot = snapshot;
                _activeEffects = active;
                _effects = effects;
                CatalogDiscovery = snapshotBuilder.Diagnostics;
                LastBindingFailure = string.Empty;
                Status = snapshot.Units.Count + " party/pet targets; " +
                    Model.Sources.Count + " discovered buff sources; " +
                    snapshot.Providers.Count + " providers; " +
                    _enhancements.Length + " cast enhancements." +
                    (Model.VariantReselectionNotices.Count == 0 ? string.Empty :
                        " " + Model.VariantReselectionNotices.Count +
                        " saved variant selection(s) require reselection.");
                int rodCount = _enhancements.Count(value => value.Category ==
                    CastEnhancementCategory.MetamagicRod);
                int classFeatureCount = _enhancements.Count(value => value.Category ==
                    CastEnhancementCategory.ClassFeature);
                _log.Info("[KBP-ENHANCEMENT] total=" + _enhancements.Length +
                    ";metamagic-rods=" + rodCount + ";class-features=" +
                    classFeatureCount + ".");
                foreach (string diagnostic in _enhancementAdapter.ContractDiagnostics)
                    _log.Info("[KBP-ENHANCEMENT-CONTRACT] " + diagnostic + ".");
                foreach (CastEnhancementSnapshot enhancement in _enhancements)
                {
                    UnitSnapshot owner = snapshot.Units.FirstOrDefault(unit =>
                        unit.UnitId == enhancement.CasterUnitId);
                    _log.Info("[KBP-ENHANCEMENT-OPTION] id=" + enhancement.EnhancementId +
                        ";name=" + enhancement.DisplayName +
                        ";owner=" + (owner == null ? enhancement.CasterUnitId : owner.DisplayName) +
                        ";effect=" + enhancement.EffectDisplayName +
                        ";remaining=" + (enhancement.RemainingUses == null ? "unlimited" :
                            enhancement.RemainingUses.Value.ToString()) +
                        ";category=" + enhancement.Category +
                        ";usagePool=" + enhancement.UsagePoolId +
                        ";requiresNativeCommand=" + enhancement.RequiresNativeCommand +
                        ";directCastProvider=" +
                            (string.IsNullOrWhiteSpace(
                                enhancement.DirectCastProviderId) ? "none" :
                                enhancement.DirectCastProviderId) +
                        (enhancement.Category == CastEnhancementCategory.MetamagicRod
                            ? ";spellLevelLimit=" + enhancement.MaximumSpellLevel
                            : ";qualifiedAbilities=" + enhancement.AbilityWhiteList.Count) + ".");
                }
                _log.Info("[KBP-CATALOG] discovery;" + CatalogDiscovery + ".");
                foreach (PartyVariantEligibilityTrace trace in CatalogDiscovery.Variants)
                    _log.Info("[KBP-VARIANT-ELIGIBILITY] caster=" + trace.CasterUnitId +
                        ";spellbook=" + trace.SpellbookGuid +
                        ";source=" + trace.SourceGuid +
                        ";child=" + trace.ChildGuid +
                        ";eligible=" + trace.Eligible +
                        ";reason=" + trace.Reason + ".");
                foreach (PartySpellbookRoleTrace trace in CatalogDiscovery.SpellbookRoles)
                    _log.Info("[KBP-SPELLBOOK-ROLE] caster=" + trace.CasterUnitId +
                        ";spellbook=" + trace.SpellbookGuid +
                        ";spontaneous=" + trace.Spontaneous +
                        ";role=" + trace.Role +
                        ";relationship=" + trace.RelationshipTargetGuid +
                        ";included=" + trace.Included +
                        ";reason=" + trace.Reason + ".");
                LogBlessSlice(loaded.Profile, snapshot, _providerOptions, Model);
            }
            catch (Exception exception)
            {
                Model = null;
                _enhancementAdapter = null;
                CatalogDiscovery = null;
                Status = "Setup refresh failed: " + exception.Message;
                _log.Error("Planner UI refresh failed.", exception);
            }
        }

        internal void RecordBindingFailure(string stage, Exception exception)
        {
            LastBindingFailure = (stage ?? "binding") + ": " +
                (exception == null ? "unknown failure" : exception.Message);
            Status = "Catalog UI binding failed at " + LastBindingFailure;
            _log.Error("[KBP-CATALOG] " + Status, exception ??
                new InvalidOperationException(LastBindingFailure));
        }

        private void LogBlessSlice(
            BuffPlannerProfile profile,
            PartyProviderSnapshot snapshot,
            IEnumerable<ProviderPlanningOption> options,
            PlannerSetupModel model)
        {
            const string bless = "90e59f4a4ada87243b7b3535a06d0638";
            ProviderSnapshot provider = snapshot.Providers.FirstOrDefault(item =>
                item.Key.Ability.BaseAbilityGuid == bless || item.Key.Ability.VariantGuid == bless);
            if (provider == null)
            {
                PartySourceDiscoveryTrace excluded = CatalogDiscovery == null ? null :
                    CatalogDiscovery.Sources.FirstOrDefault(item => item.BlueprintGuid == bless);
                _log.Info("[KBP-CATALOG] Bless;present=false;classification=" +
                    (excluded == null ? "not-enumerated" : excluded.Reason) + ".");
                return;
            }
            ResourcePoolSnapshot pool = snapshot.ResourcePools.First(item =>
                item.PoolKey == provider.ResourcePoolKey);
            ProviderPlanningOption option = (options ?? new ProviderPlanningOption[0])
                .FirstOrDefault(item => item.Provider.Key.Equals(provider.Key));
            SetupSourceRow source = model == null ? null : model.Sources.FirstOrDefault(item =>
                item.Abilities.Any(ability => ability.Equals(provider.Key.Ability)));
            string assignmentId = source == null ? provider.Key.Ability.Canonical : source.SourceId;
            SourceAssignmentProfile assignment = profile.Routines.SelectMany(item => item.Assignments)
                .FirstOrDefault(item => item.SourceId == assignmentId);
            int availableTokens = pool.Tokens.Count(item => item.Available && item.IsPrimary &&
                provider.EligibleTokenIds.Contains(item.TokenId));
            _log.Info("[KBP-CATALOG] Bless;present=true;source=" +
                provider.Key.Ability.Canonical + ";blueprint=" + bless + ";spellbook=" +
                provider.Key.SpellbookGuid + ";provider=" + provider.Key.Canonical +
                ";pool=" + pool.Kind + ";eligibleTokens=" + provider.EligibleTokenIds.Count +
                ";availableTokens=" + availableTokens + ";remaining=" + pool.Remaining +
                ";durationRounds=" + provider.ExpectedDurationRounds + ";legalTargets=" +
                (option == null ? 0 : option.ReachableTargetIds.Count) + ";assigned=" +
                (assignment != null) + ";savedTargets=" +
                (assignment == null ? 0 : assignment.WantedTargetUnitIds.Count) +
                ";material=" + (CatalogDiscovery == null ? "missing" :
                    CatalogDiscovery.BlessMaterialEvidence) + ".");
        }

        internal void RecordEnhancementUiEvidence(string evidence)
        {
            if (!string.IsNullOrWhiteSpace(evidence))
                _log.Info("[KBP-ENHANCEMENT-UI] " + evidence);
            if (_enhancementAdapter == null || Model == null ||
                Model.SelectedSource == null) return;
            foreach (ProviderSnapshot provider in Model.SelectedSource.Providers
                .OrderBy(value => value.Key.Canonical, StringComparer.Ordinal))
            {
                string trace = _enhancementAdapter.Describe(provider,
                    _enhancements);
                if (!string.IsNullOrWhiteSpace(trace)) _log.Info(trace);
            }
        }
        internal RoutinePlanResult PreviewRoutine(string routineId)
        {
            if (Model == null || _snapshot == null || _activeEffects == null ||
                _effects == null || _providerOptions == null)
                throw new InvalidOperationException("A campaign planner snapshot is required.");
            LastPreview = new RoutinePlanService().Plan(Model.Profile, routineId, _snapshot,
                _activeEffects, _effects, _providerOptions, _enhancements,
                _targeting);
            LastPreviewRoutineId = routineId;
            // Computing a preview NEVER acknowledges review. Resource
            // inspection, forecasts, and preflight computation all route
            // through here; acknowledgment happens only when the planner
            // view binds the exact visible plan to the player and calls
            // AcknowledgeDisplayedPlan for that routine.
            _review.PlanComputed(Model.Profile.CampaignId, routineId);
            if (_shareTargeting != null)
                foreach (string diagnostic in _shareTargeting.DrainDiagnostics())
                    _log.Info("[KBP-SHARE-TARGETING] " + diagnostic + ".");
            return LastPreview;
        }

        // Current-routine resource lines with competing configured demand from
        // the other routines. Competing lines are demand, not reservations:
        // every routine plan is computed against the same live snapshot.
        internal IReadOnlyList<ResourceUsageLineViewModel> GetResourceUsageLines(string routineId)
        {
            if (Model == null) throw new InvalidOperationException("A campaign planner snapshot is required.");
            RoutinePlanResult current = PreviewRoutine(routineId);
            var competing = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (RoutineProfile routine in Model.Profile.Routines.Where(r =>
                r.RoutineId != routineId && r.Assignments.Count != 0))
            {
                RoutinePlanResult other;
                try { other = PreviewRoutine(routine.RoutineId); }
                catch { continue; }
                foreach (ResourcePoolAllocation allocation in other.Plan.ResourceAllocations)
                {
                    if (allocation.RequestedUsage == 0 && allocation.AllocatedUsage == 0) continue;
                    List<string> names;
                    if (!competing.TryGetValue(allocation.PoolKey, out names))
                        competing[allocation.PoolKey] = names = new List<string>();
                    names.Add(routine.Name);
                }
            }
            var lines = new List<ResourceUsageLineViewModel>();
            string routineName = RoutineDisplayName(routineId);
            foreach (ResourcePoolAllocation allocation in current.Plan.ResourceAllocations)
            {
                List<string> names;
                competing.TryGetValue(allocation.PoolKey, out names);
                lines.Add(new ResourceUsageLineViewModel(allocation,
                    ResourcePoolDisplayName(allocation.PoolKey), routineName,
                    new List<string>((names ?? new List<string>())
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(value => value, StringComparer.Ordinal).ToList())));
            }
            return lines;
        }

        // The planner screen calls this after binding a specific routine's
        // preview into its visible controls — the only acknowledgment of
        // review. The previously open-plan flag never established that the
        // player saw this exact plan.
        internal void AcknowledgeDisplayedPlan(string routineId)
        {
            if (Model == null || LastPreview == null ||
                !string.Equals(LastPreviewRoutineId, routineId,
                    StringComparison.Ordinal))
                return;
            _review.Presented(Model.Profile.CampaignId, routineId,
                LastPreview.Plan);
        }

        // Read-only combined forecast: each selected routine occurrence is
        // planned by the production planner against the balances, tokens,
        // materials, charges, and projected effects carried forward from the
        // previous occurrence, in the caller's explicit order. This must not
        // mutate the reviewed-plan state, so the shared LastPreview baseline
        // is captured and restored around the computation.
        internal SequentialForecastPlanner.Result ForecastSequence(
            IReadOnlyList<string> routineIdsInOrder)
        {
            if (routineIdsInOrder == null) throw new ArgumentNullException("routineIdsInOrder");
            if (Model == null || _snapshot == null || _activeEffects == null ||
                _effects == null || _providerOptions == null)
                throw new InvalidOperationException("A campaign planner snapshot is required.");
            RoutinePlanResult previousPreview = LastPreview;
            string previousRoutineId = LastPreviewRoutineId;
            try
            {
                return SequentialForecastPlanner.Compute(Model.Profile,
                    routineIdsInOrder, _snapshot, _activeEffects, _effects,
                    _providerOptions, _enhancements, _targeting);
            }
            finally
            {
                LastPreview = previousPreview;
                LastPreviewRoutineId = previousRoutineId;
            }
        }

        private string ResourcePoolDisplayName(string allocationPoolKey)
        {
            const string enhancementPrefix = "enhancement:";
            if (allocationPoolKey != null &&
                allocationPoolKey.StartsWith(enhancementPrefix, StringComparison.Ordinal))
            {
                string usagePool = allocationPoolKey.Substring(enhancementPrefix.Length);
                CastEnhancementSnapshot enhancement = _enhancements == null ? null :
                    _enhancements.FirstOrDefault(value =>
                        value.UsagePoolId == usagePool);
                return enhancement == null ? usagePool :
                    enhancement.UsagePoolDisplayName + " (" + enhancement.EffectDisplayName + ")";
            }
            ResourcePoolSnapshot pool = _snapshot == null ? null :
                _snapshot.ResourcePools.FirstOrDefault(value => value.PoolKey == allocationPoolKey);
            return pool == null ? allocationPoolKey : pool.PoolKey;
        }

        internal IReadOnlyList<CastingAssignmentRowViewModel> GetCastingOrderRows(string routineId)
        {
            if (Model == null) throw new InvalidOperationException("A campaign planner snapshot is required.");
            RoutinePlanResult preview = PreviewRoutine(routineId);
            return CastingAssignmentRowViewModel.CreateRoutineRows(
                Model.Profile, routineId,
                sourceId =>
                {
                    SetupSourceRow source = Model.Sources.FirstOrDefault(value => value.SourceId == sourceId);
                    return source == null ? sourceId : source.DisplayName;
                },
                unitId =>
                {
                    UnitSnapshot unit = Model.Snapshot.Units.FirstOrDefault(value => value.UnitId == unitId);
                    return unit == null ? unitId : unit.DisplayName;
                },
                enhancementId =>
                {
                    CastEnhancementSnapshot enhancement = Model.GetEnhancement(enhancementId);
                    return enhancement == null ? enhancementId : enhancement.DisplayName;
                },
                preview.Plan,
                Model.SelectedSourceId,
                casterUnitId =>
                {
                    UnitSnapshot unit = Model.Snapshot.Units.FirstOrDefault(value => value.UnitId == casterUnitId);
                    return unit == null ? casterUnitId : unit.DisplayName;
                });
        }

        internal IEnumerator ExecuteRoutine(string routineId)
        {
            return ExecuteRoutine(routineId, null, false);
        }

        internal IEnumerator ExecuteRoutine(string routineId, Action<QuickExecutionResult> completed)
        {
            return ExecuteRoutine(routineId, completed, false);
        }

        // readyOnlyExplicit is the mission's explicit "Apply Ready Casts Only"
        // path. Without it, an incomplete routine (any unmet target) is never
        // silently executed as its ready subset — the HUD quick-run shares
        // this exact rule through the same gate.
        internal IEnumerator ExecuteRoutine(string routineId,
            Action<QuickExecutionResult> completed, bool readyOnlyExplicit)
        {
            string routineName = RoutineDisplayName(routineId);
            _log.Info("[KBP-QUICK] pointer/listener accepted;group=" + routineId + ".");
            if (IsExecuting)
            {
                Complete(completed, new QuickExecutionResult(routineId, routineName,
                    QuickExecutionDisposition.Refused,
                    "Another buff routine is already executing.", 0, 0, 0));
                yield break;
            }
            Refresh();
            _log.Info("[KBP-QUICK] profile refreshed;group=" + routineId +
                ";model=" + (Model != null) + ";profile=" + (ProfileStatus ?? string.Empty) + ".");
            if (Model == null)
            {
                string unavailable = "Cannot run " + routineName + ": " + Status;
                Complete(completed, new QuickExecutionResult(routineId, routineName,
                    QuickExecutionDisposition.Refused, unavailable, 0, 0, 0));
                _log.Info("[KBP-QUICK] deliberately refused;group=" + routineId +
                    ";reason=" + unavailable + ".");
                yield break;
            }
            // The acknowledged review baseline gates execution: after the
            // refresh below, a materially different fresh plan (caster/item,
            // anchor, targets, recipients, enhancement omissions, full cost
            // vector, order, coverage) requires renewed review instead of
            // silent execution. Only an explicitly acknowledged plan counts.
            CastPlan reviewedPlan = _review.BaselineFor(
                Model.Profile.CampaignId, routineId);
            RoutineProfile configuredRoutine = Model.Profile.Routines.First(r =>
                r.RoutineId == routineId);
            _log.Info("[KBP-QUICK] assignments resolved;group=" + routineId +
                ";assignments=" + configuredRoutine.Assignments.Count + ".");
            string variantReselection = VariantReselectionSummary(
                Model, routineId);
            RoutinePlanResult preview;
            try
            {
                preview = PreviewRoutine(routineId);
            }
            catch (Exception exception)
            {
                Status = "Routine preview failed: " + exception.Message;
                _log.Error("Routine preview failed.", exception);
                Complete(completed, new QuickExecutionResult(routineId, routineName,
                    QuickExecutionDisposition.Failed, Status, 0, 0, 0));
                yield break;
            }
            _log.Info("[KBP-QUICK] plan refreshed and validation completed;group=" + routineId +
                ";steps=" + preview.Plan.Steps.Count + ";outcomes=" +
                preview.Plan.Outcomes.Count + ";unsupported=" +
                preview.UnsupportedSourceIds.Count + ".");
            foreach (string diagnostic in preview.Plan.Diagnostics)
                _log.Info("[KBP-PLAN-DIAGNOSTIC] group=" + routineId + ";" +
                    diagnostic + ".");
            RoutineProfile routine = configuredRoutine;
            if (routine.Assignments.Count == 0)
            {
                Status = "No " + routineName + " buffs are configured.";
                LastExecutionReport = new ExecutionReport(preview.Plan);
                Complete(completed, new QuickExecutionResult(routineId, routineName,
                    QuickExecutionDisposition.Refused, Status, 0, 0, 0));
                _log.Info("[KBP-QUICK] deliberately refused;group=" + routineId +
                    ";reason=" + Status + ".");
                yield break;
            }
            if (preview.Plan.Steps.Count == 0)
            {
                LastExecutionReport = new ExecutionReport(preview.Plan);
                int skipped = preview.Plan.Outcomes.Count(outcome =>
                    outcome.Kind == TargetOutcomeKind.SkippedAlreadyActive);
                int unfulfilled = preview.Plan.Outcomes.Count(outcome =>
                    outcome.Kind == TargetOutcomeKind.Unfulfilled);
                Status = "No " + routineName + " casts can run: skipped active=" + skipped +
                    "; unfulfilled=" + unfulfilled + "." + variantReselection;
                Complete(completed, new QuickExecutionResult(routineId, routineName,
                    QuickExecutionDisposition.Refused, Status, 0, 0, 0));
                _log.Info("[KBP-QUICK] deliberately refused;group=" + routineId +
                    ";reason=" + Status + ".");
                yield break;
            }
            string materialChange = reviewedPlan != null
                ? PlanMaterialChangeDetector.DescribeMaterialChange(
                    reviewedPlan, preview.Plan)
                : null;
            if (materialChange != null)
            {
                LastExecutionReport = new ExecutionReport(preview.Plan);
                Status = "The plan changed since your last review (" + materialChange +
                    "). Review the updated preview before applying.";
                Complete(completed, new QuickExecutionResult(routineId, routineName,
                    QuickExecutionDisposition.Refused, Status,
                    preview.Plan.Steps.Count, 0, 0));
                _log.Info("[KBP-QUICK] material-change gate refused;group=" + routineId +
                    ";change=" + materialChange + ".");
                yield break;
            }
            PartialExecutionGate.Decision gate = PartialExecutionGate.Evaluate(preview.Plan);
            if (gate.Blocked && !readyOnlyExplicit)
            {
                LastExecutionReport = new ExecutionReport(preview.Plan);
                Status = gate.Summary + " Apply blocked to avoid running only part of " +
                    routineName + "; use Apply Ready Casts Only to run the ready subset.";
                Complete(completed, new QuickExecutionResult(routineId, routineName,
                    QuickExecutionDisposition.Refused, Status,
                    gate.PlannedCasts, 0, 0));
                _log.Info("[KBP-QUICK] partial-apply gate refused;group=" + routineId +
                    ";requested=" + gate.RequestedTargets + ";unfulfilled=" +
                    gate.Unfulfilled + ".");
                yield break;
            }
            if (readyOnlyExplicit)
            {
                _log.Info("[KBP-QUICK] explicit ready-only execution;group=" + routineId +
                    ";requested=" + gate.RequestedTargets + ";planned=" + gate.PlannedCasts +
                    ";unfulfilled=" + gate.Unfulfilled + ";skipped=" + gate.SkippedActive + ".");
            }
            // An automation session never casts through the classic routes
            // either (NativeCastingSessionPolicy); the plan was still built
            // and gated above, so the refusal is the only difference. The
            // one exception is an allowance-bound classic cast run's
            // single-use grant for exactly this plan, routine and mode.
            string grantRefusal = null;
            if (NativeCastingSessionPolicy.Locked &&
                !NativeCastingSessionPolicy.TryConsumeClassicGrant(routineId,
                    Execution.ClassicPlanDigest.Of(preview.Plan), Model.Profile.Execution.Mode,
                    preview.Plan.Steps.Count, out grantRefusal))
            {
                LastExecutionReport = new ExecutionReport(preview.Plan);
                Status = routineName + " was not cast: native casting is disabled in this " +
                    "automated test session.";
                Complete(completed, new QuickExecutionResult(routineId, routineName,
                    QuickExecutionDisposition.Refused, Status, preview.Plan.Steps.Count, 0, 0));
                _log.Info("[KBP-QUICK] runtime-test lock refused;group=" + routineId +
                    ";reason=" + NativeCastingSessionPolicy.LockReason + ";grant=" + grantRefusal + ".");
                yield break;
            }
            if (NativeCastingSessionPolicy.Locked)
                _log.Info("[KBP-QUICK] classic grant consumed;group=" + routineId + ";" +
                    NativeCastingSessionPolicy.ClassicGrant.Describe() + ".");
            // The native state is about to change by design; the reviewed
            // baseline for this routine is spent with it.
            _review.Spent(routineId);
            LastExecutionReport = new ExecutionReport(preview.Plan);
            ICastExecutor executor;
            var fallbackWarnings = new HashSet<string>(StringComparer.Ordinal);
            if (Model.Profile.Execution.Mode == "instant")
            {
                CastEnhancementSnapshot[] executionEnhancements = _enhancements;
                var nativeEnhancements = new HashSet<string>(executionEnhancements
                    .Where(value => value.RequiresNativeCommand)
                    .Select(value => value.EnhancementId), StringComparer.Ordinal);
                string providerVersion;
                bool directCapable;
                string directReason;
                ShareCastDiagnostics.Capture(_log.Info, out providerVersion,
                    out directCapable, out directReason);
                executor = new HybridCastExecutor(
                    new KingmakerInstantCastAdapter(_log.Info), new KingmakerAnimatedCastAdapter(),
                    Model.Profile.Execution.AllowAnimatedFallback,
                    Model.Profile.Execution.OutOfCombatOnly,
                    step => step.EnhancementIds.Any(nativeEnhancements.Contains),
                    (index, step, animated, route) =>
                    {
                        CastEnhancementSnapshot[] selected = executionEnhancements.Where(value =>
                            step.EnhancementIds.Contains(value.EnhancementId)).ToArray();
                        _log.Info("[KBP-ROUTE] group=" + routineId + ";step=" + index +
                            ";provider=" + step.Provider.Canonical + ";source=" + step.SourceId +
                            ";targets=" + string.Join(",", step.TargetUnitIds.ToArray()) +
                            ";selected-enhancements=" + string.Join(",", step.EnhancementIds.ToArray()) +
                            ";native-enhancements=" + string.Join(",", selected.Where(value =>
                                value.RequiresNativeCommand).Select(value => value.EnhancementId).ToArray()) +
                            ";direct-providers=" + string.Join(",", selected.Select(value =>
                                value.DirectCastProviderId).ToArray()) + ";" + route);
                        if (!animated) return;
                        bool share = selected.Any(value => value.AffectsTargeting);
                        string cause = share && !directCapable
                            ? "Gunslinger " + providerVersion +
                                " has no compatible Instant Share capability (" + directReason + ")."
                            : selected.Any(value => value.RequiresNativeCommand)
                                ? string.Join(", ", selected.Where(value => value.RequiresNativeCommand)
                                    .Select(value => value.DisplayName).ToArray()) +
                                    " requires native animated casting."
                                : step.ExecutionStrategyReason;
                        string warning = "Warning: " + (share ? "Share Transmutation" : "This cast") +
                            " is using animated casting in Instant mode. " + cause;
                        fallbackWarnings.Add(warning);
                        Status = warning;
                        _log.Info("[KBP-INSTANT-FALLBACK] " + warning);
                    });
            }
            else executor = new AnimatedCastExecutor(new KingmakerAnimatedCastAdapter(),
                Model.Profile.Execution.OutOfCombatOnly);
            IsExecuting = true;
            _log.Info("[KBP-QUICK] execution invoked;group=" + routineId +
                ";mode=" + Model.Profile.Execution.Mode + ";steps=" +
                preview.Plan.Steps.Count + ".");
            Status = "Executing " + routineId + " routine: " + preview.Plan.Steps.Count + " planned casts.";
            _log.Info("Routine plan: " + DescribePlan(preview.Plan));
            // Failures stop later castings (batch 3, section 6): the runner
            // halts after the first step that did not confirm its effect.
            var runner = new HaltingPlanRunner(executor);
            // Final review A1 (re-review): only the casting phase waits for
            // the world. The checks above ran at the press, so a refusal
            // reaches the open screen at once, and IsExecuting now holds the
            // screen and the HUD; a paused game or an open window never uses
            // up a cast's frame-counted confirmation window.
            Func<bool> worldRuns = ClassicWorldRuns ?? (() => true);
            IEnumerator work = new WorldGatedEnumerator(runner.Run(preview.Plan, LastExecutionReport),
                worldRuns);
            Exception failure = null;
            try
            {
                while (true)
                {
                    bool moved = false;
                    object current = null;
                    try
                    {
                        moved = work.MoveNext();
                        if (moved) current = work.Current;
                    }
                    catch (Exception exception)
                    {
                        failure = exception;
                    }
                    if (!moved || failure != null) break;
                    yield return current;
                }
            }
            finally
            {
                IDisposable disposable = work as IDisposable;
                if (disposable != null) disposable.Dispose();
            }
            IsExecuting = false;
            if (failure != null)
            {
                Status = "Routine execution failed: " + failure.Message +
                    (fallbackWarnings.Count == 0 ? "" : " " +
                        string.Join(" ", fallbackWarnings.OrderBy(value => value).ToArray()));
                _log.Error("Routine execution failed.", failure);
                Complete(completed, new QuickExecutionResult(routineId, routineName,
                    QuickExecutionDisposition.Failed, Status,
                    LastExecutionReport.Planned, LastExecutionReport.Submitted,
                    LastExecutionReport.Confirmed, fallbackWarnings.Count != 0));
                yield break;
            }
            ExecutionReport report = LastExecutionReport;
            Refresh();
            bool confirmed = report.Failed == 0 && report.Confirmed == report.Planned;
            Status = (confirmed ? "Routine confirmed: " : "Routine not confirmed: ") +
                "planned=" + report.Planned + "; queued=" + report.Queued +
                "; submitted=" + report.Submitted + "; cast-started=" + report.CastStarted +
                "; effect-confirmed=" + report.Confirmed +
                "; spend-invoked=" + report.SpendInvocations +
                "; spent=" + report.ResourcesSpent + "; failed=" + report.Failed +
                "; skipped=" + report.Skipped + "; unfulfilled=" + report.Unfulfilled + "." +
                (runner.HaltedAfterStep == null ? string.Empty
                    : " Stopped after cast " + (runner.HaltedAfterStep.Value + 1) +
                        " did not confirm; the later casts were not attempted.") +
                variantReselection;
            if (fallbackWarnings.Count != 0)
                Status = "Instant mode was not fully satisfied. " +
                    string.Join(" ", fallbackWarnings.OrderBy(value => value).ToArray()) + " " + Status;
            CastExecutionRecord firstFailure = report.Records.FirstOrDefault(record =>
                record.Status == CastExecutionStatus.FailedValidation ||
                record.Status == CastExecutionStatus.FailedSubmission ||
                record.Status == CastExecutionStatus.FailedExecution ||
                record.Status == CastExecutionStatus.TimedOutUnconfirmed);
            if (firstFailure != null)
                Status += " Failure: " + firstFailure.Status + "; provider=" +
                    firstFailure.ProviderKey + "; targets=" +
                    string.Join(",", firstFailure.TargetUnitIds.ToArray()) + "; " +
                    firstFailure.Detail;
            foreach (CastExecutionRecord record in report.Records)
                _log.Info("Routine outcome: group=" + routineId + ";step=" +
                    record.StepIndex + ";status=" + record.Status +
                    ";mode=" + Model.Profile.Execution.Mode +
                    ";source=" + record.SourceId + ";ability=" +
                    record.AbilityKey + ";provider=" + record.ProviderKey +
                    ";caster=" + record.CasterUnitId +
                    ";targets=" + string.Join(",", record.TargetUnitIds.ToArray()) +
                    ";expected-recipients=" + string.Join(",",
                        record.ExpectedRecipientUnitIds.ToArray()) +
                    ";strategy=" + record.ExecutionStrategy +
                    ";strategy-reason=" + record.ExecutionStrategyReason +
                    ";enhancements=" + string.Join(",", record.EnhancementIds.ToArray()) +
                    ";pool=" + record.ResourcePoolKey + ";tokens=" +
                    string.Join(",", record.ResourceTokenIds.ToArray()) + ";detail=" + record.Detail);
            Complete(completed, new QuickExecutionResult(routineId, routineName,
                confirmed ? QuickExecutionDisposition.Completed : QuickExecutionDisposition.Failed,
                Status, report.Planned, report.Submitted, report.Confirmed,
                fallbackWarnings.Count != 0));
            _log.Info("[KBP-QUICK] confirmed result produced;group=" + routineId +
                ";confirmed=" + report.Confirmed + ";failed=" + report.Failed +
                ";message=" + Status + ".");
        }

        private static string VariantReselectionSummary(
            PlannerSetupModel model, string routineId)
        {
            if (model == null) return string.Empty;
            string[] names = model.VariantReselectionNotices
                .Where(value => value.RoutineId == routineId)
                .Select(value => value.DisplayName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
            return names.Length == 0 ? string.Empty :
                " Reselect a concrete variant for: " +
                string.Join(", ", names) + ".";
        }

        private static string DescribePlan(CastPlan plan)
        {
            return string.Join(" | ", plan.Steps.Select((step, index) => "step=" + index +
                ";source=" + step.SourceId + ";ability=" +
                step.Provider.Ability.Canonical + ";provider=" +
                step.Provider.Canonical + ";targets=" + string.Join(",", step.TargetUnitIds.ToArray()) +
                ";expected-recipients=" + string.Join(",",
                    step.ExpectedRecipientUnitIds.ToArray()) +
                ";strategy=" + step.ExecutionStrategy +
                ";strategy-reason=" + step.ExecutionStrategyReason +
                ";pool=" + step.Reservation.PoolKey + ";tokens=" +
                string.Join(",", step.Reservation.TokenIds.ToArray()) + ";enhancements=" +
                string.Join(",", step.EnhancementIds.ToArray()) + ";units=" +
                step.Reservation.Units + ";material=" +
                (step.MaterialReservation == null ? "none" :
                    step.MaterialReservation.ItemGuid + "x" + step.MaterialReservation.Count) +
                ";expected=" +
                KingmakerAnimatedCastAdapter.ExpectedEffectIds(step.ExpectedEffects)).ToArray());
        }

        // The Classic run ended by its owner (mod disabled, area change,
        // teardown): disposing the run already ran the executor's cleanup
        // for the cast in progress; this records the outcome for the player.
        internal QuickExecutionResult EndInterruptedExecution(string routineId, string reason)
        {
            IsExecuting = false;
            string name = RoutineDisplayName(routineId);
            // Focused re-review: a run that never submitted a cast says so.
            bool submitted = LastExecutionReport != null && LastExecutionReport.Submitted > 0;
            Status = name + " stopped before it finished (" + (reason ?? "stopped") + "): " +
                (submitted
                    ? "the cast in progress was interrupted and cleaned up, and nothing after it was attempted."
                    : "it had not cast anything yet, and nothing was attempted.");
            _log.Info("[KBP-QUICK] classic run ended by its owner;group=" + routineId +
                ";reason=" + (reason ?? "stopped") + ".");
            return new QuickExecutionResult(routineId, name,
                QuickExecutionDisposition.Failed, Status,
                LastExecutionReport == null ? 0 : LastExecutionReport.Planned,
                LastExecutionReport == null ? 0 : LastExecutionReport.Submitted,
                LastExecutionReport == null ? 0 : LastExecutionReport.Confirmed);
        }

        internal QuickExecutionResult AbortUnexpectedExecution(
            string routineId,
            Exception exception)
        {
            IsExecuting = false;
            string name = RoutineDisplayName(routineId);
            Status = name + " failed before a confirmed result: " +
                (exception == null ? "unknown execution error" : exception.Message);
            _log.Error("[KBP-QUICK] unexpected execution-stage failure;group=" +
                routineId + ";visibleResult=true.", exception ??
                new InvalidOperationException(Status));
            return new QuickExecutionResult(routineId, name,
                QuickExecutionDisposition.Failed, Status,
                LastExecutionReport == null ? 0 : LastExecutionReport.Planned,
                LastExecutionReport == null ? 0 : LastExecutionReport.Submitted,
                LastExecutionReport == null ? 0 : LastExecutionReport.Confirmed);
        }

        private string RoutineDisplayName(string routineId)
        {
            RoutineProfile routine = Model == null ? null : Model.Profile.Routines
                .FirstOrDefault(item => item.RoutineId == routineId);
            if (routine != null && !string.IsNullOrWhiteSpace(routine.Name)) return routine.Name;
            if (string.IsNullOrWhiteSpace(routineId)) return "Routine";
            return char.ToUpperInvariant(routineId[0]) + routineId.Substring(1);
        }

        private static void Complete(Action<QuickExecutionResult> completed, QuickExecutionResult result)
        {
            if (completed != null) completed(result);
        }
    }
}
