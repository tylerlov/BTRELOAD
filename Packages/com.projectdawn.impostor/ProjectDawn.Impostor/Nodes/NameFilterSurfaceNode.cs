
using GraphProcessor;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Name Filter Surface")]
    public class NameFilterSurfaceNode : ImpostorNode
    {
        public override string name => "Name Filter Surface";

        [Input]
        public Surface Input;
        [Output]
        public Surface Output;
        public List<string> Names = new();
        public bool MatchCase = true;
        public bool MatchWholeWord = true;

        protected override void Process()
        {
            var stringComparison = MatchCase ? System.StringComparison.Ordinal : System.StringComparison.OrdinalIgnoreCase;

            Output = Input.Clone((Renderer renderer) =>
            {
                if (MatchWholeWord)
                {
                    foreach (var name in Names)
                    {
                        if (renderer.name.Equals(name, stringComparison))
                            return false;
                    }
                }
                else
                {
                    foreach (var name in Names)
                    {
                        if (renderer.name.Contains(name, stringComparison))
                            return false;
                    }
                }

                return true;
            });
        }
    }
}