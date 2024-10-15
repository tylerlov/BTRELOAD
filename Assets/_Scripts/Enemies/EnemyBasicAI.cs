using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(FollowerEntity))]
[RequireComponent(typeof(EnemyBasicSetup))]
public class EnemyBasicAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Minimum distance to maintain from the player")]
    public float minPlayerDistance = 5f;
    [Tooltip("Maximum distance to maintain from the player")]
    public float maxPlayerDistance = 10f;
    [Tooltip("Time to wait at each position before moving")]
    public float timeAtPosition = 2f;
    [Tooltip("Minimum angle to rotate when choosing a new position")]
    public float minMoveAngle = 10f;
    [Tooltip("Maximum angle to rotate when choosing a new position")]
    public float maxMoveAngle = 20f;

    [Header("Attack Settings")]
    [Tooltip("Cooldown between attacks")]
    public float attackCooldown = 1f;

    [Header("Line of Sight Settings")]
    [Tooltip("Should the enemy prioritize positions with line of sight to the player?")]
    public bool prioritizeLineOfSight = true;
    [Tooltip("Number of attempts to find a position with line of sight")]
    public int lineOfSightAttempts = 5;
    [Tooltip("Layer mask for obstacles that block line of sight")]
    public LayerMask obstacleLayerMask;

    private FollowerEntity followerEntity;
    private EnemyBasicSetup enemySetup;
    private static Transform playerTransform;
    private float lastAttackTime;
    private float lastPositionChangeTime;
    private Vector3 currentDestination;
    private float pathUpdateInterval = 0.5f; // Add this line
    private float lastPathUpdateTime;
    private float lineOfSightCheckInterval = 0.2f; // Check line of sight every 0.2 seconds
    private float lastLineOfSightCheckTime;
    private bool cachedLineOfSight;
    private RaycastHit[] raycastHits = new RaycastHit[1];
    private bool hasLineOfSightToPlayer;

    private void Awake()
    {
        followerEntity = GetComponent<FollowerEntity>();
        enemySetup = GetComponent<EnemyBasicSetup>();
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }
        EnemyShootingManager.Instance.RegisterBasicEnemy(this);
    }

    private void OnDestroy()
    {
        EnemyShootingManager.Instance.UnregisterBasicEnemy(this);
    }

    private void Start()
    {
        // Initial position setup
        ChooseNewPosition();
    }

    private void Update()
    {
        // Check if it's time to move to a new position
        if (Time.time - lastPositionChangeTime > timeAtPosition)
        {
            ChooseNewPosition();
        }

        // Update path less frequently
        if (Time.time - lastPathUpdateTime > pathUpdateInterval)
        {
            followerEntity.destination = currentDestination;
            lastPathUpdateTime = Time.time;
        }

        // Attack if possible
        if (Time.time - lastAttackTime > attackCooldown && hasLineOfSightToPlayer)
        {
            Attack();
        }
    }

    private void ChooseNewPosition()
    {
        Vector3 newPosition;
        if (prioritizeLineOfSight)
        {
            newPosition = FindPositionWithLineOfSight();
        }
        else
        {
            newPosition = GetRandomPositionAroundPlayer();
        }

        currentDestination = newPosition;
        lastPositionChangeTime = Time.time;
    }

    private Vector3 GetRandomPositionAroundPlayer()
    {
        float angle = Random.Range(0f, 360f);
        float distance = Random.Range(minPlayerDistance, maxPlayerDistance);
        Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
        return playerTransform.position + offset;
    }

    private Vector3 FindPositionWithLineOfSight()
    {
        Vector3 bestPosition = GetRandomPositionAroundPlayer();
        float bestDistance = float.MaxValue;

        for (int i = 0; i < lineOfSightAttempts; i++)
        {
            Vector3 potentialPosition = GetRandomPositionAroundPlayer();
            if (HasLineOfSight(potentialPosition))
            {
                float distance = Vector3.Distance(potentialPosition, playerTransform.position);
                if (distance < bestDistance)
                {
                    bestPosition = potentialPosition;
                    bestDistance = distance;
                }
            }
        }

        return bestPosition;
    }

    private bool HasLineOfSight(Vector3 fromPosition)
    {
        Vector3 direction = playerTransform.position - fromPosition;
        int hitCount = Physics.RaycastNonAlloc(fromPosition, direction, raycastHits, direction.magnitude, obstacleLayerMask);
        return hitCount == 0;
    }

    private bool HasLineOfSightToPlayer()
    {
        if (Time.time - lastLineOfSightCheckTime > lineOfSightCheckInterval)
        {
            cachedLineOfSight = HasLineOfSight(transform.position);
            lastLineOfSightCheckTime = Time.time;
        }
        return cachedLineOfSight;
    }

    private void Attack()
    {
        // Use EnemyBasicSetup to perform the attack
        enemySetup.Attack(playerTransform.position);
        lastAttackTime = Time.time;
    }

    // This method can be called to force the enemy to choose a new position immediately
    public void ForceNewPosition()
    {
        ChooseNewPosition();
    }

    public void HandleLineOfSightResult(bool hasLineOfSight)
    {
        hasLineOfSightToPlayer = hasLineOfSight;

        if (hasLineOfSight)
        {
            // Enemy can see the player, maybe prepare to attack
            if (Time.time - lastAttackTime > attackCooldown)
            {
                Attack();
            }
        }
        else
        {
            // Enemy can't see the player, maybe move to a better position
            ForceNewPosition();
        }
    }
}
