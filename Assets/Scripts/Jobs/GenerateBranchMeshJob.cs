using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

public struct GenerateBranchMeshJob : IJob
{
  [ReadOnly] public NativeArray<BranchData> branches;
  public NativeList<float3> vertices;
  public NativeList<int> triangles;
  public bool isTrunk;
  
  private const int SEGMENTS = 8;
  
  public void Execute()
  {
    for (int i = 0; i < branches.Length; i++)
    {
      var branch = branches[i];
      if (branch.isTrunkSegment != isTrunk) continue;
      
      GenerateCylinder(branch.startPosition, branch.endPosition, branch.thickness);
    }
  }
  
  private void GenerateCylinder(float3 start, float3 end, float radius)
  {
    // Safety check for invalid positions
    if (!IsValidPosition(start) || !IsValidPosition(end) || radius <= 0)
      return;

    var baseVertCount = vertices.Length;
    var direction = end - start;
    
    // Safety check for zero-length branches
    if (math.lengthsq(direction) < 1e-6f)
      return;
      
    var up = math.normalize(direction);
    
    // Find perpendicular vectors
    float3 right;
    if (math.abs(math.dot(up, new float3(0, 1, 0))) > 0.9f)
      right = math.normalize(math.cross(up, new float3(1, 0, 0)));
    else
      right = math.normalize(math.cross(up, new float3(0, 1, 0)));
      
    var forward = math.normalize(math.cross(right, up));
    
    // Generate circle vertices at start and end
    for (int i = 0; i < SEGMENTS; i++)
    {
      float angle = (i / (float)SEGMENTS) * math.PI * 2;
      float cos = math.cos(angle);
      float sin = math.sin(angle);
      
      float3 offset = right * (cos * radius) + forward * (sin * radius);
      
      vertices.Add(start + offset);
      vertices.Add(end + offset);
    }
    
    // Generate triangles
    for (int i = 0; i < SEGMENTS; i++)
    {
      int current = baseVertCount + i * 2;
      int next = baseVertCount + ((i + 1) % SEGMENTS) * 2;
      
      // First triangle
      triangles.Add(current);
      triangles.Add(next);
      triangles.Add(current + 1);
      
      // Second triangle
      triangles.Add(next);
      triangles.Add(next + 1);
      triangles.Add(current + 1);
    }
    
    // Add end caps
    int centerStart = vertices.Length;
    vertices.Add(start);
    vertices.Add(end);
    
    // Start cap
    for (int i = 0; i < SEGMENTS; i++)
    {
      int current = baseVertCount + i * 2;
      int next = baseVertCount + ((i + 1) % SEGMENTS) * 2;
      
      triangles.Add(centerStart);
      triangles.Add(next);
      triangles.Add(current);
    }
    
    // End cap
    for (int i = 0; i < SEGMENTS; i++)
    {
      int current = baseVertCount + i * 2 + 1;
      int next = baseVertCount + ((i + 1) % SEGMENTS) * 2 + 1;
      
      triangles.Add(centerStart + 1);
      triangles.Add(current);
      triangles.Add(next);
    }
  }
  
  private bool IsValidPosition(float3 pos)
  {
    return !math.any(math.isnan(pos)) && 
           !math.any(math.isinf(pos)) && 
           math.all(math.abs(pos) < 1000f); // Reasonable bounds check
  }
} 