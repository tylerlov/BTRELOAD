using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }

    [SerializeField] private ProjectileStateBased projectilePrefab;
    [SerializeField] private int initialPoolSize = 10;
    
    private Queue<ProjectileStateBased> projectilePool = new Queue<ProjectileStateBased>(200);
    private Queue<ProjectileRequest> projectileRequestPool = new Queue<ProjectileRequest>();
    private Queue<ProjectileRequest> projectileRequests = new Queue<ProjectileRequest>();

    private void Start()
    {
        InitializeProjectilePool();
    }

    public void InitializeProjectilePool()
    {
        projectilePool = new Queue<ProjectileStateBased>(200);
        for (int i = 0; i < 200; i++)
        {
            ProjectileStateBased proj = Instantiate(projectilePrefab, transform);
            proj.gameObject.SetActive(false);
            projectilePool.Enqueue(proj);
        }
    }

    public ProjectileStateBased GetProjectileFromPool()
    {
        if (projectilePool.Count == 0)
        {
            ConditionalDebug.LogWarning("[ProjectilePool] No projectile available in pool, creating new one.");
            return Instantiate(projectilePrefab, transform);
        }
        return projectilePool.Dequeue();
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

        if (projectile.modelRenderer != null && projectilePrefab.modelRenderer != null)
        {
            projectile.modelRenderer.sharedMaterial = projectilePrefab.modelRenderer.sharedMaterial;
        }

        if (!projectilePool.Contains(projectile))
        {
            projectilePool.Enqueue(projectile);
        }
        else
        {
            ConditionalDebug.LogWarning("Attempted to return a projectile that's already in the pool.");
        }
    }

    public void ClearPool()
    {
        projectilePool.Clear();
        projectileRequests.Clear();
    }

    public void CheckAndReplenishPool()
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
        }
    }

    public ProjectileRequest GetProjectileRequest()
    {
        if (projectileRequestPool.Count > 0)
            return projectileRequestPool.Dequeue();
        return new ProjectileRequest();
    }

    public void ReturnProjectileRequest(ProjectileRequest request)
    {
        projectileRequestPool.Enqueue(request);
    }

    public void EnqueueProjectileRequest(ProjectileRequest request)
    {
        projectileRequests.Enqueue(request);
    }

    public int GetProjectileRequestCount()
    {
        return projectileRequests.Count;
    }

    public ProjectileRequest DequeueProjectileRequest()
    {
        return projectileRequests.Dequeue();
    }

    public void ClearProjectileRequests()
    {
        projectileRequests.Clear();
    }

    public int GetPoolSize()
    {
        return projectilePool.Count;
    }

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
    public string ClockKey;  // Add this line
    public float Accuracy;   // Add this line

    public void Set(Vector3 position, Quaternion rotation, float speed, float lifetime, float uniformScale, bool enableHoming, int materialId, string clockKey, float accuracy)
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
    }
}