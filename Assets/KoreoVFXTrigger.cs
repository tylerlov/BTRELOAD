using UnityEngine;
using SonicBloom.Koreo;
using UnityEngine.VFX;

public class KoreoVFXTrigger : MonoBehaviour
{
    [SerializeField, EventID] private string eventID; // Event ID to listen for
    private VisualEffect vfxGraph; // Reference to the Visual Effect Graph component

    void Awake()
    {
        // Find the VisualEffect component on the same GameObject
        vfxGraph = GetComponent<VisualEffect>();
        if (vfxGraph == null)
        {
            Debug.LogError("[KoreoVFXTrigger] No VisualEffect found on the GameObject.");
        }
    }

    void OnEnable()
    {
        // Register for Koreographer events
        Koreographer.Instance.RegisterForEvents(eventID, TriggerVFX);
    }

    void OnDisable()
    {
        // Unregister from Koreographer events
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.UnregisterForEvents(eventID, TriggerVFX);
        }
    }

    void TriggerVFX(KoreographyEvent evt)
    {
        if (vfxGraph != null)
        {
            vfxGraph.Play(); // Play the Visual Effect Graph effect
        }
        else
        {
            Debug.LogWarning("[KoreoVFXTrigger] VisualEffect is not assigned or found.");
        }
    }
}

