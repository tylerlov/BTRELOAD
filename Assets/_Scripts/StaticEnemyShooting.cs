using UnityEngine;
using Chronos;
using System.Collections.Generic;
using System.Collections;

public class StaticEnemyShooting : MonoBehaviour
{
    [SerializeField] private static bool enableAnimation = false; // Make this static
    [SerializeField] private string enemyType;
    [SerializeField] private float animationDuration = 2f;
    [SerializeField] private float irisSizeStart = 0.5f, irisSizeEnd = 1f;
    [SerializeField] private float pupilSizeStart = 0.5f, pupilSizeEnd = 1f;
    [SerializeField] private float mainLightingIntensityStart = 0.5f, mainLightingIntensityEnd = 1f;
    [SerializeField] private float shootSpeed = 20f;
    [SerializeField] private float projectileLifetime = 5f;
    [SerializeField] private float projectileScale = 1f;
    [SerializeField] private Material alternativeProjectileMaterial;
    [SerializeField] private Transform target;
    [SerializeField] private float shootCooldown = 0.15f; // Cooldown time in seconds

    private static List<StaticEnemyShooting> activeInstances = new List<StaticEnemyShooting>();
    private static MaterialPropertyBlock propertyBlock;
    private static Coroutine sharedAnimationCoroutine;

    private Renderer enemyRenderer;
    private Timeline myTime;
    private float lastShootTime;

    private static readonly int IrisSizeID = Shader.PropertyToID("Vector1_520f2e2b2d664517a415c2d1d2d003e1");
    private static readonly int PupilSizeID = Shader.PropertyToID("Vector1_c88d82cf95c0459d90a5f7c35020e695");
    private static readonly int MainLightingIntensityID = Shader.PropertyToID("Vector1_62c9d5aca0154b4386a16cd0625b239b");

    void Awake()
    {
        enemyRenderer = GetComponent<Renderer>();
        myTime = GetComponent<Timeline>();

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();
    }

    public void OnEnable()
    {
        activeInstances.Add(this);
        EnemyShootingManager.Instance?.RegisterStaticEnemyShooting(this);

        if (sharedAnimationCoroutine == null)
            sharedAnimationCoroutine = StartCoroutine(SharedAnimationCoroutine());
    }

    void OnDisable()
    {
        activeInstances.Remove(this);
        EnemyShootingManager.Instance?.UnregisterStaticEnemyShooting(this);

        if (activeInstances.Count == 0 && sharedAnimationCoroutine != null)
        {
            StopCoroutine(sharedAnimationCoroutine);
            sharedAnimationCoroutine = null;
        }
    }

    private static IEnumerator SharedAnimationCoroutine()
    {
        while (true)
        {
            if (!enableAnimation) // Access the static field directly
            {
                yield return null;
                continue;
            }

            float elapsedTime = 0f;
            while (elapsedTime < activeInstances[0].animationDuration)
            {
                float t = elapsedTime / activeInstances[0].animationDuration;
                UpdateAllInstances(t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            elapsedTime = activeInstances[0].animationDuration;
            while (elapsedTime > 0f)
            {
                float t = elapsedTime / activeInstances[0].animationDuration;
                UpdateAllInstances(t);
                elapsedTime -= Time.deltaTime;
                yield return null;
            }
        }
    }

    private static void UpdateAllInstances(float t)
    {
        foreach (var instance in activeInstances)
        {
            instance.UpdateMaterialProperties(t);
        }
    }

    private void UpdateMaterialProperties(float t)
    {
        enemyRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat(IrisSizeID, Mathf.Lerp(irisSizeStart, irisSizeEnd, t));
        propertyBlock.SetFloat(PupilSizeID, Mathf.Lerp(pupilSizeStart, pupilSizeEnd, t));
        propertyBlock.SetFloat(MainLightingIntensityID, Mathf.Lerp(mainLightingIntensityStart, mainLightingIntensityEnd, t));

        enemyRenderer.SetPropertyBlock(propertyBlock);
    }

    public void Shoot()
    {
        if (Time.time - lastShootTime < shootCooldown)
        {
            return; // Exit if we're still in the cooldown period
        }

        if (this == null || !gameObject.activeInHierarchy)
        {
            ConditionalDebug.LogWarning("[StaticEnemyShooting] GameObject is not active in hierarchy or has been destroyed.");
            return;
        }

        Vector3 directionToTarget = target != null ? (target.position - transform.position).normalized : transform.forward;

        if (ProjectileManager.Instance == null)
        {
            ConditionalDebug.LogError("[StaticEnemyShooting] ProjectileManager instance is null.");
        }
        else
        {
            ProjectileManager.Instance.ShootProjectile(transform.position, Quaternion.LookRotation(directionToTarget), shootSpeed, projectileLifetime, projectileScale, false, alternativeProjectileMaterial);
            lastShootTime = Time.time; // Update the last shoot time
            ConditionalDebug.Log($"[StaticEnemyShooting] Projectile fired from {gameObject.name} at {Time.time}");
        }
    }

    public void StopAllAnimations()
    {
        if (sharedAnimationCoroutine != null)
        {
            StopCoroutine(sharedAnimationCoroutine);
            sharedAnimationCoroutine = null;
        }
        
        ResetMaterialProperties();
    }

    private void ResetMaterialProperties()
    {
        enemyRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat(IrisSizeID, irisSizeStart);
        propertyBlock.SetFloat(PupilSizeID, pupilSizeStart);
        propertyBlock.SetFloat(MainLightingIntensityID, mainLightingIntensityStart);

        enemyRenderer.SetPropertyBlock(propertyBlock);
    }
}
