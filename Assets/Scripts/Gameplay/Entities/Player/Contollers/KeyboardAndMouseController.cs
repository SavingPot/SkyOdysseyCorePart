using UnityEngine.InputSystem;

namespace GameCore
{
    public class KeyboardAndMouseController : PlayerController
    {
        public override bool Jump() => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        public override bool HoldingJump() => Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        public override float Move() => GControls.GetAD();
        public override bool ClickingAttack() => Mouse.current?.leftButton?.wasPressedThisFrame ?? false;
        public override bool HoldingAttack() => Mouse.current?.leftButton?.isPressed ?? false;

        public override PlayerOrientation SetPlayerOrientation()
        {
            //* 检测鼠标和玩家的相对位置
            float delta = GControls.mousePos.ToWorldPos().x - player.transform.position.x;

            if (delta < 0)
                return PlayerOrientation.Left;
            else if (delta > 0)
                return PlayerOrientation.Right;
            else
                return PlayerOrientation.Previous;
        }



        public KeyboardAndMouseController(Player player) : base(player) { }
    }
}