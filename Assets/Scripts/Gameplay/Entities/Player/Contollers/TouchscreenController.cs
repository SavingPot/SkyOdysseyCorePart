namespace GameCore
{
    public class TouchscreenController : PlayerController
    {
        public override bool Jump() => player.pui != null && player.pui.touchScreenMoveJoystick.Vertical >= 0.65f;
        public override bool HoldingJump() => player.pui != null && player.pui.touchScreenMoveJoystick.Vertical >= 0.65f;
        public override float Move() => player.pui?.touchScreenMoveJoystick != null ? player.pui.touchScreenMoveJoystick.Horizontal : 0;
        public override bool ClickingAttack() => player.pui?.touchScreenAttackButton != null && player.pui.touchScreenAttackButton.button.wasPressedThisFrame;
        public override bool HoldingAttack() => player.pui?.touchScreenAttackButton != null && player.pui.touchScreenAttackButton.button.isPressed;

        public override PlayerOrientation SetPlayerOrientation()
        {
            if (player.pui.touchScreenCursorImage == null)
                return PlayerOrientation.Previous;



            //* 检测光标和玩家的相对位置
            var joystickX = player.pui.touchScreenCursorImage.rt.localPosition.x;
            var playerX = player.transform.position.x;



            if (joystickX < playerX)
                return PlayerOrientation.Left;
            else if (joystickX > playerX)
                return PlayerOrientation.Right;
            else
                return PlayerOrientation.Previous;
        }



        public TouchscreenController(Player player) : base(player) { }
    }
}