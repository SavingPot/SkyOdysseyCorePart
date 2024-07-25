using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SP.Tools.Unity
{
    public static class UVectorExtensions
    {
        public static void Set(this Vector3 vec, float x, float y) => vec.Set(x, y, vec.z);

        public static Vector2 ToVector2(this IList<float> floats)
        {
            if (floats == null || floats.Count != 2)
                throw new ArgumentException();

            return new Vector2(floats[0], floats[1]);
        }

        public static Vector3 ToVector3(this IList<float> floats)
        {
            if (floats == null)
                throw new ArgumentNullException();

            if (floats.Count == 3)
                return new Vector3(floats[0], floats[1], floats[2]);
            if (floats.Count == 2)
                return new Vector3(floats[0], floats[1]);

            throw new ArgumentException("数组长度需为 2 或 3");
        }

        public static Vector2Int ToVector2Int(this IList<int> ints)
        {
            if (ints == null || ints.Count != 2)
                throw new ArgumentException();

            return new Vector2Int(ints[0], ints[1]);
        }

        public static Vector3Int ToVector3Int(this IList<int> ints)
        {
            if (ints == null)
                throw new ArgumentNullException();

            if (ints.Count == 3)
                return new Vector3Int(ints[0], ints[1], ints[2]);
            if (ints.Count == 2)
                return new Vector3Int(ints[0], ints[1]);

            throw new ArgumentException("数组长度需为 2 或 3");
        }

        public static Vector3Int ReturnChangedZ(this Vector3Int vec, int newZ) => new Vector3Int(vec.x, vec.y, newZ);


        public static Vector2Int ToInt2(this Vector2 vec) => VectorConverter.ToInt2(vec);
        public static Vector3Int ToInt3(this Vector2 vec, int z = 0) => VectorConverter.ToInt3(vec, z);
        public static Vector2Int ToInt2(this Vector3 vec) => VectorConverter.ToInt2(vec);
        public static Vector3Int ToInt3(this Vector3 vec) => VectorConverter.ToInt3(vec);
        public static Vector3Int ToInt3(this Vector3 vec, int z) => VectorConverter.ToInt3(vec, z);
        public static Vector2Int ToInt2(this Vector3Int vec) => VectorConverter.ToInt2(vec);
        public static Vector3Int ToInt3(this Vector2Int vec, int z = 0) => VectorConverter.ToInt3(vec, z);

        public static Vector2 To2(this Vector2Int vec) => VectorConverter.To2(vec);
        public static Vector2 To2(this Vector3Int vec) => VectorConverter.To2(vec);
        public static Vector3 To3(this Vector2 vec) => VectorConverter.To3(vec);
        public static Vector3 To3(this Vector2 vec, float z) => VectorConverter.To3(vec, z);
        public static Vector3 To3(this Vector2Int vec) => VectorConverter.To3(vec);
        public static Vector3 To3(this Vector2Int vec, float z) => VectorConverter.To3(vec, z);
        public static Vector3 To3(this Vector3Int vec) => VectorConverter.To3(vec);

        public static Vector2 Abs(this Vector2 vec) => new Vector2(Mathf.Abs(vec.x), Mathf.Abs(vec.y));
        public static Vector3 Abs(this Vector3 vec) => new Vector3(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
        public static Vector3Int Abs(this Vector3Int vec) => new Vector3Int(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
        public static Vector2Int Abs(this Vector2Int vec) => new Vector2Int(Mathf.Abs(vec.x), Mathf.Abs(vec.y));
    }
}
