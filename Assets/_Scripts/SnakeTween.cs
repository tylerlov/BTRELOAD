using PrimeTween;
using UnityEngine;

public class SnakeTween : MonoBehaviour
{
    [SerializeField]
    private float movementAmplitude = 0.1f;

    [SerializeField]
    private float movementFrequency = 1f;

    [SerializeField]
    private float rotationAmplitude = 5f;

    [SerializeField]
    private float rotationFrequency = 0.5f;

    [SerializeField]
    private Ease movementEase = Ease.InOutSine;

    [SerializeField]
    private Ease rotationEase = Ease.InOutSine;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        StartSnakeMovement();
    }

    private void StartSnakeMovement()
    {
        // Create a sequence for the snake-like movement
        Sequence
            .Create()
            .Group(CreatePositionTween())
            .Group(CreateRotationTween())
            .SetRemainingCycles(-1); // Repeat the sequence indefinitely
    }

    private Tween CreatePositionTween()
    {
        return Tween.LocalPosition(
            transform,
            startValue: initialPosition,
            endValue: initialPosition + new Vector3(0, movementAmplitude, 0),
            duration: 1f / movementFrequency,
            ease: movementEase,
            cycles: 1,
            cycleMode: CycleMode.Yoyo
        );
    }

    private Tween CreateRotationTween()
    {
        return Tween.LocalRotation(
            transform,
            startValue: initialRotation * Quaternion.Euler(0, 0, -rotationAmplitude),
            endValue: initialRotation * Quaternion.Euler(0, 0, rotationAmplitude),
            duration: 1f / rotationFrequency,
            ease: rotationEase,
            cycles: 1,
            cycleMode: CycleMode.Yoyo
        );
    }
}
