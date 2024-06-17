
using GraphProcessor;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Scene/Render Smoothness")]
    public class RenderSmoothnessNode : ImpostorNode
    {
        public override string name => "Render Smoothness";

        [Input]
        public RenderTexture Source;
        [Input]
        public Scene Scene;
        [Output(allowMultiple = false)]
        public RenderTexture Destination;

        protected override void Process()
        {
            Scene.Render(Source, RenderMode.Smoothness);
            Destination = Source;
        }
    }
}