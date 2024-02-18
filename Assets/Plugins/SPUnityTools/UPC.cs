using UnityEngine;

namespace SP.Tools.Unity
{
    /// <summary>
    /// 即 UIPositionCurrent, 界面位置指针
    /// </summary>
    public static class UPC
    {
        public static Vector4 Middle { get; } = new(0.5f, 0.5f, 0.5f, 0.5f);
        public static Vector4 Left { get; } = new(0, 0.5f, 0, 0.5f);
        public static Vector4 Right { get; } = new(1, 0.5f, 1, 0.5f);
        public static Vector4 Down { get; } = new(0.5f, 0, 0.5f, 0);
        public static Vector4 Up { get; } = new(0.5f, 1, 0.5f, 1);
        public static Vector4 UpperLeft { get; } = new(0, 1, 0, 1);
        public static Vector4 UpperRight { get; } = new(1, 1, 1, 1);
        public static Vector4 LowerLeft { get; } = new(0, 0, 0, 0);
        public static Vector4 LowerRight { get; } = new(1, 0, 1, 0);
        public static Vector4 StretchLeft { get; } = new(0, 0, 0, 1);
        public static Vector4 StretchCenter { get; } = new(0.5f, 0, 0.5f, 1);
        public static Vector4 StretchRight { get; } = new(1, 0, 1, 1);
        public static Vector4 StretchDouble { get; } = new(0, 0, 1, 1);
        public static Vector4 StretchTop { get; } = new(0, 1, 1, 1);
        public static Vector4 StretchMiddle { get; } = new(0, 0.5f, 1, 0.5f);
        public static Vector4 StretchBottom { get; }= new (0, 0, 1, 0);
    }
}
