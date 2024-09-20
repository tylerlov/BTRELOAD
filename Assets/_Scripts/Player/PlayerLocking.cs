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
    private int range = 400; // Increased from 300
    public float bulletLockInterval = 0.05f; // Decreased from 0.1f
    public float enemyLockInterval = 0.2f;
    public Vector3 bulletLockBoxSize = new Vector3(2, 2, 2); // Increased from (1, 1, 1)
    public MMF_Player lockFeedback;
    public List<Transform> projectileTargetList = new List<Transform>();
    public Transform enemyTarget;
    public List<Transform> enemyTargetList = new List<Transform>();
    public List<Transform> LockedList = new List<Transform>();
    public List<Transform> qteEnemyLockList = new List<Transform>();
    #endregion

    #region References
    [Header("References")]
    [SerializeField] private CrosshairCore crosshairCore;
    [SerializeField] private UILockOnEffect uiLockOnEffect;
    [SerializeField] private StaminaController staminaController;
    [SerializeField] private ShooterMovement shooterMovement;
    #endregion

    private float lastBulletLockTime,
                  lastEnemyTime;
    private int enemyTargetListIndex;

    [SerializeField]
    private GameObject lockIndicator;

    #region Aim Assist
    [Header("Aim Assist")]
    [SerializeField, Range(0f, 1f)]
    private float aimAssistStrength = 0.8f; // Increased from 0.7f

    [SerializeField, Range(0f, 45f)]
    private float maxAimAssistAngle = 20f; // Increased from 15f

    [SerializeField, Range(0f, 45f)]
    private float lockMaintainAngle = 25f; // Increased from 20f

    [SerializeField, Range(0f, 1f)]
    private float lockGracePeriod = 0.8f; // Increased from 0.7f
    private Dictionary<Transform, float> enemyLockTimes = new Dictionary<Transform, float>();

    [Header("Aim Assist Stickiness")]
    [SerializeField, Range(0f, 1f)]
    private float stickinessStrength = 0.7f; // Increased from 0.5f

    [SerializeField, Range(0f, 1f)]
    private float stickinessRadius = 0.5f; // Increased from 0.3f
    #endregion

    #region Locking Stability
    [Header("Locking Stability")]
    [SerializeField, Range(0f, 1f)]
    private float lockSwitchThreshold = 0.5f; // Threshold to switch targets

    private Transform currentLockedEnemy;
    private float lockTimer;
    private const float lockDuration = 1f; // Duration to wait before considering a target switch
    #endregion

    #region Lock Highlight
    [Header("Lock Highlight")]
    [SerializeField]
    private GameObject lockHighlightPrefab;

    private Dictionary<Transform, GameObject> lockHighlights = new Dictionary<Transform, GameObject>();
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

        // Ensure references are assigned
        if (crosshairCore == null)
        {
            crosshairCore = GetComponent<CrosshairCore>();
            if (crosshairCore == null)
            {
                Debug.LogError("CrosshairCore component not found on the same GameObject.");
            }
        }

        if (uiLockOnEffect == null)
        {
            uiLockOnEffect = FindObjectOfType<UILockOnEffect>();
            if (uiLockOnEffect == null)
            {
                Debug.LogError("UILockOnEffect not found in the scene.");
            }
        }

        if (staminaController == null)
        {
            staminaController = FindObjectOfType<StaminaController>();
            if (staminaController == null)
            {
                Debug.LogError("StaminaController not found in the scene.");
            }
        }

        if (shooterMovement == null)
        {
            shooterMovement = GetComponent<ShooterMovement>();
            if (shooterMovement == null)
            {
                Debug.LogError("ShooterMovement component not found on the same GameObject.");
            }
        }
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
            currentLockedEnemy = null;
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
            // If no current lock or after cooldown, consider switching
            if (currentLockedEnemy == null || ShouldSwitchTarget(nearestEnemyInSight.transform))
            {
                LockOntoEnemy(nearestEnemyInSight.transform);
                ShowLockIndicator();
                currentLockedEnemy = nearestEnemyInSight.transform;
                lockTimer = Time.time;
            }

            Vector3 directionToEnemy = (
                nearestEnemyInSight.transform.position - crosshairCore.RaySpawn.transform.position
            ).normalized;
            aimAssistDirection = Vector3
                .ProjectOnPlane(directionToEnemy, crosshairCore.RaySpawn.transform.forward)
                .normalized;

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
            currentLockedEnemy = null;
        }

        ApplyAimAssist(aimAssistDirection);
        ConditionalDebug.Log($"CheckEnemyLock finished. enemyTargetList count: {enemyTargetList.Count}");
    }

    private bool ShouldSwitchTarget(Transform newTarget)
    {
        if (currentLockedEnemy == null)
            return true;

        Vector3 currentDirection = currentLockedEnemy.position - crosshairCore.RaySpawn.transform.position;
        Vector3 newDirection = newTarget.position - crosshairCore.RaySpawn.transform.position;

        float currentAngle = Vector3.Angle(crosshairCore.RaySpawn.transform.forward, currentDirection);
        float newAngle = Vector3.Angle(crosshairCore.RaySpawn.transform.forward, newDirection);

        float angleDifference = newAngle - currentAngle;

        // Only switch if the new target is significantly more aligned
        return angleDifference < -lockSwitchThreshold * 1.5f;
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

            // Instantiate lock highlight
            if (lockHighlightPrefab != null && !lockHighlights.ContainsKey(enemyTransform))
            {
                GameObject highlight = Instantiate(lockHighlightPrefab, enemyTransform);
                lockHighlights[enemyTransform] = highlight;
            }
        }
        else
        {
            ConditionalDebug.Log($"Enemy {enemyTransform.name} is already locked.");
        }
    }

    public void UnlockEnemies()
    {
        ConditionalDebug.Log($"UnlockEnemies called. Current enemyTargetList count: {enemyTargetList.Count}");
        foreach (var enemy in enemyTargetList.ToList()) // Use ToList to avoid modification during iteration
        {
            if (enemy != null)
            {
                EnemyBasicSetup enemySetup = enemy.GetComponent<EnemyBasicSetup>();
                if (enemySetup != null)
                {
                    enemySetup.SetLockOnStatus(false);
                    ConditionalDebug.Log($"Unlocked enemy {enemy.name}");
                }

                // Destroy lock highlight
                if (lockHighlights.ContainsKey(enemy))
                {
                    Destroy(lockHighlights[enemy]);
                    lockHighlights.Remove(enemy);
                }
            }
        }
        enemyTargetList.Clear();
        currentLockedEnemy = null;
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

            // Destroy lock highlight
            if (lockHighlights.ContainsKey(enemy))
            {
                Destroy(lockHighlights[enemy]);
                lockHighlights.Remove(enemy);
            }

            if (enemyTargetList.Count == 0)
            {
                HideLockIndicator();
                currentLockedEnemy = null;
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

    private bool IsTimeToLockBullet() => Time.time >= lastBulletLockTime + bulletLockInterval;

    private RaycastHit[] PerformBulletLockBoxCast()
    {
        RaycastHit[] hits = new RaycastHit[20]; // Increased from 10 for more potential targets
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

    private void UpdateLastBulletLockTime() => lastBulletLockTime = Time.time;

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
        if (shooterMovement == null)
        {
            Debug.LogWarning("ShooterMovement is not assigned in PlayerLocking.");
            return;
        }

        if (currentLockedEnemy == null)
        {
            // If there's no locked enemy, apply no aim assist
            shooterMovement.ApplyAimAssist(Vector3.zero);
            return;
        }

        // Calculate the direction from the reticle to the enemy in world space
        Vector3 reticlePosition = transform.position;
        Vector3 enemyPosition = currentLockedEnemy.position;
        Vector3 directionToEnemy = (enemyPosition - reticlePosition).normalized;

        // Project the direction on the screen plane (assuming camera is Z-up)
        Vector3 screenDirection = Camera.main.transform.InverseTransformDirection(directionToEnemy);
        screenDirection.z = 0;
        screenDirection = screenDirection.normalized;

        // Calculate the angle between the camera's forward direction and the direction to the enemy
        float angle = Vector3.Angle(Camera.main.transform.forward, directionToEnemy);

        // If within the maximum aim assist angle, apply the aim assist
        if (angle <= maxAimAssistAngle)
        {
            // Apply stickiness based on proximity
            float distanceToTarget = Vector3.Distance(crosshairCore.RaySpawn.transform.position, currentLockedEnemy.position);
            float normalizedDistance = Mathf.Clamp01(distanceToTarget / range);
            float dynamicStrength = Mathf.Lerp(aimAssistStrength * 2.5f, aimAssistStrength, normalizedDistance);

            // Calculate stickiness factor
            float stickinessFactor = Mathf.Clamp01(1f - (angle / maxAimAssistAngle));
            stickinessFactor = Mathf.Pow(stickinessFactor, 2); // Apply a power curve for more pronounced effect

            // Combine stickiness strength and factor
            Vector3 finalAimAssist = screenDirection * dynamicStrength * stickinessStrength * stickinessFactor;

            shooterMovement.ApplyAimAssist(finalAimAssist);
        }
        else
        {
            shooterMovement.ApplyAimAssist(Vector3.zero);
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