using Sirenix.OdinInspector;
using SP.Tools.Unity;
using SP.Tools;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System;
using UnityEngine;

namespace GameCore
{
    public static class GFiles
    {
        public static GameSettings settings;
        public static World world;
        public static Action SaveAllBlockDataToFiles = () => { };
        public static Action OnSaveAllDataToFiles = () => { Debug.Log("保存了所有数据至文件"); };





        public static void CreateWorld(int seed, string name, bool isShortTerm, float requiredTotalTime = GTime.timeOneDay * 3)
        {
            SaveAllDataToFiles();
            world = new(seed, name, isShortTerm, requiredTotalTime);
            SaveAllDataToFiles();
        }

        public static void CheckPaths()
        {
            //创建不存在的必要文件夹
            IOTools.CreateDirectoryIfNone(GInit.savesPath);
            IOTools.CreateDirectoryIfNone(GInit.worldPath);
        }


        public static void SaveFileString(string savePath, string text, bool compress = false)
        {
            if (compress)
                text = Compressor.CompressString(text);

            SaveFileContext(savePath, text);
        }

        public static void SaveFileJson(string savePath, object obj, bool compress = false, bool prettyJson = false)
        {
            SaveFileContext(savePath, JsonUtils.ToJson(obj, compress, prettyJson));
        }

        static void SaveFileContext(string savePath, string context)
        {
            CheckPaths();

            IOTools.CreateDirectoryIfNone(Path.GetDirectoryName(savePath));

            //使用 StreamWriter 将文本内容写入路径
            StreamWriter sw = File.CreateText(savePath);

            sw.Write(context);
            sw.Close();
        }

        public static GameSettings GameSettingsOnFile()
        {
            if (File.Exists(GInit.settingsPath))
            {
                var datum = JsonUtils.LoadTypeFromJsonPath<GameSettings>(GInit.settingsPath);

                return datum ?? new(true);
            }
            else
            {
                return new(true);
            }
        }

        public static void SaveAllDataToFiles()
        {
            CheckPaths();

            //保存世界, 仅有服务器保存
            if (world != null)
            {
                //将时间写入
                world.basicData.time = GTime.time;
                world.basicData.isAM = GTime.isMorning;
                world.basicData.totalTime = GTime.totalTime;

                //将方块数据写入
                SaveAllBlockDataToFiles();

                //应用实体数据
                ApplyEntityDataToWorld();

                //将世界数据写入文件
                IOTools.CreateDirsIfNone(World.GetCachePath(world.worldPath), World.GetDisplayCachePath(world.worldPath));
                SaveFileJson(World.GetBasicDataPath(world.worldPath), world.basicData, false, true);
                SaveFileJson(World.GetRegionDataPath(world.worldPath), world.regionData, false, false);
                SaveFileJson(World.GetPlayerDataPath(world.worldPath), world.playerSaves, false, true);
            }

            //保存设置文件
            SaveFileJson(GInit.settingsPath, settings, false, true);

            OnSaveAllDataToFiles();
        }

        public static void ApplyEntityDataToWorld()
        {
            List<Entity> entities = EntityCenter.all;
            foreach (Entity entity in entities)
            {
                entity.WriteDataToWorldSave();
            }
        }

        public static void LoadGame()
        {
            CheckPaths();

            settings = GameSettingsOnFile();

            SaveAllDataToFiles();
            CoroutineStarter.Do(IESetMixersVolume(1.5f));
        }

        static IEnumerator IESetMixersVolume(float waitTime)
        {
            yield return new WaitForSecondsRealtime(waitTime);

            ApplyVolumesToMixers();
        }

        public static void ApplyVolumesToMixers()
        {
            GAudio.globalAudioMixer.SetVolume(settings.volume.globalVolume);
            GAudio.musicAudioMixer.SetVolume(settings.volume.musicVolume);
            GAudio.defaultAudioMixer.SetVolume(settings.volume.defaultVolume);
            GAudio.uiAudioMixer.SetVolume(settings.volume.uiVolume);
            GAudio.ambientAudioMixer.SetVolume(settings.volume.ambientVolume);
        }
    }

    [Serializable]
    public sealed class World
    {
        public static string GetImagePath(string worldPath) => Path.Combine(GetDisplayCachePath(worldPath), "image.png");
        public static string GetBasicDataPath(string worldPath) => Path.Combine(worldPath, "basic_data.json");
        public static string GetRegionDataPath(string worldPath) => Path.Combine(worldPath, "region_data.json");
        public static string GetPlayerDataPath(string worldPath) => Path.Combine(worldPath, "player_data.json");
        public static string GetCachePath(string worldPath) => Path.Combine(worldPath, "caches");
        public static string GetDisplayCachePath(string worldPath) => Path.Combine(GetCachePath(worldPath), "display");



        public string worldPath => basicData.worldPath;
        public string worldImagePath => GetImagePath(worldPath);
        public string worldRegionDataPath => GetRegionDataPath(worldPath);



        public WorldBasicData basicData = new();
        public List<Region> regionData = new();
        public List<PlayerSave> playerSaves = new();



        public static World Load(string dirPath)
        {
            WorldBasicData basicData = JsonUtils.LoadTypeFromJsonPath<WorldBasicData>(GetBasicDataPath(dirPath));
            List<Region> regionData = JsonUtils.LoadTypeFromJsonPath<List<Region>>(GetRegionDataPath(dirPath));
            List<PlayerSave> playerData = JsonUtils.LoadTypeFromJsonPath<List<PlayerSave>>(GetPlayerDataPath(dirPath));

            return new(basicData, regionData, playerData);
        }



        public World(int seed, string worldName, bool isShortTerm, float requiredTotalTime = GTime.timeOneDay * 3)
        {
            basicData.seed = seed;
            basicData.gameVersion = GInit.gameVersion;
            basicData.worldName = worldName;
            basicData.isShortTerm = isShortTerm;
            basicData.requiredTotalTime = requiredTotalTime;
        }

        public World(WorldBasicData basicData, List<Region> regionData, List<PlayerSave> playerData)
        {
            this.basicData = basicData;
            this.regionData = regionData;
            this.playerSaves = playerData;
        }

        public void AddRegion(Region region)
        {
            //检查重复区域
            for (int i = 0; i < regionData.Count; i++)
            {
                var temp = regionData[i];

                //如果有就合并数据 (以 传入的区域sb 为主)
                if (temp.index == region.index)
                {
                    //遍历每一个方块并添加
                    foreach (var saveTemp in temp.blocks)
                    {
                        foreach (var locationTemp in saveTemp.locations)
                        {
                            region.AddPos(saveTemp.blockId, locationTemp.x, locationTemp.y, saveTemp.isBg);
                        }
                    }

                    //添加后把对应区域去掉
                    regionData[i] = null;
                    regionData.RemoveAt(i);
                    break;
                }
            }

            regionData.Add(region);
        }

        public Region GetOrAddRegion(Vector2Int index)
        {
            if (TryGetRegion(index, out Region region))
            {
                return region;
            }

            AddRegion(new() { index = index });
            return regionData[^1];
        }

        public bool TryGetRegion(Vector2Int index, out Region region)
        {
            foreach (var sb in regionData)
            {
                if (sb.index == index)
                {
                    region = sb;
                    return true;
                }
            }

            region = null;
            return false;
        }

        public Region GetRegion(Vector2Int index)
        {
            foreach (var region in regionData)
                if (region.index == index)
                    return region;

            return null;
        }
    }

    [Serializable]
    public class WorldBasicData
    {
        public int seed;
        public string worldName;
        public string gameVersion;
        public float time = 300;
        public float totalTime = 0;
        public float requiredTotalTime;
        public bool isAM = true;
        public bool isShortTerm;



        public string worldPath => Path.Combine(GInit.worldPath, worldName);
    }

    [Serializable]
    public sealed class GameSettings
    {
        [LabelText("音量")]
        public VolumeSettingsDatum volume = new()
        {
            globalVolume = -8,
            musicVolume = -12
        };
        [LabelText("语言ID")] public string langId = "ori:zh_cn";
        [LabelText("玩家名")] public string playerName = Tools.computerName;
        [LabelText("玩家光标速度")] public float playerCursorSpeed = 10f;
        [LabelText("屏幕光标速度")] public int screenCursorSpeed = 500;
        [LabelText("性能等级")] public byte performanceLevel = (byte)(Application.isMobilePlatform ? 65 : 130);   // 0 <-> 255
        [LabelText("自动隐藏区块")] public bool autoHideChunks = true;   // 0 <-> 255
        [LabelText("UI速度")] public float uiSpeed = 80;   // 0 <-> 255
        [LabelText("皮肤名")] public string playerSkinName = null;

        public GameSettings(bool init)
        {
            if (init)
            {
                langId = Application.systemLanguage switch
                {
                    SystemLanguage.ChineseSimplified => "ori:zh_cn",
                    SystemLanguage.ChineseTraditional => "ori:zh_tw",
                    _ => "ori:en_us"
                };
            }
        }
    }

    [Serializable]
    public struct VolumeSettingsDatum
    {
        //From -80 To +20
        [LabelText("总音量")] public float globalVolume;
        [LabelText("音乐音量")] public float musicVolume;
        [LabelText("默认音量")] public float defaultVolume;
        [LabelText("UI音量")] public float uiVolume;
        [LabelText("环境音量")] public float ambientVolume;
    }
}
