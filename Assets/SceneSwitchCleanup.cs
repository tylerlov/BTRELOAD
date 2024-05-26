using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitchCleanup : MonoBehaviour
{
    public List<GameObject> objectsToActivate; // List to hold the GameObjects
    public float activationDelay = 0.1f; // Delay in seconds before activation

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded; // Subscribe to the sceneLoaded event
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe to prevent memory leaks
    }

    private void Start()
    {
        // Directly activate objects if running in the editor, without waiting for scene load
        if (Application.isEditor)
        {
            StartCoroutine(DelayedActivateObjects());
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if the scene is loaded individually or as part of a sequence
        if (SceneManager.sceneCount == 1 || Application.isEditor)
        {
            StartCoroutine(DelayedActivateObjects()); // Activate objects with a delay when scene is loaded
        }
        else
        {
            DeactivateObjects(); // Optionally deactivate objects
        }
    }

    private IEnumerator DelayedActivateObjects()
    {
        yield return new WaitForSeconds(activationDelay); // Wait for the specified delay

        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(true); // Activate each GameObject in the list
            }
        }
    }

    private void DeactivateObjects()
    {
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(false); // Deactivate each GameObject in the list
            }
        }
    }
}