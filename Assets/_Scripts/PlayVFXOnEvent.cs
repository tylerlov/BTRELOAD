using UnityEngine;
using UnityEngine.VFX;

public class PlayVFXOnEvent : MonoBehaviour
{
    [SerializeField] private string eventName = "OnWaveEnded";
    [SerializeField] private VisualEffect visualEffect;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogWarning("Event name is not set in the inspector.", this);
            return;
        }

        if (visualEffect == null)
        {
            visualEffect = GetComponent<VisualEffect>();
            if (visualEffect == null)
            {
                Debug.LogError("No VisualEffect component found on this GameObject or assigned in the inspector.", this);
                return;
            }
        }

        EventManager.Instance.AddListener(eventName, PlayVFX);
    }

    private void OnDisable()
    {
        EventManager.Instance.RemoveListener(eventName, PlayVFX);
    }

    private void PlayVFX()
    {
        visualEffect.Play();
    }
}