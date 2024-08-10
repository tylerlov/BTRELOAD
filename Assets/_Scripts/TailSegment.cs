using UnityEngine;

public class TailSegment : MonoBehaviour
{
    public float followSpeed = 5f;
    public float springForce = 10f;
    public float damping = 5f;

    private Vector3 initialLocalPosition;
    private Vector3 velocity;

    private void Start()
    {
        initialLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        // Calculate the target position (initial position relative to parent)
        Vector3 targetPosition = transform.parent.TransformPoint(initialLocalPosition);

        // Calculate the force towards the target position
        Vector3 force = (targetPosition - transform.position) * springForce;

        // Apply damping to the velocity
        velocity = Vector3.Lerp(velocity, Vector3.zero, damping * Time.deltaTime);

        // Add the force to the velocity
        velocity += force * Time.deltaTime;

        // Move the segment
        transform.position += velocity * Time.deltaTime;

        // Optional: Make the segment look in the direction of movement
        if (velocity.magnitude > 0.01f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(velocity), followSpeed * Time.deltaTime);
        }
    }
}