using SP.Tools;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SP.Tools.Unity
{
    public static class UTransformExtensions
    {
        public static void SetPos(this Transform trans, float x, float y) => trans.position = new Vector3(x, y);

        public static void SetPos(this Transform trans, Vector2 vector) => trans.position = vector;

        public static void SetPosX(this Transform trans, float x) => trans.position = new Vector3(x, trans.position.y);

        public static void SetPosY(this Transform trans, float y) => trans.position = new Vector3(trans.position.x, y);

        public static void AddPos(this Transform trans, float x, float y) => trans.position = new Vector3(trans.position.x + x, trans.position.y + y);

        public static void AddPos(this Transform trans, Vector2 vector) => trans.position = new Vector3(trans.position.x + vector.x, trans.position.y + vector.y);

        public static void AddPosX(this Transform trans, float x) => trans.position = new Vector3(trans.position.x + x, trans.position.y);

        public static void AddPosY(this Transform trans, float y) => trans.position = new Vector3(trans.position.x, trans.position.y + y);

        public static void SetLocalPos(this Transform trans, float x, float y) => trans.localPosition = new Vector3(x, y);

        public static void SetLocalPos(this Transform trans, Vector2 vector) => trans.localPosition = vector;

        public static void SetLocalPosX(this Transform trans, float x) => trans.localPosition = new Vector3(x, trans.localPosition.y);

        public static void SetLocalPosY(this Transform trans, float y) => trans.localPosition = new Vector3(trans.localPosition.x, y);

        public static void SetLocalPosZ(this Transform trans, float z) => trans.localPosition = new Vector3(trans.localPosition.x, trans.localPosition.y, z);

        public static void AddLocalPos(this Transform trans, float x, float y) => trans.localPosition = new Vector3(trans.localPosition.x + x, trans.localPosition.y + y);

        public static void AddLocalPos(this Transform trans, Vector2 vector) => trans.localPosition = new Vector3(trans.localPosition.x + vector.x, trans.localPosition.y + vector.y);

        public static void AddLocalPosX(this Transform trans, float x) => trans.localPosition = new Vector3(trans.localPosition.x + x, trans.localPosition.y);

        public static void AddLocalPosY(this Transform trans, float y) => trans.localPosition = new Vector3(trans.localPosition.x, trans.localPosition.y + y);

        public static void AddLocalPosZ(this Transform trans, float z) => trans.localPosition = new Vector3(trans.localPosition.x, trans.localPosition.y, trans.localPosition.z + z);

        public static void SetScale(this Transform trans, float x, float y) => trans.localScale = new Vector3(x, y);

        public static void SetScale(this Transform trans, float total) => trans.localScale = new Vector3(total, total);

        public static void SetScale(this Transform trans, Vector2 vector) => trans.localScale = vector;

        public static void SetScaleMultiply(this Transform trans, float multiple) => trans.localScale = new Vector3(trans.localScale.x * multiple, trans.localScale.y * multiple);

        public static void SetScaleMultiply(this Transform trans, float xMultiple, float yMultiple) => trans.localScale = new Vector3(trans.localScale.x * xMultiple, trans.localScale.y * yMultiple);

        public static void SetScaleMultiply(this Transform trans, Vector2 vector) => trans.localScale = new Vector3(trans.localScale.x * vector.x, trans.localScale.y * vector.y);

        public static void SetScaleX(this Transform trans, float x) => trans.localScale = new Vector3(x, trans.localScale.y);

        public static void SetScaleY(this Transform trans, float y) => trans.localScale = new Vector3(trans.localScale.x, y);

        public static void SetScaleXNegativeAbs(this Transform trans) => trans.localScale = new Vector3(-trans.localScale.x.Abs(), trans.localScale.y);

        public static void SetScaleYNegativeAbs(this Transform trans) => trans.localScale = new Vector3(trans.localScale.x, -trans.localScale.y.Abs());

        public static void SetScaleXAbs(this Transform trans) => trans.localScale = new Vector3(trans.localScale.x.Abs(), trans.localScale.y);

        public static void SetScaleYAbs(this Transform trans) => trans.localScale = new Vector3(trans.localScale.x, trans.localScale.y.Abs());

        public static void SetScaleXMultiply(this Transform trans, float x) => trans.localScale = new Vector3(trans.localScale.x * x, trans.localScale.y);

        public static void SetScaleYMultiply(this Transform trans, float y) => trans.localScale = new Vector3(trans.localScale.x, trans.localScale.y * y);

        public static void DestroyChildren(this Transform trans)
        {
            for (int i = 0; i < trans.childCount; i++)
            {
                GameObject.Destroy(trans.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 获取某对象从根节点到自身的路径
        /// </summary>
        [ChineseName("获取路径")]
        public static string GetPath(this Transform trans, string separator = "/")
        {
            if (trans == null)
            {
                Debug.LogError("对象为空");
                return "";
            }

            StringBuilder tempPath = new StringBuilder(trans.name);
            Transform tempTra = trans;
            string g = separator;
            while (tempTra.parent != null)
            {
                tempTra = tempTra.parent;
                tempPath.Insert(0, tempTra.name + g);
            }
            return tempPath.ToString();
        }

        [ChineseName("获取子物体")]
        public static Transform[] GetChildren(this Transform trans)
        {
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < trans.childCount; i++)
            {
                children.Add(trans.GetChild(i));
            }
            return children.ToArray();
        }
    }
}
