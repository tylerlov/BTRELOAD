using UnityEngine;
using BehaviorDesigner.Runtime.Tactical.Tasks;
using Pathfinding;

namespace BehaviorDesigner.Runtime.Tactical.AstarPathfindingProject.Tasks
{
    /// <summary>
    /// Base class for all IAstarAI Tactical tasks.
    /// </summary>
    public abstract class IAstarAITacticalGroup : TacticalGroup
    {
        [Tooltip("The radius of the agent.")]
        public float agentRadius = 1;
        [Tooltip("The distance that the agent can stop in front of the destination.")]
        public float stoppingDistance = 0.2f;
        [Tooltip("The rotation speed of the agent.")]
        public float rotationSpeed = 100;
        [Tooltip("The agent is considered in rotation position if the delta angle is less then this threshold between the agent's rotation and target rotation.")]
        public float rotationThreshold = 1;

        /// <summary>
        /// The IAstarAITacticalAgent class contains component references and variables for each IAstarAI agent.
        /// </summary>
        private class NavMeshTacticalAgent : TacticalAgent
        {
            private float agentRadius;
            private float stoppingDistance;
            private float rotationSpeed;
            private float rotationThreshold;
            private IAstarAI aStarAgent;
            private bool destinationSet;

            /// <summary>
            /// Caches the component references and initialize default values.
            /// </summary>
            public NavMeshTacticalAgent(Transform agent, float radius, float dist, float rotation, float rotThreshold) : base(agent)
            {
                aStarAgent = agent.GetComponent<IAstarAI>();
                agentRadius = radius;
                stoppingDistance = dist;
                rotationSpeed = rotation;
                rotationThreshold = rotThreshold;

                Stop();
            }

            /// <summary>
            /// Sets the destination.
            /// </summary>
            public override void SetDestination(Vector3 destination)
            {
                destinationSet = true;
                if (aStarAgent.destination != destination) {
                    aStarAgent.destination = destination;
                    aStarAgent.canMove = true;
                    aStarAgent.isStopped = false;
                    aStarAgent.SearchPath();
                }
            }

            /// <summary>
            /// Has the agent arrived at its destination?
            /// </summary>
            public override bool HasArrived()
            {
                return destinationSet && aStarAgent.hasPath && Vector3.Distance(transform.position, aStarAgent.destination) <= stoppingDistance;
            }

            /// <summary>
            /// Rotates towards the target rotation.
            /// </summary>
            public override bool RotateTowards(Quaternion targetRotation)
            {
                if (Quaternion.Angle(transform.rotation, targetRotation) < rotationThreshold) {
                    return true;
                }
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                return false;
            }

            /// <summary>
            /// Returns the radius of the agent.
            /// </summary>
            public override float Radius()
            {
                return agentRadius;
            }

            /// <summary>
            /// Starts or stops the rotation from updating.
            /// </summary>
            public override void UpdateRotation(bool update)
            {
            }

            /// <summary>
            /// Stops the agent from moving.
            /// </summary>
            public override void Stop()
            {
                aStarAgent.destination = transform.position;
                aStarAgent.canMove = false;
            }

            /// <summary>
            /// The task has ended. Perform any cleanup.
            /// </summary>
            public override void End()
            {
                Stop();
            }
        }

        /// <summary>
        /// Adds the agent to the agent list.
        /// </summary>
        /// <param name="agent">The agent to add.</param>
        protected override void AddAgentToGroup(Behavior agent, int index)
        {
            base.AddAgentToGroup(agent, index);

            if (tacticalAgent == null && gameObject == agent.gameObject) {
                tacticalAgent = new NavMeshTacticalAgent(agent.transform, agentRadius, stoppingDistance, rotationSpeed, rotationThreshold);
                tacticalAgent.AttackOffset = attackOffset.Value;
                tacticalAgent.TargetOffset = targetOffset.Value;
            }
        }
    }
}