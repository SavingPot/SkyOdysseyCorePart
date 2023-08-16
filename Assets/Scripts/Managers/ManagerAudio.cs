using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using Debug = UnityEngine.Debug;
using System;
using System.IO;
using SP.Tools;
using System.Linq;

namespace GameCore.High
{
    [ChineseName("音频管理器")]
    public class ManagerAudio : SingletonToolsClass<ManagerAudio>
    {
#if UNITY_EDITOR
        [Tooltip("游戏需要用到的音频, 模组管理器也会把音效存在这里"), LabelText("音频"), SerializeField] private List<AudioData> audios;

        private void Update()
        {
            audios = GAudio.audios;
        }
#endif



        protected override void DestroyOrSave() => DontDestroyOnLoadSingleton();



        [ChineseName("从文件添加音频")] public void AddClipFromFile(AudioData data) => StartCoroutine(IEAddClipFromFile(data));

        [ChineseName("IE从文件添加音频")]
        public IEnumerator IEAddClipFromFile(AudioData data)
        {
            #region 参数检查
            if (data == null)
            {
                Debug.LogError($"{nameof(ManagerAudio)}.{nameof(IEAddClipFromFile)}: {nameof(data)} 值为空");
                yield break;
            }
            if (data.path.IsNullOrWhiteSpace())
            {
                Debug.LogError($"{nameof(ManagerAudio)}.{nameof(IEAddClipFromFile)}: {nameof(data)}.{nameof(AudioData.path)} 值为空");
                yield break;
            }
            if (!File.Exists(data.path))
            {
                Debug.LogError($"{nameof(ManagerAudio)}.{nameof(IEAddClipFromFile)}: 未找到文件 {data.path}");
                yield break;
            }
            #endregion

            /* ---------------------------------- 处理目录 ---------------------------------- */
            string path = data.path;

            if (GInit.platform == RuntimePlatform.Android)
            {
                //为安卓平台添加前缀否则会抛出异常
                path = $"file:///{path}";
            }

            /* ---------------------------------- 加载 Clip ---------------------------------- */
            using UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.UNKNOWN);
            yield return uwr.SendWebRequest();
            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(uwr);
            yield return audioClip;

            /* ---------------------------------- 设置音频 ---------------------------------- */
            data.clip = audioClip;
            GAudio.audios.Add(data);
        }
    }
}
