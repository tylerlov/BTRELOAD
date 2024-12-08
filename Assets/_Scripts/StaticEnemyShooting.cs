using UnityEngine;

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

    private const string LOG_PREFIX = "[StaticEnemyShooting] ";
    private static readonly System.Text.StringBuilder logBuilder = new System.Text.StringBuilder(256);

    void Awake()
    {
        cachedTransform = transform;
    }

    public void OnEnable()
    {
        EnemyManager.Instance?.RegisterStaticEnemyShooting(this);
    }

    void OnDisable()
    {
        EnemyManager.Instance?.UnregisterStaticEnemyShooting(this);
    }

    public void Shoot()
    {
        #if UNITY_EDITOR
        logBuilder.Clear()
            .Append(LOG_PREFIX)
            .Append(gameObject.name)
            .Append(" Shoot called");
        #endif

        if (this == null)
        {
            #if UNITY_EDITOR
            ConditionalDebug.LogError(logBuilder.Append(": Component is null").ToString());
            #endif
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            #if UNITY_EDITOR
            ConditionalDebug.Log(logBuilder.Append(": GameObject is inactive").ToString());
            #endif
            return;
        }

        if (EnemyManager.Instance == null)
        {
            #if UNITY_EDITOR
            ConditionalDebug.LogError(logBuilder.Append(": EnemyManager.Instance is null").ToString());
            #endif
            return;
        }

        bool shouldShoot = true;
        float currentTime = EnemyManager.Instance.GetCurrentTime();
        float timeSinceLastShot = currentTime - lastShootTime;

        #if UNITY_EDITOR
        logBuilder.Append("\n- Time since last shot: ")
            .Append(timeSinceLastShot.ToString("F3"))
            .Append("s (min: ")
            .Append(minTimeBetweenShots.ToString("F3"))
            .Append("s)");
        #endif

        if (timeSinceLastShot < minTimeBetweenShots)
        {
            #if UNITY_EDITOR
            ConditionalDebug.Log(logBuilder.Append("\n- Too soon to shoot again").ToString());
            #endif
            shouldShoot = false;
        }

        if (shouldShoot)
        {
            #if UNITY_EDITOR
            logBuilder.Append("\n- Attempting PerformShoot");
            #endif
            
            bool shotResult = PerformShoot();
            lastShootTime = shotResult ? currentTime : lastShootTime;

            #if UNITY_EDITOR
            ConditionalDebug.Log(logBuilder.Append("\n- Shot result: ").Append(shotResult ? "Success" : "Failed").ToString());
            #endif
        }
    }

    private bool PerformShoot()
    {
        #if UNITY_EDITOR
        logBuilder.Clear()
            .Append(LOG_PREFIX)
            .Append(gameObject.name)
            .Append(" PerformShoot:");
        #endif

        if (ProjectileSpawner.Instance == null)
        {
            #if UNITY_EDITOR
            ConditionalDebug.LogError(logBuilder.Append("\nProjectileSpawner.Instance is null").ToString());
            #endif
            return false;
        }

        if (ProjectileManager.Instance == null)
        {
            #if UNITY_EDITOR
            ConditionalDebug.LogError(logBuilder.Append("\nProjectileManager.Instance is null").ToString());
            #endif
            return false;
        }

        Vector3 shootDirection = cachedTransform.up;
        #if UNITY_EDITOR
        logBuilder.Append("\n- Shoot Direction: ")
            .Append(shootDirection)
            .Append("\n- Position: ")
            .Append(cachedTransform.position)
            .Append("\n- Speed: ")
            .Append(shootSpeed);
        #endif

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
                #if UNITY_EDITOR
                ConditionalDebug.LogWarning(logBuilder.Append("\nFailed to create projectile").ToString());
                #endif
                return false;
            }

            #if UNITY_EDITOR
            logBuilder.Append("\n- Projectile created successfully: ID=")
                .Append(projectile.GetInstanceID())
                .Append("\n- Projectile position: ")
                .Append(projectile.transform.position);
            if (projectile.rb != null)
            {
                logBuilder.Append("\n- Projectile velocity: ")
                    .Append(projectile.rb.linearVelocity);
            }
            ConditionalDebug.Log(logBuilder.ToString());
            #endif
            return true;
        }
        catch (System.Exception e)
        {
            #if UNITY_EDITOR
            ConditionalDebug.LogError(logBuilder.Append("\nError while shooting: ")
                .Append(e.Message)
                .Append("\n")
                .Append(e.StackTrace)
                .ToString());
            #endif
            return false;
        }
    }
}
