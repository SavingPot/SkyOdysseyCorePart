using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCore;
using Sirenix.OdinInspector;
using GameCore.High;
using SP.Tools.Unity;

namespace GameCore.UI
{
    public class InfiniteBackground : MonoBehaviour
    {
        public List<SpriteRenderer> renderers = new();

        public void AddRenderers(string textureId, int count, int sortingOrder)
        {
            if (count % 2 != 0)
            {
                Debug.LogError("无限背景的渲染器数量必须为偶数!");
            }

            var sprite = ModFactory.CompareTexture(textureId).sprite;
            var deltaFactor = sprite.texture.width / sprite.pixelsPerUnit;

            for (int i = -count / 2; i < count; i++)
            {
                var sr = UObjectTools.CreateComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.material = GInit.instance.spriteDefaultMat;
                sr.sortingOrder = sortingOrder;
                sr.transform.SetParent(transform);
                sr.transform.AddPosX(i * deltaFactor);
                renderers.Add(sr);
            }
        }
    }
}
