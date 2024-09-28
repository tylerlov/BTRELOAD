using System.Collections;
using Chronos;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Time Control")]
    [SerializeField]
    private float defaultRewindTimeScale = -2f;

    [SerializeField]
    private float defaultRewindDuration = 0.5f;

    [SerializeField]
    private float defaultReturnToNormalDuration = 0.25f;

    [SerializeField]
    private string globalClockName = "Test";

    private GlobalClock globalClock;
    private DebugSettings debugSettings;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(InitializeGlobalClock());
    }

    private IEnumerator InitializeGlobalClock()
    {
        yield return null;

        try
        {
            globalClock = Timekeeper.instance.Clock(debugSettings.globalClockName);
            InitializeDebugTimeScale();
        }
        catch (ChronosException)
        {
            Debug.LogWarning(
                $"Global clock '{debugSettings.globalClockName}' not found. Debug time scale will not be applied."
            );
        }
    }

    public void InitializeDebugTimeScale()
    {
        if (globalClock != null)
        {
            globalClock.localTimeScale = debugSettings.debugTimeScale;
            Debug.Log(
                $"Debug time scale set to {debugSettings.debugTimeScale} on global clock '{debugSettings.globalClockName}'"
            );
        }
        else
        {
            Debug.LogWarning("Global clock not initialized. Debug time scale not applied.");
        }
    }

    public IEnumerator RewindTime(
        float rewindTimeScale = -2f,
        float rewindDuration = 0.5f,
        float returnToNormalDuration = 0.25f
    )
    {
        if (globalClock == null)
        {
            Debug.LogError("Global clock is not yet initialized in TimeManager.RewindTime");
            yield break;
        }

        float startTime = globalClock.time;
        globalClock.LerpTimeScale(rewindTimeScale, rewindDuration);

        Debug.Log(
            $"Rewinding time... Start time: {startTime}, Rewind scale: {rewindTimeScale}, Duration: {rewindDuration}"
        );

        yield return new WaitForSeconds(rewindDuration);

        float rewoundTime = globalClock.time;
        globalClock.LerpTimeScale(1f, returnToNormalDuration);

        Debug.Log(
            $"Returning to normal time... Rewound time: {rewoundTime}, Return duration: {returnToNormalDuration}"
        );

        yield return new WaitForSeconds(returnToNormalDuration);

        Debug.Log("Rewind complete");
    }

    public void StartRewindTime(
        float rewindTimeScale = -2f,
        float rewindDuration = 0.5f,
        float returnToNormalDuration = 0.25f
    )
    {
        if (QuickTimeEventManager.Instance != null && QuickTimeEventManager.Instance.IsQTEActive)
        {
            // Don't start rewind if QTE is active
            return;
        }
        StartCoroutine(RewindTime(rewindTimeScale, rewindDuration, returnToNormalDuration));
    }

    private GlobalClock TryGetGlobalClock()
    {
        try
        {
            return Timekeeper.instance.Clock(globalClockName);
        }
        catch (ChronosException)
        {
            return null;
        }
    }

    public void SetTimeScale(float timeScale)
    {
        if (globalClock != null)
        {
            globalClock.localTimeScale = timeScale;
            Debug.Log(
                $"Time scale set to {timeScale} on global clock '{debugSettings.globalClockName}'"
            );
        }
        else
        {
            Debug.LogError("Global clock is not initialized. Cannot set time scale.");
        }
    }

    public void SetDebugSettings(DebugSettings settings)
    {
        debugSettings = settings;
    }

    public GlobalClock GetGlobalClock()
    {
        return globalClock;
    }

    public void PauseTime()
    {
        if (globalClock != null)
        {
            globalClock.localTimeScale = 0f;
            Debug.Log("Time paused");
        }
        else
        {
            Debug.LogError("Global clock is not initialized. Cannot pause time.");
        }
    }

    public void ResumeTime()
    {
        if (globalClock != null)
        {
            globalClock.localTimeScale = 1f;
            Debug.Log("Time resumed");
        }
        else
        {
            Debug.LogError("Global clock is not initialized. Cannot resume time.");
        }
    }

    public float GetCurrentTime()
    {
        if (globalClock != null)
        {
            return globalClock.time;
        }
        else
        {
            Debug.LogError("Global clock is not initialized. Cannot get current time.");
            return 0f;
        }
    }

    public void SlowMotion(float slowdownFactor, float duration)
    {
        StartCoroutine(SlowMotionCoroutine(slowdownFactor, duration));
    }

    private IEnumerator SlowMotionCoroutine(float slowdownFactor, float duration)
    {
        if (globalClock == null)
        {
            Debug.LogError("Global clock is not initialized. Cannot perform slow motion.");
            yield break;
        }

        float originalTimeScale = globalClock.localTimeScale;
        globalClock.localTimeScale = slowdownFactor;

        yield return new WaitForSeconds(duration);

        globalClock.localTimeScale = originalTimeScale;
    }
}
