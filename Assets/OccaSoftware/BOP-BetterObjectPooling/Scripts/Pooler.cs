using System.Collections.Generic;
using UnityEngine;

namespace OccaSoftware.BOP
{
    public class Pooler : MonoBehaviour
    {
        [SerializeField] private GameObject objectToPool = null;
        [SerializeField, Min(1)] private int initialCount = 10;
        [SerializeField] private Vector3 storagePosition = Vector3.zero;

        public List<ObjectInstance> Pool { get { return pool; } }
        private List<ObjectInstance> pool = new List<ObjectInstance>();

        private GameObject cachedObjectReference = null;

        private void Awake()
        {
            objectToPool.SetActive(false);
            cachedObjectReference = objectToPool;
            CreateNewPool(initialCount);
        }


        // Gets a fresh object from the pool. If none is available, a new one is dynamically created.
        private int GetNextIndex()
        {
            for (int a = 0; a < pool.Count; a++)
            {
                if (!pool[a].IsActive() && pool[a].GetObject() != null)
                {
                    return a;
                }
            }

            int newIndex = IncreasePoolSize(1);
            return newIndex;
        }


        public GameObject GetFromPool()
        {
            int a = GetNextIndex();
            return pool[a].ActivateAndGetInstance();
        }

        public GameObject GetFromPool(Vector3 position, Quaternion rotation)
        {
            int a = GetNextIndex();
            return pool[a].ActivateAndGetInstance(position, rotation);
        }

        public GameObject GetFromPool(Transform parent)
        {
            int a = GetNextIndex();
            return pool[a].ActivateAndGetInstance(parent);
        }
        
        public GameObject GetFromPool(Vector3 position, Quaternion rotation, Transform parent)
        {
            int a = GetNextIndex();
            return pool[a].ActivateAndGetInstance(position, rotation, parent);
        }
        

        internal void ReturnToPool(Instance instance)
        {
            pool[instance.GetIndex()].SetInactive(storagePosition);
        }


        private ObjectInstance CreateObjectInstance(int i)
        {
            if(objectToPool != cachedObjectReference)
            {
                Debug.LogError("The object registered to this pooler has been changed since initialization. It is recommended that you avoid changing pooler objects during runtime.");
            }

            return new ObjectInstance(this, i);
        }

        private void DestroyObjectInstance(int i)
        {
            Destroy(pool[i].GetObject());
        }


        // Increases the pool size and returns the index of the first new instance.
        public int IncreasePoolSize(int count)
        {
            int initialPoolSize = pool.Count;
            for(int a = 0; a < count; a++)
            {
                ObjectInstance newInstance = CreateObjectInstance(initialPoolSize + a);
                pool.Add(newInstance);
            }

            return initialPoolSize;
        }

        // Disposes of any existing pool, then sets up a fresh pool.
        public void CreateNewPool(int count)
        {
            DisposePool();

            for (int a = 0; a < count; a++)
            {
                ObjectInstance newInstance = CreateObjectInstance(a);
                pool.Add(newInstance);
            }
        }

        // Destroys all extant instanced objects, then clears the pool.
        public void DisposePool()
        {
            for (int a = 0; a < pool.Count; a++)
            {
                DestroyObjectInstance(a);
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

            internal ObjectInstance(Pooler origin, int index)
            {
                this.origin = origin;
                instancedObject = Instantiate(origin.objectToPool, origin.transform);
                instancedObject.name = origin.objectToPool.name + "-" + origin.name + "-" + index;
                instance = instancedObject.AddComponent<Instance>();
                instance.Setup(origin, index);
                SetInactive(origin.storagePosition);
            }

            internal void SetActive()
            {
                SetState(true);
                // Restart all ParticleSystem components when the object is activated
                foreach (var particleSystem in instancedObject.GetComponentsInChildren<ParticleSystem>())
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particleSystem.Play();
                }
            }

            internal void SetInactive(Vector3 storagePosition)
            {
                SetState(false);
                instancedObject.transform.SetParent(origin.transform); // Ensure the parent is set to the Pooler GameObject
                instancedObject.transform.SetPositionAndRotation(storagePosition, Quaternion.identity);
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
        }
    }
}
