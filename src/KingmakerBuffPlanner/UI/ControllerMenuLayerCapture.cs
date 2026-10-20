using System.Collections.Generic;

namespace KingmakerBuffPlanner.UI
{
    // A GetInputLayer observation belongs to a menu and the exact layer it
    // returned, even when the native card has not been constructed yet.
    internal sealed class ControllerMenuLayerCapture<TMenu, TLayer>
        where TMenu : class where TLayer : class
    {
        private sealed class Capture
        {
            internal TLayer Source;
            internal TLayer Below;
        }

        private readonly Dictionary<TMenu, Capture> _captures = new Dictionary<TMenu, Capture>();

        internal bool Observe(TMenu menu, TLayer source, TLayer current, TLayer baseLayer, IList<TLayer> layers)
        {
            if (menu == null || source == null || layers == null) return false;
            int index = -1;
            for (int i = 0; i < layers.Count; i++)
                if (ReferenceEquals(layers[i], source)) { index = i; break; }
            // An already pushed source has its own real predecessor. On a
            // repeated GetInputLayer observation, never record it as Below.
            if (index < 0 && ReferenceEquals(current, source))
            { _captures.Remove(menu); return false; }
            TLayer below = index == 0 ? baseLayer : index > 0 ? layers[index - 1] : current;
            _captures[menu] = new Capture { Source = source, Below = below };
            return true;
        }

        internal bool TryGet(TMenu menu, out TLayer source, out TLayer below)
        {
            Capture capture;
            if (menu != null && _captures.TryGetValue(menu, out capture))
            { source = capture.Source; below = capture.Below; return true; }
            source = null; below = null; return false;
        }

        internal void Remove(TMenu menu) { if (menu != null) _captures.Remove(menu); }
        internal void Clear() { _captures.Clear(); }
    }
}
