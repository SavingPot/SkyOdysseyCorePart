using System;
using System.Runtime.InteropServices;
using GameCore.UI;
using UnityEngine;

namespace GameCore
{
    public class TouchscreenController : PlayerController
    {
        PlayerUI pui => player.pui;
        TouchScreenUI tui => pui.TouchScreen;



        public override bool SwitchControlMode() => throw new NotImplementedException();
        public override float PlacementModeZoom() => throw new NotImplementedException();
        public override Vector2 PlacementModeMove() => throw new NotImplementedException();
        public override bool Apply() => pui != null && pui.gainRareItemButtonPanel.button.wasPressedThisFrame;
        public override bool Jump() => pui != null && tui.moveJoystick.Vertical >= 0.62f;
        public override bool HoldingJump() => pui != null && tui.moveJoystick.Vertical >= 0.62f;
        public override bool Rush(out bool direction) => throw new NotImplementedException();
        public override float Move() => tui?.moveJoystick != null ? tui.moveJoystick.Horizontal : 0;
        public override bool ClickingAttack() => tui?.attackButton != null && tui.attackButton.button.wasPressedThisFrame;
        public override bool HoldingAttack() => tui?.attackButton != null && tui.attackButton.button.isPressed;
        public override bool IsControllingBackground() => false;
        public override bool SkipDialog() => pui != null && pui.dialogPanel.button.isPressed;
        public override Vector2Int DialogOptions() => throw new NotImplementedException();
        public override bool Backpack() => false;
        public override bool Interact() => pui != null && tui.useItemButton.button.wasPressedThisFrame;
        public override bool PlaceBlockUnderPlayer() => pui != null && tui.placeBlockUnderPlayerButton.button.wasPressedThisFrame;
        public override bool DisablePlatform() => pui != null && tui.moveJoystick.Vertical <= -0.62f;
        public override bool ThrowItem() => false;
        public override bool SwitchToPreviousItem() => false;
        public override bool SwitchToNextItem() => false;
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
            if (tui.cursorImage == null)
                return PlayerOrientation.Previous;



            //* 检测光标和玩家的相对位置
            var joystickX = tui.cursorImage.rt.localPosition.x;
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