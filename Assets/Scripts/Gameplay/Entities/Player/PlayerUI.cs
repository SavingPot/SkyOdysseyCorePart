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
        /*                                     金币                                     */
        /* -------------------------------------------------------------------------- */
        public TextImageIdentity coinTextImage;



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
        public List<CraftingViewButton> craftingViewButtonPool = new();

        public CraftingViewButton GenerateCraftingViewButton()
        {
            //添加按钮
            var button = GameUI.AddButton(UPC.Up, $"ori:button.player_crafting_recipe_{Tools.randomGUID}");
            button.image.sprite = ModFactory.CompareTexture("ori:item_tab").sprite;
            craftingView.AddChild(button);

            //物品图标
            var image = GameUI.AddImage(UPC.Middle, $"ori:image.player_crafting_recipe_{Tools.randomGUID}", "ori:item_tab", button);
            image.sd = craftingView.gridLayoutGroup.cellSize * 0.75f;

            //文本
            button.buttonText.rectTransform.SetAsLastSibling();
            button.buttonText.rectTransform.AddLocalPosY(-20);
            button.buttonText.SetSizeDelta(85, 27);
            button.buttonText.text.SetFontSize(10);
            button.buttonText.autoCompareText = false;

            //推入池中
            CraftingViewButton result = new(button, image);
            craftingViewButtonPool.Add(result);

            return result;
        }

        public class CraftingViewButton
        {
            public ButtonIdentity buttonIdentity;
            public ImageIdentity imageIdentity;

            public CraftingViewButton(ButtonIdentity buttonIdentity, ImageIdentity imageIdentity)
            {
                this.buttonIdentity = buttonIdentity;
                this.imageIdentity = imageIdentity;
            }
        }



        /* -------------------------------------------------------------------------- */
        /*                                     暂停                                     */
        /* -------------------------------------------------------------------------- */
        public BackpackPanel pausePanel;


        #endregion



        /* -------------------------------------------------------------------------- */
        /*                                     属性                                     */
        /* -------------------------------------------------------------------------- */
        public ImageIdentity hungerBarBg;
        public ImageIdentity happinessBarBg;
        public ImageIdentity healthBarBg;
        public ImageIdentity hungerBarFull;
        public ImageIdentity happinessBarFull;
        public ImageIdentity healthBarFull;



        /* -------------------------------------------------------------------------- */
        /*                                    任务系统                                    */
        /* -------------------------------------------------------------------------- */
        public BackpackPanel taskPanel;
        public NodeTree<TaskNode, TaskData> taskNodeTree;
        public ImageIdentity taskCompleteBackground;
        public ImageIdentity taskCompleteIcon;
        public TextIdentity taskCompleteText;
        public static List<TaskData> tasks = new()
        {
            new("ori:get_dirt", "ori:task.get_dirt", null, new[] { $"{BlockID.Dirt}/=/25/=/null" }),

            new("ori:craft", "ori:task.craft", "ori:get_dirt", null),

            new("ori:get_log", "ori:task.get_log", "ori:get_dirt", new[] { $"{BlockID.OakLog}/=/10/=/null" }),

            new("ori:get_meat", "ori:task.get_meat", "ori:get_dirt", null),
            new("ori:get_egg", "ori:task.get_egg", "ori:get_meat", null),
            new("ori:get_potato", "ori:task.get_potato", "ori:get_meat", null),
            new("ori:get_onion", "ori:task.get_onion", "ori:get_meat", null),
            new("ori:get_watermelon", "ori:task.get_watermelon", "ori:get_meat", null),

            new("ori:get_feather", "ori:task.get_feather", "ori:get_dirt", new[] { $"{ItemID.ChickenFeather}/=/5/=/null" }),
            new("ori:get_feather_wing", "ori:task.get_feather_wing", "ori:get_feather", null),

            new("ori:get_grass", "ori:task.get_grass", "ori:get_dirt", null),
            new("ori:get_straw_rope", "ori:task.get_straw_rope", "ori:get_grass", new[] { $"{ItemID.StrawRope}/=/3/=/null" }),
            new("ori:get_plant_fiber", "ori:task.get_plant_fiber", "ori:get_straw_rope", null),

            new("ori:get_gravel", "ori:task.get_gravel", "ori:get_dirt", new[] { $"{BlockID.Gravel}/=/3/=/null" }),
            new("ori:get_flint", "ori:task.get_flint", "ori:get_gravel", new[] { $"{ItemID.Flint}/=/2/=/null" }),
            new("ori:get_stone", "ori:task.get_stone", "ori:get_flint", new[] { $"{BlockID.Stone}/=/10/=/null" }),

            new("ori:get_planks", "ori:task.get_planks", "ori:get_log", new[] { $"{BlockID.OakPlanks}/=/10/=/null" }),
            new("ori:get_stick", "ori:task.get_stick", "ori:get_planks", new[] { $"{ItemID.Stick}/=/10/=/null" }),
            new("ori:get_campfire", "ori:task.get_campfire", "ori:get_stick", null),

            new("ori:get_flint_knife", "ori:task.get_flint_knife", "ori:get_stick", null),
            new("ori:get_flint_hoe", "ori:task.get_flint_hoe", "ori:get_stick", null),
            new("ori:get_flint_sword", "ori:task.get_flint_sword", "ori:get_stick", null),
            new("ori:get_iron_knife", "ori:task.get_iron_knife", "ori:get_flint_knife", null),
            new("ori:get_iron_hoe", "ori:task.get_iron_hoe", "ori:get_flint_hoe", null),
            new("ori:get_iron_sword", "ori:task.get_iron_sword", "ori:get_flint_sword", null),

            new("ori:get_bark", "ori:task.get_bark", "ori:get_log", new[] { $"{ItemID.Bark}/=/1/=/null" }),
            new("ori:get_bark_vest", "ori:task.get_bark_vest", "ori:get_bark", null),
        };




        /* -------------------------------------------------------------------------- */
        /*                                    技能系统                                    */
        /* -------------------------------------------------------------------------- */
        public List<SkillData> skills = new()
        {
            new("ori:run_faster", "ori:skill.run_faster", null,"ori:skill_description.run_faster",10)
        };
        public BackpackPanel skillPanel;
        public NodeTree<SkillNode, SkillData> skillNodeTree;



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
                        if (player.playerController.SkipDialog())
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
                while (!player.playerController.SkipDialog())
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
            while (!player.playerController.SkipDialog())
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
                            Map.instance.RecycleChunks();
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






            #region 触摸屏

            Vector2 touchScreenUniversalSize = new(100, 100);

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
                touchScreenAttackButton.sd = touchScreenUniversalSize;
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
                touchScreenUseItemButton.sd = touchScreenUniversalSize;
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
                touchScreenPlaceBlockUnderPlayerButton.sd = touchScreenUniversalSize;
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
                (inventoryItemPanel, inventoryItemView) = GenerateItemViewBackpackPanel(
                    "ori:inventory",
                    "ori:switch_button.inventory",
                    80,
                    Vector2.zero,
                    Vector2.zero,
                    () =>
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
                    });

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

                //设置背包面板
                SetBackpackPanel("ori:inventory");

                #endregion

                #region 合成

                //制作结果
                (craftingPanel, craftingView) = GenerateItemViewBackpackPanel(
                    "ori:crafting",
                    "ori:switch_button.crafting",
                    70,
                    Vector2.zero,
                    Vector2.zero,
                    () =>
                    {
                        //关闭原有的按钮
                        foreach (var item in craftingViewButtonPool)
                        {
                            if (item.buttonIdentity.gameObject.activeSelf)
                            {
                                item.buttonIdentity.gameObject.SetActive(false);
                            }
                        }

                        //获取所有配方
                        var recipes = ModFactory.GetAllCraftingRecipes();



                        //遍历每个配方
                        foreach (var recipe in recipes)
                        {
                            /* ----------------------------- 获取一个 ViewButton ---------------------------- */

                            CraftingViewButton viewButton = null;

                            foreach (var item in craftingViewButtonPool)
                            {
                                //如果是关闭的就采用
                                if (!item.buttonIdentity.gameObject.activeSelf)
                                {
                                    item.buttonIdentity.gameObject.SetActive(true);
                                    viewButton = item;
                                    break;
                                }
                            }

                            viewButton ??= GenerateCraftingViewButton();

                            /* ----------------------------------------------------------------------------- */

                            //获取结果
                            var itemGot = ModFactory.CompareItem(recipe.result.id);

                            //图标
                            viewButton.imageIdentity.image.sprite = itemGot.texture.sprite;

                            //按钮
                            viewButton.buttonIdentity.button.onClick.RemoveAllListeners();
                            viewButton.buttonIdentity.buttonText.text.text = $"{GameUI.CompareText(itemGot.id)?.text}x{recipe.result.count}";




                            //如果可以合成
                            if (recipe.WhetherCanBeCrafted(player.inventory.slots, out var ingredientTables))
                            {
                                //放到第一个
                                viewButton.buttonIdentity.transform.SetAsFirstSibling();

                                //图标颜色
                                viewButton.imageIdentity.image.SetColorBrightness(1);

                                //绑定方法
                                viewButton.buttonIdentity.button.OnPointerEnterAction = _ => craftingInfoShower.Show(recipe, ingredientTables);
                                viewButton.buttonIdentity.button.OnPointerExitAction = _ => craftingInfoShower.Hide();
                                viewButton.buttonIdentity.button.onClick.AddListener(() =>
                                {
                                    var resultItem = ModConvert.ItemDataToItem(ModFactory.CompareItem(recipe.result.id));
                                    resultItem.count = recipe.result.count;

                                    //检查背包空间
                                    if (!Inventory.GetIndexesToPutItemIntoItems(player.inventory.slots, resultItem, out var _))
                                    {
                                        InternalUIAdder.instance.SetStatusText("背包栏位不够了，请清理背包后再合成");
                                        return;
                                    }

                                    //从玩家身上减去物品
                                    ingredientTables.For(ingredientToRemove =>
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
                            }
                            //如果不能合成
                            else
                            {
                                //放到最后
                                viewButton.buttonIdentity.transform.SetAsLastSibling();

                                //图标颜色
                                viewButton.imageIdentity.image.SetColorBrightness(0.3f, 0.8f);

                                //绑定方法
                                viewButton.buttonIdentity.button.OnPointerEnterAction = _ => { };
                                viewButton.buttonIdentity.button.OnPointerExitAction = _ => { };
                            }
                        }

                    });

                #region 生成 craftingInfoShower
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
                #endregion

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

                /* -------------------------------- 生成任务完成图像 -------------------------------- */
                taskCompleteBackground = GameUI.AddImage(UPC.UpperLeft, "ori:image.task_complete_background", "ori:task_complete");
                taskCompleteBackground.SetSizeDelta(320, 100);
                taskCompleteBackground.SetAPos(taskCompleteBackground.sd.x / 2, -taskCompleteBackground.sd.y / 2);
                taskCompleteBackground.gameObject.SetActive(false);
                taskCompleteBackground.OnUpdate += _ =>
                {
                    GameUI.SetUILayerToFirst(taskCompleteBackground);
                };

                taskCompleteIcon = GameUI.AddImage(UPC.Left, "ori:image.task_complete_icon", null, taskCompleteBackground);
                taskCompleteIcon.SetSizeDelta(taskCompleteBackground.sd.y, taskCompleteBackground.sd.y);
                taskCompleteIcon.SetAPosX(taskCompleteIcon.sd.x / 2);

                taskCompleteText = GameUI.AddText(UPC.Middle, "ori:text.task_complete", taskCompleteBackground);
                taskCompleteText.sd = taskCompleteBackground.sd;
                taskCompleteText.text.margin = new(taskCompleteIcon.sd.x + 5, 5, 5, 5);
                taskCompleteText.autoCompareText = false;

                /* ----------------------------------- 生成任务视图 ----------------------------------- */
                taskPanel = GenerateBackpackPanel("ori:tasks", "ori:switch_button.tasks");

                taskNodeTree = new(
                    "task_tree",
                    tasks,
                    taskPanel.rectTransform,
                    node => (node.status.completed, node.status.hasGotRewards) switch
                    {
                        (true, true) => Color.white,  //完成了且领取了奖励
                        (true, false) => Tools.HexToColor("#00FFD6"),  //完成了但没领取奖励
                        (false, _) => new(0.5f, 0.5f, 0.5f, 0.75f)  //没完成
                    },
                    node => TaskInfoShower.Show(node),
                    _ => TaskInfoShower.Hide(),
                    node =>
                    {
                        if (!node.status.completed || node.status.hasGotRewards)
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

                        node.status.hasGotRewards = true;
                        taskNodeTree.RefreshNodes(false);
                    }
                );

                /* --------------------------------- 加载已有任务 --------------------------------- */
                foreach (var task in player.completedTasks)
                {
                    CompleteTask(task.id, false, task.hasGotRewards);
                }

                #endregion

                #region 技能树

                skillPanel = GenerateBackpackPanel("ori:skills", "ori:switch_button.skills");
                skillNodeTree = new(
                    "skill_tree",
                    skills,
                    skillPanel.rectTransform,
                    node => node.status.unlocked ? Color.white : new(0.5f, 0.5f, 0.5f, 0.75f),
                    node => SkillInfoShower.Show(node),
                    _ => SkillInfoShower.Hide(),
                    node =>
                    {
                        if (!node.status.unlocked && player.coin > node.data.cost)
                        {
                            player.coin -= node.data.cost;
                            node.status.unlocked = true;
                            skillNodeTree.RefreshNodes(false);
                        }
                    }
                );

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
                SetIt(happinessBarBg, happinessBarFull, xExtraOffset, yExtraOffset * 2);

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

            #region 金币
            {
                coinTextImage = GameUI.AddTextImage(UPC.LowerLeft, "ori:text_image.coin", "ori:coin");
                coinTextImage.SetSizeDeltaBoth(70, 70);
                coinTextImage.ap = coinTextImage.sd / 2;
                coinTextImage.OnUpdate += _ => coinTextImage.SetText(player.coin);
                coinTextImage.SetTextAttach(TextImageIdentity.TextAttach.Right);
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

        public void RefreshCurrentBackpackPanel() => RefreshBackpackPanel(currentBackpackPanel);

        public void RefreshBackpackPanel(string id)
        {
            foreach (var item in backpackPanels)
            {
                if (item.id == id)
                {
                    item.Refresh?.Invoke();
                    return;
                }
            }

            Debug.LogError($"刷新背包界面 {id} 失败：不存在该背包界面");
        }

        public BackpackPanel GenerateBackpackPanel(
            string id,
            string switchButtonTexture,
            Action Refresh = null,
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

            BackpackPanel result = new(id, panel, switchButtonBackground, switchButton, Refresh, ActualActivate, ActualDeactivate);
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
            Action Refresh = null,
            Action OnActivate = null,
            Action OnDeactivate = null,
            string texture = "ori:backpack_inventory_background")
        {
            (var modId, var panelName) = Tools.SplitModIdAndName(id);

            var panel = GenerateBackpackPanel(id, switchButtonTexture, Refresh, OnActivate, OnDeactivate, texture);
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
                    item.Refresh?.Invoke();
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

                touchScreenUseItemButtonImage.image.sprite = player.GetUsingItemChecked()?.data?.texture?.sprite;
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
            hungerBarFull.image.fillAmount = player.hungerValue / Player.maxHungerValue;
            happinessBarFull.image.fillAmount = player.happinessValue / Player.maxHappinessValue;
            healthBarFull.image.fillAmount = (float)player.health / player.maxHealth;
        }






        #region 任务系统

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
            if (CompleteTask_Internal(taskNodeTree.rootNode, id, out bool hasCompletedBefore, out TaskNode nodeCompleted) && !hasCompletedBefore && nodeCompleted != null)
            {
                if (hasGotRewards)
                {
                    nodeCompleted.status.hasGotRewards = true;
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

            taskNodeTree.RefreshNodes(false);
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
                if (CompleteTask_Internal((TaskNode)node, id, out hasCompletedBefore, out nodeCompleted))
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
                    hasCompletedBefore = node.status.completed;

                    if (!player.completedTasks.Any(p => p.id == id))
                    {
                        player.AddCompletedTasks(new() { id = id, completed = true, hasGotRewards = node.status.hasGotRewards });
                    };

                    node.status.completed = true;
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

        public class TaskData : TreeNodeData
        {
            public string[] rewards;

            public TaskData(string id, string icon, string parent, string[] rewards) : base(id, icon, parent)
            {
                this.rewards = rewards;
            }
        }

        public class TaskNode : TreeNode<TaskData>
        {
            public TaskStatus status = new();

            public TaskNode(TaskData data) : base(data)
            {
                if (data.rewards == null || data.rewards.Length == 0)
                    status.hasGotRewards = true;
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

        #endregion





        #region 技能系统


        public class SkillInfoShower
        {
            public class SkillInfoUI
            {
                public ImageIdentity image;
                public TextIdentity nameText;
                public TextIdentity detailText;

                public SkillInfoUI(ImageIdentity image, TextIdentity nameText, TextIdentity detailText)
                {
                    this.image = image;
                    this.nameText = nameText;
                    this.detailText = detailText;
                }
            }

            private static SkillInfoUI uiInstance;

            public static SkillInfoUI GetUI()
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

            public static void Show(SkillNode skill)
            {
                SkillInfoUI ui = GetUI();
                ui.image.transform.SetParent(skill.button.transform);
                Vector2 pos = Vector2.zero;
                pos.x += ui.image.sd.x;
                pos.y -= ui.image.sd.y;

                ui.image.ap = pos;
                ui.nameText.text.text = skill.button.buttonText.text.text;
                ui.detailText.text.text = GameUI.CompareText(skill.data.description).text;

                ui.image.gameObject.SetActive(true);
            }

            public static void Hide()
            {
                SkillInfoUI ui = GetUI();
                ui.image.gameObject.SetActive(false);
            }
        }


        [Serializable]
        public class SkillStatus
        {
            public bool unlocked;
        }

        [Serializable]
        public class SkillStatusForSave : SkillStatus
        {
            public string id;
        }

        public class SkillData : TreeNodeData
        {
            public string description;
            public int cost;

            public SkillData(string id, string icon, string parent, string description, int cost) : base(id, icon, parent)
            {
                this.description = description;
                this.cost = cost;
            }
        }

        public class SkillNode : TreeNode<SkillData>
        {
            public SkillStatus status = new();

            public SkillNode(SkillData data) : base(data)
            {

            }
        }

        #endregion










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
        public Action Refresh;
        public Action Activate;
        public Action Deactivate;

        public BackpackPanel(string id, PanelIdentity panel, ImageIdentity switchButtonBackground, ButtonIdentity switchButton, Action Refresh, Action Activate, Action Deactivate)
        {
            this.id = id;
            this.panel = panel;
            this.switchButtonBackground = switchButtonBackground;
            this.switchButton = switchButton;
            this.Refresh = Refresh;
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

    //TODO: 把 InfoShower 打包为一个 abstract class，然后再派生出各种具体的实现类，这样可以更灵活快捷地使用
    //TODO: 注意要划分为两种 InfoShower：一种是名字文本+信息文本，另一种是只有一个文本
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

            //如果成功匹配到了描述文本
            if (GameUI.TryCompareTextNullable(item.description, out var description))
                sb.Append(description.text);

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

        public static void SwapDraggingAndOldDragger(Item itemToDrag, Action<Item> placement, Action cancel, Func<Item, bool> replacementCondition)
        {
            //* 注意！
            //* 这里的具体场景是: 我先拽了一个物品，然后又点了另一个物品
            //* previousItem 就是被拽的物品, itemToDrag 就是被点的物品

            var previousDragger = ItemDragger.draggingItem;
            var previousItem = previousDragger.item;

            /* ------------------------------- 如果物品不同 ———— 直接交换 ------------------------------- */
            if (Item.Null(previousItem) || Item.Null(itemToDrag) || !Item.Same(previousItem, itemToDrag))
            {
                //检查可不可以交换
                if (replacementCondition(previousItem) && previousDragger.replacementCondition(itemToDrag))
                {
                    //交换物品
                    previousDragger.placement(itemToDrag);
                    placement(previousItem);
                }

                //取消拖拽
                CancelDragging();
            }
            /* ------------------------------- 如果物品相同 & 数量未满 ———— 合并 ------------------------------- */
            else if (itemToDrag.count < itemToDrag.data.maxCount)
            {
                //TODO
                //如果可以数量直接添加
                if (previousItem.count + itemToDrag.count <= itemToDrag.data.maxCount)
                {
                    //增加数量
                    itemToDrag.count += previousItem.count;

                    //替换物品
                    placement(itemToDrag);
                    previousDragger.placement(null);

                    //取消拖拽
                    CancelDragging();
                }
                //如果数量过多
                else
                {
                    //计算出要执行的数量
                    ushort countToExe = (ushort)Mathf.Min(previousItem.count, itemToDrag.data.maxCount - itemToDrag.count);

                    previousItem.count -= countToExe;
                    itemToDrag.count += countToExe;

                    placement(previousItem);
                    previousDragger.placement(itemToDrag);
                }
            }
        }
    }
}