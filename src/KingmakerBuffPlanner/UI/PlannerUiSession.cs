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
                    Status = "No campaign is loaded. Profiles are external and are not created at the main menu.";
                    return;
                }
                string campaignId = Game.Instance.Player.GameId;
                PartyProviderSnapshot snapshot = new KingmakerPartySnapshotBuilder(_overrides).Build();
                var active = new KingmakerActiveEffectSnapshotBuilder().Build();
                var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal);
                var adapter = new KingmakerActionGraphAdapter();
                var scanner = new ActionGraphScanner();
                foreach (var abilityKey in snapshot.Providers.Select(p => p.Key.Ability)
                    .GroupBy(k => k.Canonical, StringComparer.Ordinal).Select(g => g.First()))
                {
                    string guid = string.IsNullOrEmpty(abilityKey.VariantGuid)
                        ? abilityKey.BaseAbilityGuid
                        : abilityKey.VariantGuid;
                    BlueprintAbility ability = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(guid);
                    if (ability != null)
                        effects[abilityKey.Canonical] = _overrides.Apply(
                            guid, scanner.Scan(adapter.Adapt(ability)).Expression).Expression;
                }
                ProfileLoadResult loaded = _profiles.Load(campaignId);
                if (!string.IsNullOrEmpty(loaded.Warning))
                    _log.Info("Profile recovery warning: " + loaded.Warning);
                Model = new PlannerSetupModel(loaded.Profile, snapshot, active, effects, _profiles.Save);
                _snapshot = snapshot;
                _activeEffects = active;
                _effects = effects;
                _providerOptions = new KingmakerProviderOptionBuilder().Build(snapshot, effects);
                Status = snapshot.Units.Count + " party/pet targets; " +
                    Model.Sources.Count + " discovered buff sources; " +
                    snapshot.Providers.Count + " providers.";
            }
            catch (Exception exception)
            {
                Model = null;
                Status = "Setup refresh failed: " + exception.Message;
                _log.Error("Planner UI refresh failed.", exception);
            }
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
            if (IsExecuting) yield break;
            RoutinePlanResult preview;
            try
            {
                preview = PreviewRoutine(routineId);
            }
            catch (Exception exception)
            {
                Status = "Routine preview failed: " + exception.Message;
                _log.Error("Routine preview failed.", exception);
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
            Status = "Executing " + routineId + " routine: " + preview.Plan.Steps.Count + " planned casts.";
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
                yield break;
            }
            ExecutionReport report = LastExecutionReport;
            Refresh();
            Status = "Routine complete: planned=" + report.Planned +
                "; fired=" + report.Fired + "; observed=" + report.SuccessfullyObserved +
                "; spent=" + report.ResourcesSpent + "; failed=" + report.Failed +
                "; skipped=" + report.Skipped + "; unfulfilled=" + report.Unfulfilled + ".";
        }
    }
}
