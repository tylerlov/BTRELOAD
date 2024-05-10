using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode] // This will ensure that the script also runs in the editor
public class ButoParent : MonoBehaviour
{
    // This will appear as a drop-down list in the inspector, populated with all available tags.
    public string parentTag;

    void Awake()
    {
        // Since Awake is called in the editor as well, we check if we are actually playing
        if (Application.isPlaying)
        {
            ParentToObjectWithTag();
        }
    }

    private void ParentToObjectWithTag()
    {
        // Find all game objects with the selected tag.
        GameObject[] parentObjects = GameObject.FindGameObjectsWithTag(parentTag);

        // If there is at least one object with the tag, parent this object to the first one found.
        if (parentObjects.Length > 0)
        {
            transform.SetParent(parentObjects[0].transform, false);
        }
        else
        {
            Debug.LogError("No object with tag " + parentTag + " found.");
        }
    }
    
}
