using UnityEngine;

public class StaticEnemyShooting : MonoBehaviour
{
    [SerializeField] private string enemyType;
    [SerializeField] private float shootSpeed = 20f;
    [SerializeField] private float projectileLifetime = 5f;
    [SerializeField] private float projectileScale = 1f;
    [SerializeField] private Material alternativeProjectileMaterial;
    [SerializeField] private Transform target;

    private Vector3 directionToTarget;
    private Transform cachedTransform;
    private float lastShootTime = 0f;
    [SerializeField] private float minTimeBetweenShots = 0.1f;

    void Awake()
    {
        cachedTransform = transform;
        UpdateDirectionToTarget();
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
            ConditionalDebug.Log($"[StaticEnemyShooting] Skipped shot due to rapid firing at {currentTime}");
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

        ProjectileManager.Instance.ShootProjectile(
            cachedTransform.position,
            Quaternion.LookRotation(directionToTarget),
            shootSpeed,
            projectileLifetime,
            projectileScale,
            false,
            alternativeProjectileMaterial
        );
        ConditionalDebug.Log($"[StaticEnemyShooting] Projectile fired from {gameObject.name} at {EnemyShootingManager.Instance.GetCurrentTime()}. Position: {cachedTransform.position}, Direction: {directionToTarget}");
    }

    public void UpdateDirectionToTarget()
    {
        directionToTarget = target != null ? (target.position - cachedTransform.position).normalized : cachedTransform.forward;
    }
}