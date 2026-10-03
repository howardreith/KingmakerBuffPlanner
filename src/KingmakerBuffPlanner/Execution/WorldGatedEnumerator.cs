using System;
using System.Collections;

namespace KingmakerBuffPlanner.Execution
{
    // Advances an execution routine only while the world runs (final review
    // A1): while the game is paused or a full-screen window (such as the
    // planner itself) holds it, a frame passes without advancing the routine,
    // so no executor confirmation window, counted in frames, is used up while
    // a submitted cast cannot land. The Classic run is pumped this way, as
    // the casting-first host is. Disposing it disposes the routine, whose
    // cleanup then runs.
    public sealed class WorldGatedEnumerator : IEnumerator, IDisposable
    {
        private readonly IEnumerator _inner;
        private readonly Func<bool> _worldRuns;
        private object _current;
        private bool _disposed;

        public WorldGatedEnumerator(IEnumerator inner, Func<bool> worldRuns)
        {
            _inner = inner ?? throw new ArgumentNullException("inner");
            _worldRuns = worldRuns ?? throw new ArgumentNullException("worldRuns");
        }

        // Frames the routine waited for the world.
        public int HeldFrames { get; private set; }

        public object Current { get { return _current; } }

        public bool MoveNext()
        {
            if (_disposed) return false;
            if (!_worldRuns())
            {
                HeldFrames++;
                _current = null;
                return true;
            }
            if (!_inner.MoveNext()) return false;
            _current = _inner.Current;
            return true;
        }

        public void Reset() { throw new NotSupportedException(); }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            IDisposable inner = _inner as IDisposable;
            if (inner != null) inner.Dispose();
        }
    }
}
