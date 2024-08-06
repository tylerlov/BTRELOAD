using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace OccaSoftware.BOP
{
    public class ParticleSystemPooler : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particleSystemPrefab = null;
        [SerializeField, Min(1)] private int initialCount = 10;
        [SerializeField] private Vector3 storagePosition = new Vector3(1000, 1000, 1000); // Offscreen or under the map

        private NativeArray<ParticleSystemWrapper> pool;
        private JobHandle updateJobHandle;

        [System.Serializable]
        public struct ParticleSystemWrapper
        {
            public ParticleSystem particleSystem;
            public bool isPlaying;
        }

        private void Awake()
        {
            pool = new NativeArray<ParticleSystemWrapper>(initialCount, Allocator.Persistent);
            for (int i = 0; i < initialCount; i++)
            {
                CreateNewInstance(i);
            }
        }

        private void CreateNewInstance(int index)
        {
            ParticleSystem newInstance = Instantiate(particleSystemPrefab, storagePosition, Quaternion.identity, transform);
            newInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            newInstance.gameObject.SetActive(false);
            pool[index] = new ParticleSystemWrapper { particleSystem = newInstance, isPlaying = false };
        }

        [BurstCompile]
        private struct UpdateParticleSystemsJob : IJobParallelFor
        {
            public NativeArray<ParticleSystemWrapper> pool;

            public void Execute(int index)
            {
                ParticleSystemWrapper wrapper = pool[index];
                if (wrapper.particleSystem != null)
                {
                    wrapper.isPlaying = wrapper.particleSystem.isPlaying;
                    pool[index] = wrapper;
                }
            }
        }

        private void Update()
        {
            updateJobHandle.Complete();

            var job = new UpdateParticleSystemsJob
            {
                pool = pool
            };

            updateJobHandle = job.Schedule(pool.Length, 64);
        }

        [BurstCompile]
        public ParticleSystem GetFromPool()
        {
            for (int i = 0; i < pool.Length; i++)
            {
                var wrapper = pool[i];
                if (!wrapper.isPlaying)
                {
                    wrapper.particleSystem.gameObject.SetActive(true);
                    wrapper.particleSystem.transform.position = storagePosition;
                    wrapper.isPlaying = true;
                    pool[i] = wrapper;
                    return wrapper.particleSystem;
                }
            }

            // If all instances are in use, create a new array with doubled capacity
            int newCapacity = pool.Length * 2;
            var newPool = new NativeArray<ParticleSystemWrapper>(newCapacity, Allocator.Persistent);
            pool.CopyTo(newPool);
            pool.Dispose();
            pool = newPool;

            // Create and activate the new instance
            int newIndex = pool.Length / 2; // Use the first new slot
            CreateNewInstance(newIndex);
            var newWrapper = pool[newIndex];
            newWrapper.particleSystem.gameObject.SetActive(true);
            newWrapper.isPlaying = true;
            pool[newIndex] = newWrapper;
            return newWrapper.particleSystem;
        }

        [BurstCompile]
        public void ReturnToPool(ParticleSystem ps)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                var wrapper = pool[i];
                if (wrapper.particleSystem == ps)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.transform.position = storagePosition;
                    ps.gameObject.SetActive(false);
                    wrapper.isPlaying = false;
                    pool[i] = wrapper;
                    break;
                }
            }
        }

        private void OnDestroy()
        {
            updateJobHandle.Complete();
            pool.Dispose();
        }
    }
}