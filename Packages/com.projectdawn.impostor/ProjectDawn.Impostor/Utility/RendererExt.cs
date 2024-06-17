using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ProjectDawn.Impostor
{
    public static class RendererExt
    {
        public static Mesh GetSharedMesh(this Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                var transform = skinnedMeshRenderer.transform;
                return skinnedMeshRenderer.sharedMesh;
            }
            else if (renderer is MeshRenderer meshRenderer)
            {
                var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                return meshFilter.sharedMesh;
            }
            else
            {
                throw new System.InvalidOperationException($"Renderer with type {renderer.GetType()} is not supported!");
            }
        }
    }
}