using FIMSpace.FEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    [CustomEditor(typeof(FOptimizer_Base))]
    [CanEditMultipleObjects]
    public class FOptimizer_BaseEditor : Editor
    {
        public static bool DrawSetup = true;
        public static bool DrawToOptimize = false;
        public static bool ClickedOnSlider = false;
        public static bool DrawAddCompOptions = false;

        private float distance = 0f;

        protected int selectedLOD = -1;
        protected bool drawHiddenRange = true;
        protected int selectedLODSlider = 0;
        private readonly int sliderControlId = "LODSliderIDHash".GetHashCode();

        protected SerializedProperty sp_LodLevels;
        protected SerializedProperty sp_FadeDuration;
        protected SerializedProperty sp_Shared;
        protected SerializedProperty sp_MaxDist;
        protected SerializedProperty sp_CullIfNotSee;
        protected SerializedProperty sp_DetectionRadius;
        protected SerializedProperty sp_GizmosAlpha;
        protected SerializedProperty sp_UnlockFirstLOD;


        protected bool drawDetectionRadius = true;
        protected bool drawNothingToOptimizeWarning = true;
        protected bool drawCullIfNotSee = true;
        protected bool drawDetectionOffset = true;
        protected bool drawHideable = true;
        protected bool drawDetectionSphereHandle = true;

        protected static Color individualColor = new Color(0.725f, 0.85f, 1f, 0.9f);
        protected static Color preCol;
        private bool isRunningInEditor = false;
        private bool isPrefabInEditor = false;


        protected virtual void ConvertToV2(FOptimizer_Base old, Optimizer_Base n)
        {
            n.MaxDistance = old.MaxDistance;
            n.OptimizingMethod = (EOptimizingMethod)(old.OptimizingMethod);
            n.DetectionRadius = old.DetectionRadius;
            n.DetectionBounds = old.DetectionBounds;
            n.DetectionOffset = old.DetectionOffset;
            n.AddToContainer = old.AddToContainer;
            n.CullIfNotSee = old.CullIfNotSee;
            n.LODLevels = old.LODLevels;
            n.FadeDuration = old.FadeDuration;
            n.LODPercent = old.LODPercent;
            n.HiddenCullAt = old.HiddenCullAt;
            n.Hideable = old.Hideable;

            EditorUtility.SetDirty(n);
            new SerializedObject(n).ApplyModifiedProperties();

            EssentialOptimizer e = n as EssentialOptimizer;
            if (e)
            {
                e.ToOptimize.Clear();

                for (int i = 0; i < old.ToOptimize.Count; i++)
                {
                    if (old.ToOptimize[i] == null) continue;
                    if (old.ToOptimize[i].Component == null) continue;
                    e.AddToOptimizeIfCan(old.ToOptimize[i].Component, null);
                }

                for (int i = 0; i < e.ToOptimize.Count; i++)
                {
                    e.ToOptimize[i].GenerateLODParameters();
                }

                #region Copying LOD Settings

                for (int i = 0; i < e.ToOptimize.Count; i++)
                {
                    //if (old.ToOptimize[i].Component is Light)
                    //{
                    //    if (e.ToOptimize[i].Component is Light)
                    //    {

                    //        // Copying Light LOD settings
                    //        for (int l = 0; l < e.ToOptimize[i].LODLevelsCount; l++)
                    //        {
                    //            LODI_Light light = e.ToOptimize[i].GetLODSetting(i) as LODI_Light;
                    //            if (light != null)
                    //            {
                    //                FLOD_Light oldL = old.ToOptimize[i].LODSet.LevelOfDetailSets[l] as FLOD_Light;
                    //                if (oldL)
                    //                {
                    //                    light.ChangeIntensity = oldL.ChangeIntensity;
                    //                    light.IntensityMul = oldL.IntensityMul;
                    //                    light.RangeMul = oldL.RangeMul;
                    //                    light.ShadowsStrength = oldL.ShadowsStrength;
                    //                    light.ShadowsMode = oldL.ShadowsMode;
                    //                    light.RenderMode = (LODI_Light.EOptLightMode)oldL.RenderMode;
                    //                    light.Disable = oldL.Disable;
                    //                }
                    //            }
                    //        }

                    //    }
                    //}
                    //else 
                    if (old.ToOptimize[i].Component is ParticleSystem)
                    {
                        if (e.ToOptimize[i].Component is ParticleSystem)
                        {

                            // Copying Light LOD settings
                            for (int l = 0; l < e.ToOptimize[i].LODLevelsCount; l++)
                            {
                                LODI_ParticleSystem light = e.ToOptimize[i].GetLODSetting(i) as LODI_ParticleSystem;
                                if (light != null)
                                {
                                    FLOD_ParticleSystem oldL = old.ToOptimize[i].LODSet.LevelOfDetailSets[l] as FLOD_ParticleSystem;
                                    if (oldL)
                                    {
                                        light.QualityLowerer = oldL.QualityLowerer;
                                        //light.EmmissionAmount = oldL.EmmissionAmount;
                                        //light.ParticleSizeMul = oldL.ParticleSizeMul;
                                        //light.BurstsAmount = oldL.BurstsAmount;
                                        //light.LifetimeAlpha = oldL.LifetimeAlpha;
                                        //light.OverDistanceMul = oldL.OverDistanceMul;
                                        //light.MaxParticlAmount = oldL.MaxParticlAmount;
                                        //light.Disable = oldL.Disable;
                                    }
                                }
                            }

                        }
                    }
                }

                #endregion

            }
            else
            {
                ScriptableOptimizer s = n as ScriptableOptimizer;
                if (s)
                {

                    s.SaveSetFilesInPrefab = !old.DrawSharedSettingsOptions;
                    s.ToOptimize.Clear();

                    for (int i = 0; i < old.ToOptimize.Count; i++)
                    {
                        if (old.ToOptimize[i] == null) continue;
                        if (old.ToOptimize[i].Component == null) continue;
                        MonoBehaviour mono = old.ToOptimize[i].Component as MonoBehaviour;
                        if (mono) s.AssignCustomComponentToOptimize(mono);
                        else
                            s.AssignComponentsToOptimizeFrom(old.ToOptimize[i].Component);
                    }

                    s.LODLevels = old.LODLevels + 1;
                    s.OnValidate();
                    s.LODLevels = old.LODLevels;

                    for (int i = 0; i < s.ToOptimize.Count; i++)
                    {
                        s.ToOptimize[i].RefreshLODAutoParametersSettings();
                    }

                    s.OnValidate();
                }
            }

            new SerializedObject(n).Update();
            new SerializedObject(n).ApplyModifiedProperties();
            EditorUtility.SetDirty(n);
        }


        protected virtual void OnEnable()
        {
            sp_LodLevels = serializedObject.FindProperty("LODLevels");
            sp_FadeDuration = serializedObject.FindProperty("FadeDuration");
            sp_Shared = serializedObject.FindProperty("DrawSharedSettingsOptions");

            sp_MaxDist = serializedObject.FindProperty("MaxDistance");
            sp_CullIfNotSee = serializedObject.FindProperty("CullIfNotSee");
            sp_DetectionRadius = serializedObject.FindProperty("DetectionRadius");
            sp_GizmosAlpha = serializedObject.FindProperty("GizmosAlpha");
            sp_UnlockFirstLOD = serializedObject.FindProperty("UnlockFirstLOD");

            if (target) // Unity 2020 have problem with that
            {
                FOptimizer_Base targetScript = (FOptimizer_Base)target;
                if (targetScript.ToOptimize.Count == 0) DrawAddCompOptions = true;
                if (Application.isPlaying) DrawSetup = false;

                targetScript.SyncWithReferences();
                targetScript.EditorResetLODValues();

                if (targetScript.ToOptimize.Count > 2)
                {
                    for (int i = 0; i < targetScript.ToOptimize.Count; i++)
                        targetScript.ToOptimize[i].Editor_HideProperties(true);
                }

                isRunningInEditor = Application.isEditor && !Application.isPlaying;


                targetScript.Editor_InIsolatedScene = targetScript.gameObject.scene.rootCount == 1;
                if (targetScript.gameObject.scene.rootCount >= 1) isPrefabInEditor = false; else isPrefabInEditor = true;

                if (isPrefabInEditor)
                {
                    if (targetScript.Editor_JustCreated)
                    {

#if UNITY_2019_3_OR_NEWER
                        // I assume here that with unity 2019.3 user is working with assets pipeline v2

                        if (targetScript.ToOptimize.Count > 0)
                            if (targetScript.ToOptimize[0].LODSet == null)
                            {
                                Debug.Log("<b><color=red>[OPTIMIZERS EDITOR]</color></b> <b>Using Asset Pipeline V2 is having bug of creating prefabs with optimizers or it's because nested prefabs</b> in it by dragging them from scene. Please remove and add again optimizer component on prefab or move 'Lod Levels' slider. (Waiting for bugfix in next versions of asset pipeline v2)");
                            }
#else
                        UnityEngine.Object prefab = FOptimizers_LODTransport.GetPrefab(targetScript.gameObject);

                        if (prefab)
                            if (prefab is GameObject)
                                FOptimizers_LODTransport.ClearPrefabFromUnusedOptimizersSubAssets((GameObject)prefab);
#endif
                        //if (target != null) // Unity 2020 have problem with that
                        //    EditorUtility.SetDirty(target);

                        targetScript.Editor_JustCreated = false;
                    }
                }

                //        targetScript.CheckForNullsToOptimize();

                if (targetScript.ToOptimize != null)
                {
                    bool generated = false;
                    for (int i = 0; i < targetScript.ToOptimize.Count; i++)
                    {
                        if (targetScript.ToOptimize[i].LODSet == null)
                        {
                            targetScript.ToOptimize[i].GenerateLODParameters();
                            generated = true;
                        }
                    }

                    if (generated)
                    {
                        Debug.LogWarning("[OPTIMIZERS EDITOR] LOD Settings generated from scratch for " + targetScript.name + ". Did you copy and paste objects through scenes? Unity is not able to remember LOD settings for not prefabed objects and to objects without shared settings between scenes like that :/");
                    }
                }
            }


            if (drawNothingToOptimizeWarning)

                preCol = GUI.color;
        }


        void OnDestroy()
        {
            if (Application.isEditor && !Application.isPlaying && (FOptimizer_Base)target == null)
            {
                if (isRunningInEditor)
                {
                    FOptimizer_Base targetScript = (FOptimizer_Base)target;
                    if (isPrefabInEditor) targetScript.CleanAsset();
                }
            }

            FOptimizer_Base opt = (FOptimizer_Base)target;
            if (opt) opt.Editor_InIsolatedScene = false;
        }

        public void LODFrame(FOptimizer_Base targetScript)
        {
            if (Application.isPlaying)
            {
                if (targetScript.IsCulled)
                    EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(FOptimizers_LODGUI.culledLODColor * new Color(1.5f, 1.5f, 1.5f, 0.325f)));
                else
                {
                    if (targetScript.TransitionPercent <= 0f)
                        EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(FOptimizers_LODGUI.lODColors[targetScript.CurrentLODLevel] * new Color(1f, 1f, 1f, 0.2f * (targetScript.OutOfCameraView ? 0.5f : 1f))));
                    else
                    {
                        Color c = Color.Lerp(FOptimizers_LODGUI.lODColors[targetScript.CurrentLODLevel], FOptimizers_LODGUI.lODColors[targetScript.TransitionNextLOD], targetScript.TransitionPercent);
                        c *= new Color(1f, 1f, 1f, 0.2f * (targetScript.OutOfCameraView ? 0.5f : 1f));
                        EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(c));
                    }
                }
            }

            serializedObject.Update();
        }


        protected void ScriptField(FOptimizer_Base targetScript)
        {
            GUI.enabled = false;
            UnityEditor.EditorGUILayout.ObjectField("Script", UnityEditor.MonoScript.FromMonoBehaviour(targetScript), typeof(FOptimizer_Base), false);
            GUI.enabled = true;
        }


        public override void OnInspectorGUI()
        {
            FOptimizer_Base targetScript = (FOptimizer_Base)target;


            Color prec = GUI.color;
            GUILayout.Space(3f);

            if (targetScript.gameObject.scene.rootCount != 0)
                EditorGUILayout.HelpBox("It's recommended to do conversion through prefab in browser window.\nNOT through isolated scene (prefab mode) and NOT through object placed on scene. (cleaning scriptable sub-assets efficiency)", MessageType.Error);


            if (targetScript is FOptimizer_Terrain == false)
                {
                    GUI.color = new Color(0.2f, 1f, 0.4f, 0.9f);

                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Convert To Essential Optimizer (Recommended)", GUILayout.Height(26)))
                    {
                        foreach (GameObject item in Selection.objects)
                        {
                            EssentialOptimizer newOpt = targetScript.gameObject.AddComponent<EssentialOptimizer>();
                            ConvertToV2(targetScript, newOpt);
                            //DestroyImmediate(targetScript);
                        }

                        return;
                    }

                    if (GUILayout.Button("To Essential + Destroy", GUILayout.Height(26)))
                    {
                        foreach (GameObject item in Selection.objects)
                        {
                            EssentialOptimizer newOpt = targetScript.gameObject.AddComponent<EssentialOptimizer>();
                            ConvertToV2(targetScript, newOpt);



                            UnityEngine.Object prefab = FOptimizers_LODTransport.GetPrefab(targetScript.gameObject);
                            if (prefab)
                            {
                                Debug.Log("[Optimizers Conversion] Algorithm will try to clear prefab file from scriptable files");

                                List<UnityEngine.Object> toRemove = FOptimizer_Cleaner.CheckForLeftovers(targetScript.gameObject, false);
                                int left = toRemove.Count;
                                string prefabPath = AssetDatabase.GetAssetPath(prefab);

                                left = FOptimizer_Cleaner.TryClear(prefabPath, toRemove);

                                if (left > 0)
                                    Debug.Log("[Optimizers Conversion] Removed " + left + " scriptables from prefab");
                            }


                            DestroyImmediate(targetScript, true);
                        }
                        return;
                    }
                    EditorGUILayout.EndHorizontal();
                }


            EditorGUILayout.BeginHorizontal();

            GUI.color = new Color(0.2f, 1f, 0.4f, 0.9f);
            if (GUILayout.Button("Convert To Scriptable Optimizer", GUILayout.Height(26)))
            {
                foreach (GameObject item in Selection.objects)
                {
                    ScriptableOptimizer newOpt;

                    if (targetScript is FOptimizer_Terrain == false)
                        newOpt = targetScript.gameObject.AddComponent<ScriptableOptimizer>();
                    else
                        newOpt = targetScript.gameObject.AddComponent<TerrainOptimizer>();

                    ConvertToV2(targetScript, newOpt);
                    //DestroyImmediate(targetScript);
                }

                return;
            }

            if (GUILayout.Button("To Scr + Destroy", GUILayout.Height(26)))
            {
                foreach (GameObject item in Selection.objects)
                {
                    ScriptableOptimizer newOpt;

                    if (targetScript is FOptimizer_Terrain == false)
                        newOpt = targetScript.gameObject.AddComponent<ScriptableOptimizer>();
                    else
                        newOpt = targetScript.gameObject.AddComponent<TerrainOptimizer>();

                    ConvertToV2(targetScript, newOpt);


                    UnityEngine.Object prefab = FOptimizers_LODTransport.GetPrefab(targetScript.gameObject);
                    if (prefab)
                    {
                        Debug.Log("[Optimizers Conversion] Algorithm will try to clear prefab file from scriptable files");

                        List<UnityEngine.Object> toRemove = FOptimizer_Cleaner.CheckForLeftovers(targetScript.gameObject, false);
                        int left = toRemove.Count;
                        string prefabPath = AssetDatabase.GetAssetPath(prefab);

                        left = FOptimizer_Cleaner.TryClear(prefabPath, toRemove);

                        if (left > 0)
                            Debug.Log("[Optimizers Conversion] Removed " + left + " scriptables from prefab");
                    }


                    DestroyImmediate(targetScript, true);
                }

                return;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3f);

            GUI.color = prec;
            EditorGUILayout.HelpBox("After converting you might have to adjust desired LOD settings again if you wasn't using just automatic settings!", MessageType.Info);
            GUILayout.Space(3f);


            LODFrame(targetScript);

#if UNITY_2020_1_OR_NEWER
            if (target == null) return;
            if (targetScript == null) return;
#endif

            ScriptField(targetScript);


            if (targetScript.OptimizingMethod == FEOptimizingMethod.TriggerBased)
                DrawAddRigidbodyToCamera();


            DefaultInspectorStack(targetScript);


            GUILayout.Space(2f);

            DrawToOptimizeStack(targetScript);


            serializedObject.ApplyModifiedProperties();


            targetScript.EditorUpdate();


            if (drawNothingToOptimizeWarning)
                if (targetScript.ToOptimize.Count == 0)
                {
                    string childrenInfo = "";
                    if (targetScript.gameObject.transform.childCount > 0) childrenInfo = " Maybe there are components to optimize in child game objects? Please check buttons inside 'To Optimize' tab.";
                    EditorGUILayout.HelpBox("Nothing to optimize! You can only cull game object with the component." + childrenInfo, MessageType.Info);
                }


            GUIStyle boldCenter = new GUIStyle(EditorStyles.boldLabel);
            boldCenter.alignment = TextAnchor.MiddleCenter;

            EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(new Color(0.4f, 0.7f, 0.2f, 0.15f)));

            if (Application.isPlaying) GUI.enabled = false;

            EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(new Color(0.95f, 0.95f, 0.95f, 0.15f)));

            if (targetScript.LimitLODLevels == 0 || targetScript.LimitLODLevels < 2 || targetScript.LimitLODLevels > 7)
            {
                EditorGUILayout.PropertyField(sp_LodLevels, true);
            }
            else
            {
                targetScript.LODLevels = EditorGUILayout.IntSlider(new GUIContent("LOD Levels", "Level of detail (LOD) steps to configure optimization levels"), targetScript.LODLevels, 1, targetScript.LimitLODLevels);
                serializedObject.ApplyModifiedProperties();
            }

            DrawFadeDurationSlider(targetScript);

            GUI.color = individualColor;

            EditorGUIUtility.labelWidth = 202;
            EditorGUILayout.PropertyField(sp_Shared);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndVertical();

            GUI.color = preCol;
            GUI.enabled = true;

            if (targetScript != null) // Unity 2020 is destroying reference somehow, what a nonsense
            {
                if (targetScript.enabled == false && Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Optimizer is disabled so I can't draw more here.", MessageType.Info);
                }
                else
                {
                    if (Application.isPlaying)
                    {
                        GUI.color = new Color(1f, 1f, 1f, 0.8f);
                        GUILayout.Space(2);

                        string lodNameTxt = "Active";
                        if (targetScript.IsCulled) lodNameTxt = "Culled";
                        else
                        {
                            if (targetScript.ToOptimize != null)
                                if (targetScript.ToOptimize.Count > 0)
                                {
                                    if (targetScript.CurrentLODLevel < targetScript.LODLevels)
                                        lodNameTxt = GetLODName(targetScript.CurrentLODLevel, targetScript.LODLevels);
                                    else
                                        lodNameTxt = "Hide";
                                }
                        }

                        string dist = "";

                        if (targetScript.TargetCamera != null)
                            if (targetScript.GetReferenceDistance() != 0f)
                            {
                                //distance = Vector3.Distance(Camera.main.transform.position, targetScript.GetReferencePosition());
                                distance = targetScript.GetReferenceDistance();
                                dist = "Distance: " + Math.Round(distance, 1) + " ";
                            }

                        string transition = "";
                        if (targetScript.TransitionPercent > 0f)
                        {
                            transition = " Transition: " + Mathf.Round(Mathf.Min(targetScript.TransitionPercent, 1f) * 100f) + "%" + (targetScript.TransitionPercent > 1.1f ? " (Add Delay " + (targetScript.TransitionPercent - 1f) + ")" : "");
                        }

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Current LOD: " + lodNameTxt + transition, new GUIStyle(EditorStyles.boldLabel));
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(dist, new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleRight });
                        GUILayout.EndHorizontal();

                        if (targetScript.OutOfCameraView && !targetScript.FarAway) GUILayout.Label("Camera Looking Away", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter });

                        GUILayout.Space(4);
                        GUI.color = preCol;
                    }

                    DrawLODSettingsStack(targetScript);

                    targetScript.Gizmos_SelectLOD(selectedLOD);
                }
            }

            EditorGUILayout.EndVertical();
            if (Application.isPlaying) EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            if (target != null) // Unity 2020 have problems with that
                if (Application.isPlaying) EditorUtility.SetDirty(target);

            if (serializedObject != null) // Unity 2020 prevention
                serializedObject.ApplyModifiedProperties();
        }


        protected virtual void DrawFadeDurationSlider(FOptimizer_Base targetScript)
        {
            GUI.color = individualColor;

            if (targetScript.FadeDuration <= 0f)
            {
                bool transitions = false;
                transitions = EditorGUILayout.Toggle(new GUIContent("Use Transitioning", "If you want changing LOD levels to be smooth changed in time \n\nLOD class of component needs to support transitioning, otherwise change will be done immediately anyway.\n\n(looking away/hiding will do it immediately anyway)"), transitions);
                if (transitions) targetScript.FadeDuration = 0.5f;
            }
            else
            {
                EditorGUILayout.PropertyField(sp_FadeDuration);
            }

            GUI.color = preCol;
        }


        protected virtual void DefaultInspectorStack(FOptimizer_Base targetScript, bool endVert = true)
        {

            if (Application.isPlaying) GUI.enabled = false;
            EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(new Color(0.975f, 0.975f, 0.975f, .325f)));

            EditorGUI.indentLevel++;
            DrawSetup = EditorGUILayout.Foldout(DrawSetup, "Optimizer Setup", true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
            EditorGUI.indentLevel--;

            if (DrawSetup)
            {
                List<string> excluded = new List<string> { "MaxDistance", "DetectionBounds", "m_Script", "LODLevels", "FadeDuration", "DeactivateObject", "DrawSharedSettingsOptions" };

                if (targetScript.OptimizingMethod == FEOptimizingMethod.Static || targetScript.OptimizingMethod == FEOptimizingMethod.Effective)
                {
                    // Culling groups related settings
                    if (!targetScript.CullIfNotSee) excluded.Add("DetectionRadius");
                }
                else // Repeating clocks related settings
                {
                    excluded.Add("DetectionRadius");
                    //excluded.Add("CullIfNotSee");
                }

                if (targetScript.CullIfNotSee) excluded.Add("Hideable");

                if (!drawDetectionOffset)
                    if (!excluded.Contains("DetectionOffset")) excluded.Add("DetectionOffset");

                if (!drawHideable)
                    if (!excluded.Contains("Hideable")) excluded.Add("Hideable");

                if (targetScript.OptimizingMethod == FEOptimizingMethod.Dynamic || targetScript.OptimizingMethod == FEOptimizingMethod.TriggerBased)
                    if (targetScript.CullIfNotSee)
                    {
                        if (excluded.Contains("DetectionBounds")) excluded.Remove("DetectionBounds");
                    }

                if (!drawCullIfNotSee)
                {
                    if (!drawDetectionRadius) if (!excluded.Contains("DetectionRadius")) excluded.Add("DetectionRadius");
                    if (!excluded.Contains("CullIfNotSee")) excluded.Add("CullIfNotSee");
                }

                FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 4);

                EditorGUILayout.BeginHorizontal();


                if (targetScript.AutoDistance) GUI.enabled = false;

                EditorGUILayout.PropertyField(sp_MaxDist);

                if (targetScript.AutoDistance) GUI.enabled = true;

                if (targetScript.DrawAutoDistanceToggle)
                {
                    //, camera fov and camera far clip planes
                    if (targetScript.AutoDistance) GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.925f);

                    if (GUILayout.Button(new GUIContent("Auto", "Automatic max distance basing on detection shape size, algorithm will try to set max distance to value where object would be so small on screen it would be almost not visible.\nIf you want to cull small objects ou can click auto, unclick and set even lower value."), new GUILayoutOption[2] { GUILayout.Width(42), GUILayout.Height(15) }))
                    {
                        targetScript.AutoDistance = !targetScript.AutoDistance;
                        targetScript.SetAutoDistance(1f);

                    }

                    GUI.color = preCol;

                    if (targetScript.AutoDistance) GUI.enabled = false;
                    if (GUILayout.Button(new GUIContent("Set Far", "Automatic max distance basing on detection shape size, algorithm will try to set max distance to value where object would be so small on screen it would be very small but still kinda visible."), new GUILayoutOption[2] { GUILayout.Width(55), GUILayout.Height(15) }))
                    {
                        targetScript.SetAutoDistance(0.7f);
                    }



                    if (targetScript.AutoDistance) GUI.enabled = true;

                }


                EditorGUILayout.EndHorizontal();

                FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 1, 5);

                GUILayout.Space(1f);
                DrawPropertiesExcluding(serializedObject, excluded.ToArray());
                GUILayout.Space(3f);
            }

            if (endVert) EditorGUILayout.EndVertical();
            if (Application.isPlaying) GUI.enabled = true;

        }


        protected virtual void OnSceneGUI()
        {
            if (Event.current.commandName == "Delete") Debug.Log(target.name + " is deleted");

            if (target == null) return; // Unity 2020 have problems with that

            FOptimizer_Base scr = (FOptimizer_Base)target;

            if (scr.CullIfNotSee)
                if (drawDetectionSphereHandle)
                {
                    Matrix4x4 m = scr.transform.localToWorldMatrix;
                    Matrix4x4 mw = scr.transform.worldToLocalMatrix;

                    Undo.RecordObject(scr, "Changing position of detection sphere");
                    EditorGUI.BeginChangeCheck();

                    Vector3 pos = m.MultiplyPoint(scr.DetectionOffset);

                    Vector3 scaled = FEditor_TransformHandles.ScaleHandle(Vector3.one * scr.DetectionRadius, pos, Quaternion.identity, .3f, true, true);
                    scr.DetectionRadius = scaled.x;

                    Vector3 transformed = FEditor_TransformHandles.PositionHandle(pos, Quaternion.identity, .3f, true, false);
                    if (Vector3.Distance(transformed, pos) > 0.00001f) scr.DetectionOffset = mw.MultiplyPoint(transformed);
                    EditorGUI.EndChangeCheck();
                }
        }


        protected virtual void DrawLODSettingsStack(FOptimizer_Base targetScript)
        {

            if (!ClickedOnSlider)
                EditorGUILayout.HelpBox("Click on L.O.D. distance state to view settings, drag the edges to change distance ranges", MessageType.None);

            if (targetScript.DrawGeneratedPrefabInfo)
            {
                if (targetScript.gameObject.scene.rootCount == 0)
                    EditorGUILayout.HelpBox("Creating prefab erased previous settings. Now settings will be stored inside prefab asset but consider using shared settings (Save LOD Set Files).", MessageType.Warning);
                else
                    EditorGUILayout.HelpBox("Optimizer lost LOD settings references. You probably break link to prefab or something, there are generated new reseted settings.", MessageType.Warning);

                EditorGUILayout.BeginHorizontal();

                if (targetScript.gameObject.scene.rootCount == 0)
                {
                    if (GUILayout.Button(new GUIContent("I prefer storing settings inside prefab asset", "LOD Levels parameters will be store inside prefab asset")))
                        targetScript.DrawGeneratedPrefabInfo = false;

                    if (GUILayout.Button(new GUIContent("Save LOD Sets (" + targetScript.ToOptimize.Count + ")", "Save LOD Levels Parameters file in project directory, you can share it with different prefabs"), new GUILayoutOption[2] { GUILayout.Width(135), GUILayout.Height(18) }))
                    {
                        targetScript.DrawSharedSettingsOptions = true;
                        targetScript.DrawGeneratedPrefabInfo = false;
                        SaveAllLODSets(targetScript);
                    }
                }
                else
                {
                    if (GUILayout.Button(new GUIContent("Ok I know, hide this message")))
                        targetScript.DrawGeneratedPrefabInfo = false;
                }

                EditorGUILayout.EndHorizontal();
            }

            #region LODs reset and preparation to draw

            if (selectedLOD > targetScript.LODLevels + 1) selectedLOD = targetScript.LODLevels;

            bool hiddenRange = false;
            if (drawHiddenRange) if (targetScript.CullIfNotSee || targetScript.Hideable) hiddenRange = true;

            Rect sliderRect = GUILayoutUtility.GetRect(0, 30 + (hiddenRange ? 14 : 0), GUILayout.ExpandWidth(true));
            Rect buttonsRect = sliderRect;
            if (hiddenRange) buttonsRect = new Rect(sliderRect.x, sliderRect.y, sliderRect.width, sliderRect.height - 14);

            List<FOptimizers_LODGUI.FOptimizers_LODInfo> infos = new List<FOptimizers_LODGUI.FOptimizers_LODInfo>();
            for (int i = 0; i < targetScript.LODLevels; i++)
            {
                string name = GetLODName(i, targetScript.LODLevels);

                if (i >= targetScript.LODPercent.Count) return;

                FOptimizers_LODGUI.FOptimizers_LODInfo info = new FOptimizers_LODGUI.FOptimizers_LODInfo(i, name, targetScript.LODPercent[i]);
                info.MinMax = targetScript.MinMaxDistance;
                info.ButtonRect = FOptimizers_LODGUI.CalcLODButton(buttonsRect, info.LODPercentage, hiddenRange);

                float previousPerc = 0f;
                if (i != 0) previousPerc = infos[i - 1].LODPercentage;
                float percentage = info.LODPercentage;

                info.RangeRect = FOptimizers_LODGUI.CalcLODRange(buttonsRect, previousPerc, info.LODPercentage);

                infos.Add(info);
            }

            #endregion

            DrawLODLevelsSlider(targetScript, sliderRect, infos, hiddenRange);

            if (selectedLOD >= 0 && selectedLOD <= targetScript.LODLevels + 1)
            {
                #region Drawing info about selected LOD Level

                Color frameCol = FOptimizers_LODGUI.culledLODColor;
                if (selectedLOD < targetScript.LODLevels) frameCol = FOptimizers_LODGUI.lODColors[selectedLOD];

                EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(frameCol * new Color(1f, 1f, 1f, 0.135f)));
                GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel);
                nameStyle.alignment = TextAnchor.MiddleLeft;

                string lodName = "Culled";
                if (selectedLOD != targetScript.LODLevels)
                    if (selectedLOD < targetScript.LODLevels + 1) lodName = infos[selectedLOD].LODName;
                    else
                        lodName = "Hide";

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(lodName, nameStyle, new GUILayoutOption[1] { GUILayout.Width(lodName.Length * 8) });

                float startAmount = targetScript.MinMaxDistance.x;
                float endAmount = targetScript.MinMaxDistance.y;

                float[] measure = targetScript.GetDistanceMeasures();

                if (selectedLOD > 0)
                {
                    if (selectedLOD != targetScript.LODLevels)
                        if (selectedLOD < targetScript.LODLevels + 1)
                        {
                            startAmount = measure[selectedLOD - 1];
                            endAmount = measure[selectedLOD];

                            //startAmount = Mathf.Lerp(targetScript.MinMaxDistance.x, targetScript.MinMaxDistance.y, infos[selectedLOD - 1].LODPercentage);
                            //endAmount = Mathf.Lerp(targetScript.MinMaxDistance.x, targetScript.MinMaxDistance.y, infos[selectedLOD].LODPercentage);
                        }
                }
                else
                {
                    if (targetScript.LODLevels > 1)
                    {
                        endAmount = measure[0];
                        //endAmount = Mathf.Lerp(targetScript.MinMaxDistance.x, targetScript.MinMaxDistance.y, infos[0].LODPercentage);
                    }
                }

                startAmount = (float)Math.Round(startAmount, 1);
                endAmount = (float)Math.Round(endAmount, 1);

                GUIStyle infoStyle = new GUIStyle(EditorStyles.label);
                infoStyle.alignment = TextAnchor.MiddleLeft;

                if (selectedLOD < targetScript.LODLevels)
                    EditorGUILayout.LabelField(new GUIContent("| Distance between " + startAmount + " - " + endAmount + " units", "Distance from object to main camera"), infoStyle);
                else
                    if (selectedLOD < targetScript.LODLevels + 1)
                    EditorGUILayout.LabelField(new GUIContent("| Distance above " + targetScript.MinMaxDistance.y + " units", "Distance from object to main camera"), infoStyle);
                else
                {
                    if (targetScript.Hideable == false)
                    {
                        if (targetScript.CullIfNotSee)
                        {
                            EditorGUILayout.LabelField(new GUIContent("| When camera looking away"), infoStyle);
                        }
                    }
                    else
                    {
                        if (targetScript.CullIfNotSee)
                        {
                            EditorGUILayout.LabelField(new GUIContent("| When camera looking away or used Optimizer.SetHidden()"), infoStyle);
                        }
                        else
                        {
                            EditorGUILayout.LabelField(new GUIContent("| When object used SetHidden() through code"), infoStyle);
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();

                FEditor.FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 4);

                #endregion

                if (selectedLOD == 0)
                    if (targetScript.DrawSharedSettingsOptions)
                    {
                        if (targetScript.ToOptimize.Count > 1)
                        {
                            int notShared = 0;
                            for (int i = 0; i < targetScript.ToOptimize.Count; i++)
                                if (!targetScript.ToOptimize[i].UsingShared) notShared++;

                            if (notShared > 1)
                                if (GUILayout.Button(new GUIContent("Save all LOD Sets to be Shared (" + targetScript.ToOptimize.Count + ")", "Generate new LOD set files basing on current settings in optimizer component."), EditorStyles.miniButton))
                                    SaveAllLODSets(targetScript);
                        }
                    }

                DrawLODOptionsStack(selectedLOD);

                EditorGUILayout.EndVertical();
            }

        }


        private string GetLODName(int i, int count)
        {
            string name = "LOD " + (i);
            if (i == 0) name = "Nearest";
            if (i == count) name = "Farthest";
            if (count <= 1) name = "Active";
            return name;
        }


        protected virtual void DrawToOptimizeStack(FOptimizer_Base targetScript)
        {
            EditorGUILayout.BeginVertical(FEditor.FGUI_Inspector.Style(new Color(0.75f, 0.75f, 0.15f, 0.2f)));

            EditorGUI.indentLevel++;
            if (targetScript.ToOptimize == null) targetScript.AssignComponentsToOptimizeFrom(targetScript);
            DrawToOptimize = EditorGUILayout.Foldout(DrawToOptimize, "To Optimize (" + targetScript.ToOptimize.Count + ")", true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
            EditorGUI.indentLevel--;

            if (DrawToOptimize)
            {
                FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 4);

                EditorGUILayout.HelpBox("Here you can manage components for optimization", MessageType.None);

                if (targetScript.IsPrefabed())
                    EditorGUILayout.HelpBox("Try to not remove/add components in 'To Optimize' list when you're in prefabed object! Do it in prefab file instead or just apply after editing.", MessageType.Warning);

                for (int i = targetScript.ToOptimize.Count - 1; i >= 0; i--)
                {
                    if (targetScript.ToOptimize[i].Component == null) targetScript.ToOptimize.RemoveAt(i);
                }

                if (targetScript.ToOptimize.Count == 1)
                {
                    if (targetScript.ToOptimize[0].Component is Renderer)
                    {
                        EditorGUILayout.HelpBox("Using optimizer on just one mesh renderer is not recommended, try using it on more complex objects.", MessageType.Warning);
                        GUI.color = new Color(1f, 0.9f, 0.5f);
                    }
                }


                for (int i = 0; i < targetScript.ToOptimize.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    GUI.enabled = false;
                    EditorGUIUtility.labelWidth = 35;
                    string objTitle = "[" + i + "]";

                    if (targetScript.ToOptimize[i].Component != null)
                        EditorGUILayout.ObjectField(objTitle, targetScript.ToOptimize[i].Component, typeof(Component), true);

                    GUI.enabled = true;

                    EditorGUIUtility.labelWidth = 0;

                    if (!Application.isPlaying)
                        if (GUILayout.Button(new GUIContent("X", "Remove component to be optimized from the list"), EditorStyles.toolbarButton, new GUILayoutOption[2] { GUILayout.Width(20), GUILayout.Height(12) }))
                        {
                            Undo.RecordObject(serializedObject.targetObject, "Removing component to optimize");
                            targetScript.RemoveFromToOptimizeAt(i);
                            return;
                        }

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(2f);
                }

                GUI.color = preCol;
                if (Application.isPlaying) GUI.enabled = false;

                FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.35f), 1, 6);

                GUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUI.indentLevel++;
                DrawAddCompOptions = EditorGUILayout.Foldout(DrawAddCompOptions, "Assigning new components tab", true);

                if (DrawAddCompOptions)
                {
                    FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.45f), 2, 4);

                    EditorGUILayout.BeginVertical(/*FGUI_Inspector.LGrayBackground*/);
                    if (GUILayout.Button(new GUIContent("Detect UNITY Components to optimize", "Checking UNITY components added to this game object and adding them to be optimized if they're supported by Optimizers"), EditorStyles.toolbarButton)) { Undo.RecordObject(serializedObject.targetObject, "Finding components to optimize"); targetScript.AssignComponentsToOptimizeFrom(targetScript); }
                    GUILayout.Space(3f);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent("Find Comps. in children", "Checking UNITY components added to this game object and all child game objects for components to be optimized"), EditorStyles.toolbarButton)) { Undo.RecordObject(serializedObject.targetObject, "Finding components to optimize in children"); targetScript.AssignComponentsToBeOptimizedFromAllChildren(targetScript.gameObject); }
                    if (GUILayout.Button(new GUIContent("Find Custom Comps. in children", "Checking components added to this game object and all child game objects for components to be optimized"), EditorStyles.toolbarButton)) { Undo.RecordObject(serializedObject.targetObject, "Finding components to optimize in children"); targetScript.AssignComponentsToBeOptimizedFromAllChildren(targetScript.gameObject, true); }
                    GUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    if (Application.isPlaying) GUI.enabled = true;
                    GUILayout.Space(1f);
                    FGUI_Inspector.DrawUILine(new Color(0.5f, 0.5f, 0.5f, 0.45f), 2, 4);
                    DrawDragAndDropSquare(targetScript);
                }

                EditorGUI.indentLevel--;
                GUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }


        protected virtual void DrawLODOptionsStack(int lodId)
        {
            FOptimizer_Base scr = target as FOptimizer_Base;
            if (scr.ToOptimize != null)
            {
                if (lodId == scr.LODLevels) if (scr.DrawDeactivateToggle)
                    {
                        GUI.color = GUI.color * new Color(1f, 1f, 1f, 0.7f);

                        if (!Application.isPlaying)
                            if (scr.transform.childCount > 0)
                            {
                                if (scr.DeactivateObject)
                                {
                                    EditorGUILayout.HelpBox("Whole GameObject deactivation sometimes can cause lags. Enter on red '!' on the right for tooltip", MessageType.Info);
                                }
                            }

                        GUI.color = individualColor;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("DeactivateObject"));

                        if (scr.DeactivateObject) GUI.color = Color.red; else GUI.color = Color.green;

                        GUIStyle whiteL = new GUIStyle(EditorStyles.whiteBoldLabel);
                        whiteL.normal.textColor = Color.white; // Unity 2019.3 makes "WhiteBoldLabel" black
                        EditorGUILayout.LabelField(new GUIContent("!", "Deactivating whole game object sometimes can be hard to compute for Unity.\nIf you experience lags when rotating camera, try to NOT disable whole game object, but only disable components."), whiteL, new GUILayoutOption[1] { GUILayout.Width(20) });

                        EditorGUILayout.EndHorizontal();
                        GUI.color = preCol;
                    }

                Undo.RecordObject(serializedObject.targetObject, "Changing LOD Settings");

                if (scr.CullIfNotSee || scr.Hideable)
                    if (lodId == scr.LODLevels + 1)
                    {
                        GUI.color = individualColor;
                        if (scr.HiddenCullAt == -1)
                        {
                            bool toggle = true;
                            toggle = EditorGUILayout.Toggle("Same as culling", toggle);
                            if (toggle == false) scr.HiddenCullAt = 0;
                        }
                        else
                        {
                            scr.HiddenCullAt = EditorGUILayout.IntSlider(new GUIContent("Cull from LOD", "From which LOD level, looking away or hiding object will apply culling LOD settings"), scr.HiddenCullAt + 1, 0, scr.LODLevels) - 1;
                        }

                        GUI.color = preCol;
                    }


                if (lodId == 0)
                {
                    if (!scr.UnlockFirstLOD)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUI.enabled = false;
                        EditorGUILayout.LabelField("First LOD - Default Settings - Nothing to change", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 9 });
                        GUI.enabled = true;
                        EditorGUIUtility.labelWidth = 4;
                        scr.UnlockFirstLOD = EditorGUILayout.Toggle(new GUIContent(" ", "Toggle to enable editing first LOD level (experimental) - can be helpful if you would need to make lower parameters when object is near to camera then change it to higher values when camera goes further."), scr.UnlockFirstLOD, new GUILayoutOption[2] { GUILayout.Width(20), GUILayout.Height(12) });
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("First LOD - Default Settings", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 9 });
                        EditorGUIUtility.labelWidth = 4;
                        scr.UnlockFirstLOD = EditorGUILayout.Toggle(new GUIContent(" ", "Toggle to enable editing first LOD level (experimental) - can be helpful if you would need to make lower parameters when object is near to camera then change it to higher values when camera goes further."), scr.UnlockFirstLOD, new GUILayoutOption[2] { GUILayout.Width(20), GUILayout.Height(12) });
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUIUtility.labelWidth = 0;
                }

                serializedObject.ApplyModifiedProperties();

                bool preEnabled = GUI.enabled;

                if (lodId == scr.LODLevels + 1)
                    if (scr.HiddenCullAt < 0)
                    {
                        lodId = scr.LODLevels;
                        GUI.enabled = false;
                    }

                EditorGUILayout.BeginHorizontal();

                //if (scr.ToOptimize.Count > 3)
                //{
                //    if (lodId > 0 || scr.UnlockFirstLOD)
                //    {
                //        if (GUILayout.Button("Set Enabled All")) for (int i = 0; i < scr.ToOptimize.Count; i++) { scr.ToOptimize[i].LODSet.LevelOfDetailSets[selectedLOD].Disable = false; }
                //        if (GUILayout.Button("Set Disabled All")) for (int i = 0; i < scr.ToOptimize.Count; i++) { scr.ToOptimize[i].LODSet.LevelOfDetailSets[selectedLOD].Disable = true; }
                //    }
                //}

                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < scr.ToOptimize.Count; i++)
                {
                    if (scr.ToOptimize[i] == null)
                    {
                        scr.ToOptimize.RemoveAt(i);
                        return;
                    }
                    else if (scr.ToOptimize[i].Component == null)
                    {
                        scr.ToOptimize.RemoveAt(i);
                        return;
                    }


                    scr.ToOptimize[i].Editor_DrawValues(lodId);
                }

                GUI.enabled = preEnabled;
            }
        }


        private void DrawLODLevelsSlider(FOptimizer_Base script, Rect lodRect, List<FOptimizers_LODGUI.FOptimizers_LODInfo> lods, bool hidden)
        {
            FOptimizer_Base targetScript = (FOptimizer_Base)target;

            int sliderId = GUIUtility.GetControlID(sliderControlId, FocusType.Passive);
            Event evt = Event.current;

            bool canInPlaymode = true;
            if (Application.isPlaying) canInPlaymode = targetScript.OptimizingMethod != FEOptimizingMethod.Static;

            switch (evt.GetTypeForControl(sliderId))
            {
                case EventType.Repaint:
                    {
                        FOptimizers_LODGUI.DrawLODSlider(targetScript, lodRect, lods, selectedLOD, distance, targetScript.MaxDistance, hidden, targetScript.HiddenCullAt); break;
                    }

                case EventType.MouseDown:
                    {
                        targetScript.OnValidate();

                        // Grow position on the x because edge buttons overflow by 5 pixels
                        Rect barRect = lodRect;
                        barRect.x -= 5;
                        barRect.width += 10;

                        if (barRect.Contains(evt.mousePosition))
                        {
                            evt.Use();
                            GUIUtility.hotControl = sliderId;

                            // Check for button click
                            bool clickedSliderButton = false;

                            // Re-sort the LOD array for these buttons to get the overlaps in the right order
                            var lodsLeft = lods.Where(lod => lod.LODPercentage > 0.5f).OrderByDescending(x => x.LODLevel);
                            var lodsRight = lods.Where(lod => lod.LODPercentage <= 0.5f).OrderBy(x => x.LODLevel);

                            var lodButtonOrder = new List<FOptimizers_LODGUI.FOptimizers_LODInfo>();
                            lodButtonOrder.AddRange(lodsLeft);
                            lodButtonOrder.AddRange(lodsRight);

                            foreach (FOptimizers_LODGUI.FOptimizers_LODInfo lod in lodButtonOrder)
                                if (lod.ButtonRect.Contains(evt.mousePosition))
                                {
                                    selectedLODSlider = lod.LODLevel;
                                    clickedSliderButton = true;
                                    ClickedOnSlider = true;
                                    break;
                                }

                            if (!clickedSliderButton)
                            {
                                // Check for culled selection
                                if (FOptimizers_LODGUI.GetCulledBox(lodRect, lods[lods.Count - 1].LODPercentage).Contains(evt.mousePosition))
                                {
                                    ClickedOnSlider = true;
                                    selectedLOD = script.LODLevels; break;
                                }

                                // Check for range click
                                foreach (FOptimizers_LODGUI.FOptimizers_LODInfo lod in lodButtonOrder)
                                    if (lod.RangeRect.Contains(evt.mousePosition))
                                    {
                                        ClickedOnSlider = true;
                                        selectedLOD = lod.LODLevel; break;
                                    }

                                if (hidden)
                                {
                                    Rect hiddenRect = new Rect(barRect.x, barRect.y + 30, barRect.width, 14); ;
                                    if (hiddenRect.Contains(evt.mousePosition))
                                    {
                                        ClickedOnSlider = true;
                                        selectedLOD = script.LODLevels + 1; break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Rect safeRect = lodRect;
                            safeRect.height += 1900;

                            if (!safeRect.Contains(evt.mousePosition))
                            {
                                selectedLOD = -1;
                                targetScript.Gizmos_SelectLOD(-1);
                            }

                            safeRect.height -= 1900;
                        }

                        serializedObject.ApplyModifiedProperties();
                        break;

                    }


                case EventType.MouseDrag:
                    {
                        if (Application.isPlaying) break;

                        Rect barRect = lodRect;
                        barRect.x -= 5;
                        barRect.width += 10;

                        if (barRect.Contains(evt.mousePosition)) // If mouse is on lod slider then we using drag event elseware it whould be used and not allow dragging properties sliders
                        {
                            if (selectedLODSlider < script.LODLevels - 1)
                            {
                                evt.Use();

                                if (canInPlaymode)
                                {
                                    float cameraPercent = FOptimizers_LODGUI.GetLODSliderPercent(evt.mousePosition, lodRect);
                                    FOptimizers_LODGUI.SetSelectedLODLevelPercentage(cameraPercent - 0.001f, selectedLODSlider, lods);
                                    if (selectedLODSlider > -1) script.LODPercent[selectedLODSlider] = lods[selectedLODSlider].LODPercentage;
                                    ClickedOnSlider = true;
                                    targetScript.Gizmos_IsResizingLOD(selectedLODSlider);
                                }
                                else
                                    Debug.Log("[OPTIMIZERS EDITOR] It's not allowed to change culling size in playmode!");
                            }
                        }

                        break;
                    }

                case EventType.MouseUp:
                    {

                        if (GUIUtility.hotControl == sliderId)
                        {
                            targetScript.Gizmos_StopChanging();

                            GUIUtility.hotControl = 0;
                            selectedLODSlider = -1;
                            evt.Use();
                        }

                        serializedObject.ApplyModifiedProperties();
                        break;
                    }

                case EventType.DragUpdated:
                case EventType.DragPerform:
                    {
                        int lodLevel = -2;

                        foreach (FOptimizers_LODGUI.FOptimizers_LODInfo lod in lods)
                            if (lod.RangeRect.Contains(evt.mousePosition))
                            {
                                lodLevel = lod.LODLevel;
                                break;
                            }

                        if (lodLevel == -2)
                        {
                            Rect culledRange = FOptimizers_LODGUI.GetCulledBox(lodRect, lods.Count > 0 ? lods[lods.Count - 1].LODPercentage : 1.0f);
                            if (culledRange.Contains(evt.mousePosition)) lodLevel = -1;
                        }

                        if (lodLevel >= -1)
                        {
                            selectedLOD = lodLevel;
                            evt.Use();
                        }

                        break;
                    }

                case EventType.DragExited: { evt.Use(); break; }
            }

        }


        private void DrawDragAndDropSquare(FOptimizer_Base optimizer)
        {
            Color c = GUI.color;

            Color preCol = GUI.color;
            GUI.color = new Color(0.5f, 1f, 0.5f, 0.9f);

            var drop = GUILayoutUtility.GetRect(0f, 45f, new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
            GUI.Box(drop, "Drag & Drop your GameObjects / Components here", new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter, fixedHeight = drop.height });
            var dropEvent = Event.current;
            GUILayout.Space(1);

            switch (dropEvent.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop.Contains(dropEvent.mousePosition)) break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (dropEvent.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (var dragged in DragAndDrop.objectReferences)
                        {
                            GameObject draggedObject = dragged as GameObject;
                            if (!draggedObject)
                            {
                                MonoBehaviour mono = dragged as MonoBehaviour;
                                if (mono)
                                {
                                    Undo.RecordObject(serializedObject.targetObject, "Adding MonoBehaviour to optimize");
                                    optimizer.AssignCustomComponentToOptimize(mono);
                                    EditorUtility.SetDirty(target);
                                }
                                else
                                {
                                    Component comp = dragged as Component;
                                    if (comp)
                                    {
                                        Undo.RecordObject(serializedObject.targetObject, "Adding Component to optimize");
                                        optimizer.AssignComponentsToOptimizeFrom(comp);
                                        EditorUtility.SetDirty(target);
                                    }
                                }
                            }
                            else
                            {
                                Undo.RecordObject(serializedObject.targetObject, "Adding MonoBehaviours to optimize");
                                MonoBehaviour[] comps = draggedObject.GetComponents<MonoBehaviour>();

                                int optims = 0;
                                for (int i = 0; i < comps.Length; i++)
                                {
                                    //Debug.Log("Trajing " + comps[i].GetType().ToString());
                                    optimizer.AssignCustomComponentToOptimize(comps[i]);
                                    if (comps[i] is FOptimizer_Base) optims++;
                                }

                                EditorUtility.SetDirty(target);

                                if (comps.Length == 0 || comps.Length == optims)
                                {
                                    Undo.RecordObject(serializedObject.targetObject, "Adding Component to optimize");
                                    optimizer.AssignComponentsToOptimizeFrom(draggedObject.transform);
                                    EditorUtility.SetDirty(target);
                                }
                            }
                        }

                    }

                    Event.current.Use();
                    break;
            }


            GUILayout.BeginVertical();
            if (ActiveEditorTracker.sharedTracker.isLocked) GUI.color = new Color(0.44f, 0.44f, 0.44f, 0.8f); else GUI.color = new Color(0.9f, 0.9f, 0.9f, 0.85f);
            GUILayout.Space(3);
            if (GUILayout.Button(new GUIContent("Lock Inspector for Drag & Drop", "Drag & drop components or game objects with components to the box"), EditorStyles.miniButton)) ActiveEditorTracker.sharedTracker.isLocked = !ActiveEditorTracker.sharedTracker.isLocked;
            GUI.color = c;
            GUILayout.EndVertical();


            GUI.color = preCol;
        }


        private void DrawAddRigidbodyToCamera()
        {
            Camera c = Camera.main;
            if (!c) c = GameObject.FindObjectOfType<Camera>();
            if (!c) return;

            Rigidbody rig = c.GetComponent<Rigidbody>();
            Collider col = c.GetComponent<Collider>();

            if (!rig & !!!col)
            {
                EditorGUILayout.HelpBox("If you are using Trigger Based method, your camera needs to have rigidbody and small collider to make it work correctly.", MessageType.Info);
                if (GUILayout.Button("Add Rigidbody And/Or Collider to Main Camera"))
                {
                    if (!rig)
                    {
                        rig = c.gameObject.AddComponent<Rigidbody>();
                        rig.isKinematic = true;
                        rig.useGravity = false;
                    }

                    if (!col)
                    {
                        SphereCollider sph = c.gameObject.AddComponent<SphereCollider>();
                        sph.radius = 0.1f;
                    }
                }
            }
        }


        protected void SaveAllLODSets(FOptimizer_Base optimizer)
        {
            for (int i = 0; i < optimizer.ToOptimize.Count; i++)
                optimizer.ToOptimize[i].SaveLODSet();

#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
#endif
        }
    }


    [CanEditMultipleObjects]
    [CustomEditor(typeof(FOptimizer))]
    public class FOptimizerEditor : FOptimizer_BaseEditor
    { }
}

