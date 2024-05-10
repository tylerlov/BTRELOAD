using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BDRT = BehaviorDesigner.Runtime.Tasks;

namespace Micosmo.SensorToolkit.BehaviorDesigner {

    [TaskCategory("SensorToolkit")]
    [TaskIcon("Assets/Gizmos/SensorToolkit/STEERING.png")]
    [TaskDescription("Returns success when the SteeringSensor is currently seeking towards a destination. Returns false if it's not seeking or has already reached its destination.")]
    public class IsSeeking : Conditional {

        [ObjectType(typeof(ISteeringSensor))]
        [BDRT.Tooltip("The Sensor to query.")]
        public SharedSensor sensor;
        [BDRT.Tooltip("Stores the current steering vector.")]
        public SharedVector3 storeSteeringVector;

        public override TaskStatus OnUpdate() {
            var actualSensor = (sensor?.Value as ISteeringSensor);

            if (actualSensor == null) {
                return TaskStatus.Failure;
            }

            if (actualSensor.IsDestinationReached) {
                return TaskStatus.Failure;
            }

            storeSteeringVector.Value = actualSensor.GetSteeringVector();

            return TaskStatus.Success;
        }

        public override void OnReset() {
            sensor = null;
            storeSteeringVector = null;
        }

    }

}