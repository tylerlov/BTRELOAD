using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections;

public class SimpleFmodTesting : MonoBehaviour
{
    [SerializeField]
    private StudioEventEmitter eventEmitter;

    [SerializeField]
    private string parameterName = "State";

    [SerializeField]
    private int stateCount = 3;

    [SerializeField]
    private float stateChangeInterval = 2f;

    private int currentState = 0;

    private void Start()
    {
        if (eventEmitter == null)
        {
            Debug.LogError("StudioEventEmitter is not assigned. Please assign it in the inspector.");
            return;
        }

        Debug.Log($"SimpleFmodTesting started. Event path: {eventEmitter.EventReference.Path}");
        Debug.Log($"Parameter Name: {parameterName}, State Count: {stateCount}, Interval: {stateChangeInterval}");

        // Check if the parameter exists
        EventDescription eventDescription;
        eventEmitter.EventInstance.getDescription(out eventDescription);
        PARAMETER_DESCRIPTION parameterDescription;
        FMOD.RESULT result = eventDescription.getParameterDescriptionByName(parameterName, out parameterDescription);
        
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Parameter '{parameterName}' not found in the event. Check if the parameter name is correct.");
            return;
        }

        Debug.Log($"Parameter '{parameterName}' found in the event. Min: {parameterDescription.minimum}, Max: {parameterDescription.maximum}");

        StartCoroutine(ChangeParameterRoutine());
    }

    private IEnumerator ChangeParameterRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(stateChangeInterval);

            currentState = (currentState + 1) % stateCount;

            // Set the parameter on the event instance
            FMOD.RESULT result = eventEmitter.EventInstance.setParameterByName(parameterName, currentState);
            
            if (result == FMOD.RESULT.OK)
            {
                Debug.Log($"Changed '{parameterName}' parameter to state: {currentState}");
                
                // Verify the parameter was set correctly
                float value;
                eventEmitter.EventInstance.getParameterByName(parameterName, out value);
                Debug.Log($"Verified parameter value: {value}");
            }
            else
            {
                Debug.LogError($"Failed to set parameter. FMOD Result: {result}");
            }

            // Check if the event is actually playing
            PLAYBACK_STATE playbackState;
            eventEmitter.EventInstance.getPlaybackState(out playbackState);
            Debug.Log($"Current playback state: {playbackState}");
        }
    }
}
