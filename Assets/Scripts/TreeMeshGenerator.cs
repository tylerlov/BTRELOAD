using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

public class TreeMeshGenerator
{
  private readonly MonoBehaviour owner;
  private readonly MeshFilter meshFilter;
  private readonly MeshRenderer meshRenderer;
  private readonly Material trunkMaterial;
  private readonly Material branchMaterial;
  private Mesh treeMesh;

  public TreeMeshGenerator(MonoBehaviour owner, Material trunkMaterial, Material branchMaterial)
  {
    this.owner = owner;
    this.trunkMaterial = trunkMaterial;
    this.branchMaterial = branchMaterial;
    this.meshFilter = owner.GetComponent<MeshFilter>();
    this.meshRenderer = owner.GetComponent<MeshRenderer>();
    SetupMeshComponents();
  }

  private void SetupMeshComponents()
  {
    treeMesh = new Mesh();
    meshFilter.mesh = treeMesh;
    meshRenderer.materials = new Material[] { trunkMaterial, branchMaterial };
  }

  public void UpdateMesh(TreeGrowthManager growthManager)
  {
    if (growthManager.Branches.Length == 0) return;

    // Convert NativeList to NativeArray for read-only access
    var branchesArray = new NativeArray<BranchData>(
      growthManager.Branches.Length,
      Allocator.TempJob,
      NativeArrayOptions.UninitializedMemory
    );
    branchesArray.CopyFrom(growthManager.Branches.AsArray());

    // Create separate jobs for trunk and branches
    var trunkJob = new GenerateBranchMeshJob
    {
      branches = branchesArray,
      vertices = new NativeList<float3>(Allocator.TempJob),
      triangles = new NativeList<int>(Allocator.TempJob),
      isTrunk = true
    };

    var branchJob = new GenerateBranchMeshJob
    {
      branches = branchesArray,
      vertices = new NativeList<float3>(Allocator.TempJob),
      triangles = new NativeList<int>(Allocator.TempJob),
      isTrunk = false
    };

    // Schedule jobs with dependencies
    var trunkHandle = trunkJob.Schedule();
    var branchHandle = branchJob.Schedule(trunkHandle);

    // Wait for all jobs to complete
    branchHandle.Complete();

    // Combine meshes with submeshes
    CombineMeshes(
      trunkJob.vertices.AsArray(),
      trunkJob.triangles.AsArray(),
      branchJob.vertices.AsArray(),
      branchJob.triangles.AsArray()
    );

    // Cleanup
    branchesArray.Dispose();
    trunkJob.vertices.Dispose();
    trunkJob.triangles.Dispose();
    branchJob.vertices.Dispose();
    branchJob.triangles.Dispose();
  }

  private void CombineMeshes(
    NativeArray<float3> trunkVertices,
    NativeArray<int> trunkTriangles,
    NativeArray<float3> branchVertices,
    NativeArray<int> branchTriangles)
  {
    // Convert vertices
    var allVertices = new Vector3[trunkVertices.Length + branchVertices.Length];
    for (int i = 0; i < trunkVertices.Length; i++)
    {
      allVertices[i] = trunkVertices[i];
    }
    for (int i = 0; i < branchVertices.Length; i++)
    {
      allVertices[trunkVertices.Length + i] = branchVertices[i];
    }

    // Adjust branch triangle indices
    var adjustedBranchTriangles = new int[branchTriangles.Length];
    for (int i = 0; i < branchTriangles.Length; i++)
    {
      adjustedBranchTriangles[i] = branchTriangles[i] + trunkVertices.Length;
    }

    // Set mesh data
    treeMesh.Clear();
    treeMesh.vertices = allVertices;
    treeMesh.subMeshCount = 2;
    treeMesh.SetTriangles(trunkTriangles.ToArray(), 0);  // Trunk submesh
    treeMesh.SetTriangles(adjustedBranchTriangles, 1);   // Branch submesh
    treeMesh.RecalculateNormals();
    treeMesh.RecalculateBounds();
  }
} 