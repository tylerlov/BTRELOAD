using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using FluffyUnderware.Curvy;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using HelpURL = BehaviorDesigner.Runtime.Tasks.HelpURLAttribute;
using System.Collections.Generic;

namespace BehaviorDesigner.Runtime.Tactical.Tasks
{
    [TaskCategory("Tactical")]
    [TaskDescription("Moves position along a Curvy Spline to maintain ideal range from the target")]
    [HelpURL("https://www.opsive.com/support/documentation/behavior-designer-tactical-pack/")]
    [TaskIcon("Assets/Behavior Designer Tactical/Editor/Icons/{SkinColor}ShootAndScootIcon.png")]
    public class CurvyShootAndScoot : Action
    {
        [Tooltip("List of spline lists for different waves")]
        public List<SharedVariable> splineListVariables = new List<SharedVariable>();

        [Tooltip("The tag of the target to follow")]
        public SharedString targetTag = "Player";
        [Tooltip("The amount of time that should elapse before moving to the next position")]
        public SharedFloat timeStationary = 1;
        [Tooltip("The speed at which the agent moves (units per second along the spline)")]
        public SharedFloat moveSpeed = 5f;
        [Tooltip("When moving positions the agents will move based on a new random angle. The minimum move angle specifies the minimum random angle")]
        public SharedFloat minMoveAngle = 10;
        [Tooltip("When moving positions the agents will move based on a new random angle. The maximum move angle specifies the maximum random angle")]
        public SharedFloat maxMoveAngle = 20;
        [Tooltip("When moving positions the agents will move based on a new random radius. The minimum radius specifies the minimum radius")]
        public SharedFloat minRadius = 5;
        [Tooltip("When moving positions the agents will move based on a new random radius. The maximum radius specifies the maximum radius")]
        public SharedFloat maxRadius = 10;

        private CurvySpline mSpline;
        private int currentSplineIndex;
        private float currentTF;
        private float targetTF;
        private float arrivalTime;
        private bool inPosition;
        private Transform targetTransform;
        private Transform agentTransform;
        private float currentAngle;
        private float currentRadius;
        private SharedFloat speedMultiplier = 0.1f;
        private bool isMovingForward = true;

        private GameManager gameManager;

        public override void OnAwake()
        {
            base.OnAwake();
            agentTransform = transform;
            gameManager = GameManager.instance;
        }

        public override void OnStart()
        {
            if (gameManager == null)
            {
                Debug.LogError("CurvyShootAndScoot: GameManager instance not found.");
                return;
            }

            // Find the player
            GameObject player = GameObject.FindGameObjectWithTag(targetTag.Value);
            if (player == null)
            {
                Debug.LogError("CurvyShootAndScoot: Player not found.");
                return;
            }

            // Determine the closest spline list
            int closestSplineListIndex = FindClosestSplineList(player.transform.position);

            if (closestSplineListIndex >= splineListVariables.Count)
            {
                Debug.LogError($"CurvyShootAndScoot: Not enough spline lists available. Using last available list.");
                closestSplineListIndex = splineListVariables.Count - 1;
            }

            SharedVariable currentSplineListVariable = splineListVariables[closestSplineListIndex];
            List<GameObject> splineList = currentSplineListVariable.GetValue() as List<GameObject>;
            
            if (splineList == null || splineList.Count == 0)
            {
                Debug.LogError($"CurvyShootAndScoot: No spline objects in the global variable list for index {closestSplineListIndex}.");
                return;
            }

            // Randomly select a spline from the chosen list
            currentSplineIndex = Random.Range(0, splineList.Count);
            GameObject selectedSplineObject = splineList[currentSplineIndex];

            mSpline = selectedSplineObject.GetComponent<CurvySpline>();
            if (mSpline == null || !mSpline.IsInitialized)
            {
                Debug.LogError($"CurvyShootAndScoot: Invalid or uninitialized Curvy Spline at index {currentSplineIndex}.");
                return;
            }

            FindTarget();

            arrivalTime = -timeStationary.Value;
            currentTF = mSpline.GetNearestPointTF(agentTransform.position);
            inPosition = false;
            DetermineNewPosition();
            UpdateAgentPosition();
        }

        private int FindClosestSplineList(Vector3 playerPosition)
        {
            int closestIndex = 0;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < splineListVariables.Count; i++)
            {
                List<GameObject> splineList = splineListVariables[i].GetValue() as List<GameObject>;
                if (splineList != null && splineList.Count > 0)
                {
                    float avgDistance = CalculateAverageDistance(splineList, playerPosition);
                    if (avgDistance < closestDistance)
                    {
                        closestDistance = avgDistance;
                        closestIndex = i;
                    }
                }
            }

            return closestIndex;
        }

        private float CalculateAverageDistance(List<GameObject> splineList, Vector3 playerPosition)
        {
            float totalDistance = 0f;
            int validSplines = 0;

            foreach (GameObject splineObject in splineList)
            {
                if (splineObject != null)
                {
                    CurvySpline spline = splineObject.GetComponent<CurvySpline>();
                    if (spline != null && spline.IsInitialized)
                    {
                        totalDistance += Vector3.Distance(playerPosition, spline.Interpolate(0.5f, Space.World));
                        validSplines++;
                    }
                }
            }

            return validSplines > 0 ? totalDistance / validSplines : float.MaxValue;
        }

        public override TaskStatus OnUpdate()
        {
            FindTarget();

            if (!inPosition || (arrivalTime + timeStationary.Value < Time.time))
            {
                if (inPosition)
                {
                    DetermineNewPosition();
                }

                MoveTowardsTarget();

                if (HasArrived())
                {
                    if (RotateTowardsTarget())
                    {
                        inPosition = true;
                        arrivalTime = Time.time;
                    }
                }
            }

            return TaskStatus.Running;
        }

        private void DetermineNewPosition()
        {
            currentAngle += Random.Range(minMoveAngle.Value, maxMoveAngle.Value) * (Random.value > 0.5f ? 1 : -1);
            currentRadius = Random.Range(minRadius.Value, maxRadius.Value);

            Vector3 center = mSpline.Interpolate(currentTF, Space.World);
            Vector3 newPosition = center + Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * currentRadius;
            targetTF = mSpline.GetNearestPointTF(newPosition);

            DetermineBestMoveDirection();
        }

        private void DetermineBestMoveDirection()
        {
            if (targetTransform == null) return;

            Vector3 currentPosition = mSpline.Interpolate(currentTF, Space.World);
            Vector3 forwardPosition = mSpline.Interpolate((currentTF + 0.1f) % 1f, Space.World);
            Vector3 backwardPosition = mSpline.Interpolate((currentTF - 0.1f + 1f) % 1f, Space.World);

            float distanceToTargetCurrent = Vector3.Distance(currentPosition, targetTransform.position);
            float distanceToTargetForward = Vector3.Distance(forwardPosition, targetTransform.position);
            float distanceToTargetBackward = Vector3.Distance(backwardPosition, targetTransform.position);

            if (distanceToTargetForward < distanceToTargetCurrent && distanceToTargetForward <= distanceToTargetBackward)
            {
                isMovingForward = true;
            }
            else if (distanceToTargetBackward < distanceToTargetCurrent && distanceToTargetBackward < distanceToTargetForward)
            {
                isMovingForward = false;
            }
            // If current position is best, maintain current direction
        }

        private void MoveTowardsTarget()
        {
            float direction = isMovingForward ? 1f : -1f;
            float adjustedSpeed = moveSpeed.Value * speedMultiplier.Value;
            currentTF += direction * adjustedSpeed * Time.deltaTime / mSpline.Length;

            if (currentTF >= 1f)
                currentTF -= 1f;
            else if (currentTF < 0f)
                currentTF += 1f;

            UpdateAgentPosition();
        }

        private void UpdateAgentPosition()
        {
            Vector3 worldPosition = mSpline.Interpolate(currentTF, Space.World);
            agentTransform.position = worldPosition;

            Vector3 tangent = mSpline.GetTangent(currentTF, Space.World);
            Vector3 up = mSpline.GetOrientationUpFast(currentTF, Space.World);

            agentTransform.rotation = Quaternion.LookRotation(tangent, up);
        }

        private bool HasArrived()
        {
            return Mathf.Abs(currentTF - targetTF) < 0.01f;
        }

        private bool RotateTowardsTarget()
        {
            if (targetTransform == null)
                return false;

            Vector3 targetDirection = targetTransform.position - agentTransform.position;
            targetDirection.y = 0;
            
            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                agentTransform.rotation = Quaternion.RotateTowards(agentTransform.rotation, targetRotation, 360 * Time.deltaTime);
                
                return Quaternion.Angle(agentTransform.rotation, targetRotation) < 0.1f;
            }

            return true;
        }

        private void FindTarget()
        {
            GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag.Value);
            if (targetObject != null)
            {
                targetTransform = targetObject.transform;
            }
            else
            {
                targetTransform = null;
            }
        }

        public override void OnReset()
        {
            splineListVariables.Clear();
            timeStationary = 2f;
            moveSpeed = 5f;
            speedMultiplier = 0.1f;
            minMoveAngle = 10f;
            maxMoveAngle = 20f;
            minRadius = 5f;
            maxRadius = 10f;
            targetTag = "Player";
        }
    }
}