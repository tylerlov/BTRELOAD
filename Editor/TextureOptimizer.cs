using UnityEngine;
using UnityEditor;

public class TextureOptimizer : EditorWindow
{
  [MenuItem("Tools/Optimize Textures")]
  public static void ShowWindow()
  {
    GetWindow<TextureOptimizer>("Texture Optimizer");
  }

  void OnGUI()
  {
    EditorGUILayout.HelpBox("This tool will optimize all textures in your project. Make a backup first!", MessageType.Warning);
    
    if (GUILayout.Button("Optimize All Textures"))
    {
      if (EditorUtility.DisplayDialog("Confirm Optimization", 
          "Are you sure you want to optimize all textures? This cannot be undone.", 
          "Yes, Optimize", "Cancel"))
      {
        OptimizeAllTextures();
      }
    }
  }

  void OptimizeAllTextures()
  {
    string[] guids = AssetDatabase.FindAssets("t:texture");
    int count = 0;
    
    EditorUtility.DisplayProgressBar("Optimizing Textures", "Starting...", 0f);
    
    foreach (string guid in guids)
    {
      string path = AssetDatabase.GUIDToAssetPath(guid);
      TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
      
      if (importer != null)
      {
        EditorUtility.DisplayProgressBar("Optimizing Textures", 
          $"Processing: {path}", count / (float)guids.Length);
        
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.compressionQuality = 50;
        
        var androidSettings = importer.GetPlatformTextureSettings("Android");
        androidSettings.format = TextureImporterFormat.ASTC_6x6;
        androidSettings.overridden = true;
        importer.SetPlatformTextureSettings(androidSettings);
        
        importer.SaveAndReimport();
        count++;
      }
    }
    
    EditorUtility.ClearProgressBar();
    EditorUtility.DisplayDialog("Optimization Complete", 
      $"Successfully optimized {count} textures.", "OK");
    
    AssetDatabase.Refresh();
  }
} 