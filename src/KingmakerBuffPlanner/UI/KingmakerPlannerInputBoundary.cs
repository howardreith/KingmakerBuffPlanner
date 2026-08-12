using System;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.UI.FullScreenUITypes;
using Kingmaker.UI.Selection;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class KingmakerPlannerInputBoundary : IPlannerInputBoundary
    {
        private bool _requested;

        public bool PlannerModeRequested
        {
            get
            {
                return _requested && Game.Instance != null &&
                    Game.Instance.IsModeActive(GameModeType.FullScreenUi) &&
                    (SelectionManager.Instance == null || SelectionManager.Instance.IsDisabled);
            }
        }

        public object CaptureState()
        {
            if (Game.Instance == null || Game.Instance.UI == null || StaticCanvas.Instance == null)
                throw new InvalidOperationException("Kingmaker campaign UI is not available.");
            if (Game.Instance.IsModeActive(GameModeType.FullScreenUi))
                throw new InvalidOperationException("Close the current full-screen game window first.");
            SelectionManager selection = SelectionManager.Instance;
            return new KingmakerInputState
            {
                Mode = Game.Instance.CurrentMode,
                Paused = Game.Instance.IsPaused,
                SelectionWasAvailable = selection != null,
                SelectionDisabled = selection != null && selection.IsDisabled
            };
        }

        public void EnterPlannerMode()
        {
            if (_requested) return;
            SelectionManager selection = SelectionManager.Instance;
            if (selection != null) selection.IsDisabled = true;
            _requested = true;
            EventBus.RaiseEvent<IFullScreenUIHandler>(handler =>
                handler.HandleFullScreenUiChanged(true, FullScreenUIType.Unknown));
        }

        public void RestoreState(object state)
        {
            var captured = state as KingmakerInputState;
            try
            {
                if (_requested)
                    EventBus.RaiseEvent<IFullScreenUIHandler>(handler =>
                        handler.HandleFullScreenUiChanged(false, FullScreenUIType.Unknown));
            }
            finally
            {
                _requested = false;
                SelectionManager selection = SelectionManager.Instance;
                if (captured != null && captured.SelectionWasAvailable && selection != null)
                    selection.IsDisabled = captured.SelectionDisabled;
            }
        }

        internal sealed class KingmakerInputState
        {
            internal GameModeType Mode;
            internal bool Paused;
            internal bool SelectionWasAvailable;
            internal bool SelectionDisabled;
        }
    }
}
