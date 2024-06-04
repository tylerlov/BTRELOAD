using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using FluffyUnderware.Curvy;

public class ReloadScene : MonoBehaviour
{
    public InputAction reloadAction;

    private void OnEnable()
    {
        reloadAction.Enable();
    }

    private void OnDisable()
    {
        reloadAction.Disable();
    }

    private void Start()
    {
        reloadAction.performed += _ => ReloadCurrentScene();
    }

    private void ReloadCurrentScene()
    {
        // Ensure the CurvyGlobalManager singleton persists and is not recreated if it already exists
        if (CurvyGlobalManager.Instance == null)
        {
            GameObject curvyManager = new GameObject("_CurvyGlobal_");
            curvyManager.AddComponent<CurvyGlobalManager>();
            DontDestroyOnLoad(curvyManager); // Make sure it persists across scene loads
        }

        // Destroy all DontDestroyOnLoad objects except for _CurvyGlobal_
        foreach (var go in FindObjectsOfType<GameObject>())
        {
            if (go.scene.buildIndex == -1 && go.name != "_CurvyGlobal_") // -1 indicates it's a DontDestroyOnLoad object
            {
                Destroy(go);
            }
        }

        // Get the name of the current active scene
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        // Load the scene in single mode, which unloads all other scenes and loads the specified scene
        SceneManager.LoadScene(currentSceneName, LoadSceneMode.Single);
    }
}

