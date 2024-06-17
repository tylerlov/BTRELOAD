using FIMSpace.FEditor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    [CustomEditor(typeof(FOptimizer_ComplexShape))]
    [CanEditMultipleObjects]
    public class FOptimizerComplexShapeEditor : FOptimizer_BaseEditor
    {
        private SerializedProperty sp_AutoPrecision;
        private SerializedProperty sp_AutoReferenceMesh;
        private SerializedProperty sp_DrawPositionHandles;
        private SerializedProperty sp_ScalingHandles;
        //private SerializedProperty sp_shapes;

        public static bool DrawDetectionSetup = false;

        protected override void ConvertToV2(FOptimizer_Base old, Optimizer_Base n)
        {
            base.ConvertToV2(old, n);

            FOptimizer_ComplexShape oldc = old as FOptimizer_ComplexShape;

            n.UseMultiShape = true;
            n.ShapePos = oldc.ShapePos;
            n.Shapes = new List<Optimizer_Base.MultiShapeBound>();

            for (int i = 0; i < oldc.Shapes.Count; i++)
            {
                Optimizer_Base.MultiShapeBound nsh = new Optimizer_Base.MultiShapeBound();
                nsh.position = oldc.Shapes[i].position;
                nsh.radius = oldc.Shapes[i].radius;
                nsh.transform = oldc.Shapes[i].transform;
                n.Shapes.Add(nsh);
            }

            new SerializedObject(n).Update();
            new SerializedObject(n).ApplyModifiedProperties();
            EditorUtility.SetDirty(n);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            drawCullIfNotSee = false;
            drawDetectionRadius = false;
            drawDetectionSphereHandle = false;
            drawHideable = false;
            drawDetectionOffset = false;
            sp_AutoPrecision = serializedObject.FindProperty("AutoPrecision");
            sp_AutoReferenceMesh = serializedObject.FindProperty("AutoReferenceMesh");
            sp_DrawPositionHandles = serializedObject.FindProperty("DrawPositionHandles");
            sp_ScalingHandles = serializedObject.FindProperty("ScalingHandles");
            //sp_shapes = serializedObject.FindProperty("Shapes");

            FOptimizer_ComplexShape complex = target as FOptimizer_ComplexShape;
            if (complex.Shapes == null || complex.Shapes.Count == 0)
                DrawDetectionSetup = true;
        }

        protected override void DefaultInspectorStack(FOptimizer_Base targetScript, bool endVert = true)
        {
            if (!DrawSetup)
            {
                base.DefaultInspectorStack(targetScript, true);
                return;
            }

            base.DefaultInspectorStack(targetScript, false);

            FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 1, 4);
            FOptimizer_ComplexShape complex = targetScript as FOptimizer_ComplexShape;

            EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(new Color(0.975f, 0.975f, 0.975f, .325f)));

            if (complex.Shapes == null) complex.Shapes = new List<FOptimizer_ComplexShape.FOptComplex_DetectionSphere>();

            EditorGUI.indentLevel++;
            if (!DrawDetectionSetup)
            {
                DrawDetectionSetup = EditorGUILayout.Foldout(DrawDetectionSetup, " Detection Spheres Setup (" + complex.Shapes.Count + ")", true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.BeginHorizontal();

                DrawDetectionSetup = EditorGUILayout.Foldout(DrawDetectionSetup, " Detection Spheres Setup (" + complex.Shapes.Count + ")", true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
                EditorGUI.indentLevel--;
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(new GUIContent("+", "Adding new detection sphere to optimizer list"), new GUILayoutOption[2] { GUILayout.Width(20), GUILayout.Height(15) }))
                {
                    Undo.RecordObject(serializedObject.targetObject, "Adding Detection Sphere");
                    EditorGUI.BeginChangeCheck();
                    Vector3 newPos = Vector3.zero;
                    if (complex.Shapes.Count > 0) newPos = (complex.Shapes[complex.Shapes.Count - 1].position + Vector3.up / 2);
                    FOptimizer_ComplexShape.FOptComplex_DetectionSphere sph = new FOptimizer_ComplexShape.FOptComplex_DetectionSphere();
                    sph.position = newPos;
                    complex.Shapes.Add(sph);
                    serializedObject.ApplyModifiedProperties();
                    EditorGUI.EndChangeCheck();
                }

                EditorGUILayout.EndHorizontal();

                FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 4);

                EditorGUIUtility.labelWidth = 20;

                GUILayoutOption[] op = new GUILayoutOption[1] { GUILayout.MinWidth(60) };

                for (int i = 0; i < complex.Shapes.Count; i++)
                {
                    if (complex.Shapes[i] == null) continue;

                    EditorGUILayout.BeginHorizontal();
                    Vector3 vPos = complex.Shapes[i].position;

                    Undo.RecordObject(serializedObject.targetObject, "Changing Detection Sphere");

                    EditorGUILayout.LabelField("[" + i + "] ", new GUILayoutOption[1] { GUILayout.MaxWidth(28) });
                    GUILayout.FlexibleSpace();
                    EditorGUI.BeginChangeCheck();

                    complex.Shapes[i].transform = (Transform)EditorGUILayout.ObjectField(complex.Shapes[i].transform, typeof(Transform), true, new GUILayoutOption[1] { GUILayout.Width(66) });//EditorGUILayout.ObjectField(GUIContent.none, complex.Shapes[i].transform, op);
                    //EditorGUILayout.ObjectField(sp_shapes.GetArrayElementAtIndex(i), GUIContent.none, new GUILayoutOption[1] { GUILayout.Width(50) });//EditorGUILayout.ObjectField(GUIContent.none, complex.Shapes[i].transform, op);
                    //EditorGUILayout.PropertyField(sp_shapes.GetArrayElementAtIndex(i), new GUIContent("E"), new GUILayoutOption[1] { GUILayout.Width(30) });//EditorGUILayout.ObjectField(GUIContent.none, complex.Shapes[i].transform, op);
                    vPos.x = EditorGUILayout.FloatField("x", complex.Shapes[i].position.x, op);
                    vPos.y = EditorGUILayout.FloatField("y", complex.Shapes[i].position.y, op);
                    vPos.z = EditorGUILayout.FloatField("z", complex.Shapes[i].position.z, op);
                    complex.Shapes[i].radius = EditorGUILayout.FloatField("s", complex.Shapes[i].radius, op);
                    EditorGUI.EndChangeCheck();
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(new GUIContent("X", "Removing detection sphere from list"), new GUILayoutOption[2] { GUILayout.Width(20), GUILayout.Height(15) }))
                    {
                        Undo.RecordObject(serializedObject.targetObject, "Removing Detection Sphere");
                        EditorGUI.BeginChangeCheck();
                        complex.Shapes.RemoveAt(i);
                        EditorGUI.EndChangeCheck();

                        return;
                    }

                    EditorGUILayout.EndHorizontal();

                    complex.Shapes[i].position = vPos;
                }

                EditorGUIUtility.labelWidth = 0;
                EditorGUIUtility.fieldWidth = 0;
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(2);

            //FEditor_Styles.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 1, 3);
            Undo.RecordObject(target, "Detection Sphere Variable");
            EditorGUI.BeginChangeCheck();
            targetScript.DetectionRadius = EditorGUILayout.FloatField(new GUIContent("Detection Radius", "Radius multiplier for all detection spheres"), targetScript.DetectionRadius);
            EditorGUIUtility.labelWidth = 158;
            EditorGUILayout.PropertyField(sp_DrawPositionHandles);
            if (complex.DrawPositionHandles)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(sp_ScalingHandles);
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndChangeCheck();

            EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(new Color(0.975f, 0.975f, 0.975f, .325f)));
            EditorGUILayout.PropertyField(sp_AutoReferenceMesh);
            EditorGUIUtility.labelWidth = 0;
            if (!complex.AutoReferenceMesh) GUI.enabled = false;
            if (GUILayout.Button(new GUIContent("Auto Detect Spheres For Mesh", "Automatically creating detection spheres basing on target mesh structure")))
            {
                Undo.RecordObject(serializedObject.targetObject, "Generating Auto Shape");
                EditorGUI.BeginChangeCheck();
                complex.GenerateAutoShape();
                EditorGUI.EndChangeCheck();
            }
            EditorGUILayout.PropertyField(sp_AutoPrecision);
            EditorGUILayout.EndVertical();

            if (!complex.AutoReferenceMesh) GUI.enabled = true;
            GUILayout.Space(2);

            EditorGUILayout.EndVertical();
        }

        protected override void OnSceneGUI()
        {
            FOptimizer_ComplexShape complex = target as FOptimizer_ComplexShape;

            //if (!DrawDetectionSetup)
            if (complex.DrawPositionHandles)
                if (complex.Shapes != null)
                {
                    Matrix4x4 m = complex.transform.localToWorldMatrix;
                    Matrix4x4 mw = complex.transform.worldToLocalMatrix;

                    Undo.RecordObject(complex, "Changing position of detection spheres");

                    for (int i = 0; i < complex.Shapes.Count; i++)
                    {
                        Matrix4x4 mt;
                        Matrix4x4 mtw;

                        if (complex.Shapes[i].transform == null)
                        {
                            mt = m;
                            mtw = mw;
                        }
                        else
                        {
                            mt = complex.Shapes[i].transform.localToWorldMatrix;
                            mtw = complex.Shapes[i].transform.worldToLocalMatrix;
                        }

                        Vector3 pos = mt.MultiplyPoint(complex.Shapes[i].position);

                        if (complex.ScalingHandles)
                        {
                            Vector3 scaled = FEditor_TransformHandles.ScaleHandle(Vector3.one * complex.Shapes[i].radius, pos, Quaternion.identity, .3f, true, true);
                            complex.Shapes[i].radius = scaled.x;
                        }

                        Vector3 transformed = FEditor_TransformHandles.PositionHandle(pos, Quaternion.identity, .3f, true, !complex.ScalingHandles);
                        if (Vector3.Distance(transformed, pos) > 0.00001f) complex.Shapes[i].position = mtw.MultiplyPoint(transformed);
                    }
                }
        }
    }
}