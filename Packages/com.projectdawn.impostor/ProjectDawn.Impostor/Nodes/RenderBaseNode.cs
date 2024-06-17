
using GraphProcessor;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Scene/Render Base Color")]
    public class RenderBaseNode : ImpostorNode
    {
        public override string name => "Render Base Color";

        [Input]
        public RenderTexture Source;
        [Input]
        public Scene Scene;
        [Output(allowMultiple = false)]
        public RenderTexture Destination;

        protected override void Process()
        {
            Scene.Render(Source, RenderMode.BaseColor);
            Destination = Source;
        }
    }
}