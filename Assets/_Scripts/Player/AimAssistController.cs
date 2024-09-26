using UnityEngine;

public class AimAssistController : MonoBehaviour
{
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

    private ShooterMovement shooterMovement;
    private Transform currentLockedEnemy;
    private float range;

    public void Initialize(ShooterMovement movement, float detectionRange)
    {
        shooterMovement = movement;
        range = detectionRange;
    }

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
}
