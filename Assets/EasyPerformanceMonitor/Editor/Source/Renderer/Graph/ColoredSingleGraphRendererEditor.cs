// Unity
using UnityEngine;
using UnityEditor;

// GUPS
using GUPS.EasyPerformanceMonitor.Renderer;

namespace GUPS.EasyPerformanceMonitor.Editor
{
    /// <summary>
    /// Custom editor for managing and configuring colored single graph renderers in the Unity editor.
    /// </summary>
    [CustomEditor(typeof(ColoredSingleGraphRenderer), editorForChildClasses: true)]
    public class ColoredSingleGraphRendererEditor : GraphRendererEditor
    {
        // The serialized properties of the ColoredSingleGraphRendererEditor component.
        private SerializedProperty colorProp;
        private SerializedProperty legendImageProp;

        /// <summary>
        /// Called when the editor is enabled, initializing serialized properties.
        /// </summary>
        protected override void OnEnable()
        {
            // Call base implementation.
            base.OnEnable();

            // Initialize serialized properties.
            this.colorProp = this.serializedObject.FindProperty("color");
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

                // Display the 'Color' property.
                EditorGUILayout.PropertyField(this.colorProp, new GUIContent("Color", "The color for the graph values."));
            
                // Display the 'Legend Image' property.
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
    }
}