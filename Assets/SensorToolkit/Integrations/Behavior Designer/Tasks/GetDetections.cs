using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BDRT = BehaviorDesigner.Runtime.Tasks;

namespace Micosmo.SensorToolkit.BehaviorDesigner {

    [TaskCategory("SensorToolkit")]
    [TaskIcon("Assets/Gizmos/SensorToolkit/SENSORTOOLKIT.png")]
    [TaskDescription("Queries the sensor for the GameObjects it detects. Returns success if at least one object is detected. Returns failure otherwise.")]
    public class GetDetections : Conditional {

        [ObjectType(typeof(Sensor))]
        [BDRT.Tooltip("The Sensor to query.")]
        public SharedSensor sensor;
        [BDRT.Tooltip("If this is non-empty then detected objects must have this tag.")]
        public SharedString requiredTag;
        [BDRT.Tooltip("When true the sensors detections are ordered by distance to a world point. If false they are ordered by distance to the sensor.")]
        public bool orderByDistanceToPoint;
        [BDRT.Tooltip("The point that detections are ordered by distance to. Only applicable when 'orderByDistanceToPoint' is marked.")]
        public SharedVector3 targetPoint;
        [BDRT.Tooltip("Stores the nearest detected GameObject.")]
        public SharedGameObject storeNearest;
        [BDRT.Tooltip("Stores all the detected GameObjects.")]
        public SharedGameObjectList storeAll;

        bool hasTagFilter => requiredTag?.Value?.Length > 0;

        public override void OnAwake() {
            storeAll.Value = new List<GameObject>();
        }

        public override TaskStatus OnUpdate() {
            var actualSensor = (sensor?.Value as Sensor);

            if (actualSensor == null) {
                return TaskStatus.Failure;
            }

            storeAll.Value = orderByDistanceToPoint 
                ? hasTagFilter
                    ? actualSensor.GetDetectionsByDistanceToPoint(targetPoint.Value, requiredTag.Value, storeAll.Value)
                    : actualSensor.GetDetectionsByDistanceToPoint(targetPoint.Value, storeAll.Value)
                : hasTagFilter
                    ? actualSensor.GetDetectionsByDistance(requiredTag.Value, storeAll.Value)
                    : actualSensor.GetDetectionsByDistance(storeAll.Value);

            storeNearest.Value = storeAll.Value.Count > 0 ? storeAll.Value[0] : default;

            return storeNearest.Value == null ? TaskStatus.Failure : TaskStatus.Success;
        }

        public override void OnReset() {
            sensor = null;
            requiredTag = null;
            orderByDistanceToPoint = false;
            targetPoint = null;
            storeNearest = null;
            storeAll = null;
        }

    }

}