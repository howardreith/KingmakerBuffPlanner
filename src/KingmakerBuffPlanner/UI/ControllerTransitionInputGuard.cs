using System;
using Kingmaker.Assets.Console.GamepadInput;
using Rewired;
using UnityEngine;

namespace KingmakerBuffPlanner.UI
{
    // Covers the short gap AFTER a native source releases its layer and BEFORE
    // the neutral-gated destination acquires input. It never owns pause/mode.
    internal sealed class ControllerTransitionInputGuard : IDisposable
    {
        internal static ControllerTransitionInputGuard Active { get; private set; }
        private readonly ControllerLayerOwnership _ownership;
        private readonly InputLayer _layer;
        private readonly GamePad _pad;
        private readonly GameObject _surface;
        private readonly PlannerNativeKit _kit;
        private bool _disposed;
        internal bool Canceled { get; private set; }
        internal bool Current { get { return !_disposed && _ownership.IsCurrent; } }

        internal ControllerTransitionInputGuard(Component host, InputLayer expected)
        {
            _pad = GamePad.Instance;
            if (Active != null || !ReferenceEquals(_pad.CurrentInputLayer, expected))
                throw new InvalidOperationException("The native handoff owner changed.");
            _surface = new GameObject("KingmakerBuffPlanner.Handoff", typeof(RectTransform));
            try
            {
                var canvas = _surface.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32500;
                _kit = PlannerNativeKit.Controller(host);
                var panel = KingmakerUiFactory.CreateRect("Notice", _surface.transform);
                panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f); panel.sizeDelta = new Vector2(680, 112);
                KingmakerUiFactory.AddPanel(panel, new Color(.94f, .89f, .77f, 1));
                var label = _kit.Text(panel, "Status", "Opening Buff Planner…\nRelease controls to continue. Back cancels.", PlannerTextRole.Body, 22);
                KingmakerUiFactory.Stretch(label.rectTransform, 20, 20, 12, 12);
                _layer = new InputLayer { ContextName = "KingmakerBuffPlanner.Handoff", CursorEnabled = false };
                var back = _layer.AddButton(_ => Canceled = true, RewiredConsts.Action.Decline, InputActionEventType.ButtonJustPressed, false, false);
                _ownership = new ControllerLayerOwnership(_layer, () => _pad.CurrentInputLayer, back.Binds);
                Active = this;
                _ownership.Acquired(_pad.PushLayer(_layer));
            }
            catch { Dispose(); throw; }
        }

        internal bool Allows(BindDescription binding) { return _disposed || _ownership.Allows(binding); }
        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            if (Active == this) Active = null;
            try { if (_ownership != null) _ownership.Dispose(); }
            finally
            {
                try { if (_layer != null && _pad.Layers.Contains(_layer)) _pad.PopLayer(_layer); }
                finally { if (_surface != null) { _surface.SetActive(false); UnityEngine.Object.Destroy(_surface); } if (_kit != null) _kit.Dispose(); }
            }
        }
    }
}
