using System.Collections.Generic;
using FluffyUnderware.Curvy;
using UnityEditor;
using UnityEngine;

public class GenerateCurvySplineFromGameobjects : MonoBehaviour
{
    public GameObject sourceObject;
    private CurvySpline generatedSpline;

    [CustomEditor(typeof(GenerateCurvySplineFromGameobjects))]
    public class GenerateCurvySplineFromGameobjectsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GenerateCurvySplineFromGameobjects script = (GenerateCurvySplineFromGameobjects)target;

            if (GUILayout.Button("Generate Curvy Spline"))
            {
                script.GenerateSpline();
            }
        }
    }

    public void GenerateSpline()
    {
        if (sourceObject == null)
        {
            Debug.LogError("Source object is not set!");
            return;
        }

        Undo.RecordObject(this, "Generate Curvy Spline");

        // Remove existing spline if any
        if (generatedSpline != null)
        {
            Undo.DestroyObjectImmediate(generatedSpline.gameObject);
        }

        // Create new spline
        GameObject splineObject = new GameObject("Generated Curvy Spline");
        Undo.RegisterCreatedObjectUndo(splineObject, "Generate Curvy Spline");
        generatedSpline = splineObject.AddComponent<CurvySpline>();

        // Collect all child positions
        List<Vector3> positions = new List<Vector3>();
        CollectChildPositions(sourceObject.transform, positions);

        // Create spline segments
        foreach (Vector3 position in positions)
        {
            CurvySplineSegment segment = generatedSpline.Add();
            Undo.RecordObject(segment.transform, "Set Segment Position");
            segment.transform.position = position;
        }

        // Refresh the spline
        generatedSpline.Refresh();

        Debug.Log("Curvy Spline generated with " + positions.Count + " control points.");
    }

    private void CollectChildPositions(Transform parent, List<Vector3> positions)
    {
        positions.Add(parent.position);

        foreach (Transform child in parent)
        {
            CollectChildPositions(child, positions);
        }
    }
}
