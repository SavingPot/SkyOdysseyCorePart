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
using System.Security.Cryptography.X509Certificates;

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
        /*                                     聊天                                     */
        /* -------------------------------------------------------------------------- */
        public ScrollViewIdentity chatView;
        public InputButtonIdentity chatInput;



        public static void AddChatMsg(Sprite portrait, string playerName, string msg)
        {
            if (instance == null || !instance.chatView)
                return;

            /* ------------------------ 添加文本, 设置向左对其, 不能溢出, 设置内容 ------------------------ */
            var text = GameUI.AddText(UPC.Middle, $"ori:text.chat.{msg}");
            text.autoCompareText = false;
            text.text.alignment = TMPro.TextAlignmentOptions.Left;
            text.text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            text.text.text = $"{playerName}: {msg}";

            var image = GameUI.AddImage(UPC.Left, $"ori:image.chat.{msg}", null, text);
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



        #region 背包界面

        public List<BackpackPanel> backpackPanels = new();
        public string currentBackpackPanel = string.Empty;
        public PanelIdentity backpackMask;
        public ImageIdentity backpackPanelBackground;
        public Color backpackColor = Color.white;


        /* -------------------------------------------------------------------------- */
        /*                                     物品栏                                    */
        /* -------------------------------------------------------------------------- */
        public InventorySlotUI[] inventorySlotsUIs = new InventorySlotUI[Player.inventorySlotCountConst];
        public InventorySlotUI inventoryHelmetUI;
        public InventorySlotUI inventoryBreastplateUI;
        public InventorySlotUI inventoryLeggingUI;
        public InventorySlotUI inventoryBootsUI;
        public readonly InventorySlotUI[] quickInventorySlots;
        public BackpackPanel inventoryItemPanel;
        public ScrollViewIdentity inventoryItemView;


        /* -------------------------------------------------------------------------- */
        /*                                     合成                                     */
        /* -------------------------------------------------------------------------- */
        public CraftingInfoShower craftingInfoShower;
        public BackpackPanel craftingPanel;
        public ScrollViewIdentity craftingView;



        /* -------------------------------------------------------------------------- */
        /*                                     暂停                                     */
        /* -------------------------------------------------------------------------- */
        public BackpackPanel pausePanel;


        #endregion



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
        public BackpackPanel taskPanel;
        public ScrollViewIdentity taskView;
        public ImageIdentity taskCompleteBackground;
        public ImageIdentity taskCompleteIcon;
        public TextIdentity taskCompleteText;

        public List<TaskData> tasks = new();
        public TaskNode rootTaskNode;

        public void AddTask(string id, string icon, string parent, string[] rewards)
        {
            tasks.Add(new(id, icon, parent, rewards));
        }



        /* -------------------------------------------------------------------------- */
        /*                                    手机端操纵                                   */
        /* -------------------------------------------------------------------------- */
        public Joystick touchScreenMoveJoystick;
        public Joystick touchScreenCursorJoystick;
        public ImageIdentity touchScreenCursorImage;
        public ButtonIdentity touchScreenAttackButton;
        public ImageIdentity touchScreenUseItemButtonImage;
        public ButtonIdentity touchScreenUseItemButton;
        public ButtonIdentity touchScreenPlaceBlockUnderPlayerButton;
        public ButtonIdentity touchScreenPauseButton;
        public ButtonIdentity touchScreenCraftingButton;
        public ButtonIdentity touchScreenShowTaskButton;





        /* -------------------------------------------------------------------------- */
        /*                                     对话                                     */
        /* -------------------------------------------------------------------------- */
        public ButtonIdentity dialogPanel;
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
            "<color=",
            "</"
        };

        public async Task DisplayDialogTask()
        {
            GameUI.SetPage(dialogPanel, GameUI.DisappearType.PositionUpToDown, GameUI.AppearType.PositionDownToUp);

            //遍历对话列表
            for (int i = 0; i < displayingDialog.dialogs.Count; i++)
            {
                var current = displayingDialog.dialogs[i];

                //清空对话文本
                dialogText.text.text = string.Empty;

                //获取当前对话文本
                string fullContent = current.text;
                char[] fullContentChars = fullContent.ToCharArray();

                //更改对话者的头像和名字
                dialogHead.image.sprite = ModFactory.CompareTexture(current.head).sprite;
                dialogNameText.text.text = GameUI.CompareText(displayingDialog.name).text;

                //遍历文本
                for (int t = 0; t < fullContent.Length;)
                {
                    var item = fullContent[t];
                    var charsAfterItem = new ArraySegment<char>(fullContentChars, t, fullContent.Length - t).ToArray(); //? 包括 item
                    var strAfterItem = new string(charsAfterItem);
                    
                    string output;
                    int tDelta;

                    //如果是富文本，要立刻输出好整段富文本
                    if (dialogRichTextSupported.Any(p => strAfterItem.StartsWith(p)))
                    {
                        var endIndex = strAfterItem.IndexOf('>') + 1; //? 如果不 +1, 富文本会瞬间闪烁然后消失 

                        tDelta = endIndex;
                        output = new string(new ArraySegment<char>(charsAfterItem, 0, endIndex).ToArray());
                    }
                    //如果不是富文本，就正常的一个字一个字输出
                    else
                    {
                        output = item.ToString();
                        tDelta = 1;
                    }

                    //输出文本
                    dialogText.text.text += output;


                    //判断是否达到等待时间
                    float timer = Tools.time + current.waitTime;
                    while (Tools.time < timer)
                    {
                        //如果点击跳过对话，则直接输出所有对话文本
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


                //判断是否为连续对话或最后一个对话
                if (current.continued || i == displayingDialog.dialogs.Count - 1)
                    goto finishContentDirectly;

                finishContent:
                //等待玩家跳过对话
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

            //等待玩家结束对话
            while (!PlayerControls.SkipDialog(player))
                await UniTask.NextFrame();   //等一帧, 防止连续跳过 (我猜会有这个问题:D)

            //等一帧防止跳跃
            await UniTask.NextFrame();

            //关闭对话框
            GameUI.SetPage(null);
            displayingDialog = null;
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
                    (var panel, _) = GameUI.LeavingGameMask(new((_, _) =>
                    {
                        //清除方块防止警告
                        if (Map.HasInstance())
                        {
                            Map.instance.RecoverChunks();
                        }

                        ManagerNetwork.instance.StopHost();
                    }), null);

                    panel.OnUpdate += x => GameUI.SetUILayerToTop(x);
                    panel.CustomMethod("fade_in", null);
                });
            }
            //如果是单纯的客户端
            else
            {
                (var panel, _) = GameUI.LeavingGameMask(new((_, _) =>
                {
                    Client.Disconnect();
                }), null);

                panel.OnUpdate += x => GameUI.SetUILayerToTop(x);
                panel.CustomMethod("fade_in", null);
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
            /*                                     聊天                                     */
            /* -------------------------------------------------------------------------- */
            //生成面板, 设置颜色为深灰色半透明
            chatView = GameUI.AddScrollView(UPC.StretchDouble, "ori:view.chat");
            chatView.scrollViewImage.color = new(0.2f, 0.2f, 0.2f, 0.6f);
            chatView.rt.sizeDelta = Vector2.zero;
            chatView.gameObject.SetActive(false);

            chatInput = GameUI.AddInputButton(UPC.Down, "ori:input_button.chat", chatView);
            chatInput.field.image.color = new(1, 1, 1, 0.8f);
            chatInput.button.image.color = new(1, 1, 1, 0.8f);
            chatInput.OnClickBind(() =>
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






            #region 手机

            Vector2 phoneUniversalSize = new(100, 100);

            /* -------------------------------------------------------------------------- */
            /*                                    虚拟指针                                    */
            /* -------------------------------------------------------------------------- */
            {
                touchScreenCursorImage = GameUI.AddImage(UPC.Middle, "ori:image.player_cursor", "ori:player_cursor", GameUI.worldSpaceCanvas.gameObject);
                touchScreenCursorImage.rt.sizeDelta = Vector2.one;
            }

            /* -------------------------------------------------------------------------- */
            /*                                    添加摇杆                                    */
            /* -------------------------------------------------------------------------- */
            {
                touchScreenMoveJoystick = Joystick.Create("PlayerMoveJoystick", "ori:image.player_move_joystick_background", "ori:image.player_move_joystick_handle");

                touchScreenCursorJoystick = Joystick.Create("PlayerCursorJoystick", "ori:image.player_cursor_joystick_background", "ori:image.player_cursor_joystick_handle");
                touchScreenCursorJoystick.SetAnchorMinMax(UPC.LowerRight);
                touchScreenCursorJoystick.SetAPos(-touchScreenMoveJoystick.rectTransform.anchoredPosition.x, touchScreenMoveJoystick.rectTransform.anchoredPosition.y);
            }

            /* -------------------------------------------------------------------------- */
            /*                                     攻击                                     */
            /* -------------------------------------------------------------------------- */
            {
                touchScreenAttackButton = GameUI.AddButton(UPC.LowerRight, "ori:button.player_attack", GameUI.canvas.transform, "ori:player_attack_button");
                Component.Destroy(touchScreenAttackButton.buttonText.gameObject);
                touchScreenAttackButton.sd = phoneUniversalSize;
                touchScreenAttackButton.SetAPosOnBySizeLeft(touchScreenCursorJoystick, 150);
                touchScreenAttackButton.AddAPosY(75);
                touchScreenAttackButton.button.HideClickAction();
                touchScreenAttackButton.button.onClick.RemoveAllListeners();
            }

            /* -------------------------------------------------------------------------- */
            /*                                     使用                                     */
            /* -------------------------------------------------------------------------- */
            {
                touchScreenUseItemButton = GameUI.AddButton(UPC.LowerRight, "ori:button.player_use_item", GameUI.canvas.transform, "ori:player_use_item_button");
                Component.Destroy(touchScreenUseItemButton.buttonText.gameObject);
                touchScreenUseItemButton.sd = phoneUniversalSize;
                touchScreenUseItemButton.SetAPosOnBySizeDown(touchScreenAttackButton, 50);
                touchScreenUseItemButton.button.HideClickAction();
                touchScreenUseItemButton.button.onClick.RemoveAllListeners();

                touchScreenUseItemButtonImage = GameUI.AddImage(UPC.Middle, "ori:image.player_use_item_icon", null, touchScreenUseItemButton);
                touchScreenUseItemButtonImage.sd = touchScreenUseItemButton.sd * 0.5f;
            }

            /* -------------------------------------------------------------------------- */
            /*                                     在脚下放方块                                     */
            /* -------------------------------------------------------------------------- */
            {
                touchScreenPlaceBlockUnderPlayerButton = GameUI.AddButton(UPC.LowerRight, "ori:button.player_place_block_under_player", GameUI.canvas.transform, "ori:player_place_block_under_player_button");
                Component.Destroy(touchScreenPlaceBlockUnderPlayerButton.buttonText.gameObject);
                touchScreenPlaceBlockUnderPlayerButton.sd = phoneUniversalSize;
                touchScreenPlaceBlockUnderPlayerButton.SetAPosOnBySizeLeft(touchScreenUseItemButton, 50);
                touchScreenPlaceBlockUnderPlayerButton.button.HideClickAction();
                touchScreenPlaceBlockUnderPlayerButton.button.onClick.RemoveAllListeners();
            }

            /* -------------------------------------------------------------------------- */
            /*                                     暂停                                     */
            /* -------------------------------------------------------------------------- */
            {
                touchScreenPauseButton = GameUI.AddButton(UPC.UpperRight, "ori:button.player_pause", GameUI.canvas.transform, "ori:player_pause_button");
                touchScreenPauseButton.buttonText.gameObject.SetActive(false);
                touchScreenPauseButton.image.rectTransform.sizeDelta = new(75, 75);
                touchScreenPauseButton.image.rectTransform.anchoredPosition = new(-70, -75);
                touchScreenPauseButton.button.HideClickAction();
                touchScreenPauseButton.button.onClick.RemoveAllListeners();
                touchScreenPauseButton.OnClickBind(() =>
                {
                    PauseGame();
                });
            }

            /* -------------------------------------------------------------------------- */
            /*                                     合成                                     */
            /* -------------------------------------------------------------------------- */
            {
                touchScreenCraftingButton = GameUI.AddButton(UPC.UpperRight, "ori:button.player_crafting", GameUI.canvas.transform, "ori:player_crafting_button");
                touchScreenCraftingButton.buttonText.gameObject.SetActive(false);
                touchScreenCraftingButton.image.rectTransform.sizeDelta = new(75, 75);
                touchScreenCraftingButton.SetAPosOnBySizeDown(touchScreenPauseButton, 20);
                touchScreenCraftingButton.button.HideClickAction();
                touchScreenCraftingButton.button.onClick.RemoveAllListeners();
                touchScreenCraftingButton.OnClickBind(() =>
                {
                    if (backpackMask && GameUI.page?.ui != dialogPanel)
                        ShowOrHideBackpackAndSetPanelToInventory();
                });
            }

            /* -------------------------------------------------------------------------- */
            /*                                     任务                                     */
            /* -------------------------------------------------------------------------- */
            {
                touchScreenShowTaskButton = GameUI.AddButton(UPC.UpperRight, "ori:button.player_show_task", GameUI.canvas.transform, "ori:player_show_task_button");
                touchScreenShowTaskButton.buttonText.gameObject.SetActive(false);
                touchScreenShowTaskButton.image.rectTransform.sizeDelta = new(75, 75);
                touchScreenShowTaskButton.SetAPosOnBySizeDown(touchScreenCraftingButton, 20);
                touchScreenShowTaskButton.button.HideClickAction();
                touchScreenShowTaskButton.button.onClick.RemoveAllListeners();
                touchScreenShowTaskButton.OnClickBind(() =>
                {
                    if (backpackMask && GameUI.page?.ui != dialogPanel)
                        ShowOrHideBackpackAndSetPanelToTask();
                });
            }

            #endregion

            /* -------------------------------------------------------------------------- */
            /*                                    对话                                    */
            /* -------------------------------------------------------------------------- */
            {
                dialogPanel = GameUI.AddButton(new(0, 0, 1, 0.4f), "ori:panel.dialog");
                dialogPanel.gameObject.SetActive(false);
                dialogPanel.image.sprite = null;
                dialogPanel.image.SetColor(0.15f, 0.15f, 0.15f, 0.6f);
                dialogPanel.button.HideClickAction();
                dialogPanel.button.onClick.RemoveAllListeners();
                dialogPanel.sd = Vector2.zero;
                GameObject.Destroy(dialogPanel.buttonText.gameObject);

                dialogHead = GameUI.AddImage(UPC.UpperLeft, "ori:image.dialog_head", null, dialogPanel);
                dialogHead.SetSizeDelta(160, 160);
                dialogHead.ap = new(dialogHead.sd.x / 2, -dialogHead.sd.y / 2);

                dialogNameText = GameUI.AddText(UPC.Down, "ori:text.dialog_name", dialogHead);
                dialogNameText.SetAPosY(-dialogNameText.sd.y / 2 - 10);
                dialogNameText.doRefresh = false;

                dialogText = GameUI.AddText(UPC.Right, "ori:text.dialog", dialogHead);
                dialogText.text.SetFontSize(28);
                dialogText.doRefresh = false;
                dialogText.OnUpdate += x =>
                {
                    x.SetSizeDelta(GameUI.canvasScaler.referenceResolution.x - dialogHead.sd.x, dialogHead.sd.y);
                    x.SetAPos(dialogText.sd.x / 2, 0);
                };
            }

            #region 添加快速物品栏
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
                    var button = GameUI.AddButton(UPC.Down, "ori:button.item_tab_" + indexAs0, GameUI.canvas.transform, "ori:item_tab");
                    var item = GameUI.AddImage(UPC.Down, "ori:image.item_tab_item_" + indexAs0, "ori:item_tab", button);

                    item.rectTransform.SetParentForUI(button.rectTransform);
                    button.rectTransform.sizeDelta = vecButtonSize;
                    button.rectTransform.AddLocalPosX((i + 0.5f) * buttonSize);
                    button.rectTransform.AddLocalPosY(buttonExtraY);
                    item.rectTransform.sizeDelta = vecButtonItemSize;

                    //Destroy(button.buttonText.gameObject);
                    button.buttonText.rectTransform.anchorMin = UPC.Down;
                    button.buttonText.rectTransform.anchorMax = UPC.Down;
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
                backpackMask = GameUI.AddPanel("ori:panel.backpack_mask");
                backpackMask.panelImage.color = new Color32(50, 50, 50, 80);
                backpackMask.gameObject.SetActive(false);
                backpackMask.OnUpdate += x => GameUI.SetUILayerToFirst(x);

                backpackPanelBackground = GameUI.AddImage(UPC.Middle, "ori:image.backpack_panel_background", null, backpackMask);
                backpackPanelBackground.SetSizeDelta(700, Player.backpackPanelHeight);

                #region 物品栏

                //背包物品视图
                (inventoryItemPanel, inventoryItemView) = GenerateItemViewBackpackPanel("ori:inventory", "ori:switch_button.inventory", 80, Vector2.zero, Vector2.zero);

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

                inventoryHelmetUI.button.SetAnchorMinMax(UPC.LowerLeft);
                inventoryBreastplateUI.button.SetAnchorMinMax(UPC.LowerLeft);
                inventoryLeggingUI.button.SetAnchorMinMax(UPC.LowerLeft);
                inventoryBootsUI.button.SetAnchorMinMax(UPC.LowerLeft);

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

                //制作结果
                (craftingPanel, craftingView) = GenerateItemViewBackpackPanel("ori:crafting", "ori:switch_button.crafting", 70, Vector2.zero, Vector2.zero);

                {
                    int borderSize = 10;
                    int innerInterval = 3;

                    ImageIdentity backgroundImage = GameUI.AddImage(UPC.Middle, "ori:image.crafting_info_shower_background", "ori:crafting_info_shower_background");
                    ScrollViewIdentity ingredientsView = GameUI.AddScrollView(UPC.Up, "ori:scrollview.crafting_info_shower_ingredients", backgroundImage);
                    ImageIdentity arrow = GameUI.AddImage(UPC.Up, "ori:image.crafting_info_shower_arrow", "ori:crafting_info_shower_arrow", backgroundImage);
                    ScrollViewIdentity resultsView = GameUI.AddScrollView(UPC.Up, "ori:scrollview.crafting_info_shower_results", backgroundImage);
                    TextIdentity maximumCraftingTimesText = GameUI.AddText(UPC.LowerRight, "ori:text.crafting_info_shower.maximum_crafting_times", backgroundImage);

                    backgroundImage.OnUpdate += x => GameUI.SetUILayerToTop(x);

                    backgroundImage.SetSizeDelta(300, 200);
                    backgroundImage.image.raycastTarget = false;

                    ingredientsView.SetSizeDelta(backgroundImage.sd.x - 15, 45);
                    ingredientsView.SetAPosY(-ingredientsView.sd.y / 2 - borderSize - 5);
                    ingredientsView.gridLayoutGroup.cellSize = new(35, 35);
                    ingredientsView.scrollRect.vertical = false;
                    ingredientsView.scrollRect.horizontal = false;

                    arrow.SetSizeDelta(40, 40);
                    arrow.SetAPosY(ingredientsView.ap.y - ingredientsView.sd.y / 2 - arrow.sd.y - innerInterval);

                    resultsView.SetSizeDelta(ingredientsView.sd);
                    resultsView.SetAPosY(arrow.ap.y - arrow.sd.y / 2 - resultsView.sd.y - innerInterval);
                    resultsView.gridLayoutGroup.cellSize = ingredientsView.gridLayoutGroup.cellSize;
                    resultsView.scrollRect.vertical = false;
                    resultsView.scrollRect.horizontal = false;

                    maximumCraftingTimesText.text.alignment = TMPro.TextAlignmentOptions.Right;
                    maximumCraftingTimesText.SetSizeDelta(backgroundImage.sd.x, 30);
                    maximumCraftingTimesText.SetAPos(-maximumCraftingTimesText.sd.x / 2, maximumCraftingTimesText.sd.y / 2 - borderSize);
                    maximumCraftingTimesText.text.SetFontSize(16);
                    maximumCraftingTimesText.autoCompareText = false;
                    maximumCraftingTimesText.text.raycastTarget = false;


                    craftingInfoShower = new(player, backgroundImage, ingredientsView, arrow, resultsView, maximumCraftingTimesText);
                    craftingInfoShower.Hide();
                }



                craftingView.CustomMethod += (type, _) =>
                    {
                        type ??= "refresh";

                        if (type == "refresh")
                        {
                            //获取本地玩家的所有物品
                            craftingView.Clear();
                            var craftingResults = Player.GetCraftingRecipesThatCanBeCrafted(player.inventory.slots);

                            foreach (var pair in craftingResults)
                            {
                                var recipe = pair.recipe;
                                var itemGot = ModFactory.CompareItem(recipe.result.id);

                                //添加按钮
                                var button = GameUI.AddButton(UPC.Up, $"ori:button.player_crafting_recipe_{recipe.id}");
                                button.image.sprite = ModFactory.CompareTexture("ori:item_tab").sprite;
                                button.button.OnPointerEnterAction += _ => craftingInfoShower.Show(recipe, pair.ingredients);
                                button.button.OnPointerExitAction += _ => craftingInfoShower.Hide();
                                button.OnClickBind(() =>
                                {
                                    var resultItem = ModConvert.ItemDataToItem(ModFactory.CompareItem(recipe.result.id));
                                    resultItem.count = recipe.result.count;

                                    //检查背包空间
                                    Inventory.GetIndexesToPutItemIntoItems(player.inventory.slots, resultItem, out bool hasReachedNeededCount);
                                    if (!hasReachedNeededCount)
                                    {
                                        InternalUIAdder.instance.SetStatusText("背包栏位不够了，请清理背包后再合成");
                                        return;
                                    }

                                    //从玩家身上减去物品
                                    pair.ingredients.For(ingredientToRemove =>
                                    {
                                        ingredientToRemove.For(itemToRemove =>
                                        {
                                            player.ServerReduceItemCount(itemToRemove.Key.ToString(), itemToRemove.Value);
                                        });
                                    });

                                    //给予玩家物品
                                    player.ServerAddItem(resultItem);

                                    //达成效果
                                    CompleteTask("ori:craft");
                                    GAudio.Play(AudioID.Crafting);


                                    //制作后刷新合成界面, 原料表与标题
                                    player.OnInventoryItemChange(player.inventory, null);
                                });

                                //图标
                                var image = GameUI.AddImage(UPC.Middle, $"ori:image.player_crafting_recipe_{recipe.id}", "ori:item_tab", button);
                                image.image.sprite = itemGot.texture.sprite;
                                image.sd = craftingView.gridLayoutGroup.cellSize * 0.75f;

                                //文本
                                button.buttonText.rectTransform.SetAsLastSibling();
                                button.buttonText.rectTransform.AddLocalPosY(-20);
                                button.buttonText.SetSizeDelta(85, 27);
                                button.buttonText.text.SetFontSize(10);
                                button.buttonText.AfterRefreshing += t =>
                                {
                                    t.text.text = $"{GameUI.CompareText(itemGot.id)?.text}x{recipe.result.count}";
                                };

                                craftingView.AddChild(button);
                            }
                        }
                    };

                #endregion

                #region 暂停界面

                pausePanel = GenerateBackpackPanel("ori:pause", "ori:switch_button.pause");

                ButtonIdentity continueGame = GameUI.AddButton(UPC.Middle, "ori:button.pause_continue_game", pausePanel.panel).OnClickBind(() =>
                {
                    GameUI.SetPage(null);
                });
                ButtonIdentity quitGame = GameUI.AddButton(UPC.Middle, "ori:button.pause_quit_game", pausePanel.panel).OnClickBind(LeftGame);

                continueGame.rt.AddLocalPosY(30);
                quitGame.rt.AddLocalPosY(-30);

                #endregion

                #region 任务系统

                /* ----------------------------------- 生成任务视图 ----------------------------------- */
                //生成面板, 设置颜色为深灰色半透明
                taskPanel = GenerateBackpackPanel("ori:tasks", "ori:switch_button.tasks");
                taskView = GameUI.AddScrollView(UPC.StretchDouble, "ori:view.task", taskPanel);
                taskView.scrollViewImage.color = new(0.2f, 0.2f, 0.2f, 0.6f);
                taskView.rt.sizeDelta = Vector2.zero;
                taskView.content.anchoredPosition = new(GameUI.canvasScaler.referenceResolution.x / 2, GameUI.canvasScaler.referenceResolution.y / 2);  //将任务居中
                taskView.scrollRect.horizontal = true;   //允许水平拖拽
                taskView.scrollRect.movementType = ScrollRect.MovementType.Unrestricted;   //不限制拖拽
                taskView.scrollRect.scrollSensitivity = 0;   //不允许滚轮控制
                taskView.gameObject.AddComponent<RectMask2D>();   //添加新的遮罩
                UnityEngine.Object.Destroy(taskView.viewportMask);   //删除自带的遮罩
                UnityEngine.Object.Destroy(taskView.gridLayoutGroup);   //删除自动排序器
                UnityEngine.Object.Destroy(taskView.scrollRect.horizontalScrollbar.gameObject);   //删除水平滚动条
                UnityEngine.Object.Destroy(taskView.scrollRect.verticalScrollbar.gameObject);   //删除水平滚动条

                /* -------------------------------- 生成任务完成图像 -------------------------------- */
                taskCompleteBackground = GameUI.AddImage(UPC.UpperLeft, "ori:image.task_complete_background", "ori:task_complete");
                taskCompleteBackground.SetSizeDelta(320, 100);
                taskCompleteBackground.SetAPos(taskCompleteBackground.sd.x / 2, -taskCompleteBackground.sd.y / 2);
                taskCompleteBackground.gameObject.SetActive(false);
                taskCompleteBackground.OnUpdate += _ =>
                {
                    GameUI.SetUILayerToTop(taskView);
                };

                taskCompleteIcon = GameUI.AddImage(UPC.Left, "ori:image.task_complete_icon", null, taskCompleteBackground);
                taskCompleteIcon.SetSizeDelta(taskCompleteBackground.sd.y, taskCompleteBackground.sd.y);
                taskCompleteIcon.SetAPosX(taskCompleteIcon.sd.x / 2);

                taskCompleteText = GameUI.AddText(UPC.Middle, "ori:text.task_complete", taskCompleteBackground);
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
                        rootTaskNode = AddChildrenNodesToNode(item);
                        break;
                    }
                }

                TaskNode AddChildrenNodesToNode(TaskData data)
                {
                    TaskNode temp = new(data);

                    foreach (var task in tasks)
                    {
                        //如果是 current 的子任务
                        if (task.parent == data.id)
                        {
                            temp.nodes.Add(AddChildrenNodesToNode(task));
                        }
                    }

                    return temp;
                }

                /* --------------------------------- 显示任务节点 --------------------------------- */
                RefreshTaskNodesDisplay(true);

                /* --------------------------------- 加载已有任务 --------------------------------- */
                foreach (var task in player.completedTasks)
                {
                    CompleteTask(task.id, false, task.hasGotRewards);
                }

                #endregion
            }
            #endregion

            #region 添加状态栏
            {
                Vector4 posC = UPC.UpperRight;
                int xExtraOffset = -40;
                int yExtraOffset = -35;

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
                    Vector2 size = new(160, 40);
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
                dayNightBar = GameUI.AddImage(UPC.Up, "ori:image.day_night_bar", "ori:day_night_bar");
                dayNightBar.SetSizeDelta(300, 50);
                dayNightBar.SetAPosY(-dayNightBar.sd.y / 2 - 50);

                dayNightBarPointer = GameUI.AddImage(UPC.UpperLeft, "ori:image.day_night_bar_pointer", "ori:day_night_bar_pointer", dayNightBar);
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
                rebornButton = GameUI.AddButton(UPC.Middle, "ori:button.reborn", rebornPanel);
                rebornPanelText = GameUI.AddText(UPC.Middle, "ori:text.reborn_info", rebornPanel);
                rebornTimerText = GameUI.AddText(UPC.Middle, "ori:text.reborn_timer", rebornPanel);

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
#if UNITY_EDITOR
                    if (Keyboard.current?.spaceKey?.wasPressedThisFrame ?? false)
                        player.deathTimer = 0;
#endif

                    if (Tools.time >= player.deathTimer)
                    {
                        rebornButton.button.interactable = true;
                        rebornTimerText.gameObject.SetActive(false);
                    }
                    else
                    {
                        rebornButton.button.interactable = false;
                        rebornTimerText.gameObject.SetActive(true);
                        rebornTimerText.text.text = ((int)(player.deathTimer - Tools.time)).ToString();
                    }
                };
            }
            #endregion
        }







        #region  背包界面

        public BackpackPanel GenerateBackpackPanel(
            string id,
            string switchButtonTexture,
            Action OnActivate = null,
            Action OnDeactivate = null,
            string texture = "ori:backpack_inventory_background")
        {
            (var modId, var panelName) = Tools.SplitModIdAndName(id);


            /* -------------------------------------------------------------------------- */
            /*                                    生成面板                                    */
            /* -------------------------------------------------------------------------- */
            var panel = GameUI.AddPanel($"{modId}:panel.{panelName}", backpackPanelBackground);
            panel.panelImage.sprite = ModFactory.CompareTexture(texture).sprite;
            panel.panelImage.color = Color.white;
            panel.gameObject.SetActive(false);



            /* -------------------------------------------------------------------------- */
            /*                                    生成按钮                                    */
            /* -------------------------------------------------------------------------- */

            /* ---------------------------------- 按钮显示 ---------------------------------- */
            var switchButtonBackground = GameUI.AddImage(UPC.UpperLeft, $"{modId}:image.{panelName}_switch_background", "ori:backpack_panel_switch_button", backpackPanelBackground);
            switchButtonBackground.sd = new(50, 50);

            float signImageBackgroundX = switchButtonBackground.sd.x / 2;
            foreach (var item in backpackPanels)
            {
                signImageBackgroundX += switchButtonBackground.sd.x + 15;
            }
            switchButtonBackground.SetAPos(signImageBackgroundX, switchButtonBackground.sd.y / 2);




            var switchButton = GameUI.AddButton(UPC.Middle, $"{modId}:button.{panelName}_switch", switchButtonBackground, switchButtonTexture);
            switchButton.sd = switchButtonBackground.sd;
            GameObject.Destroy(switchButton.buttonText.gameObject);

            /* ---------------------------------- 按钮功能 ---------------------------------- */
            switchButton.OnClickBind(() => SetBackpackPanel(id));






            /* ----------------------------------------------------------------------- */
            var ActualActivate = new Action(() =>
            {
                panel.gameObject.SetActive(true);
                OnActivate?.Invoke();
            });
            var ActualDeactivate = new Action(() =>
            {
                panel.gameObject.SetActive(false);
                OnDeactivate?.Invoke();
            });

            BackpackPanel result = new(id, panel, switchButtonBackground, switchButton, ActualActivate, ActualDeactivate);
            backpackPanels.Add(result);
            return result;
        }

        public void DestroyBackpackPanel(string id)
        {
            foreach (var item in backpackPanels)
            {
                if (item.id == id)
                {
                    GameObject.Destroy(item.panel.gameObject);
                    backpackPanels.Remove(item);
                    return;
                }
            }

            Debug.LogError($"未找到背包界面 {id}, 销毁失败");
        }

        public (BackpackPanel panel, ScrollViewIdentity itemView) GenerateItemViewBackpackPanel(
            string id,
            string switchButtonTexture,
            float cellSize,
            Vector2 viewSize,
            Vector2 cellSpacing,
            Action OnActivate = null,
            Action OnDeactivate = null,
            string texture = "ori:backpack_inventory_background")
        {
            (var modId, var panelName) = Tools.SplitModIdAndName(id);

            var panel = GenerateBackpackPanel(id, switchButtonTexture, OnActivate, OnDeactivate, texture);
            var view = GenerateItemScrollView($"{modId}:scrollview.{panelName}", cellSize, viewSize, cellSpacing, null);
            view.transform.SetParent(panel.panel.rt, false);
            view.SetAnchorMinMax(UPC.StretchDouble);
            view.content.anchoredPosition = Vector2.zero;
            view.content.sizeDelta = Vector2.zero;
            Component.Destroy(view.viewportImage);

            return (panel, view);
        }

        public void SetBackpackPanel(string id)
        {
            foreach (var item in backpackPanels)
                if (item.id == currentBackpackPanel)
                    item.Deactivate();

            foreach (var item in backpackPanels)
            {
                if (item.id == id)
                {
                    item.Activate();
                    currentBackpackPanel = id;
                    return;
                }
            }

            Debug.LogError($"未找到背包界面 {id}, 设置背包界面失败");
        }

        public void ShowOrHideBackpackAndSetPanelTo(string backpackPanelId)
        {
            if (GameUI.page.ui == backpackMask)
            {
                //如果处于背包界面，并且是指定面板，就关闭
                if (currentBackpackPanel == backpackPanelId)
                {
                    ShowOrHideBackpack();
                    GameUI.SetPage(null);
                }
                //如果处于背包界面，且不是指定面板，就切换到指定面板
                else
                {
                    SetBackpackPanel(backpackPanelId);
                }
            }
            //如果不处于背包界面，就打开并切换到指定面板
            else if (GameUI.page == null || !GameUI.page.ui)
            {
                ShowOrHideBackpack();
                SetBackpackPanel(backpackPanelId);
            }
        }

        public void ShowOrHideBackpack()
        {
            //启用状态 -> 禁用
            if (backpackMask.gameObject.activeSelf)
            {
                //TODO: Fix
                ItemInfoShower.Hide();
                craftingInfoShower.Hide();
                TaskInfoShower.Hide();
                ItemDragger.CancelDragging();

                GameUI.SetPage(null);
                GAudio.Play(AudioID.CloseBackpack);
            }
            //禁用状态 -> 启用
            else
            {
                player.OnInventoryItemChange(player.inventory, null);
                GAudio.Play(AudioID.OpenBackpack);

                GameUI.SetPage(backpackMask);
            }
        }

        public void ShowOrHideBackpackAndSetPanelToInventory()
        {
            //Backpack 是整个界面
            //Inventory 是中间的所有物品
            //QuickInventory 是不打开背包时看到的几格物品栏

            ShowOrHideBackpackAndSetPanelTo("ori:inventory");
        }

        public void ShowOrHideBackpackAndSetPanelToCrafting()
        {
            ShowOrHideBackpackAndSetPanelTo("ori:crafting");
        }

        public void ShowOrHideBackpackAndSetPanelToTask()
        {
            ShowOrHideBackpackAndSetPanelTo("ori:tasks");
        }

        public void PauseGame()
        {
            //如果 没有界面&不在暂停页面
            if ((GameUI.page == null || !GameUI.page.ui) && GameUI.page.ui != pausePanel.panel)
                ShowOrHideBackpackAndSetPanelTo("ori:pause");
            else
                GameUI.SetPage(null);
        }

        #endregion





        public ScrollViewIdentity GenerateItemScrollView(string id, float cellSize, Vector2 viewSize, Vector2 cellSpacing, string backgroundTexture)
        {
            //桶的物品视图
            var itemView = GameUI.AddScrollView(UPC.Middle, id);
            itemView.SetSizeDelta(viewSize);
            itemView.viewportImage.sprite = backgroundTexture == null ? null : ModFactory.CompareTexture(backgroundTexture).sprite;

            //设置收缩
            int offset = 5;
            itemView.gridLayoutGroup.padding = new(offset, offset, offset, offset);

            itemView.gridLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            itemView.gridLayoutGroup.cellSize = new(cellSize, cellSize);
            itemView.gridLayoutGroup.spacing = cellSpacing;
            itemView.scrollViewImage.color = Color.clear;
            itemView.viewportImage.color = backpackColor;
            itemView.content.sizeDelta = new(0, itemView.content.sizeDelta.y);
            itemView.content.anchoredPosition = new(-backpackPanelBackground.sd.x / 2 - itemView.content.sizeDelta.x / 2, itemView.content.anchoredPosition.y);

            return itemView;
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

                var item = inventoryTemp?.GetItemChecked(i);

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
            //TODO: PlayerControls ify
            if (GameUI.page?.ui != dialogPanel)
            {
                switch (GControls.mode)
                {
                    case ControlMode.KeyboardAndMouse:
                        if (Keyboard.current != null)
                        {
                            if (Keyboard.current.cKey.wasReleasedThisFrame)
                                ShowOrHideBackpackAndSetPanelToCrafting();

                            if (Keyboard.current.escapeKey.wasReleasedThisFrame)
                                PauseGame();

                            if (Keyboard.current.enterKey.wasReleasedThisFrame)
                                Chat();

                            if (Keyboard.current.tKey.wasReleasedThisFrame)
                                ShowOrHideBackpackAndSetPanelToTask();
                        }

                        break;

                    case ControlMode.Gamepad:
                        if (Gamepad.current != null)
                        {
                            if (Gamepad.current.yButton.wasReleasedThisFrame)
                                ShowOrHideBackpackAndSetPanelToCrafting();

                            if (Gamepad.current.startButton.wasReleasedThisFrame)
                                PauseGame();

                            if (Gamepad.current.dpad.down.wasReleasedThisFrame)
                                Chat();

                            if (Gamepad.current.dpad.up.wasReleasedThisFrame)
                                ShowOrHideBackpackAndSetPanelToTask();
                        }

                        break;
                }
            }

            #region 手机操控

            if (GControls.mode == ControlMode.Touchscreen)
            {
                SetUIHighest(touchScreenMoveJoystick);
                SetUIHighest(touchScreenCursorJoystick);
                SetUIHighest(touchScreenCursorImage);
                SetUIHighest(touchScreenAttackButton);
                SetUIHighest(touchScreenUseItemButton);
                SetUIHighest(touchScreenPlaceBlockUnderPlayerButton);
                SetUIHighest(touchScreenPauseButton);
                SetUIHighest(touchScreenCraftingButton);
                SetUIHighest(touchScreenShowTaskButton);

                touchScreenUseItemButtonImage.image.sprite = player.TryGetUsingItem()?.data?.texture?.sprite;
                touchScreenUseItemButtonImage.image.color = touchScreenUseItemButtonImage.image.sprite ? Color.white : Color.clear;

                if (touchScreenCursorJoystick.Horizontal != 0 || touchScreenCursorJoystick.Vertical != 0)
                {
                    float radius = player.useRadius;

                    touchScreenCursorImage.image.enabled = true;
                    touchScreenCursorImage.rt.localPosition = new(
                        player.transform.position.x + touchScreenCursorJoystick.Horizontal * radius,
                        player.transform.position.y + touchScreenCursorJoystick.Vertical * radius);

                    player.OnHoldAttack();
                }
                else
                {
                    touchScreenCursorImage.image.enabled = false;
                }
            }
            else
            {
                SetUIDisabled(touchScreenMoveJoystick);
                SetUIDisabled(touchScreenCursorJoystick);
                SetUIDisabled(touchScreenCursorImage);
                SetUIDisabled(touchScreenAttackButton);
                SetUIDisabled(touchScreenUseItemButton);
                SetUIDisabled(touchScreenPlaceBlockUnderPlayerButton);
                SetUIDisabled(touchScreenPauseButton);
                SetUIDisabled(touchScreenCraftingButton);
                SetUIDisabled(touchScreenShowTaskButton);
            }

            #endregion






            /* ---------------------------------- 刷新状态 ---------------------------------- */
            RefreshPropertiesBar();
        }

        public void RefreshPropertiesBar()
        {
            thirstBarFull.image.fillAmount = player.thirstValue / Player.maxThirstValue;
            hungerBarFull.image.fillAmount = player.hungerValue / Player.maxHungerValue;
            happinessBarFull.image.fillAmount = player.happinessValue / Player.maxHappinessValue;
            healthBarFull.image.fillAmount = (float)player.health / player.maxHealth;
        }

        public static Action<PlayerUI> BindTasks = ui =>
        {
            ui.AddTask("ori:get_dirt", "ori:task.get_dirt", null, new[] { $"{BlockID.Dirt}/=/25/=/null" });

            ui.AddTask("ori:craft", "ori:task.craft", "ori:get_dirt", null);

            ui.AddTask("ori:get_log", "ori:task.get_log", "ori:get_dirt", new[] { $"{BlockID.OakLog}/=/10/=/null" });

            ui.AddTask("ori:get_meat", "ori:task.get_meat", "ori:get_dirt", null);
            ui.AddTask("ori:get_egg", "ori:task.get_egg", "ori:get_meat", null);
            ui.AddTask("ori:get_potato", "ori:task.get_potato", "ori:get_meat", null);
            ui.AddTask("ori:get_onion", "ori:task.get_onion", "ori:get_meat", null);
            ui.AddTask("ori:get_watermelon", "ori:task.get_watermelon", "ori:get_meat", null);

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

        public void RefreshTaskNodesDisplay(bool init)
        {
            if (init)
                taskView.Clear();

            RefreshChildrenTaskNodesDisplay(rootTaskNode, null, new(), init);
        }

        private void RefreshChildrenTaskNodesDisplay(TaskNode current, TaskNode parentNode, List<TaskNode> siblingNodes, bool init)
        {
            /* ----------------------------------- 初始化 ---------------------------------- */
            if (init)
                TaskNodeDisplay_InitButton(current, parentNode, siblingNodes);

            /* ---------------------------------- 设置图标 ---------------------------------- */
            current.icon.SetID($"ori:image.task_node.{current.data.id}");
            current.icon.image.sprite = ModFactory.CompareTexture(current.data.icon).sprite;

            /* ---------------------------------- 设置颜色 ---------------------------------- */
            current.icon.image.color = current.button.image.color =
                        current.completed ?
                            (current.hasGotRewards ?
                                Color.white :  //完成了且领取了奖励
                                Tools.HexToColor("#00FFD6")) :  //完成了且没领取奖励
                            new(0.5f, 0.5f, 0.5f, 0.75f);  //没完成

            if (current.line) current.line.image.color = current.icon.image.color;

            /* ---------------------------------- 添加到节点组 & 初始化子节点 --------------------------------- */
            siblingNodes.Add(current);

            List<TaskNode> childrenNodes = new();
            foreach (var node in current.nodes)
            {
                RefreshChildrenTaskNodesDisplay(node, current, childrenNodes, init);
            }
        }

        private void TaskNodeDisplay_InitButton(TaskNode node, TaskNode parentNode, List<TaskNode> siblingNodes)
        {
            /* ---------------------------------- 初始化按钮 --------------------------------- */
            int space = 40;
            node.button = GameUI.AddButton(UPC.Middle, $"ori:button.task_node.{node.data.id}", GameUI.canvas.transform, "ori:square_button");
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
                RefreshTaskNodesDisplay(false);
            });

            /* ---------------------------------- 设置父物体 --------------------------------- */
            if (parentNode == null)
                taskView.AddChild(node.button);
            else
                node.button.SetParentForUI(parentNode.button);

            /* -------------------------------- 根据父节点更改位置 ------------------------------- */
            Vector2 tempVec = Vector2.zero;
            if (parentNode != null) { tempVec.y -= node.button.sd.y + space; }
            int childrenCountOfCurrentNode = 0;

            //统计自己的子任务数
            foreach (var task in tasks)
            {
                if (task.parent == node.data.id)
                {
                    childrenCountOfCurrentNode++;
                }
            }

            /* ------------------------------- 根据同级节点更改位置 ------------------------------- */
            foreach (var siblingNode in siblingNodes)
            {
                int childrenCountOfSiblingNode = 0;

                //统计自己和相邻节点的子任务数
                foreach (var task in tasks)
                {
                    if (task.parent == siblingNode.data.id)
                    {
                        childrenCountOfSiblingNode++;
                    }
                }

                float countOfChildrenNodesThatCauseCoincidence = childrenCountOfCurrentNode / 2f + childrenCountOfSiblingNode / 2f; // 要除以 2 是因为只有一半的子节点会影响到对方
                float deltaPos = siblingNode.button.sd.x * 0.5f + node.button.sd.x * 0.5f + space * countOfChildrenNodesThatCauseCoincidence;

                //更改同级节点位置
                siblingNode.button.ap = new(siblingNode.button.ap.x - deltaPos, siblingNode.button.ap.y);

                //重新计算节点
                TaskNodeDisplay_InitLine(siblingNode);

                //更改本身
                tempVec.x += deltaPos;
            }

            /* -------------------------------- 设置按钮和文本位置 ------------------------------- */
            node.button.ap = tempVec;
            node.button.buttonText.AddAPosY(-node.button.sd.y / 2 - node.button.buttonText.sd.y / 2 - 5);

            /* ---------------------------------- 设置图标 ---------------------------------- */
            node.icon = GameUI.AddImage(UPC.Middle, $"ori:image.task_node.{node.data.id}", null, node.button);
            node.icon.sd = node.button.sd;

            /* --------------------------------- 初始化连接线 --------------------------------- */
            TaskNodeDisplay_InitLine(node);
        }

        private static void TaskNodeDisplay_InitLine(TaskNode node)
        {
            if (node.parent == null)
                return;

            if (!node.line)
                node.line = GameUI.AddImage(UPC.Middle, $"ori:button.task_node.{node.data.id}.line", null, node.button);

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
            yield return new WaitForSeconds(5f);

            if (taskCompleteBackground)
                GameUI.Disappear(taskCompleteBackground);

            playingTaskCompletion = false;
        }

        public void CompleteTask(string id, bool feedback = true, bool hasGotRewards = false)
        {
            if (CompleteTask_Internal(rootTaskNode, id, out bool hasCompletedBefore, out TaskNode nodeCompleted) && !hasCompletedBefore && nodeCompleted != null)
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

            RefreshTaskNodesDisplay(false);
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
                    ImageIdentity image = GameUI.AddImage(UPC.Middle, "ori:image.task_info_shower", "ori:item_info_shower");
                    TextIdentity nameText = GameUI.AddText(UPC.UpperLeft, "ori:text.task_info_shower.name", image);
                    TextIdentity detailText = GameUI.AddText(UPC.UpperLeft, "ori:text.task_info_shower.detail", image);

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

    public class BackpackPanel : IRectTransform
    {
        public string id;
        public PanelIdentity panel;
        public ImageIdentity switchButtonBackground;
        public ButtonIdentity switchButton;
        public Action Activate;
        public Action Deactivate;

        public BackpackPanel(string id, PanelIdentity panel, ImageIdentity switchButtonBackground, ButtonIdentity switchButton, Action Activate, Action Deactivate)
        {
            this.id = id;
            this.panel = panel;
            this.switchButtonBackground = switchButtonBackground;
            this.switchButton = switchButton;
            this.Activate = Activate;
            this.Deactivate = Deactivate;
        }

        public RectTransform rectTransform => panel.rectTransform;
    }

    public class InventorySlotUI
    {
        public ButtonIdentity button;
        public ImageIdentity content;

        public static InventorySlotUI Generate(string buttonId, string imageId, Vector2 sizeDelta)
        {
            ButtonIdentity button = GameUI.AddButton(UPC.Middle, buttonId);
            ImageIdentity image = GameUI.AddImage(UPC.Middle, imageId, null, button);

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
                        player.inventory.GetItemBehaviourChecked(itemIndex)?.ModifyInfo(ui);
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


    public class CraftingInfoShower
    {
        public Player player;
        public ImageIdentity background;
        public ScrollViewIdentity ingredientsView;
        public ImageIdentity arrow;
        public ScrollViewIdentity resultsView;
        public TextIdentity maximumCraftingTimesText;
        private readonly StringBuilder stringBuilder = new();

        public void Show(CraftingRecipe recipe, List<Dictionary<int, ushort>> ingredients)
        {
            stringBuilder.Clear();
            stringBuilder.AppendLine(GameUI.CompareText("可合成次数(TODO)").text.Replace("{value}", recipe.ingredients.Length.ToString()));


            Vector2 pos = GControls.cursorPosInMainCanvas;
            pos.x += background.sd.x * 0.75f;
            pos.y -= background.sd.y * 0.75f;


            background.ap = pos;
            maximumCraftingTimesText.text.text = $"<color=#E0E0E0>{stringBuilder}</color>";

            //显示原料
            ingredientsView.Clear();
            foreach (var ele in ingredients)
            {
                foreach (var ingredient in ele)
                {
                    Item itemGot = player.inventory.GetItem(ingredient.Key);

                    //图标
                    var ingredientsBackground = GameUI.AddImage(UPC.Middle, $"ori:image.crafting_info_shower.ingredients_background_{recipe.id}", "ori:item_tab");
                    var ingredientsIcon = GameUI.AddImage(UPC.Middle, $"ori:button.crafting_info_shower.ingredients_{ingredient.Key}", null, ingredientsBackground);
                    var ingredientsText = GameUI.AddText(UPC.Middle, $"ori:text.crafting_info_shower.ingredients_{recipe.id}", ingredientsBackground);

                    ingredientsIcon.SetSizeDelta(ingredientsView.gridLayoutGroup.cellSize);
                    ingredientsIcon.image.sprite = Item.Null(itemGot) ? null : itemGot.data.texture.sprite;

                    ingredientsText.autoCompareText = false;
                    ingredientsText.text.enableAutoSizing = true;
                    ingredientsText.text.fontSizeMin = 0;
                    ingredientsText.text.text = $"{GameUI.CompareText(itemGot.data.id)?.text}x{ingredient.Value}";
                    ingredientsText.text.margin = Vector4.zero;
                    ingredientsText.SetSizeDelta(ingredientsIcon.sd.x, 8);
                    ingredientsText.SetAPosY(ingredientsIcon.ap.y / 2 - ingredientsIcon.sd.y / 2 - ingredientsText.sd.y / 2);

                    ingredientsView.AddChild(ingredientsBackground);
                }
            }

            //显示结果
            resultsView.Clear();

            var iconBackground = GameUI.AddImage(UPC.Middle, $"ori:image.crafting_info_shower.result_background_{recipe.id}", "ori:item_tab");
            var icon = GameUI.AddImage(UPC.Middle, $"ori:image.crafting_info_shower.result_{recipe.id}", "ori:item_tab", iconBackground);
            var iconText = GameUI.AddText(UPC.Middle, $"ori:text.crafting_info_shower.result_{recipe.id}", iconBackground);

            icon.SetSizeDelta(resultsView.gridLayoutGroup.cellSize);
            icon.image.sprite = ModFactory.CompareItem(recipe.result.id).texture.sprite;

            iconText.autoCompareText = false;
            iconText.text.enableAutoSizing = true;
            iconText.text.fontSizeMin = 0;
            iconText.text.text = $"{GameUI.CompareText(recipe.result.id)?.text}x{recipe.result.count}";
            iconText.text.margin = Vector4.zero;
            iconText.SetSizeDelta(icon.sd.x, 8);
            iconText.SetAPosY(icon.ap.y / 2 - icon.sd.y / 2 - iconText.sd.y / 2);

            resultsView.AddChild(iconBackground);


            background.gameObject.SetActive(true);
        }


        public void Hide()
        {
            background.gameObject.SetActive(false);
        }






        public CraftingInfoShower(Player player, ImageIdentity background, ScrollViewIdentity ingredientsView, ImageIdentity arrow, ScrollViewIdentity resultsView, TextIdentity maximumCraftingTimesText)
        {
            this.player = player;
            this.background = background;
            this.ingredientsView = ingredientsView;
            this.arrow = arrow;
            this.resultsView = resultsView;
            this.maximumCraftingTimesText = maximumCraftingTimesText;
        }
    }

    //TODO
    public static class ItemInfoShower
    {
        private static ItemInfoUI uiInstance;
        private static readonly StringBuilder stringBuilder = new();

        public static ItemInfoUI GetUI()
        {
            if (uiInstance == null || !uiInstance.image || !uiInstance.nameText || !uiInstance.detailText)
            {
                int borderSize = 5;
                int detailTextFontSize = 15;

                ImageIdentity backgroundImage = GameUI.AddImage(UPC.Middle, "ori:image.item_info_shower", "ori:item_info_shower");
                TextIdentity nameText = GameUI.AddText(UPC.UpperLeft, "ori:text.item_info_shower.name", backgroundImage);
                //ImageIdentity damageIcon = GameUI.AddImage(UPC.UpperLeft, "ori:image.item_info_shower.damage_icon", "ori:item_info_shower_damage", backgroundImage);
                TextIdentity detailText = GameUI.AddText(UPC.UpperLeft, "ori:text.item_info_shower.detail", backgroundImage);

                backgroundImage.OnUpdate += x => GameUI.SetUILayerToTop(x);

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

        public static ItemInfoUI Show(Item item) => Show(item.data);

        public static ItemInfoUI Show(ItemData item)
        {
            ItemInfoUI ui = GetUI();
            Vector2 pos = GControls.cursorPosInMainCanvas;
            pos.x += ui.image.sd.x * 0.75f;
            pos.y -= ui.image.sd.y * 0.75f;

            ui.image.ap = pos;
            ui.nameText.text.text = GameUI.CompareText(item.id).text;   //$"{GameUI.CompareText(item.basic.id).text} <size=60%>({item.basic.id})";
            ui.detailText.text.text = $"<color=#E0E0E0>{GetDetailText(item, stringBuilder.Clear())}</color>";

            ui.image.gameObject.SetActive(true);
            return ui;
        }

        public static StringBuilder GetDetailText(Item item) => GetDetailText(item.data, stringBuilder);

        public static StringBuilder GetDetailText(ItemData item, StringBuilder sb)
        {
            sb.AppendLine(GameUI.CompareText("ori:item.damage").text.Replace("{value}", item.damage.ToString()));
            sb.AppendLine(GameUI.CompareText("ori:item.excavation_strength").text.Replace("{value}", item.excavationStrength.ToString()));
            sb.AppendLine(GameUI.CompareText("ori:item.use_cd").text.Replace("{value}", item.useCD.ToString()));
            sb.AppendLine(string.Empty);
            sb.Append(GameUI.CompareText(item.description).text);

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
                ImageIdentity image = GameUI.AddImage(UPC.Middle, "ori:image.item_dragger", "ori:square_button_flat");
                image.OnUpdate += i =>
                {
                    i.ap = GControls.cursorPosInMainCanvas;
                    GameUI.SetUILayerToTop(i);
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