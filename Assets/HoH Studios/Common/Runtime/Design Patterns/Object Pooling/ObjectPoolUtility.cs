using System.Collections.Generic;
using System.Linq;
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
    public sealed partial class ObjectPool
    {
        /// <summary>
        /// Returns the total number of available pool objects at any given time
        /// </summary>
        public int TotalAvailableCount { get; private set; }

        /// <summary>
        /// The struct representing a spawnable pool object and its unique pool Id
        /// </summary>
        public struct PoolObject
        {
            /// <summary>
            /// The game object to spawn, activate, and deactivate
            /// </summary>
            public GameObject Object { get; }

            /// <summary>
            /// The game object's transform
            /// </summary>
            public Transform Transform { get; }

            /// <summary>
            /// Unique ID for the spawn object, necessary so objects that are spawned know which object it was cloned from.
            /// </summary>
            public int PoolId { get; }

            public PoolObject(GameObject obj)
            {
                Object = obj;
                Transform = obj.transform;
                PoolId = obj.GetInstanceID();
            }
            public PoolObject(GameObject obj, int poolInstanceId)
            {
                Object = obj;
                Transform = obj.transform;
                PoolId = poolInstanceId;
            }
        }

        /// <summary>
        /// Abstraction for the spawn objects to be serializable, made a class so it can serialize properly over struct
        /// </summary>
        [System.Serializable]
        public class PoolSpawnObject
        {
            /// <summary>
            /// The object to spawn
            /// </summary>
            public GameObject Object;

            /// <summary>
            /// The spawn weight of the object
            /// </summary>
            public int Weight;
        }

        /// <summary>
        /// Returns the instance ID of the FIRST spawn object that matches the object name given. Returns 0 if no spawn object exists with that name
        /// </summary>
        public int GetPoolIdByName(string spawnObjectName)
        {
            var first = _objectsToSpawn.FirstOrDefault(x => x.Object?.name == spawnObjectName);
            return first?.Object.GetInstanceID() ?? 0;
        }
        /// <summary>
        /// Returns the instance ID of the FIRST spawn object that matches the object name given. Returns 0 if no spawn object exists with that name
        /// </summary>
        public int GetPoolId(GameObject spawnObj)
        {
            var first = _objectsToSpawn.FirstOrDefault(x => x.Object == spawnObj);
            return first?.Object.GetInstanceID() ?? 0;
        }

        /// <summary>
        /// Returns all of the pool buckets
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, List<PoolObject>> GetPoolBuckets()
        {
            return new Dictionary<int, List<PoolObject>>(_poolsByInstanceId);
        }

        /// <summary>
        /// Returns the bucket of available pool objects for the given spawn object (must be the same instance of the object given). Returns null if key does not exist.
        /// </summary>
        public List<PoolObject> GetPoolBucket(GameObject bucketObject)
        {
            return _poolsByInstanceId.FirstOrDefault(x => x.Key == bucketObject?.GetInstanceID()).Value;
        }

        /// <summary>
        /// Returns the bucket of available pool objects for the bucket instance ID given. Returns null if key does not exist.
        /// </summary>
        public List<PoolObject> GetPoolBucket(int instanceId)
        {
            return _poolsByInstanceId.FirstOrDefault(x => x.Key == instanceId).Value;
        }

        /// <summary>
        /// Clears and destroys all available objects in the pool system.
        /// </summary>
        public void ClearAllPoolBuckets()
        {
            foreach (var bucket in _poolsByInstanceId)
                foreach (var obj in bucket.Value)
                    if (obj.Object)
                        Object.Destroy(obj.Object);

            TotalAvailableCount = 0;
            _poolsByInstanceId.Clear();
        }

        /// <summary>
        /// Clears and destroys all available objects inside given bucket and removes the bucket when done. Returns true if successfully found and cleared bucket.
        /// </summary>
        public bool ClearPoolBucket(GameObject bucketObject)
        {
            return bucketObject != null && ClearPoolBucket(bucketObject.GetInstanceID());
        }

        /// <summary>
        /// Clears and destroys all available objects inside given bucket and removes the bucket when done. Returns true if successfully found and cleared bucket.
        /// </summary>
        public bool ClearPoolBucket(int instanceId)
        {
            var bucket = _poolsByInstanceId.FirstOrDefault(x => x.Key == instanceId).Value;

            if (bucket == null)
                return false;

            foreach (var obj in bucket)
                if (obj.Object)
                {
                    Object.Destroy(obj.Object);
                    TotalAvailableCount--;
                }

            _poolsByInstanceId.Remove(instanceId);
            return true;
        }

        /// <summary>
        /// Grooms the spawn object list to make sure everything is as expected
        /// </summary>
        private void GroomSpawnObjectsList()
        {
            _weightedSelector.ClearChoices();

            if (_objectsToSpawn == null)
                _objectsToSpawn = new List<PoolSpawnObject>();

            // Re-populates the choices each time incase the choices had changed (didnt do reverse loop - want to remove from end of list)
            for (var i = 0; i < _objectsToSpawn.Count; i++)
            {
                var spawnObj = _objectsToSpawn[i];

                // Stop possibility of pool recursively spawning itself
                if (spawnObj == null || _monoInstance && _monoInstance.gameObject == spawnObj.Object)
                {
                    _objectsToSpawn.RemoveAt(i);
                    i--;
                    continue;
                }

                // Don't spawn any null objects
                if (spawnObj.Object == null)
                {
                    spawnObj.Weight = 0;
                    continue;
                }

                // No negative weights
                spawnObj.Weight = spawnObj.Weight < 0 ? 0 : spawnObj.Weight;
                _weightedSelector.AddChoice(spawnObj.Object, spawnObj.Weight);
            }
        }

        /// <summary>
        /// Utility function to get a COPY of the Objects To Spawn list
        /// </summary>
        public List<PoolSpawnObject> SpawnObjectsGetAll()
        {
            return new List<PoolSpawnObject>(_objectsToSpawn);
        }

        /// <summary>
        /// Utility function to clear and empty the Objects To Spawn list all at once
        /// </summary>
        public void SpawnObjectsClearAll()
        {
            _objectsToSpawn.Clear();
        }
        /// <summary>
        /// Returns the FIRST spawn object from the list that matches the gameobject given (must be the same instance of the object given), returns default otherwise
        /// </summary>
        public PoolSpawnObject SpawnObjectsGet(GameObject objToGet)
        {
            return _objectsToSpawn.FirstOrDefault(x => x.Object == objToGet);

        }

        /// <summary>
        /// Returns the FIRST spawn object from the list that matches the gameobject and weight given (must be the same instance of the object given), returns default otherwise
        /// </summary>
        public PoolSpawnObject SpawnObjectsGet(GameObject objToGet, int weight)
        {
            return _objectsToSpawn.FirstOrDefault(x => (int)x.Weight == weight && x.Object == objToGet);

        }

        /// <summary>
        /// Returns the FIRST spawn object from the list that matches the gameobject name given, returns default otherwise
        /// </summary>
        public PoolSpawnObject SpawnObjectsGet(string objName)
        {
            return _objectsToSpawn.FirstOrDefault(x => x.Object?.name == objName);

        }

        /// <summary>
        /// Returns the FIRST spawn object from the list that matches the gameobject name given with the weight given, returns default otherwise
        /// </summary>
        public PoolSpawnObject SpawnObjectsGet(string objName, int weight)
        {
            return _objectsToSpawn.FirstOrDefault(x => (int)x.Weight == weight && x.Object?.name == objName);

        }

        /// <summary>
        /// Returns the spawn object from the list that matches the instance id given, returns default otherwise
        /// </summary>
        public PoolSpawnObject SpawnObjectsGet(int instanceId)
        {
            return _objectsToSpawn.FirstOrDefault(x => x.Object?.GetInstanceID() == instanceId);

        }

        /// <summary>
        /// Returns the spawn object from the list that matches the instance id given, returns default otherwise
        /// </summary>
        public PoolSpawnObject SpawnObjectsGet(int instanceId, int weight)
        {
            return _objectsToSpawn.FirstOrDefault(x =>
                (int)x.Weight == weight && x.Object?.GetInstanceID() == instanceId);
        }

        /// <summary>
        /// Returns the random choice selection from all of the Objects to Spawn. Returns null if invalid choice received.
        /// </summary>
        /// <returns></returns>
        public GameObject SpawnObjectGetRandom()
        {
            _weightedSelector.ClearChoices();

            foreach (var spawnObj in _objectsToSpawn)
                _weightedSelector.AddChoice(spawnObj.Object, spawnObj.Weight);

            return _weightedSelector.Choose();
        }

        /// <summary>
        /// Safely adds a spawn object to the list with given weight
        /// </summary>
        public void SpawnObjectsAdd(GameObject obj, int weight)
        {
            _objectsToSpawn.Add(new PoolSpawnObject()
            {
                Object = obj,
                Weight = weight,
            });

            GroomSpawnObjectsList();
        }

        /// <summary>
        /// Safely removes the FIRST spawn object from the list that matches the game object given (must be the same instance of the object given)
        /// </summary>
        public void SpawnObjectsRemove(GameObject objToRemove)
        {
            for (var i = 0; i < _objectsToSpawn.Count; i++)
            {
                if (_objectsToSpawn[i].Object != objToRemove) continue;

                _objectsToSpawn.RemoveAt(i);
                GroomSpawnObjectsList();
                return;
            }
        }

        /// <summary>
        /// Safely removes the FIRST spawn object from the list that matches the game object and weight given (must be the same instance of the object given)
        /// </summary>
        public void SpawnObjectsRemove(GameObject objToRemove, int weight)
        {
            for (var i = 0; i < _objectsToSpawn.Count; i++)
            {
                if ((int)_objectsToSpawn[i].Weight != weight || _objectsToSpawn[i].Object != objToRemove) continue;

                _objectsToSpawn.RemoveAt(i);
                GroomSpawnObjectsList();
                return;
            }
        }

        /// <summary>
        /// Safely removes the FIRST spawn object from the list that matches the game object name given
        /// </summary>
        public void SpawnObjectsRemove(string objNameToRemove)
        {
            for (var i = 0; i < _objectsToSpawn.Count; i++)
            {
                if (_objectsToSpawn[i].Object?.name != objNameToRemove) continue;

                _objectsToSpawn.RemoveAt(i);
                GroomSpawnObjectsList();
                return;
            }
        }

        /// <summary>
        /// Safely removes the FIRST spawn object from the list that matches the game object name and weight given
        /// </summary>
        public void SpawnObjectsRemove(string objNameToRemove, int weight)
        {
            for (var i = 0; i < _objectsToSpawn.Count; i++)
            {
                if ((int)_objectsToSpawn[i].Weight != weight ||
                    _objectsToSpawn[i].Object?.name != objNameToRemove) continue;

                _objectsToSpawn.RemoveAt(i);
                GroomSpawnObjectsList();
                return;
            }
        }

        /// <summary>
        /// Safely removes spawn object in the list that matches the instance id given
        /// </summary>
        public void SpawnObjectsRemove(int instanceId)
        {
            for (var i = 0; i < _objectsToSpawn.Count; i++)
            {
                if (_objectsToSpawn[i].Object?.GetInstanceID() != instanceId) continue;

                _objectsToSpawn.RemoveAt(i);
                GroomSpawnObjectsList();
                return;
            }
        }

        /// <summary>
        /// Safely removes spawn object in the list that matches the instance id and weight given
        /// </summary>
        public void SpawnObjectsRemove(int instanceId, int weight)
        {
            for (var i = 0; i < _objectsToSpawn.Count; i++)
            {
                if ((int)_objectsToSpawn[i].Weight != weight ||
                    _objectsToSpawn[i].Object?.GetInstanceID() != instanceId) continue;

                _objectsToSpawn.RemoveAt(i);
                GroomSpawnObjectsList();
                return;
            }
        }

        /// <summary>
        /// Changes the weight of the FIRST spawn object that matches the object given (must be the same instance of the object given)
        /// </summary>
        public void SpawnObjectsChange(GameObject objToChange, int newWeight)
        {
            if (!objToChange)
                newWeight = 0;

            for (var i = 0; i < _objectsToSpawn.Count; i++)
            {
                var spawnObj = _objectsToSpawn[i];
                if (spawnObj.Object == objToChange)
                {
                    _objectsToSpawn[i].Object = spawnObj.Object;
                    _objectsToSpawn[i].Weight = newWeight;
                    GroomSpawnObjectsList();
                    return;
                }
            }
        }

        /// <summary>
        /// Changes the weight of the FIRST spawn object that matches the object and old weight given (must be the same instance of the object given)
        /// </summary>
        public void SpawnObjectsChange(GameObject objToChange, int oldWeight, int newWeight)
        {
            if (!objToChange)
                newWeight = 0;

            for (var i = 0; i < _objectsToSpawn.Count; i++)
            {
                var spawnObj = _objectsToSpawn[i];
                if ((int)spawnObj.Weight == oldWeight && spawnObj.Object == objToChange)
                {
                    _objectsToSpawn[i].Object = spawnObj.Object;
                    _objectsToSpawn[i].Weight = newWeight;
                    GroomSpawnObjectsList();
                    return;
                }
            }
        }

        /// <summary>
        /// Changes the weight of the FIRST spawn object that matches the object name given
        /// </summary>
        public void SpawnObjectsChange(string objNameToChange, int newWeight)
        {
            if (string.IsNullOrEmpty(objNameToChange))
                newWeight = 0;

            for (var i = 0; i < _objectsToSpawn.Count; i++)
            {
                var spawnObj = _objectsToSpawn[i];
                if (spawnObj.Object?.name == objNameToChange)
                {
                    _objectsToSpawn[i].Object = spawnObj.Object;
                    _objectsToSpawn[i].Weight = newWeight;
                    GroomSpawnObjectsList();
                    return;
                }
            }
        }

        /// <summary>
        /// Changes the weight of the FIRST spawn object that matches the object name and old weight given 
        /// </summary>
        public void SpawnObjectsChange(string objNameToChange, int oldWeight, int newWeight)
        {
            if (string.IsNullOrEmpty(objNameToChange))
                newWeight = 0;

            for (var i = 0; i < _objectsToSpawn.Count; i++)
            {
                var spawnObj = _objectsToSpawn[i];
                if ((int)spawnObj.Weight == oldWeight && spawnObj.Object?.name == objNameToChange)
                {
                    _objectsToSpawn[i].Object = spawnObj.Object;
                    _objectsToSpawn[i].Weight = newWeight;
                    GroomSpawnObjectsList();
                    return;
                }
            }
        }

        /// <summary>
        /// Changes the weight of the spawn object in the list that matches the instance id given
        /// </summary>
        public void SpawnObjectsChange(int instanceId, int newWeight)
        {
            for (var i = 0; i < _objectsToSpawn.Count; i++)
            {
                var spawnObj = _objectsToSpawn[i];
                if (spawnObj.Object?.GetInstanceID() == instanceId)
                {
                    _objectsToSpawn[i].Object = spawnObj.Object;
                    _objectsToSpawn[i].Weight = newWeight;
                    GroomSpawnObjectsList();
                    return;
                }
            }
        }

        /// <summary>
        /// Changes the weight of the FIRST spawn object that matches the object name and old weight given 
        /// </summary>
        public void SpawnObjectsChange(int instanceId, int oldWeight, int newWeight)
        {
            for (var i = 0; i < _objectsToSpawn.Count; i++)
            {
                var spawnObj = _objectsToSpawn[i];
                if ((int)spawnObj.Weight == oldWeight && spawnObj.Object?.GetInstanceID() == instanceId)
                {
                    _objectsToSpawn[i].Object = spawnObj.Object;
                    _objectsToSpawn[i].Weight = newWeight;
                    GroomSpawnObjectsList();
                    return;
                }
            }
        }

        /// <summary>
        /// Returns the total weight of the weighted object choices
        /// </summary>
        public int SpawnObjectsTotalWeight()
        {
            return _weightedSelector.TotalWeight;
        }
    }
}