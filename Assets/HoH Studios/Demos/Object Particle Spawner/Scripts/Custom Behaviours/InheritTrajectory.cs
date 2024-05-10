using UnityEngine;

namespace HohStudios.Tools.ObjectParticleSpawner.Demo
{
    /// <summary>
    /// A small example demonstrating how to continue the object's movement and rotational trajectory after release, indefinitely, without Rigidbodies.
    /// 
    /// Meant to be used as reference or template for creating your own custom behaviours.
    /// </summary>
    public class InheritTrajectory : ObjectParticle
    {
        // Establish logic variables
        private bool _startCustomBehaviour = false;
        private Vector3 _releaseVelocity;
        private Vector3 _releaseAngularVelocity;

        /// <summary>
        /// Begin custom trajectory behaviour once the object is released by setting the logic variables as needed
        /// </summary>
        public override void OnRelease(ObjectParticleSpawner.ParticleInfo particleInfo, ObjectParticleSpawner spawner)
        {
            // Get the movement velocity on release
            _releaseVelocity = particleInfo.GetWorldVelocity(spawner);

            // Get the rotational angular velocity on release
            _releaseAngularVelocity = particleInfo.Particle.angularVelocity3D;

            // Start the custom trajectory inside of update
            _startCustomBehaviour = true;
        }

        /// <summary>
        /// Use update to move the transform position and rotation
        /// </summary>
        private void Update()
        {
            // Don't do anything until the object is released
            if (!_startCustomBehaviour)
                return;

            var deltaTime = Time.deltaTime;

            // Translate and rotate the object to continue its trajectory as defined by OnRelease()
            transform.Translate(_releaseVelocity * deltaTime, Space.World);
            transform.Rotate(_releaseAngularVelocity * deltaTime, Space.Self);
        }
    }
}