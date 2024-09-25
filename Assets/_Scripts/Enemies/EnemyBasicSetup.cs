using System;
using System.Collections;
using BehaviorDesigner.Runtime.Tactical;
using Chronos;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using OccaSoftware.BOP;
using Pathfinding;
using Pathfinding.Examples;
using Pathfinding.RVO;
using PathologicalGames;
using SonicBloom.Koreo;
using UltimateSpawner.Spawning;
using UnityEngine;

[RequireComponent(typeof(Timeline))]
public class EnemyBasicSetup : BaseBehaviour, IDamageable, IAttackAgent
{
    private float currentHealth;

    // Serialized fields
    [SerializeField]
    private string enemyType;

    [SerializeField, EventID]
    private string eventID;

    [SerializeField]
    private float startHealth = 100;

    [SerializeField]
    private float repeatAttackDelay;

    [Space]
    [SerializeField]
    private GameObject enemyModel;

    [SerializeField]
    private Pooler birthParticles;

    [SerializeField]
    private Pooler deathParticles;

    [SerializeField]
    private Pooler lockOnDisabledParticles; // Reference to the Pooler for the particle effect

    [SerializeField]
    private GameObject lockedonAnim;

    [SerializeField]
    private ParticleSystem trails;

    [SerializeField]
    private float shootSpeed; // Default value, adjust in Inspector

    [SerializeField]
    private float projectileLifetime = 5f; // Default value, adjust in Inspector

    [SerializeField]
    private float projectileScale = 1f; // Default value, adjust in Inspector

    [SerializeField]
    private Material alternativeProjectileMaterial; // New field to specify an alternative material

    [SerializeField]
    private EnemyBasicDamagablePart[] damageableParts; // Array of damageable parts

    public int hitsToKillPart = 3; // Number of hits required to destroy a damageable part

    [SerializeField]
    private EventReference shootingSound; // Renamed from firingSound

    [SerializeField]
    private EventReference shootingSound2; // New sound event

    private bool enemyPooling = true;
    private StudioEventEmitter musicPlayback;
    private bool particleSwitch;
    private bool constantFire;
    private bool locked;
    private float attackDistance;
    private float attackAngle;
    private float lastAttackTime = -1f;
    private string associatedPool;
    private GameObject playerTarget;
    private Timeline myTime;
    private Clock clock;
    private RVOController controller;

    // Cached components
    private Transform cachedTransform;
    private Rigidbody cachedRigidbody;
    private Collider cachedCollider;
    private StudioEventEmitter cachedMusicPlayback;
    private Timeline cachedMyTime;
    private Clock cachedClock;
    private RVOController cachedController;

    // Cache these values
    private Vector3 cachedShootDirection;
    private Quaternion cachedShootRotation;

    private EnemyBasicDamagablePart[] cachedDamageableParts;

    private bool isLockedOn = false; // Add this field

    // Flag to prevent multiple registrations
    private bool isRegisteredWithGameManager = false;

    // Awake method
    private void Awake()
    {
        CacheComponents();
        Initialize();
        RegisterWithGameManager(); // Register here
        if (currentHealth <= 0)
        {
            ResetHealth();
        }
    }

    private void CacheComponents()
    {
        cachedTransform = transform;
        cachedRigidbody = GetComponent<Rigidbody>();
        cachedCollider = GetComponent<Collider>();
        cachedMusicPlayback = FindMusicPlaybackEmitter();
        cachedMyTime = GetComponent<Timeline>();
        cachedClock = Timekeeper.instance.Clock("Test");
        cachedController = GetComponent<RVOController>();
        cachedDamageableParts = GetComponentsInChildren<EnemyBasicDamagablePart>(true);
    }

    private void Initialize()
    {
        playerTarget = GameObject.FindGameObjectWithTag("Player");
        InitializeDamagableParts();
        AssignPool();
        ResetHealth();
    }

    // OnEnable method
    private void OnEnable()
    {
        RegisterForEvents();
        ActivateEnemy();
        ResetHealth(); // Add this line
    }

    private void ActivateEnemy()
    {
        enemyModel.SetActive(true);
        SetLockOnStatus(false);
        FMODUnity.RuntimeManager.PlayOneShotAttached($"event:/Enemy/{enemyType}/Birth", gameObject);
        birthParticles.GetFromPool(cachedTransform.position, Quaternion.identity);
    }

    // OnDisable method
    private void OnDisable()
    {
        UnregisterForEvents();
    }

    private void RegisterForEvents()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.RegisterForEvents(eventID, OnMusicalEnemyShoot);
        }
    }

    private void UnregisterForEvents()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.UnregisterForEvents(eventID, OnMusicalEnemyShoot);
        }
    }

    private void InitializeDamagableParts()
    {
        foreach (var part in cachedDamageableParts)
        {
            part.mainEnemyScript = this;
        }
    }

    // Public methods
    public void AssignPool()
    {
        if (enemyPooling)
        {
            associatedPool = GetComponentInParent<SpawnPool>().poolName;
        }
    }

    public void Damage(float amount)
    {
        ConditionalDebug.Log($"Enemy {gameObject.name} received {amount} damage");

        HandleDamage(amount);
        if (currentHealth <= 0)
        {
            SetLockOnStatus(false); // Unlock when the enemy is hit and health is depleted
        }
    }

    // Rename this method from lockedStatus to SetLockOnStatus
    public void SetLockOnStatus(bool status)
    {
        isLockedOn = status;
        UpdateLockOnVisuals();
        GameManager.Instance.SetEnemyLockState(cachedTransform, status);
    }

    // Keep this method for GameManager compatibility
    public void lockedStatus(bool status)
    {
        SetLockOnStatus(status);
    }

    private void UpdateLockOnVisuals()
    {
        if (lockedonAnim != null)
        {
            lockedonAnim.SetActive(isLockedOn);
        }

        if (!isLockedOn && lockOnDisabledParticles != null)
        {
            lockOnDisabledParticles.GetFromPool(cachedTransform.position, Quaternion.identity);
        }
    }

    public bool IsAlive() => currentHealth > 0;

    public void ResetHealth()
    {
        currentHealth = startHealth;
        gameObject.SetActive(true);
    }

    public void DebugTriggerDeath()
    {
        StartCoroutine(Death());
    }

    public void Attack(Vector3 targetPosition)
    {
        lastAttackTime = Time.time;
    }

    public float AttackDistance() => attackDistance;

    public bool CanAttack() => lastAttackTime + repeatAttackDelay < Time.time;

    public float AttackAngle() => attackAngle;

    // Private methods
    private StudioEventEmitter FindMusicPlaybackEmitter()
    {
        StudioEventEmitter[] emitters = FindObjectsOfType<StudioEventEmitter>();

        foreach (var emitter in emitters)
        {
            if (emitter.gameObject.name == "FMOD Music")
            {
                return emitter;
            }
        }

        ConditionalDebug.LogError(
            $"FMOD Studio Event Emitter with name FMOD Music not found in the scene."
        );
        return null;
    }

    private void HandleDamage(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        SetLockOnStatus(false);

        ConditionalDebug.Log($"{gameObject} is taking damage of {amount}");

        if (currentHealth <= 0)
        {
            StartCoroutine(Death());
        }
    }

    private IEnumerator Death()
    {
        SetLockOnStatus(false);
        ConditionalDebug.Log("Enemy has died");

        // Add this line to update the score
        ScoreManager.Instance.AddScore(CalculateScoreValue());

        deathParticles.GetFromPool(cachedTransform.position, Quaternion.identity);
        FMODUnity.RuntimeManager.PlayOneShot(
            "event:/Enemy/" + enemyType + "/Death",
            cachedTransform.position
        );

        yield return StartCoroutine(TimeManager.Instance.RewindTime(-1f, 0.5f, 0f));

        yield return new WaitForSeconds(0.5f);
        enemyModel.SetActive(false);
        yield return new WaitForSeconds(0.5f);

        cachedTransform.position = Vector3.zero;
        SpawnableItems.InformSpawnableDestroyed(cachedTransform);
        PoolManager.Pools[associatedPool].Despawn(cachedTransform);
        Destroy(GetComponent<SpawnableIdentity>());
    }

    private int CalculateScoreValue()
    {
        return (int)(startHealth * 2f);
    }

    private void OnMusicalEnemyShoot(KoreographyEvent evt)
    {
        ConditionalDebug.Log($"[EnemyBasicSetup] OnMusicalEnemyShoot triggered for {gameObject.name}");
        if (!CanShoot())
        {
            ConditionalDebug.Log($"[EnemyBasicSetup] CanShoot returned false for {gameObject.name}");
            return;
        }

        UpdateShootDirection();

        if (ProjectileSpawner.Instance == null)
        {
            ConditionalDebug.LogError("[EnemyBasicSetup] ProjectileSpawner.Instance is null!");
            return;
        }

        Vector3 shootPosition = cachedTransform.position;

        ProjectileStateBased projectile = ProjectileSpawner.Instance.ShootProjectileFromEnemy(
            shootPosition,
            cachedShootRotation,
            shootSpeed,
            projectileLifetime,
            projectileScale,
            10f,
            enableHoming: true,
            material: alternativeProjectileMaterial,
            clockKey: "",
            accuracy: -1f,
            target: playerTarget.transform,
            isStatic: false
        );

        if (projectile != null)
        {
            projectile.SetHomingTarget(playerTarget.transform);
            
            // Play both shooting sounds
            if (shootingSound.IsNull)
            {
                ConditionalDebug.LogWarning($"[EnemyBasicSetup] Shooting sound not set for {gameObject.name}");
            }
            else
            {
                RuntimeManager.PlayOneShot(shootingSound, cachedTransform.position);
            }

            if (shootingSound2.IsNull)
            {
                ConditionalDebug.LogWarning($"[EnemyBasicSetup] Shooting sound 2 not set for {gameObject.name}");
            }
            else
            {
                RuntimeManager.PlayOneShot(shootingSound2, cachedTransform.position);
            }

            ConditionalDebug.Log($"[EnemyBasicSetup] Projectile successfully created and shot from {gameObject.name} at position {shootPosition} towards {playerTarget.name}. Projectile position: {projectile.transform.position}, Velocity: {projectile.rb?.velocity}, Target: {projectile.currentTarget}");
        }
        else
        {
            ConditionalDebug.LogError($"[EnemyBasicSetup] Failed to create projectile for {gameObject.name}");
        }

        lastAttackTime = Time.time;
    }

    private bool CanShoot()
    {
        return Time.timeScale != 0f
            && CanAttack()
            && playerTarget != null
            && playerTarget.activeInHierarchy;
    }

    private void UpdateShootDirection()
    {
        if (playerTarget != null)
        {
            cachedShootDirection = playerTarget.transform.position - cachedTransform.position;
            cachedShootDirection.Normalize();
            cachedShootRotation = Quaternion.LookRotation(cachedShootDirection);
        }
        else
        {
            ConditionalDebug.LogWarning("[EnemyBasicSetup] Player target is null in UpdateShootDirection");
        }
    }

    public void RegisterProjectiles()
    {
        if (ProjectileManager.Instance != null)
        {
            var projectiles = GetComponentsInChildren<ProjectileStateBased>(true);
            foreach (var projectile in projectiles)
            {
                if (projectile != null)
                {
                    ProjectileManager.Instance.RegisterProjectile(projectile);
                }
            }
        }
    }

    public bool IsLockedOn()
    {
        return isLockedOn;
    }

    // New method to register with GameManager
    private void RegisterWithGameManager()
    {
        if (GameManager.Instance != null)
        {
            if (!isRegisteredWithGameManager)
            {
                GameManager.Instance.RegisterEnemy(cachedTransform);
                isRegisteredWithGameManager = true;
                ConditionalDebug.Log($"[EnemyBasicSetup] {gameObject.name} registered with GameManager.");
            }
            else
            {
                ConditionalDebug.LogWarning($"[EnemyBasicSetup] {gameObject.name} is already registered with GameManager.");
            }
        }
        else
        {
            ConditionalDebug.LogError($"[EnemyBasicSetup] GameManager instance is null. Cannot register enemy {gameObject.name}.");
        }
    }
}
