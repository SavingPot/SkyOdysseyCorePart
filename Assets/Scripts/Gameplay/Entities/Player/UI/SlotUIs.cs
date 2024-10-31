using System;
using GameCore.UI;
using Org.BouncyCastle.Crypto.Prng;
using SP.Tools.Unity;
using UnityEngine;

namespace GameCore.UI
{
    public class ItemSlotUI
    {
        public ButtonIdentity button;
        public ImageIdentity content;





        public ItemSlotUI(ButtonIdentity button, ImageIdentity content)
        {
            this.button = button;
            this.content = content;
        }

        public ItemSlotUI(string buttonId, string imageId, Vector2 sizeDelta)
        {
            button = GameUI.AddButton(UIA.Middle, buttonId);
            content = GameUI.AddImage(UIA.Middle, imageId, null, button);

            button.image.sprite = ModFactory.CompareTexture("ori:item_slot").sprite;
            content.image.sprite = null;

            button.sd = sizeDelta;
            content.sd = sizeDelta * 0.6f;

            content.ap = new(0, 3);

            button.buttonText.text.raycastTarget = false;
            content.image.raycastTarget = false;

            button.buttonText.SetSizeDeltaX(100);
            button.buttonText.text.SetFontSize(13);
            button.buttonText.SetAPosOnBySizeDown(button, -27.5f);
            button.buttonText.doRefresh = false;
            button.buttonText.text.text = string.Empty;
        }
    }





    public class InventorySlotUI : ItemSlotUI
    {
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
                button.button.OnPointerStayAction = () => ItemInfoShower.instance.Hide();

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
                    ItemInfoShower.instance.Show(item);

                    //应用物品的信息修改器
                    if (!Item.Null(item))
                    {
                        //先匹配 Id
                        if (Item.infoModifiersForId.TryGetValue(item.data.id, out var modifier))
                        {
                            ItemInfoShower.instance.detailText.text.text += modifier(item) + "\n";
                        }

                        //再匹配 Tag
                        foreach (var tag in item.data.tags)
                        {
                            //获取标签名称
                            string tagName = tag.Contains('=') ? tag.Split('=')[0] : tag;

                            if (Item.infoModifiersForTag.TryGetValue(tagName, out modifier))
                            {
                                ItemInfoShower.instance.detailText.text.text += modifier(item) + "\n";
                            }
                        }
                    }
                };
                button.button.OnPointerExitAction = _ => ItemInfoShower.instance.Hide();

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





        public InventorySlotUI(ButtonIdentity button, ImageIdentity content) : base(button, content) { }

        public InventorySlotUI(string buttonId, string imageId, Vector2 sizeDelta) : base(buttonId, imageId, sizeDelta) { }
    }





    public class TradeUI
    {
        public BackpackPanel itemBackpackPanel { get; private set; }
        public ScrollViewIdentity itemView { get; private set; }
        public ItemSlotUI[] slotUIs { get; private set; }
        public ItemTrade[] items;
        public string backpackPanelId;
        internal PlayerUI pui;

        public void RefreshItems()
        {
            for (int i = 0; i < items.Length; i++)
            {
                var slotUI = slotUIs[i];
                var item = items[i];
                var itemData = ModFactory.CompareItem(item.itemId);

                slotUI.content.image.sprite = itemData.texture.sprite;
                slotUI.button.SetText($"\n\n{GameUI.CompareText(item.itemId)}x{item.itemCount}\nCost:{item.cost}");
            }
        }

        private TradeUI()
        {

        }

        static (TradeUI result, string modId, string panelName) NewTradeUI(PlayerUI pui, string backpackPanelId, ItemTrade[] items)
        {
            (var modId, var panelName) = Tools.SplitModIdAndName(backpackPanelId);
            var result = new TradeUI
            {
                items = items,
                backpackPanelId = backpackPanelId,
                slotUIs = new ItemSlotUI[items.Length],
                pui = pui
            };

            return (result, modId, panelName);
        }

        static void AddSlots(TradeUI tradeUI, ItemTrade[] items, Player player, string modId, string panelName)
        {
            //初始化所有UI
            for (int i = 0; i < tradeUI.slotUIs.Length; i++)
            {
                int index = i;
                ItemSlotUI slotUI = new($"{modId}:button.{panelName}_item_{i}", $"{modId}:image.{panelName}_item_{i}", tradeUI.itemView.gridLayoutGroup.cellSize);
                tradeUI.slotUIs[i] = slotUI;
                tradeUI.itemView.AddChild(slotUI.button);
                slotUI.button.button.onClick.RemoveAllListeners();
                slotUI.button.OnClickBind(() =>
                {
                    var item = items[index];

                    //检查金币
                    if (player.coin < item.cost)
                    {
                        InternalUIAdder.instance.SetStatusText(GameUI.CompareText("ori:trade_not_enough_coin"));
                        return;
                    }

                    //减少金币
                    player.ServerAddCoin(-item.cost);
                    GAudio.Play(AudioID.Trade, null);

                    //给予物品
                    var itemToGive = ModFactory.CompareItem(item.itemId).DataToItem();
                    itemToGive.count = item.itemCount;
                    player.ServerAddItem(itemToGive);
                });
            }
        }

        public static TradeUI GenerateItemView(PlayerUI pui, string viewId, ItemTrade[] items, Transform parent)
        {
            var (result, modId, panelName) = NewTradeUI(pui, viewId, items);

            //物品视图
            result.itemView = pui.Backpack.GenerateItemView(
                viewId,
                80,
                Vector2.zero,
                Vector2.zero,
                null);

            result.itemView.transform.SetParent(parent, false);
            result.itemView.SetAnchorMinMax(UIA.StretchDouble);
            result.itemView.content.anchoredPosition = Vector2.zero;
            result.itemView.content.sizeDelta = Vector2.zero;
            Component.Destroy(result.itemView.viewportImage);

            AddSlots(result, items, pui.player, modId, panelName);

            return result;
        }

        public static TradeUI GenerateItemViewBackpackPanel(PlayerUI pui, string backpackPanelId, ItemTrade[] items)
        {
            var (result, modId, panelName) = NewTradeUI(pui, backpackPanelId, items);

            //物品视图
            (result.itemBackpackPanel, result.itemView) = pui.Backpack.GenerateItemViewBackpackPanel(
                backpackPanelId,
                $"{modId}:switch_button.{panelName}",
                80,
                Vector2.zero,
                Vector2.zero,
                result.RefreshItems,
                () => result.itemView.gameObject.SetActive(true));

            AddSlots(result, items, pui.player, modId, panelName);

            return result;
        }

        public struct ItemTrade
        {
            public string itemId;
            public ushort itemCount;
            public int cost;

            public ItemTrade(string itemId, ushort itemCount, int cost)
            {
                this.itemId = itemId;
                this.itemCount = itemCount;
                this.cost = cost;
            }
        }
    }
}