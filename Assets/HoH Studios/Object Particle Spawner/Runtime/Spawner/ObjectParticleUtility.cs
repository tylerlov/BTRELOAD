using System.Collections.Generic;
using HohStudios.Common.Attributes;
using UnityEngine;

namespace HohStudios.Tools.ObjectParticleSpawner
{
    /// <summary>
    /// The utility part of the ObjectParticleSpawner system, holding the data structs, public properties, and useful functions/fields
    /// </summary>
    public partial class ObjectParticleSpawner
    {

        #region Fields and Properties

        /// <summary>
        /// The cached particle system property of the spawner system
        /// </summary>
        public ParticleSystem ParticleSystem => _particleSystem;

        /// <summary>
        /// The handler for the object spawner events to return the object particle info data
        /// </summary>
        public delegate void ObjectSpawnerHandler(ObjectParticleInfo objectParticleInfo);

        /// <summary>
        /// Gets called when any game object is activated and spawned, still being controlled by the spawner system
        /// </summary>
        public ObjectSpawnerHandler OnSpawnEvent;

        /// <summary>
        /// Gets called when any game object is released from the system as an independant object, no longer being controlled by the system
        /// </summary>
        public ObjectSpawnerHandler OnReleaseEvent;

        /// <summary>
        /// The current number of living particles in the particle spawner
        /// </summary>
        public int NumberOfAliveParticles => _numberOfAliveParticles;

        /// <summary>
        /// The number of currently alive objects in the spawner
        /// </summary>
        public int NumberOfAliveObjects => _numberOfAliveObjects;

        /// <summary>
        /// Cache the particle spawner 'main module' for performance
        /// </summary>
        private ParticleSystem.MainModule _cachedSystemMain;
        public ParticleSystem.MainModule CachedSystemMainModule => _cachedSystemMain;

        /// <summary>
        /// Cache the particle spawner renderer for performance
        /// </summary>
        private ParticleSystemRenderer _cachedSystemRenderer;

        /// <summary>
        /// Cache the last particle spawner 'Simulation Space' for performance
        /// </summary>
        private ParticleSystemSimulationSpace _cachedSimulationSpace;
        public ParticleSystemSimulationSpace CachedSystemSimulationSpace => _cachedSimulationSpace;


        #endregion Fields and Properties




        /// <summary>
        /// Gets a COPY of all of the Alive Objects array and returns the number of currently alive objects
        /// </summary>
        /// <param name="allObjects"></param>
        /// <returns></returns>
        public int GetAliveObjects(out ObjectParticleInfo[] allObjects)
        {
            allObjects = (ObjectParticleInfo[])_aliveObjects.Clone();
            return _numberOfAliveObjects;
        }

        /// <summary>
        /// Gets a COPY of all of the Alive Particles array and returns the number of currently alive particles
        /// </summary>
        /// <param name="allParticles"></param>
        /// <returns></returns>
        public int GetAliveParticles(out ParticleSystem.Particle[] allParticles)
        {
            allParticles = (ParticleSystem.Particle[])_aliveParticles.Clone();
            return _numberOfAliveParticles;
        }

        /// <summary>
        /// Clears the particle array's tail
        /// </summary>
        private void ClearParticleArrayTail()
        {
            // Lets forcefully clear out old particles from the array's tail by setting all entries after number of alive to default
            var clearCounter = _numberOfAliveParticles;
            while (clearCounter < _aliveParticles.Length && _aliveParticles[clearCounter].remainingLifetime > 0)
            {
                _aliveParticles[clearCounter] = default;
                clearCounter++;
            }
        }

        /// <summary>
        /// Editor Convenience
        /// </summary>
        public void Reset()
        {
            ObjectPool.SpawnPoolOnAwake = true;
            ObjectPool.PoolSize = 20;
            ObjectPool.AllowAdaptivePool = true;
            ObjectPool.AdaptivePoolSpeed = 1;
            ObjectPool.AdaptivePoolPadding = 10;
            SystemSettings.RecycleOnRelease = false;
            SystemSettings.InheritMovement = true;
        }



        #region Data Structures



        /// <summary>
        /// This struct holds all of the settings used for the ObjectParticle and the ObjectParticleSpawner.
        /// </summary>
        [System.Serializable]
        public struct ObjectParticleSettings
        {
            /// <summary>
            /// Inherits the parent particle position and applies it to the target object
            /// </summary>
            [Tooltip("Inherits the parent particle position and applies it to the target object.")]
            public bool InheritMovement;

            /// <summary>
            /// Inherits the parent particle rotation and applies it to the target object
            /// </summary>
            [Tooltip("Inherits the parent particle rotation and applies it to the target object.")]
            public bool InheritRotation;

            /// <summary>
            /// Inherits the parent particle scale and applies it to the target object
            /// </summary>
            [Tooltip("Inherits the parent particle scale and applies it to the target object.")]
            public bool InheritScale;

            /// <summary>
            /// If true, calls OnFixedUpdate in addition to OnUpdate on the object particle components to be compatible with physics
            /// </summary>
            [Tooltip("If true, calls OnFixedUpdate in addition to OnUpdate on the object particle components to be compatible with physics.")]
            public bool CallFixedUpdate;

            /// <summary>
            /// Releases the object from the parent Object Particle System on registered OnCollisionEnter or OnCollisionEnter2D if true
            /// </summary>
            [Tooltip(
                "If true, releases the ObjectParticle from the system on valid OnCollisionEnter or OnCollisionEnter2D collision. (Only works when ObjectParticle component exists to listen for collisions. Has no effect on Collision module of the Particle System. Must be attached to collision object.)")]
            public bool ReleaseOnRigidbodyCollision;

            /// <summary>
            /// Layers to ignore collision for Release On Collision
            /// </summary>
            [Tooltip(
                "Collisions with these layers will be ignored in the Rigidbody collision. (Only works when ObjectParticle component exists to listen for collisions. Has no effect on Collision module of the Particle System)")]
            public LayerMask IgnoreLayers;

            /// <summary>
            /// Tag to ignore collision for Release On Collision
            /// </summary>
            [Tooltip(
                "Collisions with this tag will be ignored in the Rigidbody collision. (Only works when ObjectParticle component exists to listen for collisions. Has no effect on Collision module of the Particle System)")]
            [TagField]
            public string IgnoreTag;

            /// <summary>
            /// Either destroys the target object or returns the object to the object-pool on release from the parent Object Particle System. 
            /// </summary>
            [Tooltip(
                "Either destroys the target object or returns the object to the object-pool on release from the parent Object Particle System.")]
            public bool RecycleOnRelease;

            /// <summary>
            /// Destroys the ObjectParticle component on release if true
            /// </summary>
            [Tooltip("Destroys the ObjectParticle component (if it exists) on release if true")]
            public bool DestroyComponentOnRelease;

            /// <summary>
            /// The container object transform location in the heirarchy to parent the object to On-Release from ObjectParticleSpawner (for editor organization)
            /// </summary>
            [Tooltip("The container object transform location in the heirarchy to parent the object to On-Release from ObjectParticleSpawner (for editor organization)")]
            public Transform ReleaseContainer;
        }


        /// <summary>
        /// This exists as its own class wrapper so that we only need to copy each particle struct one time and can distribute it as a reference so avoid String.memcpy performance loss.
        /// The particle struct itself is large enough that it invokes String.memcpy each time it is copied which incurs massive cpu losses when spread over thousands of objects.
        /// Also, using this class as a reference we dont need to expose more than we need to
        /// </summary>
        public class ParticleInfo
        {
            /// <summary>
            /// The particle linked to the ObjectParticle, updated each frame to stay consistent
            /// </summary>
            public ParticleSystem.Particle Particle;
        }

        /// <summary>
        /// This class exists to hold some particle-pool info used to hold pool state in the Object Particle component
        /// </summary>
        public class ParticlePoolInfo
        {
            /// <summary>
            /// Returns true if this object was released from its particle spawner already, returns false if not released yet. Left as public field for performance
            /// </summary>
            public bool IsReleased;

            /// <summary>
            /// The target "instance ID" of the particle to keep track of, based on the particle's RandomSeed value. Left as public field for performance, DONT CHANGE THIS MANUALLY!
            /// </summary>
            public uint ParticleId;

            /// <summary>
            /// The spawn object ID associated with this Object, so we know which spawn object it was instantiated from, DONT CHANGE THIS MANUALLY!
            /// </summary>
            public int PoolId;

            public void UpdateInfo(bool isReleased, uint particleId, int poolId)
            {
                IsReleased = isReleased;
                ParticleId = particleId;
                PoolId = poolId;
            }
        }

        /// <summary>
        /// This struct exists for re-parenting objects on release to the release container in fixed update after the object was released and its info was cleared
        /// </summary>
        public struct ObjectParticleReleaseInfo
        {
            public Transform ObjectTransform;
            public Transform ReleaseContainer;
        }

        /// <summary>
        /// This class holds all of the target references needed by the Object Particle System,
        /// abstracted for allowing for Game Objects to work without an ObjectParticle component attached
        /// </summary>
        public class ObjectParticleInfo
        {
            /// <summary>
            /// The linked particle's references that holds particle information data
            /// </summary>
            public ParticleInfo ParticleInfo = new ParticleInfo();

            /// <summary>
            /// The information that links the particle to the pool information
            /// </summary>
            public ParticlePoolInfo PoolInfo = new ParticlePoolInfo();

            /// <summary>
            /// The ObjectParticle the target struct points toward
            /// </summary>
            public ObjectParticle ObjectParticle;

            /// <summary>
            /// The reference to the Target GameObject to syncronize with the actual particle
            /// </summary>
            public GameObject Object;

            /// <summary>
            /// The cached transform of the gameobject (for performance)
            /// </summary>
            public Transform ObjectTransform;

            /// <summary>
            /// Cached a bool knowing if we're an object particle to avoid null checks for performance. Left as public field for performance, DONT CHANGE THIS MANUALLY EVER!
            /// </summary>
            public bool IsObjectParticle;

            /// <summary>
            /// Quick way to know if the object info is null/empty and not in use without using a null check for performance
            /// </summary>
            public bool IsInitialized;

            /// <summary>
            /// Convenient function to set all of the references at once
            /// </summary>
            public void Initialize(ParticleSystem.Particle particle, ObjectParticle objectParticle, GameObject targetObject, int poolInstanceId, uint particleId)
            {
                IsInitialized = true;

                Object = targetObject;
                ObjectTransform = targetObject.transform;

                ParticleInfo.Particle = particle;
                PoolInfo.UpdateInfo(false, particleId, poolInstanceId);

                ObjectParticle = objectParticle;
                if (ObjectParticle)
                    IsObjectParticle = true;
            }

            public void Reset()
            {
                IsInitialized = false;
                Object = null;
                ObjectTransform = null;
                ObjectParticle = null;
                IsObjectParticle = false;
                ParticleInfo.Particle = default;
                PoolInfo.UpdateInfo(false, 0, 0);
            }
        }

        #endregion Data Structs

    }

    public static class ObjectParticleExtensions
    {
        /// <summary>
        /// Swaps two entries of a list or array by index
        /// </summary>
        public static void SwapEntries<T>(this IList<T> list, int first, int second)
        {
            if (list == null || list.Count < 2)
                return;

            T tmp = list[first];
            list[first] = list[second];
            list[second] = tmp;
        }

        /// <summary>
        /// Returns the "true" world velocity of the particle that is originally distorted by the particle system's modules.
        /// Does NOT guarantee correct velocity if using "Random between two constants" or "Random between to curves" 
        /// for the "Speed Modifier" of "VelocityOverLifetime" module, since it would be influenced by a randomly generated number.
        /// </summary>
        public static Vector3 GetWorldVelocity(this ObjectParticleSpawner.ParticleInfo info, ObjectParticleSpawner spawner)
        {
            var worldVelocity = Vector3.zero;
            var system = spawner.ParticleSystem;

            // Need to take into account the simulation space to get accurate velocity in world space
            if (spawner.CachedSystemSimulationSpace == ParticleSystemSimulationSpace.Local)
                worldVelocity = system.transform.TransformDirection(info.Particle.totalVelocity);
            else if (spawner.CachedSystemSimulationSpace == ParticleSystemSimulationSpace.World)
                worldVelocity = info.Particle.totalVelocity;
            else if (spawner.CachedSystemSimulationSpace == ParticleSystemSimulationSpace.Custom)
                worldVelocity = spawner.CachedSystemMainModule.customSimulationSpace != null ? spawner.CachedSystemMainModule.customSimulationSpace.TransformDirection(info.Particle.totalVelocity) : info.Particle.totalVelocity;

            // Simulation speed changes world velocity directly
            worldVelocity *= spawner.CachedSystemMainModule.simulationSpeed;

            // Velocity over lifetime Speed Modifier changes velocity directly (cannot account for random velocity modifier)
            var velocityOverLifetime = system.velocityOverLifetime;
            if (velocityOverLifetime.enabled)
                worldVelocity *= velocityOverLifetime.speedModifier.Evaluate((1f - info.Particle.remainingLifetime) / info.Particle.startLifetime);

            return worldVelocity;
        }

        /// <summary>
        /// Returns the world space position of the particle taking into account simulation space
        /// </summary>
        public static Vector3 GetWorldPosition(this ObjectParticleSpawner.ParticleInfo info, ObjectParticleSpawner spawner)
        {
            if (spawner.CachedSystemSimulationSpace == ParticleSystemSimulationSpace.Local)
                return spawner.ParticleSystem.transform.TransformPoint(info.Particle.position);
            else if (spawner.CachedSystemSimulationSpace == ParticleSystemSimulationSpace.World)
                return info.Particle.position;
            else
                return spawner.CachedSystemMainModule.customSimulationSpace != null ? spawner.CachedSystemMainModule.customSimulationSpace.TransformPoint(info.Particle.position) : info.Particle.position;
        }
    }
}