using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SonicBloom.Koreo;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{

    [EventID]
    public string eventID;

    public Animator transition;

    public float transitionTime = 1.0f;

    public KeyCode nextLevelButton;
    public KeyCode restartLevelButton;

    void Start()
    {
        Koreographer.Instance.RegisterForEvents(eventID, LoadNextLevel);
        Koreographer.Instance.RegisterForEvents(eventID, RestartLevel);
    }

    public void LoadNextLevel(KoreographyEvent evt)
    {
        if (Time.timeScale != 0f && Input.GetKey(nextLevelButton))
        {
            StartCoroutine(LoadLevel(SceneManager.GetActiveScene().buildIndex + 1));
        }
    }
    public void RestartLevel(KoreographyEvent evt)
    {
        if (Time.timeScale != 0f && Input.GetKey(restartLevelButton))
        {
            StartCoroutine(LoadLevel(SceneManager.GetActiveScene().buildIndex));
        }
    }

    IEnumerator LoadLevel(int levelIndex)
    {
        SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        AsyncOperation asyncScene = SceneManager.LoadSceneAsync(levelIndex, LoadSceneMode.Single);
        asyncScene.allowSceneActivation = false;

        //Need to make this the length of the transition clip
        // Hacky way of trying to time it properly
        yield return new WaitForSeconds(transitionTime);

        asyncScene.allowSceneActivation = true;

    }
}
