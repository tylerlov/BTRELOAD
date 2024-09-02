using UnityEngine;
using System.Collections;
using FMODUnity;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    public StudioEventEmitter musicPlayback;

    private FMOD.Studio.EventInstance musicEventInstance;

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
            return;
        }

        // Additional initialization if needed
    }

    public static void FindActiveFMODInstance()
    {
        Instance.FindActiveFMODInstanceInternal();
    }

    private void FindActiveFMODInstanceInternal()
    {
        var fmodInstances = FindObjectsOfType<StudioEventEmitter>(true);
        foreach (var instance in fmodInstances)
        {
            if (instance.gameObject.name == "FMOD Music" && instance.gameObject.activeInHierarchy)
            {
                musicPlayback = instance;
                break;
            }
        }

        if (musicPlayback == null)
        {
            Debug.LogError("Active FMOD Music instance not found in the scene.");
        }
    }

    public static void ApplyMusicChanges(SceneGroup currentGroup, int currentScene, float currentSongSection)
    {
        Instance.ApplyMusicChangesInternal(currentGroup, currentScene, currentSongSection);
    }

    private void ApplyMusicChangesInternal(SceneGroup currentGroup, int currentScene, float currentSongSection)
    {
        if (musicPlayback != null && currentGroup != null && currentScene < currentGroup.scenes.Length)
        {
            float sectionValue = currentSongSection;
            musicPlayback.SetParameter("Sections", sectionValue);
        }
    }

    public static void ChangeMusicSectionByName(string sectionName, SceneGroup currentGroup)
    {
        Instance.ChangeMusicSectionByNameInternal(sectionName, currentGroup);
    }

    private void ChangeMusicSectionByNameInternal(string sectionName, SceneGroup currentGroup)
    {
        for (int i = 0; i < currentGroup.scenes.Length; i++)
        {
            for (int j = 0; j < currentGroup.scenes[i].songSections.Length; j++)
            {
                if (currentGroup.scenes[i].songSections[j].name == sectionName)
                {
                    int currentScene = i;
                    float currentSongSection = currentGroup.scenes[i].songSections[j].section;
                    ChangeSongSectionInternal(currentGroup, currentScene, currentSongSection);
                    break;
                }
            }
        }
    }

    public static void ChangeSongSection(SceneGroup currentGroup, int currentScene, float currentSongSection)
    {
        Instance.ChangeSongSectionInternal(currentGroup, currentScene, currentSongSection);
    }

    private void ChangeSongSectionInternal(SceneGroup currentGroup, int currentScene, float currentSongSection)
    {
        if (musicPlayback == null || currentGroup == null || currentGroup.scenes == null || 
            currentScene >= currentGroup.scenes.Length)
        {
            Debug.LogWarning("One or more references are null in changeSongSection, or currentScene is out of bounds.");
            return;
        }

        var songSections = currentGroup.scenes[currentScene].songSections;
        int sectionIndex = FindSectionIndex(songSections, currentSongSection);

        if (sectionIndex == -1)
        {
            Debug.LogWarning($"Could not find section with value {currentSongSection} in scene {currentScene}");
            return;
        }

        musicPlayback.EventInstance.setParameterByName("Sections", currentSongSection);

        string currentSectionName = songSections[sectionIndex].name;

        Debug.Log($"Song section changed to: {currentSectionName} (Section value: {currentSongSection})");
    }

    private int FindSectionIndex(SongSection[] songSections, float sectionValue)
    {
        for (int i = 0; i < songSections.Length; i++)
        {
            if (songSections[i].section == sectionValue)
            {
                return i;
            }
        }
        return -1;
    }

    public IEnumerator RewindMusic(bool isRewinding, float duration)
    {
        if (musicPlayback == null || !musicPlayback.EventInstance.isValid())
        {
            Debug.LogError("Music event instance is not valid");
            yield break;
        }

        musicEventInstance = musicPlayback.EventInstance;

        // Set the Rewind parameter
        musicEventInstance.setParameterByName("Rewind", isRewinding ? 1f : 0f);

        // Wait for the specified duration
        yield return new WaitForSeconds(duration);

        // If we were rewinding, turn off the rewind effect after the duration
        if (isRewinding)
        {
            musicEventInstance.setParameterByName("Rewind", 0f);
        }
    }
}