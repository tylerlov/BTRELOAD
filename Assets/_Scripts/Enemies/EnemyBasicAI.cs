using UnityEngine;
using Pathfinding;
using System.Collections;

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
    private Transform playerTransform;
    private float lastAttackTime;
    private float lastPositionChangeTime;
    private Vector3 currentDestination;

    private void Awake()
    {
        followerEntity = GetComponent<FollowerEntity>();
        enemySetup = GetComponent<EnemyBasicSetup>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
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

        // Move towards the current destination
        followerEntity.destination = currentDestination;

        // Attack if possible
        if (Time.time - lastAttackTime > attackCooldown && HasLineOfSightToPlayer())
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
        for (int i = 0; i < lineOfSightAttempts; i++)
        {
            Vector3 potentialPosition = GetRandomPositionAroundPlayer();
            if (HasLineOfSight(potentialPosition))
            {
                return potentialPosition;
            }
        }
        // If no position with line of sight is found, return a random position
        return GetRandomPositionAroundPlayer();
    }

    private bool HasLineOfSight(Vector3 fromPosition)
    {
        Vector3 direction = playerTransform.position - fromPosition;
        return !Physics.Raycast(fromPosition, direction, direction.magnitude, obstacleLayerMask);
    }

    private bool HasLineOfSightToPlayer()
    {
        return HasLineOfSight(transform.position);
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
}
