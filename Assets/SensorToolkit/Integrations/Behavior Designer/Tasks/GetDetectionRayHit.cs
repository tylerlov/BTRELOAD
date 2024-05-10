using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BDRT = BehaviorDesigner.Runtime.Tasks;

namespace Micosmo.SensorToolkit.BehaviorDesigner {

    [TaskCategory("SensorToolkit")]
    [TaskIcon("Assets/Gizmos/SensorToolkit/RAY.png")]
    [TaskDescription("For a valid raycasting sensor this will retrieve the ray intersection details for a detected object. Works with the Ray Sensor, Arc Sensor and their 2D analogues. Returns failure if the object is not detected.")]
    public class GetDetectionRayHit : Conditional {

        [ObjectType(typeof(IRayCastingSensor))]
        [BDRT.Tooltip("The Sensor to query. Must be a ray casting type sensor.")]
        public SharedSensor sensor;
        [BDRT.Tooltip("The detected object to retrieve the ray intersection details for.")]
        public SharedGameObject targetObject;
        [BDRT.Tooltip("Stores the world-space point of intersection.")]
        public SharedVector3 storePoint;
        [BDRT.Tooltip("Stores the world-space normal at the point of intersection.")]
        public SharedVector3 storeNormal;
        [BDRT.Tooltip("Stores the distance of the ray at the point of intersection.")]
        public SharedFloat storeDistance;
        [BDRT.Tooltip("Stores the fraction of the rays max distance at the point of intersection.")]
        public SharedFloat storeDistanceFraction;

        public override TaskStatus OnUpdate() {
            var actualSensor = (sensor?.Value as IRayCastingSensor);

            storePoint.Value = Vector3.zero;
            storeNormal.Value = Vector3.zero;
            storeDistance.Value = 0f;
            storeDistanceFraction.Value = 0f;

            if (actualSensor == null) {
                return TaskStatus.Failure;
            }

            var hit = actualSensor.GetDetectionRayHit(targetObject.Value);
            if (hit.Equals(RayHit.None)) {
                return TaskStatus.Failure;
            }

            storePoint.Value = hit.Point;
            storeNormal.Value = hit.Normal;
            storeDistance.Value = hit.Distance;
            storeDistanceFraction.Value = hit.DistanceFraction;

            return TaskStatus.Success;
        }

        public override void OnReset() {
            sensor = null;
            targetObject = null;
            storePoint = null;
            storeNormal = null;
            storeDistance = null;
            storeDistanceFraction = null;
        }

    }

}