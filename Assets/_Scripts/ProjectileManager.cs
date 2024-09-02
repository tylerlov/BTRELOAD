using System;
using System.Collections;
using System.Collections.Generic;
using Chronos;
using SickscoreGames.HUDNavigationSystem;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    private Dictionary<int, Material> materialLookup = new Dictionary<int, Material>();


    [SerializeField] private ProjectileStateBased projectilePrefab;
    [SerializeField] private int staticShootingRequestsPerFrame = 10;

    private NativeArray<int> projectileIds;
    private NativeHashMap<int, float> projectileLifetimes;
    private Dictionary<int, ProjectileStateBased> projectileLookup = new Dictionary<int, ProjectileStateBased>();

    [SerializeField] private Timekeeper timekeeper;
    private Dictionary<GameObject, Vector3> lastPositions = new Dictionary<GameObject, Vector3>();
    private Crosshair crosshair;

    private GameObject playerGameObject;

    [Range(0f, 1f)] public float projectileAccuracy = 1f;

    [SerializeField] private int maxEnemyShotsPerInterval = 4;
    [SerializeField] private float enemyShotIntervalSeconds = 3f;
    private Queue<Action> enemyShotQueue = new Queue<Action>();
    private int currentEnemyShotCount = 0;
    private float lastEnemyShotResetTime;

    private ProjectileStateBased lastCreatedProjectile;

    private Dictionary<Transform, Vector3> cachedEnemyPositions = new Dictionary<Transform, Vector3>();
    private float enemyPositionUpdateInterval = 0.5f;
    private float lastEnemyPositionUpdateTime;

    private Dictionary<ProjectileStateBased, float> lastPredictionTimes = new Dictionary<ProjectileStateBased, float>();
    private float predictionUpdateInterval = 0.1f;

    private Dictionary<int, Transform> enemyTransforms = new Dictionary<int, Transform>();
    private float lastEnemyUpdateTime = 0f;
    private const float ENEMY_UPDATE_INTERVAL = 0.5f;

    private ProjectilePool projectilePool;
    private ProjectileEffectManager effectManager;

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

        playerGameObject = GameObject.FindGameObjectWithTag("Player");
        if (playerGameObject == null)
        {
            ConditionalDebug.LogWarning("Player GameObject not found during initialization.");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        timekeeper = FindObjectOfType<Timekeeper>();
        if (timekeeper == null)
        {
            ConditionalDebug.LogWarning("Timekeeper not found in the scene.");
        }

        crosshair = FindObjectOfType<Crosshair>();
        if (crosshair == null)
        {
            ConditionalDebug.LogWarning("Crosshair not found in the scene.");
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

    private void Start()
    {
        StartCoroutine(ProcessEnemyShotQueue());
        lastEnemyShotResetTime = Time.time;
    }

    private void Update()
    {
        float globalTimeScale = timekeeper.Clock("Test").localTimeScale;
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

        ProcessProjectileRequests();
        projectilePool.CheckAndReplenishPool();
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
            ProcessShootProjectile(projectilePool.DequeueProjectileRequest());
        }
    }

     private void ProcessShootProjectile(ProjectileRequest request)
    {
        ProjectileStateBased projectile = projectilePool.GetProjectileFromPool();
        if (projectile == null)
        {
            ConditionalDebug.LogWarning("[ProjectileManager] No projectile available in pool, skipping shot.");
            return;
        }

        SetupProjectile(projectile, request);
        RegisterProjectile(projectile);
        ConditionalDebug.Log($"Projectile shot. Remaining in pool: {projectilePool.GetPoolSize()}");
    }

    private void SetupProjectile(ProjectileStateBased projectile, ProjectileRequest request)
    {
        projectile.transform.SetParent(transform);
        projectile.transform.position = request.Position;
        projectile.transform.rotation = request.Rotation;
        projectile.gameObject.SetActive(true);
        projectile.transform.localScale = Vector3.one * request.UniformScale;

        projectile.rb.isKinematic = false;
        projectile.rb.velocity = request.Rotation * Vector3.forward * request.Speed;
        projectile.bulletSpeed = request.Speed;
        projectile.SetLifetime(request.Lifetime);
        projectile.EnableHoming(request.EnableHoming);

         Material material = GetMaterialById(request.MaterialId);
        if (material != null && projectile.modelRenderer != null)
        {
            projectile.modelRenderer.material = material;
        }

        effectManager.CreateEnemyShotFX(projectile.transform, Vector3.zero, Vector3.one * request.UniformScale);

        GameObject radarSymbol = effectManager.GetRadarSymbolFromPool();
        if (radarSymbol != null)
        {
            radarSymbol.transform.SetParent(projectile.transform);
            radarSymbol.transform.localPosition = Vector3.zero;
            radarSymbol.SetActive(true);
        }

        projectile.initialSpeed = request.Speed;
        projectile.SetAccuracy(projectileAccuracy);

        if (!string.IsNullOrEmpty(request.ClockKey))
        {
            projectile.SetClock(request.ClockKey);
        }

        projectile.SetAccuracy(request.Accuracy);

        effectManager.CreateEnemyShotFX(projectile.transform, Vector3.zero, Vector3.one * request.UniformScale);
    }

    public void RegisterProjectile(ProjectileStateBased projectile)
    {
        int projectileId = projectile.GetInstanceID();
        if (!projectileLookup.ContainsKey(projectileId))
        {
            projectileIds[projectileIds.Length - 1] = projectileId;
            projectileLifetimes[projectileId] = projectile.lifetime;
            projectileLookup[projectileId] = projectile;
            ConditionalDebug.Log($"Registered projectile: {projectile.name}");
        }
    }

    public bool RequestEnemyShot(Action shotAction)
    {
        if (Time.time - lastEnemyShotResetTime >= enemyShotIntervalSeconds)
        {
            currentEnemyShotCount = 0;
            lastEnemyShotResetTime = Time.time;
        }

        if (currentEnemyShotCount < maxEnemyShotsPerInterval)
        {
            enemyShotQueue.Enqueue(shotAction);
            currentEnemyShotCount++;
            return true;
        }

        return false;
    }

    private IEnumerator ProcessEnemyShotQueue()
    {
        while (true)
        {
            if (enemyShotQueue.Count > 0)
            {
                Action shotAction = enemyShotQueue.Dequeue();
                shotAction.Invoke();
            }
            yield return null;
        }
    }

     public void ShootProjectile(Vector3 position, Quaternion rotation, float speed, float lifetime, float uniformScale, bool enableHoming = false, Material material = null)
    {
        ProjectileRequest request = projectilePool.GetProjectileRequest();
        request.Set(
            position,
            rotation,
            speed,
            lifetime,
            uniformScale,
            enableHoming,
            RegisterMaterial(material),
            "", // clockKey (empty for player projectiles)
            projectileAccuracy // using the projectileAccuracy field from ProjectileManager
        );
        projectilePool.EnqueueProjectileRequest(request);
    }

public ProjectileStateBased ShootProjectileFromEnemy(
        Vector3 position,
        Quaternion rotation,
        float speed,
        float lifetime,
        float uniformScale,
        bool enableHoming = false,
        Material material = null,
        string clockKey = "",
        float accuracy = 1f
    )
    {
        bool shotRequested = RequestEnemyShot(() =>
        {
            ProjectileRequest request = projectilePool.GetProjectileRequest();
            request.Set(position, rotation, speed, lifetime, uniformScale, enableHoming, RegisterMaterial(material), clockKey, accuracy);
            projectilePool.EnqueueProjectileRequest(request);
            
            lastCreatedProjectile = null; // Will be set in ProcessShootProjectile
        });

        if (!shotRequested)
        {
            ConditionalDebug.Log("[ProjectileManager] Enemy shot request denied due to rate limiting.");
        }

        return lastCreatedProjectile;
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

    public void PredictAndRotateProjectile(ProjectileStateBased projectile)
    {
        if (projectile.currentTarget == null)
            return;

        if (!lastPredictionTimes.TryGetValue(projectile, out float lastPredictionTime) || Time.time - lastPredictionTime >= predictionUpdateInterval)
        {
            Vector3 targetVelocity = CalculateTargetVelocity(projectile.currentTarget.gameObject);
            Vector3 toTarget = projectile.currentTarget.position - projectile.transform.position;
            float distanceToTarget = toTarget.magnitude;
            float projectileSpeed = projectile.bulletSpeed;

            float predictionTime = distanceToTarget / projectileSpeed;
            Vector3 predictedPosition = projectile.currentTarget.position + targetVelocity * predictionTime;

            float randomFactor = Mathf.Lerp(0.2f, 0f, projectile.accuracy);
            predictedPosition += UnityEngine.Random.insideUnitSphere * (distanceToTarget * randomFactor);

            projectile.predictedPosition = predictedPosition;
            lastPredictionTimes[projectile] = Time.time;
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

    public void NotifyEnemyHit(GameObject enemy, ProjectileStateBased projectile)
    {
        if (projectile.GetCurrentState() is PlayerShotState)
        {
            if (crosshair != null)
            {
                crosshair.RemoveLockedEnemy(enemy.transform);
            }
            else
            {
                ConditionalDebug.LogError("Crosshair reference is not set in ProjectileManager.");
            }
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

    public ProjectileStateBased GetLastCreatedProjectile()
    {
        return lastCreatedProjectile;
    }

    private int RegisterMaterial(Material material)
    {
        if (material == null)
            return -1;
        int id = material.GetInstanceID();
        if (!materialLookup.ContainsKey(id))
        {
            materialLookup[id] = material;
        }
        return id;
    }
    private Material GetMaterialById(int materialId)
    {
        if (materialId != -1 && materialLookup.TryGetValue(materialId, out Material material))
        {
            return material;
        }
        return null;
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