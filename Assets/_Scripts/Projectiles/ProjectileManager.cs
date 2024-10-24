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
using System.Linq;

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

    [SerializeField] private int maxRaycastsPerJob = 500; // Reduced from 1000
    [SerializeField] private int subSteps = 1; // Reduced from 2
    [SerializeField] private float maxRaycastDistance = 3f; // Reduced from 5f
    [SerializeField] private bool useRaycastsOnlyForHoming = true;
    [SerializeField] private bool useRaycastsForPlayerShots = false; // New field

    private NativeArray<RaycastCommand> raycastCommands;
    private NativeArray<RaycastHit> raycastResults;
    private TransformAccessArray transformAccessArray;

    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private LayerMask playerLayerMask;

    [SerializeField] private float baseProcessingInterval = 0.02f;
    private float currentProcessingInterval;
    private float lastProcessingTime;

    private bool isInitialized = false;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        try
        {
            // Initialize collections with proper error handling
            InitializeCollections();
            
            // Initialize components
            InitializeComponents();
            
            // Set up scene handling
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            isInitialized = true;
            ConditionalDebug.Log("[ProjectileManager] Initialization completed successfully");
        }
        catch (System.Exception e)
        {
            ConditionalDebug.LogError($"[ProjectileManager] Initialization failed: {e.Message}");
            throw; // Rethrow to ensure the error is visible
        }
    }

    private void InitializeCollections()
    {
        SafeDispose(); // Ensure clean slate
        
        projectileIds = new NativeArray<int>(INITIAL_CAPACITY, Allocator.Persistent);
        projectileLifetimes = new NativeHashMap<int, float>(INITIAL_CAPACITY, Allocator.Persistent);
        updatedLifetimes = new NativeArray<float>(INITIAL_CAPACITY, Allocator.Persistent);
        
        if (!ValidateCollections())
        {
            throw new System.InvalidOperationException("Failed to initialize collections properly");
        }
    }

    private void InitializeComponents()
    {
        projectilePool = GetComponent<ProjectilePool>() ?? throw new System.NullReferenceException("ProjectilePool component missing");
        effectManager = GetComponent<ProjectileEffectManager>() ?? throw new System.NullReferenceException("ProjectileEffectManager component missing");
        projectileSpawner = GetComponent<ProjectileSpawner>() ?? throw new System.NullReferenceException("ProjectileSpawner component missing");
    }

    private void Start()
    {
        InitializeGlobalClock();
        TimeManager.Instance.OnTimeScaleChanged += UpdateProcessingInterval;
        UpdateProcessingInterval(TimeManager.Instance.GetCurrentTimeScale());
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
        StartCoroutine(InitializeAfterSceneLoad());
    }

    private IEnumerator InitializeAfterSceneLoad()
    {
        yield return new WaitForEndOfFrame();
        
        // Clear existing collections
        SafeDispose();
        
        // Reinitialize collections
        projectileIds = new NativeArray<int>(INITIAL_CAPACITY, Allocator.Persistent);
        projectileLifetimes = new NativeHashMap<int, float>(INITIAL_CAPACITY, Allocator.Persistent);
        updatedLifetimes = new NativeArray<float>(INITIAL_CAPACITY, Allocator.Persistent);
        
        // Clear dictionaries
        projectileLookup.Clear();
        
        // Validate the initialization
        if (!ValidateCollections())
        {
            ConditionalDebug.LogError("[ProjectileManager] Failed to initialize collections after scene load");
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SafeDispose();
        isInitialized = false;
    }

    private void UpdateProcessingInterval(float timeScale)
    {
        currentProcessingInterval = baseProcessingInterval / timeScale;
    }

    private void Update()
    {
        if (isTransitioning)
        {
            return;
        }

        if (!ValidateCollections())
        {
            ConditionalDebug.LogWarning("[ProjectileManager] Collections were invalid and have been recovered during Update");
            return;
        }

        if (Time.time - lastProcessingTime >= currentProcessingInterval)
        {
            ProcessProjectileRequests();
            lastProcessingTime = Time.time;
        }

        float subStepTime = Time.deltaTime / subSteps;

        for (int step = 0; step < subSteps; step++)
        {
            // Handle collisions
            HandleCollisions();
        }

        // Process results and clean up
        ProcessCollisionResults();
    }

    private void ProcessProjectileRequests()
    {
        if (isTransitioning)
        {
            return;
        }

        // Process a fixed number of requests per interval, adjusted for time scale
        int requestsToProcess = Mathf.CeilToInt(staticShootingRequestsPerFrame / TimeManager.Instance.GetCurrentTimeScale());
        
        for (int i = 0; i < requestsToProcess && ProjectilePool.Instance.HasPendingRequests(); i++)
        {
            ProjectileRequest request;
            if (ProjectilePool.Instance.TryDequeueProjectileRequest(out request))
            {
                ProjectileStateBased projectile = projectilePool.GetProjectile();
                if (projectile != null)
                {
                    projectileSpawner.ProcessShootProjectile(request, projectile, request.IsStatic);
                }
            }
        }
    }

    private void HandleCollisions()
    {
        // Count only projectiles that need raycasts
        int projectileCount = projectileLookup.Values.Where(p => 
            ShouldUseRaycastForProjectile(p)
        ).Count();  // Changed from .Count to .Count()

        if (projectileCount == 0) return;

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
            if (!ShouldUseRaycastForProjectile(projectile))
                continue;

            // Calculate dynamic raycast distance based on projectile speed
            float dynamicDistance = Mathf.Min(
                projectile.bulletSpeed * Time.fixedDeltaTime * 2f, 
                maxRaycastDistance
            );

            LayerMask layerMask = GetLayerMaskForProjectile(projectile);
            raycastCommands[index] = new RaycastCommand(
                projectile.transform.position,
                projectile.transform.forward,
                dynamicDistance,
                layerMask
            );
            index++;
        }

        // Schedule the raycast job with appropriate batch size
        if (index > 0)
        {
            int batchSize = Mathf.Max(1, index / 32); // Divide work into reasonable batches
            JobHandle raycastJobHandle = RaycastCommand.ScheduleBatch(
                raycastCommands.GetSubArray(0, index), 
                raycastResults.GetSubArray(0, index), 
                batchSize,
                default(JobHandle)
            );
            raycastJobHandle.Complete();
        }
    }

    private LayerMask GetLayerMaskForProjectile(ProjectileStateBased projectile)
    {
        return projectile.isPlayerShot ? enemyLayerMask : playerLayerMask;
    }

    private void ProcessCollisionResults()
    {
        // Create a safe copy of projectiles to process
        var projectilesToProcess = projectileLookup.Values
            .Where(p => ShouldUseRaycastForProjectile(p))
            .ToList();

        int processedCount = 0;
        foreach (var projectile in projectilesToProcess)
        {
            if (projectile == null || !projectileLookup.ContainsValue(projectile))
                continue;

            if (raycastResults[processedCount].collider != null)
            {
                try
                {
                    projectile.OnTriggerEnter(raycastResults[processedCount].collider);
                    ConditionalDebug.Log($"Projectile {projectile.GetInstanceID()} hit {raycastResults[processedCount].collider.name}");
                }
                catch (Exception e)
                {
                    ConditionalDebug.LogError($"Error processing collision for projectile {projectile.GetInstanceID()}: {e.Message}");
                }
            }
            processedCount++;
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
        if (isTransitioning)
        {
            ConditionalDebug.LogWarning("[ProjectileManager] Attempting to register projectile during scene transition. Skipping.");
            return;
        }

        if (projectile == null)
        {
            ConditionalDebug.LogError("[ProjectileManager] Attempting to register null projectile!");
            return;
        }

        if (!ValidateCollections())
        {
            ConditionalDebug.LogWarning("[ProjectileManager] Collections were invalid and have been recovered");
        }

        if (!projectileIds.IsCreated)
        {
            ConditionalDebug.LogError("[ProjectileManager] Attempting to register projectile but arrays are not initialized!");
            return;
        }

        if (_isJobRunning)
        {
            _updateProjectilesJobHandle.Complete();
            _isJobRunning = false;
        }

        int projectileId = projectile.GetInstanceID();
        if (!projectileLookup.ContainsKey(projectileId))
        {
            int currentCount = projectileLookup.Count;
            
            // Validate array capacity before adding
            if (currentCount >= projectileIds.Length)
            {
                ResizeNativeArrays(projectileIds.Length * GROWTH_FACTOR);
            }

            // Validate after resize
            if (currentCount < projectileIds.Length)
            {
                projectileIds[currentCount] = projectileId;
                projectileLifetimes[projectileId] = projectile.lifetime;
                projectileLookup[projectileId] = projectile;
                projectile.SetAccuracy(projectileAccuracy);
            }
            else
            {
                ConditionalDebug.LogError("[ProjectileManager] Failed to register projectile - insufficient capacity");
            }
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
        if (_isJobRunning)
        {
            _updateProjectilesJobHandle.Complete();
            _isJobRunning = false;
        }

        try
        {
            // Create a copy of the dictionary keys to avoid modification during iteration
            var projectileIds = new List<int>(projectileLookup.Keys);
            
            foreach (var id in projectileIds)
            {
                if (projectileLookup.TryGetValue(id, out ProjectileStateBased projectile))
                {
                    if (projectile != null)
                    {
                        projectile.Death();
                        projectilePool.ReturnProjectileToPool(projectile);
                    }
                }
            }

            // Clear all collections
            SafeDispose();
            InitializeCollections();
            
            projectileLookup.Clear();
            projectilePool.ClearProjectileRequests();

            ConditionalDebug.Log($"[ProjectileManager] Cleared all projectiles and reinitialized collections");
        }
        catch (System.Exception e)
        {
            ConditionalDebug.LogError($"[ProjectileManager] Error during ClearAllProjectiles: {e.Message}");
            // Try to recover
            RecoverCollections();
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

    public void ResetManager()
    {
        // Dispose existing collections
        if (projectileIds.IsCreated) projectileIds.Dispose();
        if (projectileLifetimes.IsCreated) projectileLifetimes.Dispose();
        if (updatedLifetimes.IsCreated) updatedLifetimes.Dispose();

        // Reinitialize collections
        projectileIds = new NativeArray<int>(INITIAL_CAPACITY, Allocator.Persistent);
        projectileLifetimes = new NativeHashMap<int, float>(INITIAL_CAPACITY, Allocator.Persistent);
        updatedLifetimes = new NativeArray<float>(INITIAL_CAPACITY, Allocator.Persistent);

        // Clear dictionaries
        projectileLookup.Clear();
        lastPredictionTimes.Clear();
        lastPositions.Clear();
        enemyTransforms.Clear();
        cachedEnemyPositions.Clear();
    }

    private bool ValidateCollections()
    {
        bool isValid = true;
        string errorMessage = "";

        // Check if collections are created
        if (!projectileIds.IsCreated)
        {
            errorMessage += "ProjectileIds not created. ";
            isValid = false;
        }
        if (!projectileLifetimes.IsCreated)
        {
            errorMessage += "ProjectileLifetimes not created. ";
            isValid = false;
        }
        if (!updatedLifetimes.IsCreated)
        {
            errorMessage += "UpdatedLifetimes not created. ";
            isValid = false;
        }

        // Check for zero-length arrays
        if (projectileIds.Length == 0)
        {
            errorMessage += "ProjectileIds has zero length. ";
            isValid = false;
        }

        // Check for capacity mismatches
        if (projectileIds.Length != updatedLifetimes.Length)
        {
            errorMessage += "Array length mismatch. ";
            isValid = false;
        }

        if (!isValid)
        {
            ConditionalDebug.LogError($"[ProjectileManager] Collection validation failed: {errorMessage}");
            RecoverCollections();
        }

        return isValid;
    }

    private void RecoverCollections()
    {
        ConditionalDebug.Log("[ProjectileManager] Attempting to recover collections...");
        
        try
        {
            // Store existing data if possible
            Dictionary<int, float> tempLifetimes = new Dictionary<int, float>();
            if (projectileLifetimes.IsCreated)
            {
                foreach (var kvp in projectileLifetimes)
                {
                    tempLifetimes[kvp.Key] = kvp.Value;
                }
            }

            // Safely dispose existing collections
            SafeDispose();

            // Reinitialize with default capacity
            projectileIds = new NativeArray<int>(INITIAL_CAPACITY, Allocator.Persistent);
            projectileLifetimes = new NativeHashMap<int, float>(INITIAL_CAPACITY, Allocator.Persistent);
            updatedLifetimes = new NativeArray<float>(INITIAL_CAPACITY, Allocator.Persistent);

            // Restore data
            int index = 0;
            foreach (var projectile in projectileLookup.Values)
            {
                if (projectile != null && index < INITIAL_CAPACITY)
                {
                    int projectileId = projectile.GetInstanceID();
                    projectileIds[index] = projectileId;
                    projectileLifetimes[projectileId] = tempLifetimes.ContainsKey(projectileId) 
                        ? tempLifetimes[projectileId] 
                        : projectile.lifetime;
                    index++;
                }
            }

            ConditionalDebug.Log("[ProjectileManager] Collections recovered successfully");
        }
        catch (System.Exception e)
        {
            ConditionalDebug.LogError($"[ProjectileManager] Failed to recover collections: {e.Message}");
            throw; // Rethrow to ensure the error is not silently swallowed
        }
    }

    private void SafeDispose()
    {
        if (projectileIds.IsCreated) projectileIds.Dispose();
        if (projectileLifetimes.IsCreated) projectileLifetimes.Dispose();
        if (updatedLifetimes.IsCreated) updatedLifetimes.Dispose();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        if (SceneManagerBTR.Instance != null)
        {
            // Use the event names defined in GameManager
            EventManager.Instance.AddListener(GameManager.StartingTransitionEvent, OnSceneTransitionStart);
            EventManager.Instance.AddListener(GameManager.TransCamOffEvent, OnSceneTransitionEnd);
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        if (EventManager.Instance != null)
        {
            // Use the event names defined in GameManager
            EventManager.Instance.RemoveListener(GameManager.StartingTransitionEvent, OnSceneTransitionStart);
            EventManager.Instance.RemoveListener(GameManager.TransCamOffEvent, OnSceneTransitionEnd);
        }
    }

    private void OnSceneTransitionStart()
    {
        isTransitioning = true;
        // Clear all projectiles when transitioning starts
        ClearAllProjectiles();
    }

    private void OnSceneTransitionEnd()
    {
        isTransitioning = false;
        // Reinitialize collections after transition
        InitializeCollections();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene != SceneManager.GetActiveScene()) return;
        
        // Safely dispose and clear collections when unloading scene
        SafeDispose();
        projectileLookup.Clear();
        lastPredictionTimes.Clear();
        lastPositions.Clear();
        enemyTransforms.Clear();
        cachedEnemyPositions.Clear();
    }

    // Add async initialization support
    public async System.Threading.Tasks.Task InitializeAsync()
    {
        if (isInitialized)
        {
            return;
        }

        try
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                InitializeCollections();
            });

            InitializeComponents();
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            isInitialized = true;
            ConditionalDebug.Log("[ProjectileManager] Async initialization completed successfully");
        }
        catch (System.Exception e)
        {
            ConditionalDebug.LogError($"[ProjectileManager] Async initialization failed: {e.Message}");
            throw;
        }
    }

    public void OnWaveEnd()
    {
        // Complete any running jobs first
        CompleteRunningJobs();
        
        // Clear all projectiles safely
        var projectileIds = new List<int>(projectileLookup.Keys);
        foreach (var id in projectileIds)
        {
            if (projectileLookup.TryGetValue(id, out ProjectileStateBased projectile))
            {
                RemoveProjectile(id);
            }
        }
        
        // Rest of wave end cleanup...
        lastEnemyUpdateTime = 0f;
        lastEnemyPositionUpdateTime = 0f;
        lastPositions.Clear();
        enemyTransforms.Clear();
        cachedEnemyPositions.Clear();
        
        // Reinitialize collections for the next wave
        InitializeCollections();
    }

    public void OnWaveStart()
    {
        ConditionalDebug.Log("[ProjectileManager] Wave started, initializing systems...");
        
        // Ensure we're starting with clean collections
        if (!ValidateCollections())
        {
            InitializeCollections();
        }
        
        // Reset any wave-specific state
        lastEnemyUpdateTime = 0f;
        lastEnemyPositionUpdateTime = 0f;
        
        // Clear any leftover data from previous wave
        lastPositions.Clear();
        enemyTransforms.Clear();
        cachedEnemyPositions.Clear();
    }

    // Add this new method to determine if a projectile should use raycasts
    private bool ShouldUseRaycastForProjectile(ProjectileStateBased projectile)
    {
        // Always use raycasts for high-speed projectiles
        if (projectile.bulletSpeed > 30f) 
            return true;

        // Use state-based checks
        if (projectile.GetCurrentState() is PlayerShotState)
            return useRaycastsForPlayerShots;

        if (projectile.GetCurrentState() is EnemyShotState)
            return useRaycastsOnlyForHoming && projectile.homing;

        return false;
    }
}
