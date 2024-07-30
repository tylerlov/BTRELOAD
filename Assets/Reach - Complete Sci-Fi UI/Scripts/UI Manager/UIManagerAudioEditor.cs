#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using FMODUnity; // Add FMODUnity namespace

namespace Michsky.UI.Reach
{
    [CustomEditor(typeof(UIManagerAudio))]
    public class UIManagerAudioEditor : Editor
    {
        private UIManagerAudio uimaTarget;
        private GUISkin customSkin;

        private SerializedProperty UIManagerAsset;
        private SerializedProperty fmodEventEmitter;
        private SerializedProperty masterSlider;
        private SerializedProperty musicSlider;
        private SerializedProperty SFXSlider;
        private SerializedProperty UISlider;

        private void OnEnable()
        {
            uimaTarget = (UIManagerAudio)target;

            customSkin = EditorGUIUtility.isProSkin 
                ? ReachUIEditorHandler.GetDarkEditor(customSkin) 
                : ReachUIEditorHandler.GetLightEditor(customSkin);

            // Initialize SerializedProperties
            UIManagerAsset = serializedObject.FindProperty("UIManagerAsset");
            fmodEventEmitter = serializedObject.FindProperty("fmodEventEmitter");
            masterSlider = serializedObject.FindProperty("masterSlider");
            musicSlider = serializedObject.FindProperty("musicSlider");
            SFXSlider = serializedObject.FindProperty("SFXSlider");
            UISlider = serializedObject.FindProperty("UISlider");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ReachUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            DrawPropertySafely(UIManagerAsset, "UI Manager");
            DrawPropertySafely(fmodEventEmitter, "FMOD Event Emitter");
            DrawPropertySafely(masterSlider, "Master Slider");
            DrawPropertySafely(musicSlider, "Music Slider");
            DrawPropertySafely(SFXSlider, "SFX Slider");
            DrawPropertySafely(UISlider, "UI Slider");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPropertySafely(SerializedProperty property, string label)
        {
            if (property != null && property.propertyType != SerializedPropertyType.Generic)
            {
                ReachUIEditorHandler.DrawProperty(property, customSkin, label);
            }
            else
            {
                EditorGUILayout.HelpBox($"Property '{label}' is missing or invalid.", MessageType.Warning);
            }
        }
    }
}
#endif