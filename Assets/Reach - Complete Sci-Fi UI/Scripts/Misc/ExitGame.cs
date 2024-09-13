using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace Michsky.UI.Reach
{
    public class ExitGame : MonoBehaviour
    {
        [SerializeField] private float quitDelay = 0.5f;

        public void Exit()
        {
            StartCoroutine(QuitGameCoroutine());
        }

        private IEnumerator QuitGameCoroutine()
        {
            Debug.Log("<b>[Reach UI]</b> Initiating exit process...");

            // Perform any cleanup or saving operations here
            // For example:
            // SaveGameState();
            // CleanupNetworkConnections();

            // Unload all scenes
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                yield return SceneManager.UnloadSceneAsync(scene);
            }

            // Wait for a short delay to allow for any final cleanup
            yield return new WaitForSecondsRealtime(quitDelay);

            Debug.Log("<b>[Reach UI]</b> Exiting application...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            try
            {
                Application.Quit();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"<b>[Reach UI]</b> Error during application quit: {e.Message}");
            }
#endif
        }
    }
}