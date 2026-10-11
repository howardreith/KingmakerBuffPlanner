using System;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal enum NativeUiPauseRestoreStatus { Pending, Restored, TimedOut }

    // Native StopMode refuses to stop Pause beneath FullScreenUi and can
    // queue a valid request. Completion requires the observed original state.
    internal sealed class NativeUiPauseRestoration
    {
        private readonly bool _originalPause;
        private readonly long _timeoutMilliseconds;
        private bool _requested;
        private NativeUiPauseRestoreStatus _status;

        internal NativeUiPauseRestoration(bool originalPause, long timeoutMilliseconds)
        {
            if (timeoutMilliseconds <= 0) throw new ArgumentOutOfRangeException("timeoutMilliseconds");
            _originalPause = originalPause;
            _timeoutMilliseconds = timeoutMilliseconds;
        }

        internal NativeUiPauseRestoreStatus Observe(bool worldReleased, bool paused,
            long elapsedMilliseconds, Action<bool> restorePause)
        {
            if (_status != NativeUiPauseRestoreStatus.Pending) return _status;
            if (elapsedMilliseconds >= _timeoutMilliseconds)
                return _status = NativeUiPauseRestoreStatus.TimedOut;
            if (!worldReleased) return NativeUiPauseRestoreStatus.Pending;
            if (paused == _originalPause) return _status = NativeUiPauseRestoreStatus.Restored;
            if (!_requested)
            {
                if (restorePause == null) throw new ArgumentNullException("restorePause");
                restorePause(_originalPause);
                _requested = true;
            }
            return NativeUiPauseRestoreStatus.Pending;
        }
    }
}
