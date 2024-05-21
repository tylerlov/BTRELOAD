using UnityEngine;
using SonicBloom.Koreo;
using UnityEngine.VFX;

public class KoreoVFXTrigger : MonoBehaviour
{
    [SerializeField, EventID] private string eventID; // Event ID to listen for
    private VisualEffect vfxGraph; // Reference to the Visual Effect Graph component

    void Awake()
    {
        vfxGraph = GetComponent<VisualEffect>();
        if (vfxGraph == null)
        {
            Debug.LogError("[KoreoVFXTrigger] No VisualEffect found on the GameObject.");
        }
        else
        {
            Debug.Log("[KoreoVFXTrigger] VisualEffect component found and ready.");
        }
    }

    void OnEnable()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.RegisterForEvents(eventID, TriggerVFX);
            Debug.Log("[KoreoVFXTrigger] Registered for Koreographer events.");
        }
        else
        {
            Debug.LogError("[KoreoVFXTrigger] Koreographer instance not found.");
        }
    }

    void OnDisable()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.UnregisterForEvents(eventID, TriggerVFX);
            Debug.Log("[KoreoVFXTrigger] Unregistered from Koreographer events.");
        }
    }

    void TriggerVFX(KoreographyEvent evt)
    {
        Debug.Log("[KoreoVFXTrigger] TriggerVFX called.");
        if (vfxGraph != null)
        {
            vfxGraph.Play();
            Debug.Log("[KoreoVFXTrigger] VisualEffect played.");
        }
        else
        {
            Debug.LogWarning("[KoreoVFXTrigger] VisualEffect is not assigned or found.");
        }
    }
}

