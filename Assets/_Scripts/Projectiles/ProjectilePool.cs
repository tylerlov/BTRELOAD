using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }

    [SerializeField] private ProjectileStateBased projectilePrefab;
    [SerializeField] private int initialPoolSize = 100;
    private Queue<ProjectileStateBased> projectilePool = new Queue<ProjectileStateBased>();
    private Queue<ProjectileRequest> projectileRequestQueue = new Queue<ProjectileRequest>();

    private const int MAX_POOL_SIZE = 1000; // Add a maximum pool size
    private int totalProjectilesCreated = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewProjectile();
        }
    }

    public ProjectileStateBased GetProjectile()
    {
        ProjectileStateBased projectile = null;

        // Try to get from pool first
        while (projectilePool.Count > 0)
        {
            projectile = projectilePool.Dequeue();
            if (projectile != null && !projectile.gameObject.activeInHierarchy)
            {
                ConditionalDebug.Log($"Retrieved projectile from pool. Pool size: {projectilePool.Count}");
                return projectile;
            }
        }

        // Create new if pool is empty and we haven't hit the limit
        if (totalProjectilesCreated < MAX_POOL_SIZE)
        {
            projectile = CreateNewProjectile();
            totalProjectilesCreated++;
            ConditionalDebug.Log($"Created new projectile. Total created: {totalProjectilesCreated}");
            return projectile;
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
        
        // Set parent back to pool
        projectile.transform.SetParent(transform);
        
        // Return to pool if not already in it
        if (!projectilePool.Contains(projectile))
        {
            projectilePool.Enqueue(projectile);
            ConditionalDebug.Log($"Returned projectile to pool. Pool size: {projectilePool.Count}");
        }
    }

    public void ClearPool()
    {
        while (projectilePool.Count > 0)
        {
            var projectile = projectilePool.Dequeue();
            if (projectile != null)
            {
                Destroy(projectile.gameObject);
            }
        }
        totalProjectilesCreated = 0;
        projectileRequestQueue.Clear();
        ConditionalDebug.Log("Pool cleared");
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
        InitializePool();
    }

    public bool HasPendingRequests()
    {
        return projectileRequestQueue.Count > 0;
    }

    private void Update()
    {
        if (projectilePool.Count < initialPoolSize * 0.2f) // If pool is less than 20% full
        {
            ConditionalDebug.LogWarning($"Pool running low. Current size: {projectilePool.Count}");
        }
    }

    private ProjectileStateBased CreateNewProjectile()
    {
        ProjectileStateBased newProjectile = Instantiate(projectilePrefab, transform);
        newProjectile.gameObject.SetActive(false);
        newProjectile.Initialize(); // Make sure this method exists in ProjectileStateBased
        ConditionalDebug.Log($"Created new projectile. Total: {totalProjectilesCreated + 1}");
        return newProjectile;
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
