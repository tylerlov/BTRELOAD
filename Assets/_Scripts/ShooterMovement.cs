using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cinemachine;
//using MoreMountains.Feedbacks;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.InputSystem;
using Chronos;

public class ShooterMovement : MonoBehaviour
{
    public float xySpeed = 35f; // Speed of the reticle movement
    private Vector3 startingPosition;

    private DefaultControls playerInputActions;
    private Vector2 inputVector;

    private Timeline timeline; 

    public GameObject objectToRotate;

    private float maxRotationX = 20f; // Maximum rotation when reticle is at the sides
    private float maxRotationYTop = -60f; // Maximum rotation when reticle is at the top
    private float maxRotationYBottom = -30f; // Maximum rotation when reticle is at the bottom
    private float toleranceX = 15f; // How close the reticle gets to the sides before rotation starts
    private float toleranceY = 20f; // How close the reticle gets to the top/bottom before rotation starts

    public bool enableClamping = true; // New boolean to enable/disable clamping

    void Awake()
    {
        playerInputActions = new DefaultControls();
    }

    void Start()
    {
        if (objectToRotate == null)
        {
            Debug.LogError("objectToRotate is not assigned.");
            enabled = false;
            return;
        }

        playerInputActions.Player.Enable();
        playerInputActions.Player.ReticleMovement.performed += OnReticleMovement;
        playerInputActions.Player.ReticleMovement.canceled += OnReticleMovement; // Ensure to handle input stop
        timeline = GetComponent<Timeline>();
        startingPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        if (playerInputActions != null)
        {
            playerInputActions.Player.Enable();
            playerInputActions.Player.ReticleMovement.performed += OnReticleMovement;
            playerInputActions.Player.ReticleMovement.canceled += OnReticleMovement;
        }
    }

    private void OnDisable()
    {
        if (playerInputActions != null)
        {
            playerInputActions.Player.ReticleMovement.performed -= OnReticleMovement;
            playerInputActions.Player.ReticleMovement.canceled -= OnReticleMovement;
            playerInputActions.Player.Disable();
        }
    }

    private void OnReticleMovement(InputAction.CallbackContext context)
    {
        inputVector = context.ReadValue<Vector2>();
    }

    void Update()
    {
        MoveReticle(inputVector);
    }

    void MoveReticle(Vector2 direction)
    {
        // Use Chronos' deltaTime if available, otherwise use Unity's Time.deltaTime
        float deltaTime = timeline != null ? timeline.deltaTime : Time.deltaTime;

        transform.localPosition += new Vector3(direction.x, direction.y, 0) * xySpeed * deltaTime;
        ClampPosition();
    }

    void ClampPosition()
    {
        if (!enableClamping) return; // Skip clamping if enableClamping is false

        Vector3 pos = transform.localPosition;

        // Adjust the min and max values for x to allow more horizontal movement
        float minX = -30f; // Increased from -22f
        float maxX = 30f;  // Increased from 22f
        float minY = -12f; 
        float maxY = 15f; 

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.localPosition = pos;

        // Call the method to rotate the object based on reticle position
        RotateObjectBasedOnReticlePosition(pos);
    }

    void RotateObjectBasedOnReticlePosition(Vector3 reticlePosition)
    {
        // Calculate how close the reticle is to each edge within the tolerance range
        float percentX = Mathf.InverseLerp(-20f + toleranceX, 20f - toleranceX, reticlePosition.x);
        float percentYTop = Mathf.InverseLerp(25f - toleranceY, 25f, reticlePosition.y);
        float percentYBottom = Mathf.InverseLerp(-15f, -15f + toleranceY, reticlePosition.y);

        // Map the percentage to the desired rotation range
        float rotationX = Mathf.Lerp(-maxRotationX, maxRotationX, percentX);
        float rotationY = 0f;

        // Apply different rotation if reticle is at the top or at the bottom
        if (reticlePosition.y > 0)
        {
            rotationY = Mathf.Lerp(0, maxRotationYTop, percentYTop);
        }
        else
        {
            rotationY = Mathf.Lerp(0, -maxRotationYBottom, percentYBottom);
        }

        // Apply the rotation to the object
        objectToRotate.transform.localEulerAngles = new Vector3(rotationY, rotationX, 0);
    }

    public void ResetToCenter()
    {
        transform.localPosition = startingPosition;
        inputVector = Vector2.zero;
    }
}
