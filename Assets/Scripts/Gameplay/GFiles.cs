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
                //应用数据
                world.ApplyData();

                //将世界数据写入文件
                world.WriteDataToFiles();
            }

            //保存设置文件
            SaveFileJson(GInit.settingsPath, settings, false, true);

            World.OnSaveAllDataToFiles();
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
