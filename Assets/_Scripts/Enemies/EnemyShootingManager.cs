using System.Collections.Generic;
using Chronos;
using SonicBloom.Koreo;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using FMODUnity;  // Add this for StudioEventEmitter
using FMOD.Studio; // Add this for EventInstance

public class EnemyShootingManager : MonoBehaviour
{
    public static EnemyShootingManager Instance { get; private set; }

    private List<StaticEnemyShooting> staticEnemyShootings = new List<StaticEnemyShooting>();
    private List<EnemyBasicAI> basicEnemies = new List<EnemyBasicAI>();

    [SerializeField, EventID]
    private string eventID;
    private int shootCounter = 0;

    private Timeline managerTimeline;

    private int koreographerEventCount = 0;
    private float lastLogTime = 0f;
    private const float LOG_INTERVAL = 5f; // Log every 5 seconds

    [SerializeField]
    private LayerMask obstacleLayerMask;

    [SerializeField]
    private int maxRaycastsPerJob = 1024;

    private StudioEventEmitter musicEmitter;
    private bool isInLockLoop = false;
    private float lastLoopPosition = 0f;
    private float loopLength = 0f;
    private const float LOOP_THRESHOLD = 0.1f; // Threshold to detect loop points

    [SerializeField]
    private float defaultLoopLength = 2000f; // Default value in milliseconds

    private const float MINIMUM_SHOOT_INTERVAL = 700f; // 700ms
    private bool shouldSkipNextShot = false;
    private float currentLoopDuration = 0f;

    private List<EnemyBasicSetup> registeredEnemies = new List<EnemyBasicSetup>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            managerTimeline = GetComponent<Timeline>();
            ConditionalDebug.Log("EnemyShootingManager initialized");
        }
        else
        {
            ConditionalDebug.Log("Duplicate EnemyShootingManager found, destroying");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        musicEmitter = GameObject.Find("FMOD Music")?.GetComponent<StudioEventEmitter>();
        if (musicEmitter == null)
        {
            ConditionalDebug.LogError("[EnemyShootingManager] Could not find FMOD Music emitter");
            return;
        }

        // Instead of trying to get loop points from FMOD, we'll use position tracking
        loopLength = defaultLoopLength;
        ConditionalDebug.Log($"[EnemyShootingManager] Using default loop length: {loopLength}ms");

        StartCoroutine(CheckMusicLoopStateRoutine());
    }

    private System.Collections.IEnumerator CheckMusicLoopStateRoutine()
    {
        WaitForSeconds waitTime = new WaitForSeconds(0.1f);
        int lastPosition = 0;
        bool positionDecreased = false;
        int loopStartPosition = 0;

        while (true)
        {
            if (musicEmitter != null && musicEmitter.EventInstance.isValid())
            {
                int currentPosition = 0;
                musicEmitter.EventInstance.getTimelinePosition(out currentPosition);
                
                float lockState = 0f;
                musicEmitter.EventInstance.getParameterByName("Lock State", out lockState);
                bool isLocked = lockState > 0.5f;

                if (isLocked)
                {
                    if (!isInLockLoop)
                    {
                        // Just entered lock state
                        isInLockLoop = true;
                        lastPosition = currentPosition;
                        loopStartPosition = currentPosition;
                        ConditionalDebug.Log($"[EnemyShootingManager] Entered lock loop at position: {currentPosition}ms");
                        TriggerAllStaticEnemies();
                    }
                    else
                    {
                        // Detect if position has decreased (indicating a loop)
                        if (currentPosition < lastPosition)
                        {
                            positionDecreased = true;
                            currentLoopDuration = lastPosition - loopStartPosition;
                            loopStartPosition = currentPosition;
                            
                            ConditionalDebug.Log($"[EnemyShootingManager] Loop detected. Duration: {currentLoopDuration}ms");

                            if (currentLoopDuration < MINIMUM_SHOOT_INTERVAL)
                            {
                                if (!shouldSkipNextShot)
                                {
                                    TriggerAllStaticEnemies();
                                    shouldSkipNextShot = true;
                                    ConditionalDebug.Log("[EnemyShootingManager] Shot triggered, skipping next loop");
                                }
                                else
                                {
                                    shouldSkipNextShot = false;
                                    ConditionalDebug.Log("[EnemyShootingManager] Skipping shot this loop");
                                }
                            }
                            else
                            {
                                TriggerAllStaticEnemies();
                                ConditionalDebug.Log("[EnemyShootingManager] Loop duration sufficient, shooting normally");
                            }
                        }
                        else if (positionDecreased && (currentPosition - lastPosition) > 1000)
                        {
                            positionDecreased = false;
                            lastPosition = currentPosition;
                        }
                    }
                }
                else if (isInLockLoop)
                {
                    // Reset state when exiting lock loop
                    isInLockLoop = false;
                    positionDecreased = false;
                    shouldSkipNextShot = false;
                    ConditionalDebug.Log("[EnemyShootingManager] Exited lock loop");
                }

                lastPosition = currentPosition;
            }

            yield return waitTime;
        }
    }

    private void OnEnable()
    {
        Koreographer.Instance.RegisterForEvents(eventID, OnMusicalEnemyShoot);
    }

    private void OnDisable()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.UnregisterForEvents(eventID, OnMusicalEnemyShoot);
        }
    }

    public void RegisterStaticEnemyShooting(StaticEnemyShooting shooting)
    {
        if (!staticEnemyShootings.Contains(shooting))
        {
            staticEnemyShootings.Add(shooting);
        }
        else
        {
            ConditionalDebug.Log($"StaticEnemyShooting {shooting.name} already registered");
        }
    }

    public void RegisterBasicEnemy(EnemyBasicAI enemy)
    {
        if (!basicEnemies.Contains(enemy))
        {
            basicEnemies.Add(enemy);
        }
        else
        {
            ConditionalDebug.Log($"EnemyBasicAI {enemy.name} already registered");
        }
    }

    public void UnregisterStaticEnemyShooting(StaticEnemyShooting shooting)
    {
        if (shooting != null && staticEnemyShootings.Contains(shooting))
        {
            staticEnemyShootings.Remove(shooting);
        }
    }

    public void UnregisterBasicEnemy(EnemyBasicAI enemy)
    {
        if (enemy != null && basicEnemies.Contains(enemy))
        {
            basicEnemies.Remove(enemy);
        }
    }

    public void UnregisterAllStaticEnemyShootingsFromKoreographer()
    {
        foreach (var shooting in staticEnemyShootings)
        {
            //shooting.UnregisterFromKoreographer();
        }
    }

    public float GetCurrentTime()
    {
        return managerTimeline != null ? managerTimeline.time : Time.time;
    }

    private void Update()
    {
        if (Time.time - lastLogTime >= LOG_INTERVAL)
        {
            ConditionalDebug.Log($"[EnemyShootingManager] Koreographer events in the last {LOG_INTERVAL} seconds: {koreographerEventCount}");
            ConditionalDebug.Log($"[EnemyShootingManager] Current registered StaticEnemyShooting count: {staticEnemyShootings.Count}");
            ConditionalDebug.Log($"[EnemyShootingManager] Current registered EnemyBasicAI count: {basicEnemies.Count}");
            ConditionalDebug.Log($"[EnemyShootingManager] Current registered Enemies count: {registeredEnemies.Count}");
            lastLogTime = Time.time;
            koreographerEventCount = 0;
        }

        CheckLineOfSightBatched();
    }

    private void TriggerAllStaticEnemies()
    {
        string debugInfo = "[EnemyShootingManager] Triggering all static enemies on loop:\n";
        
        if (staticEnemyShootings == null || staticEnemyShootings.Count == 0)
        {
            ConditionalDebug.Log($"{debugInfo} No static enemies to trigger");
            return;
        }

        int successfulShots = 0;
        int inactiveCount = 0;
        int nullCount = 0;
        int errorCount = 0;

        foreach (var shooting in staticEnemyShootings.ToArray())
        {
            if (shooting == null)
            {
                nullCount++;
                continue;
            }

            if (!shooting.gameObject.activeInHierarchy)
            {
                inactiveCount++;
                continue;
            }

            try
            {
                shooting.Shoot();
                successfulShots++;
            }
            catch (System.Exception e)
            {
                errorCount++;
                ConditionalDebug.LogError($"[EnemyShootingManager] Error triggering enemy: {e.Message}");
            }
        }

        ConditionalDebug.Log($"{debugInfo}Results: Success={successfulShots}, Inactive={inactiveCount}, Null={nullCount}, Errors={errorCount}");
    }

    private void OnMusicalEnemyShoot(KoreographyEvent evt)
    {
        // Process this event if we're not in a lock loop
        if (!isInLockLoop)
        {
            try
            {
                koreographerEventCount++;
                string debugInfo = "[EnemyShootingManager] OnMusicalEnemyShoot triggered:\n";
                
                if (managerTimeline == null)
                {
                    ConditionalDebug.LogWarning($"{debugInfo} Timeline is null");
                    return;
                }

                if (managerTimeline.timeScale == 0f)
                {
                    ConditionalDebug.Log($"{debugInfo} Timeline is paused");
                    return;
                }

                if (staticEnemyShootings == null)
                {
                    ConditionalDebug.LogError($"{debugInfo} staticEnemyShootings list is null");
                    return;
                }

                debugInfo += $"- Registered shooters count: {staticEnemyShootings.Count}\n";
                if (staticEnemyShootings.Count == 0)
                {
                    ConditionalDebug.Log($"{debugInfo} No static enemies registered");
                    return;
                }

                // Remove any null entries and log removed count
                int beforeCount = staticEnemyShootings.Count;
                staticEnemyShootings.RemoveAll(x => x == null);
                int removedCount = beforeCount - staticEnemyShootings.Count;
                if (removedCount > 0)
                {
                    debugInfo += $"- Removed {removedCount} null entries\n";
                }

                int successfulShots = 0;
                int inactiveCount = 0;
                int nullCount = 0;
                int errorCount = 0;

                // Create copy to avoid modification during iteration
                var shooters = staticEnemyShootings.ToArray();
                foreach (var shooting in shooters)
                {
                    if (shooting == null)
                    {
                        nullCount++;
                        continue;
                    }

                    if (!shooting.gameObject.activeInHierarchy)
                    {
                        inactiveCount++;
                        debugInfo += $"- Shooter {shooting.name} is inactive\n";
                        continue;
                    }

                    try
                    {
                        ConditionalDebug.Log($"[EnemyShootingManager] Attempting to shoot with {shooting.name}");
                        shooting.Shoot();
                        successfulShots++;
                    }
                    catch (System.Exception e)
                    {
                        errorCount++;
                        debugInfo += $"- Error with shooter {shooting.name}: {e.Message}\n";
                        ConditionalDebug.LogError($"[EnemyShootingManager] Error during shooting with {shooting.name}: {e.Message}");
                    }
                }

                debugInfo += $"- Results: Success={successfulShots}, Inactive={inactiveCount}, Null={nullCount}, Errors={errorCount}";
                ConditionalDebug.Log(debugInfo);
                TriggerAllStaticEnemies();
            }
            catch (System.Exception e)
            {
                ConditionalDebug.LogError($"[EnemyShootingManager] Critical error in OnMusicalEnemyShoot: {e.Message}\n{e.StackTrace}");
            }
        }
        else
        {
            ConditionalDebug.Log("[EnemyShootingManager] Skipping Koreographer event during lock loop");
        }
    }

    private void CheckLineOfSightBatched()
    {
        int enemyCount = registeredEnemies.Count;
        if (enemyCount == 0) return;

        Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null) return;

        int batchSize = Mathf.Min(enemyCount, maxRaycastsPerJob);
        int batchCount = Mathf.CeilToInt((float)enemyCount / batchSize);

        for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
        {
            int startIndex = batchIndex * batchSize;
            int endIndex = Mathf.Min(startIndex + batchSize, enemyCount);
            int currentBatchSize = endIndex - startIndex;

            NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(currentBatchSize, Allocator.TempJob);
            NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(currentBatchSize, Allocator.TempJob);

            // Setup raycast commands
            for (int i = 0; i < currentBatchSize; i++)
            {
                int enemyIndex = startIndex + i;
                if (registeredEnemies[enemyIndex] != null && registeredEnemies[enemyIndex].gameObject.activeInHierarchy)
                {
                    Vector3 origin = registeredEnemies[enemyIndex].transform.position;
                    Vector3 direction = playerTransform.position - origin;
                    commands[i] = new RaycastCommand(origin, direction.normalized, direction.magnitude, obstacleLayerMask);
                }
            }

            // Schedule and complete the batch
            JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, 1, default(JobHandle));
            handle.Complete();

            // Process results
            for (int i = 0; i < currentBatchSize; i++)
            {
                int enemyIndex = startIndex + i;
                if (registeredEnemies[enemyIndex] != null && registeredEnemies[enemyIndex].gameObject.activeInHierarchy)
                {
                    bool hasLineOfSight = !results[i].collider;
                    registeredEnemies[enemyIndex].HandleLineOfSightResult(hasLineOfSight);
                }
            }

            commands.Dispose();
            results.Dispose();
        }
    }

    public void RegisterEnemy(EnemyBasicSetup enemy)
    {
        if (!registeredEnemies.Contains(enemy))
        {
            registeredEnemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(EnemyBasicSetup enemy)
    {
        registeredEnemies.Remove(enemy);
    }
}
