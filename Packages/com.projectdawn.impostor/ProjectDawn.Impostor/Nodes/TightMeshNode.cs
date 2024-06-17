
using GraphProcessor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ProjectDawn.Impostor
{
    [HelpURL("https://lukaschod.github.io/impostor-graph-docs/manual/nodes/tight-mesh-node.html")]
    [NodeMenuItem("Mesh/Tight Mesh")]
    public class TightMeshNode : ImpostorNode
    {
        public override string name => "Tight Mesh";

        [Input]
        public Scene Scene;
        [Input, SerializeField]
        public int Resolution = 128;
        [Output]
        public Mesh Mesh;

        [Reload("Packages/com.projectdawn.impostor/Shaders/TightMesh.mat")]
        public Material BlitMaterial;

        protected override void Process()
        {
            Mesh = CreateTightMesh(Scene, BlitMaterial, Resolution, 1);
            Mesh.name = "TightMesh";
        }

        public static Mesh CreateTightMesh(Scene scene, Material blitMaterial, int resolution, uint extrude)
        {
            var mask = RenderTexture.GetTemporary(resolution, resolution, 0, GraphicsFormat.R16G16B16A16_SFloat);

            scene.RenderCombinedMask(mask, blitMaterial);

            var maskTexture = mask.ToTexture2D(GraphicsFormat.R8G8B8A8_UNorm, FilterMode.Point);

            var mesh = CreateTightMesh(maskTexture, resolution, extrude);
            mesh.bounds = new Bounds(scene.CapturePoints.Bounds.position, Vector3.one * scene.CapturePoints.Bounds.radius * 2);

            GameObject.DestroyImmediate(maskTexture);
            RenderTexture.ReleaseTemporary(mask);

            return mesh;
        }

        static Mesh CreateTightMesh(Texture2D texture, int resolution, uint extrude)
        {
            // TODO: get rid of this, once there is API that does not involve sprite creation
            var sprite = Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution, extrude, SpriteMeshType.Tight, Vector4.zero);

            var spriteVertices = sprite.vertices;
            var vertices = new Vector3[spriteVertices.Length];
            for (int i = 0; i < spriteVertices.Length; i++)
                vertices[i] = spriteVertices[i];
            var spriteTriangles = sprite.triangles;
            var triangles = new int[spriteTriangles.Length];
            for (int i = 0; i < spriteTriangles.Length; i++)
                triangles[i] = spriteTriangles[i];

            GameObject.DestroyImmediate(sprite);

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.SetTriangles(triangles, 0);
            return mesh;
        }
    }
}