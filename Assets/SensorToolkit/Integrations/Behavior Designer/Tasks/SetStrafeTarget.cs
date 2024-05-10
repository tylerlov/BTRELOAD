using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BDRT = BehaviorDesigner.Runtime.Tasks;

namespace Micosmo.SensorToolkit.BehaviorDesigner {

    [TaskCategory("SensorToolkit")]
    [TaskIcon("Assets/Gizmos/SensorToolkit/STEERING.png")]
    [TaskDescription("If the Steering Sensor has built-in locomotion enabled this will control the strafing behaviour. The target is a direction or GameObject the agent should face while it seeks it's destination. Call this with no parameters to stop strafing.")]
    public class SetStrafeTarget : Action {

        [ObjectType(typeof(ISteeringSensor))]
        [BDRT.Tooltip("The steering sensor.")]
        public SharedSensor sensor;
        [BDRT.Tooltip("The sensor will face towards this GameObject regardless of what direction it moves in.")]
        public SharedGameObject targetGameObject;
        [BDRT.Tooltip("Only applicable when 'targetGameObject' is left empty. The sensor will face in this direction regardless of what direction it moves in.")]
        public SharedVector3 targetDirection;

        public override TaskStatus OnUpdate() {
            var actualSensor = (sensor?.Value as ISteeringSensor);

            if (actualSensor == null) {
                return TaskStatus.Failure;
            }
            
            if (!targetGameObject.IsNone && targetGameObject.Value != null) {
                actualSensor.Locomotion.Strafing.SetFaceTarget(targetGameObject.Value.transform);
            } else if (!targetDirection.IsNone && targetDirection.Value != Vector3.zero) {
                actualSensor.Locomotion.Strafing.SetFaceTarget(targetDirection.Value);
            } else {
                actualSensor.Locomotion.Strafing.Clear();
            }

            return TaskStatus.Success;
        }

        public override void OnReset() {
            sensor = null;
            targetGameObject = null;
            targetDirection = null;
        }

    }

}