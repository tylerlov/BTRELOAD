using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BDRT = BehaviorDesigner.Runtime.Tasks;

namespace Micosmo.SensorToolkit.BehaviorDesigner {

    [TaskCategory("SensorToolkit")]
    [TaskIcon("Assets/Gizmos/SensorToolkit/STEERING.png")]
    [TaskDescription("Stops the steering sensor from seeking towards a destination.")]
    public class SetSeekStop : Action {

        [ObjectType(typeof(ISteeringSensor))]
        [BDRT.Tooltip("The steering sensor.")]
        public SharedSensor sensor;

        public override TaskStatus OnUpdate() {
            var actualSensor = (sensor?.Value as ISteeringSensor);

            if (actualSensor == null) {
                return TaskStatus.Failure;
            }

            actualSensor.Stop();
            return TaskStatus.Success;
        }

        public override void OnReset() {
            sensor = null;
        }

    }

}