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


        public static void AddObject(this JObject jo, string name)
        {
            jo.Add(new JProperty(name, new JObject()));
        }
        public static void AddObject(this JObject jo, string name, params object[] content)
        {
            jo.Add(new JProperty(name, new JObject(content)));
        }
        public static void AddObject(this JObject jo, string name, object content)
        {
            jo.Add(new JProperty(name, new JObject(content)));
        }

        public static void AddProperty(this JObject jo, string name)
        {
            jo.Add(new JProperty(name));
        }
        public static void AddProperty(this JObject jo, string name, params object[] content)
        {
            jo.Add(new JProperty(name, content));
        }
        public static void AddProperty(this JObject jo, string name, object content)
        {
            jo.Add(new JProperty(name, content));
        }

        public static void AddArray(this JObject jo, string name)
        {
            jo.Add(new JProperty(name, new JArray()));
        }
        public static void AddArray(this JObject jo, string name, JArray other)
        {
            jo.Add(new JProperty(name, new JArray(other)));
        }
        public static void AddArray(this JObject jo, string name, params object[] content)
        {
            jo.Add(new JProperty(name, new JArray(content)));
        }
        public static void AddArray(this JObject jo, string name, object content)
        {
            jo.Add(new JProperty(name, new JArray(content)));
        }





        /* -------------------------------------------------------------------------- */
        /*                                   JToken                                   */
        /* -------------------------------------------------------------------------- */
        public static void AddObject(this JToken jt, string name)
        {
            AddObject((JObject)jt, name);
        }
        public static void AddObject(this JToken jt, string name, params object[] content)
        {
            AddObject((JObject)jt, name, content);
        }
        public static void AddObject(this JToken jt, string name, object content)
        {
            AddObject((JObject)jt, name, content);
        }

        public static void AddProperty(this JToken jt, string name)
        {
            AddProperty((JObject)jt, name);
        }
        public static void AddProperty(this JToken jt, string name, params object[] content)
        {
            AddProperty((JObject)jt, name, content);
        }
        public static void AddProperty(this JToken jt, string name, object content)
        {
            AddProperty((JObject)jt, name, content);
        }

        public static void AddArray(this JToken jt, string name)
        {
            AddArray((JObject)jt, name);
        }
        public static void AddArray(this JToken jt, string name, JArray other)
        {
            AddArray((JObject)jt, name, other);
        }
        public static void AddArray(this JToken jt, string name, params object[] content)
        {
            AddArray((JObject)jt, name, content);
        }
        public static void AddArray(this JToken jt, string name, object content)
        {
            AddArray((JObject)jt, name, content);
        }
    }
}
