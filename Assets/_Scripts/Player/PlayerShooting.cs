using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FMODUnity;
using MoreMountains.Feedbacks;
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

        // Populate the qteEnemyLockList with current locked targets
        playerLocking.qteEnemyLockList.Clear();
        foreach (var lockedState in projectilesToLaunch)
        {
            if (lockedState.GetProjectile()?.currentTarget != null)
            {
                playerLocking.qteEnemyLockList.Add(lockedState.GetProjectile().currentTarget);
            }
        }
        Debug.Log(
            $"QTE Enemy Lock List populated with {playerLocking.qteEnemyLockList.Count} targets"
        );

        for (int i = 0; i < projectilesToLaunch.Count; i++)
        {
            PlayerLockedState lockedState = projectilesToLaunch[i];
            ProjectileStateBased projectile = lockedState.GetProjectile();

            if (projectile != null)
            {
                if (projectile.currentTarget != null)
                    lockedState.LaunchAtEnemy(projectile.currentTarget);
                else
                    lockedState.LaunchBack();

                AnimateLockOnEffect();
                yield return new WaitForSeconds(launchDelay);
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

                spriteRenderer.DOFade(1f, 0.5f);
            }

            lockOnInstance
                .transform.DOScale(Vector3.zero, 1f)
                .OnComplete(() =>
                {
                    lockOnInstance.SetActive(false);
                    lockOnEffectPool.Enqueue(lockOnInstance);
                });
        }
    }
}
