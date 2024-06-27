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
        if (_projectile.isParried) return; // Skip if parried

        _projectile.currentTarget = null;

        _projectile.homing = false;
        _projectile.tag = "LaunchableBulletLocked";

        _projectile.TLine.ResetRecording();
        _projectile.TLine.rewindable = false;

        _projectile.TLine.globalClockKey = "Test";

        if (ProjectileStateBased.shootingObject != null)
        {
           // Set the projectile's parent to the Shooting GameObject
            _projectile.transform.SetParent(ProjectileStateBased.shootingObject.transform, true);

            // Calculate the new position in front of the shootingObject by 2 units
            Vector3 newPosition = ProjectileStateBased.shootingObject.transform.position + ProjectileStateBased.shootingObject.transform.forward * 2f;

            // Since the projectile is a child of the shootingObject, set its local position accordingly
            _projectile.transform.position = newPosition;
            
            // Optionally, if you want the projectile to inherit the parent's orientation but offset in position:
            _projectile.transform.rotation = ProjectileStateBased.shootingObject.transform.rotation;

            // Freeze Rigidbody constraints
            _projectile.rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        else
        {
            ConditionalDebug.LogError("Shooting not found. Make sure there is a GameObject tagged 'Shooting' in the scene.");
        }

        _projectile.playerProjPath.enabled = true;

        // Add extra lifetime for player shots
        _projectile.lifetime = 6f; 

        //where is the projectile being locked?? in crosshair - should it be here???
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

        // Play the LockedFX VFX Graph effect
        if (_projectile.LockedFX != null)
        {
            _projectile.LockedFX.Play(); // For VFX Graph, ensure your effect is set to play on Awake or manually trigger it here
        }
        else
        {
            Debug.LogError("LockedFX VisualEffect is not assigned in ProjectileStateBased.");
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
            _projectile.rb.constraints = RigidbodyConstraints.None;
            // Assuming the shootingObject's forward is the opposite direction of launch
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
        _projectile.rb.constraints = RigidbodyConstraints.None;
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

public class ProjectileStateBased : BaseBehaviour
{
    public bool targetLocking;
    public ProjectilePoolType poolType;
    public bool isParried = false; // Add this line

    #region Constants

    private const string LaunchableBulletTag = "LaunchableBullet";
    private const string LaunchableBulletLockedTag = "LaunchableBulletLocked";
    #endregion

    #region Fields
    private string _currentStateName;
    public string projectileType;
    private bool disableOnProjMove;

    [Space]

    [HideInInspector]public Rigidbody rb;
    [HideInInspector] public Clock clock;
    [HideInInspector] public string _currentTag;

    [Header("Projectile State")]
    public float turnRate = 5f; 
    public float damageAmount = 10f;
    public float lifetime;
    public Transform currentTarget;

    [Space]
    [Header("Projectile Debug Variable States")]
    [HideInInspector]public bool collectable;
    [HideInInspector]public bool homing;
    private bool activeLine = false;
    [Space]

    [HideInInspector] public TrailRenderer tRn;
    [HideInInspector] public LineRenderer lRn;
    [HideInInspector] public Timeline TLine;

    public float maxRotateSpeed = 180; // Maximum rotation speed in degrees per second.
    public float maxDistance = 100f; // Added for dynamic rotation speed adjustment

    [Header("Movement")]
    public float bulletSpeed;
    public float bulletSpeedMultiplier = 4f;
    public float _rotateSpeed = 95;
    public float slowSpeed;

    [Header("Prediction")]
    public float _maxDistancePredict = 100;
    public float _minDistancePredict = 5;
    public float _maxTimePrediction = 5;
    public Vector3 _standardPrediction, _deviatedPrediction;
    public Vector3 predictedPosition { get; set; }

    [Header("Deviation")]
    public float _deviationAmount = 5;
    public float _deviationSpeed = 2;
    public float _deviationThreshold = 50;

    [Header("Effects")]
    public Renderer modelRenderer;
    //[SerializeField] private GameObject movementParticles;
    //[SerializeField] private ParticleSystem deathParticles;
    [HideInInspector] public Material myMaterial;
    public Color lockedProjectileColor;
    private Color originalProjectileColor;
    public TrailRenderer playerProjPath;
    public Material lockedStateMaterial; // Assign this in the Unity Inspector
    public VisualEffect LockedFX; // Changed to use VisualEffect for the locked state particle effect

    private const string FIRST_TIME_ENABLED_KEY = "FIRST_TIME_ENABLED_KEY";

    [Range(0f, 1f)]
    public float accuracy = 0.5f; // Default to 0.5 if not set

    private Vector3 inaccuracyOffset;
    private float inaccuracyUpdateInterval = 0.5f;
    private float inaccuracyTimer = 0f;
    public float maxInaccuracyRadius = 5f;
    public float minInaccuracyRadius = 0.1f;

    public float minTurnRadius = 10f; // Minimum turn radius
    public float maxTurnRadius = 50f; // Maximum turn radius
    public float approachAngle = 45f; // Angle of approach in degrees

    public float closeProximityThreshold = 2f; // Distance at which to start smooth approach
    public float smoothApproachFactor = 0.1f; // Factor for smooth approach (0-1)

    public float minDistanceToTarget = 0.1f; // Minimum distance to consider the target reached
    public float maxRotationSpeed = 360f; // Maximum rotation speed in degrees per second

    #endregion

    private ProjectileState currentState;
    internal bool shotAtEnemy;
    private Vector3 _previousPosition;
    [HideInInspector] public float initialSpeed; // Add this line to store the initial speed
    internal bool projHitPlayer = false; // Renamed variable to track if the projectile has hit a player
    [HideInInspector] public bool lifetimeExtended = false; // Renamed and used to track if the lifetime extension has occurred

    // Add this field to the ProjectileStateBased class
    internal static GameObject shootingObject;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int DissolveCutoutProperty = Shader.PropertyToID("_AdvancedDissolveCutoutStandardClip");
    private MaterialPropertyBlock propBlock;

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

    private void Awake()
    {
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
        if (modelRenderer != null && modelRenderer.material != null && modelRenderer.material.HasProperty("_AdvancedDissolveCutoutStandardClip"))
        {
            modelRenderer.material.DOFloat(0.05f, "_AdvancedDissolveCutoutStandardClip", 1f);
        }

        // Reset the lifetimeExtended variable each time the projectile is enabled
        lifetimeExtended = false;

        // Reset the projHitPlayer variable each time the projectile is enabled
        projHitPlayer = false;

        // Animate the clip value from 1 to 0 when the projectile is reused.
        if (modelRenderer != null && modelRenderer.material != null)
        {
            modelRenderer.material.DOFloat(0.05f, "_AdvancedDissolveCutoutStandardClip", 1f);
        }
        ProjectileManager.Instance.RegisterProjectile(this);

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

       // Koreographer.Instance.RegisterForEvents(eventID, OnMusicalProjMove);

        _currentTag = gameObject.tag;
    }

    private void OnDisable()
    {
        ProjectileManager.Instance.UnregisterProjectile(this);
    }

    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        TLine = gameObject.GetComponent<Timeline>();
        TLine.rewindable = true;
        tRn = GetComponent<TrailRenderer>();
        playerProjPath.enabled = false;


        clock = GetComponent<Clock>();

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
            Vector3 directionToTarget = (predictedPosition - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, predictedPosition);
            
            // Apply accuracy to rotation
            float accuracyAdjustedRotateSpeed = Mathf.Lerp(_rotateSpeed * 0.5f, maxRotateSpeed, accuracy);
            float dynamicRotateSpeed = Mathf.Lerp(accuracyAdjustedRotateSpeed, _rotateSpeed, distance / maxDistance);
            
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, dynamicRotateSpeed * timeScale * Time.deltaTime);

            // Apply accuracy to velocity
            Vector3 desiredVelocity = transform.forward * bulletSpeed;
            Vector3 velocityAdjustment = (desiredVelocity - rb.velocity) * accuracy;
            rb.velocity += velocityAdjustment * timeScale * Time.deltaTime;
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, bulletSpeed);
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

        // Freeze the projectile in place
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // This prevents any further physics interactions
        }

        // Stop any ongoing movement or rotation
        transform.DOKill(); // This stops any DOTween animations on the transform

        // Disable any scripts that might be moving the projectile
        this.enabled = false;

        // Stop the lifetime coroutine if it's running.
        ProjectileManager.Instance.PlayDeathEffect(this.transform.position);

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

    public float smoothTime = 0.1f; // Adjust this value to change smoothing amount
    private Vector3 currentVelocity;

    public float maxTurnRate = 360f; // Maximum turn rate in degrees per second
    public float minTurnRate = 45f;  // Minimum turn rate in degrees per second
    public float turnRateDistance = 10f; // Distance at which turn rate starts to decrease

    public float minApproachDistance = 1f; // Minimum distance to maintain from target

    public float Kp = 1f; // Proportional gain
    public float Ki = 0.1f; // Integral gain
    public float Kd = 0.1f; // Derivative gain
    private Vector3 previousError;
    private Vector3 integralError;

    void FixedUpdate()
    {
        if (currentState != null)
        {
            float timeScale = clock != null ? clock.localTimeScale : Time.timeScale;

            currentState.FixedUpdate(timeScale);

            if (homing && currentTarget != null)
            {
                // Calculate direction to target with inaccuracy
                Vector3 targetPosition = ApplyInaccuracy(currentTarget.position);
                Vector3 directionToTarget = (targetPosition - transform.position).normalized;

                // Rotate towards the target
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnRate * Time.fixedDeltaTime * timeScale);

                // Move forward
                rb.velocity = transform.forward * bulletSpeed;
            }
        }

        // Update lifetime
        float deltaTime = Time.fixedDeltaTime * (clock != null ? clock.localTimeScale : Time.timeScale);
        lifetime -= deltaTime;
        if (lifetime <= 0)
        {
            Death();
        }
    }

    private Vector3 ApplyInaccuracy(Vector3 targetPosition)
    {
        if (accuracy >= 1f) return targetPosition; // Perfect accuracy, no deviation

        float inaccuracyRadius = Mathf.Lerp(maxInaccuracyRadius, minInaccuracyRadius, accuracy);
        Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * inaccuracyRadius;
        return targetPosition + randomOffset;
    }

    // Add this method to update non-physics related stuff
    void Update()
    {
        UpdateLifetime(Time.deltaTime);
    }

    public void UpdateMaterial(Color color)
    {
        if (modelRenderer != null)
        {
            propBlock.SetColor(BaseColorProperty, color);
            modelRenderer.SetPropertyBlock(propBlock);
        }
    }

    public void UpdateDissolveCutout(float cutoutValue)
    {
        if (modelRenderer != null)
        {
            propBlock.SetFloat(DissolveCutoutProperty, cutoutValue);
            modelRenderer.SetPropertyBlock(propBlock);
        }
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
            Gizmos.DrawLine(transform.position, currentTarget.position);

            // Draw a line from the projectile to the predicted position
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, predictedPosition);

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
            Vector3 oppositeDirection = -transform.forward;

            // Apply velocity in the opposite direction with the current speed
            rb.velocity = oppositeDirection * bulletSpeed;

            // Optionally, you can log this action for debugging
            Debug.Log("Projectile is now moving in the opposite direction due to Ricochet Dodge.");
        }
    }

    public void ResetForPool()
    {
        // Kill all tweens associated with this projectile
        DOTween.Kill(this.transform);
        DOTween.Kill(this.gameObject);
        
        if (myMaterial != null)
        {
            DOTween.Kill(myMaterial);
            // Reset material properties
            if (myMaterial.HasProperty("_AdvancedDissolveCutoutStandardClip"))
            {
                myMaterial.SetFloat("_AdvancedDissolveCutoutStandardClip", 1f);
            }
        }

        // Reset other properties
        transform.localScale = Vector3.one;
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false; // Re-enable physics interactions
        }

        this.enabled = true; // Re-enable the script

        // Stop any particle systems
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Stop();
            ps.Clear();
        }

        // Reset any other custom properties
        homing = false;
        currentTarget = null;
        lifetimeExtended = false;
        projHitPlayer = false;
        bulletSpeed = initialSpeed;

        // Reset state
        ChangeState(new EnemyShotState(this));

        // Disable any effects
        if (LockedFX != null)
        {
            LockedFX.Stop();
        }

        if (playerProjPath != null)
        {
            playerProjPath.enabled = false;
        }

        // Reset material to original
        if (modelRenderer != null && myMaterial != null)
        {
            modelRenderer.material = myMaterial;
            myMaterial.SetColor("_BaseColor", originalProjectileColor);
        }
    }
}
