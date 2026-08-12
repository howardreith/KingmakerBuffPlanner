using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KingmakerBuffPlanner.UI
{
    public interface IPlannerInputBoundary
    {
        object CaptureState();
        void EnterPlannerMode();
        void RestoreState(object state);
        bool PlannerModeRequested { get; }
    }

    public sealed class BuffPlannerInputLease : IDisposable
    {
        private readonly IPlannerInputBoundary _boundary;
        private readonly object _state;
        private bool _disposed;

        private BuffPlannerInputLease(IPlannerInputBoundary boundary, object state)
        {
            _boundary = boundary;
            _state = state;
        }

        public bool IsReleased { get { return _disposed; } }

        public static BuffPlannerInputLease Acquire(IPlannerInputBoundary boundary)
        {
            if (boundary == null) throw new ArgumentNullException("boundary");
            object state = boundary.CaptureState();
            try
            {
                boundary.EnterPlannerMode();
                if (!boundary.PlannerModeRequested)
                    throw new InvalidOperationException("Planner input mode was not requested.");
                return new BuffPlannerInputLease(boundary, state);
            }
            catch
            {
                boundary.RestoreState(state);
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _boundary.RestoreState(_state);
        }
    }

    public sealed class PlannerScreenStateMachine : IDisposable
    {
        private readonly Func<BuffPlannerInputLease> _acquire;
        private BuffPlannerInputLease _lease;

        public PlannerScreenStateMachine(Func<BuffPlannerInputLease> acquire)
        {
            _acquire = acquire ?? throw new ArgumentNullException("acquire");
        }

        public bool IsOpen { get { return _lease != null; } }
        public int OpenTransitions { get; private set; }
        public int CloseTransitions { get; private set; }

        public bool Open()
        {
            if (_lease != null) return false;
            BuffPlannerInputLease lease = _acquire();
            if (lease == null) throw new InvalidOperationException("Input lease factory returned null.");
            _lease = lease;
            OpenTransitions++;
            return true;
        }

        public bool Close()
        {
            if (_lease == null) return false;
            BuffPlannerInputLease lease = _lease;
            _lease = null;
            try { lease.Dispose(); }
            finally { CloseTransitions++; }
            return true;
        }

        public void Dispose()
        {
            Close();
        }
    }

    public enum QuickExecutionDisposition
    {
        Completed,
        Refused,
        Failed
    }

    public sealed class QuickExecutionResult
    {
        public QuickExecutionResult(
            string routineId,
            string routineName,
            QuickExecutionDisposition disposition,
            string message,
            int planned,
            int fired)
        {
            RoutineId = routineId ?? string.Empty;
            RoutineName = routineName ?? string.Empty;
            Disposition = disposition;
            Message = message ?? string.Empty;
            Planned = planned;
            Fired = fired;
        }

        public string RoutineId { get; private set; }
        public string RoutineName { get; private set; }
        public QuickExecutionDisposition Disposition { get; private set; }
        public string Message { get; private set; }
        public int Planned { get; private set; }
        public int Fired { get; private set; }
    }

    public interface IPlannerRoutineRunner
    {
        bool TryStart(string routineId, Action<QuickExecutionResult> completed);
    }

    public sealed class BuffPlannerQuickExecuteController
    {
        private readonly IPlannerRoutineRunner _runner;
        private readonly BuffPlannerUiLifecycleDiagnostics _diagnostics;
        private readonly Action<QuickExecutionResult> _present;

        public BuffPlannerQuickExecuteController(
            IPlannerRoutineRunner runner,
            BuffPlannerUiLifecycleDiagnostics diagnostics,
            Action<QuickExecutionResult> present)
        {
            _runner = runner ?? throw new ArgumentNullException("runner");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
            _present = present ?? throw new ArgumentNullException("present");
        }

        public bool Execute(string routineId)
        {
            string normalized = NormalizeRoutineId(routineId);
            _diagnostics.RecordListener(normalized);
            _diagnostics.RecordGroupResolved(normalized);
            bool started = _runner.TryStart(normalized, result =>
            {
                _diagnostics.RecordPlanRevalidated(normalized);
                if (result.Disposition == QuickExecutionDisposition.Refused)
                    _diagnostics.RecordRefused(normalized);
                else _diagnostics.RecordExecutionInvoked(normalized);
                _present(result);
                _diagnostics.RecordResultPresented(normalized);
            });
            if (!started)
            {
                var result = new QuickExecutionResult(normalized, DisplayName(normalized),
                    QuickExecutionDisposition.Refused,
                    "Another buff routine is already executing.", 0, 0);
                _diagnostics.RecordRefused(normalized);
                _present(result);
                _diagnostics.RecordResultPresented(normalized);
            }
            return started;
        }

        private static string NormalizeRoutineId(string routineId)
        {
            if (routineId == "long" || routineId == "important" || routineId == "short")
                return routineId;
            throw new ArgumentException("Unknown routine.", "routineId");
        }

        private static string DisplayName(string routineId)
        {
            return char.ToUpperInvariant(routineId[0]) + routineId.Substring(1);
        }
    }

    public sealed class BuffPlannerUiLifecycleDiagnostics
    {
        private readonly Dictionary<string, MutableQuickFlowDiagnostics> _flows =
            new Dictionary<string, MutableQuickFlowDiagnostics>(StringComparer.Ordinal);

        public int HudInstallCount { get; private set; }
        public int HudDestroyCount { get; private set; }
        public int ScreenCreateCount { get; private set; }
        public int ScreenDestroyCount { get; private set; }
        public int InputLeaseAcquireCount { get; private set; }
        public int InputLeaseReleaseCount { get; private set; }
        public int PointerEventCount { get; private set; }
        public int ScrollEventCount { get; private set; }
        public int DragEventCount { get; private set; }

        public void RecordHudInstalled() { HudInstallCount++; }
        public void RecordHudDestroyed() { HudDestroyCount++; }
        public void RecordScreenCreated() { ScreenCreateCount++; }
        public void RecordScreenDestroyed() { ScreenDestroyCount++; }
        public void RecordInputLeaseAcquired() { InputLeaseAcquireCount++; }
        public void RecordInputLeaseReleased() { InputLeaseReleaseCount++; }
        public void RecordPointer(string routineId)
        {
            PointerEventCount++;
            if (!string.IsNullOrEmpty(routineId)) Flow(routineId).PointerEvents++;
        }
        public void RecordScroll() { ScrollEventCount++; }
        public void RecordDrag() { DragEventCount++; }
        public void RecordListener(string routineId) { Flow(routineId).Listeners++; }
        public void RecordGroupResolved(string routineId) { Flow(routineId).GroupsResolved++; }
        public void RecordPlanRevalidated(string routineId) { Flow(routineId).PlansRevalidated++; }
        public void RecordExecutionInvoked(string routineId) { Flow(routineId).ExecutionsInvoked++; }
        public void RecordRefused(string routineId) { Flow(routineId).Refusals++; }
        public void RecordResultPresented(string routineId) { Flow(routineId).ResultsPresented++; }

        public QuickFlowDiagnostics GetFlow(string routineId)
        {
            MutableQuickFlowDiagnostics flow;
            if (!_flows.TryGetValue(routineId, out flow)) flow = new MutableQuickFlowDiagnostics();
            return new QuickFlowDiagnostics(flow.PointerEvents, flow.Listeners, flow.GroupsResolved,
                flow.PlansRevalidated, flow.ExecutionsInvoked, flow.Refusals, flow.ResultsPresented);
        }

        public IReadOnlyDictionary<string, QuickFlowDiagnostics> SnapshotFlows()
        {
            var result = _flows.ToDictionary(pair => pair.Key, pair => GetFlow(pair.Key), StringComparer.Ordinal);
            return new ReadOnlyDictionary<string, QuickFlowDiagnostics>(result);
        }

        private MutableQuickFlowDiagnostics Flow(string routineId)
        {
            MutableQuickFlowDiagnostics flow;
            if (!_flows.TryGetValue(routineId, out flow))
            {
                flow = new MutableQuickFlowDiagnostics();
                _flows.Add(routineId, flow);
            }
            return flow;
        }

        private sealed class MutableQuickFlowDiagnostics
        {
            internal int PointerEvents;
            internal int Listeners;
            internal int GroupsResolved;
            internal int PlansRevalidated;
            internal int ExecutionsInvoked;
            internal int Refusals;
            internal int ResultsPresented;
        }
    }

    public sealed class QuickFlowDiagnostics
    {
        internal QuickFlowDiagnostics(int pointerEvents, int listeners, int groupsResolved,
            int plansRevalidated, int executionsInvoked, int refusals, int resultsPresented)
        {
            PointerEvents = pointerEvents;
            Listeners = listeners;
            GroupsResolved = groupsResolved;
            PlansRevalidated = plansRevalidated;
            ExecutionsInvoked = executionsInvoked;
            Refusals = refusals;
            ResultsPresented = resultsPresented;
        }

        public int PointerEvents { get; private set; }
        public int Listeners { get; private set; }
        public int GroupsResolved { get; private set; }
        public int PlansRevalidated { get; private set; }
        public int ExecutionsInvoked { get; private set; }
        public int Refusals { get; private set; }
        public int ResultsPresented { get; private set; }
    }
}
