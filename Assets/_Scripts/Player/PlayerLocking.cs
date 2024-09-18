using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [SerializeField]
    private GameObject lockIndicator;

    #region Aim Assist
    [Header("Aim Assist")]
    [SerializeField, Range(0f, 1f)]
    private float aimAssistStrength = 0.5f;

    [SerializeField, Range(0f, 45f)]
    private float maxAimAssistAngle = 10f;

    [SerializeField, Range(0f, 1f)]
    private float lockMaintainAngle = 15f;

    [SerializeField, Range(0f, 1f)]
    private float lockGracePeriod = 0.5f;
    private Dictionary<Transform, float> enemyLockTimes = new Dictionary<Transform, float>();
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

        crosshairCore = GetComponent<CrosshairCore>();
        uiLockOnEffect = FindObjectOfType<UILockOnEffect>();
        staminaController = FindObjectOfType<StaminaController>();
    }

    private void Update()
    {
        OnLock();
        CheckEnemyLock();
    }

    public void CheckEnemyLock()
    {
        ConditionalDebug.Log(
            $"CheckEnemyLock called. LockedList count: {LockedList.Count}, enemyTargetList count: {enemyTargetList.Count}"
        );

        if (LockedList.Count == 0)
        {
            ConditionalDebug.Log("No locked projectiles, unlocking enemies");
            UnlockEnemies();
            HideLockIndicator();
            return;
        }

        Vector3 aimAssistDirection = Vector3.zero;
        Collider nearestEnemyInSight = null;
        float nearestDistance = float.MaxValue;

        Collider[] nearbyEnemies = Physics.OverlapSphere(
            crosshairCore.RaySpawn.transform.position,
            range,
            LayerMask.GetMask("Enemy")
        );
        ConditionalDebug.Log($"Found {nearbyEnemies.Length} nearby enemies");

        foreach (Collider enemy in nearbyEnemies)
        {
            Vector3 directionToEnemy =
                enemy.transform.position - crosshairCore.RaySpawn.transform.position;
            float angle = Vector3.Angle(crosshairCore.RaySpawn.transform.forward, directionToEnemy);

            bool isCurrentlyLocked = enemyTargetList.Contains(enemy.transform);
            float angleThreshold = isCurrentlyLocked ? lockMaintainAngle : maxAimAssistAngle;

            ConditionalDebug.Log(
                $"Enemy: {enemy.name}, Angle: {angle}, Threshold: {angleThreshold}, Currently locked: {isCurrentlyLocked}"
            );

            if (angle <= angleThreshold)
            {
                float distance = directionToEnemy.magnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemyInSight = enemy;
                    ConditionalDebug.Log($"New nearest enemy: {enemy.name}, Distance: {distance}");
                }
            }
            else if (isCurrentlyLocked)
            {
                if (!enemyLockTimes.ContainsKey(enemy.transform))
                {
                    enemyLockTimes[enemy.transform] = Time.time;
                    ConditionalDebug.Log($"Started grace period for {enemy.name}");
                }
                else if (Time.time - enemyLockTimes[enemy.transform] > lockGracePeriod)
                {
                    ConditionalDebug.Log($"Grace period expired for {enemy.name}, unlocking");
                    UnlockEnemy(enemy.transform);
                }
            }
        }

        if (nearestEnemyInSight != null)
        {
            Vector3 directionToEnemy = (
                nearestEnemyInSight.transform.position - crosshairCore.RaySpawn.transform.position
            ).normalized;
            aimAssistDirection = Vector3
                .ProjectOnPlane(directionToEnemy, crosshairCore.RaySpawn.transform.forward)
                .normalized;

            if (!enemyTargetList.Contains(nearestEnemyInSight.transform))
            {
                ConditionalDebug.Log($"Locking onto new enemy: {nearestEnemyInSight.name}");
                LockOntoEnemy(nearestEnemyInSight.transform);
                ShowLockIndicator();
            }
            enemyLockTimes.Remove(nearestEnemyInSight.transform);
        }
        else if (
            enemyTargetList.Count > 0
            && !enemyLockTimes.Any(kvp => Time.time - kvp.Value <= lockGracePeriod)
        )
        {
            ConditionalDebug.Log("No enemies in sight and grace period expired, unlocking all");
            UnlockEnemies();
            HideLockIndicator();
        }

        ApplyAimAssist(aimAssistDirection);
        ConditionalDebug.Log($"CheckEnemyLock finished. enemyTargetList count: {enemyTargetList.Count}");
    }

    private void LockOntoEnemy(Transform enemyTransform)
    {
        if (!enemyTargetList.Contains(enemyTransform))
        {
            enemyTarget = enemyTransform;
            enemyTargetList.Add(enemyTarget);
            ConditionalDebug.Log(
                $"Locked onto enemy: {enemyTarget.name}. Total locked enemies: {enemyTargetList.Count}"
            );
            FMODUnity.RuntimeManager.PlayOneShot("event:/Player/LockEnemy");
            uiLockOnEffect.LockOnTarget(enemyTarget);
        }
        else
        {
            ConditionalDebug.Log($"Enemy {enemyTransform.name} is already locked.");
        }
    }

    public void UnlockEnemies()
    {
        ConditionalDebug.Log($"UnlockEnemies called. Current enemyTargetList count: {enemyTargetList.Count}");
        foreach (var enemy in enemyTargetList)
        {
            if (enemy != null)
            {
                EnemyBasicSetup enemySetup = enemy.GetComponent<EnemyBasicSetup>();
                if (enemySetup != null)
                {
                    enemySetup.SetLockOnStatus(false);
                    ConditionalDebug.Log($"Unlocked enemy {enemy.name}");
                }
            }
        }
        enemyTargetList.Clear();
        ConditionalDebug.Log("enemyTargetList cleared");
    }

    private void UnlockEnemy(Transform enemy)
    {
        if (enemy != null && enemyTargetList.Contains(enemy))
        {
            ConditionalDebug.Log($"Unlocking enemy: {enemy.name}");
            EnemyBasicSetup enemySetup = enemy.GetComponent<EnemyBasicSetup>();
            EnemyBasicDamagablePart damagablePart = enemy.GetComponent<EnemyBasicDamagablePart>();

            if (enemySetup != null)
                enemySetup.SetLockOnStatus(false);
            else if (damagablePart != null)
                damagablePart.SetLockOnStatus(false);

            enemyTargetList.Remove(enemy);
            enemyLockTimes.Remove(enemy);

            if (enemyTargetList.Count == 0)
            {
                HideLockIndicator();
            }
        }
        else
        {
                ConditionalDebug.Log($"Attempted to unlock invalid enemy: {enemy}");
        }
    }

    public void OnLock()
    {
        ConditionalDebug.Log("OnLock method called");
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
            {
                // After successfully locking onto a bullet, check for enemy lock
                CheckEnemyLock();
                break;
            }
        }
    }

    public void ClearLockedTargets()
    {
        ConditionalDebug.Log("Clearing locked targets");
        LockedList.Clear();
        enemyTargetList.Clear();
        projectileTargetList.Clear();
        ConditionalDebug.Log("All locked targets cleared, including QTE Enemy Lock List");
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

    public List<PlayerLockedState> PrepareProjectilesToLaunch()
    {
        List<PlayerLockedState> projectilesToLaunch = new List<PlayerLockedState>();
        CleanLockedList();

        if (enemyTargetList.Count == 0)
        {
            Debug.LogWarning("No locked enemies found. Cannot launch projectiles.");
            return projectilesToLaunch;
        }

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

    private void ShowLockIndicator()
    {
        if (lockIndicator != null && !lockIndicator.activeSelf)
        {
            lockIndicator.SetActive(true);
        }
    }

    private void HideLockIndicator()
    {
        if (lockIndicator != null && lockIndicator.activeSelf)
        {
            lockIndicator.SetActive(false);
        }
    }

    private void ApplyAimAssist(Vector3 aimAssistDirection)
    {
        ShooterMovement shooterMovement = GetComponent<ShooterMovement>();
        if (shooterMovement != null)
        {
            shooterMovement.ApplyAimAssist(aimAssistDirection * aimAssistStrength);
        }
    }

    public void DamageLockedEnemies(float totalDamage, List<Transform> enemiesHit)
    {
        ConditionalDebug.Log($"DamageLockedEnemies called. Total damage: {totalDamage}, Enemies hit: {enemiesHit.Count}");

        if (enemiesHit.Count == 0)
        {
            ConditionalDebug.LogWarning("No enemies hit to damage.");
            return;
        }

        float damagePerEnemy = totalDamage / enemiesHit.Count;

        foreach (Transform enemyTransform in enemiesHit)
        {
            if (enemyTransform != null)
            {
                EnemyBasicSetup enemy = enemyTransform.GetComponent<EnemyBasicSetup>();
                if (enemy != null)
                {
                    enemy.Damage(damagePerEnemy);
                    ConditionalDebug.Log($"Applied {damagePerEnemy} damage to enemy {enemyTransform.name}");
                }
            }
        }
    }
}
