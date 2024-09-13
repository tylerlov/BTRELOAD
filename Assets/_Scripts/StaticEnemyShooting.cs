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
        // Remove the UpdateDirectionToTarget() call as it's no longer needed
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
        if (this == null || !gameObject.activeInHierarchy || EnemyShootingManager.Instance == null)
        {
            return;
        }

        float currentTime = EnemyShootingManager.Instance.GetCurrentTime();
        if (currentTime - lastShootTime < minTimeBetweenShots)
        {
            ConditionalDebug.Log(
                $"[StaticEnemyShooting] Skipped shot due to rapid firing at {currentTime}"
            );
            return;
        }

        PerformShoot();
        lastShootTime = currentTime;
    }

    private void PerformShoot()
    {
        if (ProjectileManager.Instance == null)
        {
            ConditionalDebug.LogError("[StaticEnemyShooting] ProjectileManager instance is null.");
            return;
        }

        ProjectileSpawner.Instance.ShootProjectile(
            cachedTransform.position,
            Quaternion.LookRotation(Vector3.up), // Change this to shoot upward
            shootSpeed,
            projectileLifetime,
            projectileScale,
            false,
            alternativeProjectileMaterial
        );
    }

    // Remove the UpdateDirectionToTarget() method as it's no longer needed
}
