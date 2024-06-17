using FIMSpace.FEditor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FOptimizer_ObstacleDetection))]
    public class FOptimizerEditor_ObstacleDetection : FOptimizer_BaseEditor
    {
        private SerializedProperty sp_CoveragePrecision;
        private SerializedProperty sp_CoverageMask;
        //private SerializedProperty sp_MemoryTolerance;
        private SerializedProperty sp_CoverageScale;
        private SerializedProperty sp_CustomRayPoints;
        //private SerializedProperty sp_coverageOffsets;

        private static bool drawDetectionSetup = true;

        protected override void ConvertToV2(FOptimizer_Base old, Optimizer_Base n)
        {
            base.ConvertToV2(old, n);

            FOptimizer_ObstacleDetection oldo = old as FOptimizer_ObstacleDetection;

            n.UseObstacleDetection = true;
            n.CoveragePrecision = oldo.CoveragePrecision;
            n.CoverageScale = oldo.CoverageScale;
            n.CoverageMask = oldo.CoverageMask;
            n.CustomCoveragePoints = oldo.CustomCoveragePoints;
            n.CoverageOffsets = oldo.CoverageOffsets;

            new SerializedObject(n).Update();
            new SerializedObject(n).ApplyModifiedProperties();
            EditorUtility.SetDirty(n);
        }

        protected override void OnEnable()
        {
            sp_CoveragePrecision = serializedObject.FindProperty("CoveragePrecision");
            sp_CoverageMask = serializedObject.FindProperty("CoverageMask");
            sp_CoverageScale = serializedObject.FindProperty("CoverageScale");
            sp_CustomRayPoints = serializedObject.FindProperty("CustomCoveragePoints");
            //sp_coverageOffsets = serializedObject.FindProperty("coverageOffsets");
            //sp_MemoryTolerance = serializedObject.FindProperty("MemoryTolerance");

            base.OnEnable();
            drawCullIfNotSee = false;
        }

        public override void OnInspectorGUI()
        {
            //FOptimizer_Base targetScript = (FOptimizer_Base)target;
            //if ( !targetScript.CullIfNotSee )
            //{
            //    EditorGUILayout.HelpBox("Are you sure you don't want to use CullIfNotSee? It will help a lot detecting objects behind wall with more performant way!", MessageType.Warning);
            //}

            base.OnInspectorGUI();
        }

        protected override void DefaultInspectorStack(FOptimizer_Base targetScript, bool endVert = true)
        {
            base.DefaultInspectorStack(targetScript, false);

            FOptimizer_ObstacleDetection obstacles = targetScript as FOptimizer_ObstacleDetection;

            EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(new Color(0f, 0f, 1f, .04f)));
            GUILayout.Label("Obstacle Detection Parameters", EditorStyles.boldLabel);
            FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 2);

            if (!obstacles.CustomCoveragePoints)
            {
                if (obstacles.CoveragePrecision == -1)
                {
                    bool use = false;
                    use = EditorGUILayout.Toggle("Check Obstalces", use);
                    if (use) obstacles.CoveragePrecision = 1;
                    serializedObject.ApplyModifiedProperties();
                }
                else
                    EditorGUILayout.PropertyField(sp_CoveragePrecision);
            }

            EditorGUILayout.PropertyField(sp_CoverageMask);
            EditorGUILayout.PropertyField(sp_CoverageScale);

            EditorGUIUtility.labelWidth = 170;
            EditorGUILayout.PropertyField(sp_CustomRayPoints);
            EditorGUIUtility.labelWidth = 0;

            if (obstacles.CustomCoveragePoints)
            {
                DrawCoveragePoints(obstacles);
            }

            //EditorGUILayout.PropertyField(sp_MemoryTolerance);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }

        private void DrawCoveragePoints(FOptimizer_ObstacleDetection obstacles)
        {
            EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(new Color(0.975f, 0.975f, 0.975f, .325f)));

            EditorGUI.indentLevel++;
            if (!drawDetectionSetup)
            {
                drawDetectionSetup = EditorGUILayout.Foldout(drawDetectionSetup, " Custom coverage points", true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.BeginHorizontal();

                drawDetectionSetup = EditorGUILayout.Foldout(drawDetectionSetup, " Custom coverage points", true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
                EditorGUI.indentLevel--;
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(new GUIContent("+", "Adding new coverage detection point"), new GUILayoutOption[2] { GUILayout.Width(20), GUILayout.Height(15) }))
                {
                    Undo.RecordObject(serializedObject.targetObject, "Adding Coverage Point");
                    EditorGUI.BeginChangeCheck();
                    Vector3 newPos = Vector3.zero;
                    if (obstacles.CoverageOffsets.Count > 0) newPos = obstacles.CoverageOffsets[obstacles.CoverageOffsets.Count - 1] + Vector3.up / 2;
                    obstacles.CoverageOffsets.Add(newPos);
                    EditorGUI.EndChangeCheck();
                }

                EditorGUILayout.EndHorizontal();

                FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 4);

                EditorGUIUtility.labelWidth = 20;

                GUILayoutOption[] op = new GUILayoutOption[1] { GUILayout.MinWidth(60) };

                for (int i = 0; i < obstacles.CoverageOffsets.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    Vector3 vPos;

                    Undo.RecordObject(serializedObject.targetObject, "Changing Coverage Point");

                    EditorGUILayout.LabelField("[" + i + "] ", new GUILayoutOption[1] { GUILayout.MaxWidth(28) });
                    GUILayout.FlexibleSpace();
                    EditorGUI.BeginChangeCheck();
                    vPos.x = EditorGUILayout.FloatField("x", obstacles.CoverageOffsets[i].x, op);
                    vPos.y = EditorGUILayout.FloatField("y", obstacles.CoverageOffsets[i].y, op);
                    vPos.z = EditorGUILayout.FloatField("z", obstacles.CoverageOffsets[i].z, op);
                    EditorGUI.EndChangeCheck();
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(new GUIContent("X", "Removing detection sphere from list"), new GUILayoutOption[2] { GUILayout.Width(20), GUILayout.Height(15) }))
                    {
                        Undo.RecordObject(serializedObject.targetObject, "Removing Detection Sphere");
                        EditorGUI.BeginChangeCheck();
                        obstacles.CoverageOffsets.RemoveAt(i);
                        obstacles.CoverageOffsets.RemoveAt(i);
                        EditorGUI.EndChangeCheck();

                        return;
                    }

                    EditorGUILayout.EndHorizontal();

                    obstacles.CoverageOffsets[i] = vPos;
                }
            }

            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndVertical();
        }
    }
}