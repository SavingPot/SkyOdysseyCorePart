using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.High
{
    public static class ObjectTools
    {
        public static Tools tools => Tools.instance;

        public static SpriteRenderer CreateSpriteObject(Transform parent, string name = "New Sprite Object")
        {
            //创建组件
            SpriteRenderer r = CreateSpriteObject(name);

            //设置父物体
            r.transform.SetParent(parent);

            return r;
        }
        public static SpriteRenderer CreateSpriteObject(string name = "New Sprite Object")
        {
            //创建物体并添加组件
            var sr = CreateObject(name).AddComponent<SpriteRenderer>();

            //设置材质为 光照材质
            sr.material = GInit.instance.spriteLitMat;

            return sr;
        }

        public static GameObject CreateObject(string name = "New Object") => new(name);
    }
}
