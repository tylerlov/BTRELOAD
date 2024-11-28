using UnityEngine;

[CreateAssetMenu(fileName = "TreeConfiguration", menuName = "Tree/Configuration")]
public class TreeConfiguration : ScriptableObject
{
  [Header("Growth Parameters")]
  public float branchLength = 0.5f;
  public float branchThickness = 0.1f;
  public float thicknessReduction = 0.9f;
  public int maxBranches = 100;
  public float branchSpawnInterval = 0.5f;
  public int branchesPerSpawn = 3;
  public float growthSpeed = 1f;
  [Range(0f, 1f)] public float upwardBias = 0.5f;
  
  [Header("Trunk Parameters")]
  public float trunkLength = 1f;
  public float trunkThickness = 0.2f;
  [Range(0f, 90f)] public float maxTrunkDeviation = 15f;
  [Range(0f, 1f)] public float trunkDeviationStrength = 0.1f;
  [Range(0f, 1f)] public float trunkStraightness = 0.5f;
  public bool accumulateTrunkDeviation = true;
  public int trunkSegments = 5;
  public int maxTrunkSegmentsToKeep = 8;
  public float trunkGrowthInterval = 0.2f;
  public float heightGrowthBias = 0.8f;
  public float newGrowthPreference = 0.7f;

  [Header("Boundary Parameters")]
  public float maxRadius = 5f;
  [Range(0f, 0.5f)] public float radiusOvershootTolerance = 0.1f;

  [Header("Visualization")]
  public bool showBoundaryGizmo = true;
  public Color boundaryGizmoColor = new Color(1f, 0f, 0f, 0.2f);

  [Header("Rotation Parameters")]
  public float maxBranchAngle = 60f;
  public float randomRotationVariation = 15f;

  [Header("Branch Restrictions")]
  [Tooltip("Clear path will be from -clearPathAngle to +clearPathAngle degrees, centered on forward (Z) axis")]
  [Range(0f, 180f)] public float clearPathAngle = 15f;

  [Header("Rendering")]
  public Material trunkMaterial;
  public Material branchMaterial;
} 