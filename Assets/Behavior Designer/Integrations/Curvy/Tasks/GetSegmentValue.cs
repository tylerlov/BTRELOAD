using UnityEngine;
using FluffyUnderware.Curvy;

namespace BehaviorDesigner.Runtime.Tasks.Curvy
{
    [TaskCategory("Curvy")]
    [TaskDescription("Retrieve or convert several segment values")]
    [TaskIcon("Assets/Behavior Designer/Integrations/Curvy/Editor/CurvyIcon.png")]
    [RequiredComponent(typeof(CurvySplineSegment))]
    public class GetSegmentValue : Action
    {
        [Tooltip("The Spline Segment to address")]
        public SharedGameObject targetGameObject;
        [Tooltip("Input value (F or Distance)")]
        public SharedFloat input;
        [Tooltip("Whether Input value is in World Units (Distance) or F")]
        public SharedBool useWorldUnits;
        [Tooltip("Use a linear approximation (slightly faster) for position?")]
        public SharedBool ApproximatePosition;
        [Tooltip("Store the interpolated position")]
        public SharedVector3 storePosition;
        [Tooltip("Store the interpolated tangent")]
        public SharedVector3 storeTangent;
        [Tooltip("Store the interpolated Up-Vector")]
        public SharedVector3 storeUpVector;
        [Tooltip("Store the interpolated Rotation")]
        public SharedQuaternion storeRotation;
        [Tooltip("Store the interpolated User Value")]
        public SharedVector3 storeUserValue;
        [Tooltip("Store the interpolated Scale")]
        public SharedVector3 storeScale;
        [Tooltip("Store the TF")]
        public SharedFloat storeTF;
        [Tooltip("Store the Distance")]
        public SharedFloat storeDistance;
        [Tooltip("Store the local F")]
        public SharedFloat storeSegmentF;
        [Tooltip("Store the local Distance")]
        public SharedFloat storeSegmentDistance;
        [Tooltip("Store the Segment length")]
        public SharedFloat storeLength;
        [Tooltip("Store the SegmentIndex")]
        public SharedInt storeSegmentIndex;
        [Tooltip("Store the ControlPointIndex")]
        public SharedInt storeControlPointIndex;

        private CurvySplineSegment mSegment;

        public override void OnStart()
        {
            var go = GetDefaultGameObject(targetGameObject.Value);
            if (go) {
                mSegment = go.GetComponent<CurvySplineSegment>();
            }
        }

        public override TaskStatus OnUpdate()
        {
            if (!mSegment || !mSegment.Spline.IsInitialized)
                return TaskStatus.Failure;

            var calc = (!input.IsNone &&
                       (storePosition.IsShared ||
                        storeTangent.IsShared ||
                        storeUpVector.IsShared ||
                        storeRotation.IsShared ||
                        storeUserValue.IsShared ||
                        storeTF.IsShared ||
                        storeDistance.IsShared ||
                        storeSegmentF.IsShared ||
                        storeSegmentDistance.IsShared));
            if (calc) {
                var inputF = useWorldUnits.Value ? mSegment.DistanceToLocalF(input.Value) : input.Value;

                if (storePosition.IsShared)
                    storePosition.Value = (ApproximatePosition.Value) ? mSegment.InterpolateFast(inputF) : mSegment.Interpolate(inputF);

                if (storeTangent.IsShared)
                    storeTangent.Value = mSegment.GetTangent(inputF);

                if (storeUpVector.IsShared)
                    storeUpVector.Value = mSegment.GetOrientationUpFast(inputF);

                if (storeRotation.IsShared)
                    storeRotation.Value = !storeUpVector.IsNone ? mSegment.GetOrientationFast(inputF) : Quaternion.LookRotation(mSegment.GetTangent(inputF), storeUpVector.Value);

                if (storeScale.IsShared) {
                    CurvySplineSegment nextControlPoint = mSegment.Spline.GetNextControlPoint(mSegment);
                    storeScale.Value = nextControlPoint
                        ? Vector3.Lerp(mSegment.transform.lossyScale, nextControlPoint.transform.lossyScale, inputF)
                        : mSegment.transform.lossyScale;
                }

                if (storeTF.IsShared)
                    storeTF.Value = mSegment.LocalFToTF(inputF);

                if (storeSegmentDistance.IsShared)
                    storeSegmentDistance.Value = mSegment.LocalFToDistance(inputF);

                if (storeDistance.IsShared)
                    storeDistance.Value = (storeSegmentDistance.IsShared) ? storeSegmentDistance.Value + mSegment.Distance : mSegment.LocalFToDistance(inputF) + mSegment.Distance;

                if (storeSegmentF.IsShared)
                    storeSegmentF.Value = inputF;

            }
            // General
            if (storeLength.IsShared)
                storeLength.Value = mSegment.Length;

            if (storeSegmentIndex.IsShared)
                storeSegmentIndex.Value = mSegment.Spline.GetSegmentIndex(mSegment);
            if (storeControlPointIndex.IsShared)
                storeControlPointIndex.Value = mSegment.Spline.GetControlPointIndex(mSegment);

            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            ApproximatePosition = false;
            useWorldUnits = false;
            storePosition = Vector3.zero;
            storeUpVector = Vector3.zero;
            storeRotation = Quaternion.identity;
            storeUserValue = Vector3.zero;
            storeTangent = Vector3.zero;
            storeDistance = 0;
            storeTF = 0;
            storeSegmentIndex = 0;
            storeControlPointIndex = 0;
            storeSegmentDistance = 0;
            storeSegmentF = 0;
            input = 0;
        }
    }
}