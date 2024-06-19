using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using FMODUnity;
using Cinemachine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Dependencies")]
    public SceneGroup currentGroup; // Changed from SceneListData to SceneGroup
    public EnemyShootingManager enemyShootingManager;
    public StudioEventEmitter musicPlayback;

    [Header("Debug Info")]
    [SerializeField] private string currentSectionName;

    public int Score { get; private set; }
    public int ShotTally { get; private set; }
    public int currWaveCount;
    public int currentScene; // Renamed from currentSection
    public float currentSongSection;

    private bool _isLoadingScene = false;
    private UnityEvent transCamOn;
    private UnityEvent transCamOff;
    private UnityEvent StartingTransition;
    private CinemachineStateDrivenCamera stateDrivenCamera;

    private int nextSceneIndex;
    private float nextSongSection;

    private void Awake()
    {
        InitializeSingleton();
        FindActiveFMODInstance();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneLoaded += InitializeListenersAndComponents;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded -= InitializeListenersAndComponents;
    }

    #region Initialization

    private void InitializeSingleton()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void FindActiveFMODInstance()
    {
        var fmodInstances = FindObjectsOfType<StudioEventEmitter>(true); // true to include inactive objects
        foreach (var instance in fmodInstances)
        {
            if (instance.gameObject.name == "FMOD Music" && instance.gameObject.activeInHierarchy)
            {
                musicPlayback = instance;
                break;
            }
        }

        if (musicPlayback == null)
        {
            Debug.LogError("Active FMOD Music instance not found in the scene.");
        }
    }

    private void InitializeListenersAndComponents(Scene scene, LoadSceneMode mode)
    {
        transCamOn = new UnityEvent();
        transCamOff = new UnityEvent();
        StartingTransition = new UnityEvent();

        currWaveCount = 0;
        currentScene = 0;
        currentSongSection = 0;

        InitializeCameraSwitching();
        InitializeCrosshair();
        InitializeSplineManager();
        InitializeShooterMovement();

        stateDrivenCamera = FindObjectOfType<CinemachineStateDrivenCamera>();
        if (stateDrivenCamera == null)
        {
            Debug.LogError("No Cinemachine State Driven Camera found in the scene.");
        }

        StartingTransition.Invoke();
    }

    private void InitializeCameraSwitching()
    {
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
    }

    private void InitializeCrosshair()
    {
        var crosshair = FindObjectOfType<Crosshair>();
        if (crosshair != null)
        {
            transCamOn.AddListener(crosshair.ReleasePlayerLocks);
        }
        else
        {
            Debug.LogError("Crosshair component not found in the scene.");
        }
    }

    private void InitializeSplineManager()
    {
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
    }

    private void InitializeShooterMovement()
    {
        var shooterMovement = FindObjectOfType<ShooterMovement>();
        if (shooterMovement != null)
        {
            transCamOff.AddListener(shooterMovement.ResetToCenter);
        }
        else
        {
            Debug.LogError("ShooterMovement component not found in the scene for transCamOff.");
        }
    }

    #endregion

    #region Scene Management

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isLoadingScene = false;
        Debug.Log($"Scene {scene.name} loaded.");
        Debug.Log($"Total scenes in build settings: {SceneManager.sceneCountInBuildSettings}");

        if (scene.buildIndex < 0 || scene.buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"Scene index {scene.buildIndex} is out of range.");
            return;
        }

        ProjectileManager.Instance?.ReRegisterEnemiesAndProjectiles();
        Crosshair.Instance?.ClearLockedTargets();

        // Update scene attributes for the newly loaded scene
        UpdateSceneAttributes();

        // Now update the current scene and song section to the next values
        currentScene = nextSceneIndex;
        currentSongSection = nextSongSection;

        // Update scene attributes for the newly loaded scene
        UpdateSceneAttributes();
    }

    public void ChangeToNextScene()
    {
        ChangeSceneWithTransitionToNext();
    }

    public void ChangeSceneWithTransitionToNext()
    {
        if (_isLoadingScene) return;
        _isLoadingScene = true;

        enemyShootingManager?.UnregisterAllStaticEnemyShootingsFromKoreographer();
        KillAllEnemies();
        KillAllProjectiles();

        string currentSceneName = SceneManager.GetActiveScene().name;
        int currentSceneIndex = -1;

        FindCurrentSceneIndex(currentSceneName, out currentSceneIndex);

        if (currentSceneIndex != -1)
        {
            nextSceneIndex = (currentSceneIndex + 1) % currentGroup.scenes.Length;
            string nextSceneName = currentGroup.scenes[nextSceneIndex].sceneName;

            // Ensure there are song sections defined and set the nextSongSection correctly
            if (currentGroup.scenes[nextSceneIndex].songSections.Length > 0)
            {
                nextSongSection = currentGroup.scenes[nextSceneIndex].songSections[0].section;

                float musicSectionValue = currentGroup.scenes[nextSceneIndex].songSections[0].section;
                LoadScene(nextSceneName, musicSectionValue);
            }
            else
            {
                Debug.LogError("No song sections defined for the next scene.");
                _isLoadingScene = false;
            }
        }
        else
        {
            Debug.LogError("Current scene not found in SceneGroup.");
            _isLoadingScene = false;
        }
    }

    private void LoadScene(string sceneName, float sectionValue)
    {
        SceneManager.LoadScene(sceneName);
        musicPlayback.SetParameter("Section", sectionValue);
        _isLoadingScene = false;

        // Update scene attributes after the scene is loaded
        UpdateSceneAttributes();
    }

    private void FindCurrentSceneIndex(string currentSceneName, out int sceneIndex)
    {
        // Initialize to -1 to indicate the scene was not found
        sceneIndex = -1;

        for (int i = 0; i < currentGroup.scenes.Length; i++)
        {
            if (currentGroup.scenes[i].sceneName == currentSceneName)
            {
                sceneIndex = i;
                return;
            }
        }
    }

    private void UpdateSceneAttributes()
    {
        if (currentGroup != null && currentScene < currentGroup.scenes.Length)
        {
            var sceneData = currentGroup.scenes[currentScene];
            currentSectionName = sceneData.sceneName;

            // Ensure there are song sections defined
            if (sceneData.songSections.Length > 0)
            {
                // Ensure currentSongSection is within the bounds of songSections array
                if (currentSongSection < sceneData.songSections.Length)
                {
                    var songSection = sceneData.songSections[(int)currentSongSection];
                    currWaveCount = songSection.waves;
                    musicPlayback.SetParameter("Section", songSection.section);

                    // Debug logs to trace the issue
                    Debug.Log($"Updated Scene Attributes: Scene Name = {currentSectionName}, Section = {songSection.section}, Waves = {currWaveCount}");
                }
                else
                {
                    Debug.LogError("currentSongSection is out of bounds.");
                    currentSongSection = 0; // Reset to a valid value
                }
            }
            else
            {
                Debug.LogError("No song sections defined for the current scene.");
            }
        }
        else
        {
            Debug.LogError("currentGroup is null or currentScene is out of bounds.");
        }
    }

    private void KillAllEnemies()
    {
        var enemies = FindObjectsOfType<EnemyBasicSetup>();
        foreach (var enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }
    }

    private void KillAllProjectiles()
    {
        var projectiles = FindObjectsOfType<ProjectileStateBased>();
        foreach (var projectile in projectiles)
        {
            Destroy(projectile.gameObject);
        }
    }

    #endregion

    #region Score Management

    public void AddShotTally(int shotsToAdd)
    {
        ShotTally += shotsToAdd;
        // Optionally, update UI or other game elements here
    }

    public void SetScore(int newScore)
    {
        Score = newScore;
        // Optionally, update UI or other game elements here
    }

    public void AddScore(int amount)
    {
        Score += amount;
        // Update UI or other systems as needed
    }

    public void ResetScore()
    {
        Score = 0;
        // Update UI or other systems as needed
    }

    public int RetrieveScore()
    {
        // Add any additional logic here if needed before returning the score
        return Score;
    }

    #endregion

    #region Debugging

    public void DebugMoveToNextScene()
    {
        ChangeToNextScene();
    }

    #endregion

    #region Music and Wave Management

    public void updateStatus()
    {
        if (currWaveCount >= currentGroup.scenes[currentScene].songSections[(int)currentSongSection].waves)
        {
            currentSongSection++;
            if ((int)currentSongSection >= currentGroup.scenes[currentScene].songSections.Length)
            {
                currentScene++;
                currentSongSection = 0;
            }
            changeSongSection();
            currWaveCount = 0;
        }
    }

    public void changeSongSection()
    {
        if (musicPlayback == null || currentGroup == null || currentGroup.scenes == null || currentScene >= currentGroup.scenes.Length || (int)currentSongSection >= currentGroup.scenes[currentScene].songSections.Length)
        {
            Debug.LogWarning("One or more references are null in changeSongSection, or currentScene or currentSongSection is out of bounds.");
            return;
        }
        musicPlayback.EventInstance.setParameterByName("Sections", currentGroup.scenes[currentScene].songSections[(int)currentSongSection].section);

        currentSectionName = currentGroup.scenes[currentScene].songSections[(int)currentSongSection].name;

        if (currentGroup.scenes[currentScene].songSections[(int)currentSongSection].waves == 0)
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
        for (int i = 0; i < currentGroup.scenes.Length; i++)
        {
            for (int j = 0; j < currentGroup.scenes[i].songSections.Length; j++)
            {
                if (currentGroup.scenes[i].songSections[j].name == sectionName)
                {
                    currentScene = i;
                    currentSongSection = currentGroup.scenes[i].songSections[j].section;
                    changeSongSection();
                    break;
                }
            }
        }
    }

    public void HandleDebugSceneTransition()
    {
        int wavesToSimulate = 3;
        for (int i = 0; i < wavesToSimulate; i++)
        {
            waveCounterAdd();
            updateStatus();
        }
    }

    #endregion
}
