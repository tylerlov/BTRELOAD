using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using FMODUnity;
using Cinemachine;
using System.Collections.Generic;
using PrimeTween;
using Chronos;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    #region Serialized Fields
    [Header("Dependencies")]
    public SceneGroup currentGroup;
    public EnemyShootingManager enemyShootingManager;
    public StudioEventEmitter musicPlayback;

    [Header("Debug Info")]
    [SerializeField] private string currentSectionName;
    [SerializeField] private int _currentScene;
    [SerializeField] private float _currentSongSection;
    [SerializeField] private int _currWaveCount;

    [Header("Scene Management")]
    [SerializeField] private string baseSceneName;

    [Header("Time Management")]
    [SerializeField] private Timeline timeline;
    [SerializeField] private float scoreDecayRate = 1f; // Points lost per second
    #endregion

    #region Properties
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
    #endregion

    #region Private Fields
    private Scene baseScene;
    private Scene currentAdditiveScene;
    private bool _isLoadingScene = false;
    private UnityEvent transCamOn;
    private UnityEvent transCamOff;
    private UnityEvent StartingTransition;
    private CinemachineStateDrivenCamera stateDrivenCamera;
    private int nextSceneIndex;
    private float nextSongSection;
    private float lastScoreUpdateTime;
    private float accumulatedScoreDecrease = 0f;
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        InitializeSingleton();
        FindActiveFMODInstance();
        PrimeTweenConfig.SetTweensCapacity(1600);
        InitializeTimeline();
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
        LoadBaseScene();
        
        // Check for already open scene from Ouroboros group
        if (!TryUseOpenOuroborosScene())
        {
            LoadFirstAdditiveScene();
        }
    }

    private void Update()
    {
        if (timeline != null)
        {
            ConditionalDebug.Log($"Timeline time: {timeline.time}");
            UpdateScoreOverTime();
        }
        else
        {
            ConditionalDebug.LogWarning("Timeline is null in GameManager Update");
        }
    }
    #endregion

    #region Initialization Methods
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

    private void InitializeNextSceneValues()
    {
        if (currentGroup == null || currentGroup.scenes.Length == 0)
        {
            ConditionalDebug.LogError("CurrentGroup is null or has no scenes.");
            return;
        }
    }

    private void FindActiveFMODInstance()
    {
        var fmodInstances = FindObjectsOfType<StudioEventEmitter>(true);
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
            ConditionalDebug.LogError("Active FMOD Music instance not found in the scene.");
        }
    }

    private void InitializeTimeline()
    {
        timeline = GetComponent<Timeline>();
        if (timeline == null)
        {
            ConditionalDebug.LogError("Timeline component not found on the GameManager object.");
        }
        lastScoreUpdateTime = timeline.time;
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
            ConditionalDebug.LogError("No Cinemachine State Driven Camera found in the scene.");
        }

        StartingTransition.Invoke();
    }

    private void InitializeSplineManager()
    {
        // Remove the specific GameObject.Find for "PlayerPlane"
        var splineManager = FindObjectOfType<SplineManager>();
        if (splineManager != null)
        {
            transCamOn.AddListener(splineManager.IncrementSpline);
            transCamOff.AddListener(splineManager.IncrementSpline);
        }
        else
        {
            ConditionalDebug.LogWarning("SplineManager component not found in the scene. It may be loaded later.");
        }
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
            ConditionalDebug.LogError("CinemachineCameraSwitching component not found in the scene.");
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
            ConditionalDebug.LogError("Crosshair component not found in the scene.");
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
            ConditionalDebug.LogError("ShooterMovement component not found in the scene for transCamOff.");
        }
    }

    #endregion

    private void LoadBaseScene()
    {
        if (string.IsNullOrEmpty(baseSceneName))
        {
            ConditionalDebug.LogError("Base scene name is not set in the inspector.");
            return;
        }

        baseScene = SceneManager.GetSceneByName(baseSceneName);
        if (!baseScene.isLoaded)
        {
            SceneManager.LoadScene(baseSceneName, LoadSceneMode.Single);
        }
    }

    private void LoadFirstAdditiveScene()
    {
        if (currentGroup == null || currentGroup.scenes.Length == 0)
        {
            ConditionalDebug.LogError("No scenes defined in the current group.");
            return;
        }

        string firstSceneName = currentGroup.scenes[0].sceneName;
        LoadAdditiveScene(firstSceneName);
    }

    private void LoadAdditiveScene(string sceneName)
    {
        StartCoroutine(LoadAdditiveSceneCoroutine(sceneName));
    }

    private IEnumerator LoadAdditiveSceneCoroutine(string sceneName)
    {
        if (currentAdditiveScene.IsValid() && currentAdditiveScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(currentAdditiveScene);
        }

        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        currentAdditiveScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(currentAdditiveScene);

        OnSceneLoaded(currentAdditiveScene, LoadSceneMode.Additive);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isLoadingScene = false;
        
        currentScene = nextSceneIndex;
        currentSongSection = nextSongSection;
        
        UpdateSceneAttributes();
        ApplyMusicChanges();

        LogCurrentState();

        GlobalVolumeManager.Instance.TransitionEffectIn(1.5f);
        GlobalVolumeManager.Instance.TransitionEffectOut(1.5f);

        InitializeListenersAndComponents(scene, mode);

        // Add this: Re-initialize SplineManager after scene is fully loaded
        StartCoroutine(InitializeSplineManagerDelayed());
    }

    private IEnumerator InitializeSplineManagerDelayed()
    {
        yield return null; // Wait for one frame to ensure all objects are initialized

        InitializeSplineManager();
    }

    public void ChangeToNextScene()
    {
        ChangeSceneWithTransitionToNext();
    }

    public void ChangeSceneWithTransitionToNext()
    {
        if (_isLoadingScene) return;
        _isLoadingScene = true;

        Tween.StopAll();

        enemyShootingManager?.UnregisterAllStaticEnemyShootingsFromKoreographer();
        KillAllEnemies();
        KillAllProjectiles();

        int correctNextSceneIndex = (currentScene + 1) % currentGroup.scenes.Length;
        
        while (correctNextSceneIndex != currentScene && 
               (currentGroup.scenes[correctNextSceneIndex].songSections == null || 
                currentGroup.scenes[correctNextSceneIndex].songSections.Length == 0))
        {
            correctNextSceneIndex = (correctNextSceneIndex + 1) % currentGroup.scenes.Length;
        }

        if (correctNextSceneIndex == currentScene)
        {
            ConditionalDebug.LogWarning("No valid next scene found. Staying on current scene.");
            _isLoadingScene = false;
            return;
        }

        float correctNextSongSection = 0;
        if (currentGroup.scenes[correctNextSceneIndex].songSections.Length > 0)
        {
            correctNextSongSection = currentGroup.scenes[correctNextSceneIndex].songSections[0].section;
        }

        string nextSceneName = currentGroup.scenes[correctNextSceneIndex].sceneName;

        nextSceneIndex = correctNextSceneIndex;
        nextSongSection = correctNextSongSection;

        GlobalVolumeManager.Instance.TransitionEffectIn(1.5f);
        LoadAdditiveScene(nextSceneName);
    }

    private void LoadScene(string sceneName, float songSection)
    {
        SceneManager.LoadScene(sceneName);
        _isLoadingScene = false;
    }

    private void FindCurrentSceneIndex(string currentSceneName, out int sceneIndex)
    {
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
        ConditionalDebug.Log($"Current State - Scene: {currentScene}, Section: {currentSongSection}, Wave: {currWaveCount}");
    }

    public void AddShotTally(int shotsToAdd)
    {
        ShotTally += shotsToAdd;
    }

    public void SetScore(int newScore)
    {
        Score = newScore;
    }

    public void AddScore(int amount)
    {
        Score += amount;
    }

    public void ResetScore()
    {
        Score = 0;
    }

    public int RetrieveScore()
    {
        return Score;
    }

    public void DebugMoveToNextScene()
    {
        ChangeToNextScene();
    }

    public void updateStatus()
    {
        if (currentScene >= currentGroup.scenes.Length)
        {
            ConditionalDebug.LogWarning($"Current scene index {currentScene} is out of bounds. Resetting to 0.");
            currentScene = 0;
            currentSongSection = 0;
            return;
        }

        var currentSceneData = currentGroup.scenes[currentScene];

        if (currentSceneData.songSections == null || currentSceneData.songSections.Length == 0)
        {
            ConditionalDebug.Log($"Scene {currentSceneData.sceneName} has no song sections. Moving to next scene.");
            MoveToNextScene();
            return;
        }

        int currentSectionIndex = Mathf.Clamp((int)currentSongSection, 0, currentSceneData.songSections.Length - 1);
        var currentSection = currentSceneData.songSections[currentSectionIndex];

        if (currentSection.waves == 0)
        {
            ConditionalDebug.Log($"Section {currentSection.name} has no waves. Moving to next section or scene.");
            MoveToNextSectionOrScene();
            return;
        }

        if (currWaveCount >= currentSection.waves)
        {
            ConditionalDebug.Log($"Completed all waves in section {currentSection.name}. Moving to next section or scene.");
            MoveToNextSectionOrScene();
        }
    }

    private void MoveToNextSectionOrScene()
    {
        ClearAllProjectiles();
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
        ClearAllProjectiles();
        currentScene++;
        if (currentScene >= currentGroup.scenes.Length)
        {
            currentScene = 0;
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
            ConditionalDebug.LogError("ProjectileManager instance not found.");
        }
    }

    public void changeSongSection()
    {
        if (musicPlayback == null || currentGroup == null || currentGroup.scenes == null || currentScene >= currentGroup.scenes.Length || (int)currentSongSection >= currentGroup.scenes[currentScene].songSections.Length)
        {
            ConditionalDebug.LogWarning("One or more references are null in changeSongSection, or currentScene or currentSongSection is out of bounds.");
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
        // This method is intentionally left empty or you can add logic here if needed
    }

    private void UpdateScoreOverTime()
    {
        float currentTime = timeline.time;
        float deltaTime = currentTime - lastScoreUpdateTime;
        
        if (deltaTime > 0)
        {
            accumulatedScoreDecrease += scoreDecayRate * deltaTime;
            int scoreDecrease = Mathf.FloorToInt(accumulatedScoreDecrease);
            
            if (scoreDecrease > 0)
            {
                Score = Mathf.Max(0, Score - scoreDecrease);
                accumulatedScoreDecrease -= scoreDecrease;
                
                // Debug log to see when score actually decreases
                ConditionalDebug.Log($"Score decreased by {scoreDecrease}. New score: {Score}");
            }
            
            lastScoreUpdateTime = currentTime;
        }
    }

    private bool TryUseOpenOuroborosScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene openScene = SceneManager.GetSceneAt(i);
            if (openScene.isLoaded && IsSceneInOuroborosGroup(openScene.name))
            {
                currentAdditiveScene = openScene;
                SceneManager.SetActiveScene(currentAdditiveScene);
                
                // Find the index of the open scene in the Ouroboros group
                for (int j = 0; j < currentGroup.scenes.Length; j++)
                {
                    if (currentGroup.scenes[j].sceneName == openScene.name)
                    {
                        currentScene = j;
                        currentSongSection = currentGroup.scenes[j].songSections[0].section;
                        break;
                    }
                }
            
            OnSceneLoaded(currentAdditiveScene, LoadSceneMode.Additive);
            return true;
        }
    }
    return false;
}

private bool IsSceneInOuroborosGroup(string sceneName)
{
    return currentGroup.scenes.Any(scene => scene.sceneName == sceneName);
}
                
}