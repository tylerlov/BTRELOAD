// Unity
using UnityEngine;
using UnityEditor;

// GUPS
using GUPS.EasyPerformanceMonitor.Provider;
using GUPS.EasyPerformanceMonitor.Renderer;
using System;

namespace GUPS.EasyPerformanceMonitor.Editor
{
    /// <summary>
    /// Custom editor for inspecting and modifying the BarRenderer component in the Unity editor.
    /// </summary>
    [CustomEditor(typeof(BarRenderer), editorForChildClasses: true)]
    public class BarRendererEditor : UnityEditor.Editor
    {
        // The serialized properties of the BarRenderer component.
        private SerializedProperty lowerBoundValuesProp;
        private SerializedProperty upperBoundValuesProp;
        private SerializedProperty uiValueSliderProp;
        private SerializedProperty uiValuePercentageTextProp;

        private int selectedThresholdType = 0;

        /// <summary>
        /// Initializes serialized properties when the editor is enabled.
        /// </summary>
        private void OnEnable()
        {
            // Initialize serialized properties.
            this.lowerBoundValuesProp = this.serializedObject.FindProperty("lowerBoundValues");
            this.upperBoundValuesProp = this.serializedObject.FindProperty("upperBoundValues");
            this.uiValueSliderProp = this.serializedObject.FindProperty("uiValueSlider");
            this.uiValuePercentageTextProp = this.serializedObject.FindProperty("uiValuePercentageText");
        }

        /// <summary>
        /// Overrides the default inspector GUI to display custom options for the RatedTextRenderer component.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Update serialized object.
            serializedObject.Update();

            // General settings section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bar Renderer Settings", EditorStyles.boldLabel);

            // Display and edit the properties.
            var var_Providers = this.GetProvider();

            if(var_Providers.Length > 0)
            { 
                // Show provider name.
                EditorGUILayout.LabelField(new GUIContent("-> Provider - " + var_Providers[0].Name + ":", "Settings for provider" + "."));

                // Indent.
                EditorGUI.indentLevel++;

                // Display and edit the threshold properties.
                this.selectedThresholdType = EditorGUILayout.Popup(new GUIContent("Boundaries for platform:", "Set custom boundaries for each platform."), this.selectedThresholdType, new String[] { "Desktop", "Mobile", "Console" });

                while (this.lowerBoundValuesProp.arraySize < 3)
                {
                    this.lowerBoundValuesProp.InsertArrayElementAtIndex(this.lowerBoundValuesProp.arraySize);
                }

                while (this.upperBoundValuesProp.arraySize < 3)
                {
                    this.upperBoundValuesProp.InsertArrayElementAtIndex(this.upperBoundValuesProp.arraySize);
                }

                // Display and edit the 'Min Value' property.
                EditorGUILayout.PropertyField(this.lowerBoundValuesProp.GetArrayElementAtIndex(this.selectedThresholdType), new GUIContent("Min Value (" + var_Providers[0].Unit + ")", "Is the min value of the bar, representing 0%."));

                // Display and edit the 'Max Value' property.
                EditorGUILayout.PropertyField(this.upperBoundValuesProp.GetArrayElementAtIndex(this.selectedThresholdType), new GUIContent("Max Value (" + var_Providers[0].Unit + ")", "Is the max value of the bar, representing 100%."));

                // Display and edit the 'Slider' property with the option to expand array elements.
                EditorGUILayout.PropertyField(this.uiValueSliderProp, new GUIContent("Slider", "UI Slider components representing the bar object."));

                // Display and edit the 'Percent Texts' property with the option to expand array elements.
                EditorGUILayout.PropertyField(this.uiValuePercentageTextProp, new GUIContent("Percent Text", "UI Text components associated with normalized percentage value."));

                // Unindent.
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.LabelField("-> No provider found.");
            }

            // Apply modified properties to the serialized object.
            bool var_Modified = this.serializedObject.hasModifiedProperties;

            this.serializedObject.ApplyModifiedProperties();

            if (var_Modified)
            {
                if (Application.isPlaying)
                {
                    (this.serializedObject.targetObject as IBarRenderer).RefreshBar();
                }
            }
        }

        /// <summary>
        /// Get all performance provider of the target object.
        /// </summary>
        /// <returns>An array of performance provider.</returns>
        protected IPerformanceProvider[] GetProvider()
        {
            // Get the target object.
            MonoBehaviour var_Target = this.serializedObject.targetObject as MonoBehaviour;

            // Get the providers.
            var var_Providers = var_Target.GetComponents<IPerformanceProvider>();

            // Return the providers.
            return var_Providers;
        }
    }
}