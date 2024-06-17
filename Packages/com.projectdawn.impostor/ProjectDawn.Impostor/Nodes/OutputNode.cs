
using GraphProcessor;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Output")]
    public class OutputNode : BaseNode
    {
        public override string name => "Output";

        public override bool deletable => false;

        public override Color color => UnityEngine.Color.yellow;

        [Input]
        public Impostor Impostor;

        protected override void Process()
        {
        }
    }
}