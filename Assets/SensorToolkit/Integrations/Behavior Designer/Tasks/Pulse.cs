using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BDRT = BehaviorDesigner.Runtime.Tasks;

namespace Micosmo.SensorToolkit.BehaviorDesigner {

    [TaskCategory("SensorToolkit")]
    [TaskIcon("Assets/Gizmos/SensorToolkit/SENSORTOOLKIT.png")]
    [TaskDescription("Manually pulses the sensor.")]
    public class Pulse : Action {
        [BDRT.Tooltip("The Sensor to pulse.")]
        public SharedSensor sensor;

        [BDRT.Tooltip("Also pulse any input sensors.")]
        public SharedBool pulseInputs;

        public override TaskStatus OnUpdate() {
            if (sensor.Value == null) {
                return TaskStatus.Failure;
            }
            if (pulseInputs.Value) {
                sensor.Value.PulseAll();
            } else {
                sensor.Value.Pulse();
            }
            return TaskStatus.Success;
        }

        public override void OnReset() {
            sensor = null;
            pulseInputs = false;
        }

    }

}