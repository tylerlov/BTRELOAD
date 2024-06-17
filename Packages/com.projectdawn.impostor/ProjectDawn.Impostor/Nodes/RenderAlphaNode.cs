
using GraphProcessor;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Scene/Render Alpha")]
    public class RenderAlphaNode : ImpostorNode
    {
        public override string name => "Render Alpha";

        [Input]
        public RenderTexture Source;
        [Input]
        public Scene Scene;
        [Output(allowMultiple = false)]
        public RenderTexture Destination;

        protected override void Process()
        {
            Scene.Render(Source, RenderMode.Alpha);
            Destination = Source;
        }
    }
}