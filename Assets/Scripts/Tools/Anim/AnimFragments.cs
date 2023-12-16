using System;
using DG.Tweening;
using UnityEngine;

namespace GameCore
{
    public abstract class AnimFragment
    {
        public readonly float duration;
        public readonly Ease ease;
        public abstract bool SetValue(float duration);
        public abstract Tween GenerateTween();

        public AnimFragment(float duration, Ease ease)
        {
            this.duration = duration;
            this.ease = ease;
        }
    }

    public class SpriteAnimFragment : AnimFragment
    {
        public readonly Func<Sprite, bool> setSprite;
        public readonly Sprite[] sprites;

        public override bool SetValue(float progress)
        {
            Sprite current = sprites[Mathf.FloorToInt(progress * sprites.Length)];

            return setSprite(current);
        }

        public override Tween GenerateTween()
        {
            float currentTime = 0;
            var anim = DOTween.To(() => currentTime, null, 1f, duration);

            anim.setter = value =>
            {
                currentTime = value;

                //* Mathf.Min 的意义是把进度限制在 [0,1] 之间, 因为 currentTime 有可能略微大于 0
                SetValue(Mathf.Min(currentTime, 1f));
            };

            return anim;
        }

        public SpriteAnimFragment(Func<Sprite, bool> setSprite, Sprite[] sprites, float duration, Ease ease) : base(duration, ease)
        {
            this.setSprite = setSprite;
            this.sprites = sprites;
        }
    }

    public class RotationAnimFragment : AnimFragment
    {
        public readonly Transform transform;
        public readonly Vector2 endValue;

        public override bool SetValue(float progress)
        {
            if (progress == 1)
                transform.rotation = Quaternion.Euler(endValue);
            else
                transform.rotation = Quaternion.Euler(endValue * progress);

            return true;
        }

        public override Tween GenerateTween()
        {
            return transform.DORotate(endValue, duration);
        }

        public RotationAnimFragment(Transform transform, Vector2 endValue, float duration, Ease ease) : base(duration, ease)
        {
            this.transform = transform;
            this.endValue = endValue;
        }
    }

    public class LocalRotationAnimFragment : AnimFragment
    {
        public readonly Transform transform;
        public readonly Vector2 endValue;

        public override bool SetValue(float progress)
        {
            if (progress == 1)
                transform.localRotation = Quaternion.Euler(endValue);
            else
                transform.localRotation = Quaternion.Euler(endValue * progress);

            return true;
        }

        public override Tween GenerateTween()
        {
            return transform.DOLocalRotate(endValue, duration);
        }

        public LocalRotationAnimFragment(Transform transform, Vector2 endValue, float duration, Ease ease) : base(duration, ease)
        {
            this.transform = transform;
            this.endValue = endValue;
        }
    }

    public class LocalRotationZAnimFragment : AnimFragment
    {
        public readonly Transform transform;
        public readonly float endValue;

        public override bool SetValue(float progress)
        {
            if (progress == 1)
                transform.localRotation = Quaternion.Euler(new(transform.localRotation.x, transform.localRotation.y, endValue));
            else
                transform.localRotation = Quaternion.Euler(new(transform.localRotation.x, transform.localRotation.y, endValue * progress));

            return true;
        }

        public override Tween GenerateTween()
        {
            return transform.DOLocalRotateZ(endValue, duration);
        }

        public LocalRotationZAnimFragment(Transform transform, float endValue, float duration, Ease ease) : base(duration, ease)
        {
            this.transform = transform;
            this.endValue = endValue;
        }
    }
}