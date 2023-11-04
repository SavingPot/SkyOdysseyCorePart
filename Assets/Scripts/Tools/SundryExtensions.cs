using DG.Tweening;
using SP.Tools;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using TMPro;
using static UnityEngine.ParticleSystem;

namespace GameCore
{
    public static class SundryExtensions
    {
        public static void Clear(this TextureSheetAnimationModule module)
        {
            for (int i = 0; i < module.spriteCount; i++)
            {
                module.RemoveSprite(i);
            }
        }

        public static void HideClickAction(this Button button)
        {
            button.colors = new()
            {
                normalColor = button.colors.normalColor,
                highlightedColor = button.colors.normalColor,
                pressedColor = button.colors.normalColor,
                selectedColor = button.colors.normalColor,
                fadeDuration = button.colors.fadeDuration,
                disabledColor = button.colors.disabledColor,
                colorMultiplier = button.colors.colorMultiplier
            };
        }

        public static Tween DOLocalRotateZ(this Transform trans, float z, float duration) => trans.DOLocalRotate(new(0, 0, z), duration);

        //public static Tween DORotateAroundZ(this Transform trans, Vector2 point, float angle, float duration)
        //{
        //	float z = trans.rotation.z;
        //	return DOTween.To(() => z, f => { z = f; trans.rotation = Quaternion.identity; trans.RotateAround((Vector2)trans.position + point, Axis.z, f); }, angle, duration);
        //}

        /// <summary>
        /// Func = Func.Add(8) <=> 常量 (错误示范 Func = Func.Add(transform.localScale.x.Sign() != rb2dSelf.velocity.x.Sign() ? 0.75f : 1))
        /// </summary>
        /// <param name="func"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public static Func<float> Add(this Func<float> func, float num)
        {
            float oldValue = func();
            func = () => oldValue + num;
            return func;
        }

        /// <summary>
        /// Func = Func.Multiply(8) <=> 常量 (错误示范 Func = Func.Multiply(transform.localScale.x.Sign() != rb2dSelf.velocity.x.Sign() ? 0.75f : 1))
        /// </summary>
        /// <param name="func"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public static Func<float> Multiply(this Func<float> func, float num)
        {
            float oldValue = func();
            func = () => oldValue * num;
            return func;
        }

        [ChineseName("输出值")]
        public static void LogValue(this object obj, string prefix = "")
        {
            StringBuilder msg = new();

            if (!prefix.IsNullOrWhiteSpace())
                msg.Append(prefix).Append(" : ");

            msg.Append(obj);
            Debug.Log(msg);
        }

        [ChineseName("详细输出值")]
        public static void LogValueDetailedly(this object obj, string prefix = "")
        {
            StringBuilder msg = new(MethodGetter.GetLastAndCurrentMethodPath());
            msg.Append(" / ");

            if (!prefix.IsNullOrEmpty())
                msg.Append(prefix).Append(" : ");

            msg.Append(obj);
            Debug.Log(msg);
        }

        [ChineseName("输出NULL值")]
        public static void LogNull(this object obj, string prefix = "")
        {
            StringBuilder msg = new();

            if (!prefix.IsNullOrEmpty())
                msg.Append(prefix).Append(" : ");

            msg.Append(obj == null);
            Debug.Log(msg);
        }

        [ChineseName("详细输出NULL值")]
        public static void LogNullDetailedly(this object obj, string prefix = "")
        {
            StringBuilder msg = new(MethodGetter.GetLastAndCurrentMethodPath());
            msg.Append(" / ");

            if (!prefix.IsNullOrEmpty())
                msg.Append(prefix).Append(" : ");

            msg.Append(obj == null);
            Debug.Log(msg);
        }

        [ChineseName("按着")] public static bool Holding(this KeyControl key) => key.isPressed;

        public static void SetColorBrightness(this SpriteRenderer r, float num) => r.color = new(num, num, num, r.color.a);

        public static void SetColorBrightness(this Graphic graphic, float num, float a = 1) => graphic.color = new(num, num, num, a);

        public static void SetColor(this Graphic graphic, float r, float g, float b, float a = 1) => graphic.color = new(r, g, b, a);

        public static void SetAlpha(this Graphic graphic, float alpha) => graphic.color = new(graphic.color.r, graphic.color.g, graphic.color.b, alpha);

        public static void ReduceColorBrightness(this Graphic graphic, float num) => graphic.color = new(graphic.color.r - num, graphic.color.g - num, graphic.color.b - num);

        public static void IncreaseColorBrightness(this Graphic graphic, float num) => graphic.color = new(graphic.color.r + num, graphic.color.g + num, graphic.color.b + num);

        public static void SetFontSize(this TMP_Text text, int size)
        {
            text.enableAutoSizing = false;
            text.fontSize = size;
        }
    }
}
