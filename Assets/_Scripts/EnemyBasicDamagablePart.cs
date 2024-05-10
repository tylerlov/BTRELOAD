using UnityEngine;
using OccaSoftware.BOP;


public class EnemyBasicDamagablePart : MonoBehaviour
{
    public EnemyBasicSetup mainEnemyScript;
    [SerializeField] private GameObject lockOnAnim; // Reference to the lock-on animation GameObject
    [SerializeField] private Pooler lockOnDisabledParticles; // Reference to the Pooler for the particle effect
    [SerializeField] private Pooler deathParticles; // Reference to the Pooler for the death particle effect

    private int hitsTaken = 0; // Track the number of hits taken

    public void TakeDamage(float damage)
    {
        if (mainEnemyScript != null)
        {
            mainEnemyScript.Damage(damage);
            hitsTaken++;
            CheckForDeath();
        }
        DisableLockOn(); // Disable lock-on when taking damage
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
        gameObject.SetActive(false); // Make the GameObject inactive
    }

    public void EnableLockOn()
    {
        if (lockOnAnim != null)
        {
            lockOnAnim.SetActive(true);
        }
    }

    public void DisableLockOn()
    {
        if (lockOnAnim != null)
        {
            lockOnAnim.SetActive(false);
        }
        TriggerLockDisabledParticles();
    }

    private void TriggerLockDisabledParticles()
    {
        if (lockOnDisabledParticles != null)
        {
            lockOnDisabledParticles.GetFromPool(transform.position, Quaternion.identity);
        }
    }
}