using UnityEngine;
using System.Collections.Generic;
using FMODUnity;

[RequireComponent(typeof(EnemyBasics))]
[RequireComponent(typeof(EnemyBasicAI))]
public class EnemyBasicCombat : MonoBehaviour
{
    #region Settings
    [Header("Combat Settings")]
    [SerializeField] private float minAttackCooldown = 0.5f;
    [SerializeField] private float maxAttackCooldown = 2f;
    [SerializeField] private float shootSpeed = 10f;
    [SerializeField] private float projectileLifetime = 5f;
    [SerializeField] private float projectileScale = 1f;

    [Header("Audio")]
    [SerializeField] private EventReference attackSound;
    [SerializeField] private EventReference attackSound2;
    #endregion

    #region Private Fields
    private EnemyBasics basics;
    private EnemyBasicAI ai;
    private float lastAttackTime;
    private float currentAttackCooldown;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        basics = GetComponent<EnemyBasics>();
        ai = GetComponent<EnemyBasicAI>();
        ResetAttackCooldown();
    }

    private void Start()
    {
        // Registration with EnemyManager is handled in EnemyBasicAI
    }

    private void OnDestroy()
    {
        // Unregistration with EnemyManager is handled in EnemyBasicAI
    }
    #endregion

    #region Combat Logic
    public bool CanAttack()
    {
        return Time.time - lastAttackTime >= currentAttackCooldown &&
               ai.HasLineOfSight() &&
               basics.IsAlive;
    }

    public void TryAttack()
    {
        if (!CanAttack()) return;

        PerformAttack();
        lastAttackTime = Time.time;
        ResetAttackCooldown();
    }

    protected virtual void PerformAttack()
    {
        // Calculate shoot direction
        Vector3 shootDirection = (basics.PlayerTransform.position - transform.position).normalized;
        Vector3 shootPosition = transform.position;
        Quaternion shootRotation = Quaternion.LookRotation(shootDirection);

        // Spawn projectile
        if (ProjectileSpawner.Instance != null)
        {
            ProjectileStateBased projectile = ProjectileSpawner.Instance.ShootProjectileFromEnemy(
                shootPosition,
                shootRotation,
                shootSpeed,
                projectileLifetime,
                projectileScale,
                10f,
                enableHoming: true,
                material: null,
                clockKey: "",
                accuracy: -1f,
                target: basics.PlayerTransform,
                isStatic: false
            );

            if (projectile != null)
            {
                projectile.SetHomingTarget(basics.PlayerTransform);
            }
        }

        // Play attack sounds
        if (!attackSound.IsNull)
        {
            RuntimeManager.PlayOneShot(attackSound, gameObject.transform.position);
        }
        if (!attackSound2.IsNull)
        {
            RuntimeManager.PlayOneShot(attackSound2, gameObject.transform.position);
        }
    }

    private void ResetAttackCooldown()
    {
        currentAttackCooldown = Random.Range(minAttackCooldown, maxAttackCooldown);
    }
    #endregion

    #region Public Interface
    public void ResetCombat()
    {
        lastAttackTime = -minAttackCooldown;
    }

    public float GetLastAttackTime() => lastAttackTime;
    public float GetAttackCooldown() => currentAttackCooldown;
    #endregion
}
