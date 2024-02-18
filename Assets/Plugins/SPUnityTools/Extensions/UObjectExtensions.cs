using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SP.Tools.Unity
{
    public static class UObjectExtensions
    {
        public static void RemoveComponent<T>(this GameObject gameObject) => gameObject.RemoveComponent(typeof(T));

        public static void RemoveComponent(this GameObject gameObject, Type type)
        {
            var com = gameObject.GetComponent(type);

            if (com)
                GameObject.Destroy(com);
        }

        public static void DestroyChildren(this GameObject go) => go.transform.DestroyChildren();


        public static void SetMaterial(this LineRenderer lineRenderer, Material mat) => lineRenderer.material = mat;

        public static void SetWidth(this LineRenderer lineRenderer, float width)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }

        public static void SetColor(this LineRenderer lineRenderer, float colorF) => SetColor(lineRenderer, new Color(colorF, colorF, colorF));

        public static void SetColor(this LineRenderer lineRenderer, Color color)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return GetOrAddComponent(gameObject, typeof(T)) as T;
        }

        public static T FindComponent<T>(this Component com, string child)
        {
            return com.transform.Find(child).GetComponent<T>();
        }

        public static Component GetOrAddComponent(this GameObject gameObject, Type type)
        {
            var t = gameObject.GetComponent(type);
            return t ? t : gameObject.AddComponent(type);
        }



        public static Vector2 LeftPoint(this BoxCollider2D c)
        {
            return c.transform.TransformPoint(new(c.offset.x - c.size.x * 0.5f, c.offset.y));
        }

        public static Vector2 RightPoint(this BoxCollider2D c)
        {
            return c.transform.TransformPoint(new(c.offset.x + c.size.x * 0.5f, c.offset.y));
        }

        public static Vector2 LeftUpPoint(this BoxCollider2D c)
        {
            return c.transform.TransformPoint(new(c.offset.x - c.size.x * 0.5f, c.offset.y + c.size.y * 0.5f));
        }

        public static Vector2 RightUpPoint(this BoxCollider2D c)
        {
            return c.transform.TransformPoint(new(c.offset.x + c.size.x * 0.5f, c.offset.y + c.size.y * 0.5f));
        }

        public static Vector2 UpPoint(this BoxCollider2D c)
        {
            return c.transform.TransformPoint(new(c.offset.x, c.offset.y + c.size.y * 0.5f));
        }

        public static Vector2 LeftDownPoint(this BoxCollider2D c)
        {
            return c.transform.TransformPoint(new(c.offset.x - c.size.x * 0.5f, c.offset.y - c.size.y * 0.5f));
        }

        public static Vector2 RightDownPoint(this BoxCollider2D c)
        {
            return c.transform.TransformPoint(new(c.offset.x + c.size.x * 0.5f, c.offset.y - c.size.y * 0.5f));
        }

        public static Vector2 DownPoint(this BoxCollider2D c)
        {
            return c.transform.TransformPoint(new(c.offset.x, c.offset.y - c.size.y * 0.5f));
        }

        public static bool IsInCollider(this BoxCollider2D c, Vector2 worldPos)
        {
            var localPos = c.transform.InverseTransformPoint(worldPos);
            var offset = c.offset;
            var halfSizeX = c.size.x * 0.5f;
            var halfSizeY = c.size.y * 0.5f;

            return localPos.x > offset.x - halfSizeX && localPos.x < offset.x + halfSizeX &&
                    localPos.y > offset.y - halfSizeY && localPos.y < offset.y + halfSizeY;
        }



        public static bool TryGetComponentInChildren<T>(this Component com, out T comOut) where T : Component => UObjectTools.GetComponentInChildren<T>(com, out comOut);

        public static bool TryGetComponentInChildren(this Component com, Type type, out Component comOut) => UObjectTools.GetComponentInChildren(com, type, out comOut);

        public static bool TryGetComponentInParent<T>(this Component com, out T comOut) where T : Component => UObjectTools.GetComponentInParent<T>(com, out comOut);

        public static bool TryGetComponentInParent(this Component com, Type type, out Component comOut) => UObjectTools.GetComponentInParent(com, type, out comOut);




        public static bool TryGetComponentInChildren<T>(this GameObject com, out T comOut) where T : Component => UObjectTools.GetComponentInChildren<T>(com, out comOut);

        public static bool TryGetComponentInChildren(this GameObject com, Type type, out Component comOut) => UObjectTools.GetComponentInChildren(com, type, out comOut);

        public static bool TryGetComponentInParent<T>(this GameObject com, out T comOut) where T : Component => UObjectTools.GetComponentInParent<T>(com, out comOut);

        public static bool TryGetComponentInParent(this GameObject com, Type type, out Component comOut) => UObjectTools.GetComponentInParent(com, type, out comOut);
    }
}
