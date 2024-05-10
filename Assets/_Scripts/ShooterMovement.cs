using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cinemachine;
//using MoreMountains.Feedbacks;
using UnityEngine.Rendering.PostProcessing;
//using Opsive.Shared.Camera;
using UnityEngine.InputSystem;
using Chronos;

public class ShooterMovement : MonoBehaviour
{
    public float xySpeed;    
    private Vector3 startingPosition;

    private DefaultControls playerInputActions;

    private Timeline timeline; 

    public GameObject objectToRotate;

    private float maxRotationX = 20f; // Maximum rotation when reticle is at the sides
    private float maxRotationYTop = -60f; // Maximum rotation when reticle is at the top
    private float maxRotationYBottom = -30f; // Maximum rotation when reticle is at the bottom
    private float toleranceX = 15f; // How close the reticle gets to the sides before rotation starts
    private float toleranceY = 20f; // How close the reticle gets to the top/bottom before rotation starts

    void Start()
    {
        playerInputActions = new DefaultControls();
        playerInputActions.Player.Enable();
        playerInputActions.Player.ReticleMovement.performed += ReticleMovement_performed;
        timeline = GetComponent<Timeline>();
        startingPosition = transform.localPosition;
    }
    private void ReticleMovement_performed(InputAction.CallbackContext context)
    {
        Vector2 inputVector = context.ReadValue<Vector2>();
        // Log the input vector to the console
        LocalMove(inputVector.x, inputVector.y, xySpeed);
    }

     void LocalMove(float x, float y, float speed)
    {
        // Use Chronos' deltaTime instead of Unity's Time.deltaTime
        float deltaTime = timeline.deltaTime;

        transform.localPosition += new Vector3(x, y, 0) * speed * deltaTime;
        ClampPosition();
    }

    void ClampPosition()
    {
        Vector3 pos = transform.localPosition;

        // Define the min and max values for x and y in local space
        float minX = -22f; 
        float maxX = 22f; 
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
        //Do I want to implement this function?
    }

    private void OnDisable()
    {
        playerInputActions.Player.Disable();
    }
}