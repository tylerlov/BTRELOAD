using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

#if UNITY_6000_0_OR_NEWER
using System;
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace TND.FSR
{
    public class FSRRenderPass : ScriptableRenderPass
    {
        private CommandBuffer cmd;

        private FSR3_URP m_fsrURP;
        private readonly Vector4 flipVector = new Vector4(1, -1, 0, 1);
        private int multipassId = 0;

        public FSRRenderPass(FSR3_URP _fsrURP)
        {
            renderPassEvent = RenderPassEvent.AfterRendering + 2;
            m_fsrURP = _fsrURP;
        }

        public void OnSetReference(FSR3_URP _fsrURP)
        {
            m_fsrURP = _fsrURP;
        }

#if UNITY_6000_0_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            try
            {
                //Stereo
                if (XRSettings.enabled)
                {
                    multipassId++;
                    if (multipassId >= 2)
                    {
                        multipassId = 0;
                    }
                }

                cmd = CommandBufferPool.Get();

                if (m_fsrURP.generateReactiveMask)
                {
                    m_fsrURP.m_context[multipassId].GenerateReactiveMask(m_fsrURP.m_genReactiveDescription, cmd);
                }
                m_fsrURP.m_context[multipassId].Dispatch(m_fsrURP.m_dispatchDescription, cmd);

#if UNITY_2022_1_OR_NEWER
                Blitter.BlitCameraTexture(cmd, m_fsrURP.m_fsrOutput, renderingData.cameraData.renderer.cameraColorTargetHandle, flipVector, 0, false);
#else
                Blit(cmd, m_fsrURP.m_fsrOutput, renderingData.cameraData.renderer.cameraColorTarget);
#endif

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

            }
            catch { }
        }
    }

    public class FSRBufferPass : ScriptableRenderPass
    {
        private FSR3_URP m_fsrURP;

#if !UNITY_2022_1_OR_NEWER
        private readonly int depthTexturePropertyID = Shader.PropertyToID("_CameraDepthTexture");
#endif
        private readonly int motionTexturePropertyID = Shader.PropertyToID("_MotionVectorTexture");

        public FSRBufferPass(FSR3_URP _fsrURP)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            ConfigureInput(ScriptableRenderPassInput.Depth);
            m_fsrURP = _fsrURP;
        }

#if UNITY_2022_1_OR_NEWER
#if UNITY_6000_0_OR_NEWER
        [Obsolete]
#endif
        public void Setup(ScriptableRenderer renderer)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            if (m_fsrURP == null)
            {
                return;
            }

            m_fsrURP.m_dispatchDescription.Color = new FidelityFX.ResourceView(renderer.cameraColorTargetHandle, RenderTextureSubElement.Color);
            m_fsrURP.m_dispatchDescription.Depth = new FidelityFX.ResourceView(renderer.cameraDepthTargetHandle, RenderTextureSubElement.Depth);
            m_fsrURP.m_dispatchDescription.MotionVectors = new FidelityFX.ResourceView(Shader.GetGlobalTexture(motionTexturePropertyID));
        }
#endif

        public void OnSetReference(FSR3_URP _fsrURP)
        {
            m_fsrURP = _fsrURP;
        }

#if UNITY_6000_0_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
#if UNITY_2022_1_OR_NEWER
            m_fsrURP.m_dispatchDescription.Color = new FidelityFX.ResourceView(renderingData.cameraData.renderer.cameraColorTargetHandle, RenderTextureSubElement.Color);
#else
            m_fsrURP.m_dispatchDescription.Color = new FidelityFX.ResourceView(renderingData.cameraData.renderer.cameraColorTarget, RenderTextureSubElement.Color);
            m_fsrURP.m_dispatchDescription.Depth = new FidelityFX.ResourceView(Shader.GetGlobalTexture(depthTexturePropertyID), RenderTextureSubElement.Depth);
            m_fsrURP.m_dispatchDescription.MotionVectors = new FidelityFX.ResourceView(Shader.GetGlobalTexture(motionTexturePropertyID));

            try
            {
                m_fsrURP.m_dispatchDescription.DepthFormat = Shader.GetGlobalTexture(depthTexturePropertyID).graphicsFormat == UnityEngine.Experimental.Rendering.GraphicsFormat.None;
            }
            catch
            {
                m_fsrURP.m_dispatchDescription.DepthFormat = true;
            }
#endif
        }
    }

    public class FSROpaqueOnlyPass : ScriptableRenderPass
    {
        private CommandBuffer cmd;
        private FSR3_URP m_fsrURP;

        public FSROpaqueOnlyPass(FSR3_URP _fsrURP)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            m_fsrURP = _fsrURP;
        }

        public void OnSetReference(FSR3_URP _fsrURP)
        {
            m_fsrURP = _fsrURP;
        }

#if UNITY_6000_0_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            cmd = CommandBufferPool.Get();

#if UNITY_2022_1_OR_NEWER
            Blit(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, m_fsrURP.m_opaqueOnlyColorBuffer);
#else
            Blit(cmd, renderingData.cameraData.renderer.cameraColorTarget, m_fsrURP.m_opaqueOnlyColorBuffer);
#endif

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public class FSRTransparentPass : ScriptableRenderPass
    {
        private CommandBuffer cmd;
        private FSR3_URP m_fsrURP;

        public FSRTransparentPass(FSR3_URP _fsrURP)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            m_fsrURP = _fsrURP;
        }

        public void OnSetReference(FSR3_URP _fsrURP)
        {
            m_fsrURP = _fsrURP;
        }

#if UNITY_6000_0_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            cmd = CommandBufferPool.Get();

#if UNITY_2022_1_OR_NEWER
            Blit(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, m_fsrURP.m_afterOpaqueOnlyColorBuffer);
#else
            Blit(cmd, renderingData.cameraData.renderer.cameraColorTarget, m_fsrURP.m_afterOpaqueOnlyColorBuffer);
#endif

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
