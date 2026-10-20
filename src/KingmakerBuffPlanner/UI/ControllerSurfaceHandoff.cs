using System;

namespace KingmakerBuffPlanner.UI
{
    // Source-specific close/release contracts are injected by thin native
    // adapters. No timeout ever grants permission to open over a native owner.
    internal sealed class ControllerSurfaceHandoff
    {
        private readonly Func<bool> _context;
        private readonly Func<bool> _source;
        private readonly Action _close;
        private readonly Func<bool> _released;
        private readonly Action _open;
        private readonly Action<string> _report;
        private readonly Func<string> _contextFailure;
        private readonly Func<bool> _destinationReady;
        private readonly Func<string> _destinationOutcome;
        private readonly ControllerActivationLatch _neutral = new ControllerActivationLatch();
        private bool _closed;
        private bool _done;
        private int _remainingFrames = 180;
        internal string Outcome { get; private set; }

        internal ControllerSurfaceHandoff(Func<bool> context, Func<bool> source,
            Action close, Func<bool> released, Action open,
            Action<string> report = null, Func<string> contextFailure = null,
            Func<bool> destinationReady = null, Func<string> destinationOutcome = null)
        { _context = context; _source = source; _close = close; _released = released; _open = open;
            _report = report; _contextFailure = contextFailure; _destinationReady = destinationReady;
            _destinationOutcome = destinationOutcome; }

        internal bool Tick(bool neutral, int frame)
        {
            if (_done) return true;
            if (!_context()) { Finish("handoff-canceled:" + (_contextFailure == null ? "context" : _contextFailure())); return true; }
            if (--_remainingFrames <= 0) { Finish("handoff-canceled:timeout"); return true; }
            if (!_closed && !_source()) { Finish("handoff-canceled:source"); return true; }
            if (!_neutral.CanRelease(neutral, frame)) return false;
            if (!_closed)
            {
                _closed = true;
                _close();
                _neutral.Reset();
                return false;
            }
            if (!_released()) return false;
            if (_report != null) _report("source-released");
            _done = true;
            try
            {
                _open();
                Finish(_destinationOutcome == null
                    ? (_destinationReady == null || _destinationReady() ?
                        "destination-opened" : "handoff-canceled:destination-unavailable")
                    : _destinationOutcome());
            }
            catch
            {
                Finish("handoff-canceled:destination-exception");
                throw;
            }
            return true;
        }

        internal void Cancel() { Finish("handoff-canceled:cancel"); }

        private void Finish(string outcome)
        {
            if (Outcome != null) return;
            _done = true; Outcome = outcome;
            if (_report != null) _report(outcome);
        }
    }

    // Called only after the native menu has released its input layer. The
    // running state can change while the handoff waits for neutral controls.
    internal static class ControllerCardDestination
    {
        internal static string Open(Func<bool> isRunning, Action openQuick, Action openEditor,
            Func<bool> quickVisible, Func<bool> editorVisible, Func<bool> classicSelected)
        {
            bool running = isRunning();
            if (running) openQuick();
            else openEditor();
            if (editorVisible()) return "destination-opened:editor";
            if (quickVisible()) return running ? "destination-opened:running-controls" :
                classicSelected() ? "destination-opened:classic-review" : "destination-opened:editor-recovery";
            return "handoff-canceled:destination-unavailable";
        }
    }
}
