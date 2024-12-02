using Chronos;
using Pathfinding;
using UnityEngine;
using PrimeTween;

public class TailOrientation : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Number of full rotations per second")]
    private float rotationsPerSecond = 0.5f;

    [SerializeField]
    [Tooltip("Distance from the parent object's center")]
    private float radius = 0.5f;

    [SerializeField]
    private Vector3 circularMotionAxis = Vector3.up;

    [SerializeField]
    private float height = 0f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("Higher values create smoother motion but less responsiveness")]
    private float positionSmoothness = 0.95f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("Higher values create smoother rotation but less responsiveness")]
    private float rotationSmoothness = 0.95f;

    private Timeline parentTimeline;
    private CustomAIPathAlignedToSurface parentAI;
    private Vector3 currentPosition;
    private Quaternion currentRotation;
    private float currentAngle = 0f;

    private Tween positionTween;
    private Tween rotationTween;

    private void Awake()
    {
        Transform parent = transform.parent;
        if (parent != null)
        {
            parentAI = parent.GetComponent<CustomAIPathAlignedToSurface>();
            parentTimeline = parent.GetComponent<Timeline>();
        }
    }

    private void Start()
    {
        if (parentAI == null || parentTimeline == null)
        {
            ConditionalDebug.LogError(
                "TailOrientation: Parent object must have CustomAIPathAlignedToSurface and Timeline components."
            );
            enabled = false;
            return;
        }

        currentPosition = CalculateTargetPosition(0f);
        currentRotation = CalculateTargetRotation();
        transform.position = currentPosition;
        transform.rotation = currentRotation;
    }

    private void Update()
    {
        if (parentAI == null || parentTimeline == null)
            return;

        float deltaTime = parentTimeline.deltaTime;

        Vector3 targetPosition = CalculateTargetPosition(deltaTime);
        Quaternion targetRotation = CalculateTargetRotation();

        // Stop existing tweens
        positionTween.Stop();
        rotationTween.Stop();

        // Create new position tween
        positionTween = Tween.Position(
            transform,
            targetPosition,
            deltaTime * 60f,
            Ease.OutExpo
        );

        // Create new rotation tween
        rotationTween = Tween.Rotation(
            transform,
            targetRotation,
            deltaTime * 60f,
            Ease.OutExpo
        );
    }

    private void OnDestroy()
    {
        positionTween.Stop();
        rotationTween.Stop();
    }

    private Vector3 CalculateTargetPosition(float deltaTime)
    {
        Vector3 parentPosition = parentAI.transform.position;

        float degreesPerSecond = rotationsPerSecond * 360f;
        currentAngle += degreesPerSecond * deltaTime;
        currentAngle %= 360f;

        // Calculate the rotation around the circular motion axis
        Quaternion rotation = Quaternion.AngleAxis(currentAngle, circularMotionAxis);

        // Calculate the offset from the parent position
        Vector3 offset = rotation * (Vector3.right * radius);

        // Project the offset onto the plane perpendicular to the circular motion axis
        Vector3 projectedOffset =
            Vector3.ProjectOnPlane(offset, circularMotionAxis).normalized * radius;

        // Add height offset
        Vector3 heightOffset = circularMotionAxis.normalized * height;

        return parentPosition + projectedOffset + heightOffset;
    }

    private Quaternion CalculateTargetRotation()
    {
        Vector3 parentVelocity = parentAI.velocity;
        if (parentVelocity.sqrMagnitude < 0.001f)
        {
            parentVelocity = parentAI.transform.forward;
        }

        Vector3 parentToTail = currentPosition - parentAI.transform.position;
        Vector3 orientationDirection = Vector3
            .ProjectOnPlane(parentToTail, parentAI.transform.up)
            .normalized;

        float behindFactor = Vector3.Dot(parentVelocity.normalized, -parentToTail.normalized);
        behindFactor = Mathf.Clamp01(behindFactor);

        Vector3 blendedDirection = Vector3.Lerp(
            orientationDirection,
            parentVelocity.normalized,
            behindFactor
        );
        return Quaternion.LookRotation(blendedDirection, parentAI.transform.up);
    }

    private void OnDisable()
    {
        if (parentAI != null)
        {
            transform.position = parentAI.transform.position;
            transform.rotation = parentAI.transform.rotation;
        }
    }

    private void OnDrawGizmos()
    {
        if (parentAI == null)
        {
            parentAI = GetComponentInParent<CustomAIPathAlignedToSurface>();
            if (parentAI == null)
                return;
        }

        Vector3 parentPosition = parentAI.transform.position;
        Vector3 circleCenter = parentPosition + circularMotionAxis.normalized * height;

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(parentPosition, 0.2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(parentPosition, circleCenter);

        Gizmos.color = Color.cyan;
        DrawCircle(circleCenter, circularMotionAxis, radius, 32);

        Gizmos.color = Color.green;
        Vector3 radiusPoint =
            circleCenter
            + Vector3.ProjectOnPlane(Vector3.right, circularMotionAxis).normalized * radius;
        Gizmos.DrawLine(circleCenter, radiusPoint);

#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(radiusPoint, $"Radius: {radius}");
        UnityEditor.Handles.Label(circleCenter, $"Height: {height}");

        Vector3 speedVisualizationPoint =
            circleCenter
            + Vector3.ProjectOnPlane(Vector3.right, circularMotionAxis).normalized
                * (radius + 0.2f);
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.DrawWireArc(
            circleCenter,
            circularMotionAxis,
            Vector3.ProjectOnPlane(Vector3.right, circularMotionAxis),
            360 * rotationsPerSecond,
            radius + 0.2f
        );
        UnityEditor.Handles.Label(speedVisualizationPoint, $"Rotations/s: {rotationsPerSecond}");
#endif

        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(circleCenter, circularMotionAxis.normalized * radius);
    }

    private void DrawCircle(Vector3 center, Vector3 axis, float radius, int segments)
    {
        Vector3 up = axis.normalized;
        Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
        Vector3 right = Vector3.Cross(up, forward).normalized;

        Vector3 lastPoint = center + right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * 360f / segments * Mathf.Deg2Rad;
            Vector3 nextPoint =
                center + Quaternion.AngleAxis(angle * Mathf.Rad2Deg, up) * (right * radius);
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }
    }
}
