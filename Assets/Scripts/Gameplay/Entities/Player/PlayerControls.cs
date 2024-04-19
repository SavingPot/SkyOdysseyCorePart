using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameCore
{
    public static class PlayerControls
    {
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里
        //TODO: 全部转移到接口里

        public static Func<Player, bool> Backpack = (p) => GControls.mode switch
            {
                ControlMode.KeyboardAndMouse => Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame,
                ControlMode.Gamepad => Gamepad.current != null && Gamepad.current.yButton.wasPressedThisFrame,
                _ => false,
            };

        public static Func<Player, bool> UseItem = (p) => GControls.mode switch
            {
                ControlMode.Touchscreen => p.pui != null && p.pui.touchScreenUseItemButton.button.wasPressedThisFrame,
                ControlMode.KeyboardAndMouse => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame,
                ControlMode.Gamepad => Gamepad.current != null && Gamepad.current.leftTrigger.wasPressedThisFrame,
                _ => false
            };

        public static Func<Player, bool> PlaceBlockUnderPlayer = (p) => GControls.mode switch
            {
                ControlMode.Touchscreen => p.pui != null && p.pui.touchScreenPlaceBlockUnderPlayerButton.button.wasPressedThisFrame,
                ControlMode.KeyboardAndMouse => Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame,
                ControlMode.Gamepad => Gamepad.current != null && Gamepad.current.aButton.wasPressedThisFrame,
                _ => false
            };

        public static Func<Player, bool> SkipDialog = (p) => GControls.mode switch
            {
                ControlMode.Touchscreen => p.pui != null && p.pui.dialogPanel.button.isPressed,
                ControlMode.KeyboardAndMouse => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame,
                ControlMode.Gamepad => Gamepad.current != null && Gamepad.current.aButton.wasPressedThisFrame,
                _ => false
            };

        public static Func<Player, Vector2Int> DialogOptions = (p) => GControls.mode switch
            {
                ControlMode.Touchscreen => throw new NotImplementedException(),// p.pui != null && p.pui.useItemButton.button.wasPressedThisFrame,
                ControlMode.KeyboardAndMouse => GControls.GetWASDVec(),
                ControlMode.Gamepad => GControls.GetLeftStickVecInt(),
                _ => Vector2Int.zero
            };

        public static Func<Player, bool> IsControllingBackground = (p) => GControls.mode switch
            {
                ControlMode.KeyboardAndMouse => Keyboard.current != null && Keyboard.current.ctrlKey.isPressed,
                ControlMode.Gamepad => Gamepad.current != null && Gamepad.current.rightStickButton.isPressed,
                _ => false,
            };

        public static Func<Player, bool> SwitchToItem1 = (p) => Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToItem2 = (p) => Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToItem3 = (p) => Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToItem4 = (p) => Keyboard.current != null && Keyboard.current.digit4Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToItem5 = (p) => Keyboard.current != null && Keyboard.current.digit5Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToItem6 = (p) => Keyboard.current != null && Keyboard.current.digit6Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToItem7 = (p) => Keyboard.current != null && Keyboard.current.digit7Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToItem8 = (p) => Keyboard.current != null && Keyboard.current.digit8Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToPreviousItem = (p) => GControls.mode switch
            {
                ControlMode.KeyboardAndMouse => Mouse.current != null && Mouse.current.scroll.y.ReadValue() > 0,
                ControlMode.Gamepad => Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame,
                _ => false,
            };

        public static Func<Player, bool> SwitchToNextItem = (p) => GControls.mode switch
            {
                ControlMode.KeyboardAndMouse => Mouse.current != null && Mouse.current.scroll.y.ReadValue() < 0,
                ControlMode.Gamepad => Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame,
                _ => false,
            };


        public static Func<Player, bool> ThrowItem = (p) => GControls.mode switch
            {
                ControlMode.Touchscreen => false,
                ControlMode.KeyboardAndMouse => Keyboard.current?.backquoteKey?.wasPressedThisFrame ?? false,
                ControlMode.Gamepad => false,
                _ => false
            };
    }
}
