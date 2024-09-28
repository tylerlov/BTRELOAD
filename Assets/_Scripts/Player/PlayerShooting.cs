using System.Collections;
using System.Collections.Generic;
using System.Linq; // Add this for LINQ methods
using FMODUnity;
using MoreMountains.Feedbacks;
using PrimeTween;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    private PlayerLocking playerLocking;
    private CrosshairCore crosshairCore;

    #region Shooting Variables
    [Header("Shooting Settings")]
    public float launchDelay = 0.1f;
    public MMF_Player shootFeedback;

    [SerializeField]
    private EventReference randomShootingEvent;

    [SerializeField]
    private EventReference shootTagEvent;

    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private float projectileSpeed = 50f;
    [SerializeField] private float projectileScale = 1f;
    [SerializeField] private float nonTargetedSpeedMultiplier = 2f; // New field for speed multiplier
    #endregion

    #region References
    private float lastFireTime;
    private WaitForSeconds launchDelayWait;
    #endregion

    #region Object Pooling
    [Header("Object Pooling")]
    [SerializeField]
    private int lockOnEffectPoolSize = 10;
    private Queue<GameObject> lockOnEffectPool;
    #endregion

    private void Awake()
    {
        playerLocking = GetComponent<PlayerLocking>();
        crosshairCore = GetComponent<CrosshairCore>();

        if (playerLocking == null || crosshairCore == null)
        {
            Debug.LogError("Required components not found on the same GameObject.");
        }

        InitializeLockOnEffectPool();
        launchDelayWait = new WaitForSeconds(launchDelay);
    }

    private void InitializeLockOnEffectPool()
    {
        lockOnEffectPool = new Queue<GameObject>();
        for (int i = 0; i < lockOnEffectPoolSize; i++)
        {
            GameObject lockOnEffect = Instantiate(
                crosshairCore.lockOnPrefab,
                crosshairCore.Reticle.transform
            );
            lockOnEffect.SetActive(false);
            lockOnEffectPool.Enqueue(lockOnEffect);
        }
    }

    public void HandleShootingEffects()
    {
        PlayRandomShooting();
        PlayRandomShootTag();
        StartCoroutine(ShootVibrate());
        shootFeedback.PlayFeedbacks();
    }

    private void PlayRandomShooting() => RuntimeManager.PlayOneShot(randomShootingEvent);

    private void PlayRandomShootTag() => RuntimeManager.PlayOneShot(shootTagEvent);

    private IEnumerator ShootVibrate()
    {
        // Implement vibration logic here
        yield return new WaitForSeconds(.1f);
    }

    public IEnumerator LaunchProjectilesWithDelay()
    {
        ProjectileManager.Instance.CompleteRunningJobs();
        
        crosshairCore.lastProjectileLaunchTime = Time.time;

        int lockedProjectileCount = playerLocking.GetLockedProjectileCount();
        
        if (lockedProjectileCount > 0)
        {
            if (playerLocking.enemyTargetList.Count > 0)
            {
                // Targeted enemy shooting
                float damagePerProjectile = 10f;
                float totalDamage = lockedProjectileCount * damagePerProjectile;
                List<Transform> enemiesHit = playerLocking.enemyTargetList.ToList();

                for (int i = 0; i < lockedProjectileCount; i++)
                {
                    AnimateLockOnEffect();
                    yield return launchDelayWait;
                }

                playerLocking.DamageLockedEnemies(totalDamage, enemiesHit);
            }
            else
            {
                // Non-targeted shooting
                for (int i = 0; i < lockedProjectileCount; i++)
                {
                    ShootNonTargetedProjectile();
                    AnimateLockOnEffect();
                    yield return launchDelayWait;
                }
            }
        }

        playerLocking.ClearLockedTargets();
    }

    public void AnimateLockOnEffect()
    {
        if (crosshairCore.lockOnPrefab != null && lockOnEffectPool.Count > 0)
        {
            GameObject lockOnInstance = lockOnEffectPool.Dequeue();
            lockOnInstance.SetActive(true);
            lockOnInstance.transform.localPosition = Vector3.zero;
            lockOnInstance.transform.localScale = Vector3.one * crosshairCore.initialScale;

            SpriteRenderer spriteRenderer = lockOnInstance.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Color initialColor = spriteRenderer.color;
                initialColor.a = crosshairCore.initialTransparency;
                spriteRenderer.color = initialColor;

                Color targetColor = spriteRenderer.color;
                targetColor.a = 1f;
                Tween.Color(spriteRenderer, targetColor, 0.5f);
            }

            Tween
                .Scale(lockOnInstance.transform, Vector3.zero, 1f)
                .OnComplete(() =>
                {
                    lockOnInstance.SetActive(false);
                    lockOnEffectPool.Enqueue(lockOnInstance);
                });
        }
    }

    private void ShootNonTargetedProjectile()
    {
        if (ProjectileSpawner.Instance != null)
        {
            float nonTargetedSpeed = projectileSpeed * nonTargetedSpeedMultiplier; // Calculate the increased speed
            ProjectileStateBased projectile = ProjectileSpawner.Instance.ShootPlayerProjectile(
                projectileDamage,
                nonTargetedSpeed, // Use the increased speed for non-targeted projectiles
                projectileScale
            );

            if (projectile != null)
            {
                // Handle any additional effects or feedback for shooting
                HandleShootingEffects();
            }
        }
        else
        {
            ConditionalDebug.LogError("[PlayerShooting] ProjectileSpawner.Instance is null!");
        }
    }
}
