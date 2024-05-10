using UnityEngine;

namespace HohStudios.Tools.ObjectParticleSpawner
{
    /// <summary>
    /// The object particle class is used to interface with the ObjectParticleSpawner. It allows more fine-tune behavior control of the
    /// objects spawned by the system. You can listen for events or trigger events, or even inherit from this script to apply custom update or event behavior.
    /// Attach this to a gameobject you want to spawn with the ObjectParticleSpawner system to override settings and functionality.
    /// </summary>
    public class ObjectParticle : MonoBehaviour
    {

        #region Fields and Properties

        /// <summary>
        /// The cached transform of this object particle
        /// </summary>
        protected Transform _transform;

        ///--------------------------------- PUBLIC FIELDS AND PROPERTIES ---------------------------------///

        /// <summary>
        /// Uses the parent 'Object Particle System' settings if false, uses override settings if true. 
        /// </summary>
        [Tooltip("Allows this ObjectParticle to have its own instanced settings, overriding the spawner's settings.")]
        [HideInInspector]
        public bool OverrideSystemSettings;

        /// <summary>
        /// The settings for this Object Particle, only used if OverridingSystemSettings = true
        /// </summary>
        [HideInInspector]
        public ObjectParticleSpawner.ObjectParticleSettings ObjectSettings;

        /// <summary>
        /// The final settings that is either the system's settings or the object's settings
        /// </summary>
        [HideInInspector]
        public ObjectParticleSpawner.ObjectParticleSettings FinalSettings;

        /// <summary>
        /// The reference to the parent spawner, set by the spawner when this object is spawned. 
        /// </summary>
        public ObjectParticleSpawner ParentSpawner { get; private set; }

        /// <summary>
        /// The most recent particle-pool information that is updated as the object lives its life cycle
        /// </summary>
        public ObjectParticleSpawner.ParticlePoolInfo Info { get; private set; } = new ObjectParticleSpawner.ParticlePoolInfo();


        ///--------------------------------------- PUBLIC EVENTS --------------------------------------///

        /// <summary>
        /// The handler for the object particle events to return both the spawner and the particle
        /// </summary>
        public delegate void ObjectParticleHandler(ObjectParticleSpawner.ParticleInfo info, ObjectParticleSpawner spawner);

        /// <summary>
        /// An event handler that invokes its subscribers when the Object Particle is activated from its parent object pool
        /// </summary>
        public ObjectParticleHandler OnActivationEvent;

        /// <summary>
        /// An event handler that invokes its subscribers when the Object Particle is updated and controlled each frame by the Object Particle System
        /// </summary>
        public ObjectParticleHandler OnUpdateEvent;

        /// <summary>
        /// An event handler that invokes its subscribers when the Object Particle is updated on fixed update and controlled each frame by the Object Particle System
        /// </summary>
        public ObjectParticleHandler OnFixedUpdateEvent;

        /// <summary>
        /// An event handler that invokes its subscribers when the Object Particle is released from its parent Object Particle System.
        /// </summary>
        public ObjectParticleHandler OnReleaseEvent;


        #endregion Fields and Properties



        #region Functions

        protected ObjectParticle() { Reset(); }

        protected virtual void Awake()
        {
            _transform = transform;
            FinalSettings = ObjectSettings;
        }

        /// <summary>
        /// Releases the Object Particle from its parent Object Particle System early. Returns true if successful, false otherwise
        /// </summary>
        public bool Release()
        {
            if (Info.IsReleased || !ParentSpawner) return false;
            return ParentSpawner.ReleaseEarly(Info.ParticleId);
        }

        /// <summary>
        /// Allows for manual override to add custom behavior on object pool activation from parent ObjectParticleSpawner
        /// </summary>
        public virtual void OnActivation(ObjectParticleSpawner.ParticleInfo particleInfo, ObjectParticleSpawner spawner) { }

        /// <summary>
        /// Allows for manual override of the Object's Behavior, called on late update. Call base function to apply default behaviour,
        /// or call ApplyDefaultBehaviour functions in the object particle spawner to apply modular behaviour.
        /// </summary>
        public virtual void OnUpdate(ObjectParticleSpawner.ParticleInfo particleInfo, ObjectParticleSpawner spawner)
        {
            spawner.ApplyDefaultMovement(particleInfo, _transform, FinalSettings);
            spawner.ApplyDefaultRotation(particleInfo, _transform, FinalSettings);
            spawner.ApplyDefaultScaling(particleInfo, _transform, FinalSettings);
        }

        /// <summary>
        /// Allows for manual physics override of the Object's Behavior, called on fixed update. No base behaviour supplied.
        /// </summary>
        public virtual void OnFixedUpdate(ObjectParticleSpawner.ParticleInfo particleInfo, ObjectParticleSpawner spawner) { }

        /// <summary>
        /// Allows for manual override to add custom behavior on object particle released from parent ObjectParticleSpawner
        /// </summary>
        public virtual void OnRelease(ObjectParticleSpawner.ParticleInfo particleInfo, ObjectParticleSpawner spawner) { }

        /// <summary>
        /// Returns the ObjectParticle to the object pool if the object pool existed and initialization references were made
        /// </summary>
        public void Recycle()
        {
            if (!ParentSpawner)
                return;

            //// Release the object and its particle if not done already
            if (!Info.IsReleased)
                ParentSpawner.ReleaseEarly(Info.ParticleId);

            // Force return to pool if it wasn't going to be returned on Release() based on Final Settings
            if (!FinalSettings.RecycleOnRelease)
                ParentSpawner.ReturnObjectToPool(Info, _transform);
        }


        /// <summary>
        /// Initialize the object particle to establish key references, called by the object particle spawner when the object particle is spawned
        /// </summary>
        public void Initialize(ObjectParticleSpawner parentSpawner, ObjectParticleSpawner.ObjectParticleSettings finalSettings)
        {
            ParentSpawner = parentSpawner;
            FinalSettings = finalSettings;
        }

        /// <summary>
        /// Releases on collision when flagged
        /// </summary>
        protected virtual void OnCollisionEnter(Collision collision)
        {
            // Checks if we're releasing on collision, and if so, checks that the we're not ignoring the collision layer and/or tag
            if (!Info.IsReleased && FinalSettings.ReleaseOnRigidbodyCollision &&
                (FinalSettings.IgnoreLayers != (FinalSettings.IgnoreLayers | (1 << collision.gameObject.layer))) &&
                (string.IsNullOrEmpty(FinalSettings.IgnoreTag) || !collision.gameObject.CompareTag(FinalSettings.IgnoreTag)))
            {
                Release();
            }
        }

        /// <summary>
        /// Releases on collision 2D when flagged
        /// </summary>
        public virtual void OnCollisionEnter2D(Collision2D collision)
        {
            // Checks if we're releasing on collision, and if so, checks that the we're not ignoring the collision layer and/or tag
            if (!Info.IsReleased && FinalSettings.ReleaseOnRigidbodyCollision &&
                (FinalSettings.IgnoreLayers != (FinalSettings.IgnoreLayers | (1 << collision.gameObject.layer))) &&
                (string.IsNullOrEmpty(FinalSettings.IgnoreTag) || !collision.gameObject.CompareTag(FinalSettings.IgnoreTag)))
            {
                Release();
            }
        }

        /// <summary>
        /// Editor Convenience
        /// </summary>
        public virtual void Reset()
        {
            ObjectSettings.RecycleOnRelease = false;
            ObjectSettings.InheritMovement = true;
        }


        #endregion Functions
    }
}
