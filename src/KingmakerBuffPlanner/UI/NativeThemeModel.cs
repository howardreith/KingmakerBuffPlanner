using System;
using System.Collections.Generic;
using System.Text;

namespace KingmakerBuffPlanner.UI
{
    // Independent presentation capabilities. Each one resolves, validates,
    // applies, and falls back on its own so a missing donor never disables
    // the planner or degrades an unrelated surface.
    internal enum NativeThemeCapability
    {
        Paper,
        Buttons,
        ButtonText,
        Body,
        Input,
        Scrollbar,
        Ornament,
        Sound
    }

    internal enum NativeThemeComponent
    {
        Image,
        Button,
        Text,
        InputField,
        Scrollbar
    }

    internal enum NativeThemeLocatorKind
    {
        ProvenPath,
        CandidatePath,
        BoundedScan
    }

    // Nodes and components are opaque here; only the Unity adapter touches
    // engine objects. Deterministic tests supply fake hierarchies.
    internal interface INativeThemeSource
    {
        bool IsAlive(object value);
        bool SameNode(object first, object second);
        string Name(object node);
        object Parent(object node);
        int ChildCount(object node);
        object Child(object node, int index);
        object[] Components(object node, NativeThemeComponent component);
        // The sound capability has no hierarchy donor; the adapter supplies
        // the validated native sound player component directly.
        NativeThemeResource SoundResource();
        // A capability is committed only after all of its components pass
        // adapter validation; throwing rejects just that capability.
        void Validate(NativeThemeCapability capability, object[] components);
    }

    internal sealed class NativeThemeResource
    {
        internal object[] Nodes;
        internal object[] Components;
        internal string Identity;
    }

    internal sealed class NativeThemeLocator
    {
        internal NativeThemeLocator(NativeThemeLocatorKind kind, string description)
        {
            Kind = kind;
            Description = description;
        }
        internal NativeThemeLocatorKind Kind;
        internal string Description;
        internal string Provenance()
        {
            return Kind == NativeThemeLocatorKind.ProvenPath ? "proven" :
                Kind == NativeThemeLocatorKind.CandidatePath ? "candidate" : "scan";
        }
    }

    internal sealed class NativeThemeResolution
    {
        internal static readonly NativeThemeCapability[] Capabilities =
            (NativeThemeCapability[])Enum.GetValues(typeof(NativeThemeCapability));

        private readonly Dictionary<NativeThemeCapability, NativeThemeResource> _resources =
            new Dictionary<NativeThemeCapability, NativeThemeResource>();
        private readonly Dictionary<NativeThemeCapability, string> _failures =
            new Dictionary<NativeThemeCapability, string>();
        private readonly Dictionary<NativeThemeCapability, NativeThemeLocator> _locators =
            new Dictionary<NativeThemeCapability, NativeThemeLocator>();
        private readonly object _owner;

        internal NativeThemeResolution(object owner)
        {
            _owner = owner;
        }

        internal object Owner { get { return _owner; } }
        internal int AvailableCount { get { return _resources.Count; } }
        internal bool IsAvailable(NativeThemeCapability capability)
        {
            return _resources.ContainsKey(capability);
        }

        internal NativeThemeResource Get(NativeThemeCapability capability)
        {
            NativeThemeResource value;
            return _resources.TryGetValue(capability, out value) ? value : null;
        }

        internal string Failure(NativeThemeCapability capability)
        {
            string value;
            return _failures.TryGetValue(capability, out value) ? value : null;
        }

        internal void Accept(NativeThemeCapability capability, NativeThemeResource resource,
            NativeThemeLocator locator)
        {
            _resources[capability] = resource;
            _locators[capability] = locator;
            _failures.Remove(capability);
        }

        internal void Reject(NativeThemeCapability capability, string reason)
        {
            _resources.Remove(capability);
            _failures[capability] = reason;
        }

        // Cached donors must still live under the recorded owner; anything
        // destroyed or reparented is discarded so a later bounded retry can
        // rebuild it instead of applying stale native state.
        internal bool DiscardStale(INativeThemeSource source)
        {
            bool changed = false;
            var lookup = new NativeThemeDonorLookup(source);
            foreach (NativeThemeCapability capability in Capabilities)
            {
                NativeThemeResource resource = Get(capability);
                if (resource == null) continue;
                bool alive = source.IsAlive(_owner);
                if (resource.Nodes != null)
                    foreach (object node in resource.Nodes)
                        alive &= lookup.IsUnderOwner(node, _owner);
                if (resource.Components != null)
                    foreach (object component in resource.Components)
                        alive &= source.IsAlive(component);
                if (!alive)
                {
                    Reject(capability, "cached donor destroyed or moved outside its owner; " +
                        resource.Identity);
                    changed = true;
                }
            }
            return changed;
        }

        internal string Summary
        {
            get
            {
                var builder = new StringBuilder();
                foreach (NativeThemeCapability capability in Capabilities)
                {
                    if (builder.Length > 0) builder.Append("; ");
                    NativeThemeResource resource = Get(capability);
                    if (resource != null)
                    {
                        NativeThemeLocator locator;
                        builder.Append(capability).Append("=ok(")
                            .Append(_locators.TryGetValue(capability, out locator)
                                ? locator.Provenance() : "?").Append(')');
                    }
                    else
                    {
                        string failure;
                        builder.Append(capability).Append("=fallback(")
                            .Append(_failures.TryGetValue(capability, out failure)
                                ? failure : "not attempted").Append(')');
                    }
                }
                return builder.ToString();
            }
        }
    }
}
