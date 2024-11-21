using UnityEngine;
using System.Collections.Generic;
using FMOD.Studio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    private Dictionary<string, Queue<EventInstance>> audioPool;
    public const int POOL_SIZE_PER_SOUND = 10;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioPool = new Dictionary<string, Queue<EventInstance>>();
            InitializeAudioSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSystem()
    {
        audioPool = new Dictionary<string, Queue<EventInstance>>();
    }

    public EventInstance GetOrCreateInstance(string eventPath)
    {
        if (!audioPool.ContainsKey(eventPath))
            audioPool[eventPath] = new Queue<EventInstance>();

        if (audioPool[eventPath].Count > 0)
            return audioPool[eventPath].Dequeue();

        return FMODUnity.RuntimeManager.CreateInstance(eventPath);
    }

    public void ReleaseInstance(string eventPath, EventInstance instance)
    {
        if (!instance.isValid()) return;

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

    private void OnDestroy()
    {
        if (audioPool != null)
        {
            foreach (var pool in audioPool.Values)
            {
                while (pool.Count > 0)
                {
                    var instance = pool.Dequeue();
                    if (instance.isValid())
                        instance.release();
                }
            }
            audioPool.Clear();
        }
    }
}
