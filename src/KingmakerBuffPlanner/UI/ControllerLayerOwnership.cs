using System;
using System.Collections.Generic;

namespace KingmakerBuffPlanner.UI
{
    // Own only the pushed layer, never a saved native stack/fullscreen state.
    // Native takeover is effective immediately, even before our next update.
    internal sealed class ControllerLayerOwnership : IDisposable
    {
        private readonly object _layer;
        private readonly Func<object> _top;
        private readonly HashSet<object> _bindings;
        private IDisposable _receipt;

        internal ControllerLayerOwnership(object layer, Func<object> top, IEnumerable<object> bindings)
        {
            _layer = layer;
            _top = top;
            _bindings = new HashSet<object>(bindings);
        }

        internal bool IsCurrent { get { return ReferenceEquals(_layer, _top()); } }

        internal void Acquired(IDisposable receipt)
        {
            _receipt = receipt ?? throw new InvalidOperationException("Native input ownership returned no receipt.");
        }

        internal bool Allows(object binding)
        {
            return !IsCurrent || _bindings.Contains(binding);
        }

        public void Dispose()
        {
            IDisposable receipt = _receipt;
            _receipt = null;
            if (receipt != null) receipt.Dispose();
        }
    }
}
