using UnityEngine;
using FluffyUnderware.Curvy;

namespace BehaviorDesigner.Runtime.Tasks.Curvy
{
    [TaskCategory("Curvy")]
    [TaskDescription("Delete spline Control Points")]
    [TaskIcon("Assets/Behavior Designer/Integrations/Curvy/Editor/CurvyIcon.png")]
    [RequiredComponent(typeof(CurvySpline))]
    public class DeleteControlPoints : Action
    {
        [Tooltip("The Spline to address")]
        public SharedGameObject targetGameObject;
        [Tooltip("The start Control Point Index to delete")]
        public SharedInt startIndex;
        [Tooltip("The number of Control Points to delete")]
        public SharedInt count;

        public override TaskStatus OnUpdate()
        {
            var go = GetDefaultGameObject(targetGameObject.Value);
            if (go) {
                var spl = go.GetComponent<CurvySpline>();
                if (spl) {
                    if (!startIndex.IsNone && !count.IsNone && count.Value > 0) {
                        for (int i = 0; i < count.Value; i++)
                            spl.Delete(spl.ControlPointsList[startIndex.Value]);

                        spl.Refresh();
                    }

                }
                return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            startIndex = 0;
            count = 1;
        }
    }
}