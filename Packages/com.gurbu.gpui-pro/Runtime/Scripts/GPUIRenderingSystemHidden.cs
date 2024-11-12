// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Events;
using UnityEngine.Profiling;
#if GPUI_URP
using UnityEngine.Rendering.Universal;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    /// <summary>
    /// This component is automatically attached to a hidden GameObject when GPUI rendering starts. 
    /// It is a singleton that persists between scenes. 
    /// It should not be added or removed manually.
    /// </summary>
    [ExecuteInEditMode]
    [DefaultExecutionOrder(1000)]
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#GPUI_Debugger_Window")]
    public sealed class GPUIRenderingSystem : MonoBehaviour, IGPUIDisposable
    {
        #region Runtime Properties

        public static GPUIRenderingSystem Instance { get; private set; }

        public static bool IsActive => Instance != null && Instance.IsInitialized;

        /// <summary>
        /// Set to true if the buffers are initialized
        /// </summary>
        public bool IsInitialized { get; private set; }
        /// <summary>
        /// <para>key => Original Material Instance ID (+ Extension Code Hash)</para>
        /// <para>value => Replacement material</para>
        /// </summary>
        public GPUIMaterialProvider MaterialProvider { get; private set; }
        /// <summary>
        /// Contains the list of runtime generated LODGroupData for each prefab GameObject
        /// <para>Key => Prototype key</para> 
        /// <para>Value => Generated LODGroupData</para> 
        /// </summary>
        public GPUILODGroupDataProvider LODGroupDataProvider { get; private set; }
        /// <summary>
        /// <para>Key => Source Group Key</para> 
        /// <para>Value => RenderSourceGroup</para> 
        /// </summary>
        public GPUIRenderSourceGroupProvider RenderSourceGroupProvider { get; private set; }
        /// <summary>
        /// <para>Key => Source Key</para> 
        /// <para>Value => GPUIRenderSource</para> 
        /// </summary>
        public GPUIRenderSourceProvider RenderSourceProvider { get; private set; }
        /// <summary>
        /// Contains data for each camera in use by GPUI
        /// </summary>
        public GPUICameraDataProvider CameraDataProvider { get; private set; }
        /// <summary>
        /// Contains references to Tree Proxy GameObjects for each camera in use by GPUI
        /// </summary>
        public GPUITreeProxyProvider TreeProxyProvider { get; private set; }

        /// <summary>
        /// Contains compute shader parameters for each GPUIProfile and GPUILODGroupData
        /// </summary>
        public GPUIDataBuffer<float> ParameterBuffer { get; private set; }
        /// <summary>
        /// Parameter buffer indexes for each object (e.g. GPUIProfile)
        /// </summary>
        public Dictionary<IGPUIParameterBufferData, int> ParameterBufferIndexes { get; private set; }

        public List<GPUIManager> ActiveGPUIManagers { get; private set; }

        /// <summary>
        /// Internal rendering parameters used by GPUI rendering functions.
        /// </summary>
        [NonSerialized]
        private RenderParams _renderParams;
        /// <summary>
        /// Defines world space bounds for the geometry. Used to cull and sort the rendered geometry.
        /// </summary>
        [NonSerialized]
        private Bounds _worldBounds;

        [NonSerialized]
        private List<IGPUIDisposable> _dependentDisposables;

#if UNITY_EDITOR
        public static PlayModeStateChange playModeStateChange = PlayModeStateChange.EnteredEditMode;
        internal static Dictionary<GPUIManager, GPUIManagerUIStoredValues> editor_managerUIStoredValues;
        public static Dictionary<GPUIProfile, GPUIProfile> editor_profileRollbackCache;
        public static UnityAction editor_UpdateMethod;
        public static List<Camera> editor_PlayModeFullRenderSceneViewCameras;

        public class GPUIManagerUIStoredValues
        {
            public List<int> selectedPrototypeIndexes;
            public bool isManagerSettingsFoldoutExpanded;
            public bool isRegisteredInstancesFoldoutExpanded;

            public GPUIManagerUIStoredValues()
            {
                selectedPrototypeIndexes = new();
                isManagerSettingsFoldoutExpanded = true;
                isRegisteredInstancesFoldoutExpanded = true;
            }
        }
#endif

        // Time management
        [NonSerialized]
        private int _lastDrawCallFrame;
        [NonSerialized]
        private float _lastDrawCallTime;
        public float TimeSinceLastDrawCall { get; private set; }
        public bool IsPaused { get; private set; }

        // Events
        public UnityEvent OnCommandBufferModified { get; private set; }
        public GPUICameraEvent OnPreCull { get; private set; }
        public GPUICameraEvent OnPreRender { get; private set; }
        public GPUICameraEvent OnPostRender { get; private set; }

        private static MaterialPropertyBlock _emptyMPB;
        public static MaterialPropertyBlock EmptyMPB
        {
            get
            {
                if (_emptyMPB == null)
                    _emptyMPB = new MaterialPropertyBlock();
                return _emptyMPB;
            }
        }

        #endregion Runtime Properties

        #region MonoBehaviour Methods

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                DestroyInstance();
                return;
            }
            else if (Instance == null)
            {
                Instance = this;
                Initialize();
            }
        }

        private void OnEnable()
        {
            if (Instance == null)
                Instance = this;
            if (CheckIsSingleton())
            {
                Initialize();
                UpdateCommandBuffers();
#if UNITY_EDITOR
                Editor_HandlePlayModeStates();
#endif
            }
        }

        private void OnDisable()
        {
            Dispose();
        }

        #endregion MonoBehaviour Methods

        #region Draw Calls

        // Camera events for Built-in Render Pipeline
        private static void CameraOnPreCull(Camera camera)
        {
#if UNITY_EDITOR
            if (playModeStateChange == PlayModeStateChange.ExitingEditMode || playModeStateChange == PlayModeStateChange.ExitingPlayMode) return;
#endif
            if (Instance.RenderSourceGroupProvider.Count == 0)
                return;
            ProcessCamera(camera);
        }
        private static void CameraOnPostRender(Camera camera)
        {
            if (Instance.CameraDataProvider.TryGetData(camera.GetInstanceID(), out GPUICameraData cameraData))
                cameraData.UpdateHiZTexture(default);
        }
        // Camera events for Scriptable Render Pipeline
        private static void CameraOnBeginRendering(ScriptableRenderContext context, Camera camera)
        {
            CameraOnPreCull(camera);
#if UNITY_6000_0_OR_NEWER && GPUI_URP
            if (Instance.CameraDataProvider.TryGetData(camera.GetInstanceID(), out GPUICameraData cameraData))
                cameraData.UpdateHiZTextureOnBeginRendering(camera, context);
#endif
        }
        private static void CameraOnEndRendering(ScriptableRenderContext context, Camera camera)
        {
            if (Instance.CameraDataProvider.TryGetData(camera.GetInstanceID(), out GPUICameraData cameraData))
                cameraData.UpdateHiZTexture(context);
        }
        private void OnEndContextRendering(ScriptableRenderContext context, List<Camera> list)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            int frameNo = Time.frameCount;
            foreach (GPUIRenderSourceGroup renderSourceGroup in RenderSourceGroupProvider.Values)
            {
                renderSourceGroup.UpdateTransformBufferData(frameNo);
            }
        }

        private static void ProcessCamera(Camera camera)
        {
            Instance.ParameterBuffer.UpdateBufferData();
            CameraType cameraType = camera.cameraType;
            GPUICameraDataProvider CameraDataProvider = Instance.CameraDataProvider;
#if UNITY_EDITOR // Camera rendering in Edit Mode
            if (!Application.isPlaying)
            {
                if (cameraType == CameraType.Preview && camera.name != "Preview Camera")
                    return;

                CameraDataProvider.ClearNullEditModeCameras();
                if (!CameraDataProvider.TryGetEditModeCameraData(camera.GetInstanceID(), out GPUICameraData editModeCameraData))
                {
                    editModeCameraData = new GPUICameraData(camera);
                    editModeCameraData.renderToSceneView = false;
                    CameraDataProvider.AddEditModeCameraData(editModeCameraData);
                }

                Instance.OnPreCull?.Invoke(editModeCameraData);
                editModeCameraData.UpdateCameraData();
                ProcessCameraData(camera, editModeCameraData, true);
                return;
            }
#endif
            if (CameraDataProvider.Count == 0)
                CameraDataProvider.RegisterDefaultCamera();

            bool hasCameraData = CameraDataProvider.TryGetData(camera.GetInstanceID(), out GPUICameraData cameraData);
            if (!hasCameraData && cameraType == CameraType.Reflection)
            {
                cameraData = new GPUICameraData(camera);
#if UNITY_EDITOR
                cameraData.renderToSceneView = false;
#endif
                CameraDataProvider.AddCameraData(cameraData);
                hasCameraData = true;
                Instance.UpdateCommandBuffers(cameraData);
            }

            if (hasCameraData)
            {
                int frameCount = Time.frameCount;
#if UNITY_EDITOR
                if (!Instance.IsPaused)
#endif
                    if (cameraData._lastUpdateFrame == frameCount) // To not make rendering calls and visibility calculations multiple times for the same frame (e.g. XR MultiPass)
                        return;

                if (cameraData._lastUpdateFrame != frameCount) // Do not invoke camera events in paused mode, check frame equality in case Step button is used
                {
                    Instance.OnPreCull?.Invoke(cameraData);
                    cameraData.UpdateCameraData();
                    cameraData._lastUpdateFrame = frameCount;
                }
                ProcessCameraData(camera, cameraData, true);
                return;
            }

#if UNITY_EDITOR // Scene view camera rendering in Play Mode
            if (cameraType == CameraType.SceneView)
            {
                CameraDataProvider.ClearNullEditModeCameras();
                if (CameraDataProvider.TryGetEditModeCameraData(camera.GetInstanceID(), out GPUICameraData editModeCameraData))
                {
                    Instance.OnPreCull?.Invoke(editModeCameraData);
                    editModeCameraData.UpdateCameraData();
                    ProcessCameraData(camera, editModeCameraData, true);
                }
                else
                {
                    foreach (var cd in CameraDataProvider.Values)
                    {
                        if (cd.renderToSceneView)
                        {
                            if (!Instance.IsPaused)
                                if (cd._instanceCountMultiplier > 1) // Instance counts might be multiplied for stereo rendering, we set it back it original value for Scene View rendering
                                    cd.SetCommandBufferInstanceCounts(1);
                            ProcessCameraData(camera, cd, false);
                        }
                    }
                }
            }
#endif
        }

        private static void ProcessCameraData(Camera camera, GPUICameraData cameraData, bool invokeEvents)
        {
            if (cameraData._commandBuffer.Buffer == null) return;

            Profiler.BeginSample(cameraData.name);
            if (invokeEvents)
                Instance.OnPreRender?.Invoke(cameraData);
            Instance._renderParams.camera = camera;
            Instance.MakeDrawCalls(cameraData);
            if (invokeEvents)
                Instance.OnPostRender?.Invoke(cameraData);
            Profiler.EndSample();
        }

        private void MakeDrawCalls(GPUICameraData cameraData)
        {
            if (cameraData.ActiveCamera == null)
                return;
            Profiler.BeginSample("GPUIRenderingSystem.MakeDrawCalls");
            if (_lastDrawCallFrame != Time.frameCount)
            {
                _lastDrawCallFrame = Time.frameCount;
                TimeSinceLastDrawCall = Time.realtimeSinceStartup - _lastDrawCallTime;
                _lastDrawCallTime = Time.realtimeSinceStartup;
            }

            _worldBounds.center = cameraData.GetCameraPosition();
            _worldBounds.size = GPUIRuntimeSettings.Instance.instancingBoundsSize;
            _renderParams.worldBounds = _worldBounds;
            int cullingMask = cameraData.ActiveCamera.cullingMask;
#if UNITY_EDITOR
            uint shadowCascades = (uint)QualitySettings.shadowCascades;
#endif
            int qualityMaximumLODLevel = QualitySettings.maximumLODLevel;
            foreach (GPUIRenderSourceGroup renderSourceGroup in RenderSourceGroupProvider.Values)
            {
                GPUILODGroupData lodGroupData = renderSourceGroup.LODGroupData;
                if (renderSourceGroup.BufferSize > 0 && renderSourceGroup.InstanceCount > 0
                    && lodGroupData != null
                    && cameraData.TryGetVisibilityBufferIndex(renderSourceGroup, out int visibilityBufferIndex))
                {
                    Profiler.BeginSample(renderSourceGroup.Name);
                    MaterialPropertyBlock mpb = renderSourceGroup.GetMaterialPropertyBlock(lodGroupData, cameraData);
                    renderSourceGroup.ApplyMaterialPropertyOverrides(mpb, -1, -1);
                    renderSourceGroup.TransformBufferData.SetMPBBuffers(mpb, cameraData);

                    _renderParams.matProps = mpb;
                    int commandIndex = (int)cameraData._visibilityBuffer[visibilityBufferIndex].commandStartIndex;

                    int lodCount = lodGroupData.Length;
                    int maximumLODLevel = GetMaximumLODLevel(lodCount, renderSourceGroup.Profile.maximumLODLevel, qualityMaximumLODLevel);
#if UNITY_EDITOR
                    GPUIRenderStatistics[] lodRenderStatistics = renderSourceGroup.GetRenderStatisticsArray(lodCount);
#endif
                    _renderParams.shadowCastingMode = ShadowCastingMode.Off;
                    for (int l = 0; l < lodCount; l++)
                    {
                        mpb.SetInt(GPUIConstants.PROP_instanceDataBufferShift, renderSourceGroup.BufferSize * l);
                        renderSourceGroup.ApplyMaterialPropertyOverrides(mpb, l, -1);
                        GPUILODData gpuiLOD = lodGroupData[l];
                        for (int r = 0; r < gpuiLOD.Length; r++)
                        {
                            GPUIRendererData renderer = gpuiLOD[r];
                            Mesh mesh = renderer.GetMesh();
                            if (mesh != null && GPUIUtility.IsInLayer(cullingMask, renderer.layer) && !renderer.IsShadowsOnly && l >= maximumLODLevel)
                            {
                                _renderParams.receiveShadows = renderer.receiveShadows;
                                renderSourceGroup.ApplyMaterialPropertyOverrides(mpb, l, r);
                                mpb.SetMatrix(GPUIConstants.PROP_gpuiTransformOffset, renderer.transformOffset);
                                _renderParams.layer = renderer.layer;
                                if (renderer.motionVectorGenerationMode == MotionVectorGenerationMode.Object && !renderSourceGroup.TransformBufferData.HasPreviousFrameTransformBuffer)
                                    _renderParams.motionVectorMode = MotionVectorGenerationMode.Camera;
                                else
                                    _renderParams.motionVectorMode = renderer.motionVectorGenerationMode;
                                _renderParams.renderingLayerMask = renderer.renderingLayerMask;

                                if (Application.isPlaying && renderer.replacementMaterials == null)
                                    renderer.InitializeReplacementMaterials(MaterialProvider);
                                for (int m = 0; m < renderer.rendererMaterials.Length; m++)
                                {
                                    _renderParams.material = GetReplacementMaterial(renderer, m, renderSourceGroup.ShaderKeywords);

                                    GPUIUtility.RenderMeshIndirect(_renderParams, mesh, cameraData._commandBuffer, 1, commandIndex);
#if UNITY_EDITOR
                                    lodRenderStatistics[l].drawCount++;
#endif
                                    commandIndex++;
                                }
#if UNITY_EDITOR
                                lodRenderStatistics[l].vertexCount += (uint)mesh.vertexCount;
#endif
                            }
                            else
                                commandIndex += renderer.rendererMaterials.Length;
                        }
                    }

                    if (renderSourceGroup.Profile.isShadowCasting)
                    {
                        _renderParams.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                        for (int l = 0; l < lodCount; l++)
                        {
                            mpb.SetInt(GPUIConstants.PROP_instanceDataBufferShift, renderSourceGroup.BufferSize * (l + lodCount));
                            GPUILODData gpuiLOD = lodGroupData[l];
                            for (int r = 0; r < gpuiLOD.Length; r++)
                            {
                                GPUIRendererData renderer = gpuiLOD[r];
                                Mesh mesh = renderer.GetMesh();
                                if (mesh != null && GPUIUtility.IsInLayer(cullingMask, renderer.layer) && renderer.IsShadowCasting && l >= maximumLODLevel)
                                {
                                    _renderParams.receiveShadows = renderer.receiveShadows;
                                    mpb.SetMatrix(GPUIConstants.PROP_gpuiTransformOffset, renderer.transformOffset);
                                    _renderParams.layer = renderer.layer;

                                    if (Application.isPlaying && renderer.replacementMaterials == null)
                                        renderer.InitializeReplacementMaterials(MaterialProvider);
                                    for (int m = 0; m < renderer.rendererMaterials.Length; m++)
                                    {
                                        _renderParams.material = GetReplacementMaterial(renderer, m, renderSourceGroup.ShaderKeywords);

                                        GPUIUtility.RenderMeshIndirect(_renderParams, mesh, cameraData._commandBuffer, 1, commandIndex);
#if UNITY_EDITOR
                                        lodRenderStatistics[l].shadowDrawCount += shadowCascades;
#endif
                                        commandIndex++;
                                    }
#if UNITY_EDITOR
                                    lodRenderStatistics[l].shadowVertexCount += (uint)mesh.vertexCount * shadowCascades;
#endif
                                }
                                else
                                    commandIndex += renderer.rendererMaterials.Length;
                            }
                        }
                    }
                    Profiler.EndSample();
                }
            }

            Profiler.EndSample();
        }

        private int GetMaximumLODLevel(int lodCount, int profileMaximumLODLevel, int qualityMaximumLODLevel)
        {
            if (lodCount <= 1) return 0;
            return Mathf.Max(profileMaximumLODLevel, qualityMaximumLODLevel);
        }

        private Material GetReplacementMaterial(GPUIRendererData renderer, int materialIndex, List<string> keywords)
        {
            Material replacementMat = null;
            if (Application.isPlaying)
                replacementMat = renderer.replacementMaterials[materialIndex];
            if (replacementMat == null)
            {
                if (MaterialProvider.TryGetReplacementMaterial(renderer.rendererMaterials[materialIndex], keywords, renderer.isSkinnedMesh ? GPUIConstants.EXTENSION_CODE_CROWD : null, out replacementMat))
                {
                    if (Application.isPlaying)
                    {
                        renderer.replacementMaterials[materialIndex] = replacementMat;
#if UNITY_EDITOR
                        MaterialProvider.AddMaterialVariant(replacementMat);
#endif
                    }
                }
            }
            return replacementMat;
        }

#endregion Draw Calls

        #region Initialize / Dispose

        private bool CheckIsSingleton()
        {
            if (Instance == null)
            {
                DestroyInstance();
                return false;
            }
            else if (Instance != this)
            {
                DestroyInstance();
                return false;
            }
            return true;
        }

        private void Initialize()
        {
            if (!GPUIRuntimeSettings.Instance.IsSupportedPlatform())
            {
                DestroyInstance();
                return;
            }
            if (!IsInitialized)
            {
                IsInitialized = true;
                GPUIRuntimeSettings.Instance.DetermineOperationMode();

                MaterialProvider = new();
                MaterialProvider.Initialize();

                LODGroupDataProvider = new();
                LODGroupDataProvider.Initialize();

                RenderSourceGroupProvider = new();
                RenderSourceGroupProvider.Initialize();

                RenderSourceProvider = new();
                RenderSourceProvider.Initialize();

                CameraDataProvider = new();
                CameraDataProvider.Initialize();

                TreeProxyProvider = new();
                TreeProxyProvider.Initialize();

                ParameterBuffer = new("Parameter");
                ParameterBufferIndexes = new();

                ActiveGPUIManagers = new();

                _renderParams = new(GPUIShaderBindings.Instance.ErrorMaterial);
                _worldBounds = new Bounds(Vector3.zero, GPUIRuntimeSettings.Instance.instancingBoundsSize);

                _dependentDisposables = new List<IGPUIDisposable>();

                OnCommandBufferModified = new UnityEvent();
                OnPreCull = new GPUICameraEvent();
                OnPreRender = new GPUICameraEvent();
                OnPostRender = new GPUICameraEvent();

                //SetGlobalWindVector();

                Camera.onPreCull -= CameraOnPreCull;
                RenderPipelineManager.beginCameraRendering -= CameraOnBeginRendering;
                if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
                    Camera.onPreCull += CameraOnPreCull;
                else
                    RenderPipelineManager.beginCameraRendering += CameraOnBeginRendering;

                Camera.onPostRender -= CameraOnPostRender;
                RenderPipelineManager.endCameraRendering -= CameraOnEndRendering;
                if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
                    Camera.onPostRender += CameraOnPostRender;
                else
                {
                    RenderPipelineManager.endCameraRendering += CameraOnEndRendering;
                    if (GPUIRuntimeSettings.Instance.IsHDRP)
                        RenderPipelineManager.endContextRendering += OnEndContextRendering;
                }
            }
        }

        public static bool SetGlobalWindVector()
        {
            WindZone[] sceneWindZones = FindObjectsByType<WindZone>(FindObjectsSortMode.None);
            for (int i = 0; i < sceneWindZones.Length; i++)
            {
                if (sceneWindZones[i].mode == WindZoneMode.Directional)
                {
                    Shader.SetGlobalVector("_Wind", new Vector4(sceneWindZones[i].windTurbulence, sceneWindZones[i].windPulseMagnitude, sceneWindZones[i].windPulseFrequency, sceneWindZones[i].windMain));
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            IsInitialized = false;

            if (MaterialProvider != null)
            {
                MaterialProvider.Dispose();
                MaterialProvider = null;
            }

            if (LODGroupDataProvider != null)
            {
                LODGroupDataProvider.Dispose();
                LODGroupDataProvider = null;
            }

            if (RenderSourceGroupProvider != null)
            {
                RenderSourceGroupProvider.Dispose();
                RenderSourceGroupProvider = null;
            }

            if (RenderSourceProvider != null)
            {
                RenderSourceProvider.Dispose();
                RenderSourceProvider = null;
            }

            if (CameraDataProvider != null)
            {
                CameraDataProvider.Dispose();
                CameraDataProvider = null;
            }

            if (TreeProxyProvider != null)
            {
                TreeProxyProvider.Dispose();
                TreeProxyProvider = null;
            }

            if (ParameterBuffer != null)
            {
                ParameterBuffer.Dispose();
                ParameterBuffer = null;
            }

            if (_dependentDisposables != null)
            {
                foreach (IGPUIDisposable disposable in _dependentDisposables)
                    disposable.Dispose();
                _dependentDisposables = null;
            }

            ParameterBufferIndexes = null;

            ActiveGPUIManagers = null;

            if (!IsActive) // Check if disposing a duplicate or the original
            {
                Camera.onPreCull -= CameraOnPreCull;
                RenderPipelineManager.beginCameraRendering -= CameraOnBeginRendering;
                Camera.onPostRender -= CameraOnPostRender;
                RenderPipelineManager.endCameraRendering -= CameraOnEndRendering;
                RenderPipelineManager.endContextRendering -= OnEndContextRendering;
            }
        }

        public void ReleaseBuffers()
        {
            if (CameraDataProvider != null)
                CameraDataProvider.ReleaseBuffers();
            if (ParameterBuffer != null)
                ParameterBuffer.ReleaseBuffers();
        }

        private void DestroyInstance()
        {
            gameObject.DestroyGeneric();
        }

#if GPUIPRO_DEVMODE && UNITY_EDITOR
        [MenuItem("Tools/GPU Instancer Pro/Development/Reset Rendering System", validate = false, priority = 9999)]
#endif
        public static void ResetRenderingSystem()
        {
            if (Instance != null)
                Instance.DestroyInstance();
            InitializeRenderingSystem();
        }


#if GPUIPRO_DEVMODE && UNITY_EDITOR
        [MenuItem("Tools/GPU Instancer Pro/Development/Regenerate Renderers", validate = false, priority = 9999)]
#endif
        public static void RegenerateRenderers()
        {
            if (Instance != null)
            {
                Instance.LODGroupDataProvider.RegenerateLODGroups();
                Instance.UpdateCommandBuffers(true);
                Instance.UpdateParameterBufferData();
                Instance.MaterialProvider.Reset();
            }
        }

        public static void InitializeRenderingSystem()
        {
            if (IsActive) return;
            if (Instance == null)
            {
                GameObject go = new GameObject();
                Instance = go.AddComponent<GPUIRenderingSystem>();
                if (Instance == null)
                    return;
                go.name = "===GPUI Rendering System [" + Instance.GetInstanceID() + "]===";
#if GPUIPRO_DEVMODE
                go.hideFlags = HideFlags.DontSave;
#else
                go.hideFlags = HideFlags.HideAndDontSave;
#endif
            }
            Instance.Initialize();
        }

        public static void AddActiveManager(GPUIManager manager)
        {
            InitializeRenderingSystem();
            if (!Instance.ActiveGPUIManagers.Contains(manager))
                Instance.ActiveGPUIManagers.Add(manager);
        }

        public static void RemoveActiveManager(GPUIManager manager)
        {
            if (Instance != null && Instance.IsInitialized)
                Instance.ActiveGPUIManagers.Remove(manager);
        }

        #endregion Initialize / Dispose

        #region RenderSource

        public static bool RegisterRenderer(UnityEngine.Object source, GameObject prefab, out int rendererKey, int groupID = 0, GPUITransformBufferType transformBufferType = GPUITransformBufferType.Default, List<string> shaderKeywords = null)
        {
            return RegisterRenderer(source, prefab, GPUIProfile.DefaultProfile, out rendererKey, groupID, transformBufferType, shaderKeywords);
        }

        public static bool RegisterRenderer(UnityEngine.Object source, GameObject prefab, GPUIProfile profile, out int rendererKey, int groupID = 0, GPUITransformBufferType transformBufferType = GPUITransformBufferType.Default, List<string> shaderKeywords = null)
        {
            GPUIPrototype prototype = new GPUIPrototype(prefab, profile);
            return RegisterRenderer(source, prototype, out rendererKey, groupID, transformBufferType, shaderKeywords);
        }

        public static bool RegisterRenderer(UnityEngine.Object source, GPUIPrototype prototype, out int rendererKey, int groupID = 0, GPUITransformBufferType transformBufferType = GPUITransformBufferType.Default, List<string> shaderKeywords = null)
        {
            InitializeRenderingSystem();

            return RegisterRenderer(source, prototype.GetKey(), Instance.LODGroupDataProvider.GetOrCreateLODGroupData(prototype), prototype.profile, out rendererKey, groupID, transformBufferType, shaderKeywords);
        }

        public static bool RegisterRenderer(UnityEngine.Object source, int prototypeKey, GPUILODGroupData lodGroupData, GPUIProfile profile, out int rendererKey, int groupID, GPUITransformBufferType transformBufferType, List<string> shaderKeywords)
        {
            InitializeRenderingSystem();

            rendererKey = 0;
            if (source == null)
            {
                Debug.LogError("Source is null!");
                return false;
            }
            if (lodGroupData == null)
            {
                Debug.LogError("LODGroupData is null!", source);
                return false;
            }
            if (profile == null)
            {
                Debug.LogError("Profile is null!", source);
                return false;
            }
            if (profile.isLODCrossFade && (shaderKeywords == null || !shaderKeywords.Contains(GPUIConstants.Kw_LOD_FADE_CROSSFADE)))
            {
                shaderKeywords ??= new List<string>();
                shaderKeywords.Add(GPUIConstants.Kw_LOD_FADE_CROSSFADE);
            }

            GPUIRenderSourceGroup renderSourceGroup = Instance.RenderSourceGroupProvider.GetOrCreateRenderSourceGroup(prototypeKey, lodGroupData, profile, groupID, transformBufferType, shaderKeywords);
            GPUIRenderSource renderSource = new GPUIRenderSource(source, renderSourceGroup);
            rendererKey = renderSource.Key;
            if (renderSourceGroup.AddRenderSource(source, renderSource))
                return Instance.RenderSourceProvider.AddOrSet(rendererKey, renderSource);
            return false;
        }

        public static int GetBufferSize(int renderKey)
        {
            if (IsActive && Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
                return renderSource.bufferSize;
            return 0;
        }

        public static bool SetBufferSize(int renderKey, int bufferSize, bool isCopyPreviousData = true)
        {
            if (bufferSize < 0)
            {
                Debug.LogError("Buffer size is not set for renderer with key: " + renderKey);
                return false;
            }
            if (bufferSize > GPUIConstants.MAX_BUFFER_SIZE)
            {
                Debug.LogError(bufferSize.ToString("#,0") + " exceeds maximum allowed buffer size (" + GPUIConstants.MAX_BUFFER_SIZE.ToString("#,0") + ").");
                return false;
            }
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.SetBufferSize(bufferSize, isCopyPreviousData);
                return true;
            }

            Debug.LogError("Renderer is not registered with key: " + renderKey);
            return false;
        }

        public static int GetInstanceCount(int renderKey)
        {
            if (IsActive && Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
                return renderSource.instanceCount;
            return 0;
        }

        public static bool SetInstanceCount(int renderKey, int instanceCount)
        {
            if (instanceCount < 0)
            {
                Debug.LogError("Instance Count is not set for renderer with key: " + renderKey);
                return false;
            }
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.SetInstanceCount(instanceCount);
                return true;
            }

            Debug.LogError("Renderer is not registered with key: " + renderKey);
            return false;
        }

        public static bool SetTransformBufferData<T>(int renderKey, NativeArray<T> matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer = true) where T : struct
        {
            if (matrices == null)
            {
                Debug.LogError("Matrices are not set for renderer with key: " + renderKey);
                return false;
            }
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.SetTransformBufferData(matrices, managedBufferStartIndex, graphicsBufferStartIndex, count, isOverwritePreviousFrameBuffer);
                return true;
            }

            Debug.LogError("Renderer is not registered with key: " + renderKey);
            return false;
        }

        public static bool SetTransformBufferData<T>(int renderKey, T[] matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer = true) where T : struct
        {
            if (matrices == null)
            {
                Debug.LogError("Matrices are not set for renderer with key: " + renderKey);
                return false;
            }
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.SetTransformBufferData(matrices, managedBufferStartIndex, graphicsBufferStartIndex, count, isOverwritePreviousFrameBuffer);
                return true;
            }

            Debug.LogError("Renderer is not registered with key: " + renderKey);
            return false;
        }

        public static bool SetTransformBufferData<T>(int renderKey, List<T> matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer = true) where T : struct
        {
            if (matrices == null)
            {
                Debug.LogError("Matrices are not set for renderer with key: " + renderKey);
                return false;
            }
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.SetTransformBufferData(matrices, managedBufferStartIndex, graphicsBufferStartIndex, count, isOverwritePreviousFrameBuffer);
                return true;
            }

            Debug.LogError("Renderer is not registered with key: " + renderKey);
            return false;
        }

        public static void AddMaterialPropertyOverride(int renderKey, string propertyName, object propertyValue, int lodIndex = -1, int rendererIndex = -1)
        {
            AddMaterialPropertyOverride(renderKey, Shader.PropertyToID(propertyName), propertyValue, lodIndex, rendererIndex);
        }

        public static void AddMaterialPropertyOverride(int renderKey, int nameID, object propertyValue, int lodIndex = -1, int rendererIndex = -1)
        {
            if (Instance == null)
            {
                Debug.LogError("Rendering system is not initialized. Can not override MaterialPropertyBlock.");
                return;
            }
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.renderSourceGroup.AddMaterialPropertyOverride(nameID, propertyValue, lodIndex, rendererIndex);
                return;
            }

            Debug.LogError("Renderer is not registered with key: " + renderKey);
        }

        public static void AddDependentDisposable(IGPUIDisposable gpuiDisposable)
        {
            if (Instance == null)
            {
                Debug.LogError("Rendering system is not initialized. Can not add Disposable.");
                return;
            }
            if (!Instance._dependentDisposables.Contains(gpuiDisposable))
                Instance._dependentDisposables.Add(gpuiDisposable);
        }

        public static void AddDependentDisposable(int renderKey, IGPUIDisposable gpuiDisposable)
        {
            if (Instance == null)
            {
                Debug.LogError("Rendering system is not initialized. Can not add Disposable.");
                return;
            }
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.renderSourceGroup.AddDependentDisposable(gpuiDisposable);
                return;
            }

            Debug.LogError("Renderer is not registered with key: " + renderKey);
            return;
        }

        public static void DisposeRenderer(int renderKey)
        {
            if (Instance == null || !Instance.IsInitialized)
                return;
            Instance.RenderSourceProvider.DisposeRenderer(renderKey);
        }

        internal void UpdateCommandBuffers(bool forceNew = false)
        {
            if (CameraDataProvider == null) return;
            Profiler.BeginSample("GPUIRenderingSystem.UpdateCommandBuffer");
            foreach (GPUICameraData cameraData in CameraDataProvider.Values)
                UpdateCommandBuffers(cameraData, forceNew);
#if UNITY_EDITOR
            CameraDataProvider.UpdateEditModeCameraDataCommandBuffers(forceNew);
#endif
            OnCommandBufferModified?.Invoke();
            Profiler.EndSample();
        }

        internal void UpdateCommandBuffers(GPUICameraData cameraData, bool forceNew = false)
        {
            if (forceNew)
                cameraData.ClearVisibilityData();
            foreach (GPUIRenderSourceGroup renderSourceGroup in RenderSourceGroupProvider.Values)
            {
                renderSourceGroup.TransformBufferData?.ReleaseInstanceDataBuffers(cameraData);
                renderSourceGroup.UpdateCommandBuffer(cameraData);
            }
        }

        internal void UpdateCommandBuffers(GPUIRenderSourceGroup rsg)
        {
            foreach (GPUICameraData cameraData in CameraDataProvider.Values)
                rsg.UpdateCommandBuffer(cameraData);
#if UNITY_EDITOR
            CameraDataProvider.UpdateEditModeCameraDataCommandBuffers(rsg);
#endif
        }

        public static bool TryGetLODGroupData(GPUIPrototype prototype, out GPUILODGroupData lodGroupData)
        {
            if (prototype == null)
            {
                lodGroupData = null;
                return false;
            }
            return TryGetLODGroupData(prototype.GetKey(), out lodGroupData);
        }

        public static bool TryGetLODGroupData(int key, out GPUILODGroupData lodGroupData)
        {
            if (!IsActive)
            {
                lodGroupData = null;
                return false;
            }
            return Instance.LODGroupDataProvider.TryGetData(key, out lodGroupData);
        }

        public static bool TryGetRenderSourceGroup(int runtimeRenderKey, out GPUIRenderSourceGroup renderSourceGroup)
        {
            renderSourceGroup = null;
            if (!IsActive || runtimeRenderKey == 0)
                return false;
            foreach (var rsg in Instance.RenderSourceGroupProvider.Values)
            {
                foreach (var rs in rsg.RenderSources)
                {
                    if (rs.Key == runtimeRenderKey)
                    {
                        renderSourceGroup = rsg;
                        return renderSourceGroup != null;
                    }
                }
            }

            return false;
        }

        public static bool TryGetRenderSource(int runtimeRenderKey, out GPUIRenderSource renderSource)
        {
            renderSource = null;
            if (!IsActive || runtimeRenderKey == 0)
                return false;
            return Instance.RenderSourceProvider.TryGetData(runtimeRenderKey, out renderSource);
        }

        public static bool TryGetTransformBuffer(int runtimeRenderKey, out GraphicsBuffer transformBuffer, out int bufferStartIndex, GPUICameraData cameraData = null, bool resetCrossFade = true)
        {
            return TryGetTransformBuffer(runtimeRenderKey, out transformBuffer, out bufferStartIndex, out _, cameraData, resetCrossFade);
        }

        public static bool TryGetTransformBuffer(int runtimeRenderKey, out GraphicsBuffer transformBuffer, out int bufferStartIndex, out int bufferSize, GPUICameraData cameraData = null, bool resetCrossFade = true)
        {
            transformBuffer = null;
            if (TryGetTransformBuffer(runtimeRenderKey, out GPUIShaderBuffer shaderBuffer, out bufferStartIndex, out bufferSize, cameraData, resetCrossFade))
            {
                transformBuffer = shaderBuffer.Buffer;
                return transformBuffer != null;
            }

            return false;
        }

        public static bool TryGetTransformBuffer(int runtimeRenderKey, out GPUIShaderBuffer shaderBuffer, out int bufferStartIndex, GPUICameraData cameraData = null, bool resetCrossFade = true)
        {
            return TryGetTransformBuffer(runtimeRenderKey, out shaderBuffer, out bufferStartIndex, out _, cameraData, resetCrossFade);
        }

        public static bool TryGetTransformBuffer(int runtimeRenderKey, out GPUIShaderBuffer shaderBuffer, out int bufferStartIndex, out int bufferSize, GPUICameraData cameraData = null, bool resetCrossFade = true)
        {
            shaderBuffer = null;
            bufferStartIndex = 0;
            bufferSize = 0;
            if (!IsActive)
                return false;
            if (Instance.RenderSourceProvider.TryGetData(runtimeRenderKey, out GPUIRenderSource rs) && rs.renderSourceGroup != null)
            {
                bufferStartIndex = rs.bufferStartIndex;
                bufferSize = rs.bufferSize;
                GPUITransformBufferData transformBufferData = rs.renderSourceGroup.TransformBufferData;
                if (transformBufferData != null)
                {
                    if (resetCrossFade)
                        transformBufferData.resetCrossFadeDataFrame = Time.frameCount;
                    if (cameraData == null)
                        shaderBuffer = transformBufferData.GetTransformBuffer();
                    else
                        shaderBuffer = transformBufferData.GetTransformBuffer(cameraData);
                    return shaderBuffer != null;
                }
            }

            return false;
        }

        #endregion RenderSource

        #region Camera Data

        internal static void AddCameraData(GPUICameraData cameraData)
        {
            InitializeRenderingSystem();
            Instance.CameraDataProvider.AddCameraData(cameraData);
        }

        #endregion Camera Data

        #region Parameters

        internal void UpdateParameterBufferData()
        {
            foreach (var parameterBufferData in ParameterBufferIndexes.Keys)
            {
                parameterBufferData.SetParameterBufferData();
            }
        }

        #endregion Parameters

        #region Editor Methods
#if UNITY_EDITOR
        private void Editor_HandlePlayModeStates()
        {
            EditorApplication.playModeStateChanged -= Editor_HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += Editor_HandlePlayModeStateChanged;

            EditorApplication.pauseStateChanged -= Editor_HandlePauseStateChanged;
            EditorApplication.pauseStateChanged += Editor_HandlePauseStateChanged;
        }

        private static void Editor_HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            playModeStateChange = state;
            //Debug.Log("State: " + state + " IsPlaying: " + Application.isPlaying);
            if (!IsActive)
                return;
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    Instance.MaterialProvider.Reset();
                    Instance.CameraDataProvider.Reset();
                    Instance.LODGroupDataProvider.ClearNullValues();
                    Instance.ParameterBuffer.ReleaseBuffers();
                    Instance.ParameterBufferIndexes.Clear();
                    Instance.CameraDataProvider.ClearEditModeCameraData();
                    editor_PlayModeFullRenderSceneViewCameras?.RemoveAll((c) => c == null);
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    Instance.MaterialProvider.Reset();
                    Instance.CameraDataProvider.Reset();
                    Instance.LODGroupDataProvider.ClearNullValues();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    Instance.CameraDataProvider.ClearEditModeCameraData();
                    editor_PlayModeFullRenderSceneViewCameras?.RemoveAll((c) => c == null);
                    break;
            }

            if (state == PlayModeStateChange.ExitingPlayMode && editor_profileRollbackCache != null)
            {
                foreach (var item in editor_profileRollbackCache)
                {
                    if (item.Key != null && item.Value != null)
                    {
                        item.Key.CopyValuesFrom(item.Value);
                        Destroy(item.Value);
                    }
                }
                editor_profileRollbackCache = null;
            }

            if (state == PlayModeStateChange.EnteredPlayMode && editor_PlayModeFullRenderSceneViewCameras != null)
            {
                foreach (Camera camera in editor_PlayModeFullRenderSceneViewCameras)
                    Editor_AddSceneViewCameraData(camera);
            }

            Instance.IsPaused = false;
            Instance.MaterialProvider.checkForShaderModifications = true;
        }

        private static void Editor_HandlePauseStateChanged(PauseState state)
        {
            if (!IsActive)
                return;
            Instance.IsPaused = state == PauseState.Paused;
        }

        public static void Editor_CacheProfile(GPUIProfile profile)
        {
            if (!Application.isPlaying) return;
            editor_profileRollbackCache ??= new();
            if (editor_profileRollbackCache.ContainsKey(profile)) return;
            editor_profileRollbackCache[profile] = ScriptableObject.Instantiate(profile);
        }

        public static GPUICameraData Editor_AddSceneViewCameraData(Camera camera)
        {
            if (!IsActive || camera == null || camera.cameraType != CameraType.SceneView)
                return null;
            if (!Instance.CameraDataProvider.TryGetEditModeCameraData(camera.GetInstanceID(), out GPUICameraData editModeCameraData))
            {
                editModeCameraData = new GPUICameraData(camera);
                editModeCameraData.renderToSceneView = false;
                Instance.CameraDataProvider.AddEditModeCameraData(editModeCameraData);
                Instance.UpdateCommandBuffers(editModeCameraData);

            }
            editor_PlayModeFullRenderSceneViewCameras ??= new();
            if (!editor_PlayModeFullRenderSceneViewCameras.Contains(camera))
                editor_PlayModeFullRenderSceneViewCameras.Add(camera);
            return editModeCameraData;
        }

        public static void Editor_RemoveSceneViewCameraData(Camera camera)
        {
            if (!IsActive || camera == null || camera.cameraType != CameraType.SceneView)
                return;
            if (Instance.CameraDataProvider.TryGetEditModeCameraData(camera.GetInstanceID(), out GPUICameraData editModeCameraData))
                Instance.CameraDataProvider.RemoveEditModeCameraData(editModeCameraData);
            if (editor_PlayModeFullRenderSceneViewCameras != null && editor_PlayModeFullRenderSceneViewCameras.Contains(camera))
                editor_PlayModeFullRenderSceneViewCameras.Remove(camera);
        }

        public static bool Editor_ContainsSceneViewCameraData(Camera camera)
        {
            if (!IsActive || camera == null || camera.cameraType != CameraType.SceneView)
                return false;
            if (Instance.CameraDataProvider.TryGetEditModeCameraData(camera.GetInstanceID(), out _))
                return true;
            if (editor_PlayModeFullRenderSceneViewCameras != null && editor_PlayModeFullRenderSceneViewCameras.Contains(camera))
                return true;
            return false;
        }
#endif
#endregion Editor Methods
    }

#if UNITY_EDITOR
    public struct GPUIRenderStatistics
    {
        public uint drawCount;
        public uint shadowDrawCount;
        public uint vertexCount;
        public uint shadowVertexCount;
    }
#endif
}
