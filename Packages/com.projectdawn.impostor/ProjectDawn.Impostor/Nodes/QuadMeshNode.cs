
using GraphProcessor;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Mesh/Quad Mesh")]
    public class QuadMeshNode : ImpostorNode
    {
        public override string name => "Quad Mesh";

        [Input]
        public CapturePoints CapturePoints;
        [Output]
        public Mesh Mesh;

        protected override void Process()
        {
            var bounds = CapturePoints.Bounds;
            Mesh = CreateQuad();
            Mesh.bounds = new Bounds(bounds.position, Vector3.one * bounds.radius * 2);
            Mesh.name = "Quad";
        }

        static Mesh CreateQuad()
        {
            var mesh = new Mesh();

            var vertices = new Vector3[]
            {
                new Vector3(0.0f, 0.0f, 0),
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(0.5f, 0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0)
            };
            mesh.vertices = vertices;

            var uvs = new Vector2[]
            {
                new Vector2(0.5f, 0.5f),
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(0.0f, 1.0f)
            };
            mesh.uv = uvs;

            var indicies = new int[]
            {
                2, 1, 0,
                3, 2, 0,
                4, 3, 0,
                1, 4, 0
            };
            mesh.SetIndices(indicies, MeshTopology.Triangles, 0);

            return mesh;
        }
    }
}