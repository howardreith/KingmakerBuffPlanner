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
            _state = new PlannerScreenStateMachine(() =>
            {
                BuffPlannerInputLease lease = BuffPlannerInputLease.Acquire(
                    new KingmakerPlannerInputBoundary());
                _diagnostics.RecordInputLeaseAcquired();
                return lease;
            });
        }

        internal bool IsOpen { get { return _state.IsOpen; } }
        internal BuffPlannerScreenView View { get { return _view; } }

        internal bool Open()
        {
            if (_disposed || _state.IsOpen) return false;
            try
            {
                _state.Open();
                _session.Refresh();
                if (StaticCanvas.Instance == null)
                    throw new InvalidOperationException("Kingmaker campaign UI is not available.");
                _view = new BuffPlannerScreenView(StaticCanvas.Instance, _session,
                    _diagnostics, () => Close(), _quickExecute);
                return true;
            }
            catch (Exception exception)
            {
                if (_view != null) _view.Dispose();
                _view = null;
                if (_state.Close()) _diagnostics.RecordInputLeaseReleased();
                _log.Error("Buff Planner screen open failed.", exception);
                return false;
            }
        }

        internal bool Close()
        {
            if (!_state.IsOpen) return false;
            try
            {
                if (_view != null) _view.Dispose();
                _view = null;
            }
            finally
            {
                if (_state.Close()) _diagnostics.RecordInputLeaseReleased();
            }
            return true;
        }

        internal void Present(QuickExecutionResult result)
        {
            if (_view != null) _view.ShowResult(result);
        }

        internal void Tick()
        {
            if (_state.IsOpen && (_view == null || !_view.IsAlive || StaticCanvas.Instance == null))
                Close();
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
