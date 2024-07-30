using UnityEngine;
using System.Collections;
using FMODUnity;
using Chronos;
using BehaviorDesigner.Runtime.Tactical;
using UnityEngine.Events; 
using SonicBloom.Koreo;
using System.Collections.Generic;
using UnityEngine.VFX;
using UnityEngine.Animations;

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
    [EventID]
    [SerializeField] private string koreographyEventID; // Serialized field to set in Inspector
    [SerializeField] private string projectileTimelineName;
    [SerializeField] private VisualEffect vfxPrefab; // Keep this line as is
    [SerializeField] private EventReference shootEventPath; // Updated to use EventReference


    private Timeline myTime;
    private Clock clock;

    // Animator parameters
    private readonly string damageAnimationTrigger = "TakeDamage";

    private List<VisualEffect> vfxPool;
    private int poolSize = 12;

    private void Awake()
    {
        if (vfxPrefab != null)
        {
            InitializeVFXPool();
        }
    }

    private void InitializeVFXPool()
    {
        vfxPool = new List<VisualEffect>();
        for (int i = 0; i < poolSize; i++)
        {
            VisualEffect vfxInstance = Instantiate(vfxPrefab, transform); // Parent to this GameObject
            vfxInstance.gameObject.SetActive(false);
            vfxPool.Add(vfxInstance);
        }
    }

    private VisualEffect GetPooledVFX()
    {
        foreach (VisualEffect vfx in vfxPool)
        {
            if (!vfx.gameObject.activeInHierarchy)
            {
                return vfx;
            }
        }

        // Optionally expand the pool if all objects are in use
        VisualEffect newVFX = Instantiate(vfxPrefab);
        newVFX.gameObject.SetActive(false);
        vfxPool.Add(newVFX);
        return newVFX;
    }

    private void ReturnVFXToPool(VisualEffect vfx)
    {
        vfx.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        InitializeEnemy();
        if (shootInterval == 0)
        {
            if (Koreographer.Instance != null && !string.IsNullOrEmpty(koreographyEventID))
            {
                Koreographer.Instance.RegisterForEvents(koreographyEventID, OnShootSyncedWithMusic);
            }
        }
    }

    private void OnDisable()
    {
        if (shootInterval == 0)
        {
            if (Koreographer.Instance != null && !string.IsNullOrEmpty(koreographyEventID))
            {
                Koreographer.Instance.UnregisterForEvents(koreographyEventID, OnShootSyncedWithMusic);
            }
        }
    }

    private void Start()
    {
        SetupEnemy();
        if (shootInterval > 0)
        {
            StartCoroutine(TimedShooting()); // Start the timed shooting coroutine only if shootInterval is greater than zero
        }
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
            true,
            alternativeProjectileMaterial,
            projectileTimelineName
        );

        FMODUnity.RuntimeManager.PlayOneShot(shootEventPath, projectileOrigin.position);


        // Handle VFX
        if (vfxPrefab != null)
        {
            VisualEffect vfx = GetPooledVFX();
            vfx.transform.position = projectileOrigin.position;
            vfx.transform.rotation = rotationTowardsTarget;
            vfx.gameObject.SetActive(true);
            vfx.Play();

            StartCoroutine(DeactivateVFX(vfx));
        }

    }

    private IEnumerator DeactivateVFX(VisualEffect vfx)
    {
        yield return new WaitForSeconds(1.0f); // Wait for 1 second.

        vfx.Stop(); // Optionally stop the effect to ensure it's not emitting any more particles.
        vfx.gameObject.SetActive(false); // Deactivate the GameObject.
        ReturnVFXToPool(vfx); // Return the VFX to the pool.
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

    private void Update()
    {
        CheckTimelineChanges();
    }

    private float previousTimeScale = 1.0f; // Default time scale
    private float originalPitch = 1.0f; // Default pitch, adjust as needed

    private void CheckTimelineChanges()
    {
        float currentTimeScale = myTime.timeScale;

        if (currentTimeScale != previousTimeScale)
        {
            if (currentTimeScale < 1.0f) // Assuming slowdown corresponds to a lower than 1.0 time scale
            {
                // Decrease pitch by 12 semitones (assuming pitch scale where 0.5 is one octave down)
                FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Pitch", originalPitch / 4);
            }
            else if (currentTimeScale == 1.0f) // Timeline returned to normal
            {
                // Restore original pitch
                FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Pitch", originalPitch);
            }

            previousTimeScale = currentTimeScale;
        }
    }

    private void OnShootSyncedWithMusic(KoreographyEvent koreoEvent)
    {
        // Implementation of shooting logic synchronized with music
        ShootProjectile();
    }

    public void RegisterProjectiles()
    {
        var projectiles = GetComponentsInChildren<ProjectileStateBased>();
        foreach (var projectile in projectiles)
        {
            if (projectile != null)
            {
                ProjectileManager.Instance.RegisterProjectile(projectile);
            }
        }
    }
}