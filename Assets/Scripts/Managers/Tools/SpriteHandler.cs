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
    }
}
