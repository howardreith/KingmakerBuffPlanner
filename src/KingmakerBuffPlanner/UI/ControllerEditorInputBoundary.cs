using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Assets.Console.GamepadInput;
using Kingmaker.GameModes;
using Rewired;
using PlayerMouse = Rewired.Components.PlayerMouse;
using Rewired.Integration.UnityUI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingmakerBuffPlanner.UI
{
    // Mixed input ONLY for the owned editor lifetime. Reuse the active native
    // EventSystem/Rewired module, selecting its verified Unity mouse fallback;
    // never add an input module or change Game.ControllerMode.
    internal sealed class ControllerEditorInputBoundary : IPlannerInputBoundary
    {
        internal static ControllerEditorInputBoundary Active { get; private set; }
        private readonly Action _back;
        private readonly ControllerActivationLatch _latch = new ControllerActivationLatch();
        private InputLayer _layer;
        private GamePad _pad;
        private ControllerLayerOwnership _ownership;
        private EventSystem _events;
        private RewiredStandaloneInputModule _module;
        private List<PlayerMouse> _previousMice;
        private List<PlayerMouse> _ownedMice;
        private bool _mouseAllowed;
        private bool _navigation;
        private bool _cursorVisible;
        private CursorLockMode _cursorLock;
        private bool _modeOwned;
        private readonly ControllerBackIntent _backIntent = new ControllerBackIntent();
        private TMPro.TMP_InputField _backText;
        private bool _armed;
        private bool _restored;
        private string _campaign;
        private object _area;
        private object _ui;
        private List<Action> _modeQueue;
        private ControllerQueuedModeAction _pendingStart;
        private int _modeWaitFrames;
        private static readonly FieldInfo ModeQueue = typeof(Game).GetField("m_BeforeTickActions",
            BindingFlags.Instance | BindingFlags.NonPublic);

        internal ControllerEditorInputBoundary(Action back) { _back = back; }
        public bool PlannerModeRequested { get { return _modeOwned && _ownership != null && _ownership.IsCurrent; } }

        public object CaptureState()
        {
            Game game = Game.Instance;
            if (game == null || !game.IsControllerGamepad || game.CurrentMode != GameModeType.Default ||
                game.Player == null || game.RootUiContext == null || game.RootUiContext.InGameUiContext == null ||
                !GamePad.HasInstance || EventSystem.current == null)
                throw new InvalidOperationException("Controller campaign input is not ready.");
            _events = EventSystem.current;
            _pad = GamePad.Instance;
            _module = _events.currentInputModule as RewiredStandaloneInputModule;
            if (_module == null) throw new InvalidOperationException("The native Rewired mouse input module is unavailable.");
            _campaign = game.Player.GameId;
            _area = game.CurrentlyLoadedArea;
            _ui = game.RootUiContext.InGameUiContext;
            if (ModeQueue == null || ModeQueue.FieldType != typeof(List<Action>))
                throw new InvalidOperationException("Unverified native mode queue contract.");
            _modeQueue = (List<Action>)ModeQueue.GetValue(game);
            _previousMice = new List<PlayerMouse>(_module.PlayerMice);
            _mouseAllowed = _module.allowMouseInput;
            _navigation = _events.sendNavigationEvents;
            _cursorVisible = Cursor.visible;
            _cursorLock = Cursor.lockState;
            return this;
        }

        public void EnterPlannerMode()
        {
            try
            {
                _layer = new InputLayer { ContextName = "KingmakerBuffPlanner.MixedInputEditor", CursorEnabled = false };
                InputBindStruct back = _layer.AddButton(_ =>
                {
                    RequestBack();
                }, RewiredConsts.Action.Decline, InputActionEventType.ButtonJustPressed, false, false);
                _ownership = new ControllerLayerOwnership(_layer, () => _pad.CurrentInputLayer, back.Binds);
                Active = this;
                _ownership.Acquired(_pad.PushLayer(_layer));
                _module.allowMouseInput = false;
                _module.PlayerMice = new List<PlayerMouse>();
                _ownedMice = _module.PlayerMice; // native setter copies the list
                _events.sendNavigationEvents = false;
                _events.SetSelectedGameObject(null);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                var before = new HashSet<Action>(_modeQueue);
                Game.Instance.StartMode(GameModeType.FullScreenUi);
                Action queuedStart = _modeQueue.FirstOrDefault(action => !before.Contains(action) && action.Method.Name.Contains("<StartMode>"));
                if (queuedStart != null) _pendingStart = new ControllerQueuedModeAction(_modeQueue, queuedStart);
                _modeOwned = true;
                if (_pendingStart == null && Game.Instance.CurrentMode != GameModeType.FullScreenUi)
                    throw new InvalidOperationException("The native fullscreen mode request was refused.");
            }
            catch { RestoreState(this); throw; }
        }

        internal bool Valid
        {
            get
            {
                Game game = Game.Instance;
                return Application.isFocused && game != null && game.IsControllerGamepad &&
                    game.Player != null && game.Player.GameId == _campaign &&
                    ReferenceEquals(game.CurrentlyLoadedArea, _area) && game.RootUiContext != null &&
                    ReferenceEquals(game.RootUiContext.InGameUiContext, _ui) &&
                    (game.CurrentMode == GameModeType.FullScreenUi ||
                        (game.CurrentMode == GameModeType.Default && _pendingStart != null &&
                         _pendingStart.Pending && _modeWaitFrames < 120)) &&
                    _ownership != null && GamePad.HasInstance && ReferenceEquals(GamePad.Instance, _pad) && _ownership.IsCurrent &&
                    EventSystem.current == _events && _module != null && _module.isActiveAndEnabled &&
                    GamePad.Instance.Player.controllers.joystickCount > 0;
            }
        }

        internal bool Allows(BindDescription binding) { return _ownership == null || _ownership.Allows(binding); }

        internal void RequestBack()
        {
            if (!Valid) return;
            var field = _events.currentSelectedGameObject == null ? null :
                _events.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>();
            if (_backIntent.Request(_latch, Time.frameCount, field != null && field.isFocused)) _backText = field;
        }

        internal void Tick()
        {
            if (!Valid) return;
            if (Game.Instance.CurrentMode != GameModeType.FullScreenUi) { _modeWaitFrames++; return; }
            bool neutral = ControllerRoutineMenu.ControlsAreNeutral() &&
                !Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2) && !Input.GetKey(KeyCode.Escape);
            _latch.Observe(neutral, Time.frameCount);
            if (!_armed && _latch.Ready(Time.frameCount))
            {
                _armed = true;
                _module.allowMouseInput = true;
            }
            _backIntent.Complete(_latch, neutral, Time.frameCount, () =>
            {
                // TMP may already have handled keyboard Escape. Keep the
                // ORIGINAL text-only intent rather than closing another layer.
                if (_backText != null)
                {
                    if (_backText.isFocused) _backText.DeactivateInputField();
                    if (_events.currentSelectedGameObject == _backText.gameObject) _events.SetSelectedGameObject(null);
                }
                _backText = null;
            }, _back);
        }

        public void YieldFullScreenOwnership() { _modeOwned = false; }

        public void RestoreState(object state)
        {
            if (_restored) return;
            _restored = true;
            bool current = _ownership != null && GamePad.HasInstance && ReferenceEquals(GamePad.Instance, _pad) && _ownership.IsCurrent;
            if (Active == this) Active = null;
            try
            {
                try
                {
                if (_module != null && _ownedMice != null && ReferenceEquals(_module.PlayerMice, _ownedMice) && _ownedMice.Count == 0)
                {
                    _module.PlayerMice = _previousMice;
                    if (_module.allowMouseInput == _armed) _module.allowMouseInput = _mouseAllowed;
                }
                if (_events != null && !_events.sendNavigationEvents) _events.sendNavigationEvents = _navigation;
                }
                finally
                {
                    try
                    {
                if (current)
                {
                    if (Cursor.lockState == CursorLockMode.None) Cursor.lockState = _cursorLock;
                    if (Cursor.visible) Cursor.visible = _cursorVisible;
                }
                    }
                    finally { ReleaseOwnedMode(current); }
                }
            }
            finally
            {
                _modeOwned = false;
                try { if (_ownership != null) _ownership.Dispose(); }
                finally
                {
                    try { if (_layer != null && _pad != null && _pad.Layers.Contains(_layer)) _pad.PopLayer(_layer); }
                    finally { _ownership = null; _layer = null; _ownedMice = null; }
                }
            }
        }

        private void ReleaseOwnedMode(bool current)
        {
                // Cancel only our exact queued Start delegate if the native
                // game has not executed it yet. Never clear a global queue.
                if (_pendingStart != null) _pendingStart.Cancel();
                if (current && _modeOwned && Game.Instance != null && Game.Instance.RootUiContext != null &&
                    ReferenceEquals(Game.Instance.RootUiContext.InGameUiContext, _ui) && Game.Instance.CurrentMode == GameModeType.FullScreenUi)
                {
                    var pad = GamePad.Instance;
                    int index = pad.Layers.IndexOf(_layer);
                    InputLayer below = index <= 0 ? pad.BaseLayer : pad.Layers[index - 1];
                    var before = new HashSet<Action>(_modeQueue);
                    Game game = Game.Instance;
                    game.StopMode(GameModeType.FullScreenUi);
                    Action queued = _modeQueue.FirstOrDefault(action => !before.Contains(action) && action.Method.Name.Contains("<StopMode>"));
                    if (queued != null)
                    {
                        // The game may defer Stop. Do not end a newer native
                        // window's mode if it acquires input before execution.
                        new ControllerQueuedModeAction(_modeQueue, queued).Guard(() =>
                            Game.Instance == game && game.Player != null && game.Player.GameId == _campaign &&
                                ReferenceEquals(game.CurrentlyLoadedArea, _area) && game.RootUiContext != null &&
                                ReferenceEquals(game.RootUiContext.InGameUiContext, _ui) &&
                                GamePad.HasInstance && ReferenceEquals(GamePad.Instance.CurrentInputLayer, below));
                    }
                }
        }
    }
}
