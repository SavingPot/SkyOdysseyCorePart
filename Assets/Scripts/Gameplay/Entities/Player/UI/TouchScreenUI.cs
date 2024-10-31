using GameCore.UI;
using SP.Tools.Unity;
using UnityEngine;

namespace GameCore
{
    public class TouchScreenUI : PlayerUIPart
    {
        public Joystick moveJoystick;
        public Joystick cursorJoystick;
        public ImageIdentity cursorImage;
        public ButtonIdentity attackButton;
        public ImageIdentity useItemButtonImage;
        public ButtonIdentity useItemButton;
        public ButtonIdentity placeBlockUnderPlayerButton;
        public ButtonIdentity pauseButton;
        public ButtonIdentity craftingButton;
        public ButtonIdentity showTaskButton;

        public void Update()
        {
            if (GControls.mode == ControlMode.Touchscreen)
            {
                PlayerUI.SetUIHighest(moveJoystick);
                PlayerUI.SetUIHighest(cursorJoystick);
                PlayerUI.SetUIHighest(cursorImage);
                PlayerUI.SetUIHighest(attackButton);
                PlayerUI.SetUIHighest(useItemButton);
                PlayerUI.SetUIHighest(placeBlockUnderPlayerButton);
                PlayerUI.SetUIHighest(pauseButton);
                PlayerUI.SetUIHighest(craftingButton);
                PlayerUI.SetUIHighest(showTaskButton);

                useItemButtonImage.image.sprite = pui.player.GetUsingItemChecked()?.data?.texture?.sprite;
                useItemButtonImage.image.color = useItemButtonImage.image.sprite ? Color.white : Color.clear;

                if (cursorJoystick.Horizontal != 0 || cursorJoystick.Vertical != 0)
                {
                    float radius = pui.player.interactiveRadius;

                    cursorImage.image.enabled = true;
                    cursorImage.rt.localPosition = new(
                        pui.player.transform.position.x + cursorJoystick.Horizontal * radius,
                        pui.player.transform.position.y + cursorJoystick.Vertical * radius);

                    pui.player.OnHoldAttack();
                }
                else
                {
                    cursorImage.image.enabled = false;
                }
            }
            else
            {
                PlayerUI.SetUIDisabled(moveJoystick);
                PlayerUI.SetUIDisabled(cursorJoystick);
                PlayerUI.SetUIDisabled(cursorImage);
                PlayerUI.SetUIDisabled(attackButton);
                PlayerUI.SetUIDisabled(useItemButton);
                PlayerUI.SetUIDisabled(placeBlockUnderPlayerButton);
                PlayerUI.SetUIDisabled(pauseButton);
                PlayerUI.SetUIDisabled(craftingButton);
                PlayerUI.SetUIDisabled(showTaskButton);
            }
        }

        internal TouchScreenUI(PlayerUI pui) : base(pui)
        {
            Vector2 touchScreenUniversalSize = new(100, 100);

            /* -------------------------------------------------------------------------- */
            /*                                    虚拟指针                                    */
            /* -------------------------------------------------------------------------- */
            {
                cursorImage = GameUI.AddImage(UIA.Middle, "ori:image.player_cursor", "ori:player_cursor", GameUI.worldSpaceCanvas.gameObject);
                cursorImage.rt.sizeDelta = Vector2.one;
            }

            /* -------------------------------------------------------------------------- */
            /*                                    添加摇杆                                    */
            /* -------------------------------------------------------------------------- */
            {
                moveJoystick = Joystick.Create("PlayerMoveJoystick", "ori:image.player_move_joystick_background", "ori:image.player_move_joystick_handle");

                cursorJoystick = Joystick.Create("PlayerCursorJoystick", "ori:image.player_cursor_joystick_background", "ori:image.player_cursor_joystick_handle");
                cursorJoystick.SetAnchorMinMax(UIA.LowerRight);
                cursorJoystick.SetAPos(-moveJoystick.rectTransform.anchoredPosition.x, moveJoystick.rectTransform.anchoredPosition.y);
            }

            /* -------------------------------------------------------------------------- */
            /*                                     攻击                                     */
            /* -------------------------------------------------------------------------- */
            {
                attackButton = GameUI.AddButton(UIA.LowerRight, "ori:button.player_attack", GameUI.canvas.transform, "ori:player_attack_button");
                Component.Destroy(attackButton.buttonText.gameObject);
                attackButton.sd = touchScreenUniversalSize;
                attackButton.SetAPosOnBySizeLeft(cursorJoystick, 150);
                attackButton.AddAPosY(75);
                attackButton.button.HideClickAction();
                attackButton.button.onClick.RemoveAllListeners();
            }

            /* -------------------------------------------------------------------------- */
            /*                                     使用                                     */
            /* -------------------------------------------------------------------------- */
            {
                useItemButton = GameUI.AddButton(UIA.LowerRight, "ori:button.player_use_item", GameUI.canvas.transform, "ori:player_use_item_button");
                Component.Destroy(useItemButton.buttonText.gameObject);
                useItemButton.sd = touchScreenUniversalSize;
                useItemButton.SetAPosOnBySizeDown(attackButton, 50);
                useItemButton.button.HideClickAction();
                useItemButton.button.onClick.RemoveAllListeners();

                useItemButtonImage = GameUI.AddImage(UIA.Middle, "ori:image.player_use_item_icon", null, useItemButton);
                useItemButtonImage.sd = useItemButton.sd * 0.5f;
            }

            /* -------------------------------------------------------------------------- */
            /*                                     在脚下放方块                                     */
            /* -------------------------------------------------------------------------- */
            {
                placeBlockUnderPlayerButton = GameUI.AddButton(UIA.LowerRight, "ori:button.player_place_block_under_player", GameUI.canvas.transform, "ori:player_place_block_under_player_button");
                Component.Destroy(placeBlockUnderPlayerButton.buttonText.gameObject);
                placeBlockUnderPlayerButton.sd = touchScreenUniversalSize;
                placeBlockUnderPlayerButton.SetAPosOnBySizeLeft(useItemButton, 50);
                placeBlockUnderPlayerButton.button.HideClickAction();
                placeBlockUnderPlayerButton.button.onClick.RemoveAllListeners();
            }

            /* -------------------------------------------------------------------------- */
            /*                                     暂停                                     */
            /* -------------------------------------------------------------------------- */
            {
                pauseButton = GameUI.AddButton(UIA.UpperRight, "ori:button.player_pause", GameUI.canvas.transform, "ori:player_pause_button");
                pauseButton.buttonText.gameObject.SetActive(false);
                pauseButton.image.rectTransform.sizeDelta = new(75, 75);
                pauseButton.image.rectTransform.anchoredPosition = new(-70, -75);
                pauseButton.button.HideClickAction();
                pauseButton.button.onClick.RemoveAllListeners();
                pauseButton.OnClickBind(() =>
                {
                    pui.PauseGame();
                });
            }

            /* -------------------------------------------------------------------------- */
            /*                                     合成                                     */
            /* -------------------------------------------------------------------------- */
            {
                craftingButton = GameUI.AddButton(UIA.UpperRight, "ori:button.player_crafting", GameUI.canvas.transform, "ori:player_crafting_button");
                craftingButton.buttonText.gameObject.SetActive(false);
                craftingButton.image.rectTransform.sizeDelta = new(75, 75);
                craftingButton.SetAPosOnBySizeDown(pauseButton, 20);
                craftingButton.button.HideClickAction();
                craftingButton.button.onClick.RemoveAllListeners();
                craftingButton.OnClickBind(() =>
                {
                    if (pui.backpackMask && GameUI.page?.ui != pui.dialogPanel)
                        pui.ShowOrHideBackpackAndSetPanelToInventory();
                });
            }

            /* -------------------------------------------------------------------------- */
            /*                                     任务                                     */
            /* -------------------------------------------------------------------------- */
            {
                showTaskButton = GameUI.AddButton(UIA.UpperRight, "ori:button.player_show_task", GameUI.canvas.transform, "ori:player_show_task_button");
                showTaskButton.buttonText.gameObject.SetActive(false);
                showTaskButton.image.rectTransform.sizeDelta = new(75, 75);
                showTaskButton.SetAPosOnBySizeDown(craftingButton, 20);
                showTaskButton.button.HideClickAction();
                showTaskButton.button.onClick.RemoveAllListeners();
                showTaskButton.OnClickBind(() =>
                {
                    if (pui.backpackMask && GameUI.page?.ui != pui.dialogPanel)
                        pui.ShowOrHideBackpackAndSetPanelToTasks();
                });
            }
        }
    }
}