using UnityEngine;

public class SceneStarterForDev : MonoBehaviour
{
    // References to the components
    public AdjustSongParameters adjustSongParameters;
    public CinemachineCameraSwitching cinemachineCameraSwitching;

    // Start is called before the first frame update
    void Start()
    {
        // Ensure references are assigned
        if (adjustSongParameters == null || cinemachineCameraSwitching == null)
        {
            Debug.LogError("References to AdjustSongParameters or CinemachineCameraSwitching are not set.");
            return;
        }

        // Call the methods immediately
        OnWaveStarted();
    }

    // Method to be called when the wave starts
    void OnWaveStarted()
    {
        adjustSongParameters.updateStatus();
        cinemachineCameraSwitching.SetMainCamera();
    }
}
