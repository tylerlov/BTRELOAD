using UnityEngine;
using FluffyUnderware.Curvy;

namespace BehaviorDesigner.Runtime.Tasks.Curvy
{
    [TaskCategory("Curvy")]
    [TaskDescription("Returns success when the Curvy Spline is fully loaded")]
    [TaskIcon("Assets/Behavior Designer/Integrations/Curvy/Editor/CurvyIcon.png")]
    [RequiredComponent(typeof(CurvySpline))]
    public class IsInitialized : Conditional
    {
        [Tooltip("The Spline to address")]
        public SharedGameObject targetGameObject;

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
            if (mSpline && mSpline.IsInitialized) {
                return TaskStatus.Success;
            }
            return TaskStatus.Failure;
        }

        public override void OnReset()
        {
            GameObject = null;
        }
    }
}