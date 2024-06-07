using Newtonsoft.Json.Linq;

namespace GameCore
{
    public static class ItemContainerBehaviour
    {
        public static JArray FixCustomDataAsItemContainer(this IItemContainer container, int defaultItemCount, ref JObject jo)
        {
            jo ??= new();

            if (!jo.TryGetJToken("ori:container", out var containerToken))
            {
                jo.AddObject("ori:container");
                containerToken = jo["ori:container"];
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

        public static void LoadItemsFromCustomData(this IItemContainer container, int defaultItemCount, ref JObject jo)
        {
            //修正 JObject
            var array = FixCustomDataAsItemContainer(container, defaultItemCount, ref jo);

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

        public static void WriteItemsToCustomData(this IItemContainer container, int defaultItemCount, ref JObject jo)
        {
            //修正 JObject
            var array = FixCustomDataAsItemContainer(container, defaultItemCount, ref jo);

            //清除数据
            array.Clear();

            //写入数据
            foreach (var item in container.items)
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
    }
}
