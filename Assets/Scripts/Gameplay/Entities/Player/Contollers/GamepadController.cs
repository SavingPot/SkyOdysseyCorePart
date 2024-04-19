using UnityEngine.InputSystem;

namespace GameCore
{
    public class GamepadController : PlayerController
    {
        public override bool Jump() => GControls.leftStickVec.y >= 0.62f;
        public override bool HoldingJump() => GControls.leftStickVec.y >= 0.62f;
        public override float Move() => GControls.leftStickVec.x;
        public override bool ClickingAttack() => Gamepad.current?.rightTrigger?.wasPressedThisFrame ?? false;
        public override bool HoldingAttack() => Gamepad.current?.rightTrigger?.isPressed ?? false;

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