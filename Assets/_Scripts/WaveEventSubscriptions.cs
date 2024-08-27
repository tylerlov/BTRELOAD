using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UltimateSpawner;

public class WaveEventSubscriptions : MonoBehaviour
{
    [SerializeField] private UnityEvent onWaveStarted;
    [SerializeField] private UnityEvent onWaveEnded;
    [SerializeField] private SplineManager splineManager;

    private WaveSpawnController waveSpawnController;
    private GameManager gameManager;
    private CinemachineCameraSwitching cameraSwitching;
    private FmodOneshots fmodOneShots;
    private ShooterMovement shooterMovement;
    private Crosshair crosshair;

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
        gameManager = GameManager.instance;
        cameraSwitching = FindActiveInstance<CinemachineCameraSwitching>();
        fmodOneShots = FindActiveInstance<FmodOneshots>();
        shooterMovement = FindActiveInstance<ShooterMovement>();
        crosshair = FindObjectOfType<Crosshair>();
        if (crosshair == null)
        {
            Debug.LogError("Crosshair not found in the scene!");
        }

        splineManager = FindObjectOfType<SplineManager>();
        if (splineManager == null)
        {
            Debug.LogError("SplineManager not found in the scene!");
        }

        if (waveSpawnController != null)
        {
            waveSpawnController.OnWaveStarted.AddListener(OnWaveStarted);
            waveSpawnController.OnWaveEnded.AddListener(OnWaveEnded);
            
            // Subscribe to the new OnEnemySpawned event
            waveSpawnController.OnEnemySpawned += OnEnemySpawned;
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
            
            // Unsubscribe from the OnEnemySpawned event
            waveSpawnController.OnEnemySpawned -= OnEnemySpawned;
        }
    }

    private void OnWaveStarted()
    {
        Debug.Log("Wave Started!");
        if (gameManager != null)
        {
            gameManager.updateStatus();
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

        // Clear locks when a new wave starts
        if (crosshair != null)
        {
            crosshair.OnNewWaveOrAreaTransition();
        }

        // Increment spline at the start of a wave
        if (splineManager != null)
        {
            splineManager.IncrementSpline();
        }

        // Invoke the UnityEvent
        onWaveStarted?.Invoke();
    }

    private void OnWaveEnded()
    {
        Debug.Log("Wave Ended!");
        if (gameManager != null)
        {
            gameManager.waveCounterAdd();
            gameManager.updateStatus();
        }

        if (shooterMovement != null)
        {
            shooterMovement.SetClamping(false);
        }

        // Increment spline at the end of a wave
        if (splineManager != null)
        {
            splineManager.IncrementSpline();
        }

        // Invoke the UnityEvent
        onWaveEnded?.Invoke();
    }

    // New method to handle enemy spawning
    private void OnEnemySpawned(Transform enemyTransform)
    {
        if (gameManager != null)
        {
            gameManager.RegisterEnemy(enemyTransform);
        }
    }
}