using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HohStudios.Common.Math;
using UnityEngine;

namespace HohStudios.Common
{
    /// <summary>
    /// This object pooling kit allows for easy implementation of object pooling game objects for any utilization. It contains a weighted-random spawn system
    /// built in, as well as events to subscribe to add behavior to the pool objects along their lifetime. The pool automatically grooms itself and allows further
    /// adaptive customizations. This allows you to control the memory usage to the fullest and keep performance high. 
    /// Partial class exists also in ObjectPoolUtility.cs for all of the utility functions.
    /// 
    /// How To Use:
    ///     1. Add weighted spawn objects to the spawn object list using the utility functions (many utility functions provided)
    ///     2. Use the ActivateObject() and DeactivateObject() overloaded function calls to spawn/deactivate/activate pool objects 
    ///     3. Subscribe to events to add object behavior at key points in the pool object's lifetime (i.e. OnSpawn/OnActivation/OnDeactivation etc)
    /// 
    /// You can use the object pool as a component attached to an object, or instantiated and used in another class.
    /// </summary>
    [System.Serializable]
#if UNITY_EDITOR
    [UnityEditor.CanEditMultipleObjects]
#endif
    public sealed partial class ObjectPool
    {

        #region Fields and Properties


        /// ______________________ FIELDS AND PROPERTIES ______________________///

        /// <summary>
        /// The hierarchy container for the pool objects, defaulting to this object if empty
        /// </summary>
        [Tooltip("The game object in the hierarchy to act as the container for the pool game objects.")]
        public Transform PoolContainer;

        /// <summary>
        /// When enabled, spawns the entire pool instantly in one frame on awake
        /// </summary>
        [Tooltip("Spawns the entire pool instantly in one frame on awake when Object Pooling is activated, rather than over several frames.")]
        public bool SpawnPoolOnAwake = true;

        /// <summary>
        /// The object pool base size, spawned either on awake or over several frames
        /// </summary>
        [Tooltip("The object pool base size.")]
        public int PoolSize = 20;

        /// <summary>
        /// Allows the object pool to grow and shrink and adapt with the supply and demand of the system
        /// </summary>
        [Tooltip("Allows the object pool to grow and shrink and adapt with the supply and demand of the system.")]
        public bool AllowAdaptivePool = true;

        /// <summary>
        /// The quantity of pool objects to always have available at the system's current supply and demand for adaptive pool
        /// </summary>
        [Tooltip("The quantity of pool objects to always strive to have available at the system's current supply and demand. " +
                 "May need to increase adaptive pool speed if it isn't reaching pool demands.")]
        [HideInInspector]
        public int AdaptivePoolPadding = 10;

        /// <summary>
        /// How quickly the adaptive pool grows and shrinks with the supply and demand of the system
        /// </summary>
        [Tooltip("How quickly the adaptive pool grows and shrinks with the supply and demand of the system.")]
        [HideInInspector]
        public int AdaptivePoolSpeed = 2;

        /// <summary>
        /// The seconds delay to wait before removing any excess objects from the pool in case of volatile supply/demands
        /// </summary>
        [Tooltip("The seconds delay to wait before removing any excess objects from the pool in case of volatile supply/demands.")]
        [HideInInspector]
        public float AdaptiveShrinkDelay = 3f;

        /// <summary>
        /// True if the pool is currently awake and actively being managed, false if asleep and unresponsive
        /// </summary>
        [SerializeField]
        private bool _isAwake;
        public bool IsAwake => _isAwake;

        /// <summary>
        /// The instance to run coroutines on, so we can use pooling both as a component and as a constructor
        /// </summary>
        private MonoBehaviour _monoInstance;

        /// <summary>
        /// Keeps track of how many 'base' objects were spawned so we can keep up with the object pool size increasing, and spawn over several frames instead of all at once if desired
        /// </summary>
        private int _baseSpawnedCounter;


        /// ______________________ MAIN COLLECTIONS ______________________///

        /// <summary>
        /// The list of weighted objects to spawn. Each weighted choice contains a game object and a weight for the weighted random functionality
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private List<PoolSpawnObject> _objectsToSpawn = new List<PoolSpawnObject>();
        /// <summary>
        /// The Weighted Random selector, cached and used to make the weighted selection choice
        /// </summary>
        private readonly WeightedRandom<GameObject> _weightedSelector = new WeightedRandom<GameObject>();
        /// <summary>
        /// The dictionary holding all of the available objects in the pool, sorted by the 'Unique instance ID' of the spawn object acting as the key for each bucket of objects. Automatically managed.
        /// </summary>
        private readonly Dictionary<int, List<PoolObject>> _poolsByInstanceId = new Dictionary<int, List<PoolObject>>();


        /// ______________________ BEHAVIOR EVENTS ______________________///

        /// <summary>
        /// Event to subscribe to add behavior to an object when it is first spawned. Only invoked once in the lifetime of the object.
        /// </summary>
        public event Action<PoolObject> OnSpawnedEvent;
        /// <summary>
        /// Event to subscribe to add behavior to the object when it is activated and released from the pool. Invoked every time its activated and released from pool.
        /// </summary>
        public event Action<PoolObject> OnActivatedEvent;
        /// <summary>
        /// Event to subscribe to add behavior to the object when it is deactivated and added to the pool. Invoked OnSpawn and every time its returned to the pool.
        /// </summary>
        public event Action<PoolObject> OnDeactivatedEvent;
        /// <summary>
        /// Event to subscribe to add behavior to the object right before its destroyed. Invoked once when pool discards the object completely.
        /// </summary>
        public event Action<PoolObject> OnDestroyedEvent;



        #endregion Fields and Properties


        #region Main Object Pool Functions

        /// ______________________ CONSTRUCTORS AND SETUP ______________________///

        /// <summary>
        /// Creates the object pool. Need to call Awaken() to activate the system.
        /// </summary>
        public ObjectPool() { }

        /// <summary>
        /// Creates an object pool, using the MonoBehavior passed in to run coroutines and manage collections.
        /// Automatically awakens the object pool and allows for optionally spawning on awake.
        /// </summary>
        public ObjectPool(Transform poolContainer, List<PoolSpawnObject> spawnChoices)
        {
            PoolContainer = poolContainer;

            if (spawnChoices != null)
                _objectsToSpawn = spawnChoices;

            // Groom the spawn objects to assert validity
            GroomSpawnObjectsList();
        }

        /// <summary>
        /// On custom Awake, set any un-set variables, spawn the initial pool and start the grooming coroutine
        /// </summary>
        public void Awaken(MonoBehaviour monoInstance)
        {
            if (!PoolContainer)
                PoolContainer = monoInstance?.transform;

            // Groom the spawn objects on awake to assert validity
            GroomSpawnObjectsList();

            // Spawn the initial object pool if desired on awake
            if (SpawnPoolOnAwake && _baseSpawnedCounter < PoolSize)
                SpawnBasePool();

            // Start grooming the object pools via coroutine
            monoInstance?.StartCoroutine(GroomObjectPool());
            _isAwake = true;
            _monoInstance = monoInstance;
        }



        /// <summary>
        /// Stops grooming the object pool until Awaken() is called again
        /// </summary>
        public void Sleep()
        {
            if (_monoInstance)
                _monoInstance.StopCoroutine(GroomObjectPool());

            _isAwake = false;
            _monoInstance = null;
        }

        /// ______________________ OBJECT POOLING FUNCTIONALITY ______________________///

        /// <summary>
        /// Spawns the initial object pool based on pool size and object weights (or re-fills the pool if needed)
        /// </summary>
        public void SpawnBasePool()
        {
            var totalWeight = _weightedSelector.TotalWeight;

            foreach (var obj in _objectsToSpawn)
            {
                // Spawns each objects in a quantity determined by their (weight fraction) * (the pool size)
                var weightedQuantityToSpawn = (int)((obj.Weight / (float)totalWeight) * PoolSize);
                for (var i = 0; i < weightedQuantityToSpawn; i++)
                {
                    if (CreatePoolObject(obj.Object).Object)
                        _baseSpawnedCounter++;
                }
            }
        }


        /// <summary>
        /// Spawns and automatically deactivates/disables a pool object of type given. Only spawns objects that are contained in the Object Spawn list, returns default otherwise.
        /// </summary>
        public PoolObject CreatePoolObject(GameObject objToSpawn)
        {
            // Do nothing if empty
            if (!objToSpawn)
                return default;

            // Stop possibility of pool recursively spawning itself
            if (_monoInstance && _monoInstance.gameObject == objToSpawn)
                return default;

            // If the given object isn't in the spawn list, return default and don't spawn anything (for loop for GC optimization)
            var contains = false;
            foreach (var obj in _objectsToSpawn)
            {
                if (obj.Object == objToSpawn)
                {
                    contains = true;
                    break;
                }
            }

            if (!contains)
                return default;

            // Spawn the object under this game object in the hierarchy
            var spawnedObject = UnityEngine.Object.Instantiate(objToSpawn, PoolContainer);

            // Create the pool object, setting its object and pool ID for its entire lifetime
            var poolObj = new PoolObject(spawnedObject, objToSpawn.GetInstanceID());

            // Invoke the OnSpawned event so other scripts can add behavior here
            OnSpawnedEvent?.Invoke(poolObj);

            // Deactivate the pool object on spawn
            DeactivateObject(poolObj);
            return poolObj;
        }

        /// <summary>
        /// Spawns and automatically deactivates/disables a pool object of type given. Only spawns objects that are contained in the Object Spawn list, returns default otherwise.
        /// </summary>
        public PoolObject CreatePoolObject(string spawnObjectName)
        {
            var first = _objectsToSpawn.FirstOrDefault(x => x.Object?.name == spawnObjectName);
            return first != default(PoolSpawnObject) ? CreatePoolObject(first.Object) : default;
        }

        /// <summary>
        /// Spawns and automatically deactivates/disables a pool object of type given. Only spawns objects that are contained in the Object Spawn list, returns default otherwise.
        /// </summary>
        public PoolObject CreatePoolObject(int instanceId)
        {
            var first = _objectsToSpawn.FirstOrDefault(x => x.Object?.GetInstanceID() == instanceId);
            return first != default(PoolSpawnObject) ? CreatePoolObject(first.Object) : default;
        }

        /// <summary>
        /// Gets or spawns an random available pool object from the objects to spawn, removes it from the pool and returns it. 
        /// Returns null if trying to grab object that is not in the spawn object list.
        /// </summary>
        public PoolObject GrabAvailablePoolObject()
        {
            // Get the random object type to spawn
            var objToSpawn = SpawnObjectGetRandom();
            if (objToSpawn == null)
                return default;

            return GrabAvailablePoolObject(objToSpawn.GetInstanceID());
        }

        /// <summary>
        /// Gets or spawns an available pool object from the pool of given pool bucket instance, removes it from the pool and returns it. 
        /// Returns null if trying to grab object that is not in the spawn object list.
        /// </summary>
        public PoolObject GrabAvailablePoolObject(GameObject poolBucketInstance)
        {
            return poolBucketInstance ? GrabAvailablePoolObject(poolBucketInstance.GetInstanceID()) : default;
        }

        /// <summary>
        /// Gets or spawns an available pool object from the pool of given pool bucket instance name, removes it from the pool and returns it.
        /// Returns null if trying to grab object that is not in the spawn object list.
        /// </summary>
        public PoolObject GrabAvailablePoolObject(string spawnObjectName)
        {
            var instanceId = GetPoolIdByName(spawnObjectName);
            return instanceId != 0 ? GrabAvailablePoolObject(instanceId) : default;
        }

        /// <summary>
        /// Gets or spawns an available pool object from the pool, removes it from the pool and returns it.
        /// Returns null if trying to grab object that is not in the spawn object list.
        /// </summary>
        public PoolObject GrabAvailablePoolObject(int poolInstanceId)
        {
            // Make sure our pool contains a bucket for the instance ID given
            if (_poolsByInstanceId.ContainsKey(poolInstanceId))
            {
                var poolBucket = _poolsByInstanceId[poolInstanceId];

                // If we have an available object in the pool, return it and remove it from the pool
                if (poolBucket != null && poolBucket.Count > 0)
                {
                    var firstAvailable = poolBucket[0];
                    poolBucket.RemoveAt(0);
                    TotalAvailableCount--;
                    return firstAvailable.Object == null ? GrabAvailablePoolObject(poolInstanceId) : firstAvailable;
                }
            }

            // Find object to spawn from spawn list and spawn new pool object if none were available
            var choices = _weightedSelector.GetChoices();
            GameObject objToSpawn = null;
            foreach (var obj in choices)
            {
                if (obj.Choice != null && obj.Choice.GetInstanceID() == poolInstanceId)
                {
                    objToSpawn = obj.Choice;
                    break;
                }
            }

            // Spawn the new object if none were available
            var spawnObj = CreatePoolObject(objToSpawn);
            if (_poolsByInstanceId.ContainsKey(poolInstanceId))
            {
                var bucket = _poolsByInstanceId[poolInstanceId];
                for (var i = bucket.Count - 1; i >= 0; i--)
                {
                    if (bucket[i].PoolId == spawnObj.PoolId)
                    {
                        bucket.RemoveAt(i);
                        TotalAvailableCount--;
                    }
                }
            }

            return spawnObj;
        }

        /// ______________________ ACTIVATE OBJECT OVERLOADS ______________________///

        /// <summary>
        /// Retrieves, activates, and releases a weighted random object from the object pool for use. Retrieves the first available pool object of desired type, or spawns one if none available.
        /// </summary>
        public bool ActivateObject()
        {
            return ActivateObject(out var activatedObj);
        }

        /// <summary>
        /// Retrieves, activates, and releases a weighted random object from the object pool for use. Retrieves the first available pool object of desired type, or spawns one if none available.
        /// Returns true if successful, false if failed. Outputs the activated pool object, or default if failed.
        /// </summary>
        public bool ActivateObject(out PoolObject activatedObject)
        {
            GroomSpawnObjectsList();

            // Cant activate if nothing to spawn
            if (_objectsToSpawn.Count == 0 || !(_weightedSelector.TotalWeight > 0))
            {
                activatedObject = default;
                return false;
            }

            // Get the random object type to spawn
            var objToSpawn = SpawnObjectGetRandom();
            if (objToSpawn == null)
            {
                activatedObject = default;
                return false;
            }

            return ActivateObject(objToSpawn.GetInstanceID(), out activatedObject);
        }

        /// <summary>
        /// Retrieves, activates, and releases the object from the object pool for use. Retrieves the first available pool object of desired type, or spawns one if none available.
        /// </summary>
        public bool ActivateObject(PoolObject poolObject)
        {
            // Return false and bail if empty object returned
            if (!poolObject.Object)
                return false;

            // Release the object from the pool if it exists there, since its activated now
            if (_poolsByInstanceId.ContainsKey(poolObject.PoolId))
            {
                if (_poolsByInstanceId[poolObject.PoolId].Remove(poolObject))
                    TotalAvailableCount--;
            }

            // Activate the object
            OnActivatedEvent?.Invoke(poolObject);
            poolObject.Object.SetActive(true);
            return true;
        }

        /// <summary>
        /// Retrieves, activates, and releases the object from the object pool for use. Retrieves the first available pool object of desired type, or spawns one if none available.
        /// </summary>
        public bool ActivateObject(GameObject poolObject)
        {
            return ActivateObject(poolObject.GetInstanceID(), out var activatedObject);
        }

        /// <summary>
        /// Retrieves, activates, and releases the object from the object pool for use. Retrieves the first available pool object of desired type, or spawns one if none available.
        /// </summary>
        public bool ActivateObject(GameObject poolObject, out PoolObject activatedObject)
        {
            if (!poolObject)
            {
                activatedObject = default;
                return false;
            }

            return ActivateObject(poolObject.GetInstanceID(), out activatedObject);
        }

        /// <summary>
        /// Retrieves, activates, and releases an object in the given pool bucket name, from the object pool for use. Retrieves the first available pool object of desired type, or spawns one if none available.
        /// </summary>
        public bool ActivateObject(string poolObjectName)
        {
            return ActivateObject(poolObjectName, out var activatedObject);
        }

        /// <summary>
        /// Retrieves, activates, and releases an object in the given pool bucket name, from the object pool for use. Retrieves the first available pool object of desired type, or spawns one if none available.
        /// Returns true if successful, false if failed. Outputs the activated pool object, or default if failed.
        /// </summary>
        public bool ActivateObject(string poolObjectName, out PoolObject activatedObject)
        {
            var poolInstanceId = GetPoolIdByName(poolObjectName);

            if (poolInstanceId == 0)
            {
                activatedObject = default;
                return false;
            }

            return ActivateObject(poolInstanceId, out activatedObject);
        }

        /// <summary>
        /// Retrieves, activates, and releases an object in the given pool instance, from the object pool for use. Retrieves the first available pool object of desired type, or spawns one if none available.
        /// </summary>
        public bool ActivateObject(int poolInstanceId)
        {
            return ActivateObject(poolInstanceId, out var activatedObj);
        }

        /// <summary>
        /// Retrieves, activates, and releases an object in the given pool instance, from the object pool for use. Retrieves the first available pool object of desired type, or spawns one if none available.
        /// Returns true if successful, false if failed. Outputs the activated pool object if successful, empty (default) pool object if failed.
        /// (Several overloads exist of this function for ease of use)
        /// </summary>
        public bool ActivateObject(int poolInstanceId, out PoolObject activatedObject)
        {
            // Get or spawn an available pool object, removes it from the pool if found one that was already spawned
            activatedObject = GrabAvailablePoolObject(poolInstanceId);

            // Return false and bail if empty object returned
            if (!activatedObject.Object)
                return false;

            // Activate the object
            OnActivatedEvent?.Invoke(activatedObject);
            activatedObject.Object.SetActive(true);
            return true;
        }


        /// ______________________ FINISHED ACTIVATE OBJECT OVERLOADS ______________________///


        /// <summary>
        /// Deactivates/disables an object and adds it to the pool to be available for future object pool use. 
        /// If the object's Pool ID is not found in the list of Objects To Spawn, the object will be destroyed instead.
        /// Returns true if something was properly deactivated or destroyed, false if nothing was deactivated.
        /// </summary>
        /// <param name="poolObject"></param>
        public bool DeactivateObject(PoolObject poolObject)
        {
            // Make sure its a valid pool obj
            if (poolObject.Object == null || poolObject.PoolId == 0)
                return false;

            // If the pool object ID doesnt match any in the Objects To Spawn list, just destroy it instead
            var containsId = false;
            foreach (var obj in _objectsToSpawn)
            {
                if (obj.Object?.GetInstanceID() == poolObject.PoolId)
                {
                    containsId = true;
                    break;
                }
            }

            if (!containsId)
                return DestroyPoolObject(poolObject);

            poolObject.Object.SetActive(false);

            // If the object was moved in the hierarchy, re-place it in the pool container in the inspector hierarchy
            if (poolObject.Transform.parent != PoolContainer)
                poolObject.Transform.parent = PoolContainer;

            // Add the pool object back into a pool bucket, or create a pool bucket for it if it doesn't already exist.
            if (_poolsByInstanceId.TryGetValue(poolObject.PoolId, out var poolList))
            {
                // Manual contains loop for GC optimization
                bool containsObj = false;
                foreach (var obj in poolList)
                {
                    if (obj.Object == poolObject.Object)
                    {
                        containsObj = true;
                        break;
                    }
                }

                if (!containsObj)
                {
                    poolList.Add(poolObject);
                    TotalAvailableCount++;
                }
            }
            else // Create the pool bucket if it didn't exist
            {
                _poolsByInstanceId.Add(poolObject.PoolId, new List<PoolObject>() { poolObject });
                TotalAvailableCount++;
            }

            OnDeactivatedEvent?.Invoke(poolObject);
            return true;
        }

        /// <summary>
        /// Deactivates/disables an object and adds it to the pool to be available for future object pool use. 
        /// If the object's Pool ID is not found in the list of Objects To Spawn, the object will be destroyed instead.
        /// Returns true if something was properly deactivated or destroyed, false if nothing was deactivated.        
        /// </summary>
        public bool DeactivateObject(GameObject objToDeactivate, int poolId)
        {
            return DeactivateObject(new PoolObject(objToDeactivate, poolId));
        }

        /// <summary>
        /// Destroys and removes the pool object given from the entire pool. Returns true if destroyed an object, returns false if no object was found to destroy.
        /// </summary>
        public bool DestroyPoolObject(PoolObject objToDestroy)
        {
            // If its in the pool, remove it from the pool count
            if (_poolsByInstanceId.ContainsKey(objToDestroy.PoolId))
            {
                var bucket = _poolsByInstanceId[objToDestroy.PoolId];
                for (var i = 0; i < bucket.Count; i++)
                {
                    if (bucket[i].Object == objToDestroy.Object)
                    {
                        bucket.RemoveAt(i);
                        TotalAvailableCount--;
                        break;
                    }
                }
            }

            // Destroy the gameobject
            if (objToDestroy.Object)
            {
                OnDestroyedEvent?.Invoke(objToDestroy);
                UnityEngine.Object.Destroy(objToDestroy.Object.gameObject);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Grooms the object pool to keep it clean and optimized, especially when using Adaptive Pool
        /// This function is performed over several frames to optimize performance
        /// </summary>
        private const float GroomTimeInterval = 0.1f;
        private readonly WaitForSecondsRealtime _groomInterval = new WaitForSecondsRealtime(GroomTimeInterval);
        private float _adaptivePoolTimer;
        private IEnumerator GroomObjectPool()
        {
            while (true)
            {
                // Groom delay interval
                yield return _groomInterval;

                GroomSpawnObjectsList();

                // Spawn any un-spawned base pool objects over several frames (handles increasing base pool size or not using SpawnPoolOnAwake())
                if (_baseSpawnedCounter < (PoolSize))
                {
                    var totalWeight = _weightedSelector.TotalWeight;
                    foreach (var obj in _objectsToSpawn)
                    {
                        // Spawns each objects in a quantity determined by their (weight fraction) * (the pool size)
                        var weightedQuantityToSpawn = (int)((obj.Weight / (float)totalWeight) * PoolSize);
                        for (var i = 0; i < weightedQuantityToSpawn; i++)
                        {
                            if (CreatePoolObject(obj.Object).Object)
                            {
                                _baseSpawnedCounter++;
                                break;
                            }
                        }
                    }
                }

                // Remove any pool objects that no longer exist as a spawnable object as it is no longer desired(base grooming)
                var keys = _poolsByInstanceId.Keys;
                foreach (var key in keys)
                {
                    var bucketIsNeeded = false;
                    foreach (var obj in _objectsToSpawn)
                    {
                        if (obj.Object?.GetInstanceID() == key)
                        {
                            bucketIsNeeded = true;
                            break;
                        }
                    }

                    // If the bucket Id is no longer needed, destroy and remove the unwanted pool objects each frame
                    if (!bucketIsNeeded)
                    {
                        if (_poolsByInstanceId[key].Count > 0)
                        {
                            //Destroy and remove a pool object from the pool
                            var poolList = _poolsByInstanceId[key];
                            DestroyPoolObject(poolList[0]);
                        }
                        else
                        {
                            // If there are no more objects in the bucket to remove, remove the entire buckets from the Id and Name pools
                            _poolsByInstanceId.Remove(key);
                            break; // Needed so we don't modify the enumerable collection, instead we just spread over multiple frames to avoid GC
                        }
                    }
                }

                // Groom the adaptive pool to keep pool on par with parameters set for supply and demand of the system
                if (AllowAdaptivePool)
                {
                    // Make sure these values are never negative
                    AdaptivePoolPadding = AdaptivePoolPadding < 0 ? 0 : AdaptivePoolPadding;
                    AdaptivePoolSpeed = AdaptivePoolSpeed < 0 ? 0 : AdaptivePoolSpeed;

                    // Get the total weight of all the spawn objects to determine the individual weighted spawn amounts
                    var total = _weightedSelector.TotalWeight;
                    total = total == 0 ? 1 : total; // (cant be 0 because its a divisor)

                    // For all of the spawn objects
                    foreach (var obj in _objectsToSpawn)
                    {
                        // Make sure they're real
                        if (obj.Object == null)
                            continue;

                        // Get the individual weighted spawn amounts based on the AdaptivePoolPadding
                        var paddingQuantity = (int)((obj.Weight / (float)total) * AdaptivePoolPadding);

                        // Make minimum weighted quantity = 1 if weight is > 0, so rounding defaults to 1 instead of 0, but no weight will have no pool objs
                        paddingQuantity = obj.Weight > 0 ? (paddingQuantity <= 0 ? 1 : paddingQuantity) : 0;

                        // Check to see if we have enough available pool objects
                        var instanceId = obj.Object.GetInstanceID();

                        if (_poolsByInstanceId.ContainsKey(instanceId))
                        {
                            var poolList = _poolsByInstanceId[instanceId];

                            // If we don't have enough available pool objects
                            if (poolList.Count < paddingQuantity)
                            {
                                // Spawn more pool objects based on adaptive pool speed
                                for (var i = 0; i < AdaptivePoolSpeed; i++)
                                    CreatePoolObject(obj.Object);

                                _adaptivePoolTimer = 0;
                            }
                            // If we have too many available pool objects, destroy the extra
                            else if (_poolsByInstanceId[instanceId].Count > paddingQuantity)
                            {

                                // Only start destroying excess pool objects after timed delay of excess exist
                                if (_adaptivePoolTimer >= AdaptiveShrinkDelay)
                                {
                                    // Destroy at the adaptive speed if we can safely, otherwise destroy one per frame
                                    if (_poolsByInstanceId[instanceId].Count - paddingQuantity > AdaptivePoolSpeed)
                                        for (var i = 0; i < AdaptivePoolSpeed; i++)
                                            DestroyPoolObject(poolList[0]);
                                    else
                                        DestroyPoolObject(poolList[0]);
                                }

                                _adaptivePoolTimer += GroomTimeInterval;
                            }
                            else
                                _adaptivePoolTimer = 0;
                        }
                    }
                }
            }
        }
    }


    #endregion Main Object Pool Functions

}