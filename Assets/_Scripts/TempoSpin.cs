using UnityEngine;

public class TempoSpin : MonoBehaviour
{
    // Define an enum for the rotation axes
    public enum RotationAxis
    {
        X_Axis,
        Y_Axis,
        Z_Axis,
    }

    // Define an enum for rotation multipliers
    public enum RotationMultiplier
    {
        x1 = 1,
        x2 = 2,
        x4 = 4,
        x8 = 8,
        x16 = 16,
        x32 = 32,
        x64 = 64,
        x128 = 128,
    }

    [SerializeField]
    private float tempo = 120f; // Tempo in beats per minute

    [SerializeField]
    private RotationAxis rotationAxis = RotationAxis.Y_Axis; // Default rotation around the Y-axis

    [SerializeField]
    private RotationMultiplier rotationMultiplier = RotationMultiplier.x1; // Default multiplier

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
