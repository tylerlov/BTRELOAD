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
using UnityEngine.VFX;

[RequireComponent(typeof(Timeline))]
public class EnemyExplodeSetup : BaseBehaviour, IDamageable, IAttackAgent
{
    [Header("Enemy Properties")]
    [SerializeField] private string enemyType;
    [SerializeField] private float startHealth = 100;
    [SerializeField] private GameObject enemyModel;
    [SerializeField] private Renderer enemyRenderer;

    [Header("Explosion Settings")]
    [SerializeField] private float explosionDamage = 50f;
    [SerializeField] private float explosionRadius = 3f;

    [Header("Attack Settings")]
    [SerializeField] private float repeatAttackDelay;

    [Header("Visual Effects")]
    [SerializeField] private Pooler birthParticles;
    [SerializeField] private Pooler deathParticles;
    [SerializeField] private Pooler lockOnDisabledParticles;
    [SerializeField] private GameObject lockedonAnim;
    [SerializeField] private ParticleSystem trails;
    [SerializeField] private VisualEffect pulseVisualEffect;

    [Header("Pulse Settings")]
    [SerializeField] private Color pulseColor = Color.red;
    [SerializeField] private float pulseDuration = 0.1f;

    [Header("Distance Settings")]
    [SerializeField] private float farDistance = 10f;
    [SerializeField] private float mediumDistance = 5f;

    [Header("Pulse Timings")]
    [SerializeField, EventID] private string farPulseEventID;
    [SerializeField, EventID] private string mediumPulseEventID;
    [SerializeField, EventID] private string closePulseEventID;
    [SerializeField] private EventReference pulseSound;
    [SerializeField] private EventReference explosionSound;

    // Private fields (not visible in inspector)
    private float currentHealth;
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

    private bool isLockedOn = false;
    private bool hasExploded = false;

    private Material enemyMaterial;
    private Color originalColor;
    private string currentEventID;

    private EventInstance pulseSoundInstance;
    private EventInstance explosionSoundInstance;

    private bool isRegisteredWithGameManager = false;

    private void Awake()
    {
        CacheComponents();
        Initialize();
        RegisterWithGameManager();
        if (currentHealth <= 0)
        {
            ResetHealth();
        }

        if (pulseVisualEffect == null)
        {
            pulseVisualEffect = GetComponent<VisualEffect>();
            if (pulseVisualEffect == null)
            {
                ConditionalDebug.LogError("No VisualEffect component found on " + gameObject.name);
            }
        }

        // Initialize the pulse sound instance
        if (!pulseSound.IsNull)
        {
            pulseSoundInstance = RuntimeManager.CreateInstance(pulseSound);
        }
        else
        {
            ConditionalDebug.LogWarning("Pulse sound event is not assigned on " + gameObject.name);
        }

        // Initialize the explosion sound instance
        if (!explosionSound.IsNull)
        {
            explosionSoundInstance = RuntimeManager.CreateInstance(explosionSound);
        }
        else
        {
            ConditionalDebug.LogWarning("Explosion sound event is not assigned on " + gameObject.name);
        }
    }

    private void RegisterWithGameManager()
    {
        if (GameManager.Instance != null && !isRegisteredWithGameManager)
        {
            GameManager.Instance.RegisterEnemy(cachedTransform);
            isRegisteredWithGameManager = true;
            ConditionalDebug.Log($"[EnemyExplodeSetup] {gameObject.name} registered with GameManager.");
        }
    }

    private void OnDestroy()
    {
        // Release the FMOD event instances when the object is destroyed
        if (pulseSoundInstance.isValid())
        {
            pulseSoundInstance.release();
        }
        if (explosionSoundInstance.isValid())
        {
            explosionSoundInstance.release();
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
    }

    private void Initialize()
    {
        playerTarget = GameObject.FindGameObjectWithTag("Player");
        AssignPool();
        ResetHealth();
    }

    private void OnEnable()
    {
        ActivateEnemy();
        ResetHealth();
        UpdatePulseEvent(); // This will now register for events
    }

    private void OnDisable()
    {
        UnregisterForEvents();
    }

    private void RegisterForEvents()
    {
        if (Koreographer.Instance != null && !string.IsNullOrEmpty(currentEventID))
        {
            Koreographer.Instance.RegisterForEvents(currentEventID, OnPulseEvent);
            ConditionalDebug.Log($"Registered for event: {currentEventID} on {gameObject.name}");
        }
    }

    private void UnregisterForEvents()
    {
        if (Koreographer.Instance != null && !string.IsNullOrEmpty(currentEventID))
        {
            Koreographer.Instance.UnregisterForEvents(currentEventID, OnPulseEvent);
            ConditionalDebug.Log($"Unregistered from event: {currentEventID} on {gameObject.name}");
        }
    }

    private void OnPulseEvent(KoreographyEvent evt)
    {
        StartCoroutine(PulseEffect());
        PlayPulseSound();
    }

    private IEnumerator PulseEffect()
    {
        if (pulseVisualEffect != null)
        {
            pulseVisualEffect.Play();
            yield return new WaitForSeconds(pulseDuration);
            pulseVisualEffect.Stop();
        }
    }

    private void PlayPulseSound()
    {
        if (pulseSoundInstance.isValid())
        {
            pulseSoundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
            pulseSoundInstance.start();
        }
        else
        {
            ConditionalDebug.LogWarning("Pulse sound instance is not valid on " + gameObject.name);
        }
    }

    private void ActivateEnemy()
    {
        enemyModel.SetActive(true);
        SetLockOnStatus(false);
        FMODUnity.RuntimeManager.PlayOneShotAttached($"event:/Enemy/{enemyType}/Birth", gameObject);
        birthParticles.GetFromPool(cachedTransform.position, Quaternion.identity);
    }

    public void AssignPool()
    {
        if (enemyPooling)
        {
            SpawnPool spawnPool = GetComponentInParent<SpawnPool>();
            if (spawnPool != null)
            {
                associatedPool = spawnPool.poolName;
            }
            else
            {
                ConditionalDebug.LogWarning($"No SpawnPool found in parent hierarchy of {gameObject.name}");
            }
        }
    }

    public void Damage(float amount)
    {
        ConditionalDebug.Log($"Enemy {gameObject.name} received {amount} damage");

        HandleDamage(amount);
        if (currentHealth <= 0)
        {
            SetLockOnStatus(false);
        }
    }

    public void SetLockOnStatus(bool status)
    {
        isLockedOn = status;
        UpdateLockOnVisuals();
        GameManager.Instance.SetEnemyLockState(cachedTransform, status);
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

    public void Attack(Vector3 targetPosition)
    {
        // Do nothing for exploding enemy
    }

    public float AttackDistance() => attackDistance;

    public bool CanAttack() => lastAttackTime + repeatAttackDelay < Time.time;

    public float AttackAngle() => attackAngle;

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

    private void Explode()
    {
        if (hasExploded) return; // Prevent multiple explosions
        hasExploded = true;
        DealDamageToPlayer();
        StartCoroutine(ExplodeAndDie());
    }

    private IEnumerator ExplodeAndDie()
    {
        SetLockOnStatus(false);
        ConditionalDebug.Log("Enemy has exploded");

        ScoreManager.Instance.AddScore(CalculateScoreValue());

        // Play death effects
        deathParticles.GetFromPool(cachedTransform.position, Quaternion.identity);
        
        // Play explosion sound
        if (explosionSoundInstance.isValid())
        {
            explosionSoundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
            explosionSoundInstance.start();
        }
        else
        {
            ConditionalDebug.LogWarning("Explosion sound instance is not valid on " + gameObject.name);
        }

        yield return StartCoroutine(TimeManager.Instance.RewindTime(-1f, 0.5f, 0f));

        yield return new WaitForSeconds(0.5f);
        enemyModel.SetActive(false);
        yield return new WaitForSeconds(0.5f);

        cachedTransform.position = Vector3.zero;
        SpawnableItems.InformSpawnableDestroyed(cachedTransform);
        PoolManager.Pools[associatedPool].Despawn(cachedTransform);
        Destroy(GetComponent<SpawnableIdentity>());
    }

    private IEnumerator Death()
    {
        if (!hasExploded)
        {
            yield return StartCoroutine(ExplodeAndDie());
        }
    }

    private int CalculateScoreValue()
    {
        return (int)(startHealth * 2f);
    }

    private void OnMusicalEnemyEvent(KoreographyEvent evt)
    {
        // Trigger the pulse effect on each musical event
        StartCoroutine(PulseEffect());
    }

    private void UpdatePulseEvent()
    {
        float distanceToPlayer = Vector3.Distance(cachedTransform.position, playerTarget.transform.position);
        string newEventID;

        if (distanceToPlayer > farDistance)
        {
            newEventID = farPulseEventID;
        }
        else if (distanceToPlayer > mediumDistance)
        {
            newEventID = mediumPulseEventID;
        }
        else
        {
            newEventID = closePulseEventID;
        }

        if (newEventID != currentEventID)
        {
            UnregisterForEvents();
            currentEventID = newEventID;
            RegisterForEvents();
            ConditionalDebug.Log($"Updated pulse event for {gameObject.name}. Distance: {distanceToPlayer}, New Event: {newEventID}");
        }
    }

    public bool IsLockedOn()
    {
        return isLockedOn;
    }

    private void DealDamageToPlayer()
    {
        if (playerTarget != null)
        {
            IDamageable playerDamageable = playerTarget.GetComponent<IDamageable>();
            if (playerDamageable != null)
            {
                playerDamageable.Damage(explosionDamage);
            }
            else
            {
                ConditionalDebug.LogWarning("Player does not implement IDamageable interface");
            }
        }
        else
        {
            ConditionalDebug.LogWarning("Player target is null");
        }
    }

    private void PlayPulseVFX()
    {
        if (pulseVisualEffect != null)
        {
            pulseVisualEffect.Play();
        }
        else
        {
            ConditionalDebug.LogWarning("Pulse Visual Effect is not assigned on " + gameObject.name);
        }
    }

    private void Update()
    {
        if (playerTarget == null)
        {
            playerTarget = GameObject.FindGameObjectWithTag("Player");
            if (playerTarget == null)
            {
                ConditionalDebug.LogWarning($"Player not found for {gameObject.name}");
                return;
            }
        }
        UpdatePulseEvent();
    }
}