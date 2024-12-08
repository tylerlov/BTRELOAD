using System;
using System.Collections;
using System.Collections.Generic;
using Chronos;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    [SerializeField]
    private int staticShootingRequestsPerFrame = 10;

    private Dictionary<int, ProjectileStateBased> projectileLookup = new Dictionary<int, ProjectileStateBased>();
    private ProjectileGrid projectileGrid;
    private List<ProjectileStateBased> playerProjectiles = new List<ProjectileStateBased>();
    private List<ProjectileStateBased> enemyProjectiles = new List<ProjectileStateBased>();

    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] public float projectileAccuracy = 1f;

    private bool isTransitioning;
    private ProjectileJobSystem projectileJobSystem;
    private ProjectilePool projectilePool;
    private ProjectileSpawner projectileSpawner;
    private Dictionary<GameObject, Vector3> lastPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<int, Transform> enemyTransforms = new Dictionary<int, Transform>();
    private float lastEnemyUpdateTime = 0f;
    private const float ENEMY_UPDATE_INTERVAL = 0.5f;
    private HashSet<int> homingProjectileIds = new HashSet<int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        projectileGrid = gameObject.AddComponent<ProjectileGrid>();
        InitializeComponents();
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void InitializeComponents()
    {
        projectileJobSystem = GetComponent<ProjectileJobSystem>();
        projectilePool = ProjectilePool.Instance;
        projectileSpawner = GetComponent<ProjectileSpawner>();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(InitializeAfterSceneLoad());
    }

    private IEnumerator InitializeAfterSceneLoad()
    {
        yield return new WaitForSeconds(0.1f);
        ReRegisterEnemiesAndProjectiles();
    }

    private void Update()
    {
        if (!isTransitioning)
        {
            UpdateProjectilePositions();
            ProcessProjectileRequests();
            ProcessCollisionResults();
            UpdateProjectileTargets();
        }
    }

    private void UpdateProjectilePositions()
    {
        foreach (var projectile in projectileLookup.Values)
        {
            if (projectile != null)
            {
                Vector3 oldPosition = projectile.transform.position;
                projectile.UpdatePosition();
                projectileGrid.UpdateProjectileGridPosition(projectile, oldPosition, projectile.transform.position);
            }
        }
    }

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

    public void RegisterProjectile(ProjectileStateBased projectile)
    {
        if (projectile == null || isTransitioning) return;

        int projectileId = projectile.GetInstanceID();
        if (projectileLookup.ContainsKey(projectileId)) return;

        projectileLookup[projectileId] = projectile;
        projectile.SetAccuracy(projectileAccuracy);
    }

    public void UnregisterProjectile(ProjectileStateBased projectile)
    {
        if (projectile == null) return;

        int projectileId = projectile.GetInstanceID();
        if (projectileLookup.ContainsKey(projectileId))
        {
            projectileLookup.Remove(projectileId);
        }
    }

    public void ClearAllProjectiles()
    {
        projectileLookup.Clear();
        projectileGrid.ClearGrids();
        UpdateProjectileLists();
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
        ClearAllProjectiles();
    }

    public void ReRegisterEnemiesAndProjectiles()
    {
        ClearAllProjectiles();
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            var enemyBasics = enemy.GetComponent<EnemyBasics>();
            if (enemyBasics != null)
            {
                enemyBasics.RegisterProjectiles();
            }
        }
    }

    private bool ShouldUseRaycastForProjectile(ProjectileStateBased projectile)
    {
        return projectile.bulletSpeed > 30f;
    }

    private LayerMask GetLayerMaskForProjectile(ProjectileStateBased projectile)
    {
        return projectile.isPlayerShot ? enemyLayerMask : playerLayerMask;
    }

    public void CompleteRunningJobs()
    {
        if (projectileJobSystem != null)
        {
            projectileJobSystem.CompleteProjectileUpdate();
        }
    }

    public void UpdateProjectileTargets()
    {
        foreach (var projectileId in projectileLookup.Keys)
        {
            if (projectileLookup.TryGetValue(projectileId, out ProjectileStateBased projectile) && projectile.homing)
            {
                Transform playerTransform = GameObject.FindWithTag("Player Aim Target")?.transform;
                if (playerTransform != null)
                    projectile.currentTarget = playerTransform;
            }
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

    private void ProcessProjectileRequests()
    {
        if (isTransitioning) return;

        int requestsToProcess = Mathf.CeilToInt(staticShootingRequestsPerFrame / TimeManager.Instance.GetCurrentTimeScale());

        for (int i = 0; i < requestsToProcess && ProjectilePool.Instance.HasPendingRequests(); i++)
        {
            if (ProjectilePool.Instance.TryDequeueProjectileRequest(out ProjectileRequest request))
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
        var projectilesToProcess = projectileLookup.Values
            .Where(p => ShouldUseRaycastForProjectile(p))
            .ToList();

        foreach (var projectile in projectilesToProcess)
        {
            if (projectile == null || !projectileLookup.ContainsValue(projectile)) continue;

            // Process collisions using grid system
            var nearbyProjectiles = projectileGrid.GetNearbyProjectiles(
                projectile.transform.position,
                3f, // maxRaycastDistance
                projectile.isPlayerShot
            );

            foreach (var nearbyProjectile in nearbyProjectiles)
            {
                if (nearbyProjectile == null || !nearbyProjectile.gameObject.activeInHierarchy) continue;

                float distance = Vector3.Distance(projectile.transform.position, nearbyProjectile.transform.position);
                if (distance <= 3f) // maxRaycastDistance
                {
                    projectile.OnTriggerEnter(nearbyProjectile.GetComponent<Collider>());
                }
            }
        }
    }

    public void NotifyEnemyHit(GameObject enemy, ProjectileStateBased projectile)
    {
        if (projectile.GetCurrentState() is PlayerShotState)
        {
            PlayerLocking.Instance.RemoveLockedEnemy(enemy.transform);
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

    public void OnWaveStart()
    {
        lastEnemyUpdateTime = 0f;
        lastPositions.Clear();
        enemyTransforms.Clear();
    }

    public void OnWaveEnd()
    {
        ClearAllProjectiles();
        lastEnemyUpdateTime = 0f;
        lastPositions.Clear();
        enemyTransforms.Clear();
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
}
