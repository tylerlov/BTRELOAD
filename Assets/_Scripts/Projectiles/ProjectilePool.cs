using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }

    [SerializeField] private ProjectileStateBased projectilePrefab;
    [SerializeField] private int initialPoolSize = 100;
    private Queue<ProjectileStateBased> projectilePool = new Queue<ProjectileStateBased>();
    private Queue<ProjectileRequest> projectileRequestQueue = new Queue<ProjectileRequest>();

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

    private ProjectileStateBased CreateNewProjectile()
    {
        ProjectileStateBased newProjectile = Instantiate(projectilePrefab, transform);
        newProjectile.gameObject.SetActive(false);
        projectilePool.Enqueue(newProjectile);
        return newProjectile;
    }

    public ProjectileStateBased GetProjectile()
    {
        if (projectilePool.Count == 0)
        {
            return CreateNewProjectile();
        }
        return projectilePool.Dequeue();
    }

    public void ReturnProjectile(ProjectileStateBased projectile)
    {
        projectile.gameObject.SetActive(false);
        
        if (projectile.rb != null)
        {
            // Reset velocities only if Rigidbody is not kinematic
            if (!projectile.rb.isKinematic)
            {
                projectile.rb.velocity = Vector3.zero;
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
        ReturnProjectile(projectile);
    }

    public void ClearPool()
    {
        projectilePool.Clear();
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
        InitializePool();
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
