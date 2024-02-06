using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using SP.Tools;

namespace GameCore
{
    public static class ModCreate
    {
        public static JToken Get(IJsonFormatWhenLoad core, string name, JToken child)
        {
            return Get(child, name, core.jsonFormatWhenLoad);
        }

        public static string GetStr(IJsonFormatWhenLoad core, string name, JToken child)
        {
            return Get(core, name, child)?.ToString();
        }

        public static JToken Get(IJOFormatCore core, string name)
        {
            return Get(core.jo, name, core.jsonFormatWhenLoad);
        }

        public static string GetStr(IJOFormatCore core, string name)
        {
            return Get(core, name)?.ToString();
        }

        public static void GetFor(IJOFormatCore core, string name, Action<JToken> action)
        {
            Get(core, name)?.For(action);
        }

        public static void GetFor(IJOFormatCore core, string name, JToken child, Action<JToken> action)
        {
            Get(core, name, child)?.For(action);
        }

        public static JToken Get(IJOFormatCoreChild core, string name)
        {
            return Get(core.jt, name, core.jsonFormatWhenLoad);
        }

        public static string GetStr(IJOFormatCoreChild core, string name)
        {
            return Get(core, name)?.ToString();
        }

        public static void GetFor(IJOFormatCoreChild core, string name, Action<JToken> action)
        {
            Get(core, name)?.For(action);
        }

        public static void GetFor(IJOFormatCoreChild core, string name, JToken child, Action<JToken> action)
        {
            Get(core, name, child)?.For(action);
        }

        public static void GetFor(JToken jt, string name, string jf, Action<JToken> action)
        {
            Get(jt, name, jf)?.For(action);
        }

        public static JToken Get(JToken jo, string name, string jf)
        {
            //获取结构
            string[] parts = pairs[name, jf];

            //通过结构获取内容
            return Get(jo, parts);
        }

        public static JToken Get(JToken jo, string[] parts)
        {
            //通过结构获取内容
            JToken j = jo;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                j = j?[part];
            }

            return j;
        }

        #region 值写入
        public static JContainer WriteValuesToJO(JContainer jo, string name, string jf, string value)
        {
            //获取结构
            string[] parts = pairs[name, jf];

            //通过结构获取内容
            JToken j = new JProperty(parts[^1], value);
            for (int i = parts.Length - 2; i >= 0; i--)
            {
                j = new JObject(j);
                j = new JProperty(parts[i], j);
            }

            #region 为了便于理解, 请看下方
            //j = new JProperty("ASDASD", new JObject(new JProperty("asdasd", new JObject(new JProperty("asd", new JObject(j))))));
            //
            //j = new JObject(j);
            //j = new JProperty("asd", j);
            //j = new JObject(j);
            //j = new JProperty("ASDASD", j);
            //j = new JObject(j);
            //j = new JProperty("123123", j);
            #endregion

            jo.Add(j);

            return jo;
        }

        public static JContainer WriteValuesToJO(JContainer jo, string name, string jf, JArray value)
        {
            //获取结构
            string[] parts = pairs[name, jf];

            //通过结构获取内容
            JToken j = new JProperty(parts[^1], value);
            for (int i = parts.Length - 2; i >= 0; i--)
            {
                j = new JObject(j);
                j = new JProperty(parts[i], j);
            }

            #region 为了便于理解, 请看下方
            //j = new JProperty("ASDASD", new JObject(new JProperty("asdasd", new JObject(new JProperty("asd", new JObject(j))))));
            //
            //j = new JObject(j);
            //j = new JProperty("asd", j);
            //j = new JObject(j);
            //j = new JProperty("ASDASD", j);
            //j = new JObject(j);
            //j = new JProperty("123123", j);
            #endregion

            jo.Add(j);

            return jo;
        }

        public static JContainer WriteValuesToJO(JContainer jo, string name, string jf, string[] values)
        {
            //获取结构
            string[] parts = pairs[name, jf];

            //通过结构获取内容
            JToken j = new JProperty(parts[^1], new JArray(values));
            for (int i = parts.Length - 2; i >= 0; i--)
            {
                j = new JObject(j);
                j = new JProperty(parts[i], j);
            }

            #region 为了便于理解, 请看下方
            //j = new JProperty("ASDASD", new JObject(new JProperty("asdasd", new JObject(new JProperty("asd", new JObject(j))))));
            //
            //j = new JObject(j);
            //j = new JProperty("asd", j);
            //j = new JObject(j);
            //j = new JProperty("ASDASD", j);
            //j = new JObject(j);
            //j = new JProperty("123123", j);
            #endregion

            jo.Add(j);

            return jo;
        }
        #endregion

        #region ToJContainer
        public static JContainer ToJContainer(Mod_Info info, string jf = "0.6.4")
        {
            //将模组信息写入 JObject 并拼接
            JObject jo = new()
            {
                new JProperty("json_format", jf)
            };

            var info_id = WriteValuesToJO(new JObject(), "info.id", jf, info.id);
            var info_version = WriteValuesToJO(new JObject(), "info.version", jf, info.version);
            var info_display_name = WriteValuesToJO(new JObject(), "info.display.name", jf, info.name);
            var info_display_description = WriteValuesToJO(new JObject(), "info.display.description", jf, info.description);

            jo.Merge(info_id);
            jo.Merge(info_version);
            jo.Merge(info_display_name);
            jo.Merge(info_display_description);

            return jo;
        }

        public static JContainer ToJContainer(GameLang text, string jf = "0.6.3")
        {
            //将模组信息写入 JObject 并拼接
            JObject jo = new()
            {
                new JProperty("json_format", jf)
            };

            var text_id = WriteValuesToJO(new JObject(), "assets.lang.id", jf, text.id);
            JArray text_texts = new();
            for (int i = 0; i < text.texts.Count; i++)
            {
                JObject text_texts_local = new();
                var text_texts_id = WriteValuesToJO(new JObject(), "assets.lang.texts.id", jf, text.texts[i].id);
                var text_texts_text = WriteValuesToJO(new JObject(), "assets.lang.texts.text", jf, text.texts[i].text);

                text_texts_local.Merge(text_texts_id);
                text_texts_local.Merge(text_texts_text);

                text_texts.Add(text_texts_local);
            }

            jo.Merge(text_id);
            jo.Merge(WriteValuesToJO(new JObject(), "assets.lang.texts", jf, text_texts));

            return jo;
        }
        #endregion

        public static DKDictionary<string, string, string[]> pairs = new()
        {
            pairs = new()
            {
                {
                    new("data.biome.blocks.type", "0.6.2"),
                    new[] { "type" }
                },
                {
                    new("data.biome.blocks.block", "0.6.2"),
                    new[] { "block" }
                },
                {
                    new("data.biome.blocks.areas", "0.6.2"),
                    new[] { "areas" }
                },
                {
                    new("data.biome.blocks.ranges", "0.6.2"),
                    new[] { "ranges" }
                },
                {
                    new("data.biome.blocks.ranges.min", "0.6.2"),
                    new[] { "min" }
                },
                {
                    new("data.biome.blocks.ranges.max", "0.6.2"),
                    new[] { "max" }
                },
                {
                    new("data.biome.blocks.attached.id", "0.6.2"),
                    new[] { "attached", "id" }
                },
                {
                    new("data.biome.blocks.attached.offset", "0.6.2"),
                    new[] { "attached", "offset" }
                },
                {
                    new("data.biome.blocks.attached.loc", "0.6.2"),
                    new[] { "attached", "loc" }
                },
                {
                    new("data.biome.blocks.rules.probability", "0.6.2"),
                    new[] { "rules", "probability" }
                },
                {
                    new("data.biome.blocks", "0.6.2"),
                    new[] { "ori:biome", "blocks" }
                },
                {
                    new("data.biome.structures.id", "0.6.2"),
                    new[] { "id" }
                },
                {
                    new("data.biome.id", "0.6.2"),
                    new[] { "ori:biome", "id" }
                },
                {
                    new("data.biome.tags", "0.6.2"),
                    new[] { "ori:biome", "tags" }
                },
                {
                    new("data.biome.structures", "0.6.2"),
                    new[] { "ori:biome", "structures" }
                },
                {
                    new("data.biome.blocks.fixed_layers", "0.6.2"),
                    new[] { "ori:biome", "blocks", "fixed_layers" }
                },
                {
                    new("data.biome.blocks.inserted_blocks", "0.6.2"),
                    new[] { "ori:biome", "blocks", "inserted_blocks" }
                },
                {
                    new("data.biome.size_scope.min", "0.6.2"),
                    new[] { "ori:biome", "size_scope", "min" }
                },
                {
                    new("data.biome.size_scope.max", "0.6.2"),
                    new[] { "ori:biome", "size_scope", "max" }
                },







                {
                    new("assets.lang.id", "0.6.4"),
                    new[] { "ori:langs", "id" }
                },
                {
                    new("assets.lang.id", "0.6.3"),
                    new[] { "ori:texts", "id" }
                },

                {
                    new("assets.lang.name", "0.6.4"),
                    new[] { "ori:langs", "name" }
                },
                {
                    new("assets.lang.name", "0.6.3"),
                    new[] { "ori:texts", "name" }
                },

                {
                    new("assets.lang.texts", "0.6.4"),
                    new[] { "ori:langs", "texts" }
                },
                {
                    new("assets.lang.texts", "0.6.3"),
                    new[] { "ori:texts", "texts" }
                },

                {
                    new("assets.lang.texts.id", "0.6.4"),
                    new[] { "id" }
                },
                {
                    new("assets.lang.texts.id", "0.6.3"),
                    new[] { "id" }
                },

                {
                    new("assets.lang.texts.text", "0.6.4"),
                    new[] { "text" }
                },
                {
                    new("assets.lang.texts.text", "0.6.3"),
                    new[] { "text" }
                }
            }
        };
    }
}
