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



        /// <returns>加载是否成功</returns>
        public static bool LoadModClass<T>(string path, string entranceId, out T obj, out JToken entrance) where T : ModClass, new()
        {
            JObject jo = JsonTools.LoadJObjectByPath(path);
            var format = GetCorrectJsonFormatByJObject(jo);
            entrance = jo[entranceId];
            var entranceIdToken = entrance["id"];

            //如果入口为空或者不是对象类型
            if (entrance == null && entrance.Type != JTokenType.Object)
            {
                obj = null;
                Debug.LogError($"{MethodGetter.GetLastMethodName()}: {path} 的 json 文件中不包含 {entranceId} 或者 {entranceId} 不是对象");
                return false;
            }

            //如果入口中不包含 id
            if (entranceIdToken == null)
            {
                obj = null;
                Debug.LogError($"{MethodGetter.GetLastMethodName()}: {path} json 文件的 {entranceId} 中必须包含 id");
                return false;
            }

            var entranceIdTokenAsString = entranceIdToken.ToString();

            //如果入口中 id 为空
            if (string.IsNullOrWhiteSpace(entranceIdTokenAsString))
            {
                obj = null;
                Debug.LogError($"{MethodGetter.GetLastMethodName()}: {path} json 文件的 {entranceId} 中的 id 为空");
                return false;
            }

            //检查 id 的格式
            if (Tools.SplitIdBySeparator(entranceIdTokenAsString).Length != 2)
            {
                obj = null;
                Debug.LogError($"{MethodGetter.GetLastMethodName()}: {path} json 文件的 {entranceId} 中 id 的格式不正确, 必须为 \"mod_id:project_name\"");
                return false;
            }

            //初始化结果实例
            obj = new()
            {
                jsonFormat = format,
                jo = jo,
                id = entranceIdTokenAsString
            };

            return true;
        }



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

                    temp.id = t["id"]?.ToString();
                    temp.count = Convert.ToByte(t["count"]?.ToString()?.ToInt() ?? 1);
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
                entity.colliderOffset = jo["ori:entity"]?["physics"]?["collider"]?["offset"]?.ToVector2() ?? Vector2.zero;
                entity.gravity = jo["ori:entity"]?["physics"]?["gravity"]?.ToFloat() ?? 7;
                entity.maxHealth = jo["ori:entity"]?["max_health"]?.ToInt() ?? Entity.DEFAULT_HEALTH;
                entity.lifetime = jo["ori:entity"]?["lifetime"]?.ToFloat() ?? EntityData.defaultLifetime;

                entity.searchRadius = jo["ori:entity"]?["search_radius"]?.ToObject<ushort>() ?? 25;
                entity.searchRadiusSqr = entity.searchRadius * entity.searchRadius;
                entity.normalAttackRadius = jo["ori:entity"]?["normal_attack_radius"]?.ToFloat() ?? 2;
                entity.normalAttackDamage = jo["ori:entity"]?["normal_attack_damage"]?.ToInt() ?? 15;
                entity.normalAttackCD = jo["ori:entity"]?["normal_attack_cd"]?.ToFloat() ?? 2;

                entity.summon.region = jo["ori:entity"]?["summon"]?["region"]?.ToString();
                entity.summon.defaultProbability = jo["ori:entity"]?["summon"]?["default_probability"]?.ToString().ToFloat() ?? 100;
                entity.summon.timeEarliest = jo["ori:entity"]?["summon"]?["time_earliest"]?.ToString().ToFloat() ?? 0;
                entity.summon.timeLatest = jo["ori:entity"]?["summon"]?["time_latest"]?.ToString().ToFloat() ?? 0;
            }

            return entity;
        }

        public static CraftingRecipe LoadCraftingRecipe(string path)
        {
            if (LoadModClass(path, "ori:crafting_recipe", out CraftingRecipe newRecipe, out var cr))
            {
                if (GameTools.CompareVersions(newRecipe.jsonFormat, "0.6.0", Operators.thanOrEqual))
                {
                    newRecipe.jsonFormatWhenLoad = "0.6.0";

                    newRecipe.result = new(cr["result"]["id"].ToString(), (cr["result"]["count"]?.ToInt() ?? 1).ToUShort(), new());

                    List<CraftingRecipe_Item> ingredients = new();
                    cr["ingredients"].Foreach(j =>
                    {
                        string id = j["id"]?.ToString();
                        ushort count = (j["count"]?.ToInt() ?? 1).ToUShort();
                        List<string> tags = j["tags"]?.ToObject<List<string>>() ?? new();

                        ingredients.Add(new(id, count, tags));
                    });
                    newRecipe.ingredients = ingredients.ToArray();
                }
            }

            return newRecipe;
        }

        public static CookingRecipe LoadCookingRecipe(string path)
        {
            if (LoadModClass(path, "ori:cooking_recipe", out CookingRecipe newRecipe, out var cr))
            {
                if (GameTools.CompareVersions(newRecipe.jsonFormat, "0.6.0", Operators.thanOrEqual))
                {
                    newRecipe.jsonFormatWhenLoad = "0.6.0";

                    newRecipe.type = cr["type"]?.ToString() ?? "ori:poach";
                    newRecipe.needBowl = cr["need_bowl"]?.ToBool() ?? false;
                    newRecipe.result = new(cr["result"]["id"].ToString(), (cr["result"]["count"]?.ToInt() ?? 1).ToUShort(), new());

                    List<CookingRecipe_Item> ingredients = new();
                    cr["ingredients"].Foreach(j =>
                    {
                        string id = j["id"]?.ToString();
                        ushort count = (j["count"]?.ToInt() ?? 1).ToUShort();
                        List<string> tags = j["tags"]?.ToObject<List<string>>() ?? new();

                        ingredients.Add(new(id, count, tags));
                    });
                    newRecipe.ingredients = ingredients.ToArray();
                }
            }

            return newRecipe;
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
                        MethodAgent.DebugQueueOnMainThread(() =>
                        {
                            temp.sprite = Tools.LoadSpriteByPath(path, tex["filter_mode"]?.ToString() switch
                            {
                                "Bilinear" => FilterMode.Bilinear,
                                "Trilinear" => FilterMode.Trilinear,
                                _ => FilterMode.Point
                            }, tex["pixel_unit"]?.ToInt() ?? 16);

                            datumTextures.Add(temp);
                        });
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

                for (int i = 0; i < au.Count(); i++) MethodAgent.DebugRun(() =>
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
                });
            }

            return datumAudios;
        }

        public static KeyValuePair<BlockData, ItemData> LoadBlock(string path)
        {
            ItemData newItem = null;

            if (LoadModClass(path, "ori:block", out BlockData newBlock, out var entrance))
            {
                if (GameTools.CompareVersions(newBlock.jsonFormat, "0.6.0", Operators.less))
                {
                    newItem = null;
                }
                // 0.6.0 -> 0.?.?
                else
                {
                    newBlock.jsonFormatWhenLoad = "0.6.0";
                    newBlock.description = entrance["display"]?["description"]?.ToString();
                    newBlock.lightLevel = entrance["display"]?["light_level"]?.ToFloat() ?? 0;
                    newBlock.hardness = entrance["property"]?["hardness"]?.ToFloat() ?? BlockData.defaultHardness;
                    newBlock.collidible = entrance["property"]?["collidible"]?.ToBool() ?? true;

                    //如果不指定介绍
                    (var jtModId, var jtProjectName) = Tools.SplitModIdAndName(newBlock.id);
                    newBlock.description ??= $"{jtModId}:description.{jtProjectName}";

                    //如果指定 texture 就是 texture, 不指定 texture 就是 id
                    newBlock.defaultTexture = new(entrance["display"]?["texture_id"]?.ToString() ?? newBlock.id);


                    entrance["property"]?["tags"]?.For(i =>
                    {
                        newBlock.tags.Add(i.ToString());
                    });

                    newBlock.behaviourName = entrance["property"]?["behaviour"]?.ToString();

                    if (entrance["property"]?["drops"] == null)
                        newBlock.drops.Add(new(newBlock.id, 1));
                    else
                    {
                        newBlock.drops = LoadDrops(entrance["property"]["drops"], "0.7.1");
                    }


                    if (newBlock.jo["ori:item"] == null)
                    {
                        newItem = new(newBlock, false);
                    }
                    else
                    {
                        newItem = LoadItem(newBlock.jo);
                        newItem.id = newBlock.id;
                        newItem.description ??= newBlock.description;
                        newItem.isBlock = true;

                        if (newItem.tags.Count == 0)
                            newItem.tags = newBlock.tags;

                        if (newItem.texture == null || newItem.texture.id == null)
                            newItem.texture = newBlock.defaultTexture;
                    }
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
                        else if (i["pos"] != null && i["pos"].ToObject<int[]>().Length == 3)
                            requireBlockTemp.Add(new(i["id"]?.ToString(), new(i["pos"].ElementAt(0).ToInt(), i["pos"].ElementAt(1).ToInt()), i["pos"].ElementAt(2).ToInt() < 0));
                        else
                            requireBlockTemp.Add(new(i["id"]?.ToString(), Vector2Int.zero, false));
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

        public static RegionTheme LoadRegionTheme(JObject jo)
        {
            if (jo == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jo)} 不能为空");
                return null;
            }

            var format = GetCorrectJsonFormatByJObject(jo);

            RegionTheme temp = new()
            {
                jo = jo,
                jsonFormat = format,
            };

            //0.4.5 -> 0.
            string jfToLoad = string.Empty;
            if (GameTools.CompareVersions(format, "0.7.8", Operators.thanOrEqual))
            {
                jfToLoad = "0.7.8";
            }

            temp.jsonFormatWhenLoad = jfToLoad;

            if (jfToLoad == "0.7.8")
            {
                temp.id = jo["ori:region_theme"]?["id"]?.ToString();
                temp.distribution = jo["ori:region_theme"]?["distribution"]?.ToString()?.ToInt() ?? 0;
                temp.biomes = jo["ori:region_theme"]?["biomes"]?.ToObject<string[]>() ?? new string[] { BiomeID.Center };
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
            jo["prefab"]?["content"]?.For(prefab => MethodAgent.DebugRun(() =>
            {
                var block = LoadBiomeBlock(prefab, jfToLoad);
                block.isPrefab = true;

                temp.content.Add(block);
            }));

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
                temp.type = (jt["type"] ?? "direct").ToString();

                if (temp.type == "perlin")
                {
                    List<BiomeData_Block_Perlin_Block> perlinBlocks = new();

                    jt["blocks"]?.Foreach(blockToken =>
                    {
                        IntervalFormulaToMinMaxFormula(blockToken["range"]?.ToString(), out var minFormula, out var maxFormula);

                        perlinBlocks.Add(new()
                        {
                            minFormula = minFormula,
                            maxFormula = maxFormula,
                            block = blockToken["block"]?.ToString(),
                            isBackground = blockToken["isBackground"]?.ToBool() ?? false,
                        });
                    });

                    temp.perlin = new()
                    {
                        fluctuationFrequency = jt["fluctuation"]?["frequency"]?.ToFloat() ?? 5,
                        fluctuationHeight = jt["fluctuation"]?["height"]?.ToFloat() ?? 8,
                        startYFormula = jt["start_y"]?.ToString(),
                        blocks = perlinBlocks.ToArray()
                    };
                }

                jfToLoad = "0.6.2";
                temp.jsonFormatWhenLoad = jfToLoad;

                temp.id = ModCreate.Get(jt, "data.biome.blocks.block", jfToLoad)?.ToString();
                temp.rules = new()
                {
                    probability = ModCreate.Get(jt, "data.biome.blocks.rules.probability", jfToLoad)?.ToFloat() ?? 100,
                };
                temp.attached = new(ModCreate.Get(jt, "data.biome.blocks.attached.id", jfToLoad)?.ToString(), ModCreate.Get(jt, "data.biome.blocks.attached.offset", jfToLoad)?.ToVector2Int() ?? new(0, -1), ModCreate.Get(jt, "data.biome.blocks.attached.offset", jfToLoad)?.ElementAtOrDefault(2)?.ToInt() < 0);


                List<BiomeData_Block_Range> rangesTemp = new();
                ModCreate.GetFor(jt, "data.biome.blocks.ranges", jfToLoad, token =>
                {
                    var tokenStr = token.ToString();
                    IntervalFormulaToMinMaxFormula(tokenStr, out var minFormula, out var maxFormula);

                    rangesTemp.Add(new()
                    {
                        minFormula = minFormula,
                        maxFormula = maxFormula
                    });
                });
                temp.ranges = rangesTemp.ToArray();

                List<Vector3Int> tempAreas = new();
                ModCreate.Get(jt, "data.biome.blocks.areas", jfToLoad)?.For(t => tempAreas.Add(t.ToObject<int[]>().ToVector3Int()));
                if (tempAreas.Count == 0) tempAreas.Add(Vector3Int.zero);
                temp.areas = tempAreas.ToArray();
            }

            return temp;
        }

        //TODO: Move it to SP.Tools
        public static void IntervalFormulaToMinMaxFormula(string intervalFormula, out string minFormula, out string maxFormula)
        {
            var splitted = intervalFormula.Split("=>");

            if (splitted.Length == 1)
            {
                minFormula = splitted[0];
                maxFormula = minFormula;
            }
            else if (splitted.Length == 2)
            {
                minFormula = splitted[0];
                maxFormula = splitted[1];
            }
            else
            {
                throw new();
            }
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
                temp.minSize = ModCreate.Get(temp, "data.biome.size_scope.min")?.ToVector2Int() ?? Vector2Int.zero;
                temp.maxSize = ModCreate.Get(temp, "data.biome.size_scope.max")?.ToVector2Int() ?? Vector2Int.zero;

                List<BiomeData_Block> blocksTemp = new();
                ModCreate.GetFor(temp, "data.biome.blocks", l =>
                {
                    BiomeData_Block block;

                    if (l["prefab"] == null)
                    {
                        block = LoadBiomeBlock(l, jfToLoad);
                    }
                    else
                    {
                        block = new() { id = l["prefab"].ToString(), isPrefab = true, initialized = false };
                    }

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



            var jt = jo["ori:item"];

            if (jt == null)
            {
                Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: 有一个物品 json 不包含 \"ori:item\" 对象");
                return null;
            }





            ItemData newItem = new();
            var format = GetCorrectJsonFormatByJObject(jo);
            var helmet = jt["helmet"];
            var breastplate = jt["breastplate"];
            var legging = jt["legging"];
            var boots = jt["boots"];





            newItem.id = jt["id"]?.ToString();
            newItem.damage = jt["damage"]?.ToInt() ?? ItemData.defaultDamage;
            newItem.excavationStrength = jt["excavation_strength"]?.ToInt() ?? ItemData.defaultExcavationStrength;
            newItem.useCD = jt["use_cd"]?.ToFloat() ?? ItemData.defaultUseCD;
            newItem.description = jt["description"]?.ToString();
            newItem.extraDistance = jt["extra_distance"]?.ToString()?.ToFloat() ?? 0;




            if (GameTools.CompareVersions(format, "0.7.8", Operators.less))
            {
                newItem.texture = new(jt["texture"]?.ToString());
            }
            else
            {
                var display = jt["display"];

                newItem.texture = new(display?["texture"]?.ToString());
            }

            //如果不指定 texture 就是 id
            newItem.texture ??= ModFactory.CompareTexture(newItem.id);





            if (helmet != null)
            {
                newItem.Helmet = new()
                {
                    defense = helmet["defense"]?.ToInt() ?? 1
                };

                string headId = helmet["head"]?.ToString();
                if (!string.IsNullOrWhiteSpace(headId)) newItem.Helmet.head = new(headId);
            }

            if (breastplate != null)
            {
                newItem.Breastplate = new()
                {
                    defense = breastplate["defense"]?.ToInt() ?? 1
                };

                string bodyId = breastplate["body"]?.ToString();
                string leftArmId = breastplate["left_arm"]?.ToString();
                string rightArmId = breastplate["right_arm"]?.ToString();
                if (!string.IsNullOrWhiteSpace(bodyId)) newItem.Breastplate.body = new(bodyId);
                if (!string.IsNullOrWhiteSpace(leftArmId)) newItem.Breastplate.leftArm = new(leftArmId);
                if (!string.IsNullOrWhiteSpace(rightArmId)) newItem.Breastplate.rightArm = new(rightArmId);
            }

            if (legging != null)
            {
                newItem.Legging = new()
                {
                    defense = legging?["defense"]?.ToInt() ?? 1
                };

                string leftLegId = legging?["left_leg"]?.ToString();
                string rightLegId = legging?["right_leg"]?.ToString();
                if (!string.IsNullOrWhiteSpace(leftLegId)) newItem.Legging.leftLeg = new(leftLegId);
                if (!string.IsNullOrWhiteSpace(rightLegId)) newItem.Legging.rightLeg = new(rightLegId);
            }

            if (boots != null)
            {
                newItem.Boots = new()
                {
                    defense = boots["defense"]?.ToInt() ?? 1
                };

                string leftFootId = boots["left_foot"]?.ToString();
                string rightFootId = boots["right_foot"]?.ToString();
                if (!string.IsNullOrWhiteSpace(leftFootId)) newItem.Boots.leftFoot = new(leftFootId);
                if (!string.IsNullOrWhiteSpace(rightFootId)) newItem.Boots.rightFoot = new(rightFootId);
            }

            jt["tags"]?.For(i =>
            {
                newItem.tags.Add(i.ToString());
            });

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
