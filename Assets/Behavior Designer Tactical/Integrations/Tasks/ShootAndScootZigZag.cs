using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using HelpURL = BehaviorDesigner.Runtime.Tasks.HelpURLAttribute;

namespace BehaviorDesigner.Runtime.Tactical.AstarPathfindingProject.Tasks
{
    [TaskCategory("Tactical/A* Pathfinding Project")]
    [TaskDescription("Attacks the target while moving in an erratic zig-zag pattern")]
    [HelpURL("https://www.opsive.com/support/documentation/behavior-designer-tactical-pack/")]
    [TaskIcon("Assets/Behavior Designer Tactical/Editor/Icons/{SkinColor}ShootAndScootIcon.png")]
    public class ShootAndScootZigZag : ShootAndScoot
    {
        [Tooltip("The minimum distance to move in each zig-zag")]
        public SharedFloat minZigZagDistance = 2f;

        [Tooltip("The maximum distance to move in each zig-zag")]
        public SharedFloat maxZigZagDistance = 5f;

        [Tooltip("The minimum time to wait between zig-zags")]
        public SharedFloat minZigZagWaitTime = 0.5f;

        [Tooltip("The maximum time to wait between zig-zags")]
        public SharedFloat maxZigZagWaitTime = 1.5f;

        [Tooltip("The probability of moving towards the target (vs. away from it)")]
        public SharedFloat moveTowardsProbability = 0.6f;

        [Tooltip("The minimum number of zig-zags before pausing")]
        public SharedInt minZigZagCount = 2;

        [Tooltip("The maximum number of zig-zags before pausing")]
        public SharedInt maxZigZagCount = 5;

        private Vector3 currentZigZagDestination;
        private float nextZigZagTime;
        private int remainingZigZags;
        private bool isZig = true;
        private Vector3 zigDirection;
        private Vector3 zagDirection;

        public override void OnStart()
        {
            base.OnStart();
            SetNextZigZagSequence();
        }

        public override TaskStatus OnUpdate()
        {
            var baseStatus = base.OnUpdate();
            if (baseStatus != TaskStatus.Running || !started) {
                return baseStatus;
            }

            if (Time.time >= nextZigZagTime)
            {
                if (remainingZigZags > 0)
                {
                    SetNextZigZagDestination();
                    remainingZigZags--;
                }
                else
                {
                    SetNextZigZagSequence();
                }
            }

            // Move towards the current zig-zag destination
            tacticalAgent.SetDestination(currentZigZagDestination);

            // Attack if possible
            if (canAttack)
            {
                FindAttackTarget();
                tacticalAgent.TryAttack();
            }

            return TaskStatus.Running;
        }

        private void SetNextZigZagSequence()
        {
            remainingZigZags = Random.Range(minZigZagCount.Value, maxZigZagCount.Value + 1) * 2; // Multiply by 2 for zig + zag
            isZig = true;

            Vector3 targetDirection;
            if (tacticalAgent.TargetTransform != null)
            {
                targetDirection = (tacticalAgent.TargetTransform.position - transform.position).normalized;
            }
            else
            {
                targetDirection = Random.insideUnitSphere.normalized;
            }

            // Determine if we're moving towards or away from the target
            if (Random.value > moveTowardsProbability.Value)
            {
                targetDirection = -targetDirection;
            }

            // Calculate zig and zag directions
            Vector3 perpendicularDirection = Vector3.Cross(targetDirection, Vector3.up).normalized;
            zigDirection = (targetDirection + perpendicularDirection).normalized;
            zagDirection = (targetDirection - perpendicularDirection).normalized;

            SetNextZigZagDestination();
        }

        private void SetNextZigZagDestination()
        {
            float zigZagDistance = Random.Range(minZigZagDistance.Value, maxZigZagDistance.Value);
            Vector3 direction = isZig ? zigDirection : zagDirection;

            currentZigZagDestination = transform.position + direction * zigZagDistance;
            isZig = !isZig; // Toggle between zig and zag

            // Set the next zig-zag time
            nextZigZagTime = Time.time + (remainingZigZags > 0 ? 0 : Random.Range(minZigZagWaitTime.Value, maxZigZagWaitTime.Value));
        }

        public override void OnReset()
        {
            base.OnReset();

            minZigZagDistance = 2f;
            maxZigZagDistance = 5f;
            minZigZagWaitTime = 0.5f;
            maxZigZagWaitTime = 1.5f;
            moveTowardsProbability = 0.6f;
            minZigZagCount = 2;
            maxZigZagCount = 5;
        }
    }
}