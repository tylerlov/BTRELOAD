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

public enum ProjectilePoolType
{
    StaticEnemy,
    EnemyBasicSetup,
    // Add more types as needed
}

public abstract class ProjectileState
{
    protected ProjectileStateBased _projectile;

    public ProjectileState(ProjectileStateBased projectile)
    {
        _projectile = projectile;
    }

    public virtual void FixedUpdate(float timeScale) { }

    public virtual void OnTriggerEnter(Collider other) { }

    public virtual void OnDeath() { }

    public virtual void OnStateEnter() { }

    public virtual void OnStateExit() { }

    public virtual void Update()
    {
        ProjectileManager.Instance.PredictAndRotateProjectile(_projectile);
    }

    public virtual void CustomUpdate(float timeScale) { }

    public ProjectileStateBased GetProjectile()
    {
        return _projectile;
    }
}

public class EnemyShotState : ProjectileState
{
    public EnemyShotState(ProjectileStateBased projectile)
        : base(projectile)
    {
        // Enemy Shot State initialization logic here...
        _projectile.currentTarget = GameObject.FindWithTag("Player Aim Target").transform;
        // Remove the hard-coded lifetime setting
        // _projectile.SetLifetime(10f);

        _projectile.minTurnRadius = 5f; // Reduced from 10f
        _projectile.maxTurnRadius = 20f; // Reduced from 50f
        _projectile.approachAngle = UnityEngine.Random.Range(15f, 30f); // Reduced range
        _projectile.turnRate = 180f; // Increased turn rate for sharper turns
    }

    public override void OnTriggerEnter(Collider other)
    {
        // Behavior for OnTriggerEnter in EnemyShotState
        if (other.gameObject.CompareTag("Player"))
        {
            IDamageable damageable = other.gameObject.GetComponent("PlayerHealth") as IDamageable;
            if (damageable != null)
            {
                damageable.Damage(_projectile.damageAmount);
                _projectile.projHitPlayer = true;
                _projectile.Death();
                ProjectileManager.Instance.PlayOneShotSound(
                    "event:/Projectile/Basic/Impact",
                    _projectile.transform.position
                );
            }
        }
        else
        {
            ConditionalDebug.Log(
                "Collided with an object "
                    + other.gameObject
                    + " at transform "
                    + other.gameObject.transform
                    + "and behaviour may not be expected"
            );
        }
    }
}

public class PlayerLockedState : ProjectileState
{
    public PlayerLockedState(ProjectileStateBased projectile)
        : base(projectile)
    {
        if (_projectile.isParried)
            return;

        _projectile.SetLifetime(200f);

        _projectile.currentTarget = null;
        _projectile.homing = false;
        _projectile.tag = "LaunchableBulletLocked";

        _projectile.TLine.ResetRecording();
        _projectile.TLine.rewindable = false;
        _projectile.TLine.globalClockKey = "Test";

        if (ProjectileStateBased.shootingObject != null)
        {
            _projectile.transform.SetParent(ProjectileStateBased.shootingObject.transform, true);
            Vector3 newPosition =
                ProjectileStateBased.shootingObject.transform.position
                + ProjectileStateBased.shootingObject.transform.forward * 2f;
            _projectile.transform.position = newPosition;
            _projectile.transform.rotation = ProjectileStateBased.shootingObject.transform.rotation;

            // Instead of freezing constraints, set the Rigidbody to kinematic
            if (_projectile.rb != null)
            {
                _projectile.rb.isKinematic = true;
            }
        }
        else
        {
            ConditionalDebug.LogError(
                "Shooting not found. Make sure there is a GameObject tagged 'Shooting' in the scene."
            );
        }

        _projectile.playerProjPath.enabled = true;
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();

        if (_projectile.modelRenderer != null && _projectile.lockedStateMaterial != null)
        {
            _projectile.modelRenderer.material = _projectile.lockedStateMaterial;
        }
        else
        {
            Debug.LogError("ModelRenderer or LockedStateMaterial is not set on the projectile.");
        }

        // Get and play the LockedFX VFX Graph effect from the pool
        _projectile.currentLockedFX = ProjectileManager.Instance.GetLockedFXFromPool();
        if (_projectile.currentLockedFX != null)
        {
            _projectile.currentLockedFX.transform.SetParent(_projectile.transform);
            _projectile.currentLockedFX.transform.localPosition = Vector3.zero;
            _projectile.currentLockedFX.transform.localRotation = Quaternion.identity;
            _projectile.currentLockedFX.gameObject.SetActive(true);
            _projectile.currentLockedFX.Play();
        }
        else
        {
            Debug.LogError("LockedFX VisualEffect could not be obtained from the pool.");
        }
    }

    public override void OnStateExit()
    {
        base.OnStateExit();

        if (_projectile.modelRenderer != null && _projectile.myMaterial != null)
        {
            _projectile.modelRenderer.material = _projectile.myMaterial;
        }
        else
        {
            Debug.LogError("ModelRenderer or myMaterial is not set on the projectile.");
        }

        // Return the LockedFX to the pool
        if (_projectile.currentLockedFX != null)
        {
            ProjectileManager.Instance.ReturnLockedFXToPool(_projectile.currentLockedFX);
            _projectile.currentLockedFX = null;
        }
    }

    public override void OnTriggerEnter(Collider other)
    {
        ConditionalDebug.Log("Player Locked State and hit something");
    }

    public override void Update()
    {
        Debug.Log("Player Locked State Update is happening");
    }

    public void LaunchBack()
    {
        if (ProjectileStateBased.shootingObject != null)
        {
            if (_projectile.rb != null)
            {
                _projectile.rb.isKinematic = false;
            }
            Vector3 launchDirection = ProjectileStateBased.shootingObject.transform.forward;

            _projectile.transform.parent = null;

            _projectile.homing = false;

            _projectile.transform.rotation = Quaternion.LookRotation(launchDirection);

            // Set the velocity directly towards the launch direction
            _projectile.rb.velocity = launchDirection * _projectile.bulletSpeed;

            // Adjust the trail renderer duration
            if (_projectile.tRn != null)
            {
                _projectile.tRn.DOTime(5, 2);
            }

            PlayerShotState newState = new PlayerShotState(_projectile, 1f);
            _projectile.ChangeState(newState);
        }
        else
        {
            ConditionalDebug.LogError("Shooting object is not assigned.");
        }
    }

    public void LaunchAtEnemy(Transform target)
    {
        if (_projectile.rb != null)
        {
            _projectile.rb.isKinematic = false;
        }
        _projectile.transform.parent = null;
        _projectile.currentTarget = target;
        _projectile.transform.LookAt(target);

        ConditionalDebug.Log("Launch at enemy successfully called on " + target);

        _projectile.homing = true;

        PlayerShotState newState = new PlayerShotState(_projectile, 1f);
        _projectile.ChangeState(newState);
    }
}

public class PlayerShotState : ProjectileState
{
    private const float CLOSE_PROXIMITY_THRESHOLD = 2f;
    private const float MAX_PREDICTION_TIME = 2f;
    private const float MIN_SPEED_MULTIPLIER = 1.2f;
    private const float PARRIED_SPEED_MULTIPLIER = 1.5f;

    public PlayerShotState(ProjectileStateBased projectile, float playerAccuracyModifier = 1f)
        : base(projectile)
    {
        projectile.bulletSpeed *= MIN_SPEED_MULTIPLIER;

        if (projectile.isParried)
        {
            projectile.bulletSpeed *= PARRIED_SPEED_MULTIPLIER;
        }
        _projectile.SetLifetime(10f);
        _projectile.isPlayerShot = true;
        _projectile.initialPosition = _projectile.transform.position;
        _projectile.initialDirection = _projectile.transform.forward;
        _projectile.homing = true;
        float finalAccuracy =
            ProjectileManager.Instance.projectileAccuracy * playerAccuracyModifier;
        _projectile.SetAccuracy(finalAccuracy);
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
    }

    public override void CustomUpdate(float timeScale)
    {
        base.CustomUpdate(timeScale);

        if (_projectile.currentTarget != null)
        {
            UpdateProjectileMovement(timeScale);
        }
        else
        {
            FindNewTarget();
        }

        CheckCollision();
    }

    private void UpdateProjectileMovement(float timeScale)
    {
        Vector3 targetPosition = _projectile.currentTarget.position;
        Vector3 toTarget = targetPosition - _projectile.transform.position;
        float distanceToTarget = toTarget.magnitude;

        // Predict target's future position
        Vector3 targetVelocity = ProjectileManager.Instance.CalculateTargetVelocity(
            _projectile.currentTarget.gameObject
        );
        float predictionTime = Mathf.Min(
            distanceToTarget / _projectile.bulletSpeed,
            MAX_PREDICTION_TIME
        );
        Vector3 predictedPosition = targetPosition + targetVelocity * predictionTime;

        Vector3 directionToTarget = (predictedPosition - _projectile.transform.position).normalized;

        // Apply accuracy
        float maxDeviationAngle = Mathf.Lerp(5f, 0f, _projectile.accuracy); // Max 5 degree deviation at 0 accuracy
        Vector3 randomDeviation = UnityEngine.Random.insideUnitSphere * maxDeviationAngle;
        directionToTarget = Quaternion.Euler(randomDeviation) * directionToTarget;

        // Adjust rotation towards the target
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        _projectile.transform.rotation = Quaternion.RotateTowards(
            _projectile.transform.rotation,
            targetRotation,
            _projectile.turnRate * timeScale * Time.deltaTime
        );

        // Move the projectile
        _projectile.rb.velocity = _projectile.transform.forward * _projectile.bulletSpeed;

        // If very close to the target, ensure a hit
        if (distanceToTarget <= CLOSE_PROXIMITY_THRESHOLD)
        {
            EnsureHit();
        }
    }

    private void FindNewTarget()
    {
        Transform nearestEnemy = ProjectileManager.Instance.FindNearestEnemy(
            _projectile.transform.position
        );
        if (nearestEnemy != null)
        {
            _projectile.currentTarget = nearestEnemy;
        }
        else
        {
            // If no target found, maintain current direction
            _projectile.rb.velocity = _projectile.transform.forward * _projectile.bulletSpeed;
        }
    }

    private void EnsureHit()
    {
        if (_projectile.currentTarget != null)
        {
            _projectile.transform.position = _projectile.currentTarget.position;
            _projectile.rb.velocity = Vector3.zero;

            // Trigger hit detection
            Collider targetCollider = _projectile.currentTarget.GetComponent<Collider>();
            if (targetCollider != null)
            {
                OnTriggerEnter(targetCollider);
            }
        }
    }

    private void CheckCollision()
    {
        float radius = 0.1f; // Adjust based on projectile size
        RaycastHit hit;
        if (
            Physics.SphereCast(
                _projectile.transform.position,
                radius,
                _projectile.transform.forward,
                out hit,
                _projectile.bulletSpeed * Time.deltaTime
            )
        )
        {
            OnTriggerEnter(hit.collider);
        }
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            IDamageable damageable = other.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.Damage(_projectile.damageAmount);
                _projectile.hasHitTarget = true;
                ConditionalDebug.Log(
                    $"Player projectile hit enemy: {other.gameObject.name}. Distance: {_projectile.distanceTraveled}, Time: {_projectile.timeAlive}"
                );
                _projectile.Death();
                ProjectileManager.Instance.NotifyEnemyHit(other.gameObject, _projectile);
                Crosshair.Instance.RemoveLockedEnemy(other.transform);
            }
        }
        else
        {
            ConditionalDebug.Log(
                $"Player projectile hit non-enemy object: {other.gameObject.name}. Distance: {_projectile.distanceTraveled}, Time: {_projectile.timeAlive}"
            );
        }
        GameManager.instance.LogProjectileHit(
            _projectile.isPlayerShot,
            other.gameObject.CompareTag("Enemy"),
            other.gameObject.tag
        );
    }
}

public class ProjectileStateBased : MonoBehaviour
{
    public bool targetLocking;
    public ProjectilePoolType poolType;
    public bool isParried = false;

    #region Constants
    private const string LaunchableBulletTag = "LaunchableBullet";
    private const string LaunchableBulletLockedTag = "LaunchableBulletLocked";
    private const string FIRST_TIME_ENABLED_KEY = "FIRST_TIME_ENABLED_KEY";
    #endregion

    #region Component References
    [HideInInspector]
    public Rigidbody rb;

    [HideInInspector]
    public TrailRenderer tRn;

    [HideInInspector]
    public LineRenderer lRn;

    [HideInInspector]
    public Timeline TLine;
    #endregion

    #region State Variables
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
    #endregion

    #region Movement Parameters
    [Header("Movement")]
    public float bulletSpeed;
    public float bulletSpeedMultiplier = 4f;
    public float _rotateSpeed = 95;
    public float slowSpeed;
    public float turnRate = 360f; // Degrees per second
    public float maxRotateSpeed = 180;
    public float maxDistance = 100f;

    [HideInInspector]
    public float initialSpeed;
    #endregion

    #region Targeting and Prediction
    [Header("Targeting and Prediction")]
    public Transform currentTarget;
    public float _maxDistancePredict = 100;
    public float _minDistancePredict = 5;
    public float _maxTimePrediction = 5;
    public Vector3 _standardPrediction,
        _deviatedPrediction;
    public Vector3 predictedPosition { get; set; }
    #endregion

    #region Combat Parameters
    [Header("Combat")]
    public float damageAmount = 10f;
    public float lifetime;
    private float originalLifetime;
    #endregion

    #region Accuracy and Approach
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
    #endregion

    #region Visual Effects
    [Header("Visual Effects")]
    public Renderer modelRenderer;

    [HideInInspector]
    public Material myMaterial;
    public Color lockedProjectileColor;
    private Color originalProjectileColor;
    public TrailRenderer playerProjPath;
    public Material lockedStateMaterial;
    public VisualEffect currentLockedFX;
    #endregion

    #region State Management
    private ProjectileState currentState;
    private string _currentStateName;

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

    public void ChangeState(ProjectileState newState)
    {
        // Call OnStateExit on the current state before changing to the new state
        if (currentState != null)
        {
            currentState.OnStateExit();
        }

        currentState = newState;

        // Update the _currentStateName for debugging purposes
        _currentStateName = newState?.GetType().Name ?? "null";

        // Call OnStateEnter on the new state after setting it
        if (currentState != null)
        {
            currentState.OnStateEnter();
        }
    }
    #endregion

    private Vector3 _previousPosition;
    internal static GameObject shootingObject;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int DissolveCutoutProperty = Shader.PropertyToID(
        "_AdvancedDissolveCutoutStandardClip"
    );
    private MaterialPropertyBlock propBlock;

    private Transform cachedTransform;
    private Rigidbody cachedRigidbody;

    // Add these fields to the ProjectileStateBased class
    public bool isPlayerShot = false;
    public Vector3 initialPosition;
    public Vector3 initialDirection;
    public float distanceTraveled = 0f;
    public float timeAlive = 0f;
    public bool hasHitTarget = false;

    private void Awake()
    {
        cachedTransform = transform;
        cachedRigidbody = GetComponent<Rigidbody>();
        tRn = GetComponent<TrailRenderer>();
        TLine = GetComponent<Timeline>();

        _currentTag = gameObject.tag;
        myMaterial = modelRenderer.material;
        originalProjectileColor = myMaterial.color;
        initialSpeed = bulletSpeed; // Store the initial speed

        // Cache the "Shooting" GameObject reference
        if (shootingObject == null)
        {
            shootingObject = GameObject.FindGameObjectWithTag("Shooting");
        }

        // Initialize the MaterialPropertyBlock
        propBlock = new MaterialPropertyBlock();
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
            ProjectileManager.Instance.ReturnLockedFXToPool(currentLockedFX);
            currentLockedFX = null;
        }
    }

    private void OnDisable()
    {
        if (ProjectileManager.Instance != null)
        {
            ProjectileManager.Instance.UnregisterProjectile(this);
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
    }

    public void CustomUpdate(float timeScale)
    {
        if (currentState != null)
        {
            currentState.CustomUpdate(timeScale);
        }

        if (homing && currentTarget != null)
        {
            Vector3 directionToTarget = (predictedPosition - cachedTransform.position).normalized;

            // Apply accuracy
            if (accuracy < 1f)
            {
                float maxDeviationAngle = Mathf.Lerp(30f, 0f, accuracy); // Max 30 degree deviation at 0 accuracy
                Vector3 randomDeviation = UnityEngine.Random.insideUnitSphere * maxDeviationAngle;
                directionToTarget = Quaternion.Euler(randomDeviation) * directionToTarget;
            }

            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            cachedTransform.rotation = Quaternion.RotateTowards(
                cachedTransform.rotation,
                targetRotation,
                turnRate * timeScale * Time.deltaTime
            );

            if (cachedRigidbody != null && !cachedRigidbody.isKinematic)
            {
                cachedRigidbody.velocity = cachedTransform.forward * bulletSpeed;
            }
        }

        // Update lifetime
        lifetime -= Time.deltaTime * timeScale;
        if (lifetime <= 0)
        {
            Death();
        }

        UpdateTrackingInfo();

        ConditionalDebug.Log(
            $"Projectile ID: {GetInstanceID()}, Position: {transform.position}, Velocity: {rb.velocity.magnitude}, Target: {(currentTarget != null ? currentTarget.name : "None")}, Lifetime: {lifetime}"
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentState != null)
        {
            currentState.OnTriggerEnter(other);
        }
    }

    public void Death()
    {
        // Log the debug message with lifetime and hit status
        ConditionalDebug.Log(
            $"Death called on projectile. Lifetime remaining: {lifetime}. Hit Player: {projHitPlayer}"
        );

        if (isPlayerShot && !hasHitTarget)
        {
            GameManager.instance.LogProjectileExpired(isPlayerShot);
        }

        // Ensure the projectile is unregistered and returned to the pool
        ProjectileManager.Instance.UnregisterProjectile(this);
        ProjectileManager.Instance.ReturnProjectileToPool(this);

        // Stop any ongoing movement or rotation
        if (cachedRigidbody != null)
        {
            cachedRigidbody.velocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
            cachedRigidbody.isKinematic = true;
        }

        cachedTransform.DOKill();

        // Disable any scripts that might be moving the projectile
        this.enabled = false;

        ProjectileManager.Instance.PlayDeathEffect(cachedTransform.position);

        if (playerProjPath != null)
        {
            playerProjPath.enabled = false;
        }

        // Perform dissolve effect
        if (myMaterial != null && myMaterial.HasProperty("_AdvancedDissolveCutoutStandardClip"))
        {
            myMaterial
                .DOFloat(0.05f, "_AdvancedDissolveCutoutStandardClip", 0.1f)
                .OnComplete(() => {
                    gameObject.SetActive(false);
                });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public Vector3 CalculateTargetVelocity(GameObject target)
    {
        Vector3 currentPos = target.transform.position;
        Vector3 velocity = (currentPos - _previousPosition) / Time.deltaTime;

        // Check for NaN values in the calculated velocity
        if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z))
        {
            ConditionalDebug.LogError("Calculated target velocity contains NaN values.");
            velocity = Vector3.zero; // Set velocity to zero to prevent further propagation of NaN values
        }

        _previousPosition = currentPos;
        return velocity;
    }

    public void SetLifetime(float seconds)
    {
        lifetime = seconds;
        originalLifetime = seconds;
    }

    public void ResetLifetime()
    {
        lifetime = originalLifetime;
    }

    public void SetHomingTarget(Transform target)
    {
        currentTarget = target;
        EnableHoming(true);
    }

    // Enable or disable homing
    public void EnableHoming(bool enable)
    {
        homing = enable;
    }

    // Set the homing target and enable homing
    public float maxTurnRate = 360f; // Maximum turn rate in degrees per second

    void FixedUpdate()
    {
        float timeScale = TLine.timeScale;
        float deltaTime = Time.fixedDeltaTime * timeScale;

        if (currentState != null)
        {
            currentState.FixedUpdate(timeScale);

            if (
                homing
                && currentTarget != null
                && cachedRigidbody != null
                && !cachedRigidbody.isKinematic
            )
            {
                Vector3 targetPosition = ApplyInaccuracy(currentTarget.position);
                Vector3 directionToTarget = (targetPosition - cachedTransform.position).normalized;

                cachedTransform.rotation = Quaternion.RotateTowards(
                    cachedTransform.rotation,
                    Quaternion.LookRotation(directionToTarget),
                    turnRate * deltaTime
                );

                cachedRigidbody.velocity = cachedTransform.forward * bulletSpeed;
            }
        }

        lifetime -= deltaTime;
        if (lifetime <= 0)
        {
            Death();
        }
    }

    private Vector3 ApplyInaccuracy(Vector3 targetPosition)
    {
        if (accuracy >= 1f)
            return targetPosition; // Perfect accuracy, no deviation

        float inaccuracyRadius = Mathf.Lerp(maxInaccuracyRadius, minInaccuracyRadius, accuracy);
        Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * inaccuracyRadius;
        return targetPosition + randomOffset;
    }

    public void SetClock(string clockKey)
    {
        //Not affecting local clock but this doesnt seem to matter?
        //May not need all components on here like i think

        if (TLine != null)
        {
            TLine.globalClockKey = clockKey;
            Debug.Log($"SetClock: Timeline's globalClockKey set to '{clockKey}'.");
        }
        else
        {
            Debug.LogWarning("SetClock: Timeline component not found on this projectile.");
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

    public void OnPlayerRicochetDodge()
    {
        // Check if the current state is EnemyShotState before applying changes
        if (currentState is EnemyShotState)
        {
            // Remove the current target
            currentTarget = null;

            // Disable homing to stop following any target
            homing = false;

            // Calculate the opposite direction from the current forward direction
            Vector3 oppositeDirection = -cachedTransform.forward;

            // Apply velocity in the opposite direction with the current speed
            cachedRigidbody.velocity = oppositeDirection * bulletSpeed;

            // Optionally, you can log this action for debugging
            Debug.Log("Projectile is now moving in the opposite direction due to Ricochet Dodge.");
        }
    }

    public void SetAccuracy(float newAccuracy)
    {
        accuracy = Mathf.Clamp01(newAccuracy);
    }

    public void UpdateTrackingInfo()
    {
        if (isPlayerShot && !hasHitTarget)
        {
            distanceTraveled += Vector3.Distance(cachedTransform.position, initialPosition);
            initialPosition = cachedTransform.position;
            timeAlive += Time.deltaTime;
        }
    }

    public void ResetForPool()
    {
        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = false;
            cachedRigidbody.velocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
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

        ChangeState(new EnemyShotState(this));

        if (currentLockedFX != null)
        {
            ProjectileManager.Instance.ReturnLockedFXToPool(currentLockedFX);
            currentLockedFX = null;
        }

        if (playerProjPath != null)
        {
            playerProjPath.enabled = false;
        }

        if (modelRenderer != null && myMaterial != null)
        {
            modelRenderer.material = myMaterial;
            myMaterial.SetColor("_BaseColor", originalProjectileColor);
        }

        accuracy = 1f;
    }
}
