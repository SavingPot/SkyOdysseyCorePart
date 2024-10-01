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

        public void AddRenderers(string textureId, int count, int sortingOrder, float scaleOfPerObject = 1)
        {
            if (count % 2 != 0 && count != 1)
            {
                Debug.LogError("���ޱ�������Ⱦ����������Ϊż����1!");
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
                sr.transform.localScale = new(scaleOfPerObject, scaleOfPerObject);
                renderers.Add(sr);
            }
        }
    }
}
