using SP.Tools.Unity;
using UnityEngine;
using UnityEngine.Audio;

namespace GameCore
{
    public static class ToolsSundryExtensions
    {
        public static int LayerMaskExcept(this int num) => Tools.LayerMaskExcept(num);

        public static int LayerMaskOnly(this int num) => Tools.LayerMaskOnly(num);

        public static void SetMaterialToSpriteDefault(this LineRenderer lineRenderer) => lineRenderer.SetMaterial(GInit.instance.spriteDefaultMat);

        public static Component GetComponentByFullName(this Component c, string fullName) => Tools.GetComponentByFullName(c, fullName);

        public static Component[] GetComponentsByFullName(this Component c, string fullName) => Tools.GetComponentsByFullName(c, fullName);

        public static void SetVolume(this AudioMixerGroup audioMixerGroup, float value) => GAudio.SetMixerVolume(audioMixerGroup, value);

        public static Vector2 ToScreenPos(this Vector3 vec) => Tools.instance.mainCamera.WorldToScreenPoint(vec);

        public static Vector2 ToWorldPos(this Vector3 vec) => Tools.instance.mainCamera.ScreenToWorldPoint(vec);

        public static Vector2 ToScreenPos(this Vector2 vec) => Tools.instance.mainCamera.WorldToScreenPoint(vec);

        public static Vector2 ToWorldPos(this Vector2 vec) => Tools.instance.mainCamera.ScreenToWorldPoint(vec);
    }
}
