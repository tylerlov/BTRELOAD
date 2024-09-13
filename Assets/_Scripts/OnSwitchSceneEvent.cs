using System.Collections;
using System.Collections.Generic;
using UltimateSpawner;
using UnityEngine;
using UnityEngine.Events;

public class OnSwitchSceneEvent : MonoBehaviour
{
    [HideInInspector]
    public WaveSpawnController waveSpawnController;

    [System.Serializable]
    public class SwitchSceneEvent : UnityEvent { }

    [SerializeField]
    private SwitchSceneEvent onSwitchScene = new SwitchSceneEvent();

    private void Awake()
    {
        InitializeWaveSpawnController();
    }

    private void OnEnable()
    {
        ConnectToGameManager();
    }

    private void OnDisable()
    {
        DisconnectFromGameManager();
    }

    private void InitializeWaveSpawnController()
    {
        waveSpawnController = GetComponent<WaveSpawnController>();

        if (waveSpawnController != null)
        {
            waveSpawnController.OnWaveCustomEvent.AddListener(HandleSwitchSceneEvent);
        }
        else
        {
            Debug.LogError($"WaveSpawnController component not found on {gameObject.name}.");
        }
    }

    private void ConnectToGameManager()
    {
        onSwitchScene.AddListener(SceneManagerBTR.Instance.ChangeToNextScene);
        Debug.Log("Connected to GameManager and set up ChangeToNextScene method.");
    }

    private void DisconnectFromGameManager()
    {
        onSwitchScene.RemoveListener(SceneManagerBTR.Instance.ChangeToNextScene);
        Debug.Log("Disconnected from GameManager.");
    }

    private void HandleSwitchSceneEvent(string eventName)
    {
        if (eventName == "Switch Scene")
        {
            if (onSwitchScene != null)
            {
                onSwitchScene.Invoke();
                Debug.Log("onSwitchScene event invoked.");
            }
            else
            {
                Debug.LogError("onSwitchScene event is null.");
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

        DisconnectFromGameManager();
    }
}
