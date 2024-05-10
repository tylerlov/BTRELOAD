using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BDRT = BehaviorDesigner.Runtime.Tasks;

namespace Micosmo.SensorToolkit.BehaviorDesigner {

    [TaskCategory("SensorToolkit")]
    [TaskIcon("Assets/Gizmos/SensorToolkit/SENSORTOOLKIT.png")]
    [TaskDescription("Retrieve the Signal data for a GameObject. This will give you the objects visibility (Signal Strength) and its bounding box. Returns failure if the object isnt detected.")]
    public class GetSignal : Conditional {
        [ObjectType(typeof(Sensor))]
        [BDRT.Tooltip("The Sensor to query.")]
        public SharedSensor sensor;
        [BDRT.Tooltip("Retrieves the Signal for this GameObject")]
        public SharedGameObject targetObject;
        [BDRT.Tooltip("Stores the signals 'strength'. Can be interpreted as visibility score between 0-1.")]
        public SharedFloat storeSignalStrength;
        [BDRT.Tooltip("Stores the center-point of the signal's bounding box (world space). Taken from Signal.Bounds.center")]
        public SharedVector3 storeSignalBoundsCenter;
        [BDRT.Tooltip("Stores size of the signal's bounding box. Taken from Signal.Bounds.size")]
        public SharedVector3 storeSignalBoundsSize;

        public override TaskStatus OnUpdate() {
            var actualSensor = (sensor?.Value as Sensor);

            storeSignalStrength.Value = 0f;
            storeSignalBoundsCenter = targetObject.Value?.transform.position;
            storeSignalBoundsCenter.Value = default;

            if (actualSensor == null) {
                return TaskStatus.Failure;
            }

            Signal signal;
            if (!actualSensor.TryGetSignal(targetObject.Value, out signal)) {
                return TaskStatus.Failure;
            }

            storeSignalStrength.Value = signal.Strength;
            storeSignalBoundsCenter.Value = signal.Bounds.center;
            storeSignalBoundsSize.Value = signal.Bounds.size;

            return TaskStatus.Success;
        }

        public override void OnReset() {
            sensor = null;
            targetObject = null;
            storeSignalStrength = null;
            storeSignalBoundsCenter = null;
            storeSignalBoundsSize = null;
        }

    }

}