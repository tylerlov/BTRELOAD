using System.Collections;
using System.Collections.Generic;
using Chronos;
using SonicBloom.Koreo;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using FMODUnity;  
using FMOD.Studio; 
using System.Linq;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    private List<StaticEnemyShooting> staticEnemyShootings = new List<StaticEnemyShooting>();
    private List<EnemyBasicAI> basicEnemies = new List<EnemyBasicAI>();
    private List<EnemyBasics> registeredEnemies = new List<EnemyBasics>();

    [SerializeField]
    private LayerMask obstacleLayerMask;
    private const int maxRaycastsPerJob = 32;

    [SerializeField, EventID]
    private string unlockEventID;

    [SerializeField, EventID]
    private string lockEventID;

    [Header("Basic Enemy Shooting")]
    [SerializeField] private float minEnemiesPerShot = 1;
    [SerializeField] private float maxEnemiesPerShot = 3;
    [SerializeField] private float shotGroupDelay = 0.2f;

    private Timeline managerTimeline;
    private PlayerLocking playerLocking;
    private bool previousLockLoopState;
    private bool isInLockLoop;

    // Cache FMOD event instance
    private EventInstance musicEventInstance;
    private const string LOCK_STATE_PARAMETER = "Lock State";
    private PARAMETER_ID lockStateParameterId;
    private bool isParameterIdInitialized;

    // Add FMOD update throttling
    private const float FMOD_UPDATE_INTERVAL = 0.1f; // Update every 100ms instead of every frame
    private float lastFMODUpdateTime;
    private bool pendingLockStateUpdate;
    private float targetLockValue;
    private float currentLockValue;
    private const float LOCK_VALUE_SMOOTH_SPEED = 5f;

    private const int FMOD_BATCH_SIZE = 32;
    private Queue<FMODParameterUpdate> fmodUpdateQueue = new Queue<FMODParameterUpdate>();
    private Dictionary<string, float> cachedParameters = new Dictionary<string, float>();
    
    private struct FMODParameterUpdate
    {
        public PARAMETER_ID ParameterId;
        public float Value;
    }

    private const int SHOOT_BATCH_SIZE = 25;
    private bool isProcessingShooting = false;
    private bool isProcessingBasicEnemies = false;
    private Queue<StaticEnemyShooting> shootingQueue = new Queue<StaticEnemyShooting>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            managerTimeline = GetComponent<Timeline>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(InitializeFMODDelayed());
        InitializePlayerLocking();
    }

    private IEnumerator InitializeFMODDelayed()
    {
        // Wait for the first frame to complete
        yield return null;

        var musicEmitter = GameObject.Find("FMOD Music")?.GetComponent<StudioEventEmitter>();
        if (musicEmitter != null)
        {
            musicEventInstance = musicEmitter.EventInstance;
            // Only initialize the parameter ID when we first need it
            isParameterIdInitialized = false;
        }
    }

    private void EnsureParameterInitialized()
    {
        if (!isParameterIdInitialized && musicEventInstance.isValid())
        {
            musicEventInstance.getDescription(out EventDescription eventDescription);
            eventDescription.getParameterDescriptionByName(LOCK_STATE_PARAMETER, out PARAMETER_DESCRIPTION parameterDescription);
            lockStateParameterId = parameterDescription.id;
            isParameterIdInitialized = true;
            
            // Cache initial parameter value
            if (musicEventInstance.getParameterByName(LOCK_STATE_PARAMETER, out float initialValue) == FMOD.RESULT.OK)
            {
                cachedParameters[LOCK_STATE_PARAMETER] = initialValue;
            }
        }
    }

    private void InitializePlayerLocking()
    {
        playerLocking = PlayerLocking.Instance;
        if (playerLocking == null)
        {
            ConditionalDebug.LogError("[EnemyManager] Could not find PlayerLocking instance");
            return;
        }
        previousLockLoopState = false;
    }

    private void OnEnable()
    {
        Koreographer.Instance.RegisterForEvents(unlockEventID, OnMusicalEnemyShootUnlocked);
        Koreographer.Instance.RegisterForEvents(lockEventID, OnMusicalEnemyShootLocked);
    }

    private void OnDisable()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.UnregisterForEvents(unlockEventID, OnMusicalEnemyShootUnlocked);
            Koreographer.Instance.UnregisterForEvents(lockEventID, OnMusicalEnemyShootLocked);
        }
    }

    public void RegisterStaticEnemyShooting(StaticEnemyShooting shooting)
    {
        if (!staticEnemyShootings.Contains(shooting))
        {
            staticEnemyShootings.Add(shooting);
        }
    }

    public void RegisterBasicEnemy(EnemyBasicAI enemy)
    {
        if (!basicEnemies.Contains(enemy))
        {
            basicEnemies.Add(enemy);
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
        EnsureParameterInitialized();

        if (playerLocking != null)
        {
            bool newLockState = playerLocking.GetLockedProjectileCount() > 0;
            
            if (newLockState != isInLockLoop)
            {
                isInLockLoop = newLockState;
                targetLockValue = isInLockLoop ? 1f : 0f;
                previousLockLoopState = isInLockLoop;
            }

            if (Mathf.Abs(currentLockValue - targetLockValue) > 0.001f)
            {
                currentLockValue = Mathf.MoveTowards(currentLockValue, targetLockValue, 
                    LOCK_VALUE_SMOOTH_SPEED * Time.deltaTime);
                
                if (Time.time - lastFMODUpdateTime >= FMOD_UPDATE_INTERVAL)
                {
                    UpdateFMODLockState(currentLockValue);
                    lastFMODUpdateTime = Time.time;
                }
            }
        }
        
        ProcessFMODUpdates();
        CheckLineOfSightBatched();
    }

    private void UpdateFMODLockState(float lockValue)
    {
        if (!isParameterIdInitialized || !musicEventInstance.isValid())
            return;

        // Only queue update if value has changed
        if (!cachedParameters.TryGetValue(LOCK_STATE_PARAMETER, out float currentValue) 
            || Mathf.Abs(currentValue - lockValue) > 0.001f)
        {
            fmodUpdateQueue.Enqueue(new FMODParameterUpdate 
            { 
                ParameterId = lockStateParameterId, 
                Value = lockValue 
            });
            cachedParameters[LOCK_STATE_PARAMETER] = lockValue;
        }
    }

    private void ProcessFMODUpdates()
    {
        if (!musicEventInstance.isValid() || fmodUpdateQueue.Count == 0)
            return;

        int updatesProcessed = 0;
        while (fmodUpdateQueue.Count > 0 && updatesProcessed < FMOD_BATCH_SIZE)
        {
            var update = fmodUpdateQueue.Dequeue();
            try
            {
                musicEventInstance.setParameterByID(update.ParameterId, update.Value);
                updatesProcessed++;
            }
            catch (System.Exception e)
            {
                ConditionalDebug.LogError($"[EnemyManager] FMOD parameter update failed: {e.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        if (musicEventInstance.isValid())
        {
            // Ensure clean parameter state
            musicEventInstance.setParameterByID(lockStateParameterId, 0f);
        }
    }

    private void OnMusicalEnemyShoot(KoreographyEvent evt, bool isLocked)
    {
        if (isLocked != isInLockLoop) return;

        try
        {
            // Clean up null references in static enemies
            if (staticEnemyShootings.Count != staticEnemyShootings.Count(x => x != null))
            {
                staticEnemyShootings.RemoveAll(x => x == null);
            }

            // Clean up null references in basic enemies
            if (basicEnemies.Count != basicEnemies.Count(x => x != null))
            {
                basicEnemies.RemoveAll(x => x == null);
            }

            // Queue all active static shooters
            foreach (var shooting in staticEnemyShootings)
            {
                if (shooting != null && shooting.gameObject.activeInHierarchy)
                {
                    shootingQueue.Enqueue(shooting);
                }
            }

            // Process basic enemies in a controlled manner
            if (!isProcessingBasicEnemies)
            {
                StartCoroutine(ProcessBasicEnemyShooting());
            }

            // Start processing if not already running
            if (!isProcessingShooting)
            {
                StartCoroutine(ProcessShootingQueue());
            }
        }
        catch (System.Exception e)
        {
            ConditionalDebug.LogError($"[EnemyManager] Critical error: {e.Message}");
        }
    }

    private IEnumerator ProcessBasicEnemyShooting()
    {
        isProcessingBasicEnemies = true;

        // Get all enemies that can attack
        var availableEnemies = basicEnemies
            .Where(e => e != null && 
                   e.gameObject.activeInHierarchy && 
                   e.GetComponent<EnemyBasicCombat>()?.CanAttack() == true)
            .ToList();

        while (availableEnemies.Count > 0)
        {
            // Randomly select number of enemies to shoot in this group
            int enemiesToShoot = Mathf.Min(
                Random.Range((int)minEnemiesPerShot, (int)maxEnemiesPerShot + 1),
                availableEnemies.Count
            );

            // Randomly select and process enemies
            for (int i = 0; i < enemiesToShoot; i++)
            {
                int index = Random.Range(0, availableEnemies.Count);
                var enemy = availableEnemies[index];
                
                if (enemy != null)
                {
                    var combat = enemy.GetComponent<EnemyBasicCombat>();
                    if (combat != null)
                    {
                        combat.TryAttack();
                    }
                }
                
                availableEnemies.RemoveAt(index);
            }

            // Wait before processing next group
            yield return new WaitForSeconds(shotGroupDelay);

            // Update available enemies list
            availableEnemies = basicEnemies
                .Where(e => e != null && 
                       e.gameObject.activeInHierarchy && 
                       e.GetComponent<EnemyBasicCombat>()?.CanAttack() == true)
                .ToList();
        }

        isProcessingBasicEnemies = false;
    }

    private IEnumerator ProcessShootingQueue()
    {
        isProcessingShooting = true;

        while (shootingQueue.Count > 0)
        {
            int batchCount = Mathf.Min(SHOOT_BATCH_SIZE, shootingQueue.Count);
            Vector3[] shooterPositions = new Vector3[batchCount];
            
            for (int i = 0; i < batchCount; i++)
            {
                if (shootingQueue.Count == 0) break;
                
                var shooter = shootingQueue.Dequeue();
                try
                {
                    if (shooter != null)
                    {
                        shooterPositions[i] = shooter.transform.position;
                        shooter.Shoot();
                    }
                }
                catch (System.Exception e)
                {
                    ConditionalDebug.LogError($"[EnemyManager] Error during shooting: {e.Message}");
                }
            }

            ProjectileAudioManager.Instance?.PlayGroupProjectileSound(shooterPositions);
            yield return new WaitForEndOfFrame();
        }

        isProcessingShooting = false;
    }

    private void OnMusicalEnemyShootUnlocked(KoreographyEvent evt)
    {
        OnMusicalEnemyShoot(evt, false);
    }

    private void OnMusicalEnemyShootLocked(KoreographyEvent evt)
    {
        OnMusicalEnemyShoot(evt, true);
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
                    float distance = direction.magnitude;
                    direction.Normalize();
                    
                    commands[i] = new RaycastCommand(
                        origin,           // From position
                        direction,        // Direction
                        distance,         // Max distance
                        obstacleLayerMask // Layer mask
                    );
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

    public void RegisterEnemy(EnemyBasics enemy)
    {
        if (!registeredEnemies.Contains(enemy))
        {
            registeredEnemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(EnemyBasics enemy)
    {
        if (enemy != null && registeredEnemies.Contains(enemy))
        {
            registeredEnemies.Remove(enemy);
        }
    }
}
