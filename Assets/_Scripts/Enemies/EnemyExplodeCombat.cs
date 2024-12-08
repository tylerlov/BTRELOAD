using UnityEngine;
using FMODUnity;

[RequireComponent(typeof(EnemyExplodeBasics))]
[RequireComponent(typeof(EnemyExplodeAI))]
public class EnemyExplodeCombat : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionDamage = 50f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private LayerMask explosionLayerMask;

    private EnemyExplodeBasics explodeBasics;
    private EnemyBasics basics;
    private bool hasExploded = false;

    private void Awake()
    {
        explodeBasics = GetComponent<EnemyExplodeBasics>();
        basics = GetComponent<EnemyBasics>();
    }

    public void TryExplode()
    {
        if (hasExploded || !basics.IsAlive) return;
        
        Explode();
    }

    private void Explode()
    {
        hasExploded = true;

        // Deal damage to nearby objects
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, explosionLayerMask);
        foreach (var hitCollider in hitColliders)
        {
            // Try to get any component that has TakeDamage method
            var damageableObject = hitCollider.GetComponent<EnemyBasics>();
            if (damageableObject != null)
            {
                Vector3 direction = (hitCollider.transform.position - transform.position).normalized;
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius);
                float finalDamage = explosionDamage * damageMultiplier;

                damageableObject.TakeDamage(finalDamage);
            }
        }

        // Notify basics for effects
        explodeBasics.SendMessage("OnExploded", SendMessageOptions.DontRequireReceiver);

        // Destroy or deactivate the enemy
        if (gameObject.activeInHierarchy)
        {
            explodeBasics.TriggerDeath();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
