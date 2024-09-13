using System.Collections;
using FMODUnity;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField]
    private StudioEventEmitter musicPlayback;

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

    public void ApplyMusicChanges(
        SceneGroup currentGroup,
        int currentScene,
        float currentSongSection
    )
    {
        if (
            musicPlayback != null
            && currentGroup != null
            && currentScene < currentGroup.scenes.Length
        )
        {
            musicPlayback.SetParameter("Sections", currentSongSection);
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
                    ChangeSongSection(
                        currentGroup,
                        i,
                        currentGroup.scenes[i].songSections[j].section
                    );
                    return;
                }
            }
        }
        Debug.LogWarning($"Section '{sectionName}' not found in the current group.");
    }

    public void ChangeSongSection(
        SceneGroup currentGroup,
        int currentScene,
        float currentSongSection
    )
    {
        if (
            musicPlayback == null
            || currentGroup == null
            || currentGroup.scenes == null
            || currentScene >= currentGroup.scenes.Length
        )
        {
            Debug.LogWarning("Invalid parameters in ChangeSongSection.");
            return;
        }

        var songSections = currentGroup.scenes[currentScene].songSections;
        int sectionIndex = System.Array.FindIndex(
            songSections,
            section => section.section == currentSongSection
        );

        if (sectionIndex == -1)
        {
            Debug.LogWarning(
                $"Could not find section with value {currentSongSection} in scene {currentScene}"
            );
            return;
        }

        SetMusicParameter("Sections", currentSongSection);
        Debug.Log(
            $"Song section changed to: {songSections[sectionIndex].name} (Section value: {currentSongSection})"
        );
    }

    public void SetMusicParameter(string parameterName, float value)
    {
        if (musicPlayback == null || !musicPlayback.EventInstance.isValid())
        {
            Debug.LogError(
                $"Failed to set music parameter '{parameterName}': Invalid event instance"
            );
            return;
        }

        FMOD.Studio.PLAYBACK_STATE playbackState;
        musicPlayback.EventInstance.getPlaybackState(out playbackState);

        if (playbackState != FMOD.Studio.PLAYBACK_STATE.PLAYING)
        {
            Debug.LogWarning("FMOD event is not playing. Starting playback.");
            musicPlayback.Play();
        }

        FMOD.RESULT result = musicPlayback.EventInstance.setParameterByName(parameterName, value);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to set music parameter '{parameterName}': {result}");
        }
    }
}
