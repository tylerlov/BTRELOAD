using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GenerateCurvySplineFromGameobjects))]
public class GenerateCurvySplineFromGameobjectsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GenerateCurvySplineFromGameobjects script = (GenerateCurvySplineFromGameobjects)target;

        if (GUILayout.Button("Generate Curvy Spline"))
        {
            Undo.RecordObject(script, "Generate Curvy Spline");
            script.GenerateSpline();
            EditorUtility.SetDirty(script);
        }
    }
}
