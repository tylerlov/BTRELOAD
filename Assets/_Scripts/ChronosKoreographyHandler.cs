using UnityEngine;
using SonicBloom.Koreo;
using Chronos;

public class ChronosKoreographyHandler : MonoBehaviour
{
    [SerializeField] private string mainClockKey = "Test";
    private GlobalClock mainClock;
    private Koreographer koreographer;
    private float initialTimeScale = 1f;
    private float lastTimeScale = 1f;

    void Start()
    {
        koreographer = GetComponent<Koreographer>();
        if (koreographer == null)
        {
            ConditionalDebug.LogError("Koreographer component not found on this GameObject.");
            return;
        }

        Timekeeper timekeeper = FindFirstObjectByType<Timekeeper>();
        if (timekeeper == null)
        {
            ConditionalDebug.LogError("Timekeeper not found in the scene.");
            return;
        }

        mainClock = timekeeper.Clock(mainClockKey) as GlobalClock;
        if (mainClock == null)
        {
            ConditionalDebug.LogError($"Global clock with key '{mainClockKey}' not found.");
            return;
        }

        initialTimeScale = mainClock.localTimeScale;
        lastTimeScale = initialTimeScale;
    }

    void Update()
    {
        if (mainClock == null || koreographer == null) return;

        float currentTimeScale = mainClock.localTimeScale;
        if (currentTimeScale != lastTimeScale)
        {
            float scaleFactor = currentTimeScale / initialTimeScale;
            koreographer.EventDelayInSeconds = koreographer.EventDelayInSeconds / scaleFactor;
            lastTimeScale = currentTimeScale;
        }
    }
}
