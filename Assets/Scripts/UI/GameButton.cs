using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class GameButton : Button, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IDropHandler
    {
        public Action<PointerEventData> OnPointerDownAction = _ => { };
        public Action<PointerEventData> OnPointerUpAction = _ => { };
        public Action<PointerEventData> OnPointerEnterAction = _ => { };
        public Action<PointerEventData> OnPointerExitAction = _ => { };
        public Action<PointerEventData> OnPointerClickAction = _ => { };
        public Action<PointerEventData> OnDragAction = _ => { };
        public Action<PointerEventData> OnDropAction = _ => { };
        public Action<PointerEventData> OnBeginDragAction = _ => { };
        public Action<PointerEventData> OnEndDragAction = _ => { };
        public Action OnPointerHoldAction = () => { };
        public Action OnPointerStayAction = () => { };

        public bool wasPressedThisFrame;
        public bool wasReleasedThisFrame;
        public bool isDragging;
        public bool isPressed;
        public bool pointerStaying;

        protected override void Start()
        {
            base.Start();

            //this.HideClickAction();
        }

        private void Update()
        {
            if (isPressed)
                OnPointerHoldAction();

            if (pointerStaying)
                OnPointerStayAction();

            wasPressedThisFrame = false;
            wasReleasedThisFrame = false;
        }

        public override void OnPointerDown(PointerEventData ped)
        {
            isPressed = true;
            wasPressedThisFrame = true;

            base.OnPointerDown(ped);

            OnPointerDownAction(ped);
        }

        public override void OnPointerUp(PointerEventData ped)
        {
            isPressed = false;
            wasReleasedThisFrame = true;

            base.OnPointerUp(ped);

            OnPointerUpAction(ped);
        }

        public void OnDrag(PointerEventData ped)
        {
            OnDragAction(ped);
        }

        public void OnDrop(PointerEventData ped)
        {
            OnDropAction(ped);
        }


        public override void OnPointerClick(PointerEventData ped)
        {
            base.OnPointerClick(ped);

            OnPointerClickAction(ped);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            pointerStaying = true;
            base.OnPointerEnter(eventData);

            OnPointerEnterAction(eventData);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            pointerStaying = false;
            base.OnPointerExit(eventData);

            OnPointerExitAction(eventData);
        }

        public void OnBeginDrag(PointerEventData ped)
        {
            isDragging = true;
            OnBeginDragAction(ped);
        }

        public void OnEndDrag(PointerEventData ped)
        {
            isDragging = false;
            OnEndDragAction(ped);
        }
    }
}