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
            ConditionalDebug.LogError("[KoreoVFXTrigger] No VisualEffect found on the GameObject.");
        }
        else
        {
            ConditionalDebug.Log("[KoreoVFXTrigger] VisualEffect component found and ready.");
        }
    }

    void OnEnable()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.RegisterForEvents(eventID, TriggerVFX);
            ConditionalDebug.Log("[KoreoVFXTrigger] Registered for Koreographer events.");
        }
        else
        {
            ConditionalDebug.LogError("[KoreoVFXTrigger] Koreographer instance not found.");
        }
    }

    void OnDisable()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.UnregisterForEvents(eventID, TriggerVFX);
            ConditionalDebug.Log("[KoreoVFXTrigger] Unregistered from Koreographer events.");
        }
    }

    void TriggerVFX(KoreographyEvent evt)
    {
        ConditionalDebug.Log("[KoreoVFXTrigger] TriggerVFX called.");
        if (vfxGraph != null)
        {
            vfxGraph.Play();
            ConditionalDebug.Log("[KoreoVFXTrigger] VisualEffect played.");
        }
        else
        {
            ConditionalDebug.LogWarning("[KoreoVFXTrigger] VisualEffect is not assigned or found.");
        }
    }
}

