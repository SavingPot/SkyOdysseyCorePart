using GameCore.High;
using GameCore.UI;
using SP.Tools.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameCore
{
    public class VirtualCursor : SingletonClass<VirtualCursor>, IVirtualCursorRaycasterCallback, IMouseGesturePointerCallback
    {
        #region UI 展示

        private ImageIdentity _image;
        public ImageIdentity image
        {
            get
            {
                if (!_image)
                    IntiImage();

                return _image;
            }
        }
        public void IntiImage()
        {
            Canvas canvas = GetCursorCanvas();
            RectTransform rt = canvas.GetComponent<RectTransform>();

            _image = GameUI.AddImage(UPC.lowerLeft, "ori:image.virtual_cursor", "ori:virtual_cursor", rt);
            _image.sd = new(40, 40);
            _image.ap = new(rt.sizeDelta.x / 2, rt.sizeDelta.y / 2);
        }


        private static Canvas m_CursorCanvas;
        public static Canvas GetCursorCanvas()
        {
            if (!m_CursorCanvas)
            {
                m_CursorCanvas = Instantiate(GInit.instance.canvasPrefab);
                m_CursorCanvas.gameObject.name = "CursorCanvas";
                m_CursorCanvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            }

            return m_CursorCanvas;
        }

        #endregion

        private VirtualCursorMover mover;
        private VirtualCursorRaycaster raycaster;

        #region 接口
        public Vector2 GetOldPos(Vector2 older)
        {
            return Mouse.current.position.ReadValue();
        }
        public Vector2 GetDeltaPos(Vector2 newPos, Vector2 oldPos)
        {
            if (GControls.mode == ControlMode.Gamepad && Gamepad.current != null)
                return Gamepad.current.rightStick.ReadValue() * GFiles.settings.screenCursorSpeed * Performance.frameTime;

            return (newPos - oldPos);
        }
        public Vector2 PointerPos()
        {
            return image.ap;
        }
        public bool Pressed()
        {
            return Gamepad.current.bButton.wasPressedThisFrame;
        }
        public bool Released()
        {
            return Gamepad.current.bButton.wasReleasedThisFrame;
        }
        public bool Holding()
        {
            return GControls.rightStickVec != Vector2.zero;// Gamepad.current.bButton.isPressed;
        }

        public void MousePointerMoveDeltaPos(Vector2 deltaPos)
        {
            Vector2 val = image.ap;
            val += deltaPos;

            image.ap = ClampPointerPosValue(val);
        }

        /// <summary>
        /// 显示指针位置
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        Vector2 ClampPointerPosValue(Vector2 val)
        {
            //使 x, y 不超过最大与最小值
            val.x = val.x < 0 ? 0 : val.x;
            val.y = val.y < 0 ? 0 : val.y;

            val.x = val.x > Screen.width ? Screen.width : val.x;
            val.y = val.y > Screen.height ? Screen.height : val.y;

            return val;
        }
        #endregion


        void OnDestroy()
        {
            //删除时把专用画布也删掉
            if (m_CursorCanvas)
                Destroy(GetCursorCanvas().gameObject);
        }

        protected override void Start()
        {
            Init();

            base.Start();
        }

        void Update()
        {
            if (Gamepad.current != null)
            {
                mover?.Update();
                raycaster?.Update();
            }

            if (image)
            {
                //使光标始终处于首位
                image.rt.SetAsLastSibling();
            }
        }

        void Init()
        {
            //初始化光标移动器, 把鼠标的位置传 Ray 发射位置
            mover = new(this);
            raycaster = new(this);
        }
    }
}