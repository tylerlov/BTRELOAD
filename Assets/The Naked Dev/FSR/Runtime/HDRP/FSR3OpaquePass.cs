using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace TND.FSR
{
    public class FSR3OpaquePass : CustomPass
    {
        public FSR3_HDRP m_hdrp;
        private readonly Vector2 vector2Zero = new Vector2Int(0, 0);

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) {
        }

        protected override void Execute(CustomPassContext ctx) {
            if(m_hdrp == null) {
                m_hdrp = ctx.hdCamera.camera.GetComponent<FSR3_HDRP>();
                if(m_hdrp == null) {
                    return;
                }
            }
            if(ctx.hdCamera.camera.cameraType != CameraType.Game) {
                return;
            }

            ctx.cmd.Blit(ctx.cameraColorBuffer, m_hdrp.m_opaqueOnlyColorBuffer, ctx.cameraColorBuffer.rtHandleProperties.rtHandleScale, vector2Zero, 0, 0);
            m_hdrp.m_genReactiveDescription.ColorOpaqueOnly = new FidelityFX.ResourceView(m_hdrp.m_opaqueOnlyColorBuffer);
        }

        protected override void Cleanup() {
        }
    }
}
