using System;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using FidelityFX;
using UnityEngine.Profiling;

namespace TND.FSR
{
    public class FSR3RenderPass : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        [HideInInspector]
        public BoolParameter enable = new BoolParameter(false);
        public bool IsActive() => enable.value;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

        private readonly int depthTexturePropertyID = Shader.PropertyToID("_CameraDepthTexture");
        private readonly int motionTexturePropertyID = Shader.PropertyToID("_CameraMotionVectorsTexture");
        Texture depthTexture;

        private FSR3_HDRP m_upscaler;
        private FSR3_Quality currentQuality;

        public override void Setup()
        {
        }


        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            if (!IsActive())
            {
                cmd.Blit(source, destination, 0, 0);
                return;
            }

            if (m_upscaler == null && !camera.camera.TryGetComponent(out m_upscaler))
            {
                cmd.Blit(source, destination, 0, 0);
                return;
            }

            if (camera.camera.cameraType != CameraType.Game)
            {
                cmd.Blit(source, destination, 0, 0);
                return;
            }

            if (currentQuality != m_upscaler.FSRQuality)
            {
                cmd.Blit(source, destination, 0, 0);
                currentQuality = m_upscaler.FSRQuality;
                return;
            }

            depthTexture = Shader.GetGlobalTexture(depthTexturePropertyID);
            m_upscaler.m_dispatchDescription.Color = new ResourceView(source.rt);
            m_upscaler.m_dispatchDescription.Depth = new ResourceView(depthTexture);
            m_upscaler.m_dispatchDescription.MotionVectors = new ResourceView(Shader.GetGlobalTexture(motionTexturePropertyID));
            m_upscaler.m_dispatchDescription.Output = new ResourceView(destination.rt);

            //This is needed because HDRP is potentially missing depth and motion vectors the first rendering of the camera.
            if (m_upscaler.m_skipFirstFrame)
            {
                m_upscaler.m_skipFirstFrame = false;
                m_upscaler.m_dispatchDescription.Depth = new ResourceView(source.rt);
                m_upscaler.m_dispatchDescription.MotionVectors = new ResourceView(source.rt);
            }

            if (m_upscaler.m_dispatchDescription.Color.IsValid && m_upscaler.m_dispatchDescription.Depth.IsValid && m_upscaler.m_dispatchDescription.MotionVectors.IsValid && m_upscaler.m_dispatchDescription.MotionVectorScale.x != 0)
            {
                if (m_upscaler.generateReactiveMask)
                {
                    m_upscaler.m_genReactiveDescription.OutReactive = new ResourceView(m_upscaler.m_reactiveMaskOutput);
                    m_upscaler.m_context.GenerateReactiveMask(m_upscaler.m_genReactiveDescription, cmd);
                }
                m_upscaler.m_context.Dispatch(m_upscaler.m_dispatchDescription, cmd);
            }
        }

        public override void Cleanup()
        {
        }
    }
}
