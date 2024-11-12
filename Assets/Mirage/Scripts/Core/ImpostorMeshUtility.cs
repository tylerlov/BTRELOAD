/*
 * Copyright (c) Léo CHAUMARTIN 2021-2024
 * All Rights Reserved
 * 
 * File: ImpostorMeshUtility.cs
 */

using UnityEngine;

namespace Mirage.Impostors.Core
{
    /// <summary>
    /// Helper class for impostor quad management
    /// </summary>
    public class ImpostorMeshUtility
    {
        /// <summary>
        /// Helper function to make a deep copy of a mesh
        /// Used to duplicate the source mesh for baking
        /// </summary>
        public static Mesh CopyMesh(Mesh mesh)
        {
            Mesh newmesh = new Mesh();
            newmesh.indexFormat = mesh.indexFormat;
            newmesh.subMeshCount = mesh.subMeshCount;
            newmesh.vertices = mesh.vertices;
            newmesh.triangles = mesh.triangles;
            UnityEngine.Rendering.SubMeshDescriptor[] smd = new UnityEngine.Rendering.SubMeshDescriptor[mesh.subMeshCount];
            for (int i = 0; i < mesh.subMeshCount; ++i)
                smd[i] = mesh.GetSubMesh(i);
            newmesh.SetSubMeshes(smd);
            newmesh.uv = mesh.uv;
            newmesh.uv2 = mesh.uv2;
            newmesh.uv3 = mesh.uv3;
            newmesh.uv4 = mesh.uv4;
            newmesh.boneWeights = mesh.boneWeights;
            newmesh.normals = mesh.normals;
            newmesh.colors = mesh.colors;
            newmesh.tangents = mesh.tangents;
            newmesh.bounds = mesh.bounds;
            return newmesh;
        }

        /// <summary>
        /// Helper function to build a quad with a custom size and proper UVs
        /// </summary>
        public static Mesh BuildQuad(float size)
        {
            Mesh mesh = new Mesh();
            // Setup vertices
            Vector3[] newVertices = new Vector3[4];
            float halfHeight = size * 0.5f;
            float halfWidth = size * 0.5f;
            newVertices[0] = new Vector3(-halfWidth, -halfHeight, 0);
            newVertices[1] = new Vector3(-halfWidth, halfHeight, 0);
            newVertices[2] = new Vector3(halfWidth, -halfHeight, 0);
            newVertices[3] = new Vector3(halfWidth, halfHeight, 0);

            // Setup UVs
            Vector2[] newUVs = new Vector2[newVertices.Length];
            newUVs[0] = new Vector2(0, 0);
            newUVs[1] = new Vector2(0, 1);
            newUVs[2] = new Vector2(1, 0);
            newUVs[3] = new Vector2(1, 1);

            // Setup triangles
            int[] newTriangles = new int[] { 0, 1, 2, 3, 2, 1 };

            // Setup normals
            Vector3[] newNormals = new Vector3[newVertices.Length];
            for (int i = 0; i < newNormals.Length; i++)
            {
                newNormals[i] = Vector3.back;
            }

            // Create quad
            mesh.vertices = newVertices;
            mesh.uv = newUVs;
            mesh.triangles = newTriangles;
            mesh.normals = newNormals;

            return mesh;
        }
    }
}
