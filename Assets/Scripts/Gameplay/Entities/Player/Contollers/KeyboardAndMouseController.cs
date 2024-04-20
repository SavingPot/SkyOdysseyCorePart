using UnityEngine;
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
        public override bool IsControllingBackground() => Keyboard.current != null && Keyboard.current.ctrlKey.isPressed;
        public override bool SkipDialog() => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        public override Vector2Int DialogOptions() => GControls.GetWASDVec();
        public override bool Backpack() => Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
        public override bool UseItem() => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        public override bool PlaceBlockUnderPlayer() => Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame;
        public override bool ThrowItem() => Keyboard.current?.backquoteKey?.wasPressedThisFrame ?? false;
        public override bool SwitchToPreviousItem() => Mouse.current != null && Mouse.current.scroll.y.ReadValue() > 0;
        public override bool SwitchToNextItem() => Mouse.current != null && Mouse.current.scroll.y.ReadValue() < 0;
        public override bool SwitchToItem1() => Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame;
        public override bool SwitchToItem2() => Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame;
        public override bool SwitchToItem3() => Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame;
        public override bool SwitchToItem4() => Keyboard.current != null && Keyboard.current.digit4Key.wasPressedThisFrame;
        public override bool SwitchToItem5() => Keyboard.current != null && Keyboard.current.digit5Key.wasPressedThisFrame;
        public override bool SwitchToItem6() => Keyboard.current != null && Keyboard.current.digit6Key.wasPressedThisFrame;
        public override bool SwitchToItem7() => Keyboard.current != null && Keyboard.current.digit7Key.wasPressedThisFrame;
        public override bool SwitchToItem8() => Keyboard.current != null && Keyboard.current.digit8Key.wasPressedThisFrame;

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