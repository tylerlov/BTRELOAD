using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class ProjectileJobSystem : MonoBehaviour
{
    private NativeArray<int> projectileIds;
    private NativeHashMap<int, float> projectileLifetimes;
    private NativeArray<float> updatedLifetimes;
    private JobHandle updateJobHandle;
    private JobHandle _updateProjectilesJobHandle;
    private bool _isJobRunning = false;

    [SerializeField] private int maxRaycastsPerJob = 500;
    [SerializeField] private int subSteps = 1;
    [SerializeField] private float maxRaycastDistance = 3f;
    [SerializeField] private bool useRaycastsOnlyForHoming = true;
    [SerializeField] private bool useRaycastsForPlayerShots = false;

    private NativeArray<RaycastCommand> raycastCommands;
    private NativeArray<RaycastHit> raycastResults;
    private TransformAccessArray transformAccessArray;

    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private LayerMask playerLayerMask;

    private const int INITIAL_CAPACITY = 1000;
    private const int GROWTH_FACTOR = 2;

    private ProjectileManager projectileManager;

    private void Awake()
    {
        projectileManager = GetComponent<ProjectileManager>();
        InitializeJobSystem();
    }

    private void InitializeJobSystem()
    {
        projectileIds = new NativeArray<int>(INITIAL_CAPACITY, Allocator.Persistent);
        projectileLifetimes = new NativeHashMap<int, float>(INITIAL_CAPACITY, Allocator.Persistent);
        updatedLifetimes = new NativeArray<float>(INITIAL_CAPACITY, Allocator.Persistent);
        
        raycastCommands = new NativeArray<RaycastCommand>(maxRaycastsPerJob, Allocator.Persistent);
        raycastResults = new NativeArray<RaycastHit>(maxRaycastsPerJob, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        SafeDispose();
    }

    private void SafeDispose()
    {
        if (projectileIds.IsCreated) projectileIds.Dispose();
        if (projectileLifetimes.IsCreated) projectileLifetimes.Dispose();
        if (updatedLifetimes.IsCreated) updatedLifetimes.Dispose();
        if (raycastCommands.IsCreated) raycastCommands.Dispose();
        if (raycastResults.IsCreated) raycastResults.Dispose();
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
    }

    public void ScheduleProjectileUpdate(float deltaTime)
    {
        if (_isJobRunning)
        {
            _updateProjectilesJobHandle.Complete();
        }

        // Schedule the job
        var job = new UpdateProjectilesJob
        {
            deltaTime = deltaTime,
            projectileIds = projectileIds,
            projectileLifetimes = projectileLifetimes,
            updatedLifetimes = updatedLifetimes
        };

        _updateProjectilesJobHandle = job.Schedule();
        _isJobRunning = true;
    }

    public void CompleteProjectileUpdate()
    {
        if (_isJobRunning)
        {
            _updateProjectilesJobHandle.Complete();
            _isJobRunning = false;
        }
    }

    [BurstCompile]
    private struct UpdateProjectilesJob : IJob
    {
        public float deltaTime;
        public NativeArray<int> projectileIds;
        [ReadOnly] public NativeHashMap<int, float> projectileLifetimes;
        public NativeArray<float> updatedLifetimes;

        public void Execute()
        {
            for (int i = 0; i < projectileIds.Length; i++)
            {
                int id = projectileIds[i];
                if (projectileLifetimes.TryGetValue(id, out float lifetime))
                {
                    updatedLifetimes[i] = lifetime - deltaTime;
                }
            }
        }
    }
}
