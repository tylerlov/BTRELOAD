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
        if (SceneManager.sceneCount == 1)
        {
            // Likely loaded individually
            gameObject.SetActive(true);
        }
        else
        {
            // Loaded as part of a sequence or additively
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Unregister the sceneLoaded event to avoid memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

