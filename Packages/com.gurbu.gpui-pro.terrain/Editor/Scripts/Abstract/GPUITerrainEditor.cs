// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro.TerrainModule
{
    public class GPUITerrainEditor : GPUIEditor
    {
        private GPUITerrain _gpuiTerrain;
        private bool _detailRenderTexturesFoldout = false;
        private bool _detailBakedTexturesFoldout = false;

        public override void DrawIMGUIContainer()
        {
            if (_gpuiTerrain == null)
                _gpuiTerrain = target as GPUITerrain;

            EditorGUI.BeginChangeCheck();
            DrawIMGUISerializedProperty(serializedObject.FindProperty("isAutoFindTreeManager"));
            DrawIMGUISerializedProperty(serializedObject.FindProperty("isAutoFindDetailManager"));
            DrawIMGUISerializedProperty(serializedObject.FindProperty("terrainHolesSampleMode"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Heightmap Texture", _gpuiTerrain.GetHeightmapTexture(), typeof(RenderTexture), false);
            EditorGUI.EndDisabledGroup();

            int detailTextureCount = _gpuiTerrain.GetDetailTextureCount();
            if (detailTextureCount > 0)
            {
                _detailRenderTexturesFoldout = EditorGUILayout.Foldout(_detailRenderTexturesFoldout, "Detail Render Textures", true);
                if (_detailRenderTexturesFoldout)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    for (int i = 0; i < detailTextureCount; i++)
                    {
                        EditorGUILayout.ObjectField("Layer " + i, _gpuiTerrain.GetDetailDensityTexture(i), typeof(RenderTexture), false);
                    }
                    EditorGUI.EndDisabledGroup();
                }

                if (_gpuiTerrain.IsBakedDetailTextures())
                {
                    EditorGUILayout.Space(10);
                    _detailBakedTexturesFoldout = EditorGUILayout.Foldout(_detailBakedTexturesFoldout, "Baked Detail Textures", true);
                    if (_detailBakedTexturesFoldout)
                    {
                        SerializedProperty isCustomBakedDetailTexturesSP = serializedObject.FindProperty("_isCustomBakedDetailTextures");
                        EditorGUI.BeginChangeCheck();
                        DrawIMGUISerializedProperty(isCustomBakedDetailTexturesSP);
                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedObject.ApplyModifiedProperties();
                            serializedObject.Update();
                        }
                        EditorGUI.BeginDisabledGroup(!isCustomBakedDetailTexturesSP.boolValue);
                        for (int i = 0; i < detailTextureCount; i++)
                        {
                            EditorGUI.BeginChangeCheck();
                            Texture2D bakedDT = (Texture2D)EditorGUILayout.ObjectField("Layer " + i, _gpuiTerrain.GetBakedDetailTexture(i), typeof(Texture2D), false);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(_gpuiTerrain, "Baked texture changed.");
                                _gpuiTerrain.SetBakedDetailTexture(i, bakedDT);
                            }
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                }
            }

            EditorGUI.BeginDisabledGroup(true);
            if (_gpuiTerrain.TreeManager != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.ObjectField("Tree Manager", _gpuiTerrain.TreeManager, typeof(GPUIDetailManager), false);
                string treePrototypeIndexes = _gpuiTerrain.GetTreePrototypeIndexesToString();
                if (!string.IsNullOrEmpty(treePrototypeIndexes))
                    EditorGUILayout.TextField("Tree Prototype Indexes", treePrototypeIndexes);
            }
            if (_gpuiTerrain.DetailManager != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.ObjectField("Detail Manager", _gpuiTerrain.DetailManager, typeof(GPUIDetailManager), false);
                string detailPrototypeIndexes = _gpuiTerrain.GetDetailPrototypeIndexesToString();
                if (!string.IsNullOrEmpty(detailPrototypeIndexes))
                    EditorGUILayout.TextField("Detail Prototype Indexes", detailPrototypeIndexes);
            }
            EditorGUI.EndDisabledGroup();

            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                GPUIEditorUtility.DrawColoredButton(new GUIContent("Reload Terrain Data"), GPUIEditorConstants.Colors.lightBlue, Color.white, FontStyle.Bold, Rect.zero, () =>
                {
                    _gpuiTerrain.enabled = false;
                    _gpuiTerrain.enabled = true;
                    if (_gpuiTerrain.DetailManager != null)
                        _gpuiTerrain.DetailManager.OnTerrainsModified();
                    if (_gpuiTerrain.TreeManager != null)
                        _gpuiTerrain.TreeManager.OnTerrainsModified();
                });
            }
        }

        //public override void DrawContentGUI(VisualElement contentElement)
        //{
        //    RenderTexture heightmapTexture = _gpuiTerrain.GetHeightmapTexture();
        //    ObjectField heightmapField = new("Heightmap Texture")
        //    {
        //        focusable = false,
        //        value = heightmapTexture,
        //        objectType = typeof(RenderTexture),
        //        allowSceneObjects = false
        //    };
        //    heightmapField.SetEnabled(false);
        //    contentElement.Add(heightmapField);

        //    RenderTexture[] detailTextures = _gpuiTerrain.GetDetailTextures();
        //    if (detailTextures != null && detailTextures.Length > 0)
        //    {
        //        Foldout foldout = new();
        //        foldout.text = "Detail Textures";
        //        for (int i = 0; i < detailTextures.Length; i++)
        //        {
        //            ObjectField detailTextureField = new("Layer " + i)
        //            {
        //                focusable = false,
        //                value = detailTextures[i],
        //                objectType = typeof(RenderTexture),
        //                allowSceneObjects = false
        //            };
        //            detailTextureField.SetEnabled(false);
        //            foldout.Add(detailTextureField);
        //        }
        //        contentElement.Add(foldout);
        //    }
        //}

        public override string GetTitleText()
        {
            return "GPUI Terrain";
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#GPUI_Terrain";
        }
    }
}