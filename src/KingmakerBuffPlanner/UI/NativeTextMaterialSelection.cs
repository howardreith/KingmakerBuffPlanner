using System;
using System.Collections.Generic;

namespace KingmakerBuffPlanner.UI
{
    // TextMeshPro can keep several materials with the same role name alive.
    // Fallback submeshes may bind one of those copies to another font atlas.
    // A role material is safe for the planner only when it belongs to the
    // selected body font's atlas.
    internal static class NativeTextMaterialSelection
    {
        internal static bool Matches(string expectedName, string candidateName,
            int bodyAtlasInstanceId, int candidateAtlasInstanceId)
        {
            return !string.IsNullOrEmpty(expectedName) &&
                string.Equals(candidateName, expectedName, StringComparison.Ordinal) &&
                bodyAtlasInstanceId != 0 &&
                candidateAtlasInstanceId == bodyAtlasInstanceId;
        }
    }

    // Per-workspace ownership. Unity's liveness check is injected because a
    // destroyed native object can still have a non-null managed reference.
    internal sealed class WorkspaceTextMaterials<T> : IDisposable where T : class
    {
        private readonly Dictionary<string, T> _owned = new Dictionary<string, T>();
        private readonly Func<T, bool> _alive;
        private readonly Action<T> _destroy;
        private bool _disposed;

        internal WorkspaceTextMaterials(Func<T, bool> alive, Action<T> destroy)
        { _alive = alive; _destroy = destroy; }

        internal T Get(string role, Func<T> create)
        {
            if (_disposed) throw new ObjectDisposedException("workspace text materials");
            T material;
            if (_owned.TryGetValue(role, out material) && _alive(material)) return material;
            material = create();
            if (!_alive(material)) throw new InvalidOperationException("Native text material creation failed.");
            _owned[role] = material;
            return material;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (T material in _owned.Values) if (_alive(material)) _destroy(material);
            _owned.Clear();
        }
    }
}
