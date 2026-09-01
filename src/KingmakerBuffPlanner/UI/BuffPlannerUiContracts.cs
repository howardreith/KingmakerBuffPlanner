using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KingmakerBuffPlanner.UI
{
    public enum HudInstallAttemptResult
    {
        None,
        NoActiveHud,
        RetryableNotReady,
        CandidateCreated,
        CandidatePending,
        AlreadyInstalled,
        StaleCandidateDisposed
    }

    public enum HudCandidateTickResult
    {
        None,
        Pending,
        Installed,
        Expired,
        Stale
    }

    public enum HudInstallationState
    {
        NoHud,
        RetryPending,
        CandidatePending,
        Installed,
        CandidateExpired,
        StaleAnchor,
        Suspended
    }

    public enum HudInstallDispatchDecision
    {
        None,
        Dispatch
    }

    public sealed class HudInstallInvalidationGate
    {
        public const int DefaultRetryIntervalFrames = 30;

        private readonly int _retryIntervalFrames;
        private bool _requested = true;
        private bool _retryScheduled;
        private bool _suspended;
        private int _hostIdentity;
        private bool _hostActive;
        private int _retryFramesRemaining;

        public HudInstallInvalidationGate()
            : this(DefaultRetryIntervalFrames)
        {
        }

        public HudInstallInvalidationGate(int retryIntervalFrames)
        {
            if (retryIntervalFrames < 1)
                throw new ArgumentOutOfRangeException("retryIntervalFrames");
            _retryIntervalFrames = retryIntervalFrames;
            State = HudInstallationState.NoHud;
            LastTransition = "initial-request";
        }

        public bool IsRequested { get { return _requested; } }
        public bool IsRetryScheduled { get { return _retryScheduled; } }
        public bool IsSuspended { get { return _suspended; } }
        public int HostIdentity { get { return _hostIdentity; } }
        public bool HostActive { get { return _hostActive; } }
        public int RetryFramesRemaining { get { return _retryFramesRemaining; } }
        public int RetryIntervalFrames { get { return _retryIntervalFrames; } }
        public int RequestCount { get; private set; }
        public int AttemptCount { get; private set; }
        public int RetryArmCount { get; private set; }
        public int RetryDispatchCount { get; private set; }
        public int HostTransitionCount { get; private set; }
        public int SuspendCount { get; private set; }
        public HudInstallationState State { get; private set; }
        public HudInstallAttemptResult LastAttemptResult { get; private set; }
        public HudCandidateTickResult LastCandidateResult { get; private set; }
        public string LastTransition { get; private set; }

        public void Request()
        {
            Request("lifecycle-invalidation");
        }

        public bool Request(string reason)
        {
            if (_suspended)
            {
                LastTransition = "request-ignored-while-suspended:" + NormalizeReason(reason);
                return false;
            }
            if (_requested && !_retryScheduled && _retryFramesRemaining == 0) return false;
            _requested = true;
            _retryScheduled = false;
            _retryFramesRemaining = 0;
            RequestCount++;
            State = _hostActive ? HudInstallationState.RetryPending : HudInstallationState.NoHud;
            LastTransition = "dispatch-requested:" + NormalizeReason(reason);
            return true;
        }

        public void Cancel()
        {
            Suspend("cancelled");
        }

        public bool Suspend(string reason)
        {
            if (_suspended) return false;
            _suspended = true;
            _requested = false;
            _retryScheduled = false;
            _retryFramesRemaining = 0;
            State = HudInstallationState.Suspended;
            SuspendCount++;
            LastTransition = "suspended:" + NormalizeReason(reason);
            return true;
        }

        public bool ResumeAndRequest(string reason)
        {
            bool resumed = _suspended;
            _suspended = false;
            bool requested = Request(reason);
            if (resumed && !requested)
            {
                _requested = true;
                _retryScheduled = false;
                _retryFramesRemaining = 0;
                RequestCount++;
                State = _hostActive ? HudInstallationState.RetryPending : HudInstallationState.NoHud;
                LastTransition = "resumed-and-requested:" + NormalizeReason(reason);
                requested = true;
            }
            else if (resumed)
                LastTransition = "resumed-and-requested:" + NormalizeReason(reason);
            return requested;
        }

        public HudInstallDispatchDecision ObserveHost(int hostIdentity, bool hostActive)
        {
            bool hostChanged = hostIdentity != _hostIdentity || hostActive != _hostActive;
            int previousIdentity = _hostIdentity;
            bool previousActive = _hostActive;
            _hostIdentity = hostIdentity;
            _hostActive = hostActive;
            if (hostChanged)
            {
                HostTransitionCount++;
                if (_suspended)
                    LastTransition = "host-observed-while-suspended:" + hostIdentity;
                else if (!hostActive)
                {
                    _requested = true;
                    _retryScheduled = false;
                    _retryFramesRemaining = 0;
                    State = HudInstallationState.NoHud;
                    LastTransition = "active-hud-absent:" + hostIdentity;
                }
                else
                {
                    ArmImmediateHostDispatch(previousIdentity, previousActive);
                }
            }
            if (_suspended) return HudInstallDispatchDecision.None;
            if (!hostActive)
            {
                State = HudInstallationState.NoHud;
                return HudInstallDispatchDecision.None;
            }
            if (!_requested) return HudInstallDispatchDecision.None;
            if (_retryFramesRemaining > 0)
            {
                _retryFramesRemaining--;
                return HudInstallDispatchDecision.None;
            }
            bool retryDispatch = _retryScheduled;
            _requested = false;
            _retryScheduled = false;
            AttemptCount++;
            if (retryDispatch) RetryDispatchCount++;
            State = HudInstallationState.RetryPending;
            LastTransition = (retryDispatch ? "retry-dispatched:" : "install-dispatched:") +
                hostIdentity;
            return HudInstallDispatchDecision.Dispatch;
        }

        public void RecordAttemptResult(HudInstallAttemptResult result)
        {
            LastAttemptResult = result;
            switch (result)
            {
                case HudInstallAttemptResult.NoActiveHud:
                    _requested = true;
                    _retryScheduled = false;
                    _retryFramesRemaining = 0;
                    State = HudInstallationState.NoHud;
                    LastTransition = "attempt:no-active-hud";
                    break;
                case HudInstallAttemptResult.RetryableNotReady:
                    ScheduleRetry(HudInstallationState.RetryPending,
                        "attempt:retryable-not-ready");
                    break;
                case HudInstallAttemptResult.CandidateCreated:
                    ClearRequest(HudInstallationState.CandidatePending,
                        "attempt:candidate-created");
                    break;
                case HudInstallAttemptResult.CandidatePending:
                    ClearRequest(HudInstallationState.CandidatePending,
                        "attempt:candidate-pending");
                    break;
                case HudInstallAttemptResult.AlreadyInstalled:
                    ClearRequest(HudInstallationState.Installed,
                        "attempt:already-installed");
                    break;
                case HudInstallAttemptResult.StaleCandidateDisposed:
                    ScheduleRetry(HudInstallationState.StaleAnchor,
                        "attempt:stale-candidate-disposed");
                    break;
                case HudInstallAttemptResult.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("result");
            }
        }

        public void RecordCandidateResult(HudCandidateTickResult result)
        {
            LastCandidateResult = result;
            switch (result)
            {
                case HudCandidateTickResult.None:
                    return;
                case HudCandidateTickResult.Pending:
                    if (State != HudInstallationState.CandidatePending)
                    {
                        State = HudInstallationState.CandidatePending;
                        LastTransition = "candidate:pending";
                    }
                    _requested = false;
                    _retryScheduled = false;
                    _retryFramesRemaining = 0;
                    return;
                case HudCandidateTickResult.Installed:
                    ClearRequest(HudInstallationState.Installed, "candidate:installed");
                    return;
                case HudCandidateTickResult.Expired:
                    ScheduleRetry(HudInstallationState.CandidateExpired,
                        "candidate:expired");
                    return;
                case HudCandidateTickResult.Stale:
                    ScheduleRetry(HudInstallationState.StaleAnchor, "candidate:stale");
                    return;
                default:
                    throw new ArgumentOutOfRangeException("result");
            }
        }

        private void ArmImmediateHostDispatch(int previousIdentity, bool previousActive)
        {
            if (!_requested || _retryScheduled || _retryFramesRemaining != 0) RequestCount++;
            _requested = true;
            _retryScheduled = false;
            _retryFramesRemaining = 0;
            State = HudInstallationState.RetryPending;
            string transition = !previousActive ? "active-hud-detected:" :
                previousIdentity != _hostIdentity ? "active-hud-replaced:" :
                "active-hud-reactivated:";
            LastTransition = transition + _hostIdentity;
        }

        private void ScheduleRetry(HudInstallationState state, string reason)
        {
            _requested = true;
            _retryScheduled = _hostActive && !_suspended;
            _retryFramesRemaining = _retryScheduled ? _retryIntervalFrames : 0;
            RetryArmCount++;
            State = state;
            LastTransition = "retry-rearmed:" + reason;
        }

        private void ClearRequest(HudInstallationState state, string transition)
        {
            _requested = false;
            _retryScheduled = false;
            _retryFramesRemaining = 0;
            State = state;
            LastTransition = transition;
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason;
        }
    }

    public sealed class HudHostingChainSnapshot
    {
        public HudHostingChainSnapshot(
            bool ownedRootExists,
            bool rootHasParent,
            bool rootActive,
            bool anchorExists,
            bool anchorActive,
            bool nativeClusterExists,
            bool nativeClusterActive,
            bool activeHudExists,
            bool activeHudActive,
            bool rootParentIsNativeCluster,
            bool anchorBelongsToActiveHud,
            bool nativeClusterBelongsToActiveHud,
            bool rootBelongsToActiveHud,
            bool nativeRaycasterActive)
        {
            OwnedRootExists = ownedRootExists;
            RootHasParent = rootHasParent;
            RootActive = rootActive;
            AnchorExists = anchorExists;
            AnchorActive = anchorActive;
            NativeClusterExists = nativeClusterExists;
            NativeClusterActive = nativeClusterActive;
            ActiveHudExists = activeHudExists;
            ActiveHudActive = activeHudActive;
            RootParentIsNativeCluster = rootParentIsNativeCluster;
            AnchorBelongsToActiveHud = anchorBelongsToActiveHud;
            NativeClusterBelongsToActiveHud = nativeClusterBelongsToActiveHud;
            RootBelongsToActiveHud = rootBelongsToActiveHud;
            NativeRaycasterActive = nativeRaycasterActive;
        }

        public bool OwnedRootExists { get; private set; }
        public bool RootHasParent { get; private set; }
        public bool RootActive { get; private set; }
        public bool AnchorExists { get; private set; }
        public bool AnchorActive { get; private set; }
        public bool NativeClusterExists { get; private set; }
        public bool NativeClusterActive { get; private set; }
        public bool ActiveHudExists { get; private set; }
        public bool ActiveHudActive { get; private set; }
        public bool RootParentIsNativeCluster { get; private set; }
        public bool AnchorBelongsToActiveHud { get; private set; }
        public bool NativeClusterBelongsToActiveHud { get; private set; }
        public bool RootBelongsToActiveHud { get; private set; }
        public bool NativeRaycasterActive { get; private set; }
    }

    public static class HudHostingChainValidator
    {
        public static bool IsViable(HudHostingChainSnapshot snapshot, out string failure)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            failure = string.Empty;
            if (!snapshot.OwnedRootExists) failure = "owned-root-missing";
            else if (!snapshot.RootHasParent) failure = "owned-root-parent-missing";
            else if (!snapshot.RootActive) failure = "owned-root-inactive";
            else if (!snapshot.AnchorExists) failure = "anchor-controller-missing";
            else if (!snapshot.AnchorActive) failure = "anchor-controller-inactive";
            else if (!snapshot.NativeClusterExists) failure = "native-cluster-missing";
            else if (!snapshot.NativeClusterActive) failure = "native-cluster-inactive";
            else if (!snapshot.ActiveHudExists) failure = "active-hud-missing";
            else if (!snapshot.ActiveHudActive) failure = "active-hud-inactive";
            else if (!snapshot.RootParentIsNativeCluster) failure = "owned-root-reparented";
            else if (!snapshot.AnchorBelongsToActiveHud) failure = "anchor-outside-active-hud";
            else if (!snapshot.NativeClusterBelongsToActiveHud) failure = "native-cluster-outside-active-hud";
            else if (!snapshot.RootBelongsToActiveHud) failure = "owned-root-outside-active-hud";
            else if (!snapshot.NativeRaycasterActive) failure = "native-raycaster-inactive";
            return failure.Length == 0;
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

    public sealed class HudCandidateValidationGate
    {
        private readonly int _maximumFailureFrames;

        public HudCandidateValidationGate(int maximumFailureFrames)
        {
            if (maximumFailureFrames < 1)
                throw new ArgumentOutOfRangeException("maximumFailureFrames");
            _maximumFailureFrames = maximumFailureFrames;
        }

        public int MaximumFailureFrames { get { return _maximumFailureFrames; } }
        public int FailureFrames { get; private set; }

        public HudCandidateTickResult RecordValidation(bool valid)
        {
            if (valid) return HudCandidateTickResult.Installed;
            if (FailureFrames < _maximumFailureFrames) FailureFrames++;
            return FailureFrames >= _maximumFailureFrames
                ? HudCandidateTickResult.Expired
                : HudCandidateTickResult.Pending;
        }

        public void Reset()
        {
            FailureFrames = 0;
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

    public sealed class SetupOpenSoundGate
    {
        private bool _pending;

        public bool BeginHiddenToVisible()
        {
            if (_pending) return false;
            _pending = true;
            return true;
        }

        public bool CompleteVisible(bool visible)
        {
            if (!_pending || !visible) return false;
            _pending = false;
            return true;
        }

        public void Cancel()
        {
            _pending = false;
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
        public int SetupOpenSoundCount { get; private set; }
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
        public void RecordSetupOpenSound() { SetupOpenSoundCount++; }
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
