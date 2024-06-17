using System;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Threading.Tasks;
#endif

using Raymarcher.RendererData;
using Raymarcher.Convertor;
using Raymarcher.Attributes;
using Raymarcher.CameraFilters;

namespace Raymarcher
{
    /// <summary>
    /// Core class of the Raymarcher renderer.
    /// Contains all the required renderer dependencies, classes, fields and methods.
    /// Written by Matej Vanco, 2019-2024.
    /// </summary>
    [ExecuteAlways]
    public sealed class RMRenderMaster : MonoBehaviour
#if UNITY_EDITOR
        ,IDisposable
#endif
    {
        // Core dependencies

        [SerializeField] private RMCoreRenderMasterRenderingData renderMasterRenderingData = new RMCoreRenderMasterRenderingData();
        [SerializeField] private RMCoreRenderMasterLights renderMasterLights = new RMCoreRenderMasterLights();
        [SerializeField] private RMCoreRenderMasterMaterials renderMasterMaterials = new RMCoreRenderMasterMaterials();
        [SerializeField] private RMCoreRenderMasterMapping renderMappingMaster = new RMCoreRenderMasterMapping();

        // Serialized fields

        [SerializeField] private bool initialized = false;

        public enum TargetPipeline { BuiltIn, URP, HDRP };
        [SerializeField, RMAttributes.ReadOnly] private TargetPipeline targetPipeline;

        public enum TargetPlatform { PCConsole, Mobile, PCVR, MobileVR, WebGL };
        [SerializeField] private TargetPlatform targetPlatform;

        [SerializeField] private Camera initialCameraTarget;

        [SerializeField, RMAttributes.ReadOnly] private string registeredSessionName;

        [SerializeField] private bool recompilationRequiredSdfObjectBuffer;
        [SerializeField] private bool recompilationRequiredMaterialBuffer;

        // Properties

        /// <summary>
        /// Returns true whether the RM renderer is properly initialized
        /// </summary>
        public bool IsInitialized => initialized;

        /// <summary>
        /// Currently compiled target pipeline
        /// </summary>
        public TargetPipeline CompiledTargetPipeline => targetPipeline;
        /// <summary>
        /// Currently compiled target platform
        /// </summary>
        public TargetPlatform CompiledTargetPlatform => targetPlatform;

        /// <summary>
        /// Is it required to unpack certain data containers? (WebGL and Mobiles will return this always true)
        /// </summary>
        public bool UnpackDataContainer => CompiledTargetPlatform != TargetPlatform.PCConsole && CompiledTargetPlatform != TargetPlatform.PCVR;

        /// <summary>
        /// Currently registered session name
        /// </summary>
        public string RegisteredSessionName => registeredSessionName;

        /// <summary>
        /// Is recompilation of the SdfObjectBuffer required?
        /// </summary>
        public bool RecompilationRequiredSdfObjectBuffer => recompilationRequiredSdfObjectBuffer;
        /// <summary>
        /// Is recompilation of the MaterialBuffer required?
        /// </summary>
        public bool RecompilationRequiredMaterialBuffer => recompilationRequiredMaterialBuffer;

        /// <summary>
        /// Reference to the initial camera target (Might be null if the initial camera has been destroyed)
        /// </summary>
        public Camera InitialCameraTarget => initialCameraTarget;

        /// <summary>
        /// Object class of the object buffer
        /// </summary>
        public RMCoreRenderMasterMapping MappingMaster => renderMappingMaster;
        /// <summary>
        /// Object class of the Raymarcher's rendering data
        /// </summary>
        public RMCoreRenderMasterRenderingData RenderingData => renderMasterRenderingData;
        /// <summary>
        /// Object class of the registered scene lights
        /// </summary>
        public RMCoreRenderMasterLights MasterLights => renderMasterLights;
        /// <summary>
        /// Object class of the material buffer
        /// </summary>
        public RMCoreRenderMasterMaterials MasterMaterials => renderMasterMaterials;

        // Editor only

#if UNITY_EDITOR

        public bool showConvertorShortcut = true;
        public bool renderCustomEditorHierarchy = true;
        public bool autoRecompileIfNeeded = false;

        public Texture GetEditorIcon => Resources.Load<Texture2D>("RMEditorIcon_Comp_RMMaster");

        public void SetupRaymarcherInEditor(
            TargetPipeline targetPipeline,
            TargetPlatform targetPlatform,
            string sessionName,
            Camera initialCameraTarget)
        {
            this.targetPipeline = targetPipeline;
            this.targetPlatform = targetPlatform;

            this.initialCameraTarget = initialCameraTarget;

            registeredSessionName = sessionName;

            MappingMaster.SetupDependency(this);
            MasterLights.SetupDependency(this);
            MasterMaterials.SetupDependency(this);
            RenderingData.SetupDependency(this);

            RMCamFilterUtils.InitializeCameraFilter(this.initialCameraTarget, this);

            initialized = true;
            EditorUtility.SetDirty(this);

            if(showConvertorShortcut)
                RMRenderMasterEditorHelper.Enable(this);
        }

        public void SetOverrideSessionName(string newSessionName)
        {
            registeredSessionName = newSessionName;
        }

        public void SetPipelineInEditor(TargetPipeline targetPipeline)
        {
            this.targetPipeline = targetPipeline;
        }

        public void SetPlatformInEditor(TargetPlatform targetPlatform)
        {
            this.targetPlatform = targetPlatform;

            RecompileTarget(true);
            RecompileTarget(false);
        }

        public void SetRecompilationRequired(bool forSdfObjectBuffer)
        {
            if (forSdfObjectBuffer)
                recompilationRequiredSdfObjectBuffer = true;
            else
                recompilationRequiredMaterialBuffer = true;

            if (autoRecompileIfNeeded)
                RecompileTarget(forSdfObjectBuffer);
        }

        public void RecompileTarget(bool forSdfObjectBuffer)
        {
            if (forSdfObjectBuffer)
            {
                MappingMaster.RecompileSdfObjectBuffer();
                recompilationRequiredSdfObjectBuffer = false;
            }
            else
            {
                MasterMaterials.RecompileMaterialBuffer();
                recompilationRequiredMaterialBuffer = false;
            }
        }

        public void RecompileTarget()
        {
            if (RecompilationRequiredMaterialBuffer)
                RecompileTarget(false);
            if (RecompilationRequiredSdfObjectBuffer)
                RecompileTarget(true);
        }

        public static bool CanSwitchRenderType(TargetPlatform targetPlatform, RMCoreRenderMasterRenderingData.RenderTypeOptions inputRenderType)
        {
            if (targetPlatform != TargetPlatform.PCConsole && targetPlatform != TargetPlatform.PCVR)
                return inputRenderType != RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality;
            else
                return true;
        }

        public void LoadRenderMasterPreset(RMRenderMasterPresetObject presetObject)
        {
            var instance = Instantiate(presetObject);

            targetPlatform = instance.targetPlatform;

            Material prevSesMaterial = renderMasterRenderingData.RendererSessionMaterialSource;
            renderMasterRenderingData = instance.renderMasterRenderingData;
            renderMasterRenderingData.SetupDependency(this);
            renderMasterRenderingData.SetRendererMaterialSource(prevSesMaterial);

            renderMasterLights = instance.renderMasterLights;
            renderMasterLights.SetupDependency(this);

            RMConvertorCore.RefreshExistingRaymarcherInstance(this, RegisteredSessionName);
            MasterLights.SetAdditionalLightsCachedCount();

            renderMasterMaterials.ReleaseComputeBuffer();
            renderMasterMaterials = instance.renderMasterMaterials;
            renderMasterMaterials.SetupDependency(this);
            renderMasterMaterials.RegisterAndDispatchGlobalMaterials();
            renderMasterMaterials.SetMaterialDataBuffers(instance.renderMasterMaterials.MaterialDataBuffers);

            SetRecompilationRequired(true);
            SetRecompilationRequired(false);

            EditorUtility.SetDirty(this);
        }

        public void Dispose()
        {
            MappingMaster.DisposeDependency();
            MasterMaterials.DisposeDependency();
            MasterLights.DisposeDependency();
            RenderingData.DisposeDependency();

            RMCamFilterUtils.DisposeCameraFilter(initialCameraTarget, this);

            isBeingRemoved = true;
        }
#endif

#if UNITY_EDITOR
        private bool avoidDialog = false;

        private void Awake()
        {
            if (Application.isPlaying)
                return;

            if (FindObjectsOfType<RMRenderMaster>().Length > 1)
            {
                avoidDialog = true;
                EditorUtility.DisplayDialog("Warning!", "In the current version of the Raymarcher, only one Raymarcher Renderer is permitted in the scene!", "OK");
                DestroyImmediate(gameObject);
            }
        }
#endif

        private void OnEnable()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += ReleaseComputeBuffers;
            EditorApplication.quitting += EditorApplication_quitting;
#endif
            if (!IsInitialized)
                return;

#if UNITY_EDITOR
            if(showConvertorShortcut)
                RMRenderMasterEditorHelper.Enable(this);
#endif
            if (targetPipeline == TargetPipeline.URP)
                RMCamFilterUtils.InitializeCameraFilter(initialCameraTarget, this);
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= ReleaseComputeBuffers;
            EditorApplication.quitting -= EditorApplication_quitting;
#endif
            ReleaseComputeBuffers();

            if (!IsInitialized)
                return;
#if UNITY_EDITOR
            RMRenderMasterEditorHelper.Disable();
#endif
            if (targetPipeline == TargetPipeline.URP)
                RMCamFilterUtils.DisposeCameraFilter(initialCameraTarget, this);
        }

#if UNITY_EDITOR

        private bool isBeingRemoved = false;
        private bool isQuitting = false;
        private void EditorApplication_quitting()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            if (!IsInitialized)
                return;

            if (avoidDialog)
                return;

            if (!EditorApplication.isPlayingOrWillChangePlaymode && !isQuitting && !isBeingRemoved)
            {
                if (Time.frameCount != 0 && Time.renderedFrameCount != 0)
                {
                    switch (EditorUtility.DisplayDialogComplex("Raymarcher Renderer Object Removed",
                        "You have removed the Raymarcher Renderer component from the scene. Would you like to remove all the shader dependencies and sdf objects for this session? You can also undo the raymarcher renderer object removal if this was a mistake...",
                        "Remove This Raymarcher Session", "Undo", "Destroy Object Only"))
                    {
                        case 0:
                            if (EditorUtility.DisplayDialog("Warning", "Are you sure to remove the current Raymarcher session? There is no way back!", "Yes", "No"))
                            {
                                Dispose();
                                RMConvertorCore.RemoveExistingRaymarcherInstance(this, RegisteredSessionName);
                                EditorUtility.SetDirty(this);
                                AssetDatabase.Refresh();
                            }
                            else
                                goto UndoFun;
                            break;

                        case 1:
                            UndoFun:
                            var _ = UndoOp();
                            static async Task UndoOp()
                            {
                                await Task.Delay(50);
                                Undo.PerformUndo();
                                await Task.Delay(20);
                                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                                await Task.Delay(20);
                                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                                EditorApplication.QueuePlayerLoopUpdate();
                                await Task.Delay(20);
                                SceneView.RepaintAll();
                            }
                            break;
                    }
                }
            }
        }
#endif

        private void ReleaseComputeBuffers()
        {
            MappingMaster.ReleaseComputeBuffer();
            MasterMaterials.ReleaseComputeBuffer();
        }

        private void Update()
        {
            if (!IsInitialized)
                return;

            Material sessionMaterial = RenderingData.RendererSessionMaterialSource;

            if (!sessionMaterial)
                return;

            MappingMaster.UpdateDependency(sessionMaterial);
            RenderingData.UpdateDependency(sessionMaterial);
            MasterLights.UpdateDependency(sessionMaterial);
            MasterMaterials.UpdateDependency(sessionMaterial);
        }
    }
}