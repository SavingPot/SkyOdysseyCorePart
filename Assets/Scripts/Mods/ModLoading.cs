using GameCore;
using GameCore.High;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameCore
{
    [Serializable]
    public class DropData : ModClassChild
    {
        public byte count;

        public DropData()
        {

        }

        public DropData(string id, byte count) : this()
        {
            this.id = id;
            this.count = count;
        }
    }

    public static class ModLoading
    {
        #region JsonFormat
        public static string GetJsonFormat(string json)
        {
            try
            {
                //反序列化JSON字符串
                return GetJsonFormat((JObject)JsonConvert.DeserializeObject(json));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return GInit.gameVersion;
            }
        }

        public static string GetJsonFormat(JObject jo)
        {
            try
            {
                //反序列化JSON字符串
                return jo?["json_format"]?.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return GInit.gameVersion;
            }
        }

        public static string GetCorrectJsonFormatByJObject(JObject jo)
        {
            var format = GetJsonFormat(jo);
            ConvertJsonFormat(ref format);
            return format;
        }

        public static void ConvertJsonFormat(ref string jsonFormat)
        {
            //如果为空或大于游戏版本
            if (jsonFormat.IsNullOrWhiteSpace() || GameTools.CompareVersions(jsonFormat, GInit.gameVersion, Operators.than) || GameTools.CompareVersions(jsonFormat, "0.4.5", Operators.less))
            {
                jsonFormat = GInit.gameVersion;
                return;
            }
        }
        #endregion

        public static Mod_Info LoadInfo(JObject jo, string iconPath)
        {
            if (jo == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 不能为空");
                return null;
            }

            var format = GetCorrectJsonFormatByJObject(jo);

            Mod_Info info = new()
            {
                jo = jo,
                jsonFormat = format
            };

            if (true)// (GameTools.CompareVersions(format, "0.6.4", Operators.less))
            {
                info.jsonFormatWhenLoad = "0.6.4";

                info.id = jo["ori:mod_info"]["id"].ToString();
                info.enabled = jo["ori:mod_info"]["enabled"].ToBool();
                info.version = jo["ori:mod_info"]["version"]?.ToString();
                info.description = jo["ori:mod_info"]["display"]["description"].ToString();
                info.name = jo["ori:mod_info"]["display"]["name"].ToString();
            }


            if (!iconPath.IsNullOrWhiteSpace() && File.Exists(iconPath))
            {
                MethodAgent.RunOnMainThread(_ => info.icon = Tools.LoadSpriteByPath(iconPath));
            }
            else
            {
                info.icon = GInit.instance.textureUnknown.sprite;
            }

            return info;
        }

        public static GameLang LoadText(JObject jo)
        {
            if (jo == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 不能为空");
                return null;
            }

            var format = GetCorrectJsonFormatByJObject(jo);
            GameLang lang = new()
            {
                jo = jo,
                jsonFormat = format
            };

            if (GameTools.CompareVersions(format, "0.6.3", Operators.lessOrEqual))
            {
                lang.jsonFormatWhenLoad = "0.6.3";

                lang.id = ModCreate.GetStr(lang, "assets.lang.id");
                lang.textName = ModCreate.GetStr(lang, "assets.lang.name");

                ModCreate.GetFor(lang, "assets.lang.texts", item =>
                {
                    lang.texts.Add(new() { id = ModCreate.GetStr(lang, "assets.lang.texts.id", item), text = ModCreate.GetStr(lang, "assets.lang.texts.text", item) });
                });
            }
            else
            {
                lang.jsonFormatWhenLoad = "0.6.4";

                lang.id = ModCreate.GetStr(lang, "assets.lang.id");
                lang.textName = ModCreate.GetStr(lang, "assets.lang.name");

                ModCreate.GetFor(lang, "assets.lang.texts", item =>
                {
                    lang.texts.Add(new() { id = ModCreate.GetStr(lang, "assets.lang.texts.id", item), text = ModCreate.GetStr(lang, "assets.lang.texts.text", item) });
                });
            }

            return lang;
        }

        public static List<DropData> LoadDrops(JToken jt, string jsonFormat)
        {
            List<DropData> ts = new();

            if (jt == null)
            {
                //Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jt)} 不能为空");
                return ts;
            }

            jt.For(t =>
            {
                DropData temp = new()
                {
                    jt = t,
                    jsonFormat = jsonFormat
                };

                if (GameTools.CompareVersions(jsonFormat, "0.7.1", Operators.thanOrEqual))
                {
                    temp.jsonFormatWhenLoad = "0.7.1";

                    temp.id = t?["id"]?.ToString();
                    temp.count = Convert.ToByte(t?["count"]?.ToString()?.ToInt() ?? 1);
                }

                ts.Add(temp);
            });

            return ts;
        }

        public static EntityData LoadEntity(JObject jo, string entityPath)
        {
            if (jo == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 不能为空");
                return null;
            }

            var format = GetCorrectJsonFormatByJObject(jo);

            EntityData entity = new()
            {
                jo = jo,
                jsonFormat = format
            };
            if (GameTools.CompareVersions(format, "0.5.1", Operators.lessOrEqual))
            {
                entity.jsonFormatWhenLoad = "0.5.1";

                entity.id = jo["ori:entity"]?["id"]?.ToString();
                entity.drops = LoadDrops(jo["ori:entity"]?["drops"], "0.7.1");
                entity.path = entityPath;
                entity.speed = jo["ori:entity"]?["speed"]?.ToFloat() ?? 3;
                entity.colliderSize = jo["ori:entity"]?["physics"]?["collider"]?["size"]?.ToVector2() ?? Vector2.one;
                entity.gravity = jo["ori:entity"]?["physics"]?["gravity"]?.ToFloat() ?? 7;
                entity.maxHealth = jo["ori:entity"]?["max_health"]?.ToFloat() ?? Player.defaultHealth;
                entity.summon.biome = jo["ori:entity"]?["summon"]?["biome"]?.ToString();
                entity.summon.defaultProbability = jo["ori:entity"]?["summon"]?["default_probability"]?.ToString().ToFloat() ?? 100;
                entity.summon.timeEarliest = jo["ori:entity"]?["summon"]?["time_earliest"]?.ToString().ToFloat() ?? 0;
                entity.summon.timeLatest = jo["ori:entity"]?["summon"]?["time_latest"]?.ToString().ToFloat() ?? 0;
            }

            return entity;
        }

        public static CraftingRecipe LoadCraftingRecipe(JObject jo)
        {
            if (jo == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 不能为空");
                return null;
            }

            var format = GetCorrectJsonFormatByJObject(jo);

            CraftingRecipe cr = new()
            {
                jsonFormat = format,
                jo = jo
            };

            if (GameTools.CompareVersions(format, "0.6.0", Operators.thanOrEqual))
            {
                cr.jsonFormatWhenLoad = "0.6.0";

                cr.id = ModCreate.GetStr(cr, "data.recipes.crafting.id");
                cr.result = new(ModCreate.GetStr(cr, "data.recipes.crafting.result.id"), (ModCreate.Get(cr, "data.recipes.crafting.result.count")?.ToInt() ?? 1).ToUShort(), new());

                ModCreate.GetFor(cr, "data.recipes.crafting.items", j =>
                {
                    CraftingRecipe_Item item = LoadCraftingRecipe_Item(cr, j);
                    cr.items.Add(item);
                });
            }

            return cr;
        }

        public static CraftingRecipe_Item LoadCraftingRecipe_Item(CraftingRecipe cr, JToken j)
        {
            if (cr == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(cr)} 不能为空");
                return null;
            }
            if (j == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(j)} 不能为空");
                return null;
            }

            string id = ModCreate.GetStr(cr, "data.recipes.crafting.items.id", j);
            ushort count = (ModCreate.Get(cr, "data.recipes.crafting.items.count", j)?.ToInt() ?? 1).ToUShort();
            List<string> tags = ModCreate.Get(cr, "data.recipes.crafting.items.tags", j)?.ToObject<List<string>>() ?? new();

            CraftingRecipe_Item temp = new(id, count, tags);
            return temp;
        }

        public static CookingRecipe LoadCookingRecipe(JObject jo)
        {
            if (jo == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 不能为空");
                return null;
            }

            var format = GetCorrectJsonFormatByJObject(jo);

            CookingRecipe cr = new()
            {
                jsonFormat = format,
                jo = jo
            };

            if (GameTools.CompareVersions(format, "0.6.0", Operators.thanOrEqual))
            {
                cr.jsonFormatWhenLoad = "0.6.0";

                cr.id = ModCreate.GetStr(cr, "data.recipes.cooking.id");
                cr.result = new(ModCreate.GetStr(cr, "data.recipes.cooking.result.id"), (ModCreate.Get(cr, "data.recipes.cooking.result.count")?.ToInt() ?? 1).ToUShort(), new());

                ModCreate.GetFor(cr, "data.recipes.cooking.items", j =>
                {
                    CookingRecipe_Item item = LoadCookingRecipe_Item(cr, j);
                    cr.items.Add(item);
                });
            }

            return cr;
        }

        public static CookingRecipe_Item LoadCookingRecipe_Item(CookingRecipe cr, JToken j)
        {
            if (cr == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(cr)} 不能为空");
                return null;
            }
            if (j == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(j)} 不能为空");
                return null;
            }

            string id = ModCreate.GetStr(cr, "data.recipes.cooking.items.id", j);
            ushort count = (ModCreate.Get(cr, "data.recipes.cooking.items.count", j)?.ToInt() ?? 1).ToUShort();
            List<string> tags = ModCreate.Get(cr, "data.recipes.cooking.items.tags", j)?.ToObject<List<string>>() ?? new();

            CookingRecipe_Item temp = new(id, count, tags);
            return temp;
        }

        public static List<TextureData> LoadFromTextureSettings(JObject jo, string modPath)
        {
            if (jo == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 不能为空");
                return new();
            }

            var format = GetCorrectJsonFormatByJObject(jo);
            List<TextureData> datumTextures = new();
            var sb = Tools.stringBuilderPool.Get();

            //版本低于或等于 0.6.1
            if (GameTools.CompareVersions(format, "0.6.1", Operators.lessOrEqual))
            {
                var t = jo["textures"];

                if (t == null)
                {
                    Debug.LogWarning($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 中不含有 textures 项");
                    return new();
                }

                foreach (var tex in t)
                {
                    if (tex == null)
                        continue;

                    TextureData temp = new(tex["id"]?.ToString(), tex["path"]?.ToString());

                    if (string.IsNullOrWhiteSpace(temp.id) || string.IsNullOrWhiteSpace(temp.texturePath))
                        continue;

                    string path = sb.Append(ModFactory.GetTexturesPath(modPath)).Append(temp.texturePath).ToString();
                    sb.Clear();

                    if (File.Exists(path))
                    {
                        MethodAgent.TryQueueOnMainThread(() =>
                        {
                            temp.sprite = Tools.LoadSpriteByPath(path, tex["filter_mode"]?.ToString() switch
                            {
                                "Bilinear" => FilterMode.Bilinear,
                                "Trilinear" => FilterMode.Trilinear,
                                _ => FilterMode.Point
                            }, tex["pixel_unit"]?.ToInt() ?? 16);

                            datumTextures.Add(temp);
                        }, true);
                    }
                    else
                    {
                        Debug.LogWarning($"无法获取 {temp.id} 的贴图文件 {path}");
                    }
                }
            }

            Tools.stringBuilderPool.Recover(sb);
            return datumTextures;
        }

        public static List<AudioData> LoadFromAudioSettings(JObject jo, string modPath)
        {
            if (jo == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 不能为空");
                return null;
            }

            var format = GetCorrectJsonFormatByJObject(jo);
            List<AudioData> datumAudios = new();

            //版本低于或等于 0.5.1
            if (GameTools.CompareVersions(format, "0.5.1", Operators.lessOrEqual))
            {
                var au = jo["audio"];

                if (au == null)
                {
                    Debug.LogWarning($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 中不含有 audios 项");
                    return null;
                }

                for (int i = 0; i < au.Count(); i++) MethodAgent.TryRun(() =>
                {
                    var auThis = au.ElementAt(i);
                    AudioData newDatum = new()
                    {
                        id = auThis["id"]?.ToString(),
                        path = ModFactory.GetAudioPath(modPath) + auThis["path"]?.ToString(),
                        loop = auThis["loop"]?.ToBool() ?? false,
                        audioMixerType = auThis["audio_type"]?.ToString() switch
                        {
                            "ori:music" => AudioMixerType.Music,
                            "ori:ui" => AudioMixerType.UI,
                            "ori:ambient" => AudioMixerType.Ambient,
                            _ => AudioMixerType.Default,
                        },
                        volume = auThis["volume"]?.ToFloat() ?? 1,
                        earliestTime = auThis["play"]?["time_earliest"]?.ToFloat() ?? 0,
                        latestTime = auThis["play"]?["time_latest"]?.ToFloat() ?? 0,
                        probability = auThis["play"]?["probability"]?.ToFloat() ?? 100,
                    };
                    datumAudios.Add(newDatum);
                    MethodAgent.QueueOnMainThread(_ => ManagerAudio.instance.AddClipFromFile(newDatum));
                }, true);
            }

            return datumAudios;
        }

        public static KeyValuePair<BlockData, ItemData> LoadBlock(JObject jo)
        {
            if (jo == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 不能为空");
                return new(null, null);
            }

            var format = GetCorrectJsonFormatByJObject(jo);

            BlockData newBlock = new()
            {
                jsonFormat = format,
                jo = jo
            };
            ItemData newItem;

            if (GameTools.CompareVersions(format, "0.6.0", Operators.less))
            {
                newItem = null;
            }
            // 0.6.0 -> 0.?.?
            else
            {
                newBlock.jsonFormatWhenLoad = "0.6.0";
                newBlock.id = jo["ori:block"]?["id"]?.ToString();
                newBlock.defaultTexture = new(jo["ori:block"]?["display"]?["texture_id"]?.ToString());
                newBlock.description = jo["ori:block"]?["display"]?["description"]?.ToString();
                newBlock.lightLevel = jo["ori:block"]?["display"]?["lightLevel"]?.ToFloat() ?? 0;
                newBlock.hardness = jo["ori:block"]?["property"]?["hardness"]?.ToFloat() ?? BlockData.defaultHardness;
                newBlock.collidible = jo["ori:block"]?["property"]?["collidible"]?.ToBool() ?? true;

                jo["ori:block"]?["property"]?["tags"]?.For(i =>
                {
                    newBlock.tags.Add(i.ToString());
                });

                newBlock.behaviourName = jo["ori:block"]?["property"]?["behaviour"]?.ToString();

                if (jo["ori:block"]?["property"]?["drops"] == null)
                    newBlock.drops.Add(new(newBlock.id, 1));
                else
                {
                    newBlock.drops = LoadDrops(jo["ori:block"]["property"]["drops"], "0.7.1");
                }


                if (jo["ori:item"] == null)
                {
                    newItem = new(newBlock, false);
                }
                else
                {
                    newItem = LoadItem(jo);
                    newItem.id = newBlock.id;
                    newItem.description ??= newBlock.description;
                    newItem.isBlock = true;

                    if (newItem.tags.Count == 0)
                        newItem.tags = newBlock.tags;

                    if (newItem.texture == null || newItem.texture.id == null)
                        newItem.texture = newBlock.defaultTexture;
                }
            }

            return new(newBlock, newItem);
        }

        public static StructureData LoadStructure(JObject jo)
        {
            if (jo == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 不能为空");
                return null;
            }

            var format = GetCorrectJsonFormatByJObject(jo);

            StructureData temp = new()
            {
                jsonFormat = format,
                jo = jo
            };

            // 0.4.5 -> 0.4.8
            if (GameTools.CompareVersions(format, "0.6.4", Operators.lessOrEqual))
            {
                temp.jsonFormatWhenLoad = "0.6.4";

                temp.id = jo["ori:structure"]?["id"]?.ToString();
                temp.probability = jo["ori:structure"]?["generation"]?["probability"]?.ToFloat() ?? 1;
                temp.mustEnough = jo["ori:structure"]?["generation"]?["must_enough"]?.ToBool() ?? true;

                List<AttachedBlockDatum> requireBlockTemp = new();
                jo["ori:structure"]?["generation"]?["require"]?.For(i =>
                {
                    if (i["id"] != null)
                    {
                        if (i["pos"] != null && i["pos"].ToObject<int[]>().Length == 2)
                            requireBlockTemp.Add(new(i["id"]?.ToString(), new(i["pos"].ElementAt(0).ToInt(), i["pos"].ElementAt(1).ToInt()), false));
                        if (i["pos"] != null && i["pos"].ToObject<int[]>().Length == 3)
                            requireBlockTemp.Add(new(i["id"]?.ToString(), new(i["pos"].ElementAt(0).ToInt(), i["pos"].ElementAt(1).ToInt()), i["pos"].ElementAt(2).ToInt() < 0));
                        else
                            requireBlockTemp.Add(new(i["id"]?.ToString(), new(), false));
                    }
                });
                temp.require = requireBlockTemp.ToArray();

                List<AttachedBlockDatum> fixedBlockTemp = new();
                jo["ori:structure"]?["blocks"]?["fixed"]?.For(i =>
                {
                    if (i["id"] != null)
                    {
                        if (i["pos"] != null && i["pos"].ToObject<int[]>().Length == 2)
                            fixedBlockTemp.Add(new(i["id"]?.ToString(), new(i["pos"].ElementAt(0).ToInt(), i["pos"].ElementAt(1).ToInt()), false));
                        if (i["pos"] != null && i["pos"].ToObject<int[]>().Length == 3)
                            fixedBlockTemp.Add(new(i["id"]?.ToString(), new(i["pos"].ElementAt(0).ToInt(), i["pos"].ElementAt(1).ToInt()), i["pos"].ElementAt(2).ToInt() < 0));
                        else
                            fixedBlockTemp.Add(new(i["id"]?.ToString(), new(), false));
                    }
                });
                temp.fixedBlocks = fixedBlockTemp.ToArray();
            }

            return temp;
        }

        public static BiomeBlockPrefab LoadBiomeBlockPrefab(JObject jo)
        {
            if (jo == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 不能为空");
                return null;
            }

            var format = GetCorrectJsonFormatByJObject(jo);

            BiomeBlockPrefab temp = new()
            {
                jo = jo,
                jsonFormat = format,
            };

            //0.4.5 -> 0.
            string jfToLoad = string.Empty;
            if (GameTools.CompareVersions(format, "0.7.0", Operators.thanOrEqual))
            {
                jfToLoad = "0.7.0";
            }

            temp.jsonFormatWhenLoad = jfToLoad;
            temp.id = jo["prefab"]?["id"]?.ToString();
            jo["prefab"]?["content"]?.For(prefab => MethodAgent.TryRun(() =>
            {
                var block = LoadBiomeBlock(prefab, jfToLoad);
                block.isPrefab = true;

                temp.content.Add(block);
            }, true));

            return temp;
        }

        public static BiomeData_Block LoadBiomeBlock(JToken jt, string format)
        {
            if (jt == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jt)} 不能为空");
                return null;
            }

            BiomeData_Block temp = new()
            {
                jsonFormat = format,
                jt = jt,
                initialized = true
            };

            //0.4.5 -> 0.7.0
            string jfToLoad = string.Empty;
            if (GameTools.CompareVersions(format, "0.6.2", Operators.thanOrEqual))
            {
                jfToLoad = "0.6.2";
                temp.jsonFormatWhenLoad = jfToLoad;

                temp.id = ModCreate.Get(jt, "data.biome.blocks.block", jfToLoad).ToString();
                temp.rules = new()
                {
                    probability = ModCreate.Get(jt, "data.biome.blocks.rules.probability", jfToLoad)?.ToFloat() ?? 100,
                };
                temp.attached = new(ModCreate.Get(jt, "data.biome.blocks.attached.id", jfToLoad)?.ToString(), ModCreate.Get(jt, "data.biome.blocks.attached.offset", jfToLoad)?.ToVector2Int() ?? new(0, -1), ModCreate.Get(jt, "data.biome.blocks.attached.offset", jfToLoad)?.ElementAtOrDefault(2)?.ToInt() < 0);


                List<BiomeData_Block_Range> rangesTemp = new();
                ModCreate.GetFor(jt, "data.biome.blocks.ranges", jfToLoad, token =>
                {
                    var tokenStr = token.ToString();
                    var splitted = tokenStr.Split("=>");

                    if (splitted.Length == 1)
                    {
                        rangesTemp.Add(new()
                        {
                            min = splitted[0],
                            max = splitted[0]
                        });
                    }
                    else if (splitted.Length == 2)
                    {
                        rangesTemp.Add(new()
                        {
                            min = splitted[0],
                            max = splitted[1]
                        });
                    }
                });
                temp.ranges = rangesTemp.ToArray();

                List<Vector3Int> tempAreas = new();
                ModCreate.Get(jt, "data.biome.blocks.areas", jfToLoad)?.For(t => tempAreas.Add(t.ToObject<int[]>().ToVector3Int()));
                if (tempAreas.Count == 0) tempAreas.Add(Vector3Int.zero);
                temp.areas = tempAreas.ToArray();
            }

            return temp;
        }

        public static BiomeData LoadBiome(JObject jo)
        {
            if (jo == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 不能为空");
                return null;
            }

            var format = GetCorrectJsonFormatByJObject(jo);

            BiomeData temp = new()
            {
                jsonFormat = format,
                jo = jo
            };

            //0.4.5 -> 0.
            string jfToLoad = string.Empty;
            if (GameTools.CompareVersions(format, "0.6.2", Operators.thanOrEqual))
            {
                jfToLoad = "0.6.2";
                temp.jsonFormatWhenLoad = jfToLoad;

                temp.id = ModCreate.GetStr(temp, "data.biome.id");
                temp.difficulty = jo["ori:biome"]["difficulty"].ToInt();
                temp.fluctuationFrequency = jo["ori:biome"]["fluctuation"]?["frequency"]?.ToFloat() ?? 5;
                temp.fluctuationHeight = jo["ori:biome"]["fluctuation"]?["height"]?.ToFloat() ?? 8;
                temp.minSize = ModCreate.Get(temp, "data.biome.size_scope.min")?.ToVector2Int() ?? Vector2Int.zero;
                temp.maxSize = ModCreate.Get(temp, "data.biome.size_scope.max")?.ToVector2Int() ?? Vector2Int.zero;

                List<BiomeData_Block> blocksTemp = new();
                ModCreate.GetFor(temp, "data.biome.blocks", l =>
                {
                    BiomeData_Block block;

                    if (l["prefab"] == null)
                        block = LoadBiomeBlock(l, jfToLoad);
                    else
                        block = new() { id = l["prefab"].ToString(), isPrefab = true, initialized = false };

                    blocksTemp.Add(block);
                });
                temp.blocks = blocksTemp.ToArray();
                List<BiomeData_Structure> structuresTempList = new();
                ModCreate.GetFor(temp, "data.biome.structures", l =>
                {
                    structuresTempList.Add(new()
                    {
                        structure = new()
                        {
                            id = ModCreate.GetStr(temp, "data.biome.structures.id", l)
                        }
                    });
                });
                temp.structures = structuresTempList.ToArray();

                ModCreate.GetFor(temp, "data.biome.tags", i =>
                {
                    temp.tags.Add(i.ToString());
                });
            }

            return temp;
        }

        public static ItemData LoadItem(JObject jo)
        {
            if (jo == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 不能为空");
                return null;
            }

            var format = GetCorrectJsonFormatByJObject(jo);

            ItemData newItem = new();

            if (GameTools.CompareVersions(format, "0.6.0", Operators.lessOrEqual))
            {
                newItem.id = jo["ori:item"]?["id"]?.ToString();
                newItem.texture = new(jo["ori:item"]?["texture"]?.ToString());
                newItem.damage = jo["ori:item"]?["damage"]?.ToInt() ?? ItemData.defaultDamage;
                newItem.excavationStrength = jo["ori:item"]?["excavation_strength"]?.ToInt() ?? ItemData.defaultExcavationStrength;
                newItem.useCD = jo["ori:item"]?["use_cd"]?.ToFloat() ?? ItemData.defaultUseCD;
                newItem.description = jo["ori:item"]?["description"]?.ToString();
                newItem.extraDistance = jo["ori:item"]?["extra_distance"]?.ToString()?.ToFloat() ?? 0;

                if (jo["ori:item"]?["helmet"] != null)
                {
                    newItem.Helmet = new()
                    {
                        defense = jo["ori:item"]?["helmet"]?["defense"]?.ToInt() ?? 1
                    };

                    string headId = jo["ori:item"]?["helmet"]?["head"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(headId)) newItem.Helmet.head = new(headId);
                }

                if (jo["ori:item"]?["breastplate"] != null)
                {
                    newItem.Breastplate = new()
                    {
                        defense = jo["ori:item"]?["breastplate"]?["defense"]?.ToInt() ?? 1
                    };

                    string bodyId = jo["ori:item"]?["breastplate"]?["body"]?.ToString();
                    string leftArmId = jo["ori:item"]?["breastplate"]?["left_arm"]?.ToString();
                    string rightArmId = jo["ori:item"]?["breastplate"]?["right_arm"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(bodyId)) newItem.Breastplate.body = new(bodyId);
                    if (!string.IsNullOrWhiteSpace(leftArmId)) newItem.Breastplate.leftArm = new(leftArmId);
                    if (!string.IsNullOrWhiteSpace(rightArmId)) newItem.Breastplate.rightArm = new(rightArmId);
                }

                if (jo["ori:item"]?["legging"] != null)
                {
                    newItem.Legging = new()
                    {
                        defense = jo["ori:item"]?["legging"]?["defense"]?.ToInt() ?? 1
                    };

                    string leftLegId = jo["ori:item"]?["legging"]?["left_leg"]?.ToString();
                    string rightLegId = jo["ori:item"]?["legging"]?["right_leg"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(leftLegId)) newItem.Legging.leftLeg = new(leftLegId);
                    if (!string.IsNullOrWhiteSpace(rightLegId)) newItem.Legging.rightLeg = new(rightLegId);
                }

                if (jo["ori:item"]?["boots"] != null)
                {
                    newItem.Boots = new()
                    {
                        defense = jo["ori:item"]?["boots"]?["defense"]?.ToInt() ?? 1
                    };

                    string leftFootId = jo["ori:item"]?["boots"]?["left_foot"]?.ToString();
                    string rightFootId = jo["ori:item"]?["boots"]?["right_foot"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(leftFootId)) newItem.Boots.leftFoot = new(leftFootId);
                    if (!string.IsNullOrWhiteSpace(rightFootId)) newItem.Boots.rightFoot = new(rightFootId);
                }

                jo["ori:item"]?["tags"]?.For(i =>
                {
                    newItem.tags.Add(i.ToString());
                });
            }

            return newItem;
        }

        public static Spell LoadSpell(JObject jo)
        {
            if (jo == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 不能为空");
                return null;
            }

            var format = GetCorrectJsonFormatByJObject(jo);

            Spell spell = new();

            if (true)//(GameTools.CompareVersions(format, "0.7.8", Operators.lessOrEqual))
            {
                spell.id = jo["ori:spell"]?["id"]?.ToString();
                spell.cost = jo["ori:spell"]?["cost"]?.ToInt() ?? 1;
                spell.description = jo["ori:spell"]?["description"]?.ToString();

                jo["ori:spell"]?["tags"]?.For(i =>
                {
                    spell.tags.Add(i.ToString());
                });
            }

            return spell;
        }
    }
}
