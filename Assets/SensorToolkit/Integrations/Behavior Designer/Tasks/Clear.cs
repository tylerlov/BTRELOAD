using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BDRT = BehaviorDesigner.Runtime.Tasks;

namespace Micosmo.SensorToolkit.BehaviorDesigner {

    [TaskCategory("SensorToolkit")]
    [TaskIcon("Assets/Gizmos/SensorToolkit/SENSORTOOLKIT.png")]
    [TaskDescription("Clears a sensor of its detections.")]
    public class Clear : Action {
        [BDRT.Tooltip("The Sensor to clear.")]
        public SharedSensor sensor;

        public override TaskStatus OnUpdate() {
            if (sensor.Value == null) {
                return TaskStatus.Failure;
            }
            sensor.Value.Clear();
            return TaskStatus.Success;
        }

        public override void OnReset() {
            sensor = null;
        }
    }

}