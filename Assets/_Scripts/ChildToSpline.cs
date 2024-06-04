using UnityEngine;
using FluffyUnderware.Curvy;

public class ChildToSpline : MonoBehaviour
{
    public void ConvertChildToSpline()
    {
        // Find all child objects of the parent object (excluding the parent itself)
        var children = GetComponentsInChildren<Transform>();
        int numberOfChildren = children.Length - 1;

        // Create a Curvy Splines spline
        var spline = gameObject.AddComponent<CurvySpline>();

        // Iterate through child objects and add them as control points
        for (int i = 0; i < numberOfChildren; i++)
        {
            // Get the child's local position
            Vector3 childLocalPosition = children[i + 1].localPosition;

            // Add the control point to the spline
            spline.Add(childLocalPosition);
        }

        // Refresh the spline to update its internal data
        spline.Refresh();
    }
}
