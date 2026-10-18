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
        private readonly ControllerActivationLatch _neutral = new ControllerActivationLatch();
        private bool _closed;
        private bool _done;
        private int _remainingFrames = 180;

        internal ControllerSurfaceHandoff(Func<bool> context, Func<bool> source,
            Action close, Func<bool> released, Action open)
        { _context = context; _source = source; _close = close; _released = released; _open = open; }

        internal bool Tick(bool neutral, int frame)
        {
            if (_done) return true;
            if (!_context() || --_remainingFrames <= 0) { _done = true; return true; }
            if (!_closed && !_source()) { _done = true; return true; }
            if (!_neutral.CanRelease(neutral, frame)) return false;
            if (!_closed)
            {
                _closed = true;
                _close();
                _neutral.Reset();
                return false;
            }
            if (!_released()) return false;
            _done = true;
            _open();
            return true;
        }

        internal void Cancel() { _done = true; }
    }
}
