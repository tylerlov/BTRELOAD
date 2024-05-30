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

    // Awake method
    private void Awake()
    {
        FindMusicPlaybackEmitter();
        shootRewind = FindObjectOfType<Crosshair>();
    }

    // OnEnable method
    private void OnEnable()
    {
        // Ensure Koreographer instance is available before registering for events
        if (Koreographer.Instance != null)
        {
            InitializeEnemy();
            Koreographer.Instance.RegisterForEvents(eventID, OnMusicalEnemyShoot);
        }
        else
        {
            // Listen to the sceneLoaded event to wait for the scene to be fully loaded
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Unsubscribe to avoid this method being called again unnecessarily
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        // Now that the scene is loaded, attempt to initialize and register for events again
        if (Koreographer.Instance != null)
        {
            InitializeEnemy();
            Koreographer.Instance.RegisterForEvents(eventID, OnMusicalEnemyShoot);
        }
    }

    // OnDisable method
    private void OnDisable()
    {
        // Unregister from events to avoid calling methods on a destroyed object
        Koreographer.Instance.UnregisterForEvents(eventID, OnMusicalEnemyShoot);
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
            // Use the Pooler to get the particle effect from the pool at the current position and rotation
            lockOnDisabledParticles.GetFromPool(transform.position, Quaternion.identity);
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
    private void FindMusicPlaybackEmitter()
    {
        StudioEventEmitter[] emitters = FindObjectsOfType<StudioEventEmitter>();
                
        foreach (var emitter in emitters)
        {
            if (emitter.gameObject.name == "FMOD Music")
            {
                musicPlayback = emitter;
                break;
            }
        }
        
        if (musicPlayback == null)
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
        shootRewind = GameObject.FindGameObjectWithTag("Shooting").GetComponent<Crosshair>();
        clock = Timekeeper.instance.Clock("Test");
        controller = gameObject.GetComponent<RVOController>();
        birthParticles.GetFromPool(transform.position, Quaternion.identity);
    }

    private void SetupEnemy()
    {
        AssignPool();
        locked = false;

        myTime = GetComponent<Timeline>();
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

        StartCoroutine(shootRewind.RewindToBeatEnemyDeath());

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
        // Check if playerTarget is null or destroyed before proceeding
        if (playerTarget == null || !playerTarget.activeInHierarchy)
        {
            return; // Exit the method if playerTarget is not valid
        }

        if (Time.timeScale != 0f && CanAttack())
        {
            // Assuming you have a target for homing projectiles. 
            // This could be the player or any other target in your game.
            Transform homingTarget = playerTarget.transform; // Example target

            // Call ProjectileManager to shoot a homing projectile
            ProjectileManager.Instance.ShootProjectileFromEnemy(
                transform.position, // Position from where the projectile is shot
                Quaternion.LookRotation(homingTarget.position - transform.position), // Rotation towards the target
                shootSpeed, // Speed of the projectile
                projectileLifetime, // Lifetime of the projectile
                projectileScale, // Scale of the projectile
                true, // Enable homing
                alternativeProjectileMaterial // Pass the alternative material instead of color
            );

            lastAttackTime = Time.time;
        }
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
