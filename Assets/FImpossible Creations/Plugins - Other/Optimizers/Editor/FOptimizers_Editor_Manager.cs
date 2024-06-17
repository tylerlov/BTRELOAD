using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    [CustomEditor(typeof(FOptimizers_Manager))]
    public class FOptimizer_EditorManager : Editor
    {
        public static bool DrawTools = false;
        public static bool DrawSetup = true;
        public static bool DrawDynamicSetup = true;


        void OnEnable()
        {
            FOptimizers_Manager targetScript = (FOptimizers_Manager)target;
            targetScript.OnValidate();
            targetScript.SetGet();
        }

        private static bool loadTry = false;

        public override void OnInspectorGUI()
        {
            FOptimizers_Manager targetScript = (FOptimizers_Manager)target;
            Color preCol = GUI.color;

            GUI.enabled = false;
            UnityEditor.EditorGUILayout.ObjectField("Script", UnityEditor.MonoScript.FromMonoBehaviour(targetScript), typeof(FOptimizers_Manager), false);
            GUI.enabled = true;

            EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(new Color(0.975f, 0.975f, 0.975f, .325f)));

            EditorGUI.indentLevel++;
            DrawSetup = EditorGUILayout.Foldout(DrawSetup, "Main Setup", true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
            EditorGUI.indentLevel--;

            if (!loadTry)
            {
                loadTry = true;
                if (Resources.Load("Optimizers/FLOD_Terrain Reference") == null)
                {
                    EditorGUILayout.HelpBox("There are no components reference types. Hit right mouse button on this component and then 'Reset'", MessageType.Error);
                }
            }

            if (DrawSetup)
            {
                FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 4);
                GUILayout.Space(1f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("TargetCamera"));

                if (!targetScript.ExistThroughScenes)
                    GUI.color = new Color(1f, 0.9f, 0.7f, 0.9f);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("ExistThroughScenes"));
                GUI.color = preCol;

                //FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 1, 2);
                //EditorGUILayout.HelpBox("Drag and drop custom LOD types components to this list", MessageType.None);
                //EditorGUI.indentLevel++;
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("CustomComponentsDefinition"), true);
                //EditorGUI.indentLevel--;
                GUILayout.Space(2f);
            }

            EditorGUILayout.EndVertical();


            FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 4);

            EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(new Color(0.975f, 0.975f, 0.975f, .325f)));

            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            DrawDynamicSetup = EditorGUILayout.Foldout(DrawDynamicSetup, "Dynamic Optimizing Setup", true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
            GUILayout.FlexibleSpace();

            EditorGUIUtility.labelWidth = 90;
            targetScript.Advanced = EditorGUILayout.Toggle("Advanced", targetScript.Advanced);
            EditorGUIUtility.labelWidth = 0;

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;

            if (DrawDynamicSetup)
            {
                FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 4);
                GUILayout.Space(1f);

                if (!targetScript.Advanced)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("WorldScale"));

                    GUILayout.FlexibleSpace();
                    GUILayout.Label(new GUIContent("(End Distance: " + Mathf.Round(targetScript.Distances[targetScript.Distances.Length - 1]) + ")"));

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("UpdateBoost"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("DetectCameraFreeze"));
                    FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 1, 2);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("GizmosAlpha"));
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("UpdateBoost"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("DetectCameraFreeze"));
                    if (targetScript.DetectCameraFreeze)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("MoveTreshold"));

                    FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 1, 2);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Debugging"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("GizmosAlpha"));
                    FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 1, 2);

                    EditorGUILayout.LabelField(new GUIContent("Clocks Distance Ranges", "Check Gizmos in scene view for more (At Main Camera position)"), new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter });
                    //GUILayout.Space(2f);

                    GUIStyle smallStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 9 };
                    GUI.color = new Color(1f, 1f, 1f, 0.7f);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("Highest Priority", "[Check scene view in camera position]\n" + FOptimizers_Manager.RangeInfos[0]), smallStyle);
                    GUI.color = new Color(1f, 1f, 0.7f, 0.7f);
                    GUILayout.Label(new GUIContent("High Priority", "[Check scene view in camera position]\n" + FOptimizers_Manager.RangeInfos[1]), smallStyle);
                    GUI.color = new Color(0.55f, 1f, .8f, 0.7f);
                    GUILayout.Label(new GUIContent("Medium Priority", "[Check scene view in camera position]\n" + FOptimizers_Manager.RangeInfos[2]), smallStyle);
                    GUI.color = new Color(0.6f, .82f, 1f, 0.7f);
                    GUILayout.Label(new GUIContent("Low Priority", "[Check scene view in camera position]\n" + FOptimizers_Manager.RangeInfos[3]), smallStyle);
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(-1f);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUIUtility.labelWidth = 5;
                    GUI.color = new Color(0.7f, 1f, 0.7f, 0.95f);
                    targetScript.Distances[0] = EditorGUILayout.FloatField(" ", targetScript.Distances[0]);
                    GUI.color = new Color(1f, 1f, 0.7f, 0.95f);
                    targetScript.Distances[1] = EditorGUILayout.FloatField(" ", targetScript.Distances[1]);
                    GUI.color = new Color(0.55f, 1f, .8f, 0.95f);
                    targetScript.Distances[2] = EditorGUILayout.FloatField(" ", targetScript.Distances[2]);
                    GUI.color = new Color(0.6f, .82f, 1f, 0.95f);
                    targetScript.Distances[3] = EditorGUILayout.FloatField(" ", targetScript.Distances[3]);
                    EditorGUIUtility.labelWidth = 0;
                    GUI.color = preCol;

                    EditorGUILayout.EndHorizontal();
                }

            }

            EditorGUILayout.EndVertical();

            FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 4);

            //GUILayout.Space(2f);
            EditorGUILayout.HelpBox("This manager component is supporting work of dynamic objects optimization and handling smooth transitions between LOD levels.", UnityEditor.MessageType.Info);
            serializedObject.ApplyModifiedProperties();

            FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 4);
            //EditorGUILayout.LabelField(new GUIContent("Helper Buttons"), new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter });


            EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(new Color(0.975f, 0.975f, 0.975f, .325f)));

            EditorGUI.indentLevel++;
            DrawTools = EditorGUILayout.Foldout(DrawTools, "Scene Tools", true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
            EditorGUI.indentLevel--;

            if (DrawTools)
            {
                FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 1, 3);

                float finDistance = targetScript.Distances[targetScript.Distances.Length - 1] + targetScript.Distances[targetScript.Distances.Length - 2];

                GUILayout.Space(2f);

                if (targetScript.TargetCamera != null)
                {
                    if (targetScript.TargetCamera.farClipPlane != finDistance)
                    {
                        if (GUILayout.Button(new GUIContent("Sync Camera Clipping Plane with 'World Scale' (" + Mathf.Round(finDistance) + ")", "Make sure range will not be too small for your game needs")))
                        {
                            targetScript.TargetCamera.farClipPlane = finDistance;
                        }

                        GUILayout.Space(2f);

                    }

                    if (RenderSettings.fog)
                    {
                        bool showFogButton = false;

                        float targetFogDensity = CalculateTargetFogDensity(finDistance, RenderSettings.fogMode);

                        if (RenderSettings.fogMode == FogMode.Linear)
                        {
                            if (Mathf.Round(RenderSettings.fogEndDistance) != Mathf.Round(finDistance * 0.965f)) showFogButton = true;
                        }
                        else
                        {
                            if (RenderSettings.fogDensity != targetFogDensity) showFogButton = true;
                        }

                        if (showFogButton)
                        {
                            if (GUILayout.Button(new GUIContent("Sync Scene Fog with Camera's 'Far Clipping Plane'", "Applying fog density/distance value in scene settings to end on Camera's 'Far Clipping Plane' distance")))
                            {
                                SyncFog(finDistance);
                            }

                            GUILayout.Space(2f);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(new GUIContent("Enable scene fog and sync range", "Enabling fog in scene settings and syncing it's range with Camera's 'Far Clipping Plane' distance")))
                        {
                            SyncFog(finDistance);
                            RenderSettings.fog = true;
                        }

                        GUILayout.Space(2f);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("There is no camera in the scene!", MessageType.Warning);
                }

                FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 6);

                //if (GUILayout.Button(new GUIContent("Enable scene fog and sync range", "Enabling fog in scene settings and syncing it's range with Camera's 'Far Clipping Plane' distance")))
                //{
                //    SyncFog(finDistance);
                //    RenderSettings.fog = true;
                //}

            }

            EditorGUILayout.EndVertical();
        }


        public static float CalculateTargetFogDensity(float maxDistance, FogMode mode)
        {
            float targetFogDensity = RenderSettings.fogDensity;

            if (mode == FogMode.Exponential)
                targetFogDensity = Mathf.Pow(maxDistance, -0.705899f);
            else
                if (mode == FogMode.ExponentialSquared)
                targetFogDensity = Mathf.Pow(maxDistance, -0.841899f);

            if (maxDistance > 1000)
            {
                float prog = Mathf.InverseLerp(400f, 10000f, maxDistance);

                if (mode == FogMode.ExponentialSquared)
                {
                    targetFogDensity *= Mathf.Lerp(1f, 0.6f, 1f - Mathf.Pow(2f, -6f * prog));

                    float prog2 = Mathf.InverseLerp(500f, 5000f, maxDistance);
                    targetFogDensity *= Mathf.Lerp(1f, 0.9f, prog2);
                }
                else
                {
                    targetFogDensity *= Mathf.Lerp(1f, 0.5f, 1f - Mathf.Pow(2f, -7.5f * prog));

                    float prog2 = Mathf.InverseLerp(500f, 4000f, maxDistance);
                    targetFogDensity *= Mathf.Lerp(1f, 0.9f, prog2);
                }
            }

            return targetFogDensity;
        }


        public static void SyncFog(float maxDistance)
        {
            RenderSettings.fogDensity = CalculateTargetFogDensity(maxDistance, RenderSettings.fogMode);
            RenderSettings.fogEndDistance = maxDistance * 0.965f;
        }
    }

}
