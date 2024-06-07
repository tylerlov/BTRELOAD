using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UltimateSpawner;

public class WaveEventSubscriptions : MonoBehaviour
{
    [SerializeField] private UnityEvent onWaveStarted;
    [SerializeField] private UnityEvent onWaveEnded;

    private WaveSpawnController waveSpawnController;
    private AdjustSongParameters adjustSongParam;
    private CinemachineCameraSwitching cameraSwitching;
    private FmodOneshots fmodOneShots;
    private ShooterMovement shooterMovement;

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Manually call OnSceneLoaded to handle the case where the scene is already loaded
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeEvents();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene Loaded: " + scene.name);
        FindAndSubscribeToComponents();
    }

    private void FindAndSubscribeToComponents()
    {
        // Attempt to find and subscribe to active instances of each component
        waveSpawnController = FindActiveInstance<WaveSpawnController>();
        adjustSongParam = FindActiveInstance<AdjustSongParameters>();
        cameraSwitching = FindActiveInstance<CinemachineCameraSwitching>();
        fmodOneShots = FindActiveInstance<FmodOneshots>();
        shooterMovement = FindActiveInstance<ShooterMovement>();

        if (waveSpawnController != null)
        {
            waveSpawnController.OnWaveStarted.AddListener(OnWaveStarted);
            waveSpawnController.OnWaveEnded.AddListener(OnWaveEnded);
        }
        else
        {
            Debug.LogWarning("Active WaveSpawnController not found in the scene!");
        }
    }

    private T FindActiveInstance<T>() where T : MonoBehaviour
    {
        T[] instances = FindObjectsOfType<T>();
        foreach (T instance in instances)
        {
            if (instance.isActiveAndEnabled)
            {
                return instance;
            }
        }
        return null;
    }

    private void UnsubscribeEvents()
    {
        if (waveSpawnController != null)
        {
            waveSpawnController.OnWaveStarted.RemoveListener(OnWaveStarted);
            waveSpawnController.OnWaveEnded.RemoveListener(OnWaveEnded);
        }
    }

    private void OnWaveStarted()
    {
        Debug.Log("Wave Started!");
        if (adjustSongParam != null)
        {
            adjustSongParam.updateStatus();
        }
        
        if (cameraSwitching != null)
        {
            cameraSwitching.SetMainCamera();
        }

        if (fmodOneShots != null)
        {
            fmodOneShots.PlayOuroborosStart();
        }

        if (shooterMovement != null)
        {
            shooterMovement.SetClamping(true);
        }

        // Invoke the UnityEvent
        onWaveStarted?.Invoke();
    }

    private void OnWaveEnded()
    {
        Debug.Log("Wave Ended!");
        if (adjustSongParam != null)
        {
            adjustSongParam.waveCounterAdd();
            adjustSongParam.updateStatus();
        }

        if (shooterMovement != null)
        {
            shooterMovement.SetClamping(false);
        }

        // Invoke the UnityEvent
        onWaveEnded?.Invoke();
    }
}
