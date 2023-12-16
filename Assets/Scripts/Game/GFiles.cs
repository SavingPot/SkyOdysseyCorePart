using GameCore.High;
using SP.Tools;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SP.Tools.Unity;

namespace GameCore
{
    public static class GFiles
    {
        public static GameSettings settings;
        public static World world;

        public static void CreateWorld(int seed, string name)
        {
            SaveAllDataToFiles();
            world = new(seed, name);
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
            SaveFileContext(savePath, JsonTools.ToJson(obj, compress, prettyJson));
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
                var datum = JsonTools.LoadJson<GameSettings>(GInit.settingsPath);

                return datum ?? new(true);
            }
            else
            {
                return new(true);
            }
        }

        public static Action SaveAllDataToFiles = () =>
        {
            CheckPaths();

            //保存世界, 仅有服务器保存
            if (world != null)
            {
                //将时间写入
                if (Server.isServer)
                {
                    world.basicData.time = GTime.time;
                    world.basicData.isAM = GTime.isMorning;
                }

                //将实体数据写入
                List<Entity> entities = EntityCenter.all;
                foreach (Entity entity in entities)
                {
                    entity.WriteDataToSave();
                }

                //将世界数据写入文件
                IOTools.CreateDirsIfNone(World.GetCachePath(world.worldPath), World.GetDisplayCachePath(world.worldPath));
                SaveFileJson(World.GetBasicDataPath(world.worldPath), world.basicData, false, true);
                SaveFileJson(World.GetSandboxDataPath(world.worldPath), world.sandboxData, false, false);
                SaveFileJson(World.GetPlayerDataPath(world.worldPath), world.playerData, false, true);
            }

            //保存设置文件
            SaveFileJson(GInit.settingsPath, settings, false, true);

            GameCallbacks.CallOnSaveAllDataToFiles();
        };

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
    public class World
    {
        public static string GetImagePath(string worldPath) => Path.Combine(GetDisplayCachePath(worldPath), "image.png");
        public static string GetBasicDataPath(string worldPath) => Path.Combine(worldPath, "basic_data.json");
        public static string GetSandboxDataPath(string worldPath) => Path.Combine(worldPath, "sandbox_data.json");
        public static string GetPlayerDataPath(string worldPath) => Path.Combine(worldPath, "player_data.json");
        public static string GetCachePath(string worldPath) => Path.Combine(worldPath, "caches");
        public static string GetDisplayCachePath(string worldPath) => Path.Combine(GetCachePath(worldPath), "display");



        public string worldPath => basicData.worldPath;
        public string worldImagePath => GetImagePath(worldPath);
        public string worldSandboxDataPath => GetSandboxDataPath(worldPath);



        public WorldBasicData basicData = new();
        public List<Sandbox> sandboxData = new();
        public List<PlayerData> playerData = new();



        public static World Load(string dirPath)
        {
            WorldBasicData basicData = JsonTools.LoadJson<WorldBasicData>(GetBasicDataPath(dirPath));
            List<Sandbox> sandboxData = JsonTools.LoadJson<List<Sandbox>>(GetSandboxDataPath(dirPath));
            List<PlayerData> playerData = JsonTools.LoadJson<List<PlayerData>>(GetPlayerDataPath(dirPath));

            return new(basicData, sandboxData, playerData);
        }



        public World(int seed, string worldName)
        {
            this.basicData.seed = seed;
            this.basicData.gameVersion = GInit.gameVersion;
            this.basicData.worldName = worldName;
        }

        public World(WorldBasicData basicData, List<Sandbox> sandboxData, List<PlayerData> playerData)
        {
            this.basicData = basicData;
            this.sandboxData = sandboxData;
            this.playerData = playerData;
        }

        public void AddSandbox(Sandbox sb)
        {
            //检查重复沙盒
            for (int i = 0; i < sandboxData.Count; i++)
            {
                var temp = sandboxData[i];

                //如果有就合并数据 (以 传入的沙盒sb 为主)
                if (temp.index == sb.index)
                {
                    //遍历每一个方块并添加
                    foreach (var saveTemp in temp.saves)
                    {
                        foreach (var locationTemp in saveTemp.locations)
                        {
                            sb.AddPos(saveTemp.blockId, locationTemp.pos, locationTemp.isBackground);
                        }
                    }

                    //添加后把对应沙盒去掉
                    sandboxData[i] = null;
                    sandboxData.RemoveAt(i);
                    break;
                }
            }

            sandboxData.Add(sb);
        }

        public Sandbox GetOrAddSandbox(Vector2Int index)
        {
            if (TryGetSandbox(index, out Sandbox sandbox))
            {
                return sandbox;
            }

            AddSandbox(new() { index = index });
            return sandboxData[^1];
        }

        public bool TryGetSandbox(Vector2Int index, out Sandbox sandbox)
        {
            foreach (var sb in sandboxData)
            {
                if (sb.index == index)
                {
                    sandbox = sb;
                    return true;
                }
            }

            sandbox = null;
            return false;
        }

        public Sandbox GetSandbox(Vector2Int index)
        {
            foreach (var sb in sandboxData)
                if (sb.index == index)
                    return sb;

            return null;
        }
    }

    [Serializable]
    public class WorldBasicData
    {
        public int seed;
        public string worldName;
        public string gameVersion;
        public float time = 420;
        public bool isAM = true;



        public string worldPath => Path.Combine(GInit.worldPath, worldName);
    }

    [Serializable]
    public class EntitySave
    {
        public string id;
        public string customData;
        public Vector2 pos;
        public float? health;
        public string saveId;
    }

    [Serializable]
    public class GameSettings
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
