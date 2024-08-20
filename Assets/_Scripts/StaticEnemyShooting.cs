using UnityEngine;
using Chronos;
using PrimeTween;

public class StaticEnemyShooting : MonoBehaviour
{
    [SerializeField] private string enemyType;
    [SerializeField] private float shootSpeed = 20f;
    [SerializeField] private float projectileLifetime = 5f;
    [SerializeField] private float projectileScale = 1f;
    [SerializeField] private Material alternativeProjectileMaterial;
    [SerializeField] private Transform target;
    [SerializeField] private float shrinkDuration = 0.15f;
    [SerializeField] private float growDuration = 0.1f;
    [SerializeField] private float stretchFactor = 1.2f;

    private Timeline myTime;
    private Vector3 originalScale;
    private bool isShooting = false;
    private Vector3 stretchedScale;
    private Vector3 directionToTarget;
    private Transform cachedTransform;
    private Sequence shootSequence;

    void Awake()
    {
        myTime = GetComponent<Timeline>();
        cachedTransform = transform;
        originalScale = cachedTransform.localScale;
        stretchedScale = originalScale * stretchFactor;
        UpdateDirectionToTarget();
        
        // Create the initial shooting sequence
        shootSequence = CreateShootSequence();
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
        shootSequence.Stop();
        shootSequence = CreateShootSequence();
    }

    private Sequence CreateShootSequence()
    {
        Vector3 currentScale = transform.localScale;
        Vector3 targetScale = new Vector3(0.04f, 0.04f, 0.04f);
        
        // Add a small offset if the scales are very close
        if (Vector3.Distance(currentScale, targetScale) < 0.001f)
        {
            targetScale += new Vector3(0.01f, 0.01f, 0.01f);
        }

        return Sequence.Create()
            .Chain(Tween.Scale(transform, targetScale, 0.1f))
            .Chain(Tween.Scale(transform, currentScale, 0.1f));
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