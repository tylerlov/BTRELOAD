using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class FMODFilterParameterControl : MonoBehaviour
{
    [ParamRef]
    public string pauseParameterName = "Pause";

    public void SetPauseValue(float value)
    {
        FMOD.RESULT result = RuntimeManager.StudioSystem.setParameterByName(
            pauseParameterName,
            value
        );

        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError(
                string.Format(
                    ("[FMOD] FilterParameterControl failed to set parameter {0} : result = {1}"),
                    pauseParameterName,
                    result
                )
            );
        }
    }
}
