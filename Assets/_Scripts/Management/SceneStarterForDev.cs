using UnityEngine;

public class SceneStarterForDev : MonoBehaviour
{
    // References to the components
    [SerializeField]
    public CinemachineCameraSwitching cinemachineCameraSwitching;

    // Start is called before the first frame update
    void Start()
    {
        cinemachineCameraSwitching = FindObjectOfType<CinemachineCameraSwitching>(true);

        // Ensure references are assigned        if (adjustSongParameters == null || cinemachineCameraSwitching == null)
        if (cinemachineCameraSwitching == null)
        {
            ConditionalDebug.LogError("References to GameManager or CinemachineCameraSwitching are not set.");
            return;
        }

        // Call the methods immediately
        OnWaveStarted();
    }

    // Method to be called when the wave starts
    void OnWaveStarted()
    {
        SceneManagerBTR.Instance.updateStatus("wavestart");
        cinemachineCameraSwitching.SetMainCamera();
    }
}
