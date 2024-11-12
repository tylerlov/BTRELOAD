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
    [CustomEditor(typeof(GPUITerrainBuiltin))]
    public class GPUITerrainBuiltinEditor : GPUITerrainEditor
    {
        private GPUITerrainBuiltin _gpuiTerrainBuiltin;

        public override void DrawIMGUIContainer()
        {
            if (_gpuiTerrainBuiltin == null)
                _gpuiTerrainBuiltin = target as GPUITerrainBuiltin;
            base.DrawIMGUIContainer();

            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                GPUIEditorTextUtility.GPUIText gpuiText;
                if (!_gpuiTerrainBuiltin.IsDetailDensityTexturesLoaded)
                {
                    GPUIEditorTextUtility.TryGetGPUIText("createDetailRenderTexturesButton", out gpuiText);
                    GPUIEditorUtility.DrawColoredButton(new GUIContent(gpuiText.title), GPUIEditorConstants.Colors.lightGreen, Color.white, FontStyle.Bold, Rect.zero, () =>
                    {
                        _gpuiTerrainBuiltin.CreateDetailTextures();
                    });
                    if (_isShowHelpText)
                        GPUIEditorUtility.DrawIMGUIHelpText(gpuiText.helpText);
                }
                else
                {
                    if (_gpuiTerrainBuiltin.IsBakedDetailTextures())
                    {
                        GPUIEditorTextUtility.TryGetGPUIText("bakedDetailTexturesWarning", out gpuiText);
                        if (!string.IsNullOrEmpty(gpuiText.helpText))
                            EditorGUILayout.HelpBox(gpuiText.helpText, MessageType.Warning);
                    }
                    EditorGUILayout.BeginHorizontal();
                    GPUIEditorTextUtility.TryGetGPUIText("bakeDetailTexturesButton", out gpuiText);
                    string bakeDetailTexturesHelpText = gpuiText.helpText;
                    GPUIEditorUtility.DrawColoredButton(new GUIContent(gpuiText.title, gpuiText.tooltip), GPUIEditorConstants.Colors.lightGreen, Color.white, FontStyle.Bold, Rect.zero, () =>
                    {
                        _gpuiTerrainBuiltin.Editor_EnableBakedDetailTextures();
                        _gpuiTerrainBuiltin.Editor_SaveDetailRenderTexturesToBakedTextures();
                        EditorUtility.SetDirty(_gpuiTerrainBuiltin);
                    });
                    if (_gpuiTerrainBuiltin.IsBakedDetailTextures())
                    {
                        GPUIEditorTextUtility.TryGetGPUIText("deleteBakedDetailTexturesButton", out gpuiText);
                        GPUIEditorUtility.DrawColoredButton(new GUIContent(gpuiText.title, gpuiText.tooltip), GPUIEditorConstants.Colors.lightRed, Color.white, FontStyle.Bold, Rect.zero, () =>
                        {
                            _gpuiTerrainBuiltin.Editor_DeleteBakedDetailTextures();
                            EditorUtility.SetDirty(_gpuiTerrainBuiltin);
                        });
                    }
                    EditorGUILayout.EndHorizontal();
                    if (_isShowHelpText)
                        GPUIEditorUtility.DrawIMGUIHelpText(bakeDetailTexturesHelpText);
                }
            }
        }
    }
}