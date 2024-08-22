using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class SetMainCameraProperties : MonoBehaviour
{
    public CinemachineVirtualCamera mainCamera;

    public void SetCameraProperties(float fOV)
    {
        mainCamera.m_Lens.FieldOfView = fOV;
    }

    public void SetDrawDistance(float distance)
    {
        mainCamera.m_Lens.FarClipPlane = distance * 100;
    }
}
