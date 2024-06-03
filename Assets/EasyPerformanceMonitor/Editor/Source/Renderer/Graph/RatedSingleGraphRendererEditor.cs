// Microsoft
using System;

// Unity
using UnityEngine;
using UnityEditor;

// GUPS
using GUPS.EasyPerformanceMonitor.Renderer;
using GUPS.EasyPerformanceMonitor.Provider;

namespace GUPS.EasyPerformanceMonitor.Editor
{
    /// <summary>
    /// Custom editor for managing and configuring rated single graph renderers in the Unity editor.
    /// </summary>
    [CustomEditor(typeof(RatedSingleGraphRenderer), editorForChildClasses: true)]
    public class RatedSingleGraphRendererEditor : GraphRendererEditor
    {
        // The serialized properties of the RatedSingleGraphRenderer component.
        private SerializedProperty highIsGoodProp;
        private SerializedProperty colorsProp;
        private SerializedProperty desktopThresholdsProp;
        private SerializedProperty mobileThresholdsProp;
        private SerializedProperty consoleThresholdsProp;
        private SerializedProperty legendImageProp;

        private int selectedThresholdType = 0;

        /// <summary>
        /// Called when the editor is enabled, initializing serialized properties.
        /// </summary>
        protected override void OnEnable()
        {
            // Call base implementation.
            base.OnEnable();

            // Initialize serialized properties.
            this.highIsGoodProp = this.serializedObject.FindProperty("highIsGood");
            this.colorsProp = this.serializedObject.FindProperty("colors");
            this.desktopThresholdsProp = this.serializedObject.FindProperty("desktopThresholds");
            this.mobileThresholdsProp = this.serializedObject.FindProperty("mobileThresholds");
            this.consoleThresholdsProp = this.serializedObject.FindProperty("consoleThresholds");
            this.legendImageProp = this.serializedObject.FindProperty("legendImage");
        }

        /// <summary>
        /// Override of the default inspector GUI to provide a custom interface for configuring graph renderers.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Update serialized object.
            this.serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Graph Settings", EditorStyles.boldLabel);

            // Display and edit the properties.
            var var_Providers = this.GetProvider();

            if (var_Providers.Length > 0)
            {
                // Show provider name.
                EditorGUILayout.LabelField(new GUIContent("-> Provider - " + var_Providers[0].Name + ":", "Settings for provider" + "."));

                // Indent.
                EditorGUI.indentLevel++;

                // Display and edit the 'HighIsGood' property.
                this.highIsGoodProp.boolValue = 0 == EditorGUILayout.Popup(new GUIContent("High value is:", "Is a higher values considered to be good or bad. Depending on that, a low threshold can be good or bad. For example low fps are good, but a high memory might be bad."), highIsGoodProp.boolValue ? 0 : 1, new String[]{ "Good", "Bad" });

                // Display and edit the threshold properties.
                this.selectedThresholdType = EditorGUILayout.Popup(new GUIContent("Thresholds for platform:", "Set custom thresholds for each platform."), this.selectedThresholdType, new String[] { "Desktop", "Mobile", "Console" });

                if(this.selectedThresholdType == 0)
                {
                    this.OnGuiThresholds(this.desktopThresholdsProp, var_Providers[0], this.highIsGoodProp.boolValue);
                }
                else if(this.selectedThresholdType == 1)
                {
                    this.OnGuiThresholds(this.mobileThresholdsProp, var_Providers[0], this.highIsGoodProp.boolValue);
                }
                else if(this.selectedThresholdType == 2)
                {
                    this.OnGuiThresholds(this.consoleThresholdsProp, var_Providers[0], this.highIsGoodProp.boolValue);
                }

                while (this.colorsProp.arraySize < 3)
                {
                    this.colorsProp.InsertArrayElementAtIndex(this.colorsProp.arraySize);
                }

                // Display and edit the 'Colors' property.
                EditorGUILayout.PropertyField(this.colorsProp.GetArrayElementAtIndex(0), new GUIContent("Good Color", "Color for good values."));
                EditorGUILayout.PropertyField(this.colorsProp.GetArrayElementAtIndex(1), new GUIContent("Warning Color", "Color for values above/below the warning threshold."));
                EditorGUILayout.PropertyField(this.colorsProp.GetArrayElementAtIndex(2), new GUIContent("Critical Color", "Color for values above/below the critical threshold."));

                EditorGUILayout.PropertyField(this.legendImageProp, new GUIContent("Legend - Image", "The image used to display the legend color for the single graph in the renderer."));

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
                    (this.serializedObject.targetObject as AGraphRenderer).RefreshGraph();
                }
            }

            // Call base implementation - Display Design Settings.
            base.OnInspectorGUI();
        }

        /// <summary>
        /// Displays the threshold settings for the graph renderer.
        /// </summary>
        /// <param name="_SerializedPropertyArray">The serialized property array to display.</param>
        /// <param name="_HighIsGood">Whether a high value is considered to be good or bad.</param>
        private void OnGuiThresholds(SerializedProperty _SerializedPropertyArray, IPerformanceProvider _Provider, bool _HighIsGood)
        {
            while (_SerializedPropertyArray.arraySize < 2)
            {
                _SerializedPropertyArray.InsertArrayElementAtIndex(_SerializedPropertyArray.arraySize);
            }

            if (_HighIsGood)
            {
                EditorGUILayout.PropertyField(_SerializedPropertyArray.GetArrayElementAtIndex(0), new GUIContent("Good Threshold (" + _Provider.Unit + ")", "Threshold for good values."));
                EditorGUILayout.PropertyField(_SerializedPropertyArray.GetArrayElementAtIndex(1), new GUIContent("Warning Threshold (" + _Provider.Unit + ")", "Threshold for warning values."));
            }
            else
            {
                EditorGUILayout.PropertyField(_SerializedPropertyArray.GetArrayElementAtIndex(0), new GUIContent("Warning Threshold (" + _Provider.Unit + ")", "Threshold for warning values."));
                EditorGUILayout.PropertyField(_SerializedPropertyArray.GetArrayElementAtIndex(1), new GUIContent("Critical Threshold (" + _Provider.Unit + ")", "Threshold for critical values."));
            }
        }
    }
}