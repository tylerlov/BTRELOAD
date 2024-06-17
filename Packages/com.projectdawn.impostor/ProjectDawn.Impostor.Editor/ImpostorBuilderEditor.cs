using UnityEditor;
using UnityEngine;

namespace ProjectDawn.Impostor.Editor
{
    [CustomEditor(typeof(ImpostorBuilder))]
    public class ImpostorBuilderEditor : UnityEditor.Editor
    {
        SerializedProperty m_Graph;
        DragImpostor m_DragImpostor;
        MaterialEditor m_MaterialEditor;

        public static ImpostorBuilder DefaultImpostorBuilder => AssetDatabase.LoadAssetAtPath<ImpostorBuilder>("Packages/com.projectdawn.impostor/Content/Default Impostor Builder.asset");
        public static GameObject Suzzanne => AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.projectdawn.impostor/Content/Suzzanne.fbx");

        public ImpostorBuilder Builder => target as ImpostorBuilder;

        public override bool HasPreviewGUI() => m_MaterialEditor != null;

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            m_MaterialEditor.OnInteractivePreviewGUI(r, background);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            m_MaterialEditor.OnPreviewGUI(r, background);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Graph);

            bool valid = m_Graph.objectReferenceValue != null;

            if (valid)
            {
                EditorGUILayout.Space();

                var builder = target as ImpostorBuilder;
                var graph = builder.Graph;

                if (graph != null)
                {
                    var graphParameters = graph.exposedParameters;
                    EditorGUILayout.BeginVertical("box");
                    foreach (var param in graphParameters)
                    {
                        if (param.settings.isHidden)
                            continue;

                        var type = param.GetValueType();
                        if (type == typeof(GameObject))
                        {
                            EditorGUI.BeginChangeCheck();
                            GameObject value = null;
                            if (builder.Contains(param.name))
                            {
                                value = builder.GetGameObject(param.name);
                            }

                            value = EditorGUILayout.ObjectField(param.name, value, type, false) as GameObject;
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(target, $"Modified {param.name} in {target.name}");
                                builder.SetGameObject(param.name, value);
                                EditorUtility.SetDirty(target);
                            }

                            if (value == null)
                            {
                                Rect controlRect = EditorGUILayout.GetControlRect();
                                controlRect.height = 20;
                                EditorGUI.HelpBox(controlRect, "This parameter must be not null", MessageType.Warning);
                                valid = false;
                            }
                            else
                            {
                                if (value.GetComponentsInChildren<SkinnedMeshRenderer>().Length > 0)
                                {
                                    Rect controlRect = EditorGUILayout.GetControlRect();
                                    controlRect.height = 20;
                                    EditorGUI.HelpBox(controlRect, "Game Object contains skinned mesh, keep in mind impostors do not support animations.", MessageType.Warning);
                                }
                            }
                        }
                        else if (type == typeof(int))
                        {
                            EditorGUI.BeginChangeCheck();
                            int value = 0;
                            if (builder.Contains(param.name))
                            {
                                value = builder.GetInteger(param.name);
                            }

                            switch (param.name)
                            {
                                case "Frames":
                                    value = EditorGUILayoutImpostor.FramesField(param.name, value);
                                    break;
                                case "Resolution":
                                case "Resolution2":
                                    value = EditorGUILayoutImpostor.ResolutionField(param.name, value);
                                    break;
                                default:
                                    value = EditorGUILayout.IntField(param.name, value);
                                    break;
                            }

                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(target, $"Set {param.name} to {value} in {target.name}");
                                builder.SetInteger(param.name, value);
                                EditorUtility.SetDirty(target);
                            }
                        }
                        else if (type == typeof(float))
                        {
                            EditorGUI.BeginChangeCheck();
                            float value = 0;
                            if (builder.Contains(param.name))
                            {
                                value = builder.GetFloat(param.name);
                            }

                            value = EditorGUILayout.FloatField(param.name, value);

                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(target, $"Set {param.name} to {value} in {target.name}");
                                builder.SetFloat(param.name, value);
                                EditorUtility.SetDirty(target);
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var prefabPath = AssetDatabase.GetAssetPath(target);
            using (new EditorGUI.DisabledGroupScope(false))
            {
                if (GUILayout.Button("Clear", GUILayout.Width(100)))
                {
                    foreach (var dependency in AssetDatabase.LoadAllAssetsAtPath(prefabPath))
                    {
                        if (AssetDatabase.IsSubAsset(dependency))
                        {
                            AssetDatabase.RemoveObjectFromAsset(dependency);
                            GameObject.DestroyImmediate(dependency);
                        }
                    }

                    Builder.Impostor = null;

                    // Apply changes
                    AssetDatabase.SaveAssets();

                    // Re-import
                    AssetDatabase.ImportAsset(prefabPath);

                    RecreateMaterialEditor();
                }
            }
            using (new EditorGUI.DisabledGroupScope(!valid))
            {
                if (GUILayout.Button("Build", GUILayout.Width(100)))
                {
                    var impostor = Builder.Build();

                    if (impostor.Material == null)
                        throw new System.InvalidOperationException("Something failed in the build process. Material is null");
                    if (impostor.Mesh == null)
                        throw new System.InvalidOperationException("Something failed in the build process. Mesh is null");
                    if (impostor.Textures == null)
                        throw new System.InvalidOperationException("Something failed in the build process. Textures is null");

                    if (Builder.Impostor != null && Builder.Impostor.Textures != null)
                    {
                        foreach (var texture in Builder.Impostor.Textures)
                        {
                            AssetDatabase.RemoveObjectFromAsset(texture);
                        }
                    }
                    foreach (var texture in impostor.Textures)
                    {
                        AssetDatabase.AddObjectToAsset(texture, prefabPath);
                    }

                    // Add to prefab as sub-assets
                    if (Builder.Impostor != null && Builder.Impostor.Material != null)
                    {
                        Builder.Impostor.Material.CopyPropertiesFromMaterial(impostor.Material);
                    }
                    else
                    {
                        AssetDatabase.AddObjectToAsset(impostor.Material, prefabPath);
                    }

                    if (Builder.Impostor != null && Builder.Impostor.Mesh != null)
                    {
                        Builder.Impostor.Mesh.Clear();
                        Builder.Impostor.Mesh.vertices = impostor.Mesh.vertices;
                        Builder.Impostor.Mesh.triangles = impostor.Mesh.triangles;
                        Builder.Impostor.Mesh.bounds = impostor.Mesh.bounds;
                    }
                    else
                    {
                        AssetDatabase.AddObjectToAsset(impostor.Mesh, prefabPath);
                    }

                    if (Builder.Impostor == null)
                        Builder.Impostor = impostor;
                    Builder.Impostor.Textures = impostor.Textures;

                    EditorUtility.SetDirty(target);

                    // Apply changes
                    AssetDatabase.SaveAssets();

                    // Re-import
                    AssetDatabase.ImportAsset(prefabPath);

                    RecreateMaterialEditor();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (m_MaterialEditor != null)
            {
                EditorGUILayout.Space();

                m_MaterialEditor.DrawHeader();
                m_MaterialEditor.OnInspectorGUI();
            }

            serializedObject.ApplyModifiedProperties();
        }

        void RecreateMaterialEditor()
        {
            if (m_MaterialEditor)
                DestroyImmediate(m_MaterialEditor);

            if (Builder.Impostor == null || !Builder.Impostor.Material)
                return;

            m_MaterialEditor = MaterialEditor.CreateEditor(Builder.Impostor.Material) as MaterialEditor;
        }

        public static ImpostorBuilder CreateDefaultImpostorBuilder(string path)
        {
            var newAsset = ScriptableObject.CreateInstance<ImpostorBuilder>();
            newAsset.Graph = ImpostorGraphEditor.DefaultImpostorGraph;
            newAsset.SetGameObject("Source", Suzzanne);
            newAsset.SetInteger("Resolution", 2048);
            newAsset.SetInteger("Resolution2", 1024);
            newAsset.SetInteger("Frames", 12);
            AssetDatabase.CreateAsset(newAsset, path);
            return newAsset;
        }

        internal void OnSceneDrag(SceneView sceneView, int index)
        {
            var impostor = Builder.Impostor;
            if (impostor == null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.None;
                return;
            }

            m_DragImpostor.OnSceneDrag(target.name, impostor.Material, impostor.Mesh);
        }

        private void OnEnable()
        {
            m_Graph = serializedObject.FindProperty("m_Graph");
            m_DragImpostor = new();
            RecreateMaterialEditor();
        }

        static class EditorGUILayoutImpostor
        {
            static readonly int[] Frames = { 6, 8, 10, 12, 14 };
            static readonly string[] FrameNames = { "6x6", "8x8", "10x10", "12x12", "14x14" };

            public static int FramesField(string label, int value)
            {
                int index = 0;
                for (int i = 0; i < Frames.Length; i++)
                {
                    if (Frames[i] == value)
                    {
                        index = i;
                        break;
                    }
                }

                index = EditorGUILayout.Popup(label, index, FrameNames);
                return Frames[index];
            }

            static readonly int[] Resolutions = { 256, 512, 1024, 2048, 4096 };
            static readonly string[] ResolutionNames = { "256", "512", "1024", "2048", "4096" };

            public static int ResolutionField(string label, int value)
            {
                int index = 0;
                for (int i = 0; i < Resolutions.Length; i++)
                {
                    if (Resolutions[i] == value)
                    {
                        index = i;
                        break;
                    }
                }

                index = EditorGUILayout.Popup(label, index, ResolutionNames);
                return Resolutions[index];
            }
        }

        class DragImpostor
        {
            GameObject m_Instance;

            public void OnSceneDrag(string name, Material material, Mesh mesh)
            {
                Event e = Event.current;

                if (m_Instance == null)
                {
                    m_Instance = new GameObject(name, typeof(MeshRenderer), typeof(MeshFilter));
                    m_Instance.GetComponent<MeshRenderer>().sharedMaterial = material;
                    m_Instance.GetComponent<MeshFilter>().sharedMesh = mesh;
                }

                switch (e.type)
                {
                    case EventType.DragUpdated:
                        if (HandleUtility.PlaceObject(e.mousePosition, out Vector3 position, out _))
                            m_Instance.transform.position = position;
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        e.Use();
                        break;
                    case EventType.DragPerform:
                        DragAndDrop.AcceptDrag();
                        e.Use();
                        Undo.RegisterCreatedObjectUndo(m_Instance, m_Instance.name);
                        m_Instance = null;
                        break;
                    case EventType.DragExited:
                        GameObject.DestroyImmediate(m_Instance);
                        e.Use();
                        break;
                }
            }
        }
    }
}
