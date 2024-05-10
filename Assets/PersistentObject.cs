using UnityEngine;
using UnityEngine.SceneManagement;


public class PersistentObject : MonoBehaviour
{
    private static bool _isCreated = false;

    private void Awake()
    {
        if (!_isCreated)
        {
            DontDestroyOnLoad(gameObject);
            _isCreated = true;
        }
        else
        {
            Destroy(gameObject); // Destroy this instance if one already exists
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (var obj in rootObjects)
        {
            if (obj != gameObject && obj.name == gameObject.name)
            {
                Destroy(obj);
            }
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
}
