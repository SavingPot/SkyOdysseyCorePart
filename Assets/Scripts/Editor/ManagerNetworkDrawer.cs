using Mirror;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using kcp2k;
using Sirenix.OdinInspector.Editor.ValueResolvers;

namespace GameCore.Drawers
{
    [CustomEditor(typeof(NetworkManager), true)]
    [CanEditMultipleObjects]
    public class ManagerNetworkDrawer : Editor
    {
        SerializedProperty spawnListProperty;
        ReorderableList spawnList;
        protected NetworkManager networkManager;

        protected void Init()
        {
            if (spawnList == null)
            {
                networkManager = target as NetworkManager;
                spawnListProperty = serializedObject.FindProperty("spawnPrefabs");
                spawnList = new ReorderableList(serializedObject, spawnListProperty)
                {
                    drawHeaderCallback = DrawHeader,
                    drawElementCallback = DrawChild,
                    onReorderCallback = Changed,
                    onRemoveCallback = RemoveButton,
                    onChangedCallback = Changed,
                    onAddCallback = AddButton,
                    // this uses a 16x16 icon. other sizes make it stretch.
                    elementHeight = 16
                };
            }
        }

        public override void OnInspectorGUI()
        {
            Init();
            DrawDefaultInspector();
            EditorGUI.BeginChangeCheck();
            spawnList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        static void DrawHeader(Rect headerRect)
        {
            GUI.Label(headerRect, "注册的预制体:");
        }

        internal void DrawChild(Rect r, int index, bool isActive, bool isFocused)
        {
            SerializedProperty prefab = spawnListProperty.GetArrayElementAtIndex(index);
            GameObject go = (GameObject)prefab.objectReferenceValue;

            GUIContent label;
            if (go == null)
            {
                label = new GUIContent("Empty", "将一个有 NetworkIdentity 的预制体拽到这里");
            }
            else
            {
                NetworkIdentity identity = go.GetComponent<NetworkIdentity>();
                label = new GUIContent(go.name, identity != null ? $"资源ID: [{identity.assetId}]" : "没有 Network Identity");
            }

            GameObject newGameObject = (GameObject)EditorGUI.ObjectField(r, label, go, typeof(GameObject), false);

            if (newGameObject != go)
            {
                if (newGameObject != null && !newGameObject.GetComponent<NetworkIdentity>())
                {
                    Debug.LogError($"预制体 {newGameObject} 不能被生成 NetworkIdentity.");
                    return;
                }
                prefab.objectReferenceValue = newGameObject;
            }
        }

        internal void Changed(ReorderableList list)
        {
            EditorUtility.SetDirty(target);
        }

        internal void AddButton(ReorderableList list)
        {
            spawnListProperty.arraySize += 1;
            list.index = spawnListProperty.arraySize - 1;

            SerializedProperty obj = spawnListProperty.GetArrayElementAtIndex(spawnListProperty.arraySize - 1);
            obj.objectReferenceValue = null;

            spawnList.index = spawnList.count - 1;

            Changed(list);
        }

        internal void RemoveButton(ReorderableList list)
        {
            spawnListProperty.DeleteArrayElementAtIndex(spawnList.index);
            if (list.index >= spawnListProperty.arraySize)
            {
                list.index = spawnListProperty.arraySize - 1;
            }
        }
    }
}
