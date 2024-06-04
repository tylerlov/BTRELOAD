using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CurvySplineControlPointAdjuster))]
public class CurvySplineControlPointAdjusterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws the default inspector

        CurvySplineControlPointAdjuster script = (CurvySplineControlPointAdjuster)target;

        if (GUILayout.Button("Apply"))
        {
            ApplyAdjustmentsWithUndo(script);
        }
    }

    private void ApplyAdjustmentsWithUndo(CurvySplineControlPointAdjuster script)
    {
        // Start a group for the undo operations
        Undo.SetCurrentGroupName("Adjust Control Points");

        int group = Undo.GetCurrentGroup();

        foreach (Transform child in script.transform)
        {
            // Record the current state of the object for undo
            Undo.RecordObject(child, "Adjust Control Point");

            // Apply the adjustments as before
            Vector3 localPos = child.localPosition;
            localPos.x *= script.xMultiplier;
            localPos.y *= script.yMultiplier;
            localPos.z *= script.zMultiplier;
            child.localPosition = localPos;
        }

        // Register the undo group. This step is crucial for grouping all changes into one undo step.
        Undo.CollapseUndoOperations(group);
    }
}