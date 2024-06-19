using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
[CreateAssetMenu(fileName = "SceneGroup", menuName = "ScriptableObjects/SceneGroup", order = 1)]
public class SceneGroup : ScriptableObject
{
    public string areaName;
    #if UNITY_EDITOR
    public SceneAsset[] sceneAssets; // Use in the editor for drag-and-drop
    #endif
    public SceneInfo[] scenes; // For runtime use

    // Call this method to update scene names based on SceneAssets
    #if UNITY_EDITOR
    public void UpdateSceneNames()
    {
        if (sceneAssets == null) return;

        if (scenes == null || scenes.Length != sceneAssets.Length)
        {
            scenes = new SceneInfo[sceneAssets.Length];
        }

        for (int i = 0; i < sceneAssets.Length; i++)
        {
            if (scenes[i] == null)
            {
                scenes[i] = new SceneInfo();
            }

            scenes[i].sceneName = sceneAssets[i] != null ? sceneAssets[i].name : "";

            // Initialize SongSections if they are null
            if (scenes[i].songSections == null || scenes[i].songSections.Length == 0)
            {
                scenes[i].songSections = new SongSection[1]; // Default to one SongSection
                scenes[i].songSections[0] = new SongSection(); // Initialize with default values
            }
        }
    }

    // This method is called when the script is loaded or a value is changed in the Inspector
    private void OnValidate()
    {
        UpdateSceneNames();
    }
    #endif
}

[System.Serializable]
public class SceneInfo
{
    public string sceneName;
    public SongSection[] songSections; // Multiple SongSections per SceneInfo
}

[System.Serializable]
public class SongSection
{
    public string name;
    public float section; // Changed from int to float
    public int waves;
}