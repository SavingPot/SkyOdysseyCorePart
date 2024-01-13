using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCore;
using Sirenix.OdinInspector;
using GameCore.High;
using SP.Tools.Unity;

namespace GameCore.UI
{
    public class ParallaxBackground : InfiniteBackground
    {
        public float parallaxFactor;
        public Vector2 positionDelta;

        private void Update()
        {
            Color color = new(
                GM.instance.globalLight.color.r * GM.instance.globalLight.intensity,
                GM.instance.globalLight.color.g * GM.instance.globalLight.intensity,
                GM.instance.globalLight.color.b * GM.instance.globalLight.intensity);

            foreach (var sr in renderers)
            {
                sr.color = color;
            }

            var cameraPos = Tools.instance.mainCamera.transform.position;

            transform.position = new(cameraPos.x * parallaxFactor + positionDelta.x, cameraPos.y * parallaxFactor + positionDelta.y);
        }
    }
}
