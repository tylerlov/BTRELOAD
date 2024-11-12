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
    public abstract class GPUITerrainManagerEditor<T> : GPUIManagerEditor where T : GPUIPrototypeData, new()
    {
        private GPUITerrainManager<T> _gpuiTerrainManager;
        private bool showEditModeAdditionalTerrains = true;

        protected override void OnEnable()
        {
            base.OnEnable();

            _gpuiTerrainManager = target as GPUITerrainManager<T>;
            _pickerAcceptModelPrefab = true;
            //_disableAddPrototypes = true;
            //_disableRemovePrototypes = true;
        }

        protected override void DrawManagerSettings()
        {
            base.DrawManagerSettings();

            _managerSettingsContentVE.Add(DrawSerializedProperty(serializedObject.FindProperty("_isAutoAddActiveTerrainsOnInitialization")));
            _managerSettingsContentVE.Add(new IMGUIContainer(DrawTerrainsProperty));

            _managerSettingsContentVE.Add(DrawSerializedProperty(serializedObject.FindProperty("_isAutoAddPrototypesBasedOnTerrains")));
            _managerSettingsContentVE.Add(DrawSerializedProperty(serializedObject.FindProperty("_isAutoRemovePrototypesBasedOnTerrains")));
        }

        private void DrawTerrainsProperty()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            EditorGUI.BeginChangeCheck();
            DrawIMGUISerializedProperty(serializedObject.FindProperty("_gpuiTerrains"));
            if (!Application.isPlaying && _gpuiTerrainManager.editor_EditModeAdditionalTerrains != null && _gpuiTerrainManager.editor_EditModeAdditionalTerrains.Count > 0)
                GPUIEditorUtility.DrawIMGUIList(ref showEditModeAdditionalTerrains, ref _gpuiTerrainManager.editor_EditModeAdditionalTerrains, "Terrains From Other Scenes", false);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                DrawManagerSettings();
                DrawPrototypeButtons();
            }
            if (!Application.isPlaying)
            {
                Terrain[] activeTerrains = Terrain.activeTerrains;
                if (!_gpuiTerrainManager.ContainsTerrains(activeTerrains))
                {
                    GPUIEditorUtility.DrawColoredButton(new GUIContent("Add Active Terrains"), GPUIEditorConstants.Colors.lightBlue, Color.white, FontStyle.Normal, Rect.zero, () =>
                    {
                        _gpuiTerrainManager.AddTerrains(activeTerrains);
                    });
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        protected override void DrawPrototypeButtons()
        {
            base.DrawPrototypeButtons();

            if (!Application.isPlaying && _gpuiTerrainManager.GetTerrainCount() > 0)
            {
                GPUIEditorTextUtility.TryGetGPUIText("terrainUpdatePrototypesButton", out GPUIEditorTextUtility.GPUIText gpuiText);
                Button loadPrototypesButton = new()
                {
                    text = gpuiText.title,
                    focusable = false,
                    tooltip = gpuiText.tooltip,
                };
                loadPrototypesButton.AddToClassList("gpui-pre-prototype-button");
                loadPrototypesButton.RegisterCallback<ClickEvent>((evt) => { _gpuiTerrainManager.ReloadTerrains(); _gpuiTerrainManager.ResetPrototypesFromTerrains(); DrawManagerSettings(); DrawPrototypeButtons(); });
                _prePrototypeButtonsVE.Add(loadPrototypesButton);
                GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, _prePrototypeButtonsVE);
            }
        }

        //protected override void DrawPrototypeSettings()
        //{
        //    if (_gpuiTerrainManager.GetTerrainCount() == 0)
        //    {
        //        _prototypeSettingsContentVE.Clear();
        //        return;
        //    }

        //    base.DrawPrototypeSettings();
        //}
    }
}