using UnityEngine;
using FMODUnity;

public class StaticEnemyShooting : MonoBehaviour
{
    [SerializeField]
    private string enemyType;

    [SerializeField]
    private float shootSpeed = 25f;

    [SerializeField]
    private float projectileLifetime = 3f;

    [SerializeField]
    private float projectileScale = 1f;

    [SerializeField]
    private Material alternativeProjectileMaterial;

    [SerializeField]
    private Transform target;

    private Transform cachedTransform;
    private float lastShootTime = 0f;

    [SerializeField]
    private float minTimeBetweenShots = 0.1f;

    void Awake()
    {
        cachedTransform = transform;
    }

    public void OnEnable()
    {
        EnemyShootingManager.Instance?.RegisterStaticEnemyShooting(this);
    }

    void OnDisable()
    {
        EnemyShootingManager.Instance?.UnregisterStaticEnemyShooting(this);
    }

    public void Shoot()
    {
        string debugInfo = $"[StaticEnemyShooting:{gameObject.name}] Shoot called:\n";

        // Add FMOD parameter check
        float lockState = 0f;
        var musicEmitter = GameObject.Find("FMOD Music")?.GetComponent<StudioEventEmitter>();
        if (musicEmitter != null && musicEmitter.EventInstance.isValid())
        {
            musicEmitter.EventInstance.getParameterByName("Lock State", out lockState);
            debugInfo += $"- Current Lock State: {lockState}\n";
        }

        if (this == null)
        {
            ConditionalDebug.LogError($"{debugInfo} Component is null");
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            ConditionalDebug.Log($"{debugInfo} GameObject is inactive");
            return;
        }

        if (EnemyShootingManager.Instance == null)
        {
            ConditionalDebug.LogError($"{debugInfo} EnemyShootingManager.Instance is null");
            return;
        }

        bool shouldShoot = true;
        float currentTime = EnemyShootingManager.Instance.GetCurrentTime();
        float timeSinceLastShot = currentTime - lastShootTime;
        debugInfo += $"- Time since last shot: {timeSinceLastShot:F3}s (min: {minTimeBetweenShots:F3}s)\n";

        if (timeSinceLastShot < minTimeBetweenShots)
        {
            ConditionalDebug.Log($"{debugInfo} Too soon to shoot again");
            shouldShoot = false;
        }

        if (shouldShoot)
        {
            debugInfo += "- Attempting PerformShoot\n";
            bool shotResult = PerformShoot();
            lastShootTime = shotResult ? currentTime : lastShootTime; // Only update lastShootTime if shot was successful
            ConditionalDebug.Log($"{debugInfo}- Shot result: {(shotResult ? "Success" : "Failed")}");
        }
    }

    private bool PerformShoot()
    {
        string debugInfo = $"[StaticEnemyShooting:{gameObject.name}] PerformShoot:\n";

        if (ProjectileSpawner.Instance == null)
        {
            ConditionalDebug.LogError($"{debugInfo} ProjectileSpawner.Instance is null");
            return false;
        }

        if (ProjectileManager.Instance == null)
        {
            ConditionalDebug.LogError($"{debugInfo} ProjectileManager.Instance is null");
            return false;
        }

        Vector3 shootDirection = cachedTransform.up;
        debugInfo += $"- Shoot Direction: {shootDirection}\n";
        debugInfo += $"- Position: {cachedTransform.position}\n";
        debugInfo += $"- Speed: {shootSpeed}\n";

        try
        {
            ProjectileStateBased projectile = ProjectileSpawner.Instance.ShootProjectileFromEnemy(
                cachedTransform.position,
                Quaternion.LookRotation(shootDirection),
                shootSpeed,
                projectileLifetime,
                projectileScale,
                10f,
                enableHoming: false,
                alternativeProjectileMaterial,
                "",
                -1f,
                null,
                true
            );

            if (projectile == null)
            {
                ConditionalDebug.LogWarning($"{debugInfo} Failed to create projectile");
                return false;
            }

            debugInfo += $"- Projectile created successfully: ID={projectile.GetInstanceID()}\n";
            debugInfo += $"- Projectile position: {projectile.transform.position}\n";
            if (projectile.rb != null)
            {
                debugInfo += $"- Projectile velocity: {projectile.rb.linearVelocity}\n";
            }

            ConditionalDebug.Log(debugInfo);
            return true;
        }
        catch (System.Exception e)
        {
            ConditionalDebug.LogError($"{debugInfo} Error while shooting: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }
}
