using FMODUnity;
using UnityEngine;

public class FmodOneshots : MonoBehaviour
{
    [SerializeField]
    private int ouroborosStartIndex = 1; // Start index can be set in the Inspector

    public void PlayOuroborosStart()
    {
        // Construct the event path dynamically based on the current index
        string eventPath = $"event:/Ouroboros/Start{ouroborosStartIndex}";
        RuntimeManager.PlayOneShot(eventPath);

        // Increment the index for the next call
        ouroborosStartIndex++;
    }
}
