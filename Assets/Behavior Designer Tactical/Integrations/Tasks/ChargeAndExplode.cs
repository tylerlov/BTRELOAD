using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using HelpURL = BehaviorDesigner.Runtime.Tasks.HelpURLAttribute;

namespace BehaviorDesigner.Runtime.Tactical.AstarPathfindingProject.Tasks
{
    [TaskCategory("Tactical/A* Pathfinding Project")]
    [TaskDescription("Chases the target in phases, maintaining set distances before finally attempting to touch")]
    [HelpURL("https://www.opsive.com/support/documentation/behavior-designer-tactical-pack/")]
    [TaskIcon("Assets/Behavior Designer Tactical/Editor/Icons/{SkinColor}ChargeIcon.png")]
    public class PhasedChaseAndExplode : IAstarAITacticalGroup
    {
        [Tooltip("Distance to maintain in phase 1")]
        public SharedFloat phase1Distance = 10f;
        [Tooltip("Duration of phase 1 in seconds")]
        public SharedFloat phase1Duration = 10f;
        [Tooltip("Distance to maintain in phase 2")]
        public SharedFloat phase2Distance = 5f;
        [Tooltip("Duration of phase 2 in seconds")]
        public SharedFloat phase2Duration = 5f;
        [Tooltip("Final approach distance for explosion")]
        public SharedFloat explosionDistance = 0.5f;

        private float phaseTimer;
        private int currentPhase = 1;

        public override void OnStart()
        {
            base.OnStart();
            phaseTimer = phase1Duration.Value;
            currentPhase = 1;
        }

        public override TaskStatus OnUpdate()
        {
            var baseStatus = base.OnUpdate();
            if (baseStatus != TaskStatus.Running || !started)
            {
                return baseStatus;
            }

            Vector3 targetPosition = GetTargetPosition();
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

            switch (currentPhase)
            {
                case 1:
                    ChaseWithDistance(targetPosition, phase1Distance.Value);
                    break;
                case 2:
                    ChaseWithDistance(targetPosition, phase2Distance.Value);
                    break;
                case 3:
                    if (distanceToTarget <= explosionDistance.Value)
                    {
                        tacticalAgent.TryAttack();
                        return TaskStatus.Success;
                    }
                    tacticalAgent.SetDestination(targetPosition);
                    break;
            }

            phaseTimer -= Time.deltaTime;
            if (phaseTimer <= 0 && currentPhase < 3)
            {
                currentPhase++;
                phaseTimer = (currentPhase == 2) ? phase2Duration.Value : float.MaxValue;
            }

            return TaskStatus.Running;
        }

        private Vector3 GetTargetPosition()
        {
            // Implement logic to get the player's position
            // This might involve using a shared variable or finding the player in the scene
            return GameObject.FindGameObjectWithTag("Player").transform.position;
        }

        private void ChaseWithDistance(Vector3 targetPosition, float desiredDistance)
        {
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            Vector3 desiredPosition = targetPosition - directionToTarget * desiredDistance;
            tacticalAgent.SetDestination(desiredPosition);
        }

        public override void OnReset()
        {
            base.OnReset();
            phase1Distance = 10f;
            phase1Duration = 10f;
            phase2Distance = 5f;
            phase2Duration = 5f;
            explosionDistance = 0.5f;
        }
    }
}