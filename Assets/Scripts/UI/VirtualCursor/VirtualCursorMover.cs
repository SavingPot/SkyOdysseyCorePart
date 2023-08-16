using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameCore
{
    public class VirtualCursorMover
    {
        private Vector2 oldPos;
        private Vector2 lastPos;

        private IMouseGesturePointerCallback callback;

        public VirtualCursorMover(IMouseGesturePointerCallback callback)
        {
            this.callback = callback;
        }

        public void Update()
        {
            if (callback.Pressed())
            {
                oldPos = callback.GetOldPos(oldPos);
            }
            else if (callback.Holding())
            {
                lastPos = callback.GetOldPos(oldPos);
                Vector2 deltaPos = callback.GetDeltaPos(lastPos, oldPos);

                callback?.MousePointerMoveDeltaPos(deltaPos);

                oldPos = lastPos;
            }
        }
    }


    public interface IMouseGesturePointerCallback : IVirtualCursorCallback
    {
        void MousePointerMoveDeltaPos(Vector2 deltaPos);
        Vector2 GetDeltaPos(Vector2 newPos, Vector2 oldPos);
    }
}