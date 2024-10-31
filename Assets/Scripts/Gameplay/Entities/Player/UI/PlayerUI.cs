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
using System.Net;

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
        /*                                    手机端操纵                                   */
        /* -------------------------------------------------------------------------- */
        public TouchScreenUI TouchScreen { get; private set; }




        /* -------------------------------------------------------------------------- */
        /*                                    背包界面                                    */
        /* -------------------------------------------------------------------------- */
        public BackpackPanelUI Backpack { get; private set; }
        public readonly InventorySlotUI[] quickInventorySlots;




        /* -------------------------------------------------------------------------- */
        /*                                    放置模式                                    */
        /* -------------------------------------------------------------------------- */
        public PlacementModeUI PlacementMode { get; private set; }
        public bool IsInInteractionMode() => !PlacementMode.placementModePanel.gameObject.activeSelf;
        public bool IsInPlacementMode() => PlacementMode.placementModePanel.gameObject.activeSelf;




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
        /*                                     时间                                    */
        /* -------------------------------------------------------------------------- */
        public TextIdentity timeText;



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
        /*                                     对话                                     */
        /* -------------------------------------------------------------------------- */
        public ButtonIdentity dialogPanel;
        public ImageIdentity dialogHead;
        public TextIdentity dialogNameText;
        public TextIdentity dialogText;

        public DialogData displayingDialog;
        public Task dialogTask;

        public void DisplayDialog(DialogData data, Action onFinish = null)
        {
            if (displayingDialog != null)
            {
                Debug.LogError("一个对话已在播放");
                return;
            }

            displayingDialog = data;
            dialogTask = DisplayDialogTask(onFinish);
        }

        public static string[] dialogRichTextSupported = new[]
        {
            "<color=",
            "</"
        };

        public async Task DisplayDialogTask(Action onFinish = null)
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

            //等一帧防止玩家跳跃
            await UniTask.NextFrame();

            //关闭对话框
            GameUI.SetPage(null);
            displayingDialog = null;

            //完成
            onFinish?.Invoke();
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










        /* -------------------------------------------------------------------------- */
        /*                                    地图界面                                    */
        /* -------------------------------------------------------------------------- */
        public PanelIdentity mapPanel;
        public ScrollViewIdentity mapTeleportPointView;

        public void RefreshTeleportPoints(TeleportPointInfo[] teleportPoints)
        {
            mapTeleportPointView.Clear();

            //添加所有传送点
            foreach (var point in teleportPoints)
            {
                var regionIndex = PosConvert.WorldPosToRegionIndex(point.pos);

                ButtonIdentity button = GameUI.AddButton(UIA.Middle, $"ori:button.map_teleport_point_{Tools.randomGUID}", mapTeleportPointView);
                mapTeleportPointView.AddChild(button);
                button.SetSizeDelta(80, 80);
                button.SetSprite(ModFactory.CompareTexture(BlockID.Portal).sprite);
                button.SetAPos(regionIndex * 300);
                button.buttonText.DisableAutoCompare().SetText($"{regionIndex}\n{GameUI.CompareText(point.biomeId)} - {GameUI.CompareText($"ori:region_index_y.{regionIndex.y}")}");
                button.buttonText.SetAPosY(-button.sd.y / 2 - button.buttonText.sd.y / 2 - 10);
                button.buttonText.text.SetFontSize(20);
                button.buttonText.text.raycastTarget = false;
                button.OnClickBind(() =>
                {
                    player.GenerateRegion(regionIndex, true);
                    GameUI.SetPage(null);
                });
            }

            //添加玩家
            foreach (var item in PlayerCenter.all)
            {
                ButtonIdentity button = GameUI.AddButton(UIA.Middle, $"ori:button.map_teleport_point_{Tools.randomGUID}", mapTeleportPointView);
                mapTeleportPointView.AddChild(button);
                button.SetSizeDelta(30, 30);
                button.SetSprite(item.skinHead);
                button.SetAPos(item.regionIndex * 300);
                button.buttonText.DisableAutoCompare().SetText(item.playerName);
                button.buttonText.SetSizeDeltaY(10);
                button.buttonText.SetAPosY(-button.sd.y / 2 - button.buttonText.sd.y / 2 - 5);
                button.buttonText.text.SetFontSize(10);
                button.button.interactable = false;
                button.buttonText.text.raycastTarget = false;
            }
        }

        [Serializable]
        public struct TeleportPointInfo
        {
            public Vector2 pos;
            public string biomeId;

            public TeleportPointInfo(Vector2 pos, string biomeId)
            {
                this.pos = pos;
                this.biomeId = biomeId;
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





            //触摸屏
            TouchScreen = new(this);



            //背包界面
            Backpack = new(this);



            //放置模式
            PlacementMode = new(this);




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
                    var button = GameUI.AddButton(UIA.Down, "ori:button.item_slot_" + indexAs0, GameUI.canvas.transform, "ori:item_slot");
                    var item = GameUI.AddImage(UIA.Down, "ori:image.item_slot_item_" + indexAs0, "ori:item_slot", button);

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
                        if (!Backpack.backpackMask.gameObject.activeSelf)
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

            #region 时间
            {
                timeText = GameUI.AddText(UIA.LowerRight, "ori:text.time");
                timeText.DisableAutoCompare().OnUpdate += _ => RefreshTime();
                timeText.text.alignment = TMPro.TextAlignmentOptions.BottomRight;
                timeText.SetAPos(-timeText.sd.x / 2 - 5, timeText.sd.y / 2 + 5);
                RefreshTime();

                void RefreshTime()
                {
                    var time = GTime.time24Format;
                    var integerPart = (int)time;
                    var decimalPart = (int)(time * 60) % 60;
                    timeText.SetText($"{integerPart}:{(decimalPart - decimalPart % 10):00}");
                }
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

            #region 地图界面
            {
                mapPanel = GameUI.AddPanel("ori:panel.map", GameUI.canvas.transform, true);
                mapTeleportPointView = GameUI.AddScrollView(UIA.StretchDouble, "ori:scrollview.map_teleport_points", mapPanel);
                mapTeleportPointView.scrollRect.horizontal = true;   //允许水平拖拽
                mapTeleportPointView.scrollRect.movementType = ScrollRect.MovementType.Unrestricted;   //不限制拖拽
                mapTeleportPointView.scrollRect.scrollSensitivity = 0;   //不允许滚轮控制
                mapTeleportPointView.ap = Vector2.zero;
                mapTeleportPointView.sd = Vector2.zero;
                mapTeleportPointView.OnUpdate += _ => mapTeleportPointView.viewportImage.color = Tools.instance.mainCamera.backgroundColor;
                Component.Destroy(mapTeleportPointView.gridLayoutGroup);
            }
            #endregion
        }









        internal static void SetUIHighest(IRectTransform ui)
        {
            ui.rectTransform.SetAsLastSibling();

            if (!ui.rectTransform.gameObject.activeSelf)
                ui.rectTransform.gameObject.SetActive(true);
        }
        internal static void SetUIDisabled(IRectTransform ui)
        {
            if (ui.rectTransform.gameObject.activeSelf)
                ui.rectTransform.gameObject.SetActive(false);
        }

        public void Update()
        {
            //更新背包
            Backpack.Update();

            //手机操控
            TouchScreen.Update();

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
                slot.button.buttonText.text.text = Item.Null(item) || item.count == 1 ? string.Empty : slot.button.buttonText.text.text = item?.count.ToString();

                //设置栏位图标
                if (player.usingItemIndex == i)
                    slot.button.image.sprite = ModFactory.CompareTexture("ori:item_slot_using")?.sprite;
                else
                    slot.button.image.sprite = ModFactory.CompareTexture("ori:item_slot")?.sprite;
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
                                Backpack.ShowOrHideBackpackAndSetPanelToCrafting();

                            if (Keyboard.current.escapeKey.wasReleasedThisFrame)
                                Backpack.PauseGame();

                            if (Keyboard.current.enterKey.wasReleasedThisFrame)
                                Chat();

                            if (Keyboard.current.tKey.wasReleasedThisFrame)
                                Backpack.ShowOrHideBackpackAndSetPanelToTasks();

                            if (Keyboard.current.kKey.wasReleasedThisFrame)
                                Backpack.ShowOrHideBackpackAndSetPanelToSkills();
                        }

                        break;

                    case ControlMode.Gamepad:
                        if (Gamepad.current != null)
                        {
                            if (Gamepad.current.yButton.wasReleasedThisFrame)
                                Backpack.ShowOrHideBackpackAndSetPanelToCrafting();

                            if (Gamepad.current.startButton.wasReleasedThisFrame)
                                Backpack.PauseGame();

                            if (Gamepad.current.dpad.down.wasReleasedThisFrame)
                                Chat();

                            if (Gamepad.current.dpad.up.wasReleasedThisFrame)
                                Backpack.ShowOrHideBackpackAndSetPanelToTasks();

                            if (Gamepad.current.dpad.right.wasReleasedThisFrame)
                                Backpack.ShowOrHideBackpackAndSetPanelToSkills();
                        }

                        break;
                }
            }



            /* ------------------------------- 检查获取珍惜物品队列 ------------------------------- */
            CheckGainRareItemQueue();


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
                    GAudio.Play(AudioID.Chat, null);
                });
            };
        }
    }





    /* -------------------------------------------------------------------------- */
    /*                                     公共类                                    */
    /* -------------------------------------------------------------------------- */

    public abstract class PlayerUIPart
    {
        protected PlayerUI pui;
        protected Player player;

        internal PlayerUIPart(PlayerUI pui)
        {
            this.pui = pui;
            this.player = pui.player;
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