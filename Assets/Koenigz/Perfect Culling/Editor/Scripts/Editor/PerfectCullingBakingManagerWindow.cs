// Perfect Culling (C) 2023 Patrick König
//

#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Koenigz.PerfectCulling
{
    public class PerfectCullingBakingManagerWindow : EditorWindow
    {
        private class SceneState
        {
            public Scene Scene;
            public bool Enabled;
        }

        private enum BakeMode
        {
            OtherSceneRenderersAreOccluders,
            Independent,
        }

        private Dictionary<string, SceneState> _states = new Dictionary<string, SceneState>();
        private BakeMode _bakeMode;
        
        public void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _bakeMode = (BakeMode) EditorGUILayout.EnumPopup("Bake Mode", _bakeMode);

                switch (_bakeMode)
                {
                    case BakeMode.OtherSceneRenderersAreOccluders:
                        EditorGUILayout.HelpBox("This option will treat renderers from all volumes in all loaded scenes as occluders. This can be useful when you split your level up into multiple chunks.", MessageType.Info);
                        break;
                    
                    case BakeMode.Independent:
                        EditorGUILayout.HelpBox("This option will bake each PerfectCullingVolume independently. Renderers from other volumes and scenes are not occluders.", MessageType.Info);
                        break;
                }
                
                GUILayout.Label("Currently loaded scenes:");
                
                List<Scene> scenes = new List<Scene>();

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);

                    scenes.Add(scene);
                }

                List<Scene> actualScenes = new List<Scene>();

                for (int i = 0; i < scenes.Count; i++)
                {
                    Scene scene = scenes[i];

                    if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
                    {
                        // Exclude scenes that are not already saved on disk such as the untitled scene

                        continue;
                    }

                    if (_states.TryGetValue(scene.path, out _))
                    {
                        continue;
                    }

                    actualScenes.Add(scene);
                    
                    _states.Add(scene.path, new SceneState()
                    {
                        Scene = scenes[i],
                        Enabled = scene.isLoaded,
                    });
                }

                foreach (var s in _states)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (!s.Value.Enabled)
                        {
                            GUI.enabled = s.Value.Scene.isLoaded;
                        }
                        
                        s.Value.Enabled = EditorGUILayout.ToggleLeft(s.Value.Scene.name, s.Value.Enabled);
                        GUILayout.Label(s.Value.Scene.isLoaded ? "Loaded" : "Unloaded");

                        GUI.enabled = true;
                    }
                }

                if (GUILayout.Button("Bake"))
                {
                    PerfectCullingBakingManager.BakeMultiScene(_states.Where(x => x.Value.Enabled).Select(x => x.Value.Scene).ToList(), _bakeMode == BakeMode.OtherSceneRenderersAreOccluders);

                    Close();
                }
            }
        }
    }
}

#endif