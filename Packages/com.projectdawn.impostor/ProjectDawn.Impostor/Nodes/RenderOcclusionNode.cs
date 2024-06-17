
using GraphProcessor;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Scene/Render Occlusion")]
    public class RenderOcclusionNode : ImpostorNode
    {
        public override string name => "Render Occlusion";

        [Input]
        public RenderTexture Source;
        [Input]
        public Scene Scene;
        [Output(allowMultiple = false)]
        public RenderTexture Destination;

        protected override void Process()
        {
            Scene.Render(Source, RenderMode.Occlusion);
            Destination = Source;
        }
    }
}