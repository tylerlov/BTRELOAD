using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitchCleanup : MonoBehaviour
{
    private const string ReticleName = "Reticle";
    private const string JoostManName = "JoostMan 3";

    [SerializeField]
    private float searchDelay = 0.5f;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DelayedSearch());
    }

    private IEnumerator DelayedSearch()
    {
        yield return new WaitForSeconds(searchDelay);

        ActivateGameObject(ReticleName);
        ActivateGameObject(JoostManName);
    }

    private void ActivateGameObject(string objectName)
    {
        GameObject obj = FindGameObjectInAllScenesRecursively(objectName);
        if (obj != null && !obj.activeSelf)
        {
            obj.SetActive(true);
        }
    }

    private GameObject FindGameObjectInAllScenesRecursively(string objectName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                GameObject found = FindRecursively(rootObject, objectName);
                if (found != null)
                {
                    return found;
                }
            }
        }
        return null;
    }

    private GameObject FindRecursively(GameObject obj, string name)
    {
        if (obj.name == name)
            return obj;

        foreach (Transform child in obj.transform)
        {
            GameObject found = FindRecursively(child.gameObject, name);
            if (found != null)
                return found;
        }

        return null;
    }
}
