// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER && GPUI_URP
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
#endif

namespace GPUInstancerPro
{
    public class GPUIOcclusionCullingData : IDisposable
    {
        #region Properties
        /// <summary>
        /// Depth texture with mipmaps that is used for Occlusion Culling
        /// </summary>
        public RenderTexture HiZDepthTexture { get; private set; }
        public int2 HiZTextureSize { get; private set; }
        public Texture UnityDepthTexture { get; private set; }
        public GPUIOcclusionCullingMode ActiveCullingMode { get; private set; }
        public bool IsHiZDepthUpdated => HiZDepthTexture != null && _isHiZDepthUpdated;

        private Camera _activeCamera;
        private bool _isVRCulling;
        private int _hiZMipLevels;
        private bool IsUseCommandBuffer => ActiveCullingMode == GPUIOcclusionCullingMode.CommandBufferAddedToCamera || ActiveCullingMode == GPUIOcclusionCullingMode.CommandBufferExecutedOnEndRendering;
        private bool _isDepthTex2DArray;
        private bool _vrMultiPassMono;
        private bool _isHiZDepthUpdated;

        private CommandBuffer _occlusionCommandBuffer;

        #region Command Buffer Mode Properties
        private RenderTargetIdentifier _unityDepthIdentifier;
        private RenderTargetIdentifier _hiZIdentifier;
        private RenderTextureSubElement _unityDepthSubElement;
        #endregion Command Buffer Mode Properties

        private const string GPUI_HiZ_DepthTexture_NAME = "GPUI_HiZDepthTexture";
        private const string GPUI_HiZ_CommandBuffer_NAME = "GPUI.HiZDepthTexture";
        #endregion Properties

        public enum GPUIOcclusionCullingMode
        {
            Auto = 0,
            DirectTextureAccess = 1,
            CommandBufferAddedToCamera = 2,
            CommandBufferExecutedOnEndRendering = 3,
            URPScriptableRenderPass = 4,
        }

        #region Initialize/Dispose

        public GPUIOcclusionCullingData(Camera camera, GPUIOcclusionCullingMode cullingMode, bool isVRCulling)
        {
            _activeCamera = camera;
            _isVRCulling = isVRCulling;
            Initialize(cullingMode);
        }

        public void Initialize(GPUIOcclusionCullingMode cullingMode)
        {
            Dispose();

            _activeCamera.depthTextureMode |= DepthTextureMode.Depth;

            DetermineOcclusionCullingMode(cullingMode);
        }

        public void Dispose()
        {
            DisposeHiZDepthTexture();
            DisposeOcclusionCommandBuffer();
            DisposeScriptableRenderPass();
            UnityDepthTexture = null;
        }

        private void DetermineOcclusionCullingMode(GPUIOcclusionCullingMode cullingMode)
        {
            if (cullingMode == GPUIOcclusionCullingMode.Auto)
            {
                if (_isVRCulling)
                {
                    ActiveCullingMode = GPUIOcclusionCullingMode.DirectTextureAccess;
                    return;
                }
                if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
                {
                    ActiveCullingMode = GPUIOcclusionCullingMode.CommandBufferAddedToCamera;
                    return;
                }

#if UNITY_6000_0_OR_NEWER && GPUI_URP
                if (GPUIRuntimeSettings.Instance.IsURP)
                {
                    ActiveCullingMode = GPUIOcclusionCullingMode.URPScriptableRenderPass;
                    return;
                }
#endif

#if UNITY_6000_0_OR_NEWER && GPUI_HDRP
                if (GPUIRuntimeSettings.Instance.IsHDRP)
                {
                    ActiveCullingMode = GPUIOcclusionCullingMode.DirectTextureAccess;
                    return;
                }
#endif

                if (GPUIRuntimeSettings.Instance.IsHDRP)
                    ActiveCullingMode = GPUIOcclusionCullingMode.CommandBufferExecutedOnEndRendering;
                else
                    ActiveCullingMode = GPUIOcclusionCullingMode.DirectTextureAccess;
                return;
            }

            if (cullingMode == GPUIOcclusionCullingMode.URPScriptableRenderPass)
            {
                if (!GPUIRuntimeSettings.Instance.IsURP)
                {
                    Debug.LogWarning("OcclusionCullingMode.URPScriptableRenderPass is only supported in Universal Render Pipeline! Switching to OcclusionCullingMode.Auto.");
                    DetermineOcclusionCullingMode(GPUIOcclusionCullingMode.Auto);
                    return;
                }
#if !UNITY_6000_0_OR_NEWER
                Debug.LogWarning("OcclusionCullingMode.URPScriptableRenderPass is only supported for Unity versions 6000 or higher! Switching to OcclusionCullingMode.Auto.");
                DetermineOcclusionCullingMode(GPUIOcclusionCullingMode.Auto);
                return;
#endif
            }
            ActiveCullingMode = cullingMode;
        }

        private bool CreateHiZDepthTexture(int2 screenSize)
        {
            DisposeOcclusionCommandBuffer();
            HiZTextureSize = screenSize;

            _hiZMipLevels = 1 + Mathf.FloorToInt(Mathf.Log(Mathf.Max(HiZTextureSize.x, HiZTextureSize.y), 2f));

            DisposeHiZDepthTexture();
            if (HiZTextureSize.x <= 0 || HiZTextureSize.y <= 0 || _hiZMipLevels == 0)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError("HiZ Texture size is zero!", _activeCamera);
#endif
                return false;
            }

            HiZDepthTexture = new RenderTexture(HiZTextureSize.x, HiZTextureSize.y, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                name = GPUI_HiZ_DepthTexture_NAME,
                filterMode = FilterMode.Point,
                useMipMap = true,
                autoGenerateMips = false,
                enableRandomWrite = true,
                hideFlags = HideFlags.HideAndDontSave
            };
            HiZDepthTexture.Create();
            HiZDepthTexture.GenerateMips();

            return true;
        }

        private void DisposeHiZDepthTexture()
        {
            if (HiZDepthTexture != null)
            {
                GPUITextureUtility.DestroyRenderTexture(HiZDepthTexture);
                HiZDepthTexture = null;
            }
        }

        private void DisposeScriptableRenderPass()
        {
#if UNITY_6000_0_OR_NEWER && GPUI_URP
            if (_hiZGeneratorRenderPass != null)
            {
                _hiZGeneratorRenderPass.Dispose();
                _hiZGeneratorRenderPass = null;
            }
            if(_hiZGeneratorRenderPassRightEye != null)
            {
                _hiZGeneratorRenderPassRightEye.Dispose();
                _hiZGeneratorRenderPassRightEye = null;
            }
#endif
        }

        #endregion Initialize/Dispose

        #region Update Methods

        private int2 GetScreenSize()
        {
            int2 screenSize = int2.zero;
#if GPUI_XR
            if (_isVRCulling)
            {
                screenSize.x = UnityEngine.XR.XRSettings.eyeTextureWidth;
                screenSize.y = UnityEngine.XR.XRSettings.eyeTextureHeight;
                screenSize.x *= 2;
            }
            else
            {
#endif
                screenSize.x = _activeCamera.pixelWidth;
                screenSize.y = _activeCamera.pixelHeight;

#if GPUI_XR
            }
#endif

#if GPUI_URP
            if (GPUIRuntimeSettings.Instance.IsURP && GPUIRuntimeSettings.Instance.TryGetURPAsset(out UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urpAsset) && urpAsset.renderScale != 1f)
            {
                screenSize.x = Mathf.FloorToInt(screenSize.x * urpAsset.renderScale);
                screenSize.y = Mathf.FloorToInt(screenSize.y * urpAsset.renderScale);
            }
#endif
            return screenSize;
        }

        internal void CheckScreenSize()
        {
            int2 newScreenSize = GetScreenSize();
            if (newScreenSize.x != HiZTextureSize.x || newScreenSize.y != HiZTextureSize.y)
                Dispose();
        }

        internal void UpdateHiZTexture(ScriptableRenderContext context)
        {
            _isHiZDepthUpdated = false;

            if (ActiveCullingMode != GPUIOcclusionCullingMode.URPScriptableRenderPass && UnityDepthTexture == null)
            {
                DisposeOcclusionCommandBuffer();
                UnityDepthTexture = Shader.GetGlobalTexture(GPUIConstants.PROP_CameraDepthTexture);
                if (UnityDepthTexture == null || UnityDepthTexture.name == "UnityBlack")
                {
                    UnityDepthTexture = null;
#if GPUIPRO_DEVMODE
                    Debug.LogWarning("Can not find Camera Depth Texture! Camera: " + _activeCamera.name, _activeCamera);
#endif
                    return;
                }

                _isDepthTex2DArray = UnityDepthTexture.dimension == TextureDimension.Tex2DArray;
            }

            if (HiZDepthTexture == null)
            {
                DisposeScriptableRenderPass();
                DisposeOcclusionCommandBuffer();
                if (!CreateHiZDepthTexture(GetScreenSize()))
                    return;
            }
#if UNITY_6000_0_OR_NEWER && GPUI_URP
            if (ActiveCullingMode == GPUIOcclusionCullingMode.URPScriptableRenderPass)
            {
                if (_hiZGeneratorRenderPass == null)
                    _hiZGeneratorRenderPass = new GPUIHiZGeneratorRenderPass(GPUIConstants.CS_HiZTextureCopy, GPUIConstants.CS_TextureReduce, _isVRCulling);
                if (!_hiZGeneratorRenderPass.IsSetup)
                    _hiZGeneratorRenderPass.Setup(HiZDepthTexture, _hiZMipLevels);
#if GPUI_XR
                if (_isVRCulling && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass && _hiZGeneratorRenderPassRightEye == null)
                    _hiZGeneratorRenderPassRightEye = new GPUIHiZGeneratorRenderPass(GPUIConstants.CS_HiZTextureCopy, GPUIConstants.CS_TextureReduce, _isVRCulling);
                if (_hiZGeneratorRenderPassRightEye != null && !_hiZGeneratorRenderPassRightEye.IsSetup)
                    _hiZGeneratorRenderPassRightEye.Setup(HiZDepthTexture, _hiZMipLevels, 1);
#endif
                _isHiZDepthUpdated = true;
                return;
            }
#endif

            if (IsUseCommandBuffer && _occlusionCommandBuffer == null)
                CreateOcclusionCommandBuffer();

            switch (ActiveCullingMode)
            {
                case GPUIOcclusionCullingMode.URPScriptableRenderPass:
                    _isHiZDepthUpdated = true;
                    return;
                case GPUIOcclusionCullingMode.CommandBufferAddedToCamera:
                    _isHiZDepthUpdated = true;
                    return;
                case GPUIOcclusionCullingMode.CommandBufferExecutedOnEndRendering:
                    if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
                        Graphics.ExecuteCommandBuffer(_occlusionCommandBuffer);
                    else
                        context.ExecuteCommandBuffer(_occlusionCommandBuffer);
                    _isHiZDepthUpdated = true;
                    return;
                case GPUIOcclusionCullingMode.DirectTextureAccess:
                    DirectTextureAccessUpdate();
                    _isHiZDepthUpdated = true;
                    return;
            }
#if GPUIPRO_DEVMODE
            Debug.LogError("Update method can not be found for the active OcclusionCullingMode: " + ActiveCullingMode, _activeCamera);
#endif
            return;
        }

        internal void UpdateHiZTextureOnBeginRendering(Camera camera, ScriptableRenderContext context)
        {
#if UNITY_6000_0_OR_NEWER && GPUI_URP
            if (ActiveCullingMode == GPUIOcclusionCullingMode.URPScriptableRenderPass && _hiZGeneratorRenderPass != null)
            {
#if GPUI_XR
                if (_isVRCulling && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass && _hiZGeneratorRenderPassRightEye != null)
                {
                    if (_renderPassQueuedFrameCount != Time.frameCount)
                        camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(_hiZGeneratorRenderPass);
                    else
                        camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(_hiZGeneratorRenderPassRightEye);
                    _renderPassQueuedFrameCount = Time.frameCount;
                }
                else
#endif
                    camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(_hiZGeneratorRenderPass);
            }
#endif
        }

        #endregion Update Methods

        #region Direct Texture Access Mode

        private void DirectTextureAccessUpdate()
        {
#if GPUI_XR
            if (_isVRCulling)
            {
                if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass)
                {
                    if (_activeCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
                        UpdateTextureWithComputeShader(0);
                    else if (_activeCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
                        UpdateTextureWithComputeShader(HiZTextureSize.x / 2);
                    else if (_activeCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Mono) // When stereoActiveEye is not set, first get the left eye and then the right
                    {
                        if (!_vrMultiPassMono)
                        {
                            UpdateTextureWithComputeShader(0);
                            _vrMultiPassMono = true;
                        }
                        else
                        {
                            UpdateTextureWithComputeShader(HiZTextureSize.x / 2);
                            _vrMultiPassMono = false;
                        }
                    }
                }
                else if (_isDepthTex2DArray && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced)
                {
                    UpdateTextureWithComputeShader(0);
                    UpdateTextureWithComputeShader(HiZTextureSize.x / 2, 1);
                }
                else
                    UpdateTextureWithComputeShader(0);
            }
            else if (_isDepthTex2DArray && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced && _activeCamera.stereoTargetEye == StereoTargetEyeMask.Right)
                UpdateTextureWithComputeShader(0, 1);
            else
#endif
                UpdateTextureWithComputeShader(0);
        }

        private void UpdateTextureWithComputeShader(int offset, int textureArrayIndex = 0)
        {
            if (_isDepthTex2DArray)
                GPUITextureUtility.CopyHiZTextureArrayWithComputeShader(UnityDepthTexture, HiZDepthTexture, offset, textureArrayIndex);
            else
                GPUITextureUtility.CopyHiZTextureWithComputeShader(UnityDepthTexture, HiZDepthTexture, offset);

            for (int i = 0; i < _hiZMipLevels - 1; i++)
                GPUITextureUtility.ReduceTextureWithComputeShader(HiZDepthTexture, HiZDepthTexture, offset, i, i + 1);
        }

        #endregion Direct Texture Access Mode

        #region Command Buffer Mode

        private void DisposeOcclusionCommandBuffer()
        {
            if (_occlusionCommandBuffer != null)
            {
                if (ActiveCullingMode == GPUIOcclusionCullingMode.CommandBufferAddedToCamera)
                    _activeCamera.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, _occlusionCommandBuffer);
                _occlusionCommandBuffer.Dispose();
                _occlusionCommandBuffer = null;
            }
        }

        private void CreateOcclusionCommandBuffer()
        {
            DisposeOcclusionCommandBuffer();
            if (HiZDepthTexture == null || UnityDepthTexture == null)
                return;

            if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
                _unityDepthIdentifier = new RenderTargetIdentifier(BuiltinRenderTextureType.Depth);
            else
                _unityDepthIdentifier = new RenderTargetIdentifier(UnityDepthTexture);
            _unityDepthSubElement = GPUIRuntimeSettings.Instance.IsBuiltInRP ? RenderTextureSubElement.Depth : RenderTextureSubElement.Color;

            _hiZIdentifier = new RenderTargetIdentifier(HiZDepthTexture);

            _occlusionCommandBuffer = new CommandBuffer();
            _occlusionCommandBuffer.name = GPUI_HiZ_CommandBuffer_NAME;

#if GPUI_XR
            if (_isVRCulling)
            {
                if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass)
                {
                    if (_activeCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left || _activeCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Mono)
                        UpdateTextureWithComputeShaderCB(0);
                    else
                        UpdateTextureWithComputeShaderCB(HiZTextureSize.x / 2);

                    if (_activeCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Mono)
                        UpdateTextureWithComputeShaderCB(HiZTextureSize.x / 2);
                }
                else if (_isDepthTex2DArray && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced)
                {
                    UpdateTextureWithComputeShaderCB(0);
                    UpdateTextureWithComputeShaderCB(HiZTextureSize.x / 2, 1);
                }
                else
                    UpdateTextureWithComputeShaderCB(0);
            }
            else
#endif
                UpdateTextureWithComputeShaderCB(0);

            if (ActiveCullingMode == GPUIOcclusionCullingMode.CommandBufferAddedToCamera)
                _activeCamera.AddCommandBuffer(CameraEvent.AfterDepthTexture, _occlusionCommandBuffer);
        }

        private void UpdateTextureWithComputeShaderCB(int offset, int textureArrayIndex = 0)
        {
            if (_isDepthTex2DArray)
                GPUITextureUtility.CopyHiZTextureArrayWithComputeShader(_occlusionCommandBuffer, _unityDepthIdentifier, _unityDepthSubElement, UnityDepthTexture.width, UnityDepthTexture.height, _hiZIdentifier, RenderTextureSubElement.Color, offset, textureArrayIndex);
            else
                GPUITextureUtility.CopyHiZTextureWithComputeShader(_occlusionCommandBuffer, _unityDepthIdentifier, _unityDepthSubElement, UnityDepthTexture.width, UnityDepthTexture.height, _hiZIdentifier, RenderTextureSubElement.Color, offset);

            for (int i = 0; i < _hiZMipLevels - 1; i++)
                GPUITextureUtility.ReduceTextureWithComputeShader(_occlusionCommandBuffer, _hiZIdentifier, RenderTextureSubElement.Color, HiZDepthTexture.width, HiZDepthTexture.height, _hiZIdentifier, RenderTextureSubElement.Color, offset, i, i + 1);
        }
        #endregion Command Buffer Mode

        #region URP Scriptable Render Pass Mode

#if UNITY_6000_0_OR_NEWER && GPUI_URP
        internal GPUIHiZGeneratorRenderPass _hiZGeneratorRenderPass;
        internal GPUIHiZGeneratorRenderPass _hiZGeneratorRenderPassRightEye;
        private int _renderPassQueuedFrameCount;

        internal class GPUIHiZGeneratorRenderPass : ScriptableRenderPass, IDisposable
        {
            public bool IsSetup { get; private set; }
            private RTHandle _renderTextureHandle;
            private ComputeShader _copyCompute;
            private ComputeShader _reduceCompute;
            private bool _isVRCulling;
            private int _hiZMipLevels;
            private int _width;
            private int _height;
            private int _eyeIndex;

            private BaseRenderFunc<PassData, ComputeGraphContext> _renderFunc;

            // PassData is used to pass data when recording to the execution of the pass.
            class PassData
            {
                public ComputeShader copyCS;
                public ComputeShader reduceCS;
                public bool isVRCulling;
                public TextureHandle cameraDepthHandle;
                public TextureHandle hiZTextureHandle;
                public int hiZMipLevels;
                public int width;
                public int height;
                public int cameraDepthSlices;
                public int eyeIndex;
            }

            public GPUIHiZGeneratorRenderPass(ComputeShader copyCompute, ComputeShader reduceCompute, bool isVRCulling)
            {
                _copyCompute = copyCompute;
                _reduceCompute = reduceCompute;
                _isVRCulling = isVRCulling;
            }

            public void Setup(RenderTexture renderTexture, int mipLevels, int eyeIndex = 0)
            {
                _renderTextureHandle = RTHandles.Alloc(renderTexture);
                _hiZMipLevels = mipLevels;
                _width = renderTexture.width;
                _height = renderTexture.height;
                _eyeIndex = eyeIndex;
                IsSetup = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;

                TextureHandle inputTexture = renderGraph.ImportTexture(_renderTextureHandle);
                int cameraDepthSlices = cameraDepthTexture.GetDescriptor(renderGraph).slices;

                if (_renderFunc == null)
                    _renderFunc = (PassData data, ComputeGraphContext cgContext) => CopyAndReducePass(data, cgContext);

                using (var builder = renderGraph.AddComputePass("GPUIDepthCopyAndReducePass", out PassData passData))
                {
                    passData.copyCS = _copyCompute;
                    passData.reduceCS = _reduceCompute;
                    passData.isVRCulling = _isVRCulling;
                    passData.cameraDepthHandle = cameraDepthTexture;
                    passData.hiZTextureHandle = inputTexture;
                    passData.hiZMipLevels = _hiZMipLevels;
                    passData.width = _width;
                    passData.height = _height;
                    passData.cameraDepthSlices = cameraDepthSlices;
                    passData.eyeIndex = _eyeIndex;

                    builder.UseTexture(passData.cameraDepthHandle, AccessFlags.Read);
                    builder.UseTexture(passData.hiZTextureHandle, AccessFlags.Write);
                    builder.SetRenderFunc(_renderFunc);
                }
            }

            static void CopyAndReducePass(PassData data, ComputeGraphContext cgContext)
            {
                CopyPass(data, cgContext);
                ReducePass(data, cgContext);
            }

            static void CopyPass(PassData data, ComputeGraphContext cgContext)
            {
                int kernelIndex = 0;
                int sourceWidth = data.isVRCulling ? data.width / 2 : data.width;

                if (data.cameraDepthSlices > 1)
                {
                    kernelIndex = 1;

                    cgContext.cmd.SetComputeTextureParam(data.copyCS, kernelIndex, GPUIConstants.PROP_textureArray, data.cameraDepthHandle, 0);
                    cgContext.cmd.SetComputeTextureParam(data.copyCS, kernelIndex, GPUIConstants.PROP_destination, data.hiZTextureHandle, 0);
                    cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_offsetX, 0);
                    cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_sourceSizeX, sourceWidth);
                    cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_sourceSizeY, data.height);
                    cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_reverseZ, GPUIRuntimeSettings.Instance.ReversedZBuffer ? 1 : 0);
                    cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_textureArrayIndex, 0);
                    cgContext.cmd.DispatchCompute(data.copyCS, kernelIndex, Mathf.CeilToInt(sourceWidth / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(data.height / GPUIConstants.CS_THREAD_COUNT_2D), 1);

                    if (data.isVRCulling)
                    {
                        cgContext.cmd.SetComputeTextureParam(data.copyCS, kernelIndex, GPUIConstants.PROP_textureArray, data.cameraDepthHandle, 0);
                        cgContext.cmd.SetComputeTextureParam(data.copyCS, kernelIndex, GPUIConstants.PROP_destination, data.hiZTextureHandle, 0);
                        cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_offsetX, sourceWidth);
                        cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_sourceSizeX, sourceWidth);
                        cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_sourceSizeY, data.height);
                        cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_reverseZ, GPUIRuntimeSettings.Instance.ReversedZBuffer ? 1 : 0);
                        cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_textureArrayIndex, 1);
                        cgContext.cmd.DispatchCompute(data.copyCS, kernelIndex, Mathf.CeilToInt(sourceWidth / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(data.height / GPUIConstants.CS_THREAD_COUNT_2D), 1);
                    }
                }
                else
                {
                    if (data.eyeIndex == 0)
                    {
                        //Debug.Log(Time.frameCount + " left eye");
                        cgContext.cmd.SetComputeTextureParam(data.copyCS, kernelIndex, GPUIConstants.PROP_source, data.cameraDepthHandle, 0);
                        cgContext.cmd.SetComputeTextureParam(data.copyCS, kernelIndex, GPUIConstants.PROP_destination, data.hiZTextureHandle, 0);
                        cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_offsetX, 0);
                        cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_sourceSizeX, sourceWidth);
                        cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_sourceSizeY, data.height);
                        cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_reverseZ, GPUIRuntimeSettings.Instance.ReversedZBuffer ? 1 : 0);
                        cgContext.cmd.DispatchCompute(data.copyCS, kernelIndex, Mathf.CeilToInt(sourceWidth / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(data.height / GPUIConstants.CS_THREAD_COUNT_2D), 1);
                    }
                    else if (data.isVRCulling)
                    {
                        //Debug.Log(Time.frameCount + " right eye");
                        cgContext.cmd.SetComputeTextureParam(data.copyCS, kernelIndex, GPUIConstants.PROP_source, data.cameraDepthHandle, 0);
                        cgContext.cmd.SetComputeTextureParam(data.copyCS, kernelIndex, GPUIConstants.PROP_destination, data.hiZTextureHandle, 0);
                        cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_offsetX, sourceWidth);
                        cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_sourceSizeX, sourceWidth);
                        cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_sourceSizeY, data.height);
                        cgContext.cmd.SetComputeIntParam(data.copyCS, GPUIConstants.PROP_reverseZ, GPUIRuntimeSettings.Instance.ReversedZBuffer ? 1 : 0);
                        cgContext.cmd.DispatchCompute(data.copyCS, kernelIndex, Mathf.CeilToInt(sourceWidth / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(data.height / GPUIConstants.CS_THREAD_COUNT_2D), 1);
                    }
                }
            }

            static void ReducePass(PassData data, ComputeGraphContext cgContext)
            {
                int kernelIndex = 0;

                int sourceW = data.width;
                int sourceH = data.height;
                int destinationW = sourceW >> 1;
                int destinationH = sourceH >> 1;
                for (int mip = 0; mip < data.hiZMipLevels - 1; mip++)
                {
                    if (destinationW == 0 || destinationH == 0) break;

                    cgContext.cmd.SetComputeTextureParam(data.reduceCS, kernelIndex, GPUIConstants.PROP_source, data.hiZTextureHandle, mip);
                    cgContext.cmd.SetComputeTextureParam(data.reduceCS, kernelIndex, GPUIConstants.PROP_destination, data.hiZTextureHandle, mip + 1);
                    cgContext.cmd.SetComputeIntParam(data.reduceCS, GPUIConstants.PROP_offsetX, 0);
                    cgContext.cmd.SetComputeIntParam(data.reduceCS, GPUIConstants.PROP_sourceSizeX, sourceW);
                    cgContext.cmd.SetComputeIntParam(data.reduceCS, GPUIConstants.PROP_sourceSizeY, sourceH);
                    cgContext.cmd.SetComputeIntParam(data.reduceCS, GPUIConstants.PROP_destinationSizeX, destinationW);
                    cgContext.cmd.SetComputeIntParam(data.reduceCS, GPUIConstants.PROP_destinationSizeY, destinationH);
                    cgContext.cmd.DispatchCompute(data.reduceCS, kernelIndex, Mathf.CeilToInt(destinationW / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(destinationH / GPUIConstants.CS_THREAD_COUNT_2D), 1);

                    sourceW = destinationW;
                    sourceH = destinationH;
                    destinationW >>= 1;
                    destinationH >>= 1;
                }
            }

            public void Dispose()
            {
                _renderTextureHandle.Release();
                IsSetup = false;
            }
        }
#endif

        #endregion URP Scriptable Render Pass Mode
    }
}