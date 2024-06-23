using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using FMODUnity;
using Cinemachine;
using System.Collections.Generic;
using PrimeTween; // Added this using statement

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Dependencies")]
    public SceneGroup currentGroup; // Changed from SceneListData to SceneGroup
    public EnemyShootingManager enemyShootingManager;
    public StudioEventEmitter musicPlayback;

    [Header("Debug Info")]
    [SerializeField] private string currentSectionName;
    [SerializeField] private int _currentScene;
    [SerializeField] private float _currentSongSection;
    [SerializeField] private int _currWaveCount;

    public int Score { get; private set; }
    public int ShotTally { get; private set; }

    public int currentScene 
    { 
        get => _currentScene;
        private set
        {
            _currentScene = value;
            OnValueChanged();
        }
    }
    public float currentSongSection 
    { 
        get => _currentSongSection;
        private set
        {
            _currentSongSection = value;
            OnValueChanged();
        }
    }
    public int currWaveCount
    {
        get => _currWaveCount;
        set
        {
            _currWaveCount = value;
            OnValueChanged();
        }
    }

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
        PrimeTweenConfig.SetTweensCapacity(1600); // Added this line to increase tweens capacity
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

    private void Start()
    {
        InitializeNextSceneValues();
    }

    private void InitializeNextSceneValues()
    {
        if (currentGroup == null || currentGroup.scenes.Length == 0)
        {
            Debug.LogError("CurrentGroup is null or has no scenes.");
            return;
        }

        // Set next scene index to the second scene (index 1) or wrap around if there's only one scene
        nextSceneIndex = currentGroup.scenes.Length > 1 ? 1 : 0;

        // Set next song section to the first section of the next scene
        if (currentGroup.scenes[nextSceneIndex].songSections.Length > 0)
        {
            nextSongSection = currentGroup.scenes[nextSceneIndex].songSections[0].section;
        }
        else
        {
            Debug.LogWarning($"No song sections defined for the next scene (index: {nextSceneIndex})");
            nextSongSection = 0;
        }

        Debug.Log($"Initialized next scene values: Scene Index = {nextSceneIndex}, Song Section = {nextSongSection}");
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
        
        currentScene = nextSceneIndex;
        currentSongSection = nextSongSection;
        
        UpdateSceneAttributes();
        ApplyMusicChanges();

        LogCurrentState();

        GlobalVolumeManager.Instance.TransitionEffectIn(1.5f); // Added this line
        GlobalVolumeManager.Instance.TransitionEffectOut(1.5f);
    }

    public void ChangeToNextScene()
    {
        ChangeSceneWithTransitionToNext();
    }

    public void ChangeSceneWithTransitionToNext()
    {
        if (_isLoadingScene) return;
        _isLoadingScene = true;

        // Stop all active tweens
        Tween.StopAll();

        enemyShootingManager?.UnregisterAllStaticEnemyShootingsFromKoreographer();
        KillAllEnemies();
        KillAllProjectiles();

        // Calculate the correct next scene index
        int correctNextSceneIndex = (currentScene + 1) % currentGroup.scenes.Length;
        
        // Find the next valid scene with defined songSections
        while (correctNextSceneIndex != currentScene && 
               (currentGroup.scenes[correctNextSceneIndex].songSections == null || 
                currentGroup.scenes[correctNextSceneIndex].songSections.Length == 0))
        {
            correctNextSceneIndex = (correctNextSceneIndex + 1) % currentGroup.scenes.Length;
        }

        // If we've looped back to the current scene, there are no valid next scenes
        if (correctNextSceneIndex == currentScene)
        {
            Debug.LogWarning("No valid next scene found. Staying on current scene.");
            _isLoadingScene = false;
            return;
        }

        // Get the correct next song section
        float correctNextSongSection = 0;
        if (currentGroup.scenes[correctNextSceneIndex].songSections.Length > 0)
        {
            correctNextSongSection = currentGroup.scenes[correctNextSceneIndex].songSections[0].section;
        }

        string nextSceneName = currentGroup.scenes[correctNextSceneIndex].sceneName;

        // Update the next values
        nextSceneIndex = correctNextSceneIndex;
        nextSongSection = correctNextSongSection;

        // Load the scene with correct next values
        GlobalVolumeManager.Instance.TransitionEffectIn(1.5f); // Added this line
        LoadScene(nextSceneName, correctNextSongSection);
    }

    private void LoadScene(string sceneName, float songSection)
    {
        SceneManager.LoadScene(sceneName);
        _isLoadingScene = false;
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

            if (sceneData.songSections.Length > 0)
            {
                int sectionIndex = Mathf.Clamp((int)currentSongSection, 0, sceneData.songSections.Length - 1);
                var songSection = sceneData.songSections[sectionIndex];
                currWaveCount = songSection.waves;
            }
        }
    }

    private void ApplyMusicChanges()
    {
        if (musicPlayback != null && currentGroup != null && 
            currentScene < currentGroup.scenes.Length)
        {
            float sectionValue = currentSongSection;
            musicPlayback.SetParameter("Sections", sectionValue);
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
            ProjectileManager.Instance.ReturnProjectileToPool(projectile);
        }
    }

    private void LogCurrentState()
    {
        Debug.Log($"Current State - Scene: {currentScene}, Section: {currentSongSection}, Wave: {currWaveCount}");
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
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
        // Check if we're within bounds
        if (currentScene >= currentGroup.scenes.Length)
        {
            Debug.LogWarning($"Current scene index {currentScene} is out of bounds. Resetting to 0.");
            currentScene = 0;
            currentSongSection = 0;
            return;
        }

        // Get the current scene
        var currentSceneData = currentGroup.scenes[currentScene];

        // Check if the scene has any song sections
        if (currentSceneData.songSections == null || currentSceneData.songSections.Length == 0)
        {
            Debug.Log($"Scene {currentSceneData.sceneName} has no song sections. Moving to next scene.");
            MoveToNextScene();
            return;
        }

        // Ensure currentSongSection is within bounds
        int currentSectionIndex = Mathf.Clamp((int)currentSongSection, 0, currentSceneData.songSections.Length - 1);
        var currentSection = currentSceneData.songSections[currentSectionIndex];

        // If the current section has no waves, move to the next section or scene
        if (currentSection.waves == 0)
        {
            Debug.Log($"Section {currentSection.name} has no waves. Moving to next section or scene.");
            MoveToNextSectionOrScene();
            return;
        }

        // Check if we've completed all waves in the current section
        if (currWaveCount >= currentSection.waves)
        {
            Debug.Log($"Completed all waves in section {currentSection.name}. Moving to next section or scene.");
            MoveToNextSectionOrScene();
        }
    }

    private void MoveToNextSectionOrScene()
    {
        ClearAllProjectiles(); // Add this line to clear projectiles before moving to the next section or scene
        currentSongSection++;
        if ((int)currentSongSection >= currentGroup.scenes[currentScene].songSections.Length)
        {
            MoveToNextScene();
        }
        else
        {
            changeSongSection();
        }
        currWaveCount = 0;
    }

    private void MoveToNextScene()
    {
        ClearAllProjectiles(); // Add this line to clear projectiles before moving to the next scene
        currentScene++;
        if (currentScene >= currentGroup.scenes.Length)
        {
            currentScene = 0; // Loop back to the first scene
        }
        currentSongSection = 0;
        changeSongSection();
    }

    public void ClearAllProjectiles()
    {
        if (ProjectileManager.Instance != null)
        {
            ProjectileManager.Instance.ClearAllProjectiles();
        }
        else
        {
            Debug.LogError("ProjectileManager instance not found.");
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

    private void OnValueChanged()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    #endregion
}
