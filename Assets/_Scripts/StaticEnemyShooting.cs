using Chronos;
using UnityEngine;

public class StaticEnemyShooting : MonoBehaviour
{
    [SerializeField] private string enemyType;
    [SerializeField] private float shootSpeed = 20f;
    [SerializeField] private float projectileLifetime = 5f;
    [SerializeField] private float projectileScale = 1f;
    [SerializeField] private Material alternativeProjectileMaterial;
    [SerializeField] private Transform target;

    private Timeline myTime;
    private Vector3 directionToTarget;
    private Transform cachedTransform;
    private float lastShootTime = 0f;
    [SerializeField] private float minTimeBetweenShots = 0.1f;

    void Awake()
    {
        myTime = GetComponent<Timeline>();
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
        if (this == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (Time.time - lastShootTime < minTimeBetweenShots)
        {
            ConditionalDebug.Log($"[StaticEnemyShooting] Skipped shot due to rapid firing at {Time.time}");
            return;
        }

        PerformShoot();
        lastShootTime = Time.time;
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
        ConditionalDebug.Log($"[StaticEnemyShooting] Projectile fired from {gameObject.name} at {Time.time}. Position: {cachedTransform.position}, Direction: {directionToTarget}");
    }

    public void UpdateDirectionToTarget()
    {
        directionToTarget = target != null ? (target.position - cachedTransform.position).normalized : cachedTransform.forward;
    }
}