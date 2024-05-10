using UnityEngine;
using FluffyUnderware.Curvy;

namespace BehaviorDesigner.Runtime.Tasks.Curvy
{
    [TaskCategory("Curvy")]
    [TaskDescription("Insert spline Control Points")]
    [TaskIcon("/Assets/Behavior Designer/Integrations/Editor/CurvyIcon.png")]
    [RequiredComponent(typeof(CurvySpline))]
    public class InsertControlPoints : Action
    {
        public enum InsertMode { Before, After };

        [Tooltip("The Spline to address")]
        public SharedGameObject targetGameObject;
        [Tooltip("Control Points position")]
        public SharedVector3[] controlPoints;
        [RequiredField, Tooltip("The Control Point Index to add before/after")]
        public SharedInt index;
        [Tooltip("Specifies how to insert the points")]
        public InsertMode mode = InsertMode.After;
        [Tooltip("Specifies the game space")]
        public Space space;

        public override TaskStatus OnUpdate()
        {
            var go = GetDefaultGameObject(targetGameObject.Value);
            if (go) {
                CurvySpline spl = go.GetComponent<CurvySpline>();
                if (spl) {
                    CurvySplineSegment seg = (index.Value >= 0 && index.Value < spl.ControlPointCount) ? spl.ControlPointsList[index.Value] : null;
                    for (int i = 0; i < controlPoints.Length; i++) {
                        CurvySplineSegment newCP;
                        if (mode == InsertMode.After)
                            newCP = spl.InsertAfter(seg);
                        else
                            newCP = spl.InsertBefore(seg);
                        if (space == Space.Self)
                            newCP.SetLocalPosition(controlPoints[i].Value);
                        else
                            newCP.SetPosition(controlPoints[i].Value);
                    }
                    spl.Refresh();
                    return TaskStatus.Success;
                }
            }

            return TaskStatus.Failure;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            controlPoints = null;
            index = 0;
            mode = InsertMode.After;
            space = Space.World;
        }
    }
}