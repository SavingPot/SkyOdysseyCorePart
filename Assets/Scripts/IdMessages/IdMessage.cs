using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace GameCore
{
    public class IdMessage : MonoBehaviour, IStringId
    {
        public string id { get; set; }

        ///<summary>
        ///string1 = type(方法种类), string2 = param(参数)
        ///</summary>
        public Action<string, string> CustomMethod = (_, _) => { };

        public Dictionary<string, string> customVars = new();

#if UNITY_EDITOR
        [Sirenix.OdinInspector.Button("输出 ID")] private void EditorOutputID() => Debug.Log($"ID: {id}");
#endif

        protected virtual void Awake()
        {
            IdMessageCenter.messages.Add(this);
        }

        public virtual void SetID(string id)
        {
            this.id = id;
            gameObject.name = id;
        }

        private void OnDestroy()
        {
            IdMessageCenter.Remove(this);
        }
    }
}
