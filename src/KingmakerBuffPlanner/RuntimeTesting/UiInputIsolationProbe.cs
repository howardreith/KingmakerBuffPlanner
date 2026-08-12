using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerBuffPlanner.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal sealed class UiInputIsolationProbe :
        IUnitCommandStartHandler, ISelectionHandler, IAbilityTargetSelectionUIHandler, IDisposable
    {
        private readonly HashSet<string> _selectedUnitIds;
        private readonly string _selectionBefore;
        private readonly Vector3 _cameraBefore;
        private readonly IDisposable _subscription;
        private int _playerCommandCount;
        private int _movementCommandCount;
        private int _abilityCommandCount;
        private int _selectionEventCount;
        private int _abilityTargetEventCount;
        private bool _disposed;

        internal UiInputIsolationProbe()
        {
            _selectedUnitIds = new HashSet<string>(SelectedUnitIds(), StringComparer.Ordinal);
            _selectionBefore = string.Join("|", _selectedUnitIds.OrderBy(value => value).ToArray());
            _cameraBefore = Camera.main == null ? Vector3.zero : Camera.main.transform.position;
            _subscription = EventBus.Subscribe((object)this);
        }

        public void HandleUnitCommandDidStart(UnitCommand command)
        {
            UnitEntityData executor = command == null ? null : command.Executor;
            if (executor == null || !_selectedUnitIds.Contains(executor.UniqueId)) return;
            _playerCommandCount++;
            if (command is UnitMoveTo || command is UnitMoveAlongPath ||
                command is UnitMoveContiniously || command is UnitInteractWithObject ||
                command is UnitInteractWithUnit)
                _movementCommandCount++;
            if (command is UnitUseAbility || command is UnitActivateAbility)
                _abilityCommandCount++;
        }

        public void OnUnitSelectionAdd(UnitEntityData unit) { _selectionEventCount++; }
        public void OnUnitSelectionRemove(UnitEntityData unit) { _selectionEventCount++; }
        public void HandleAbilityTargetSelectionStart(AbilityData ability) { _abilityTargetEventCount++; }
        public void HandleAbilityTargetSelectionEnd(AbilityData ability) { _abilityTargetEventCount++; }

        internal UiInputIsolationProbeResult Dispatch(BuffPlannerScreenView view)
        {
            if (view == null || view.RootObject == null || EventSystem.current == null)
                throw new InvalidOperationException("Planner pointer surface is unavailable.");
            GameObject root = view.RootObject;
            DispatchPointer(root, ExecuteEvents.pointerDownHandler);
            DispatchPointer(root, ExecuteEvents.pointerUpHandler);
            DispatchPointer(root, ExecuteEvents.pointerClickHandler);
            DispatchPointer(root, ExecuteEvents.beginDragHandler);
            DispatchPointer(root, ExecuteEvents.dragHandler);
            DispatchPointer(root, ExecuteEvents.endDragHandler);
            var scroll = new PointerEventData(EventSystem.current)
            {
                scrollDelta = new Vector2(0, 1),
                position = new Vector2(Screen.width / 2f, Screen.height / 2f)
            };
            bool scrollHandled = ExecuteEvents.Execute(root, scroll, ExecuteEvents.scrollHandler) && scroll.used;
            var cancel = new BaseEventData(EventSystem.current);
            bool cancelHandled = ExecuteEvents.Execute(root, cancel, ExecuteEvents.cancelHandler) && cancel.used;
            bool groupChanged = view.DispatchRoutineTabForRuntime("important") &&
                view.ActiveRoutineId == "important";
            UiInputIsolationProbeResult result = Snapshot();
            result.ScrollConsumed = scrollHandled;
            result.CancelConsumed = cancelHandled;
            result.GroupSelectorChanged = groupChanged;
            return result;
        }

        internal UiInputIsolationProbeResult Snapshot()
        {
            string selectionAfter = string.Join("|", SelectedUnitIds().OrderBy(value => value).ToArray());
            Vector3 cameraAfter = Camera.main == null ? Vector3.zero : Camera.main.transform.position;
            return new UiInputIsolationProbeResult
            {
                PlayerCommandCount = _playerCommandCount,
                MovementCommandCount = _movementCommandCount,
                AbilityCommandCount = _abilityCommandCount,
                SelectionEventCount = _selectionEventCount,
                AbilityTargetEventCount = _abilityTargetEventCount,
                SelectionUnchanged = string.Equals(_selectionBefore, selectionAfter, StringComparison.Ordinal),
                CameraUnchanged = _cameraBefore == cameraAfter
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_subscription != null) _subscription.Dispose();
            else EventBus.Unsubscribe((object)this);
        }

        private static void DispatchPointer<T>(GameObject target, ExecuteEvents.EventFunction<T> function)
            where T : IEventSystemHandler
        {
            var data = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = new Vector2(Screen.width / 2f, Screen.height / 2f),
                delta = new Vector2(24, 18)
            };
            if (!ExecuteEvents.Execute(target, data, function) || !data.used)
                throw new InvalidOperationException("Planner pointer event was not consumed: " + typeof(T).Name);
        }

        private static IEnumerable<string> SelectedUnitIds()
        {
            SelectionManager manager = SelectionManager.Instance;
            if (manager == null || manager.SelectedUnits == null) return new string[0];
            return manager.SelectedUnits.Where(unit => unit != null).Select(unit => unit.UniqueId).ToArray();
        }
    }

    internal sealed class UiInputIsolationProbeResult
    {
        internal int PlayerCommandCount;
        internal int MovementCommandCount;
        internal int AbilityCommandCount;
        internal int SelectionEventCount;
        internal int AbilityTargetEventCount;
        internal bool SelectionUnchanged;
        internal bool CameraUnchanged;
        internal bool ScrollConsumed;
        internal bool CancelConsumed;
        internal bool GroupSelectorChanged;
    }
}
