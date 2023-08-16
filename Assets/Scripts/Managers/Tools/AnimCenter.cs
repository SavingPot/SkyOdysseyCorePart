using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;

namespace GameCore
{
    public static class AnimCenter
    {
        public static TweenerCore<float, float, DG.Tweening.Plugins.Options.FloatOptions> PlaySprites(float time, Sprite[] sprites, Func<Sprite, bool> SetSprite, int loops = -1, Action OnComplete = null)
        {
            float timed = 0;
            var anim = DOTween.To(() => timed, null, 0.999f, time).OnStepComplete(() => OnComplete?.Invoke()).SetLoops(loops).SetEase(Ease.Linear);

            anim.setter = value =>
            {
                timed = value;

                Sprite current = sprites[Mathf.FloorToInt(timed * sprites.Length)];

                if (!SetSprite(current))
                    anim.Kill();
            };

            return anim;
        }

    }





    /* -------------------------------------------------------------------------- */
    /*                                     公共类                                    */
    /* -------------------------------------------------------------------------- */
    public class AnimObject
    {
        public readonly List<AnimData> animations = new();
        public List<IAnimSequence> sequences = new();

        public void AddAnim(string id, Action action, Func<Tween[]> tweens, Func<Sequence> sequence)
        {
            if (animations.Any(p => p.id == id))
            {
                Debug.LogWarning($"动画 {id} 已存在, 操作返回");
                return;
            }

            animations.Add(new(id, () =>
            {
                action();

                //遍历每个 Tween
                var t = tweens?.Invoke();
                for (int i = 0; i < (t?.Length ?? 0); i++)
                {
                    //把 Tween 拼接到 Sequence 上
                    sequence().Append(t[i]);
                }
            }));
        }

        public void ResetAnimations(string exceptAnimation = null)
        {
            foreach (var anim in animations)
                if (anim.id != exceptAnimation)
                    anim.isPlaying = false;

            ResetSequences();
        }

        public void KillSequences()
        {
            for (int i = 0; i < sequences.Count; i++)
            {
                sequences[i]?.sequence?.Kill();
            }
        }

        public void SetAnim(string animId, bool active = true)
        {
            foreach (var anim in animations)
            {
                if (anim.id == animId)
                {
                    if (active)
                    {
                        anim.isPlaying = true;
                        anim.play?.Invoke();
                    }
                    else
                    {
                        anim.isPlaying = false;
                    }
                    break;
                }
            }
        }

        public virtual void ResetSequences()
        {
            for (int i = 0; i < sequences.Count; i++)
            {
                sequences[i].ResetSequence();
            }
        }
    }

    [Serializable]
    public class AnimData
    {
        public AnimData(string id, Action play)
        {
            this.id = id;
            this.play = play;
        }

        public string id;
        public Action play;
        public bool isPlaying;
    }

    public class DefaultSpriteAnimSequence : IAnimSequence
    {
        public Sequence sequence { get; set; } = DOTween.Sequence().SetLoops(-1);

        public void ResetSequence(int loops = -1, bool resetPos = true, bool resetRot = true)
        {
            //重初始化动画队列
            sequence.Kill();
            sequence = DOTween.Sequence().SetLoops(loops);
        }
    }

    public class DefaultRotationAnimSequence : IAnimSequence
    {
        public Sequence sequence { get; set; } = DOTween.Sequence().SetLoops(-1);
        public Transform owner;
        public Vector2 defaultPos;

        public void ResetSequence(int loops = -1, bool resetPos = true, bool resetRot = true)
        {
            //重初始化动画队列
            sequence.Kill();
            sequence = DOTween.Sequence().SetLoops(loops);

            if (resetPos) ResetPos();
            if (resetRot) ResetRot();
        }

        public void ResetPos() => owner.localPosition = defaultPos;
        public void ResetRot() => owner.rotation = Quaternion.identity;



        public DefaultRotationAnimSequence(Transform owner)
        {
            this.owner = owner;
            defaultPos = this.owner.localPosition;
        }
    }

    public interface IAnimSequence
    {
        Sequence sequence { get; }
        void ResetSequence(int loops = -1, bool resetPos = true, bool resetRot = true);
    }
}