using System;
using System.Collections;
using System.Collections.Generic;
using Chronos;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Jobs;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    [SerializeField]
    private int staticShootingRequestsPerFrame = 10;

    private NativeArray<int> projectileIds;
    private NativeHashMap<int, float> projectileLifetimes;
    private Dictionary<int, ProjectileStateBased> projectileLookup =
        new Dictionary<int, ProjectileStateBased>();

    [SerializeField]
    private Timekeeper timekeeper;
    private Dictionary<GameObject, Vector3> lastPositions = new Dictionary<GameObject, Vector3>();

    private GameObject playerGameObject;

    [Range(0f, 1f)]
    public float projectileAccuracy = 1f;

    private Dictionary<ProjectileStateBased, float> lastPredictionTimes =
        new Dictionary<ProjectileStateBased, float>();
    private float predictionUpdateInterval = 0.1f;

    private Dictionary<int, Transform> enemyTransforms = new Dictionary<int, Transform>();
    private float lastEnemyUpdateTime = 0f;
    private const float ENEMY_UPDATE_INTERVAL = 0.5f;

    private ProjectilePool projectilePool;
    private ProjectileEffectManager effectManager;
    private ProjectileSpawner projectileSpawner;

    private Dictionary<Transform, Vector3> cachedEnemyPositions =
        new Dictionary<Transform, Vector3>();
    private float enemyPositionUpdateInterval = 0.5f;
    private float lastEnemyPositionUpdateTime;

    private GlobalClock globalClock;

    private float _lastFrameTime;

    private int frameCounter = 0;
    private const int UPDATE_INTERVAL = 5; // Update every 5 frames

    // Added member variables for job handling
    private NativeArray<float> updatedLifetimes;
    private JobHandle updateJobHandle;

    private JobHandle _updateProjectilesJobHandle;
    private bool _isJobRunning = false;

    private const int INITIAL_CAPACITY = 1000;
    private const int GROWTH_FACTOR = 2;

    [SerializeField] private int maxRaycastsPerJob = 10000;
    [SerializeField] private int subSteps = 4;
    [SerializeField] private float maxRaycastDistance = 10f;

    private NativeArray<RaycastCommand> raycastCommands;
    private NativeArray<RaycastHit> raycastResults;
    private TransformAccessArray transformAccessArray;

    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private LayerMask playerLayerMask;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        projectileIds = new NativeArray<int>(INITIAL_CAPACITY, Allocator.Persistent);
        projectileLifetimes = new NativeHashMap<int, float>(INITIAL_CAPACITY, Allocator.Persistent);
        updatedLifetimes = new NativeArray<float>(INITIAL_CAPACITY, Allocator.Persistent);

        SceneManager.sceneLoaded += OnSceneLoaded;

        projectilePool = GetComponent<ProjectilePool>();
        effectManager = GetComponent<ProjectileEffectManager>();
        projectileSpawner = GetComponent<ProjectileSpawner>();

        playerGameObject = GameObject.FindGameObjectWithTag("Player");
        if (playerGameObject == null)
        {
            ConditionalDebug.LogWarning("Player GameObject not found during initialization.");
        }

        // Initialize layer masks
        enemyLayerMask = LayerMask.GetMask("Enemy");
        playerLayerMask = LayerMask.GetMask("Player");

        // Initialize the NativeArrays
        raycastCommands = new NativeArray<RaycastCommand>(INITIAL_CAPACITY, Allocator.Persistent);
        raycastResults = new NativeArray<RaycastHit>(INITIAL_CAPACITY, Allocator.Persistent);
    }

    private void Start()
    {
        InitializeGlobalClock();
    }

    private void InitializeGlobalClock()
    {
        try
        {
            globalClock = Timekeeper.instance.Clock("Test") as GlobalClock;
            if (globalClock == null)
            {
                ConditionalDebug.LogWarning(
                    "'Test' clock is not a GlobalClock. Some time-related features may not work as expected."
                );
            }
        }
        catch (ChronosException e)
        {
            ConditionalDebug.LogError($"Failed to initialize global clock: {e.Message}");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        timekeeper = FindObjectOfType<Timekeeper>();
        if (timekeeper == null)
        {
            ConditionalDebug.LogWarning("Timekeeper not found in the scene.");
        }

        ClearNativeArray(projectileIds);
        projectileLifetimes.Clear();
        projectileLookup.Clear();
        projectilePool.ClearPool();
        effectManager.ClearPools();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (projectileIds.IsCreated)
            projectileIds.Dispose();
        if (projectileLifetimes.IsCreated)
            projectileLifetimes.Dispose();
        if (updatedLifetimes.IsCreated)
            updatedLifetimes.Dispose();
        if (raycastCommands.IsCreated) raycastCommands.Dispose();
        if (raycastResults.IsCreated) raycastResults.Dispose();
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
    }

    private void Update()
    {
        float subStepTime = Time.deltaTime / subSteps;

        for (int step = 0; step < subSteps; step++)
        {
            // Handle collisions
            HandleCollisions();
        }

        // Process results and clean up
        ProcessCollisionResults();
    }

    private void HandleCollisions()
    {
        int projectileCount = projectileLookup.Count;

        // Resize arrays if needed
        if (raycastCommands.Length < projectileCount)
        {
            if (raycastCommands.IsCreated) raycastCommands.Dispose();
            if (raycastResults.IsCreated) raycastResults.Dispose();
            raycastCommands = new NativeArray<RaycastCommand>(projectileCount, Allocator.Persistent);
            raycastResults = new NativeArray<RaycastHit>(projectileCount, Allocator.Persistent);
        }

        // Set up raycast commands
        int index = 0;
        foreach (var projectile in projectileLookup.Values)
        {
            LayerMask layerMask = GetLayerMaskForProjectile(projectile);
            raycastCommands[index] = new RaycastCommand(
                projectile.transform.position,
                projectile.transform.forward,
                maxRaycastDistance,
                layerMask
            );
            index++;
        }

        // Schedule the raycast job
        JobHandle raycastJobHandle = RaycastCommand.ScheduleBatch(raycastCommands, raycastResults, 1, default(JobHandle));

        // Wait for the job to complete
        raycastJobHandle.Complete();
    }

    private LayerMask GetLayerMaskForProjectile(ProjectileStateBased projectile)
    {
        return projectile.isPlayerShot ? enemyLayerMask : playerLayerMask;
    }

    private void ProcessCollisionResults()
    {
        for (int i = 0; i < raycastResults.Length; i++)
        {
            if (raycastResults[i].collider != null)
            {
                // Handle collision
                ProjectileStateBased projectile = projectileLookup[projectileIds[i]];
                projectile.OnTriggerEnter(raycastResults[i].collider);
            }
        }
    }

    private void ScheduleUpdateProjectiles(float deltaTime, float globalTimeScale)
    {
        int activeProjectileCount = projectileLookup.Count;
        if (activeProjectileCount == 0)
            return;

        // Ensure updatedLifetimes has enough capacity
        if (updatedLifetimes.Length < activeProjectileCount)
        {
            updatedLifetimes.Dispose();
            updatedLifetimes = new NativeArray<float>(activeProjectileCount, Allocator.Persistent);
        }

        var job = new UpdateProjectilesJob
        {
            ProjectileIds = projectileIds,
            ProjectileLifetimes = projectileLifetimes,
            UpdatedLifetimes = updatedLifetimes,
            DeltaTime = deltaTime,
            GlobalTimeScale = globalTimeScale,
        };

        _updateProjectilesJobHandle = job.Schedule(activeProjectileCount, 64);
        _isJobRunning = true;
    }

    private void UpdateProjectilesAfterJob(float deltaTime, float globalTimeScale)
    {
        int activeProjectileCount = projectileLookup.Count;
        if (activeProjectileCount == 0)
            return;

        List<int> projectilesToRemove = new List<int>();

        for (int i = 0; i < activeProjectileCount; i++)
        {
            int projectileId = projectileIds[i];
            if (projectileId == 0) continue; // Skip inactive slots

            if (projectileLookup.TryGetValue(projectileId, out ProjectileStateBased projectile))
            {
                float updatedLifetime = updatedLifetimes[i];

                if (updatedLifetime < 0)
                {
                    projectilesToRemove.Add(projectileId);
                }
                else
                {
                    projectileLifetimes[projectileId] = updatedLifetime;
                    projectile.CustomUpdate(globalTimeScale);
                }
            }
            else
            {
                projectilesToRemove.Add(projectileId);
            }
        }

        for (int i = projectilesToRemove.Count - 1; i >= 0; i--)
        {
            RemoveProjectile(projectilesToRemove[i]);
        }
    }

    private void RemoveProjectile(int projectileId)
    {
        if (projectileLookup.TryGetValue(projectileId, out ProjectileStateBased projectile))
        {
            projectile.Death();
            projectilePool.ReturnProjectileToPool(projectile);
            projectileLifetimes.Remove(projectileId);
            projectileLookup.Remove(projectileId);

            // Use binary search to find and remove the projectile ID
            int index = System.Array.BinarySearch(projectileIds.ToArray(), projectileId);
            if (index >= 0)
            {
                int lastIndex = projectileIds.Length - 1;
                projectileIds[index] = projectileIds[lastIndex];
                ResizeNativeArray(ref projectileIds, lastIndex);
            }
        }
    }

    public void RegisterProjectile(ProjectileStateBased projectile)
    {
        if (_isJobRunning)
        {
            _updateProjectilesJobHandle.Complete();
            _isJobRunning = false;
        }

        int projectileId = projectile.GetInstanceID();
        if (!projectileLookup.ContainsKey(projectileId))
        {
            int currentCount = projectileLookup.Count;
            if (currentCount >= projectileIds.Length)
            {
                ResizeNativeArrays(projectileIds.Length * GROWTH_FACTOR);
            }

            projectileIds[currentCount] = projectileId;
            projectileLifetimes[projectileId] = projectile.lifetime;
            projectileLookup[projectileId] = projectile;

            projectile.SetAccuracy(projectileAccuracy);

            ConditionalDebug.Log($"[ProjectileManager] Registered projectile: {projectile.name} with accuracy: {projectileAccuracy}. Total projectiles: {projectileLookup.Count}, Position: {projectile.transform.position}, Velocity: {projectile.rb?.velocity}");
        }
    }

    public void UnregisterProjectile(ProjectileStateBased projectile)
    {
        if (_isJobRunning)
        {
            _updateProjectilesJobHandle.Complete();
            _isJobRunning = false;
        }

        if (projectile == null)
            return;

        int projectileId = projectile.GetInstanceID();
        if (projectileLookup.ContainsKey(projectileId))
        {
            for (int i = 0; i < projectileIds.Length; i++)
            {
                if (projectileIds[i] == projectileId)
                {
                    projectileIds[i] = projectileIds[projectileIds.Length - 1];
                    ResizeNativeArray(ref projectileIds, projectileIds.Length - 1);
                    break;
                }
            }
            projectileLifetimes.Remove(projectileId);
            projectileLookup.Remove(projectileId);
        }
    }

    public void UpdateProjectileTargets()
    {
        foreach (var projectileId in projectileIds)
        {
            if (
                projectileLookup.TryGetValue(projectileId, out ProjectileStateBased projectile)
                && projectile.homing
            )
            {
                Transform playerTransform = GameObject.FindWithTag("Player Aim Target")?.transform;
                if (playerTransform != null)
                    projectile.currentTarget = playerTransform;
            }
        }
    }

    public Vector3 CalculateTargetVelocity(GameObject target)
    {
        Vector3 currentPos = target.transform.position;
        Vector3 previousPos;
        if (lastPositions.TryGetValue(target, out previousPos))
        {
            Vector3 velocity = (currentPos - previousPos) / Time.deltaTime;
            lastPositions[target] = currentPos;
            return velocity;
        }
        else
        {
            lastPositions[target] = currentPos;
            return Vector3.zero;
        }
    }

    public Transform FindNearestEnemy(Vector3 position)
    {
        if (Time.time - lastEnemyUpdateTime > ENEMY_UPDATE_INTERVAL)
        {
            UpdateEnemyTransforms();
        }

        Transform nearestEnemy = null;
        float nearestDistanceSqr = float.MaxValue;

        foreach (var enemy in enemyTransforms.Values)
        {
            if (enemy != null)
            {
                float distanceSqr = (enemy.position - position).sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearestEnemy = enemy;
                }
            }
        }

        return nearestEnemy;
    }

    private void UpdateEnemyTransforms()
    {
        enemyTransforms.Clear();
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            enemyTransforms[enemy.GetInstanceID()] = enemy.transform;
        }
        lastEnemyUpdateTime = Time.time;
    }

    private void UpdateEnemyPositions()
    {
        if (Time.time - lastEnemyPositionUpdateTime < enemyPositionUpdateInterval)
            return;

        cachedEnemyPositions.Clear();
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            cachedEnemyPositions[enemy.transform] = enemy.transform.position;
        }
        lastEnemyPositionUpdateTime = Time.time;
    }

    public void NotifyEnemyHit(GameObject enemy, ProjectileStateBased projectile)
    {
        if (projectile.GetCurrentState() is PlayerShotState)
        {
            PlayerLocking.Instance.RemoveLockedEnemy(enemy.transform);
        }
    }

    public void ReRegisterEnemiesAndProjectiles()
    {
        ClearNativeArray(projectileIds);
        projectileLifetimes.Clear();
        projectileLookup.Clear();
        projectilePool.ClearPool();
        effectManager.ClearPools();

        projectilePool.InitializeProjectilePool();
        effectManager.InitializeAllPools();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            var enemySetup = enemy.GetComponent<EnemyBasicSetup>();
            if (enemySetup != null)
            {
                enemySetup.RegisterProjectiles();
            }
            else
            {
                ConditionalDebug.LogWarning("EnemyBasicSetup component missing on enemy object");
            }
        }
    }

    public void ClearAllProjectiles()
    {
        foreach (var projectile in projectileLookup.Values)
        {
            projectile.Death();
        }

        ClearNativeArray(projectileIds);
        projectileLifetimes.Clear();
        projectileLookup.Clear();
        projectilePool.ClearProjectileRequests();

        if (ConditionalDebug.IsLoggingEnabled)
        {
            ConditionalDebug.Log(
                $"Cleared all projectiles. Pools: Projectile={projectilePool.GetPoolSize()}"
            );
        }
    }

    public void HandleRewind(float currentTime)
    {
        for (int i = projectileIds.Length - 1; i >= 0; i--)
        {
            int projectileId = projectileIds[i];
            if (projectileLookup.TryGetValue(projectileId, out ProjectileStateBased projectile))
            {
                if (projectile.ShouldDeactivate(currentTime))
                {
                    RemoveProjectile(projectileId);
                }
            }
        }
    }

    public void PlayOneShotSound(string soundEvent, Vector3 position)
    {
        FMODUnity.RuntimeManager.PlayOneShot(soundEvent, position);
    }

    private void ClearNativeArray<T>(NativeArray<T> array)
        where T : struct
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = default;
        }
    }

    private void ResizeNativeArray<T>(ref NativeArray<T> array, int newSize)
        where T : struct
    {
        NativeArray<T> newArray = new NativeArray<T>(newSize, Allocator.Persistent);
        int copyLength = Mathf.Min(array.Length, newSize);
        NativeArray<T>.Copy(array, newArray, copyLength);
        array.Dispose();
        array = newArray;
    }

    private void LogProjectileStatus()
    {
        int activeProjectiles = projectileLookup.Count;
        int pooledProjectiles = projectilePool.GetPoolSize();
        int enemyCount = enemyTransforms.Count;

        ConditionalDebug.Log(
            $"[ProjectileManager] Active Projectiles: {activeProjectiles}, "
                + $"Pooled Projectiles: {pooledProjectiles}, "
                + $"Enemy Count: {enemyCount}, "
                + $"Global Time Scale: {globalClock?.timeScale ?? 0}"
        );
    }

    [BurstCompile]
    private struct UpdateProjectilesJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<int> ProjectileIds;

        [ReadOnly]
        public NativeHashMap<int, float> ProjectileLifetimes;

        public NativeArray<float> UpdatedLifetimes;

        public float DeltaTime;
        public float GlobalTimeScale;

        public void Execute(int index)
        {
            if (index >= ProjectileIds.Length)
            {
                return;
            }

            int projectileId = ProjectileIds[index];
            if (
                projectileId == 0
                || !ProjectileLifetimes.TryGetValue(projectileId, out float lifetime)
            )
            {
                return;
            }

            lifetime -= DeltaTime * GlobalTimeScale;
            UpdatedLifetimes[index] = lifetime;
        }
    }

    public void CompleteRunningJobs()
    {
        if (_isJobRunning)
        {
            _updateProjectilesJobHandle.Complete();
            _isJobRunning = false;
        }
    }

    private void ResizeNativeArrays(int newSize)
    {
        ResizeNativeArray(ref projectileIds, newSize);
        ResizeNativeArray(ref updatedLifetimes, newSize);
        
        NativeHashMap<int, float> newLifetimes = new NativeHashMap<int, float>(newSize, Allocator.Persistent);
        foreach (var kvp in projectileLifetimes)
        {
            newLifetimes.Add(kvp.Key, kvp.Value);
        }
        projectileLifetimes.Dispose();
        projectileLifetimes = newLifetimes;
    }
}