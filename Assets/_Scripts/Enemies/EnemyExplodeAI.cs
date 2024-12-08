using UnityEngine;
using Pathfinding;
using System.Collections;

[RequireComponent(typeof(FollowerEntity))]
[RequireComponent(typeof(EnemyExplodeBasics))]
[RequireComponent(typeof(EnemyExplodeCombat))]
public class EnemyExplodeAI : EnemyBasicAI
{
    [Header("Phase Settings")]
    [SerializeField] private float phase1Distance = 10f;
    [SerializeField] private float phase1Duration = 10f;
    [SerializeField] private float phase2Distance = 5f;
    [SerializeField] private float phase2Duration = 5f;
    [SerializeField] private float explosionDistance = 0.5f;

    private EnemyExplodeBasics explodeBasics;
    private EnemyExplodeCombat explodeCombat;
    private float phaseTimer;
    private int currentPhase = 1;
    private Coroutine phaseRoutine;

    protected override void Awake()
    {
        base.Awake();
        explodeBasics = GetComponent<EnemyExplodeBasics>();
        explodeCombat = GetComponent<EnemyExplodeCombat>();
    }

    private void OnEnable()
    {
        StartPhaseRoutine();
    }

    private void OnDisable()
    {
        StopPhaseRoutine();
    }

    private void StartPhaseRoutine()
    {
        StopPhaseRoutine();
        phaseRoutine = StartCoroutine(PhaseRoutine());
    }

    private void StopPhaseRoutine()
    {
        if (phaseRoutine != null)
        {
            StopCoroutine(phaseRoutine);
            phaseRoutine = null;
        }
    }

    private IEnumerator PhaseRoutine()
    {
        while (true)
        {
            float targetDistance = currentPhase == 1 ? phase1Distance : phase2Distance;
            float phaseDuration = currentPhase == 1 ? phase1Duration : phase2Duration;

            // Move towards player while maintaining distance
            Vector3 directionToPlayer = (PlayerTransform.position - transform.position).normalized;
            Vector3 targetPosition = PlayerTransform.position - directionToPlayer * targetDistance;
            FollowerEntity.destination = targetPosition;

            phaseTimer += Time.deltaTime;

            // Check if it's time to switch phases
            if (phaseTimer >= phaseDuration)
            {
                phaseTimer = 0f;
                currentPhase = currentPhase == 1 ? 2 : 1;
            }

            // If in phase 2 and close enough, trigger explosion
            if (currentPhase == 2 && Vector3.Distance(transform.position, PlayerTransform.position) <= explosionDistance)
            {
                explodeCombat.TryExplode();
                yield break;
            }

            yield return null;
        }
    }
}
