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
    public Transform enemyTarget;
    public List<Transform> enemyTargetList = new List<Transform>();
    public List<Transform> LockedList = new List<Transform>();
    public List<Transform> qteEnemyLockList = new List<Transform>();
    #endregion

    #region References
    [Header("References")]
    private CrosshairCore crosshairCore;
    private UILockOnEffect uiLockOnEffect;
    private StaminaController staminaController;
    private ShooterMovement shooterMovement;
    private AimAssistController aimAssistController;
    #endregion

    private float lastBulletLockTime,
                  lastEnemyTime;
    private int enemyTargetListIndex;

    [SerializeField]
    private GameObject normalReticle;  // Add this line before lockIndicator
    [SerializeField]
    private GameObject lockIndicator;

    #region Locking Stability
    [Header("Locking Stability")]
    [SerializeField, Range(0f, 1f)]
    private float lockSwitchThreshold = 0.5f; // Threshold to switch targets

    private Transform currentLockedEnemy;
    private float lockTimer;
    private const float lockDuration = 1f; // Duration to wait before considering a target switch

    // Add this line to declare the enemyLockTimes dictionary
    private Dictionary<Transform, float> enemyLockTimes = new Dictionary<Transform, float>();
    #endregion

    #region Lock Highlight
    [Header("Lock Highlight")]
    [SerializeField]
    private GameObject lockHighlightPrefab;

    private Dictionary<Transform, GameObject> lockHighlights = new Dictionary<Transform, GameObject>();
    #endregion

    private int lockedProjectileCount = 0;

    [Header("Debug Visualization")]
    public bool showDebugVisuals = true;
    public Color rayColor = Color.red;
    public Color boxCastColor = Color.blue;

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
        crosshairCore = GetComponent<CrosshairCore>();
        uiLockOnEffect = FindObjectOfType<UILockOnEffect>();
        staminaController = FindObjectOfType<StaminaController>();
        shooterMovement = GetComponent<ShooterMovement>();
        aimAssistController = GetComponent<AimAssistController>();

        if (crosshairCore == null)
        {
            Debug.LogError("CrosshairCore component not found on the same GameObject.");
        }

        if (uiLockOnEffect == null)
        {
            Debug.LogError("UILockOnEffect not found in the scene.");
        }

        if (staminaController == null)
        {
            Debug.LogError("StaminaController not found in the scene.");
        }

        if (shooterMovement == null)
        {
            Debug.LogError("ShooterMovement component not found on the same GameObject.");
        }

        if (aimAssistController == null)
        {
            Debug.LogError("AimAssistController not found on the same GameObject.");
        }
    }

    private void Update()
    {
        OnLock();
        CheckEnemyLock();
    }

    public void CheckEnemyLock()
    {
        if (lockedProjectileCount == 0)
        {
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

        foreach (Collider enemy in nearbyEnemies)
        {
            // Check if the enemy has the NonLockableEnemy component
            if (enemy.GetComponent<NonLockableEnemy>() != null)
            {
                continue; // Skip this enemy if it has the NonLockableEnemy component
            }

            Vector3 directionToEnemy =
                enemy.transform.position - crosshairCore.RaySpawn.transform.position;
            float angle = Vector3.Angle(crosshairCore.RaySpawn.transform.forward, directionToEnemy);

            bool isCurrentlyLocked = enemyTargetList.Contains(enemy.transform);
            float angleThreshold = isCurrentlyLocked ? aimAssistController.GetLockMaintainAngle() : aimAssistController.GetMaxAimAssistAngle();

            if (angle <= angleThreshold)
            {
                float distance = directionToEnemy.magnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemyInSight = enemy;
                }
            }
            else if (isCurrentlyLocked)
            {
                if (!enemyLockTimes.ContainsKey(enemy.transform))
                {
                    enemyLockTimes[enemy.transform] = Time.time;
                }
                else if (Time.time - enemyLockTimes[enemy.transform] > aimAssistController.GetLockGracePeriod())
                {
                    UnlockEnemy(enemy.transform);
                }
            }
        }

        if (nearestEnemyInSight != null)
        {
            if (currentLockedEnemy == null || ShouldSwitchTarget(nearestEnemyInSight.transform))
            {
                LockOntoEnemy(nearestEnemyInSight.transform);
                ShowLockIndicator();
                currentLockedEnemy = nearestEnemyInSight.transform;
                lockTimer = Time.time;
            }

            enemyLockTimes.Remove(nearestEnemyInSight.transform);
        }
        else if (
            enemyTargetList.Count > 0
            && !enemyLockTimes.Any(kvp => Time.time - kvp.Value <= aimAssistController.GetLockGracePeriod())
        )
        {
            UnlockEnemies();
            HideLockIndicator();
            currentLockedEnemy = null;
        }

        aimAssistController.SetCurrentLockedEnemy(currentLockedEnemy);
        aimAssistController.ApplyAimAssist(crosshairCore.RaySpawn.transform, Camera.main.transform);
    }

    private void LockOntoEnemy(Transform enemyTransform)
    {
        if (!enemyTargetList.Contains(enemyTransform))
        {
            enemyTarget = enemyTransform;
            enemyTargetList.Add(enemyTarget);
            FMODUnity.RuntimeManager.PlayOneShot("event:/Player/LockEnemy");
            uiLockOnEffect.LockOnTarget(enemyTarget);

            // Instantiate lock highlight
            if (lockHighlightPrefab != null && !lockHighlights.ContainsKey(enemyTransform))
            {
                GameObject highlight = Instantiate(lockHighlightPrefab, enemyTransform);
                lockHighlights[enemyTransform] = highlight;
            }

            // Play lock-on feedback
            lockFeedback.PlayFeedbacks();
        }
    }

    public void UnlockEnemies()
    {
        ProjectileManager.Instance.CompleteRunningJobs();
        
        foreach (var enemy in enemyTargetList.ToList())
        {
            if (enemy != null)
            {
                EnemyBasicSetup enemySetup = enemy.GetComponent<EnemyBasicSetup>();
                if (enemySetup != null)
                {
                    enemySetup.SetLockOnStatus(false);
                }

                // Destroy lock highlight
                if (lockHighlights.ContainsKey(enemy))
                {
                    Destroy(lockHighlights[enemy]);
                    lockHighlights.Remove(enemy);
                }

                uiLockOnEffect.UnlockTarget(enemy);
            }
        }
        enemyTargetList.Clear();
        currentLockedEnemy = null;
    }

    public void UnlockEnemy(Transform enemy)
    {
        ProjectileManager.Instance.CompleteRunningJobs();
        
        if (enemy != null && enemyTargetList.Contains(enemy))
        {
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

            uiLockOnEffect.UnlockTarget(enemy);

            if (enemyTargetList.Count == 0)
            {
                HideLockIndicator();
                currentLockedEnemy = null;
            }
        }
    }

    private void ShowLockIndicator()
    {
        if (lockIndicator != null && !lockIndicator.activeSelf)
        {
            lockIndicator.SetActive(true);
            if (normalReticle != null)
            {
                normalReticle.SetActive(false);
            }
        }
    }

    private void HideLockIndicator()
    {
        if (lockIndicator != null && lockIndicator.activeSelf)
        {
            lockIndicator.SetActive(false);
            if (normalReticle != null)
            {
                normalReticle.SetActive(true);
            }
        }
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

        return angleDifference < -lockSwitchThreshold;
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
            {
                // After successfully locking onto a bullet, check for enemy lock
                CheckEnemyLock();
                break;
            }
        }
    }

    public void ClearLockedTargets()
    {
        lockedProjectileCount = 0;
        UnlockEnemies();
        HideLockIndicator();
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
            !crosshairCore.collectHealthMode
            && staminaController.canRewind
            && crosshairCore.CheckLockProjectiles()
        )
        {
            if (hitPSB && hitPSB.GetCurrentStateType() == typeof(EnemyShotState))
            {
                if (lockedProjectileCount < maxTargets)
                {
                    hitPSB.ChangeState(new PlayerLockedState(hitPSB));
                    lockedProjectileCount++;
                    crosshairCore.musicPlayback.EventInstance.setParameterByName("Lock State", 1);
                    UpdateLastBulletLockTime();
                    HandleMaxTargetsReached();

                    // Restore these lines for feedback
                    StartCoroutine(LockVibrate());
                    lockFeedback.PlayFeedbacks();
                    PlayRandomLocking();
                    Locks++;

                    // Restore visual feedback
                    hit.transform.GetChild(0).gameObject.SetActive(true);

                    return true;
                }
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

        int projectilesToCreate = lockedProjectileCount;
        lockedProjectileCount = 0; // Reset the count as we're creating new projectiles

        for (int i = 0; i < projectilesToCreate; i++)
        {
            if (enemyTargetList.Count > 0)
            {
                enemyTargetListIndex = Mathf.Clamp(enemyTargetListIndex, 1, enemyTargetList.Count);
                Transform currEnemyTarg = enemyTargetList[enemyTargetListIndex - 1];

                if (currEnemyTarg != null && currEnemyTarg.gameObject.activeSelf)
                {
                    ProjectileStateBased newProjectile = ProjectilePool.Instance.GetProjectile();
                    if (newProjectile != null)
                    {
                        SetupNewProjectile(newProjectile, currEnemyTarg);
                        projectilesToLaunch.Add(newProjectile.GetCurrentState() as PlayerLockedState);
                    }
                }
                else
                {
                    enemyTargetList.Remove(currEnemyTarg);
                }

                enemyTargetListIndex--;
            }
        }

        staminaController.locking = false;
        return projectilesToLaunch;
    }

    private void SetupNewProjectile(ProjectileStateBased projectile, Transform target)
    {
        if (ProjectileStateBased.shootingObject != null)
        {
            projectile.transform.position = ProjectileStateBased.shootingObject.transform.position;
            Vector3 directionToTarget = (target.position - projectile.transform.position).normalized;
            projectile.transform.rotation = Quaternion.LookRotation(directionToTarget);
            
            // Assuming these values are available or can be set appropriately
            float damage = 10f; // Set an appropriate damage value
            float speed = 50f;  // Set an appropriate speed
            float lifetime = 5f; // Set an appropriate lifetime
            
            projectile.SetupProjectile(damage, speed, lifetime, true, 1f, target, false);
            projectile.ChangeState(new PlayerShotState(projectile, 1f, target, true));
            ProjectileManager.Instance.RegisterProjectile(projectile);
        }
        else
        {
            Debug.LogError("Shooting object is not assigned.");
        }
    }

    public void PlayRandomLocking()
    {
        FMODUnity.RuntimeManager.PlayOneShot("event:/Player/Locking");
    }

    public IEnumerator LockVibrate()
    {
        // Implement vibration logic here
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

    public void DamageLockedEnemies(float totalDamage, List<Transform> enemiesHit)
    {
        if (enemiesHit.Count == 0)
        {
            return;
        }

        float damagePerEnemy = totalDamage / enemiesHit.Count;

        foreach (Transform enemyTransform in enemiesHit)
        {
            if (enemyTransform != null && enemyTransform.gameObject.activeInHierarchy)
            {
                EnemyBasicSetup enemy = enemyTransform.GetComponent<EnemyBasicSetup>();
                if (enemy != null)
                {
                    enemy.Damage(damagePerEnemy);
                }
            }
        }
    }

    public int GetLockedProjectileCount()
    {
        return lockedProjectileCount;
    }

    public bool TryLockOntoProjectile()
    {
        Debug.Log("TryLockOntoProjectile called");
        RaycastHit[] hits = PerformBulletLockBoxCast();
        foreach (var hit in hits)
        {
            if (IsValidBulletHit(hit) && TryLockOntoBullet(hit))
            {
                Debug.Log("Successfully locked onto projectile");
                return true;
            }
        }
        Debug.Log("Failed to lock onto projectile");
        return false;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugVisuals || crosshairCore == null || crosshairCore.RaySpawn == null)
            return;

        // Draw the raycast
        Gizmos.color = rayColor;
        Vector3 rayDirection = crosshairCore.RaySpawn.transform.forward * range;
        Gizmos.DrawRay(crosshairCore.RaySpawn.transform.position, rayDirection);

        // Draw the box cast
        Gizmos.color = boxCastColor;
        Vector3 boxCenter = crosshairCore.RaySpawn.transform.position + (crosshairCore.RaySpawn.transform.forward * range / 2f);
        Gizmos.matrix = Matrix4x4.TRS(boxCenter, crosshairCore.RaySpawn.transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(bulletLockBoxSize.x, bulletLockBoxSize.y, range));
    }
}
