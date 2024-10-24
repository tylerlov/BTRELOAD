using HohStudios.Common.Attributes;
using UnityEngine;

namespace HohStudios.Tools.ObjectParticleSpawner
{
    /// <summary>
    /// Rigidbody support for object particles to allow the rigidbody to seamlessly inherit the velocity and rotational trajectory
    /// of the particle as it's released from the system to be its own independent object. It utilizes the OnFixedUpdate functionality
    /// of the object particle spawner to illustrate physics based behaviour.
    /// 
    /// Attach this to a rigidbody gameobject you want to spawn with the ObjectParticleSpawner to have the objects follow the particle's trajectory.
    /// Be sure to use Interpolation/Extrapolation on the rigidbody settings for smooth movement.
    /// 
    /// The object particle class that is inherited is used to interface with the ObjectParticleSpawner. It allows more fine-tune behavior control of the
    /// objects spawned by the system. You can listen for events or trigger events, or even inherit from this script to apply custom update or event behavior.
    /// 
    /// NOTE:
    ///     Does NOT guarantee correct velocity if using "Random between two constants" or "Random between to curves" 
    ///     for the "Speed Modifier" of "VelocityOverLifetime" module, since it would be influenced by a randomly generated number that is not able to be accounted for.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ObjectParticleRigidbody : ObjectParticle
    {
        /// <summary>
        /// The rigidbody reference
        /// </summary>
        [InfoField("Make sure 'CallFixedUpdate' is enabled in the object or system, and 'Interpolate/Extrapolate' is used on the rigidbody for smooth physics.")]
        [SerializeField]
        private Rigidbody _rigidbody;

        /// <summary>
        /// Cached variables OnEnable() that improve performance but may not be desirable depending on your needs
        /// </summary>
        private static Vector3 _physicsGravity;
        private float _rigidbodyMass;
        private bool _usingGravity;

        private Vector3 _prevPosition;
        private Vector3 _prevRotation;

        protected override void Awake()
        {
            base.Awake();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            // Cache variables for performance OnEnable()
            _physicsGravity = Physics.gravity;
            _usingGravity = _rigidbody.useGravity;
            _rigidbodyMass = _rigidbody.mass;
        }

        /// <summary>
        /// Handles moving and rotating the rigidbody on fixed update via rigidbody physics
        /// </summary>
        public override void OnFixedUpdate(ObjectParticleSpawner.ParticleInfo particleInfo, ObjectParticleSpawner spawner)
        {
            // Get the current velocity of the gravity being applied to the object if using gravity
            var deltaTime = Time.fixedDeltaTime;

            // Inherit movement by using AddForce to keep the rigidbody's velocity the same as the particle's world velocity, taking gravity into account
            if (FinalSettings.InheritMovement)
            {
                if (particleInfo.Particle.position != _prevPosition)
                {
                    var gravityVelocity = _usingGravity ? _physicsGravity * deltaTime : Vector3.zero;
                    _rigidbody.AddForce((_rigidbodyMass * (particleInfo.GetWorldVelocity(spawner)) -
                                         (_rigidbody.linearVelocity + gravityVelocity)) / deltaTime);
                }

                else // If the particle is not being updated, freeze the objects pos
                    _rigidbody.MovePosition(particleInfo.GetWorldPosition(spawner));
            }

            // Inherit rotation by setting the rigidbody's angular velocity to the particle's
            if (FinalSettings.InheritRotation)
            {
                if (particleInfo.Particle.rotation3D != _prevRotation)
                    _rigidbody.angularVelocity = particleInfo.Particle.angularVelocity3D * Mathf.Deg2Rad;

                else // If the particle is not being updated, freeze the objects rotation
                    _rigidbody.MoveRotation(Quaternion.Euler(particleInfo.Particle.rotation3D));
            }

            // Use prev pos and rotation to hold its state in case the particle system is being culled we dont want to apply any behaviour
            _prevPosition = particleInfo.Particle.position;
            _prevRotation = particleInfo.Particle.rotation3D;
        }

        /// <summary>
        /// We still want the option to inherit scale in regular OnUpdate so it smoothly changes scale if desired
        /// </summary>
        public override void OnUpdate(ObjectParticleSpawner.ParticleInfo particleInfo, ObjectParticleSpawner spawner)
        {
            spawner.ApplyDefaultScaling(particleInfo, _transform, FinalSettings);
        }


        /// <summary>
        /// Editor Convenience
        /// </summary>
        public override void Reset()
        {
            OverrideSystemSettings = true;
            ObjectSettings.CallFixedUpdate = true;
            base.Reset();
        }
    }
}