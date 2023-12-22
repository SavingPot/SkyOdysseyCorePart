using Newtonsoft.Json.Linq;

namespace GameCore
{
    public interface IManaContainer
    {
        public int totalMana { get; set; }

        public static void LoadFromJObject(IManaContainer container, JObject jo)
        {
            var jt = jo["original:mana_container"];

            container.totalMana = jt["totalMana"].ToInt();
        }
    }
}