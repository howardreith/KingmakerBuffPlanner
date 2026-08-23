using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KingmakerBuffPlanner.UI
{
    public sealed class HudInstallInvalidationGate
    {
        private bool _requested = true;
        private int _hostIdentity;
        private bool _hostActive;

        public bool IsRequested { get { return _requested; } }
        public int RequestCount { get; private set; }
        public int AttemptCount { get; private set; }

        public void Request()
        {
            if (_requested) return;
            _requested = true;
            RequestCount++;
        }

        public void Cancel()
        {
            _requested = false;
        }

        public bool ObserveHost(int hostIdentity, bool hostActive)
        {
            if (hostIdentity != _hostIdentity || hostActive != _hostActive)
            {
                _hostIdentity = hostIdentity;
                _hostActive = hostActive;
                if (hostActive) Request();
            }
            if (!_requested || !hostActive) return false;
            _requested = false;
            AttemptCount++;
            return true;
        }
    }

    public sealed class DeferredUiReadinessGate
    {
        private readonly int _minimumFrames;

        public DeferredUiReadinessGate(int minimumFrames)
        {
            if (minimumFrames < 1) throw new ArgumentOutOfRangeException("minimumFrames");
            _minimumFrames = minimumFrames;
        }

        public int ObservedFrames { get; private set; }
        public bool IsReady { get { return ObservedFrames >= _minimumFrames; } }

        public bool ObserveFrame()
        {
            if (ObservedFrames < _minimumFrames) ObservedFrames++;
            return IsReady;
        }

        public void Reset()
        {
            ObservedFrames = 0;
        }
    }

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

    public enum PlannerScreenLifecycleState
    {
        Closed,
        OpeningPresentation,
        AcquiringInputLease,
        Open,
        Closing,
        FaultedRollback
    }

    public sealed class PlannerScreenStateMachine : IDisposable
    {
        private readonly Func<BuffPlannerInputLease> _acquire;
        private BuffPlannerInputLease _lease;

        public PlannerScreenStateMachine(Func<BuffPlannerInputLease> acquire)
        {
            _acquire = acquire ?? throw new ArgumentNullException("acquire");
        }

        public PlannerScreenLifecycleState State { get; private set; }
        public bool IsOpen { get { return State == PlannerScreenLifecycleState.Open; } }
        public bool HasInputLease { get { return _lease != null; } }
        public int OpenTransitions { get; private set; }
        public int CloseTransitions { get; private set; }
        public int RollbackTransitions { get; private set; }

        public bool BeginPresentation()
        {
            if (State != PlannerScreenLifecycleState.Closed) return false;
            State = PlannerScreenLifecycleState.OpeningPresentation;
            return true;
        }

        public void AcquireInputLease()
        {
            if (State != PlannerScreenLifecycleState.OpeningPresentation)
                throw new InvalidOperationException("Presentation must be validated before acquiring input.");
            State = PlannerScreenLifecycleState.AcquiringInputLease;
            try
            {
                BuffPlannerInputLease lease = _acquire();
                if (lease == null) throw new InvalidOperationException("Input lease factory returned null.");
                _lease = lease;
                State = PlannerScreenLifecycleState.Open;
                OpenTransitions++;
            }
            catch
            {
                RollbackInternal();
                throw;
            }
        }

        public bool Close()
        {
            if (State == PlannerScreenLifecycleState.Closed) return false;
            if (State != PlannerScreenLifecycleState.Open)
            {
                RollbackInternal();
                return true;
            }
            State = PlannerScreenLifecycleState.Closing;
            BuffPlannerInputLease lease = _lease;
            _lease = null;
            try
            {
                if (lease != null) lease.Dispose();
            }
            finally
            {
                CloseTransitions++;
                State = PlannerScreenLifecycleState.Closed;
            }
            return true;
        }

        public bool Rollback()
        {
            if (State == PlannerScreenLifecycleState.Closed) return false;
            RollbackInternal();
            return true;
        }

        private void RollbackInternal()
        {
            State = PlannerScreenLifecycleState.FaultedRollback;
            BuffPlannerInputLease lease = _lease;
            _lease = null;
            try
            {
                if (lease != null) lease.Dispose();
            }
            finally
            {
                RollbackTransitions++;
                State = PlannerScreenLifecycleState.Closed;
            }
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
            int submitted,
            int confirmed)
        {
            RoutineId = routineId ?? string.Empty;
            RoutineName = routineName ?? string.Empty;
            Disposition = disposition;
            Message = message ?? string.Empty;
            Planned = planned;
            Submitted = submitted;
            Confirmed = confirmed;
        }

        public string RoutineId { get; private set; }
        public string RoutineName { get; private set; }
        public QuickExecutionDisposition Disposition { get; private set; }
        public string Message { get; private set; }
        public int Planned { get; private set; }
        public int Submitted { get; private set; }
        public int Confirmed { get; private set; }
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
                    "Another buff routine is already executing.", 0, 0, 0);
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
        public int PresentationValidatedCount { get; private set; }
        public int PresentationValidatedOrder { get; private set; }
        public int InputLeaseAcquiredOrder { get; private set; }
        private int _eventOrder;

        public void RecordHudInstalled() { HudInstallCount++; }
        public void RecordHudDestroyed() { HudDestroyCount++; }
        public void RecordScreenCreated() { ScreenCreateCount++; }
        public void RecordScreenDestroyed() { ScreenDestroyCount++; }
        public void RecordPresentationValidated()
        {
            PresentationValidatedCount++;
            PresentationValidatedOrder = ++_eventOrder;
        }
        public void RecordInputLeaseAcquired()
        {
            InputLeaseAcquireCount++;
            InputLeaseAcquiredOrder = ++_eventOrder;
        }
        public void RecordInputLeaseReleased() { InputLeaseReleaseCount++; }
        public void RecordPointer(string routineId)
        {
            PointerEventCount++;
            if (!string.IsNullOrEmpty(routineId)) Flow(routineId).PointerEvents++;
        }
        public void RecordPointerEnter(string routineId)
        {
            if (!string.IsNullOrEmpty(routineId)) Flow(routineId).PointerEnters++;
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
            return new QuickFlowDiagnostics(flow.PointerEnters, flow.PointerEvents, flow.Listeners, flow.GroupsResolved,
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
            internal int PointerEnters;
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
        internal QuickFlowDiagnostics(int pointerEnters, int pointerEvents, int listeners, int groupsResolved,
            int plansRevalidated, int executionsInvoked, int refusals, int resultsPresented)
        {
            PointerEnters = pointerEnters;
            PointerEvents = pointerEvents;
            Listeners = listeners;
            GroupsResolved = groupsResolved;
            PlansRevalidated = plansRevalidated;
            ExecutionsInvoked = executionsInvoked;
            Refusals = refusals;
            ResultsPresented = resultsPresented;
        }

        public int PointerEnters { get; private set; }
        public int PointerEvents { get; private set; }
        public int Listeners { get; private set; }
        public int GroupsResolved { get; private set; }
        public int PlansRevalidated { get; private set; }
        public int ExecutionsInvoked { get; private set; }
        public int Refusals { get; private set; }
        public int ResultsPresented { get; private set; }
    }
}
