using System.Collections;
using System.Collections.Generic;
using System.Linq; // Add this for LINQ methods
using FMODUnity;
using MoreMountains.Feedbacks;
using PrimeTween;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public static PlayerShooting Instance { get; private set; }

    #region Shooting Variables
    [Header("Shooting Settings")]
    public float launchDelay = 0.1f;
    public MMF_Player shootFeedback;

    [SerializeField]
    private EventReference randomShootingEvent;

    [SerializeField]
    private EventReference shootTagEvent;
    #endregion

    #region References
    private PlayerLocking playerLocking;
    private CrosshairCore crosshairCore;
    private float lastFireTime;
    private WaitForSeconds launchDelayWait; // Add this line
    #endregion

    #region Object Pooling
    [Header("Object Pooling")]
    [SerializeField]
    private int lockOnEffectPoolSize = 10;
    private Queue<GameObject> lockOnEffectPool;
    #endregion

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
            return;
        }

        playerLocking = GetComponent<PlayerLocking>();
        crosshairCore = GetComponent<CrosshairCore>();
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

    public IEnumerator LaunchProjectilesWithDelay(List<PlayerLockedState> projectilesToLaunch)
    {
        crosshairCore.lastProjectileLaunchTime = Time.time;
        Debug.Log($"Projectiles launched at {crosshairCore.lastProjectileLaunchTime}");

        float totalDamage = projectilesToLaunch.Sum(state =>
            state.GetProjectile()?.damageAmount ?? 0f
        );
        List<Transform> enemiesHit = projectilesToLaunch
            .Select(state => state.GetTarget())
            .ToList();

        foreach (var lockedState in projectilesToLaunch)
        {
            ProjectileStateBased projectile = lockedState.GetProjectile();

            if (projectile != null)
            {
                projectile.ReturnToPool();
                AnimateLockOnEffect();
                yield return launchDelayWait;
            }
        }

        Debug.Log($"Total damage to be applied: {totalDamage}");

        // Apply damage to locked enemies
        playerLocking.DamageLockedEnemies(totalDamage, enemiesHit);

        // Clear locked targets after applying damage
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
}
