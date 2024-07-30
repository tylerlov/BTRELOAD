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

        private FSR3_HDRP m_hdrp;
        private FSR_Quality currentQuality;

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

            if (m_hdrp == null && !camera.camera.TryGetComponent(out m_hdrp))
            {
                cmd.Blit(source, destination, 0, 0);
                return;
            }

            if (camera.camera.cameraType != CameraType.Game)
            {
                cmd.Blit(source, destination, 0, 0);
                return;
            }

            if (currentQuality != m_hdrp.FSRQuality)
            {
                cmd.Blit(source, destination, 0, 0);
                currentQuality = m_hdrp.FSRQuality;
                return;
            }

            depthTexture = Shader.GetGlobalTexture(depthTexturePropertyID);
            m_hdrp.m_dispatchDescription.Color = new ResourceView(source.rt);
            m_hdrp.m_dispatchDescription.Depth = new ResourceView(depthTexture);
            m_hdrp.m_dispatchDescription.MotionVectors = new ResourceView(Shader.GetGlobalTexture(motionTexturePropertyID));
            m_hdrp.m_dispatchDescription.Output = new ResourceView(destination.rt);

            //This is needed because HDRP is potentially missing depth and motion vectors the first rendering of the camera.
            if (m_hdrp.m_skipFirstFrame)
            {
                m_hdrp.m_dispatchDescription.DepthFormat = false;
                m_hdrp.m_skipFirstFrame = false;
                m_hdrp.m_dispatchDescription.Depth = new ResourceView(source.rt);
                m_hdrp.m_dispatchDescription.MotionVectors = new ResourceView(source.rt);
            }
            else if (depthTexture.graphicsFormat != UnityEngine.Experimental.Rendering.GraphicsFormat.None)
            {
                m_hdrp.m_dispatchDescription.DepthFormat = false;
            }
            else
            {
                m_hdrp.m_dispatchDescription.DepthFormat = true;
            }

            if (m_hdrp.m_dispatchDescription.Color.IsValid && m_hdrp.m_dispatchDescription.Depth.IsValid && m_hdrp.m_dispatchDescription.MotionVectors.IsValid && m_hdrp.m_dispatchDescription.MotionVectorScale.x != 0)
            {
                if (m_hdrp.generateReactiveMask)
                {
                    m_hdrp.m_genReactiveDescription.OutReactive = new ResourceView(m_hdrp.m_reactiveMaskOutput);
                    //m_hdrp.m_dispatchDescription.Reactive = ResourceView.Unassigned;// new ResourceView(m_hdrp.m_reactiveMaskOutput);
                    m_hdrp.m_context.GenerateReactiveMask(m_hdrp.m_genReactiveDescription, cmd);
                }
                m_hdrp.m_context.Dispatch(m_hdrp.m_dispatchDescription, cmd);
            }
        }

        public override void Cleanup()
        {
        }
    }
}
