using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using SP.Tools.Unity;
using UnityEngine;

namespace GameCore
{
    [Serializable]
    public class Anim
    {
        public string id;
        public int loops;
        public bool isPlaying;
        public float totalDuration;
        public AnimFragment[] fragments;
        public Dictionary<float, Action> frameCalls;
        public TweenCallback totalCompleteCallback = () => { };
        public TweenCallback perLoopCompleteCallback = () => { };
        protected IEnumerator playingCoroutine;
        protected Tween playingTween;

        public void Stop()
        {
            /* ---------------------------------- 停止播放 ---------------------------------- */
            if (playingCoroutine != null)
                CoroutineStarter.instance.StopCoroutine(playingCoroutine);

            playingTween?.Kill();
            /* ----------------------------------------------------------------------------- */


            isPlaying = false;
        }

        public async void Play()
        {
            Stop();

            var fragmentTweensGeneration = new Func<Tween>[fragments.Length];

            for (int i = 0; i < fragments.Length; i++)
            {
                var fragment = fragments[i];

                if (fragment.duration == 0)
                {
                    fragmentTweensGeneration[i] = () =>
                    {
                        //* 意思是直接设置数值, 然后返回一个无意义的Tween
                        if (!fragment.SetValue(1f))
                            Stop();

                        return DOTween.To(() => 0f, _ => { }, 0f, 0f);
                    };
                }
                else
                {
                    fragmentTweensGeneration[i] = () => fragment.GenerateTween().SetEase(fragment.ease);
                }
            }

            float playTime = Tools.time;
            isPlaying = true;

            var coroutine = BeginPlayingTweens(fragmentTweensGeneration);
            playingCoroutine = coroutine;
            CoroutineStarter.Do(coroutine);

            //TODO: 可能要检测 Tween 是否被杀死
            if (frameCalls != null && frameCalls.Count != 0)
            {
                foreach (var call in frameCalls)
                {
                    await UniTask.WaitUntil(() => Tools.time >= playTime + call.Key);
                    call.Value();
                }
            }
        }

        public IEnumerator BeginPlayingTweens(Func<Tween>[] fragmentTweensGeneration)
        {
            //无限循环
            if (loops < 0)
            {
                while (true)
                {
                    //依次播放每一个片段
                    foreach (var item in fragmentTweensGeneration)
                    {
                        //获取片段的 Tween
                        var tween = item();

                        playingTween = tween;

                        yield return tween.WaitForCompletion();

                        //杀死片段的 Tween
                        tween.Kill();
                    }

                    perLoopCompleteCallback?.Invoke();
                }
            }
            //循环指定次数
            else
            {
                for (int i = 0; i < loops; i++)
                {
                    //依次播放每一个片段
                    foreach (var item in fragmentTweensGeneration)
                    {
                        //获取片段的 Tween
                        var tween = item();

                        playingTween = tween;

                        yield return tween.WaitForCompletion();

                        //杀死片段的 Tween
                        tween.Kill();
                    }

                    perLoopCompleteCallback?.Invoke();
                }
            }

            //* 即使 tween 已经播放完了, 这个动画也仍然是播放状态, 只有调用 Stop() 方法才会设置 isPlaying 为 false
            totalCompleteCallback?.Invoke();
        }

        public Anim(string id, int loops, AnimFragment[] fragments, TweenCallback totalCompleteCallback = null, TweenCallback perLoopCompleteCallback = null, Dictionary<float, Action> frameCalls = null)
        {
            this.id = id;
            this.loops = loops;
            this.totalCompleteCallback = totalCompleteCallback;
            this.perLoopCompleteCallback = perLoopCompleteCallback;

            //检查动画片段
            if (fragments == null || fragments.Length == 0)
            {
                Debug.LogError($"动画 {id} 中没有动画片段, 请检查!");
                return;
            }
            this.fragments = fragments;

            //计算动画片段总时长
            this.totalDuration = 0;
            foreach (var fragment in fragments)
            {
                this.totalDuration += fragment.duration;
            }

            //检测帧调用是否符合标准
            if (frameCalls != null && frameCalls.Count != 0)
            {
                foreach (var call in frameCalls)
                {
                    if (call.Key > this.totalDuration)
                    {
                        Debug.LogError($"动画 {id} 的其中一个帧调用被调用的时刻 {call.Key} 大于动画总时间 {totalDuration}, 该帧调用不会被调用, 请检查!");
                        return;
                    }
                }
                //检查帧调用的调用时刻是否从小到大
                if (frameCalls.Count > 1)
                {
                    //* 注意: 开始值是 1， 不是 0
                    for (int i = 1; i < frameCalls.Count; i++)
                    {
                        float currentFrameCallTime = frameCalls.ElementAt(i).Key;
                        float lastFrameCallTime = frameCalls.ElementAt(i - 1).Key;

                        if (currentFrameCallTime < lastFrameCallTime)
                        {
                            Debug.LogError($"动画 {id} 的其中一个帧调用被调用的时刻 {currentFrameCallTime} 比其上一个帧调用时刻 {lastFrameCallTime} 小, 该帧调用的调用时间会出异常, 请检查!");
                            return;
                        }
                    }
                }
            }
            this.frameCalls = frameCalls;
        }
    }
}