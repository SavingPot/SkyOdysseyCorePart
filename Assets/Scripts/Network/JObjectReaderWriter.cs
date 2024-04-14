using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SP.Tools;

namespace GameCore
{
    /// <summary>
    /// 这个类是用来序列化和反序列化JObject的
    /// 其服务于 Mirror，与自立的网络系统无关
    /// </summary>
    public static class JObjectReaderWriter
    {
        public static void WriteJObject(this NetworkWriter writer, JObject jo)
        {
            writer.WriteString(Compressor.CompressString(jo?.ToString(Formatting.None)));
        }

        public static JObject ReadJObject(this NetworkReader reader)
        {
            string str = Compressor.DecompressString(reader.ReadString());

            return JsonTools.LoadJObjectByString(str);
        }
    }
}