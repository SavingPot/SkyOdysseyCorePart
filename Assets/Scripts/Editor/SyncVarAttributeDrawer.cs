using Mirror;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using kcp2k;
using Sirenix.OdinInspector.Editor.ValueResolvers;

public class SyncVarAttributeDrawer : MonoBehaviour
{
    public class MirrorSyncVarAttributeDrawer : OdinAttributeDrawer<SyncVarAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            CallNextDrawer(label);
            GUILayout.EndVertical();
            GUILayout.Label("同步变量", EditorStyles.miniLabel, GUILayoutOptions.Width(52f));
            GUILayout.EndHorizontal();
        }
    }
}
