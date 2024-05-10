using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using BDRT = BehaviorDesigner.Runtime.Tasks;

namespace Micosmo.SensorToolkit.BehaviorDesigner {

    [TaskCategory("SensorToolkit")]
    [TaskIcon("Assets/Gizmos/SensorToolkit/STEERING.png")]
    [TaskDescription("Returns success when the SteeringSensor has reached its destination. Returns false otherwise.")]
    public class IsDestinationReached : Conditional {

        [ObjectType(typeof(ISteeringSensor))]
        [BDRT.Tooltip("The Sensor to query.")]
        public SharedSensor sensor;

        public override TaskStatus OnUpdate() {
            var actualSensor = (sensor?.Value as ISteeringSensor);

            if (actualSensor == null) {
                return TaskStatus.Failure;
            }

            if (actualSensor.IsDestinationReached) {
                return TaskStatus.Success;
            }
            
            return TaskStatus.Failure;
        }

        public override void OnReset() {
            sensor = null;
        }

    }

}