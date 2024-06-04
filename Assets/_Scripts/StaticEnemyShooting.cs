using UnityEngine;
using SonicBloom.Koreo;
using FMODUnity;
using FMOD.Studio;
using Chronos;
using System.Collections; 
public class StaticEnemyShooting : MonoBehaviour
{
    [SerializeField] private string enemyType;
    [SerializeField] private float lerpDuration = 1f; // Duration of the lerp animation
    [SerializeField] private float irisSizeStart = 0.5f;
    [SerializeField] private float irisSizeEnd = 1f;
    [SerializeField] private float pupilSizeStart = 0.5f;
    [SerializeField] private float pupilSizeEnd = 1f;
    [SerializeField] private float mainLightingIntensityStart = 0.5f;
    [SerializeField] private float mainLightingIntensityEnd = 1f;
    private Material eyeMaterial; // This will now be assigned automatically
    private Timeline myTime;
    private int shootCounter = 0; // Counter to track the number of times OnMusicalEnemyShoot has been called

    [SerializeField] private float shootSpeed = 20f; // Speed of the projectile
    [SerializeField] private float projectileLifetime = 5f; // Lifetime of the projectile in seconds
    [SerializeField] private float projectileScale = 1f; // Uniform scale of the projectile
    [SerializeField] private Material alternativeProjectileMaterial; // Define alternative material in the inspector

    [SerializeField] private Transform target; // Assign this in the inspector

    void Awake()
    {
        // Any initialization that doesn't depend on other objects
    }

    void Start()
    {
        myTime = GetComponent<Timeline>();
        eyeMaterial = GetComponent<Renderer>().material; // Automatically assign the material
    }

    void OnDisable()
    {
        if (EnemyShootingManager.Instance != null)
        {
            EnemyShootingManager.Instance.UnregisterStaticEnemyShooting(this);
        }
    }

    public void OnEnable()
    {
        if (EnemyShootingManager.Instance != null)
        {
            EnemyShootingManager.Instance.RegisterStaticEnemyShooting(this);
        }
    }

    public void Shoot()
    {
        if (this == null || !gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[StaticEnemyShooting] GameObject is not active in hierarchy or has been destroyed.");
            return;
        }

        StartCoroutine(AnimateShaderProperties());
        Vector3 directionToTarget = target != null ? (target.position - transform.position).normalized : transform.forward;

        if (ProjectileManager.Instance == null)
        {
            Debug.LogError("[StaticEnemyShooting] ProjectileManager instance is null.");
        }
        else
        {
            // Shoot the projectile
            ProjectileManager.Instance.ShootProjectile(transform.position, Quaternion.LookRotation(directionToTarget), shootSpeed, projectileLifetime, projectileScale, false, alternativeProjectileMaterial);
        }
    }

    IEnumerator AnimateShaderProperties()
    {
        float timeElapsed = 0f;
        float updateInterval = 0.1f; // Update every 0.1 seconds to reduce the number of updates
        float nextUpdateTime = 0f;

        while (timeElapsed < lerpDuration)
        {
            if (timeElapsed >= nextUpdateTime)
            {
                float lerpFactor = timeElapsed / lerpDuration;
                eyeMaterial.SetFloat("Vector1_520f2e2b2d664517a415c2d1d2d003e1", Mathf.Lerp(irisSizeStart, irisSizeEnd, lerpFactor));
                eyeMaterial.SetFloat("Vector1_c88d82cf95c0459d90a5f7c35020e695", Mathf.Lerp(pupilSizeStart, pupilSizeEnd, lerpFactor));
                eyeMaterial.SetFloat("Vector1_62c9d5aca0154b4386a16cd0625b239b", Mathf.Lerp(mainLightingIntensityStart, mainLightingIntensityEnd, lerpFactor));

                nextUpdateTime += updateInterval;
            }

            timeElapsed += myTime.deltaTime;
            yield return null;
        }

        // Reset timeElapsed for the return journey
        timeElapsed = 0f;
        // Calculate the return duration as twice the approach duration
        float returnDuration = lerpDuration * 2;

        // Animate back to the start values at half the speed
        while (timeElapsed < returnDuration)
        {
            float lerpFactor = timeElapsed / returnDuration;

            eyeMaterial.SetFloat("Vector1_520f2e2b2d664517a415c2d1d2d003e1", Mathf.Lerp(irisSizeEnd, irisSizeStart, lerpFactor));
            eyeMaterial.SetFloat("Vector1_c88d82cf95c0459d90a5f7c35020e695", Mathf.Lerp(pupilSizeEnd, pupilSizeStart, lerpFactor));
            eyeMaterial.SetFloat("Vector1_62c9d5aca0154b4386a16cd0625b239b", Mathf.Lerp(mainLightingIntensityEnd, mainLightingIntensityStart, lerpFactor));

            timeElapsed += myTime.deltaTime;
            yield return null;
        }

        // Ensure the final values are reset to the start values
        eyeMaterial.SetFloat("Vector1_520f2e2b2d664517a415c2d1d2d003e1", irisSizeStart);
        eyeMaterial.SetFloat("Vector1_c88d82cf95c0459d90a5f7c35020e695", pupilSizeStart);
        eyeMaterial.SetFloat("Vector1_62c9d5aca0154b4386a16cd0625b239b", mainLightingIntensityStart);
    }
}
