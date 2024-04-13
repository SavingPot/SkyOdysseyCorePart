using UnityEngine;

namespace GameCore
{
    public static class AngleTools
    {
        public static float IncludedDegreeBetweenX(Vector2 vec) => Mathf.Atan2(vec.y, vec.x) * Mathf.Rad2Deg;


        /// <summary>
        /// 获取物体到另一个物体的角度 (float).
        /// Get the angle of an object to another object (float).
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static float GetAngleFloat(Vector2 from, Vector2 to) => Vector2.Angle(GetAngleVector2(from, to), Vector2.up);

        /// <summary>
        /// 获取物体到另一个物体的角度 (欧拉).
        /// Get the angle of an object to another object (euler).
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Vector3 GetAngleEuler(Vector2 from, Vector2 to) => new(0, 0, GetAngleFloat(from, to));

        /// <summary>
        /// 获取物体到另一个物体的角度 [Vector (可用于方向即 Direction)].
        /// Get the angle of an object to another object [Vector (direction)].
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Vector2 GetAngleVector2(Vector2 from, Vector2 to) => to - from;
    }
}