using UnityEngine;

namespace GameCore.UI
{
    public class ParallaxBackground : InfiniteBackground
    {
        public float parallaxFactor;
        public Vector2 positionDelta;

        private void Update()
        {
            var cameraPos = Tools.instance.mainCamera.transform.position;

            transform.position = new(cameraPos.x * parallaxFactor + positionDelta.x, cameraPos.y * parallaxFactor + positionDelta.y);
        }
    }
}
