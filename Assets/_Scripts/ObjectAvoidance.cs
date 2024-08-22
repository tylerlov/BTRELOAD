using SensorToolkit; // Ensure you have this namespace to access Sensor Toolkit components
using UnityEngine;

public class ObjectAvoidance : MonoBehaviour
{
    private RangeSensor rangeSensor; // Now private and assigned in Start()
    public GameObject objectToMove; // The GameObject to move when detecting 'Ground'
    public float moveSpeed = 1f; // Speed of the movement, settable in inspector

    public bool enableXMovement = true; // Enable movement on the X axis
    public bool enableYMovement = true; // Enable movement on the Y axis
    public bool enableZMovement = true; // Enable movement on the Z axis

    private Vector3 originalPosition; // To store the original position of the object
    private bool isGroundDetected = false; // Flag to check if ground is detected

    void Start()
    {
        // Automatically assign the RangeSensor component
        rangeSensor = GetComponent<RangeSensor>();

        if (objectToMove != null)
        {
            originalPosition = objectToMove.transform.position; // Store the original position
        }
    }

    void Update()
    {
        if (rangeSensor != null && objectToMove != null)
        {
            // Check if the sensor detects any GameObjects on the 'Ground' layer
            isGroundDetected = rangeSensor.DetectedObjects.Exists(obj =>
                obj != null && obj.layer == LayerMask.NameToLayer("Ground")
            );

            if (isGroundDetected)
            {
                // Find the direction opposite to the detected ground object
                Vector3 directionToGround =
                    rangeSensor.DetectedObjects[0].transform.position
                    - objectToMove.transform.position;
                Vector3 moveDirection = -directionToGround.normalized;

                // Apply axis-specific movement restrictions
                moveDirection = new Vector3(
                    enableXMovement ? moveDirection.x : 0,
                    enableYMovement ? moveDirection.y : 0,
                    enableZMovement ? moveDirection.z : 0
                );

                // Move the object in the specified direction(s)
                objectToMove.transform.position += moveDirection * moveSpeed * Time.deltaTime;
            }
            else
            {
                // Calculate the direction to lerp back based on enabled axes
                Vector3 directionBack = new Vector3(
                    enableXMovement ? originalPosition.x - objectToMove.transform.position.x : 0,
                    enableYMovement ? originalPosition.y - objectToMove.transform.position.y : 0,
                    enableZMovement ? originalPosition.z - objectToMove.transform.position.z : 0
                );

                // Lerp back to the original position on the enabled axes
                objectToMove.transform.position +=
                    directionBack.normalized * moveSpeed * Time.deltaTime;
            }
        }
    }
}
