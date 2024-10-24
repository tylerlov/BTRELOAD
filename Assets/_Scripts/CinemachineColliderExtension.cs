using UnityEngine;
using Unity.Cinemachine;

[ExecuteAlways]
[SaveDuringPlay]
[AddComponentMenu("")]
public class CinemachineColliderExtension : CinemachineExtension
{
    [Tooltip("Additional distance to maintain from obstacles")]
    public float additionalClearance = 0.5f;

    [Tooltip("Layers to ignore for collision")]
    public LayerMask ignoreLayerMask;

    [Tooltip("Tag to ignore for collision")]
    public string ignoreTag = "";

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Body)
        {
            Vector3 displacement = ResolveCameraCollisions(state);
            state.RawPosition += displacement;
        }
    }

    private Vector3 ResolveCameraCollisions(CameraState state)
    {
        Vector3 cameraPosition = state.RawPosition;
        Vector3 lookAtPosition = state.ReferenceLookAt;
        Vector3 cameraForward = state.RawOrientation * Vector3.forward;

        Vector3 displacement = Vector3.zero;

        // Check for collisions
        if (Physics.SphereCast(
            lookAtPosition,
             0.2f, // Adjust this radius as needed
            cameraForward,
            out RaycastHit hit,
            Vector3.Distance(lookAtPosition, cameraPosition),
            ~ignoreLayerMask,
            QueryTriggerInteraction.Ignore))
        {
            if (string.IsNullOrEmpty(ignoreTag) || !hit.collider.CompareTag(ignoreTag))
            {
                Vector3 idealPosition = hit.point + hit.normal * (0.2f + additionalClearance);
                displacement = idealPosition - cameraPosition;
            }
        }

        return displacement;
    }
}
