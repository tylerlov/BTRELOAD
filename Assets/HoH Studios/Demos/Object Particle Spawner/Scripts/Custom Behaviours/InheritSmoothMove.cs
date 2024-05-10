using UnityEngine;

namespace HohStudios.Tools.ObjectParticleSpawner.Demo
{
    /// <summary>
    /// Allows the spawned object to move with a smoothing sharpness factor toward the particles position for smooth movement.
    /// 
    /// NOTE:
    ///     This script is not optimized for Rigidbodies.
    ///     It causes jittery movement when attached to a rigidbody that uses gravity and does not clear its velocity when recycled in the pool.
    ///     This is because the gravity will accrue and increase overtime that the smoothing function cannot keep up with the rigidbody's velocity.
    ///     
    ///     To avoid this issue, it is recommended to either 
    ///         1. Reset/clear the rigidbody's velocity OnActivation()
    ///         2. Smooth the rigidbody velocity directly or with addforce instead of setting position directly
    /// 
    /// </summary>
    public class InheritSmoothMove : ObjectParticle
    {
        [Range(1f, 20f)]
        [SerializeField]
        private float _sharpness = 5f;

        /// <summary>
        /// Smooths the default inherited movement over an easing function with a given sharpness
        /// </summary>
        public override void OnUpdate(ObjectParticleSpawner.ParticleInfo particleInfo, ObjectParticleSpawner spawner)
        {
            // Cache the starting position before movement is applied
            var startPos = transform.position;

            // Default inherit movement
            base.OnUpdate(particleInfo, spawner);

            // Get the final desired position
            var newPos = transform.position;

            // Smooth move from start position to new position over time with a sharpness factor
            transform.position = SmoothMove(startPos, newPos, _sharpness, Time.deltaTime);
        }

        /// <summary>
        /// The function below is a frame-independent smooth movement from a value to another value over a smoothing sharpness
        /// </summary>
        public static Vector3 SmoothMove(Vector3 from, Vector3 to, float sharpness, float deltaTime)
        {
            return UnityEngine.Vector3.Lerp(from, to, 1 - Mathf.Exp(-sharpness * deltaTime));
        }
    }
}