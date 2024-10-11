using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using System.Collections.Generic;

public class ParticleManager : MonoBehaviour
{
    [SerializeField] private int maxParticleSystems = 100;
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private float distanceThreshold = 50f;

    private List<ParticleSystem> activeSystems = new List<ParticleSystem>();
    private ObjectPool<ParticleSystem> particleSystemPool;
    private NativeArray<Vector3> particlePositions;
    private JobHandle updateJobHandle;

    private void Start()
    {
        particleSystemPool = new ObjectPool<ParticleSystem>(CreateParticleSystem, maxParticleSystems);
        particlePositions = new NativeArray<Vector3>(maxParticleSystems, Allocator.Persistent);
        
        InvokeRepeating(nameof(ScheduleParticleUpdateJob), 0f, updateInterval);
    }

    private ParticleSystem CreateParticleSystem()
    {
        // Create and configure a new particle system
        // Return the created particle system
    }

    private void ScheduleParticleUpdateJob()
    {
        // Update particle positions
        for (int i = 0; i < activeSystems.Count; i++)
        {
            particlePositions[i] = activeSystems[i].transform.position;
        }

        // Create and schedule the job
        var updateJob = new ParticleUpdateJob
        {
            positions = particlePositions,
            playerPosition = transform.position,
            distanceThreshold = distanceThreshold,
            deltaTime = Time.deltaTime
        };

        updateJobHandle = updateJob.Schedule(activeSystems.Count, 64);
    }

    private void LateUpdate()
    {
        // Complete the job and update particle systems
        updateJobHandle.Complete();

        for (int i = activeSystems.Count - 1; i >= 0; i--)
        {
            var system = activeSystems[i];
            if (Vector3.Distance(system.transform.position, transform.position) > distanceThreshold)
            {
                // Deactivate distant particle systems
                system.Stop();
                particleSystemPool.Release(system);
                activeSystems.RemoveAt(i);
            }
            else
            {
                // Update active particle systems
                system.Simulate(Time.deltaTime, true, false);
            }
        }
    }

    public void SpawnParticleSystem(Vector3 position)
    {
        if (activeSystems.Count < maxParticleSystems)
        {
            var system = particleSystemPool.Get();
            system.transform.position = position;
            system.Play();
            activeSystems.Add(system);
        }
    }

    private void OnDestroy()
    {
        if (particlePositions.IsCreated)
            particlePositions.Dispose();
    }
}

public struct ParticleUpdateJob : IJobParallelFor
{
    public NativeArray<Vector3> positions;
    public Vector3 playerPosition;
    public float distanceThreshold;
    public float deltaTime;

    public void Execute(int index)
    {
        // Perform any necessary calculations or updates for each particle system
        // This is a simplified example; adjust based on your specific needs
        if (Vector3.Distance(positions[index], playerPosition) <= distanceThreshold)
        {
            // Perform updates for active particle systems
            // For example, you might update particle positions, velocities, etc.
        }
    }
}