using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BDRT = BehaviorDesigner.Runtime.Tasks;

namespace Micosmo.SensorToolkit.BehaviorDesigner {

    [TaskCategory("SensorToolkit")]
    [TaskIcon("Assets/Gizmos/SensorToolkit/STEERING.png")]
    [TaskDescription("Sets the destination that a steering sensor should seek towards.")]
    public class SetSeekTarget : Action {

        [ObjectType(typeof(ISteeringSensor))]
        [BDRT.Tooltip("The steering sensor.")]
        public SharedSensor sensor;
        [BDRT.Tooltip("The sensor will seek this objects position.")]
        public SharedGameObject destinationGameObject;
        [BDRT.Tooltip("Only applicable if the 'destinationGameObject' is left empty. The sensor will seek this vector position.")]
        public SharedVector3 destinationPosition;
        [BDRT.Tooltip("The sensor will keep this distance away from the seek destination. When this is >0 it can produce a 'flee' behaviour.")]
        public SharedFloat targetDistance;
        [BDRT.Tooltip("Should the sensor stop when it reaches the destination?")]
        public bool stopAtDestination;

        public override TaskStatus OnUpdate() {
            var actualSensor = (sensor?.Value as ISteeringSensor);

            if (actualSensor == null) {
                return TaskStatus.Failure;
            }

            if (!destinationGameObject.IsNone && destinationGameObject.Value != null) {
                if (stopAtDestination) {
                    actualSensor.ArriveTo(destinationGameObject.Value.transform, targetDistance.Value);
                } else {
                    actualSensor.SeekTo(destinationGameObject.Value.transform, targetDistance.Value);
                }
            } else if (!destinationPosition.IsNone) {
                if (stopAtDestination) {
                    actualSensor.ArriveTo(destinationPosition.Value, targetDistance.Value);
                } else {
                    actualSensor.SeekTo(destinationPosition.Value, targetDistance.Value);
                }
            } else {
                actualSensor.Stop();
            }

            return TaskStatus.Success;
        }

        public override void OnReset() {
            sensor = null;
            destinationGameObject = null;
            destinationPosition = null;
            targetDistance = 0f;
            stopAtDestination = false;
        }

    }

}