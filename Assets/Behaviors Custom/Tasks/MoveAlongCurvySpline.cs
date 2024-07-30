using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using FluffyUnderware.Curvy;

[TaskCategory("Movement")]
[TaskDescription("Moves the GameObject along a Curvy Spline.")]
public class MoveAlongCurvySpline : Action
{
    [BehaviorDesigner.Runtime.Tasks.Tooltip("The Curvy Spline to move along")]
    public SharedGameObject splineObject;

    [BehaviorDesigner.Runtime.Tasks.Tooltip("The speed at which to move along the spline")]
    public SharedFloat moveSpeed = 10f;

    [BehaviorDesigner.Runtime.Tasks.Tooltip("Direction of movement (1 for forward, -1 for backward)")]
    public SharedInt direction = 1;

    private CurvySpline mSpline;
    private float currentTF = 0f;
    private Transform agentTransform;

    public override void OnAwake()
    {
        agentTransform = transform;
    }

    public override void OnStart()
    {
        if (splineObject.Value == null)
        {
            Debug.LogError("MoveAlongCurvySpline: Spline object is not assigned.");
            return;
        }

        mSpline = splineObject.Value.GetComponent<CurvySpline>();
        if (mSpline == null || !mSpline.IsInitialized)
        {
            Debug.LogError("MoveAlongCurvySpline: Invalid or uninitialized Curvy Spline.");
            return;
        }

        // Place the agent at the start of the spline
        currentTF = 0f;
        UpdateAgentPosition();
    }

    public override TaskStatus OnUpdate()
    {
        if (mSpline == null || !mSpline.IsInitialized)
            return TaskStatus.Failure;

        // Move along the spline
        currentTF += direction.Value * moveSpeed.Value * Time.deltaTime / mSpline.Length;

        // If we've reached the end of the spline, loop back to the start
        if (currentTF >= 1f)
            currentTF -= 1f;
        else if (currentTF < 0f)
            currentTF += 1f;

        UpdateAgentPosition();

        return TaskStatus.Running;
    }

    private void UpdateAgentPosition()
    {
        // Use Interpolate with Space.World to get the correct world position
        Vector3 worldPosition = mSpline.Interpolate(currentTF, Space.World);
        agentTransform.position = worldPosition;

        // Get the tangent in world space
        Vector3 tangent = mSpline.GetTangent(currentTF, Space.World);
        
        // Get the up vector in world space
        Vector3 up = mSpline.GetOrientationUpFast(currentTF, Space.World);

        // Create a rotation that looks in the direction of the tangent and uses the up vector
        agentTransform.rotation = Quaternion.LookRotation(tangent, up);

        Debug.DrawLine(worldPosition, worldPosition + tangent, Color.blue);
        Debug.DrawLine(worldPosition, worldPosition + up, Color.green);
    }
}