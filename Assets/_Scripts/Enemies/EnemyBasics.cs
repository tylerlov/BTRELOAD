using UnityEngine;
using Chronos;
using Typooling;
using PathologicalGames;
using FMODUnity;
using System.Collections.Generic;

public interface IProjectileRegistration
{
    void RegisterProjectiles();
}

[RequireComponent(typeof(Timeline))]
public class EnemyBasics : MonoBehaviour, IProjectileRegistration
{
    #region Properties
    [Header("Basic Settings")]
    [SerializeField] protected string enemyType;
    protected float currentHealth;

    [Header("References")]
    [SerializeField] protected GameObject enemyModel;
    [SerializeField] protected GameObject lockedOnIndicator;
    protected GameObject playerTarget;

    [Header("VFX")]
    [SerializeField] protected string birthEffectName = "EnemyBirth";
    [SerializeField] protected string deathEffectName = "EnemyDeath";
    [SerializeField] protected string lockDisabledEffectName = "LockDisabled";
    
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] public int hitsToKillPart = 3;
    [SerializeField] private Transform[] damageableParts;

    #region Pooling
    protected bool enemyPooling = true;
    protected string associatedPool;
    private Transform spawnPoint;  // Cached transform for spawning effects
    #endregion

    #region Time Management
    protected Timeline myTime;
    protected Clock clock;
    #endregion

    private Dictionary<Transform, int> partHitCounts = new Dictionary<Transform, int>();

    public bool IsAlive => currentHealth > 0;
    public Transform PlayerTransform { get; private set; }

    // Line of sight handling
    public virtual void HandleLineOfSightResult(bool hasLineOfSight)
    {
        // Base implementation can be overridden by derived classes
    }
    #endregion

    #region Unity Methods
    protected virtual void Awake()
    {
        myTime = GetComponent<Timeline>();
        InitializeComponents();
        PlayerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        
        // Create and cache a spawn point
        GameObject spawnPointObj = new GameObject("EffectSpawnPoint");
        spawnPoint = spawnPointObj.transform;
        spawnPointObj.transform.parent = transform;
    }

    protected virtual void OnEnable()
    {
        InitializeEnemy();
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        InitializeDamageableParts();
        SetupEnemy();
    }

    protected virtual void OnDisable()
    {
        // Cleanup if needed
    }

    protected virtual void OnDestroy()
    {
        if (spawnPoint != null)
        {
            Destroy(spawnPoint.gameObject);
        }
    }
    #endregion

    #region Initialization
    protected virtual void InitializeComponents()
    {
        playerTarget = GameObject.FindGameObjectWithTag("Player");
        clock = Timekeeper.instance.Clock("Test");
    }

    protected virtual void InitializeEnemy()
    {
        enemyModel.SetActive(true);
        
        // Play birth sound
        RuntimeManager.PlayOneShotAttached($"event:/Enemy/{enemyType}/Birth", gameObject);
        
        // Spawn birth VFX if configured
        SpawnEffect(birthEffectName, transform.position, Quaternion.identity);
    }

    private void InitializeDamageableParts()
    {
        if (damageableParts == null) return;

        foreach (var part in damageableParts)
        {
            if (part != null)
            {
                partHitCounts[part] = 0;
            }
        }
    }

    protected virtual void SetupEnemy()
    {
        if (lockedOnIndicator != null)
        {
            lockedOnIndicator.SetActive(false);
        }
    }
    #endregion

    #region Health Management
    public virtual void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        currentHealth -= amount;
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Alias for TakeDamage to maintain compatibility
    public void Damage(float amount)
    {
        TakeDamage(amount);
    }

    public virtual void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    protected virtual void Die()
    {
        SpawnEffect(deathEffectName, transform.position, Quaternion.identity);
        RuntimeManager.PlayOneShotAttached($"event:/Enemy/{enemyType}/Death", gameObject);
        
        if (enemyPooling)
        {
            PoolManager.Pools[associatedPool].Despawn(transform);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Projectile Registration
    public virtual void RegisterProjectiles()
    {
        // Base implementation - override in derived classes if needed
    }
    #endregion

    #region Utility Methods
    protected virtual void SpawnEffect(string effectName, Vector3 position, Quaternion rotation)
    {
        if (!string.IsNullOrEmpty(effectName) && PoolManager.Pools.ContainsKey(effectName))
        {
            var pool = PoolManager.Pools[effectName];
            if (pool != null)
            {
                // Use the cached spawn point
                spawnPoint.position = position;
                spawnPoint.rotation = rotation;

                var spawnedTransform = pool.Spawn(spawnPoint, spawnPoint);
                if (spawnedTransform == null)
                {
                    Debug.LogWarning($"Failed to spawn effect: {effectName}");
                }
            }
        }
    }

    public virtual void SetLockedOnIndicator(bool state)
    {
        if (lockedOnIndicator != null)
        {
            lockedOnIndicator.SetActive(state);
            if (!state)
            {
                SpawnEffect(lockDisabledEffectName, transform.position, Quaternion.identity);
            }
        }
    }

    public void HandlePartHit(Transform part)
    {
        if (!partHitCounts.ContainsKey(part)) return;

        partHitCounts[part]++;
        
        if (partHitCounts[part] >= hitsToKillPart)
        {
            DestroyPart(part);
        }
    }

    private void DestroyPart(Transform part)
    {
        if (part != null)
        {
            part.gameObject.SetActive(false);
            // Optional: Add particle effects or sound when part is destroyed
        }
    }

    public void ResetParts()
    {
        InitializeDamageableParts();
        if (damageableParts != null)
        {
            foreach (var part in damageableParts)
            {
                if (part != null)
                {
                    part.gameObject.SetActive(true);
                }
            }
        }
    }
    #endregion
}
