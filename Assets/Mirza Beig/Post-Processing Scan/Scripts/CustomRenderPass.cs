using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRenderPass : ScriptableRenderPass
{
    RTHandle source;
    RTHandle destination;

    CustomRenderPassFeature.CustomRenderPassSettings settings;

    public CustomRenderPass(CustomRenderPassFeature.CustomRenderPassSettings settings)
    {
        this.settings = settings; 
        renderPassEvent = settings.renderPassEvent;
    }

    // This method is called before executing the render pass.
    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in a performant manner.

    [System.Obsolete("Use ConfigureRenderGraph instead")]
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // Use cameraColorTargetHandle instead of cameraColorTarget
        source = renderingData.cameraData.renderer.cameraColorTargetHandle;
        destination = RTHandles.Alloc(renderingData.cameraData.cameraTargetDescriptor, name: "CustomRenderPassDestination");
    }

    // Here you can implement the rendering logic.
    // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
    // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
    // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.

    [System.Obsolete("Use RenderGraph API instead")]
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();

        // Use Blitter.BlitCameraTexture instead of cmd.Blit for RTHandle compatibility
        Blitter.BlitCameraTexture(cmd, source, destination, settings.material, 0);
        Blitter.BlitCameraTexture(cmd, destination, source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    // Cleanup any allocated resources that were created during the execution of this render pass.

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        if (destination != null)
            destination.Release();
    }
}
