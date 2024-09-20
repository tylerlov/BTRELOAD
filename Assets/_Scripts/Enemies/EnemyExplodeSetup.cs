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
    [SerializeField] private Pooler lockOnDisabledParticles;
    [SerializeField] private GameObject lockedonAnim;
    [SerializeField] private ParticleSystem trails;

    [SerializeField] private float explosionDamage = 50f;
    [SerializeField] private float explosionRadius = 3f;

    [SerializeField] private Renderer enemyRenderer;
    [SerializeField] private Color pulseColor = Color.red;
    [SerializeField] private float maxPulseIntensity = 1.5f;
    [SerializeField, EventID] private string farPulseEventID;
    [SerializeField, EventID] private string mediumPulseEventID;
    [SerializeField, EventID] private string closePulseEventID;
    [SerializeField] private float farDistance = 10f;
    [SerializeField] private float mediumDistance = 5f;

    // Add these new fields
    [SerializeField, EventID] private string pulseEventID; // Use this for the pulse event
    [SerializeField] private VisualEffect pulseVisualEffect;


    [SerializeField] private float maxPulseInterval = 2f;
    [SerializeField] private float minPulseInterval = 0.5f;
    [SerializeField] private float pulseDuration = 0.1f;


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

    private Coroutine pulseCoroutine;

    private void Awake()
    {
        CacheComponents();
        Initialize();
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
        RegisterForEvents();
        ActivateEnemy();
        ResetHealth();
        StartPulsing();
    }

    private void OnDisable()
    {
        UnregisterForEvents();
        StopPulsing();
    }

    private void StartPulsing()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        pulseCoroutine = StartCoroutine(ContinuousPulsing());
    }

    private void StopPulsing()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        if (pulseVisualEffect != null)
        {
            pulseVisualEffect.Stop();
        }
    }

       private IEnumerator ContinuousPulsing()
    {
        while (true)
        {
            if (pulseVisualEffect != null && playerTarget != null)
            {
                float distanceToPlayer = Vector3.Distance(cachedTransform.position, playerTarget.transform.position);
                float t = Mathf.InverseLerp(farDistance, 0, distanceToPlayer);
                float interval = Mathf.Lerp(maxPulseInterval, minPulseInterval, t);

                pulseVisualEffect.Play();
                yield return new WaitForSeconds(pulseDuration);
                pulseVisualEffect.Stop();

                yield return new WaitForSeconds(interval - pulseDuration);
            }
            else
            {
                yield return null;
            }
        }
    }
    private void ActivateEnemy()
    {
        enemyModel.SetActive(true);
        SetLockOnStatus(false);
        FMODUnity.RuntimeManager.PlayOneShotAttached($"event:/Enemy/{enemyType}/Birth", gameObject);
        birthParticles.GetFromPool(cachedTransform.position, Quaternion.identity);
    }

    private void RegisterForEvents()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.RegisterForEvents(pulseEventID, OnPulseEvent);
        }
    }

    private void UnregisterForEvents()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.UnregisterForEvents(pulseEventID, OnPulseEvent);
        }
    }

    private void OnPulseEvent(KoreographyEvent evt)
    {
        PlayPulseVFX();
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
        FMODUnity.RuntimeManager.PlayOneShot(
            "event:/Enemy/" + enemyType + "/Explosion",
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

    private void UpdatePulseEvent(float distance)
    {
        string newEventID;

        if (distance > farDistance)
        {
            newEventID = farPulseEventID;
        }
        else if (distance > mediumDistance)
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
        }
    }

    private IEnumerator PulseEffect()
    {
        if (pulseVisualEffect != null)
        {
            pulseVisualEffect.Play();
            // You might want to adjust this wait time based on your visual effect
            yield return new WaitForSeconds(0.1f);
            pulseVisualEffect.Stop();
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
}