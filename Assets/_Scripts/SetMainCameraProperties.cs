using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class SetMainCameraProperties : MonoBehaviour
{
    public CinemachineCamera mainCamera;

    public void SetCameraProperties(float fOV)
    {
        if (mainCamera != null)
        {
            LensSettings lens = mainCamera.Lens;
            lens.FieldOfView = fOV;
            mainCamera.Lens = lens;
        }
    }

    public void SetDrawDistance(float distance)
    {
        if (mainCamera != null)
        {
            LensSettings lens = mainCamera.Lens;
            lens.FarClipPlane = distance * 100;
            mainCamera.Lens = lens;
        }
    }
}
