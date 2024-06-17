using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FOptimizing
{

    [CustomEditor(typeof(FOptimizer_Terrain))]
    public class FOptimizerEditorTerrain : FOptimizer_BaseEditor
    {
        private SerializedProperty sp_Terrain;
        private SerializedProperty sp_TerrainC;
        private SerializedProperty sp_SafeBorders;

        protected override void ConvertToV2(FOptimizer_Base old, Optimizer_Base n)
        {
            base.ConvertToV2(old, n);

            new SerializedObject(n).Update();
            new SerializedObject(n).ApplyModifiedProperties();
            EditorUtility.SetDirty(n);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            drawHiddenRange = false;
            drawDetectionSphereHandle = false;
            drawNothingToOptimizeWarning = false;
            sp_Terrain = serializedObject.FindProperty("Terrain");
            sp_TerrainC = serializedObject.FindProperty("TerrainCollider");
            sp_SafeBorders = serializedObject.FindProperty("SafeBorders");
        }

        protected override void OnSceneGUI()
        { }

        protected override void DefaultInspectorStack(FOptimizer_Base targetScript, bool endVert = true)
        {
            FOptimizer_Terrain targetTerr = targetScript as FOptimizer_Terrain;

            if (Application.isPlaying) GUI.enabled = false;
            EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(new Color(0.975f, 0.975f, 0.975f, .325f)));

            EditorGUI.indentLevel++;
            DrawSetup = EditorGUILayout.Foldout(DrawSetup, "Optimizer Setup", true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
            EditorGUI.indentLevel--;

            if (DrawSetup)
            {
                FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 4);
                GUILayout.Space(1f);

                EditorGUILayout.PropertyField(sp_MaxDist);
                targetScript.DetectionRadius = EditorGUILayout.FloatField(new GUIContent("Detection Radius", "Radius for controll spheres placed on terrain, they will define visibility triggering when camera lookin on or away"), targetScript.DetectionRadius);
                targetScript.DetectionRadius = targetTerr.LimitRadius(targetScript.DetectionRadius);
                EditorGUILayout.PropertyField(sp_SafeBorders);
                EditorGUILayout.PropertyField(sp_GizmosAlpha);

                //EditorGUILayout.PropertyField(serializedObject.FindProperty("ToOptimize"), true);

                GUILayout.Space(3f);
            }

            EditorGUILayout.EndVertical();
            if (Application.isPlaying) GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        protected override void DrawFadeDurationSlider(FOptimizer_Base targetScript)
        {
        }

        protected override void DrawToOptimizeStack(FOptimizer_Base targetScript)
        {
            EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(new Color(0.75f, 0.75f, 0.15f, 0.2f)));
            EditorGUILayout.PropertyField(sp_Terrain, true);
            EditorGUILayout.PropertyField(sp_TerrainC, true);
            EditorGUILayout.EndVertical();
        }
    }

}
