using UnityEngine;
using FluffyUnderware.Curvy;
using BehaviorDesigner.Runtime;

namespace BehaviorDesigner.Runtime.Tasks.Movement
{
    [TaskDescription("Follows a target with the specified tag along a Curvy Spline.")]
    [TaskCategory("Movement")]
    public class CurvyFollow : Action
    {
        [Tooltip("The tag of the GameObject that the agent should follow")]
        public SharedString targetTag;
        [Tooltip("The Curvy Spline to move along")]
        public SharedGameObject splineObject;
        [Tooltip("The speed at which the agent moves (units per second along the spline)")]
        public SharedFloat moveSpeed = 5f;
        [Tooltip("Start moving towards the target if the target is further than the specified distance")]
        public SharedFloat moveDistance = 2f;
        [Tooltip("Speed multiplier to fine-tune the movement speed")]
        private SharedFloat speedMultiplier = 0.1f;

        private CurvySpline mSpline;
        private float currentTF = 0f;
        private GameObject targetObject;
        private Transform agentTransform;
        private Vector3 lastTargetPosition;
        private bool hasMoved;

        public override void OnAwake()
        {
            agentTransform = transform;
        }

        public override void OnStart()
        {
            if (splineObject.Value == null)
            {
                Debug.LogError("CurvyFollow: Spline object is not assigned.");
                return;
            }

            mSpline = splineObject.Value.GetComponent<CurvySpline>();
            if (mSpline == null || !mSpline.IsInitialized)
            {
                Debug.LogError("CurvyFollow: Invalid or uninitialized Curvy Spline.");
                return;
            }

            targetObject = GameObject.FindGameObjectWithTag(targetTag.Value);
            if (targetObject == null)
            {
                Debug.LogError($"CurvyFollow: Target not found. GameObject: {gameObject.name}");
                return;
            }

            currentTF = mSpline.GetNearestPointTF(agentTransform.position);
            lastTargetPosition = targetObject.transform.position + Vector3.one * (moveDistance.Value + 1);
            hasMoved = false;
            UpdateAgentPosition();
        }

        public override TaskStatus OnUpdate()
        {
            if (targetObject == null || mSpline == null || !mSpline.IsInitialized)
                return TaskStatus.Failure;

            var targetPosition = targetObject.transform.position;
            var nearestPointOnSpline = mSpline.GetNearestPoint(targetPosition, Space.World);
            var distanceToTarget = Vector3.Distance(agentTransform.position, nearestPointOnSpline);

            if (distanceToTarget >= moveDistance.Value)
            {
                float targetTF = mSpline.GetNearestPointTF(nearestPointOnSpline);
                float direction = Mathf.Sign(targetTF - currentTF);

                // Move along the spline with adjusted speed calculation
                float adjustedSpeed = moveSpeed.Value * speedMultiplier.Value;
                currentTF += direction * adjustedSpeed * Time.deltaTime / mSpline.Length;

                // Ensure we stay within the spline's bounds
                if (currentTF >= 1f)
                    currentTF -= 1f;
                else if (currentTF < 0f)
                    currentTF += 1f;

                UpdateAgentPosition();
                hasMoved = true;
            }
            else if (hasMoved && distanceToTarget < moveDistance.Value)
            {
                hasMoved = false;
            }

            lastTargetPosition = targetPosition;
            return TaskStatus.Running;
        }

        private void UpdateAgentPosition()
        {
            Vector3 worldPosition = mSpline.Interpolate(currentTF, Space.World);
            agentTransform.position = worldPosition;

            Vector3 tangent = mSpline.GetTangent(currentTF, Space.World);
            Vector3 up = mSpline.GetOrientationUpFast(currentTF, Space.World);

            agentTransform.rotation = Quaternion.LookRotation(tangent, up);

            Debug.DrawLine(worldPosition, worldPosition + tangent, Color.blue);
            Debug.DrawLine(worldPosition, worldPosition + up, Color.green);
        }

        public override void OnReset()
        {
            targetTag = null;
            splineObject = null;
            moveSpeed = 5f;
            moveDistance = 2f;
            speedMultiplier = 0.1f;
        }
    }
}