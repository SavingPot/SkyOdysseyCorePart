using GameCore.UI;
using SP.Tools.Unity;
using SP.Tools;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.Linq;

namespace GameCore
{
    public class BackpackPanelUI : PlayerUIPart
    {
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
        public InventorySlotUI inventoryHelmetSlot;
        public InventorySlotUI inventoryBreastplateSlot;
        public InventorySlotUI inventoryLeggingSlot;
        public InventorySlotUI inventoryBootsSlot;
        public InventorySlotUI inventoryShieldSlot;
        public BackpackPanel inventoryItemPanel;
        public ScrollViewIdentity inventoryItemView;


        /* -------------------------------------------------------------------------- */
        /*                                     合成                                     */
        /* -------------------------------------------------------------------------- */
        public CraftingInfoShower craftingInfoShower;
        public BackpackPanel craftingPanel;
        public ScrollViewIdentity craftingView;
        public List<CraftingViewButton> craftingViewButtonPool = new();
        public string craftingFacility { get; private set; }

        public void SetCraftingFacility(string value)
        {
            craftingFacility = value;
        }

        public CraftingViewButton GenerateCraftingViewButton()
        {
            //添加按钮
            var button = GameUI.AddButton(UIA.Up, $"ori:button.player_crafting_recipe_{Tools.randomGUID}");
            button.image.sprite = ModFactory.CompareTexture("ori:item_slot").sprite;
            craftingView.AddChild(button);

            //物品图标
            var image = GameUI.AddImage(UIA.Middle, $"ori:image.player_crafting_recipe_{Tools.randomGUID}", "ori:item_slot", button);
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
        public TextIdentity skillPointText;
        public NodeTree<SkillNode, SkillData> skillNodeTree;
        public Action<SkillData> OnUnlockSkill = _ => { };

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

                        GAudio.Play(AudioID.Complete, null);
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
            public float skillPointReward;
            public string[] itemRewards;

            public TaskData(string id, string icon, string parent, float skillPointRewards, string[] itemRewards) : base(id, icon, parent)
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

        /// <summary>
        /// 注意：该方法不会检查技能点是否足够，请在调用前自行检查
        /// </summary>
        public void UnlockSkill(SkillNode node)
        {
            if (node.status.unlocked || !node.IsParentLineUnlocked())
                return;

            //刷新玩家属性
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

        #endregion









public void Update()
{
            //更新背包界面（需要 ToArray 以及 null 检查是因为在 Update 中移除面板会导致列表变化）
            foreach (var panel in backpackPanels.ToArray())
            {
                panel?.Update?.Invoke();
            }

            //刷新装备栏
            inventoryHelmetSlot.button.image.sprite = Item.Null(player.inventory.helmet) ? ModFactory.CompareTexture("ori:item_slot_helmet").sprite : ModFactory.CompareTexture("ori:item_slot").sprite;
            inventoryBreastplateSlot.button.image.sprite = Item.Null(player.inventory.breastplate) ? ModFactory.CompareTexture("ori:item_slot_breastplate").sprite : ModFactory.CompareTexture("ori:item_slot").sprite;
            inventoryLeggingSlot.button.image.sprite = Item.Null(player.inventory.legging) ? ModFactory.CompareTexture("ori:item_slot_legging").sprite : ModFactory.CompareTexture("ori:item_slot").sprite;
            inventoryBootsSlot.button.image.sprite = Item.Null(player.inventory.boots) ? ModFactory.CompareTexture("ori:item_slot_boots").sprite : ModFactory.CompareTexture("ori:item_slot").sprite;
            inventoryShieldSlot.button.image.sprite = Item.Null(player.inventory.shield) ? ModFactory.CompareTexture("ori:item_slot_shield").sprite : ModFactory.CompareTexture("ori:item_slot").sprite;

        }







        #region 背包界面

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

                //清除合成设施
                SetCraftingFacility(null);

                GameUI.SetPage(null);
                GAudio.Play(AudioID.CloseBackpack, null);
            }
            //禁用状态 -> 启用
            else
            {
                player.OnInventoryItemChange(player.inventory, null);
                GAudio.Play(AudioID.OpenBackpack, null);

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
            //如果没有界面
            if (GameUI.page == null || !GameUI.page.ui)
                ShowOrHideBackpackAndSetPanelTo("ori:pause");
            //如果处于背包界面
            else if (backpackMask.gameObject.activeSelf)
                ShowOrHideBackpack();
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













        public BackpackPanelUI(PlayerUI pui) : base(pui)
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

                    inventoryHelmetSlot.Refresh(player, Inventory.helmetVar, item => Item.Null(item) || item.data.Helmet != null);
                    inventoryBreastplateSlot.Refresh(player, Inventory.breastplateVar, item => Item.Null(item) || item.data.Breastplate != null);
                    inventoryLeggingSlot.Refresh(player, Inventory.leggingVar, item => Item.Null(item) || item.data.Legging != null);
                    inventoryBootsSlot.Refresh(player, Inventory.bootsVar, item => Item.Null(item) || item.data.Boots != null);
                    inventoryShieldSlot.Refresh(player, Inventory.shieldVar, item => Item.Null(item) || item.data.Shield != null);
                });

            for (int i = 0; i < inventorySlotsUIs.Length; i++)
            {
                int index = i;
                var ui = new InventorySlotUI($"ori:button.backpack_inventory_item_{index}", $"ori:image.backpack_inventory_item_{index}", inventoryItemView.gridLayoutGroup.cellSize);

                inventorySlotsUIs[i] = ui;
                inventoryItemView.AddChild(ui.button);
            }

            inventoryHelmetSlot = new($"ori:button.backpack_inventory_item_{Inventory.helmetVar}", $"ori:image.backpack_inventory_item_{Inventory.helmetVar}", inventoryItemView.gridLayoutGroup.cellSize);
            inventoryBreastplateSlot = new($"ori:button.backpack_inventory_item_{Inventory.breastplateVar}", $"ori:image.backpack_inventory_item_{Inventory.breastplateVar}", inventoryItemView.gridLayoutGroup.cellSize);
            inventoryLeggingSlot = new($"ori:button.backpack_inventory_item_{Inventory.leggingVar}", $"ori:image.backpack_inventory_item_{Inventory.leggingVar}", inventoryItemView.gridLayoutGroup.cellSize);
            inventoryBootsSlot = new($"ori:button.backpack_inventory_item_{Inventory.bootsVar}", $"ori:image.backpack_inventory_item_{Inventory.bootsVar}", inventoryItemView.gridLayoutGroup.cellSize);
            inventoryShieldSlot = new($"ori:button.backpack_inventory_item_{Inventory.shieldVar}", $"ori:image.backpack_inventory_item_{Inventory.shieldVar}", inventoryItemView.gridLayoutGroup.cellSize);

            inventoryHelmetSlot.button.transform.SetParent(inventoryItemView.gridLayoutGroup.transform.parent);
            inventoryBreastplateSlot.button.transform.SetParent(inventoryItemView.gridLayoutGroup.transform.parent);
            inventoryLeggingSlot.button.transform.SetParent(inventoryItemView.gridLayoutGroup.transform.parent);
            inventoryBootsSlot.button.transform.SetParent(inventoryItemView.gridLayoutGroup.transform.parent);
            inventoryShieldSlot.button.transform.SetParent(inventoryItemView.gridLayoutGroup.transform.parent);

            inventoryHelmetSlot.button.SetAnchorMinMax(UIA.LowerLeft);
            inventoryBreastplateSlot.button.SetAnchorMinMax(UIA.LowerLeft);
            inventoryLeggingSlot.button.SetAnchorMinMax(UIA.LowerLeft);
            inventoryBootsSlot.button.SetAnchorMinMax(UIA.LowerLeft);
            inventoryShieldSlot.button.SetAnchorMinMax(UIA.LowerLeft);

            inventoryHelmetSlot.button.ap = new(inventoryHelmetSlot.button.sd.x / 2 + 20, inventoryHelmetSlot.button.sd.y / 2 + 10);
            inventoryBreastplateSlot.button.SetAPosOnBySizeRight(inventoryHelmetSlot.button, 0);
            inventoryLeggingSlot.button.SetAPosOnBySizeRight(inventoryBreastplateSlot.button, 0);
            inventoryBootsSlot.button.SetAPosOnBySizeRight(inventoryLeggingSlot.button, 0);
            inventoryShieldSlot.button.SetAPosOnBySizeRight(inventoryBootsSlot.button, 10);

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
                                GAudio.Play(AudioID.Crafting, null);

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
                    (false, _) => new(0.5f, 0.5f, 0.5f, 0.5f)  //没完成
                },
                node => node.status.completed ? Color.white : new(0.8f, 0.8f, 0.8f, 0.75f),
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
                node => node.status.unlocked ? Color.white : new(0.5f, 0.5f, 0.5f, 0.5f),
                node => node.status.unlocked ? Color.white : new(0.8f, 0.8f, 0.8f, 0.75f),
                node => SkillInfoShower.instance.Show(node),
                _ => SkillInfoShower.instance.Hide(),
                node =>
                {
                    //检查条件
                    if (player.skillPoints < node.data.cost || node.status.unlocked || !node.IsParentLineUnlocked())
                        return;

                    //扣除技能点
                    player.ServerAddSkillPoint(-node.data.cost);

                    //解锁技能
                    UnlockSkill(node);
                }
            );
            skillPointText = GameUI.AddText(UIA.UpperRight, "ori:text.skill_points", skillPanel.panel);
            skillPointText.text.SetFontSize(15);
            skillPointText.SetAPos(-skillPointText.sd.x / 2 - 5, -skillPointText.sd.y / 2 - 5);
            skillPointText.text.alignment = TMPro.TextAlignmentOptions.TopRight;
            skillPointText.text.raycastTarget = false;
            skillPointText.autoCompareText = false;
            skillPointText.AfterRefreshing += _ =>
            {
                skillPointText.SetText($"技能点数: {player.skillPoints}");
            };
            skillPointText.RefreshUI();

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
    }
}