using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TransparentOptimizationFeature : ScriptableRendererFeature
{
    class TransparentRenderPass : ScriptableRenderPass
    {
        private RTHandle m_TransparentColor;
        private RTHandle m_TransparentDepth;
        
        public TransparentRenderPass()
        {
            profilingSampler = new ProfilingSampler("Transparent Optimization Pass");
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var colorDesc = cameraTextureDescriptor;
            colorDesc.depthBufferBits = 0;
            
            RenderingUtils.ReAllocateIfNeeded(ref m_TransparentColor, colorDesc, name: "_TransparentColor");
            
            var depthDesc = cameraTextureDescriptor;
            depthDesc.colorFormat = RenderTextureFormat.Depth;
            depthDesc.depthBufferBits = 32;
            
            RenderingUtils.ReAllocateIfNeeded(ref m_TransparentDepth, depthDesc, name: "_TransparentDepth");
            
            ConfigureTarget(m_TransparentColor, m_TransparentDepth);
            ConfigureClear(ClearFlag.All, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var sortingSettings = new SortingSettings(renderingData.cameraData.camera)
            {
                criteria = SortingCriteria.CommonTransparent | SortingCriteria.QuantizedFrontToBack
            };

            var drawingSettings = CreateDrawingSettings(
                new ShaderTagId("UniversalForward"), 
                ref renderingData, 
                sortingSettings.criteria);

            var filteringSettings = new FilteringSettings(
                RenderQueueRange.transparent);

            context.DrawRenderers(
                renderingData.cullResults,
                ref drawingSettings,
                ref filteringSettings);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            m_TransparentColor?.Release();
            m_TransparentDepth?.Release();
        }
    }

    private TransparentRenderPass m_TransparentPass;

    public override void Create()
    {
        m_TransparentPass = new TransparentRenderPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (Application.isPlaying)
        {
            renderer.EnqueuePass(m_TransparentPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        m_TransparentPass?.OnCameraCleanup(null);
    }
}
