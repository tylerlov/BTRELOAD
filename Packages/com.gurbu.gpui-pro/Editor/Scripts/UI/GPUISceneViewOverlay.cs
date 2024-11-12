// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using UnityEngine;
using UnityEditor.Toolbars;
using UnityEditor.Overlays;
using UnityEngine.UIElements;
using UnityEditor;
using System.Collections.Generic;

namespace GPUInstancerPro
{
    [Overlay(typeof(SceneView), "GPUI Pro")]
    [Icon("Assets/unity.png")]
    public class GPUISceneViewOverlay : ToolbarOverlay
    {
        GPUISceneViewOverlay() : base(GPUISceneViewOverlayRenderModeToggle.id)
        { }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class GPUISceneViewOverlayRenderModeToggle : EditorToolbarToggle, IAccessContainerWindow
    {
        public const string id = "GPUISceneViewOverlay/RenderModeToggle";
        private Texture2D OnIcon;
        private Texture2D OffIcon;

        public EditorWindow containerWindow { get; set; }

        public GPUISceneViewOverlayRenderModeToggle()
        {
            text = "GPUI";
            tooltip = "GPUI Pro, switch between Culled view and Full view.";
            style.paddingLeft = 4;

            try
            {
                OnIcon = EditorGUIUtility.IconContent("animationvisibilitytoggleon").image as Texture2D;
                OffIcon = EditorGUIUtility.IconContent("animationvisibilitytoggleoff").image as Texture2D;
            }
            catch(Exception e) { Debug.LogException(e); }

            SetIcon(value);
            this.RegisterValueChangedCallback(ChangeRenderMode);
            this.RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            value = GPUIRenderingSystem.Editor_ContainsSceneViewCameraData(GetActiveSceneViewCamera());
        }

        void ChangeRenderMode(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                GPUIRenderingSystem.Editor_AddSceneViewCameraData(GetActiveSceneViewCamera());
            else
                GPUIRenderingSystem.Editor_RemoveSceneViewCameraData(GetActiveSceneViewCamera());
            SetIcon(evt.newValue);
        }

        Camera GetActiveSceneViewCamera()
        {
            if (containerWindow is SceneView view)
                return view.camera;
            return null;
        }

        void SetIcon(bool value)
        {
            if (value)
                icon = OnIcon;
            else
                icon = OffIcon;
        }
    }
}