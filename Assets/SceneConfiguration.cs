using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneProjectileConfigurator : MonoBehaviour
{
    [System.Serializable]
    public class SceneConfiguration
    {
        public string sceneName;
        public int maxEnemyShotsPerInterval = 4;
        public float enemyShotIntervalSeconds = 3f;
    }

    [SerializeField] private SceneConfiguration[] sceneConfigurations;

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
        ApplySceneConfiguration(scene.name);
    }

    private void ApplySceneConfiguration(string sceneName)
    {
        SceneConfiguration config = System.Array.Find(sceneConfigurations, c => c.sceneName == sceneName);

        if (config != null)
        {
            ProjectileSpawner spawner = ProjectileSpawner.Instance;
            if (spawner != null)
            {
                spawner.SetShotRates(config.maxEnemyShotsPerInterval, config.enemyShotIntervalSeconds);
            }
            else
            {
                Debug.LogWarning("ProjectileSpawner instance not found. Unable to apply scene configuration.");
            }
        }
        else
        {
            Debug.LogWarning($"No configuration found for scene: {sceneName}");
        }
    }
}