using UnityEngine;
using PrimeTween;

public class TailSegment : MonoBehaviour
{
    public float followSpeed = 5f;
    public float springForce = 10f;
    public float damping = 5f;

    private Vector3 initialLocalPosition;
    private Vector3 velocity;
    private Tween positionTween;
    private Tween rotationTween;

    private void Start()
    {
        initialLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        // Calculate the target position (initial position relative to parent)
        Vector3 targetPosition = transform.parent.TransformPoint(initialLocalPosition);

        // Stop any existing tweens
        positionTween.Stop();
        
        // Create new position tween
        positionTween = Tween.Position(
            transform,
            targetPosition,
            Time.deltaTime * 5f, // Increased duration for smoother movement
            Ease.OutExpo
        );

        // Apply damping to the velocity
        velocity = Vector3.Lerp(velocity, Vector3.zero, damping * Time.deltaTime);

        // Calculate movement direction
        Vector3 movement = (targetPosition - transform.position);
        if (movement.magnitude > 0.01f)
        {
            velocity = movement * springForce * Time.deltaTime;

            // Stop any existing rotation tween
            rotationTween.Stop();
            
            // Create new rotation tween
            rotationTween = Tween.Rotation(
                transform,
                Quaternion.LookRotation(movement.normalized),
                1f / followSpeed, // Convert followSpeed to duration
                Ease.OutSine
            );
        }
    }
}
