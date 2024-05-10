using UnityEngine;
using FluffyUnderware.Curvy;

namespace BehaviorDesigner.Runtime.Tasks.Curvy
{
    [TaskCategory("Curvy")]
    [TaskDescription("Retrieve or convert several spline values")]
    [TaskIcon("Assets/Behavior Designer/Integrations/Curvy/Editor/CurvyIcon.png")]
    [RequiredComponent(typeof(CurvySpline))]
    public class GetValue : Action
    {
        [Tooltip("The Spline or SplineGroup to address")]
        public SharedGameObject targetGameObject;
        [Tooltip("Input value (TF or Distance)")]
        public SharedFloat input;
        [Tooltip("Whether Input value is in World Units (Distance) or TF")]
        public SharedBool useWorldUnits;
        [Tooltip("Use a linear approximation (slightly faster) for position?")]
        public SharedBool approximatePosition;
        [Tooltip("store the interpolated position")]
        public SharedVector3 storePosition;
        [ Tooltip("store the interpolated tangent")]
        public SharedVector3 storeTangent;
        [Tooltip("store the interpolated Up-Vector")]
        public SharedVector3 storeUpVector;
        [Tooltip("store the interpolated Rotation")]
        public SharedQuaternion storeRotation;
        [Tooltip("store the interpolated User Value")]
        public SharedVector3 storeUserValue;
        [Tooltip("Index of the UserValue you're interested in")]
        public SharedInt userValueIndex;
        [Tooltip("store the interpolated Scale")]
        public SharedVector3 storeScale;
        [Tooltip("store the TF")]
        public SharedFloat storeTF;
        [Tooltip("store the Distance")]
        public SharedFloat storeDistance;
        [Tooltip("store the Segment")]
        public SharedGameObject storeSegment;
        [Tooltip("store the local F")]
        public SharedFloat storeSegmentF;
        [Tooltip("store the local Distance")]
        public SharedFloat storeSegmentDistance;
        // === GENERAL ===
        [Tooltip("store the total spline length")]
        public SharedFloat storeLength;
        
        private CurvySpline mSpline;

        public override void OnStart()
        {
            GameObject go = GetDefaultGameObject(targetGameObject.Value);
            if (go) {
                mSpline = go.GetComponent<CurvySpline>();
            }
        }

        public override TaskStatus OnUpdate()
        {
            if (!mSpline || !mSpline.IsInitialized)
                return TaskStatus.Failure;

            bool calc = (!input.IsNone &&
                                   (storePosition.IsShared ||
                                    storeTangent.IsShared ||
                                    storeUpVector.IsShared ||
                                    storeRotation.IsShared ||
                                    storeUserValue.IsShared ||
                                    storeTF.IsShared ||
                                    storeDistance.IsShared ||
                                    storeSegment.IsShared ||
                                    storeSegmentF.IsShared ||
                                    storeSegmentDistance.IsShared));
            if (calc) {
                float f = (useWorldUnits.Value) ? mSpline.DistanceToTF(input.Value) : input.Value;

                if (storePosition.IsShared)
                    storePosition.Value = (approximatePosition.Value) ? mSpline.InterpolateFast(f) : mSpline.Interpolate(f);

                if (storeTangent.IsShared)
                    storeTangent.Value = mSpline.GetTangent(f);

                if (storeUpVector.IsShared)
                    storeUpVector.Value = mSpline.GetOrientationUpFast(f);

                if (storeRotation.IsShared)
                    storeRotation.Value = storeUpVector.IsNone ? mSpline.GetOrientationFast(f) : Quaternion.LookRotation(mSpline.GetTangent(f), storeUpVector.Value);

                if (storeScale.IsShared) {
                    float localF;
                    CurvySplineSegment segment = mSpline.TFToSegment(f, out localF);
                    CurvySplineSegment nextControlPoint = segment.Spline.GetNextControlPoint(segment);
                    if (ReferenceEquals(segment, null) == false)
                        storeScale.Value = nextControlPoint
                            ? Vector3.Lerp(segment.transform.lossyScale, nextControlPoint.transform.lossyScale, localF)
                            : segment.transform.lossyScale;
                    else
                        storeScale.Value = Vector3.zero;
                }

                if (storeTF.IsShared)
                    storeTF.Value = f;

                if (storeDistance.IsShared)
                    storeDistance.Value = (useWorldUnits.Value) ? input.Value : mSpline.TFToDistance(f);

                CurvySplineSegment seg = null;
                float segF = 0;
                if (storeSegment.IsShared) {
                    seg = GetSegment(f, out segF);
                    storeSegment.Value = seg.gameObject;
                }

                if (storeSegmentF.IsShared) {
                    if (!seg)
                        seg = GetSegment(f, out segF);
                    storeSegmentF.Value = segF;
                }

                if (storeSegmentDistance.IsShared) {
                    if (!seg)
                        seg = GetSegment(f, out segF);
                    storeSegmentDistance.Value = seg.LocalFToDistance(segF);
                }
            }
            // General
            if (storeLength.IsShared)
                storeLength.Value = mSpline.Length;

            return TaskStatus.Success;
        }
        
        public override void OnReset()
        {
            targetGameObject = null;
            approximatePosition = false;
            useWorldUnits = false;
            storePosition = Vector3.zero;
            storeUpVector = Vector3.zero;
            storeRotation = Quaternion.identity;
            storeUserValue = Vector3.zero;
            userValueIndex = 0;
            storeTangent = Vector3.zero;
            storeDistance = 0;
            storeTF = 0;
            storeSegment = null;
            storeSegmentDistance = 0;
            storeSegmentF = 0;
            storeLength = 0;
            input = 0;
        }

        private CurvySplineSegment GetSegment(float tf, out float localF)
        {
            return ((CurvySpline)mSpline).TFToSegment(tf, out localF);
        }
    }
}