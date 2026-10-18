using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerBuffPlanner.UI
{
    // Own only the entry registrations we add to borrowed native layers.
    // Native/device registration is the boundary; identity and lifetime are
    // shared by production and the retained-layer behavior tests.
    internal sealed class ControllerEntryRegistry<TLayer, TBinding>
        where TLayer : class where TBinding : class
    {
        private sealed class Entry
        {
            internal bool Active;
            internal TBinding[] Bindings;
        }

        private readonly Dictionary<TLayer, Entry> _entries = new Dictionary<TLayer, Entry>();
        private readonly Action<TLayer, TBinding> _detach;

        internal ControllerEntryRegistry(Action<TLayer, TBinding> detach) { _detach = detach; }

        internal bool Ensure(TLayer layer, IEnumerable<TBinding> currentBindings,
            Func<TBinding, bool> usesEntryAction, Func<Func<bool>, TBinding[]> register)
        {
            TBinding[] current = currentBindings.ToArray();
            Entry previous;
            if (_entries.TryGetValue(layer, out previous))
            {
                if (previous.Active && previous.Bindings.All(owned => current.Any(bind => ReferenceEquals(bind, owned))) &&
                    !current.Any(bind => usesEntryAction(bind) &&
                        !previous.Bindings.Any(owned => ReferenceEquals(owned, bind)))) return true;
                Remove(layer);
                current = currentBindings.ToArray();
            }
            if (current.Any(usesEntryAction)) return false;
            var entry = new Entry();
            entry.Bindings = register(() => entry.Active);
            _entries.Add(layer, entry);
            entry.Active = true;
            return true;
        }

        internal void Remove(TLayer layer)
        {
            Entry entry;
            if (!_entries.TryGetValue(layer, out entry)) return;
            // Gate already-captured callbacks before detaching the device.
            entry.Active = false;
            var failures = new List<Exception>();
            foreach (TBinding binding in entry.Bindings)
                try { _detach(layer, binding); }
                catch (Exception exception) { failures.Add(exception); }
            if (failures.Count != 0) throw new AggregateException("Controller entry detach failed.", failures);
            _entries.Remove(layer);
        }

        internal void Clear()
        {
            var failures = new List<Exception>();
            foreach (TLayer layer in _entries.Keys.ToArray())
                try { Remove(layer); }
                catch (Exception exception) { failures.Add(exception); }
            if (failures.Count != 0) throw new AggregateException("Controller entry cleanup failed.", failures);
        }
    }
}
