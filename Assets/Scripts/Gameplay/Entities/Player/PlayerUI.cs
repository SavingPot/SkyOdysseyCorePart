using Cysharp.Threading.Tasks;
using GameCore.Converters;
using GameCore.High;
using GameCore.UI;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;

namespace GameCore
{
    public class DialogData
    {
        public List<DialogDatum> dialogs;
        public string audioId;
        public string name;

        public DialogData(string name, string audioId, params DialogDatum[] dialogs)
        {
            this.dialogs = dialogs.ToList();
            this.audioId = audioId;
            this.name = name;
        }



        public class DialogDatum
        {
            public string text;
            public string head;
            public float waitTime;
            public bool continued;
            public Dictionary<int, Action> options;

            public DialogDatum(string text, string head, float waitTime = 0.05f, bool continued = false, Dictionary<int, Action> options = null)
            {
                this.text = text;
                this.head = head;
                this.waitTime = waitTime;
                this.continued = continued;
                this.options = options ?? new();
            }
        }
    }

    public class PlayerUI
    {
        public static PlayerUI instance;
        public readonly Player player;



        /* -------------------------------------------------------------------------- */
        /*                                     暂停                                     */
        /* -------------------------------------------------------------------------- */
        public PanelIdentity pausePanel;
        public PanelIdentity pausePanelMask;



        /* -------------------------------------------------------------------------- */
        /*                                     聊天                                     */
        /* -------------------------------------------------------------------------- */
        public ScrollViewIdentity chatView;
        public InputButtonIdentity chatInput;



        public static void AddChatMsg(Sprite portrait, string playerName, string msg)
        {
            if (instance == null || !instance.chatView)
                return;

            /* ------------------------ 添加文本, 设置向左对其, 不能溢出, 设置内容 ------------------------ */
            var text = GameUI.AddText(UPC.middle, $"ori:text.chat.{msg}");
            text.autoCompareText = false;
            text.text.alignment = TMPro.TextAlignmentOptions.Left;
            text.text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            text.text.text = $"{playerName}: {msg}";

            var image = GameUI.AddImage(UPC.left, $"ori:image.chat.{msg}", null, text);
            image.sd = new(instance.chatView.gridLayoutGroup.cellSize.y, instance.chatView.gridLayoutGroup.cellSize.y);
            image.ap = new(image.sd.x / 2, 0);
            image.image.sprite = portrait;

            //限制文本, 防止被图片挡住
            text.text.margin = new(image.sd.x + 5, 5, 5, 5);

            instance.chatView.AddChild(text);
        }



        /* -------------------------------------------------------------------------- */
        /*                                     昼夜条                                    */
        /* -------------------------------------------------------------------------- */
        public ImageIdentity dayNightBar;
        public ImageIdentity dayNightBarPointer;



        /* -------------------------------------------------------------------------- */
        /*                                     状态                                     */
        /* -------------------------------------------------------------------------- */
        public TextIdentity statusText;

        public static float statusTextFadeOutTime = 5;
        public float statusTextFadeOutWaitedTime;
        public bool preparingToFadeOutStatusText;



        /* -------------------------------------------------------------------------- */
        /*                                     重生                                     */
        /* -------------------------------------------------------------------------- */
        public PanelIdentity rebornPanel;
        public TextIdentity rebornPanelText;
        public TextIdentity rebornTimerText;
        public ButtonIdentity rebornButton;



        public async void ShowRebornPanel()
        {
            await 3;

            rebornPanel.panelImage.color = new(0, 0, 0, 0);
            rebornButton.buttonText.text.color = new(1, 1, 1, 0);
            rebornButton.button.image.color = new(1, 1, 1, 0);
            rebornButton.button.interactable = false;
            GameUI.FadeIn(rebornPanel.panelImage);

            await 3;
            rebornButton.button.interactable = true;
            GameUI.FadeIn(rebornButton.image);
            GameUI.FadeIn(rebornButton.buttonText.text);
        }



        /* -------------------------------------------------------------------------- */
        /*                                     背包                                    */
        /* -------------------------------------------------------------------------- */
        public PanelIdentity backpackMask;
        public readonly InventorySlotUI[] quickInventorySlots;

        public InventorySlotUI[] inventorySlotsUIs = new InventorySlotUI[Player.inventorySlotCount];
        public InventorySlotUI inventoryHelmetUI;
        public InventorySlotUI inventoryBreastplateUI;
        public InventorySlotUI inventoryLeggingUI;
        public InventorySlotUI inventoryBootsUI;

        /* ----------------------------------- 背包 ----------------------------------- */
        public ScrollViewIdentity inventoryItemView;
        public Color backpackColor = Color.white;


        /* ----------------------------------- 合成 ----------------------------------- */
        public KeyValuePair<CraftingRecipe, List<Dictionary<int, ushort>>>? craftingSelectedRecipe;
        public Dictionary<CraftingRecipe, List<Dictionary<int, ushort>>> craftingResults = new();
        public ScrollViewIdentity craftingResultView;
        public ScrollViewIdentity craftingStuffView;
        public ButtonIdentity craftingApplyButton;
        public TextIdentity craftingSelectedItemTitleText;



        /* -------------------------------------------------------------------------- */
        /*                                     属性                                     */
        /* -------------------------------------------------------------------------- */
        public ImageIdentity thirstBarBg;
        public ImageIdentity hungerBarBg;
        public ImageIdentity happinessBarBg;
        public ImageIdentity healthBarBg;
        public ImageIdentity thirstBarFull;
        public ImageIdentity hungerBarFull;
        public ImageIdentity happinessBarFull;
        public ImageIdentity healthBarFull;



        /* -------------------------------------------------------------------------- */
        /*                                    任务系统                                    */
        /* -------------------------------------------------------------------------- */
        public ScrollViewIdentity taskView;
        public ImageIdentity taskCompleteBackground;
        public ImageIdentity taskCompleteIcon;
        public TextIdentity taskCompleteText;

        public List<TaskData> tasks = new();
        public TaskNode taskNode;

        public void AddTask(string id, string icon, string parent, string[] rewards)
        {
            tasks.Add(new(id, icon, parent, rewards));
        }



        /* -------------------------------------------------------------------------- */
        /*                                    手机端操纵                                   */
        /* -------------------------------------------------------------------------- */
        public Joystick moveJoystick;
        public Joystick cursorJoystick;
        public ImageIdentity cursorImage;
        public ButtonIdentity attackButton;
        public ButtonIdentity interactionButton;
        public ImageIdentity useItemButtonImage;
        public ButtonIdentity useItemButton;
        public ButtonIdentity craftingButton;





        /* -------------------------------------------------------------------------- */
        /*                                     对话                                     */
        /* -------------------------------------------------------------------------- */
        public PanelIdentity dialogPanel;
        public ImageIdentity dialogHead;
        public TextIdentity dialogNameText;
        public TextIdentity dialogText;

        public DialogData displayingDialog;
        public Task dialogTask;

        public void DisplayDialog(DialogData data)
        {
            if (displayingDialog != null)
            {
                Debug.LogError("一个对话已在播放");
                return;
            }

            displayingDialog = data;
            dialogTask = DisplayDialogTask();
        }

        public static string[] dialogRichTextSupported = new[]
        {
            "<color="
        };

        public async Task DisplayDialogTask()
        {
            GameUI.SetPage(dialogPanel, GameUI.DisappearType.PositionUpToDown, GameUI.AppearType.PositionDownToUp);

            for (int i = 0; i < displayingDialog.dialogs.Count; i++)
            {
                var current = displayingDialog.dialogs[i];
                dialogText.text.text = string.Empty;

                string fullContent = current.text;
                char[] fullContentChars = fullContent.ToCharArray();
                dialogHead.image.sprite = ModFactory.CompareTexture(current.head).sprite;
                dialogNameText.text.text = GameUI.CompareText(displayingDialog.name).text;

                for (int t = 0; t < fullContent.Length;)
                {
                    var item = fullContent[t];
                    var charsAfterItem = new ArraySegment<char>(fullContentChars, t, fullContent.Length - t).ToArray(); //? 包括 item
                    var strAfterItem = new string(charsAfterItem);
                    string output;
                    int tDelta;

                    if (dialogRichTextSupported.Any(p => strAfterItem.StartsWith(p)))
                    {
                        var endIndex = strAfterItem.IndexOf('>') + 1; //? 如果不 +1, 富文本会瞬间闪烁然后消失 

                        tDelta = endIndex;
                        output = new string(new ArraySegment<char>(charsAfterItem, 0, endIndex).ToArray());
                    }
                    else if (strAfterItem.StartsWith("</"))
                    {
                        var endIndex = strAfterItem.IndexOf('>') + 1; //? 如果不 +1, 富文本会瞬间闪烁然后消失

                        tDelta = endIndex;
                        output = new string(new ArraySegment<char>(charsAfterItem, 0, endIndex).ToArray());
                    }
                    else
                    {
                        output = item.ToString();
                        tDelta = 1;
                    }

                    dialogText.text.text += output;

                    float timer = Tools.time + current.waitTime;

                    while (Tools.time < timer)
                    {
                        if (PlayerControls.SkipDialog(player))
                        {
                            dialogText.text.text = fullContent;
                            await UniTask.NextFrame();   //等一帧, 防止连续跳过 (我猜会有这个问题:D)
                            goto finishContent;
                        }

                        await UniTask.NextFrame();
                    }

                    t += tDelta;
                }

                if (current.continued || i == displayingDialog.dialogs.Count - 1)
                    goto finishContentDirectly;

                finishContent:
                while (!PlayerControls.SkipDialog(player))
                    await UniTask.NextFrame();

                finishContentDirectly:
                await UniTask.NextFrame();

                //TODO: OPTIONS COMPLETE
                if (current.options.Count != 0)
                {

                }
                continue;
            }

            while (!PlayerControls.SkipDialog(player))
                await UniTask.NextFrame();   //等一帧, 防止连续跳过 (我猜会有这个问题:D)

            //等一帧防止跳跃
            await UniTask.NextFrame();

            GameUI.SetPage(null);
            displayingDialog = null;
        }





        public void PauseGame()
        {
            if (GScene.name != SceneNames.GameScene)
                return;

            if (!pausePanel)
                return;

            if ((GameUI.page == null || !GameUI.page.ui) && GameUI.page.ui != pausePanel)
                GameUI.SetPage(pausePanel);
            else
                GameUI.SetPageBack();
        }

        public void LeftGame()
        {
            //保存数据
            GFiles.SaveAllDataToFiles();

            if (Server.isServer)
            {
                //如果是服务器, 截图并关闭 Host
                GameUI.canvas.gameObject.SetActive(false);

                ScreenTools.CaptureSquare(GFiles.world.worldImagePath, () =>
                {
                    GameUI.canvas.gameObject.SetActive(true);
                    GameUI.FadeIn(pausePanelMask.panelImage, true, 1, new(() =>
                    {
                        //清除方块防止警告
                        if (Map.HasInstance())
                        {
                            Map.instance.RecoverChunks();
                        }

                        ManagerNetwork.instance.StopHost();
                    }));
                });
            }
            //如果是单纯的客户端
            else
            {
                GameUI.FadeIn(pausePanelMask.panelImage, true, 1, new(() =>
                {
                    Client.Disconnect();
                }));
            }
        }

        public void Chat()
        {
            if (GScene.name != SceneNames.GameScene)
            {
                Debug.LogWarning("场景不是游戏场景");
                return;
            }

            if (!chatView)
            {
                Debug.LogWarning($"{nameof(chatView)} 不存在");
                return;
            }

            if (GameUI.page.ui == chatView)
            {
                GameUI.SetPageBack();
            }
            else if (GameUI.page == null || !GameUI.page.ui)
            {
                GameUI.SetPage(chatView);
            }
        }





        public PlayerUI(Player player)
        {
            instance = this;
            this.player = player;

            /* -------------------------------------------------------------------------- */
            /*                                    暂停界面                                    */
            /* -------------------------------------------------------------------------- */

            pausePanel = GameUI.AddPanel("ori:panel.pause", GameUI.canvas.transform, true);
            pausePanelMask = GameUI.AddPanel("ori:panel.pause.mask", pausePanel, true);

            pausePanel.panelImage.SetColorBrightness(0.175f);
            pausePanel.panelImage.SetAlpha(0.65f);
            pausePanel.OnUpdate += x => GameUI.SetUILayerToTop(x);

            pausePanelMask.panelImage.color = new(0, 0, 0, 0);
            pausePanelMask.OnUpdate += x => GameUI.SetUILayerToTop(x);

            ButtonIdentity continueGame = GameUI.AddButton(UPC.middle, "ori:button.pause_continue_game", pausePanel).OnClickBind(() =>
            {
                GameUI.SetPage(null);
            });
            ButtonIdentity quitGame = GameUI.AddButton(UPC.middle, "ori:button.pause_quit_game", pausePanel).OnClickBind(LeftGame);

            continueGame.rt.AddLocalPosY(30);
            quitGame.rt.AddLocalPosY(-30);



            /* -------------------------------------------------------------------------- */
            /*                                     聊天                                     */
            /* -------------------------------------------------------------------------- */
            //生成面板, 设置颜色为深灰色半透明
            chatView = GameUI.AddScrollView(UPC.stretchDouble, "ori:view.chat");
            chatView.scrollViewImage.color = new(0.2f, 0.2f, 0.2f, 0.6f);
            chatView.rt.sizeDelta = Vector2.zero;
            chatView.gameObject.SetActive(false);

            chatInput = GameUI.AddInputButton(UPC.down, "ori:input_button.chat", chatView);
            chatInput.field.image.color = new(1, 1, 1, 0.8f);
            chatInput.button.image.color = new(1, 1, 1, 0.8f);
            chatInput.AddMethod(() =>
            {
                if (!Player.local)
                    return;

                Client.Send<NMChat>(new(ByteConverter.ToBytes(Player.local.head.sr.sprite), Player.local.playerName, chatInput.field.field.text));
            });

            chatView.OnUpdate += _ =>
            {
                Vector2 sd = new(GameUI.canvasScaler.referenceResolution.x, 50);
                chatInput.SetSize(sd);
                chatInput.ap = new(0, sd.y / 2);

                chatView.gridLayoutGroup.cellSize = new(GameUI.canvasScaler.referenceResolution.x, 50);
                chatView.gridLayoutGroup.spacing = new(0, 10);

                GameUI.SetUILayerToTop(chatView);
            };



            /* -------------------------------------------------------------------------- */
            /*                                    任务系统                                    */
            /* -------------------------------------------------------------------------- */

            /* ----------------------------------- 生成任务视图 ----------------------------------- */
            //生成面板, 设置颜色为深灰色半透明
            taskView = GameUI.AddScrollView(UPC.stretchDouble, "ori:view.task");
            taskView.scrollViewImage.color = new(0.2f, 0.2f, 0.2f, 0.6f);
            taskView.rt.sizeDelta = Vector2.zero;
            taskView.gameObject.SetActive(false);
            taskView.scrollRect.horizontal = true;   //允许水平拖拽
            taskView.scrollRect.movementType = ScrollRect.MovementType.Unrestricted;   //不限制拖拽
            taskView.scrollRect.scrollSensitivity = 0;   //不允许滚轮控制
            taskView.content.localScale = new(0.08f, 0.08f, 1);   //缩小界面
            taskView.content.anchoredPosition = new(GameUI.canvasScaler.referenceResolution.x / 2, GameUI.canvasScaler.referenceResolution.y / 2);  //将任务居中
            taskView.viewportMask.enabled = false;   //关闭显示剔除
            UnityEngine.Object.Destroy(taskView.gridLayoutGroup);   //删除自动排序器
            UnityEngine.Object.Destroy(taskView.scrollRect.verticalScrollbar.gameObject);   //删除滚动条

            taskView.OnUpdate += _ =>
            {
                GameUI.SetUILayerToTop(taskView);
            };

            /* -------------------------------- 生成任务完成图像 -------------------------------- */
            taskCompleteBackground = GameUI.AddImage(UPC.upperLeft, "ori:image.task_complete_background", "ori:task_complete");
            taskCompleteBackground.SetSizeDelta(320, 100);
            taskCompleteBackground.SetAPos(taskCompleteBackground.sd.x / 2, -taskCompleteBackground.sd.y / 2);
            taskCompleteBackground.gameObject.SetActive(false);
            taskCompleteBackground.OnUpdate += _ =>
            {
                GameUI.SetUILayerToTop(taskView);
            };

            taskCompleteIcon = GameUI.AddImage(UPC.left, "ori:image.task_complete_icon", null, taskCompleteBackground);
            taskCompleteIcon.SetSizeDelta(taskCompleteBackground.sd.y, taskCompleteBackground.sd.y);
            taskCompleteIcon.SetAPosX(taskCompleteIcon.sd.x / 2);

            taskCompleteText = GameUI.AddText(UPC.middle, "ori:text.task_complete", taskCompleteBackground);
            taskCompleteText.sd = taskCompleteBackground.sd;
            taskCompleteText.text.margin = new(taskCompleteIcon.sd.x + 5, 5, 5, 5);
            taskCompleteText.autoCompareText = false;

            /* ---------------------------------- 绑定任务 ---------------------------------- */
            BindTasks(this);

            /* --------------------------------- 生成任务节点 --------------------------------- */
            foreach (var item in tasks)
            {
                //是根任务
                if (string.IsNullOrWhiteSpace(item.parent))
                {
                    taskNode = Do_Internal(item);
                    break;
                }
            }

            TaskNode Do_Internal(TaskData data)
            {
                TaskNode temp = new(data);

                foreach (var task in tasks)
                {
                    //如果是 current 的子任务
                    if (task.parent == data.id)
                    {
                        temp.nodes.Add(Do_Internal(task));
                    }
                }

                return temp;
            }

            /* --------------------------------- 显示任务节点 --------------------------------- */
            RefreshTasks(true);

            /* --------------------------------- 加载已有任务 --------------------------------- */
            foreach (var task in player.completedTasks)
            {
                CompleteTask(task.id, false, task.hasGotRewards);
            }




            #region 手机

            Vector2 phoneUniversalSize = new(100, 100);

            /* -------------------------------------------------------------------------- */
            /*                                    虚拟指针                                    */
            /* -------------------------------------------------------------------------- */
            {
                cursorImage = GameUI.AddImage(UPC.middle, "ori:image.player_cursor", "ori:player_cursor", GameUI.worldSpaceCanvas.gameObject);
                cursorImage.rt.sizeDelta = Vector2.one;
            }

            /* -------------------------------------------------------------------------- */
            /*                                    添加摇杆                                    */
            /* -------------------------------------------------------------------------- */
            {
                moveJoystick = Joystick.Create("PlayerMoveJoystick", "ori:image.player_move_joystick_background", "ori:image.player_move_joystick_handle");

                cursorJoystick = Joystick.Create("PlayerCursorJoystick", "ori:image.player_cursor_joystick_background", "ori:image.player_cursor_joystick_handle");
                cursorJoystick.SetAnchorMinMax(UPC.lowerRight);
                cursorJoystick.SetAPos(-moveJoystick.rectTransform.anchoredPosition.x, moveJoystick.rectTransform.anchoredPosition.y);
            }

            /* -------------------------------------------------------------------------- */
            /*                                     攻击                                     */
            /* -------------------------------------------------------------------------- */
            {
                attackButton = GameUI.AddButton(UPC.lowerRight, "ori:button.player_attack", GameUI.canvas.transform, "ori:player_attack_button");
                Component.Destroy(attackButton.buttonText.gameObject);
                attackButton.sd = phoneUniversalSize;
                attackButton.SetAPosOnBySizeLeft(cursorJoystick, 150);
                attackButton.AddAPosY(75);
                attackButton.button.HideClickAction();
                attackButton.button.onClick.RemoveAllListeners();
            }

            /* -------------------------------------------------------------------------- */
            /*                                     使用                                     */
            /* -------------------------------------------------------------------------- */
            {
                useItemButton = GameUI.AddButton(UPC.lowerRight, "ori:button.player_use_item", GameUI.canvas.transform, "ori:player_use_item_button");
                Component.Destroy(useItemButton.buttonText.gameObject);
                useItemButton.sd = phoneUniversalSize;
                useItemButton.SetAPosOnBySizeDown(attackButton, 50);
                useItemButton.button.HideClickAction();
                useItemButton.button.onClick.RemoveAllListeners();

                useItemButtonImage = GameUI.AddImage(UPC.middle, "ori:image.player_use_item_icon", null, useItemButton);
                useItemButtonImage.sd = useItemButton.sd * 0.5f;
            }

            /* -------------------------------------------------------------------------- */
            /*                                     互动                                     */
            /* -------------------------------------------------------------------------- */
            {
                interactionButton = GameUI.AddButton(UPC.lowerLeft, "ori:button.player_interaction", GameUI.canvas.transform, "ori:player_interaction_button");
                Component.Destroy(interactionButton.buttonText.gameObject);
                interactionButton.sd = phoneUniversalSize;
                interactionButton.SetAPosOnBySizeRight(moveJoystick, 150);
                interactionButton.button.HideClickAction();
                interactionButton.button.onClick.RemoveAllListeners();
            }

            /* -------------------------------------------------------------------------- */
            /*                                     合成                                     */
            /* -------------------------------------------------------------------------- */
            {
                craftingButton = GameUI.AddButton(UPC.upperRight, "ori:button.player_crafting", GameUI.canvas.transform, "ori:player_crafting_button");
                craftingButton.buttonText.gameObject.SetActive(false);
                craftingButton.image.rectTransform.sizeDelta = new(75, 75);
                craftingButton.image.rectTransform.anchoredPosition = new(-70, -75);
                craftingButton.button.HideClickAction();
                craftingButton.button.onClick.RemoveAllListeners();
                craftingButton.OnClickBind(() =>
                {
                    if (backpackMask)
                        player.ShowOrHideBackpackAndSetSidebarToCrafting();
                });
            }

            #endregion

            /* -------------------------------------------------------------------------- */
            /*                                    对话                                    */
            /* -------------------------------------------------------------------------- */
            {
                dialogPanel = GameUI.AddPanel("ori:panel.dialog");
                dialogPanel.gameObject.SetActive(false);
                dialogPanel.SetAnchorMinMax(0, 0, 1, 0.4f);
                dialogPanel.panelImage.SetColor(0.15f, 0.15f, 0.15f, 0.6f);

                dialogHead = GameUI.AddImage(UPC.upperLeft, "ori:image.dialog_head", null, dialogPanel);
                dialogHead.SetSizeDelta(160, 160);
                dialogHead.ap = new(dialogHead.sd.x / 2, -dialogHead.sd.y / 2);

                dialogNameText = GameUI.AddText(UPC.down, "ori:text.dialog_name", dialogHead);
                dialogNameText.SetAPosY(-dialogNameText.sd.y / 2 - 10);
                dialogNameText.doRefresh = false;

                dialogText = GameUI.AddText(UPC.right, "ori:text.dialog", dialogHead);
                dialogText.text.SetFontSize(28);
                dialogText.doRefresh = false;
                dialogText.OnUpdate += x =>
                {
                    x.SetSizeDelta(GameUI.canvasScaler.referenceResolution.x - dialogHead.sd.x, dialogHead.sd.y);
                    x.SetAPos(dialogText.sd.x / 2, 0);
                };
            }

            #region 添加主物品栏与左右手按钮
            {
                //物品栏格子数
                int quickInventorySlotCount = Player.quickInventorySlotCount;
                int buttonSize = 45;
                int handUIInterval = 50;
                float itemMultiple = 0.55f;
                float buttonTextSize = 2;
                Vector2 buttonTextAnchorPos = new(0, -7f);
                Vector2 vecButtonSize = new(buttonSize, buttonSize);
                Vector2 vecButtonItemSize = new(buttonSize * itemMultiple, buttonSize * itemMultiple);
                int buttonExtraY = 20;

                buttonExtraY += buttonSize / 2;
                handUIInterval += buttonSize;

                List<InventorySlotUI> quickInventorySlotTemp = new();

                #region 添加快速物品栏
                for (int index = -quickInventorySlotCount / 2; index < quickInventorySlotCount / 2; index++)
                {
                    int i = index;
                    int indexAs0 = index + quickInventorySlotCount / 2;
                    var button = GameUI.AddButton(UPC.down, "ori:button.item_tab_" + indexAs0, GameUI.canvas.transform, "ori:item_tab");
                    var item = GameUI.AddImage(UPC.down, "ori:image.item_tab_item_" + indexAs0, "ori:item_tab", button);

                    item.rectTransform.SetParentForUI(button.rectTransform);
                    button.rectTransform.sizeDelta = vecButtonSize;
                    button.rectTransform.AddLocalPosX((i + 0.5f) * buttonSize);
                    button.rectTransform.AddLocalPosY(buttonExtraY);
                    item.rectTransform.sizeDelta = vecButtonItemSize;

                    //Destroy(button.buttonText.gameObject);
                    button.buttonText.rectTransform.anchorMin = UPC.down;
                    button.buttonText.rectTransform.anchorMax = UPC.down;
                    button.buttonText.text.fontSize = buttonTextSize;
                    button.buttonText.autoCompareText = false;
                    button.buttonText.rectTransform.anchoredPosition = buttonTextAnchorPos;

                    button.button.ClearColorEffects();
                    button.button.onClick = new();

                    button.OnClickBind(() =>
                    {
                        if (!backpackMask.gameObject.activeSelf)
                        {
                            player.SwitchItem(indexAs0);
                        }
                        else
                        {
                            player.ServerSwapItemOnHand(indexAs0.ToString());
                        }
                    });

                    quickInventorySlotTemp.Add(new(button, item));
                }

                quickInventorySlots = quickInventorySlotTemp.ToArray();
                #endregion
            }
            #endregion

            #region 添加背包界面
            {
                //背包的遮罩
                backpackMask = GameUI.AddPanel("ori:panel.backpack_panel_mask");
                backpackMask.panelImage.color = new Color32(50, 50, 50, 80);
                backpackMask.gameObject.SetActive(false);

                #region 物品栏

                //背包物品视图
                inventoryItemView = GameUI.AddScrollView(UPC.middle, "ori:sw.backpack_inventory_items", backpackMask);
                inventoryItemView.SetAnchorMin(0.5f, 0.5f);
                inventoryItemView.SetAnchorMax(0.5f, 0.5f);
                inventoryItemView.SetSizeDelta(640, Player.backpackPanelHeight);
                inventoryItemView.viewportImage.sprite = ModFactory.CompareTexture("ori:backpack_inventory_background").sprite;
                inventoryItemView.gridLayoutGroup.cellSize = new(80, 80);
                inventoryItemView.scrollViewImage.color = Color.clear;
                inventoryItemView.viewportImage.color = backpackColor;
                inventoryItemView.content.sizeDelta = new(0, inventoryItemView.content.sizeDelta.y);
                inventoryItemView.content.anchoredPosition = new(-inventoryItemView.content.sizeDelta.x / 2, inventoryItemView.content.anchoredPosition.y);

                for (int i = 0; i < inventorySlotsUIs.Length; i++)
                {
                    int index = i;
                    var ui = InventorySlotUI.Generate($"ori:button.backpack_inventory_item_{index}", $"ori:image.backpack_inventory_item_{index}", inventoryItemView.gridLayoutGroup.cellSize);

                    inventorySlotsUIs[i] = ui;
                    inventoryItemView.AddChild(ui.button);
                }

                inventoryHelmetUI = InventorySlotUI.Generate($"ori:button.backpack_inventory_item_{Inventory.helmetVar}", $"ori:image.backpack_inventory_item_{Inventory.helmetVar}", inventoryItemView.gridLayoutGroup.cellSize);
                inventoryBreastplateUI = InventorySlotUI.Generate($"ori:button.backpack_inventory_item_{Inventory.breastplateVar}", $"ori:image.backpack_inventory_item_{Inventory.breastplateVar}", inventoryItemView.gridLayoutGroup.cellSize);
                inventoryLeggingUI = InventorySlotUI.Generate($"ori:button.backpack_inventory_item_{Inventory.leggingVar}", $"ori:image.backpack_inventory_item_{Inventory.leggingVar}", inventoryItemView.gridLayoutGroup.cellSize);
                inventoryBootsUI = InventorySlotUI.Generate($"ori:button.backpack_inventory_item_{Inventory.bootsVar}", $"ori:image.backpack_inventory_item_{Inventory.bootsVar}", inventoryItemView.gridLayoutGroup.cellSize);

                inventoryHelmetUI.button.transform.SetParent(inventoryItemView.gridLayoutGroup.transform.parent);
                inventoryBreastplateUI.button.transform.SetParent(inventoryItemView.gridLayoutGroup.transform.parent);
                inventoryLeggingUI.button.transform.SetParent(inventoryItemView.gridLayoutGroup.transform.parent);
                inventoryBootsUI.button.transform.SetParent(inventoryItemView.gridLayoutGroup.transform.parent);

                inventoryHelmetUI.button.SetAnchorMinMax(UPC.lowerLeft);
                inventoryBreastplateUI.button.SetAnchorMinMax(UPC.lowerLeft);
                inventoryLeggingUI.button.SetAnchorMinMax(UPC.lowerLeft);
                inventoryBootsUI.button.SetAnchorMinMax(UPC.lowerLeft);

                inventoryHelmetUI.button.ap = inventoryHelmetUI.button.sd / 2;
                inventoryBreastplateUI.button.SetAPosOnBySizeRight(inventoryHelmetUI.button, 0);
                inventoryLeggingUI.button.SetAPosOnBySizeRight(inventoryBreastplateUI.button, 0);
                inventoryBootsUI.button.SetAPosOnBySizeRight(inventoryLeggingUI.button, 0);

                inventoryItemView.CustomMethod += (type, param) =>
                {
                    type ??= "refresh";

                    if (type == "refresh")
                    {
                        for (int i = 0; i < inventorySlotsUIs.Length; i++)
                        {
                            int index = i;

                            var inventorySlot = inventorySlotsUIs[index];

                            inventorySlot.Refresh(player, index.ToString());
                        }

                        inventoryHelmetUI.Refresh(player, Inventory.helmetVar, item => Item.Null(item) || item.data.Helmet != null);
                        inventoryBreastplateUI.Refresh(player, Inventory.breastplateVar, item => Item.Null(item) || item.data.Breastplate != null);
                        inventoryLeggingUI.Refresh(player, Inventory.leggingVar, item => Item.Null(item) || item.data.Legging != null);
                        inventoryBootsUI.Refresh(player, Inventory.bootsVar, item => Item.Null(item) || item.data.Boots != null);
                    }
                };

                #endregion

                #region 合成

                //制作的物品的名称
                craftingSelectedItemTitleText = GameUI.AddText(UPC.up, "ori:text.crafting_chose_item", inventoryItemView);
                craftingSelectedItemTitleText.SetAPos(0, 30);
                craftingSelectedItemTitleText.text.color = Color.black;
                craftingSelectedItemTitleText.text.text = string.Empty;
                craftingSelectedItemTitleText.AfterRefreshing += ct =>
                {
                    if (craftingSelectedRecipe == null)
                    {
                        craftingSelectedItemTitleText.text.text = string.Empty;
                        return;
                    }

                    var temp = (KeyValuePair<CraftingRecipe, List<Dictionary<int, ushort>>>)craftingSelectedRecipe;
                    var itemGot = ModConvert.ItemDataToItem(ModFactory.CompareItem(temp.Key.result.id));
                    ct.text.text = GameUI.CompareText(itemGot.data.id)?.text;
                };
                craftingSelectedItemTitleText.gameObject.SetActive(false);

                //确认制作
                craftingApplyButton = GameUI.AddButton(UPC.down, "ori:button.crafting_chose_item", inventoryItemView);
                craftingApplyButton.SetAPos(0, -30);
                craftingApplyButton.OnClickBind(() =>
                {
                    if (craftingSelectedRecipe == null || player.inventory.IsFull())
                        return;

                    var temp = (KeyValuePair<CraftingRecipe, List<Dictionary<int, ushort>>>)craftingSelectedRecipe;

                    //从玩家身上减去物品
                    temp.Value.For(stuffToRemove =>
                    {
                        stuffToRemove.For(itemToRemove =>
                        {
                            player.ServerReduceItemCount(itemToRemove.Key.ToString(), itemToRemove.Value);
                        });
                    });

                    //给予玩家物品
                    var resultItem = ModConvert.ItemDataToItem(ModFactory.CompareItem(temp.Key.result.id));
                    for (int i = 0; i < temp.Key.result.count; i++)
                    {
                        player.ServerAddItem(resultItem);
                    }

                    CompleteTask("ori:craft");
                    GAudio.Play(AudioID.Crafting);
                });
                craftingApplyButton.gameObject.SetActive(false);

                //制作原料
                GenerateSidebar(SidebarType.Right, "ori:scrollview.crafting_stuff", 70, 210, Vector2.zero, "ori:crafting_result", "ori:sidebar_sign.crafting_stuff", out craftingStuffView, out _, out _);
                craftingStuffView.CustomMethod += (type, _) =>
                {
                    type ??= "refresh";

                    if (type == "refresh")
                    {
                        craftingStuffView.Clear();

                        if (craftingSelectedRecipe == null)
                            return;

                        var temp = (KeyValuePair<CraftingRecipe, List<Dictionary<int, ushort>>>)craftingSelectedRecipe;

                        foreach (var ele in temp.Value)
                        {
                            foreach (var stuffPair in ele)
                            {
                                Item itemGot = player.inventory.GetItem(stuffPair.Key);

                                var stuffItemButton = GameUI.AddButton(UPC.up, $"ori:button.chose_craft_recipe_stuff_{stuffPair.Key}");
                                var stuffItemImage = GameUI.AddImage(UPC.middle, $"ori:image.chose_craft_recipe_stuff_{stuffPair.Key}", null, stuffItemButton);

                                //按钮
                                stuffItemButton.button.OnPointerStayAction += () => ItemInfoShower.Show(itemGot);
                                stuffItemButton.button.OnPointerExitAction += _ => ItemInfoShower.Hide();
                                stuffItemButton.image.sprite = ModFactory.CompareTexture("ori:item_tab").sprite;
                                stuffItemButton.buttonText.AfterRefreshing += bt =>
                                {
                                    bt.text.text = $"{GameUI.CompareText(itemGot.data.id)?.text}[{stuffPair.Value}]";
                                };
                                stuffItemButton.buttonText.rectTransform.AddLocalPosY(-38);
                                stuffItemButton.buttonText.SetSizeDelta(85, 27);
                                stuffItemButton.RefreshUI();

                                //图标
                                stuffItemImage.sd = craftingStuffView.gridLayoutGroup.cellSize * 0.75f;
                                stuffItemImage.image.sprite = Item.Null(itemGot) ? null : itemGot.data.texture.sprite;

                                craftingStuffView.AddChild(stuffItemButton);
                            }
                        }
                    }
                };
                craftingStuffView.gameObject.SetActive(false);

                //制作结果
                GenerateSidebar(SidebarType.Left, "ori:scrollview.crafting_results", 70, 210, Vector2.zero, "ori:crafting_result", "ori:sidebar_sign.crafting_results", out craftingResultView, out _, out _);
                craftingResultView.CustomMethod += (type, _) =>
                {
                    type ??= "refresh";

                    if (type == "refresh")
                    {
                        //获取本地玩家的所有物品
                        craftingResultView.Clear();
                        craftingResults = Player.GetCraftingRecipesThatCanBeCrafted(player.inventory.slots);

                        foreach (var pair in craftingResults)
                        {
                            var cr = pair.Key;
                            var itemGot = ModFactory.CompareItem(cr.result.id);

                            //添加按钮
                            var button = GameUI.AddButton(UPC.up, $"ori:button.player_crafting_recipe_{cr.id}");
                            button.image.sprite = ModFactory.CompareTexture("ori:item_tab").sprite;
                            button.button.OnPointerStayAction += () => ItemInfoShower.Show(itemGot);
                            button.button.OnPointerExitAction += _ => ItemInfoShower.Hide();
                            button.OnClickBind(() =>
                            {
                                craftingSelectedRecipe = pair;

                                //制作后刷新合成界面, 原料表与标题
                                player.OnInventoryItemChange(player.inventory, null);
                            });

                            //图标
                            var image = GameUI.AddImage(UPC.middle, $"ori:image.player_crafting_recipe_{cr.id}", "ori:item_tab", button);
                            image.image.sprite = itemGot.texture.sprite;
                            image.sd = craftingResultView.gridLayoutGroup.cellSize * 0.75f;

                            //文本
                            button.buttonText.rectTransform.SetAsLastSibling();
                            button.buttonText.rectTransform.AddLocalPosY(-20);
                            button.buttonText.SetSizeDelta(85, 27);
                            button.buttonText.text.SetFontSize(10);
                            button.buttonText.AfterRefreshing += t =>
                            {
                                t.text.text = $"{GameUI.CompareText(itemGot.id)?.text}[{cr.result.count}]";
                            };

                            craftingResultView.AddChild(button);
                        }

                        //去除掉不符合背包物品的合成表
                        if (craftingSelectedRecipe?.Key?.id != null)
                        {
                            var temp = (KeyValuePair<CraftingRecipe, List<Dictionary<int, ushort>>>)craftingSelectedRecipe;
                            KeyValuePair<CraftingRecipe, List<Dictionary<int, ushort>>>? targetRecipe = null;

                            //防止合成后原料不足
                            foreach (var result in craftingResults)
                            {
                                if (result.Key.id == temp.Key.id)
                                {
                                    targetRecipe = result;
                                    break;
                                }
                            }

                            craftingSelectedRecipe = targetRecipe;

                            //刷新合成界面
                            craftingStuffView.CustomMethod(null, null);
                            craftingSelectedItemTitleText.RefreshUI();
                        }
                    }
                };
                craftingResultView.gameObject.SetActive(false);

                player.backpackSidebarTable.Add("ori:craft", (() =>
                {
                    craftingSelectedItemTitleText.gameObject.SetActive(true);
                    craftingApplyButton.gameObject.SetActive(true);
                    craftingStuffView.gameObject.SetActive(true);
                    craftingResultView.gameObject.SetActive(true);
                }, () =>
                {
                    craftingSelectedItemTitleText.gameObject.SetActive(false);
                    craftingApplyButton.gameObject.SetActive(false);
                    craftingStuffView.gameObject.SetActive(false);
                    craftingResultView.gameObject.SetActive(false);
                }
                ));

                #endregion
            }
            #endregion

            #region 添加状态栏
            {
                Vector4 posC = UPC.upperRight;
                int xExtraOffset = -40;
                int yExtraOffset = -20;

                happinessBarBg = GameUI.AddImage(posC, "ori:image.happiness_bar_bg", "ori:happiness_bar");
                happinessBarFull = GameUI.AddImage(posC, "ori:image.happiness_bar_full", "ori:happiness_bar");
                SetIt(happinessBarBg, happinessBarFull, xExtraOffset, yExtraOffset * 3);

                thirstBarBg = GameUI.AddImage(posC, "ori:image.thirst_bar_bg", "ori:thirst_bar");
                thirstBarFull = GameUI.AddImage(posC, "ori:image.thirst_bar_full", "ori:thirst_bar");
                SetIt(thirstBarBg, thirstBarFull, xExtraOffset, yExtraOffset * 2);

                hungerBarBg = GameUI.AddImage(posC, "ori:image.hunger_bar_bg", "ori:hunger_bar");
                hungerBarFull = GameUI.AddImage(posC, "ori:image.hunger_bar_full", "ori:hunger_bar");
                SetIt(hungerBarBg, hungerBarFull, xExtraOffset, yExtraOffset);

                healthBarBg = GameUI.AddImage(posC, "ori:image.health_bar_bg", "ori:health_bar");
                healthBarFull = GameUI.AddImage(posC, "ori:image.health_bar_full", "ori:health_bar");
                SetIt(healthBarBg, healthBarFull, xExtraOffset, 0);

                static void SetIt(ImageIdentity bg, ImageIdentity full, float xOffset, float yOffset)
                {
                    Vector2 size = new(144, 15);
                    Image.Type imageType = Image.Type.Filled;
                    Image.FillMethod fillMethod = Image.FillMethod.Horizontal;
                    float bgColor = 0.5f;
                    float defaultX = -bg.sd.x / 2;
                    int defaultY = -30;

                    bg.rt.sizeDelta = size;
                    full.rt.sizeDelta = size;
                    full.rt.SetParentForUI(bg.image.rectTransform);
                    bg.rt.AddLocalPos(new(defaultX + xOffset, defaultY + yOffset));
                    bg.image.SetColorBrightness(bgColor);
                    full.image.type = imageType;
                    full.image.fillMethod = fillMethod;
                }
            }
            #endregion

            #region 昼夜条
            {
                dayNightBar = GameUI.AddImage(UPC.up, "ori:image.day_night_bar", "ori:day_night_bar");
                dayNightBar.SetSizeDelta(300, 50);
                dayNightBar.SetAPosY(-dayNightBar.sd.y / 2 - 50);

                dayNightBarPointer = GameUI.AddImage(UPC.upperLeft, "ori:image.day_night_bar_pointer", "ori:day_night_bar_pointer", dayNightBar);
                dayNightBarPointer.SetSizeDelta(50, 50);
                dayNightBarPointer.OnUpdate += i =>
                {
                    float progress = GTime.time24Format / 24;

                    i.SetAPosX(dayNightBar.rectTransform.sizeDelta.x * progress);
                };
            }
            #endregion

            #region 重生
            {
                rebornPanel = GameUI.AddPanel("ori:panel.reborn", GameUI.canvas.transform, true);
                rebornButton = GameUI.AddButton(UPC.middle, "ori:button.reborn", rebornPanel);
                rebornPanelText = GameUI.AddText(UPC.middle, "ori:text.reborn_info", rebornPanel);
                rebornTimerText = GameUI.AddText(UPC.middle, "ori:text.reborn_timer", rebornPanel);

                rebornPanelText.SetAPosY(100);
                rebornPanelText.SetSizeDelta(500, 120);
                rebornPanelText.text.SetFontSize(24);
                rebornPanelText.RefreshUI();
                rebornPanelText.doRefresh = false;

                rebornButton.SetAPosY(-20);
                rebornButton.buttonText.RefreshUI();
                rebornButton.buttonText.doRefresh = false;
                rebornButton.OnClickBind(() =>
                {
                    rebornButton.button.interactable = false;
                    GameUI.FadeOut(rebornPanel.panelImage);
                    GameUI.FadeIn(rebornButton.image);
                    GameUI.FadeIn(rebornButton.buttonText.text);

                    player.Reborn(player.maxHealth, null);
                });

                rebornPanel.OnUpdate += i =>
                {
                    if (Tools.time >= player.deathTimer)
                    {
                        rebornButton.button.interactable = true;
                        rebornTimerText.gameObject.SetActive(false);
                    }
                    else
                    {
                        rebornButton.button.interactable = false;
                        rebornTimerText.gameObject.SetActive(true);
                        rebornTimerText.text.text = (player.deathTimer - Tools.time).ToString();
                    }
                };
            }
            #endregion

            #region 状态文本
            {
                statusText = GameUI.AddText(UPC.down, "ori:text.player_status");
                statusText.SetAPosY(quickInventorySlots[0].button.ap.y / 2 + quickInventorySlots[0].button.sd.y / 2 + statusText.sd.y / 2 + 20);
                statusText.SetSizeDeltaY(40);
                statusText.text.SetFontSize(18);
                statusText.gameObject.SetActive(false);
            }
            #endregion
        }

        public enum SidebarType : byte
        {
            Left,
            Right,
        }

        public void GenerateSidebar(SidebarType type, string id, float cellSize, int sidebarSizeX, Vector2 spacing, string texture, string signTextureId, out ScrollViewIdentity itemView, out ImageIdentity signImageBackground, out ImageIdentity signImage)
        {
            //桶的物品视图
            itemView = GameUI.AddScrollView(UPC.middle, id, backpackMask);
            itemView.SetSizeDelta(sidebarSizeX, Player.backpackPanelHeight);
            itemView.viewportImage.sprite = ModFactory.CompareTexture(texture).sprite;

            //设置收缩
            int offset = 5;
            itemView.gridLayoutGroup.padding = new(offset, offset, offset, offset);
            cellSize -= offset;

            //根据 Type 设置位置
            switch (type)
            {
                case SidebarType.Left:
                    itemView.SetAPos(-inventoryItemView.sd.x / 2 - itemView.sd.x / 2, 0);
                    break;

                case SidebarType.Right:
                    itemView.SetAPos(inventoryItemView.sd.x / 2 + itemView.sd.x / 2, 0);
                    break;
            }

            itemView.gridLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            itemView.gridLayoutGroup.cellSize = new(cellSize, cellSize);
            itemView.gridLayoutGroup.spacing = spacing;
            itemView.scrollViewImage.color = Color.clear;
            itemView.viewportImage.color =backpackColor;
            itemView.content.sizeDelta = new(0, itemView.content.sizeDelta.y);
            itemView.content.anchoredPosition = new(-itemView.content.sizeDelta.x / 2, itemView.content.anchoredPosition.y);

            signImageBackground = GameUI.AddImage(UPC.middle, id + "_sign_background", "ori:crafting_result_sign", itemView);
            signImageBackground.sd = new(60, 60);
            signImageBackground.image.color = itemView.viewportImage.color;
            switch (type)
            {
                case SidebarType.Left:
                    signImageBackground.SetAPos(-itemView.sd.x / 2 - signImageBackground.sd.x / 2, itemView.sd.y / 2 - signImageBackground.sd.y / 2);
                    break;

                case SidebarType.Right:
                    signImageBackground.SetAPos(itemView.sd.x / 2 + signImageBackground.sd.x / 2, itemView.sd.y / 2 - signImageBackground.sd.y / 2);
                    break;
            }

            signImage = GameUI.AddImage(UPC.middle, id + "_sign", signTextureId, signImageBackground);
            signImage.sd = signImageBackground.sd;
            signImage.image.color = signImageBackground.image.color;
        }

        static void SetUIHighest(IRectTransform ui)
        {
            ui.rectTransform.SetAsLastSibling();

            if (!ui.rectTransform.gameObject.activeSelf)
                ui.rectTransform.gameObject.SetActive(true);
        }
        static void SetUIDisabled(IRectTransform ui)
        {
            if (ui.rectTransform.gameObject.activeSelf)
                ui.rectTransform.gameObject.SetActive(false);
        }

        public void Update()
        {
            /* --------------------------------- 刷新快捷物品栏 -------------------------------- */
            //缓存物品栏以保证性能
            var inventoryTemp = player.inventory;

            for (int i = 0; i < quickInventorySlots.Length; i++)
            {
                var slot = quickInventorySlots.ElementAt(i);

                if (slot == null)
                    return;

                var item = inventoryTemp?.TryGetItem(i);

                //设置物品图标
                slot.content.image.sprite = item?.data?.texture?.sprite;
                slot.content.image.color = new(slot.content.image.color.r, slot.content.image.color.g, slot.content.image.color.b, !slot.content.image.sprite ? 0 : 1);

                //设置文本
                slot.button.buttonText.text.text = item?.count.ToString();

                //设置栏位图标
                if (player.usingItemIndex == i)
                    slot.button.image.sprite = ModFactory.CompareTexture("ori:using_item_tab")?.sprite;
                else
                    slot.button.image.sprite = ModFactory.CompareTexture("ori:item_tab")?.sprite;
            }

            /* ----------------------------------- 检测按键 ----------------------------------- */
            switch (GControls.mode)
            {
                case ControlMode.KeyboardAndMouse:
                    if (Keyboard.current != null)
                    {
                        if (Keyboard.current.escapeKey.wasReleasedThisFrame)
                            PauseGame();

                        if (Keyboard.current.enterKey.wasReleasedThisFrame)
                            Chat();

                        if (Keyboard.current.tKey.wasReleasedThisFrame)
                            ShowHideTaskView();
                    }

                    break;

                case ControlMode.Gamepad:
                    if (Gamepad.current != null)
                    {
                        if (Gamepad.current.startButton.wasReleasedThisFrame)
                            PauseGame();

                        if (Gamepad.current.dpad.down.wasReleasedThisFrame)
                            Chat();

                        if (Gamepad.current.dpad.up.wasReleasedThisFrame)
                            ShowHideTaskView();
                    }

                    break;
            }

            #region 手机操控

            if (GControls.mode == ControlMode.Touchscreen)
            {
                SetUIHighest(moveJoystick);
                SetUIHighest(cursorJoystick);
                SetUIHighest(cursorImage);
                SetUIHighest(attackButton);
                SetUIHighest(interactionButton);
                SetUIHighest(useItemButton);
                SetUIHighest(craftingButton);

                useItemButtonImage.image.sprite = player.TryGetUsingItem()?.data?.texture?.sprite;
                useItemButtonImage.image.color = useItemButtonImage.image.sprite ? Color.white : Color.clear;

                if (Player.PlayerCanControl(player) && cursorImage)
                {
                    cursorImage.rt.anchoredPosition = new(
                        cursorImage.rt.anchoredPosition.x + cursorJoystick.Horizontal * GFiles.settings.playerCursorSpeed * Performance.frameTime,
                        cursorImage.rt.anchoredPosition.y + cursorJoystick.Vertical * GFiles.settings.playerCursorSpeed * Performance.frameTime
                    );
                }

                float maxX = player.transform.position.x + 10;
                float maxY = player.transform.position.y + 10;
                float minX = player.transform.position.x - 10;
                float minY = player.transform.position.y - 10;
                float max = (Mathf.Min(player.transform.position.x, player.transform.position.y) - Mathf.Min(GameUI.canvasScaler.referenceResolution.x, GameUI.canvasScaler.referenceResolution.y)) / 3;
                float min = -max;

                /* --------------------------------- 限制在范围内 --------------------------------- */
                if (cursorImage.transform.position.x > maxX)
                    cursorImage.transform.position = new(maxX, cursorImage.transform.position.y);
                else if (cursorImage.transform.position.x < minX)
                    cursorImage.transform.position = new(minX, cursorImage.transform.position.y);

                if (cursorImage.transform.position.y > maxY)
                    cursorImage.transform.position = new(cursorImage.transform.position.x, maxY);
                else if (cursorImage.transform.position.y < minY)
                    cursorImage.transform.position = new(cursorImage.transform.position.x, minY);
            }
            else
            {
                SetUIDisabled(moveJoystick);
                SetUIDisabled(cursorJoystick);
                SetUIDisabled(cursorImage);
                SetUIDisabled(attackButton);
                SetUIDisabled(interactionButton);
                SetUIDisabled(useItemButton);
                SetUIDisabled(craftingButton);
            }

            #endregion
        }

        public void ShowHideTaskView()
        {
            if (GameUI.page.ui == taskView)
            {
                GameUI.SetPageBack();
            }
            else if (GameUI.page == null || !GameUI.page.ui)
            {
                GameUI.SetPage(taskView);
            }
        }

        public static Action<PlayerUI> BindTasks = ui =>
        {
            ui.AddTask("ori:get_dirt", "ori:task.get_dirt", null, new[] { $"{BlockID.Dirt}/=/25/=/null" });

            ui.AddTask("ori:craft", "ori:task.craft", "ori:get_dirt", null);

            ui.AddTask("ori:get_log", "ori:task.get_log", "ori:get_dirt", new[] { $"{BlockID.OakLog}/=/10/=/null" });

            ui.AddTask("ori:get_meat", "ori:task.get_meat", "ori:get_dirt", null);

            ui.AddTask("ori:get_feather", "ori:task.get_feather", "ori:get_dirt", new[] { $"{ItemID.ChickenFeather}/=/5/=/null" });
            ui.AddTask("ori:get_feather_wing", "ori:task.get_feather_wing", "ori:get_feather", null);

            ui.AddTask("ori:get_grass", "ori:task.get_grass", "ori:get_dirt", null);
            ui.AddTask("ori:get_straw_rope", "ori:task.get_straw_rope", "ori:get_grass", new[] { $"{ItemID.StrawRope}/=/3/=/null" });
            ui.AddTask("ori:get_plant_fiber", "ori:task.get_plant_fiber", "ori:get_straw_rope", null);

            ui.AddTask("ori:get_gravel", "ori:task.get_gravel", "ori:get_dirt", new[] { $"{BlockID.Gravel}/=/3/=/null" });
            ui.AddTask("ori:get_flint", "ori:task.get_flint", "ori:get_gravel", new[] { $"{ItemID.Flint}/=/2/=/null" });
            ui.AddTask("ori:get_stone", "ori:task.get_stone", "ori:get_flint", new[] { $"{BlockID.Stone}/=/10/=/null" });

            ui.AddTask("ori:get_planks", "ori:task.get_planks", "ori:get_log", new[] { $"{BlockID.OakPlanks}/=/10/=/null" });
            ui.AddTask("ori:get_stick", "ori:task.get_stick", "ori:get_planks", new[] { $"{ItemID.Stick}/=/10/=/null" });
            ui.AddTask("ori:get_campfire", "ori:task.get_campfire", "ori:get_stick", null);

            ui.AddTask("ori:get_flint_knife", "ori:task.get_flint_knife", "ori:get_stick", null);
            ui.AddTask("ori:get_flint_hoe", "ori:task.get_flint_hoe", "ori:get_stick", null);
            ui.AddTask("ori:get_flint_sword", "ori:task.get_flint_sword", "ori:get_stick", null);
            ui.AddTask("ori:get_iron_knife", "ori:task.get_iron_knife", "ori:get_flint_knife", null);
            ui.AddTask("ori:get_iron_hoe", "ori:task.get_iron_hoe", "ori:get_flint_hoe", null);
            ui.AddTask("ori:get_iron_sword", "ori:task.get_iron_sword", "ori:get_flint_sword", null);

            ui.AddTask("ori:get_bark", "ori:task.get_bark", "ori:get_log", new[] { $"{ItemID.Bark}/=/1/=/null" });
            ui.AddTask("ori:get_bark_vest", "ori:task.get_bark_vest", "ori:get_bark", null);
        };

        public void RefreshTasks(bool init)
        {
            if (init)
                taskView.Clear();

            RefreshTasks_Internal(taskNode, null, new(), init);
        }

        private void RefreshTasks_Internal(TaskNode current, TaskNode parentNode, List<TaskNode> siblingNodes, bool init)
        {
            /* ----------------------------------- 初始化 ---------------------------------- */
            if (init)
                RefreshTasks_Internal_Init(current, parentNode, siblingNodes);

            /* ---------------------------------- 设置图标 ---------------------------------- */
            current.icon.SetID($"ori:image.task_node.{current.data.id}");
            current.icon.image.sprite = ModFactory.CompareTexture(current.data.icon).sprite;

            /* ---------------------------------- 设置颜色 ---------------------------------- */
            current.icon.image.color = current.button.image.color = current.completed ? (current.hasGotRewards ? Color.white : Tools.HexToColor("#00FFD6")) : new(0.5f, 0.5f, 0.5f, 0.75f);
            if (current.line) current.line.image.color = current.icon.image.color;

            /* ---------------------------------- 添加到节点组 & 初始化子节点 --------------------------------- */
            siblingNodes.Add(current);

            List<TaskNode> childrenNodes = new();
            foreach (var node in current.nodes)
            {
                RefreshTasks_Internal(node, current, childrenNodes, init);
            }
        }

        private void RefreshTasks_Internal_Init(TaskNode node, TaskNode parentNode, List<TaskNode> siblingNodes)
        {
            /* ---------------------------------- 初始化按钮 --------------------------------- */
            int space = 90;
            node.button = GameUI.AddButton(UPC.middle, $"ori:button.task_node.{node.data.id}", GameUI.canvas.transform, "ori:square_button");
            node.parent = parentNode;
            node.button.SetSizeDelta(space, space);   //设置按钮大小
            node.button.buttonText.RefreshUI();

            /* ---------------------------------- 绑定按钮 ---------------------------------- */
            node.button.button.OnPointerStayAction = () => TaskInfoShower.Show(node);
            node.button.button.OnPointerExitAction = _ => TaskInfoShower.Hide();
            node.button.OnClickBind(() =>
            {
                if (!node.completed || node.hasGotRewards)
                    return;

                foreach (var reward in node.data.rewards)
                {
                    /* ---------------------------------- 切割字符串 --------------------------------- */
                    if (Drop.ConvertStringItem(reward, out string id, out ushort count, out _, out string error))
                    {
                        /* ---------------------------------- 给予物品 ---------------------------------- */
                        ItemData item = ModFactory.CompareItem(id);

                        if (item == null)
                            continue;

                        var extended = item.DataToItem();
                        extended.count = count;
                        player.ServerAddItem(extended);
                    }
                    else
                    {
                        Debug.LogError(error);
                    }
                }

                var completedTasksTemp = player.completedTasks;
                foreach (var completed in completedTasksTemp)
                {
                    if (completed.id == node.data.id)
                    {
                        completed.hasGotRewards = true;
                    }
                }
                player.completedTasks = completedTasksTemp;

                node.hasGotRewards = true;
                RefreshTasks(false);
            });

            /* ---------------------------------- 设置父物体 --------------------------------- */
            if (parentNode == null)
                taskView.AddChild(node.button);
            else
                node.button.SetParentForUI(parentNode.button);

            /* -------------------------------- 根据父节点更改位置 ------------------------------- */
            Vector2 tempVec = Vector2.zero;
            if (parentNode != null) { tempVec.y -= node.button.sd.y + space; }

            /* ------------------------------- 根据同级节点更改位置 ------------------------------- */
            foreach (var siblingNode in siblingNodes)
            {
                //更改节点位置
                Vector2 tempVecFE = siblingNode.button.ap;
                tempVecFE.x -= siblingNode.button.sd.x / 2 + node.button.sd.x / 2 + space;
                siblingNode.button.ap = tempVecFE;

                //重新计算节点
                InitLine(siblingNode);

                //更改本身
                tempVec.x += siblingNode.button.sd.x / 2 + node.button.sd.x / 2 + space;
            }

            /* -------------------------------- 设置按钮和文本位置 ------------------------------- */
            node.button.ap = tempVec;
            node.button.buttonText.AddAPosY(-node.button.sd.y / 2 - node.button.buttonText.sd.y / 2 - 5);

            /* ---------------------------------- 设置图标 ---------------------------------- */
            node.icon = GameUI.AddImage(UPC.middle, $"ori:image.task_node.{node.data.id}", null, node.button);
            node.icon.sd = node.button.sd;

            /* --------------------------------- 初始化连接线 --------------------------------- */
            InitLine(node);
        }

        private static void InitLine(TaskNode node)
        {
            if (node.parent == null)
                return;

            if (!node.line)
                node.line = GameUI.AddImage(UPC.middle, $"ori:button.task_node.{node.data.id}.line", null, node.button);

            /* --------------------------------- 计算对应顶点 --------------------------------- */
            Vector2 buttonPoint = new(node.button.ap.x, node.button.ap.y + node.button.sd.y / 2);   //本身按钮上方
            Vector2 parentPoint = new(0, -node.button.sd.y / 2);   //父节点按钮下方

            /* ---------------------------------- 设置大小 ---------------------------------- */
            node.line.sd = new(Vector2.Distance(buttonPoint, parentPoint), 2);   //x轴为长度, y轴为宽度

            /* ---------------------------------- 设置旋转角 --------------------------------- */
            node.line.rt.localEulerAngles = new(0, 0, Tools.GetAngleFloat(buttonPoint, parentPoint) - 90);   //获取角度并旋转-90度 (我也不知道为啥)
            if (node.button.ap.x < 0) node.line.rt.localEulerAngles = new(0, 180, node.line.rt.localEulerAngles.z);   //如果按钮在父节点左侧就水平翻转

            /* ---------------------------------- 设置位置 ---------------------------------- */
            Vector2 temp = Vector2.zero;
            temp.x += 0.5f * (parentPoint.x - buttonPoint.x);   //使得线贴在按钮中间
            temp.y += node.button.sd.y;   //使得线贴在按钮上方
            node.line.ap = temp;
        }

        public bool playingTaskCompletion { get; private set; }

        IEnumerator TaskCompleteTimer()
        {
            yield return new WaitForSeconds(7.5f);

            if (taskCompleteBackground)
                GameUI.Disappear(taskCompleteBackground);

            playingTaskCompletion = false;
        }

        public void CompleteTask(string id, bool feedback = true, bool hasGotRewards = false)
        {
            if (CompleteTask_Internal(taskNode, id, out bool hasCompletedBefore, out TaskNode nodeCompleted) && !hasCompletedBefore && nodeCompleted != null)
            {
                if (hasGotRewards)
                {
                    nodeCompleted.hasGotRewards = true;
                }

                if (feedback)
                {
                    MethodAgent.CallUntil(() => !playingTaskCompletion, () =>
                    {
                        playingTaskCompletion = true;
                        GameUI.Appear(taskCompleteBackground);

                        taskCompleteIcon.image.sprite = nodeCompleted.icon.image.sprite;
                        taskCompleteText.text.text = nodeCompleted.button.buttonText.text.text;

                        GAudio.Play(AudioID.Complete);
                        CoroutineStarter.Do(TaskCompleteTimer());
                    });
                }
            }

            RefreshTasks(false);
        }

        private bool CompleteTask_Internal(TaskNode current, string id, out bool hasCompletedBefore, out TaskNode nodeCompleted)
        {
            //从父节点开始尝试完成
            if (MakeTheSpecificNodeCompleted(current, out hasCompletedBefore, out nodeCompleted))
            {
                return true;
            }

            //不是父节点开始尝试完成子节点
            foreach (var node in current.nodes)
            {
                if (CompleteTask_Internal(node, id, out hasCompletedBefore, out nodeCompleted))
                {
                    return true;
                }
            }

            return false;



            bool MakeTheSpecificNodeCompleted(TaskNode node, out bool hasCompletedBefore, out TaskNode nodeCompleted)
            {
                if (node.data.id == id)
                {
                    nodeCompleted = node;
                    hasCompletedBefore = node.completed;

                    if (!player.completedTasks.Any(p => p.id == id))
                    {
                        player.AddCompletedTasks(new() { id = id, completed = true, hasGotRewards = node.hasGotRewards });
                    };

                    node.completed = true;
                    return true;
                }



                nodeCompleted = null;
                hasCompletedBefore = false;

                return false;
            }
        }





        [Serializable]
        public class TaskStatus
        {
            public bool completed;
            public bool hasGotRewards;
        }

        [Serializable]
        public class TaskStatusForSave : TaskStatus
        {
            public string id;
        }

        public class TaskData
        {
            public string id;
            public string parent;
            public string icon;
            public string[] rewards;

            public TaskData(string id, string icon, string parent, string[] rewards)
            {
                this.id = id;
                this.icon = icon;
                this.parent = parent;
                this.rewards = rewards;
            }
        }

        public class TaskNode : TaskStatus
        {
            public ButtonIdentity button;
            public ImageIdentity icon;
            public TaskData data;
            public TaskNode parent;
            public ImageIdentity line;
            public List<TaskNode> nodes = new();

            public TaskNode(TaskData data)
            {
                this.data = data;

                if (data.rewards == null || data.rewards.Length == 0)
                    hasGotRewards = true;
            }
        }

        public class TaskInfoShower
        {
            public class TaskInfoUI
            {
                public ImageIdentity image;
                public TextIdentity nameText;
                public TextIdentity detailText;

                public TaskInfoUI(ImageIdentity image, TextIdentity nameText, TextIdentity detailText)
                {
                    this.image = image;
                    this.nameText = nameText;
                    this.detailText = detailText;
                }
            }

            private static TaskInfoUI uiInstance;

            public static TaskInfoUI GetUI()
            {
                if (uiInstance == null || !uiInstance.image || !uiInstance.nameText || !uiInstance.detailText)
                {
                    ImageIdentity image = GameUI.AddImage(UPC.middle, "ori:image.task_info_shower", "ori:item_info_shower");
                    TextIdentity nameText = GameUI.AddText(UPC.upperLeft, "ori:text.task_info_shower.name", image);
                    TextIdentity detailText = GameUI.AddText(UPC.upperLeft, "ori:text.task_info_shower.detail", image);

                    nameText.text.alignment = TMPro.TextAlignmentOptions.Left;
                    detailText.text.alignment = TMPro.TextAlignmentOptions.TopLeft;

                    image.SetSizeDelta(200, 200);
                    nameText.SetSizeDelta(image.sd.x, 30);
                    detailText.SetSizeDelta(nameText.sd.x, image.sd.y - nameText.sd.y);

                    nameText.SetAPos(nameText.sd.x / 2, -nameText.sd.y / 2 - 5);
                    detailText.SetAPos(nameText.ap.x, nameText.ap.y - nameText.sd.y / 2 - detailText.sd.y / 2 - 5);

                    nameText.text.SetFontSize(18);
                    detailText.text.SetFontSize(13);

                    image.image.raycastTarget = false;
                    nameText.text.raycastTarget = false;
                    detailText.text.raycastTarget = false;

                    uiInstance = new(image, nameText, detailText);
                }

                return uiInstance;
            }

            public static void Show(TaskNode task)
            {
                TaskInfoUI ui = GetUI();
                ui.image.transform.SetParent(task.button.transform);
                Vector2 pos = Vector2.zero;
                pos.x += ui.image.sd.x;
                pos.y -= ui.image.sd.y;

                ui.image.ap = pos;
                ui.nameText.text.text = task.button.buttonText.text.text;
                ui.detailText.text.text = GetText(task).ToString();

                ui.image.gameObject.SetActive(true);
            }

            public static StringBuilder GetText(TaskNode task)
            {
                StringBuilder sb = new();

                if (task.data.rewards != null)
                {
                    foreach (var reward in task.data.rewards)
                    {
                        if (string.IsNullOrWhiteSpace(reward))
                            continue;

                        if (Drop.ConvertStringItem(reward, out string id, out ushort count, out _, out _))
                        {
                            sb.AppendLine(GameUI.CompareText("ori:task.rewards").text.Replace("{id}", GameUI.CompareText(id).text).Replace("{count}", count.ToString()));
                        }
                    }
                }

                return sb;
            }

            public static void Hide()
            {
                TaskInfoUI ui = GetUI();
                ui.image.gameObject.SetActive(false);
            }
        }

        [RuntimeInitializeOnLoadMethod]
        public static void BindMethods()
        {
            NetworkCallbacks.OnTimeToServerCallback += () =>
            {
                Server.Callback<NMChat>((_, nm) => Server.Send(nm));
            };
            NetworkCallbacks.OnTimeToClientCallback += () =>
            {
                Client.Callback<NMChat>(nm =>
                {
                    //添加消息
                    AddChatMsg(ByteConverter.ToSprite(nm.portrait), nm.playerName, nm.msg);

                    //播放音效
                    GAudio.Play(AudioID.Chat);
                });
            };
        }
    }





    /* -------------------------------------------------------------------------- */
    /*                                     公共类                                    */
    /* -------------------------------------------------------------------------- */
    public class InventorySlotUI
    {
        public ButtonIdentity button;
        public ImageIdentity content;

        public static InventorySlotUI Generate(string buttonId, string imageId, Vector2 sizeDelta)
        {
            ButtonIdentity button = GameUI.AddButton(UPC.middle, buttonId);
            ImageIdentity image = GameUI.AddImage(UPC.middle, imageId, null, button);

            button.image.sprite = ModFactory.CompareTexture("ori:item_tab").sprite;
            image.image.sprite = null;
            image.image.gameObject.SetActive(false);

            button.sd = sizeDelta;
            image.sd = sizeDelta * 0.6f;

            image.ap = new(0, 3);

            button.buttonText.text.raycastTarget = false;
            image.image.raycastTarget = false;

            button.buttonText.SetSizeDeltaX(100);
            button.buttonText.text.SetFontSize(13);
            button.buttonText.SetAPosOnBySizeDown(button, -27.5f);
            button.buttonText.doRefresh = false;
            button.buttonText.text.text = string.Empty;

            return new(button, image);
        }

        public void Refresh(IItemContainer container, string itemIndex, Func<Item, bool> replacementCondition = null)
        {
            Item item = container.GetItem(itemIndex);
            replacementCondition ??= (_) => true;

            button.button.onClick.RemoveAllListeners();

            /* --------------------------------- 如果物品为空 --------------------------------- */
            if (Item.Null(item))
            {
                //设置 UI
                button.buttonText.text.text = string.Empty;
                content.image.sprite = null;
                content.image.gameObject.SetActive(false);

                //加这一行是因为物品不为空时会绑定显示，我们要在这里取消它
                button.button.OnPointerStayAction = () => ItemInfoShower.Hide();

                //当栏位被点击时
                button.OnClickBind(() =>
                {
                    ItemDragger.DragItem(
                        item,
                        Vector2.zero,
                        value =>
                        {
                            container.SetItem(itemIndex, value);
                        },
                        () =>
                        {
                            content.image.color = Color.white;
                        },
                        replacementCondition
                    );
                });
            }
            /* --------------------------------- 如果物品不为空 -------------------------------- */
            else
            {
                //设置 UI
                button.buttonText.text.text = item.count.ToString();
                content.image.sprite = item.data.texture.sprite; ////->?? GInit.instance.spriteUnknown;
                content.image.gameObject.SetActive(true);

                //当指针悬停时显示物品信息
                button.button.OnPointerStayAction = () =>
                {
                    var ui = ItemInfoShower.Show(item);

                    if (container is Player player)
                    {
                        player.inventory.TryGetItemBehaviour(itemIndex)?.ModifyInfo(ui);
                    }
                };
                button.button.OnPointerExitAction = _ => ItemInfoShower.Hide();

                //当栏位被点击时
                button.OnClickBind(() =>
                {
                    content.image.color = new(1, 1, 1, 0.5f);

                    ItemDragger.DragItem(
                        item,
                        content.sd,
                        value =>
                        {
                            container.SetItem(itemIndex, value);
                        },
                        () =>
                        {
                            content.image.color = Color.white;
                        },
                        replacementCondition
                    );
                });
            }
        }

        public InventorySlotUI(ButtonIdentity button, ImageIdentity content)
        {
            this.button = button;
            this.content = content;
        }
    }

    //TODO
    public static class ItemInfoShower
    {
        private static ItemInfoUI uiInstance;

        public static ItemInfoUI GetUI()
        {
            if (uiInstance == null || !uiInstance.image || !uiInstance.nameText || !uiInstance.detailText)
            {
                int borderSize = 5;
                int detailTextFontSize = 15;

                ImageIdentity backgroundImage = GameUI.AddImage(UPC.middle, "ori:image.item_info_shower", "ori:item_info_shower");
                TextIdentity nameText = GameUI.AddText(UPC.upperLeft, "ori:text.item_info_shower.name", backgroundImage);
                //ImageIdentity damageIcon = GameUI.AddImage(UPC.upperLeft, "ori:image.item_info_shower.damage_icon", "ori:item_info_shower_damage", backgroundImage);
                TextIdentity detailText = GameUI.AddText(UPC.upperLeft, "ori:text.item_info_shower.detail", backgroundImage);

                nameText.text.alignment = TMPro.TextAlignmentOptions.Left;
                detailText.text.alignment = TMPro.TextAlignmentOptions.TopLeft;
                detailText.text.paragraphSpacing = 15;

                backgroundImage.SetSizeDelta(200, 200);
                nameText.SetSizeDelta(backgroundImage.sd.x, 30);
                //damageIcon.SetSizeDelta(detailTextFontSize, detailTextFontSize);
                detailText.SetSizeDelta(nameText.sd.x, backgroundImage.sd.y - nameText.sd.y);

                nameText.SetAPos(nameText.sd.x / 2, -nameText.sd.y / 2 - borderSize);
                detailText.SetAPos(nameText.ap.x, nameText.ap.y - nameText.sd.y / 2 - detailText.sd.y / 2 - borderSize);
                //damageIcon.SetAPos(borderSize + damageIcon.sd.x / 2, nameText.ap.y - nameText.sd.y / 2 - damageIcon.sd.y - borderSize);

                nameText.text.SetFontSize(18);
                detailText.text.SetFontSize(detailTextFontSize);

                backgroundImage.image.raycastTarget = false;
                nameText.text.raycastTarget = false;
                detailText.text.raycastTarget = false;

                uiInstance = new(backgroundImage, nameText, detailText);
            }

            return uiInstance;
        }

        public static ItemInfoUI Show(Item item)
        {
            ItemInfoUI ui = GetUI();
            Vector2 pos = GControls.cursorPosInMainCanvas;
            pos.x += ui.image.sd.x;
            pos.y -= ui.image.sd.y;

            ui.image.ap = pos;
            ui.nameText.text.text = GameUI.CompareText(item.data.id).text;   //$"{GameUI.CompareText(item.basic.id).text} <size=60%>({item.basic.id})";
            ui.detailText.text.text = $"<color=#E0E0E0>{GetDetailText(item)}</color>";

            ui.image.gameObject.SetActive(true);
            return ui;
        }

        public static ItemInfoUI Show(ItemData item)
        {
            ItemInfoUI ui = GetUI();
            Vector2 pos = GControls.cursorPosInMainCanvas;
            pos.x += ui.image.sd.x;
            pos.y -= ui.image.sd.y;

            ui.image.ap = pos;
            ui.nameText.text.text = GameUI.CompareText(item.id).text;
            ui.detailText.text.text = $"<color=#E0E0E0>{GetDetailText(item)}</color>";

            ui.image.gameObject.SetActive(true);
            return ui;
        }

        public static StringBuilder GetDetailText(Item item)
        {
            StringBuilder sb = GetDetailTextWithoutDescription(item.data);

            sb.AppendLine(string.Empty);
            sb.AppendLine(GameUI.CompareText(item.data.description).text);

            return sb;
        }

        public static StringBuilder GetDetailText(ItemData item)
        {
            StringBuilder sb = GetDetailTextWithoutDescription(item);

            sb.AppendLine(string.Empty);
            sb.Append(GameUI.CompareText(item.description).text);

            return sb;
        }

        public static StringBuilder GetDetailTextWithoutDescription(ItemData item)
        {
            //TODO: Pool-ify
            StringBuilder sb = new();

            sb.AppendLine(GameUI.CompareText("ori:item.damage").text.Replace("{value}", item.damage.ToString()));
            sb.AppendLine(GameUI.CompareText("ori:item.excavation_strength").text.Replace("{value}", item.excavationStrength.ToString()));
            sb.AppendLine(GameUI.CompareText("ori:item.use_cd").text.Replace("{value}", item.useCD.ToString()));

            return sb;
        }

        public static void Hide()
        {
            ItemInfoUI ui = GetUI();
            ui.image.gameObject.SetActive(false);
        }
    }

    public class ItemInfoUI
    {
        public ImageIdentity image;
        public TextIdentity nameText;
        public TextIdentity detailText;

        public ItemInfoUI(ImageIdentity image, TextIdentity nameText, TextIdentity detailText)
        {
            this.image = image;
            this.nameText = nameText;
            this.detailText = detailText;
        }
    }

    public static class ItemDragger
    {
        public class ItemDraggerUI
        {
            public ImageIdentity image;

            public ItemDraggerUI(ImageIdentity image)
            {
                this.image = image;
            }
        }

        public class ItemDraggerItem
        {
            public Item item;
            public Action<Item> placement;
            public Action cancel;
            public Func<Item, bool> replacementCondition;

            public ItemDraggerItem(Item item, Action<Item> placement, Action cancel, Func<Item, bool> replacementCondition)
            {
                this.item = item;
                this.placement = placement;
                this.cancel = cancel;
                this.replacementCondition = replacementCondition;
            }
        }

        public static ItemDraggerItem draggingItem;

        private static ItemDraggerUI uiInstance;

        public static ItemDraggerUI GetUI()
        {
            if (uiInstance == null || !uiInstance.image)
            {
                ImageIdentity image = GameUI.AddImage(UPC.middle, "ori:image.item_dragger", "ori:square_button_flat");
                image.OnUpdate += i =>
                {
                    i.ap = GControls.cursorPosInMainCanvas;
                };

                image.image.raycastTarget = false;

                uiInstance = new(image);
            }

            return uiInstance;
        }




        public static void DragItem(Item item, Vector2 iconSize, Action<Item> placement, Action onCancel, Func<Item, bool> replacementCondition)
        {
            ItemDraggerUI ui = GetUI();

            /* ------------------------------- 先去掉原本在拖拽的物品 ------------------------------- */
            if (draggingItem != null)
            {
                //如果正在拖拽，又点了一次本来就在拖拽的物品，就取消拖拽
                if (draggingItem.item == item)
                {
                    CancelDragging();
                    return;
                }
                //如果正在拖拽，又点了一次其他的物品，就交换物品位置
                else
                {
                    SwapDraggingAndOldDragger(item, placement, onCancel, replacementCondition);
                    return;
                }
            }

            /* ------------------------------- 如果物品不为空就拖拽 ------------------------------- */
            if (!Item.Null(item))
            {
                //设置 UI
                ui.image.image.sprite = item.data.texture.sprite;
                ui.image.sd = iconSize;
                ui.image.gameObject.SetActive(true);

                draggingItem = new(item, placement, onCancel, replacementCondition);
            }
            /* ------------------------------- 如果物品为空就不拖拽 ------------------------------- */
            else
            {
                CancelDragging();
            }
        }

        public static void CancelDragging()
        {
            ItemDraggerUI ui = GetUI();
            ui.image.gameObject.SetActive(false);

            if (draggingItem != null)
            {
                draggingItem.cancel();

                draggingItem = null;
            }
        }

        public static void SwapDraggingAndOldDragger(Item item, Action<Item> placement, Action cancel, Func<Item, bool> replacementCondition)
        {
            var oldDragger = draggingItem;
            var draggingTemp = oldDragger.item;
            var oldTemp = item;

            /* ------------------------------- 如果物品不同直接交换 ------------------------------- */
            if (Item.Null(draggingTemp) || Item.Null(oldTemp) || !Item.Same(draggingTemp, oldTemp))
            {
                if (replacementCondition(draggingTemp) && oldDragger.replacementCondition(oldTemp))
                {
                    oldDragger.placement(oldTemp);
                    placement(draggingTemp);
                }

                CancelDragging();
            }
            /* ------------------------------- 如果物品相同且数量未满 ------------------------------- */
            else if (oldTemp.count < oldTemp.data.maxCount)
            {
                //如果可以数量直接添加
                if (draggingTemp.count + oldTemp.count <= oldTemp.data.maxCount)
                {
                    oldTemp.count += draggingTemp.count;

                    placement(oldTemp);
                    oldDragger.placement(null);

                    CancelDragging();
                }
                else
                {
                    //TODO:FIx
                    //如果数量过多先添加
                    ushort countToExe = (ushort)Mathf.Min(draggingTemp.count, oldTemp.data.maxCount - draggingTemp.count);

                    draggingTemp.count -= countToExe;
                    oldTemp.count += countToExe;

                    placement(draggingTemp);
                    oldDragger.placement(oldTemp);
                }
            }
        }
    }
}