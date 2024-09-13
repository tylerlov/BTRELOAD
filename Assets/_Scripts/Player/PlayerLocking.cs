using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerLocking : MonoBehaviour
{
    public static PlayerLocking Instance { get; private set; }

    #region Locking Variables
    [Header("Locking Settings")]
    public int Locks;
    public bool locking;
    public bool shootTag;
    public bool triggeredLockFire;
    public int maxLockedEnemyTargets = 3;
    public int maxTargets = 6;

    [SerializeField]
    private int range = 300;
    public float bulletLockInterval = 0.1f;
    public float enemyLockInterval = 0.2f;
    public Vector3 bulletLockBoxSize = new Vector3(1, 1, 1);
    public MMF_Player lockFeedback;
    public List<Transform> projectileTargetList = new List<Transform>();
    public Transform enemyTarget;
    public List<Transform> enemyTargetList = new List<Transform>();
    public List<Transform> LockedList = new List<Transform>();
    public List<Transform> qteEnemyLockList = new List<Transform>();
    #endregion

    #region References
    private CrosshairCore crosshairCore;
    private UILockOnEffect uiLockOnEffect;
    private StaminaController staminaController;
    #endregion

    private float lastBulletTime,
        lastEnemyTime;
    private int enemyTargetListIndex;

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

        crosshairCore = GetComponent<CrosshairCore>();
        uiLockOnEffect = FindObjectOfType<UILockOnEffect>();
        staminaController = FindObjectOfType<StaminaController>();
    }

    public void OnLock()
    {
        if (!IsTimeToLockBullet())
            return;

        foreach (var hit in PerformBulletLockBoxCast())
        {
            if (!IsValidBulletHit(hit))
                continue;

            UpdateLastBulletLockTime();
            if (crosshairCore.isQuickTap)
            {
                crosshairCore.isQuickTap = false;
                break;
            }
            else if (TryLockOntoBullet(hit))
                break;
        }
    }

    public void OnLockEnemy()
    {
        if (!IsTimeToLockEnemy())
            return;

        foreach (var hit in PerformEnemyLockBoxCast())
        {
            if (!IsValidEnemyHit(hit))
                continue;

            UpdateLastEnemyLockTime();
            crosshairCore.hitEnemy = hit;

            if (!ShouldLockOntoEnemy())
                continue;

            Transform enemyTransform = GetEnemyTransform(hit);
            if (enemyTransform != null && !enemyTargetList.Contains(enemyTransform))
            {
                LockOntoEnemy(enemyTransform);
                return;
            }
        }
    }

    public void ClearLockedTargets()
    {
        enemyTargetList.RemoveAll(enemy => enemy == null);
        foreach (var enemy in enemyTargetList)
        {
            EnemyBasicSetup enemySetup = enemy.GetComponent<EnemyBasicSetup>();
            EnemyBasicDamagablePart damagablePart = enemy.GetComponent<EnemyBasicDamagablePart>();

            if (enemySetup != null)
                enemySetup.SetLockOnStatus(false);
            else if (damagablePart != null)
                damagablePart.SetLockOnStatus(false);
        }

        enemyTargetList.Clear();
        projectileTargetList.RemoveAll(projectile => projectile == null);
        LockedList.RemoveAll(locked => locked == null);
        enemyTarget = null;

        qteEnemyLockList.Clear();
        Debug.Log("All locked targets cleared, including QTE Enemy Lock List");
    }

    public void ReleasePlayerLocks()
    {
        ClearLockedTargets();
        // Add any additional logic needed for releasing player locks
    }

    public void OnNewWaveOrAreaTransition() => ClearLockedTargets();

    public int returnLocks() => Locks;

    public int returnEnemyLocks() => enemyTargetList.Count;

    public void RemoveLockedEnemy(Transform enemy)
    {
        LockedList.Remove(enemy);
        projectileTargetList.Remove(enemy);
        enemyTargetList.Remove(enemy);
    }

    private bool IsTimeToLockBullet() => Time.time >= lastBulletTime + bulletLockInterval;

    private RaycastHit[] PerformBulletLockBoxCast()
    {
        RaycastHit[] hits = new RaycastHit[10];
        int hitsCount = Physics.BoxCastNonAlloc(
            crosshairCore.RaySpawn.transform.position,
            bulletLockBoxSize / 2,
            crosshairCore.RaySpawn.transform.forward,
            hits,
            crosshairCore.RaySpawn.transform.rotation,
            range
        );
        System.Array.Resize(ref hits, hitsCount);
        return hits;
    }

    private bool IsValidBulletHit(RaycastHit hit) =>
        hit.collider != null
        && (hit.collider.CompareTag("Bullet") || hit.collider.CompareTag("LaunchableBullet"));

    private void UpdateLastBulletLockTime() => lastBulletTime = Time.time;

    private bool TryLockOntoBullet(RaycastHit hit)
    {
        ProjectileStateBased hitPSB = hit.transform.GetComponent<ProjectileStateBased>();
        if (
            crosshairCore.collectHealthMode
            && hitPSB
            && hitPSB.GetCurrentStateType() == typeof(EnemyShotState)
        )
        {
            HandleCollectHealthMode(hit);
            return true;
        }
        else if (
            !crosshairCore.collectHealthMode
            && staminaController.canRewind
            && crosshairCore.CheckLockProjectiles()
        )
        {
            return TryAddBulletToLockList(hit, hitPSB);
        }
        return false;
    }

    private void HandleCollectHealthMode(RaycastHit hit)
    {
        ScoreManager.Instance.AddScore(100);
        StartCoroutine(LockVibrate());
        lockFeedback.PlayFeedbacks();
        PlayRandomLocking();
        hit.transform.GetComponent<ProjectileStateBased>().Death();
        UpdateLastBulletLockTime();
    }

    private bool TryAddBulletToLockList(RaycastHit hit, ProjectileStateBased hitPSB)
    {
        if (
            !projectileTargetList.Contains(hit.transform)
            && hitPSB
            && hitPSB.GetCurrentStateType() == typeof(EnemyShotState)
        )
        {
            if (LockedList.Count < maxTargets && projectileTargetList.Count < maxTargets)
            {
                hitPSB.ChangeState(new PlayerLockedState(hitPSB));
                projectileTargetList.Add(hit.transform);
                crosshairCore.musicPlayback.EventInstance.setParameterByName("Lock State", 1);
                UpdateLastBulletLockTime();
                HandleMaxTargetsReached();
                return true;
            }
        }
        return false;
    }

    private void HandleMaxTargetsReached()
    {
        if (LockedList.Count == maxTargets - 1)
        {
            crosshairCore.BonusDamage.SetActive(true);
            foreach (Transform TargetProjectile in LockedList)
                TargetProjectile.GetComponent<ProjectileStateBased>().damageAmount *= 1.5f;
        }
    }

    private bool IsTimeToLockEnemy() => Time.time >= lastEnemyTime + enemyLockInterval;

    private RaycastHit[] PerformEnemyLockBoxCast()
    {
        int combinedLayerMask =
            (1 << LayerMask.NameToLayer("Enemy")) | (1 << LayerMask.NameToLayer("Ground"));
        RaycastHit[] hits = new RaycastHit[10];
        Physics.BoxCastNonAlloc(
            crosshairCore.RaySpawn.transform.position,
            crosshairCore.RaySpawnEnemyLocking.transform.lossyScale / 8,
            crosshairCore.RaySpawnEnemyLocking.transform.forward,
            hits,
            crosshairCore.RaySpawnEnemyLocking.transform.rotation,
            range,
            combinedLayerMask
        );
        return hits;
    }

    private bool IsValidEnemyHit(RaycastHit hit) =>
        hit.collider != null
        && hit.collider.CompareTag("Enemy")
        && crosshairCore.CheckLockProjectiles();

    private bool ShouldLockOntoEnemy() => crosshairCore.CheckLockEnemies();

    private Transform GetEnemyTransform(RaycastHit hit)
    {
        EnemyBasicSetup enemySetup = hit.collider.GetComponentInParent<EnemyBasicSetup>();
        EnemyBasicDamagablePart damagablePart =
            hit.collider.GetComponent<EnemyBasicDamagablePart>();

        if (enemySetup != null)
        {
            enemySetup.SetLockOnStatus(true);
            return hit.collider.transform;
        }
        else if (damagablePart != null)
        {
            damagablePart.SetLockOnStatus(true);
            return hit.collider.transform;
        }

        return null;
    }

    private void LockOntoEnemy(Transform enemyTransform)
    {
        if (enemyTargetList.Count > maxLockedEnemyTargets)
            UnlockOldestEnemy();

        enemyTarget = enemyTransform;
        enemyTargetList.Add(enemyTarget);
        enemyTargetListIndex = enemyTargetList.Count;
        FMODUnity.RuntimeManager.PlayOneShot("event:/Player/LockEnemy");
        uiLockOnEffect.LockOnTarget(enemyTarget);
        lastEnemyTime = Time.time;
    }

    private void UnlockOldestEnemy()
    {
        Transform oldestEnemy = enemyTargetList[0];
        EnemyBasicSetup enemySetup = oldestEnemy.GetComponent<EnemyBasicSetup>();
        EnemyBasicDamagablePart damagablePart = oldestEnemy.GetComponent<EnemyBasicDamagablePart>();

        if (enemySetup != null)
            enemySetup.SetLockOnStatus(false);
        else if (damagablePart != null)
            damagablePart.SetLockOnStatus(false);

        enemyTargetList.RemoveAt(0);
    }

    private void UpdateLastEnemyLockTime() => lastEnemyTime = Time.time;

    public List<PlayerLockedState> PrepareProjectilesToLaunch()
    {
        List<PlayerLockedState> projectilesToLaunch = new List<PlayerLockedState>();
        CleanLockedList();

        for (int i = LockedList.Count - 1; i >= 0; i--)
        {
            if (LockedList[i] == null)
                continue;

            ProjectileStateBased projectile = LockedList[i].GetComponent<ProjectileStateBased>();
            if (projectile == null)
                continue;

            if (!(projectile.GetCurrentState() is PlayerLockedState))
                projectile.ChangeState(new PlayerLockedState(projectile));

            PlayerLockedState lockedState = projectile.GetCurrentState() as PlayerLockedState;
            if (lockedState != null)
            {
                if (enemyTargetList.Count > 0)
                {
                    enemyTargetListIndex = Mathf.Clamp(
                        enemyTargetListIndex,
                        1,
                        enemyTargetList.Count
                    );
                    Transform currEnemyTarg = enemyTargetList[enemyTargetListIndex - 1];

                    if (currEnemyTarg != null && currEnemyTarg.gameObject.activeSelf)
                        lockedState.LaunchAtEnemy(currEnemyTarg);
                    else
                        enemyTargetList.Remove(currEnemyTarg);

                    projectilesToLaunch.Add(lockedState);
                    enemyTargetListIndex--;
                }
                else
                    projectilesToLaunch.Add(lockedState);

                staminaController.locking = false;
            }

            LockedList.RemoveAt(i);
        }

        return projectilesToLaunch;
    }

    public void PlayRandomLocking() =>
        FMODUnity.RuntimeManager.PlayOneShot("event:/Player/Locking");

    public IEnumerator LockVibrate()
    {
        yield return new WaitForSeconds(.2f);
    }

    public void HandleLockFire()
    {
        if (triggeredLockFire && !crosshairCore.CheckLockProjectiles())
            triggeredLockFire = false;
    }

    private void CleanLockedList()
    {
        LockedList.RemoveAll(locked => locked == null);
    }

    public Vector3 RaycastTarget()
    {
        if (
            Physics.Raycast(
                crosshairCore.RaySpawn.transform.position,
                crosshairCore.RaySpawn.transform.forward,
                out RaycastHit hit,
                range
            )
        )
            return hit.point;
        else
            return new Ray(
                crosshairCore.RaySpawn.transform.position,
                crosshairCore.RaySpawn.transform.forward
            ).GetPoint(range);
    }

    public void ApplyIncreasedDamage()
    {
        foreach (var target in qteEnemyLockList)
        {
            ProjectileStateBased projectile = target.GetComponent<ProjectileStateBased>();
            if (projectile != null)
            {
                projectile.SetDamageMultiplier(2f); // Double the damage
            }
        }
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearLockedTargets();
    }
}
