using UnityEngine;
using Chronos;
using PrimeTween; // Add this line to import PrimeTween

public class StaticEnemyShooting : MonoBehaviour
{
    [SerializeField] private string enemyType;
    [SerializeField] private float shootSpeed = 20f;
    [SerializeField] private float projectileLifetime = 5f;
    [SerializeField] private float projectileScale = 1f;
    [SerializeField] private Material alternativeProjectileMaterial;
    [SerializeField] private Transform target;
    [SerializeField] private float shrinkDuration = 0.15f; // Cooldown time in seconds

    [SerializeField] private float growDuration = 0.1f;
    [SerializeField] private float stretchFactor = 1.2f;

    private Timeline myTime;
    private Vector3 originalScale;
    private Tween currentShrinkTween;
    private bool isShooting = false;
    private Vector3 stretchedScale;
    private Vector3 directionToTarget;
    private Transform cachedTransform; // New cached transform

    void Awake()
    {
        myTime = GetComponent<Timeline>();
        cachedTransform = transform; // Cache the transform
        originalScale = cachedTransform.localScale;
        stretchedScale = originalScale * stretchFactor;
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
        if (this == null || !gameObject.activeInHierarchy || isShooting)
        {
            return;
        }

        isShooting = true;
        currentShrinkTween.Stop();

        // Optimized shooting sequence
        Sequence.Create()
            .Chain(Tween.Scale(cachedTransform, stretchedScale, growDuration, Ease.OutQuad))
            .ChainDelay(0.05f)
            .ChainCallback(PerformShoot)
            .Chain(Tween.Scale(cachedTransform, originalScale, shrinkDuration, Ease.InQuad))
            .ChainCallback(() => isShooting = false);
    }

    private void PerformShoot()
    {
        if (ProjectileManager.Instance == null)
        {
            ConditionalDebug.LogError("[StaticEnemyShooting] ProjectileManager instance is null.");
            return;
        }

        ProjectileManager.Instance.ShootProjectile(cachedTransform.position, Quaternion.LookRotation(directionToTarget), shootSpeed, projectileLifetime, projectileScale, false, alternativeProjectileMaterial);
        ConditionalDebug.Log($"[StaticEnemyShooting] Projectile fired from {gameObject.name} at {Time.time}");
    }

    public void UpdateDirectionToTarget()
    {
        directionToTarget = target != null ? (target.position - cachedTransform.position).normalized : cachedTransform.forward;
    }
}