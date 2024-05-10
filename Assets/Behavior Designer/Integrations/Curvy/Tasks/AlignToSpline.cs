using FluffyUnderware.Curvy;

namespace BehaviorDesigner.Runtime.Tasks.Curvy
{
    [TaskCategory("Curvy")]
    [TaskDescription("Align a GameObject to a Curvy Spline ")]
    [TaskIcon("Assets/Behavior Designer/Integrations/Curvy/Editor/CurvyIcon.png")]
    [RequiredComponent(typeof(CurvySpline))]
    public class AlignToSpline : Action
    {
        [Tooltip("The GameObject to align")]
        public SharedGameObject targetGameObject;
        [Tooltip("The Splineor SplineGroup to address")]
        public SharedGameObject targetSpline;
        [RequiredField, Tooltip("TF value to interpolate")]
        public SharedFloat TF;
        [Tooltip("Use a linear approximation (slightly faster) for position?")]
        public SharedBool approximatePosition;
        [Tooltip("Set Orientation?")]
        public bool setOrientation;

        private CurvySpline mSpline;

        public override void OnAwake()
        {
            mSpline = targetSpline.Value.GetComponent<CurvySpline>();
        }

        public override TaskStatus OnUpdate()
        {
            if (!mSpline || !mSpline.IsInitialized)
                return TaskStatus.Failure;

            var go = GetDefaultGameObject(targetGameObject.Value);
            if (go) {
                go.transform.position = approximatePosition.Value ? mSpline.InterpolateFast(TF.Value) : mSpline.Interpolate(TF.Value);

                if (setOrientation) {
                    go.transform.rotation = mSpline.GetOrientationFast(TF.Value);
                }
            }
            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            targetSpline = null;
            setOrientation = true;
            approximatePosition = false;
            TF = 0;
        }
    }
}