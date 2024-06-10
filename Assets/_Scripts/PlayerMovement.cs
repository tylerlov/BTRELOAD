using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cinemachine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.InputSystem;
using System.Linq;
using Chronos;
using Lofelt.NiceVibrations;
using SensorToolkit;
using MoreMountains.Feedbacks;

public class PlayerMovement : MonoBehaviour
{

        public Animator animator;


    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastRange = 10f;
    [SerializeField] private float raycastLength = 10f;
    [SerializeField] private bool enableGroundDetection = true;

    [Header("Rotation")]
    [SerializeField] private GameObject horizontalRotatePlatform;
    [SerializeField] private GameObject enclosingSphere;
    [SerializeField] private GameObject shooting;

    [Header("RicochetDodge Mechanic")]
    [SerializeField] private float dodgeDistance = 5f;
    [SerializeField] private float dodgeDuration = 0.5f;
    [SerializeField] private float dodgeStayDuration = 1f;
    [SerializeField] private float returnDuration = 0.5f;
    [SerializeField] private float dodgeResetTime = 0.6f;
    [SerializeField] private GameObject ricochetBlast; // Reference to the GameObject containing the particle system

    [Header("Look At Settings")]
    [SerializeField] private GameObject lookAtTarget;

    [Header("Feedback Settings")]
    [SerializeField] private GameObject ricochetFeedbackObject;

    #region Private Fields

    private Rigidbody rigidbody;
    private DefaultControls playerInputActions;
    private PlayerHealth playerHealth;
    private float currentYRotation;
    private float shootingZOffset;
    private int playerFacingDirection;
    private bool isRotating;
    private bool isDodging;
    private int consecutiveDodges;
    private float lastDodgeTime;
    private Vector3 startingPosition;
    private Timeline timeline;

    private float groundCheckInterval = 0.1f; // Check ground every 0.1 seconds
    private float nextGroundCheckTime = 0f;
    private float targetHeight = 0f; // Target height for interpolation
    private bool shouldLerpHeight = false; // Flag to control height interpolation

    private float rotationDebounceTime = 0.1f; // 200 milliseconds debounce period
    private float lastRotationTime = 0;

    #endregion

    private void Awake()
    {
        playerInputActions = new DefaultControls();
        playerInputActions.Player.Enable();

        rigidbody = GetComponent<Rigidbody>();
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationY | rigidbody.constraints;
    }

    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        timeline = GetComponent<Timeline>();

        playerFacingDirection = 0;
        animator.SetInteger("PlayerDirection", playerFacingDirection);

        startingPosition = transform.localPosition;
        shootingZOffset = shooting.transform.localPosition.z;
    }

    private void Update()
    {
        RestrictToSphere();

        if (CheckRicochetDodge())
            RicochetDodge();

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
        Vector3 newPosition = new Vector3(transform.position.x, Mathf.Lerp(transform.position.y, targetHeight, Time.deltaTime * 5), transform.position.z);
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
        Vector3 clampedOffset = Vector3.ClampMagnitude(offset, enclosingSphere.transform.localScale.x / 2);
        Vector3 finalPosition = enclosingSphere.transform.position + clampedOffset;
        transform.position = finalPosition;
    }

    private void AdjustYRotation()
    {
        currentYRotation = transform.eulerAngles.y;
        float targetYRotation = GetTargetYRotation(currentYRotation);

        if (Mathf.Abs(currentYRotation - targetYRotation) > 1)
        {
            string rotationTweenId = "rotationTween";
            DOTween.Kill(rotationTweenId);

            transform.DORotate(new Vector3(transform.eulerAngles.x, targetYRotation, transform.eulerAngles.z), 0.3f)
                .SetEase(Ease.InOutQuad)
                .SetId(rotationTweenId);
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

    public void RotateLeft()
    {
        if (isRotating || Time.time - lastRotationTime < rotationDebounceTime) return;

        lastRotationTime = Time.time;
        isRotating = true;
        playerFacingDirection--;

        if (playerFacingDirection == -1)
            playerFacingDirection = 3;

        animator.SetInteger("PlayerDirection", playerFacingDirection);
        RotatePlayer(-90, () => isRotating = false);
    }

    public void RotateRight()
    {
        if (isRotating || Time.time - lastRotationTime < rotationDebounceTime) return;

        lastRotationTime = Time.time;
        isRotating = true;
        playerFacingDirection++;

        if (playerFacingDirection == 4)
            playerFacingDirection = 0;

        animator.SetInteger("PlayerDirection", playerFacingDirection);
        RotatePlayer(90, () => isRotating = false);
    }

    private void RotatePlayer(float rotationAmount, Action onComplete)
    {
        Quaternion currentLocalRotation = horizontalRotatePlatform.transform.localRotation;
        Quaternion targetLocalRotation = currentLocalRotation * Quaternion.Euler(0, rotationAmount, 0);
        float zMultiplier = GetShootingZMultiplier(playerFacingDirection);

        shooting.transform.DOLocalMoveZ(shootingZOffset * zMultiplier, 0.5f, false);

        horizontalRotatePlatform.transform.DOLocalRotateQuaternion(targetLocalRotation, 0.15f)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => {
                isRotating = false;
                onComplete?.Invoke();
            });
    }

    private float GetShootingZMultiplier(int facingDirection)
    {
        switch (facingDirection)
        {
            case 1: return 1.8f;
            case 2: return 2.8f;
            case 3: return 1.8f;
            case 0: return 1f;
            default: throw new System.InvalidOperationException($"Invalid playerFacingDirection: {facingDirection}");
        }
    }

    private bool CheckRicochetDodge()
    {
        return playerInputActions.Player.Dodge.ReadValue<float>() > 0;
    }

    private void RicochetDodge()
    {
        if (isDodging)
            return;

        // Play the Ricochet Dodge sound event
        FMODUnity.RuntimeManager.PlayOneShot("event:/Player/Ricochet");

        HapticPatterns.PlayEmphasis(1.0f, 0.0f);

        isDodging = true;
        playerHealth.DodgeInvincibility = true;

        UpdateConsecutiveDodges();

        Vector3 dodgeDirection = GetRicochetDodgeDirection();
        Vector3 dodgePosition = transform.position + dodgeDirection * dodgeDistance;

        string dodgeTweenId = "ricochetDodgeTween";

        transform.DOMove(dodgePosition, dodgeDuration)
            .OnComplete(() => StartCoroutine(ResetRicochetDodge()))
            .SetEase(Ease.OutQuad)
            .SetId(dodgeTweenId)
            .SetLink(gameObject);

        TriggerProjectileBehaviorChange();

        // Trigger all Particle Systems in the Ricochet Blast GameObject
        PlayAllParticleSystems(ricochetBlast);

        // Play the ricochet feedbacks
        if (ricochetFeedbackObject != null)
        {
            var feedbacks = ricochetFeedbackObject.GetComponent<MMFeedbacks>();
            if (feedbacks != null)
            {
                feedbacks.PlayFeedbacks();
            }
        }
    }

    private void PlayAllParticleSystems(GameObject root)
    {
        if (root == null) return;

        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Play();
        }
    }

    private Vector3 GetRicochetDodgeDirection()
    {
        var sensor = GetComponent<RangeSensor>();
        if (sensor.DetectedObjects.Count > 0)
        {
            var projectile = sensor.DetectedObjects.First().gameObject;
            Vector3 projectileDirection = transform.position - projectile.transform.position;
            return projectileDirection.normalized;
        }
        return new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized;
    }

    private void TriggerProjectileBehaviorChange()
    {
        var sensor = GetComponent<RangeSensor>();
        foreach (var obj in sensor.DetectedObjects)
        {
            var projectile = obj.GetComponent<ProjectileStateBased>();
            if (projectile != null)
            {
                projectile.OnPlayerRicochetDodge();
            }
        }
    }

    private void UpdateConsecutiveDodges()
    {
        if (timeline.time - lastDodgeTime <= dodgeResetTime)
            consecutiveDodges++;
        else
            consecutiveDodges = 1;

        lastDodgeTime = timeline.time;
    }

    private System.Collections.IEnumerator ResetRicochetDodge()
    {
        yield return new WaitForSeconds(dodgeDuration + dodgeStayDuration);

        isDodging = false;
        playerHealth.DodgeInvincibility = false;

        ResetConsecutiveDodges();
    }

    private void ResetConsecutiveDodges()
    {
        if (timeline.time - lastDodgeTime > dodgeResetTime)
            consecutiveDodges = 0;
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

    public float GetPlayerFacingDirection()
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
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 340); // Using a constant lookSpeed of 340
        }
    }

    private void EnsurePlayerUpright()
    {
        if (transform.parent != null)
        {
            // Calculate the upright rotation relative to the parent's rotation
            Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y - transform.parent.eulerAngles.y, 0);
            // Apply the calculated rotation to the GameObject
            transform.localRotation = targetRotation;
        }
        else
        {
            // If the GameObject doesn't have a parent, just ensure it's upright in world space
            transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }
    }
}
