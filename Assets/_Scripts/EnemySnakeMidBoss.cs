using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using BehaviorDesigner.Runtime.Tactical;
using Chronos;
using FMODUnity;
using SonicBloom.Koreo;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.VFX;

[RequireComponent(typeof(Timeline))]
public class EnemySnakeMidBoss : BaseBehaviour, IDamageable, ILimbDamageReceiver
{
    // Serialized fields
    [SerializeField]
    private string enemyType;

    [SerializeField]
    private float startHealth = 100;

    [SerializeField]
    private float currentHealth;

    [SerializeField]
    private Animator animator; // Add this line

    // Fields for shooting functionality
    [SerializeField]
    private Transform projectileOrigin;

    [SerializeField]
    private float shootSpeed = 20f;

    [SerializeField]
    private float projectileLifetime = 5f;

    [SerializeField]
    private float projectileScale = 1f;

    [SerializeField]
    private Material alternativeProjectileMaterial;

    [SerializeField]
    private float shootInterval = 2f;

    [EventID]
    [SerializeField]
    private string koreographyEventID; // Serialized field to set in Inspector

    [SerializeField]
    private string projectileTimelineName;

    [SerializeField]
    private VisualEffect vfxPrefab; // Keep this line as is

    [SerializeField]
    private EventReference shootEventPath; // Updated to use EventReference

    [SerializeField]
    private EventReference hitSoundEventPath;

    [SerializeField]
    private EventReference deathSoundEventPath;

    [SerializeField]
    private Renderer enemyRenderer; // New serialized field for the renderer

    [SerializeField]
    private float flashIntensity = 2f; // Adjust in inspector

    [SerializeField]
    private GameObject shootEffectPrefab; // Changed to GameObject

    private List<GameObject> shootEffectPool;
    private const int ShootEffectPoolSize = 5;

    private Timeline myTime;
    private Clock clock;

    // Animator parameters
    private readonly string damageAnimationTrigger = "TakeDamage";

    private List<VisualEffect> vfxPool;
    private int poolSize = 12;

    private Material enemyMaterial;
    private float originalFinalPower;

    private DestroyEffect destroyEffect;

    // New cached references
    private Transform cachedTransform;
    private GameObject playerTarget;
    private Vector3 cachedShootDirection;
    private Quaternion cachedShootRotation;

    private void Awake()
    {
        // Find the DestroyEffect component
        destroyEffect = GetComponent<DestroyEffect>();

        if (destroyEffect == null)
        {
            Debug.LogWarning("DestroyEffect component not found on " + gameObject.name);
        }

        if (vfxPrefab != null)
        {
            InitializeVFXPool();
        }

        // Cache components
        cachedTransform = transform;
        playerTarget = GameObject.FindGameObjectWithTag("Player");

        InitializeShootEffectPool();
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

    private void InitializeShootEffectPool()
    {
        shootEffectPool = new List<GameObject>();
        for (int i = 0; i < ShootEffectPoolSize; i++)
        {
            GameObject effectInstance = Instantiate(shootEffectPrefab, transform);
            effectInstance.SetActive(false);
            shootEffectPool.Add(effectInstance);
        }
    }

    private GameObject GetPooledShootEffect()
    {
        foreach (GameObject effect in shootEffectPool)
        {
            if (!effect.activeInHierarchy)
            {
                return effect;
            }
        }

        // If all effects are in use, return the first one (overriding its use)
        return shootEffectPool[0];
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
                Koreographer.Instance.UnregisterForEvents(
                    koreographyEventID,
                    OnShootSyncedWithMusic
                );
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

        // Initialize material and original power
        enemyMaterial = enemyRenderer.material;
        originalFinalPower = enemyMaterial.GetFloat("_FinalPower");
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
        _ = OnDeath();
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

    private async void HandleDamage(float amount)
    {
        if (this == null || gameObject == null)
        {
            Debug.LogWarning(
                "EnemySnakeMidBoss or its GameObject has been destroyed. Skipping damage handling."
            );
            return;
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0);

        // Play hit sound
        FMODUnity.RuntimeManager.PlayOneShot(hitSoundEventPath, transform.position);

        // Start flashing effect
        StartCoroutine(FlashEnemy());

        // Check if the damage caused death
        if (currentHealth == 0)
        {
            await OnDeath();
            return; // Exit the method early to skip hit animation
        }

        // Determine the health percentage
        float healthPercentage = (currentHealth / startHealth) * 100;

        // Check for specific health thresholds and trigger the animation
        if (
            healthPercentage <= 75 && healthPercentage > 50
            || healthPercentage <= 50 && healthPercentage > 25
            || healthPercentage <= 25
        )
        {
            animator.SetTrigger("GetHitFront");
        }
    }

    private IEnumerator FlashEnemy()
    {
        // Flash the enemy 3 times
        for (int i = 0; i < 3; i++)
        {
            // Increase brightness
            enemyMaterial.SetFloat("_FinalPower", originalFinalPower * flashIntensity);
            yield return new WaitForSeconds(0.1f);

            // Return to original brightness
            enemyMaterial.SetFloat("_FinalPower", originalFinalPower);
            yield return new WaitForSeconds(0.1f);
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
        if (ProjectileManager.Instance == null || playerTarget == null)
        {
            Debug.LogError("ProjectileManager instance or player target not found.");
            return;
        }

        ProjectileSpawner.Instance.ShootProjectileFromEnemy(
            projectileOrigin.position,
            cachedShootRotation,
            shootSpeed,
            projectileLifetime,
            projectileScale,
            true,
            alternativeProjectileMaterial,
            projectileTimelineName
        );

        FMODUnity.RuntimeManager.PlayOneShot(shootEventPath);

        // Handle VFX
        if (vfxPrefab != null)
        {
            VisualEffect vfx = GetPooledVFX();
            vfx.transform.position = projectileOrigin.position;
            vfx.transform.rotation = cachedShootRotation;
            vfx.gameObject.SetActive(true);
            vfx.Play();

            StartCoroutine(DeactivateVFX(vfx));
        }

        // Play shoot effect
        PlayShootEffect();
    }

    private void PlayShootEffect()
    {
        GameObject effect = GetPooledShootEffect();
        effect.transform.position = projectileOrigin.position;
        effect.transform.rotation = cachedShootRotation;
        effect.SetActive(true);

        // Assuming the effect has a ParticleSystem component
        ParticleSystem particleSystem = effect.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            particleSystem.Play();
            StartCoroutine(DeactivateShootEffect(effect, particleSystem.main.duration));
        }
        else
        {
            // If there's no ParticleSystem, deactivate after a fixed time
            StartCoroutine(DeactivateShootEffect(effect, 1f));
        }
    }

    private IEnumerator DeactivateShootEffect(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        effect.SetActive(false);
    }

    private IEnumerator DeactivateVFX(VisualEffect vfx)
    {
        yield return new WaitForSeconds(1.0f); // Wait for 1 second.

        vfx.Stop(); // Optionally stop the effect to ensure it's not emitting any more particles.
        vfx.gameObject.SetActive(false); // Deactivate the GameObject.
        ReturnVFXToPool(vfx); // Return the VFX to the pool.
    }

    private async Task OnDeath()
    {
        // Notify SceneManagerBTR of the enemy's death immediately
        NotifySceneManagerOfDeath();

        // Play death sound
        FMODUnity.RuntimeManager.PlayOneShot(deathSoundEventPath, transform.position);

        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetTrigger("Die");
            await WaitForAnimationState("Death");
            float animationLength = animator.GetCurrentAnimatorStateInfo(0).length;
            await Task.Delay(Mathf.RoundToInt(animationLength * 1000));
        }
        else
        {
            Debug.LogWarning("Animator is null or disabled during OnDeath for EnemySnakeMidBoss");
            await Task.Delay(2000); // Wait for 2 seconds as a fallback
        }

        // Cleanup effects
        if (destroyEffect != null)
        {
            destroyEffect.CleanupEffects();
        }
        else
        {
            Debug.LogWarning("DestroyEffect is null, skipping effect cleanup");
        }

        // Additional cleanup or state management here, if necessary
        CleanupDeathEffects();

        // The scene change will be handled by SceneManagerBTR based on the updateStatus call
    }

    private void CleanupDeathEffects()
    {
        if (this == null || gameObject == null)
        {
            Debug.LogWarning(
                "EnemySnakeMidBoss or its GameObject has been destroyed. Skipping cleanup."
            );
            return;
        }

        // Stop any ongoing tweens or effects
        PrimeTween.Tween.StopAll(this);

        try
        {
            // Disable any lights or particle systems
            DisableComponents<Light>();
            DisableComponents<ParticleSystem>();
            DisableComponents<Renderer>();
            DisableComponents<Collider>();
        }
        catch (MissingReferenceException)
        {
            Debug.LogWarning(
                "Some components were destroyed during cleanup. Continuing with available components."
            );
        }
    }

    private void DisableComponents<T>()
        where T : Component
    {
        var components = GetComponentsInChildren<T>(true);
        foreach (var component in components)
        {
            if (component != null)
            {
                if (component is ParticleSystem ps)
                    ps.Stop();
                else if (component is Behaviour behaviour)
                    behaviour.enabled = false;
            }
        }
    }

    private async Task WaitForAnimationState(string stateName)
    {
        float timeout = 5f; // 5 seconds timeout
        float elapsedTime = 0f;

        while (
            animator != null
            && animator.isActiveAndEnabled
            && !animator.GetCurrentAnimatorStateInfo(0).IsName(stateName)
        )
        {
            await Task.Delay(50); // Wait for 50ms instead of using Task.Yield()
            elapsedTime += 0.05f;

            if (elapsedTime > timeout)
            {
                Debug.LogWarning($"Timeout waiting for animation state: {stateName}");
                break;
            }
        }
    }

    private void NotifySceneManagerOfDeath()
    {
        SceneManagerBTR.Instance.updateStatus("waveend");
    }

    private void Update()
    {
        CheckTimelineChanges();
        UpdateShootDirection(); // Update the shooting direction each frame
    }

    private void UpdateShootDirection()
    {
        if (playerTarget != null)
        {
            cachedShootDirection = playerTarget.transform.position - projectileOrigin.position;
            cachedShootDirection.Normalize();
            cachedShootRotation = Quaternion.LookRotation(cachedShootDirection);
        }
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
                FMODUnity.RuntimeManager.StudioSystem.setParameterByName(
                    "Pitch",
                    originalPitch / 4
                );
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
