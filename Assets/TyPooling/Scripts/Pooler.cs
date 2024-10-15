using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Typooling
{
    public class Pooler : MonoBehaviour
    {
        [SerializeField] private GameObject objectToPool = null;
        [SerializeField, Min(1)] private int initialCount = 10;
        [SerializeField] private Vector3 storagePosition = Vector3.zero;
        [SerializeField] private bool clearParticlesOnDespawn = true;
        [SerializeField] private float autoReturnDelay = 5f; // Add this field

        public List<ObjectInstance> Pool { get { return pool.ToList(); } }
        private Queue<ObjectInstance> pool = new Queue<ObjectInstance>();

        private GameObject cachedObjectReference = null;
        private bool isParticleSystem;

        private void Awake()
        {
            objectToPool.SetActive(false);
            cachedObjectReference = objectToPool;
            isParticleSystem = objectToPool.GetComponent<ParticleSystem>() != null;
            CreateNewPool(initialCount);
        }

        // Gets a fresh object from the pool. If none is available, a new one is dynamically created.
        private ObjectInstance GetNextAvailableInstance()
        {
            if (pool.Count > 0)
            {
                var instance = pool.Dequeue();
                if (!instance.IsActive() && instance.GetObject() != null)
                {
                    return instance;
                }
            }

            return CreateObjectInstance(pool.Count);
        }

        public GameObject GetFromPool()
        {
            var instance = GetNextAvailableInstance();
            Debug.Log($"[Pooler] GetFromPool: Activated instance {instance.GetObject().name}");
            return instance.ActivateAndGetInstance();
        }

        public GameObject GetFromPool(Vector3 position, Quaternion rotation)
        {
            var instance = GetNextAvailableInstance();
            return instance.ActivateAndGetInstance(position, rotation);
        }

        public GameObject GetFromPool(Transform parent)
        {
            var instance = GetNextAvailableInstance();
            return instance.ActivateAndGetInstance(parent);
        }
        
        public GameObject GetFromPool(Vector3 position, Quaternion rotation, Transform parent)
        {
            var instance = GetNextAvailableInstance();
            return instance.ActivateAndGetInstance(position, rotation, parent);
        }
        

        public void ReturnToPool(Instance instance)
        {
            var objectInstance = pool.FirstOrDefault(oi => oi.GetInstance() == instance);
            if (objectInstance != null)
            {
                Debug.Log($"[Pooler] ReturnToPool: Deactivated instance {objectInstance.GetObject().name}");
                objectInstance.SetInactive(storagePosition);
                pool.Enqueue(objectInstance);
            }
            else
            {
                Debug.LogWarning($"[Pooler] ReturnToPool: Instance not found in pool");
            }
        }

        private ObjectInstance CreateObjectInstance(int i)
        {
            if(objectToPool != cachedObjectReference)
            {
                Debug.LogError("The object registered to this pooler has been changed since initialization. It is recommended that you avoid changing pooler objects during runtime.");
            }

            return new ObjectInstance(this, i);
        }

        private void DestroyObjectInstance(ObjectInstance instance)
        {
            Destroy(instance.GetObject());
        }

        // Increases the pool size and returns the index of the first new instance.
        public int IncreasePoolSize(int count)
        {
            int initialCount = pool.Count;
            for(int a = 0; a < count; a++)
            {
                ObjectInstance newInstance = CreateObjectInstance(pool.Count);
                pool.Enqueue(newInstance);
            }

            return initialCount;
        }

        // Disposes of any existing pool, then sets up a fresh pool.
        public void CreateNewPool(int count)
        {
            DisposePool();

            for (int a = 0; a < count; a++)
            {
                ObjectInstance newInstance = CreateObjectInstance(a);
                pool.Enqueue(newInstance);
            }
        }

        // Destroys all extant instanced objects, then clears the pool.
        public void DisposePool()
        {
            foreach (var instance in pool)
            {
                DestroyObjectInstance(instance);
            }

            pool.Clear();
        }

        private void OnDestroy()
        {
            DisposePool();
        }

        public PoolStatistics GetPoolStats()
        {
            return new PoolStatistics(this);
        }

        public class ObjectInstance
        {
            private GameObject instancedObject = null;
            private bool isActive = false;
            private Instance instance = null;
            private Pooler origin;
            private ParticleSystem[] particleSystems;
            private bool isParticleSystem;
            private bool particlesPlaying = false;
            private Coroutine autoReturnCoroutine;

            internal ObjectInstance(Pooler origin, int index)
            {
                this.origin = origin;
                instancedObject = Instantiate(origin.objectToPool, origin.transform);
                instancedObject.name = origin.objectToPool.name + "-" + origin.name + "-" + index;
                instance = instancedObject.AddComponent<Instance>();
                instance.Setup(origin, index);
                isParticleSystem = origin.isParticleSystem;
                if (isParticleSystem)
                {
                    particleSystems = instancedObject.GetComponentsInChildren<ParticleSystem>(true);
                }
                SetInactive(origin.storagePosition);
            }

            internal void SetActive()
            {
                if (!isActive)
                {
                    SetState(true);
                    if (isParticleSystem && !particlesPlaying)
                    {
                        PlayParticles();
                    }
                }
            }

            internal void SetInactive(Vector3 storagePosition)
            {
                // Ensure the object is active before manipulating it
                if (instancedObject != null && !instancedObject.activeSelf)
                {
                    instancedObject.SetActive(true);
                }

                SetState(false);
                if (instancedObject != null)
                {
                    instancedObject.transform.SetParent(origin.transform);
                    instancedObject.transform.SetPositionAndRotation(storagePosition, Quaternion.identity);
                }
                if (isParticleSystem)
                {
                    StopParticles();
                }
                StopAutoReturnCoroutine();
            }

            private void SetState(bool state)
            {
                isActive = state;
                instancedObject.SetActive(state);
            }

            public bool IsActive()
            {
                return isActive;
            }

            internal GameObject ActivateAndGetInstance()
            {
                SetActive();
                StartAutoReturnCoroutine();
                return instancedObject;
            }

            internal GameObject ActivateAndGetInstance(Vector3 position, Quaternion rotation)
            {
                SetActive();
                instancedObject.transform.SetPositionAndRotation(position, rotation);
                return instancedObject;
            }

            internal GameObject ActivateAndGetInstance(Transform parent)
            {
                SetActive();
                instancedObject.transform.SetParent(parent);
                return instancedObject;
            }

            internal GameObject ActivateAndGetInstance(Vector3 position, Quaternion rotation, Transform parent)
            {
                SetActive();
                instancedObject.transform.SetPositionAndRotation(position, rotation);
                instancedObject.transform.SetParent(parent);
                return instancedObject;
            }

            public GameObject GetObject()
            {
                return instancedObject;
            }

            public Instance GetInstance()
            {
                return instance;
            }

            private void PlayParticles()
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    particleSystems[i].Clear();
                    particleSystems[i].Play();
                }
                particlesPlaying = true;
            }

            private void StopParticles()
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                particlesPlaying = false;
            }

            private void StartAutoReturnCoroutine()
            {
                StopAutoReturnCoroutine();
                autoReturnCoroutine = origin.StartCoroutine(AutoReturnWhenFinished());
            }

            private void StopAutoReturnCoroutine()
            {
                if (autoReturnCoroutine != null)
                {
                    origin.StopCoroutine(autoReturnCoroutine);
                    autoReturnCoroutine = null;
                }
            }

            private IEnumerator AutoReturnWhenFinished()
            {
                if (isParticleSystem)
                {
                    float longestDuration = 0f;
                    foreach (var ps in particleSystems)
                    {
                        float totalDuration = ps.main.duration + ps.main.startLifetime.constantMax;
                        if (totalDuration > longestDuration)
                        {
                            longestDuration = totalDuration;
                        }
                    }

                    yield return new WaitForSeconds(longestDuration);
                    Debug.Log($"[Pooler] AutoReturnWhenFinished: Particle system finished for {instancedObject.name} after {longestDuration} seconds");
                }
                else
                {
                    yield return new WaitForSeconds(origin.autoReturnDelay);
                    Debug.Log($"[Pooler] AutoReturnWhenFinished: Auto-return delay finished for {instancedObject.name}");
                }
                
                if (instancedObject != null)
                {
                    // Ensure the object is active before returning it to the pool
                    instancedObject.SetActive(true);
                    
                    if (IsActive())
                    {
                        Debug.Log($"[Pooler] AutoReturnWhenFinished: Returning {instancedObject.name} to pool");
                        origin.ReturnToPool(instance);
                    }
                }
            }
        }
    }
}
