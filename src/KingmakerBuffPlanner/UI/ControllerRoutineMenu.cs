using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.Assets.Console.GamepadInput;
using Kingmaker.GameModes;
using Rewired;
using TMPro;
using UnityEngine;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class ControllerRoutineMenu : IDisposable
    {
        private readonly GamePad _pad;
        private readonly InputLayer _layer;
        private readonly ControllerRoutineModel _model;
        private readonly ControllerActivationLatch _latch = new ControllerActivationLatch();
        private readonly object _area;
        private readonly object _context;
        private readonly string _campaign;
        private readonly Action<string> _log;
        private ControllerRoutineView _view;
        private ControllerLayerOwnership _ownership;
        private ControllerRoutineCommand? _pending;
        private bool _closing;
        private bool _yielded;
        private bool _running;
        private bool _paused;
        private bool _disposed;

        internal ControllerRoutineMenu(TMP_Text donor, ControllerRoutineModel model, Action<string> log)
        {
            _model = model;
            _log = log;
            _pad = GamePad.Instance;
            _campaign = Game.Instance.Player.GameId;
            _area = Game.Instance.CurrentlyLoadedArea;
            _context = Game.Instance.RootUiContext.InGameUiContext;
            _layer = new InputLayer { ContextName = "KingmakerBuffPlanner.SavedRoutines", CursorEnabled = false };
            var bindings = new List<object>();
            Add(bindings, RewiredConsts.Action.DPadUp, ControllerRoutineCommand.Up);
            Add(bindings, RewiredConsts.Action.DPadDown, ControllerRoutineCommand.Down);
            Add(bindings, RewiredConsts.Action.DPadLeft, ControllerRoutineCommand.PreviousPage);
            Add(bindings, RewiredConsts.Action.DPadRight, ControllerRoutineCommand.NextPage);
            Add(bindings, RewiredConsts.Action.Confirm, ControllerRoutineCommand.Confirm);
            Add(bindings, RewiredConsts.Action.Decline, ControllerRoutineCommand.Back);
            _layer.OnUnbind = () => _yielded = true;
            _ownership = new ControllerLayerOwnership(_layer, () => _pad.CurrentInputLayer, bindings);
            try
            {
                // Construction and a usable exit route precede ownership.
                _view = new ControllerRoutineView(donor);
                Refresh();
                ControllerRoutineIntegration.ActiveMenu = this;
                _ownership.Acquired(_pad.PushLayer(_layer));
                _log("[KBP-CONTROLLER] opened;campaign=" + _campaign + ";input=native-layer;fullscreenLease=false.");
            }
            catch
            {
                // PushLayer can fail after adding/binding its layer but before
                // returning the disposable. Remove just that partial layer.
                try { if (_pad.Layers.Contains(_layer)) _pad.PopLayer(_layer); }
                finally { Dispose(); }
                throw;
            }
        }

        internal bool IsOpen { get { return !_disposed; } }

        private void Add(List<object> bindings, int action, ControllerRoutineCommand command)
        {
            InputBindStruct bind = _layer.AddButton(_ => Receive(command), action,
                InputActionEventType.ButtonJustPressed, false, false);
            foreach (BindDescription description in bind.Binds) bindings.Add(description);
        }

        private void Receive(ControllerRoutineCommand command)
        {
            if (_disposed || _closing || !_ownership.IsCurrent || !ContextValid() ||
                !_latch.Take(Time.frameCount)) return;
            _pending = command;
        }

        internal bool AllowsNativeDispatch(BindDescription binding)
        {
            // All controller operations still use InputLayer. This prefix is
            // ownership only: it also suppresses the game's always-bound base
            // layer and callbacks captured before PushLayer unbound gameplay.
            return _disposed || !_view.Usable || !ContextValid() || _ownership.Allows(binding);
        }

        internal void Tick()
        {
            if (_disposed) return;
            try
            {
                if (_yielded || !ContextValid() || !_ownership.IsCurrent || !_view.Usable)
                { Dispose(); return; }
                int frame = Time.frameCount;
                bool neutral = ControlsNeutral();
                if (_closing)
                {
                    if (_latch.CanRelease(neutral, frame)) Dispose();
                    return;
                }
                _latch.Observe(neutral, frame);
                ControllerRoutineCommand? command = _pending;
                _pending = null;
                if (command.HasValue)
                {
                    switch (command.Value)
                    {
                        case ControllerRoutineCommand.Up: _model.Move(-1); break;
                        case ControllerRoutineCommand.Down: _model.Move(1); break;
                        case ControllerRoutineCommand.PreviousPage: _view.TurnPage(-1); break;
                        case ControllerRoutineCommand.NextPage: _view.TurnPage(1); break;
                        case ControllerRoutineCommand.Confirm: _model.Confirm(); break;
                        case ControllerRoutineCommand.Back: _closing = true; _latch.Reset(); break;
                    }
                    Refresh();
                }
                if (_running != _model.Running || _paused != Game.Instance.IsPaused) Refresh();
            }
            catch (Exception exception)
            {
                _log("[KBP-CONTROLLER] menu failed;closing;" + exception.Message);
                Dispose();
            }
        }

        internal void ShowResult(string message)
        {
            if (_disposed) return;
            try
            {
                _model.ShowResult(message);
                Refresh();
            }
            catch (Exception exception)
            {
                _log("[KBP-CONTROLLER] result surface failed;closing;" + exception.Message);
                Dispose();
            }
        }

        private void Refresh()
        {
            _running = _model.Running;
            _paused = Game.Instance != null && Game.Instance.IsPaused;
            _view.Refresh(_model, _paused, _closing);
        }

        private bool ContextValid()
        {
            Game game = Game.Instance;
            return Application.isFocused && game != null && game.IsControllerGamepad &&
                game.CurrentMode == GameModeType.Default && game.Player != null &&
                game.Player.GameId == _campaign && ReferenceEquals(game.CurrentlyLoadedArea, _area) &&
                game.RootUiContext != null && ReferenceEquals(game.RootUiContext.InGameUiContext, _context) &&
                _pad.Player.controllers.joystickCount > 0 && InputLayer.CanReceiveInput();
        }

        private bool ControlsNeutral()
        {
            // These are the verified mapped action constants, not device
            // indices. Include both Confirm/Decline mappings and every native
            // action so closing cannot pass a held input into gameplay.
            for (int action = RewiredConsts.Action.DPadLeft; action <= RewiredConsts.Action.RightStickButton; action++)
                if (_pad.Player.GetButton(action)) return false;
            return Mathf.Abs(_pad.Player.GetAxis(RewiredConsts.Action.LeftStickX)) < .2f &&
                Mathf.Abs(_pad.Player.GetAxis(RewiredConsts.Action.LeftStickY)) < .2f &&
                Mathf.Abs(_pad.Player.GetAxis(RewiredConsts.Action.RightStickX)) < .2f &&
                Mathf.Abs(_pad.Player.GetAxis(RewiredConsts.Action.RightStickY)) < .2f;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _pending = null;
            if (ControllerRoutineIntegration.ActiveMenu == this) ControllerRoutineIntegration.ActiveMenu = null;
            try
            {
                if (_ownership != null) _ownership.Dispose();
                _log("[KBP-CONTROLLER] closed;owned-layer-release-completed=true;native-stack-kept.");
            }
            finally
            {
                if (_view != null) _view.Dispose();
                _view = null;
            }
        }
    }
}
