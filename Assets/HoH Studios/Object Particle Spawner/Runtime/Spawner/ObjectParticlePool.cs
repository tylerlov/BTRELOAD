using HohStudios.Common;
using UnityEngine;

namespace HohStudios.Tools.ObjectParticleSpawner
{
    /// <summary>
    /// The object pool adapter layer of the object particle system. Handles interfacing with the object pool.
    /// </summary>
    public partial class ObjectParticleSpawner
    {
        /// <summary>
        /// The object pool of the spawner system
        /// </summary>
        public ObjectPool ObjectPool = new ObjectPool();


        private void Awake()
        {
            if (_particleSystem == null)
                _particleSystem = GetComponent<ParticleSystem>();

            // Spawn the initial object pool if desired on awake and begin grooming the pool
            ObjectPool.Awaken(this);
        }


        /// <summary>
        /// Gets or Spawns an available pool object and activates it to be used in the system. Returns true if successful, false otherwise
        /// </summary>
        private bool ActivateObjectFromPool(int particleIndex)
        {
            if (_numberOfAliveObjects >= _aliveObjects.Length)
                return false;

            // Get or spawn an available pool object, removes it from the pool if found one that was already spawned
            var activated = ObjectPool.ActivateObject(out var activatedObject);

            if (!activated)
                return false;

            var particleId = _aliveParticles[particleIndex].randomSeed;

            // Check to see if the newly spawned object contains an ObjectParticle component
            var objectParticle = activatedObject.Object.GetComponentInChildren<ObjectParticle>();

            // Create (or re-use) the info data and set all of the initial values and references needed in the system
            var info = _aliveObjects[_numberOfAliveObjects] ?? new ObjectParticleInfo();
            info.Initialize(_aliveParticles[particleIndex], objectParticle, activatedObject.Object, activatedObject.PoolId, particleId);

            // If the object has the ObjectParticle component, initialize it and trigger OnActivation events
            if (info.IsObjectParticle)
            {
                info.ObjectParticle.Initialize(this, info.ObjectParticle.OverrideSystemSettings ? info.ObjectParticle.ObjectSettings : SystemSettings);
                info.ObjectParticle.Info.UpdateInfo(false, particleId, activatedObject.PoolId);
                info.ObjectParticle.OnActivation(info.ParticleInfo, this);
                info.ObjectParticle.OnActivationEvent?.Invoke(info.ParticleInfo, this);
            }

            // Place the object to activate in the alive objects array
            _aliveObjects[_numberOfAliveObjects] = info;

            // Jump transform to particle on activation and update the object
            info.ObjectTransform.position = info.ParticleInfo.GetWorldPosition(this);
            UpdateObject(_numberOfAliveObjects, particleIndex);

            // Increment our null index placeholder for the aliveobjects array
            if (_numberOfAliveObjects < _aliveObjects.Length)
                _numberOfAliveObjects++;

            OnSpawnEvent?.Invoke(info);
            return true;
        }

        /// <summary>
        /// Deactivates an object to be once again available in the object pool
        /// </summary>
        public void ReturnObjectToPool(ParticlePoolInfo poolInfo, Transform objToDeactivate)
        {
            ObjectPool.DeactivateObject(objToDeactivate.gameObject, poolInfo.PoolId);
            poolInfo.UpdateInfo(false, 0, poolInfo.PoolId);
        }
    }
}
