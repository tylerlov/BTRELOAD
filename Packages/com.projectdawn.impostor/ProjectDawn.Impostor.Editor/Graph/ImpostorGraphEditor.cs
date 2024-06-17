using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace ProjectDawn.Impostor.Editor
{
    [CustomEditor(typeof(ImpostorGraph))]
    public class ImpostorGraphEditor : UnityEditor.Editor
    {
        public static ImpostorGraph DefaultImpostorGraph => AssetDatabase.LoadAssetAtPath<ImpostorGraph>("Packages/com.projectdawn.impostor/Content/Lit Impostor Graph.asset");

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (GUILayout.Button("Edit Graph"))
            {
                ImpostorGraphWindow.Open(target as ImpostorGraph);
            }

            serializedObject.ApplyModifiedProperties();
        }

        [OnOpenAsset]
        //Handles opening the editor window when double-clicking project files
        public static bool OnOpenAsset(int instanceID, int line)
        {
            ImpostorGraph project = EditorUtility.InstanceIDToObject(instanceID) as ImpostorGraph;
            if (project != null)
            {
                ImpostorGraphWindow.Open(project);
                return true;
            }
            return false;
        }
    }
}
