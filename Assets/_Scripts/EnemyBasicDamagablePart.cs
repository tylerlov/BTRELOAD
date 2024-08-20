using UnityEngine;
using OccaSoftware.BOP;
using BehaviorDesigner.Runtime.Tactical;


[RequireComponent(typeof(Collider))]
public class EnemyBasicDamagablePart : MonoBehaviour, IDamageable
{
    public EnemyBasicSetup mainEnemyScript;
    [SerializeField] private GameObject lockOnAnim;
    [SerializeField] private Pooler lockOnDisabledParticles;
    [SerializeField] private Pooler deathParticles;

    private int hitsTaken = 0;
    private Collider partCollider;
    private bool isLockedOn = false;

    private void Awake()
    {
        partCollider = GetComponent<Collider>();
        partCollider.isTrigger = true;
    }

    public void Damage(float amount)
    {
        if (mainEnemyScript != null)
        {
            float partDamage = mainEnemyScript.GetPartDamageAmount();
            mainEnemyScript.Damage(partDamage);
            hitsTaken++;
            CheckForDeath();
        }
        SetLockOnStatus(false);
    }

    private void CheckForDeath()
    {
        if (hitsTaken >= mainEnemyScript.hitsToKillPart)
        {
            Die();
        }
    }

    private void Die()
    {
        if (deathParticles != null)
        {
            deathParticles.GetFromPool(transform.position, Quaternion.identity);
        }

        if (mainEnemyScript != null)
        {
            float partDamage = mainEnemyScript.GetPartDamageAmount();
            mainEnemyScript.Damage(partDamage);
        }

        gameObject.SetActive(false);
    }

    public void SetLockOnStatus(bool status)
    {
        isLockedOn = status;
        UpdateLockOnVisuals();
    }

    private void UpdateLockOnVisuals()
    {
        if (lockOnAnim != null)
        {
            lockOnAnim.SetActive(isLockedOn);
        }

        if (!isLockedOn)
        {
            TriggerLockDisabledParticles();
        }
    }

    private void TriggerLockDisabledParticles()
    {
        if (lockOnDisabledParticles != null)
        {
            lockOnDisabledParticles.GetFromPool(transform.position, Quaternion.identity);
        }
    }

    public bool IsAlive()
    {
        return hitsTaken < mainEnemyScript.hitsToKillPart;
    }

    public bool IsLockedOn()
    {
        return isLockedOn;
    }
}