using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshPrefabPlacer))]
public class MeshPrefabPlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MeshPrefabPlacer placer = (MeshPrefabPlacer)target;

        // Check if MeshFilter component exists
        MeshFilter meshFilter = placer.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            EditorGUILayout.HelpBox("This GameObject needs a MeshFilter component!", MessageType.Error);
        }
        else if (meshFilter.sharedMesh == null)
        {
            EditorGUILayout.HelpBox("The MeshFilter doesn't have a mesh assigned!", MessageType.Warning);
        }

        // Check if prefab is assigned
        if (placer.prefabToPlace == null)
        {
            EditorGUILayout.HelpBox("Please assign a prefab to place!", MessageType.Warning);
        }

        DrawDefaultInspector();

        EditorGUI.BeginDisabledGroup(meshFilter == null || meshFilter.sharedMesh == null || placer.prefabToPlace == null);
        if (GUILayout.Button("Generate Prefabs"))
        {
            placer.GeneratePrefabs();
        }
        EditorGUI.EndDisabledGroup();
    }

    private void OnEnable()
    {
        // This will make sure the inspector updates when components are added or removed
        EditorApplication.update += Update;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Update;
    }

    private void Update()
    {
        // Force the inspector to repaint
        if (target != null)
        {
            Repaint();
        }
    }
}