using System;
using System.Collections.Generic;

namespace KingmakerBuffPlanner.UI
{
    // Bounded re-resolution state. Recovery is capped per owner and never
    // re-enters, so a permanently missing donor degrades to fallback instead
    // of producing a resolve loop.
    internal sealed class NativeThemeRecovery
    {
        internal const int MaximumAttempts = 3;
        private object _owner;
        private bool _resolving;
        internal int Attempts { get; private set; }

        internal void Bind(object currentOwner)
        {
            if (ReferenceEquals(_owner, currentOwner)) return;
            _owner = currentOwner;
            Attempts = 0;
        }

        internal bool TryBegin(bool needsRecovery)
        {
            if (_resolving || !needsRecovery || _owner == null ||
                Attempts >= MaximumAttempts) return false;
            Attempts++;
            _resolving = true;
            return true;
        }

        internal void Complete() { _resolving = false; }

        internal void Reset()
        {
            _owner = null;
            Attempts = 0;
            _resolving = false;
        }
    }

    // Presentation-property applications only: bindings set sprites, fonts,
    // colors, and transitions on already-owned widgets. They never construct
    // controls or register command listeners, so a failed apply can always
    // fall back to the readable parchment style without losing function.
    internal sealed class NativeThemeBindings
    {
        private sealed class Binding
        {
            internal NativeThemeCapability Capability;
            internal Action<object[]> Apply;
            internal Action Fallback;
        }

        private readonly List<Binding> _bindings = new List<Binding>();
        private readonly Dictionary<NativeThemeCapability, NativeThemeResource> _applied =
            new Dictionary<NativeThemeCapability, NativeThemeResource>();
        private readonly Action<string> _diagnostic;

        internal NativeThemeBindings(Action<string> diagnostic)
        {
            _diagnostic = diagnostic ?? (ignored => { });
        }

        internal int Count { get { return _bindings.Count; } }

        internal void Add(NativeThemeCapability capability, Action<object[]> apply,
            Action fallback)
        {
            _bindings.Add(new Binding
            {
                Capability = capability,
                Apply = apply,
                Fallback = fallback
            });
        }

        internal void Clear()
        {
            _bindings.Clear();
            _applied.Clear();
        }

        internal void Apply(NativeThemeResolution resolution)
        {
            foreach (NativeThemeCapability capability in NativeThemeResolution.Capabilities)
            {
                NativeThemeResource resource = resolution == null
                    ? null : resolution.Get(capability);
                NativeThemeResource previous;
                if (_applied.TryGetValue(capability, out previous) &&
                    SameResource(previous, resource)) continue;
                if (resource != null)
                {
                    try
                    {
                        foreach (Binding binding in _bindings)
                            if (binding.Capability == capability)
                                binding.Apply(resource.Components);
                        _applied[capability] = resource;
                        continue;
                    }
                    catch (Exception exception)
                    {
                        resolution.Reject(capability,
                            "applying owned style failed: " + exception.Message +
                            "; " + resource.Identity);
                    }
                }
                _applied[capability] = null;
                foreach (Binding binding in _bindings)
                {
                    if (binding.Capability != capability) continue;
                    try { binding.Fallback(); }
                    catch (Exception exception)
                    {
                        _diagnostic("Native theme " + capability + " fallback failed: " +
                            exception.Message);
                    }
                }
            }
        }

        private static bool SameResource(NativeThemeResource first, NativeThemeResource second)
        {
            if (ReferenceEquals(first, second)) return true;
            if (first == null || second == null) return false;
            if (first.Components == null || second.Components == null) return false;
            if (first.Components.Length != second.Components.Length) return false;
            for (int index = 0; index < first.Components.Length; index++)
                if (!ReferenceEquals(first.Components[index], second.Components[index]))
                    return false;
            return true;
        }
    }
}
