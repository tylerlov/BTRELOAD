using UnityEngine;
using FluffyUnderware.Curvy;

namespace BehaviorDesigner.Runtime.Tasks.Curvy
{
    [TaskCategory("Curvy")]
    [TaskDescription("Get data from a spline point nearest to a given point")]
    [TaskIcon("Assets/Behavior Designer/Integrations/Curvy/Editor/CurvyIcon.png")]
    [RequiredComponent(typeof(CurvySpline))]
    public class GetNearestPoint : Action
    {
        [Tooltip("The Spline or SplineGroup to address")]
        public SharedGameObject targetGameObject;
        [Tooltip("The known point in space")]
        public SharedVector3 sourcePoint;
        [Tooltip("store TF")]
        public SharedFloat storeTF;
        [Tooltip("store the interpolated position")]
        public SharedVector3 storePosition;
        [Tooltip("store the interpolated tangent")]
        public SharedVector3 storeTangent;
        [Tooltip("store the interpolated Up-Vector")]
        public SharedVector3 storeUpVector;
        [Tooltip("store the interpolated Rotation")]
        public SharedQuaternion storeRotation;
        [Tooltip("Repeat every frame.")]
        public bool everyFrame;
        [Tooltip("Perform in LateUpdate.")]
        public bool lateUpdate;

        private CurvySpline mSpline;

        public override void OnStart()
        {
            var go = GetDefaultGameObject(targetGameObject.Value);
            if (go) {
                mSpline = go.GetComponent<CurvySpline>();
            }
        }

        public override TaskStatus OnUpdate()
        {
            if (!mSpline || !mSpline.IsInitialized) {
                return TaskStatus.Failure;
            }

            if (!storeTF.IsShared && !storePosition.IsShared && !storeUpVector.IsShared && !storeRotation.IsShared) {
                return TaskStatus.Failure;
            }

            var _tf = mSpline.GetNearestPointTF(sourcePoint.Value);

            if (storeTF.IsShared)
                storeTF.Value = _tf;

            if (storePosition.IsShared)
                storePosition.Value = mSpline.Interpolate(_tf);

            if (storeTangent.IsShared)
                storeTangent.Value = mSpline.GetTangent(_tf);

            if (storeUpVector.IsShared) {
                storeUpVector.Value = mSpline.GetOrientationUpFast(_tf);
            }
            if (storeRotation.IsShared) {
                storeRotation.Value = storeUpVector.IsNone ? mSpline.GetOrientationFast(_tf) : Quaternion.LookRotation(mSpline.GetTangent(_tf), storeUpVector.Value);
            }

            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            sourcePoint = Vector3.zero;
            storeTF = 0;
            storePosition = Vector3.zero;
            storeTangent = Vector3.zero;
            storeUpVector = Vector3.zero;
            storeRotation = Quaternion.identity;
            everyFrame = false;
            lateUpdate = false;
        }
    }
}