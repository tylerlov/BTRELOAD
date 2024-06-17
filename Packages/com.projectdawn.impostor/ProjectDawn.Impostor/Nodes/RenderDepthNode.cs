
using GraphProcessor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Scene/Render Depth")]
    public class RenderDepthNode : ImpostorNode
    {
        public override string name => "Render Depth";

        [Input]
        public RenderTexture Source;
        [Input]
        public Scene Scene;
        [Output(allowMultiple = false)]
        public RenderTexture Destination;

        [Reload("Packages/com.projectdawn.impostor/Shaders/ConvertDepth.mat")]
        public Material ConvertDepthMaterial;

        protected override void Process()
        {
            var temp = RenderTexture.GetTemporary(Source.width, Source.height, 32, Source.format);

            Scene.Render(temp, RenderMode.Depth);

            // Convert depth
            ConvertDepthMaterial.SetFloat("_ImpostorRadius", Scene.CapturePoints.Radius * 2);
            ConvertDepthMaterial.SetKeyword(new LocalKeyword(ConvertDepthMaterial.shader, "NORMALIZE_DEPTH_ON"), false);
            Graphics.Blit(temp, Source, ConvertDepthMaterial);

            RenderTexture.ReleaseTemporary(temp);

            Destination = Source;
        }
    }
}