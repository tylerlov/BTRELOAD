using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SonicBloom.Koreo;
using Chronos;
using DG.Tweening;
using HohStudios.Tools.ObjectParticleSpawner;
using BehaviorDesigner.Runtime.Tactical;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject;
using SensorToolkit.Example;
using BehaviorDesigner.Runtime.Tasks.Movement;
using UnityEngine.VFX; // Add this line to use Visual Effect Graph

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

    public virtual void FixedUpdate() {}
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
                _projectile.projHitPlayer = true; // Updated to use the renamed variable
                _projectile.Death();
                // Play the sound only when the projectile hits a player
                FMODUnity.RuntimeManager.PlayOneShot("event:/Projectile/Basic/Impact");
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

        GameObject shootingObject = GameObject.FindGameObjectWithTag("Shooting");

        if (shootingObject != null)
        {
           // Set the projectile's parent to the Shooting GameObject
            _projectile.transform.SetParent(shootingObject.transform, true);

            // Calculate the new position in front of the shootingObject by 2 units
            Vector3 newPosition = shootingObject.transform.position + shootingObject.transform.forward * 2f;

            // Since the projectile is a child of the shootingObject, set its local position accordingly
            _projectile.transform.position = newPosition;
            
            // Optionally, if you want the projectile to inherit the parent's orientation but offset in position:
            _projectile.transform.rotation = shootingObject.transform.rotation;

            // Freeze Rigidbody constraints
            _projectile.rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        else
        {
            ConditionalDebug.LogError("SHooting not found. Make sure there is a GameObject tagged 'Shooting' in the scene.");
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
        GameObject shootingObject = GameObject.FindGameObjectWithTag("Shooting");

        if (shootingObject != null)
        {
            _projectile.rb.constraints = RigidbodyConstraints.None;
            // Assuming the shootingObject's forward is the opposite direction of launch
            Vector3 launchDirection = shootingObject.transform.forward;

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
    private FMOD.Studio.EventInstance instance;

    private bool disableOnProjMove;

    [EventID]
    public string eventID;

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

    #endregion

    private ProjectileState currentState;
    internal bool shotAtEnemy;
    private Vector3 _previousPosition;
    private Coroutine lifetimeCoroutine;
    [HideInInspector] public float initialSpeed; // Add this line to store the initial speed
    internal bool projHitPlayer = false; // Renamed variable to track if the projectile has hit a player
    [HideInInspector] public bool lifetimeExtended = false; // Renamed and used to track if the lifetime extension has occurred

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

    // Stop the lifetime coroutine if it's running.
    ProjectileManager.Instance.PlayDeathEffect(this.transform.position);

    if (playerProjPath != null)
    {
        playerProjPath.enabled = false;
    }

    // Check if lifetime is less than zero to decide the course of action
    if (lifetime < 0)
    {
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }
        // Proceed with DOTween animation if lifetime has expired
        if (myMaterial != null && myMaterial.HasProperty("_AdvancedDissolveCutoutStandardClip"))
        {
            myMaterial.DOFloat(0.05f, "_AdvancedDissolveCutoutStandardClip", 1f).OnComplete(() =>
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
    else
    {
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }

        // If lifetime is not less than zero, skip the DOTween and immediately return to pool
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

   /* void OnMusicalProjMove(KoreographyEvent evt)
    {
        if (Time.timeScale != 0f & movementParticles != null & disableOnProjMove == false)
        {
            movementParticles.GetComponent<ParticleSystem>().Play();

            disableOnProjMove = true;
        }
    }*/


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
    if (lifetimeCoroutine != null)
    {
        StopCoroutine(lifetimeCoroutine); // Stop the existing coroutine if it's running.
    }
    // Check if the GameObject is active in the hierarchy before starting the coroutine
    if (gameObject.activeInHierarchy)
    {
        lifetimeCoroutine = StartCoroutine(LifetimeCoroutine()); // Start a new coroutine.
    }
    else
    {
        // Optionally, handle the case when the GameObject is not active
        Debug.LogWarning("GameObject is inactive. Coroutine not started.");
    }
}

private IEnumerator LifetimeCoroutine()
{
   bool dissolveTriggered = false;

    while (lifetime > 0)
    {
        yield return null; // Wait for the next frame.

        if (currentState is PlayerLockedState)
        {
            yield break; // Exit the coroutine.
        }

        lifetime -= clock.deltaTime;

        // Check if lifetime is close to 1 second and the dissolve effect hasn't been triggered yet.
        if (lifetime <= 1f && !dissolveTriggered)
        {
            dissolveTriggered = true; // Ensure this block only runs once.
            // Trigger the dissolve effect.
            if (myMaterial != null)
            {
                myMaterial.DOFloat(0.05f, "_AdvancedDissolveCutoutStandardClip", 1f).OnComplete(() =>
                {
                    Death();
                });
            }
        }
    }

    if (lifetime < 0)
    {
        ConditionalDebug.Log("Lifetime is less than 0, calling Death");
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

    void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }

        if (homing && currentTarget != null)
        {
            Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            
            // Calculate dynamic rotation speed based on distance.
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            float dynamicRotateSpeed = Mathf.Lerp(maxRotateSpeed, _rotateSpeed, distance / maxDistance);
            
            // Rotate towards the target.
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, dynamicRotateSpeed * Time.deltaTime);
        
            // Use initialSpeed for the velocity towards the target.
            rb.velocity = directionToTarget * bulletSpeed * clock.localTimeScale;

            // Check distance to the current target and pause or extend lifetime if within a certain radius
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            float pauseRadius = 25.0f; // Updated pause radius to 25 units

            if (distanceToTarget <= pauseRadius && !lifetimeExtended)
            {
                lifetimeExtended = true;
                lifetime += 5.0f; // Extend lifetime by 5 seconds
            }
        }
    }

    public void UpdateMaterial(Material newMaterial)
    {
        if (modelRenderer != null)
        {
            modelRenderer.material = newMaterial;
            myMaterial = newMaterial;

            // Initialize the dissolve effect when a new material is set
            if (modelRenderer.material.HasProperty("_AdvancedDissolveCutoutStandardClip"))
            {
                modelRenderer.material.SetFloat("_AdvancedDissolveCutoutStandardClip", 1f); // Ensure the material starts without being dissolved
            }
            else
            {
                Debug.LogWarning("The assigned material does not support the required dissolve property.");
            }
        }
        else
        {
            Debug.LogError("ModelRenderer is not set on the projectile.");
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

    void OnDrawGizmos()
    {
        if (homing && currentTarget != null)
        {
            // Draw a line from the projectile to the current target
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);

            // Assuming you store the predicted position in a variable
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, predictedPosition); // Make sure you have a way to access `predictedPosition`

            // Draw a sphere at the predicted position
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
}