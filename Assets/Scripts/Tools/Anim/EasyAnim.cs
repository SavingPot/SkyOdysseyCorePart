using System;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;

namespace GameCore
{
    public static class EasyAnim
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
}