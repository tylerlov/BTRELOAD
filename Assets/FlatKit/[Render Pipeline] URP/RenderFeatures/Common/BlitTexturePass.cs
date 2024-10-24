using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// TODO: Remove for URP 13.
// https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@13.1/manual/upgrade-guide-2022-1.html
#pragma warning disable CS0618

namespace FlatKit {
internal class BlitTexturePass : ScriptableRenderPass {
    public static readonly string CopyEffectShaderName = "Hidden/FlatKit/CopyTexture";

    private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Copy Texture");
    private readonly Material _effectMaterial;
    private readonly Material _copyMaterial;
    private RTHandle _temporaryColorTexture;

    public BlitTexturePass(Material effectMaterial, Material copyMaterial) {
        _effectMaterial = effectMaterial;
        _copyMaterial = CoreUtils.CreateEngineMaterial(CopyEffectShaderName);
    }

    public void Setup(bool useDepth, bool useNormals, bool useColor) {
#if UNITY_2020_3_OR_NEWER
        ConfigureInput(
            (useColor ? ScriptableRenderPassInput.Color : ScriptableRenderPassInput.None) |
            (useDepth ? ScriptableRenderPassInput.Depth : ScriptableRenderPassInput.None) |
            (useNormals ? ScriptableRenderPassInput.Normal : ScriptableRenderPassInput.None)
        );
#endif
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
        // Replace the obsolete cameraColorTarget with cameraColorTargetHandle
        ConfigureTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        if (_effectMaterial == null) return;
        if (renderingData.cameraData.camera.cameraType != CameraType.Game) return;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, _profilingSampler)) {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            SetSourceSize(cmd, descriptor);

            // Use cameraColorTargetHandle consistently
            var cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
            _temporaryColorTexture = RTHandles.Alloc(descriptor, name: "TemporaryColorTexture");

            if (renderingData.cameraData.xrRendering) {
                _effectMaterial.EnableKeyword("_USE_DRAW_PROCEDURAL");
                cmd.SetRenderTarget(_temporaryColorTexture);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _effectMaterial, 0, 0);
                cmd.SetGlobalTexture("_EffectTexture", _temporaryColorTexture);
                cmd.SetRenderTarget(cameraTargetHandle);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _copyMaterial, 0, 0);
            } else {
                _effectMaterial.DisableKeyword("_USE_DRAW_PROCEDURAL");
                cmd.Blit(cameraTargetHandle, _temporaryColorTexture, _effectMaterial, 0);
                cmd.Blit(_temporaryColorTexture, cameraTargetHandle);
            }
        }

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        _temporaryColorTexture?.Release();
    }

    // Copied from `PostProcessUtils.cs`.
    private static void SetSourceSize(CommandBuffer cmd, RenderTextureDescriptor desc) {
        float width = desc.width;
        float height = desc.height;
        if (desc.useDynamicScale) {
            width *= ScalableBufferManager.widthScaleFactor;
            height *= ScalableBufferManager.heightScaleFactor;
        }

        cmd.SetGlobalVector("_SourceSize", new Vector4(width, height, 1.0f / width, 1.0f / height));
    }
}
}
