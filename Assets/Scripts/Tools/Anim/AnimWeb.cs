using Cysharp.Threading.Tasks;
using DG.Tweening.Core;
using DG.Tweening;
using SP.Tools;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using UnityEngine;
using SP.Tools.Unity;
using System.Collections;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace GameCore
{
    /* -------------------------------------------------------------------------- */
    /*                                     公共类                                    */
    /* -------------------------------------------------------------------------- */
    public class AnimWeb   //* 模仿的是 UnityEngine.Animator
    {
        public class AnimConnection
        {
            public short layer;
            public string from;
            public string to;
            public Func<bool> switchCondition;
            public float transitionTime;

            public AnimConnection(short layer, string from, string to, Func<bool> switchCondition, float transitionTime)
            {
                this.from = from;
                this.to = to;
                this.switchCondition = switchCondition;
                this.transitionTime = transitionTime;
            }
        }

        public class AnimGroup
        {
            public short layer;
            public string id;
            public Anim[] anims;

            public void Play()
            {
                foreach (var anim in anims)
                {
                    anim.Play();
                }
            }

            public void Stop()
            {
                foreach (var anim in anims)
                {
                    anim.Stop();
                }
            }

            public AnimGroup(short layer, string id, Anim[] anims)
            {
                this.layer = layer;
                this.id = id;
                this.anims = anims;
            }
        }

        public class WebbedAnim : Anim
        {
            public AnimWeb web;
            public short layer;

            public WebbedAnim(AnimWeb web, short layer, string id, int loops, AnimFragment[] fragments, TweenCallback totalCompleteCallback = null, TweenCallback perLoopCompleteCallback = null, Dictionary<float, Action> frameCalls = null)
                : base(id, loops, fragments, totalCompleteCallback, perLoopCompleteCallback, frameCalls)
            {
                this.web = web;
                this.layer = layer;
            }
        }

        public readonly List<WebbedAnim> animations = new();
        public readonly List<AnimGroup> groups = new();
        public readonly List<AnimConnection> connections = new();
        public readonly List<AnimConnection> detectingConnections = new();
        public Dictionary<short, string> playingAnimOrGroup = new();

        public void AddAnim(string animId, int loops, AnimFragment[] fragments, short layer = 0, TweenCallback totalCompleteCallback = null, TweenCallback perLoopCompleteCallback = null, Dictionary<float, Action> frameCalls = null)
        {
            if (animations.Any(p => p.layer == layer && p.id == animId))
            {
                Debug.LogWarning($"{layer} 层 已存在一个 {animId} 动画, 操作返回");
                return;
            }

            if (groups.Any(p => p.layer == layer && p.id == animId))
            {
                Debug.LogWarning($"{layer} 层 已存在一个 {animId} 动画*组*, 操作返回");
                return;
            }

            animations.Add(new(this, layer, animId, loops, fragments, () =>
            {
                if (playingAnimOrGroup.ContainsKey(layer))
                {
                    StopAnim(animId, layer);
                }

                totalCompleteCallback?.Invoke();
            }, perLoopCompleteCallback, frameCalls));
        }

        public WebbedAnim GetAnim(string animId, short layer = 0)
        {
            foreach (var anim in animations)
            {
                if (anim.layer == layer && anim.id == animId)
                {
                    return anim;
                }
            }

            return null;
        }

        public void Stop()
        {
            foreach (var anim in animations)
            {
                anim.Stop();
            }

            playingAnimOrGroup.Clear();
        }

        public void StopAnim(string id, short layer = 0)
        {
            Debug.Log($"Stop {id}");
            foreach (var anim in animations)
            {
                if (anim.layer == layer && anim.id == id)
                {
                    playingAnimOrGroup.Remove(layer);
                    anim.Stop();
                    return;
                }
            }

            foreach (var group in groups)
            {
                if (group.layer == layer && group.id == id)
                {
                    playingAnimOrGroup.Remove(layer);
                    group.Stop();
                    return;
                }
            }
        }

        public void SwitchPlayingTo(string id, short layer = 0)
        {
            Debug.Log($"play {id}");
            //获取层数与 id 匹配的 anim
            foreach (var anim in animations)
            {
                if (anim.layer == layer && anim.id == id)
                {
                    //播放动画
                    if (playingAnimOrGroup.ContainsKey(layer))
                        StopAnim(playingAnimOrGroup[layer], layer);

                    playingAnimOrGroup.Add(layer, id);
                    anim.Play();

                    return;
                }
            }

            foreach (var group in groups)
            {
                if (group.id == id)
                {
                    //播放动画组
                    if (playingAnimOrGroup.ContainsKey(layer))
                        StopAnim(playingAnimOrGroup[layer], layer);

                    playingAnimOrGroup.Add(layer, id);
                    group.Play();

                    return;
                }
            }

            Debug.LogError($"不存在 {id} 动画或动画组, 无法切换, 操作返回");
        }

        public void UpdateWeb()
        {
            // foreach (var playing in playingAnimOrGroup)
            // {
            //     Debug.Log($"{playing.Key} : {playing.Value}");
            // }
            foreach (var connection in connections)
            {
                if (playingAnimOrGroup.TryGetValue(connection.layer, out var playing) && playing == connection.from && connection.switchCondition())
                {
                    //没有过渡时间就直接换
                    if (connection.transitionTime == 0)
                    {
                        SwitchPlayingTo(connection.to, connection.layer);
                    }
                    //有过渡时间且没有携程这在检测, 就开始反复检测
                    else if (!detectingConnections.Contains(connection))
                    {
                        //检测在时间 connection.Value.transitionTime 内, condition() 是否都返回true
                        detectingConnections.Add(connection);
                        CoroutineStarter.Do(DetectConnectionChange(connection));
                    }
                }
            }
        }

        private IEnumerator DetectConnectionChange(AnimConnection connection)
        {
            var endTime = Tools.time + connection.transitionTime;

            while (Tools.time < endTime)
            {
                if (!(playingAnimOrGroup.TryGetValue(connection.layer, out var playing) && playing == connection.from && connection.switchCondition()))
                    yield break;
                else
                    yield return null;
            }

            SwitchPlayingTo(connection.to, connection.layer);
            detectingConnections.Remove(connection);
        }

        public bool TryGetAnim(string animId, out WebbedAnim result, short layer = 0)
        {
            foreach (var anim in animations)
            {
                if (anim.layer == layer && anim.id == animId)
                {
                    result = anim;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public bool ContainsAnimOrGroup(string id, short layer = 0)
        {
            foreach (var anim in animations)
            {
                if (anim.layer == layer && anim.id == id)
                {
                    return true;
                }
            }

            foreach (var group in groups)
            {
                if (group.layer == layer && group.id == id)
                {
                    return true;
                }
            }

            return false;
        }

        public void CreateConnectionFromTo(string from, string to, Func<bool> switchCondition, float transitionTime = 0, short layer = 0)
        {
            if (!ContainsAnimOrGroup(from, layer))
            {
                Debug.LogError($"不存在 'to' 动画(或组) {from}, 无法创建连接, 操作返回");
                return;
            }

            connections.Add(new(layer, from, to, switchCondition, transitionTime));
        }

        //TODO: 也许 group 可以不分 layer?
        public void GroupAnim(short layer, string groupId, params string[] ids)
        {
            var anims = new List<WebbedAnim>();

            if (groups.Any(p => p.layer == layer && p.id == groupId))
            {
                Debug.LogWarning($"{layer} 层 已存在一个 {groupId} 动画*组*, 操作返回");
                return;
            }
            if (animations.Any(p => p.layer == layer && p.id == groupId))
            {
                Debug.LogWarning($"{layer} 层 已存在一个 {groupId} 动画, 操作返回");
                return;
            }

            foreach (var id in ids)
            {
                foreach (var anim in animations)
                {
                    if (anim.layer == layer && anim.id == id)
                    {
                        anims.Add(anim);
                        break;
                    }
                }
            }

            groups.Add(new(layer, groupId, anims.ToArray()));
        }

        //TODO: 也许 group 可以不分 layer?
        public void UngroupAnim(short layer, string groupId)
        {
            foreach (var group in groups)
            {
                if (group.layer == layer && group.id == groupId)
                {
                    groups.Remove(group);
                    return;
                }
            }

            Debug.LogError($"不存在 {groupId} 动画*组, 无法解组, 操作返回");
        }

        //TODO: 也许 group 可以不分 layer?
        public void RegroupAnim(short layer, string groupId, params string[] ids)
        {
            foreach (var group in groups)
            {
                if (group.layer == layer && group.id == groupId)
                {
                    groups.Remove(group);
                    break;
                }
            }

            GroupAnim(layer, groupId, ids);
        }
    }
}
