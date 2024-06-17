using UnityEngine;
using UnityEditor;

using Raymarcher.RendererData;
using Raymarcher.Constants;
using Raymarcher.Materials;
using Raymarcher.Convertor;

namespace Raymarcher.UEditor
{
    using static RMConstants.CommonRendererFeatures;

    [InitializeOnLoad]
    [CustomEditor(typeof(RMRenderMaster))]
    public sealed class RMRenderMasterEditor : RMEditorUtilities
    {
        private RMRenderMaster renderMaster;

        private const string RENDERER_DATA = "renderMasterRenderingData.";
        private const string RENDERER_LIGHTS = "renderMasterLights.";
        private const string RENDERER_MATERIALS = "renderMasterMaterials.";

        public Texture2D eIcon_Head;
        public Texture2D eIcon_RenderSettings;
        public Texture2D eIcon_RenderFeatures;
        public Texture2D eIcon_RenderLighting;
        public Texture2D eIcon_RenderMaterials;

        private static Texture eIcon_HierarchyRenderer;

        private bool lightingFoldout = true;
        private bool materialFoldout = true;
        private bool previewMaterials = false;

        private bool cloneSession;
        private string newSessionName;

        private RMRenderMaster.TargetPlatform targetPlatform;
        private RMCoreRenderMasterRenderingData.RenderTypeOptions renderTypeOptions;

        static RMRenderMasterEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += RMRenderMasterEditorHierarchy;
        }

        private static void RMRenderMasterEditorHierarchy(int instanceID, Rect selectionRect)
        {
            var item = EditorUtility.InstanceIDToObject(instanceID);
            if (item == null)
                return;
            var obj = item as GameObject;
            if (!obj)
                return;
            if (!obj.TryGetComponent(out RMRenderMaster rmMaster))
                return;
            if (!rmMaster.renderCustomEditorHierarchy)
                return;

            const float BRIGHT = 0.1f;
            bool isActive = obj.activeInHierarchy && obj.activeSelf;

            Rect offsetRect = new Rect(selectionRect.position + new Vector2(17,-1f), selectionRect.size);
            Color col = new Color(BRIGHT, BRIGHT, BRIGHT, isActive ? 0.8f : 0.4f);
            selectionRect.size += new Vector2(20, 0);
            EditorGUI.DrawRect(selectionRect, col);
            Rect iconRect = offsetRect;
            iconRect.position = new Vector2(iconRect.position.x - 17, iconRect.position.y+1);
            iconRect.size = new Vector2(16, 16);
            if (eIcon_HierarchyRenderer == null)
                eIcon_HierarchyRenderer = rmMaster.GetEditorIcon;

            Color cCache = GUI.color;
            GUI.color = isActive ? Color.white : Color.gray;

            if (eIcon_HierarchyRenderer != null)
                GUI.Label(iconRect, eIcon_HierarchyRenderer);
            EditorGUI.LabelField(offsetRect, item.name);
            GUI.color = cCache;
        }

        private void OnEnable()
        {
            renderMaster = (RMRenderMaster)target;
            targetPlatform = renderMaster.CompiledTargetPlatform;
            renderTypeOptions = renderMaster.RenderingData.CompiledRenderType;
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject == null)
                return;
            if (renderMaster == null)
                return;
            Material targetMaterial = renderMaster.RenderingData.RendererSessionMaterialSource;

            if(!renderMaster.IsInitialized)
            {
                RMhelpbox("Raymarcher renderer is not properly initialized. Please use the official procedure for setup!", MessageType.Error);
                if (RMb("Remove Component"))
                    DestroyImmediate(renderMaster);
                return;
            }

            RMs();
            RMimage(eIcon_Head);
            RMRenderMasterEditorHelper.GenerateContactLayout();
            RMhelpbox("Raymarcher version " + RMConstants.RM_VERSION + "; Last update on " + RMConstants.RM_LAST_UPDATE + ". Written by " + RMConstants.RM_DEV, MessageType.None);
            RMbv();
            RMproperty("registeredSessionName");
            RMproperty(RENDERER_DATA + "rendererSessionMaterialSource", "Registered Session Material");
            RMproperty("targetPipeline");

            if(targetMaterial == null)
            {
                RMhelpbox("Renderer Session Material Source is null! Please refresh the material source below.", MessageType.Error);
                if (RMb("Setup Session Material"))
                    RMInitWindow.CreateRaymarcherSessionMaterial(renderMaster, renderMaster.RegisteredSessionName, renderMaster.CompiledTargetPipeline);
                RMbve();
                return;
            }

            RMs(5);
            RMbh(false);
            GUI.backgroundColor = Color.red / 1.6f;
            if (RMb("Remove Raymarcher Session") && EditorUtility.DisplayDialog("Warning", "You are about to remove the current raymarcher session with all its dependencies. Are you sure? There is no way back!", "Yes", "No"))
                RMInitWindow.RemoveExistingRaymarcherInstance(renderMaster);
            GUI.backgroundColor = Color.blue / 1.6f;
            if (RMb("Clone Raymarcher Session"))
                cloneSession = !cloneSession;
            GUI.backgroundColor = Color.white;
            RMbhe();
            if(cloneSession)
            {
                GUI.backgroundColor = Color.blue / 1.6f;
                RMbv();
                RMl("New Session Name", true);
                newSessionName = GUILayout.TextField(newSessionName);
                GUI.backgroundColor = Color.blue / 1.4f;
                if (RMb("Clone Current Raymarcher Session", 250))
                {
                    RMInitWindow.CloneExistingRaymarcherInstance(renderMaster, newSessionName);
                    cloneSession = false;
                }
                RMbve();
                GUI.backgroundColor = Color.white;
            }

            RMs();
            RMl("Convertor Panel", true);
            RMbv();
            RMRenderMasterEditorHelper.GenerateRecompilationLayout(renderMaster, false, false);
            RMproperty(nameof(renderMaster.autoRecompileIfNeeded), "Auto-Recompile If Needed (Experimental)", "Raymarcher convertor will auto recompile shaders if needed. This feature is still in an experimental version!");
            RMproperty(nameof(renderMaster.showConvertorShortcut));
            RMproperty(nameof(renderMaster.renderCustomEditorHierarchy));

            if (renderMaster.showConvertorShortcut == false && RMRenderMasterEditorHelper.IsInitialized)
                RMRenderMasterEditorHelper.Disable();
            else if (renderMaster.showConvertorShortcut == true && !RMRenderMasterEditorHelper.IsInitialized)
                RMRenderMasterEditorHelper.Enable(renderMaster);

            if (!DrawPreset())
                return;

            RMbve();
            RMbve();

            RMs(20);

            DrawRendererData(targetMaterial);

            if (!DrawLights(targetMaterial))
                return;
            DrawMaterials(targetMaterial);

            RMs(64);

            serializedObject.ApplyModifiedProperties();
        }

        private bool loadPresetObj = false;
        private RMRenderMasterPresetObject presetObj;

        private bool DrawPreset()
        {
            GUI.color = Color.white * 0.8f;
            RMs();
            RMl("Render Master Preset (Experimental)", true);
            RMbh(false);
            if (RMb("Save Render Master As Preset"))
            {
                string path = EditorUtility.SaveFilePanelInProject("Save RM As Preset Object (Experimental)", "RMPreset", "asset", "");
                if(!string.IsNullOrEmpty(path))
                {
                    RMRenderMasterPresetObject obj = (RMRenderMasterPresetObject)CreateInstance(typeof(RMRenderMasterPresetObject));
                    obj.targetPlatform = renderMaster.CompiledTargetPlatform;
                    obj.renderMasterRenderingData = renderMaster.RenderingData;
                    obj.renderMasterMaterials = renderMaster.MasterMaterials;
                    obj.renderMasterLights = renderMaster.MasterLights;
                    obj = Instantiate(obj);
                    RMbhe();
                    AssetDatabase.CreateAsset(obj, path);
                    AssetDatabase.Refresh();
                    
                }
                return false;
            }
            if (RMb("Load Render Master Preset"))
                loadPresetObj = !loadPresetObj;
            RMbhe();
            if(loadPresetObj)
            {
                presetObj = (RMRenderMasterPresetObject)EditorGUILayout.ObjectField("Render Master Preset", presetObj, typeof(RMRenderMasterPresetObject), false);
                if (presetObj != null && RMb("Load Preset Object", 150) && RMDDialog("Are you sure?", "You are about to load a Render Master preset object. This feature is still in an experimental version and you might lose your progress. Are you sure?"))
                {
                    renderMaster.LoadRenderMasterPreset(presetObj);
                    loadPresetObj = false;
                    presetObj = null;
                    return false;
                }
            }    
            GUI.color = Color.white;
            return true;
        }

        private void DrawRendererData(Material m)
        {
            if (m == null)
                return;

            RMs();

            RMimage(eIcon_RenderSettings);
            RMbv();
            RMbv();
            targetPlatform = (RMRenderMaster.TargetPlatform)EditorGUILayout.EnumPopup("Target Platform", targetPlatform);
            if (targetPlatform != renderMaster.CompiledTargetPlatform)
            {
                RMhelpbox("Target platform has changed. Please recompile Raymarcher below", MessageType.None);
                RMbh();
                if(RMb("Recompile"))
                {
                    renderMaster.SetPlatformInEditor(targetPlatform);
                }
                RMbhe();
            }
            renderTypeOptions = (RMCoreRenderMasterRenderingData.RenderTypeOptions)EditorGUILayout.EnumPopup("Render Type", renderTypeOptions);
            if (renderTypeOptions != renderMaster.RenderingData.CompiledRenderType)
            {
                RMhelpbox("Render Type has changed. Please recompile Raymarcher below", MessageType.None);
                RMbh();
                if (RMb("Recompile"))
                {
                    renderMaster.RenderingData.SetRenderType(renderTypeOptions, true);
                }
                RMbhe();
            }
            if (!RMRenderMaster.CanSwitchRenderType(targetPlatform, renderTypeOptions))
            {
                EditorUtility.DisplayDialog("Warning", "Quality Render Type is not allowed for Mobile/ WebGL platforms! Please choose 'Standard' or 'Performant' (recommended).", "OK");
                renderTypeOptions = RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard;
            }
            RMproperty(RENDERER_DATA + "renderIterations");
            RMbve();

            switch (renderMaster.RenderingData.RenderIterations)
            {
                case RMCoreRenderMasterRenderingData.RenderIterationOptions.x16:
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx16, true);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx32, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx64, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx128, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx256, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx512, false);
                    break;
                case RMCoreRenderMasterRenderingData.RenderIterationOptions.x32:
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx16, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx32, true);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx64, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx128, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx256, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx512, false);
                    break;
                case RMCoreRenderMasterRenderingData.RenderIterationOptions.x64:
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx16, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx32, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx64, true);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx128, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx256, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx512, false);
                    break;
                case RMCoreRenderMasterRenderingData.RenderIterationOptions.x128:
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx16, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx32, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx64, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx128, true);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx256, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx512, false);
                    break;
                case RMCoreRenderMasterRenderingData.RenderIterationOptions.x256:
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx16, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx32, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx64, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx128, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx256, true);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx512, false);
                    break;
                case RMCoreRenderMasterRenderingData.RenderIterationOptions.x512:
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx16, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx32, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx64, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx128, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx256, false);
                    RMcheckKeyword(m, RAYMARCHER_ITERATIONSx512, true);
                    break;
            }

            RMs(5);

            RMbv();
            RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.maximumRenderDistance));
            RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.simpleRenderPrecisionSettings));
            if (renderMaster.RenderingData.simpleRenderPrecisionSettings)
                RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.renderPrecisionOption));
            else
                RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.renderPrecision));
            RMbve();
            RMbve();

            RMs();

            RMimage(eIcon_RenderFeatures);
            RMbv();
            RMbv();
            RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.rendererColorTint));
            RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.rendererExposure));
            if (renderMaster.RenderingData.CompiledRenderType != RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality)
            {
                RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.rendererGlobalHueSpectrumOffset));
                RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.rendererGlobalHueSaturation));
            }
            RMbve();

            RMs(5);

            RMbv();
            RMkeyWordProperty(m, RENDERER_DATA + "useSceneDepth",
                RAYMARCHER_SCENE_DEPTH, renderMaster.RenderingData.UseSceneDepth);
            if (renderMaster.RenderingData.UseSceneDepth)
            {
                RMlvlPlus();
                RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.sceneDepthSmoothness));
                RMlvlMinus();
            }
            RMbve();

            RMs(5);

            RMbv();
            bool usingSceneDepth = renderMaster.RenderingData.UseSceneDepth;
            if (!usingSceneDepth)
                GUI.enabled = false;
            RMkeyWordProperty(m, RENDERER_DATA + "reactWithSceneGeometry",
                RAYMARCHER_REACT_GEOMETRY, renderMaster.RenderingData.IsReactingWithSceneGeometry);
            if (renderMaster.RenderingData.IsReactingWithSceneGeometry)
            {
                RMlvlPlus();
                RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.sceneGeometrySmoothness));
                RMlvlMinus();
            }
            if (!usingSceneDepth)
                GUI.enabled = true;
            RMbve();

            RMs(5);

            RMbv();
            RMkeyWordProperty(m, RENDERER_DATA + "useSdfSmoothBlend",
                RAYMARCHER_SMOOTH_BLEND, renderMaster.RenderingData.UseSdfSmoothBlend, "Use Global SDF Smooth Blend");
            if (renderMaster.RenderingData.UseSdfSmoothBlend)
            {
                RMlvlPlus();
                RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.globalSdfObjectSmoothness));
                RMlvlMinus();
            }
            RMbve();

            RMs(5);

            RMbv();
            RMkeyWordProperty(m, RENDERER_DATA + "useDistanceFog",
                RAYMARCHER_DISTANCE_FOG, renderMaster.RenderingData.UseDistanceFog);
            if (renderMaster.RenderingData.UseDistanceFog)
            {
                RMlvlPlus();
                RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.distanceFogDistance));
                RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.distanceFogSmoothness));
                RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.distanceFogColor));
                RMlvlMinus();
            }
            RMbve();

            RMs(5);

            RMbv();
            RMkeyWordProperty(m, RENDERER_DATA + "usePixelation",
                RAYMARCHER_PIXELATION, renderMaster.RenderingData.UsePixelation, "Use Pixelation Filter");
            if (renderMaster.RenderingData.UsePixelation)
            {
                RMlvlPlus();
                RMproperty(RENDERER_DATA + nameof(renderMaster.RenderingData.pixelSize));
                RMlvlMinus();
            }
            RMbve();

            RMbve();
        }

        private bool DrawLights(Material m)
        {
            RMs();
            if (m == null)
                return false;

            RMimage(eIcon_RenderLighting);
            RMbv();
            lightingFoldout = EditorGUILayout.Foldout(lightingFoldout, "Lighting Settings", true, EditorStyles.foldout);
            if (!lightingFoldout)
            {
                RMbve();
                return true;
            }

            RMs();

            RMbv();
            RMkeyWordProperty(m, RENDERER_LIGHTS + "useMainDirectionalLight",
                RAYMARCHER_MAIN_LIGHT, renderMaster.MasterLights.UseMainDirectionalLight);
            if (renderMaster.MasterLights.UseMainDirectionalLight)
            {
                RMlvlPlus();
                RMproperty(RENDERER_LIGHTS + nameof(renderMaster.MasterLights.mainDirectionalLight));
                RMproperty(RENDERER_LIGHTS + nameof(renderMaster.MasterLights.mainDirectionalLightDamping));
                RMlvlMinus();
            }
            RMbve();

            RMs(5);

            RMbv();
            RMkeyWordProperty(m, RENDERER_LIGHTS + "useAdditionalLights",
                RAYMARCHER_ADDITIONAL_LIGHTS, renderMaster.MasterLights.UseAdditionalLights);
            if(renderMaster.MasterLights.UseAdditionalLights)
                RMproperty(RENDERER_LIGHTS + nameof(renderMaster.MasterLights.addLightsDamping));
            RMbve();

            if (renderMaster.MasterLights.UseAdditionalLights)
            {
                RMs(5);
                RMbh();
                RMl("Point Lights Collection", true);
                if (RMb("Add Light Element", 150))
                {
                    renderMaster.MasterLights.AdditionalLightsCollection.Add(new RMCoreRenderMasterLights.AdditionalLightData());
                    RMbhe();
                    RMbve();
                    return true;
                }
                if (renderMaster.MasterLights.AdditionalLightsCollection.Count > 0 && RMb("Remove Last Light", 160))
                {
                    if (EditorUtility.DisplayDialog("Warning", "You are about to remove the last light from the collection below. The light won't be removed from the scene. Are you sure to process this action?", "Yes", "No"))
                    {
                        renderMaster.MasterLights.AdditionalLightsCollection.RemoveAt(renderMaster.MasterLights.AdditionalLightsCollection.Count - 1);
                        RMbhe();
                        RMbve();
                        return true;
                    }
                }
                RMbhe();

                if (renderMaster.MasterLights.GetAdditionalLightsCompiledCount != renderMaster.MasterLights.AdditionalLightsCollection.Count)
                {
                    if (RMb("Recompile Light Database", 220, "The count of lights in the database has changed. It's required to recompile lights in the shader"))
                    {
                        RMConvertorCore.RefreshExistingRaymarcherInstance(renderMaster, renderMaster.RegisteredSessionName);
                        renderMaster.MasterLights.SetAdditionalLightsCachedCount();
                        AssetDatabase.Refresh();
                        return true;
                    }
                }
                if (renderMaster.MasterLights.AdditionalLightsCollection == null || renderMaster.MasterLights.AdditionalLightsCollection.Count == 0)
                {
                    RMhelpbox("There are no lights created yet", MessageType.None);
                    RMbve();
                    return true;
                }

                if (renderMaster.MasterLights.AdditionalLightsCollection.Count >= 5)
                    RMhelpbox("Adding more lights with shadows may significantly impact performance!");

                RMlvlPlus(1);
                for (int i = 0; i < renderMaster.MasterLights.AdditionalLightsCollection.Count; i++)
                {
                    SerializedProperty sp = serializedObject.FindProperty(RENDERER_LIGHTS + "additionalLightsCollection").GetArrayElementAtIndex(i);
                    RMbv();
                    RMproperty(sp, "Point Light " + (i + 1).ToString(), hasChildren:true);
                    if (!sp.isExpanded)
                    {
                        RMbve();
                        continue;
                    }
                    RMlvlPlus();
                    GUI.backgroundColor = Color.gray * 1.5f;
                    if (RMb("Remove Light", new RectOffset(32,0,0,0)) && EditorUtility.DisplayDialog("Warning", "You are about to remove the selected light from the Raymarcher. The light won't be removed from the scene. Are you sure to process this action?", "Yes", "No"))
                    {
                        renderMaster.MasterLights.AdditionalLightsCollection.RemoveAt(i);
                        RMbve();
                        RMbve();
                        return true;
                    }
                    GUI.backgroundColor = Color.white;
                    RMlvlMinus();
                    RMbve();
                }
                RMlvlMinus(1);
            }

            RMbve();
            return true;
        }

        private void DrawMaterials(Material m)
        {
            RMs();

            RMimage(eIcon_RenderMaterials);
            RMbv();
            materialFoldout = EditorGUILayout.Foldout(materialFoldout, "Material Settings", true, EditorStyles.foldout);
            if (!materialFoldout)
            {
                RMbve();
                return;
            }
            RMs();

            RMbv();
            previewMaterials = EditorGUILayout.Foldout(previewMaterials, "Preview Materials", true, EditorStyles.foldout);
            if (previewMaterials)
            {
                RMs();
                RMl("Total Material Types: " + renderMaster.MasterMaterials.MaterialDataBuffers.Count);
                RMs(5);
                RMlvlPlus();
                foreach(var buffer in renderMaster.MasterMaterials.MaterialDataBuffers)
                {
                    RMbv();
                    RMleditor(buffer.MaterialIdentifier.MaterialTypeName, true, 14);
                    RMleditor("Material Family: " + 
                        (string.IsNullOrEmpty(buffer.MaterialIdentifier.MaterialFamilyName) 
                        ? "No common material family" 
                        : buffer.MaterialIdentifier.MaterialFamilyName), false, 11);
                    RMleditor("Buffer Active: " + buffer.BufferIsInUse.ToString(), false, 11);
                    RMleditor("Objects Using: " + buffer.SceneObjectsAreUsingSomeInstances.ToString(), false, 11);
                    RMlvlPlus();
                    RMbv();
                    int instanceCounter = 0;
                    foreach (var instances in buffer.MaterialInstances)
                    {
                        if(instances == null)
                        {
                            RMhelpbox("Instance is null!", MessageType.None);
                            instanceCounter++;
                            continue;
                        }
                        if (RMb("[" + instanceCounter.ToString("00") + "] " + instances.name,
                            new RectOffset(50, (int)EditorGUIUtility.currentViewWidth - 400, 0, 0),
                            "",
                            TextAnchor.MiddleLeft))
                        {
                            Selection.activeObject = instances;
                            EditorGUIUtility.PingObject(Selection.activeObject);
                        }
                        instanceCounter++;
                    }
                    RMbve();
                    RMbve();
                    RMlvlMinus();
                }
                RMlvlMinus();
            }
            RMbve();

            RMs();
            RMline();
            RMs();

            RMl("Global Material Instances", true);
            RMbv();
            RMlvlPlus(1);
            RMproperty(RENDERER_MATERIALS + "globalMaterialInstances", hasChildren: true);
            RMlvlMinus(1);
            RMs(4);
            if (RMb("Register Global Materials", 200))
                renderMaster.MasterMaterials.RegisterAndDispatchGlobalMaterials();

            RMbve();

            RMs();
            RMline();
            RMs();

            RMl("Material Global Features & Settings", true);
            RMbv();
            if (!renderMaster.MasterMaterials.HasMaterialDataBuffers)
            {
                RMhelpbox("There are no materials used in the scene", MessageType.None);
                RMbve();
                RMbve();
                return;
            }

            RMs();

            int i = 0;
            foreach (var matType in renderMaster.MasterMaterials.MaterialDataBuffers)
            {
                var identifier = matType.MaterialIdentifier;
                if (!renderMaster.MasterMaterials.FilteredCommonMaterialTypes.Contains(identifier.MaterialTypeName))
                {
                    i++;
                    continue;
                }

                var materialBuffers = serializedObject.FindProperty(RENDERER_MATERIALS + "materialDataBuffers");
                if (materialBuffers.arraySize <= i)
                    return;

                SerializedProperty bufferElement = materialBuffers.GetArrayElementAtIndex(i);
                var globalData = identifier.MaterialDataContainerTypeGlobal;
                SerializedProperty globalDataElement = bufferElement.FindPropertyRelative(globalData.containerType);
                int costCount = 0;

                RMbh(false);
                if (identifier.MaterialEditorIcon != null)
                    RMimage(identifier.MaterialEditorIcon, 32, 32);
                RMl(string.IsNullOrEmpty(identifier.MaterialFamilyName) ? identifier.MaterialTypeName : identifier.MaterialFamilyName, true, 16, 4);
                RMbhe();
                RMl("Count of material instances: " + matType.MaterialInstances.Count);

                RMlvlPlus(1);
                RMbv();
                RMl("Material Global Features:", true);
                RMs(3);
                if (matType.MaterialGlobalKeywordsArray != null && matType.MaterialGlobalKeywordsArray.Length > 0)
                {
                    for (int x = 0; x < matType.MaterialGlobalKeywordsArray.Length; x++)
                    {
                        RMMaterialIdentifier.MaterialGlobalKeywords kwd = matType.MaterialGlobalKeywordsArray[x];
                        kwd.enabled = EditorGUILayout.Toggle(kwd.displayName, kwd.enabled);
                        RMcheckKeyword(m, kwd.keyword, kwd.enabled);
                        matType.MaterialGlobalKeywordsArray[x] = kwd;
                        if (kwd.enabled)
                            costCount += kwd.performanceCost;
                    }
                }
                else
                    RMhelpbox("This material type has no global features", MessageType.None);

                if (globalDataElement != null)
                {
                    RMs(5);
                    RMl("Material Global Settings:", true);
                    RMs(3);
                    RMproperty(globalDataElement, globalData.displayName, hasChildren: true);
                }

                RMs(5);
                int cost = identifier.ShaderKeywordFeaturesGlobalPerformanceCost;
                if (cost != 0)
                {
                    float quart = cost / 3f;
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), (float)costCount / cost, "Performance Cost: " +
                        (costCount <= quart ? "Low" :
                        costCount >= quart && costCount <= cost - quart ? "Good" :
                        costCount >= cost - quart ? "High" : ""));
                }
                RMlvlMinus(1);
                RMbve();
                RMs();
                i++;
            }
            RMbve();
            RMbve();
        }
    }
}