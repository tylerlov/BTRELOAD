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

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    [SerializeField] private int staticShootingRequestsPerFrame = 10;

    private NativeArray<int> projectileIds;
    private NativeHashMap<int, float> projectileLifetimes;
    private Dictionary<int, ProjectileStateBased> projectileLookup = new Dictionary<int, ProjectileStateBased>();

    [SerializeField] private Timekeeper timekeeper;
    private Dictionary<GameObject, Vector3> lastPositions = new Dictionary<GameObject, Vector3>();

    private GameObject playerGameObject;

    [Range(0f, 1f)] public float projectileAccuracy = 1f;

    private Dictionary<ProjectileStateBased, float> lastPredictionTimes = new Dictionary<ProjectileStateBased, float>();
    private float predictionUpdateInterval = 0.1f;

    private Dictionary<int, Transform> enemyTransforms = new Dictionary<int, Transform>();
    private float lastEnemyUpdateTime = 0f;
    private const float ENEMY_UPDATE_INTERVAL = 0.5f;

    private ProjectilePool projectilePool;
    private ProjectileEffectManager effectManager;
    private ProjectileSpawner projectileSpawner;

    private Dictionary<Transform, Vector3> cachedEnemyPositions = new Dictionary<Transform, Vector3>();
    private float enemyPositionUpdateInterval = 0.5f;
    private float lastEnemyPositionUpdateTime;

    private GlobalClock globalClock;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        projectileIds = new NativeArray<int>(100, Allocator.Persistent);
        projectileLifetimes = new NativeHashMap<int, float>(100, Allocator.Persistent);

        SceneManager.sceneLoaded += OnSceneLoaded;

        projectilePool = GetComponent<ProjectilePool>();
        effectManager = GetComponent<ProjectileEffectManager>();
        projectileSpawner = GetComponent<ProjectileSpawner>();

        playerGameObject = GameObject.FindGameObjectWithTag("Player");
        if (playerGameObject == null)
        {
            ConditionalDebug.LogWarning("Player GameObject not found during initialization.");
        }

        // Remove the globalClock initialization from here
    }

    private void Start()
    {
        // Initialize the global clock here instead
        InitializeGlobalClock();

        // ... other Start logic ...
    }

    private void InitializeGlobalClock()
    {
        try
        {
            globalClock = Timekeeper.instance.Clock("Test") as GlobalClock;
            if (globalClock == null)
            {
                Debug.LogWarning("'Test' clock is not a GlobalClock. Some time-related features may not work as expected.");
            }
        }
        catch (ChronosException e)
        {
            Debug.LogError($"Failed to initialize global clock: {e.Message}");
            // Optionally, you could create the clock here if it doesn't exist
            // globalClock = Timekeeper.instance.AddClock("Test");
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
    }

    private void Update()
    {
        if (globalClock == null)
        {
            // Try to initialize the clock again if it failed earlier
            InitializeGlobalClock();
            if (globalClock == null)
            {
                // If it's still null, skip this update
                return;
            }
        }

        float globalTimeScale = globalClock.timeScale;
        float deltaTime = Time.deltaTime;

        int projectileCount = projectileIds.Length;
        if (projectileCount == 0) return;

        NativeArray<float> updatedLifetimes = new NativeArray<float>(projectileCount, Allocator.TempJob);

        var job = new UpdateProjectilesJob
        {
            ProjectileIds = projectileIds,
            ProjectileLifetimes = projectileLifetimes,
            UpdatedLifetimes = updatedLifetimes,
            DeltaTime = deltaTime,
            GlobalTimeScale = globalTimeScale
        };

        JobHandle jobHandle = job.Schedule(projectileCount, 64);
        jobHandle.Complete();

        for (int i = 0; i < projectileCount; i++)
        {
            float updatedLifetime = updatedLifetimes[i];
            int projectileId = projectileIds[i];

            if (updatedLifetime < 0)
            {
                RemoveProjectile(projectileId);
            }
            else
            {
                projectileLifetimes[projectileId] = updatedLifetime;
                if (projectileLookup.TryGetValue(projectileId, out ProjectileStateBased projectile))
                {
                    projectile.CustomUpdate(globalTimeScale);
                }
            }
        }

        updatedLifetimes.Dispose();

        ProcessProjectileRequests(globalTimeScale);
        projectilePool.CheckAndReplenishPool();
    }

    private void ProcessProjectileRequests(float timeScale)
    {
        if (timeScale <= 0)
        {
            return; // Don't process requests when time is stopped or rewinding
        }

        int processCount = Mathf.CeilToInt(staticShootingRequestsPerFrame * timeScale);
        processCount = Math.Min(projectilePool.GetProjectileRequestCount(), processCount);
        
        for (int i = 0; i < processCount; i++)
        {
            projectileSpawner.ProcessShootProjectile(projectilePool.DequeueProjectileRequest());
        }
    }

    private void RemoveProjectile(int projectileId)
    {
        if (projectileLookup.TryGetValue(projectileId, out ProjectileStateBased projectile))
        {
            projectile.Death();
        }
        projectileLifetimes.Remove(projectileId);
        projectileLookup.Remove(projectileId);
    }

    private void ProcessProjectileRequests()
    {
        int processCount = Math.Min(projectilePool.GetProjectileRequestCount(), staticShootingRequestsPerFrame);
        for (int i = 0; i < processCount; i++)
        {
            projectileSpawner.ProcessShootProjectile(projectilePool.DequeueProjectileRequest());
        }
    }

    public void RegisterProjectile(ProjectileStateBased projectile)
    {
        int projectileId = projectile.GetInstanceID();
        if (!projectileLookup.ContainsKey(projectileId))
        {
            projectileIds[projectileIds.Length - 1] = projectileId;
            projectileLifetimes[projectileId] = projectile.lifetime;
            projectileLookup[projectileId] = projectile;
            
            // Set the accuracy value for the projectile
            projectile.SetAccuracy(projectileAccuracy);
            
            ConditionalDebug.Log($"Registered projectile: {projectile.name} with accuracy: {projectileAccuracy}");
        }
    }

    public void UnregisterProjectile(ProjectileStateBased projectile)
    {
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
                Transform playerTransform = GameObject.FindWithTag("Player Aim Target").transform;
                projectile.currentTarget = playerTransform;
            }
        }
    }

    // Replace the existing CalculateTargetVelocity method with this improved version
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
            float distanceSqr = (enemy.position - position).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

    private void UpdateEnemyTransforms()
    {
        enemyTransforms.Clear();
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
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
        for (int i = projectileIds.Length - 1; i >= 0; i--)
        {
            int projectileId = projectileIds[i];
            if (projectileLookup.TryGetValue(projectileId, out ProjectileStateBased projectile))
            {
                projectile.Death();
            }
        }

        ClearNativeArray(projectileIds);
        projectileLifetimes.Clear();
        projectileLookup.Clear();
        projectilePool.ClearProjectileRequests();

        ConditionalDebug.Log($"Cleared all projectiles. Pools: Projectile={projectilePool.GetPoolSize()}");
    }

    public void PlayOneShotSound(string soundEvent, Vector3 position)
    {
        FMODUnity.RuntimeManager.PlayOneShot(soundEvent, position);
    }

    private void ClearNativeArray<T>(NativeArray<T> array) where T : struct
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = default;
        }
    }

    private void ResizeNativeArray<T>(ref NativeArray<T> array, int newSize) where T : struct
    {
        NativeArray<T> newArray = new NativeArray<T>(newSize, Allocator.Persistent);
        int copyLength = Mathf.Min(array.Length, newSize);
        NativeArray<T>.Copy(array, newArray, copyLength);
        array.Dispose();
        array = newArray;
    }

    [BurstCompile]
    private struct UpdateProjectilesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> ProjectileIds;
        [ReadOnly] public NativeHashMap<int, float> ProjectileLifetimes;
        [WriteOnly] public NativeArray<float> UpdatedLifetimes;
        public float DeltaTime;
        public float GlobalTimeScale;

        public void Execute(int index)
        {
            int projectileId = ProjectileIds[index];
            if (ProjectileLifetimes.TryGetValue(projectileId, out float lifetime))
            {
                lifetime -= DeltaTime * GlobalTimeScale;
                UpdatedLifetimes[index] = lifetime;
            }
            else
            {
                UpdatedLifetimes[index] = -1f;
            }
        }
    }
}