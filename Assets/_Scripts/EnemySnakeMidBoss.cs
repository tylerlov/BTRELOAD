using UnityEngine;
using System.Collections;
using FMODUnity;
using Chronos;
using BehaviorDesigner.Runtime.Tactical;
using UnityEngine.Events; // Added line

[RequireComponent(typeof(Timeline))] 
public class EnemySnakeMidBoss : BaseBehaviour, IDamageable, ILimbDamageReceiver
{
    
    // Serialized fields
    [SerializeField] private string enemyType;
    [SerializeField] private float startHealth = 100;
    [SerializeField] private float currentHealth;
    [SerializeField] private Animator animator; // Add this line
    // Fields for shooting functionality
    [SerializeField] private Transform projectileOrigin;
    [SerializeField] private float shootSpeed = 20f;
    [SerializeField] private float projectileLifetime = 5f;
    [SerializeField] private float projectileScale = 1f;
    [SerializeField] private Material alternativeProjectileMaterial;
    [SerializeField] private float shootInterval = 2f;
    [SerializeField] private string projectileTimelineName;
    private Timeline myTime;
    private Clock clock;

    // Animator parameters
    private readonly string damageAnimationTrigger = "TakeDamage";


    private void Awake()
    {

    }

    private void OnEnable()
    {
        InitializeEnemy();
    }

    private void Start()
    {
        SetupEnemy();
        StartCoroutine(TimedShooting()); // Start the timed shooting coroutine
    }

    public void Damage(float amount)
    {
        HandleDamage(amount);
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

    public void DamageFromLimb(string limbName, float amount)
    {
        Debug.Log($"Damaged limb: {limbName}");
        HandleDamage(amount);
    }

    private void InitializeEnemy()
    {
        currentHealth = startHealth;
        clock = Timekeeper.instance.Clock("Test");
    }

    private void SetupEnemy()
    {
        myTime = GetComponent<Timeline>();
    }

    private void HandleDamage(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);

        // Determine the health percentage
        float healthPercentage = (currentHealth / startHealth) * 100;

        // Check for specific health thresholds and trigger the animation
        if (healthPercentage <= 75 && healthPercentage > 50 ||
            healthPercentage <= 50 && healthPercentage > 25 ||
            healthPercentage <= 25 && healthPercentage > 0)
        {
            animator.SetTrigger("GetHitFront");
        }

        if (currentHealth == 0)
        {
            StartCoroutine(Death());
        }
    }

    private IEnumerator TimedShooting()
    {
        while (IsAlive())
        {
            ShootProjectile();
            yield return new WaitForSeconds(shootInterval);
        }
    }

    public void ShootProjectile()
    {
        if (ProjectileManager.Instance == null)
        {
            Debug.LogError("ProjectileManager instance not found.");
            return;
        }

        Vector3 targetPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
        Quaternion rotationTowardsTarget = Quaternion.LookRotation(targetPosition - projectileOrigin.position);

        ProjectileManager.Instance.ShootProjectileFromEnemy(
            projectileOrigin.position,
            rotationTowardsTarget,
            shootSpeed,
            projectileLifetime,
            projectileScale,
            enableHoming: false,
            alternativeProjectileMaterial,
            projectileTimelineName
        );
    }

    private IEnumerator Death()
    {
        animator.SetTrigger("Die"); // Trigger the death animation

        // Wait for the death animation to start
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Death"));

        // Now wait for the death animation to actually finish
        float animationLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animationLength);

        FMODUnity.RuntimeManager.PlayOneShot("event:/Enemy/" + enemyType + "/Death", transform.position);

        // Additional cleanup or state management here, if necessary

        // Call to GameManager to transition to the next scene
        GameManager.instance.ChangeSceneWithTransitionToNext();
    }
}
