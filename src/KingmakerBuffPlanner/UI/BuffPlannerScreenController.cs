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
        private readonly DeferredUiReadinessGate _readiness = new DeferredUiReadinessGate(2);

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
        internal string LastFailure { get; private set; }

        internal bool Open()
        {
            if (_disposed || !_state.BeginPresentation()) return false;
            try
            {
                _session.Refresh();
                if (StaticCanvas.Instance == null)
                    throw new InvalidOperationException("Kingmaker campaign UI is not available.");
                _view = new BuffPlannerScreenView(StaticCanvas.Instance, _session,
                    _diagnostics, () => Close(), _quickExecute);
                _readiness.Reset();
                _validationTick = 0;
                LastFailure = "candidate-awaiting-deferred-readiness";
                _log.Info("[KBP-BOOT] full-screen install attempted;root=" +
                    _view.RootObject.GetInstanceID() + ";deferredFrames=2;inputLease=false.");
                return true;
            }
            catch (Exception exception)
            {
                FailOpen("construction:" + exception.Message, exception);
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
                _readiness.Reset();
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
            if (_state.State == PlannerScreenLifecycleState.OpeningPresentation)
            {
                if (_view == null || !_view.IsAlive || StaticCanvas.Instance == null)
                {
                    FailOpen("candidate-or-campaign-ui-lost-before-validation", null);
                    return;
                }
                if (!_readiness.ObserveFrame()) return;
                try
                {
                    LastValidation = _view.ValidatePresentation();
                    _log.Info("[KBP-BOOT] full-screen presentation phase A;" + LastValidation);
                    if (!LastValidation.Valid)
                        throw new InvalidOperationException("Planner presentation validation failed: " +
                            LastValidation.Failure);
                    CatalogLayoutDiagnostics catalog = _view.GetCatalogDiagnostics();
                    _log.Info("[KBP-CATALOG] presentation phase A;" + catalog + ".");
                    if (catalog == null || catalog.Filters == null)
                        throw new InvalidOperationException("Catalog diagnostics are absent.");
                    if (catalog.Filters.VisibleViewModels > 0 &&
                        (catalog.InstantiatedRows < catalog.ActiveRows ||
                        catalog.ActiveRows < 1 || catalog.VisibleRows < 1 ||
                        catalog.ContentHeight <= 1 || !catalog.SelectedDetailsBound))
                        throw new InvalidOperationException("Catalog layout validation failed: " + catalog);
                    if (!string.IsNullOrEmpty(catalog.BindingFailure))
                        throw new InvalidOperationException("Catalog binding failed: " +
                            catalog.BindingFailure);
                    _diagnostics.RecordPresentationValidated();
                    _state.AcquireInputLease();
                    _diagnostics.RecordInputLeaseAcquired();
                    LastValidation = _view.ValidatePresentation();
                    _log.Info("[KBP-BOOT] full-screen presentation phase B;" + LastValidation);
                    if (!LastValidation.Valid)
                        throw new InvalidOperationException(
                            "Planner presentation became invalid after input lease: " +
                            LastValidation.Failure);
                    catalog = _view.GetCatalogDiagnostics();
                    _log.Info("[KBP-CATALOG] presentation phase B;" + catalog + ".");
                    LastFailure = string.Empty;
                    _log.Info("[KBP-BOOT] full-screen install succeeded;root=" +
                        _view.RootObject.GetInstanceID() + ";inputLease=true;active=" +
                        _view.RootObject.activeInHierarchy + ".");
                }
                catch (Exception exception)
                {
                    FailOpen(LastValidation == null ? exception.Message : LastValidation.Failure,
                        exception);
                }
                return;
            }
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

        private void FailOpen(string reason, Exception exception)
        {
            bool hadLease = _state.HasInputLease;
            LastFailure = string.IsNullOrEmpty(reason) ? "unknown" : reason;
            if (_view != null) _view.Dispose();
            _view = null;
            _readiness.Reset();
            _state.Rollback();
            if (hadLease) _diagnostics.RecordInputLeaseReleased();
            var failure = exception ?? new InvalidOperationException(LastFailure);
            _log.Error("[KBP-BOOT] full-screen install failed;reason=" + LastFailure +
                ";retryable=true;inputLease=" + hadLease + ".", failure);
            _log.Info("Buff Planner UI is unavailable: " + LastFailure);
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
