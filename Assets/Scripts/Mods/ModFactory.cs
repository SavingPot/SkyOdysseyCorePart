using Cysharp.Threading.Tasks;
using GameCore.High;
using GameCore.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using SP.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameCore
{
    /// <summary>
    /// 用于加载模组
    /// </summary>
    [ChineseName("模组工厂")]
    public static class ModFactory
    {
        public static List<CraftingRecipe> GetAllCraftingRecipes()
        {
            List<CraftingRecipe> results = new();

            mods.Foreach(mod => mod.craftingRecipes.Foreach(recipe =>
                {
                    results.Add(recipe);
                }));

            return results;
        }

        public static void EachType(Action<Assembly, Type> action)
        {
            foreach (var ass in assemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    action(ass, type);
                }
            }
        }

        public static void EachMethod(Action<Assembly, Type, MethodInfo> action, BindingFlags? flags = null)
        {
            BindingFlags trueFlags = flags ?? ReflectionTools.BindingFlags_All;

            foreach (var ass in assemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    foreach (var method in type.GetMethods(trueFlags))
                    {
                        action(ass, type, method);
                    }
                }
            }
        }

        public static void EachProperty(Action<Assembly, Type, PropertyInfo> action, BindingFlags? flags = null)
        {
            BindingFlags trueFlags = flags ?? ReflectionTools.BindingFlags_All;

            foreach (var ass in assemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    foreach (var property in type.GetProperties(trueFlags))
                    {
                        action(ass, type, property);
                    }
                }
            }
        }

        public static void EachUserType(Action<Assembly, Type> action)
        {
            foreach (var ass in assemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    //排除无用的程序集, 加快加载
                    if (!IsUserType(type))
                        continue;

                    action(ass, type);
                }
            }
        }

        public static void EachUserMethod(Action<Assembly, Type, MethodInfo> action, BindingFlags? flags = null)
        {
            BindingFlags trueFlags = flags ?? ReflectionTools.BindingFlags_All;

            foreach (var ass in assemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    //排除无用的程序集, 加快加载
                    if (!IsUserType(type))
                        continue;

                    foreach (var method in type.GetMethods(trueFlags))
                    {
                        action(ass, type, method);
                    }
                }
            }
        }

        public static void EachUserProperty(Action<Assembly, Type, PropertyInfo> action, BindingFlags? flags = null)
        {
            BindingFlags trueFlags = flags ?? ReflectionTools.BindingFlags_All;

            foreach (var ass in assemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    //排除无用的程序集, 加快加载
                    if (!IsUserType(type))
                        continue;

                    foreach (var property in type.GetProperties(trueFlags))
                    {
                        action(ass, type, property);
                    }
                }
            }
        }




        public static MethodInfo SearchMethod(string targetMethod)
        {
            //遍历每个程序集中的方法
            foreach (var ass in assemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    //获取所有可用方法
                    foreach (var mtd in type.GetAllMethods())
                    {
                        if ($"{type.FullName}.{mtd.Name}" == targetMethod)
                        {
                            return mtd;
                        }
                    }
                }
            }

            return null;
        }

        public static MethodInfo SearchUserMethod(string targetMethod)
        {
            //遍历每个程序集中的方法
            foreach (var ass in assemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    //排除无用的程序集, 加快加载
                    if (!IsUserType(type))
                        continue;

                    //获取所有可用方法
                    foreach (var mtd in type.GetAllMethods())
                    {
                        if ($"{type.FullName}.{mtd.Name}" == targetMethod)
                        {
                            return mtd;
                        }
                    }
                }
            }

            return null;
        }




        public static FieldInfo SearchField(string targetField)
        {
            //遍历每个程序集中的方法
            foreach (var ass in assemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    //获取所有可用字段
                    foreach (var field in type.GetFields(ReflectionTools.BindingFlags_All))
                    {
                        if ($"{type.FullName}.{field.Name}" == targetField)
                        {
                            return field;
                        }
                    }
                }
            }

            return null;
        }

        public static FieldInfo SearchUserField(string targetField)
        {
            //遍历每个程序集中的方法
            foreach (var ass in assemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    //排除无用的程序集, 加快加载
                    if (!IsUserType(type))
                        continue;

                    //获取所有可用字段
                    foreach (var field in type.GetFields(ReflectionTools.BindingFlags_All))
                    {
                        if ($"{type.FullName}.{field.Name}" == targetField)
                        {
                            return field;
                        }
                    }
                }
            }

            return null;
        }



        public static Type SearchType(string targetType)
        {
            //遍历每个程序集中的方法
            foreach (var ass in assemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    if (type.FullName == targetType)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        public static Type SearchUserType(string targetType)
        {
            //遍历每个程序集中的方法
            foreach (var ass in assemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    if (!IsUserType(type))
                        continue;

                    if (type.FullName == targetType)
                    {
                        return type;
                    }
                }
            }

            return null;
        }


        public static bool IsUserType(Type type)
        {
            //1. type 不为空
            if (type == null)
                return false;

            //* 2. type 无命名空间则认为   是用户类型
            if (type.Namespace.IsNullOrWhiteSpace())
                return true;

            //3. 不是 System 或其子命名空间
            //4. 不是 UnityEngine 或其子命名空间
            //5. 不是 Mirror 或其子命名空间
            //5. 不是 DG.Tweening 或其子命名空间
            //5. 不是 TMPro 命名空间
            //5. 不是 SP.Tools 命名空间
            //5. 不是 SP.Tools.Unity 命名空间
            return !type.Namespace.StartsWith("System") &&
                   !type.Namespace.StartsWith("UnityEngine") &&
                   !type.Namespace.StartsWith("Mirror") &&
                   !type.Namespace.StartsWith("DG.Tweening") &&
                   type.Namespace != "TMPro" &&
                   type.Namespace != "SP.Tools.Unity" &&
                   type.Namespace != "SP.Tools";
        }













        [SerializeField, Tooltip("模组管理器识别并加载的所有模组 (包括游戏本体)"), LabelText("模组")] public static Mod[] mods;

        public static List<Assembly> assemblies = new();

        public static int modCountFound;
        public static int enabledModCount;

        public static List<FinalLang> finalTextData = new();

        public static FinalLang CompareFinalDatumText(string id)
        {
            foreach (var item in finalTextData)
            {
                if (item.id == id)
                {
                    return item;
                }
            }

            return null;
        }

        [ChineseName("在已加载的全局模组中匹配方块数据")] public static BlockData CompareBlockData(string id) => CompareBlockData(id, mods);

        [ChineseName("在指定的模组中匹配方块数据")] public static BlockData CompareBlockData(string id, Mod targetMod) => CompareBlockData(id, new[] { targetMod });

        [ChineseName("在指定的模组中匹配方块数据")] public static BlockData CompareBlockData(string id, IList<Mod> mods) => CompareModElement(id, mods, mod => mod.blocks, new(id));


        [ChineseName("在已加载的全局模组中匹配群系")] public static BiomeData CompareBiome(string id) => CompareBiome(id, mods);

        [ChineseName("在指定的模组中匹配群系")] public static BiomeData CompareBiome(string id, Mod targetMod) => CompareBiome(id, new[] { targetMod });

        [ChineseName("在指定的模组中匹配群系")] public static BiomeData CompareBiome(string id, IList<Mod> mods) => CompareModElement(id, mods, mod => mod.biomes, new() { id = id });


        [ChineseName("在已加载的全局模组中匹配结构")] public static StructureData CompareStructure(string id) => CompareStructure(id, mods);

        [ChineseName("在指定的模组中匹配结构")] public static StructureData CompareStructure(string id, Mod targetMod) => CompareStructure(id, new[] { targetMod });

        [ChineseName("在指定的模组中匹配结构")] public static StructureData CompareStructure(string id, IList<Mod> mods) => CompareModElement(id, mods, mod => mod.structures, new() { id = id });


        [ChineseName("在已加载的全局模组中匹配群系方块预置")] public static BiomeBlockPrefab CompareBiomeBlockPrefab(string id) => CompareBiomeBlockPrefab(id, mods);

        [ChineseName("在指定的模组中匹配群系方块预置")] public static BiomeBlockPrefab CompareBiomeBlockPrefab(string id, Mod targetMod) => CompareBiomeBlockPrefab(id, new[] { targetMod });

        [ChineseName("在指定的模组中匹配群系方块预置")] public static BiomeBlockPrefab CompareBiomeBlockPrefab(string id, IList<Mod> mods) => CompareModElement(id, mods, mod => mod.biomeBlockPrefabs, null);


        [ChineseName("在已加载的全局模组中匹配贴图")] public static TextureData CompareTexture(string id) => CompareTexture(id, mods);

        [ChineseName("在指定的模组中匹配贴图")] public static TextureData CompareTexture(string id, Mod targetMod) => CompareTexture(id, new[] { targetMod });

        [ChineseName("在指定的模组中匹配贴图")] public static TextureData CompareTexture(string id, IList<Mod> mods) => CompareModElement(id, mods, mod => mod.textures, new(id));


        [ChineseName("在已加载的全局模组中匹配物品")] public static ItemData CompareItem(string id) => CompareItem(id, mods);

        [ChineseName("在指定的模组中匹配物品")] public static ItemData CompareItem(string id, Mod targetMod) => CompareItem(id, new[] { targetMod });

        [ChineseName("在指定的模组中匹配物品")] public static ItemData CompareItem(string id, IList<Mod> mods) => CompareModElement(id, mods, mod => mod.items);


        [ChineseName("在已加载的全局模组中匹配物品")] public static Spell CompareSpell(string id) => CompareSpell(id, mods);

        [ChineseName("在指定的模组中匹配物品")] public static Spell CompareSpell(string id, Mod targetMod) => CompareSpell(id, new[] { targetMod });

        [ChineseName("在指定的模组中匹配物品")] public static Spell CompareSpell(string id, IList<Mod> mods) => CompareModElement(id, ModFactory.mods, mod => mod.spells);


        [ChineseName("在已加载的全局模组中匹配文本")] public static GameLang CompareLang(string id) => CompareLang(id, mods);

        [ChineseName("在指定的模组中匹配文本")] public static GameLang CompareLang(string id, Mod targetMod) => CompareLang(id, new[] { targetMod });

        [ChineseName("在指定的模组中匹配文本")] public static GameLang CompareLang(string id, IList<Mod> mods) => CompareModElement(id, mods, mod => mod.langs);


        [ChineseName("在已加载的全局模组中匹配实体")] public static EntityData CompareEntity(string id) => CompareEntity(id, mods);

        [ChineseName("在指定的模组中匹配实体")] public static EntityData CompareEntity(string id, Mod targetMod) => CompareEntity(id, new[] { targetMod });

        [ChineseName("在指定的模组中匹配实体")] public static EntityData CompareEntity(string id, IList<Mod> mods) => CompareModElement(id, mods, mod => mod.entities);


        [ChineseName("在已加载的全局模组中匹配合成配方")] public static CraftingRecipe CompareCraftingRecipe(string id) => CompareCraftingRecipe(id, mods);

        [ChineseName("在指定的模组中匹配合成配方")] public static CraftingRecipe CompareCraftingRecipe(string id, Mod targetMod) => CompareCraftingRecipe(id, new[] { targetMod });

        [ChineseName("在指定的模组中匹配合成配方")] public static CraftingRecipe CompareCraftingRecipe(string id, IList<Mod> mods) => CompareModElement(id, mods, mod => mod.craftingRecipes);


        [ChineseName("在已加载的全局模组中匹配烹饪配方")] public static CookingRecipe CompareCookingRecipe(string id) => CompareCookingRecipe(id, mods);

        [ChineseName("在指定的模组中匹配烹饪配方")] public static CookingRecipe CompareCookingRecipe(string id, Mod targetMod) => CompareCookingRecipe(id, new[] { targetMod });

        [ChineseName("在指定的模组中匹配烹饪配方")] public static CookingRecipe CompareCookingRecipe(string id, IList<Mod> mods) => CompareModElement(id, mods, mod => mod.cookingRecipes);



        [ChineseName("在模组中匹配项目")]
        public static T CompareModElement<T>(string id, IList<Mod> mods, Func<Mod, IList<T>> elementsInMod, T defaultValue = null) where T : class, IStringId
        {
            if (id.IsNullOrWhiteSpace())
            {
                Debug.LogWarning($"{MethodGetter.GetLastMethodName()}: {nameof(id)} 不应为空");
                return defaultValue;
            }

            //模组查找时倒序的, 因此是优先遍历模组, 其次是原版
            for (int i = mods.Count - 1; i >= 0; i--)
            {
                IList<T> collection = elementsInMod(mods[i]);

                foreach (var element in collection)
                {
                    if (element.id == id)
                    {
                        return element;
                    }
                }
            }

            Debug.LogWarning($"{MethodGetter.GetLastMethodName()}: 未找到项目 {id}");
            return defaultValue;
        }

        public static List<string> GetIds(IStringId[] classes)
        {
            List<string> ids = new();

            foreach (var item in classes)
                ids.Add(item.id);

            return ids;
        }

        #region 获取目录
        public static string GetInfoPath(string modPath) => Path.Combine(modPath, "mod_info.json");
        public static string GetIconPath(string modPath) => Path.Combine(modPath, "icon.png");
        public static string GetScriptsPath(string modPath) => Path.Combine(modPath, "scripts");
        public static string GetEntitiesPath(string modPath) => Path.Combine(modPath, "entities");
        public static string GetStructurePath(string modPath) => Path.Combine(modPath, "structures");
        public static string GetBlocksPath(string modPath) => Path.Combine(modPath, "blocks");
        public static string GetTerrainBlocksPath(string modPath) => Path.Combine(GetBlocksPath(modPath), "terrains");
        public static string GetWallBlocksPath(string modPath) => Path.Combine(GetBlocksPath(modPath), "walls");
        public static string GetRegionThemesPath(string modPath) => Path.Combine(modPath, "region_themes");
        public static string GetBiomePath(string modPath) => Path.Combine(modPath, "biomes");
        public static string GetBiomePrefabPath(string modPath) => Path.Combine(GetBiomePath(modPath), "prefabs");
        public static string GetBiomeBlockPrefabPath(string modPath) => Path.Combine(GetBiomePrefabPath(modPath), "blocks");
        public static string GetTextureSettingsPath(string modPath) => Path.Combine(modPath, "texture_settings.json");
        public static string GetAudioSettingsPath(string modPath) => Path.Combine(modPath, "audio_settings.json");
        public static string GetTexturesPath(string modPath) => Path.Combine(modPath, "textures");
        public static string GetAudioPath(string modPath) => Path.Combine(modPath, "audio");
        public static string GetLangsPath(string modPath) => Path.Combine(modPath, "langs");
        public static string GetItemsPath(string modPath) => Path.Combine(modPath, "items");
        public static string GetSpellsPath(string modPath) => Path.Combine(modPath, "spells");
        public static string GetRecipesPath(string modPath) => Path.Combine(modPath, "recipes");
        public static string GetCraftingRecipesPath(string modPath) => Path.Combine(GetRecipesPath(modPath), "crafting");
        public static string GetCookingRecipesPath(string modPath) => Path.Combine(GetRecipesPath(modPath), "cooking");

        /// <summary>
        /// 不会添加 Icon
        /// </summary>
        /// <param name="modPath"></param>
        //TODO: Say goodbye
        public static void CreateAllFilesAndDir(string modPath)
        {
            string modInfoPath = GetInfoPath(modPath);

            string modAudiosPath = GetAudioPath(modPath);
            string modTextsPath = GetLangsPath(modPath);
            string modTexturesPath = GetTexturesPath(modPath);
            string modAudioSettingsPath = GetAudioSettingsPath(modPath);
            string modTextureSettingsPath = GetTextureSettingsPath(modPath);

            string modCraftingRecipesPath = GetCraftingRecipesPath(modPath);
            string modScriptsPath = GetScriptsPath(modPath);
            string modBlocksPath = GetBlocksPath(modPath);
            string modBiomesPath = GetBiomePath(modPath);
            string modEntitiesPath = GetEntitiesPath(modPath);
            string modItemsPath = GetItemsPath(modPath);
            string modStructurePath = GetStructurePath(modPath);

            IOTools.CreateDirectoryIfNone(modPath);

            IOTools.CreateFileIfNone(modInfoPath).Dispose();

            IOTools.CreateDirsIfNone(modAudiosPath, modTextsPath, modTexturesPath);
            IOTools.CreateFilesIfNone(modAudioSettingsPath, modTextureSettingsPath);

            IOTools.CreateDirsIfNone(modCraftingRecipesPath, modScriptsPath, modBlocksPath, modBiomesPath, modEntitiesPath, modItemsPath, modStructurePath);
        }
        #endregion

        public static void ReloadMods(Action afterReload)
        {
            //TODO: 优化寻址逻辑
            Debug.Log("开始重加载所有模组");

            //初始化资源
            assemblies.Clear();

            if (!Directory.Exists(GInit.modsPath))
            {
                Directory.CreateDirectory(GInit.modsPath);
                Debug.Log("不存在模组文件夹，程序已自动创建");
            }

            Debug.Log($"模组路径:\n{GInit.modsPath}\n{Path.Combine(GInit.soleAssetsPath, "mods")}");




            //先在独占资源中寻找模组
            List<string> folders = IOTools.GetFoldersInFolder(Path.Combine(GInit.soleAssetsPath, "mods"), true).ToList();

            //把 ori 排在第一个
            for (int i = 0; i < folders.Count; i++)
            {
                var folder = folders[i];

                if (IOTools.GetDirectoryName(folder) == "ori")
                {
                    folders.Remove(folder);
                    folders.Insert(0, folder);
                }
            }

            //然后在共享资源中寻找模组
            folders.AddRange(IOTools.GetFoldersInFolder(GInit.modsPath, true));




            //处理日志信息
            StringBuilder foldersFound = Tools.stringBuilderPool.Get();

            foldersFound.Append("寻找模组文件夹完成, 找到以下文件夹\n");

            folders.For(item => foldersFound.AppendLine(item));

            Debug.Log(foldersFound);
            Tools.stringBuilderPool.Recover(foldersFound);



            //获取模组文件夹并分出有 info 与无 info 的文件夹
            string[] modPathsWithInfo = folders.Where(p => File.Exists(GetInfoPath(p))).ToArray();
            string[] modPathsWithoutInfo = folders.Where(p => !File.Exists(GetInfoPath(p))).ToArray();
            string[] modPathsEnabled = modPathsWithInfo.Where(p => ModLoading.LoadInfo(GetInfoPath(p), null).enabled).ToArray();

            modCountFound = modPathsWithInfo.Length;
            enabledModCount = modPathsEnabled.Length;


            //加载模组
            mods = new Mod[enabledModCount];
            for (int i = 0; i < enabledModCount; i++)
            {
                string modPath = modPathsEnabled[i];
                LoadMod(modPath, i);
                //Task loading = new(() => LoadMod(modPathsWithInfo[i]));

                //loading.Start();
                //loading.Wait();
            }





            //处理日志信息
            StringBuilder sb = Tools.stringBuilderPool.Get();
            sb.Append("模组加载完毕, 列表如下:\n");
            //TODO: 显示加载失败的模组

            foreach (var folderPath in modPathsWithInfo)
            {
                string folderName = IOTools.GetDirectoryName(folderPath);
                sb.AppendLine($"<color=#00FF00>{folderName}</color> ({folderPath})");
            }

            foreach (var folderPath in modPathsWithoutInfo)
            {
                string folderName = IOTools.GetDirectoryName(folderPath);
                sb.AppendLine($"<color=red>{folderName}</color> ({folderPath})");
            }

            sb.AppendLine(string.Empty);
            sb.Append("启用的模组数量: ");
            sb.Append(mods.Length);
            sb.Append("/");
            sb.AppendLine(modPathsWithInfo.Length.ToString());
            sb.Append(" (没有信息文件的模组有 ");
            sb.Append(modPathsWithoutInfo.Length);
            sb.AppendLine(" 个)");
            // sb.AppendLine("具体信息如下\n\n");
            // for (int i = 0; i < mods.Count; i++)
            // {
            //     //输出模组基本信息
            //     sb.AppendLine(mods[i].ToString());
            //     sb.AppendLine("\n\n");
            // }
            Debug.Log(sb);
            Tools.stringBuilderPool.Recover(sb);





            //重新配置所有模组
            ReconfigureAllMods();




            //完成
            MethodAgent.RunOnMainThread(_ =>
            {
                afterReload?.Invoke();
                Performance.CollectMemory();
            });
        }

        public static void AddToFinalText(GameLang text)
        {
            if (CompareFinalDatumText(text.id) == null)
            {
                finalTextData.Add(new(text));
            }
            else
            {
                foreach (var data in finalTextData)
                {
                    if (data.id == text.id)
                    {
                        data.AddTexts(text);
                    }
                }
            }
        }

        [ChineseName("加载模组")]
        public static void LoadMod(string modPath, int modIndex)
        {
            string folderName = IOTools.GetDirectoryName(modPath);

            string infoPath = GetInfoPath(modPath);
            string iconPath = GetIconPath(modPath);
            string scriptsPath = GetScriptsPath(modPath);

            string entitiesPath = GetEntitiesPath(modPath);

            string craftingRecipesPath = GetCraftingRecipesPath(modPath);
            string cookingRecipesPath = GetCookingRecipesPath(modPath);

            string structurePath = GetStructurePath(modPath);
            string blocksPath = GetBlocksPath(modPath);
            //string terrainBlocksPath = GetTerrainBlocksPath(modPath);
            //string wallBlocksPath = GetWallBlocksPath(modPath);
            string regionThemesPath = GetRegionThemesPath(modPath);
            string biomesPath = GetBiomePath(modPath);
            string biomePrefabPath = GetBiomePrefabPath(modPath);
            string biomeBlockPrefabPath = GetBiomeBlockPrefabPath(modPath);

            string audiosPath = GetAudioPath(modPath);
            string textureSettingsPath = GetTextureSettingsPath(modPath);
            string audioSettingsPath = GetAudioSettingsPath(modPath);

            string langsPath = GetLangsPath(modPath);

            string itemsPath = GetItemsPath(modPath);

            string spellsPath = GetSpellsPath(modPath);

            //Debug.Log("尝试加载新模组");

            MethodAgent.DebugRun(() =>
            {
                Mod newMod = new();

                #region 初始化
                //加载Json文件并赋值
                newMod.info = ModLoading.LoadInfo(infoPath, iconPath);

                newMod.folderName = folderName;
                newMod.path = modPath;

                if (newMod.isOri)
                    newMod.info.version = GInit.gameVersion;
                #endregion

                #region 加载纹理
                if (File.Exists(textureSettingsPath) && JsonUtils.IsJsonByPath(textureSettingsPath))
                {
                    //加载贴图设置
                    JObject jo = JsonUtils.LoadJObjectByPath(textureSettingsPath);

                    newMod.textures = ModLoading.LoadFromTextureSettings(jo, modPath);

                    #region 添加 Unknown 贴图
                    if (newMod.isOri)
                    {
                        newMod.textures.Add(GInit.instance.textureUnknown);
                    }
                    #endregion
                }
                #endregion

                #region 加载方块
                if (Directory.Exists(blocksPath))
                {
                    List<string> blockPaths = IOTools.GetFilesInFolderIncludingChildren(blocksPath, true, "json");

                    for (int b = 0; b < blockPaths.Count; b++) MethodAgent.DebugRun(() =>
                    {
                        var blockPair = ModLoading.LoadBlock(blockPaths[b]);

                        if (blockPair.Key != null && blockPair.Value != null)
                        {
                            newMod.blocks.Add(blockPair.Key);
                            newMod.items.Add(blockPair.Value);
                        }
                    });

                    //if (Directory.Exists(terrainTilesPath)) MethodAgent.RunInTry(() =>
                    //{
                    //    string[] terrainTilePaths = Tools.GetFileNamesInFolder(terrainTilesPath, true, "json");

                    //    for (int b = 0; b < terrainTilePaths.Length; b++)
                    //    {
                    //        var format = GetCorrectJsonFormatByPath(terrainTilePaths[b]);

                    //        TerrainTileInfo newInfo = new();
                    //        DatumItemBase newItem = new();

                    //        if (Tools.CompareVersion(format, "0.5.1", Tools.OperatorType.lessOrEqual))
                    //        {
                    //            JObject jo = Tools.LoadJObjectByPath(terrainTilePaths[b]);
                    //            newInfo.id = jo?["ori:tile"]?["inherent_components"]?["ori:id"]?.ToString();
                    //            newInfo.defaultTextureId = jo?["ori:tile"]?["inherent_components"]?["ori:id_tile"]?["display"]?["default_texture_id"]?.ToString();
                    //            newInfo.hardness = jo?["ori:tile"]?["inherent_components"]?["ori:id_tile"]?["property"]?["hardness"].ToString().ToFloat() ?? 0;
                    //            var transition_tile_data = jo?["ori:tile"]?["inherent_components"]?["ori:terrain_tile"]?["transition_tile_data"];

                    //            if (transition_tile_data != null)
                    //                foreach (var item in transition_tile_data)
                    //                {
                    //                    newInfo.transitionTileData.Add(new()
                    //                    {
                    //                        tiledSpriteId = item?["tiled_sprite"]?.ToString(),
                    //                        angularSpriteId = item?["angular_sprite"]?.ToString(),
                    //                        edgeSpriteId = item?["edge_sprite"]?.ToString(),
                    //                        missingOneFaceSpriteId = item?["missing_one_face_sprite"]?.ToString(),
                    //                        missingTwoFaceSpriteId = item?["missing_two_face_sprite"]?.ToString(),
                    //                        intersectingTileId = item?["intersecting_tile"]?.ToString(),
                    //                        active = Convert.ToBoolean(item?["active"]?.ToObject<bool>()),
                    //                    });
                    //                }

                    //            newItem.id = newInfo.id + "_item";
                    //            newItem.textureId = newInfo.defaultTextureId;
                    //        }

                    //        newMod.terrainTileInfos.Add(newInfo);
                    //        newMod.items.Add(newItem);
                    //    }
                    //}, true);

                    //if (Directory.Exists(wallTilesPath))
                    //{
                    //    string[] wallTilePaths = Tools.GetFileNamesInFolder(wallTilesPath, true, "json");

                    //    for (int b = 0; b < wallTilePaths.Length; b++) MethodAgent.RunInTry(() =>
                    //    {
                    //        var format = GetCorrectJsonFormatByPath(wallTilePaths[b]);

                    //        WallTileInfo newInfo = new();
                    //        DatumItemBase newItem = new();

                    //        if (Tools.CompareVersion(format, "0.5.1", Tools.OperatorType.lessOrEqual))
                    //        {
                    //            JObject jo = Tools.LoadJObjectByPath(wallTilePaths[b]);
                    //            newInfo.id = jo?["ori:tile"]?["ori:id"]?.ToString();
                    //            newInfo.defaultTextureId = jo?["ori:tile"]?["ori:id_tile"]?["display"]?["default_texture_id"]?.ToString();
                    //            newInfo.hardness = jo?["ori:tile"]?["ori:id_tile"]?["property"]?["hardness"]?.ToString()?.ToFloat() ?? 0;
                    //            newInfo.sideSpriteId = jo?["ori:tile"]?["ori:wall_tile"]?["display"]?["side_texture_id"]?.ToString();

                    //            newItem.id = newInfo.id + "_item";
                    //            newItem.textureId = newInfo.defaultTextureId;
                    //        }

                    //        newMod.wallTileInfos.Add(newInfo);
                    //        newMod.items.Add(newItem);
                    //    }, true);
                    //}
                }
                #endregion

                //加载结构
                LoadModSubitem(structurePath, newMod.structures, path => ModLoading.LoadStructure(path));

                //加载群系方块预制体
                LoadModSubitem(biomeBlockPrefabPath, newMod.biomeBlockPrefabs, path => ModLoading.LoadBiomeBlockPrefab(path));

                //加载群系
                LoadModSubitem(biomesPath, newMod.biomes, path => ModLoading.LoadBiome(path), false);

                #region 加载文本
                if (Directory.Exists(langsPath))
                {
                    string[] textPaths = IOTools.GetFilesInFolder(langsPath, true, "json");

                    for (int b = 0; b < textPaths.Length; b++) MethodAgent.DebugRun(() =>
                    {
                        //加载文本数据
                        JObject jo = JsonUtils.LoadJObjectByPath(textPaths[b]);

                        GameLang newText = ModLoading.LoadText(jo);
                        AddToFinalText(newText);
                        newMod.langs.Add(newText);
                    });
                }
                #endregion

                #region 加载音频
                if (File.Exists(audioSettingsPath) && JsonUtils.IsJsonByPath(audioSettingsPath))
                {
                    //加载音频设置
                    JObject jo = JsonUtils.LoadJObjectByPath(audioSettingsPath);

                    newMod.audios = ModLoading.LoadFromAudioSettings(jo, modPath);
                }
                #endregion

                //加载物品
                LoadModSubitem(itemsPath, newMod.items, path => ModLoading.LoadItem(path));

                //加载魔咒
                LoadModSubitem(spellsPath, newMod.spells, path => ModLoading.LoadSpell(path));

                //加载合成表
                LoadModSubitem(craftingRecipesPath, newMod.craftingRecipes, path => ModLoading.LoadCraftingRecipe(path));

                //加载菜谱
                LoadModSubitem(cookingRecipesPath, newMod.cookingRecipes, path => ModLoading.LoadCookingRecipe(path));

                #region 加载脚本 (Dll)
                List<Type> importTypesTemp = new();
                if (Directory.Exists(scriptsPath))
                {
                    string[] dllPaths = IOTools.GetFilesInFolder(scriptsPath, true, "dll");



                    static void LoadDLLInternal(Type[] types, string dllPath, List<Type> importTypes)
                    {
                        foreach (Type type in types)
                        {
                            importTypes.Add(type);
                        }
                    }



                    foreach (var dllPath in dllPaths)
                    {
                        MethodAgent.DebugRun(() =>
                        {
                            //Assembly.LoadFrom 也会顺带加载需要的程序集, 这会导致程序集被多次加载, 因此使用 LoadFile
                            var ass = Assembly.LoadFile(dllPath);
                            Debug.Log($"加载了程序集 {dllPath}");

                            assemblies.Add(ass);
                            LoadDLLInternal(ass.GetTypes(), dllPath, importTypesTemp);
                        });
                    }

                    if (newMod.isOri)
                    {
                        assemblies.Add(Tools.coreAssembly);
                        LoadDLLInternal(Tools.coreAssembly.GetTypes(), string.Empty, importTypesTemp);
                    }
                }
                newMod.importedTypes = importTypesTemp.ToArray();
                #endregion

                #region 加载实体
                if (Directory.Exists(entitiesPath))
                {
                    string[] entityPaths = IOTools.GetFilesInFolderIncludingChildren(entitiesPath, true, "json").ToArray();

                    Array.ForEach(entityPaths, p => MethodAgent.DebugRun(() =>
                    {
                        //加载文本数据
                        JObject jo = JsonUtils.LoadJObjectByPath(p);
                        EntityData newEntity = ModLoading.LoadEntity(jo, p);

                        newMod.entities.Add(newEntity);
                    }));
                }
                #endregion


                newMod.FixWrongPart();
                mods[modIndex] = newMod;


                #region 添加内置 UI
                if (newMod.isOri)
                {
                    MethodAgent.RunOnMainThread(() =>
                    {
                        GScene.Next();
                        MethodAgent.QueueOnMainThread(_ => Tools.NewObjectToComponent(typeof(InternalUIAdder)));
                    });
                }
                #endregion

                //调用 ModEntry 的 OnModLoaded
                MethodAgent.DebugRun(() => CallOnModLoaded(newMod));
            });
        }

        private static void LoadModSubitem<T>(string directoryPath, List<T> subitemList, Func<string, T> LoadSubitem, bool includingChildren = true)
        {
            if (Directory.Exists(directoryPath))
            {
                string[] subitemPaths = includingChildren ? IOTools.GetFilesInFolderIncludingChildren(directoryPath, true, "json").ToArray() :
                                                            IOTools.GetFilesInFolder(directoryPath, true, "json");

                for (int i = 0; i < subitemPaths.Length; i++) MethodAgent.DebugRun(() =>
                {
                    var temp = LoadSubitem(subitemPaths[i]);

                    if (temp != null)
                        subitemList.Add(temp);
                });
            }
        }

        public static Mod GetMod(string id)
        {
            foreach (var mod in mods)
                if (mod.info.id == id)
                    return mod;

            return null;
        }

        public static bool TryGetMod(string id, out Mod result)
        {
            result = GetMod(id);

            return result != null;
        }

        public static void ReconfigureAllMods()
        {
            //重配置要在主线程运行
            MethodAgent.DebugQueueOnMainThread(() =>
            {
                //遍历所有模组并重新配置
                for (int i = 0; i < mods.Length; i++)
                {
                    var mod = mods[i];
                    ReconfigureMod(ref mod);
                    mods[i] = mod;
                }
            });
        }

        internal static void CallOnModLoaded(Mod mod) => CallModEntryMethod(mod, entry => entry.OnLoaded());

        internal static void CallOnModReconfigured(Mod mod) => CallModEntryMethod(mod, entry => entry.OnReconfigured());

        internal static void CallModEntryMethod(Mod mod, Action<ModEntry> call) => MethodAgent.DebugQueueOnMainThread(() =>
        {
            if (mod.entryInstance == null)
                GetModEntry(mod);

            if (mod.entryInstance == null)
                return;

            mod.entryInstance.datum = mod;

            call(mod.entryInstance);
        });

        public static void GetModEntry(Mod mod)
        {
            //遍历加载的所有 Type
            foreach (Type importType in mod.importedTypes)
            {
                //如果不为空并继承自 ModEntry 就执行指定方法
                if (importType != null && importType.IsSubclassOf(typeof(ModEntry)))
                {
                    mod.entryInstance = (ModEntry)Activator.CreateInstance(importType, null);
                    return;
                }
            }
        }

        static void GetEntityBehaviourType(EntityData entity)
        {
            foreach (Mod mod in mods)
            {
                foreach (Type type in mod.importedTypes)
                {
                    //如果检测到 EntityBindingAttribute
                    if (AttributeGetter.TryGetAttribute(type, out EntityBindingAttribute attribute))
                    {
                        //如果继承自 Entity (包括间接继承)
                        if (type.IsSubclassOf(typeof(Entity)))
                        {
                            //如果 id 相同
                            if (attribute.entityId == entity.id)
                            {
                                entity.behaviourType = type;

                                return;
                            }
                        }
                        else if (!type.IsSubclassOf(typeof(EntitySave)))
                        {
                            Debug.LogError($"实体数据 {entity.id} 所绑定的实体行为类不继承于 {nameof(Entity)} 或 {nameof(EntitySave)}");
                            return;
                        }
                    }
                }
            }

            Debug.LogError($"实体数据 {entity.id} 没有绑定到任何实体行为类 (请使用 EntityBindingAttribute 来绑定)");
        }

        static void GetItemBehaviour(ItemData item)
        {
            foreach (Mod mod in mods)
            {
                foreach (Type type in mod.importedTypes)
                {
                    if (type.IsSubclassOf(typeof(ItemBehaviour)) &&
                        AttributeGetter.TryGetAttribute<ItemBindingAttribute>(type, out var attribute) &&
                        attribute.id == item.id)
                    {
                        item.behaviourType = type;
                        return;
                    }
                }
            }

            item.behaviourType = typeof(ItemBehaviour);
        }

        static void GetSpellBehaviour(Spell spell)
        {
            foreach (Mod mod in mods)
            {
                foreach (Type type in mod.importedTypes)
                {
                    if (type.IsSubclassOf(typeof(SpellBehaviour)) &&
                        AttributeGetter.TryGetAttribute<SpellBindingAttribute>(type, out var attribute) &&
                        attribute.id == spell.id)
                    {
                        spell.behaviourType = type;
                        return;
                    }
                }
            }

            spell.behaviourType = typeof(SpellBehaviour);
        }

        static void GetBlockBehaviour(BlockData block)
        {
            if (block.behaviourName.IsNullOrWhiteSpace())
                return;

            foreach (Mod mod in mods)
            {
                foreach (Type type in mod.importedTypes)
                {
                    if (type.FullName != block.behaviourName)
                        continue;

                    if (type.IsSubclassOf(typeof(Block)))
                    {
                        block.behaviourType = type;
                        return;
                    }
                    else
                    {
                        Debug.LogError($"方块 {block.id} 所绑定的行为类 {block.behaviourName} 不继承于 {nameof(Block)}");
                    }
                }
            }

            Debug.LogError($"方块 {block.id} 所绑定的行为包 {block.behaviourName} 无法找到");
        }

        public static void ReconfigureMod(ref Mod mod)
        {
            //将实体的 type 都清空重新匹配
            foreach (var entityData in mod.entities)
            {
                entityData.behaviourType = null;
                GetEntityBehaviourType(entityData);
            }

            //将物品的 type 都清空重新匹配
            for (int c = 0; c < mod.items.Count; c++)
            {
                ItemData item = mod.items[c];

                GetItemBehaviour(item);

                item.texture = CompareTexture(item.texture.id);

                if (item.Helmet != null)
                {
                    if (item.Helmet.head != null)
                        item.Helmet.head = CompareTexture(item.Helmet.head.id);
                }

                if (item.Breastplate != null)
                {
                    if (item.Breastplate.body != null)
                        item.Breastplate.body = CompareTexture(item.Breastplate.body.id);

                    if (item.Breastplate.leftArm != null)
                        item.Breastplate.leftArm = CompareTexture(item.Breastplate.leftArm.id);

                    if (item.Breastplate.rightArm != null)
                        item.Breastplate.rightArm = CompareTexture(item.Breastplate.rightArm.id);
                }

                if (item.Legging != null)
                {
                    if (item.Legging.leftLeg != null)
                        item.Legging.leftLeg = CompareTexture(item.Legging.leftLeg.id);

                    if (item.Legging.rightLeg != null)
                        item.Legging.rightLeg = CompareTexture(item.Legging.rightLeg.id);
                }

                if (item.Boots != null)
                {
                    if (item.Boots.leftFoot != null)
                        item.Boots.leftFoot = CompareTexture(item.Boots.leftFoot.id);

                    if (item.Boots.rightFoot != null)
                        item.Boots.rightFoot = CompareTexture(item.Boots.rightFoot.id);
                }
            }

            foreach (var spell in mod.spells)
            {
                GetSpellBehaviour(spell);
            }

            foreach (var biome in mod.biomes)
            {
                //匹配生态群系中的结构
                for (int b = 0; b < biome.structures.Length; b++)
                {
                    biome.structures[b].structure = CompareStructure(biome.structures[b].structure.id);
                }

                //将方块预制体转为实际的方块
                List<BiomeData_Block> toInit = new();
                foreach (var block in biome.blocks)
                {
                    //如果是预制体而且没被初始化 (即转为实际方块)
                    if (block.isPrefab && !block.initialized)
                    {
                        //添加至候选列表
                        toInit.Add(block);
                    }
                }

                List<BiomeData_Block> blocksTemp = biome.blocks.ToList();
                //将候选列表中的预制体匹配并添加回去
                foreach (BiomeData_Block init in toInit)
                {
                    var compared = CompareBiomeBlockPrefab(init.id);

                    //若果匹配到了就添加实际方块
                    if (compared != null)
                    {
                        foreach (BiomeData_Block content in compared.content)
                        {
                            blocksTemp.Add(content);
                        }

                        blocksTemp.Remove(init);
                    }
                }
                biome.blocks = blocksTemp.ToArray();
            }

            foreach (var block in mod.blocks)
            {
                block.defaultTexture = CompareTexture(block.defaultTexture.id);

                block.behaviourType = null;
                GetBlockBehaviour(block);
            }

            //调用 ModEntry 的 OnModReconfigured
            CallOnModReconfigured(mod);
        }

        public static string GetJsonFormat(string json) => ModLoading.GetJsonFormat(json);

        public static string GetJsonFormat(JObject jo) => ModLoading.GetJsonFormat(jo);

        public static string GetJsonFormatByPath(string path) => GetJsonFormat(File.ReadAllText(path));

        public static string GetCorrectJsonFormatByPath(string path) => GetCorrectJsonFormatByJObject(JsonUtils.LoadJObjectByPath(path) ?? new());

        public static string GetCorrectJsonFormatByJObject(JObject jo) => ModLoading.GetCorrectJsonFormatByJObject(jo);

        public static void ConvertJsonFormat(ref string jsonFormat) => ModLoading.ConvertJsonFormat(ref jsonFormat);
    }

    [Serializable]
    public class IdTileInfoADS : IdClassBase
    {
        [LabelText("默认贴图Id")] public string defaultTextureId;
        [LabelText("硬度")] public float hardness = 10;
    }

    [Serializable]
    public class TerrainTileInfoADS : IdTileInfoADS
    {
        //[LabelText("交接瓦片的数据")] public List<TransitionTileDatum> transitionTileData = new();
    }

    [Serializable]
    public class WallTileInfoADS : IdTileInfoADS
    {
        [LabelText("边缘贴图ID")] public string sideSpriteId;
    }

    [Serializable]
    public class JsonFormatClassBase
    {
        [JsonProperty("json_format")] public string jsonFormat;
    }


    [Serializable]
    public class ModClass : IdClassBase, IJOFormatCore
    {
        [LabelText("Json 格式")] public string jsonFormat;
        [LabelText("加载时的 Json 格式")] public string jsonFormatWhenLoad;

        string IJsonFormat.jsonFormat { get => jsonFormat; set => jsonFormat = value; }
        string IJsonFormatWhenLoad.jsonFormatWhenLoad { get => jsonFormatWhenLoad; set => jsonFormatWhenLoad = value; }
        public JObject jo { get; set; }
    }

    [Serializable]
    public class ModClassChild : IdClassBase, IJOFormatCoreChild
    {
        [LabelText("Json 格式")] public string jsonFormat;
        [LabelText("加载时的 Json 格式")] public string jsonFormatWhenLoad;

        string IJsonFormat.jsonFormat { get => jsonFormat; set => jsonFormat = value; }
        string IJsonFormatWhenLoad.jsonFormatWhenLoad { get => jsonFormatWhenLoad; set => jsonFormatWhenLoad = value; }
        public JToken jt { get; set; }
    }


    [Serializable]
    public sealed class TextureData : IdClassBase
    {
        [LabelText("纹理路径")] public string texturePath;
        [NonSerialized, LabelText("加载好的精灵")] public Sprite sprite;

        public TextureData()
        {

        }

        public TextureData(string id)
        {
            this.id = id;
            this.sprite = GInit.instance.textureUnknown.sprite;
        }

        public TextureData(string id, string texturePath)
        {
            this.id = id;
            this.texturePath = texturePath;
            this.sprite = GInit.instance.textureUnknown.sprite;
        }

        public TextureData(string id, Sprite sprite)
        {
            this.id = id;
            this.sprite = sprite;
        }

        public override string ToString()
        {
            StringBuilder sb = Tools.stringBuilderPool.Get();
            string indentation = "   ";

            sb.AppendLine("{");

            sb.Append(indentation).Append("id: ").AppendLine(id);
            sb.Append(indentation).Append("path: ").AppendLine(texturePath);
            sb.Append(indentation).Append("is-sprite-null: ").AppendLine((sprite == null).ToString());

            sb.AppendLine("}");

            string content = sb.ToString();
            Tools.stringBuilderPool.Recover(sb);
            return content;
        }
    }

    [Serializable]
    public struct ImportedType
    {
        [JsonIgnore] public Type type;
        [JsonIgnore, LabelText("Dll路径")] public string dllPath;
    }

    [Serializable]
    public class IdClassBase : IStringId
    {
        [LabelText("ID")] public string id;
        string IId<string>.id { get => id; }
    }

    [Serializable]
    public sealed class Mod_Info : ModClass, IJOFormatCore
    {
        public string version;
        public string description;
        public string name;
        public bool enabled;
        public Sprite icon;
        public bool isOri => id == "ori";
    }

    [Serializable]
    public sealed class FinalLang : IdClassBase
    {
        public List<GameLang_Text> texts = new();
        public string textName;

        public GameLang_Text CompareOrCreateText(string id)
        {
            for (int i = 0; i < texts.Count; i++)
                if (texts[i].id == id)
                    return texts[i];

            GameLang_Text text = new() { id = id, text = id };
            return text;
        }

        public bool TryCompareText(string id, out GameLang_Text text)
        {
            if (id.IsNullOrWhiteSpace())
                goto failure;

            foreach (var t in texts)
                if (t.id == id)
                {
                    text = t;
                    return true;
                }

            failure:
            text = null;
            return false;
        }

        public FinalLang(GameLang datumText)
        {
            id = datumText.id;
            texts = new(datumText.texts);
            textName = datumText.textName;
        }

        public void AddTexts(GameLang datumText)
        {
            foreach (var t in datumText.texts)
            {
                texts.Add(t);
            }
        }
    }

    [Serializable]
    public class GameLang : IdClassBase, IJOFormatCore
    {
        public List<GameLang_Text> texts = new();
        [LabelText("名称")] public string textName;

        public GameLang_Text CompareText(string id)
        {
            for (int i = 0; i < texts.Count; i++)
                if (texts[i].id == id)
                    return texts[i];

            return null;
        }


        [LabelText("JF")] public string jsonFormat;
        [LabelText("加载时的 JF")] public string jsonFormatWhenLoad;

        string IJsonFormat.jsonFormat { get => jsonFormat; set => jsonFormat = value; }
        string IJsonFormatWhenLoad.jsonFormatWhenLoad { get => jsonFormatWhenLoad; set => jsonFormatWhenLoad = value; }
        public JObject jo { get; set; }
    }

    [Serializable]
    public class GameLang_Text : IdClassBase
    {
        [JsonProperty(propertyName: "text"), LabelText("文本内容")] public string text;
    }
}
