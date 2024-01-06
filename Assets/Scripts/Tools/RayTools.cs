using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public static class RayTools
    {
        /// <summary>
        /// 发射并返回射线.
        /// Hit and return a ray.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static RaycastHit2D Hit(Vector2 startPoint, Vector2 direction)
        {
            RaycastHit2D hit = Physics2D.Raycast(startPoint, direction);

            return hit;
        }

        /// <summary>
        /// 发射并返回射线.
        /// Hit and return a ray.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="direction"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static RaycastHit2D Hit(Vector2 startPoint, Vector2 direction, float length)
        {
            RaycastHit2D hit = Physics2D.Raycast(startPoint, direction, length);

            return hit;
        }

        /// <summary>
        /// 发射并返回射线.
        /// Hit and return a ray.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="direction"></param>
        /// <param name="length"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public static RaycastHit2D Hit(Vector2 startPoint, Vector2 direction, float length, int layerMask)
        {
            RaycastHit2D hit = Physics2D.Raycast(startPoint, direction, length, layerMask);

            return hit;
        }



        /// <summary>
        /// 发射并返回射线.
        /// Hit and return a ray.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="direction"></param>
        /// <param name="layer"></param>
        /// <param name="length"></param>
        /// <param name="debugRay"></param>
        /// <returns></returns>
        public static RaycastHit2D[] HitAll(Vector2 startPoint, Vector2 direction)
        {
            RaycastHit2D[] hit = Physics2D.RaycastAll(startPoint, direction);

            return hit;
        }

        /// <summary>
        /// 发射并返回射线.
        /// Hit and return a ray.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="direction"></param>
        /// <param name="layer"></param>
        /// <param name="length"></param>
        /// <param name="debugRay"></param>
        /// <returns></returns>
        public static RaycastHit2D[] HitAll(Vector2 startPoint, Vector2 direction, float length)
        {
            RaycastHit2D[] hit = Physics2D.RaycastAll(startPoint, direction, length);

            return hit;
        }

        /// <summary>
        /// 发射并返回射线.
        /// Hit and return a ray.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="direction"></param>
        /// <param name="layerMask"></param>
        /// <param name="length"></param>
        /// <param name="debugRay"></param>
        /// <returns></returns>
        public static RaycastHit2D[] HitAll(Vector2 startPoint, Vector2 direction, float length, int layerMask)
        {
            RaycastHit2D[] hit = Physics2D.RaycastAll(startPoint, direction, length, layerMask);

            return hit;
        }



        public static bool TryHitAll(Vector2 startPoint, Vector2 direction, out RaycastHit2D[] results)
        {
            results = HitAll(startPoint, direction);

            return results != null && results.Length > 0;
        }

        public static bool TryHitAll(Vector2 startPoint, Vector2 direction, float length, out RaycastHit2D[] results)
        {
            results = HitAll(startPoint, direction, length);

            return results != null && results.Length > 0;
        }

        public static bool TryHitAll(Vector2 startPoint, Vector2 direction, float length, int layerMask, out RaycastHit2D[] results)
        {
            results = HitAll(startPoint, direction, length, layerMask);

            return results != null && results.Length > 0;
        }



        /// <summary>
        /// angle == 0 是正右!!!!!!!! size.x and y 必须 >=0!!!!!!!!!!!
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <param name="angle"></param>
        /// <param name="layerMask"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryOverlapBox(Vector2 point, Vector2 size, float angle, int layerMask, out Collider2D result)
        {
            result = Physics2D.OverlapBox(point, size, angle, layerMask);

            return result != null;
        }

        /// <summary>
        /// angle == 0 是正右!!!!!!!! size.x and y 必须 >=0!!!!!!!!!!!
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <param name="angle"></param>
        /// <param name="layer"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryOverlapBox(Vector2 point, Vector2 size, float angle, out Collider2D result)
        {
            result = Physics2D.OverlapBox(point, Vector2.one, 0);

            return result != null;
        }



        /// <summary>
        /// radius 必须 >=0!!!!!!!!!!!
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <param name="layerMask"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryOverlapCircle(Vector2 point, float radius, int layerMask, out Collider2D result)
        {
            result = Physics2D.OverlapCircle(point, radius, layerMask);

            return result != null;
        }

        /// <summary>
        /// radius 必须 >=0!!!!!!!!!!!
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryOverlapCircle(Vector2 point, float radius, out Collider2D result)
        {
            result = Physics2D.OverlapCircle(point, radius);

            return result != null;
        }
    }
}
