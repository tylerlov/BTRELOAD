#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Reach
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(GraphicsManager))]
    public class GraphicsManagerEditor : Editor
    {
        private GraphicsManager gmTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            gmTarget = (GraphicsManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = ReachUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = ReachUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            // Update the serializedObject
            serializedObject.Update();

            var resolutionDropdown = serializedObject.FindProperty("resolutionDropdown");
            var windowModeSelector = serializedObject.FindProperty("windowModeSelector");
            var vSyncSwitch = serializedObject.FindProperty("vSyncSwitch"); // Add this line

            // Header
            if (customSkin != null)
            {
                ReachUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            }
            else
            {
                EditorGUILayout.LabelField("Resources", EditorStyles.boldLabel);
            }

            // Resolution Dropdown
            EditorGUILayout.PropertyField(resolutionDropdown, new GUIContent("Resolution Dropdown"));

            // Window Mode Selector
            EditorGUILayout.PropertyField(windowModeSelector, new GUIContent("Window Mode Selector"));

            // VSync Switch
            EditorGUILayout.PropertyField(vSyncSwitch, new GUIContent("VSync Switch")); // Add this line

            // Texture Quality Selector
            var textureQualitySelector = serializedObject.FindProperty("textureQualitySelector");
            EditorGUILayout.PropertyField(textureQualitySelector, new GUIContent("Texture Quality Selector"));

            // Anisotropic Filtering Selector
            var anisotropicFilteringSelector = serializedObject.FindProperty("anisotropicFilteringSelector");
            EditorGUILayout.PropertyField(anisotropicFilteringSelector, new GUIContent("Anisotropic Filtering Selector"));

            // Apply changes
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif