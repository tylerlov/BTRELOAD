using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;

namespace OccaSoftware.BOP
{
    [BurstCompile]
    public class ParticleSystemPooler : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particleSystemPrefab = null;
        [SerializeField, Min(1)] private int initialCount = 10;
        [SerializeField] private Vector3 storagePosition = new Vector3(1000, 1000, 1000); // Offscreen or under the map

        private NativeArray<ParticleSystemWrapper> jobPool;
        private NativeArray<ParticleSystemWrapper> mainThreadPool;
        private JobHandle updateJobHandle;
        private bool isJobRunning = false;
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
            jobPool = new NativeArray<ParticleSystemWrapper>(initialCount, Allocator.Persistent);
            mainThreadPool = new NativeArray<ParticleSystemWrapper>(initialCount, Allocator.Persistent);
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
            jobPool[index] = new ParticleSystemWrapper { id = id, isPlaying = false };
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
            if (isJobRunning)
            {
                // Complete the previous job before accessing the data
                updateJobHandle.Complete();
                isJobRunning = false;

                // Copy data from job to main thread array
                jobPool.CopyTo(mainThreadPool);
            }

            // Update isPlaying status on the main thread using mainThreadPool
            for (int i = 0; i < mainThreadPool.Length; i++)
            {
                var wrapper = mainThreadPool[i];
                if (particleSystemMap.TryGetValue(wrapper.id, out var ps))
                {
                    wrapper.isPlaying = ps.isPlaying;
                    mainThreadPool[i] = wrapper;
                }
            }

            // Schedule the new job
            var job = new UpdateParticleSystemsJob
            {
                pool = jobPool
            };

            updateJobHandle = job.Schedule(jobPool.Length, 64);
            isJobRunning = true;

            // Copy updated data to the job array
            mainThreadPool.CopyTo(jobPool);
        }

        [BurstCompile]
        public ParticleSystem GetFromPool()
        {
            if (isJobRunning)
            {
                updateJobHandle.Complete();
                isJobRunning = false;
                jobPool.CopyTo(mainThreadPool);
            }

            for (int i = 0; i < mainThreadPool.Length; i++)
            {
                var wrapper = mainThreadPool[i];
                if (!wrapper.isPlaying && particleSystemMap.TryGetValue(wrapper.id, out var ps))
                {
                    ps.gameObject.SetActive(true);
                    ps.transform.position = storagePosition;
                    wrapper.isPlaying = true;
                    mainThreadPool[i] = wrapper;
                    return ps;
                }
            }

            // If all instances are in use, create a new array with doubled capacity
            int newCapacity = mainThreadPool.Length * 2;
            var newPool = new NativeArray<ParticleSystemWrapper>(newCapacity, Allocator.Persistent);
            mainThreadPool.CopyTo(newPool);
            mainThreadPool.Dispose();
            jobPool.Dispose();
            mainThreadPool = newPool;
            jobPool = new NativeArray<ParticleSystemWrapper>(newCapacity, Allocator.Persistent);

            // Create and activate the new instance
            int newIndex = mainThreadPool.Length / 2; // Use the first new slot
            CreateNewInstance(newIndex);
            var newWrapper = mainThreadPool[newIndex];
            var newPs = particleSystemMap[newWrapper.id];
            newPs.gameObject.SetActive(true);
            newWrapper.isPlaying = true;
            mainThreadPool[newIndex] = newWrapper;
            return newPs;
        }

        [BurstCompile]
        public void ReturnToPool(ParticleSystem ps)
        {
            if (isJobRunning)
            {
                updateJobHandle.Complete();
                isJobRunning = false;
                jobPool.CopyTo(mainThreadPool);
            }

            for (int i = 0; i < mainThreadPool.Length; i++)
            {
                var wrapper = mainThreadPool[i];
                if (particleSystemMap.TryGetValue(wrapper.id, out var pooledPs) && pooledPs == ps)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.transform.position = storagePosition;
                    ps.gameObject.SetActive(false);
                    wrapper.isPlaying = false;
                    mainThreadPool[i] = wrapper;
                    break;
                }
            }
        }

        private void OnDestroy()
        {
            if (isJobRunning)
            {
                updateJobHandle.Complete();
            }
            jobPool.Dispose();
            mainThreadPool.Dispose();
        }
    }
}