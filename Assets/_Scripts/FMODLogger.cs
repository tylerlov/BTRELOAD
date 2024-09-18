using UnityEngine;

public class FMODLogger : MonoBehaviour
{
    void Start()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        FMOD.Debug.Initialize(FMOD.DEBUG_FLAGS.LOG, FMOD.DEBUG_MODE.TTY, null, null);
#else
        FMOD.Debug.Initialize(FMOD.DEBUG_FLAGS.ERROR, FMOD.DEBUG_MODE.TTY, null, null);
#endif
    }
}