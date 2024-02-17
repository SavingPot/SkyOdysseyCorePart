using Newtonsoft.Json.Linq;

namespace GameCore
{
    public interface IManaContainer
    {
        public int totalMana { get; set; }
        public int maxMana { get; }

        public static void LoadFromJObject(IManaContainer container, JObject jo)
        {
            var jt = jo["ori:mana_container"];

            container.totalMana = jt["total_mana"].ToInt();
        }

        public static void WriteIntoJObject(IManaContainer container, JObject jo)
        {
            var jt = jo["ori:mana_container"];

            if (jt == null)
                jo.AddObject("ori:mana_container");

            jt["total_mana"] = container.totalMana;
        }
    }
}