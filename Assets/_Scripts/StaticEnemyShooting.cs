using UnityEngine;

public class StaticEnemyShooting : MonoBehaviour
{
    [SerializeField]
    private string enemyType;

    [SerializeField]
    private float shootSpeed = 25f;

    [SerializeField]
    private float projectileLifetime = 3f;

    [SerializeField]
    private float projectileScale = 1f;

    [SerializeField]
    private Material alternativeProjectileMaterial;

    [SerializeField]
    private Transform target;

    private Transform cachedTransform;
    private float lastShootTime = 0f;

    [SerializeField]
    private float minTimeBetweenShots = 0.1f;

    void Awake()
    {
        cachedTransform = transform;
    }

    public void OnEnable()
    {
        EnemyShootingManager.Instance?.RegisterStaticEnemyShooting(this);
    }

    void OnDisable()
    {
        EnemyShootingManager.Instance?.UnregisterStaticEnemyShooting(this);
    }

    public void Shoot()
    {
        ConditionalDebug.Log($"[StaticEnemyShooting] Shoot method called on {gameObject.name}");
        if (this == null || !gameObject.activeInHierarchy || EnemyShootingManager.Instance == null)
        {
            ConditionalDebug.LogWarning($"[StaticEnemyShooting] Shoot conditions not met for {gameObject.name}");
            return;
        }

        float currentTime = EnemyShootingManager.Instance.GetCurrentTime();
        if (currentTime - lastShootTime < minTimeBetweenShots)
        {
            ConditionalDebug.Log($"[StaticEnemyShooting] Skipped shot due to rapid firing at {currentTime}");
            return;
        }

        PerformShoot();
        lastShootTime = currentTime;
    }

    private void PerformShoot()
    {
        ConditionalDebug.Log($"[StaticEnemyShooting] PerformShoot called for {gameObject.name}");
        if (ProjectileSpawner.Instance == null)
        {
            ConditionalDebug.LogError("[StaticEnemyShooting] ProjectileSpawner instance is null.");
            return;
        }

        ProjectileSpawner.Instance.ShootProjectileFromEnemy(
            cachedTransform.position,
            Quaternion.LookRotation(Vector3.up),
            shootSpeed,
            projectileLifetime,
            projectileScale,
            10f,
            enableHoming: false,
            alternativeProjectileMaterial,
            "",
            -1f,
            null,
            true // Add this parameter to indicate it's from a static enemy
        );

        ConditionalDebug.Log($"[StaticEnemyShooting] Projectile shot from {gameObject.name}");
    }

    // Remove the UpdateDirectionToTarget() method as it's no longer needed
}
