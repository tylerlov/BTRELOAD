using UnityEngine;
using BehaviorDesigner.Runtime.Tactical;
using DG.Tweening; // Required for DOTween animations

public class ColliderHitCallback : BaseBehaviour, IDamageable
{
    [SerializeField] private GameObject bossObject; // Serialize a GameObject reference

    private ILimbDamageReceiver bossScript; // Keep the interface reference, but don't serialize it

    [SerializeField] private GameObject lockedOnIndicator; // Add a reference to a locked-on indicator, such as a visual effect or model

    public bool enableShakeOnDamage = true; // Public bool to control shake animation

    private void Awake()
    {
        if (bossObject != null)
        {
            bossScript = bossObject.GetComponent<ILimbDamageReceiver>();
            if (bossScript == null)
            {
                Debug.LogWarning("The assigned GameObject does not have a component that implements ILimbDamageReceiver.");
            }
        }
        else
        {
            Debug.LogWarning("Boss object is not assigned in ColliderHitCallback.");
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
            transform.DOShakePosition(0.5f, 0.1f, 10, 90, false, true)
                .SetEase(Ease.InOutQuad); // Adjust the duration, strength, vibrato, randomness, snapping, and ease type as needed
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
        // Update the locked status visual or other game logic here
        if (lockedOnIndicator != null)
        {
            lockedOnIndicator.SetActive(isLocked);
        }
        // Additional logic for when the object is locked or unlocked can be added here
    }
}
