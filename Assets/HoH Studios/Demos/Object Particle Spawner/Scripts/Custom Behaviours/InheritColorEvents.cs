using UnityEngine;

namespace HohStudios.Tools.ObjectParticleSpawner.Demo
{
    /// <summary>
    /// Allows object particles to inherit the color of the particle system by subscribing to the events on the objecct particle
    /// </summary>
    public class InheritColorEvents : MonoBehaviour
    {
        ObjectParticle _objectParticle;
        Material _objectMaterial;

        private void Awake()
        {
            // Get the reference to the object particle component
            _objectParticle = GetComponent<ObjectParticle>();

            // Subscribe to the public events of the Object Particle
            _objectParticle.OnActivationEvent += ObjectActivated;
            _objectParticle.OnUpdateEvent += ObjectUpdated;
            _objectParticle.OnReleaseEvent += ObjectReleased;
        }

        /// <summary>
        /// Invoked when the object is activated from the object pool
        /// </summary>
        private void ObjectActivated(ObjectParticleSpawner.ParticleInfo particleInfo, ObjectParticleSpawner spawner)
        {
            // Add initial behaviour here
        }

        /// <summary>
        /// Invoked when the object is updated and controlled each frame from the Object Particle Spawner
        /// </summary>
        private void ObjectUpdated(ObjectParticleSpawner.ParticleInfo particleInfo, ObjectParticleSpawner spawner)
        {
            // Get the material for color update
            if (_objectMaterial == null)
                _objectMaterial = GetComponent<MeshRenderer>().material;

            // Set the object's color to the current color of the particle this frame
            _objectMaterial.color = particleInfo.Particle.GetCurrentColor(spawner.ParticleSystem);
        }

        /// <summary>
        /// Invoked when the object is released from the Object Particle Spawner and can act as an independent object
        /// </summary>
        private void ObjectReleased(ObjectParticleSpawner.ParticleInfo particleInfo, ObjectParticleSpawner spawner)
        {
            // Informs the custom behaviour when the object is now released and independent 
        }
    }
}
