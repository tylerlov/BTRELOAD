using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable] // Make sure Unity can serialize this class
public class SceneGroup
{
    public string areaName;
    #if UNITY_EDITOR
    public SceneAsset[] sceneAssets; // Use in the editor for drag-and-drop
    #endif
    public string[] scenes; // For runtime use
    public string[] musicSectionNames; // Array to hold music section names for each scene

    // Call this method to update scene names based on SceneAssets
    #if UNITY_EDITOR
    public void UpdateSceneNames()
    {
        if (sceneAssets == null) return;

        scenes = new string[sceneAssets.Length];
        musicSectionNames = new string[sceneAssets.Length]; // Initialize the music names array
        for (int i = 0; i < sceneAssets.Length; i++)
        {
            scenes[i] = sceneAssets[i] != null ? sceneAssets[i].name : "";
            musicSectionNames[i] = ""; // Initialize with empty strings or default values
        }
    }

    // This method is called when the script is loaded or a value is changed in the Inspector
    private void OnValidate()
    {
        UpdateSceneNames();
    }
    #endif
}
