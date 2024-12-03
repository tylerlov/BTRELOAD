using UnityEngine;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;

[DefaultExecutionOrder(-200)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("FMOD Events")]
    [SerializeField]
    private EventReference musicEvent;
    
    [SerializeField]
    private StudioEventEmitter musicPlayback;
    
    private Dictionary<string, Queue<EventInstance>> audioPool;
    private bool isMusicInitialized = false;
    public const int POOL_SIZE_PER_SOUND = 10;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSystem();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitializeMusicPlayback();
    }

    private void InitializeAudioSystem()
    {
        audioPool = new Dictionary<string, Queue<EventInstance>>();
    }

    private void InitializeMusicPlayback()
    {
        // Get the StudioEventEmitter from the same GameObject
        if (musicPlayback == null)
        {
            musicPlayback = GetComponent<StudioEventEmitter>();
            if (musicPlayback == null)
            {
                ConditionalDebug.LogError("StudioEventEmitter not found on the FMOD music object! Please ensure it's properly set up.");
                return;
            }
        }

        // If musicEvent is not assigned but the StudioEventEmitter has an event reference, use that
        if (musicEvent.IsNull && !musicPlayback.EventReference.IsNull)
        {
            musicEvent = musicPlayback.EventReference;
        }

        if (!musicEvent.IsNull)
        {
            // Only set the event reference if it's different from what's already set
            if (musicPlayback.EventReference.IsNull || musicPlayback.EventReference.Guid != musicEvent.Guid)
            {
                musicPlayback.EventReference = musicEvent;
            }
            
            // Check if it's already playing
            FMOD.Studio.PLAYBACK_STATE playbackState;
            musicPlayback.EventInstance.getPlaybackState(out playbackState);
            
            if (playbackState != FMOD.Studio.PLAYBACK_STATE.PLAYING)
            {
                musicPlayback.Play();
            }
            
            isMusicInitialized = true;
        }
        else
        {
            ConditionalDebug.LogWarning("No music event assigned in AudioManager or StudioEventEmitter. Music functionality will be disabled.");
        }
    }

    #region Sound Effects Management
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
        if (!instance.isValid() || audioPool == null) return;

        // Check if the pool exists for this event
        if (!audioPool.ContainsKey(eventPath))
        {
            instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            instance.release();
            return;
        }

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
    #endregion

    #region Music Management
    public void SetMusicParameter(string parameterName, float value)
    {
        if (musicPlayback == null || !musicPlayback.EventInstance.isValid())
        {
            ConditionalDebug.LogError($"Failed to set music parameter '{parameterName}': Invalid event instance");
            return;
        }

        FMOD.Studio.PLAYBACK_STATE playbackState;
        musicPlayback.EventInstance.getPlaybackState(out playbackState);

        if (playbackState != FMOD.Studio.PLAYBACK_STATE.PLAYING)
        {
            ConditionalDebug.LogWarning("FMOD event is not playing. Starting playback.");
            musicPlayback.Play();
        }

        FMOD.RESULT result = musicPlayback.EventInstance.setParameterByName(parameterName, value);
        if (result != FMOD.RESULT.OK)
        {
            ConditionalDebug.LogError($"Failed to set music parameter '{parameterName}': {result}");
        }
    }

    public void ApplyMusicChanges(SceneGroup currentGroup, int currentScene, float currentSongSection)
    {
        if (musicPlayback != null && currentGroup != null && currentScene < currentGroup.scenes.Length)
        {
            SetMusicParameter("Sections", currentSongSection);
        }
    }

    public void ChangeMusicSectionByName(string sectionName, SceneGroup currentGroup)
    {
        for (int i = 0; i < currentGroup.scenes.Length; i++)
        {
            for (int j = 0; j < currentGroup.scenes[i].songSections.Length; j++)
            {
                if (currentGroup.scenes[i].songSections[j].name == sectionName)
                {
                    ChangeSongSection(currentGroup, i, currentGroup.scenes[i].songSections[j].section);
                    return;
                }
            }
        }
        ConditionalDebug.LogWarning($"Section '{sectionName}' not found in the current group.");
    }

    public void ChangeSongSection(SceneGroup currentGroup, int currentScene, float currentSongSection)
    {
        if (musicPlayback == null || currentGroup == null || 
            currentGroup.scenes == null || currentScene >= currentGroup.scenes.Length)
        {
            ConditionalDebug.LogWarning("Invalid parameters in ChangeSongSection.");
            return;
        }

        var songSections = currentGroup.scenes[currentScene].songSections;
        int sectionIndex = System.Array.FindIndex(
            songSections,
            section => section.section == currentSongSection
        );

        if (sectionIndex == -1)
        {
            ConditionalDebug.LogWarning($"Could not find section with value {currentSongSection} in scene {currentScene}");
            return;
        }

        SetMusicParameter("Sections", currentSongSection);
        ConditionalDebug.Log($"Song section changed to: {songSections[sectionIndex].name} (Section value: {currentSongSection})");
    }
    #endregion

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
