
using GraphProcessor;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Scene/Render Metallic")]
    public class RenderMetallicNode : ImpostorNode
    {
        public override string name => "Render Metallic";

        [Input]
        public RenderTexture Source;
        [Input]
        public Scene Scene;
        [Output(allowMultiple = false)]
        public RenderTexture Destination;

        protected override void Process()
        {
            Scene.Render(Source, RenderMode.Metallic);
            Destination = Source;
        }
    }
}