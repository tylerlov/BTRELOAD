using UnityEngine;

namespace HohStudios.Tools.ObjectParticleSpawner.Demo
{
    /// <summary>
    /// 
    /// This script exists with the intention of Recycling object particle system
    /// objects OnTriggerEnter.
    /// 
    /// Attach it to a trigger collider with the intent of recycling any rigidbody object perticles that enter it.
    /// 
    /// IMPORTANT:
    ///     The objects entering the trigger must have the Rigidbody, Collider, and ObjectParticle components attached to work
    /// 
    /// </summary>
    public class RecycleOnTrigger : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Recycle on trigger collision registered
            var objParticle = collision.GetComponent<ObjectParticle>();
            objParticle?.Recycle();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Recycle on trigger collision registered
            var objParticle = other.GetComponent<ObjectParticle>();
            objParticle?.Recycle();
        }
    }
}