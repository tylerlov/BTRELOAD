using UnityEngine;
using Pathfinding;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(FollowerEntity))]
[RequireComponent(typeof(EnemyBasics))]
public class EnemyBasicAI : MonoBehaviour
{
    [Header("Movement Settings")]
    private float _minPlayerDistance = 5f;
    private float _maxPlayerDistance = 10f;
    private float _repositionInterval = 0.5f; // How often to update position
    private float _minEnemySpacing = 3f; // Minimum distance between enemies

    [Header("Positioning")]
    private bool _prioritizeLineOfSight = true;
    private int _positionAttempts = 8;
    private LayerMask _obstacleLayerMask;
    private LayerMask _enemyLayerMask;

    protected float MinPlayerDistance => _minPlayerDistance;
    protected float MaxPlayerDistance => _maxPlayerDistance;
    protected float RepositionInterval => _repositionInterval;
    protected float MinEnemySpacing => _minEnemySpacing;
    protected bool PrioritizeLineOfSight => _prioritizeLineOfSight;
    protected int PositionAttempts => _positionAttempts;
    protected LayerMask ObstacleLayerMask => _obstacleLayerMask;
    protected LayerMask EnemyLayerMask => _enemyLayerMask;

    private FollowerEntity _followerEntity;
    private EnemyBasics _basics;
    private Transform _playerTransform;
    private Vector3 currentDestination;
    private float lastRepositionTime;
    private bool hasLineOfSightToPlayer;
    private List<EnemyBasicAI> nearbyEnemies = new List<EnemyBasicAI>();
    private const float NEARBY_ENEMY_CHECK_RADIUS = 10f;

    protected FollowerEntity FollowerEntity => _followerEntity;
    protected Transform PlayerTransform => _playerTransform;
    protected EnemyBasics Basics => _basics;

    protected virtual void Awake()
    {
        _followerEntity = GetComponent<FollowerEntity>();
        _basics = GetComponent<EnemyBasics>();
        _playerTransform = _basics.PlayerTransform;
    }

    private void Start()
    {
        StartCoroutine(StaggeredStart());
    }

    private IEnumerator StaggeredStart()
    {
        // Register with manager first - this is lightweight
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterBasicEnemy(this);
            EnemyManager.Instance.RegisterEnemy(GetComponent<EnemyBasics>());
        }
        
        // Wait for a random short delay to spread out the heavy operations
        yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
        
        // Then do the expensive operations
        ChooseNewPosition();
    }

    private void OnDestroy()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterBasicEnemy(this);
            EnemyManager.Instance.UnregisterEnemy(GetComponent<EnemyBasics>());
        }
    }

    private void Update()
    {
        // Constantly evaluate and update position
        if (Time.time - lastRepositionTime >= RepositionInterval)
        {
            UpdateNearbyEnemies();
            ChooseNewPosition();
        }

        // Ensure we have a valid line of sight state
        if (!hasLineOfSightToPlayer)
        {
            hasLineOfSightToPlayer = CheckLineOfSight();
        }
    }

    private void UpdateNearbyEnemies()
    {
        nearbyEnemies.Clear();
        Collider[] colliders = Physics.OverlapSphere(transform.position, NEARBY_ENEMY_CHECK_RADIUS, EnemyLayerMask);
        
        foreach (var collider in colliders)
        {
            var enemy = collider.GetComponent<EnemyBasicAI>();
            if (enemy != null && enemy != this)
            {
                nearbyEnemies.Add(enemy);
            }
        }
    }

    private void ChooseNewPosition()
    {
        Vector3 bestPosition = transform.position;
        float bestScore = float.MinValue;

        // Generate positions around the player at different angles
        for (int i = 0; i < PositionAttempts; i++)
        {
            float angle = (360f / PositionAttempts) * i;
            Vector3 potentialPosition = GetPositionAtAngle(angle);
            float score = EvaluatePosition(potentialPosition);

            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = potentialPosition;
            }
        }

        currentDestination = bestPosition;
        lastRepositionTime = Time.time;
        FollowerEntity.destination = currentDestination;
    }

    private Vector3 GetPositionAtAngle(float angle)
    {
        float distance = Random.Range(MinPlayerDistance, MaxPlayerDistance);
        Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        return PlayerTransform.position + direction * distance;
    }

    private float EvaluatePosition(Vector3 position)
    {
        float score = 0f;

        // Base distance score
        float distanceToPlayer = Vector3.Distance(position, PlayerTransform.position);
        score += GetDistanceScore(distanceToPlayer);

        // Line of sight score is now handled by EnemyManager
        if (PrioritizeLineOfSight && hasLineOfSightToPlayer)
        {
            score += 2f;
        }

        // Enemy spacing score
        score += GetSpacingScore(position);

        // Surrounding score (prefer positions that help surround the player)
        score += GetSurroundingScore(position);

        return score;
    }

    private float GetDistanceScore(float distance)
    {
        // Prefer distances in the middle of the min-max range
        float optimalDistance = (MinPlayerDistance + MaxPlayerDistance) * 0.5f;
        return 1f - Mathf.Abs(distance - optimalDistance) / MaxPlayerDistance;
    }

    private float GetSpacingScore(Vector3 position)
    {
        float score = 0f;
        
        foreach (var enemy in nearbyEnemies)
        {
            float distance = Vector3.Distance(position, enemy.transform.position);
            if (distance < MinEnemySpacing)
            {
                // Heavily penalize positions too close to other enemies
                score -= (MinEnemySpacing - distance) * 2f;
            }
        }
        
        return score;
    }

    private float GetSurroundingScore(Vector3 position)
    {
        // Calculate the angle between this position and other enemies relative to the player
        float score = 0f;
        Vector3 dirToPosition = (position - PlayerTransform.position).normalized;
        
        foreach (var enemy in nearbyEnemies)
        {
            Vector3 dirToEnemy = (enemy.transform.position - PlayerTransform.position).normalized;
            float angle = Vector3.Angle(dirToPosition, dirToEnemy);
            
            // Prefer positions that maximize angles between enemies (better surrounding)
            score += Mathf.Clamp01(angle / 180f);
        }
        
        return score;
    }

    #region Line of Sight
    public bool CheckLineOfSight()
    {
        Vector3 directionToPlayer = (PlayerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
        
        return !Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, ObstacleLayerMask);
    }

    public bool HasLineOfSightFrom(Vector3 position)
    {
        Vector3 directionToPlayer = (PlayerTransform.position - position).normalized;
        float distanceToPlayer = Vector3.Distance(position, PlayerTransform.position);
        
        return !Physics.Raycast(position, directionToPlayer, distanceToPlayer, ObstacleLayerMask);
    }

    public void HandleLineOfSightResult(bool hasLineOfSight)
    {
        hasLineOfSightToPlayer = hasLineOfSight;
    }
    #endregion

    #region Public Interface
    public bool HasLineOfSight() => hasLineOfSightToPlayer;
    public float GetDistanceToPlayer() => Vector3.Distance(transform.position, PlayerTransform.position);
    public Vector3 GetDirectionToPlayer() => (PlayerTransform.position - transform.position).normalized;

    public void ForceNewPosition()
    {
        ChooseNewPosition();
    }
    #endregion
}
