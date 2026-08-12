using System;
using Kingmaker.UI;
using KingmakerBuffPlanner.Infrastructure;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class BuffPlannerScreenController : IDisposable
    {
        private readonly PlannerUiSession _session;
        private readonly BuffPlannerUiLifecycleDiagnostics _diagnostics;
        private readonly ModLog _log;
        private readonly PlannerScreenStateMachine _state;
        private readonly Action<string> _quickExecute;
        private BuffPlannerScreenView _view;
        private bool _disposed;
        private int _validationTick;

        internal BuffPlannerScreenController(
            PlannerUiSession session,
            BuffPlannerUiLifecycleDiagnostics diagnostics,
            ModLog log,
            Action<string> quickExecute)
        {
            _session = session ?? throw new ArgumentNullException("session");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
            _log = log ?? throw new ArgumentNullException("log");
            _quickExecute = quickExecute ?? throw new ArgumentNullException("quickExecute");
            _state = new PlannerScreenStateMachine(() => BuffPlannerInputLease.Acquire(
                new KingmakerPlannerInputBoundary()));
        }

        internal bool IsOpen { get { return _state.IsOpen; } }
        internal PlannerScreenLifecycleState LifecycleState { get { return _state.State; } }
        internal BuffPlannerScreenView View { get { return _view; } }
        internal PlannerPresentationValidation LastValidation { get; private set; }

        internal bool Open()
        {
            if (_disposed || !_state.BeginPresentation()) return false;
            BuffPlannerScreenView candidate = null;
            try
            {
                _session.Refresh();
                if (StaticCanvas.Instance == null)
                    throw new InvalidOperationException("Kingmaker campaign UI is not available.");
                candidate = new BuffPlannerScreenView(StaticCanvas.Instance, _session,
                    _diagnostics, () => Close(), _quickExecute);
                LastValidation = candidate.ValidatePresentation();
                _log.Info("Buff Planner presentation phase A: " + LastValidation);
                if (!LastValidation.Valid)
                    throw new InvalidOperationException("Planner presentation validation failed: " +
                        LastValidation.Failure);
                _diagnostics.RecordPresentationValidated();
                _view = candidate;
                _validationTick = 0;
                _state.AcquireInputLease();
                _diagnostics.RecordInputLeaseAcquired();
                LastValidation = candidate.ValidatePresentation();
                _log.Info("Buff Planner presentation phase B: " + LastValidation);
                if (!LastValidation.Valid)
                    throw new InvalidOperationException("Planner presentation became invalid after input lease: " +
                        LastValidation.Failure);
                return true;
            }
            catch (Exception exception)
            {
                bool hadLease = _state.HasInputLease;
                if (candidate != null) candidate.Dispose();
                _view = null;
                _state.Rollback();
                if (hadLease) _diagnostics.RecordInputLeaseReleased();
                _log.Error("Buff Planner screen open failed.", exception);
                return false;
            }
        }

        internal bool Close()
        {
            if (_state.State == PlannerScreenLifecycleState.Closed) return false;
            bool hadLease = _state.HasInputLease;
            try
            {
                if (_view != null) _view.Dispose();
                _view = null;
            }
            finally
            {
                _state.Close();
                if (hadLease) _diagnostics.RecordInputLeaseReleased();
            }
            return true;
        }

        internal void Present(QuickExecutionResult result)
        {
            if (_view != null) _view.ShowResult(result);
        }

        internal void Tick()
        {
            if (_state.IsOpen)
            {
                if (_view == null || !_view.IsAlive || StaticCanvas.Instance == null)
                    Close();
                else
                {
                    if (++_validationTick < 30) return;
                    _validationTick = 0;
                    PlannerPresentationValidation validation = _view.ValidatePresentation();
                    if (!validation.Valid)
                    {
                        LastValidation = validation;
                        _log.Error("Buff Planner presentation lost visibility.",
                            new InvalidOperationException(validation.ToString()));
                        Close();
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Close();
            _state.Dispose();
        }
    }
}
