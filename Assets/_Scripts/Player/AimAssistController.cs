using UnityEngine;

public class AimAssistController : MonoBehaviour
{
    private ShooterMovement shooterMovement;

    private void Awake()
    {
        shooterMovement = GetComponent<ShooterMovement>();
        if (shooterMovement == null)
        {
            Debug.LogError("ShooterMovement not found on the same GameObject.");
        }
    }

    [Header("Aim Assist")]
    [SerializeField, Range(0f, 1f)]
    private float aimAssistStrength = 0.8f;

    [SerializeField, Range(0f, 45f)]
    private float maxAimAssistAngle = 20f;

    [SerializeField, Range(0f, 45f)]
    private float lockMaintainAngle = 25f;

    [SerializeField, Range(0f, 1f)]
    private float lockGracePeriod = 0.8f;

    [Header("Aim Assist Stickiness")]
    [SerializeField, Range(0f, 1f)]
    private float stickinessStrength = 0.7f;

    [SerializeField, Range(0f, 1f)]
    private float stickinessRadius = 0.5f;

    private Transform currentLockedEnemy;
    private float range;

    public void SetCurrentLockedEnemy(Transform enemy)
    {
        currentLockedEnemy = enemy;
    }

    public void ApplyAimAssist(Transform reticleTransform, Transform cameraTransform)
    {
        if (shooterMovement == null || currentLockedEnemy == null)
        {
            shooterMovement?.ApplyAimAssist(Vector3.zero);
            return;
        }

        Vector3 reticlePosition = reticleTransform.position;
        Vector3 enemyPosition = currentLockedEnemy.position;
        Vector3 directionToEnemy = (enemyPosition - reticlePosition).normalized;

        Vector3 screenDirection = cameraTransform.InverseTransformDirection(directionToEnemy);
        screenDirection.z = 0;
        screenDirection = screenDirection.normalized;

        float angle = Vector3.Angle(cameraTransform.forward, directionToEnemy);

        if (angle <= maxAimAssistAngle)
        {
            float distanceToTarget = Vector3.Distance(reticlePosition, currentLockedEnemy.position);
            float normalizedDistance = Mathf.Clamp01(distanceToTarget / range);
            float dynamicStrength = Mathf.Lerp(aimAssistStrength * 2.5f, aimAssistStrength, normalizedDistance);

            float stickinessFactor = Mathf.Clamp01(1f - (angle / maxAimAssistAngle));
            stickinessFactor = Mathf.Pow(stickinessFactor, 2);

            Vector3 finalAimAssist = screenDirection * dynamicStrength * stickinessStrength * stickinessFactor;

            shooterMovement.ApplyAimAssist(finalAimAssist);
        }
        else
        {
            shooterMovement.ApplyAimAssist(Vector3.zero);
        }
    }

    public float GetMaxAimAssistAngle() => maxAimAssistAngle;
    public float GetLockMaintainAngle() => lockMaintainAngle;
    public float GetLockGracePeriod() => lockGracePeriod;

    [SerializeField, Range(0f, 1f)]
    private float centeringStrength = 0.5f;

    [SerializeField, Range(0f, 0.5f)]
    private float deadZoneThreshold = 0.1f;

    private Vector3 lastAimAssistDirection;

    public Vector3 CalculateAimAssistDirection(Transform reticleTransform, Transform cameraTransform)
    {
        if (currentLockedEnemy == null)
        {
            lastAimAssistDirection = Vector3.zero;
            return Vector3.zero;
        }

        Vector3 reticlePosition = reticleTransform.position;
        Vector3 enemyPosition = currentLockedEnemy.position;
        Vector3 directionToEnemy = (enemyPosition - reticlePosition).normalized;

        Vector3 screenDirection = cameraTransform.InverseTransformDirection(directionToEnemy);
        screenDirection.z = 0;
        screenDirection = screenDirection.normalized;

        float angle = Vector3.Angle(cameraTransform.forward, directionToEnemy);

        if (angle <= maxAimAssistAngle)
        {
            float distanceToTarget = Vector3.Distance(reticlePosition, enemyPosition);
            float normalizedDistance = Mathf.Clamp01(distanceToTarget / range);
            float dynamicStrength = Mathf.Lerp(aimAssistStrength * 2.5f, aimAssistStrength, normalizedDistance);

            float stickinessFactor = Mathf.Clamp01(1f - (angle / maxAimAssistAngle));
            stickinessFactor = Mathf.Pow(stickinessFactor, 2);

            Vector3 aimAssistDirection = screenDirection * dynamicStrength * stickinessStrength * stickinessFactor;

            // Apply centering effect
            aimAssistDirection += screenDirection * centeringStrength;

            // Apply dead zone
            if (Vector3.Distance(aimAssistDirection, lastAimAssistDirection) < deadZoneThreshold)
            {
                aimAssistDirection = lastAimAssistDirection;
            }

            lastAimAssistDirection = aimAssistDirection;
            return aimAssistDirection;
        }

        lastAimAssistDirection = Vector3.zero;
        return Vector3.zero;
    }
}
