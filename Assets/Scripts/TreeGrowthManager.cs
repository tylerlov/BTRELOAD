using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;
using Unity.Jobs;

public class TreeGrowthManager
{
  private readonly MonoBehaviour owner;
  private readonly TreeConfiguration config;

  private NativeList<BranchData> branches;
  private float spawnTimer;
  private float trunkGrowthTimer;
  private int currentTrunkSegment;
  private bool isTrunkComplete;
  private int mainTrunkEndIndex;
  private int activeBranchCount;
  private quaternion previousRotation = quaternion.identity;

  public NativeList<BranchData> Branches => branches;
  public int MainTrunkEndIndex => mainTrunkEndIndex;
  public int ActiveBranchCount => activeBranchCount;

  public TreeGrowthManager(MonoBehaviour owner, TreeConfiguration config)
  {
    this.owner = owner;
    this.config = config;
    branches = new NativeList<BranchData>(Allocator.Persistent);
    CreateInitialBranch();
  }

  public void UpdateGrowth()
  {
    JobHandle.CompleteAll(new NativeArray<JobHandle>(0, Allocator.Temp));
    
    UpdateBranchGrowth();
    
    trunkGrowthTimer += Time.deltaTime;
    if (trunkGrowthTimer >= config.trunkGrowthInterval && !isTrunkComplete)
    {
      GrowTrunk();
      trunkGrowthTimer -= config.trunkGrowthInterval;
    }
    
    if (mainTrunkEndIndex > 0)
    {
      spawnTimer += Time.deltaTime;
      if (spawnTimer >= config.branchSpawnInterval)
      {
        SpawnNewBranches();
        spawnTimer -= config.branchSpawnInterval;
      }
    }
  }

  private void UpdateBranchGrowth()
  {
    float deltaTime = Time.deltaTime;
    
    for (int i = 0; i < branches.Length; i++)
    {
      var branch = branches[i];
      float growthMultiplier = branch.isTrunkSegment ? 1f : 1f;
      branch.age += deltaTime * config.growthSpeed * growthMultiplier;
      branches[i] = branch;
    }
  }

  private bool HasTrunkSegments()
  {
    for (int i = 0; i < branches.Length; i++)
    {
      if (branches[i].isTrunkSegment)
      {
        return true;
      }
    }
    return false;
  }

  private void GrowTrunk()
  {
    if (currentTrunkSegment > config.trunkSegments)
    {
      isTrunkComplete = true;
      return;
    }

    if (branches.Length == 0 || !HasTrunkSegments())
    {
      Debug.Log("No valid trunk found, creating initial branch");
      ResetTreeState();
      return;
    }

    if (currentTrunkSegment > config.maxTrunkSegmentsToKeep && currentTrunkSegment % 2 == 0)
    {
      RemoveOldestTrunkSegment();
      if (branches.Length == 0 || !HasTrunkSegments())
      {
        Debug.Log("Lost trunk after removal, resetting tree");
        ResetTreeState();
        return;
      }
    }

    bool foundTrunkSegment = false;
    if (mainTrunkEndIndex >= branches.Length || !branches[mainTrunkEndIndex].isTrunkSegment)
    {
      for (int i = branches.Length - 1; i >= 0; i--)
      {
        if (branches[i].isTrunkSegment)
        {
          mainTrunkEndIndex = i;
          foundTrunkSegment = true;
          break;
        }
      }

      if (!foundTrunkSegment)
      {
        Debug.Log("No trunk segments found, creating initial branch");
        CreateInitialBranch();
        return;
      }
    }

    var lastSegment = branches[mainTrunkEndIndex];
    Debug.Assert(lastSegment.isTrunkSegment, "Last segment must be a trunk segment");

    float3 upDirection = new float3(0, 1, 0);
    quaternion newRotation = GetTrunkRotation();
    
    if (config.accumulateTrunkDeviation && currentTrunkSegment > 1)
    {
        newRotation = math.slerp(previousRotation, newRotation, 1f - config.trunkStraightness);
    }
    
    float3 newDir = math.mul(newRotation, upDirection);
    float3 blendedDir = math.normalize(math.lerp(
        upDirection, 
        newDir, 
        config.trunkDeviationStrength * (1f - config.trunkStraightness)
    ));
    
    float3 startPos = lastSegment.endPosition;
    float3 endPos = startPos + blendedDir * config.trunkLength;
    
    previousRotation = newRotation;
    
    int newTrunkIndex = branches.Length;
    branches.Add(new BranchData
    {
      startPosition = startPos,
      endPosition = endPos,
      thickness = config.trunkThickness,
      parentIndex = mainTrunkEndIndex,
      canSpawnBranches = false,
      age = 0,
      isTrunkSegment = true,
      depth = 0
    });
    
    var previousSegment = branches[mainTrunkEndIndex];
    previousSegment.canSpawnBranches = true;
    branches[mainTrunkEndIndex] = previousSegment;
    
    mainTrunkEndIndex = newTrunkIndex;
    currentTrunkSegment++;
    activeBranchCount++;

    Debug.Log($"Added trunk segment {currentTrunkSegment} at index {mainTrunkEndIndex}");
  }

  private void SpawnNewBranches()
  {
    if (branches.Length == 0 || mainTrunkEndIndex >= branches.Length)
    {
      Debug.LogWarning($"Invalid state for spawning branches. Branches: {branches.Length}, MainTrunkIndex: {mainTrunkEndIndex}");
      return;
    }

    int nonTrunkBranches = 0;
    for (int i = 0; i < branches.Length; i++)
    {
      if (!branches[i].isTrunkSegment)
      {
        nonTrunkBranches++;
      }
    }
    
    if (nonTrunkBranches >= config.maxBranches * 0.9f)
    {
      int excessBranches = Mathf.CeilToInt((nonTrunkBranches - config.maxBranches * 0.8f) + config.branchesPerSpawn);
      RemoveOldestBranches(excessBranches);
      
      nonTrunkBranches = 0;
      for (int i = 0; i < branches.Length; i++)
      {
        if (!branches[i].isTrunkSegment)
        {
          nonTrunkBranches++;
        }
      }
    }

    int branchesToSpawn = Mathf.Min(config.branchesPerSpawn, config.maxBranches - nonTrunkBranches);
    int attempts = 0;
    int maxAttempts = branchesToSpawn * 5;
    int spawned = 0;
    
    while (spawned < branchesToSpawn && attempts < maxAttempts)
    {
      float heightBias = Mathf.Pow(Random.value, 1f - config.heightGrowthBias);
      int parentIndex;
      
      if (Random.value < 0.2f)
      {
        float normalizedHeight = Mathf.Pow(heightBias, 0.7f);
        int maxIndex = Mathf.Min(mainTrunkEndIndex, branches.Length - 1);
        parentIndex = Mathf.Clamp(
          Mathf.FloorToInt(normalizedHeight * maxIndex),
          0,
          maxIndex
        );
      }
      else
      {
        int availableBranchCount = Mathf.Max(0, branches.Length - (mainTrunkEndIndex + 1));
        
        if (availableBranchCount > 0)
        {
          if (Random.value < config.newGrowthPreference * 0.8f)
          {
            int maxOffset = Mathf.Min(5, branches.Length - mainTrunkEndIndex - 1);
            int minIndex = mainTrunkEndIndex + 1;
            parentIndex = Random.Range(minIndex, minIndex + maxOffset);
          }
          else
          {
            parentIndex = SelectBranchParent(availableBranchCount);
          }
        }
        else
        {
          parentIndex = Random.Range(0, Mathf.Min(mainTrunkEndIndex + 1, branches.Length));
        }
      }
      
      if (parentIndex >= 0 && parentIndex < branches.Length)
      {
        var branch = branches[parentIndex];
        float distanceFromTrunk = GetRadialDistance(branch.endPosition);
        float distanceProbability = math.lerp(1f, 0.3f, distanceFromTrunk / config.maxRadius);
        
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
    float minThickness = config.branchThickness * 0.3f;
    
    if (parentBranch.thickness >= minThickness)
    {
      float newThickness = parentBranch.isTrunkSegment ?
        math.min(config.branchThickness, config.trunkThickness * config.thicknessReduction) :
        math.max(parentBranch.thickness * config.thicknessReduction, 0.02f);
      
      float t = Mathf.Pow(Random.value, 0.5f);
      float3 branchPoint = math.lerp(parentBranch.startPosition, parentBranch.endPosition, t);
      
      if (GetRadialDistance(branchPoint) > config.maxRadius * (1f + config.radiusOvershootTolerance))
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
      
      float angleVariation = Random.Range(-config.maxBranchAngle * 1.2f, config.maxBranchAngle);
      quaternion randomRot = quaternion.AxisAngle(
        rotationAxis,
        math.radians(angleVariation)
      );
      
      float3 direction = math.mul(randomRot, parentDir);
      float3 upwardDirection = new float3(0, 1, 0);
      direction = math.normalize(math.lerp(direction, upwardDirection, config.upwardBias * 0.7f));
      
      float3 forwardDirection = new float3(0, 0, 1);
      float2 directionXZ = new float2(direction.x, direction.z);
      float2 forwardXZ = new float2(forwardDirection.x, forwardDirection.z);
      
      float angle = math.degrees(math.atan2(directionXZ.x, directionXZ.y));
      
      if (math.abs(angle) <= config.clearPathAngle)
      {
        return false;
      }
      
      float newLength = config.branchLength * Random.Range(0.8f, 1.2f);
      float3 endPoint = branchPoint + direction * newLength;
      
      float radialDistance = GetRadialDistance(endPoint);
      if (radialDistance > config.maxRadius * (1f + config.radiusOvershootTolerance))
      {
        float scale = config.maxRadius * (1f + Random.Range(0f, config.radiusOvershootTolerance)) / radialDistance;
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
        depth = parentBranch.depth + 1
      });
      
      activeBranchCount++;
      return true;
    }
    
    return false;
  }

  private void CreateInitialBranch()
  {
    quaternion initialRotation = GetTrunkRotation();
    float3 direction = math.mul(initialRotation, new float3(0, 1, 0));
    
    branches.Add(new BranchData
    {
      startPosition = float3.zero,
      endPosition = direction * config.trunkLength,
      thickness = config.trunkThickness,
      parentIndex = -1,
      canSpawnBranches = false,
      age = 0,
      isTrunkSegment = true,
      depth = 0
    });
    
    mainTrunkEndIndex = 0;
    currentTrunkSegment = 1;
    activeBranchCount = 1;
  }

  private quaternion GetTrunkRotation()
  {
    float deviationMultiplier = 1f - config.trunkStraightness;
    float angleX = Random.Range(-config.maxTrunkDeviation, config.maxTrunkDeviation) * deviationMultiplier;
    float angleZ = Random.Range(-config.maxTrunkDeviation, config.maxTrunkDeviation) * deviationMultiplier;
    
    return quaternion.Euler(
        math.radians(angleX),
        0,
        math.radians(angleZ)
    );
  }

  private float GetRadialDistance(float3 point)
  {
    return math.length(new float2(point.x, point.z));
  }

  private bool IsValidPosition(float3 pos)
  {
    return !math.any(math.isnan(pos)) && 
           !math.any(math.isinf(pos)) && 
           math.all(math.abs(pos) < 100f);
  }

  private void RemoveOldestTrunkSegment()
  {
    if (currentTrunkSegment <= config.maxTrunkSegmentsToKeep || branches.Length == 0) 
    {
        return;
    }

    Debug.Log($"Before removal - Branches: {branches.Length}, MainTrunkIndex: {mainTrunkEndIndex}");

    bool hasTrunkSegments = false;
    for (int i = 0; i < branches.Length; i++)
    {
        if (branches[i].isTrunkSegment)
        {
            hasTrunkSegments = true;
            break;
        }
    }

    if (!hasTrunkSegments)
    {
        Debug.LogWarning("No trunk segments found, resetting tree");
        ResetTreeState();
        return;
    }

    int firstTrunkIndex = -1;
    for (int i = 0; i < branches.Length; i++)
    {
        if (branches[i].isTrunkSegment)
        {
            firstTrunkIndex = i;
            break;
        }
    }

    if (firstTrunkIndex == -1)
    {
        Debug.LogWarning("Could not find first trunk segment, resetting tree");
        ResetTreeState();
        return;
    }

    var branchesToKeep = new List<BranchData>();
    var indexMapping = new Dictionary<int, int>();
    int newIndex = 0;

    HashSet<int> branchesToRemove = new HashSet<int>();
    branchesToRemove.Add(firstTrunkIndex);

    for (int i = 0; i < branches.Length; i++)
    {
        if (branches[i].parentIndex == firstTrunkIndex)
        {
            branchesToRemove.Add(i);
        }
    }

    for (int i = 0; i < branches.Length; i++)
    {
        if (!branchesToRemove.Contains(i))
        {
            var branch = branches[i];
            if (branch.parentIndex > firstTrunkIndex)
            {
                branch.parentIndex--;
            }
            indexMapping[i] = newIndex;
            branchesToKeep.Add(branch);
            newIndex++;
        }
    }

    bool hasRemainingTrunk = false;
    foreach (var branch in branchesToKeep)
    {
        if (branch.isTrunkSegment)
        {
            hasRemainingTrunk = true;
            break;
        }
    }

    if (!hasRemainingTrunk)
    {
        Debug.LogWarning("No trunk segments would remain after removal, resetting tree");
        ResetTreeState();
        return;
    }

    branches.Clear();
    foreach (var branch in branchesToKeep)
    {
        branches.Add(branch);
    }

    int trunkCount = 0;
    int lastTrunkIndex = -1;
    for (int i = 0; i < branches.Length; i++)
    {
        if (branches[i].isTrunkSegment)
        {
            trunkCount++;
            lastTrunkIndex = i;
        }
    }

    if (lastTrunkIndex == -1)
    {
        Debug.LogWarning("Lost trunk tracking after removal, resetting tree");
        ResetTreeState();
        return;
    }

    mainTrunkEndIndex = lastTrunkIndex;
    currentTrunkSegment--;
    activeBranchCount = branches.Length;

    Debug.Log($"After removal - Branches: {branches.Length}, MainTrunkIndex: {mainTrunkEndIndex}, TrunkCount: {trunkCount}");
  }

  private void RemoveOldestBranches(int branchesToRemove)
  {
    if (branches.Length == 0) return;

    var branchesToKeep = new List<BranchData>();
    var indexMapping = new Dictionary<int, int>();
    HashSet<int> indicesToRemove = new HashSet<int>();

    var branchAges = new List<(int index, float age)>();
    for (int i = 0; i < branches.Length; i++)
    {
      if (!branches[i].isTrunkSegment)
      {
        branchAges.Add((i, branches[i].age));
      }
    }

    branchAges.Sort((a, b) => b.age.CompareTo(a.age));

    for (int i = 0; i < branchAges.Count && indicesToRemove.Count < branchesToRemove; i++)
    {
      MarkBranchAndChildrenForRemoval(branchAges[i].index, indicesToRemove);
    }

    for (int i = 0; i < branches.Length; i++)
    {
      if (!indicesToRemove.Contains(i))
      {
        indexMapping[i] = branchesToKeep.Count;
        branchesToKeep.Add(branches[i]);
      }
    }

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

    branches.Clear();
    foreach (var branch in branchesToKeep)
    {
      branches.Add(branch);
    }

    activeBranchCount = branches.Length;
  }

  private void MarkBranchAndChildrenForRemoval(int branchIndex, HashSet<int> indicesToRemove)
  {
    if (indicesToRemove.Contains(branchIndex)) return;
    if (branches[branchIndex].isTrunkSegment) return;

    indicesToRemove.Add(branchIndex);

    for (int i = 0; i < branches.Length; i++)
    {
      if (branches[i].parentIndex == branchIndex)
      {
        MarkBranchAndChildrenForRemoval(i, indicesToRemove);
      }
    }
  }

  private int SelectBranchParent(int nonTrunkBranches)
  {
    int maxAttempts = 5;
    for (int i = 0; i < maxAttempts; i++)
    {
      int candidateIndex = mainTrunkEndIndex + 1 + Random.Range(0, nonTrunkBranches);
      if (candidateIndex < branches.Length)
      {
        var branch = branches[candidateIndex];
        if (branch.thickness >= config.branchThickness * 0.4f &&
          !branch.isTrunkSegment &&
          branch.age >= 0.2f)
        {
          return candidateIndex;
        }
      }
    }
    return mainTrunkEndIndex + 1 + Random.Range(0, nonTrunkBranches);
  }

  public void ResetTreeState()
  {
    Debug.Log("Resetting tree state");
    branches.Clear();
    mainTrunkEndIndex = 0;
    currentTrunkSegment = 0;
    activeBranchCount = 0;
    CreateInitialBranch();
  }

  public void Dispose()
  {
    if (branches.IsCreated) branches.Dispose();
  }
} 