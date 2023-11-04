using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public static class TextureHandler
    {
        public static Texture2D CombineTexture(Texture2D background, Texture2D cover)
        {
            //新tex，大小用哪个都无所谓，因为之前保证了所有素材大小一致
            Texture2D newTex = new Texture2D(background.width, background.height);

            Color[] bgColors = background.GetPixels();
            Color[] cvColors = cover.GetPixels();
            Color[] newColors = new Color[background.width * background.height];

            for (int x = 0; x < newTex.width; x++)
                for (int y = 0; y < newTex.height; y++)
                {
                    int index = x + y * background.width;
                    //混合背景和封面
                    //注意：这个函数只适用于背景色完全不透明
                    newColors[index] = BlendColorNormally(bgColors[index], cvColors[index]);
                }

            //newTex.SetPixels(newColors);
            newTex.SetPixels(newColors);
            newTex.Apply();

            return newTex;
        }

        //注意：这个函数只适用于背景色完全不透明
        //如果需要考虑背景色透明的函数，请看“混合模式”的链接
        public static Color BlendColorNormally(Color background, Color cover)
        {
            float CoverAlpha = cover.a;
            Color blendColor;
            blendColor.r = cover.r * CoverAlpha + background.r * (1 - CoverAlpha);
            blendColor.g = cover.g * CoverAlpha + background.g * (1 - CoverAlpha);
            blendColor.b = cover.b * CoverAlpha + background.b * (1 - CoverAlpha);
            blendColor.a = 1;
            return blendColor;
        }

        public static Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);

            for (int h = 0; h < result.height; ++h)
            {
                for (int w = 0; w < result.width; ++w)
                {
                    Color newColor = source.GetPixelBilinear((float)w / (float)result.width, (float)h / (float)result.height);
                    result.SetPixel(w, h, newColor);
                }
            }

            result.Apply();
            return result;
        }
    }
}
