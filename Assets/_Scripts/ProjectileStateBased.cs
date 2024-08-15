using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Chronos;
using DG.Tweening;
using HohStudios.Tools.ObjectParticleSpawner;
using BehaviorDesigner.Runtime.Tactical;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject;
using SensorToolkit.Example;
using BehaviorDesigner.Runtime.Tasks.Movement;
using UnityEngine.VFX;
using SickscoreGames;
using SickscoreGames.HUDNavigationSystem;
using Unity.Collections;

public enum ProjectilePoolType
{
    StaticEnemy,
    EnemyBasicSetup
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
    public virtual void Update() { 
        ProjectileManager.Instance.PredictAndRotateProjectile(_projectile);
    }
    public virtual void CustomUpdate(float timeScale) { } // Added this line

    public ProjectileStateBased GetProjectile()
    {
        return _projectile;
    }
}

public class EnemyShotState : ProjectileState
{

    public EnemyShotState(ProjectileStateBased projectile) : base(projectile)
    {
        // Enemy Shot State initialization logic here...
        _projectile.currentTarget = GameObject.FindWithTag("Player Aim Target").transform;
        _projectile.SetLifetime(10f);

        _projectile.minTurnRadius = 5f;  // Reduced from 10f
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
                ProjectileManager.Instance.PlayOneShotSound("event:/Projectile/Basic/Impact", _projectile.transform.position);
            }
        }
        else
        {
            ConditionalDebug.Log("Collided with an object " + other.gameObject + " at transform " + other.gameObject.transform + "and behaviour may not be expected");
        }
    }
}

public class PlayerLockedState : ProjectileState
{
    public PlayerLockedState(ProjectileStateBased projectile) : base(projectile)
    {
        if (_projectile.isParried) return;

        _projectile.currentTarget = null;
        _projectile.homing = false;
        _projectile.tag = "LaunchableBulletLocked";

        _projectile.TLine.ResetRecording();
        _projectile.TLine.rewindable = false;
        _projectile.TLine.globalClockKey = "Test";

        if (ProjectileStateBased.shootingObject != null)
        {
            _projectile.transform.SetParent(ProjectileStateBased.shootingObject.transform, true);
            Vector3 newPosition = ProjectileStateBased.shootingObject.transform.position + ProjectileStateBased.shootingObject.transform.forward * 2f;
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
            ConditionalDebug.LogError("Shooting not found. Make sure there is a GameObject tagged 'Shooting' in the scene.");
        }

        _projectile.playerProjPath.enabled = true;
        _projectile.lifetime = 6f;
        _projectile.isLifetimePaused = true;
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

        _projectile.isLifetimePaused = false;
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

            PlayerShotState newState = new PlayerShotState(_projectile);
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
        
        PlayerShotState newState = new PlayerShotState(_projectile);
        _projectile.ChangeState(newState);
    }
}

public class PlayerShotState : ProjectileState
{
    public PlayerShotState(ProjectileStateBased projectile) : base(projectile)
    {
        projectile.bulletSpeed *= 3; 
        
        if (projectile.isParried)
        {
            projectile.bulletSpeed *= 4;
        }
        _projectile.SetLifetime(10f);
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            IDamageable damageable;
            if ((damageable = other.gameObject.GetComponent(typeof(IDamageable)) as IDamageable) != null)
            {
                damageable.Damage(_projectile.damageAmount);
                ConditionalDebug.Log("Projectile collided with " + other.gameObject + " and they should be taking damage");
                _projectile.Death();
                // Notify ProjectileManager that an enemy has been hit
                ProjectileManager.Instance.NotifyEnemyHit(other.gameObject, _projectile);
                // Assuming Crosshair instance is accessible
                Crosshair.Instance.RemoveLockedEnemy(other.transform);
            }
        }
        // Additional conditions can be added here
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
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public TrailRenderer tRn;
    [HideInInspector] public LineRenderer lRn;
    [HideInInspector] public Timeline TLine;
    #endregion

    #region State Variables
    [Header("State")]
    public string projectileType;
    [HideInInspector] public string _currentTag;
    [HideInInspector] public bool collectable;
    [HideInInspector] public bool homing;
    private bool activeLine = false;
    private bool disableOnProjMove;
    internal bool shotAtEnemy;
    internal bool projHitPlayer = false;
    [HideInInspector] public bool lifetimeExtended = false;
    #endregion

    #region Movement Parameters
    [Header("Movement")]
    public float bulletSpeed;
    public float bulletSpeedMultiplier = 4f;
    public float _rotateSpeed = 95;
    public float slowSpeed;
    public float turnRate = 5f;
    public float maxRotateSpeed = 180;
    public float maxDistance = 100f;
    [HideInInspector] public float initialSpeed;
    #endregion

    #region Targeting and Prediction
    [Header("Targeting and Prediction")]
    public Transform currentTarget;
    public float _maxDistancePredict = 100;
    public float _minDistancePredict = 5;
    public float _maxTimePrediction = 5;
    public Vector3 _standardPrediction, _deviatedPrediction;
    public Vector3 predictedPosition { get; set; }
    #endregion

    #region Combat Parameters
    [Header("Combat")]
    public float damageAmount = 10f;
    public float lifetime;
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
    [HideInInspector] public Material myMaterial;
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
    private static readonly int DissolveCutoutProperty = Shader.PropertyToID("_AdvancedDissolveCutoutStandardClip");
    private MaterialPropertyBlock propBlock;

    private Transform cachedTransform;
    private Rigidbody cachedRigidbody;

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
            myMaterial.SetColor("_BaseColor",originalProjectileColor);
        }

        ChangeState(new EnemyShotState(this));

        lifetime = 10f;
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
            float distance = Vector3.Distance(cachedTransform.position, predictedPosition);
            
            // Apply accuracy to rotation
            float accuracyAdjustedRotateSpeed = Mathf.Lerp(_rotateSpeed * 0.5f, maxRotateSpeed, accuracy);
            float dynamicRotateSpeed = Mathf.Lerp(accuracyAdjustedRotateSpeed, _rotateSpeed, distance / maxDistance);
            
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            cachedTransform.rotation = Quaternion.RotateTowards(cachedTransform.rotation, targetRotation, dynamicRotateSpeed * timeScale * Time.deltaTime);

            // Apply accuracy to velocity only if the Rigidbody is not kinematic
            if (cachedRigidbody != null && !cachedRigidbody.isKinematic)
            {
                Vector3 desiredVelocity = cachedTransform.forward * bulletSpeed;
                Vector3 velocityAdjustment = (desiredVelocity - cachedRigidbody.velocity) * accuracy;
                cachedRigidbody.velocity += velocityAdjustment * timeScale * Time.deltaTime;
                cachedRigidbody.velocity = Vector3.ClampMagnitude(cachedRigidbody.velocity, bulletSpeed);
            }
        }

        // Update lifetime
        lifetime -= Time.deltaTime * timeScale;
        if (lifetime <= 0)
        {
            Death();
        }
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
        ConditionalDebug.Log($"Death called on projectile. Lifetime remaining: {lifetime}. Hit Player: {projHitPlayer}");

        // Check if the Rigidbody is not null and not kinematic before trying to set velocities
        if (cachedRigidbody != null && !cachedRigidbody.isKinematic)
        {
            cachedRigidbody.velocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
        }

        // Set the Rigidbody to kinematic to prevent further physics interactions
        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = true;
        }

        // Stop any ongoing movement or rotation
        cachedTransform.DOKill(); // This stops any DOTween animations on the transform

        // Disable any scripts that might be moving the projectile
        this.enabled = false;

        // Stop the lifetime coroutine if it's running.
        ProjectileManager.Instance.PlayDeathEffect(cachedTransform.position);

        if (playerProjPath != null)
        {
            playerProjPath.enabled = false;
        }

        // Proceed with DOTween animation if lifetime has expired
        if (myMaterial != null && myMaterial.HasProperty("_AdvancedDissolveCutoutStandardClip"))
        {
            // Changed the duration from 1f to 0.1f for a much quicker dissolve effect
            myMaterial.DOFloat(0.05f, "_AdvancedDissolveCutoutStandardClip", 0.1f).OnComplete(() =>
            {
                // This code will execute after the DOTween animation completes.
                // Now it's safe to recycle the projectile.
                ProjectileManager.Instance.ReturnProjectileToPool(this);
            });
        }
        else
        {
            // Ensure the projectile is returned to the pool even if the material does not have the required property.
            ProjectileManager.Instance.ReturnProjectileToPool(this);
        }

        // Ensure the projectile is unregistered
        ProjectileManager.Instance.UnregisterProjectile(this);
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
        lifetime = seconds; // Set the lifetime variable to the specified seconds.
    }

    public void UpdateLifetime(float deltaTime)
    {
        lifetime -= deltaTime;
        if (lifetime <= 0)
        {
            Death();
        }
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

    public bool isLifetimePaused = false;

    void FixedUpdate()
    {
        float timeScale = TLine.timeScale;
        float deltaTime = Time.fixedDeltaTime * timeScale;

        if (currentState != null)
        {
            currentState.FixedUpdate(timeScale);

            if (homing && currentTarget != null && cachedRigidbody != null && !cachedRigidbody.isKinematic)
            {
                Vector3 targetPosition = ApplyInaccuracy(currentTarget.position);
                Vector3 directionToTarget = (targetPosition - cachedTransform.position).normalized;

                cachedTransform.rotation = Quaternion.RotateTowards(cachedTransform.rotation, 
                    Quaternion.LookRotation(directionToTarget), turnRate * deltaTime);

                cachedRigidbody.velocity = cachedTransform.forward * bulletSpeed;
            }
        }

        if (!isLifetimePaused)
        {
            lifetime -= deltaTime;
            if (lifetime <= 0)
            {
                Death();
            }
        }
    }

    private Vector3 ApplyInaccuracy(Vector3 targetPosition)
    {
        if (accuracy >= 1f) return targetPosition; // Perfect accuracy, no deviation

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