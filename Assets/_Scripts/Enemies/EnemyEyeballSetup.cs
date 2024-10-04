using System.Collections;
using Chronos;
using OccaSoftware.BOP;
using PathologicalGames;
using SonicBloom.Koreo;
using UnityEngine;

[RequireComponent(typeof(Timeline))]
public class EnemyEyeballSetup : MonoBehaviour, IDamageable
{
    #region Health and Type
    [SerializeField]
    protected float startHealth = 100;
    protected float currentHealth;

    [SerializeField]
    protected string enemyType;
    #endregion

    #region GameObject References
    [SerializeField]
    protected GameObject enemyModel;

    [SerializeField]
    protected GameObject lockedOnIndicator;

    [HideInInspector]
    public GameObject playerTarget;
    #endregion

    #region Particle Systems
    [SerializeField]
    protected Pooler deathParticles;

    [SerializeField]
    protected Pooler birthParticles;

    [SerializeField]
    protected Pooler lockOnDisabledParticles;
    #endregion

    #region Koreographer
    [SerializeField, EventID]
    protected string eventID;
    #endregion

    #region Pooling
    protected bool enemyPooling = true;
    protected string associatedPool;
    #endregion

    #region Time Management
    protected Timeline myTime;
    protected Clock clock;
    #endregion

    #region Shooting
    [SerializeField]
    private float minShootDelay = 1f;

    [SerializeField]
    private float maxShootDelay = 3f;

    [SerializeField]
    private float shootSpeed = 10f;

    [SerializeField]
    private float projectileLifetime = 5f;

    [SerializeField]
    private float projectileScale = 1f;

    [SerializeField]
    private Material projectileMaterial;
    private float nextShootTime;
    #endregion

    protected virtual void Awake()
    {
        // ... existing Awake code ...
    }

    protected virtual void OnEnable()
    {
        InitializeEnemy();
    }

    protected virtual void Start()
    {
        SetupEnemy();
        SetNextShootTime();
    }

    public virtual void Damage(float amount)
    {
        HandleDamage(amount);
    }

    public bool IsAlive() => currentHealth > 0;

    public void ResetHealth()
    {
        currentHealth = startHealth;
        gameObject.SetActive(true);
    }

    protected virtual void InitializeEnemy()
    {
        playerTarget = GameObject.FindGameObjectWithTag("Player");
        enemyModel.SetActive(true);
        currentHealth = startHealth;
        FMODUnity.RuntimeManager.PlayOneShotAttached(
            "event:/Enemy/" + enemyType + "/Birth",
            gameObject
        );
        clock = Timekeeper.instance.Clock("Test");

        if (birthParticles != null)
        {
            birthParticles.GetFromPool(transform.position, Quaternion.identity);
        }
    }

    protected virtual void SetupEnemy()
    {
        myTime = GetComponent<Timeline>();
    }

    protected virtual void HandleDamage(float amount)
    {
        float previousHealth = currentHealth;
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        ConditionalDebug.Log(
            $"{gameObject.name} took {amount} damage. Health reduced from {previousHealth} to {currentHealth}"
        );

        if (currentHealth <= 0)
        {
            ConditionalDebug.Log(
                $"{gameObject.name} health reached 0 or below, initiating Death coroutine"
            );
            StartCoroutine(Death());
        }
    }

    protected virtual IEnumerator Death()
    {
        ConditionalDebug.Log($"{enemyType} has died");

        deathParticles.GetFromPool(transform.position, Quaternion.identity);
        FMODUnity.RuntimeManager.PlayOneShot(
            "event:/Enemy/" + enemyType + "/Death",
            transform.position
        );

        yield return new WaitForSeconds(0.5f);
        enemyModel.SetActive(false);
        yield return new WaitForSeconds(0.5f);

        transform.position = Vector3.zero;
        PoolManager.Pools[associatedPool].Despawn(transform);
    }

    public virtual void lockedStatus(bool status)
    {
        if (lockedOnIndicator != null)
        {
            lockedOnIndicator.SetActive(status);
        }

        if (status)
        {
            FMODUnity.RuntimeManager.PlayOneShotAttached(
                "event:/Enemy/" + enemyType + "/Locked",
                gameObject
            );
        }
        else if (lockOnDisabledParticles != null)
        {
            lockOnDisabledParticles.GetFromPool(transform.position, Quaternion.identity);
        }
    }

    protected virtual void Update()
    {
        if (Time.time >= nextShootTime)
        {
            AttemptToShoot();
        }
    }

    private void SetNextShootTime()
    {
        nextShootTime = Time.time + Random.Range(minShootDelay, maxShootDelay);
    }

    private void AttemptToShoot()
    {
        ConditionalDebug.Log($"[{gameObject.name}] Attempting to shoot at time: {Time.time}");
        if (playerTarget == null)
        {
            ConditionalDebug.LogWarning(
                $"[{gameObject.name}] Player target is null. Cannot shoot."
            );
            SetNextShootTime();
            return;
        }

        Vector3 directionToPlayer = (
            playerTarget.transform.position - transform.position
        ).normalized;
        Quaternion rotationToPlayer = Quaternion.LookRotation(directionToPlayer);

        bool shotRequested = ProjectileSpawner.Instance.RequestEnemyShot(() =>
        {
            ConditionalDebug.Log(
                $"[{gameObject.name}] Shot request approved. Shooting projectile."
            );
            ProjectileSpawner.Instance.ShootProjectileFromEnemy(
                transform.position,
                rotationToPlayer,
                shootSpeed,
                projectileLifetime,
                projectileScale,
                10f, // Add a damage value here, adjust as needed
                enableHoming: true,
                material: projectileMaterial,
                clockKey: "Test",
                accuracy: 0.9f
            );

            // Play shooting sound
            FMODUnity.RuntimeManager.PlayOneShotAttached(
                "event:/Enemy/" + enemyType + "/Shoot",
                gameObject
            );
        });

        if (shotRequested)
        {
            ConditionalDebug.Log($"[{gameObject.name}] Shot successfully requested and fired.");
        }
        else
        {
            ConditionalDebug.Log($"[{gameObject.name}] Shot request denied by ProjectileManager.");
        }

        SetNextShootTime();
    }

    public virtual void RegisterProjectiles()
    {
        // This method is called to register projectiles with the ProjectileManager
        // It's not needed for this setup as we're using ProjectileManager.ShootProjectileFromEnemy
        // But we'll keep it for consistency with the IDamageable interface
    }
}
