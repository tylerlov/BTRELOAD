using UnityEngine;
using FMODUnity;
using FMOD;

public class FMODCustomLogger : MonoBehaviour
{
    void Awake()
    {
        // Disable all FMOD logging
        FMOD.Debug.Initialize(FMOD.DEBUG_FLAGS.NONE, FMOD.DEBUG_MODE.TTY, null, null);

        if (RuntimeManager.IsInitialized)
        {
            // Disable studio logging
            FMOD.Studio.System studioSystem = RuntimeManager.StudioSystem;
            studioSystem.setCallback(null, FMOD.Studio.SYSTEM_CALLBACK_TYPE.ALL);

            // Disable core system logging
            FMOD.System coreSystem = RuntimeManager.CoreSystem;
            coreSystem.setCallback(null, FMOD.SYSTEM_CALLBACK_TYPE.ALL);

            // Attempt to disable EventDescription logging
            DisableEventDescriptionLogging(studioSystem);
        }
        else
        {
            UnityEngine.Debug.LogError("FMOD is not initialized. Cannot disable logging.");
        }
    }

    private void DisableEventDescriptionLogging(FMOD.Studio.System studioSystem)
    {
        // Get all banks
        FMOD.Studio.Bank[] banks;
        int bankCount;
        studioSystem.getBankList(out banks);
        studioSystem.getBankCount(out bankCount);

        for (int i = 0; i < bankCount; i++)
        {
            FMOD.Studio.EventDescription[] eventDescriptions;
            int eventCount;
            banks[i].getEventList(out eventDescriptions);
            banks[i].getEventCount(out eventCount);

            for (int j = 0; j < eventCount; j++)
            {
                eventDescriptions[j].setCallback(null, FMOD.Studio.EVENT_CALLBACK_TYPE.ALL);
            }
        }
    }
}