using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameCore
{
    public static class PlayerControls
    {
        public static Func<Player, float> Move = (p) =>
        {
            return GControls.mode switch
            {
                ControlMode.Touchscreen => p.pui != null && p.pui.moveJoystick ? p.pui.moveJoystick.Horizontal : 0,
                ControlMode.Gamepad => GControls.leftStickVec.x,
                _ => GControls.GetAD()
            };
        };

        public static Func<Player, bool> Jump = (p) =>
        {
            return GControls.mode switch
            {
                ControlMode.Touchscreen => p.pui != null && p.pui.moveJoystick.Vertical >= 0.65f,
                ControlMode.KeyboardAndMouse => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame,
                ControlMode.Gamepad => GControls.leftStickVec.y >= 0.65f,
                _ => false
            };
        };

        public static Func<Player, bool> HoldJump = (p) =>
        {
            return GControls.mode switch
            {
                ControlMode.Touchscreen => p.pui != null && p.pui.moveJoystick.Vertical >= 0.65f,
                ControlMode.KeyboardAndMouse => Keyboard.current != null && Keyboard.current.spaceKey.isPressed,
                ControlMode.Gamepad => GControls.leftStickVec.y >= 0.65f,
                _ => false
            };
        };

        public static Func<Player, bool> Backpack = (p) =>
        {
            switch (GControls.mode)
            {
                case ControlMode.KeyboardAndMouse:
                    return Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;

                case ControlMode.Gamepad:
                    return Gamepad.current != null && Gamepad.current.yButton.wasPressedThisFrame;

                default:
                    return false;
            }
        };

        public static Func<Player, bool> UseItem = (p) => GControls.mode switch
            {
                ControlMode.Touchscreen => p.pui != null && p.pui.useItemButton.button.wasPressedThisFrame,
                ControlMode.KeyboardAndMouse => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame,
                ControlMode.Gamepad => Gamepad.current != null && Gamepad.current.leftTrigger.wasPressedThisFrame,
                _ => false
            };

        public static Func<Player, bool> SkipDialog = (p) => GControls.mode switch
        {
            ControlMode.Touchscreen => false,// p.pui != null && p.pui.useItemButton.button.wasPressedThisFrame,
            ControlMode.KeyboardAndMouse => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame,
            ControlMode.Gamepad => Gamepad.current != null && Gamepad.current.aButton.wasPressedThisFrame,
            _ => false
        };

        public static Func<Player, Vector2Int> DialogOptions = (p) => GControls.mode switch
        {
            ControlMode.Touchscreen => Vector2Int.zero,// p.pui != null && p.pui.useItemButton.button.wasPressedThisFrame,
            ControlMode.KeyboardAndMouse => GControls.GetWASDVec(),
            ControlMode.Gamepad => GControls.GetLeftStickVecInt(),
            _ => Vector2Int.zero
        };

        public static Func<Player, BlockLayer> SwitchControllingLayer = (caller) =>
        {
            switch (GControls.mode)
            {
                case ControlMode.KeyboardAndMouse:
                    if (Keyboard.current != null)
                    {
                        if (Keyboard.current.zKey.wasPressedThisFrame)
                        {
                            return BlockLayer.Wall;
                        }
                        if (Keyboard.current.xKey.wasPressedThisFrame)
                        {
                            return BlockLayer.Background;
                        }
                        if (Keyboard.current.cKey.wasPressedThisFrame)
                        {
                            return BlockLayer.Foreground;
                        }
                    }
                    break;

                case ControlMode.Gamepad:
                    if (Gamepad.current != null)
                    {
                        if (Gamepad.current.dpad.up.wasPressedThisFrame)
                        {
                            return BlockLayer.Wall;
                        }
                        if (Gamepad.current.dpad.left.wasPressedThisFrame)
                        {
                            return BlockLayer.Background;
                        }
                        if (Gamepad.current.dpad.down.wasPressedThisFrame)
                        {
                            return BlockLayer.Foreground;
                        }
                    }
                    break;

                default:
                    break;
            }

            return caller.controllingLayer;
        };

        public static Func<Player, bool> SwitchToItem1 = (p) => Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToItem2 = (p) => Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToItem3 = (p) => Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToItem4 = (p) => Keyboard.current != null && Keyboard.current.digit4Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToItem5 = (p) => Keyboard.current != null && Keyboard.current.digit5Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToItem6 = (p) => Keyboard.current != null && Keyboard.current.digit6Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToItem7 = (p) => Keyboard.current != null && Keyboard.current.digit7Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToItem8 = (p) => Keyboard.current != null && Keyboard.current.digit8Key.wasPressedThisFrame;

        public static Func<Player, bool> SwitchToLastItem = (p) =>
        {
            switch (GControls.mode)
            {
                case ControlMode.KeyboardAndMouse:
                    return Mouse.current != null && Mouse.current.scroll.y.ReadValue() > 0;

                case ControlMode.Gamepad:
                    return Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame;
            }

            return false;
        };

        public static Func<Player, bool> SwitchToNextItem = (p) =>
        {
            switch (GControls.mode)
            {
                case ControlMode.KeyboardAndMouse:
                    return Mouse.current != null && Mouse.current.scroll.y.ReadValue() < 0;

                case ControlMode.Gamepad:
                    return Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame;
            }

            return false;
        };

        public static Func<Player, bool> HoldingAttack = (p) => GControls.mode switch
            {
                ControlMode.Touchscreen => p.pui != null && p.pui.attackButton && p.pui.attackButton.button.isPressed,
                ControlMode.KeyboardAndMouse => Mouse.current?.leftButton?.isPressed ?? false,
                ControlMode.Gamepad => Gamepad.current?.rightTrigger?.isPressed ?? false,
                _ => false
            };

        public static Func<Player, bool> ThrowItem = (p) => GControls.mode switch
            {
                ControlMode.Touchscreen => false,
                ControlMode.KeyboardAndMouse => Keyboard.current?.backquoteKey?.wasPressedThisFrame ?? false,
                ControlMode.Gamepad => false,
                _ => false
            };

        public static Func<Player, bool> Interaction = (p) => GControls.mode switch
            {
                ControlMode.Touchscreen => p.pui != null && p.pui.interactionButton.button.wasPressedThisFrame,
                ControlMode.KeyboardAndMouse => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame,
                ControlMode.Gamepad => Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame,
                _ => false
            };
    }
}
