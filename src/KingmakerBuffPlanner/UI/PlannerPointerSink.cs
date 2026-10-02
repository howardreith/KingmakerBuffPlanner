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

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Diagnostics != null) Diagnostics.RecordPointer(RoutineId);
            eventData.Use();
        }

        public void OnPointerUp(PointerEventData eventData) { eventData.Use(); }
        public void OnPointerClick(PointerEventData eventData) { eventData.Use(); }
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
