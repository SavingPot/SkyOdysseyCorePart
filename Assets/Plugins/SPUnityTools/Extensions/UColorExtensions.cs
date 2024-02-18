using UnityEngine;

namespace SP.Tools.Unity
{
    public static class UColorExtensions
    {
        public static void SetAlpha(ref this Color color, float a) => color = new Color(color.r, color.g, color.b, a);
    }
}
