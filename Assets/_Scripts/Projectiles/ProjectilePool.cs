using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chronos;
using System.Linq;

public struct ProjectileSpawnRequest
{
    public Vector3 Position;
    public Quaternion Rotation;
    public float Speed;
    public float Lifetime;
    public float Scale;
    public float Damage;
    public bool EnableHoming;
    public Transform Target;
    public bool IsStatic;
    public int MaterialId;  
}

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }

    [SerializeField] private ProjectileStateBased projectilePrefab;
    [SerializeField] private int initialPoolSize = 350; // Increased from 100 to handle peak load
    [SerializeField] private Material projectileMaterial;
    
    [Header("Dynamic Pool Settings")]
    [SerializeField] private bool enableDynamicPooling = true;
    [SerializeField] private float growthThreshold = 0.9f; // Increased to be more aggressive with growth
    [SerializeField] private float shrinkThreshold = 0.4f; // Adjusted to prevent too frequent shrinking
    [SerializeField] private int growthAmount = 50; // Increased for better burst handling
    [SerializeField] private float poolCheckInterval = 1f; // Reduced from 2f for more responsive scaling
    
    [Header("Pre-warming Settings")]
    [SerializeField] private bool enablePrewarming = true;
    [SerializeField] private int commonProjectileCount = 50; 
    [SerializeField] private float preWarmingInterval = 5f; 

    private Queue<ProjectileStateBased> projectilePool = new Queue<ProjectileStateBased>();
    private Queue<ProjectileRequest> projectileRequestQueue = new Queue<ProjectileRequest>();
    private HashSet<ProjectileStateBased> activeProjectiles = new HashSet<ProjectileStateBased>();

    private float lastPoolCheckTime = 0f;
    private float lastPreWarmTime = 0f;
    private int peakActiveCount = 0;
    private float poolUtilization = 0f;

    private const int MAX_POOL_SIZE = 1000; 
    private int totalProjectilesCreated = 0;

    private const int MAX_SIMULTANEOUS_SPAWNS = 5;
    private Queue<ProjectileRequest> spawnQueue = new Queue<ProjectileRequest>();

    private const int BATCH_SIZE = 10;

    private const int INITIALIZATION_BATCH_SIZE = 5; 
    private const float BATCH_DELAY = 0.02f; 
    private bool isInitializing = false;

    private const int TIMELINE_BATCH_SIZE = 2; 
    private const float TIMELINE_INIT_DELAY = 0.1f; 
    private const int MAX_TIMELINE_INITS_PER_FRAME = 1; 
    private int timelineInitsThisFrame = 0;
    private float lastTimelineInitTime = 0f;

    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int OpacityProperty = Shader.PropertyToID("_Opacity");
    private static readonly int TimeOffsetProperty = Shader.PropertyToID("_TimeOffset");

    private Transform projectileContainer; 

    private class PoolStats
    {
        public int TotalRequests { get; set; }
        public int PeakActiveCount { get; set; }
        public float AverageActiveTime { get; set; }
        public Queue<float> RecentUtilization { get; set; } = new Queue<float>();
        
        public void AddUtilization(float utilization)
        {
            RecentUtilization.Enqueue(utilization);
            if (RecentUtilization.Count > 10) 
                RecentUtilization.Dequeue();
        }
        
        public float GetAverageUtilization()
        {
            if (RecentUtilization.Count == 0) return 0f;
            return RecentUtilization.Sum() / RecentUtilization.Count;
        }
    }
    
    private PoolStats stats = new PoolStats();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateProjectileContainer(); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CreateProjectileContainer()
    {
        projectileContainer = new GameObject("Projectiles_Container").transform;
        projectileContainer.SetParent(transform);
    }

    private IEnumerator InitializePoolGradually()
    {
        isInitializing = true;
        int created = 0;
        
        for (int i = 0; i < INITIALIZATION_BATCH_SIZE; i++)
        {
            CreateNewProjectile(false);
            created++;
        }

        yield return new WaitForSeconds(BATCH_DELAY);

        while (created < initialPoolSize)
        {
            if (Time.deltaTime < 0.033f) 
            {
                int batchSize = Mathf.Min(INITIALIZATION_BATCH_SIZE, initialPoolSize - created);
                for (int i = 0; i < batchSize; i++)
                {
                    var projectile = CreateNewProjectile(false);
                    created++;
                    
                    if (i % (TIMELINE_BATCH_SIZE * 2) == 0) 
                    {
                        yield return new WaitForSeconds(TIMELINE_INIT_DELAY);
                        StartCoroutine(InitializeTimeline(projectile));
                    }
                }
                yield return new WaitForSeconds(BATCH_DELAY * 1.5f); 
            }
            else
            {
                yield return new WaitForSeconds(BATCH_DELAY * 2);
            }
        }

        isInitializing = false;
    }

    private void Update()
    {
        if (enableDynamicPooling && Time.time - lastPoolCheckTime > poolCheckInterval)
        {
            UpdatePoolSize();
            lastPoolCheckTime = Time.time;
        }

        if (enablePrewarming && Time.time - lastPreWarmTime > preWarmingInterval)
        {
            PreWarmCommonProjectiles();
            lastPreWarmTime = Time.time;
        }

        int activeCount = activeProjectiles.Count;
        peakActiveCount = Mathf.Max(peakActiveCount, activeCount);
        poolUtilization = (float)activeCount / (projectilePool.Count + activeCount);
        stats.AddUtilization(poolUtilization);

        int spawnsThisFrame = 0;
        while (spawnQueue.Count > 0 && spawnsThisFrame < MAX_SIMULTANEOUS_SPAWNS)
        {
            var request = spawnQueue.Dequeue();
            spawnsThisFrame++;
        }
    }

    private void UpdatePoolSize()
    {
        float avgUtilization = stats.GetAverageUtilization();
        
        if (avgUtilization > growthThreshold && totalProjectilesCreated < MAX_POOL_SIZE)
        {
            int growthSize = Mathf.Min(growthAmount, MAX_POOL_SIZE - totalProjectilesCreated);
            StartCoroutine(GrowPool(growthSize));
            ConditionalDebug.Log($"Growing pool by {growthSize} projectiles. Current utilization: {avgUtilization:P}");
        }
        else if (avgUtilization < shrinkThreshold && projectilePool.Count > initialPoolSize)
        {
            int shrinkAmount = Mathf.FloorToInt(projectilePool.Count * 0.2f); 
            ShrinkPool(shrinkAmount);
            ConditionalDebug.Log($"Shrinking pool by {shrinkAmount} projectiles. Current utilization: {avgUtilization:P}");
        }
    }

    private IEnumerator GrowPool(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (Time.deltaTime < 0.033f) 
            {
                CreateNewProjectile(false);
                yield return new WaitForSeconds(0.02f);
            }
            else
            {
                yield return new WaitForSeconds(0.05f);
            }
        }
    }

    private void ShrinkPool(int amount)
    {
        for (int i = 0; i < amount && projectilePool.Count > initialPoolSize; i++)
        {
            if (projectilePool.Count > 0)
            {
                var projectile = projectilePool.Dequeue();
                if (projectile != null)
                {
                    Destroy(projectile.gameObject);
                    totalProjectilesCreated--;
                }
            }
        }
    }

    private void PreWarmCommonProjectiles()
    {
        if (projectilePool.Count < commonProjectileCount)
        {
            int amountToPreWarm = commonProjectileCount - projectilePool.Count;
            StartCoroutine(GrowPool(amountToPreWarm));
            ConditionalDebug.Log($"Pre-warming {amountToPreWarm} common projectiles");
        }
    }

    private ProjectileStateBased CreateNewProjectile(bool initializeTimeline = true)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile prefab is null!");
            return null;
        }

        ProjectileStateBased newProjectile = Instantiate(projectilePrefab, projectileContainer); 
        
        var renderer = newProjectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = projectileMaterial;
        }
        
        newProjectile.InitializeProjectile();
        newProjectile.gameObject.SetActive(false);
        
        if (initializeTimeline)
        {
            StartCoroutine(InitializeTimeline(newProjectile));
        }
        
        projectilePool.Enqueue(newProjectile);
        totalProjectilesCreated++;
        
        return newProjectile;
    }

    private IEnumerator InitializeTimeline(ProjectileStateBased projectile)
    {
        if (projectile == null) yield break;

        if (timelineInitsThisFrame >= MAX_TIMELINE_INITS_PER_FRAME)
        {
            yield return new WaitForEndOfFrame();
            timelineInitsThisFrame = 0;
        }

        float timeSinceLastInit = Time.time - lastTimelineInitTime;
        if (timeSinceLastInit < TIMELINE_INIT_DELAY)
        {
            yield return new WaitForSeconds(TIMELINE_INIT_DELAY - timeSinceLastInit);
        }

        Timeline timeline = projectile.gameObject.GetComponent<Timeline>();
        if (timeline != null)
        {
            timeline.enabled = false;
            timeline.rewindable = true;
            
            var cachedActive = projectile.gameObject.activeSelf;
            projectile.transform.gameObject.SetActive(true);
            timeline.enabled = true;
            projectile.transform.gameObject.SetActive(cachedActive);
        }

        timelineInitsThisFrame++;
        lastTimelineInitTime = Time.time;
    }

    public ProjectileStateBased GetProjectile()
    {
        ProjectileStateBased projectile = null;

        while (projectilePool.Count > 0)
        {
            projectile = projectilePool.Dequeue();
            if (projectile != null && !projectile.gameObject.activeInHierarchy)
            {
                activeProjectiles.Add(projectile);
                stats.TotalRequests++;
                
                if (projectile.TLine == null)
                {
                    StartCoroutine(InitializeTimeline(projectile));
                }
                return projectile;
            }
        }

        if (totalProjectilesCreated < MAX_POOL_SIZE)
        {
            projectile = CreateNewProjectile(true);
            if (projectile != null)
            {
                activeProjectiles.Add(projectile);
                stats.TotalRequests++;
            }
            return projectile;
        }

        if (activeProjectiles.Count > 0)
        {
            projectile = activeProjectiles.First();
            projectile.Death(false); 
            ConditionalDebug.Log("Forced recycling of oldest projectile");
            return GetProjectile(); 
        }

        ConditionalDebug.LogWarning("Failed to get projectile from pool or create new one");
        return null;
    }

    public void ReturnProjectileToPool(ProjectileStateBased projectile)
    {
        if (projectile == null) return;

        activeProjectiles.Remove(projectile);
        projectile.gameObject.SetActive(false);
        
        if (projectile.rb != null)
        {
            if (!projectile.rb.isKinematic)
            {
                projectile.rb.linearVelocity = Vector3.zero;
                projectile.rb.angularVelocity = Vector3.zero;
            }
            projectile.rb.isKinematic = true;
        }
        
        projectilePool.Enqueue(projectile);
    }

    public ProjectileRequest GetProjectileRequest()
    {
        return new ProjectileRequest();
    }

    public void EnqueueProjectileRequest(ProjectileRequest request)
    {
        projectileRequestQueue.Enqueue(request);
    }

    public bool TryDequeueProjectileRequest(out ProjectileRequest request)
    {
        return projectileRequestQueue.TryDequeue(out request);
    }

    public void ClearPool()
    {
        if (projectileContainer != null)
        {
            foreach (Transform child in projectileContainer)
            {
                Destroy(child.gameObject);
            }
        }

        projectilePool.Clear();
        totalProjectilesCreated = 0;
        projectileRequestQueue.Clear();
    }

    public void CheckAndReplenishPool()
    {
        while (projectilePool.Count < initialPoolSize)
        {
            CreateNewProjectile();
        }
    }

    public int GetProjectileRequestCount()
    {
        return projectileRequestQueue.Count;
    }

    public int GetPoolSize()
    {
        return projectilePool.Count;
    }

    public void ClearProjectileRequests()
    {
        projectileRequestQueue.Clear();
    }

    public void InitializeProjectilePool()
    {
        ClearPool();
        StartCoroutine(InitializePoolGradually());
    }

    public bool HasPendingRequests()
    {
        return projectileRequestQueue.Count > 0;
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
    public string ClockKey; 
    public float Accuracy; 
    public Transform Target; 
    public float Damage; 
    public bool IsStatic; 

    public void Set(
        Vector3 position,
        Quaternion rotation,
        float speed,
        float lifetime,
        float uniformScale,
        bool enableHoming,
        int materialId,
        string clockKey,
        float accuracy,
        Transform target,
        float damage,
        bool isStatic
    )
    {
        Position = position;
        Rotation = rotation;
        Speed = speed;
        Lifetime = lifetime;
        UniformScale = uniformScale;
        EnableHoming = enableHoming;
        MaterialId = materialId;
        ClockKey = clockKey;
        Accuracy = accuracy;
        Target = target;
        Damage = damage;
        IsStatic = isStatic; 
    }
}
