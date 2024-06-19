using Newtonsoft.Json.Linq;

namespace GameCore
{
    public static class ItemContainerBehaviour
    {
        public static JArray FixCustomDataAsItemContainer(int defaultItemCount, ref JObject customData)
        {
            customData ??= new();

            if (!customData.TryGetJToken("ori:container", out var containerToken))
            {
                customData.AddObject("ori:container");
                containerToken = customData["ori:container"];
            }
            if (!containerToken.TryGetJToken("items", out var itemsToken))
            {
                containerToken.AddObject("items");
                itemsToken = containerToken["items"];
            }
            if (!itemsToken.TryGetJToken("array", out var arrayToken))
            {
                JToken[] tokens = new JToken[defaultItemCount];
                itemsToken.AddArray("array", tokens);
                arrayToken = itemsToken["array"];
            }

            return arrayToken as JArray;
        }



        public static void LoadItemsFromCustomData(this IItemContainer container, int defaultItemCount, ref JObject customData)
        {
            //修正 JObject
            var array = FixCustomDataAsItemContainer(defaultItemCount, ref customData);

            /* -------------------------------------------------------------------------- */
            /*                                    读取数据                                    */
            /* -------------------------------------------------------------------------- */
            if (array.Count != 0)
            {
                container.items = array.ToObject<Item[]>();

                for (int i = 0; i < container.items.Length; i++)
                {
                    Item.ResumeFromStreamTransport(ref container.items[i]);
                }
            }
            else
            {
                container.items = new Item[defaultItemCount];
            }
        }



        public static void WriteItemsToCustomData(this IItemContainer container, int defaultItemCount, ref JObject customData)
        {
            //修正 JObject
            var array = FixCustomDataAsItemContainer(defaultItemCount, ref customData);

            //写入
            WriteItemsToCustomData(array, container.items);
        }

        public static void WriteItemsToCustomData(Item[] items, int defaultItemCount, ref JObject customData)
        {
            //修正 JObject
            var array = FixCustomDataAsItemContainer(defaultItemCount, ref customData);

            //写入
            WriteItemsToCustomData(array, items);
        }

        public static void WriteItemsToCustomData(JArray array, Item[] items)
        {
            //清除数据
            array.Clear();

            //逐个写入物品
            foreach (var item in items)
            {
                if (Item.Null(item))
                {
                    array.Add(null);
                    continue;
                }

                array.Add(JToken.FromObject(item));
            }
        }
    }

    public interface IItemContainer
    {
        Item[] items { get; set; }

        Item GetItem(string index);
        void SetItem(string index, Item value);
        //TODO: void AddItem(Item value);
    }
}
