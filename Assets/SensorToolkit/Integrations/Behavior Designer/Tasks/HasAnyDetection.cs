using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BDRT = BehaviorDesigner.Runtime.Tasks;

namespace Micosmo.SensorToolkit.BehaviorDesigner {

    [TaskCategory("SensorToolkit")]
    [TaskIcon("Assets/Gizmos/SensorToolkit/SENSORTOOLKIT.png")]
    [TaskDescription("Returns success if the sensor has at least one detection. Returns failure otherwise.")]
    public class HasAnyDetection : Conditional {
        [ObjectType(typeof(Sensor))]
        [BDRT.Tooltip("The Sensor to query.")]
        public SharedSensor sensor;
        [BDRT.Tooltip("If this is non-empty then detected objects must have this tag.")]
        public SharedString requiredTag;

        bool hasTagFilter => requiredTag?.Value?.Length > 0;

        public override TaskStatus OnUpdate() {
            var actualSensor = (sensor?.Value as Sensor);

            if (actualSensor == null) {
                return TaskStatus.Failure;
            }
            var nearest = hasTagFilter ? actualSensor.GetNearestDetection(requiredTag.Value) : actualSensor.GetNearestDetection();
            return nearest != null ? TaskStatus.Success : TaskStatus.Failure;
        }

        public override void OnReset() {
            sensor = null;
            requiredTag = null;
        }

    }

}