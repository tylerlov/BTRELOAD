using System;
using System.Collections;
using System.Collections.Generic;
using Chronos;
using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int OpacityProperty = Shader.PropertyToID("_Opacity");
    private static readonly int TimeOffsetProperty = Shader.PropertyToID("_TimeOffset");

    // Singleton instance
    public static ProjectileSpawner Instance { get; private set; }

    private Dictionary<int, Material> materialLookup = new Dictionary<int, Material>();

    [SerializeField]
    private ProjectileStateBased projectilePrefab;

    [SerializeField]
    private int maxEnemyShotsPerInterval = 4;

    [SerializeField]
    private float enemyShotIntervalSeconds = 3f;
    private Queue<Action> enemyShotQueue = new Queue<Action>();
    private float lastShotTime;
    private int shotsInCurrentInterval;

    private ProjectileStateBased lastCreatedProjectile;

    private ProjectilePool projectilePool;
    private ProjectileEffectManager effectManager;
    private ProjectileManager projectileManager;

    private GlobalClock globalClock;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        projectilePool = GetComponent<ProjectilePool>();
        effectManager = GetComponent<ProjectileEffectManager>();
        projectileManager = GetComponent<ProjectileManager>();

        // Get the global clock
        globalClock = Timekeeper.instance.Clock("Test");
        lastShotTime = Time.time;
        shotsInCurrentInterval = 0;

        StartCoroutine(ProcessEnemyShotQueue());
    }

    public void ProcessShootProjectile(ProjectileRequest request, ProjectileStateBased projectile, bool isStatic)
    {
        projectile.transform.position = request.Position;
        projectile.transform.rotation = request.Rotation;
        projectile.gameObject.SetActive(true);
        projectile.transform.localScale = Vector3.one * request.UniformScale;

        projectile.rb.isKinematic = false;
        projectile.rb.linearVelocity = request.Rotation * Vector3.forward * request.Speed;
        projectile.bulletSpeed = request.Speed;
        projectile.SetLifetime(request.Lifetime);
        projectile.EnableHoming(request.EnableHoming);

        // Use MaterialPropertyBlock instead of changing material
        if (projectile.modelRenderer != null)
        {
            var propertyBlock = ProjectileEffectManager.Instance.GetPropertyBlock();
            Material material = GetMaterialById(request.MaterialId);
            if (material != null)
            {
                propertyBlock.SetColor(ColorProperty, material.GetColor(ColorProperty));
                propertyBlock.SetFloat(OpacityProperty, material.GetFloat(OpacityProperty));
                projectile.modelRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        // Store the created effect GameObject reference in the projectile
        GameObject shotEffect = effectManager.CreateEnemyShotFX(
            request.Position,
            request.Rotation
        );
        
        // Store the effect reference in the projectile for cleanup
        projectile.SetShotEffect(shotEffect);

        if (!request.IsStatic)
        {
            GameObject radarSymbol = effectManager.GetRadarSymbolFromPool();
            if (radarSymbol != null)
            {
                radarSymbol.transform.SetParent(projectile.transform);
                radarSymbol.transform.localPosition = Vector3.zero;
                radarSymbol.SetActive(true);
                // Store the radar symbol reference in the projectile for cleanup
                projectile.SetRadarSymbol(radarSymbol);
            }
        }

        projectile.initialSpeed = request.Speed;
        projectile.SetAccuracy(projectileManager.projectileAccuracy);

        if (!string.IsNullOrEmpty(request.ClockKey))
        {
            projectile.SetClock(request.ClockKey);
        }

        projectile.SetAccuracy(request.Accuracy);

        effectManager.CreateEnemyShotFX(
            request.Position,
            request.Rotation
        );

        ConditionalDebug.Log($"[ProjectileSpawner] Projectile setup complete. Position: {request.Position}, Speed: {request.Speed}, Lifetime: {request.Lifetime}");
    }

    public bool RequestEnemyShot(Action shotAction)
    {
        float timeScale = globalClock.timeScale;

        if (timeScale <= 0)
        {
            ConditionalDebug.Log("[ProjectileSpawner] Shot denied due to timeScale <= 0");
            return false;
        }

        // Check if rate limiting is disabled
        if (maxEnemyShotsPerInterval == -1 && enemyShotIntervalSeconds == -1)
        {
            ConditionalDebug.Log("[ProjectileSpawner] Rate limiting disabled, executing shot immediately");
            shotAction.Invoke();
            return true;
        }

        enemyShotQueue.Enqueue(shotAction);
        ConditionalDebug.Log($"[ProjectileSpawner] Shot request approved, enqueued. Queue size: {enemyShotQueue.Count}");
        return true;
    }

    private IEnumerator ProcessEnemyShotQueue()
    {
        while (true)
        {
            float timeScale = globalClock.timeScale;

            if (timeScale > 0 && enemyShotQueue.Count > 0)
            {
                float currentTime = Time.time;
                float scaledInterval = enemyShotIntervalSeconds / timeScale;

                if (currentTime - lastShotTime >= scaledInterval)
                {
                    lastShotTime = currentTime;
                    shotsInCurrentInterval = 0;
                }

                if (shotsInCurrentInterval < maxEnemyShotsPerInterval)
                {
                    Action shotAction = enemyShotQueue.Dequeue();
                    shotAction.Invoke();
                    shotsInCurrentInterval++;
                    ConditionalDebug.Log($"[ProjectileSpawner] Processing enemy shot from queue. Remaining in queue: {enemyShotQueue.Count}");

                    // Calculate time to next shot, adjusted for time scale
                    float timeToNextShot = (scaledInterval / maxEnemyShotsPerInterval) / timeScale;
                    yield return new WaitForSeconds(timeToNextShot);
                }
                else
                {
                    // Wait for the next interval, adjusted for time scale
                    yield return new WaitForSeconds((scaledInterval - (Time.time - lastShotTime)) / timeScale);
                }
            }
            else
            {
                yield return null;
            }
        }
    }

    public void ShootProjectile(
        Vector3 position,
        Quaternion rotation,
        float speed,
        float lifetime,
        float uniformScale,
        float damage,  // Add this parameter
        bool enableHoming = false,
        Material material = null,
        Transform target = null,
        bool isStatic = false
    )
    {
        ProjectileRequest request = ProjectilePool.Instance.GetProjectileRequest();
        request.Set(
            position,
            rotation,
            speed,
            lifetime,
            uniformScale,
            enableHoming,
            RegisterMaterial(material),
            "", // clockKey (empty for player projectiles)
            projectileManager.projectileAccuracy, // using the projectileAccuracy field from ProjectileManager
            target,
            damage,  // Add this argument
            isStatic
        );
        ProjectilePool.Instance.EnqueueProjectileRequest(request);

        // Log the scale being used in the request
        ConditionalDebug.Log($"[ProjectileSpawner] Enqueued projectile request with scale: {uniformScale}, IsStatic: {isStatic}");
    }

    public ProjectileStateBased ShootProjectileFromEnemy(
        Vector3 position,
        Quaternion rotation,
        float speed,
        float lifetime,
        float uniformScale,
        float damage,
        bool enableHoming = false,
        Material material = null,
        string clockKey = "",
        float accuracy = -1f,
        Transform target = null,
        bool isStatic = false
    )
    {
        ConditionalDebug.Log($"[ProjectileSpawner] ShootProjectileFromEnemy called. Position: {position}, Target: {target?.name}, IsStatic: {isStatic}");
        
        ProjectileRequest request = ProjectilePool.Instance.GetProjectileRequest();
        float finalAccuracy = accuracy >= 0 ? accuracy : ProjectileManager.Instance.projectileAccuracy;
        request.Set(
            position,
            rotation,
            speed,
            lifetime,
            uniformScale,
            enableHoming,
            RegisterMaterial(material),
            clockKey,
            finalAccuracy,
            target,
            damage,
            isStatic
        );

        ProjectileStateBased projectile = projectilePool.GetProjectile();
        ProcessShootProjectile(request, projectile, isStatic);
        ProjectileManager.Instance.RegisterProjectile(projectile);

            ConditionalDebug.Log($"[ProjectileSpawner] Projectile created and registered for enemy. Position: {position}, Speed: {speed}, Target: {(target != null ? target.name : "None")}, IsStatic: {isStatic}");

        return projectile;
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

    public ProjectileStateBased GetLastCreatedProjectile()
    {
        return lastCreatedProjectile;
    }

    public void SetShotRates(int maxShots, float intervalSeconds)
    {
        maxEnemyShotsPerInterval = maxShots;
        enemyShotIntervalSeconds = intervalSeconds;

        // Reset the current shot count and timer when changing rates
        shotsInCurrentInterval = 0;
        lastShotTime = Time.time;

        ConditionalDebug.Log(
            $"[ProjectileSpawner] Shot rates updated: Max shots = {maxShots}, Interval = {intervalSeconds}s"
        );
    }

    private Queue<StaticEnemyProjectileRequest> staticEnemyProjectileQueue = new Queue<StaticEnemyProjectileRequest>();
    private bool isProcessingStaticEnemyProjectiles = false;

    private struct StaticEnemyProjectileRequest
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Speed;
        public float Lifetime;
        public float Scale;
        public float Damage;
        public bool EnableHoming;
        public int MaterialId; // Change from Material to MaterialId
    }

    public void ShootStaticEnemyProjectile(
        Vector3 position,
        Quaternion rotation,
        float speed,
        float lifetime,
        float scale,
        float damage,
        bool enableHoming,
        Material material)
    {
        staticEnemyProjectileQueue.Enqueue(new StaticEnemyProjectileRequest
        {
            Position = position,
            Rotation = rotation,
            Speed = speed,
            Lifetime = lifetime,
            Scale = scale,
            Damage = damage,
            EnableHoming = enableHoming,
            MaterialId = RegisterMaterial(material)
        });

        if (!isProcessingStaticEnemyProjectiles)
        {
            StartCoroutine(ProcessStaticEnemyProjectiles());
        }
    }

    private IEnumerator ProcessStaticEnemyProjectiles()
    {
        isProcessingStaticEnemyProjectiles = true;
        WaitForSeconds shortDelay = new WaitForSeconds(0.02f); // Reduced delay between batches

        while (staticEnemyProjectileQueue.Count > 0)
        {
            int batchSize = Mathf.Min(30, staticEnemyProjectileQueue.Count); // Process up to 30 projectiles per batch
            for (int i = 0; i < batchSize; i++)
            {
                if (staticEnemyProjectileQueue.Count > 0)
                {
                    StaticEnemyProjectileRequest request = staticEnemyProjectileQueue.Dequeue();
                    ProjectileStateBased projectile = projectilePool.GetProjectile();

                    if (projectile != null)
                    {
                        SetupStaticEnemyProjectile(projectile, request);
                        projectile.gameObject.SetActive(true);
                        ConditionalDebug.Log($"[ProjectileSpawner] Static enemy projectile shot from {request.Position}");
                    }
                    else
                    {
                        ConditionalDebug.LogWarning("[ProjectileSpawner] Failed to get projectile from pool for static enemy.");
                    }

                    // Add a small random delay between individual shots in the batch
                    yield return new WaitForSeconds(UnityEngine.Random.Range(0.005f, 0.015f));
                }
            }

            yield return shortDelay; // Short delay between batches
        }

        isProcessingStaticEnemyProjectiles = false;
    }

    private void SetupStaticEnemyProjectile(ProjectileStateBased projectile, StaticEnemyProjectileRequest request)
    {
        projectile.transform.position = request.Position;
        projectile.transform.rotation = request.Rotation;
        projectile.transform.localScale = Vector3.one * request.Scale;
        projectile.SetupProjectile(request.Damage, request.Speed, request.Lifetime, request.EnableHoming, request.Scale, null, true);
        
        if (request.MaterialId != -1 && projectile.modelRenderer != null)
        {
            projectile.modelRenderer.material = GetMaterialById(request.MaterialId);
        }
    }

    public ProjectileStateBased ShootPlayerProjectile(float damage, float speed, float scale)
    {
        if (CrosshairCore.Instance == null)
        {
            ConditionalDebug.LogError("[ProjectileSpawner] CrosshairCore.Instance is null!");
            return null;
        }

        Vector3 shootPosition = CrosshairCore.Instance.RaySpawn.transform.position;
        Quaternion shootRotation = CrosshairCore.Instance.RaySpawn.transform.rotation;

        ConditionalDebug.Log($"[ProjectileSpawner] ShootPlayerProjectile called. Position: {shootPosition}, Rotation: {shootRotation}");

        ProjectileStateBased projectile = ProjectilePool.Instance.GetProjectile();
        
        if (projectile != null)
        {
            projectile.transform.position = shootPosition;
            projectile.transform.rotation = shootRotation;
            projectile.transform.localScale = Vector3.one * scale;
            projectile.SetupProjectile(damage, speed, 10f, false, scale, null, false);
            projectile.gameObject.SetActive(true);

            if (projectile.rb != null)
            {
                projectile.rb.isKinematic = false;
                projectile.rb.linearVelocity = shootRotation * Vector3.forward * speed;
            }

            ProjectileManager.Instance.RegisterProjectile(projectile);

            // Set the projectile state to PlayerShotState
            projectile.ChangeState(new PlayerShotState(projectile, 1f, null, false));

            ConditionalDebug.Log($"[ProjectileSpawner] Player projectile created and shot. Position: {shootPosition}, Speed: {speed}, Velocity: {projectile.rb?.linearVelocity}");
        }
        else
        {
            ConditionalDebug.LogError("[ProjectileSpawner] Failed to get projectile from pool for player shot.");
        }

        return projectile;
    }
}
