using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SonicBloom.Koreo;


//[AddComponentMenu("Koreographer/Demos/Emit Particles On Span")]

public class BackgroundFX : MonoBehaviour
{
    [EventID]
    public string eventID;

    public float SceneBPM = 120;
    public float step = 0;
    public float duration;
    public Color colorOne;
    public Color colorTwo;


    private bool colorChangeSwitch = false;
    private bool lockingFx = false;


    void Awake()
    {
        // QualitySettings.vSyncCount = 0;
        // Application.targetFrameRate = 120;
        //RenderSettings.skybox = new Material(RenderSettings.skybox);
        Koreographer.Instance.RegisterForEvents(eventID, OnMusicalSkybox);
    }
    public void activateBackgroundFx()
    {
        if (lockingFx == false)
        {
            lockingFx = true;
        }
    }
    public void disableBackgroundFx()
    {
        if (lockingFx == true)
        {
            lockingFx = false;
        }
    }

    void OnMusicalSkybox(KoreographyEvent evt)
    {
        colorChangeSwitch = !colorChangeSwitch;

        if (Time.timeScale != 0 && lockingFx)
        {   
            if (colorChangeSwitch == true)
            {
                Debug.Log("Changing to First Color!");
                RenderSettings.skybox.SetColor("_Tint",colorOne);
                //RenderSettings.skybox.SetColor("_Tint", colorTwo);
            }
            else if (colorChangeSwitch == false)
            {
                Debug.Log("Changing to Second Color!");
                RenderSettings.skybox.SetColor("_Tint", colorTwo);
                //RenderSettings.skybox.SetColor("_Tint", colorOne);

            }
        }
    }

}


