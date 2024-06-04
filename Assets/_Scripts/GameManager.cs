using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI; // Required for UI elements
using System.Collections.Generic; // Add this line at the top for List<T>

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public SceneListData sceneListData;
    // Score and ShotTally properties
    public int Score { get; private set; }
    public int ShotTally { get; private set; }

    public EnemyShootingManager enemyShootingManager;

    private bool _isLoadingScene = false; // Add this line

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Ensure persistence across scenes
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

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
        _isLoadingScene = false; // Reset the flag here

        Debug.Log($"Scene {scene.name} loaded.");

        // Debugging information
        int totalScenes = SceneManager.sceneCountInBuildSettings;
        Debug.Log($"Total scenes in build settings: {totalScenes}");

        // Ensure the scene index is within range
        if (scene.buildIndex < 0 || scene.buildIndex >= totalScenes)
        {
            Debug.LogError($"Scene index {scene.buildIndex} is out of range. Total scenes: {totalScenes}");
            return;
        }

        // Delegate re-registration to ProjectileManager
        if (ProjectileManager.Instance != null)
        {
            ProjectileManager.Instance.ReRegisterEnemiesAndProjectiles();
        }
    }

    public void ChangeToNextScene()
    {
        if (_isLoadingScene) return; // Prevent multiple triggers
        _isLoadingScene = true; // Set the flag

        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneGroup currentGroup = null;
        int currentSceneIndex = -1;

        // Find the current scene group and index
        foreach (var group in sceneListData.sceneGroups)
        {
            for (int i = 0; i < group.scenes.Length; i++)
            {
                if (group.scenes[i] == currentSceneName)
                {
                    currentGroup = group;
                    currentSceneIndex = i;
                    break;
                }
            }
            if (currentGroup != null) break;
        }

        if (currentGroup != null && currentSceneIndex != -1)
        {
            // Determine the next scene index, wrapping if necessary
            int nextSceneIndex = (currentSceneIndex + 1) % currentGroup.scenes.Length;

            // Load the next scene using its name
            SceneManager.LoadScene(currentGroup.scenes[nextSceneIndex]);
        }
        else
        {
            Debug.LogError("Current scene or scene group not found in SceneListData.");
            _isLoadingScene = false; // Reset the flag if there's an error
        }
    }

    public void ChangeSceneWithTransitionToNext()
    {
        if (_isLoadingScene) return; // Prevent multiple triggers
        _isLoadingScene = true; // Set the flag

        // Check if enemyShootingManager is not null and then call its method
        if (enemyShootingManager != null)
        {
            enemyShootingManager.UnregisterAllStaticEnemyShootingsFromKoreographer();
        }

        // Kill all enemies before changing the scene
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            var enemySetup = enemy.GetComponent<EnemyBasicSetup>();
            if (enemySetup != null)
            {
                enemySetup.DebugTriggerDeath();
            }
            else
            {
                Debug.LogWarning("EnemyBasicSetup component missing on enemy object", enemy);
            }
        }
        // Additionally, kill all projectiles or objects on a specific layer if needed
        var gameObjects = FindObjectsOfType(typeof(GameObject)) as GameObject[];
        int layer = 3; // Assuming projectiles are on layer 3 as per your setup
        foreach (var obj in gameObjects)
        {
            if (obj.layer == layer)
            {
                var projectileState = obj.GetComponent<ProjectileStateBased>();
                if (projectileState != null)
                {
                    projectileState.Death();
                }
            }
        }

        // Existing code to change the scene with transition
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneGroup currentGroup = null;
        int currentSceneIndex = -1;

        // Find the current scene group and index
        foreach (var group in sceneListData.sceneGroups)
        {
            for (int i = 0; i < group.scenes.Length; i++)
            {
                if (group.scenes[i] == currentSceneName)
                {
                    currentGroup = group;
                    currentSceneIndex = i;
                    break;
                }
            }
            if (currentGroup != null) break;
        }

        if (currentGroup != null && currentSceneIndex != -1)
        {
            int nextSceneIndex = (currentSceneIndex + 1) % currentGroup.scenes.Length;
            string nextSceneName = currentGroup.scenes[nextSceneIndex];
            string musicSectionName = currentGroup.musicSectionNames[nextSceneIndex]; // Get the music section name

            StartCoroutine(LoadSceneAsync(nextSceneName, musicSectionName));
        }
        else
        {
            Debug.LogError("Current scene or scene group not found in SceneListData.");
            _isLoadingScene = false; // Reset the flag if there's an error
        }
    }

    IEnumerator LoadSceneAsync(string sceneName, string musicSectionName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true;

                // Adjust music parameters only if the music section name is provided
                if (!string.IsNullOrEmpty(musicSectionName))
                {
                    AdjustSongParameters musicParameters = FindObjectOfType<AdjustSongParameters>();
                    if (musicParameters != null)
                    {
                        musicParameters.ChangeMusicSectionByName(musicSectionName);
                    }
                }
                break;
            }
            yield return null;
        }
    }

    // Method to add to the score
    public void AddScore(int scoreToAdd)
    {
        ConditionalDebug.Log($"Adding score. Current score: {Score}, adding: {scoreToAdd}");
        Score += scoreToAdd;
        // Optionally, update UI or other game elements here
    }

    // Method to add to the shot tally
    public void AddShotTally(int shotsToAdd)
    {
        ShotTally += shotsToAdd;
        // Optionally, update UI or other game elements here
    }

    // Method to set the initial score
    public void SetScore(int newScore)
    {
        Score = newScore;
        // Optionally, update UI or other game elements here
    }

    // Method to retrieve the current score
    public int RetrieveScore()
    {
        // Add any additional logic here if needed before returning the score
        return Score;
    }

    // Method to reset the score to zero
    public void ResetScore()
    {
        Score = 0;
        // Optionally, update UI or other game elements here
    }

    // Add this method to the GameManager class
    public void DebugMoveToNextScene()
    {
        // Implement the logic for moving to the next scene for debugging purposes
        ChangeToNextScene();
    }
}

