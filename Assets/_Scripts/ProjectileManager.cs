using Chronos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management functions

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    [SerializeField] private ProjectileStateBased projectilePrefab; // Assign in inspector
    [SerializeField] private GameObject enemyShotFXPrefab; // Assign in inspector
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private ParticleSystem deathEffectPrefab; // Assign in inspector
    [SerializeField] private int initialDeathEffectPoolSize = 10;

    private List<ProjectileStateBased> projectiles = new List<ProjectileStateBased>(100); // Adjust 100 based on expected usage
    private Queue<ProjectileStateBased> staticEnemyProjectilePool = new Queue<ProjectileStateBased>(100);
    private Queue<ProjectileStateBased> enemyBasicSetupProjectilePool = new Queue<ProjectileStateBased>(100);
    private Queue<ParticleSystem> deathEffectPool = new Queue<ParticleSystem>(50);

    [SerializeField] private Timekeeper timekeeper;
    private Dictionary<GameObject, Vector3> lastPositions = new Dictionary<GameObject, Vector3>();

    [SerializeField] private Crosshair crosshair;

    private GameObject playerGameObject; // Cache for player GameObject

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Initialize all pools
        InitializeStaticEnemyProjectilePool();
        InitializeEnemyBasicSetupProjectilePool();
        InitializeDeathEffectPool();

        // Find and cache the player GameObject
        playerGameObject = GameObject.FindGameObjectWithTag("Player");
        if (playerGameObject == null)
        {
            Debug.LogWarning("Player GameObject not found during initialization.");
        }
    }

    // This method will be called every time a scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Attempt to find the Timekeeper and Crosshair references in the newly loaded scene
        timekeeper = FindObjectOfType<Timekeeper>();
        if (timekeeper == null)
        {
            Debug.LogWarning("Timekeeper not found in the scene.");
        }

        crosshair = FindObjectOfType<Crosshair>();
        if (crosshair == null)
        {
            Debug.LogWarning("Crosshair not found in the scene.");
        }

        // Clear existing pools and lists to reinitialize
        projectiles.Clear();
        staticEnemyProjectilePool.Clear();
        enemyBasicSetupProjectilePool.Clear();
        deathEffectPool.Clear();

        // Reinitialize pools
        InitializeStaticEnemyProjectilePool();
        InitializeEnemyBasicSetupProjectilePool();
        InitializeDeathEffectPool();
    }

    private void OnDestroy()
    {
        // Unsubscribe from the sceneLoaded event to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitializeStaticEnemyProjectilePool()
    {
        staticEnemyProjectilePool = new Queue<ProjectileStateBased>(initialPoolSize); // Predefine the capacity
        for (int i = 0; i < initialPoolSize; i++)
        {
            ProjectileStateBased proj = Instantiate(projectilePrefab, transform); // Parent set to ProjectileManager
            proj.gameObject.SetActive(false);
            proj.poolType = ProjectilePoolType.StaticEnemy;
            staticEnemyProjectilePool.Enqueue(proj);
        }
    }

    private void InitializeEnemyBasicSetupProjectilePool()
    {
        enemyBasicSetupProjectilePool = new Queue<ProjectileStateBased>(initialPoolSize); // Predefine the capacity
        for (int i = 0; i < initialPoolSize; i++)
        {
            ProjectileStateBased proj = Instantiate(projectilePrefab, transform); // Parent set to ProjectileManager
            proj.gameObject.SetActive(false);
            proj.poolType = ProjectilePoolType.EnemyBasicSetup;
            enemyBasicSetupProjectilePool.Enqueue(proj);
        }
    }

    private void InitializeDeathEffectPool()
    {
        deathEffectPool = new Queue<ParticleSystem>(initialDeathEffectPoolSize); // Predefine the capacity
        for (int i = 0; i < initialDeathEffectPoolSize; i++)
        {
            ParticleSystem effect = Instantiate(deathEffectPrefab, transform);
            effect.gameObject.SetActive(false);
            deathEffectPool.Enqueue(effect);
        }
    }

    public void ShootProjectile(Vector3 position, Quaternion rotation, float speed, float lifetime, float uniformScale, bool enableHoming = false, Material material = null)
    {
        if (staticEnemyProjectilePool.Count == 0)
        {
            Debug.LogWarning("[ProjectileManager] No projectile available in pool, skipping shot.");
            return;
        }

        ProjectileStateBased projectile = staticEnemyProjectilePool.Dequeue();
        projectile.transform.SetParent(transform); // Ensure the projectile is parented to ProjectileManager
        projectile.transform.position = position;
        projectile.transform.rotation = rotation;
        projectile.gameObject.SetActive(true);
        projectile.transform.localScale = Vector3.one * uniformScale; // Apply uniform scale

        // Ensure Rigidbody is not kinematic
        projectile.rb.isKinematic = false;
        projectile.rb.velocity = rotation * Vector3.forward * speed; // Assumes the projectile has a Rigidbody component
        projectile.bulletSpeed = speed; // Set the bullet speed here

        projectile.SetLifetime(lifetime); // Set the lifetime of the projectile
        projectile.EnableHoming(enableHoming); // Set homing based on the parameter

        // Swap the material if a new one is provided and it's not already on the projectile
        if (material != null && projectile.modelRenderer.material != material)
        {
            projectile.modelRenderer.material = material;
            projectile.UpdateMaterial(material); // Update the material reference in ProjectileStateBased
        }

        RegisterProjectile(projectile);
    }

    public void ShootProjectileFromEnemy(Vector3 position, Quaternion rotation, float speed, float lifetime, float uniformScale, bool enableHoming = false, Material material = null, string clockKey = "")
    {
        if (enemyBasicSetupProjectilePool.Count == 0)
        {
            Debug.LogWarning("[ProjectileManager] No EnemyBasicSetup projectile available in pool, skipping shot.");
            return;
        }

        ProjectileStateBased projectile = enemyBasicSetupProjectilePool.Dequeue();
        projectile.transform.SetParent(transform);
        projectile.transform.position = position;
        projectile.transform.rotation = rotation;
        projectile.gameObject.SetActive(true);
        projectile.transform.localScale = Vector3.one * uniformScale; // Set the scale of the projectile

        // Check if the Enemy Shot FX prefab is assigned before instantiating
        if (enemyShotFXPrefab != null)
        {
            GameObject enemyShotFX = Instantiate(enemyShotFXPrefab, projectile.transform);
            enemyShotFX.transform.localPosition = Vector3.zero; // Center it on the projectile
            enemyShotFX.transform.localScale = Vector3.one * uniformScale; // Set the scale of the FX to match the projectile
            SetChildrenScale(enemyShotFX, Vector3.one * uniformScale); // Recursively set scale for all children
            enemyShotFX.SetActive(true);
        }

        projectile.rb.isKinematic = false;
        projectile.rb.velocity = rotation * Vector3.forward * speed;
        projectile.bulletSpeed = speed;
        projectile.SetLifetime(lifetime);
        projectile.EnableHoming(enableHoming);

        if (material != null && projectile.modelRenderer.material != material)
        {
            projectile.modelRenderer.material = material;
            projectile.UpdateMaterial(material);
        }

        if (!string.IsNullOrEmpty(clockKey))
        {
            projectile.SetClock(clockKey);
        }

        projectile.initialSpeed = speed;

        RegisterProjectile(projectile);
    }

    private void SetChildrenScale(GameObject parent, Vector3 scale)
    {
        foreach (Transform child in parent.transform)
        {
            child.localScale = scale;
            SetChildrenScale(child.gameObject, scale); // Recursive call to set scale for all sub-children
        }
    }

    public void PlayDeathEffect(Vector3 position)
    {
        if (deathEffectPool.Count == 0)
        {
            Debug.LogWarning("No death effect available in pool, skipping effect.");
            return;
        }

        ParticleSystem effect = deathEffectPool.Dequeue();
        if (playerGameObject != null)
        {
            effect.transform.SetParent(playerGameObject.transform);
        }
        else
        {
            Debug.LogWarning("Player GameObject not found. Effect will not follow the player.");
            effect.transform.SetParent(transform); // Fallback to the default parent
        }
        effect.transform.position = position;
        effect.gameObject.SetActive(true);
        effect.Play();

        // Return the effect to the pool after it has finished playing
        StartCoroutine(ReturnEffectToPoolAfterFinished(effect, playerGameObject != null ? playerGameObject.transform : transform));
    }

    private IEnumerator ReturnEffectToPoolAfterFinished(ParticleSystem effect, Transform originalParent)
    {
        yield return new WaitWhile(() => effect.isPlaying);

        effect.Stop(); // Ensure the effect is stopped
        effect.gameObject.SetActive(false);
        effect.transform.SetParent(originalParent); // Reset the parent to the original (ProjectileManager)
        deathEffectPool.Enqueue(effect);
    }

    public void ReturnProjectileToPool(ProjectileStateBased projectile)
    {
        projectile.gameObject.SetActive(false);
        projectile.rb.velocity = Vector3.zero; // Reset velocity
        projectile.rb.isKinematic = true; // Optionally make it kinematic to avoid unnecessary physics calculations while inactive

        switch (projectile.poolType)
        {
            case ProjectilePoolType.StaticEnemy:
                if (!staticEnemyProjectilePool.Contains(projectile))
                {
                    staticEnemyProjectilePool.Enqueue(projectile);
                }
                break;
            case ProjectilePoolType.EnemyBasicSetup:
                if (!enemyBasicSetupProjectilePool.Contains(projectile))
                {
                    enemyBasicSetupProjectilePool.Enqueue(projectile);
                }
                break;
            // Handle other types as needed
            default:
                Debug.LogError("Unhandled projectile pool type.");
                break;
        }
    }

    public void RegisterProjectile(ProjectileStateBased projectile)
    {
        if (!projectiles.Contains(projectile))
        {
            projectiles.Add(projectile);
        }
    }

    public void UnregisterProjectile(ProjectileStateBased projectile)
    {
        if (projectiles.Contains(projectile))
        {
            projectiles.Remove(projectile);
        }
    }

    private void Update()
    {
        // Example of using Chronos to adjust time scale for projectiles
        float globalTimeScale = timekeeper.Clock("Test").localTimeScale; // Adjust "Global" to your specific clock name

        foreach (var projectile in projectiles)
        {
            if (projectile != null)
            {
                // Adjust projectile logic based on globalTimeScale
                // This could involve modifying movement speed, animation speed, etc.
                projectile.CustomUpdate(globalTimeScale);
                if (projectile.homing)
                {
                    PredictAndRotateProjectile(projectile); // Ensure this method is refined and used
                }
            }
        }
    }

    public void UpdateProjectileTargets()
    {
        foreach (var projectile in projectiles)
        {
            if (projectile != null && projectile.homing)
            {
                // Example: Always target the player
                Transform playerTransform = GameObject.FindWithTag("Player Aim Target").transform;
                projectile.currentTarget = playerTransform;

                // Extend this logic based on your game's needs, such as targeting the closest enemy, etc.
            }
        }
    }

    private void PerformHomingWithObstacleAvoidance(ProjectileStateBased projectile)
    {
        Vector3 directionToTarget = (projectile.currentTarget.position - projectile.transform.position).normalized;
        Vector3 currentPosition = projectile.transform.position;

        // Check for obstacles in the path to the target
        if (Physics.Raycast(currentPosition, directionToTarget, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            // Obstacle detected, calculate a new direction to avoid the ground
            Vector3 avoidDirection = Vector3.ProjectOnPlane(directionToTarget, hit.normal).normalized;

            // Adjust the projectile's velocity to steer away from the obstacle
            projectile.rb.velocity = Vector3.Lerp(projectile.rb.velocity, avoidDirection * projectile.bulletSpeed, Time.deltaTime * projectile.turnRate);
        }
        else
        {
            // No obstacle, proceed towards the target
            projectile.rb.velocity = directionToTarget * projectile.bulletSpeed;
        }
    }

    public void PredictAndRotateProjectile(ProjectileStateBased projectile)
    {
        if (crosshair == null || projectile.currentTarget == null) return;

        Vector3 targetVelocity = CalculateTargetVelocity(projectile.currentTarget.gameObject);
        float distanceToTarget = Vector3.Distance(projectile.transform.position, projectile.currentTarget.position);
        float projectileSpeed = projectile.rb.velocity.magnitude;

        if (projectileSpeed < Mathf.Epsilon) return; // Avoid division by zero

        float predictionTime = distanceToTarget / projectileSpeed;
        Vector3 predictedPosition = projectile.currentTarget.position + targetVelocity * predictionTime;

        projectile.predictedPosition = predictedPosition;

        Vector3 directionToTarget = predictedPosition - projectile.transform.position;
        if (directionToTarget == Vector3.zero) return; // Check if direction is zero

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        projectile.rb.MoveRotation(Quaternion.RotateTowards(projectile.transform.rotation, targetRotation, projectile._rotateSpeed * Time.deltaTime));
    }

    private Vector3 CalculateTargetVelocity(GameObject target)
    {
        Vector3 currentPos = target.transform.position;
        Vector3 previousPos = Vector3.zero;
        if (lastPositions.ContainsKey(target))
        {
            previousPos = lastPositions[target];
        }
        else
        {
            lastPositions.Add(target, currentPos);
        }

        Vector3 velocity = (currentPos - previousPos) / Time.deltaTime;
        lastPositions[target] = currentPos; // Update the last known position
        return velocity;
    }

    public void NotifyEnemyHit(GameObject enemy, ProjectileStateBased projectile)
{
    // Check if the projectile is from a locked-on shot
    if (projectile.GetCurrentState() is PlayerShotState)
    {
        // Use the assigned Crosshair reference to remove the enemy from the locked-on list
        if (crosshair != null)
        {
            crosshair.RemoveLockedEnemy(enemy.transform);

        }
        else
        {
            Debug.LogError("Crosshair reference is not set in ProjectileManager.");
        }
    }
}
}
