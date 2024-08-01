using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameCore.Converters;
using GameCore.Network;
using GameCore;
using SP.Tools.Unity;
using SP.Tools;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine;

namespace GameCore.UI
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
        private readonly StringBuilder chatStringBuilder = new();



        public static void AddChatMsg(Sprite portrait, string playerName, string msg)
        {
            if (instance == null || !instance.chatView)
                return;

            /* ------------------------ 添加文本, 设置向左对其, 不能溢出, 设置内容 ------------------------ */
            var text = GameUI.AddText(UIA.Middle, $"ori:text.chat.{msg}");
            text.autoCompareText = false;
            text.text.alignment = TMPro.TextAlignmentOptions.Left;
            text.text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            text.text.text = $"{playerName}: {msg}";

            var image = GameUI.AddImage(UIA.Left, $"ori:image.chat.{msg}", null, text);
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
        public PanelIdentity respawnPanel;
        public TextIdentity respawnPanelText;
        public TextIdentity respawnTimerText;
        public ButtonIdentity respawnButton;



        public async void ShowRespawnPanel()
        {
            await 3;

            respawnPanel.panelImage.color = new(0, 0, 0, 0);
            respawnButton.buttonText.text.color = new(1, 1, 1, 0);
            respawnButton.button.image.color = new(1, 1, 1, 0);
            respawnButton.button.interactable = false;
            GameUI.FadeIn(respawnPanel.panelImage);

            await 3;
            respawnButton.button.interactable = true;
            GameUI.FadeIn(respawnButton.image);
            GameUI.FadeIn(respawnButton.buttonText.text);
        }



        #region 背包界面

        public static int backpackPanelWidth = 700;
        public static int backpackPanelHeight = 450;
        public List<BackpackPanel> backpackPanels = new();
        public BackpackPanel currentBackpackPanel = null;
        public string currentBackpackPanelId = string.Empty;
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
        public InventorySlotUI inventoryShieldUI;
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
            var button = GameUI.AddButton(UIA.Up, $"ori:button.player_crafting_recipe_{Tools.randomGUID}");
            button.image.sprite = ModFactory.CompareTexture("ori:item_tab").sprite;
            craftingView.AddChild(button);

            //物品图标
            var image = GameUI.AddImage(UIA.Middle, $"ori:image.player_crafting_recipe_{Tools.randomGUID}", "ori:item_tab", button);
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
        public ImageIdentity manaBarBg;
        public ImageIdentity manaBarFull;
        public ImageIdentity manaBarEffect;
        public ImageIdentity healthBarBg;
        public ImageIdentity healthBarFull;
        public ImageIdentity healthBarEffect;



        /* -------------------------------------------------------------------------- */
        /*                                    任务系统                                    */
        /* -------------------------------------------------------------------------- */
        public BackpackPanel taskPanel;
        public NodeTree<TaskNode, TaskData> taskNodeTree;
        public ImageIdentity taskCompleteBackground;
        public ImageIdentity taskCompleteIcon;
        public TextIdentity taskCompleteText;
        public static List<TaskData> tasks { get; internal set; }




        /* -------------------------------------------------------------------------- */
        /*                                    技能系统                                    */
        /* -------------------------------------------------------------------------- */
        public static List<SkillData> skills { get; internal set; }
        public BackpackPanel skillPanel;
        public NodeTree<SkillNode, SkillData> skillNodeTree;
        public Action<SkillData> OnUnlockSkill = _ => { };



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
                var fullContent = current.text;
                var fullContentChars = fullContent.ToCharArray();

                //更改对话者的头像和名字
                dialogHead.image.sprite = ModFactory.CompareTexture(current.head).sprite;
                dialogNameText.text.text = GameUI.CompareText(displayingDialog.name);

                //遍历文本
                for (int t = 0; t < fullContent.Length;)
                {
                    var currentLetter = fullContent[t];
                    var charsAfterLetter = new ArraySegment<char>(fullContentChars, t, fullContent.Length - t).ToArray(); //? 包括当前的字和之后所有的字
                    var strAfterItem = new string(charsAfterLetter);

                    string output;
                    int tDelta;

                    //如果是富文本，要立刻输出好整段富文本
                    if (dialogRichTextSupported.Any(p => strAfterItem.StartsWith(p)))
                    {
                        var endIndex = strAfterItem.IndexOf('>') + 1; //? 如果不 +1, 富文本会瞬间闪烁然后消失 

                        tDelta = endIndex;
                        output = new string(new ArraySegment<char>(charsAfterLetter, 0, endIndex).ToArray());
                    }
                    //如果不是富文本，就正常的一个字一个字输出
                    else
                    {
                        output = currentLetter.ToString();
                        tDelta = 1;
                    }

                    //输出文本
                    dialogText.text.text += output;


                    //判断是否达到字输出的等待时间
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








        /* -------------------------------------------------------------------------- */
        /*                                    获取珍惜物品                                    */
        /* -------------------------------------------------------------------------- */
        public ButtonIdentity gainRareItemButtonPanel;
        public ImageIdentity gainRareItemIcon;
        public TextIdentity gainRareItemNameText;
        public TextIdentity gainRareItemDescriptionText;
        public Queue<Item> gainRareItemQueue = new();

        public void ShowGainRareItemUI(string itemId)
        {
            var item = ModFactory.CompareItem(itemId);

            if (item == null)
            {
                Debug.LogWarning($"找不到物品 {itemId}");
                return;
            }

            ShowGainRareItemUI(item);
        }
        public void ShowGainRareItemUI(ItemData item) => ShowGainRareItemUI(item.DataToItem());
        public void ShowGainRareItemUI(Item item)
        {
            if (GameUI.page.ui == gainRareItemButtonPanel)
            {
                gainRareItemQueue.Enqueue(item);
                return;
            }

            gainRareItemIcon.SetSprite(item.data.texture.sprite);
            gainRareItemNameText.SetText($"{GameUI.CompareText(item.data.id)}{(item.count == 0 ? "" : $" x{item.count}")}");
            gainRareItemDescriptionText.SetText(GameUI.CompareTextNullable(item.data.description));

            gainRareItemButtonPanel.ap = Vector2.zero;
            GameUI.SetPage(gainRareItemButtonPanel, GameUI.DisappearType.PositionDownToUp, GameUI.AppearType.PositionDownToUp);
        }

        void CheckGainRareItemQueue()
        {
            //珍惜物品界面关闭且队列不为空, 则尝试弹出队列的第一个物品
            if (!gainRareItemButtonPanel.gameObject.activeInHierarchy && gainRareItemQueue.TryDequeue(out var item))
            {
                ShowGainRareItemUI(item);
            }
        }







        /* -------------------------------------------------------------------------- */
        /*                                    实体锁定                                    */
        /* -------------------------------------------------------------------------- */
        public ImageIdentity enemyLockOnMark;

        public void LockOnEnemy(Entity enemy)
        {
            if (enemy == null)
            {
                enemyLockOnMark.transform.SetParent(null);
                return;
            }

            var startY = 35;
            var shakeY = 8;
            enemyLockOnMark.rt.SetParentForUI(enemy.usingCanvas.transform);
            enemyLockOnMark.transform.localScale = Vector3.one;
            enemyLockOnMark.SetAPosY(startY);
            enemyLockOnMark.SetSizeDelta(30, 30);

            //让指针上下移动
            DOTween.Sequence().Append(enemyLockOnMark.rt.DOLocalMoveY(startY + shakeY, 0.5f))
                              .Append(enemyLockOnMark.rt.DOLocalMoveY(startY - shakeY, 0.5f))
                              .Append(enemyLockOnMark.rt.DOLocalMoveY(startY + shakeY, 0.5f))
                              .Append(enemyLockOnMark.rt.DOLocalMoveY(startY, 0.5f))
                              .SetLoops(-1)
                              .Play();
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
            chatView = GameUI.AddScrollView(UIA.StretchDouble, "ori:view.chat");
            chatView.scrollViewImage.color = new(0.2f, 0.2f, 0.2f, 0.6f);
            chatView.rt.sizeDelta = Vector2.zero;
            chatView.gameObject.SetActive(false);

            chatInput = GameUI.AddInputButton(UIA.Down, "ori:input_button.chat", chatView);
            chatInput.field.image.color = new(1, 1, 1, 0.8f);
            chatInput.button.image.color = new(1, 1, 1, 0.8f);
            chatInput.OnClickBind(() =>
            {
                if (!Player.local)
                    return;

                //处理屏蔽词
                StringTools.ModifyObscenities(chatStringBuilder.Clear().Append(chatInput.field.field.text), "*");
                var messageContent = chatStringBuilder.ToString();

                //将消息发送给服务器
                Client.Send<NMChat>(new(
                    ByteConverter.ToBytes(Player.local.head.sr.sprite),
                    Player.local.playerName,
                    messageContent));
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
                touchScreenCursorImage = GameUI.AddImage(UIA.Middle, "ori:image.player_cursor", "ori:player_cursor", GameUI.worldSpaceCanvas.gameObject);
                touchScreenCursorImage.rt.sizeDelta = Vector2.one;
            }

            /* -------------------------------------------------------------------------- */
            /*                                    添加摇杆                                    */
            /* -------------------------------------------------------------------------- */
            {
                touchScreenMoveJoystick = Joystick.Create("PlayerMoveJoystick", "ori:image.player_move_joystick_background", "ori:image.player_move_joystick_handle");

                touchScreenCursorJoystick = Joystick.Create("PlayerCursorJoystick", "ori:image.player_cursor_joystick_background", "ori:image.player_cursor_joystick_handle");
                touchScreenCursorJoystick.SetAnchorMinMax(UIA.LowerRight);
                touchScreenCursorJoystick.SetAPos(-touchScreenMoveJoystick.rectTransform.anchoredPosition.x, touchScreenMoveJoystick.rectTransform.anchoredPosition.y);
            }

            /* -------------------------------------------------------------------------- */
            /*                                     攻击                                     */
            /* -------------------------------------------------------------------------- */
            {
                touchScreenAttackButton = GameUI.AddButton(UIA.LowerRight, "ori:button.player_attack", GameUI.canvas.transform, "ori:player_attack_button");
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
                touchScreenUseItemButton = GameUI.AddButton(UIA.LowerRight, "ori:button.player_use_item", GameUI.canvas.transform, "ori:player_use_item_button");
                Component.Destroy(touchScreenUseItemButton.buttonText.gameObject);
                touchScreenUseItemButton.sd = touchScreenUniversalSize;
                touchScreenUseItemButton.SetAPosOnBySizeDown(touchScreenAttackButton, 50);
                touchScreenUseItemButton.button.HideClickAction();
                touchScreenUseItemButton.button.onClick.RemoveAllListeners();

                touchScreenUseItemButtonImage = GameUI.AddImage(UIA.Middle, "ori:image.player_use_item_icon", null, touchScreenUseItemButton);
                touchScreenUseItemButtonImage.sd = touchScreenUseItemButton.sd * 0.5f;
            }

            /* -------------------------------------------------------------------------- */
            /*                                     在脚下放方块                                     */
            /* -------------------------------------------------------------------------- */
            {
                touchScreenPlaceBlockUnderPlayerButton = GameUI.AddButton(UIA.LowerRight, "ori:button.player_place_block_under_player", GameUI.canvas.transform, "ori:player_place_block_under_player_button");
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
                touchScreenPauseButton = GameUI.AddButton(UIA.UpperRight, "ori:button.player_pause", GameUI.canvas.transform, "ori:player_pause_button");
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
                touchScreenCraftingButton = GameUI.AddButton(UIA.UpperRight, "ori:button.player_crafting", GameUI.canvas.transform, "ori:player_crafting_button");
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
                touchScreenShowTaskButton = GameUI.AddButton(UIA.UpperRight, "ori:button.player_show_task", GameUI.canvas.transform, "ori:player_show_task_button");
                touchScreenShowTaskButton.buttonText.gameObject.SetActive(false);
                touchScreenShowTaskButton.image.rectTransform.sizeDelta = new(75, 75);
                touchScreenShowTaskButton.SetAPosOnBySizeDown(touchScreenCraftingButton, 20);
                touchScreenShowTaskButton.button.HideClickAction();
                touchScreenShowTaskButton.button.onClick.RemoveAllListeners();
                touchScreenShowTaskButton.OnClickBind(() =>
                {
                    if (backpackMask && GameUI.page?.ui != dialogPanel)
                        ShowOrHideBackpackAndSetPanelToTasks();
                });
            }

            #endregion


            /* -------------------------------------------------------------------------- */
            /*                                    锁定敌人                                    */
            /* -------------------------------------------------------------------------- */
            enemyLockOnMark = GameUI.AddImage(UIA.Middle, "ori:image.player_lock_on_enemy_mark", "ori:enemy_lock_on_mark");
            enemyLockOnMark.transform.SetParent(null);


            /* -------------------------------------------------------------------------- */
            /*                                    对话                                    */
            /* -------------------------------------------------------------------------- */
            {
                dialogPanel = GameUI.AddButton(new(0, 0, 1, 0.4f), "ori:panel.dialog");
                dialogPanel.gameObject.SetActive(false);
                dialogPanel.image.sprite = null;
                dialogPanel.image.SetColor(0.1f, 0.1f, 0.1f, 0.6f);
                dialogPanel.button.HideClickAction();
                dialogPanel.button.onClick.RemoveAllListeners();
                dialogPanel.sd = Vector2.zero;
                GameObject.Destroy(dialogPanel.buttonText.gameObject);

                dialogHead = GameUI.AddImage(UIA.UpperLeft, "ori:image.dialog_head", null, dialogPanel);
                dialogHead.SetSizeDelta(160, 160);
                dialogHead.ap = new(dialogHead.sd.x / 2, -dialogHead.sd.y / 2);

                dialogNameText = GameUI.AddText(UIA.Down, "ori:text.dialog_name", dialogHead);
                dialogNameText.SetAPosY(-dialogNameText.sd.y / 2 - 10);
                dialogNameText.doRefresh = false;

                dialogText = GameUI.AddText(UIA.Right, "ori:text.dialog", dialogHead);
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
                    var button = GameUI.AddButton(UIA.Down, "ori:button.item_tab_" + indexAs0, GameUI.canvas.transform, "ori:item_tab");
                    var item = GameUI.AddImage(UIA.Down, "ori:image.item_tab_item_" + indexAs0, "ori:item_tab", button);

                    item.rectTransform.SetParentForUI(button.rectTransform);
                    button.rectTransform.sizeDelta = vecButtonSize;
                    button.rectTransform.AddLocalPosX((i + 0.5f) * buttonSize);
                    button.rectTransform.AddLocalPosY(buttonExtraY);
                    item.rectTransform.sizeDelta = vecButtonItemSize;

                    //Destroy(button.buttonText.gameObject);
                    button.buttonText.rectTransform.anchorMin = UIA.Down;
                    button.buttonText.rectTransform.anchorMax = UIA.Down;
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

                backpackPanelBackground = GameUI.AddImage(UIA.Middle, "ori:image.backpack_panel_background", null, backpackMask);
                backpackPanelBackground.SetSizeDelta(backpackPanelWidth, backpackPanelHeight);

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
                        inventoryShieldUI.Refresh(player, Inventory.shieldVar, item => Item.Null(item) || item.data.Shield != null);
                    });

                for (int i = 0; i < inventorySlotsUIs.Length; i++)
                {
                    int index = i;
                    var ui = new InventorySlotUI($"ori:button.backpack_inventory_item_{index}", $"ori:image.backpack_inventory_item_{index}", inventoryItemView.gridLayoutGroup.cellSize);

                    inventorySlotsUIs[i] = ui;
                    inventoryItemView.AddChild(ui.button);
                }

                inventoryHelmetUI = new($"ori:button.backpack_inventory_item_{Inventory.helmetVar}", $"ori:image.backpack_inventory_item_{Inventory.helmetVar}", inventoryItemView.gridLayoutGroup.cellSize);
                inventoryBreastplateUI = new($"ori:button.backpack_inventory_item_{Inventory.breastplateVar}", $"ori:image.backpack_inventory_item_{Inventory.breastplateVar}", inventoryItemView.gridLayoutGroup.cellSize);
                inventoryLeggingUI = new($"ori:button.backpack_inventory_item_{Inventory.leggingVar}", $"ori:image.backpack_inventory_item_{Inventory.leggingVar}", inventoryItemView.gridLayoutGroup.cellSize);
                inventoryBootsUI = new($"ori:button.backpack_inventory_item_{Inventory.bootsVar}", $"ori:image.backpack_inventory_item_{Inventory.bootsVar}", inventoryItemView.gridLayoutGroup.cellSize);
                inventoryShieldUI = new($"ori:button.backpack_inventory_item_{Inventory.shieldVar}", $"ori:image.backpack_inventory_item_{Inventory.shieldVar}", inventoryItemView.gridLayoutGroup.cellSize);

                inventoryHelmetUI.button.transform.SetParent(inventoryItemView.gridLayoutGroup.transform.parent);
                inventoryBreastplateUI.button.transform.SetParent(inventoryItemView.gridLayoutGroup.transform.parent);
                inventoryLeggingUI.button.transform.SetParent(inventoryItemView.gridLayoutGroup.transform.parent);
                inventoryBootsUI.button.transform.SetParent(inventoryItemView.gridLayoutGroup.transform.parent);
                inventoryShieldUI.button.transform.SetParent(inventoryItemView.gridLayoutGroup.transform.parent);

                inventoryHelmetUI.button.SetAnchorMinMax(UIA.LowerLeft);
                inventoryBreastplateUI.button.SetAnchorMinMax(UIA.LowerLeft);
                inventoryLeggingUI.button.SetAnchorMinMax(UIA.LowerLeft);
                inventoryBootsUI.button.SetAnchorMinMax(UIA.LowerLeft);
                inventoryShieldUI.button.SetAnchorMinMax(UIA.LowerLeft);

                inventoryHelmetUI.button.ap = inventoryHelmetUI.button.sd / 2;
                inventoryBreastplateUI.button.SetAPosOnBySizeRight(inventoryHelmetUI.button, 0);
                inventoryLeggingUI.button.SetAPosOnBySizeRight(inventoryBreastplateUI.button, 0);
                inventoryBootsUI.button.SetAPosOnBySizeRight(inventoryLeggingUI.button, 0);
                inventoryShieldUI.button.SetAPosOnBySizeRight(inventoryBootsUI.button, 10);

                //设置背包面板为物品栏
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
                            //如果配方不能被合成
                            if (!recipe.IsEligibleFor(player))
                                continue;

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
                            viewButton.buttonIdentity.buttonText.text.text = $"{GameUI.CompareText(itemGot.id)}x{recipe.result.count}";




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
                                    //获取结果并设置数量
                                    var resultItem = ModFactory.CompareItem(recipe.result.id).DataToItem().SetCount(recipe.result.count);

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

                                    //音效
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

                    ImageIdentity backgroundImage = GameUI.AddImage(UIA.Middle, "ori:image.crafting_info_shower_background", "ori:crafting_info_shower_background");
                    ScrollViewIdentity ingredientsView = GameUI.AddScrollView(UIA.Up, "ori:scrollview.crafting_info_shower_ingredients", backgroundImage);
                    ImageIdentity arrow = GameUI.AddImage(UIA.Up, "ori:image.crafting_info_shower_arrow", "ori:crafting_info_shower_arrow", backgroundImage);
                    ScrollViewIdentity resultsView = GameUI.AddScrollView(UIA.Up, "ori:scrollview.crafting_info_shower_results", backgroundImage);
                    TextIdentity maximumCraftingTimesText = GameUI.AddText(UIA.LowerRight, "ori:text.crafting_info_shower.maximum_crafting_times", backgroundImage);

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

                ButtonIdentity continueGame = GameUI.AddButton(UIA.Middle, "ori:button.pause_continue_game", pausePanel.panel).OnClickBind(() =>
                {
                    GameUI.SetPage(null);
                });
                ButtonIdentity quitGame = GameUI.AddButton(UIA.Middle, "ori:button.pause_quit_game", pausePanel.panel).OnClickBind(GM.instance.LeftGame);

                continueGame.rt.AddLocalPosY(30);
                quitGame.rt.AddLocalPosY(-30);

                #endregion

                #region 任务系统

                /* -------------------------------- 生成任务完成图像 -------------------------------- */
                taskCompleteBackground = GameUI.AddImage(UIA.UpperLeft, "ori:image.task_complete_background", "ori:task_complete");
                taskCompleteBackground.SetSizeDelta(320, 100);
                taskCompleteBackground.SetAPos(taskCompleteBackground.sd.x / 2, -taskCompleteBackground.sd.y / 2);
                taskCompleteBackground.gameObject.SetActive(false);
                taskCompleteBackground.OnUpdate += _ =>
                {
                    GameUI.SetUILayerToFirst(taskCompleteBackground);
                };

                taskCompleteIcon = GameUI.AddImage(UIA.Left, "ori:image.task_complete_icon", null, taskCompleteBackground);
                taskCompleteIcon.SetSizeDelta(taskCompleteBackground.sd.y, taskCompleteBackground.sd.y);
                taskCompleteIcon.SetAPosX(taskCompleteIcon.sd.x / 2);

                taskCompleteText = GameUI.AddText(UIA.Middle, "ori:text.task_complete", taskCompleteBackground);
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
                    node => TaskInfoShower.instance.Show(node),
                    _ => TaskInfoShower.instance.Hide(),
                    node =>
                    {
                        if (!node.status.completed || node.status.hasGotRewards)
                            return;

                        //给予技能点
                        if (node.data.skillPointReward != 0)
                        {
                            player.ServerAddSkillPoint(node.data.skillPointReward);
                        }

                        //给予物品奖励
                        if (node.data.itemRewards != null)
                        {
                            foreach (var reward in node.data.itemRewards)
                            {
                                /* ---------------------------------- 切割字符串 --------------------------------- */
                                if (Drop.ConvertStringItem(reward, out string id, out ushort count, out string customData, out string error))
                                {
                                    /* ---------------------------------- 给予物品 ---------------------------------- */
                                    ItemData item = ModFactory.CompareItem(id);

                                    if (item == null)
                                    {
                                        Debug.LogError(error);
                                        continue;
                                    }
                                    
                                    var extended = item.DataToItem();
                                    extended.count = count;
                                    if (!customData.IsNullOrWhiteSpace()) extended.customData = JsonUtils.LoadJObjectByString(customData);
                                    player.ServerAddItem(extended);
                                }
                                else
                                {
                                    Debug.LogError(error);
                                }
                            }
                        }

                        //刷新玩家属性
                        var completedTasksTemp = player.completedTasks;
                        foreach (var completed in completedTasksTemp)
                        {
                            if (completed.id == node.data.id)
                            {
                                completed.hasGotRewards = true;
                            }
                        }
                        player.completedTasks = completedTasksTemp;

                        //刷新节点显示
                        node.status.hasGotRewards = true;
                        taskNodeTree.RefreshNodes(false);
                    }
                );



                /* --------------------------------- 加载已有任务 --------------------------------- */
                for (int i = player.completedTasks.Count - 1; i >= 0; i--)
                {
                    var task = player.completedTasks[i];
                    var node = taskNodeTree.FindTreeNode(task.id);

                    if (node == null)
                    {
                        player.completedTasks.RemoveAt(i);
                        Debug.LogError($"任务 {task.id} 未找到对应的节点，已删除");
                        continue;
                    }

                    node.status.completed = task.completed;
                    node.status.hasGotRewards = task.hasGotRewards;
                }

                //刷新节点显示
                taskNodeTree.RefreshNodes(false);

                #endregion

                #region 技能树

                /* --------------------------------- 加载已有任务 --------------------------------- */

                skillPanel = GenerateBackpackPanel("ori:skills", "ori:switch_button.skills");
                skillNodeTree = new(
                    "skill_tree",
                    skills,
                    skillPanel.rectTransform,
                    node => node.status.unlocked ? Color.white : new(0.5f, 0.5f, 0.5f, 0.75f),
                    node => SkillInfoShower.instance.Show(node),
                    _ => SkillInfoShower.instance.Hide(),
                    node =>
                    {
                        if (player.skillPoints < node.data.cost)
                            return;

                        UnlockSkill(node);
                    }
                );

                //加载已解锁的技能
                foreach (var skill in player.unlockedSkills)
                {
                    var node = skillNodeTree.FindTreeNode(skill.id);

                    if (node == null)
                    {
                        Debug.LogError($"技能 {skill.id} 未找到对应的节点");
                        continue;
                    }

                    node.status.unlocked = true;
                }

                //刷新节点显示
                skillNodeTree.RefreshNodes(false);

                #endregion
            }
            #endregion

            #region 添加状态栏
            {
                Vector4 posC = UIA.UpperRight;
                int xExtraOffset = -40;
                int yExtraOffset = -35;

                manaBarBg = GameUI.AddImage(posC, "ori:image.mana_bar_bg", "ori:mana_bar");
                manaBarFull = GameUI.AddImage(posC, "ori:image.mana_bar_full", "ori:mana_bar");
                manaBarEffect = GameUI.AddImage(posC, "ori:image.mana_bar_effect", "ori:mana_bar");
                SetIt(manaBarBg, manaBarFull, manaBarEffect, xExtraOffset, yExtraOffset, GetManaBarFullAmount);

                healthBarBg = GameUI.AddImage(posC, "ori:image.health_bar_bg", "ori:health_bar");
                healthBarFull = GameUI.AddImage(posC, "ori:image.health_bar_full", "ori:health_bar");
                healthBarEffect = GameUI.AddImage(posC, "ori:image.health_bar_effect", "ori:health_bar");
                SetIt(healthBarBg, healthBarFull, healthBarEffect, xExtraOffset, 0, GetHealthBarFullAmount);

                static void SetIt(ImageIdentity bg, ImageIdentity full, ImageIdentity effect, float xOffset, float yOffset, Func<float> getValue)
                {
                    Vector2 size = new(160, 40);
                    Image.Type imageType = Image.Type.Filled;
                    Image.FillMethod fillMethod = Image.FillMethod.Horizontal;
                    float bgBrightness = 0.5f;
                    float defaultX = -bg.sd.x / 2;
                    int defaultY = -30;

                    bg.rt.sizeDelta = size;
                    bg.rt.AddLocalPos(new(defaultX + xOffset, defaultY + yOffset));
                    bg.image.SetColorBrightness(bgBrightness);

                    effect.rt.sizeDelta = size;
                    effect.rt.SetParentForUI(bg.image.rectTransform);
                    effect.image.type = imageType;
                    effect.image.fillMethod = fillMethod;
                    effect.SetColor(new(1, 1, 1, 0.5f));

                    full.rt.sizeDelta = size;
                    full.rt.SetParentForUI(effect.image.rectTransform);
                    full.image.type = imageType;
                    full.image.fillMethod = fillMethod;

                    //初始化时就直接设置填充率，否则会触发 effect 读条
                    var value = getValue();
                    effect.image.fillAmount = value;
                    full.image.fillAmount = value;
                }
            }
            #endregion

            #region 昼夜条
            {
                dayNightBar = GameUI.AddImage(UIA.Up, "ori:image.day_night_bar", "ori:day_night_bar");
                dayNightBar.SetSizeDelta(300, 50);
                dayNightBar.SetAPosY(-dayNightBar.sd.y / 2 - 50);

                dayNightBarPointer = GameUI.AddImage(UIA.UpperLeft, "ori:image.day_night_bar_pointer", "ori:day_night_bar_pointer", dayNightBar);
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
                coinTextImage = GameUI.AddTextImage(UIA.LowerLeft, "ori:text_image.coin", "ori:coin");
                coinTextImage.SetSizeDeltaBoth(70, 70);
                coinTextImage.ap = coinTextImage.sd / 2;
                coinTextImage.OnUpdate += _ => coinTextImage.SetText(player.coin);
                coinTextImage.SetTextAttach(TextImageIdentity.TextAttach.Right);
            }
            #endregion

            #region 重生
            {
                respawnPanel = GameUI.AddPanel("ori:panel.respawn", GameUI.canvas.transform, true);
                respawnButton = GameUI.AddButton(UIA.Middle, "ori:button.respawn", respawnPanel);
                respawnPanelText = GameUI.AddText(UIA.Middle, "ori:text.respawn_info", respawnPanel);
                respawnTimerText = GameUI.AddText(UIA.Middle, "ori:text.respawn_timer", respawnPanel);

                respawnPanelText.SetAPosY(100);
                respawnPanelText.SetSizeDelta(500, 120);
                respawnPanelText.text.SetFontSize(24);
                respawnPanelText.RefreshUI();
                respawnPanelText.doRefresh = false;

                respawnButton.SetAPosY(-20);
                respawnButton.buttonText.RefreshUI();
                respawnButton.buttonText.doRefresh = false;
                respawnButton.OnClickBind(() =>
                {
                    respawnButton.button.interactable = false;
                    GameUI.FadeOut(respawnPanel.panelImage);
                    GameUI.FadeIn(respawnButton.image);
                    GameUI.FadeIn(respawnButton.buttonText.text);

                    player.Respawn(player.maxHealth, null);
                });

                respawnPanel.OnUpdate += i =>
                {
#if UNITY_EDITOR
                    if (Keyboard.current?.spaceKey?.wasPressedThisFrame ?? false)
                        player.respawnTimer = 0;
#endif

                    if (Tools.time >= player.respawnTimer)
                    {
                        respawnButton.button.interactable = true;
                        respawnTimerText.gameObject.SetActive(false);
                    }
                    else
                    {
                        respawnButton.button.interactable = false;
                        respawnTimerText.gameObject.SetActive(true);
                        respawnTimerText.text.text = ((int)(player.respawnTimer - Tools.time)).ToString();
                    }
                };
            }
            #endregion

            #region 获得珍贵物品界面

            gainRareItemButtonPanel = GameUI.AddButton(UIA.Middle, "ori:button.get_item", GameUI.canvas.transform, null);
            gainRareItemButtonPanel.SetSizeDelta(600, 250);
            gainRareItemButtonPanel.SetSprite(null);
            gainRareItemButtonPanel.SetColor(new(0.1f, 0.1f, 0.1f, 0.75f));
            gainRareItemButtonPanel.button.ClearColorEffects();
            gainRareItemButtonPanel.OnUpdate += _ =>
            {
                if (Application.isFocused && player.playerController.Apply())
                {
                    GameUI.SetPage(null, GameUI.DisappearType.PositionDownToUp, GameUI.AppearType.PositionDownToUp);
                }
            };
            gainRareItemButtonPanel.gameObject.SetActive(false);
            GameObject.Destroy(gainRareItemButtonPanel.buttonText.gameObject);


            gainRareItemIcon = GameUI.AddImage(UIA.Left, "ori:image.get_item_icon", null, gainRareItemButtonPanel);
            gainRareItemIcon.SetAPosX(gainRareItemIcon.sd.x / 2 + 30);


            gainRareItemNameText = GameUI.AddText(UIA.Up, "ori:text.get_item_name", gainRareItemButtonPanel);
            gainRareItemNameText.autoCompareText = false;
            gainRareItemNameText.SetAPos(50, -gainRareItemNameText.sd.y / 2 - 20);
            gainRareItemNameText.SetSizeDeltaY(40);
            gainRareItemNameText.text.SetFontSize(24);
            gainRareItemNameText.text.alignment = TMPro.TextAlignmentOptions.Left;


            gainRareItemDescriptionText = GameUI.AddText(UIA.Up, "ori:text.get_item_description", gainRareItemButtonPanel);
            gainRareItemDescriptionText.autoCompareText = false;
            gainRareItemDescriptionText.SetAPosOnBySizeDown(gainRareItemNameText, 3);
            gainRareItemDescriptionText.text.SetFontSize(14);
            gainRareItemDescriptionText.text.alignment = TMPro.TextAlignmentOptions.TopLeft;


            #endregion
        }

        /// <summary>
        /// 注意：该方法不会检查技能点是否足够，请在调用前自行检查
        /// </summary>
        public void UnlockSkill(SkillNode node)
        {
            if (node.status.unlocked || !node.IsParentLineUnlocked())
                return;

            //刷新玩家属性
            player.ServerAddSkillPoint(-node.data.cost);
            if (!player.unlockedSkills.Any(p => p.id == node.data.id))
            {
                player.AddUnlockedSkills(new() { id = node.data.id, unlocked = true });
            };

            //刷新节点显示
            node.status.unlocked = true;
            skillNodeTree.RefreshNodes(false);

            //调用委托
            OnUnlockSkill(node.data);
        }







        #region  背包界面

        public void RefreshCurrentBackpackPanel() => currentBackpackPanel?.Refresh?.Invoke();

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
            Action Update = null,
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
            (var switchButtonBackground, var switchButton) = GameUI.GenerateSidebarSwitchButton(
                $"{modId}:image.{panelName}_switch_background",
                $"{modId}:button.{panelName}_switch",
                switchButtonTexture,
                backpackPanelBackground,
                backpackPanels.Count);

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

            BackpackPanel result = new(id, panel, switchButtonBackground, switchButton, Refresh, ActualActivate, ActualDeactivate, Update);
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
                    GameObject.Destroy(item.switchButtonBackground.gameObject);
                    backpackPanels.Remove(item);

                    //如果当前面板是该面板，则切换到背包面板
                    if (currentBackpackPanelId == id)
                        SetBackpackPanel("ori:inventory");

                    return;
                }
            }

            Debug.LogError($"未找到背包界面 {id}, 销毁失败");
        }

        public void SetBackpackPanel(string id)
        {
            //先关闭当前面板
            foreach (var panel in backpackPanels)
            {
                if (panel.id == currentBackpackPanelId)
                {
                    //改变按钮大小
                    panel.switchButtonBackground.sd /= 1.3f;
                    panel.switchButton.sd /= 1.3f;
                    panel.switchButtonBackground.SetAPosY(panel.switchButtonBackground.sd.y / 2);

                    //取消激活
                    panel.Deactivate();
                    currentBackpackPanel = null;
                }
            }

            //然后打开指定面板
            foreach (var panel in backpackPanels)
            {
                if (panel.id == id)
                {
                    //改变按钮大小
                    panel.switchButtonBackground.sd *= 1.3f;
                    panel.switchButton.sd *= 1.3f;
                    panel.switchButtonBackground.SetAPosY(panel.switchButtonBackground.sd.y / 2);

                    panel.Refresh?.Invoke();
                    panel.Activate();
                    currentBackpackPanelId = id;
                    currentBackpackPanel = panel;
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
                if (currentBackpackPanelId == backpackPanelId)
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
                ItemInfoShower.instance.Hide();
                craftingInfoShower.Hide();
                TaskInfoShower.instance.Hide();
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

        public void ShowOrHideBackpackAndSetPanelToTasks()
        {
            ShowOrHideBackpackAndSetPanelTo("ori:tasks");
        }

        public void ShowOrHideBackpackAndSetPanelToSkills()
        {
            ShowOrHideBackpackAndSetPanelTo("ori:skills");
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





        public (BackpackPanel panel, ScrollViewIdentity itemView) GenerateItemViewBackpackPanel(
            string id,
            string switchButtonTexture,
            float cellSize,
            Vector2 viewSize,
            Vector2 cellSpacing,
            Action Refresh = null,
            Action OnActivate = null,
            Action OnDeactivate = null,
            Action Update = null,
            string texture = "ori:backpack_inventory_background")
        {
            (var modId, var panelName) = Tools.SplitModIdAndName(id);

            var panel = GenerateBackpackPanel(id, switchButtonTexture, Refresh, OnActivate, OnDeactivate, Update, texture);
            var view = GenerateItemScrollView($"{modId}:scrollview.{panelName}", cellSize, viewSize, cellSpacing, null);
            view.transform.SetParent(panel.panel.rt, false);
            view.SetAnchorMinMax(UIA.StretchDouble);
            view.content.anchoredPosition = Vector2.zero;
            view.content.sizeDelta = Vector2.zero;
            Component.Destroy(view.viewportImage);

            return (panel, view);
        }

        public ScrollViewIdentity GenerateItemScrollView(string id, float cellSize, Vector2 viewSize, Vector2 cellSpacing, string backgroundTexture)
        {
            //桶的物品视图
            var itemView = GameUI.AddScrollView(UIA.Middle, id);
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
            //更新背包界面（需要 ToArray 以及 null 检查是因为在 Update 中移除面板会导致列表变化）
            foreach (var panel in backpackPanels.ToArray())
            {
                panel?.Update?.Invoke();
            }

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
                                ShowOrHideBackpackAndSetPanelToTasks();

                            if (Keyboard.current.kKey.wasReleasedThisFrame)
                                ShowOrHideBackpackAndSetPanelToSkills();
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
                                ShowOrHideBackpackAndSetPanelToTasks();

                            if (Gamepad.current.dpad.right.wasReleasedThisFrame)
                                ShowOrHideBackpackAndSetPanelToSkills();
                        }

                        break;
                }
            }

            /* ------------------------------- 检查获取珍惜物品队列 ------------------------------- */
            CheckGainRareItemQueue();





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
                    float radius = player.interactiveRadius;

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
            manaBarFull.image.fillAmount = GetManaBarFullAmount();
            healthBarFull.image.fillAmount = GetHealthBarFullAmount();

            RefreshEffect(manaBarEffect, manaBarFull);
            RefreshEffect(healthBarEffect, healthBarFull);

            static void RefreshEffect(ImageIdentity effect, ImageIdentity full)
            {
                if (effect.image.fillAmount > full.image.fillAmount)
                    effect.image.fillAmount -= Tools.deltaTime * 0.15f;
                else if (effect.image.fillAmount < full.image.fillAmount)
                    effect.image.fillAmount = full.image.fillAmount;
            }
        }

        float GetManaBarFullAmount() => player.mana / Player.maxMana;
        float GetHealthBarFullAmount() => (float)player.health / player.maxHealth;






        #region 任务系统

        public bool playingTaskCompletion { get; private set; }

        IEnumerator TaskCompleteTimer()
        {
            yield return new WaitForSeconds(5f);

            if (taskCompleteBackground)
                GameUI.Disappear(taskCompleteBackground);

            playingTaskCompletion = false;
        }

        public void CompleteTask(string id, bool feedback = true)
        {
            var node = taskNodeTree.FindTreeNode(id);
            if (node == null)
            {
                Debug.LogError($"未找到任务 {id}, 完成失败");
                return;
            }

            if (!node.status.completed)
            {
                //更改玩家数据
                if (!player.completedTasks.Any(p => p.id == id))
                {
                    player.AddCompletedTasks(new() { id = id, completed = true, hasGotRewards = node.status.hasGotRewards });
                };

                //更改节点显示数据
                node.status.completed = true;

                //如果要求反馈，就播放完成动画
                if (feedback)
                {
                    MethodAgent.CallUntil(() => !playingTaskCompletion, () =>
                    {
                        playingTaskCompletion = true;
                        GameUI.Appear(taskCompleteBackground);

                        taskCompleteIcon.image.sprite = node.icon.image.sprite;
                        taskCompleteText.text.text = node.button.buttonText.text.text;

                        GAudio.Play(AudioID.Complete);
                        CoroutineStarter.Do(TaskCompleteTimer());
                    });
                }
            }


            taskNodeTree.RefreshNodes(false);
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
            public int skillPointReward;
            public string[] itemRewards;

            public TaskData(string id, string icon, string parent, int skillPointRewards, string[] itemRewards) : base(id, icon, parent)
            {
                this.skillPointReward = skillPointRewards;
                this.itemRewards = itemRewards;
            }
        }

        public class TaskNode : TreeNode<TaskData>
        {
            public TaskStatus status = new();

            public TaskNode(TaskData data) : base(data)
            {
                //如果没有技能点奖励和物品奖励，则不需要领取
                if (data.skillPointReward == 0 && (data.itemRewards == null || data.itemRewards.Length == 0))
                    status.hasGotRewards = true;
            }
        }

        #endregion





        #region 技能系统


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

            public bool IsParentLineUnlocked()
            {
                var currentParent = parent;

                while (currentParent != null)
                {
                    if (!((SkillNode)currentParent).status.unlocked)
                        return false;

                    currentParent = currentParent.parent;
                }

                return true;
            }

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
        public Action Update;

        public BackpackPanel(string id, PanelIdentity panel, ImageIdentity switchButtonBackground, ButtonIdentity switchButton, Action Refresh, Action Activate, Action Deactivate, Action Update)
        {
            this.id = id;
            this.panel = panel;
            this.switchButtonBackground = switchButtonBackground;
            this.switchButton = switchButton;
            this.Refresh = Refresh;
            this.Activate = Activate;
            this.Deactivate = Deactivate;
            this.Update = Update;
        }

        public RectTransform rectTransform => panel.rectTransform;
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
                ImageIdentity image = GameUI.AddImage(UIA.Middle, "ori:image.item_dragger", "ori:square_button_flat");
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