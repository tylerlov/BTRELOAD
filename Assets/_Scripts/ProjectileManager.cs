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
using UnityEngine.VFX;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    [SerializeField]
    private ProjectileStateBased projectilePrefab; // Assign in inspector

    [SerializeField]
    private GameObject enemyShotFXPrefab; // Assign in inspector

    [SerializeField]
    private int initialPoolSize = 10;

    [SerializeField]
    private ParticleSystem deathEffectPrefab; // Assign in inspector

    [SerializeField]
    private int initialDeathEffectPoolSize = 10;

    [SerializeField]
    private int staticShootingRequestsPerFrame = 10; // Configurable batch size

    private NativeArray<int> projectileIds;
    private NativeHashMap<int, float> projectileLifetimes;
    private Dictionary<int, ProjectileStateBased> projectileLookup =
        new Dictionary<int, ProjectileStateBased>();
    private Queue<ProjectileStateBased> projectilePool = new Queue<ProjectileStateBased>(200);
    private Queue<ParticleSystem> deathEffectPool = new Queue<ParticleSystem>(50);

    [SerializeField]
    private Timekeeper timekeeper;
    private Dictionary<GameObject, Vector3> lastPositions = new Dictionary<GameObject, Vector3>();
    private Crosshair crosshair;

    private GameObject playerGameObject; // Cache for player GameObject

    [Range(0f, 1f)]
    public float projectileAccuracy = 1f; // 0 = always miss, 1 = perfect accuracy

    public GameObject projectileRadarSymbol;

    [SerializeField]
    private int radarSymbolPoolSize = 50;
    private Queue<GameObject> radarSymbolPool = new Queue<GameObject>(50);

    [SerializeField]
    private int maxEnemyShotsPerInterval = 4;

    [SerializeField]
    private float enemyShotIntervalSeconds = 3f;
    private Queue<Action> enemyShotQueue = new Queue<Action>();
    private int currentEnemyShotCount = 0;
    private float lastEnemyShotResetTime;

    private ProjectileStateBased lastCreatedProjectile;

    [SerializeField]
    private VisualEffect lockedFXPrefab;

    [SerializeField]
    private int initialLockedFXPoolSize = 10;
    private Queue<VisualEffect> lockedFXPool = new Queue<VisualEffect>();

    private Dictionary<int, Material> materialLookup = new Dictionary<int, Material>();

    private Dictionary<Transform, Vector3> cachedEnemyPositions = new Dictionary<Transform, Vector3>();
    private float enemyPositionUpdateInterval = 0.5f;
    private float lastEnemyPositionUpdateTime;

    private Dictionary<ProjectileStateBased, float> lastPredictionTimes = new Dictionary<ProjectileStateBased, float>();
    private float predictionUpdateInterval = 0.1f;

    private Dictionary<int, Transform> enemyTransforms = new Dictionary<int, Transform>();
    private float lastEnemyUpdateTime = 0f;
    private const float ENEMY_UPDATE_INTERVAL = 0.5f;

    private Queue<ProjectileRequest> projectileRequestPool = new Queue<ProjectileRequest>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Initialize collections
        projectileIds = new NativeArray<int>(initialPoolSize, Allocator.Persistent);
        projectileLifetimes = new NativeHashMap<int, float>(100, Allocator.Persistent);
        projectileLookup = new Dictionary<int, ProjectileStateBased>();

        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Initialize all pools
        InitializeProjectilePool();
        InitializeDeathEffectPool();
        InitializeRadarSymbolPool();
        InitializeLockedFXPool();

        // Find and cache the player GameObject
        playerGameObject = GameObject.FindGameObjectWithTag("Player");
        if (playerGameObject == null)
        {
            ConditionalDebug.LogWarning("Player GameObject not found during initialization.");
        }
    }

    private void InitializeRadarSymbolPool()
    {
        radarSymbolPool = new Queue<GameObject>(radarSymbolPoolSize);
        for (int i = 0; i < radarSymbolPoolSize; i++)
        {
            GameObject radarSymbol = Instantiate(projectileRadarSymbol, transform);
            radarSymbol.SetActive(false);
            radarSymbolPool.Enqueue(radarSymbol);
        }
    }

    public GameObject GetRadarSymbolFromPool()
    {
        if (radarSymbolPool.Count > 0)
        {
            GameObject radarSymbol = radarSymbolPool.Dequeue();
            radarSymbol.SetActive(true);
            return radarSymbol;
        }
        return null;
    }

    public void ReturnRadarSymbolToPool(GameObject radarSymbol)
    {
        radarSymbol.SetActive(false);
        radarSymbol.transform.SetParent(transform);
        radarSymbolPool.Enqueue(radarSymbol);
    }

    // This method will be called every time a scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Attempt to find the Timekeeper and Crosshair references in the newly loaded scene
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

        // Clear existing pools and lists to reinitialize
        ClearNativeArray(projectileIds);
        projectileLifetimes.Clear();
        projectileLookup.Clear();
        projectilePool.Clear();
        deathEffectPool.Clear();

        // Reinitialize pools
        InitializeProjectilePool();
        InitializeDeathEffectPool();
    }

    private void OnDestroy()
    {
        // Unsubscribe from the sceneLoaded event to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (projectileIds.IsCreated)
            projectileIds.Dispose();
        if (projectileLifetimes.IsCreated)
            projectileLifetimes.Dispose();
    }

    private void InitializeProjectilePool()
    {
        projectilePool = new Queue<ProjectileStateBased>(200);
        for (int i = 0; i < 200; i++)
        {
            ProjectileStateBased proj = Instantiate(projectilePrefab, transform);
            proj.gameObject.SetActive(false);
            projectilePool.Enqueue(proj);
        }
    }

    private void InitializeDeathEffectPool()
    {
        deathEffectPool = new Queue<ParticleSystem>(initialDeathEffectPoolSize);
        for (int i = 0; i < initialDeathEffectPoolSize; i++)
        {
            ParticleSystem effect = Instantiate(deathEffectPrefab, transform);
            effect.gameObject.SetActive(false);
            deathEffectPool.Enqueue(effect);
        }
    }

    private void InitializeLockedFXPool()
    {
        for (int i = 0; i < initialLockedFXPoolSize; i++)
        {
            VisualEffect effect = Instantiate(lockedFXPrefab, transform);
            effect.gameObject.SetActive(false);
            lockedFXPool.Enqueue(effect);
        }
    }

    private Queue<ProjectileRequest> projectileRequests = new Queue<ProjectileRequest>();

    private void Start()
    {
        StartCoroutine(ProcessEnemyShotQueue());
        lastEnemyShotResetTime = Time.time;
    }

    private ProjectileRequest GetProjectileRequest()
    {
        if (projectileRequestPool.Count > 0)
            return projectileRequestPool.Dequeue();
        return new ProjectileRequest();
    }

    private void ReturnProjectileRequest(ProjectileRequest request)
    {
        projectileRequestPool.Enqueue(request);
    }

    public void ShootProjectile(Vector3 position, Quaternion rotation, float speed, float lifetime, float uniformScale, bool enableHoming = false, Material material = null)
    {
        ProjectileRequest request = GetProjectileRequest();
        request.Set(position, rotation, speed, lifetime, uniformScale, enableHoming, RegisterMaterial(material));
        projectileRequests.Enqueue(request);
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
        return materialId != -1 && materialLookup.TryGetValue(materialId, out Material material)
            ? material
            : null;
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
                // We can't check isLifetimePaused here, so we'll update the lifetime
                // and let the main thread decide whether to use this update or not
                lifetime -= DeltaTime * GlobalTimeScale;
                UpdatedLifetimes[index] = lifetime;
            }
            else
            {
                UpdatedLifetimes[index] = -1f; // Indicate that this projectile should be removed
            }
        }
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

        // Process the results
        for (int i = 0; i < projectileCount; i++)
        {
            float updatedLifetime = updatedLifetimes[i];
            int projectileId = projectileIds[i];

            if (updatedLifetime < 0)
            {
                // Remove projectile
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
        CheckAndReplenishPool();
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
        int processCount = Math.Min(projectileRequests.Count, staticShootingRequestsPerFrame);
        if (processCount == 0)
            return;

        for (int i = 0; i < processCount; i++)
        {
            ProcessShootProjectile(projectileRequests.Dequeue());
        }
    }

    private void ProcessShootProjectile(ProjectileRequest request)
    {
        if (projectilePool.Count == 0)
        {
            ConditionalDebug.LogWarning(
                "[ProjectileManager] No projectile available in pool, skipping shot. Pool size: " + projectilePool.Count
            );
            return;
        }

        ProjectileStateBased projectile = projectilePool.Dequeue();
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
        if (material != null && projectile.modelRenderer.material != material)
        {
            projectile.modelRenderer.material = material;
            projectile.modelRenderer.material.color = material.color;
        }

        RegisterProjectile(projectile);
        ConditionalDebug.Log($"Projectile shot. Remaining in pool: {projectilePool.Count}");
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

    public struct ProjectileRequest
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Speed;
        public float Lifetime;
        public float UniformScale;
        public bool EnableHoming;
        public int MaterialId;

        public void Set(Vector3 position, Quaternion rotation, float speed, float lifetime, float uniformScale, bool enableHoming, int materialId)
        {
            Position = position;
            Rotation = rotation;
            Speed = speed;
            Lifetime = lifetime;
            UniformScale = uniformScale;
            EnableHoming = enableHoming;
            MaterialId = materialId;
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
            if (projectilePool.Count == 0)
            {
                Debug.LogWarning("[ProjectileManager] No projectile available in pool, skipping shot.");
                return;
            }

            ProjectileStateBased projectile = projectilePool.Dequeue();
            SetupProjectile(projectile, position, rotation, speed, lifetime, uniformScale, enableHoming, material, clockKey, accuracy);
            
            lastCreatedProjectile = projectile;
        });

        if (!shotRequested)
        {
            ConditionalDebug.Log("[ProjectileManager] Enemy shot request denied due to rate limiting.");
        }

        return lastCreatedProjectile;
    }

    private void SetupProjectile(
        ProjectileStateBased projectile,
        Vector3 position,
        Quaternion rotation,
        float speed,
        float lifetime,
        float uniformScale,
        bool enableHoming,
        Material material,
        string clockKey,
        float accuracy
    )
    {
        projectile.transform.SetParent(transform);
        projectile.transform.position = position;
        projectile.transform.rotation = rotation;
        projectile.gameObject.SetActive(true);
        projectile.transform.localScale = Vector3.one * uniformScale;

        if (enemyShotFXPrefab != null)
        {
            GameObject enemyShotFX = Instantiate(enemyShotFXPrefab, projectile.transform);
            enemyShotFX.transform.localPosition = Vector3.zero;
            enemyShotFX.transform.localScale = Vector3.one * uniformScale;
            SetChildrenScale(enemyShotFX, Vector3.one * uniformScale);
            enemyShotFX.SetActive(true);
        }

        GameObject radarSymbol = GetRadarSymbolFromPool();
        if (radarSymbol != null)
        {
            radarSymbol.transform.SetParent(projectile.transform);
            radarSymbol.transform.localPosition = Vector3.zero;
            radarSymbol.SetActive(true);
        }

        projectile.rb.isKinematic = false;
        projectile.rb.velocity = rotation * Vector3.forward * speed;
        projectile.bulletSpeed = speed;
        projectile.SetLifetime(lifetime);
        projectile.EnableHoming(enableHoming);

        if (material != null && projectile.modelRenderer.material != material)
        {
            projectile.modelRenderer.material = material;
            projectile.modelRenderer.material.color = material.color;
        }

        if (!string.IsNullOrEmpty(clockKey))
        {
            projectile.SetClock(clockKey);
        }

        projectile.initialSpeed = speed;
        projectile.SetAccuracy(accuracy);

        RegisterProjectile(projectile);
    }

    private void SetChildrenScale(GameObject parent, Vector3 scale)
    {
        foreach (Transform child in parent.transform)
        {
            child.localScale = scale;
            SetChildrenScale(child.gameObject, scale); // Recursive call to set scale for all sub-children
        }
    }

    public void PlayDeathEffect(Vector3 position)
    {
        if (deathEffectPool.Count == 0)
        {
            ConditionalDebug.LogWarning("No death effect available in pool, skipping effect.");
            return;
        }

        ParticleSystem effect = deathEffectPool.Dequeue();
        if (playerGameObject != null)
        {
            effect.transform.SetParent(playerGameObject.transform);
        }
        else
        {
            ConditionalDebug.LogWarning(
                "Player GameObject not found. Effect will not follow the player."
            );
            effect.transform.SetParent(transform); // Fallback to the default parent
        }
        effect.transform.position = position;
        effect.gameObject.SetActive(true);
        effect.Play();

        // Return the effect to the pool after it has finished playing
        StartCoroutine(
            ReturnEffectToPoolAfterFinished(
                effect,
                playerGameObject != null ? playerGameObject.transform : transform
            )
        );
    }

    private IEnumerator ReturnEffectToPoolAfterFinished(
        ParticleSystem effect,
        Transform originalParent
    )
    {
        yield return new WaitWhile(() => effect.isPlaying);

        effect.Stop(); // Ensure the effect is stopped
        effect.gameObject.SetActive(false);
        effect.transform.SetParent(originalParent); // Reset the parent to the original (ProjectileManager)
        deathEffectPool.Enqueue(effect);
    }

    public void ReturnProjectileToPool(ProjectileStateBased projectile)
    {
        if (projectile == null)
        {
            ConditionalDebug.LogWarning("Attempted to return null projectile to pool.");
            return;
        }

        projectile.ResetForPool();
        projectile.gameObject.SetActive(false);

        // Reset the material to the original
        if (projectile.modelRenderer != null && projectilePrefab.modelRenderer != null)
        {
            projectile.modelRenderer.sharedMaterial = projectilePrefab.modelRenderer.sharedMaterial;
        }

        // Detach and return Radar Symbol to pool
        foreach (Transform child in projectile.transform)
        {
            if (child.gameObject.CompareTag("RadarSymbol"))
            {
                ReturnRadarSymbolToPool(child.gameObject);
            }
        }

        if (!projectilePool.Contains(projectile))
        {
            projectilePool.Enqueue(projectile);
            ConditionalDebug.Log($"Projectile returned to pool. Pool size: {projectilePool.Count}");
        }
        else
        {
            ConditionalDebug.LogWarning("Attempted to return a projectile that's already in the pool.");
        }

        UnregisterProjectile(projectile);
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
                // Example: Always target the player
                Transform playerTransform = GameObject.FindWithTag("Player Aim Target").transform;
                projectile.currentTarget = playerTransform;

                // Extend this logic based on your game's needs, such as targeting the closest enemy, etc.
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
        // Check if the projectile is from a locked-on shot
        if (projectile.GetCurrentState() is PlayerShotState)
        {
            // Use the assigned Crosshair reference to remove the enemy from the locked-on list
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
        // Clear existing projectiles and reinitialize pools
        ClearNativeArray(projectileIds);
        projectileLifetimes.Clear();
        projectileLookup.Clear();
        projectilePool.Clear();
        deathEffectPool.Clear();

        InitializeProjectilePool();
        InitializeDeathEffectPool();

        // Re-register enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            var enemySetup = enemy.GetComponent<EnemyBasicSetup>();
            if (enemySetup != null)
            {
                // Re-register enemy projectiles or any other necessary components
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

        // Clear all collections
        ClearNativeArray(projectileIds);
        projectileLifetimes.Clear();
        projectileLookup.Clear();

        // Clear the projectile requests queue
        projectileRequests.Clear();

        ConditionalDebug.Log($"Cleared all projectiles. Pools: Projectile={projectilePool.Count}");
    }

    public void PlayOneShotSound(string soundEvent, Vector3 position)
    {
        FMODUnity.RuntimeManager.PlayOneShot(soundEvent, position);
    }

    public ProjectileStateBased GetLastCreatedProjectile()
    {
        return lastCreatedProjectile;
    }

    public VisualEffect GetLockedFXFromPool()
    {
        if (lockedFXPool.Count == 0)
        {
            VisualEffect newEffect = Instantiate(lockedFXPrefab, transform);
            return newEffect;
        }
        return lockedFXPool.Dequeue();
    }

    public void ReturnLockedFXToPool(VisualEffect effect)
    {
        effect.Stop();
        effect.gameObject.SetActive(false);
        effect.transform.SetParent(transform);
        lockedFXPool.Enqueue(effect);
    }

    private void CheckAndReplenishPool()
    {
        if (projectilePool.Count < initialPoolSize / 2)
        {
            int toAdd = initialPoolSize - projectilePool.Count;
            for (int i = 0; i < toAdd; i++)
            {
                ProjectileStateBased proj = Instantiate(projectilePrefab, transform);
                proj.gameObject.SetActive(false);
                projectilePool.Enqueue(proj);
            }
            ConditionalDebug.Log($"Replenished projectile pool. New size: {projectilePool.Count}");
        }
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
}
