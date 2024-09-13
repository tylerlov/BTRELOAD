using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;
using static AsyncOperationExtensions;

public class SceneManagerBTR : MonoBehaviour
{
    public static SceneManagerBTR Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public SceneGroup currentGroup;

    [SerializeField]
    private string baseSceneName;
    private Scene baseScene;
    private Scene currentAdditiveScene;
    private bool _isLoadingScene = false;
    private int currentSceneIndex;
    private int currentSectionIndex;
    private SplineManager splineManager;
    private bool isTransitioning = false;
    private int expectedWaves = 0;
    private int completedWaves = 0;
    private bool isFirstUpdate = true;
    private int currentWaveCount; // Add this line to declare the variable

    private void Start()
    {
        InitializeNextSceneValues();
        splineManager = FindObjectOfType<SplineManager>();
        if (splineManager != null)
        {
            splineManager.OnFinalSplineReached += HandleFinalSplineReached;
        }
    }

    private void OnDisable()
    {
        if (splineManager != null)
        {
            splineManager.OnFinalSplineReached -= HandleFinalSplineReached;
        }
    }

    public async Task LoadBaseSceneAsync()
    {
        if (string.IsNullOrEmpty(baseSceneName))
        {
            Debug.LogError("Base scene name is not set in the inspector.");
            return;
        }

        baseScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(baseSceneName);
        if (!baseScene.isLoaded)
        {
            await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                baseSceneName,
                LoadSceneMode.Single
            );
        }
    }

    public async Task InitializeScenes()
    {
        await LoadBaseSceneAsync();

        if (!await TryUseOpenOuroborosSceneAsync())
        {
            currentSceneIndex = 0;
            currentSectionIndex = 0;
            await LoadFirstAdditiveSceneAsync();
        }

        currentWaveCount = 0;
        isFirstUpdate = true;

        // Play initial music (Start section)
        UpdateMusicSection();
    }

    public async Task LoadFirstAdditiveSceneAsync()
    {
        if (currentGroup == null || currentGroup.scenes.Length == 0)
        {
            Debug.LogError("No scenes defined in the current group.");
            return;
        }

        string firstSceneName = currentGroup.scenes[0].sceneName;
        await LoadAdditiveSceneAsync(firstSceneName);
    }

    public async Task LoadAdditiveSceneAsync(string sceneName)
    {
        Debug.Log(
            $"<color=cyan>[SCENE] LoadAdditiveSceneAsync called for scene: {sceneName}</color>"
        );
        if (currentAdditiveScene.IsValid() && currentAdditiveScene.isLoaded)
        {
            Debug.Log(
                $"<color=cyan>[SCENE] Unloading current additive scene: {currentAdditiveScene.name}</color>"
            );
            await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentAdditiveScene);
        }

        try
        {
            Debug.Log($"<color=cyan>[SCENE] Starting to load additive scene: {sceneName}</color>");
            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Additive
            );
            await asyncLoad;

            currentAdditiveScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(
                sceneName
            );
            if (!currentAdditiveScene.IsValid())
            {
                throw new System.Exception($"Failed to load scene: {sceneName}");
            }

            Debug.Log(
                $"<color=cyan>[SCENE] Successfully loaded additive scene: {sceneName}</color>"
            );
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(currentAdditiveScene);

            ScoreManager.Instance.CurrentSceneWaveCount = 0;

            OnSceneLoaded(currentAdditiveScene, LoadSceneMode.Additive);
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"<color=red>[SCENE] Error loading additive scene {sceneName}: {e.Message}</color>"
            );
            Debug.LogException(e);
            throw;
        }
    }

    public void updateStatus(string waveEvent)
    {
        Debug.Log($"<color=cyan>[SCENE] updateStatus called with event: {waveEvent}</color>");
        if (isTransitioning)
        {
            Debug.Log(
                "<color=yellow>[SCENE] Skipping updateStatus due to ongoing transition</color>"
            );
            return;
        }

        var currentScene = currentGroup.scenes[currentSceneIndex];
        var currentSection = currentScene.songSections[currentSectionIndex];

        Debug.Log(
            $"<color=cyan>[SCENE] Update Status: {waveEvent} - Scene: {currentSceneIndex}, Section: {currentSectionIndex} ({currentSection.name}), Completed Waves: {completedWaves}, Expected Waves: {expectedWaves}</color>"
        );

        if (isFirstUpdate && waveEvent == "wavestart")
        {
            Debug.Log("<color=cyan>[SCENE] First update detected, moving to next section</color>");
            isFirstUpdate = false;
            MoveToNextSection();
            return;
        }

        if (expectedWaves == 0)
        {
            Debug.Log("<color=cyan>[SCENE] Expected waves is 0, moving to next section</color>");
            MoveToNextSection();
        }
        else
        {
            if (waveEvent == "wavestart")
            {
                Debug.Log("<color=cyan>[SCENE] Wave start event, no action taken</color>");
            }
            else if (waveEvent == "waveend")
            {
                completedWaves++;
                Debug.Log(
                    $"<color=cyan>[SCENE] Wave end event, completed waves: {completedWaves}</color>"
                );
                if (completedWaves >= expectedWaves)
                {
                    Debug.Log(
                        "<color=cyan>[SCENE] All waves completed, moving to next section</color>"
                    );
                    MoveToNextSection();
                }
            }
        }

        Debug.Log(
            $"<color=cyan>[SCENE] After Update - Scene: {currentSceneIndex}, Section: {currentSectionIndex}, Completed Waves: {completedWaves}, Expected Waves: {expectedWaves}</color>"
        );

        if (
            currentSectionIndex == currentScene.songSections.Length - 1
            && completedWaves >= expectedWaves
        )
        {
            Debug.Log("<color=cyan>[SCENE] Final section completed, moving to next scene</color>");
            MoveToNextScene();
        }
    }

    private void MoveToNextSection()
    {
        Debug.Log("<color=cyan>[SCENE] MoveToNextSection called</color>");
        currentSectionIndex++;
        completedWaves = 0;

        var currentScene = currentGroup.scenes[currentSceneIndex];
        if (currentSectionIndex >= currentScene.songSections.Length)
        {
            Debug.Log(
                $"<color=cyan>[SCENE] End of sections reached for scene {currentSceneIndex}. Attempting to move to next scene.</color>"
            );
            MoveToNextScene();
        }
        else
        {
            expectedWaves = currentScene.songSections[currentSectionIndex].waves;
            UpdateMusicSection();
            Debug.Log(
                $"<color=cyan>[SCENE] Moved to next section. Scene: {currentSceneIndex}, Section: {currentSectionIndex} ({currentScene.songSections[currentSectionIndex].name}), Expected Waves: {expectedWaves}</color>"
            );
        }
    }

    private async Task MoveToNextScene()
    {
        Debug.Log("<color=cyan>[SCENE] MoveToNextScene called</color>");
        if (isTransitioning)
        {
            Debug.LogWarning(
                "<color=orange>[SCENE] Already transitioning to next scene. Aborting.</color>"
            );
            return;
        }

        isTransitioning = true;

        try
        {
            await CleanupCurrentScene();

            int initialSceneIndex = currentSceneIndex;
            currentSceneIndex = (currentSceneIndex + 1) % currentGroup.scenes.Length;
            Debug.Log(
                $"<color=cyan>[SCENE] Moving to next scene. Current index: {initialSceneIndex}, New index: {currentSceneIndex}</color>"
            );

            if (currentSceneIndex == initialSceneIndex)
            {
                Debug.LogWarning(
                    "<color=orange>[SCENE] Cycled through all scenes, returning to the first scene.</color>"
                );
            }

            currentSectionIndex = 0;
            completedWaves = 0;

            string nextSceneName = currentGroup.scenes[currentSceneIndex].sceneName;
            Debug.Log(
                $"<color=cyan>[SCENE] Attempting to load next scene: {nextSceneName}</color>"
            );

            if (!string.IsNullOrEmpty(nextSceneName))
            {
                await LoadAdditiveSceneAsync(nextSceneName);
            }
            else
            {
                Debug.LogError(
                    $"<color=red>[SCENE] Next scene name is null or empty. Scene index: {currentSceneIndex}</color>"
                );
            }

            expectedWaves = currentGroup
                .scenes[currentSceneIndex]
                .songSections[currentSectionIndex]
                .waves;
            UpdateMusicSection();

            Debug.Log(
                $"<color=cyan>[SCENE] Moved to next scene. Scene: {currentSceneIndex}, Section: {currentSectionIndex} ({currentGroup.scenes[currentSceneIndex].songSections[currentSectionIndex].name}), Expected Waves: {expectedWaves}</color>"
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"<color=red>[SCENE] Error during scene transition: {e.Message}</color>"
            );
            Debug.LogException(e);
        }
        finally
        {
            isTransitioning = false;
            Debug.Log("<color=cyan>[SCENE] Scene transition completed</color>");
        }
    }

    private async Task CleanupCurrentScene()
    {
        // Stop all coroutines
        StopAllCoroutines();

        // Unsubscribe from all events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.UnsubscribeFromAllEvents(this);
        }
        else
        {
            Debug.LogWarning("EventManager.Instance is null. Unable to unsubscribe from events.");
        }

        // Destroy all enemies and other scene-specific objects
        var enemies = FindObjectsOfType<EnemyBase>();
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.gameObject != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        // Wait for a frame to ensure all destructions are processed
        await Task.Yield();
    }

    private void HandleFinalSplineReached()
    {
        if (!isTransitioning)
        {
            Debug.Log(
                "<color=cyan>[SCENE] Final spline reached. Transitioning to next scene.</color>"
            );
            MoveToNextScene();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene != currentAdditiveScene)
            return;

        _isLoadingScene = false;

        UpdateSceneAttributes();
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.ApplyMusicChanges(
                currentGroup,
                currentSceneIndex,
                currentGroup.scenes[currentSceneIndex].songSections[currentSectionIndex].section
            );
        }

        LogCurrentState();

        GlobalVolumeManager.Instance.TransitionEffectIn(1.5f);
        GlobalVolumeManager.Instance.TransitionEffectOut(1.5f);

        GameManager.Instance.InitializeListenersAndComponents(scene, mode);
    }

    private void UpdateSceneAttributes()
    {
        if (currentGroup != null && currentSceneIndex < currentGroup.scenes.Length)
        {
            var sceneData = currentGroup.scenes[currentSceneIndex];
            if (sceneData.songSections.Length > 0)
            {
                var songSection = sceneData.songSections[currentSectionIndex];
                ScoreManager.Instance.CurrentSceneWaveCount = songSection.waves;
            }
        }
    }

    private void LogCurrentState()
    {
        Debug.Log(
            $"<color=cyan>[SCENE] Current State - Scene: {currentSceneIndex}, Section: {currentSectionIndex}, Wave: {ScoreManager.Instance.CurrentSceneWaveCount}</color>"
        );
    }

    public void InitializeNextSceneValues()
    {
        if (currentGroup == null || currentGroup.scenes.Length == 0)
        {
            Debug.LogError("CurrentGroup is null or has no scenes.");
            return;
        }
        // Add any additional initialization logic here if needed
    }

    public int GetCurrentWaveCount()
    {
        if (currentGroup != null && currentSceneIndex < currentGroup.scenes.Length)
        {
            var sceneData = currentGroup.scenes[currentSceneIndex];
            if (sceneData.songSections.Length > 0)
            {
                return sceneData.songSections[currentSectionIndex].waves;
            }
        }
        return 0;
    }

    public void RestartGame()
    {
        currentSceneIndex = 0;
        currentSectionIndex = 0;
        LoadFirstAdditiveSceneAsync();
    }

    public async Task UnloadAllAdditiveScenes()
    {
        int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (
                scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                && scene.name != baseSceneName
            )
            {
                await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            }
        }
    }

    private async Task<bool> TryUseOpenOuroborosSceneAsync()
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            Scene openScene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (openScene.isLoaded && IsSceneInOuroborosGroup(openScene.name))
            {
                currentAdditiveScene = openScene;
                bool setActiveSuccess = UnityEngine.SceneManagement.SceneManager.SetActiveScene(
                    currentAdditiveScene
                );

                if (!setActiveSuccess)
                {
                    Debug.LogWarning(
                        $"<color=orange>[SCENE] Failed to set {currentAdditiveScene.name} as active scene.</color>"
                    );
                    continue;
                }

                for (int j = 0; j < currentGroup.scenes.Length; j++)
                {
                    if (currentGroup.scenes[j].sceneName == openScene.name)
                    {
                        currentSceneIndex = j;
                        currentSectionIndex = 0;
                        break;
                    }
                }

                UpdateSceneAttributes();
                if (MusicManager.Instance != null)
                {
                    MusicManager.Instance.ApplyMusicChanges(
                        currentGroup,
                        currentSceneIndex,
                        currentGroup
                            .scenes[currentSceneIndex]
                            .songSections[currentSectionIndex]
                            .section
                    );
                }

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

    public string GetCurrentSongSectionName()
    {
        if (currentGroup != null && currentSceneIndex < currentGroup.scenes.Length)
        {
            return currentGroup.scenes[currentSceneIndex].songSections[currentSectionIndex].name;
        }
        return string.Empty;
    }

    private void UpdateMusicSection()
    {
        var currentScene = currentGroup.scenes[currentSceneIndex];
        if (currentScene.songSections.Length > 0 && MusicManager.Instance != null)
        {
            float sectionValue = currentScene.songSections[currentSectionIndex].section;
            string sectionName = currentScene.songSections[currentSectionIndex].name;
            Debug.Log(
                $"<color=cyan>[SCENE] Updating music section: Scene {currentSceneIndex}, Section {currentSectionIndex} ({sectionName}), Value {sectionValue}</color>"
            );
            MusicManager.Instance.ChangeSongSection(currentGroup, currentSceneIndex, sectionValue);
        }
    }

    public void ChangeSceneWithTransitionToNext()
    {
        _ = ChangeSceneWithTransitionToNextAsync();
    }

    public async Task ChangeSceneWithTransitionToNextAsync()
    {
        await ChangeScene(true);
    }

    public void ChangeToNextScene()
    {
        _ = ChangeToNextSceneAsync();
    }

    public async Task ChangeToNextSceneAsync()
    {
        await ChangeScene(true);
    }

    public void MoveToNextSectionOrScene()
    {
        _ = MoveToNextSectionOrSceneAsync();
    }

    public async Task MoveToNextSectionOrSceneAsync()
    {
        await ChangeScene();
    }

    private async Task ChangeScene(bool forceNextScene = false)
    {
        isTransitioning = true;

        if (
            forceNextScene
            || currentSectionIndex >= currentGroup.scenes[currentSceneIndex].songSections.Length - 1
        )
        {
            await MoveToNextScene();
        }
        else
        {
            MoveToNextSection();
        }

        isTransitioning = false;
    }

    public string GetCurrentSceneName()
    {
        return currentGroup.scenes[currentSceneIndex].sceneName;
    }
}
