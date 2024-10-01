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
        if (this == null || !gameObject.activeInHierarchy || EnemyShootingManager.Instance == null)
        {
            return;
        }

        float currentTime = EnemyShootingManager.Instance.GetCurrentTime();
        if (currentTime - lastShootTime < minTimeBetweenShots)
        {
            return;
        }

        PerformShoot();
        lastShootTime = currentTime;
    }

    private void PerformShoot()
    {
        if (ProjectileSpawner.Instance == null)
        {
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
            true // Indicates it's from a static enemy
        );
    }
}
