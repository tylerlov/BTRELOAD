using UnityEngine;
using FMODUnity;
using FMOD;

public class FMODManager : MonoBehaviour
{
    void Awake()
    {
        #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            RuntimeManager.CoreSystem.setDSPBufferSize(1024, 4);
            RuntimeManager.CoreSystem.mixerSuspend();
            RuntimeManager.CoreSystem.mixerResume();
            // Remove the setCallback line as it's causing issues
            // RuntimeManager.CoreSystem.setCallback(null, FMOD.SYSTEM_CALLBACK_ALL);
        #endif
    }
}