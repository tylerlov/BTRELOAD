using UnityEngine;
using UnityEngine.Events;
using FMODUnity;
using Cinemachine; // Ensure you have this namespace to access Cinemachine classes
using UnityEngine.SceneManagement; // Required for listening to scene changes

public class AdjustSongParameters : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private SongArrangement _songArrangement;

    [Space]
    public int currWaveCount;
    public int currentSection;
    [Space]
    private StudioEventEmitter musicPlayback;

    private UnityEvent transCamOn;
    private UnityEvent transCamOff;
    private UnityEvent StartingTransition;

    private CinemachineStateDrivenCamera stateDrivenCamera; // Reference to the State Driven Camera

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeListenersAndComponents();
    }

    private void InitializeListenersAndComponents()
    {
        transCamOn = new UnityEvent();
        transCamOff = new UnityEvent();
        StartingTransition = new UnityEvent();

        musicPlayback = GetComponent<StudioEventEmitter>();
        currWaveCount = 0;
        currentSection = 0;

        var cameraSwitching = FindObjectOfType<CinemachineCameraSwitching>();
        if (cameraSwitching != null)
        {
            transCamOn.AddListener(cameraSwitching.SwitchToTransitionCamera);
            StartingTransition.AddListener(cameraSwitching.SwitchToTransitionCamera);
        }
        else
        {
            Debug.LogError("CinemachineCameraSwitching component not found in the scene.");
        }

        var crosshair = FindObjectOfType<Crosshair>();
        if (crosshair != null)
        {
            transCamOn.AddListener(crosshair.ReleasePlayerLocks);
        }
        else
        {
            Debug.LogError("Crosshair component not found in the scene.");
        }

        var splineManager = GameObject.Find("PlayerPlane")?.GetComponent<SplineManager>();
        if (splineManager != null)
        {
            transCamOn.AddListener(splineManager.IncrementSpline);
            transCamOff.AddListener(splineManager.IncrementSpline);
        }
        else
        {
            Debug.LogError("SplineManager component not found on the PlayerPlane GameObject.");
        }

        var shooterMovement = FindObjectOfType<ShooterMovement>();
        if (shooterMovement != null)
        {
            transCamOff.AddListener(shooterMovement.ResetToCenter);
        }
        else
        {
            Debug.LogError("ShooterMovement component not found in the scene for transCamOff.");
        }

        stateDrivenCamera = FindObjectOfType<CinemachineStateDrivenCamera>();
        if (stateDrivenCamera == null)
        {
            Debug.LogError("No Cinemachine State Driven Camera found in the scene.");
        }

        StartingTransition.Invoke();
    }

    void Start()
    {
        InitializeListenersAndComponents();
    }

    public void updateStatus()
    {
        if (currWaveCount >= _songArrangement.sections[currentSection].waves)
        {
            currentSection++;
            changeSongSection();
            currWaveCount = 0;
        }
    }

    public void changeSongSection()
    {
        if (musicPlayback == null || _songArrangement == null || _songArrangement.sections == null || currentSection >= _songArrangement.sections.Length)
        {
            Debug.LogWarning("One or more references are null in changeSongSection, or currentSection is out of bounds.");
            return;
        }
        musicPlayback.EventInstance.setParameterByName("Sections", _songArrangement.sections[currentSection].section);

        if (_songArrangement.sections[currentSection].waves == 0)
        {
            transCamOn?.Invoke();
        }
    }

    public void waveCounterAdd()
    {
        currWaveCount++;
    }

    public void ChangeMusicSectionByName(string sectionName)
    {
        for (int i = 0; i < _songArrangement.sections.Length; i++)
        {
            if (_songArrangement.sections[i].name == sectionName)
            {
                currentSection = i;
                changeSongSection();
                break;
            }
        }
    }
}
