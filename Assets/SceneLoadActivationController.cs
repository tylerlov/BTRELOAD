using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadActivationController : MonoBehaviour
{
    private void Awake()
    {
        // Register the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if the loaded scene is the active scene
        if (scene == SceneManager.GetActiveScene())
        {
            // Disable the game object if coming from another scene
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Unregister the sceneLoaded event to avoid memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

