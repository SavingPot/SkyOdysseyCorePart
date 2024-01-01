using GameCore.High;
using GameCore.UI;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GameCore
{
    public static class GControls
    {
        [RuntimeInitializeOnLoadMethod]
        private static void BindMethod()
        {
            //检测点击全屏
            //controls.Gameplay.Move.performed += ctx => ChooseUI();



            //private void ChooseUI()
            //{
            //    if (!GameUI.eventSystem.currentSelectedGameObject || !GameUI.eventSystem.currentSelectedGameObject.activeInHierarchy)
            //    {
            //        GameObject select = gameObject;

            //        if (UObjectTools.FindComponentWithType(out Button button))
            //        {
            //            select = button.gameObject;
            //        }

            //        if (select != gameObject)
            //            GameUI.eventSystem.SetSelectedGameObject(select);
            //    }
            //}

            MethodAgent.updates += () =>
            {
                if (Gamepad.current != null)
                {
                    if (Gamepad.current.rightStick.down.wasPressedThisFrame || Gamepad.current.rightStick.up.wasPressedThisFrame || Gamepad.current.rightStick.left.wasPressedThisFrame || Gamepad.current.rightStick.right.wasPressedThisFrame)
                    {
                        OnStartDragRightStick();
                    }
                }
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.f11Key.wasPressedThisFrame)
                        OnDownFullScreen();
                }
            };

            InputSystem.onActionChange += DetectDeviceInput;
        }



        private static ControlMode m_mode;
        public static ControlMode mode
        {
            get => m_mode;
            set
            {
                OnModeChanged(value);
                m_mode = value;
            }
        }

        public static Action OnDownFullScreen = () => { };
        public static Action<bool> OnDownMoveHorizontal = (b) => { };
        public static Action OnStartDragRightStick = () => { };

        public static Func<Vector2Int> GetWASDVec = () =>
        {
            return new(GetAD(), GetWS());
        };

        public static Func<int> GetAD = () =>
        {
            int x = 0;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed && !Keyboard.current.dKey.isPressed)
                    x = -1;
                if (Keyboard.current.dKey.isPressed && !Keyboard.current.aKey.isPressed)
                    x = 1;
            }

            return x;
        };

        public static Func<int> GetWS = () =>
        {
            int y = 0;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed && !Keyboard.current.sKey.isPressed)
                    y = 1;
                if (Keyboard.current.sKey.isPressed && !Keyboard.current.wKey.isPressed)
                    y = -1;
            }

            return y;
        };

        public static Func<Vector2Int> GetKeyboardArrowVec = () =>
        {
            int x = 0, y = 0;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                    y = 1;
                if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                    y = -1;
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                    x = -1;
                if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                    x = 1;
            }

            return new(x, y);
        };
        public static Func<Vector2> GetLeftStickVec = () => (Gamepad.current == null) ? Vector2.zero : Gamepad.current.leftStick.ReadValue();
        public static Func<Vector2Int> GetLeftStickVecInt = () => { var vec = GetLeftStickVec(); return new(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y)); };
        public static Func<Vector2> GetRightStickVec = () => (Gamepad.current == null) ? Vector2.zero : Gamepad.current.rightStick.ReadValue();
        public static Func<Vector2Int> GetRightStickVecInt = () => { var vec = GetRightStickVec(); return new(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y)); };
        public static Func<Vector2> GetMousePos = () =>
        {
            return (Mouse.current == null) ? Vector2.zero : Mouse.current.position.ReadValue();
        };
        public static Action<Vector2> SetMousePos = pos =>
        {
            Mouse.current.WarpCursorPosition(pos);
            InputState.Change(Mouse.current.position, pos);
        };

        public static Vector2 leftStickVec => GetLeftStickVec();
        public static Vector2 rightStickVec => GetRightStickVec();
        public static Vector2 mousePos
        {
            get => GetMousePos();
            set => SetMousePos(value);
        }

        public static Vector2 mousePosInMainCanvas
        {
            get
            {
                Vector2 pos = mousePos;
                float X = pos.x - Screen.width / 2f;
                float Y = pos.y - Screen.height / 2f;

                //得到画布的尺寸
                Vector2 uiSize = GameUI.canvasRT.sizeDelta;

                Vector2 finalPos = new(X * (uiSize.x / Screen.width), Y * (uiSize.y / Screen.height));

                return finalPos;
            }
        }

        public static Vector2 cursorPosInMainCanvas
        {
            get
            {
                return mode switch
                {
                    ControlMode.KeyboardAndMouse => mousePosInMainCanvas,
                    // GameUI.ScreenUIPosInConstantCanvas(VirtualCursor.instance.image.rectTransform.anchoredPosition, GameUI.canvasScaler);
                    // Vector2 pos = VirtualCursor.instance.image.rectTransform.anchoredPosition * GameUI.canvasScaler.referenceResolution / VirtualCursor.GetCursorCanvas().GetComponent<RectTransform>().sizeDelta;
                    // return pos - GameUI.canvasScaler.referenceResolution / 2;
                    ControlMode.Gamepad => GameUI.ScreenUIPosInConstantCanvas(VirtualCursor.instance.image.rectTransform.anchoredPosition, GameUI.canvasScaler),
                    _ => Vector2.zero,
                };
            }
        }


        public static Func<bool> GetMouseLeftButtonUp = () => ControlsExtensions.GetButtonUp(Mouse.current.leftButton);
        public static Func<bool> GetMouseLeftButton = () => ControlsExtensions.GetButton(Mouse.current.leftButton);
        public static Func<bool> GetMouseLeftButtonDown = () => ControlsExtensions.GetButtonDown(Mouse.current.leftButton);

        public static void SetVirtualCursorActive(bool active)
        {
            if (active)
            {
                if (!VirtualCursor.HasInstance())
                    VirtualCursor.SummonInstance();
            }
            else
            {
                if (VirtualCursor.HasInstance())
                {
                    GameObject.Destroy(VirtualCursor.instance.gameObject);
                }
            }
        }

        #region 检测输入设备切换
        static InputSystemUIInputModule UIInputModule => GameUI.engineInputMod;
        public static bool detectUIInputOnly = true;
        public static InputDevice currentDevice;




        internal static void DetectDeviceInput(object obj, InputActionChange change)
        {
            if (detectUIInputOnly && !UIInputModule.isActiveAndEnabled) return;

            //Performed: 持续
            if (change == InputActionChange.ActionPerformed)
            {
                InputAction action = (InputAction)obj;

                //屏蔽鼠标移动
                if (Mouse.current != null && action.activeControl.path == Mouse.current.position.path)
                    return;

                OnInput(action);

                InputDevice device = action.activeControl.device;

                if (currentDevice != device)
                    OnDeviceChanged(device);

                currentDevice = device;
            }
        }





        public static Action<InputAction> OnInput = action =>
        {
            if (action.activeControl.device == Gamepad.current)
            {
                SetVirtualCursorActive(true);
            }
            else
            {
                SetVirtualCursorActive(false);
            }

            //Debug.Log($"检测到输入 {action.activeControl.path}");
        };

        public static Action<InputDevice> OnDeviceChanged = device =>
        {
            //Debug.Log($"设备从 {currentDevice?.name} 变为了 {device.name}");

            if (device == Mouse.current || device == Keyboard.current)
            {
                if (mode != ControlMode.KeyboardAndMouse)
                    mode = ControlMode.KeyboardAndMouse;

                Tools.ShowCursor();
            }
            else if (device == Gamepad.current)
            {
                mode = ControlMode.Gamepad;

                Tools.HideCursor();
            }
            else if (device == Touchscreen.current)
            {
                mode = ControlMode.Touchscreen;

                Tools.HideCursor();
            }
        };

        public static Action<ControlMode> OnModeChanged = newMode =>
        {
            Debug.Log($"控制模式从 {mode} 变为了 {newMode}");
        };
        #endregion

        public static void ShowCursor()
        {
            Tools.ShowCursor();
        }

        public static void HideCursor()
        {
            Tools.HideCursor();
        }

        public static void GamepadVibrationSlighter(float time = 0.25f) => GamepadVibration(0.1f, 0.1f, time);
        public static void GamepadVibrationSlight(float time = 0.25f) => GamepadVibration(0.2f, 0.2f, time);
        public static void GamepadVibrationSlightMedium(float time = 0.25f) => GamepadVibration(0.4f, 0.4f, time);
        public static void GamepadVibrationMedium(float time = 0.25f) => GamepadVibration(0.6f, 0.6f, time);
        public static void GamepadVibrationMediumStrong(float time = 0.25f) => GamepadVibration(0.8f, 0.8f, time);
        public static void GamepadVibrationStrong(float time = 0.25f) => GamepadVibration(1f, 1f, time);

        public static void GamepadVibration(float low, float high, float time) => CoroutineStarter.Do(IEGamepadVibration(low, high, time));

        public static IEnumerator IEGamepadVibration(float low, float high, float time)
        {
            //防止因未连接手柄造成的 DebugError
            if (Gamepad.current == null)
                yield break;

            //设置手柄的 震动速度 以及 恢复震动 , 计时到达之后暂停震动
            Gamepad.current.SetMotorSpeeds(low, high);
            Gamepad.current.ResumeHaptics();

            float endTime = Time.time + time;

            while (Time.time < endTime && Gamepad.current != null)
            {
                Gamepad.current.ResumeHaptics();
                yield return null;
            }

            //防止震动完成前断开手柄
            if (Gamepad.current == null)
                yield break;

            Gamepad.current.PauseHaptics();
        }
    }

    public static class ControlsExtensions
    {
        public static bool GetButtonDown(this InputAction ia) => ia.WasPressedThisFrame();
        public static bool GetButton(this InputAction ia) => ia.IsPressed();
        public static bool GetButtonUp(this InputAction ia) => ia.WasReleasedThisFrame();

        public static bool GetButtonDown(this ButtonControl bc) => bc.wasPressedThisFrame;
        public static bool GetButton(this ButtonControl bc) => bc.isPressed;
        public static bool GetButtonUp(this ButtonControl bc) => bc.wasReleasedThisFrame;
    }

    public enum ControlMode
    {
        KeyboardAndMouse,
        Gamepad,
        Touchscreen
    }
}
