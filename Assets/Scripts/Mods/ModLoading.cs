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
using System.Security.Cryptography;
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
        public static bool LoadModClass<T>(string path, string entranceId, out T obj, out JToken entrance, bool ignoreIdCheck = false) where T : ModClass, new()
        {
            JObject jo = JsonUtils.LoadJObjectByPath(path) ?? new();
            var format = GetCorrectJsonFormatByJObject(jo);
            entrance = jo[entranceId];

            //如果入口为空或者不是对象类型
            if (entrance == null || entrance.Type != JTokenType.Object)
            {
                obj = null;
                Debug.LogError($"{MethodGetter.GetLastMethodName()}: {path} 的 json 文件中不包含 {entranceId} 或者 {entranceId} 不是对象");
                return false;
            }

            var entranceIdToken = entrance["id"];
            string entranceIdTokenAsString = null;

            //检查 Id
            if (!ignoreIdCheck)
            {
                //如果入口中不包含 id
                if (entranceIdToken == null)
                {
                    obj = null;
                    Debug.LogError($"{MethodGetter.GetLastMethodName()}: {path} json 文件的 {entranceId} 中必须包含 id");
                    return false;
                }

                entranceIdTokenAsString = entranceIdToken.ToString();

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
            }

            //初始化结果实例
            obj = new()
            {
                jsonFormat = format,
                jo = jo,
                id = entranceIdTokenAsString,
            };

            return true;
        }



        public static Mod_Info LoadInfo(string path, string iconPath)
        {
            if (LoadModClass(path, "ori:mod_info", out Mod_Info info, out JToken entrance, true))
            {
                if (true)// (GameTools.CompareVersions(format, "0.6.4", Operators.less))
                {
                    info.jsonFormatWhenLoad = "0.6.4";

                    info.id = entrance["id"].ToString();
                    info.enabled = entrance["enabled"].ToBool();
                    info.version = entrance["version"]?.ToString();
                    info.description = entrance["display"]["description"].ToString();
                    info.name = entrance["display"]["name"].ToString();
                }


                if (!iconPath.IsNullOrWhiteSpace() && File.Exists(iconPath))
                {
                    MethodAgent.RunOnMainThread(_ => info.icon = Tools.LoadSpriteByPath(iconPath));
                }
                else
                {
                    info.icon = GInit.instance.textureUnknown.sprite;
                }
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

        public static DropData[] LoadDrops(JToken jt, string jsonFormat)
        {
            List<DropData> ts = new();

            if (jt == null)
            {
                //Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {nameof(jt)} 不能为空");
                return ts.ToArray();
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

            return ts.ToArray();
        }

        public static EntityData LoadEntity(string path)
        {
            if (LoadModClass(path, "ori:entity", out EntityData entity, out var entrance))
            {
                //if (GameTools.CompareVersions(format, "0.5.1", Operators.lessOrEqual))
                if (true)
                {
                    entity.jsonFormatWhenLoad = "0.7.0";

                    entity.drops = LoadDrops(entrance["drops"], "0.7.1");
                    entity.speed = entrance["speed"]?.ToFloat() ?? 3;
                    entity.coinCount = entrance["coin_count"]?.ToInt() ?? 1;
                    entity.colliderSize = entrance["physics"]?["collider"]?["size"]?.ToVector2() ?? Vector2.one;
                    entity.colliderOffset = entrance["physics"]?["collider"]?["offset"]?.ToVector2() ?? Vector2.zero;
                    entity.gravity = entrance["physics"]?["gravity"]?.ToFloat() ?? 7;
                    entity.maxHealth = entrance["max_health"]?.ToInt() ?? Entity.DEFAULT_HEALTH;
                    entity.lifetime = entrance["lifetime"]?.ToFloat() ?? EntityData.defaultLifetime;

                    entity.searchRadius = entrance["search_radius"]?.ToObject<ushort>() ?? 25;
                    entity.searchRadiusSqr = entity.searchRadius * entity.searchRadius;
                    if (entrance.TryGetJToken("normal_attack", out var normalAttack))
                    {
                        entity.normalAttack = new()
                        {
                            radius = normalAttack["radius"]?.ToFloat() ?? 2,
                            damage = normalAttack["damage"]?.ToInt() ?? 15,
                            warningTime = normalAttack["warning_time"]?.ToFloat() ?? 0.8f,
                            dodgeTime = normalAttack["dodge_time"]?.ToFloat() ?? 0.3f,
                            hitJudgementTime = normalAttack["hit_judgement_time"]?.ToFloat() ?? 0.2f,
                            recoveryTime = normalAttack["recovery_time"]?.ToFloat() ?? 1f,
                        };
                    }

                    entity.summon.biome = entrance["summon"]?["biome"]?.ToString();
                    entity.summon.defaultProbability = entrance["summon"]?["default_probability"]?.ToString().ToFloat() ?? 100;
                    entity.summon.timeEarliest = entrance["summon"]?["time_earliest"]?.ToString().ToFloat() ?? 0;
                    entity.summon.timeLatest = entrance["summon"]?["time_latest"]?.ToString().ToFloat() ?? 0;
                }
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

                    newRecipe.facility = cr["facility"]?.ToString();
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

                    newRecipe.facility = cr["facility"]?.ToString() ?? BlockID.SoupPot;
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

        public static FishingResult LoadFishingResult(string path)
        {
            if (LoadModClass(path, "ori:fishing_result", out FishingResult result, out var entrance))
            {
                result.jsonFormatWhenLoad = "0.7.9";

                result.result = entrance["result"]?.ToString();
                result.biome = entrance["biome"]?.ToString();
                result.probability = entrance["probability"]?.ToFloat() ?? 100;

                if (result.result.IsNullOrWhiteSpace())
                    Debug.Log($"钓鱼结果错误, 需指定 result 项, 请检查 {path}");
            }

            return result;
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
                    Debug.LogError($"{MethodGetter.GetCurrentMethodName()}: {path} 的 json 文件格式不正确, 请更新到 0.6.0 或以上版本");
                }
                // 0.6.0 -> 0.?.?
                else
                {
                    var property = entrance["property"];

                    newBlock.jsonFormatWhenLoad = "0.6.0";

                    if (entrance.TryGetJToken("display", out var display))
                    {
                        newBlock.lightLevel = display["light_level"]?.ToFloat() ?? 0;
                        newBlock.description = display["description"]?.ToString();
                    }

                    //如果指定 texture 就是 texture, 不指定 texture 就是 id
                    newBlock.defaultTexture = new(display?["texture_id"]?.ToString() ?? newBlock.id);

                    //如果不指定介绍就按照 id 自动生成
                    if (newBlock.description == null)
                    {
                        (var jtModId, var jtProjectName) = Tools.SplitModIdAndName(newBlock.id);
                        newBlock.description ??= $"{jtModId}:description.{jtProjectName}";
                    }

                    newBlock.hardness = property?["hardness"]?.ToFloat() ?? BlockData.defaultHardness;
                    newBlock.collidible = property?["collidible"]?.ToBool() ?? true;
                    newBlock.behaviourName = property?["behaviour"]?.ToString();
                    property?["tags"]?.For(i =>
                    {
                        newBlock.tags.Add(i.ToString());
                    });
                    if (property?["drops"] == null)
                        newBlock.drops = new DropData[] { new(newBlock.id, 1) };
                    else
                        newBlock.drops = LoadDrops(property?["drops"], "0.7.1");



                    if (newBlock.jo["ori:item"] == null)
                    {
                        newItem = new(newBlock, false);
                    }
                    else
                    {
                        newItem = LoadItem(path, true);
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

        public static StructureData LoadStructure(string path)
        {
            if (LoadModClass(path, "ori:structure", out StructureData temp, out var entrance))
            {
                // 0.6.4 -> _
                if (GameTools.CompareVersions(temp.jsonFormat, "0.6.4", Operators.thanOrEqual))
                {
                    temp.jsonFormatWhenLoad = "0.6.4";

                    temp.probability = entrance["generation"]?["probability"]?.ToFloat() ?? 1;
                    temp.mustEnough = entrance["generation"]?["must_enough"]?.ToBool() ?? true;

                    List<AttachedBlockDatum> requireBlockTemp = new();
                    entrance["generation"]?["require"]?.For(i =>
                    {
                        if (i["pos"] != null && i["pos"].ToObject<int[]>().Length == 2)
                            requireBlockTemp.Add(new(i["id"]?.ToString(), new(i["pos"].ElementAt(0).ToInt(), i["pos"].ElementAt(1).ToInt()), false));
                        else if (i["pos"] != null && i["pos"].ToObject<int[]>().Length == 3)
                            requireBlockTemp.Add(new(i["id"]?.ToString(), new(i["pos"].ElementAt(0).ToInt(), i["pos"].ElementAt(1).ToInt()), i["pos"].ElementAt(2).ToInt() < 0));
                        else
                            requireBlockTemp.Add(new(i["id"]?.ToString(), Vector2Int.zero, false));
                    });
                    temp.require = requireBlockTemp.ToArray();

                    List<AttachedBlockDatum> fixedBlockTemp = new();
                    entrance["blocks"]?["fixed"]?.For(i =>
                    {
                        if (i["pos"] != null && i["pos"].ToObject<int[]>().Length == 2)
                            fixedBlockTemp.Add(new(i["id"]?.ToString(), new(i["pos"].ElementAt(0).ToInt(), i["pos"].ElementAt(1).ToInt()), false));
                        else if (i["pos"] != null && i["pos"].ToObject<int[]>().Length == 3)
                            fixedBlockTemp.Add(new(i["id"]?.ToString(), new(i["pos"].ElementAt(0).ToInt(), i["pos"].ElementAt(1).ToInt()), i["pos"].ElementAt(2).ToInt() < 0));
                        else
                            fixedBlockTemp.Add(new(i["id"]?.ToString(), new(), false));
                    });
                    temp.fixedBlocks = fixedBlockTemp.ToArray();
                }
            }

            return temp;
        }

        public static BiomeBlockPrefab LoadBiomeBlockPrefab(string path)
        {
            if (LoadModClass(path, "prefab", out BiomeBlockPrefab temp, out var entrance))
            {
                //0.7.0 -> 0.~~
                if (GameTools.CompareVersions(temp.jsonFormat, "0.7.0", Operators.thanOrEqual))
                {
                    temp.jsonFormatWhenLoad = "0.7.0";
                    entrance["content"]?.For(prefab => MethodAgent.DebugRun(() =>
                    {
                        var block = LoadBiomeBlock(prefab, temp.jsonFormatWhenLoad);
                        block.isPrefab = true;

                        temp.content.Add(block);
                    }));
                }
            }


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
                        StringExtensions.SplitIntervalFormulaIntoMinMaxFormula(blockToken["range"]?.ToString(), out var minFormula, out var maxFormula);

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
                Enum.TryParse(ModCreate.Get(jt, "data.biome.blocks.status", jfToLoad)?.ToString(), out temp.status);


                List<BiomeData_Block_Range> rangesTemp = new();
                ModCreate.GetFor(jt, "data.biome.blocks.ranges", jfToLoad, token =>
                {
                    var tokenStr = token.ToString();
                    StringExtensions.SplitIntervalFormulaIntoMinMaxFormula(tokenStr, out var minFormula, out var maxFormula);

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


        public static BiomeData LoadBiome(string path)
        {
            if (LoadModClass(path, "ori:biome", out BiomeData temp, out var entrance))
            {
                //* 0.6.2 -> ~
                if (GameTools.CompareVersions(temp.jsonFormat, "0.6.2", Operators.thanOrEqual))
                {
                    temp.jsonFormatWhenLoad = "0.6.2";
                    temp.isFightingBiome = entrance["is_fighting_biome"]?.ToBool() ?? false;
                    temp.minScale = entrance["size_scope"]?["min"]?.ToVector2() ?? new Vector2(0.4f, 0.35f);
                    temp.maxScale = entrance["size_scope"]?["max"]?.ToVector2() ?? new Vector2(0.55f, 0.4f);

                    List<BiomeData_Block> blocksTemp = new();
                    entrance["blocks"]?.For(blockJT =>
                    {
                        BiomeData_Block block;

                        if (blockJT["prefab"] == null)
                        {
                            block = LoadBiomeBlock(blockJT, temp.jsonFormatWhenLoad);
                        }
                        else
                        {
                            block = new() { id = blockJT["prefab"].ToString(), isPrefab = true, initialized = false };
                        }

                        blocksTemp.Add(block);
                    });
                    temp.blocks = blocksTemp.ToArray();

                    List<BiomeData_Structure> structuresTempList = new();
                    entrance["structures"]?.For(structJT =>
                    {
                        structuresTempList.Add(new(
                            new()
                            {
                                id = structJT["id"].ToString()
                            }
                        ));
                    });
                    temp.structures = structuresTempList.ToArray();

                    entrance["tags"]?.For(tagJT =>
                    {
                        temp.tags.Add(tagJT.ToString());
                    });
                }
            }

            return temp;
        }

        public static ItemData LoadItem(string path, bool ignoreIdCheck = false)
        {
            if (LoadModClass(path, "ori:item", out ItemData newItem, out var jt, ignoreIdCheck))
            {
                newItem.damage = jt["damage"]?.ToInt() ?? ItemData.defaultDamage;
                newItem.excavationStrength = jt["excavation_strength"]?.ToInt() ?? ItemData.defaultExcavationStrength;
                newItem.useCD = jt["use_cd"]?.ToFloat() ?? ItemData.defaultUseCD;
                newItem.extraDistance = jt["extra_distance"]?.ToString()?.ToFloat() ?? 0;



                if (jt.TryGetJToken("economy", out var economy))
                {
                    newItem.economy.worth = economy["worth"]?.ToInt() ?? 0;
                }

                if (GameTools.CompareVersions(newItem.jsonFormat, "0.7.8", Operators.thanOrEqual))
                {
                    newItem.jsonFormatWhenLoad = "0.7.8";

                    if (jt.TryGetJToken("display", out var display))
                    {
                        newItem.description = display["description"]?.ToString();
                        newItem.size = display["size"]?.ToVector2() ?? EntityInventoryOwnerBehaviour.defaultItemLocalScale;
                        newItem.offset = display["offset"]?.ToVector2() ?? Vector2.zero;
                        newItem.rotation = display["rotation"]?.ToInt() ?? 0;
                    }

                    //如果不指定 texture 就是 id
                    var textureJT = display?["texture"];
                    newItem.texture = new(textureJT != null ? textureJT.ToString() : newItem.id);
                }
                else
                {
                    newItem.jsonFormatWhenLoad = "0.5.1";

                    newItem.texture = new(jt["texture"]?.ToString());
                    newItem.description = jt["description"]?.ToString();
                    newItem.size = Vector2.one;
                    newItem.offset = Vector2.zero;
                }


                jt["tags"]?.For(i =>
                {
                    newItem.tags.Add(i.ToString());
                });







                if (jt.TryGetJToken("helmet", out var helmet))
                {
                    newItem.Helmet = new()
                    {
                        defense = helmet["defense"]?.ToInt() ?? 1
                    };

                    string headId = helmet["head"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(headId)) newItem.Helmet.head = new(headId);
                }

                if (jt.TryGetJToken("breastplate", out var breastplate))
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

                if (jt.TryGetJToken("legging", out var legging))
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

                if (jt.TryGetJToken("boots", out var boots))
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

                if (jt.TryGetJToken("shield", out var shield))
                {
                    newItem.Shield = new()
                    {
                        parryTime = shield["parry_time"]?.ToFloat() ?? 0.2f,
                        parryCD = shield["parry_cd"]?.ToFloat() ?? 0.8f,
                    };
                }
            }

            return newItem;
        }

        public static Spell LoadSpell(string path)
        {
            if (LoadModClass(path, "ori:spell", out Spell temp, out var entrance))
            {
                if (true)//(GameTools.CompareVersions(format, "0.7.8", Operators.lessOrEqual))
                {
                    temp.id = entrance?["id"]?.ToString();
                    temp.cost = entrance?["cost"]?.ToInt() ?? 1;
                    temp.description = entrance?["description"]?.ToString();

                    entrance?["tags"]?.For(i =>
                    {
                        temp.tags.Add(i.ToString());
                    });
                }
            }

            return temp;
        }
    }
}
