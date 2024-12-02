using UnityEngine;
using FMODUnity;
using FMOD;
using FMOD.Studio;

public class FMODCustomLogger : MonoBehaviour
{
    private void Awake()
    {
        ConditionalDebug.Log("Initializing FMOD logging settings...");
        
        FMOD.Debug.Initialize(
            FMOD.DEBUG_FLAGS.NONE,
            FMOD.DEBUG_MODE.TTY,
            null,
            null
        );

        if (RuntimeManager.IsInitialized)
        {
            var studioSystem = RuntimeManager.StudioSystem;
            var coreSystem = RuntimeManager.CoreSystem;

        }
    }

    private void DisableEventLogging(FMOD.Studio.System studioSystem)
    {
        FMOD.Studio.Bank[] banks = new FMOD.Studio.Bank[256];
        int bankCount;
        studioSystem.getBankList(out banks);
        studioSystem.getBankCount(out bankCount);

        for (int i = 0; i < bankCount; i++)
        {
            FMOD.Studio.EventDescription[] events = new FMOD.Studio.EventDescription[256];
            int eventCount;
            banks[i].getEventList(out events);
            banks[i].getEventCount(out eventCount);

            for (int j = 0; j < eventCount; j++)
            {
                if (events[j].isValid())
                {
                    events[j].setCallback(null, EVENT_CALLBACK_TYPE.ALL);
                }
            }
        }
    }

    private void ERRCHECK(FMOD.RESULT result)
    {
        if (result != FMOD.RESULT.OK)
        {
            UnityEngine.Debug.LogError($"FMOD Error: {result}");
        }
    }
}