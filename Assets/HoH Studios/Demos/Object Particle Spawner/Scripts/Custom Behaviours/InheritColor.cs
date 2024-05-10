using UnityEngine;

namespace HohStudios.Tools.ObjectParticleSpawner.Demo
{
    /// <summary>
    /// Allows object particles to inherit the color of the particle system
    /// </summary>
    public class InheritColor : ObjectParticle
    {
        Material _objectMaterial;

        /// <summary>
        /// Gets called once the moment the object gets spawned
        /// </summary>
        public override void OnActivation(ObjectParticleSpawner.ParticleInfo particleInfo, ObjectParticleSpawner spawner)
        {
            // Use this override for attaching behavior the moment the particle is activated from the pool into the system
        }

        /// <summary>
        /// Gets called each frame that the object particle is alive and being controlled by the system
        /// </summary>
        public override void OnUpdate(ObjectParticleSpawner.ParticleInfo particleInfo, ObjectParticleSpawner spawner)
        {
            // We still want to inherit the particles movement, rotation, and scale in the base class function
            base.OnUpdate(particleInfo, spawner);

            // Get the material for color update
            if (_objectMaterial == null)
                _objectMaterial = GetComponent<MeshRenderer>().material;

            // Set the object's color to the current color of the particle this frame
            _objectMaterial.color = particleInfo.Particle.GetCurrentColor(spawner.ParticleSystem);
        }

        /// <summary>
        /// Gets called once when the particle is officially released from the system
        /// </summary>
        public override void OnRelease(ObjectParticleSpawner.ParticleInfo particleInfo, ObjectParticleSpawner spawner)
        {
            // Use this override for attaching behavior the moment the particle is released from the system
        }
    }
}