using SP.Tools.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameCore
{
    public interface IVirtualCursorCallback
    {
        bool Pressed();
        bool Released();
        bool Holding();
        Vector2 GetOldPos(Vector2 olderPos);
    }

    public interface IVirtualCursorRaycasterCallback : IVirtualCursorCallback
    {
        Vector2 PointerPos();
    }

    public class VirtualCursorRaycaster
    {
        private IVirtualCursorRaycasterCallback call;

        //防止在同一可选项上循环, 导致游戏卡死
        Selectable excluded;
        Selectable currentSelectable = null;
        RaycastResult currentRaycastResult;

        IPointerClickHandler clickHandler;
        IDragHandler dragHandler;

        readonly EventSystem eventSystem;
        readonly PointerEventData pointerEvent;

        //点击, 滑动, 抓动
        private bool isClick = false;
        private bool isSwipe = false;
        private bool isDrag = false;

        private Vector2 oldPos;
        private Vector2 lastPos;
        private const float moveLengthLimit = 10;
        readonly List<RaycastResult> results = new();

        public VirtualCursorRaycaster(IVirtualCursorRaycasterCallback callback)
        {
            eventSystem = EventSystem.current;
            pointerEvent = new(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
            };

            this.call = callback;
        }

        public void Update()
        {
            //设置指针位置
            pointerEvent.position = call.PointerPos();

            FindResults();

            //使目标激活
            if (currentSelectable)
            {
                if (clickHandler != null && call.Released() && !isSwipe)
                {
                    clickHandler.OnPointerClick(pointerEvent);
                    Unselect();
                }

                else if (dragHandler != null && isClick)
                {
                    isDrag = true;
                    pointerEvent.pointerPressRaycast = currentRaycastResult;
                    dragHandler.OnDrag(pointerEvent);
                }
                if (dragHandler == null)
                {
                    isClick = false;
                    isSwipe = false;
                }

                if (clickHandler != null || dragHandler != null)
                {
                    if (call.Pressed())
                    {
                        oldPos = call.GetOldPos(Vector2.zero);
                        isClick = true;
                    }
                    else if (call.Released())
                    {
                        UnselectAll();
                    }
                    else
                    {
                        lastPos = call.GetOldPos(Vector2.zero);
                        isSwipe = JudgeIsSwipe(oldPos, lastPos);
                    }
                }
            }
            else
            {
                isClick = false;
                isSwipe = false;

                Unselect();
            }

            if (call.Released())
            {
                UnselectAll();
            }
        }

        void UnselectAll()
        {
            isSwipe = false;
            isClick = false;
            isDrag = false;

            Unselect();
        }

        void Unselect()
        {
            Select(null, null);
        }

        void FindResults()
        {
            //获取扫描结果
            results.Clear();
            eventSystem.RaycastAll(pointerEvent, results);

            //从指针位置发射射线扫描可选择物体
            if (results.Count > 0)
            {
                foreach (var result in results)
                {
                    if (result.gameObject.TryGetComponentInParent<Selectable>(out var newSelectable))
                    {
                        //如果检测到的未被排除且不被选择
                        if (newSelectable != excluded && newSelectable != currentSelectable)
                        {
                            Select(newSelectable);
                            currentRaycastResult = result;
                        }
                        break;
                    }
                }
            }
            else
            {
                //如果没扫描到就取消选择
                if (currentSelectable || excluded)
                {
                    Unselect();
                }
            }
        }

        /// <summary>
        /// 判断是否是滑动
        /// </summary>
        /// <param name="oldPos"></param>
        /// <param name="lastPos"></param>
        /// <returns></returns>
        private bool JudgeIsSwipe(Vector2 oldPos, Vector2 lastPos)
        {
            return Vector2.Distance(oldPos, lastPos) > moveLengthLimit;
        }

        /// <summary>
        /// 选择操作
        /// </summary>
        /// <param name="s"></param>
        /// <param name="exclude"></param>
        void Select(Selectable s, Selectable exclude = null)
        {
            if (isDrag)
                return;

            //调用指针离开
            if (currentSelectable)
                currentSelectable.OnPointerExit(pointerEvent);

            excluded = exclude;
            currentSelectable = s;

            if (currentSelectable)
            {
                //调用指针进入方法, 并获取 Handle
                currentSelectable.OnPointerEnter(pointerEvent);
                clickHandler = currentSelectable.GetComponent<IPointerClickHandler>();
                dragHandler = currentSelectable.GetComponent<IDragHandler>();
            }
            else
            {
                clickHandler = null;
                dragHandler = null;
            }
        }
    }
}