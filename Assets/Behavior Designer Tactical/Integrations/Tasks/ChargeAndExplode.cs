using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using HelpURL = BehaviorDesigner.Runtime.Tasks.HelpURLAttribute;

namespace BehaviorDesigner.Runtime.Tactical.AstarPathfindingProject.Tasks
{
    [TaskCategory("Tactical/A* Pathfinding Project")]
    [TaskDescription("Charges towards the target in cycles, stopping at decreasing distances before attacking")]
    [HelpURL("https://www.opsive.com/support/documentation/behavior-designer-tactical-pack/")]
    [TaskIcon("Assets/Behavior Designer Tactical/Editor/Icons/{SkinColor}ChargeIcon.png")]
    public class CyclingChargeAndAttack : IAstarAITacticalGroup
    {
        [Tooltip("The number of agents that should be in a row")]
        public SharedInt agentsPerRow = 2;
        [Tooltip("The separation between agents")]
        public SharedVector2 separation = new Vector2(2, 2);
        [Tooltip("The maximum distance to stop at during the first cycle")]
        public SharedFloat maximumStopDistance = 10f;
        [Tooltip("The distance to decrease the stop point by each cycle")]
        public SharedFloat stoppingDifference = 5f;
        [Tooltip("The time to wait at each stop point")]
        public SharedFloat stopDuration = 2f;
        [Tooltip("The final attack distance")]
        public SharedFloat attackDistance = 2f;

        private Vector3 offset;
        private bool inPosition;
        private float currentStopDistance;
        private float stopTimer;
        private bool isStopped;

        protected override void FormationUpdated(int index)
        {
            base.FormationUpdated(index);

            if (leader.Value != null)
            {
                var row = formationIndex / agentsPerRow.Value;
                var column = formationIndex % agentsPerRow.Value;

                // Arrange the agents in charging position.
                if (column == 0)
                {
                    offset.Set(0, 0, -separation.Value.y * row);
                }
                else
                {
                    offset.Set(separation.Value.x * (column % 2 == 0 ? -1 : 1) * (((column - 1) / 2) + 1), 0, -separation.Value.y * row);
                }
            }

            inPosition = false;
            currentStopDistance = maximumStopDistance.Value;
            stopTimer = 0f;
            isStopped = false;
        }
        
        public override TaskStatus OnUpdate()
        {
            var baseStatus = base.OnUpdate();
            if (baseStatus != TaskStatus.Running || !started)
            {
                return baseStatus;
            }

            var attackCenter = CenterAttackPosition();
            var attackRotation = ReverseCenterAttackRotation(attackCenter);

            // Move the agents into their starting position if they haven't been there already.
            if (!inPosition)
            {
                var leaderTransform = leader.Value != null ? leader.Value.transform : transform;
                var destination = TransformPoint(leaderTransform.position, offset, attackRotation);
                if (tacticalAgent.HasArrived())
                {
                    // The agent is in position but it may not be facing the target.
                    if (tacticalAgent.RotateTowardsPosition(TransformPoint(attackCenter, offset, attackRotation)))
                    {
                        inPosition = true;
                        // Notify the leader when the agent is in position.
                        if (leaderTree != null)
                        {
                            leaderTree.SendEvent("UpdateInPosition", formationIndex, true);
                        }
                        else
                        {
                            UpdateInPosition(0, true);
                        }
                    }
                }
                else
                {
                    tacticalAgent.SetDestination(destination);
                }
            }
            else if (canAttack)
            {
                var destination = TransformPoint(attackCenter, offset, attackRotation);
                var distanceToTarget = (destination - transform.position).magnitude;

                if (distanceToTarget <= attackDistance.Value)
                {
                    // We've reached the final attack distance, start attacking
                    tacticalAgent.AttackPosition = true;
                    if (MoveToAttackPosition())
                    {
                        tacticalAgent.TryAttack();
                    }
                }
                else if (distanceToTarget <= currentStopDistance)
                {
                    // We've reached a stop point
                    if (!isStopped)
                    {
                        isStopped = true;
                        stopTimer = stopDuration.Value;
                    }

                    if (stopTimer > 0)
                    {
                        stopTimer -= Time.deltaTime;
                    }
                    else
                    {
                        // Stop duration is over, move to the next cycle
                        isStopped = false;
                        currentStopDistance -= stoppingDifference.Value;
                        currentStopDistance = Mathf.Max(currentStopDistance, attackDistance.Value);
                    }
                }
                else
                {
                    // Continue moving towards the target
                    tacticalAgent.SetDestination(destination);
                }
            }

            return TaskStatus.Running;
        }

        public override void OnReset()
        {
            base.OnReset();

            agentsPerRow = 2;
            separation = new Vector2(2, 2);
            maximumStopDistance = 10f;
            stoppingDifference = 5f;
            stopDuration = 2f;
            attackDistance = 2f;
        }
    }
}