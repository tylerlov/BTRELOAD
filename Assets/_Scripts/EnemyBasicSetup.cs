using UnityEngine;
using System.Collections;
using Pathfinding;
using Pathfinding.Examples;
using SonicBloom.Koreo;
using DG.Tweening;
using FMODUnity;
using FMOD.Studio;
using BehaviorDesigner.Runtime.Tactical;
using PathologicalGames;
using Chronos;
using UltimateSpawner.Spawning;
using System;
using Pathfinding.RVO;
using OccaSoftware.BOP;

[RequireComponent(typeof(Timeline))] 
public class EnemyBasicSetup : BaseBehaviour, IDamageable, IAttackAgent
{
    private float currentHealth;
    // Serialized fields
    [SerializeField] private string enemyType;
    [SerializeField, EventID] private string eventID;
    [SerializeField] private float startHealth = 100;
    [SerializeField] private float repeatAttackDelay;
    [Space]
    [SerializeField] private GameObject enemyModel;
    [SerializeField] private Pooler birthParticles;
    [SerializeField] private Pooler deathParticles;
    [SerializeField] private Pooler lockOnDisabledParticles; // Reference to the Pooler for the particle effect

    [SerializeField] private GameObject lockedonAnim;
    [SerializeField] private ParticleSystem trails;
    [SerializeField] private float shootSpeed; // Default value, adjust in Inspector
    [SerializeField] private float projectileLifetime = 5f; // Default value, adjust in Inspector
    [SerializeField] private float projectileScale = 1f; // Default value, adjust in Inspector
    [SerializeField] private Material alternativeProjectileMaterial; // New field to specify an alternative material
    [SerializeField] private EnemyBasicDamagablePart[] damageableParts; // Array of damageable parts

    [SerializeField] private float partDamageAmount = 10f; // New field for part damage

    public int hitsToKillPart = 3; // Number of hits required to destroy a damageable part

    private bool enemyPooling = true;
    private Crosshair shootRewind;
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
    private Crosshair cachedShootRewind;
    private Timeline cachedMyTime;
    private Clock cachedClock;
    private RVOController cachedController;

    // Cache these values
    private Vector3 cachedShootDirection;
    private Quaternion cachedShootRotation;

    // Awake method
    private void Awake()
    {
        CacheComponents();
        Initialize();
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
        cachedShootRewind = FindObjectOfType<Crosshair>();
        cachedMyTime = GetComponent<Timeline>();
        cachedClock = Timekeeper.instance.Clock("Test");
        cachedController = GetComponent<RVOController>();
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
        lockedStatus(false);
        FMODUnity.RuntimeManager.PlayOneShotAttached($"event:/Enemy/{enemyType}/Birth", gameObject);
        birthParticles.GetFromPool(cachedTransform.position, Quaternion.identity);
    }

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
        if (damageableParts != null && damageableParts.Length > 0)
        {
            foreach (var part in damageableParts)
            {
                part.mainEnemyScript = this;
            }
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
        HandleDamage(amount);
        if (currentHealth <= 0)
        {
            lockedStatus(false); // Unlock when the enemy is hit and health is depleted
        }
    }

    public void lockedStatus(bool status)
    {
        locked = status;
        lockedonAnim.SetActive(status);
        if (!status && lockOnDisabledParticles != null)
        {
            lockOnDisabledParticles.GetFromPool(cachedTransform.position, Quaternion.identity);
        }
        
        // Inform GameManager of the lock state change
        GameManager.instance.SetEnemyLockState(cachedTransform, status);
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
        
        ConditionalDebug.LogError($"FMOD Studio Event Emitter with name FMOD Music not found in the scene.");
        return null;
    }

    private void HandleDamage(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        lockedStatus(false);

        ConditionalDebug.Log($"{gameObject} is taking damage of {amount}");

        if (currentHealth <= 0)
        {
            StartCoroutine(Death());
        }
    }

    private IEnumerator Death()
    {
        lockedStatus(false);
        ConditionalDebug.Log("Enemy has died");

        deathParticles.GetFromPool(cachedTransform.position, Quaternion.identity);
        FMODUnity.RuntimeManager.PlayOneShot("event:/Enemy/" + enemyType + "/Death", cachedTransform.position);

        yield return StartCoroutine(GameManager.instance.RewindTime(-1f, 0.5f, 0f));

        yield return new WaitForSeconds(0.5f);
        enemyModel.SetActive(false);
        yield return new WaitForSeconds(0.5f);

        cachedTransform.position = Vector3.zero;
        SpawnableItems.InformSpawnableDestroyed(cachedTransform);
        PoolManager.Pools[associatedPool].Despawn(cachedTransform);
        Destroy(GetComponent<SpawnableIdentity>());
    }

    private void OnMusicalEnemyShoot(KoreographyEvent evt)
    {
        if (!CanShoot())
        {
            return;
        }

        UpdateShootDirection();
        
        ProjectileManager.Instance.ShootProjectileFromEnemy(
            cachedTransform.position,
            cachedShootRotation,
            shootSpeed,
            projectileLifetime,
            projectileScale,
            true,
            alternativeProjectileMaterial
        );

        lastAttackTime = Time.time;
    }

    private bool CanShoot()
    {
        return Time.timeScale != 0f && 
               CanAttack() && 
               playerTarget != null && 
               playerTarget.activeInHierarchy;
    }

    private void UpdateShootDirection()
    {
        cachedShootDirection = playerTarget.transform.position - cachedTransform.position;
        cachedShootDirection.Normalize();
        cachedShootRotation = Quaternion.LookRotation(cachedShootDirection);
    }

    public void RegisterProjectiles()
    {
        // Assuming you have a list or array of projectiles associated with this enemy
        foreach (var projectile in GetComponentsInChildren<ProjectileStateBased>())
        {
            if (projectile != null)
            {
                // Register the projectile with the ProjectileManager
                ProjectileManager.Instance.RegisterProjectile(projectile);
            }
        }
    }

    // New method to get the part damage amount
    public float GetPartDamageAmount()
    {
        return partDamageAmount;
    }
}