using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class PlannerPointerSink : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler,
        ICancelHandler, IPointerEnterHandler, IPointerExitHandler
    {
        internal BuffPlannerUiLifecycleDiagnostics Diagnostics;
        internal string RoutineId;
        internal Action<bool> HoverChanged;
        // Everyday-use v1.2 §5: a hold on the moon button opens the editor
        // (its click runs Long). Set only on that button.
        internal Action HoldAction;
        internal float HoldSeconds = 0.55f;
        private float _heldSince = -1f;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Diagnostics != null) Diagnostics.RecordPointer(RoutineId);
            if (HoldAction != null) _heldSince = Time.unscaledTime;
            eventData.Use();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _heldSince = -1f;
            eventData.Use();
        }

        public void OnPointerClick(PointerEventData eventData) { eventData.Use(); }

        private void Update()
        {
            if (HoldAction == null || _heldSince < 0f) return;
            if (Time.unscaledTime - _heldSince >= HoldSeconds)
            {
                _heldSince = -1f;
                HoldAction();
            }
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Diagnostics != null) Diagnostics.RecordPointerEnter(RoutineId);
            if (HoverChanged != null) HoverChanged(true);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            if (HoverChanged != null) HoverChanged(false);
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Diagnostics != null) Diagnostics.RecordDrag();
            eventData.Use();
        }
        public void OnDrag(PointerEventData eventData)
        {
            if (Diagnostics != null) Diagnostics.RecordDrag();
            eventData.Use();
        }
        public void OnEndDrag(PointerEventData eventData) { eventData.Use(); }
        public void OnScroll(PointerEventData eventData)
        {
            if (Diagnostics != null) Diagnostics.RecordScroll();
            eventData.Use();
        }
        public void OnCancel(BaseEventData eventData) { eventData.Use(); }
    }
}
