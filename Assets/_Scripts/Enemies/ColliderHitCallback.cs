using BehaviorDesigner.Runtime.Tactical;
using PrimeTween; // Changed from DOTween to PrimeTween
using UnityEngine;

public class ColliderHitCallback : BaseBehaviour, IDamageable
{
    public GameObject bossObject;
    private ILimbDamageReceiver bossScript; // Keep the interface reference, but don't serialize it

    [SerializeField]
    private GameObject lockedOnIndicator; // Add a reference to a locked-on indicator, such as a visual effect or model

    public bool enableShakeOnDamage = true; // Public bool to control shake animation

    [SerializeField]
    private bool lockable = true; // New boolean to determine if this object can be locked onto

    private void Awake()
    {
        if (bossObject != null)
        {
            bossScript = bossObject.GetComponent<ILimbDamageReceiver>();
            if (bossScript == null)
            {
                ConditionalDebug.LogWarning(
                    "The assigned GameObject does not have a component that implements ILimbDamageReceiver."
                );
            }
        }
        else
        {
            ConditionalDebug.LogWarning("Boss object is not assigned in ColliderHitCallback.");
        }
        // Optionally initialize the lockedOnIndicator state
        SetLockedStatus(false); // Ensure the indicator is initially disabled
    }

    public void Damage(float amount)
    {
        bossScript.DamageFromLimb(gameObject.name, amount); // Pass the name of the limb being hit and the damage amount
        SetLockedStatus(false); // Disable the lockedOnIndicator when hit

        // Conditional shake animation based on enableShakeOnDamage
        if (enableShakeOnDamage)
        {
            // Convert DOTween shake to PrimeTween shake
            Tween.ShakeLocalPosition(
                transform,
                duration: 0.5f,
                strength: new Vector3(0.1f, 0.1f, 0.1f),
                frequency: 10,
                easeBetweenShakes: Ease.InOutQuad
            );
        }
    }

    public bool IsAlive()
    {
        // Implement logic to determine if the object is alive
        // For example, return false if health <= 0
        return true; // Placeholder return value, adjust based on your game logic
    }

    // Method to handle the locked status
    public void SetLockedStatus(bool isLocked)
    {
        if (!lockable)
        {
            isLocked = false; // Force unlock if not lockable
        }

        if (lockedOnIndicator != null)
        {
            lockedOnIndicator.SetActive(isLocked);
        }
        // Additional logic for when the object is locked or unlocked can be added here
    }

    // New method to check if the object is lockable
    public bool IsLockable()
    {
        return lockable;
    }
}
