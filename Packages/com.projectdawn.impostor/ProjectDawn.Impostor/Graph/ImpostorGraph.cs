using GraphProcessor;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [HelpURL("https://lukaschod.github.io/impostor-graph-docs/manual/graph.html")]
    [CreateAssetMenu(fileName = "New Impostor Graph", menuName = "Impostor Graph", order = 150)]
    /// <summary>
    /// Represents a serialized graph consisting of nodes, with functionality for executing the graph and producing impostor assets as output.
    /// </summary>
    public class ImpostorGraph : BaseGraph
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            var output = FindNodeInGraphOfType<OutputNode>(this);
            if (output == null)
            {
                output = BaseNode.CreateFromType<OutputNode>(new Vector3(400, 400, 0));
                AddNode(output);
            }
        }

        static T FindNodeInGraphOfType<T>(BaseGraph graph) where T : BaseNode
        {
            return graph.nodes.Find(x => x is T) as T;
        }
    }
}
