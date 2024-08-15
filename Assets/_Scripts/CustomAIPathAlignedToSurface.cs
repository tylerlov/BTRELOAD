using UnityEngine;
using Pathfinding;
using Chronos;

namespace Pathfinding
{
    public class CustomAIPathAlignedToSurface : AIPathAlignedToSurface
    {
        public Timeline clock; // Chronos local clock
        
        [SerializeField] private float pathUpdateInterval = 0.5f; // Time between path updates
        [SerializeField] private float outOfBoundsThreshold = 10f; // Distance threshold to consider out of bounds
        [SerializeField] private float recoveryCheckInterval = 1f; // Time between recovery checks
        [SerializeField] private int maxRecoveryAttempts = 5; // Maximum number of recovery attempts before resetting
        [SerializeField] private Vector3 resetPosition = Vector3.zero; // Position to reset to if recovery fails
        [SerializeField] private float stuckDetectionTime = 3f; // Time to consider an enemy stuck
        
        private float lastPathUpdateTime;
        private float lastRecoveryCheckTime;
        private Vector3 lastKnownGoodPosition;
        private bool isRecovering = false;
        private int recoveryAttempts = 0;
        private float stuckTime = 0f;
        private Vector3 lastPosition;
        private Vector3 lastUpdatePosition;
        private const float updatePositionThreshold = 0.1f;

        protected override void Awake()
        {
            base.Awake();
            clock = GetComponent<Timeline>();
            lastKnownGoodPosition = transform.position;
            lastPosition = transform.position;
            lastUpdatePosition = transform.position;
        }

        void Update()
        {
            float deltaTime = clock.deltaTime;

            // Only update path and perform checks if the agent has moved significantly
            if (Vector3.Distance(transform.position, lastUpdatePosition) > updatePositionThreshold)
            {
                if (clock.time - lastPathUpdateTime >= pathUpdateInterval)
                {
                    lastPathUpdateTime = clock.time;
                    SearchPath();
                }

                if (clock.time - lastRecoveryCheckTime >= recoveryCheckInterval)
                {
                    lastRecoveryCheckTime = clock.time;
                    CheckAndRecover();
                }

                UpdateLastKnownGoodPosition();
                lastUpdatePosition = transform.position;
            }

            base.OnUpdate(deltaTime);

            DetectStuckState(deltaTime);
        }

        private void UpdateLastKnownGoodPosition()
        {
            // Use a non-allocating method to get the nearest node
            var nearestNode = AstarPath.active.GetNearest(transform.position, NNConstraint.Walkable);
            if (nearestNode.node != null && nearestNode.node.Walkable)
            {
                lastKnownGoodPosition = nearestNode.position;
                isRecovering = false;
                recoveryAttempts = 0;
                stuckTime = 0f;
            }
        }

        private void DetectStuckState(float deltaTime)
        {
            if (Vector3.Distance(transform.position, lastPosition) < 0.01f)
            {
                stuckTime += deltaTime;
                if (stuckTime > stuckDetectionTime)
                {
                    ConditionalDebug.LogWarning("Enemy detected as stuck. Initiating recovery.");
                    StartRecovery();
                }
            }
            else
            {
                stuckTime = 0f;
            }
            lastPosition = transform.position;
        }

        private void CheckAndRecover()
        {
            if (Vector3.Distance(transform.position, lastKnownGoodPosition) > outOfBoundsThreshold)
            {
                ConditionalDebug.LogWarning($"Enemy out of bounds. Attempting recovery. Current pos: {transform.position}, Last good pos: {lastKnownGoodPosition}");
                StartRecovery();
            }
        }

        private void StartRecovery()
        {
            isRecovering = true;
            recoveryAttempts++;

            if (recoveryAttempts > maxRecoveryAttempts)
            {
                ConditionalDebug.LogWarning("Max recovery attempts reached. Resetting to default position.");
                ResetToDefaultPosition();
                return;
            }

            Vector3 recoveryPosition = FindSafePosition();
            if (recoveryPosition != Vector3.zero)
            {
                transform.position = recoveryPosition;
                lastKnownGoodPosition = recoveryPosition;
                SearchPath();
            }
            else
            {
                ConditionalDebug.LogWarning("Failed to find valid recovery position. Resetting to default position.");
                ResetToDefaultPosition();
            }
        }

        private Vector3 FindSafePosition()
        {
            // Use a non-allocating method to get the nearest node
            var nearestToDestination = AstarPath.active.GetNearest(destination, NNConstraint.Walkable);
            if (nearestToDestination.node != null && nearestToDestination.node.Walkable)
            {
                return nearestToDestination.position;
            }

            // If that fails, try to find any walkable position within a large radius
            float searchRadius = 50f;
            int maxIterations = 20;
            var constraint = NNConstraint.Walkable;

            for (int i = 0; i < maxIterations; i++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * searchRadius;
                Vector3 testPosition = resetPosition + randomOffset;
                var nearestNode = AstarPath.active.GetNearest(testPosition, constraint);

                if (nearestNode.node != null && nearestNode.node.Walkable)
                {
                    return nearestNode.position;
                }
            }

            return Vector3.zero;
        }

        private void ResetToDefaultPosition()
        {
            transform.position = resetPosition;
            lastKnownGoodPosition = resetPosition;
            isRecovering = false;
            recoveryAttempts = 0;
            stuckTime = 0f;
            SearchPath();
        }

        public override void SearchPath()
        {
            if (canSearch && seeker != null && seeker.IsDone())
            {
                canSearch = false;
                seeker.StartPath(GetFeetPosition(), destination, OnPathComplete);
            }
        }

        protected override void OnPathComplete(Path p)
        {
            base.OnPathComplete(p);

            if (p.error)
            {
                ConditionalDebug.LogWarning("Path failed: " + p.errorLog);
                StartRecovery();
            }
            else if (isRecovering)
            {
                ConditionalDebug.Log("Recovery successful. Path found from recovery position.");
                isRecovering = false;
                recoveryAttempts = 0;
                stuckTime = 0f;
            }

            canSearch = true;
        }
    }
}