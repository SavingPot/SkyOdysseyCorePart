using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SP.Tools.Unity
{
    public static class UObjectTools
    {
        public static bool GetComponent<T>(Component com, out T comOut) where T : Component
        {
            comOut = com.GetComponent<T>();

            return comOut;
        }

        public static bool GetComponent(Component com, Type type, out Component comOut)
        {
            comOut = com.GetComponent(type);

            return comOut;
        }

        public static bool GetComponentInChildren<T>(Component com, out T comOut) where T : Component
        {
            comOut = com.GetComponentInChildren<T>();

            return comOut;
        }

        public static bool GetComponentInChildren(Component com, Type type, out Component comOut)
        {
            comOut = com.GetComponentInChildren(type);

            return comOut;
        }

        public static bool GetComponentInParent<T>(Component com, out T comOut) where T : Component
        {
            comOut = com.GetComponentInParent<T>();

            return comOut;
        }

        public static bool GetComponentInParent(Component com, Type type, out Component comOut)
        {
            comOut = com.GetComponentInParent(type);

            return comOut;
        }




        public static bool GetComponent<T>(GameObject com, out T comOut) where T : Component
        {
            comOut = com.GetComponent<T>();

            return comOut;
        }

        public static bool GetComponent(GameObject com, Type type, out Component comOut)
        {
            comOut = com.GetComponent(type);

            return comOut;
        }

        public static bool GetComponentInChildren<T>(GameObject com, out T comOut) where T : Component
        {
            comOut = com.GetComponentInChildren<T>();

            return comOut;
        }

        public static bool GetComponentInChildren(GameObject com, Type type, out Component comOut)
        {
            comOut = com.GetComponentInChildren(type);

            return comOut;
        }

        public static bool GetComponentInParent<T>(GameObject com, out T comOut) where T : Component
        {
            comOut = com.GetComponentInParent<T>();

            return comOut;
        }

        public static bool GetComponentInParent(GameObject com, Type type, out Component comOut)
        {
            comOut = com.GetComponentInParent(type);

            return comOut;
        }




        public static bool FindComponentWithType<T>(out T comOut) where T : Component
        {
            comOut = GameObject.FindObjectOfType<T>();

            return comOut;
        }

        public static bool FindComponentWithType(Type type, out UnityEngine.Object comOut)
        {
            comOut = GameObject.FindObjectOfType(type);

            return comOut;
        }

        public static T CreateComponent<T>(string name = "New Object") where T : Component
        {
            return new GameObject(name).AddComponent<T>();
        }

        public static Component CreateComponent(Type type, string name = "New Object")
        {
            return new GameObject(name).AddComponent(type);
        }
    }

    [Serializable]
    public class ComponentPool<T> where T : Component
    {
        public Stack<T> stack = new();

        public virtual T Get(params object[] param)
        {
            T obj = stack.Count == 0 ? Generation(param) : stack.Pop();

            Apply(obj, param);

            return obj;
        }

        public virtual void Recover(T obj)
        {
            obj.gameObject.SetActive(false);

            stack.Push(obj);
        }

        public virtual T Generation(object[] param)
        {
            return new GameObject().AddComponent<T>();
        }

        public virtual void Apply(T obj, object[] param)
        {
            obj.gameObject.SetActive(true);
        }
    }
}
