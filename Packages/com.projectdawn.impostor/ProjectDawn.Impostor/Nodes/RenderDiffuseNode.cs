
using GraphProcessor;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Scene/Render Diffuse Color")]
    public class RenderDiffuseNode : ImpostorNode
    {
        public override string name => "Render Diffuse Color";

        [Input]
        public RenderTexture Source;
        [Input]
        public Scene Scene;
        [Output(allowMultiple = false)]
        public RenderTexture Destination;

        protected override void Process()
        {
            Scene.Render(Source, RenderMode.DiffuseColor);
            Destination = Source;
        }
    }
}