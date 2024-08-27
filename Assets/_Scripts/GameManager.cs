using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Chronos;
using Cinemachine;
using FMODUnity;
using PrimeTween;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AsyncOperationExtensions
{
    public struct AsyncOperationAwaiter : INotifyCompletion
    {
        private AsyncOperation asyncOperation;

        public AsyncOperationAwaiter(AsyncOperation asyncOperation) =>
            this.asyncOperation = asyncOperation;

        public bool IsCompleted => asyncOperation.isDone;

        public void OnCompleted(Action continuation) =>
            asyncOperation.completed += _ => continuation();

        public void GetResult() { }
    }

    public static AsyncOperationAwaiter GetAwaiter(this AsyncOperation asyncOperation)
    {
        return new AsyncOperationAwaiter(asyncOperation);
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Player Reference")]
    [SerializeField]
    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;

    [SerializeField]
    private bool isPlayerInvincible = false;

    #region Serialized Fields
    [Header("Dependencies")]
    public SceneGroup currentGroup;
    public EnemyShootingManager enemyShootingManager;
    public StudioEventEmitter musicPlayback;


    [Header("Debug Info")]
    [SerializeField]
    private string currentSectionName;

    [SerializeField]
    private int _currentScene;

    [SerializeField]
    private float _currentSongSection;

    [Header("Scene Management")]
    [SerializeField]
    private string baseSceneName;

    [Header("Time Management")]
    [SerializeField]
    private Timeline timeline;

    [SerializeField]
    private float scoreDecayRate = 1f; // Points lost per second

    [Header("Wave Management")]
    [SerializeField]
    private int _totalWaveCount = 0;

    [SerializeField]
    private int _currentSceneWaveCount = 0;

    [Header("Time Control")]
    [SerializeField]
    private float defaultRewindTimeScale = -2f;

    [SerializeField]
    private float defaultRewindDuration = 0.5f;

    [SerializeField]
    private float defaultReturnToNormalDuration = 0.25f;

    [SerializeField]
    private string globalClockName = "Test"; // Allow setting the clock name in the inspector
    #endregion

    #region Properties
    public int Score { get; private set; }
    public int ShotTally { get; private set; }

    public int totalPlayerProjectilesShot = 0;
    public int playerProjectileHits = 0;

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

    public int TotalWaveCount
    {
        get => _totalWaveCount;
        private set
        {
            _totalWaveCount = value;
            OnValueChanged();
        }
    }

    public int CurrentSceneWaveCount
    {
        get => _currentSceneWaveCount;
        private set
        {
            _currentSceneWaveCount = value;
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
    private List<Transform> spawnedEnemies = new List<Transform>();
    private Dictionary<Transform, bool> lockedEnemies = new Dictionary<Transform, bool>();
    private GlobalClock globalClock;
    private FMOD.Studio.EventInstance musicEventInstance;
    private Score scoreUI;
    private DebugSettings debugSettings;
    private SplineManager splineManager;
    #endregion

    private const int SECTION_TRANSITION_SCORE_BOOST = 200;

    #region Unity Lifecycle Methods
    private void Awake()
    {
        InitializeSingleton();
        FindActiveFMODInstance();
        PrimeTweenConfig.SetTweensCapacity(1600);
        InitializeTimeline();
        InitializePlayerHealth();

        debugSettings = Resources.Load<DebugSettings>("DebugSettings");
        if (debugSettings == null)
        {
            ConditionalDebug.LogError(
                "DebugSettings asset not found. Create it in Resources folder."
            );
        }

        // Start a coroutine to initialize the global clock
        StartCoroutine(InitializeGlobalClock());
    }

    private IEnumerator InitializeGlobalClock()
    {
        // Wait for the next frame to ensure Timekeeper is initialized
        yield return null;

        try
        {
            globalClock = Timekeeper.instance.Clock(debugSettings.globalClockName);
            InitializeDebugTimeScale();
        }
        catch (ChronosException)
        {
            ConditionalDebug.LogWarning(
                $"Global clock '{debugSettings.globalClockName}' not found. Debug time scale will not be applied."
            );
        }
    }

    private void InitializePlayerHealth()
    {
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth == null)
            {
                ConditionalDebug.LogError("PlayerHealth component not found in the scene.");
            }
        }
    }

    public void SetPlayerInvincibility(bool isInvincible)
    {
        if (isPlayerInvincible != isInvincible)
        {
            isPlayerInvincible = isInvincible;
            if (playerHealth != null)
            {
                playerHealth.SetInvincibleInternal(isInvincible);
            }
            else
            {
                ConditionalDebug.LogError("PlayerHealth is not set in GameManager.");
            }
        }
    }

    public bool IsPlayerInvincible()
    {
        return isPlayerInvincible;
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

        if (splineManager != null)
        {
            splineManager.OnFinalSplineReached -= HandleFinalSplineReached;
        }
    }

    private async void Start()
    {
        InitializeNextSceneValues();
        await LoadBaseSceneAsync();

        if (!await TryUseOpenOuroborosSceneAsync())
        {
            await LoadFirstAdditiveSceneAsync();
        }

        scoreUI = FindObjectOfType<Score>();
        InitializeDebugTimeScale();

        if (splineManager == null)
        {
            splineManager = FindObjectOfType<SplineManager>();
            if (splineManager == null)
            {
                Debug.LogError("SplineManager not found in the scene!");
            }
        }

        splineManager.OnFinalSplineReached += HandleFinalSplineReached;
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

        if (globalClock != null && globalClock.localTimeScale != debugSettings.debugTimeScale)
        {
            globalClock.localTimeScale = debugSettings.debugTimeScale;
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

    private void InitializeDebugTimeScale()
    {
        if (globalClock != null)
        {
            globalClock.localTimeScale = debugSettings.debugTimeScale;
            ConditionalDebug.Log(
                $"Debug time scale set to {debugSettings.debugTimeScale} on global clock '{debugSettings.globalClockName}'"
            );
        }
        else
        {
            ConditionalDebug.LogWarning(
                "Global clock not initialized. Debug time scale not applied."
            );
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
        // Use Unity's main thread dispatcher to ensure we're on the main thread
        UnityMainThreadDispatcher
            .Instance()
            .Enqueue(() =>
            {
                transCamOn = new UnityEvent();
                transCamOff = new UnityEvent();
                StartingTransition = new UnityEvent();

                InitializeCameraSwitching();
                InitializeCrosshair();
                InitializeShooterMovement();

                stateDrivenCamera = FindObjectOfType<CinemachineStateDrivenCamera>();
                if (stateDrivenCamera == null)
                {
                    ConditionalDebug.LogError(
                        "No Cinemachine State Driven Camera found in the scene."
                    );
                }

                StartingTransition.Invoke();
            });
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
            ConditionalDebug.LogError(
                "CinemachineCameraSwitching component not found in the scene."
            );
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
            ConditionalDebug.LogError(
                "ShooterMovement component not found in the scene for transCamOff."
            );
        }
    }

    #endregion

    private async Task LoadBaseSceneAsync()
    {
        if (string.IsNullOrEmpty(baseSceneName))
        {
            ConditionalDebug.LogError("Base scene name is not set in the inspector.");
            return;
        }

        baseScene = SceneManager.GetSceneByName(baseSceneName);
        if (!baseScene.isLoaded)
        {
            await SceneManager.LoadSceneAsync(baseSceneName, LoadSceneMode.Single);
        }
    }

    private async Task LoadFirstAdditiveSceneAsync()
    {
        if (currentGroup == null || currentGroup.scenes.Length == 0)
        {
            ConditionalDebug.LogError("No scenes defined in the current group.");
            return;
        }

        string firstSceneName = currentGroup.scenes[0].sceneName;
        await LoadAdditiveSceneAsync(firstSceneName);
    }

    private async Task LoadAdditiveSceneAsync(string sceneName)
    {
        if (currentAdditiveScene.IsValid() && currentAdditiveScene.isLoaded)
        {
            await SceneManager.UnloadSceneAsync(currentAdditiveScene);
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        await asyncLoad; // This now works with our custom GetAwaiter

        currentAdditiveScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(currentAdditiveScene);

        // Reset the current scene wave count when loading a new scene
        CurrentSceneWaveCount = 0;

        OnSceneLoaded(currentAdditiveScene, LoadSceneMode.Additive);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isLoadingScene = false;

        // Only update these if we're actually changing scenes
        if (scene != currentAdditiveScene)
        {
            currentScene = nextSceneIndex;
            currentSongSection = nextSongSection;
        }

        UpdateSceneAttributes();
        ApplyMusicChanges();

        LogCurrentState();

        GlobalVolumeManager.Instance.TransitionEffectIn(1.5f);
        GlobalVolumeManager.Instance.TransitionEffectOut(1.5f);

        InitializeListenersAndComponents(scene, mode);
    }

    public async void ChangeToNextScene()
    {
        await ChangeSceneWithTransitionToNext();
    }

    public async Task ChangeSceneWithTransitionToNext()
    {
        if (_isLoadingScene)
            return;
        _isLoadingScene = true;

        Tween.StopAll();

        enemyShootingManager?.UnregisterAllStaticEnemyShootingsFromKoreographer();
        KillAllEnemies();
        KillAllProjectiles();

        int correctNextSceneIndex = (currentScene + 1) % currentGroup.scenes.Length;

        while (
            correctNextSceneIndex != currentScene
            && (
                currentGroup.scenes[correctNextSceneIndex].songSections == null
                || currentGroup.scenes[correctNextSceneIndex].songSections.Length == 0
            )
        )
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
            correctNextSongSection = currentGroup
                .scenes[correctNextSceneIndex]
                .songSections[0]
                .section;
        }

        string nextSceneName = currentGroup.scenes[correctNextSceneIndex].sceneName;

        nextSceneIndex = correctNextSceneIndex;
        nextSongSection = correctNextSongSection;

        GlobalVolumeManager.Instance.TransitionEffectIn(1.5f);
        await LoadAdditiveSceneAsync(nextSceneName);
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
                int sectionIndex = Mathf.Clamp(
                    (int)currentSongSection,
                    0,
                    sceneData.songSections.Length - 1
                );
                var songSection = sceneData.songSections[sectionIndex];
                CurrentSceneWaveCount = songSection.waves;
            }
        }
    }

    private void ApplyMusicChanges()
    {
        if (
            musicPlayback != null
            && currentGroup != null
            && currentScene < currentGroup.scenes.Length
        )
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
        ConditionalDebug.Log(
            $"Current State - Scene: {currentScene}, Section: {currentSongSection}, Wave: {CurrentSceneWaveCount}"
        );
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
            ConditionalDebug.LogWarning(
                $"Current scene index {currentScene} is out of bounds. Resetting to 0."
            );
            currentScene = 0;
            currentSongSection = 0;
            return;
        }

        var currentSceneData = currentGroup.scenes[currentScene];

        if (currentSceneData.songSections == null || currentSceneData.songSections.Length == 0)
        {
            ConditionalDebug.Log(
                $"Scene {currentSceneData.sceneName} has no song sections. Moving to next scene."
            );
            MoveToNextScene();
            return;
        }

        int currentSectionIndex = Mathf.Clamp(
            (int)currentSongSection,
            0,
            currentSceneData.songSections.Length - 1
        );
        var currentSection = currentSceneData.songSections[currentSectionIndex];

        if (currentSection.waves == 0)
        {
            ConditionalDebug.Log(
                $"Section {currentSection.name} has no waves. Moving to next section or scene."
            );
            MoveToNextSectionOrScene();
            return;
        }

        if (CurrentSceneWaveCount >= currentSection.waves)
        {
            ConditionalDebug.Log(
                $"Completed all waves in section {currentSection.name}. Moving to next section or scene."
            );
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
        CurrentSceneWaveCount = 0;
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
        if (
            musicPlayback == null
            || currentGroup == null
            || currentGroup.scenes == null
            || currentScene >= currentGroup.scenes.Length
            || (int)currentSongSection >= currentGroup.scenes[currentScene].songSections.Length
        )
        {
            ConditionalDebug.LogWarning(
                "One or more references are null in changeSongSection, or currentScene or currentSongSection is out of bounds."
            );
            return;
        }
        musicPlayback.EventInstance.setParameterByName(
            "Sections",
            currentGroup.scenes[currentScene].songSections[(int)currentSongSection].section
        );

        currentSectionName = currentGroup
            .scenes[currentScene]
            .songSections[(int)currentSongSection]
            .name;

        if (currentGroup.scenes[currentScene].songSections[(int)currentSongSection].waves == 0)
        {
            transCamOn?.Invoke();
        }

        ConditionalDebug.Log("changeSongSection called. About to reset player direction.");

        if (playerMovement != null)
        {
            ConditionalDebug.Log(
                $"Player state before PlayerDirectionForward: Rotation={playerMovement.transform.rotation.eulerAngles}, FacingDirection={playerMovement.GetPlayerFacingDirection()}"
            );
            playerMovement.PlayerDirectionForward();
            ConditionalDebug.Log(
                $"Player state after PlayerDirectionForward: Rotation={playerMovement.transform.rotation.eulerAngles}, FacingDirection={playerMovement.GetPlayerFacingDirection()}"
            );

            // Force an immediate update of the player's visuals
            playerMovement.UpdateAnimation();
        }
        else
        {
            ConditionalDebug.LogError(
                "PlayerMovement reference is null in GameManager. Cannot reset player direction."
            );
        }

        ConditionalDebug.Log($"Song section changed to: {currentSectionName}");

        // Add score boost when changing to a new section
        if ((int)currentSongSection > 0)
        {
            AddScore(SECTION_TRANSITION_SCORE_BOOST);
            if (scoreUI != null)
            {
                scoreUI.ShowSectionTransitionBoost(SECTION_TRANSITION_SCORE_BOOST);
            }
        }
    }

    public void waveCounterAdd()
    {
        CurrentSceneWaveCount++;
        TotalWaveCount++;
        ConditionalDebug.Log(
            $"Wave added. Current scene wave: {CurrentSceneWaveCount}, Total waves: {TotalWaveCount}"
        );
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

    public void LogProjectileHit(bool isPlayerShot, bool hitEnemy, string additionalInfo = "")
    {
        debugSettings.LogProjectileHit(isPlayerShot, hitEnemy);

        string message = isPlayerShot
            ? (hitEnemy ? "Player projectile hit enemy" : "Player projectile missed")
            : (hitEnemy ? "Enemy projectile hit player" : "Enemy projectile missed");

        if (!string.IsNullOrEmpty(additionalInfo))
        {
            message += $" - {additionalInfo}";
        }

        ConditionalDebug.Log($"[ProjectileHit] {message}");
    }

    public void LogProjectileExpired(bool isPlayerShot)
    {
        debugSettings.LogProjectileExpired(isPlayerShot);

        string message = isPlayerShot ? "Player projectile expired" : "Enemy projectile expired";
        ConditionalDebug.Log($"[ProjectileExpired] {message}");
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

    private async Task<bool> TryUseOpenOuroborosSceneAsync()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene openScene = SceneManager.GetSceneAt(i);
            if (openScene.isLoaded && IsSceneInOuroborosGroup(openScene.name))
            {
                currentAdditiveScene = openScene;
                bool setActiveSuccess = SceneManager.SetActiveScene(currentAdditiveScene);

                if (!setActiveSuccess)
                {
                    ConditionalDebug.LogWarning(
                        $"Failed to set {currentAdditiveScene.name} as active scene."
                    );
                    continue;
                }

                for (int j = 0; j < currentGroup.scenes.Length; j++)
                {
                    if (currentGroup.scenes[j].sceneName == openScene.name)
                    {
                        currentScene = j;
                        nextSceneIndex = j;
                        currentSongSection = currentGroup.scenes[j].songSections[0].section;
                        nextSongSection = currentSongSection;
                        break;
                    }
                }

                // Update scene attributes and apply music changes
                UpdateSceneAttributes();
                ApplyMusicChanges();

                // Call OnSceneLoaded on the main thread
                await UnityMainThreadDispatcher
                    .Instance()
                    .EnqueueAsync(() =>
                    {
                        OnSceneLoaded(currentAdditiveScene, LoadSceneMode.Additive);
                    });

                return true;
            }
        }
        return false;
    }

    private bool IsSceneInOuroborosGroup(string sceneName)
    {
        return currentGroup.scenes.Any(scene => scene.sceneName == sceneName);
    }

    public IEnumerator RewindTime(
        float rewindTimeScale = -2f,
        float rewindDuration = 0.5f,
        float returnToNormalDuration = 0.25f
    )
    {
        if (globalClock == null)
        {
            ConditionalDebug.LogError(
                "Global clock is not yet initialized in GameManager.RewindTime"
            );
            yield break;
        }

        float startTime = globalClock.time;
        globalClock.LerpTimeScale(rewindTimeScale, rewindDuration);

        // Start rewinding the music
        StartCoroutine(RewindMusic(true, rewindDuration));

        ConditionalDebug.Log(
            $"Rewinding time... Start time: {startTime}, Rewind scale: {rewindTimeScale}, Duration: {rewindDuration}"
        );

        yield return new WaitForSeconds(Mathf.Abs(rewindDuration));

        float rewoundTime = globalClock.time;
        globalClock.LerpTimeScale(1f, returnToNormalDuration);

        // Return music to normal
        StartCoroutine(RewindMusic(false, returnToNormalDuration));

        ConditionalDebug.Log(
            $"Returning to normal time... Rewound time: {rewoundTime}, Return duration: {returnToNormalDuration}"
        );

        yield return new WaitForSeconds(returnToNormalDuration);

        ConditionalDebug.Log("Rewind complete");
    }

    private IEnumerator RewindMusic(bool isRewinding, float duration)
    {
        if (musicPlayback == null || !musicPlayback.EventInstance.isValid())
        {
            ConditionalDebug.LogError("Music event instance is not valid");
            yield break;
        }

        musicEventInstance = musicPlayback.EventInstance;

        // Set the Rewind parameter
        musicEventInstance.setParameterByName("Rewind", isRewinding ? 1f : 0f);

        // Wait for the specified duration
        yield return new WaitForSeconds(duration);

        // If we were rewinding, turn off the rewind effect after the duration
        if (isRewinding)
        {
            musicEventInstance.setParameterByName("Rewind", 0f);
        }
    }

    public void StartRewindTime(
        float rewindTimeScale = -2f,
        float rewindDuration = 0.5f,
        float returnToNormalDuration = 0.25f
    )
    {
        StartCoroutine(RewindTime(rewindTimeScale, rewindDuration, returnToNormalDuration));
    }

    private GlobalClock TryGetGlobalClock()
    {
        try
        {
            return Timekeeper.instance.Clock(globalClockName);
        }
        catch (ChronosException)
        {
            return null;
        }
    }

    public void SetTimeScale(float timeScale)
    {
        if (globalClock != null)
        {
            globalClock.localTimeScale = timeScale;
            debugSettings.debugTimeScale = timeScale;
            ConditionalDebug.Log(
                $"Time scale set to {timeScale} on global clock '{debugSettings.globalClockName}'"
            );
        }
        else
        {
            ConditionalDebug.LogError("Global clock is not initialized. Cannot set time scale.");
        }
    }

    public void RegisterEnemy(Transform enemy)
    {
        if (!spawnedEnemies.Contains(enemy))
        {
            spawnedEnemies.Add(enemy);
            lockedEnemies[enemy] = false; // Initialize as unlocked
            ConditionalDebug.Log($"Enemy registered: {enemy.name}");
        }
    }

    public void SetEnemyLockState(Transform enemy, bool isLocked)
    {
        if (enemy == null)
        {
            lockedEnemies.Remove(enemy);
            return;
        }

        if (lockedEnemies.ContainsKey(enemy))
        {
            lockedEnemies[enemy] = isLocked;
            ConditionalDebug.Log($"Enemy {enemy.name} lock state set to: {isLocked}");
        }
        else
        {
            ConditionalDebug.LogWarning(
                $"Attempted to set lock state for unregistered enemy: {enemy.name}"
            );
        }
    }

    public void ClearAllEnemyLocks()
    {
        var enemiesToRemove = new List<Transform>();
        var enemyKeys = new List<Transform>(lockedEnemies.Keys);

        foreach (var enemy in enemyKeys)
        {
            if (enemy == null)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }

            SetEnemyLockState(enemy, false);
            EnemyBasicSetup enemySetup = enemy.GetComponent<EnemyBasicSetup>();
            if (enemySetup != null)
            {
                enemySetup.lockedStatus(false);
            }
        }

        foreach (var enemy in enemiesToRemove)
        {
            lockedEnemies.Remove(enemy);
        }

        ConditionalDebug.Log("All enemy locks cleared");
    }

    public void ClearSpawnedEnemies()
    {
        spawnedEnemies.Clear();
        lockedEnemies.Clear();
    }

    public void RemoveDestroyedEnemies()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null);
        var destroyedEnemies = lockedEnemies.Keys.Where(enemy => enemy == null).ToList();
        foreach (var enemy in destroyedEnemies)
        {
            lockedEnemies.Remove(enemy);
        }
    }

    public void ReportDamage(int damage)
    {
        if (scoreUI != null)
        {
            scoreUI.ReportDamage(damage);
        }
    }

    public string GetCurrentSongSectionName()
{
    if (currentGroup != null && currentScene < currentGroup.scenes.Length)
    {
        return currentGroup.scenes[currentScene].songSections[(int)currentSongSection].name;
    }
    return string.Empty; // Return empty if not valid
}

private void HandleFinalSplineReached()
    {
        Debug.Log("Final spline reached. Transitioning to next scene.");
        ChangeToNextScene();
    }
}
