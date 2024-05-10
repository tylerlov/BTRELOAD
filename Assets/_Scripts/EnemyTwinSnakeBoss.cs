using System.Collections;
using UnityEngine;
using FMODUnity;
using Chronos;
using BehaviorDesigner.Runtime.Tactical;
using UnityEngine.Events;
using SonicBloom.Koreo;

[RequireComponent(typeof(Timeline))]
public class EnemyTwinSnakeBoss : MonoBehaviour, ILimbDamageReceiver
{
    // Basic Enemy Information
    [Header("Basic Enemy Information")]
    [SerializeField] private string enemyType;
    [SerializeField] private float startHealth = 100;
    [SerializeField] private Animator animator;

    // Twin Snake Logic
    [Header("Twin Snake Logic")]
    public EnemyTwinSnakeBoss otherSnake; // Reference to the other part of the twin snake

    // Shooting Functionality
    [Header("Shooting Functionality")]
    [SerializeField] private Transform projectileOrigin;
    [SerializeField] private float shootSpeed = 20f;
    [SerializeField] private float projectileLifetime = 5f;
    [SerializeField] private float projectileScale = 1f;
    [SerializeField] private Material alternativeProjectileMaterial;
    [SerializeField] private string projectileTimelineName; // Added field for specifying projectile timeline
    [SerializeField] private float noKoreoShootInterval = 2f; // Time in seconds between each shot, settable in the Inspector
    public float shootDelay = 0.5f; // Time in seconds to delay the shooting action

    // Animation Triggers
    [Header("Animation Triggers")]
    [SerializeField] private string attackAnimationTrigger = "Attack"; // Animation trigger for attacks

    // Koreographer Shooting
    [Header("Koreographer Shooting")]
    [SerializeField]
    private string[] shootEventIDs = new string[1]; // Array to hold multiple shootEventIDs

    [EventID]
    public string activeShootEventID; // Currently active Koreographer event ID for shooting

    // Private fields (not shown in Inspector)
    private float currentHealth;
    private Timeline myTime;
    private Clock clock;

    public Timeline MyTime
    {
        get { return myTime; }
        set { myTime = value; } // Added setter to allow modification
    }

    public string ClockName { get; private set; }

    public float ShootInterval
    {
        get { return noKoreoShootInterval; }
    }

    // Removed Events section including onDamageTaken

    private void Awake()
    {
        // Removed onDamageTaken initialization
        // Initialize the active shootEventID with the first one from the array
        if (shootEventIDs.Length > 0)
        {
            activeShootEventID = shootEventIDs[0];
        }
    }

    private void OnEnable()
    {
        InitializeEnemy();
        // Register for events using the active shootEventID
        if (!string.IsNullOrEmpty(activeShootEventID))
        {
            Koreographer.Instance.RegisterForEvents(activeShootEventID, OnMusicalShoot);
        }
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(activeShootEventID))
        {
            Koreographer.Instance.UnregisterForEvents(activeShootEventID, OnMusicalShoot);
        }
    }

    private void Start()
    {
        SetupEnemy();
    }

    public bool HasShootEventID()
    {
        return !string.IsNullOrEmpty(activeShootEventID);
    }

    public bool IsAlive() => currentHealth > 0;

    public void Damage(float amount)
    {
        HandleDamage(amount);
    }

    private void InitializeEnemy()
    {
        currentHealth = startHealth;
        // Assign the clock based on some logic or directly. This is just an example.
        ClockName = gameObject.name.Contains("Snake1") ? "Boss Time 1" : "Boss Time 2";
        clock = Timekeeper.instance.Clock(ClockName);
    }

    private void SetupEnemy()
    {
        myTime = GetComponent<Timeline>();
    }

    private void HandleDamage(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);

        // Removed onDamageTaken.Invoke();

        if (currentHealth <= 0)
        {
            StartCoroutine(Death());
        }
    }

    public void DamageFromLimb(string limbName, float amount)
    {
         Debug.Log($"Damaged limb: {limbName}");
        HandleDamage(amount);
    }

    public void ShootProjectile()
    {
        if (string.IsNullOrEmpty(activeShootEventID)) // No Koreographer event assigned
        {
            StartCoroutine(DelayedShoot());
        }
        else
        {
              
        }
    }

    private IEnumerator DelayedShoot()
    {
        // Trigger the attack animation immediately, without waiting
        animator.SetTrigger(attackAnimationTrigger);

        // Now wait for the shootDelay before continuing with shooting the projectile
        yield return new WaitForSeconds(shootDelay);

        if (ProjectileManager.Instance == null)
        {
            Debug.LogError("ProjectileManager instance not found.");
            yield break; // Correct way to exit a coroutine early
        }

        // Assuming the target is the player or another enemy. Adjust as necessary.
        Vector3 targetPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
        Quaternion rotationTowardsTarget = Quaternion.LookRotation(targetPosition - projectileOrigin.position); // Use projectileOrigin.position

        // Call ProjectileManager to shoot a projectile, now also passing the projectileTimelineName
        // This call is now made after the delay, ensuring only the shooting is delayed
        ProjectileManager.Instance.ShootProjectileFromEnemy(
            projectileOrigin.position,
            rotationTowardsTarget,
            shootSpeed,
            projectileLifetime,
            projectileScale,
            enableHoming: true,
            alternativeProjectileMaterial,
            projectileTimelineName // Pass the specified timeline name for the projectile
        );

    }

    private void OnMusicalShoot(KoreographyEvent evt)
    {
         StartCoroutine(DelayedShoot());
    }

    private IEnumerator Death()
    {
        animator.SetTrigger("Die");

        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Death"));
        float animationLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animationLength);

        FMODUnity.RuntimeManager.PlayOneShot("event:/Enemy/" + enemyType + "/Death", transform.position);

        // Additional logic for twin snake boss death
        if (!otherSnake.IsAlive())
        {
            // Both snakes are dead
            Debug.Log("Both Twin Snakes are defeated!");
            // Call to GameManager or any other logic to handle the defeat of both snakes
        }
    }

    // Method to update the active shootEventID based on your logic
    public void UpdateActiveShootEventID(int index)
    {
        if (index >= 0 && index < shootEventIDs.Length)
        {
            // Unregister the current event ID
            if (!string.IsNullOrEmpty(activeShootEventID))
            {
                Koreographer.Instance.UnregisterForEvents(activeShootEventID, OnMusicalShoot);
            }

            // Update the active shootEventID
            activeShootEventID = shootEventIDs[index];

            // Register the new event ID
            if (!string.IsNullOrEmpty(activeShootEventID))
            {
                Koreographer.Instance.RegisterForEvents(activeShootEventID, OnMusicalShoot);
            }
        }
        else
        {
            Debug.LogError("Invalid shootEventID index.");
        }
    }
}