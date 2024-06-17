
using GraphProcessor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Texture/Blit")]
    public class BlitMaskNode : ImpostorNode
    {
        public override string name => "Blit";

        [Input]
        public RenderTexture Source;
        [Input]
        public RenderTexture Destination;
        [Output]
        public RenderTexture Combined;
        [Reload("Packages/com.projectdawn.impostor/Shaders/Blit.mat")]
        public Material BlitMaskMaterial;
        public Packing Packing = Packing.RGBA;

        protected override void Process()
        {
            var cmd = CommandBufferPool.Get();
            BlitMask.Execute(cmd, Source, Destination, Packing, BlitMaskMaterial);
            Combined = Destination;
            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}