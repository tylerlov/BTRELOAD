using UnityEngine;
using Unity.Mathematics;

public class TreeBoundaryManager
{
  private readonly MonoBehaviour owner;
  private readonly TreeConfiguration config;

  public TreeBoundaryManager(MonoBehaviour owner)
  {
    this.owner = owner;
    this.config = owner.GetComponent<ProceduralTreeGenerator>().Configuration;
  }

  public void DrawGizmos()
  {
    if (!config.showBoundaryGizmo) return;
    
    // This is now handled in ProceduralTreeGenerator
    // to ensure consistent visualization in both edit and play mode
  }

  public bool IsValidPosition(float3 pos)
  {
    return !math.any(math.isnan(pos)) && 
           !math.any(math.isinf(pos)) && 
           math.all(math.abs(pos) < 100f);
  }

  public float GetRadialDistance(float3 point)
  {
    return math.length(new float2(point.x, point.z));
  }
} 