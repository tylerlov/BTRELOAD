using System;
using System.Collections;
using System.Collections.Generic;
using Chronos;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Text;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    [SerializeField]
    private int staticShootingRequestsPerFrame = 10;

    private Dictionary<int, ProjectileStateBased> projectileLookup =
        new Dictionary<int, ProjectileStateBased>();

    // Spatial partitioning grid system
    private const float GRID_CELL_SIZE = 10f; // Increased from 5f to better handle fast-moving projectiles
    private Dictionary<Vector2Int, HashSet<ProjectileStateBased>> playerProjectileGrid = new Dictionary<Vector2Int, HashSet<ProjectileStateBased>>();
    private Dictionary<Vector2Int, HashSet<ProjectileStateBased>> enemyProjectileGrid = new Dictionary<Vector2Int, HashSet<ProjectileStateBased>>();

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugGrid = false;
    [SerializeField] private bool showProjectileConnections = false;
    [SerializeField] private Color gridColor = new Color(0.2f, 1f, 0.2f, 0.1f);
    [SerializeField] private Color connectionColor = new Color(1f, 0f, 0f, 0.5f);

    private Vector2Int GetGridCell(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / GRID_CELL_SIZE),
            Mathf.FloorToInt(worldPosition.z / GRID_CELL_SIZE)
        );
    }

    private void UpdateProjectileGridPosition(ProjectileStateBased projectile, Vector3 oldPosition, Vector3 newPosition)
    {
        Vector2Int oldCell = GetGridCell(oldPosition);
        Vector2Int newCell = GetGridCell(newPosition);

        // If the projectile hasn't changed cells, no need to update
        if (oldCell == newCell) return;

        var grid = projectile.isPlayerShot ? playerProjectileGrid : enemyProjectileGrid;

        // Remove from old cell
        if (grid.ContainsKey(oldCell))
        {
            grid[oldCell].Remove(projectile);
            LogGridOperation("Remove", oldCell, projectile);
            if (grid[oldCell].Count == 0)
            {
                grid.Remove(oldCell);
            }
        }

        // Add to new cell
        if (!grid.ContainsKey(newCell))
        {
            grid[newCell] = new HashSet<ProjectileStateBased>();
        }
        grid[newCell].Add(projectile);
        LogGridOperation("Add", newCell, projectile);
    }

    private HashSet<ProjectileStateBased> tempProjectileSet = new HashSet<ProjectileStateBased>();
    private List<ProjectileStateBased> playerProjectiles = new List<ProjectileStateBased>();
    private List<ProjectileStateBased> enemyProjectiles = new List<ProjectileStateBased>();
    private StringBuilder logBuilder = new StringBuilder(256);

    private void UpdateProjectileLists()
    {
        playerProjectiles.Clear();
        enemyProjectiles.Clear();
        
        foreach (var projectile in projectileLookup.Values)
        {
            if (projectile.isPlayerShot)
                playerProjectiles.Add(projectile);
            else
                enemyProjectiles.Add(projectile);
        }
    }

    private IEnumerable<ProjectileStateBased> GetNearbyProjectiles(Vector3 position, float radius, bool isPlayerShot)
    {
        tempProjectileSet.Clear();
        int cellRadius = Mathf.CeilToInt(radius / GRID_CELL_SIZE);
        Vector2Int centerCell = GetGridCell(position);

        // Only check the grid that contains potential targets
        var targetGrid = isPlayerShot ? enemyProjectileGrid : playerProjectileGrid;

        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                Vector2Int cell = new Vector2Int(centerCell.x + x, centerCell.y + y);
                if (targetGrid.TryGetValue(cell, out var projectiles))
                {
                    foreach (var projectile in projectiles)
                    {
                        tempProjectileSet.Add(projectile);
                    }
                }
            }
        }

        return tempProjectileSet;
    }

    private void LogGridOperation(string operation, Vector2Int cell, ProjectileStateBased projectile)
    {
        if (!showDebugGrid) return;
        
        logBuilder.Clear();
        logBuilder.Append("Grid Operation: ")
                 .Append(operation)
                 .Append(" | Cell: ")
                 .Append(cell)
                 .Append(" | Projectile: ")
                 .Append(projectile.name)
                 .Append(" | Total in cell: ");

        int totalCount = 0;
        if (playerProjectileGrid.TryGetValue(cell, out var playerProjectiles))
            totalCount += playerProjectiles.Count;
        if (enemyProjectileGrid.TryGetValue(cell, out var enemyProjectiles))
            totalCount += enemyProjectiles.Count;
            
        logBuilder.Append(totalCount);
        Debug.Log(logBuilder.ToString());
    }

    private void HandleCollisions()
    {
        UpdateProjectileLists();

        // Process player projectiles against enemies
        for (int i = 0; i < playerProjectiles.Count; i++)
        {
            var projectile = playerProjectiles[i];
            if (projectile == null || !projectile.gameObject.activeInHierarchy)
                continue;

            ProcessProjectileCollisions(projectile, true);
        }

        // Process enemy projectiles against player
        for (int i = 0; i < enemyProjectiles.Count; i++)
        {
            var projectile = enemyProjectiles[i];
            if (projectile == null || !projectile.gameObject.activeInHierarchy)
                continue;

            ProcessProjectileCollisions(projectile, false);
        }
    }

    private void ProcessProjectileCollisions(ProjectileStateBased projectile, bool isPlayerShot)
    {
        var nearbyProjectiles = GetNearbyProjectiles(projectile.transform.position, maxRaycastDistance, isPlayerShot);

        foreach (var nearbyProjectile in nearbyProjectiles)
        {
            if (nearbyProjectile == null || !nearbyProjectile.gameObject.activeInHierarchy)
                continue;

            // Only check relevant collisions based on projectile type
            if (isPlayerShot)
            {
                // Player projectiles only check against enemies
                if (nearbyProjectile.gameObject.CompareTag("Enemy"))
                {
                    float distance = Vector3.Distance(projectile.transform.position, nearbyProjectile.transform.position);
                    if (distance <= maxRaycastDistance)
                    {
                        projectile.OnTriggerEnter(nearbyProjectile.GetComponent<Collider>());
                    }
                }
            }
            else
            {
                // Enemy projectiles only check against player
                if (nearbyProjectile.gameObject.CompareTag("Player"))
                {
                    float distance = Vector3.Distance(projectile.transform.position, nearbyProjectile.transform.position);
                    if (distance <= maxRaycastDistance)
                    {
                        projectile.OnTriggerEnter(nearbyProjectile.GetComponent<Collider>());
                    }
                }
            }
        }
    }

    [SerializeField]
    private Timekeeper timekeeper;
    private Dictionary<GameObject, Vector3> lastPositions = new Dictionary<GameObject, Vector3>();

    private GameObject playerGameObject;

    [Range(0f, 1f)]
    public float projectileAccuracy = 1f;

    private Dictionary<ProjectileStateBased, float> lastPredictionTimes =
        new Dictionary<ProjectileStateBased, float>();
    private float predictionUpdateInterval = 0.1f;

    private Dictionary<int, Transform> enemyTransforms = new Dictionary<int, Transform>();
    private float lastEnemyUpdateTime = 0f;
    private const float ENEMY_UPDATE_INTERVAL = 0.5f;

    private ProjectilePool projectilePool;
    private ProjectileEffectManager effectManager;
    private ProjectileSpawner projectileSpawner;
    private ProjectileJobSystem projectileJobSystem;

    private Dictionary<Transform, Vector3> cachedEnemyPositions =
        new Dictionary<Transform, Vector3>();
    private float enemyPositionUpdateInterval = 0.5f;
    private float lastEnemyPositionUpdateTime;

    private GlobalClock globalClock;

    private float _lastFrameTime;

    private int frameCounter = 0;
    private const int UPDATE_INTERVAL = 5; // Update every 5 frames

    private const int INITIAL_CAPACITY = 1000;
    private const int GROWTH_FACTOR = 2;

    [SerializeField] private int maxRaycastsPerJob = 500;
    [SerializeField] private int subSteps = 1;
    [SerializeField] private float maxRaycastDistance = 3f;
    [SerializeField] private bool useRaycastsOnlyForHoming = true;
    [SerializeField] private bool useRaycastsForPlayerShots = false;

    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private LayerMask playerLayerMask;

    [SerializeField] private float baseProcessingInterval = 0.02f;
    private float currentProcessingInterval;
    private float lastProcessingTime;

    private bool isInitialized = false;

    private bool isTransitioning = false;

    private HashSet<int> homingProjectileIds = new HashSet<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        try
        {
            // Initialize components
            InitializeComponents();
            isInitialized = true;
            ConditionalDebug.Log("[ProjectileManager] Initialization completed successfully");
        }
        catch (System.Exception e)
        {
            ConditionalDebug.LogError($"[ProjectileManager] Initialization failed: {e.Message}");
            throw;
        }
    }

    private void InitializeComponents()
    {
        projectilePool = GetComponent<ProjectilePool>() ?? throw new System.NullReferenceException("ProjectilePool component missing");
        effectManager = GetComponent<ProjectileEffectManager>() ?? throw new System.NullReferenceException("ProjectileEffectManager component missing");
        projectileSpawner = GetComponent<ProjectileSpawner>() ?? throw new System.NullReferenceException("ProjectileSpawner component missing");
        projectileJobSystem = GetComponent<ProjectileJobSystem>() ?? throw new System.NullReferenceException("ProjectileJobSystem component missing");
    }

    private void Start()
    {
        InitializeGlobalClock();
        TimeManager.Instance.OnTimeScaleChanged += UpdateProcessingInterval;
        UpdateProcessingInterval(TimeManager.Instance.GetCurrentTimeScale());
    }

    private void InitializeGlobalClock()
    {
        try
        {
            globalClock = Timekeeper.instance.Clock("Test") as GlobalClock;
            if (globalClock == null)
            {
                ConditionalDebug.LogWarning(
                    "'Test' clock is not a GlobalClock. Some time-related features may not work as expected."
                );
            }
        }
        catch (ChronosException e)
        {
            ConditionalDebug.LogError($"Failed to initialize global clock: {e.Message}");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this == null || !gameObject.activeInHierarchy) return;
        StartCoroutine(InitializeAfterSceneLoad());
    }

    private IEnumerator InitializeAfterSceneLoad()
    {
        isTransitioning = true;
        yield return new WaitForEndOfFrame();
        
        if (projectileLookup != null)
        {
            projectileLookup.Clear();
        }
        
        if (playerProjectileGrid != null)
        {
            playerProjectileGrid.Clear();
        }
        
        if (enemyProjectileGrid != null)
        {
            enemyProjectileGrid.Clear();
        }
        
        isTransitioning = false;
    }

    private void Update()
    {
        if (!isInitialized || isTransitioning)
            return;

        float deltaTime = Time.deltaTime;

        UpdateProjectilePositions();
        ProcessProjectileRequests();
        HandleCollisions();
        ProcessCollisionResults();

        // Rest of your update logic...
    }

    private void UpdateProjectilePositions()
    {
        foreach (var projectile in projectileLookup.Values)
        {
            if (projectile != null && projectile.gameObject != null)
            {
                Vector3 currentPosition = projectile.transform.position;
                if (lastPositions.TryGetValue(projectile.gameObject, out Vector3 lastPosition))
                {
                    UpdateProjectileGridPosition(projectile, lastPosition, currentPosition);
                }
                else
                {
                    // First time seeing this projectile, just add it to the grid
                    Vector2Int cell = GetGridCell(currentPosition);
                    var grid = projectile.isPlayerShot ? playerProjectileGrid : enemyProjectileGrid;
                    if (!grid.ContainsKey(cell))
                    {
                        grid[cell] = new HashSet<ProjectileStateBased>();
                    }
                    grid[cell].Add(projectile);
                }
                lastPositions[projectile.gameObject] = currentPosition;
            }
        }
    }

    private void ProcessProjectileRequests()
    {
        if (isTransitioning)
        {
            return;
        }

        // Process a fixed number of requests per interval, adjusted for time scale
        int requestsToProcess = Mathf.CeilToInt(staticShootingRequestsPerFrame / TimeManager.Instance.GetCurrentTimeScale());

        for (int i = 0; i < requestsToProcess && ProjectilePool.Instance.HasPendingRequests(); i++)
        {
            ProjectileRequest request;
            if (ProjectilePool.Instance.TryDequeueProjectileRequest(out request))
            {
                ProjectileStateBased projectile = projectilePool.GetProjectile();
                if (projectile != null)
                {
                    projectileSpawner.ProcessShootProjectile(request, projectile, request.IsStatic);
                }
            }
        }
    }

    private void ProcessCollisionResults()
    {
        // Create a safe copy of projectiles to process
        var projectilesToProcess = projectileLookup.Values
            .Where(p => ShouldUseRaycastForProjectile(p))
            .ToList();

        foreach (var projectile in projectilesToProcess)
        {
            if (projectile == null || !projectileLookup.ContainsValue(projectile))
                continue;
        }
    }

    private LayerMask GetLayerMaskForProjectile(ProjectileStateBased projectile)
    {
        return projectile.isPlayerShot ? enemyLayerMask : playerLayerMask;
    }

    public void RegisterProjectile(ProjectileStateBased projectile)
    {
        if (projectile == null || isTransitioning) return;

        int projectileId = projectile.GetInstanceID();
        if (projectileLookup.ContainsKey(projectileId)) return;

        // Register projectile
        projectileLookup[projectileId] = projectile;
        projectile.SetAccuracy(projectileAccuracy);
    }

    public void UnregisterProjectile(ProjectileStateBased projectile)
    {
        if (projectile == null)
            return;

        int projectileId = projectile.GetInstanceID();
        if (projectileLookup.ContainsKey(projectileId))
        {
            projectileLookup.Remove(projectileId);
        }
    }

    public void CompleteRunningJobs()
    {
        // This method is kept for compatibility with existing code
        // In the new system, jobs are handled by ProjectileJobSystem
        if (projectileJobSystem != null)
        {
            projectileJobSystem.CompleteProjectileUpdate();
        }
    }

    public void UpdateProjectileTargets()
    {
        foreach (var projectileId in projectileLookup.Keys)
        {
            if (
                projectileLookup.TryGetValue(projectileId, out ProjectileStateBased projectile)
                && projectile.homing
            )
            {
                Transform playerTransform = GameObject.FindWithTag("Player Aim Target")?.transform;
                if (playerTransform != null)
                    projectile.currentTarget = playerTransform;
            }
        }
    }

    private bool ShouldUseRaycastForProjectile(ProjectileStateBased projectile)
    {
        // Always use raycasts for high-speed projectiles
        if (projectile.bulletSpeed > 30f)
            return true;

        // Use state-based checks
        if (projectile.GetCurrentState() is PlayerShotState)
            return useRaycastsForPlayerShots;

        if (projectile.GetCurrentState() is EnemyShotState)
            return useRaycastsOnlyForHoming && projectile.homing;

        return false;
    }

    public ProjectileStateBased GetProjectileById(int id)
    {
        if (projectileLookup.TryGetValue(id, out ProjectileStateBased projectile))
        {
            return projectile;
        }
        return null;
    }

    public Vector3 CalculateTargetVelocity(GameObject target)
    {
        Vector3 currentPos = target.transform.position;
        Vector3 previousPos;
        if (lastPositions.TryGetValue(target, out previousPos))
        {
            Vector3 velocity = (currentPos - previousPos) / Time.deltaTime;
            lastPositions[target] = currentPos;
            return velocity;
        }
        else
        {
            lastPositions[target] = currentPos;
            return Vector3.zero;
        }
    }

    public Transform FindNearestEnemy(Vector3 position)
    {
        if (Time.time - lastEnemyUpdateTime > ENEMY_UPDATE_INTERVAL)
        {
            UpdateEnemyTransforms();
        }

        Transform nearestEnemy = null;
        float nearestDistanceSqr = float.MaxValue;

        foreach (var enemy in enemyTransforms.Values)
        {
            if (enemy != null)
            {
                float distanceSqr = (enemy.position - position).sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearestEnemy = enemy;
                }
            }
        }

        return nearestEnemy;
    }

    private void UpdateEnemyTransforms()
    {
        enemyTransforms.Clear();
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            enemyTransforms[enemy.GetInstanceID()] = enemy.transform;
        }
        lastEnemyUpdateTime = Time.time;
    }

    private void UpdateEnemyPositions()
    {
        if (Time.time - lastEnemyPositionUpdateTime < enemyPositionUpdateInterval)
            return;

        cachedEnemyPositions.Clear();
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            cachedEnemyPositions[enemy.transform] = enemy.transform.position;
        }
        lastEnemyPositionUpdateTime = Time.time;
    }

    public void NotifyEnemyHit(GameObject enemy, ProjectileStateBased projectile)
    {
        if (projectile.GetCurrentState() is PlayerShotState)
        {
            PlayerLocking.Instance.RemoveLockedEnemy(enemy.transform);
        }
    }

    public void ReRegisterEnemiesAndProjectiles()
    {
        projectileLookup.Clear();
        projectilePool.ClearPool();
        effectManager.ClearPools();

        projectilePool.InitializeProjectilePool();
        effectManager.InitializeAllPools();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            var enemySetup = enemy.GetComponent<EnemyBasicSetup>();
            if (enemySetup != null)
            {
                enemySetup.RegisterProjectiles();
            }
            else
            {
                ConditionalDebug.LogWarning("EnemyBasicSetup component missing on enemy object");
            }
        }
    }

    public void ClearAllProjectiles()
    {
        try
        {
            // Create a copy of the dictionary keys to avoid modification during iteration
            var projectileIds = new List<int>(projectileLookup.Keys);

            foreach (var id in projectileIds)
            {
                if (projectileLookup.TryGetValue(id, out ProjectileStateBased projectile))
                {
                    if (projectile != null)
                    {
                        projectile.Death();
                        projectilePool.ReturnProjectileToPool(projectile);
                    }
                }
            }

            projectileLookup.Clear();
            projectilePool.ClearProjectileRequests();

            ConditionalDebug.Log($"[ProjectileManager] Cleared all projectiles");
        }
        catch (System.Exception e)
        {
            ConditionalDebug.LogError($"[ProjectileManager] Error during ClearAllProjectiles: {e.Message}");
        }
    }

    public void HandleRewind(float currentTime)
    {
        var projectileIds = new List<int>(projectileLookup.Keys);
        foreach (var id in projectileIds)
        {
            if (projectileLookup.TryGetValue(id, out ProjectileStateBased projectile))
            {
                if (projectile.ShouldDeactivate(currentTime))
                {
                    if (projectile != null)
                    {
                        projectile.Death();
                        projectilePool.ReturnProjectileToPool(projectile);
                    }
                    projectileLookup.Remove(id);
                }
            }
        }
    }

    private void OnSceneTransitionStart()
    {
        isTransitioning = true;
        ClearAllProjectiles();
    }

    private void OnSceneTransitionEnd()
    {
        isTransitioning = false;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene != SceneManager.GetActiveScene()) return;

        projectileLookup.Clear();
        lastPredictionTimes.Clear();
        lastPositions.Clear();
        enemyTransforms.Clear();
        cachedEnemyPositions.Clear();
    }

    public async System.Threading.Tasks.Task InitializeAsync()
    {
        if (isInitialized)
        {
            return;
        }

        try
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                InitializeComponents();
            });

            SceneManager.sceneLoaded += OnSceneLoaded;

            isInitialized = true;
            ConditionalDebug.Log("[ProjectileManager] Async initialization completed successfully");
        }
        catch (System.Exception e)
        {
            ConditionalDebug.LogError($"[ProjectileManager] Async initialization failed: {e.Message}");
            throw;
        }
    }

    public void OnWaveEnd()
    {
        // Clear all projectiles safely
        var projectileIds = new List<int>(projectileLookup.Keys);
        foreach (var id in projectileIds)
        {
            if (projectileLookup.TryGetValue(id, out ProjectileStateBased projectile))
            {
                if (projectile != null)
                {
                    projectile.Death();
                    projectilePool.ReturnProjectileToPool(projectile);
                }
            }
        }

        // Rest of wave end cleanup...
        lastEnemyUpdateTime = 0f;
        lastEnemyPositionUpdateTime = 0f;
        lastPositions.Clear();
        enemyTransforms.Clear();
        cachedEnemyPositions.Clear();
        projectileLookup.Clear();
    }

    public void OnWaveStart()
    {
        ConditionalDebug.Log("[ProjectileManager] Wave started, initializing systems...");

        // Reset any wave-specific state
        lastEnemyUpdateTime = 0f;
        lastEnemyPositionUpdateTime = 0f;

        // Clear any leftover data from previous wave
        lastPositions.Clear();
        enemyTransforms.Clear();
        cachedEnemyPositions.Clear();
    }

    private void UpdateProcessingInterval(float timeScale)
    {
        currentProcessingInterval = baseProcessingInterval / timeScale;
    }

    public void RegisterHomingProjectile(int projectileId)
    {
        homingProjectileIds.Add(projectileId);
    }

    public void UnregisterHomingProjectile(int projectileId)
    {
        homingProjectileIds.Remove(projectileId);
    }

    public IReadOnlyCollection<int> GetActiveHomingProjectileIds()
    {
        return homingProjectileIds;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGrid || !Application.isPlaying) return;

        // Draw player projectile grid in blue
        DrawGrid(playerProjectileGrid, new Color(0f, 0.5f, 1f, 0.1f), "Player");

        // Draw enemy projectile grid in red
        DrawGrid(enemyProjectileGrid, new Color(1f, 0.2f, 0.2f, 0.1f), "Enemy");
    }

    private void DrawGrid(Dictionary<Vector2Int, HashSet<ProjectileStateBased>> grid, Color color, string label)
    {
        foreach (var cell in grid.Keys)
        {
            Vector3 cellCenter = new Vector3(
                (cell.x + 0.5f) * GRID_CELL_SIZE,
                0,
                (cell.y + 0.5f) * GRID_CELL_SIZE
            );

            // Draw cell boundaries
            Gizmos.color = color;
            Gizmos.DrawWireCube(cellCenter, new Vector3(GRID_CELL_SIZE, 0.1f, GRID_CELL_SIZE));

#if UNITY_EDITOR
            // Show projectile count in scene view
            UnityEditor.Handles.Label(cellCenter + Vector3.up * 2,
                $"{label} Projectiles: {grid[cell].Count}");
#endif

            if (showProjectileConnections)
            {
                DrawProjectileConnections(grid[cell], color);
            }
        }
    }

    private void DrawProjectileConnections(HashSet<ProjectileStateBased> projectiles, Color baseColor)
    {
        var projectileList = projectiles.ToList();
        for (int i = 0; i < projectileList.Count; i++)
        {
            if (projectileList[i] == null) continue;

            Vector3 pos1 = projectileList[i].transform.position;
            for (int j = i + 1; j < projectileList.Count; j++)
            {
                if (projectileList[j] == null) continue;

                Vector3 pos2 = projectileList[j].transform.position;
                if (Vector3.Distance(pos1, pos2) <= maxRaycastDistance)
                {
                    Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f);
                    Gizmos.DrawLine(pos1, pos2);
                }
            }
        }
    }
}
