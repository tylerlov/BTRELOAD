using FMODUnity;
using UnityEngine;
using System.Collections;

public class FmodOneshots : MonoBehaviour
{
    [SerializeField]
    private int ouroborosStartIndex = 1; // Start index can be set in the Inspector

    public void PlayOuroborosStart()
    {
        string eventPath = $"event:/Ouroboros/Start{ouroborosStartIndex}";
        var instance = AudioManager.Instance.GetOrCreateInstance(eventPath);
        instance.start();
        // Don't release immediately - let the sound play first!
        StartCoroutine(ReleaseAfterPlay(eventPath, instance));
        ouroborosStartIndex++;
    }

    private IEnumerator ReleaseAfterPlay(string eventPath, FMOD.Studio.EventInstance instance)
    {
        FMOD.Studio.PLAYBACK_STATE state;
        do
        {
            instance.getPlaybackState(out state);
            yield return null;
        } while (state != FMOD.Studio.PLAYBACK_STATE.STOPPED);

        AudioManager.Instance.ReleaseInstance(eventPath, instance);
    }
}
