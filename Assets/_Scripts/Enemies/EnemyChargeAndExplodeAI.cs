using UnityEngine;
using Pathfinding;
using System.Collections;

[RequireComponent(typeof(FollowerEntity))]
[RequireComponent(typeof(EnemyExplodeSetup))]
public class EnemyChargeAndExplodeAI : MonoBehaviour
{
    [Header("Charge Settings")]
    [Tooltip("Distance to maintain in phase 1")]
    public float phase1Distance = 10f;
    [Tooltip("Duration of phase 1 in seconds")]
    public float phase1Duration = 10f;
    [Tooltip("Distance to maintain in phase 2")]
    public float phase2Distance = 5f;
    [Tooltip("Duration of phase 2 in seconds")]
    public float phase2Duration = 5f;
    [Tooltip("Final approach distance for explosion")]
    public float explosionDistance = 0.5f;

    private FollowerEntity followerEntity;
    private EnemyExplodeSetup enemySetup;
    private Transform playerTransform;
    private float phaseTimer;
    private int currentPhase = 1;

    private void Awake()
    {
        followerEntity = GetComponent<FollowerEntity>();
        enemySetup = GetComponent<EnemyExplodeSetup>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Start()
    {
        StartCoroutine(ChargeAndExplodeRoutine());
    }

    private IEnumerator ChargeAndExplodeRoutine()
    {
        phaseTimer = phase1Duration;
        currentPhase = 1;

        while (true)
        {
            Vector3 targetPosition = playerTransform.position;
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

            switch (currentPhase)
            {
                case 1:
                    ChaseWithDistance(targetPosition, phase1Distance);
                    break;
                case 2:
                    ChaseWithDistance(targetPosition, phase2Distance);
                    break;
                case 3:
                    if (distanceToTarget <= explosionDistance)
                    {
                        enemySetup.Explode();
                        yield break; // End the coroutine after exploding
                    }
                    followerEntity.destination = targetPosition;
                    break;
            }

            phaseTimer -= Time.deltaTime;
            if (phaseTimer <= 0 && currentPhase < 3)
            {
                currentPhase++;
                phaseTimer = (currentPhase == 2) ? phase2Duration : float.MaxValue;
            }

            yield return null;
        }
    }

    private void ChaseWithDistance(Vector3 targetPosition, float desiredDistance)
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Vector3 desiredPosition = targetPosition - directionToTarget * desiredDistance;
        followerEntity.destination = desiredPosition;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}