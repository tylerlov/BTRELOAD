using FluffyUnderware.Curvy;

namespace BehaviorDesigner.Runtime.Tasks.Curvy
{
    [TaskCategory("Curvy")]
    [TaskDescription("Creates a Curvy spline")]
    [TaskIcon("Assets/Behavior Designer/Integrations/Curvy/Editor/CurvyIcon.png")]
    public class CreateSpline : Action
    {
        [Tooltip("Close the spline?")]
        public SharedBool closeSpline;
        [Tooltip("Granularity of internal approximation")]
        public SharedInt cacheDensity;
        [Tooltip("Automatic end tangents?")]
        public SharedBool autoEndTangents;
        [Tooltip("How the Up-Vector should be calculated")]
        public CurvyOrientation orientation;
        [Tooltip("Optionally store the created spline object")]
        public SharedGameObject storeObject;

        public override TaskStatus OnUpdate()
        {
            var spl = CurvySpline.Create();
            spl.Closed = closeSpline.Value;
            spl.CacheDensity = cacheDensity.Value;
            spl.AutoEndTangents = autoEndTangents.Value;
            spl.Orientation = orientation;
            storeObject.Value = spl.gameObject;

            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            closeSpline = true;
            autoEndTangents = true;
            cacheDensity = 25;
            orientation = CurvyOrientation.Dynamic;
            storeObject = null;
        }
    }
}