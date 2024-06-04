using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ChildToSpline))]
public class ChildToSplineEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ChildToSpline cts = (ChildToSpline)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate"))
        {
            cts.ConvertChildToSpline();
        }
    }
}
