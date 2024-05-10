using DG.Tweening;
using FMOD.Studio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FmodVolumeControl : MonoBehaviour
{
    FMOD.Studio.VCA masterVCA;

    void Start()
    {
        masterVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Master VCA");
    }

    public void adjustMasterVolume(float volume)
    {
        masterVCA.setVolume(volume);
    }
}