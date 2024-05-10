using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BDRT = BehaviorDesigner.Runtime.Tasks;

namespace Micosmo.SensorToolkit.BehaviorDesigner {

    [TaskCategory("SensorToolkit")]
    [TaskIcon("Assets/Gizmos/SensorToolkit/SENSORTOOLKIT.png")]
    [TaskDescription("Returns success if the target object is detected by the sensor. Returns failure otherwise.")]
    public class IsDetected : Conditional {
        [ObjectType(typeof(Sensor))]
        [BDRT.Tooltip("The Sensor to query.")]
        public SharedSensor sensor;
        [BDRT.Tooltip("Will test if this object is detected by the sensor.")]
        public SharedGameObject targetObject;

        public override TaskStatus OnUpdate() {
            var actualSensor = (sensor?.Value as Sensor);

            if (actualSensor == null || targetObject.Value == null) {
                return TaskStatus.Failure;
            }

            return actualSensor.IsDetected(targetObject.Value) ? TaskStatus.Success : TaskStatus.Failure;
        }

        public override void OnReset() {
            sensor = null;
            targetObject = null;
        }

    }

}