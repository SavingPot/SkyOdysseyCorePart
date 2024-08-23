using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameCore
{
    public class GamepadController : PlayerController
    {
        public override bool Apply() => Gamepad.current != null && Gamepad.current.aButton.wasPressedThisFrame;
        public override bool Jump() => GControls.leftStickVec.y >= 0.62f;
        public override bool HoldingJump() => GControls.leftStickVec.y >= 0.62f;
        public override bool Rush(out bool direction) => throw new NotImplementedException();
        public override float Move() => GControls.leftStickVec.x;
        public override bool ClickingAttack() => Gamepad.current?.rightTrigger?.wasPressedThisFrame ?? false;
        public override bool HoldingAttack() => Gamepad.current?.rightTrigger?.isPressed ?? false;
        public override bool IsControllingBackground() => Gamepad.current != null && Gamepad.current.rightStickButton.isPressed;
        public override bool SkipDialog() => Apply();
        public override Vector2Int DialogOptions() => GControls.GetLeftStickVecInt();
        public override bool Backpack() => Gamepad.current != null && Gamepad.current.yButton.wasPressedThisFrame;
        public override bool Interact() => Gamepad.current != null && Gamepad.current.leftTrigger.wasPressedThisFrame;
        public override bool PlaceBlockUnderPlayer() => Gamepad.current != null && Gamepad.current.bButton.wasPressedThisFrame;
        public override bool ThrowItem() => false;
        public override bool SwitchToPreviousItem() => Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame;
        public override bool SwitchToNextItem() => Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame;
        public override bool SwitchToItem1() => false;
        public override bool SwitchToItem2() => false;
        public override bool SwitchToItem3() => false;
        public override bool SwitchToItem4() => false;
        public override bool SwitchToItem5() => false;
        public override bool SwitchToItem6() => false;
        public override bool SwitchToItem7() => false;
        public override bool SwitchToItem8() => false;

        public override PlayerOrientation SetPlayerOrientation()
        {
            //* 检测左摇杆
            float move = Move();

            if (move < 0)
                return PlayerOrientation.Left;
            else if (move > 0)
                return PlayerOrientation.Right;
            else
                return PlayerOrientation.Previous;
        }



        public GamepadController(Player player) : base(player) { }
    }
}