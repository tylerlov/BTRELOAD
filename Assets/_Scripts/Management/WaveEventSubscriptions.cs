using UltimateSpawner;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class WaveEventSubscriptions : MonoBehaviour
{
    [SerializeField]
    private UnityEvent onWaveStarted;

    [SerializeField]
    private UnityEvent onWaveEnded;

    [SerializeField]
    private SplineManager splineManager;

    private WaveSpawnController waveSpawnController;
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
        cameraSwitching = FindActiveInstance<CinemachineCameraSwitching>();
        fmodOneShots = FindActiveInstance<FmodOneshots>();
        shooterMovement = FindActiveInstance<ShooterMovement>();

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

    private T FindActiveInstance<T>()
        where T : MonoBehaviour
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
        Debug.Log($"<color=blue>[WAVE] Wave Started! Current Scene: {SceneManagerBTR.Instance.GetCurrentSceneName()}, Section: {SceneManagerBTR.Instance.GetCurrentSongSectionName()}</color>");
        
        // Notify ProjectileManager of wave start
        if (ProjectileManager.Instance != null)
        {
            ProjectileManager.Instance.OnWaveStart();
        }
        
        SceneManagerBTR.Instance.updateStatus("wavestart");

        // Clear all player locks at the start of a wave
        GameManager.Instance.ClearAllPlayerLocks();

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

        PlayerLocking.Instance.OnNewWaveOrAreaTransition();

        // Increment spline at the start of a wave
        if (splineManager != null)
        {
            Debug.Log($"<color=blue>[WAVE] Calling IncrementSpline from OnWaveStarted</color>");
            splineManager.IncrementSpline();
        }

        // Invoke the UnityEvent
        onWaveStarted?.Invoke();
    }

    private void OnWaveEnded()
    {
        Debug.Log($"<color=blue>[WAVE] Wave Ended! Current Scene: {SceneManagerBTR.Instance.GetCurrentSceneName()}, Section: {SceneManagerBTR.Instance.GetCurrentSongSectionName()}</color>");
        
        // Clean up projectiles at wave end
        if (ProjectileManager.Instance != null)
        {
            ProjectileManager.Instance.OnWaveEnd();
        }
        
        ScoreManager.Instance.waveCounterAdd();
        SceneManagerBTR.Instance.updateStatus("waveend");

        // Clear all player locks at the end of a wave
        GameManager.Instance.ClearAllPlayerLocks();

        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerEvent("OnWaveEnd");
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
        GameManager.Instance.RegisterEnemy(enemyTransform);
    }
}
