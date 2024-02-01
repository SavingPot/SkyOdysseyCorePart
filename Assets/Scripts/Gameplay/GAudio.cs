using GameCore.High;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using SP.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GameCore
{
    public static class GAudio
    {
        public static List<AudioData> audios = new();

        public static AudioMixerGroup globalAudioMixer => GInit.instance.globalAudioMixer;
        public static AudioMixerGroup musicAudioMixer => GInit.instance.musicAudioMixer;
        public static AudioMixerGroup defaultAudioMixer => GInit.instance.defaultAudioMixer;
        public static AudioMixerGroup uiAudioMixer => GInit.instance.uiAudioMixer;
        public static AudioMixerGroup ambientAudioMixer => GInit.instance.ambientAudioMixer;

        public static AudioSourcePool sourcePool = new(ManagerAudio.instance.transform);


        public static AudioData GetAudio(string id)
        {
            foreach (var audio in audios)
            {
                if (audio.id == id)
                {
                    return audio;
                }
            }

            return null;
        }



        [ChineseName("设置混音器音量")] public static void SetMixerVolume(AudioMixerGroup audioMixerGroup, float value) => audioMixerGroup.audioMixer.SetFloat("MixerVolume", value);

        public static void Play(string audioId, bool dontPlayWhenPlaying = false)
        {
            //如果audioId为空或者为空字符串，则报错
            if (audioId.IsNullOrWhiteSpace())
            {
                Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: {nameof(audioId)} 不能为空");
                return;
            }

            //遍历所有音频
            foreach (AudioData audio in audios)
            {
                if (audio.id == audioId)
                {
                    //如果正在播放而且要求播放时不能打断, 就不播放
                    if (dontPlayWhenPlaying && audio.sources.Count != 0)
                        return;

                    //读取对象池池
                    var source = sourcePool.Get(audio);
                    audio.sources.Add(source);

                    //播放新音频
                    source.Play();

                    //如果不是循环音频, 就在播放完成后回收
                    if (!source.loop) ManagerAudio.instance.StartCoroutine(IERecoverSource(audio, source));

                    return;
                }
            }

            //如果没有匹配到音频，则报错
            Debug.LogWarning($"播放音频 {audioId} 失败, 未匹配到音频");
        }

        static IEnumerator IERecoverSource(AudioData data, AudioSource source)
        {
            //等待完全播放
            yield return new WaitForSeconds(source.clip.length);

            data.sources.Remove(source);
            sourcePool.Recover(source);
        }

        public static void Stop(string audioId)
        {
            if (audioId.IsNullOrWhiteSpace())
            {
                Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: {nameof(audioId)} 不能为空");
                return;
            }

            foreach (AudioData audio in audios)
            {
                if (audio.id == audioId)
                {
                    if (audio.sources.Count == 0)
                        return;

                    foreach (var source in audio.sources)
                    {
                        source.Stop();
                        sourcePool.Recover(source);
                    }

                    audio.sources.Clear();

                    return;
                }
            }

            Debug.LogWarning($"停止音频 {audioId} 失败, 未匹配到音频");
        }
    }

    [Serializable]
    public class AudioData
    {
        [LabelText("音频ID")] public string id;
        [HideInInspector, JsonIgnore, LabelText("音频 Clip")] public AudioClip clip;
        [NonSerialized] public List<AudioSource> sources = new();
        [LabelText("音量")] public float volume = 1;
        [LabelText("循环")] public bool loop;
        [LabelText("混音器种类")] public AudioMixerType audioMixerType;
        [JsonIgnore, LabelText("路径")] public string path;
        [LabelText("最早时间")] public float earliestTime;
        [LabelText("最晚时间")] public float latestTime;
        [LabelText("几率")] public float probability;
    }

    public class AudioSourcePool : ObjectPool<AudioSource>
    {
        public Transform container;

        public override AudioSource Generation()
        {
            var source = container.gameObject.AddComponent<AudioSource>();

            source.playOnAwake = false;

            return source;
        }

        public AudioSource Get(AudioData data)
        {
            var source = Get();

            source.clip = data.clip;

            //设置混音器
            source.outputAudioMixerGroup = data.audioMixerType switch
            {
                AudioMixerType.Default => GAudio.defaultAudioMixer,
                AudioMixerType.Music => GAudio.musicAudioMixer,
                AudioMixerType.UI => GAudio.uiAudioMixer,
                _ => GAudio.defaultAudioMixer
            };

            //设置基本菜蔬
            source.volume = data.volume;
            source.loop = data.loop;

            return source;
        }

        public AudioSourcePool(Transform container)
        {
            this.container = container;
        }
    }

    public enum AudioMixerType : byte
    {
        Default,
        Music,
        UI,
        Ambient
    }
}
