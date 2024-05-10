using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI; // Required for UI elements
using System.Collections.Generic; // Add this line at the top for List<T>

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public CanvasGroup transitionCanvasGroup; // Adjusted to use CanvasGroup
    public float fadeDuration = 0.5f; // Adjusted for quicker fade
    public SceneListData sceneListData;
    public string musicSectionSceneChangeName; // Add this line

    // Score and ShotTally properties
    public int Score { get; private set; }
    public int ShotTally { get; private set; }

    public EnemyShootingManager enemyShootingManager;

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
        Debug.Log($"Scene {scene.name} loaded.");
    }

    public void ChangeToNextScene()
    {
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
        }
    }

    public void ChangeSceneWithTransitionToNext()
    {
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
            // Determine the next scene index, wrapping if necessary
            int nextSceneIndex = (currentSceneIndex + 1) % currentGroup.scenes.Length;
            // Load the next scene with transition
            StartCoroutine(LoadSceneAsync(currentGroup.scenes[nextSceneIndex]));

            // Find and adjust music parameters only if musicSectionSceneChangeName is not blank
            if (!string.IsNullOrEmpty(musicSectionSceneChangeName))
            {
                AdjustSongParameters musicParameters = FindObjectOfType<AdjustSongParameters>();
                if (musicParameters != null)
                {
                    musicParameters.ChangeMusicSectionByName(musicSectionSceneChangeName);
                }
                else
                {
                    Debug.LogWarning("AdjustSongParameters component not found.");
                }
            }
            // If musicSectionSceneChangeName is blank, do not attempt to change the music section
        }
        else
        {
            Debug.LogError("Current scene or scene group not found in SceneListData.");
        }
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        // Start fade out
        yield return StartCoroutine(FadeEffect(true)); // Ensure this completes before proceeding

        // Now start loading the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false; // Prevent immediate activation

        // Wait until the scene is nearly loaded
        while (!asyncLoad.isDone)
        {
            if (asyncLoad.progress >= 0.9f)
            {
                // Scene is loaded but not activated
                yield return StartCoroutine(FadeEffect(false)); // Start fade in
                asyncLoad.allowSceneActivation = true; // Allow scene activation
                break;
            }
            yield return null;
        }
    }

    IEnumerator FadeEffect(bool fadeOut)
    {
        float targetAlpha = fadeOut ? 1f : 0f;
        float fadeSpeed = Mathf.Abs(transitionCanvasGroup.alpha - targetAlpha) / fadeDuration;

        while (!Mathf.Approximately(transitionCanvasGroup.alpha, targetAlpha))
        {
            float newAlpha = Mathf.MoveTowards(transitionCanvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
            transitionCanvasGroup.alpha = newAlpha;
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
}