using Chronos;
using Pathfinding;
using UnityEngine;

namespace Pathfinding
{
    public class CustomAIPathAlignedToSurface : AIPathAlignedToSurface
    {
        public Timeline clock; // Chronos local clock

        [SerializeField]
        private float pathUpdateInterval = 0.5f; // Time between path updates

        [SerializeField]
        private float outOfBoundsThreshold = 10f; // Distance threshold to consider out of bounds

        [SerializeField]
        private float recoveryCheckInterval = 1f; // Time between recovery checks

        [SerializeField]
        private int maxRecoveryAttempts = 5; // Maximum number of recovery attempts before resetting

        [SerializeField]
        private Vector3 resetPosition = Vector3.zero; // Position to reset to if recovery fails

        [SerializeField]
        private float stuckDetectionTime = 3f; // Time to consider an enemy stuck

        [SerializeField] private float gravityStrength = 9.81f;
        [SerializeField] private float alignmentSpeed = 10f;
        [SerializeField] private float groundCheckDistance = 0.1f;
        [SerializeField] private LayerMask groundLayer;

        private Vector3 surfaceNormal;
        private Vector3 gravityVector;

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
            if (float.IsInfinity(transform.position.x) || float.IsInfinity(transform.position.y) || float.IsInfinity(transform.position.z))
            {
                ConditionalDebug.LogError($"Invalid position detected for {gameObject.name}. Attempting recovery.");
                StartRecovery();
                return;
            }

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

            ApplyGravity();
            AlignToSurface();
            GroundCheck();
        }

        private void UpdateLastKnownGoodPosition()
        {
            // Use a non-allocating method to get the nearest node
            var nearestNode = AstarPath.active.GetNearest(
                transform.position,
                NNConstraint.Walkable
            );
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
                ConditionalDebug.LogWarning(
                    $"Enemy out of bounds. Attempting recovery. Current pos: {transform.position}, Last good pos: {lastKnownGoodPosition}"
                );
                StartRecovery();
            }
        }

        private void StartRecovery()
        {
            if (!isRecovering)
            {
                isRecovering = true;
                recoveryAttempts = 0;
                AttemptRecovery();
            }
        }

        private void AttemptRecovery()
        {
            recoveryAttempts++;
            if (recoveryAttempts <= maxRecoveryAttempts)
            {
                Vector3 recoveryPosition = FindSafePosition();
                if (recoveryPosition != Vector3.zero)
                {
                    transform.position = recoveryPosition;
                    lastKnownGoodPosition = recoveryPosition;
                    SearchPath();
                    isRecovering = false;
                }
                else
                {
                    ConditionalDebug.LogWarning($"Recovery attempt {recoveryAttempts} failed for {gameObject.name}. Retrying...");
                    Invoke("AttemptRecovery", 0.5f);
                }
            }
            else
            {
                ConditionalDebug.LogWarning($"Max recovery attempts reached for {gameObject.name}. Resetting to default position.");
                ResetToDefaultPosition();
            }
        }

        private Vector3 FindSafePosition()
        {
            // Use a non-allocating method to get the nearest node
            var nearestToDestination = AstarPath.active.GetNearest(
                destination,
                NNConstraint.Walkable
            );
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

        private void ApplyGravity()
        {
            if (!Physics.Raycast(transform.position, -transform.up, groundCheckDistance, groundLayer))
            {
                gravityVector = Vector3.down * gravityStrength;
                Vector3 newPosition = transform.position + gravityVector * clock.deltaTime;
                if (!float.IsInfinity(newPosition.x) && !float.IsInfinity(newPosition.y) && !float.IsInfinity(newPosition.z))
                {
                    transform.position = newPosition;
                }
                else
                {
                    ConditionalDebug.LogWarning($"Invalid gravity application for {gameObject.name}");
                    StartRecovery();
                }
            }
        }

        private void AlignToSurface()
        {
            if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, 10f, groundLayer))
            {
                surfaceNormal = hit.normal;
                Quaternion targetRotation = Quaternion.FromToRotation(transform.up, surfaceNormal) * transform.rotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, alignmentSpeed * clock.deltaTime);
            }
        }

        private void GroundCheck()
        {
            if (!Physics.Raycast(transform.position, -transform.up, groundCheckDistance, groundLayer))
            {
                try
                {
                    var nearest = AstarPath.active.GetNearest(transform.position);
                    if (nearest.node != null && nearest.node.Walkable)
                    {
                        Vector3 closestPoint = nearest.position;
                        if (!float.IsInfinity(closestPoint.x) && !float.IsInfinity(closestPoint.y) && !float.IsInfinity(closestPoint.z))
                        {
                            transform.position = Vector3.Lerp(transform.position, closestPoint, 0.5f);
                        }
                        else
                        {
                            ConditionalDebug.LogWarning($"Invalid closest point found for {gameObject.name}: {closestPoint}");
                            StartRecovery();
                        }
                    }
                    else
                    {
                        ConditionalDebug.LogWarning($"No valid node found for {gameObject.name}");
                        StartRecovery();
                    }
                }
                catch (System.Exception e)
                {
                    ConditionalDebug.LogError($"Error in GroundCheck for {gameObject.name}: {e.Message}");
                    StartRecovery();
                }
            }
        }

        public override Vector3 GetFeetPosition()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, -transform.up, out hit, 10f, groundLayer))
            {
                return hit.point;
            }
            return base.GetFeetPosition();
        }

        public override void Move(Vector3 direction)
        {
            // Project the movement direction onto the surface plane
            Vector3 projectedDirection = Vector3.ProjectOnPlane(direction, surfaceNormal).normalized;
            
            // Use the projected direction for movement
            base.Move(projectedDirection * direction.magnitude);
        }
    }
}
