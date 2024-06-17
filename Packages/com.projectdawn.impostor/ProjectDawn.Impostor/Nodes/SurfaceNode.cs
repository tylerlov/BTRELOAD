
using GraphProcessor;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Surface")]
    public class SurfaceNode : ImpostorNode
    {
        public override string name => "Surface";

        [Input]
        public GameObject Source;
        [Output]
        public Surface Surface;

        protected override void Process()
        {
            Surface = new Surface(Source);
        }
    }
}