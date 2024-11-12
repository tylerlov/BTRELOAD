using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace Mirage.Impostors
{
    public class ImpostorFormatConverter
    {
        public GameObject impostor;
        public TextureFormat targetFormatAlbedo = TextureFormat.RGBA32;
        public TextureFormat targetFormatMask = TextureFormat.RGBA32;
        public TextureFormat targetFormatNormals = TextureFormat.RGB24;

        public static bool CheckValidity(GameObject impostorCandidate)
        {
            bool isImpostor = impostorCandidate.GetComponent<MeshRenderer>() != null && impostorCandidate.GetComponent<MeshRenderer>().sharedMaterial.shader.name.Contains("mpostor");
            return isImpostor && impostorCandidate.scene.name == null;
        }

        public static bool DisplayImpostor4ChannelsTextureFormat(Enum format)
        {
            switch (format)
            {
                case TextureFormat.ARGB32:
                case TextureFormat.ARGB4444:
                case TextureFormat.BGRA32:
                case TextureFormat.RGBA32:
                case TextureFormat.RGBA4444:
                case TextureFormat.RGBA64:
                case TextureFormat.RGBAFloat:
                    return true;
                default:
                    return false;
            }
        }

        public static bool DisplayImpostorNormalsTextureFormat(Enum format)
        {
            switch (format)
            {
                case TextureFormat.RGB24:
                case TextureFormat.RGB48:
                case TextureFormat.RGB565:
                case TextureFormat.RGB9e5Float:
                    return true;
                default:
                    return false;
            }
        }

        public void ReplaceCompression()
        {
            string sourcePrefabPath = AssetDatabase.GetAssetPath(impostor);
            UnityEngine.Object[] packedObjects = AssetDatabase.LoadAllAssetsAtPath(sourcePrefabPath);
            Material packedMaterial = AssetDatabase.LoadAssetAtPath<Material>(sourcePrefabPath);
            
            foreach (UnityEngine.Object o in packedObjects)
            {
                if (o.GetType() == typeof(Texture2D))
                {
                    Texture2D sourceTexture = o as Texture2D;
                    switch (sourceTexture.format)
                    {
                        case TextureFormat.ARGB32:
                        case TextureFormat.ARGB4444:
                        case TextureFormat.BGRA32:
                        case TextureFormat.RGBA32:
                        case TextureFormat.RGBA4444:
                        case TextureFormat.RGBA64:
                        case TextureFormat.RGBAFloat:
                        case TextureFormat.DXT5:
                        case TextureFormat.DXT5Crunched:
                            {
                                Texture2D targetTexture = new Texture2D(sourceTexture.width, sourceTexture.height, targetFormatAlbedo, false);
                                targetTexture.name = sourceTexture.name;
                                targetTexture.SetPixels(sourceTexture.GetPixels());
                                AssetDatabase.RemoveObjectFromAsset(o);
                                AssetDatabase.AddObjectToAsset(targetTexture, impostor);
                                packedMaterial.SetTexture("_MainTex", targetTexture);
                                Debug.Log("Converted " + sourcePrefabPath + " " + sourceTexture.name + " from " + sourceTexture.format + " to " + targetFormatAlbedo);
                            }
                            break;

                        case TextureFormat.RGB24:
                        case TextureFormat.RGB48:
                        case TextureFormat.RGB565:
                        case TextureFormat.RGB9e5Float:
                        case TextureFormat.DXT1:
                        case TextureFormat.DXT1Crunched:
                            {
                                Texture2D targetTexture = new Texture2D(sourceTexture.width, sourceTexture.height, targetFormatNormals, false);
                                targetTexture.name = sourceTexture.name;
                                targetTexture.SetPixels(sourceTexture.GetPixels());
                                AssetDatabase.RemoveObjectFromAsset(o);
                                AssetDatabase.AddObjectToAsset(targetTexture, impostor);
                                packedMaterial.SetTexture("_NormalMap", targetTexture);
                                Debug.Log("Converted " + sourcePrefabPath + " " + sourceTexture.name + " from " + sourceTexture.format + " to " + targetFormatNormals);
                            }
                            break;
                    }
                }
            }
            PrefabUtility.SavePrefabAsset(impostor);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(sourcePrefabPath);
        }
    }

    public class FormatConverterWindow : EditorWindow
    {
        private ImpostorFormatConverter converter;
        private List<GameObject> impostorPrefabs;
        private GUIStyle centeredStyle;
        private GUIStyle warningLabelStyle;
        private GUIStyle dropBoxStyle;
        private Vector2 scrollPosition;

        [MenuItem("Window/Mirage/Format Converter")]
        public static void ShowWindow()
        {
            EditorWindow win = GetWindow(typeof(FormatConverterWindow));
            win.titleContent = new GUIContent("Impostor Format Converter", Resources.Load<Texture>("MirageIcon"));
        }


        private void OnEnable()
        {
            converter = new ImpostorFormatConverter();
            impostorPrefabs = new List<GameObject>();
        }

        private void OnGUI()
        {
            if (centeredStyle == null)
            {
                centeredStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold

                };
            }
            if (warningLabelStyle == null)
            {
                warningLabelStyle = new GUIStyle(EditorStyles.label);
                warningLabelStyle.normal.textColor = Color.yellow;
                warningLabelStyle.fontSize = 10;
                warningLabelStyle.alignment = TextAnchor.MiddleCenter;
                warningLabelStyle.wordWrap = true;
            }
            if (dropBoxStyle == null)
            {
                dropBoxStyle = new GUIStyle(GUI.skin.box);
                dropBoxStyle.alignment = TextAnchor.MiddleCenter;
                dropBoxStyle.fontSize = 10;
                dropBoxStyle.hover.textColor = Color.green;
            }

            GUILayout.Label(Resources.Load<Texture>("MirageLogo"), centeredStyle, GUILayout.Height(96), GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox(new GUIContent("Some platforms are not compatible with DXT compression. You can mass convert your impostors to any other format here."));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            Rect dropBoxRect = GUILayoutUtility.GetRect(0, 16, GUILayout.Height(24), GUILayout.Width(156), GUILayout.ExpandWidth(true));
            GUI.skin.box = dropBoxStyle;
            GUI.SetNextControlName("DragDropBox");
            GUI.Box(dropBoxRect, "Drag and Drop impostor prefabs here", dropBoxStyle);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button(EditorGUIUtility.IconContent("CrossIcon"), GUILayout.Width(30), GUILayout.Height(30)))
                impostorPrefabs.Clear();
            EditorGUILayout.EndHorizontal();
            if (dropBoxRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    GUI.FocusControl("DragDropBox");
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.DragPerform)
                {
                    for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                    {
                        if ((DragAndDrop.objectReferences[i] as GameObject).GetComponent<Renderer>() != null)
                        {
                            bool found = false;
                            for (int j = 0; j < impostorPrefabs.Count; ++j)
                            {
                                if (impostorPrefabs[j] == (DragAndDrop.objectReferences[i] as GameObject))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                impostorPrefabs.Add(DragAndDrop.objectReferences[i] as GameObject);
                            }
                        }
                    }
                    Event.current.Use();
                }
            }
            else
            {
                if (GUI.GetNameOfFocusedControl() == "DragDropBox")
                {
                    GUI.FocusControl(null);
                }
            }


            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);
            for (int i = 0; i < impostorPrefabs.Count; ++i)
                impostorPrefabs[i] = EditorGUILayout.ObjectField(impostorPrefabs[i], typeof(GameObject), false) as GameObject;
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            converter.targetFormatAlbedo = (TextureFormat)EditorGUILayout.EnumPopup(new GUIContent("New Albedo Atlas Format"), converter.targetFormatAlbedo, ImpostorFormatConverter.DisplayImpostor4ChannelsTextureFormat, false);
            converter.targetFormatMask = (TextureFormat)EditorGUILayout.EnumPopup(new GUIContent("New Albedo Atlas Format"), converter.targetFormatMask, ImpostorFormatConverter.DisplayImpostor4ChannelsTextureFormat, false);
            converter.targetFormatNormals = (TextureFormat)EditorGUILayout.EnumPopup(new GUIContent("New Normals Atlas Format"), converter.targetFormatNormals, ImpostorFormatConverter.DisplayImpostorNormalsTextureFormat, false);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Separator();

            bool valid = impostorPrefabs.Count > 0 ? true : false;

            for (int i = 0; i < impostorPrefabs.Count; ++i)
            {
                valid &= ImpostorFormatConverter.CheckValidity(impostorPrefabs[i]);
                if (!valid)
                    break;
            }

            if (converter != null && valid)
            {
                if (GUILayout.Button(new GUIContent("Convert Textures Format", Resources.Load<Texture>("MirageIcon")), GUILayout.Height(42)))
                {
                    for (int i = 0; i < impostorPrefabs.Count; ++i)
                    {
                        converter.impostor = impostorPrefabs[i];
                        converter.ReplaceCompression();
                    }
                    impostorPrefabs.Clear();
                }
            }
            else
            {
                if(impostorPrefabs.Count == 0)
                    EditorGUILayout.HelpBox(new GUIContent("Please drag impostor prefabs in the drop area above"));
                else
                    EditorGUILayout.HelpBox(new GUIContent("Error: Some prefabs are not impostors."));
            }

        }
    }
   
}


