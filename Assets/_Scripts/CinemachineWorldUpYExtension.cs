using UnityEngine;
using Cinemachine;

[SaveDuringPlay]
[AddComponentMenu("Cinemachine/WorldUpY Extension")]
public class CinemachineWorldUpYExtension : CinemachineExtension
{
    [Tooltip("Distance from screen edge to maintain target (in viewport space, 0 is edge, 0.5 is center)")]
    [Range(0f, 0.5f)]
    public float borderBuffer = 0.05f;

    [Tooltip("Angle threshold for snapping to 90-degree rotations")]
    public float snapThreshold = 10f;

    [Tooltip("Maximum allowed rotation change per frame (in degrees)")]
    public float maxRotationDelta = 10f;

    [Tooltip("Camera tilt angle in degrees (positive tilts upward)")]
    [Range(-30f, 30f)]
    public float verticalTiltAngle = 15f;

    [Tooltip("Camera tilt angle in degrees (positive tilts rightward)")]
    [Range(-30f, 30f)]
    public float horizontalTiltAngle = 0f;

    private Quaternion lastRotation;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Finalize)
        {
            Vector3 lookAtPoint = state.ReferenceLookAt;
            Vector3 cameraPosition = state.FinalPosition;
            Vector3 directionToTarget = (lookAtPoint - cameraPosition).normalized;

            // Project the direction onto the camera's view plane
            Vector3 cameraForward = state.FinalOrientation * Vector3.forward;
            Vector3 cameraRight = state.FinalOrientation * Vector3.right;
            Vector3 cameraUp = state.FinalOrientation * Vector3.up;

            float rightDot = Vector3.Dot(directionToTarget, cameraRight);
            float upDot = Vector3.Dot(directionToTarget, cameraUp);

            // Clamp the target position to the screen border
            float clampedRightDot = Mathf.Clamp(rightDot, -0.5f + borderBuffer, 0.5f - borderBuffer);
            float clampedUpDot = Mathf.Clamp(upDot, -0.5f + borderBuffer, 0.5f - borderBuffer);

            // Calculate the adjusted forward direction
            Vector3 adjustedForward = cameraForward + clampedRightDot * cameraRight + clampedUpDot * cameraUp;
            adjustedForward.Normalize();

            // Apply camera tilt
            Quaternion tilt = Quaternion.Euler(-verticalTiltAngle, horizontalTiltAngle, 0);
            adjustedForward = tilt * adjustedForward;

            // Determine the closest world axis for up vector
            Vector3 up = GetClosestWorldAxis(state.ReferenceUp);

            // Check if we're close enough to snap
            if (Vector3.Angle(state.ReferenceUp, up) >= snapThreshold)
            {
                up = Vector3.up;
            }

            // Create new rotation
            Quaternion targetRotation = Quaternion.LookRotation(adjustedForward, up);

            // Limit rotation change to prevent sudden flips
            Quaternion limitedRotation = Quaternion.RotateTowards(lastRotation, targetRotation, maxRotationDelta);

            // Apply the new rotation
            state.OrientationCorrection = Quaternion.Inverse(state.FinalOrientation) * limitedRotation;

            // Store the new rotation for the next frame
            lastRotation = limitedRotation;
        }
    }

    private Vector3 GetClosestWorldAxis(Vector3 direction)
    {
        Vector3[] worldAxes = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        Vector3 closestAxis = Vector3.up;
        float closestAngle = float.MaxValue;

        foreach (Vector3 axis in worldAxes)
        {
            float angle = Vector3.Angle(direction, axis);
            if (angle < closestAngle)
            {
                closestAngle = angle;
                closestAxis = axis;
            }
        }

        return closestAxis;
    }
}
