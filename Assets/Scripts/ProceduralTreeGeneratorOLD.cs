using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Linq;

public class ProceduralTreeGeneratorOLD : MonoBehaviour
{
  [Header("Growth Parameters")]
  [SerializeField] private float branchLength = 0.5f;
  [SerializeField] private float branchThickness = 0.1f;
  [SerializeField] private float thicknessReduction = 0.9f;
  [SerializeField] private int maxBranches = 100;
  [SerializeField] private float branchSpawnInterval = 0.5f; // Time between branch spawns
  [SerializeField] private int branchesPerSpawn = 3; // How many branches to spawn each interval
  [SerializeField] private float growthSpeed = 1f;
  [SerializeField, Range(0f, 1f)] private float upwardBias = 0.5f; // 0 = random, 1 = straight up
  
  [Header("Rotation Parameters")]
  [SerializeField] private float maxBranchAngle = 60f;
  [SerializeField] private float randomRotationVariation = 15f;
  
  [Header("Rendering")]
  [SerializeField] private Material treeMaterial;
  
  [Header("Boundary Parameters")]
  [SerializeField] private float maxRadius = 5f;
  [SerializeField, Range(0f, 0.5f)] private float radiusOvershootTolerance = 0.1f; // How much branches can exceed radius
  [SerializeField] private bool showBoundaryGizmo = true;
  [SerializeField] private Color boundaryGizmoColor = new Color(1f, 0f, 0f, 0.2f);
  
  [Header("Trunk Parameters")]
  [SerializeField] private float trunkLength = 1f;
  [SerializeField] private float trunkThickness = 0.2f;
  [SerializeField, Range(0f, 45f)] private float maxTrunkDeviation = 15f; // Max angle from vertical
  [SerializeField] private int trunkSegments = 5; // Total number of trunk segments to grow
  [SerializeField] private int maxTrunkSegmentsToKeep = 8; // Maximum segments before removal
  [SerializeField] private float trunkGrowthInterval = 0.2f; // Faster trunk segment creation
  [Tooltip("Segments below this number will be removed along with their branches")]
  [SerializeField] private float heightGrowthBias = 0.8f; // Bias towards spawning branches higher up
  [SerializeField] private float newGrowthPreference = 0.7f; // Preference for growing from newer segments
  
  private NativeList<BranchData> branches;
  private Mesh treeMesh;
  private float spawnTimer;
  private int activeBranchCount;
  private float trunkGrowthTimer;
  private int currentTrunkSegment;
  private bool isTrunkComplete;
  private int mainTrunkEndIndex; // Index of the last trunk segment
  
  private void Start()
  {
    if (!TryGetComponent<MeshFilter>(out var meshFilter))
    {
      meshFilter = gameObject.AddComponent<MeshFilter>();
    }
    
    if (!TryGetComponent<MeshRenderer>(out var meshRenderer))
    {
      meshRenderer = gameObject.AddComponent<MeshRenderer>();
    }
    
    branches = new NativeList<BranchData>(Allocator.Persistent);
    treeMesh = new Mesh();
    meshFilter.mesh = treeMesh;
    meshRenderer.material = treeMaterial;
    
    // Create initial branch
    CreateInitialBranch();
  }
  
  private void CreateInitialBranch()
  {
    // Create first trunk segment
    quaternion initialRotation = GetTrunkRotation();
    float3 direction = math.mul(initialRotation, new float3(0, 1, 0));
    
    branches.Add(new BranchData
    {
      startPosition = float3.zero,
      endPosition = direction * trunkLength,
      thickness = trunkThickness,
      parentIndex = -1,
      canSpawnBranches = false, // Don't spawn from first segment until next is created
      age = 0,
      isTrunkSegment = true,
      depth = 0
    });
    
    mainTrunkEndIndex = 0;
    currentTrunkSegment = 1;
    activeBranchCount = 1;
  }
  
  private void Update()
  {
    // Move mesh generation to fixed update for consistency
    UpdateBranchGrowth();
    
    // Trunk growth is independent and should be more consistent
    trunkGrowthTimer += Time.deltaTime;
    if (trunkGrowthTimer >= trunkGrowthInterval && !isTrunkComplete)
    {
        GrowTrunk();
        trunkGrowthTimer -= trunkGrowthInterval; // Use subtraction instead of setting to 0
    }
    
    // Branch spawning is separate from trunk growth
    if (mainTrunkEndIndex > 0)
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= branchSpawnInterval)
        {
            SpawnNewBranches();
            spawnTimer -= branchSpawnInterval; // Use subtraction instead of setting to 0
        }
    }
  }
  
  private void GrowTrunk()
  {
    if (currentTrunkSegment > trunkSegments)
    {
        isTrunkComplete = true;
        return;
    }

    // Simplified bounds checking
    if (branches.Length == 0)
    {
        CreateInitialBranch();
        return;
    }

    // Handle trunk segment removal less frequently
    if (currentTrunkSegment > maxTrunkSegmentsToKeep && currentTrunkSegment % 2 == 0)
    {
        RemoveOldestTrunkSegment();
        if (branches.Length == 0)
        {
            CreateInitialBranch();
            return;
        }
    }

    // Simplified trunk segment validation
    if (mainTrunkEndIndex >= branches.Length || !branches[mainTrunkEndIndex].isTrunkSegment)
    {
      int lastTrunkIndex = branches.Length - 1;
      while (lastTrunkIndex >= 0 && !branches[lastTrunkIndex].isTrunkSegment)
      {
        lastTrunkIndex--;
      }

      if (lastTrunkIndex < 0)
      {
        CreateInitialBranch();
        return;
      }
      mainTrunkEndIndex = lastTrunkIndex;
    }

    var lastSegment = branches[mainTrunkEndIndex];
    
    // Simplified direction calculation
    float3 upDirection = new float3(0, 1, 0);
    quaternion rotation = GetTrunkRotation();
    float3 newDir = math.mul(rotation, upDirection);
    float3 blendedDir = math.normalize(math.lerp(upDirection, newDir, 0.1f));
    
    float3 startPos = lastSegment.endPosition;
    float3 endPos = startPos + blendedDir * trunkLength;
    
    // Add new trunk segment
    branches.Add(new BranchData
    {
        startPosition = startPos,
        endPosition = endPos,
        thickness = trunkThickness,
        parentIndex = mainTrunkEndIndex,
        canSpawnBranches = false,
        age = 0,
        isTrunkSegment = true,
        depth = 0
    });
    
    // Update previous segment
    if (mainTrunkEndIndex >= 0)
    {
        var previousSegment = branches[mainTrunkEndIndex];
        previousSegment.canSpawnBranches = true;
        branches[mainTrunkEndIndex] = previousSegment;
    }
    
    mainTrunkEndIndex = branches.Length - 1;
    currentTrunkSegment++;
    activeBranchCount++;
  }
  
  private quaternion GetTrunkRotation()
  {
    // Reduce random deviation for more consistent growth
    float angleX = Random.Range(-maxTrunkDeviation * 0.5f, maxTrunkDeviation * 0.5f);
    float angleZ = Random.Range(-maxTrunkDeviation * 0.5f, maxTrunkDeviation * 0.5f);
    
    return quaternion.Euler(
      math.radians(angleX),
      0,
      math.radians(angleZ)
    );
  }
  
  private void SpawnNewBranches()
  {
    // Add early bounds check
    if (branches.Length == 0 || mainTrunkEndIndex >= branches.Length)
    {
      Debug.LogWarning($"Invalid state for spawning branches. Branches: {branches.Length}, MainTrunkIndex: {mainTrunkEndIndex}");
      return;
    }

    // Calculate actual number of non-trunk branches
    int nonTrunkBranches = 0;
    for (int i = 0; i < branches.Length; i++)
    {
      if (!branches[i].isTrunkSegment)
      {
        nonTrunkBranches++;
      }
    }
    
    Debug.Log($"Current non-trunk branches: {nonTrunkBranches}, Max allowed: {maxBranches}");
    
    // If we're over or close to the limit, remove some old branches
    if (nonTrunkBranches >= maxBranches * 0.9f) // Start removing at 90% capacity
    {
      int excessBranches = Mathf.CeilToInt((nonTrunkBranches - maxBranches * 0.8f) + branchesPerSpawn);
      Debug.Log($"Removing {excessBranches} branches");
      RemoveOldestBranches(excessBranches);
      
      // Recalculate after removal
      nonTrunkBranches = 0;
      for (int i = 0; i < branches.Length; i++)
      {
        if (!branches[i].isTrunkSegment)
        {
          nonTrunkBranches++;
        }
      }
    }

    int branchesToSpawn = Mathf.Min(branchesPerSpawn, maxBranches - nonTrunkBranches);
    int attempts = 0;
    int maxAttempts = branchesToSpawn * 5;
    int spawned = 0;
    
    while (spawned < branchesToSpawn && attempts < maxAttempts)
    {
      float heightBias = Mathf.Pow(Random.value, 1f - heightGrowthBias);
      int parentIndex;
      
      if (Random.value < 0.2f)  // Changed from 0.4f
      {
        // Ensure we stay within bounds for trunk spawning
        float normalizedHeight = Mathf.Pow(heightBias, 0.7f);
        int maxIndex = Mathf.Min(mainTrunkEndIndex, branches.Length - 1);
        parentIndex = Mathf.Clamp(
          Mathf.FloorToInt(normalizedHeight * maxIndex),
          0,
          maxIndex
        );
      }
      else  // Spawn from existing branches (now 80% chance)
      {
        int availableBranchCount = Mathf.Max(0, branches.Length - (mainTrunkEndIndex + 1));
        
        if (availableBranchCount > 0)
        {
          if (Random.value < newGrowthPreference * 0.8f)  // Reduced from 1.0
          {
            // Focus on newer branches
            int maxOffset = Mathf.Min(5, branches.Length - mainTrunkEndIndex - 1);
            int minIndex = mainTrunkEndIndex + 1;
            parentIndex = Random.Range(minIndex, minIndex + maxOffset);
          }
          else
          {
            // Select from all non-trunk branches with preference for lower depth
            parentIndex = SelectBranchParent(availableBranchCount);
          }
        }
        else
        {
          parentIndex = Random.Range(0, Mathf.Min(mainTrunkEndIndex + 1, branches.Length));
        }
      }
      
      // Verify index is valid before using
      if (parentIndex >= 0 && parentIndex < branches.Length)
      {
        var branch = branches[parentIndex];
        float distanceFromTrunk = GetRadialDistance(branch.endPosition);
        float distanceProbability = math.lerp(1f, 0.3f, distanceFromTrunk / maxRadius);
        
        if (branch.canSpawnBranches && 
            branch.age >= 0.3f && 
            Random.value < distanceProbability)
        {
          if (CreateNewBranch(parentIndex))
          {
            spawned++;
          }
        }
      }
      
      attempts++;
    }
  }
  
  private bool CreateNewBranch(int parentIndex)
  {
    var parentBranch = branches[parentIndex];
    
    // Relax thickness requirements for branch spawning
    float minThickness = branchThickness * 0.3f; // Reduced from implicit 1.0
    if (parentBranch.thickness >= minThickness)
    {
      float newThickness = parentBranch.isTrunkSegment ?
        math.min(branchThickness, trunkThickness * thicknessReduction) :
        math.max(parentBranch.thickness * thicknessReduction, 0.02f); // Increased from 0.01f
      
      float t = Mathf.Pow(Random.value, 0.5f);
      float3 branchPoint = math.lerp(parentBranch.startPosition, parentBranch.endPosition, t);
      
      if (GetRadialDistance(branchPoint) > maxRadius * (1f + radiusOvershootTolerance))
        return false;
        
      float3 parentDir = math.normalize(parentBranch.endPosition - parentBranch.startPosition);
      
      float3 rotationAxis = math.normalize(new float3(
        Random.Range(-1f, 1f),
        Random.Range(-0.5f, 1f),
        Random.Range(-1f, 1f)
      ));
      
      if (math.abs(math.dot(rotationAxis, parentDir)) > 0.9f)
      {
        rotationAxis = math.normalize(math.cross(parentDir, new float3(0, 1, 0)));
      }
      
      float angleVariation = Random.Range(-maxBranchAngle * 1.2f, maxBranchAngle);
      quaternion randomRot = quaternion.AxisAngle(
        rotationAxis,
        math.radians(angleVariation)
      );
      
      float3 direction = math.mul(randomRot, parentDir);
      float3 upwardDirection = new float3(0, 1, 0);
      direction = math.normalize(math.lerp(direction, upwardDirection, upwardBias * 0.7f));
      
      float newLength = branchLength * Random.Range(0.8f, 1.2f);
      float3 endPoint = branchPoint + direction * newLength;
      
      float radialDistance = GetRadialDistance(endPoint);
      if (radialDistance > maxRadius * (1f + radiusOvershootTolerance))
      {
        float scale = maxRadius * (1f + Random.Range(0f, radiusOvershootTolerance)) / radialDistance;
        float2 xzScaled = new float2(endPoint.x, endPoint.z) * scale;
        endPoint = new float3(xzScaled.x, endPoint.y, xzScaled.y);
      }
      
      if (!IsValidPosition(branchPoint) || !IsValidPosition(endPoint))
        return false;
      
      branches.Add(new BranchData
      {
        startPosition = branchPoint,
        endPosition = endPoint,
        thickness = newThickness,
        parentIndex = parentIndex,
        canSpawnBranches = true,
        age = 0,
        isTrunkSegment = false,
        depth = parentBranch.depth + 1 // Track branch depth
      });
      
      activeBranchCount++;
      return true;
    }
    
    return false;
  }
  
  private bool IsValidPosition(float3 pos)
  {
    return !math.any(math.isnan(pos)) && 
           !math.any(math.isinf(pos)) && 
           math.all(math.abs(pos) < 100f); // Keep branches within reasonable bounds
  }
  
  private void UpdateBranchGrowth()
  {
    float deltaTime = Time.deltaTime;
    
    for (int i = 0; i < branches.Length; i++)
    {
      var branch = branches[i];
      // Use trunk growth rate only for trunk segments
      float growthMultiplier = branch.isTrunkSegment ? 1f : 1f;
      branch.age += deltaTime * growthSpeed * growthMultiplier;
      branches[i] = branch;
    }
  }
  
  private void UpdateMeshGeneration()
  {
    if (branches.Length == 0) return;

    var generateMeshJob = new GenerateBranchMeshJob
    {
      branches = branches,
      vertices = new NativeList<float3>(Allocator.TempJob),
      triangles = new NativeList<int>(Allocator.TempJob)
    };
    
    var jobHandle = generateMeshJob.Schedule();
    jobHandle.Complete();
    
    var vertices = generateMeshJob.vertices.AsArray();
    
    // Safety check for vertex count
    if (vertices.Length == 0)
    {
      generateMeshJob.vertices.Dispose();
      generateMeshJob.triangles.Dispose();
      return;
    }

    var verticesPerBranch = vertices.Length / branches.Length;
    
    // Update vertices based on branch growth
    for (int branchIndex = 0; branchIndex < branches.Length; branchIndex++)
    {
      var branch = branches[branchIndex];
      if (branch.age < 1f)
      {
        float3 growthPos = math.lerp(branch.startPosition, branch.endPosition, math.saturate(branch.age));
        
        int startIndex = branchIndex * verticesPerBranch;
        int endIndex = startIndex + verticesPerBranch;
        
        // Add bounds check
        endIndex = math.min(endIndex, vertices.Length);
        
        for (int i = startIndex; i < endIndex; i++)
        {
          float thickness = branch.isTrunkSegment ? trunkThickness : 
            math.max(branches[branch.parentIndex].thickness * thicknessReduction, 0.01f);
          
          float t = math.length(vertices[i] - branch.startPosition) / 
                   math.length(branch.endPosition - branch.startPosition);
          vertices[i] = math.lerp(branch.startPosition, growthPos, t);
        }
      }
    }
    
    UpdateMesh(vertices, generateMeshJob.triangles.AsArray());
    
    generateMeshJob.vertices.Dispose();
    generateMeshJob.triangles.Dispose();
  }
  
  private void UpdateMesh(NativeArray<float3> vertices, NativeArray<int> triangles)
  {
    var verts = new Vector3[vertices.Length];
    for (int i = 0; i < vertices.Length; i++)
    {
      verts[i] = vertices[i];
    }
    
    treeMesh.Clear();
    treeMesh.vertices = verts;
    treeMesh.triangles = triangles.ToArray();
    treeMesh.RecalculateNormals();
    treeMesh.RecalculateBounds();
  }
  
  private void OnDestroy()
  {
    if (branches.IsCreated) branches.Dispose();
  }
  
  #if UNITY_EDITOR
  private void OnDrawGizmos()
  {
    if (!showBoundaryGizmo) return;
    
    // Draw cylinder boundary
    DrawWireCylinder(transform.position, maxRadius, 20f, boundaryGizmoColor);
    
    // Draw slightly larger cylinder for overshoot tolerance
    var toleranceColor = new Color(boundaryGizmoColor.r, boundaryGizmoColor.g, boundaryGizmoColor.b, boundaryGizmoColor.a * 0.5f);
    DrawWireCylinder(transform.position, maxRadius * (1f + radiusOvershootTolerance), 20f, toleranceColor);
    
    // Draw branches
    if (Application.isPlaying && branches.IsCreated)
    {
      Gizmos.color = Color.green;
      for (int i = 0; i < branches.Length; i++)
      {
        var branch = branches[i];
        Gizmos.DrawLine(branch.startPosition, branch.endPosition);
      }
    }
  }

  private void DrawWireCylinder(Vector3 position, float radius, float height, Color color)
  {
    Gizmos.color = color;
    
    // Draw circles at top and bottom
    int segments = 32;
    float angleStep = 2f * Mathf.PI / segments;
    
    // Bottom circle
    Vector3 prevBottom = new Vector3(radius, -height/2f, 0);
    Vector3 prevTop = new Vector3(radius, height/2f, 0);
    
    for (int i = 1; i <= segments; i++)
    {
      float angle = i * angleStep;
      Vector3 nextBottom = new Vector3(radius * Mathf.Cos(angle), -height/2f, radius * Mathf.Sin(angle));
      Vector3 nextTop = new Vector3(radius * Mathf.Cos(angle), height/2f, radius * Mathf.Sin(angle));
      
      // Draw bottom circle
      Gizmos.DrawLine(position + prevBottom, position + nextBottom);
      // Draw top circle
      Gizmos.DrawLine(position + prevTop, position + nextTop);
      // Draw vertical lines
      Gizmos.DrawLine(position + prevBottom, position + prevTop);
      
      prevBottom = nextBottom;
      prevTop = nextTop;
    }
  }
  #endif

  // Helper method to get radial distance (XZ plane only)
  private float GetRadialDistance(float3 point)
  {
    return math.length(new float2(point.x, point.z));
  }

  private void ResetTreeState()
  {
    Debug.Log("Resetting tree state");
    branches.Clear();
    mainTrunkEndIndex = 0;
    currentTrunkSegment = 0;
    activeBranchCount = 0;
    CreateInitialBranch();
  }

  private void RemoveOldestTrunkSegment()
  {
    // Safety checks
    if (currentTrunkSegment <= maxTrunkSegmentsToKeep || branches.Length == 0) 
    {
      return;
    }

    Debug.Log($"Before removal - Branches: {branches.Length}, MainTrunkIndex: {mainTrunkEndIndex}");

    var branchesToKeep = new List<BranchData>();
    var indexMapping = new Dictionary<int, int>();
    int newIndex = 0;

    // First pass: Identify branches directly connected to oldest trunk segment
    HashSet<int> branchesToRemove = new HashSet<int>();
    branchesToRemove.Add(0); // Mark oldest trunk for removal

    // Only mark branches that have the oldest trunk as direct parent
    for (int i = 0; i < branches.Length; i++)
    {
      if (branches[i].parentIndex == 0)
      {
        branchesToRemove.Add(i);
      }
    }

    Debug.Log($"Found {branchesToRemove.Count} branches to remove (including oldest trunk)");

    // Second pass: Keep and update remaining branches
    for (int i = 0; i < branches.Length; i++)
    {
      if (!branchesToRemove.Contains(i))
      {
        var branch = branches[i];
        
        // Update parent indices
        if (branch.parentIndex > 0)
        {
          // Shift parent index down by 1 since we're removing the first trunk
          branch.parentIndex--;
        }
        
        indexMapping[i] = newIndex;
        branchesToKeep.Add(branch);
        newIndex++;
      }
    }

    // Clear and rebuild the branches list
    branches.Clear();
    foreach (var branch in branchesToKeep)
    {
      branches.Add(branch);
    }

    // Update tracking variables
    int trunkCount = branchesToKeep.Where(b => b.isTrunkSegment).Count();
    mainTrunkEndIndex = trunkCount - 1;
    currentTrunkSegment--;
    activeBranchCount = branches.Length;

    Debug.Log($"After removal - Branches: {branches.Length}, MainTrunkIndex: {mainTrunkEndIndex}, TrunkCount: {trunkCount}, Removed: {branchesToRemove.Count}");
  }

  // Add new method for smart branch parent selection
  private int SelectBranchParent(int nonTrunkBranches)
  {
    int maxAttempts = 5;
    for (int i = 0; i < maxAttempts; i++)
    {
      int candidateIndex = mainTrunkEndIndex + 1 + Random.Range(0, nonTrunkBranches);
      if (candidateIndex < branches.Length)
      {
        var branch = branches[candidateIndex];
        // Prefer branches with lower depth and appropriate thickness
        if (branch.thickness >= branchThickness * 0.4f && // Relaxed thickness requirement
          !branch.isTrunkSegment &&
          branch.age >= 0.2f) // Reduced age requirement
        {
          return candidateIndex;
        }
      }
    }
    // Fallback to random non-trunk branch
    return mainTrunkEndIndex + 1 + Random.Range(0, nonTrunkBranches);
  }

  // Add new method for branch removal
  private void RemoveOldestBranches(int branchesToRemove)
  {
    if (branches.Length == 0) return;

    Debug.Log($"Starting branch removal. Current count: {branches.Length}, To remove: {branchesToRemove}");

    var branchesToKeep = new List<BranchData>();
    var indexMapping = new Dictionary<int, int>();
    HashSet<int> indicesToRemove = new HashSet<int>();

    // First identify oldest non-trunk branches
    var branchAges = new List<(int index, float age)>();
    for (int i = 0; i < branches.Length; i++)
    {
      if (!branches[i].isTrunkSegment)
      {
        branchAges.Add((i, branches[i].age));
      }
    }

    // Sort by age, oldest first
    branchAges.Sort((a, b) => b.age.CompareTo(a.age));

    // Mark oldest branches and their children for removal
    for (int i = 0; i < branchAges.Count && indicesToRemove.Count < branchesToRemove; i++)
    {
      MarkBranchAndChildrenForRemoval(branchAges[i].index, indicesToRemove);
    }

    // Keep all trunk segments and non-marked branches
    for (int i = 0; i < branches.Length; i++)
    {
      if (!indicesToRemove.Contains(i))
      {
        indexMapping[i] = branchesToKeep.Count;
        branchesToKeep.Add(branches[i]);
      }
    }

    // Update parent indices
    for (int i = 0; i < branchesToKeep.Count; i++)
    {
      var branch = branchesToKeep[i];
      if (branch.parentIndex >= 0)
      {
        if (indexMapping.ContainsKey(branch.parentIndex))
        {
          branch.parentIndex = indexMapping[branch.parentIndex];
        }
        else
        {
          branch.parentIndex = -1;
        }
      }
      branchesToKeep[i] = branch;
    }

    // Update branch list
    branches.Clear();
    foreach (var branch in branchesToKeep)
    {
      branches.Add(branch);
    }

    activeBranchCount = branches.Length;
    Debug.Log($"Removed {indicesToRemove.Count} branches. New count: {activeBranchCount}");
  }

  private void MarkBranchAndChildrenForRemoval(int branchIndex, HashSet<int> indicesToRemove)
  {
    if (indicesToRemove.Contains(branchIndex)) return;
    if (branches[branchIndex].isTrunkSegment) return;

    indicesToRemove.Add(branchIndex);

    // Mark all children for removal
    for (int i = 0; i < branches.Length; i++)
    {
      if (branches[i].parentIndex == branchIndex)
      {
        MarkBranchAndChildrenForRemoval(i, indicesToRemove);
      }
    }
  }

  // Add this new method for mesh updates
  private void FixedUpdate()
  {
    UpdateMeshGeneration();
  }
} 
