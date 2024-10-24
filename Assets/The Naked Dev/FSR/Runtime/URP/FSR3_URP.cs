using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using FidelityFX;
using UnityEngine.XR;

namespace TND.FSR
{
    [RequireComponent(typeof(Camera))]
    public class FSR3_URP : FSR3_BASE
    {
        //Rendertextures
        public RTHandle m_opaqueOnlyColorBuffer;
        public RTHandle m_afterOpaqueOnlyColorBuffer;
        public RTHandle m_reactiveMaskOutput;
        //public RTHandle m_colorBuffer;
        public RTHandle m_fsrOutput;
        public bool m_autoHDR;

        private List<FSRScriptableRenderFeature> fsrScriptableRenderFeature;
        private bool containsRenderFeature = false;
        private bool m_usePhysicalProperties;

        //UniversalRenderPipelineAsset 
        private UniversalRenderPipelineAsset UniversalRenderPipelineAsset;
        private UniversalAdditionalCameraData m_cameraData;

        private GraphicsFormat m_graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
        public GraphicsFormat m_prevGraphicsFormat;

        public readonly Fsr3.DispatchDescription m_dispatchDescription = new Fsr3.DispatchDescription();
        public readonly Fsr3.GenerateReactiveDescription m_genReactiveDescription = new Fsr3.GenerateReactiveDescription();

        public Fsr3UpscalerContext[] m_context;
        private Fsr3UpscalerAssets m_assets;

        public bool m_cameraStacking = false;
        public Camera m_topCamera;
        private int m_prevCameraStackCount;
        private bool m_isBaseCamera;
        private List<FSR3_URP> m_prevCameraStack = new List<FSR3_URP>();
        private FSR3_Quality m_prevStackQuality = (FSR3_Quality)(-1);

        private int prevDisplayWidth, prevDisplayHeight;


        protected override void InitializeFSR()
        {
            base.InitializeFSR();
            m_mainCamera.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
            if (m_assets == null)
            {
                m_assets = Resources.Load<Fsr3UpscalerAssets>("Fsr3UpscalerAssets");
            }
            SetupResolution();

            if (!m_fsrInitialized)
            {
                RenderPipelineManager.beginCameraRendering += PreRenderCamera;
                RenderPipelineManager.endCameraRendering += PostRenderCamera;
            }

            if (m_cameraData == null)
            {
                m_cameraData = m_mainCamera.GetUniversalAdditionalCameraData();
                if (m_cameraData != null)
                {
                    if (m_cameraData.renderType == CameraRenderType.Base)
                    {
                        m_isBaseCamera = true;
                        SetupCameraStacking();
                    }
                }
            }
        }


        /// <summary>
        /// Sets up the buffers, initializes the fsr context, and sets up the command buffer
        /// Must be recalled whenever the display resolution changes
        /// </summary>
        private void SetupCommandBuffer()
        {
            if (m_fsrOutput != null)
            {
                m_fsrOutput.Release();
                if (m_opaqueOnlyColorBuffer != null)
                {
                    m_opaqueOnlyColorBuffer.Release();
                    m_afterOpaqueOnlyColorBuffer.Release();
                    m_reactiveMaskOutput.Release();
                }

                if (m_genReactiveDescription.ColorOpaqueOnly.IsValid)
                {
                    m_genReactiveDescription.ColorOpaqueOnly = ResourceView.Unassigned;
                    m_genReactiveDescription.ColorPreUpscale = ResourceView.Unassigned;
                    m_genReactiveDescription.OutReactive = ResourceView.Unassigned;
                    m_dispatchDescription.Reactive = ResourceView.Unassigned;
                }
            }

            if (fsrScriptableRenderFeature != null)
            {
                for (int i = 0; i < fsrScriptableRenderFeature.Count; i++)
                {
                    fsrScriptableRenderFeature[i].OnDispose();
                }
            }
            else
            {
                containsRenderFeature = GetRenderFeature();
            }
            SetDynamicResolution(m_scaleFactor);

            m_renderWidth = (int)(m_mainCamera.pixelWidth / m_scaleFactor);
            m_renderHeight = (int)(m_mainCamera.pixelHeight / m_scaleFactor);

            if (m_mainCamera.stereoEnabled)
            {
                m_renderWidth = XRSettings.eyeTextureWidth;
                m_renderHeight = XRSettings.eyeTextureHeight;
                m_displayWidth = (int)(XRSettings.eyeTextureWidth / XRSettings.eyeTextureResolutionScale);
                m_displayHeight = (int)(XRSettings.eyeTextureHeight / XRSettings.eyeTextureResolutionScale);
            }

            m_fsrOutput = RTHandles.Alloc(m_displayWidth, m_displayHeight, enableRandomWrite: true, colorFormat: m_graphicsFormat, msaaSamples: MSAASamples.None, name: "FSR OUTPUT");

            m_dispatchDescription.RenderSize = new Vector2Int(m_renderWidth, m_renderHeight);
            m_dispatchDescription.Output = new ResourceView(m_fsrOutput);

            if (generateReactiveMask)
            {
                m_opaqueOnlyColorBuffer = RTHandles.Alloc(m_renderWidth, m_renderHeight, enableRandomWrite: false, colorFormat: m_graphicsFormat, msaaSamples: MSAASamples.None, name: "OPAQUE ONLY BUFFER");
                m_afterOpaqueOnlyColorBuffer = RTHandles.Alloc(m_renderWidth, m_renderHeight, enableRandomWrite: false, colorFormat: m_graphicsFormat, msaaSamples: MSAASamples.None, name: "AFTER OPAQUE ONLY BUFFER");
                m_reactiveMaskOutput = RTHandles.Alloc(m_renderWidth, m_renderHeight, enableRandomWrite: true, colorFormat: m_graphicsFormat, msaaSamples: MSAASamples.None, name: "FSR REACTIVE MASK OUTPUT");

                m_genReactiveDescription.ColorOpaqueOnly = new ResourceView(m_opaqueOnlyColorBuffer);
                m_genReactiveDescription.ColorPreUpscale = new ResourceView(m_afterOpaqueOnlyColorBuffer);
                m_genReactiveDescription.OutReactive = new ResourceView(m_reactiveMaskOutput);
                m_dispatchDescription.Reactive = m_genReactiveDescription.OutReactive;
            }

            if (!containsRenderFeature)
            {
                Debug.LogError("Current Universal Render Data is missing the 'FSR Scriptable Render Pass URP' Rendering Feature");
            }
            else
            {
                for (int i = 0; i < fsrScriptableRenderFeature.Count; i++)
                {
                    fsrScriptableRenderFeature[i].OnSetReference(this);
                }
            }
            for (int i = 0; i < fsrScriptableRenderFeature.Count; i++)
            {
                fsrScriptableRenderFeature[i].IsEnabled = true;
            }
        }


        private bool GetRenderFeature()
        {
            fsrScriptableRenderFeature = new List<FSRScriptableRenderFeature>();

            UniversalRenderPipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            bool fsrScriptableRenderFeatureFound = false;
            if (UniversalRenderPipelineAsset != null)
            {
                UniversalRenderPipelineAsset.upscalingFilter = UpscalingFilterSelection.Linear;

                var type = UniversalRenderPipelineAsset.GetType();
                var propertyInfo = type.GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);

                if (propertyInfo != null)
                {
                    var scriptableRenderData = (ScriptableRendererData[])propertyInfo.GetValue(UniversalRenderPipelineAsset);

                    if (scriptableRenderData != null && scriptableRenderData.Length > 0)
                    {
                        foreach (var renderData in scriptableRenderData)
                        {

                            foreach (var renderFeature in renderData.rendererFeatures)
                            {

                                FSRScriptableRenderFeature fsrFeature = renderFeature as FSRScriptableRenderFeature;
                                if (fsrFeature != null)
                                {
                                    fsrScriptableRenderFeature.Add(renderFeature as FSRScriptableRenderFeature);
                                    fsrScriptableRenderFeatureFound = true;

                                    //Stop looping the current renderer, we only allow 1 instance per renderer 
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("FSR 3: Can't find UniversalRenderPipelineAsset");
            }

            return fsrScriptableRenderFeatureFound;
        }

        void PreRenderCamera(ScriptableRenderContext context, Camera cameras)
        {

            if (cameras != m_mainCamera)
            {
                return;
            }
            m_displayWidth = m_mainCamera.pixelWidth;
            m_displayHeight = m_mainCamera.pixelHeight;
            if (cameras.stereoEnabled)
            {
                m_displayWidth = (int)(XRSettings.eyeTextureWidth / XRSettings.eyeTextureResolutionScale);
                m_displayHeight = (int)(XRSettings.eyeTextureHeight / XRSettings.eyeTextureResolutionScale);
            }

            // Set up the parameters to auto-generate a reactive mask
            if (generateReactiveMask)
            {
                m_genReactiveDescription.RenderSize = new Vector2Int(m_renderWidth, m_renderHeight);
                m_genReactiveDescription.Scale = autoReactiveScale;
                m_genReactiveDescription.CutoffThreshold = autoReactiveThreshold;
                m_genReactiveDescription.BinaryValue = autoReactiveBinaryValue;

                m_genReactiveDescription.Flags = reactiveFlags;
            }

            //m_dispatchDescription.Exposure = null;
            m_dispatchDescription.Exposure = ResourceView.Unassigned;
            m_dispatchDescription.PreExposure = 1;
            m_dispatchDescription.EnableSharpening = sharpening;
            m_dispatchDescription.Sharpness = sharpness;
            m_dispatchDescription.MotionVectorScale.x = -m_renderWidth;
            m_dispatchDescription.MotionVectorScale.y = -m_renderHeight;
            m_dispatchDescription.RenderSize = new Vector2Int(m_renderWidth, m_renderHeight);
            m_dispatchDescription.FrameTimeDelta = Time.deltaTime;
            m_dispatchDescription.CameraNear = m_mainCamera.nearClipPlane;
            m_dispatchDescription.CameraFar = m_mainCamera.farClipPlane;
            m_dispatchDescription.CameraFovAngleVertical = m_mainCamera.fieldOfView * Mathf.Deg2Rad;
            m_dispatchDescription.ViewSpaceToMetersFactor = 1.0f;
            m_dispatchDescription.Reset = m_resetCamera;
            m_resetCamera = false;

            if (SystemInfo.usesReversedZBuffer)
            {
                // Swap the near and far clip plane distances as FSR3 expects this when using inverted depth
                (m_dispatchDescription.CameraNear, m_dispatchDescription.CameraFar) = (m_dispatchDescription.CameraFar, m_dispatchDescription.CameraNear);
            }

            JitterCameraMatrix(context);

            if (UniversalRenderPipelineAsset != GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset)
            {
                if (fsrScriptableRenderFeature != null)
                {
                    for (int i = 0; i < fsrScriptableRenderFeature.Count; i++)
                    {
                        fsrScriptableRenderFeature[i].OnDispose();
                    }
                }
                fsrScriptableRenderFeature = null;
                OnSetQuality(FSRQuality);
                SetupCommandBuffer();
            }

            //Check if display resolution has changed
            if (m_displayWidth != prevDisplayWidth || m_displayHeight != prevDisplayHeight || m_previousHDR != m_autoHDR || m_prevGraphicsFormat != m_graphicsFormat)
            {
                SetupResolution();
            }

            if (m_previousScaleFactor != m_scaleFactor || m_previousReactiveMask != generateReactiveMask || m_previousTCMask != generateTCMask || m_previousRenderingPath != m_mainCamera.actualRenderingPath)
            {
                SetupFrameBuffers();
            }

            //Camera Stacking
            if (m_isBaseCamera)
            {
                if (m_cameraData != null)
                {
                    if (m_cameraStacking)
                    {
                        try
                        {
                            if (m_topCamera != m_cameraData.cameraStack[m_cameraData.cameraStack.Count - 1] || m_prevCameraStackCount != m_cameraData.cameraStack.Count || m_prevStackQuality != FSRQuality)
                            {
                                SetupCameraStacking();
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        void PostRenderCamera(ScriptableRenderContext context, Camera cameras)
        {
            if (cameras != m_mainCamera)
            {
                return;
            }

            m_mainCamera.usePhysicalProperties = m_usePhysicalProperties;
            if (!m_mainCamera.usePhysicalProperties)
                m_mainCamera.ResetProjectionMatrix();
        }

        /// <summary>
        /// FSR TAA Jitter
        /// </summary>
        private void JitterCameraMatrix(ScriptableRenderContext context)
        {
            if (fsrScriptableRenderFeature == null)
            {
                return;
            }
            else if (!fsrScriptableRenderFeature[0].IsEnabled)
            {
                return;
            }


            int jitterPhaseCount = Fsr3.GetJitterPhaseCount(m_renderWidth, (int)(m_renderWidth * m_scaleFactor));
            Fsr3.GetJitterOffset(out float jitterX, out float jitterY, Time.frameCount, jitterPhaseCount);
            m_dispatchDescription.JitterOffset = new Vector2(jitterX, jitterY);

            jitterX = 2.0f * jitterX / (float)m_renderWidth;
            jitterY = 2.0f * jitterY / (float)m_renderHeight;

            jitterX += UnityEngine.Random.Range(-0.0001f * antiGhosting, 0.0001f * antiGhosting);
            jitterY += UnityEngine.Random.Range(-0.0001f * antiGhosting, 0.0001f * antiGhosting);

            m_usePhysicalProperties = m_mainCamera.usePhysicalProperties;

            if (m_mainCamera.stereoEnabled)
            {
                // We only need to configure all of this once for stereo, during OnPreCull
                ConfigureStereoJitteredProjectionMatrices(context, m_mainCamera, jitterX, jitterY);
            }
            else
            {
                ConfigureJitteredProjectionMatrix(m_mainCamera, jitterX, jitterY);
            }
        }

        /// <summary>
        /// Prepares the jittered and non jittered projection matrices.
        /// </summary>
        /// <param name="context">The current post-processing context.</param>
        public void ConfigureJitteredProjectionMatrix(Camera camera, float jitterX, float jitterY)
        {
            var jitterTranslationMatrix = Matrix4x4.Translate(new Vector3(jitterX, jitterY, 0));
            var m_projectionMatrix = camera.projectionMatrix;
            camera.nonJitteredProjectionMatrix = m_projectionMatrix;
            camera.projectionMatrix = jitterTranslationMatrix * camera.nonJitteredProjectionMatrix;
            camera.useJitteredProjectionMatrixForTransparentRendering = true;
        }

        /// <summary>
        /// Prepares the jittered and non jittered projection matrices for stereo rendering.
        /// </summary>
        /// <param name="context">The current post-processing context.</param>
        // TODO: We'll probably need to isolate most of this for SRPs
        public void ConfigureStereoJitteredProjectionMatrices(ScriptableRenderContext context, Camera camera, float jitterX, float jitterY)
        {
            for (var eye = Camera.StereoscopicEye.Left; eye <= Camera.StereoscopicEye.Right; eye++)
            {
                // This saves off the device generated projection matrices as non-jittered
                camera.CopyStereoDeviceProjectionMatrixToNonJittered(eye);
                var originalProj = camera.GetStereoNonJitteredProjectionMatrix(eye);
                // Currently no support for custom jitter func, as VR devices would need to provide
                // original projection matrix as input along with jitter
                var jitteredMatrix = GenerateJitteredProjectionMatrixFromOriginal(camera, originalProj, jitterX, jitterY);
                camera.SetStereoProjectionMatrix(eye, jitteredMatrix);
            }

            // jitter has to be scaled for the actual eye texture size, not just the intermediate texture size
            // which could be double-wide in certain stereo rendering scenarios
            //jitter = new Vector2(jitter.x / context.screenWidth, jitter.y / context.screenHeight);
            camera.useJitteredProjectionMatrixForTransparentRendering = true;
        }

        /// <summary>
        /// Gets a jittered perspective projection matrix from an original projection matrix.
        /// </summary>
        /// <param name="context">The current render context</param>
        /// <param name="origProj">The original projection matrix</param>
        /// <param name="jitter">The jitter offset</param>
        /// <returns>A jittered projection matrix</returns>
        public static Matrix4x4 GenerateJitteredProjectionMatrixFromOriginal(Camera context, Matrix4x4 origProj, float jitterX, float jitterY)
        {
            var planes = origProj.decomposeProjection;

            float vertFov = Mathf.Abs(planes.top) + Mathf.Abs(planes.bottom);
            float horizFov = Mathf.Abs(planes.left) + Mathf.Abs(planes.right);

            var planeJitter = new Vector2(jitterX * horizFov / context.pixelWidth,
                jitterY * vertFov / context.pixelHeight);

            planes.left += planeJitter.x;
            planes.right += planeJitter.x;
            planes.top += planeJitter.y;
            planes.bottom += planeJitter.y;

            var jitteredMatrix = Matrix4x4.Frustum(planes);

            return jitteredMatrix;
        }

        /// <summary>
        /// Handle Dynamic Scaling
        /// </summary>
        /// <param name="_value"></param>
        public void SetDynamicResolution(float _value)
        {
            if (UniversalRenderPipelineAsset != null)
            {
                UniversalRenderPipelineAsset.renderScale = 1 / _value;
            }
        }

        /// <summary>
        /// Creates new buffers and sends them to the plugin
        /// </summary>
        private void SetupFrameBuffers()
        {
            m_previousScaleFactor = m_scaleFactor;
            m_previousReactiveMask = generateReactiveMask;

            SetupCommandBuffer();

            m_previousRenderingPath = m_mainCamera.actualRenderingPath;
        }

        /// <summary>
        /// Creates new buffers, sends them to the plugin, and reintilized FSR to adjust the display size
        /// </summary>
        private void SetupResolution()
        {
            m_displayWidth = m_mainCamera.pixelWidth;
            m_displayHeight = m_mainCamera.pixelHeight;

            if (m_mainCamera.stereoEnabled)
            {
                m_displayWidth = (int)(XRSettings.eyeTextureWidth / XRSettings.eyeTextureResolutionScale);
                m_displayHeight = (int)(XRSettings.eyeTextureHeight / XRSettings.eyeTextureResolutionScale);

                if (m_displayWidth == 0)
                {
                    return;
                }
            }
            prevDisplayWidth = m_displayWidth;
            prevDisplayHeight = m_displayHeight;

            m_previousHDR = m_autoHDR;

            m_prevGraphicsFormat = m_graphicsFormat;

            Fsr3.InitializationFlags flags = Fsr3.InitializationFlags.EnableMotionVectorsJitterCancellation
                                           | Fsr3.InitializationFlags.EnableHighDynamicRange
                                           | Fsr3.InitializationFlags.EnableAutoExposure;

            //if (enableF16)//Breaks FSR 3.1, so we disabled it for now!
            //    flags |= Fsr3.InitializationFlags.EnableFP16Usage;

            DestroyContext();

            if (m_mainCamera.stereoEnabled)
            {
                m_context = new Fsr3UpscalerContext[2];
            }
            else
            {
                m_context = new Fsr3UpscalerContext[1];
            }


            for (int i = 0; i < m_context.Length; i++)
            {
                m_context[i] = Fsr3.CreateContext(new Vector2Int(m_displayWidth, m_displayHeight), new Vector2Int((int)(m_displayWidth), (int)(m_displayHeight)), m_assets.shaders, flags);
            }

            SetupFrameBuffers();
        }

        /// <summary>
        /// Automatically Setup camera stacking
        /// </summary>
        private void SetupCameraStacking()
        {
            m_prevCameraStackCount = m_cameraData.cameraStack.Count;
            if (m_cameraData.renderType == CameraRenderType.Base)
            {
                m_isBaseCamera = true;

                m_cameraStacking = m_cameraData.cameraStack.Count > 0;
                if (m_cameraStacking)
                {
                    CleanupOverlayCameras();
                    m_prevStackQuality = FSRQuality;

                    m_topCamera = m_cameraData.cameraStack[m_cameraData.cameraStack.Count - 1];

                    for (int i = 0; i < m_cameraData.cameraStack.Count; i++)
                    {
                        FSR3_URP stackedCamera = m_cameraData.cameraStack[i].gameObject.GetComponent<FSR3_URP>();
                        if (stackedCamera == null)
                        {
                            stackedCamera = m_cameraData.cameraStack[i].gameObject.AddComponent<FSR3_URP>();
                        }
                        m_prevCameraStack.Add(m_cameraData.cameraStack[i].gameObject.GetComponent<FSR3_URP>());

                        //stackedCamera.hideFlags = HideFlags.HideInInspector;
                        stackedCamera.m_cameraStacking = true;
                        stackedCamera.m_topCamera = m_topCamera;

                        stackedCamera.OnSetQuality(FSRQuality);

                        stackedCamera.sharpening = sharpening;
                        stackedCamera.sharpness = sharpness;
                        stackedCamera.generateReactiveMask = generateReactiveMask;
                        stackedCamera.autoReactiveScale = autoReactiveScale;
                        stackedCamera.autoReactiveThreshold = autoReactiveThreshold;
                        stackedCamera.autoReactiveBinaryValue = autoReactiveBinaryValue;
                    }
                }
            }
        }

        private void CleanupOverlayCameras()
        {
            for (int i = 0; i < m_prevCameraStack.Count; i++)
            {
                if (!m_prevCameraStack[i].m_isBaseCamera)
                    DestroyImmediate(m_prevCameraStack[i]);
            }
            m_prevCameraStack = new List<FSR3_URP>();
        }

        protected override void DisableFSR()
        {
            base.DisableFSR();

            RenderPipelineManager.beginCameraRendering -= PreRenderCamera;
            RenderPipelineManager.endCameraRendering -= PostRenderCamera;

            SetDynamicResolution(1);
            if (fsrScriptableRenderFeature != null)
            {
                for (int i = 0; i < fsrScriptableRenderFeature.Count; i++)
                {
                    fsrScriptableRenderFeature[i].IsEnabled = false;
                }
            }
            CleanupOverlayCameras();
            m_previousScaleFactor = -1;
            m_prevStackQuality = (FSR3_Quality)(-1);

            if (m_fsrOutput != null)
            {
                m_fsrOutput.Release();

                if (m_opaqueOnlyColorBuffer != null)
                {
                    m_opaqueOnlyColorBuffer.Release();
                    m_afterOpaqueOnlyColorBuffer.Release();
                    m_reactiveMaskOutput.Release();
                }
            }

            DestroyContext();
        }

        private void DestroyContext()
        {
            if (m_context != null)
            {
                for (int i = 0; i < m_context.Length; i++)
                {
                    m_context[i].Destroy();
                }
                m_context = null;
            }
        }
    }
}
