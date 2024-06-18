using Newtonsoft.Json.Linq;

namespace GameCore
{
    public interface IManaContainer
    {
        public int totalMana { get; set; }
        public int maxMana { get; }

        public static void FixJObject(JObject jo)
        {
            if (!jo.TryGetJToken("ori:mana_container", out var containerJT))
            {
                containerJT = new JObject();
                jo.Add(new JProperty("ori:mana_container", containerJT));
            }

            if (!containerJT.TryGetJToken("total_mana", out var totalManaJT))
            {
                totalManaJT = new JValue(0);
                ((JObject)containerJT).Add(new JProperty("total_mana", totalManaJT));
            }
        }

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

        public static void SetTotalMana(JObject customData, int totalMana)
        {
            FixJObject(customData);
            customData["ori:mana_container"]["total_mana"] = totalMana;
        }
    }
}