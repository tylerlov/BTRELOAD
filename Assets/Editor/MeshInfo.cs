using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

public class MeshInfo : EditorWindow
{
    private Vector2 scrollPosition;

    [MenuItem("Window/Analysis/Mesh Info")]
    public static void ShowWindow()
    {
        GetWindow<MeshInfo>("Mesh Info");
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private void OnGUI()
    {
        var selectedObject = Selection.activeGameObject;

        if (selectedObject)
        {
            EditorGUILayout.LabelField($"Selected Object: {selectedObject.name}", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            var meshFilters = selectedObject.GetComponentsInChildren<MeshFilter>(true);
            var skinnedMeshRenderers = selectedObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (meshFilters.Length > 0 || skinnedMeshRenderers.Length > 0)
            {
                foreach (var meshFilter in meshFilters)
                {
                    DisplayMeshInfo(meshFilter.sharedMesh, meshFilter.gameObject.name, "MeshFilter");
                }

                foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
                {
                    DisplayMeshInfo(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.gameObject.name, "SkinnedMeshRenderer");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No GameObjects with MeshFilter or SkinnedMeshRenderer found in the hierarchy.", MessageType.Info);
            }
            
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("Select a GameObject to see mesh information.", MessageType.Info);
        }
    }

    private void DisplayMeshInfo(Mesh mesh, string objectName, string componentType)
    {
        if (mesh)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Object: {objectName} ({componentType})", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Vertices: {mesh.vertexCount}");
            EditorGUILayout.LabelField($"Triangles: {mesh.triangles.Length / 3}");
            EditorGUILayout.LabelField($"SubMeshes: {mesh.subMeshCount}");
            EditorGUILayout.LabelField($"UV Sets: {(mesh.uv.Length > 0 ? mesh.uv.Length / mesh.vertexCount : 0)}");
            EditorGUILayout.LabelField($"Bounds: {mesh.bounds}");
            EditorGUILayout.LabelField($"Read/Write Enabled: {mesh.isReadable}");

            if (componentType == "SkinnedMeshRenderer")
            {
                EditorGUILayout.LabelField($"Blend Shapes: {mesh.blendShapeCount}");
            }
        }
    }
}
