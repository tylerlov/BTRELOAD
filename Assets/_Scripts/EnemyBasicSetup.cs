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
    private StudioEventEmitter cachedMusicPlayback;
    private Crosshair cachedShootRewind;
    private Timeline cachedMyTime;
    private Clock cachedClock;
    private RVOController cachedController;

    // Awake method
    private void Awake()
    {
        CacheComponents();
    }

    private void CacheComponents()
    {
        FindMusicPlaybackEmitter();
        cachedShootRewind = FindObjectOfType<Crosshair>();
        cachedMyTime = GetComponent<Timeline>();
        cachedClock = Timekeeper.instance.Clock("Test");
        cachedController = GetComponent<RVOController>();
    }

    // OnEnable method
    private void OnEnable()
    {
        if (Koreographer.Instance != null)
        {
            InitializeEnemy();
            Koreographer.Instance.RegisterForEvents(eventID, OnMusicalEnemyShoot);
        }
        else
        {
            StartCoroutine(WaitForKoreographer());
        }
    }

    private IEnumerator WaitForKoreographer()
    {
        while (Koreographer.Instance == null)
        {
            yield return null;
        }
        InitializeEnemy();
        Koreographer.Instance.RegisterForEvents(eventID, OnMusicalEnemyShoot);
    }

    private void OnDisable()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.UnregisterForEvents(eventID, OnMusicalEnemyShoot);
        }
    }


    // Start method
    private void Start()
    {
        SetupEnemy();
        InitializeDamagableParts();
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
            lockOnDisabledParticles.GetFromPool(transform.position, Quaternion.identity);
        }
        
        // Inform GameManager of the lock state change
        GameManager.instance.SetEnemyLockState(transform, status);
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
    private void FindMusicPlaybackEmitter()
    {
        StudioEventEmitter[] emitters = FindObjectsOfType<StudioEventEmitter>();
                
        foreach (var emitter in emitters)
        {
            if (emitter.gameObject.name == "FMOD Music")
            {
                cachedMusicPlayback = emitter;
                break;
            }
        }
        
        if (cachedMusicPlayback == null)
        {
            ConditionalDebug.LogError($"FMOD Studio Event Emitter with name FMOD Music not found in the scene.");
        }
    }

    private void InitializeEnemy()
    {
        playerTarget = GameObject.FindGameObjectWithTag("Player");
        enemyModel.SetActive(true);
        lockedStatus(false);
        currentHealth = startHealth;
        FMODUnity.RuntimeManager.PlayOneShotAttached("event:/Enemy/" + enemyType + "/Birth", gameObject);
        cachedShootRewind = GameObject.FindGameObjectWithTag("Shooting").GetComponent<Crosshair>();
        cachedClock = Timekeeper.instance.Clock("Test");
        cachedController = gameObject.GetComponent<RVOController>();
        birthParticles.GetFromPool(transform.position, Quaternion.identity);
    }

    private void SetupEnemy()
    {
        AssignPool();
        locked = false;

        cachedMyTime = GetComponent<Timeline>();
    }

    private void HandleDamage(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        lockedStatus(false);

        ConditionalDebug.Log($"{gameObject} is taking damage of {amount}");

        if (currentHealth == 0)
        {
            StartCoroutine(Death());
        }
    }

    private IEnumerator Death()
    {
        lockedStatus(false);
        ConditionalDebug.Log("Enemy has died");

        deathParticles.GetFromPool(transform.position, Quaternion.identity);
        FMODUnity.RuntimeManager.PlayOneShot("event:/Enemy/" + enemyType + "/Death", transform.position);

        StartCoroutine(cachedShootRewind.RewindToBeatEnemyDeath());

        yield return new WaitForSeconds(0.5f);
        enemyModel.SetActive(false);
        yield return new WaitForSeconds(0.5f);

        transform.position = Vector3.zero;
        SpawnableItems.InformSpawnableDestroyed(transform);
        //Despawn the object and remove SpawnableIdentity component to prevent any issues when respawning
        PoolManager.Pools[associatedPool].Despawn(transform);
        Destroy(GetComponent<SpawnableIdentity>());
    }

    void OnMusicalEnemyShoot(KoreographyEvent evt)
    {
        if (Time.timeScale == 0f || !CanAttack() || playerTarget == null || !playerTarget.activeInHierarchy)
        {
            return;
        }

        Vector3 targetPosition = playerTarget.transform.position;
        Vector3 shootDirection = (targetPosition - transform.position).normalized;
        
        ProjectileManager.Instance.ShootProjectileFromEnemy(
            transform.position,
            Quaternion.LookRotation(shootDirection),
            shootSpeed,
            projectileLifetime,
            projectileScale,
            true,
            alternativeProjectileMaterial
        );

        lastAttackTime = Time.time;
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
}