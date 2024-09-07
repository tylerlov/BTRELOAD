using UnityEngine;
using UnityEngine.InputSystem;

public class DisablePlayerFeatures : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private InputAction reverseDirectionAction;

    private void Awake()
    {
        // Find the PlayerMovement component in the scene
        playerMovement = FindObjectOfType<PlayerMovement>();

        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement not found in the scene!");
            return;
        }

        // Access the playerInputActions field using reflection
        var playerInputActionsField = typeof(PlayerMovement).GetField("playerInputActions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (playerInputActionsField != null)
        {
            var playerInputActions = playerInputActionsField.GetValue(playerMovement) as DefaultControls;
            if (playerInputActions != null)
            {
                // Disable the ReverseDirection action
                reverseDirectionAction = playerInputActions.Player.ReverseDirection;
                DisableReverseDirection();
            }
            else
            {
                Debug.LogError("Failed to access playerInputActions!");
            }
        }
        else
        {
            Debug.LogError("Failed to find playerInputActions field!");
        }
    }

    public void DisableReverseDirection()
    {
        if (reverseDirectionAction != null)
        {
            reverseDirectionAction.Disable();
            Debug.Log("Reverse direction control disabled.");
        }
    }

    public void EnableReverseDirection()
    {
        if (reverseDirectionAction != null)
        {
            reverseDirectionAction.Enable();
            Debug.Log("Reverse direction control enabled.");
        }
    }

    private void OnDestroy()
    {
        // Re-enable the control when this script is destroyed (e.g., when leaving the scene)
        EnableReverseDirection();
    }
}