using System;
using Typooling;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyBasicDamagablePart : MonoBehaviour, IDamageable
{
    public EnemyBasics mainEnemyScript;

    [SerializeField]
    private GameObject lockOnAnim;

    [SerializeField]
    private Pooler lockOnDisabledParticles;

    [SerializeField]
    private Pooler deathParticles;

    private int hitsTaken = 0;
    private Collider partCollider;
    private bool isLockedOn = false;

    public event Action OnPartDestroyed;

    private void Awake()
    {
        partCollider = GetComponent<Collider>();
        partCollider.isTrigger = true;
    }

    public void Damage(float amount)
    {
        if (mainEnemyScript != null)
        {
            mainEnemyScript.Damage(amount);
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
        OnPartDestroyed?.Invoke();
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

    public void SetLockedOnIndicator(bool status)
    {
        isLockedOn = status;
        if (lockOnAnim != null)
        {
            lockOnAnim.SetActive(status);
        }

        if (!status && lockOnDisabledParticles != null)
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
