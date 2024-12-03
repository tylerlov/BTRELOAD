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
    public static LayerMask obstacleLayerMask;

    private FollowerEntity followerEntity;
    private EnemyBasicSetup enemySetup;
    private static Transform playerTransform;
    private float lastAttackTime;
    private float lastPositionChangeTime;
    private Vector3 currentDestination;
    private float pathUpdateInterval = 0.5f;
    private float lastPathUpdateTime;
    private float distanceThresholdForPathUpdate = 1f; // Only update path if moved this far
    private Vector3 lastPathUpdatePosition;
    private bool hasLineOfSightToPlayer;

    // Cache for position calculations
    private static readonly Vector3[] cachedDirections;
    private static readonly Vector3[] potentialPositions;
    private static int currentPositionIndex;

    static EnemyBasicAI()
    {
        cachedDirections = new Vector3[8];
        potentialPositions = new Vector3[8];

        // Pre-calculate evenly distributed directions
        for (int i = 0; i < 8; i++)
        {
            float angle = i * (360f / 8);
            cachedDirections[i] = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        }
    }

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

        // Update path based on distance moved
        if (Vector3.Distance(transform.position, lastPathUpdatePosition) > distanceThresholdForPathUpdate)
        {
            followerEntity.destination = currentDestination;
            lastPathUpdatePosition = transform.position;
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
        // Use cached directions instead of random angles
        int dirIndex = Random.Range(0, cachedDirections.Length);
        float distance = Random.Range(minPlayerDistance, maxPlayerDistance);
        return playerTransform.position + cachedDirections[dirIndex] * distance;
    }

    private Vector3 FindPositionWithLineOfSight()
    {
        // Pre-calculate potential positions
        for (int i = 0; i < cachedDirections.Length; i++)
        {
            float distance = Random.Range(minPlayerDistance, maxPlayerDistance);
            potentialPositions[i] = playerTransform.position + cachedDirections[i] * distance;
        }

        // Find best position from pre-calculated positions
        Vector3 bestPosition = potentialPositions[0];
        float bestDistance = float.MaxValue;

        for (int i = 0; i < cachedDirections.Length; i++)
        {
            Vector3 potentialPosition = potentialPositions[i];
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
        // This method is now only called from FindPositionWithLineOfSight
        // Line of sight to player is handled by EnemyShootingManager
        Vector3 direction = playerTransform.position - fromPosition;
        return !Physics.Raycast(fromPosition, direction, direction.magnitude, obstacleLayerMask);
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
