using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    private Dictionary<string, Queue<FMOD.Studio.EventInstance>> audioPool;
    private const int POOL_SIZE_PER_SOUND = 10;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioPool = new Dictionary<string, Queue<FMOD.Studio.EventInstance>>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public FMOD.Studio.EventInstance GetOrCreateInstance(string eventPath)
    {
        if (!audioPool.ContainsKey(eventPath))
            audioPool[eventPath] = new Queue<FMOD.Studio.EventInstance>();

        if (audioPool[eventPath].Count > 0)
            return audioPool[eventPath].Dequeue();

        return FMODUnity.RuntimeManager.CreateInstance(eventPath);
    }

    public void ReleaseInstance(string eventPath, FMOD.Studio.EventInstance instance)
    {
        if (audioPool[eventPath].Count < POOL_SIZE_PER_SOUND)
        {
            instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            instance.setVolume(0);
            audioPool[eventPath].Enqueue(instance);
        }
        else
        {
            instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            instance.release();
        }
    }
}
