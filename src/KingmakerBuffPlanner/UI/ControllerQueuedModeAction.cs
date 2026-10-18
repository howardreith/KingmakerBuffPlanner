using System;
using System.Collections.Generic;

namespace KingmakerBuffPlanner.UI
{
    // Identity-only ownership for an action the native API just enqueued.
    // Never restore/clear a whole queue or remove a foreign delegate.
    internal sealed class ControllerQueuedModeAction
    {
        private readonly IList<Action> _queue;
        private readonly Action _action;
        internal ControllerQueuedModeAction(IList<Action> queue, Action action)
        { _queue = queue; _action = action; }
        internal bool Pending { get { return _queue.Contains(_action); } }
        internal void Cancel() { lock (_queue) _queue.Remove(_action); }
        internal bool Guard(Func<bool> stillOwns)
        {
            lock (_queue)
            {
                int index = _queue.IndexOf(_action);
                if (index < 0) return false;
                _queue[index] = () => { if (stillOwns()) _action(); };
                return true;
            }
        }
    }
}
