using GraphProcessor;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Scene/Render Normal")]
    public class HDRenderNormalNode : ImpostorNode
    {
        public override string name => "Render Normal";

        [Input]
        public RenderTexture Source;
        [Input]
        public Scene Scene;
        [Output(allowMultiple = false)]
        public RenderTexture Destination;

        protected override void Process()
        {
            Scene.Render(Source, RenderMode.Normal);
            Destination = Source;
        }
    }
}