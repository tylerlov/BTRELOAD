using System.Collections;
using Chronos;
using Typooling;
using PathologicalGames;
using SonicBloom.Koreo;
using UnityEngine;

[RequireComponent(typeof(Timeline))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
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

    protected virtual void Awake()
    {
        // Common Awake logic
    }

    protected virtual void OnEnable()
    {
        InitializeEnemy();
    }

    protected virtual void Start()
    {
        SetupEnemy();
    }

    public abstract void Damage(float amount);

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
            ConditionalDebug.Log($"{gameObject.name} health reached 0 or below, initiating Death coroutine");
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
        lockedOnIndicator.SetActive(status);
        if (!status && lockOnDisabledParticles != null)
        {
            lockOnDisabledParticles.GetFromPool(transform.position, Quaternion.identity);
        }
    }

    public abstract void RegisterProjectiles();
}
