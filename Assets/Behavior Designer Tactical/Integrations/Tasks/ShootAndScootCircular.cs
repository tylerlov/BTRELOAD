using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using HelpURL = BehaviorDesigner.Runtime.Tasks.HelpURLAttribute;

namespace BehaviorDesigner.Runtime.Tactical.AstarPathfindingProject.Tasks
{
    [TaskCategory("Tactical/A* Pathfinding Project")]
    [TaskDescription("Attacks the target while moving in circular and figure-eight patterns")]
    [HelpURL("https://www.opsive.com/support/documentation/behavior-designer-tactical-pack/")]
    [TaskIcon("Assets/Behavior Designer Tactical/Editor/Icons/{SkinColor}ShootAndScootIcon.png")]
    public class ShootAndScootCircular : ShootAndScoot
    {
        [Tooltip("The radius of the circular movement")]
        public SharedFloat circleRadius = 5f;

        [Tooltip("The speed of the circular movement")]
        public SharedFloat circularSpeed = 2f;

        [Tooltip("The probability of switching to figure-eight pattern")]
        public SharedFloat figureEightProbability = 0.3f;

        [Tooltip("The width of the figure-eight pattern")]
        public SharedFloat figureEightWidth = 10f;

        [Tooltip("The height of the figure-eight pattern")]
        public SharedFloat figureEightHeight = 5f;

        private Vector3 circleCenter;
        private float currentAngle;
        private bool isMovingInFigureEight;
        private float figureEightProgress;
        private Vector3 figureEightCenter;

        public override void OnStart()
        {
            base.OnStart();
            InitializeMovement();
        }

        public override TaskStatus OnUpdate()
        {
            var baseStatus = base.OnUpdate();
            if (baseStatus != TaskStatus.Running || !started) {
                return baseStatus;
            }

            Vector3 targetPosition;
            if (isMovingInFigureEight)
            {
                targetPosition = CalculateFigureEightPosition();
                figureEightProgress += Time.deltaTime * circularSpeed.Value;
                if (figureEightProgress >= Mathf.PI * 2)
                {
                    InitializeMovement();
                }
            }
            else
            {
                targetPosition = CalculateCircularPosition();
                currentAngle += circularSpeed.Value * Time.deltaTime;
                if (currentAngle >= Mathf.PI * 2)
                {
                    InitializeMovement();
                }
            }

            tacticalAgent.SetDestination(targetPosition);

            // Attack if possible
            if (canAttack)
            {
                FindAttackTarget();
                tacticalAgent.TryAttack();
            }

            return TaskStatus.Running;
        }

        private void InitializeMovement()
        {
            if (Random.value < figureEightProbability.Value)
            {
                isMovingInFigureEight = true;
                figureEightProgress = 0f;
                figureEightCenter = transform.position;
            }
            else
            {
                isMovingInFigureEight = false;
                currentAngle = 0f;
                circleCenter = transform.position;
            }
        }

        private Vector3 CalculateCircularPosition()
        {
            float x = Mathf.Cos(currentAngle) * circleRadius.Value;
            float z = Mathf.Sin(currentAngle) * circleRadius.Value;
            return circleCenter + new Vector3(x, 0, z);
        }

        private Vector3 CalculateFigureEightPosition()
        {
            float x = Mathf.Sin(figureEightProgress) * figureEightWidth.Value * 0.5f;
            float z = Mathf.Sin(figureEightProgress * 2) * figureEightHeight.Value * 0.5f;
            return figureEightCenter + new Vector3(x, 0, z);
        }

        public override void OnReset()
        {
            base.OnReset();

            circleRadius = 5f;
            circularSpeed = 2f;
            figureEightProbability = 0.3f;
            figureEightWidth = 10f;
            figureEightHeight = 5f;
        }
    }
}