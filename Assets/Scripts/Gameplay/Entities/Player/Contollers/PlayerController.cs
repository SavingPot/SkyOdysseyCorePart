using UnityEngine;

namespace GameCore
{
    public abstract class PlayerController
    {
        public Player player { get; }



        public abstract bool SwitchControlMode();
        public abstract float PlacementModeZoom();
        public abstract Vector2 PlacementModeMove();
        public abstract bool Apply();
        public abstract bool Jump();
        public abstract bool HoldingJump();
        public abstract bool Rush(out bool direction);
        public abstract float Move();
        public abstract bool ClickingAttack();
        public abstract bool HoldingAttack();
        public abstract bool IsControllingBackground();
        public abstract bool SkipDialog();
        public abstract Vector2Int DialogOptions();
        public abstract bool Backpack();
        public abstract bool Interact();
        public abstract bool PlaceBlockUnderPlayer();
        public abstract PlayerOrientation SetPlayerOrientation();
        public abstract bool DisablePlatform();
        public abstract bool ThrowItem();
        public abstract bool SwitchToPreviousItem();
        public abstract bool SwitchToNextItem();
        public abstract bool SwitchToItem1();
        public abstract bool SwitchToItem2();
        public abstract bool SwitchToItem3();
        public abstract bool SwitchToItem4();
        public abstract bool SwitchToItem5();
        public abstract bool SwitchToItem6();
        public abstract bool SwitchToItem7();
        public abstract bool SwitchToItem8();



        public enum PlayerOrientation
        {
            Right,
            Left,
            Previous
        }



        public PlayerController(Player player)
        {
            this.player = player;
        }
    }
}