using UnityEngine;

public class SceneStarterForDev : MonoBehaviour
{
    // References to the components
    [SerializeField] public CinemachineCameraSwitching cinemachineCameraSwitching;
    private GameManager gameManager;
    // Start is called before the first frame update
    void Start()
    {
        // Find the active AdjustSongParameters component
        gameManager = GameManager.instance;
        cinemachineCameraSwitching = FindObjectOfType<CinemachineCameraSwitching>(true);

        // Ensure references are assigned        if (adjustSongParameters == null || cinemachineCameraSwitching == null)
        if (gameManager == null || cinemachineCameraSwitching == null)
        {
            Debug.LogError("References to GameManager or CinemachineCameraSwitching are not set.");
            return;
        }

        // Call the methods immediately
        OnWaveStarted();
    }

    // Method to be called when the wave starts
    void OnWaveStarted()
    {
        gameManager.updateStatus();
        cinemachineCameraSwitching.SetMainCamera();
    }
}
