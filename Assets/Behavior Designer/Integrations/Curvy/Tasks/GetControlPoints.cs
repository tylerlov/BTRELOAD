using UnityEngine;
using FluffyUnderware.Curvy;

namespace BehaviorDesigner.Runtime.Tasks.Curvy
{
    [TaskCategory("Curvy")]
    [TaskDescription("Gets a Control Point or Segment GameObject")]
    [TaskIcon("Assets/Behavior Designer/Integrations/Curvy/Editor/CurvyIcon.png")]
    [RequiredComponent(typeof(CurvySpline))]
    public class GetControlPoint : Action
    {
        [Tooltip("The Spline to address")]
        public SharedGameObject targetGameObject;
        [Tooltip("Index of Control Point or Segment")]
        public SharedInt index;
        [Tooltip("Whether to retrieve Segments or Control Points")]
        public SharedBool getSegment;
        [Tooltip("Store the Control Point")]
        public SharedGameObject storeObject;

        public override TaskStatus OnUpdate()
        {
            var go = GetDefaultGameObject(targetGameObject.Value);
            if (go) {
                var spl = go.GetComponent<CurvySpline>();
                if (spl) {
                    if (getSegment.Value) {
                        if (spl.Count > 0) {
                            storeObject.Value = spl[Mathf.Clamp(index.Value, 0, spl.Count - 1)].gameObject;
                        }
                    } else if (spl.ControlPointCount > 0) {
                        storeObject.Value = spl.ControlPointsList[Mathf.Clamp(index.Value, 0, spl.ControlPointCount - 1)].gameObject;
                    }
                }
                return TaskStatus.Success;
            }
            return TaskStatus.Failure;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            storeObject = null;
            getSegment = false;
            index = 0;
        }
    }
}