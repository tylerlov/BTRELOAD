using UnityEngine;

public class CurvySplineControlPointAdjuster : MonoBehaviour
{
    public float xMultiplier = 1f;
    public float yMultiplier = 1f;
    public float zMultiplier = 1f;

    // Method to adjust control points, called from the custom editor
    public void AdjustControlPoints()
    {
        foreach (Transform child in transform)
        {
            Vector3 localPos = child.localPosition;
            localPos.x *= xMultiplier;
            localPos.y *= yMultiplier;
            localPos.z *= zMultiplier;
            child.localPosition = localPos;
        }
    }
}
