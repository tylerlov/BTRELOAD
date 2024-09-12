using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chronos;

public class ProjectileSpawner : MonoBehaviour
{
    // Singleton instance
    public static ProjectileSpawner Instance { get; private set; }

    private Dictionary<int, Material> materialLookup = new Dictionary<int, Material>();

    [SerializeField] private ProjectileStateBased projectilePrefab;
    [SerializeField] private int maxEnemyShotsPerInterval = 4;
    [SerializeField] private float enemyShotIntervalSeconds = 3f;
    private Queue<Action> enemyShotQueue = new Queue<Action>();
    private int currentEnemyShotCount = 0;
    private float lastEnemyShotResetTime;

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

        StartCoroutine(ProcessEnemyShotQueue());
        lastEnemyShotResetTime = Time.time;
    }

    public void ProcessShootProjectile(ProjectileRequest request)
    {
        ProjectileStateBased projectile = projectilePool.GetProjectileFromPool();
        if (projectile == null)
        {
            ConditionalDebug.LogWarning("[ProjectileSpawner] No projectile available in pool, skipping shot.");
            return;
        }

        SetupProjectile(projectile, request);
        projectileManager.RegisterProjectile(projectile);
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
        projectile.SetAccuracy(projectileManager.projectileAccuracy);

        if (!string.IsNullOrEmpty(request.ClockKey))
        {
            projectile.SetClock(request.ClockKey);
        }

        projectile.SetAccuracy(request.Accuracy);

        effectManager.CreateEnemyShotFX(projectile.transform, Vector3.zero, Vector3.one * request.UniformScale);
    }

    public bool RequestEnemyShot(Action shotAction)
    {
        float timeScale = globalClock.timeScale;

        if (timeScale <= 0)
        {
            return false; // Don't spawn projectiles when time is stopped or rewinding
        }

        // Check if rate limiting is disabled
        if (maxEnemyShotsPerInterval == -1 && enemyShotIntervalSeconds == -1)
        {
            shotAction.Invoke();
            return true;
        }

        // Apply rate limiting if enabled
        if (enemyShotIntervalSeconds != -1 && Time.time - lastEnemyShotResetTime >= enemyShotIntervalSeconds / timeScale)
        {
            currentEnemyShotCount = 0;
            lastEnemyShotResetTime = Time.time;
        }

        if (maxEnemyShotsPerInterval == -1 || currentEnemyShotCount < maxEnemyShotsPerInterval)
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
            float timeScale = globalClock.timeScale;
            
            if (timeScale > 0 && enemyShotQueue.Count > 0)
            {
                Action shotAction = enemyShotQueue.Dequeue();
                shotAction.Invoke();
                
                // Wait for the adjusted interval based on time scale, if interval is set
                if (enemyShotIntervalSeconds != -1)
                {
                    yield return new WaitForSeconds(enemyShotIntervalSeconds / timeScale);
                }
                else
                {
                    yield return null;
                }
            }
            else
            {
                yield return null;
            }
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
            projectileManager.projectileAccuracy // using the projectileAccuracy field from ProjectileManager
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
            ConditionalDebug.Log("[ProjectileSpawner] Enemy shot request denied due to rate limiting.");
        }

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

    public ProjectileStateBased GetLastCreatedProjectile()
    {
        return lastCreatedProjectile;
    }

    public void SetShotRates(int maxShots, float intervalSeconds)
    {
        maxEnemyShotsPerInterval = maxShots;
        enemyShotIntervalSeconds = intervalSeconds;
        
        // Reset the current shot count and timer when changing rates
        currentEnemyShotCount = 0;
        lastEnemyShotResetTime = Time.time;
        
        ConditionalDebug.Log($"[ProjectileSpawner] Shot rates updated: Max shots = {maxShots}, Interval = {intervalSeconds}s");
    }
}