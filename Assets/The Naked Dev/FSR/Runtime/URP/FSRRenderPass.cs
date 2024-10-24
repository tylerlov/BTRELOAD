using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using UnityEngine.UIElements;


#if UNITY_2023_3_OR_NEWER
using System;
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace TND.FSR
{
    public class FSRRenderPass : ScriptableRenderPass
    {
        private FSR3_URP m_upscaler;
        private const string blitPass = "[FSR 3] Upscaler";

        //Legacy
        private Vector4 _scaleBias;

        public FSRRenderPass(FSR3_URP _upscaler, bool usingRenderGraph)
        {
            m_upscaler = _upscaler;
            renderPassEvent = usingRenderGraph ? RenderPassEvent.AfterRenderingPostProcessing : RenderPassEvent.AfterRendering + 2;

            _scaleBias = SystemInfo.graphicsUVStartsAtTop ? new Vector4(1, -1, 0, 1) : Vector4.one;
        }

        #region Unity 6

#if UNITY_2023_3_OR_NEWER
        private class PassData
        {
            public TextureHandle Source;
            public TextureHandle Depth;
            public TextureHandle MotionVector;
            public TextureHandle Destination;
            public Rect PixelRect;
        }

        private int multipassId = 0;
        private const string _upscaledTextureName = "_FSR3_UpscaledTexture";

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Setting up the render pass in RenderGraph
            using (var builder = renderGraph.AddUnsafePass<PassData>(blitPass, out var passData))
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();


                RenderTextureDescriptor upscaledDesc = cameraData.cameraTargetDescriptor;
                upscaledDesc.depthBufferBits = 0;
                upscaledDesc.width = m_upscaler.m_displayWidth;
                upscaledDesc.height = m_upscaler.m_displayHeight;

                TextureHandle upscaled = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,
                    upscaledDesc,
                    _upscaledTextureName,
                    false
                );

                passData.Source = resourceData.activeColorTexture;
                passData.Depth = resourceData.activeDepthTexture;
                passData.MotionVector = resourceData.motionVectorColor;
                passData.Destination = upscaled;
                passData.PixelRect = cameraData.camera.pixelRect;

                builder.UseTexture(passData.Source, AccessFlags.Read);
                builder.UseTexture(passData.Depth, AccessFlags.Read);
                builder.UseTexture(passData.MotionVector, AccessFlags.Read);
                builder.UseTexture(passData.Destination, AccessFlags.Write);

                builder.AllowPassCulling(false);

                resourceData.cameraColor = upscaled;
                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
            }
        }

        private void ExecutePass(PassData data, UnsafeGraphContext context)
        {
            CommandBuffer unsafeCmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

            //Stereo
            if (XRSettings.enabled)
            {
                multipassId++;
                if (multipassId >= 2)
                {
                    multipassId = 0;
                }
            }
            m_upscaler.m_dispatchDescription.Color = new FidelityFX.ResourceView(data.Source, RenderTextureSubElement.Color);
            m_upscaler.m_dispatchDescription.Depth = new FidelityFX.ResourceView(data.Depth);
            m_upscaler.m_dispatchDescription.MotionVectors = new FidelityFX.ResourceView(data.MotionVector);

            if (m_upscaler.generateReactiveMask)
            {
                m_upscaler.m_context[multipassId].GenerateReactiveMask(m_upscaler.m_genReactiveDescription, unsafeCmd);
            }
            m_upscaler.m_context[multipassId].Dispatch(m_upscaler.m_dispatchDescription, unsafeCmd);

            CoreUtils.SetRenderTarget(unsafeCmd, data.Destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.None, Color.clear);
            unsafeCmd.SetViewport(data.PixelRect);

            Blitter.BlitTexture(unsafeCmd, m_upscaler.m_fsrOutput, new Vector4(1, 1, 0, 0), 0, false);
        }

#endif
        #endregion

        #region Unity Legacy
#if UNITY_2023_3_OR_NEWER
        [Obsolete]
#endif

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            try
            {
                CommandBuffer cmd = CommandBufferPool.Get(blitPass);

                CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.None, Color.clear);
                cmd.SetViewport(renderingData.cameraData.camera.pixelRect);
                if (renderingData.cameraData.camera.targetTexture != null)
                {
                    _scaleBias = Vector2.one;
                }
                Blitter.BlitTexture(cmd, m_upscaler.m_fsrOutput, _scaleBias, 0, false);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
            catch { }
        }
    }

    #endregion

    public class FSRBufferPass : ScriptableRenderPass
    {
        private FSR3_URP m_upscaler;

        private int multipassId = 0;
        private const string blitPass = "[FSR 3] Upscaler";

        private readonly int depthTexturePropertyID = Shader.PropertyToID("_CameraDepthTexture");
        private readonly int motionTexturePropertyID = Shader.PropertyToID("_MotionVectorTexture");

        public FSRBufferPass(FSR3_URP _upscaler, bool usingRenderGraph)
        {
            m_upscaler = _upscaler;

            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

#if UNITY_2023_3_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(blitPass);

#if UNITY_2022_1_OR_NEWER
            m_upscaler.m_dispatchDescription.Color = new FidelityFX.ResourceView(renderingData.cameraData.renderer.cameraColorTargetHandle, RenderTextureSubElement.Color);
#else
            m_upscaler.m_dispatchDescription.Color = new FidelityFX.ResourceView(renderingData.cameraData.renderer.cameraColorTarget, RenderTextureSubElement.Color);
#endif
            m_upscaler.m_dispatchDescription.Depth = new FidelityFX.ResourceView(Shader.GetGlobalTexture(depthTexturePropertyID));

            m_upscaler.m_dispatchDescription.MotionVectors = new FidelityFX.ResourceView(Shader.GetGlobalTexture(motionTexturePropertyID));
            try
            {
                m_upscaler.m_dispatchDescription.DepthFormat = Shader.GetGlobalTexture(depthTexturePropertyID).graphicsFormat == UnityEngine.Experimental.Rendering.GraphicsFormat.None;
            }
            catch
            {
                m_upscaler.m_dispatchDescription.DepthFormat = true;
            }

            //Stereo
            if (XRSettings.enabled)
            {
                multipassId++;
                if (multipassId >= 2)
                {
                    multipassId = 0;
                }
            }

            if (m_upscaler.generateReactiveMask)
            {
                m_upscaler.m_context[multipassId].GenerateReactiveMask(m_upscaler.m_genReactiveDescription, cmd);
            }

            m_upscaler.m_context[multipassId].Dispatch(m_upscaler.m_dispatchDescription, cmd);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public class FSROpaqueOnlyPass : ScriptableRenderPass
    {
        private FSR3_URP m_upscaler;

        public FSROpaqueOnlyPass(FSR3_URP _upscaler, bool usingRenderGraph)
        {
            m_upscaler = _upscaler;

            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        #region Unity 6
#if UNITY_2023_3_OR_NEWER

        private class PassData
        {
            public TextureHandle Source;
            public Rect PixelRect;
        }
        private const string blitPass = "[FSR 3] Opaque Pass";

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Setting up the render pass in RenderGraph
            using (var builder = renderGraph.AddUnsafePass<PassData>(blitPass, out var passData))
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();


                passData.Source = resourceData.activeColorTexture;
                passData.PixelRect = cameraData.camera.pixelRect;

                builder.UseTexture(passData.Source);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
            }
        }

        private void ExecutePass(PassData data, UnsafeGraphContext context)
        {
            CommandBuffer unsafeCmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
            CoreUtils.SetRenderTarget(unsafeCmd, m_upscaler.m_opaqueOnlyColorBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.None, Color.clear);
            Blitter.BlitTexture(unsafeCmd, data.Source, new Vector4(1, 1, 0, 0), 0, false);
        }
#endif

        #endregion

        #region Unity Legacy

#if UNITY_2023_3_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

#if UNITY_2022_1_OR_NEWER
            Blit(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, m_upscaler.m_opaqueOnlyColorBuffer);
#else
      If you       Blit(cmd, renderingData.cameraData.renderer.cameraColorTarget, m_upscaler.m_opaqueOnlyColorBuffer);
#endif

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        #endregion
    }

    public class FSRTransparentPass : ScriptableRenderPass
    {
        private FSR3_URP m_upscaler;

        public FSRTransparentPass(FSR3_URP _upscaler, bool usingRenderGraph)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            m_upscaler = _upscaler;
        }

        #region Unity 6
#if UNITY_2023_3_OR_NEWER

        private class PassData
        {
            public TextureHandle Source;
            public Rect PixelRect;
        }
        private const string blitPass = "[FSR 3] Transparent Pass";

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Setting up the render pass in RenderGraph
            using (var builder = renderGraph.AddUnsafePass<PassData>(blitPass, out var passData))
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();

                passData.Source = resourceData.activeColorTexture;
                passData.PixelRect = cameraData.camera.pixelRect;

                builder.UseTexture(passData.Source);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
            }
        }

        private void ExecutePass(PassData data, UnsafeGraphContext context)
        {
            CommandBuffer unsafeCmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
            CoreUtils.SetRenderTarget(unsafeCmd, m_upscaler.m_afterOpaqueOnlyColorBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.None, Color.clear);
            Blitter.BlitTexture(unsafeCmd, data.Source, new Vector4(1, 1, 0, 0), 0, false);
        }
#endif

        #endregion

        #region Unity Legacy
#if UNITY_2023_3_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

#if UNITY_2022_1_OR_NEWER
            Blit(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, m_upscaler.m_afterOpaqueOnlyColorBuffer);
#else
            Blit(cmd, renderingData.cameraData.renderer.cameraColorTarget, m_upscaler.m_afterOpaqueOnlyColorBuffer);
#endif

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        #endregion
    }
}
