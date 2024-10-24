using HohStudios.Common.Attributes;
using UnityEngine;

namespace HohStudios.Tools.ObjectParticleSpawner
{
    /// <summary>
    /// Rigidbody2D support for object particles to allow the Rigidbody2D to seamlessly inherit the velocity and rotational trajectory
    /// of the particle as it's released from the system to be its own independent object. It utilizes the OnFixedUpdate functionality
    /// of the object particle spawner to illustrate physics based behaviour.
    /// 
    /// Attach this to a Rigidbody2D gameobject you want to spawn with the ObjectParticleSpawner to have the objects follow the particle's trajectory.
    /// 
    /// The object particle class that is inherited is used to interface with the ObjectParticleSpawner. It allows more fine-tune behavior control of the
    /// objects spawned by the system. You can listen for events or trigger events, or even inherit from this script to apply custom update or event behavior.
    /// 
    /// NOTE:
    ///     Does NOT guarantee correct velocity if using "Random between two constants" or "Random between to curves" 
    ///     for the "Speed Modifier" of "VelocityOverLifetime" module, since it would be influenced by a randomly generated number that is not able to be accounted for.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ObjectParticleRigidbody2D : ObjectParticle
    {
        /// <summary>
        /// The rigidbody reference
        /// </summary>
        [InfoField("Make sure 'CallFixedUpdate' is enabled in the object or system, and 'Interpolate/Extrapolate' is used on the Rigidbody2D for smooth physics.")]
        [SerializeField]
        private Rigidbody2D _rigidbody2D;

        /// <summary>
        /// Cached variables OnEnable() that improve performance but may not be desirable depending on your needs
        /// </summary>
        private static Vector2 _physicsGravity;
        private float _rigidbodyMass;
        private float _gravityScale;

        private Vector3 _prevPosition;
        private Vector3 _prevRotation;

        protected override void Awake()
        {
            base.Awake();
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            // Cache variables for performance OnEnable()
            _physicsGravity = Physics2D.gravity;
            _gravityScale = _rigidbody2D.gravityScale;
            _rigidbodyMass = _rigidbody2D.mass;
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
                    var gravityVelocity = _physicsGravity * _gravityScale * deltaTime;
                    _rigidbody2D.AddForce((_rigidbodyMass *
                                           ((Vector2)particleInfo.GetWorldVelocity(spawner) -
                                            (_rigidbody2D.linearVelocity + gravityVelocity))) / deltaTime);
                }

                else // If the particle is not being updated, freeze the objects pos
                    _rigidbody2D.MovePosition(particleInfo.GetWorldPosition(spawner));
            }

            // Inherit rotation by setting the rigidbody's angular velocity to the particle's
            if (FinalSettings.InheritRotation)
            {
                if (particleInfo.Particle.rotation3D != _prevRotation)
                    _rigidbody2D.angularVelocity = particleInfo.Particle.angularVelocity * Mathf.Deg2Rad;

                else // If the particle is not being updated, freeze the objects rotation
                    _rigidbody2D.MoveRotation(Quaternion.Euler(particleInfo.Particle.rotation3D).z);
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
