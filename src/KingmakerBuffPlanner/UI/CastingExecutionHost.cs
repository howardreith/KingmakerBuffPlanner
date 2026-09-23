using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    // What happened to one casting of a production run, from the player's
    // point of view. Resource spending is reported beside the state, never
    // folded into it: a failed casting may still have spent its resource.
    public enum CastingOutcomeState
    {
        // Not submitted by design: the intended effect was already active.
        Skipped,
        // Not submitted: disclosed omission (Ready Casts Only, disabled, or
        // a draft/blocked casting outside an ordinary run).
        Omitted,
        // Submitted and the expected effect was confirmed.
        EffectConfirmed,
        // Processed and not confirmed: refused by runtime validation, failed
        // submission or execution, timed out, or left uncertain state.
        Failed,
        // In flight when the run was terminated (player cancel, deadline,
        // mod disabled or unloaded).
        Cancelled,
        // Never reached: the run stopped earlier (failure or cancel).
        NotProcessed
    }

    public sealed class CastingOutcomeEntry
    {
        internal CastingOutcomeEntry(string castingId, CastingOutcomeState state,
            bool planned, bool submitted, bool spendReported, string detail,
            bool freeCast = false)
        {
            CastingId = castingId ?? string.Empty;
            State = state;
            Planned = planned;
            Submitted = submitted;
            SpendReported = spendReported;
            FreeCast = freeCast;
            Detail = detail ?? string.Empty;
        }

        public string CastingId { get; private set; }
        public CastingOutcomeState State { get; private set; }
        // Part of the accepted projection (one planned native invocation).
        public bool Planned { get; private set; }
        // The executor reported a native submission for this casting.
        public bool Submitted { get; private set; }
        // The executor reported the native resource spent (or the spend
        // invoked) for this casting.
        public bool SpendReported { get; private set; }
        // The planned cost was a verified free native source (no pool
        // charge, no enhancement or material cost).
        public bool FreeCast { get; private set; }
        // A charged resource was spent: a spend was reported for a casting
        // whose planned cost is not free.
        public bool ResourceSpent
        {
            get { return SpendReported && !FreeCast; }
        }
        public bool EffectConfirmed
        {
            get { return State == CastingOutcomeState.EffectConfirmed; }
        }
        public string Detail { get; private set; }
    }

    public sealed class CastingRunReport
    {
        internal CastingRunReport(string runId, string scopeRoutineId,
            CastingApplyMode mode, string projectionId, string terminalReason,
            bool cancelled, bool halted, IList<CastingOutcomeEntry> entries,
            IList<string> cleanupFailures)
        {
            RunId = runId ?? string.Empty;
            ScopeRoutineId = scopeRoutineId ?? string.Empty;
            Mode = mode;
            ProjectionId = projectionId ?? string.Empty;
            TerminalReason = terminalReason ?? string.Empty;
            Cancelled = cancelled;
            Halted = halted;
            Entries = new ReadOnlyCollection<CastingOutcomeEntry>(
                (entries ?? new CastingOutcomeEntry[0]).ToList());
            CleanupFailures = new ReadOnlyCollection<string>(
                (cleanupFailures ?? new string[0]).ToList());
        }

        public string RunId { get; private set; }
        public string ScopeRoutineId { get; private set; }
        public CastingApplyMode Mode { get; private set; }
        public string ProjectionId { get; private set; }
        // "completed", "halted:<first failure>", or "cancelled:<reason>".
        public string TerminalReason { get; private set; }
        public bool Cancelled { get; private set; }
        public bool Halted { get; private set; }
        public IReadOnlyList<CastingOutcomeEntry> Entries { get; private set; }
        public IReadOnlyList<string> CleanupFailures { get; private set; }

        public int Planned { get { return Entries.Count(entry => entry.Planned); } }
        public int Submitted { get { return Entries.Count(entry => entry.Submitted); } }
        public int Confirmed { get { return Count(CastingOutcomeState.EffectConfirmed); } }
        public int Failed { get { return Count(CastingOutcomeState.Failed); } }
        public int CancelledCastings { get { return Count(CastingOutcomeState.Cancelled); } }
        public int NotProcessed { get { return Count(CastingOutcomeState.NotProcessed); } }
        public int Skipped { get { return Count(CastingOutcomeState.Skipped); } }
        public int Omitted { get { return Count(CastingOutcomeState.Omitted); } }
        public int ResourcesSpent { get { return Entries.Count(entry => entry.ResourceSpent); } }

        // Every planned casting confirmed, nothing failed, stopped or left
        // uncertain.
        public bool Succeeded
        {
            get
            {
                return !Cancelled && !Halted && CleanupFailures.Count == 0 &&
                    Confirmed == Planned;
            }
        }

        private int Count(CastingOutcomeState state)
        {
            return Entries.Count(entry => entry.State == state);
        }
    }

    // Owns at most ONE production casting run. The UI root pumps it once per
    // frame (executors yield per frame). Completion, the player's cancel, a
    // run deadline, mod disable/unload and root teardown all end a run
    // through one idempotent terminal that disposes the coordinator - and
    // with it the in-flight executor, whose own finally-blocks restore any
    // temporary native state - then reports exactly once. Nothing is
    // retried; spent resources and applied effects are reported, never
    // described as rolled back. After Shutdown no run can start until the
    // owner explicitly resumes (mod re-enabled).
    public sealed class CastingExecutionHost
    {
        public const long BaseDeadlineMillis = 20000;
        public const long PerCastingDeadlineMillis = 45000;

        private readonly Func<ExecutionProfile, ICastExecutor> _executorFactory;
        private readonly Func<long> _clockMillis;
        private ActiveRun _active;
        private int _sequence;

        public CastingExecutionHost(Func<ExecutionProfile, ICastExecutor> executorFactory,
            Func<long> clockMillis)
        {
            _executorFactory = executorFactory ?? throw new ArgumentNullException("executorFactory");
            _clockMillis = clockMillis ?? throw new ArgumentNullException("clockMillis");
        }

        public bool IsRunning { get { return _active != null; } }
        public bool Accepting { get { return ShutdownReason == null; } }
        public string ShutdownReason { get; private set; }
        public string ActiveRunId { get { return _active == null ? null : _active.RunId; } }
        public string ActiveScopeRoutineId
        {
            get { return _active == null ? null : _active.Scope; }
        }
        public CastingRunReport LastReport { get; private set; }
        public int StartedRuns { get; private set; }
        public int ReportedRuns { get; private set; }
        // Invoked exactly once per started run, after its terminal.
        public Action<CastingRunReport> RunCompleted { get; set; }
        public string LastCallbackFailure { get; private set; }

        internal CastingDispatchOutcome Start(ExplicitCastingPlan plan,
            CastingApplyDecision decision, string scopeRoutineId,
            ExplicitStepConversion projection, ExecutionProfile settings)
        {
            IReadOnlyList<string> ids = projection == null
                ? (IReadOnlyList<string>)new string[0] : projection.CastingIds;
            if (!Accepting)
                return new CastingDispatchOutcome(false,
                    "native-casting-unavailable:" + ShutdownReason, ids);
            if (_active != null)
                return new CastingDispatchOutcome(false, "execution-in-progress:" +
                    _active.RunId, ids);
            ICastExecutor executor;
            try { executor = _executorFactory(settings ?? ExecutionProfile.Default()); }
            catch (Exception exception)
            {
                return new CastingDispatchOutcome(false, "executor-unavailable:" +
                    exception.GetType().Name + ":" + exception.Message, ids);
            }
            if (executor == null)
                return new CastingDispatchOutcome(false, "executor-unavailable:null", ids);
            string runId = "run-" + (++_sequence);
            var active = new ActiveRun(runId, plan, decision, scopeRoutineId, projection,
                _clockMillis(), BaseDeadlineMillis +
                    PerCastingDeadlineMillis * projection.Plan.Steps.Count);
            active.Run = new ExplicitCastingRunCoordinator(executor)
                .Run(projection, outcome => active.Outcome = outcome);
            _active = active;
            StartedRuns++;
            return new CastingDispatchOutcome(true, "run-started:" + runId, ids);
        }

        // One executor step per frame.
        public void Pump()
        {
            ActiveRun active = _active;
            if (active == null) return;
            if (_clockMillis() - active.StartedMillis > active.DeadlineMillis)
            {
                Terminate(active, "deadline");
                return;
            }
            bool moved;
            active.Pumped = true;
            try { moved = active.Run.MoveNext(); }
            catch (Exception exception)
            {
                Terminate(active, "run-pump-exception:" + exception.GetType().Name +
                    ":" + exception.Message);
                return;
            }
            if (!moved) Terminate(active, "completed");
        }

        // The player's deliberate stop (or any owner stop) of the active run.
        public bool Cancel(string reason)
        {
            ActiveRun active = _active;
            if (active == null) return false;
            Terminate(active, string.IsNullOrEmpty(reason) ? "cancelled" : reason);
            return true;
        }

        // Disable, unload or teardown: end the active run and refuse new ones
        // until Resume.
        public void Shutdown(string reason)
        {
            ShutdownReason = string.IsNullOrEmpty(reason) ? "shutdown" : reason;
            ActiveRun active = _active;
            if (active != null) Terminate(active, ShutdownReason);
        }

        public void Resume()
        {
            ShutdownReason = null;
        }

        private void Terminate(ActiveRun active, string reason)
        {
            if (active.Terminated) return;
            active.Terminated = true;
            if (ReferenceEquals(_active, active)) _active = null;
            var cleanup = new List<string>();
            var disposable = active.Run as IDisposable;
            active.Run = null;
            if (disposable != null)
            {
                // Disposing the coordinator disposes the in-flight executor
                // exactly once; its own cleanup restores temporary native
                // state. A throwing disposal is uncertain cleanup, reported.
                try { disposable.Dispose(); }
                catch (Exception exception)
                {
                    cleanup.Add("run-dispose:" + exception.GetType().Name + ":" +
                        exception.Message);
                }
            }
            CastingRunReport report = BuildReport(active, reason, cleanup);
            LastReport = report;
            ReportedRuns++;
            Action<CastingRunReport> callback = RunCompleted;
            if (callback == null) return;
            try { callback(report); }
            catch (Exception exception)
            {
                LastCallbackFailure = exception.GetType().Name + ":" + exception.Message;
            }
        }

        private static CastingRunReport BuildReport(ActiveRun active, string reason,
            List<string> cleanup)
        {
            ExplicitCastingRunOutcome outcome = active.Outcome;
            var byId = new Dictionary<string, ExplicitCastingRunEntry>(StringComparer.Ordinal);
            if (outcome != null)
                foreach (ExplicitCastingRunEntry entry in outcome.Entries)
                    byId[entry.CastingId] = entry;
            var planned = new HashSet<string>(active.Projection.CastingIds,
                StringComparer.Ordinal);
            var omissions = new Dictionary<string, CastingOmission>(StringComparer.Ordinal);
            foreach (CastingOmission omission in active.Decision.Omissions)
                omissions[omission.CastingId] = omission;
            var entries = new List<CastingOutcomeEntry>();
            foreach (ResolvedCasting casting in active.Plan.Castings)
            {
                if (casting == null || (active.Scope != null &&
                    !string.Equals(casting.RoutineId, active.Scope, StringComparison.Ordinal)))
                    continue;
                if (planned.Contains(casting.CastingId))
                {
                    ExplicitCastingRunEntry entry;
                    if (!byId.TryGetValue(casting.CastingId, out entry))
                    {
                        entries.Add(new CastingOutcomeEntry(casting.CastingId,
                            CastingOutcomeState.NotProcessed, true, false, false,
                            active.Pumped ? "no-run-outcome" : "stopped-before-start"));
                        continue;
                    }
                    CastingOutcomeState state = entry.Confirmed
                        ? CastingOutcomeState.EffectConfirmed
                        : string.Equals(entry.FinalStatus, "Cancelled", StringComparison.Ordinal)
                            ? CastingOutcomeState.Cancelled
                            : !entry.Processed
                                ? CastingOutcomeState.NotProcessed
                                : CastingOutcomeState.Failed;
                    entries.Add(new CastingOutcomeEntry(casting.CastingId, state, true,
                        entry.NativeSubmissionReported,
                        entry.ResourceSpentReported || entry.SpendInvoked,
                        entry.FinalStatus + ":" + entry.Detail, IsFree(casting)));
                    continue;
                }
                CastingOmission disclosed;
                string detail = omissions.TryGetValue(casting.CastingId, out disclosed)
                    ? string.Join(",", disclosed.Reasons.ToArray())
                    : string.Join(",", casting.ReadinessReasons.ToArray());
                entries.Add(new CastingOutcomeEntry(casting.CastingId,
                    casting.Readiness == ResolvedCastingReadiness.AlreadySatisfied
                        ? CastingOutcomeState.Skipped
                        : CastingOutcomeState.Omitted,
                    false, false, false, detail));
            }
            // A run stopped before its first step never touched the game; a
            // started run without an outcome is uncertain and says so.
            if (outcome == null && active.Pumped) cleanup.Add("run-outcome-missing");
            bool cancelled = outcome == null || outcome.Cancelled;
            bool halted = !cancelled && outcome.Halted;
            string terminal = cancelled ? "cancelled:" + reason
                : halted ? "halted:" + outcome.HaltReason
                : "completed";
            return new CastingRunReport(active.RunId, active.Scope, active.Decision.Mode,
                active.Projection.ProjectionId, terminal, cancelled, halted, entries, cleanup);
        }

        private static bool IsFree(ResolvedCasting casting)
        {
            return casting.Cost.Count == 1 &&
                casting.Cost[0].Category == CastingCostCategory.NativePool &&
                casting.Cost[0].Unlimited;
        }

        private sealed class ActiveRun
        {
            internal ActiveRun(string runId, ExplicitCastingPlan plan,
                CastingApplyDecision decision, string scope,
                ExplicitStepConversion projection, long startedMillis, long deadlineMillis)
            {
                RunId = runId;
                Plan = plan;
                Decision = decision;
                Scope = scope;
                Projection = projection;
                StartedMillis = startedMillis;
                DeadlineMillis = deadlineMillis;
            }

            internal readonly string RunId;
            internal readonly ExplicitCastingPlan Plan;
            internal readonly CastingApplyDecision Decision;
            internal readonly string Scope;
            internal readonly ExplicitStepConversion Projection;
            internal readonly long StartedMillis;
            internal readonly long DeadlineMillis;
            internal IEnumerator Run;
            internal ExplicitCastingRunOutcome Outcome;
            internal bool Terminated;
            internal bool Pumped;
        }
    }

    // The production dispatch boundary: the reviewed, accepted and freshly
    // preflighted projection is handed to the execution host, which runs it
    // through the existing instant/animated executors one casting at a time.
    // It accepts only a converted STANDARD projection whose recomputed
    // identity matches its contract and whose casting order is exactly the
    // executable set of the decision - never a probe projection, never a
    // re-planned or partial list.
    public sealed class NativeCastingDispatchBoundary : ICastingDispatchBoundary
    {
        private readonly CastingExecutionHost _host;
        private readonly Func<ExecutionProfile> _settings;
        private readonly Action<string> _log;

        public NativeCastingDispatchBoundary(CastingExecutionHost host,
            Func<ExecutionProfile> settings, Action<string> log = null)
        {
            _host = host ?? throw new ArgumentNullException("host");
            _settings = settings ?? throw new ArgumentNullException("settings");
            _log = log;
        }

        public CastingExecutionHost Host { get { return _host; } }

        public string DispositionReason
        {
            get
            {
                if (!_host.Accepting)
                    return "native-casting-unavailable:" + _host.ShutdownReason;
                return _host.IsRunning ? "native-casting-busy" : "native-casting-enabled";
            }
        }

        public CastingDispatchOutcome Submit(ExplicitCastingPlan plan,
            CastingApplyDecision decision, string scopeRoutineId,
            ExplicitStepConversion projection)
        {
            string refusal = Validate(plan, decision, projection);
            if (refusal != null)
                return new CastingDispatchOutcome(false, refusal,
                    projection == null ? (IReadOnlyList<string>)new string[0]
                        : projection.CastingIds);
            ExecutionProfile settings;
            try { settings = _settings(); }
            catch (Exception exception)
            {
                return new CastingDispatchOutcome(false, "execution-settings-unavailable:" +
                    exception.GetType().Name, projection.CastingIds);
            }
            CastingDispatchOutcome outcome = _host.Start(plan, decision, scopeRoutineId,
                projection, settings);
            if (_log != null)
            {
                try
                {
                    _log("dispatch;submitted=" + outcome.Submitted + ";reason=" + outcome.Reason +
                        ";scope=" + (scopeRoutineId ?? "one-pass") + ";mode=" + decision.Mode +
                        ";executionMode=" + (settings == null ? "default" : settings.Mode) +
                        ";castings=" + string.Join(",", projection.CastingIds.ToArray()) +
                        ";projection=" + projection.ProjectionId);
                }
                catch (Exception)
                {
                    // Logging never changes the dispatch outcome.
                }
            }
            return outcome;
        }

        internal static string Validate(ExplicitCastingPlan plan,
            CastingApplyDecision decision, ExplicitStepConversion projection)
        {
            if (plan == null || decision == null || !decision.Allowed)
                return "decision-not-allowed";
            if (projection == null || !projection.Converted)
                return "projection-not-converted";
            if (projection.Scope != ExplicitProjectionScope.Standard)
                return "projection-scope-not-standard:" + projection.Scope;
            if (projection.Plan.Steps.Count == 0 ||
                projection.Plan.Steps.Count != projection.CastingIds.Count)
                return "projection-shape-invalid";
            if (!projection.CastingIds.SequenceEqual(decision.ExecutableCastingIds,
                    StringComparer.Ordinal))
                return "projection-decision-mismatch";
            string recomputed;
            try
            {
                recomputed = ExplicitCastingStepConverter.Identity(
                    projection.Plan.Steps.ToList(), projection.Scope);
            }
            catch (NotSupportedException exception)
            {
                return "projection-identity-unrepresentable:" + exception.Message;
            }
            if (!string.Equals(recomputed, projection.ProjectionId, StringComparison.Ordinal))
                return "projection-tampered";
            return null;
        }
    }
}
