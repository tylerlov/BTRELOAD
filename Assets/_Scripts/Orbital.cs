using Chronos;
using UnityEngine;

[RequireComponent(typeof(Timeline))]
public class Orbital : MonoBehaviour
{
    public float orbitSpeed = 50f;
    public Vector3 rotationAxis = Vector3.up;
    public float orbitRadius = 1f;
    public float orbitHeight = 0f;

    private Vector3 originalLocalPosition;
    private float currentAngle = 0f;
    private Timeline timeline;

    private void Start()
    {
        originalLocalPosition = transform.localPosition;
        timeline = GetComponent<Timeline>();
        SetOrbitPosition(currentAngle);
    }

    private void Update()
    {
        // Use Chronos time
        float deltaTime = timeline.deltaTime;
        currentAngle += orbitSpeed * deltaTime * Mathf.Deg2Rad;
        SetOrbitPosition(currentAngle);
    }

    private void SetOrbitPosition(float angle)
    {
        if (transform.parent == null)
            return;

        // Calculate the new position in parent's local space
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * orbitRadius,
            orbitHeight,
            Mathf.Sin(angle) * orbitRadius
        );

        // Rotate the offset around the parent's local rotation axis
        offset =
            Quaternion.AngleAxis(
                Vector3.SignedAngle(Vector3.forward, rotationAxis, Vector3.up),
                rotationAxis
            ) * offset;

        // Set the new local position
        transform.localPosition = originalLocalPosition + offset;

        // Optionally, make the object face the direction of movement
        if (orbitSpeed != 0)
        {
            transform.localRotation = Quaternion.LookRotation(
                offset.normalized,
                transform.parent.TransformDirection(rotationAxis)
            );
        }
    }

    private void OnDrawGizmos()
    {
        if (transform.parent == null)
            return;

        // Calculate the world space center and axis
        Vector3 worldCenter = transform.parent.TransformPoint(originalLocalPosition);
        Vector3 worldAxis = transform.parent.TransformDirection(rotationAxis);

        // Draw the orbit path
        Gizmos.color = Color.yellow;
        DrawCircle(worldCenter, worldAxis, orbitRadius, 32);

        // Draw the rotation axis
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(worldCenter, worldCenter + worldAxis.normalized * orbitRadius);

        // Draw the radius
        Gizmos.color = Color.red;
        Vector3 radiusEnd =
            worldCenter
            + Quaternion.AngleAxis(
                Vector3.SignedAngle(Vector3.forward, worldAxis, Vector3.up),
                Vector3.up
            )
                * Vector3.forward
                * orbitRadius;
        Gizmos.DrawLine(worldCenter, radiusEnd);

        // Draw the orbit height
        Gizmos.color = Color.green;
        Vector3 heightVector = Vector3.Project(worldAxis, Vector3.up).normalized * orbitHeight;
        Gizmos.DrawLine(worldCenter, worldCenter + heightVector);

        // Draw the elevated orbit path
        Gizmos.color = Color.cyan;
        DrawCircle(worldCenter + heightVector, worldAxis, orbitRadius, 32);

        // Draw the object's position on the orbit
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(transform.position, 0.1f);
    }

    private void DrawCircle(Vector3 center, Vector3 normal, float radius, int segments)
    {
        Vector3 from = Vector3.ProjectOnPlane(Vector3.forward, normal).normalized * radius;
        Quaternion rotation = Quaternion.AngleAxis(360f / segments, normal);

        for (int i = 0; i <= segments; i++)
        {
            Vector3 to = rotation * from;
            Gizmos.DrawLine(center + from, center + to);
            from = to;
        }
    }
}
