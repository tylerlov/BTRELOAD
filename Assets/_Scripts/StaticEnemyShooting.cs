using UnityEngine;
using Chronos;
using System.Collections;
using PrimeTween;

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
            ConditionalDebug.LogWarning("[StaticEnemyShooting] GameObject is not active in hierarchy or has been destroyed.");
            return;
        }

        AnimateShaderProperties();
        Vector3 directionToTarget = target != null ? (target.position - transform.position).normalized : transform.forward;

        if (ProjectileManager.Instance == null)
        {
            ConditionalDebug.LogError("[StaticEnemyShooting] ProjectileManager instance is null.");
        }
        else
        {
            // Shoot the projectile
            ProjectileManager.Instance.ShootProjectile(transform.position, Quaternion.LookRotation(directionToTarget), shootSpeed, projectileLifetime, projectileScale, false, alternativeProjectileMaterial);
        }
    }

    private void AnimateShaderProperties()
    {
        // Animate to end values
        Tween.Custom(irisSizeStart, irisSizeEnd, lerpDuration, newVal => eyeMaterial.SetFloat("Vector1_520f2e2b2d664517a415c2d1d2d003e1", newVal))
            .OnComplete(target: this, target => target.OnTweenComplete());

        Tween.Custom(pupilSizeStart, pupilSizeEnd, lerpDuration, newVal => eyeMaterial.SetFloat("Vector1_c88d82cf95c0459d90a5f7c35020e695", newVal))
            .OnComplete(target: this, target => target.OnTweenComplete());

        Tween.Custom(mainLightingIntensityStart, mainLightingIntensityEnd, lerpDuration, newVal => eyeMaterial.SetFloat("Vector1_62c9d5aca0154b4386a16cd0625b239b", newVal))
            .OnComplete(target: this, target => target.OnTweenComplete());

        // Animate back to start values at half the speed
        float returnDuration = lerpDuration * 2;

        Tween.Custom(irisSizeEnd, irisSizeStart, returnDuration, newVal => eyeMaterial.SetFloat("Vector1_520f2e2b2d664517a415c2d1d2d003e1", newVal))
            .OnComplete(target: this, target => target.OnTweenComplete());

        Tween.Custom(pupilSizeEnd, pupilSizeStart, returnDuration, newVal => eyeMaterial.SetFloat("Vector1_c88d82cf95c0459d90a5f7c35020e695", newVal))
            .OnComplete(target: this, target => target.OnTweenComplete());

        Tween.Custom(mainLightingIntensityEnd, mainLightingIntensityStart, returnDuration, newVal => eyeMaterial.SetFloat("Vector1_62c9d5aca0154b4386a16cd0625b239b", newVal))
            .OnComplete(target: this, target => target.OnTweenComplete());
    }

    private void OnTweenComplete()
    {
        // This code will execute after the PrimeTween animation completes.
        // Ensure the final values are reset to the start values
        eyeMaterial.SetFloat("Vector1_520f2e2b2d664517a415c2d1d2d003e1", irisSizeStart);
        eyeMaterial.SetFloat("Vector1_c88d82cf95c0459d90a5f7c35020e695", pupilSizeStart);
        eyeMaterial.SetFloat("Vector1_62c9d5aca0154b4386a16cd0625b239b", mainLightingIntensityStart);
    }
}
