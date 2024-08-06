using Chronos;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; 
using SickscoreGames.HUDNavigationSystem; 
using UnityEngine.VFX;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    [SerializeField] private ProjectileStateBased projectilePrefab; // Assign in inspector
    [SerializeField] private GameObject enemyShotFXPrefab; // Assign in inspector
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private ParticleSystem deathEffectPrefab; // Assign in inspector
    [SerializeField] private int initialDeathEffectPoolSize = 10;
    [SerializeField] private int staticShootingRequestsPerFrame = 10; // Configurable batch size

    private NativeList<int> projectileIds;
    private NativeHashMap<int, float> projectileLifetimes;
    private Dictionary<int, ProjectileStateBased> projectileLookup = new Dictionary<int, ProjectileStateBased>();
    private Queue<ProjectileStateBased> projectilePool = new Queue<ProjectileStateBased>(200);
    private Queue<ParticleSystem> deathEffectPool = new Queue<ParticleSystem>(50);

    [SerializeField] private Timekeeper timekeeper;
    private Dictionary<GameObject, Vector3> lastPositions = new Dictionary<GameObject, Vector3>();
    private Crosshair crosshair;

    private GameObject playerGameObject; // Cache for player GameObject

     [Range(0f, 1f)]
    public float projectileAccuracy = 0.5f; // 0 = always miss, 1 = perfect accuracy

    public GameObject projectileRadarSymbol;
    [SerializeField] private int radarSymbolPoolSize = 50;
    [SerializeField] private List<GameObject> radarSymbolPool = new List<GameObject>(50);

    [SerializeField] private int maxEnemyShotsPerInterval = 4;
    [SerializeField] private float enemyShotIntervalSeconds = 3f;
    private Queue<Action> enemyShotQueue = new Queue<Action>();
    private int currentEnemyShotCount = 0;
    private float lastEnemyShotResetTime;

    private ProjectileStateBased lastCreatedProjectile;

    [SerializeField] private VisualEffect lockedFXPrefab;
    [SerializeField] private int initialLockedFXPoolSize = 10;
    private Queue<VisualEffect> lockedFXPool = new Queue<VisualEffect>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Initialize collections
        projectileIds = new NativeList<int>(100, Allocator.Persistent);
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
        radarSymbolPool = new List<GameObject>(radarSymbolPoolSize);
        for (int i = 0; i < radarSymbolPoolSize; i++)
        {
            GameObject radarSymbol = Instantiate(projectileRadarSymbol, transform);
            radarSymbol.SetActive(false);
            radarSymbolPool.Add(radarSymbol);
        }
    }

     public void ReturnRadarSymbolToPool(GameObject radarSymbol)
    {
        radarSymbol.SetActive(false);
        radarSymbol.transform.SetParent(transform); // Reset parent to ProjectileManager
        if (!radarSymbolPool.Contains(radarSymbol))
        {
            radarSymbolPool.Add(radarSymbol);
        }
    }

    public GameObject GetRadarSymbolFromPool()
    {
        if (radarSymbolPool.Count > 0)
        {
            GameObject radarSymbol = radarSymbolPool[radarSymbolPool.Count - 1];
            radarSymbolPool.RemoveAt(radarSymbolPool.Count - 1);
            radarSymbol.SetActive(true);
            return radarSymbol;
        }
        return null; // Return null if no symbols are available
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
        projectileIds.Clear();
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

        if (projectileIds.IsCreated) projectileIds.Dispose();
        if (projectileLifetimes.IsCreated) projectileLifetimes.Dispose();
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

    public void ShootProjectile(Vector3 position, Quaternion rotation, float speed, float lifetime, float uniformScale, bool enableHoming = false, Material material = null)
    {
        int materialId = RegisterMaterial(material);
        projectileRequests.Enqueue(new ProjectileRequest(position, rotation, speed, lifetime, uniformScale, enableHoming, materialId));
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
                UpdatedLifetimes[index] = -1f; // Indicate that this projectile should be removed
            }
        }
    }

    private void Update()
    {
        float globalTimeScale = timekeeper.Clock("Test").localTimeScale;
        float deltaTime = Time.deltaTime;

        var projectileIdsArray = new NativeArray<int>(projectileIds.AsArray(), Allocator.TempJob);
        var updatedLifetimes = new NativeArray<float>(projectileIds.Length, Allocator.TempJob);
        var updateJob = new UpdateProjectilesJob
        {
            ProjectileIds = projectileIdsArray,
            ProjectileLifetimes = projectileLifetimes,
            UpdatedLifetimes = updatedLifetimes,
            DeltaTime = deltaTime,
            GlobalTimeScale = globalTimeScale
        };

        JobHandle jobHandle = updateJob.Schedule(projectileIds.Length, 64);
        jobHandle.Complete();

        projectileIdsArray.Dispose();

        // Process updated projectiles
        for (int i = projectileIds.Length - 1; i >= 0; i--)
        {
            int projectileId = projectileIds[i];
            if (projectileLookup.TryGetValue(projectileId, out ProjectileStateBased projectile))
            {
                projectile.CustomUpdate(globalTimeScale);

                float lifetime = updatedLifetimes[i];
                if (lifetime <= 0)
                {
                    projectile.Death();
                    projectileIds.RemoveAt(i);
                    projectileLifetimes.Remove(projectileId);
                    projectileLookup.Remove(projectileId);
                }
                else
                {
                    projectileLifetimes[projectileId] = lifetime;
                }
            }
            else
            {
                projectileIds.RemoveAt(i);
                projectileLifetimes.Remove(projectileId);
                projectileLookup.Remove(projectileId);
            }
        }

        updatedLifetimes.Dispose();

        ProcessProjectileRequests();
    }

    [BurstCompile]
    private struct ProcessProjectileRequestsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<ProjectileRequest> Requests;
        [WriteOnly] public NativeArray<int> NewProjectileIds;
        [WriteOnly] public NativeArray<float> NewProjectileLifetimes;

        public void Execute(int index)
        {
            var request = Requests[index];
            int projectileId = index; // We'll use the index as a temporary ID
            NewProjectileIds[index] = projectileId;
            NewProjectileLifetimes[index] = request.Lifetime;
        }
    }

    private void ProcessProjectileRequests()
    {
        int processCount = Math.Min(projectileRequests.Count, staticShootingRequestsPerFrame);
        if (processCount == 0) return;

        var requestsArray = new NativeArray<ProjectileRequest>(processCount, Allocator.TempJob);
        var newProjectileIds = new NativeArray<int>(processCount, Allocator.TempJob);
        var newProjectileLifetimes = new NativeArray<float>(processCount, Allocator.TempJob);

        for (int i = 0; i < processCount; i++)
        {
            requestsArray[i] = projectileRequests.Dequeue();
        }

        var processJob = new ProcessProjectileRequestsJob
        {
            Requests = requestsArray,
            NewProjectileIds = newProjectileIds,
            NewProjectileLifetimes = newProjectileLifetimes
        };

        JobHandle jobHandle = processJob.Schedule(processCount, 64);
        jobHandle.Complete();

        // Process the results
        for (int i = 0; i < processCount; i++)
        {
            int newProjectileId = newProjectileIds[i];
            float newLifetime = newProjectileLifetimes[i];
            projectileIds.Add(newProjectileId);
            projectileLifetimes[newProjectileId] = newLifetime;
            ProcessShootProjectile(requestsArray[i], newProjectileId);
        }

        requestsArray.Dispose();
        newProjectileIds.Dispose();
        newProjectileLifetimes.Dispose();
    }

    private void ProcessShootProjectile(ProjectileRequest request, int projectileId)
    {
        if (projectilePool.Count == 0)
        {
            ConditionalDebug.LogWarning("[ProjectileManager] No projectile available in pool, skipping shot.");
            return;
        }

        ProjectileStateBased projectile = projectilePool.Dequeue();
        projectile.transform.SetParent(transform); // Ensure the projectile is parented to ProjectileManager
        projectile.transform.position = request.Position;
        projectile.transform.rotation = request.Rotation;
        projectile.gameObject.SetActive(true);
        projectile.transform.localScale = Vector3.one * request.UniformScale; // Apply uniform scale

        // Ensure Rigidbody is not kinematic
        projectile.rb.isKinematic = false;
        projectile.rb.velocity = request.Rotation * Vector3.forward * request.Speed; // Assumes the projectile has a Rigidbody component
        projectile.bulletSpeed = request.Speed; // Set the bullet speed here

        projectile.SetLifetime(request.Lifetime); // Set the lifetime of the projectile
        projectile.EnableHoming(request.EnableHoming); // Set homing based on the parameter

        // Swap the material if a new one is provided and it's not already on the projectile
        Material material = GetMaterialById(request.MaterialId);
        if (material != null && projectile.modelRenderer.material != material)
        {
            projectile.modelRenderer.material = material;
            projectile.modelRenderer.material.color = material.color;
        }

        RegisterProjectile(projectile, projectileId);
    }

    private void RegisterProjectile(ProjectileStateBased projectile, int projectileId)
    {
        projectileLookup[projectileId] = projectile;
    }

    private struct ProjectileRequest
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Speed;
        public float Lifetime;
        public float UniformScale;
        public bool EnableHoming;
        public int MaterialId;

        public ProjectileRequest(Vector3 position, Quaternion rotation, float speed, float lifetime, float uniformScale, bool enableHoming, int materialId)
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

    public ProjectileStateBased ShootProjectileFromEnemy(Vector3 position, Quaternion rotation, float speed, float lifetime, float uniformScale, bool enableHoming = false, Material material = null, string clockKey = "", float accuracy = 1f)
    {
        bool shotRequested = RequestEnemyShot(() =>
        {
            if (projectilePool.Count == 0)
            {
                Debug.LogWarning("[ProjectileManager] No projectile available in pool, skipping shot.");
                return;
            }

            ProjectileStateBased projectile = projectilePool.Dequeue();
            projectile.transform.SetParent(transform);
            projectile.transform.position = position;
            projectile.transform.rotation = rotation;
            projectile.gameObject.SetActive(true);
            projectile.transform.localScale = Vector3.one * uniformScale; // Set the scale of the projectile

            // Check if the Enemy Shot FX prefab is assigned before instantiating
            if (enemyShotFXPrefab != null)
            {
                GameObject enemyShotFX = Instantiate(enemyShotFXPrefab, projectile.transform);
                enemyShotFX.transform.localPosition = Vector3.zero; // Center it on the projectile
                enemyShotFX.transform.localScale = Vector3.one * uniformScale; // Set the scale of the FX to match the projectile
                SetChildrenScale(enemyShotFX, Vector3.one * uniformScale); // Recursively set scale for all children
                enemyShotFX.SetActive(true);
            }

             if (radarSymbolPool.Count > 0)
            {
                GameObject radarSymbol = GetRadarSymbolFromPool();
                radarSymbol.transform.SetParent(projectile.transform);
                radarSymbol.transform.localPosition = Vector3.zero; // Center it on the projectile
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

            // Set the accuracy for this specific projectile
            projectile.SetAccuracy(accuracy);

            RegisterProjectile(projectile);

            lastCreatedProjectile = projectile; // Store the last created projectile
        });

        if (!shotRequested)
        {
            ConditionalDebug.Log("[ProjectileManager] Enemy shot request denied due to rate limiting.");
        }

        return lastCreatedProjectile;
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
            ConditionalDebug.LogWarning("Player GameObject not found. Effect will not follow the player.");
            effect.transform.SetParent(transform); // Fallback to the default parent
        }
        effect.transform.position = position;
        effect.gameObject.SetActive(true);
        effect.Play();

        // Return the effect to the pool after it has finished playing
        StartCoroutine(ReturnEffectToPoolAfterFinished(effect, playerGameObject != null ? playerGameObject.transform : transform));
    }

    private IEnumerator ReturnEffectToPoolAfterFinished(ParticleSystem effect, Transform originalParent)
    {
        yield return new WaitWhile(() => effect.isPlaying);

        effect.Stop(); // Ensure the effect is stopped
        effect.gameObject.SetActive(false);
        effect.transform.SetParent(originalParent); // Reset the parent to the original (ProjectileManager)
        deathEffectPool.Enqueue(effect);
    }

    public void ReturnProjectileToPool(ProjectileStateBased projectile)
{
    projectile.ResetForPool();
    projectile.gameObject.SetActive(false);

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
    }

    UnregisterProjectile(projectile);
}

    public void RegisterProjectile(ProjectileStateBased projectile)
{
    int projectileId = projectile.GetInstanceID();
    if (!projectileLookup.ContainsKey(projectileId))
    {
        projectileIds.Add(projectileId);
        projectileLifetimes[projectileId] = projectile.lifetime;
        projectileLookup[projectileId] = projectile;
        ConditionalDebug.Log($"Registered projectile: {projectile.name}");
    }
}

    public void UnregisterProjectile(ProjectileStateBased projectile)
    {
        if (projectile == null) return;

        int projectileId = projectile.GetInstanceID();
        if (projectileLookup.ContainsKey(projectileId))
        {
            for (int i = 0; i < projectileIds.Length; i++)
            {
                if (projectileIds[i] == projectileId)
                {
                    projectileIds.RemoveAt(i);
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
            if (projectileLookup.TryGetValue(projectileId, out ProjectileStateBased projectile) && projectile.homing)
            {
                // Example: Always target the player
                Transform playerTransform = GameObject.FindWithTag("Player Aim Target").transform;
                projectile.currentTarget = playerTransform;

                // Extend this logic based on your game's needs, such as targeting the closest enemy, etc.
            }
        }
    }

    public void PredictAndRotateProjectile(ProjectileStateBased projectile)
    {
        if (projectile.currentTarget == null) return;

        Vector3 targetVelocity = CalculateTargetVelocity(projectile.currentTarget.gameObject);
        Vector3 toTarget = projectile.currentTarget.position - projectile.transform.position;
        float distanceToTarget = toTarget.magnitude;
        float projectileSpeed = projectile.bulletSpeed;

        float predictionTime = distanceToTarget / projectileSpeed;
        Vector3 predictedPosition = projectile.currentTarget.position + targetVelocity * predictionTime;

        projectile.predictedPosition = predictedPosition;
    }

    private Vector3 CalculateTargetVelocity(GameObject target)
    {
        Vector3 currentPos = target.transform.position;
        Vector3 previousPos = Vector3.zero;
        if (lastPositions.ContainsKey(target))
        {
            previousPos = lastPositions[target];
        }
        else
        {
            lastPositions.Add(target, currentPos);
        }

        Vector3 velocity = (currentPos - previousPos) / Time.deltaTime;
        lastPositions[target] = currentPos; // Update the last known position
        return velocity;
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
    projectileIds.Clear();
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
    projectileIds.Clear();
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

    private Dictionary<int, Material> materialLookup = new Dictionary<int, Material>();

    private int RegisterMaterial(Material material)
    {
        if (material == null) return -1;
        int id = material.GetInstanceID();
        if (!materialLookup.ContainsKey(id))
        {
            materialLookup[id] = material;
        }
        return id;
    }

    private Material GetMaterialById(int materialId)
    {
        return materialId != -1 && materialLookup.TryGetValue(materialId, out Material material) ? material : null;
    }
}
