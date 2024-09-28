using System;
using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tactical;
using BehaviorDesigner.Runtime.Tasks.Movement;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject;
using Chronos;
using DG.Tweening;
using HohStudios.Tools.ObjectParticleSpawner;
using SensorToolkit.Example;
using SickscoreGames;
using SickscoreGames.HUDNavigationSystem;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;
using FMODUnity;

public enum ProjectilePoolType
{
    // Add your pool types here, for example:
    Standard,
    Homing,
    Explosive,
}

// Define ProjectileVector3 if it's not defined elsewhere
[System.Serializable]
public struct ProjectileVector3
{
    public float x;
    public float y;
    public float z;

    public ProjectileVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static implicit operator Vector3(ProjectileVector3 pv3) =>
        new Vector3(pv3.x, pv3.y, pv3.z);

    public static implicit operator ProjectileVector3(Vector3 v3) =>
        new ProjectileVector3(v3.x, v3.y, v3.z);

    public static float Distance(ProjectileVector3 a, ProjectileVector3 b)
    {
        return Vector3.Distance((Vector3)a, (Vector3)b);
    }
}

public class ProjectileStateBased : MonoBehaviour
{
    private const string FIRST_TIME_ENABLED_KEY = "FIRST_TIME_ENABLED_KEY";

    // Existing fields and properties
    public bool targetLocking;
    public ProjectilePoolType poolType;
    public bool isParried = false;

    [HideInInspector]
    public Rigidbody rb;

    [HideInInspector]
    public TrailRenderer tRn;

    [HideInInspector]
    public LineRenderer lRn;

    [HideInInspector]
    public Timeline TLine;

    [Header("State")]
    public string projectileType;

    [HideInInspector]
    public string _currentTag;

    [HideInInspector]
    public bool collectable;

    [HideInInspector]
    public bool homing;
    private bool activeLine = false;
    private bool disableOnProjMove;
    internal bool shotAtEnemy;
    internal bool projHitPlayer = false;

    [HideInInspector]
    public bool lifetimeExtended = false;

    [Header("Movement")]
    public float bulletSpeed;
    public float bulletSpeedMultiplier = 4f;
    public float _rotateSpeed = 95;
    public float slowSpeed;
    public float turnRate = 360f;
    public float maxRotateSpeed = 180;
    public float maxDistance = 100f;

    [HideInInspector]
    public float initialSpeed;

    [Header("Targeting and Prediction")]
    public Transform currentTarget;
    public float _maxDistancePredict = 100;
    public float _minDistancePredict = 5;
    public float _maxTimePrediction = 5;
    public ProjectileVector3 _standardPrediction,
        _deviatedPrediction;
    public ProjectileVector3 predictedPosition { get; set; }

    [Header("Combat")]
    public float damageAmount = 10f;
    public float damageMultiplier = 1f;
    public float lifetime;
    private float originalLifetime;

    [Header("Accuracy and Approach")]
    [Range(0f, 1f)]
    public float accuracy = 1f;
    public float maxInaccuracyRadius = 5f;
    public float minInaccuracyRadius = 0.1f;
    public float minTurnRadius = 10f;
    public float maxTurnRadius = 50f;
    public float approachAngle = 45f;
    public float closeProximityThreshold = 2f;
    public float smoothApproachFactor = 0.1f;
    public float minDistanceToTarget = 0.1f;
    public float maxRotationSpeed = 360f;

    [Header("Visual Effects")]
    public Renderer modelRenderer;

    [HideInInspector]
    public Material myMaterial;
    public Color lockedProjectileColor;
    private Color originalProjectileColor;
    public TrailRenderer playerProjPath;
    public Material lockedStateMaterial;
    public VisualEffect currentLockedFX;

    // New fields for refactored functionality
    private ProjectileState currentState;
    private ProjectileMovement _movement;
    private ProjectileCombat _combat;
    private ProjectileVisualEffects _visualEffects;

    public ProjectileVector3 initialPosition;
    public ProjectileVector3 initialDirection;
    public float distanceTraveled = 0f;
    public float timeAlive = 0f;
    public bool hasHitTarget = false;
    public bool isPlayerShot = false;

    internal Vector3 _previousPosition;
    internal static GameObject shootingObject;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int DissolveCutoutProperty = Shader.PropertyToID(
        "_AdvancedDissolveCutoutStandardClip"
    );
    private MaterialPropertyBlock propBlock;

    private Transform cachedTransform;
    private Rigidbody cachedRigidbody;

    public static int EnemyLayerMask { get; private set; }

    public float maxSpeed = 50f;

    public float initialLifetime;

    private Vector3 _lastTargetPosition;

    public string CurrentStateName => currentState?.GetType().Name ?? "NoState";

    public float creationTime;

    public bool isMissing { get; set; }

    public bool isFromStaticEnemy = false;

    [SerializeField] private bool isSoundEnabled = true;
    [SerializeField] private EventReference movingSoundEvent;

    public bool IsSoundEnabled() => isSoundEnabled;

    public EventReference GetMovingSoundEvent()
    {
        return movingSoundEvent;
    }

    private void Awake()
    {
        cachedTransform = transform;
        cachedRigidbody = GetComponent<Rigidbody>();
        rb = cachedRigidbody; // Assign rb here
        tRn = GetComponent<TrailRenderer>();
        TLine = GetComponent<Timeline>();

        _currentTag = gameObject.tag;
        myMaterial = modelRenderer.material;
        originalProjectileColor = myMaterial.color;
        initialSpeed = bulletSpeed;
        initialLifetime = lifetime;

        if (shootingObject == null)
        {
            shootingObject = GameObject.FindGameObjectWithTag("Shooting");
        }

        propBlock = new MaterialPropertyBlock();

        // New initializations for refactored classes
        _movement = new ProjectileMovement(this);
        _combat = new ProjectileCombat(this);
        _visualEffects = new ProjectileVisualEffects(this);

        EnemyLayerMask = 1 << LayerMask.NameToLayer("Enemy");
    }

    void OnEnable()
    {
        lifetimeExtended = false;
        projHitPlayer = false;

        if (gameObject.tag == "LaunchableBulletLocked")
        {
            gameObject.tag = "LaunchableBullet";
        }

        if (PlayerPrefs.GetInt(FIRST_TIME_ENABLED_KEY, 0) == 0)
        {
            ConditionalDebug.Log("First Time Enabled");
            PlayerPrefs.SetInt(FIRST_TIME_ENABLED_KEY, 1);
        }
        else
        {
            modelRenderer.material = myMaterial;
            myMaterial.SetColor("_BaseColor", originalProjectileColor);
        }

        ChangeState(new EnemyShotState(this));

        disableOnProjMove = false;

        transform.GetChild(0).gameObject.SetActive(false);

        _currentTag = gameObject.tag;

        // Remove any existing LockedFX
        if (currentLockedFX != null)
        {
            ProjectileEffectManager.Instance.ReturnLockedFXToPool(currentLockedFX);
            currentLockedFX = null;
        }
    }

    private void OnDisable()
    {
        // Optional: Ensure projectile is returned to the pool only if not already handled
        if (!rb.isKinematic)
        {
            ProjectilePool.Instance.ReturnProjectile(this);
        }
    }

    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        TLine = gameObject.GetComponent<Timeline>();
        TLine.rewindable = true;
        tRn = GetComponent<TrailRenderer>();
        playerProjPath.enabled = false;

        _currentTag = gameObject.tag;
        _lastTargetPosition = currentTarget != null ? currentTarget.position : Vector3.zero;
    }

    public void CustomUpdate(float timeScale)
    {
        if (homing && currentTarget != null)
        {
            Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(directionToTarget), turnRate * Time.deltaTime * timeScale);
        }

        ConditionalDebug.Log($"[ProjectileStateBased] ID: {GetInstanceID()}, State: {CurrentStateName}, Position: {transform.position}, Velocity: {rb.velocity}, TimeScale: {timeScale}");

        currentState?.CustomUpdate(timeScale);

        UpdatePredictedPosition();

        if (!isPlayerShot || (isPlayerShot && currentState is PlayerShotState))
        {
            lifetime -= Time.deltaTime * timeScale;
            if (lifetime <= 0)
            {
                if (isPlayerShot && currentTarget != null)
                {
                    EnsureHit();
                }
                else
                {
                    Death();
                }
            }
        }

        UpdateTrackingInfo();
    }

    private void EnsureHit()
    {
        if (currentTarget != null)
        {
            transform.position = currentTarget.position;
            rb.velocity = Vector3.zero;

            Collider targetCollider = currentTarget.GetComponent<Collider>();
            if (targetCollider != null)
            {
                OnTriggerEnter(targetCollider);
            }
        }
        else
        {
            Death();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        currentState?.OnTriggerEnter(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        currentState?.OnCollisionEnter(collision);
    }

    public void Death()
    {
        // Log death event
        ConditionalDebug.Log($"[ProjectileStateBased] Projectile {GetInstanceID()} is dying.");

        // Unregister from ProjectileManager
        ProjectileManager.Instance.UnregisterProjectile(this);

        if (rb != null)
        {
            // Reset velocities before setting to kinematic
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Return to pool
        ProjectilePool.Instance.ReturnProjectileToPool(this);

        transform.DOKill();

        _visualEffects.PlayDeathEffect();
    }

    public void SetLifetime(float seconds)
    {
        lifetime = seconds;
        //originalLifetime = seconds;
    }

    public void ResetLifetime()
    {
        lifetime = originalLifetime;
    }

    public void SetHomingTarget(Transform target)
    {
        currentTarget = target;
        homing = (target != null);
        ConditionalDebug.Log($"[ProjectileStateBased] Homing target set to {(target != null ? target.name : "None")}. Homing: {homing}");
    }

    public void EnableHoming(bool enable)
    {
        homing = enable;
    }

    public float maxTurnRate = 360f;

    void FixedUpdate()
    {
        if (rb == null || rb.isKinematic)
        {
            return; // Skip processing if Rigidbody is kinematic or null
        }

        float timeScale = TLine.timeScale;
        float deltaTime = Time.fixedDeltaTime * timeScale;

        currentState?.FixedUpdate(timeScale);
        _movement.UpdateMovement(timeScale);

        lifetime -= deltaTime;
        if (lifetime <= 0)
        {
            Death();
        }
    }

    public void SetClock(string clockKey)
    {
        if (TLine != null)
        {
            TLine.globalClockKey = clockKey;
            ConditionalDebug.Log($"SetClock: Timeline's globalClockKey set to '{clockKey}'.");
        }
        else
        {
            ConditionalDebug.LogWarning("SetClock: Timeline component not found on this projectile.");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (homing && currentTarget != null)
        {
            // Draw a line from the projectile to the current target
            Gizmos.color = Color.red;
            Gizmos.DrawLine(cachedTransform.position, currentTarget.position);

            // Draw a line from the projectile to the predicted position
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(cachedTransform.position, predictedPosition);

            // Draw spheres at the current target and predicted positions
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentTarget.position, 0.5f);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(predictedPosition, 0.5f);
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw a sphere at the projectile's position
        Gizmos.color = CurrentStateName switch
        {
            "EnemyShotState" => Color.red,
            "PlayerShotState" => Color.blue,
            "PlayerLockedState" => Color.green,
            _ => Color.yellow
        };
        Gizmos.DrawSphere(transform.position, 0.5f);

        // Draw a line in the direction of the projectile's velocity
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + rb.velocity.normalized * 2f);
    }

    public void OnPlayerRicochetDodge()
    {
        _movement.OnPlayerRicochetDodge();
    }

    public void SetAccuracy(float newAccuracy)
    {
        accuracy = Mathf.Clamp01(newAccuracy);
    }

    public float GetAccuracy()
    {
        return accuracy;
    }

    public void UpdateTrackingInfo()
    {
        if (isPlayerShot && !hasHitTarget)
        {
            distanceTraveled += ProjectileVector3.Distance(
                (ProjectileVector3)cachedTransform.position,
                initialPosition
            );
            initialPosition = cachedTransform.position;
            timeAlive += Time.deltaTime;
        }
    }

    public void ResetForPool()
    {
        if (cachedRigidbody != null)
        {
            // Do not change isKinematic here; let the pool handle it
            // Only reset velocities if needed
            if (!cachedRigidbody.isKinematic)
            {
                cachedRigidbody.velocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
            }
        }

        this.enabled = true;

        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Stop();
            ps.Clear();
        }

        homing = false;
        currentTarget = null;
        lifetimeExtended = false;
        projHitPlayer = false;
        bulletSpeed = initialSpeed;
        lifetime = initialLifetime;

        ChangeState(new EnemyShotState(this));

        if (currentLockedFX != null)
        {
            ProjectileEffectManager.Instance.ReturnLockedFXToPool(currentLockedFX);
            currentLockedFX = null;
        }

        if (playerProjPath != null)
        {
            playerProjPath.enabled = false;
        }

        if (modelRenderer != null && myMaterial != null)
        {
            // Reset material or other properties as needed
        }

        accuracy = 1f;
        lifetime = initialLifetime;
        isMissing = false;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        _combat.SetDamageMultiplier(multiplier);
    }

    public void ApplyDamage(IDamageable target)
    {
        float finalDamage = damageAmount * damageMultiplier;
        Debug.Log($"Applying damage: {finalDamage} to target {target.GetType().Name}");
        target.Damage(finalDamage);
    }

    public void ChangeState(ProjectileState newState)
    {
        currentState?.OnStateExit();
        currentState = newState;
        currentState.OnStateEnter();
    }

    public ProjectileState GetCurrentState()
    {
        return currentState;
    }

    public Type GetCurrentStateType()
    {
        if (currentState != null)
        {
            ConditionalDebug.Log("Current state type: " + currentState.GetType().ToString());
            return currentState.GetType();
        }
        else
        {
            ConditionalDebug.Log("Current state is null");
            return null;
        }
    }

    public void UpdatePredictedPosition()
    {
        if (currentTarget == null)
            return;

        // Calculate target velocity
        Vector3 targetVelocity = Vector3.zero;
        Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            targetVelocity = targetRb.velocity;
        }
        else
        {
            // If no Rigidbody, estimate velocity based on position change
            Vector3 currentTargetPosition = currentTarget.position;
            targetVelocity = (currentTargetPosition - _lastTargetPosition) / Time.deltaTime;
            _lastTargetPosition = currentTargetPosition;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        float predictionTime = Mathf.Min(distanceToTarget / bulletSpeed, _maxTimePrediction);

        predictedPosition = new ProjectileVector3(
            currentTarget.position.x + targetVelocity.x * predictionTime,
            currentTarget.position.y + targetVelocity.y * predictionTime,
            currentTarget.position.z + targetVelocity.z * predictionTime
        );
    }

    public void LogProjectileHit(string hitObject)
    {
        string message = isPlayerShot
            ? $"Player projectile hit {hitObject}. Distance traveled: {distanceTraveled:F2}, Time alive: {timeAlive:F2}"
            : $"Enemy projectile hit {hitObject}. Distance traveled: {distanceTraveled:F2}, Time alive: {timeAlive:F2}";
        ConditionalDebug.Log(message);
    }

    public void LogProjectileExpired()
    {
        string message = isPlayerShot
            ? $"Player projectile expired. Distance traveled: {distanceTraveled:F2}, Time alive: {timeAlive:F2}"
            : $"Enemy projectile expired. Distance traveled: {distanceTraveled:F2}, Time alive: {timeAlive:F2}";
        ConditionalDebug.Log(message);
    }

    public void ReportPlayerProjectileHit(bool hitTargetedEnemy, string enemyName)
    {
        string message = hitTargetedEnemy
            ? $"Player projectile hit its targeted enemy: {enemyName}. Distance traveled: {distanceTraveled:F2}, Time alive: {timeAlive:F2}"
            : $"Player projectile hit a non-targeted enemy: {enemyName}. Distance traveled: {distanceTraveled:F2}, Time alive: {timeAlive:F2}";
        ConditionalDebug.Log(message);
    }

    public void LogProjectileMiss()
    {
        ConditionalDebug.Log(
            $"Player projectile missed. Position: {transform.position}, Target: {(currentTarget != null ? currentTarget.name : "None")}, Distance to target: {(currentTarget != null ? Vector3.Distance(transform.position, currentTarget.position) : 0f)}"
        );
    }

     public void Initialize()
    {
        creationTime = Time.time;
    }

    // Add this method to check if the projectile should be deactivated
    public bool ShouldDeactivate(float currentTime)
    {
        return currentTime < creationTime;
    }

    public void ReturnToPool()
    {
        // Reset any necessary properties
        rb.velocity = Vector3.zero;
        transform.position = Vector3.zero;
        gameObject.SetActive(false);

        // Return to pool
        ProjectilePool.Instance.ReturnProjectileToPool(this);
    }

    public void SetupProjectile(float damage, float speed, float lifetime, bool homing, float scale, Transform target, bool isStatic)
    {
        this.damageAmount = damage;
        this.bulletSpeed = speed;
        this.lifetime = lifetime;
        this.homing = homing;
        this.currentTarget = target;
        this.isFromStaticEnemy = isStatic;
        
        transform.localScale = Vector3.one * scale;

        // Ensure the Rigidbody is non-kinematic
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = transform.forward * speed;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        else
        {
            Debug.LogWarning("Rigidbody is null on ProjectileStateBased");
        }

        ConditionalDebug.Log($"[ProjectileStateBased] Set up projectile with scale: {scale}, Target: {(target != null ? target.name : "None")}, IsStatic: {isStatic}, Speed: {speed}, Lifetime: {lifetime}, Homing: {homing}, isKinematic: {rb?.isKinematic}");

        // Only add radar symbol if it's not from a static enemy
        if (!isFromStaticEnemy)
        {
            GameObject radarSymbol = ProjectileEffectManager.Instance.GetRadarSymbolFromPool();
            if (radarSymbol != null)
            {
                radarSymbol.transform.SetParent(transform);
                radarSymbol.transform.localPosition = Vector3.zero;
                radarSymbol.SetActive(true);
            }
        }

        // Ensure the projectile is in the correct state
        ChangeState(new EnemyShotState(this, target));
    }
}