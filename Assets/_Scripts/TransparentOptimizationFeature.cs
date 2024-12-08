using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TransparentOptimizationFeature : ScriptableRendererFeature
{
    class TransparentRenderPass : ScriptableRenderPass
    {
        private RTHandle m_TransparentColor;
        private RTHandle m_TransparentDepth;
        private ProfilingSampler m_ProfilingSampler;
        
        public TransparentRenderPass()
        {
            m_ProfilingSampler = new ProfilingSampler("Transparent Optimization Pass");
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        [System.Obsolete("Use RenderGraph API instead")]
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var colorDesc = cameraTextureDescriptor;
            colorDesc.depthBufferBits = 0;
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_TransparentColor, colorDesc, name: "_TransparentColor");
            
            var depthDesc = cameraTextureDescriptor;
            depthDesc.colorFormat = RenderTextureFormat.Depth;
            depthDesc.depthBufferBits = 32;
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_TransparentDepth, depthDesc, name: "_TransparentDepth");
            
            ConfigureTarget(m_TransparentColor, m_TransparentDepth);
            ConfigureClear(ClearFlag.All, Color.clear);
        }

        [System.Obsolete("Use RenderGraph API instead")]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                var sortingSettings = new SortingSettings(renderingData.cameraData.camera)
                {
                    criteria = SortingCriteria.CommonTransparent | SortingCriteria.QuantizedFrontToBack
                };

                var drawingSettings = CreateDrawingSettings(
                    new ShaderTagId("UniversalForward"), 
                    ref renderingData, 
                    sortingSettings.criteria);

                var filteringSettings = new FilteringSettings(RenderQueueRange.transparent);

                // Use DrawRenderers directly since we're in compatibility mode
                context.DrawRenderers(
                    renderingData.cullResults,
                    ref drawingSettings,
                    ref filteringSettings);
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (m_TransparentColor != null)
                m_TransparentColor.Release();
            if (m_TransparentDepth != null)
                m_TransparentDepth.Release();
        }
    }

    private TransparentRenderPass m_RenderPass;

    public override void Create()
    {
        m_RenderPass = new TransparentRenderPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_RenderPass);
    }
}
