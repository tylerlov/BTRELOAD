using System.Collections.Generic;
using FluffyUnderware.Curvy;
using UnityEngine;

public class GenerateCurvySplineFromGameobjects : MonoBehaviour
{
    public GameObject sourceObject;
    private CurvySpline generatedSpline;

    public void GenerateSpline()
    {
        if (sourceObject == null)
        {
            Debug.LogError("Source object is not set!");
            return;
        }

        // Remove existing spline if any
        if (generatedSpline != null)
        {
            DestroyImmediate(generatedSpline.gameObject);
        }

        // Create new spline
        GameObject splineObject = new GameObject("Generated Curvy Spline");
        generatedSpline = splineObject.AddComponent<CurvySpline>();

        // Collect all child positions
        List<Vector3> positions = new List<Vector3>();
        CollectChildPositions(sourceObject.transform, positions);

        // Create spline segments
        foreach (Vector3 position in positions)
        {
            CurvySplineSegment segment = generatedSpline.Add();
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
