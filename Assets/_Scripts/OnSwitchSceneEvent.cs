using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UltimateSpawner;

public class OnSwitchSceneEvent : MonoBehaviour
{
    [HideInInspector] public WaveSpawnController waveSpawnController;
    public UnityEvent onSwitchScene;

    private void Awake()
    {
        waveSpawnController = GetComponent<WaveSpawnController>();

        if (waveSpawnController != null)
        {
            waveSpawnController.OnWaveCustomEvent.AddListener(HandleSwitchSceneEvent);
        }
        else
        {
            Debug.LogError("WaveSpawnController component not found on the GameObject.");
        }
    }

    private void HandleSwitchSceneEvent(string eventName)
    {
        if (eventName == "Switch Scene")
        {
            onSwitchScene.Invoke();
            if (GameManager.instance != null)
            {
                GameManager.instance.ChangeSceneWithTransitionToNext();
            }
            else
            {
                Debug.LogError("GameManager instance not found.");
            }
        }
        else
        {
            Debug.Log($"Unhandled wave custom event: {eventName}");
        }
    }

    private void OnDestroy()
    {
        if (waveSpawnController != null)
        {
            waveSpawnController.OnWaveCustomEvent.RemoveListener(HandleSwitchSceneEvent);
        }
    }
}