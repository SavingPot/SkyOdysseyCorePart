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

        public static Sprite CutUpperHalfTextureKeepingLowerArea(Sprite sprite)
        {
            var newTexture = TextureHandler.CutUpperHalfTextureKeepingLowerArea(sprite.texture);

            return Sprite.Create(newTexture, sprite.rect, new(0.5f, 0.5f), sprite.pixelsPerUnit);
        }

        public static Sprite CutLowerHalfTextureKeepingUpperArea(Sprite sprite)
        {
            var newTexture = TextureHandler.CutLowerHalfTextureKeepingUpperArea(sprite.texture);

            return Sprite.Create(newTexture, sprite.rect, new(0.5f, 0.5f), sprite.pixelsPerUnit);
        }
    }
}
