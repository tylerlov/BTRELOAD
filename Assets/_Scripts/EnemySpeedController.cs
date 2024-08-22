using System.Linq;
using Pathfinding;
using UnityEngine;

[RequireComponent(typeof(EnemyBasicSetup))]
[RequireComponent(typeof(CustomAIPathAlignedToSurface))]
public class EnemySpeedController : MonoBehaviour
{
    [SerializeField]
    private float speedIncreasePerPartDestroyed = 0.5f;

    [SerializeField]
    private float maxSpeedMultiplier = 2f;

    private EnemyBasicSetup enemySetup;
    private CustomAIPathAlignedToSurface aiPath;
    private int initialPartCount;
    private int currentAliveParts;
    private float initialSpeed;

    private void Awake()
    {
        enemySetup = GetComponent<EnemyBasicSetup>();
        aiPath = GetComponent<CustomAIPathAlignedToSurface>();

        // Initialize part count and speed
        initialPartCount = GetDamageablePartsCount();
        currentAliveParts = initialPartCount;
        initialSpeed = aiPath.maxSpeed;
    }

    private void OnEnable()
    {
        // Subscribe to the Die event for each damageable part
        var damageableParts = GetComponentsInChildren<EnemyBasicDamagablePart>();
        foreach (var part in damageableParts)
        {
            part.OnPartDestroyed += HandlePartDestroyed;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from the Die event for each damageable part
        var damageableParts = GetComponentsInChildren<EnemyBasicDamagablePart>();
        foreach (var part in damageableParts)
        {
            part.OnPartDestroyed -= HandlePartDestroyed;
        }
    }

    private int GetDamageablePartsCount()
    {
        return GetComponentsInChildren<EnemyBasicDamagablePart>().Length;
    }

    private void HandlePartDestroyed()
    {
        currentAliveParts--;
        UpdateSpeed();
    }

    private void UpdateSpeed()
    {
        int destroyedParts = initialPartCount - currentAliveParts;
        float speedMultiplier = 1f + (destroyedParts * speedIncreasePerPartDestroyed);

        // Clamp the speed multiplier to the maximum allowed value
        speedMultiplier = Mathf.Min(speedMultiplier, maxSpeedMultiplier);

        // Update the AI path speed
        aiPath.maxSpeed = initialSpeed * speedMultiplier;

        ConditionalDebug.Log(
            $"Enemy speed updated. Destroyed parts: {destroyedParts}, New speed: {aiPath.maxSpeed}"
        );
    }

    public void ResetSpeed()
    {
        currentAliveParts = initialPartCount;
        aiPath.maxSpeed = initialSpeed;
        ConditionalDebug.Log($"Enemy speed reset to initial value: {initialSpeed}");
    }
}
