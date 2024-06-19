using GameCore.UI;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    [ItemBinding(ItemID.LootBag)]
    public class LootBagBehaviour : ItemBehaviour
    {
        Item[] items;


        //TODO: Implement loot bag behaviour
        public override bool Use(Vector2 point)
        {
            if (owner is not Player player)
            {
                Debug.LogWarning("Loot bag can only be used by player");
                return false;
            }

            //给予物品
            if (items != null && items.Length != 0)
            {
                //先检查一下背包是否有足够的位置
                var itemsToGive = new Item[items.Length];
                for (int i = 0; i < items.Length; i++)
                {
                    var item = items[i];
                    if (!Inventory.GetIndexesToPutItemIntoItems(player.inventory.slots, item, out _))
                    {
                        InternalUIAdder.instance.SetStatusText("背包栏位不够了，请清理背包后再打开");
                        return true;
                    }
                    itemsToGive[i] = item;
                }

                //检查完毕后开始给予物品
                foreach (var item in itemsToGive)
                {
                    player.ServerAddItem(item);
                }
            }

            //删除物品
            owner.SetItem(inventoryIndex, null);
            return true;
        }

        public static JObject NewCustomData(params Item[] items)
        {
            var ja = new JArray();
            foreach (var item in items)
            {
                ja.Add(JToken.FromObject(item));
            }
            return NewCustomData(ja);
        }

        public static JObject NewCustomData(JArray items)
        {
            return new JObject()
            {
                {
                    "ori:loot_bag",
                    new JObject()
                    {
                        { "items", items }
                    }
                }
            };
        }

        public static Item[] LoadItemsFromJObject(JObject customData)
        {
            var result = new Queue<Item>();
            foreach (var itemJToken in customData["ori:loot_bag"]["items"])
            {
                var item = itemJToken.ToObject<Item>();
                Item.ResumeFromStreamTransport(ref item);
                result.Enqueue(item);
            }
            return result.ToArray();
        }

        public LootBagBehaviour(IInventoryOwner owner, Item instance, string inventoryIndex) : base(owner, instance, inventoryIndex)
        {
            MethodAgent.DebugRun(() => items = LoadItemsFromJObject(instance.customData));
        }
    }
}
