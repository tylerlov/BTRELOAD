using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TND.FSR
{
    //Not allowed to be in a namespace
    public class FSRScriptableRenderFeature : ScriptableRendererFeature
    {
        [HideInInspector]
        public bool IsEnabled = false;
        private bool usingRenderGraph = false;

        private FSR3_URP m_upscaler;

        private FSRBufferPass _bufferPass;
        private FSRRenderPass _renderPass;
        private FSROpaqueOnlyPass _opaqueBufferPass;
        private FSRTransparentPass _transparentBufferPass;

        private CameraData cameraData;

        public override void Create()
        {
            name = "FSRRenderFeature";
            SetupPasses();
        }

        public void OnSetReference(FSR3_URP _upscaler)
        {
            m_upscaler = _upscaler;
            SetupPasses();
        }

        private void SetupPasses()
        {
#if UNITY_2023_3_OR_NEWER
            var renderGraphSettings = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
            usingRenderGraph = !renderGraphSettings.enableRenderCompatibilityMode;
#endif

            if (!usingRenderGraph)
            {
                _bufferPass = new FSRBufferPass(m_upscaler);
                _bufferPass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Motion);
            }

            _renderPass = new FSRRenderPass(m_upscaler, usingRenderGraph);
            _renderPass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Motion);

            _opaqueBufferPass = new FSROpaqueOnlyPass(m_upscaler);
            _transparentBufferPass = new FSRTransparentPass(m_upscaler);
        }

        public void OnDispose()
        {
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            if (!IsEnabled)
            {
                return;
            }

            cameraData = renderingData.cameraData;
            if (cameraData.camera != m_upscaler.m_mainCamera)
            {
                return;
            }
            if (!cameraData.resolveFinalTarget)
            {
                return;
            }

            m_upscaler.m_autoHDR = cameraData.isHdrEnabled;

            // Here you can queue up multiple passes after each other.
            if (!usingRenderGraph)
            {
                renderer.EnqueuePass(_bufferPass);
            }

            renderer.EnqueuePass(_renderPass);
            if (m_upscaler.generateReactiveMask)
            {
                renderer.EnqueuePass(_opaqueBufferPass);
                renderer.EnqueuePass(_transparentBufferPass);
            }
        }
    }
}
