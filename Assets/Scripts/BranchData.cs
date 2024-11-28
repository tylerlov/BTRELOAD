using Unity.Mathematics;

public struct BranchData
{
  public float3 startPosition;
  public float3 endPosition;
  public float thickness;
  public int parentIndex;
  public bool canSpawnBranches;
  public float age;
  public bool isTrunkSegment;
  public int depth;
  public int materialIndex;
} 