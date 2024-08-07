using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;

namespace OccaSoftware.BOP
{
    public class ParticleSystemPooler : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particleSystemPrefab = null;
        [SerializeField, Min(1)] private int initialCount = 10;
        [SerializeField] private Vector3 storagePosition = new Vector3(1000, 1000, 1000); // Offscreen or under the map

        private NativeArray<ParticleSystemWrapper> pool;
        private JobHandle updateJobHandle;
        private Dictionary<int, ParticleSystem> particleSystemMap;
        private int nextId = 0;

        [System.Serializable]
        public struct ParticleSystemWrapper
        {
            public int id;
            public bool isPlaying;
        }

        private void Awake()
        {
            pool = new NativeArray<ParticleSystemWrapper>(initialCount, Allocator.Persistent);
            particleSystemMap = new Dictionary<int, ParticleSystem>();
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
            int id = nextId++;
            particleSystemMap[id] = newInstance;
            pool[index] = new ParticleSystemWrapper { id = id, isPlaying = false };
        }

        [BurstCompile]
        private struct UpdateParticleSystemsJob : IJobParallelFor
        {
            public NativeArray<ParticleSystemWrapper> pool;

            public void Execute(int index)
            {
                ParticleSystemWrapper wrapper = pool[index];
                // We can't check isPlaying here, so we'll update this in the main thread
                pool[index] = wrapper;
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

            // Update isPlaying status on the main thread
            for (int i = 0; i < pool.Length; i++)
            {
                var wrapper = pool[i];
                if (particleSystemMap.TryGetValue(wrapper.id, out var ps))
                {
                    wrapper.isPlaying = ps.isPlaying;
                    pool[i] = wrapper;
                }
            }
        }

        [BurstCompile]
        public ParticleSystem GetFromPool()
        {
            for (int i = 0; i < pool.Length; i++)
            {
                var wrapper = pool[i];
                if (!wrapper.isPlaying && particleSystemMap.TryGetValue(wrapper.id, out var ps))
                {
                    ps.gameObject.SetActive(true);
                    ps.transform.position = storagePosition;
                    wrapper.isPlaying = true;
                    pool[i] = wrapper;
                    return ps;
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
            var newPs = particleSystemMap[newWrapper.id];
            newPs.gameObject.SetActive(true);
            newWrapper.isPlaying = true;
            pool[newIndex] = newWrapper;
            return newPs;
        }

        [BurstCompile]
        public void ReturnToPool(ParticleSystem ps)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                var wrapper = pool[i];
                if (particleSystemMap.TryGetValue(wrapper.id, out var pooledPs) && pooledPs == ps)
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