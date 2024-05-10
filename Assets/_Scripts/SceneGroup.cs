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

    // Call this method to update scene names based on SceneAssets
    #if UNITY_EDITOR
    public void UpdateSceneNames()
    {
        scenes = new string[sceneAssets.Length];
        for (int i = 0; i < sceneAssets.Length; i++)
        {
            scenes[i] = sceneAssets[i].name;
        }
    }
    #endif
}
