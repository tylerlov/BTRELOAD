using UltimateSpawner;
using UnityEngine;

public class WaveCustomEventSorter : MonoBehaviour
{
    public WaveSpawnController waveSpawnController;

    private void Awake()
    {
        waveSpawnController = GetComponent<WaveSpawnController>();

        if (waveSpawnController != null)
        {
            waveSpawnController.OnWaveCustomEvent.AddListener(HandleCustomWaveEvent);
        }
        else
        {
            Debug.LogError("WaveSpawnController component not found on the GameObject.");
        }
    }

    private void HandleCustomWaveEvent(string eventName)
    {
        switch (eventName)
        {
            case "Switch Scene":
                SceneManagerBTR.Instance.ChangeSceneWithTransitionToNext();
                break;
            // Add more cases here for other custom events
            default:
                Debug.Log($"Unhandled wave custom event: {eventName}");
                break;
        }
    }

    private void OnDestroy()
    {
        if (waveSpawnController != null)
        {
            waveSpawnController.OnWaveCustomEvent.RemoveListener(HandleCustomWaveEvent);
        }
    }
}
