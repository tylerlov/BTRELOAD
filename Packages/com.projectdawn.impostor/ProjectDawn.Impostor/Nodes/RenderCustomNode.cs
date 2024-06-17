using GraphProcessor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectDawn.Impostor
{
    [HelpURL("https://lukaschod.github.io/impostor-graph-docs/manual/nodes/render-custom-node.html")]
    [NodeMenuItem("Scene/Render Custom")]
    public class RenderCustomNode : ImpostorNode
    {
        public override string name => "Render Custom";

        [Input]
        public RenderTexture Source;
        [Input]
        public Surface Surface;
        [Input]
        public CapturePoints CapturePoints;
        [Output(allowMultiple = false)]
        public RenderTexture Destination;
        public Material OverrideMaterial;

        protected override void Process()
        {
            CustomRenderMode.Render(Source, Surface, CapturePoints, OverrideMaterial);
            Destination = Source;
        }
    }
}