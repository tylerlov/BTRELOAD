using UnityEngine;
using Cinemachine;
using UnityEngine.UI;

public class FOVAdjuster : MonoBehaviour
{
    private CinemachineVirtualCamera cinemachineCamera;
    [SerializeField] private Slider fovSlider;

    void Start()
    {
        // Find the specific Cinemachine Virtual Camera by name
        cinemachineCamera = GameObject.Find("CM vcam1").GetComponent<CinemachineVirtualCamera>();
        if (cinemachineCamera == null)
        {
            Debug.LogError("Cinemachine Virtual Camera 'CM vcam1' is not found.");
            return;
        }

        if (fovSlider == null)
        {
            Debug.LogError("FOV Slider is not assigned.");
            return;
        }

        // Initialize slider value to current FOV
        fovSlider.value = cinemachineCamera.m_Lens.FieldOfView;
        fovSlider.onValueChanged.AddListener(AdjustFOV);
    }

    private void AdjustFOV(float fov)
    {
        if (cinemachineCamera != null)
        {
            cinemachineCamera.m_Lens.FieldOfView = fov;
        }
    }
}
