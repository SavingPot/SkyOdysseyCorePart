using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SP.Tools.Unity;
using SP.Tools;

namespace GameCore
{
    public static class JsonExtensions
    {
        public static Vector2 ToVector2(this JToken j) => j.ToObject<float[]>().ToVector2();

        public static Vector3 ToVector3(this JToken j) => j.ToObject<float[]>().ToVector3();

        public static Vector2Int ToVector2Int(this JToken j) => j.ToObject<int[]>().ToVector2Int();

        public static Vector3Int ToVector3Int(this JToken j) => j.ToObject<int[]>().ToVector3Int();

        public static T ToEnum<T>(this JToken j) where T : struct => j.ToString().ToEnum<T>();

        public static bool ToBool(this JToken j) => j.ToObject<bool>();

        public static float ToFloat(this JToken j) => j.ToString().ToFloat();

        public static int ToInt(this JToken j) => j.ToString().ToInt();
    }
}
