using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// TODO: Remove for URP 13.
// https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@13.1/manual/upgrade-guide-2022-1.html
#pragma warning disable CS0618

public class FlatKitDepthNormals : ScriptableRendererFeature {
    public bool overrideRenderEvent;
    public RenderPassEvent renderEvent = RenderPassEvent.AfterRenderingTransparents;

    class DepthNormalsPass : ScriptableRenderPass {
        private RTHandle _depthAttachmentHandle;
        private RenderTextureDescriptor _descriptor;
        private FilteringSettings _filteringSettings;
        private readonly Material _depthNormalsMaterial;
        private readonly string _profilerTag = "[Flat Kit] Depth Normals Pass";
        private readonly ShaderTagId _shaderTagId = new ShaderTagId("DepthOnly");
        private readonly int _depthBufferBits = 32;

        public DepthNormalsPass(RenderQueueRange renderQueueRange, LayerMask layerMask, Material material) {
            _filteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            _depthNormalsMaterial = material;
        }

        public void Setup(RenderTextureDescriptor baseDescriptor, RTHandle depthRTHandle) {
            this._depthAttachmentHandle = depthRTHandle;
            baseDescriptor.colorFormat = RenderTextureFormat.ARGB32;
            baseDescriptor.depthBufferBits = _depthBufferBits;
            _descriptor = baseDescriptor;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            cmd.GetTemporaryRT(Shader.PropertyToID(_depthAttachmentHandle.name), _descriptor, FilterMode.Point);
            ConfigureTarget(_depthAttachmentHandle);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

            using (new ProfilingScope(cmd, new ProfilingSampler(_profilerTag))) {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(_shaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;
                if (cameraData.xr.enabled) {
                    context.StartMultiEye(camera);
                }

                drawSettings.overrideMaterial = _depthNormalsMaterial;

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _filteringSettings);

                cmd.SetGlobalTexture("_CameraDepthNormalsTexture", _depthAttachmentHandle.nameID);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd) {
            if (_depthAttachmentHandle == null) return;
            _depthAttachmentHandle.Release();
        }
    }

    DepthNormalsPass _depthNormalsPass;
    RTHandle _depthNormalsTexture;
    Material _depthNormalsMaterial;

    public override void Create() {
        _depthNormalsMaterial = CoreUtils.CreateEngineMaterial("Hidden/Internal-DepthNormalsTexture");
        _depthNormalsPass = new DepthNormalsPass(RenderQueueRange.all, -1, _depthNormalsMaterial) {
            renderPassEvent = overrideRenderEvent? renderEvent : RenderPassEvent.AfterRenderingTransparents
        };
        _depthNormalsTexture = RTHandles.Alloc("_CameraDepthNormalsTexture", name: "_CameraDepthNormalsTexture");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        _depthNormalsPass.Setup(renderingData.cameraData.cameraTargetDescriptor, _depthNormalsTexture);
        renderer.EnqueuePass(_depthNormalsPass);
    }

    protected override void Dispose(bool disposing) {
        _depthNormalsTexture?.Release();
        CoreUtils.Destroy(_depthNormalsMaterial);
    }
}
