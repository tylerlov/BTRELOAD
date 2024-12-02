using UnityEngine;

public class TempoSpin : MonoBehaviour
{
    public float tempo = 120f;

    public enum RotationAxis
    {
        X_Axis,
        Y_Axis,
        Z_Axis
    }

    public RotationAxis rotationAxis;

    public enum RotationMultiplier
    {
        One = 1,
        Two = 2,
        Four = 4,
        Eight = 8,
        Sixteen = 16,
        ThirtyTwo = 32,
        SixtyFour = 64,
        OneTwentyEight = 128,
        TwoFiftySix = 256,
        FiveOneTwo = 512,
        OneZeroTwoFour = 1024
    }

    public RotationMultiplier rotationMultiplier = RotationMultiplier.One;

    private float rotationSpeed;
    private Vector3 axisVector;

    private void Start()
    {
        // Calculate rotation speed in degrees per second
        rotationSpeed = (360 * tempo / 60) / (float)rotationMultiplier;

        // Set the rotation axis vector based on the selected enum
        switch (rotationAxis)
        {
            case RotationAxis.X_Axis:
                axisVector = Vector3.right;
                break;
            case RotationAxis.Y_Axis:
                axisVector = Vector3.up;
                break;
            case RotationAxis.Z_Axis:
                axisVector = Vector3.forward;
                break;
        }
    }

    private void Update()
    {
        // Rotate the object around the specified axis at the calculated speed
        transform.Rotate(axisVector * rotationSpeed * Time.deltaTime);
    }
}
