using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chronos;
using Cinemachine;
using FluffyUnderware.Curvy.Controllers;
using Lofelt.NiceVibrations;
using MoreMountains.Feedbacks;
using PrimeTween;
using SensorToolkit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.PostProcessing;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }

    [SerializeField]
    private Animator animator;

    [Header("Ground Detection")]
    [SerializeField]
    private LayerMask groundLayer;

    [SerializeField]
    private float raycastRange = 10f;

    [SerializeField]
    private float raycastLength = 10f;

    [SerializeField]
    private bool enableGroundDetection = true;

    [Header("Rotation")]
    [SerializeField]
    private GameObject horizontalRotatePlatform;

    [SerializeField]
    private GameObject enclosingSphere;

    [SerializeField]
    private GameObject shooting;

    [SerializeField]
    private float rotationCooldown = 0.2f; // New: Cooldown between rotations

    [SerializeField]
    private float rotationDuration = 0.5f; // Increased duration for smoother rotation

    [Header("Ricochet Dodge")]
    [SerializeField]
    private float dodgeDuration = 0.5f;

    [SerializeField]
    private float dodgeCooldown = 1f;

    [SerializeField]
    private float dodgeDistance = 5f;

    [SerializeField]
    private GameObject ricochetBlast;

    [Header("Look At Settings")]
    [SerializeField]
    private GameObject lookAtTarget;

    [Header("Feedback Settings")]
    [SerializeField]
    private GameObject ricochetFeedbackObject;

    [Header("Spline Controller")]
    [SerializeField]
    private SplineController splineController;

    [Header("Camera")]
    [SerializeField]
    private CinemachineVirtualCamera mainVirtualCamera;
    private CinemachineBrain cinemachineBrain;
    private Vector3 cameraPositionBeforeRotation;

    [SerializeField]
    private ShooterMovement shooterMovement;

    #region Private Fields

    private Rigidbody rigidbody;
    private DefaultControls playerInputActions;
    private PlayerHealth playerHealth;
    private float currentYRotation;
    private float shootingZOffset;
    private int playerFacingDirection;
    private bool isRotating;
    private bool isDodging = false;
    private float lastDodgeTime = -Mathf.Infinity;
    private Vector3 startingPosition;
    private Timeline timeline;

    private float groundCheckInterval = 0.1f; // Check ground every 0.1 seconds
    private float nextGroundCheckTime = 0f;
    private float targetHeight = 0f; // Target height for interpolation
    private bool shouldLerpHeight = false; // Flag to control height interpolation

    private float rotationDebounceTime = 0.1f; // 200 milliseconds debounce period
    private float lastRotationTime;

    private RangeSensor rangeSensor;

    private StaminaController staminaController;

    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        playerInputActions = new DefaultControls();
        playerInputActions.Player.Enable();
        playerInputActions.Player.ReverseDirection.performed += OnReverseDirection;
        playerInputActions.Player.RotateLeft.performed += ctx => RotateLeft();
        playerInputActions.Player.RotateRight.performed += ctx => RotateRight();

        rigidbody = GetComponent<Rigidbody>();
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationY | rigidbody.constraints;

        rangeSensor = GetComponent<RangeSensor>();
        if (rangeSensor == null)
        {
            Debug.LogError("RangeSensor component not found on the player!");
        }

        playerHealth = GetComponent<PlayerHealth>();
        timeline = GetComponent<Timeline>();
        cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();

        staminaController = GetComponent<StaminaController>();
        if (staminaController == null)
        {
            Debug.LogError("StaminaController component not found on the same GameObject as PlayerMovement!");
        }
    }

    private void OnDestroy()
    {
        // Unbind the input action when the object is destroyed
        playerInputActions.Player.ReverseDirection.performed -= OnReverseDirection;
    }

    private void Start()
    {
        playerFacingDirection = 0;
        animator.SetInteger("PlayerDirection", playerFacingDirection);

        startingPosition = transform.localPosition;
        shootingZOffset = shooting.transform.localPosition.z;
    }

    private void Update()
    {
        RestrictToSphere();

        if (CheckRicochetDodge())
        {
            TryRicochetDodge();
        }

        AdjustYRotation();

        if (enableGroundDetection && Time.time >= nextGroundCheckTime)
        {
            CalculateHeightDifference();
            nextGroundCheckTime = Time.time + groundCheckInterval;
        }
        else if (shouldLerpHeight)
        {
            LerpToTargetHeight();
        }

        LookAtTarget();

        EnsurePlayerUpright();
    }

    private void OnDrawGizmos()
    {
        DrawGroundDetectionRaycast();
    }

    private void CalculateHeightDifference()
    {
        if (rigidbody == null || GetComponent<BoxCollider>() == null)
            return;

        Vector3 raycastOrigin = GetRaycastOrigin();
        RaycastHit hit;

        // Perform the raycast to check for ground
        if (Physics.Raycast(raycastOrigin, -transform.up, out hit, raycastLength, groundLayer))
        {
            float heightDifference = hit.distance;

            if (heightDifference <= raycastRange)
            {
                // Set the target height and enable height interpolation
                targetHeight = transform.position.y - heightDifference;
                shouldLerpHeight = true;
            }
        }
    }

    private void LerpToTargetHeight()
    {
        // Determine the new position by interpolating towards the target height
        Vector3 newPosition = new Vector3(
            transform.position.x,
            Mathf.Lerp(transform.position.y, targetHeight, Time.deltaTime * 5),
            transform.position.z
        );
        rigidbody.MovePosition(newPosition);

        // If the player is close enough to the target height, stop interpolating
        if (Mathf.Abs(transform.position.y - targetHeight) < 0.01f)
        {
            shouldLerpHeight = false;
        }
    }

    private Vector3 GetRaycastOrigin()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        return transform.position + boxCollider.center - new Vector3(0, boxCollider.size.y / 2, 0);
    }

    private void DrawGroundDetectionRaycast()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        Vector3 raycastOrigin = GetRaycastOrigin();
        Gizmos.color = Color.red;
        Gizmos.DrawLine(raycastOrigin, raycastOrigin - transform.up * raycastLength);
    }

    private void RestrictToSphere()
    {
        Vector3 offset = transform.position - enclosingSphere.transform.position;
        Vector3 clampedOffset = Vector3.ClampMagnitude(
            offset,
            enclosingSphere.transform.localScale.x / 2
        );
        Vector3 finalPosition = enclosingSphere.transform.position + clampedOffset;
        transform.position = finalPosition;
    }

    private void AdjustYRotation()
    {
        currentYRotation = transform.eulerAngles.y;
        float targetYRotation = GetTargetYRotation(currentYRotation);

        if (Mathf.Abs(currentYRotation - targetYRotation) > 1)
        {
            // Stop any existing rotation tween
            Tween.StopAll(transform);

            // Create a new rotation tween
            Tween.Rotation(
                transform,
                Quaternion.Euler(transform.eulerAngles.x, targetYRotation, transform.eulerAngles.z),
                0.3f,
                Ease.InOutQuad
            );
        }
    }

    private float GetTargetYRotation(float currentYRotation)
    {
        if (currentYRotation >= 315 || currentYRotation < 45)
            return 0;

        if (currentYRotation >= 45 && currentYRotation < 135)
            return 90;

        if (currentYRotation >= 135 && currentYRotation < 225)
            return 180;

        return 270;
    }

    private void RotateLeft()
    {
        if (isRotating || Time.time - lastRotationTime < rotationCooldown)
            return;

        lastRotationTime = Time.time;
        isRotating = true;
        Rotate(-90);
    }

    private void RotateRight()
    {
        if (isRotating || Time.time - lastRotationTime < rotationCooldown)
            return;

        lastRotationTime = Time.time;
        isRotating = true;
        Rotate(90);
    }

    private void Rotate(float targetRotationDelta)
    {
        float currentRotation = horizontalRotatePlatform.transform.localEulerAngles.y;
        float targetRotation = (currentRotation + targetRotationDelta + 360) % 360;

        // Ensure we're rotating in the shortest direction
        float rotationDelta = Mathf.DeltaAngle(currentRotation, targetRotation);

        // Update playerFacingDirection based on the new rotation
        playerFacingDirection = Mathf.RoundToInt(targetRotation / 90f) % 4;
        playerFacingDirection = (playerFacingDirection + 4) % 4; // Ensure it's always 0, 1, 2, or 3
        animator.SetInteger("PlayerDirection", playerFacingDirection);

        float zMultiplier = GetShootingZMultiplier(playerFacingDirection);

        Tween.LocalPositionZ(shooting.transform, shootingZOffset * zMultiplier, 0.5f);

        Tween
            .LocalRotation(
                horizontalRotatePlatform.transform,
                Quaternion.Euler(0, targetRotation, 0),
                0.15f,
                Ease.InOutQuad
            )
            .OnComplete(() =>
            {
                isRotating = false;
            });
    }

    private float GetShootingZMultiplier(int facingDirection)
    {
        switch (facingDirection)
        {
            case 1:
                return 1.8f;
            case 2:
                return 2.8f;
            case 3:
                return 1.8f;
            case 0:
                return 1f;
            default:
                Debug.LogWarning(
                    $"Unexpected playerFacingDirection: {facingDirection}. Defaulting to 1."
                );
                return 1f;
        }
    }

    private bool CheckRicochetDodge()
    {
        bool isDodgePressed = playerInputActions.Player.Dodge.ReadValue<float>() > 0;
        if (isDodgePressed)
        {
            Debug.Log("Dodge input detected"); // New debug log
        }
        return isDodgePressed;
    }

    private void TryRicochetDodge()
    {
        Debug.Log("TryRicochetDodge called"); // New debug log

        if (isDodging || Time.time - lastDodgeTime < dodgeCooldown)
        {
            Debug.Log("Dodge attempted, but either already dodging or on cooldown.");
            return;
        }

        if (staminaController == null)
        {
            Debug.LogError("StaminaController is not initialized!");
            return;
        }

        StartCoroutine(PerformRicochetDodge());
    }

    private IEnumerator PerformRicochetDodge()
    {
        Debug.Log("PerformRicochetDodge started"); // New debug log

        if (!staminaController.TryUseDodgeStamina())
        {
            Debug.Log("Not enough stamina to dodge.");
            yield break;
        }

        isDodging = true;
        lastDodgeTime = Time.time;

        Vector3 dodgeDirection = GetDodgeDirection();
        Vector3 dodgePosition = transform.position + dodgeDirection * dodgeDistance;

        // Play dodge effects
        PlayDodgeEffects();

        // Ricochet nearby projectiles
        RicochetNearbyProjectiles();

        // Perform the dodge movement
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        while (elapsedTime < dodgeDuration)
        {
            transform.position = Vector3.Lerp(
                startPosition,
                dodgePosition,
                elapsedTime / dodgeDuration
            );
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = dodgePosition;

        isDodging = false;
        Debug.Log("Dodge completed. Next dodge available in " + dodgeCooldown + " seconds.");
    }

    private Vector3 GetDodgeDirection()
    {
        // Implement logic to determine dodge direction
        // For example, use input direction or away from nearest projectile
        return transform.forward; // Placeholder
    }

    private void PlayDodgeEffects()
    {
        // Play sound effect
        FMODUnity.RuntimeManager.PlayOneShot("event:/Player/Ricochet");

        // Play haptic feedback
        HapticPatterns.PlayEmphasis(1.0f, 0.0f);

        // Trigger particle effects
        if (ricochetBlast != null)
        {
            ricochetBlast.SetActive(true);
            StartCoroutine(DisableRicochetBlast());
        }
    }

    private IEnumerator DisableRicochetBlast()
    {
        yield return new WaitForSeconds(dodgeDuration);
        ricochetBlast.SetActive(false);
    }

    private void RicochetNearbyProjectiles()
    {
        if (rangeSensor != null && rangeSensor.DetectedObjects.Count > 0)
        {
            foreach (var detectedObject in rangeSensor.DetectedObjects)
            {
                var projectile = detectedObject.GetComponent<ProjectileStateBased>();
                if (projectile != null)
                {
                    projectile.OnPlayerRicochetDodge();
                    Debug.Log($"Ricochet applied to projectile: {projectile.gameObject.name}");
                }
            }
        }
    }

    public void PlayerPositionReset()
    {
        transform.localPosition = startingPosition;
    }

    public void TransitionAnim()
    {
        animator.SetBool("PlayerTransition", true);
    }

    public void PlayerDirectionForward()
    {
        animator.SetBool("PlayerTransition", false);
        animator.SetInteger("PlayerDirection", 0);

        currentYRotation = 0;
        shooting.transform.localPosition = new Vector3(0, 0, shootingZOffset);
    }

    public int GetPlayerFacingDirection()
    {
        return playerFacingDirection;
    }

    public void UpdateAnimation()
    {
        if (animator != null && animator.enabled)
        {
            float horizontalRotation = horizontalRotatePlatform.transform.localEulerAngles.y;
            int directionIndex = Mathf.RoundToInt(horizontalRotation / 90) % 4;

            if (directionIndex != playerFacingDirection)
            {
                playerFacingDirection = directionIndex;
                animator.SetInteger("PlayerDirection", playerFacingDirection);
            }
        }
    }

    private void LookAtTarget()
    {
        if (lookAtTarget != null)
        {
            Vector3 lookPosition = lookAtTarget.transform.position - transform.position;
            lookPosition.y = 0;
            Quaternion rotation = Quaternion.LookRotation(lookPosition);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotation,
                Time.deltaTime * 340
            ); // Using a constant lookSpeed of 340
        }
    }

    private void EnsurePlayerUpright()
    {
        if (transform.parent != null)
        {
            // Calculate the upright rotation relative to the parent's rotation
            Quaternion targetRotation = Quaternion.Euler(
                0,
                transform.eulerAngles.y - transform.parent.eulerAngles.y,
                0
            );
            // Apply the calculated rotation to the GameObject
            transform.localRotation = targetRotation;
        }
        else
        {
            // If the GameObject doesn't have a parent, just ensure it's upright in world space
            transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }
    }

    // New method to handle direction reversal
    private void OnReverseDirection(InputAction.CallbackContext context)
    {
        if (splineController != null)
        {
            splineController.MovementDirection =
                splineController.MovementDirection == MovementDirection.Forward
                    ? MovementDirection.Backward
                    : MovementDirection.Forward;

            // Perform the 180-degree rotation
            Rotate180();
        }
    }

    private void Rotate180()
    {
        if (isRotating)
            return;

        isRotating = true;
        float currentRotation = horizontalRotatePlatform.transform.localEulerAngles.y;
        float targetRotation = (currentRotation + 180) % 360;

        // Disable ShooterMovement and LookAtReversed during rotation
        ShooterMovement shooterMovement = shooting.GetComponent<ShooterMovement>();
        LookAtReversed lookAtReversed = shooting.GetComponent<LookAtReversed>();
        if (shooterMovement)
            shooterMovement.enabled = false;
        if (lookAtReversed)
            lookAtReversed.enabled = false;

        // Rotate the horizontalRotatePlatform
        Tween
            .LocalRotation(
                horizontalRotatePlatform.transform,
                Quaternion.Euler(0, targetRotation, 0),
                rotationDuration,
                Ease.InOutQuad
            )
            .OnComplete(() =>
            {
                isRotating = false;

                // Re-enable ShooterMovement and LookAtReversed
                if (shooterMovement)
                    shooterMovement.enabled = true;
                if (lookAtReversed)
                    lookAtReversed.enabled = true;

                // Update playerFacingDirection
                playerFacingDirection = (playerFacingDirection + 2) % 4;
                animator.SetInteger("PlayerDirection", playerFacingDirection);

                // Call OnRotate180 on ShooterMovement
                if (shooterMovement)
                    shooterMovement.OnRotate180();
            });
    }

    private IEnumerator FreezeCameraDuringRotation()
    {
        if (cinemachineBrain != null)
        {
            // Store the current camera position
            cameraPositionBeforeRotation = Camera.main.transform.position;

            // Disable Cinemachine updates
            cinemachineBrain.enabled = false;

            // Wait for the rotation to complete
            yield return new WaitForSeconds(rotationDuration);

            // Re-enable Cinemachine updates
            cinemachineBrain.enabled = true;

            // Force update the virtual camera to prevent any sudden movements
            if (mainVirtualCamera != null)
            {
                mainVirtualCamera.OnTargetObjectWarped(
                    transform,
                    transform.position - mainVirtualCamera.transform.position
                );
            }
        }
    }
}