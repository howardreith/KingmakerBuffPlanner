using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Discovery;
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
        internal bool IsExecuting { get; private set; }
        internal RoutinePlanResult LastPreview { get; private set; }
        internal ExecutionReport LastExecutionReport { get; private set; }
        internal string ProfileStatus { get; private set; }
        internal PartyCatalogDiscoveryDiagnostics CatalogDiscovery { get; private set; }
        internal IReadOnlyList<ProviderPlanningOption> ProviderOptions
        {
            get { return _providerOptions ?? new ProviderPlanningOption[0]; }
        }
        internal string LastBindingFailure { get; private set; }

        internal void Refresh()
        {
            try
            {
                if (Game.Instance == null || Game.Instance.Player == null ||
                    string.IsNullOrWhiteSpace(Game.Instance.Player.GameId))
                {
                    Model = null;
                    _snapshot = null;
                    _activeEffects = null;
                    _effects = null;
                    _providerOptions = null;
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
                ProfileStatus = string.IsNullOrEmpty(loaded.SourcePath)
                    ? "No prior profile was found; using a new schema " +
                        BuffPlannerProfile.CurrentSchemaVersion + " profile."
                    : "Loaded profile " + loaded.SourcePath + "; schema=" +
                        loaded.Profile.SchemaVersion + "; migrated=" + loaded.Migrated +
                        "; recoveredFromBackup=" + loaded.RecoveredFromBackup + ".";
                _log.Info("Profile load: " + ProfileStatus);
                if (!string.IsNullOrEmpty(loaded.Warning))
                    _log.Info("Profile recovery warning: " + loaded.Warning);
                _providerOptions = new KingmakerProviderOptionBuilder().Build(snapshot, effects);
                Model = new PlannerSetupModel(loaded.Profile, snapshot, active, effects,
                    _providerOptions, _profiles.Save);
                _snapshot = snapshot;
                _activeEffects = active;
                _effects = effects;
                CatalogDiscovery = snapshotBuilder.Diagnostics;
                LastBindingFailure = string.Empty;
                Status = snapshot.Units.Count + " party/pet targets; " +
                    Model.Sources.Count + " discovered buff sources; " +
                    snapshot.Providers.Count + " providers.";
                _log.Info("[KBP-CATALOG] discovery;" + CatalogDiscovery + ".");
                LogBlessSlice(loaded.Profile, snapshot, _providerOptions);
            }
            catch (Exception exception)
            {
                Model = null;
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
            IEnumerable<ProviderPlanningOption> options)
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
            SourceAssignmentProfile assignment = profile.Routines.SelectMany(item => item.Assignments)
                .FirstOrDefault(item => item.SourceId == provider.Key.Ability.Canonical);
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

        internal RoutinePlanResult PreviewRoutine(string routineId)
        {
            if (Model == null || _snapshot == null || _activeEffects == null ||
                _effects == null || _providerOptions == null)
                throw new InvalidOperationException("A campaign planner snapshot is required.");
            LastPreview = new RoutinePlanService().Plan(Model.Profile, routineId, _snapshot,
                _activeEffects, _effects, _providerOptions);
            return LastPreview;
        }

        internal IEnumerator ExecuteRoutine(string routineId)
        {
            return ExecuteRoutine(routineId, null);
        }

        internal IEnumerator ExecuteRoutine(string routineId, Action<QuickExecutionResult> completed)
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
            RoutineProfile configuredRoutine = Model.Profile.Routines.First(r =>
                r.RoutineId == routineId);
            _log.Info("[KBP-QUICK] assignments resolved;group=" + routineId +
                ";assignments=" + configuredRoutine.Assignments.Count + ".");
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
                    "; unfulfilled=" + unfulfilled + ".";
                Complete(completed, new QuickExecutionResult(routineId, routineName,
                    QuickExecutionDisposition.Refused, Status, 0, 0, 0));
                _log.Info("[KBP-QUICK] deliberately refused;group=" + routineId +
                    ";reason=" + Status + ".");
                yield break;
            }
            LastExecutionReport = new ExecutionReport(preview.Plan);
            ICastExecutor executor;
            if (Model.Profile.Execution.Mode == "instant")
            {
                var fallbackProviders = new HashSet<string>(_providerOptions
                    .Where(o => o.RequiresAnimatedExecution)
                    .Select(o => o.Provider.Key.Canonical), StringComparer.Ordinal);
                executor = new HybridCastExecutor(
                    new KingmakerInstantCastAdapter(), new KingmakerAnimatedCastAdapter(),
                    step => fallbackProviders.Contains(step.Provider.Canonical),
                    Model.Profile.Execution.AllowAnimatedFallback,
                    Model.Profile.Execution.OutOfCombatOnly);
            }
            else executor = new AnimatedCastExecutor(new KingmakerAnimatedCastAdapter(),
                Model.Profile.Execution.OutOfCombatOnly);
            IsExecuting = true;
            _log.Info("[KBP-QUICK] execution invoked;group=" + routineId +
                ";mode=" + Model.Profile.Execution.Mode + ";steps=" +
                preview.Plan.Steps.Count + ".");
            Status = "Executing " + routineId + " routine: " + preview.Plan.Steps.Count + " planned casts.";
            _log.Info("Routine plan: " + DescribePlan(preview.Plan));
            IEnumerator work = executor.Execute(preview.Plan, LastExecutionReport);
            Exception failure = null;
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
            IsExecuting = false;
            if (failure != null)
            {
                Status = "Routine execution failed: " + failure.Message;
                _log.Error("Routine execution failed.", failure);
                Complete(completed, new QuickExecutionResult(routineId, routineName,
                    QuickExecutionDisposition.Failed, Status,
                    LastExecutionReport.Planned, LastExecutionReport.Submitted,
                    LastExecutionReport.Confirmed));
                yield break;
            }
            ExecutionReport report = LastExecutionReport;
            Refresh();
            bool confirmed = report.Failed == 0 && report.Confirmed == report.Planned;
            Status = (confirmed ? "Routine confirmed: " : "Routine not confirmed: ") +
                "planned=" + report.Planned + "; queued=" + report.Queued +
                "; submitted=" + report.Submitted + "; cast-started=" + report.CastStarted +
                "; effect-confirmed=" + report.Confirmed +
                "; spent=" + report.ResourcesSpent + "; failed=" + report.Failed +
                "; skipped=" + report.Skipped + "; unfulfilled=" + report.Unfulfilled + ".";
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
                _log.Info("Routine outcome: step=" + record.StepIndex + ";status=" + record.Status +
                    ";ability=" + record.AbilityKey + ";provider=" + record.ProviderKey +
                    ";targets=" + string.Join(",", record.TargetUnitIds.ToArray()) +
                    ";pool=" + record.ResourcePoolKey + ";tokens=" +
                    string.Join(",", record.ResourceTokenIds.ToArray()) + ";detail=" + record.Detail);
            Complete(completed, new QuickExecutionResult(routineId, routineName,
                confirmed ? QuickExecutionDisposition.Completed : QuickExecutionDisposition.Failed,
                Status, report.Planned, report.Submitted, report.Confirmed));
            _log.Info("[KBP-QUICK] confirmed result produced;group=" + routineId +
                ";confirmed=" + report.Confirmed + ";failed=" + report.Failed +
                ";message=" + Status + ".");
        }

        private static string DescribePlan(CastPlan plan)
        {
            return string.Join(" | ", plan.Steps.Select((step, index) => "step=" + index +
                ";ability=" + step.Provider.Ability.Canonical + ";provider=" +
                step.Provider.Canonical + ";targets=" + string.Join(",", step.TargetUnitIds.ToArray()) +
                ";pool=" + step.Reservation.PoolKey + ";tokens=" +
                string.Join(",", step.Reservation.TokenIds.ToArray()) + ";units=" +
                step.Reservation.Units + ";material=" +
                (step.MaterialReservation == null ? "none" :
                    step.MaterialReservation.ItemGuid + "x" + step.MaterialReservation.Count) +
                ";expected=" +
                KingmakerAnimatedCastAdapter.ExpectedEffectIds(step.ExpectedEffects)).ToArray());
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
