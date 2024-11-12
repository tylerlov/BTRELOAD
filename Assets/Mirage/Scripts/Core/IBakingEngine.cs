using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirage.Impostors.Core
{
    public abstract class IBakingEngine
    {
        public List<Vector3> CameraPositions { get; protected set; }
        public List<Mesh> Meshes { get; protected set; }
        public List<Material[]> Materials { get; protected set; }
        public List<Matrix4x4> MeshesTransforms { get; protected set; }
        public int TextureSize { get; protected set; }

        protected void Initialize(List<Vector3> cameraPositions, List<Mesh> meshes, List<Material[]> materials, List<Matrix4x4> meshesTransforms, int textureSize)
        {
            CameraPositions = cameraPositions;
            Meshes = meshes;
            Materials = materials;
            MeshesTransforms = meshesTransforms;
            TextureSize = textureSize;
        }

        public abstract Texture2D ComputeColorMaps();
        public abstract Texture2D ComputeNormalMaps();
        public abstract Texture2D ComputeMaskMaps();

        public virtual void Cleanup()
        {

        }
    }
}
