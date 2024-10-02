using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public static class SpriteHandler
    {
        public static Sprite CombineSprite(Sprite background, Sprite cover)
        {
            var newTexture = TextureHandler.CombineTexture(background.texture, cover.texture);

            return Sprite.Create(newTexture, background.rect, background.pivot, background.pixelsPerUnit);
        }

        public static Sprite ScaleSprite(Sprite background, int targetWidth, int targetHeight)
        {
            var newTexture = TextureHandler.ScaleTexture(background.texture, targetWidth, targetHeight);

            return Sprite.Create(newTexture, background.rect, background.pivot, background.pixelsPerUnit);
        }

        public static Sprite CutLowerThreeQuarterKeepingUpperArea(Sprite sprite)
        {
            var newTexture = TextureHandler.CutLowerThreeQuarterKeepingUpperArea(sprite.texture);

            return Sprite.Create(newTexture, sprite.rect, new(0.5f, 0.5f), sprite.pixelsPerUnit);
        }

        public static Sprite CutLowerQuarterKeepingUpperArea(Sprite sprite)
        {
            var newTexture = TextureHandler.CutLowerQuarterKeepingUpperArea(sprite.texture);

            return Sprite.Create(newTexture, sprite.rect, new(0.5f, 0.5f), sprite.pixelsPerUnit);
        }

        public static Sprite CutUpperHalfKeepingLowerArea(Sprite sprite)
        {
            var newTexture = TextureHandler.CutUpperHalfKeepingLowerArea(sprite.texture);

            return Sprite.Create(newTexture, sprite.rect, new(0.5f, 0.5f), sprite.pixelsPerUnit);
        }

        public static Sprite CutLowerHalfKeepingUpperArea(Sprite sprite)
        {
            var newTexture = TextureHandler.CutLowerHalfKeepingUpperArea(sprite.texture);

            return Sprite.Create(newTexture, sprite.rect, new(0.5f, 0.5f), sprite.pixelsPerUnit);
        }

        public static Sprite CutUpperLeft(Sprite sprite)
        {
            var newTexture = TextureHandler.CutUpperLeft(sprite.texture);

            return Sprite.Create(newTexture, sprite.rect, new(0.5f, 0.5f), sprite.pixelsPerUnit);
        }

        public static Sprite CutUpperRight(Sprite sprite)
        {
            var newTexture = TextureHandler.CutUpperRight(sprite.texture);

            return Sprite.Create(newTexture, sprite.rect, new(0.5f, 0.5f), sprite.pixelsPerUnit);
        }

        public static Sprite CutLowerLeft(Sprite sprite)
        {
            var newTexture = TextureHandler.CutLowerLeft(sprite.texture);

            return Sprite.Create(newTexture, sprite.rect, new(0.5f, 0.5f), sprite.pixelsPerUnit);
        }

        public static Sprite CutLowerRight(Sprite sprite)
        {
            var newTexture = TextureHandler.CutLowerRight(sprite.texture);

            return Sprite.Create(newTexture, sprite.rect, new(0.5f, 0.5f), sprite.pixelsPerUnit);
        }
    }
}
