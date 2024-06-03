// Unity
using UnityEngine;
using UnityEditor;

// GUPS
using GUPS.EasyPerformanceMonitor.Provider;

namespace GUPS.EasyPerformanceMonitor.Editor
{
    /// <summary>
    /// Custom editor for managing and configuring performance providers in the Unity editor.
    /// </summary>
    [CustomEditor(typeof(APerformanceProvider), editorForChildClasses: true)]
    public class PerformanceProviderEditor : UnityEditor.Editor
    {
        private SerializedProperty isScaleAbleProp;
        private SerializedProperty scaleFactorProp;
        private SerializedProperty scaleSuffixesProp;
        private SerializedProperty fetchIntervalProp;
        private SerializedProperty historySizeProp;
        private SerializedProperty storeValuesInCsvFileProp;

        /// <summary>
        /// Called when the editor is enabled, initializing serialized properties.
        /// </summary>
        protected virtual void OnEnable()
        {
            // Initialize serialized properties.
            isScaleAbleProp = serializedObject.FindProperty("isScaleAble");
            scaleFactorProp = serializedObject.FindProperty("scaleFactor");
            scaleSuffixesProp = serializedObject.FindProperty("scaleSuffixes");
            fetchIntervalProp = serializedObject.FindProperty("fetchInterval");
            historySizeProp = serializedObject.FindProperty("historySize");
            storeValuesInCsvFileProp = serializedObject.FindProperty("storeValuesInCsvFile");
        }

        /// <summary>
        /// Override of the default inspector GUI to provide a custom interface for configuring performance providers.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Update serialized object.
            this.serializedObject.Update();

            // General settings section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);

            // Display and edit the 'Is Scalable' property.
            EditorGUILayout.PropertyField(isScaleAbleProp, new GUIContent("Is Scalable", "Toggle to make the provided data scalable."));

            // If 'Is Scalable' is true, display and edit the 'Scale Factor' and 'Scale Suffixes' properties.
            if (this.isScaleAbleProp.boolValue)
            {
                EditorGUILayout.PropertyField(scaleFactorProp, new GUIContent("Scale Factor", "The scale factor for the provided data."));
                EditorGUILayout.PropertyField(scaleSuffixesProp, new GUIContent("Scale Suffixes:", "The scale suffixes for the provided data."), true);
            }

            // Performance settings section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Performance Settings", EditorStyles.boldLabel);

            // Display and edit the 'Fetch Interval' property using a slider.
            this.fetchIntervalProp.floatValue = EditorGUILayout.Slider(new GUIContent("Fetch Interval", "Interval in seconds to fetch the performance value."), this.fetchIntervalProp.floatValue, 0.01f, 1.0f);

            // Display and edit the 'History Size' property using a slider.
            this.historySizeProp.intValue = EditorGUILayout.IntSlider(new GUIContent("History Size", "The count of last read performance values, used to calculate min/mean/max values."), this.historySizeProp.intValue, 1, 100);

            // File settings section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("File Settings", EditorStyles.boldLabel);

            // Display and edit the 'Store Values in CSV File' property.
            EditorGUILayout.PropertyField(this.storeValuesInCsvFileProp, new GUIContent("Store Values in CSV File", "Store the graph values inside the 'Application.persistentDataPath' in a CSV file."));

            // Check if there is a modified property, to refresh the performance provider.
            bool var_Modified = this.serializedObject.hasModifiedProperties;

            // Apply modified properties to the serialized object.
            this.serializedObject.ApplyModifiedProperties();

            // Refresh the performance provider, if a property was modified.
            if (var_Modified)
            {
                if (Application.isPlaying)
                {
                    (this.serializedObject.targetObject as APerformanceProvider).Refresh();
                }
            }
        }
    }
}