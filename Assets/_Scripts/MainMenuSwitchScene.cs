using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity; // Add this import

public class MainMenuSwitchScene : MonoBehaviour
{
    [SerializeField]
    private GameObject objectToDisable;

    [SerializeField]
    private float minimumLoadTime = 0.5f; // Minimum time to show transition

    private static MainMenuSwitchScene instance;

    private StudioEventEmitter fmodEventEmitter; // Add this field

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        // If this is the instance we're keeping, find the FMOD emitter
        if (instance == this)
        {
            fmodEventEmitter = GetComponent<StudioEventEmitter>();
        }
    }

    public void SwitchToScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    public void SwitchToSceneByIndex(int sceneIndex)
    {
        string sceneName = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Disable the specified object
        if (objectToDisable != null)
            objectToDisable.SetActive(false);

        // Stop and destroy the FMOD event emitter
        if (fmodEventEmitter != null)
        {
            fmodEventEmitter.Stop();
            Destroy(fmodEventEmitter);
        }

        // Unload all loaded assets
        Resources.UnloadUnusedAssets();

        // Start loading the new scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        float elapsedTime = 0f;

        // Wait until the scene is fully loaded and minimum time has passed
        while (!asyncLoad.isDone || elapsedTime < minimumLoadTime)
        {
            elapsedTime += Time.deltaTime;

            if (asyncLoad.progress >= 0.9f && elapsedTime >= minimumLoadTime)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        // Clean up this instance if it's not needed in the new scene
        if (instance == this && !ShouldPersist())
        {
            Destroy(gameObject);
        }
    }

    private bool ShouldPersist()
    {
        // Implement logic to determine if this object should persist
        // For example, you might want to persist only when switching to certain scenes
        return false; // Default to not persisting
    }

    public void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        StartCoroutine(LoadSceneAsync(currentSceneName));
    }
}
