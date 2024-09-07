using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AsyncOperationExtensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using PrimeTween;

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
    [SerializeField] private string baseSceneName;
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
            await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(baseSceneName, LoadSceneMode.Single);
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
        if (currentAdditiveScene.IsValid() && currentAdditiveScene.isLoaded)
        {
            await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentAdditiveScene);
        }

        try
        {
            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            await asyncLoad;

            currentAdditiveScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            if (!currentAdditiveScene.IsValid())
            {
                throw new System.Exception($"Failed to load scene: {sceneName}");
            }

            UnityEngine.SceneManagement.SceneManager.SetActiveScene(currentAdditiveScene);

            ScoreManager.Instance.CurrentSceneWaveCount = 0;

            OnSceneLoaded(currentAdditiveScene, LoadSceneMode.Additive);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading additive scene {sceneName}: {e.Message}");
            throw;
        }
    }

    public void updateStatus(string waveEvent)
    {
        if (isTransitioning) return;

        var currentScene = currentGroup.scenes[currentSceneIndex];
        var currentSection = currentScene.songSections[currentSectionIndex];

        Debug.Log($"Update Status: {waveEvent} - Scene: {currentSceneIndex}, Section: {currentSectionIndex}, Completed Waves: {completedWaves}, Expected Waves: {expectedWaves}");

        if (isFirstUpdate && waveEvent == "wavestart")
        {
            // First wave start should transition from Start to Verse 1
            isFirstUpdate = false;
            MoveToNextSection();
            return;
        }

        if (expectedWaves == 0)
        {
            // For sections with 0 waves, move to next section on any event
            MoveToNextSection();
        }
        else
        {
            if (waveEvent == "wavestart")
            {
                // Do nothing on wave start for non-zero wave sections
            }
            else if (waveEvent == "waveend")
            {
                completedWaves++;
                if (completedWaves >= expectedWaves)
                {
                    MoveToNextSection();
                }
            }
        }

        Debug.Log($"After Update - Scene: {currentSceneIndex}, Section: {currentSectionIndex}, Completed Waves: {completedWaves}, Expected Waves: {expectedWaves}");
    }

    private void MoveToNextSection()
    {
        currentSectionIndex++;
        completedWaves = 0;

        var currentScene = currentGroup.scenes[currentSceneIndex];
        if (currentSectionIndex >= currentScene.songSections.Length)
        {
            MoveToNextScene();
        }
        else
        {
            expectedWaves = currentScene.songSections[currentSectionIndex].waves;
            UpdateMusicSection();
        }

        Debug.Log($"Moved to next section. Scene: {currentSceneIndex}, Section: {currentSectionIndex}, Expected Waves: {expectedWaves}");
    }

    private async Task MoveToNextScene()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("Already transitioning to next scene. Aborting.");
            return;
        }

        isTransitioning = true;

        try
        {
            int initialSceneIndex = currentSceneIndex;
            do
            {
                currentSceneIndex = (currentSceneIndex + 1) % currentGroup.scenes.Length;
                currentSectionIndex = 0;
                completedWaves = 0;

                if (!string.IsNullOrEmpty(currentGroup.scenes[currentSceneIndex].sceneName))
                {
                    await LoadAdditiveSceneAsync(currentGroup.scenes[currentSceneIndex].sceneName);
                    break;
                }
            } while (currentSceneIndex != initialSceneIndex);

            if (currentSceneIndex == initialSceneIndex && string.IsNullOrEmpty(currentGroup.scenes[currentSceneIndex].sceneName))
            {
                Debug.LogError("No valid scenes found in the current group.");
                return;
            }

            expectedWaves = currentGroup.scenes[currentSceneIndex].songSections[currentSectionIndex].waves;
            UpdateMusicSection();

            Debug.Log($"Moved to next scene. Scene: {currentSceneIndex}, Section: {currentSectionIndex}, Expected Waves: {expectedWaves}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during scene transition: {e.Message}");
        }
        finally
        {
            isTransitioning = false;
        }
    }

    private void HandleFinalSplineReached()
    {
        if (!isTransitioning)
        {
            Debug.Log("Final spline reached. Transitioning to next scene.");
            MoveToNextScene();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene != currentAdditiveScene) return;

        _isLoadingScene = false;

        UpdateSceneAttributes();
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.ApplyMusicChanges(currentGroup, currentSceneIndex, currentGroup.scenes[currentSceneIndex].songSections[currentSectionIndex].section);
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
        Debug.Log($"Current State - Scene: {currentSceneIndex}, Section: {currentSectionIndex}, Wave: {ScoreManager.Instance.CurrentSceneWaveCount}");
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
            if (scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene() && scene.name != baseSceneName)
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
                bool setActiveSuccess = UnityEngine.SceneManagement.SceneManager.SetActiveScene(currentAdditiveScene);

                if (!setActiveSuccess)
                {
                    Debug.LogWarning($"Failed to set {currentAdditiveScene.name} as active scene.");
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
                    MusicManager.Instance.ApplyMusicChanges(currentGroup, currentSceneIndex, currentGroup.scenes[currentSceneIndex].songSections[currentSectionIndex].section);
                }

                await UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
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

        if (forceNextScene || currentSectionIndex >= currentGroup.scenes[currentSceneIndex].songSections.Length - 1)
        {
            await MoveToNextScene();
        }
        else
        {
            MoveToNextSection();
        }

        isTransitioning = false;
    }
}