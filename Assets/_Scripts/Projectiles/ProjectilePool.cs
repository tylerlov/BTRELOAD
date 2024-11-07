using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chronos; // Add this line

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
    public int MaterialId;  // Changed from Material to MaterialId
}

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }

    [SerializeField] private ProjectileStateBased projectilePrefab;
    [SerializeField] private int initialPoolSize = 100;
    [SerializeField] private Material projectileMaterial; // Add this line
    private Queue<ProjectileStateBased> projectilePool = new Queue<ProjectileStateBased>();
    private Queue<ProjectileRequest> projectileRequestQueue = new Queue<ProjectileRequest>();

    private const int MAX_POOL_SIZE = 1000; // Add a maximum pool size
    private int totalProjectilesCreated = 0;

    private const int MAX_SIMULTANEOUS_SPAWNS = 5;
    private Queue<ProjectileRequest> spawnQueue = new Queue<ProjectileRequest>();

    private const int BATCH_SIZE = 10;

    // Add these fields at the top of the ProjectilePool class
    private const int INITIALIZATION_BATCH_SIZE = 5; // Smaller batch size
    private const float BATCH_DELAY = 0.02f; // 20ms delay between batches
    private bool isInitializing = false;

    // Add these constants
    private const int TIMELINE_BATCH_SIZE = 2; // Reduce from 3 to 2
    private const float TIMELINE_INIT_DELAY = 0.1f; // Increase delay between Timeline inits
    private const int MAX_TIMELINE_INITS_PER_FRAME = 1; // Limit Timeline initializations per frame
    private int timelineInitsThisFrame = 0;
    private float lastTimelineInitTime = 0f;

    // Add these shader property IDs
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int OpacityProperty = Shader.PropertyToID("_Opacity");
    private static readonly int TimeOffsetProperty = Shader.PropertyToID("_TimeOffset");

    private Transform projectileContainer; // Add this field

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateProjectileContainer(); // Add this line
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CreateProjectileContainer()
    {
        // Create main projectile container
        projectileContainer = new GameObject("Projectiles_Container").transform;
        projectileContainer.SetParent(transform);
    }

    private IEnumerator InitializePoolGradually()
    {
        isInitializing = true;
        int created = 0;
        
        // Create initial batch without Timelines
        for (int i = 0; i < INITIALIZATION_BATCH_SIZE; i++)
        {
            CreateNewProjectile(false);
            created++;
        }

        yield return new WaitForSeconds(BATCH_DELAY);

        // Create the rest gradually
        while (created < initialPoolSize)
        {
            if (Time.deltaTime < 0.033f) // Only create when frame time is good
            {
                int batchSize = Mathf.Min(INITIALIZATION_BATCH_SIZE, initialPoolSize - created);
                for (int i = 0; i < batchSize; i++)
                {
                    var projectile = CreateNewProjectile(false);
                    created++;
                    
                    // More conservative Timeline initialization
                    if (i % (TIMELINE_BATCH_SIZE * 2) == 0) // Double the spacing between Timeline inits
                    {
                        yield return new WaitForSeconds(TIMELINE_INIT_DELAY);
                        StartCoroutine(InitializeTimeline(projectile));
                    }
                }
                yield return new WaitForSeconds(BATCH_DELAY * 1.5f); // Increase delay between batches
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
        if (projectilePool.Count < initialPoolSize * 0.2f) // If pool is less than 20% full
        {
            ConditionalDebug.LogWarning($"Pool running low. Current size: {projectilePool.Count}");
        }

        int spawnsThisFrame = 0;
        while (spawnQueue.Count > 0 && spawnsThisFrame < MAX_SIMULTANEOUS_SPAWNS)
        {
            var request = spawnQueue.Dequeue();
            // Process spawn request
            spawnsThisFrame++;
        }
    }

    private ProjectileStateBased CreateNewProjectile(bool initializeTimeline = true)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile prefab is null!");
            return null;
        }

        ProjectileStateBased newProjectile = Instantiate(projectilePrefab, projectileContainer); // Parent to container
        
        // Set shared material before initialization
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

        // Check if we've hit our per-frame limit
        if (timelineInitsThisFrame >= MAX_TIMELINE_INITS_PER_FRAME)
        {
            yield return new WaitForEndOfFrame();
            timelineInitsThisFrame = 0;
        }

        // Ensure minimum time between Timeline initializations
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
            
            // Activate without triggering full hierarchy
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
                // Initialize Timeline if not already done
                if (projectile.TLine == null)
                {
                    StartCoroutine(InitializeTimeline(projectile));
                    return projectile;
                }
                return projectile;
            }
        }

        // Create new if needed
        if (totalProjectilesCreated < MAX_POOL_SIZE)
        {
            return CreateNewProjectile(true);
        }

        // If we hit the limit, force recycle the oldest projectile
        ProjectileStateBased[] activeProjectiles = FindObjectsOfType<ProjectileStateBased>();
        if (activeProjectiles.Length > 0)
        {
            projectile = activeProjectiles[0];
            projectile.Death(false); // This will return it to pool
            ConditionalDebug.Log("Forced recycling of oldest projectile");
            return GetProjectile(); // Try getting from pool again
        }

        ConditionalDebug.LogWarning("Failed to get projectile from pool or create new one");
        return null;
    }

    public void ReturnProjectile(ProjectileStateBased projectile)
    {
        projectile.gameObject.SetActive(false);
        
        if (projectile.rb != null)
        {
            // Reset velocities only if Rigidbody is not kinematic
            if (!projectile.rb.isKinematic)
            {
                projectile.rb.linearVelocity = Vector3.zero;
                projectile.rb.angularVelocity = Vector3.zero;
            }

            // Now set isKinematic to true
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

    public void ReturnProjectileToPool(ProjectileStateBased projectile)
    {
        if (projectile == null) return;

        // Reset the projectile state
        projectile.ResetForPool();
        
        // Ensure it's inactive
        projectile.gameObject.SetActive(false);
        
        // Set parent back to pool container
        projectile.transform.SetParent(projectileContainer);
        
        // Return to pool if not already in it
        if (!projectilePool.Contains(projectile))
        {
            projectilePool.Enqueue(projectile);
        }
    }

    public void ClearPool()
    {
        // Clear all projectiles in container
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
    public string ClockKey; // Add this line
    public float Accuracy; // Add this line
    public Transform Target; // Add this line
    public float Damage; // Add this line
    public bool IsStatic; // Add this line

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
        IsStatic = isStatic; // Add this line
    }
}
